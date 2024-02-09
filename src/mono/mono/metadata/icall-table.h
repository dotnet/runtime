/**
 * \file
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_ICALL_TABLE_H__
#define __MONO_METADATA_ICALL_TABLE_H__

#include <config.h>
#include <glib.h>
#include <mono/utils/mono-publib.h>
#include "marshal.h"
#include "icalls.h"

#define MONO_ICALL_TABLE_CALLBACKS_VERSION 3

typedef struct {
	int version;
	gpointer (*lookup) (MonoMethod *method, char *classname, char *methodname, char *sigstart, MonoInternalCallFlags *out_flags);
	const char* (*lookup_icall_symbol) (gpointer func);
} MonoIcallTableCallbacks;

ICALL_EXTERN_C
void
mono_install_icall_table_callbacks (const MonoIcallTableCallbacks *cb);

MONO_API void
mono_icall_table_init (void);

// It helps for types to be single tokens, though this can be relaxed in some places.
// Marshaling a "ptr" does nothing -- just pass it on unchanged.
// Marshaling a "ref" also does nothing at this layer, but
// creates a handle in  marshal-ilgen.c.
// "ref" means "can be an interior pointer".
// "ptr" means "to a local".
// It is generally difficult to know, and "ref" is safer.
// Presently it does not matter.
typedef gint32  *gint32_ptr;
typedef gsize *gsize_ptr;
typedef guchar *guchar_ptr;
typedef const guchar *const_guchar_ptr;
typedef gpointer *gpointer_ptr;
typedef const char *const_char_ptr;
typedef char *char_ptr;
typedef char **char_ptr_ptr;
typedef gunichar2 *gunichar2_ptr;
typedef const gunichar2 *const_gunichar2_ptr;
typedef int *int_ptr;
typedef int **int_ptr_ref;
typedef guint8 **guint8_ptr_ref;
typedef GPtrArray *GPtrArray_ptr;
// HANDLE is not used just to avoid duplicate typedef warnings with some compilers.
// gpointer == void* == HANDLE == FILE_HANDLE == PROCESS_HANDLE.
typedef gpointer PROCESS_HANDLE;
typedef gpointer FILE_HANDLE;
typedef MonoAssemblyName *MonoAssemblyName_ptr;
typedef MonoBoolean *MonoBoolean_ptr;
typedef MonoClass *MonoClass_ptr;
typedef MonoClassField *MonoClassField_ptr;
typedef MonoEvent *MonoEvent_ptr;
typedef MonoImage *MonoImage_ptr;
typedef MonoMethod *MonoMethod_ptr;
typedef MonoProperty *MonoProperty_ptr;
typedef MonoPropertyInfo *MonoPropertyInfo_ref;
typedef MonoType *MonoType_ptr;
typedef MonoTypedRef *MonoTypedRef_ptr;
typedef MonoStackCrawlMark *MonoStackCrawlMark_ptr;
typedef MonoVTable *MonoVTable_ptr;
typedef unsigned *unsigned_ptr;
typedef mono_unichar2 *mono_unichar2_ptr;
typedef mono_unichar4 *mono_unichar4_ptr;
typedef MonoSpanOfObjects *MonoSpanOfObjects_ref;

typedef char **char_ptr_ref;
typedef gint32  *gint32_ref;
typedef gint64  *gint64_ref;
typedef gpointer *gpointer_ref;
typedef gsize *gsize_ref;
typedef guint32 *guint32_ref;
typedef guint64 *guint64_ref;
typedef int *int_ref;
typedef MonoAssemblyName *MonoAssemblyName_ref;
typedef MonoBoolean *MonoBoolean_ref;
typedef MonoClassField *MonoClassField_ref;
typedef MonoEvent *MonoEvent_ref;
typedef MonoEventInfo *MonoEventInfo_ref;
typedef MonoGenericParamInfo *MonoGenericParamInfo_ptr;
typedef MonoMethod *MonoMethod_ref;
typedef MonoMethodInfo *MonoMethodInfo_ref;
typedef MonoResolveTokenError *MonoResolveTokenError_ref;
typedef MonoType *MonoType_ref;
typedef MonoTypedRef *MonoTypedRef_ref;

// Maybe do this in TYPED_HANDLE_DECL.
typedef MonoArray MonoArrayOut;
typedef MonoArray MonoArrayInOut;
typedef MonoArrayHandle MonoArrayOutHandle;
typedef MonoArrayHandle MonoArrayInOutHandle;
typedef MonoException MonoExceptionOut;
typedef MonoExceptionHandle MonoExceptionOutHandle;
typedef MonoObject MonoObjectOut;
typedef MonoObject MonoObjectInOut;
typedef MonoObjectHandle MonoObjectOutHandle;
typedef MonoObjectHandle MonoObjectInOutHandle;
typedef MonoReflectionModule MonoReflectionModuleOut;
typedef MonoReflectionModuleHandle MonoReflectionModuleOutHandle;
typedef MonoString MonoStringOut;
typedef MonoStringHandle MonoStringOutHandle;

// How the arguments and return value of an icall should be wrapped.
// The names and meanings are from marshal-ilgen.c.
// 	ICALL_HANDLES_WRAP_NONE
// 	ICALL_HANDLES_WRAP_OBJ
// 	ICALL_HANDLES_WRAP_OBJ_INOUT
// 	ICALL_HANDLES_WRAP_OBJ_OUT
// 	ICALL_HANDLES_WRAP_VALUETYPE_REF
//
// In the present implementation, all that matters is, handle-or-not,
// in and out and inout are the same, and none and valuetype_ref are the same.
// Handle creation is in marshal-ilgen.c.

// Map a type to a type class: Void and above.
#define MONO_HANDLE_TYPE_WRAP_void 			Void
#define MONO_HANDLE_TYPE_WRAP_GPtrArray_ptr  		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_MonoBoolean   		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_const_gunichar2_ptr	ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_gunichar2_ptr		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_gint   			ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_gint32   			ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_gint64   			ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_gpointer   		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_gconstpointer   		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_gsize   			ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_gssize   			ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_guchar_ptr		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_guint     		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_const_guchar_ptr		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_guint32  			ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_guint64  			ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_int 			ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_uint     			ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_PInfo			ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_mono_bstr			ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_mono_bstr_const		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_unsigned_ptr		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_mono_unichar2_ptr		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_mono_unichar4_ptr		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_MonoImage_ptr		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_MonoClassField_ptr	ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_MonoMarshalNative		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_MonoProperty_ptr		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_size_t			ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_MonoVTable_ptr		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_MonoQCallTypeHandle	ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_MonoQCallAssemblyHandle	ICALL_HANDLES_WRAP_NONE

#define MONO_HANDLE_TYPE_WRAP_MonoAssemblyName_ref	ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_MonoBoolean_ref 		ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_MonoClassField_ref  	ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_MonoEvent_ref		ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_MonoEventInfo_ref		ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_MonoMethod_ref 		ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_MonoMethodInfo_ref	ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_MonoPropertyInfo_ref	ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_MonoType_ref  		ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_MonoTypedRef_ref 		ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_gint32_ref   		ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_gint64_ref  		ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_gpointer_ref   		ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_gsize_ref   		ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_guint32_ref   		ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_guint64_ref   		ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_int_ref  			ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_gint32_ref  			ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_int_ptr_ref  		ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_char_ptr_ref		ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_guint8_ptr_ref		ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_MonoResolveTokenError_ref	ICALL_HANDLES_WRAP_VALUETYPE_REF
#define MONO_HANDLE_TYPE_WRAP_MonoSpanOfObjects_ref		ICALL_HANDLES_WRAP_VALUETYPE_REF

// HANDLE is not used just to avoid duplicate typedef warnings with some compilers.
// gpointer == void* == HANDLE == FILE_HANDLE == PROCESS_HANDLE.
#define MONO_HANDLE_TYPE_WRAP_char_ptr   		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_const_char_ptr		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_FILE_HANDLE		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_MonoClass_ptr  		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_MonoEvent_ptr		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_MonoGenericParamInfo_ptr	ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_MonoMethod_ptr 		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_MonoType_ptr  		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_MonoTypedRef_ptr 		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_MonoStackCrawlMark_ptr  	ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_gint32_ptr   		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_gpointer_ptr		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_PROCESS_HANDLE		ICALL_HANDLES_WRAP_NONE
#define MONO_HANDLE_TYPE_WRAP_MonoObjectHandleOnStack  		ICALL_HANDLES_WRAP_NONE

// Please keep this sorted (grep ICALL_HANDLES_WRAP_OBJ$ | sort)
#define MONO_HANDLE_TYPE_WRAP_MonoAppContext 			ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoAppDomain			ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoAppDomainSetup		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoArray				ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoAsyncResult			ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoCalendarData		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoComObject			ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoCultureData		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoCultureInfo		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoDelegate			ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoException			ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoInternalThread		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoIOSelectorJob			ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoObject			ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoManifestResourceInfo 		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoMulticastDelegate		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoReflectionAssembly		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoReflectionAssemblyBuilder 	ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoReflectionDynamicMethod 	ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoReflectionEvent		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoReflectionMonoEvent		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoReflectionField		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoReflectionMarshalAsAttribute	ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoReflectionMethod     		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoReflectionMethodBody 		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoReflectionModule 		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoReflectionModuleBuilder 	ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoReflectionParameter		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoReflectionProperty		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoReflectionSigHelper		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoReflectionType		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoReflectionTypeBuilder		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoRegionInfo		ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoString			ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoStringBuilder			ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoThreadObject			ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoTransparentProxy			ICALL_HANDLES_WRAP_OBJ
#define MONO_HANDLE_TYPE_WRAP_MonoW32ProcessStartInfo		ICALL_HANDLES_WRAP_OBJ

#define MONO_HANDLE_TYPE_WRAP_MonoExceptionOut		ICALL_HANDLES_WRAP_OBJ_OUT
#define MONO_HANDLE_TYPE_WRAP_MonoObjectOut		ICALL_HANDLES_WRAP_OBJ_OUT
#define MONO_HANDLE_TYPE_WRAP_MonoStringOut		ICALL_HANDLES_WRAP_OBJ_OUT
#define MONO_HANDLE_TYPE_WRAP_MonoArrayOut		ICALL_HANDLES_WRAP_OBJ_OUT
#define MONO_HANDLE_TYPE_WRAP_MonoReflectionModuleOut 	ICALL_HANDLES_WRAP_OBJ_OUT

#define MONO_HANDLE_TYPE_WRAP_MonoW32ProcessInfo_ref	ICALL_HANDLES_WRAP_VALUETYPE_REF

// These are rare, and could be eliminated.
// They could be return values, or just separate out parameters.
#define MONO_HANDLE_TYPE_WRAP_MonoObjectInOut	ICALL_HANDLES_WRAP_OBJ_INOUT
#define MONO_HANDLE_TYPE_WRAP_MonoArrayInOut	ICALL_HANDLES_WRAP_OBJ_INOUT

// Do macro_prefix for type type, mapping type to a type class.
// Note that the macro can further be followed by parameters.
#define MONO_HANDLE_DO3(macro_prefix, type) macro_prefix ## type
#define MONO_HANDLE_DO2(macro_prefix, type) MONO_HANDLE_DO3 (macro_prefix, type)
#define MONO_HANDLE_DO(macro_prefix, type)  MONO_HANDLE_DO2 (macro_prefix, MONO_HANDLE_TYPE_WRAP_ ## type)

#define MONO_HANDLE_RETURN_BEGIN(type)				MONO_HANDLE_DO (MONO_HANDLE_RETURN_BEGIN_, type) (type)
#define MONO_HANDLE_RETURN_BEGIN_Void(type)			/* nothing */
#define MONO_HANDLE_RETURN_BEGIN_ICALL_HANDLES_WRAP_NONE(type)  type icall_result =
#define MONO_HANDLE_RETURN_BEGIN_ICALL_HANDLES_WRAP_OBJ(type)   type ## Handle icall_result =

#define MONO_HANDLE_RETURN_END(type)				MONO_HANDLE_DO (MONO_HANDLE_RETURN_END_, type);
#define MONO_HANDLE_RETURN_END_Void				HANDLE_FUNCTION_RETURN ()
#define MONO_HANDLE_RETURN_END_ICALL_HANDLES_WRAP_NONE   	HANDLE_FUNCTION_RETURN_VAL (icall_result)
#define MONO_HANDLE_RETURN_END_ICALL_HANDLES_WRAP_OBJ		HANDLE_FUNCTION_RETURN_OBJ (icall_result)

// Convert raw handles to typed handles, just by casting and copying a pointer.
#define MONO_HANDLE_MARSHAL(type, n)					MONO_HANDLE_DO (MONO_HANDLE_MARSHAL_, type) (type, n)
#define MONO_HANDLE_MARSHAL_ICALL_HANDLES_WRAP_NONE(type, n)     	a ## n
#define MONO_HANDLE_MARSHAL_ICALL_HANDLES_WRAP_OBJ(type, n)		*(type ## Handle*)&a ## n
#define MONO_HANDLE_MARSHAL_ICALL_HANDLES_WRAP_OBJ_OUT(type, n)		*(type ## Handle*)&a ## n
#define MONO_HANDLE_MARSHAL_ICALL_HANDLES_WRAP_OBJ_INOUT(type, n)	*(type ## Handle*)&a ## n
#define MONO_HANDLE_MARSHAL_ICALL_HANDLES_WRAP_VALUETYPE_REF(type, n)	a ## n

// Declare and initialize a local for an object in, out, inout parameters, upon input.
#define MONO_HANDLE_REGISTER_ICALL_LOCALS(type, n)					MONO_HANDLE_DO (MONO_HANDLE_REGISTER_ICALL_LOCALS_, type) (type, n)
#define MONO_HANDLE_REGISTER_ICALL_LOCALS_ICALL_HANDLES_WRAP_NONE(type, n)     		/* nothing */
#define MONO_HANDLE_REGISTER_ICALL_LOCALS_ICALL_HANDLES_WRAP_OBJ(type, n)		type ## Handle a ## n = MONO_HANDLE_NEW (type, a ## n ## _raw);
#define MONO_HANDLE_REGISTER_ICALL_LOCALS_ICALL_HANDLES_WRAP_OBJ_OUT(type, n)		unused_untested_looks_correct1 type ## Handle a ## n = MONO_HANDLE_NEW (type, NULL);
#define MONO_HANDLE_REGISTER_ICALL_LOCALS_ICALL_HANDLES_WRAP_OBJ_INOUT(type, n)		unused_untested_looks_correct2 type ## Handle a ## n = MONO_HANDLE_NEW (type, *a ## n ## _raw);
#define MONO_HANDLE_REGISTER_ICALL_LOCALS_ICALL_HANDLES_WRAP_VALUETYPE_REF(type, n)	FIXME restore mono_icall_handle_new_interior from e8b037642104527bd9b9ba70d502210b9c12d2b8 \
											type ## Handle a ## n = mono_icall_handle_new_interior (a ## n ## _raw);
// Produce all the locals, i.e. up to one per parameter.
#define MONO_HANDLE_REGISTER_ICALL_LOCALS_0()					/* nothing  */
#define MONO_HANDLE_REGISTER_ICALL_LOCALS_1(t0)					MONO_HANDLE_REGISTER_ICALL_LOCALS (t0, 0)
#define MONO_HANDLE_REGISTER_ICALL_LOCALS_2(t0, t1)				MONO_HANDLE_REGISTER_ICALL_LOCALS_1 (t0) 				MONO_HANDLE_REGISTER_ICALL_LOCALS (t1, 1)
#define MONO_HANDLE_REGISTER_ICALL_LOCALS_3(t0, t1, t2)				MONO_HANDLE_REGISTER_ICALL_LOCALS_2 (t0, t1) 				MONO_HANDLE_REGISTER_ICALL_LOCALS (t2, 2)
#define MONO_HANDLE_REGISTER_ICALL_LOCALS_4(t0, t1, t2, t3)			MONO_HANDLE_REGISTER_ICALL_LOCALS_3 (t0, t1, t2)			MONO_HANDLE_REGISTER_ICALL_LOCALS (t3, 3)
#define MONO_HANDLE_REGISTER_ICALL_LOCALS_5(t0, t1, t2, t3, t4)			MONO_HANDLE_REGISTER_ICALL_LOCALS_4 (t0, t1, t2, t3)			MONO_HANDLE_REGISTER_ICALL_LOCALS (t4, 4)
#define MONO_HANDLE_REGISTER_ICALL_LOCALS_6(t0, t1, t2, t3, t4, t5)		MONO_HANDLE_REGISTER_ICALL_LOCALS_5 (t0, t1, t2, t3, t4) 		MONO_HANDLE_REGISTER_ICALL_LOCALS (t5, 5)
#define MONO_HANDLE_REGISTER_ICALL_LOCALS_7(t0, t1, t2, t3, t4, t5, t6)		MONO_HANDLE_REGISTER_ICALL_LOCALS_6 (t0, t1, t2, t3, t4, t5) 		MONO_HANDLE_REGISTER_ICALL_LOCALS (t6, 6)
#define MONO_HANDLE_REGISTER_ICALL_LOCALS_8(t0, t1, t2, t3, t4, t5, t6, t7)	MONO_HANDLE_REGISTER_ICALL_LOCALS_7 (t0, t1, t2, t3, t4, t5, t6) 	MONO_HANDLE_REGISTER_ICALL_LOCALS (t7, 7)
#define MONO_HANDLE_REGISTER_ICALL_LOCALS_9(t0, t1, t2, t3, t4, t5, t6, t7, t8)	MONO_HANDLE_REGISTER_ICALL_LOCALS_8 (t0, t1, t2, t3, t4, t5, t6, t7)	MONO_HANDLE_REGISTER_ICALL_LOCALS (t8, 8)

// Convert a typed handle to raw pointer upon output.
#define MONO_HANDLE_REGISTER_ICALL_OUT(type, n)					MONO_HANDLE_DO (MONO_HANDLE_REGISTER_ICALL_OUT_, type) (type, n)
#define MONO_HANDLE_REGISTER_ICALL_OUT_ICALL_HANDLES_WRAP_NONE(type, n)		/* nothing */
#define MONO_HANDLE_REGISTER_ICALL_OUT_ICALL_HANDLES_WRAP_OBJ(type, n)		/* nothing */
#define MONO_HANDLE_REGISTER_ICALL_OUT_ICALL_HANDLES_WRAP_OBJ_OUT(type, n)	unused_untested_looks_correct3 *a ## n ## _raw = MONO_HANDLE_RAW (a ## n);
#define MONO_HANDLE_REGISTER_ICALL_OUT_ICALL_HANDLES_WRAP_OBJ_INOUT		unused_untested_looks_correct4 *a ## n ## _raw = MONO_HANDLE_RAW (a ## n);
#define MONO_HANDLE_REGISTER_ICALL_OUT_ICALL_HANDLES_VALUETYPE_REF(type, n)	/* nothing */

// Convert all the typed handles to raw pointers upon output, i.e. up to one per parameter.
#define MONO_HANDLE_REGISTER_ICALL_OUT_0()					/* nothing  */
#define MONO_HANDLE_REGISTER_ICALL_OUT_1(t0)					MONO_HANDLE_REGISTER_ICALL_OUT (t0, 0)
#define MONO_HANDLE_REGISTER_ICALL_OUT_2(t0, t1)				MONO_HANDLE_REGISTER_ICALL_OUT_1 (t0) 					MONO_HANDLE_REGISTER_ICALL_OUT (t1, 1)
#define MONO_HANDLE_REGISTER_ICALL_OUT_3(t0, t1, t2)				MONO_HANDLE_REGISTER_ICALL_OUT_2 (t0, t1) 				MONO_HANDLE_REGISTER_ICALL_OUT (t2, 2)
#define MONO_HANDLE_REGISTER_ICALL_OUT_4(t0, t1, t2, t3)			MONO_HANDLE_REGISTER_ICALL_OUT_3 (t0, t1, t2)				MONO_HANDLE_REGISTER_ICALL_OUT (t3, 3)
#define MONO_HANDLE_REGISTER_ICALL_OUT_5(t0, t1, t2, t3, t4)			MONO_HANDLE_REGISTER_ICALL_OUT_4 (t0, t1, t2, t3)			MONO_HANDLE_REGISTER_ICALL_OUT (t4, 4)
#define MONO_HANDLE_REGISTER_ICALL_OUT_6(t0, t1, t2, t3, t4, t5)		MONO_HANDLE_REGISTER_ICALL_OUT_5 (t0, t1, t2, t3, t4) 			MONO_HANDLE_REGISTER_ICALL_OUT (t5, 5)
#define MONO_HANDLE_REGISTER_ICALL_OUT_7(t0, t1, t2, t3, t4, t5, t6)		MONO_HANDLE_REGISTER_ICALL_OUT_6 (t0, t1, t2, t3, t4, t5) 		MONO_HANDLE_REGISTER_ICALL_OUT (t6, 6)
#define MONO_HANDLE_REGISTER_ICALL_OUT_8(t0, t1, t2, t3, t4, t5, t6, t7)	MONO_HANDLE_REGISTER_ICALL_OUT_7 (t0, t1, t2, t3, t4, t5, t6) 		MONO_HANDLE_REGISTER_ICALL_OUT (t7, 7)
#define MONO_HANDLE_REGISTER_ICALL_OUT_9(t0, t1, t2, t3, t4, t5, t6, t7, t8)	MONO_HANDLE_REGISTER_ICALL_OUT_8 (t0, t1, t2, t3, t4, t5, t6, t7)	MONO_HANDLE_REGISTER_ICALL_OUT (t8, 8)

#define MONO_HANDLE_TYPE_TYPED(type)					MONO_HANDLE_DO (MONO_HANDLE_TYPE_TYPED_, type) (type)
#define MONO_HANDLE_TYPE_TYPED_Void(type)				type
#define MONO_HANDLE_TYPE_TYPED_ICALL_HANDLES_WRAP_NONE(type)		type
#define MONO_HANDLE_TYPE_TYPED_ICALL_HANDLES_WRAP_OBJ(type)		type ## Handle
#define MONO_HANDLE_TYPE_TYPED_ICALL_HANDLES_WRAP_OBJ_OUT(type)		type ## Handle
#define MONO_HANDLE_TYPE_TYPED_ICALL_HANDLES_WRAP_OBJ_INOUT(type)	type ## Handle
#define MONO_HANDLE_TYPE_TYPED_ICALL_HANDLES_WRAP_VALUETYPE_REF(type)	type

// Map a type to a raw handle, or itself.
#define MONO_HANDLE_TYPE_RAWHANDLE(type)					MONO_HANDLE_DO (MONO_HANDLE_TYPE_RAWHANDLE_, type) (type)
#define MONO_HANDLE_TYPE_RAWHANDLE_Void(type)					type
#define MONO_HANDLE_TYPE_RAWHANDLE_ICALL_HANDLES_WRAP_NONE(type)		type
#define MONO_HANDLE_TYPE_RAWHANDLE_ICALL_HANDLES_WRAP_OBJ(type)			MonoRawHandle
#define MONO_HANDLE_TYPE_RAWHANDLE_ICALL_HANDLES_WRAP_OBJ_OUT(type)		MonoRawHandle
#define MONO_HANDLE_TYPE_RAWHANDLE_ICALL_HANDLES_WRAP_OBJ_INOUT(type)		MonoRawHandle
#define MONO_HANDLE_TYPE_RAWHANDLE_ICALL_HANDLES_WRAP_VALUETYPE_REF(type)	type

// Map a type to a raw pointer, or itself.
#define MONO_HANDLE_TYPE_RAWPOINTER(type)					MONO_HANDLE_DO (MONO_HANDLE_TYPE_RAWPOINTER_, type) (type)
#define MONO_HANDLE_TYPE_RAWPOINTER_Void(type)					type
#define MONO_HANDLE_TYPE_RAWPOINTER_ICALL_HANDLES_WRAP_NONE(type)		type
#define MONO_HANDLE_TYPE_RAWPOINTER_ICALL_HANDLES_WRAP_OBJ(type)		type*
// Only used for return types.
//#define MONO_HANDLE_TYPE_RAWPOINTER_ICALL_HANDLES_WRAP_OBJ_OUT(type)		type*
//#define MONO_HANDLE_TYPE_RAWPOINTER_ICALL_HANDLES_WRAP_OBJ_INOUT(type)	type*
#define MONO_HANDLE_TYPE_RAWPOINTER_ICALL_HANDLES_WRAP_VALUETYPE_REF(type)	type

// Type/name in raw handle prototype and implementation.
#define MONO_HANDLE_ARG_RAWHANDLE(type, n)					MONO_HANDLE_DO (MONO_HANDLE_ARG_RAWHANDLE_, type) (type, n)
#define MONO_HANDLE_ARG_RAWHANDLE_ICALL_HANDLES_WRAP_NONE(type, n)		MONO_HANDLE_TYPE_RAWHANDLE (type) a ## n
#define MONO_HANDLE_ARG_RAWHANDLE_ICALL_HANDLES_WRAP_OBJ(type, n)		MONO_HANDLE_TYPE_RAWHANDLE (type) a ## n
#define MONO_HANDLE_ARG_RAWHANDLE_ICALL_HANDLES_WRAP_OBJ_OUT(type, n)		MONO_HANDLE_TYPE_RAWHANDLE (type) a ## n
#define MONO_HANDLE_ARG_RAWHANDLE_ICALL_HANDLES_WRAP_OBJ_INOUT(type, n)	MONO_HANDLE_TYPE_RAWHANDLE (type) a ## n
#define MONO_HANDLE_ARG_RAWHANDLE_ICALL_HANDLES_WRAP_VALUETYPE_REF(type, n)	MONO_HANDLE_TYPE_RAWHANDLE (type) a ## n

// Type/name in raw pointer prototype and implementation.
#define MONO_HANDLE_ARG_RAWPOINTER(type, n)					MONO_HANDLE_DO (MONO_HANDLE_ARG_RAWPOINTER_, type) (type, n)
#define MONO_HANDLE_ARG_RAWPOINTER_ICALL_HANDLES_WRAP_NONE(type, n)		MONO_HANDLE_TYPE_RAWPOINTER (type) a ## n
#define MONO_HANDLE_ARG_RAWPOINTER_ICALL_HANDLES_WRAP_OBJ(type, n)		MONO_HANDLE_TYPE_RAWPOINTER (type) a ## n ## _raw
#define MONO_HANDLE_ARG_RAWPOINTER_ICALL_HANDLES_WRAP_OBJ_OUT(type, n)		unused_untested_looks_correct5 MONO_HANDLE_TYPE_RAWPOINTER (type) a ## n ## _raw
#define MONO_HANDLE_ARG_RAWPOINTER_ICALL_HANDLES_WRAP_OBJ_INOUT(type, n)	unused_untested_looks_correct6 MONO_HANDLE_TYPE_RAWPOINTER (type) a ## n ## _raw
#define MONO_HANDLE_ARG_RAWPOINTER_ICALL_HANDLES_WRAP_VALUETYPE_REF(type, n)	FIXME //MONO_HANDLE_TYPE_RAWPOINTER (type) a ## n

// Generate a parameter list, types only, for a function accepting/returning typed handles.
#define MONO_HANDLE_FOREACH_TYPE_TYPED_0()	   			     /* nothing */
#define MONO_HANDLE_FOREACH_TYPE_TYPED_1(t0) 	   			     MONO_HANDLE_TYPE_TYPED (t0)
#define MONO_HANDLE_FOREACH_TYPE_TYPED_2(t0, t1)     			     MONO_HANDLE_FOREACH_TYPE_TYPED_1 (t0) 				,MONO_HANDLE_TYPE_TYPED (t1)
#define MONO_HANDLE_FOREACH_TYPE_TYPED_3(t0, t1, t2)			     MONO_HANDLE_FOREACH_TYPE_TYPED_2 (t0, t1)		        	,MONO_HANDLE_TYPE_TYPED (t2)
#define MONO_HANDLE_FOREACH_TYPE_TYPED_4(t0, t1, t2, t3)		     MONO_HANDLE_FOREACH_TYPE_TYPED_3 (t0, t1, t2) 			,MONO_HANDLE_TYPE_TYPED (t3)
#define MONO_HANDLE_FOREACH_TYPE_TYPED_5(t0, t1, t2, t3, t4) 		     MONO_HANDLE_FOREACH_TYPE_TYPED_4 (t0, t1, t2, t3) 			,MONO_HANDLE_TYPE_TYPED (t4)
#define MONO_HANDLE_FOREACH_TYPE_TYPED_6(t0, t1, t2, t3, t4, t5)             MONO_HANDLE_FOREACH_TYPE_TYPED_5 (t0, t1, t2, t3, t4) 		,MONO_HANDLE_TYPE_TYPED (t5)
#define MONO_HANDLE_FOREACH_TYPE_TYPED_7(t0, t1, t2, t3, t4, t5, t6) 	     MONO_HANDLE_FOREACH_TYPE_TYPED_6 (t0, t1, t2, t3, t4, t5) 	 	,MONO_HANDLE_TYPE_TYPED (t6)
#define MONO_HANDLE_FOREACH_TYPE_TYPED_8(t0, t1, t2, t3, t4, t5, t6, t7)     MONO_HANDLE_FOREACH_TYPE_TYPED_7 (t0, t1, t2, t3, t4, t5, t6)	,MONO_HANDLE_TYPE_TYPED (t7)
#define MONO_HANDLE_FOREACH_TYPE_TYPED_9(t0, t1, t2, t3, t4, t5, t6, t7, t8) MONO_HANDLE_FOREACH_TYPE_TYPED_8 (t0, t1, t2, t3, t4, t5, t6, t7)	,MONO_HANDLE_TYPE_TYPED (t8)

// Generate a parameter list, types and names, for a function accepting raw handles and no MonoError,
// and returning a raw pointer.
#define MONO_HANDLE_FOREACH_ARG_RAW_0()						void
#define MONO_HANDLE_FOREACH_ARG_RAW_1(t0) 	   	  			MONO_HANDLE_ARG_RAWHANDLE (t0, 0)
#define MONO_HANDLE_FOREACH_ARG_RAW_2(t0, t1)	  				MONO_HANDLE_FOREACH_ARG_RAW_1 (t0),             		MONO_HANDLE_ARG_RAWHANDLE (t1, 1)
#define MONO_HANDLE_FOREACH_ARG_RAW_3(t0, t1, t2)	  			MONO_HANDLE_FOREACH_ARG_RAW_2 (t0, t1),         		MONO_HANDLE_ARG_RAWHANDLE (t2, 2)
#define MONO_HANDLE_FOREACH_ARG_RAW_4(t0, t1, t2, t3)				MONO_HANDLE_FOREACH_ARG_RAW_3 (t0, t1, t2),     		MONO_HANDLE_ARG_RAWHANDLE (t3, 3)
#define MONO_HANDLE_FOREACH_ARG_RAW_5(t0, t1, t2, t3, t4)			MONO_HANDLE_FOREACH_ARG_RAW_4 (t0, t1, t2, t3), 		MONO_HANDLE_ARG_RAWHANDLE (t4, 4)
#define MONO_HANDLE_FOREACH_ARG_RAW_6(t0, t1, t2, t3, t4, t5)			MONO_HANDLE_FOREACH_ARG_RAW_5 (t0, t1, t2, t3, t4), 		MONO_HANDLE_ARG_RAWHANDLE (t5, 5)
#define MONO_HANDLE_FOREACH_ARG_RAW_7(t0, t1, t2, t3, t4, t5, t6)		MONO_HANDLE_FOREACH_ARG_RAW_6 (t0, t1, t2, t3, t4, t5), 	MONO_HANDLE_ARG_RAWHANDLE (t6, 6)
#define MONO_HANDLE_FOREACH_ARG_RAW_8(t0, t1, t2, t3, t4, t5, t6, t7)		MONO_HANDLE_FOREACH_ARG_RAW_7 (t0, t1, t2, t3, t4, t5, t6),	MONO_HANDLE_ARG_RAWHANDLE (t7, 7)
#define MONO_HANDLE_FOREACH_ARG_RAW_9(t0, t1, t2, t3, t4, t5, t6, t7, t8)  	MONO_HANDLE_FOREACH_ARG_RAW_8 (t0, t1, t2, t3, t4, t5, t6, t7),	MONO_HANDLE_ARG_RAWHANDLE (t8, 8)

// Generate a parameter list, types and names, for a function accepting raw pointers and no MonoError,
// and returning a raw pointer.
#define MONO_HANDLE_FOREACH_ARG_RAWPOINTER_0()						void
#define MONO_HANDLE_FOREACH_ARG_RAWPOINTER_1(t0) 	   	  			MONO_HANDLE_ARG_RAWPOINTER (t0, 0)
#define MONO_HANDLE_FOREACH_ARG_RAWPOINTER_2(t0, t1)	  				MONO_HANDLE_FOREACH_ARG_RAWPOINTER_1 (t0),             			MONO_HANDLE_ARG_RAWPOINTER (t1, 1)
#define MONO_HANDLE_FOREACH_ARG_RAWPOINTER_3(t0, t1, t2)	  			MONO_HANDLE_FOREACH_ARG_RAWPOINTER_2 (t0, t1),         			MONO_HANDLE_ARG_RAWPOINTER (t2, 2)
#define MONO_HANDLE_FOREACH_ARG_RAWPOINTER_4(t0, t1, t2, t3)				MONO_HANDLE_FOREACH_ARG_RAWPOINTER_3 (t0, t1, t2),     			MONO_HANDLE_ARG_RAWPOINTER (t3, 3)
#define MONO_HANDLE_FOREACH_ARG_RAWPOINTER_5(t0, t1, t2, t3, t4)			MONO_HANDLE_FOREACH_ARG_RAWPOINTER_4 (t0, t1, t2, t3), 			MONO_HANDLE_ARG_RAWPOINTER (t4, 4)
#define MONO_HANDLE_FOREACH_ARG_RAWPOINTER_6(t0, t1, t2, t3, t4, t5)			MONO_HANDLE_FOREACH_ARG_RAWPOINTER_5 (t0, t1, t2, t3, t4), 		MONO_HANDLE_ARG_RAWPOINTER (t5, 5)
#define MONO_HANDLE_FOREACH_ARG_RAWPOINTER_7(t0, t1, t2, t3, t4, t5, t6)		MONO_HANDLE_FOREACH_ARG_RAWPOINTER_6 (t0, t1, t2, t3, t4, t5), 		MONO_HANDLE_ARG_RAWPOINTER (t6, 6)
#define MONO_HANDLE_FOREACH_ARG_RAWPOINTER_8(t0, t1, t2, t3, t4, t5, t6, t7)		MONO_HANDLE_FOREACH_ARG_RAWPOINTER_7 (t0, t1, t2, t3, t4, t5, t6),	MONO_HANDLE_ARG_RAWPOINTER (t7, 7)
#define MONO_HANDLE_FOREACH_ARG_RAWPOINTER_9(t0, t1, t2, t3, t4, t5, t6, t7, t8)	MONO_HANDLE_FOREACH_ARG_RAWPOINTER_8 (t0, t1, t2, t3, t4, t5, t6, t7),	MONO_HANDLE_ARG_RAWPOINTER (t8, 8)

#define MONO_HANDLE_REGISTER_ICALL_CALL_0 /*  nothing */
#define MONO_HANDLE_REGISTER_ICALL_CALL_1 a0,
#define MONO_HANDLE_REGISTER_ICALL_CALL_2 MONO_HANDLE_REGISTER_ICALL_CALL_1 a1,
#define MONO_HANDLE_REGISTER_ICALL_CALL_3 MONO_HANDLE_REGISTER_ICALL_CALL_2 a2,
#define MONO_HANDLE_REGISTER_ICALL_CALL_4 MONO_HANDLE_REGISTER_ICALL_CALL_3 a3,
#define MONO_HANDLE_REGISTER_ICALL_CALL_5 MONO_HANDLE_REGISTER_ICALL_CALL_4 a4,
#define MONO_HANDLE_REGISTER_ICALL_CALL_6 MONO_HANDLE_REGISTER_ICALL_CALL_5 a5,
#define MONO_HANDLE_REGISTER_ICALL_CALL_7 MONO_HANDLE_REGISTER_ICALL_CALL_6 a6,
#define MONO_HANDLE_REGISTER_ICALL_CALL_8 MONO_HANDLE_REGISTER_ICALL_CALL_7 a7,
#define MONO_HANDLE_REGISTER_ICALL_CALL_9 MONO_HANDLE_REGISTER_ICALL_CALL_8 a8,

// Call from the wrapper to the actual icall, passing on the
// WRAP_NONE parameters directly, casting handles from raw to typed.
#define MONO_HANDLE_CALL_0()					/* nothing  */
#define MONO_HANDLE_CALL_1(t0)					MONO_HANDLE_MARSHAL (t0, 0)
#define MONO_HANDLE_CALL_2(t0, t1)				MONO_HANDLE_CALL_1 (t0), 				MONO_HANDLE_MARSHAL (t1, 1)
#define MONO_HANDLE_CALL_3(t0, t1, t2)				MONO_HANDLE_CALL_2 (t0, t1), 				MONO_HANDLE_MARSHAL (t2, 2)
#define MONO_HANDLE_CALL_4(t0, t1, t2, t3)			MONO_HANDLE_CALL_3 (t0, t1, t2),			MONO_HANDLE_MARSHAL (t3, 3)
#define MONO_HANDLE_CALL_5(t0, t1, t2, t3, t4)			MONO_HANDLE_CALL_4 (t0, t1, t2, t3),			MONO_HANDLE_MARSHAL (t4, 4)
#define MONO_HANDLE_CALL_6(t0, t1, t2, t3, t4, t5)		MONO_HANDLE_CALL_5 (t0, t1, t2, t3, t4), 		MONO_HANDLE_MARSHAL (t5, 5)
#define MONO_HANDLE_CALL_7(t0, t1, t2, t3, t4, t5, t6)		MONO_HANDLE_CALL_6 (t0, t1, t2, t3, t4, t5), 		MONO_HANDLE_MARSHAL (t6, 6)
#define MONO_HANDLE_CALL_8(t0, t1, t2, t3, t4, t5, t6, t7)	MONO_HANDLE_CALL_7 (t0, t1, t2, t3, t4, t5, t6), 	MONO_HANDLE_MARSHAL (t7, 7)
#define MONO_HANDLE_CALL_9(t0, t1, t2, t3, t4, t5, t6, t7, t8)	MONO_HANDLE_CALL_8 (t0, t1, t2, t3, t4, t5, t6, t7),	MONO_HANDLE_MARSHAL (t8, 8)

// Place a comma after a parameter list of length n, i.e. nothing for 0, else comma.
#define MONO_HANDLE_COMMA_0 /* nothing */
#define MONO_HANDLE_COMMA_1 ,
#define MONO_HANDLE_COMMA_2 ,
#define MONO_HANDLE_COMMA_3 ,
#define MONO_HANDLE_COMMA_4 ,
#define MONO_HANDLE_COMMA_5 ,
#define MONO_HANDLE_COMMA_6 ,
#define MONO_HANDLE_COMMA_7 ,
#define MONO_HANDLE_COMMA_8 ,
#define MONO_HANDLE_COMMA_9 ,

// Declare the function that takes/returns typed handles and a MonoError.
#define MONO_HANDLE_DECLARE(id, name, func, rettype, n, argtypes)	\
MONO_HANDLE_TYPE_TYPED (rettype)					\
func (MONO_HANDLE_FOREACH_TYPE_TYPED_ ## n argtypes MONO_HANDLE_COMMA_ ## n MonoError *error)

// Declare the function wrapper that takes raw handles and returns a raw pointer.
#define MONO_HANDLE_DECLARE_RAW(id, name, func, rettype, n, argtypes)	\
ICALL_EXPORT MONO_HANDLE_TYPE_RAWPOINTER (rettype)				\
func ## _raw ( MONO_HANDLE_FOREACH_ARG_RAW_ ## n argtypes)

// Implement ves_icall_foo_raw over ves_icall_foo.
// Raw handles are converted to/from typed handles and the rest is passed through.
// This is for functions in icall-def.h.

#define MONO_HANDLE_IMPLEMENT(id, name, func, rettype, n, argtypes)	\
										\
MONO_HANDLE_DECLARE_RAW (id, name, func, rettype, n, argtypes)			\
{										\
	HANDLE_FUNCTION_ENTER ();						\
										\
	ERROR_DECL (error);							\
										\
	MONO_HANDLE_RETURN_BEGIN (rettype)					\
										\
	func (MONO_HANDLE_CALL_ ## n argtypes MONO_HANDLE_COMMA_ ## n error);	\
										\
	mono_error_set_pending_exception (error);				\
										\
	MONO_HANDLE_RETURN_END (rettype)					\
}										\

// Declare the function that takes/returns raw pointers and no MonoError.
#define MONO_HANDLE_REGISTER_ICALL_DECLARE_RAW(func, rettype, n, argtypes)	\
ICALL_EXPORT MONO_HANDLE_TYPE_RAWPOINTER (rettype)				\
func ( MONO_HANDLE_FOREACH_ARG_RAWPOINTER_ ## n argtypes)

// Implement ves_icall_foo over ves_icall_foo_impl.
//
// Raw pointers are converted to/from handles and the rest is passed through.
// The in/out/inout-ness of parameters must be correct. (unlike MONO_HANDLE_IMPLEMENT)
// Valuetype-refs are not handled. (unlike MONO_HANDLE_IMPLEMENT)
// Handle creation is less efficient than MONO_HANDLE_IMPLEMENT (marshal-ilgen.c) -- using TLS
// and per-handle work.
//
// In future this should produce an array of IcallHandlesWrap and send that through
// to emit_native_icall_wrapper_ilgen to gain its efficient handles.
//
// Or put the handles directly in the coop frame, or pointers to them.
// i.e. one TLS access at function start and end.
//
// This is for functions passed to mono_register_jit_icall_info, etc.

#define MONO_HANDLE_REGISTER_ICALL_IMPLEMENT(func, rettype, n, argtypes)	\
										\
MONO_HANDLE_REGISTER_ICALL_DECLARE_RAW (func, rettype, n, argtypes)		\
{										\
	HANDLE_FUNCTION_ENTER ();						\
										\
	ERROR_DECL (error);							\
										\
	MONO_HANDLE_REGISTER_ICALL_LOCALS_ ## n argtypes			\
										\
	MONO_HANDLE_RETURN_BEGIN (rettype)					\
										\
	func ## _impl (MONO_HANDLE_REGISTER_ICALL_CALL_ ## n error);			\
										\
	MONO_HANDLE_REGISTER_ICALL_OUT_ ## n argtypes				\
										\
	mono_error_set_pending_exception (error);				\
										\
	MONO_HANDLE_RETURN_END (rettype)					\
}										\

#endif
