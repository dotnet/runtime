/**
 * \file
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *	 Patrik Torstensson (patrik.torstensson@labs2.com)
 *   Marek Safar (marek.safar@gmail.com)
 *   Aleksey Kliger (aleksey@xamarin.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011-2015 Xamarin Inc (http://www.xamarin.com).
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
#if defined (HAVE_WCHAR_H)
#include <wchar.h>
#endif
#include "mono/metadata/icall-internals.h"
#include "mono/utils/mono-membar.h"
#include <mono/metadata/object.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/threadpool.h>
#include <mono/metadata/threadpool-io.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/image-internals.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/w32file.h>
#include <mono/metadata/console-io.h>
#include <mono/metadata/mono-route.h>
#include <mono/metadata/w32socket.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/rand.h>
#include <mono/metadata/sysmath.h>
#include <mono/metadata/appdomain-icalls.h>
#include <mono/metadata/string-icalls.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/w32process.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/locales.h>
#include <mono/metadata/filewatcher.h>
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
#include <mono/metadata/seq-points-data.h>
#include <mono/metadata/handle.h>
#include <mono/metadata/w32mutex.h>
#include <mono/metadata/w32semaphore.h>
#include <mono/metadata/w32event.h>
#include <mono/utils/monobitset.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/mono-string.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-io-portability.h>
#include <mono/utils/mono-digest.h>
#include <mono/utils/bsearch.h>
#include <mono/utils/mono-os-mutex.h>
#include <mono/utils/mono-threads.h>
#include <mono/metadata/w32error.h>
#include <mono/utils/w32api.h>

#include "decimal-ms.h"
#include "number-ms.h"

#if !defined(HOST_WIN32) && defined(HAVE_SYS_UTSNAME_H)
#include <sys/utsname.h>
#endif

extern MonoStringHandle ves_icall_System_Environment_GetOSVersionString (MonoError *error);

ICALL_EXPORT MonoReflectionAssemblyHandle ves_icall_System_Reflection_Assembly_GetCallingAssembly (MonoError *error);

/* Lazy class loading functions */
static GENERATE_GET_CLASS_WITH_CACHE (system_version, "System", "Version")
static GENERATE_GET_CLASS_WITH_CACHE (assembly_name, "System.Reflection", "AssemblyName")
static GENERATE_GET_CLASS_WITH_CACHE (constructor_info, "System.Reflection", "ConstructorInfo")
static GENERATE_GET_CLASS_WITH_CACHE (property_info, "System.Reflection", "PropertyInfo")
static GENERATE_GET_CLASS_WITH_CACHE (event_info, "System.Reflection", "EventInfo")
static GENERATE_GET_CLASS_WITH_CACHE (module, "System.Reflection", "Module")

static void
array_set_value_impl (MonoArray *arr, MonoObject *value, guint32 pos, MonoError *error);

static MonoArrayHandle
type_array_from_modifiers (MonoImage *image, MonoType *type, int optional, MonoError *error);

static inline MonoBoolean
is_generic_parameter (MonoType *type)
{
	return !type->byref && (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR);
}

static void
mono_class_init_checked (MonoClass *klass, MonoError *error)
{
	error_init (error);

	if (!mono_class_init (klass))
		mono_error_set_for_class_failure (error, klass);
}

#ifndef HOST_WIN32
static inline void
mono_icall_make_platform_path (gchar *path)
{
	return;
}

static inline const gchar *
mono_icall_get_file_path_prefix (const gchar *path)
{
	return "file://";
}
#endif /* HOST_WIN32 */

ICALL_EXPORT MonoObject *
ves_icall_System_Array_GetValueImpl (MonoArray *arr, guint32 pos)
{
	MonoError error;
	MonoClass *ac;
	gint32 esize;
	gpointer *ea;
	MonoObject *result = NULL;

	ac = (MonoClass *)arr->obj.vtable->klass;

	esize = mono_array_element_size (ac);
	ea = (gpointer*)((char*)arr->vector + (pos * esize));

	if (ac->element_class->valuetype) {
		result = mono_value_box_checked (arr->obj.vtable->domain, ac->element_class, ea, &error);
		mono_error_set_pending_exception (&error);
	} else
		result = (MonoObject *)*ea;
	return result;
}

ICALL_EXPORT MonoObject *
ves_icall_System_Array_GetValue (MonoArray *arr, MonoArray *idxs)
{
	MonoClass *ac, *ic;
	MonoArray *io;
	gint32 i, pos, *ind;

	MONO_CHECK_ARG_NULL (idxs, NULL);

	io = idxs;
	ic = (MonoClass *)io->obj.vtable->klass;
	
	ac = (MonoClass *)arr->obj.vtable->klass;

	g_assert (ic->rank == 1);
	if (io->bounds != NULL || io->max_length !=  ac->rank) {
		mono_set_pending_exception (mono_get_exception_argument (NULL, NULL));
		return NULL;
	}

	ind = (gint32 *)io->vector;

	if (arr->bounds == NULL) {
		if (*ind < 0 || *ind >= arr->max_length) {
			mono_set_pending_exception (mono_get_exception_index_out_of_range ());
			return NULL;
		}

		return ves_icall_System_Array_GetValueImpl (arr, *ind);
	}
	
	for (i = 0; i < ac->rank; i++) {
		if ((ind [i] < arr->bounds [i].lower_bound) ||
		    (ind [i] >=  (mono_array_lower_bound_t)arr->bounds [i].length + arr->bounds [i].lower_bound)) {
			mono_set_pending_exception (mono_get_exception_index_out_of_range ());
			return NULL;
		}
	}

	pos = ind [0] - arr->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++)
		pos = pos * arr->bounds [i].length + ind [i] - 
			arr->bounds [i].lower_bound;

	return ves_icall_System_Array_GetValueImpl (arr, pos);
}

ICALL_EXPORT void
ves_icall_System_Array_SetValueImpl (MonoArray *arr, MonoObject *value, guint32 pos)
{
	MonoError error;
	error_init (&error);
	array_set_value_impl (arr, value, pos, &error);
	mono_error_set_pending_exception (&error);
}

static void
array_set_value_impl (MonoArray *arr, MonoObject *value, guint32 pos, MonoError *error)
{
	MonoClass *ac, *vc, *ec;
	gint32 esize, vsize;
	gpointer *ea, *va;
	int et, vt;

	guint64 u64 = 0;
	gint64 i64 = 0;
	gdouble r64 = 0;

	error_init (error);

	if (value)
		vc = value->vtable->klass;
	else
		vc = NULL;

	ac = arr->obj.vtable->klass;
	ec = ac->element_class;

	esize = mono_array_element_size (ac);
	ea = (gpointer*)((char*)arr->vector + (pos * esize));
	va = (gpointer*)((char*)value + sizeof (MonoObject));

	if (mono_class_is_nullable (ec)) {
		mono_nullable_init ((guint8*)ea, value, ec);
		return;
	}

	if (!value) {
		mono_gc_bzero_atomic (ea, esize);
		return;
	}

#define NO_WIDENING_CONVERSION G_STMT_START{				\
		mono_error_set_argument (error, "value", "not a widening conversion"); \
		return;							\
	}G_STMT_END

#define CHECK_WIDENING_CONVERSION(extra) G_STMT_START{			\
		if (esize < vsize + (extra)) {				\
			mono_error_set_argument (error, "value", "not a widening conversion"); \
			return;						\
		}							\
	}G_STMT_END

#define INVALID_CAST G_STMT_START{					\
		mono_get_runtime_callbacks ()->set_cast_details (vc, ec); \
		mono_error_set_invalid_cast (error);			\
		return;							\
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
	default:
		break;
	}

	if (!ec->valuetype) {
		gboolean castOk = (NULL != mono_object_isinst_checked (value, ec, error));
		if (!is_ok (error))
			return;
		if (!castOk)
			INVALID_CAST;
		mono_gc_wbarrier_set_arrayref (arr, ea, (MonoObject*)value);
		return;
	}

	if (mono_object_isinst_checked (value, ec, error)) {
		if (ec->has_references)
			mono_value_copy (ea, (char*)value + sizeof (MonoObject), ec);
		else
			mono_gc_memmove_atomic (ea, (char *)value + sizeof (MonoObject), esize);
		return;
	}
	if (!is_ok (error))
		return;

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
ves_icall_System_Array_SetValue (MonoArray *arr, MonoObject *value,
				 MonoArray *idxs)
{
	MonoError error;
	MonoClass *ac, *ic;
	gint32 i, pos, *ind;

	MONO_CHECK_ARG_NULL (idxs,);

	error_init (&error);

	ic = idxs->obj.vtable->klass;
	ac = arr->obj.vtable->klass;

	g_assert (ic->rank == 1);
	if (idxs->bounds != NULL || idxs->max_length != ac->rank) {
		mono_set_pending_exception (mono_get_exception_argument (NULL, NULL));
		return;
	}

	ind = (gint32 *)idxs->vector;

	if (arr->bounds == NULL) {
		if (*ind < 0 || *ind >= arr->max_length) {
			mono_set_pending_exception (mono_get_exception_index_out_of_range ());
			return;
		}

		array_set_value_impl (arr, value, *ind, &error);
		mono_error_set_pending_exception (&error);
		return;
	}
	
	for (i = 0; i < ac->rank; i++)
		if ((ind [i] < arr->bounds [i].lower_bound) ||
		    (ind [i] >= (mono_array_lower_bound_t)arr->bounds [i].length + arr->bounds [i].lower_bound)) {
			mono_set_pending_exception (mono_get_exception_index_out_of_range ());
			return;
		}

	pos = ind [0] - arr->bounds [0].lower_bound;
	for (i = 1; i < ac->rank; i++)
		pos = pos * arr->bounds [i].length + ind [i] - 
			arr->bounds [i].lower_bound;

	array_set_value_impl (arr, value, pos, &error);
	mono_error_set_pending_exception (&error);
}

ICALL_EXPORT MonoArray *
ves_icall_System_Array_CreateInstanceImpl (MonoReflectionType *type, MonoArray *lengths, MonoArray *bounds)
{
	MonoError error;
	MonoClass *aklass, *klass;
	MonoArray *array;
	uintptr_t *sizes, i;
	gboolean bounded = FALSE;

	MONO_CHECK_ARG_NULL (type, NULL);
	MONO_CHECK_ARG_NULL (lengths, NULL);

	MONO_CHECK_ARG (lengths, mono_array_length (lengths) > 0, NULL);
	if (bounds)
		MONO_CHECK_ARG (bounds, mono_array_length (lengths) == mono_array_length (bounds), NULL);

	for (i = 0; i < mono_array_length (lengths); i++) {
		if (mono_array_get (lengths, gint32, i) < 0) {
			mono_set_pending_exception (mono_get_exception_argument_out_of_range (NULL));
			return NULL;
		}
	}

	klass = mono_class_from_mono_type (type->type);
	mono_class_init_checked (klass, &error);
	if (mono_error_set_pending_exception (&error))
		return NULL;

	if (klass->element_class->byval_arg.type == MONO_TYPE_VOID) {
		mono_set_pending_exception (mono_get_exception_not_supported ("Arrays of System.Void are not supported."));
		return NULL;
	}

	if (bounds && (mono_array_length (bounds) == 1) && (mono_array_get (bounds, gint32, 0) != 0))
		/* vectors are not the same as one dimensional arrays with no-zero bounds */
		bounded = TRUE;
	else
		bounded = FALSE;

	aklass = mono_bounded_array_class_get (klass, mono_array_length (lengths), bounded);

	sizes = (uintptr_t *)alloca (aklass->rank * sizeof(intptr_t) * 2);
	for (i = 0; i < aklass->rank; ++i) {
		sizes [i] = mono_array_get (lengths, guint32, i);
		if (bounds)
			sizes [i + aklass->rank] = mono_array_get (bounds, gint32, i);
		else
			sizes [i + aklass->rank] = 0;
	}

	array = mono_array_new_full_checked (mono_object_domain (type), aklass, sizes, (intptr_t*)sizes + aklass->rank, &error);
	mono_error_set_pending_exception (&error);

	return array;
}

ICALL_EXPORT MonoArray *
ves_icall_System_Array_CreateInstanceImpl64 (MonoReflectionType *type, MonoArray *lengths, MonoArray *bounds)
{
	MonoError error;
	MonoClass *aklass, *klass;
	MonoArray *array;
	uintptr_t *sizes, i;
	gboolean bounded = FALSE;

	MONO_CHECK_ARG_NULL (type, NULL);
	MONO_CHECK_ARG_NULL (lengths, NULL);

	MONO_CHECK_ARG (lengths, mono_array_length (lengths) > 0, NULL);
	if (bounds)
		MONO_CHECK_ARG (bounds, mono_array_length (lengths) == mono_array_length (bounds), NULL);

	for (i = 0; i < mono_array_length (lengths); i++) {
		if ((mono_array_get (lengths, gint64, i) < 0) ||
		    (mono_array_get (lengths, gint64, i) > MONO_ARRAY_MAX_INDEX)) {
			mono_set_pending_exception (mono_get_exception_argument_out_of_range (NULL));
			return NULL;
		}
	}

	klass = mono_class_from_mono_type (type->type);
	mono_class_init_checked (klass, &error);
	if (mono_error_set_pending_exception (&error))
		return NULL;

	if (bounds && (mono_array_length (bounds) == 1) && (mono_array_get (bounds, gint64, 0) != 0))
		/* vectors are not the same as one dimensional arrays with no-zero bounds */
		bounded = TRUE;
	else
		bounded = FALSE;

	aklass = mono_bounded_array_class_get (klass, mono_array_length (lengths), bounded);

	sizes = (uintptr_t *)alloca (aklass->rank * sizeof(intptr_t) * 2);
	for (i = 0; i < aklass->rank; ++i) {
		sizes [i] = mono_array_get (lengths, guint64, i);
		if (bounds)
			sizes [i + aklass->rank] = (mono_array_size_t) mono_array_get (bounds, guint64, i);
		else
			sizes [i + aklass->rank] = 0;
	}

	array = mono_array_new_full_checked (mono_object_domain (type), aklass, sizes, (intptr_t*)sizes + aklass->rank, &error);
	mono_error_set_pending_exception (&error);

	return array;
}

ICALL_EXPORT gint32 
ves_icall_System_Array_GetRank (MonoObject *arr)
{
	return arr->vtable->klass->rank;
}

ICALL_EXPORT gint32
ves_icall_System_Array_GetLength (MonoArray *arr, gint32 dimension)
{
	gint32 rank = arr->obj.vtable->klass->rank;
	uintptr_t length;

	if ((dimension < 0) || (dimension >= rank)) {
		mono_set_pending_exception (mono_get_exception_index_out_of_range ());
		return 0;
	}
	
	if (arr->bounds == NULL)
		length = arr->max_length;
	else
		length = arr->bounds [dimension].length;

#ifdef MONO_BIG_ARRAYS
	if (length > G_MAXINT32) {
		mono_set_pending_exception (mono_get_exception_overflow ());
		return 0;
	}
#endif
	return length;
}

ICALL_EXPORT gint64
ves_icall_System_Array_GetLongLength (MonoArray *arr, gint32 dimension)
{
	gint32 rank = arr->obj.vtable->klass->rank;

	if ((dimension < 0) || (dimension >= rank)) {
		mono_set_pending_exception (mono_get_exception_index_out_of_range ());
		return 0;
	}
	
	if (arr->bounds == NULL)
 		return arr->max_length;
 	
 	return arr->bounds [dimension].length;
}

ICALL_EXPORT gint32
ves_icall_System_Array_GetLowerBound (MonoArray *arr, gint32 dimension)
{
	gint32 rank = arr->obj.vtable->klass->rank;

	if ((dimension < 0) || (dimension >= rank)) {
		mono_set_pending_exception (mono_get_exception_index_out_of_range ());
		return 0;
	}
	
	if (arr->bounds == NULL)
		return 0;
	
	return arr->bounds [dimension].lower_bound;
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
ves_icall_System_Array_GetGenericValueImpl (MonoArray *arr, guint32 pos, gpointer value)
{
	MonoClass *ac;
	gint32 esize;
	gpointer *ea;

	ac = (MonoClass *)arr->obj.vtable->klass;

	esize = mono_array_element_size (ac);
	ea = (gpointer*)((char*)arr->vector + (pos * esize));

	mono_gc_memmove_atomic (value, ea, esize);
}

ICALL_EXPORT void
ves_icall_System_Array_SetGenericValueImpl (MonoArray *arr, guint32 pos, gpointer value)
{
	MonoClass *ac, *ec;
	gint32 esize;
	gpointer *ea;

	ac = (MonoClass *)arr->obj.vtable->klass;
	ec = ac->element_class;

	esize = mono_array_element_size (ac);
	ea = (gpointer*)((char*)arr->vector + (pos * esize));

	if (MONO_TYPE_IS_REFERENCE (&ec->byval_arg)) {
		g_assert (esize == sizeof (gpointer));
		mono_gc_wbarrier_generic_store (ea, *(MonoObject **)value);
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
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray (MonoArrayHandle array, MonoClassField *field_handle, MonoError *error)
{
	error_init (error);

	MonoClass *klass = mono_handle_class (array);
	guint32 size = mono_array_element_size (klass);
	MonoType *type = mono_type_get_underlying_type (&klass->element_class->byval_arg);
	int align;
	const char *field_data;

	if (MONO_TYPE_IS_REFERENCE (type) || type->type == MONO_TYPE_VALUETYPE) {
		mono_error_set_argument (error, "array", "Cannot initialize array of non-primitive type");
		return;
	}

	if (!(field_handle->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA)) {
		mono_error_set_argument (error, "field_handle", "Field '%s' doesn't have an RVA", mono_field_get_name (field_handle));
		return;
	}

	size *= MONO_HANDLE_GETVAL(array, max_length);
	field_data = mono_field_get_data (field_handle);

	if (size > mono_type_size (field_handle->type, &align)) {
		mono_error_set_argument (error, "field_handle", "Field not large enough to fill array");
		return;
	}

#if G_BYTE_ORDER != G_LITTLE_ENDIAN
#define SWAP(n) {								\
	guint ## n *data = (guint ## n *) mono_array_addr (MONO_HANDLE_RAW(array), char, 0); \
	guint ## n *src = (guint ## n *) field_data; 				\
	int i,									\
	    nEnt = (size / sizeof(guint ## n));					\
										\
	for (i = 0; i < nEnt; i++) {						\
		data[i] = read ## n (&src[i]);					\
	} 									\
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
		memcpy (mono_array_addr (MONO_HANDLE_RAW(array), char, 0), field_data, size);
		break;
	}
#else
	memcpy (mono_array_addr (MONO_HANDLE_RAW(array), char, 0), field_data, size);
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
	else {
		MonoError error;
		MonoObject *ret = mono_object_clone_checked (obj, &error);
		mono_error_set_pending_exception (&error);

		return ret;
	}
}

ICALL_EXPORT void
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunClassConstructor (MonoType *handle)
{
	MonoError error;
	MonoClass *klass;
	MonoVTable *vtable;

	MONO_CHECK_ARG_NULL (handle,);

	klass = mono_class_from_mono_type (handle);
	MONO_CHECK_ARG (handle, klass,);

	if (mono_class_is_gtd (klass))
		return;

	vtable = mono_class_vtable_full (mono_domain_get (), klass, &error);
	if (!is_ok (&error)) {
		mono_error_set_pending_exception (&error);
		return;
	}

	/* This will call the type constructor */
	if (!mono_runtime_class_init_full (vtable, &error))
		mono_error_set_pending_exception (&error);
}

ICALL_EXPORT void
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunModuleConstructor (MonoImage *image)
{
	MonoError error;

	mono_image_check_for_module_cctor (image);
	if (image->has_module_cctor) {
		MonoClass *module_klass = mono_class_get_checked (image, MONO_TOKEN_TYPE_DEF | 1, &error);
		if (!mono_error_ok (&error)) {
			mono_error_set_pending_exception (&error);
			return;
		}
		/*It's fine to raise the exception here*/
		MonoVTable * vtable = mono_class_vtable_full (mono_domain_get (), module_klass, &error);
		if (!is_ok (&error)) {
			mono_error_set_pending_exception (&error);
			return;
		}
		if (!mono_runtime_class_init_full (vtable, &error))
			mono_error_set_pending_exception (&error);
	}
}

ICALL_EXPORT MonoBoolean
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_SufficientExecutionStack (void)
{
#if defined(TARGET_WIN32) || defined(HOST_WIN32)
	// It does not work on win32
#elif defined(TARGET_ANDROID)
	// No need for now
#else
	guint8 *stack_addr;
	guint8 *current;
	size_t stack_size;
	int min_size;
	MonoInternalThread *thread;

	mono_thread_info_get_stack_bounds (&stack_addr, &stack_size);
	/* if we have no info we are optimistic and assume there is enough room */
	if (!stack_addr)
		return TRUE;

	thread = mono_thread_internal_current ();
	// .net seems to check that at least 50% of stack is available
	min_size = thread->stack_size / 2;

	// TODO: It's not always set
	if (!min_size)
		return TRUE;

	current = (guint8 *)&stack_addr;
	if (current > stack_addr) {
		if ((current - stack_addr) < min_size)
			return FALSE;
	} else {
		if (current - (stack_addr - stack_size) < min_size)
			return FALSE;
	}
#endif
	return TRUE;
}

ICALL_EXPORT MonoObject *
ves_icall_System_Object_MemberwiseClone (MonoObject *this_obj)
{
	MonoError error;
	MonoObject *ret = mono_object_clone_checked (this_obj, &error);
	mono_error_set_pending_exception (&error);

	return ret;
}

ICALL_EXPORT gint32
ves_icall_System_ValueType_InternalGetHashCode (MonoObject *this_obj, MonoArray **fields)
{
	MonoError error;
	MonoClass *klass;
	MonoObject **values = NULL;
	MonoObject *o;
	int count = 0;
	gint32 result = (int)(gsize)mono_defaults.int32_class;
	MonoClassField* field;
	gpointer iter;

	klass = mono_object_class (this_obj);

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
			result ^= *(gint32*)((guint8*)this_obj + field->offset);
			break;
		case MONO_TYPE_STRING: {
			MonoString *s;
			s = *(MonoString**)((guint8*)this_obj + field->offset);
			if (s != NULL)
				result ^= mono_string_hash (s);
			break;
		}
		default:
			if (!values)
				values = g_newa (MonoObject*, mono_class_num_fields (klass));
			o = mono_field_get_value_object_checked (mono_object_domain (this_obj), field, this_obj, &error);
			if (!is_ok (&error)) {
				mono_error_set_pending_exception (&error);
				return 0;
			}
			values [count++] = o;
		}
	}

	if (values) {
		int i;
		MonoArray *fields_arr = mono_array_new_checked (mono_domain_get (), mono_defaults.object_class, count, &error);
		if (mono_error_set_pending_exception (&error))
			return 0;
		mono_gc_wbarrier_generic_store (fields, (MonoObject*) fields_arr);
		for (i = 0; i < count; ++i)
			mono_array_setref (*fields, i, values [i]);
	} else {
		*fields = NULL;
	}
	return result;
}

ICALL_EXPORT MonoBoolean
ves_icall_System_ValueType_Equals (MonoObject *this_obj, MonoObject *that, MonoArray **fields)
{
	MonoError error;
	MonoClass *klass;
	MonoObject **values = NULL;
	MonoObject *o;
	MonoClassField* field;
	gpointer iter;
	int count = 0;

	MONO_CHECK_ARG_NULL (that, FALSE);

	if (this_obj->vtable != that->vtable)
		return FALSE;

	klass = mono_object_class (this_obj);

	if (klass->enumtype && mono_class_enum_basetype (klass) && mono_class_enum_basetype (klass)->type == MONO_TYPE_I4)
		return (*(gint32*)((guint8*)this_obj + sizeof (MonoObject)) == *(gint32*)((guint8*)that + sizeof (MonoObject)));

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
			if (*((guint8*)this_obj + field->offset) != *((guint8*)that + field->offset))
				return FALSE;
			break;
		case MONO_TYPE_U2:
		case MONO_TYPE_I2:
		case MONO_TYPE_CHAR:
			if (*(gint16*)((guint8*)this_obj + field->offset) != *(gint16*)((guint8*)that + field->offset))
				return FALSE;
			break;
		case MONO_TYPE_U4:
		case MONO_TYPE_I4:
			if (*(gint32*)((guint8*)this_obj + field->offset) != *(gint32*)((guint8*)that + field->offset))
				return FALSE;
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			if (*(gint64*)((guint8*)this_obj + field->offset) != *(gint64*)((guint8*)that + field->offset))
				return FALSE;
			break;
		case MONO_TYPE_R4:
			if (*(float*)((guint8*)this_obj + field->offset) != *(float*)((guint8*)that + field->offset))
				return FALSE;
			break;
		case MONO_TYPE_R8:
			if (*(double*)((guint8*)this_obj + field->offset) != *(double*)((guint8*)that + field->offset))
				return FALSE;
			break;


		case MONO_TYPE_STRING: {
			MonoString *s1, *s2;
			guint32 s1len, s2len;
			s1 = *(MonoString**)((guint8*)this_obj + field->offset);
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
			o = mono_field_get_value_object_checked (mono_object_domain (this_obj), field, this_obj, &error);
			if (!is_ok (&error)) {
				mono_error_set_pending_exception (&error);
				return FALSE;
			}
			values [count++] = o;
			o = mono_field_get_value_object_checked (mono_object_domain (this_obj), field, that, &error);
			if (!is_ok (&error)) {
				mono_error_set_pending_exception (&error);
				return FALSE;
			}
			values [count++] = o;
		}

		if (klass->enumtype)
			/* enums only have one non-static field */
			break;
	}

	if (values) {
		int i;
		MonoArray *fields_arr = mono_array_new_checked (mono_domain_get (), mono_defaults.object_class, count, &error);
		if (mono_error_set_pending_exception (&error))
			return FALSE;
		mono_gc_wbarrier_generic_store (fields, (MonoObject*) fields_arr);
		for (i = 0; i < count; ++i)
			mono_array_setref_fast (*fields, i, values [i]);
		return FALSE;
	} else {
		return TRUE;
	}
}

ICALL_EXPORT MonoReflectionTypeHandle
ves_icall_System_Object_GetType (MonoObjectHandle obj, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (obj);
	MonoClass *klass = mono_handle_class (obj);
#ifndef DISABLE_REMOTING
	if (mono_class_is_transparent_proxy (klass)) {
		MonoTransparentProxyHandle proxy_obj = MONO_HANDLE_CAST (MonoTransparentProxy, obj);
		MonoRemoteClass *remote_class = MONO_HANDLE_GETVAL (proxy_obj, remote_class);
		MonoType *proxy_type = &remote_class->proxy_class->byval_arg;
		return mono_type_get_object_handle (domain, proxy_type, error);
	} else
#endif
		return mono_type_get_object_handle (domain, &klass->byval_arg, error);
}

static gboolean
get_executing (MonoMethod *m, gint32 no, gint32 ilo, gboolean managed, gpointer data)
{
	MonoMethod **dest = (MonoMethod **)data;

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
	MonoMethod **dest = (MonoMethod **)data;

	/* skip unmanaged frames */
	if (!managed)
		return FALSE;

	if (m->wrapper_type != MONO_WRAPPER_NONE)
		return FALSE;

	if (m == *dest) {
		*dest = NULL;
		return FALSE;
	}

	if (m->klass->image == mono_defaults.corlib && !strcmp (m->klass->name_space, "System.Reflection"))
		return FALSE;

	if (!(*dest)) {
		*dest = m;
		return TRUE;
	}
	return FALSE;
}

static gboolean
get_caller_no_system_or_reflection (MonoMethod *m, gint32 no, gint32 ilo, gboolean managed, gpointer data)
{
	MonoMethod **dest = (MonoMethod **)data;

	/* skip unmanaged frames */
	if (!managed)
		return FALSE;

	if (m->wrapper_type != MONO_WRAPPER_NONE)
		return FALSE;

	if (m == *dest) {
		*dest = NULL;
		return FALSE;
	}

	if (m->klass->image == mono_defaults.corlib && ((!strcmp (m->klass->name_space, "System.Reflection"))
													|| (!strcmp (m->klass->name_space, "System"))))
		return FALSE;

	if (!(*dest)) {
		*dest = m;
		return TRUE;
	}
	return FALSE;
}

static MonoReflectionTypeHandle
type_from_parsed_name (MonoTypeNameParse *info, MonoBoolean ignoreCase, MonoAssembly **caller_assembly, MonoError *error)
{
	MonoMethod *m, *dest;

	MonoType *type = NULL;
	MonoAssembly *assembly = NULL;
	gboolean type_resolve = FALSE;
	MonoImage *rootimage = NULL;

	error_init (error);

	/*
	 * We must compute the calling assembly as type loading must happen under a metadata context.
	 * For example. The main assembly is a.exe and Type.GetType is called from dir/b.dll. Without
	 * the metadata context (basedir currently) set to dir/b.dll we won't be able to load a dir/c.dll.
	 */
	m = mono_method_get_last_managed ();
	dest = m;
	if (m && m->klass->image != mono_defaults.corlib) {
		/* Happens with inlining */
	} else {
		/* Ugly hack: type_from_parsed_name is called from
		 * System.Type.internal_from_name, which is called most
		 * directly from System.Type.GetType(string,bool,bool) but
		 * also indirectly from places such as
		 * System.Type.GetType(string,func,func) (via
		 * System.TypeNameParser.GetType and System.TypeSpec.Resolve)
		 * so we need to skip over all of those to find the true caller.
		 *
		 * It would be nice if we had stack marks.
		 */
		mono_stack_walk_no_il (get_caller_no_system_or_reflection, &dest);
		if (!dest)
			dest = m;
	}

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
		rootimage = assembly->image;
	} else {
		g_warning (G_STRLOC);
	}
	*caller_assembly = assembly;

	if (info->assembly.name)
		assembly = mono_assembly_load (&info->assembly, assembly ? assembly->basedir : NULL, NULL);

	if (assembly) {
		/* When loading from the current assembly, AppDomain.TypeResolve will not be called yet */
		type = mono_reflection_get_type_checked (rootimage, assembly->image, info, ignoreCase, &type_resolve, error);
		if (!is_ok (error))
			goto fail;
	}

	// XXXX - aleksey -
	//  Say we're looking for System.Generic.Dict<int, Local>
	//  we FAIL the get type above, because S.G.Dict isn't in assembly->image.  So we drop down here.
	//  but then we FAIL AGAIN because now we pass null as the image and the rootimage and everything
	//  is messed up when we go to construct the Local as the type arg...
	//
	// By contrast, if we started with Mine<System.Generic.Dict<int, Local>> we'd go in with assembly->image
	// as the root and then even the detour into generics would still not screw us when we went to load Local.
	if (!info->assembly.name && !type) {
		/* try mscorlib */
		type = mono_reflection_get_type_checked (rootimage, NULL, info, ignoreCase, &type_resolve, error);
		if (!is_ok (error))
			goto fail;
	}
	if (assembly && !type && type_resolve) {
		type_resolve = FALSE; /* This will invoke TypeResolve if not done in the first 'if' */
		type = mono_reflection_get_type_checked (rootimage, assembly->image, info, ignoreCase, &type_resolve, error);
		if (!is_ok (error))
			goto fail;
	}

	if (!type) 
		goto fail;

	return mono_type_get_object_handle (mono_domain_get (), type, error);
fail:
	return MONO_HANDLE_NEW (MonoReflectionType, NULL);
}

ICALL_EXPORT MonoReflectionTypeHandle
ves_icall_System_Type_internal_from_name (MonoStringHandle name,
					  MonoBoolean throwOnError,
					  MonoBoolean ignoreCase,
					  MonoError *error)
{
	error_init (error);
	MonoTypeNameParse info;
	gboolean parsedOk;
	MonoAssembly *caller_assembly;
	MonoReflectionTypeHandle type = MONO_HANDLE_NEW (MonoReflectionType, NULL);

	char *str = mono_string_handle_to_utf8 (name, error);
	if (!is_ok (error))
		goto leave;

	parsedOk = mono_reflection_parse_type (str, &info);

	/* mono_reflection_parse_type() mangles the string */
	if (!parsedOk) {
		mono_reflection_free_type_info (&info);
		if (throwOnError)
			mono_error_set_argument (error, "typeName", "failed parse: %s", str);
		goto leave;
	}

	MONO_HANDLE_ASSIGN (type, type_from_parsed_name (&info, ignoreCase, &caller_assembly, error));

	if (!is_ok (error)) {
		mono_reflection_free_type_info (&info);
		goto leave;
	}

	if (MONO_HANDLE_IS_NULL (type)) {
		if (throwOnError) {
			char *tname = info.name_space ? g_strdup_printf ("%s.%s", info.name_space, info.name) : g_strdup (info.name);
			char *aname;
			if (info.assembly.name)
				aname = mono_stringify_assembly_name (&info.assembly);
			else if (caller_assembly)
				aname = mono_stringify_assembly_name (mono_assembly_get_name (caller_assembly));
			else
				aname = g_strdup ("");
			mono_error_set_type_load_name (error, tname, aname, "");
		}
		mono_reflection_free_type_info (&info);
		goto leave;
	}
	
leave:
	g_free (str);
	if (!is_ok (error)) {
		if (!throwOnError)
			mono_error_cleanup (error);
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
	}

	return type;
}


ICALL_EXPORT MonoReflectionTypeHandle
ves_icall_System_Type_internal_from_handle (MonoType *handle, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = mono_domain_get (); 

	return mono_type_get_object_handle (domain, handle, error);
}

ICALL_EXPORT MonoType*
ves_icall_Mono_RuntimeClassHandle_GetTypeFromClass (MonoClass *klass)
{
	return mono_class_get_type (klass);
}

ICALL_EXPORT void
ves_icall_Mono_RuntimeGPtrArrayHandle_GPtrArrayFree (GPtrArray *ptr_array)
{
	g_ptr_array_free (ptr_array, TRUE);
}

ICALL_EXPORT void
ves_icall_Mono_SafeStringMarshal_GFree (void *c_str)
{
	g_free (c_str);
}

ICALL_EXPORT char*
ves_icall_Mono_SafeStringMarshal_StringToUtf8 (MonoString *s)
{
	MonoError error;
	char *res = mono_string_to_utf8_checked (s, &error);
	mono_error_set_pending_exception (&error);
	return res;
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
ves_icall_type_GetTypeCodeInternal (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	int t = type->type;

	if (type->byref)
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
		MonoClass *klass = type->data.klass;
		
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
			MonoClass *klass =  type->data.klass;
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
ves_icall_RuntimeTypeHandle_type_is_assignable_from (MonoReflectionTypeHandle ref_type, MonoReflectionTypeHandle ref_c, MonoError *error)
{
	error_init (error);

	g_assert (!MONO_HANDLE_IS_NULL (ref_type));
	
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type (type);
	MonoType *ctype = MONO_HANDLE_GETVAL (ref_c, type);
	MonoClass *klassc = mono_class_from_mono_type (ctype);

	if (type->byref ^ ctype->byref)
		return FALSE;

	if (type->byref) {
		MonoType *t = mono_type_get_underlying_type_ignore_byref (type);
		MonoType *ot = mono_type_get_underlying_type_ignore_byref (ctype);

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
ves_icall_RuntimeTypeHandle_IsInstanceOfType (MonoReflectionTypeHandle ref_type, MonoObjectHandle obj, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type (type);
	mono_class_init_checked (klass, error);
	return_val_if_nok (error, FALSE);
	MonoObjectHandle inst = mono_object_handle_isinst (obj, klass, error);
	return_val_if_nok (error, FALSE);
	return !MONO_HANDLE_IS_NULL (inst);
}

ICALL_EXPORT guint32
ves_icall_RuntimeTypeHandle_GetAttributes (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type (type);
	return mono_class_get_flags (klass);
}

ICALL_EXPORT MonoReflectionMarshalAsAttributeHandle
ves_icall_System_Reflection_FieldInfo_get_marshal_info (MonoReflectionFieldHandle field_h, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (field_h);
	MonoClassField *field = MONO_HANDLE_GETVAL (field_h, field);
	MonoClass *klass = field->parent;

	MonoGenericClass *gklass = mono_class_try_get_generic_class (klass);
	if (mono_class_is_gtd (klass) ||
	    (gklass && gklass->context.class_inst->is_open))
		return MONO_HANDLE_CAST (MonoReflectionMarshalAsAttribute, NULL_HANDLE);

	MonoType *ftype = mono_field_get_type (field);
	if (ftype && !(ftype->attrs & FIELD_ATTRIBUTE_HAS_FIELD_MARSHAL))
		return MONO_HANDLE_CAST (MonoReflectionMarshalAsAttribute, NULL_HANDLE);

	MonoMarshalType *info = mono_marshal_load_type_info (klass);

	for (int i = 0; i < info->num_fields; ++i) {
		if (info->fields [i].field == field) {
			if (!info->fields [i].mspec)
				return MONO_HANDLE_CAST (MonoReflectionMarshalAsAttribute, NULL_HANDLE);
			else {
				return mono_reflection_marshal_as_attribute_from_marshal_spec (domain, klass, info->fields [i].mspec, error);
			}
		}
	}

	return MONO_HANDLE_CAST (MonoReflectionMarshalAsAttribute, NULL_HANDLE);
}

ICALL_EXPORT MonoReflectionFieldHandle
ves_icall_System_Reflection_FieldInfo_internal_from_handle_type (MonoClassField *handle, MonoType *type, MonoError *error)
{
	MonoClass *klass;

	g_assert (handle);

	error_init (error);

	if (!type) {
		klass = handle->parent;
	} else {
		klass = mono_class_from_mono_type (type);

		gboolean found = klass == handle->parent || mono_class_has_parent (klass, handle->parent);

		if (!found)
			/* The managed code will throw the exception */
			return MONO_HANDLE_CAST (MonoReflectionField, NULL_HANDLE);
	}

	return mono_field_get_object_handle (mono_domain_get (), klass, handle, error);
}

ICALL_EXPORT MonoReflectionEventHandle
ves_icall_System_Reflection_EventInfo_internal_from_handle_type (MonoEvent *handle, MonoType *type, MonoError *error)
{
	MonoClass *klass;

	g_assert (handle);

	error_init (error);

	if (!type) {
		klass = handle->parent;
	} else {
		klass = mono_class_from_mono_type (type);

		gboolean found = klass == handle->parent || mono_class_has_parent (klass, handle->parent);
		if (!found)
			/* Managed code will throw an exception */
			return MONO_HANDLE_CAST (MonoReflectionEvent, NULL_HANDLE);
	}

	return mono_event_get_object_handle (mono_domain_get (), klass, handle, error);
}


ICALL_EXPORT MonoReflectionPropertyHandle
ves_icall_System_Reflection_PropertyInfo_internal_from_handle_type (MonoProperty *handle, MonoType *type, MonoError *error)
{
	error_init (error);
	MonoClass *klass;

	g_assert (handle);

	if (!type) {
		klass = handle->parent;
	} else {
		klass = mono_class_from_mono_type (type);

		gboolean found = klass == handle->parent || mono_class_has_parent (klass, handle->parent);
		if (!found)
			/* Managed code will throw an exception */
			return MONO_HANDLE_CAST (MonoReflectionProperty, NULL_HANDLE);
	}

	return mono_property_get_object_handle (mono_domain_get (), klass, handle, error);
}

ICALL_EXPORT MonoArrayHandle
ves_icall_System_Reflection_FieldInfo_GetTypeModifiers (MonoReflectionFieldHandle field_h, MonoBoolean optional, MonoError *error)
{
	error_init (error);
	MonoClassField *field = MONO_HANDLE_GETVAL (field_h, field);

	MonoType *type = mono_field_get_type_checked (field, error);
	if (!is_ok (error))
		return MONO_HANDLE_CAST (MonoArray, NULL_HANDLE);

	return type_array_from_modifiers (field->parent->image, type, optional, error);
}

ICALL_EXPORT int
vell_icall_get_method_attributes (MonoMethod *method)
{
	return method->flags;
}

ICALL_EXPORT void
ves_icall_get_method_info (MonoMethod *method, MonoMethodInfo *info, MonoError *error)
{
	MonoDomain *domain = mono_domain_get ();

	MonoMethodSignature* sig = mono_method_signature_checked (method, error);
	return_if_nok (error);

	MonoReflectionTypeHandle rt = mono_type_get_object_handle (domain, &method->klass->byval_arg, error);
	return_if_nok (error);

	MONO_STRUCT_SETREF (info, parent, MONO_HANDLE_RAW (rt));

	MONO_HANDLE_ASSIGN (rt, mono_type_get_object_handle (domain, sig->ret, error));
	return_if_nok (error);

	MONO_STRUCT_SETREF (info, ret, MONO_HANDLE_RAW (rt));

	info->attrs = method->flags;
	info->implattrs = method->iflags;
	guint32 callconv;
	if (sig->call_convention == MONO_CALL_DEFAULT)
		callconv = sig->sentinelpos >= 0 ? 2 : 1;
	else {
		if (sig->call_convention == MONO_CALL_VARARG || sig->sentinelpos >= 0)
			callconv = 2;
		else
			callconv = 1;
	}
	callconv |= (sig->hasthis << 5) | (sig->explicit_this << 6);
	info->callconv = callconv;
}

ICALL_EXPORT MonoArrayHandle
ves_icall_System_Reflection_MonoMethodInfo_get_parameter_info (MonoMethod *method, MonoReflectionMethodHandle member, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = mono_domain_get (); 

	MonoReflectionTypeHandle reftype = MONO_HANDLE_NEW (MonoReflectionType, NULL);
	MONO_HANDLE_GET (reftype, member, reftype);
	MonoClass *klass = NULL;
	if (!MONO_HANDLE_IS_NULL (reftype))
		klass = mono_class_from_mono_type (MONO_HANDLE_GETVAL (reftype, type));
	return mono_param_get_objects_internal (domain, method, klass, error);
}

ICALL_EXPORT MonoReflectionMarshalAsAttributeHandle
ves_icall_System_MonoMethodInfo_get_retval_marshal (MonoMethod *method, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = mono_domain_get (); 
	MonoReflectionMarshalAsAttributeHandle res = MONO_HANDLE_NEW (MonoReflectionMarshalAsAttribute, NULL);

	MonoMarshalSpec **mspecs = g_new (MonoMarshalSpec*, mono_method_signature (method)->param_count + 1);
	mono_method_get_marshal_info (method, mspecs);

	if (mspecs [0]) {
		MONO_HANDLE_ASSIGN (res, mono_reflection_marshal_as_attribute_from_marshal_spec (domain, method->klass, mspecs [0], error));
		if (!is_ok (error))
			goto leave;
	}
		
leave:
	for (int i = mono_method_signature (method)->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);

	return res;
}

ICALL_EXPORT gint32
ves_icall_MonoField_GetFieldOffset (MonoReflectionField *field)
{
	MonoClass *parent = field->field->parent;
	mono_class_setup_fields (parent);

	return field->field->offset - sizeof (MonoObject);
}

ICALL_EXPORT MonoReflectionTypeHandle
ves_icall_MonoField_GetParentType (MonoReflectionFieldHandle field, MonoBoolean declaring, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (field);
	MonoClass *parent;

	if (declaring) {
		MonoClassField *f = MONO_HANDLE_GETVAL (field, field);
		parent = f->parent;
	} else {
		parent = MONO_HANDLE_GETVAL (field, klass);
	}

	return mono_type_get_object_handle (domain, &parent->byval_arg, error);
}

ICALL_EXPORT MonoObject *
ves_icall_MonoField_GetValueInternal (MonoReflectionField *field, MonoObject *obj)
{	
	MonoError error;
	MonoClass *fklass = field->klass;
	MonoClassField *cf = field->field;
	MonoDomain *domain = mono_object_domain (field);

	if (fklass->image->assembly->ref_only) {
		mono_set_pending_exception (mono_get_exception_invalid_operation (
					"It is illegal to get the value on a field on a type loaded using the ReflectionOnly methods."));
		return NULL;
	}

	if (mono_security_core_clr_enabled () &&
	    !mono_security_core_clr_ensure_reflection_access_field (cf, &error)) {
		mono_error_set_pending_exception (&error);
		return NULL;
	}

	MonoObject * result = mono_field_get_value_object_checked (domain, cf, obj, &error);
	mono_error_set_pending_exception (&error);
	return result;
}

ICALL_EXPORT void
ves_icall_MonoField_SetValueInternal (MonoReflectionFieldHandle field, MonoObjectHandle obj, MonoObjectHandle value, MonoError  *error)
{
	MonoClassField *cf = MONO_HANDLE_GETVAL (field, field);

	MonoClass *field_klass = MONO_HANDLE_GETVAL (field, klass);
	if (field_klass->image->assembly->ref_only) {
		mono_error_set_invalid_operation (error, "It is illegal to set the value on a field on a type loaded using the ReflectionOnly methods.");
		return;
	}

	if (mono_security_core_clr_enabled () &&
	    !mono_security_core_clr_ensure_reflection_access_field (cf, error)) {
		return;
	}

	MonoType *type = mono_field_get_type_checked (cf, error);
	return_if_nok (error);

	gboolean isref = FALSE;
	uint32_t value_gchandle = 0;
	gchar *v = NULL;
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
			isref = FALSE;
			if (!MONO_HANDLE_IS_NULL (value))
				v = mono_object_handle_pin_unbox (value, &value_gchandle);
			break;
		case MONO_TYPE_STRING:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			/* Do nothing */
			isref = TRUE;
			break;
		case MONO_TYPE_GENERICINST: {
			MonoGenericClass *gclass = type->data.generic_class;
			g_assert (!gclass->context.class_inst->is_open);

			if (mono_class_is_nullable (mono_class_from_mono_type (type))) {
				MonoClass *nklass = mono_class_from_mono_type (type);

				/* 
				 * Convert the boxed vtype into a Nullable structure.
				 * This is complicated by the fact that Nullables have
				 * a variable structure.
				 */
				MonoObjectHandle nullable = MONO_HANDLE_NEW (MonoObject, mono_object_new_checked (mono_domain_get (), nklass, error));
				return_if_nok (error);

				uint32_t nullable_gchandle = 0;
				guint8 *nval = mono_object_handle_pin_unbox (nullable, &nullable_gchandle);
				mono_nullable_init_from_handle (nval, value, nklass);

				isref = FALSE;
				value_gchandle = nullable_gchandle;
				v = (gchar*)nval;
			}
			else {
				isref = !gclass->container_class->valuetype;
				if (!isref && !MONO_HANDLE_IS_NULL (value)) {
					v = mono_object_handle_pin_unbox (value, &value_gchandle);
				};
			}
			break;
		}
		default:
			g_error ("type 0x%x not handled in "
				 "ves_icall_FieldInfo_SetValueInternal", type->type);
			return;
		}
	}

	/* either value is a reference type, or it's a value type and we pinned
	 * it and v points to the payload. */
	g_assert ((isref && v == NULL && value_gchandle == 0) ||
		  (!isref && v != NULL && value_gchandle != 0) ||
		  (!isref && v == NULL && value_gchandle == 0));

	if (type->attrs & FIELD_ATTRIBUTE_STATIC) {
		MonoVTable *vtable = mono_class_vtable_full (MONO_HANDLE_DOMAIN (field), cf->parent, error);
		if (!is_ok (error))
			goto leave;

		if (!vtable->initialized) {
			if (!mono_runtime_class_init_full (vtable, error))
				goto leave;
		}
		if (isref)
			mono_field_static_set_value (vtable, cf, MONO_HANDLE_RAW (value)); /* FIXME make mono_field_static_set_value work with handles for value */
		else
			mono_field_static_set_value (vtable, cf, v);
	} else {

		if (isref)
			MONO_HANDLE_SET_FIELD_REF (obj, cf, value);
		else
			mono_field_set_value (MONO_HANDLE_RAW (obj), cf, v); /* FIXME: make mono_field_set_value take a handle for obj */
	}
leave:
	if (value_gchandle)
		mono_gchandle_free (value_gchandle);
}

ICALL_EXPORT void
ves_icall_System_RuntimeFieldHandle_SetValueDirect (MonoReflectionField *field, MonoReflectionType *field_type, MonoTypedRef *obj, MonoObject *value, MonoReflectionType *context_type)
{
	MonoClassField *f;

	g_assert (field);
	g_assert (obj);
	g_assert (value);

	f = field->field;
	if (!MONO_TYPE_ISSTRUCT (&f->parent->byval_arg)) {
		mono_set_pending_exception (mono_get_exception_not_implemented (NULL));
		return;
	}

	if (MONO_TYPE_IS_REFERENCE (f->type))
		mono_copy_value (f->type, (guint8*)obj->value + f->offset - sizeof (MonoObject), value, FALSE);
	else
		mono_copy_value (f->type, (guint8*)obj->value + f->offset - sizeof (MonoObject), mono_object_unbox (value), FALSE);
}

ICALL_EXPORT MonoObject *
ves_icall_MonoField_GetRawConstantValue (MonoReflectionField *rfield)
{	
	MonoObject *o = NULL;
	MonoClassField *field = rfield->field;
	MonoClass *klass;
	MonoDomain *domain = mono_object_domain (rfield);
	gchar *v;
	MonoTypeEnum def_type;
	const char *def_value;
	MonoType *t;
	MonoError error;

	mono_class_init (field->parent);

	t = mono_field_get_type_checked (field, &error);
	if (!mono_error_ok (&error)) {
		mono_error_set_pending_exception (&error);
		return NULL;
	}

	if (!(t->attrs & FIELD_ATTRIBUTE_HAS_DEFAULT)) {
		mono_set_pending_exception (mono_get_exception_invalid_operation (NULL));
		return NULL;
	}

	if (image_is_dynamic (field->parent->image)) {
		MonoClass *klass = field->parent;
		int fidx = field - klass->fields;
		MonoFieldDefaultValue *def_values = mono_class_get_field_def_values (klass);

		g_assert (def_values);
		def_type = def_values [fidx].def_type;
		def_value = def_values [fidx].data;

		if (def_type == MONO_TYPE_END) {
			mono_set_pending_exception (mono_get_exception_invalid_operation (NULL));
			return NULL;
		}
	} else {
		def_value = mono_class_get_field_default_value (field, &def_type);
		/* FIXME, maybe we should try to raise TLE if field->parent is broken */
		if (!def_value) {
			mono_set_pending_exception (mono_get_exception_invalid_operation (NULL));
			return NULL;
		}
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
		o = mono_object_new_checked (domain, klass, &error);
		if (!mono_error_ok (&error)) {
			mono_error_set_pending_exception (&error);
			return NULL;
		}
		v = ((gchar *) o) + sizeof (MonoObject);
		mono_get_constant_value_from_blob (domain, def_type, def_value, v, &error);
		if (mono_error_set_pending_exception (&error))
			return NULL;
		break;
	}
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:
		mono_get_constant_value_from_blob (domain, def_type, def_value, &o, &error);
		if (mono_error_set_pending_exception (&error))
			return NULL;
		break;
	default:
		g_assert_not_reached ();
	}

	return o;
}

ICALL_EXPORT MonoReflectionTypeHandle
ves_icall_MonoField_ResolveType (MonoReflectionFieldHandle ref_field, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (ref_field);
	MonoClassField *field = MONO_HANDLE_GETVAL (ref_field, field);
	MonoType *type = mono_field_get_type_checked (field, error);
	if (!is_ok (error)) {
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
	}
	return mono_type_get_object_handle (domain, type, error);
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
ves_icall_MonoPropertyInfo_get_property_info (MonoReflectionPropertyHandle property, MonoPropertyInfo *info, PInfo req_info, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (property); 
	const MonoProperty *pproperty = MONO_HANDLE_GETVAL (property, property);

	if ((req_info & PInfo_ReflectedType) != 0) {
		MonoClass *klass = MONO_HANDLE_GETVAL (property, klass);
		MonoReflectionTypeHandle rt = mono_type_get_object_handle (domain, &klass->byval_arg, error);
		return_if_nok (error);

		MONO_STRUCT_SETREF (info, parent, MONO_HANDLE_RAW (rt));
	}
	if ((req_info & PInfo_DeclaringType) != 0) {
		MonoReflectionTypeHandle rt = mono_type_get_object_handle (domain, &pproperty->parent->byval_arg, error);
		return_if_nok (error);

		MONO_STRUCT_SETREF (info, declaring_type, MONO_HANDLE_RAW (rt));
	}

	if ((req_info & PInfo_Name) != 0) {
		MonoStringHandle name = mono_string_new_handle (domain, pproperty->name, error);
		return_if_nok (error);

		MONO_STRUCT_SETREF (info, name, MONO_HANDLE_RAW (name));
	}

	if ((req_info & PInfo_Attributes) != 0)
		info->attrs = pproperty->attrs;

	if ((req_info & PInfo_GetMethod) != 0) {
		MonoClass *property_klass = MONO_HANDLE_GETVAL (property, klass);
		MonoReflectionMethodHandle rm;
		if (pproperty->get &&
		    (((pproperty->get->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) != METHOD_ATTRIBUTE_PRIVATE) ||
		     pproperty->get->klass == property_klass)) {
			rm = mono_method_get_object_handle (domain, pproperty->get, property_klass, error);
			return_if_nok (error);
		} else {
			rm = MONO_HANDLE_NEW (MonoReflectionMethod, NULL);
		}

		MONO_STRUCT_SETREF (info, get, MONO_HANDLE_RAW (rm));
	}
	if ((req_info & PInfo_SetMethod) != 0) {
		MonoClass *property_klass = MONO_HANDLE_GETVAL (property, klass);
		MonoReflectionMethodHandle rm;
		if (pproperty->set &&
		    (((pproperty->set->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) != METHOD_ATTRIBUTE_PRIVATE) ||
		     pproperty->set->klass == property_klass)) {
			rm =  mono_method_get_object_handle (domain, pproperty->set, property_klass, error);
			return_if_nok (error);
		} else {
			rm = MONO_HANDLE_NEW (MonoReflectionMethod, NULL);
		}

		MONO_STRUCT_SETREF (info, set, MONO_HANDLE_RAW (rm));
	}
	/* 
	 * There may be other methods defined for properties, though, it seems they are not exposed 
	 * in the reflection API 
	 */
}

static gboolean
add_event_other_methods_to_array (MonoDomain *domain, MonoMethod *m, MonoArrayHandle dest, int i, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoReflectionMethodHandle rm = mono_method_get_object_handle (domain, m, NULL, error);
	if (!is_ok (error))
		goto leave;
	MONO_HANDLE_ARRAY_SETREF (dest, i, rm);
leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

ICALL_EXPORT void
ves_icall_MonoEventInfo_get_event_info (MonoReflectionMonoEventHandle ref_event, MonoEventInfo *info, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (ref_event); 

	MonoClass *klass = MONO_HANDLE_GETVAL (ref_event, klass);
	MonoEvent *event = MONO_HANDLE_GETVAL (ref_event, event);

	MonoReflectionTypeHandle rt = mono_type_get_object_handle (domain, &klass->byval_arg, error);
	return_if_nok (error);
	MONO_STRUCT_SETREF (info, reflected_type, MONO_HANDLE_RAW (rt));

	rt = mono_type_get_object_handle (domain, &event->parent->byval_arg, error);
	return_if_nok (error);
	MONO_STRUCT_SETREF (info, declaring_type, MONO_HANDLE_RAW (rt));

	MonoStringHandle ev_name = mono_string_new_handle (domain, event->name, error);
	return_if_nok (error);
	MONO_STRUCT_SETREF (info, name, MONO_HANDLE_RAW (ev_name));

	info->attrs = event->attrs;

	MonoReflectionMethodHandle rm;
	if (event->add) {
		rm = mono_method_get_object_handle (domain, event->add, NULL, error);
		return_if_nok (error);
	} else {
		rm = MONO_HANDLE_NEW (MonoReflectionMethod, NULL);
	}

	MONO_STRUCT_SETREF (info, add_method, MONO_HANDLE_RAW (rm));

	if (event->remove) {
		rm = mono_method_get_object_handle (domain, event->remove, NULL, error);
		return_if_nok (error);
	} else {
		rm = MONO_HANDLE_NEW (MonoReflectionMethod, NULL);
	}

	MONO_STRUCT_SETREF (info, remove_method, MONO_HANDLE_RAW (rm));

	if (event->raise) {
		rm = mono_method_get_object_handle (domain, event->raise, NULL, error);
		return_if_nok (error);
	} else {
		rm = MONO_HANDLE_NEW (MonoReflectionMethod, NULL);
	}

	MONO_STRUCT_SETREF (info, raise_method, MONO_HANDLE_RAW (rm));

#ifndef MONO_SMALL_CONFIG
	if (event->other) {
		int i, n = 0;
		while (event->other [n])
			n++;
		MonoArrayHandle info_arr = mono_array_new_handle (domain, mono_defaults.method_info_class, n, error);
		return_if_nok (error);

		MONO_STRUCT_SETREF (info, other_methods, MONO_HANDLE_RAW  (info_arr));

		for (i = 0; i < n; i++)
			if (!add_event_other_methods_to_array (domain, event->other [i], info_arr, i, error))
				return;
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
	MonoArrayHandle iface_array;
	MonoGenericContext *context;
	MonoError *error;
	MonoDomain *domain;
	int next_idx;
} FillIfaceArrayData;

static void
fill_iface_array (gpointer key, gpointer value, gpointer user_data)
{
	HANDLE_FUNCTION_ENTER ();
	FillIfaceArrayData *data = (FillIfaceArrayData *)user_data;
	MonoClass *ic = (MonoClass *)key;
	MonoType *ret = &ic->byval_arg, *inflated = NULL;
	MonoError *error = data->error;

	if (!is_ok (error))
		goto leave;

	if (data->context && mono_class_is_ginst (ic) && mono_class_get_generic_class (ic)->context.class_inst->is_open) {
		inflated = ret = mono_class_inflate_generic_type_checked (ret, data->context, error);
		if (!is_ok (error))
			goto leave;
	}

	MonoReflectionTypeHandle rt = mono_type_get_object_handle (data->domain, ret, error);
	if (!is_ok (error))
		goto leave;

	MONO_HANDLE_ARRAY_SETREF (data->iface_array, data->next_idx, rt);
	data->next_idx++;

	if (inflated)
		mono_metadata_free_type (inflated);
leave:
	HANDLE_FUNCTION_RETURN ();
}

static guint
get_interfaces_hash (gconstpointer v1)
{
	MonoClass *k = (MonoClass*)v1;

	return k->type_token;
}

ICALL_EXPORT MonoArrayHandle
ves_icall_RuntimeType_GetInterfaces (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type (type);

	GHashTable *iface_hash = g_hash_table_new (get_interfaces_hash, NULL);

	MonoGenericContext *context = NULL;
	if (mono_class_is_ginst (klass) && mono_class_get_generic_class (klass)->context.class_inst->is_open) {
		context = mono_class_get_context (klass);
		klass = mono_class_get_generic_class (klass)->container_class;
	}

	for (MonoClass *parent = klass; parent; parent = parent->parent) {
		mono_class_setup_interfaces (parent, error);
		if (!is_ok (error))
			goto fail;
		collect_interfaces (parent, iface_hash, error);
		if (!is_ok (error))
			goto fail;
	}

	MonoDomain *domain = MONO_HANDLE_DOMAIN (ref_type);

	int len = g_hash_table_size (iface_hash);
	if (len == 0) {
		g_hash_table_destroy (iface_hash);
		if (!domain->empty_types) {
			domain->empty_types = mono_array_new_cached (domain, mono_defaults.runtimetype_class, 0, error);
			if (!is_ok (error))
				goto fail;
		}
		return MONO_HANDLE_NEW (MonoArray, domain->empty_types);
	}

	FillIfaceArrayData data;
	data.iface_array = MONO_HANDLE_NEW (MonoArray, mono_array_new_cached (domain, mono_defaults.runtimetype_class, len, error));
	if (!is_ok (error))
		goto fail;
	data.context = context;
	data.error = error;
	data.domain = domain;
	data.next_idx = 0;

	g_hash_table_foreach (iface_hash, fill_iface_array, &data);

	if (!is_ok (error))
		goto fail;

	g_hash_table_destroy (iface_hash);
	return data.iface_array;

fail:
	g_hash_table_destroy (iface_hash);
	return MONO_HANDLE_CAST (MonoArray, NULL_HANDLE);
}

static gboolean
set_interface_map_data_method_object (MonoDomain *domain, MonoMethod *method, MonoClass *iclass, int ioffset, MonoClass *klass, MonoArrayHandle targets, MonoArrayHandle methods, int i, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoReflectionMethodHandle member = mono_method_get_object_handle (domain, method, iclass, error);
	if (!is_ok (error))
		goto leave;

	MONO_HANDLE_ARRAY_SETREF (methods, i, member);

	MONO_HANDLE_ASSIGN (member, mono_method_get_object_handle (domain, klass->vtable [i + ioffset], klass, error));
	if (!is_ok (error))
		goto leave;

	MONO_HANDLE_ARRAY_SETREF (targets, i, member);
		
leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

ICALL_EXPORT void
ves_icall_RuntimeType_GetInterfaceMapData (MonoReflectionTypeHandle ref_type, MonoReflectionTypeHandle ref_iface, MonoArrayHandleOut targets, MonoArrayHandleOut methods, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type (type);
	MonoType *iface = MONO_HANDLE_GETVAL (ref_iface, type);
	MonoClass *iclass = mono_class_from_mono_type (iface);

	mono_class_init_checked (klass, error);
	return_if_nok (error);
	mono_class_init_checked (iclass, error);
	return_if_nok (error);

	mono_class_setup_vtable (klass);

	gboolean variance_used;
	int ioffset = mono_class_interface_offset_with_variance (klass, iclass, &variance_used);
	if (ioffset == -1)
		return;

	int len = mono_class_num_methods (iclass);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (ref_type);
	MonoArrayHandle targets_arr = mono_array_new_handle (domain, mono_defaults.method_info_class, len, error);
	return_if_nok (error);
	MONO_HANDLE_ASSIGN (targets, targets_arr);

	MonoArrayHandle methods_arr = mono_array_new_handle (domain, mono_defaults.method_info_class, len, error);
	return_if_nok (error);
	MONO_HANDLE_ASSIGN (methods, methods_arr);

	MonoMethod* method;
	int i = 0;
	gpointer iter = NULL;
	while ((method = mono_class_get_methods (iclass, &iter))) {
		if (!set_interface_map_data_method_object (domain, method, iclass, ioffset, klass, targets, methods, i, error))
			return;
		i ++;
	}
}

ICALL_EXPORT void
ves_icall_RuntimeType_GetPacking (MonoReflectionTypeHandle ref_type, guint32 *packing, guint32 *size, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type (type);

	mono_class_init_checked (klass, error);
	if (!is_ok (error))
		return;

	if (image_is_dynamic (klass->image)) {
		MonoReflectionTypeBuilderHandle tb = MONO_HANDLE_CAST (MonoReflectionTypeBuilder, ref_type);
		*packing = MONO_HANDLE_GETVAL (tb, packing_size);
		*size = MONO_HANDLE_GETVAL (tb, class_size);
	} else {
		mono_metadata_packing_from_typedef (klass->image, klass->type_token, packing, size);
	}
}

ICALL_EXPORT MonoReflectionTypeHandle
ves_icall_RuntimeTypeHandle_GetElementType (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);

	MonoDomain *domain = MONO_HANDLE_DOMAIN (ref_type);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	if (!type->byref && type->type == MONO_TYPE_SZARRAY) {
		return mono_type_get_object_handle (domain, &type->data.klass->byval_arg, error);
	}

	MonoClass *klass = mono_class_from_mono_type (type);
	mono_class_init_checked (klass, error);
	if (!is_ok (error))
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);

	// GetElementType should only return a type for:
	// Array Pointer PassedByRef
	if (type->byref)
		return mono_type_get_object_handle (domain, &klass->byval_arg, error);
	else if (klass->element_class && MONO_CLASS_IS_ARRAY (klass))
		return mono_type_get_object_handle (domain, &klass->element_class->byval_arg, error);
	else if (klass->element_class && type->type == MONO_TYPE_PTR)
		return mono_type_get_object_handle (domain, &klass->element_class->byval_arg, error);
	else
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
}

ICALL_EXPORT MonoReflectionTypeHandle
ves_icall_RuntimeTypeHandle_GetBaseType (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);

	MonoDomain *domain = MONO_HANDLE_DOMAIN (ref_type);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	if (type->byref)
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);

	MonoClass *klass = mono_class_from_mono_type (type);
	if (!klass->parent)
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);

	return mono_type_get_object_handle (domain, &klass->parent->byval_arg, error);
}

ICALL_EXPORT MonoBoolean
ves_icall_RuntimeTypeHandle_IsPointer (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	return type->type == MONO_TYPE_PTR;
}

ICALL_EXPORT MonoBoolean
ves_icall_RuntimeTypeHandle_IsPrimitive (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	return (!type->byref && (((type->type >= MONO_TYPE_BOOLEAN) && (type->type <= MONO_TYPE_R8)) || (type->type == MONO_TYPE_I) || (type->type == MONO_TYPE_U)));
}

ICALL_EXPORT MonoBoolean
ves_icall_RuntimeTypeHandle_HasReferences (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass;

	klass = mono_class_from_mono_type (type);
	mono_class_init (klass);
	return klass->has_references;
}

ICALL_EXPORT MonoBoolean
ves_icall_RuntimeTypeHandle_IsByRef (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	return type->byref;
}

ICALL_EXPORT MonoBoolean
ves_icall_RuntimeTypeHandle_IsComObject (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type (type);
	mono_class_init_checked (klass, error);
	if (!is_ok (error))
		return FALSE;

	return mono_class_is_com_object (klass);
}

ICALL_EXPORT guint32
ves_icall_reflection_get_token (MonoObjectHandle obj, MonoError *error)
{
	error_init (error);
	return mono_reflection_get_token_checked (obj, error);
}

ICALL_EXPORT MonoReflectionModuleHandle
ves_icall_RuntimeTypeHandle_GetModule (MonoReflectionTypeHandle type, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (type);
	MonoType *t = MONO_HANDLE_GETVAL (type, type);
	MonoClass *klass = mono_class_from_mono_type (t);
	return mono_module_get_object_handle (domain, klass->image, error);
}

ICALL_EXPORT MonoReflectionAssemblyHandle
ves_icall_RuntimeTypeHandle_GetAssembly (MonoReflectionTypeHandle type, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = mono_domain_get (); 
	MonoType *t = MONO_HANDLE_GETVAL (type, type);
	MonoClass *klass = mono_class_from_mono_type (t);
	return mono_assembly_get_object_handle (domain, klass->image->assembly, error);
}

ICALL_EXPORT MonoReflectionTypeHandle
ves_icall_RuntimeType_get_DeclaringType (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = mono_domain_get ();
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass;

	if (type->byref)
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
	if (type->type == MONO_TYPE_VAR) {
		MonoGenericContainer *param = mono_type_get_generic_param_owner (type);
		klass = param ? param->owner.klass : NULL;
	} else if (type->type == MONO_TYPE_MVAR) {
		MonoGenericContainer *param = mono_type_get_generic_param_owner (type);
		klass = param ? param->owner.method->klass : NULL;
	} else {
		klass = mono_class_from_mono_type (type)->nested_in;
	}

	if (!klass)
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);

	return mono_type_get_object_handle (domain, &klass->byval_arg, error);
}

ICALL_EXPORT MonoStringHandle
ves_icall_RuntimeType_get_Name (MonoReflectionTypeHandle reftype, MonoError *error)
{
	MonoDomain *domain = mono_domain_get ();
	MonoType *type = MONO_HANDLE_RAW(reftype)->type; 
	MonoClass *klass = mono_class_from_mono_type (type);

	if (type->byref) {
		char *n = g_strdup_printf ("%s&", klass->name);
		MonoStringHandle res = mono_string_new_handle (domain, n, error);

		g_free (n);

		return res;
	} else {
		return mono_string_new_handle (domain, klass->name, error);
	}
}

ICALL_EXPORT MonoStringHandle
ves_icall_RuntimeType_get_Namespace (MonoReflectionTypeHandle type, MonoError *error)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *klass = mono_class_from_mono_type_handle (type);

	while (klass->nested_in)
		klass = klass->nested_in;

	if (klass->name_space [0] == '\0')
		return NULL_HANDLE_STRING;
	else
		return mono_string_new_handle (domain, klass->name_space, error);
}

ICALL_EXPORT gint32
ves_icall_RuntimeTypeHandle_GetArrayRank (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	if (type->type != MONO_TYPE_ARRAY && type->type != MONO_TYPE_SZARRAY) {
		mono_error_set_argument (error, "type", "Type must be an array type");
		return 0;
	}

	MonoClass *klass = mono_class_from_mono_type (type);

	return klass->rank;
}

static MonoArrayHandle
create_type_array (MonoDomain *domain, MonoBoolean runtimeTypeArray, int count, MonoError *error)
{
	return mono_array_new_handle (domain, runtimeTypeArray ? mono_defaults.runtimetype_class : mono_defaults.systemtype_class, count, error);
}

static gboolean
set_type_object_in_array (MonoDomain *domain, MonoType *type, MonoArrayHandle dest, int i, MonoError *error)
{
	HANDLE_FUNCTION_ENTER();
	error_init (error);
	MonoReflectionTypeHandle rt = mono_type_get_object_handle (domain, type, error);
	if (!is_ok (error))
		goto leave;

	MONO_HANDLE_ARRAY_SETREF (dest, i, rt);

leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

ICALL_EXPORT MonoArrayHandle
ves_icall_RuntimeType_GetGenericArguments (MonoReflectionTypeHandle ref_type, MonoBoolean runtimeTypeArray, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (ref_type);

	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type (type);

	MonoArrayHandle res = MONO_HANDLE_NEW (MonoArray, NULL);
	if (mono_class_is_gtd (klass)) {
		MonoGenericContainer *container = mono_class_get_generic_container (klass);
		MONO_HANDLE_ASSIGN (res, create_type_array (domain, runtimeTypeArray, container->type_argc, error));
		if (!is_ok (error))
			goto leave;
		for (int i = 0; i < container->type_argc; ++i) {
			MonoClass *pklass = mono_class_from_generic_parameter_internal (mono_generic_container_get_param (container, i));

			if (!set_type_object_in_array (domain, &pklass->byval_arg, res, i, error))
				goto leave;
		}
		
	} else if (mono_class_is_ginst (klass)) {
		MonoGenericInst *inst = mono_class_get_generic_class (klass)->context.class_inst;
		MONO_HANDLE_ASSIGN (res, create_type_array (domain, runtimeTypeArray, inst->type_argc, error));
		if (!is_ok (error))
			goto leave;
		for (int i = 0; i < inst->type_argc; ++i) {
			if (!set_type_object_in_array (domain, inst->type_argv [i], res, i, error))
				goto leave;
		}
	}

leave:
	return res;
}

ICALL_EXPORT gboolean
ves_icall_RuntimeTypeHandle_IsGenericTypeDefinition (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);

	if (!IS_MONOTYPE (MONO_HANDLE_RAW(ref_type)))
		return FALSE;

	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	if (type->byref)
		return FALSE;

	MonoClass *klass = mono_class_from_mono_type (type);
	return mono_class_is_gtd (klass);
}

ICALL_EXPORT MonoReflectionTypeHandle
ves_icall_RuntimeTypeHandle_GetGenericTypeDefinition_impl (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	MonoReflectionTypeHandle ret = MONO_HANDLE_NEW (MonoReflectionType, NULL);

	if (type->byref)
		goto leave;

	MonoClass *klass = mono_class_from_mono_type (type);

	if (mono_class_is_gtd (klass)) {
		/* check this one */
		MONO_HANDLE_ASSIGN (ret, ref_type);
		goto leave;
	}
	if (mono_class_is_ginst (klass)) {
		MonoClass *generic_class = mono_class_get_generic_class (klass)->container_class;

		guint32 ref_info_handle = mono_class_get_ref_info_handle (generic_class);
		
		if (generic_class->wastypebuilder && ref_info_handle) {
			MonoObjectHandle tb = mono_gchandle_get_target_handle (ref_info_handle);
			g_assert (!MONO_HANDLE_IS_NULL (tb));
			MONO_HANDLE_ASSIGN (ret, tb);
		} else {
			MonoDomain *domain = MONO_HANDLE_DOMAIN (ref_type);
			MONO_HANDLE_ASSIGN (ret, mono_type_get_object_handle (domain, &generic_class->byval_arg, error));
		}
	}
leave:
	return ret;
}

ICALL_EXPORT MonoReflectionTypeHandle
ves_icall_RuntimeType_MakeGenericType (MonoReflectionTypeHandle reftype, MonoArrayHandle type_array, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (reftype);

	g_assert (IS_MONOTYPE_HANDLE (reftype));
	MonoType *type = MONO_HANDLE_GETVAL (reftype, type);
	mono_class_init_checked (mono_class_from_mono_type (type), error);
	if (!is_ok (error))
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);

	int count = mono_array_handle_length (type_array);
	MonoType **types = g_new0 (MonoType *, count);

	MonoReflectionTypeHandle t = MONO_HANDLE_NEW (MonoReflectionType, NULL);
	for (int i = 0; i < count; i++) {
		MONO_HANDLE_ARRAY_GETREF (t, type_array, i);
		types [i] = MONO_HANDLE_GETVAL (t, type);
	}

	MonoType *geninst = mono_reflection_bind_generic_parameters (reftype, count, types, error);
	g_free (types);
	if (!geninst) {
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
	}

	MonoClass *klass = mono_class_from_mono_type (geninst);

	/*we might inflate to the GTD*/
	if (mono_class_is_ginst (klass) && !mono_verifier_class_is_valid_generic_instantiation (klass)) {
		mono_error_set_argument (error, "typeArguments", "Invalid generic arguments");
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
	}

	return mono_type_get_object_handle (domain, geninst, error);
}

ICALL_EXPORT gboolean
ves_icall_RuntimeTypeHandle_HasInstantiation (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoClass *klass;

	if (!IS_MONOTYPE (MONO_HANDLE_RAW (ref_type)))
		return FALSE;

	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	if (type->byref)
		return FALSE;

	klass = mono_class_from_mono_type (type);
	return mono_class_is_ginst (klass) || mono_class_is_gtd (klass);
}

ICALL_EXPORT gint32
ves_icall_RuntimeType_GetGenericParameterPosition (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	if (!IS_MONOTYPE_HANDLE (ref_type))
		return -1;
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	if (is_generic_parameter (type))
		return mono_type_get_generic_param_num (type);
	return -1;
}

ICALL_EXPORT MonoGenericParamInfo *
ves_icall_RuntimeTypeHandle_GetGenericParameterInfo (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	return mono_generic_param_info (type->data.generic_param);
}

ICALL_EXPORT MonoBoolean
ves_icall_RuntimeTypeHandle_IsGenericVariable (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL(ref_type, type);
	return is_generic_parameter (type);
}

ICALL_EXPORT MonoReflectionMethodHandle
ves_icall_RuntimeType_GetCorrespondingInflatedMethod (MonoReflectionTypeHandle ref_type, 
						      MonoReflectionMethodHandle generic,
						      MonoError *error)
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (ref_type);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type (type);
		
	mono_class_init_checked (klass, error);
	if (!is_ok (error))
		return MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE);

	MonoMethod *generic_method = MONO_HANDLE_GETVAL (generic, method);
	
	MonoReflectionMethodHandle ret = MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE);
	MonoMethod *method;
	gpointer iter = NULL;
	while ((method = mono_class_get_methods (klass, &iter))) {
                if (method->token == generic_method->token) {
			ret = mono_method_get_object_handle (domain, method, klass, error);
			if (!is_ok (error))
				return MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE);
		}
        }

	return ret;
}

ICALL_EXPORT MonoReflectionMethodHandle
ves_icall_RuntimeType_get_DeclaringMethod (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoReflectionMethodHandle ret = MONO_HANDLE_NEW (MonoReflectionMethod, NULL);

	if (type->byref || (type->type != MONO_TYPE_MVAR && type->type != MONO_TYPE_VAR)) {
		mono_error_set_invalid_operation (error, "DeclaringMethod can only be used on generic arguments");
		goto leave;
	}
	if (type->type == MONO_TYPE_VAR)
		goto leave;

	MonoMethod *method = mono_type_get_generic_param_owner (type)->owner.method;
	g_assert (method);

	MonoDomain *domain = MONO_HANDLE_DOMAIN (ref_type);

	MONO_HANDLE_ASSIGN (ret, mono_method_get_object_handle (domain, method, method->klass, error));
leave:
	return ret;
}

ICALL_EXPORT MonoBoolean
ves_icall_System_RuntimeType_IsTypeExportedToWindowsRuntime (MonoError *error)
{
	error_init (error);
	mono_error_set_not_implemented (error, "%s", "");
	return FALSE;
}

ICALL_EXPORT MonoBoolean
ves_icall_System_RuntimeType_IsWindowsRuntimeObjectType (MonoError *error)
{
	error_init (error);
	mono_error_set_not_implemented (error, "%s", "");
	return FALSE;
}

ICALL_EXPORT void
ves_icall_MonoMethod_GetPInvoke (MonoReflectionMethodHandle ref_method, int* flags, MonoStringHandleOut entry_point, MonoStringHandleOut dll_name, MonoError *error)
{
	MonoDomain *domain = mono_domain_get ();
	MonoMethod *method = MONO_HANDLE_GETVAL (ref_method, method);
	MonoImage *image = method->klass->image;
	MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *)method;
	MonoTableInfo *tables = image->tables;
	MonoTableInfo *im = &tables [MONO_TABLE_IMPLMAP];
	MonoTableInfo *mr = &tables [MONO_TABLE_MODULEREF];
	guint32 im_cols [MONO_IMPLMAP_SIZE];
	guint32 scope_token;
	const char *import = NULL;
	const char *scope = NULL;

	error_init (error);

	if (image_is_dynamic (image)) {
		MonoReflectionMethodAux *method_aux = 
			(MonoReflectionMethodAux *)g_hash_table_lookup (((MonoDynamicImage*)image)->method_aux_hash, method);
		if (method_aux) {
			import = method_aux->dllentry;
			scope = method_aux->dll;
		}

		if (!import || !scope) {
			mono_error_set_argument (error, "method", "System.Refleciton.Emit method with invalid pinvoke information");
			return;
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
	
	*flags = piinfo->piflags;
	MONO_HANDLE_ASSIGN (entry_point,  mono_string_new_handle (domain, import, error));
	return_if_nok (error);
	MONO_HANDLE_ASSIGN (dll_name, mono_string_new_handle (domain, scope, error));
}

ICALL_EXPORT MonoReflectionMethodHandle
ves_icall_MonoMethod_GetGenericMethodDefinition (MonoReflectionMethodHandle ref_method, MonoError *error)
{
	error_init (error);
	MonoMethod *method = MONO_HANDLE_GETVAL (ref_method, method);

	if (method->is_generic)
		return ref_method;

	if (!method->is_inflated)
		return MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE);

	MonoMethodInflated *imethod = (MonoMethodInflated *) method;

	MonoMethod *result = imethod->declaring;
	/* Not a generic method.  */
	if (!result->is_generic)
		return MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE);

	if (image_is_dynamic (method->klass->image)) {
		MonoDynamicImage *image = (MonoDynamicImage*)method->klass->image;

		/*
		 * FIXME: Why is this stuff needed at all ? Why can't the code below work for
		 * the dynamic case as well ?
		 */
		mono_image_lock ((MonoImage*)image);
		MonoReflectionMethodHandle res = MONO_HANDLE_NEW (MonoReflectionMethod, mono_g_hash_table_lookup (image->generic_def_objects, imethod));
		mono_image_unlock ((MonoImage*)image);

		if (!MONO_HANDLE_IS_NULL (res))
			return res;
	}

	if (imethod->context.class_inst) {
		MonoClass *klass = ((MonoMethod *) imethod)->klass;
		/*Generic methods gets the context of the GTD.*/
		if (mono_class_get_context (klass)) {
			result = mono_class_inflate_generic_method_full_checked (result, klass, mono_class_get_context (klass), error);
			return_val_if_nok (error, MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE));
		}
	}

	return mono_method_get_object_handle (MONO_HANDLE_DOMAIN (ref_method), result, NULL, error);
}

ICALL_EXPORT gboolean
ves_icall_MonoMethod_get_IsGenericMethod (MonoReflectionMethodHandle ref_method, MonoError *erro)
{
	MonoMethod *method = MONO_HANDLE_GETVAL (ref_method, method);
	return mono_method_signature (method)->generic_param_count != 0;
}

ICALL_EXPORT gboolean
ves_icall_MonoMethod_get_IsGenericMethodDefinition (MonoReflectionMethodHandle ref_method, MonoError *Error)
{
	MonoMethod *method = MONO_HANDLE_GETVAL (ref_method, method);
	return method->is_generic;
}

static gboolean
set_array_generic_argument_handle_inflated (MonoDomain *domain, MonoGenericInst *inst, int i, MonoArrayHandle arr, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoReflectionTypeHandle rt = mono_type_get_object_handle (domain, inst->type_argv [i], error);
	if (!is_ok (error))
		goto leave;
	MONO_HANDLE_ARRAY_SETREF (arr, i, rt);
leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

static gboolean
set_array_generic_argument_handle_gparam (MonoDomain *domain, MonoGenericContainer *container, int i, MonoArrayHandle arr, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoGenericParam *param = mono_generic_container_get_param (container, i);
	MonoClass *pklass = mono_class_from_generic_parameter_internal (param);
	MonoReflectionTypeHandle rt = mono_type_get_object_handle (domain, &pklass->byval_arg, error);
	if (!is_ok (error))
		goto leave;
	MONO_HANDLE_ARRAY_SETREF (arr, i, rt);
leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

ICALL_EXPORT MonoArrayHandle
ves_icall_MonoMethod_GetGenericArguments (MonoReflectionMethodHandle ref_method, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (ref_method);
	MonoMethod *method = MONO_HANDLE_GETVAL (ref_method, method);

	if (method->is_inflated) {
		MonoGenericInst *inst = mono_method_get_context (method)->method_inst;

		if (inst) {
			int count = inst->type_argc;
			MonoArrayHandle res = mono_array_new_handle (domain, mono_defaults.systemtype_class, count, error);
			return_val_if_nok (error, MONO_HANDLE_CAST (MonoArray, NULL_HANDLE));

			for (int i = 0; i < count; i++) {
				if (!set_array_generic_argument_handle_inflated (domain, inst, i, res, error))
					break;
			}
			return_val_if_nok (error, MONO_HANDLE_CAST (MonoArray, NULL_HANDLE));
			return res;
		}
	}

	int count = mono_method_signature (method)->generic_param_count;
	MonoArrayHandle res = mono_array_new_handle (domain, mono_defaults.systemtype_class, count, error);
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoArray, NULL_HANDLE));

	MonoGenericContainer *container = mono_method_get_generic_container (method);
	for (int i = 0; i < count; i++) {
		if (!set_array_generic_argument_handle_gparam (domain, container, i, res, error))
			break;
	}
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoArray, NULL_HANDLE));
	return res;
}

ICALL_EXPORT MonoObject *
ves_icall_InternalInvoke (MonoReflectionMethod *method, MonoObject *this_arg, MonoArray *params, MonoException **exc) 
{
	MonoError error;
	/* 
	 * Invoke from reflection is supposed to always be a virtual call (the API
	 * is stupid), mono_runtime_invoke_*() calls the provided method, allowing
	 * greater flexibility.
	 */
	MonoMethod *m = method->method;
	MonoMethodSignature *sig = mono_method_signature (m);
	MonoImage *image;
	int pcount;
	void *obj = this_arg;

	*exc = NULL;

	if (mono_security_core_clr_enabled () &&
	    !mono_security_core_clr_ensure_reflection_access_method (m, &error)) {
		mono_error_set_pending_exception (&error);
		return NULL;
	}

	if (!(m->flags & METHOD_ATTRIBUTE_STATIC)) {
		if (!mono_class_vtable_full (mono_object_domain (method), m->klass, &error)) {
			mono_error_cleanup (&error); /* FIXME does this make sense? */
			mono_gc_wbarrier_generic_store (exc, (MonoObject*) mono_class_get_exception_for_failure (m->klass));
			return NULL;
		}

		if (this_arg) {
			if (!mono_object_isinst_checked (this_arg, m->klass, &error)) {
				if (!is_ok (&error)) {
					mono_gc_wbarrier_generic_store (exc, (MonoObject*) mono_error_convert_to_exception (&error));
					return NULL;
				}
				char *this_name = mono_type_get_full_name (mono_object_get_class (this_arg));
				char *target_name = mono_type_get_full_name (m->klass);
				char *msg = g_strdup_printf ("Object of type '%s' doesn't match target type '%s'", this_name, target_name);
				mono_gc_wbarrier_generic_store (exc, (MonoObject*) mono_exception_from_name_msg (mono_defaults.corlib, "System.Reflection", "TargetException", msg));
				g_free (msg);
				g_free (target_name);
				g_free (this_name);
				return NULL;
			}
			m = mono_object_get_virtual_method (this_arg, m);
			/* must pass the pointer to the value for valuetype methods */
			if (m->klass->valuetype)
				obj = mono_object_unbox (this_arg);
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

	if (mono_class_is_abstract (m->klass) && !strcmp (m->name, ".ctor") && !this_arg) {
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
		MonoArray *arr;
		int i;
		uintptr_t *lengths;
		intptr_t *lower_bounds;
		pcount = mono_array_length (params);
		lengths = (uintptr_t *)alloca (sizeof (uintptr_t) * pcount);
		/* Note: the synthetized array .ctors have int32 as argument type */
		for (i = 0; i < pcount; ++i)
			lengths [i] = *(int32_t*) ((char*)mono_array_get (params, gpointer, i) + sizeof (MonoObject));

		if (m->klass->rank == 1 && sig->param_count == 2 && m->klass->element_class->rank) {
			/* This is a ctor for jagged arrays. MS creates an array of arrays. */
			arr = mono_array_new_full_checked (mono_object_domain (params), m->klass, lengths, NULL, &error);
			if (!mono_error_ok (&error)) {
				mono_error_set_pending_exception (&error);
				return NULL;
			}

			for (i = 0; i < mono_array_length (arr); ++i) {
				MonoArray *subarray = mono_array_new_full_checked (mono_object_domain (params), m->klass->element_class, &lengths [1], NULL, &error);
				if (!mono_error_ok (&error)) {
					mono_error_set_pending_exception (&error);
					return NULL;
				}
				mono_array_setref_fast (arr, i, subarray);
			}
			return (MonoObject*)arr;
		}

		if (m->klass->rank == pcount) {
			/* Only lengths provided. */
			arr = mono_array_new_full_checked (mono_object_domain (params), m->klass, lengths, NULL, &error);
			if (!mono_error_ok (&error)) {
				mono_error_set_pending_exception (&error);
				return NULL;
			}

			return (MonoObject*)arr;
		} else {
			g_assert (pcount == (m->klass->rank * 2));
			/* The arguments are lower-bound-length pairs */
			lower_bounds = (intptr_t *)g_alloca (sizeof (intptr_t) * pcount);

			for (i = 0; i < pcount / 2; ++i) {
				lower_bounds [i] = *(int32_t*) ((char*)mono_array_get (params, gpointer, (i * 2)) + sizeof (MonoObject));
				lengths [i] = *(int32_t*) ((char*)mono_array_get (params, gpointer, (i * 2) + 1) + sizeof (MonoObject));
			}

			arr = mono_array_new_full_checked (mono_object_domain (params), m->klass, lengths, lower_bounds, &error);
			if (!mono_error_ok (&error)) {
				mono_error_set_pending_exception (&error);
				return NULL;
			}

			return (MonoObject*)arr;
		}
	}
	MonoObject *result = mono_runtime_invoke_array_checked (m, obj, params, &error);
	mono_error_set_pending_exception (&error);
	return result;
}

#ifndef DISABLE_REMOTING
static void
internal_execute_field_getter (MonoDomain *domain, MonoObject *this_arg, MonoArray *params, MonoArray **outArgs, MonoError *error)
{
	error_init (error);
	MonoArray *out_args;
	MonoClass *k = mono_object_class (this_arg);
	MonoString *name;
	char *str;
			
	/* If this is a proxy, then it must be a CBO */
	if (mono_class_is_transparent_proxy (k)) {
		MonoTransparentProxy *tp = (MonoTransparentProxy*) this_arg;
		this_arg = tp->rp->unwrapped_server;
		g_assert (this_arg);
		k = mono_object_class (this_arg);
	}
			
	name = mono_array_get (params, MonoString *, 1);
	str = mono_string_to_utf8_checked (name, error);
	return_if_nok (error);
		
	do {
		MonoClassField* field = mono_class_get_field_from_name (k, str);
		if (field) {
			g_free (str);
			MonoClass *field_klass =  mono_class_from_mono_type (field->type);
			MonoObject *result;
			if (field_klass->valuetype) {
				result = mono_value_box_checked (domain, field_klass, (char *)this_arg + field->offset, error);
				return_if_nok (error);
			} else 
				result = (MonoObject *)*((gpointer *)((char *)this_arg + field->offset));

			out_args = mono_array_new_checked (domain, mono_defaults.object_class, 1, error);
			return_if_nok (error);
			mono_gc_wbarrier_generic_store (outArgs, (MonoObject*) out_args);
			mono_array_setref (out_args, 0, result);
			return;
		}
		k = k->parent;
	} while (k);

	g_free (str);
	g_assert_not_reached ();
}

static void
internal_execute_field_setter (MonoDomain *domain, MonoObject *this_arg, MonoArray *params, MonoArray **outArgs, MonoError *error)
{
	error_init (error);
	MonoArray *out_args;
	MonoClass *k = mono_object_class (this_arg);
	MonoString *name;
	guint32 size;
	gint32 align;
	char *str;
			
	/* If this is a proxy, then it must be a CBO */
	if (mono_class_is_transparent_proxy (k)) {
		MonoTransparentProxy *tp = (MonoTransparentProxy*) this_arg;
		this_arg = tp->rp->unwrapped_server;
		g_assert (this_arg);
		k = mono_object_class (this_arg);
	}
			
	name = mono_array_get (params, MonoString *, 1);
	str = mono_string_to_utf8_checked (name, error);
	return_if_nok (error);
		
	do {
		MonoClassField* field = mono_class_get_field_from_name (k, str);
		if (field) {
			g_free (str);
			MonoClass *field_klass =  mono_class_from_mono_type (field->type);
			MonoObject *val = (MonoObject *)mono_array_get (params, gpointer, 2);

			if (field_klass->valuetype) {
				size = mono_type_size (field->type, &align);
				g_assert (size == mono_class_value_size (field_klass, NULL));
				mono_gc_wbarrier_value_copy ((char *)this_arg + field->offset, (char*)val + sizeof (MonoObject), 1, field_klass);
			} else {
				mono_gc_wbarrier_set_field (this_arg, (char*)this_arg + field->offset, val);
			}

			out_args = mono_array_new_checked (domain, mono_defaults.object_class, 0, error);
			return_if_nok (error);
			mono_gc_wbarrier_generic_store (outArgs, (MonoObject*) out_args);
			return;
		}
				
		k = k->parent;
	} while (k);

	g_free (str);
	g_assert_not_reached ();
}

ICALL_EXPORT MonoObject *
ves_icall_InternalExecute (MonoReflectionMethod *method, MonoObject *this_arg, MonoArray *params, MonoArray **outArgs) 
{
	MonoError error;
	MonoDomain *domain = mono_object_domain (method); 
	MonoMethod *m = method->method;
	MonoMethodSignature *sig = mono_method_signature (m);
	MonoArray *out_args;
	MonoObject *result;
	int i, j, outarg_count = 0;

	if (m->klass == mono_defaults.object_class) {
		if (!strcmp (m->name, "FieldGetter")) {
			internal_execute_field_getter (domain, this_arg, params, outArgs, &error);
			mono_error_set_pending_exception (&error);
			return NULL;
		} else if (!strcmp (m->name, "FieldSetter")) {
			internal_execute_field_setter (domain, this_arg, params, outArgs, &error);
			mono_error_set_pending_exception (&error);
			return NULL;
		}
	}

	for (i = 0; i < mono_array_length (params); i++) {
		if (sig->params [i]->byref) 
			outarg_count++;
	}

	out_args = mono_array_new_checked (domain, mono_defaults.object_class, outarg_count, &error);
	if (mono_error_set_pending_exception (&error))
		return NULL;

	/* handle constructors only for objects already allocated */
	if (!strcmp (method->method->name, ".ctor"))
		g_assert (this_arg);

	/* This can be called only on MBR objects, so no need to unbox for valuetypes. */
	g_assert (!method->method->klass->valuetype);
	result = mono_runtime_invoke_array_checked (method->method, this_arg, params, &error);
	if (mono_error_set_pending_exception (&error))
		return NULL;

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
read_enum_value (const char *mem, int type)
{
	switch (type) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_U1:
		return *(guint8*)mem;
	case MONO_TYPE_I1:
		return *(gint8*)mem;
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U2:
		return read16 (mem);
	case MONO_TYPE_I2:
		return (gint16) read16 (mem);
	case MONO_TYPE_U4:
		return read32 (mem);
	case MONO_TYPE_I4:
		return (gint32) read32 (mem);
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
		return read64 (mem);
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
	case MONO_TYPE_I2:
	case MONO_TYPE_CHAR: {
		guint16 *p = (guint16 *)mem;
		*p = value;
		break;
	}
	case MONO_TYPE_U4:
	case MONO_TYPE_I4: {
		guint32 *p = (guint32 *)mem;
		*p = value;
		break;
	}
	case MONO_TYPE_U8:
	case MONO_TYPE_I8: {
		guint64 *p = (guint64 *)mem;
		*p = value;
		break;
	}
	default:
		g_assert_not_reached ();
	}
	return;
}

ICALL_EXPORT MonoObject *
ves_icall_System_Enum_ToObject (MonoReflectionType *enumType, guint64 value)
{
	MonoError error;
	MonoDomain *domain; 
	MonoClass *enumc;
	MonoObject *res;
	MonoType *etype;

	domain = mono_object_domain (enumType); 
	enumc = mono_class_from_mono_type (enumType->type);

	mono_class_init_checked (enumc, &error);
	if (mono_error_set_pending_exception (&error))
		return NULL;

	etype = mono_class_enum_basetype (enumc);

	res = mono_object_new_checked (domain, enumc, &error);
	if (mono_error_set_pending_exception (&error))
		return NULL;
	write_enum_value ((char *)res + sizeof (MonoObject), etype->type, value);

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
ves_icall_System_Enum_get_value (MonoObject *eobj)
{
	MonoError error;
	MonoObject *res;
	MonoClass *enumc;
	gpointer dst;
	gpointer src;
	int size;

	if (!eobj)
		return NULL;

	g_assert (eobj->vtable->klass->enumtype);
	
	enumc = mono_class_from_mono_type (mono_class_enum_basetype (eobj->vtable->klass));
	res = mono_object_new_checked (mono_object_domain (eobj), enumc, &error);
	if (mono_error_set_pending_exception (&error))
		return NULL;
	dst = (char *)res + sizeof (MonoObject);
	src = (char *)eobj + sizeof (MonoObject);
	size = mono_class_value_size (enumc, NULL);

	memcpy (dst, src, size);

	return res;
}

ICALL_EXPORT MonoReflectionType *
ves_icall_System_Enum_get_underlying_type (MonoReflectionType *type)
{
	MonoError error;
	MonoReflectionType *ret;
	MonoType *etype;
	MonoClass *klass;

	klass = mono_class_from_mono_type (type->type);
	mono_class_init_checked (klass, &error);
	if (mono_error_set_pending_exception (&error))
		return NULL;

	etype = mono_class_enum_basetype (klass);
	if (!etype) {
		mono_set_pending_exception (mono_get_exception_argument ("enumType", "Type provided must be an Enum."));
		return NULL;
	}

	ret = mono_type_get_object_checked (mono_object_domain (type), etype, &error);
	mono_error_set_pending_exception (&error);

	return ret;
}

ICALL_EXPORT int
ves_icall_System_Enum_compare_value_to (MonoObject *eobj, MonoObject *other)
{
	gpointer tdata = (char *)eobj + sizeof (MonoObject);
	gpointer odata = (char *)other + sizeof (MonoObject);
	MonoType *basetype = mono_class_enum_basetype (eobj->vtable->klass);
	g_assert (basetype);

	if (other == NULL)
		return 1;

	if (eobj->vtable->klass != other->vtable->klass)
		return 2;

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
			break;
	}
#undef COMPARE_ENUM_VALUES
	/* indicates that the enum was of an unsupported unerlying type */
	return 3;
}

ICALL_EXPORT int
ves_icall_System_Enum_get_hashcode (MonoObject *eobj)
{
	gpointer data = (char *)eobj + sizeof (MonoObject);
	MonoType *basetype = mono_class_enum_basetype (eobj->vtable->klass);
	g_assert (basetype);

	switch (basetype->type) {
		case MONO_TYPE_I1:	 {
			gint8 value = *((gint8*)data);
			return ((int)value ^ (int)value << 8);
		}
		case MONO_TYPE_U1:
			return *((guint8*)data);
		case MONO_TYPE_CHAR:
		case MONO_TYPE_U2:
			return *((guint16*)data);
		
		case MONO_TYPE_I2: {
			gint16 value = *((gint16*)data);
			return ((int)(guint16)value | (((int)value) << 16));
		}
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

static void
get_enum_field (MonoDomain *domain, MonoArrayHandle names, MonoArrayHandle values, int base_type, MonoClassField *field, guint* j, guint64 *previous_value, gboolean *sorted, MonoError *error)
{
	error_init (error);
	HANDLE_FUNCTION_ENTER();
	guint64 field_value;
	const char *p;
	MonoTypeEnum def_type;

	if (!(field->type->attrs & FIELD_ATTRIBUTE_STATIC))
		goto leave;
	if (strcmp ("value__", mono_field_get_name (field)) == 0)
		goto leave;
	if (mono_field_is_deleted (field))
		goto leave;
	MonoStringHandle name = mono_string_new_handle (domain, mono_field_get_name (field), error);
	if (!is_ok (error))
		goto leave;
	MONO_HANDLE_ARRAY_SETREF (names, *j, name);

	p = mono_class_get_field_default_value (field, &def_type);
	/* len = */ mono_metadata_decode_blob_size (p, &p);

	field_value = read_enum_value (p, base_type);
	MONO_HANDLE_ARRAY_SETVAL (values, guint64, *j, field_value);

	if (*previous_value > field_value)
		*sorted = FALSE;

	*previous_value = field_value;
	(*j)++;
leave:
	HANDLE_FUNCTION_RETURN();
}

ICALL_EXPORT MonoBoolean
ves_icall_System_Enum_GetEnumValuesAndNames (MonoReflectionTypeHandle type, MonoArrayHandleOut values, MonoArrayHandleOut names, MonoError *error)
{
	MonoDomain *domain = MONO_HANDLE_DOMAIN (type);
	MonoClass *enumc = mono_class_from_mono_type (MONO_HANDLE_RAW(type)->type);
	guint j = 0, nvalues;
	gpointer iter;
	MonoClassField *field;
	int base_type;
	guint64 previous_value = 0;
	gboolean sorted = TRUE;

	error_init (error);
	mono_class_init_checked (enumc, error);
	return_val_if_nok (error, FALSE);

	if (!enumc->enumtype) {
		mono_error_set_argument (error, "enumType", "Type provided must be an Enum.");
		return TRUE;
	}

	base_type = mono_class_enum_basetype (enumc)->type;

	nvalues = mono_class_num_fields (enumc) > 0 ? mono_class_num_fields (enumc) - 1 : 0;
	MONO_HANDLE_ASSIGN(names, mono_array_new_handle (domain, mono_defaults.string_class, nvalues, error));
	return_val_if_nok (error, FALSE);
	MONO_HANDLE_ASSIGN(values, mono_array_new_handle (domain, mono_defaults.uint64_class, nvalues, error));
	return_val_if_nok (error, FALSE);

	iter = NULL;
	while ((field = mono_class_get_fields (enumc, &iter))) {
		get_enum_field(domain, names, values, base_type, field, &j, &previous_value, &sorted, error);
		if (!is_ok (error))
			break;
	}
	return_val_if_nok (error, FALSE);

	return sorted;
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

ICALL_EXPORT GPtrArray*
ves_icall_RuntimeType_GetFields_native (MonoReflectionTypeHandle ref_type, char *utf8_name, guint32 bflags, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	if (type->byref) {
		return g_ptr_array_new ();
	}

	int (*compare_func) (const char *s1, const char *s2) = NULL;	
	compare_func = (bflags & BFLAGS_IgnoreCase) ? mono_utf8_strcasecmp : strcmp;

	MonoClass *startklass, *klass;
	klass = startklass = mono_class_from_mono_type (type);

	GPtrArray *ptr_array = g_ptr_array_sized_new (16);
	
handle_parent:	
	if (mono_class_has_failure (klass)) {
		mono_error_set_for_class_failure (error, klass);
		goto fail;
	}

	MonoClassField *field;
	gpointer iter = NULL;
	while ((field = mono_class_get_fields_lazy (klass, &iter))) {
		guint32 flags = mono_field_get_flags (field);
		int match = 0;
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

		if (utf8_name != NULL && compare_func (mono_field_get_name (field), utf8_name))
				continue;

		g_ptr_array_add (ptr_array, field);
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;

	return ptr_array;

fail:
	g_ptr_array_free (ptr_array, TRUE);
	return NULL;
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
mono_class_get_methods_by_name (MonoClass *klass, const char *name, guint32 bflags, gboolean ignore_case, gboolean allow_ctors, MonoError *error)
{
	GPtrArray *array;
	MonoClass *startklass;
	MonoMethod *method;
	gpointer iter;
	int match, nslots;
	/*FIXME, use MonoBitSet*/
	guint32 method_slots_default [8];
	guint32 *method_slots = NULL;
	int (*compare_func) (const char *s1, const char *s2) = NULL;

	array = g_ptr_array_new ();
	startklass = klass;
	error_init (error);

	if (name != NULL)
		compare_func = (ignore_case) ? mono_utf8_strcasecmp : strcmp;

	/* An optimization for calls made from Delegate:CreateDelegate () */
	if (klass->delegate && name && !strcmp (name, "Invoke") && (bflags == (BFLAGS_Public | BFLAGS_Static | BFLAGS_Instance))) {
		method = mono_get_delegate_invoke (klass);
		g_assert (method);

		g_ptr_array_add (array, method);
		return array;
	}

	mono_class_setup_methods (klass);
	mono_class_setup_vtable (klass);
	if (mono_class_has_failure (klass))
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
	if (mono_class_has_failure (klass))
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

	g_assert (mono_class_has_failure (klass));
	mono_error_set_for_class_failure (error, klass);
	return NULL;
}

ICALL_EXPORT GPtrArray*
ves_icall_RuntimeType_GetMethodsByName_native (MonoReflectionTypeHandle ref_type, const char *mname, guint32 bflags, MonoBoolean ignore_case, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	MonoClass *klass = mono_class_from_mono_type (type);
	if (type->byref) {
		return g_ptr_array_new ();
	}

	return mono_class_get_methods_by_name (klass, mname, bflags, ignore_case, FALSE, error);
}

ICALL_EXPORT GPtrArray*
ves_icall_RuntimeType_GetConstructors_native (MonoReflectionTypeHandle ref_type, guint32 bflags, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	if (type->byref) {
		return g_ptr_array_new ();
	}

	MonoClass *startklass, *klass;
	klass = startklass = mono_class_from_mono_type (type);

	mono_class_setup_methods (klass);
	if (mono_class_has_failure (klass)) {
		mono_error_set_for_class_failure (error, klass);
		return NULL;
	}
	

	GPtrArray *res_array = g_ptr_array_sized_new (4); /* FIXME, guestimating */

	MonoMethod *method;
	gpointer iter = NULL;
	while ((method = mono_class_get_methods (klass, &iter))) {
		int match = 0;
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
		g_ptr_array_add (res_array, method);
	}

	return res_array;
}

static guint
property_hash (gconstpointer data)
{
	MonoProperty *prop = (MonoProperty*)data;

	return g_str_hash (prop->name);
}

static gboolean
property_accessor_override (MonoMethod *method1, MonoMethod *method2)
{
	if (method1->slot != -1 && method1->slot == method2->slot)
		return TRUE;

	if (mono_class_get_generic_type_definition (method1->klass) == mono_class_get_generic_type_definition (method2->klass)) {
		if (method1->is_inflated)
			method1 = ((MonoMethodInflated*) method1)->declaring;
		if (method2->is_inflated)
			method2 = ((MonoMethodInflated*) method2)->declaring;
	}

	return mono_metadata_signature_equal (mono_method_signature (method1), mono_method_signature (method2));
}

static gboolean
property_equal (MonoProperty *prop1, MonoProperty *prop2)
{
	// Properties are hide-by-name-and-signature
	if (!g_str_equal (prop1->name, prop2->name))
		return FALSE;

	/* If we see a property in a generic method, we want to
	   compare the generic signatures, not the inflated signatures
	   because we might conflate two properties that were
	   distinct:

	   class Foo<T,U> {
	     public T this[T t] { getter { return t; } } // method 1
	     public U this[U u] { getter { return u; } } // method 2
	   }

	   If we see int Foo<int,int>::Item[int] we need to know if
	   the indexer came from method 1 or from method 2, and we
	   shouldn't conflate them.   (Bugzilla 36283)
	*/
	if (prop1->get && prop2->get && !property_accessor_override (prop1->get, prop2->get))
		return FALSE;

	if (prop1->set && prop2->set && !property_accessor_override (prop1->set, prop2->set))
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

ICALL_EXPORT GPtrArray*
ves_icall_RuntimeType_GetPropertiesByName_native (MonoReflectionTypeHandle ref_type, gchar *propname, guint32 bflags, MonoBoolean ignore_case, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);


	if (type->byref) {
		return g_ptr_array_new ();
	}

	
	MonoClass *startklass, *klass;
	klass = startklass = mono_class_from_mono_type (type);

	int (*compare_func) (const char *s1, const char *s2) = (ignore_case) ? mono_utf8_strcasecmp : strcmp;

	GPtrArray *res_array = g_ptr_array_sized_new (8); /*This the average for ASP.NET types*/

	GHashTable *properties = g_hash_table_new (property_hash, (GEqualFunc)property_equal);

handle_parent:
	mono_class_setup_methods (klass);
	mono_class_setup_vtable (klass);
	if (mono_class_has_failure (klass)) {
		mono_error_set_for_class_failure (error, klass);
		goto loader_error;
	}

	MonoProperty *prop;
	gpointer iter = NULL;
	while ((prop = mono_class_get_properties (klass, &iter))) {
		int match = 0;
		MonoMethod *method = prop->get;
		if (!method)
			method = prop->set;
		guint32 flags = 0;
		if (method)
			flags = method->flags;
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

		if (propname != NULL && compare_func (propname, prop->name))
			continue;
		
		if (g_hash_table_lookup (properties, prop))
			continue;

		g_ptr_array_add (res_array, prop);
		
		g_hash_table_insert (properties, prop, prop);
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;

	g_hash_table_destroy (properties);

	return res_array;


loader_error:
	if (properties)
		g_hash_table_destroy (properties);
	g_ptr_array_free (res_array, TRUE);

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

ICALL_EXPORT GPtrArray*
ves_icall_RuntimeType_GetEvents_native (MonoReflectionTypeHandle ref_type, char *utf8_name, guint32 bflags, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	if (type->byref) {
		return g_ptr_array_new ();
	}

	int (*compare_func) (const char *s1, const char *s2) = (bflags & BFLAGS_IgnoreCase) ? mono_utf8_strcasecmp : strcmp;

	GPtrArray *res_array = g_ptr_array_sized_new (4);

	MonoClass *startklass, *klass;
	klass = startklass = mono_class_from_mono_type (type);

	GHashTable *events = g_hash_table_new (event_hash, (GEqualFunc)event_equal);
handle_parent:
	mono_class_setup_methods (klass);
	mono_class_setup_vtable (klass);
	if (mono_class_has_failure (klass)) {
		mono_error_set_for_class_failure (error, klass);
		goto failure;
	}

	MonoEvent *event;
	gpointer iter = NULL;
	while ((event = mono_class_get_events (klass, &iter))) {
		int match = 0;
		MonoMethod *method = event->add;
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

		if (utf8_name != NULL && compare_func (event->name, utf8_name))
			continue;

		if (g_hash_table_lookup (events, event))
			continue;

		g_ptr_array_add (res_array, event); 

		g_hash_table_insert (events, event, event);
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = klass->parent))
		goto handle_parent;

	g_hash_table_destroy (events);

	return res_array;

failure:
	if (events != NULL)
		g_hash_table_destroy (events);

	g_ptr_array_free (res_array, TRUE);

	return NULL;
}

ICALL_EXPORT GPtrArray *
ves_icall_RuntimeType_GetNestedTypes_native (MonoReflectionTypeHandle ref_type, char *str, guint32 bflags, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	if (type->byref) {
		return g_ptr_array_new ();
	}

	MonoClass *klass = mono_class_from_mono_type (type);

	/*
	 * If a nested type is generic, return its generic type definition.
	 * Note that this means that the return value is essentially the set
	 * of nested types of the generic type definition of @klass.
	 *
	 * A note in MSDN claims that a generic type definition can have
	 * nested types that aren't generic.  In any case, the container of that
	 * nested type would be the generic type definition.
	 */
	if (mono_class_is_ginst (klass))
		klass = mono_class_get_generic_class (klass)->container_class;

	GPtrArray *res_array = g_ptr_array_new ();
	
	MonoClass *nested;
	gpointer iter = NULL;
	while ((nested = mono_class_get_nested_types (klass, &iter))) {
		int match = 0;
		if ((mono_class_get_flags (nested) & TYPE_ATTRIBUTE_VISIBILITY_MASK) == TYPE_ATTRIBUTE_NESTED_PUBLIC) {
			if (bflags & BFLAGS_Public)
				match++;
		} else {
			if (bflags & BFLAGS_NonPublic)
				match++;
		}
		if (!match)
			continue;

		if (str != NULL && strcmp (nested->name, str))
				continue;

		g_ptr_array_add (res_array, &nested->byval_arg);
	}

	return res_array;
}

static MonoType*
get_type_from_module_builder_module (MonoArrayHandle modules, int i, MonoTypeNameParse *info, MonoBoolean ignoreCase, gboolean *type_resolve, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoType *type = NULL;
	MonoReflectionModuleBuilderHandle mb = MONO_HANDLE_NEW (MonoReflectionModuleBuilder, NULL);
	MONO_HANDLE_ARRAY_GETREF (mb, modules, i);
	MonoDynamicImage *dynamic_image = MONO_HANDLE_GETVAL (mb, dynamic_image);
	type = mono_reflection_get_type_checked (&dynamic_image->image, &dynamic_image->image, info, ignoreCase, type_resolve, error);
	HANDLE_FUNCTION_RETURN_VAL (type);
}

static MonoType*
get_type_from_module_builder_loaded_modules (MonoArrayHandle loaded_modules, int i, MonoTypeNameParse *info, MonoBoolean ignoreCase, gboolean *type_resolve, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoType *type = NULL;
	MonoReflectionModuleHandle mod = MONO_HANDLE_NEW (MonoReflectionModule, NULL);
	MONO_HANDLE_ARRAY_GETREF (mod, loaded_modules, i);
	MonoImage *image = MONO_HANDLE_GETVAL (mod, image);
	type = mono_reflection_get_type_checked (image, image, info, ignoreCase, type_resolve, error);
	HANDLE_FUNCTION_RETURN_VAL (type);
}

ICALL_EXPORT MonoReflectionTypeHandle
ves_icall_System_Reflection_Assembly_InternalGetType (MonoReflectionAssemblyHandle assembly_h, MonoReflectionModuleHandle module, MonoStringHandle name, MonoBoolean throwOnError, MonoBoolean ignoreCase, MonoError *error)
{
	error_init (error);

	MonoTypeNameParse info;
	gboolean type_resolve;

	/* On MS.NET, this does not fire a TypeResolve event */
	type_resolve = TRUE;
	char *str = mono_string_handle_to_utf8 (name, error);
	if (!is_ok (error))
		goto fail;

	/*g_print ("requested type %s in %s\n", str, assembly->assembly->aname.name);*/
	if (!mono_reflection_parse_type (str, &info)) {
		g_free (str);
		mono_reflection_free_type_info (&info);
		if (throwOnError) {
			mono_error_set_argument (error, "name", "failed to parse the type");
			goto fail;
		}
		/*g_print ("failed parse\n");*/
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
	}

	if (info.assembly.name) {
		g_free (str);
		mono_reflection_free_type_info (&info);
		if (throwOnError) {
			/* 1.0 and 2.0 throw different exceptions */
			if (mono_defaults.generic_ilist_class)
				mono_error_set_argument (error, NULL, "Type names passed to Assembly.GetType() must not specify an assembly.");
			else
				mono_error_set_type_load_name (error, g_strdup (""), g_strdup (""), "Type names passed to Assembly.GetType() must not specify an assembly.");
			goto fail;
		}
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
	}

	MonoType *type = NULL;
	if (!MONO_HANDLE_IS_NULL (module)) {
		MonoImage *image = MONO_HANDLE_GETVAL (module, image);
		if (image) {
			type = mono_reflection_get_type_checked (image, image, &info, ignoreCase, &type_resolve, error);
			if (!is_ok (error)) {
				g_free (str);
				mono_reflection_free_type_info (&info);
				goto fail;
			}
		}
	}
	else {
		MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_h, assembly);
		if (assembly_is_dynamic (assembly)) {
			/* Enumerate all modules */
			MonoReflectionAssemblyBuilderHandle abuilder = MONO_HANDLE_NEW (MonoReflectionAssemblyBuilder, NULL);
			MONO_HANDLE_ASSIGN (abuilder, assembly_h);
			int i;

			MonoArrayHandle modules = MONO_HANDLE_NEW (MonoArray, NULL);
			MONO_HANDLE_GET (modules, abuilder, modules);
			if (!MONO_HANDLE_IS_NULL (modules)) {
				int n = mono_array_handle_length (modules);
				for (i = 0; i < n; ++i) {
					type = get_type_from_module_builder_module (modules, i, &info, ignoreCase, &type_resolve, error);
					if (!is_ok (error)) {
						g_free (str);
						mono_reflection_free_type_info (&info);
						goto fail;
					}
					if (type)
						break;
				}
			}

			MonoArrayHandle loaded_modules = MONO_HANDLE_NEW (MonoArray, NULL);
			MONO_HANDLE_GET (loaded_modules, abuilder, loaded_modules);
			if (!type && !MONO_HANDLE_IS_NULL (loaded_modules)) {
				int n = mono_array_handle_length (loaded_modules);
				for (i = 0; i < n; ++i) {
					type = get_type_from_module_builder_loaded_modules (loaded_modules, i, &info, ignoreCase, &type_resolve, error);

					if (!is_ok (error)) {
						g_free (str);
						mono_reflection_free_type_info (&info);
						goto fail;
					}
					if (type)
						break;
				}
			}
		}
		else {
			type = mono_reflection_get_type_checked (assembly->image, assembly->image, &info, ignoreCase, &type_resolve, error);
			if (!is_ok (error)) {
				g_free (str);
				mono_reflection_free_type_info (&info);
				goto fail;
			}
		}
	}
	g_free (str);
	mono_reflection_free_type_info (&info);

	if (!type) {
		if (throwOnError) {
			MonoError inner_error;
			char *typename = mono_string_handle_to_utf8 (name, &inner_error);
			mono_error_assert_ok (&inner_error);
			MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_h, assembly);
			char *assmname = mono_stringify_assembly_name (&assembly->aname);
			mono_error_set_type_load_name (error, typename, assmname, "%s", "");
			goto fail;
		}

		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
	}

	if (type->type == MONO_TYPE_CLASS) {
		MonoClass *klass = mono_type_get_class (type);

		/* need to report exceptions ? */
		if (throwOnError && mono_class_has_failure (klass)) {
			/* report SecurityException (or others) that occured when loading the assembly */
			mono_error_set_for_class_failure (error, klass);
			goto fail;
		}
	}

	/* g_print ("got it\n"); */
	return mono_type_get_object_handle (MONO_HANDLE_DOMAIN (assembly_h), type, error);
fail:
	g_assert (!is_ok (error));
	return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
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

ICALL_EXPORT MonoStringHandle
ves_icall_System_Reflection_Assembly_get_code_base (MonoReflectionAssemblyHandle assembly, MonoBoolean escaped, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (assembly);
	MonoAssembly *mass = MONO_HANDLE_GETVAL (assembly, assembly);
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

	mono_icall_make_platform_path (absolute);

	gchar *uri;
	if (escaped) {
		uri = g_filename_to_uri (absolute, NULL, NULL);
	} else {
		const gchar *prepend = mono_icall_get_file_path_prefix (absolute);
		uri = g_strconcat (prepend, absolute, NULL);
	}

	g_free (absolute);

	MonoStringHandle res;
	if (uri) {
		res = mono_string_new_handle (domain, uri, error);
		g_free (uri);
	} else {
		res = MONO_HANDLE_NEW (MonoString, NULL);
	}
	return res;
}

ICALL_EXPORT MonoBoolean
ves_icall_System_Reflection_Assembly_get_global_assembly_cache (MonoReflectionAssemblyHandle assembly, MonoError *error)
{
	error_init (error);
	MonoAssembly *mass = MONO_HANDLE_GETVAL (assembly,assembly);

	return mass->in_gac;
}

ICALL_EXPORT MonoReflectionAssemblyHandle
ves_icall_System_Reflection_Assembly_load_with_partial_name (MonoStringHandle mname, MonoObjectHandle evidence, MonoError *error)
{
	gchar *name;
	MonoImageOpenStatus status;
	MonoReflectionAssemblyHandle result = MONO_HANDLE_CAST (MonoReflectionAssembly, NULL_HANDLE);
	
	name = mono_string_handle_to_utf8 (mname, error);
	if (!is_ok (error))
		goto leave;
	MonoAssembly *res = mono_assembly_load_with_partial_name (name, &status);

	g_free (name);

	if (res == NULL)
		goto leave;
	result = mono_assembly_get_object_handle (mono_domain_get (), res, error);
leave:
	return result;
}

ICALL_EXPORT MonoStringHandle
ves_icall_System_Reflection_Assembly_get_location (MonoReflectionAssemblyHandle refassembly, MonoError *error)
{
	MonoDomain *domain = MONO_HANDLE_DOMAIN (refassembly);
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (refassembly, assembly);
	return mono_string_new_handle (domain, mono_image_get_filename (assembly->image), error);
}

ICALL_EXPORT MonoBoolean
ves_icall_System_Reflection_Assembly_get_ReflectionOnly (MonoReflectionAssemblyHandle assembly_h, MonoError *error)
{
	error_init (error);
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_h, assembly);
	return assembly->ref_only;
}

ICALL_EXPORT MonoStringHandle
ves_icall_System_Reflection_Assembly_InternalImageRuntimeVersion (MonoReflectionAssemblyHandle refassembly, MonoError *error)
{
	MonoDomain *domain = MONO_HANDLE_DOMAIN (refassembly);
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (refassembly, assembly);

	return mono_string_new_handle (domain, assembly->image->version, error);
}

ICALL_EXPORT MonoReflectionMethodHandle
ves_icall_System_Reflection_Assembly_get_EntryPoint (MonoReflectionAssemblyHandle assembly_h, MonoError *error) 
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (assembly_h);
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_h, assembly);
	MonoMethod *method;

	MonoReflectionMethodHandle res = MONO_HANDLE_NEW (MonoReflectionMethod, NULL);
	guint32 token = mono_image_get_entry_point (assembly->image);

	if (!token)
		goto leave;
	method = mono_get_method_checked (assembly->image, token, NULL, NULL, error);
	if (!is_ok (error))
		goto leave;

	MONO_HANDLE_ASSIGN (res, mono_method_get_object_handle (domain, method, NULL, error));
leave:
	return res;
}

ICALL_EXPORT MonoReflectionModuleHandle
ves_icall_System_Reflection_Assembly_GetManifestModuleInternal (MonoReflectionAssemblyHandle assembly, MonoError *error) 
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (assembly);
	MonoAssembly *a = MONO_HANDLE_GETVAL (assembly, assembly);
	return mono_module_get_object_handle (domain, a->image, error);
}

static gboolean
add_manifest_resource_name_to_array (MonoDomain *domain, MonoImage *image, MonoTableInfo *table, int i, MonoArrayHandle dest, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	const char *val = mono_metadata_string_heap (image, mono_metadata_decode_row_col (table, i, MONO_MANIFEST_NAME));
	MonoStringHandle str = mono_string_new_handle (domain, val, error);
	if (!is_ok (error))
		goto leave;
	MONO_HANDLE_ARRAY_SETREF (dest, i, str);
leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

ICALL_EXPORT MonoArrayHandle
ves_icall_System_Reflection_Assembly_GetManifestResourceNames (MonoReflectionAssemblyHandle assembly_h, MonoError *error) 
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (assembly_h);
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_h, assembly);
	MonoTableInfo *table = &assembly->image->tables [MONO_TABLE_MANIFESTRESOURCE];
	MonoArrayHandle result = mono_array_new_handle (domain, mono_defaults.string_class, table->rows, error);
	if (!is_ok (error))
		goto fail;
	int i;

	for (i = 0; i < table->rows; ++i) {
		if (!add_manifest_resource_name_to_array (domain, assembly->image, table, i, result, error))
			goto fail;
	}
	return result;
fail:
	return MONO_HANDLE_CAST (MonoArray, NULL_HANDLE);
}

ICALL_EXPORT MonoStringHandle
ves_icall_System_Reflection_Assembly_GetAotId (MonoError *error)
{
	char *guid = mono_runtime_get_aotid ();
	if (guid == NULL)
		return NULL;
	MonoStringHandle res = mono_string_new_handle (mono_domain_get (), guid, error);
	g_free (guid);
	return res;
}

static MonoAssemblyName*
create_referenced_assembly_name (MonoDomain *domain, MonoImage *image, MonoTableInfo *t, int i, MonoError *error)
{
	error_init (error);
	MonoAssemblyName *aname = g_new0 (MonoAssemblyName, 1);

	mono_assembly_get_assemblyref (image, i, aname);
	aname->hash_alg = ASSEMBLY_HASH_SHA1 /* SHA1 (default) */;
	/* name and culture are pointers into the image tables, but we need
	 * real malloc'd strings (so that we can g_free() them later from
	 * Mono.RuntimeMarshal.FreeAssemblyName) */
	aname->name = g_strdup (aname->name);
	aname->culture = g_strdup  (aname->culture);
	/* Don't need the hash value in managed */
	aname->hash_value = NULL;
	aname->hash_len = 0;
	g_assert (aname->public_key == NULL);
		
	/* note: this function doesn't return the codebase on purpose (i.e. it can
	   be used under partial trust as path information isn't present). */
	return aname;
}

ICALL_EXPORT GPtrArray*
ves_icall_System_Reflection_Assembly_InternalGetReferencedAssemblies (MonoReflectionAssemblyHandle assembly, MonoError *error) 
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (assembly);
	MonoAssembly *ass = MONO_HANDLE_GETVAL(assembly, assembly);
	MonoImage *image = ass->image;

	MonoTableInfo *t = &image->tables [MONO_TABLE_ASSEMBLYREF];
	int count = t->rows;

	GPtrArray *result = g_ptr_array_sized_new (count);

	for (int i = 0; i < count; i++) {
		MonoAssemblyName *aname = create_referenced_assembly_name (domain, image, t, i, error);
		if (!is_ok (error))
			break;
		g_ptr_array_add (result, aname);
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
ves_icall_System_Reflection_Assembly_GetManifestResourceInternal (MonoReflectionAssemblyHandle assembly_h, MonoStringHandle name, gint32 *size, MonoReflectionModuleHandleOut ref_module, MonoError *error) 
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (assembly_h);
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_h, assembly);
	MonoTableInfo *table = &assembly->image->tables [MONO_TABLE_MANIFESTRESOURCE];
	guint32 i;
	guint32 cols [MONO_MANIFEST_SIZE];
	guint32 impl, file_idx;
	const char *val;
	MonoImage *module;

	char *n = mono_string_handle_to_utf8 (name, error);
	return_val_if_nok (error, NULL);

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, cols, MONO_MANIFEST_SIZE);
		val = mono_metadata_string_heap (assembly->image, cols [MONO_MANIFEST_NAME]);
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

		module = mono_image_load_file_for_image_checked (assembly->image, file_idx, error);
		if (!is_ok (error) || !module)
			return NULL;
	}
	else
		module = assembly->image;

	
	MonoReflectionModuleHandle rm = mono_module_get_object_handle (domain, module, error);
	if (!is_ok (error))
		return NULL;
	MONO_HANDLE_ASSIGN (ref_module, rm);

	return (void*)mono_image_get_resource (module, cols [MONO_MANIFEST_OFFSET], (guint32*)size);
}

static gboolean
get_manifest_resource_info_internal (MonoReflectionAssemblyHandle assembly_h, MonoStringHandle name, MonoManifestResourceInfoHandle info, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	MonoDomain *domain = MONO_HANDLE_DOMAIN (assembly_h);
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_h, assembly);
	MonoTableInfo *table = &assembly->image->tables [MONO_TABLE_MANIFESTRESOURCE];
	int i;
	guint32 cols [MONO_MANIFEST_SIZE];
	guint32 file_cols [MONO_FILE_SIZE];
	const char *val;
	char *n;

	gboolean result = FALSE;
	
	n = mono_string_handle_to_utf8 (name, error);
	if (!is_ok (error))
		goto leave;

	for (i = 0; i < table->rows; ++i) {
		mono_metadata_decode_row (table, i, cols, MONO_MANIFEST_SIZE);
		val = mono_metadata_string_heap (assembly->image, cols [MONO_MANIFEST_NAME]);
		if (strcmp (val, n) == 0)
			break;
	}
	g_free (n);
	if (i == table->rows)
		goto leave;

	if (!cols [MONO_MANIFEST_IMPLEMENTATION]) {
		MONO_HANDLE_SETVAL (info, location, guint32, RESOURCE_LOCATION_EMBEDDED | RESOURCE_LOCATION_IN_MANIFEST);
	}
	else {
		switch (cols [MONO_MANIFEST_IMPLEMENTATION] & MONO_IMPLEMENTATION_MASK) {
		case MONO_IMPLEMENTATION_FILE:
			i = cols [MONO_MANIFEST_IMPLEMENTATION] >> MONO_IMPLEMENTATION_BITS;
			table = &assembly->image->tables [MONO_TABLE_FILE];
			mono_metadata_decode_row (table, i - 1, file_cols, MONO_FILE_SIZE);
			val = mono_metadata_string_heap (assembly->image, file_cols [MONO_FILE_NAME]);
			MONO_HANDLE_SET (info, filename, mono_string_new_handle (domain, val, error));
			if (file_cols [MONO_FILE_FLAGS] & FILE_CONTAINS_NO_METADATA)
				MONO_HANDLE_SETVAL (info, location, guint32, 0);
			else
				MONO_HANDLE_SETVAL (info, location, guint32, RESOURCE_LOCATION_EMBEDDED);
			break;

		case MONO_IMPLEMENTATION_ASSEMBLYREF:
			i = cols [MONO_MANIFEST_IMPLEMENTATION] >> MONO_IMPLEMENTATION_BITS;
			mono_assembly_load_reference (assembly->image, i - 1);
			if (assembly->image->references [i - 1] == REFERENCE_MISSING) {
				mono_error_set_assembly_load (error, NULL, "Assembly %d referenced from assembly %s not found ", i - 1, assembly->image->name);
				goto leave;
			}
			MonoReflectionAssemblyHandle assm_obj = mono_assembly_get_object_handle (mono_domain_get (), assembly->image->references [i - 1], error);
			if (!is_ok (error))
				goto leave;
			MONO_HANDLE_SET (info, assembly, assm_obj);

			/* Obtain info recursively */
			get_manifest_resource_info_internal (assm_obj, name, info, error);
			if (!is_ok (error))
				goto leave;
			guint32 location = MONO_HANDLE_GETVAL (info, location);
			location |= RESOURCE_LOCATION_ANOTHER_ASSEMBLY;
			MONO_HANDLE_SETVAL (info, location, guint32, location);
			break;

		case MONO_IMPLEMENTATION_EXP_TYPE:
			g_assert_not_reached ();
			break;
		}
	}

	result = TRUE;
leave:
	HANDLE_FUNCTION_RETURN_VAL (result);
}

ICALL_EXPORT gboolean
ves_icall_System_Reflection_Assembly_GetManifestResourceInfoInternal (MonoReflectionAssemblyHandle assembly_h, MonoStringHandle name, MonoManifestResourceInfoHandle info_h, MonoError *error)
{
	error_init (error);
	return get_manifest_resource_info_internal (assembly_h, name, info_h, error);
}

static gboolean
add_filename_to_files_array (MonoDomain *domain, MonoAssembly * assembly, MonoTableInfo *table, int i, MonoArrayHandle dest, int dest_idx, MonoError *error)
{
	HANDLE_FUNCTION_ENTER();
	error_init (error);
	const char *val = mono_metadata_string_heap (assembly->image, mono_metadata_decode_row_col (table, i, MONO_FILE_NAME));
	char *n = g_concat_dir_and_file (assembly->basedir, val);
	MonoStringHandle str = mono_string_new_handle (domain, n, error);
	g_free (n);
	if (!is_ok (error))
		goto leave;
	MONO_HANDLE_ARRAY_SETREF (dest, dest_idx, str);
leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

ICALL_EXPORT MonoObjectHandle
ves_icall_System_Reflection_Assembly_GetFilesInternal (MonoReflectionAssemblyHandle assembly_h, MonoStringHandle name, MonoBoolean resource_modules, MonoError *error) 
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (assembly_h);
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_h, assembly);
	MonoTableInfo *table = &assembly->image->tables [MONO_TABLE_FILE];
	int i, count;

	/* check hash if needed */
	if (!MONO_HANDLE_IS_NULL(name)) {
		char *n = mono_string_handle_to_utf8 (name, error);
		if (!is_ok (error))
			goto fail;

		for (i = 0; i < table->rows; ++i) {
			const char *val = mono_metadata_string_heap (assembly->image, mono_metadata_decode_row_col (table, i, MONO_FILE_NAME));
			if (strcmp (val, n) == 0) {
				g_free (n);
				n = g_concat_dir_and_file (assembly->basedir, val);
				MonoStringHandle fn = mono_string_new_handle (domain, n, error);
				g_free (n);
				if (!is_ok (error))
					goto fail;
				return MONO_HANDLE_CAST (MonoObject, fn);
			}
		}
		g_free (n);
		return NULL_HANDLE;
	}

	count = 0;
	for (i = 0; i < table->rows; ++i) {
		if (resource_modules || !(mono_metadata_decode_row_col (table, i, MONO_FILE_FLAGS) & FILE_CONTAINS_NO_METADATA))
			count ++;
	}

	MonoArrayHandle result = mono_array_new_handle (domain, mono_defaults.string_class, count, error);
	if (!is_ok (error))
		goto fail;

	count = 0;
	for (i = 0; i < table->rows; ++i) {
		if (resource_modules || !(mono_metadata_decode_row_col (table, i, MONO_FILE_FLAGS) & FILE_CONTAINS_NO_METADATA)) {
			if (!add_filename_to_files_array (domain, assembly, table, i, result, count, error))
				goto fail;
			count++;
		}
	}
	return MONO_HANDLE_CAST (MonoObject, result);
fail:
	return NULL_HANDLE;
}

static gboolean
add_module_to_modules_array (MonoDomain *domain, MonoArrayHandle dest, int *dest_idx, MonoImage* module, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	if (module) {
		MonoReflectionModuleHandle rm = mono_module_get_object_handle (domain, module, error);
		if (!is_ok (error))
			goto leave;
		
		MONO_HANDLE_ARRAY_SETREF (dest, *dest_idx, rm);
		++(*dest_idx);
	}

leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

static gboolean
add_file_to_modules_array (MonoDomain *domain, MonoArrayHandle dest, int dest_idx, MonoImage *image, MonoTableInfo *table, int table_idx,  MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);

	guint32 cols [MONO_FILE_SIZE];
	mono_metadata_decode_row (table, table_idx, cols, MONO_FILE_SIZE);
	if (cols [MONO_FILE_FLAGS] & FILE_CONTAINS_NO_METADATA) {
		MonoReflectionModuleHandle rm = mono_module_file_get_object_handle (domain, image, table_idx, error);
		if (!is_ok (error))
			goto leave;
		MONO_HANDLE_ARRAY_SETREF (dest, dest_idx, rm);
	} else {
		MonoImage *m = mono_image_load_file_for_image_checked (image, table_idx + 1, error);
		if (!is_ok (error))
			goto leave;
		if (!m) {
			const char *filename = mono_metadata_string_heap (image, cols [MONO_FILE_NAME]);
			mono_error_set_assembly_load (error, g_strdup (filename), "%s", "");
			goto leave;
		}
		MonoReflectionModuleHandle rm = mono_module_get_object_handle (domain, m, error);
		if (!is_ok (error))
			goto leave;
		MONO_HANDLE_ARRAY_SETREF (dest, dest_idx, rm);
	}

leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

ICALL_EXPORT MonoArrayHandle
ves_icall_System_Reflection_Assembly_GetModulesInternal (MonoReflectionAssemblyHandle assembly_h, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = mono_domain_get();
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_h, assembly);
	MonoClass *klass;
	int i, j, file_count = 0;
	MonoImage **modules;
	guint32 module_count, real_module_count;
	MonoTableInfo *table;
	MonoImage *image = assembly->image;

	g_assert (image != NULL);
	g_assert (!assembly_is_dynamic (assembly));

	table = &image->tables [MONO_TABLE_FILE];
	file_count = table->rows;

	modules = image->modules;
	module_count = image->module_count;

	real_module_count = 0;
	for (i = 0; i < module_count; ++i)
		if (modules [i])
			real_module_count ++;

	klass = mono_class_get_module_class ();
	MonoArrayHandle res = mono_array_new_handle (domain, klass, 1 + real_module_count + file_count, error);
	if (!is_ok (error))
		goto fail;

	MonoReflectionModuleHandle image_obj = mono_module_get_object_handle (domain, image, error);
	if (!is_ok (error))
		goto fail;

	MONO_HANDLE_ARRAY_SETREF (res, 0, image_obj);

	j = 1;
	for (i = 0; i < module_count; ++i)
		if (!add_module_to_modules_array (domain, res, &j, modules[i], error))
			goto fail;

	for (i = 0; i < file_count; ++i, ++j) {
		if (!add_file_to_modules_array (domain, res, j, image, table, i, error))
			goto fail;
	}

	return res;
fail:
	return MONO_HANDLE_CAST (MonoArray, NULL_HANDLE);
}

ICALL_EXPORT MonoReflectionMethodHandle
ves_icall_GetCurrentMethod (MonoError *error) 
{
	error_init (error);

	MonoMethod *m = mono_method_get_last_managed ();

	if (!m) {
		mono_error_set_not_supported (error, "Stack walks are not supported on this platform.");
		return MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE);
	}

	while (m->is_inflated)
		m = ((MonoMethodInflated*)m)->declaring;

	return mono_method_get_object_handle (mono_domain_get (), m, NULL, error);
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
		if (mono_class_is_ginst (klass))
			ctx.class_inst = mono_class_get_generic_class (klass)->context.class_inst;
		else if (mono_class_is_gtd (klass))
			ctx.class_inst = mono_class_get_generic_container (klass)->context.class_inst;
		result = mono_class_inflate_generic_method_full_checked (inflated->declaring, klass, &ctx, &error);
		g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */
		return result;
	}

	mono_class_setup_methods (method->klass);
	if (mono_class_has_failure (method->klass))
		return NULL;
	int mcount = mono_class_get_method_count (method->klass);
	for (i = 0; i < mcount; ++i) {
		if (method->klass->methods [i] == method) {
			offset = i;
			break;
		}	
	}
	mono_class_setup_methods (klass);
	if (mono_class_has_failure (klass))
		return NULL;
	g_assert (offset >= 0 && offset < mono_class_get_method_count (klass));
	return klass->methods [offset];
}

ICALL_EXPORT MonoReflectionMethodHandle
ves_icall_System_Reflection_MethodBase_GetMethodFromHandleInternalType_native (MonoMethod *method, MonoType *type, MonoBoolean generic_check, MonoError *error)
{
	error_init (error);
	MonoClass *klass;
	if (type && generic_check) {
		klass = mono_class_from_mono_type (type);
		if (mono_class_get_generic_type_definition (method->klass) != mono_class_get_generic_type_definition (klass))
			return MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE);

		if (method->klass != klass) {
			method = mono_method_get_equivalent_method (method, klass);
			if (!method)
				return MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE);
		}
	} else if (type)
		klass = mono_class_from_mono_type (type);
	else
		klass = method->klass;
	return mono_method_get_object_handle (mono_domain_get (), method, klass, error);
}

ICALL_EXPORT MonoReflectionMethodBodyHandle
ves_icall_System_Reflection_MethodBase_GetMethodBodyInternal (MonoMethod *method, MonoError *error)
{
	error_init (error);
	return mono_method_body_get_object_handle (mono_domain_get (), method, error);
}

ICALL_EXPORT MonoReflectionAssemblyHandle
ves_icall_System_Reflection_Assembly_GetExecutingAssembly (MonoError *error)
{
	error_init (error);

	MonoMethod *dest = NULL;
	mono_stack_walk_no_il (get_executing, &dest);
	g_assert (dest);
	return mono_assembly_get_object_handle (mono_domain_get (), dest->klass->image->assembly, error);
}


ICALL_EXPORT MonoReflectionAssemblyHandle
ves_icall_System_Reflection_Assembly_GetEntryAssembly (MonoError *error)
{
	error_init (error);

	MonoDomain* domain = mono_domain_get ();

	if (!domain->entry_assembly)
		return MONO_HANDLE_CAST (MonoReflectionAssembly, NULL_HANDLE);

	return mono_assembly_get_object_handle (domain, domain->entry_assembly, error);
}

ICALL_EXPORT MonoReflectionAssemblyHandle
ves_icall_System_Reflection_Assembly_GetCallingAssembly (MonoError *error)
{
	error_init (error);
	MonoMethod *m;
	MonoMethod *dest;

	dest = NULL;
	mono_stack_walk_no_il (get_executing, &dest);
	m = dest;
	mono_stack_walk_no_il (get_caller_no_reflection, &dest);
	if (!dest)
		dest = m;
	if (!m) {
		mono_error_set_not_supported (error, "Stack walks are not supported on this platform.");
		return MONO_HANDLE_CAST (MonoReflectionAssembly, NULL_HANDLE);
	}
	return mono_assembly_get_object_handle (mono_domain_get (), dest->klass->image->assembly, error);
}

ICALL_EXPORT MonoStringHandle
ves_icall_System_RuntimeType_getFullName (MonoReflectionTypeHandle object, gboolean full_name,
										  gboolean assembly_qualified, MonoError *error)
{
	MonoDomain *domain = mono_object_domain (MONO_HANDLE_RAW (object));
	MonoType *type = MONO_HANDLE_RAW (object)->type;
	MonoTypeNameFormat format;
	MonoStringHandle res;
	gchar *name;

	if (full_name)
		format = assembly_qualified ?
			MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED :
			MONO_TYPE_NAME_FORMAT_FULL_NAME;
	else
		format = MONO_TYPE_NAME_FORMAT_REFLECTION;
 
	name = mono_type_get_name_full (type, format);
	if (!name)
		return NULL_HANDLE_STRING;

	if (full_name && (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR)) {
		g_free (name);
		return NULL_HANDLE_STRING;
	}

	res = mono_string_new_handle (domain, name, error);
	g_free (name);

	return res;
}

ICALL_EXPORT int
vell_icall_RuntimeType_get_core_clr_security_level (MonoReflectionTypeHandle rfield, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (rfield, type);
	MonoClass *klass = mono_class_from_mono_type (type);

	mono_class_init_checked (klass, error);
	if (!is_ok (error))
		return -1;
	return mono_security_core_clr_class_level (klass);
}

ICALL_EXPORT int
ves_icall_MonoField_get_core_clr_security_level (MonoReflectionField *rfield)
{
	MonoClassField *field = rfield->field;
	return mono_security_core_clr_field_level (field, TRUE);
}

ICALL_EXPORT int
ves_icall_MonoMethod_get_core_clr_security_level (MonoReflectionMethodHandle rfield, MonoError *error)
{
	error_init (error);
	MonoMethod *method = MONO_HANDLE_GETVAL (rfield, method);
	return mono_security_core_clr_method_level (method, TRUE);
}

ICALL_EXPORT MonoStringHandle
ves_icall_System_Reflection_Assembly_get_fullName (MonoReflectionAssemblyHandle assembly, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (assembly);
	MonoAssembly *mass = MONO_HANDLE_GETVAL (assembly, assembly);
	gchar *name;

	name = mono_stringify_assembly_name (&mass->aname);
	MonoStringHandle res = mono_string_new_handle (domain, name, error);
	g_free (name);
	return res;
}

ICALL_EXPORT MonoAssemblyName *
ves_icall_System_Reflection_AssemblyName_GetNativeName (MonoAssembly *mass)
{
	return &mass->aname;
}

ICALL_EXPORT void
ves_icall_System_Reflection_Assembly_InternalGetAssemblyName (MonoStringHandle fname, MonoAssemblyName *name, MonoStringHandleOut normalized_codebase, MonoError *error)
{
	char *filename;
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	char *codebase = NULL;
	gboolean res;
	MonoImage *image;
	char *dirname;

	error_init (error);

	filename = mono_string_handle_to_utf8 (fname, error);
	return_if_nok (error);

	dirname = g_path_get_dirname (filename);
	replace_shadow_path (mono_domain_get (), dirname, &filename);
	g_free (dirname);

	image = mono_image_open_full (filename, &status, TRUE);

	if (!image){
		if (status == MONO_IMAGE_IMAGE_INVALID)
			mono_error_set_bad_image_name (error, g_strdup (filename), "%s", "");
		else
			mono_error_set_assembly_load (error, g_strdup (filename), "%s", "");
		g_free (filename);
		return;
	}

	res = mono_assembly_fill_assembly_name_full (image, name, TRUE);
	if (!res) {
		mono_image_close (image);
		g_free (filename);
		mono_error_set_argument (error, "assemblyFile", "The file does not contain a manifest");
		return;
	}

	if (filename != NULL && *filename != '\0') {
		gchar *result;

		codebase = g_strdup (filename);

		mono_icall_make_platform_path (codebase);

		const gchar *prepend = mono_icall_get_file_path_prefix (codebase);

		result = g_strconcat (prepend, codebase, NULL);
		g_free (codebase);
		codebase = result;
	}
	MONO_HANDLE_ASSIGN (normalized_codebase, mono_string_new_handle (mono_domain_get (), codebase, error));
	g_free (codebase);

	mono_image_close (image);
	g_free (filename);
}

ICALL_EXPORT MonoBoolean
ves_icall_System_Reflection_Assembly_LoadPermissions (MonoReflectionAssemblyHandle assembly_h,
						      char **minimum, guint32 *minLength, char **optional, guint32 *optLength, char **refused, guint32 *refLength, MonoError *error)
{
	error_init (error);
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_h, assembly);
	MonoBoolean result = FALSE;
	MonoDeclSecurityEntry entry;

	/* SecurityAction.RequestMinimum */
	if (mono_declsec_get_assembly_action (assembly, SECURITY_ACTION_REQMIN, &entry)) {
		*minimum = entry.blob;
		*minLength = entry.size;
		result = TRUE;
	}
	/* SecurityAction.RequestOptional */
	if (mono_declsec_get_assembly_action (assembly, SECURITY_ACTION_REQOPT, &entry)) {
		*optional = entry.blob;
		*optLength = entry.size;
		result = TRUE;
	}
	/* SecurityAction.RequestRefuse */
	if (mono_declsec_get_assembly_action (assembly, SECURITY_ACTION_REQREFUSE, &entry)) {
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

static void
image_get_type (MonoDomain *domain, MonoImage *image, MonoTableInfo *tdef, int table_idx, int count, MonoArrayHandle res, MonoArrayHandle exceptions, MonoBoolean exportedOnly, MonoError *error)
{
	error_init (error);
	HANDLE_FUNCTION_ENTER ();
	MonoError klass_error;
	MonoClass *klass = mono_class_get_checked (image, table_idx | MONO_TOKEN_TYPE_DEF, &klass_error);

	if (klass) {
		MonoReflectionTypeHandle rt = mono_type_get_object_handle (domain, &klass->byval_arg, error);
		return_if_nok (error);

		MONO_HANDLE_ARRAY_SETREF (res, count, rt);
	} else {
		MonoException *ex = mono_error_convert_to_exception (error);
		MONO_HANDLE_ARRAY_SETRAW (exceptions, count, ex);
	}
	HANDLE_FUNCTION_RETURN ();
}

static MonoArrayHandle
mono_module_get_types (MonoDomain *domain, MonoImage *image, MonoArrayHandleOut exceptions, MonoBoolean exportedOnly, MonoError *error)
{
	MonoTableInfo *tdef = &image->tables [MONO_TABLE_TYPEDEF];
	int i, count;

	error_init (error);

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
	MonoArrayHandle res = mono_array_new_handle (domain, mono_defaults.runtimetype_class, count, error);
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoArray, NULL_HANDLE));
	MONO_HANDLE_ASSIGN (exceptions,  mono_array_new_handle (domain, mono_defaults.exception_class, count, error));
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoArray, NULL_HANDLE));
	count = 0;
	for (i = 1; i < tdef->rows; ++i) {
		if (!exportedOnly || mono_module_type_is_visible (tdef, image, i+1)) {
			image_get_type (domain, image, tdef, i + 1, count, res, exceptions, exportedOnly, error);
			return_val_if_nok (error, MONO_HANDLE_CAST (MonoArray, NULL_HANDLE));
			count++;
		}
	}
	
	return res;
}

static void
append_module_types (MonoDomain *domain, MonoArrayHandleOut res, MonoArrayHandleOut exceptions, MonoImage *image, MonoBoolean exportedOnly, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoArrayHandle ex2 = MONO_HANDLE_NEW (MonoArray, NULL);
	MonoArrayHandle res2 = mono_module_get_types (domain, image, ex2, exportedOnly, error);
	if (!is_ok (error))
		goto leave;

	/* Append the new types to the end of the array */
	if (mono_array_handle_length (res2) > 0) {
		guint32 len1, len2;

		len1 = mono_array_handle_length (res);
		len2 = mono_array_handle_length (res2);

		MonoArrayHandle res3 = mono_array_new_handle (domain, mono_defaults.runtimetype_class, len1 + len2, error);
		if (!is_ok (error))
			goto leave;

		mono_array_handle_memcpy_refs (res3, 0, res, 0, len1);
		mono_array_handle_memcpy_refs (res3, len1, res2, 0, len2);
		MONO_HANDLE_ASSIGN (res, res3);

		MonoArrayHandle ex3 = mono_array_new_handle (domain, mono_defaults.runtimetype_class, len1 + len2, error);
		if (!is_ok (error))
			goto leave;

		mono_array_handle_memcpy_refs (ex3, 0, exceptions, 0, len1);
		mono_array_handle_memcpy_refs (ex3, len1, ex2, 0, len2);
		MONO_HANDLE_ASSIGN (exceptions, ex3);
	}
leave:
	HANDLE_FUNCTION_RETURN ();
}

static void
set_class_failure_in_array (MonoArrayHandle exl, int i, MonoClass *klass)
{
	HANDLE_FUNCTION_ENTER ();
	MonoError unboxed_error;
	error_init (&unboxed_error);
	mono_error_set_for_class_failure (&unboxed_error, klass);

	MonoExceptionHandle exc = MONO_HANDLE_NEW (MonoException, mono_error_convert_to_exception (&unboxed_error));
	MONO_HANDLE_ARRAY_SETREF (exl, i, exc);
	HANDLE_FUNCTION_RETURN ();
}

ICALL_EXPORT MonoArrayHandle
ves_icall_System_Reflection_Assembly_GetTypes (MonoReflectionAssemblyHandle assembly_handle, MonoBoolean exportedOnly, MonoError *error)
{
	MonoArrayHandle exceptions = MONO_HANDLE_NEW(MonoArray, NULL);
	int i;

	MonoDomain *domain = MONO_HANDLE_DOMAIN (assembly_handle);
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_handle, assembly);

	g_assert (!assembly_is_dynamic (assembly));
	MonoImage *image = assembly->image;
	MonoTableInfo *table = &image->tables [MONO_TABLE_FILE];
	MonoArrayHandle res = mono_module_get_types (domain, image, exceptions, exportedOnly, error);
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoArray, NULL_HANDLE));

	/* Append data from all modules in the assembly */
	for (i = 0; i < table->rows; ++i) {
		if (!(mono_metadata_decode_row_col (table, i, MONO_FILE_FLAGS) & FILE_CONTAINS_NO_METADATA)) {
			MonoImage *loaded_image = mono_assembly_load_module_checked (image->assembly, i + 1, error);
			return_val_if_nok (error, MONO_HANDLE_CAST (MonoArray, NULL_HANDLE));

			if (loaded_image) {
				append_module_types (domain, res, exceptions, loaded_image, exportedOnly, error);
				return_val_if_nok (error, MONO_HANDLE_CAST (MonoArray, NULL_HANDLE));
			}
		}
	}

	/* the ReflectionTypeLoadException must have all the types (Types property), 
	 * NULL replacing types which throws an exception. The LoaderException must
	 * contain all exceptions for NULL items.
	 */

	int len = mono_array_handle_length (res);

	int ex_count = 0;
	GList *list = NULL;
	MonoReflectionTypeHandle t = MONO_HANDLE_NEW (MonoReflectionType, NULL);
	for (i = 0; i < len; i++) {
		MONO_HANDLE_ARRAY_GETREF (t, res, i);

		if (!MONO_HANDLE_IS_NULL (t)) {
			MonoClass *klass = mono_type_get_class (MONO_HANDLE_GETVAL (t, type));
			if ((klass != NULL) && mono_class_has_failure (klass)) {
				/* keep the class in the list */
				list = g_list_append (list, klass);
				/* and replace Type with NULL */
				MONO_HANDLE_ARRAY_SETRAW (res, i, NULL);
			}
		} else {
			ex_count ++;
		}
	}

	if (list || ex_count) {
		GList *tmp = NULL;
		int j, length = g_list_length (list) + ex_count;

		MonoArrayHandle exl = mono_array_new_handle (domain, mono_defaults.exception_class, length, error);
		if (!is_ok (error)) {
			g_list_free (list);
			return MONO_HANDLE_CAST (MonoArray, NULL_HANDLE);
		}
		/* Types for which mono_class_get_checked () succeeded */
		MonoExceptionHandle exc = MONO_HANDLE_NEW (MonoException, NULL);
		for (i = 0, tmp = list; tmp; i++, tmp = tmp->next) {
			set_class_failure_in_array (exl, i, (MonoClass*)tmp->data);
		}
		/* Types for which it don't */
		for (j = 0; j < mono_array_handle_length (exceptions); ++j) {
			MONO_HANDLE_ARRAY_GETREF (exc, exceptions, j);
			if (!MONO_HANDLE_IS_NULL (exc)) {
				g_assert (i < length);
				MONO_HANDLE_ARRAY_SETREF (exl, i, exc);
				i ++;
			}
		}
		g_list_free (list);
		list = NULL;

		MONO_HANDLE_ASSIGN (exc, mono_get_exception_reflection_type_load_checked (res, exl, error));
		if (!is_ok (error)) {
			return MONO_HANDLE_CAST (MonoArray, NULL_HANDLE);
		}
		mono_error_set_exception_handle (error, exc);
		return MONO_HANDLE_CAST (MonoArray, NULL_HANDLE);
	}
		
	return res;
}

ICALL_EXPORT void
ves_icall_Mono_RuntimeMarshal_FreeAssemblyName (MonoAssemblyName *aname, gboolean free_struct)
{
	mono_assembly_name_free (aname);
	if (free_struct)
		g_free (aname);
}

ICALL_EXPORT gboolean
ves_icall_System_Reflection_AssemblyName_ParseAssemblyName (const char *name, MonoAssemblyName *aname, gboolean *is_version_definited, gboolean *is_token_defined)
{
	*is_version_definited = *is_token_defined = FALSE;

	return mono_assembly_name_parse_full (name, aname, TRUE, is_version_definited, is_token_defined);
}

ICALL_EXPORT MonoReflectionTypeHandle
ves_icall_System_Reflection_Module_GetGlobalType (MonoReflectionModuleHandle module, MonoError *error)
{
	MonoDomain *domain = MONO_HANDLE_DOMAIN (module);
	MonoImage *image = MONO_HANDLE_GETVAL (module, image);
	MonoClass *klass;

	g_assert (image);

	MonoReflectionTypeHandle ret = MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);

	if (image_is_dynamic (image) && ((MonoDynamicImage*)image)->initial_image)
		/* These images do not have a global type */
		goto leave;

	klass = mono_class_get_checked (image, 1 | MONO_TOKEN_TYPE_DEF, error);
	if (!is_ok (error))
		goto leave;

	ret = mono_type_get_object_handle (domain, &klass->byval_arg, error);
leave:
	return ret;
}

ICALL_EXPORT void
ves_icall_System_Reflection_Module_Close (MonoReflectionModuleHandle module, MonoError *error)
{
	/*if (module->image)
		mono_image_close (module->image);*/
}

ICALL_EXPORT MonoStringHandle
ves_icall_System_Reflection_Module_GetGuidInternal (MonoReflectionModuleHandle refmodule, MonoError *error)
{
	MonoDomain *domain = MONO_HANDLE_DOMAIN (refmodule);
	MonoImage *image = MONO_HANDLE_GETVAL (refmodule, image);

	g_assert (image);
	return mono_string_new_handle (domain, image->guid, error);
}

#ifndef HOST_WIN32
static inline gpointer
mono_icall_module_get_hinstance (MonoReflectionModuleHandle module)
{
	return (gpointer) (-1);
}
#endif /* HOST_WIN32 */

ICALL_EXPORT gpointer
ves_icall_System_Reflection_Module_GetHINSTANCE (MonoReflectionModuleHandle module, MonoError *error)
{
	return mono_icall_module_get_hinstance (module);
}

ICALL_EXPORT void
ves_icall_System_Reflection_Module_GetPEKind (MonoImage *image, gint32 *pe_kind, gint32 *machine, MonoError *error)
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
ves_icall_System_Reflection_Module_GetMDStreamVersion (MonoImage *image, MonoError *error)
{
	return (image->md_version_major << 16) | (image->md_version_minor);
}

ICALL_EXPORT MonoArrayHandle
ves_icall_System_Reflection_Module_InternalGetTypes (MonoReflectionModuleHandle module, MonoError *error)
{
	error_init (error);

	MonoImage *image = MONO_HANDLE_GETVAL (module, image);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (module);

	if (!image) {
		MonoArrayHandle arr = mono_array_new_handle (domain, mono_defaults.runtimetype_class, 0, error);
		return arr;
	} else {
		MonoArrayHandle exceptions = MONO_HANDLE_NEW (MonoArray, NULL);
		MonoArrayHandle res = mono_module_get_types (domain, image, exceptions, FALSE, error);
		return_val_if_nok (error, MONO_HANDLE_CAST(MonoArray, NULL_HANDLE));

		int n = mono_array_handle_length (exceptions);
		MonoExceptionHandle ex = MONO_HANDLE_NEW (MonoException, NULL);
		for (int i = 0; i < n; ++i) {
			MONO_HANDLE_ARRAY_GETREF(ex, exceptions, i);
			if (!MONO_HANDLE_IS_NULL (ex)) {
				mono_error_set_exception_handle (error, ex);
				return MONO_HANDLE_CAST(MonoArray, NULL_HANDLE);
			}
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
		MonoError error;
		MonoClass *handle_class;

		if (!mono_lookup_dynamic_token_class (image, token, FALSE, &handle_class, NULL, &error)) {
			mono_error_cleanup (&error); /* just probing, ignore error */
			return FALSE;
		}

		return mono_defaults.methodhandle_class == handle_class;
	}
}

static MonoGenericInst *
get_generic_inst_from_array_handle (MonoArrayHandle type_args)
{
	int type_argc = mono_array_handle_length (type_args);
	int size = MONO_SIZEOF_GENERIC_INST + type_argc * sizeof (MonoType *);

	MonoGenericInst *ginst = (MonoGenericInst *)g_alloca (size);
	memset (ginst, 0, sizeof (MonoGenericInst));
	ginst->type_argc = type_argc;
	for (int i = 0; i < type_argc; i++) {
		MONO_HANDLE_ARRAY_GETVAL (ginst->type_argv[i], type_args, MonoType*, i);
	}
	ginst->is_open = FALSE;
	for (int i = 0; i < type_argc; i++) {
		if (mono_class_is_open_constructed_type (ginst->type_argv[i])) {
			ginst->is_open = TRUE;
			break;
		}
	}

	return mono_metadata_get_canonical_generic_inst (ginst);
}

static void
init_generic_context_from_args_handles (MonoGenericContext *context, MonoArrayHandle type_args, MonoArrayHandle method_args)
{
	if (!MONO_HANDLE_IS_NULL (type_args)) {
		context->class_inst = get_generic_inst_from_array_handle (type_args);
	} else {
		context->class_inst = NULL;
	}
	if (!MONO_HANDLE_IS_NULL  (method_args)) {
		context->method_inst = get_generic_inst_from_array_handle (method_args);
	} else {
		context->method_inst = NULL;
	}
}


static MonoType*
module_resolve_type_token (MonoImage *image, guint32 token, MonoArrayHandle type_args, MonoArrayHandle method_args, MonoResolveTokenError *resolve_error, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoType *result = NULL;
	MonoClass *klass;
	int table = mono_metadata_token_table (token);
	int index = mono_metadata_token_index (token);
	MonoGenericContext context;

	*resolve_error = ResolveTokenError_Other;

	/* Validate token */
	if ((table != MONO_TABLE_TYPEDEF) && (table != MONO_TABLE_TYPEREF) && 
		(table != MONO_TABLE_TYPESPEC)) {
		*resolve_error = ResolveTokenError_BadTable;
		goto leave;
	}

	if (image_is_dynamic (image)) {
		if ((table == MONO_TABLE_TYPEDEF) || (table == MONO_TABLE_TYPEREF)) {
			MonoError inner_error;
			klass = (MonoClass *)mono_lookup_dynamic_token_class (image, token, FALSE, NULL, NULL, &inner_error);
			mono_error_cleanup (&inner_error);
			result = klass ? &klass->byval_arg : NULL;
			goto leave;
		}

		init_generic_context_from_args_handles (&context, type_args, method_args);
		MonoError inner_error;
		klass = (MonoClass *)mono_lookup_dynamic_token_class (image, token, FALSE, NULL, &context, &inner_error);
		mono_error_cleanup (&inner_error);
		result = klass ? &klass->byval_arg : NULL;
		goto leave;
	}

	if ((index <= 0) || (index > image->tables [table].rows)) {
		*resolve_error = ResolveTokenError_OutOfRange;
		goto leave;
	}

	init_generic_context_from_args_handles (&context, type_args, method_args);
	klass = mono_class_get_checked (image, token, error);
	if (klass)
		klass = mono_class_inflate_generic_class_checked (klass, &context, error);
	if (!is_ok (error))
		goto leave;

	if (klass)
		result = &klass->byval_arg;
leave:
	HANDLE_FUNCTION_RETURN_VAL (result);

}
ICALL_EXPORT MonoType*
ves_icall_System_Reflection_Module_ResolveTypeToken (MonoImage *image, guint32 token, MonoArrayHandle type_args, MonoArrayHandle method_args, MonoResolveTokenError *resolve_error, MonoError *error)
{
	return module_resolve_type_token (image, token, type_args, method_args, resolve_error, error);
}

static MonoMethod*
module_resolve_method_token (MonoImage *image, guint32 token, MonoArrayHandle type_args, MonoArrayHandle method_args, MonoResolveTokenError *resolve_error, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoMethod *method = NULL;
	int table = mono_metadata_token_table (token);
	int index = mono_metadata_token_index (token);
	MonoGenericContext context;

	*resolve_error = ResolveTokenError_Other;

	/* Validate token */
	if ((table != MONO_TABLE_METHOD) && (table != MONO_TABLE_METHODSPEC) && 
		(table != MONO_TABLE_MEMBERREF)) {
		*resolve_error = ResolveTokenError_BadTable;
		goto leave;
	}

	if (image_is_dynamic (image)) {
		if (table == MONO_TABLE_METHOD) {
			MonoError inner_error;
			method = (MonoMethod *)mono_lookup_dynamic_token_class (image, token, FALSE, NULL, NULL, &inner_error);
			mono_error_cleanup (&inner_error);
			goto leave;
		}

		if ((table == MONO_TABLE_MEMBERREF) && !(mono_memberref_is_method (image, token))) {
			*resolve_error = ResolveTokenError_BadTable;
			goto leave;
		}

		init_generic_context_from_args_handles (&context, type_args, method_args);
		MonoError inner_error;
		method = (MonoMethod *)mono_lookup_dynamic_token_class (image, token, FALSE, NULL, &context, &inner_error);
		mono_error_cleanup (&inner_error);
		goto leave;
	}

	if ((index <= 0) || (index > image->tables [table].rows)) {
		*resolve_error = ResolveTokenError_OutOfRange;
		goto leave;
	}
	if ((table == MONO_TABLE_MEMBERREF) && (!mono_memberref_is_method (image, token))) {
		*resolve_error = ResolveTokenError_BadTable;
		goto leave;
	}

	init_generic_context_from_args_handles (&context, type_args, method_args);
	method = mono_get_method_checked (image, token, NULL, &context, error);

leave:
	HANDLE_FUNCTION_RETURN_VAL (method);
}

ICALL_EXPORT MonoMethod*
ves_icall_System_Reflection_Module_ResolveMethodToken (MonoImage *image, guint32 token, MonoArrayHandle type_args, MonoArrayHandle method_args, MonoResolveTokenError *resolve_error, MonoError *error)
{
	return module_resolve_method_token (image, token, type_args, method_args, resolve_error, error);
}

ICALL_EXPORT MonoString*
ves_icall_System_Reflection_Module_ResolveStringToken (MonoImage *image, guint32 token, MonoResolveTokenError *resolve_error)
{
	MonoError error;
	int index = mono_metadata_token_index (token);

	*resolve_error = ResolveTokenError_Other;

	/* Validate token */
	if (mono_metadata_token_code (token) != MONO_TOKEN_STRING) {
		*resolve_error = ResolveTokenError_BadTable;
		return NULL;
	}

	if (image_is_dynamic (image)) {
		MonoString * result = (MonoString *)mono_lookup_dynamic_token_class (image, token, FALSE, NULL, NULL, &error);
		mono_error_cleanup (&error);
		return result;
	}

	if ((index <= 0) || (index >= image->heap_us.size)) {
		*resolve_error = ResolveTokenError_OutOfRange;
		return NULL;
	}

	/* FIXME: What to do if the index points into the middle of a string ? */

	MonoString *result = mono_ldstr_checked (mono_domain_get (), image, index, &error);
	mono_error_set_pending_exception (&error);
	return result;
}

static MonoClassField*
module_resolve_field_token (MonoImage *image, guint32 token, MonoArrayHandle type_args, MonoArrayHandle method_args, MonoResolveTokenError *resolve_error, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	MonoClass *klass;
	int table = mono_metadata_token_table (token);
	int index = mono_metadata_token_index (token);
	MonoGenericContext context;
	MonoClassField *field = NULL;

	error_init (error);
	*resolve_error = ResolveTokenError_Other;

	/* Validate token */
	if ((table != MONO_TABLE_FIELD) && (table != MONO_TABLE_MEMBERREF)) {
		*resolve_error = ResolveTokenError_BadTable;
		goto leave;
	}

	if (image_is_dynamic (image)) {
		if (table == MONO_TABLE_FIELD) {
			MonoError inner_error;
			field = (MonoClassField *)mono_lookup_dynamic_token_class (image, token, FALSE, NULL, NULL, &inner_error);
			mono_error_cleanup (&inner_error);
			goto leave;
		}

		if (mono_memberref_is_method (image, token)) {
			*resolve_error = ResolveTokenError_BadTable;
			goto leave;
		}

		init_generic_context_from_args_handles (&context, type_args, method_args);
		MonoError inner_error;
		field = (MonoClassField *)mono_lookup_dynamic_token_class (image, token, FALSE, NULL, &context, &inner_error);
		mono_error_cleanup (&inner_error);
		goto leave;
	}

	if ((index <= 0) || (index > image->tables [table].rows)) {
		*resolve_error = ResolveTokenError_OutOfRange;
		goto leave;
	}
	if ((table == MONO_TABLE_MEMBERREF) && (mono_memberref_is_method (image, token))) {
		*resolve_error = ResolveTokenError_BadTable;
		goto leave;
	}

	init_generic_context_from_args_handles (&context, type_args, method_args);
	field = mono_field_from_token_checked (image, token, &klass, &context, error);
	
leave:
	HANDLE_FUNCTION_RETURN_VAL (field);
}

ICALL_EXPORT MonoClassField*
ves_icall_System_Reflection_Module_ResolveFieldToken (MonoImage *image, guint32 token, MonoArrayHandle type_args, MonoArrayHandle method_args, MonoResolveTokenError *resolve_error, MonoError *error)
{
	return module_resolve_field_token (image, token, type_args, method_args, resolve_error, error);
}

ICALL_EXPORT MonoObjectHandle
ves_icall_System_Reflection_Module_ResolveMemberToken (MonoImage *image, guint32 token, MonoArrayHandle type_args, MonoArrayHandle method_args, MonoResolveTokenError *error, MonoError *merror)
{
	int table = mono_metadata_token_table (token);

	error_init (merror);
	*error = ResolveTokenError_Other;

	switch (table) {
	case MONO_TABLE_TYPEDEF:
	case MONO_TABLE_TYPEREF:
	case MONO_TABLE_TYPESPEC: {
		MonoType *t = module_resolve_type_token (image, token, type_args, method_args, error, merror);
		if (t) {
			return MONO_HANDLE_CAST (MonoObject, mono_type_get_object_handle (mono_domain_get (), t, merror));
		}
		else
			return NULL_HANDLE;
	}
	case MONO_TABLE_METHOD:
	case MONO_TABLE_METHODSPEC: {
		MonoMethod *m = module_resolve_method_token (image, token, type_args, method_args, error, merror);
		if (m) {
			return MONO_HANDLE_CAST (MonoObject, mono_method_get_object_handle (mono_domain_get (), m, m->klass, merror));
		} else
			return NULL_HANDLE;
	}		
	case MONO_TABLE_FIELD: {
		MonoClassField *f = module_resolve_field_token (image, token, type_args, method_args, error, merror);
		if (f) {
			return MONO_HANDLE_CAST (MonoObject, mono_field_get_object_handle (mono_domain_get (), f->parent, f, merror));
		}
		else
			return NULL_HANDLE;
	}
	case MONO_TABLE_MEMBERREF:
		if (mono_memberref_is_method (image, token)) {
			MonoMethod *m = module_resolve_method_token (image, token, type_args, method_args, error, merror);
			if (m) {
				return MONO_HANDLE_CAST (MonoObject, mono_method_get_object_handle (mono_domain_get (), m, m->klass, merror));
			} else
				return NULL_HANDLE;
		}
		else {
			MonoClassField *f = module_resolve_field_token (image, token, type_args, method_args, error, merror);
			if (f) {
				return MONO_HANDLE_CAST (MonoObject, mono_field_get_object_handle (mono_domain_get (), f->parent, f, merror));
			}
			else
				return NULL_HANDLE;
		}
		break;

	default:
		*error = ResolveTokenError_BadTable;
	}

	return NULL_HANDLE;
}

ICALL_EXPORT MonoArrayHandle
ves_icall_System_Reflection_Module_ResolveSignature (MonoImage *image, guint32 token, MonoResolveTokenError *resolve_error, MonoError *error)
{
	error_init (error);
	int table = mono_metadata_token_table (token);
	int idx = mono_metadata_token_index (token);
	MonoTableInfo *tables = image->tables;
	guint32 sig, len;
	const char *ptr;

	*resolve_error = ResolveTokenError_OutOfRange;

	/* FIXME: Support other tables ? */
	if (table != MONO_TABLE_STANDALONESIG)
		return MONO_HANDLE_CAST (MonoArray, NULL);

	if (image_is_dynamic (image))
		return MONO_HANDLE_CAST (MonoArray, NULL);

	if ((idx == 0) || (idx > tables [MONO_TABLE_STANDALONESIG].rows))
		return MONO_HANDLE_CAST (MonoArray, NULL);

	sig = mono_metadata_decode_row_col (&tables [MONO_TABLE_STANDALONESIG], idx - 1, 0);

	ptr = mono_metadata_blob_heap (image, sig);
	len = mono_metadata_decode_blob_size (ptr, &ptr);

	MonoArrayHandle res = mono_array_new_handle (mono_domain_get (), mono_defaults.byte_class, len, error);
	if (!is_ok (error))
		return MONO_HANDLE_CAST (MonoArray, NULL);
	uint32_t h;
	gpointer array_base = MONO_ARRAY_HANDLE_PIN (res, guint8, 0, &h);
	memcpy (array_base, ptr, len);
	mono_gchandle_free (h);
	return res;
}

ICALL_EXPORT MonoBoolean
ves_icall_RuntimeTypeHandle_IsArray (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	MonoBoolean res = !type->byref && (type->type == MONO_TYPE_ARRAY || type->type == MONO_TYPE_SZARRAY);

	return res;
}

static void
check_for_invalid_type (MonoClass *klass, MonoError *error)
{
	char *name;

	error_init (error);

	if (klass->byval_arg.type != MONO_TYPE_TYPEDBYREF)
		return;

	name = mono_type_get_full_name (klass);
	mono_error_set_type_load_name (error, name, g_strdup (""), "");
}

ICALL_EXPORT MonoReflectionTypeHandle
ves_icall_RuntimeType_make_array_type (MonoReflectionTypeHandle ref_type, int rank, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	MonoClass *klass = mono_class_from_mono_type (type);
	check_for_invalid_type (klass, error);
	if (!is_ok (error))
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);

	MonoClass *aklass;
	if (rank == 0) //single dimentional array
		aklass = mono_array_class_get (klass, 1);
	else
		aklass = mono_bounded_array_class_get (klass, rank, TRUE);

	if (mono_class_has_failure (aklass)) {
		mono_error_set_for_class_failure (error, aklass);
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
	}

	MonoDomain *domain = MONO_HANDLE_DOMAIN (ref_type);
	return mono_type_get_object_handle (domain, &aklass->byval_arg, error);
}

ICALL_EXPORT MonoReflectionTypeHandle
ves_icall_RuntimeType_make_byref_type (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	MonoClass *klass = mono_class_from_mono_type (type);
	mono_class_init_checked (klass, error);
	if (!is_ok (error))
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);

	check_for_invalid_type (klass, error);
	if (!is_ok (error))
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);

	MonoDomain *domain = MONO_HANDLE_DOMAIN (ref_type);
	return mono_type_get_object_handle (domain, &klass->this_arg, error);
}

ICALL_EXPORT MonoReflectionTypeHandle
ves_icall_RuntimeType_MakePointerType (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (ref_type);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type (type);
	mono_class_init_checked (klass, error);
	if (!is_ok (error))
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);

	check_for_invalid_type (klass, error);
	if (!is_ok (error))
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);

	MonoClass *pklass = mono_ptr_class_get (type);

	return mono_type_get_object_handle (domain, &pklass->byval_arg, error);
}

ICALL_EXPORT MonoObjectHandle
ves_icall_System_Delegate_CreateDelegate_internal (MonoReflectionTypeHandle ref_type, MonoObjectHandle target,
						   MonoReflectionMethodHandle info, MonoBoolean throwOnBindFailure, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *delegate_class = mono_class_from_mono_type (type);
	gpointer func;
	MonoMethod *method = MONO_HANDLE_GETVAL (info, method);
	MonoMethodSignature *sig = mono_method_signature(method);

	mono_class_init_checked (delegate_class, error);
	return_val_if_nok (error, NULL_HANDLE);

	if (!(delegate_class->parent == mono_defaults.multicastdelegate_class)) {
		/* FIXME improve this exception message */
		mono_error_set_execution_engine (error, "file %s: line %d (%s): assertion failed: (%s)", __FILE__, __LINE__,
						 __func__,
						 "delegate_class->parent == mono_defaults.multicastdelegate_class");
		return NULL_HANDLE;
	}

	if (mono_security_core_clr_enabled ()) {
		MonoError security_error;
		if (!mono_security_core_clr_ensure_delegate_creation (method, &security_error)) {
			if (throwOnBindFailure)
				mono_error_move (error, &security_error);
			else
				mono_error_cleanup (&security_error);
			return NULL_HANDLE;
		}
	}

	if (sig->generic_param_count && method->wrapper_type == MONO_WRAPPER_NONE) {
		if (!method->is_inflated) {
			mono_error_set_argument (error, "method", " Cannot bind to the target method because its signature differs from that of the delegate type");
			return NULL_HANDLE;
		}
	}

	MonoObjectHandle delegate = MONO_HANDLE_NEW (MonoObject, mono_object_new_checked (MONO_HANDLE_DOMAIN (ref_type), delegate_class, error));
	return_val_if_nok (error, NULL_HANDLE);

	if (method_is_dynamic (method)) {
		/* Creating a trampoline would leak memory */
		func = mono_compile_method_checked (method, error);
		return_val_if_nok (error, NULL_HANDLE);
	} else {
		if (!MONO_HANDLE_IS_NULL (target) && method->flags & METHOD_ATTRIBUTE_VIRTUAL && method->klass != mono_handle_class (target)) {
			method = mono_object_handle_get_virtual_method (target, method, error);
			return_val_if_nok (error, NULL_HANDLE);
		}
		gpointer trampoline = mono_runtime_create_jump_trampoline (mono_domain_get (), method, TRUE, error);
		return_val_if_nok (error, NULL_HANDLE);
		func = mono_create_ftnptr (mono_domain_get (), trampoline);
	}

	mono_delegate_ctor_with_method (delegate, target, func, method, error);
	return_val_if_nok (error, NULL_HANDLE);
	return delegate;
}

ICALL_EXPORT MonoMulticastDelegateHandle
ves_icall_System_Delegate_AllocDelegateLike_internal (MonoDelegateHandle delegate, MonoError *error)
{
	error_init (error);

	MonoClass *klass = mono_handle_class (delegate);
	g_assert (mono_class_has_parent (klass, mono_defaults.multicastdelegate_class));

	MonoMulticastDelegateHandle ret = MONO_HANDLE_NEW (MonoMulticastDelegate,  mono_object_new_checked (MONO_HANDLE_DOMAIN (delegate), klass, error));
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoMulticastDelegate, NULL_HANDLE));

	MONO_HANDLE_SETVAL (MONO_HANDLE_CAST (MonoDelegate, ret), invoke_impl, gpointer, mono_runtime_create_delegate_trampoline (klass));

	return ret;
}

ICALL_EXPORT MonoReflectionMethodHandle
ves_icall_System_Delegate_GetVirtualMethod_internal (MonoDelegateHandle delegate, MonoError *error)
{
	error_init (error);

	MonoObjectHandle delegate_target = MONO_HANDLE_NEW_GET (MonoObject, delegate, target);
	MonoMethod *m = mono_object_handle_get_virtual_method (delegate_target, MONO_HANDLE_GETVAL (delegate, method), error);
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE));
	return mono_method_get_object_handle (mono_domain_get (), m, m->klass, error);
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

	if (count < 0) {
		mono_set_pending_exception (mono_get_exception_argument ("count", "is negative"));
		return FALSE;
	}

	g_assert (count >= 0);

	/* This is called directly from the class libraries without going through the managed wrapper */
	MONO_CHECK_ARG_NULL (src, FALSE);
	MONO_CHECK_ARG_NULL (dest, FALSE);

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
ICALL_EXPORT MonoObjectHandle
ves_icall_Remoting_RealProxy_GetTransparentProxy (MonoObjectHandle this_obj, MonoStringHandle class_name, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (this_obj);
	MonoRealProxyHandle rp = MONO_HANDLE_CAST (MonoRealProxy, this_obj);

	MonoObjectHandle res = MONO_HANDLE_NEW (MonoObject, mono_object_new_checked (domain, mono_defaults.transparent_proxy_class, error));
	if (!is_ok (error))
		return NULL_HANDLE;

	MonoTransparentProxyHandle tp = MONO_HANDLE_CAST (MonoTransparentProxy, res);
	
	MONO_HANDLE_SET (tp, rp, rp);

	MonoReflectionTypeHandle reftype = MONO_HANDLE_NEW (MonoReflectionType, NULL);
	MONO_HANDLE_GET (reftype, rp, class_to_proxy);
	MonoType *type = MONO_HANDLE_GETVAL (reftype, type);
	MonoClass *klass = mono_class_from_mono_type (type);

	// mono_remote_class_vtable cannot handle errors well, so force any loading error to occur early
	mono_class_setup_vtable (klass);
	if (mono_class_has_failure (klass)) {
		mono_error_set_for_class_failure (error, klass);
		return NULL_HANDLE;
	}

	MonoObjectHandle remoting_obj = mono_object_handle_isinst (this_obj, mono_defaults.iremotingtypeinfo_class, error);
	if (!is_ok (error))
		return NULL_HANDLE;
	MONO_HANDLE_SETVAL (tp, custom_type_info, MonoBoolean, !MONO_HANDLE_IS_NULL (remoting_obj));

	MonoRemoteClass *remote_class = mono_remote_class (domain, class_name, klass, error);
	if (!is_ok (error))
		return NULL_HANDLE;
	MONO_HANDLE_SETVAL (tp, remote_class, MonoRemoteClass*, remote_class);

	MONO_HANDLE_SETVAL (res, vtable, MonoVTable*, mono_remote_class_vtable (domain, remote_class, rp, error));
	if (!is_ok (error))
		return NULL_HANDLE;
	return res;
}

ICALL_EXPORT MonoReflectionType *
ves_icall_Remoting_RealProxy_InternalGetProxyType (MonoTransparentProxy *tp)
{
	MonoError error;
	g_assert (tp != NULL && mono_object_class (tp) == mono_defaults.transparent_proxy_class);
	g_assert (tp->remote_class != NULL && tp->remote_class->proxy_class != NULL);
	MonoReflectionType *ret = mono_type_get_object_checked (mono_object_domain (tp), &tp->remote_class->proxy_class->byval_arg, &error);
	mono_error_set_pending_exception (&error);

	return ret;
}
#endif

/* System.Environment */

MonoStringHandle
ves_icall_System_Environment_get_UserName (MonoError *error)
{
	error_init (error);
	/* using glib is more portable */
	return mono_string_new_handle (mono_domain_get (), g_get_user_name (), error);
}

#ifndef HOST_WIN32
static MonoStringHandle
mono_icall_get_machine_name (MonoError *error)
{
	error_init (error);
#if !defined(DISABLE_SOCKETS)
	MonoStringHandle result;
	char *buf;
	int n;
#if defined _SC_HOST_NAME_MAX
	n = sysconf (_SC_HOST_NAME_MAX);
	if (n == -1)
#endif
	n = 512;
	buf = g_malloc (n+1);
	
	if (gethostname (buf, n) == 0){
		buf [n] = 0;
		result = mono_string_new_handle (mono_domain_get (), buf, error);
	} else
		result = MONO_HANDLE_CAST (MonoString, NULL_HANDLE);
	g_free (buf);
	
	return result;
#else
	return mono_string_new_handle (mono_domain_get (), "mono", error);
#endif
}
#endif /* !HOST_WIN32 */

ICALL_EXPORT MonoStringHandle
ves_icall_System_Environment_get_MachineName (MonoError *error)
{
	error_init (error);
	return mono_icall_get_machine_name (error);
}

#ifndef HOST_WIN32
static inline int
mono_icall_get_platform (void)
{
#if defined(__MACH__)
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
#endif /* !HOST_WIN32 */

ICALL_EXPORT int
ves_icall_System_Environment_get_Platform (void)
{
	return mono_icall_get_platform ();
}

#ifndef HOST_WIN32
static inline MonoStringHandle
mono_icall_get_new_line (MonoError *error)
{
	error_init (error);
	return mono_string_new_handle (mono_domain_get (), "\n", error);
}
#endif /* !HOST_WIN32 */

ICALL_EXPORT MonoStringHandle
ves_icall_System_Environment_get_NewLine (MonoError *error)
{
	return mono_icall_get_new_line (error);
}

#ifndef HOST_WIN32
static inline MonoBoolean
mono_icall_is_64bit_os (void)
{
#if SIZEOF_VOID_P == 8
	return TRUE;
#else
#if defined(HAVE_SYS_UTSNAME_H)
	struct utsname name;

	if (uname (&name) >= 0) {
		return strcmp (name.machine, "x86_64") == 0 || strncmp (name.machine, "aarch64", 7) == 0 || strncmp (name.machine, "ppc64", 5) == 0;
	}
#endif
	return FALSE;
#endif
}
#endif /* !HOST_WIN32 */

ICALL_EXPORT MonoBoolean
ves_icall_System_Environment_GetIs64BitOperatingSystem (void)
{
	return mono_icall_is_64bit_os ();
}

ICALL_EXPORT MonoStringHandle
ves_icall_System_Environment_GetEnvironmentVariable_native (const gchar *utf8_name, MonoError *error)
{
	gchar *value;

	if (utf8_name == NULL)
		return NULL_HANDLE_STRING;

	value = g_getenv (utf8_name);

	if (value == 0)
		return NULL_HANDLE_STRING;
	
	MonoStringHandle res = mono_string_new_handle (mono_domain_get (), value, error);
	g_free (value);
	return res;
}

/*
 * There is no standard way to get at environ.
 */
#ifndef _MSC_VER
#ifndef __MINGW32_VERSION
#if defined(__APPLE__)
#if defined (TARGET_OSX)
/* Apple defines this in crt_externs.h but doesn't provide that header for 
 * arm-apple-darwin9.  We'll manually define the symbol on Apple as it does
 * in fact exist on all implementations (so far) 
 */
gchar ***_NSGetEnviron(void);
#define environ (*_NSGetEnviron())
#else
static char *mono_environ[1] = { NULL };
#define environ mono_environ
#endif /* defined (TARGET_OSX) */
#else
extern
char **environ;
#endif
#endif
#endif

ICALL_EXPORT MonoArray *
ves_icall_System_Environment_GetCommandLineArgs (void)
{
	MonoError error;
	MonoArray *result = mono_runtime_get_main_args_checked (&error);
	mono_error_set_pending_exception (&error);
	return result;
}

#ifndef HOST_WIN32
static MonoArray *
mono_icall_get_environment_variable_names (MonoError *error)
{
	MonoArray *names;
	MonoDomain *domain;
	MonoString *str;
	gchar **e, **parts;
	int n;

	error_init (error);
	n = 0;
	for (e = environ; *e != 0; ++ e)
		++ n;

	domain = mono_domain_get ();
	names = mono_array_new_checked (domain, mono_defaults.string_class, n, error);
	return_val_if_nok (error, NULL);

	n = 0;
	for (e = environ; *e != 0; ++ e) {
		parts = g_strsplit (*e, "=", 2);
		if (*parts != 0) {
			str = mono_string_new_checked (domain, *parts, error);
			if (!is_ok (error)) {
				g_strfreev (parts);
				return NULL;
			}
			mono_array_setref (names, n, str);
		}

		g_strfreev (parts);

		++ n;
	}

	return names;
}
#endif /* !HOST_WIN32 */

ICALL_EXPORT MonoArray *
ves_icall_System_Environment_GetEnvironmentVariableNames (void)
{
	MonoError error;
	MonoArray *result = mono_icall_get_environment_variable_names (&error);
	mono_error_set_pending_exception (&error);
	return result;
}

#ifndef HOST_WIN32
static void
mono_icall_set_environment_variable (MonoString *name, MonoString *value)
{
	gchar *utf8_name, *utf8_value;
	MonoError error;

	utf8_name = mono_string_to_utf8_checked (name, &error);	/* FIXME: this should be ascii */
	if (mono_error_set_pending_exception (&error))
		return;

	if ((value == NULL) || (mono_string_length (value) == 0) || (mono_string_chars (value)[0] == 0)) {
		g_unsetenv (utf8_name);
		g_free (utf8_name);
		return;
	}

	utf8_value = mono_string_to_utf8_checked (value, &error);
	if (!mono_error_ok (&error)) {
		g_free (utf8_name);
		mono_error_set_pending_exception (&error);
		return;
	}
	g_setenv (utf8_name, utf8_value, TRUE);

	g_free (utf8_name);
	g_free (utf8_value);
}
#endif /* !HOST_WIN32 */

ICALL_EXPORT void
ves_icall_System_Environment_InternalSetEnvironmentVariable (MonoString *name, MonoString *value)
{
	mono_icall_set_environment_variable (name, value);
}

ICALL_EXPORT void
ves_icall_System_Environment_Exit (int result)
{
	mono_environment_exitcode_set (result);

	if (!mono_runtime_try_shutdown ())
		mono_thread_exit ();

	/* Suspend all managed threads since the runtime is going away */
	mono_thread_suspend_all_other_threads ();

	mono_runtime_quit ();

	/* we may need to do some cleanup here... */
	exit (result);
}

ICALL_EXPORT MonoStringHandle
ves_icall_System_Environment_GetGacPath (MonoError *error)
{
	return mono_string_new_handle (mono_domain_get (), mono_assembly_getrootdir (), error);
}

#ifndef HOST_WIN32
static inline MonoStringHandle
mono_icall_get_windows_folder_path (int folder, MonoError *error)
{
	error_init (error);
	g_warning ("ves_icall_System_Environment_GetWindowsFolderPath should only be called on Windows!");
	return mono_string_new_handle (mono_domain_get (), "", error);
}
#endif /* !HOST_WIN32 */

ICALL_EXPORT MonoStringHandle
ves_icall_System_Environment_GetWindowsFolderPath (int folder, MonoError *error)
{
	return mono_icall_get_windows_folder_path (folder, error);
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
static MonoArray *
mono_icall_get_logical_drives (void)
{
	MonoError error;
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
		size = (guint) mono_w32file_get_logical_drive (initial_size, ptr);
		if (size > initial_size) {
			if (ptr != buf)
				g_free (ptr);
			ptr = (gunichar2 *)g_malloc0 ((size + 1) * sizeof (gunichar2));
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
	result = mono_array_new_checked (domain, mono_defaults.string_class, ndrives, &error);
	if (mono_error_set_pending_exception (&error))
		goto leave;

	ndrives = 0;
	do {
		len = 0;
		u16 = dname;
		while (*u16) { u16++; len ++; }
		drivestr = mono_string_new_utf16_checked (domain, dname, len, &error);
		if (mono_error_set_pending_exception (&error))
			goto leave;

		mono_array_setref (result, ndrives++, drivestr);
		while (*dname++);
	} while (*dname);

leave:
	if (ptr != buf)
		g_free (ptr);

	return result;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

ICALL_EXPORT MonoArray *
ves_icall_System_Environment_GetLogicalDrives (void)
{
	return mono_icall_get_logical_drives ();
}

ICALL_EXPORT MonoString *
ves_icall_System_IO_DriveInfo_GetDriveFormat (MonoString *path)
{
	MonoError error;
	gunichar2 volume_name [MAX_PATH + 1];
	
	if (mono_w32file_get_volume_information (mono_string_chars (path), NULL, 0, NULL, NULL, NULL, volume_name, MAX_PATH + 1) == FALSE)
		return NULL;
	MonoString *result = mono_string_from_utf16_checked (volume_name, &error);
	mono_error_set_pending_exception (&error);
	return result;
}

ICALL_EXPORT MonoStringHandle
ves_icall_System_Environment_InternalGetHome (MonoError *error)
{
	return mono_string_new_handle (mono_domain_get (), g_get_home_dir (), error);
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
ICALL_EXPORT MonoStringHandle
ves_icall_System_Text_EncodingHelper_InternalCodePage (gint32 *int_code_page, MonoError *error)
{
	error_init (error);
	const char *cset;
	const char *p;
	char *c;
	char *codepage = NULL;
	int code;
	int want_name = *int_code_page;
	int i;
	
	*int_code_page = -1;

	g_get_charset (&cset);
	c = codepage = g_strdup (cset);
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
		if ((gsize) p < 7){
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
	g_free (codepage);
	
	if (want_name && *int_code_page == -1)
		return mono_string_new_handle (mono_domain_get (), cset, error);
	else
		return MONO_HANDLE_CAST (MonoString, NULL_HANDLE);
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

#ifndef HOST_WIN32
static inline MonoBoolean
mono_icall_broadcast_setting_change (MonoError *error)
{
	error_init (error);
	return TRUE;
}
#endif /* !HOST_WIN32 */

ICALL_EXPORT void
ves_icall_System_Environment_BroadcastSettingChange (MonoError *error)
{
	error_init (error);
	mono_icall_broadcast_setting_change (error);
}

ICALL_EXPORT
gint32
ves_icall_System_Environment_get_TickCount (void)
{
	/* this will overflow after ~24 days */
	return (gint32) (mono_msec_boottime () & 0xffffffff);
}

ICALL_EXPORT gint32
ves_icall_System_Runtime_Versioning_VersioningHelper_GetRuntimeId (void)
{
	return 9;
}

#ifndef DISABLE_REMOTING
ICALL_EXPORT MonoBoolean
ves_icall_IsTransparentProxy (MonoObjectHandle proxy, MonoError *error)
{
	error_init (error);
	if (MONO_HANDLE_IS_NULL (proxy))
		return 0;

	if (mono_class_is_transparent_proxy (mono_handle_class (proxy)))
		return 1;

	return 0;
}

ICALL_EXPORT MonoReflectionMethodHandle
ves_icall_Remoting_RemotingServices_GetVirtualMethod (
	MonoReflectionTypeHandle rtype, MonoReflectionMethodHandle rmethod, MonoError *error)
{
	MonoReflectionMethodHandle ret = MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE);

	error_init (error);
	if (MONO_HANDLE_IS_NULL (rtype)) {
		mono_error_set_argument_null (error, "type", "");
		return ret;
	}
	if (MONO_HANDLE_IS_NULL (rmethod)) {
		mono_error_set_argument_null (error, "method", "");
		return ret;
	}

	MonoMethod *method = MONO_HANDLE_GETVAL (rmethod, method);
	MonoType *type = MONO_HANDLE_GETVAL (rtype, type);
	MonoClass *klass = mono_class_from_mono_type (type);
	mono_class_init_checked (klass, error);
	return_val_if_nok (error, ret);

	if (MONO_CLASS_IS_INTERFACE (klass))
		return ret;

	if (method->flags & METHOD_ATTRIBUTE_STATIC)
		return ret;

	if ((method->flags & METHOD_ATTRIBUTE_FINAL) || !(method->flags & METHOD_ATTRIBUTE_VIRTUAL)) {
		if (klass == method->klass || mono_class_is_subclass_of (klass, method->klass, FALSE))
			ret = rmethod;
		return ret;
	}

	mono_class_setup_vtable (klass);
	MonoMethod **vtable = klass->vtable;

	MonoMethod *res = NULL;
	if (mono_class_is_interface (method->klass)) {
		gboolean variance_used = FALSE;
		/*MS fails with variant interfaces but it's the right thing to do anyway.*/
		int offs = mono_class_interface_offset_with_variance (klass, method->klass, &variance_used);
		if (offs >= 0)
			res = vtable [offs + method->slot];
	} else {
		if (!(klass == method->klass || mono_class_is_subclass_of (klass, method->klass, FALSE)))
			return ret;

		if (method->slot != -1)
			res = vtable [method->slot];
	}

	if (!res)
		return ret;

	ret = mono_method_get_object_handle (mono_domain_get (), res, NULL, error);
	return ret;
}

ICALL_EXPORT void
ves_icall_System_Runtime_Activation_ActivationServices_EnableProxyActivation (MonoReflectionTypeHandle type, MonoBoolean enable, MonoError *error)
{
	error_init (error);

	MonoClass *klass = mono_class_from_mono_type (MONO_HANDLE_GETVAL (type, type));
	MonoVTable *vtable = mono_class_vtable_full (mono_domain_get (), klass, error);
	return_if_nok (error);

	mono_vtable_set_is_remote (vtable, enable);
}

#else /* DISABLE_REMOTING */

ICALL_EXPORT void
ves_icall_System_Runtime_Activation_ActivationServices_EnableProxyActivation (MonoReflectionTypeHandle type, MonoBoolean enable, MonoError *error)
{
	error_init (error);
	g_assert_not_reached ();
}

#endif

ICALL_EXPORT MonoObjectHandle
ves_icall_System_Runtime_Activation_ActivationServices_AllocateUninitializedClassInstance (MonoReflectionTypeHandle type, MonoError *error)
{
	error_init (error);
	
	MonoDomain *domain = MONO_HANDLE_DOMAIN (type);
	MonoClass *klass = mono_class_from_mono_type (MONO_HANDLE_GETVAL (type, type));
	mono_class_init_checked (klass, error);
	return_val_if_nok (error, NULL_HANDLE);

	if (MONO_CLASS_IS_INTERFACE (klass) || mono_class_is_abstract (klass)) {
		mono_error_set_argument (error, "type", "Type cannot be instantiated");
		return NULL_HANDLE;
	}

	if (klass->rank >= 1) {
		g_assert (klass->rank == 1);
		return MONO_HANDLE_CAST (MonoObject, mono_array_new_handle (domain, klass->element_class, 0, error));
	} else {
		MonoVTable *vtable = mono_class_vtable_full (domain, klass, error);
		return_val_if_nok (error, NULL_HANDLE);

		/* Bypass remoting object creation check */
		return MONO_HANDLE_NEW (MonoObject, mono_object_new_alloc_specific_checked (vtable, error));
	}
}

ICALL_EXPORT MonoStringHandle
ves_icall_System_IO_get_temp_path (MonoError *error)
{
	return mono_string_new_handle (mono_domain_get (), g_get_tmp_dir (), error);
}

#ifndef PLATFORM_NO_DRIVEINFO
ICALL_EXPORT MonoBoolean
ves_icall_System_IO_DriveInfo_GetDiskFreeSpace (MonoString *path_name, guint64 *free_bytes_avail,
						guint64 *total_number_of_bytes, guint64 *total_number_of_free_bytes,
						gint32 *error)
{
	gboolean result;

	*error = ERROR_SUCCESS;

	result = mono_w32file_get_disk_free_space (mono_string_chars (path_name), free_bytes_avail, total_number_of_bytes, total_number_of_free_bytes);
	if (!result)
		*error = mono_w32error_get_last ();

	return result;
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
static inline guint32
mono_icall_drive_info_get_drive_type (MonoString *root_path_name)
{
	return mono_w32file_get_drive_type (mono_string_chars (root_path_name));
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

ICALL_EXPORT guint32
ves_icall_System_IO_DriveInfo_GetDriveType (MonoString *root_path_name)
{
	return mono_icall_drive_info_get_drive_type (root_path_name);
}

#endif /* PLATFORM_NO_DRIVEINFO */

ICALL_EXPORT gpointer
ves_icall_RuntimeMethodHandle_GetFunctionPointer (MonoMethod *method, MonoError *error)
{
	error_init (error);
	return mono_compile_method_checked (method, error);
}

ICALL_EXPORT MonoStringHandle
ves_icall_System_Configuration_DefaultConfig_get_machine_config_path (MonoError *error)
{
	error_init (error);
	gchar *path;

	path = g_build_path (G_DIR_SEPARATOR_S, mono_get_config_dir (), "mono", mono_get_runtime_info ()->framework_version, "machine.config", NULL);

	mono_icall_make_platform_path (path);

	MonoStringHandle mcpath = mono_string_new_handle (mono_domain_get (), path, error);
	g_free (path);

	mono_error_assert_ok (error);

	return mcpath;
}

static MonoStringHandle
ves_icall_System_Configuration_InternalConfigurationHost_get_bundled_app_config (MonoError *error)
{
	error_init (error);
	const gchar *app_config;
	MonoDomain *domain;
	gchar *config_file_name, *config_file_path;
	gsize len, config_file_path_length, config_ext_length;
	gchar *module;

	domain = mono_domain_get ();
	MonoStringHandle file = MONO_HANDLE_NEW (MonoString, domain->setup->configuration_file);
	if (MONO_HANDLE_IS_NULL (file) || MONO_HANDLE_GETVAL (file, length) == 0)
		return NULL;

	// Retrieve config file and remove the extension
	config_file_name = mono_string_handle_to_utf8 (file, error);
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoString, NULL_HANDLE));

	config_file_path = mono_portability_find_file (config_file_name, TRUE);
	if (!config_file_path)
		config_file_path = config_file_name;

	config_file_path_length = strlen (config_file_path);
	config_ext_length = strlen (".config");
	if (config_file_path_length <= config_ext_length) {
		if (config_file_name != config_file_path)
			g_free (config_file_name);
		return MONO_HANDLE_CAST (MonoString, NULL_HANDLE);
	}

	len = config_file_path_length - config_ext_length;
	module = (gchar *)g_malloc0 (len + 1);
	memcpy (module, config_file_path, len);
	// Get the config file from the module name
	app_config = mono_config_string_for_assembly_file (module);
	// Clean-up
	g_free (module);
	if (config_file_name != config_file_path)
		g_free (config_file_name);
	g_free (config_file_path);

	if (!app_config)
		return MONO_HANDLE_CAST (MonoString, NULL_HANDLE);

	return mono_string_new_handle (mono_domain_get (), app_config, error);
}

static MonoStringHandle
get_bundled_machine_config (MonoError *error)
{
	const gchar *machine_config;

	machine_config = mono_get_machine_config ();

	if (!machine_config)
		return NULL_HANDLE_STRING;

	return mono_string_new_handle (mono_domain_get (), machine_config, error);
}

ICALL_EXPORT MonoStringHandle
ves_icall_System_Environment_get_bundled_machine_config (MonoError *error)
{
	return get_bundled_machine_config (error);
}


ICALL_EXPORT MonoStringHandle
ves_icall_System_Configuration_DefaultConfig_get_bundled_machine_config (MonoError *error)
{
	return get_bundled_machine_config (error);
}

ICALL_EXPORT MonoStringHandle
ves_icall_System_Configuration_InternalConfigurationHost_get_bundled_machine_config (MonoError *error)
{
	return get_bundled_machine_config (error);
}


ICALL_EXPORT MonoStringHandle
ves_icall_System_Web_Util_ICalls_get_machine_install_dir (MonoError *error)
{
	error_init (error);
	gchar *path;

	path = g_path_get_dirname (mono_get_config_dir ());

	mono_icall_make_platform_path (path);

	MonoStringHandle ipath = mono_string_new_handle (mono_domain_get (), path, error);
	g_free (path);

	return ipath;
}

ICALL_EXPORT gboolean
ves_icall_get_resources_ptr (MonoReflectionAssemblyHandle assembly, gpointer *result, gint32 *size, MonoError *error)
{
	error_init (error);
	MonoPEResourceDataEntry *entry;
	MonoImage *image;

	if (!assembly || !result || !size)
		return FALSE;

	*result = NULL;
	*size = 0;
	MonoAssembly *assm = MONO_HANDLE_GETVAL (assembly, assembly);
	image = assm->image;
	entry = (MonoPEResourceDataEntry *)mono_image_lookup_resource (image, MONO_PE_RESOURCE_ID_ASPNET_STRING, 0, NULL);
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

#ifndef HOST_WIN32
static inline void
mono_icall_write_windows_debug_string (MonoString *message)
{
	g_warning ("WriteWindowsDebugString called and HOST_WIN32 not defined!\n");
}
#endif /* !HOST_WIN32 */

ICALL_EXPORT void
ves_icall_System_Diagnostics_DefaultTraceListener_WriteWindowsDebugString (MonoString *message)
{
	mono_icall_write_windows_debug_string (message);
}

/* Only used for value types */
ICALL_EXPORT MonoObjectHandle
ves_icall_System_Activator_CreateInstanceInternal (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoDomain *domain = MONO_HANDLE_DOMAIN (ref_type);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type (type);

	mono_class_init_checked (klass, error);
	if (!is_ok (error))
		return NULL_HANDLE;

	if (mono_class_is_nullable (klass))
		/* No arguments -> null */
		return NULL_HANDLE;

	return MONO_HANDLE_NEW (MonoObject, mono_object_new_checked (domain, klass, error));
}

ICALL_EXPORT MonoReflectionMethodHandle
ves_icall_MonoMethod_get_base_method (MonoReflectionMethodHandle m, gboolean definition, MonoError *error)
{
	error_init (error);
	MonoMethod *method = MONO_HANDLE_GETVAL (m, method);

	MonoMethod *base = mono_method_get_base_method (method, definition, error);
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE));
	if (base == method) {
		/* we want to short-circuit and return 'm' here. But we should
		   return the same method object that
		   mono_method_get_object_handle, below would return.  Since
		   that call takes NULL for the reftype argument, it will take
		   base->klass as the reflected type for the MonoMethod.  So we
		   need to check that m also has base->klass as the reflected
		   type. */
		MonoReflectionTypeHandle orig_reftype = MONO_HANDLE_NEW_GET (MonoReflectionType, m, reftype);
		MonoClass *orig_klass = mono_class_from_mono_type (MONO_HANDLE_GETVAL (orig_reftype, type));
		if (base->klass == orig_klass)
			return m;
	}
	return mono_method_get_object_handle (mono_domain_get (), base, NULL, error);
}

ICALL_EXPORT MonoStringHandle
ves_icall_MonoMethod_get_name (MonoReflectionMethodHandle m, MonoError *error)
{
	error_init (error);
	MonoMethod *method = MONO_HANDLE_GETVAL (m, method);

	MonoStringHandle s = mono_string_new_handle (MONO_HANDLE_DOMAIN (m), method->name, error);
	if (!is_ok (error))
		return NULL_HANDLE_STRING;
	MONO_HANDLE_SET (m, name, s);
	return s;
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
mono_TypedReference_ToObject (MonoTypedRef* tref)
{
	MonoError error;
	MonoObject *result = NULL;
	if (MONO_TYPE_IS_REFERENCE (tref->type)) {
		MonoObject** objp = (MonoObject **)tref->value;
		return *objp;
	}

	result = mono_value_box_checked (mono_domain_get (), tref->klass, tref->value, &error);
	mono_error_set_pending_exception (&error);
	return result;
}

ICALL_EXPORT MonoTypedRef
mono_TypedReference_MakeTypedReferenceInternal (MonoObject *target, MonoArray *fields)
{
	MonoTypedRef res;
	MonoReflectionField *f;
	MonoClass *klass;
	MonoType *ftype = NULL;
	guint8 *p = NULL;
	int i;

	memset (&res, 0, sizeof (res));

	g_assert (fields);
	g_assert (mono_array_length (fields) > 0);

	klass = target->vtable->klass;

	for (i = 0; i < mono_array_length (fields); ++i) {
		f = mono_array_get (fields, MonoReflectionField*, i);
		if (f == NULL) {
			mono_set_pending_exception (mono_get_exception_argument_null ("field"));
			return res;
		}
		if (f->field->parent != klass) {
			mono_set_pending_exception (mono_get_exception_argument ("field", ""));
			return res;
		}
		if (i == 0)
			p = (guint8*)target + f->field->offset;
		else
			p += f->field->offset - sizeof (MonoObject);
		klass = mono_class_from_mono_type (f->field->type);
		ftype = f->field->type;
	}

	res.type = ftype;
	res.klass = mono_class_from_mono_type (ftype);
	res.value = p;

	return res;
}

static void
prelink_method (MonoMethod *method, MonoError *error)
{
	const char *exc_class, *exc_arg;

	error_init (error);
	if (!(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		return;
	mono_lookup_pinvoke_call (method, &exc_class, &exc_arg);
	if (exc_class) {
		mono_error_set_generic_error (error, "System", exc_class, "%s", exc_arg);
		return;
	}
	/* create the wrapper, too? */
}

ICALL_EXPORT void
ves_icall_System_Runtime_InteropServices_Marshal_Prelink (MonoReflectionMethodHandle method, MonoError *error)
{
	error_init (error);

	prelink_method (MONO_HANDLE_GETVAL (method, method), error);
}

ICALL_EXPORT void
ves_icall_System_Runtime_InteropServices_Marshal_PrelinkAll (MonoReflectionTypeHandle type, MonoError *error)
{
	error_init (error);
	MonoClass *klass = mono_class_from_mono_type (MONO_HANDLE_GETVAL (type, type));
	MonoMethod* m;
	gpointer iter = NULL;

	mono_class_init_checked (klass, error);
	return_if_nok (error);

	while ((m = mono_class_get_methods (klass, &iter))) {
		prelink_method (m, error);
		return_if_nok (error);
	}
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

static gboolean
add_modifier_to_array (MonoDomain *domain, MonoImage *image, MonoCustomMod *modifier, MonoArrayHandle dest, int dest_idx, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoClass *klass = mono_class_get_checked (image, modifier->token, error);
	if (!is_ok (error))
		goto leave;

	MonoReflectionTypeHandle rt = mono_type_get_object_handle (domain, &klass->byval_arg, error);
	if (!is_ok (error))
		goto leave;

	MONO_HANDLE_ARRAY_SETREF (dest, dest_idx, rt);
leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

/*
 * We return NULL for no modifiers so the corlib code can return Type.EmptyTypes
 * and avoid useless allocations.
 */
static MonoArrayHandle
type_array_from_modifiers (MonoImage *image, MonoType *type, int optional, MonoError *error)
{
	int i, count = 0;
	MonoDomain *domain = mono_domain_get ();

	error_init (error);
	for (i = 0; i < type->num_mods; ++i) {
		if ((optional && !type->modifiers [i].required) || (!optional && type->modifiers [i].required))
			count++;
	}
	if (!count)
		return MONO_HANDLE_NEW (MonoArray, NULL);

	MonoArrayHandle res = mono_array_new_handle (domain, mono_defaults.systemtype_class, count, error);
	if (!is_ok (error))
		goto fail;
	count = 0;
	for (i = 0; i < type->num_mods; ++i) {
		if ((optional && !type->modifiers [i].required) || (!optional && type->modifiers [i].required)) {
			if (!add_modifier_to_array (domain, image, &type->modifiers[i], res, count , error))
				goto fail;
			count++;
		}
	}
	return res;
fail:
	return MONO_HANDLE_NEW (MonoArray, NULL);
}

ICALL_EXPORT MonoArrayHandle
ves_icall_ParameterInfo_GetTypeModifiers (MonoReflectionParameterHandle param, MonoBoolean optional, MonoError *error)
{
	error_init (error);
	MonoReflectionTypeHandle rt = MONO_HANDLE_NEW (MonoReflectionType, NULL);
	MONO_HANDLE_GET (rt, param, ClassImpl);
	MonoType *type = MONO_HANDLE_GETVAL (rt, type);
	MonoObjectHandle member = MONO_HANDLE_NEW (MonoObject, NULL);
	MONO_HANDLE_GET (member, param, MemberImpl);
	MonoClass *member_class = mono_handle_class (member);
	MonoMethod *method = NULL;
	MonoImage *image;
	int pos;
	MonoMethodSignature *sig;

	if (mono_class_is_reflection_method_or_constructor (member_class)) {
		method = MONO_HANDLE_GETVAL (MONO_HANDLE_CAST (MonoReflectionMethod, member), method);
	} else if (member_class->image == mono_defaults.corlib && !strcmp ("MonoProperty", member_class->name)) {
		MonoProperty *prop = MONO_HANDLE_GETVAL (MONO_HANDLE_CAST (MonoReflectionProperty, member), property);
		if (!(method = prop->get))
			method = prop->set;
		g_assert (method);	
	} else {
		char *type_name = mono_type_get_full_name (member_class);
		mono_error_set_not_supported (error, "Custom modifiers on a ParamInfo with member %s are not supported", type_name);
		g_free (type_name);
		return MONO_HANDLE_CAST (MonoArray, NULL_HANDLE);
	}

	image = method->klass->image;
	pos = MONO_HANDLE_GETVAL (param, PositionImpl);
	sig = mono_method_signature (method);
	if (pos == -1)
		type = sig->ret;
	else
		type = sig->params [pos];

	return type_array_from_modifiers (image, type, optional, error);
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

ICALL_EXPORT MonoArrayHandle
ves_icall_MonoPropertyInfo_GetTypeModifiers (MonoReflectionPropertyHandle property, MonoBoolean optional, MonoError *error)
{
	error_init (error);
	MonoProperty *prop = MONO_HANDLE_GETVAL (property, property);
	MonoClass *klass = MONO_HANDLE_GETVAL (property, klass);
	MonoType *type = get_property_type (prop);
	MonoImage *image = klass->image;

	if (!type)
		return MONO_HANDLE_CAST (MonoArray, NULL_HANDLE);
	return type_array_from_modifiers (image, type, optional, error);
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
	MonoError error;
	MonoType blob_type;
	MonoProperty *prop = property->property;
	MonoType *type = get_property_type (prop);
	MonoDomain *domain = mono_object_domain (property); 
	MonoTypeEnum def_type;
	const char *def_value;
	MonoObject *o;

	mono_class_init (prop->parent);

	if (!(prop->attrs & PROPERTY_ATTRIBUTE_HAS_DEFAULT)) {
		mono_set_pending_exception (mono_get_exception_invalid_operation (NULL));
		return NULL;
	}

	def_value = mono_class_get_property_default_value (prop, &def_type);

	mono_type_from_blob_type (&blob_type, def_type, type);
	o = mono_get_object_from_blob (domain, &blob_type, def_value, &error);

	mono_error_set_pending_exception (&error);
	return o;
}

ICALL_EXPORT MonoBoolean
ves_icall_MonoCustomAttrs_IsDefinedInternal (MonoObjectHandle obj, MonoReflectionTypeHandle attr_type, MonoError *error)
{
	error_init (error);
	MonoClass *attr_class = mono_class_from_mono_type (MONO_HANDLE_GETVAL (attr_type, type));

	mono_class_init_checked (attr_class, error);
	return_val_if_nok (error, FALSE);

	MonoCustomAttrInfo *cinfo = mono_reflection_get_custom_attrs_info_checked (obj, error);
	return_val_if_nok (error, FALSE);

	if (!cinfo)
		return FALSE;
	gboolean found = mono_custom_attrs_has_attr (cinfo, attr_class);
	if (!cinfo->cached)
		mono_custom_attrs_free (cinfo);
	return found;
}

ICALL_EXPORT MonoArrayHandle
ves_icall_MonoCustomAttrs_GetCustomAttributesInternal (MonoObjectHandle obj, MonoReflectionTypeHandle attr_type, mono_bool pseudoattrs, MonoError *error)
{
	MonoClass *attr_class;
	if (MONO_HANDLE_IS_NULL (attr_type))
		attr_class = NULL;
	else
		attr_class = mono_class_from_mono_type (MONO_HANDLE_GETVAL (attr_type, type));

	if (attr_class) {
		mono_class_init_checked (attr_class, error);
		if (!is_ok (error))
			return MONO_HANDLE_CAST (MonoArray, NULL_HANDLE);
	}

	return mono_reflection_get_custom_attrs_by_type_handle (obj, attr_class, error);
}

ICALL_EXPORT MonoArrayHandle
ves_icall_MonoCustomAttrs_GetCustomAttributesDataInternal (MonoObjectHandle obj, MonoError *error)
{
	error_init (error);
	return mono_reflection_get_custom_attrs_data_checked (obj, error);
}


ICALL_EXPORT MonoStringHandle
ves_icall_Mono_Runtime_GetDisplayName (MonoError *error)
{
	char *info;
	MonoStringHandle display_name;

	error_init (error);
	info = mono_get_runtime_callbacks ()->get_runtime_build_info ();
	display_name = mono_string_new_handle (mono_domain_get (), info, error);
	g_free (info);
	return display_name;
}

#ifndef HOST_WIN32
static inline gint32
mono_icall_wait_for_input_idle (gpointer handle, gint32 milliseconds)
{
	return WAIT_TIMEOUT;
}
#endif /* !HOST_WIN32 */

ICALL_EXPORT gint32
ves_icall_Microsoft_Win32_NativeMethods_WaitForInputIdle (gpointer handle, gint32 milliseconds)
{
	return mono_icall_wait_for_input_idle (handle, milliseconds);
}

ICALL_EXPORT gint32
ves_icall_Microsoft_Win32_NativeMethods_GetCurrentProcessId (void)
{
	return mono_process_current_pid ();
}

ICALL_EXPORT MonoBoolean
ves_icall_Mono_TlsProviderFactory_IsBtlsSupported (void)
{
#if HAVE_BTLS
	return TRUE;
#else
	return FALSE;
#endif
}

#ifndef DISABLE_COM

ICALL_EXPORT int
ves_icall_System_Runtime_InteropServices_Marshal_GetHRForException_WinRT(MonoException* ex)
{
	mono_set_pending_exception(mono_get_exception_not_implemented("System.Runtime.InteropServices.Marshal.GetHRForException_WinRT internal call is not implemented."));
	return 0;
}

ICALL_EXPORT MonoObject*
ves_icall_System_Runtime_InteropServices_Marshal_GetNativeActivationFactory(MonoObject* type)
{
	mono_set_pending_exception(mono_get_exception_not_implemented("System.Runtime.InteropServices.Marshal.GetNativeActivationFactory internal call is not implemented."));
	return NULL;
}

ICALL_EXPORT void*
ves_icall_System_Runtime_InteropServices_Marshal_GetRawIUnknownForComObjectNoAddRef(MonoObject* obj)
{
	mono_set_pending_exception(mono_get_exception_not_implemented("System.Runtime.InteropServices.Marshal.GetRawIUnknownForComObjectNoAddRef internal call is not implemented."));
	return NULL;
}

ICALL_EXPORT MonoObject*
ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_GetRestrictedErrorInfo()
{
	mono_set_pending_exception(mono_get_exception_not_implemented("System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.GetRestrictedErrorInfo internal call is not implemented."));
	return NULL;
}

ICALL_EXPORT MonoBoolean
ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_RoOriginateLanguageException(int error, MonoString* message, void* languageException)
{
	mono_set_pending_exception(mono_get_exception_not_implemented("System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.RoOriginateLanguageException internal call is not implemented."));
	return FALSE;
}

ICALL_EXPORT void
ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_RoReportUnhandledError(MonoObject* error)
{
	mono_set_pending_exception(mono_get_exception_not_implemented("System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.RoReportUnhandledError internal call is not implemented."));
}

ICALL_EXPORT int
ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_WindowsCreateString(MonoString* sourceString, int length, void** hstring)
{
	mono_set_pending_exception(mono_get_exception_not_implemented("System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsCreateString internal call is not implemented."));
	return 0;
}

ICALL_EXPORT int
ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_WindowsDeleteString(void* hstring)
{
	mono_set_pending_exception(mono_get_exception_not_implemented("System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsDeleteString internal call is not implemented."));
	return 0;
}

ICALL_EXPORT mono_unichar2*
ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_WindowsGetStringRawBuffer(void* hstring, unsigned* length)
{
	mono_set_pending_exception(mono_get_exception_not_implemented("System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsGetStringRawBuffer internal call is not implemented."));
	return NULL;
}

#endif


#ifndef DISABLE_ICALL_TABLES

#define ICALL_TYPE(id,name,first)
#define ICALL(id,name,func) Icall_ ## id,
#define HANDLES(inner) inner

enum {
#include "metadata/icall-def.h"
	Icall_last
};

#undef ICALL_TYPE
#undef ICALL
#define ICALL_TYPE(id,name,first) Icall_type_ ## id,
#define ICALL(id,name,func)
#undef HANDLES
#define HANDLES(inner) inner
enum {
#include "metadata/icall-def.h"
	Icall_type_num
};

#undef ICALL_TYPE
#undef ICALL
#define ICALL_TYPE(id,name,firstic) {(Icall_ ## firstic)},
#define ICALL(id,name,func)
#undef HANDLES
#define HANDLES(inner) inner
typedef struct {
	guint16 first_icall;
} IcallTypeDesc;

static const IcallTypeDesc
icall_type_descs [] = {
#include "metadata/icall-def.h"
	{Icall_last}
};

#define icall_desc_num_icalls(desc) ((desc) [1].first_icall - (desc) [0].first_icall)

#undef HANDLES
#define HANDLES(inner) inner
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

#undef HANDLES
#define HANDLES(inner) inner
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
#undef HANDLES
#define HANDLES(inner) inner
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

#undef ICALL_TYPE
#undef ICALL
#define ICALL_TYPE(id,name,first)
#define ICALL(id,name,func) 0,
#undef HANDLES
#define HANDLES(inner) 1,
static const guchar
icall_uses_handles [] = {
#include "metadata/icall-def.h"
#undef ICALL
#undef HANDLES
};

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
	mono_os_mutex_init (&icall_mutex);
}

static void
mono_icall_lock (void)
{
	mono_locks_os_acquire (&icall_mutex, IcallLock);
}

static void
mono_icall_unlock (void)
{
	mono_locks_os_release (&icall_mutex, IcallLock);
}

void
mono_icall_cleanup (void)
{
	g_hash_table_destroy (icall_hash);
	g_hash_table_destroy (jit_icall_hash_name);
	g_hash_table_destroy (jit_icall_hash_addr);
	mono_os_mutex_destroy (&icall_mutex);
}

/**
 * mono_add_internal_call:
 * \param name method specification to surface to the managed world
 * \param method pointer to a C method to invoke when the method is called
 *
 * This method surfaces the C function pointed by \p method as a method
 * that has been surfaced in managed code with the method specified in
 * \p name as an internal call.
 *
 * Internal calls are surfaced to all app domains loaded and they are
 * accessibly by a type with the specified name.
 *
 * You must provide a fully qualified type name, that is namespaces
 * and type name, followed by a colon and the method name, with an
 * optional signature to bind.
 *
 * For example, the following are all valid declarations:
 *
 * \c MyApp.Services.ScriptService:Accelerate
 *
 * \c MyApp.Services.ScriptService:Slowdown(int,bool)
 *
 * You use method parameters in cases where there might be more than
 * one surface method to managed code.  That way you can register different
 * internal calls for different method overloads.
 *
 * The internal calls are invoked with no marshalling.   This means that .NET
 * types like \c System.String are exposed as \c MonoString* parameters.   This is
 * different than the way that strings are surfaced in P/Invoke.
 *
 * For more information on how the parameters are marshalled, see the
 * <a href="http://www.mono-project.com/docs/advanced/embedding/">Mono Embedding</a>
 * page.
 *
 * See the <a  href="mono-api-methods.html#method-desc">Method Description</a>
 * reference for more information on the format of method descriptions.
 */
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

static gsize
find_slot_icall (const IcallTypeDesc *imap, const char *name)
{
	const guint16 *nameslot = (const guint16 *)mono_binary_search (name, icall_names_idx + imap->first_icall, icall_desc_num_icalls (imap), sizeof (icall_names_idx [0]), compare_method_imap);
	if (!nameslot)
		return -1;
	return (nameslot - &icall_names_idx [0]);
}

static gboolean
find_uses_handles_icall (const IcallTypeDesc *imap, const char *name)
{
	gsize slotnum = find_slot_icall (imap, name);
	if (slotnum == -1)
		return FALSE;
	return (gboolean)icall_uses_handles [slotnum];
}

static gpointer
find_method_icall (const IcallTypeDesc *imap, const char *name)
{
	gsize slotnum = find_slot_icall (imap, name);
	if (slotnum == -1)
		return NULL;
	return (gpointer)icall_functions [slotnum];
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
	const guint16 *nameslot = (const guint16 *)mono_binary_search (name, icall_type_names_idx, Icall_type_num, sizeof (icall_type_names_idx [0]), compare_class_imap);
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

static gsize
find_slot_icall (const IcallTypeDesc *imap, const char *name)
{
	const char **nameslot = mono_binary_search (name, icall_names + imap->first_icall, icall_desc_num_icalls (imap), sizeof (icall_names [0]), compare_method_imap);
	if (!nameslot)
		return -1;
	return nameslot - icall_names;
}

static gpointer
find_method_icall (const IcallTypeDesc *imap, const char *name)
{
	gsize slotnum = find_slot_icall (imap, name);
	if (slotnum == -1)
		return NULL;
	return (gpointer)icall_functions [slotnum];
}

static gboolean
find_uses_handles_icall (const IcallTypeDesc *imap, const char *name)
{
	gsize slotnum = find_slot_icall (imap, name);
	if (slotnum == -1)
		return FALSE;
	return (gboolean)icall_uses_handles [slotnum];
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

/**
 * mono_lookup_internal_call_full:
 * \param method the method to look up
 * \param uses_handles out argument if method needs handles around managed objects.
 * \returns a pointer to the icall code for the given method.  If
 * \p uses_handles is not NULL, it will be set to TRUE if the method
 * needs managed objects wrapped using the infrastructure in handle.h
 *
 * If the method is not found, warns and returns NULL.
 */
gpointer
mono_lookup_internal_call_full (MonoMethod *method, mono_bool *uses_handles)
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
		if (uses_handles)
			*uses_handles = FALSE;
		mono_icall_unlock ();;
		return res;
	}
	/* try without signature */
	*sigstart = 0;
	res = g_hash_table_lookup (icall_hash, mname);
	if (res) {
		if (uses_handles)
			*uses_handles = FALSE;
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
		if (uses_handles)
			*uses_handles = FALSE;
		mono_icall_unlock ();
		return NULL;
	}
	res = find_method_icall (imap, sigstart - mlen);
	if (res) {
		if (uses_handles)
			*uses_handles = find_uses_handles_icall (imap, sigstart - mlen);
		mono_icall_unlock ();
		return res;
	}
	/* try _with_ signature */
	*sigstart = '(';
	res = find_method_icall (imap, sigstart - mlen);
	if (res) {
		if (uses_handles)
			*uses_handles = find_uses_handles_icall (imap, sigstart - mlen);
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

/**
 * mono_lookup_internal_call:
 */
gpointer
mono_lookup_internal_call (MonoMethod *method)
{
	return mono_lookup_internal_call_full (method, NULL);
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
type_from_typename (char *type_name)
{
	MonoClass *klass = NULL;	/* assignment to shut GCC warning up */

	if (!strcmp (type_name, "int"))
		klass = mono_defaults.int_class;
	else if (!strcmp (type_name, "ptr"))
		klass = mono_defaults.int_class;
	else if (!strcmp (type_name, "void"))
		klass = mono_defaults.void_class;
	else if (!strcmp (type_name, "int32"))
		klass = mono_defaults.int32_class;
	else if (!strcmp (type_name, "uint32"))
		klass = mono_defaults.uint32_class;
	else if (!strcmp (type_name, "int8"))
		klass = mono_defaults.sbyte_class;
	else if (!strcmp (type_name, "uint8"))
		klass = mono_defaults.byte_class;
	else if (!strcmp (type_name, "int16"))
		klass = mono_defaults.int16_class;
	else if (!strcmp (type_name, "uint16"))
		klass = mono_defaults.uint16_class;
	else if (!strcmp (type_name, "long"))
		klass = mono_defaults.int64_class;
	else if (!strcmp (type_name, "ulong"))
		klass = mono_defaults.uint64_class;
	else if (!strcmp (type_name, "float"))
		klass = mono_defaults.single_class;
	else if (!strcmp (type_name, "double"))
		klass = mono_defaults.double_class;
	else if (!strcmp (type_name, "object"))
		klass = mono_defaults.object_class;
	else if (!strcmp (type_name, "obj"))
		klass = mono_defaults.object_class;
	else if (!strcmp (type_name, "string"))
		klass = mono_defaults.string_class;
	else if (!strcmp (type_name, "bool"))
		klass = mono_defaults.boolean_class;
	else if (!strcmp (type_name, "boolean"))
		klass = mono_defaults.boolean_class;
	else {
		g_error ("%s", type_name);
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
	res = (MonoMethodSignature *)g_hash_table_lookup (corlib->helper_signatures, sigstr);
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

#ifdef TARGET_WIN32
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
	res2 = (MonoMethodSignature *)g_hash_table_lookup (corlib->helper_signatures, sigstr);
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
	info = (MonoJitICallInfo *)g_hash_table_lookup (jit_icall_hash_name, name);
	mono_icall_unlock ();
	return info;
}

MonoJitICallInfo *
mono_find_jit_icall_by_addr (gconstpointer addr)
{
	MonoJitICallInfo *info;
	g_assert (jit_icall_hash_addr);

	mono_icall_lock ();
	info = (MonoJitICallInfo *)g_hash_table_lookup (jit_icall_hash_addr, (gpointer)addr);
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
	info = (MonoJitICallInfo *)g_hash_table_lookup (jit_icall_hash_name, name);
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

/*
 * If NO_RAISE is set, that means the icall is not calling mono_raise_exception () directly or indirectly. The JIT might be able to call these
 * icalls without wrappers in some cases.
 */
MonoJitICallInfo *
mono_register_jit_icall_full (gconstpointer func, const char *name, MonoMethodSignature *sig, gboolean is_save, gboolean no_raise, const char *c_symbol)
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
	info->no_raise = no_raise;

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
	return mono_register_jit_icall_full (func, name, sig, is_save, FALSE, NULL);
}

