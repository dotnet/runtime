/*
 * object.c: Object creation for the Mono runtime
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */
#include <config.h>
#include <stdlib.h>
#include <stdio.h>
#include <signal.h>
#include <string.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/threadpool.h>
#include <mono/metadata/marshal.h>
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/marshal.h"
#include <mono/metadata/threads.h>
#include <mono/metadata/environment.h>
#include "mono/metadata/profiler-private.h"
#include <mono/os/gc_wrapper.h>

/*
 * Enable experimental typed allocation using the GC_gcj_malloc function.
 */
#ifdef HAVE_GC_GCJ_MALLOC
#define CREATION_SPEEDUP 1
#endif

void
mono_runtime_object_init (MonoObject *this)
{
	int i;
	MonoMethod *method = NULL;
	MonoClass *klass = this->vtable->klass;

	for (i = 0; i < klass->method.count; ++i) {
		if (!strcmp (".ctor", klass->methods [i]->name) &&
		    klass->methods [i]->signature->param_count == 0) {
			method = klass->methods [i];
			break;
		}
	}

	g_assert (method);

	mono_runtime_invoke (method, this, NULL, NULL);
}

/*
 * mono_runtime_class_init:
 * @vtable: vtable that needs to be initialized
 *
 * This routine calls the class constructor for @vtable.
 */
void
mono_runtime_class_init (MonoVTable *vtable)
{
	int i;
	MonoException *exc;
	MonoException *exc_to_throw;
	MonoMethod *method;
	MonoClass *klass;
	gchar *full_name;
	gboolean found;

	MONO_ARCH_SAVE_REGS;

	if (vtable->initialized || vtable->initializing)
		return;

	exc = NULL;
	found = FALSE;
	klass = vtable->klass;
	
	for (i = 0; i < klass->method.count; ++i) {
		method = klass->methods [i];
		if ((method->flags & METHOD_ATTRIBUTE_SPECIAL_NAME) && 
		    (strcmp (".cctor", method->name) == 0)) {
			found = TRUE;
			break;
		}
	}

	if (found) {
		mono_domain_lock (vtable->domain);
		/* double check... */
		if (vtable->initialized || vtable->initializing)
			return;
		vtable->initializing = 1;
		mono_runtime_invoke (method, NULL, NULL, (MonoObject **) &exc);
		vtable->initialized = 1;
		vtable->initializing = 0;
		/* FIXME: if the cctor fails, the type must be marked as unusable */
		mono_domain_unlock (vtable->domain);
	} else {
		vtable->initialized = 1;
		return;
	}

	if (exc == NULL ||
	    (klass->image == mono_defaults.corlib &&		
	     !strcmp (klass->name_space, "System") &&
	     !strcmp (klass->name, "TypeInitializationException")))
		return; /* No static constructor found or avoid infinite loop */

	if (klass->name_space && *klass->name_space)
		full_name = g_strdup_printf ("%s.%s", klass->name_space, klass->name);
	else
		full_name = g_strdup (klass->name);

	exc_to_throw = mono_get_exception_type_initialization (full_name, exc);
	g_free (full_name);

	mono_raise_exception (exc_to_throw);
}

static gpointer
default_trampoline (MonoMethod *method)
{
	return method;
}

static gpointer
default_remoting_trampoline (MonoMethod *method)
{
	g_error ("remoting not installed");
	return NULL;
}

static MonoTrampoline arch_create_jit_trampoline = default_trampoline;
static MonoTrampoline arch_create_remoting_trampoline = default_remoting_trampoline;

void
mono_install_trampoline (MonoTrampoline func) 
{
	arch_create_jit_trampoline = func? func: default_trampoline;
}

void
mono_install_remoting_trampoline (MonoTrampoline func) 
{
	arch_create_remoting_trampoline = func? func: default_remoting_trampoline;
}

static MonoCompileFunc default_mono_compile_method = NULL;

void        
mono_install_compile_method (MonoCompileFunc func)
{
	default_mono_compile_method = func;
}

gpointer 
mono_compile_method (MonoMethod *method)
{
	if (!default_mono_compile_method) {
		g_error ("compile method called on uninitialized runtime");
		return NULL;
	}
	return default_mono_compile_method (method);
}


#if 0 && HAVE_BOEHM_GC
static void
vtable_finalizer (void *obj, void *data) {
	g_print ("%s finalized (%p)\n", (char*)data, obj);
}
#endif

#if CREATION_SPEEDUP

#define GC_NO_DESCRIPTOR ((gpointer)(0 | GC_DS_LENGTH))

/*
 * The vtable is assumed to be reachable by other roots, since vtables
 * are currently never freed. That might change in the future, but
 * for now, this is a correct (and worthwhile) optimization.
 */

#define GC_HEADER_BITMAP (1 << (G_STRUCT_OFFSET (MonoObject,synchronisation) / sizeof(gpointer)))

static void
mono_class_compute_gc_descriptor (MonoClass *class)
{
	MonoClassField *field;
	guint64 bitmap;
	int i;
	static gboolean gcj_inited = FALSE;

	if (!gcj_inited) {
		gcj_inited = TRUE;

		GC_init_gcj_malloc (5, NULL);
	}

	if (!class->inited)
		mono_class_init (class);

	if (class->gc_descr_inited)
		return;

	class->gc_descr_inited = TRUE;
	class->gc_descr = GC_NO_DESCRIPTOR;

	if (class == mono_defaults.string_class) {
		bitmap = GC_HEADER_BITMAP;
		class->gc_descr = (gpointer)GC_make_descriptor ((GC_bitmap)&bitmap, 2);
	}
	else if (class->rank) {
		mono_class_compute_gc_descriptor (class->element_class);

		if (class->element_class->valuetype && (class->element_class->gc_descr != GC_NO_DESCRIPTOR) && (class->element_class->gc_bitmap == GC_HEADER_BITMAP)) {
			bitmap = GC_HEADER_BITMAP;
			if (class->rank > 1)
				bitmap += 1 << (G_STRUCT_OFFSET (MonoArray,bounds) / sizeof(gpointer));
			class->gc_descr = (gpointer)GC_make_descriptor ((GC_bitmap)&bitmap, 3);
		}
	}
	else {
		static int count = 0;
		MonoClass *p;
		guint32 pos;

		/* GC 6.1 has trouble handling 64 bit descriptors... */
		if ((class->instance_size / sizeof (gpointer)) > 30) {
//			printf ("TOO LARGE: %s %d.\n", class->name, class->instance_size / sizeof (gpointer));
			return;
		}

		bitmap = GC_HEADER_BITMAP;

		count ++;

//		if (count > 442)
//			return;

//		printf("KLASS: %s.\n", class->name);

		for (p = class; p != NULL; p = p->parent) {
		for (i = 0; i < p->field.count; ++i) {
			field = &p->fields [i];
			if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
				continue;
			if (field->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA)
				return;

			pos = field->offset / sizeof (gpointer);
			
			if (field->type->byref)
				return;

			switch (field->type->type) {
			case MONO_TYPE_BOOLEAN:
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
			case MONO_TYPE_CHAR:
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
			case MONO_TYPE_R4:
			case MONO_TYPE_R8:
//				printf ("F: %s %s %d %lld %llx.\n", class->name, field->name, field->offset, ((guint64)1) << pos, bitmap);
				break;
			case MONO_TYPE_I:
			case MONO_TYPE_STRING:
			case MONO_TYPE_SZARRAY:
			case MONO_TYPE_CLASS:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_ARRAY:
			case MONO_TYPE_PTR:
				g_assert ((field->offset % sizeof(gpointer)) == 0);

				bitmap |= ((guint64)1) << pos;
//				printf ("F: %s %s %d %d %lld %llx.\n", class->name, field->name, field->offset, pos, ((guint64)(1)) << pos, bitmap);
				break;
			case MONO_TYPE_VALUETYPE: {
				MonoClass *fclass = field->type->data.klass;
				if (!fclass->enumtype) {
					mono_class_compute_gc_descriptor (fclass);
					bitmap |= (fclass->gc_bitmap & ~2) << pos;
				}
				break;
			}
			default:
				return;
			}
		}
		}

//		printf("CLASS: %s.%s -> %d %llx.\n", class->name_space, class->name, class->instance_size / sizeof (gpointer), bitmap);
		class->gc_bitmap = bitmap;
		class->gc_descr = (gpointer)GC_make_descriptor ((GC_bitmap)&bitmap, class->instance_size / sizeof (gpointer));
	}
}
#endif /* CREATION_SPEEDUP */

static gboolean
field_is_thread_static (MonoClass *fklass, MonoClassField *field)
{
	MonoCustomAttrInfo *ainfo;
	int i;
	ainfo = mono_custom_attrs_from_field (fklass, field);
	if (!ainfo)
		return FALSE;
	for (i = 0; i < ainfo->num_attrs; ++i) {
		MonoClass *klass = ainfo->attrs [i].ctor->klass;
		if (strcmp (klass->name, "ThreadStaticAttribute") == 0 && klass->image == mono_defaults.corlib) {
			mono_custom_attrs_free (ainfo);
			return TRUE;
		}
	}
	mono_custom_attrs_free (ainfo);
	return FALSE;
}

/**
 * mono_class_vtable:
 * @domain: the application domain
 * @class: the class to initialize
 *
 * VTables are domain specific because we create domain specific code, and 
 * they contain the domain specific static class data.
 */
MonoVTable *
mono_class_vtable (MonoDomain *domain, MonoClass *class)
{
	MonoVTable *vt;
	MonoClassField *field;
	const char *p;
	char *t;
	int i, len;

	g_assert (class);

	vt = class->cached_vtable;
	if (vt && vt->domain == domain)
		return vt;

	mono_domain_lock (domain);
	if ((vt = mono_g_hash_table_lookup (domain->class_vtable_hash, class))) {
		mono_domain_unlock (domain);
		return vt;
	}
	
	if (!class->inited)
		mono_class_init (class);

	mono_stats.used_class_count++;
	mono_stats.class_vtable_size += sizeof (MonoVTable) + class->vtable_size * sizeof (gpointer);

	vt = mono_mempool_alloc0 (domain->mp,  sizeof (MonoVTable) + 
				  class->vtable_size * sizeof (gpointer));
	vt->klass = class;
	vt->domain = domain;

#if CREATION_SPEEDUP
	mono_class_compute_gc_descriptor (class);
	vt->gc_descr = class->gc_descr;
#endif

	if (class->class_size) {
#if HAVE_BOEHM_GC
		vt->data = GC_MALLOC (class->class_size + 8);
		/*vt->data = GC_debug_malloc (class->class_size + 8, class->name, 2);*/
		/*GC_register_finalizer (vt->data, vtable_finalizer, class->name, NULL, NULL);*/
		mono_g_hash_table_insert (domain->static_data_hash, class, vt->data);
#else
		vt->data = mono_mempool_alloc0 (domain->mp, class->class_size + 8);
		
#endif
		mono_stats.class_static_data_size += class->class_size + 8;
	}

	for (i = class->field.first; i < class->field.last; ++i) {
		field = &class->fields [i - class->field.first];
		if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC))
			continue;
		if (!(field->type->attrs & FIELD_ATTRIBUTE_LITERAL)) {
			if (field_is_thread_static (class, field)) {
				guint32 size, align, offset;
				size = mono_type_size (field->type, &align);
				offset = mono_threads_alloc_static_data (size, align);
				if (!domain->thread_static_fields)
					domain->thread_static_fields = g_hash_table_new (NULL, NULL);
				g_hash_table_insert (domain->thread_static_fields, field, GUINT_TO_POINTER (offset));
				continue;
			}
		}
		if ((field->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA)) {
			MonoClass *fklass = mono_class_from_mono_type (field->type);
			t = (char*)vt->data + field->offset;
			if (fklass->valuetype) {
				memcpy (t, field->data, mono_class_value_size (fklass, NULL));
			} else {
				/* it's a pointer type: add check */
				g_assert (fklass->byval_arg.type == MONO_TYPE_PTR);
				*t = *(char *)field->data;
			}
			continue;
		}
		if (!(field->type->attrs & FIELD_ATTRIBUTE_HAS_DEFAULT))
			continue;
		p = field->def_value->value;
		len = mono_metadata_decode_blob_size (p, &p);
		t = (char*)vt->data + field->offset;
		/* should we check that the type matches? */
		switch (field->def_value->type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_U1:
		case MONO_TYPE_I1:
			*t = *p;
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_U2:
		case MONO_TYPE_I2: {
			guint16 *val = (guint16*)t;
			*val = read16 (p);
			break;
		}
		case MONO_TYPE_U4:
		case MONO_TYPE_I4: {
			guint32 *val = (guint32*)t;
			*val = read32 (p);
			break;
		}
		case MONO_TYPE_U8:
		case MONO_TYPE_I8: {
			guint64 *val = (guint64*)t;
			*val = read64 (p);
			break;
		}
		case MONO_TYPE_R4: {
			float *val = (float*)t;
			readr4 (p, val);
			break;
		}
		case MONO_TYPE_R8: {
			double *val = (double*)t;
			readr8 (p, val);
			break;
		}
		case MONO_TYPE_STRING: {
			gpointer *val = (gpointer*)t;
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
			gunichar2 *copy = g_malloc (len);
			int j;
			for (j = 0; j < len/2; j++) {
				copy [j] = read16 (p);
				p += 2;
			}
			*val = mono_string_new_utf16 (domain, copy, len/2);
			g_free (copy);
#else
			*val = mono_string_new_utf16 (domain, (const guint16*)p, len/2);
#endif
			break;
		}
		case MONO_TYPE_CLASS:
			/* nothing to do, we malloc0 the data and the value can be 0 only */
			break;
		default:
			g_warning ("type 0x%02x should not be in constant table", field->def_value->type);
		}
	}

	vt->max_interface_id = class->max_interface_id;
	
	vt->interface_offsets = mono_mempool_alloc0 (domain->mp, 
	        sizeof (gpointer) * (class->max_interface_id + 1));

	/* initialize interface offsets */
	for (i = 0; i <= class->max_interface_id; ++i) {
		int slot = class->interface_offsets [i];
		if (slot >= 0)
			vt->interface_offsets [i] = &(vt->vtable [slot]);
	}

	/* 
	 * arch_create_jit_trampoline () can recursively call this function again
	 * because it compiles icall methods right away.
	 */
	mono_g_hash_table_insert (domain->class_vtable_hash, class, vt);
	if (!class->cached_vtable)
		class->cached_vtable = vt;

	/* initialize vtable */
	for (i = 0; i < class->vtable_size; ++i) {
		MonoMethod *cm;
	       
		if ((cm = class->vtable [i]))
			vt->vtable [i] = arch_create_jit_trampoline (cm);
	}

	mono_domain_unlock (domain);

	/* make sure the the parent is initialized */
	if (class->parent)
		mono_class_vtable (domain, class->parent);

	if (class->contextbound)
		vt->remote = 1;
	else
		vt->remote = 0;

	return vt;
}

/**
 * mono_class_proxy_vtable:
 * @domain: the application domain
 * @class: the class to proxy
 *
 * Creates a vtable for transparent proxies. It is basically
 * a copy of the real vtable of @class, but all function pointers invoke
 * the remoting functions, and vtable->klass points to the 
 * transparent proxy class, and not to @class.
 */
MonoVTable *
mono_class_proxy_vtable (MonoDomain *domain, MonoClass *class)
{
	MonoVTable *vt, *pvt;
	int i, j, vtsize, interface_vtsize = 0;
	MonoClass* iclass = NULL;
	MonoClass* k;

	if ((pvt = mono_g_hash_table_lookup (domain->proxy_vtable_hash, class)))
		return pvt;

	if (class->flags & TYPE_ATTRIBUTE_INTERFACE) {
		int method_count;
		iclass = class;
		class = mono_defaults.marshalbyrefobject_class;

		method_count = iclass->method.count;
		for (i = 0; i < iclass->interface_count; i++)
			method_count += iclass->interfaces[i]->method.count;

		interface_vtsize = method_count * sizeof (gpointer);
	}

	vt = mono_class_vtable (domain, class);
	vtsize = sizeof (MonoVTable) + class->vtable_size * sizeof (gpointer);

	mono_stats.class_vtable_size += vtsize + interface_vtsize;

	pvt = mono_mempool_alloc (domain->mp, vtsize + interface_vtsize);
	memcpy (pvt, vt, vtsize);

	pvt->klass = mono_defaults.transparent_proxy_class;

	/* initialize vtable */
	for (i = 0; i < class->vtable_size; ++i) {
		MonoMethod *cm;
		    
		if ((cm = class->vtable [i]))
			pvt->vtable [i] = arch_create_remoting_trampoline (cm);
	}

	if (class->flags & TYPE_ATTRIBUTE_ABSTRACT)
	{
		/* create trampolines for abstract methods */
		for (k = class; k; k = k->parent) {
			for (i = 0; i < k->method.count; i++) {
				int slot = k->methods [i]->slot;
				if (!pvt->vtable [slot]) 
					pvt->vtable [slot] = arch_create_remoting_trampoline (k->methods[i]);
			}
		}
	}

	if (iclass)
	{
		int slot;
		MonoClass* interf;

		pvt->max_interface_id = iclass->max_interface_id;
		
		pvt->interface_offsets = mono_mempool_alloc0 (domain->mp, 
				sizeof (gpointer) * (pvt->max_interface_id + 1));

		/* Create trampolines for the methods of the interfaces */
		slot = class->vtable_size;
		interf = iclass;
		i = -1;
		do {
			pvt->interface_offsets [interf->interface_id] = &pvt->vtable [slot];

			for (j = 0; j < interf->method.count; ++j) {
				MonoMethod *cm = interf->methods [j];
				pvt->vtable [slot + j] = arch_create_remoting_trampoline (cm);
			}
			slot += interf->method.count;
			if (++i < iclass->interface_count) interf = iclass->interfaces[i];
			else interf = NULL;
			
		} while (interf);

		class = iclass;
	}
	else
	{
		pvt->interface_offsets = mono_mempool_alloc0 (domain->mp, 
				sizeof (gpointer) * (pvt->max_interface_id + 1));

		/* initialize interface offsets */
		for (i = 0; i <= class->max_interface_id; ++i) {
			int slot = class->interface_offsets [i];
			if (slot >= 0)
				pvt->interface_offsets [i] = &(pvt->vtable [slot]);
		}
	}

	mono_g_hash_table_insert (domain->proxy_vtable_hash, class, pvt);

	return pvt;
}

/*
 * Retrieve the MonoMethod that would to be called on obj if obj is passed as
 * the instance of a callvirt of method.
 */
MonoMethod*
mono_object_get_virtual_method (MonoObject *obj, MonoMethod *method) {
	MonoClass *klass;
	MonoMethod **vtable;
	gboolean is_proxy;
	MonoMethod *res;

	klass = mono_object_class (obj);
	if (klass == mono_defaults.transparent_proxy_class) {
		klass = ((MonoTransparentProxy *)obj)->klass;
		is_proxy = TRUE;
	} else {
		is_proxy = FALSE;
	}

	if (!is_proxy && ((method->flags & METHOD_ATTRIBUTE_FINAL) || !(method->flags & METHOD_ATTRIBUTE_VIRTUAL)))
			return method;

	vtable = klass->vtable;

	/* check method->slot is a valid index: perform isinstance? */
	if (method->klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
		res = vtable [klass->interface_offsets [method->klass->interface_id] + method->slot];
	} else {
		res = vtable [method->slot];
	}
	g_assert (res);

	if (is_proxy)
		return mono_marshal_get_remoting_invoke (res);
	
	return res;
}

static MonoObject*
dummy_mono_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc)
{
	g_error ("runtime invoke called on uninitialized runtime");
	return NULL;
}

static MonoInvokeFunc default_mono_runtime_invoke = dummy_mono_runtime_invoke;

MonoObject*
mono_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc)
{
	return default_mono_runtime_invoke (method, obj, params, exc);
}

static void
set_value (MonoType *type, void *dest, void *value, int deref_pointer) {
	int t;
	if (type->byref) {
		gpointer *p = (gpointer*)dest;
		*p = value;
		return;
	}
	t = type->type;
handle_enum:
	switch (t) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1: {
		guint8 *p = (guint8*)dest;
		*p = *(guint8*)value;
		return;
	}
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR: {
		guint16 *p = (guint16*)dest;
		*p = *(guint16*)value;
		return;
	}
#if SIZEOF_VOID_P == 4
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
	case MONO_TYPE_I4:
	case MONO_TYPE_U4: {
		gint32 *p = (gint32*)dest;
		*p = *(gint32*)value;
		return;
	}
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
	case MONO_TYPE_I8:
	case MONO_TYPE_U8: {
		gint64 *p = (gint64*)dest;
		*p = *(gint64*)value;
		return;
	}
	case MONO_TYPE_R4: {
		float *p = (float*)dest;
		*p = *(float*)value;
		return;
	}
	case MONO_TYPE_R8: {
		double *p = (double*)dest;
		*p = *(double*)value;
		return;
	}
	case MONO_TYPE_STRING:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_PTR: {
		gpointer *p = (gpointer*)dest;
		*p = deref_pointer? *(gpointer*)value: value;
		return;
	}
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			t = type->data.klass->enum_basetype->type;
			goto handle_enum;
		} else {
			int size;
			size = mono_class_value_size (type->data.klass, NULL);
			memcpy (dest, value, size);
		}
		return;
	default:
		g_warning ("got type %x", type->type);
		g_assert_not_reached ();
	}
}

void
mono_field_set_value (MonoObject *obj, MonoClassField *field, void *value)
{
	void *dest;

	g_return_if_fail (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC));

	dest = (char*)obj + field->offset;
	set_value (field->type, dest, value, FALSE);
}

void
mono_field_static_set_value (MonoVTable *vt, MonoClassField *field, void *value)
{
	void *dest;

	g_return_if_fail (field->type->attrs & FIELD_ATTRIBUTE_STATIC);

	dest = (char*)vt->data + field->offset;
	set_value (field->type, dest, value, FALSE);
}

void
mono_field_get_value (MonoObject *obj, MonoClassField *field, void *value)
{
	void *src;

	g_return_if_fail (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC));

	src = (char*)obj + field->offset;
	set_value (field->type, value, src, TRUE);
}

void
mono_field_static_get_value (MonoVTable *vt, MonoClassField *field, void *value)
{
	void *src;

	g_return_if_fail (field->type->attrs & FIELD_ATTRIBUTE_STATIC);

	src = (char*)vt->data + field->offset;
	set_value (field->type, value, src, TRUE);
}

void
mono_property_set_value (MonoProperty *prop, void *obj, void **params, MonoObject **exc)
{
	default_mono_runtime_invoke (prop->set, obj, params, exc);
}

MonoObject*
mono_property_get_value (MonoProperty *prop, void *obj, void **params, MonoObject **exc)
{
	return default_mono_runtime_invoke (prop->get, obj, params, exc);
}


MonoMethod *
mono_get_delegate_invoke (MonoClass *klass)
{
	MonoMethod *im;
	int i;

	im = NULL;

	for (i = 0; i < klass->method.count; ++i) {
		if (klass->methods [i]->name[0] == 'I' && 
		    !strcmp ("Invoke", klass->methods [i]->name)) {
			im = klass->methods [i];
		}
	}

	g_assert (im);

	return im;
}

MonoObject*
mono_runtime_delegate_invoke (MonoObject *delegate, void **params, MonoObject **exc)
{
	MonoMethod *im;

	im = mono_get_delegate_invoke (delegate->vtable->klass);
	g_assert (im);

	return mono_runtime_invoke (im, delegate, params, exc);
}

static MonoArray* main_args;

MonoArray*
mono_runtime_get_main_args (void)
{
	return main_args;
}

/*
 * Execute a standard Main() method (argc/argv contains the
 * executable name). This method also sets the command line argument value
 * needed by System.Environment.
 */
int
mono_runtime_run_main (MonoMethod *method, int argc, char* argv[],
		       MonoObject **exc)
{
	int i;
	MonoArray *args = NULL;
	MonoDomain *domain = mono_domain_get ();

	main_args = (MonoArray*)mono_array_new (domain, mono_defaults.string_class, argc);

	if (!g_path_is_absolute (argv [0])) {
		gchar *basename = g_path_get_basename (argv [0]);
		gchar *fullpath = g_build_filename (method->klass->image->assembly->basedir,
						    basename,
						    NULL);
		
		mono_array_set (main_args, gpointer, 0, mono_string_new (domain, fullpath));
		g_free (fullpath);
		g_free (basename);
	} else {
		mono_array_set (main_args, gpointer, 0, mono_string_new (domain, argv [0]));
	}

	for (i = 1; i < argc; ++i) {
		MonoString *arg = mono_string_new (domain, argv [i]);
		mono_array_set (main_args, gpointer, i, arg);
	}
	argc--;
	argv++;
	if (method->signature->param_count) {
		args = (MonoArray*)mono_array_new (domain, mono_defaults.string_class, argc);
		for (i = 0; i < argc; ++i) {
			MonoString *arg = mono_string_new (domain, argv [i]);
			mono_array_set (args, gpointer, i, arg);
		}
	} else {
		args = (MonoArray*)mono_array_new (domain, mono_defaults.string_class, 0);
	}
	
	mono_assembly_set_main (method->klass->image->assembly);

	return mono_runtime_exec_main (method, args, exc);
}

/* Used in mono_unhandled_exception */
static MonoObject *
create_unhandled_exception_eventargs (MonoObject *exc)
{
	MonoClass *klass;
	gpointer args [2];
	MonoMethod *method;
	MonoBoolean is_terminating = TRUE;
	MonoObject *obj;
	gint i;

	klass = mono_class_from_name (mono_defaults.corlib, "System", "UnhandledExceptionEventArgs");
	g_assert (klass);

	mono_class_init (klass);

	/* UnhandledExceptionEventArgs only has 1 public ctor with 2 args */
	for (i = 0; i < klass->method.count; ++i) {
		method = klass->methods [i];
		if (!strcmp (".ctor", method->name) &&
		    method->signature->param_count == 2 &&
		    method->flags & METHOD_ATTRIBUTE_PUBLIC)
			break;
		method = NULL;
	}

	g_assert (method);

	args [0] = exc;
	args [1] = &is_terminating;

	obj = mono_object_new (mono_domain_get (), klass);
	mono_runtime_invoke (method, obj, args, NULL);

	return obj;
}

/*
 * We call this function when we detect an unhandled exception
 * in the default domain.
 * It invokes the * UnhandledException event in AppDomain or prints
 * a warning to the console 
 */
void
mono_unhandled_exception (MonoObject *exc)
{
	MonoDomain *domain = mono_domain_get ();
	MonoClassField *field;
	MonoObject *delegate;
	
	field=mono_class_get_field_from_name(mono_defaults.appdomain_class, 
					     "UnhandledException");
	g_assert (field);

	if (exc->vtable->klass != mono_defaults.threadabortexception_class) {
		delegate = *(MonoObject **)(((char *)domain->domain) + field->offset); 

		/* set exitcode only in the main thread? */
		mono_environment_exitcode_set (1);
		if (domain != mono_root_domain || !delegate) {
			mono_print_unhandled_exception (exc);
		} else {
			MonoObject *e = NULL;
			gpointer pa [2];

			pa [0] = domain->domain;
			pa [1] = create_unhandled_exception_eventargs (exc);
			mono_runtime_delegate_invoke (delegate, pa, &e);
			
			if (e) {
				gchar *msg = mono_string_to_utf8 (((MonoException *) e)->message);
				g_warning ("exception inside UnhandledException handler: %s\n", msg);
				g_free (msg);
			}
		}
	}
}

/*
 * Launch a new thread to start all setup that requires managed code
 * to be executed.
 *
 * main_func is called back from the thread with main_args as the
 * parameter.  The callback function is expected to start Main()
 * eventually.  This function then waits for all managed threads to
 * finish.
 */
void
mono_runtime_exec_managed_code (MonoDomain *domain,
				MonoMainThreadFunc main_func,
				gpointer main_args)
{
	mono_thread_create (domain, main_func, main_args);

	mono_thread_manage ();
}

/*
 * Execute a standard Main() method (args doesn't contain the
 * executable name).
 */
int
mono_runtime_exec_main (MonoMethod *method, MonoArray *args, MonoObject **exc)
{
	MonoDomain *domain;
	gpointer pa [1];
	int rval;

	g_assert (args);

	pa [0] = args;

	domain = mono_object_domain (args);
	if (!domain->entry_assembly) {
		domain->entry_assembly = method->klass->image->assembly;
		ves_icall_System_AppDomainSetup_InitAppDomainSetup (domain->setup);
	}

	/* FIXME: check signature of method */
	if (method->signature->ret->type == MONO_TYPE_I4) {
		MonoObject *res;
		res = mono_runtime_invoke (method, NULL, pa, exc);
		if (!exc || !*exc)
			rval = *(guint32 *)((char *)res + sizeof (MonoObject));
		else
			rval = -1;

		mono_environment_exitcode_set (rval);
	} else {
		mono_runtime_invoke (method, NULL, pa, exc);
		if (!exc || !*exc)
			rval = 0;
		else {
			/* If the return type of Main is void, only
			 * set the exitcode if an exception was thrown
			 * (we don't want to blow away an
			 * explicitly-set exit code)
			 */
			rval = -1;
			mono_environment_exitcode_set (rval);
		}
	}

	return rval;
}

void
mono_install_runtime_invoke (MonoInvokeFunc func)
{
	default_mono_runtime_invoke = func ? func: dummy_mono_runtime_invoke;
}

MonoObject*
mono_runtime_invoke_array (MonoMethod *method, void *obj, MonoArray *params,
			   MonoObject **exc)
{
	MonoMethodSignature *sig = method->signature;
	gpointer *pa = NULL;
	int i;
		
	if (NULL != params) {
		pa = alloca (sizeof (gpointer) * mono_array_length (params));
		for (i = 0; i < mono_array_length (params); i++) {
			if (sig->params [i]->byref) {
				/* nothing to do */
			}

			switch (sig->params [i]->type) {
			case MONO_TYPE_U1:
			case MONO_TYPE_I1:
			case MONO_TYPE_BOOLEAN:
			case MONO_TYPE_U2:
			case MONO_TYPE_I2:
			case MONO_TYPE_CHAR:
			case MONO_TYPE_U:
			case MONO_TYPE_I:
			case MONO_TYPE_U4:
			case MONO_TYPE_I4:
			case MONO_TYPE_U8:
			case MONO_TYPE_I8:
			case MONO_TYPE_R4:
			case MONO_TYPE_R8:
			case MONO_TYPE_VALUETYPE:
				pa [i] = (char *)(((gpointer *)params->vector)[i]) + sizeof (MonoObject);
				break;
			case MONO_TYPE_STRING:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_CLASS:
			case MONO_TYPE_ARRAY:
			case MONO_TYPE_SZARRAY:
				if (sig->params [i]->byref)
					pa [i] = &(((gpointer *)params->vector)[i]);
				else
					pa [i] = (char *)(((gpointer *)params->vector)[i]);
				break;
			default:
				g_error ("type 0x%x not handled in ves_icall_InternalInvoke", sig->params [i]->type);
			}
		}
	}

	if (!strcmp (method->name, ".ctor") && method->klass != mono_defaults.string_class) {
		if (!obj) {
			obj = mono_object_new (mono_domain_get (), method->klass);
			if (mono_object_class(obj) == mono_defaults.transparent_proxy_class) {
				method = mono_marshal_get_remoting_invoke (method->klass->vtable [method->slot]);
			}
		}
		mono_runtime_invoke (method, obj, pa, exc);
		return obj;
	} else
		return mono_runtime_invoke (method, obj, pa, exc);
}

G_GNUC_NORETURN static void
out_of_memory (size_t size)
{
	/* 
	 * we could allocate at program startup some memory that we could release 
	 * back to the system at this point if we're really low on memory (ie, size is
	 * lower than the memory we set apart)
	 */
	MonoException * ex = mono_exception_from_name (mono_defaults.corlib, "System", "OutOfMemoryException");
	mono_raise_exception (ex);
}

/**
 * mono_object_allocate:
 * @size: number of bytes to allocate
 *
 * This is a very simplistic routine until we have our GC-aware
 * memory allocator. 
 *
 * Returns: an allocated object of size @size, or NULL on failure.
 */
static void *
mono_object_allocate (size_t size)
{
#if HAVE_BOEHM_GC
	/* if this is changed to GC_debug_malloc(), we need to change also metadata/gc.c */
	void *o = GC_MALLOC (size);
#else
	void *o = calloc (1, size);
#endif
	mono_stats.new_object_count++;

	if (!o)
		out_of_memory (size);
	return o;
}

#if CREATION_SPEEDUP
static void *
mono_object_allocate_spec (size_t size, void *gcdescr)
{
	/* if this is changed to GC_debug_malloc(), we need to change also metadata/gc.c */
	void *o = GC_GCJ_MALLOC (size, gcdescr);
	mono_stats.new_object_count++;

	if (!o)
		out_of_memory (size);
	return o;
}
#endif

/**
 * mono_object_free:
 *
 * Frees the memory used by the object.  Debugging purposes
 * only, as we will have our GC system.
 */
void
mono_object_free (MonoObject *o)
{
#if HAVE_BOEHM_GC
	g_error ("mono_object_free called with boehm gc.");
#else
	MonoClass *c = o->vtable->klass;
	
	memset (o, 0, c->instance_size);
	free (o);
#endif
}

/**
 * mono_object_new:
 * @klass: the class of the object that we want to create
 *
 * Returns: A newly created object whose definition is
 * looked up using @klass
 */
MonoObject *
mono_object_new (MonoDomain *domain, MonoClass *klass)
{
	MONO_ARCH_SAVE_REGS;
	return mono_object_new_specific (mono_class_vtable (domain, klass));
}

/**
 * mono_object_new_specific:
 * @vtable: the vtable of the object that we want to create
 *
 * Returns: A newly created object with class and domain specified
 * by @vtable
 */
MonoObject *
mono_object_new_specific (MonoVTable *vtable)
{
	MonoObject *o;

	MONO_ARCH_SAVE_REGS;
	
	if (vtable->remote)
	{
		gpointer pa [1];
		MonoMethod *im = vtable->domain->create_proxy_for_type_method;

		if (im == NULL) {
			MonoClass *klass = mono_class_from_name (mono_defaults.corlib, "System.Runtime.Remoting.Activation", "ActivationServices");
			int i;

			if (!klass->inited)
				mono_class_init (klass);

			for (i = 0; i < klass->method.count; ++i) {
				if (!strcmp ("CreateProxyForType", klass->methods [i]->name) &&
					klass->methods [i]->signature->param_count == 1) {
					im = klass->methods [i];
					break;
				}
			}
			g_assert (im);
			vtable->domain->create_proxy_for_type_method = im;
		}
	
		pa [0] = mono_type_get_object (mono_domain_get (), &vtable->klass->byval_arg);

		o = mono_runtime_invoke (im, NULL, pa, NULL);		
		if (o != NULL) return o;
	}

	return mono_object_new_alloc_specific (vtable);
}

MonoObject *
mono_object_new_alloc_specific (MonoVTable *vtable)
{
	MonoObject *o;

#if CREATION_SPEEDUP
	if (vtable->gc_descr != GC_NO_DESCRIPTOR) {
		o = mono_object_allocate_spec (vtable->klass->instance_size, vtable);
	} else {
//		printf("OBJECT: %s.%s.\n", vtable->klass->name_space, vtable->klass->name);
		o = mono_object_allocate (vtable->klass->instance_size);
		o->vtable = vtable;
	}
#else
	o = mono_object_allocate (vtable->klass->instance_size);
	o->vtable = vtable;
#endif
	if (vtable->klass->has_finalize)
		mono_object_register_finalizer (o);
	
	mono_profiler_allocation (o, vtable->klass);
	return o;
}

/**
 * mono_object_new_from_token:
 * @image: Context where the type_token is hosted
 * @token: a token of the type that we want to create
 *
 * Returns: A newly created object whose definition is
 * looked up using @token in the @image image
 */
MonoObject *
mono_object_new_from_token  (MonoDomain *domain, MonoImage *image, guint32 token)
{
	MonoClass *class;

	class = mono_class_get (image, token);

	return mono_object_new (domain, class);
}


/**
 * mono_object_clone:
 * @obj: the object to clone
 *
 * Returns: A newly created object who is a shallow copy of @obj
 */
MonoObject *
mono_object_clone (MonoObject *obj)
{
	MonoObject *o;
	int size;

	size = obj->vtable->klass->instance_size;
	o = mono_object_allocate (size);
	mono_profiler_allocation (o, obj->vtable->klass);

	memcpy (o, obj, size);

	if (obj->vtable->klass->has_finalize)
		mono_object_register_finalizer (o);
	return o;
}

/**
 * mono_array_clone:
 * @array: the array to clone
 *
 * Returns: A newly created array who is a shallow copy of @array
 */
MonoArray*
mono_array_clone (MonoArray *array)
{
	MonoArray *o;
	int size, i;
	guint32 *sizes;
	MonoClass *klass = array->obj.vtable->klass;

	MONO_ARCH_SAVE_REGS;

	if (array->bounds == NULL) {
		size = mono_array_length (array);
		o = mono_array_new_full (((MonoObject *)array)->vtable->domain,
					 klass, &size, NULL);

		size *= mono_array_element_size (klass);
		memcpy (o, array, sizeof (MonoArray) + size);

		return o;
	}
	
	sizes = alloca (klass->rank * sizeof(guint32) * 2);
	size = mono_array_element_size (klass);
	for (i = 0; i < klass->rank; ++i) {
		sizes [i] = array->bounds [i].length;
		size *= array->bounds [i].length;
		sizes [i + klass->rank] = array->bounds [i].lower_bound;
	}
	o = mono_array_new_full (((MonoObject *)array)->vtable->domain, 
				 klass, sizes, sizes + klass->rank);
	memcpy (o, array, sizeof(MonoArray) + size);

	return o;
}

/* helper macros to check for overflow when calculating the size of arrays */
#define MYGUINT32_MAX 4294967295U
#define CHECK_ADD_OVERFLOW_UN(a,b) \
        (guint32)(MYGUINT32_MAX) - (guint32)(b) < (guint32)(a) ? -1 : 0
#define CHECK_MUL_OVERFLOW_UN(a,b) \
        ((guint32)(a) == 0) || ((guint32)(b) == 0) ? 0 : \
        (guint32)(b) > ((MYGUINT32_MAX) / (guint32)(a))

/*
 * mono_array_new_full:
 * @domain: domain where the object is created
 * @array_class: array class
 * @lengths: lengths for each dimension in the array
 * @lower_bounds: lower bounds for each dimension in the array (may be NULL)
 *
 * This routine creates a new array objects with the given dimensions,
 * lower bounds and type.
 */
MonoArray*
mono_array_new_full (MonoDomain *domain, MonoClass *array_class, 
		     guint32 *lengths, guint32 *lower_bounds)
{
	guint32 byte_len, len;
	MonoObject *o;
	MonoArray *array;
	MonoArrayBounds *bounds;
	MonoVTable *vtable;
	int i;

	if (!array_class->inited)
		mono_class_init (array_class);

	byte_len = mono_array_element_size (array_class);
	len = 1;

	if (array_class->rank == 1 &&
	    (lower_bounds == NULL || lower_bounds [0] == 0)) {
		bounds = NULL;
		len = lengths [0];
	} else {
	#if HAVE_BOEHM_GC
		bounds = GC_MALLOC (sizeof (MonoArrayBounds) * array_class->rank);
	#else
		bounds = g_malloc0 (sizeof (MonoArrayBounds) * array_class->rank);
	#endif
		for (i = 0; i < array_class->rank; ++i) {
			bounds [i].length = lengths [i];
			if (CHECK_MUL_OVERFLOW_UN (len, lengths [i]))
				out_of_memory (MYGUINT32_MAX);
			len *= lengths [i];
		}

		if (lower_bounds)
			for (i = 0; i < array_class->rank; ++i)
				bounds [i].lower_bound = lower_bounds [i];
	}

	if (CHECK_MUL_OVERFLOW_UN (byte_len, len))
		out_of_memory (MYGUINT32_MAX);
	byte_len *= len;
	if (CHECK_ADD_OVERFLOW_UN (byte_len, sizeof (MonoArray)))
		out_of_memory (MYGUINT32_MAX);
	byte_len += sizeof (MonoArray);
	/* 
	 * Following three lines almost taken from mono_object_new ():
	 * they need to be kept in sync.
	 */
	vtable = mono_class_vtable (domain, array_class);
#if CREATION_SPEEDUP
	if (vtable->gc_descr != GC_NO_DESCRIPTOR)
		o = mono_object_allocate_spec (byte_len, vtable);
	else {
		o = mono_object_allocate (byte_len);
		o->vtable = vtable;
	}
#else
	o = mono_object_allocate (byte_len);
	o->vtable = vtable;
#endif

	array = (MonoArray*)o;

	array->bounds = bounds;
	array->max_length = len;

	mono_profiler_allocation (o, array_class);

	return array;
}

/*
 * mono_array_new:
 * @domain: domain where the object is created
 * @eclass: element class
 * @n: number of array elements
 *
 * This routine creates a new szarray with @n elements of type @eclass.
 */
MonoArray *
mono_array_new (MonoDomain *domain, MonoClass *eclass, guint32 n)
{
	MonoClass *ac;

	MONO_ARCH_SAVE_REGS;

	ac = mono_array_class_get (&eclass->byval_arg, 1);
	g_assert (ac != NULL);

	return mono_array_new_specific (mono_class_vtable (domain, ac), n);
}

/*
 * mono_array_new_specific:
 * @vtable: a vtable in the appropriate domain for an initialized class
 * @n: number of array elements
 *
 * This routine is a fast alternative to mono_array_new() for code which
 * can be sure about the domain it operates in.
 */
MonoArray *
mono_array_new_specific (MonoVTable *vtable, guint32 n)
{
	MonoObject *o;
	MonoArray *ao;
	guint32 byte_len, elem_size;

	MONO_ARCH_SAVE_REGS;

	elem_size = mono_array_element_size (vtable->klass);
	if (CHECK_MUL_OVERFLOW_UN (n, elem_size))
		out_of_memory (MYGUINT32_MAX);
	byte_len = n * elem_size;
	if (CHECK_ADD_OVERFLOW_UN (byte_len, sizeof (MonoArray)))
		out_of_memory (MYGUINT32_MAX);
	byte_len += sizeof (MonoArray);
#if CREATION_SPEEDUP
	if (vtable->gc_descr != GC_NO_DESCRIPTOR) {
		o = mono_object_allocate_spec (byte_len, vtable);
	} else {
//		printf("ARRAY: %s.%s.\n", vtable->klass->name_space, vtable->klass->name);
		o = mono_object_allocate (byte_len);
		o->vtable = vtable;
	}
#else
	o = mono_object_allocate (byte_len);
	o->vtable = vtable;
#endif

	ao = (MonoArray *)o;
	ao->bounds = NULL;
	ao->max_length = n;
	mono_profiler_allocation (o, vtable->klass);

	return ao;
}

/**
 * mono_string_new_utf16:
 * @text: a pointer to an utf16 string
 * @len: the length of the string
 *
 * Returns: A newly created string object which contains @text.
 */
MonoString *
mono_string_new_utf16 (MonoDomain *domain, const guint16 *text, gint32 len)
{
	MonoString *s;
	
	s = mono_string_new_size (domain, len);
	g_assert (s != NULL);

	memcpy (mono_string_chars (s), text, len * 2);

	return s;
}

/**
 * mono_string_new_size:
 * @text: a pointer to an utf16 string
 * @len: the length of the string
 *
 * Returns: A newly created string object of @len
 */
MonoString *
mono_string_new_size (MonoDomain *domain, gint32 len)
{
	MonoString *s;
	MonoVTable *vtable;

	vtable = mono_class_vtable (domain, mono_defaults.string_class);

#if CREATION_SPEEDUP
	s = mono_object_allocate_spec (sizeof (MonoString) + ((len + 1) * 2), vtable);
#else
	s = (MonoString*)mono_object_allocate (sizeof (MonoString) + ((len + 1) * 2));
	s->object.vtable = vtable;
#endif

	s->length = len;
	mono_profiler_allocation ((MonoObject*)s, mono_defaults.string_class);

	return s;
}

/*
 * mono_string_new_len:
 * @text: a pointer to an utf8 string
 * @length: number of bytes in @text to consider
 *
 * Returns: A newly created string object which contains @text.
 */
MonoString*
mono_string_new_len (MonoDomain *domain, const char *text, guint length)
{
	GError *error = NULL;
	MonoString *o = NULL;
	guint16 *ut;
	glong items_written;

	ut = g_utf8_to_utf16 (text, length, NULL, &items_written, &error);

	if (!error)
		o = mono_string_new_utf16 (domain, ut, items_written);
	else 
		g_error_free (error);

	g_free (ut);

	return o;
}

/**
 * mono_string_new:
 * @text: a pointer to an utf8 string
 *
 * Returns: A newly created string object which contains @text.
 */
MonoString*
mono_string_new (MonoDomain *domain, const char *text)
{
	GError *error = NULL;
	MonoString *o = NULL;
	guint16 *ut;
	glong items_written;
	int l;

	l = strlen (text);
	
	ut = g_utf8_to_utf16 (text, l, NULL, &items_written, &error);

	if (!error)
		o = mono_string_new_utf16 (domain, ut, items_written);
	else 
		g_error_free (error);

	g_free (ut);

	return o;
}

/*
 * mono_string_new_wrapper:
 * @text: pointer to utf8 characters.
 *
 * Helper function to create a string object from @text in the current domain.
 */
MonoString*
mono_string_new_wrapper (const char *text)
{
	MonoDomain *domain = mono_domain_get ();

	MONO_ARCH_SAVE_REGS;

	if (text)
		return mono_string_new (domain, text);

	return NULL;
}

/**
 * mono_value_box:
 * @class: the class of the value
 * @value: a pointer to the unboxed data
 *
 * Returns: A newly created object which contains @value.
 */
MonoObject *
mono_value_box (MonoDomain *domain, MonoClass *class, gpointer value)
{
	MonoObject *res;
	int size;
	MonoVTable *vtable;

	g_assert (class->valuetype);

	vtable = mono_class_vtable (domain, class);
	size = mono_class_instance_size (class);
	res = mono_object_allocate (size);
	res->vtable = vtable;
	mono_profiler_allocation (res, class);

	size = size - sizeof (MonoObject);

#if NO_UNALIGNED_ACCESS
	memcpy ((char *)res + sizeof (MonoObject), value, size);
#else
	switch (size) {
	case 1:
		*((guint8 *) res + sizeof (MonoObject)) = *(guint8 *) value;
		break;
	case 2:
		*(guint16 *)((guint8 *) res + sizeof (MonoObject)) = *(guint16 *) value;
		break;
	case 4:
		*(guint32 *)((guint8 *) res + sizeof (MonoObject)) = *(guint32 *) value;
		break;
	case 8:
		*(guint64 *)((guint8 *) res + sizeof (MonoObject)) = *(guint64 *) value;
		break;
	default:
		memcpy ((char *)res + sizeof (MonoObject), value, size);
	}
#endif
	if (class->has_finalize)
		mono_object_register_finalizer (res);
	return res;
}

gpointer
mono_object_unbox (MonoObject *obj)
{
	/* add assert for valuetypes? */
	return ((char*)obj) + sizeof (MonoObject);
}

/**
 * mono_object_isinst:
 * @obj: an object
 * @klass: a pointer to a class 
 *
 * Returns: @obj if @obj is derived from @klass
 */
MonoObject *
mono_object_isinst (MonoObject *obj, MonoClass *klass)
{
	MonoVTable *vt;
	MonoClass *oklass;

	if (!obj)
		return NULL;

	vt = obj->vtable;
	oklass = vt->klass;

	if (!klass->inited)
		mono_class_init (klass);

	if (klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
		if ((klass->interface_id <= vt->max_interface_id) &&
		    vt->interface_offsets [klass->interface_id])
			return obj;
	} else {
		if (oklass == mono_defaults.transparent_proxy_class) {
			/* fixme: add check for IRemotingTypeInfo */
			oklass = ((MonoTransparentProxy *)obj)->klass;
		}
		if (klass->rank) {
			if (oklass->rank == klass->rank && mono_class_has_parent (oklass->cast_class, klass->cast_class))
				return obj;
			
		} else if (mono_class_has_parent (oklass, klass))
			return obj;
	}

	return NULL;
}

typedef struct {
	MonoDomain *orig_domain;
	char *ins;
	MonoString *res;
} LDStrInfo;

static void
str_lookup (MonoDomain *domain, gpointer user_data)
{
	LDStrInfo *info = user_data;
	if (info->res || domain == info->orig_domain)
		return;
	mono_domain_lock (domain);
	info->res = mono_g_hash_table_lookup (domain->ldstr_table, info->ins);
	mono_domain_unlock (domain);
}

static MonoString*
mono_string_is_interned_lookup (MonoString *str, int insert)
{
	MonoGHashTable *ldstr_table;
	MonoString *res;
	MonoDomain *domain;
	char *ins = g_malloc (4 + str->length * 2);
	char *p;
	int bloblen;
	
	/* Encode the length */
	p = ins;
	mono_metadata_encode_value (2 * str->length, p, &p);
	bloblen = p - ins;
	p = ins;
	mono_metadata_encode_value (bloblen + 2 * str->length, p, &p);
	bloblen = (p - ins) + 2 * str->length;
	/*
	 * ins is stored in the hash table as a key and needs to have the same
	 * representation as in the metadata: we swap the character bytes on big
	 * endian boxes.
	 */
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	{
		int i;
		char *p2 = mono_string_chars (str);
		for (i = 0; i < str->length; ++i) {
			*p++ = p2 [1];
			*p++ = p2 [0];
			p2 += 2;
		}
	}
#else
	memcpy (p, mono_string_chars (str), str->length * 2);
#endif
	domain = ((MonoObject *)str)->vtable->domain;
	ldstr_table = domain->ldstr_table;
	mono_domain_lock (domain);
	if ((res = mono_g_hash_table_lookup (ldstr_table, ins))) {
		mono_domain_unlock (domain);
		g_free (ins);
		return res;
	}
	if (insert) {
		mono_g_hash_table_insert (ldstr_table, ins, str);
		mono_domain_unlock (domain);
		return str;
	} else {
		LDStrInfo ldstr_info;
		ldstr_info.orig_domain = domain;
		ldstr_info.ins = ins;
		ldstr_info.res = NULL;

		mono_domain_foreach (str_lookup, &ldstr_info);
		if (ldstr_info.res) {
			/* 
			 * the string was already interned in some other domain:
			 * intern it in the current one as well.
			 */
			mono_g_hash_table_insert (ldstr_table, ins, str);
			mono_domain_unlock (domain);
			return str;
		}
	}
	mono_domain_unlock (domain);
	g_free (ins);
	return NULL;
}

MonoString*
mono_string_is_interned (MonoString *o)
{
	return mono_string_is_interned_lookup (o, FALSE);
}

MonoString*
mono_string_intern (MonoString *str)
{
	return mono_string_is_interned_lookup (str, TRUE);
}

/*
 * mono_ldstr:
 * @domain: the domain where the string will be used.
 * @image: a metadata context
 * @idx: index into the user string table.
 * 
 * Implementation for the ldstr opcode.
 */
MonoString*
mono_ldstr (MonoDomain *domain, MonoImage *image, guint32 idx)
{
	const char *str, *sig;
	MonoString *o;
	size_t len2;

	MONO_ARCH_SAVE_REGS;

	if (image->assembly->dynamic)
		return mono_lookup_dynamic_token (image, MONO_TOKEN_STRING | idx);
	else
		sig = str = mono_metadata_user_string (image, idx);

	mono_domain_lock (domain);
	if ((o = mono_g_hash_table_lookup (domain->ldstr_table, sig))) {
		mono_domain_unlock (domain);
		return o;
	}
	
	len2 = mono_metadata_decode_blob_size (str, &str);
	len2 >>= 1;

	o = mono_string_new_utf16 (domain, (guint16*)str, len2);
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	{
		int i;
		guint16 *p2 = (guint16*)mono_string_chars (o);
		for (i = 0; i < len2; ++i) {
			*p2 = GUINT16_FROM_LE (*p2);
			++p2;
		}
	}
#endif
	mono_g_hash_table_insert (domain->ldstr_table, (gpointer)sig, o);
	mono_domain_unlock (domain);

	return o;
}

/*
 * mono_string_to_utf8:
 * @s: a System.String
 *
 * Return the UTF8 representation for @s.
 * the resulting buffer nedds to be freed with g_free().
 */
char *
mono_string_to_utf8 (MonoString *s)
{
	char *as;
	GError *error = NULL;

	if (s == NULL)
		return NULL;

	if (!s->length)
		return g_strdup ("");

	as = g_utf16_to_utf8 (mono_string_chars (s), s->length, NULL, NULL, &error);
	if (error) {
		g_warning (error->message);
		g_error_free (error);
	}

	return as;
}

/*
 * mono_string_to_utf16:
 * @s: a MonoString
 *
 * Return an null-terminated array of the utf-16 chars
 * contained in @s. The result must be freed with g_free().
 * This is a temporary helper until our string implementation
 * is reworked to always include the null terminating char.
 */
gunichar2 *
mono_string_to_utf16 (MonoString *s)
{
	char *as;

	if (s == NULL)
		return NULL;

	as = g_malloc ((s->length * 2) + 2);
	as [(s->length * 2)] = '\0';
	as [(s->length * 2) + 1] = '\0';

	if (!s->length) {
		return (gunichar2 *)(as);
	}
	
	memcpy (as, mono_string_chars(s), s->length * 2);
	return (gunichar2 *)(as);
}

/*
 * Converts a NULL terminated UTF16 string (LPWSTR) to a MonoString
 */
MonoString *
mono_string_from_utf16 (gunichar2 *data)
{
	MonoDomain *domain = mono_domain_get ();
	int len = 0;

	if (!data)
		return NULL;

	while (data [len]) len++;

	return mono_string_new_utf16 (domain, data, len);
}

G_GNUC_NORETURN static void
default_ex_handler (MonoException *ex)
{
	MonoObject *o = (MonoObject*)ex;
	g_error ("Exception %s.%s raised in C code", o->vtable->klass->name_space, o->vtable->klass->name);
	exit (1);
}

static G_GNUC_NORETURN MonoExceptionFunc ex_handler = default_ex_handler;

void
mono_install_handler        (MonoExceptionFunc func)
{
	ex_handler = func? func: default_ex_handler;
}

/*
 * mono_raise_exception:
 * @ex: exception object
 *
 * Signal the runtime that the exception @ex has been raised in unmanaged code.
 */
G_GNUC_NORETURN void
mono_raise_exception (MonoException *ex) 
{
	ex_handler (ex);
}

MonoWaitHandle *
mono_wait_handle_new (MonoDomain *domain, HANDLE handle)
{
	MonoWaitHandle *res;

	res = (MonoWaitHandle *)mono_object_new (domain, mono_defaults.waithandle_class);

	res->handle = handle;

	return res;
}

MonoAsyncResult *
mono_async_result_new (MonoDomain *domain, HANDLE handle, MonoObject *state, gpointer data)
{
	MonoAsyncResult *res;

	res = (MonoAsyncResult *)mono_object_new (domain, mono_defaults.asyncresult_class);

	res->data = data;
	res->async_state = state;
	res->handle = (MonoObject *)mono_wait_handle_new (domain, handle);
	res->sync_completed = FALSE;
	res->completed = FALSE;

	return res;
}

void
mono_message_init (MonoDomain *domain,
		   MonoMethodMessage *this, 
		   MonoReflectionMethod *method,
		   MonoArray *out_args)
{
	MonoMethodSignature *sig = method->method->signature;
	MonoString *name;
	int i, j;
	char **names;
	guint8 arg_type;

	this->method = method;

	this->args = mono_array_new (domain, mono_defaults.object_class, sig->param_count);
	this->arg_types = mono_array_new (domain, mono_defaults.byte_class, sig->param_count);

	names = g_new (char *, sig->param_count);
	mono_method_get_param_names (method->method, (const char **) names);
	this->names = mono_array_new (domain, mono_defaults.string_class, sig->param_count);
	
	for (i = 0; i < sig->param_count; i++) {
		 name = mono_string_new (domain, names [i]);
		 mono_array_set (this->names, gpointer, i, name);	
	}

	g_free (names);
	
	for (i = 0, j = 0; i < sig->param_count; i++) {

		if (sig->params [i]->byref) {
			if (out_args) {
				gpointer arg = mono_array_get (out_args, gpointer, j);
				mono_array_set (this->args, gpointer, i, arg);
				j++;
			}
			arg_type = 2;
			if (sig->params [i]->attrs & PARAM_ATTRIBUTE_IN)
				arg_type |= 1;
		} else {
			arg_type = 1;
		}

		mono_array_set (this->arg_types, guint8, i, arg_type);
	}
}

/**
 * mono_remoting_invoke:
 * @real_proxy: pointer to a RealProxy object
 * @msg: The MonoMethodMessage to execute
 * @exc: used to store exceptions
 * @out_args: used to store output arguments
 *
 * This is used to call RealProxy::Invoke(). RealProxy::Invoke() returns an
 * IMessage interface and it is not trivial to extract results from there. So
 * we call an helper method PrivateInvoke instead of calling
 * RealProxy::Invoke() directly.
 *
 * Returns: the result object.
 */
MonoObject *
mono_remoting_invoke (MonoObject *real_proxy, MonoMethodMessage *msg, 
		      MonoObject **exc, MonoArray **out_args)
{
	MonoMethod *im = real_proxy->vtable->domain->private_invoke_method;
	gpointer pa [4];

	/*static MonoObject *(*invoke) (gpointer, gpointer, MonoObject **, MonoArray **) = NULL;*/

	if (!im) {
		MonoClass *klass;
		int i;

		klass = mono_defaults.real_proxy_class; 
		       
		for (i = 0; i < klass->method.count; ++i) {
			if (!strcmp ("PrivateInvoke", klass->methods [i]->name) &&
			    klass->methods [i]->signature->param_count == 4) {
				im = klass->methods [i];
				break;
			}
		}
	
		g_assert (im);
		real_proxy->vtable->domain->private_invoke_method = im;
	}

	pa [0] = real_proxy;
	pa [1] = msg;
	pa [2] = exc;
	pa [3] = out_args;

	return mono_runtime_invoke (im, NULL, pa, exc);
}

MonoObject *
mono_message_invoke (MonoObject *target, MonoMethodMessage *msg, 
		     MonoObject **exc, MonoArray **out_args) 
{
	MonoDomain *domain; 
	MonoMethod *method;
	MonoMethodSignature *sig;
	int i, j, outarg_count = 0;

	if (target && target->vtable->klass == mono_defaults.transparent_proxy_class) {

		MonoTransparentProxy* tp = (MonoTransparentProxy *)target;
		if (tp->klass->contextbound && tp->rp->context == (MonoObject *) mono_context_get ()) {
			target = tp->rp->unwrapped_server;
		} else {
			return mono_remoting_invoke ((MonoObject *)tp->rp, msg, exc, out_args);
		}
	}

	domain = mono_domain_get (); 
	method = msg->method->method;
	sig = method->signature;

	for (i = 0; i < sig->param_count; i++) {
		if (sig->params [i]->byref) 
			outarg_count++;
	}

	*out_args = mono_array_new (domain, mono_defaults.object_class, outarg_count);
	*exc = NULL;

	for (i = 0, j = 0; i < sig->param_count; i++) {
		if (sig->params [i]->byref) {
			gpointer arg;
			arg = mono_array_get (msg->args, gpointer, i);
			mono_array_set (*out_args, gpointer, j, arg);
			j++;
		}
	}

	return mono_runtime_invoke_array (method, target, msg->args, exc);
}

void
mono_print_unhandled_exception (MonoObject *exc)
{
	char *message = (char *) "";
	MonoString *str; 
	MonoMethod *method;
	MonoClass *klass;
	gboolean free_message = FALSE;
	gint i;

	if (mono_object_isinst (exc, mono_defaults.exception_class)) {
		klass = exc->vtable->klass;
		method = NULL;
		while (klass && method == NULL) {
			for (i = 0; i < klass->method.count; ++i) {
				method = klass->methods [i];
				if (!strcmp ("ToString", method->name) &&
				    method->signature->param_count == 0 &&
				    method->flags & METHOD_ATTRIBUTE_VIRTUAL &&
				    method->flags & METHOD_ATTRIBUTE_PUBLIC) {
					break;
				}
				method = NULL;
			}
			
			if (method == NULL)
				klass = klass->parent;
		}

		g_assert (method);

		str = (MonoString *) mono_runtime_invoke (method, exc, NULL, NULL);
		if (str) {
			message = mono_string_to_utf8 (str);
			free_message = TRUE;
		}
	}				

	/*
	 * g_printerr ("\nUnhandled Exception: %s.%s: %s\n", exc->vtable->klass->name_space, 
	 *	   exc->vtable->klass->name, message);
	 */
	g_printerr ("\nUnhandled Exception: %s\n", message);
	
	if (free_message)
		g_free (message);
}

/**
 * mono_delegate_ctor:
 * @this: pointer to an uninitialized delegate object
 * @target: target object
 * @addr: pointer to native code
 *
 * This is used to initialize a delegate. We also insert the method_info if
 * we find the info with mono_jit_info_table_find().
 */
void
mono_delegate_ctor (MonoObject *this, MonoObject *target, gpointer addr)
{
	MonoDomain *domain = mono_domain_get ();
	MonoDelegate *delegate = (MonoDelegate *)this;
	MonoMethod *method = NULL;
	MonoClass *class;
	MonoJitInfo *ji;

	g_assert (this);
	g_assert (addr);

	class = this->vtable->klass;

	if ((ji = mono_jit_info_table_find (domain, addr))) {
		method = ji->method;
		delegate->method_info = mono_method_get_object (domain, method, NULL);
	}

	if (target && target->vtable->klass == mono_defaults.transparent_proxy_class) {
		g_assert (method);
		method = mono_marshal_get_remoting_invoke (method);
		delegate->method_ptr = mono_compile_method (method);
		delegate->target = target;
	} else {
		delegate->method_ptr = addr;
		delegate->target = target;
	}
}

/**
 * mono_method_call_message_new:
 *
 * Translates arguments pointers into a Message.
 */
MonoMethodMessage *
mono_method_call_message_new (MonoMethod *method, gpointer *params, MonoMethod *invoke, 
			      MonoDelegate **cb, MonoObject **state)
{
	MonoDomain *domain = mono_domain_get ();
	MonoMethodSignature *sig = method->signature;
	MonoMethodMessage *msg;
	int i, count, type;

	msg = (MonoMethodMessage *)mono_object_new (domain, mono_defaults.mono_method_message_class); 
	
	if (invoke) {
		mono_message_init (domain, msg, mono_method_get_object (domain, invoke, NULL), NULL);
		count =  sig->param_count - 2;
	} else {
		mono_message_init (domain, msg, mono_method_get_object (domain, method, NULL), NULL);
		count =  sig->param_count;
	}

	for (i = 0; i < count; i++) {
		gpointer vpos;
		MonoClass *class;
		MonoObject *arg;

		if (sig->params [i]->byref)
			vpos = *((gpointer *)params [i]);
		else 
			vpos = params [i];

		type = sig->params [i]->type;
		class = mono_class_from_mono_type (sig->params [i]);

		if (class->valuetype)
			arg = mono_value_box (domain, class, vpos);
		else 
			arg = *((MonoObject **)vpos);
		      
		mono_array_set (msg->args, gpointer, i, arg);
	}

	if (invoke) {
		*cb = *((MonoDelegate **)params [i]);
		i++;
		*state = *((MonoObject **)params [i]);
	}

	return msg;
}

/**
 * mono_method_return_message_restore:
 *
 * Restore results from message based processing back to arguments pointers
 */
void
mono_method_return_message_restore (MonoMethod *method, gpointer *params, MonoArray *out_args)
{
	MonoMethodSignature *sig = method->signature;
	int i, j, type, size;
	
	for (i = 0, j = 0; i < sig->param_count; i++) {
		MonoType *pt = sig->params [i];

		size = mono_type_stack_size (pt, NULL);

		if (pt->byref) {
			char *arg = mono_array_get (out_args, gpointer, j);
			type = pt->type;
			
			switch (type) {
			case MONO_TYPE_VOID:
				g_assert_not_reached ();
				break;
			case MONO_TYPE_U1:
			case MONO_TYPE_I1:
			case MONO_TYPE_BOOLEAN:
			case MONO_TYPE_U2:
			case MONO_TYPE_I2:
			case MONO_TYPE_CHAR:
			case MONO_TYPE_U4:
			case MONO_TYPE_I4:
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
			case MONO_TYPE_R4:
			case MONO_TYPE_R8:
			case MONO_TYPE_VALUETYPE: {
				memcpy (*((gpointer *)params [i]), arg + sizeof (MonoObject), size); 
				break;
			}
			case MONO_TYPE_STRING:
			case MONO_TYPE_CLASS: 
			case MONO_TYPE_ARRAY:
			case MONO_TYPE_SZARRAY:
				**((MonoObject ***)params [i]) = (MonoObject *)arg;
				break;
			default:
				g_assert_not_reached ();
			}

			j++;
		}
	}
}

/**
 * mono_load_remote_field:
 * @this: pointer to an object
 * @klass: klass of the object containing @field
 * @field: the field to load
 * @res: a storage to store the result
 *
 * This method is called by the runtime on attempts to load fields of
 * transparent proxy objects. @this points to such TP, @klass is the class of
 * the object containing @field. @res is a storage location which can be
 * used to store the result.
 *
 * Returns: an address pointing to the value of field.
 */
gpointer
mono_load_remote_field (MonoObject *this, MonoClass *klass, MonoClassField *field, gpointer *res)
{
	static MonoMethod *getter = NULL;
	MonoDomain *domain = mono_domain_get ();
	MonoTransparentProxy *tp = (MonoTransparentProxy *) this;
	MonoClass *field_class;
	MonoMethodMessage *msg;
	MonoArray *out_args;
	MonoObject *exc;
	gpointer tmp;

	g_assert (this->vtable->klass == mono_defaults.transparent_proxy_class);

	if (!res)
		res = &tmp;

	if (tp->klass->contextbound && tp->rp->context == (MonoObject *) mono_context_get ()) {
		mono_field_get_value (tp->rp->unwrapped_server, field, res);
		return res;
	}
	
	if (!getter) {
		int i;

		for (i = 0; i < mono_defaults.object_class->method.count; ++i) {
			MonoMethod *cm = mono_defaults.object_class->methods [i];
	       
			if (!strcmp (cm->name, "FieldGetter")) {
				getter = cm;
				break;
			}
		}
		g_assert (getter);
	}
	
	field_class = mono_class_from_mono_type (field->type);

	msg = (MonoMethodMessage *)mono_object_new (domain, mono_defaults.mono_method_message_class);
	out_args = mono_array_new (domain, mono_defaults.object_class, 1);
	mono_message_init (domain, msg, mono_method_get_object (domain, getter, NULL), out_args);

	mono_array_set (msg->args, gpointer, 0, mono_string_new (domain, klass->name));
	mono_array_set (msg->args, gpointer, 1, mono_string_new (domain, field->name));

	mono_remoting_invoke ((MonoObject *)(tp->rp), msg, &exc, &out_args);

	*res = mono_array_get (out_args, MonoObject *, 0);

	if (field_class->valuetype) {
		return ((char *)*res) + sizeof (MonoObject);
	} else
		return res;
}

MonoObject *
mono_load_remote_field_new (MonoObject *this, MonoClass *klass, MonoClassField *field)
{
	static MonoMethod *getter = NULL;
	MonoDomain *domain = mono_domain_get ();
	MonoTransparentProxy *tp = (MonoTransparentProxy *) this;
	MonoClass *field_class;
	MonoMethodMessage *msg;
	MonoArray *out_args;
	MonoObject *exc, *res;

	g_assert (this->vtable->klass == mono_defaults.transparent_proxy_class);

	field_class = mono_class_from_mono_type (field->type);

	if (tp->klass->contextbound && tp->rp->context == (MonoObject *) mono_context_get ()) {
		gpointer val;
		if (field_class->valuetype) {
			res = mono_object_new (domain, field_class);
			val = ((gchar *) res) + sizeof (MonoObject);
		} else {
			val = &res;
		}
		mono_field_get_value (tp->rp->unwrapped_server, field, val);
		return res;
	}

	if (!getter) {
		int i;

		for (i = 0; i < mono_defaults.object_class->method.count; ++i) {
			MonoMethod *cm = mono_defaults.object_class->methods [i];
	       
			if (!strcmp (cm->name, "FieldGetter")) {
				getter = cm;
				break;
			}
		}
		g_assert (getter);
	}
	
	msg = (MonoMethodMessage *)mono_object_new (domain, mono_defaults.mono_method_message_class);
	out_args = mono_array_new (domain, mono_defaults.object_class, 1);
	mono_message_init (domain, msg, mono_method_get_object (domain, getter, NULL), out_args);

	mono_array_set (msg->args, gpointer, 0, mono_string_new (domain, klass->name));
	mono_array_set (msg->args, gpointer, 1, mono_string_new (domain, field->name));

	mono_remoting_invoke ((MonoObject *)(tp->rp), msg, &exc, &out_args);

	res = mono_array_get (out_args, MonoObject *, 0);

	return res;
}

/**
 * mono_store_remote_field:
 * @this: pointer to an object
 * @klass: klass of the object containing @field
 * @field: the field to load
 * @val: the value/object to store
 *
 * This method is called by the runtime on attempts to store fields of
 * transparent proxy objects. @this points to such TP, @klass is the class of
 * the object containing @field. @val is the new value to store in @field.
 */
void
mono_store_remote_field (MonoObject *this, MonoClass *klass, MonoClassField *field, gpointer val)
{
	static MonoMethod *setter = NULL;
	MonoDomain *domain = mono_domain_get ();
	MonoTransparentProxy *tp = (MonoTransparentProxy *) this;
	MonoClass *field_class;
	MonoMethodMessage *msg;
	MonoArray *out_args;
	MonoObject *exc;
	MonoObject *arg;

	g_assert (this->vtable->klass == mono_defaults.transparent_proxy_class);

	field_class = mono_class_from_mono_type (field->type);

	if (tp->klass->contextbound && tp->rp->context == (MonoObject *) mono_context_get ()) {
		if (field_class->valuetype) mono_field_set_value (tp->rp->unwrapped_server, field, val);
		else mono_field_set_value (tp->rp->unwrapped_server, field, *((MonoObject **)val));
		return;
	}

	if (!setter) {
		int i;

		for (i = 0; i < mono_defaults.object_class->method.count; ++i) {
			MonoMethod *cm = mono_defaults.object_class->methods [i];
	       
			if (!strcmp (cm->name, "FieldSetter")) {
				setter = cm;
				break;
			}
		}
		g_assert (setter);
	}

	if (field_class->valuetype)
		arg = mono_value_box (domain, field_class, val);
	else 
		arg = *((MonoObject **)val);
		

	msg = (MonoMethodMessage *)mono_object_new (domain, mono_defaults.mono_method_message_class);
	mono_message_init (domain, msg, mono_method_get_object (domain, setter, NULL), NULL);

	mono_array_set (msg->args, gpointer, 0, mono_string_new (domain, klass->name));
	mono_array_set (msg->args, gpointer, 1, mono_string_new (domain, field->name));
	mono_array_set (msg->args, gpointer, 2, arg);

	mono_remoting_invoke ((MonoObject *)(tp->rp), msg, &exc, &out_args);
}

void
mono_store_remote_field_new (MonoObject *this, MonoClass *klass, MonoClassField *field, MonoObject *arg)
{
	static MonoMethod *setter = NULL;
	MonoDomain *domain = mono_domain_get ();
	MonoTransparentProxy *tp = (MonoTransparentProxy *) this;
	MonoClass *field_class;
	MonoMethodMessage *msg;
	MonoArray *out_args;
	MonoObject *exc;

	g_assert (this->vtable->klass == mono_defaults.transparent_proxy_class);

	field_class = mono_class_from_mono_type (field->type);

	if (tp->klass->contextbound && tp->rp->context == (MonoObject *) mono_context_get ()) {
		if (field_class->valuetype) mono_field_set_value (tp->rp->unwrapped_server, field, ((gchar *) arg) + sizeof (MonoObject));
		else mono_field_set_value (tp->rp->unwrapped_server, field, arg);
		return;
	}

	if (!setter) {
		int i;

		for (i = 0; i < mono_defaults.object_class->method.count; ++i) {
			MonoMethod *cm = mono_defaults.object_class->methods [i];
	       
			if (!strcmp (cm->name, "FieldSetter")) {
				setter = cm;
				break;
			}
		}
		g_assert (setter);
	}

	msg = (MonoMethodMessage *)mono_object_new (domain, mono_defaults.mono_method_message_class);
	mono_message_init (domain, msg, mono_method_get_object (domain, setter, NULL), NULL);

	mono_array_set (msg->args, gpointer, 0, mono_string_new (domain, klass->name));
	mono_array_set (msg->args, gpointer, 1, mono_string_new (domain, field->name));
	mono_array_set (msg->args, gpointer, 2, arg);

	mono_remoting_invoke ((MonoObject *)(tp->rp), msg, &exc, &out_args);
}

