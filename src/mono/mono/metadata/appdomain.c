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

#include <mono/os/gc_wrapper.h>

#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/socket-io.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/gc-internal.h>

HANDLE mono_delegate_semaphore = NULL;
CRITICAL_SECTION mono_delegate_section;

static MonoObject *
mono_domain_transfer_object (MonoDomain *src, MonoDomain *dst, MonoObject *obj);

static MonoAssembly *
mono_domain_assembly_preload (MonoAssemblyName *aname,
			      gchar **assemblies_path,
			      gpointer user_data);

static void
mono_domain_fire_assembly_load (MonoAssembly *assembly, gpointer user_data);

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
		   MonoThreadStartCB attach_cb)
{
	MonoAppDomainSetup *setup;
	MonoAppDomain *ad;
	MonoClass *class;
	
	mono_install_assembly_preload_hook (mono_domain_assembly_preload, NULL);
	mono_install_assembly_load_hook (mono_domain_fire_assembly_load, NULL);

	class = mono_class_from_name (mono_defaults.corlib, "System", "AppDomainSetup");
	setup = (MonoAppDomainSetup *) mono_object_new (domain, class);

	class = mono_class_from_name (mono_defaults.corlib, "System", "AppDomain");
	ad = (MonoAppDomain *) mono_object_new (domain, class);
	ad->data = domain;
	domain->domain = ad;
	domain->setup = setup;

	mono_delegate_semaphore = CreateSemaphore (NULL, 0, 0x7fffffff, NULL);
	g_assert (mono_delegate_semaphore != INVALID_HANDLE_VALUE);
	InitializeCriticalSection (&mono_delegate_section);

	mono_thread_init (domain, start_cb, attach_cb);
	
	/* GC init has to happen after thread init */
	mono_gc_init ();

	mono_network_init ();

	return;
}

void
mono_runtime_cleanup (MonoDomain *domain)
{
	mono_thread_cleanup ();

	/* Do this after the thread cleanup, because subthreads might
	 * still be doing socket calls.
	 */
	mono_network_cleanup ();
}

void
ves_icall_System_AppDomainSetup_InitAppDomainSetup (MonoAppDomainSetup *setup)
{
	MonoDomain* domain = mono_domain_get ();
	MonoAssembly *assembly;
	gchar *str;

	MONO_ARCH_SAVE_REGS;

	assembly = domain->entry_assembly;
	g_assert (assembly);

	setup->application_base = mono_string_new (domain, assembly->basedir);
	str = g_strconcat (assembly->basedir, assembly->aname.name,
			   ".exe.config", NULL);
	setup->configuration_file = mono_string_new (domain, str);
	g_free (str);
}

/*
 * invokes a method in a specific domain.
 */
static MonoObject*
mono_runtime_invoke_in_domain (MonoDomain *domain, MonoMethod *method, void *obj, 
			       void **params, MonoObject **exc)
{
	MonoDomain *cur = mono_domain_get ();
	MonoObject **real_exc, *default_exc;
	MonoObject *res;

	if (domain == cur)
		return mono_runtime_invoke (method, obj, params, exc);

	if (!exc)
		real_exc = &default_exc;
	else
		real_exc = exc;

	*real_exc = NULL;
	mono_domain_set (domain);
	res = mono_runtime_invoke (method, obj, params, real_exc);
	mono_domain_set (cur);

	if (*real_exc) {
		/* transfer Exception to the right domain */
		*real_exc = mono_domain_transfer_object (domain, cur, *real_exc);

		if (!exc)
			mono_raise_exception ((MonoException *)*real_exc);

	}

	return res;
}

static MonoObject *
mono_domain_transfer_array (MonoDomain *src, MonoDomain *dst, MonoArray *ao)
{
	MonoObject *res = NULL;
	MonoClass *klass;
	int esize, ecount, i;
	guint32 *sizes;
		
	klass = ao->obj.vtable->klass;

	esize = mono_array_element_size (klass);
	if (ao->bounds == NULL) {
		ecount = mono_array_length (ao);
		res = (MonoObject *)mono_array_new_full (dst, klass, &ecount, NULL);
	} else {
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
		if (klass->element_class->blittable) {
			memcpy (((MonoArray *)res)->vector, ao->vector, esize * ecount);
		} else {
			for (i = 0; i < ecount; i++) {
				MonoObject *s, *d;
				gpointer *src_ea = (gpointer *)mono_array_addr_with_size (ao, esize, i);
				gpointer *dst_ea = (gpointer *)mono_array_addr_with_size ((MonoArray *)res, esize, i);
				s = mono_value_box (src, klass->element_class, src_ea);
				d = mono_domain_transfer_object (src, dst, s);
				memcpy (dst_ea, (char *)d + sizeof(MonoObject), esize);
			}
		}
	} else {
		g_assert (esize == sizeof (gpointer));
		for (i = 0; i < ecount; i++) {
			gpointer *src_ea = (gpointer *)mono_array_addr_with_size (ao, esize, i);
			gpointer *dst_ea = (gpointer *)mono_array_addr_with_size ((MonoArray *)res, esize, i);
			*dst_ea = mono_domain_transfer_object (src, dst, *src_ea);
		}
	}
	return res;
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
	MonoClass *sic = mono_defaults.serializationinfo_class;
	MonoClass *klass;
	MonoObject *res = NULL;	
	int i;

	if (!obj)
		return NULL;

	g_assert (obj->vtable->domain == src);

	klass = obj->vtable->klass;

	/* some special cases */
	if (klass == mono_defaults.string_class) {
		MonoString *str = (MonoString *)obj;
		return (MonoObject *)mono_string_new_utf16 (dst, 
		        (const guint16 *)mono_string_chars (str), str->length); 
	} 

	if (klass == mono_defaults.monotype_class)
		return (MonoObject *) mono_type_get_object (dst, ((MonoReflectionType *)obj)->type);	

	if (MONO_CLASS_IS_ARRAY (klass))
		return mono_domain_transfer_array (src, dst, (MonoArray *)obj); 

	if (mono_object_isinst (obj, mono_defaults.iserializeable_class)) {
		static MonoMethod *serinfo_ctor1 = NULL, *serinfo_ctor2 = NULL, *get_entries = NULL;
		MonoMethod *get_object_data, *ser_ctor;
		MonoObject *src_serinfo, *dst_serinfo;
		void *pa [2];
		MonoStreamingContext ctx;
		MonoArray *entries, *trans_entries;
		int len;

		/* get a pointer to the GetObjectData method */ 
		get_object_data = klass->vtable [klass->interface_offsets [mono_defaults.iserializeable_class->interface_id]];
		g_assert (get_object_data);

		src_serinfo = mono_object_new (src, sic);

		if (!serinfo_ctor1) {
			for (i = 0; i < sic->method.count; ++i) {
				if (!strcmp (".ctor", sic->methods [i]->name) &&
				    sic->methods [i]->signature->param_count == 1) {
					serinfo_ctor1 = sic->methods [i];
					break;
				}
			}
			g_assert (serinfo_ctor1);
		}
		
		if (!serinfo_ctor2) {
			for (i = 0; i < sic->method.count; ++i) {
				if (!strcmp (".ctor", sic->methods [i]->name) &&
				    sic->methods [i]->signature->param_count == 2 &&
				    sic->methods [i]->signature->params [1]->type == MONO_TYPE_SZARRAY) {
					serinfo_ctor2 = sic->methods [i];
					break;
				}
			}
			g_assert (serinfo_ctor2);
		}
		
		if (!get_entries) {
			for (i = 0; i < sic->method.count; ++i) {
				if (!strcmp ("get_entries", sic->methods [i]->name) &&
				    sic->methods [i]->signature->param_count == 0) {
					get_entries = sic->methods [i];
					break;
				}
			}
			g_assert (get_entries);
		}

		pa [0] = mono_type_get_object (src, &klass->byval_arg);
		mono_runtime_invoke_in_domain (src, serinfo_ctor1, src_serinfo, pa, NULL);

		ctx.additional = NULL;
		ctx.state = 128; /* CrossAppDomain */
		pa [0] = src_serinfo;
		pa [1] = &ctx;
		mono_runtime_invoke_in_domain (src, get_object_data, obj, pa, NULL);

		entries = (MonoArray *)mono_runtime_invoke_in_domain (src, get_entries, src_serinfo, NULL, NULL);

		g_assert (src_serinfo->vtable->domain == src);
		g_assert (entries->obj.vtable->domain == src);

		/* transfer all SerializationEntry objects */
		len = mono_array_length ((MonoArray*)entries);
		trans_entries = mono_array_new (dst, entries->obj.vtable->klass->element_class, len);

		for (i = 0; i < len; i++) {
			MonoSerializationEntry *s, *d;
			s = (MonoSerializationEntry *)mono_array_addr_with_size (entries, 
			        sizeof (MonoSerializationEntry), i);
			d = (MonoSerializationEntry *)mono_array_addr_with_size (trans_entries, 
                                sizeof (MonoSerializationEntry), i);
			d->name = (MonoString *)mono_domain_transfer_object (src, dst, (MonoObject *)s->name);
			d->value = mono_domain_transfer_object (src, dst, s->value);
			d->type = (MonoReflectionType *)mono_domain_transfer_object (src, dst, (MonoObject *)s->type);
		}

		dst_serinfo = mono_object_new (dst, sic);

		pa [0] = mono_type_get_object (dst, &klass->byval_arg);
		pa [1] = trans_entries;
		mono_runtime_invoke_in_domain (dst, serinfo_ctor2, dst_serinfo, pa, NULL);

		ser_ctor = NULL;
		for (i = 0; i < klass->method.count; i++) {
			MonoMethod *t = klass->methods [i];
			if (!strcmp (t->name, ".ctor") && t->signature->param_count == 2 &&
			    mono_metadata_type_equal (t->signature->params [0], 
						      &mono_defaults.serializationinfo_class->byval_arg) &&
			    mono_metadata_type_equal (t->signature->params [1], 
						      &mono_defaults.streamingcontext_class->byval_arg))
				ser_ctor = t;
		}
		g_assert (ser_ctor);

		res = mono_object_new (dst, klass);

		ctx.additional = NULL;
		ctx.state = 128; /* CrossAppDomain */
		pa [0] = dst_serinfo;
		pa [1] = &ctx;
		mono_runtime_invoke_in_domain (dst, ser_ctor, res, pa, NULL);

		return res;
	}

	if (!(klass->flags & TYPE_ATTRIBUTE_SERIALIZABLE)) {
		MonoException *exc = NULL;
		char *msg;

		msg = g_strdup_printf ("klass \"%s.%s\" is not serializable", 
				       klass->name_space, klass->name);
		exc = mono_get_exception_serialization (msg);
		g_free (msg);

		mono_raise_exception (exc);
	}

	res = mono_object_new (dst, klass);

	for (i = 0; i < klass->field.count; i++) {
		MonoClassField *field = &klass->fields [i];
		MonoType *type = field->type;
		int size, align;
		char *src_ptr, *dst_ptr;
		int simple_type;

		if (type->attrs & FIELD_ATTRIBUTE_STATIC ||
		    type->attrs & FIELD_ATTRIBUTE_NOT_SERIALIZED)
			continue;

		size = mono_type_size (type, &align);

		dst_ptr = (char*)res + field->offset;
		src_ptr = (char *)obj + field->offset;

		g_assert (!type->byref);
		
		simple_type = type->type;
	handle_enum:
		switch (simple_type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_CHAR: 
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_I8: 
		case MONO_TYPE_U8:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			memcpy (dst_ptr, src_ptr, size);
			break;
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_STRING: {
			*(MonoObject **)dst_ptr = mono_domain_transfer_object (src, dst, *(MonoObject **)src_ptr);
			break;
		}
		case MONO_TYPE_VALUETYPE: {
			MonoObject *boxed_src, *tmp;

			if (type->data.klass->enumtype) {
				simple_type = type->data.klass->enum_basetype->type;
				goto handle_enum;
			}
			boxed_src = mono_value_box (src, type->data.klass, src_ptr);
			tmp = mono_domain_transfer_object (src, dst, boxed_src);
			memcpy (dst_ptr, (char *)tmp + sizeof (MonoObject), size);
			break;
		}
		default:
			g_assert_not_reached ();
		}
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

	return mono_domain_transfer_object (add, cur, o);
}

void
ves_icall_System_AppDomain_SetData (MonoAppDomain *ad, MonoString *name, MonoObject *data)
{
	MonoDomain *add = ad->data;
	MonoDomain *cur = mono_domain_get ();
	MonoObject *o;

	MONO_ARCH_SAVE_REGS;

	g_assert (ad != NULL);
	g_assert (name != NULL);

	o = mono_domain_transfer_object (cur, add, data);

	mono_domain_lock (add);
	mono_g_hash_table_insert (add->env, name, o);

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
ves_icall_System_AppDomain_createDomain (MonoString *friendly_name, MonoAppDomainSetup *setup)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *adclass;
	MonoAppDomain *ad;
	MonoDomain *data;
	
	MONO_ARCH_SAVE_REGS;

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

	klass = domain->domain->object.vtable->klass;
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

	if (g_hash_table_lookup (domain->assemblies, ass->aname.name))
		return; /* This is ok while no lazy loading of assemblies */

	mono_domain_lock (domain);
	g_hash_table_insert (domain->assemblies, (gpointer) ass->aname.name, ass);
	mono_domain_unlock (domain);

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

	klass = domain->domain->object.vtable->klass;
	
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

static void
set_domain_search_path (MonoDomain *domain)
{
	MonoAppDomainSetup *setup;
	gchar **tmp;
	gchar *utf8;
	gint i;
	gint npaths = 0;
	gchar **pvt_split = NULL;

	if (domain->search_path != NULL)
		return;

	setup = domain->setup;
	if (setup->application_base)
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

	domain->search_path = tmp = g_malloc ((npaths + 1) * sizeof (gchar *));
	tmp [npaths] = NULL;
	if (setup->application_base) {
		*tmp = mono_string_to_utf8 (setup->application_base);
	} else {
		*tmp = g_strdup ("");
	}

	tmp++;
	npaths--;
	for (i = 0; i < npaths; i++)
		tmp [i] = pvt_split [i];

	g_strfreev (pvt_split);
}

static MonoAssembly *
real_load (gchar **search_path, gchar *filename)
{
	MonoAssembly *result;
	gchar **path;
	gchar *fullpath;

	for (path = search_path; *path; path++) {
		if (**path == '\0')
			continue; /* Ignore empty ApplicationBase */
		fullpath = g_build_filename (*path, filename, NULL);
		result = mono_assembly_open (fullpath, NULL);
		g_free (fullpath);
		if (result)
			return result;
	}

	return NULL;
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
	MonoAssembly *result;
	gchar *dll, *exe;

	set_domain_search_path (domain);

	dll = g_strconcat (aname->name, ".dll", NULL);
	exe = g_strdup (dll);
	strcpy (exe + strlen (exe) - 4, ".exe");

	if (domain->search_path && domain->search_path [0] != NULL) {
		/* TODO: should also search in name/name.dll and name/name.exe from appbase */
		result = real_load (domain->search_path, dll);
		if (result) {
			g_free (dll);
			g_free (exe);
			return result;
		}

		result = real_load (domain->search_path, exe);
		if (result) {
			g_free (dll);
			g_free (exe);
			return result;
		}
	}

	if (assemblies_path && assemblies_path [0] != NULL) {
		result = real_load (assemblies_path, dll);
		if (result) {
			g_free (dll);
			g_free (exe);
			return result;
		}

		result = real_load (assemblies_path, exe);
		if (result) {
			g_free (dll);
			g_free (exe);
			return result;
		}
	}
	
	g_free (dll);
	g_free (exe);
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

	/* FIXME: move uri handling to mono_assembly_open */
	if (strncmp (filename, "file://", 7) == 0)
		filename += 7;

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
get_info_from_assembly_name (MonoReflectionAssemblyName *assRef, MonoAssemblyName *aname)
{
	gchar *name;
	gchar *value;
	gchar **parts;
	gchar **tmp;
	gint major, minor, build, revision;

	memset (aname, 0, sizeof (MonoAssemblyName));

	name = mono_string_to_utf8 (assRef->name);
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

	value = g_strstrip (*tmp);
	if (strncmp (value, "Version=", 8)) {
		g_strfreev (parts);
		return FALSE;
	}
	
	if (sscanf (value + 8, "%u.%u.%u.%u", &major, &minor, &build, &revision) != 4) {
		g_strfreev (parts);
		return FALSE;
	}

	/* g_print ("Version: %u.%u.%u.%u\n", major, minor, build, revision); */
	aname->major = major;
	aname->minor = minor;
	aname->build = build;
	aname->revision = revision;
	tmp++;

	if (!*tmp) {
		g_strfreev (parts);
		return FALSE;
	}

	value = g_strstrip (*tmp);
	if (strncmp (value, "Culture=", 8)) {
		g_strfreev (parts);
		return FALSE;
	}

	/* g_print ("Culture: %s\n", aname->culture); */
	aname->culture = g_strstrip (g_strdup (value + 8));
	tmp++;

	if (!*tmp) {
		g_strfreev (parts);
		return FALSE;
	}

	value = g_strstrip (*tmp);
	if (strncmp (value, "PublicKeyToken=", 15)) {
		g_strfreev (parts);
		return FALSE;
	}

	value += 15;
	if (*value && strcmp (value, "null")) {
		gint i, len;
		gchar h, l;
		gchar *result;
		
		value = g_strstrip (g_strdup (value));
		len = strlen (value);
		if (len % 2) {
			g_strfreev (parts);
			return FALSE;
		}
		
		aname->hash_len = len / 2;
		aname->hash_value = g_malloc0 (aname->hash_len);
		result = (gchar *) aname->hash_value;
		
		for (i = 0; i < len; i++) {
			if (i % 2) {
				l = g_ascii_xdigit_value (value [i]);
				if (l == -1) {
					g_strfreev (parts);
					return FALSE;
				}
				result [i / 2] = (h * 16) + l;
			} else {
				h = g_ascii_xdigit_value (value [i]);
				if (h == -1) {
					g_strfreev (parts);
					return FALSE;
				}
			}
		}

		/*
		g_print ("PublicKeyToken: ");
		for (i = 0; i < aname->hash_len; i++) {
			g_print ("%x", 0x00FF & aname->hash_value [i]); 
		}
		g_print ("\n");
		*/
	}

	g_strfreev (parts);
	return TRUE;
}

MonoReflectionAssembly *
ves_icall_System_AppDomain_LoadAssembly (MonoAppDomain *ad,  MonoReflectionAssemblyName *assRef, MonoObject *evidence)
{
	MonoDomain *domain = ad->data; 
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	MonoAssembly *ass;
	MonoAssemblyName aname;
	MonoReflectionAssembly *refass = NULL;

	MONO_ARCH_SAVE_REGS;

	memset (&aname, 0, sizeof (aname));

	/* FIXME : examine evidence? */

	g_assert (assRef != NULL);
	g_assert (assRef->name != NULL);

	if (!get_info_from_assembly_name (assRef, &aname)) {
		MonoException *exc;

		free_assembly_name (&aname);
		/* This is a parse error... */
		exc = mono_get_exception_file_not_found (assRef->name);
		mono_raise_exception (exc);
	}

	ass = mono_assembly_load (&aname, NULL, &status);
	free_assembly_name (&aname);

	if (!ass && (refass = try_assembly_resolve (domain, assRef->name)) == NULL){
		/* FIXME: it doesn't make much sense since we really don't have a filename ... */
		MonoException *exc = mono_get_exception_file_not_found (assRef->name);
		mono_raise_exception (exc);
	}

	if (refass != NULL)
		return refass;

	return mono_assembly_get_object (domain, ass);
}

void
ves_icall_System_AppDomain_Unload (MonoAppDomain *ad)
{
	MONO_ARCH_SAVE_REGS;

	mono_domain_unload (ad->data, FALSE);
}

gint32
ves_icall_System_AppDomain_ExecuteAssembly (MonoAppDomain *ad, MonoString *file, 
					    MonoObject *evidence, MonoArray *args)
{
	MonoDomain *cdom = mono_domain_get ();
	MonoAssembly *assembly;
	MonoImage *image;
	MonoMethod *method;
	MonoObject *margs;
	char *filename;
	gint32 res;

	MONO_ARCH_SAVE_REGS;

	mono_domain_set (ad->data);

	filename = mono_string_to_utf8 (file);
	assembly = mono_assembly_open (filename, NULL);
	g_free (filename);

	if (!assembly) {
		mono_raise_exception ((MonoException *)mono_exception_from_name (
		        mono_defaults.corlib, "System.IO", "FileNotFoundException"));
	}

	image = assembly->image;

	method = mono_get_method (image, mono_image_get_entry_point (image), NULL);

	if (!method)
		g_error ("No entry point method found in %s", image->name);

	margs = mono_domain_transfer_object (cdom, ad->data, (MonoObject *)args);
	if (!margs)
		margs = (MonoObject *) mono_array_new (ad->data, mono_defaults.string_class, 0);
	res = mono_runtime_exec_main (method, (MonoArray *)margs, NULL);

	mono_domain_set (cdom);

	return res;
}

