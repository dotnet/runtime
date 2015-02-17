/*
 * icall.c:
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *	 Patrik Torstensson (patrik.torstensson@labs2.com)
 *   Marek Safar (marek.safar@gmail.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011-2014 Xamarin Inc (http://www.xamarin.com).
 */

#include <config.h>
#include <glib.h>
#include <stdarg.h>
#include <string.h>
#include <ctype.h>
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#if defined (HOST_WIN32)
#include <stdlib.h>
#endif
#if defined (HAVE_WCHAR_H)
#include <wchar.h>
#endif

#include "mono/utils/mono-membar.h"
#include <mono/metadata/object.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/threadpool.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/file-io.h>
#include <mono/metadata/console-io.h>
#include <mono/metadata/mono-route.h>
#include <mono/metadata/socket-io.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/rand.h>
#include <mono/metadata/sysmath.h>
#include <mono/metadata/string-icalls.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/process.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/locales.h>
#include <mono/metadata/filewatcher.h>
#include <mono/metadata/char-conversions.h>
#include <mono/metadata/security.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/number-formatter.h>
#include <mono/metadata/security-manager.h>
#include <mono/metadata/security-core-clr.h>
#include <mono/metadata/mono-perfcounters.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-ptr-array.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/runtime.h>
#include <mono/metadata/file-mmap.h>
#include <mono/io-layer/io-layer.h>
#include <mono/utils/strtod.h>
#include <mono/utils/monobitset.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/mono-string.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-io-portability.h>
#include <mono/utils/mono-digest.h>
#include <mono/utils/bsearch.h>
#include <mono/utils/mono-mutex.h>
#include <mono/utils/mono-threads.h>

#if defined (HOST_WIN32)
#include <windows.h>
#include <shlobj.h>
#endif
#include "decimal-ms.h"

extern MonoString* ves_icall_System_Environment_GetOSVersionString (void) MONO_INTERNAL;

ICALL_EXPORT MonoReflectionAssembly* ves_icall_System_Reflection_Assembly_GetCallingAssembly (void);

static MonoArray*
type_array_from_modifiers (MonoImage *image, MonoType *type, int optional);

static inline MonoBoolean
is_generic_parameter (MonoType *type)
{
	return !type->byref && (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR);
}

static void
mono_class_init_or_throw (MonoClass *klass)
{
	if (!mono_class_init (klass))
		mono_raise_exception (mono_class_get_exception_for_failure (klass));
}

/*
 * We expect a pointer to a char, not a string
 */
ICALL_EXPORT gboolean
mono_double_ParseImpl (char *ptr, double *result)
{
	gchar *endptr = NULL;
	*result = 0.0;

	if (*ptr){
		/* mono_strtod () is not thread-safe */
		mono_mutex_lock (&mono_strtod_mutex);
		*result = mono_strtod (ptr, &endptr);
		mono_mutex_unlock (&mono_strtod_mutex);
	}

	if (!*ptr || (endptr && *endptr))
		return FALSE;
	
	return TRUE;
}

ICALL_EXPORT MonoObject *
ves_icall_System_Array_GetValueImpl (MonoObject *this, guint32 pos)
{
	MonoClass *ac;
	MonoArray *ao;
	gint32 esize;
	gpointer *ea;

	ao = (MonoArray *)this;
	ac = (MonoClass *)ao->obj.vtable->klass;

	esize = mono_array_element_size (ac);
	ea = (gpointer*)((char*)ao->vector + (pos * esize));

	if (ac->element_class->valuetype)
		return mono_value_box (this->vtable->domain, ac->element_class, ea);
	else
		return *ea;
}

ICALL_EXPORT MonoObject *
ves_icall_System_Array_GetValue (MonoObject *this, MonoObject *idxs)
{
	MonoClass *ac, *ic;
	MonoArray *ao, *io;
	gint32 i, pos, *ind;

	MONO_CHECK_ARG_NULL (idxs);

	io = (MonoArray *)idxs;
	ic = (MonoClass *)io->obj.vtable->klass;
	
	ao = (MonoArray *)this;
	ac = (MonoClass *)ao->obj.vtable->klass;

	g_assert (ic->rank == 1);
	if (io->bounds != NULL || io->max_length !=  ac->rank)
		mono_raise_exception (mono_get_exception_argument (NULL, NULL));

	ind = (gint32 *)io->vector;

	if (ao->bounds == NULL) {
		if (*ind < 0 || *ind >= ao->max_length)
			mono_raise_exception (mono_get_exception_index_out_of_range ());

		return ves_icall_System_Array_GetValueImpl (this, *ind);
	}
	
	for (i = 0; i < ac->rank; i++)
		if ((ind [i] < ao->bounds [i].lower_bound) ||
		    (ind [i] >=  (mono_array_lower_bound_t)ao->bounds [i].length + ao->bounds [i].lower_bound))
			mono_raise_exception (mono_get_exception_index_out_of_range ());

	pos = ind [0] - ao->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++)
		pos = pos*ao->bounds [i].length + ind [i] - 
			ao->bounds [i].lower_bound;

	return ves_icall_System_Array_GetValueImpl (this, pos);
}

ICALL_EXPORT void
ves_icall_System_Array_SetValueImpl (MonoArray *this, MonoObject *value, guint32 pos)
{
	MonoClass *ac, *vc, *ec;
	gint32 esize, vsize;
	gpointer *ea, *va;
	int et, vt;

	guint64 u64 = 0;
	gint64 i64 = 0;
	gdouble r64 = 0;

	if (value)
		vc = value->vtable->klass;
	else
		vc = NULL;

	ac = this->obj.vtable->klass;
	ec = ac->element_class;

	esize = mono_array_element_size (ac);
	ea = (gpointer*)((char*)this->vector + (pos * esize));
	va = (gpointer*)((char*)value + sizeof (MonoObject));

	if (mono_class_is_nullable (ec)) {
		mono_nullable_init ((guint8*)ea, value, ec);
		return;
	}

	if (!value) {
		mono_gc_bzero_atomic (ea, esize);
		return;
	}

#define NO_WIDENING_CONVERSION G_STMT_START{\
	mono_raise_exception (mono_get_exception_argument ( \
		"value", "not a widening conversion")); \
}G_STMT_END

#define CHECK_WIDENING_CONVERSION(extra) G_STMT_START{\
	if (esize < vsize + (extra)) \
		mono_raise_exception (mono_get_exception_argument ( \
			"value", "not a widening conversion")); \
}G_STMT_END

#define INVALID_CAST G_STMT_START{ \
		mono_get_runtime_callbacks ()->set_cast_details (vc, ec); \
	mono_raise_exception (mono_get_exception_invalid_cast ()); \
}G_STMT_END

	/* Check element (destination) type. */
	switch (ec->byval_arg.type) {
	case MONO_TYPE_STRING:
		switch (vc->byval_arg.type) {
		case MONO_TYPE_STRING:
			break;
		default:
			INVALID_CAST;
		}
		break;
	case MONO_TYPE_BOOLEAN:
		switch (vc->byval_arg.type) {
		case MONO_TYPE_BOOLEAN:
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_U1:
		case MONO_TYPE_U2:
		case MONO_TYPE_U4:
		case MONO_TYPE_U8:
		case MONO_TYPE_I1:
		case MONO_TYPE_I2:
		case MONO_TYPE_I4:
		case MONO_TYPE_I8:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			NO_WIDENING_CONVERSION;
		default:
			INVALID_CAST;
		}
		break;
	}

	if (!ec->valuetype) {
		if (!mono_object_isinst (value, ec))
			INVALID_CAST;
		mono_gc_wbarrier_set_arrayref (this, ea, (MonoObject*)value);
		return;
	}

	if (mono_object_isinst (value, ec)) {
		if (ec->has_references)
			mono_value_copy (ea, (char*)value + sizeof (MonoObject), ec);
		else
			mono_gc_memmove_atomic (ea, (char *)value + sizeof (MonoObject), esize);
		return;
	}

	if (!vc->valuetype)
		INVALID_CAST;

	vsize = mono_class_instance_size (vc) - sizeof (MonoObject);

	et = ec->byval_arg.type;
	if (et == MONO_TYPE_VALUETYPE && ec->byval_arg.data.klass->enumtype)
		et = mono_class_enum_basetype (ec->byval_arg.data.klass)->type;

	vt = vc->byval_arg.type;
	if (vt == MONO_TYPE_VALUETYPE && vc->byval_arg.data.klass->enumtype)
		vt = mono_class_enum_basetype (vc->byval_arg.data.klass)->type;

#define ASSIGN_UNSIGNED(etype) G_STMT_START{\
	switch (vt) { \
	case MONO_TYPE_U1: \
	case MONO_TYPE_U2: \
	case MONO_TYPE_U4: \
	case MONO_TYPE_U8: \
	case MONO_TYPE_CHAR: \
		CHECK_WIDENING_CONVERSION(0); \
		*(etype *) ea = (etype) u64; \
		return; \
	/* You can't assign a signed value to an unsigned array. */ \
	case MONO_TYPE_I1: \
	case MONO_TYPE_I2: \
	case MONO_TYPE_I4: \
	case MONO_TYPE_I8: \
	/* You can't assign a floating point number to an integer array. */ \
	case MONO_TYPE_R4: \
	case MONO_TYPE_R8: \
		NO_WIDENING_CONVERSION; \
	} \
}G_STMT_END

#define ASSIGN_SIGNED(etype) G_STMT_START{\
	switch (vt) { \
	case MONO_TYPE_I1: \
	case MONO_TYPE_I2: \
	case MONO_TYPE_I4: \
	case MONO_TYPE_I8: \
		CHECK_WIDENING_CONVERSION(0); \
		*(etype *) ea = (etype) i64; \
		return; \
	/* You can assign an unsigned value to a signed array if the array's */ \
	/* element size is larger than the value size. */ \
	case MONO_TYPE_U1: \
	case MONO_TYPE_U2: \
	case MONO_TYPE_U4: \
	case MONO_TYPE_U8: \
	case MONO_TYPE_CHAR: \
		CHECK_WIDENING_CONVERSION(1); \
		*(etype *) ea = (etype) u64; \
		return; \
	/* You can't assign a floating point number to an integer array. */ \
	case MONO_TYPE_R4: \
	case MONO_TYPE_R8: \
		NO_WIDENING_CONVERSION; \
	} \
}G_STMT_END

#define ASSIGN_REAL(etype) G_STMT_START{\
	switch (vt) { \
	case MONO_TYPE_R4: \
	case MONO_TYPE_R8: \
		CHECK_WIDENING_CONVERSION(0); \
		*(etype *) ea = (etype) r64; \
		return; \
	/* All integer values fit into a floating point array, so we don't */ \
	/* need to CHECK_WIDENING_CONVERSION here. */ \
	case MONO_TYPE_I1: \
	case MONO_TYPE_I2: \
	case MONO_TYPE_I4: \
	case MONO_TYPE_I8: \
		*(etype *) ea = (etype) i64; \
		return; \
	case MONO_TYPE_U1: \
	case MONO_TYPE_U2: \
	case MONO_TYPE_U4: \
	case MONO_TYPE_U8: \
	case MONO_TYPE_CHAR: \
		*(etype *) ea = (etype) u64; \
		return; \
	} \
}G_STMT_END

	switch (vt) {
	case MONO_TYPE_U1:
		u64 = *(guint8 *) va;
		break;
	case MONO_TYPE_U2:
		u64 = *(guint16 *) va;
		break;
	case MONO_TYPE_U4:
		u64 = *(guint32 *) va;
		break;
	case MONO_TYPE_U8:
		u64 = *(guint64 *) va;
		break;
	case MONO_TYPE_I1:
		i64 = *(gint8 *) va;
		break;
	case MONO_TYPE_I2:
		i64 = *(gint16 *) va;
		break;
	case MONO_TYPE_I4:
		i64 = *(gint32 *) va;
		break;
	case MONO_TYPE_I8:
		i64 = *(gint64 *) va;
		break;
	case MONO_TYPE_R4:
		r64 = *(gfloat *) va;
		break;
	case MONO_TYPE_R8:
		r64 = *(gdouble *) va;
		break;
	case MONO_TYPE_CHAR:
		u64 = *(guint16 *) va;
		break;
	case MONO_TYPE_BOOLEAN:
		/* Boolean is only compatible with itself. */
		switch (et) {
		case MONO_TYPE_CHAR:
		case MONO_TYPE_U1:
		case MONO_TYPE_U2:
		case MONO_TYPE_U4:
		case MONO_TYPE_U8:
		case MONO_TYPE_I1:
		case MONO_TYPE_I2:
		case MONO_TYPE_I4:
		case MONO_TYPE_I8:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			NO_WIDENING_CONVERSION;
		default:
			INVALID_CAST;
		}
		break;
	}

	/* If we can't do a direct copy, let's try a widening conversion. */
	switch (et) {
	case MONO_TYPE_CHAR:
		ASSIGN_UNSIGNED (guint16);
	case MONO_TYPE_U1:
		ASSIGN_UNSIGNED (guint8);
	case MONO_TYPE_U2:
		ASSIGN_UNSIGNED (guint16);
	case MONO_TYPE_U4:
		ASSIGN_UNSIGNED (guint32);
	case MONO_TYPE_U8:
		ASSIGN_UNSIGNED (guint64);
	case MONO_TYPE_I1:
		ASSIGN_SIGNED (gint8);
	case MONO_TYPE_I2:
		ASSIGN_SIGNED (gint16);
	case MONO_TYPE_I4:
		ASSIGN_SIGNED (gint32);
	case MONO_TYPE_I8:
		ASSIGN_SIGNED (gint64);
	case MONO_TYPE_R4:
		ASSIGN_REAL (gfloat);
	case MONO_TYPE_R8:
		ASSIGN_REAL (gdouble);
	}

	INVALID_CAST;
	/* Not reached, INVALID_CAST does not return. Just to avoid a compiler warning ... */
	return;

#undef INVALID_CAST
#undef NO_WIDENING_CONVERSION
#undef CHECK_WIDENING_CONVERSION
#undef ASSIGN_UNSIGNED
#undef ASSIGN_SIGNED
#undef ASSIGN_REAL
}

ICALL_EXPORT void 
ves_icall_System_Array_SetValue (MonoArray *this, MonoObject *value,
				 MonoArray *idxs)
{
	MonoClass *ac, *ic;
	gint32 i, pos, *ind;

	MONO_CHECK_ARG_NULL (idxs);

	ic = idxs->obj.vtable->klass;
	ac = this->obj.vtable->klass;

	g_assert (ic->rank == 1);
	if (idxs->bounds != NULL || idxs->max_length != ac->rank)
		mono_raise_exception (mono_get_exception_argument (NULL, NULL));

	ind = (gint32 *)idxs->vector;

	if (this->bounds == NULL) {
		if (*ind < 0 || *ind >= this->max_length)
			mono_raise_exception (mono_get_exception_index_out_of_range ());

		ves_icall_System_Array_SetValueImpl (this, value, *ind);
		return;
	}
	
	for (i = 0; i < ac->rank; i++)
		if ((ind [i] < this->bounds [i].lower_bound) ||
		    (ind [i] >= (mono_array_lower_bound_t)this->bounds [i].length + this->bounds [i].lower_bound))
			mono_raise_exception (mono_get_exception_index_out_of_range ());

	pos = ind [0] - this->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++)
		pos = pos * this->bounds [i].length + ind [i] - 
			this->bounds [i].lower_bound;

	ves_icall_System_Array_SetValueImpl (this, value, pos);
}

ICALL_EXPORT MonoArray *
ves_icall_System_Array_CreateInstanceImpl (MonoReflectionType *type, MonoArray *lengths, MonoArray *bounds)
{
	MonoClass *aklass, *klass;
	MonoArray *array;
	uintptr_t *sizes, i;
	gboolean bounded = FALSE;

	MONO_CHECK_ARG_NULL (type);
	MONO_CHECK_ARG_NULL (lengths);

	MONO_CHECK_ARG (lengths, mono_array_length (lengths) > 0);
	if (bounds)
		MONO_CHECK_ARG (bounds, mono_array_length (lengths) == mono_array_length (bounds));

	for (i = 0; i < mono_array_length (lengths); i++)
		if (mono_array_get (lengths, gint32, i) < 0)
			mono_raise_exception (mono_get_exception_argument_out_of_range (NULL));

	klass = mono_class_from_mono_type (type->type);
	mono_class_init_or_throw (klass);

	if (bounds && (mono_array_length (bounds) == 1) && (mono_array_get (bounds, gint32, 0) != 0))
		/* vectors are not the same as one dimensional arrays with no-zero bounds */
		bounded = TRUE;
	else
		bounded = FALSE;

	aklass = mono_bounded_array_class_get (klass, mono_array_length (lengths), bounded);

	sizes = alloca (aklass->rank * sizeof(intptr_t) * 2);
	for (i = 0; i < aklass->rank; ++i) {
		sizes [i] = mono_array_get (lengths, guint32, i);
		if (bounds)
			sizes [i + aklass->rank] = mono_array_get (bounds, gint32, i);
		else
			sizes [i + aklass->rank] = 0;
	}

	array = mono_array_new_full (mono_object_domain (type), aklass, sizes, (intptr_t*)sizes + aklass->rank);

	return array;
}

ICALL_EXPORT MonoArray *
ves_icall_System_Array_CreateInstanceImpl64 (MonoReflectionType *type, MonoArray *lengths, MonoArray *bounds)
{
	MonoClass *aklass, *klass;
	MonoArray *array;
	uintptr_t *sizes, i;
	gboolean bounded = FALSE;

	MONO_CHECK_ARG_NULL (type);
	MONO_CHECK_ARG_NULL (lengths);

	MONO_CHECK_ARG (lengths, mono_array_length (lengths) > 0);
	if (bounds)
		MONO_CHECK_ARG (bounds, mono_array_length (lengths) == mono_array_length (bounds));

	for (i = 0; i < mono_array_length (lengths); i++) 
		if ((mono_array_get (lengths, gint64, i) < 0) ||
		    (mono_array_get (lengths, gint64, i) > MONO_ARRAY_MAX_INDEX))
			mono_raise_exception (mono_get_exception_argument_out_of_range (NULL));

	klass = mono_class_from_mono_type (type->type);
	mono_class_init_or_throw (klass);

	if (bounds && (mono_array_length (bounds) == 1) && (mono_array_get (bounds, gint64, 0) != 0))
		/* vectors are not the same as one dimensional arrays with no-zero bounds */
		bounded = TRUE;
	else
		bounded = FALSE;

	aklass = mono_bounded_array_class_get (klass, mono_array_length (lengths), bounded);

	sizes = alloca (aklass->rank * sizeof(intptr_t) * 2);
	for (i = 0; i < aklass->rank; ++i) {
		sizes [i] = mono_array_get (lengths, guint64, i);
		if (bounds)
			sizes [i + aklass->rank] = (mono_array_size_t) mono_array_get (bounds, guint64, i);
		else
			sizes [i + aklass->rank] = 0;
	}

	array = mono_array_new_full (mono_object_domain (type), aklass, sizes, (intptr_t*)sizes + aklass->rank);

	return array;
}

ICALL_EXPORT gint32 
ves_icall_System_Array_GetRank (MonoObject *this)
{
	return this->vtable->klass->rank;
}

ICALL_EXPORT gint32
ves_icall_System_Array_GetLength (MonoArray *this, gint32 dimension)
{
	gint32 rank = ((MonoObject *)this)->vtable->klass->rank;
	uintptr_t length;

	if ((dimension < 0) || (dimension >= rank))
		mono_raise_exception (mono_get_exception_index_out_of_range ());
	
	if (this->bounds == NULL)
		length = this->max_length;
	else
		length = this->bounds [dimension].length;

#ifdef MONO_BIG_ARRAYS
	if (length > G_MAXINT32)
	        mono_raise_exception (mono_get_exception_overflow ());
#endif
	return length;
}

ICALL_EXPORT gint64
ves_icall_System_Array_GetLongLength (MonoArray *this, gint32 dimension)
{
	gint32 rank = ((MonoObject *)this)->vtable->klass->rank;

	if ((dimension < 0) || (dimension >= rank))
		mono_raise_exception (mono_get_exception_index_out_of_range ());
	
	if (this->bounds == NULL)
 		return this->max_length;
 	
 	return this->bounds [dimension].length;
}

ICALL_EXPORT gint32
ves_icall_System_Array_GetLowerBound (MonoArray *this, gint32 dimension)
{
	gint32 rank = ((MonoObject *)this)->vtable->klass->rank;

	if ((dimension < 0) || (dimension >= rank))
		mono_raise_exception (mono_get_exception_index_out_of_range ());
	
	if (this->bounds == NULL)
		return 0;
	
	return this->bounds [dimension].lower_bound;
}

ICALL_EXPORT void
ves_icall_System_Array_ClearInternal (MonoArray *arr, int idx, int length)
{
	int sz = mono_array_element_size (mono_object_class (arr));
	mono_gc_bzero_atomic (mono_array_addr_with_size_fast (arr, sz, idx), length * sz);
}

ICALL_EXPORT gboolean
ves_icall_System_Array_FastCopy (MonoArray *source, int source_idx, MonoArray* dest, int dest_idx, int length)
{
	int element_size;
	void * dest_addr;
	void * source_addr;
	MonoVTable *src_vtable;
	MonoVTable *dest_vtable;
	MonoClass *src_class;
	MonoClass *dest_class;

	src_vtable = source->obj.vtable;
	dest_vtable = dest->obj.vtable;

	if (src_vtable->rank != dest_vtable->rank)
		return FALSE;

	if (source->bounds || dest->bounds)
		return FALSE;

	/* there's no integer overflow since mono_array_length returns an unsigned integer */
	if ((dest_idx + length > mono_array_length_fast (dest)) ||
		(source_idx + length > mono_array_length_fast (source)))
		return FALSE;

	src_class = src_vtable->klass->element_class;
	dest_class = dest_vtable->klass->element_class;

	/*
	 * Handle common cases.
	 */

	/* Case1: object[] -> valuetype[] (ArrayList::ToArray) 
	We fallback to managed here since we need to typecheck each boxed valuetype before storing them in the dest array.
	*/
	if (src_class == mono_defaults.object_class && dest_class->valuetype)
		return FALSE;

	/* Check if we're copying a char[] <==> (u)short[] */
	if (src_class != dest_class) {
		if (dest_class->valuetype || dest_class->enumtype || src_class->valuetype || src_class->enumtype)
			return FALSE;

		/* It's only safe to copy between arrays if we can ensure the source will always have a subtype of the destination. We bail otherwise. */
		if (!mono_class_is_subclass_of (src_class, dest_class, FALSE))
			return FALSE;
	}

	if (dest_class->valuetype) {
		element_size = mono_array_element_size (source->obj.vtable->klass);
		source_addr = mono_array_addr_with_size_fast (source, element_size, source_idx);
		if (dest_class->has_references) {
			mono_value_copy_array (dest, dest_idx, source_addr, length);
		} else {
			dest_addr = mono_array_addr_with_size_fast (dest, element_size, dest_idx);
			mono_gc_memmove_atomic (dest_addr, source_addr, element_size * length);
		}
	} else {
		mono_array_memcpy_refs_fast (dest, dest_idx, source, source_idx, length);
	}

	return TRUE;
}

ICALL_EXPORT void
ves_icall_System_Array_GetGenericValueImpl (MonoObject *this, guint32 pos, gpointer value)
{
	MonoClass *ac;
	MonoArray *ao;
	gint32 esize;
	gpointer *ea;

	ao = (MonoArray *)this;
	ac = (MonoClass *)ao->obj.vtable->klass;

	esize = mono_array_element_size (ac);
	ea = (gpointer*)((char*)ao->vector + (pos * esize));

	mono_gc_memmove_atomic (value, ea, esize);
}

ICALL_EXPORT void
ves_icall_System_Array_SetGenericValueImpl (MonoObject *this, guint32 pos, gpointer value)
{
	MonoClass *ac, *ec;
	MonoArray *ao;
	gint32 esize;
	gpointer *ea;

	ao = (MonoArray *)this;
	ac = (MonoClass *)ao->obj.vtable->klass;
	ec = ac->element_class;

	esize = mono_array_element_size (ac);
	ea = (gpointer*)((char*)ao->vector + (pos * esize));

	if (MONO_TYPE_IS_REFERENCE (&ec->byval_arg)) {
		g_assert (esize == sizeof (gpointer));
		mono_gc_wbarrier_generic_store (ea, *(gpointer*)value);
	} else {
		g_assert (ec->inited);
		g_assert (esize == mono_class_value_size (ec, NULL));
		if (ec->has_references)
			mono_gc_wbarrier_value_copy (ea, value, 1, ec);
		else
			mono_gc_memmove_atomic (ea, value, esize);
	}
}

ICALL_EXPORT void
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray (MonoArray *array, MonoClassField *field_handle)
{
	MonoClass *klass = array->obj.vtable->klass;
	guint32 size = mono_array_element_size (klass);
	MonoType *type = mono_type_get_underlying_type (&klass->element_class->byval_arg);
	int align;
	const char *field_data;

	if (MONO_TYPE_IS_REFERENCE (type) || type->type == MONO_TYPE_VALUETYPE) {
		MonoException *exc = mono_get_exception_argument("array",
			"Cannot initialize array of non-primitive type.");
		mono_raise_exception (exc);
	}

	if (!(field_handle->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA)) {
		MonoException *exc = mono_get_exception_argument("field_handle",
			"Field doesn't have an RVA");
		mono_raise_exception (exc);
	}

	size *= array->max_length;
	field_data = mono_field_get_data (field_handle);

	if (size > mono_type_size (field_handle->type, &align)) {
		MonoException *exc = mono_get_exception_argument("field_handle",
			"Field not large enough to fill array");
		mono_raise_exception (exc);
	}

#if G_BYTE_ORDER != G_LITTLE_ENDIAN
#define SWAP(n) {\
	guint ## n *data = (guint ## n *) mono_array_addr (array, char, 0); \
	guint ## n *src = (guint ## n *) field_data; \
	guint ## n *end = (guint ## n *)((char*)src + size); \
\
	for (; src < end; data++, src++) { \
		*data = read ## n (src); \
	} \
}

	/* printf ("Initialize array with elements of %s type\n", klass->element_class->name); */

	switch (type->type) {
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
		SWAP (16);
		break;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_R4:
		SWAP (32);
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R8:
		SWAP (64);
		break;
	default:
		memcpy (mono_array_addr (array, char, 0), field_data, size);
		break;
	}
#else
	memcpy (mono_array_addr (array, char, 0), field_data, size);
#endif
}

ICALL_EXPORT gint
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetOffsetToStringData (void)
{
	return offsetof (MonoString, chars);
}

ICALL_EXPORT MonoObject *
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetObjectValue (MonoObject *obj)
{
	if ((obj == NULL) || (! (obj->vtable->klass->valuetype)))
		return obj;
	else
		return mono_object_clone (obj);
}

ICALL_EXPORT void
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunClassConstructor (MonoType *handle)
{
	MonoClass *klass;
	MonoVTable *vtable;

	MONO_CHECK_ARG_NULL (handle);

	klass = mono_class_from_mono_type (handle);
	MONO_CHECK_ARG (handle, klass);

	vtable = mono_class_vtable_full (mono_domain_get (), klass, TRUE);

	/* This will call the type constructor */
	mono_runtime_class_init (vtable);
}

ICALL_EXPORT void
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunModuleConstructor (MonoImage *image)
{
	MonoError error;

	mono_image_check_for_module_cctor (image);
	if (image->has_module_cctor) {
		MonoClass *module_klass = mono_class_get_checked (image, MONO_TOKEN_TYPE_DEF | 1, &error);
		mono_error_raise_exception (&error);
		/*It's fine to raise the exception here*/
		mono_runtime_class_init (mono_class_vtable_full (mono_domain_get (), module_klass, TRUE));
	}
}

ICALL_EXPORT MonoBoolean
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_SufficientExecutionStack (void)
{
	guint8 *stack_addr;
	guint8 *current;
	size_t stack_size;
	/* later make this configurable and per-arch */
	int min_size = 4096 * 4 * sizeof (void*);
	mono_thread_info_get_stack_bounds (&stack_addr, &stack_size);
	/* if we have no info we are optimistic and assume there is enough room */
	if (!stack_addr)
		return TRUE;
#ifdef HOST_WIN32
	// FIXME: Windows dynamically extends the stack, so stack_addr might be close
	// to the current sp
	return TRUE;
#endif
	current = (guint8 *)&stack_addr;
	if (current > stack_addr) {
		if ((current - stack_addr) < min_size)
			return FALSE;
	} else {
		if (current - (stack_addr - stack_size) < min_size)
			return FALSE;
	}
	return TRUE;
}

ICALL_EXPORT MonoObject *
ves_icall_System_Object_MemberwiseClone (MonoObject *this)
{
	return mono_object_clone (this);
}

ICALL_EXPORT gint32
ves_icall_System_ValueType_InternalGetHashCode (MonoObject *this, MonoArray **fields)
{
	MonoClass *klass;
	MonoObject **values = NULL;
	MonoObject *o;
	int count = 0;
	gint32 result = (int)(gsize)mono_defaults.int32_class;
	MonoClassField* field;
	gpointer iter;

	klass = mono_object_class (this);

	if (mono_class_num_fields (klass) == 0)
		return result;

	/*
	 * Compute the starting value of the hashcode for fields of primitive
	 * types, and return the remaining fields in an array to the managed side.
	 * This way, we can avoid costly reflection operations in managed code.
	 */
	iter = NULL;
	while ((field = mono_class_get_fields (klass, &iter))) {
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		if (mono_field_is_deleted (field))
			continue;
		/* FIXME: Add more types */
		switch (field->type->type) {
		case MONO_TYPE_I4:
			result ^= *(gint32*)((guint8*)this + field->offset);
			break;
		case MONO_TYPE_STRING: {
			MonoString *s;
			s = *(MonoString**)((guint8*)this + field->offset);
			if (s != NULL)
				result ^= mono_string_hash (s);
			break;
		}
		default:
			if (!values)
				values = g_newa (MonoObject*, mono_class_num_fields (klass));
			o = mono_field_get_value_object (mono_object_domain (this), field, this);
			values [count++] = o;
		}
	}

	if (values) {
		int i;
		mono_gc_wbarrier_generic_store (fields, (MonoObject*) mono_array_new (mono_domain_get (), mono_defaults.object_class, count));
		for (i = 0; i < count; ++i)
			mono_array_setref (*fields, i, values [i]);
	} else {
		*fields = NULL;
	}
	return result;
}

ICALL_EXPORT MonoBoolean
ves_icall_System_ValueType_Equals (MonoObject *this, MonoObject *that, MonoArray **fields)
{
	MonoClass *klass;
	MonoObject **values = NULL;
	MonoObject *o;
	MonoClassField* field;
	gpointer iter;
	int count = 0;

	MONO_CHECK_ARG_NULL (that);

	if (this->vtable != that->vtable)
		return FALSE;

	klass = mono_object_class (this);

	if (klass->enumtype && mono_class_enum_basetype (klass) && mono_class_enum_basetype (klass)->type == MONO_TYPE_I4)
		return (*(gint32*)((guint8*)this + sizeof (MonoObject)) == *(gint32*)((guint8*)that + sizeof (MonoObject)));

	/*
	 * Do the comparison for fields of primitive type and return a result if
	 * possible. Otherwise, return the remaining fields in an array to the 
	 * managed side. This way, we can avoid costly reflection operations in 
	 * managed code.
	 */
	*fields = NULL;
	iter = NULL;
	while ((field = mono_class_get_fields (klass, &iter))) {
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		if (mono_field_is_deleted (field))
			continue;
		/* FIXME: Add more types */
		switch (field->type->type) {
		case MONO_TYPE_U1:
		case MONO_TYPE_I1:
		case MONO_TYPE_BOOLEAN:
			if (*((guint8*)this + field->offset) != *((guint8*)that + field->offset))
				return FALSE;
			break;
		case MONO_TYPE_U2:
		case MONO_TYPE_I2:
		case MONO_TYPE_CHAR:
			if (*(gint16*)((guint8*)this + field->offset) != *(gint16*)((guint8*)that + field->offset))
				return FALSE;
			break;
		case MONO_TYPE_U4:
		case MONO_TYPE_I4:
			if (*(gint32*)((guint8*)this + field->offset) != *(gint32*)((guint8*)that + field->offset))
				return FALSE;
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			if (*(gint64*)((guint8*)this + field->offset) != *(gint64*)((guint8*)that + field->offset))
				return FALSE;
			break;
		case MONO_TYPE_R4:
			if (*(float*)((guint8*)this + field->offset) != *(float*)((guint8*)that + field->offset))
				return FALSE;
			break;
		case MONO_TYPE_R8:
			if (*(double*)((guint8*)this + field->offset) != *(double*)((guint8*)that + field->offset))
				return FALSE;
			break;


		case MONO_TYPE_STRING: {
			MonoString *s1, *s2;
			guint32 s1len, s2len;
			s1 = *(MonoString**)((guint8*)this + field->offset);
			s2 = *(MonoString**)((guint8*)that + field->offset);
			if (s1 == s2)
				break;
			if ((s1 == NULL) || (s2 == NULL))
				return FALSE;
			s1len = mono_string_length (s1);
			s2len = mono_string_length (s2);
			if (s1len != s2len)
				return FALSE;

			if (memcmp (mono_string_chars (s1), mono_string_chars (s2), s1len * sizeof (gunichar2)) != 0)
				return FALSE;
			break;
		}
		default:
			if (!values)
				values = g_newa (MonoObject*, mono_class_num_fields (klass) * 2);
			o = mono_field_get_value_object (mono_object_domain (this), field, this);
			values [count++] = o;
			o = mono_field_get_value_object (mono_object_domain (this), field, that);
			values [count++] = o;
		}

		if (klass->enumtype)
			/* enums only have one non-static field */
			break;
	}

	if (values) {
		int i;
		mono_gc_wbarrier_generic_store (fields, (MonoObject*) mono_array_new (mono_domain_get (), mono_defaults.object_class, count));
		for (i = 0; i < count; ++i)
			mono_array_setref_fast (*fields, i, values [i]);
		return FALSE;
	} else {
		return TRUE;
	}
}

ICALL_EXPORT MonoReflectionType *
ves_icall_System_Object_GetType (MonoObject *obj)
{
#ifndef DISABLE_REMOTING
	if (obj->vtable->klass == mono_defaults.transparent_proxy_class)
		return mono_type_get_object (mono_object_domain (obj), &((MonoTransparentProxy*)obj)->remote_class->proxy_class->byval_arg);
	else
#endif
		return mono_type_get_object (mono_object_domain (obj), &obj->vtable->klass->byval_arg);
}

ICALL_EXPORT void
mono_type_type_from_obj (MonoReflectionType *mtype, MonoObject *obj)
{
	mtype->type = &obj->vtable->klass->byval_arg;
	g_assert (mtype->type->type);
}

ICALL_EXPORT gint32
ves_icall_ModuleBuilder_getToken (MonoReflectionModuleBuilder *mb, MonoObject *obj, gboolean create_open_instance)
{
	MONO_CHECK_ARG_NULL (obj);
	
	return mono_image_create_token (mb->dynamic_image, obj, create_open_instance, TRUE);
}

ICALL_EXPORT gint32
ves_icall_ModuleBuilder_getMethodToken (MonoReflectionModuleBuilder *mb,
					MonoReflectionMethod *method,
					MonoArray *opt_param_types)
{
	MONO_CHECK_ARG_NULL (method);
	
	return mono_image_create_method_token (
		mb->dynamic_image, (MonoObject *) method, opt_param_types);
}

ICALL_EXPORT void
ves_icall_ModuleBuilder_WriteToFile (MonoReflectionModuleBuilder *mb, HANDLE file)
{
	mono_image_create_pefile (mb, file);
}

ICALL_EXPORT void
ves_icall_ModuleBuilder_build_metadata (MonoReflectionModuleBuilder *mb)
{
	mono_image_build_metadata (mb);
}

ICALL_EXPORT void
ves_icall_ModuleBuilder_RegisterToken (MonoReflectionModuleBuilder *mb, MonoObject *obj, guint32 token)
{
	mono_image_register_token (mb->dynamic_image, token, obj);
}

static gboolean
get_caller (MonoMethod *m, gint32 no, gint32 ilo, gboolean managed, gpointer data)
{
	MonoMethod **dest = data;

	/* skip unmanaged frames */
	if (!managed)
		return FALSE;

	if (m == *dest) {
		*dest = NULL;
		return FALSE;
	}
	if (!(*dest)) {
		*dest = m;
		return TRUE;
	}
	return FALSE;
}

static gboolean
get_executing (MonoMethod *m, gint32 no, gint32 ilo, gboolean managed, gpointer data)
{
	MonoMethod **dest = data;

	/* skip unmanaged frames */
	if (!managed)
		return FALSE;

	if (!(*dest)) {
		if (!strcmp (m->klass->name_space, "System.Reflection"))
			return FALSE;
		*dest = m;
		return TRUE;
	}
	return FALSE;
}

static gboolean
get_caller_no_reflection (MonoMethod *m, gint32 no, gint32 ilo, gboolean managed, gpointer data)
{
	MonoMethod **dest = data;

	/* skip unmanaged frames */
	if (!managed)
		return FALSE;

	if (m->wrapper_type != MONO_WRAPPER_NONE)
		return FALSE;

	if (m->klass->image == mono_defaults.corlib && !strcmp (m->klass->name_space, "System.Reflection"))
		return FALSE;

	if (m == *dest) {
		*dest = NULL;
		return FALSE;
	}
	if (!(*dest)) {
		*dest = m;
		return TRUE;
	}
	return FALSE;
}

static MonoReflectionType *
type_from_name (const char *str, MonoBoolean ignoreCase)
{
	MonoMethod *m, *dest;

	MonoType *type = NULL;
	MonoAssembly *assembly = NULL;
	MonoTypeNameParse info;
	char *temp_str = g_strdup (str);
	gboolean type_resolve = FALSE;

	/* mono_reflection_parse_type() mangles the string */
	if (!mono_reflection_parse_type (temp_str, &info)) {
		mono_reflection_free_type_info (&info);
		g_free (temp_str);
		return NULL;
	}


	/*
	 * We must compute the calling assembly as type loading must happen under a metadata context.
	 * For example. The main assembly is a.exe and Type.GetType is called from dir/b.dll. Without
	 * the metadata context (basedir currently) set to dir/b.dll we won't be able to load a dir/c.dll.
	 */
	m = mono_method_get_last_managed ();
	dest = m;

	mono_stack_walk_no_il (get_caller_no_reflection, &dest);
	if (!dest)
		dest = m;

	/*
	 * FIXME: mono_method_get_last_managed() sometimes returns NULL, thus
	 *        causing ves_icall_System_Reflection_Assembly_GetCallingAssembly()
	 *        to crash.  This only seems to happen in some strange remoting
	 *        scenarios and I was unable to figure out what's happening there.
	 *        Dec 10, 2005 - Martin.
	 */

	if (dest) {
		assembly = dest->klass->image->assembly;
		type_resolve = TRUE;
	} else {
		g_warning (G_STRLOC);
	}

	if (info.assembly.name)
		assembly = mono_assembly_load (&info.assembly, assembly ? assembly->basedir : NULL, NULL);


	if (assembly) {
		/* When loading from the current assembly, AppDomain.TypeResolve will not be called yet */
		type = mono_reflection_get_type (assembly->image, &info, ignoreCase, &type_resolve);
	}

	if (!info.assembly.name && !type) /* try mscorlib */
		type = mono_reflection_get_type (NULL, &info, ignoreCase, &type_resolve);

	if (assembly && !type && type_resolve) {
		type_resolve = FALSE; /* This will invoke TypeResolve if not done in the first 'if' */
		type = mono_reflection_get_type (assembly->image, &info, ignoreCase, &type_resolve);
	}

	mono_reflection_free_type_info (&info);
	g_free (temp_str);

	if (!type) 
		return NULL;

	return mono_type_get_object (mono_domain_get (), type);
}

#ifdef UNUSED
MonoReflectionType *
mono_type_get (const char *str)
{
	char *copy = g_strdup (str);
	MonoReflectionType *type = type_from_name (copy, FALSE);

	g_free (copy);
	return type;
}
#endif

ICALL_EXPORT MonoReflectionType*
ves_icall_type_from_name (MonoString *name,
			  MonoBoolean throwOnError,
			  MonoBoolean ignoreCase)
{
	char *str = mono_string_to_utf8 (name);
	MonoReflectionType *type;

	type = type_from_name (str, ignoreCase);
	g_free (str);
	if (type == NULL){
		MonoException *e = NULL;
		
		if (throwOnError)
			e = mono_get_exception_type_load (name, NULL);

		mono_loader_clear_error ();
		if (e != NULL)
			mono_raise_exception (e);
	}
	
	return type;
}


ICALL_EXPORT MonoReflectionType*
ves_icall_type_from_handle (MonoType *handle)
{
	MonoDomain *domain = mono_domain_get (); 

	return mono_type_get_object (domain, handle);
}

ICALL_EXPORT MonoBoolean
ves_icall_System_Type_EqualsInternal (MonoReflectionType *type, MonoReflectionType *c)
{
	if (c && type->type && c->type)
		return mono_metadata_type_equal (type->type, c->type);
	else
		return (type == c) ? TRUE : FALSE;
}

/* System.TypeCode */
typedef enum {
	TYPECODE_EMPTY,
	TYPECODE_OBJECT,
	TYPECODE_DBNULL,
	TYPECODE_BOOLEAN,
	TYPECODE_CHAR,
	TYPECODE_SBYTE,
	TYPECODE_BYTE,
	TYPECODE_INT16,
	TYPECODE_UINT16,
	TYPECODE_INT32,
	TYPECODE_UINT32,
	TYPECODE_INT64,
	TYPECODE_UINT64,
	TYPECODE_SINGLE,
	TYPECODE_DOUBLE,
	TYPECODE_DECIMAL,
	TYPECODE_DATETIME,
	TYPECODE_STRING = 18
} TypeCode;

ICALL_EXPORT guint32
ves_icall_type_GetTypeCodeInternal (MonoReflectionType *type)
{
	int t = type->type->type;

	if (type->type->byref)
		return TYPECODE_OBJECT;

handle_enum:
	switch (t) {
	case MONO_TYPE_VOID:
		return TYPECODE_OBJECT;
	case MONO_TYPE_BOOLEAN:
		return TYPECODE_BOOLEAN;
	case MONO_TYPE_U1:
		return TYPECODE_BYTE;
	case MONO_TYPE_I1:
		return TYPECODE_SBYTE;
	case MONO_TYPE_U2:
		return TYPECODE_UINT16;
	case MONO_TYPE_I2:
		return TYPECODE_INT16;
	case MONO_TYPE_CHAR:
		return TYPECODE_CHAR;
	case MONO_TYPE_PTR:
	case MONO_TYPE_U:
	case MONO_TYPE_I:
		return TYPECODE_OBJECT;
	case MONO_TYPE_U4:
		return TYPECODE_UINT32;
	case MONO_TYPE_I4:
		return TYPECODE_INT32;
	case MONO_TYPE_U8:
		return TYPECODE_UINT64;
	case MONO_TYPE_I8:
		return TYPECODE_INT64;
	case MONO_TYPE_R4:
		return TYPECODE_SINGLE;
	case MONO_TYPE_R8:
		return TYPECODE_DOUBLE;
	case MONO_TYPE_VALUETYPE: {
		MonoClass *klass = type->type->data.klass;
		
		if (klass->enumtype) {
			t = mono_class_enum_basetype (klass)->type;
			goto handle_enum;
		} else if (mono_is_corlib_image (klass->image)) {
			if (strcmp (klass->name_space, "System") == 0) {
				if (strcmp (klass->name, "Decimal") == 0)
					return TYPECODE_DECIMAL;
				else if (strcmp (klass->name, "DateTime") == 0)
					return TYPECODE_DATETIME;
			}
		}
		return TYPECODE_OBJECT;
	}
	case MONO_TYPE_STRING:
		return TYPECODE_STRING;
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
	case MONO_TYPE_TYPEDBYREF:
		return TYPECODE_OBJECT;
	case MONO_TYPE_CLASS:
		{
			MonoClass *klass =  type->type->data.klass;
			if (klass->image == mono_defaults.corlib && strcmp (klass->name_space, "System") == 0) {
				if (strcmp (klass->name, "DBNull") == 0)
					return TYPECODE_DBNULL;
			}
		}
		return TYPECODE_OBJECT;
	case MONO_TYPE_GENERICINST:
		return TYPECODE_OBJECT;
	default:
		g_error ("type 0x%02x not handled in GetTypeCode()", t);
	}
	return 0;
}

ICALL_EXPORT guint32
ves_icall_type_is_subtype_of (MonoReflectionType *type, MonoReflectionType *c, MonoBoolean check_interfaces)
{
	MonoDomain *domain; 
	MonoClass *klass;
	MonoClass *klassc;

	g_assert (type != NULL);
	
	domain = ((MonoObject *)type)->vtable->domain;

	if (!c) /* FIXME: dont know what do do here */
		return 0;

	klass = mono_class_from_mono_type (type->type);
	klassc = mono_class_from_mono_type (c->type);

	/* Interface check requires a more complex setup so we
	 * only do for them. Otherwise we simply avoid mono_class_init.
	 */
	if (check_interfaces) {
		mono_class_init_or_throw (klass);
		mono_class_init_or_throw (klassc);
	} else if (!klass->supertypes || !klassc->supertypes) {
		mono_class_setup_supertypes (klass);
		mono_class_setup_supertypes (klassc);
	}

	if (type->type->byref)
		return klassc == mono_defaults.object_class;

	return mono_class_is_subclass_of (klass, klassc, check_interfaces);
}

static gboolean
mono_type_is_primitive (MonoType *type)
{
	return (type->type >= MONO_TYPE_BOOLEAN && type->type <= MONO_TYPE_R8) ||
			type-> type == MONO_TYPE_I || type->type == MONO_TYPE_U;
}

static MonoType*
mono_type_get_underlying_type_ignore_byref (MonoType *type)
{
	if (type->type == MONO_TYPE_VALUETYPE && type->data.klass->enumtype)
		return mono_class_enum_basetype (type->data.klass);
	if (type->type == MONO_TYPE_GENERICINST && type->data.generic_class->container_class->enumtype)
		return mono_class_enum_basetype (type->data.generic_class->container_class);
	return type;
}

ICALL_EXPORT guint32
ves_icall_type_is_assignable_from (MonoReflectionType *type, MonoReflectionType *c)
{
	MonoDomain *domain; 
	MonoClass *klass;
	MonoClass *klassc;

	g_assert (type != NULL);
	
	domain = ((MonoObject *)type)->vtable->domain;

	klass = mono_class_from_mono_type (type->type);
	klassc = mono_class_from_mono_type (c->type);

	if (type->type->byref ^ c->type->byref)
		return FALSE;

	if (type->type->byref) {
		MonoType *t = mono_type_get_underlying_type_ignore_byref (type->type);
		MonoType *ot = mono_type_get_underlying_type_ignore_byref (c->type);

		klass = mono_class_from_mono_type (t);
		klassc = mono_class_from_mono_type (ot);

		if (mono_type_is_primitive (t)) {
			return mono_type_is_primitive (ot) && klass->instance_size == klassc->instance_size;
		} else if (t->type == MONO_TYPE_VAR || t->type == MONO_TYPE_MVAR) {
			return t->type == ot->type && t->data.generic_param->num == ot->data.generic_param->num;
		} else if (t->type == MONO_TYPE_PTR || t->type == MONO_TYPE_FNPTR) {
			return t->type == ot->type;
		} else {
			 if (ot->type == MONO_TYPE_VAR || ot->type == MONO_TYPE_MVAR)
				 return FALSE;

			 if (klass->valuetype)
				return klass == klassc;
			return klass->valuetype == klassc->valuetype;
		}
	}
	return mono_class_is_assignable_from (klass, klassc);
}

ICALL_EXPORT guint32
ves_icall_type_IsInstanceOfType (MonoReflectionType *type, MonoObject *obj)
{
	MonoClass *klass = mono_class_from_mono_type (type->type);
	mono_class_init_or_throw (klass);
	return mono_object_isinst (obj, klass) != NULL;
}

ICALL_EXPORT guint32
ves_icall_get_attributes (MonoReflectionType *type)
{
	MonoClass *klass = mono_class_from_mono_type (type->type);
	return klass->flags;
}

ICALL_EXPORT MonoReflectionMarshalAsAttribute*
ves_icall_System_Reflection_FieldInfo_get_marshal_info (MonoReflectionField *field)
{
	MonoClass *klass = field->field->parent;
	MonoMarshalType *info;
	MonoType *ftype;
	int i;

	if (klass->generic_container ||
	    (klass->generic_class && klass->generic_class->context.class_inst->is_open))
		return NULL;

	ftype = mono_field_get_type (field->field);
	if (ftype && !(ftype->attrs & FIELD_ATTRIBUTE_HAS_FIELD_MARSHAL))
		return NULL;

	info = mono_marshal_load_type_info (klass);

	for (i = 0; i < info->num_fields; ++i) {
		if (info->fields [i].field == field->field) {
			if (!info->fields [i].mspec)
				return NULL;
			else
				return mono_reflection_marshal_as_attribute_from_marshal_spec (field->object.vtable->domain, klass, info->fields [i].mspec);
		}
	}

	return NULL;
}

ICALL_EXPORT MonoReflectionField*
ves_icall_System_Reflection_FieldInfo_internal_from_handle_type (MonoClassField *handle, MonoType *type)
{
	gboolean found = FALSE;
	MonoClass *klass;
	MonoClass *k;

	g_assert (handle);

	if (!type) {
		klass = handle->parent;
	} else {
		klass = mono_class_from_mono_type (type);

		/* Check that the field belongs to the class */
		for (k = klass; k; k = k->parent) {
			if (k == handle->parent) {
				found = TRUE;
				break;
			}
		}

		if (!found)
			/* The managed code will throw the exception */
			return NULL;
	}

	return mono_field_get_object (mono_domain_get (), klass, handle);
}

ICALL_EXPORT MonoArray*
ves_icall_System_Reflection_FieldInfo_GetTypeModifiers (MonoReflectionField *field, MonoBoolean optional)
{
	MonoError error;
	MonoType *type = mono_field_get_type_checked (field->field, &error);
	mono_error_raise_exception (&error);

	return type_array_from_modifiers (field->field->parent->image, type, optional);
}

ICALL_EXPORT int
vell_icall_get_method_attributes (MonoMethod *method)
{
	return method->flags;
}

ICALL_EXPORT void
ves_icall_get_method_info (MonoMethod *method, MonoMethodInfo *info)
{
	MonoError error;
	MonoDomain *domain = mono_domain_get ();
	MonoMethodSignature* sig;

	sig = mono_method_signature_checked (method, &error);
	if (!mono_error_ok (&error))
		mono_error_raise_exception (&error);


	MONO_STRUCT_SETREF (info, parent, mono_type_get_object (domain, &method->klass->byval_arg));
	MONO_STRUCT_SETREF (info, ret, mono_type_get_object (domain, sig->ret));
	info->attrs = method->flags;
	info->implattrs = method->iflags;
	if (sig->call_convention == MONO_CALL_DEFAULT)
		info->callconv = sig->sentinelpos >= 0 ? 2 : 1;
	else {
		if (sig->call_convention == MONO_CALL_VARARG || sig->sentinelpos >= 0)
			info->callconv = 2;
		else
			info->callconv = 1;
	}
	info->callconv |= (sig->hasthis << 5) | (sig->explicit_this << 6); 
}

ICALL_EXPORT MonoArray*
ves_icall_get_parameter_info (MonoMethod *method, MonoReflectionMethod *member)
{
	MonoDomain *domain = mono_domain_get (); 

	return mono_param_get_objects_internal (domain, method, member->reftype ? mono_class_from_mono_type (member->reftype->type) : NULL);
}

ICALL_EXPORT MonoReflectionMarshalAsAttribute*
ves_icall_System_MonoMethodInfo_get_retval_marshal (MonoMethod *method)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoReflectionMarshalAsAttribute* res = NULL;
	MonoMarshalSpec **mspecs;
	int i;

	mspecs = g_new (MonoMarshalSpec*, mono_method_signature (method)->param_count + 1);
	mono_method_get_marshal_info (method, mspecs);

	if (mspecs [0])
		res = mono_reflection_marshal_as_attribute_from_marshal_spec (domain, method->klass, mspecs [0]);
		
	for (i = mono_method_signature (method)->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);

	return res;
}

ICALL_EXPORT gint32
ves_icall_MonoField_GetFieldOffset (MonoReflectionField *field)
{
	MonoClass *parent = field->field->parent;
	if (!parent->size_inited)
		mono_class_init (parent);
	mono_class_setup_fields_locking (parent);

	return field->field->offset - sizeof (MonoObject);
}

ICALL_EXPORT MonoReflectionType*
ves_icall_MonoField_GetParentType (MonoReflectionField *field, MonoBoolean declaring)
{
	MonoClass *parent;

	parent = declaring? field->field->parent: field->klass;

	return mono_type_get_object (mono_object_domain (field), &parent->byval_arg);
}

ICALL_EXPORT MonoObject *
ves_icall_MonoField_GetValueInternal (MonoReflectionField *field, MonoObject *obj)
{	
	MonoClass *fklass = field->klass;
	MonoClassField *cf = field->field;
	MonoDomain *domain = mono_object_domain (field);

	if (fklass->image->assembly->ref_only)
		mono_raise_exception (mono_get_exception_invalid_operation (
					"It is illegal to get the value on a field on a type loaded using the ReflectionOnly methods."));

	if (mono_security_core_clr_enabled ())
		mono_security_core_clr_ensure_reflection_access_field (cf);

	return mono_field_get_value_object (domain, cf, obj);
}

ICALL_EXPORT void
ves_icall_MonoField_SetValueInternal (MonoReflectionField *field, MonoObject *obj, MonoObject *value)
{
	MonoError error;
	MonoClassField *cf = field->field;
	MonoType *type;
	gchar *v;

	if (field->klass->image->assembly->ref_only)
		mono_raise_exception (mono_get_exception_invalid_operation (
					"It is illegal to set the value on a field on a type loaded using the ReflectionOnly methods."));

	if (mono_security_core_clr_enabled ())
		mono_security_core_clr_ensure_reflection_access_field (cf);

	type = mono_field_get_type_checked (cf, &error);
	if (!mono_error_ok (&error))
		mono_error_raise_exception (&error);

	v = (gchar *) value;
	if (!type->byref) {
		switch (type->type) {
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
		case MONO_TYPE_R4:
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
		case MONO_TYPE_R8:
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_PTR:
			if (v != NULL)
				v += sizeof (MonoObject);
			break;
		case MONO_TYPE_STRING:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			/* Do nothing */
			break;
		case MONO_TYPE_GENERICINST: {
			MonoGenericClass *gclass = type->data.generic_class;
			g_assert (!gclass->context.class_inst->is_open);

			if (mono_class_is_nullable (mono_class_from_mono_type (type))) {
				MonoClass *nklass = mono_class_from_mono_type (type);
				MonoObject *nullable;

				/* 
				 * Convert the boxed vtype into a Nullable structure.
				 * This is complicated by the fact that Nullables have
				 * a variable structure.
				 */
				nullable = mono_object_new (mono_domain_get (), nklass);

				mono_nullable_init (mono_object_unbox (nullable), value, nklass);

				v = mono_object_unbox (nullable);
			}
			else 
				if (gclass->container_class->valuetype && (v != NULL))
					v += sizeof (MonoObject);
			break;
		}
		default:
			g_error ("type 0x%x not handled in "
				 "ves_icall_FieldInfo_SetValueInternal", type->type);
			return;
		}
	}

	if (type->attrs & FIELD_ATTRIBUTE_STATIC) {
		MonoVTable *vtable = mono_class_vtable_full (mono_object_domain (field), cf->parent, TRUE);
		if (!vtable->initialized)
			mono_runtime_class_init (vtable);
		mono_field_static_set_value (vtable, cf, v);
	} else {
		mono_field_set_value (obj, cf, v);
	}
}

ICALL_EXPORT MonoObject *
ves_icall_MonoField_GetRawConstantValue (MonoReflectionField *this)
{	
	MonoObject *o = NULL;
	MonoClassField *field = this->field;
	MonoClass *klass;
	MonoDomain *domain = mono_object_domain (this); 
	gchar *v;
	MonoTypeEnum def_type;
	const char *def_value;
	MonoType *t;
	MonoError error;

	mono_class_init (field->parent);

	t = mono_field_get_type_checked (field, &error);
	if (!mono_error_ok (&error))
		mono_error_raise_exception (&error);

	if (!(t->attrs & FIELD_ATTRIBUTE_HAS_DEFAULT))
		mono_raise_exception (mono_get_exception_invalid_operation (NULL));

	if (image_is_dynamic (field->parent->image)) {
		MonoClass *klass = field->parent;
		int fidx = field - klass->fields;

		g_assert (fidx >= 0 && fidx < klass->field.count);
		g_assert (klass->ext);
		g_assert (klass->ext->field_def_values);
		def_type = klass->ext->field_def_values [fidx].def_type;
		def_value = klass->ext->field_def_values [fidx].data;
		if (def_type == MONO_TYPE_END)
			mono_raise_exception (mono_get_exception_invalid_operation (NULL));
	} else {
		def_value = mono_class_get_field_default_value (field, &def_type);
		/* FIXME, maybe we should try to raise TLE if field->parent is broken */
		if (!def_value)
			mono_raise_exception (mono_get_exception_invalid_operation (NULL));
	}

	/*FIXME unify this with reflection.c:mono_get_object_from_blob*/
	switch (def_type) {
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
	case MONO_TYPE_R4:
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
	case MONO_TYPE_R8: {
		MonoType *t;

		/* boxed value type */
		t = g_new0 (MonoType, 1);
		t->type = def_type;
		klass = mono_class_from_mono_type (t);
		g_free (t);
		o = mono_object_new (domain, klass);
		v = ((gchar *) o) + sizeof (MonoObject);
		mono_get_constant_value_from_blob (domain, def_type, def_value, v);
		break;
	}
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:
		mono_get_constant_value_from_blob (domain, def_type, def_value, &o);
		break;
	default:
		g_assert_not_reached ();
	}

	return o;
}

ICALL_EXPORT MonoReflectionType*
ves_icall_MonoField_ResolveType (MonoReflectionField *ref_field)
{
	MonoError error;
	MonoClassField *field = ref_field->field;
	MonoType *type = mono_field_get_type_checked (field, &error);
	if (!mono_error_ok (&error))
		mono_error_raise_exception (&error);
	return mono_type_get_object (mono_object_domain (ref_field), type);
}

ICALL_EXPORT MonoReflectionType*
ves_icall_MonoGenericMethod_get_ReflectedType (MonoReflectionGenericMethod *rmethod)
{
	MonoMethod *method = rmethod->method.method;

	return mono_type_get_object (mono_object_domain (rmethod), &method->klass->byval_arg);
}

/* From MonoProperty.cs */
typedef enum {
	PInfo_Attributes = 1,
	PInfo_GetMethod  = 1 << 1,
	PInfo_SetMethod  = 1 << 2,
	PInfo_ReflectedType = 1 << 3,
	PInfo_DeclaringType = 1 << 4,
	PInfo_Name = 1 << 5
} PInfo;

ICALL_EXPORT void
ves_icall_get_property_info (MonoReflectionProperty *property, MonoPropertyInfo *info, PInfo req_info)
{
	MonoDomain *domain = mono_object_domain (property); 

	if ((req_info & PInfo_ReflectedType) != 0)
		MONO_STRUCT_SETREF (info, parent, mono_type_get_object (domain, &property->klass->byval_arg));
	if ((req_info & PInfo_DeclaringType) != 0)
		MONO_STRUCT_SETREF (info, declaring_type, mono_type_get_object (domain, &property->property->parent->byval_arg));

	if ((req_info & PInfo_Name) != 0)
		MONO_STRUCT_SETREF (info, name, mono_string_new (domain, property->property->name));

	if ((req_info & PInfo_Attributes) != 0)
		info->attrs = property->property->attrs;

	if ((req_info & PInfo_GetMethod) != 0)
		MONO_STRUCT_SETREF (info, get, property->property->get ?
							mono_method_get_object (domain, property->property->get, property->klass): NULL);
	
	if ((req_info & PInfo_SetMethod) != 0)
		MONO_STRUCT_SETREF (info, set, property->property->set ?
							mono_method_get_object (domain, property->property->set, property->klass): NULL);
	/* 
	 * There may be other methods defined for properties, though, it seems they are not exposed 
	 * in the reflection API 
	 */
}

ICALL_EXPORT void
ves_icall_get_event_info (MonoReflectionMonoEvent *event, MonoEventInfo *info)
{
	MonoDomain *domain = mono_object_domain (event); 

	MONO_STRUCT_SETREF (info, reflected_type, mono_type_get_object (domain, &event->klass->byval_arg));
	MONO_STRUCT_SETREF (info, declaring_type, mono_type_get_object (domain, &event->event->parent->byval_arg));

	MONO_STRUCT_SETREF (info, name, mono_string_new (domain, event->event->name));
	info->attrs = event->event->attrs;
	MONO_STRUCT_SETREF (info, add_method, event->event->add ? mono_method_get_object (domain, event->event->add, NULL): NULL);
	MONO_STRUCT_SETREF (info, remove_method, event->event->remove ? mono_method_get_object (domain, event->event->remove, NULL): NULL);
	MONO_STRUCT_SETREF (info, raise_method, event->event->raise ? mono_method_get_object (domain, event->event->raise, NULL): NULL);

#ifndef MONO_SMALL_CONFIG
	if (event->event->other) {
		int i, n = 0;
		while (event->event->other [n])
			n++;
		MONO_STRUCT_SETREF (info, other_methods, mono_array_new (domain, mono_defaults.method_info_class, n));

		for (i = 0; i < n; i++)
			mono_array_setref (info->other_methods, i, mono_method_get_object (domain, event->event->other [i], NULL));
	}		
#endif
}

static void
collect_interfaces (MonoClass *klass, GHashTable *ifaces, MonoError *error)
{
	int i;
	MonoClass *ic;

	mono_class_setup_interfaces (klass, error);
	if (!mono_error_ok (error))
		return;

	for (i = 0; i < klass->interface_count; i++) {
		ic = klass->interfaces [i];
		g_hash_table_insert (ifaces, ic, ic);

		collect_interfaces (ic, ifaces, error);
		if (!mono_error_ok (error))
			return;
	}
}

typedef struct {
	MonoArray *iface_array;
	MonoGenericContext *context;
	MonoError *error;
	MonoDomain *domain;
	int next_idx;
} FillIfaceArrayData;

static void
fill_iface_array (gpointer key, gpointer value, gpointer user_data)
{
	FillIfaceArrayData *data = user_data;
	MonoClass *ic = key;
	MonoType *ret = &ic->byval_arg, *inflated = NULL;

	if (!mono_error_ok (data->error))
		return;

	if (data->context && ic->generic_class && ic->generic_class->context.class_inst->is_open) {
		inflated = ret = mono_class_inflate_generic_type_checked (ret, data->context, data->error);
		if (!mono_error_ok (data->error))
			return;
	}

	mono_array_setref (data->iface_array, data->next_idx++, mono_type_get_object (data->domain, ret));

	if (inflated)
		mono_metadata_free_type (inflated);
}

ICALL_EXPORT MonoArray*
ves_icall_Type_GetInterfaces (MonoReflectionType* type)
{
	MonoError error;
	MonoClass *class = mono_class_from_mono_type (type->type);
	MonoClass *parent;
	FillIfaceArrayData data = { 0 };
	int len;

	GHashTable *iface_hash = g_hash_table_new (NULL, NULL);

	if (class->generic_class && class->generic_class->context.class_inst->is_open) {
		data.context = mono_class_get_context (class);
		class = class->generic_class->container_class;
	}

	for (parent = class; parent; parent = parent->parent) {
		mono_class_setup_interfaces (parent, &error);
		if (!mono_error_ok (&error))
			goto fail;
		collect_interfaces (parent, iface_hash, &error);
		if (!mono_error_ok (&error))
			goto fail;
	}

	data.error = &error;
	data.domain = mono_object_domain (type);

	len = g_hash_table_size (iface_hash);
	if (len == 0) {
		g_hash_table_destroy (iface_hash);
		if (!data.domain->empty_types)
			data.domain->empty_types = mono_array_new_cached (data.domain, mono_defaults.monotype_class, 0);
		return data.domain->empty_types;
	}

	data.iface_array = mono_array_new_cached (data.domain, mono_defaults.monotype_class, len);
	g_hash_table_foreach (iface_hash, fill_iface_array, &data);
	if (!mono_error_ok (&error))
		goto fail;

	g_hash_table_destroy (iface_hash);
	return data.iface_array;

fail:
	g_hash_table_destroy (iface_hash);
	mono_error_raise_exception (&error);
	return NULL;
}

ICALL_EXPORT void
ves_icall_Type_GetInterfaceMapData (MonoReflectionType *type, MonoReflectionType *iface, MonoArray **targets, MonoArray **methods)
{
	gboolean variance_used;
	MonoClass *class = mono_class_from_mono_type (type->type);
	MonoClass *iclass = mono_class_from_mono_type (iface->type);
	MonoReflectionMethod *member;
	MonoMethod* method;
	gpointer iter;
	int i = 0, len, ioffset;
	MonoDomain *domain;

	mono_class_init_or_throw (class);
	mono_class_init_or_throw (iclass);

	mono_class_setup_vtable (class);

	ioffset = mono_class_interface_offset_with_variance (class, iclass, &variance_used);
	if (ioffset == -1)
		return;

	len = mono_class_num_methods (iclass);
	domain = mono_object_domain (type);
	mono_gc_wbarrier_generic_store (targets, (MonoObject*) mono_array_new (domain, mono_defaults.method_info_class, len));
	mono_gc_wbarrier_generic_store (methods, (MonoObject*) mono_array_new (domain, mono_defaults.method_info_class, len));
	iter = NULL;
	while ((method = mono_class_get_methods (iclass, &iter))) {
		member = mono_method_get_object (domain, method, iclass);
		mono_array_setref (*methods, i, member);
		member = mono_method_get_object (domain, class->vtable [i + ioffset], class);
		mono_array_setref (*targets, i, member);
		
		i ++;
	}
}

ICALL_EXPORT void
ves_icall_Type_GetPacking (MonoReflectionType *type, guint32 *packing, guint32 *size)
{
	MonoClass *klass = mono_class_from_mono_type (type->type);
	mono_class_init_or_throw (klass);

	if (image_is_dynamic (klass->image)) {
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder*)type;
		*packing = tb->packing_size;
		*size = tb->class_size;
	} else {
		mono_metadata_packing_from_typedef (klass->image, klass->type_token, packing, size);
	}
}

ICALL_EXPORT MonoReflectionType*
ves_icall_MonoType_GetElementType (MonoReflectionType *type)
{
	MonoClass *class;

	if (!type->type->byref && type->type->type == MONO_TYPE_SZARRAY)
		return mono_type_get_object (mono_object_domain (type), &type->type->data.klass->byval_arg);

	class = mono_class_from_mono_type (type->type);
	mono_class_init_or_throw (class);

	// GetElementType should only return a type for:
	// Array Pointer PassedByRef
	if (type->type->byref)
		return mono_type_get_object (mono_object_domain (type), &class->byval_arg);
	else if (class->element_class && MONO_CLASS_IS_ARRAY (class))
		return mono_type_get_object (mono_object_domain (type), &class->element_class->byval_arg);
	else if (class->element_class && type->type->type == MONO_TYPE_PTR)
		return mono_type_get_object (mono_object_domain (type), &class->element_class->byval_arg);
	else
		return NULL;
}

ICALL_EXPORT MonoReflectionType*
ves_icall_get_type_parent (MonoReflectionType *type)
{
	MonoClass *class = mono_class_from_mono_type (type->type);
	return class->parent ? mono_type_get_object (mono_object_domain (type), &class->parent->byval_arg): NULL;
}

ICALL_EXPORT MonoBoolean
ves_icall_type_ispointer (MonoReflectionType *type)
{
	return type->type->type == MONO_TYPE_PTR;
}

ICALL_EXPORT MonoBoolean
ves_icall_type_isprimitive (MonoReflectionType *type)
{
	return (!type->type->byref && (((type->type->type >= MONO_TYPE_BOOLEAN) && (type->type->type <= MONO_TYPE_R8)) || (type->type->type == MONO_TYPE_I) || (type->type->type == MONO_TYPE_U)));
}

ICALL_EXPORT MonoBoolean
ves_icall_type_isbyref (MonoReflectionType *type)
{
	return type->type->byref;
}

ICALL_EXPORT MonoBoolean
ves_icall_type_iscomobject (MonoReflectionType *type)
{
	MonoClass *klass = mono_class_from_mono_type (type->type);
	mono_class_init_or_throw (klass);

	return mono_class_is_com_object (klass);
}

ICALL_EXPORT MonoReflectionModule*
ves_icall_MonoType_get_Module (MonoReflectionType *type)
{
	MonoClass *class = mono_class_from_mono_type (type->type);
	return mono_module_get_object (mono_object_domain (type), class->image);
}

ICALL_EXPORT MonoReflectionAssembly*
ves_icall_MonoType_get_Assembly (MonoReflectionType *type)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *class = mono_class_from_mono_type (type->type);
	return mono_assembly_get_object (domain, class->image->assembly);
}

ICALL_EXPORT MonoReflectionType*
ves_icall_MonoType_get_DeclaringType (MonoReflectionType *type)
{
	MonoDomain *domain = mono_domain_get ();
	MonoClass *class;

	if (type->type->byref)
		return NULL;
	if (type->type->type == MONO_TYPE_VAR) {
		MonoGenericContainer *param = mono_type_get_generic_param_owner (type->type);
		class = param ? param->owner.klass : NULL;
	} else if (type->type->type == MONO_TYPE_MVAR) {
		MonoGenericContainer *param = mono_type_get_generic_param_owner (type->type);
		class = param ? param->owner.method->klass : NULL;
	} else {
		class = mono_class_from_mono_type (type->type)->nested_in;
	}

	return class ? mono_type_get_object (domain, &class->byval_arg) : NULL;
}

ICALL_EXPORT MonoString*
ves_icall_MonoType_get_Name (MonoReflectionType *type)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *class = mono_class_from_mono_type (type->type);

	if (type->type->byref) {
		char *n = g_strdup_printf ("%s&", class->name);
		MonoString *res = mono_string_new (domain, n);

		g_free (n);

		return res;
	} else {
		return mono_string_new (domain, class->name);
	}
}

ICALL_EXPORT MonoString*
ves_icall_MonoType_get_Namespace (MonoReflectionType *type)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *class = mono_class_from_mono_type (type->type);

	while (class->nested_in)
		class = class->nested_in;

	if (class->name_space [0] == '\0')
		return NULL;
	else
		return mono_string_new (domain, class->name_space);
}

ICALL_EXPORT gint32
ves_icall_MonoType_GetArrayRank (MonoReflectionType *type)
{
	MonoClass *class;

	if (type->type->type != MONO_TYPE_ARRAY && type->type->type != MONO_TYPE_SZARRAY)
		mono_raise_exception (mono_get_exception_argument ("type", "Type must be an array type"));

	class = mono_class_from_mono_type (type->type);

	return class->rank;
}

ICALL_EXPORT MonoArray*
ves_icall_MonoType_GetGenericArguments (MonoReflectionType *type)
{
	MonoArray *res;
	MonoClass *klass, *pklass;
	MonoDomain *domain = mono_object_domain (type);
	MonoVTable *array_vtable = mono_class_vtable_full (domain, mono_array_class_get_cached (mono_defaults.systemtype_class, 1), TRUE);
	int i;

	klass = mono_class_from_mono_type (type->type);

	if (klass->generic_container) {
		MonoGenericContainer *container = klass->generic_container;
		res = mono_array_new_specific (array_vtable, container->type_argc);
		for (i = 0; i < container->type_argc; ++i) {
			pklass = mono_class_from_generic_parameter (mono_generic_container_get_param (container, i), klass->image, FALSE);
			mono_array_setref (res, i, mono_type_get_object (domain, &pklass->byval_arg));
		}
	} else if (klass->generic_class) {
		MonoGenericInst *inst = klass->generic_class->context.class_inst;
		res = mono_array_new_specific (array_vtable, inst->type_argc);
		for (i = 0; i < inst->type_argc; ++i)
			mono_array_setref (res, i, mono_type_get_object (domain, inst->type_argv [i]));
	} else {
		res = mono_array_new_specific (array_vtable, 0);
	}
	return res;
}

ICALL_EXPORT gboolean
ves_icall_Type_get_IsGenericTypeDefinition (MonoReflectionType *type)
{
	MonoClass *klass;

	if (!IS_MONOTYPE (type))
		return FALSE;

	if (type->type->byref)
		return FALSE;

	klass = mono_class_from_mono_type (type->type);
	return klass->generic_container != NULL;
}

ICALL_EXPORT MonoReflectionType*
ves_icall_Type_GetGenericTypeDefinition_impl (MonoReflectionType *type)
{
	MonoClass *klass;

	if (type->type->byref)
		return NULL;

	klass = mono_class_from_mono_type (type->type);

	if (klass->generic_container) {
		return type; /* check this one */
	}
	if (klass->generic_class) {
		MonoClass *generic_class = klass->generic_class->container_class;
		gpointer tb;

		tb = mono_class_get_ref_info (generic_class);

		if (generic_class->wastypebuilder && tb)
			return tb;
		else
			return mono_type_get_object (mono_object_domain (type), &generic_class->byval_arg);
	}
	return NULL;
}

ICALL_EXPORT MonoReflectionType*
ves_icall_Type_MakeGenericType (MonoReflectionType *type, MonoArray *type_array)
{
	MonoClass *class;
	MonoType *geninst, **types;
	int i, count;

	g_assert (IS_MONOTYPE (type));
	mono_class_init_or_throw (mono_class_from_mono_type (type->type));

	count = mono_array_length (type_array);
	types = g_new0 (MonoType *, count);

	for (i = 0; i < count; i++) {
		MonoReflectionType *t = mono_array_get (type_array, gpointer, i);
		types [i] = t->type;
	}

	geninst = mono_reflection_bind_generic_parameters (type, count, types);
	g_free (types);
	if (!geninst)
		return NULL;

	class = mono_class_from_mono_type (geninst);

	/*we might inflate to the GTD*/
	if (class->generic_class && !mono_verifier_class_is_valid_generic_instantiation (class))
		mono_raise_exception (mono_get_exception_argument ("typeArguments", "Invalid generic arguments"));

	return mono_type_get_object (mono_object_domain (type), geninst);
}

ICALL_EXPORT gboolean
ves_icall_Type_get_IsGenericInstance (MonoReflectionType *type)
{
	MonoClass *klass;

	if (type->type->byref)
		return FALSE;

	klass = mono_class_from_mono_type (type->type);

	return klass->generic_class != NULL;
}

ICALL_EXPORT gboolean
ves_icall_Type_get_IsGenericType (MonoReflectionType *type)
{
	MonoClass *klass;

	if (!IS_MONOTYPE (type))
		return FALSE;

	if (type->type->byref)
		return FALSE;

	klass = mono_class_from_mono_type (type->type);
	return klass->generic_class != NULL || klass->generic_container != NULL;
}

ICALL_EXPORT gint32
ves_icall_Type_GetGenericParameterPosition (MonoReflectionType *type)
{
	if (!IS_MONOTYPE (type))
		return -1;

	if (is_generic_parameter (type->type))
		return mono_type_get_generic_param_num (type->type);
	return -1;
}

ICALL_EXPORT GenericParameterAttributes
ves_icall_Type_GetGenericParameterAttributes (MonoReflectionType *type)
{
	g_assert (IS_MONOTYPE (type));
	g_assert (is_generic_parameter (type->type));
	return mono_generic_param_info (type->type->data.generic_param)->flags;
}

ICALL_EXPORT MonoArray *
ves_icall_Type_GetGenericParameterConstraints (MonoReflectionType *type)
{
	MonoGenericParamInfo *param_info;
	MonoDomain *domain;
	MonoClass **ptr;
	MonoArray *res;
	int i, count;

	g_assert (IS_MONOTYPE (type));

	domain = mono_object_domain (type);
	param_info = mono_generic_param_info (type->type->data.generic_param);
	for (count = 0, ptr = param_info->constraints; ptr && *ptr; ptr++, count++)
		;

	res = mono_array_new (domain, mono_defaults.monotype_class, count);
	for (i = 0; i < count; i++)
		mono_array_setref (res, i, mono_type_get_object (domain, &param_info->constraints [i]->byval_arg));


	return res;
}

ICALL_EXPORT MonoBoolean
ves_icall_MonoType_get_IsGenericParameter (MonoReflectionType *type)
{
	return is_generic_parameter (type->type);
}

ICALL_EXPORT MonoBoolean
ves_icall_TypeBuilder_get_IsGenericParameter (MonoReflectionTypeBuilder *tb)
{
	return is_generic_parameter (tb->type.type);
}

ICALL_EXPORT void
ves_icall_EnumBuilder_setup_enum_type (MonoReflectionType *enumtype,
									   MonoReflectionType *t)
{
	enumtype->type = t->type;
}

ICALL_EXPORT MonoReflectionMethod*
ves_icall_MonoType_GetCorrespondingInflatedMethod (MonoReflectionType *type, 
                                                   MonoReflectionMethod* generic)
{
	MonoDomain *domain; 
	MonoClass *klass;
	MonoMethod *method;
	gpointer iter;
		
	domain = ((MonoObject *)type)->vtable->domain;

	klass = mono_class_from_mono_type (type->type);
	mono_class_init_or_throw (klass);

	iter = NULL;
	while ((method = mono_class_get_methods (klass, &iter))) {
                if (method->token == generic->method->token)
                        return mono_method_get_object (domain, method, klass);
        }

        return NULL;
}



ICALL_EXPORT MonoReflectionMethod *
ves_icall_MonoType_get_DeclaringMethod (MonoReflectionType *ref_type)
{
	MonoMethod *method;
	MonoType *type = ref_type->type;

	if (type->byref || (type->type != MONO_TYPE_MVAR && type->type != MONO_TYPE_VAR))
		mono_raise_exception (mono_get_exception_invalid_operation ("DeclaringMethod can only be used on generic arguments"));
	if (type->type == MONO_TYPE_VAR)
		return NULL;

	method = mono_type_get_generic_param_owner (type)->owner.method;
	g_assert (method);
	return mono_method_get_object (mono_object_domain (ref_type), method, method->klass);
}

ICALL_EXPORT MonoReflectionDllImportAttribute*
ves_icall_MonoMethod_GetDllImportAttribute (MonoMethod *method)
{
	static MonoClass *DllImportAttributeClass = NULL;
	MonoDomain *domain = mono_domain_get ();
	MonoReflectionDllImportAttribute *attr;
	MonoImage *image = method->klass->image;
	MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *)method;
	MonoTableInfo *tables = image->tables;
	MonoTableInfo *im = &tables [MONO_TABLE_IMPLMAP];
	MonoTableInfo *mr = &tables [MONO_TABLE_MODULEREF];
	guint32 im_cols [MONO_IMPLMAP_SIZE];
	guint32 scope_token;
	const char *import = NULL;
	const char *scope = NULL;
	guint32 flags;

	if (!(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		return NULL;

	if (!DllImportAttributeClass) {
		DllImportAttributeClass = 
			mono_class_from_name (mono_defaults.corlib,
								  "System.Runtime.InteropServices", "DllImportAttribute");
		g_assert (DllImportAttributeClass);
	}
														
	if (image_is_dynamic (method->klass->image)) {
		MonoReflectionMethodAux *method_aux = 
			g_hash_table_lookup (
									  ((MonoDynamicImage*)method->klass->image)->method_aux_hash, method);
		if (method_aux) {
			import = method_aux->dllentry;
			scope = method_aux->dll;
		}

		if (!import || !scope) {
			mono_raise_exception (mono_get_exception_argument ("method", "System.Reflection.Emit method with invalid pinvoke information"));
			return NULL;
		}
	}
	else {
		if (piinfo->implmap_idx) {
			mono_metadata_decode_row (im, piinfo->implmap_idx - 1, im_cols, MONO_IMPLMAP_SIZE);
			
			piinfo->piflags = im_cols [MONO_IMPLMAP_FLAGS];
			import = mono_metadata_string_heap (image, im_cols [MONO_IMPLMAP_NAME]);
			scope_token = mono_metadata_decode_row_col (mr, im_cols [MONO_IMPLMAP_SCOPE] - 1, MONO_MODULEREF_NAME);
			scope = mono_metadata_string_heap (image, scope_token);
		}
	}
	flags = piinfo->piflags;
	
	attr = (MonoReflectionDllImportAttribute*)mono_object_new (domain, DllImportAttributeClass);

	MONO_OBJECT_SETREF (attr, dll, mono_string_new (domain, scope));
	MONO_OBJECT_SETREF (attr, entry_point, mono_string_new (domain, import));
	attr->call_conv = (flags & 0x700) >> 8;
	attr->charset = ((flags & 0x6) >> 1) + 1;
	if (attr->charset == 1)
		attr->charset = 2;
	attr->exact_spelling = (flags & 0x1) != 0;
	attr->set_last_error = (flags & 0x40) != 0;
	attr->best_fit_mapping = (flags & 0x30) == 0x10;
	attr->throw_on_unmappable = (flags & 0x3000) == 0x1000;
	attr->preserve_sig = FALSE;

	return attr;
}

ICALL_EXPORT MonoReflectionMethod *
ves_icall_MonoMethod_GetGenericMethodDefinition (MonoReflectionMethod *method)
{
	MonoMethodInflated *imethod;
	MonoMethod *result;

	if (method->method->is_generic)
		return method;

	if (!method->method->is_inflated)
		return NULL;

	imethod = (MonoMethodInflated *) method->method;

	result = imethod->declaring;
	/* Not a generic method.  */
	if (!result->is_generic)
		return NULL;

	if (image_is_dynamic (method->method->klass->image)) {
		MonoDynamicImage *image = (MonoDynamicImage*)method->method->klass->image;
		MonoReflectionMethod *res;

		/*
		 * FIXME: Why is this stuff needed at all ? Why can't the code below work for
		 * the dynamic case as well ?
		 */
		mono_image_lock ((MonoImage*)image);
		res = mono_g_hash_table_lookup (image->generic_def_objects, imethod);
		mono_image_unlock ((MonoImage*)image);

		if (res)
			return res;
	}

	if (imethod->context.class_inst) {
		MonoClass *klass = ((MonoMethod *) imethod)->klass;
		/*Generic methods gets the context of the GTD.*/
		if (mono_class_get_context (klass)) {
			MonoError error;
			result = mono_class_inflate_generic_method_full_checked (result, klass, mono_class_get_context (klass), &error);
			mono_error_raise_exception (&error);
		}
	}

	return mono_method_get_object (mono_object_domain (method), result, NULL);
}

ICALL_EXPORT gboolean
ves_icall_MonoMethod_get_IsGenericMethod (MonoReflectionMethod *method)
{
	return mono_method_signature (method->method)->generic_param_count != 0;
}

ICALL_EXPORT gboolean
ves_icall_MonoMethod_get_IsGenericMethodDefinition (MonoReflectionMethod *method)
{
	return method->method->is_generic;
}

ICALL_EXPORT MonoArray*
ves_icall_MonoMethod_GetGenericArguments (MonoReflectionMethod *method)
{
	MonoArray *res;
	MonoDomain *domain;
	int count, i;

	domain = mono_object_domain (method);

	if (method->method->is_inflated) {
		MonoGenericInst *inst = mono_method_get_context (method->method)->method_inst;

		if (inst) {
			count = inst->type_argc;
			res = mono_array_new (domain, mono_defaults.systemtype_class, count);

			for (i = 0; i < count; i++)
				mono_array_setref (res, i, mono_type_get_object (domain, inst->type_argv [i]));

			return res;
		}
	}

	count = mono_method_signature (method->method)->generic_param_count;
	res = mono_array_new (domain, mono_defaults.systemtype_class, count);

	for (i = 0; i < count; i++) {
		MonoGenericContainer *container = mono_method_get_generic_container (method->method);
		MonoGenericParam *param = mono_generic_container_get_param (container, i);
		MonoClass *pklass = mono_class_from_generic_parameter (
			param, method->method->klass->image, TRUE);
		mono_array_setref (res, i,
				mono_type_get_object (domain, &pklass->byval_arg));
	}

	return res;
}

ICALL_EXPORT MonoObject *
ves_icall_InternalInvoke (MonoReflectionMethod *method, MonoObject *this, MonoArray *params, MonoException **exc) 
{
	/* 
	 * Invoke from reflection is supposed to always be a virtual call (the API
	 * is stupid), mono_runtime_invoke_*() calls the provided method, allowing
	 * greater flexibility.
	 */
	MonoMethod *m = method->method;
	MonoMethodSignature *sig = mono_method_signature (m);
	MonoImage *image;
	int pcount;
	void *obj = this;

	*exc = NULL;

	if (mono_security_core_clr_enabled ())
		mono_security_core_clr_ensure_reflection_access_method (m);

	if (!(m->flags & METHOD_ATTRIBUTE_STATIC)) {
		if (!mono_class_vtable_full (mono_object_domain (method), m->klass, FALSE)) {
			mono_gc_wbarrier_generic_store (exc, (MonoObject*) mono_class_get_exception_for_failure (m->klass));
			return NULL;
		}

		if (this) {
			if (!mono_object_isinst (this, m->klass)) {
				char *this_name = mono_type_get_full_name (mono_object_get_class (this));
				char *target_name = mono_type_get_full_name (m->klass);
				char *msg = g_strdup_printf ("Object of type '%s' doesn't match target type '%s'", this_name, target_name);
				mono_gc_wbarrier_generic_store (exc, (MonoObject*) mono_exception_from_name_msg (mono_defaults.corlib, "System.Reflection", "TargetException", msg));
				g_free (msg);
				g_free (target_name);
				g_free (this_name);
				return NULL;
			}
			m = mono_object_get_virtual_method (this, m);
			/* must pass the pointer to the value for valuetype methods */
			if (m->klass->valuetype)
				obj = mono_object_unbox (this);
		} else if (strcmp (m->name, ".ctor") && !m->wrapper_type) {
			mono_gc_wbarrier_generic_store (exc, (MonoObject*) mono_exception_from_name_msg (mono_defaults.corlib, "System.Reflection", "TargetException", "Non-static method requires a target."));
			return NULL;
		}
	}

	if (sig->ret->byref) {
		mono_gc_wbarrier_generic_store (exc, (MonoObject*) mono_exception_from_name_msg (mono_defaults.corlib, "System", "NotSupportedException", "Cannot invoke method returning ByRef type via reflection"));
		return NULL;
	}

	pcount = params? mono_array_length (params): 0;
	if (pcount != sig->param_count) {
		mono_gc_wbarrier_generic_store (exc, (MonoObject*) mono_exception_from_name (mono_defaults.corlib, "System.Reflection", "TargetParameterCountException"));
		return NULL;
	}

	if ((m->klass->flags & TYPE_ATTRIBUTE_ABSTRACT) && !strcmp (m->name, ".ctor") && !this) {
		mono_gc_wbarrier_generic_store (exc, (MonoObject*) mono_exception_from_name_msg (mono_defaults.corlib, "System.Reflection", "TargetException", "Cannot invoke constructor of an abstract class."));
		return NULL;
	}

	image = m->klass->image;
	if (image->assembly->ref_only) {
		mono_gc_wbarrier_generic_store (exc, (MonoObject*) mono_get_exception_invalid_operation ("It is illegal to invoke a method on a type loaded using the ReflectionOnly api."));
		return NULL;
	}

	if (image_is_dynamic (image) && !((MonoDynamicImage*)image)->run) {
		mono_gc_wbarrier_generic_store (exc, (MonoObject*) mono_get_exception_not_supported ("Cannot invoke a method in a dynamic assembly without run access."));
		return NULL;
	}
	
	if (m->klass->rank && !strcmp (m->name, ".ctor")) {
		int i;
		uintptr_t *lengths;
		intptr_t *lower_bounds;
		pcount = mono_array_length (params);
		lengths = alloca (sizeof (uintptr_t) * pcount);
		/* Note: the synthetized array .ctors have int32 as argument type */
		for (i = 0; i < pcount; ++i)
			lengths [i] = *(int32_t*) ((char*)mono_array_get (params, gpointer, i) + sizeof (MonoObject));

		if (m->klass->rank == 1 && sig->param_count == 2 && m->klass->element_class->rank) {
			/* This is a ctor for jagged arrays. MS creates an array of arrays. */
			MonoArray *arr = mono_array_new_full (mono_object_domain (params), m->klass, lengths, NULL);

			for (i = 0; i < mono_array_length (arr); ++i) {
				MonoArray *subarray = mono_array_new_full (mono_object_domain (params), m->klass->element_class, &lengths [1], NULL);

				mono_array_setref_fast (arr, i, subarray);
			}
			return (MonoObject*)arr;
		}

		if (m->klass->rank == pcount) {
			/* Only lengths provided. */
			lower_bounds = NULL;
		} else {
			g_assert (pcount == (m->klass->rank * 2));
			/* lower bounds are first. */
			lower_bounds = (intptr_t*)lengths;
			lengths += m->klass->rank;
		}

		return (MonoObject*)mono_array_new_full (mono_object_domain (params), m->klass, lengths, lower_bounds);
	}
	return mono_runtime_invoke_array (m, obj, params, NULL);
}

#ifndef DISABLE_REMOTING
ICALL_EXPORT MonoObject *
ves_icall_InternalExecute (MonoReflectionMethod *method, MonoObject *this, MonoArray *params, MonoArray **outArgs) 
{
	MonoDomain *domain = mono_object_domain (method); 
	MonoMethod *m = method->method;
	MonoMethodSignature *sig = mono_method_signature (m);
	MonoArray *out_args;
	MonoObject *result;
	int i, j, outarg_count = 0;

	if (m->klass == mono_defaults.object_class) {
		if (!strcmp (m->name, "FieldGetter")) {
			MonoClass *k = this->vtable->klass;
			MonoString *name;
			char *str;
			
			/* If this is a proxy, then it must be a CBO */
			if (k == mono_defaults.transparent_proxy_class) {
				MonoTransparentProxy *tp = (MonoTransparentProxy*) this;
				this = tp->rp->unwrapped_server;
				g_assert (this);
				k = this->vtable->klass;
			}
			
			name = mono_array_get (params, MonoString *, 1);
			str = mono_string_to_utf8 (name);
		
			do {
				MonoClassField* field = mono_class_get_field_from_name (k, str);
				if (field) {
					MonoClass *field_klass =  mono_class_from_mono_type (field->type);
					if (field_klass->valuetype)
						result = mono_value_box (domain, field_klass, (char *)this + field->offset);
					else 
						result = *((gpointer *)((char *)this + field->offset));
				
					out_args = mono_array_new (domain, mono_defaults.object_class, 1);
					mono_gc_wbarrier_generic_store (outArgs, (MonoObject*) out_args);
					mono_array_setref (out_args, 0, result);
					g_free (str);
					return NULL;
				}
				k = k->parent;
			} while (k);

			g_free (str);
			g_assert_not_reached ();

		} else if (!strcmp (m->name, "FieldSetter")) {
			MonoClass *k = this->vtable->klass;
			MonoString *name;
			guint32 size;
			gint32 align;
			char *str;
			
			/* If this is a proxy, then it must be a CBO */
			if (k == mono_defaults.transparent_proxy_class) {
				MonoTransparentProxy *tp = (MonoTransparentProxy*) this;
				this = tp->rp->unwrapped_server;
				g_assert (this);
				k = this->vtable->klass;
			}
			
			name = mono_array_get (params, MonoString *, 1);
			str = mono_string_to_utf8 (name);
		
			do {
				MonoClassField* field = mono_class_get_field_from_name (k, str);
				if (field) {
					MonoClass *field_klass =  mono_class_from_mono_type (field->type);
					MonoObject *val = mono_array_get (params, gpointer, 2);

					if (field_klass->valuetype) {
						size = mono_type_size (field->type, &align);
						g_assert (size == mono_class_value_size (field_klass, NULL));
						mono_gc_wbarrier_value_copy ((char *)this + field->offset, (char*)val + sizeof (MonoObject), 1, field_klass);
					} else {
						mono_gc_wbarrier_set_field (this, (char*)this + field->offset, val);
					}
				
					out_args = mono_array_new (domain, mono_defaults.object_class, 0);
					mono_gc_wbarrier_generic_store (outArgs, (MonoObject*) out_args);

					g_free (str);
					return NULL;
				}
				
				k = k->parent;
			} while (k);

			g_free (str);
			g_assert_not_reached ();

		}
	}

	for (i = 0; i < mono_array_length (params); i++) {
		if (sig->params [i]->byref) 
			outarg_count++;
	}

	out_args = mono_array_new (domain, mono_defaults.object_class, outarg_count);
	
	/* handle constructors only for objects already allocated */
	if (!strcmp (method->method->name, ".ctor"))
		g_assert (this);

	/* This can be called only on MBR objects, so no need to unbox for valuetypes. */
	g_assert (!method->method->klass->valuetype);
	result = mono_runtime_invoke_array (method->method, this, params, NULL);

	for (i = 0, j = 0; i < mono_array_length (params); i++) {
		if (sig->params [i]->byref) {
			gpointer arg;
			arg = mono_array_get (params, gpointer, i);
			mono_array_setref (out_args, j, arg);
			j++;
		}
	}

	mono_gc_wbarrier_generic_store (outArgs, (MonoObject*) out_args);

	return result;
}
#endif

static guint64
read_enum_value (char *mem, int type)
{
	switch (type) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_U1:
		return *(guint8*)mem;
	case MONO_TYPE_I1:
		return *(gint8*)mem;
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U2:
		return *(guint16*)mem;
	case MONO_TYPE_I2:
		return *(gint16*)mem;
	case MONO_TYPE_U4:
		return *(guint32*)mem;
	case MONO_TYPE_I4:
		return *(gint32*)mem;
	case MONO_TYPE_U8:
		return *(guint64*)mem;
	case MONO_TYPE_I8:
		return *(gint64*)mem;
	default:
		g_assert_not_reached ();
	}
	return 0;
}

static void
write_enum_value (char *mem, int type, guint64 value)
{
	switch (type) {
	case MONO_TYPE_U1:
	case MONO_TYPE_I1: {
		guint8 *p = (guint8*)mem;
		*p = value;
		break;
	}
	case MONO_TYPE_U2:
	case MONO_TYPE_I2: {
		guint16 *p = (void*)mem;
		*p = value;
		break;
	}
	case MONO_TYPE_U4:
	case MONO_TYPE_I4: {
		guint32 *p = (void*)mem;
		*p = value;
		break;
	}
	case MONO_TYPE_U8:
	case MONO_TYPE_I8: {
		guint64 *p = (void*)mem;
		*p = value;
		break;
	}
	default:
		g_assert_not_reached ();
	}
	return;
}

ICALL_EXPORT MonoObject *
ves_icall_System_Enum_ToObject (MonoReflectionType *enumType, MonoObject *value)
{
	MonoDomain *domain; 
	MonoClass *enumc, *objc;
	MonoObject *res;
	MonoType *etype;
	guint64 val;
	
	MONO_CHECK_ARG_NULL (enumType);
	MONO_CHECK_ARG_NULL (value);

	domain = mono_object_domain (enumType); 
	enumc = mono_class_from_mono_type (enumType->type);

	mono_class_init_or_throw (enumc);

	objc = value->vtable->klass;

	if (!enumc->enumtype)
		mono_raise_exception (mono_get_exception_argument ("enumType", "Type provided must be an Enum."));
	if (!((objc->enumtype) || (objc->byval_arg.type >= MONO_TYPE_BOOLEAN && objc->byval_arg.type <= MONO_TYPE_U8)))
		mono_raise_exception (mono_get_exception_argument ("value", "The value passed in must be an enum base or an underlying type for an enum, such as an Int32."));

	etype = mono_class_enum_basetype (enumc);
	if (!etype)
		/* MS throws this for typebuilders */
		mono_raise_exception (mono_get_exception_argument ("Type must be a type provided by the runtime.", "enumType"));

	res = mono_object_new (domain, enumc);
	val = read_enum_value ((char *)value + sizeof (MonoObject), objc->enumtype? mono_class_enum_basetype (objc)->type: objc->byval_arg.type);
	write_enum_value ((char *)res + sizeof (MonoObject), etype->type, val);

	return res;
}

ICALL_EXPORT MonoBoolean
ves_icall_System_Enum_InternalHasFlag (MonoObject *a, MonoObject *b)
{
	int size = mono_class_value_size (a->vtable->klass, NULL);
	guint64 a_val = 0, b_val = 0;

	memcpy (&a_val, mono_object_unbox (a), size);
	memcpy (&b_val, mono_object_unbox (b), size);

	return (a_val & b_val) == b_val;
}

ICALL_EXPORT MonoObject *
ves_icall_System_Enum_get_value (MonoObject *this)
{
	MonoObject *res;
	MonoClass *enumc;
	gpointer dst;
	gpointer src;
	int size;

	if (!this)
		return NULL;

	g_assert (this->vtable->klass->enumtype);
	
	enumc = mono_class_from_mono_type (mono_class_enum_basetype (this->vtable->klass));
	res = mono_object_new (mono_object_domain (this), enumc);
	dst = (char *)res + sizeof (MonoObject);
	src = (char *)this + sizeof (MonoObject);
	size = mono_class_value_size (enumc, NULL);

	memcpy (dst, src, size);

	return res;
}

ICALL_EXPORT MonoReflectionType *
ves_icall_System_Enum_get_underlying_type (MonoReflectionType *type)
{
	MonoType *etype;
	MonoClass *klass;

	klass = mono_class_from_mono_type (type->type);
	mono_class_init_or_throw (klass);

	etype = mono_class_enum_basetype (klass);
	if (!etype)
		/* MS throws this for typebuilders */
		mono_raise_exception (mono_get_exception_argument ("Type must be a type provided by the runtime.", "enumType"));

	return mono_type_get_object (mono_object_domain (type), etype);
}

ICALL_EXPORT int
ves_icall_System_Enum_compare_value_to (MonoObject *this, MonoObject *other)
{
	gpointer tdata = (char *)this + sizeof (MonoObject);
	gpointer odata = (char *)other + sizeof (MonoObject);
	MonoType *basetype = mono_class_enum_basetype (this->vtable->klass);
	g_assert (basetype);

#define COMPARE_ENUM_VALUES(ENUM_TYPE) do { \
		ENUM_TYPE me = *((ENUM_TYPE*)tdata); \
		ENUM_TYPE other = *((ENUM_TYPE*)odata); \
		if (me == other) \
			return 0; \
		return me > other ? 1 : -1; \
	} while (0)

	switch (basetype->type) {
		case MONO_TYPE_U1:
			COMPARE_ENUM_VALUES (guint8);
		case MONO_TYPE_I1:
			COMPARE_ENUM_VALUES (gint8);
		case MONO_TYPE_CHAR:
		case MONO_TYPE_U2:
			COMPARE_ENUM_VALUES (guint16);
		case MONO_TYPE_I2:
			COMPARE_ENUM_VALUES (gint16);
		case MONO_TYPE_U4:
			COMPARE_ENUM_VALUES (guint32);
		case MONO_TYPE_I4:
			COMPARE_ENUM_VALUES (gint32);
		case MONO_TYPE_U8:
			COMPARE_ENUM_VALUES (guint64);
		case MONO_TYPE_I8:
			COMPARE_ENUM_VALUES (gint64);
		default:
			g_error ("Implement type 0x%02x in get_hashcode", basetype->type);
	}
#undef COMPARE_ENUM_VALUES
	return 0;
}

ICALL_EXPORT int
ves_icall_System_Enum_get_hashcode (MonoObject *this)
{
	gpointer data = (char *)this + sizeof (MonoObject);
	MonoType *basetype = mono_class_enum_basetype (this->vtable->klass);
	g_assert (basetype);

	switch (basetype->type) {
		case MONO_TYPE_I1:	
			return *((gint8*)data);
		case MONO_TYPE_U1:
			return *((guint8*)data);
		case MONO_TYPE_CHAR:
		case MONO_TYPE_U2:
			return *((guint16*)data);
		
		case MONO_TYPE_I2:
			return *((gint16*)data);
		case MONO_TYPE_U4:
			return *((guint32*)data);
		case MONO_TYPE_I4:
			return *((gint32*)data);
		case MONO_TYPE_U8:
		case MONO_TYPE_I8: {
			gint64 value = *((gint64*)data);
			return (gint)(value & 0xffffffff) ^ (int)(value >> 32);
		}
		default:
			g_error ("Implement type 0x%02x in get_hashcode", basetype->type);
	}
	return 0;
}

ICALL_EXPORT void
ves_icall_get_enum_info (MonoReflectionType *type, MonoEnumInfo *info)
{
	MonoDomain *domain = mono_object_domain (type); 
	MonoClass *enumc = mono_class_from_mono_type (type->type);
	guint j = 0, nvalues, crow;
	gpointer iter;
	MonoClassField *field;

	mono_class_init_or_throw (enumc);

	MONO_STRUCT_SETREF (info, utype, mono_type_get_object (domain, mono_class_enum_basetype (enumc)));
	nvalues = mono_class_num_fields (enumc) ? mono_class_num_fields (enumc) - 1 : 0;
	MONO_STRUCT_SETREF (info, names, mono_array_new (domain, mono_defaults.string_class, nvalues));
	MONO_STRUCT_SETREF (info, values, mono_array_new (domain, enumc, nvalues));

	crow = -1;
	iter = NULL;
	while ((field = mono_class_get_fields (enumc, &iter))) {
		const char *p;
		int len;
		MonoTypeEnum def_type;
		
		if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC))
			continue;
		if (strcmp ("value__", mono_field_get_name (field)) == 0)
			continue;
		if (mono_field_is_deleted (field))
			continue;
		mono_array_setref (info->names, j, mono_string_new (domain, mono_field_get_name (field)));

		p = mono_class_get_field_default_value (field, &def_type);
		len = mono_metadata_decode_blob_size (p, &p);
		switch (mono_class_enum_basetype (enumc)->type) {
		case MONO_TYPE_U1:
		case MONO_TYPE_I1:
			mono_array_set (info->values, gchar, j, *p);
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_U2:
		case MONO_TYPE_I2:
			mono_array_set (info->values, gint16, j, read16 (p));
			break;
		case MONO_TYPE_U4:
		case MONO_TYPE_I4:
			mono_array_set (info->values, gint32, j, read32 (p));
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			mono_array_set (info->values, gint64, j, read64 (p));
			break;
		default:
			g_error ("Implement type 0x%02x in get_enum_info", mono_class_enum_basetype (enumc)->type);
		}
		++j;
	}
}

enum {
	BFLAGS_IgnoreCase = 1,
	BFLAGS_DeclaredOnly = 2,
	BFLAGS_Instance = 4,
	BFLAGS_Static = 8,
	BFLAGS_Public = 0x10,
	BFLAGS_NonPublic = 0x20,
	BFLAGS_FlattenHierarchy = 0x40,
	BFLAGS_InvokeMethod = 0x100,
	BFLAGS_CreateInstance = 0x200,
	BFLAGS_GetField = 0x400,
	BFLAGS_SetField = 0x800,
	BFLAGS_GetProperty = 0x1000,
	BFLAGS_SetProperty = 0x2000,
	BFLAGS_ExactBinding = 0x10000,
	BFLAGS_SuppressChangeType = 0x20000,
	BFLAGS_OptionalParamBinding = 0x40000
};

ICALL_EXPORT MonoReflectionField *
ves_icall_Type_GetField (MonoReflectionType *type, MonoString *name, guint32 bflags)
{
	MonoDomain *domain; 
	MonoClass *startklass, *klass;
	int match;
	MonoClassField *field;
	gpointer iter;
	char *utf8_name;
	int (*compare_func) (const char *s1, const char *s2) = NULL;
	domain = ((MonoObject *)type)->vtable->domain;
	klass = startklass = mono_class_from_mono_type (type->type);

	if (!name)
		mono_raise_exception (mono_get_exception_argument_null ("name"));
	if (type->type->byref)
		return NULL;

	compare_func = (bflags & BFLAGS_IgnoreCase) ? mono_utf8_strcasecmp : strcmp;

handle_parent:
	if (klass->exception_type != MONO_EXCEPTION_NONE)
		mono_raise_exception (mono_class_get_exception_for_failure (klass));

	iter = NULL;
	while ((field = mono_class_get_fields_lazy (klass, &iter))) {
		guint32 flags = mono_field_get_flags (field);
		match = 0;

		if (mono_field_is_deleted_with_flags (field, flags))
			continue;
		if ((flags & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) == FIELD_ATTRIBUTE_PUBLIC) {
			if (bflags & BFLAGS_Public)
				match++;
		} else if ((klass == startklass) || (flags & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) != FIELD_ATTRIBUTE_PRIVATE) {
			if (bflags & BFLAGS_NonPublic) {
				match++;
			}
		}
		if (!match)
			continue;
		match = 0;
		if (flags & FIELD_ATTRIBUTE_STATIC) {
			if (bflags & BFLAGS_Static)
				if ((bflags & BFLAGS_FlattenHierarchy) || (klass == startklass))
					match++;
		} else {
			if (bflags & BFLAGS_Instance)
				match++;
		}

		if (!match)
			continue;
		
		utf8_name = mono_string_to_utf8 (name);

		if (compare_func (mono_field_get_name (field), utf8_name)) {
			g_free (utf8_name);
			continue;
		}
		g_free (utf8_name);
		
		return mono_field_get_object (domain, klass, field);
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;

	return NULL;
}

ICALL_EXPORT MonoArray*
ves_icall_Type_GetFields_internal (MonoReflectionType *type, guint32 bflags, MonoReflectionType *reftype)
{
	MonoDomain *domain; 
	MonoClass *startklass, *klass, *refklass;
	MonoArray *res;
	MonoObject *member;
	int i, match;
	gpointer iter;
	MonoClassField *field;
	MonoPtrArray tmp_array;

	domain = ((MonoObject *)type)->vtable->domain;
	if (type->type->byref)
		return mono_array_new (domain, mono_defaults.field_info_class, 0);
	klass = startklass = mono_class_from_mono_type (type->type);
	refklass = mono_class_from_mono_type (reftype->type);

	mono_ptr_array_init (tmp_array, 2);
	
handle_parent:	
	if (klass->exception_type != MONO_EXCEPTION_NONE) {
		mono_ptr_array_destroy (tmp_array);
		mono_raise_exception (mono_class_get_exception_for_failure (klass));
	}

	iter = NULL;
	while ((field = mono_class_get_fields_lazy (klass, &iter))) {
		guint32 flags = mono_field_get_flags (field);
		match = 0;
		if (mono_field_is_deleted_with_flags (field, flags))
			continue;
		if ((flags & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) == FIELD_ATTRIBUTE_PUBLIC) {
			if (bflags & BFLAGS_Public)
				match++;
		} else if ((klass == startklass) || (flags & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) != FIELD_ATTRIBUTE_PRIVATE) {
			if (bflags & BFLAGS_NonPublic) {
				match++;
			}
		}
		if (!match)
			continue;
		match = 0;
		if (flags & FIELD_ATTRIBUTE_STATIC) {
			if (bflags & BFLAGS_Static)
				if ((bflags & BFLAGS_FlattenHierarchy) || (klass == startklass))
					match++;
		} else {
			if (bflags & BFLAGS_Instance)
				match++;
		}

		if (!match)
			continue;
		member = (MonoObject*)mono_field_get_object (domain, refklass, field);
		mono_ptr_array_append (tmp_array, member);
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;

	res = mono_array_new_cached (domain, mono_defaults.field_info_class, mono_ptr_array_size (tmp_array));

	for (i = 0; i < mono_ptr_array_size (tmp_array); ++i)
		mono_array_setref (res, i, mono_ptr_array_get (tmp_array, i));

	mono_ptr_array_destroy (tmp_array);

	return res;
}

static gboolean
method_nonpublic (MonoMethod* method, gboolean start_klass)
{
	switch (method->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) {
		case METHOD_ATTRIBUTE_ASSEM:
			return (start_klass || mono_defaults.generic_ilist_class);
		case METHOD_ATTRIBUTE_PRIVATE:
			return start_klass;
		case METHOD_ATTRIBUTE_PUBLIC:
			return FALSE;
		default:
			return TRUE;
	}
}

GPtrArray*
mono_class_get_methods_by_name (MonoClass *klass, const char *name, guint32 bflags, gboolean ignore_case, gboolean allow_ctors, MonoException **ex)
{
	GPtrArray *array;
	MonoClass *startklass;
	MonoMethod *method;
	gpointer iter;
	int len, match, nslots;
	/*FIXME, use MonoBitSet*/
	guint32 method_slots_default [8];
	guint32 *method_slots = NULL;
	int (*compare_func) (const char *s1, const char *s2) = NULL;

	array = g_ptr_array_new ();
	startklass = klass;
	*ex = NULL;

	len = 0;
	if (name != NULL)
		compare_func = (ignore_case) ? mono_utf8_strcasecmp : strcmp;

	/* An optimization for calls made from Delegate:CreateDelegate () */
	if (klass->delegate && name && !strcmp (name, "Invoke") && (bflags == (BFLAGS_Public | BFLAGS_Static | BFLAGS_Instance))) {
		method = mono_get_delegate_invoke (klass);
		if (mono_loader_get_last_error ())
			goto loader_error;

		g_ptr_array_add (array, method);
		return array;
	}

	mono_class_setup_methods (klass);
	mono_class_setup_vtable (klass);
	if (klass->exception_type != MONO_EXCEPTION_NONE || mono_loader_get_last_error ())
		goto loader_error;

	if (is_generic_parameter (&klass->byval_arg))
		nslots = mono_class_get_vtable_size (klass->parent);
	else
		nslots = MONO_CLASS_IS_INTERFACE (klass) ? mono_class_num_methods (klass) : mono_class_get_vtable_size (klass);
	if (nslots >= sizeof (method_slots_default) * 8) {
		method_slots = g_new0 (guint32, nslots / 32 + 1);
	} else {
		method_slots = method_slots_default;
		memset (method_slots, 0, sizeof (method_slots_default));
	}
handle_parent:
	mono_class_setup_methods (klass);
	mono_class_setup_vtable (klass);
	if (klass->exception_type != MONO_EXCEPTION_NONE || mono_loader_get_last_error ())
		goto loader_error;		

	iter = NULL;
	while ((method = mono_class_get_methods (klass, &iter))) {
		match = 0;
		if (method->slot != -1) {
			g_assert (method->slot < nslots);
			if (method_slots [method->slot >> 5] & (1 << (method->slot & 0x1f)))
				continue;
			if (!(method->flags & METHOD_ATTRIBUTE_NEW_SLOT))
				method_slots [method->slot >> 5] |= 1 << (method->slot & 0x1f);
		}

		if (!allow_ctors && method->name [0] == '.' && (strcmp (method->name, ".ctor") == 0 || strcmp (method->name, ".cctor") == 0))
			continue;
		if ((method->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PUBLIC) {
			if (bflags & BFLAGS_Public)
				match++;
		} else if ((bflags & BFLAGS_NonPublic) && method_nonpublic (method, (klass == startklass))) {
				match++;
		}
		if (!match)
			continue;
		match = 0;
		if (method->flags & METHOD_ATTRIBUTE_STATIC) {
			if (bflags & BFLAGS_Static)
				if ((bflags & BFLAGS_FlattenHierarchy) || (klass == startklass))
					match++;
		} else {
			if (bflags & BFLAGS_Instance)
				match++;
		}

		if (!match)
			continue;

		if (name != NULL) {
			if (compare_func (name, method->name))
				continue;
		}
		
		match = 0;
		g_ptr_array_add (array, method);
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;
	if (method_slots != method_slots_default)
		g_free (method_slots);

	return array;

loader_error:
	if (method_slots != method_slots_default)
		g_free (method_slots);
	g_ptr_array_free (array, TRUE);

	if (klass->exception_type != MONO_EXCEPTION_NONE) {
		*ex = mono_class_get_exception_for_failure (klass);
	} else {
		*ex = mono_loader_error_prepare_exception (mono_loader_get_last_error ());
		mono_loader_clear_error ();
	}
	return NULL;
}

ICALL_EXPORT MonoArray*
ves_icall_Type_GetMethodsByName (MonoReflectionType *type, MonoString *name, guint32 bflags, MonoBoolean ignore_case, MonoReflectionType *reftype)
{
	static MonoClass *MethodInfo_array;
	MonoDomain *domain; 
	MonoArray *res;
	MonoVTable *array_vtable;
	MonoException *ex = NULL;
	const char *mname = NULL;
	GPtrArray *method_array;
	MonoClass *klass, *refklass;
	int i;

	if (!MethodInfo_array) {
		MonoClass *klass = mono_array_class_get (mono_defaults.method_info_class, 1);
		mono_memory_barrier ();
		MethodInfo_array = klass;
	}

	klass = mono_class_from_mono_type (type->type);
	refklass = mono_class_from_mono_type (reftype->type);
	domain = ((MonoObject *)type)->vtable->domain;
	array_vtable = mono_class_vtable_full (domain, MethodInfo_array, TRUE);
	if (type->type->byref)
		return mono_array_new_specific (array_vtable, 0);

	if (name)
		mname = mono_string_to_utf8 (name);

	method_array = mono_class_get_methods_by_name (klass, mname, bflags, ignore_case, FALSE, &ex);
	g_free ((char*)mname);
	if (ex)
		mono_raise_exception (ex);

	res = mono_array_new_specific (array_vtable, method_array->len);


	for (i = 0; i < method_array->len; ++i) {
		MonoMethod *method = g_ptr_array_index (method_array, i);
		mono_array_setref (res, i, mono_method_get_object (domain, method, refklass));
	}

	g_ptr_array_free (method_array, TRUE);
	return res;
}

ICALL_EXPORT MonoArray*
ves_icall_Type_GetConstructors_internal (MonoReflectionType *type, guint32 bflags, MonoReflectionType *reftype)
{
	MonoDomain *domain; 
	static MonoClass *System_Reflection_ConstructorInfo;
	MonoClass *startklass, *klass, *refklass;
	MonoArray *res;
	MonoMethod *method;
	MonoObject *member;
	int i, match;
	gpointer iter = NULL;
	MonoPtrArray tmp_array;
	
	mono_ptr_array_init (tmp_array, 4); /*FIXME, guestimating*/

	domain = ((MonoObject *)type)->vtable->domain;
	if (type->type->byref)
		return mono_array_new_cached (domain, mono_defaults.method_info_class, 0);
	klass = startklass = mono_class_from_mono_type (type->type);
	refklass = mono_class_from_mono_type (reftype->type);

	if (!System_Reflection_ConstructorInfo)
		System_Reflection_ConstructorInfo = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "ConstructorInfo");

	mono_class_setup_methods (klass);
	if (klass->exception_type != MONO_EXCEPTION_NONE)
		mono_raise_exception (mono_class_get_exception_for_failure (klass));


	iter = NULL;
	while ((method = mono_class_get_methods (klass, &iter))) {
		match = 0;
		if (strcmp (method->name, ".ctor") && strcmp (method->name, ".cctor"))
			continue;
		if ((method->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PUBLIC) {
			if (bflags & BFLAGS_Public)
				match++;
		} else {
			if (bflags & BFLAGS_NonPublic)
				match++;
		}
		if (!match)
			continue;
		match = 0;
		if (method->flags & METHOD_ATTRIBUTE_STATIC) {
			if (bflags & BFLAGS_Static)
				if ((bflags & BFLAGS_FlattenHierarchy) || (klass == startklass))
					match++;
		} else {
			if (bflags & BFLAGS_Instance)
				match++;
		}

		if (!match)
			continue;
		member = (MonoObject*)mono_method_get_object (domain, method, refklass);

		mono_ptr_array_append (tmp_array, member);
	}

	res = mono_array_new_cached (domain, System_Reflection_ConstructorInfo, mono_ptr_array_size (tmp_array));

	for (i = 0; i < mono_ptr_array_size (tmp_array); ++i)
		mono_array_setref (res, i, mono_ptr_array_get (tmp_array, i));

	mono_ptr_array_destroy (tmp_array);

	return res;
}

static guint
property_hash (gconstpointer data)
{
	MonoProperty *prop = (MonoProperty*)data;

	return g_str_hash (prop->name);
}

static gboolean
property_equal (MonoProperty *prop1, MonoProperty *prop2)
{
	// Properties are hide-by-name-and-signature
	if (!g_str_equal (prop1->name, prop2->name))
		return FALSE;

	if (prop1->get && prop2->get && !mono_metadata_signature_equal (mono_method_signature (prop1->get), mono_method_signature (prop2->get)))
		return FALSE;
	if (prop1->set && prop2->set && !mono_metadata_signature_equal (mono_method_signature (prop1->set), mono_method_signature (prop2->set)))
		return FALSE;
	return TRUE;
}

static gboolean
property_accessor_nonpublic (MonoMethod* accessor, gboolean start_klass)
{
	if (!accessor)
		return FALSE;

	return method_nonpublic (accessor, start_klass);
}

ICALL_EXPORT MonoArray*
ves_icall_Type_GetPropertiesByName (MonoReflectionType *type, MonoString *name, guint32 bflags, MonoBoolean ignore_case, MonoReflectionType *reftype)
{
	MonoException *ex;
	MonoDomain *domain; 
	static MonoClass *System_Reflection_PropertyInfo;
	MonoClass *startklass, *klass;
	MonoArray *res;
	MonoMethod *method;
	MonoProperty *prop;
	int i, match;
	guint32 flags;
	gchar *propname = NULL;
	int (*compare_func) (const char *s1, const char *s2) = NULL;
	gpointer iter;
	GHashTable *properties = NULL;
	MonoPtrArray tmp_array;

	mono_ptr_array_init (tmp_array, 8); /*This the average for ASP.NET types*/

	if (!System_Reflection_PropertyInfo)
		System_Reflection_PropertyInfo = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "PropertyInfo");

	domain = ((MonoObject *)type)->vtable->domain;
	if (type->type->byref)
		return mono_array_new_cached (domain, System_Reflection_PropertyInfo, 0);
	klass = startklass = mono_class_from_mono_type (type->type);

	if (name != NULL) {
		propname = mono_string_to_utf8 (name);
		compare_func = (ignore_case) ? mono_utf8_strcasecmp : strcmp;
	}

	properties = g_hash_table_new (property_hash, (GEqualFunc)property_equal);
handle_parent:
	mono_class_setup_methods (klass);
	mono_class_setup_vtable (klass);
	if (klass->exception_type != MONO_EXCEPTION_NONE || mono_loader_get_last_error ())
		goto loader_error;

	iter = NULL;
	while ((prop = mono_class_get_properties (klass, &iter))) {
		match = 0;
		method = prop->get;
		if (!method)
			method = prop->set;
		if (method)
			flags = method->flags;
		else
			flags = 0;
		if ((prop->get && ((prop->get->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PUBLIC)) ||
			(prop->set && ((prop->set->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PUBLIC))) {
			if (bflags & BFLAGS_Public)
				match++;
		} else if (bflags & BFLAGS_NonPublic) {
			if (property_accessor_nonpublic(prop->get, startklass == klass) ||
				property_accessor_nonpublic(prop->set, startklass == klass)) {
				match++;
			}
		}
		if (!match)
			continue;
		match = 0;
		if (flags & METHOD_ATTRIBUTE_STATIC) {
			if (bflags & BFLAGS_Static)
				if ((bflags & BFLAGS_FlattenHierarchy) || (klass == startklass))
					match++;
		} else {
			if (bflags & BFLAGS_Instance)
				match++;
		}

		if (!match)
			continue;
		match = 0;

		if (name != NULL) {
			if (compare_func (propname, prop->name))
				continue;
		}
		
		if (g_hash_table_lookup (properties, prop))
			continue;

		mono_ptr_array_append (tmp_array, mono_property_get_object (domain, startklass, prop));
		
		g_hash_table_insert (properties, prop, prop);
	}
	if ((!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent)))
		goto handle_parent;

	g_hash_table_destroy (properties);
	g_free (propname);

	res = mono_array_new_cached (domain, System_Reflection_PropertyInfo, mono_ptr_array_size (tmp_array));
	for (i = 0; i < mono_ptr_array_size (tmp_array); ++i)
		mono_array_setref (res, i, mono_ptr_array_get (tmp_array, i));

	mono_ptr_array_destroy (tmp_array);

	return res;

loader_error:
	if (properties)
		g_hash_table_destroy (properties);
	if (name)
		g_free (propname);
	mono_ptr_array_destroy (tmp_array);

	if (klass->exception_type != MONO_EXCEPTION_NONE) {
		ex = mono_class_get_exception_for_failure (klass);
	} else {
		ex = mono_loader_error_prepare_exception (mono_loader_get_last_error ());
		mono_loader_clear_error ();
	}
	mono_raise_exception (ex);
	return NULL;
}

ICALL_EXPORT MonoReflectionEvent *
ves_icall_MonoType_GetEvent (MonoReflectionType *type, MonoString *name, guint32 bflags)
{
	MonoDomain *domain;
	MonoClass *klass, *startklass;
	gpointer iter;
	MonoEvent *event;
	MonoMethod *method;
	gchar *event_name;
	int (*compare_func) (const char *s1, const char *s2);

	event_name = mono_string_to_utf8 (name);
	if (type->type->byref)
		return NULL;
	klass = startklass = mono_class_from_mono_type (type->type);
	domain = mono_object_domain (type);

	mono_class_init_or_throw (klass);

	compare_func = (bflags & BFLAGS_IgnoreCase) ? mono_utf8_strcasecmp : strcmp;
handle_parent:	
	if (klass->exception_type != MONO_EXCEPTION_NONE)
		mono_raise_exception (mono_class_get_exception_for_failure (klass));

	iter = NULL;
	while ((event = mono_class_get_events (klass, &iter))) {
		if (compare_func (event->name, event_name))
			continue;

		method = event->add;
		if (!method)
			method = event->remove;
		if (!method)
			method = event->raise;
		if (method) {
			if ((method->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PUBLIC) {
				if (!(bflags & BFLAGS_Public))
					continue;
			} else {
				if (!(bflags & BFLAGS_NonPublic))
					continue;
				if ((klass != startklass) && (method->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PRIVATE)
					continue;
			}

			if (method->flags & METHOD_ATTRIBUTE_STATIC) {
				if (!(bflags & BFLAGS_Static))
					continue;
				if (!(bflags & BFLAGS_FlattenHierarchy) && (klass != startklass))
					continue;
			} else {
				if (!(bflags & BFLAGS_Instance))
					continue;
			}
		} else 
			if (!(bflags & BFLAGS_NonPublic))
				continue;
		
		g_free (event_name);
		return mono_event_get_object (domain, startklass, event);
	}

	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;

	g_free (event_name);
	return NULL;
}

static guint
event_hash (gconstpointer data)
{
	MonoEvent *event = (MonoEvent*)data;

	return g_str_hash (event->name);
}

static gboolean
event_equal (MonoEvent *event1, MonoEvent *event2)
{
	// Events are hide-by-name
	return g_str_equal (event1->name, event2->name);
}

ICALL_EXPORT MonoArray*
ves_icall_Type_GetEvents_internal (MonoReflectionType *type, guint32 bflags, MonoReflectionType *reftype)
{
	MonoException *ex;
	MonoDomain *domain; 
	static MonoClass *System_Reflection_EventInfo;
	MonoClass *startklass, *klass;
	MonoArray *res;
	MonoMethod *method;
	MonoEvent *event;
	int i, match;
	gpointer iter;
	GHashTable *events = NULL;
	MonoPtrArray tmp_array;

	mono_ptr_array_init (tmp_array, 4);

	if (!System_Reflection_EventInfo)
		System_Reflection_EventInfo = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "EventInfo");

	domain = mono_object_domain (type);
	if (type->type->byref)
		return mono_array_new_cached (domain, System_Reflection_EventInfo, 0);
	klass = startklass = mono_class_from_mono_type (type->type);

	events = g_hash_table_new (event_hash, (GEqualFunc)event_equal);
handle_parent:
	mono_class_setup_methods (klass);
	mono_class_setup_vtable (klass);
	if (klass->exception_type != MONO_EXCEPTION_NONE || mono_loader_get_last_error ())
		goto loader_error;

	iter = NULL;
	while ((event = mono_class_get_events (klass, &iter))) {
		match = 0;
		method = event->add;
		if (!method)
			method = event->remove;
		if (!method)
			method = event->raise;
		if (method) {
			if ((method->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PUBLIC) {
				if (bflags & BFLAGS_Public)
					match++;
			} else if ((klass == startklass) || (method->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) != METHOD_ATTRIBUTE_PRIVATE) {
				if (bflags & BFLAGS_NonPublic)
					match++;
			}
		}
		else
			if (bflags & BFLAGS_NonPublic)
				match ++;
		if (!match)
			continue;
		match = 0;
		if (method) {
			if (method->flags & METHOD_ATTRIBUTE_STATIC) {
				if (bflags & BFLAGS_Static)
					if ((bflags & BFLAGS_FlattenHierarchy) || (klass == startklass))
						match++;
			} else {
				if (bflags & BFLAGS_Instance)
					match++;
			}
		}
		else
			if (bflags & BFLAGS_Instance)
				match ++;
		if (!match)
			continue;

		if (g_hash_table_lookup (events, event))
			continue;

		mono_ptr_array_append (tmp_array, mono_event_get_object (domain, startklass, event));

		g_hash_table_insert (events, event, event);
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;

	g_hash_table_destroy (events);

	res = mono_array_new_cached (domain, System_Reflection_EventInfo, mono_ptr_array_size (tmp_array));

	for (i = 0; i < mono_ptr_array_size (tmp_array); ++i)
		mono_array_setref (res, i, mono_ptr_array_get (tmp_array, i));

	mono_ptr_array_destroy (tmp_array);

	return res;

loader_error:
	mono_ptr_array_destroy (tmp_array);
	if (klass->exception_type != MONO_EXCEPTION_NONE) {
		ex = mono_class_get_exception_for_failure (klass);
	} else {
		ex = mono_loader_error_prepare_exception (mono_loader_get_last_error ());
		mono_loader_clear_error ();
	}
	mono_raise_exception (ex);
	return NULL;
}

ICALL_EXPORT MonoReflectionType *
ves_icall_Type_GetNestedType (MonoReflectionType *type, MonoString *name, guint32 bflags)
{
	MonoDomain *domain; 
	MonoClass *klass;
	MonoClass *nested;
	char *str;
	gpointer iter;
	
	if (name == NULL)
		mono_raise_exception (mono_get_exception_argument_null ("name"));
	
	domain = ((MonoObject *)type)->vtable->domain;
	if (type->type->byref)
		return NULL;
	klass = mono_class_from_mono_type (type->type);

	str = mono_string_to_utf8 (name);

 handle_parent:
	if (klass->exception_type != MONO_EXCEPTION_NONE)
		mono_raise_exception (mono_class_get_exception_for_failure (klass));

	/*
	 * If a nested type is generic, return its generic type definition.
	 * Note that this means that the return value is essentially a
	 * nested type of the generic type definition of @klass.
	 *
	 * A note in MSDN claims that a generic type definition can have
	 * nested types that aren't generic.  In any case, the container of that
	 * nested type would be the generic type definition.
	 */
	if (klass->generic_class)
		klass = klass->generic_class->container_class;

	iter = NULL;
	while ((nested = mono_class_get_nested_types (klass, &iter))) {
		int match = 0;
		if ((nested->flags & TYPE_ATTRIBUTE_VISIBILITY_MASK) == TYPE_ATTRIBUTE_NESTED_PUBLIC) {
			if (bflags & BFLAGS_Public)
				match++;
		} else {
			if (bflags & BFLAGS_NonPublic)
				match++;
		}
		if (!match)
			continue;
		if (strcmp (nested->name, str) == 0){
			g_free (str);
			return mono_type_get_object (domain, &nested->byval_arg);
		}
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;
	g_free (str);
	return NULL;
}

ICALL_EXPORT MonoArray*
ves_icall_Type_GetNestedTypes (MonoReflectionType *type, guint32 bflags)
{
	MonoDomain *domain; 
	MonoClass *klass;
	MonoArray *res;
	MonoObject *member;
	int i, match;
	MonoClass *nested;
	gpointer iter;
	MonoPtrArray tmp_array;

	domain = ((MonoObject *)type)->vtable->domain;
	if (type->type->byref)
		return mono_array_new (domain, mono_defaults.monotype_class, 0);
	klass = mono_class_from_mono_type (type->type);

	/*
	 * If a nested type is generic, return its generic type definition.
	 * Note that this means that the return value is essentially the set
	 * of nested types of the generic type definition of @klass.
	 *
	 * A note in MSDN claims that a generic type definition can have
	 * nested types that aren't generic.  In any case, the container of that
	 * nested type would be the generic type definition.
	 */
	if (klass->generic_class)
		klass = klass->generic_class->container_class;

	mono_ptr_array_init (tmp_array, 1);
	iter = NULL;
	while ((nested = mono_class_get_nested_types (klass, &iter))) {
		match = 0;
		if ((nested->flags & TYPE_ATTRIBUTE_VISIBILITY_MASK) == TYPE_ATTRIBUTE_NESTED_PUBLIC) {
			if (bflags & BFLAGS_Public)
				match++;
		} else {
			if (bflags & BFLAGS_NonPublic)
				match++;
		}
		if (!match)
			continue;
		member = (MonoObject*)mono_type_get_object (domain, &nested->byval_arg);
		mono_ptr_array_append (tmp_array, member);
	}

	res = mono_array_new_cached (domain, mono_defaults.monotype_class, mono_ptr_array_size (tmp_array));

	for (i = 0; i < mono_ptr_array_size (tmp_array); ++i)
		mono_array_setref (res, i, mono_ptr_array_get (tmp_array, i));

	mono_ptr_array_destroy (tmp_array);

	return res;
}

ICALL_EXPORT MonoReflectionType*
ves_icall_System_Reflection_Assembly_InternalGetType (MonoReflectionAssembly *assembly, MonoReflectionModule *module, MonoString *name, MonoBoolean throwOnError, MonoBoolean ignoreCase)
{
	gchar *str;
	MonoType *type = NULL;
	MonoTypeNameParse info;
	gboolean type_resolve;

	/* On MS.NET, this does not fire a TypeResolve event */
	type_resolve = TRUE;
	str = mono_string_to_utf8 (name);
	/*g_print ("requested type %s in %s\n", str, assembly->assembly->aname.name);*/
	if (!mono_reflection_parse_type (str, &info)) {
		g_free (str);
		mono_reflection_free_type_info (&info);
		if (throwOnError) /* uhm: this is a parse error, though... */
			mono_raise_exception (mono_get_exception_type_load (name, NULL));
		/*g_print ("failed parse\n");*/
		return NULL;
	}

	if (info.assembly.name) {
		g_free (str);
		mono_reflection_free_type_info (&info);
		if (throwOnError) {
			/* 1.0 and 2.0 throw different exceptions */
			if (mono_defaults.generic_ilist_class)
				mono_raise_exception (mono_get_exception_argument (NULL, "Type names passed to Assembly.GetType() must not specify an assembly."));
			else
				mono_raise_exception (mono_get_exception_type_load (name, NULL));
		}
		return NULL;
	}

	if (module != NULL) {
		if (module->image)
			type = mono_reflection_get_type (module->image, &info, ignoreCase, &type_resolve);
		else
			type = NULL;
	}
	else
		if (assembly_is_dynamic (assembly->assembly)) {
			/* Enumerate all modules */
			MonoReflectionAssemblyBuilder *abuilder = (MonoReflectionAssemblyBuilder*)assembly;
			int i;

			type = NULL;
			if (abuilder->modules) {
				for (i = 0; i < mono_array_length (abuilder->modules); ++i) {
					MonoReflectionModuleBuilder *mb = mono_array_get (abuilder->modules, MonoReflectionModuleBuilder*, i);
					type = mono_reflection_get_type (&mb->dynamic_image->image, &info, ignoreCase, &type_resolve);
					if (type)
						break;
				}
			}

			if (!type && abuilder->loaded_modules) {
				for (i = 0; i < mono_array_length (abuilder->loaded_modules); ++i) {
					MonoReflectionModule *mod = mono_array_get (abuilder->loaded_modules, MonoReflectionModule*, i);
					type = mono_reflection_get_type (mod->image, &info, ignoreCase, &type_resolve);
					if (type)
						break;
				}
			}
		}
		else
			type = mono_reflection_get_type (assembly->assembly->image, &info, ignoreCase, &type_resolve);
	g_free (str);
	mono_reflection_free_type_info (&info);
	if (!type) {
		MonoException *e = NULL;
		
		if (throwOnError)
			e = mono_get_exception_type_load (name, NULL);

		if (mono_loader_get_last_error () && mono_defaults.generic_ilist_class)
			e = mono_loader_error_prepare_exception (mono_loader_get_last_error ());

		mono_loader_clear_error ();

		if (e != NULL)
			mono_raise_exception (e);

		return NULL;
	} else if (mono_loader_get_last_error ()) {
		if (throwOnError)
			mono_raise_exception (mono_loader_error_prepare_exception (mono_loader_get_last_error ()));
		mono_loader_clear_error ();
	}

	if (type->type == MONO_TYPE_CLASS) {
		MonoClass *klass = mono_type_get_class (type);

		if (mono_security_enabled () && !klass->exception_type)
			/* Some security problems are detected during generic vtable construction */
			mono_class_setup_vtable (klass);

		/* need to report exceptions ? */
		if (throwOnError && klass->exception_type) {
			/* report SecurityException (or others) that occured when loading the assembly */
			MonoException *exc = mono_class_get_exception_for_failure (klass);
			mono_loader_clear_error ();
			mono_raise_exception (exc);
		} else if (mono_security_enabled () && klass->exception_type == MONO_EXCEPTION_SECURITY_INHERITANCEDEMAND) {
			return NULL;
		}
	}

	/* g_print ("got it\n"); */
	return mono_type_get_object (mono_object_domain (assembly), type);
}

static gboolean
replace_shadow_path (MonoDomain *domain, gchar *dirname, gchar **filename)
{
	gchar *content;
	gchar *shadow_ini_file;
	gsize len;

	/* Check for shadow-copied assembly */
	if (mono_is_shadow_copy_enabled (domain, dirname)) {
		shadow_ini_file = g_build_filename (dirname, "__AssemblyInfo__.ini", NULL);
		content = NULL;
		if (!g_file_get_contents (shadow_ini_file, &content, &len, NULL) ||
			!g_file_test (content, G_FILE_TEST_IS_REGULAR)) {
			if (content) {
				g_free (content);
				content = NULL;
			}
		}
		g_free (shadow_ini_file);
		if (content != NULL) {
			if (*filename)
				g_free (*filename);
			*filename = content;
			return TRUE;
		}
	}
	return FALSE;
}

ICALL_EXPORT MonoString *
ves_icall_System_Reflection_Assembly_get_code_base (MonoReflectionAssembly *assembly, MonoBoolean escaped)
{
	MonoDomain *domain = mono_object_domain (assembly); 
	MonoAssembly *mass = assembly->assembly;
	MonoString *res = NULL;
	gchar *uri;
	gchar *absolute;
	gchar *dirname;
	
	if (g_path_is_absolute (mass->image->name)) {
		absolute = g_strdup (mass->image->name);
		dirname = g_path_get_dirname (absolute);
	} else {
		absolute = g_build_filename (mass->basedir, mass->image->name, NULL);
		dirname = g_strdup (mass->basedir);
	}

	replace_shadow_path (domain, dirname, &absolute);
	g_free (dirname);
#if HOST_WIN32
	{
		gint i;
		for (i = strlen (absolute) - 1; i >= 0; i--)
			if (absolute [i] == '\\')
				absolute [i] = '/';
	}
#endif
	if (escaped) {
		uri = g_filename_to_uri (absolute, NULL, NULL);
	} else {
		const char *prepend = "file://";
#if HOST_WIN32
		if (*absolute == '/' && *(absolute + 1) == '/') {
			prepend = "file:";
		} else {
			prepend = "file:///";
		}
#endif
		uri = g_strconcat (prepend, absolute, NULL);
	}

	if (uri) {
		res = mono_string_new (domain, uri);
		g_free (uri);
	}
	g_free (absolute);
	return res;
}

ICALL_EXPORT MonoBoolean
ves_icall_System_Reflection_Assembly_get_global_assembly_cache (MonoReflectionAssembly *assembly)
{
	MonoAssembly *mass = assembly->assembly;

	return mass->in_gac;
}

ICALL_EXPORT MonoReflectionAssembly*
ves_icall_System_Reflection_Assembly_load_with_partial_name (MonoString *mname, MonoObject *evidence)
{
	gchar *name;
	MonoAssembly *res;
	MonoImageOpenStatus status;
	
	name = mono_string_to_utf8 (mname);
	res = mono_assembly_load_with_partial_name (name, &status);

	g_free (name);

	if (res == NULL)
		return NULL;
	return mono_assembly_get_object (mono_domain_get (), res);
}

ICALL_EXPORT MonoString *
ves_icall_System_Reflection_Assembly_get_location (MonoReflectionAssembly *assembly)
{
	MonoDomain *domain = mono_object_domain (assembly); 
	MonoString *res;

	res = mono_string_new (domain, mono_image_get_filename (assembly->assembly->image));

	return res;
}

ICALL_EXPORT MonoBoolean
ves_icall_System_Reflection_Assembly_get_ReflectionOnly (MonoReflectionAssembly *assembly)
{
	return assembly->assembly->ref_only;
}

ICALL_EXPORT MonoString *
ves_icall_System_Reflection_Assembly_InternalImageRuntimeVersion (MonoReflectionAssembly *assembly)
{
	MonoDomain *domain = mono_object_domain (assembly); 

	return mono_string_new (domain, assembly->assembly->image->version);
}

ICALL_EXPORT MonoReflectionMethod*
ves_icall_System_Reflection_Assembly_get_EntryPoint (MonoReflectionAssembly *assembly) 
{
	MonoError error;
	MonoMethod *method;
	guint32 token = mono_image_get_entry_point (assembly->assembly->image);

	if (!token)
		return NULL;
	method = mono_get_method_checked (assembly->assembly->image, token, NULL, NULL, &error);
	mono_error_raise_exception (&error);

	return mono_method_get_object (mono_object_domain (assembly), method, NULL);
}

ICALL_EXPORT MonoReflectionModule*
ves_icall_System_Reflection_Assembly_GetManifestModuleInternal (MonoReflectionAssembly *assembly) 
{
	return mono_module_get_object (mono_object_domain (assembly), assembly->assembly->image);
}

ICALL_EXPORT MonoArray*
ves_icall_System_Reflection_Assembly_GetManifestResourceNames (MonoReflectionAssembly *assembly) 
{
	MonoTableInfo *table = &assembly->assembly->image->tables [MONO_TABLE_MANIFESTRESOURCE];
	MonoArray *result = mono_array_new (mono_object_domain (assembly), mono_defaults.string_class, table->rows);
	int i;
	const char *val;

	for (i = 0; i < table->rows; ++i) {
		val = mono_metadata_string_heap (assembly->assembly->image, mono_metadata_decode_row_col (table, i, MONO_MANIFEST_NAME));
		mono_array_setref (result, i, mono_string_new (mono_object_domain (assembly), val));
	}
	return result;
}

static MonoObject*
create_version (MonoDomain *domain, guint32 major, guint32 minor, guint32 build, guint32 revision)
{
	static MonoClass *System_Version = NULL;
	static MonoMethod *create_version = NULL;
	MonoObject *result;
	gpointer args [4];
	
	if (!System_Version) {
		System_Version = mono_class_from_name (mono_defaults.corlib, "System", "Version");
		g_assert (System_Version);
	}

	if (!create_version) {
		MonoMethodDesc *desc = mono_method_desc_new (":.ctor(int,int,int,int)", FALSE);
		create_version = mono_method_desc_search_in_class (desc, System_Version);
		g_assert (create_version);
		mono_method_desc_free (desc);
	}

	args [0] = &major;
	args [1] = &minor;
	args [2] = &build;
	args [3] = &revision;
	result = mono_object_new (domain, System_Version);
	mono_runtime_invoke (create_version, result, args, NULL);

	return result;
}

ICALL_EXPORT MonoArray*
ves_icall_System_Reflection_Assembly_GetReferencedAssemblies (MonoReflectionAssembly *assembly) 
{
	static MonoClass *System_Reflection_AssemblyName;
	MonoArray *result;
	MonoDomain *domain = mono_object_domain (assembly);
	int i, count = 0;
	static MonoMethod *create_culture = NULL;
	MonoImage *image = assembly->assembly->image;
	MonoTableInfo *t;

	if (!System_Reflection_AssemblyName)
		System_Reflection_AssemblyName = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "AssemblyName");

	t = &assembly->assembly->image->tables [MONO_TABLE_ASSEMBLYREF];
	count = t->rows;

	result = mono_array_new (domain, System_Reflection_AssemblyName, count);

	if (count > 0 && !create_culture) {
		MonoMethodDesc *desc = mono_method_desc_new (
			"System.Globalization.CultureInfo:CreateCulture(string,bool)", TRUE);
		create_culture = mono_method_desc_search_in_image (desc, mono_defaults.corlib);
		g_assert (create_culture);
		mono_method_desc_free (desc);
	}

	for (i = 0; i < count; i++) {
		MonoReflectionAssemblyName *aname;
		guint32 cols [MONO_ASSEMBLYREF_SIZE];

		mono_metadata_decode_row (t, i, cols, MONO_ASSEMBLYREF_SIZE);

		aname = (MonoReflectionAssemblyName *) mono_object_new (
			domain, System_Reflection_AssemblyName);

		MONO_OBJECT_SETREF (aname, name, mono_string_new (domain, mono_metadata_string_heap (image, cols [MONO_ASSEMBLYREF_NAME])));

		aname->major = cols [MONO_ASSEMBLYREF_MAJOR_VERSION];
		aname->minor = cols [MONO_ASSEMBLYREF_MINOR_VERSION];
		aname->build = cols [MONO_ASSEMBLYREF_BUILD_NUMBER];
		aname->revision = cols [MONO_ASSEMBLYREF_REV_NUMBER];
		aname->flags = cols [MONO_ASSEMBLYREF_FLAGS];
		aname->versioncompat = 1; /* SameMachine (default) */
		aname->hashalg = ASSEMBLY_HASH_SHA1; /* SHA1 (default) */
		MONO_OBJECT_SETREF (aname, version, create_version (domain, aname->major, aname->minor, aname->build, aname->revision));

		if (create_culture) {
			gpointer args [2];
			MonoBoolean assembly_ref = 1;
			args [0] = mono_string_new (domain, mono_metadata_string_heap (image, cols [MONO_ASSEMBLYREF_CULTURE]));
			args [1] = &assembly_ref;
			MONO_OBJECT_SETREF (aname, cultureInfo, mono_runtime_invoke (create_culture, NULL, args, NULL));
		}
		
		if (cols [MONO_ASSEMBLYREF_PUBLIC_KEY]) {
			const gchar *pkey_ptr = mono_metadata_blob_heap (image, cols [MONO_ASSEMBLYREF_PUBLIC_KEY]);
			guint32 pkey_len = mono_metadata_decode_blob_size (pkey_ptr, &pkey_ptr);

			if ((cols [MONO_ASSEMBLYREF_FLAGS] & ASSEMBLYREF_FULL_PUBLIC_KEY_FLAG)) {
				/* public key token isn't copied - the class library will 
		   		automatically generate it from the public key if required */
				MONO_OBJECT_SETREF (aname, publicKey, mono_array_new (domain, mono_defaults.byte_class, pkey_len));
				memcpy (mono_array_addr (aname->publicKey, guint8, 0), pkey_ptr, pkey_len);
			} else {
				MONO_OBJECT_SETREF (aname, keyToken, mono_array_new (domain, mono_defaults.byte_class, pkey_len));
				memcpy (mono_array_addr (aname->keyToken, guint8, 0), pkey_ptr, pkey_len);
			}
		} else {
			MONO_OBJECT_SETREF (aname, keyToken, mono_array_new (domain, mono_defaults.byte_class, 0));
		}
		
		/* note: this function doesn't return the codebase on purpose (i.e. it can
		         be used under partial trust as path information isn't present). */

		mono_array_setref (result, i, aname);
	}
	return result;
}

/* move this in some file in mono/util/ */
static char *
g_concat_dir_and_file (const char *dir, const char *file)
{
	g_return_val_if_fail (dir != NULL, NULL);
	g_return_val_if_fail (file != NULL, NULL);

        /*
	 * If the directory name doesn't have a / on the end, we need
	 * to add one so we get a proper path to the file
	 */
	if (dir [strlen(dir) - 1] != G_DIR_SEPARATOR)
		return g_strconcat (dir, G_DIR_SEPARATOR_S, file, NULL);
	else
		return g_strconcat (dir, file, NULL);
}

ICALL_EXPORT void *
ves_icall_System_Reflection_Assembly_GetManifestResourceInternal (MonoReflectionAssembly *assembly, MonoString *name, gint32 *size, MonoReflectionModule **ref_module) 
{
	char *n = mono_string_to_utf8 (name);
	MonoTableInfo *table = &assembly->assembly->image->tables [MONO_TABLE_MANIFESTRESOURCE];
	guint32 i;
	guint32 cols [MONO_MANIFEST_SIZE];
	guint32 impl, file_idx;
	const char *val;
	MonoImage *module;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, cols, MONO_MANIFEST_SIZE);
		val = mono_metadata_string_heap (assembly->assembly->image, cols [MONO_MANIFEST_NAME]);
		if (strcmp (val, n) == 0)
			break;
	}
	g_free (n);
	if (i == table->rows)
		return NULL;
	/* FIXME */
	impl = cols [MONO_MANIFEST_IMPLEMENTATION];
	if (impl) {
		/*
		 * this code should only be called after obtaining the 
		 * ResourceInfo and handling the other cases.
		 */
		g_assert ((impl & MONO_IMPLEMENTATION_MASK) == MONO_IMPLEMENTATION_FILE);
		file_idx = impl >> MONO_IMPLEMENTATION_BITS;

		module = mono_image_load_file_for_image (assembly->assembly->image, file_idx);
		if (!module)
			return NULL;
	}
	else
		module = assembly->assembly->image;

	mono_gc_wbarrier_generic_store (ref_module, (MonoObject*) mono_module_get_object (mono_domain_get (), module));

	return (void*)mono_image_get_resource (module, cols [MONO_MANIFEST_OFFSET], (guint32*)size);
}

ICALL_EXPORT gboolean
ves_icall_System_Reflection_Assembly_GetManifestResourceInfoInternal (MonoReflectionAssembly *assembly, MonoString *name, MonoManifestResourceInfo *info)
{
	MonoTableInfo *table = &assembly->assembly->image->tables [MONO_TABLE_MANIFESTRESOURCE];
	int i;
	guint32 cols [MONO_MANIFEST_SIZE];
	guint32 file_cols [MONO_FILE_SIZE];
	const char *val;
	char *n;

	n = mono_string_to_utf8 (name);
	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, cols, MONO_MANIFEST_SIZE);
		val = mono_metadata_string_heap (assembly->assembly->image, cols [MONO_MANIFEST_NAME]);
		if (strcmp (val, n) == 0)
			break;
	}
	g_free (n);
	if (i == table->rows)
		return FALSE;

	if (!cols [MONO_MANIFEST_IMPLEMENTATION]) {
		info->location = RESOURCE_LOCATION_EMBEDDED | RESOURCE_LOCATION_IN_MANIFEST;
	}
	else {
		switch (cols [MONO_MANIFEST_IMPLEMENTATION] & MONO_IMPLEMENTATION_MASK) {
		case MONO_IMPLEMENTATION_FILE:
			i = cols [MONO_MANIFEST_IMPLEMENTATION] >> MONO_IMPLEMENTATION_BITS;
			table = &assembly->assembly->image->tables [MONO_TABLE_FILE];
			mono_metadata_decode_row (table, i - 1, file_cols, MONO_FILE_SIZE);
			val = mono_metadata_string_heap (assembly->assembly->image, file_cols [MONO_FILE_NAME]);
			MONO_OBJECT_SETREF (info, filename, mono_string_new (mono_object_domain (assembly), val));
			if (file_cols [MONO_FILE_FLAGS] && FILE_CONTAINS_NO_METADATA)
				info->location = 0;
			else
				info->location = RESOURCE_LOCATION_EMBEDDED;
			break;

		case MONO_IMPLEMENTATION_ASSEMBLYREF:
			i = cols [MONO_MANIFEST_IMPLEMENTATION] >> MONO_IMPLEMENTATION_BITS;
			mono_assembly_load_reference (assembly->assembly->image, i - 1);
			if (assembly->assembly->image->references [i - 1] == (gpointer)-1) {
				char *msg = g_strdup_printf ("Assembly %d referenced from assembly %s not found ", i - 1, assembly->assembly->image->name);
				MonoException *ex = mono_get_exception_file_not_found2 (msg, NULL);
				g_free (msg);
				mono_raise_exception (ex);
			}
			MONO_OBJECT_SETREF (info, assembly, mono_assembly_get_object (mono_domain_get (), assembly->assembly->image->references [i - 1]));

			/* Obtain info recursively */
			ves_icall_System_Reflection_Assembly_GetManifestResourceInfoInternal (info->assembly, name, info);
			info->location |= RESOURCE_LOCATION_ANOTHER_ASSEMBLY;
			break;

		case MONO_IMPLEMENTATION_EXP_TYPE:
			g_assert_not_reached ();
			break;
		}
	}

	return TRUE;
}

ICALL_EXPORT MonoObject*
ves_icall_System_Reflection_Assembly_GetFilesInternal (MonoReflectionAssembly *assembly, MonoString *name, MonoBoolean resource_modules) 
{
	MonoTableInfo *table = &assembly->assembly->image->tables [MONO_TABLE_FILE];
	MonoArray *result = NULL;
	int i, count;
	const char *val;
	char *n;

	/* check hash if needed */
	if (name) {
		n = mono_string_to_utf8 (name);
		for (i = 0; i < table->rows; ++i) {
			val = mono_metadata_string_heap (assembly->assembly->image, mono_metadata_decode_row_col (table, i, MONO_FILE_NAME));
			if (strcmp (val, n) == 0) {
				MonoString *fn;
				g_free (n);
				n = g_concat_dir_and_file (assembly->assembly->basedir, val);
				fn = mono_string_new (mono_object_domain (assembly), n);
				g_free (n);
				return (MonoObject*)fn;
			}
		}
		g_free (n);
		return NULL;
	}

	count = 0;
	for (i = 0; i < table->rows; ++i) {
		if (resource_modules || !(mono_metadata_decode_row_col (table, i, MONO_FILE_FLAGS) & FILE_CONTAINS_NO_METADATA))
			count ++;
	}

	result = mono_array_new (mono_object_domain (assembly), mono_defaults.string_class, count);

	count = 0;
	for (i = 0; i < table->rows; ++i) {
		if (resource_modules || !(mono_metadata_decode_row_col (table, i, MONO_FILE_FLAGS) & FILE_CONTAINS_NO_METADATA)) {
			val = mono_metadata_string_heap (assembly->assembly->image, mono_metadata_decode_row_col (table, i, MONO_FILE_NAME));
			n = g_concat_dir_and_file (assembly->assembly->basedir, val);
			mono_array_setref (result, count, mono_string_new (mono_object_domain (assembly), n));
			g_free (n);
			count ++;
		}
	}
	return (MonoObject*)result;
}

ICALL_EXPORT MonoArray*
ves_icall_System_Reflection_Assembly_GetModulesInternal (MonoReflectionAssembly *assembly)
{
	MonoDomain *domain = mono_domain_get();
	MonoArray *res;
	MonoClass *klass;
	int i, j, file_count = 0;
	MonoImage **modules;
	guint32 module_count, real_module_count;
	MonoTableInfo *table;
	guint32 cols [MONO_FILE_SIZE];
	MonoImage *image = assembly->assembly->image;

	g_assert (image != NULL);
	g_assert (!assembly_is_dynamic (assembly->assembly));

	table = &image->tables [MONO_TABLE_FILE];
	file_count = table->rows;

	modules = image->modules;
	module_count = image->module_count;

	real_module_count = 0;
	for (i = 0; i < module_count; ++i)
		if (modules [i])
			real_module_count ++;

	klass = mono_class_from_name (mono_defaults.corlib, "System.Reflection", "Module");
	res = mono_array_new (domain, klass, 1 + real_module_count + file_count);

	mono_array_setref (res, 0, mono_module_get_object (domain, image));
	j = 1;
	for (i = 0; i < module_count; ++i)
		if (modules [i]) {
			mono_array_setref (res, j, mono_module_get_object (domain, modules[i]));
			++j;
		}

	for (i = 0; i < file_count; ++i, ++j) {
		mono_metadata_decode_row (table, i, cols, MONO_FILE_SIZE);
		if (cols [MONO_FILE_FLAGS] && FILE_CONTAINS_NO_METADATA)
			mono_array_setref (res, j, mono_module_file_get_object (domain, image, i));
		else {
			MonoImage *m = mono_image_load_file_for_image (image, i + 1);
			if (!m) {
				MonoString *fname = mono_string_new (mono_domain_get (), mono_metadata_string_heap (image, cols [MONO_FILE_NAME]));
				mono_raise_exception (mono_get_exception_file_not_found2 (NULL, fname));
			}
			mono_array_setref (res, j, mono_module_get_object (domain, m));
		}
	}

	return res;
}

ICALL_EXPORT MonoReflectionMethod*
ves_icall_GetCurrentMethod (void) 
{
	MonoMethod *m = mono_method_get_last_managed ();

	while (m->is_inflated)
		m = ((MonoMethodInflated*)m)->declaring;

	return mono_method_get_object (mono_domain_get (), m, NULL);
}


static MonoMethod*
mono_method_get_equivalent_method (MonoMethod *method, MonoClass *klass)
{
	int offset = -1, i;
	if (method->is_inflated && ((MonoMethodInflated*)method)->context.method_inst) {
		MonoError error;
		MonoMethod *result;
		MonoMethodInflated *inflated = (MonoMethodInflated*)method;
		//method is inflated, we should inflate it on the other class
		MonoGenericContext ctx;
		ctx.method_inst = inflated->context.method_inst;
		ctx.class_inst = inflated->context.class_inst;
		if (klass->generic_class)
			ctx.class_inst = klass->generic_class->context.class_inst;
		else if (klass->generic_container)
			ctx.class_inst = klass->generic_container->context.class_inst;
		result = mono_class_inflate_generic_method_full_checked (inflated->declaring, klass, &ctx, &error);
		g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */
		return result;
	}

	mono_class_setup_methods (method->klass);
	if (method->klass->exception_type)
		return NULL;
	for (i = 0; i < method->klass->method.count; ++i) {
		if (method->klass->methods [i] == method) {
			offset = i;
			break;
		}	
	}
	mono_class_setup_methods (klass);
	if (klass->exception_type)
		return NULL;
	g_assert (offset >= 0 && offset < klass->method.count);
	return klass->methods [offset];
}

ICALL_EXPORT MonoReflectionMethod*
ves_icall_System_Reflection_MethodBase_GetMethodFromHandleInternalType (MonoMethod *method, MonoType *type)
{
	MonoClass *klass;
	if (type) {
		klass = mono_class_from_mono_type (type);
		if (mono_class_get_generic_type_definition (method->klass) != mono_class_get_generic_type_definition (klass)) 
			return NULL;
		if (method->klass != klass) {
			method = mono_method_get_equivalent_method (method, klass);
			if (!method)
				return NULL;
		}
	} else
		klass = method->klass;
	return mono_method_get_object (mono_domain_get (), method, klass);
}

ICALL_EXPORT MonoReflectionMethod*
ves_icall_System_Reflection_MethodBase_GetMethodFromHandleInternal (MonoMethod *method)
{
	return mono_method_get_object (mono_domain_get (), method, NULL);
}

ICALL_EXPORT MonoReflectionMethodBody*
ves_icall_System_Reflection_MethodBase_GetMethodBodyInternal (MonoMethod *method)
{
	return mono_method_body_get_object (mono_domain_get (), method);
}

ICALL_EXPORT MonoReflectionAssembly*
ves_icall_System_Reflection_Assembly_GetExecutingAssembly (void)
{
	MonoMethod *dest = NULL;

	mono_stack_walk_no_il (get_executing, &dest);
	g_assert (dest);
	return mono_assembly_get_object (mono_domain_get (), dest->klass->image->assembly);
}


ICALL_EXPORT MonoReflectionAssembly*
ves_icall_System_Reflection_Assembly_GetEntryAssembly (void)
{
	MonoDomain* domain = mono_domain_get ();

	if (!domain->entry_assembly)
		return NULL;

	return mono_assembly_get_object (domain, domain->entry_assembly);
}

ICALL_EXPORT MonoReflectionAssembly*
ves_icall_System_Reflection_Assembly_GetCallingAssembly (void)
{
	MonoMethod *m;
	MonoMethod *dest;

	dest = NULL;
	mono_stack_walk_no_il (get_executing, &dest);
	m = dest;
	mono_stack_walk_no_il (get_caller, &dest);
	if (!dest)
		dest = m;
	return mono_assembly_get_object (mono_domain_get (), dest->klass->image->assembly);
}

ICALL_EXPORT MonoString *
ves_icall_System_MonoType_getFullName (MonoReflectionType *object, gboolean full_name,
				       gboolean assembly_qualified)
{
	MonoDomain *domain = mono_object_domain (object); 
	MonoTypeNameFormat format;
	MonoString *res;
	gchar *name;

	if (full_name)
		format = assembly_qualified ?
			MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED :
			MONO_TYPE_NAME_FORMAT_FULL_NAME;
	else
		format = MONO_TYPE_NAME_FORMAT_REFLECTION;
 
	name = mono_type_get_name_full (object->type, format);
	if (!name)
		return NULL;

	if (full_name && (object->type->type == MONO_TYPE_VAR || object->type->type == MONO_TYPE_MVAR)) {
		g_free (name);
		return NULL;
	}

	res = mono_string_new (domain, name);
	g_free (name);

	return res;
}

ICALL_EXPORT int
vell_icall_MonoType_get_core_clr_security_level (MonoReflectionType *this)
{
	MonoClass *klass = mono_class_from_mono_type (this->type);
	mono_class_init_or_throw (klass);
	return mono_security_core_clr_class_level (klass);
}

static void
fill_reflection_assembly_name (MonoDomain *domain, MonoReflectionAssemblyName *aname, MonoAssemblyName *name, const char *absolute, gboolean by_default_version, gboolean default_publickey, gboolean default_token)
{
	static MonoMethod *create_culture = NULL;
	gpointer args [2];
	guint32 pkey_len;
	const char *pkey_ptr;
	gchar *codebase;
	MonoBoolean assembly_ref = 0;

	MONO_OBJECT_SETREF (aname, name, mono_string_new (domain, name->name));
	aname->major = name->major;
	aname->minor = name->minor;
	aname->build = name->build;
	aname->flags = name->flags;
	aname->revision = name->revision;
	aname->hashalg = name->hash_alg;
	aname->versioncompat = 1; /* SameMachine (default) */
	aname->processor_architecture = name->arch;

	if (by_default_version)
		MONO_OBJECT_SETREF (aname, version, create_version (domain, name->major, name->minor, name->build, name->revision));

	codebase = NULL;
	if (absolute != NULL && *absolute != '\0') {
		const gchar *prepend = "file://";
		gchar *result;

		codebase = g_strdup (absolute);

#if HOST_WIN32
		{
			gint i;
			for (i = strlen (codebase) - 1; i >= 0; i--)
				if (codebase [i] == '\\')
					codebase [i] = '/';

			if (*codebase == '/' && *(codebase + 1) == '/') {
				prepend = "file:";
			} else {
				prepend = "file:///";
			}
		}
#endif
		result = g_strconcat (prepend, codebase, NULL);
		g_free (codebase);
		codebase = result;
	}

	if (codebase) {
		MONO_OBJECT_SETREF (aname, codebase, mono_string_new (domain, codebase));
		g_free (codebase);
	}

	if (!create_culture) {
		MonoMethodDesc *desc = mono_method_desc_new ("System.Globalization.CultureInfo:CreateCulture(string,bool)", TRUE);
		create_culture = mono_method_desc_search_in_image (desc, mono_defaults.corlib);
		g_assert (create_culture);
		mono_method_desc_free (desc);
	}

	if (name->culture) {
		args [0] = mono_string_new (domain, name->culture);
		args [1] = &assembly_ref;
		MONO_OBJECT_SETREF (aname, cultureInfo, mono_runtime_invoke (create_culture, NULL, args, NULL));
	}

	if (name->public_key) {
		pkey_ptr = (char*)name->public_key;
		pkey_len = mono_metadata_decode_blob_size (pkey_ptr, &pkey_ptr);

		MONO_OBJECT_SETREF (aname, publicKey, mono_array_new (domain, mono_defaults.byte_class, pkey_len));
		memcpy (mono_array_addr (aname->publicKey, guint8, 0), pkey_ptr, pkey_len);
		aname->flags |= ASSEMBLYREF_FULL_PUBLIC_KEY_FLAG;
	} else if (default_publickey) {
		MONO_OBJECT_SETREF (aname, publicKey, mono_array_new (domain, mono_defaults.byte_class, 0));
		aname->flags |= ASSEMBLYREF_FULL_PUBLIC_KEY_FLAG;
	}

	/* MonoAssemblyName keeps the public key token as an hexadecimal string */
	if (name->public_key_token [0]) {
		int i, j;
		char *p;

		MONO_OBJECT_SETREF (aname, keyToken, mono_array_new (domain, mono_defaults.byte_class, 8));
		p = mono_array_addr (aname->keyToken, char, 0);

		for (i = 0, j = 0; i < 8; i++) {
			*p = g_ascii_xdigit_value (name->public_key_token [j++]) << 4;
			*p |= g_ascii_xdigit_value (name->public_key_token [j++]);
			p++;
		}
	} else if (default_token) {
		MONO_OBJECT_SETREF (aname, keyToken, mono_array_new (domain, mono_defaults.byte_class, 0));
	}
}

ICALL_EXPORT MonoString *
ves_icall_System_Reflection_Assembly_get_fullName (MonoReflectionAssembly *assembly)
{
	MonoDomain *domain = mono_object_domain (assembly); 
	MonoAssembly *mass = assembly->assembly;
	MonoString *res;
	gchar *name;

	name = mono_stringify_assembly_name (&mass->aname);
	res = mono_string_new (domain, name);
	g_free (name);

	return res;
}

ICALL_EXPORT void
ves_icall_System_Reflection_Assembly_FillName (MonoReflectionAssembly *assembly, MonoReflectionAssemblyName *aname)
{
	gchar *absolute;
	MonoAssembly *mass = assembly->assembly;

	if (g_path_is_absolute (mass->image->name)) {
		fill_reflection_assembly_name (mono_object_domain (assembly),
			aname, &mass->aname, mass->image->name, TRUE,
			TRUE, TRUE);
		return;
	}
	absolute = g_build_filename (mass->basedir, mass->image->name, NULL);

	fill_reflection_assembly_name (mono_object_domain (assembly),
		aname, &mass->aname, absolute, TRUE, TRUE,
		TRUE);

	g_free (absolute);
}

ICALL_EXPORT void
ves_icall_System_Reflection_Assembly_InternalGetAssemblyName (MonoString *fname, MonoReflectionAssemblyName *aname)
{
	char *filename;
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	gboolean res;
	MonoImage *image;
	MonoAssemblyName name;
	char *dirname;

	filename = mono_string_to_utf8 (fname);

	dirname = g_path_get_dirname (filename);
	replace_shadow_path (mono_domain_get (), dirname, &filename);
	g_free (dirname);

	image = mono_image_open (filename, &status);

	if (!image){
		MonoException *exc;

		g_free (filename);
		if (status == MONO_IMAGE_IMAGE_INVALID)
			exc = mono_get_exception_bad_image_format2 (NULL, fname);
		else
			exc = mono_get_exception_file_not_found2 (NULL, fname);
		mono_raise_exception (exc);
	}

	res = mono_assembly_fill_assembly_name (image, &name);
	if (!res) {
		mono_image_close (image);
		g_free (filename);
		mono_raise_exception (mono_get_exception_argument ("assemblyFile", "The file does not contain a manifest"));
	}

	fill_reflection_assembly_name (mono_domain_get (), aname, &name, filename,
		TRUE, FALSE, TRUE);

	g_free (filename);
	mono_image_close (image);
}

ICALL_EXPORT MonoBoolean
ves_icall_System_Reflection_Assembly_LoadPermissions (MonoReflectionAssembly *assembly,
	char **minimum, guint32 *minLength, char **optional, guint32 *optLength, char **refused, guint32 *refLength)
{
	MonoBoolean result = FALSE;
	MonoDeclSecurityEntry entry;

	/* SecurityAction.RequestMinimum */
	if (mono_declsec_get_assembly_action (assembly->assembly, SECURITY_ACTION_REQMIN, &entry)) {
		*minimum = entry.blob;
		*minLength = entry.size;
		result = TRUE;
	}
	/* SecurityAction.RequestOptional */
	if (mono_declsec_get_assembly_action (assembly->assembly, SECURITY_ACTION_REQOPT, &entry)) {
		*optional = entry.blob;
		*optLength = entry.size;
		result = TRUE;
	}
	/* SecurityAction.RequestRefuse */
	if (mono_declsec_get_assembly_action (assembly->assembly, SECURITY_ACTION_REQREFUSE, &entry)) {
		*refused = entry.blob;
		*refLength = entry.size;
		result = TRUE;
	}

	return result;	
}

static gboolean
mono_module_type_is_visible (MonoTableInfo *tdef, MonoImage *image, int type)
{
	guint32 attrs, visibility;
	do {
		attrs = mono_metadata_decode_row_col (tdef, type - 1, MONO_TYPEDEF_FLAGS);
		visibility = attrs & TYPE_ATTRIBUTE_VISIBILITY_MASK;
		if (visibility != TYPE_ATTRIBUTE_PUBLIC && visibility != TYPE_ATTRIBUTE_NESTED_PUBLIC)
			return FALSE;

	} while ((type = mono_metadata_token_index (mono_metadata_nested_in_typedef (image, type))));

	return TRUE;
}

static MonoArray*
mono_module_get_types (MonoDomain *domain, MonoImage *image, MonoArray **exceptions, MonoBoolean exportedOnly)
{
	MonoArray *res;
	MonoClass *klass;
	MonoTableInfo *tdef = &image->tables [MONO_TABLE_TYPEDEF];
	int i, count;

	/* we start the count from 1 because we skip the special type <Module> */
	if (exportedOnly) {
		count = 0;
		for (i = 1; i < tdef->rows; ++i) {
			if (mono_module_type_is_visible (tdef, image, i + 1))
				count++;
		}
	} else {
		count = tdef->rows - 1;
	}
	res = mono_array_new (domain, mono_defaults.monotype_class, count);
	*exceptions = mono_array_new (domain, mono_defaults.exception_class, count);
	count = 0;
	for (i = 1; i < tdef->rows; ++i) {
		if (!exportedOnly || mono_module_type_is_visible (tdef, image, i + 1)) {
			MonoError error;
			klass = mono_class_get_checked (image, (i + 1) | MONO_TOKEN_TYPE_DEF, &error);
			g_assert (!mono_loader_get_last_error ()); /* Plug any leaks */
			
			if (klass) {
				mono_array_setref (res, count, mono_type_get_object (domain, &klass->byval_arg));
			} else {
				MonoException *ex = mono_error_convert_to_exception (&error);
				mono_array_setref (*exceptions, count, ex);
			}
			count++;
		}
	}
	
	return res;
}

ICALL_EXPORT MonoArray*
ves_icall_System_Reflection_Assembly_GetTypes (MonoReflectionAssembly *assembly, MonoBoolean exportedOnly)
{
	MonoArray *res = NULL;
	MonoArray *exceptions = NULL;
	MonoImage *image = NULL;
	MonoTableInfo *table = NULL;
	MonoDomain *domain;
	GList *list = NULL;
	int i, len, ex_count;

	domain = mono_object_domain (assembly);

	g_assert (!assembly_is_dynamic (assembly->assembly));
	image = assembly->assembly->image;
	table = &image->tables [MONO_TABLE_FILE];
	res = mono_module_get_types (domain, image, &exceptions, exportedOnly);

	/* Append data from all modules in the assembly */
	for (i = 0; i < table->rows; ++i) {
		if (!(mono_metadata_decode_row_col (table, i, MONO_FILE_FLAGS) & FILE_CONTAINS_NO_METADATA)) {
			MonoImage *loaded_image = mono_assembly_load_module (image->assembly, i + 1);
			if (loaded_image) {
				MonoArray *ex2;
				MonoArray *res2 = mono_module_get_types (domain, loaded_image, &ex2, exportedOnly);
				/* Append the new types to the end of the array */
				if (mono_array_length (res2) > 0) {
					guint32 len1, len2;
					MonoArray *res3, *ex3;

					len1 = mono_array_length (res);
					len2 = mono_array_length (res2);

					res3 = mono_array_new (domain, mono_defaults.monotype_class, len1 + len2);
					mono_array_memcpy_refs (res3, 0, res, 0, len1);
					mono_array_memcpy_refs (res3, len1, res2, 0, len2);
					res = res3;

					ex3 = mono_array_new (domain, mono_defaults.monotype_class, len1 + len2);
					mono_array_memcpy_refs (ex3, 0, exceptions, 0, len1);
					mono_array_memcpy_refs (ex3, len1, ex2, 0, len2);
					exceptions = ex3;
				}
			}
		}
	}

	/* the ReflectionTypeLoadException must have all the types (Types property), 
	 * NULL replacing types which throws an exception. The LoaderException must
	 * contain all exceptions for NULL items.
	 */

	len = mono_array_length (res);

	ex_count = 0;
	for (i = 0; i < len; i++) {
		MonoReflectionType *t = mono_array_get (res, gpointer, i);
		MonoClass *klass;

		if (t) {
			klass = mono_type_get_class (t->type);
			if ((klass != NULL) && klass->exception_type) {
				/* keep the class in the list */
				list = g_list_append (list, klass);
				/* and replace Type with NULL */
				mono_array_setref (res, i, NULL);
			}
		} else {
			ex_count ++;
		}
	}

	if (list || ex_count) {
		GList *tmp = NULL;
		MonoException *exc = NULL;
		MonoArray *exl = NULL;
		int j, length = g_list_length (list) + ex_count;

		mono_loader_clear_error ();

		exl = mono_array_new (domain, mono_defaults.exception_class, length);
		/* Types for which mono_class_get_checked () succeeded */
		for (i = 0, tmp = list; tmp; i++, tmp = tmp->next) {
			MonoException *exc = mono_class_get_exception_for_failure (tmp->data);
			mono_array_setref (exl, i, exc);
		}
		/* Types for which it don't */
		for (j = 0; j < mono_array_length (exceptions); ++j) {
			MonoException *exc = mono_array_get (exceptions, MonoException*, j);
			if (exc) {
				g_assert (i < length);
				mono_array_setref (exl, i, exc);
				i ++;
			}
		}
		g_list_free (list);
		list = NULL;

		exc = mono_get_exception_reflection_type_load (res, exl);
		mono_loader_clear_error ();
		mono_raise_exception (exc);
	}
		
	return res;
}

ICALL_EXPORT gboolean
ves_icall_System_Reflection_AssemblyName_ParseName (MonoReflectionAssemblyName *name, MonoString *assname)
{
	MonoAssemblyName aname;
	MonoDomain *domain = mono_object_domain (name);
	char *val;
	gboolean is_version_defined;
	gboolean is_token_defined;

	aname.public_key = NULL;
	val = mono_string_to_utf8 (assname);
	if (!mono_assembly_name_parse_full (val, &aname, TRUE, &is_version_defined, &is_token_defined)) {
		g_free ((guint8*) aname.public_key);
		g_free (val);
		return FALSE;
	}
	
	fill_reflection_assembly_name (domain, name, &aname, "", is_version_defined,
		FALSE, is_token_defined);

	mono_assembly_name_free (&aname);
	g_free ((guint8*) aname.public_key);
	g_free (val);

	return TRUE;
}

ICALL_EXPORT MonoReflectionType*
ves_icall_System_Reflection_Module_GetGlobalType (MonoReflectionModule *module)
{
	MonoError error;
	MonoDomain *domain = mono_object_domain (module); 
	MonoClass *klass;

	g_assert (module->image);

	if (image_is_dynamic (module->image) && ((MonoDynamicImage*)(module->image))->initial_image)
		/* These images do not have a global type */
		return NULL;

	klass = mono_class_get_checked (module->image, 1 | MONO_TOKEN_TYPE_DEF, &error);
	mono_error_raise_exception (&error);
	return mono_type_get_object (domain, &klass->byval_arg);
}

ICALL_EXPORT void
ves_icall_System_Reflection_Module_Close (MonoReflectionModule *module)
{
	/*if (module->image)
		mono_image_close (module->image);*/
}

ICALL_EXPORT MonoString*
ves_icall_System_Reflection_Module_GetGuidInternal (MonoReflectionModule *module)
{
	MonoDomain *domain = mono_object_domain (module); 

	g_assert (module->image);
	return mono_string_new (domain, module->image->guid);
}

ICALL_EXPORT gpointer
ves_icall_System_Reflection_Module_GetHINSTANCE (MonoReflectionModule *module)
{
#ifdef HOST_WIN32
	if (module->image && module->image->is_module_handle)
		return module->image->raw_data;
#endif

	return (gpointer) (-1);
}

ICALL_EXPORT void
ves_icall_System_Reflection_Module_GetPEKind (MonoImage *image, gint32 *pe_kind, gint32 *machine)
{
	if (image_is_dynamic (image)) {
		MonoDynamicImage *dyn = (MonoDynamicImage*)image;
		*pe_kind = dyn->pe_kind;
		*machine = dyn->machine;
	}
	else {
		*pe_kind = ((MonoCLIImageInfo*)(image->image_info))->cli_cli_header.ch_flags & 0x3;
		*machine = ((MonoCLIImageInfo*)(image->image_info))->cli_header.coff.coff_machine;
	}
}

ICALL_EXPORT gint32
ves_icall_System_Reflection_Module_GetMDStreamVersion (MonoImage *image)
{
	return (image->md_version_major << 16) | (image->md_version_minor);
}

ICALL_EXPORT MonoArray*
ves_icall_System_Reflection_Module_InternalGetTypes (MonoReflectionModule *module)
{
	MonoArray *exceptions;
	int i;

	if (!module->image)
		return mono_array_new (mono_object_domain (module), mono_defaults.monotype_class, 0);
	else {
		MonoArray *res = mono_module_get_types (mono_object_domain (module), module->image, &exceptions, FALSE);
		for (i = 0; i < mono_array_length (exceptions); ++i) {
			MonoException *ex = mono_array_get (exceptions, MonoException *, i);
			if (ex)
				mono_raise_exception (ex);
		}
		return res;
	}
}

static gboolean
mono_memberref_is_method (MonoImage *image, guint32 token)
{
	if (!image_is_dynamic (image)) {
		guint32 cols [MONO_MEMBERREF_SIZE];
		const char *sig;
		mono_metadata_decode_row (&image->tables [MONO_TABLE_MEMBERREF], mono_metadata_token_index (token) - 1, cols, MONO_MEMBERREF_SIZE);
		sig = mono_metadata_blob_heap (image, cols [MONO_MEMBERREF_SIGNATURE]);
		mono_metadata_decode_blob_size (sig, &sig);
		return (*sig != 0x6);
	} else {
		MonoClass *handle_class;

		if (!mono_lookup_dynamic_token_class (image, token, FALSE, &handle_class, NULL))
			return FALSE;

		return mono_defaults.methodhandle_class == handle_class;
	}
}

static void
init_generic_context_from_args (MonoGenericContext *context, MonoArray *type_args, MonoArray *method_args)
{
	if (type_args)
		context->class_inst = mono_metadata_get_generic_inst (mono_array_length (type_args),
								      mono_array_addr (type_args, MonoType*, 0));
	else
		context->class_inst = NULL;
	if (method_args)
		context->method_inst = mono_metadata_get_generic_inst (mono_array_length (method_args),
								       mono_array_addr (method_args, MonoType*, 0));
	else
		context->method_inst = NULL;
}

ICALL_EXPORT MonoType*
ves_icall_System_Reflection_Module_ResolveTypeToken (MonoImage *image, guint32 token, MonoArray *type_args, MonoArray *method_args, MonoResolveTokenError *resolve_error)
{
	MonoClass *klass;
	int table = mono_metadata_token_table (token);
	int index = mono_metadata_token_index (token);
	MonoGenericContext context;
	MonoError error;

	*resolve_error = ResolveTokenError_Other;

	/* Validate token */
	if ((table != MONO_TABLE_TYPEDEF) && (table != MONO_TABLE_TYPEREF) && 
		(table != MONO_TABLE_TYPESPEC)) {
		*resolve_error = ResolveTokenError_BadTable;
		return NULL;
	}

	if (image_is_dynamic (image)) {
		if ((table == MONO_TABLE_TYPEDEF) || (table == MONO_TABLE_TYPEREF)) {
			klass = mono_lookup_dynamic_token_class (image, token, FALSE, NULL, NULL);
			return klass ? &klass->byval_arg : NULL;
		}

		init_generic_context_from_args (&context, type_args, method_args);
		klass = mono_lookup_dynamic_token_class (image, token, FALSE, NULL, &context);
		return klass ? &klass->byval_arg : NULL;
	}

	if ((index <= 0) || (index > image->tables [table].rows)) {
		*resolve_error = ResolveTokenError_OutOfRange;
		return NULL;
	}

	init_generic_context_from_args (&context, type_args, method_args);
	klass = mono_class_get_checked (image, token, &error);
	if (klass)
		klass = mono_class_inflate_generic_class_checked (klass, &context, &error);
	mono_error_raise_exception (&error);

	if (klass)
		return &klass->byval_arg;
	else
		return NULL;
}

ICALL_EXPORT MonoMethod*
ves_icall_System_Reflection_Module_ResolveMethodToken (MonoImage *image, guint32 token, MonoArray *type_args, MonoArray *method_args, MonoResolveTokenError *resolve_error)
{
	MonoError error;
	int table = mono_metadata_token_table (token);
	int index = mono_metadata_token_index (token);
	MonoGenericContext context;
	MonoMethod *method;

	*resolve_error = ResolveTokenError_Other;

	/* Validate token */
	if ((table != MONO_TABLE_METHOD) && (table != MONO_TABLE_METHODSPEC) && 
		(table != MONO_TABLE_MEMBERREF)) {
		*resolve_error = ResolveTokenError_BadTable;
		return NULL;
	}

	if (image_is_dynamic (image)) {
		if (table == MONO_TABLE_METHOD)
			return mono_lookup_dynamic_token_class (image, token, FALSE, NULL, NULL);

		if ((table == MONO_TABLE_MEMBERREF) && !(mono_memberref_is_method (image, token))) {
			*resolve_error = ResolveTokenError_BadTable;
			return NULL;
		}

		init_generic_context_from_args (&context, type_args, method_args);
		return mono_lookup_dynamic_token_class (image, token, FALSE, NULL, &context);
	}

	if ((index <= 0) || (index > image->tables [table].rows)) {
		*resolve_error = ResolveTokenError_OutOfRange;
		return NULL;
	}
	if ((table == MONO_TABLE_MEMBERREF) && (!mono_memberref_is_method (image, token))) {
		*resolve_error = ResolveTokenError_BadTable;
		return NULL;
	}

	init_generic_context_from_args (&context, type_args, method_args);
	method = mono_get_method_checked (image, token, NULL, &context, &error);
	mono_error_raise_exception (&error);

	return method;
}

ICALL_EXPORT MonoString*
ves_icall_System_Reflection_Module_ResolveStringToken (MonoImage *image, guint32 token, MonoResolveTokenError *error)
{
	int index = mono_metadata_token_index (token);

	*error = ResolveTokenError_Other;

	/* Validate token */
	if (mono_metadata_token_code (token) != MONO_TOKEN_STRING) {
		*error = ResolveTokenError_BadTable;
		return NULL;
	}

	if (image_is_dynamic (image))
		return mono_lookup_dynamic_token_class (image, token, FALSE, NULL, NULL);

	if ((index <= 0) || (index >= image->heap_us.size)) {
		*error = ResolveTokenError_OutOfRange;
		return NULL;
	}

	/* FIXME: What to do if the index points into the middle of a string ? */

	return mono_ldstr (mono_domain_get (), image, index);
}

ICALL_EXPORT MonoClassField*
ves_icall_System_Reflection_Module_ResolveFieldToken (MonoImage *image, guint32 token, MonoArray *type_args, MonoArray *method_args, MonoResolveTokenError *resolve_error)
{
	MonoError error;
	MonoClass *klass;
	int table = mono_metadata_token_table (token);
	int index = mono_metadata_token_index (token);
	MonoGenericContext context;
	MonoClassField *field;

	*resolve_error = ResolveTokenError_Other;

	/* Validate token */
	if ((table != MONO_TABLE_FIELD) && (table != MONO_TABLE_MEMBERREF)) {
		*resolve_error = ResolveTokenError_BadTable;
		return NULL;
	}

	if (image_is_dynamic (image)) {
		if (table == MONO_TABLE_FIELD)
			return mono_lookup_dynamic_token_class (image, token, FALSE, NULL, NULL);

		if (mono_memberref_is_method (image, token)) {
			*resolve_error = ResolveTokenError_BadTable;
			return NULL;
		}

		init_generic_context_from_args (&context, type_args, method_args);
		return mono_lookup_dynamic_token_class (image, token, FALSE, NULL, &context);
	}

	if ((index <= 0) || (index > image->tables [table].rows)) {
		*resolve_error = ResolveTokenError_OutOfRange;
		return NULL;
	}
	if ((table == MONO_TABLE_MEMBERREF) && (mono_memberref_is_method (image, token))) {
		*resolve_error = ResolveTokenError_BadTable;
		return NULL;
	}

	init_generic_context_from_args (&context, type_args, method_args);
	field = mono_field_from_token_checked (image, token, &klass, &context, &error);
	mono_error_raise_exception (&error);
	
	return field;
}


ICALL_EXPORT MonoObject*
ves_icall_System_Reflection_Module_ResolveMemberToken (MonoImage *image, guint32 token, MonoArray *type_args, MonoArray *method_args, MonoResolveTokenError *error)
{
	int table = mono_metadata_token_table (token);

	*error = ResolveTokenError_Other;

	switch (table) {
	case MONO_TABLE_TYPEDEF:
	case MONO_TABLE_TYPEREF:
	case MONO_TABLE_TYPESPEC: {
		MonoType *t = ves_icall_System_Reflection_Module_ResolveTypeToken (image, token, type_args, method_args, error);
		if (t)
			return (MonoObject*)mono_type_get_object (mono_domain_get (), t);
		else
			return NULL;
	}
	case MONO_TABLE_METHOD:
	case MONO_TABLE_METHODSPEC: {
		MonoMethod *m = ves_icall_System_Reflection_Module_ResolveMethodToken (image, token, type_args, method_args, error);
		if (m)
			return (MonoObject*)mono_method_get_object (mono_domain_get (), m, m->klass);
		else
			return NULL;
	}		
	case MONO_TABLE_FIELD: {
		MonoClassField *f = ves_icall_System_Reflection_Module_ResolveFieldToken (image, token, type_args, method_args, error);
		if (f)
			return (MonoObject*)mono_field_get_object (mono_domain_get (), f->parent, f);
		else
			return NULL;
	}
	case MONO_TABLE_MEMBERREF:
		if (mono_memberref_is_method (image, token)) {
			MonoMethod *m = ves_icall_System_Reflection_Module_ResolveMethodToken (image, token, type_args, method_args, error);
			if (m)
				return (MonoObject*)mono_method_get_object (mono_domain_get (), m, m->klass);
			else
				return NULL;
		}
		else {
			MonoClassField *f = ves_icall_System_Reflection_Module_ResolveFieldToken (image, token, type_args, method_args, error);
			if (f)
				return (MonoObject*)mono_field_get_object (mono_domain_get (), f->parent, f);
			else
				return NULL;
		}
		break;

	default:
		*error = ResolveTokenError_BadTable;
	}

	return NULL;
}

ICALL_EXPORT MonoArray*
ves_icall_System_Reflection_Module_ResolveSignature (MonoImage *image, guint32 token, MonoResolveTokenError *error)
{
	int table = mono_metadata_token_table (token);
	int idx = mono_metadata_token_index (token);
	MonoTableInfo *tables = image->tables;
	guint32 sig, len;
	const char *ptr;
	MonoArray *res;

	*error = ResolveTokenError_OutOfRange;

	/* FIXME: Support other tables ? */
	if (table != MONO_TABLE_STANDALONESIG)
		return NULL;

	if (image_is_dynamic (image))
		return NULL;

	if ((idx == 0) || (idx > tables [MONO_TABLE_STANDALONESIG].rows))
		return NULL;

	sig = mono_metadata_decode_row_col (&tables [MONO_TABLE_STANDALONESIG], idx - 1, 0);

	ptr = mono_metadata_blob_heap (image, sig);
	len = mono_metadata_decode_blob_size (ptr, &ptr);

	res = mono_array_new (mono_domain_get (), mono_defaults.byte_class, len);
	memcpy (mono_array_addr (res, guint8, 0), ptr, len);
	return res;
}

ICALL_EXPORT MonoReflectionType*
ves_icall_ModuleBuilder_create_modified_type (MonoReflectionTypeBuilder *tb, MonoString *smodifiers)
{
	MonoClass *klass;
	int isbyref = 0, rank;
	char *str = mono_string_to_utf8 (smodifiers);
	char *p;

	klass = mono_class_from_mono_type (tb->type.type);
	p = str;
	/* logic taken from mono_reflection_parse_type(): keep in sync */
	while (*p) {
		switch (*p) {
		case '&':
			if (isbyref) { /* only one level allowed by the spec */
				g_free (str);
				return NULL;
			}
			isbyref = 1;
			p++;
			g_free (str);
			return mono_type_get_object (mono_object_domain (tb), &klass->this_arg);
			break;
		case '*':
			klass = mono_ptr_class_get (&klass->byval_arg);
			mono_class_init (klass);
			p++;
			break;
		case '[':
			rank = 1;
			p++;
			while (*p) {
				if (*p == ']')
					break;
				if (*p == ',')
					rank++;
				else if (*p != '*') { /* '*' means unknown lower bound */
					g_free (str);
					return NULL;
				}
				++p;
			}
			if (*p != ']') {
				g_free (str);
				return NULL;
			}
			p++;
			klass = mono_array_class_get (klass, rank);
			mono_class_init (klass);
			break;
		default:
			break;
		}
	}
	g_free (str);
	return mono_type_get_object (mono_object_domain (tb), &klass->byval_arg);
}

ICALL_EXPORT MonoBoolean
ves_icall_Type_IsArrayImpl (MonoReflectionType *t)
{
	MonoType *type;
	MonoBoolean res;

	type = t->type;
	res = !type->byref && (type->type == MONO_TYPE_ARRAY || type->type == MONO_TYPE_SZARRAY);

	return res;
}

static void
check_for_invalid_type (MonoClass *klass)
{
	char *name;
	MonoString *str;
	if (klass->byval_arg.type != MONO_TYPE_TYPEDBYREF)
		return;

	name = mono_type_get_full_name (klass);
	str =  mono_string_new (mono_domain_get (), name);
	g_free (name);
	mono_raise_exception ((MonoException*)mono_get_exception_type_load (str, NULL));

}
ICALL_EXPORT MonoReflectionType *
ves_icall_Type_make_array_type (MonoReflectionType *type, int rank)
{
	MonoClass *klass, *aklass;

	klass = mono_class_from_mono_type (type->type);
	check_for_invalid_type (klass);

	if (rank == 0) //single dimentional array
		aklass = mono_array_class_get (klass, 1);
	else
		aklass = mono_bounded_array_class_get (klass, rank, TRUE);

	return mono_type_get_object (mono_object_domain (type), &aklass->byval_arg);
}

ICALL_EXPORT MonoReflectionType *
ves_icall_Type_make_byref_type (MonoReflectionType *type)
{
	MonoClass *klass;

	klass = mono_class_from_mono_type (type->type);
	mono_class_init_or_throw (klass);
	check_for_invalid_type (klass);

	return mono_type_get_object (mono_object_domain (type), &klass->this_arg);
}

ICALL_EXPORT MonoReflectionType *
ves_icall_Type_MakePointerType (MonoReflectionType *type)
{
	MonoClass *klass, *pklass;

	klass = mono_class_from_mono_type (type->type);
	mono_class_init_or_throw (klass);
	check_for_invalid_type (klass);

	pklass = mono_ptr_class_get (type->type);

	return mono_type_get_object (mono_object_domain (type), &pklass->byval_arg);
}

ICALL_EXPORT MonoObject *
ves_icall_System_Delegate_CreateDelegate_internal (MonoReflectionType *type, MonoObject *target,
						   MonoReflectionMethod *info, MonoBoolean throwOnBindFailure)
{
	MonoClass *delegate_class = mono_class_from_mono_type (type->type);
	MonoObject *delegate;
	gpointer func;
	MonoMethod *method = info->method;

	mono_class_init_or_throw (delegate_class);

	mono_assert (delegate_class->parent == mono_defaults.multicastdelegate_class);

	if (mono_security_core_clr_enabled ()) {
		if (!mono_security_core_clr_ensure_delegate_creation (method, throwOnBindFailure))
			return NULL;
	}

	delegate = mono_object_new (mono_object_domain (type), delegate_class);

	if (method_is_dynamic (method)) {
		/* Creating a trampoline would leak memory */
		func = mono_compile_method (method);
	} else {
		if (target && method->flags & METHOD_ATTRIBUTE_VIRTUAL && method->klass != mono_object_class (target))
			method = mono_object_get_virtual_method (target, method);
		func = mono_create_ftnptr (mono_domain_get (),
			mono_runtime_create_jump_trampoline (mono_domain_get (), method, TRUE));
	}

	mono_delegate_ctor_with_method (delegate, target, func, method);

	return delegate;
}

ICALL_EXPORT void
ves_icall_System_Delegate_SetMulticastInvoke (MonoDelegate *this)
{
	/* Reset the invoke impl to the default one */
	this->invoke_impl = mono_runtime_create_delegate_trampoline (this->object.vtable->klass);
}

/*
 * Magic number to convert a time which is relative to
 * Jan 1, 1970 into a value which is relative to Jan 1, 0001.
 */
#define	EPOCH_ADJUST	((guint64)62135596800LL)

/*
 * Magic number to convert FILETIME base Jan 1, 1601 to DateTime - base Jan, 1, 0001
 */
#define FILETIME_ADJUST ((guint64)504911232000000000LL)

#ifdef HOST_WIN32
/* convert a SYSTEMTIME which is of the form "last thursday in october" to a real date */
static void
convert_to_absolute_date(SYSTEMTIME *date)
{
#define IS_LEAP(y) ((y % 4) == 0 && ((y % 100) != 0 || (y % 400) == 0))
	static int days_in_month[] = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
	static int leap_days_in_month[] = { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
	/* from the calendar FAQ */
	int a = (14 - date->wMonth) / 12;
	int y = date->wYear - a;
	int m = date->wMonth + 12 * a - 2;
	int d = (1 + y + y/4 - y/100 + y/400 + (31*m)/12) % 7;

	/* d is now the day of the week for the first of the month (0 == Sunday) */

	int day_of_week = date->wDayOfWeek;

	/* set day_in_month to the first day in the month which falls on day_of_week */    
	int day_in_month = 1 + (day_of_week - d);
	if (day_in_month <= 0)
		day_in_month += 7;

	/* wDay is 1 for first weekday in month, 2 for 2nd ... 5 means last - so work that out allowing for days in the month */
	date->wDay = day_in_month + (date->wDay - 1) * 7;
	if (date->wDay > (IS_LEAP(date->wYear) ? leap_days_in_month[date->wMonth - 1] : days_in_month[date->wMonth - 1]))
		date->wDay -= 7;
}
#endif

#ifndef HOST_WIN32
/*
 * Return's the offset from GMT of a local time.
 * 
 *  tm is a local time
 *  t  is the same local time as seconds.
 */
static int 
gmt_offset(struct tm *tm, time_t t)
{
#if defined (HAVE_TM_GMTOFF)
	return tm->tm_gmtoff;
#else
	struct tm g;
	time_t t2;
	g = *gmtime(&t);
	g.tm_isdst = tm->tm_isdst;
	t2 = mktime(&g);
	return (int)difftime(t, t2);
#endif
}
#endif
/*
 * This is heavily based on zdump.c from glibc 2.2.
 *
 *  * data[0]:  start of daylight saving time (in DateTime ticks).
 *  * data[1]:  end of daylight saving time (in DateTime ticks).
 *  * data[2]:  utcoffset (in TimeSpan ticks).
 *  * data[3]:  additional offset when daylight saving (in TimeSpan ticks).
 *  * name[0]:  name of this timezone when not daylight saving.
 *  * name[1]:  name of this timezone when daylight saving.
 *
 *  FIXME: This only works with "standard" Unix dates (years between 1900 and 2100) while
 *         the class library allows years between 1 and 9999.
 *
 *  Returns true on success and zero on failure.
 */
ICALL_EXPORT guint32
ves_icall_System_CurrentSystemTimeZone_GetTimeZoneData (guint32 year, MonoArray **data, MonoArray **names)
{
#ifndef HOST_WIN32
	MonoDomain *domain = mono_domain_get ();
	struct tm start, tt;
	time_t t;

	long int gmtoff, gmtoff_after, gmtoff_st, gmtoff_ds;
	int day, transitioned;
	char tzone [64];

	gmtoff_st = gmtoff_ds = transitioned = 0;

	MONO_CHECK_ARG_NULL (data);
	MONO_CHECK_ARG_NULL (names);

	mono_gc_wbarrier_generic_store (data, (MonoObject*) mono_array_new (domain, mono_defaults.int64_class, 4));
	mono_gc_wbarrier_generic_store (names, (MonoObject*) mono_array_new (domain, mono_defaults.string_class, 2));

	/* 
	 * no info is better than crashing: we'll need our own tz data
	 * to make this work properly, anyway. The range is probably
	 * reduced to 1970 .. 2037 because that is what mktime is
	 * guaranteed to support (we get into an infinite loop
	 * otherwise).
	 */

	memset (&start, 0, sizeof (start));

	start.tm_mday = 1;
	start.tm_year = year-1900;

	t = mktime (&start);

	if ((year < 1970) || (year > 2037) || (t == -1)) {
		t = time (NULL);
		tt = *localtime (&t);
		strftime (tzone, sizeof (tzone), "%Z", &tt);
		mono_array_setref ((*names), 0, mono_string_new (domain, tzone));
		mono_array_setref ((*names), 1, mono_string_new (domain, tzone));
		return 1;
	}

	gmtoff = gmt_offset (&start, t);

	/* For each day of the year, calculate the tm_gmtoff. */
	for (day = 0; day < 365 && transitioned < 2; day++) {

		t += 3600*24;
		tt = *localtime (&t);

        gmtoff_after = gmt_offset(&tt, t);

		/* Daylight saving starts or ends here. */
		if (gmtoff_after != gmtoff) {
			struct tm tt1;
			time_t t1;

			/* Try to find the exact hour when daylight saving starts/ends. */
			t1 = t;
			do {
				t1 -= 3600;
				tt1 = *localtime (&t1);
			} while (gmt_offset (&tt1, t1) != gmtoff);

			/* Try to find the exact minute when daylight saving starts/ends. */
			do {
				t1 += 60;
				tt1 = *localtime (&t1);
			} while (gmt_offset (&tt1, t1) == gmtoff);
			t1+=gmtoff;
			strftime (tzone, sizeof (tzone), "%Z", &tt);
			
			/* Write data, if we're already in daylight saving, we're done. */
			if (tt.tm_isdst) {
				mono_array_setref ((*names), 1, mono_string_new (domain, tzone));
				mono_array_set ((*data), gint64, 0, ((gint64)t1 + EPOCH_ADJUST) * 10000000L);
				if (gmtoff_ds == 0) {
					gmtoff_st = gmtoff;
					gmtoff_ds = gmtoff_after;
				}
				transitioned++;
			} else {
				time_t te;
				te = mktime (&tt);
				
				mono_array_setref ((*names), 0, mono_string_new (domain, tzone));
				mono_array_set ((*data), gint64, 1, ((gint64)t1 + EPOCH_ADJUST) * 10000000L);
				if (gmtoff_ds == 0) {
					gmtoff_st = gmtoff_after;
					gmtoff_ds = gmtoff;
				}
				transitioned++;
			}

			/* This is only set once when we enter daylight saving. */
			if (tt1.tm_isdst) {
				mono_array_set ((*data), gint64, 2, (gint64)gmtoff_st * 10000000L);
				mono_array_set ((*data), gint64, 3, (gint64)(gmtoff_ds - gmtoff_st) * 10000000L);
			}
			gmtoff = gmt_offset (&tt, t);
		}
	}

	if (transitioned < 2) {
		strftime (tzone, sizeof (tzone), "%Z", &tt);
		mono_array_setref ((*names), 0, mono_string_new (domain, tzone));
		mono_array_setref ((*names), 1, mono_string_new (domain, tzone));
		mono_array_set ((*data), gint64, 0, 0);
		mono_array_set ((*data), gint64, 1, 0);
		mono_array_set ((*data), gint64, 2, (gint64) gmtoff * 10000000L);
		mono_array_set ((*data), gint64, 3, 0);
	}

	return 1;
#else
	MonoDomain *domain = mono_domain_get ();
	TIME_ZONE_INFORMATION tz_info;
	FILETIME ft;
	int i;
	int err, tz_id;

	tz_id = GetTimeZoneInformation (&tz_info);
	if (tz_id == TIME_ZONE_ID_INVALID)
		return 0;

	MONO_CHECK_ARG_NULL (data);
	MONO_CHECK_ARG_NULL (names);

	mono_gc_wbarrier_generic_store (data, mono_array_new (domain, mono_defaults.int64_class, 4));
	mono_gc_wbarrier_generic_store (names, mono_array_new (domain, mono_defaults.string_class, 2));

	for (i = 0; i < 32; ++i)
		if (!tz_info.DaylightName [i])
			break;
	mono_array_setref ((*names), 1, mono_string_new_utf16 (domain, tz_info.DaylightName, i));
	for (i = 0; i < 32; ++i)
		if (!tz_info.StandardName [i])
			break;
	mono_array_setref ((*names), 0, mono_string_new_utf16 (domain, tz_info.StandardName, i));

	if ((year <= 1601) || (year > 30827)) {
		/*
		 * According to MSDN, the MS time functions can't handle dates outside
		 * this interval.
		 */
		return 1;
	}

	/* even if the timezone has no daylight savings it may have Bias (e.g. GMT+13 it seems) */
	if (tz_id != TIME_ZONE_ID_UNKNOWN) {
		tz_info.StandardDate.wYear = year;
		convert_to_absolute_date(&tz_info.StandardDate);
		err = SystemTimeToFileTime (&tz_info.StandardDate, &ft);
		//g_assert(err);
		if (err == 0)
			return 0;
		
		mono_array_set ((*data), gint64, 1, FILETIME_ADJUST + (((guint64)ft.dwHighDateTime<<32) | ft.dwLowDateTime));
		tz_info.DaylightDate.wYear = year;
		convert_to_absolute_date(&tz_info.DaylightDate);
		err = SystemTimeToFileTime (&tz_info.DaylightDate, &ft);
		//g_assert(err);
		if (err == 0)
			return 0;
		
		mono_array_set ((*data), gint64, 0, FILETIME_ADJUST + (((guint64)ft.dwHighDateTime<<32) | ft.dwLowDateTime));
	}
	mono_array_set ((*data), gint64, 2, (tz_info.Bias + tz_info.StandardBias) * -600000000LL);
	mono_array_set ((*data), gint64, 3, (tz_info.DaylightBias - tz_info.StandardBias) * -600000000LL);

	return 1;
#endif
}

/* System.Buffer */

static inline gint32 
mono_array_get_byte_length (MonoArray *array)
{
	MonoClass *klass;
	int length;
	int i;

	klass = array->obj.vtable->klass;

	if (array->bounds == NULL)
		length = array->max_length;
	else {
		length = 1;
		for (i = 0; i < klass->rank; ++ i)
			length *= array->bounds [i].length;
	}

	switch (klass->element_class->byval_arg.type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		return length;
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		return length << 1;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_R4:
		return length << 2;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		return length * sizeof (gpointer);
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R8:
		return length << 3;
	default:
		return -1;
	}
}

ICALL_EXPORT gint32 
ves_icall_System_Buffer_ByteLengthInternal (MonoArray *array) 
{
	return mono_array_get_byte_length (array);
}

ICALL_EXPORT gint8 
ves_icall_System_Buffer_GetByteInternal (MonoArray *array, gint32 idx) 
{
	return mono_array_get (array, gint8, idx);
}

ICALL_EXPORT void 
ves_icall_System_Buffer_SetByteInternal (MonoArray *array, gint32 idx, gint8 value) 
{
	mono_array_set (array, gint8, idx, value);
}

ICALL_EXPORT MonoBoolean
ves_icall_System_Buffer_BlockCopyInternal (MonoArray *src, gint32 src_offset, MonoArray *dest, gint32 dest_offset, gint32 count) 
{
	guint8 *src_buf, *dest_buf;

	/* This is called directly from the class libraries without going through the managed wrapper */
	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (dest);

	/* watch out for integer overflow */
	if ((src_offset > mono_array_get_byte_length (src) - count) || (dest_offset > mono_array_get_byte_length (dest) - count))
		return FALSE;

	src_buf = (guint8 *)src->vector + src_offset;
	dest_buf = (guint8 *)dest->vector + dest_offset;

	if (src != dest)
		memcpy (dest_buf, src_buf, count);
	else
		memmove (dest_buf, src_buf, count); /* Source and dest are the same array */

	return TRUE;
}

#ifndef DISABLE_REMOTING
ICALL_EXPORT MonoObject *
ves_icall_Remoting_RealProxy_GetTransparentProxy (MonoObject *this, MonoString *class_name)
{
	MonoDomain *domain = mono_object_domain (this); 
	MonoObject *res;
	MonoRealProxy *rp = ((MonoRealProxy *)this);
	MonoTransparentProxy *tp;
	MonoType *type;
	MonoClass *klass;

	res = mono_object_new (domain, mono_defaults.transparent_proxy_class);
	tp = (MonoTransparentProxy*) res;
	
	MONO_OBJECT_SETREF (tp, rp, rp);
	type = ((MonoReflectionType *)rp->class_to_proxy)->type;
	klass = mono_class_from_mono_type (type);

	tp->custom_type_info = (mono_object_isinst (this, mono_defaults.iremotingtypeinfo_class) != NULL);
	tp->remote_class = mono_remote_class (domain, class_name, klass);

	res->vtable = mono_remote_class_vtable (domain, tp->remote_class, rp);
	return res;
}

ICALL_EXPORT MonoReflectionType *
ves_icall_Remoting_RealProxy_InternalGetProxyType (MonoTransparentProxy *tp)
{
	return mono_type_get_object (mono_object_domain (tp), &tp->remote_class->proxy_class->byval_arg);
}
#endif

/* System.Environment */

MonoString*
ves_icall_System_Environment_get_UserName (void)
{
	/* using glib is more portable */
	return mono_string_new (mono_domain_get (), g_get_user_name ());
}


ICALL_EXPORT MonoString *
ves_icall_System_Environment_get_MachineName (void)
{
#if defined (HOST_WIN32)
	gunichar2 *buf;
	guint32 len;
	MonoString *result;

	len = MAX_COMPUTERNAME_LENGTH + 1;
	buf = g_new (gunichar2, len);

	result = NULL;
	if (GetComputerName (buf, (PDWORD) &len))
		result = mono_string_new_utf16 (mono_domain_get (), buf, len);

	g_free (buf);
	return result;
#elif !defined(DISABLE_SOCKETS)
	gchar buf [256];
	MonoString *result;

	if (gethostname (buf, sizeof (buf)) == 0)
		result = mono_string_new (mono_domain_get (), buf);
	else
		result = NULL;
	
	return result;
#else
	return mono_string_new (mono_domain_get (), "mono");
#endif
}

ICALL_EXPORT int
ves_icall_System_Environment_get_Platform (void)
{
#if defined (TARGET_WIN32)
	/* Win32NT */
	return 2;
#elif defined(__MACH__)
	/* OSX */
	//
	// Notice that the value is hidden from user code, and only exposed
	// to mscorlib.   This is due to Mono's Unix/MacOS code predating the
	// define and making assumptions based on Unix/128/4 values before there
	// was a MacOS define.    Lots of code would assume that not-Unix meant
	// Windows, but in this case, it would be OSX. 
	//
	return 6;
#else
	/* Unix */
	return 4;
#endif
}

ICALL_EXPORT MonoString *
ves_icall_System_Environment_get_NewLine (void)
{
#if defined (HOST_WIN32)
	return mono_string_new (mono_domain_get (), "\r\n");
#else
	return mono_string_new (mono_domain_get (), "\n");
#endif
}

ICALL_EXPORT MonoString *
ves_icall_System_Environment_GetEnvironmentVariable (MonoString *name)
{
	const gchar *value;
	gchar *utf8_name;

	if (name == NULL)
		return NULL;

	utf8_name = mono_string_to_utf8 (name);	/* FIXME: this should be ascii */
	value = g_getenv (utf8_name);

	g_free (utf8_name);

	if (value == 0)
		return NULL;
	
	return mono_string_new (mono_domain_get (), value);
}

/*
 * There is no standard way to get at environ.
 */
#ifndef _MSC_VER
#ifndef __MINGW32_VERSION
#if defined(__APPLE__) && !defined (__arm__)
/* Apple defines this in crt_externs.h but doesn't provide that header for 
 * arm-apple-darwin9.  We'll manually define the symbol on Apple as it does
 * in fact exist on all implementations (so far) 
 */
gchar ***_NSGetEnviron(void);
#define environ (*_NSGetEnviron())
#else
extern
char **environ;
#endif
#endif
#endif

ICALL_EXPORT MonoArray *
ves_icall_System_Environment_GetEnvironmentVariableNames (void)
{
#ifdef HOST_WIN32
	MonoArray *names;
	MonoDomain *domain;
	MonoString *str;
	WCHAR* env_strings;
	WCHAR* env_string;
	WCHAR* equal_str;
	int n = 0;

	env_strings = GetEnvironmentStrings();

	if (env_strings) {
		env_string = env_strings;
		while (*env_string != '\0') {
		/* weird case that MS seems to skip */
			if (*env_string != '=')
				n++;
			while (*env_string != '\0')
				env_string++;
			env_string++;
		}
	}

	domain = mono_domain_get ();
	names = mono_array_new (domain, mono_defaults.string_class, n);

	if (env_strings) {
		n = 0;
		env_string = env_strings;
		while (*env_string != '\0') {
			/* weird case that MS seems to skip */
			if (*env_string != '=') {
				equal_str = wcschr(env_string, '=');
				g_assert(equal_str);
				str = mono_string_new_utf16 (domain, env_string, equal_str-env_string);
				mono_array_setref (names, n, str);
				n++;
			}
			while (*env_string != '\0')
				env_string++;
			env_string++;
		}

		FreeEnvironmentStrings (env_strings);
	}

	return names;

#else
	MonoArray *names;
	MonoDomain *domain;
	MonoString *str;
	gchar **e, **parts;
	int n;

	n = 0;
	for (e = environ; *e != 0; ++ e)
		++ n;

	domain = mono_domain_get ();
	names = mono_array_new (domain, mono_defaults.string_class, n);

	n = 0;
	for (e = environ; *e != 0; ++ e) {
		parts = g_strsplit (*e, "=", 2);
		if (*parts != 0) {
			str = mono_string_new (domain, *parts);
			mono_array_setref (names, n, str);
		}

		g_strfreev (parts);

		++ n;
	}

	return names;
#endif
}

/*
 * If your platform lacks setenv/unsetenv, you must upgrade your glib.
 */
#if !GLIB_CHECK_VERSION(2,4,0)
#define g_setenv(a,b,c)   setenv(a,b,c)
#define g_unsetenv(a) unsetenv(a)
#endif

ICALL_EXPORT void
ves_icall_System_Environment_InternalSetEnvironmentVariable (MonoString *name, MonoString *value)
{
#ifdef HOST_WIN32
	gunichar2 *utf16_name, *utf16_value;
#else
	gchar *utf8_name, *utf8_value;
	MonoError error;
#endif

#ifdef HOST_WIN32
	utf16_name = mono_string_to_utf16 (name);
	if ((value == NULL) || (mono_string_length (value) == 0) || (mono_string_chars (value)[0] == 0)) {
		SetEnvironmentVariable (utf16_name, NULL);
		g_free (utf16_name);
		return;
	}

	utf16_value = mono_string_to_utf16 (value);

	SetEnvironmentVariable (utf16_name, utf16_value);

	g_free (utf16_name);
	g_free (utf16_value);
#else
	utf8_name = mono_string_to_utf8 (name);	/* FIXME: this should be ascii */

	if ((value == NULL) || (mono_string_length (value) == 0) || (mono_string_chars (value)[0] == 0)) {
		g_unsetenv (utf8_name);
		g_free (utf8_name);
		return;
	}

	utf8_value = mono_string_to_utf8_checked (value, &error);
	if (!mono_error_ok (&error)) {
		g_free (utf8_name);
		mono_error_raise_exception (&error);
	}
	g_setenv (utf8_name, utf8_value, TRUE);

	g_free (utf8_name);
	g_free (utf8_value);
#endif
}

ICALL_EXPORT void
ves_icall_System_Environment_Exit (int result)
{
	mono_environment_exitcode_set (result);

/* FIXME: There are some cleanup hangs that should be worked out, but
 * if the program is going to exit, everything will be cleaned up when
 * NaCl exits anyway.
 */
#ifndef __native_client__
	if (!mono_runtime_try_shutdown ())
		mono_thread_exit ();

	/* Suspend all managed threads since the runtime is going away */
	mono_thread_suspend_all_other_threads ();

	mono_runtime_quit ();
#endif

	/* we may need to do some cleanup here... */
	exit (result);
}

ICALL_EXPORT MonoString*
ves_icall_System_Environment_GetGacPath (void)
{
	return mono_string_new (mono_domain_get (), mono_assembly_getrootdir ());
}

ICALL_EXPORT MonoString*
ves_icall_System_Environment_GetWindowsFolderPath (int folder)
{
#if defined (HOST_WIN32)
	#ifndef CSIDL_FLAG_CREATE
		#define CSIDL_FLAG_CREATE	0x8000
	#endif

	WCHAR path [MAX_PATH];
	/* Create directory if no existing */
	if (SUCCEEDED (SHGetFolderPathW (NULL, folder | CSIDL_FLAG_CREATE, NULL, 0, path))) {
		int len = 0;
		while (path [len])
			++ len;
		return mono_string_new_utf16 (mono_domain_get (), path, len);
	}
#else
	g_warning ("ves_icall_System_Environment_GetWindowsFolderPath should only be called on Windows!");
#endif
	return mono_string_new (mono_domain_get (), "");
}

ICALL_EXPORT MonoArray *
ves_icall_System_Environment_GetLogicalDrives (void)
{
        gunichar2 buf [256], *ptr, *dname;
	gunichar2 *u16;
	guint initial_size = 127, size = 128;
	gint ndrives;
	MonoArray *result;
	MonoString *drivestr;
	MonoDomain *domain = mono_domain_get ();
	gint len;

	buf [0] = '\0';
	ptr = buf;

	while (size > initial_size) {
		size = (guint) GetLogicalDriveStrings (initial_size, ptr);
		if (size > initial_size) {
			if (ptr != buf)
				g_free (ptr);
			ptr = g_malloc0 ((size + 1) * sizeof (gunichar2));
			initial_size = size;
			size++;
		}
	}

	/* Count strings */
	dname = ptr;
	ndrives = 0;
	do {
		while (*dname++);
		ndrives++;
	} while (*dname);

	dname = ptr;
	result = mono_array_new (domain, mono_defaults.string_class, ndrives);
	ndrives = 0;
	do {
		len = 0;
		u16 = dname;
		while (*u16) { u16++; len ++; }
		drivestr = mono_string_new_utf16 (domain, dname, len);
		mono_array_setref (result, ndrives++, drivestr);
		while (*dname++);
	} while (*dname);

	if (ptr != buf)
		g_free (ptr);

	return result;
}

ICALL_EXPORT MonoString *
ves_icall_System_IO_DriveInfo_GetDriveFormat (MonoString *path)
{
	gunichar2 volume_name [MAX_PATH + 1];
	
	if (GetVolumeInformation (mono_string_chars (path), NULL, 0, NULL, NULL, NULL, volume_name, MAX_PATH + 1) == FALSE)
		return NULL;
	return mono_string_from_utf16 (volume_name);
}

ICALL_EXPORT MonoString *
ves_icall_System_Environment_InternalGetHome (void)
{
	return mono_string_new (mono_domain_get (), g_get_home_dir ());
}

static const char *encodings [] = {
	(char *) 1,
		"ascii", "us_ascii", "us", "ansi_x3.4_1968",
		"ansi_x3.4_1986", "cp367", "csascii", "ibm367",
		"iso_ir_6", "iso646_us", "iso_646.irv:1991",
	(char *) 2,
		"utf_7", "csunicode11utf7", "unicode_1_1_utf_7",
		"unicode_2_0_utf_7", "x_unicode_1_1_utf_7",
		"x_unicode_2_0_utf_7",
	(char *) 3,
		"utf_8", "unicode_1_1_utf_8", "unicode_2_0_utf_8",
		"x_unicode_1_1_utf_8", "x_unicode_2_0_utf_8",
	(char *) 4,
		"utf_16", "UTF_16LE", "ucs_2", "unicode",
		"iso_10646_ucs2",
	(char *) 5,
		"unicodefffe", "utf_16be",
	(char *) 6,
		"iso_8859_1",
	(char *) 0
};

/*
 * Returns the internal codepage, if the value of "int_code_page" is
 * 1 at entry, and we can not compute a suitable code page number,
 * returns the code page as a string
 */
ICALL_EXPORT MonoString*
ves_icall_System_Text_EncodingHelper_InternalCodePage (gint32 *int_code_page) 
{
	const char *cset;
	const char *p;
	char *c;
	char *codepage = NULL;
	int code;
	int want_name = *int_code_page;
	int i;
	
	*int_code_page = -1;

	g_get_charset (&cset);
	c = codepage = strdup (cset);
	for (c = codepage; *c; c++){
		if (isascii (*c) && isalpha (*c))
			*c = tolower (*c);
		if (*c == '-')
			*c = '_';
	}
	/* g_print ("charset: %s\n", cset); */
	
	/* handle some common aliases */
	p = encodings [0];
	code = 0;
	for (i = 0; p != 0; ){
		if ((gssize) p < 7){
			code = (gssize) p;
			p = encodings [++i];
			continue;
		}
		if (strcmp (p, codepage) == 0){
			*int_code_page = code;
			break;
		}
		p = encodings [++i];
	}
	
	if (strstr (codepage, "utf_8") != NULL)
		*int_code_page |= 0x10000000;
	free (codepage);
	
	if (want_name && *int_code_page == -1)
		return mono_string_new (mono_domain_get (), cset);
	else
		return NULL;
}

ICALL_EXPORT MonoBoolean
ves_icall_System_Environment_get_HasShutdownStarted (void)
{
	if (mono_runtime_is_shutting_down ())
		return TRUE;

	if (mono_domain_is_unloading (mono_domain_get ()))
		return TRUE;

	return FALSE;
}

ICALL_EXPORT void
ves_icall_System_Environment_BroadcastSettingChange (void)
{
#ifdef HOST_WIN32
	SendMessageTimeout (HWND_BROADCAST, WM_SETTINGCHANGE, (WPARAM)NULL, (LPARAM)L"Environment", SMTO_ABORTIFHUNG, 2000, 0);
#endif
}

ICALL_EXPORT void
ves_icall_MonoMethodMessage_InitMessage (MonoMethodMessage *this, 
					 MonoReflectionMethod *method,
					 MonoArray *out_args)
{
	mono_message_init (mono_object_domain (this), this, method, out_args);
}

#ifndef DISABLE_REMOTING
ICALL_EXPORT MonoBoolean
ves_icall_IsTransparentProxy (MonoObject *proxy)
{
	if (!proxy)
		return 0;

	if (proxy->vtable->klass == mono_defaults.transparent_proxy_class)
		return 1;

	return 0;
}

ICALL_EXPORT MonoReflectionMethod *
ves_icall_Remoting_RemotingServices_GetVirtualMethod (
	MonoReflectionType *rtype, MonoReflectionMethod *rmethod)
{
	MonoClass *klass;
	MonoMethod *method;
	MonoMethod **vtable;
	MonoMethod *res = NULL;

	MONO_CHECK_ARG_NULL (rtype);
	MONO_CHECK_ARG_NULL (rmethod);

	method = rmethod->method;
	klass = mono_class_from_mono_type (rtype->type);
	mono_class_init_or_throw (klass);

	if (MONO_CLASS_IS_INTERFACE (klass))
		return NULL;

	if (method->flags & METHOD_ATTRIBUTE_STATIC)
		return NULL;

	if ((method->flags & METHOD_ATTRIBUTE_FINAL) || !(method->flags & METHOD_ATTRIBUTE_VIRTUAL)) {
		if (klass == method->klass || mono_class_is_subclass_of (klass, method->klass, FALSE))
			return rmethod;
		else
			return NULL;
	}

	mono_class_setup_vtable (klass);
	vtable = klass->vtable;

	if (method->klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
		gboolean variance_used = FALSE;
		/*MS fails with variant interfaces but it's the right thing to do anyway.*/
		int offs = mono_class_interface_offset_with_variance (klass, method->klass, &variance_used);
		if (offs >= 0)
			res = vtable [offs + method->slot];
	} else {
		if (!(klass == method->klass || mono_class_is_subclass_of (klass, method->klass, FALSE)))
			return NULL;

		if (method->slot != -1)
			res = vtable [method->slot];
	}

	if (!res)
		return NULL;

	return mono_method_get_object (mono_domain_get (), res, NULL);
}

ICALL_EXPORT void
ves_icall_System_Runtime_Activation_ActivationServices_EnableProxyActivation (MonoReflectionType *type, MonoBoolean enable)
{
	MonoClass *klass;
	MonoVTable* vtable;

	klass = mono_class_from_mono_type (type->type);
	vtable = mono_class_vtable_full (mono_domain_get (), klass, TRUE);

	mono_vtable_set_is_remote (vtable, enable);
}

#else /* DISABLE_REMOTING */

ICALL_EXPORT void
ves_icall_System_Runtime_Activation_ActivationServices_EnableProxyActivation (MonoReflectionType *type, MonoBoolean enable)
{
	g_assert_not_reached ();
}

#endif

ICALL_EXPORT MonoObject *
ves_icall_System_Runtime_Activation_ActivationServices_AllocateUninitializedClassInstance (MonoReflectionType *type)
{
	MonoClass *klass;
	MonoDomain *domain;
	
	domain = mono_object_domain (type);
	klass = mono_class_from_mono_type (type->type);
	mono_class_init_or_throw (klass);

	if (MONO_CLASS_IS_INTERFACE (klass) || (klass->flags & TYPE_ATTRIBUTE_ABSTRACT))
		mono_raise_exception (mono_get_exception_argument ("type", "Type cannot be instantiated"));

	if (klass->rank >= 1) {
		g_assert (klass->rank == 1);
		return (MonoObject *) mono_array_new (domain, klass->element_class, 0);
	} else {
		/* Bypass remoting object creation check */
		return mono_object_new_alloc_specific (mono_class_vtable_full (domain, klass, TRUE));
	}
}

ICALL_EXPORT MonoString *
ves_icall_System_IO_get_temp_path (void)
{
	return mono_string_new (mono_domain_get (), g_get_tmp_dir ());
}

#ifndef PLATFORM_NO_DRIVEINFO
ICALL_EXPORT MonoBoolean
ves_icall_System_IO_DriveInfo_GetDiskFreeSpace (MonoString *path_name, guint64 *free_bytes_avail,
						guint64 *total_number_of_bytes, guint64 *total_number_of_free_bytes,
						gint32 *error)
{
	gboolean result;
	ULARGE_INTEGER wapi_free_bytes_avail;
	ULARGE_INTEGER wapi_total_number_of_bytes;
	ULARGE_INTEGER wapi_total_number_of_free_bytes;

	*error = ERROR_SUCCESS;
	result = GetDiskFreeSpaceEx (mono_string_chars (path_name), &wapi_free_bytes_avail, &wapi_total_number_of_bytes,
				     &wapi_total_number_of_free_bytes);

	if (result) {
		*free_bytes_avail = wapi_free_bytes_avail.QuadPart;
		*total_number_of_bytes = wapi_total_number_of_bytes.QuadPart;
		*total_number_of_free_bytes = wapi_total_number_of_free_bytes.QuadPart;
	} else {
		*free_bytes_avail = 0;
		*total_number_of_bytes = 0;
		*total_number_of_free_bytes = 0;
		*error = GetLastError ();
	}

	return result;
}

ICALL_EXPORT guint32
ves_icall_System_IO_DriveInfo_GetDriveType (MonoString *root_path_name)
{
	return GetDriveType (mono_string_chars (root_path_name));
}
#endif

ICALL_EXPORT gpointer
ves_icall_RuntimeMethod_GetFunctionPointer (MonoMethod *method)
{
	return mono_compile_method (method);
}

ICALL_EXPORT MonoString *
ves_icall_System_Configuration_DefaultConfig_get_machine_config_path (void)
{
	MonoString *mcpath;
	gchar *path;

	path = g_build_path (G_DIR_SEPARATOR_S, mono_get_config_dir (), "mono", mono_get_runtime_info ()->framework_version, "machine.config", NULL);

#if defined (HOST_WIN32)
	/* Avoid mixing '/' and '\\' */
	{
		gint i;
		for (i = strlen (path) - 1; i >= 0; i--)
			if (path [i] == '/')
				path [i] = '\\';
	}
#endif
	mcpath = mono_string_new (mono_domain_get (), path);
	g_free (path);

	return mcpath;
}

static MonoString *
get_bundled_app_config (void)
{
	const gchar *app_config;
	MonoDomain *domain;
	MonoString *file;
	gchar *config_file_name, *config_file_path;
	gsize len;
	gchar *module;

	domain = mono_domain_get ();
	file = domain->setup->configuration_file;
	if (!file)
		return NULL;

	// Retrieve config file and remove the extension
	config_file_name = mono_string_to_utf8 (file);
	config_file_path = mono_portability_find_file (config_file_name, TRUE);
	if (!config_file_path)
		config_file_path = config_file_name;
	len = strlen (config_file_path) - strlen (".config");
	module = g_malloc0 (len + 1);
	memcpy (module, config_file_path, len);
	// Get the config file from the module name
	app_config = mono_config_string_for_assembly_file (module);
	// Clean-up
	g_free (module);
	if (config_file_name != config_file_path)
		g_free (config_file_name);
	g_free (config_file_path);

	if (!app_config)
		return NULL;

	return mono_string_new (mono_domain_get (), app_config);
}

static MonoString *
get_bundled_machine_config (void)
{
	const gchar *machine_config;

	machine_config = mono_get_machine_config ();

	if (!machine_config)
		return NULL;

	return mono_string_new (mono_domain_get (), machine_config);
}

ICALL_EXPORT MonoString *
ves_icall_System_Web_Util_ICalls_get_machine_install_dir (void)
{
	MonoString *ipath;
	gchar *path;

	path = g_path_get_dirname (mono_get_config_dir ());

#if defined (HOST_WIN32)
	/* Avoid mixing '/' and '\\' */
	{
		gint i;
		for (i = strlen (path) - 1; i >= 0; i--)
			if (path [i] == '/')
				path [i] = '\\';
	}
#endif
	ipath = mono_string_new (mono_domain_get (), path);
	g_free (path);

	return ipath;
}

ICALL_EXPORT gboolean
ves_icall_get_resources_ptr (MonoReflectionAssembly *assembly, gpointer *result, gint32 *size)
{
	MonoPEResourceDataEntry *entry;
	MonoImage *image;

	if (!assembly || !result || !size)
		return FALSE;

	*result = NULL;
	*size = 0;
	image = assembly->assembly->image;
	entry = mono_image_lookup_resource (image, MONO_PE_RESOURCE_ID_ASPNET_STRING, 0, NULL);
	if (!entry)
		return FALSE;

	*result = mono_image_rva_map (image, entry->rde_data_offset);
	if (!(*result)) {
		g_free (entry);
		return FALSE;
	}
	*size = entry->rde_size;
	g_free (entry);
	return TRUE;
}

ICALL_EXPORT MonoBoolean
ves_icall_System_Diagnostics_Debugger_IsAttached_internal (void)
{
	return mono_is_debugger_attached ();
}

ICALL_EXPORT MonoBoolean
ves_icall_System_Diagnostics_Debugger_IsLogging (void)
{
	if (mono_get_runtime_callbacks ()->debug_log_is_enabled)
		return mono_get_runtime_callbacks ()->debug_log_is_enabled ();
	else
		return FALSE;
}

ICALL_EXPORT void
ves_icall_System_Diagnostics_Debugger_Log (int level, MonoString *category, MonoString *message)
{
	if (mono_get_runtime_callbacks ()->debug_log)
		mono_get_runtime_callbacks ()->debug_log (level, category, message);
}

ICALL_EXPORT void
ves_icall_System_Diagnostics_DefaultTraceListener_WriteWindowsDebugString (MonoString *message)
{
#if defined (HOST_WIN32)
	OutputDebugString (mono_string_chars (message));
#else
	g_warning ("WriteWindowsDebugString called and HOST_WIN32 not defined!\n");
#endif
}

/* Only used for value types */
ICALL_EXPORT MonoObject *
ves_icall_System_Activator_CreateInstanceInternal (MonoReflectionType *type)
{
	MonoClass *klass;
	MonoDomain *domain;
	
	domain = mono_object_domain (type);
	klass = mono_class_from_mono_type (type->type);
	mono_class_init_or_throw (klass);

	if (mono_class_is_nullable (klass))
		/* No arguments -> null */
		return NULL;

	return mono_object_new (domain, klass);
}

ICALL_EXPORT MonoReflectionMethod *
ves_icall_MonoMethod_get_base_method (MonoReflectionMethod *m, gboolean definition)
{
	MonoClass *klass, *parent;
	MonoMethod *method = m->method;
	MonoMethod *result = NULL;
	int slot;

	if (method->klass == NULL)
		return m;

	if (!(method->flags & METHOD_ATTRIBUTE_VIRTUAL) ||
	    MONO_CLASS_IS_INTERFACE (method->klass) ||
	    method->flags & METHOD_ATTRIBUTE_NEW_SLOT)
		return m;

	slot = mono_method_get_vtable_slot (method);
	if (slot == -1)
		return m;

	klass = method->klass;
	if (klass->generic_class)
		klass = klass->generic_class->container_class;

	if (definition) {
		/* At the end of the loop, klass points to the eldest class that has this virtual function slot. */
		for (parent = klass->parent; parent != NULL; parent = parent->parent) {
			mono_class_setup_vtable (parent);
			if (parent->vtable_size <= slot)
				break;
			klass = parent;
		}
	} else {
		klass = klass->parent;
		if (!klass)
			return m;
	}

	if (klass == method->klass)
		return m;

	/*This is possible if definition == FALSE.
	 * Do it here to be really sure we don't read invalid memory.
	 */
	if (slot >= klass->vtable_size)
		return m;

	mono_class_setup_vtable (klass);

	result = klass->vtable [slot];
	if (result == NULL) {
		/* It is an abstract method */
		gpointer iter = NULL;
		while ((result = mono_class_get_methods (klass, &iter)))
			if (result->slot == slot)
				break;
	}

	if (result == NULL)
		return m;

	return mono_method_get_object (mono_domain_get (), result, NULL);
}

ICALL_EXPORT MonoString*
ves_icall_MonoMethod_get_name (MonoReflectionMethod *m)
{
	MonoMethod *method = m->method;

	MONO_OBJECT_SETREF (m, name, mono_string_new (mono_object_domain (m), method->name));
	return m->name;
}

ICALL_EXPORT void
mono_ArgIterator_Setup (MonoArgIterator *iter, char* argsp, char* start)
{
	iter->sig = *(MonoMethodSignature**)argsp;
	
	g_assert (iter->sig->sentinelpos <= iter->sig->param_count);
	g_assert (iter->sig->call_convention == MONO_CALL_VARARG);

	iter->next_arg = 0;
	/* FIXME: it's not documented what start is exactly... */
	if (start) {
		iter->args = start;
	} else {
		iter->args = argsp + sizeof (gpointer);
	}
	iter->num_args = iter->sig->param_count - iter->sig->sentinelpos;

	/* g_print ("sig %p, param_count: %d, sent: %d\n", iter->sig, iter->sig->param_count, iter->sig->sentinelpos); */
}

ICALL_EXPORT MonoTypedRef
mono_ArgIterator_IntGetNextArg (MonoArgIterator *iter)
{
	guint32 i, arg_size;
	gint32 align;
	MonoTypedRef res;

	i = iter->sig->sentinelpos + iter->next_arg;

	g_assert (i < iter->sig->param_count);

	res.type = iter->sig->params [i];
	res.klass = mono_class_from_mono_type (res.type);
	arg_size = mono_type_stack_size (res.type, &align);
#if defined(__arm__) || defined(__mips__)
	iter->args = (guint8*)(((gsize)iter->args + (align) - 1) & ~(align - 1));
#endif
	res.value = iter->args;
#if defined(__native_client__) && SIZEOF_REGISTER == 8
	/* Values are stored as 8 byte register sized objects, but 'value'
	 * is dereferenced as a pointer in other routines.
	 */
	res.value = (char*)res.value + 4;
#endif
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	if (arg_size <= sizeof (gpointer)) {
		int dummy;
		int padding = arg_size - mono_type_size (res.type, &dummy);
		res.value = (guint8*)res.value + padding;
	}
#endif
	iter->args = (char*)iter->args + arg_size;
	iter->next_arg++;

	/* g_print ("returning arg %d, type 0x%02x of size %d at %p\n", i, res.type->type, arg_size, res.value); */

	return res;
}

ICALL_EXPORT MonoTypedRef
mono_ArgIterator_IntGetNextArgT (MonoArgIterator *iter, MonoType *type)
{
	guint32 i, arg_size;
	gint32 align;
	MonoTypedRef res;

	i = iter->sig->sentinelpos + iter->next_arg;

	g_assert (i < iter->sig->param_count);

	while (i < iter->sig->param_count) {
		if (!mono_metadata_type_equal (type, iter->sig->params [i]))
			continue;
		res.type = iter->sig->params [i];
		res.klass = mono_class_from_mono_type (res.type);
		/* FIXME: endianess issue... */
		arg_size = mono_type_stack_size (res.type, &align);
#if defined(__arm__) || defined(__mips__)
		iter->args = (guint8*)(((gsize)iter->args + (align) - 1) & ~(align - 1));
#endif
		res.value = iter->args;
		iter->args = (char*)iter->args + arg_size;
		iter->next_arg++;
		/* g_print ("returning arg %d, type 0x%02x of size %d at %p\n", i, res.type->type, arg_size, res.value); */
		return res;
	}
	/* g_print ("arg type 0x%02x not found\n", res.type->type); */

	res.type = NULL;
	res.value = NULL;
	res.klass = NULL;
	return res;
}

ICALL_EXPORT MonoType*
mono_ArgIterator_IntGetNextArgType (MonoArgIterator *iter)
{
	gint i;
	
	i = iter->sig->sentinelpos + iter->next_arg;

	g_assert (i < iter->sig->param_count);

	return iter->sig->params [i];
}

ICALL_EXPORT MonoObject*
mono_TypedReference_ToObject (MonoTypedRef tref)
{
	if (MONO_TYPE_IS_REFERENCE (tref.type)) {
		MonoObject** objp = tref.value;
		return *objp;
	}

	return mono_value_box (mono_domain_get (), tref.klass, tref.value);
}

ICALL_EXPORT MonoObject*
mono_TypedReference_ToObjectInternal (MonoType *type, gpointer value, MonoClass *klass)
{
	if (MONO_TYPE_IS_REFERENCE (type)) {
		MonoObject** objp = value;
		return *objp;
	}

	return mono_value_box (mono_domain_get (), klass, value);
}

static void
prelink_method (MonoMethod *method)
{
	const char *exc_class, *exc_arg;
	if (!(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		return;
	mono_lookup_pinvoke_call (method, &exc_class, &exc_arg);
	if (exc_class) {
		mono_raise_exception( 
			mono_exception_from_name_msg (mono_defaults.corlib, "System", exc_class, exc_arg ) );
	}
	/* create the wrapper, too? */
}

ICALL_EXPORT void
ves_icall_System_Runtime_InteropServices_Marshal_Prelink (MonoReflectionMethod *method)
{
	prelink_method (method->method);
}

ICALL_EXPORT void
ves_icall_System_Runtime_InteropServices_Marshal_PrelinkAll (MonoReflectionType *type)
{
	MonoClass *klass = mono_class_from_mono_type (type->type);
	MonoMethod* m;
	gpointer iter = NULL;

	mono_class_init_or_throw (klass);

	while ((m = mono_class_get_methods (klass, &iter)))
		prelink_method (m);
}

/* These parameters are "readonly" in corlib/System/NumberFormatter.cs */
ICALL_EXPORT void
ves_icall_System_NumberFormatter_GetFormatterTables (guint64 const **mantissas,
					    gint32 const **exponents,
					    gunichar2 const **digitLowerTable,
					    gunichar2 const **digitUpperTable,
					    gint64 const **tenPowersList,
					    gint32 const **decHexDigits)
{
	*mantissas = Formatter_MantissaBitsTable;
	*exponents = Formatter_TensExponentTable;
	*digitLowerTable = Formatter_DigitLowerTable;
	*digitUpperTable = Formatter_DigitUpperTable;
	*tenPowersList = Formatter_TenPowersList;
	*decHexDigits = Formatter_DecHexDigits;
}

/* These parameters are "readonly" in corlib/System/Globalization/TextInfo.cs */
ICALL_EXPORT void
ves_icall_System_Globalization_TextInfo_GetDataTablePointersLite (
					    guint16 const **to_lower_data_low,
					    guint16 const **to_lower_data_high,
					    guint16 const **to_upper_data_low,
					    guint16 const **to_upper_data_high)
{
	*to_lower_data_low = ToLowerDataLow;
	*to_lower_data_high = ToLowerDataHigh;
	*to_upper_data_low = ToUpperDataLow;
	*to_upper_data_high = ToUpperDataHigh;
}

/*
 * We return NULL for no modifiers so the corlib code can return Type.EmptyTypes
 * and avoid useless allocations.
 * 
 * MAY THROW
 */
static MonoArray*
type_array_from_modifiers (MonoImage *image, MonoType *type, int optional)
{
	MonoArray *res;
	int i, count = 0;
	for (i = 0; i < type->num_mods; ++i) {
		if ((optional && !type->modifiers [i].required) || (!optional && type->modifiers [i].required))
			count++;
	}
	if (!count)
		return NULL;
	res = mono_array_new (mono_domain_get (), mono_defaults.systemtype_class, count);
	count = 0;
	for (i = 0; i < type->num_mods; ++i) {
		if ((optional && !type->modifiers [i].required) || (!optional && type->modifiers [i].required)) {
			MonoError error;
			MonoClass *klass = mono_class_get_checked (image, type->modifiers [i].token, &error);
			mono_error_raise_exception (&error); /* this is safe, no cleanup needed on callers */ 
			mono_array_setref (res, count, mono_type_get_object (mono_domain_get (), &klass->byval_arg));
			count++;
		}
	}
	return res;
}

ICALL_EXPORT MonoArray*
param_info_get_type_modifiers (MonoReflectionParameter *param, MonoBoolean optional)
{
	MonoType *type = param->ClassImpl->type;
	MonoClass *member_class = mono_object_class (param->MemberImpl);
	MonoMethod *method = NULL;
	MonoImage *image;
	int pos;
	MonoMethodSignature *sig;

	if (mono_class_is_reflection_method_or_constructor (member_class)) {
		MonoReflectionMethod *rmethod = (MonoReflectionMethod*)param->MemberImpl;
		method = rmethod->method;
	} else if (member_class->image == mono_defaults.corlib && !strcmp ("MonoProperty", member_class->name)) {
		MonoReflectionProperty *prop = (MonoReflectionProperty *)param->MemberImpl;
		if (!(method = prop->property->get))
			method = prop->property->set;
		g_assert (method);	
	} else {
		char *type_name = mono_type_get_full_name (member_class);
		char *msg = g_strdup_printf ("Custom modifiers on a ParamInfo with member %s are not supported", type_name);
		MonoException *ex = mono_get_exception_not_supported  (msg);
		g_free (type_name);
		g_free (msg);
		mono_raise_exception (ex);
	}

	image = method->klass->image;
	pos = param->PositionImpl;
	sig = mono_method_signature (method);
	if (pos == -1)
		type = sig->ret;
	else
		type = sig->params [pos];

	return type_array_from_modifiers (image, type, optional);
}

static MonoType*
get_property_type (MonoProperty *prop)
{
	MonoMethodSignature *sig;
	if (prop->get) {
		sig = mono_method_signature (prop->get);
		return sig->ret;
	} else if (prop->set) {
		sig = mono_method_signature (prop->set);
		return sig->params [sig->param_count - 1];
	}
	return NULL;
}

ICALL_EXPORT MonoArray*
property_info_get_type_modifiers (MonoReflectionProperty *property, MonoBoolean optional)
{
	MonoType *type = get_property_type (property->property);
	MonoImage *image = property->klass->image;

	if (!type)
		return NULL;
	return type_array_from_modifiers (image, type, optional);
}

/*
 *Construct a MonoType suited to be used to decode a constant blob object.
 *
 * @type is the target type which will be constructed
 * @blob_type is the blob type, for example, that comes from the constant table
 * @real_type is the expected constructed type.
 */
static void
mono_type_from_blob_type (MonoType *type, MonoTypeEnum blob_type, MonoType *real_type)
{
	type->type = blob_type;
	type->data.klass = NULL;
	if (blob_type == MONO_TYPE_CLASS)
		type->data.klass = mono_defaults.object_class;
	else if (real_type->type == MONO_TYPE_VALUETYPE && real_type->data.klass->enumtype) {
		/* For enums, we need to use the base type */
		type->type = MONO_TYPE_VALUETYPE;
		type->data.klass = mono_class_from_mono_type (real_type);
	} else
		type->data.klass = mono_class_from_mono_type (real_type);
}

ICALL_EXPORT MonoObject*
property_info_get_default_value (MonoReflectionProperty *property)
{
	MonoType blob_type;
	MonoProperty *prop = property->property;
	MonoType *type = get_property_type (prop);
	MonoDomain *domain = mono_object_domain (property); 
	MonoTypeEnum def_type;
	const char *def_value;
	MonoObject *o;

	mono_class_init (prop->parent);

	if (!(prop->attrs & PROPERTY_ATTRIBUTE_HAS_DEFAULT))
		mono_raise_exception (mono_get_exception_invalid_operation (NULL));

	def_value = mono_class_get_property_default_value (prop, &def_type);

	mono_type_from_blob_type (&blob_type, def_type, type);
	o = mono_get_object_from_blob (domain, &blob_type, def_value);

	return o;
}

ICALL_EXPORT MonoBoolean
custom_attrs_defined_internal (MonoObject *obj, MonoReflectionType *attr_type)
{
	MonoClass *attr_class = mono_class_from_mono_type (attr_type->type);
	MonoCustomAttrInfo *cinfo;
	gboolean found;

	mono_class_init_or_throw (attr_class);

	cinfo = mono_reflection_get_custom_attrs_info (obj);
	if (!cinfo)
		return FALSE;
	found = mono_custom_attrs_has_attr (cinfo, attr_class);
	if (!cinfo->cached)
		mono_custom_attrs_free (cinfo);
	return found;
}

ICALL_EXPORT MonoArray*
custom_attrs_get_by_type (MonoObject *obj, MonoReflectionType *attr_type)
{
	MonoClass *attr_class = attr_type ? mono_class_from_mono_type (attr_type->type) : NULL;
	MonoArray *res;
	MonoError error;

	if (attr_class)
		mono_class_init_or_throw (attr_class);

	res = mono_reflection_get_custom_attrs_by_type (obj, attr_class, &error);
	mono_error_raise_exception (&error);

	if (mono_loader_get_last_error ()) {
		mono_raise_exception (mono_loader_error_prepare_exception (mono_loader_get_last_error ()));
		g_assert_not_reached ();
		/* Not reached */
		return NULL;
	} else {
		return res;
	}
}

ICALL_EXPORT MonoString*
ves_icall_Mono_Runtime_GetDisplayName (void)
{
	char *info;
	MonoString *display_name;

	info = mono_get_runtime_callbacks ()->get_runtime_build_info ();
	display_name = mono_string_new (mono_domain_get (), info);
	g_free (info);
	return display_name;
}

ICALL_EXPORT MonoString*
ves_icall_System_ComponentModel_Win32Exception_W32ErrorMessage (guint32 code)
{
	MonoString *message;
	guint32 ret;
	gunichar2 buf[256];
	
	ret = FormatMessage (FORMAT_MESSAGE_FROM_SYSTEM |
			     FORMAT_MESSAGE_IGNORE_INSERTS, NULL, code, 0,
			     buf, 255, NULL);
	if (ret == 0) {
		message = mono_string_new (mono_domain_get (), "Error looking up error string");
	} else {
		message = mono_string_new_utf16 (mono_domain_get (), buf, ret);
	}
	
	return message;
}

#ifndef DISABLE_ICALL_TABLES

#define ICALL_TYPE(id,name,first)
#define ICALL(id,name,func) Icall_ ## id,

enum {
#include "metadata/icall-def.h"
	Icall_last
};

#undef ICALL_TYPE
#undef ICALL
#define ICALL_TYPE(id,name,first) Icall_type_ ## id,
#define ICALL(id,name,func)
enum {
#include "metadata/icall-def.h"
	Icall_type_num
};

#undef ICALL_TYPE
#undef ICALL
#define ICALL_TYPE(id,name,firstic) {(Icall_ ## firstic)},
#define ICALL(id,name,func)
typedef struct {
	guint16 first_icall;
} IcallTypeDesc;

static const IcallTypeDesc
icall_type_descs [] = {
#include "metadata/icall-def.h"
	{Icall_last}
};

#define icall_desc_num_icalls(desc) ((desc) [1].first_icall - (desc) [0].first_icall)

#undef ICALL_TYPE
#define ICALL_TYPE(id,name,first)
#undef ICALL

#ifdef HAVE_ARRAY_ELEM_INIT
#define MSGSTRFIELD(line) MSGSTRFIELD1(line)
#define MSGSTRFIELD1(line) str##line

static const struct msgstrtn_t {
#define ICALL(id,name,func)
#undef ICALL_TYPE
#define ICALL_TYPE(id,name,first) char MSGSTRFIELD(__LINE__) [sizeof (name)];
#include "metadata/icall-def.h"
#undef ICALL_TYPE
} icall_type_names_str = {
#define ICALL_TYPE(id,name,first) (name),
#include "metadata/icall-def.h"
#undef ICALL_TYPE
};
static const guint16 icall_type_names_idx [] = {
#define ICALL_TYPE(id,name,first) [Icall_type_ ## id] = offsetof (struct msgstrtn_t, MSGSTRFIELD(__LINE__)),
#include "metadata/icall-def.h"
#undef ICALL_TYPE
};
#define icall_type_name_get(id) ((const char*)&icall_type_names_str + icall_type_names_idx [(id)])

static const struct msgstr_t {
#undef ICALL
#define ICALL_TYPE(id,name,first)
#define ICALL(id,name,func) char MSGSTRFIELD(__LINE__) [sizeof (name)];
#include "metadata/icall-def.h"
#undef ICALL
} icall_names_str = {
#define ICALL(id,name,func) (name),
#include "metadata/icall-def.h"
#undef ICALL
};
static const guint16 icall_names_idx [] = {
#define ICALL(id,name,func) [Icall_ ## id] = offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#include "metadata/icall-def.h"
#undef ICALL
};
#define icall_name_get(id) ((const char*)&icall_names_str + icall_names_idx [(id)])

#else

#undef ICALL_TYPE
#undef ICALL
#define ICALL_TYPE(id,name,first) name,
#define ICALL(id,name,func)
static const char* const
icall_type_names [] = {
#include "metadata/icall-def.h"
	NULL
};

#define icall_type_name_get(id) (icall_type_names [(id)])

#undef ICALL_TYPE
#undef ICALL
#define ICALL_TYPE(id,name,first)
#define ICALL(id,name,func) name,
static const char* const
icall_names [] = {
#include "metadata/icall-def.h"
	NULL
};
#define icall_name_get(id) icall_names [(id)]

#endif /* !HAVE_ARRAY_ELEM_INIT */

#undef ICALL_TYPE
#undef ICALL
#define ICALL_TYPE(id,name,first)
#define ICALL(id,name,func) func,
static const gconstpointer
icall_functions [] = {
#include "metadata/icall-def.h"
	NULL
};

#ifdef ENABLE_ICALL_SYMBOL_MAP
#undef ICALL_TYPE
#undef ICALL
#define ICALL_TYPE(id,name,first)
#define ICALL(id,name,func) #func,
static const gconstpointer
icall_symbols [] = {
#include "metadata/icall-def.h"
	NULL
};
#endif

#endif /* DISABLE_ICALL_TABLES */

static mono_mutex_t icall_mutex;
static GHashTable *icall_hash = NULL;
static GHashTable *jit_icall_hash_name = NULL;
static GHashTable *jit_icall_hash_addr = NULL;

void
mono_icall_init (void)
{
#ifndef DISABLE_ICALL_TABLES
	int i = 0;

	/* check that tables are sorted: disable in release */
	if (TRUE) {
		int j;
		const char *prev_class = NULL;
		const char *prev_method;
		
		for (i = 0; i < Icall_type_num; ++i) {
			const IcallTypeDesc *desc;
			int num_icalls;
			prev_method = NULL;
			if (prev_class && strcmp (prev_class, icall_type_name_get (i)) >= 0)
				g_print ("class %s should come before class %s\n", icall_type_name_get (i), prev_class);
			prev_class = icall_type_name_get (i);
			desc = &icall_type_descs [i];
			num_icalls = icall_desc_num_icalls (desc);
			/*g_print ("class %s has %d icalls starting at %d\n", prev_class, num_icalls, desc->first_icall);*/
			for (j = 0; j < num_icalls; ++j) {
				const char *methodn = icall_name_get (desc->first_icall + j);
				if (prev_method && strcmp (prev_method, methodn) >= 0)
					g_print ("method %s should come before method %s\n", methodn, prev_method);
				prev_method = methodn;
			}
		}
	}
#endif

	icall_hash = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, NULL);
	mono_mutex_init (&icall_mutex);
}

static void
mono_icall_lock (void)
{
	mono_locks_mutex_acquire (&icall_mutex, IcallLock);
}

static void
mono_icall_unlock (void)
{
	mono_locks_mutex_release (&icall_mutex, IcallLock);
}

void
mono_icall_cleanup (void)
{
	g_hash_table_destroy (icall_hash);
	g_hash_table_destroy (jit_icall_hash_name);
	g_hash_table_destroy (jit_icall_hash_addr);
	mono_mutex_destroy (&icall_mutex);
}

void
mono_add_internal_call (const char *name, gconstpointer method)
{
	mono_icall_lock ();

	g_hash_table_insert (icall_hash, g_strdup (name), (gpointer) method);

	mono_icall_unlock ();
}

#ifndef DISABLE_ICALL_TABLES

#ifdef HAVE_ARRAY_ELEM_INIT
static int
compare_method_imap (const void *key, const void *elem)
{
	const char* method_name = (const char*)&icall_names_str + (*(guint16*)elem);
	return strcmp (key, method_name);
}

static gpointer
find_method_icall (const IcallTypeDesc *imap, const char *name)
{
	const guint16 *nameslot = mono_binary_search (name, icall_names_idx + imap->first_icall, icall_desc_num_icalls (imap), sizeof (icall_names_idx [0]), compare_method_imap);
	if (!nameslot)
		return NULL;
	return (gpointer)icall_functions [(nameslot - &icall_names_idx [0])];
}

static int
compare_class_imap (const void *key, const void *elem)
{
	const char* class_name = (const char*)&icall_type_names_str + (*(guint16*)elem);
	return strcmp (key, class_name);
}

static const IcallTypeDesc*
find_class_icalls (const char *name)
{
	const guint16 *nameslot = mono_binary_search (name, icall_type_names_idx, Icall_type_num, sizeof (icall_type_names_idx [0]), compare_class_imap);
	if (!nameslot)
		return NULL;
	return &icall_type_descs [nameslot - &icall_type_names_idx [0]];
}

#else /* HAVE_ARRAY_ELEM_INIT */

static int
compare_method_imap (const void *key, const void *elem)
{
	const char** method_name = (const char**)elem;
	return strcmp (key, *method_name);
}

static gpointer
find_method_icall (const IcallTypeDesc *imap, const char *name)
{
	const char **nameslot = mono_binary_search (name, icall_names + imap->first_icall, icall_desc_num_icalls (imap), sizeof (icall_names [0]), compare_method_imap);
	if (!nameslot)
		return NULL;
	return (gpointer)icall_functions [(nameslot - icall_names)];
}

static int
compare_class_imap (const void *key, const void *elem)
{
	const char** class_name = (const char**)elem;
	return strcmp (key, *class_name);
}

static const IcallTypeDesc*
find_class_icalls (const char *name)
{
	const char **nameslot = mono_binary_search (name, icall_type_names, Icall_type_num, sizeof (icall_type_names [0]), compare_class_imap);
	if (!nameslot)
		return NULL;
	return &icall_type_descs [nameslot - icall_type_names];
}

#endif /* HAVE_ARRAY_ELEM_INIT */

#endif /* DISABLE_ICALL_TABLES */

/* 
 * we should probably export this as an helper (handle nested types).
 * Returns the number of chars written in buf.
 */
static int
concat_class_name (char *buf, int bufsize, MonoClass *klass)
{
	int nspacelen, cnamelen;
	nspacelen = strlen (klass->name_space);
	cnamelen = strlen (klass->name);
	if (nspacelen + cnamelen + 2 > bufsize)
		return 0;
	if (nspacelen) {
		memcpy (buf, klass->name_space, nspacelen);
		buf [nspacelen ++] = '.';
	}
	memcpy (buf + nspacelen, klass->name, cnamelen);
	buf [nspacelen + cnamelen] = 0;
	return nspacelen + cnamelen;
}

#ifdef DISABLE_ICALL_TABLES
static void
no_icall_table (void)
{
	g_assert_not_reached ();
}
#endif

gpointer
mono_lookup_internal_call (MonoMethod *method)
{
	char *sigstart;
	char *tmpsig;
	char mname [2048];
	int typelen = 0, mlen, siglen;
	gpointer res;
#ifndef DISABLE_ICALL_TABLES
	const IcallTypeDesc *imap = NULL;
#endif

	g_assert (method != NULL);

	if (method->is_inflated)
		method = ((MonoMethodInflated *) method)->declaring;

	if (method->klass->nested_in) {
		int pos = concat_class_name (mname, sizeof (mname)-2, method->klass->nested_in);
		if (!pos)
			return NULL;

		mname [pos++] = '/';
		mname [pos] = 0;

		typelen = concat_class_name (mname+pos, sizeof (mname)-pos-1, method->klass);
		if (!typelen)
			return NULL;

		typelen += pos;
	} else {
		typelen = concat_class_name (mname, sizeof (mname), method->klass);
		if (!typelen)
			return NULL;
	}

#ifndef DISABLE_ICALL_TABLES
	imap = find_class_icalls (mname);
#endif

	mname [typelen] = ':';
	mname [typelen + 1] = ':';

	mlen = strlen (method->name);
	memcpy (mname + typelen + 2, method->name, mlen);
	sigstart = mname + typelen + 2 + mlen;
	*sigstart = 0;

	tmpsig = mono_signature_get_desc (mono_method_signature (method), TRUE);
	siglen = strlen (tmpsig);
	if (typelen + mlen + siglen + 6 > sizeof (mname))
		return NULL;
	sigstart [0] = '(';
	memcpy (sigstart + 1, tmpsig, siglen);
	sigstart [siglen + 1] = ')';
	sigstart [siglen + 2] = 0;
	g_free (tmpsig);
	
	mono_icall_lock ();

	res = g_hash_table_lookup (icall_hash, mname);
	if (res) {
		mono_icall_unlock ();;
		return res;
	}
	/* try without signature */
	*sigstart = 0;
	res = g_hash_table_lookup (icall_hash, mname);
	if (res) {
		mono_icall_unlock ();
		return res;
	}

#ifdef DISABLE_ICALL_TABLES
	mono_icall_unlock ();
	/* Fail only when the result is actually used */
	/* mono_marshal_get_native_wrapper () depends on this */
	if (method->klass == mono_defaults.string_class && !strcmp (method->name, ".ctor"))
		return ves_icall_System_String_ctor_RedirectToCreateString;
	else
		return no_icall_table;
#else
	/* it wasn't found in the static call tables */
	if (!imap) {
		mono_icall_unlock ();
		return NULL;
	}
	res = find_method_icall (imap, sigstart - mlen);
	if (res) {
		mono_icall_unlock ();
		return res;
	}
	/* try _with_ signature */
	*sigstart = '(';
	res = find_method_icall (imap, sigstart - mlen);
	if (res) {
		mono_icall_unlock ();
		return res;
	}

	g_warning ("cant resolve internal call to \"%s\" (tested without signature also)", mname);
	g_print ("\nYour mono runtime and class libraries are out of sync.\n");
	g_print ("The out of sync library is: %s\n", method->klass->image->name);
	g_print ("\nWhen you update one from git you need to update, compile and install\nthe other too.\n");
	g_print ("Do not report this as a bug unless you're sure you have updated correctly:\nyou probably have a broken mono install.\n");
	g_print ("If you see other errors or faults after this message they are probably related\n");
	g_print ("and you need to fix your mono install first.\n");

	mono_icall_unlock ();

	return NULL;
#endif
}

#ifdef ENABLE_ICALL_SYMBOL_MAP
static int
func_cmp (gconstpointer key, gconstpointer p)
{
	return (gsize)key - (gsize)*(gsize*)p;
}
#endif

/*
 * mono_lookup_icall_symbol:
 *
 *   Given the icall METHOD, returns its C symbol.
 */
const char*
mono_lookup_icall_symbol (MonoMethod *m)
{
#ifdef DISABLE_ICALL_TABLES
	g_assert_not_reached ();
	return NULL;
#else
#ifdef ENABLE_ICALL_SYMBOL_MAP
	gpointer func;
	int i;
	gpointer slot;
	static gconstpointer *functions_sorted;
	static const char**symbols_sorted;
	static gboolean inited;

	if (!inited) {
		gboolean changed;

		functions_sorted = g_malloc (G_N_ELEMENTS (icall_functions) * sizeof (gpointer));
		memcpy (functions_sorted, icall_functions, G_N_ELEMENTS (icall_functions) * sizeof (gpointer));
		symbols_sorted = g_malloc (G_N_ELEMENTS (icall_functions) * sizeof (gpointer));
		memcpy (symbols_sorted, icall_symbols, G_N_ELEMENTS (icall_functions) * sizeof (gpointer));
		/* Bubble sort the two arrays */
		changed = TRUE;
		while (changed) {
			changed = FALSE;
			for (i = 0; i < G_N_ELEMENTS (icall_functions) - 1; ++i) {
				if (functions_sorted [i] > functions_sorted [i + 1]) {
					gconstpointer tmp;

					tmp = functions_sorted [i];
					functions_sorted [i] = functions_sorted [i + 1];
					functions_sorted [i + 1] = tmp;
					tmp = symbols_sorted [i];
					symbols_sorted [i] = symbols_sorted [i + 1];
					symbols_sorted [i + 1] = tmp;
					changed = TRUE;
				}
			}
		}
	}

	func = mono_lookup_internal_call (m);
	if (!func)
		return NULL;
	slot = mono_binary_search (func, functions_sorted, G_N_ELEMENTS (icall_functions), sizeof (gpointer), func_cmp);
	if (!slot)
		return NULL;
	g_assert (slot);
	return symbols_sorted [(gpointer*)slot - (gpointer*)functions_sorted];
#else
	fprintf (stderr, "icall symbol maps not enabled, pass --enable-icall-symbol-map to configure.\n");
	g_assert_not_reached ();
	return 0;
#endif
#endif
}

static MonoType*
type_from_typename (char *typename)
{
	MonoClass *klass = NULL;	/* assignment to shut GCC warning up */

	if (!strcmp (typename, "int"))
		klass = mono_defaults.int_class;
	else if (!strcmp (typename, "ptr"))
		klass = mono_defaults.int_class;
	else if (!strcmp (typename, "void"))
		klass = mono_defaults.void_class;
	else if (!strcmp (typename, "int32"))
		klass = mono_defaults.int32_class;
	else if (!strcmp (typename, "uint32"))
		klass = mono_defaults.uint32_class;
	else if (!strcmp (typename, "int8"))
		klass = mono_defaults.sbyte_class;
	else if (!strcmp (typename, "uint8"))
		klass = mono_defaults.byte_class;
	else if (!strcmp (typename, "int16"))
		klass = mono_defaults.int16_class;
	else if (!strcmp (typename, "uint16"))
		klass = mono_defaults.uint16_class;
	else if (!strcmp (typename, "long"))
		klass = mono_defaults.int64_class;
	else if (!strcmp (typename, "ulong"))
		klass = mono_defaults.uint64_class;
	else if (!strcmp (typename, "float"))
		klass = mono_defaults.single_class;
	else if (!strcmp (typename, "double"))
		klass = mono_defaults.double_class;
	else if (!strcmp (typename, "object"))
		klass = mono_defaults.object_class;
	else if (!strcmp (typename, "obj"))
		klass = mono_defaults.object_class;
	else if (!strcmp (typename, "string"))
		klass = mono_defaults.string_class;
	else if (!strcmp (typename, "bool"))
		klass = mono_defaults.boolean_class;
	else if (!strcmp (typename, "boolean"))
		klass = mono_defaults.boolean_class;
	else {
		g_error ("%s", typename);
		g_assert_not_reached ();
	}
	return &klass->byval_arg;
}

/**
 * LOCKING: Take the corlib image lock.
 */
MonoMethodSignature*
mono_create_icall_signature (const char *sigstr)
{
	gchar **parts;
	int i, len;
	gchar **tmp;
	MonoMethodSignature *res, *res2;
	MonoImage *corlib = mono_defaults.corlib;

	mono_image_lock (corlib);
	res = g_hash_table_lookup (corlib->helper_signatures, sigstr);
	mono_image_unlock (corlib);

	if (res)
		return res;

	parts = g_strsplit (sigstr, " ", 256);

	tmp = parts;
	len = 0;
	while (*tmp) {
		len ++;
		tmp ++;
	}

	res = mono_metadata_signature_alloc (corlib, len - 1);
	res->pinvoke = 1;

#ifdef HOST_WIN32
	/* 
	 * Under windows, the default pinvoke calling convention is STDCALL but
	 * we need CDECL.
	 */
	res->call_convention = MONO_CALL_C;
#endif

	res->ret = type_from_typename (parts [0]);
	for (i = 1; i < len; ++i) {
		res->params [i - 1] = type_from_typename (parts [i]);
	}

	g_strfreev (parts);

	mono_image_lock (corlib);
	res2 = g_hash_table_lookup (corlib->helper_signatures, sigstr);
	if (res2)
		res = res2; /*Value is allocated in the image pool*/
	else
		g_hash_table_insert (corlib->helper_signatures, (gpointer)sigstr, res);
	mono_image_unlock (corlib);

	return res;
}

MonoJitICallInfo *
mono_find_jit_icall_by_name (const char *name)
{
	MonoJitICallInfo *info;
	g_assert (jit_icall_hash_name);

	mono_icall_lock ();
	info = g_hash_table_lookup (jit_icall_hash_name, name);
	mono_icall_unlock ();
	return info;
}

MonoJitICallInfo *
mono_find_jit_icall_by_addr (gconstpointer addr)
{
	MonoJitICallInfo *info;
	g_assert (jit_icall_hash_addr);

	mono_icall_lock ();
	info = g_hash_table_lookup (jit_icall_hash_addr, (gpointer)addr);
	mono_icall_unlock ();

	return info;
}

/*
 * mono_get_jit_icall_info:
 *
 *   Return the hashtable mapping JIT icall names to MonoJitICallInfo structures. The
 * caller should access it while holding the icall lock.
 */
GHashTable*
mono_get_jit_icall_info (void)
{
	return jit_icall_hash_name;
}

/*
 * mono_lookup_jit_icall_symbol:
 *
 *   Given the jit icall NAME, returns its C symbol if possible, or NULL.
 */
const char*
mono_lookup_jit_icall_symbol (const char *name)
{
	MonoJitICallInfo *info;
	const char *res = NULL;

	mono_icall_lock ();
	info = g_hash_table_lookup (jit_icall_hash_name, name);
	if (info)
		res = info->c_symbol;
	mono_icall_unlock ();
	return res;
}

void
mono_register_jit_icall_wrapper (MonoJitICallInfo *info, gconstpointer wrapper)
{
	mono_icall_lock ();
	g_hash_table_insert (jit_icall_hash_addr, (gpointer)wrapper, info);
	mono_icall_unlock ();
}

MonoJitICallInfo *
mono_register_jit_icall_full (gconstpointer func, const char *name, MonoMethodSignature *sig, gboolean is_save, const char *c_symbol)
{
	MonoJitICallInfo *info;
	
	g_assert (func);
	g_assert (name);

	mono_icall_lock ();

	if (!jit_icall_hash_name) {
		jit_icall_hash_name = g_hash_table_new_full (g_str_hash, g_str_equal, NULL, g_free);
		jit_icall_hash_addr = g_hash_table_new (NULL, NULL);
	}

	if (g_hash_table_lookup (jit_icall_hash_name, name)) {
		g_warning ("jit icall already defined \"%s\"\n", name);
		g_assert_not_reached ();
	}

	info = g_new0 (MonoJitICallInfo, 1);
	
	info->name = name;
	info->func = func;
	info->sig = sig;
	info->c_symbol = c_symbol;

	if (is_save) {
		info->wrapper = func;
	} else {
		info->wrapper = NULL;
	}

	g_hash_table_insert (jit_icall_hash_name, (gpointer)info->name, info);
	g_hash_table_insert (jit_icall_hash_addr, (gpointer)func, info);

	mono_icall_unlock ();
	return info;
}

MonoJitICallInfo *
mono_register_jit_icall (gconstpointer func, const char *name, MonoMethodSignature *sig, gboolean is_save)
{
	return mono_register_jit_icall_full (func, name, sig, is_save, NULL);
}

