/*
 * appdomain.c: AppDomain functions
 *
 * Authors:
 *	Dietmar Maurer (dietmar@ximian.com)
 *	Patrik Torstensson
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * (c) 2001-2003 Ximian, Inc. (http://www.ximian.com)
 */

#include <config.h>
#include <glib.h>
#include <string.h>

#include <mono/os/gc_wrapper.h>

#include <mono/metadata/object.h>
#include <mono/metadata/domain-internals.h>
#include "mono/metadata/metadata-internals.h"
#include <mono/metadata/assembly.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/socket-io.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/marshal.h>
#include <mono/utils/mono-uri.h>

#define MONO_CORLIB_VERSION 27

CRITICAL_SECTION mono_delegate_section;

static gunichar2 process_guid [36];
static gboolean process_guid_set = FALSE;

static gboolean shutting_down = FALSE;

static MonoAssembly *
mono_domain_assembly_preload (MonoAssemblyName *aname,
			      gchar **assemblies_path,
			      gpointer user_data);

static void
mono_domain_fire_assembly_load (MonoAssembly *assembly, gpointer user_data);

static void
add_assemblies_to_domain (MonoDomain *domain, MonoAssembly *ass);

static MonoMethod *
look_for_method_by_name (MonoClass *klass, const gchar *name);

static void
mono_domain_unload (MonoDomain *domain);

/*
 * mono_runtime_init:
 * @domain: domain returned by mono_init ()
 *
 * Initialize the core AppDomain: this function will run also some
 * IL initialization code, so it needs the execution engine to be fully 
 * operational.
 *
 * AppDomain.SetupInformation is set up in mono_runtime_exec_main, where
 * we know the entry_assembly.
 *
 */
void
mono_runtime_init (MonoDomain *domain, MonoThreadStartCB start_cb,
		   MonoThreadAttachCB attach_cb)
{
	MonoAppDomainSetup *setup;
	MonoAppDomain *ad;
	MonoClass *class;
	MonoString *arg;
	
	MONO_GC_PRE_INIT ();
	mono_marshal_init ();

	mono_install_assembly_preload_hook (mono_domain_assembly_preload, NULL);
	mono_install_assembly_load_hook (mono_domain_fire_assembly_load, NULL);
	mono_install_lookup_dynamic_token (mono_reflection_lookup_dynamic_token);

	mono_thread_init (start_cb, attach_cb);

	class = mono_class_from_name (mono_defaults.corlib, "System", "AppDomainSetup");
	setup = (MonoAppDomainSetup *) mono_object_new (domain, class);

	class = mono_class_from_name (mono_defaults.corlib, "System", "AppDomain");
	ad = (MonoAppDomain *) mono_object_new (domain, class);
	ad->data = domain;
	domain->domain = ad;
	domain->setup = setup;

	InitializeCriticalSection (&mono_delegate_section);

	mono_context_init (domain);
	mono_context_set (domain->default_context);

	mono_type_initialization_init ();

	
	/*
	 * Create an instance early since we can't do it when there is no memory.
	 */
	arg = mono_string_new (domain, "Out of memory");
	domain->out_of_memory_ex = mono_exception_from_name_two_strings (mono_defaults.corlib, "System", "OutOfMemoryException", arg, NULL);
	
	/* 
	 * These two are needed because the signal handlers might be executing on
	 * an alternate stack, and Boehm GC can't handle that.
	 */
	arg = mono_string_new (domain, "A null value was found where an object instance was required");
	domain->null_reference_ex = mono_exception_from_name_two_strings (mono_defaults.corlib, "System", "NullReferenceException", arg, NULL);
	arg = mono_string_new (domain, "The requested operation caused a stack overflow.");
	domain->stack_overflow_ex = mono_exception_from_name_two_strings (mono_defaults.corlib, "System", "StackOverflowException", arg, NULL);
	
	/* GC init has to happen after thread init */
	mono_gc_init ();

	mono_network_init ();

	/* mscorlib is loaded before we install the load hook */
	mono_domain_fire_assembly_load (mono_defaults.corlib->assembly, NULL);

	return;
}

static int
mono_get_corlib_version (void)
{
	MonoClass *klass;
	MonoClassField *field;
	MonoObject *value;

	klass = mono_class_from_name (mono_defaults.corlib, "System", "Environment");
	mono_class_init (klass);
	field = mono_class_get_field_from_name (klass, "mono_corlib_version");
	if (!field)
		return -1;
	if (! (field->type->attrs & FIELD_ATTRIBUTE_STATIC))
		return -1;
	value = mono_field_get_value_object (mono_domain_get (), field, NULL);
	return *(gint32*)((gchar*)value + sizeof (MonoObject));
}

const char*
mono_check_corlib_version (void)
{
	int version = mono_get_corlib_version ();
	if (version != MONO_CORLIB_VERSION)
		return g_strdup_printf ("expected corlib version %d, found %d.", MONO_CORLIB_VERSION, version);
	else
		return NULL;
}

void
mono_context_init (MonoDomain *domain)
{
	MonoClass *class;
	MonoAppContext *context;

	class = mono_class_from_name (mono_defaults.corlib, "System.Runtime.Remoting.Contexts", "Context");
	context = (MonoAppContext *) mono_object_new (domain, class);
	context->domain_id = domain->domain_id;
	context->context_id = 0;
	domain->default_context = context;
}

/* This must not be called while there are still running threads executing
 * managed code.
 */
void
mono_runtime_cleanup (MonoDomain *domain)
{
	shutting_down = TRUE;

	/* This ends up calling any pending pending (for at most 2 seconds) */
	mono_gc_cleanup ();

	mono_network_cleanup ();
}

static MonoDomainFunc quit_function = NULL;

void
mono_runtime_install_cleanup (MonoDomainFunc func)
{
	quit_function = func;
}

void
mono_runtime_quit ()
{
	if (quit_function != NULL)
		quit_function (mono_get_root_domain (), NULL);
}

gboolean
mono_runtime_is_shutting_down (void)
{
	return shutting_down;
}

gboolean
mono_domain_has_type_resolve (MonoDomain *domain)
{
	static MonoClassField *field = NULL;
	MonoObject *o;

	if (field == NULL) {
		MonoClass *klass = mono_defaults.appdomain_class;
		int i;

		for (i = 0; i < klass->field.count; ++i)
			if (strcmp (klass->fields [i].name, "TypeResolve") == 0)
				field = &klass->fields [i];
		g_assert (field);
	}

	mono_field_get_value ((MonoObject*)(domain->domain), field, &o);
	return o != NULL;
}

MonoReflectionAssembly *
mono_domain_try_type_resolve (MonoDomain *domain, char *name, MonoObject *tb)
{
	MonoClass *klass;
	void *params [1];
	static MonoMethod *method = NULL;

	g_assert (domain != NULL && ((name != NULL) || (tb != NULL)));

	if (method == NULL) {
		klass = domain->domain->mbr.obj.vtable->klass;
		g_assert (klass);

		method = look_for_method_by_name (klass, "DoTypeResolve");
		if (method == NULL) {
			g_warning ("Method AppDomain.DoTypeResolve not found.\n");
			return NULL;
		}
	}

	if (name)
		*params = (MonoObject*)mono_string_new (mono_domain_get (), name);
	else
		*params = tb;
	return (MonoReflectionAssembly *) mono_runtime_invoke (method, domain->domain, params, NULL);
}

/*
 * mono_domain_set:
 *
 *   Set the current appdomain to @domain. If @force is set, set it even
 * if it is being unloaded.
 * Returns:
 *   - TRUE on success
 *   - FALSE if the domain is unloaded
 */
gboolean
mono_domain_set (MonoDomain *domain, gboolean force)
{
	if (!force && domain->state == MONO_APPDOMAIN_UNLOADED)
		return FALSE;

	mono_domain_set_internal (domain);

	return TRUE;
}

MonoObject *
ves_icall_System_AppDomain_GetData (MonoAppDomain *ad, MonoString *name)
{
	MonoDomain *add = ad->data;
	MonoObject *o;
	char *str;

	MONO_ARCH_SAVE_REGS;

	g_assert (ad != NULL);
	g_assert (name != NULL);

	str = mono_string_to_utf8 (name);

	mono_domain_lock (add);

	if (!strcmp (str, "APPBASE"))
		o = (MonoObject *)add->setup->application_base;
	else if (!strcmp (str, "APP_CONFIG_FILE"))
		o = (MonoObject *)add->setup->configuration_file;
	else if (!strcmp (str, "DYNAMIC_BASE"))
		o = (MonoObject *)add->setup->dynamic_base;
	else if (!strcmp (str, "APP_NAME"))
		o = (MonoObject *)add->setup->application_name;
	else if (!strcmp (str, "CACHE_BASE"))
		o = (MonoObject *)add->setup->cache_path;
	else if (!strcmp (str, "PRIVATE_BINPATH"))
		o = (MonoObject *)add->setup->private_bin_path;
	else if (!strcmp (str, "BINPATH_PROBE_ONLY"))
		o = (MonoObject *)add->setup->private_bin_path_probe;
	else if (!strcmp (str, "SHADOW_COPY_DIRS"))
		o = (MonoObject *)add->setup->shadow_copy_directories;
	else if (!strcmp (str, "FORCE_CACHE_INSTALL"))
		o = (MonoObject *)add->setup->shadow_copy_files;
	else 
		o = mono_g_hash_table_lookup (add->env, name);

	mono_domain_unlock (add);
	g_free (str);

	if (!o)
		return NULL;

	return o;
}

void
ves_icall_System_AppDomain_SetData (MonoAppDomain *ad, MonoString *name, MonoObject *data)
{
	MonoDomain *add = ad->data;

	MONO_ARCH_SAVE_REGS;

	g_assert (ad != NULL);
	g_assert (name != NULL);

	mono_domain_lock (add);

	mono_g_hash_table_insert (add->env, name, data);

	mono_domain_unlock (add);
}

MonoAppDomainSetup *
ves_icall_System_AppDomain_getSetup (MonoAppDomain *ad)
{
	MONO_ARCH_SAVE_REGS;

	g_assert (ad != NULL);
	g_assert (ad->data != NULL);

	return ad->data->setup;
}

MonoString *
ves_icall_System_AppDomain_getFriendlyName (MonoAppDomain *ad)
{
	MONO_ARCH_SAVE_REGS;

	g_assert (ad != NULL);
	g_assert (ad->data != NULL);

	return mono_string_new (ad->data, ad->data->friendly_name);
}

MonoAppDomain *
ves_icall_System_AppDomain_getCurDomain ()
{
	MonoDomain *add = mono_domain_get ();

	MONO_ARCH_SAVE_REGS;

	return add->domain;
}

MonoAppDomain *
ves_icall_System_AppDomain_getDomainByID (gint32 domain_id)
{
	MonoDomain *add = mono_domain_get_by_id (domain_id);

	MONO_ARCH_SAVE_REGS;

	return ((add == NULL) ? NULL : add->domain);
}

static void
add_assembly_to_domain (gpointer key, gpointer value, gpointer user_data)
{
	add_assemblies_to_domain ((MonoDomain*)user_data, (MonoAssembly*)value);
}

MonoAppDomain *
ves_icall_System_AppDomain_createDomain (MonoString *friendly_name, MonoAppDomainSetup *setup)
{
	MonoDomain *domain = mono_domain_get ();
	MonoClass *adclass;
	MonoAppDomain *ad;
	MonoDomain *data;
	
	MONO_ARCH_SAVE_REGS;

	adclass = mono_class_from_name (mono_defaults.corlib, "System", "AppDomain");

	/* FIXME: pin all those objects */
	data = mono_domain_create();

	ad = (MonoAppDomain *) mono_object_new (data, adclass);
	ad->data = data;
	data->domain = ad;
	data->setup = setup;
	data->friendly_name = mono_string_to_utf8 (friendly_name);
	data->out_of_memory_ex = mono_exception_from_name_domain (data, mono_defaults.corlib, "System", "OutOfMemoryException");

	mono_context_init (data);

	/* The new appdomain should have all assemblies loaded */
	mono_domain_lock (domain);
	g_hash_table_foreach (domain->assemblies, add_assembly_to_domain, data);
	mono_domain_unlock (domain);

	return ad;
}

typedef struct {
	MonoArray *res;
	MonoDomain *domain;
	int idx;
} add_assembly_helper_t;

static void
add_assembly (gpointer key, gpointer value, gpointer user_data)
{
	add_assembly_helper_t *ah = (add_assembly_helper_t *) user_data;

	mono_array_set (ah->res, gpointer, ah->idx++, mono_assembly_get_object (ah->domain, value));
}

MonoArray *
ves_icall_System_AppDomain_GetAssemblies (MonoAppDomain *ad)
{
	MonoDomain *domain = ad->data; 
	static MonoClass *System_Reflection_Assembly;
	MonoArray *res;
	add_assembly_helper_t ah;
	
	MONO_ARCH_SAVE_REGS;

	if (!System_Reflection_Assembly)
		System_Reflection_Assembly = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "Assembly");

	res = mono_array_new (domain, System_Reflection_Assembly, g_hash_table_size (domain->assemblies));

	ah.domain = domain;
	ah.res = res;
	ah.idx = 0;
	mono_domain_lock (domain);
	g_hash_table_foreach (domain->assemblies, add_assembly, &ah);
	mono_domain_unlock (domain);

	return res;
}

/*
 * Used to find methods in AppDomain class.
 * It only works if there are no multiple signatures for any given method name
 */
static MonoMethod *
look_for_method_by_name (MonoClass *klass, const gchar *name)
{
	gint i;
	MonoMethod *method;

	for (i = 0; i < klass->method.count; i++) {
		method = klass->methods [i];
		if (!strcmp (method->name, name))
			return method;
	}

	return NULL;
}

static MonoReflectionAssembly *
try_assembly_resolve (MonoDomain *domain, MonoString *fname)
{
	MonoClass *klass;
	MonoMethod *method;
	void *params [1];

	g_assert (domain != NULL && fname != NULL);

	klass = domain->domain->mbr.obj.vtable->klass;
	g_assert (klass);
	
	method = look_for_method_by_name (klass, "DoAssemblyResolve");
	if (method == NULL) {
		g_warning ("Method AppDomain.DoAssemblyResolve not found.\n");
		return NULL;
	}

	*params = fname;
	return (MonoReflectionAssembly *) mono_runtime_invoke (method, domain->domain, params, NULL);
}

static void
add_assemblies_to_domain (MonoDomain *domain, MonoAssembly *ass)
{
	gint i;

	if (!ass->aname.name)
		return;

	mono_domain_lock (domain);

	if (g_hash_table_lookup (domain->assemblies, ass->aname.name)) {
		mono_domain_unlock (domain);
		return; /* This is ok while no lazy loading of assemblies */
	}

	g_hash_table_insert (domain->assemblies, (gpointer) ass->aname.name, ass);
	mono_domain_unlock (domain);

	if (ass->image->references)
		for (i = 0; ass->image->references [i] != NULL; i++)
			add_assemblies_to_domain (domain, ass->image->references [i]);
}

static void
mono_domain_fire_assembly_load (MonoAssembly *assembly, gpointer user_data)
{
	MonoDomain *domain = mono_domain_get ();
	MonoReflectionAssembly *ref_assembly;
	MonoClass *klass;
	MonoMethod *method;
	void *params [1];

	klass = domain->domain->mbr.obj.vtable->klass;

	method = look_for_method_by_name (klass, "DoAssemblyLoad");
	if (method == NULL) {
		g_warning ("Method AppDomain.DoAssemblyLoad not found.\n");
		return;
	}

	add_assemblies_to_domain (domain, assembly);

	ref_assembly = mono_assembly_get_object (domain, assembly);
	g_assert (ref_assembly);

	*params = ref_assembly;
	mono_runtime_invoke (method, domain->domain, params, NULL);
}

static gchar *
reduce_path (const gchar *dirname)
{
	gchar **parts;
	gchar *part;
	GList *list, *tmp;
	GString *result;
	gchar *res;
	gint i;

	parts = g_strsplit (dirname, G_DIR_SEPARATOR_S, 0);
	list = NULL;
	for (i = 0; (part = parts [i]) != NULL; i++) {
		if (!strcmp (part, "."))
			continue;

		if (!strcmp (part, "..")) {
			if (list && list->next) /* Don't remove root */
				list = g_list_delete_link (list, list);
		} else {
			list = g_list_prepend (list, part);
		}
	}

	result = g_string_new ("");
	list = g_list_reverse (list);

	for (tmp = list; tmp; tmp = tmp->next) {
		gchar *data = (gchar *) tmp->data;

		if (data && *data) {
#ifdef PLATFORM_WIN32
			if (result->len == 0)
				g_string_append_printf (result, "%s\\", data);
			else if (result->str [result->len - 1] == '\\')
				g_string_append_printf (result, "%s", data);
			else
#endif
				g_string_append_printf (result, "%c%s",
							G_DIR_SEPARATOR, data);
		}
	}
	
	res = result->str;
	g_string_free (result, FALSE);
	g_list_free (list);
	g_strfreev (parts);
	return res;
}

static void
set_domain_search_path (MonoDomain *domain)
{
	MonoAppDomainSetup *setup;
	gchar **tmp;
	gchar *utf8;
	gint i;
	gint npaths = 0;
	gchar **pvt_split = NULL;
	GError *error = NULL;
	gint appbaselen = -1;

	if ((domain->search_path != NULL) && !domain->setup->path_changed)
		return;

	setup = domain->setup;
	if (!setup->application_base)
		return; /* Must set application base to get private path working */

	npaths++;
	if (setup->private_bin_path) {
		utf8 = mono_string_to_utf8 (setup->private_bin_path);
		pvt_split = g_strsplit (utf8, G_SEARCHPATH_SEPARATOR_S, 1000);
		g_free (utf8);
		for (tmp = pvt_split; *tmp; tmp++, npaths++);
	}

	if (!npaths) {
		if (pvt_split)
			g_strfreev (pvt_split);
		/*
		 * Don't do this because the first time is called, the domain
		 * setup is not finished.
		 *
		 * domain->search_path = g_malloc (sizeof (char *));
		 * domain->search_path [0] = NULL;
		*/
		return;
	}

	if (domain->search_path)
		g_strfreev (domain->search_path);

	domain->search_path = tmp = g_malloc ((npaths + 1) * sizeof (gchar *));
	tmp [npaths] = NULL;

	*tmp = mono_string_to_utf8 (setup->application_base);

	/* FIXME: is this needed? */
	if (strncmp (*tmp, "file://", 7) == 0) {
		gchar *file = *tmp;
		gchar *uri = *tmp;
		gchar *tmpuri;

		if (uri [7] != '/')
			uri = g_strdup_printf ("file:///%s", uri + 7);

		tmpuri = uri;
		uri = mono_escape_uri_string (tmpuri);
		*tmp = g_filename_from_uri (uri, NULL, &error);
		g_free (uri);

		if (tmpuri != file)
			g_free (tmpuri);

		if (error != NULL) {
			g_warning ("%s\n", error->message);
			g_error_free (error);
			*tmp = file;
		} else {
			g_free (file);
		}
	}

	for (i = 1; pvt_split && i < npaths; i++) {
		if (g_path_is_absolute (pvt_split [i - 1])) {
			tmp [i] = g_strdup (pvt_split [i - 1]);
		} else {
			tmp [i] = g_build_filename (tmp [0], pvt_split [i - 1], NULL);
		}

		if (strchr (tmp [i], '.')) {
			gchar *reduced;
			gchar *freeme;

			reduced = reduce_path (tmp [i]);
			if (appbaselen == -1)
				appbaselen = strlen (tmp [0]);

			if (strncmp (tmp [0], reduced, appbaselen)) {
				g_free (reduced);
				g_free (tmp [i]);
				tmp [i] = g_strdup ("");
				continue;
			}

			freeme = tmp [i];
			tmp [i] = reduced;
			g_free (freeme);
		}
	}
	
	if (setup->private_bin_path_probe != NULL) {
		g_free (tmp [0]);
		tmp [0] = g_strdup ("");
	}
		
	domain->setup->path_changed = FALSE;

	g_strfreev (pvt_split);
}

static gboolean
try_load_from (MonoAssembly **assembly, const gchar *path1, const gchar *path2,
					const gchar *path3, const gchar *path4)
{
	gchar *fullpath;

	*assembly = NULL;
	fullpath = g_build_filename (path1, path2, path3, path4, NULL);
	if (g_file_test (fullpath, G_FILE_TEST_IS_REGULAR))
		*assembly = mono_assembly_open (fullpath, NULL);

	g_free (fullpath);
	return (*assembly != NULL);
}

static MonoAssembly *
real_load (gchar **search_path, const gchar *culture, const gchar *name)
{
	MonoAssembly *result = NULL;
	gchar **path;
	gchar *filename;
	const gchar *local_culture;
	gint len;

	if (!culture || *culture == '\0') {
		local_culture = "";
	} else {
		local_culture = culture;
	}

	filename =  g_strconcat (name, ".dll", NULL);
	len = strlen (filename);

	for (path = search_path; *path; path++) {
		if (**path == '\0')
			continue; /* Ignore empty ApplicationBase */

		/* See test cases in bug #58992 and bug #57710 */
		/* 1st try: [culture]/[name].dll (culture may be empty) */
		strcpy (filename + len - 4, ".dll");
		if (try_load_from (&result, *path, local_culture, "", filename))
			break;

		/* 2nd try: [culture]/[name].exe (culture may be empty) */
		strcpy (filename + len - 4, ".exe");
		if (try_load_from (&result, *path, local_culture, "", filename))
			break;

		/* 3rd try: [culture]/[name]/[name].dll (culture may be empty) */
		strcpy (filename + len - 4, ".dll");
		if (try_load_from (&result, *path, local_culture, name, filename))
			break;

		/* 4th try: [culture]/[name]/[name].exe (culture may be empty) */
		strcpy (filename + len - 4, ".exe");
		if (try_load_from (&result, *path, local_culture, name, filename))
			break;
	}

	g_free (filename);
	return result;
}

/*
 * Try loading the assembly from ApplicationBase and PrivateBinPath 
 * and then from assemblies_path if any.
 */
static MonoAssembly *
mono_domain_assembly_preload (MonoAssemblyName *aname,
			      gchar **assemblies_path,
			      gpointer user_data)
{
	MonoDomain *domain = mono_domain_get ();
	MonoAssembly *result = NULL;

	set_domain_search_path (domain);

	if (domain->search_path && domain->search_path [0] != NULL) {
		result = real_load (domain->search_path, aname->culture, aname->name);
	}

	if (result == NULL && assemblies_path && assemblies_path [0] != NULL) {
		result = real_load (assemblies_path, aname->culture, aname->name);
	}

	return NULL;
}

MonoReflectionAssembly *
ves_icall_System_Reflection_Assembly_LoadFrom (MonoString *fname)
{
	MonoDomain *domain = mono_domain_get ();
	char *name, *filename;
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	MonoAssembly *ass;

	MONO_ARCH_SAVE_REGS;

	if (fname == NULL) {
		MonoException *exc = mono_get_exception_argument_null ("assemblyFile");
		mono_raise_exception (exc);
	}
		
	name = filename = mono_string_to_utf8 (fname);

	ass = mono_assembly_open (filename, &status);
	
	g_free (name);

	if (!ass){
		MonoException *exc = mono_get_exception_file_not_found (fname);
		mono_raise_exception (exc);
	}

	return mono_assembly_get_object (domain, ass);
}

static void
free_assembly_name (MonoAssemblyName *aname)
{
	if (aname == NULL)
		return;

	g_free ((void *) aname->name);
	g_free ((void *) aname->culture);
	g_free ((void *) aname->hash_value);
}

static gboolean
get_info_from_assembly_name (MonoString *assRef, MonoAssemblyName *aname)
{
	gchar *name;
	gchar *value;
	gchar **parts;
	gchar **tmp;
	gint major, minor, build, revision;

	memset (aname, 0, sizeof (MonoAssemblyName));

	name = mono_string_to_utf8 (assRef);
	parts = tmp = g_strsplit (name, ",", 4);
	g_free (name);
	if (!tmp || !*tmp) {
		g_strfreev (tmp);
		return FALSE;
	}

	value = g_strstrip (*tmp);
	/* g_print ("Assembly name: %s\n", value); */
	aname->name = g_strdup (value);
	tmp++;
	if (!*tmp) {
		g_strfreev (parts);
		return TRUE;
	}

	while (*tmp) {
		value = g_strstrip (*tmp);
		if (!g_ascii_strncasecmp (value, "Version=", 8)) {
			if (sscanf (value + 8, "%u.%u.%u.%u",
				    &major, &minor, &build, &revision) != 4) {
				g_strfreev (parts);
				return FALSE;
			}
			/* g_print ("Version: %u.%u.%u.%u\n", major, minor, build, revision); */
			aname->major = major;
			aname->minor = minor;
			aname->build = build;
			aname->revision = revision;
			tmp++;
			continue;
		}

		if (!g_ascii_strncasecmp (value, "Culture=", 8)) {
			gchar *t = g_strdup (value + 8);
			g_strchug (t);
			aname->culture = g_strdup (g_strchomp (t));
			tmp++;
			g_free (t);
			if (g_strcasecmp (aname->culture, "neutral") == 0) {
				g_free ((void *) aname->culture);
				aname->culture = g_strdup ("");
			}
			continue;
		}

		if (!g_ascii_strncasecmp (value, "PublicKeyToken=", 15)) {
			tmp++;
			value += 15;
			if (*value && strncmp (value, "null", 4)) {
				gchar *t = g_strdup (value);
				g_strchug (t);
				g_strlcpy (aname->public_key_token, g_strchomp (value), MONO_PUBLIC_KEY_TOKEN_LENGTH);
				g_free (t);
			}
			continue;
		}

		g_strfreev (parts);
		return FALSE;
	}

	g_strfreev (parts);
	return TRUE;
}

MonoReflectionAssembly *
ves_icall_System_AppDomain_LoadAssemblyRaw (MonoAppDomain *ad, 
											MonoArray *raw_assembly,
											MonoArray *raw_symbol_store, MonoObject *evidence)
{
	MonoAssembly *ass;
	MonoReflectionAssembly *refass = NULL;
	MonoDomain *domain = ad->data;
	MonoImageOpenStatus status;
	guint32 raw_assembly_len = mono_array_length (raw_assembly);
	MonoImage *image = mono_image_open_from_data (mono_array_addr (raw_assembly, gchar, 0), raw_assembly_len, TRUE, NULL);

	if (raw_symbol_store)
		mono_raise_exception (mono_get_exception_not_implemented ("LoadAssemblyRaw: Raw Symbol Store not Implemented"));
  
	if (!image) {
		mono_raise_exception (mono_get_exception_bad_image_format (""));
		return NULL;
	}

	ass = mono_assembly_load_from (image, "", &status);

	if (!ass) {
		mono_image_close (image);
		mono_raise_exception (mono_get_exception_bad_image_format (""));
		return NULL; 
	}

	refass = mono_assembly_get_object (domain, ass);
	refass->evidence = evidence;
	return refass;
}

MonoReflectionAssembly *
ves_icall_System_AppDomain_LoadAssembly (MonoAppDomain *ad,  MonoString *assRef, MonoObject *evidence)
{
	MonoDomain *domain = ad->data; 
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	MonoAssembly *ass;
	MonoAssemblyName aname;
	MonoReflectionAssembly *refass = NULL;

	MONO_ARCH_SAVE_REGS;

	g_assert (assRef != NULL);

	if (!get_info_from_assembly_name (assRef, &aname)) {
		MonoException *exc;

		free_assembly_name (&aname);
		/* This is a parse error... */
		exc = mono_get_exception_file_not_found (assRef);
		mono_raise_exception (exc);
	}

	ass = mono_assembly_load (&aname, NULL, &status);
	free_assembly_name (&aname);

	if (!ass && (refass = try_assembly_resolve (domain, assRef)) == NULL){
		/* FIXME: it doesn't make much sense since we really don't have a filename ... */
		MonoException *exc = mono_get_exception_file_not_found (assRef);
		mono_raise_exception (exc);
	}

	if (refass == NULL)
		refass = mono_assembly_get_object (domain, ass);

	refass->evidence = evidence;
	return refass;
}

void
ves_icall_System_AppDomain_InternalUnload (gint32 domain_id)
{
	MonoDomain * domain = mono_domain_get_by_id (domain_id);

	MONO_ARCH_SAVE_REGS;

	if (NULL == domain) {
		MonoException *exc = mono_get_exception_execution_engine ("Failed to unload domain, domain id not found");
		mono_raise_exception (exc);
	}
	
	if (domain == mono_get_root_domain ()) {
		mono_raise_exception (mono_get_exception_cannot_unload_appdomain ("The default appdomain can not be unloaded."));
		return;
	}

	/* 
	 * Unloading seems to cause problems when running NUnit/NAnt, hence
	 * this workaround.
	 */
	if (g_getenv ("MONO_NO_UNLOAD"))
		return;

	mono_domain_unload (domain);
}

gboolean
ves_icall_System_AppDomain_InternalIsFinalizingForUnload (gint32 domain_id)
{
	MonoDomain *domain = mono_domain_get_by_id (domain_id);

	if (!domain)
		return TRUE;

	return mono_domain_is_unloading (domain);
}

gint32
ves_icall_System_AppDomain_ExecuteAssembly (MonoAppDomain *ad, MonoString *file, 
					    MonoObject *evidence, MonoArray *args)
{
	MonoAssembly *assembly;
	MonoImage *image;
	MonoMethod *method;
	char *filename;
	gint32 res;
	MonoReflectionAssembly *refass;

	MONO_ARCH_SAVE_REGS;

	filename = mono_string_to_utf8 (file);
	assembly = mono_assembly_open (filename, NULL);
	g_free (filename);

	if (!assembly)
		mono_raise_exception (mono_get_exception_file_not_found (filename));

	image = assembly->image;

	method = mono_get_method (image, mono_image_get_entry_point (image), NULL);

	if (!method)
		g_error ("No entry point method found in %s", image->name);

	if (!args)
		args = (MonoArray *) mono_array_new (ad->data, mono_defaults.string_class, 0);

	refass = mono_assembly_get_object (ad->data, assembly);
	refass->evidence = evidence;

	res = mono_runtime_exec_main (method, (MonoArray *)args, NULL);

	return res;
}

gint32 
ves_icall_System_AppDomain_GetIDFromDomain (MonoAppDomain * ad) 
{
	MONO_ARCH_SAVE_REGS;

	return ad->data->domain_id;
}

MonoAppDomain * 
ves_icall_System_AppDomain_InternalSetDomain (MonoAppDomain *ad)
{
	MonoDomain *old_domain = mono_domain_get();

	MONO_ARCH_SAVE_REGS;

	if (!mono_domain_set (ad->data, FALSE))
		mono_raise_exception (mono_get_exception_appdomain_unloaded ());

	return old_domain->domain;
}

MonoAppDomain * 
ves_icall_System_AppDomain_InternalSetDomainByID (gint32 domainid)
{
	MonoDomain *current_domain = mono_domain_get ();
	MonoDomain *domain = mono_domain_get_by_id (domainid);

	MONO_ARCH_SAVE_REGS;

	if (!domain || !mono_domain_set (domain, FALSE))	
		mono_raise_exception (mono_get_exception_appdomain_unloaded ());

	return current_domain->domain;
}

void
ves_icall_System_AppDomain_InternalPushDomainRef (MonoAppDomain *ad)
{
	MONO_ARCH_SAVE_REGS;

	mono_thread_push_appdomain_ref (ad->data);
}

void
ves_icall_System_AppDomain_InternalPushDomainRefByID (gint32 domain_id)
{
	MonoDomain *domain = mono_domain_get_by_id (domain_id);

	MONO_ARCH_SAVE_REGS;

	if (!domain)
		/* 
		 * Raise an exception to prevent the managed code from executing a pop
		 * later.
		 */
		mono_raise_exception (mono_get_exception_appdomain_unloaded ());

	mono_thread_push_appdomain_ref (domain);
}

void
ves_icall_System_AppDomain_InternalPopDomainRef (void)
{
	MONO_ARCH_SAVE_REGS;

	mono_thread_pop_appdomain_ref ();
}

MonoAppContext * 
ves_icall_System_AppDomain_InternalGetContext ()
{
	MONO_ARCH_SAVE_REGS;

	return mono_context_get ();
}

MonoAppContext * 
ves_icall_System_AppDomain_InternalGetDefaultContext ()
{
	MONO_ARCH_SAVE_REGS;

	return mono_domain_get ()->default_context;
}

MonoAppContext * 
ves_icall_System_AppDomain_InternalSetContext (MonoAppContext *mc)
{
	MonoAppContext *old_context = mono_context_get ();

	MONO_ARCH_SAVE_REGS;

	mono_context_set (mc);

	return old_context;
}

MonoString *
ves_icall_System_AppDomain_InternalGetProcessGuid (MonoString* newguid)
{
	MonoDomain* mono_root_domain = mono_get_root_domain ();
	mono_domain_lock (mono_root_domain);
	if (process_guid_set) {
		mono_domain_unlock (mono_root_domain);
		return mono_string_new_utf16 (mono_domain_get (), process_guid, sizeof(process_guid)/2);
	}
	memcpy (process_guid, mono_string_chars(newguid), sizeof(process_guid));
	process_guid_set = TRUE;
	mono_domain_unlock (mono_root_domain);
	return newguid;
}

gboolean
mono_domain_is_unloading (MonoDomain *domain)
{
	if (domain->state == MONO_APPDOMAIN_UNLOADING || domain->state == MONO_APPDOMAIN_UNLOADED)
		return TRUE;
	else
		return FALSE;
}

static void
clear_cached_vtable (gpointer key, gpointer value, gpointer user_data)
{
	MonoClass *klass = (MonoClass*)key;
	MonoDomain *domain = (MonoDomain*)user_data;
	MonoVTable *vt;

	vt = klass->cached_vtable;
	if (vt && vt->domain == domain)
		klass->cached_vtable = NULL;
}

typedef struct unload_data {
	MonoDomain *domain;
	char *failure_reason;
} unload_data;

static guint32
unload_thread_main (void *arg)
{
	unload_data *data = (unload_data*)arg;
	MonoDomain *domain = data->domain;

	/* 
	 * FIXME: Abort our parent thread last, so we can return a failure 
	 * indication if aborting times out.
	 */
	if (!mono_threads_abort_appdomain_threads (domain, 10000)) {
		data->failure_reason = g_strdup_printf ("Aborting of threads in domain %s timed out.", domain->friendly_name);
		return 1;
	}

	/* Finalize all finalizable objects in the doomed appdomain */
	if (!mono_domain_finalize (domain, 10000)) {
		data->failure_reason = g_strdup_printf ("Finalization of domain %s timed out.", domain->friendly_name);
		return 1;
	}

	/* Clear references to our vtables in class->cached_vtable */
	mono_domain_lock (domain);
	mono_g_hash_table_foreach (domain->class_vtable_hash, clear_cached_vtable,
							   domain);
	mono_domain_unlock (domain);

	domain->state = MONO_APPDOMAIN_UNLOADED;

	/* printf ("UNLOADED %s.\n", domain->friendly_name); */

	mono_domain_free (domain, FALSE);

#ifdef HAVE_BOEHM_GC
	GC_gcollect ();
#endif

	return 0;
}

/*
 * mono_domain_unload:
 *
 *  Unloads an appdomain. Follows the process outlined in:
 *  http://blogs.gotdotnet.com/cbrumme
 *  If doing things the 'right' way is too hard or complex, we do it the 
 *  'simple' way, which means do everything needed to avoid crashes and
 *  memory leaks, but not much else.
 */
static void
mono_domain_unload (MonoDomain *domain)
{
	HANDLE thread_handle;
	guint32 tid;
	MonoAppDomainState prev_state;
	MonoMethod *method;
	MonoObject *exc;
	unload_data thread_data;
	MonoDomain *caller_domain = mono_domain_get ();

	/* printf ("UNLOAD STARTING FOR %s.\n", domain->friendly_name); */

	/* Atomically change our state to UNLOADING */
	prev_state = InterlockedCompareExchange (&domain->state,
											 MONO_APPDOMAIN_UNLOADING,
											 MONO_APPDOMAIN_CREATED);
	if (prev_state != MONO_APPDOMAIN_CREATED) {
		if (prev_state == MONO_APPDOMAIN_UNLOADING)
			mono_raise_exception (mono_get_exception_cannot_unload_appdomain ("Appdomain is already being unloaded."));
		else
			if (prev_state == MONO_APPDOMAIN_UNLOADED)
				mono_raise_exception (mono_get_exception_cannot_unload_appdomain ("Appdomain is already unloaded."));
		else
			g_assert_not_reached ();
	}

	mono_domain_set (domain, FALSE);
	/* Notify OnDomainUnload listeners */
	method = look_for_method_by_name (domain->domain->mbr.obj.vtable->klass, "DoDomainUnload");	
	g_assert (method);

	exc = NULL;
	mono_runtime_invoke (method, domain->domain, NULL, &exc);
	if (exc) {
		/* Roll back the state change */
		domain->state = MONO_APPDOMAIN_CREATED;
		mono_domain_set (caller_domain, FALSE);
		mono_raise_exception ((MonoException*)exc);
	}

	thread_data.domain = domain;
	thread_data.failure_reason = NULL;

	/* 
	 * First we create a separate thread for unloading, since
	 * we might have to abort some threads, including the current one.
	 */
	/*
	 * If we create a non-suspended thread, the runtime will hang.
	 * See:
	 * http://bugzilla.ximian.com/show_bug.cgi?id=27663
	 */ 
#if 0
	thread_handle = CreateThread (NULL, 0, unload_thread_main, &thread_data, 0, &tid);
#else
#if defined(PLATFORM_WIN32) && defined (HAVE_BOEHM_GC)
	thread_handle = GC_CreateThread (NULL, 0, unload_thread_main, &thread_data, CREATE_SUSPENDED, &tid);
#else
	thread_handle = CreateThread (NULL, 0, unload_thread_main, &thread_data, CREATE_SUSPENDED, &tid);
#endif
	ResumeThread (thread_handle);
#endif
	
	while (WaitForSingleObjectEx (thread_handle, INFINITE, FALSE) == WAIT_IO_COMPLETION)
		; /* wait for the thread */
	

	mono_domain_set (caller_domain, FALSE);
	if (thread_data.failure_reason) {
		MonoException *ex;

		ex = mono_get_exception_cannot_unload_appdomain (thread_data.failure_reason);
		/* Roll back the state change */
		domain->state = MONO_APPDOMAIN_CREATED;

		g_warning (thread_data.failure_reason);

		g_free (thread_data.failure_reason);

		mono_raise_exception (ex);
	}
}
