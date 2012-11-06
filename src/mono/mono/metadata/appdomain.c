/*
 * appdomain.c: AppDomain functions
 *
 * Authors:
 *	Dietmar Maurer (dietmar@ximian.com)
 *	Patrik Torstensson
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2012 Xamarin Inc
 */
#undef ASSEMBLY_LOAD_DEBUG
#include <config.h>
#include <glib.h>
#include <string.h>
#include <errno.h>
#include <time.h>
#include <sys/types.h>
#include <sys/stat.h>
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_UTIME_H
#include <utime.h>
#else
#ifdef HAVE_SYS_UTIME_H
#include <sys/utime.h>
#endif
#endif

#include <mono/metadata/gc-internal.h>
#include <mono/metadata/object.h>
#include <mono/metadata/domain-internals.h>
#include "mono/metadata/metadata-internals.h"
#include <mono/metadata/assembly.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/socket-io.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/threadpool.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/metadata/attach.h>
#include <mono/metadata/file-io.h>
#include <mono/metadata/lock-tracer.h>
#include <mono/metadata/console-io.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/tokentype.h>
#include <mono/utils/mono-uri.h>
#include <mono/utils/mono-logger-internal.h>
#include <mono/utils/mono-path.h>
#include <mono/utils/mono-stdlib.h>
#include <mono/utils/mono-io-portability.h>
#include <mono/utils/mono-error-internals.h>
#ifdef HOST_WIN32
#include <direct.h>
#endif

/*
 * This is the version number of the corlib-runtime interface. When
 * making changes to this interface (by changing the layout
 * of classes the runtime knows about, changing icall signature or
 * semantics etc), increment this variable. Also increment the
 * pair of this variable in mscorlib in:
 *       mcs/class/mscorlib/System/Environment.cs
 *
 * Changes which are already detected at runtime, like the addition
 * of icalls, do not require an increment.
 */
#define MONO_CORLIB_VERSION 108

typedef struct
{
	int runtime_count;
	int assemblybinding_count;
	MonoDomain *domain;
	gchar *filename;
} RuntimeConfig;

CRITICAL_SECTION mono_delegate_section;

CRITICAL_SECTION mono_strtod_mutex;

static gunichar2 process_guid [36];
static gboolean process_guid_set = FALSE;

static gboolean shutting_down = FALSE;

static gboolean no_exec = FALSE;

static MonoAssembly *
mono_domain_assembly_preload (MonoAssemblyName *aname,
			      gchar **assemblies_path,
			      gpointer user_data);

static MonoAssembly *
mono_domain_assembly_search (MonoAssemblyName *aname,
							 gpointer user_data);

static MonoAssembly *
mono_domain_assembly_postload_search (MonoAssemblyName *aname,
									  gpointer user_data);

static void
mono_domain_fire_assembly_load (MonoAssembly *assembly, gpointer user_data);

static void
add_assemblies_to_domain (MonoDomain *domain, MonoAssembly *ass, GHashTable *hash);

static MonoAppDomain *
mono_domain_create_appdomain_internal (char *friendly_name, MonoAppDomainSetup *setup);

static char *
get_shadow_assembly_location_base (MonoDomain *domain, MonoError *error);

static MonoLoadFunc load_function = NULL;

void
mono_install_runtime_load (MonoLoadFunc func)
{
	load_function = func;
}

MonoDomain*
mono_runtime_load (const char *filename, const char *runtime_version)
{
	g_assert (load_function);
	return load_function (filename, runtime_version);
}

/**
 * mono_runtime_set_no_exec:
 *
 * Instructs the runtime to operate in static mode, i.e. avoid/do not
 * allow managed code execution. This is useful for running the AOT
 * compiler on platforms which allow full-aot execution only.  This
 * should be called before mono_runtime_init ().
 */
void
mono_runtime_set_no_exec (gboolean val)
{
	no_exec = val;
}

/**
 * mono_runtime_get_no_exec:
 *
 * If true, then the runtime will not allow managed code execution.
 */
gboolean
mono_runtime_get_no_exec (void)
{
	return no_exec;
}

static void
create_domain_objects (MonoDomain *domain)
{
	MonoDomain *old_domain = mono_domain_get ();
	MonoString *arg;

	if (domain != old_domain) {
		mono_thread_push_appdomain_ref (domain);
		mono_domain_set_internal_with_options (domain, FALSE);
	}

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

	/*The ephemeron tombstone i*/
	domain->ephemeron_tombstone = mono_object_new (domain, mono_defaults.object_class);

	if (domain != old_domain) {
		mono_thread_pop_appdomain_ref ();
		mono_domain_set_internal_with_options (old_domain, FALSE);
	}

	/* 
	 * This class is used during exception handling, so initialize it here, to prevent
	 * stack overflows while handling stack overflows.
	 */
	mono_class_init (mono_array_class_get (mono_defaults.int_class, 1));
}

/**
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

	mono_portability_helpers_init ();
	
	mono_gc_base_init ();
	mono_monitor_init ();
	mono_thread_pool_init ();
	mono_marshal_init ();

	mono_install_assembly_preload_hook (mono_domain_assembly_preload, GUINT_TO_POINTER (FALSE));
	mono_install_assembly_refonly_preload_hook (mono_domain_assembly_preload, GUINT_TO_POINTER (TRUE));
	mono_install_assembly_search_hook (mono_domain_assembly_search, GUINT_TO_POINTER (FALSE));
	mono_install_assembly_refonly_search_hook (mono_domain_assembly_search, GUINT_TO_POINTER (TRUE));
	mono_install_assembly_postload_search_hook (mono_domain_assembly_postload_search, GUINT_TO_POINTER (FALSE));
	mono_install_assembly_postload_refonly_search_hook (mono_domain_assembly_postload_search, GUINT_TO_POINTER (TRUE));
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

	InitializeCriticalSection (&mono_strtod_mutex);
	
	mono_thread_attach (domain);
	mono_context_init (domain);
	mono_context_set (domain->default_context);

	mono_type_initialization_init ();

	if (!mono_runtime_get_no_exec ())
		create_domain_objects (domain);

	/* GC init has to happen after thread init */
	mono_gc_init ();

#ifndef DISABLE_SOCKETS
	mono_network_init ();
#endif
	
	mono_console_init ();
	mono_attach_init ();

	mono_locks_tracer_init ();

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

/**
 * mono_check_corlib_version
 *
 * Checks that the corlib that is loaded matches the version of this runtime.
 *
 * Returns: NULL if the runtime will work with the corlib, or a g_malloc
 * allocated string with the error otherwise.
 */
const char*
mono_check_corlib_version (void)
{
	int version = mono_get_corlib_version ();
	if (version != MONO_CORLIB_VERSION)
		return g_strdup_printf ("expected corlib version %d, found %d.", MONO_CORLIB_VERSION, version);
	else
		return NULL;
}

/**
 * mono_context_init:
 * @domain: The domain where the System.Runtime.Remoting.Context.Context is initialized
 *
 * Initializes the @domain's default System.Runtime.Remoting's Context.
 */
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

/**
 * mono_runtime_cleanup:
 * @domain: unused.
 *
 * Internal routine.
 *
 * This must not be called while there are still running threads executing
 * managed code.
 */
void
mono_runtime_cleanup (MonoDomain *domain)
{
	shutting_down = TRUE;

	mono_attach_cleanup ();

	/* This ends up calling any pending pending (for at most 2 seconds) */
	mono_gc_cleanup ();

	mono_thread_cleanup ();

#ifndef DISABLE_SOCKETS
	mono_network_cleanup ();
#endif
	mono_marshal_cleanup ();

	mono_type_initialization_cleanup ();

	mono_monitor_cleanup ();
}

static MonoDomainFunc quit_function = NULL;

void
mono_install_runtime_cleanup (MonoDomainFunc func)
{
	quit_function = func;
}

void
mono_runtime_quit ()
{
	if (quit_function != NULL)
		quit_function (mono_get_root_domain (), NULL);
}

/** 
 * mono_runtime_set_shutting_down:
 *
 * Invoked by System.Environment.Exit to flag that the runtime
 * is shutting down.
 */
void
mono_runtime_set_shutting_down (void)
{
	shutting_down = TRUE;
}

/**
 * mono_runtime_is_shutting_down:
 *
 * Returns whether the runtime has been flagged for shutdown.
 *
 * This is consumed by the P:System.Environment.HasShutdownStarted
 * property.
 *
 */
gboolean
mono_runtime_is_shutting_down (void)
{
	return shutting_down;
}

/**
 * mono_domain_create_appdomain:
 * @friendly_name: The friendly name of the appdomain to create
 * @configuration_file: The configuration file to initialize the appdomain with
 * 
 * Returns a MonoDomain initialized with the appdomain
 */
MonoDomain *
mono_domain_create_appdomain (char *friendly_name, char *configuration_file)
{
	MonoAppDomain *ad;
	MonoAppDomainSetup *setup;
	MonoClass *class;

	class = mono_class_from_name (mono_defaults.corlib, "System", "AppDomainSetup");
	setup = (MonoAppDomainSetup *) mono_object_new (mono_domain_get (), class);
	setup->configuration_file = configuration_file != NULL ? mono_string_new (mono_domain_get (), configuration_file) : NULL;

	ad = mono_domain_create_appdomain_internal (friendly_name, setup);

	return mono_domain_from_appdomain (ad);
}

static MonoAppDomainSetup*
copy_app_domain_setup (MonoDomain *domain, MonoAppDomainSetup *setup)
{
	MonoDomain *caller_domain = mono_domain_get ();
	MonoClass *ads_class = mono_class_from_name (mono_defaults.corlib, "System", "AppDomainSetup");
	MonoAppDomainSetup *copy = (MonoAppDomainSetup*)mono_object_new (domain, ads_class);

	mono_domain_set_internal (domain);

	MONO_OBJECT_SETREF (copy, application_base, mono_marshal_xdomain_copy_value ((MonoObject*)setup->application_base));
	MONO_OBJECT_SETREF (copy, application_name, mono_marshal_xdomain_copy_value ((MonoObject*)setup->application_name));
	MONO_OBJECT_SETREF (copy, cache_path, mono_marshal_xdomain_copy_value ((MonoObject*)setup->cache_path));
	MONO_OBJECT_SETREF (copy, configuration_file, mono_marshal_xdomain_copy_value ((MonoObject*)setup->configuration_file));
	MONO_OBJECT_SETREF (copy, dynamic_base, mono_marshal_xdomain_copy_value ((MonoObject*)setup->dynamic_base));
	MONO_OBJECT_SETREF (copy, license_file, mono_marshal_xdomain_copy_value ((MonoObject*)setup->license_file));
	MONO_OBJECT_SETREF (copy, private_bin_path, mono_marshal_xdomain_copy_value ((MonoObject*)setup->private_bin_path));
	MONO_OBJECT_SETREF (copy, private_bin_path_probe, mono_marshal_xdomain_copy_value ((MonoObject*)setup->private_bin_path_probe));
	MONO_OBJECT_SETREF (copy, shadow_copy_directories, mono_marshal_xdomain_copy_value ((MonoObject*)setup->shadow_copy_directories));
	MONO_OBJECT_SETREF (copy, shadow_copy_files, mono_marshal_xdomain_copy_value ((MonoObject*)setup->shadow_copy_files));
	copy->publisher_policy = setup->publisher_policy;
	copy->path_changed = setup->path_changed;
	copy->loader_optimization = setup->loader_optimization;
	copy->disallow_binding_redirects = setup->disallow_binding_redirects;
	copy->disallow_code_downloads = setup->disallow_code_downloads;
	MONO_OBJECT_SETREF (copy, domain_initializer_args, mono_marshal_xdomain_copy_value ((MonoObject*)setup->domain_initializer_args));
	copy->disallow_appbase_probe = setup->disallow_appbase_probe;
	MONO_OBJECT_SETREF (copy, application_trust, mono_marshal_xdomain_copy_value ((MonoObject*)setup->application_trust));
	MONO_OBJECT_SETREF (copy, configuration_bytes, mono_marshal_xdomain_copy_value ((MonoObject*)setup->configuration_bytes));
	MONO_OBJECT_SETREF (copy, serialized_non_primitives, mono_marshal_xdomain_copy_value ((MonoObject*)setup->serialized_non_primitives));

	mono_domain_set_internal (caller_domain);

	return copy;
}

static MonoAppDomain *
mono_domain_create_appdomain_internal (char *friendly_name, MonoAppDomainSetup *setup)
{
	MonoError error;
	MonoClass *adclass;
	MonoAppDomain *ad;
	MonoDomain *data;
	char *shadow_location;
	
	MONO_ARCH_SAVE_REGS;

	adclass = mono_class_from_name (mono_defaults.corlib, "System", "AppDomain");

	/* FIXME: pin all those objects */
	data = mono_domain_create();

	ad = (MonoAppDomain *) mono_object_new (data, adclass);
	ad->data = data;
	data->domain = ad;
	data->friendly_name = g_strdup (friendly_name);

	if (!setup->application_base) {
		/* Inherit from the root domain since MS.NET does this */
		MonoDomain *root = mono_get_root_domain ();
		if (root->setup->application_base)
			MONO_OBJECT_SETREF (setup, application_base, mono_string_new_utf16 (data, mono_string_chars (root->setup->application_base), mono_string_length (root->setup->application_base)));
	}

	mono_context_init (data);

	data->setup = copy_app_domain_setup (data, setup);
	mono_set_private_bin_path_from_config (data);
	add_assemblies_to_domain (data, mono_defaults.corlib->assembly, NULL);

#ifndef DISABLE_SHADOW_COPY
	/*FIXME, guard this for when the debugger is not running */
	shadow_location = get_shadow_assembly_location_base (data, &error);
	if (!mono_error_ok (&error))
		mono_error_raise_exception (&error);
	mono_debugger_event_create_appdomain (data, shadow_location);
	g_free (shadow_location);
#endif

	create_domain_objects (data);

	return ad;
}

/**
 * mono_domain_has_type_resolve:
 * @domain: application domains being looked up
 *
 * Returns true if the AppDomain.TypeResolve field has been
 * set.
 */
gboolean
mono_domain_has_type_resolve (MonoDomain *domain)
{
	static MonoClassField *field = NULL;
	MonoObject *o;

	if (field == NULL) {
		field = mono_class_get_field_from_name (mono_defaults.appdomain_class, "TypeResolve");
		g_assert (field);
	}

	/*pedump doesn't create an appdomin, so the domain object doesn't exist.*/
	if (!domain->domain)
		return FALSE;

	mono_field_get_value ((MonoObject*)(domain->domain), field, &o);
	return o != NULL;
}

/**
 * mono_domain_try_type_resolve:
 * @domain: application domainwhere the name where the type is going to be resolved
 * @name: the name of the type to resolve or NULL.
 * @tb: A System.Reflection.Emit.TypeBuilder, used if name is NULL.
 *
 * This routine invokes the internal System.AppDomain.DoTypeResolve and returns
 * the assembly that matches name.
 *
 * If @name is null, the value of ((TypeBuilder)tb).FullName is used instead
 *
 * Returns: A MonoReflectionAssembly or NULL if not found
 */
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

		method = mono_class_get_method_from_name (klass, "DoTypeResolve", -1);
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

/**
 * mono_domain_owns_vtable_slot:
 *
 *  Returns whenever VTABLE_SLOT is inside a vtable which belongs to DOMAIN.
 */
gboolean
mono_domain_owns_vtable_slot (MonoDomain *domain, gpointer vtable_slot)
{
	gboolean res;

	mono_domain_lock (domain);
	res = mono_mempool_contains_addr (domain->mp, vtable_slot);
	mono_domain_unlock (domain);
	return res;
}

/**
 * mono_domain_set:
 * @domain: domain
 * @force: force setting.
 *
 * Set the current appdomain to @domain. If @force is set, set it even
 * if it is being unloaded.
 *
 * Returns:
 *   TRUE on success;
 *   FALSE if the domain is unloaded
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
	MonoDomain *add;
	MonoObject *o;
	char *str;

	MONO_ARCH_SAVE_REGS;

	g_assert (ad != NULL);
	add = ad->data;
	g_assert (add != NULL);

	if (name == NULL)
		mono_raise_exception (mono_get_exception_argument_null ("name"));

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
	MonoDomain *add;

	MONO_ARCH_SAVE_REGS;

	g_assert (ad != NULL);
	add = ad->data;
	g_assert (add != NULL);

	if (name == NULL)
		mono_raise_exception (mono_get_exception_argument_null ("name"));

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
ves_icall_System_AppDomain_getRootDomain ()
{
	MonoDomain *root = mono_get_root_domain ();

	MONO_ARCH_SAVE_REGS;

	return root->domain;
}

static char*
get_attribute_value (const gchar **attribute_names, 
		     const gchar **attribute_values, 
		     const char *att_name)
{
	int n;
	for (n = 0; attribute_names [n] != NULL; n++) {
		if (strcmp (attribute_names [n], att_name) == 0)
			return g_strdup (attribute_values [n]);
	}
	return NULL;
}

static void
start_element (GMarkupParseContext *context, 
	       const gchar         *element_name,
	       const gchar        **attribute_names,
	       const gchar        **attribute_values,
	       gpointer             user_data,
	       GError             **error)
{
	RuntimeConfig *runtime_config = user_data;
	
	if (strcmp (element_name, "runtime") == 0) {
		runtime_config->runtime_count++;
		return;
	}

	if (strcmp (element_name, "assemblyBinding") == 0) {
		runtime_config->assemblybinding_count++;
		return;
	}
	
	if (runtime_config->runtime_count != 1 || runtime_config->assemblybinding_count != 1)
		return;

	if (strcmp (element_name, "probing") != 0)
		return;

	g_free (runtime_config->domain->private_bin_path);
	runtime_config->domain->private_bin_path = get_attribute_value (attribute_names, attribute_values, "privatePath");
	if (runtime_config->domain->private_bin_path && !runtime_config->domain->private_bin_path [0]) {
		g_free (runtime_config->domain->private_bin_path);
		runtime_config->domain->private_bin_path = NULL;
		return;
	}
}

static void
end_element (GMarkupParseContext *context,
	     const gchar         *element_name,
	     gpointer             user_data,
	     GError             **error)
{
	RuntimeConfig *runtime_config = user_data;
	if (strcmp (element_name, "runtime") == 0)
		runtime_config->runtime_count--;
	else if (strcmp (element_name, "assemblyBinding") == 0)
		runtime_config->assemblybinding_count--;
}

static void
parse_error   (GMarkupParseContext *context, GError *error, gpointer user_data)
{
	RuntimeConfig *state = user_data;
	const gchar *msg;
	const gchar *filename;

	filename = state && state->filename ? (gchar *) state->filename : "<unknown>";
	msg = error && error->message ? error->message : "";
	g_warning ("Error parsing %s: %s", filename, msg);
}

static const GMarkupParser
mono_parser = {
	start_element,
	end_element,
	NULL,
	NULL,
	parse_error
};

void
mono_set_private_bin_path_from_config (MonoDomain *domain)
{
	MonoError error;
	gchar *config_file_name = NULL, *text = NULL, *config_file_path = NULL;
	gsize len;
	GMarkupParseContext *context;
	RuntimeConfig runtime_config;
	gint offset;
	
	if (!domain || !domain->setup || !domain->setup->configuration_file)
		return;

	config_file_name = mono_string_to_utf8_checked (domain->setup->configuration_file, &error);
	if (!mono_error_ok (&error)) {
		mono_error_cleanup (&error);
		goto free_and_out;
	}

	config_file_path = mono_portability_find_file (config_file_name, TRUE);
	if (!config_file_path)
		config_file_path = config_file_name;

	if (!g_file_get_contents (config_file_path, &text, &len, NULL))
		goto free_and_out;

	runtime_config.runtime_count = 0;
	runtime_config.assemblybinding_count = 0;
	runtime_config.domain = domain;
	runtime_config.filename = config_file_path;
	
	offset = 0;
	if (len > 3 && text [0] == '\xef' && text [1] == (gchar) '\xbb' && text [2] == '\xbf')
		offset = 3; /* Skip UTF-8 BOM */

	context = g_markup_parse_context_new (&mono_parser, 0, &runtime_config, NULL);
	if (g_markup_parse_context_parse (context, text + offset, len - offset, NULL))
		g_markup_parse_context_end_parse (context, NULL);
	g_markup_parse_context_free (context);

  free_and_out:
	g_free (text);
	if (config_file_name != config_file_path)
		g_free (config_file_name);
	g_free (config_file_path);
}

MonoAppDomain *
ves_icall_System_AppDomain_createDomain (MonoString *friendly_name, MonoAppDomainSetup *setup)
{
	char *fname = mono_string_to_utf8 (friendly_name);
	MonoAppDomain *ad = mono_domain_create_appdomain_internal (fname, setup);
	
	g_free (fname);

	return ad;
}

MonoArray *
ves_icall_System_AppDomain_GetAssemblies (MonoAppDomain *ad, MonoBoolean refonly)
{
	MonoDomain *domain = ad->data; 
	MonoAssembly* ass;
	static MonoClass *System_Reflection_Assembly;
	MonoArray *res;
	GSList *tmp;
	int i;
	GPtrArray *assemblies;

	MONO_ARCH_SAVE_REGS;

	if (!System_Reflection_Assembly)
		System_Reflection_Assembly = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "Assembly");

	/* 
	 * Make a copy of the list of assemblies because we can't hold the assemblies
	 * lock while creating objects etc.
	 */
	assemblies = g_ptr_array_new ();
	/* Need to skip internal assembly builders created by remoting */
	mono_domain_assemblies_lock (domain);
	for (tmp = domain->domain_assemblies; tmp; tmp = tmp->next) {
		ass = tmp->data;
		if (refonly != ass->ref_only)
			continue;
		if (ass->corlib_internal)
			continue;
		g_ptr_array_add (assemblies, ass);
	}
	mono_domain_assemblies_unlock (domain);

	res = mono_array_new (domain, System_Reflection_Assembly, assemblies->len);
	for (i = 0; i < assemblies->len; ++i) {
		ass = g_ptr_array_index (assemblies, i);
		mono_array_setref (res, i, mono_assembly_get_object (domain, ass));
	}

	g_ptr_array_free (assemblies, TRUE);

	return res;
}

MonoReflectionAssembly *
mono_try_assembly_resolve (MonoDomain *domain, MonoString *fname, gboolean refonly)
{
	MonoClass *klass;
	MonoMethod *method;
	MonoBoolean isrefonly;
	gpointer params [2];

	if (mono_runtime_get_no_exec ())
		return NULL;

	g_assert (domain != NULL && fname != NULL);

	klass = domain->domain->mbr.obj.vtable->klass;
	g_assert (klass);
	
	method = mono_class_get_method_from_name (klass, "DoAssemblyResolve", -1);
	if (method == NULL) {
		g_warning ("Method AppDomain.DoAssemblyResolve not found.\n");
		return NULL;
	}

	isrefonly = refonly ? 1 : 0;
	params [0] = fname;
	params [1] = &isrefonly;
	return (MonoReflectionAssembly *) mono_runtime_invoke (method, domain->domain, params, NULL);
}

static MonoAssembly *
mono_domain_assembly_postload_search (MonoAssemblyName *aname,
									  gpointer user_data)
{
	gboolean refonly = GPOINTER_TO_UINT (user_data);
	MonoReflectionAssembly *assembly;
	MonoDomain *domain = mono_domain_get ();
	char *aname_str;
	MonoString *str;

	aname_str = mono_stringify_assembly_name (aname);

	/* FIXME: We invoke managed code here, so there is a potential for deadlocks */
	str = mono_string_new (domain, aname_str);
	if (!str) {
		g_free (aname_str);
		return NULL;
	}
	assembly = mono_try_assembly_resolve (domain, str, refonly);
	g_free (aname_str);

	if (assembly)
		return assembly->assembly;
	else
		return NULL;
}
	
/*
 * LOCKING: assumes assemblies_lock in the domain is already locked.
 */
static void
add_assemblies_to_domain (MonoDomain *domain, MonoAssembly *ass, GHashTable *ht)
{
	gint i;
	GSList *tmp;
	gboolean destroy_ht = FALSE;

	if (!ass->aname.name)
		return;

	if (!ht) {
		ht = g_hash_table_new (mono_aligned_addr_hash, NULL);
		destroy_ht = TRUE;
	}

	/* FIXME: handle lazy loaded assemblies */
	for (tmp = domain->domain_assemblies; tmp; tmp = tmp->next) {
		g_hash_table_insert (ht, tmp->data, tmp->data);
	}
	if (!g_hash_table_lookup (ht, ass)) {
		mono_assembly_addref (ass);
		g_hash_table_insert (ht, ass, ass);
		domain->domain_assemblies = g_slist_prepend (domain->domain_assemblies, ass);
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Assembly %s[%p] added to domain %s, ref_count=%d", ass->aname.name, ass, domain->friendly_name, ass->ref_count);
	}

	if (ass->image->references) {
		for (i = 0; ass->image->references [i] != NULL; i++) {
			if (ass->image->references [i] != REFERENCE_MISSING)
				if (!g_hash_table_lookup (ht, ass->image->references [i])) {
					add_assemblies_to_domain (domain, ass->image->references [i], ht);
				}
		}
	}
	if (destroy_ht)
		g_hash_table_destroy (ht);
}

static void
mono_domain_fire_assembly_load (MonoAssembly *assembly, gpointer user_data)
{
	static MonoClassField *assembly_load_field;
	static MonoMethod *assembly_load_method;
	MonoDomain *domain = mono_domain_get ();
	MonoReflectionAssembly *ref_assembly;
	MonoClass *klass;
	gpointer load_value;
	void *params [1];

	if (!domain->domain)
		/* This can happen during startup */
		return;
#ifdef ASSEMBLY_LOAD_DEBUG
	fprintf (stderr, "Loading %s into domain %s\n", assembly->aname.name, domain->friendly_name);
#endif
	klass = domain->domain->mbr.obj.vtable->klass;

	mono_domain_assemblies_lock (domain);
	add_assemblies_to_domain (domain, assembly, NULL);
	mono_domain_assemblies_unlock (domain);

	if (assembly_load_field == NULL) {
		assembly_load_field = mono_class_get_field_from_name (klass, "AssemblyLoad");
		g_assert (assembly_load_field);
	}

	mono_field_get_value ((MonoObject*) domain->domain, assembly_load_field, &load_value);
	if (load_value == NULL) {
		/* No events waiting to be triggered */
		return;
	}

	ref_assembly = mono_assembly_get_object (domain, assembly);
	g_assert (ref_assembly);

	if (assembly_load_method == NULL) {
		assembly_load_method = mono_class_get_method_from_name (klass, "DoAssemblyLoad", -1);
		g_assert (assembly_load_method);
	}

	*params = ref_assembly;
	mono_runtime_invoke (assembly_load_method, domain->domain, params, NULL);
}

/*
 * LOCKING: Acquires the domain assemblies lock.
 */
static void
set_domain_search_path (MonoDomain *domain)
{
	MonoError error;
	MonoAppDomainSetup *setup;
	gchar **tmp;
	gchar *search_path = NULL;
	gint i;
	gint npaths = 0;
	gchar **pvt_split = NULL;
	GError *gerror = NULL;
	gint appbaselen = -1;

	/* 
	 * We use the low-level domain assemblies lock, since this is called from
	 * assembly loads hooks, which means this thread might hold the loader lock.
	 */
	mono_domain_assemblies_lock (domain);

	if (!domain->setup) {
		mono_domain_assemblies_unlock (domain);
		return;
	}

	if ((domain->search_path != NULL) && !domain->setup->path_changed) {
		mono_domain_assemblies_unlock (domain);
		return;
	}
	setup = domain->setup;
	if (!setup->application_base) {
		mono_domain_assemblies_unlock (domain);
		return; /* Must set application base to get private path working */
	}

	npaths++;
	
	if (setup->private_bin_path) {
		search_path = mono_string_to_utf8_checked (setup->private_bin_path, &error);
		if (!mono_error_ok (&error)) { /*FIXME maybe we should bubble up the error.*/
			g_warning ("Could not decode AppDomain search path since it contains invalid characters");
			mono_error_cleanup (&error);
			mono_domain_assemblies_unlock (domain);
			return;
		}
	}
	
	if (domain->private_bin_path) {
		if (search_path == NULL)
			search_path = domain->private_bin_path;
		else {
			gchar *tmp2 = search_path;
			search_path = g_strjoin (";", search_path, domain->private_bin_path, NULL);
			g_free (tmp2);
		}
	}
	
	if (search_path) {
		/*
		 * As per MSDN documentation, AppDomainSetup.PrivateBinPath contains a list of
		 * directories relative to ApplicationBase separated by semicolons (see
		 * http://msdn2.microsoft.com/en-us/library/system.appdomainsetup.privatebinpath.aspx)
		 * The loop below copes with the fact that some Unix applications may use ':' (or
		 * System.IO.Path.PathSeparator) as the path search separator. We replace it with
		 * ';' for the subsequent split.
		 *
		 * The issue was reported in bug #81446
		 */

#ifndef TARGET_WIN32
		gint slen;

		slen = strlen (search_path);
		for (i = 0; i < slen; i++)
			if (search_path [i] == ':')
				search_path [i] = ';';
#endif
		
		pvt_split = g_strsplit (search_path, ";", 1000);
		g_free (search_path);
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
		mono_domain_assemblies_unlock (domain);
		return;
	}

	if (domain->search_path)
		g_strfreev (domain->search_path);

	tmp = g_malloc ((npaths + 1) * sizeof (gchar *));
	tmp [npaths] = NULL;

	*tmp = mono_string_to_utf8_checked (setup->application_base, &error);
	if (!mono_error_ok (&error)) {
		mono_error_cleanup (&error);
		g_strfreev (pvt_split);
		g_free (tmp);

		mono_domain_assemblies_unlock (domain);
		return;
	}

	domain->search_path = tmp;

	/* FIXME: is this needed? */
	if (strncmp (*tmp, "file://", 7) == 0) {
		gchar *file = *tmp;
		gchar *uri = *tmp;
		gchar *tmpuri;

		if (uri [7] != '/')
			uri = g_strdup_printf ("file:///%s", uri + 7);

		tmpuri = uri;
		uri = mono_escape_uri_string (tmpuri);
		*tmp = g_filename_from_uri (uri, NULL, &gerror);
		g_free (uri);

		if (tmpuri != file)
			g_free (tmpuri);

		if (gerror != NULL) {
			g_warning ("%s\n", gerror->message);
			g_error_free (gerror);
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

			reduced = mono_path_canonicalize (tmp [i]);
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

	mono_domain_assemblies_unlock (domain);
}

#ifdef DISABLE_SHADOW_COPY
gboolean
mono_is_shadow_copy_enabled (MonoDomain *domain, const gchar *dir_name)
{
	return FALSE;
}

char *
mono_make_shadow_copy (const char *filename)
{
	return (char *) filename;
}
#else
static gboolean
shadow_copy_sibling (gchar *src, gint srclen, const char *extension, gchar *target, gint targetlen, gint tail_len)
{
	guint16 *orig, *dest;
	gboolean copy_result;
	
	strcpy (src + srclen - tail_len, extension);

	if (IS_PORTABILITY_CASE) {
		gchar *file = mono_portability_find_file (src, TRUE);

		if (file == NULL)
			return TRUE;

		g_free (file);
	} else if (!g_file_test (src, G_FILE_TEST_IS_REGULAR)) {
		return TRUE;
	}

	orig = g_utf8_to_utf16 (src, strlen (src), NULL, NULL, NULL);

	strcpy (target + targetlen - tail_len, extension);
	dest = g_utf8_to_utf16 (target, strlen (target), NULL, NULL, NULL);
	
	DeleteFile (dest);
	copy_result = CopyFile (orig, dest, FALSE);

	/* Fix for bug #556884 - make sure the files have the correct mode so that they can be
	 * overwritten when updated in their original locations. */
	if (copy_result)
		copy_result = SetFileAttributes (dest, FILE_ATTRIBUTE_NORMAL);

	g_free (orig);
	g_free (dest);
	
	return copy_result;
}

static gint32 
get_cstring_hash (const char *str)
{
	int len, i;
	const char *p;
	gint32 h = 0;
	
	if (!str || !str [0])
		return 0;
		
	len = strlen (str);
	p = str;
	for (i = 0; i < len; i++) {
		h = (h << 5) - h + *p;
		p++;
	}
	
	return h;
}

/*
 * Returned memory is malloc'd. Called must free it 
 */
static char *
get_shadow_assembly_location_base (MonoDomain *domain, MonoError *error)
{
	MonoAppDomainSetup *setup;
	char *cache_path, *appname;
	char *userdir;
	char *location;

	mono_error_init (error);
	
	setup = domain->setup;
	if (setup->cache_path != NULL && setup->application_name != NULL) {
		cache_path = mono_string_to_utf8_checked (setup->cache_path, error);
		if (!mono_error_ok (error))
			return NULL;
#ifndef TARGET_WIN32
		{
			gint i;
			for (i = strlen (cache_path) - 1; i >= 0; i--)
				if (cache_path [i] == '\\')
					cache_path [i] = '/';
		}
#endif

		appname = mono_string_to_utf8_checked (setup->application_name, error);
		if (!mono_error_ok (error)) {
			g_free (cache_path);
			return NULL;
		}

		location = g_build_filename (cache_path, appname, "assembly", "shadow", NULL);
		g_free (appname);
		g_free (cache_path);
	} else {
		userdir = g_strdup_printf ("%s-mono-cachepath", g_get_user_name ());
		location = g_build_filename (g_get_tmp_dir (), userdir, "assembly", "shadow", NULL);
		g_free (userdir);
	}
	return location;
}

static char *
get_shadow_assembly_location (const char *filename, MonoError *error)
{
	gint32 hash = 0, hash2 = 0;
	char name_hash [9];
	char path_hash [30];
	char *bname = g_path_get_basename (filename);
	char *dirname = g_path_get_dirname (filename);
	char *location, *tmploc;
	MonoDomain *domain = mono_domain_get ();

	mono_error_init (error);
	
	hash = get_cstring_hash (bname);
	hash2 = get_cstring_hash (dirname);
	g_snprintf (name_hash, sizeof (name_hash), "%08x", hash);
	g_snprintf (path_hash, sizeof (path_hash), "%08x_%08x_%08x", hash ^ hash2, hash2, domain->shadow_serial);
	tmploc = get_shadow_assembly_location_base (domain, error);
	if (!mono_error_ok (error)) {
		g_free (bname);
		g_free (dirname);
		return NULL;
	}

	location = g_build_filename (tmploc, name_hash, path_hash, bname, NULL);
	g_free (tmploc);
	g_free (bname);
	g_free (dirname);
	return location;
}

static gboolean
ensure_directory_exists (const char *filename)
{
#ifdef HOST_WIN32
	gchar *dir_utf8 = g_path_get_dirname (filename);
	gunichar2 *p;
	gunichar2 *dir_utf16 = NULL;
	int retval;
	
	if (!dir_utf8 || !dir_utf8 [0])
		return FALSE;

	dir_utf16 = g_utf8_to_utf16 (dir_utf8, strlen (dir_utf8), NULL, NULL, NULL);
	g_free (dir_utf8);

	if (!dir_utf16)
		return FALSE;

	p = dir_utf16;

	/* make life easy and only use one directory seperator */
	while (*p != '\0')
	{
		if (*p == '/')
			*p = '\\';
		p++;
	}

	p = dir_utf16;

	/* get past C:\ )*/
	while (*p++ != '\\')	
	{
	}

	while (1) {
		BOOL bRet = FALSE;
		p = wcschr (p, '\\');
		if (p)
			*p = '\0';
		retval = _wmkdir (dir_utf16);
		if (retval != 0 && errno != EEXIST) {
			g_free (dir_utf16);
			return FALSE;
		}
		if (!p)
			break;
		*p++ = '\\';
	}
	
	g_free (dir_utf16);
	return TRUE;
#else
	char *p;
	gchar *dir = g_path_get_dirname (filename);
	int retval;
	struct stat sbuf;
	
	if (!dir || !dir [0]) {
		g_free (dir);
		return FALSE;
	}
	
	if (stat (dir, &sbuf) == 0 && S_ISDIR (sbuf.st_mode)) {
		g_free (dir);
		return TRUE;
	}
	
	p = dir;
	while (*p == '/')
		p++;

	while (1) {
		p = strchr (p, '/');
		if (p)
			*p = '\0';
		retval = mkdir (dir, 0777);
		if (retval != 0 && errno != EEXIST) {
			g_free (dir);
			return FALSE;
		}
		if (!p)
			break;
		*p++ = '/';
	}
	
	g_free (dir);
	return TRUE;
#endif
}

static gboolean
private_file_needs_copying (const char *src, struct stat *sbuf_src, char *dest)
{
	struct stat sbuf_dest;
	gchar *real_src = mono_portability_find_file (src, TRUE);

	if (!real_src)
		real_src = (gchar*)src;
	
	if (stat (real_src, sbuf_src) == -1) {
		time_t tnow = time (NULL);
		memset (sbuf_src, 0, sizeof (*sbuf_src));
		sbuf_src->st_mtime = tnow;
		sbuf_src->st_atime = tnow;
		return TRUE;
	}

	if (stat (dest, &sbuf_dest) == -1)
		return TRUE;
	
	if (sbuf_src->st_size == sbuf_dest.st_size &&
	    sbuf_src->st_mtime == sbuf_dest.st_mtime)
		return FALSE;

	return TRUE;
}

static gboolean
shadow_copy_create_ini (const char *shadow, const char *filename)
{
	char *dir_name;
	char *ini_file;
	guint16 *u16_ini;
	gboolean result;
	guint32 n;
	HANDLE *handle;
	gchar *full_path;

	dir_name = g_path_get_dirname (shadow);
	ini_file = g_build_filename (dir_name, "__AssemblyInfo__.ini", NULL);
	g_free (dir_name);
	if (g_file_test (ini_file, G_FILE_TEST_IS_REGULAR)) {
		g_free (ini_file);
		return TRUE;
	}

	u16_ini = g_utf8_to_utf16 (ini_file, strlen (ini_file), NULL, NULL, NULL);
	g_free (ini_file);
	if (!u16_ini) {
		return FALSE;
	}
	handle = CreateFile (u16_ini, GENERIC_WRITE, FILE_SHARE_READ|FILE_SHARE_WRITE,
				NULL, CREATE_NEW, FileAttributes_Normal, NULL);
	g_free (u16_ini);
	if (handle == INVALID_HANDLE_VALUE) {
		return FALSE;
	}

	full_path = mono_path_resolve_symlinks (filename);
	result = WriteFile (handle, full_path, strlen (full_path), &n, NULL);
	g_free (full_path);
	CloseHandle (handle);
	return result;
}

gboolean
mono_is_shadow_copy_enabled (MonoDomain *domain, const gchar *dir_name)
{
	MonoError error;
	MonoAppDomainSetup *setup;
	gchar *all_dirs;
	gchar **dir_ptr;
	gchar **directories;
	gchar *shadow_status_string;
	gchar *base_dir;
	gboolean shadow_enabled;
	gboolean found = FALSE;

	if (domain == NULL)
		return FALSE;

	setup = domain->setup;
	if (setup == NULL || setup->shadow_copy_files == NULL)
		return FALSE;

	shadow_status_string = mono_string_to_utf8_checked (setup->shadow_copy_files, &error);
	if (!mono_error_ok (&error)) {
		mono_error_cleanup (&error);
		return FALSE;
	}
	shadow_enabled = !g_ascii_strncasecmp (shadow_status_string, "true", 4);
	g_free (shadow_status_string);

	if (!shadow_enabled)
		return FALSE;

	if (setup->shadow_copy_directories == NULL)
		return TRUE;

	/* Is dir_name a shadow_copy destination already? */
	base_dir = get_shadow_assembly_location_base (domain, &error);
	if (!mono_error_ok (&error)) {
		mono_error_cleanup (&error);
		return FALSE;
	}

	if (strstr (dir_name, base_dir)) {
		g_free (base_dir);
		return TRUE;
	}
	g_free (base_dir);

	all_dirs = mono_string_to_utf8_checked (setup->shadow_copy_directories, &error);
	if (!mono_error_ok (&error)) {
		mono_error_cleanup (&error);
		return FALSE;
	}

	directories = g_strsplit (all_dirs, G_SEARCHPATH_SEPARATOR_S, 1000);
	dir_ptr = directories;
	while (*dir_ptr) {
		if (**dir_ptr != '\0' && !strcmp (*dir_ptr, dir_name)) {
			found = TRUE;
			break;
		}
		dir_ptr++;
	}
	g_strfreev (directories);
	g_free (all_dirs);
	return found;
}

/*
This function raises exceptions so it can cause as sorts of nasty stuff if called
while holding a lock.
FIXME bubble up the error instead of raising it here
*/
char *
mono_make_shadow_copy (const char *filename)
{
	MonoError error;
	gchar *sibling_source, *sibling_target;
	gint sibling_source_len, sibling_target_len;
	guint16 *orig, *dest;
	char *shadow;
	gboolean copy_result;
	MonoException *exc;
	struct stat src_sbuf;
	struct utimbuf utbuf;
	char *dir_name = g_path_get_dirname (filename);
	MonoDomain *domain = mono_domain_get ();
	char *shadow_dir;

	set_domain_search_path (domain);

	if (!mono_is_shadow_copy_enabled (domain, dir_name)) {
		g_free (dir_name);
		return (char *) filename;
	}

	/* Is dir_name a shadow_copy destination already? */
	shadow_dir = get_shadow_assembly_location_base (domain, &error);
	if (!mono_error_ok (&error)) {
		mono_error_cleanup (&error);
		g_free (dir_name);
		exc = mono_get_exception_execution_engine ("Failed to create shadow copy (invalid characters in shadow directory name).");
		mono_raise_exception (exc);
	}

	if (strstr (dir_name, shadow_dir)) {
		g_free (shadow_dir);
		g_free (dir_name);
		return (char *) filename;
	}
	g_free (shadow_dir);
	g_free (dir_name);

	shadow = get_shadow_assembly_location (filename, &error);
	if (!mono_error_ok (&error)) {
		mono_error_cleanup (&error);
		exc = mono_get_exception_execution_engine ("Failed to create shadow copy (invalid characters in file name).");
		mono_raise_exception (exc);
	}

	if (ensure_directory_exists (shadow) == FALSE) {
		g_free (shadow);
		exc = mono_get_exception_execution_engine ("Failed to create shadow copy (ensure directory exists).");
		mono_raise_exception (exc);
	}	

	if (!private_file_needs_copying (filename, &src_sbuf, shadow))
		return (char*) shadow;

	orig = g_utf8_to_utf16 (filename, strlen (filename), NULL, NULL, NULL);
	dest = g_utf8_to_utf16 (shadow, strlen (shadow), NULL, NULL, NULL);
	DeleteFile (dest);
	copy_result = CopyFile (orig, dest, FALSE);

	/* Fix for bug #556884 - make sure the files have the correct mode so that they can be
	 * overwritten when updated in their original locations. */
	if (copy_result)
		copy_result = SetFileAttributes (dest, FILE_ATTRIBUTE_NORMAL);

	g_free (dest);
	g_free (orig);

	if (copy_result == FALSE) {
		g_free (shadow);
		exc = mono_get_exception_execution_engine ("Failed to create shadow copy (CopyFile).");
		mono_raise_exception (exc);
	}

	/* attempt to copy .mdb, .config if they exist */
	sibling_source = g_strconcat (filename, ".config", NULL);
	sibling_source_len = strlen (sibling_source);
	sibling_target = g_strconcat (shadow, ".config", NULL);
	sibling_target_len = strlen (sibling_target);
	
	copy_result = shadow_copy_sibling (sibling_source, sibling_source_len, ".mdb", sibling_target, sibling_target_len, 7);
	if (copy_result == TRUE)
		copy_result = shadow_copy_sibling (sibling_source, sibling_source_len, ".config", sibling_target, sibling_target_len, 7);
	
	g_free (sibling_source);
	g_free (sibling_target);
	
	if (copy_result == FALSE)  {
		g_free (shadow);
		exc = mono_get_exception_execution_engine ("Failed to create shadow copy of sibling data (CopyFile).");
		mono_raise_exception (exc);
	}

	/* Create a .ini file containing the original assembly location */
	if (!shadow_copy_create_ini (shadow, filename)) {
		g_free (shadow);
		exc = mono_get_exception_execution_engine ("Failed to create shadow copy .ini file.");
		mono_raise_exception (exc);
	}

	utbuf.actime = src_sbuf.st_atime;
	utbuf.modtime = src_sbuf.st_mtime;
	utime (shadow, &utbuf);
	
	return shadow;
}
#endif /* DISABLE_SHADOW_COPY */

MonoDomain *
mono_domain_from_appdomain (MonoAppDomain *appdomain)
{
	if (appdomain == NULL)
		return NULL;
	
	return appdomain->data;
}

static gboolean
try_load_from (MonoAssembly **assembly, const gchar *path1, const gchar *path2,
					const gchar *path3, const gchar *path4,
					gboolean refonly, gboolean is_private)
{
	gchar *fullpath;
	gboolean found = FALSE;
	
	*assembly = NULL;
	fullpath = g_build_filename (path1, path2, path3, path4, NULL);

	if (IS_PORTABILITY_SET) {
		gchar *new_fullpath = mono_portability_find_file (fullpath, TRUE);
		if (new_fullpath) {
			g_free (fullpath);
			fullpath = new_fullpath;
			found = TRUE;
		}
	} else
		found = g_file_test (fullpath, G_FILE_TEST_IS_REGULAR);
	
	if (found)
		*assembly = mono_assembly_open_full (fullpath, NULL, refonly);

	g_free (fullpath);
	return (*assembly != NULL);
}

static MonoAssembly *
real_load (gchar **search_path, const gchar *culture, const gchar *name, gboolean refonly)
{
	MonoAssembly *result = NULL;
	gchar **path;
	gchar *filename;
	const gchar *local_culture;
	gint len;
	gboolean is_private = FALSE;

	if (!culture || *culture == '\0') {
		local_culture = "";
	} else {
		local_culture = culture;
	}

	filename =  g_strconcat (name, ".dll", NULL);
	len = strlen (filename);

	for (path = search_path; *path; path++) {
		if (**path == '\0') {
			is_private = TRUE;
			continue; /* Ignore empty ApplicationBase */
		}

		/* See test cases in bug #58992 and bug #57710 */
		/* 1st try: [culture]/[name].dll (culture may be empty) */
		strcpy (filename + len - 4, ".dll");
		if (try_load_from (&result, *path, local_culture, "", filename, refonly, is_private))
			break;

		/* 2nd try: [culture]/[name].exe (culture may be empty) */
		strcpy (filename + len - 4, ".exe");
		if (try_load_from (&result, *path, local_culture, "", filename, refonly, is_private))
			break;

		/* 3rd try: [culture]/[name]/[name].dll (culture may be empty) */
		strcpy (filename + len - 4, ".dll");
		if (try_load_from (&result, *path, local_culture, name, filename, refonly, is_private))
			break;

		/* 4th try: [culture]/[name]/[name].exe (culture may be empty) */
		strcpy (filename + len - 4, ".exe");
		if (try_load_from (&result, *path, local_culture, name, filename, refonly, is_private))
			break;
	}

	g_free (filename);
	return result;
}

/*
 * Try loading the assembly from ApplicationBase and PrivateBinPath 
 * and then from assemblies_path if any.
 * LOCKING: This is called from the assembly loading code, which means the caller
 * might hold the loader lock. Thus, this function must not acquire the domain lock.
 */
static MonoAssembly *
mono_domain_assembly_preload (MonoAssemblyName *aname,
			      gchar **assemblies_path,
			      gpointer user_data)
{
	MonoDomain *domain = mono_domain_get ();
	MonoAssembly *result = NULL;
	gboolean refonly = GPOINTER_TO_UINT (user_data);

	set_domain_search_path (domain);

	if (domain->search_path && domain->search_path [0] != NULL) {
		result = real_load (domain->search_path, aname->culture, aname->name, refonly);
	}

	if (result == NULL && assemblies_path && assemblies_path [0] != NULL) {
		result = real_load (assemblies_path, aname->culture, aname->name, refonly);
	}

	return result;
}

/*
 * Check whenever a given assembly was already loaded in the current appdomain.
 */
static MonoAssembly *
mono_domain_assembly_search (MonoAssemblyName *aname,
							 gpointer user_data)
{
	MonoDomain *domain = mono_domain_get ();
	GSList *tmp;
	MonoAssembly *ass;
	gboolean refonly = GPOINTER_TO_UINT (user_data);

	mono_domain_assemblies_lock (domain);
	for (tmp = domain->domain_assemblies; tmp; tmp = tmp->next) {
		ass = tmp->data;
		/* Dynamic assemblies can't match here in MS.NET */
		if (ass->dynamic || refonly != ass->ref_only || !mono_assembly_names_equal (aname, &ass->aname))
			continue;

		mono_domain_assemblies_unlock (domain);
		return ass;
	}
	mono_domain_assemblies_unlock (domain);

	return NULL;
}

MonoReflectionAssembly *
ves_icall_System_Reflection_Assembly_LoadFrom (MonoString *fname, MonoBoolean refOnly)
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
	
	ass = mono_assembly_open_full (filename, &status, refOnly);
	
	if (!ass){
		MonoException *exc;

		if (status == MONO_IMAGE_IMAGE_INVALID)
			exc = mono_get_exception_bad_image_format2 (NULL, fname);
		else
			exc = mono_get_exception_file_not_found2 (NULL, fname);
		g_free (name);
		mono_raise_exception (exc);
	}

	g_free (name);

	return mono_assembly_get_object (domain, ass);
}

MonoReflectionAssembly *
ves_icall_System_AppDomain_LoadAssemblyRaw (MonoAppDomain *ad, 
					    MonoArray *raw_assembly,
					    MonoArray *raw_symbol_store, MonoObject *evidence,
					    MonoBoolean refonly)
{
	MonoAssembly *ass;
	MonoReflectionAssembly *refass = NULL;
	MonoDomain *domain = ad->data;
	MonoImageOpenStatus status;
	guint32 raw_assembly_len = mono_array_length (raw_assembly);
	MonoImage *image = mono_image_open_from_data_full (mono_array_addr (raw_assembly, gchar, 0), raw_assembly_len, TRUE, NULL, refonly);

	if (!image) {
		mono_raise_exception (mono_get_exception_bad_image_format (""));
		return NULL;
	}

	if (raw_symbol_store != NULL)
		mono_debug_open_image_from_memory (image, mono_array_addr (raw_symbol_store, guint8, 0), mono_array_length (raw_symbol_store));

	ass = mono_assembly_load_from_full (image, "", &status, refonly);


	if (!ass) {
		mono_image_close (image);
		mono_raise_exception (mono_get_exception_bad_image_format (""));
		return NULL; 
	}

	refass = mono_assembly_get_object (domain, ass);
	MONO_OBJECT_SETREF (refass, evidence, evidence);
	return refass;
}

MonoReflectionAssembly *
ves_icall_System_AppDomain_LoadAssembly (MonoAppDomain *ad,  MonoString *assRef, MonoObject *evidence, MonoBoolean refOnly)
{
	MonoDomain *domain = ad->data; 
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	MonoAssembly *ass;
	MonoAssemblyName aname;
	MonoReflectionAssembly *refass = NULL;
	gchar *name;
	gboolean parsed;

	MONO_ARCH_SAVE_REGS;

	g_assert (assRef != NULL);

	name = mono_string_to_utf8 (assRef);
	parsed = mono_assembly_name_parse (name, &aname);
	g_free (name);

	if (!parsed) {
		/* This is a parse error... */
		return NULL;
	}

	ass = mono_assembly_load_full_nosearch (&aname, NULL, &status, refOnly);
	mono_assembly_name_free (&aname);

	if (!ass) {
		/* MS.NET doesn't seem to call the assembly resolve handler for refonly assemblies */
		if (!refOnly)
			refass = mono_try_assembly_resolve (domain, assRef, refOnly);
		else
			refass = NULL;
		if (!refass) {
			return NULL;
		}
	}

	if (refass == NULL)
		refass = mono_assembly_get_object (domain, ass);

	MONO_OBJECT_SETREF (refass, evidence, evidence);
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
ves_icall_System_AppDomain_ExecuteAssembly (MonoAppDomain *ad, 
											MonoReflectionAssembly *refass, MonoArray *args)
{
	MonoImage *image;
	MonoMethod *method;

	MONO_ARCH_SAVE_REGS;

	g_assert (refass);
	image = refass->assembly->image;
	g_assert (image);

	method = mono_get_method (image, mono_image_get_entry_point (image), NULL);

	if (!method)
		g_error ("No entry point method found in %s", image->name);

	if (!args)
		args = (MonoArray *) mono_array_new (ad->data, mono_defaults.string_class, 0);

	return mono_runtime_exec_main (method, (MonoArray *)args, NULL);
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
clear_cached_vtable (MonoVTable *vtable)
{
	MonoClass *klass = vtable->klass;
	MonoDomain *domain = vtable->domain;
	MonoClassRuntimeInfo *runtime_info;
	void *data;

	runtime_info = klass->runtime_info;
	if (runtime_info && runtime_info->max_domain >= domain->domain_id)
		runtime_info->domain_vtables [domain->domain_id] = NULL;
	if (klass->has_static_refs && (data = mono_vtable_get_static_field_data (vtable)))
		mono_gc_free_fixed (data);
}

static G_GNUC_UNUSED void
zero_static_data (MonoVTable *vtable)
{
	MonoClass *klass = vtable->klass;
	void *data;

	if (klass->has_static_refs && (data = mono_vtable_get_static_field_data (vtable)))
		mono_gc_bzero (data, mono_class_data_size (klass));
}

typedef struct unload_data {
	MonoDomain *domain;
	char *failure_reason;
} unload_data;

static void
deregister_reflection_info_roots_nspace_table (gpointer key, gpointer value, gpointer image)
{
	guint32 index = GPOINTER_TO_UINT (value);
	MonoClass *class = mono_class_get (image, MONO_TOKEN_TYPE_DEF | index);

	g_assert (class);

	mono_class_free_ref_info (class);
}

static void
deregister_reflection_info_roots_name_space (gpointer key, gpointer value, gpointer user_data)
{
	g_hash_table_foreach (value, deregister_reflection_info_roots_nspace_table, user_data);
}

static void
deregister_reflection_info_roots_from_list (MonoImage *image)
{
	GSList *list = image->reflection_info_unregister_classes;

	while (list) {
		MonoClass *class = list->data;

		mono_class_free_ref_info (class);

		list = list->next;
	}

	g_slist_free (image->reflection_info_unregister_classes);
	image->reflection_info_unregister_classes = NULL;
}

static void
deregister_reflection_info_roots (MonoDomain *domain)
{
	GSList *list;

	mono_loader_lock ();
	mono_domain_assemblies_lock (domain);
	for (list = domain->domain_assemblies; list; list = list->next) {
		MonoAssembly *assembly = list->data;
		MonoImage *image = assembly->image;
		int i;
		/*No need to take the image lock here since dynamic images are appdomain bound and at this point the mutator is gone.*/
		if (image->dynamic && image->name_cache)
			g_hash_table_foreach (image->name_cache, deregister_reflection_info_roots_name_space, image);
		deregister_reflection_info_roots_from_list (image);
		for (i = 0; i < image->module_count; ++i) {
			MonoImage *module = image->modules [i];
			if (module) {
				if (module->dynamic && module->name_cache) {
					g_hash_table_foreach (module->name_cache,
							deregister_reflection_info_roots_name_space, module);
				}
				deregister_reflection_info_roots_from_list (module);
			}
		}
	}
	mono_domain_assemblies_unlock (domain);
	mono_loader_unlock ();
}

static guint32 WINAPI
unload_thread_main (void *arg)
{
	unload_data *data = (unload_data*)arg;
	MonoDomain *domain = data->domain;
	MonoThread *thread;
	int i;

	/* Have to attach to the runtime so shutdown can wait for this thread */
	thread = mono_thread_attach (mono_get_root_domain ());

	/* 
	 * FIXME: Abort our parent thread last, so we can return a failure 
	 * indication if aborting times out.
	 */
	if (!mono_threads_abort_appdomain_threads (domain, -1)) {
		data->failure_reason = g_strdup_printf ("Aborting of threads in domain %s timed out.", domain->friendly_name);
		return 1;
	}

	if (!mono_thread_pool_remove_domain_jobs (domain, -1)) {
		data->failure_reason = g_strdup_printf ("Cleanup of threadpool jobs of domain %s timed out.", domain->friendly_name);
		return 1;
	}

	/* Finalize all finalizable objects in the doomed appdomain */
	if (!mono_domain_finalize (domain, -1)) {
		data->failure_reason = g_strdup_printf ("Finalization of domain %s timed out.", domain->friendly_name);
		return 1;
	}

	/* Clear references to our vtables in class->runtime_info.
	 * We also hold the loader lock because we're going to change
	 * class->runtime_info.
	 */

	mono_loader_lock ();
	mono_domain_lock (domain);
#ifdef HAVE_SGEN_GC
	/*
	 * We need to make sure that we don't have any remsets
	 * pointing into static data of the to-be-freed domain because
	 * at the next collections they would be invalid.  So what we
	 * do is we first zero all static data and then do a minor
	 * collection.  Because all references in the static data will
	 * now be null we won't do any unnecessary copies and after
	 * the collection there won't be any more remsets.
	 */
	for (i = 0; i < domain->class_vtable_array->len; ++i)
		zero_static_data (g_ptr_array_index (domain->class_vtable_array, i));
	mono_gc_collect (0);
#endif
	for (i = 0; i < domain->class_vtable_array->len; ++i)
		clear_cached_vtable (g_ptr_array_index (domain->class_vtable_array, i));
	deregister_reflection_info_roots (domain);

	mono_assembly_cleanup_domain_bindings (domain->domain_id);

	mono_domain_unlock (domain);
	mono_loader_unlock ();

	mono_threads_clear_cached_culture (domain);

	domain->state = MONO_APPDOMAIN_UNLOADED;

	/* printf ("UNLOADED %s.\n", domain->friendly_name); */

	/* remove from the handle table the items related to this domain */
	mono_gchandle_free_domain (domain);

	mono_domain_free (domain, FALSE);

	mono_gc_collect (mono_gc_max_generation ());

	mono_thread_detach (thread);

	return 0;
}

/*
 * mono_domain_unload:
 * @domain: The domain to unload
 *
 *  Unloads an appdomain. Follows the process outlined in the comment
 *  for mono_domain_try_unload.
 */
void
mono_domain_unload (MonoDomain *domain)
{
	MonoObject *exc = NULL;
	mono_domain_try_unload (domain, &exc);
	if (exc)
		mono_raise_exception ((MonoException*)exc);
}

/*
 * mono_domain_unload:
 * @domain: The domain to unload
 * @exc: Exception information
 *
 *  Unloads an appdomain. Follows the process outlined in:
 *  http://blogs.gotdotnet.com/cbrumme
 *
 *  If doing things the 'right' way is too hard or complex, we do it the 
 *  'simple' way, which means do everything needed to avoid crashes and
 *  memory leaks, but not much else.
 *
 *  It is required to pass a valid reference to the exc argument, upon return
 *  from this function *exc will be set to the exception thrown, if any.
 *
 *  If this method is not called from an icall (embedded scenario for instance),
 *  it must not be called with any managed frames on the stack, since the unload
 *  process could end up trying to abort the current thread.
 */
void
mono_domain_try_unload (MonoDomain *domain, MonoObject **exc)
{
	HANDLE thread_handle;
	gsize tid;
	guint32 res;
	MonoAppDomainState prev_state;
	MonoMethod *method;
	unload_data thread_data;
	MonoDomain *caller_domain = mono_domain_get ();

	/* printf ("UNLOAD STARTING FOR %s (%p) IN THREAD 0x%x.\n", domain->friendly_name, domain, GetCurrentThreadId ()); */

	/* Atomically change our state to UNLOADING */
	prev_state = InterlockedCompareExchange ((gint32*)&domain->state,
											 MONO_APPDOMAIN_UNLOADING_START,
											 MONO_APPDOMAIN_CREATED);
	if (prev_state != MONO_APPDOMAIN_CREATED) {
		switch (prev_state) {
		case MONO_APPDOMAIN_UNLOADING_START:
		case MONO_APPDOMAIN_UNLOADING:
			*exc = (MonoObject *) mono_get_exception_cannot_unload_appdomain ("Appdomain is already being unloaded.");
			return;
		case MONO_APPDOMAIN_UNLOADED:
			*exc = (MonoObject *) mono_get_exception_cannot_unload_appdomain ("Appdomain is already unloaded.");
			return;
		default:
			g_warning ("Invalid appdomain state %d", prev_state);
			g_assert_not_reached ();
		}
	}

	mono_debugger_event_unload_appdomain (domain);

	mono_domain_set (domain, FALSE);
	/* Notify OnDomainUnload listeners */
	method = mono_class_get_method_from_name (domain->domain->mbr.obj.vtable->klass, "DoDomainUnload", -1);	
	g_assert (method);

	mono_runtime_invoke (method, domain->domain, NULL, exc);
	if (*exc) {
		/* Roll back the state change */
		domain->state = MONO_APPDOMAIN_CREATED;
		mono_domain_set (caller_domain, FALSE);
		return;
	}
	mono_domain_set (caller_domain, FALSE);

	thread_data.domain = domain;
	thread_data.failure_reason = NULL;

	/*The managed callback finished successfully, now we start tearing down the appdomain*/
	domain->state = MONO_APPDOMAIN_UNLOADING;
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
	thread_handle = mono_create_thread (NULL, 0, unload_thread_main, &thread_data, 0, &tid);
#else
	thread_handle = mono_create_thread (NULL, 0, (LPTHREAD_START_ROUTINE)unload_thread_main, &thread_data, CREATE_SUSPENDED, &tid);
	if (thread_handle == NULL) {
		return;
	}
	ResumeThread (thread_handle);
#endif

	/* Wait for the thread */	
	while ((res = WaitForSingleObjectEx (thread_handle, INFINITE, TRUE) == WAIT_IO_COMPLETION)) {
		if (mono_thread_internal_has_appdomain_ref (mono_thread_internal_current (), domain) && (mono_thread_interruption_requested ())) {
			/* The unload thread tries to abort us */
			/* The icall wrapper will execute the abort */
			CloseHandle (thread_handle);
			return;
		}
	}
	CloseHandle (thread_handle);

	if (thread_data.failure_reason) {
		/* Roll back the state change */
		domain->state = MONO_APPDOMAIN_CREATED;

		g_warning ("%s", thread_data.failure_reason);

		*exc = (MonoObject *) mono_get_exception_cannot_unload_appdomain (thread_data.failure_reason);

		g_free (thread_data.failure_reason);
		thread_data.failure_reason = NULL;
	}
}
