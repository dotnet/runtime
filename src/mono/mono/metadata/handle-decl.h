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

#ifndef __MONO_HANDLE_DECL_H__
#define __MONO_HANDLE_DECL_H__

#include <config.h>
#include <glib.h>
#include <mono/metadata/object-forward.h>
#include <mono/utils/mono-compiler.h>

// Type-safe handles are a struct with a pointer to pointer.
// The only operations allowed on them are the functions/macros in this file, and assignment
// from same handle type to same handle type.
//
// Raw handles are void* but still same underlying representation, really void**.
//
// marshal-ilgen.c does not know how to marshal type safe handles.
// It passes/accepts raw handles and generated wrappers in C convert.

/*
Handle macros/functions
*/

#define TYPED_HANDLE_NAME(TYPE) TYPE ## Handle
#define TYPED_OUT_HANDLE_NAME(TYPE) TYPE ## HandleOut
#define TYPED_IN_OUT_HANDLE_NAME(TYPE) TYPE ## HandleInOut

// internal helpers:
#define MONO_HANDLE_CAST_FOR(type) mono_handle_cast_##type
#define MONO_HANDLE_TYPECHECK_FOR(type) mono_handle_typecheck_##type

/*
 * TYPED_HANDLE_DECL(SomeType):
 *   Expands to a decl for handles to SomeType.
 *
 * For example, TYPED_HANDLE_DECL(MonoObject) (see below) expands to:
 *
 * typedef struct {
 *   MonoObject **__raw;
 * } MonoObjectHandle,
 *   MonoObjectHandleOut,
 *   MonoObjectHandleInOut;
 *
 * Out is intended for out parameters, InOut is intended for ref parameters.
 * This is not enforced.
 * Internal helper functions are also generated.
 */
#ifdef __cplusplus
#define MONO_IF_CPLUSPLUS(x) x
#else
#define MONO_IF_CPLUSPLUS(x) /* nothing */
#endif

#define TYPED_HANDLE_DECL(TYPE)							\
	typedef struct {							\
		MONO_IF_CPLUSPLUS (						\
			MONO_ALWAYS_INLINE					\
			TYPE * GetRaw () const { return __raw ? *__raw : NULL; } \
			TYPE * volatile * Ref () { g_assert (__raw); return __raw; } \
		)								\
		TYPE * volatile *__raw;						\
	} TYPED_HANDLE_NAME (TYPE),						\
	  TYPED_OUT_HANDLE_NAME (TYPE),						\
	  TYPED_IN_OUT_HANDLE_NAME (TYPE);					\
/* Do not call these functions directly. Use MONO_HANDLE_NEW and MONO_HANDLE_CAST. */ \
/* Another way to do this involved casting mono_handle_new function to a different type. */ \
static inline MONO_ALWAYS_INLINE TYPED_HANDLE_NAME (TYPE) 	\
MONO_HANDLE_CAST_FOR (TYPE) (MonoRawHandle a)			\
{								\
	TYPED_HANDLE_NAME (TYPE) b = { (TYPE**)a };		\
	return b;						\
}								\
static inline MONO_ALWAYS_INLINE MonoObject* 			\
MONO_HANDLE_TYPECHECK_FOR (TYPE) (TYPE *a)			\
{								\
	return (MonoObject*)a;					\
}								\
/* Out/InOut synonyms for icall-def.h HANDLES () */		\
static inline MONO_ALWAYS_INLINE TYPED_HANDLE_NAME (TYPE) 	\
MONO_HANDLE_CAST_FOR (TYPE##Out) (gpointer a)			\
{								\
	return MONO_HANDLE_CAST_FOR (TYPE) (a);			\
}								\
static inline MONO_ALWAYS_INLINE MonoObject* 			\
MONO_HANDLE_TYPECHECK_FOR (TYPE##Out) (TYPE *a)			\
{								\
	return MONO_HANDLE_TYPECHECK_FOR (TYPE) (a);		\
}								\
static inline MONO_ALWAYS_INLINE TYPED_HANDLE_NAME (TYPE) 	\
MONO_HANDLE_CAST_FOR (TYPE##InOut) (gpointer a)			\
{								\
	return MONO_HANDLE_CAST_FOR (TYPE) (a);			\
}								\
static inline MONO_ALWAYS_INLINE MonoObject* 			\
MONO_HANDLE_TYPECHECK_FOR (TYPE##InOut) (TYPE *a)		\
{								\
	return MONO_HANDLE_TYPECHECK_FOR (TYPE) (a);		\
}

#endif /* __MONO_HANDLE_DECL_H__ */
