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

#if defined(TARGET_WIN32) || defined(HOST_WIN32)
#include <stdio.h>
#endif

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
#include <mono/metadata/monitor.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/image-internals.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/w32file.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/metadata-update.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/class-init.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/appdomain-icalls.h>
#include <mono/metadata/string-icalls.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/mono-perfcounters.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-ptr-array.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/runtime.h>
#include <mono/metadata/seq-points-data.h>
#include <mono/metadata/icall-table.h>
#include <mono/metadata/handle.h>
#include <mono/metadata/w32event.h>
#include <mono/metadata/abi-details.h>
#include <mono/metadata/loader-internals.h>
#include <mono/utils/monobitset.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/mono-string.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-digest.h>
#include <mono/utils/bsearch.h>
#include <mono/utils/mono-os-mutex.h>
#include <mono/utils/mono-threads.h>
#include <mono/metadata/w32error.h>
#include <mono/utils/w32api.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-math.h>
#if !defined(HOST_WIN32) && defined(HAVE_SYS_UTSNAME_H)
#include <sys/utsname.h>
#endif
#if defined(HOST_WIN32)
#include <windows.h>
#endif
#include "icall-decl.h"
#include "mono/utils/mono-threads-coop.h"
#include "mono/metadata/icall-signatures.h"
#include "mono/utils/mono-signal-handler.h"

#if _MSC_VER
#pragma warning(disable:4047) // FIXME differs in levels of indirection
#endif

//#define MONO_DEBUG_ICALLARRAY

// Inline with CoreCLR heuristics, https://github.com/dotnet/runtime/blob/69e114c1abf91241a0eeecf1ecceab4711b8aa62/src/coreclr/vm/threads.cpp#L6408.
// Minimum stack size should be sufficient to allow a typical non-recursive call chain to execute,
// including potential exception handling and garbage collection. Used for probing for available
// stack space through RuntimeHelpers.EnsureSufficientExecutionStack.
#if TARGET_SIZEOF_VOID_P == 8
#define MONO_MIN_EXECUTION_STACK_SIZE (128 * 1024)
#else
#define MONO_MIN_EXECUTION_STACK_SIZE (64 * 1024)
#endif

#ifdef MONO_DEBUG_ICALLARRAY

static char debug_icallarray; // 0:uninitialized 1:true 2:false

static gboolean
icallarray_print_enabled (void)
{
	if (!debug_icallarray)
		debug_icallarray = MONO_TRACE_IS_TRACED (G_LOG_LEVEL_DEBUG, MONO_TRACE_ICALLARRAY) ? 1 : 2;
	return debug_icallarray == 1;
}

static void
icallarray_print (const char *format, ...)
{
	if (!icallarray_print_enabled ())
		return;
	va_list args;
	va_start (args, format);
	g_printv (format, args);
	va_end (args);
}

#else
#define icallarray_print_enabled() (FALSE)
#define icallarray_print(...) /* nothing */
#endif

/* Lazy class loading functions */
static GENERATE_GET_CLASS_WITH_CACHE (module, "System.Reflection", "Module")

static void
array_set_value_impl (MonoArrayHandle arr, MonoObjectHandle value, guint32 pos, gboolean strict_enums, gboolean strict_signs, MonoError *error);

static MonoArrayHandle
type_array_from_modifiers (MonoType *type, int optional, MonoError *error);

static inline MonoBoolean
is_generic_parameter (MonoType *type)
{
	return !type->byref && (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR);
}

#ifdef HOST_WIN32
static void
mono_icall_make_platform_path (gchar *path)
{
	for (size_t i = strlen (path); i > 0; i--)
		if (path [i-1] == '\\')
			path [i-1] = '/';
}

static const gchar *
mono_icall_get_file_path_prefix (const gchar *path)
{
	if (*path == '/' && *(path + 1) == '/') {
		return "file:";
	} else {
		return "file:///";
	}
}

#else

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

MonoJitICallInfos mono_jit_icall_info;

MonoObjectHandle
ves_icall_System_Array_GetValueImpl (MonoArrayHandle array, guint32 pos, MonoError *error)
{
	MonoClass * const array_class = mono_handle_class (array);
	MonoClass * const element_class = m_class_get_element_class (array_class);

	if (m_class_is_native_pointer (element_class)) {
		mono_error_set_not_supported (error, NULL);
		return NULL_HANDLE;
	}

	if (m_class_is_valuetype (element_class)) {
		gsize element_size = mono_array_element_size (array_class);
		gpointer element_address = mono_array_addr_with_size_fast (MONO_HANDLE_RAW (array), element_size, (gsize)pos);
		return mono_value_box_handle (element_class, element_address, error);
	}
	MonoObjectHandle result = mono_new_null ();
	mono_handle_array_getref (result, array, pos);
	return result;
}

void
ves_icall_System_Array_SetValueImpl (MonoArrayHandle arr, MonoObjectHandle value, guint32 pos, MonoError *error)
{
	array_set_value_impl (arr, value, pos, TRUE, TRUE, error);
}

static inline void
set_invalid_cast (MonoError *error, MonoClass *src_class, MonoClass *dst_class)
{
	mono_get_runtime_callbacks ()->set_cast_details (src_class, dst_class);
	mono_error_set_invalid_cast (error);
}

void
ves_icall_System_Array_SetValueRelaxedImpl (MonoArrayHandle arr, MonoObjectHandle value, guint32 pos, MonoError *error)
{
	array_set_value_impl (arr, value, pos, FALSE, FALSE, error);
}

// Copied from CoreCLR: https://github.com/dotnet/coreclr/blob/d3e39bc2f81e3dbf9e4b96347f62b49d8700336c/src/vm/invokeutil.cpp#L33
#define PT_Primitive          0x01000000

static const guint32 primitive_conversions [] = {
	0x00,					// MONO_TYPE_END
	0x00,					// MONO_TYPE_VOID
	PT_Primitive | 0x0004,	// MONO_TYPE_BOOLEAN
	PT_Primitive | 0x3F88,	// MONO_TYPE_CHAR (W = U2, CHAR, I4, U4, I8, U8, R4, R8)
	PT_Primitive | 0x3550,	// MONO_TYPE_I1   (W = I1, I2, I4, I8, R4, R8) 
	PT_Primitive | 0x3FE8,	// MONO_TYPE_U1   (W = CHAR, U1, I2, U2, I4, U4, I8, U8, R4, R8)
	PT_Primitive | 0x3540,	// MONO_TYPE_I2   (W = I2, I4, I8, R4, R8)
	PT_Primitive | 0x3F88,	// MONO_TYPE_U2   (W = U2, CHAR, I4, U4, I8, U8, R4, R8)
	PT_Primitive | 0x3500,	// MONO_TYPE_I4   (W = I4, I8, R4, R8)
	PT_Primitive | 0x3E00,	// MONO_TYPE_U4   (W = U4, I8, R4, R8)
	PT_Primitive | 0x3400,	// MONO_TYPE_I8   (W = I8, R4, R8)
	PT_Primitive | 0x3800,	// MONO_TYPE_U8   (W = U8, R4, R8)
	PT_Primitive | 0x3000,	// MONO_TYPE_R4   (W = R4, R8)
	PT_Primitive | 0x2000,	// MONO_TYPE_R8   (W = R8) 
};

// Copied from CoreCLR: https://github.com/dotnet/coreclr/blob/030a3ea9b8dbeae89c90d34441d4d9a1cf4a7de6/src/vm/invokeutil.h#L176
static 
gboolean can_primitive_widen (MonoTypeEnum src_type, MonoTypeEnum dest_type)
{
	if (dest_type > MONO_TYPE_R8 || src_type > MONO_TYPE_R8) {
		return (MONO_TYPE_I == dest_type && MONO_TYPE_I == src_type) || (MONO_TYPE_U == dest_type && MONO_TYPE_U == src_type);
	}
	return ((1 << dest_type) & primitive_conversions [src_type]) != 0;
}

// Copied from CoreCLR: https://github.com/dotnet/coreclr/blob/eafa8648ebee92de1380278b15cd5c2b6ef11218/src/vm/array.cpp#L1406
static MonoTypeEnum
get_normalized_integral_array_element_type (MonoTypeEnum elementType)
{
	// Array Primitive types such as E_T_I4 and E_T_U4 are interchangeable
	// Enums with interchangeable underlying types are interchangable
	// BOOL is NOT interchangeable with I1/U1, neither CHAR -- with I2/U2

	switch (elementType) {
	case MONO_TYPE_U1:
	case MONO_TYPE_U2:
	case MONO_TYPE_U4:
	case MONO_TYPE_U8:
	case MONO_TYPE_U:
		return (MonoTypeEnum) (elementType - 1); // normalize to signed type
	}

	return elementType;
}

MonoBoolean 
ves_icall_System_Array_CanChangePrimitive (MonoReflectionType *volatile* ref_src_type_handle, MonoReflectionType *volatile* ref_dst_type_handle, MonoBoolean reliable)
{
	MonoReflectionType* const ref_src_type = *ref_src_type_handle;
	MonoReflectionType* const ref_dst_type = *ref_dst_type_handle;

	MonoType *src_type = ref_src_type->type;
	MonoType *dst_type = ref_dst_type->type;

	g_assert (mono_type_is_primitive (src_type));
	g_assert (mono_type_is_primitive (dst_type));

	MonoTypeEnum normalized_src_type = get_normalized_integral_array_element_type (src_type->type);
	MonoTypeEnum normalized_dst_type = get_normalized_integral_array_element_type (dst_type->type);

	// Allow conversions like int <-> uint
	if (normalized_src_type == normalized_dst_type) {
		return TRUE;
	}

	// Widening is not allowed if reliable is true.
	if (reliable) {
		return FALSE;
	}

	// NOTE we don't use normalized types here so int -> ulong will be false
	// see https://github.com/dotnet/coreclr/pull/25209#issuecomment-505952295
	return can_primitive_widen (src_type->type, dst_type->type);
}

static void
array_set_value_impl (MonoArrayHandle arr_handle, MonoObjectHandle value_handle, guint32 pos, gboolean strict_enums, gboolean strict_signs, MonoError *error)
{
	MonoClass *ac, *vc, *ec;
	gint32 esize, vsize;
	gpointer *ea = NULL, *va = NULL;

	guint64 u64 = 0;
	gint64 i64 = 0;
	gdouble r64 = 0;
	gboolean castOk = FALSE;
	gboolean et_isenum = FALSE;
	gboolean vt_isenum = FALSE;

	error_init (error);

	if (!MONO_HANDLE_IS_NULL (value_handle))
		vc = mono_handle_class (value_handle);
	else
		vc = NULL;

	ac = mono_handle_class (arr_handle);
	ec = m_class_get_element_class (ac);
	esize = mono_array_element_size (ac);

	if (mono_class_is_nullable (ec)) {
		if (vc && m_class_is_primitive (vc) && vc != m_class_get_nullable_elem_class (ec)) {
            // T -> Nullable<T>  T must be exact
			set_invalid_cast (error, vc, ec);
			goto leave;
		}

		MONO_ENTER_NO_SAFEPOINTS;
		ea = (gpointer*) mono_array_addr_with_size_internal (MONO_HANDLE_RAW (arr_handle), esize, pos);
		if (!MONO_HANDLE_IS_NULL (value_handle))
			va = (gpointer*) mono_object_unbox_internal (MONO_HANDLE_RAW (value_handle));
		mono_nullable_init_unboxed ((guint8*)ea, va, ec);
		MONO_EXIT_NO_SAFEPOINTS;
		goto leave;
	}

	if (MONO_HANDLE_IS_NULL (value_handle)) {
		MONO_ENTER_NO_SAFEPOINTS;
		ea = (gpointer*) mono_array_addr_with_size_internal (MONO_HANDLE_RAW (arr_handle), esize, pos);
		mono_gc_bzero_atomic (ea, esize);
		MONO_EXIT_NO_SAFEPOINTS;
		goto leave;
	}

#define WIDENING_MSG NULL
#define WIDENING_ARG NULL

#define NO_WIDENING_CONVERSION G_STMT_START{				\
		mono_error_set_argument (error, WIDENING_ARG, WIDENING_MSG); \
		break;							\
	}G_STMT_END

#define CHECK_WIDENING_CONVERSION(extra) G_STMT_START{			\
		if (esize < vsize + (extra)) {				\
			mono_error_set_argument (error, WIDENING_ARG, WIDENING_MSG); \
			break;						\
		}							\
	}G_STMT_END

#define INVALID_CAST G_STMT_START{					\
		mono_get_runtime_callbacks ()->set_cast_details (vc, ec); \
		mono_error_set_invalid_cast (error);			\
		break;							\
	}G_STMT_END

	MonoTypeEnum et;
	et = m_class_get_byval_arg (ec)->type;
	MonoTypeEnum vt;
	vt = m_class_get_byval_arg (vc)->type;

	/* Check element (destination) type. */
	switch (et) {
	case MONO_TYPE_STRING:
		switch (vt) {
		case MONO_TYPE_STRING:
			break;
		default:
			INVALID_CAST;
		}
		break;
	case MONO_TYPE_BOOLEAN:
		switch (vt) {
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
			break;
		default:
			INVALID_CAST;
		}
		break;
	default:
		break;
	}
	if (!is_ok (error))
		goto leave;

	castOk = mono_object_handle_isinst_mbyref_raw (value_handle, ec, error);
	if (!is_ok (error))
		goto leave;

	if (!m_class_is_valuetype (ec)) {
		if (!castOk)
			INVALID_CAST;
		if (is_ok (error))
			MONO_HANDLE_ARRAY_SETREF (arr_handle, pos, value_handle);
		goto leave;
	}

	if (castOk) {
		MONO_ENTER_NO_SAFEPOINTS;
		ea = (gpointer*) mono_array_addr_with_size_internal (MONO_HANDLE_RAW (arr_handle), esize, pos);
		va = (gpointer*) mono_object_unbox_internal (MONO_HANDLE_RAW (value_handle));
		if (m_class_has_references (ec))
			mono_value_copy_internal (ea, va, ec);
		else
			mono_gc_memmove_atomic (ea, va, esize);
		MONO_EXIT_NO_SAFEPOINTS;

		goto leave;
	}

	if (!m_class_is_valuetype (vc))
		INVALID_CAST;

	if (!is_ok (error))
		goto leave;

	vsize = mono_class_value_size (vc, NULL);

	et_isenum = et == MONO_TYPE_VALUETYPE && m_class_is_enumtype (m_class_get_byval_arg (ec)->data.klass);
	vt_isenum = vt == MONO_TYPE_VALUETYPE && m_class_is_enumtype (m_class_get_byval_arg (vc)->data.klass);

	if (strict_enums && et_isenum && !vt_isenum) {
		INVALID_CAST;
		goto leave;
	}

	if (et_isenum)
		et = mono_class_enum_basetype_internal (m_class_get_byval_arg (ec)->data.klass)->type;

	if (vt_isenum)
		vt = mono_class_enum_basetype_internal (m_class_get_byval_arg (vc)->data.klass)->type;

	// Treat MONO_TYPE_U/I as MONO_TYPE_U8/I8/U4/I4
#if SIZEOF_VOID_P == 8
	vt = vt == MONO_TYPE_U ? MONO_TYPE_U8 : (vt == MONO_TYPE_I ? MONO_TYPE_I8 : vt);
	et = et == MONO_TYPE_U ? MONO_TYPE_U8 : (et == MONO_TYPE_I ? MONO_TYPE_I8 : et);
#else
	vt = vt == MONO_TYPE_U ? MONO_TYPE_U4 : (vt == MONO_TYPE_I ? MONO_TYPE_I4 : vt);
	et = et == MONO_TYPE_U ? MONO_TYPE_U4 : (et == MONO_TYPE_I ? MONO_TYPE_I4 : et);
#endif

#define ASSIGN_UNSIGNED(etype) G_STMT_START{\
	switch (vt) { \
	case MONO_TYPE_U1: \
	case MONO_TYPE_U2: \
	case MONO_TYPE_U4: \
	case MONO_TYPE_U8: \
	case MONO_TYPE_CHAR: \
		CHECK_WIDENING_CONVERSION(0); \
		*(etype *) ea = (etype) u64; \
		break; \
	/* You can't assign a signed value to an unsigned array. */ \
	case MONO_TYPE_I1: \
	case MONO_TYPE_I2: \
	case MONO_TYPE_I4: \
	case MONO_TYPE_I8: \
		if (!strict_signs) { \
			CHECK_WIDENING_CONVERSION(0); \
			*(etype *) ea = (etype) i64; \
			break; \
		} \
	/* You can't assign a floating point number to an integer array. */ \
	case MONO_TYPE_R4: \
	case MONO_TYPE_R8: \
		NO_WIDENING_CONVERSION; \
		break; \
	default: \
		INVALID_CAST; \
		break; \
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
		break; \
	/* You can assign an unsigned value to a signed array if the array's */ \
	/* element size is larger than the value size. */ \
	case MONO_TYPE_U1: \
	case MONO_TYPE_U2: \
	case MONO_TYPE_U4: \
	case MONO_TYPE_U8: \
	case MONO_TYPE_CHAR: \
		CHECK_WIDENING_CONVERSION(strict_signs ? 1 : 0); \
		*(etype *) ea = (etype) u64; \
		break; \
	/* You can't assign a floating point number to an integer array. */ \
	case MONO_TYPE_R4: \
	case MONO_TYPE_R8: \
		NO_WIDENING_CONVERSION; \
		break; \
	default: \
		INVALID_CAST; \
		break; \
	} \
}G_STMT_END

#define ASSIGN_REAL(etype) G_STMT_START{\
	switch (vt) { \
	case MONO_TYPE_R4: \
	case MONO_TYPE_R8: \
		CHECK_WIDENING_CONVERSION(0); \
		*(etype *) ea = (etype) r64; \
		break; \
	/* All integer values fit into a floating point array, so we don't */ \
	/* need to CHECK_WIDENING_CONVERSION here. */ \
	case MONO_TYPE_I1: \
	case MONO_TYPE_I2: \
	case MONO_TYPE_I4: \
	case MONO_TYPE_I8: \
		*(etype *) ea = (etype) i64; \
		break; \
	case MONO_TYPE_U1: \
	case MONO_TYPE_U2: \
	case MONO_TYPE_U4: \
	case MONO_TYPE_U8: \
	case MONO_TYPE_CHAR: \
		*(etype *) ea = (etype) u64; \
		break; \
	default: \
		INVALID_CAST; \
		break; \
	} \
}G_STMT_END

	MONO_ENTER_NO_SAFEPOINTS;
	g_assert (!MONO_HANDLE_IS_NULL (value_handle));
	g_assert (m_class_is_valuetype (vc));
	va = (gpointer*) mono_object_unbox_internal (MONO_HANDLE_RAW (value_handle));
	ea = (gpointer*) mono_array_addr_with_size_internal (MONO_HANDLE_RAW (arr_handle), esize, pos);

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
			break;
		default:
			INVALID_CAST;
		}
		break;
	default:
		break;
	}
	/* If we can't do a direct copy, let's try a widening conversion. */

	if (is_ok (error)) {
		switch (et) {
		case MONO_TYPE_CHAR:
			ASSIGN_UNSIGNED (guint16);
			break;
		case MONO_TYPE_U1:
			ASSIGN_UNSIGNED (guint8);
			break;
		case MONO_TYPE_U2:
			ASSIGN_UNSIGNED (guint16);
			break;
		case MONO_TYPE_U4:
			ASSIGN_UNSIGNED (guint32);
			break;
		case MONO_TYPE_U8:
			ASSIGN_UNSIGNED (guint64);
			break;
		case MONO_TYPE_I1:
			ASSIGN_SIGNED (gint8);
			break;
		case MONO_TYPE_I2:
			ASSIGN_SIGNED (gint16);
			break;
		case MONO_TYPE_I4:
			ASSIGN_SIGNED (gint32);
			break;
		case MONO_TYPE_I8:
			ASSIGN_SIGNED (gint64);
			break;
		case MONO_TYPE_R4:
			ASSIGN_REAL (gfloat);
			break;
		case MONO_TYPE_R8:
			ASSIGN_REAL (gdouble);
			break;
		default:
			INVALID_CAST;
		}
	}

	MONO_EXIT_NO_SAFEPOINTS;

#undef INVALID_CAST
#undef NO_WIDENING_CONVERSION
#undef CHECK_WIDENING_CONVERSION
#undef ASSIGN_UNSIGNED
#undef ASSIGN_SIGNED
#undef ASSIGN_REAL

leave:
	return;
}

void
ves_icall_System_Array_InternalCreate (MonoArray *volatile* result, MonoType* type, gint32 rank, gint32* pLengths, gint32* pLowerBounds)
{
	ERROR_DECL (error);

	MonoClass* klass = mono_class_from_mono_type_internal (type);
	if (!mono_class_init_checked (klass, error))
		goto exit;

	if (m_class_get_byval_arg (m_class_get_element_class (klass))->type == MONO_TYPE_VOID) {
		mono_error_set_not_supported (error, "Arrays of System.Void are not supported.");
		goto exit;
	}

	if (type->byref || m_class_is_byreflike (klass)) {
		mono_error_set_not_supported (error, NULL);
		goto exit;
	}

	MonoGenericClass *gklass;
	gklass = mono_class_try_get_generic_class (klass);
	if (is_generic_parameter (type) || mono_class_is_gtd (klass) || (gklass && gklass->context.class_inst->is_open)) {
		mono_error_set_not_supported (error, NULL);
		goto exit;
	}

	/* vectors are not the same as one dimensional arrays with non-zero bounds */
	gboolean bounded;
	bounded = pLowerBounds != NULL && rank == 1 && pLowerBounds [0] != 0;

	MonoClass* aklass;
	aklass = mono_class_create_bounded_array (klass, rank, bounded);

	uintptr_t aklass_rank;
	aklass_rank = m_class_get_rank (aklass);

	uintptr_t* sizes;
	sizes = g_newa (uintptr_t, aklass_rank * 2);

	intptr_t* lower_bounds;
	lower_bounds = (intptr_t*)(sizes + aklass_rank);

	// Copy lengths and lower_bounds from gint32 to [u]intptr_t.
	for (uintptr_t i = 0; i < aklass_rank; ++i) {
		if (pLowerBounds != NULL) {
			lower_bounds [i] = pLowerBounds [i];
			if ((gint64) pLowerBounds [i] + (gint64) pLengths [i] > G_MAXINT32) {
				mono_error_set_argument_out_of_range (error, NULL, "Length + bound must not exceed Int32.MaxValue.");
				goto exit;
			}
		} else {
			lower_bounds [i] = 0;
		}
		sizes [i] = pLengths [i];
	}

	*result = mono_array_new_full_checked (aklass, sizes, lower_bounds, error);

exit:
	mono_error_set_pending_exception (error);
}

gint32
ves_icall_System_Array_GetCorElementTypeOfElementType (MonoArrayHandle arr, MonoError *error)
{
	MonoType *type = mono_type_get_underlying_type (m_class_get_byval_arg (m_class_get_element_class (mono_handle_class (arr))));
	return type->type;
}

gint32
ves_icall_System_Array_IsValueOfElementType (MonoArrayHandle arr, MonoObjectHandle obj, MonoError *error)
{
	return m_class_get_element_class (mono_handle_class (arr)) == mono_handle_class (obj);
}

static mono_array_size_t
mono_array_get_length (MonoArrayHandle arr, gint32 dimension, MonoError *error)
{
	if (dimension < 0 || dimension >= m_class_get_rank (mono_handle_class (arr))) {
		mono_error_set_index_out_of_range (error);
		return 0;
	}

	return MONO_HANDLE_GETVAL (arr, bounds) ? MONO_HANDLE_GETVAL (arr, bounds [dimension].length)
						: MONO_HANDLE_GETVAL (arr, max_length);
}

gint32
ves_icall_System_Array_GetLength (MonoArrayHandle arr, gint32 dimension, MonoError *error)
{
	icallarray_print ("%s arr:%p dimension:%d\n", __func__, MONO_HANDLE_RAW (arr), (int)dimension);

	mono_array_size_t const length = mono_array_get_length (arr, dimension, error);
	if (length > G_MAXINT32) {
		mono_error_set_overflow (error);
		return 0;
	}
	return (gint32)length;
}

gint32
ves_icall_System_Array_GetLowerBound (MonoArrayHandle arr, gint32 dimension, MonoError *error)
{
	icallarray_print ("%s arr:%p dimension:%d\n", __func__, MONO_HANDLE_RAW (arr), (int)dimension);

	if (dimension < 0 || dimension >= m_class_get_rank (mono_handle_class (arr))) {
		mono_error_set_index_out_of_range (error);
		return 0;
	}

	return MONO_HANDLE_GETVAL (arr, bounds) ? MONO_HANDLE_GETVAL (arr, bounds [dimension].lower_bound)
						: 0;
}

MonoBoolean
ves_icall_System_Array_FastCopy (MonoArrayHandle source, int source_idx, MonoArrayHandle dest, int dest_idx, int length, MonoError *error)
{
	MonoVTable * const src_vtable = MONO_HANDLE_GETVAL (source, obj.vtable);
	MonoVTable * const dest_vtable = MONO_HANDLE_GETVAL (dest, obj.vtable);

	if (src_vtable->rank != dest_vtable->rank)
		return FALSE;

	MonoArrayBounds *source_bounds = MONO_HANDLE_GETVAL (source, bounds);
	MonoArrayBounds *dest_bounds = MONO_HANDLE_GETVAL (dest, bounds);

	for (int i = 0; i < src_vtable->rank; i++) {
		if ((source_bounds && source_bounds [i].lower_bound > 0) ||
			(dest_bounds && dest_bounds [i].lower_bound > 0))
			return FALSE;
	}

	/* there's no integer overflow since mono_array_length_internal returns an unsigned integer */
	if ((dest_idx + length > mono_array_handle_length (dest)) ||
		(source_idx + length > mono_array_handle_length (source)))
		return FALSE;

	MonoClass * const src_class = m_class_get_element_class (src_vtable->klass);
	MonoClass * const dest_class = m_class_get_element_class (dest_vtable->klass);

	/*
	 * Handle common cases.
	 */

	/* Case1: object[] -> valuetype[] (ArrayList::ToArray) 
	We fallback to managed here since we need to typecheck each boxed valuetype before storing them in the dest array.
	*/
	if (src_class == mono_defaults.object_class && m_class_is_valuetype (dest_class))
		return FALSE;

	/* Check if we're copying a char[] <==> (u)short[] */
	if (src_class != dest_class) {
		if (m_class_is_valuetype (dest_class) || m_class_is_enumtype (dest_class) ||
		 	m_class_is_valuetype (src_class) || m_class_is_valuetype (src_class))
			return FALSE;

		/* It's only safe to copy between arrays if we can ensure the source will always have a subtype of the destination. We bail otherwise. */
		if (!mono_class_is_subclass_of_internal (src_class, dest_class, FALSE))
			return FALSE;

		if (m_class_is_native_pointer (src_class) || m_class_is_native_pointer (dest_class))
			return FALSE;
	}

	if (m_class_is_valuetype (dest_class)) {
		gsize const element_size = mono_array_element_size (MONO_HANDLE_GETVAL (source, obj.vtable->klass));

		MONO_ENTER_NO_SAFEPOINTS; // gchandle would also work here, is slow, breaks profiler tests.

		gconstpointer const source_addr =
			mono_array_addr_with_size_fast (MONO_HANDLE_RAW (source), element_size, source_idx);
		if (m_class_has_references (dest_class)) {
			mono_value_copy_array_handle (dest, dest_idx, source_addr, length);
		} else {
			gpointer const dest_addr =
				mono_array_addr_with_size_fast (MONO_HANDLE_RAW (dest), element_size, dest_idx);
			mono_gc_memmove_atomic (dest_addr, source_addr, element_size * length);
		}

		MONO_EXIT_NO_SAFEPOINTS;
	} else {
		mono_array_handle_memcpy_refs (dest, dest_idx, source, source_idx, length);
	}

	return TRUE;
}

void
ves_icall_System_Array_GetGenericValue_icall (MonoArray **arr, guint32 pos, gpointer value)
{
	icallarray_print ("%s arr:%p pos:%u value:%p\n", __func__, *arr, pos, value);

	MONO_REQ_GC_UNSAFE_MODE;	// because of gpointer value

	MonoClass * const ac = mono_object_class (*arr);
	gsize const esize = mono_array_element_size (ac);
	gconstpointer * const ea = (gconstpointer*)((char*)(*arr)->vector + (pos * esize));

	mono_gc_memmove_atomic (value, ea, esize);
}

void
ves_icall_System_Array_SetGenericValue_icall (MonoArray **arr, guint32 pos, gpointer value)
{
	icallarray_print ("%s arr:%p pos:%u value:%p\n", __func__, *arr, pos, value);

	MONO_REQ_GC_UNSAFE_MODE;	// because of gpointer value

	MonoClass * const ac = mono_object_class (*arr);
	MonoClass * const ec = m_class_get_element_class (ac);

	gsize const esize = mono_array_element_size (ac);
	gpointer * const ea = (gpointer*)((char*)(*arr)->vector + (pos * esize));

	if (MONO_TYPE_IS_REFERENCE (m_class_get_byval_arg (ec))) {
		g_assert (esize == sizeof (gpointer));
		mono_gc_wbarrier_generic_store_internal (ea, *(MonoObject **)value);
	} else {
		g_assert (m_class_is_inited (ec));
		g_assert (esize == mono_class_value_size (ec, NULL));
		if (m_class_has_references (ec))
			mono_gc_wbarrier_value_copy_internal (ea, value, 1, ec);
		else
			mono_gc_memmove_atomic (ea, value, esize);
	}
}

void
ves_icall_System_Runtime_RuntimeImports_Memmove (guint8 *destination, guint8 *source, size_t byte_count)
{
	mono_gc_memmove_atomic (destination, source, byte_count);
}

void
ves_icall_System_Buffer_BulkMoveWithWriteBarrier (guint8 *destination, guint8 *source, size_t len, MonoType *type)
{
	if (MONO_TYPE_IS_REFERENCE (type))
		mono_gc_wbarrier_arrayref_copy_internal (destination, source, (guint)len);
	else
		mono_gc_wbarrier_value_copy_internal (destination, source, (guint)len, mono_class_from_mono_type_internal (type));
}

void
ves_icall_System_Runtime_RuntimeImports_ZeroMemory (guint8 *p, size_t byte_length)
{
	memset (p, 0, byte_length);
}

void
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray (MonoArrayHandle array, MonoClassField *field_handle, MonoError *error)
{
	MonoClass *klass = mono_handle_class (array);
	guint32 size = mono_array_element_size (klass);
	MonoType *type = mono_type_get_underlying_type (m_class_get_byval_arg (m_class_get_element_class (klass)));
	int align;
	const char *field_data;

	if (MONO_TYPE_IS_REFERENCE (type) || type->type == MONO_TYPE_VALUETYPE) {
		mono_error_set_argument (error, "array", "Cannot initialize array of non-primitive type");
		return;
	}

	MonoType *field_type = mono_field_get_type_checked (field_handle, error);
	if (!field_type)
		return;

	if (!(field_type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_RVA)) {
		mono_error_set_argument_format (error, "field_handle", "Field '%s' doesn't have an RVA", mono_field_get_name (field_handle));
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
	guint ## n *data = (guint ## n *) mono_array_addr_internal (MONO_HANDLE_RAW(array), char, 0); \
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
		memcpy (mono_array_addr_internal (MONO_HANDLE_RAW(array), char, 0), field_data, size);
		break;
	}
#else
	memcpy (mono_array_addr_internal (MONO_HANDLE_RAW(array), char, 0), field_data, size);
#endif
}

MonoObjectHandle
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetObjectValue (MonoObjectHandle obj, MonoError *error)
{
	if (MONO_HANDLE_IS_NULL (obj) || !m_class_is_valuetype (mono_handle_class (obj)))
		return obj;

	return mono_object_clone_handle (obj, error);
}

void
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunClassConstructor (MonoType *handle, MonoError *error)
{
	MonoClass *klass;
	MonoVTable *vtable;

	MONO_CHECK_ARG_NULL (handle,);

	klass = mono_class_from_mono_type_internal (handle);
	MONO_CHECK_ARG (handle, klass,);

	if (mono_class_is_gtd (klass))
		return;

	vtable = mono_class_vtable_checked (klass, error);
	return_if_nok (error);

	/* This will call the type constructor */
	mono_runtime_class_init_full (vtable, error);
}

void
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunModuleConstructor (MonoImage *image, MonoError *error)
{
	mono_image_check_for_module_cctor (image);
	if (!image->has_module_cctor)
		return;

	MonoClass *module_klass = mono_class_get_checked (image, MONO_TOKEN_TYPE_DEF | 1, error);
	return_if_nok (error);

	MonoVTable * vtable = mono_class_vtable_checked (module_klass, error);
	return_if_nok (error);

	mono_runtime_class_init_full (vtable, error);
}

MonoBoolean
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_SufficientExecutionStack (void)
{
	MonoThreadInfo *thread = mono_thread_info_current ();
	void *current = &thread;

	// Stack upper/lower bound should have been calculated and set as part of register_thread.
	// If not, we are optimistic and assume there is enough room.
	if (!thread->stack_start_limit || !thread->stack_end)
		return TRUE;

	// Stack start limit is stack lower bound. Make sure there is enough room left.
	void *limit = ((uint8_t *)thread->stack_start_limit) + ALIGN_TO (MONO_STACK_OVERFLOW_GUARD_SIZE + MONO_MIN_EXECUTION_STACK_SIZE, ((gssize)mono_pagesize ()));

	if (current < limit)
		return FALSE;

	return TRUE;
}

MonoObjectHandle
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetUninitializedObjectInternal (MonoType *handle, MonoError *error)
{
	MonoClass *klass;
	MonoVTable *vtable;

	g_assert (handle);

	klass = mono_class_from_mono_type_internal (handle);
	if (m_class_is_string (klass)) {
		mono_error_set_argument (error, NULL, NULL);
		return NULL_HANDLE;
	}

	if (mono_class_is_array (klass) || mono_class_is_pointer (klass) || handle->byref) {
		mono_error_set_argument (error, NULL, NULL);
		return NULL_HANDLE;
	}

	if (MONO_TYPE_IS_VOID (handle)) {
		mono_error_set_argument (error, NULL, NULL);
		return NULL_HANDLE;
	}

	if (m_class_is_abstract (klass) || m_class_is_interface (klass) || m_class_is_gtd (klass)) {
		mono_error_set_member_access (error, NULL, NULL);
		return NULL_HANDLE;
	}

	if (m_class_is_byreflike (klass)) {
		mono_error_set_not_supported (error, NULL, NULL);
		return NULL_HANDLE;
	}

	if (!mono_class_is_before_field_init (klass)) {
		vtable = mono_class_vtable_checked (klass, error);
		return_val_if_nok (error, NULL_HANDLE);

		mono_runtime_class_init_full (vtable, error);
		return_val_if_nok (error, NULL_HANDLE);
	}

	if (m_class_is_nullable (klass))
		return mono_object_new_handle (m_class_get_nullable_elem_class (klass), error);
	else
		return mono_object_new_handle (klass, error);
}

void
ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_PrepareMethod (MonoMethod *method, gpointer inst_types, int n_inst_types, MonoError *error)
{
	if (method->flags & METHOD_ATTRIBUTE_ABSTRACT) {
		mono_error_set_argument (error, NULL, NULL);
		return;
	}

	MonoGenericContainer *container = NULL;
	if (method->is_generic)
		container = mono_method_get_generic_container (method);
	else if (m_class_is_gtd (method->klass))
		container = mono_class_get_generic_container (method->klass);
	if (container) {
		int nparams = container->type_argc + (container->parent ? container->parent->type_argc : 0);
		if (nparams != n_inst_types) {
			mono_error_set_argument (error, NULL, NULL);
			return;
		}
	}

	// FIXME: Implement
}

MonoObjectHandle
ves_icall_System_Object_MemberwiseClone (MonoObjectHandle this_obj, MonoError *error)
{
	return mono_object_clone_handle (this_obj, error);
}

gint32
ves_icall_System_ValueType_InternalGetHashCode (MonoObjectHandle this_obj, MonoArrayHandleOut fields, MonoError *error)
{
	MonoClass *klass;
	MonoClassField **unhandled = NULL;
	int count = 0;
	gint32 result = (int)(gsize)mono_defaults.int32_class;
	MonoClassField* field;
	gpointer iter;

	klass = mono_handle_class (this_obj);

	if (mono_class_num_fields (klass) == 0)
		return result;

	/*
	 * Compute the starting value of the hashcode for fields of primitive
	 * types, and return the remaining fields in an array to the managed side.
	 * This way, we can avoid costly reflection operations in managed code.
	 */
	iter = NULL;
	while ((field = mono_class_get_fields_internal (klass, &iter))) {
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		if (mono_field_is_deleted (field))
			continue;
		gpointer addr = (guint8*)MONO_HANDLE_RAW (this_obj)  + field->offset;
		/* FIXME: Add more types */
		switch (field->type->type) {
		case MONO_TYPE_I4:
			result ^= *(gint32*)addr;
			break;
		case MONO_TYPE_PTR:
			result ^= mono_aligned_addr_hash (*(gpointer*)addr);
			break;
		case MONO_TYPE_STRING: {
			MonoString *s;
			s = *(MonoString**)addr;
			if (s != NULL)
				result ^= mono_string_hash_internal (s);
			break;
		}
		default:
			if (!unhandled)
				unhandled = g_newa (MonoClassField*, mono_class_num_fields (klass));
			unhandled [count ++] = field;
		}
	}

	if (unhandled) {
		MonoArrayHandle fields_arr = mono_array_new_handle (mono_defaults.object_class, count, error);
		return_val_if_nok (error, 0);
		MONO_HANDLE_ASSIGN (fields, fields_arr);
		MonoObjectHandle h = MONO_HANDLE_NEW (MonoObject, NULL);
		for (int i = 0; i < count; ++i) {
			MonoObject *o = mono_field_get_value_object_checked (unhandled [i], MONO_HANDLE_RAW (this_obj), error);
			return_val_if_nok (error, 0);
			MONO_HANDLE_ASSIGN_RAW (h, o);
			mono_array_handle_setref (fields_arr, i, h);
		}
	} else {
		MONO_HANDLE_ASSIGN (fields, NULL_HANDLE);
	}
	return result;
}

MonoBoolean
ves_icall_System_ValueType_Equals (MonoObjectHandle this_obj, MonoObjectHandle that, MonoArrayHandleOut fields, MonoError *error)
{
	MonoClass *klass;
	MonoClassField **unhandled = NULL;
	MonoClassField* field;
	gpointer iter;
	int count = 0;

	MONO_CHECK_ARG_NULL_HANDLE (that, FALSE);

	MONO_HANDLE_ASSIGN (fields, NULL_HANDLE);

	if (mono_handle_vtable (this_obj) != mono_handle_vtable (that))
		return FALSE;

	klass = mono_handle_class (this_obj);

	if (m_class_is_enumtype (klass) && mono_class_enum_basetype_internal (klass) && mono_class_enum_basetype_internal (klass)->type == MONO_TYPE_I4)
		return *(gint32*)mono_handle_get_data_unsafe (this_obj) == *(gint32*)mono_handle_get_data_unsafe (that);

	/*
	 * Do the comparison for fields of primitive type and return a result if
	 * possible. Otherwise, return the remaining fields in an array to the 
	 * managed side. This way, we can avoid costly reflection operations in 
	 * managed code.
	 */
	iter = NULL;
	while ((field = mono_class_get_fields_internal (klass, &iter))) {
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		if (mono_field_is_deleted (field))
			continue;
		guint8 *this_field = (guint8 *)MONO_HANDLE_RAW (this_obj) + field->offset;
		guint8 *that_field = (guint8 *)MONO_HANDLE_RAW (that) + field->offset;

#define UNALIGNED_COMPARE(type) \
			do { \
				type left, right; \
				memcpy (&left, this_field, sizeof (type)); \
				memcpy (&right, that_field, sizeof (type)); \
				if (left != right) \
					return FALSE; \
			} while (0)

		/* FIXME: Add more types */
		switch (field->type->type) {
		case MONO_TYPE_U1:
		case MONO_TYPE_I1:
		case MONO_TYPE_BOOLEAN:
			if (*this_field != *that_field)
				return FALSE;
			break;
		case MONO_TYPE_U2:
		case MONO_TYPE_I2:
		case MONO_TYPE_CHAR:
#ifdef NO_UNALIGNED_ACCESS
			if (G_UNLIKELY ((intptr_t) this_field & 1 || (intptr_t) that_field & 1))
				UNALIGNED_COMPARE (gint16);
			else
#endif
			if (*(gint16 *) this_field != *(gint16 *) that_field)
				return FALSE;
			break;
		case MONO_TYPE_U4:
		case MONO_TYPE_I4:
#ifdef NO_UNALIGNED_ACCESS
			if (G_UNLIKELY ((intptr_t) this_field & 3 || (intptr_t) that_field & 3))
				UNALIGNED_COMPARE (gint32);
			else
#endif
			if (*(gint32 *) this_field != *(gint32 *) that_field)
				return FALSE;
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
#ifdef NO_UNALIGNED_ACCESS
			if (G_UNLIKELY ((intptr_t) this_field & 7 || (intptr_t) that_field & 7))
				UNALIGNED_COMPARE (gint64);
			else
#endif
			if (*(gint64 *) this_field != *(gint64 *) that_field)
				return FALSE;
			break;

		case MONO_TYPE_R4: {
			float d1, d2;
#ifdef NO_UNALIGNED_ACCESS
			memcpy (&d1, this_field, sizeof (float));
			memcpy (&d2, that_field, sizeof (float));
#else
			d1 = *(float *) this_field;
			d2 = *(float *) that_field;
#endif
			if (d1 != d2 && !(mono_isnan (d1) && mono_isnan (d2)))
				return FALSE;
			break;
		}
		case MONO_TYPE_R8: {
			double d1, d2;
#ifdef NO_UNALIGNED_ACCESS
			memcpy (&d1, this_field, sizeof (double));
			memcpy (&d2, that_field, sizeof (double));
#else
			d1 = *(double *) this_field;
			d2 = *(double *) that_field;
#endif
			if (d1 != d2 && !(mono_isnan (d1) && mono_isnan (d2)))
				return FALSE;
			break;
		}
		case MONO_TYPE_PTR:
#ifdef NO_UNALIGNED_ACCESS
			if (G_UNLIKELY ((intptr_t) this_field & 7 || (intptr_t) that_field & 7))
				UNALIGNED_COMPARE (gpointer);
			else
#endif
			if (*(gpointer *) this_field != *(gpointer *) that_field)
				return FALSE;
			break;
		case MONO_TYPE_STRING: {
			MonoString *s1, *s2;
			guint32 s1len, s2len;
			s1 = *(MonoString**)this_field;
			s2 = *(MonoString**)that_field;
			if (s1 == s2)
				break;
			if ((s1 == NULL) || (s2 == NULL))
				return FALSE;
			s1len = mono_string_length_internal (s1);
			s2len = mono_string_length_internal (s2);
			if (s1len != s2len)
				return FALSE;

			if (memcmp (mono_string_chars_internal (s1), mono_string_chars_internal (s2), s1len * sizeof (gunichar2)) != 0)
				return FALSE;
			break;
		}
		default:
			if (!unhandled)
				unhandled = g_newa (MonoClassField*, mono_class_num_fields (klass));
			unhandled [count ++] = field;
		}

#undef UNALIGNED_COMPARE

		if (m_class_is_enumtype (klass))
			/* enums only have one non-static field */
			break;
	}

	if (unhandled) {
		MonoArrayHandle fields_arr = mono_array_new_handle (mono_defaults.object_class, count * 2, error);
		return_val_if_nok (error, 0);
		MONO_HANDLE_ASSIGN (fields, fields_arr);
		MonoObjectHandle h = MONO_HANDLE_NEW (MonoObject, NULL);
		for (int i = 0; i < count; ++i) {
			MonoObject *o = mono_field_get_value_object_checked (unhandled [i], MONO_HANDLE_RAW (this_obj), error);
			return_val_if_nok (error, FALSE);
			MONO_HANDLE_ASSIGN_RAW (h, o);
			mono_array_handle_setref (fields_arr, i * 2, h);

			o = mono_field_get_value_object_checked (unhandled [i], MONO_HANDLE_RAW (that), error);
			return_val_if_nok (error, FALSE);
			MONO_HANDLE_ASSIGN_RAW (h, o);
			mono_array_handle_setref (fields_arr, (i * 2) + 1, h);
		}
		return FALSE;
	} else {
		return TRUE;
	}
}

static gboolean
get_executing (MonoMethod *m, gint32 no, gint32 ilo, gboolean managed, gpointer data)
{
	MonoMethod **dest = (MonoMethod **)data;

	/* skip unmanaged frames */
	if (!managed)
		return FALSE;

	if (!(*dest)) {
		if (!strcmp (m_class_get_name_space (m->klass), "System.Reflection"))
			return FALSE;
		*dest = m;
		return TRUE;
	}
	return FALSE;
}

static gboolean
in_corlib_name_space (MonoClass *klass, const char *name_space)
{
	return m_class_get_image (klass) == mono_defaults.corlib &&
		!strcmp (m_class_get_name_space (klass), name_space);
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

	if (in_corlib_name_space (m->klass, "System.Reflection"))
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

	if (in_corlib_name_space (m->klass, "System.Reflection") || in_corlib_name_space (m->klass, "System"))
		return FALSE;

	if (!(*dest)) {
		*dest = m;
		return TRUE;
	}
	return FALSE;
}

/**
 * mono_runtime_get_caller_no_system_or_reflection:
 *
 * Walk the stack of the current thread and find the first managed method that
 * is not in the mscorlib System or System.Reflection namespace.  This skips
 * unmanaged callers and wrapper methods.
 *
 * \returns a pointer to the \c MonoMethod or NULL if we walked past all the
 * callers.
 */
MonoMethod*
mono_runtime_get_caller_no_system_or_reflection (void)
{
	MonoMethod *dest = NULL;
	mono_stack_walk_no_il (get_caller_no_system_or_reflection, &dest);
	return dest;
}

/*
 * mono_runtime_get_caller_from_stack_mark:
 *
 *   Walk the stack and return the assembly of the method referenced
 * by the stack mark STACK_MARK.
 */
MonoAssembly*
mono_runtime_get_caller_from_stack_mark (MonoStackCrawlMark *stack_mark)
{
	// FIXME: Use the stack mark
	MonoMethod *dest = NULL;
	mono_stack_walk_no_il (get_caller_no_system_or_reflection, &dest);
	if (dest)
		return m_class_get_image (dest->klass)->assembly;
	else
		return NULL;
}

static MonoReflectionTypeHandle
type_from_parsed_name (MonoTypeNameParse *info, MonoStackCrawlMark *stack_mark, MonoBoolean ignoreCase, MonoAssembly **caller_assembly, MonoError *error)
{
	MonoMethod *m;
	MonoType *type = NULL;
	MonoAssembly *assembly = NULL;
	gboolean type_resolve = FALSE;
	MonoImage *rootimage = NULL;
	MonoAssemblyLoadContext *alc = mono_alc_get_ambient ();

	error_init (error);

	/*
	 * We must compute the calling assembly as type loading must happen under a metadata context.
	 * For example. The main assembly is a.exe and Type.GetType is called from dir/b.dll. Without
	 * the metadata context (basedir currently) set to dir/b.dll we won't be able to load a dir/c.dll.
	 */
	m = mono_method_get_last_managed ();
	if (m && m_class_get_image (m->klass) != mono_defaults.corlib) {
		/* Happens with inlining */
		assembly = m_class_get_image (m->klass)->assembly;
	} else {
		assembly = mono_runtime_get_caller_from_stack_mark (stack_mark);
	}

	if (assembly) {
		type_resolve = TRUE;
		rootimage = assembly->image;
	} else {
		// FIXME: once wasm can use stack marks, consider turning all this into an assert
		g_warning (G_STRLOC);
	}

	*caller_assembly = assembly;

	if (info->assembly.name) {
		MonoAssemblyByNameRequest req;
		mono_assembly_request_prepare_byname (&req, MONO_ASMCTX_DEFAULT, alc);
		req.requesting_assembly = assembly;
		req.basedir = assembly ? assembly->basedir : NULL;
		assembly = mono_assembly_request_byname (&info->assembly, &req, NULL);
	}

	if (assembly) {
		/* When loading from the current assembly, AppDomain.TypeResolve will not be called yet */
		type = mono_reflection_get_type_checked (alc, rootimage, assembly->image, info, ignoreCase, TRUE, &type_resolve, error);
		goto_if_nok (error, fail);
	}

	// XXXX - aleksey -
	//  Say we're looking for System.Generic.Dict<int, Local>
	//  we FAIL the get type above, because S.G.Dict isn't in assembly->image.  So we drop down here.
	//  but then we FAIL AGAIN because now we pass null as the image and the rootimage and everything
	//  is messed up when we go to construct the Local as the type arg...
	//
	// By contrast, if we started with Mine<System.Generic.Dict<int, Local>> we'd go in with assembly->image
	// as the root and then even the detour into generics would still not cause issues when we went to load Local.
	if (!info->assembly.name && !type) {
		/* try mscorlib */
		type = mono_reflection_get_type_checked (alc, rootimage, NULL, info, ignoreCase, TRUE, &type_resolve, error);
		goto_if_nok (error, fail);
	}
	if (assembly && !type && type_resolve) {
		type_resolve = FALSE; /* This will invoke TypeResolve if not done in the first 'if' */
		type = mono_reflection_get_type_checked (alc, rootimage, assembly->image, info, ignoreCase, TRUE, &type_resolve, error);
		goto_if_nok (error, fail);
	}

	if (!type) 
		goto fail;

	return mono_type_get_object_handle (type, error);
fail:
	return MONO_HANDLE_NEW (MonoReflectionType, NULL);
}

MonoReflectionTypeHandle
ves_icall_System_RuntimeTypeHandle_internal_from_name (MonoStringHandle name,
					  MonoStackCrawlMark *stack_mark,
					  MonoReflectionAssemblyHandle callerAssembly,
					  MonoBoolean throwOnError,
					  MonoBoolean ignoreCase,
					  MonoError *error)
{
	MonoTypeNameParse info;
	gboolean free_info = FALSE;
	MonoAssembly *caller_assembly;
	MonoReflectionTypeHandle type = MONO_HANDLE_NEW (MonoReflectionType, NULL);

	/* The callerAssembly argument is unused for now */

	char *str = mono_string_handle_to_utf8 (name, error);
	goto_if_nok (error, leave);

	free_info = TRUE;
	if (!mono_reflection_parse_type_checked (str, &info, error))
		goto leave;

	/* mono_reflection_parse_type() mangles the string */

	MONO_HANDLE_ASSIGN (type, type_from_parsed_name (&info, (MonoStackCrawlMark*)stack_mark, ignoreCase, &caller_assembly, error));

	goto_if_nok (error, leave);

	if (MONO_HANDLE_IS_NULL (type)) {
		if (throwOnError) {
			char *tname = info.name_space ? g_strdup_printf ("%s.%s", info.name_space, info.name) : g_strdup (info.name);
			char *aname;
			if (info.assembly.name)
				aname = mono_stringify_assembly_name (&info.assembly);
			else if (caller_assembly)
				aname = mono_stringify_assembly_name (mono_assembly_get_name_internal (caller_assembly));
			else
				aname = g_strdup ("");
			mono_error_set_type_load_name (error, tname, aname, "");
		}
		goto leave;
	}
	
leave:
	if (free_info)
		mono_reflection_free_type_info (&info);
	g_free (str);
	if (!is_ok (error)) {
		if (!throwOnError) {
			mono_error_cleanup (error);
			error_init (error);
		}
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
	} else
		return type;
}


MonoReflectionTypeHandle
ves_icall_System_Type_internal_from_handle (MonoType *handle, MonoError *error)
{
	return mono_type_get_object_handle (handle, error);
}

MonoType*
ves_icall_Mono_RuntimeClassHandle_GetTypeFromClass (MonoClass *klass)
{
	return m_class_get_byval_arg (klass);
}

void
ves_icall_Mono_RuntimeGPtrArrayHandle_GPtrArrayFree (GPtrArray *ptr_array)
{
	g_ptr_array_free (ptr_array, TRUE);
}

void
ves_icall_Mono_SafeStringMarshal_GFree (void *c_str)
{
	g_free (c_str);
}

char*
ves_icall_Mono_SafeStringMarshal_StringToUtf8 (MonoString *volatile* s)
{
	ERROR_DECL (error);
	char *result = mono_string_to_utf8_checked_internal (*s, error);
	mono_error_set_pending_exception (error);
	return result;
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

guint32
ves_icall_RuntimeTypeHandle_type_is_assignable_from (MonoReflectionTypeHandle ref_type, MonoReflectionTypeHandle ref_c, MonoError *error)
{
	g_assert (!MONO_HANDLE_IS_NULL (ref_type));
	
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type_internal (type);
	MonoType *ctype = MONO_HANDLE_GETVAL (ref_c, type);
	MonoClass *klassc = mono_class_from_mono_type_internal (ctype);

	if (type->byref ^ ctype->byref)
		return FALSE;

	if (type->byref) {
		return mono_byref_type_is_assignable_from (type, ctype, FALSE);
	}

	gboolean result;
	mono_class_is_assignable_from_checked (klass, klassc, &result, error);
	return (guint32)result;
}

MonoBoolean
ves_icall_RuntimeTypeHandle_is_subclass_of (MonoType *childType, MonoType *baseType)
{
	ERROR_DECL (error);
	mono_bool result = FALSE;
	MonoClass *childClass;
	MonoClass *baseClass;

	childClass = mono_class_from_mono_type_internal (childType);
	baseClass = mono_class_from_mono_type_internal (baseType);

	if (G_UNLIKELY (childType->byref)) {
		result = !baseType->byref && baseClass == mono_defaults.object_class;
		goto done;
	}

	if (G_UNLIKELY (baseType->byref)) {
		result = FALSE;
		goto done;
	}

	if (childType == baseType) {
		/* .NET IsSubclassOf is not reflexive */
		result = FALSE;
		goto done;
	}

	if (G_UNLIKELY (is_generic_parameter (childType))) {
		/* slow path: walk the type hierarchy looking at base types
		 * until we see baseType.  If the current type is not a gparam,
		 * break out of the loop and use is_subclass_of.
		 */
		MonoClass *c = mono_generic_param_get_base_type (childClass);

		result = FALSE;
		while (c != NULL) {
			if (c == baseClass) {
				result = TRUE;
				break;
			}
			if (!is_generic_parameter (m_class_get_byval_arg (c))) {
				result = mono_class_is_subclass_of_internal (c, baseClass, FALSE);
				break;
			} else
				c = mono_generic_param_get_base_type (c);
		}
	} else {
		result = mono_class_is_subclass_of_internal (childClass, baseClass, FALSE);
	}
done:
	mono_error_set_pending_exception (error);
	return result;
}

guint32
ves_icall_RuntimeTypeHandle_IsInstanceOfType (MonoReflectionTypeHandle ref_type, MonoObjectHandle obj, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type_internal (type);
	mono_class_init_checked (klass, error);
	return_val_if_nok (error, FALSE);
	MonoObjectHandle inst = mono_object_handle_isinst (obj, klass, error);
	return_val_if_nok (error, FALSE);
	return !MONO_HANDLE_IS_NULL (inst);
}

guint32
ves_icall_RuntimeTypeHandle_GetAttributes (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	if (type->byref || type->type == MONO_TYPE_PTR || type->type == MONO_TYPE_FNPTR)
		return TYPE_ATTRIBUTE_NOT_PUBLIC;

	MonoClass *klass = mono_class_from_mono_type_internal (type);
	return mono_class_get_flags (klass);
}

MonoReflectionMarshalAsAttributeHandle
ves_icall_System_Reflection_FieldInfo_get_marshal_info (MonoReflectionFieldHandle field_h, MonoError *error)
{
	MonoClassField *field = MONO_HANDLE_GETVAL (field_h, field);
	MonoClass *klass = field->parent;

	MonoGenericClass *gklass = mono_class_try_get_generic_class (klass);
	if (mono_class_is_gtd (klass) ||
	    (gklass && gklass->context.class_inst->is_open))
		return MONO_HANDLE_CAST (MonoReflectionMarshalAsAttribute, NULL_HANDLE);

	MonoType *ftype = mono_field_get_type_internal (field);
	if (ftype && !(ftype->attrs & FIELD_ATTRIBUTE_HAS_FIELD_MARSHAL))
		return MONO_HANDLE_CAST (MonoReflectionMarshalAsAttribute, NULL_HANDLE);

	MonoMarshalType *info = mono_marshal_load_type_info (klass);

	for (int i = 0; i < info->num_fields; ++i) {
		if (info->fields [i].field == field) {
			if (!info->fields [i].mspec)
				return MONO_HANDLE_CAST (MonoReflectionMarshalAsAttribute, NULL_HANDLE);
			else {
				return mono_reflection_marshal_as_attribute_from_marshal_spec (klass, info->fields [i].mspec, error);
			}
		}
	}

	return MONO_HANDLE_CAST (MonoReflectionMarshalAsAttribute, NULL_HANDLE);
}

MonoReflectionFieldHandle
ves_icall_System_Reflection_FieldInfo_internal_from_handle_type (MonoClassField *handle, MonoType *type, MonoError *error)
{
	MonoClass *klass;

	g_assert (handle);

	if (!type) {
		klass = handle->parent;
	} else {
		klass = mono_class_from_mono_type_internal (type);

		gboolean found = klass == handle->parent || mono_class_has_parent (klass, handle->parent);

		if (!found)
			/* The managed code will throw the exception */
			return MONO_HANDLE_CAST (MonoReflectionField, NULL_HANDLE);
	}

	return mono_field_get_object_handle (klass, handle, error);
}

MonoReflectionEventHandle
ves_icall_System_Reflection_EventInfo_internal_from_handle_type (MonoEvent *handle, MonoType *type, MonoError *error)
{
	MonoClass *klass;

	g_assert (handle);

	if (!type) {
		klass = handle->parent;
	} else {
		klass = mono_class_from_mono_type_internal (type);

		gboolean found = klass == handle->parent || mono_class_has_parent (klass, handle->parent);
		if (!found)
			/* Managed code will throw an exception */
			return MONO_HANDLE_CAST (MonoReflectionEvent, NULL_HANDLE);
	}

	return mono_event_get_object_handle (klass, handle, error);
}

MonoReflectionPropertyHandle
ves_icall_System_Reflection_RuntimePropertyInfo_internal_from_handle_type (MonoProperty *handle, MonoType *type, MonoError *error)
{
	MonoClass *klass;

	g_assert (handle);

	if (!type) {
		klass = handle->parent;
	} else {
		klass = mono_class_from_mono_type_internal (type);

		gboolean found = klass == handle->parent || mono_class_has_parent (klass, handle->parent);
		if (!found)
			/* Managed code will throw an exception */
			return MONO_HANDLE_CAST (MonoReflectionProperty, NULL_HANDLE);
	}

	return mono_property_get_object_handle (klass, handle, error);
}

MonoArrayHandle
ves_icall_System_Reflection_FieldInfo_GetTypeModifiers (MonoReflectionFieldHandle field_h, MonoBoolean optional, MonoError *error)
{
	MonoClassField *field = MONO_HANDLE_GETVAL (field_h, field);

	MonoType *type = mono_field_get_type_checked (field, error);
	return_val_if_nok (error, NULL_HANDLE_ARRAY);

	return type_array_from_modifiers (type, optional, error);
}

int
ves_icall_get_method_attributes (MonoMethod *method)
{
	return method->flags;
}

void
ves_icall_get_method_info (MonoMethod *method, MonoMethodInfo *info, MonoError *error)
{
	MonoMethodSignature* sig = mono_method_signature_checked (method, error);
	return_if_nok (error);

	MonoReflectionTypeHandle rt = mono_type_get_object_handle (m_class_get_byval_arg (method->klass), error);
	return_if_nok (error);

	MONO_STRUCT_SETREF_INTERNAL (info, parent, MONO_HANDLE_RAW (rt));

	MONO_HANDLE_ASSIGN (rt, mono_type_get_object_handle (sig->ret, error));
	return_if_nok (error);

	MONO_STRUCT_SETREF_INTERNAL (info, ret, MONO_HANDLE_RAW (rt));

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

MonoArrayHandle
ves_icall_System_Reflection_MonoMethodInfo_get_parameter_info (MonoMethod *method, MonoReflectionMethodHandle member, MonoError *error)
{
	MonoReflectionTypeHandle reftype = MONO_HANDLE_NEW (MonoReflectionType, NULL);
	MONO_HANDLE_GET (reftype, member, reftype);
	MonoClass *klass = NULL;
	if (!MONO_HANDLE_IS_NULL (reftype))
		klass = mono_class_from_mono_type_internal (MONO_HANDLE_GETVAL (reftype, type));
	return mono_param_get_objects_internal (method, klass, error);
}

MonoReflectionMarshalAsAttributeHandle
ves_icall_System_MonoMethodInfo_get_retval_marshal (MonoMethod *method, MonoError *error)
{
	MonoReflectionMarshalAsAttributeHandle res = MONO_HANDLE_NEW (MonoReflectionMarshalAsAttribute, NULL);

	MonoMarshalSpec **mspecs = g_new (MonoMarshalSpec*, mono_method_signature_internal (method)->param_count + 1);
	mono_method_get_marshal_info (method, mspecs);

	if (mspecs [0]) {
		MONO_HANDLE_ASSIGN (res, mono_reflection_marshal_as_attribute_from_marshal_spec (method->klass, mspecs [0], error));
		goto_if_nok (error, leave);
	}
		
leave:
	for (int i = mono_method_signature_internal (method)->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);

	return res;
}

gint32
ves_icall_RuntimeFieldInfo_GetFieldOffset (MonoReflectionFieldHandle field, MonoError *error)
{
	MonoClassField *class_field = MONO_HANDLE_GETVAL (field, field);
	mono_class_setup_fields (class_field->parent);

	return class_field->offset - MONO_ABI_SIZEOF (MonoObject);
}

MonoReflectionTypeHandle
ves_icall_RuntimeFieldInfo_GetParentType (MonoReflectionFieldHandle field, MonoBoolean declaring, MonoError *error)
{
	MonoClass *parent;

	if (declaring) {
		MonoClassField *f = MONO_HANDLE_GETVAL (field, field);
		parent = f->parent;
	} else {
		parent = MONO_HANDLE_GETVAL (field, klass);
	}

	return mono_type_get_object_handle (m_class_get_byval_arg (parent), error);
}

MonoObjectHandle
ves_icall_RuntimeFieldInfo_GetValueInternal (MonoReflectionFieldHandle field_handle, MonoObjectHandle obj_handle, MonoError *error)
{	
	MonoReflectionField * const field = MONO_HANDLE_RAW (field_handle);
	MonoClassField *cf = field->field;

	MonoObject * const obj = MONO_HANDLE_RAW (obj_handle);
	MonoObject *result;

	result = mono_field_get_value_object_checked (cf, obj, error);

	return MONO_HANDLE_NEW (MonoObject, result);
}

void
ves_icall_RuntimeFieldInfo_SetValueInternal (MonoReflectionFieldHandle field, MonoObjectHandle obj, MonoObjectHandle value, MonoError  *error)
{
	MonoClassField *cf = MONO_HANDLE_GETVAL (field, field);
	MonoType *type = mono_field_get_type_checked (cf, error);
	return_if_nok (error);

	gboolean isref = FALSE;
	MonoGCHandle value_gchandle = 0;
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
				v = (char*)mono_object_handle_pin_unbox (value, &value_gchandle);
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

			if (mono_class_is_nullable (mono_class_from_mono_type_internal (type))) {
				MonoClass *nklass = mono_class_from_mono_type_internal (type);

				/* 
				 * Convert the boxed vtype into a Nullable structure.
				 * This is complicated by the fact that Nullables have
				 * a variable structure.
				 */
				MonoObjectHandle nullable = mono_object_new_handle (nklass, error);
				return_if_nok (error);

				MonoGCHandle nullable_gchandle = 0;
				guint8 *nval = (guint8*)mono_object_handle_pin_unbox (nullable, &nullable_gchandle);
				mono_nullable_init_from_handle (nval, value, nklass);

				isref = FALSE;
				value_gchandle = nullable_gchandle;
				v = (gchar*)nval;
			}
			else {
				isref = !m_class_is_valuetype (gclass->container_class);
				if (!isref && !MONO_HANDLE_IS_NULL (value)) {
					v = (char*)mono_object_handle_pin_unbox (value, &value_gchandle);
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
		MonoVTable *vtable = mono_class_vtable_checked (cf->parent, error);
		goto_if_nok (error, leave);

		if (!vtable->initialized) {
			if (!mono_runtime_class_init_full (vtable, error))
				goto leave;
		}
		if (isref)
			mono_field_static_set_value_internal (vtable, cf, MONO_HANDLE_RAW (value)); /* FIXME make mono_field_static_set_value work with handles for value */
		else
			mono_field_static_set_value_internal (vtable, cf, v);
	} else {

		if (isref)
			MONO_HANDLE_SET_FIELD_REF (obj, cf, value);
		else
			mono_field_set_value_internal (MONO_HANDLE_RAW (obj), cf, v); /* FIXME: make mono_field_set_value take a handle for obj */
	}
leave:
	if (value_gchandle)
		mono_gchandle_free_internal (value_gchandle);
}

static MonoObjectHandle
typed_reference_to_object (MonoTypedRef *tref, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	MonoObjectHandle result;
	if (MONO_TYPE_IS_REFERENCE (tref->type)) {
		MonoObject** objp = (MonoObject **)tref->value;
		result = MONO_HANDLE_NEW (MonoObject, *objp);
	} else if (mono_type_is_pointer (tref->type)) {
		/* Boxed as UIntPtr */
		result = mono_value_box_handle (mono_get_uintptr_class (), tref->value, error);
	} else {
		result = mono_value_box_handle (tref->klass, tref->value, error);
	}
	HANDLE_FUNCTION_RETURN_REF (MonoObject, result);
}

MonoObjectHandle
ves_icall_System_RuntimeFieldHandle_GetValueDirect (MonoReflectionFieldHandle field_h, MonoReflectionTypeHandle field_type_h, MonoTypedRef *obj, MonoReflectionTypeHandle context_type_h, MonoError *error)
{
	MonoClassField *field = MONO_HANDLE_GETVAL (field_h, field);
	MonoClass *klass = mono_class_from_mono_type_internal (field->type);

	if (!MONO_TYPE_ISSTRUCT (m_class_get_byval_arg (field->parent))) {
		mono_error_set_not_implemented (error, "");
		return MONO_HANDLE_NEW (MonoObject, NULL);
	} else if (MONO_TYPE_IS_REFERENCE (field->type)) {
		return MONO_HANDLE_NEW (MonoObject, *(MonoObject**)((guint8*)obj->value + field->offset - sizeof (MonoObject)));
	} else {
		return mono_value_box_handle (klass, (guint8*)obj->value + field->offset - sizeof (MonoObject), error);
	}
}

void
ves_icall_System_RuntimeFieldHandle_SetValueDirect (MonoReflectionFieldHandle field_h, MonoReflectionTypeHandle field_type_h, MonoTypedRef *obj, MonoObjectHandle value_h, MonoReflectionTypeHandle context_type_h, MonoError *error)
{
	MonoClassField *f = MONO_HANDLE_GETVAL (field_h, field);

	g_assert (obj);

	mono_class_setup_fields (f->parent);

	if (!MONO_TYPE_ISSTRUCT (m_class_get_byval_arg (f->parent))) {
		MonoObjectHandle objHandle = typed_reference_to_object (obj, error);
		return_if_nok (error);
		ves_icall_RuntimeFieldInfo_SetValueInternal (field_h, objHandle, value_h, error);
	} else if (MONO_TYPE_IS_REFERENCE (f->type)) {
		mono_copy_value (f->type, (guint8*)obj->value + m_field_get_offset (f) - sizeof (MonoObject), MONO_HANDLE_RAW (value_h), FALSE);
	} else {
		MonoGCHandle gchandle = NULL;
		g_assert (MONO_HANDLE_RAW (value_h));
		mono_copy_value (f->type, (guint8*)obj->value + m_field_get_offset (f) - sizeof (MonoObject), mono_object_handle_pin_unbox (value_h, &gchandle), FALSE);
		mono_gchandle_free_internal (gchandle);
	}
}

MonoObjectHandle
ves_icall_RuntimeFieldInfo_GetRawConstantValue (MonoReflectionFieldHandle rfield, MonoError* error)
{
	MonoObjectHandle o_handle = NULL_HANDLE_INIT;

	MonoObject *o = NULL;
	MonoClassField *field = MONO_HANDLE_GETVAL  (rfield, field);
	MonoClass *klass;
	gchar *v;
	MonoTypeEnum def_type;
	const char *def_value;
	MonoType *t;
	MonoStringHandle string_handle = MONO_HANDLE_NEW (MonoString, NULL); // FIXME? Not always needed.

	mono_class_init_internal (field->parent);

	t = mono_field_get_type_checked (field, error);
	goto_if_nok (error, return_null);

	if (!(t->attrs & FIELD_ATTRIBUTE_HAS_DEFAULT))
		goto invalid_operation;

	if (image_is_dynamic (m_class_get_image (field->parent))) {
		MonoClass *klass = field->parent;
		int fidx = field - m_class_get_fields (klass);
		MonoFieldDefaultValue *def_values = mono_class_get_field_def_values (klass);

		g_assert (def_values);
		def_type = def_values [fidx].def_type;
		def_value = def_values [fidx].data;

		if (def_type == MONO_TYPE_END)
			goto invalid_operation;
	} else {
		def_value = mono_class_get_field_default_value (field, &def_type);
		/* FIXME, maybe we should try to raise TLE if field->parent is broken */
		if (!def_value)
			goto invalid_operation;
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
		klass = mono_class_from_mono_type_internal (t);
		g_free (t);
		o = mono_object_new_checked (klass, error);
		goto_if_nok (error, return_null);
		o_handle = MONO_HANDLE_NEW (MonoObject, o);
		v = ((gchar *) o) + sizeof (MonoObject);
		(void)mono_get_constant_value_from_blob (def_type, def_value, v, string_handle, error);
		goto_if_nok (error, return_null);
		break;
	}
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:
		(void)mono_get_constant_value_from_blob (def_type, def_value, &o, string_handle, error);
		goto_if_nok (error, return_null);
		o_handle = MONO_HANDLE_NEW (MonoObject, o);
		break;
	default:
		g_assert_not_reached ();
	}

	goto exit;
invalid_operation:
	mono_error_set_invalid_operation (error, NULL);
	// fall through
return_null:
	o_handle = NULL_HANDLE;
	// fall through
exit:
	return o_handle;
}

MonoReflectionTypeHandle
ves_icall_RuntimeFieldInfo_ResolveType (MonoReflectionFieldHandle ref_field, MonoError *error)
{
	MonoClassField *field = MONO_HANDLE_GETVAL (ref_field, field);
	MonoType *type = mono_field_get_type_checked (field, error);
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE));
	return mono_type_get_object_handle (type, error);
}

void
ves_icall_RuntimePropertyInfo_get_property_info (MonoReflectionPropertyHandle property, MonoPropertyInfo *info, PInfo req_info, MonoError *error)
{
	const MonoProperty *pproperty = MONO_HANDLE_GETVAL (property, property);

	if ((req_info & PInfo_ReflectedType) != 0) {
		MonoClass *klass = MONO_HANDLE_GETVAL (property, klass);
		MonoReflectionTypeHandle rt = mono_type_get_object_handle (m_class_get_byval_arg (klass), error);
		return_if_nok (error);

		MONO_STRUCT_SETREF_INTERNAL (info, parent, MONO_HANDLE_RAW (rt));
	}
	if ((req_info & PInfo_DeclaringType) != 0) {
		MonoReflectionTypeHandle rt = mono_type_get_object_handle (m_class_get_byval_arg (pproperty->parent), error);
		return_if_nok (error);

		MONO_STRUCT_SETREF_INTERNAL (info, declaring_type, MONO_HANDLE_RAW (rt));
	}

	if ((req_info & PInfo_Name) != 0) {
		MonoStringHandle name = mono_string_new_handle (pproperty->name, error);
		return_if_nok (error);

		MONO_STRUCT_SETREF_INTERNAL (info, name, MONO_HANDLE_RAW (name));
	}

	if ((req_info & PInfo_Attributes) != 0)
		info->attrs = pproperty->attrs;

	if ((req_info & PInfo_GetMethod) != 0) {
		MonoClass *property_klass = MONO_HANDLE_GETVAL (property, klass);
		MonoReflectionMethodHandle rm;
		if (pproperty->get &&
		    (((pproperty->get->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) != METHOD_ATTRIBUTE_PRIVATE) ||
		     pproperty->get->klass == property_klass)) {
			rm = mono_method_get_object_handle (pproperty->get, property_klass, error);
			return_if_nok (error);
		} else {
			rm = MONO_HANDLE_NEW (MonoReflectionMethod, NULL);
		}

		MONO_STRUCT_SETREF_INTERNAL (info, get, MONO_HANDLE_RAW (rm));
	}
	if ((req_info & PInfo_SetMethod) != 0) {
		MonoClass *property_klass = MONO_HANDLE_GETVAL (property, klass);
		MonoReflectionMethodHandle rm;
		if (pproperty->set &&
		    (((pproperty->set->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) != METHOD_ATTRIBUTE_PRIVATE) ||
		     pproperty->set->klass == property_klass)) {
			rm = mono_method_get_object_handle (pproperty->set, property_klass, error);
			return_if_nok (error);
		} else {
			rm = MONO_HANDLE_NEW (MonoReflectionMethod, NULL);
		}

		MONO_STRUCT_SETREF_INTERNAL (info, set, MONO_HANDLE_RAW (rm));
	}
	/* 
	 * There may be other methods defined for properties, though, it seems they are not exposed 
	 * in the reflection API 
	 */
}

static gboolean
add_event_other_methods_to_array (MonoMethod *m, MonoArrayHandle dest, int i, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoReflectionMethodHandle rm = mono_method_get_object_handle (m, NULL, error);
	goto_if_nok (error, leave);
	MONO_HANDLE_ARRAY_SETREF (dest, i, rm);
leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

void
ves_icall_RuntimeEventInfo_get_event_info (MonoReflectionMonoEventHandle ref_event, MonoEventInfo *info, MonoError *error)
{
	MonoClass *klass = MONO_HANDLE_GETVAL (ref_event, klass);
	MonoEvent *event = MONO_HANDLE_GETVAL (ref_event, event);

	MonoReflectionTypeHandle rt = mono_type_get_object_handle (m_class_get_byval_arg (klass), error);
	return_if_nok (error);
	MONO_STRUCT_SETREF_INTERNAL (info, reflected_type, MONO_HANDLE_RAW (rt));

	rt = mono_type_get_object_handle (m_class_get_byval_arg (event->parent), error);
	return_if_nok (error);
	MONO_STRUCT_SETREF_INTERNAL (info, declaring_type, MONO_HANDLE_RAW (rt));

	MonoStringHandle ev_name = mono_string_new_handle (event->name, error);
	return_if_nok (error);
	MONO_STRUCT_SETREF_INTERNAL (info, name, MONO_HANDLE_RAW (ev_name));

	info->attrs = event->attrs;

	MonoReflectionMethodHandle rm;
	if (event->add) {
		rm = mono_method_get_object_handle (event->add, klass, error);
		return_if_nok (error);
	} else {
		rm = MONO_HANDLE_NEW (MonoReflectionMethod, NULL);
	}

	MONO_STRUCT_SETREF_INTERNAL (info, add_method, MONO_HANDLE_RAW (rm));

	if (event->remove) {
		rm = mono_method_get_object_handle (event->remove, klass, error);
		return_if_nok (error);
	} else {
		rm = MONO_HANDLE_NEW (MonoReflectionMethod, NULL);
	}

	MONO_STRUCT_SETREF_INTERNAL (info, remove_method, MONO_HANDLE_RAW (rm));

	if (event->raise) {
		rm = mono_method_get_object_handle (event->raise, klass, error);
		return_if_nok (error);
	} else {
		rm = MONO_HANDLE_NEW (MonoReflectionMethod, NULL);
	}

	MONO_STRUCT_SETREF_INTERNAL (info, raise_method, MONO_HANDLE_RAW (rm));

#ifndef MONO_SMALL_CONFIG
	if (event->other) {
		int i, n = 0;
		while (event->other [n])
			n++;
		MonoArrayHandle info_arr = mono_array_new_handle (mono_defaults.method_info_class, n, error);
		return_if_nok (error);

		MONO_STRUCT_SETREF_INTERNAL (info, other_methods, MONO_HANDLE_RAW  (info_arr));

		for (i = 0; i < n; i++)
			if (!add_event_other_methods_to_array (event->other [i], info_arr, i, error))
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
	return_if_nok (error);

	int klass_interface_count = m_class_get_interface_count (klass);
	MonoClass **klass_interfaces = m_class_get_interfaces (klass);
	for (i = 0; i < klass_interface_count; i++) {
		ic = klass_interfaces [i];
		g_hash_table_insert (ifaces, ic, ic);

		collect_interfaces (ic, ifaces, error);
		return_if_nok (error);
	}
}

typedef struct {
	MonoArrayHandle iface_array;
	MonoGenericContext *context;
	MonoError *error;
	int next_idx;
} FillIfaceArrayData;

static void
fill_iface_array (gpointer key, gpointer value, gpointer user_data)
{
	HANDLE_FUNCTION_ENTER ();
	FillIfaceArrayData *data = (FillIfaceArrayData *)user_data;
	MonoClass *ic = (MonoClass *)key;
	MonoType *ret = m_class_get_byval_arg (ic), *inflated = NULL;
	MonoError *error = data->error;

	goto_if_nok (error, leave);

	if (data->context && mono_class_is_ginst (ic) && mono_class_get_generic_class (ic)->context.class_inst->is_open) {
		inflated = ret = mono_class_inflate_generic_type_checked (ret, data->context, error);
		goto_if_nok (error, leave);
	}

	MonoReflectionTypeHandle rt;
	rt = mono_type_get_object_handle (ret, error);
	goto_if_nok (error, leave);

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

	return m_class_get_type_token (k);
}

MonoArrayHandle
ves_icall_RuntimeType_GetInterfaces (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type_internal (type);

	GHashTable *iface_hash = g_hash_table_new (get_interfaces_hash, NULL);

	MonoGenericContext *context = NULL;
	if (mono_class_is_ginst (klass) && mono_class_get_generic_class (klass)->context.class_inst->is_open) {
		context = mono_class_get_context (klass);
		klass = mono_class_get_generic_class (klass)->container_class;
	}

	for (MonoClass *parent = klass; parent; parent = m_class_get_parent (parent)) {
		mono_class_setup_interfaces (parent, error);
		goto_if_nok (error, fail);
		collect_interfaces (parent, iface_hash, error);
		goto_if_nok (error, fail);
	}

	MonoDomain *domain = mono_get_root_domain ();

	int len;
	len = g_hash_table_size (iface_hash);
	if (len == 0) {
		g_hash_table_destroy (iface_hash);
		if (!domain->empty_types) {
			domain->empty_types = mono_array_new_cached (mono_defaults.runtimetype_class, 0, error);
			goto_if_nok (error, fail);
		}
		return MONO_HANDLE_NEW (MonoArray, domain->empty_types);
	}

	FillIfaceArrayData data;
	data.iface_array = MONO_HANDLE_NEW (MonoArray, mono_array_new_cached (mono_defaults.runtimetype_class, len, error));
	goto_if_nok (error, fail);
	data.context = context;
	data.error = error;
	data.next_idx = 0;

	g_hash_table_foreach (iface_hash, fill_iface_array, &data);

	goto_if_nok (error, fail);

	g_hash_table_destroy (iface_hash);
	return data.iface_array;

fail:
	g_hash_table_destroy (iface_hash);
	return NULL_HANDLE_ARRAY;
}

static gboolean
method_is_reabstracted (MonoMethod *method)
{
	/* only on interfaces */
	/* method is marked "final abstract" */
	/* FIXME: we need some other way to detect reabstracted methods.  "final" is an incidental detail of the spec. */
	return m_method_is_final (method) && m_method_is_abstract (method);
}

static gboolean
method_is_dim (MonoMethod *method)
{
	/* only valid on interface methods*/
	/* method is marked "virtual" but not "virtual abstract" */
	return m_method_is_virtual (method) && !m_method_is_abstract (method);
}

static gboolean
set_interface_map_data_method_object (MonoMethod *method, MonoClass *iclass, int ioffset, MonoClass *klass, MonoArrayHandle targets, MonoArrayHandle methods, int i, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoReflectionMethodHandle member = mono_method_get_object_handle (method, iclass, error);
	goto_if_nok (error, leave);

	MONO_HANDLE_ARRAY_SETREF (methods, i, member);

	MonoMethod* foundMethod = m_class_get_vtable (klass) [i + ioffset];

	if (mono_class_has_dim_conflicts (klass) && mono_class_is_interface (foundMethod->klass)) {
		GSList* conflicts = mono_class_get_dim_conflicts (klass);
		GSList* l;
		MonoMethod* decl = method;

		if (decl->is_inflated)
			decl = ((MonoMethodInflated*)decl)->declaring;

		gboolean in_conflict = FALSE;
		for (l = conflicts; l; l = l->next) {
			if (decl == l->data) {
				in_conflict = TRUE;
				break;
			}
		}
		if (in_conflict) {
			MONO_HANDLE_ARRAY_SETREF (targets, i, NULL_HANDLE);
			goto leave;
		}
	}

	/*
	 * if the iterface method is reabstracted, and either the found implementation method is abstract, or the found
	 * implementation method is from another DIM (meaning neither klass nor any of its ancestor classes implemented
	 * the method), then say the target method is null.
	 */
	if (method_is_reabstracted (method) &&
	    (m_method_is_abstract (foundMethod) ||
	     (mono_class_is_interface (foundMethod->klass) && method_is_dim (foundMethod))))
		MONO_HANDLE_ARRAY_SETREF (targets, i, NULL_HANDLE);
	else if (mono_class_is_interface (foundMethod->klass) && method_is_reabstracted (foundMethod) && !m_class_is_abstract (klass)) {
		/* if the method we found is a reabstracted DIM method, but the class isn't abstract, return NULL */
		/*
		 * (C# doesn't seem to allow constructing such types, it requires the whole class to be abstract - in
		 * which case we are supposed to return the reabstracted interface method.  But in IL we can make a
		 * non-abstract class with reabstracted interface methods - which is supposed to fail with an
		 * EntryPointNotFoundException at invoke time, but does not prevent the class from loading.)
		 */
		MONO_HANDLE_ARRAY_SETREF (targets, i, NULL_HANDLE);
	} else {
		MONO_HANDLE_ASSIGN (member, mono_method_get_object_handle (foundMethod, mono_class_is_interface (foundMethod->klass) ? foundMethod->klass : klass, error));
		goto_if_nok (error, leave);
		MONO_HANDLE_ARRAY_SETREF (targets, i, member);
	}
		
leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

void
ves_icall_RuntimeType_GetInterfaceMapData (MonoReflectionTypeHandle ref_type, MonoReflectionTypeHandle ref_iface, MonoArrayHandleOut targets, MonoArrayHandleOut methods, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type_internal (type);
	MonoType *iface = MONO_HANDLE_GETVAL (ref_iface, type);
	MonoClass *iclass = mono_class_from_mono_type_internal (iface);

	mono_class_init_checked (klass, error);
	return_if_nok (error);
	mono_class_init_checked (iclass, error);
	return_if_nok (error);

	mono_class_setup_vtable (klass);

	gboolean variance_used;
	int ioffset = mono_class_interface_offset_with_variance (klass, iclass, &variance_used);
	if (ioffset == -1)
		return;

	MonoMethod* method;
	int i = 0;
	gpointer iter = NULL;

	while ((method = mono_class_get_methods(iclass, &iter))) {
		if (method->flags & METHOD_ATTRIBUTE_VIRTUAL)
			i++;
	}
	
	MonoArrayHandle targets_arr = mono_array_new_handle (mono_defaults.method_info_class, i, error);
	return_if_nok (error);
	MONO_HANDLE_ASSIGN (targets, targets_arr);

	MonoArrayHandle methods_arr = mono_array_new_handle (mono_defaults.method_info_class, i, error);
	return_if_nok (error);
	MONO_HANDLE_ASSIGN (methods, methods_arr);

	i = 0;
	iter = NULL;

	while ((method = mono_class_get_methods (iclass, &iter))) {
		if (!(method->flags & METHOD_ATTRIBUTE_VIRTUAL))
			continue;
		if (!set_interface_map_data_method_object (method, iclass, ioffset, klass, targets, methods, i, error))
			return;
		i ++;
	}
}

void
ves_icall_RuntimeType_GetPacking (MonoReflectionTypeHandle ref_type, guint32 *packing, guint32 *size, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type_internal (type);

	mono_class_init_checked (klass, error);
	return_if_nok (error);

	if (image_is_dynamic (m_class_get_image (klass))) {
		MonoReflectionTypeBuilderHandle tb = MONO_HANDLE_CAST (MonoReflectionTypeBuilder, ref_type);
		*packing = MONO_HANDLE_GETVAL (tb, packing_size);
		*size = MONO_HANDLE_GETVAL (tb, class_size);
	} else {
		mono_metadata_packing_from_typedef (m_class_get_image (klass), m_class_get_type_token (klass), packing, size);
	}
}

MonoReflectionTypeHandle
ves_icall_RuntimeTypeHandle_GetElementType (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	if (!type->byref && type->type == MONO_TYPE_SZARRAY)
		return mono_type_get_object_handle (m_class_get_byval_arg (type->data.klass), error);

	MonoClass *klass = mono_class_from_mono_type_internal (type);
	mono_class_init_checked (klass, error);
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE));

	// GetElementType should only return a type for:
	// Array Pointer PassedByRef
	if (type->byref)
		return mono_type_get_object_handle (m_class_get_byval_arg (klass), error);
	else if (m_class_get_element_class (klass) && MONO_CLASS_IS_ARRAY (klass))
		return mono_type_get_object_handle (m_class_get_byval_arg (m_class_get_element_class (klass)), error);
	else if (m_class_get_element_class (klass) && type->type == MONO_TYPE_PTR)
		return mono_type_get_object_handle (m_class_get_byval_arg (m_class_get_element_class (klass)), error);
	else
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
}

MonoReflectionTypeHandle
ves_icall_RuntimeTypeHandle_GetBaseType (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	if (type->byref)
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);

	MonoClass *klass = mono_class_from_mono_type_internal (type);
	if (!m_class_get_parent (klass))
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);

	return mono_type_get_object_handle (m_class_get_byval_arg (m_class_get_parent (klass)), error);
}

guint32
ves_icall_RuntimeTypeHandle_GetCorElementType (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	if (type->byref)
		return MONO_TYPE_BYREF;
	else
		return (guint32)type->type;
}

MonoBoolean
ves_icall_RuntimeTypeHandle_HasReferences (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass;

	klass = mono_class_from_mono_type_internal (type);
	mono_class_init_internal (klass);
	return m_class_has_references (klass);
}

MonoBoolean
ves_icall_RuntimeTypeHandle_IsByRefLike (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	/* .NET Core says byref types are not IsByRefLike */
	if (type->byref)
		return FALSE;
	MonoClass *klass = mono_class_from_mono_type_internal (type);
	return m_class_is_byreflike (klass);
}

MonoBoolean
ves_icall_RuntimeTypeHandle_IsComObject (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type_internal (type);
	mono_class_init_checked (klass, error);
	return_val_if_nok (error, FALSE);

	return mono_class_is_com_object (klass);
}

guint32
ves_icall_reflection_get_token (MonoObjectHandle obj, MonoError *error)
{
	error_init (error);
	return mono_reflection_get_token_checked (obj, error);
}

MonoReflectionModuleHandle
ves_icall_RuntimeTypeHandle_GetModule (MonoReflectionTypeHandle type, MonoError *error)
{
	MonoType *t = MONO_HANDLE_GETVAL (type, type);
	MonoClass *klass = mono_class_from_mono_type_internal (t);
	return mono_module_get_object_handle (m_class_get_image (klass), error);
}

MonoReflectionAssemblyHandle
ves_icall_RuntimeTypeHandle_GetAssembly (MonoReflectionTypeHandle type, MonoError *error)
{
	MonoType *t = MONO_HANDLE_GETVAL (type, type);
	MonoClass *klass = mono_class_from_mono_type_internal (t);
	return mono_assembly_get_object_handle (m_class_get_image (klass)->assembly, error);
}

MonoReflectionTypeHandle
ves_icall_RuntimeType_get_DeclaringType (MonoReflectionTypeHandle ref_type, MonoError *error)
{
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
		klass = m_class_get_nested_in (mono_class_from_mono_type_internal (type));
	}

	if (!klass)
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);

	return mono_type_get_object_handle (m_class_get_byval_arg (klass), error);
}

MonoStringHandle
ves_icall_RuntimeType_get_Name (MonoReflectionTypeHandle reftype, MonoError *error)
{
	MonoType *type = MONO_HANDLE_RAW(reftype)->type; 
	MonoClass *klass = mono_class_from_mono_type_internal (type);
	// FIXME: this should be escaped in some scenarios with mono_identifier_escape_type_name_chars
	// Determining exactly when to do so is fairly difficult, so for now we don't bother to avoid regressions
	const char *klass_name = m_class_get_name (klass);

	if (type->byref) {
		char *n = g_strdup_printf ("%s&", klass_name);
		MonoStringHandle res = mono_string_new_handle (n, error);

		g_free (n);

		return res;
	} else {
		return mono_string_new_handle (klass_name, error);
	}
}

MonoStringHandle
ves_icall_RuntimeType_get_Namespace (MonoReflectionTypeHandle type, MonoError *error)
{
	MonoClass *klass = mono_class_from_mono_type_handle (type);

	MonoClass *klass_nested_in;
	while ((klass_nested_in = m_class_get_nested_in (klass)))
		klass = klass_nested_in;

	if (m_class_get_name_space (klass) [0] == '\0')
		return NULL_HANDLE_STRING;

	char *escaped = mono_identifier_escape_type_name_chars (m_class_get_name_space (klass));
	MonoStringHandle res = mono_string_new_handle (escaped, error);
	g_free (escaped);
	return res;
}

gint32
ves_icall_RuntimeTypeHandle_GetArrayRank (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	if (type->type != MONO_TYPE_ARRAY && type->type != MONO_TYPE_SZARRAY) {
		mono_error_set_argument (error, "type", "Type must be an array type");
		return 0;
	}

	MonoClass *klass = mono_class_from_mono_type_internal (type);

	return m_class_get_rank (klass);
}

static MonoArrayHandle
create_type_array (MonoBoolean runtimeTypeArray, int count, MonoError *error)
{
	return mono_array_new_handle (runtimeTypeArray ? mono_defaults.runtimetype_class : mono_defaults.systemtype_class, count, error);
}

static gboolean
set_type_object_in_array (MonoType *type, MonoArrayHandle dest, int i, MonoError *error)
{
	HANDLE_FUNCTION_ENTER();
	error_init (error);
	MonoReflectionTypeHandle rt = mono_type_get_object_handle (type, error);
	goto_if_nok (error, leave);

	MONO_HANDLE_ARRAY_SETREF (dest, i, rt);

leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

MonoArrayHandle
ves_icall_RuntimeType_GetGenericArguments (MonoReflectionTypeHandle ref_type, MonoBoolean runtimeTypeArray, MonoError *error)
{
	error_init (error);

	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type_internal (type);

	MonoArrayHandle res = MONO_HANDLE_NEW (MonoArray, NULL);
	if (mono_class_is_gtd (klass)) {
		MonoGenericContainer *container = mono_class_get_generic_container (klass);
		MONO_HANDLE_ASSIGN (res, create_type_array (runtimeTypeArray, container->type_argc, error));
		goto_if_nok (error, leave);
		for (int i = 0; i < container->type_argc; ++i) {
			MonoClass *pklass = mono_class_create_generic_parameter (mono_generic_container_get_param (container, i));

			if (!set_type_object_in_array (m_class_get_byval_arg (pklass), res, i, error))
				goto leave;
		}
		
	} else if (mono_class_is_ginst (klass)) {
		MonoGenericInst *inst = mono_class_get_generic_class (klass)->context.class_inst;
		MONO_HANDLE_ASSIGN (res, create_type_array (runtimeTypeArray, inst->type_argc, error));
		goto_if_nok (error, leave);
		for (int i = 0; i < inst->type_argc; ++i) {
			if (!set_type_object_in_array (inst->type_argv [i], res, i, error))
				goto leave;
		}
	}

leave:
	return res;
}

MonoBoolean
ves_icall_RuntimeTypeHandle_IsGenericTypeDefinition (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);

	if (!IS_MONOTYPE (MONO_HANDLE_RAW(ref_type)))
		return FALSE;

	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	if (type->byref)
		return FALSE;

	MonoClass *klass = mono_class_from_mono_type_internal (type);
	return mono_class_is_gtd (klass);
}

MonoReflectionTypeHandle
ves_icall_RuntimeTypeHandle_GetGenericTypeDefinition_impl (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	MonoReflectionTypeHandle ret = MONO_HANDLE_NEW (MonoReflectionType, NULL);

	if (type->byref)
		goto leave;

	MonoClass *klass;
	klass = mono_class_from_mono_type_internal (type);

	if (mono_class_is_gtd (klass)) {
		/* check this one */
		MONO_HANDLE_ASSIGN (ret, ref_type);
		goto leave;
	}
	if (mono_class_is_ginst (klass)) {
		MonoClass *generic_class = mono_class_get_generic_class (klass)->container_class;

		MonoGCHandle ref_info_handle = mono_class_get_ref_info_handle (generic_class);
		
		if (m_class_was_typebuilder (generic_class) && ref_info_handle) {
			MonoObjectHandle tb = mono_gchandle_get_target_handle (ref_info_handle);
			g_assert (!MONO_HANDLE_IS_NULL (tb));
			MONO_HANDLE_ASSIGN (ret, tb);
		} else {
			MONO_HANDLE_ASSIGN (ret, mono_type_get_object_handle (m_class_get_byval_arg (generic_class), error));
		}
	}
leave:
	return ret;
}

MonoReflectionTypeHandle
ves_icall_RuntimeType_MakeGenericType (MonoReflectionTypeHandle reftype, MonoArrayHandle type_array, MonoError *error)
{
	error_init (error);

	g_assert (IS_MONOTYPE_HANDLE (reftype));
	MonoType *type = MONO_HANDLE_GETVAL (reftype, type);
	mono_class_init_checked (mono_class_from_mono_type_internal (type), error);
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE));

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

	MonoClass *klass = mono_class_from_mono_type_internal (geninst);

	/*we might inflate to the GTD*/
	if (mono_class_is_ginst (klass) && !mono_verifier_class_is_valid_generic_instantiation (klass)) {
		mono_error_set_argument (error, "typeArguments", "Invalid generic arguments");
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
	}

	return mono_type_get_object_handle (geninst, error);
}

MonoBoolean
ves_icall_RuntimeTypeHandle_HasInstantiation (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoClass *klass;

	if (!IS_MONOTYPE (MONO_HANDLE_RAW (ref_type)))
		return FALSE;

	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	if (type->byref)
		return FALSE;

	klass = mono_class_from_mono_type_internal (type);
	return mono_class_is_ginst (klass) || mono_class_is_gtd (klass);
}

gint32
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

MonoGenericParamInfo *
ves_icall_RuntimeTypeHandle_GetGenericParameterInfo (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	return mono_generic_param_info (type->data.generic_param);
}

MonoBoolean
ves_icall_RuntimeTypeHandle_IsGenericVariable (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL(ref_type, type);
	return is_generic_parameter (type);
}

MonoReflectionMethodHandle
ves_icall_RuntimeType_GetCorrespondingInflatedMethod (MonoReflectionTypeHandle ref_type, 
						      MonoReflectionMethodHandle generic,
						      MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type_internal (type);
		
	mono_class_init_checked (klass, error);
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE));

	MonoMethod *generic_method = MONO_HANDLE_GETVAL (generic, method);
	
	MonoReflectionMethodHandle ret = MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE);
	MonoMethod *method;
	gpointer iter = NULL;
	while ((method = mono_class_get_methods (klass, &iter))) {
                if (method->token == generic_method->token) {
			ret = mono_method_get_object_handle (method, klass, error);
			return_val_if_nok (error, MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE));
		}
        }

	return ret;
}

MonoReflectionMethodHandle
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

	MonoMethod *method;
	method = mono_type_get_generic_param_owner (type)->owner.method;
	g_assert (method);

	MONO_HANDLE_ASSIGN (ret, mono_method_get_object_handle (method, method->klass, error));
leave:
	return ret;
}

void
ves_icall_RuntimeMethodInfo_GetPInvoke (MonoReflectionMethodHandle ref_method, int* flags, MonoStringHandleOut entry_point, MonoStringHandleOut dll_name, MonoError *error)
{
	MonoMethod *method = MONO_HANDLE_GETVAL (ref_method, method);
	MonoImage *image = m_class_get_image (method->klass);
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
	MONO_HANDLE_ASSIGN (entry_point,  mono_string_new_handle (import, error));
	return_if_nok (error);
	MONO_HANDLE_ASSIGN (dll_name, mono_string_new_handle (scope, error));
}

MonoReflectionMethodHandle
ves_icall_RuntimeMethodInfo_GetGenericMethodDefinition (MonoReflectionMethodHandle ref_method, MonoError *error)
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

	if (image_is_dynamic (m_class_get_image (method->klass))) {
		MonoDynamicImage *image = (MonoDynamicImage*)m_class_get_image (method->klass);

		/*
		 * FIXME: Why is this stuff needed at all ? Why can't the code below work for
		 * the dynamic case as well ?
		 */
		mono_image_lock ((MonoImage*)image);
		MonoReflectionMethodHandle res = MONO_HANDLE_NEW (MonoReflectionMethod, (MonoReflectionMethod*)mono_g_hash_table_lookup (image->generic_def_objects, imethod));
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

	return mono_method_get_object_handle (result, NULL, error);
}

static GENERATE_TRY_GET_CLASS_WITH_CACHE (stream, "System.IO", "Stream")
static int io_stream_begin_read_slot = -1;
static int io_stream_begin_write_slot = -1;
static int io_stream_end_read_slot = -1;
static int io_stream_end_write_slot = -1;
static gboolean io_stream_slots_set = FALSE;

static void
init_io_stream_slots (void)
{
	MonoClass* klass = mono_class_try_get_stream_class ();
	mono_class_setup_vtable (klass);
	MonoMethod **klass_methods = m_class_get_methods (klass);
	if (!klass_methods) {
		mono_class_setup_methods (klass);
		klass_methods = m_class_get_methods (klass);
	}
	int method_count = mono_class_get_method_count (klass);
	int methods_found = 0;
	for (int i = 0; i < method_count; i++) {
		// find slots for Begin(End)Read and Begin(End)Write
		MonoMethod* m = klass_methods [i];
		if (m->slot == -1)
			continue;

		if (!strcmp (m->name, "BeginRead")) {
			methods_found++;
			io_stream_begin_read_slot = m->slot;
		} else if (!strcmp (m->name, "BeginWrite")) {
			methods_found++;
			io_stream_begin_write_slot = m->slot;
		} else if (!strcmp (m->name, "EndRead")) {
			methods_found++;
			io_stream_end_read_slot = m->slot;
		} else if (!strcmp (m->name, "EndWrite")) {
			methods_found++;
			io_stream_end_write_slot = m->slot;
		}
	}
	g_assert (methods_found <= 4); // some of them can be linked out
	io_stream_slots_set = TRUE;
}

MonoBoolean
ves_icall_System_IO_Stream_HasOverriddenBeginEndRead (MonoObjectHandle stream, MonoError *error)
{
	MonoClass* curr_klass = MONO_HANDLE_GET_CLASS (stream);
	MonoClass* base_klass = mono_class_try_get_stream_class ();

	if (!io_stream_slots_set)
		init_io_stream_slots ();

	// slots can still be -1 and it means Linker removed the methods from the base class (Stream)
	// in this case we can safely assume the methods are not overridden
	// otherwise - check vtable
	MonoMethod **curr_klass_vtable = m_class_get_vtable (curr_klass);
	gboolean begin_read_is_overriden = io_stream_begin_read_slot != -1 && curr_klass_vtable [io_stream_begin_read_slot]->klass != base_klass;
	gboolean end_read_is_overriden = io_stream_end_read_slot != -1 && curr_klass_vtable [io_stream_end_read_slot]->klass != base_klass;

	// return true if BeginRead or EndRead were overriden
	return begin_read_is_overriden || end_read_is_overriden;
}

MonoBoolean
ves_icall_System_IO_Stream_HasOverriddenBeginEndWrite (MonoObjectHandle stream, MonoError *error)
{
	MonoClass* curr_klass = MONO_HANDLE_GETVAL (stream, vtable)->klass;
	MonoClass* base_klass = mono_class_try_get_stream_class ();

	if (!io_stream_slots_set)
		init_io_stream_slots ();

	// slots can still be -1 and it means Linker removed the methods from the base class (Stream)
	// in this case we can safely assume the methods are not overridden
	// otherwise - check vtable
	MonoMethod **curr_klass_vtable = m_class_get_vtable (curr_klass);
	gboolean begin_write_is_overriden = io_stream_begin_write_slot != -1 && curr_klass_vtable [io_stream_begin_write_slot]->klass != base_klass;
	gboolean end_write_is_overriden = io_stream_end_write_slot != -1 && curr_klass_vtable [io_stream_end_write_slot]->klass != base_klass;

	// return true if BeginWrite or EndWrite were overriden
	return begin_write_is_overriden || end_write_is_overriden;
}

MonoBoolean
ves_icall_RuntimeMethodInfo_get_IsGenericMethod (MonoReflectionMethodHandle ref_method, MonoError *erro)
{
	MonoMethod *method = MONO_HANDLE_GETVAL (ref_method, method);
	return mono_method_signature_internal (method)->generic_param_count != 0;
}

MonoBoolean
ves_icall_RuntimeMethodInfo_get_IsGenericMethodDefinition (MonoReflectionMethodHandle ref_method, MonoError *Error)
{
	MonoMethod *method = MONO_HANDLE_GETVAL (ref_method, method);
	return method->is_generic;
}

static gboolean
set_array_generic_argument_handle_inflated (MonoGenericInst *inst, int i, MonoArrayHandle arr, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoReflectionTypeHandle rt = mono_type_get_object_handle (inst->type_argv [i], error);
	goto_if_nok (error, leave);
	MONO_HANDLE_ARRAY_SETREF (arr, i, rt);
leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

static gboolean
set_array_generic_argument_handle_gparam (MonoGenericContainer *container, int i, MonoArrayHandle arr, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoGenericParam *param = mono_generic_container_get_param (container, i);
	MonoClass *pklass = mono_class_create_generic_parameter (param);
	MonoReflectionTypeHandle rt = mono_type_get_object_handle (m_class_get_byval_arg (pklass), error);
	goto_if_nok (error, leave);
	MONO_HANDLE_ARRAY_SETREF (arr, i, rt);
leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

MonoArrayHandle
ves_icall_RuntimeMethodInfo_GetGenericArguments (MonoReflectionMethodHandle ref_method, MonoError *error)
{
	error_init (error);
	MonoMethod *method = MONO_HANDLE_GETVAL (ref_method, method);

	if (method->is_inflated) {
		MonoGenericInst *inst = mono_method_get_context (method)->method_inst;

		if (inst) {
			int count = inst->type_argc;
			MonoArrayHandle res = mono_array_new_handle (mono_defaults.systemtype_class, count, error);
			return_val_if_nok (error, NULL_HANDLE_ARRAY);

			for (int i = 0; i < count; i++) {
				if (!set_array_generic_argument_handle_inflated (inst, i, res, error))
					break;
			}
			return_val_if_nok (error, NULL_HANDLE_ARRAY);
			return res;
		}
	}

	int count = mono_method_signature_internal (method)->generic_param_count;
	MonoArrayHandle res = mono_array_new_handle (mono_defaults.systemtype_class, count, error);
	return_val_if_nok (error, NULL_HANDLE_ARRAY);

	MonoGenericContainer *container = mono_method_get_generic_container (method);
	for (int i = 0; i < count; i++) {
		if (!set_array_generic_argument_handle_gparam (container, i, res, error))
			break;
	}
	return_val_if_nok (error, NULL_HANDLE_ARRAY);
	return res;
}

MonoObjectHandle
ves_icall_InternalInvoke (MonoReflectionMethodHandle method_handle, MonoObjectHandle this_arg_handle,
			  MonoArrayHandle params_handle, MonoExceptionHandleOut exception_out, MonoError *error)
{
	MonoReflectionMethod* const method = MONO_HANDLE_RAW (method_handle);
	MonoObject* const this_arg = MONO_HANDLE_RAW (this_arg_handle);
	MonoArray* const params = MONO_HANDLE_RAW (params_handle);

	/* 
	 * Invoke from reflection is supposed to always be a virtual call (the API
	 * is stupid), mono_runtime_invoke_*() calls the provided method, allowing
	 * greater flexibility.
	 */
	MonoMethod *m = method->method;
	MonoMethodSignature* const sig = mono_method_signature_internal (m);
	MonoImage *image = NULL;
	int pcount = 0;
	void *obj = this_arg;
	char *this_name = NULL;
	char *target_name = NULL;
	char *msg = NULL;
	MonoObject *result = NULL;
	MonoArray *arr = NULL;
	MonoException *exception = NULL;

	*MONO_HANDLE_REF (exception_out) = NULL;

	if (!(m->flags & METHOD_ATTRIBUTE_STATIC)) {
		if (!mono_class_vtable_checked (m->klass, error)) {
			mono_error_cleanup (error); /* FIXME does this make sense? */
			error_init_reuse (error);
			exception = mono_class_get_exception_for_failure (m->klass);
			goto return_null;
		}

		if (this_arg) {
			if (!mono_object_isinst_checked (this_arg, m->klass, error)) {
				if (!is_ok (error)) {
					exception = mono_error_convert_to_exception (error);
					goto return_null;
				}
				this_name = mono_type_get_full_name (mono_object_class (this_arg));
				target_name = mono_type_get_full_name (m->klass);
				msg = g_strdup_printf ("Object of type '%s' doesn't match target type '%s'", this_name, target_name);
				exception = mono_exception_from_name_msg (mono_defaults.corlib, "System.Reflection", "TargetException", msg);
				goto return_null;
			}
			m = mono_object_get_virtual_method_internal (this_arg, m);
			/* must pass the pointer to the value for valuetype methods */
			if (m_class_is_valuetype (m->klass)) {
				obj = mono_object_unbox_internal (this_arg);
				// FIXMEcoop? Does obj need to be put into a handle?
			}
		} else if (strcmp (m->name, ".ctor") && !m->wrapper_type) {
			exception = mono_exception_from_name_msg (mono_defaults.corlib, "System.Reflection", "TargetException", "Non-static method requires a target.");
			goto return_null;
		}
	}

	if ((m->klass != NULL && m_class_is_byreflike (m->klass)) || m_class_is_byreflike (mono_class_from_mono_type_internal (sig->ret))) {
		exception = mono_exception_from_name_msg (mono_defaults.corlib, "System", "NotSupportedException", "Cannot invoke method with stack pointers via reflection");
		goto return_null;
	}

	if (sig->ret->byref) {
		MonoType* ret_byval = m_class_get_byval_arg (mono_class_from_mono_type_internal (sig->ret));
		if (ret_byval->type == MONO_TYPE_VOID) {
			exception = mono_exception_from_name_msg (mono_defaults.corlib, "System", "NotSupportedException", "ByRef to void return values are not supported in reflection invocation");
			goto return_null;
		}	
		if (m_class_is_byreflike (mono_class_from_mono_type_internal (ret_byval))) {
			exception = mono_exception_from_name_msg (mono_defaults.corlib, "System", "NotSupportedException", "Cannot invoke method returning ByRef to ByRefLike type via reflection");
			goto return_null;
		}
	}

	pcount = params? mono_array_length_internal (params): 0;
	if (pcount != sig->param_count) {
		exception = mono_exception_from_name (mono_defaults.corlib, "System.Reflection", "TargetParameterCountException");
		goto return_null;
	}

	if (mono_class_is_abstract (m->klass) && !strcmp (m->name, ".ctor") && !this_arg) {
		exception = mono_exception_from_name_msg (mono_defaults.corlib, "System.Reflection", "TargetException", "Cannot invoke constructor of an abstract class.");
		goto return_null;
	}

	image = m_class_get_image (m->klass);
	
	if (m_class_get_rank (m->klass) && !strcmp (m->name, ".ctor")) {
		int i;
		pcount = mono_array_length_internal (params);
		uintptr_t * const lengths = g_newa (uintptr_t, pcount);
		/* Note: the synthetized array .ctors have int32 as argument type */
		for (i = 0; i < pcount; ++i)
			lengths [i] = *(int32_t*) ((char*)mono_array_get_internal (params, gpointer, i) + sizeof (MonoObject));

		if (m_class_get_rank (m->klass) == 1 && sig->param_count == 2 && m_class_get_rank (m_class_get_element_class (m->klass))) {
			/* This is a ctor for jagged arrays. MS creates an array of arrays. */
			arr = mono_array_new_full_checked (m->klass, lengths, NULL, error);
			goto_if_nok (error, return_null);

			MonoArrayHandle subarray_handle = MONO_HANDLE_NEW (MonoArray, NULL);

			for (i = 0; i < mono_array_length_internal (arr); ++i) {
				MonoArray *subarray = mono_array_new_full_checked (m_class_get_element_class (m->klass), &lengths [1], NULL, error);
				goto_if_nok (error, return_null);
				MONO_HANDLE_ASSIGN_RAW (subarray_handle, subarray); // FIXME? Overkill?
				mono_array_setref_fast (arr, i, subarray);
			}
			goto exit;
		}

		if (m_class_get_rank (m->klass) == pcount) {
			/* Only lengths provided. */
			arr = mono_array_new_full_checked (m->klass, lengths, NULL, error);
			goto_if_nok (error, return_null);
			goto exit;
		} else {
			g_assert (pcount == (m_class_get_rank (m->klass) * 2));
			/* The arguments are lower-bound-length pairs */
			intptr_t * const lower_bounds = (intptr_t *)g_alloca (sizeof (intptr_t) * pcount);

			for (i = 0; i < pcount / 2; ++i) {
				lower_bounds [i] = *(int32_t*) ((char*)mono_array_get_internal (params, gpointer, (i * 2)) + sizeof (MonoObject));
				lengths [i] = *(int32_t*) ((char*)mono_array_get_internal (params, gpointer, (i * 2) + 1) + sizeof (MonoObject));
			}

			arr = mono_array_new_full_checked (m->klass, lengths, lower_bounds, error);
			goto_if_nok (error, return_null);
			goto exit;
		}
	}
	result = mono_runtime_invoke_array_checked (m, obj, params, error);
	goto exit;
return_null:
	result = NULL;
	arr = NULL;
exit:
	if (exception) {
		MONO_HANDLE_NEW (MonoException, exception); // FIXME? overkill?
		mono_gc_wbarrier_generic_store_internal (MONO_HANDLE_REF (exception_out), (MonoObject*)exception);
	}
	g_free (target_name);
	g_free (this_name);
	g_free (msg);
	g_assert (!result || !arr); // only one, or neither, should be set
	return result ? MONO_HANDLE_NEW (MonoObject, result) : arr ? MONO_HANDLE_NEW (MonoObject, (MonoObject*)arr) : NULL_HANDLE;
}

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
	case MONO_TYPE_R4:
		return read32 (mem);
	case MONO_TYPE_I4:
		return (gint32) read32 (mem);
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
	case MONO_TYPE_R8:
		return read64 (mem);
	case MONO_TYPE_U:
	case MONO_TYPE_I:
#if SIZEOF_REGISTER == 8
		return read64 (mem);
#else
		return read32 (mem);
#endif
	default:
		g_assert_not_reached ();
	}
	return 0;
}

static void
write_enum_value (void *mem, int type, guint64 value)
{
	switch (type) {
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
	case MONO_TYPE_BOOLEAN: {
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
	case MONO_TYPE_I4:
	case MONO_TYPE_R4: {
		guint32 *p = (guint32 *)mem;
		*p = value;
		break;
	}
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
	case MONO_TYPE_R8: {
		guint64 *p = (guint64 *)mem;
		*p = value;
		break;
	}
	case MONO_TYPE_U:
	case MONO_TYPE_I: {
#if SIZEOF_REGISTER == 8
		guint64 *p = (guint64 *)mem;
		*p = value;
#else
		guint32 *p = (guint32 *)mem;
		*p = value;
		break;
#endif
		break;
	}
	default:
		g_assert_not_reached ();
	}
	return;
}

MonoObjectHandle
ves_icall_System_Enum_ToObject (MonoReflectionTypeHandle enumType, guint64 value, MonoError *error)
{
	MonoClass *enumc;
	MonoObjectHandle resultHandle;
	MonoType *etype;

	enumc = mono_class_from_mono_type_internal (MONO_HANDLE_GETVAL (enumType, type));

	mono_class_init_checked (enumc, error);
	goto_if_nok (error, return_null);

	etype = mono_class_enum_basetype_internal (enumc);

	resultHandle = mono_object_new_handle (enumc, error);
	goto_if_nok (error, return_null);

	write_enum_value (mono_handle_unbox_unsafe (resultHandle), etype->type, value);

	return resultHandle;

return_null:
	return MONO_HANDLE_NEW (MonoObject, NULL);
}

MonoBoolean
ves_icall_System_Enum_InternalHasFlag (MonoObjectHandle a, MonoObjectHandle b, MonoError *error)
{
	int size = mono_class_value_size (mono_handle_class (a), NULL);
	guint64 a_val = 0, b_val = 0;

	memcpy (&a_val, mono_handle_unbox_unsafe (a), size);
	memcpy (&b_val, mono_handle_unbox_unsafe (b), size);

	return (a_val & b_val) == b_val;
}

MonoReflectionTypeHandle
ves_icall_System_Enum_get_underlying_type (MonoReflectionTypeHandle type, MonoError *error)
{
	MonoType *etype;
	MonoClass *klass;

	klass = mono_class_from_mono_type_internal (MONO_HANDLE_GETVAL (type, type));
	mono_class_init_checked (klass, error);
	goto_if_nok (error, return_null);

	etype = mono_class_enum_basetype_internal (klass);
	if (!etype) {
		mono_error_set_argument (error, "enumType", "Type provided must be an Enum.");
		goto return_null;
	}

	return mono_type_get_object_handle (etype, error);

return_null:
	return MONO_HANDLE_NEW (MonoReflectionType, NULL);
}

int
ves_icall_System_Enum_InternalGetCorElementType (MonoObjectHandle this_handle, MonoError *error)
{
	MonoClass *klass = MONO_HANDLE_GETVAL (this_handle, vtable)->klass;

	return (int)m_class_get_byval_arg (m_class_get_element_class (klass))->type;
}

static void
get_enum_field (MonoArrayHandle names, MonoArrayHandle values, int base_type, MonoClassField *field, guint* j, guint64 *previous_value, gboolean *sorted, MonoError *error)
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
	MonoStringHandle name;
	name = mono_string_new_handle (mono_field_get_name (field), error);
	goto_if_nok (error, leave);
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

MonoBoolean
ves_icall_System_Enum_GetEnumValuesAndNames (MonoReflectionTypeHandle type, MonoArrayHandleOut values, MonoArrayHandleOut names, MonoError *error)
{
	MonoClass *enumc = mono_class_from_mono_type_internal (MONO_HANDLE_RAW(type)->type);
	guint j = 0, nvalues;
	gpointer iter;
	MonoClassField *field;
	int base_type;
	guint64 previous_value = 0;
	gboolean sorted = TRUE;

	error_init (error);
	mono_class_init_checked (enumc, error);
	return_val_if_nok (error, FALSE);

	if (!m_class_is_enumtype (enumc)) {
		mono_error_set_argument (error, NULL, "Type provided must be an Enum.");
		return TRUE;
	}

	base_type = mono_class_enum_basetype_internal (enumc)->type;

	nvalues = mono_class_num_fields (enumc) > 0 ? mono_class_num_fields (enumc) - 1 : 0;
	MONO_HANDLE_ASSIGN(names, mono_array_new_handle (mono_defaults.string_class, nvalues, error));
	return_val_if_nok (error, FALSE);
	MONO_HANDLE_ASSIGN(values, mono_array_new_handle (mono_defaults.uint64_class, nvalues, error));
	return_val_if_nok (error, FALSE);

	iter = NULL;
	while ((field = mono_class_get_fields_internal (enumc, &iter))) {
		get_enum_field (names, values, base_type, field, &j, &previous_value, &sorted, error);
		if (!is_ok (error))
			break;
	}
	return_val_if_nok (error, FALSE);

	return sorted || base_type == MONO_TYPE_R4 || base_type == MONO_TYPE_R8;
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

enum {
	MLISTTYPE_All = 0,
	MLISTTYPE_CaseSensitive = 1,
	MLISTTYPE_CaseInsensitive = 2,
	MLISTTYPE_HandleToInfo = 3
};

GPtrArray*
ves_icall_RuntimeType_GetFields_native (MonoReflectionTypeHandle ref_type, char *utf8_name, guint32 bflags, guint32 mlisttype, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	if (type->byref) {
		return g_ptr_array_new ();
	}

	int (*compare_func) (const char *s1, const char *s2) = NULL;	
	compare_func = ((bflags & BFLAGS_IgnoreCase) || (mlisttype == MLISTTYPE_CaseInsensitive)) ? mono_utf8_strcasecmp : strcmp;

	MonoClass *startklass, *klass;
	klass = startklass = mono_class_from_mono_type_internal (type);

	GPtrArray *ptr_array = g_ptr_array_sized_new (16);
	
handle_parent:	
	if (mono_class_has_failure (klass)) {
		mono_error_set_for_class_failure (error, klass);
		goto fail;
	}

	MonoClassField *field;
	gpointer iter;
	iter = NULL;
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

		if (((mlisttype != MLISTTYPE_All) && (utf8_name != NULL)) && compare_func (mono_field_get_name (field), utf8_name))
				continue;

		g_ptr_array_add (ptr_array, field);
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = m_class_get_parent (klass)))
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
			return TRUE;
		case METHOD_ATTRIBUTE_PRIVATE:
			return start_klass;
		case METHOD_ATTRIBUTE_PUBLIC:
			return FALSE;
		default:
			return TRUE;
	}
}

GPtrArray*
mono_class_get_methods_by_name (MonoClass *klass, const char *name, guint32 bflags, guint32 mlisttype, gboolean allow_ctors, MonoError *error)
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
	
	compare_func = ((bflags & BFLAGS_IgnoreCase) || (mlisttype == MLISTTYPE_CaseInsensitive)) ? mono_utf8_strcasecmp : strcmp;

	/* An optimization for calls made from Delegate:CreateDelegate () */
	if (m_class_is_delegate (klass) && klass != mono_defaults.delegate_class && klass != mono_defaults.multicastdelegate_class && name && !strcmp (name, "Invoke") && (bflags == (BFLAGS_Public | BFLAGS_Static | BFLAGS_Instance))) {
		method = mono_get_delegate_invoke_internal (klass);
		g_assert (method);

		g_ptr_array_add (array, method);
		return array;
	}

	mono_class_setup_methods (klass);
	mono_class_setup_vtable (klass);
	if (mono_class_has_failure (klass))
		goto loader_error;

	if (is_generic_parameter (m_class_get_byval_arg (klass)))
		nslots = mono_class_get_vtable_size (m_class_get_parent (klass));
	else
		nslots = MONO_CLASS_IS_INTERFACE_INTERNAL (klass) ? mono_class_num_methods (klass) : mono_class_get_vtable_size (klass);
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

		if ((mlisttype != MLISTTYPE_All) && (name != NULL)) {
			if (compare_func (name, method->name))
				continue;
		}
		
		match = 0;
		g_ptr_array_add (array, method);
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = m_class_get_parent (klass)))
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

GPtrArray*
ves_icall_RuntimeType_GetMethodsByName_native (MonoReflectionTypeHandle ref_type, const char *mname, guint32 bflags, guint32 mlisttype, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	MonoClass *klass = mono_class_from_mono_type_internal (type);
	if (type->byref) {
		return g_ptr_array_new ();
	}

	return mono_class_get_methods_by_name (klass, mname, bflags, mlisttype, FALSE, error);
}

GPtrArray*
ves_icall_RuntimeType_GetConstructors_native (MonoReflectionTypeHandle ref_type, guint32 bflags, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	if (type->byref) {
		return g_ptr_array_new ();
	}

	MonoClass *startklass, *klass;
	klass = startklass = mono_class_from_mono_type_internal (type);

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

	return mono_metadata_signature_equal (mono_method_signature_internal (method1), mono_method_signature_internal (method2));
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

GPtrArray*
ves_icall_RuntimeType_GetPropertiesByName_native (MonoReflectionTypeHandle ref_type, gchar *propname, guint32 bflags, guint32 mlisttype, MonoError *error)
{
	// Fetch non-public properties as well because they can hide public properties with the same name in base classes
	bflags |= BFLAGS_NonPublic;
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	if (type->byref) {
		return g_ptr_array_new ();
	}

	
	MonoClass *startklass, *klass;
	klass = startklass = mono_class_from_mono_type_internal (type);

	int (*compare_func) (const char *s1, const char *s2) = (mlisttype == MLISTTYPE_CaseInsensitive) ? mono_utf8_strcasecmp : strcmp;

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
	gpointer iter;
	iter = NULL;
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

		if ((mlisttype != MLISTTYPE_All) && (propname != NULL) && compare_func (propname, prop->name))
			continue;
		
		if (g_hash_table_lookup (properties, prop))
			continue;

		g_ptr_array_add (res_array, prop);
		
		g_hash_table_insert (properties, prop, prop);
	}
	if (!(bflags & BFLAGS_DeclaredOnly) && (klass = m_class_get_parent (klass))) {
		// BFLAGS_NonPublic should be excluded for base classes
		bflags &= ~BFLAGS_NonPublic;
		goto handle_parent;
	}

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

GPtrArray*
ves_icall_RuntimeType_GetEvents_native (MonoReflectionTypeHandle ref_type, char *utf8_name, guint32 mlisttype, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	if (type->byref) {
		return g_ptr_array_new ();
	}

	int (*compare_func) (const char *s1, const char *s2) = (mlisttype == MLISTTYPE_CaseInsensitive) ? mono_utf8_strcasecmp : strcmp;

	GPtrArray *res_array = g_ptr_array_sized_new (4);

	MonoClass *startklass, *klass;
	klass = startklass = mono_class_from_mono_type_internal (type);

	GHashTable *events = g_hash_table_new (event_hash, (GEqualFunc)event_equal);
handle_parent:
	mono_class_setup_methods (klass);
	mono_class_setup_vtable (klass);
	if (mono_class_has_failure (klass)) {
		mono_error_set_for_class_failure (error, klass);
		goto failure;
	}

	MonoEvent *event;
	gpointer iter;
	iter = NULL;
	while ((event = mono_class_get_events (klass, &iter))) {

		// Remove inherited privates and inherited
		// without add/remove/raise methods
		if (klass != startklass)
		{
			MonoMethod *method = event->add;
			if (!method)
				method = event->remove;
			if (!method)
				method = event->raise;
			if (!method)
				continue;
			if ((method->flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) == METHOD_ATTRIBUTE_PRIVATE)
				continue;
		}

		if ((mlisttype != MLISTTYPE_All) && (utf8_name != NULL) && compare_func (event->name, utf8_name))
			continue;

		if (g_hash_table_lookup (events, event))
			continue;

		g_ptr_array_add (res_array, event); 

		g_hash_table_insert (events, event, event);
	}
	if ((klass = m_class_get_parent (klass)))
		goto handle_parent;

	g_hash_table_destroy (events);

	return res_array;

failure:
	if (events != NULL)
		g_hash_table_destroy (events);

	g_ptr_array_free (res_array, TRUE);

	return NULL;
}

GPtrArray *
ves_icall_RuntimeType_GetNestedTypes_native (MonoReflectionTypeHandle ref_type, char *str, guint32 bflags, guint32 mlisttype, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	if (type->byref) {
		return g_ptr_array_new ();
	}

	int (*compare_func) (const char *s1, const char *s2) = ((bflags & BFLAGS_IgnoreCase) || (mlisttype == MLISTTYPE_CaseInsensitive)) ? mono_utf8_strcasecmp : strcmp;

	MonoClass *klass = mono_class_from_mono_type_internal (type);

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

		if ((mlisttype != MLISTTYPE_All) && (str != NULL) && compare_func (m_class_get_name (nested), str))
				continue;

		g_ptr_array_add (res_array, m_class_get_byval_arg (nested));
	}

	return res_array;
}

static MonoType*
get_type_from_module_builder_module (MonoAssemblyLoadContext *alc, MonoArrayHandle modules, int i, MonoTypeNameParse *info, MonoBoolean ignoreCase, gboolean *type_resolve, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	MonoType *type = NULL;
	MonoReflectionModuleBuilderHandle mb = MONO_HANDLE_NEW (MonoReflectionModuleBuilder, NULL);
	MONO_HANDLE_ARRAY_GETREF (mb, modules, i);
	MonoDynamicImage *dynamic_image = MONO_HANDLE_GETVAL (mb, dynamic_image);
	type = mono_reflection_get_type_checked (alc, &dynamic_image->image, &dynamic_image->image, info, ignoreCase, FALSE, type_resolve, error);
	HANDLE_FUNCTION_RETURN_VAL (type);
}

static MonoType*
get_type_from_module_builder_loaded_modules (MonoAssemblyLoadContext *alc, MonoArrayHandle loaded_modules, int i, MonoTypeNameParse *info, MonoBoolean ignoreCase, gboolean *type_resolve, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	MonoType *type = NULL;
	MonoReflectionModuleHandle mod = MONO_HANDLE_NEW (MonoReflectionModule, NULL);
	MONO_HANDLE_ARRAY_GETREF (mod, loaded_modules, i);
	MonoImage *image = MONO_HANDLE_GETVAL (mod, image);
	type = mono_reflection_get_type_checked (alc, image, image, info, ignoreCase, FALSE, type_resolve, error);
	HANDLE_FUNCTION_RETURN_VAL (type);
}

MonoReflectionTypeHandle
ves_icall_System_Reflection_Assembly_InternalGetType (MonoReflectionAssemblyHandle assembly_h, MonoReflectionModuleHandle module, MonoStringHandle name, MonoBoolean throwOnError, MonoBoolean ignoreCase, MonoError *error)
{
	ERROR_DECL (parse_error);

	MonoTypeNameParse info;
	gboolean type_resolve;
	MonoAssemblyLoadContext *alc = mono_alc_get_ambient ();

	/* On MS.NET, this does not fire a TypeResolve event */
	type_resolve = TRUE;
	char *str = mono_string_handle_to_utf8 (name, error);
	goto_if_nok (error, fail);

	/*g_print ("requested type %s in %s\n", str, assembly->assembly->aname.name);*/
	if (!mono_reflection_parse_type_checked (str, &info, parse_error)) {
		g_free (str);
		mono_reflection_free_type_info (&info);
		mono_error_cleanup (parse_error);
		if (throwOnError) {
			mono_error_set_argument (error, "typeName@0", "failed to parse the type");
			goto fail;
		}
		/*g_print ("failed parse\n");*/
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
	}

	if (info.assembly.name) {
		g_free (str);
		mono_reflection_free_type_info (&info);
		if (throwOnError) {
			mono_error_set_argument (error, NULL, "Type names passed to Assembly.GetType() must not specify an assembly.");
			goto fail;
		}
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
	}

	MonoType *type;
	type = NULL;
	if (!MONO_HANDLE_IS_NULL (module)) {
		MonoImage *image = MONO_HANDLE_GETVAL (module, image);
		if (image) {
			type = mono_reflection_get_type_checked (alc, image, image, &info, ignoreCase, FALSE, &type_resolve, error);
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
					type = get_type_from_module_builder_module (alc, modules, i, &info, ignoreCase, &type_resolve, error);
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
					type = get_type_from_module_builder_loaded_modules (alc, loaded_modules, i, &info, ignoreCase, &type_resolve, error);

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
			type = mono_reflection_get_type_checked (alc, assembly->image, assembly->image, &info, ignoreCase, FALSE, &type_resolve, error);
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
			ERROR_DECL (inner_error);
			char *type_name = mono_string_handle_to_utf8 (name, inner_error);
			mono_error_assert_ok (inner_error);
			MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_h, assembly);
			char *assmname = mono_stringify_assembly_name (&assembly->aname);
			mono_error_set_type_load_name (error, type_name, assmname, "%s", "");
			goto fail;
		}

		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
	}

	if (type->type == MONO_TYPE_CLASS) {
		MonoClass *klass = mono_type_get_class_internal (type);

		/* need to report exceptions ? */
		if (throwOnError && mono_class_has_failure (klass)) {
			/* report SecurityException (or others) that occured when loading the assembly */
			mono_error_set_for_class_failure (error, klass);
			goto fail;
		}
	}

	/* g_print ("got it\n"); */
	return mono_type_get_object_handle (type, error);
fail:
	g_assert (!is_ok (error));
	return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
}

MonoStringHandle
ves_icall_System_Reflection_RuntimeAssembly_get_code_base (MonoReflectionAssemblyHandle assembly, MonoError *error)
{
	MonoAssembly *mass = MONO_HANDLE_GETVAL (assembly, assembly);
	
	/* return NULL for bundled assemblies in single-file scenarios */
	const char* filename = m_image_get_filename (mass->image);

	if (!filename)
		return NULL_HANDLE_STRING;

	gchar *absolute;
	if (g_path_is_absolute (filename)) {
		absolute = g_strdup (filename);
	} else {
		absolute = g_build_filename (mass->basedir, filename, (const char*)NULL);
	}

	mono_icall_make_platform_path (absolute);

	const gchar *prepend = mono_icall_get_file_path_prefix (absolute);
	gchar *uri = g_strconcat (prepend, absolute, (const char*)NULL);

	g_free (absolute);

	MonoStringHandle res;
	if (uri) {
		res = mono_string_new_handle (uri, error);
		g_free (uri);
	} else {
		res = MONO_HANDLE_NEW (MonoString, NULL);
	}
	return res;
}

MonoStringHandle
ves_icall_System_Reflection_RuntimeAssembly_get_location (MonoReflectionAssemblyHandle refassembly, MonoError *error)
{
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (refassembly, assembly);
	const char *image_name = m_image_get_filename (assembly->image);
	return mono_string_new_handle (image_name != NULL ? image_name : "", error);
}

MonoStringHandle
ves_icall_System_Reflection_RuntimeAssembly_InternalImageRuntimeVersion (MonoReflectionAssemblyHandle refassembly, MonoError *error)
{
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (refassembly, assembly);

	return mono_string_new_handle (assembly->image->version, error);
}

MonoReflectionMethodHandle
ves_icall_System_Reflection_RuntimeAssembly_get_EntryPoint (MonoReflectionAssemblyHandle assembly_h, MonoError *error) 
{
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_h, assembly);
	MonoMethod *method;

	MonoReflectionMethodHandle res = MONO_HANDLE_NEW (MonoReflectionMethod, NULL);
	guint32 token = mono_image_get_entry_point (assembly->image);

	if (!token)
		goto leave;
	method = mono_get_method_checked (assembly->image, token, NULL, NULL, error);
	goto_if_nok (error, leave);

	MONO_HANDLE_ASSIGN (res, mono_method_get_object_handle (method, NULL, error));
leave:
	return res;
}

MonoReflectionModuleHandle
ves_icall_System_Reflection_Assembly_GetManifestModuleInternal (MonoReflectionAssemblyHandle assembly, MonoError *error) 
{
	MonoAssembly *a = MONO_HANDLE_GETVAL (assembly, assembly);
	return mono_module_get_object_handle (a->image, error);
}

static gboolean
add_manifest_resource_name_to_array (MonoImage *image, MonoTableInfo *table, int i, MonoArrayHandle dest, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	const char *val = mono_metadata_string_heap (image, mono_metadata_decode_row_col (table, i, MONO_MANIFEST_NAME));
	MonoStringHandle str = mono_string_new_handle (val, error);
	goto_if_nok (error, leave);
	MONO_HANDLE_ARRAY_SETREF (dest, i, str);
leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

MonoArrayHandle
ves_icall_System_Reflection_RuntimeAssembly_GetManifestResourceNames (MonoReflectionAssemblyHandle assembly_h, MonoError *error) 
{
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_h, assembly);
	MonoTableInfo *table = &assembly->image->tables [MONO_TABLE_MANIFESTRESOURCE];
	/* FIXME: metadata-update */
	int rows = table_info_get_rows (table);
	MonoArrayHandle result = mono_array_new_handle (mono_defaults.string_class, rows, error);
	goto_if_nok (error, fail);
	int i;

	for (i = 0; i < rows; ++i) {
		if (!add_manifest_resource_name_to_array (assembly->image, table, i, result, error))
			goto fail;
	}
	return result;
fail:
	return NULL_HANDLE_ARRAY;
}

static MonoAssemblyName*
create_referenced_assembly_name (MonoImage *image, int i, MonoError *error)
{
	MonoAssemblyName *aname = g_new0 (MonoAssemblyName, 1);

	mono_assembly_get_assemblyref_checked (image, i, aname, error);
	return_val_if_nok (error, NULL);
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

GPtrArray*
ves_icall_System_Reflection_Assembly_InternalGetReferencedAssemblies (MonoReflectionAssemblyHandle assembly, MonoError *error) 
{
	error_init (error);
	MonoAssembly *ass = MONO_HANDLE_GETVAL(assembly, assembly);
	MonoImage *image = ass->image;
	int count;

	/* FIXME: metadata-update */

	if (image_is_dynamic (ass->image)) {
		MonoDynamicTable *t = &(((MonoDynamicImage*) image)->tables [MONO_TABLE_ASSEMBLYREF]);
		count = t->rows;
	}
	else {
		MonoTableInfo *t = &image->tables [MONO_TABLE_ASSEMBLYREF];
		count = table_info_get_rows (t);
	}

	GPtrArray *result = g_ptr_array_sized_new (count);

	for (int i = 0; i < count; i++) {
		MonoAssemblyName *aname = create_referenced_assembly_name (image, i, error);
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
		return g_strconcat (dir, G_DIR_SEPARATOR_S, file, (const char*)NULL);
	else
		return g_strconcat (dir, file, (const char*)NULL);
}

static MonoReflectionAssemblyHandle
try_resource_resolve_name (MonoReflectionAssemblyHandle assembly_handle, MonoStringHandle name_handle)
{
	MonoObjectHandle ret;

	ERROR_DECL (error);

	HANDLE_FUNCTION_ENTER ();

	if (mono_runtime_get_no_exec ())
		goto return_null;

	MONO_STATIC_POINTER_INIT (MonoMethod, resolve_method)

		static gboolean inited;
		if (!inited) {
			MonoClass *alc_class = mono_class_get_assembly_load_context_class ();
			g_assert (alc_class);
			resolve_method = mono_class_get_method_from_name_checked (alc_class, "OnResourceResolve", -1, 0, error);
			inited = TRUE;
		}
		mono_error_cleanup (error);
		error_init_reuse (error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, resolve_method)

	if (!resolve_method)
		goto return_null;

	gpointer args [2];
	args [0] = MONO_HANDLE_RAW (assembly_handle);
	args [1] = MONO_HANDLE_RAW (name_handle);
	ret = mono_runtime_try_invoke_handle (resolve_method, NULL_HANDLE, args, error);
	goto_if_nok (error, return_null);

	goto exit;

return_null:
	ret = NULL_HANDLE;

exit:
	HANDLE_FUNCTION_RETURN_REF (MonoReflectionAssembly, MONO_HANDLE_CAST (MonoReflectionAssembly, ret));
}

static void *
get_manifest_resource_internal (MonoReflectionAssemblyHandle assembly_h, MonoStringHandle name, gint32 *size, MonoReflectionModuleHandleOut ref_module, MonoError *error)
{
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_h, assembly);
	MonoTableInfo *table = &assembly->image->tables [MONO_TABLE_MANIFESTRESOURCE];
	guint32 i;
	guint32 cols [MONO_MANIFEST_SIZE];
	guint32 impl, file_idx;
	const char *val;
	MonoImage *module;

	char *n = mono_string_handle_to_utf8 (name, error);
	return_val_if_nok (error, NULL);

	/* FIXME: metadata update */
	int rows = table_info_get_rows (table);
	for (i = 0; i < rows; ++i) {
		mono_metadata_decode_row (table, i, cols, MONO_MANIFEST_SIZE);
		val = mono_metadata_string_heap (assembly->image, cols [MONO_MANIFEST_NAME]);
		if (strcmp (val, n) == 0)
			break;
	}
	g_free (n);
	if (i == rows)
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

	
	MonoReflectionModuleHandle rm = mono_module_get_object_handle (module, error);
	return_val_if_nok (error, NULL);
	MONO_HANDLE_ASSIGN (ref_module, rm);

	return (void*)mono_image_get_resource (module, cols [MONO_MANIFEST_OFFSET], (guint32*)size);
}

void *
ves_icall_System_Reflection_RuntimeAssembly_GetManifestResourceInternal (MonoReflectionAssemblyHandle assembly_h, MonoStringHandle name, gint32 *size, MonoReflectionModuleHandleOut ref_module, MonoError *error) 
{
	gpointer ret = get_manifest_resource_internal (assembly_h, name, size, ref_module, error);

	if (!ret) {
		MonoReflectionAssemblyHandle event_assembly_h = try_resource_resolve_name (assembly_h, name);
		if (MONO_HANDLE_BOOL (event_assembly_h))
			ret = get_manifest_resource_internal (event_assembly_h, name, size, ref_module, error);
	}

	return ret;
}

static gboolean
get_manifest_resource_info_internal (MonoReflectionAssemblyHandle assembly_h, MonoStringHandle name, MonoManifestResourceInfoHandle info, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_h, assembly);
	MonoTableInfo *table = &assembly->image->tables [MONO_TABLE_MANIFESTRESOURCE];
	int i;
	guint32 cols [MONO_MANIFEST_SIZE];
	guint32 file_cols [MONO_FILE_SIZE];
	const char *val;
	char *n;

	gboolean result = FALSE;
	
	n = mono_string_handle_to_utf8 (name, error);
	goto_if_nok (error, leave);

	int rows = table_info_get_rows (table);
	for (i = 0; i < rows; ++i) {
		mono_metadata_decode_row (table, i, cols, MONO_MANIFEST_SIZE);
		val = mono_metadata_string_heap (assembly->image, cols [MONO_MANIFEST_NAME]);
		if (strcmp (val, n) == 0)
			break;
	}
	g_free (n);
	if (i == rows)
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
			MONO_HANDLE_SET (info, filename, mono_string_new_handle (val, error));
			if (file_cols [MONO_FILE_FLAGS] & FILE_CONTAINS_NO_METADATA)
				MONO_HANDLE_SETVAL (info, location, guint32, 0);
			else
				MONO_HANDLE_SETVAL (info, location, guint32, RESOURCE_LOCATION_EMBEDDED);
			break;

		case MONO_IMPLEMENTATION_ASSEMBLYREF:
			i = cols [MONO_MANIFEST_IMPLEMENTATION] >> MONO_IMPLEMENTATION_BITS;
			mono_assembly_load_reference (assembly->image, i - 1);
			if (assembly->image->references [i - 1] == REFERENCE_MISSING) {
				mono_error_set_file_not_found (error, NULL, "Assembly %d referenced from assembly %s not found ", i - 1, assembly->image->name);
				goto leave;
			}
			MonoReflectionAssemblyHandle assm_obj;
			assm_obj = mono_assembly_get_object_handle (assembly->image->references [i - 1], error);
			goto_if_nok (error, leave);
			MONO_HANDLE_SET (info, assembly, assm_obj);

			/* Obtain info recursively */
			get_manifest_resource_info_internal (assm_obj, name, info, error);
			goto_if_nok (error, leave);
			guint32 location;
			location = MONO_HANDLE_GETVAL (info, location);
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

MonoBoolean
ves_icall_System_Reflection_RuntimeAssembly_GetManifestResourceInfoInternal (MonoReflectionAssemblyHandle assembly_h, MonoStringHandle name, MonoManifestResourceInfoHandle info_h, MonoError *error)
{
	return get_manifest_resource_info_internal (assembly_h, name, info_h, error);
}

static gboolean
add_filename_to_files_array (MonoAssembly * assembly, MonoTableInfo *table, int i, MonoArrayHandle dest, int dest_idx, MonoError *error)
{
	HANDLE_FUNCTION_ENTER();
	const char *val = mono_metadata_string_heap (assembly->image, mono_metadata_decode_row_col (table, i, MONO_FILE_NAME));
	char *n = g_concat_dir_and_file (assembly->basedir, val);
	MonoStringHandle str = mono_string_new_handle (n, error);
	g_free (n);
	goto_if_nok (error, leave);
	MONO_HANDLE_ARRAY_SETREF (dest, dest_idx, str);
leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

MonoObjectHandle
ves_icall_System_Reflection_RuntimeAssembly_GetFilesInternal (MonoReflectionAssemblyHandle assembly_h, MonoStringHandle name, MonoBoolean resource_modules, MonoError *error) 
{
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_h, assembly);
	MonoTableInfo *table = &assembly->image->tables [MONO_TABLE_FILE];
	int i, count;

	int rows = table_info_get_rows (table);
	/* check hash if needed */
	if (!MONO_HANDLE_IS_NULL(name)) {
		char *n = mono_string_handle_to_utf8 (name, error);
		goto_if_nok (error, fail);

		for (i = 0; i < rows; ++i) {
			const char *val = mono_metadata_string_heap (assembly->image, mono_metadata_decode_row_col (table, i, MONO_FILE_NAME));
			if (strcmp (val, n) == 0) {
				g_free (n);
				n = g_concat_dir_and_file (assembly->basedir, val);
				MonoStringHandle fn = mono_string_new_handle (n, error);
				g_free (n);
				goto_if_nok (error, fail);
				return MONO_HANDLE_CAST (MonoObject, fn);
			}
		}
		g_free (n);
		return NULL_HANDLE;
	}

	count = 0;
	for (i = 0; i < rows; ++i) {
		if (resource_modules || !(mono_metadata_decode_row_col (table, i, MONO_FILE_FLAGS) & FILE_CONTAINS_NO_METADATA))
			count ++;
	}

	MonoArrayHandle result;
	result = mono_array_new_handle (mono_defaults.string_class, count, error);
	goto_if_nok (error, fail);

	count = 0;
	for (i = 0; i < rows; ++i) {
		if (resource_modules || !(mono_metadata_decode_row_col (table, i, MONO_FILE_FLAGS) & FILE_CONTAINS_NO_METADATA)) {
			if (!add_filename_to_files_array (assembly, table, i, result, count, error))
				goto fail;
			count++;
		}
	}
	return MONO_HANDLE_CAST (MonoObject, result);
fail:
	return NULL_HANDLE;
}

static gboolean
add_module_to_modules_array (MonoArrayHandle dest, int *dest_idx, MonoImage* module, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	if (module) {
		MonoReflectionModuleHandle rm = mono_module_get_object_handle (module, error);
		goto_if_nok (error, leave);
		
		MONO_HANDLE_ARRAY_SETREF (dest, *dest_idx, rm);
		++(*dest_idx);
	}

leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

static gboolean
add_file_to_modules_array (MonoArrayHandle dest, int dest_idx, MonoImage *image, MonoTableInfo *table, int table_idx,  MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	guint32 cols [MONO_FILE_SIZE];
	mono_metadata_decode_row (table, table_idx, cols, MONO_FILE_SIZE);
	if (cols [MONO_FILE_FLAGS] & FILE_CONTAINS_NO_METADATA) {
		MonoReflectionModuleHandle rm = mono_module_file_get_object_handle (image, table_idx, error);
		goto_if_nok (error, leave);
		MONO_HANDLE_ARRAY_SETREF (dest, dest_idx, rm);
	} else {
		MonoImage *m = mono_image_load_file_for_image_checked (image, table_idx + 1, error);
		goto_if_nok (error, leave);
		if (!m) {
			const char *filename = mono_metadata_string_heap (image, cols [MONO_FILE_NAME]);
			mono_error_set_simple_file_not_found (error, filename);
			goto leave;
		}
		MonoReflectionModuleHandle rm = mono_module_get_object_handle (m, error);
		goto_if_nok (error, leave);
		MONO_HANDLE_ARRAY_SETREF (dest, dest_idx, rm);
	}

leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

MonoArrayHandle
ves_icall_System_Reflection_RuntimeAssembly_GetModulesInternal (MonoReflectionAssemblyHandle assembly_h, MonoError *error)
{
	error_init (error);
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
	file_count = table_info_get_rows (table);

	modules = image->modules;
	module_count = image->module_count;

	real_module_count = 0;
	for (i = 0; i < module_count; ++i)
		if (modules [i])
			real_module_count ++;

	klass = mono_class_get_module_class ();
	MonoArrayHandle res = mono_array_new_handle (klass, 1 + real_module_count + file_count, error);
	goto_if_nok (error, fail);

	MonoReflectionModuleHandle image_obj;
	image_obj = mono_module_get_object_handle (image, error);
	goto_if_nok (error, fail);

	MONO_HANDLE_ARRAY_SETREF (res, 0, image_obj);

	j = 1;
	for (i = 0; i < module_count; ++i)
		if (!add_module_to_modules_array (res, &j, modules[i], error))
			goto fail;

	for (i = 0; i < file_count; ++i, ++j) {
		if (!add_file_to_modules_array (res, j, image, table, i, error))
			goto fail;
	}

	return res;
fail:
	return NULL_HANDLE_ARRAY;
}

MonoReflectionMethodHandle
ves_icall_GetCurrentMethod (MonoError *error) 
{
	MonoMethod *m = mono_method_get_last_managed ();

	if (!m) {
		mono_error_set_not_supported (error, "Stack walks are not supported on this platform.");
		return MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE);
	}

	while (m->is_inflated)
		m = ((MonoMethodInflated*)m)->declaring;

	return mono_method_get_object_handle (m, NULL, error);
}

static MonoMethod*
mono_method_get_equivalent_method (MonoMethod *method, MonoClass *klass)
{
	int offset = -1, i;
	if (method->is_inflated && ((MonoMethodInflated*)method)->context.method_inst) {
		ERROR_DECL (error);
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
		result = mono_class_inflate_generic_method_full_checked (inflated->declaring, klass, &ctx, error);
		g_assert (is_ok (error)); /* FIXME don't swallow the error */
		return result;
	}

	mono_class_setup_methods (method->klass);
	if (mono_class_has_failure (method->klass))
		return NULL;
	int mcount = mono_class_get_method_count (method->klass);
	MonoMethod **method_klass_methods = m_class_get_methods (method->klass);
	for (i = 0; i < mcount; ++i) {
		if (method_klass_methods [i] == method) {
			offset = i;
			break;
		}	
	}
	mono_class_setup_methods (klass);
	if (mono_class_has_failure (klass))
		return NULL;
	g_assert (offset >= 0 && offset < mono_class_get_method_count (klass));
	return m_class_get_methods (klass) [offset];
}

MonoReflectionMethodHandle
ves_icall_System_Reflection_RuntimeMethodInfo_GetMethodFromHandleInternalType_native (MonoMethod *method, MonoType *type, MonoBoolean generic_check, MonoError *error)
{
	MonoClass *klass;
	if (type && generic_check) {
		klass = mono_class_from_mono_type_internal (type);
		if (mono_class_get_generic_type_definition (method->klass) != mono_class_get_generic_type_definition (klass))
			return MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE);

		if (method->klass != klass) {
			method = mono_method_get_equivalent_method (method, klass);
			if (!method)
				return MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE);
		}
	} else if (type)
		klass = mono_class_from_mono_type_internal (type);
	else
		klass = method->klass;
	return mono_method_get_object_handle (method, klass, error);
}

MonoReflectionMethodBodyHandle
ves_icall_System_Reflection_RuntimeMethodInfo_GetMethodBodyInternal (MonoMethod *method, MonoError *error)
{
	return mono_method_body_get_object_handle (method, error);
}

MonoReflectionAssemblyHandle
ves_icall_System_Reflection_Assembly_GetExecutingAssembly (MonoStackCrawlMark *stack_mark, MonoError *error)
{
	MonoAssembly *assembly;
	assembly = mono_runtime_get_caller_from_stack_mark (stack_mark);
	g_assert (assembly);
	return mono_assembly_get_object_handle (assembly, error);
}

MonoReflectionAssemblyHandle
ves_icall_System_Reflection_Assembly_GetEntryAssembly (MonoError *error)
{
	MonoAssembly *assembly = mono_runtime_get_entry_assembly ();

	if (!assembly)
		return MONO_HANDLE_CAST (MonoReflectionAssembly, NULL_HANDLE);

	return mono_assembly_get_object_handle (assembly, error);
}

MonoReflectionAssemblyHandle
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
	return mono_assembly_get_object_handle (m_class_get_image (dest->klass)->assembly, error);
}

MonoStringHandle
ves_icall_System_RuntimeType_getFullName (MonoReflectionTypeHandle object, MonoBoolean full_name,
										  MonoBoolean assembly_qualified, MonoError *error)
{
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

	res = mono_string_new_handle (name, error);
	g_free (name);

	return res;
}

MonoStringHandle
ves_icall_System_Reflection_RuntimeAssembly_get_fullname (MonoReflectionAssemblyHandle assembly, MonoError *error)
{
	MonoAssembly *mass = MONO_HANDLE_GETVAL (assembly, assembly);
	gchar *name;

	name = mono_stringify_assembly_name (&mass->aname);
	MonoStringHandle res = mono_string_new_handle (name, error);
	g_free (name);
	return res;
}

MonoAssemblyName *
ves_icall_System_Reflection_AssemblyName_GetNativeName (MonoAssembly *mass)
{
	return &mass->aname;
}

void
ves_icall_System_Reflection_Assembly_InternalGetAssemblyName (MonoStringHandle fname, MonoAssemblyName *name, MonoStringHandleOut normalized_codebase, MonoError *error)
{
	char *filename;
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	char *codebase = NULL;
	gboolean res;
	MonoImage *image;

	error_init (error);

	filename = mono_string_handle_to_utf8 (fname, error);
	return_if_nok (error);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "InternalGetAssemblyName (\"%s\")", filename);

	MonoAssemblyLoadContext *alc = mono_alc_get_default ();
	image = mono_image_open_a_lot (alc, filename, &status, FALSE);

	if (!image){
		if (status == MONO_IMAGE_IMAGE_INVALID)
			mono_error_set_bad_image_by_name (error, filename, "Invalid Image: %s", filename);
		else
			mono_error_set_simple_file_not_found (error, filename);
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

		result = g_strconcat (prepend, codebase, (const char*)NULL);
		g_free (codebase);
		codebase = result;
	}
	MONO_HANDLE_ASSIGN (normalized_codebase, mono_string_new_handle (codebase, error));
	g_free (codebase);

	mono_image_close (image);
	g_free (filename);
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
image_get_type (MonoImage *image, MonoTableInfo *tdef, int table_idx, int count, MonoArrayHandle res, MonoArrayHandle exceptions, MonoBoolean exportedOnly, MonoError *error)
{
	error_init (error);
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (klass_error);
	MonoClass *klass = mono_class_get_checked (image, table_idx | MONO_TOKEN_TYPE_DEF, klass_error);

	if (klass) {
		MonoReflectionTypeHandle rt = mono_type_get_object_handle (m_class_get_byval_arg (klass), error);
		return_if_nok (error);

		MONO_HANDLE_ARRAY_SETREF (res, count, rt);
	} else {
		MonoExceptionHandle ex = mono_error_convert_to_exception_handle (klass_error);
		MONO_HANDLE_ARRAY_SETREF (exceptions, count, ex);
	}
	HANDLE_FUNCTION_RETURN ();
}

static MonoArrayHandle
mono_module_get_types (MonoImage *image, MonoArrayHandleOut exceptions, MonoBoolean exportedOnly, MonoError *error)
{
	/* FIXME: metadata-update */
	MonoTableInfo *tdef = &image->tables [MONO_TABLE_TYPEDEF];
	int rows = table_info_get_rows (tdef);
	int i, count;

	error_init (error);

	/* we start the count from 1 because we skip the special type <Module> */
	if (exportedOnly) {
		count = 0;
		for (i = 1; i < rows; ++i) {
			if (mono_module_type_is_visible (tdef, image, i + 1))
				count++;
		}
	} else {
		count = rows - 1;
	}
	MonoArrayHandle res = mono_array_new_handle (mono_defaults.runtimetype_class, count, error);
	return_val_if_nok (error, NULL_HANDLE_ARRAY);
	MONO_HANDLE_ASSIGN (exceptions,  mono_array_new_handle (mono_defaults.exception_class, count, error));
	return_val_if_nok (error, NULL_HANDLE_ARRAY);
	count = 0;
	for (i = 1; i < rows; ++i) {
		if (!exportedOnly || mono_module_type_is_visible (tdef, image, i+1)) {
			image_get_type (image, tdef, i + 1, count, res, exceptions, exportedOnly, error);
			return_val_if_nok (error, NULL_HANDLE_ARRAY);
			count++;
		}
	}
	
	return res;
}

static void
append_module_types (MonoArrayHandleOut res, MonoArrayHandleOut exceptions, MonoImage *image, MonoBoolean exportedOnly, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoArrayHandle ex2 = MONO_HANDLE_NEW (MonoArray, NULL);
	MonoArrayHandle res2 = mono_module_get_types (image, ex2, exportedOnly, error);
	goto_if_nok (error, leave);

	/* Append the new types to the end of the array */
	if (mono_array_handle_length (res2) > 0) {
		guint32 len1, len2;

		len1 = mono_array_handle_length (res);
		len2 = mono_array_handle_length (res2);

		MonoArrayHandle res3 = mono_array_new_handle (mono_defaults.runtimetype_class, len1 + len2, error);
		goto_if_nok (error, leave);

		mono_array_handle_memcpy_refs (res3, 0, res, 0, len1);
		mono_array_handle_memcpy_refs (res3, len1, res2, 0, len2);
		MONO_HANDLE_ASSIGN (res, res3);

		MonoArrayHandle ex3 = mono_array_new_handle (mono_defaults.runtimetype_class, len1 + len2, error);
		goto_if_nok (error, leave);

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
	ERROR_DECL (unboxed_error);
	mono_error_set_for_class_failure (unboxed_error, klass);

	MonoExceptionHandle exc = MONO_HANDLE_NEW (MonoException, mono_error_convert_to_exception (unboxed_error));
	MONO_HANDLE_ARRAY_SETREF (exl, i, exc);
	HANDLE_FUNCTION_RETURN ();
}

static MonoArrayHandle
assembly_get_types (MonoReflectionAssemblyHandle assembly_handle, MonoBoolean exportedOnly, MonoError *error)
{
	MonoArrayHandle exceptions = MONO_HANDLE_NEW(MonoArray, NULL);
	int i;

	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_handle, assembly);

	g_assert (!assembly_is_dynamic (assembly));
	MonoImage *image = assembly->image;
	MonoTableInfo *table = &image->tables [MONO_TABLE_FILE];
	MonoArrayHandle res = mono_module_get_types (image, exceptions, exportedOnly, error);
	return_val_if_nok (error, NULL_HANDLE_ARRAY);

	/* Append data from all modules in the assembly */
	int rows = table_info_get_rows (table);
	for (i = 0; i < rows; ++i) {
		if (!(mono_metadata_decode_row_col (table, i, MONO_FILE_FLAGS) & FILE_CONTAINS_NO_METADATA)) {
			MonoImage *loaded_image = mono_assembly_load_module_checked (image->assembly, i + 1, error);
			return_val_if_nok (error, NULL_HANDLE_ARRAY);

			if (loaded_image) {
				append_module_types (res, exceptions, loaded_image, exportedOnly, error);
				return_val_if_nok (error, NULL_HANDLE_ARRAY);
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
			MonoClass *klass = mono_type_get_class_internal (MONO_HANDLE_GETVAL (t, type));
			if ((klass != NULL) && mono_class_has_failure (klass)) {
				/* keep the class in the list */
				list = g_list_append (list, klass);
				/* and replace Type with NULL */
				MONO_HANDLE_ARRAY_SETREF (res, i, NULL_HANDLE);
			}
		} else {
			ex_count ++;
		}
	}

	if (list || ex_count) {
		GList *tmp = NULL;
		int j, length = g_list_length (list) + ex_count;

		MonoArrayHandle exl = mono_array_new_handle (mono_defaults.exception_class, length, error);
		if (!is_ok (error)) {
			g_list_free (list);
			return NULL_HANDLE_ARRAY;
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
		return_val_if_nok (error, NULL_HANDLE_ARRAY);
		mono_error_set_exception_handle (error, exc);
		return NULL_HANDLE_ARRAY;
	}
		
	return res;
}

MonoArrayHandle
ves_icall_System_Reflection_RuntimeAssembly_GetExportedTypes (MonoReflectionAssemblyHandle assembly_handle, MonoError *error)
{
	return assembly_get_types (assembly_handle, TRUE, error);
}

static void
get_top_level_forwarded_type (MonoImage *image, MonoTableInfo *table, int i, MonoArrayHandle types, MonoArrayHandle exceptions, int *aindex, int *exception_count)
{
	ERROR_DECL (local_error);
	guint32 cols [MONO_EXP_TYPE_SIZE];
	MonoClass *klass;
	MonoReflectionTypeHandle rt;
	
	mono_metadata_decode_row (table, i, cols, MONO_EXP_TYPE_SIZE);
	if (!(cols [MONO_EXP_TYPE_FLAGS] & TYPE_ATTRIBUTE_FORWARDER))
		return;
	guint32 impl = cols [MONO_EXP_TYPE_IMPLEMENTATION];
	const char *name = mono_metadata_string_heap (image, cols [MONO_EXP_TYPE_NAME]);
	const char *nspace = mono_metadata_string_heap (image, cols [MONO_EXP_TYPE_NAMESPACE]);

	g_assert ((impl & MONO_IMPLEMENTATION_MASK) == MONO_IMPLEMENTATION_ASSEMBLYREF);
	guint32 assembly_idx = impl >> MONO_IMPLEMENTATION_BITS;

	mono_assembly_load_reference (image, assembly_idx - 1);
	g_assert (image->references [assembly_idx - 1]);

	HANDLE_FUNCTION_ENTER ();

	if (image->references [assembly_idx - 1] == REFERENCE_MISSING) {
		MonoExceptionHandle ex = MONO_HANDLE_NEW (MonoException, mono_get_exception_bad_image_format ("Invalid image"));
		MONO_HANDLE_ARRAY_SETREF (types, *aindex, NULL_HANDLE);
		MONO_HANDLE_ARRAY_SETREF (exceptions, *aindex, ex);
		(*exception_count)++; (*aindex)++;
		goto exit;
	}
	klass = mono_class_from_name_checked (image->references [assembly_idx - 1]->image, nspace, name, local_error);
	if (!is_ok (local_error)) {
		MonoExceptionHandle ex = mono_error_convert_to_exception_handle (local_error);
		MONO_HANDLE_ARRAY_SETREF (types, *aindex, NULL_HANDLE);
		MONO_HANDLE_ARRAY_SETREF (exceptions, *aindex, ex);
		mono_error_cleanup (local_error);
		(*exception_count)++; (*aindex)++;
		goto exit;
	}
	rt = mono_type_get_object_handle (m_class_get_byval_arg (klass), local_error);
	if (!is_ok (local_error)) {
		MonoExceptionHandle ex = mono_error_convert_to_exception_handle (local_error);
		MONO_HANDLE_ARRAY_SETREF (types, *aindex, NULL_HANDLE);
		MONO_HANDLE_ARRAY_SETREF (exceptions, *aindex, ex);
		mono_error_cleanup (local_error);
		(*exception_count)++; (*aindex)++;
		goto exit;
	}
	MONO_HANDLE_ARRAY_SETREF (types, *aindex, rt);
	MONO_HANDLE_ARRAY_SETREF (exceptions, *aindex, NULL_HANDLE);
	(*aindex)++;

exit:
	HANDLE_FUNCTION_RETURN ();
}

MonoArrayHandle
ves_icall_System_Reflection_RuntimeAssembly_GetTopLevelForwardedTypes (MonoReflectionAssemblyHandle assembly_h, MonoError *error)
{
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_h, assembly);
	MonoImage *image = assembly->image;
	int count = 0;

	g_assert (!assembly_is_dynamic (assembly));
	MonoTableInfo *table = &image->tables [MONO_TABLE_EXPORTEDTYPE];
	int rows = table_info_get_rows (table);
	for (int i = 0; i < rows; ++i) {
		if (mono_metadata_decode_row_col (table, i, MONO_EXP_TYPE_FLAGS) & TYPE_ATTRIBUTE_FORWARDER)
			count ++;
	}
	
	MonoArrayHandle types = mono_array_new_handle (mono_defaults.runtimetype_class, count, error);
	return_val_if_nok (error, NULL_HANDLE_ARRAY);
	MonoArrayHandle exceptions = mono_array_new_handle (mono_defaults.exception_class, count, error);
	return_val_if_nok (error, NULL_HANDLE_ARRAY);

	int aindex = 0;
	int exception_count = 0;
	for (int i = 0; i < rows; ++i) {
		get_top_level_forwarded_type (image, table, i, types, exceptions, &aindex, &exception_count);
	}

	if (exception_count > 0) {
		MonoExceptionHandle exc = MONO_HANDLE_NEW (MonoException, NULL);
		MONO_HANDLE_ASSIGN (exc, mono_get_exception_reflection_type_load_checked (types, exceptions, error));
		return_val_if_nok (error, NULL_HANDLE_ARRAY);
		mono_error_set_exception_handle (error, exc);
		return NULL_HANDLE_ARRAY;
	}

	return types;
}

void
ves_icall_Mono_RuntimeMarshal_FreeAssemblyName (MonoAssemblyName *aname, MonoBoolean free_struct)
{
	mono_assembly_name_free_internal (aname);
	if (free_struct)
		g_free (aname);
}

void
ves_icall_AssemblyExtensions_ApplyUpdate (MonoAssembly *assm,
					   gconstpointer dmeta_bytes, int32_t dmeta_len,
					   gconstpointer dil_bytes, int32_t dil_len,
					   gconstpointer dpdb_bytes, int32_t dpdb_len)
{
	ERROR_DECL (error);
	g_assert (assm);
	g_assert (dmeta_len >= 0);
	MonoImage *image_base = assm->image;
	g_assert (image_base);

#ifndef HOST_WASM
        if (mono_is_debugger_attached ()) {
                mono_error_set_not_supported (error, "Cannot use System.Reflection.Metadata.MetadataUpdater.ApplyChanges while debugger is attached");
                mono_error_set_pending_exception (error);
                return;
        }
#endif

	mono_image_load_enc_delta (MONO_ENC_DELTA_API, image_base, dmeta_bytes, dmeta_len, dil_bytes, dil_len, dpdb_bytes, dpdb_len, error);
	
	mono_error_set_pending_exception (error);
}

gint32 ves_icall_AssemblyExtensions_ApplyUpdateEnabled (gint32 just_component_check)
{
	// if just_component_check is true, we only care whether the hot_reload component is enabled,
	// not whether the environment is appropriately setup to apply updates.
	return mono_metadata_update_available () && (just_component_check || mono_metadata_update_enabled (NULL));
}

MonoBoolean
ves_icall_System_Reflection_AssemblyName_ParseAssemblyName (const char *name, MonoAssemblyName *aname, MonoBoolean *is_version_defined_arg, MonoBoolean *is_token_defined_arg)
{
	gboolean is_version_defined = FALSE;
	gboolean is_token_defined = FALSE;
	gboolean result = FALSE;

	result = mono_assembly_name_parse_full (name, aname, TRUE, &is_version_defined, &is_token_defined);

	*is_version_defined_arg = (MonoBoolean)is_version_defined;
	*is_token_defined_arg = (MonoBoolean)is_token_defined;

	return result;
}

MonoReflectionTypeHandle
ves_icall_System_Reflection_RuntimeModule_GetGlobalType (MonoImage *image, MonoError *error)
{
	MonoClass *klass;

	g_assert (image);

	MonoReflectionTypeHandle ret = MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);

	if (image_is_dynamic (image) && ((MonoDynamicImage*)image)->initial_image)
		/* These images do not have a global type */
		goto leave;

	klass = mono_class_get_checked (image, 1 | MONO_TOKEN_TYPE_DEF, error);
	goto_if_nok (error, leave);

	ret = mono_type_get_object_handle (m_class_get_byval_arg (klass), error);
leave:
	return ret;
}

void
ves_icall_System_Reflection_RuntimeModule_GetGuidInternal (MonoImage *image, MonoArrayHandle guid_h, MonoError *error)
{
	g_assert (mono_array_handle_length (guid_h) == 16);

	if (!image->metadata_only) {
		g_assert (image->heap_guid.data);
		g_assert (image->heap_guid.size >= 16);

		MONO_ENTER_NO_SAFEPOINTS;
		guint8 *data = (guint8*) mono_array_addr_with_size_internal (MONO_HANDLE_RAW (guid_h), 1, 0);
		memcpy (data, (guint8*)image->heap_guid.data, 16);
		MONO_EXIT_NO_SAFEPOINTS;
	} else {
		MONO_ENTER_NO_SAFEPOINTS;
		guint8 *data = (guint8*) mono_array_addr_with_size_internal (MONO_HANDLE_RAW (guid_h), 1, 0);
		memset (data, 0, 16);
		MONO_EXIT_NO_SAFEPOINTS;
	}
}

void
ves_icall_System_Reflection_RuntimeModule_GetPEKind (MonoImage *image, gint32 *pe_kind, gint32 *machine, MonoError *error)
{
	if (image_is_dynamic (image)) {
		MonoDynamicImage *dyn = (MonoDynamicImage*)image;
		*pe_kind = dyn->pe_kind;
		*machine = dyn->machine;
	}
	else {
		*pe_kind = (image->image_info->cli_cli_header.ch_flags & 0x3);
		*machine = image->image_info->cli_header.coff.coff_machine;
	}
}

gint32
ves_icall_System_Reflection_RuntimeModule_GetMDStreamVersion (MonoImage *image, MonoError *error)
{
	return (image->md_version_major << 16) | (image->md_version_minor);
}

MonoArrayHandle
ves_icall_System_Reflection_RuntimeModule_InternalGetTypes (MonoImage *image, MonoError *error)
{
	if (!image) {
		MonoArrayHandle arr = mono_array_new_handle (mono_defaults.runtimetype_class, 0, error);
		return arr;
	} else {
		MonoArrayHandle exceptions = MONO_HANDLE_NEW (MonoArray, NULL);
		MonoArrayHandle res = mono_module_get_types (image, exceptions, FALSE, error);
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
		int idx = mono_metadata_token_index (token);
		if (idx <= 0 || mono_metadata_table_bounds_check (image, MONO_TABLE_MEMBERREF, idx)) {
			return FALSE;
		}

		guint32 cols [MONO_MEMBERREF_SIZE];
		const MonoTableInfo *table = &image->tables [MONO_TABLE_MEMBERREF];
		mono_metadata_decode_row (table, idx - 1, cols, MONO_MEMBERREF_SIZE);
		const char *sig = mono_metadata_blob_heap (image, cols [MONO_MEMBERREF_SIGNATURE]);
		mono_metadata_decode_blob_size (sig, &sig);
		return (*sig != 0x6);
	} else {
		ERROR_DECL (error);
		MonoClass *handle_class;

		if (!mono_lookup_dynamic_token_class (image, token, FALSE, &handle_class, NULL, error)) {
			mono_error_cleanup (error); /* just probing, ignore error */
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
	memset (ginst, 0, MONO_SIZEOF_GENERIC_INST);
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
			ERROR_DECL (inner_error);
			klass = (MonoClass *)mono_lookup_dynamic_token_class (image, token, FALSE, NULL, NULL, inner_error);
			mono_error_cleanup (inner_error);
			result = klass ? m_class_get_byval_arg (klass) : NULL;
			goto leave;
		}

		init_generic_context_from_args_handles (&context, type_args, method_args);
		ERROR_DECL (inner_error);
		klass = (MonoClass *)mono_lookup_dynamic_token_class (image, token, FALSE, NULL, &context, inner_error);
		mono_error_cleanup (inner_error);
		result = klass ? m_class_get_byval_arg (klass) : NULL;
		goto leave;
	}

	if ((index <= 0) || mono_metadata_table_bounds_check (image, table, index)) {
		*resolve_error = ResolveTokenError_OutOfRange;
		goto leave;
	}

	init_generic_context_from_args_handles (&context, type_args, method_args);
	klass = mono_class_get_checked (image, token, error);
	if (klass)
		klass = mono_class_inflate_generic_class_checked (klass, &context, error);
	goto_if_nok (error, leave);

	if (klass)
		result = m_class_get_byval_arg (klass);
leave:
	HANDLE_FUNCTION_RETURN_VAL (result);

}
MonoType*
ves_icall_System_Reflection_RuntimeModule_ResolveTypeToken (MonoImage *image, guint32 token, MonoArrayHandle type_args, MonoArrayHandle method_args, MonoResolveTokenError *resolve_error, MonoError *error)
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
			ERROR_DECL (inner_error);
			method = (MonoMethod *)mono_lookup_dynamic_token_class (image, token, FALSE, NULL, NULL, inner_error);
			mono_error_cleanup (inner_error);
			goto leave;
		}

		if ((table == MONO_TABLE_MEMBERREF) && !(mono_memberref_is_method (image, token))) {
			*resolve_error = ResolveTokenError_BadTable;
			goto leave;
		}

		init_generic_context_from_args_handles (&context, type_args, method_args);
		ERROR_DECL (inner_error);
		method = (MonoMethod *)mono_lookup_dynamic_token_class (image, token, FALSE, NULL, &context, inner_error);
		mono_error_cleanup (inner_error);
		goto leave;
	}

	if ((index <= 0) || mono_metadata_table_bounds_check (image, table, index)) {
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

MonoMethod*
ves_icall_System_Reflection_RuntimeModule_ResolveMethodToken (MonoImage *image, guint32 token, MonoArrayHandle type_args, MonoArrayHandle method_args, MonoResolveTokenError *resolve_error, MonoError *error)
{
	return module_resolve_method_token (image, token, type_args, method_args, resolve_error, error);
}

MonoStringHandle
ves_icall_System_Reflection_RuntimeModule_ResolveStringToken (MonoImage *image, guint32 token, MonoResolveTokenError *resolve_error, MonoError *error)
{
	int index = mono_metadata_token_index (token);

	*resolve_error = ResolveTokenError_Other;

	/* Validate token */
	if (mono_metadata_token_code (token) != MONO_TOKEN_STRING) {
		*resolve_error = ResolveTokenError_BadTable;
		return NULL_HANDLE_STRING;
	}

	if (image_is_dynamic (image)) {
		ERROR_DECL (ignore_inner_error);
		// FIXME ignoring error
		// FIXME Push MONO_HANDLE_NEW to lower layers.
		MonoStringHandle result = MONO_HANDLE_NEW (MonoString, (MonoString*)mono_lookup_dynamic_token_class (image, token, FALSE, NULL, NULL, ignore_inner_error));
		mono_error_cleanup (ignore_inner_error);
		return result;
	}

	if ((index <= 0) || (index >= image->heap_us.size)) {
		*resolve_error = ResolveTokenError_OutOfRange;
		return NULL_HANDLE_STRING;
	}

	/* FIXME: What to do if the index points into the middle of a string ? */
	return mono_ldstr_handle (image, index, error);
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
			ERROR_DECL (inner_error);
			field = (MonoClassField *)mono_lookup_dynamic_token_class (image, token, FALSE, NULL, NULL, inner_error);
			mono_error_cleanup (inner_error);
			goto leave;
		}

		if (mono_memberref_is_method (image, token)) {
			*resolve_error = ResolveTokenError_BadTable;
			goto leave;
		}

		init_generic_context_from_args_handles (&context, type_args, method_args);
		ERROR_DECL (inner_error);
		field = (MonoClassField *)mono_lookup_dynamic_token_class (image, token, FALSE, NULL, &context, inner_error);
		mono_error_cleanup (inner_error);
		goto leave;
	}

	if ((index <= 0) || mono_metadata_table_bounds_check (image, table, index)) {
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

MonoClassField*
ves_icall_System_Reflection_RuntimeModule_ResolveFieldToken (MonoImage *image, guint32 token, MonoArrayHandle type_args, MonoArrayHandle method_args, MonoResolveTokenError *resolve_error, MonoError *error)
{
	return module_resolve_field_token (image, token, type_args, method_args, resolve_error, error);
}

MonoObjectHandle
ves_icall_System_Reflection_RuntimeModule_ResolveMemberToken (MonoImage *image, guint32 token, MonoArrayHandle type_args, MonoArrayHandle method_args, MonoResolveTokenError *error, MonoError *merror)
{
	int table = mono_metadata_token_table (token);

	*error = ResolveTokenError_Other;

	switch (table) {
	case MONO_TABLE_TYPEDEF:
	case MONO_TABLE_TYPEREF:
	case MONO_TABLE_TYPESPEC: {
		MonoType *t = module_resolve_type_token (image, token, type_args, method_args, error, merror);
		if (t) {
			return MONO_HANDLE_CAST (MonoObject, mono_type_get_object_handle (t, merror));
		}
		else
			return NULL_HANDLE;
	}
	case MONO_TABLE_METHOD:
	case MONO_TABLE_METHODSPEC: {
		MonoMethod *m = module_resolve_method_token (image, token, type_args, method_args, error, merror);
		if (m) {
			return MONO_HANDLE_CAST (MonoObject, mono_method_get_object_handle (m, m->klass, merror));
		} else
			return NULL_HANDLE;
	}		
	case MONO_TABLE_FIELD: {
		MonoClassField *f = module_resolve_field_token (image, token, type_args, method_args, error, merror);
		if (f) {
			return MONO_HANDLE_CAST (MonoObject, mono_field_get_object_handle (f->parent, f, merror));
		}
		else
			return NULL_HANDLE;
	}
	case MONO_TABLE_MEMBERREF:
		if (mono_memberref_is_method (image, token)) {
			MonoMethod *m = module_resolve_method_token (image, token, type_args, method_args, error, merror);
			if (m) {
				return MONO_HANDLE_CAST (MonoObject, mono_method_get_object_handle (m, m->klass, merror));
			} else
				return NULL_HANDLE;
		}
		else {
			MonoClassField *f = module_resolve_field_token (image, token, type_args, method_args, error, merror);
			if (f) {
				return MONO_HANDLE_CAST (MonoObject, mono_field_get_object_handle (f->parent, f, merror));
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

MonoArrayHandle
ves_icall_System_Reflection_RuntimeModule_ResolveSignature (MonoImage *image, guint32 token, MonoResolveTokenError *resolve_error, MonoError *error)
{
	int table = mono_metadata_token_table (token);
	int idx = mono_metadata_token_index (token);
	MonoTableInfo *tables = image->tables;
	guint32 sig, len;
	const char *ptr;

	*resolve_error = ResolveTokenError_OutOfRange;

	/* FIXME: Support other tables ? */
	if (table != MONO_TABLE_STANDALONESIG)
		return NULL_HANDLE_ARRAY;

	if (image_is_dynamic (image))
		return NULL_HANDLE_ARRAY;

	if ((idx == 0) || mono_metadata_table_bounds_check (image, MONO_TABLE_STANDALONESIG, idx))
		return NULL_HANDLE_ARRAY;

	sig = mono_metadata_decode_row_col (&tables [MONO_TABLE_STANDALONESIG], idx - 1, 0);

	ptr = mono_metadata_blob_heap (image, sig);
	len = mono_metadata_decode_blob_size (ptr, &ptr);

	MonoArrayHandle res = mono_array_new_handle (mono_defaults.byte_class, len, error);
	return_val_if_nok (error, NULL_HANDLE_ARRAY);

	// FIXME MONO_ENTER_NO_SAFEPOINTS instead of pin/gchandle.

	MonoGCHandle h;
	gpointer array_base = MONO_ARRAY_HANDLE_PIN (res, guint8, 0, &h);
	memcpy (array_base, ptr, len);
	mono_gchandle_free_internal (h);

	return res;
}

static void
check_for_invalid_array_type (MonoType *type, MonoError *error)
{
	gboolean allowed = TRUE;
	char *name;

	error_init (error);

	if (type->byref)
		allowed = FALSE;
	else if (type->type == MONO_TYPE_TYPEDBYREF)
		allowed = FALSE;

	MonoClass *klass = mono_class_from_mono_type_internal (type);

	if (m_class_is_byreflike (klass))
		allowed = FALSE;

	if (allowed)
		return;
	name = mono_type_get_full_name (klass);
	mono_error_set_type_load_name (error, name, g_strdup (""), "");
}

static void
check_for_invalid_byref_or_pointer_type (MonoClass *klass, MonoError *error)
{
	return;
}

MonoReflectionTypeHandle
ves_icall_RuntimeType_make_array_type (MonoReflectionTypeHandle ref_type, int rank, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	check_for_invalid_array_type (type, error);
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE));
	MonoClass *klass = mono_class_from_mono_type_internal (type);

	MonoClass *aklass;
	if (rank == 0) //single dimension array
		aklass = mono_class_create_array (klass, 1);
	else
		aklass = mono_class_create_bounded_array (klass, rank, TRUE);

	if (mono_class_has_failure (aklass)) {
		mono_error_set_for_class_failure (error, aklass);
		return MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE);
	}

	return mono_type_get_object_handle (m_class_get_byval_arg (aklass), error);
}

MonoReflectionTypeHandle
ves_icall_RuntimeType_make_byref_type (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);

	MonoClass *klass = mono_class_from_mono_type_internal (type);
	mono_class_init_checked (klass, error);
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE));

	check_for_invalid_byref_or_pointer_type (klass, error);
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE));

	return mono_type_get_object_handle (m_class_get_this_arg (klass), error);
}

MonoReflectionTypeHandle
ves_icall_RuntimeType_MakePointerType (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type_internal (type);
	mono_class_init_checked (klass, error);
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE));

	check_for_invalid_byref_or_pointer_type (klass, error);
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoReflectionType, NULL_HANDLE));

	MonoClass *pklass = mono_class_create_ptr (type);

	return mono_type_get_object_handle (m_class_get_byval_arg (pklass), error);
}

MonoObjectHandle
ves_icall_System_Delegate_CreateDelegate_internal (MonoReflectionTypeHandle ref_type, MonoObjectHandle target,
						   MonoReflectionMethodHandle info, MonoBoolean throwOnBindFailure, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *delegate_class = mono_class_from_mono_type_internal (type);
	MonoMethod *method = MONO_HANDLE_GETVAL (info, method);
	MonoMethodSignature *sig = mono_method_signature_internal (method);

	mono_class_init_checked (delegate_class, error);
	return_val_if_nok (error, NULL_HANDLE);

	if (!(m_class_get_parent (delegate_class) == mono_defaults.multicastdelegate_class)) {
		/* FIXME improve this exception message */
		mono_error_set_execution_engine (error, "file %s: line %d (%s): assertion failed: (%s)", __FILE__, __LINE__,
						 __func__,
						 "delegate_class->parent == mono_defaults.multicastdelegate_class");
		return NULL_HANDLE;
	}

	if (sig->generic_param_count && method->wrapper_type == MONO_WRAPPER_NONE) {
		if (!method->is_inflated) {
			mono_error_set_argument (error, "method", " Cannot bind to the target method because its signature differs from that of the delegate type");
			return NULL_HANDLE;
		}
	}

	MonoObjectHandle delegate = mono_object_new_handle (delegate_class, error);
	return_val_if_nok (error, NULL_HANDLE);

	if (!method_is_dynamic (method) && (!MONO_HANDLE_IS_NULL (target) && method->flags & METHOD_ATTRIBUTE_VIRTUAL && method->klass != mono_handle_class (target))) {
		method = mono_object_handle_get_virtual_method (target, method, error);
		return_val_if_nok (error, NULL_HANDLE);
	}

	mono_delegate_ctor (delegate, target, NULL, method, error);
	return_val_if_nok (error, NULL_HANDLE);
	return delegate;
}

MonoMulticastDelegateHandle
ves_icall_System_Delegate_AllocDelegateLike_internal (MonoDelegateHandle delegate, MonoError *error)
{
	MonoClass *klass = mono_handle_class (delegate);
	g_assert (mono_class_has_parent (klass, mono_defaults.multicastdelegate_class));

	MonoMulticastDelegateHandle ret = MONO_HANDLE_CAST (MonoMulticastDelegate, mono_object_new_handle (klass, error));
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoMulticastDelegate, NULL_HANDLE));

	MONO_HANDLE_SETVAL (MONO_HANDLE_CAST (MonoDelegate, ret), invoke_impl, gpointer, mono_runtime_create_delegate_trampoline (klass));

	return ret;
}

MonoReflectionMethodHandle
ves_icall_System_Delegate_GetVirtualMethod_internal (MonoDelegateHandle delegate, MonoError *error)
{
	MonoObjectHandle delegate_target = MONO_HANDLE_NEW_GET (MonoObject, delegate, target);
	MonoMethod *m = mono_object_handle_get_virtual_method (delegate_target, MONO_HANDLE_GETVAL (delegate, method), error);
	return_val_if_nok (error, MONO_HANDLE_CAST (MonoReflectionMethod, NULL_HANDLE));
	return mono_method_get_object_handle (m, m->klass, error);
}

/* System.Buffer */

static gint32
mono_array_get_byte_length (MonoArrayHandle array)
{
	int length;

	MonoClass * const klass = mono_handle_class (array);

	// This resembles mono_array_get_length, but adds the loop.

	if (mono_handle_array_has_bounds (array)) {
		length = 1;
		const int klass_rank = m_class_get_rank (klass);
		for (int i = 0; i < klass_rank; ++ i)
			length *= MONO_HANDLE_GETVAL (array, bounds [i].length);
	} else {
		length = mono_array_handle_length (array);
	}

	switch (m_class_get_byval_arg (m_class_get_element_class (klass))->type) {
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

/* System.Environment */

MonoStringHandle
ves_icall_System_Environment_GetEnvironmentVariable_native (const gchar *utf8_name, MonoError *error)
{
	gchar *value;

	if (utf8_name == NULL)
		return NULL_HANDLE_STRING;

	value = g_getenv (utf8_name);

	if (value == 0)
		return NULL_HANDLE_STRING;
	
	MonoStringHandle res = mono_string_new_handle (value, error);
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
G_BEGIN_DECLS
gchar ***_NSGetEnviron(void);
G_END_DECLS
#define environ (*_NSGetEnviron())
#else
static char *mono_environ[1] = { NULL };
#define environ mono_environ
#endif /* defined (TARGET_OSX) */
#else
G_BEGIN_DECLS
extern
char **environ;
G_END_DECLS
#endif
#endif
#endif

MonoArrayHandle
ves_icall_System_Environment_GetCommandLineArgs (MonoError *error)
{
	MonoArrayHandle result = mono_runtime_get_main_args_handle (error);
	return result;
}

#ifdef HOST_WIN32
static MonoArrayHandle
mono_icall_get_environment_variable_names (MonoError *error)
{
	MonoArrayHandle names;
	MonoStringHandle str;
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

	names = mono_array_new_handle (mono_defaults.string_class, n, error);
	return_val_if_nok (error, NULL_HANDLE_ARRAY);

	if (env_strings) {
		n = 0;
		str = MONO_HANDLE_NEW (MonoString, NULL);
		env_string = env_strings;
		while (*env_string != '\0') {
			/* weird case that MS seems to skip */
			if (*env_string != '=') {
				equal_str = wcschr(env_string, '=');
				g_assert(equal_str);
				MonoString *s = mono_string_new_utf16_checked (env_string, (gint32)(equal_str - env_string), error);
				goto_if_nok (error, cleanup);
				MONO_HANDLE_ASSIGN_RAW (str, s);

				mono_array_handle_setref (names, n, str);
				n++;
			}
			while (*env_string != '\0')
				env_string++;
			env_string++;
		}

	}

cleanup:
	if (env_strings)
		FreeEnvironmentStrings (env_strings);
	if (!is_ok (error))
		return NULL_HANDLE_ARRAY;
	return names;
}

#else

static MonoArrayHandle
mono_icall_get_environment_variable_names (MonoError *error)
{
	MonoArrayHandle names;
	MonoStringHandle str;
	gchar **e, **parts;
	int n;

	n = 0;
	for (e = environ; *e != 0; ++ e)
		++ n;

	names = mono_array_new_handle (mono_defaults.string_class, n, error);
	return_val_if_nok (error, NULL_HANDLE_ARRAY);

	str = MONO_HANDLE_NEW (MonoString, NULL);
	n = 0;
	for (e = environ; *e != 0; ++ e) {
		parts = g_strsplit (*e, "=", 2);
		if (*parts != 0) {
			MonoString *s = mono_string_new_checked (*parts, error);
			MONO_HANDLE_ASSIGN_RAW (str, s);
			if (!is_ok (error)) {
				g_strfreev (parts);
				return NULL_HANDLE_ARRAY;
			}
			mono_array_handle_setref (names, n, str);
		}

		g_strfreev (parts);

		++ n;
	}

	return names;
}
#endif /* !HOST_WIN32 */

MonoArrayHandle
ves_icall_System_Environment_GetEnvironmentVariableNames (MonoError *error)
{
	return mono_icall_get_environment_variable_names (error);
}

void
ves_icall_System_Environment_Exit (int result)
{
	mono_environment_exitcode_set (result);

	if (!mono_runtime_try_shutdown ())
		mono_thread_exit ();

	mono_runtime_quit_internal ();

	/* we may need to do some cleanup here... */
	exit (result);
}

void
ves_icall_System_Environment_FailFast (MonoStringHandle message, MonoExceptionHandle exception, MonoStringHandle errorSource, MonoError *error)
{
	if (MONO_HANDLE_IS_NULL (message)) {
		g_warning ("Process terminated.");
	} else {
		char *msg = mono_string_handle_to_utf8 (message, error);
		g_warning ("Process terminated due to \"%s\"", msg);
		g_free (msg);
	}

	if (!MONO_HANDLE_IS_NULL (exception)) {
		mono_print_unhandled_exception_internal ((MonoObject *) MONO_HANDLE_RAW (exception));
	}

	// NOTE: While this does trigger WER on Windows it doesn't quite provide all the
	// information in the error dump that CoreCLR would. On Windows 7+ we should call
	// RaiseFailFastException directly instead of relying on the C runtime doing it
	// for us and pass it as much information as possible. On Windows 8+ we can also
	// use the __fastfail intrinsic.
	abort ();
}

gint32
ves_icall_System_Environment_get_TickCount (void)
{
	/* this will overflow after ~24 days */
	return (gint32) (mono_msec_boottime () & 0xffffffff);
}

gint64
ves_icall_System_Environment_get_TickCount64 (void)
{
	return mono_msec_boottime ();
}

gpointer
ves_icall_RuntimeMethodHandle_GetFunctionPointer (MonoMethod *method, MonoError *error)
{
	return mono_get_runtime_callbacks ()->get_ftnptr (method, error);
}

MonoBoolean
ves_icall_System_Diagnostics_Debugger_IsAttached_internal (void)
{
	return mono_is_debugger_attached ();
}

MonoBoolean
ves_icall_System_Diagnostics_Debugger_IsLogging (void)
{
	return mono_get_runtime_callbacks ()->debug_log_is_enabled
		&& mono_get_runtime_callbacks ()->debug_log_is_enabled ();
}

void
ves_icall_System_Diagnostics_Debugger_Log (int level, MonoString *volatile* category, MonoString *volatile* message)
{
	if (mono_get_runtime_callbacks ()->debug_log)
		mono_get_runtime_callbacks ()->debug_log (level, *category, *message);
}

/* Only used for value types */
MonoObjectHandle
ves_icall_System_Activator_CreateInstanceInternal (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type_internal (type);
	(void)klass;

	mono_class_init_checked (klass, error);
	return_val_if_nok (error, NULL_HANDLE);

	if (mono_class_is_nullable (klass))
		/* No arguments -> null */
		return NULL_HANDLE;

	return mono_object_new_handle (klass, error);
}

MonoReflectionMethodHandle
ves_icall_RuntimeMethodInfo_get_base_method (MonoReflectionMethodHandle m, MonoBoolean definition, MonoError *error)
{
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
		MonoClass *orig_klass = mono_class_from_mono_type_internal (MONO_HANDLE_GETVAL (orig_reftype, type));
		if (base->klass == orig_klass)
			return m;
	}
	return mono_method_get_object_handle (base, NULL, error);
}

MonoStringHandle
ves_icall_RuntimeMethodInfo_get_name (MonoReflectionMethodHandle m, MonoError *error)
{
	MonoMethod *method = MONO_HANDLE_GETVAL (m, method);

	MonoStringHandle s = mono_string_new_handle (method->name, error);
	return_val_if_nok (error, NULL_HANDLE_STRING);
	MONO_HANDLE_SET (m, name, s);
	return s;
}

void
ves_icall_System_ArgIterator_Setup (MonoArgIterator *iter, char* argsp, char* start)
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

void
ves_icall_System_ArgIterator_IntGetNextArg (MonoArgIterator *iter, MonoTypedRef *res)
{
	guint32 i, arg_size;
	gint32 align;

	i = iter->sig->sentinelpos + iter->next_arg;

	g_assert (i < iter->sig->param_count);

	res->type = iter->sig->params [i];
	res->klass = mono_class_from_mono_type_internal (res->type);
	arg_size = mono_type_stack_size (res->type, &align);
#if defined(__arm__) || defined(__mips__)
	iter->args = (guint8*)(((gsize)iter->args + (align) - 1) & ~(align - 1));
#endif
	res->value = iter->args;
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
	if (arg_size <= sizeof (gpointer)) {
		int dummy;
		int padding = arg_size - mono_type_size (res->type, &dummy);
		res->value = (guint8*)res->value + padding;
	}
#endif
	iter->args = (char*)iter->args + arg_size;
	iter->next_arg++;

	/* g_print ("returning arg %d, type 0x%02x of size %d at %p\n", i, res->type->type, arg_size, res->value); */
}

void
ves_icall_System_ArgIterator_IntGetNextArgWithType (MonoArgIterator *iter, MonoTypedRef *res, MonoType *type)
{
	guint32 i, arg_size;
	gint32 align;

	i = iter->sig->sentinelpos + iter->next_arg;

	g_assert (i < iter->sig->param_count);

	while (i < iter->sig->param_count) {
		if (!mono_metadata_type_equal (type, iter->sig->params [i]))
			continue;
		res->type = iter->sig->params [i];
		res->klass = mono_class_from_mono_type_internal (res->type);
		/* FIXME: endianess issue... */
		arg_size = mono_type_stack_size (res->type, &align);
#if defined(__arm__) || defined(__mips__)
		iter->args = (guint8*)(((gsize)iter->args + (align) - 1) & ~(align - 1));
#endif
		res->value = iter->args;
		iter->args = (char*)iter->args + arg_size;
		iter->next_arg++;
		/* g_print ("returning arg %d, type 0x%02x of size %d at %p\n", i, res.type->type, arg_size, res.value); */
		return;
	}
	/* g_print ("arg type 0x%02x not found\n", res.type->type); */

	memset (res, 0, sizeof (MonoTypedRef));
}

MonoType*
ves_icall_System_ArgIterator_IntGetNextArgType (MonoArgIterator *iter)
{
	gint i;
	
	i = iter->sig->sentinelpos + iter->next_arg;

	g_assert (i < iter->sig->param_count);

	return iter->sig->params [i];
}

MonoObjectHandle
ves_icall_System_TypedReference_ToObject (MonoTypedRef* tref, MonoError *error)
{
	return typed_reference_to_object (tref, error);
}

void
ves_icall_System_TypedReference_InternalMakeTypedReference (MonoTypedRef *res, MonoObjectHandle target, MonoArrayHandle fields, MonoReflectionTypeHandle last_field, MonoError *error)
{
	MonoType *ftype = NULL;
	int i;

	memset (res, 0, sizeof (MonoTypedRef));

	g_assert (mono_array_handle_length (fields) > 0);

	(void)mono_handle_class (target);

	int offset = 0;
	for (i = 0; i < mono_array_handle_length (fields); ++i) {
		MonoClassField *f;
		MONO_HANDLE_ARRAY_GETVAL (f, fields, MonoClassField*, i);

		g_assert (f);

		if (i == 0)
			offset = f->offset;
		else
			offset += f->offset - sizeof (MonoObject);
		(void)mono_class_from_mono_type_internal (f->type);
		ftype = f->type;
	}

	res->type = ftype;
	res->klass = mono_class_from_mono_type_internal (ftype);
	res->value = (guint8*)MONO_HANDLE_RAW (target) + offset;
}

static void
prelink_method (MonoMethod *method, MonoError *error)
{
	error_init (error);
	if (!(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		return;
	mono_lookup_pinvoke_call_internal (method, error);
	/* create the wrapper, too? */
}

void
ves_icall_System_Runtime_InteropServices_Marshal_Prelink (MonoReflectionMethodHandle method, MonoError *error)
{
	error_init (error);

	prelink_method (MONO_HANDLE_GETVAL (method, method), error);
}

int
ves_icall_Interop_Sys_DoubleToString(double value, char *format, char *buffer, int bufferLength)
{
#if defined(TARGET_ARM)
	/* workaround for faulty vcmp.f64 implementation on some 32bit ARM CPUs */
	guint64 bits = *(guint64 *) &value;
	if (bits == 0x1) { /* 4.9406564584124654E-324 */
		g_assert (!strcmp (format, "%.40e"));
		return snprintf (buffer, bufferLength, "%s", "4.9406564584124654417656879286822137236506e-324");
	} else if (bits == 0x4) { /* 2E-323 */
		g_assert (!strcmp (format, "%.40e"));
		return snprintf (buffer, bufferLength, "%s", "1.9762625833649861767062751714728854894602e-323");
	}
#endif

	return snprintf(buffer, bufferLength, format, value);
}

static gboolean
add_modifier_to_array (MonoType *type, MonoArrayHandle dest, int dest_idx, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoClass *klass = mono_class_from_mono_type_internal (type);

	MonoReflectionTypeHandle rt;
	rt = mono_type_get_object_handle (m_class_get_byval_arg (klass), error);
	goto_if_nok (error, leave);

	MONO_HANDLE_ARRAY_SETREF (dest, dest_idx, rt);
leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

/*
 * We return NULL for no modifiers so the corlib code can return Type.EmptyTypes
 * and avoid useless allocations.
 */
static MonoArrayHandle
type_array_from_modifiers (MonoType *type, int optional, MonoError *error)
{
	int i, count = 0;

	int cmod_count = mono_type_custom_modifier_count (type);
	if (cmod_count == 0)
		goto fail;

	error_init (error);
	for (i = 0; i < cmod_count; ++i) {
		gboolean required;
		(void) mono_type_get_custom_modifier (type, i, &required, error);
		goto_if_nok (error, fail);
		if ((optional && !required) || (!optional && required))
			count++;
	}
	if (!count)
		goto fail;

	MonoArrayHandle res;
	res = mono_array_new_handle (mono_defaults.systemtype_class, count, error);
	goto_if_nok (error, fail);
	count = 0;
	for (i = 0; i < cmod_count; ++i) {
		gboolean required;
		MonoType *cmod_type = mono_type_get_custom_modifier (type, i, &required, error);
		goto_if_nok (error, fail);
		if ((optional && !required) || (!optional && required)) {
			if (!add_modifier_to_array (cmod_type, res, count, error))
				goto fail;
			count++;
		}
	}
	return res;
fail:
	return MONO_HANDLE_NEW (MonoArray, NULL);
}

MonoArrayHandle
ves_icall_RuntimeParameterInfo_GetTypeModifiers (MonoReflectionTypeHandle rt, MonoObjectHandle member, int pos, MonoBoolean optional, MonoError *error)
{
	error_init (error);
	MonoType *type = MONO_HANDLE_GETVAL (rt, type);
	MonoClass *member_class = mono_handle_class (member);
	MonoMethod *method = NULL;
	MonoImage *image;
	MonoMethodSignature *sig;

	if (mono_class_is_reflection_method_or_constructor (member_class)) {
		method = MONO_HANDLE_GETVAL (MONO_HANDLE_CAST (MonoReflectionMethod, member), method);
	} else if (m_class_get_image (member_class) == mono_defaults.corlib && !strcmp ("RuntimePropertyInfo", m_class_get_name (member_class))) {
		MonoProperty *prop = MONO_HANDLE_GETVAL (MONO_HANDLE_CAST (MonoReflectionProperty, member), property);
		if (!(method = prop->get))
			method = prop->set;
		g_assert (method);	
	} else if (strcmp (m_class_get_name (member_class), "DynamicMethod") == 0 && strcmp (m_class_get_name_space (member_class), "System.Reflection.Emit") == 0) {
		MonoArrayHandle params = MONO_HANDLE_NEW_GET (MonoArray, MONO_HANDLE_CAST (MonoReflectionDynamicMethod, member), parameters);
		MonoReflectionTypeHandle t = MONO_HANDLE_NEW (MonoReflectionType, NULL);
		MONO_HANDLE_ARRAY_GETREF (t, params, pos);
		type = mono_reflection_type_handle_mono_type (t, error);
		return type_array_from_modifiers (type, optional, error);
	} else {
		char *type_name = mono_type_get_full_name (member_class);
		mono_error_set_not_supported (error, "Custom modifiers on a ParamInfo with member %s are not supported", type_name);
		g_free (type_name);
		return NULL_HANDLE_ARRAY;
	}

	image = m_class_get_image (method->klass);
	sig = mono_method_signature_internal (method);
	if (pos == -1)
		type = sig->ret;
	else
		type = sig->params [pos];

	return type_array_from_modifiers (type, optional, error);
}

static MonoType*
get_property_type (MonoProperty *prop)
{
	MonoMethodSignature *sig;
	if (prop->get) {
		sig = mono_method_signature_internal (prop->get);
		return sig->ret;
	} else if (prop->set) {
		sig = mono_method_signature_internal (prop->set);
		return sig->params [sig->param_count - 1];
	}
	return NULL;
}

MonoArrayHandle
ves_icall_RuntimePropertyInfo_GetTypeModifiers (MonoReflectionPropertyHandle property, MonoBoolean optional, MonoError *error)
{
	error_init (error);
	MonoProperty *prop = MONO_HANDLE_GETVAL (property, property);
	MonoType *type = get_property_type (prop);

	if (!type)
		return NULL_HANDLE_ARRAY;
	return type_array_from_modifiers (type, optional, error);
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
	else if (real_type->type == MONO_TYPE_VALUETYPE && m_class_is_enumtype (real_type->data.klass)) {
		/* For enums, we need to use the base type */
		type->type = MONO_TYPE_VALUETYPE;
		type->data.klass = mono_class_from_mono_type_internal (real_type);
	} else
		type->data.klass = mono_class_from_mono_type_internal (real_type);
}

MonoObjectHandle
ves_icall_property_info_get_default_value (MonoReflectionPropertyHandle property_handle, MonoError* error)
{
	MonoReflectionProperty* property = MONO_HANDLE_RAW (property_handle);

	MonoType blob_type;
	MonoProperty *prop = property->property;
	MonoType *type = get_property_type (prop);
	MonoTypeEnum def_type;
	const char *def_value;

	mono_class_init_internal (prop->parent);

	if (!(prop->attrs & PROPERTY_ATTRIBUTE_HAS_DEFAULT)) {
		mono_error_set_invalid_operation (error, NULL);
		return NULL_HANDLE;
	}

	def_value = mono_class_get_property_default_value (prop, &def_type);

	mono_type_from_blob_type (&blob_type, def_type, type);

	return mono_get_object_from_blob (&blob_type, def_value, MONO_HANDLE_NEW (MonoString, NULL), error);
}

MonoBoolean
ves_icall_MonoCustomAttrs_IsDefinedInternal (MonoObjectHandle obj, MonoReflectionTypeHandle attr_type, MonoError *error)
{
	MonoClass *attr_class = mono_class_from_mono_type_internal (MONO_HANDLE_GETVAL (attr_type, type));

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

MonoArrayHandle
ves_icall_MonoCustomAttrs_GetCustomAttributesInternal (MonoObjectHandle obj, MonoReflectionTypeHandle attr_type, MonoBoolean pseudoattrs, MonoError *error)
{
	MonoClass *attr_class;
	if (MONO_HANDLE_IS_NULL (attr_type))
		attr_class = NULL;
	else
		attr_class = mono_class_from_mono_type_internal (MONO_HANDLE_GETVAL (attr_type, type));

	if (attr_class) {
		mono_class_init_checked (attr_class, error);
		return_val_if_nok (error, NULL_HANDLE_ARRAY);
	}

	return mono_reflection_get_custom_attrs_by_type_handle (obj, attr_class, error);
}

MonoArrayHandle
ves_icall_MonoCustomAttrs_GetCustomAttributesDataInternal (MonoObjectHandle obj, MonoError *error)
{
	return mono_reflection_get_custom_attrs_data_checked (obj, error);
}

#ifndef DISABLE_COM

int
ves_icall_System_Runtime_InteropServices_Marshal_GetHRForException_WinRT(MonoExceptionHandle ex, MonoError *error)
{
	mono_error_set_not_implemented (error, "System.Runtime.InteropServices.Marshal.GetHRForException_WinRT internal call is not implemented.");
	return 0;
}

MonoObjectHandle
ves_icall_System_Runtime_InteropServices_Marshal_GetNativeActivationFactory(MonoObjectHandle type, MonoError *error)
{
	mono_error_set_not_implemented (error, "System.Runtime.InteropServices.Marshal.GetNativeActivationFactory internal call is not implemented.");
	return NULL_HANDLE;
}

void*
ves_icall_System_Runtime_InteropServices_Marshal_GetRawIUnknownForComObjectNoAddRef(MonoObjectHandle obj, MonoError *error)
{
	mono_error_set_not_implemented (error, "System.Runtime.InteropServices.Marshal.GetRawIUnknownForComObjectNoAddRef internal call is not implemented.");
	return NULL;
}

MonoObjectHandle
ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_GetRestrictedErrorInfo(MonoError *error)
{
	mono_error_set_not_implemented (error, "System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.GetRestrictedErrorInfo internal call is not implemented.");
	return NULL_HANDLE;
}

MonoBoolean
ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_RoOriginateLanguageException (int ierr, MonoStringHandle message, void* languageException, MonoError *error)
{
	mono_error_set_not_implemented (error, "System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.RoOriginateLanguageException internal call is not implemented.");
	return FALSE;
}

void
ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_RoReportUnhandledError (MonoObjectHandle oerr, MonoError *error)
{
	mono_error_set_not_implemented (error, "System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.RoReportUnhandledError internal call is not implemented.");
}

int
ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_WindowsCreateString(MonoStringHandle sourceString, int length, void** hstring, MonoError *error)
{
	mono_error_set_not_implemented (error, "System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsCreateString internal call is not implemented.");
	return 0;
}

int
ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_WindowsDeleteString(void* hstring, MonoError *error)
{
	mono_error_set_not_implemented (error, "System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsDeleteString internal call is not implemented.");
	return 0;
}

mono_unichar2*
ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_WindowsGetStringRawBuffer(void* hstring, unsigned* length, MonoError *error)
{
	mono_error_set_not_implemented (error, "System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods.WindowsGetStringRawBuffer internal call is not implemented.");
	return NULL;
}

#endif

static const MonoIcallTableCallbacks *icall_table;
static mono_mutex_t icall_mutex;
static GHashTable *icall_hash = NULL;

typedef struct _MonoIcallHashTableValue {
	gconstpointer method;
	guint32 flags;
} MonoIcallHashTableValue;

void
mono_install_icall_table_callbacks (const MonoIcallTableCallbacks *cb)
{
	g_assert (cb->version == MONO_ICALL_TABLE_CALLBACKS_VERSION);
	icall_table = cb;
}

void
mono_icall_init (void)
{
#ifndef DISABLE_ICALL_TABLES
	mono_icall_table_init ();
#endif
	icall_hash = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, g_free);
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

static void
add_internal_call_with_flags (const char *name, gconstpointer method, guint32 flags)
{
	char *key = g_strdup (name);
	MonoIcallHashTableValue *value = g_new (MonoIcallHashTableValue, 1);
	if (key && value) {
		value->method = method;
		value->flags = flags;

		mono_icall_lock ();
		g_hash_table_insert (icall_hash, key, (gpointer)value);
		mono_icall_unlock ();
	}
}

/**
* mono_dangerous_add_internal_call_coop:
* \param name method specification to surface to the managed world
* \param method pointer to a C method to invoke when the method is called
*
* Similar to \c mono_dangerous_add_raw_internal_call.
*
*/
void
mono_dangerous_add_internal_call_coop (const char *name, gconstpointer method)
{
	add_internal_call_with_flags (name, method, MONO_ICALL_FLAGS_COOPERATIVE);
}

/**
* mono_dangerous_add_internal_call_no_wrapper:
* \param name method specification to surface to the managed world
* \param method pointer to a C method to invoke when the method is called
*
* Similar to \c mono_dangerous_add_raw_internal_call but with more requirements for correct
* operation.
*
* The \p method must NOT:
*
* Run for an unbounded amount of time without calling the mono runtime.
* Additionally, the method must switch to GC Safe mode to perform all blocking
* operations: performing blocking I/O, taking locks, etc. The method can't throw or raise
* exceptions or call other methods that will throw or raise exceptions since the runtime won't
* be able to detect exeptions and unwinder won't be able to correctly find last managed frame in callstack.
* This registration method is for icalls that needs very low overhead and follow all rules in their implementation.
*
*/
void
mono_dangerous_add_internal_call_no_wrapper (const char *name, gconstpointer method)
{
	add_internal_call_with_flags (name, method, MONO_ICALL_FLAGS_NO_WRAPPER);
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
	add_internal_call_with_flags (name, method, MONO_ICALL_FLAGS_FOREIGN);
}

/**
 * mono_dangerous_add_raw_internal_call:
 * \param name method specification to surface to the managed world
 * \param method pointer to a C method to invoke when the method is called
 *
 * Similar to \c mono_add_internal_call but with more requirements for correct
 * operation.
 *
 * A thread running a dangerous raw internal call will avoid a thread state
 * transition on entry and exit, but it must take responsiblity for cooperating
 * with the Mono runtime.
 *
 * The \p method must NOT:
 *
 * Run for an unbounded amount of time without calling the mono runtime.
 * Additionally, the method must switch to GC Safe mode to perform all blocking
 * operations: performing blocking I/O, taking locks, etc.
 *
 */
void
mono_dangerous_add_raw_internal_call (const char *name, gconstpointer method)
{
	add_internal_call_with_flags (name, method, MONO_ICALL_FLAGS_COOPERATIVE);
}

/**
 * mono_add_internal_call_with_flags:
 * \param name method specification to surface to the managed world
 * \param method pointer to a C method to invoke when the method is called
 * \param cooperative if \c TRUE, run icall in GC Unsafe (cooperatively suspended) mode,
 *        otherwise GC Safe (blocking)
 *
 * Like \c mono_add_internal_call, but if \p cooperative is \c TRUE the added
 * icall promises that it will use the coopertive API to inform the runtime
 * when it is running blocking operations, that it will not run for unbounded
 * amounts of time without safepointing, and that it will not hold managed
 * object references across suspend safepoints.
 *
 * If \p cooperative is \c FALSE, run the icall in GC Safe mode - the icall may
 * block. The icall must obey the GC Safe rules, e.g. it must not touch
 * unpinned managed memory.
 *
 */
void
mono_add_internal_call_with_flags (const char *name, gconstpointer method, gboolean cooperative)
{
	add_internal_call_with_flags (name, method, cooperative ? MONO_ICALL_FLAGS_COOPERATIVE : MONO_ICALL_FLAGS_FOREIGN);
}

void
mono_add_internal_call_internal (const char *name, gconstpointer method)
{
	add_internal_call_with_flags (name, method, MONO_ICALL_FLAGS_COOPERATIVE);
}

/* 
 * we should probably export this as an helper (handle nested types).
 * Returns the number of chars written in buf.
 */
static int
concat_class_name (char *buf, int bufsize, MonoClass *klass)
{
	int nspacelen, cnamelen;
	nspacelen = strlen (m_class_get_name_space (klass));
	cnamelen = strlen (m_class_get_name (klass));
	if (nspacelen + cnamelen + 2 > bufsize)
		return 0;
	if (nspacelen) {
		memcpy (buf, m_class_get_name_space (klass), nspacelen);
		buf [nspacelen ++] = '.';
	}
	memcpy (buf + nspacelen, m_class_get_name (klass), cnamelen);
	buf [nspacelen + cnamelen] = 0;
	return nspacelen + cnamelen;
}

static void
no_icall_table (void)
{
	g_assert_not_reached ();
}

gboolean
mono_is_missing_icall_addr (gconstpointer addr)
{
	return addr == NULL || addr == no_icall_table;
}

/*
 * Returns either NULL or no_icall_table for missing icalls.
 */
gconstpointer
mono_lookup_internal_call_full_with_flags (MonoMethod *method, gboolean warn_on_missing, guint32 *flags)
{
	char *sigstart = NULL;
	char *tmpsig = NULL;
	char mname [2048];
	char *classname = NULL;
	int typelen = 0, mlen, siglen;
	gconstpointer res = NULL;
	gboolean locked = FALSE;

	g_assert (method != NULL);

	if (method->is_inflated)
		method = ((MonoMethodInflated *) method)->declaring;

	if (m_class_get_nested_in (method->klass)) {
		int pos = concat_class_name (mname, sizeof (mname)-2, m_class_get_nested_in (method->klass));
		if (!pos)
			goto exit;

		mname [pos++] = '/';
		mname [pos] = 0;

		typelen = concat_class_name (mname+pos, sizeof (mname)-pos-1, method->klass);
		if (!typelen)
			goto exit;

		typelen += pos;
	} else {
		typelen = concat_class_name (mname, sizeof (mname), method->klass);
		if (!typelen)
			goto exit;
	}

	classname = g_strdup (mname);

	mname [typelen] = ':';
	mname [typelen + 1] = ':';

	mlen = strlen (method->name);
	memcpy (mname + typelen + 2, method->name, mlen);
	sigstart = mname + typelen + 2 + mlen;
	*sigstart = 0;

	tmpsig = mono_signature_get_desc (mono_method_signature_internal (method), TRUE);
	siglen = strlen (tmpsig);
	if (typelen + mlen + siglen + 6 > sizeof (mname))
		goto exit;

	sigstart [0] = '(';
	memcpy (sigstart + 1, tmpsig, siglen);
	sigstart [siglen + 1] = ')';
	sigstart [siglen + 2] = 0;

	/* mono_marshal_get_native_wrapper () depends on this */
	if (method->klass == mono_defaults.string_class && !strcmp (method->name, ".ctor")) {
		res = (gconstpointer)ves_icall_System_String_ctor_RedirectToCreateString;
		goto exit;
	}

	mono_icall_lock ();
	locked = TRUE;

	res = g_hash_table_lookup (icall_hash, mname);
	if (res) {
		MonoIcallHashTableValue *value = (MonoIcallHashTableValue *)res;
		if (flags)
			*flags = value->flags;
		res = value->method;
		goto exit;
	}

	/* try without signature */
	*sigstart = 0;
	res = g_hash_table_lookup (icall_hash, mname);
	if (res) {
		MonoIcallHashTableValue *value = (MonoIcallHashTableValue *)res;
		if (flags)
			*flags = value->flags;
		res = value->method;
		goto exit;
	}

	if (!icall_table) {
		/* Fail only when the result is actually used */
		res = (gconstpointer)no_icall_table;
		goto exit;
	} else {
		gboolean uses_handles = FALSE;
		g_assert (icall_table->lookup);
		res = icall_table->lookup (method, classname, sigstart - mlen, sigstart, &uses_handles);
		if (res && flags && uses_handles)
			*flags = *flags | MONO_ICALL_FLAGS_USES_HANDLES;
		mono_icall_unlock ();
		locked = FALSE;

		if (res)
			goto exit;

		if (warn_on_missing) {
			g_warning ("cant resolve internal call to \"%s\" (tested without signature also)", mname);
			g_print ("\nYour mono runtime and class libraries are out of sync.\n");
			g_print ("The out of sync library is: %s\n", m_class_get_image (method->klass)->name);
			g_print ("\nWhen you update one from git you need to update, compile and install\nthe other too.\n");
			g_print ("Do not report this as a bug unless you're sure you have updated correctly:\nyou probably have a broken mono install.\n");
			g_print ("If you see other errors or faults after this message they are probably related\n");
			g_print ("and you need to fix your mono install first.\n");
		}

		res = NULL;
	}

exit:
	if (locked)
		mono_icall_unlock ();
	g_free (classname);
	g_free (tmpsig);
	return res;
}

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
gconstpointer
mono_lookup_internal_call_full (MonoMethod *method, gboolean warn_on_missing, mono_bool *uses_handles, mono_bool *foreign)
{
	if (uses_handles)
		*uses_handles = FALSE;
	if (foreign)
		*foreign = FALSE;

	guint32 flags = MONO_ICALL_FLAGS_NONE;
	gconstpointer addr = mono_lookup_internal_call_full_with_flags (method, warn_on_missing, &flags);

	if (uses_handles && (flags & MONO_ICALL_FLAGS_USES_HANDLES))
		*uses_handles = TRUE;
	if (foreign && (flags & MONO_ICALL_FLAGS_FOREIGN))
		*foreign = TRUE;
	return addr;
}

/**
 * mono_lookup_internal_call:
 */
gpointer
mono_lookup_internal_call (MonoMethod *method)
{
	return (gpointer)mono_lookup_internal_call_full (method, TRUE, NULL, NULL);
}

/*
 * mono_lookup_icall_symbol:
 *
 *   Given the icall METHOD, returns its C symbol.
 */
const char*
mono_lookup_icall_symbol (MonoMethod *m)
{
	if (!icall_table)
		return NULL;

	g_assert (icall_table->lookup_icall_symbol);
	gpointer func;
	func = (gpointer)mono_lookup_internal_call_full (m, FALSE, NULL, NULL);
	if (!func)
		return NULL;
	return icall_table->lookup_icall_symbol (func);
}

#if defined(TARGET_WIN32) && defined(TARGET_X86)
/*
 * Under windows, the default pinvoke calling convention is STDCALL but
 * we need CDECL.
 */
#define MONO_ICALL_SIGNATURE_CALL_CONVENTION MONO_CALL_C
#else
#define MONO_ICALL_SIGNATURE_CALL_CONVENTION 0
#endif

// Storage for these enums is pointer-sized as it gets replaced with MonoType*.
//
// mono_create_icall_signatures depends on this order. Handle with care.
typedef enum ICallSigType {
	ICALL_SIG_TYPE_bool     = 0x00,
	ICALL_SIG_TYPE_boolean  = ICALL_SIG_TYPE_bool,
	ICALL_SIG_TYPE_double   = 0x01,
	ICALL_SIG_TYPE_float    = 0x02,
	ICALL_SIG_TYPE_int      = 0x03,
	ICALL_SIG_TYPE_int16    = 0x04,
	ICALL_SIG_TYPE_int32    = ICALL_SIG_TYPE_int,
	ICALL_SIG_TYPE_int8     = 0x05,
	ICALL_SIG_TYPE_long     = 0x06,
	ICALL_SIG_TYPE_obj      = 0x07,
	ICALL_SIG_TYPE_object   = ICALL_SIG_TYPE_obj,
	ICALL_SIG_TYPE_ptr      = 0x08,
	ICALL_SIG_TYPE_ptrref   = 0x09,
	ICALL_SIG_TYPE_string   = 0x0A,
	ICALL_SIG_TYPE_uint16   = 0x0B,
	ICALL_SIG_TYPE_uint32   = 0x0C,
	ICALL_SIG_TYPE_uint8    = 0x0D,
	ICALL_SIG_TYPE_ulong    = 0x0E,
	ICALL_SIG_TYPE_void     = 0x0F,
	ICALL_SIG_TYPE_sizet    = 0x10
} ICallSigType;

#define ICALL_SIG_TYPES_1(a) 		  	ICALL_SIG_TYPE_ ## a,
#define ICALL_SIG_TYPES_2(a, b) 	  	ICALL_SIG_TYPES_1 (a            ) ICALL_SIG_TYPES_1 (b)
#define ICALL_SIG_TYPES_3(a, b, c) 	  	ICALL_SIG_TYPES_2 (a, b         ) ICALL_SIG_TYPES_1 (c)
#define ICALL_SIG_TYPES_4(a, b, c, d) 	  	ICALL_SIG_TYPES_3 (a, b, c      ) ICALL_SIG_TYPES_1 (d)
#define ICALL_SIG_TYPES_5(a, b, c, d, e)	ICALL_SIG_TYPES_4 (a, b, c, d   ) ICALL_SIG_TYPES_1 (e)
#define ICALL_SIG_TYPES_6(a, b, c, d, e, f)	ICALL_SIG_TYPES_5 (a, b, c, d, e) ICALL_SIG_TYPES_1 (f)
#define ICALL_SIG_TYPES_7(a, b, c, d, e, f, g)	ICALL_SIG_TYPES_6 (a, b, c, d, e, f) ICALL_SIG_TYPES_1 (g)
#define ICALL_SIG_TYPES_8(a, b, c, d, e, f, g, h) ICALL_SIG_TYPES_7 (a, b, c, d, e, f, g) ICALL_SIG_TYPES_1 (h)

#define ICALL_SIG_TYPES(n, types) ICALL_SIG_TYPES_ ## n types

// A scheme to make these const would be nice.
static struct {
#define ICALL_SIG(n, xtypes) 			\
	struct {				\
		MonoMethodSignature sig;	\
		gsize types [n];		\
	} ICALL_SIG_NAME (n, xtypes);
ICALL_SIGS
	MonoMethodSignature end; // terminal zeroed element
} mono_icall_signatures = {
#undef ICALL_SIG
#define ICALL_SIG(n, types) { { \
	0,			/* ret */ \
	n,			/* param_count */ \
	-1,			/* sentinelpos */ \
	0,			/* generic_param_count */ \
	MONO_ICALL_SIGNATURE_CALL_CONVENTION, \
	0,			/* hasthis */ \
	0, 			/* explicit_this */ \
	1, 			/* pinvoke */ \
	0, 			/* is_inflated */ \
	0,			/* has_type_parameters */ \
},  /* possible gap here, depending on MONO_ZERO_LEN_ARRAY */ \
    { ICALL_SIG_TYPES (n, types) } }, /* params and ret */
ICALL_SIGS
};

#undef ICALL_SIG
#define ICALL_SIG(n, types) MonoMethodSignature * const ICALL_SIG_NAME (n, types) = &mono_icall_signatures.ICALL_SIG_NAME (n, types).sig;
ICALL_SIGS
#undef ICALL_SIG

void
mono_create_icall_signatures (void)
{
	// Fixup the mostly statically initialized icall signatures.
	//   x = m_class_get_byval_arg (x)
	//   Initialize ret with params [0] and params [i] with params [i + 1].
	//   ptrref is special
	//
	// FIXME This is a bit obscure.

	typedef MonoMethodSignature G_MAY_ALIAS MonoMethodSignature_a;
	typedef gsize G_MAY_ALIAS gsize_a;

	MonoType * const lookup [ ] = {
		m_class_get_byval_arg (mono_defaults.boolean_class), // ICALL_SIG_TYPE_bool
		m_class_get_byval_arg (mono_defaults.double_class),	 // ICALL_SIG_TYPE_double
		m_class_get_byval_arg (mono_defaults.single_class),  // ICALL_SIG_TYPE_float
		m_class_get_byval_arg (mono_defaults.int32_class),	 // ICALL_SIG_TYPE_int
		m_class_get_byval_arg (mono_defaults.int16_class),	 // ICALL_SIG_TYPE_int16
		m_class_get_byval_arg (mono_defaults.sbyte_class),	 // ICALL_SIG_TYPE_int8
		m_class_get_byval_arg (mono_defaults.int64_class),	 // ICALL_SIG_TYPE_long
		m_class_get_byval_arg (mono_defaults.object_class),	 // ICALL_SIG_TYPE_obj
		m_class_get_byval_arg (mono_defaults.int_class),	 // ICALL_SIG_TYPE_ptr
		mono_class_get_byref_type (mono_defaults.int_class), // ICALL_SIG_TYPE_ptrref
		m_class_get_byval_arg (mono_defaults.string_class),	 // ICALL_SIG_TYPE_string
		m_class_get_byval_arg (mono_defaults.uint16_class),	 // ICALL_SIG_TYPE_uint16
		m_class_get_byval_arg (mono_defaults.uint32_class),	 // ICALL_SIG_TYPE_uint32
		m_class_get_byval_arg (mono_defaults.byte_class),	 // ICALL_SIG_TYPE_uint8
		m_class_get_byval_arg (mono_defaults.uint64_class),	 // ICALL_SIG_TYPE_ulong
		m_class_get_byval_arg (mono_defaults.void_class),	 // ICALL_SIG_TYPE_void
		m_class_get_byval_arg (mono_defaults.int_class),	 // ICALL_SIG_TYPE_sizet
	};

	MonoMethodSignature_a *sig = (MonoMethodSignature*)&mono_icall_signatures;
	int n;
	while ((n = sig->param_count)) {
		--sig->param_count; // remove ret
		gsize_a *types = (gsize_a*)(sig + 1);
		for (int i = 0; i < n; ++i) {
			gsize index = *types++;
			g_assert (index < G_N_ELEMENTS (lookup));
			// Casts on next line are attempt to follow strict aliasing rules,
			// to ensure reading from *types precedes writing
			// to params [].
			*(gsize*)(i ? &sig->params [i - 1] : &sig->ret) = (gsize)lookup [index];
		}
		sig = (MonoMethodSignature*)types;
	}
}

void
mono_register_jit_icall_info (MonoJitICallInfo *info, gconstpointer func, const char *name, MonoMethodSignature *sig, gboolean avoid_wrapper, const char *c_symbol)
{
	// Duplicate initialization is allowed and racy, assuming it is equivalent.

	info->name = name;
	info->func = func;
	info->sig = sig;
	info->c_symbol = c_symbol;

	// Fill in wrapper ahead of time, to just be func, to avoid
	// later initializing it to anything else. So therefore, no wrapper.
	if (avoid_wrapper) {
		info->wrapper = func;
	} else {
		// Leave it alone in case of a race.
	}
}

int
ves_icall_System_GC_GetCollectionCount (int generation)
{
	return mono_gc_collection_count (generation);
}

int
ves_icall_System_GC_GetGeneration (MonoObjectHandle object, MonoError *error)
{
	return mono_gc_get_generation (MONO_HANDLE_RAW (object));
}

int
ves_icall_System_GC_GetMaxGeneration (void)
{
	return mono_gc_max_generation ();
}

gint64
ves_icall_System_GC_GetAllocatedBytesForCurrentThread (void)
{
	return mono_gc_get_allocated_bytes_for_current_thread ();
}

guint64
ves_icall_System_GC_GetTotalAllocatedBytes (MonoBoolean precise, MonoError* error)
{
	return mono_gc_get_total_allocated_bytes (precise);
}

void
ves_icall_System_GC_RecordPressure (gint64 value)
{
	mono_gc_add_memory_pressure (value);
}

MonoBoolean
ves_icall_System_Threading_Thread_YieldInternal (void)
{
	mono_threads_platform_yield ();
	return TRUE;
}

gint32
ves_icall_System_Environment_get_ProcessorCount (void)
{
	return mono_cpu_count ();
}

// Generate wrappers.

#define ICALL_TYPE(id,name,first) /* nothing */
#define ICALL(id,name,func) /* nothing */
#define NOHANDLES(inner)  /* nothing */

#define MONO_HANDLE_REGISTER_ICALL(func, ret, nargs, argtypes) MONO_HANDLE_REGISTER_ICALL_IMPLEMENT (func, ret, nargs, argtypes)

// Some native functions are exposed via multiple managed names.
// Producing a wrapper for these results in duplicate wrappers with the same names,
// which fails to compile. Do not produce such duplicate wrappers. Alternatively,
// a one line native function with a different name that calls the main one could be used.
// i.e. the wrapper would also have a different name.
#define HANDLES_REUSE_WRAPPER(...) /* nothing  */

#define HANDLES(id, name, func, ret, nargs, argtypes) \
	MONO_HANDLE_DECLARE (id, name, func, ret, nargs, argtypes); \
	MONO_HANDLE_IMPLEMENT (id, name, func, ret, nargs, argtypes)

#include "metadata/icall-def.h"

#undef HANDLES
#undef HANDLES_REUSE_WRAPPER
#undef ICALL_TYPE
#undef ICALL
#undef NOHANDLES
#undef MONO_HANDLE_REGISTER_ICALL
