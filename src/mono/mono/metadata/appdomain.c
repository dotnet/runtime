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
#include <mono/metadata/cil-coff.h>

static guint32 appdomain_thread_id = 0;

static int
ldstr_hash (const char* str)
{
	guint len, h;
	const char *end;
	len = mono_metadata_decode_blob_size (str, &str);
	end = str + len;
	h = *str;
	/*
	 * FIXME: The distribution may not be so nice with lots of
	 * null chars in the string.
	 */
	for (str += 1; str < end; str++)
		h = (h << 5) - h + *str;
	return h;
}

static gboolean
ldstr_equal (const char *str1, const char *str2) {
	int len, len2;
	len = mono_metadata_decode_blob_size (str1, NULL);
	len2 = mono_metadata_decode_blob_size (str2, NULL);
	if (len != len2)
		return 0;
	return memcmp (str1, str2, len) == 0;
}

static MonoDomain *
mono_create_domain ()
{
	MonoDomain *domain;

#if HAVE_BOEHM_GC
	domain = GC_malloc (sizeof (MonoDomain));
#else
	domain = g_new0 (MonoDomain, 1);
#endif
	domain->domain = NULL;
	domain->setup = NULL;
	domain->friendly_name = NULL;

	domain->mp = mono_mempool_new ();
	domain->env = g_hash_table_new (g_str_hash, g_str_equal);
	domain->assemblies = g_hash_table_new (g_str_hash, g_str_equal);
	domain->class_vtable_hash = g_hash_table_new (NULL, NULL);
	domain->jit_code_hash = g_hash_table_new (NULL, NULL);
	domain->ldstr_table = g_hash_table_new ((GHashFunc)ldstr_hash, (GCompareFunc)ldstr_equal);

	return domain;
}

MonoDomain *mono_root_domain = NULL;

/**
 * mono_init:
 * 
 * Creates the initial application domain and initializes the mono_defaults
 * structure.
 *
 * Returns: the initial domain.
 */
MonoDomain *
mono_init (const char *filename)
{
	static MonoDomain *domain = NULL;
	MonoAppDomainSetup *setup;
	MonoAppDomain *ad;
	MonoAssembly *ass;
	MonoClass *class;
	MonoString *name;
	enum MonoImageOpenStatus status = MONO_IMAGE_OK;

	if (domain)
		g_assert_not_reached ();

	appdomain_thread_id = TlsAlloc ();

	domain = mono_create_domain ();
	mono_root_domain = domain;

	TlsSetValue (appdomain_thread_id, domain);

	/* find the corlib */
	ass = mono_assembly_open (CORLIB_NAME, NULL, &status);
	if ((status != MONO_IMAGE_OK) || (ass == NULL)) {
		switch (status){
		case MONO_IMAGE_ERROR_ERRNO:
			g_print ("The assembly corlib.dll was not found or could not be loaded.\n");
			g_print ("It should have been installed in the `%s' directory.\n", MONO_ASSEMBLIES);
			break;

		case MONO_IMAGE_IMAGE_INVALID:
			g_print ("The file %s/corlib.dll is an invalid CIL image", MONO_ASSEMBLIES);
			break;
		}
		
		exit (1);
	}
	mono_defaults.corlib = ass->image;

	mono_defaults.object_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Object");
	g_assert (mono_defaults.object_class != 0);

	mono_defaults.void_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Void");
	g_assert (mono_defaults.void_class != 0);

	mono_defaults.boolean_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Boolean");
	g_assert (mono_defaults.boolean_class != 0);

	mono_defaults.byte_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Byte");
	g_assert (mono_defaults.byte_class != 0);

	mono_defaults.sbyte_class = mono_class_from_name (
                mono_defaults.corlib, "System", "SByte");
	g_assert (mono_defaults.sbyte_class != 0);

	mono_defaults.int16_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Int16");
	g_assert (mono_defaults.int16_class != 0);

	mono_defaults.uint16_class = mono_class_from_name (
                mono_defaults.corlib, "System", "UInt16");
	g_assert (mono_defaults.uint16_class != 0);

	mono_defaults.int32_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Int32");
	g_assert (mono_defaults.int32_class != 0);

	mono_defaults.uint32_class = mono_class_from_name (
                mono_defaults.corlib, "System", "UInt32");
	g_assert (mono_defaults.uint32_class != 0);

	mono_defaults.uint_class = mono_class_from_name (
                mono_defaults.corlib, "System", "UIntPtr");
	g_assert (mono_defaults.uint_class != 0);

	mono_defaults.int_class = mono_class_from_name (
                mono_defaults.corlib, "System", "IntPtr");
	g_assert (mono_defaults.int_class != 0);

	mono_defaults.int64_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Int64");
	g_assert (mono_defaults.int64_class != 0);

	mono_defaults.uint64_class = mono_class_from_name (
                mono_defaults.corlib, "System", "UInt64");
	g_assert (mono_defaults.uint64_class != 0);

	mono_defaults.single_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Single");
	g_assert (mono_defaults.single_class != 0);

	mono_defaults.double_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Double");
	g_assert (mono_defaults.double_class != 0);

	mono_defaults.char_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Char");
	g_assert (mono_defaults.char_class != 0);

	mono_defaults.string_class = mono_class_from_name (
                mono_defaults.corlib, "System", "String");
	g_assert (mono_defaults.string_class != 0);

	mono_defaults.enum_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Enum");
	g_assert (mono_defaults.enum_class != 0);

	mono_defaults.array_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Array");
	g_assert (mono_defaults.array_class != 0);

	mono_defaults.multicastdelegate_class = mono_class_from_name (
		mono_defaults.corlib, "System", "MulticastDelegate");
	g_assert (mono_defaults.multicastdelegate_class != 0 );

	mono_defaults.typehandle_class = mono_class_from_name (
                mono_defaults.corlib, "System", "RuntimeTypeHandle");
	g_assert (mono_defaults.typehandle_class != 0);

	mono_defaults.methodhandle_class = mono_class_from_name (
                mono_defaults.corlib, "System", "RuntimeMethodHandle");
	g_assert (mono_defaults.methodhandle_class != 0);

	mono_defaults.fieldhandle_class = mono_class_from_name (
                mono_defaults.corlib, "System", "RuntimeFieldHandle");
	g_assert (mono_defaults.fieldhandle_class != 0);

	mono_defaults.monotype_class = mono_class_from_name (
                mono_defaults.corlib, "System", "MonoType");
	g_assert (mono_defaults.monotype_class != 0);

	mono_defaults.exception_class = mono_class_from_name (
                mono_defaults.corlib, "System", "Exception");
	g_assert (mono_defaults.exception_class != 0);


	class = mono_class_from_name (mono_defaults.corlib, "System", "AppDomainSetup");
	setup = (MonoAppDomainSetup *) mono_object_new (domain, class);
	ves_icall_System_AppDomainSetup_InitAppDomainSetup (setup);

	name = mono_string_new (domain, g_path_get_basename (filename));

	class = mono_class_from_name (mono_defaults.corlib, "System", "AppDomain");
	ad = (MonoAppDomain *) mono_object_new (domain, class);
	ad->data = domain;
	domain->domain = ad;
	domain->setup = setup;
	domain->friendly_name = name;

	return domain;
}

void
ves_icall_System_AppDomainSetup_InitAppDomainSetup (MonoAppDomainSetup *setup)
{
	// fixme: implement me
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
		
		sizes = alloca (klass->rank * sizeof(guint32) * 2);
		esize = mono_array_element_size (klass);
		ecount = 1;
		for (i = 0; i < klass->rank; ++i) {
			sizes [i] = ao->bounds [i].length;
			ecount *= ao->bounds [i].length;
			sizes [i + klass->rank] = ao->bounds [i].lower_bound;
		}
		res = (MonoObject *)mono_array_new_full (dst, klass, sizes, sizes + klass->rank);
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
		        (const guint16 *)str->c_str->vector, str->length); 
	} else {
		// fixme: we need generic marshalling code here */
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

MonoString *
ves_icall_System_AppDomain_getFriendlyName (MonoAppDomain *ad)
{
	g_assert (ad != NULL);
	g_assert (ad->data != NULL);

	return ad->data->friendly_name;
}

/**
 * mono_domain_get:
 *
 * Returns the current domain.
 */
inline MonoDomain *
mono_domain_get ()
{
	return ((MonoDomain *)TlsGetValue (appdomain_thread_id));
}

/**
 * mono_domain_set:
 * @domain: the new domain
 *
 * Sets the current domain to @domain.
 */
inline void
mono_domain_set (MonoDomain *domain)
{
	TlsSetValue (appdomain_thread_id, domain);
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
	
	// fixme: pin all those objects
	ad = (MonoAppDomain *) mono_object_new (domain, adclass);
	ad->data = data = mono_create_domain ();
	data->domain = ad;
	data->setup = setup;
	data->friendly_name = friendly_name;

	// fixme: what to do next ?

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
	g_hash_table_foreach (domain->assemblies, add_assembly, &ah);

	return res;
}


MonoReflectionAssembly *
ves_icall_System_AppDomain_LoadAssembly (MonoAppDomain *ad,  MonoReflectionAssemblyName *assRef, MonoObject *evidence)
{
	MonoDomain *domain = ad->data; 
	char *name, *filename;
	enum MonoImageOpenStatus status = MONO_IMAGE_OK;
	MonoAssembly *ass;

	/* FIXME : examine evidence? */

	g_assert (assRef != NULL);
	g_assert (assRef->name != NULL);

	/* FIXME : examine version, culture info */

	name = filename = mono_string_to_utf8 (assRef->name);

	if (strncmp (filename, "file://", 7) == 0)
		filename += 7;

	ass = mono_assembly_open (filename, NULL, &status);
	
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

/**
 * mono_domain_assembly_open:
 * @domain: the application domain
 * @name: file name of the assembly
 *
 * fixme: maybe we should integrate this with mono_assembly_open ??
 */
MonoAssembly *
mono_domain_assembly_open (MonoDomain *domain, char *name)
{
	MonoAssembly *ass, *tmp;
	int i;

	if ((ass = g_hash_table_lookup (domain->assemblies, name)))
		return ass;

	if (!(ass = mono_assembly_open (name, NULL, NULL)))
		return NULL;

	g_hash_table_insert (domain->assemblies, ass->name, ass);

	// fixme: maybe this must be recursive ?
	for (i = 0; (tmp = ass->image->references [i]) != NULL; i++) {
		if (!g_hash_table_lookup (domain->assemblies, tmp->name))
			g_hash_table_insert (domain->assemblies, tmp->name, tmp);
	}

	return ass;
}

static void
remove_assembly (gpointer key, gpointer value, gpointer user_data)
{
	mono_assembly_close ((MonoAssembly *)value);
}

void
mono_domain_unload (MonoDomain *domain, gboolean force)
{
	if ((domain == mono_root_domain) && !force) {
		g_warning ("cant unload root domain");
		return;
	}

	g_hash_table_foreach (domain->assemblies, remove_assembly, NULL);

	g_hash_table_destroy (domain->env);
	g_hash_table_destroy (domain->assemblies);
	g_hash_table_destroy (domain->class_vtable_hash);
	g_hash_table_destroy (domain->jit_code_hash);
	g_hash_table_destroy (domain->ldstr_table);
	mono_mempool_destroy (domain->mp);
	
	// fixme: anything else required ? */

#if HAVE_BOEHM_GC
#else
	g_free (domain);
#endif

	if ((domain == mono_root_domain))
		mono_root_domain = NULL;
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
	assembly = mono_assembly_open (filename, NULL, NULL);
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
	res = mono_runtime_exec_main (method, (MonoArray *)margs);

	mono_domain_set (cdom);

	return res;
}
