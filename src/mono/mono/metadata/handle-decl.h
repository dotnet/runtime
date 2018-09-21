/**
 * \file
 * Handle to object in native code
 *
 * Authors:
 *  - Ludovic Henry <ludovic@xamarin.com>
 *  - Aleksey Klieger <aleksey.klieger@xamarin.com>
 *  - Rodrigo Kumpera <kumpera@xamarin.com>
 *
 * Copyright 2016 Dot net foundation.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_HANDLE_0_H__
#define __MONO_HANDLE_0_H__

#include <config.h>
#include <glib.h>
#include <mono/metadata/object-forward.h>
#include <mono/utils/mono-compiler.h>

// Type-safe handles are a struct with a pointer to pointer.
// The only operations allowed on them are the functions/macros in this file, and assignment
// from same handle type to same handle type.
//
// Type-unsafe handles are a pointer to a struct with a pointer.
// Besides the type-safe operations, these can also be:
//  1. compared to NULL, instead of only MONO_HANDLE_IS_NULL
//  2. assigned from NULL, instead of only a handle
//  3. MONO_HANDLE_NEW (T) from anything, instead of only a T*
//  4. MONO_HANDLE_CAST from anything, instead of only another handle type
//  5. assigned from any void*, at least in C
//  6. Cast from any handle type to any handle type, without using MONO_HANDLE_CAST.
//  7. Cast from any handle type to any pointer type and vice versa, such as incorrect unboxing.
//  8. mono_object_class (handle), instead of mono_handle_class
//
// None of those operations were likely intended.
//
// FIXME Do this only on checked builds? Or certain architectures?
// There is not runtime cost.
// NOTE: Running this code depends on the ABI to pass a struct
// with a pointer the same as a pointer. This is tied in with
// marshaling. If this is not the case, turn off type-safety, perhaps per-OS per-CPU.
#if defined (HOST_DARWIN) || defined (HOST_WIN32) || defined (HOST_ARM64) || defined (HOST_ARM) || defined (HOST_AMD64)
#define MONO_TYPE_SAFE_HANDLES 1
#else
#define MONO_TYPE_SAFE_HANDLES 0 // PowerPC, S390X, SPARC, MIPS, Linux/x86, BSD/x86, etc.
#endif

/*
Handle macros/functions
*/

#define TYPED_HANDLE_PAYLOAD_NAME(TYPE) TYPE ## HandlePayload
#define TYPED_HANDLE_NAME(TYPE) TYPE ## Handle
#define TYPED_OUT_HANDLE_NAME(TYPE) TYPE ## HandleOut

// internal helpers:
#define MONO_HANDLE_CAST_FOR(type) mono_handle_cast_##type
#define MONO_HANDLE_TYPECHECK_FOR(type) mono_handle_typecheck_##type

/*
 * TYPED_HANDLE_DECL(SomeType):
 *   Expands to a decl for handles to SomeType and to an internal payload struct.
 *
 * For example, TYPED_HANDLE_DECL(MonoObject) (see below) expands to:
 *
 * #if MONO_TYPE_SAFE_HANDLES
 *
 * typedef struct {
 *   MonoObject **__raw;
 * } MonoObjectHandlePayload,
 *   MonoObjectHandle,
 *   MonoObjectHandleOut;
 *
 * Internal helper functions are also generated.
 *
 * #else
 *
 * typedef struct {
 *   MonoObject *__raw;
 * } MonoObjectHandlePayload;
 *
 * typedef MonoObjectHandlePayload* MonoObjectHandle;
 * typedef MonoObjectHandlePayload* MonoObjectHandleOut;
 *
 * #endif
 */

#ifdef __cplusplus
#define MONO_IF_CPLUSPLUS(x) x
#else
#define MONO_IF_CPLUSPLUS(x) /* nothing */
#endif

#if MONO_TYPE_SAFE_HANDLES
#define TYPED_HANDLE_DECL(TYPE)							\
	typedef struct {							\
		MONO_IF_CPLUSPLUS (						\
			MONO_ALWAYS_INLINE					\
			TYPE * GetRaw () { return __raw ? *__raw : NULL; }	\
		)								\
		TYPE **__raw;							\
	} TYPED_HANDLE_PAYLOAD_NAME (TYPE),					\
	  TYPED_HANDLE_NAME (TYPE),						\
	  TYPED_OUT_HANDLE_NAME (TYPE);						\
/* Do not call these functions directly. Use MONO_HANDLE_NEW and MONO_HANDLE_CAST. */ \
/* Another way to do this involved casting mono_handle_new function to a different type. */ \
static inline MONO_ALWAYS_INLINE TYPED_HANDLE_NAME (TYPE) 	\
MONO_HANDLE_CAST_FOR (TYPE) (gpointer a)			\
{								\
	TYPED_HANDLE_NAME (TYPE) b = { (TYPE**)a };		\
	return b;						\
}								\
static inline MONO_ALWAYS_INLINE MonoObject* 			\
MONO_HANDLE_TYPECHECK_FOR (TYPE) (TYPE *a)			\
{								\
	return (MonoObject*)a;					\
}

#else
#define TYPED_HANDLE_DECL(TYPE)						\
	typedef struct { TYPE *__raw; } TYPED_HANDLE_PAYLOAD_NAME (TYPE) ; \
	typedef TYPED_HANDLE_PAYLOAD_NAME (TYPE) * TYPED_HANDLE_NAME (TYPE); \
	typedef TYPED_HANDLE_PAYLOAD_NAME (TYPE) * TYPED_OUT_HANDLE_NAME (TYPE);
#endif

/*
 * TYPED_VALUE_HANDLE_DECL(SomeType):
 *   Expands to a decl for handles to SomeType (which is a managed valuetype (likely a struct) of some sort) and to an internal payload struct.
 * It is currently identical to TYPED_HANDLE_DECL (valuetypes vs. referencetypes).
 */
#define TYPED_VALUE_HANDLE_DECL(TYPE) TYPED_HANDLE_DECL(TYPE)

#endif /* __MONO_HANDLE_0_H__ */
