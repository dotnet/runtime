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

#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/exception.h>

#define CUR_DOMAIN_DATA ((MonoAppDomainData *)TlsGetValue (appdomain_thread_id))

static guint32 appdomain_thread_id = 0;

void
mono_appdomain_init (char *friendly_name)
{
	static gboolean initialized = FALSE;
	MonoAppDomainSetup *setup;
	MonoAppDomain *ad;
	MonoClass *class;
	MonoString *name;

	if (initialized)
		return;

	appdomain_thread_id = TlsAlloc ();

	class = mono_class_from_name (mono_defaults.corlib, "System", "AppDomainSetup");
	setup = (MonoAppDomainSetup *) mono_object_new (class);
	ves_icall_System_AppDomainSetup_InitAppDomainSetup (setup);

	name = mono_string_new (friendly_name);
	ad = ves_icall_System_AppDomain_createDomain (name, setup);

	TlsSetValue (appdomain_thread_id, ad->data);
}

void
ves_icall_System_AppDomainSetup_InitAppDomainSetup (MonoAppDomainSetup *setup)
{
	// fixme: implement me
}

static MonoObject *
mono_appdomain_transfer_object (MonoAppDomainData *src, MonoAppDomainData *dst, MonoObject *obj)
{
	/* fixme: transfer an object from one domain into another */
	g_assert_not_reached ();
	return obj;
}

MonoObject *
ves_icall_System_AppDomain_GetData (MonoAppDomain *ad, MonoString *name)
{
	MonoAppDomainData *add = ad->data;
	MonoAppDomainData *cur = CUR_DOMAIN_DATA;
	MonoObject *o;
	char *str;

	g_assert (ad != NULL);
	g_assert (name != NULL);

	str = mono_string_to_utf8 (name);

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

	g_free (str);

	if (!o)
		return NULL;

	return mono_appdomain_transfer_object (add, cur, o);
}

void
ves_icall_System_AppDomain_SetData (MonoAppDomain *ad, MonoString *name, MonoObject *data)
{
	MonoAppDomainData *add = ad->data;
	MonoAppDomainData *cur = CUR_DOMAIN_DATA;
	MonoObject *o;
	char *str;

	g_assert (ad != NULL);
	g_assert (name != NULL);

	o = mono_appdomain_transfer_object (cur, add, data);

	/* fixme: need a hash func for MonoString */
	str = mono_string_to_utf8 (name);
	g_hash_table_insert (add->env, str, o);
	g_free (str);
}

MonoAppDomainSetup *
ves_icall_System_AppDomain_getSetup (MonoAppDomain *ad)
{
	g_assert (ad != NULL);
	g_assert (ad->data != NULL);

	return ad->data->setup;
}

MonoAppDomainSetup *
ves_icall_System_AppDomain_getFriendlyName (MonoAppDomain *ad)
{
	g_assert (ad != NULL);
	g_assert (ad->data != NULL);

	return ad->data->friendly_name;
}

MonoAppDomainData *
mono_get_cur_domain ()
{
	return CUR_DOMAIN_DATA;
}

MonoAppDomain *
ves_icall_System_AppDomain_getCurDomain ()
{
	MonoAppDomainData *add = CUR_DOMAIN_DATA;

	return add->domain;
}

MonoAppDomain *
ves_icall_System_AppDomain_createDomain (MonoString *friendly_name, MonoAppDomainSetup *setup)
{
	MonoClass *adclass;
	MonoAppDomain *ad;
	MonoAppDomainData *data;
	
	adclass = mono_class_from_name (mono_defaults.corlib, "System", "AppDomain");
	
	// fixme: pin all those objects
	ad = (MonoAppDomain *) mono_object_new (adclass);
	ad->data = data = g_new0 (MonoAppDomainData, 1);
	data->domain = ad;
	data->setup = setup;
	data->friendly_name = friendly_name;

	data->env = g_hash_table_new (g_str_hash, g_str_equal);
	data->assemblies = g_hash_table_new (g_str_hash, g_str_equal);

	// fixme: what to do next ?

	return ad;
}

typedef struct {
	MonoArray *res;
	int idx;
} add_assembly_helper_t;

static void
add_assembly (gpointer key, gpointer value, gpointer user_data)
{
	add_assembly_helper_t *ah = (add_assembly_helper_t *) user_data;

	mono_array_set (ah->res, gpointer, ah->idx++, mono_assembly_get_object (value));
}

//
// This is not correct, because we return all the assemblies loaded, and not
// those that come from the AppDomain, but its better than nothing.
//
MonoArray *
ves_icall_System_AppDomain_GetAssemblies (MonoAppDomain *ad)
{
	static MonoClass *System_Reflection_Assembly;
	GHashTable *assemblies = mono_get_assemblies ();
	MonoArray *res;
	add_assembly_helper_t ah;
	
	if (!System_Reflection_Assembly)
		System_Reflection_Assembly = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "Assembly");

	res = mono_array_new (System_Reflection_Assembly, g_hash_table_size (assemblies));

	ah.res = res;
	ah.idx = 0;
	g_hash_table_foreach (assemblies, add_assembly, &ah);

	return res;
}


MonoReflectionAssembly *
ves_icall_System_AppDomain_LoadAssembly (MonoAppDomain *ad, MonoString *assName, MonoObject *evidence)
{
	/* FIXME : examine evidence? */
	char *name = mono_string_to_utf8 (assName);
	enum MonoImageOpenStatus status = MONO_IMAGE_OK;
	MonoAssembly *ass = mono_assembly_open (name, NULL, &status);

	if (!ass)
		mono_raise_exception (mono_exception_from_name (mono_defaults.corlib, "System.IO", "FileNotFoundException"));

	return mono_assembly_get_object (ass);
}


