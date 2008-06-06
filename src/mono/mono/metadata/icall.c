/*
 * icall.c:
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *	 Patrik Torstensson (patrik.torstensson@labs2.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <stdarg.h>
#include <string.h>
#include <ctype.h>
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#if defined (PLATFORM_WIN32)
#include <stdlib.h>
#endif

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
#include <mono/io-layer/io-layer.h>
#include <mono/utils/strtod.h>
#include <mono/utils/monobitset.h>
#include <mono/utils/mono-time.h>

#if defined (PLATFORM_WIN32)
#include <windows.h>
#include <shlobj.h>
#endif
#include "decimal.h"

static MonoReflectionAssembly* ves_icall_System_Reflection_Assembly_GetCallingAssembly (void);

static MonoArray*
type_array_from_modifiers (MonoImage *image, MonoType *type, int optional);

static inline MonoBoolean
is_generic_parameter (MonoType *type)
{
	return !type->byref && (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR);
}

/*
 * We expect a pointer to a char, not a string
 */
static gboolean
mono_double_ParseImpl (char *ptr, double *result)
{
	gchar *endptr = NULL;
	*result = 0.0;

	MONO_ARCH_SAVE_REGS;

#ifdef __arm__
	if (*ptr)
		*result = strtod (ptr, &endptr);
#else
	if (*ptr)
		*result = mono_strtod (ptr, &endptr);
#endif

	if (!*ptr || (endptr && *endptr))
		return FALSE;
	
	return TRUE;
}

static MonoClass *
mono_class_get_throw (MonoImage *image, guint32 type_token)
{
	MonoClass *class = mono_class_get (image, type_token);
	MonoLoaderError *error;
	MonoException *ex;
	
	if (class != NULL)
		return class;

	error = mono_loader_get_last_error ();
	g_assert (error != NULL);
	
	ex = mono_loader_error_prepare_exception (error);
	mono_raise_exception (ex);
	return NULL;
}

static MonoObject *
ves_icall_System_Array_GetValueImpl (MonoObject *this, guint32 pos)
{
	MonoClass *ac;
	MonoArray *ao;
	gint32 esize;
	gpointer *ea;

	MONO_ARCH_SAVE_REGS;

	ao = (MonoArray *)this;
	ac = (MonoClass *)ao->obj.vtable->klass;

	esize = mono_array_element_size (ac);
	ea = (gpointer*)((char*)ao->vector + (pos * esize));

	if (ac->element_class->valuetype)
		return mono_value_box (this->vtable->domain, ac->element_class, ea);
	else
		return *ea;
}

static MonoObject *
ves_icall_System_Array_GetValue (MonoObject *this, MonoObject *idxs)
{
	MonoClass *ac, *ic;
	MonoArray *ao, *io;
	gint32 i, pos, *ind;

	MONO_ARCH_SAVE_REGS;

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
		    (ind [i] >= ao->bounds [i].length + ao->bounds [i].lower_bound))
			mono_raise_exception (mono_get_exception_index_out_of_range ());

	pos = ind [0] - ao->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++)
		pos = pos*ao->bounds [i].length + ind [i] - 
			ao->bounds [i].lower_bound;

	return ves_icall_System_Array_GetValueImpl (this, pos);
}

static void
ves_icall_System_Array_SetValueImpl (MonoArray *this, MonoObject *value, guint32 pos)
{
	MonoClass *ac, *vc, *ec;
	gint32 esize, vsize;
	gpointer *ea, *va;
	int et, vt;

	guint64 u64 = 0;
	gint64 i64 = 0;
	gdouble r64 = 0;

	MONO_ARCH_SAVE_REGS;

	if (value)
		vc = value->vtable->klass;
	else
		vc = NULL;

	ac = this->obj.vtable->klass;
	ec = ac->element_class;

	esize = mono_array_element_size (ac);
	ea = (gpointer*)((char*)this->vector + (pos * esize));
	va = (gpointer*)((char*)value + sizeof (MonoObject));

	if (!value) {
		memset (ea, 0,  esize);
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

#define INVALID_CAST G_STMT_START{\
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
			memcpy (ea, (char *)value + sizeof (MonoObject), esize);
		return;
	}

	if (!vc->valuetype)
		INVALID_CAST;

	vsize = mono_class_instance_size (vc) - sizeof (MonoObject);

	et = ec->byval_arg.type;
	if (et == MONO_TYPE_VALUETYPE && ec->byval_arg.data.klass->enumtype)
		et = ec->byval_arg.data.klass->enum_basetype->type;

	vt = vc->byval_arg.type;
	if (vt == MONO_TYPE_VALUETYPE && vc->byval_arg.data.klass->enumtype)
		vt = vc->byval_arg.data.klass->enum_basetype->type;

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

static void 
ves_icall_System_Array_SetValue (MonoArray *this, MonoObject *value,
				 MonoArray *idxs)
{
	MonoClass *ac, *ic;
	gint32 i, pos, *ind;

	MONO_ARCH_SAVE_REGS;

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
		    (ind [i] >= this->bounds [i].length + this->bounds [i].lower_bound))
			mono_raise_exception (mono_get_exception_index_out_of_range ());

	pos = ind [0] - this->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++)
		pos = pos * this->bounds [i].length + ind [i] - 
			this->bounds [i].lower_bound;

	ves_icall_System_Array_SetValueImpl (this, value, pos);
}

static MonoArray *
ves_icall_System_Array_CreateInstanceImpl (MonoReflectionType *type, MonoArray *lengths, MonoArray *bounds)
{
	MonoClass *aklass;
	MonoArray *array;
	mono_array_size_t *sizes, i;
	gboolean bounded = FALSE;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (type);
	MONO_CHECK_ARG_NULL (lengths);

	MONO_CHECK_ARG (lengths, mono_array_length (lengths) > 0);
	if (bounds)
		MONO_CHECK_ARG (bounds, mono_array_length (lengths) == mono_array_length (bounds));

	for (i = 0; i < mono_array_length (lengths); i++)
		if (mono_array_get (lengths, gint32, i) < 0)
			mono_raise_exception (mono_get_exception_argument_out_of_range (NULL));

	if (bounds && (mono_array_length (bounds) == 1) && (mono_array_get (bounds, gint32, 0) != 0))
		/* vectors are not the same as one dimensional arrays with no-zero bounds */
		bounded = TRUE;
	else
		bounded = FALSE;

	aklass = mono_bounded_array_class_get (mono_class_from_mono_type (type->type), mono_array_length (lengths), bounded);

	sizes = alloca (aklass->rank * sizeof(mono_array_size_t) * 2);
	for (i = 0; i < aklass->rank; ++i) {
		sizes [i] = mono_array_get (lengths, guint32, i);
		if (bounds)
			sizes [i + aklass->rank] = mono_array_get (bounds, guint32, i);
		else
			sizes [i + aklass->rank] = 0;
	}

	array = mono_array_new_full (mono_object_domain (type), aklass, sizes, sizes + aklass->rank);

	return array;
}

static MonoArray *
ves_icall_System_Array_CreateInstanceImpl64 (MonoReflectionType *type, MonoArray *lengths, MonoArray *bounds)
{
	MonoClass *aklass;
	MonoArray *array;
	mono_array_size_t *sizes, i;
	gboolean bounded = FALSE;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (type);
	MONO_CHECK_ARG_NULL (lengths);

	MONO_CHECK_ARG (lengths, mono_array_length (lengths) > 0);
	if (bounds)
		MONO_CHECK_ARG (bounds, mono_array_length (lengths) == mono_array_length (bounds));

	for (i = 0; i < mono_array_length (lengths); i++) 
		if ((mono_array_get (lengths, gint64, i) < 0) ||
		    (mono_array_get (lengths, gint64, i) > MONO_ARRAY_MAX_INDEX))
			mono_raise_exception (mono_get_exception_argument_out_of_range (NULL));

	if (bounds && (mono_array_length (bounds) == 1) && (mono_array_get (bounds, gint64, 0) != 0))
		/* vectors are not the same as one dimensional arrays with no-zero bounds */
		bounded = TRUE;
	else
		bounded = FALSE;

	aklass = mono_bounded_array_class_get (mono_class_from_mono_type (type->type), mono_array_length (lengths), bounded);

	sizes = alloca (aklass->rank * sizeof(mono_array_size_t) * 2);
	for (i = 0; i < aklass->rank; ++i) {
		sizes [i] = mono_array_get (lengths, guint64, i);
		if (bounds)
			sizes [i + aklass->rank] = (mono_array_size_t) mono_array_get (bounds, guint64, i);
		else
			sizes [i + aklass->rank] = 0;
	}

	array = mono_array_new_full (mono_object_domain (type), aklass, sizes, sizes + aklass->rank);

	return array;
}

static gint32 
ves_icall_System_Array_GetRank (MonoObject *this)
{
	MONO_ARCH_SAVE_REGS;

	return this->vtable->klass->rank;
}

static gint32
ves_icall_System_Array_GetLength (MonoArray *this, gint32 dimension)
{
	gint32 rank = ((MonoObject *)this)->vtable->klass->rank;
	mono_array_size_t length;

	MONO_ARCH_SAVE_REGS;

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

static gint64
ves_icall_System_Array_GetLongLength (MonoArray *this, gint32 dimension)
{
	gint32 rank = ((MonoObject *)this)->vtable->klass->rank;

	MONO_ARCH_SAVE_REGS;

	if ((dimension < 0) || (dimension >= rank))
		mono_raise_exception (mono_get_exception_index_out_of_range ());
	
	if (this->bounds == NULL)
 		return this->max_length;
 	
 	return this->bounds [dimension].length;
}

static gint32
ves_icall_System_Array_GetLowerBound (MonoArray *this, gint32 dimension)
{
	gint32 rank = ((MonoObject *)this)->vtable->klass->rank;

	MONO_ARCH_SAVE_REGS;

	if ((dimension < 0) || (dimension >= rank))
		mono_raise_exception (mono_get_exception_index_out_of_range ());
	
	if (this->bounds == NULL)
		return 0;
	
	return this->bounds [dimension].lower_bound;
}

static void
ves_icall_System_Array_ClearInternal (MonoArray *arr, int idx, int length)
{
	int sz = mono_array_element_size (mono_object_class (arr));
	memset (mono_array_addr_with_size (arr, sz, idx), 0, length * sz);
}

static gboolean
ves_icall_System_Array_FastCopy (MonoArray *source, int source_idx, MonoArray* dest, int dest_idx, int length)
{
	int element_size;
	void * dest_addr;
	void * source_addr;
	MonoClass *src_class;
	MonoClass *dest_class;
	int i;

	MONO_ARCH_SAVE_REGS;

	if (source->obj.vtable->klass->rank != dest->obj.vtable->klass->rank)
		return FALSE;

	if (source->bounds || dest->bounds)
		return FALSE;

	if ((dest_idx + length > mono_array_length (dest)) ||
		(source_idx + length > mono_array_length (source)))
		return FALSE;

	src_class = source->obj.vtable->klass->element_class;
	dest_class = dest->obj.vtable->klass->element_class;

	/*
	 * Handle common cases.
	 */

	/* Case1: object[] -> valuetype[] (ArrayList::ToArray) */
	if (src_class == mono_defaults.object_class && dest_class->valuetype) {
		int has_refs = dest_class->has_references;
		for (i = source_idx; i < source_idx + length; ++i) {
			MonoObject *elem = mono_array_get (source, MonoObject*, i);
			if (elem && !mono_object_isinst (elem, dest_class))
				return FALSE;
		}

		element_size = mono_array_element_size (dest->obj.vtable->klass);
		memset (mono_array_addr_with_size (dest, element_size, dest_idx), 0, element_size * length);
		for (i = 0; i < length; ++i) {
			MonoObject *elem = mono_array_get (source, MonoObject*, source_idx + i);
			void *addr = mono_array_addr_with_size (dest, element_size, dest_idx + i);
			if (!elem)
				continue;
			if (has_refs)
				mono_value_copy (addr, (char *)elem + sizeof (MonoObject), dest_class);
			else
				memcpy (addr, (char *)elem + sizeof (MonoObject), element_size);
		}
		return TRUE;
	}

	/* Check if we're copying a char[] <==> (u)short[] */
	if (src_class != dest_class) {
		if (dest_class->valuetype || dest_class->enumtype || src_class->valuetype || src_class->enumtype)
			return FALSE;

		if (mono_class_is_subclass_of (src_class, dest_class, FALSE))
			;
		/* Case2: object[] -> reftype[] (ArrayList::ToArray) */
		else if (mono_class_is_subclass_of (dest_class, src_class, FALSE))
			for (i = source_idx; i < source_idx + length; ++i) {
				MonoObject *elem = mono_array_get (source, MonoObject*, i);
				if (elem && !mono_object_isinst (elem, dest_class))
					return FALSE;
			}
		else
			return FALSE;
	}

	if (dest_class->valuetype) {
		element_size = mono_array_element_size (source->obj.vtable->klass);
		source_addr = mono_array_addr_with_size (source, element_size, source_idx);
		if (dest_class->has_references) {
			mono_value_copy_array (dest, dest_idx, source_addr, length);
		} else {
			dest_addr = mono_array_addr_with_size (dest, element_size, dest_idx);
			memmove (dest_addr, source_addr, element_size * length);
		}
	} else {
		mono_array_memcpy_refs (dest, dest_idx, source, source_idx, length);
	}

	return TRUE;
}

static void
ves_icall_System_Array_GetGenericValueImpl (MonoObject *this, guint32 pos, gpointer value)
{
	MonoClass *ac;
	MonoArray *ao;
	gint32 esize;
	gpointer *ea;

	MONO_ARCH_SAVE_REGS;

	ao = (MonoArray *)this;
	ac = (MonoClass *)ao->obj.vtable->klass;

	esize = mono_array_element_size (ac);
	ea = (gpointer*)((char*)ao->vector + (pos * esize));

	memcpy (value, ea, esize);
}

static void
ves_icall_System_Array_SetGenericValueImpl (MonoObject *this, guint32 pos, gpointer value)
{
	MonoClass *ac;
	MonoArray *ao;
	gint32 esize;
	gpointer *ea;

	MONO_ARCH_SAVE_REGS;

	ao = (MonoArray *)this;
	ac = (MonoClass *)ao->obj.vtable->klass;

	esize = mono_array_element_size (ac);
	ea = (gpointer*)((char*)ao->vector + (pos * esize));

	memcpy (ea, value, esize);
}

static void
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray (MonoArray *array, MonoClassField *field_handle)
{
	MonoClass *klass = array->obj.vtable->klass;
	guint32 size = mono_array_element_size (klass);
	MonoType *type = mono_type_get_underlying_type (&klass->element_class->byval_arg);
	int align;

	if (MONO_TYPE_IS_REFERENCE (type) ||
			(type->type == MONO_TYPE_VALUETYPE &&
				(!mono_type_get_class (type) ||
				mono_type_get_class (type)->has_references))) {
		MonoException *exc = mono_get_exception_argument("array",
			"Cannot initialize array containing references");
		mono_raise_exception (exc);
	}

	if (!(field_handle->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA)) {
		MonoException *exc = mono_get_exception_argument("field_handle",
			"Field doesn't have an RVA");
		mono_raise_exception (exc);
	}

	size *= array->max_length;

	if (size > mono_type_size (field_handle->type, &align)) {
		MonoException *exc = mono_get_exception_argument("field_handle",
			"Field not large enough to fill array");
		mono_raise_exception (exc);
	}

#if G_BYTE_ORDER != G_LITTLE_ENDIAN
#define SWAP(n) {\
	guint ## n *data = (guint ## n *) mono_array_addr (array, char, 0); \
	guint ## n *src = (guint ## n *) field_handle->data; \
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
		memcpy (mono_array_addr (array, char, 0), field_handle->data, size);
		break;
	}
#else
	memcpy (mono_array_addr (array, char, 0), field_handle->data, size);
#ifdef ARM_FPU_FPA
	if (klass->element_class->byval_arg.type == MONO_TYPE_R8) {
		gint i;
		double tmp;
		double *data = (double*)mono_array_addr (array, double, 0);

		for (i = 0; i < size; i++, data++) {
			readr8 (data, &tmp);
			*data = tmp;
		}
	}
#endif
#endif
}

static gint
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetOffsetToStringData (void)
{
	MONO_ARCH_SAVE_REGS;

	return offsetof (MonoString, chars);
}

static MonoObject *
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetObjectValue (MonoObject *obj)
{
	MONO_ARCH_SAVE_REGS;

	if ((obj == NULL) || (! (obj->vtable->klass->valuetype)))
		return obj;
	else
		return mono_object_clone (obj);
}

static void
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunClassConstructor (MonoType *handle)
{
	MonoClass *klass;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (handle);

	klass = mono_class_from_mono_type (handle);
	MONO_CHECK_ARG (handle, klass);

	/* This will call the type constructor */
	mono_runtime_class_init (mono_class_vtable (mono_domain_get (), klass));
}

static void
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunModuleConstructor (MonoImage *image)
{
	MONO_ARCH_SAVE_REGS;

	mono_image_check_for_module_cctor (image);
	if (image->has_module_cctor) {
		MonoClass *module_klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | 1);
		mono_runtime_class_init (mono_class_vtable (mono_domain_get (), module_klass));
	}
}

static MonoObject *
ves_icall_System_Object_MemberwiseClone (MonoObject *this)
{
	MONO_ARCH_SAVE_REGS;

	return mono_object_clone (this);
}

static gint32
ves_icall_System_ValueType_InternalGetHashCode (MonoObject *this, MonoArray **fields)
{
	MonoClass *klass;
	MonoObject **values = NULL;
	MonoObject *o;
	int count = 0;
	gint32 result = 0;
	MonoClassField* field;
	gpointer iter;

	MONO_ARCH_SAVE_REGS;

	klass = mono_object_class (this);

	if (mono_class_num_fields (klass) == 0)
		return mono_object_hash (this);

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
		*fields = mono_array_new (mono_domain_get (), mono_defaults.object_class, count);
		for (i = 0; i < count; ++i)
			mono_array_setref (*fields, i, values [i]);
	} else {
		*fields = NULL;
	}
	return result;
}

static MonoBoolean
ves_icall_System_ValueType_Equals (MonoObject *this, MonoObject *that, MonoArray **fields)
{
	MonoClass *klass;
	MonoObject **values = NULL;
	MonoObject *o;
	MonoClassField* field;
	gpointer iter;
	int count = 0;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (that);

	if (this->vtable != that->vtable)
		return FALSE;

	klass = mono_object_class (this);

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
		case MONO_TYPE_I4:
			if (*(gint32*)((guint8*)this + field->offset) != *(gint32*)((guint8*)that + field->offset))
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
	}

	if (values) {
		int i;
		*fields = mono_array_new (mono_domain_get (), mono_defaults.object_class, count);
		for (i = 0; i < count; ++i)
			mono_array_setref (*fields, i, values [i]);
		return FALSE;
	} else {
		return TRUE;
	}
}

static MonoReflectionType *
ves_icall_System_Object_GetType (MonoObject *obj)
{
	MONO_ARCH_SAVE_REGS;

	if (obj->vtable->klass != mono_defaults.transparent_proxy_class)
		return mono_type_get_object (mono_object_domain (obj), &obj->vtable->klass->byval_arg);
	else
		return mono_type_get_object (mono_object_domain (obj), &((MonoTransparentProxy*)obj)->remote_class->proxy_class->byval_arg);
}

static void
mono_type_type_from_obj (MonoReflectionType *mtype, MonoObject *obj)
{
	MONO_ARCH_SAVE_REGS;

	mtype->type = &obj->vtable->klass->byval_arg;
	g_assert (mtype->type->type);
}

static gint32
ves_icall_ModuleBuilder_getToken (MonoReflectionModuleBuilder *mb, MonoObject *obj)
{
	MONO_ARCH_SAVE_REGS;
	
	MONO_CHECK_ARG_NULL (obj);
	
	return mono_image_create_token (mb->dynamic_image, obj, TRUE, TRUE);
}

static gint32
ves_icall_ModuleBuilder_getMethodToken (MonoReflectionModuleBuilder *mb,
					MonoReflectionMethod *method,
					MonoArray *opt_param_types)
{
	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (method);
	
	return mono_image_create_method_token (
		mb->dynamic_image, (MonoObject *) method, opt_param_types);
}

static void
ves_icall_ModuleBuilder_WriteToFile (MonoReflectionModuleBuilder *mb, HANDLE file)
{
	MONO_ARCH_SAVE_REGS;

	mono_image_create_pefile (mb, file);
}

static void
ves_icall_ModuleBuilder_build_metadata (MonoReflectionModuleBuilder *mb)
{
	MONO_ARCH_SAVE_REGS;

	mono_image_build_metadata (mb);
}

static void
ves_icall_ModuleBuilder_RegisterToken (MonoReflectionModuleBuilder *mb, MonoObject *obj, guint32 token)
{
	MONO_ARCH_SAVE_REGS;

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

static MonoReflectionType *
type_from_name (const char *str, MonoBoolean ignoreCase)
{
	MonoType *type = NULL;
	MonoAssembly *assembly = NULL;
	MonoTypeNameParse info;
	char *temp_str = g_strdup (str);
	gboolean type_resolve = FALSE;

	MONO_ARCH_SAVE_REGS;

	/* mono_reflection_parse_type() mangles the string */
	if (!mono_reflection_parse_type (temp_str, &info)) {
		mono_reflection_free_type_info (&info);
		g_free (temp_str);
		return NULL;
	}

	if (info.assembly.name) {
		assembly = mono_assembly_load (&info.assembly, NULL, NULL);
	} else {
		MonoMethod *m = mono_method_get_last_managed ();
		MonoMethod *dest = m;

		mono_stack_walk_no_il (get_caller, &dest);
		if (!dest)
			dest = m;

		/*
		 * FIXME: mono_method_get_last_managed() sometimes returns NULL, thus
		 *        causing ves_icall_System_Reflection_Assembly_GetCallingAssembly()
		 *        to crash.  This only seems to happen in some strange remoting
		 *        scenarios and I was unable to figure out what's happening there.
		 *        Dec 10, 2005 - Martin.
		 */

		if (dest)
			assembly = dest->klass->image->assembly;
		else {
			g_warning (G_STRLOC);
		}
	}

	if (assembly)
		type = mono_reflection_get_type (assembly->image, &info, ignoreCase, &type_resolve);
	
	if (!info.assembly.name && !type) /* try mscorlib */
		type = mono_reflection_get_type (NULL, &info, ignoreCase, &type_resolve);

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

static MonoReflectionType*
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


static MonoReflectionType*
ves_icall_type_from_handle (MonoType *handle)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *klass = mono_class_from_mono_type (handle);

	MONO_ARCH_SAVE_REGS;

	mono_class_init (klass);
	return mono_type_get_object (domain, handle);
}

static MonoBoolean
ves_icall_System_Type_EqualsInternal (MonoReflectionType *type, MonoReflectionType *c)
{
	MONO_ARCH_SAVE_REGS;

	if (c && type->type && c->type)
		return mono_metadata_type_equal (type->type, c->type);
	else
		return FALSE;
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

static guint32
ves_icall_type_GetTypeCodeInternal (MonoReflectionType *type)
{
	int t = type->type->type;

	MONO_ARCH_SAVE_REGS;

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
	case MONO_TYPE_VALUETYPE:
		if (type->type->data.klass->enumtype) {
			t = type->type->data.klass->enum_basetype->type;
			goto handle_enum;
		} else {
			MonoClass *k =  type->type->data.klass;
			if (strcmp (k->name_space, "System") == 0) {
				if (strcmp (k->name, "Decimal") == 0)
					return TYPECODE_DECIMAL;
				else if (strcmp (k->name, "DateTime") == 0)
					return TYPECODE_DATETIME;
			}
		}
		return TYPECODE_OBJECT;
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
			MonoClass *k =  type->type->data.klass;
			if (strcmp (k->name_space, "System") == 0) {
				if (strcmp (k->name, "DBNull") == 0)
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

static guint32
ves_icall_type_is_subtype_of (MonoReflectionType *type, MonoReflectionType *c, MonoBoolean check_interfaces)
{
	MonoDomain *domain; 
	MonoClass *klass;
	MonoClass *klassc;

	MONO_ARCH_SAVE_REGS;

	g_assert (type != NULL);
	
	domain = ((MonoObject *)type)->vtable->domain;

	if (!c) /* FIXME: dont know what do do here */
		return 0;

	klass = mono_class_from_mono_type (type->type);
	klassc = mono_class_from_mono_type (c->type);

	if (type->type->byref)
		return klassc == mono_defaults.object_class;

	return mono_class_is_subclass_of (klass, klassc, check_interfaces);
}

static guint32
ves_icall_type_is_assignable_from (MonoReflectionType *type, MonoReflectionType *c)
{
	MonoDomain *domain; 
	MonoClass *klass;
	MonoClass *klassc;

	MONO_ARCH_SAVE_REGS;

	g_assert (type != NULL);
	
	domain = ((MonoObject *)type)->vtable->domain;

	klass = mono_class_from_mono_type (type->type);
	klassc = mono_class_from_mono_type (c->type);

	if (type->type->byref && !c->type->byref)
		return FALSE;

	return mono_class_is_assignable_from (klass, klassc);
}

static guint32
ves_icall_type_IsInstanceOfType (MonoReflectionType *type, MonoObject *obj)
{
	MonoClass *klass = mono_class_from_mono_type (type->type);
	return mono_object_isinst (obj, klass) != NULL;
}

static guint32
ves_icall_get_attributes (MonoReflectionType *type)
{
	MonoClass *klass = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	return klass->flags;
}

static MonoReflectionMarshal*
ves_icall_System_Reflection_FieldInfo_GetUnmanagedMarshal (MonoReflectionField *field)
{
	MonoClass *klass = field->field->parent;
	MonoMarshalType *info;
	int i;

	if (klass->generic_container ||
	    (klass->generic_class && klass->generic_class->context.class_inst->is_open))
		return NULL;

	info = mono_marshal_load_type_info (klass);

	for (i = 0; i < info->num_fields; ++i) {
		if (info->fields [i].field == field->field) {
			if (!info->fields [i].mspec)
				return NULL;
			else
				return mono_reflection_marshal_from_marshal_spec (field->object.vtable->domain, klass, info->fields [i].mspec);
		}
	}

	return NULL;
}

static MonoReflectionField*
ves_icall_System_Reflection_FieldInfo_internal_from_handle_type (MonoClassField *handle, MonoClass *klass)
{
	g_assert (handle);

	if (!klass)
		klass = handle->parent;

	/* FIXME: check that handle is a field of klass or of a parent: return null
	 * and throw the exception in managed code.
	 */
	return mono_field_get_object (mono_domain_get (), klass, handle);
}

static MonoReflectionField*
ves_icall_System_Reflection_FieldInfo_internal_from_handle (MonoClassField *handle)
{
	MONO_ARCH_SAVE_REGS;

	g_assert (handle);

	return mono_field_get_object (mono_domain_get (), handle->parent, handle);
}

static MonoArray*
ves_icall_System_Reflection_FieldInfo_GetTypeModifiers (MonoReflectionField *field, MonoBoolean optional)
{
	MonoType *type = field->field->type;

	return type_array_from_modifiers (field->field->parent->image, type, optional);
}

static void
ves_icall_get_method_info (MonoMethod *method, MonoMethodInfo *info)
{
	MonoDomain *domain = mono_domain_get ();
	MonoMethodSignature* sig;
	MONO_ARCH_SAVE_REGS;

	sig = mono_method_signature (method);
	if (!sig) {
		g_assert (mono_loader_get_last_error ());
		mono_raise_exception (mono_loader_error_prepare_exception (mono_loader_get_last_error ()));
	}
	
	info->parent = mono_type_get_object (domain, &method->klass->byval_arg);
	info->ret = mono_type_get_object (domain, sig->ret);
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

static MonoArray*
ves_icall_get_parameter_info (MonoMethod *method)
{
	MonoDomain *domain = mono_domain_get (); 

	return mono_param_get_objects (domain, method);
}

static MonoReflectionMarshal*
ves_icall_System_MonoMethodInfo_get_retval_marshal (MonoMethod *method)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoReflectionMarshal* res = NULL;
	MonoMarshalSpec **mspecs;
	int i;

	mspecs = g_new (MonoMarshalSpec*, mono_method_signature (method)->param_count + 1);
	mono_method_get_marshal_info (method, mspecs);

	if (mspecs [0])
		res = mono_reflection_marshal_from_marshal_spec (domain, method->klass, mspecs [0]);
		
	for (i = mono_method_signature (method)->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);

	return res;
}

static gint32
ves_icall_MonoField_GetFieldOffset (MonoReflectionField *field)
{
	return field->field->offset - sizeof (MonoObject);
}

static MonoReflectionType*
ves_icall_MonoField_GetParentType (MonoReflectionField *field, MonoBoolean declaring)
{
	MonoClass *parent;
	MONO_ARCH_SAVE_REGS;

	parent = declaring? field->field->parent: field->klass;

	return mono_type_get_object (mono_object_domain (field), &parent->byval_arg);
}

static MonoObject *
ves_icall_MonoField_GetValueInternal (MonoReflectionField *field, MonoObject *obj)
{	
	MonoObject *o;
	MonoClassField *cf = field->field;
	MonoClass *klass;
	MonoVTable *vtable;
	MonoType *t;
	MonoDomain *domain = mono_object_domain (field); 
	gchar *v;
	gboolean is_static = FALSE;
	gboolean is_ref = FALSE;

	MONO_ARCH_SAVE_REGS;

	if (field->klass->image->assembly->ref_only)
		mono_raise_exception (mono_get_exception_invalid_operation (
					"It is illegal to get the value on a field on a type loaded using the ReflectionOnly methods."));
	
	mono_class_init (field->klass);

	if (cf->type->attrs & FIELD_ATTRIBUTE_STATIC)
		is_static = TRUE;

	if (obj && !is_static) {
		/* Check that the field belongs to the object */
		gboolean found = FALSE;
		MonoClass *k;

		for (k = obj->vtable->klass; k; k = k->parent) {
			if (k == cf->parent) {
				found = TRUE;
				break;
			}
		}

		if (!found) {
			char *msg = g_strdup_printf ("Field '%s' defined on type '%s' is not a field on the target object which is of type '%s'.", cf->name, cf->parent->name, obj->vtable->klass->name);
			MonoException *ex = mono_get_exception_argument (NULL, msg);
			g_free (msg);
			mono_raise_exception (ex);
		}
	}

	t = mono_type_get_underlying_type (cf->type);
	switch (t->type) {
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
		is_ref = TRUE;
		break;
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
		is_ref = t->byref;
		break;
	case MONO_TYPE_GENERICINST:
		if (mono_type_generic_inst_is_valuetype (t)) {
			is_ref = t->byref;
		} else {
			is_ref = TRUE;
		}
		break;
	default:
		g_error ("type 0x%x not handled in "
			 "ves_icall_Monofield_GetValue", t->type);
		return NULL;
	}

	vtable = NULL;
	if (is_static) {
		vtable = mono_class_vtable (domain, cf->parent);
		if (!vtable->initialized && !(cf->type->attrs & FIELD_ATTRIBUTE_LITERAL))
			mono_runtime_class_init (vtable);
	}
	
	if (is_ref) {
		if (is_static) {
			mono_field_static_get_value (vtable, cf, &o);
		} else {
			mono_field_get_value (obj, cf, &o);
		}
		return o;
	}

	if (mono_class_is_nullable (mono_class_from_mono_type (cf->type))) {
		MonoClass *nklass = mono_class_from_mono_type (cf->type);
		guint8 *buf;

		/* Convert the Nullable structure into a boxed vtype */
		if (is_static)
			buf = (guint8*)vtable->data + cf->offset;
		else
			buf = (guint8*)obj + cf->offset;

		return mono_nullable_box (buf, nklass);
	}

	/* boxed value type */
	klass = mono_class_from_mono_type (cf->type);
	o = mono_object_new (domain, klass);
	v = ((gchar *) o) + sizeof (MonoObject);
	if (is_static) {
		mono_field_static_get_value (vtable, cf, v);
	} else {
		mono_field_get_value (obj, cf, v);
	}

	return o;
}

static void
ves_icall_MonoField_SetValueInternal (MonoReflectionField *field, MonoObject *obj, MonoObject *value)
{
	MonoClassField *cf = field->field;
	gchar *v;

	MONO_ARCH_SAVE_REGS;

	if (field->klass->image->assembly->ref_only)
		mono_raise_exception (mono_get_exception_invalid_operation (
					"It is illegal to set the value on a field on a type loaded using the ReflectionOnly methods."));

	v = (gchar *) value;
	if (!cf->type->byref) {
		switch (cf->type->type) {
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
			MonoGenericClass *gclass = cf->type->data.generic_class;
			g_assert (!gclass->context.class_inst->is_open);

			if (mono_class_is_nullable (mono_class_from_mono_type (cf->type))) {
				MonoClass *nklass = mono_class_from_mono_type (cf->type);
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
				 "ves_icall_FieldInfo_SetValueInternal", cf->type->type);
			return;
		}
	}

	if (cf->type->attrs & FIELD_ATTRIBUTE_STATIC) {
		MonoVTable *vtable = mono_class_vtable (mono_object_domain (field), cf->parent);
		if (!vtable->initialized)
			mono_runtime_class_init (vtable);
		mono_field_static_set_value (vtable, cf, v);
	} else {
		mono_field_set_value (obj, cf, v);
	}
}

static MonoObject *
ves_icall_MonoField_GetRawConstantValue (MonoReflectionField *this)
{	
	MonoObject *o = NULL;
	MonoClassField *field = this->field;
	MonoClass *klass;
	MonoDomain *domain = mono_object_domain (this); 
	gchar *v;
	MonoTypeEnum def_type;
	const char *def_value;

	MONO_ARCH_SAVE_REGS;
	
	mono_class_init (field->parent);

	if (!(field->type->attrs & FIELD_ATTRIBUTE_HAS_DEFAULT))
		mono_raise_exception (mono_get_exception_invalid_operation (NULL));

	if (field->parent->image->dynamic) {
		/* FIXME: */
		g_assert_not_reached ();
	}

	def_value = mono_class_get_field_default_value (field, &def_type);

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

static MonoReflectionType*
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

static void
ves_icall_get_property_info (MonoReflectionProperty *property, MonoPropertyInfo *info, PInfo req_info)
{
	MonoDomain *domain = mono_object_domain (property); 

	MONO_ARCH_SAVE_REGS;

	if ((req_info & PInfo_ReflectedType) != 0)
		MONO_STRUCT_SETREF (info, parent, mono_type_get_object (domain, &property->klass->byval_arg));
	else if ((req_info & PInfo_DeclaringType) != 0)
		MONO_STRUCT_SETREF (info, parent, mono_type_get_object (domain, &property->property->parent->byval_arg));

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

static void
ves_icall_get_event_info (MonoReflectionEvent *event, MonoEventInfo *info)
{
	MonoDomain *domain = mono_object_domain (event); 

	MONO_ARCH_SAVE_REGS;

	info->reflected_type = mono_type_get_object (domain, &event->klass->byval_arg);
	info->declaring_type = mono_type_get_object (domain, &event->event->parent->byval_arg);

	info->name = mono_string_new (domain, event->event->name);
	info->attrs = event->event->attrs;
	info->add_method = event->event->add ? mono_method_get_object (domain, event->event->add, NULL): NULL;
	info->remove_method = event->event->remove ? mono_method_get_object (domain, event->event->remove, NULL): NULL;
	info->raise_method = event->event->raise ? mono_method_get_object (domain, event->event->raise, NULL): NULL;

	if (event->event->other) {
		int i, n = 0;
		while (event->event->other [n])
			n++;
		info->other_methods = mono_array_new (domain, mono_defaults.method_info_class, n);

		for (i = 0; i < n; i++)
			mono_array_setref (info->other_methods, i, mono_method_get_object (domain, event->event->other [i], NULL));
	}		
}

static MonoArray*
ves_icall_Type_GetInterfaces (MonoReflectionType* type)
{
	MonoDomain *domain = mono_object_domain (type); 
	MonoArray *intf;
	GPtrArray *ifaces = NULL;
	int i;
	MonoClass *class = mono_class_from_mono_type (type->type);
	MonoClass *parent;
	MonoBitSet *slots;
	MonoGenericContext *context = NULL;

	MONO_ARCH_SAVE_REGS;

	if (class->generic_class && class->generic_class->context.class_inst->is_open) {
		context = mono_class_get_context (class);
		class = class->generic_class->container_class;
	}

	mono_class_setup_vtable (class);

	slots = mono_bitset_new (class->max_interface_id + 1, 0);

	for (parent = class; parent; parent = parent->parent) {
		GPtrArray *tmp_ifaces = mono_class_get_implemented_interfaces (parent);
		if (tmp_ifaces) {
			for (i = 0; i < tmp_ifaces->len; ++i) {
				MonoClass *ic = g_ptr_array_index (tmp_ifaces, i);

				if (mono_bitset_test (slots, ic->interface_id))
					continue;

				mono_bitset_set (slots, ic->interface_id);
				if (ifaces == NULL)
					ifaces = g_ptr_array_new ();
				g_ptr_array_add (ifaces, ic);
			}
			g_ptr_array_free (tmp_ifaces, TRUE);
		}
	}
	mono_bitset_free (slots);

	if (!ifaces)
		return mono_array_new (domain, mono_defaults.monotype_class, 0);
		
	intf = mono_array_new (domain, mono_defaults.monotype_class, ifaces->len);
	for (i = 0; i < ifaces->len; ++i) {
		MonoClass *ic = g_ptr_array_index (ifaces, i);
		MonoType *ret = &ic->byval_arg, *inflated = NULL;
		if (context && ic->generic_class && ic->generic_class->context.class_inst->is_open)
			inflated = ret = mono_class_inflate_generic_type (ret, context);
		
		mono_array_setref (intf, i, mono_type_get_object (domain, ret));
		if (inflated)
			mono_metadata_free_type (inflated);
	}
	g_ptr_array_free (ifaces, TRUE);

	return intf;
}

static void
ves_icall_Type_GetInterfaceMapData (MonoReflectionType *type, MonoReflectionType *iface, MonoArray **targets, MonoArray **methods)
{
	MonoClass *class = mono_class_from_mono_type (type->type);
	MonoClass *iclass = mono_class_from_mono_type (iface->type);
	MonoReflectionMethod *member;
	MonoMethod* method;
	gpointer iter;
	int i = 0, len, ioffset;
	MonoDomain *domain;

	MONO_ARCH_SAVE_REGS;

	mono_class_setup_vtable (class);

	/* type doesn't implement iface: the exception is thrown in managed code */
	if (! MONO_CLASS_IMPLEMENTS_INTERFACE (class, iclass->interface_id))
			return;

	len = mono_class_num_methods (iclass);
	ioffset = mono_class_interface_offset (class, iclass);
	domain = mono_object_domain (type);
	*targets = mono_array_new (domain, mono_defaults.method_info_class, len);
	*methods = mono_array_new (domain, mono_defaults.method_info_class, len);
	iter = NULL;
	iter = NULL;
	while ((method = mono_class_get_methods (iclass, &iter))) {
		member = mono_method_get_object (domain, method, iclass);
		mono_array_setref (*methods, i, member);
		member = mono_method_get_object (domain, class->vtable [i + ioffset], class);
		mono_array_setref (*targets, i, member);
		
		i ++;
	}
}

static void
ves_icall_Type_GetPacking (MonoReflectionType *type, guint32 *packing, guint32 *size)
{
	MonoClass *klass = mono_class_from_mono_type (type->type);

	if (klass->image->dynamic) {
		MonoReflectionTypeBuilder *tb = (MonoReflectionTypeBuilder*)type;
		*packing = tb->packing_size;
		*size = tb->class_size;
	} else {
		mono_metadata_packing_from_typedef (klass->image, klass->type_token, packing, size);
	}
}

static MonoReflectionType*
ves_icall_MonoType_GetElementType (MonoReflectionType *type)
{
	MonoClass *class = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

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

static MonoReflectionType*
ves_icall_get_type_parent (MonoReflectionType *type)
{
	MonoClass *class = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	return class->parent ? mono_type_get_object (mono_object_domain (type), &class->parent->byval_arg): NULL;
}

static MonoBoolean
ves_icall_type_ispointer (MonoReflectionType *type)
{
	MONO_ARCH_SAVE_REGS;

	return type->type->type == MONO_TYPE_PTR;
}

static MonoBoolean
ves_icall_type_isprimitive (MonoReflectionType *type)
{
	MONO_ARCH_SAVE_REGS;

	return (!type->type->byref && (((type->type->type >= MONO_TYPE_BOOLEAN) && (type->type->type <= MONO_TYPE_R8)) || (type->type->type == MONO_TYPE_I) || (type->type->type == MONO_TYPE_U)));
}

static MonoBoolean
ves_icall_type_isbyref (MonoReflectionType *type)
{
	MONO_ARCH_SAVE_REGS;

	return type->type->byref;
}

static MonoBoolean
ves_icall_type_iscomobject (MonoReflectionType *type)
{
	MonoClass *klass = mono_class_from_mono_type (type->type);
	MONO_ARCH_SAVE_REGS;

	return (klass && klass->is_com_object);
}

static MonoReflectionModule*
ves_icall_MonoType_get_Module (MonoReflectionType *type)
{
	MonoClass *class = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	return mono_module_get_object (mono_object_domain (type), class->image);
}

static MonoReflectionAssembly*
ves_icall_MonoType_get_Assembly (MonoReflectionType *type)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *class = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	return mono_assembly_get_object (domain, class->image->assembly);
}

static MonoReflectionType*
ves_icall_MonoType_get_DeclaringType (MonoReflectionType *type)
{
	MonoDomain *domain = mono_domain_get ();
	MonoClass *class;

	MONO_ARCH_SAVE_REGS;

	if (type->type->byref)
		return NULL;
	if (type->type->type == MONO_TYPE_VAR)
		class = type->type->data.generic_param->owner->owner.klass;
	else if (type->type->type == MONO_TYPE_MVAR)
		class = type->type->data.generic_param->owner->owner.method->klass;
	else
		class = mono_class_from_mono_type (type->type)->nested_in;

	return class ? mono_type_get_object (domain, &class->byval_arg) : NULL;
}

static MonoReflectionType*
ves_icall_MonoType_get_UnderlyingSystemType (MonoReflectionType *type)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *class = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	if (class->enumtype && class->enum_basetype) /* types that are modified typebuilders may not have enum_basetype set */
		return mono_type_get_object (domain, class->enum_basetype);
	else if (class->element_class)
		return mono_type_get_object (domain, &class->element_class->byval_arg);
	else
		return NULL;
}

static MonoString*
ves_icall_MonoType_get_Name (MonoReflectionType *type)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *class = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	if (type->type->byref) {
		char *n = g_strdup_printf ("%s&", class->name);
		MonoString *res = mono_string_new (domain, n);

		g_free (n);

		return res;
	} else {
		return mono_string_new (domain, class->name);
	}
}

static MonoString*
ves_icall_MonoType_get_Namespace (MonoReflectionType *type)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *class = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	while (class->nested_in)
		class = class->nested_in;

	if (class->name_space [0] == '\0')
		return NULL;
	else
		return mono_string_new (domain, class->name_space);
}

static gint32
ves_icall_MonoType_GetArrayRank (MonoReflectionType *type)
{
	MonoClass *class = mono_class_from_mono_type (type->type);

	MONO_ARCH_SAVE_REGS;

	return class->rank;
}

static MonoArray*
ves_icall_MonoType_GetGenericArguments (MonoReflectionType *type)
{
	MonoArray *res;
	MonoClass *klass, *pklass;
	int i;
	MONO_ARCH_SAVE_REGS;

	klass = mono_class_from_mono_type (type->type);

	if (klass->generic_container) {
		MonoGenericContainer *container = klass->generic_container;
		res = mono_array_new (mono_object_domain (type), mono_defaults.systemtype_class, container->type_argc);
		for (i = 0; i < container->type_argc; ++i) {
			pklass = mono_class_from_generic_parameter (&container->type_params [i], klass->image, FALSE);
			mono_array_setref (res, i, mono_type_get_object (mono_object_domain (type), &pklass->byval_arg));
		}
	} else if (klass->generic_class) {
		MonoGenericInst *inst = klass->generic_class->context.class_inst;
		res = mono_array_new (mono_object_domain (type), mono_defaults.systemtype_class, inst->type_argc);
		for (i = 0; i < inst->type_argc; ++i)
			mono_array_setref (res, i, mono_type_get_object (mono_object_domain (type), inst->type_argv [i]));
	} else {
		res = mono_array_new (mono_object_domain (type), mono_defaults.systemtype_class, 0);
	}
	return res;
}

static gboolean
ves_icall_Type_get_IsGenericTypeDefinition (MonoReflectionType *type)
{
	MonoClass *klass;
	MONO_ARCH_SAVE_REGS;

	if (type->type->byref)
		return FALSE;

	klass = mono_class_from_mono_type (type->type);

	return klass->generic_container != NULL;
}

static MonoReflectionType*
ves_icall_Type_GetGenericTypeDefinition_impl (MonoReflectionType *type)
{
	MonoClass *klass;
	MONO_ARCH_SAVE_REGS;

	if (type->type->byref)
		return NULL;

	klass = mono_class_from_mono_type (type->type);
	if (klass->generic_container) {
		return type; /* check this one */
	}
	if (klass->generic_class) {
		MonoClass *generic_class = klass->generic_class->container_class;

		if (generic_class->wastypebuilder && generic_class->reflection_info)
			return generic_class->reflection_info;
		else
			return mono_type_get_object (mono_object_domain (type), &generic_class->byval_arg);
	}
	return NULL;
}

static MonoReflectionType*
ves_icall_Type_MakeGenericType (MonoReflectionType *type, MonoArray *type_array)
{
	MonoType *geninst, **types;
	int i, count;

	MONO_ARCH_SAVE_REGS;

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

	return mono_type_get_object (mono_object_domain (type), geninst);
}

static gboolean
ves_icall_Type_get_IsGenericInstance (MonoReflectionType *type)
{
	MonoClass *klass;
	MONO_ARCH_SAVE_REGS;

	if (type->type->byref)
		return FALSE;

	klass = mono_class_from_mono_type (type->type);
	return klass->generic_class != NULL;
}

static gboolean
ves_icall_Type_get_IsGenericType (MonoReflectionType *type)
{
	MonoClass *klass;
	MONO_ARCH_SAVE_REGS;

	if (type->type->byref)
		return FALSE;

	klass = mono_class_from_mono_type (type->type);
	return klass->generic_class != NULL || klass->generic_container != NULL;
}

static gint32
ves_icall_Type_GetGenericParameterPosition (MonoReflectionType *type)
{
	MONO_ARCH_SAVE_REGS;

	if (is_generic_parameter (type->type))
		return type->type->data.generic_param->num;
	return -1;
}

static GenericParameterAttributes
ves_icall_Type_GetGenericParameterAttributes (MonoReflectionType *type)
{
	MONO_ARCH_SAVE_REGS;
	g_assert (is_generic_parameter (type->type));
	return type->type->data.generic_param->flags;
}

static MonoArray *
ves_icall_Type_GetGenericParameterConstraints (MonoReflectionType *type)
{
	MonoGenericParam *param;
	MonoDomain *domain;
	MonoClass **ptr;
	MonoArray *res;
	int i, count;

	MONO_ARCH_SAVE_REGS;

	domain = mono_object_domain (type);
	param = type->type->data.generic_param;
	for (count = 0, ptr = param->constraints; ptr && *ptr; ptr++, count++)
		;

	res = mono_array_new (domain, mono_defaults.monotype_class, count);
	for (i = 0; i < count; i++)
		mono_array_setref (res, i, mono_type_get_object (domain, &param->constraints [i]->byval_arg));


	return res;
}

static MonoBoolean
ves_icall_MonoType_get_IsGenericParameter (MonoReflectionType *type)
{
	MONO_ARCH_SAVE_REGS;
	return is_generic_parameter (type->type);
}

static MonoBoolean
ves_icall_TypeBuilder_get_IsGenericParameter (MonoReflectionTypeBuilder *tb)
{
	MONO_ARCH_SAVE_REGS;
	return is_generic_parameter (tb->type.type);
}

static void
ves_icall_EnumBuilder_setup_enum_type (MonoReflectionType *enumtype,
									   MonoReflectionType *t)
{
	enumtype->type = t->type;
}

static MonoReflectionType*
ves_icall_MonoGenericClass_GetParentType (MonoReflectionGenericClass *type)
{
	MonoDynamicGenericClass *gclass;
	MonoReflectionType *parent = NULL, *res;
	MonoDomain *domain;
	MonoType *inflated;
	MonoClass *klass;


	MONO_ARCH_SAVE_REGS;

	g_assert (type->type.type->data.generic_class->is_dynamic);
	gclass = (MonoDynamicGenericClass *) type->type.type->data.generic_class;

	domain = mono_object_domain (type);
	klass = mono_class_from_mono_type (type->generic_type->type.type);

	if (!klass->generic_class && !klass->generic_container)
		return NULL;

	parent = type->generic_type->parent;

	if (!parent || (parent->type->type != MONO_TYPE_GENERICINST))
		return NULL;

	inflated = mono_class_inflate_generic_type (
		parent->type, mono_generic_class_get_context ((MonoGenericClass *) gclass));

	res = mono_type_get_object (domain, inflated);
	mono_metadata_free_type (inflated);
	return res;
}

static MonoArray*
ves_icall_MonoGenericClass_GetInterfaces (MonoReflectionGenericClass *type)
{
	static MonoClass *System_Reflection_MonoGenericClass;
	MonoGenericClass *gclass;
	MonoReflectionTypeBuilder *tb = NULL;
	MonoClass *klass = NULL;
	MonoDomain *domain;
	MonoArray *res;
	int icount, i;

	MONO_ARCH_SAVE_REGS;

	if (!System_Reflection_MonoGenericClass) {
		System_Reflection_MonoGenericClass = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "MonoGenericClass");
		g_assert (System_Reflection_MonoGenericClass);
	}

	domain = mono_object_domain (type);

	gclass = type->type.type->data.generic_class;
	g_assert (gclass->is_dynamic);

	tb = type->generic_type;
	icount = tb->interfaces ? mono_array_length (tb->interfaces) : 0;

	res = mono_array_new (domain, System_Reflection_MonoGenericClass, icount);

	for (i = 0; i < icount; i++) {
		MonoReflectionType *iface;
		MonoType *it;

		if (tb) {
			iface = mono_array_get (tb->interfaces, MonoReflectionType *, i);
			it = iface->type;
		} else
			it = &klass->interfaces [i]->byval_arg;

		it = mono_class_inflate_generic_type (it, mono_generic_class_get_context (gclass));

		iface = mono_type_get_object (domain, it);
		mono_array_setref (res, i, iface);
		mono_metadata_free_type (it);
	}

	return res;
}

static MonoReflectionMethod*
ves_icall_MonoGenericClass_GetCorrespondingInflatedMethod (MonoReflectionGenericClass *type, 
                                                           MonoReflectionMethod* generic)
{
	MonoGenericClass *gclass;
	MonoDynamicGenericClass *dgclass;
	MonoDomain *domain;
	int i;

	MONO_ARCH_SAVE_REGS;

	gclass = type->type.type->data.generic_class;
	g_assert (gclass->is_dynamic);

	dgclass = (MonoDynamicGenericClass *) gclass;

	domain = mono_object_domain (type);

	for (i = 0; i < dgclass->count_methods; i++)
		if (generic->method->token == dgclass->methods [i]->token)
                        return mono_method_get_object (domain, dgclass->methods [i], NULL);

	return NULL;
}

static MonoReflectionMethod*
ves_icall_MonoGenericClass_GetCorrespondingInflatedConstructor (MonoReflectionGenericClass *type, 
                                                                MonoReflectionMethod* generic)
{
	MonoGenericClass *gclass;
	MonoDynamicGenericClass *dgclass;
	MonoDomain *domain;
	int i;

	MONO_ARCH_SAVE_REGS;

	gclass = type->type.type->data.generic_class;
	g_assert (gclass->is_dynamic);

	dgclass = (MonoDynamicGenericClass *) gclass;

	domain = mono_object_domain (type);

	for (i = 0; i < dgclass->count_ctors; i++)
		if (generic->method->token == dgclass->ctors [i]->token)
                        return mono_method_get_object (domain, dgclass->ctors [i], NULL);

	return NULL;
}


static MonoReflectionField*
ves_icall_MonoGenericClass_GetCorrespondingInflatedField (MonoReflectionGenericClass *type, 
                                                          MonoString* generic_name)
{
	MonoGenericClass *gclass;
	MonoDynamicGenericClass *dgclass;
	MonoDomain *domain;
        MonoClass *refclass;
	char *utf8_name = mono_string_to_utf8 (generic_name);
	int i;

	MONO_ARCH_SAVE_REGS;

	gclass = type->type.type->data.generic_class;
	g_assert (gclass->is_dynamic);

	dgclass = (MonoDynamicGenericClass *) gclass;

	refclass = mono_class_from_mono_type (type->type.type);

	domain = mono_object_domain (type);

	for (i = 0; i < dgclass->count_fields; i++)
                if (strcmp (utf8_name, dgclass->fields [i].name) == 0) {
			g_free (utf8_name);
                        return mono_field_get_object (domain, refclass, &dgclass->fields [i]);
		}
	
	g_free (utf8_name);

	return NULL;
}


static MonoReflectionMethod*
ves_icall_MonoType_GetCorrespondingInflatedMethod (MonoReflectionType *type, 
                                                   MonoReflectionMethod* generic)
{
	MonoDomain *domain; 
	MonoClass *klass;
	MonoMethod *method;
	gpointer iter;
		
	MONO_ARCH_SAVE_REGS;

	domain = ((MonoObject *)type)->vtable->domain;

	klass = mono_class_from_mono_type (type->type);

	iter = NULL;
	while ((method = mono_class_get_methods (klass, &iter))) {
                if (method->token == generic->method->token)
                        return mono_method_get_object (domain, method, klass);
        }

        return NULL;
}

static MonoArray*
ves_icall_MonoGenericClass_GetMethods (MonoReflectionGenericClass *type,
				       MonoReflectionType *reflected_type)
{
	MonoGenericClass *gclass;
	MonoDynamicGenericClass *dgclass;
	MonoDomain *domain;
	MonoClass *refclass;
	MonoArray *res;
	int i;

	MONO_ARCH_SAVE_REGS;

	gclass = type->type.type->data.generic_class;
	g_assert (gclass->is_dynamic);
	dgclass = (MonoDynamicGenericClass *) gclass;

	refclass = mono_class_from_mono_type (reflected_type->type);

	domain = mono_object_domain (type);
	res = mono_array_new (domain, mono_defaults.method_info_class, dgclass->count_methods);

	for (i = 0; i < dgclass->count_methods; i++)
		mono_array_setref (res, i, mono_method_get_object (domain, dgclass->methods [i], refclass));

	return res;
}

static MonoArray*
ves_icall_MonoGenericClass_GetConstructors (MonoReflectionGenericClass *type,
					    MonoReflectionType *reflected_type)
{
	static MonoClass *System_Reflection_ConstructorInfo;
	MonoGenericClass *gclass;
	MonoDynamicGenericClass *dgclass;
	MonoDomain *domain;
	MonoClass *refclass;
	MonoArray *res;
	int i;

	MONO_ARCH_SAVE_REGS;

	if (!System_Reflection_ConstructorInfo)
		System_Reflection_ConstructorInfo = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "ConstructorInfo");

	gclass = type->type.type->data.generic_class;
	g_assert (gclass->is_dynamic);
	dgclass = (MonoDynamicGenericClass *) gclass;

	refclass = mono_class_from_mono_type (reflected_type->type);

	domain = mono_object_domain (type);
	res = mono_array_new (domain, System_Reflection_ConstructorInfo, dgclass->count_ctors);

	for (i = 0; i < dgclass->count_ctors; i++)
		mono_array_setref (res, i, mono_method_get_object (domain, dgclass->ctors [i], refclass));

	return res;
}

static MonoArray*
ves_icall_MonoGenericClass_GetFields (MonoReflectionGenericClass *type,
				      MonoReflectionType *reflected_type)
{
	MonoGenericClass *gclass;
	MonoDynamicGenericClass *dgclass;
	MonoDomain *domain;
	MonoClass *refclass;
	MonoArray *res;
	int i;

	MONO_ARCH_SAVE_REGS;

	gclass = type->type.type->data.generic_class;
	g_assert (gclass->is_dynamic);
	dgclass = (MonoDynamicGenericClass *) gclass;

	refclass = mono_class_from_mono_type (reflected_type->type);

	domain = mono_object_domain (type);
	res = mono_array_new (domain, mono_defaults.field_info_class, dgclass->count_fields);

	for (i = 0; i < dgclass->count_fields; i++)
		mono_array_setref (res, i, mono_field_get_object (domain, refclass, &dgclass->fields [i]));

	return res;
}

static MonoArray*
ves_icall_MonoGenericClass_GetProperties (MonoReflectionGenericClass *type,
					  MonoReflectionType *reflected_type)
{
	static MonoClass *System_Reflection_PropertyInfo;
	MonoGenericClass *gclass;
	MonoDynamicGenericClass *dgclass;
	MonoDomain *domain;
	MonoClass *refclass;
	MonoArray *res;
	int i;

	MONO_ARCH_SAVE_REGS;

	if (!System_Reflection_PropertyInfo)
		System_Reflection_PropertyInfo = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "PropertyInfo");

	gclass = type->type.type->data.generic_class;
	g_assert (gclass->is_dynamic);
	dgclass = (MonoDynamicGenericClass *) gclass;

	refclass = mono_class_from_mono_type (reflected_type->type);

	domain = mono_object_domain (type);
	res = mono_array_new (domain, System_Reflection_PropertyInfo, dgclass->count_properties);

	for (i = 0; i < dgclass->count_properties; i++)
		mono_array_setref (res, i, mono_property_get_object (domain, refclass, &dgclass->properties [i]));

	return res;
}

static MonoArray*
ves_icall_MonoGenericClass_GetEvents (MonoReflectionGenericClass *type,
				      MonoReflectionType *reflected_type)
{
	static MonoClass *System_Reflection_EventInfo;
	MonoGenericClass *gclass;
	MonoDynamicGenericClass *dgclass;
	MonoDomain *domain;
	MonoClass *refclass;
	MonoArray *res;
	int i;

	MONO_ARCH_SAVE_REGS;

	if (!System_Reflection_EventInfo)
		System_Reflection_EventInfo = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "EventInfo");

	gclass = type->type.type->data.generic_class;
	g_assert (gclass->is_dynamic);
	dgclass = (MonoDynamicGenericClass *) gclass;

	refclass = mono_class_from_mono_type (reflected_type->type);

	domain = mono_object_domain (type);
	res = mono_array_new (domain, System_Reflection_EventInfo, dgclass->count_events);

	for (i = 0; i < dgclass->count_events; i++)
		mono_array_setref (res, i, mono_event_get_object (domain, refclass, &dgclass->events [i]));

	return res;
}

static MonoReflectionMethod *
ves_icall_MonoType_get_DeclaringMethod (MonoReflectionType *type)
{
	MonoMethod *method;
	MonoClass *klass;

	MONO_ARCH_SAVE_REGS;

	if (type->type->byref || type->type->type != MONO_TYPE_MVAR)
		return NULL;

	method = type->type->data.generic_param->owner->owner.method;
	g_assert (method);
	klass = mono_class_from_mono_type (type->type);
	return mono_method_get_object (mono_object_domain (type), method, klass);
}

static MonoReflectionDllImportAttribute*
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

	if (!method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)
		return NULL;

	if (!DllImportAttributeClass) {
		DllImportAttributeClass = 
			mono_class_from_name (mono_defaults.corlib,
								  "System.Runtime.InteropServices", "DllImportAttribute");
		g_assert (DllImportAttributeClass);
	}
														
	if (method->klass->image->dynamic) {
		MonoReflectionMethodAux *method_aux = 
			g_hash_table_lookup (
									  ((MonoDynamicImage*)method->klass->image)->method_aux_hash, method);
		if (method_aux) {
			import = method_aux->dllentry;
			scope = method_aux->dll;
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

static MonoReflectionMethod *
ves_icall_MonoMethod_GetGenericMethodDefinition (MonoReflectionMethod *method)
{
	MonoMethodInflated *imethod;
	MonoMethod *result;

	MONO_ARCH_SAVE_REGS;

	if (method->method->is_generic)
		return method;

	if (!method->method->is_inflated)
		return NULL;

	imethod = (MonoMethodInflated *) method->method;

	result = imethod->declaring;
	/* Not a generic method.  */
	if (!result->is_generic)
		return NULL;

	if (method->method->klass->image->dynamic) {
		MonoDynamicImage *image = (MonoDynamicImage*)method->method->klass->image;
		MonoReflectionMethod *res;

		/*
		 * FIXME: Why is this stuff needed at all ? Why can't the code below work for
		 * the dynamic case as well ?
		 */
		mono_loader_lock ();
		res = mono_g_hash_table_lookup (image->generic_def_objects, imethod);
		mono_loader_unlock ();

		if (res)
			return res;
	}

	if (imethod->context.class_inst) {
		MonoClass *klass = ((MonoMethod *) imethod)->klass;
		result = mono_class_inflate_generic_method_full (result, klass, mono_class_get_context (klass));
	}

	return mono_method_get_object (mono_object_domain (method), result, NULL);
}

static gboolean
ves_icall_MonoMethod_get_IsGenericMethod (MonoReflectionMethod *method)
{
	MONO_ARCH_SAVE_REGS;

	return mono_method_signature (method->method)->generic_param_count != 0;
}

static gboolean
ves_icall_MonoMethod_get_IsGenericMethodDefinition (MonoReflectionMethod *method)
{
	MONO_ARCH_SAVE_REGS;

	return method->method->is_generic;
}

static MonoArray*
ves_icall_MonoMethod_GetGenericArguments (MonoReflectionMethod *method)
{
	MonoArray *res;
	MonoDomain *domain;
	int count, i;
	MONO_ARCH_SAVE_REGS;

	domain = mono_object_domain (method);

	if (method->method->is_inflated) {
		MonoGenericInst *inst = mono_method_get_context (method->method)->method_inst;

		if (inst) {
			count = inst->type_argc;
			res = mono_array_new (domain, mono_defaults.monotype_class, count);

			for (i = 0; i < count; i++)
				mono_array_setref (res, i, mono_type_get_object (domain, inst->type_argv [i]));

			return res;
		}
	}

	count = mono_method_signature (method->method)->generic_param_count;
	res = mono_array_new (domain, mono_defaults.monotype_class, count);

	for (i = 0; i < count; i++) {
		MonoGenericContainer *container = mono_method_get_generic_container (method->method);
		MonoGenericParam *param = &container->type_params [i];
		MonoClass *pklass = mono_class_from_generic_parameter (
			param, method->method->klass->image, TRUE);
		mono_array_setref (res, i,
				mono_type_get_object (domain, &pklass->byval_arg));
	}

	return res;
}

static void
ensure_reflection_security (void)
{
	MonoMethod *m = mono_method_get_last_managed ();

	while (m) {
		/*
		g_print ("method %s.%s.%s in image %s\n",
			m->klass->name_space, m->klass->name, m->name, m->klass->image->name);
		*/

		/* We stop at the first method which is not in
		   System.Reflection or which is not in a platform
		   image. */
		if (strcmp (m->klass->name_space, "System.Reflection") != 0 ||
				!mono_security_core_clr_is_platform_image (m->klass->image)) {
			/* If the method is transparent we throw an exception. */
			if (mono_security_core_clr_method_level (m, TRUE) == MONO_SECURITY_CORE_CLR_TRANSPARENT ) {
				MonoException *ex = mono_exception_from_name_msg (mono_defaults.corlib, "System", "MethodAccessException", "Reflection called from transparent code");

				mono_raise_exception (ex);
			}
			return;
		}

		mono_stack_walk_no_il (get_caller, &m);
	}
}

static MonoObject *
ves_icall_InternalInvoke (MonoReflectionMethod *method, MonoObject *this, MonoArray *params, MonoException **exc) 
{
	/* 
	 * Invoke from reflection is supposed to always be a virtual call (the API
	 * is stupid), mono_runtime_invoke_*() calls the provided method, allowing
	 * greater flexibility.
	 */
	MonoMethod *m = method->method;
	int pcount;
	void *obj = this;

	MONO_ARCH_SAVE_REGS;

	*exc = NULL;

	if (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR &&
			mono_security_core_clr_method_level (m, TRUE) == MONO_SECURITY_CORE_CLR_CRITICAL)
		ensure_reflection_security ();

	if (!(m->flags & METHOD_ATTRIBUTE_STATIC)) {
		if (this) {
			if (!mono_object_isinst (this, m->klass)) {
				*exc = mono_exception_from_name_msg (mono_defaults.corlib, "System.Reflection", "TargetException", "Object does not match target type.");
				return NULL;
			}
			m = mono_object_get_virtual_method (this, m);
			/* must pass the pointer to the value for valuetype methods */
			if (m->klass->valuetype)
				obj = mono_object_unbox (this);
		} else if (strcmp (m->name, ".ctor") && !m->wrapper_type) {
			*exc = mono_exception_from_name_msg (mono_defaults.corlib, "System.Reflection", "TargetException", "Non-static method requires a target.");
			return NULL;
		}
	}

	pcount = params? mono_array_length (params): 0;
	if (pcount != mono_method_signature (m)->param_count) {
		*exc = mono_exception_from_name (mono_defaults.corlib, "System.Reflection", "TargetParameterCountException");
		return NULL;
	}

	if ((m->klass->flags & TYPE_ATTRIBUTE_ABSTRACT) && !strcmp (m->name, ".ctor") && !this) {
		*exc = mono_exception_from_name_msg (mono_defaults.corlib, "System", "MethodAccessException", "Cannot invoke constructor of an abstract class.");
		return NULL;
	}

	if (m->klass->image->assembly->ref_only) {
		*exc = mono_get_exception_invalid_operation ("It is illegal to invoke a method on a type loaded using the ReflectionOnly api.");
		return NULL;
	}
	
	if (m->klass->rank && !strcmp (m->name, ".ctor")) {
		int i;
		mono_array_size_t *lengths;
		mono_array_size_t *lower_bounds;
		pcount = mono_array_length (params);
		lengths = alloca (sizeof (mono_array_size_t) * pcount);
		for (i = 0; i < pcount; ++i)
			lengths [i] = *(mono_array_size_t*) ((char*)mono_array_get (params, gpointer, i) + sizeof (MonoObject));

		if (m->klass->rank == pcount) {
			/* Only lengths provided. */
			lower_bounds = NULL;
		} else {
			g_assert (pcount == (m->klass->rank * 2));
			/* lower bounds are first. */
			lower_bounds = lengths;
			lengths += m->klass->rank;
		}

		return (MonoObject*)mono_array_new_full (mono_object_domain (params), m->klass, lengths, lower_bounds);
	}
	return mono_runtime_invoke_array (m, obj, params, NULL);
}

static MonoObject *
ves_icall_InternalExecute (MonoReflectionMethod *method, MonoObject *this, MonoArray *params, MonoArray **outArgs) 
{
	MonoDomain *domain = mono_object_domain (method); 
	MonoMethod *m = method->method;
	MonoMethodSignature *sig = mono_method_signature (m);
	MonoArray *out_args;
	MonoObject *result;
	int i, j, outarg_count = 0;

	MONO_ARCH_SAVE_REGS;

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
					*outArgs = out_args;
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
						memcpy ((char *)this + field->offset, 
							((char *)val) + sizeof (MonoObject), size);
					} else 
						*(MonoObject**)((char *)this + field->offset) = val;
				
					out_args = mono_array_new (domain, mono_defaults.object_class, 0);
					*outArgs = out_args;

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

	*outArgs = out_args;

	return result;
}

static guint64
read_enum_value (char *mem, int type)
{
	switch (type) {
	case MONO_TYPE_U1:
		return *(guint8*)mem;
	case MONO_TYPE_I1:
		return *(gint8*)mem;
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

static MonoObject *
ves_icall_System_Enum_ToObject (MonoReflectionType *enumType, MonoObject *value)
{
	MonoDomain *domain; 
	MonoClass *enumc, *objc;
	MonoObject *res;
	guint64 val;
	
	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (enumType);
	MONO_CHECK_ARG_NULL (value);

	domain = mono_object_domain (enumType); 
	enumc = mono_class_from_mono_type (enumType->type);
	objc = value->vtable->klass;

	if (!enumc->enumtype)
		mono_raise_exception (mono_get_exception_argument ("enumType", "Type provided must be an Enum."));
	if (!((objc->enumtype) || (objc->byval_arg.type >= MONO_TYPE_I1 && objc->byval_arg.type <= MONO_TYPE_U8)))
		mono_raise_exception (mono_get_exception_argument ("value", "The value passed in must be an enum base or an underlying type for an enum, such as an Int32."));

	res = mono_object_new (domain, enumc);
	val = read_enum_value ((char *)value + sizeof (MonoObject), objc->enumtype? objc->enum_basetype->type: objc->byval_arg.type);
	write_enum_value ((char *)res + sizeof (MonoObject), enumc->enum_basetype->type, val);

	return res;
}

static MonoObject *
ves_icall_System_Enum_get_value (MonoObject *this)
{
	MonoObject *res;
	MonoClass *enumc;
	gpointer dst;
	gpointer src;
	int size;

	MONO_ARCH_SAVE_REGS;

	if (!this)
		return NULL;

	g_assert (this->vtable->klass->enumtype);
	
	enumc = mono_class_from_mono_type (this->vtable->klass->enum_basetype);
	res = mono_object_new (mono_object_domain (this), enumc);
	dst = (char *)res + sizeof (MonoObject);
	src = (char *)this + sizeof (MonoObject);
	size = mono_class_value_size (enumc, NULL);

	memcpy (dst, src, size);

	return res;
}

static void
ves_icall_get_enum_info (MonoReflectionType *type, MonoEnumInfo *info)
{
	MonoDomain *domain = mono_object_domain (type); 
	MonoClass *enumc = mono_class_from_mono_type (type->type);
	guint j = 0, nvalues, crow;
	gpointer iter;
	MonoClassField *field;

	MONO_ARCH_SAVE_REGS;

	info->utype = mono_type_get_object (domain, enumc->enum_basetype);
	nvalues = mono_class_num_fields (enumc) ? mono_class_num_fields (enumc) - 1 : 0;
	info->names = mono_array_new (domain, mono_defaults.string_class, nvalues);
	info->values = mono_array_new (domain, enumc, nvalues);
	
	crow = -1;
	iter = NULL;
	while ((field = mono_class_get_fields (enumc, &iter))) {
		const char *p;
		int len;
		
		if (strcmp ("value__", field->name) == 0)
			continue;
		if (mono_field_is_deleted (field))
			continue;
		mono_array_setref (info->names, j, mono_string_new (domain, field->name));

		if (!field->data) {
			crow = mono_metadata_get_constant_index (enumc->image, mono_class_get_field_token (field), crow + 1);
			field->def_type = mono_metadata_decode_row_col (&enumc->image->tables [MONO_TABLE_CONSTANT], crow-1, MONO_CONSTANT_TYPE);
			crow = mono_metadata_decode_row_col (&enumc->image->tables [MONO_TABLE_CONSTANT], crow-1, MONO_CONSTANT_VALUE);
			field->data = (gpointer)mono_metadata_blob_heap (enumc->image, crow);
		}

		p = field->data;
		len = mono_metadata_decode_blob_size (p, &p);
		switch (enumc->enum_basetype->type) {
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
			g_error ("Implement type 0x%02x in get_enum_info", enumc->enum_basetype->type);
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

static MonoReflectionField *
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

	MONO_ARCH_SAVE_REGS;

	if (!name)
		mono_raise_exception (mono_get_exception_argument_null ("name"));
	if (type->type->byref)
		return NULL;

	compare_func = (bflags & BFLAGS_IgnoreCase) ? g_strcasecmp : strcmp;

handle_parent:
	if (klass->exception_type != MONO_EXCEPTION_NONE)
		mono_raise_exception (mono_class_get_exception_for_failure (klass));

	iter = NULL;
	while ((field = mono_class_get_fields (klass, &iter))) {
		match = 0;

		if (field->type == NULL)
			continue;
		if (mono_field_is_deleted (field))
			continue;
		if ((field->type->attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) == FIELD_ATTRIBUTE_PUBLIC) {
			if (bflags & BFLAGS_Public)
				match++;
		} else if ((klass == startklass) || (field->type->attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) != FIELD_ATTRIBUTE_PRIVATE) {
			if (bflags & BFLAGS_NonPublic) {
				match++;
			}
		}
		if (!match)
			continue;
		match = 0;
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC) {
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

		if (compare_func (field->name, utf8_name)) {
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

static MonoArray*
ves_icall_Type_GetFields_internal (MonoReflectionType *type, guint32 bflags, MonoReflectionType *reftype)
{
	MonoDomain *domain; 
	MonoClass *startklass, *klass, *refklass;
	MonoArray *res;
	MonoObject *member;
	int i, len, match;
	gpointer iter;
	MonoClassField *field;

	MONO_ARCH_SAVE_REGS;

	domain = ((MonoObject *)type)->vtable->domain;
	if (type->type->byref)
		return mono_array_new (domain, mono_defaults.field_info_class, 0);
	klass = startklass = mono_class_from_mono_type (type->type);
	refklass = mono_class_from_mono_type (reftype->type);

	i = 0;
	len = 2;
	res = mono_array_new (domain, mono_defaults.field_info_class, len);
handle_parent:	
	if (klass->exception_type != MONO_EXCEPTION_NONE)
		mono_raise_exception (mono_class_get_exception_for_failure (klass));

	iter = NULL;
	while ((field = mono_class_get_fields (klass, &iter))) {
		match = 0;
		if (mono_field_is_deleted (field))
			continue;
		if ((field->type->attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) == FIELD_ATTRIBUTE_PUBLIC) {
			if (bflags & BFLAGS_Public)
				match++;
		} else if ((klass == startklass) || (field->type->attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK) != FIELD_ATTRIBUTE_PRIVATE) {
			if (bflags & BFLAGS_NonPublic) {
				match++;
			}
		}
		if (!match)
			continue;
		match = 0;
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC) {
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
		if (i >= len) {
			MonoArray *new_res = mono_array_new (domain, mono_defaults.field_info_class, len * 2);
			mono_array_memcpy_refs (new_res, 0, res, 0, len);
			len *= 2;
			res = new_res;
		}
		mono_array_setref (res, i, member);
		++i;
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;
	if (i != len) {
		MonoArray *new_res = mono_array_new (domain, mono_defaults.field_info_class, i);
		mono_array_memcpy_refs (new_res, 0, res, 0, i);
		res = new_res;
		/*
		 * Better solution for the new GC.
		 * res->max_length = i;
		 */
	}
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

static MonoArray*
ves_icall_Type_GetMethodsByName (MonoReflectionType *type, MonoString *name, guint32 bflags, MonoBoolean ignore_case, MonoReflectionType *reftype)
{
	MonoDomain *domain; 
	MonoClass *startklass, *klass, *refklass;
	MonoArray *res;
	MonoMethod *method;
	gpointer iter;
	MonoObject *member;
	int i, len, match, nslots;
	guint32 method_slots_default [8];
	guint32 *method_slots;
	gchar *mname = NULL;
	int (*compare_func) (const char *s1, const char *s2) = NULL;
		
	MONO_ARCH_SAVE_REGS;

	domain = ((MonoObject *)type)->vtable->domain;
	if (type->type->byref)
		return mono_array_new (domain, mono_defaults.method_info_class, 0);
	klass = startklass = mono_class_from_mono_type (type->type);
	refklass = mono_class_from_mono_type (reftype->type);
	len = 0;
	if (name != NULL) {
		mname = mono_string_to_utf8 (name);
		compare_func = (ignore_case) ? g_strcasecmp : strcmp;
	}

	mono_class_setup_vtable (klass);

	if (is_generic_parameter (type->type))
		nslots = klass->parent->vtable_size;
	else
		nslots = MONO_CLASS_IS_INTERFACE (klass) ? mono_class_num_methods (klass) : klass->vtable_size;
	if (nslots >= sizeof (method_slots_default) * 8) {
		method_slots = g_new0 (guint32, nslots / 32 + 1);
	} else {
		method_slots = method_slots_default;
		memset (method_slots, 0, sizeof (method_slots_default));
	}
	i = 0;
	len = 1;
	res = mono_array_new (domain, mono_defaults.method_info_class, len);
handle_parent:
	mono_class_setup_vtable (klass);
	if (klass->exception_type != MONO_EXCEPTION_NONE)
		mono_raise_exception (mono_class_get_exception_for_failure (klass));

	iter = NULL;
	while ((method = mono_class_get_methods (klass, &iter))) {
		match = 0;
		if (method->name [0] == '.' && (strcmp (method->name, ".ctor") == 0 || strcmp (method->name, ".cctor") == 0))
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
			if (compare_func (mname, method->name))
				continue;
		}
		
		match = 0;
		if (method->slot != -1) {
			g_assert (method->slot < nslots);
			if (method_slots [method->slot >> 5] & (1 << (method->slot & 0x1f)))
				continue;
			method_slots [method->slot >> 5] |= 1 << (method->slot & 0x1f);
		}
		
		member = (MonoObject*)mono_method_get_object (domain, method, refklass);
		
		if (i >= len) {
			MonoArray *new_res = mono_array_new (domain, mono_defaults.method_info_class, len * 2);
			mono_array_memcpy_refs (new_res, 0, res, 0, len);
			len *= 2;
			res = new_res;
		}
		mono_array_setref (res, i, member);
		++i;
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;

	g_free (mname);
	if (method_slots != method_slots_default)
		g_free (method_slots);
	if (i != len) {
		MonoArray *new_res = mono_array_new (domain, mono_defaults.method_info_class, i);
		mono_array_memcpy_refs (new_res, 0, res, 0, i);
		res = new_res;
		/*
		 * Better solution for the new GC.
		 * res->max_length = i;
		 */
	}
	return res;
}

static MonoArray*
ves_icall_Type_GetConstructors_internal (MonoReflectionType *type, guint32 bflags, MonoReflectionType *reftype)
{
	MonoDomain *domain; 
	static MonoClass *System_Reflection_ConstructorInfo;
	MonoClass *startklass, *klass, *refklass;
	MonoArray *res;
	MonoMethod *method;
	MonoObject *member;
	int i, len, match;
	gpointer iter = NULL;
	
	MONO_ARCH_SAVE_REGS;

	domain = ((MonoObject *)type)->vtable->domain;
	if (type->type->byref)
		return mono_array_new (domain, mono_defaults.method_info_class, 0);
	klass = startklass = mono_class_from_mono_type (type->type);
	refklass = mono_class_from_mono_type (reftype->type);

	if (klass->exception_type != MONO_EXCEPTION_NONE)
		mono_raise_exception (mono_class_get_exception_for_failure (klass));

	if (!System_Reflection_ConstructorInfo)
		System_Reflection_ConstructorInfo = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "ConstructorInfo");

	i = 0;
	len = 2;
	res = mono_array_new (domain, System_Reflection_ConstructorInfo, len);
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

		if (i >= len) {
			MonoArray *new_res = mono_array_new (domain, System_Reflection_ConstructorInfo, len * 2);
			mono_array_memcpy_refs (new_res, 0, res, 0, len);
			len *= 2;
			res = new_res;
		}
		mono_array_setref (res, i, member);
		++i;
	}
	if (i != len) {
		MonoArray *new_res = mono_array_new (domain, System_Reflection_ConstructorInfo, i);
		mono_array_memcpy_refs (new_res, 0, res, 0, i);
		res = new_res;
		/*
		 * Better solution for the new GC.
		 * res->max_length = i;
		 */
	}
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

static MonoArray*
ves_icall_Type_GetPropertiesByName (MonoReflectionType *type, MonoString *name, guint32 bflags, MonoBoolean ignore_case, MonoReflectionType *reftype)
{
	MonoDomain *domain; 
	static MonoClass *System_Reflection_PropertyInfo;
	MonoClass *startklass, *klass;
	MonoArray *res;
	MonoMethod *method;
	MonoProperty *prop;
	int i, match;
	int len = 0;
	guint32 flags;
	gchar *propname = NULL;
	int (*compare_func) (const char *s1, const char *s2) = NULL;
	gpointer iter;
	GHashTable *properties;

	MONO_ARCH_SAVE_REGS;

	if (!System_Reflection_PropertyInfo)
		System_Reflection_PropertyInfo = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "PropertyInfo");

	domain = ((MonoObject *)type)->vtable->domain;
	if (type->type->byref)
		return mono_array_new (domain, System_Reflection_PropertyInfo, 0);
	klass = startklass = mono_class_from_mono_type (type->type);
	if (name != NULL) {
		propname = mono_string_to_utf8 (name);
		compare_func = (ignore_case) ? g_strcasecmp : strcmp;
	}

	mono_class_setup_vtable (klass);

	properties = g_hash_table_new (property_hash, (GEqualFunc)property_equal);
	i = 0;
	len = 2;
	res = mono_array_new (domain, System_Reflection_PropertyInfo, len);
handle_parent:
	mono_class_setup_vtable (klass);
	if (klass->exception_type != MONO_EXCEPTION_NONE) {
		g_hash_table_destroy (properties);
		if (name != NULL)
			g_free (propname);
		mono_raise_exception (mono_class_get_exception_for_failure (klass));
	}

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

		if (i >= len) {
			MonoArray *new_res = mono_array_new (domain, System_Reflection_PropertyInfo, len * 2);
			mono_array_memcpy_refs (new_res, 0, res, 0, len);
			len *= 2;
			res = new_res;
		}
		mono_array_setref (res, i, mono_property_get_object (domain, startklass, prop));
		++i;
		
		g_hash_table_insert (properties, prop, prop);
	}
	if ((!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent)))
		goto handle_parent;

	g_hash_table_destroy (properties);
	g_free (propname);
	if (i != len) {
		MonoArray *new_res = mono_array_new (domain, System_Reflection_PropertyInfo, i);
		mono_array_memcpy_refs (new_res, 0, res, 0, i);
		res = new_res;
		/*
		 * Better solution for the new GC.
		 * res->max_length = i;
		 */
	}
	return res;
}

static MonoReflectionEvent *
ves_icall_MonoType_GetEvent (MonoReflectionType *type, MonoString *name, guint32 bflags)
{
	MonoDomain *domain;
	MonoClass *klass, *startklass;
	gpointer iter;
	MonoEvent *event;
	MonoMethod *method;
	gchar *event_name;

	MONO_ARCH_SAVE_REGS;

	event_name = mono_string_to_utf8 (name);
	if (type->type->byref)
		return NULL;
	klass = startklass = mono_class_from_mono_type (type->type);
	domain = mono_object_domain (type);

handle_parent:	
	if (klass->exception_type != MONO_EXCEPTION_NONE)
		mono_raise_exception (mono_class_get_exception_for_failure (klass));

	iter = NULL;
	while ((event = mono_class_get_events (klass, &iter))) {
		if (strcmp (event->name, event_name))
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
		}
		else
			if (!(bflags & BFLAGS_NonPublic))
				continue;

		if (method->flags & METHOD_ATTRIBUTE_STATIC) {
			if (!(bflags & BFLAGS_Static))
				continue;
			if (!(bflags & BFLAGS_FlattenHierarchy) && (klass != startklass))
				continue;
		} else {
			if (!(bflags & BFLAGS_Instance))
				continue;
		}

		g_free (event_name);
		return mono_event_get_object (domain, startklass, event);
	}

	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;

	g_free (event_name);
	return NULL;
}

static MonoArray*
ves_icall_Type_GetEvents_internal (MonoReflectionType *type, guint32 bflags, MonoReflectionType *reftype)
{
	MonoDomain *domain; 
	static MonoClass *System_Reflection_EventInfo;
	MonoClass *startklass, *klass;
	MonoArray *res;
	MonoMethod *method;
	MonoEvent *event;
	int i, len, match;
	gpointer iter;

	MONO_ARCH_SAVE_REGS;

	if (!System_Reflection_EventInfo)
		System_Reflection_EventInfo = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "EventInfo");

	domain = mono_object_domain (type);
	if (type->type->byref)
		return mono_array_new (domain, System_Reflection_EventInfo, 0);
	klass = startklass = mono_class_from_mono_type (type->type);

	i = 0;
	len = 2;
	res = mono_array_new (domain, System_Reflection_EventInfo, len);
handle_parent:	
	if (klass->exception_type != MONO_EXCEPTION_NONE)
		mono_raise_exception (mono_class_get_exception_for_failure (klass));

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
		match = 0;
		if (i >= len) {
			MonoArray *new_res = mono_array_new (domain, System_Reflection_EventInfo, len * 2);
			mono_array_memcpy_refs (new_res, 0, res, 0, len);
			len *= 2;
			res = new_res;
		}
		mono_array_setref (res, i, mono_event_get_object (domain, startklass, event));
		++i;
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;
	if (i != len) {
		MonoArray *new_res = mono_array_new (domain, System_Reflection_EventInfo, i);
		mono_array_memcpy_refs (new_res, 0, res, 0, i);
		res = new_res;
		/*
		 * Better solution for the new GC.
		 * res->max_length = i;
		 */
	}
	return res;
}

static MonoReflectionType *
ves_icall_Type_GetNestedType (MonoReflectionType *type, MonoString *name, guint32 bflags)
{
	MonoDomain *domain; 
	MonoClass *klass;
	MonoClass *nested;
	GList *tmpn;
	char *str;
	
	MONO_ARCH_SAVE_REGS;

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

	for (tmpn = klass->nested_classes; tmpn; tmpn = tmpn->next) {
		int match = 0;
		nested = tmpn->data;
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

static MonoArray*
ves_icall_Type_GetNestedTypes (MonoReflectionType *type, guint32 bflags)
{
	MonoDomain *domain; 
	GList *tmpn;
	MonoClass *klass;
	MonoArray *res;
	MonoObject *member;
	int i, len, match;
	MonoClass *nested;

	MONO_ARCH_SAVE_REGS;

	domain = ((MonoObject *)type)->vtable->domain;
	if (type->type->byref)
		return mono_array_new (domain, mono_defaults.monotype_class, 0);
	klass = mono_class_from_mono_type (type->type);
	if (klass->exception_type != MONO_EXCEPTION_NONE)
		mono_raise_exception (mono_class_get_exception_for_failure (klass));

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

	i = 0;
	len = 1;
	res = mono_array_new (domain, mono_defaults.monotype_class, len);
	for (tmpn = klass->nested_classes; tmpn; tmpn = tmpn->next) {
		match = 0;
		nested = tmpn->data;
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
		if (i >= len) {
			MonoArray *new_res = mono_array_new (domain, mono_defaults.monotype_class, len * 2);
			mono_array_memcpy_refs (new_res, 0, res, 0, len);
			len *= 2;
			res = new_res;
		}
		mono_array_setref (res, i, member);
		++i;
	}
	if (i != len) {
		MonoArray *new_res = mono_array_new (domain, mono_defaults.monotype_class, i);
		mono_array_memcpy_refs (new_res, 0, res, 0, i);
		res = new_res;
		/*
		 * Better solution for the new GC.
		 * res->max_length = i;
		 */
	}
	return res;
}

static MonoReflectionType*
ves_icall_System_Reflection_Assembly_InternalGetType (MonoReflectionAssembly *assembly, MonoReflectionModule *module, MonoString *name, MonoBoolean throwOnError, MonoBoolean ignoreCase)
{
	gchar *str;
	MonoType *type = NULL;
	MonoTypeNameParse info;
	gboolean type_resolve;

	MONO_ARCH_SAVE_REGS;

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
		if (assembly->assembly->dynamic) {
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

		mono_loader_clear_error ();

		if (e != NULL)
			mono_raise_exception (e);

		return NULL;
	}

	if (type->type == MONO_TYPE_CLASS) {
		MonoClass *klass = mono_type_get_class (type);

		if (mono_is_security_manager_active () && !klass->exception_type)
			/* Some security problems are detected during generic vtable construction */
			mono_class_setup_vtable (klass);
		/* need to report exceptions ? */
		if (throwOnError && klass->exception_type) {
			/* report SecurityException (or others) that occured when loading the assembly */
			MonoException *exc = mono_class_get_exception_for_failure (klass);
			mono_loader_clear_error ();
			mono_raise_exception (exc);
		} else if (klass->exception_type == MONO_EXCEPTION_SECURITY_INHERITANCEDEMAND) {
			return NULL;
		}
	}

	/* g_print ("got it\n"); */
	return mono_type_get_object (mono_object_domain (assembly), type);
}

static MonoString *
ves_icall_System_Reflection_Assembly_get_code_base (MonoReflectionAssembly *assembly, MonoBoolean escaped)
{
	MonoDomain *domain = mono_object_domain (assembly); 
	MonoAssembly *mass = assembly->assembly;
	MonoString *res = NULL;
	gchar *uri;
	gchar *absolute;
	
	MONO_ARCH_SAVE_REGS;

	if (g_path_is_absolute (mass->image->name))
		absolute = g_strdup (mass->image->name);
	else
		absolute = g_build_filename (mass->basedir, mass->image->name, NULL);
#if PLATFORM_WIN32
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
		uri = g_strconcat ("file://", absolute, NULL);
	}

	if (uri) {
		res = mono_string_new (domain, uri);
		g_free (uri);
	}
	g_free (absolute);
	return res;
}

static MonoBoolean
ves_icall_System_Reflection_Assembly_get_global_assembly_cache (MonoReflectionAssembly *assembly)
{
	MonoAssembly *mass = assembly->assembly;

	MONO_ARCH_SAVE_REGS;

	return mass->in_gac;
}

static MonoReflectionAssembly*
ves_icall_System_Reflection_Assembly_load_with_partial_name (MonoString *mname, MonoObject *evidence)
{
	gchar *name;
	MonoAssembly *res;
	MonoImageOpenStatus status;
	
	MONO_ARCH_SAVE_REGS;

	name = mono_string_to_utf8 (mname);
	res = mono_assembly_load_with_partial_name (name, &status);

	g_free (name);

	if (res == NULL)
		return NULL;
	return mono_assembly_get_object (mono_domain_get (), res);
}

static MonoString *
ves_icall_System_Reflection_Assembly_get_location (MonoReflectionAssembly *assembly)
{
	MonoDomain *domain = mono_object_domain (assembly); 
	MonoString *res;

	MONO_ARCH_SAVE_REGS;

	res = mono_string_new (domain, mono_image_get_filename (assembly->assembly->image));

	return res;
}

static MonoBoolean
ves_icall_System_Reflection_Assembly_get_ReflectionOnly (MonoReflectionAssembly *assembly)
{
	MONO_ARCH_SAVE_REGS;

	return assembly->assembly->ref_only;
}

static MonoString *
ves_icall_System_Reflection_Assembly_InternalImageRuntimeVersion (MonoReflectionAssembly *assembly)
{
	MonoDomain *domain = mono_object_domain (assembly); 

	MONO_ARCH_SAVE_REGS;

	return mono_string_new (domain, assembly->assembly->image->version);
}

static MonoReflectionMethod*
ves_icall_System_Reflection_Assembly_get_EntryPoint (MonoReflectionAssembly *assembly) 
{
	guint32 token = mono_image_get_entry_point (assembly->assembly->image);

	MONO_ARCH_SAVE_REGS;

	if (!token)
		return NULL;
	return mono_method_get_object (mono_object_domain (assembly), mono_get_method (assembly->assembly->image, token, NULL), NULL);
}

static MonoReflectionModule*
ves_icall_System_Reflection_Assembly_GetManifestModuleInternal (MonoReflectionAssembly *assembly) 
{
	return mono_module_get_object (mono_object_domain (assembly), assembly->assembly->image);
}

static MonoArray*
ves_icall_System_Reflection_Assembly_GetManifestResourceNames (MonoReflectionAssembly *assembly) 
{
	MonoTableInfo *table = &assembly->assembly->image->tables [MONO_TABLE_MANIFESTRESOURCE];
	MonoArray *result = mono_array_new (mono_object_domain (assembly), mono_defaults.string_class, table->rows);
	int i;
	const char *val;

	MONO_ARCH_SAVE_REGS;

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

static MonoArray*
ves_icall_System_Reflection_Assembly_GetReferencedAssemblies (MonoReflectionAssembly *assembly) 
{
	static MonoClass *System_Reflection_AssemblyName;
	MonoArray *result;
	MonoDomain *domain = mono_object_domain (assembly);
	int i, count = 0;
	static MonoMethod *create_culture = NULL;
	MonoImage *image = assembly->assembly->image;
	MonoTableInfo *t;

	MONO_ARCH_SAVE_REGS;

	if (!System_Reflection_AssemblyName)
		System_Reflection_AssemblyName = mono_class_from_name (
			mono_defaults.corlib, "System.Reflection", "AssemblyName");

	t = &assembly->assembly->image->tables [MONO_TABLE_ASSEMBLYREF];
	count = t->rows;

	result = mono_array_new (domain, System_Reflection_AssemblyName, count);

	if (count > 0) {
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
			gboolean assembly_ref = TRUE;
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

typedef struct {
	MonoArray *res;
	int idx;
} NameSpaceInfo;

static void
foreach_namespace (const char* key, gconstpointer val, NameSpaceInfo *info)
{
	MonoString *name = mono_string_new (mono_object_domain (info->res), key);

	mono_array_setref (info->res, info->idx, name);
	info->idx++;
}

static MonoArray*
ves_icall_System_Reflection_Assembly_GetNamespaces (MonoReflectionAssembly *assembly) 
{
	MonoImage *img = assembly->assembly->image;
	MonoArray *res;
	NameSpaceInfo info;

	MONO_ARCH_SAVE_REGS;

	if (!img->name_cache)
		mono_image_init_name_cache (img);

	res = mono_array_new (mono_object_domain (assembly), mono_defaults.string_class, g_hash_table_size (img->name_cache));
	info.res = res;
	info.idx = 0;
	g_hash_table_foreach (img->name_cache, (GHFunc)foreach_namespace, &info);

	return res;
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

static void *
ves_icall_System_Reflection_Assembly_GetManifestResourceInternal (MonoReflectionAssembly *assembly, MonoString *name, gint32 *size, MonoReflectionModule **ref_module) 
{
	char *n = mono_string_to_utf8 (name);
	MonoTableInfo *table = &assembly->assembly->image->tables [MONO_TABLE_MANIFESTRESOURCE];
	guint32 i;
	guint32 cols [MONO_MANIFEST_SIZE];
	guint32 impl, file_idx;
	const char *val;
	MonoImage *module;

	MONO_ARCH_SAVE_REGS;

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

	*ref_module = mono_module_get_object (mono_domain_get (), module);

	return (void*)mono_image_get_resource (module, cols [MONO_MANIFEST_OFFSET], (guint32*)size);
}

static gboolean
ves_icall_System_Reflection_Assembly_GetManifestResourceInfoInternal (MonoReflectionAssembly *assembly, MonoString *name, MonoManifestResourceInfo *info)
{
	MonoTableInfo *table = &assembly->assembly->image->tables [MONO_TABLE_MANIFESTRESOURCE];
	int i;
	guint32 cols [MONO_MANIFEST_SIZE];
	guint32 file_cols [MONO_FILE_SIZE];
	const char *val;
	char *n;

	MONO_ARCH_SAVE_REGS;

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

static MonoObject*
ves_icall_System_Reflection_Assembly_GetFilesInternal (MonoReflectionAssembly *assembly, MonoString *name, MonoBoolean resource_modules) 
{
	MonoTableInfo *table = &assembly->assembly->image->tables [MONO_TABLE_FILE];
	MonoArray *result = NULL;
	int i, count;
	const char *val;
	char *n;

	MONO_ARCH_SAVE_REGS;

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

static MonoArray*
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
	g_assert (!assembly->assembly->dynamic);

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

static MonoReflectionMethod*
ves_icall_GetCurrentMethod (void) 
{
	MonoMethod *m = mono_method_get_last_managed ();

	MONO_ARCH_SAVE_REGS;

	return mono_method_get_object (mono_domain_get (), m, NULL);
}

static MonoReflectionMethod*
ves_icall_System_Reflection_MethodBase_GetMethodFromHandleInternalType (MonoMethod *method, MonoType *type)
{
	/* FIXME check that method belongs to klass or a parent */
	MonoClass *klass;
	if (type)
		klass = mono_class_from_mono_type (type);
	else
		klass = method->klass;
	return mono_method_get_object (mono_domain_get (), method, klass);
}

static MonoReflectionMethod*
ves_icall_System_Reflection_MethodBase_GetMethodFromHandleInternal (MonoMethod *method)
{
	return mono_method_get_object (mono_domain_get (), method, NULL);
}

static MonoReflectionMethodBody*
ves_icall_System_Reflection_MethodBase_GetMethodBodyInternal (MonoMethod *method)
{
	return mono_method_body_get_object (mono_domain_get (), method);
}

static MonoReflectionAssembly*
ves_icall_System_Reflection_Assembly_GetExecutingAssembly (void)
{
	MonoMethod *m = mono_method_get_last_managed ();

	MONO_ARCH_SAVE_REGS;

	return mono_assembly_get_object (mono_domain_get (), m->klass->image->assembly);
}


static MonoReflectionAssembly*
ves_icall_System_Reflection_Assembly_GetEntryAssembly (void)
{
	MonoDomain* domain = mono_domain_get ();

	MONO_ARCH_SAVE_REGS;

	if (!domain->entry_assembly)
		return NULL;

	return mono_assembly_get_object (domain, domain->entry_assembly);
}

static MonoReflectionAssembly*
ves_icall_System_Reflection_Assembly_GetCallingAssembly (void)
{
	MonoMethod *m = mono_method_get_last_managed ();
	MonoMethod *dest = m;

	MONO_ARCH_SAVE_REGS;

	mono_stack_walk_no_il (get_caller, &dest);
	if (!dest)
		dest = m;
	return mono_assembly_get_object (mono_domain_get (), dest->klass->image->assembly);
}

static MonoString *
ves_icall_System_MonoType_getFullName (MonoReflectionType *object, gboolean full_name,
				       gboolean assembly_qualified)
{
	MonoDomain *domain = mono_object_domain (object); 
	MonoTypeNameFormat format;
	MonoString *res;
	gchar *name;

	MONO_ARCH_SAVE_REGS;
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

static void
fill_reflection_assembly_name (MonoDomain *domain, MonoReflectionAssemblyName *aname, MonoAssemblyName *name, const char *absolute, gboolean by_default_version, gboolean default_publickey, gboolean default_token)
{
	static MonoMethod *create_culture = NULL;
	gpointer args [2];
	guint32 pkey_len;
	const char *pkey_ptr;
	gchar *codebase;
	gboolean assembly_ref = FALSE;

	MONO_ARCH_SAVE_REGS;

	MONO_OBJECT_SETREF (aname, name, mono_string_new (domain, name->name));
	aname->major = name->major;
	aname->minor = name->minor;
	aname->build = name->build;
	aname->flags = name->flags;
	aname->revision = name->revision;
	aname->hashalg = name->hash_alg;
	aname->versioncompat = 1; /* SameMachine (default) */

	if (by_default_version)
		MONO_OBJECT_SETREF (aname, version, create_version (domain, name->major, name->minor, name->build, name->revision));
	
	codebase = g_filename_to_uri (absolute, NULL, NULL);
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

static MonoString *
ves_icall_System_Reflection_Assembly_get_fullName (MonoReflectionAssembly *assembly)
{
	MonoDomain *domain = mono_object_domain (assembly); 
	MonoAssembly *mass = assembly->assembly;
	MonoString *res;
	gchar *name;

	name = g_strdup_printf (
		"%s, Version=%d.%d.%d.%d, Culture=%s, PublicKeyToken=%s%s",
		mass->aname.name,
		mass->aname.major, mass->aname.minor, mass->aname.build, mass->aname.revision,
		mass->aname.culture && *mass->aname.culture? mass->aname.culture: "neutral",
		mass->aname.public_key_token [0] ? (char *)mass->aname.public_key_token : "null",
		(mass->aname.flags & ASSEMBLYREF_RETARGETABLE_FLAG) ? ", Retargetable=Yes" : "");

	res = mono_string_new (domain, name);
	g_free (name);

	return res;
}

static void
ves_icall_System_Reflection_Assembly_FillName (MonoReflectionAssembly *assembly, MonoReflectionAssemblyName *aname)
{
	gchar *absolute;
	MonoAssembly *mass = assembly->assembly;

	MONO_ARCH_SAVE_REGS;

	if (g_path_is_absolute (mass->image->name)) {
		fill_reflection_assembly_name (mono_object_domain (assembly),
			aname, &mass->aname, mass->image->name, TRUE,
			TRUE, mono_get_runtime_info ()->framework_version [0] >= '2');
		return;
	}
	absolute = g_build_filename (mass->basedir, mass->image->name, NULL);

	fill_reflection_assembly_name (mono_object_domain (assembly),
		aname, &mass->aname, absolute, TRUE, TRUE,
		mono_get_runtime_info ()->framework_version [0] >= '2');

	g_free (absolute);
}

static void
ves_icall_System_Reflection_Assembly_InternalGetAssemblyName (MonoString *fname, MonoReflectionAssemblyName *aname)
{
	char *filename;
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	gboolean res;
	MonoImage *image;
	MonoAssemblyName name;

	MONO_ARCH_SAVE_REGS;

	filename = mono_string_to_utf8 (fname);

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
		TRUE, mono_get_runtime_info ()->framework_version [0] == '1',
		mono_get_runtime_info ()->framework_version [0] >= '2');

	g_free (filename);
	mono_image_close (image);
}

static MonoBoolean
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

static MonoArray*
mono_module_get_types (MonoDomain *domain, MonoImage *image, MonoBoolean exportedOnly)
{
	MonoArray *res;
	MonoClass *klass;
	MonoTableInfo *tdef = &image->tables [MONO_TABLE_TYPEDEF];
	int i, count;
	guint32 attrs, visibility;

	/* we start the count from 1 because we skip the special type <Module> */
	if (exportedOnly) {
		count = 0;
		for (i = 1; i < tdef->rows; ++i) {
			attrs = mono_metadata_decode_row_col (tdef, i, MONO_TYPEDEF_FLAGS);
			visibility = attrs & TYPE_ATTRIBUTE_VISIBILITY_MASK;
			if (visibility == TYPE_ATTRIBUTE_PUBLIC || visibility == TYPE_ATTRIBUTE_NESTED_PUBLIC)
				count++;
		}
	} else {
		count = tdef->rows - 1;
	}
	res = mono_array_new (domain, mono_defaults.monotype_class, count);
	count = 0;
	for (i = 1; i < tdef->rows; ++i) {
		attrs = mono_metadata_decode_row_col (tdef, i, MONO_TYPEDEF_FLAGS);
		visibility = attrs & TYPE_ATTRIBUTE_VISIBILITY_MASK;
		if (!exportedOnly || (visibility == TYPE_ATTRIBUTE_PUBLIC || visibility == TYPE_ATTRIBUTE_NESTED_PUBLIC)) {
			klass = mono_class_get_throw (image, (i + 1) | MONO_TOKEN_TYPE_DEF);
			if (mono_loader_get_last_error ())
				mono_loader_clear_error ();
			mono_array_setref (res, count, mono_type_get_object (domain, &klass->byval_arg));
			count++;
		}
	}
	
	return res;
}

static MonoArray*
ves_icall_System_Reflection_Assembly_GetTypes (MonoReflectionAssembly *assembly, MonoBoolean exportedOnly)
{
	MonoArray *res = NULL;
	MonoImage *image = NULL;
	MonoTableInfo *table = NULL;
	MonoDomain *domain;
	GList *list = NULL;
	int i, len;

	MONO_ARCH_SAVE_REGS;

	domain = mono_object_domain (assembly);

	g_assert (!assembly->assembly->dynamic);
	image = assembly->assembly->image;
	table = &image->tables [MONO_TABLE_FILE];
	res = mono_module_get_types (domain, image, exportedOnly);

	/* Append data from all modules in the assembly */
	for (i = 0; i < table->rows; ++i) {
		if (!(mono_metadata_decode_row_col (table, i, MONO_FILE_FLAGS) & FILE_CONTAINS_NO_METADATA)) {
			MonoImage *loaded_image = mono_assembly_load_module (image->assembly, i + 1);
			if (loaded_image) {
				MonoArray *res2 = mono_module_get_types (domain, loaded_image, exportedOnly);
				/* Append the new types to the end of the array */
				if (mono_array_length (res2) > 0) {
					guint32 len1, len2;
					MonoArray *res3;

					len1 = mono_array_length (res);
					len2 = mono_array_length (res2);
					res3 = mono_array_new (domain, mono_defaults.monotype_class, len1 + len2);
					mono_array_memcpy_refs (res3, 0, res, 0, len1);
					mono_array_memcpy_refs (res3, len1, res2, 0, len2);
					res = res3;
				}
			}
		}
	}

	/* the ReflectionTypeLoadException must have all the types (Types property), 
	 * NULL replacing types which throws an exception. The LoaderException must
	 * contain all exceptions for NULL items.
	 */

	len = mono_array_length (res);

	for (i = 0; i < len; i++) {
		MonoReflectionType *t = mono_array_get (res, gpointer, i);
		MonoClass *klass = mono_type_get_class (t->type);
		if ((klass != NULL) && klass->exception_type) {
			/* keep the class in the list */
			list = g_list_append (list, klass);
			/* and replace Type with NULL */
			mono_array_setref (res, i, NULL);
		}
	}

	if (list) {
		GList *tmp = NULL;
		MonoException *exc = NULL;
		MonoArray *exl = NULL;
		int length = g_list_length (list);

		mono_loader_clear_error ();

		exl = mono_array_new (domain, mono_defaults.exception_class, length);
		for (i = 0, tmp = list; i < length; i++, tmp = tmp->next) {
			MonoException *exc = mono_class_get_exception_for_failure (tmp->data);
			mono_array_setref (exl, i, exc);
		}
		g_list_free (list);
		list = NULL;

		exc = mono_get_exception_reflection_type_load (res, exl);
		mono_loader_clear_error ();
		mono_raise_exception (exc);
	}
		
	return res;
}

static gboolean
ves_icall_System_Reflection_AssemblyName_ParseName (MonoReflectionAssemblyName *name, MonoString *assname)
{
	MonoAssemblyName aname;
	MonoDomain *domain = mono_object_domain (name);
	char *val;
	gboolean is_version_defined;
	gboolean is_token_defined;

	val = mono_string_to_utf8 (assname);
	if (!mono_assembly_name_parse_full (val, &aname, TRUE, &is_version_defined, &is_token_defined))
		return FALSE;
	
	fill_reflection_assembly_name (domain, name, &aname, "", is_version_defined,
		FALSE, is_token_defined);

	mono_assembly_name_free (&aname);
	g_free ((guint8*) aname.public_key);
	g_free (val);

	return TRUE;
}

static MonoReflectionType*
ves_icall_System_Reflection_Module_GetGlobalType (MonoReflectionModule *module)
{
	MonoDomain *domain = mono_object_domain (module); 
	MonoClass *klass;

	MONO_ARCH_SAVE_REGS;

	g_assert (module->image);

	if (module->image->dynamic && ((MonoDynamicImage*)(module->image))->initial_image)
		/* These images do not have a global type */
		return NULL;

	klass = mono_class_get (module->image, 1 | MONO_TOKEN_TYPE_DEF);
	return mono_type_get_object (domain, &klass->byval_arg);
}

static void
ves_icall_System_Reflection_Module_Close (MonoReflectionModule *module)
{
	/*if (module->image)
		mono_image_close (module->image);*/
}

static MonoString*
ves_icall_System_Reflection_Module_GetGuidInternal (MonoReflectionModule *module)
{
	MonoDomain *domain = mono_object_domain (module); 

	MONO_ARCH_SAVE_REGS;

	g_assert (module->image);
	return mono_string_new (domain, module->image->guid);
}

static void
ves_icall_System_Reflection_Module_GetPEKind (MonoImage *image, gint32 *pe_kind, gint32 *machine)
{
	if (image->dynamic) {
		MonoDynamicImage *dyn = (MonoDynamicImage*)image;
		*pe_kind = dyn->pe_kind;
		*machine = dyn->machine;
	}
	else {
		*pe_kind = ((MonoCLIImageInfo*)(image->image_info))->cli_cli_header.ch_flags & 0x3;
		*machine = ((MonoCLIImageInfo*)(image->image_info))->cli_header.coff.coff_machine;
	}
}

static gint32
ves_icall_System_Reflection_Module_GetMDStreamVersion (MonoImage *image)
{
	return (image->md_version_major << 16) | (image->md_version_minor);
}

static MonoArray*
ves_icall_System_Reflection_Module_InternalGetTypes (MonoReflectionModule *module)
{
	MONO_ARCH_SAVE_REGS;

	if (!module->image)
		return mono_array_new (mono_object_domain (module), mono_defaults.monotype_class, 0);
	else
		return mono_module_get_types (mono_object_domain (module), module->image, FALSE);
}

static gboolean
mono_metadata_memberref_is_method (MonoImage *image, guint32 token)
{
	guint32 cols [MONO_MEMBERREF_SIZE];
	const char *sig;
	mono_metadata_decode_row (&image->tables [MONO_TABLE_MEMBERREF], mono_metadata_token_index (token) - 1, cols, MONO_MEMBERREF_SIZE);
	sig = mono_metadata_blob_heap (image, cols [MONO_MEMBERREF_SIGNATURE]);
	mono_metadata_decode_blob_size (sig, &sig);
	return (*sig != 0x6);
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

static MonoType*
ves_icall_System_Reflection_Module_ResolveTypeToken (MonoImage *image, guint32 token, MonoArray *type_args, MonoArray *method_args, MonoResolveTokenError *error)
{
	MonoClass *klass;
	int table = mono_metadata_token_table (token);
	int index = mono_metadata_token_index (token);
	MonoGenericContext context;

	*error = ResolveTokenError_Other;

	/* Validate token */
	if ((table != MONO_TABLE_TYPEDEF) && (table != MONO_TABLE_TYPEREF) && 
		(table != MONO_TABLE_TYPESPEC)) {
		*error = ResolveTokenError_BadTable;
		return NULL;
	}

	if (image->dynamic) {
		if (type_args || method_args)
			mono_raise_exception (mono_get_exception_not_implemented (NULL));
		klass = mono_lookup_dynamic_token_class (image, token, FALSE, NULL, NULL);
		if (!klass)
			return NULL;
		return &klass->byval_arg;
	}

	if ((index <= 0) || (index > image->tables [table].rows)) {
		*error = ResolveTokenError_OutOfRange;
		return NULL;
	}

	init_generic_context_from_args (&context, type_args, method_args);
	klass = mono_class_get_full (image, token, &context);

	if (mono_loader_get_last_error ())
		mono_raise_exception (mono_loader_error_prepare_exception (mono_loader_get_last_error ()));

	if (klass)
		return &klass->byval_arg;
	else
		return NULL;
}

static MonoMethod*
ves_icall_System_Reflection_Module_ResolveMethodToken (MonoImage *image, guint32 token, MonoArray *type_args, MonoArray *method_args, MonoResolveTokenError *error)
{
	int table = mono_metadata_token_table (token);
	int index = mono_metadata_token_index (token);
	MonoGenericContext context;
	MonoMethod *method;

	*error = ResolveTokenError_Other;

	/* Validate token */
	if ((table != MONO_TABLE_METHOD) && (table != MONO_TABLE_METHODSPEC) && 
		(table != MONO_TABLE_MEMBERREF)) {
		*error = ResolveTokenError_BadTable;
		return NULL;
	}

	if (image->dynamic) {
		if (type_args || method_args)
			mono_raise_exception (mono_get_exception_not_implemented (NULL));
		/* FIXME: validate memberref token type */
		return mono_lookup_dynamic_token_class (image, token, FALSE, NULL, NULL);
	}

	if ((index <= 0) || (index > image->tables [table].rows)) {
		*error = ResolveTokenError_OutOfRange;
		return NULL;
	}
	if ((table == MONO_TABLE_MEMBERREF) && (!mono_metadata_memberref_is_method (image, token))) {
		*error = ResolveTokenError_BadTable;
		return NULL;
	}

	init_generic_context_from_args (&context, type_args, method_args);
	method = mono_get_method_full (image, token, NULL, &context);

	if (mono_loader_get_last_error ())
		mono_raise_exception (mono_loader_error_prepare_exception (mono_loader_get_last_error ()));

	return method;
}

static MonoString*
ves_icall_System_Reflection_Module_ResolveStringToken (MonoImage *image, guint32 token, MonoResolveTokenError *error)
{
	int index = mono_metadata_token_index (token);

	*error = ResolveTokenError_Other;

	/* Validate token */
	if (mono_metadata_token_code (token) != MONO_TOKEN_STRING) {
		*error = ResolveTokenError_BadTable;
		return NULL;
	}

	if (image->dynamic)
		return mono_lookup_dynamic_token_class (image, token, FALSE, NULL, NULL);

	if ((index <= 0) || (index >= image->heap_us.size)) {
		*error = ResolveTokenError_OutOfRange;
		return NULL;
	}

	/* FIXME: What to do if the index points into the middle of a string ? */

	return mono_ldstr (mono_domain_get (), image, index);
}

static MonoClassField*
ves_icall_System_Reflection_Module_ResolveFieldToken (MonoImage *image, guint32 token, MonoArray *type_args, MonoArray *method_args, MonoResolveTokenError *error)
{
	MonoClass *klass;
	int table = mono_metadata_token_table (token);
	int index = mono_metadata_token_index (token);
	MonoGenericContext context;
	MonoClassField *field;

	*error = ResolveTokenError_Other;

	/* Validate token */
	if ((table != MONO_TABLE_FIELD) && (table != MONO_TABLE_MEMBERREF)) {
		*error = ResolveTokenError_BadTable;
		return NULL;
	}

	if (image->dynamic) {
		if (type_args || method_args)
			mono_raise_exception (mono_get_exception_not_implemented (NULL));
		/* FIXME: validate memberref token type */
		return mono_lookup_dynamic_token_class (image, token, FALSE, NULL, NULL);
	}

	if ((index <= 0) || (index > image->tables [table].rows)) {
		*error = ResolveTokenError_OutOfRange;
		return NULL;
	}
	if ((table == MONO_TABLE_MEMBERREF) && (mono_metadata_memberref_is_method (image, token))) {
		*error = ResolveTokenError_BadTable;
		return NULL;
	}

	init_generic_context_from_args (&context, type_args, method_args);
	field = mono_field_from_token (image, token, &klass, &context);

	if (mono_loader_get_last_error ())
		mono_raise_exception (mono_loader_error_prepare_exception (mono_loader_get_last_error ()));
	
	return field;
}


static MonoObject*
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
		if (mono_metadata_memberref_is_method (image, token)) {
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

static MonoArray*
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

	if (image->dynamic)
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

static MonoReflectionType*
ves_icall_ModuleBuilder_create_modified_type (MonoReflectionTypeBuilder *tb, MonoString *smodifiers)
{
	MonoClass *klass;
	int isbyref = 0, rank;
	char *str = mono_string_to_utf8 (smodifiers);
	char *p;

	MONO_ARCH_SAVE_REGS;

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

static MonoBoolean
ves_icall_Type_IsArrayImpl (MonoReflectionType *t)
{
	MonoType *type;
	MonoBoolean res;

	MONO_ARCH_SAVE_REGS;

	type = t->type;
	res = !type->byref && (type->type == MONO_TYPE_ARRAY || type->type == MONO_TYPE_SZARRAY);

	return res;
}

static MonoReflectionType *
ves_icall_Type_make_array_type (MonoReflectionType *type, int rank)
{
	MonoClass *klass, *aklass;

	MONO_ARCH_SAVE_REGS;

	klass = mono_class_from_mono_type (type->type);
	aklass = mono_array_class_get (klass, rank);

	return mono_type_get_object (mono_object_domain (type), &aklass->byval_arg);
}

static MonoReflectionType *
ves_icall_Type_make_byref_type (MonoReflectionType *type)
{
	MonoClass *klass;

	MONO_ARCH_SAVE_REGS;

	klass = mono_class_from_mono_type (type->type);

	return mono_type_get_object (mono_object_domain (type), &klass->this_arg);
}

static MonoReflectionType *
ves_icall_Type_MakePointerType (MonoReflectionType *type)
{
	MonoClass *pklass;

	MONO_ARCH_SAVE_REGS;

	pklass = mono_ptr_class_get (type->type);

	return mono_type_get_object (mono_object_domain (type), &pklass->byval_arg);
}

static MonoObject *
ves_icall_System_Delegate_CreateDelegate_internal (MonoReflectionType *type, MonoObject *target,
						   MonoReflectionMethod *info)
{
	MonoClass *delegate_class = mono_class_from_mono_type (type->type);
	MonoObject *delegate;
	gpointer func;

	MONO_ARCH_SAVE_REGS;

	mono_assert (delegate_class->parent == mono_defaults.multicastdelegate_class);

	/* FIME: We must check if target is visible to the caller under coreclr.
	 * The check should be disabled otherwise as it shouldn't raise expection under fulltrust.
	 */

	delegate = mono_object_new (mono_object_domain (type), delegate_class);

	if (info->method->dynamic)
		/* Creating a trampoline would leak memory */
		func = mono_compile_method (info->method);
	else
		func = mono_create_ftnptr (mono_domain_get (), mono_runtime_create_jump_trampoline (mono_domain_get (), info->method, TRUE));

	mono_delegate_ctor (delegate, target, func);

	return delegate;
}

static void
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

#ifdef PLATFORM_WIN32
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

#ifndef PLATFORM_WIN32
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
static guint32
ves_icall_System_CurrentSystemTimeZone_GetTimeZoneData (guint32 year, MonoArray **data, MonoArray **names)
{
#ifndef PLATFORM_WIN32
	MonoDomain *domain = mono_domain_get ();
	struct tm start, tt;
	time_t t;

	long int gmtoff;
	int is_daylight = 0, day;
	char tzone [64];

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (data);
	MONO_CHECK_ARG_NULL (names);

	(*data) = mono_array_new (domain, mono_defaults.int64_class, 4);
	(*names) = mono_array_new (domain, mono_defaults.string_class, 2);

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
	for (day = 0; day < 365; day++) {

		t += 3600*24;
		tt = *localtime (&t);

		/* Daylight saving starts or ends here. */
		if (gmt_offset (&tt, t) != gmtoff) {
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
			if (is_daylight) {
				mono_array_setref ((*names), 0, mono_string_new (domain, tzone));
				mono_array_set ((*data), gint64, 1, ((gint64)t1 + EPOCH_ADJUST) * 10000000L);
				return 1;
			} else {
				mono_array_setref ((*names), 1, mono_string_new (domain, tzone));
				mono_array_set ((*data), gint64, 0, ((gint64)t1 + EPOCH_ADJUST) * 10000000L);
				is_daylight = 1;
			}

			/* This is only set once when we enter daylight saving. */
			mono_array_set ((*data), gint64, 2, (gint64)gmtoff * 10000000L);
			mono_array_set ((*data), gint64, 3, (gint64)(gmt_offset (&tt, t) - gmtoff) * 10000000L);

			gmtoff = gmt_offset (&tt, t);
		}
	}

	if (!is_daylight) {
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

	(*data) = mono_array_new (domain, mono_defaults.int64_class, 4);
	(*names) = mono_array_new (domain, mono_defaults.string_class, 2);

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

static gpointer
ves_icall_System_Object_obj_address (MonoObject *this) 
{
	MONO_ARCH_SAVE_REGS;

	return this;
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

static gint32 
ves_icall_System_Buffer_ByteLengthInternal (MonoArray *array) 
{
	MONO_ARCH_SAVE_REGS;

	return mono_array_get_byte_length (array);
}

static gint8 
ves_icall_System_Buffer_GetByteInternal (MonoArray *array, gint32 idx) 
{
	MONO_ARCH_SAVE_REGS;

	return mono_array_get (array, gint8, idx);
}

static void 
ves_icall_System_Buffer_SetByteInternal (MonoArray *array, gint32 idx, gint8 value) 
{
	MONO_ARCH_SAVE_REGS;

	mono_array_set (array, gint8, idx, value);
}

static MonoBoolean
ves_icall_System_Buffer_BlockCopyInternal (MonoArray *src, gint32 src_offset, MonoArray *dest, gint32 dest_offset, gint32 count) 
{
	guint8 *src_buf, *dest_buf;

	MONO_ARCH_SAVE_REGS;

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

static MonoObject *
ves_icall_Remoting_RealProxy_GetTransparentProxy (MonoObject *this, MonoString *class_name)
{
	MonoDomain *domain = mono_object_domain (this); 
	MonoObject *res;
	MonoRealProxy *rp = ((MonoRealProxy *)this);
	MonoTransparentProxy *tp;
	MonoType *type;
	MonoClass *klass;

	MONO_ARCH_SAVE_REGS;

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

static MonoReflectionType *
ves_icall_Remoting_RealProxy_InternalGetProxyType (MonoTransparentProxy *tp)
{
	return mono_type_get_object (mono_object_domain (tp), &tp->remote_class->proxy_class->byval_arg);
}

/* System.Environment */

static MonoString *
ves_icall_System_Environment_get_MachineName (void)
{
#if defined (PLATFORM_WIN32)
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
#else
	gchar buf [256];
	MonoString *result;

	if (gethostname (buf, sizeof (buf)) == 0)
		result = mono_string_new (mono_domain_get (), buf);
	else
		result = NULL;
	
	return result;
#endif
}

static int
ves_icall_System_Environment_get_Platform (void)
{
	MONO_ARCH_SAVE_REGS;

#if defined (PLATFORM_WIN32)
	/* Win32NT */
	return 2;
#else
	/* Unix */
	return 128;
#endif
}

static MonoString *
ves_icall_System_Environment_get_NewLine (void)
{
	MONO_ARCH_SAVE_REGS;

#if defined (PLATFORM_WIN32)
	return mono_string_new (mono_domain_get (), "\r\n");
#else
	return mono_string_new (mono_domain_get (), "\n");
#endif
}

static MonoString *
ves_icall_System_Environment_GetEnvironmentVariable (MonoString *name)
{
	const gchar *value;
	gchar *utf8_name;

	MONO_ARCH_SAVE_REGS;

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
#ifdef __APPLE__
/* Apple defines this in crt_externs.h but doesn't provide that header for 
 * arm-apple-darwin9.  We'll manually define the symbol on Apple as it does
 * in fact exist on all implementations (so far) 
 */
gchar ***_NSGetEnviron();
#define environ (*_NSGetEnviron())
#else
extern
char **environ;
#endif
#endif
#endif

static MonoArray *
ves_icall_System_Environment_GetEnvironmentVariableNames (void)
{
#ifdef PLATFORM_WIN32
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

	MONO_ARCH_SAVE_REGS;

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

static void
ves_icall_System_Environment_InternalSetEnvironmentVariable (MonoString *name, MonoString *value)
{
#ifdef PLATFORM_WIN32
	gunichar2 *utf16_name, *utf16_value;
#else
	gchar *utf8_name, *utf8_value;
#endif

	MONO_ARCH_SAVE_REGS;
	
#ifdef PLATFORM_WIN32
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

	utf8_value = mono_string_to_utf8 (value);
	g_setenv (utf8_name, utf8_value, TRUE);

	g_free (utf8_name);
	g_free (utf8_value);
#endif
}

static void
ves_icall_System_Environment_Exit (int result)
{
	MONO_ARCH_SAVE_REGS;

	mono_threads_set_shutting_down ();

	mono_runtime_set_shutting_down ();

	/* Suspend all managed threads since the runtime is going away */
	mono_thread_suspend_all_other_threads ();

	mono_runtime_quit ();

	/* we may need to do some cleanup here... */
	exit (result);
}

static MonoString*
ves_icall_System_Environment_GetGacPath (void)
{
	return mono_string_new (mono_domain_get (), mono_assembly_getrootdir ());
}

static MonoString*
ves_icall_System_Environment_GetWindowsFolderPath (int folder)
{
#if defined (PLATFORM_WIN32)
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

static MonoArray *
ves_icall_System_Environment_GetLogicalDrives (void)
{
        gunichar2 buf [128], *ptr, *dname;
	gunichar2 *u16;
	gint initial_size = 127, size = 128;
	gint ndrives;
	MonoArray *result;
	MonoString *drivestr;
	MonoDomain *domain = mono_domain_get ();
	gint len;

	MONO_ARCH_SAVE_REGS;

        buf [0] = '\0';
	ptr = buf;

	while (size > initial_size) {
		size = GetLogicalDriveStrings (initial_size, ptr);
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

static MonoString *
ves_icall_System_Environment_InternalGetHome (void)
{
	MONO_ARCH_SAVE_REGS;

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
static MonoString*
ves_icall_System_Text_Encoding_InternalCodePage (gint32 *int_code_page) 
{
	const char *cset;
	const char *p;
	char *c;
	char *codepage = NULL;
	int code;
	int want_name = *int_code_page;
	int i;
	
	*int_code_page = -1;
	MONO_ARCH_SAVE_REGS;

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

static MonoBoolean
ves_icall_System_Environment_get_HasShutdownStarted (void)
{
	if (mono_runtime_is_shutting_down ())
		return TRUE;

	if (mono_domain_is_unloading (mono_domain_get ()))
		return TRUE;

	return FALSE;
}

static void
ves_icall_MonoMethodMessage_InitMessage (MonoMethodMessage *this, 
					 MonoReflectionMethod *method,
					 MonoArray *out_args)
{
	MONO_ARCH_SAVE_REGS;

	mono_message_init (mono_object_domain (this), this, method, out_args);
}

static MonoBoolean
ves_icall_IsTransparentProxy (MonoObject *proxy)
{
	MONO_ARCH_SAVE_REGS;

	if (!proxy)
		return 0;

	if (proxy->vtable->klass == mono_defaults.transparent_proxy_class)
		return 1;

	return 0;
}

static MonoReflectionMethod *
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
		int offs = mono_class_interface_offset (klass, method->klass);
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

static void
ves_icall_System_Runtime_Activation_ActivationServices_EnableProxyActivation (MonoReflectionType *type, MonoBoolean enable)
{
	MonoClass *klass;
	MonoVTable* vtable;

	MONO_ARCH_SAVE_REGS;

	klass = mono_class_from_mono_type (type->type);
	vtable = mono_class_vtable (mono_domain_get (), klass);

	if (enable) vtable->remote = 1;
	else vtable->remote = 0;
}

static MonoObject *
ves_icall_System_Runtime_Activation_ActivationServices_AllocateUninitializedClassInstance (MonoReflectionType *type)
{
	MonoClass *klass;
	MonoDomain *domain;
	
	MONO_ARCH_SAVE_REGS;

	domain = mono_object_domain (type);
	klass = mono_class_from_mono_type (type->type);

	if (klass->rank >= 1) {
		g_assert (klass->rank == 1);
		return (MonoObject *) mono_array_new (domain, klass->element_class, 0);
	} else {
		/* Bypass remoting object creation check */
		return mono_object_new_alloc_specific (mono_class_vtable (domain, klass));
	}
}

static MonoString *
ves_icall_System_IO_get_temp_path (void)
{
	MONO_ARCH_SAVE_REGS;

	return mono_string_new (mono_domain_get (), g_get_tmp_dir ());
}

static gpointer
ves_icall_RuntimeMethod_GetFunctionPointer (MonoMethod *method)
{
	MONO_ARCH_SAVE_REGS;

	return mono_compile_method (method);
}

static MonoString *
ves_icall_System_Configuration_DefaultConfig_get_machine_config_path (void)
{
	MonoString *mcpath;
	gchar *path;

	MONO_ARCH_SAVE_REGS;

	path = g_build_path (G_DIR_SEPARATOR_S, mono_get_config_dir (), "mono", mono_get_runtime_info ()->framework_version, "machine.config", NULL);

#if defined (PLATFORM_WIN32)
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
get_bundled_machine_config (void)
{
	const gchar *machine_config;

	MONO_ARCH_SAVE_REGS;

	machine_config = mono_get_machine_config ();

	if (!machine_config)
		return NULL;

	return mono_string_new (mono_domain_get (), machine_config);
}

static MonoString *
ves_icall_System_Web_Util_ICalls_get_machine_install_dir (void)
{
	MonoString *ipath;
	gchar *path;

	MONO_ARCH_SAVE_REGS;

	path = g_path_get_dirname (mono_get_config_dir ());

#if defined (PLATFORM_WIN32)
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

static void
ves_icall_System_Diagnostics_DefaultTraceListener_WriteWindowsDebugString (MonoString *message)
{
#if defined (PLATFORM_WIN32)
	OutputDebugString (mono_string_chars (message));
#else
	g_warning ("WriteWindowsDebugString called and PLATFORM_WIN32 not defined!\n");
#endif
}

/* Only used for value types */
static MonoObject *
ves_icall_System_Activator_CreateInstanceInternal (MonoReflectionType *type)
{
	MonoClass *klass;
	MonoDomain *domain;
	
	MONO_ARCH_SAVE_REGS;

	domain = mono_object_domain (type);
	klass = mono_class_from_mono_type (type->type);

	if (mono_class_is_nullable (klass))
		/* No arguments -> null */
		return NULL;

	return mono_object_new (domain, klass);
}

static MonoReflectionMethod *
ves_icall_MonoMethod_get_base_definition (MonoReflectionMethod *m)
{
	MonoClass *klass, *parent;
	MonoMethod *method = m->method;
	MonoMethod *result = NULL;

	MONO_ARCH_SAVE_REGS;

	if (method->klass == NULL)
		return m;

	if (!(method->flags & METHOD_ATTRIBUTE_VIRTUAL) ||
	    MONO_CLASS_IS_INTERFACE (method->klass) ||
	    method->flags & METHOD_ATTRIBUTE_NEW_SLOT)
		return m;

	klass = method->klass;
	if (klass->generic_class)
		klass = klass->generic_class->container_class;

	/* At the end of the loop, klass points to the eldest class that has this virtual function slot. */
	for (parent = klass->parent; parent != NULL; parent = parent->parent) {
		mono_class_setup_vtable (parent);
		if (parent->vtable_size <= method->slot)
			break;
		klass = parent;
	}		

	if (klass == method->klass)
		return m;

	result = klass->vtable [method->slot];
	if (result == NULL) {
		/* It is an abstract method */
		gpointer iter = NULL;
		while ((result = mono_class_get_methods (klass, &iter)))
			if (result->slot == method->slot)
				break;
	}

	if (result == NULL)
		return m;

	return mono_method_get_object (mono_domain_get (), result, NULL);
}

static MonoString*
ves_icall_MonoMethod_get_name (MonoReflectionMethod *m)
{
	MonoMethod *method = m->method;

	MONO_OBJECT_SETREF (m, name, mono_string_new (mono_object_domain (m), method->name));
	return m->name;
}

static void
mono_ArgIterator_Setup (MonoArgIterator *iter, char* argsp, char* start)
{
	MONO_ARCH_SAVE_REGS;

	iter->sig = *(MonoMethodSignature**)argsp;
	
	g_assert (iter->sig->sentinelpos <= iter->sig->param_count);
	g_assert (iter->sig->call_convention == MONO_CALL_VARARG);

	iter->next_arg = 0;
	/* FIXME: it's not documented what start is exactly... */
	if (start) {
		iter->args = start;
	} else {
		guint32 i, arg_size;
		gint32 align;
		iter->args = argsp + sizeof (gpointer);
#ifndef MONO_ARCH_REGPARMS
		for (i = 0; i < iter->sig->sentinelpos; ++i) {
			arg_size = mono_type_stack_size (iter->sig->params [i], &align);
			iter->args = (char*)iter->args + arg_size;
		}
#endif
	}
	iter->num_args = iter->sig->param_count - iter->sig->sentinelpos;

	/* g_print ("sig %p, param_count: %d, sent: %d\n", iter->sig, iter->sig->param_count, iter->sig->sentinelpos); */
}

static MonoTypedRef
mono_ArgIterator_IntGetNextArg (MonoArgIterator *iter)
{
	guint32 i, arg_size;
	gint32 align;
	MonoTypedRef res;
	MONO_ARCH_SAVE_REGS;

	i = iter->sig->sentinelpos + iter->next_arg;

	g_assert (i < iter->sig->param_count);

	res.type = iter->sig->params [i];
	res.klass = mono_class_from_mono_type (res.type);
	/* FIXME: endianess issue... */
	res.value = iter->args;
	arg_size = mono_type_stack_size (res.type, &align);
	iter->args = (char*)iter->args + arg_size;
	iter->next_arg++;

	/* g_print ("returning arg %d, type 0x%02x of size %d at %p\n", i, res.type->type, arg_size, res.value); */

	return res;
}

static MonoTypedRef
mono_ArgIterator_IntGetNextArgT (MonoArgIterator *iter, MonoType *type)
{
	guint32 i, arg_size;
	gint32 align;
	MonoTypedRef res;
	MONO_ARCH_SAVE_REGS;

	i = iter->sig->sentinelpos + iter->next_arg;

	g_assert (i < iter->sig->param_count);

	while (i < iter->sig->param_count) {
		if (!mono_metadata_type_equal (type, iter->sig->params [i]))
			continue;
		res.type = iter->sig->params [i];
		res.klass = mono_class_from_mono_type (res.type);
		/* FIXME: endianess issue... */
		res.value = iter->args;
		arg_size = mono_type_stack_size (res.type, &align);
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

static MonoType*
mono_ArgIterator_IntGetNextArgType (MonoArgIterator *iter)
{
	gint i;
	MONO_ARCH_SAVE_REGS;
	
	i = iter->sig->sentinelpos + iter->next_arg;

	g_assert (i < iter->sig->param_count);

	return iter->sig->params [i];
}

static MonoObject*
mono_TypedReference_ToObject (MonoTypedRef tref)
{
	MONO_ARCH_SAVE_REGS;

	if (MONO_TYPE_IS_REFERENCE (tref.type)) {
		MonoObject** objp = tref.value;
		return *objp;
	}

	return mono_value_box (mono_domain_get (), tref.klass, tref.value);
}

static MonoObject*
mono_TypedReference_ToObjectInternal (MonoType *type, gpointer value, MonoClass *klass)
{
	MONO_ARCH_SAVE_REGS;

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

static void
ves_icall_System_Runtime_InteropServices_Marshal_Prelink (MonoReflectionMethod *method)
{
	MONO_ARCH_SAVE_REGS;
	prelink_method (method->method);
}

static void
ves_icall_System_Runtime_InteropServices_Marshal_PrelinkAll (MonoReflectionType *type)
{
	MonoClass *klass = mono_class_from_mono_type (type->type);
	MonoMethod* m;
	gpointer iter = NULL;
	MONO_ARCH_SAVE_REGS;

	while ((m = mono_class_get_methods (klass, &iter)))
		prelink_method (m);
}

/* These parameters are "readonly" in corlib/System/NumberFormatter.cs */
static void
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

/* These parameters are "readonly" in corlib/System/Char.cs */
static void
ves_icall_System_Char_GetDataTablePointers (guint8 const **category_data,
					    guint8 const **numeric_data,
					    gdouble const **numeric_data_values,
					    guint16 const **to_lower_data_low,
					    guint16 const **to_lower_data_high,
					    guint16 const **to_upper_data_low,
					    guint16 const **to_upper_data_high)
{
	*category_data = CategoryData;
	*numeric_data = NumericData;
	*numeric_data_values = NumericDataValues;
	*to_lower_data_low = ToLowerDataLow;
	*to_lower_data_high = ToLowerDataHigh;
	*to_upper_data_low = ToUpperDataLow;
	*to_upper_data_high = ToUpperDataHigh;
}

static gint32
ves_icall_MonoDebugger_GetMethodToken (MonoReflectionMethod *method)
{
	return method->method->token;
}

/*
 * We return NULL for no modifiers so the corlib code can return Type.EmptyTypes
 * and avoid useless allocations.
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
			MonoClass *klass = mono_class_get (image, type->modifiers [i].token);
			mono_array_setref (res, count, mono_type_get_object (mono_domain_get (), &klass->byval_arg));
			count++;
		}
	}
	return res;
}

static MonoArray*
param_info_get_type_modifiers (MonoReflectionParameter *param, MonoBoolean optional)
{
	MonoType *type = param->ClassImpl->type;
	MonoReflectionMethod *method = (MonoReflectionMethod*)param->MemberImpl;
	MonoImage *image = method->method->klass->image;
	int pos = param->PositionImpl;
	MonoMethodSignature *sig = mono_method_signature (method->method);
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

static MonoArray*
property_info_get_type_modifiers (MonoReflectionProperty *property, MonoBoolean optional)
{
	MonoType *type = get_property_type (property->property);
	MonoImage *image = property->klass->image;

	if (!type)
		return NULL;
	return type_array_from_modifiers (image, type, optional);
}

static MonoBoolean
custom_attrs_defined_internal (MonoObject *obj, MonoReflectionType *attr_type)
{
	MonoCustomAttrInfo *cinfo;
	gboolean found;

	cinfo = mono_reflection_get_custom_attrs_info (obj);
	if (!cinfo)
		return FALSE;
	found = mono_custom_attrs_has_attr (cinfo, mono_class_from_mono_type (attr_type->type));
	if (!cinfo->cached)
		mono_custom_attrs_free (cinfo);
	return found;
}

static MonoArray*
custom_attrs_get_by_type (MonoObject *obj, MonoReflectionType *attr_type)
{
	MonoArray *res = mono_reflection_get_custom_attrs_by_type (obj, attr_type ? mono_class_from_mono_type (attr_type->type) : NULL);

	if (mono_loader_get_last_error ()) {
		mono_raise_exception (mono_loader_error_prepare_exception (mono_loader_get_last_error ()));
		g_assert_not_reached ();
	} else {
		return res;
	}
}

static MonoBoolean
GCHandle_CheckCurrentDomain (guint32 gchandle)
{
	return mono_gchandle_is_in_domain (gchandle, mono_domain_get ());
}

static MonoString*
ves_icall_Mono_Runtime_GetDisplayName (void)
{
	static const char display_name_str [] = "Mono " VERSION;
	MonoString *display_name = mono_string_new (mono_domain_get (), display_name_str);
	return display_name;
}

static MonoString*
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

const static guchar
dbase64 [] = {
	128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128,
	128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128,
	128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 62, 128, 128, 128, 63,
	52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 128, 128, 128, 0, 128, 128,
	128, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
	15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 128, 128, 128, 128, 128,
	128, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
	41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51
};

static MonoArray *
base64_to_byte_array (gunichar2 *start, gint ilength, MonoBoolean allowWhitespaceOnly)
{
	gint ignored;
	gint i;
	gunichar2 c;
	gunichar2 last, prev_last, prev2_last;
	gint olength;
	MonoArray *result;
	guchar *res_ptr;
	gint a [4], b [4];
	MonoException *exc;

	ignored = 0;
	last = prev_last = 0, prev2_last = 0;
	for (i = 0; i < ilength; i++) {
		c = start [i];
		if (c >= sizeof (dbase64)) {
			exc = mono_exception_from_name_msg (mono_get_corlib (),
				"System", "FormatException",
				"Invalid character found.");
			mono_raise_exception (exc);
		} else if (isspace (c)) {
			ignored++;
		} else {
			prev2_last = prev_last;
			prev_last = last;
			last = c;
		}
	}

	olength = ilength - ignored;

	if (allowWhitespaceOnly && olength == 0) {
		return mono_array_new (mono_domain_get (), mono_defaults.byte_class, 0);
	}

	if ((olength & 3) != 0 || olength <= 0) {
		exc = mono_exception_from_name_msg (mono_get_corlib (), "System",
					"FormatException", "Invalid length.");
		mono_raise_exception (exc);
	}

	if (prev2_last == '=') {
		exc = mono_exception_from_name_msg (mono_get_corlib (), "System", "FormatException", "Invalid format.");
		mono_raise_exception (exc);
	}

	olength = (olength * 3) / 4;
	if (last == '=')
		olength--;

	if (prev_last == '=')
		olength--;

	result = mono_array_new (mono_domain_get (), mono_defaults.byte_class, olength);
	res_ptr = mono_array_addr (result, guchar, 0);
	for (i = 0; i < ilength; ) {
		int k;

		for (k = 0; k < 4 && i < ilength;) {
			c = start [i++];
			if (isspace (c))
				continue;

			a [k] = (guchar) c;
			if (((b [k] = dbase64 [c]) & 0x80) != 0) {
				exc = mono_exception_from_name_msg (mono_get_corlib (),
					"System", "FormatException",
					"Invalid character found.");
				mono_raise_exception (exc);
			}
			k++;
		}

		*res_ptr++ = (b [0] << 2) | (b [1] >> 4);
		if (a [2] != '=')
			*res_ptr++ = (b [1] << 4) | (b [2] >> 2);
		if (a [3] != '=')
			*res_ptr++ = (b [2] << 6) | b [3];

		while (i < ilength && isspace (start [i]))
			i++;
	}

	return result;
}

static MonoArray *
InternalFromBase64String (MonoString *str, MonoBoolean allowWhitespaceOnly)
{
	MONO_ARCH_SAVE_REGS;

	return base64_to_byte_array (mono_string_chars (str), 
		mono_string_length (str), allowWhitespaceOnly);
}

static MonoArray *
InternalFromBase64CharArray (MonoArray *input, gint offset, gint length)
{
	MONO_ARCH_SAVE_REGS;

	return base64_to_byte_array (mono_array_addr (input, gunichar2, offset),
		length, FALSE);
}

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

static GHashTable *icall_hash = NULL;
static GHashTable *jit_icall_hash_name = NULL;
static GHashTable *jit_icall_hash_addr = NULL;

void
mono_icall_init (void)
{
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

	icall_hash = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, NULL);
}

void
mono_icall_cleanup (void)
{
	g_hash_table_destroy (icall_hash);
	g_hash_table_destroy (jit_icall_hash_name);
	g_hash_table_destroy (jit_icall_hash_addr);
}

void
mono_add_internal_call (const char *name, gconstpointer method)
{
	mono_loader_lock ();

	g_hash_table_insert (icall_hash, g_strdup (name), (gpointer) method);

	mono_loader_unlock ();
}

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
	const guint16 *nameslot = bsearch (name, icall_names_idx + imap->first_icall, icall_desc_num_icalls (imap), sizeof (icall_names_idx [0]), compare_method_imap);
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
	const guint16 *nameslot = bsearch (name, icall_type_names_idx, Icall_type_num, sizeof (icall_type_names_idx [0]), compare_class_imap);
	if (!nameslot)
		return NULL;
	return &icall_type_descs [nameslot - &icall_type_names_idx [0]];
}

#else
static int
compare_method_imap (const void *key, const void *elem)
{
	const char** method_name = (const char**)elem;
	return strcmp (key, *method_name);
}

static gpointer
find_method_icall (const IcallTypeDesc *imap, const char *name)
{
	const char **nameslot = bsearch (name, icall_names + imap->first_icall, icall_desc_num_icalls (imap), sizeof (icall_names [0]), compare_method_imap);
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
	const char **nameslot = bsearch (name, icall_type_names, Icall_type_num, sizeof (icall_type_names [0]), compare_class_imap);
	if (!nameslot)
		return NULL;
	return &icall_type_descs [nameslot - icall_type_names];
}

#endif

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

gpointer
mono_lookup_internal_call (MonoMethod *method)
{
	char *sigstart;
	char *tmpsig;
	char mname [2048];
	int typelen = 0, mlen, siglen;
	gpointer res;
	const IcallTypeDesc *imap;

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

	imap = find_class_icalls (mname);

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
	
	mono_loader_lock ();

	res = g_hash_table_lookup (icall_hash, mname);
	if (res) {
		mono_loader_unlock ();
		return res;
	}
	/* try without signature */
	*sigstart = 0;
	res = g_hash_table_lookup (icall_hash, mname);
	if (res) {
		mono_loader_unlock ();
		return res;
	}

	/* it wasn't found in the static call tables */
	if (!imap) {
		mono_loader_unlock ();
		return NULL;
	}
	res = find_method_icall (imap, sigstart - mlen);
	if (res) {
		mono_loader_unlock ();
		return res;
	}
	/* try _with_ signature */
	*sigstart = '(';
	res = find_method_icall (imap, sigstart - mlen);
	if (res) {
		mono_loader_unlock ();
		return res;
	}

	g_warning ("cant resolve internal call to \"%s\" (tested without signature also)", mname);
	g_print ("\nYour mono runtime and class libraries are out of sync.\n");
	g_print ("The out of sync library is: %s\n", method->klass->image->name);
	g_print ("\nWhen you update one from svn you need to update, compile and install\nthe other too.\n");
	g_print ("Do not report this as a bug unless you're sure you have updated correctly:\nyou probably have a broken mono install.\n");
	g_print ("If you see other errors or faults after this message they are probably related\n");
	g_print ("and you need to fix your mono install first.\n");

	mono_loader_unlock ();

	return NULL;
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
	else if (!strcmp (typename, "bool"))
		klass = mono_defaults.boolean_class;
	else if (!strcmp (typename, "boolean"))
		klass = mono_defaults.boolean_class;
	else {
		g_error (typename);
		g_assert_not_reached ();
	}
	return &klass->byval_arg;
}

MonoMethodSignature*
mono_create_icall_signature (const char *sigstr)
{
	gchar **parts;
	int i, len;
	gchar **tmp;
	MonoMethodSignature *res;

	mono_loader_lock ();
	res = g_hash_table_lookup (mono_defaults.corlib->helper_signatures, sigstr);
	if (res) {
		mono_loader_unlock ();
		return res;
	}

	parts = g_strsplit (sigstr, " ", 256);

	tmp = parts;
	len = 0;
	while (*tmp) {
		len ++;
		tmp ++;
	}

	res = mono_metadata_signature_alloc (mono_defaults.corlib, len - 1);
	res->pinvoke = 1;

#ifdef PLATFORM_WIN32
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

	g_hash_table_insert (mono_defaults.corlib->helper_signatures, (gpointer)sigstr, res);

	mono_loader_unlock ();

	return res;
}

MonoJitICallInfo *
mono_find_jit_icall_by_name (const char *name)
{
	MonoJitICallInfo *info;
	g_assert (jit_icall_hash_name);

	mono_loader_lock ();
	info = g_hash_table_lookup (jit_icall_hash_name, name);
	mono_loader_unlock ();
	return info;
}

MonoJitICallInfo *
mono_find_jit_icall_by_addr (gconstpointer addr)
{
	MonoJitICallInfo *info;
	g_assert (jit_icall_hash_addr);

	mono_loader_lock ();
	info = g_hash_table_lookup (jit_icall_hash_addr, (gpointer)addr);
	mono_loader_unlock ();

	return info;
}

void
mono_register_jit_icall_wrapper (MonoJitICallInfo *info, gconstpointer wrapper)
{
	mono_loader_lock ();
	g_hash_table_insert (jit_icall_hash_addr, (gpointer)wrapper, info);
	mono_loader_unlock ();
}

MonoJitICallInfo *
mono_register_jit_icall (gconstpointer func, const char *name, MonoMethodSignature *sig, gboolean is_save)
{
	MonoJitICallInfo *info;
	
	g_assert (func);
	g_assert (name);

	mono_loader_lock ();

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

	if (is_save) {
		info->wrapper = func;
	} else {
		info->wrapper = NULL;
	}

	g_hash_table_insert (jit_icall_hash_name, (gpointer)info->name, info);
	g_hash_table_insert (jit_icall_hash_addr, (gpointer)func, info);

	mono_loader_unlock ();
	return info;
}
