/*
 * appdomain.c: AppDomain functions
 *
 * Author:
 *	Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <string.h>

#if HAVE_BOEHM_GC
#include <gc/gc.h>
#endif

#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/socket-io.h>
#include <mono/metadata/cil-coff.h>

HANDLE mono_delegate_semaphore = NULL;
CRITICAL_SECTION mono_delegate_section;
int mono_runtime_shutdown = 0;

/*
 * mono_runtime_init:
 * @domain: domain returned by mono_init ()
 *
 * Initialize the core AppDomain: this function will run also some
 * IL initialization code, so it needs the execution engine to be fully 
 * operational.
 */
void
mono_runtime_init (MonoDomain *domain, MonoThreadStartCB start_cb)
{
	MonoAppDomainSetup *setup;
	MonoAppDomain *ad;
	MonoClass *class;
	
	class = mono_class_from_name (mono_defaults.corlib, "System", "AppDomainSetup");
	setup = (MonoAppDomainSetup *) mono_object_new (domain, class);
	ves_icall_System_AppDomainSetup_InitAppDomainSetup (setup);

	class = mono_class_from_name (mono_defaults.corlib, "System", "AppDomain");
	ad = (MonoAppDomain *) mono_object_new (domain, class);
	ad->data = domain;
	domain->domain = ad;
	domain->setup = setup;

	mono_delegate_semaphore = CreateSemaphore (NULL, 0, 0x7fffffff, NULL);
	g_assert (mono_delegate_semaphore != INVALID_HANDLE_VALUE);
	InitializeCriticalSection (&mono_delegate_section);
	
	mono_thread_init (domain, start_cb);

	mono_network_init ();

	return;
}

void
mono_runtime_cleanup (MonoDomain *domain)
{
	mono_runtime_shutdown = 1;

	/* signal all waiters in order to stop all workers (max. 0xffff) */
	ReleaseSemaphore (mono_delegate_semaphore, 0xffff, NULL);

	mono_thread_cleanup ();

	/* Do this after the thread cleanup, because subthreads might
	 * still be doing socket calls.
	 */
	mono_network_cleanup ();
}

void
ves_icall_System_AppDomainSetup_InitAppDomainSetup (MonoAppDomainSetup *setup)
{
	/* FIXME: implement me */
}

/**
 * mono_domain_transfer_object:
 * @src: the source domain
 * @dst: the destination domain
 * @obj: the object to transfer
 *
 * This function is used to transfer objects between domains. This is done by
 * marshalling or serialisation. 
 */
static MonoObject *
mono_domain_transfer_object (MonoDomain *src, MonoDomain *dst, MonoObject *obj)
{
	MonoClass *klass;
	MonoObject *res;	

	if (!obj)
		return NULL;

	g_assert (obj->vtable->domain == src);

	/* fixme: transfer an object from one domain into another */

	klass = obj->vtable->klass;

	if (MONO_CLASS_IS_ARRAY (klass)) {
		MonoArray *ao = (MonoArray *)obj;
		int esize, ecount, i;
		guint32 *sizes;
		
		esize = mono_array_element_size (klass);
		if (ao->bounds == NULL) {
			ecount = mono_array_length (ao);
			res = (MonoObject *)mono_array_new_full (dst, klass, &ecount, NULL);
		}
		else {
			sizes = alloca (klass->rank * sizeof(guint32) * 2);
			ecount = 1;
			for (i = 0; i < klass->rank; ++i) {
				sizes [i] = ao->bounds [i].length;
				ecount *= ao->bounds [i].length;
				sizes [i + klass->rank] = ao->bounds [i].lower_bound;
			}
			res = (MonoObject *)mono_array_new_full (dst, klass, sizes, sizes + klass->rank);
		}
		if (klass->element_class->valuetype) {
			memcpy (res, (char *)ao + sizeof(MonoArray), esize * ecount);
		} else {
			g_assert (esize == sizeof (gpointer));
			for (i = 0; i < ecount; i++) {
				int offset = sizeof (MonoArray) + esize * i;
				gpointer *src_ea = (gpointer *)((char *)ao + offset);
				gpointer *dst_ea = (gpointer *)((char *)res + offset);

				*dst_ea = mono_domain_transfer_object (src, dst, *src_ea);
			}
		}
	} else if (klass == mono_defaults.string_class) {
		MonoString *str = (MonoString *)obj;
		res = (MonoObject *)mono_string_new_utf16 (dst, 
		        (const guint16 *)mono_string_chars (str), str->length); 
	} else {
		/* FIXME: we need generic marshalling code here */
		g_assert_not_reached ();
	}
	
	return res;
}

MonoObject *
ves_icall_System_AppDomain_GetData (MonoAppDomain *ad, MonoString *name)
{
	MonoDomain *add = ad->data;
	MonoDomain *cur = mono_domain_get ();
	MonoObject *o;
	char *str;

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
		o = g_hash_table_lookup (add->env, str);

	mono_domain_unlock (add);
	g_free (str);

	if (!o)
		return NULL;

	return mono_domain_transfer_object (add, cur, o);
}

void
ves_icall_System_AppDomain_SetData (MonoAppDomain *ad, MonoString *name, MonoObject *data)
{
	MonoDomain *add = ad->data;
	MonoDomain *cur = mono_domain_get ();
	MonoObject *o;
	char *str;

	g_assert (ad != NULL);
	g_assert (name != NULL);

	o = mono_domain_transfer_object (cur, add, data);

	/* fixme: need a hash func for MonoString */
	str = mono_string_to_utf8 (name);
	mono_domain_lock (add);
	g_hash_table_insert (add->env, str, o);
	mono_domain_unlock (add);
	g_free (str);
}

MonoAppDomainSetup *
ves_icall_System_AppDomain_getSetup (MonoAppDomain *ad)
{
	g_assert (ad != NULL);
	g_assert (ad->data != NULL);

	return ad->data->setup;
}

MonoString *
ves_icall_System_AppDomain_getFriendlyName (MonoAppDomain *ad)
{
	g_assert (ad != NULL);
	g_assert (ad->data != NULL);

	return mono_string_new (ad->data, ad->data->friendly_name);
}

MonoAppDomain *
ves_icall_System_AppDomain_getCurDomain ()
{
	MonoDomain *add = mono_domain_get ();
	return add->domain;
}

MonoAppDomain *
ves_icall_System_AppDomain_createDomain (MonoString *friendly_name, MonoAppDomainSetup *setup)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *adclass;
	MonoAppDomain *ad;
	MonoDomain *data;
	
	adclass = mono_class_from_name (mono_defaults.corlib, "System", "AppDomain");
	
	/* FIXME: pin all those objects */
	ad = (MonoAppDomain *) mono_object_new (domain, adclass);
	ad->data = data = mono_domain_create ();
	data->domain = ad;
	data->setup = setup;
	data->friendly_name = mono_string_to_utf8 (friendly_name);

	/* FIXME: what to do next ? */

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

MonoReflectionAssembly *
ves_icall_System_Reflection_Assembly_LoadFrom (MonoString *fname)
{
	MonoDomain *domain = mono_domain_get ();
	char *name, *filename;
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	MonoAssembly *ass;

	name = filename = mono_string_to_utf8 (fname);

	/* FIXME: move uri handling to mono_assembly_open */
	if (strncmp (filename, "file://", 7) == 0)
		filename += 7;

	ass = mono_assembly_open (filename, &status);
	
	g_free (name);

	if (!ass)
		mono_raise_exception ((MonoException *)mono_exception_from_name (mono_defaults.corlib, "System.IO", "FileNotFoundException"));

	return mono_assembly_get_object (domain, ass);
}


MonoReflectionAssembly *
ves_icall_System_AppDomain_LoadAssembly (MonoAppDomain *ad,  MonoReflectionAssemblyName *assRef, MonoObject *evidence)
{
	MonoDomain *domain = ad->data; 
	char *name;
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	MonoAssembly *ass;
	MonoAssemblyName aname;

	memset (&aname, 0, sizeof (aname));

	/* FIXME : examine evidence? */

	g_assert (assRef != NULL);
	g_assert (assRef->name != NULL);

	/* FIXME : examine version, culture info */

	aname.name = name = mono_string_to_utf8 (assRef->name);

	ass = mono_assembly_load (&aname, NULL, &status);
	
	g_free (name);

	if (!ass)
		mono_raise_exception ((MonoException *)mono_exception_from_name (mono_defaults.corlib, "System.IO", "FileNotFoundException"));

	return mono_assembly_get_object (domain, ass);
}

void
ves_icall_System_AppDomain_Unload (MonoAppDomain *ad)
{
	mono_domain_unload (ad->data, FALSE);
}

gint32
ves_icall_System_AppDomain_ExecuteAssembly (MonoAppDomain *ad, MonoString *file, 
					    MonoObject *evidence, MonoArray *args)
{
	MonoDomain *cdom = mono_domain_get ();
	MonoAssembly *assembly;
	MonoImage *image;
	MonoCLIImageInfo *iinfo;
	MonoMethod *method;
	MonoObject *margs;
	char *filename;
	gint32 res;

	mono_domain_set (ad->data);

	filename = mono_string_to_utf8 (file);
	assembly = mono_assembly_open (filename, NULL);
	g_free (filename);

	if (!assembly) {
		mono_raise_exception ((MonoException *)mono_exception_from_name (
		        mono_defaults.corlib, "System.IO", "FileNotFoundException"));
	}

	image = assembly->image;
	iinfo = image->image_info;
	method = mono_get_method (image, iinfo->cli_cli_header.ch_entry_point, NULL);

	if (!method)
		g_error ("No entry point method found in %s", image->name);

	margs = mono_domain_transfer_object (cdom, ad->data, (MonoObject *)args);
	res = mono_runtime_exec_main (method, (MonoArray *)margs, NULL);

	mono_domain_set (cdom);

	return res;
}

