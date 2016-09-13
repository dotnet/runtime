/*
 * handle.h: Handle to object in native code
 *
 * Authors:
 *  - Ludovic Henry <ludovic@xamarin.com>
 *  - Aleksey Klieger <aleksey.klieger@xamarin.com>
 *  - Rodrigo Kumpera <kumpera@xamarin.com>
 *
 * Copyright 2016 Dot net foundation.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_HANDLE_H__
#define __MONO_HANDLE_H__

#include <config.h>
#include <glib.h>

#include <mono/metadata/object.h>
#include <mono/metadata/class.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/checked-build.h>

G_BEGIN_DECLS


/*
Handle stack.

The handle stack is designed so it's efficient to pop a large amount of entries at once.
The stack is made out of a series of fixed size segments.

To do bulk operations you use a stack mark.
	
*/

/*
3 is the number of fields besides the data in the struct;
128 words makes each chunk 512 or 1024 bytes each
*/
#define OBJECTS_PER_HANDLES_CHUNK (128 - 3)

/*
Whether this config needs stack watermark recording to know where to start scanning from.
*/
#ifdef HOST_WATCHOS
#define MONO_NEEDS_STACK_WATERMARK 1
#endif

typedef struct _HandleChunk HandleChunk;

struct _HandleChunk {
	int size; //number of bytes
	HandleChunk *prev, *next;
	MonoObject *objects [OBJECTS_PER_HANDLES_CHUNK];
};

typedef struct {
	HandleChunk *top; //alloc from here
	HandleChunk *bottom; //scan from here
} HandleStack;

typedef struct {
	int size;
	HandleChunk *chunk;
} HandleStackMark;

typedef void *MonoRawHandle;

typedef void (*GcScanFunc) (gpointer*, gpointer);


MonoRawHandle mono_handle_new (MonoObject *object);

void mono_handle_stack_scan (HandleStack *stack, GcScanFunc func, gpointer gc_data);
HandleStack* mono_handle_stack_alloc (void);
void mono_handle_stack_free (HandleStack *handlestack);
MonoRawHandle mono_stack_mark_pop_value (MonoThreadInfo *info, HandleStackMark *stackmark, MonoRawHandle value);
void mono_stack_mark_record_size (MonoThreadInfo *info, HandleStackMark *stackmark, const char *func_name);

static inline void
mono_stack_mark_init (MonoThreadInfo *info, HandleStackMark *stackmark)
{
	HandleStack *handles = (HandleStack *)info->handle_stack;
	stackmark->size = handles->top->size;
	stackmark->chunk = handles->top;
}

static inline void
mono_stack_mark_pop (MonoThreadInfo *info, HandleStackMark *stackmark)
{
	HandleStack *handles = (HandleStack *)info->handle_stack;
	handles->top = stackmark->chunk;
	handles->top->size = stackmark->size;
}

/*
Icall macros
*/
#define SETUP_ICALL_COMMON	\
	do { \
		MonoError error;	\
		MonoThreadInfo *__info = mono_thread_info_current ();	\
		error_init (&error);	\

#define CLEAR_ICALL_COMMON	\
	mono_error_set_pending_exception (&error);

#define SETUP_ICALL_FRAME	\
	HandleStackMark __mark;	\
	mono_stack_mark_init (__info, &__mark);

#define CLEAR_ICALL_FRAME	\
	mono_stack_mark_record_size (__info, &__mark, __FUNCTION__);	\
	mono_stack_mark_pop (__info, &__mark);

#ifdef MONO_NEEDS_STACK_WATERMARK

static void
mono_thread_info_pop_stack_mark (MonoThreadInfo *info, void *old_mark)
{
	info->stack_mark = old_mark;
}

static void*
mono_thread_info_push_stack_mark (MonoThreadInfo *info, void *mark)
{
	void *old = info->stack_mark;
	info->stack_mark = mark;
	return old;
}

#define SETUP_STACK_WATERMARK	\
	int __dummy;	\
	__builtin_unwind_init ();	\
	void *__old_stack_mark = mono_thread_info_push_stack_mark (__info, &__dummy);

#define CLEAR_STACK_WATERMARK	\
	mono_thread_info_pop_stack_mark (__info, __old_stack_mark);

#else
#define SETUP_STACK_WATERMARK
#define CLEAR_STACK_WATERMARK
#endif

#define ICALL_ENTRY()	\
	SETUP_ICALL_COMMON	\
	SETUP_ICALL_FRAME	\
	SETUP_STACK_WATERMARK

#define ICALL_RETURN()	\
	do {	\
		CLEAR_STACK_WATERMARK	\
		CLEAR_ICALL_COMMON	\
		CLEAR_ICALL_FRAME	\
		return;	\
	} while (0); } while (0)

#define ICALL_RETURN_VAL(VAL)	\
	do {	\
		CLEAR_STACK_WATERMARK	\
		CLEAR_ICALL_COMMON	\
		CLEAR_ICALL_FRAME	\
		return VAL;	\
	} while (0); } while (0)

#define ICALL_RETURN_OBJ(HANDLE)	\
	do {	\
		CLEAR_STACK_WATERMARK	\
		CLEAR_ICALL_COMMON	\
		void* __ret = MONO_HANDLE_RAW (HANDLE);	\
		CLEAR_ICALL_FRAME	\
		return __ret;	\
	} while (0); } while (0)

/*
Handle macros/functions
*/

#ifdef ENABLE_CHECKED_BUILD
void mono_handle_verify (MonoRawHandle handle);
#define HANDLE_INVARIANTS(H) mono_handle_verify((void*)(H))
#else
#define HANDLE_INVARIANTS(H) (0)
#endif

#define TYPED_HANDLE_PAYLOAD_NAME(TYPE) TYPE ## HandlePayload
#define TYPED_HANDLE_NAME(TYPE) TYPE ## Handle

/*
 * TYPED_HANDLE_DECL(SomeType):
 *   Expands to a decl for handles to SomeType and to an internal payload struct.
 *
 * For example, TYPED_HANDLE_DECL(MonoObject) (see below) expands to:
 *
 * typedef struct {
 *   MonoObject *__obj;
 * } MonoObjectHandlePayload;
 *
 * typedef MonoObjectHandlePayload* MonoObjectHandle;
 */
#define TYPED_HANDLE_DECL(TYPE) typedef struct { TYPE *__obj; } TYPED_HANDLE_PAYLOAD_NAME (TYPE) ; typedef TYPED_HANDLE_PAYLOAD_NAME (TYPE) * TYPED_HANDLE_NAME (TYPE)
/* Have to double expand because MONO_STRUCT_OFFSET is doing token pasting on cross-compilers. */
#define MONO_HANDLE_PAYLOAD_OFFSET_(PayloadType) MONO_STRUCT_OFFSET(PayloadType, __obj)
#define MONO_HANDLE_PAYLOAD_OFFSET(TYPE) MONO_HANDLE_PAYLOAD_OFFSET_(TYPED_HANDLE_PAYLOAD_NAME (TYPE))

#define MONO_HANDLE_INIT ((void*) mono_null_value_handle)
#define NULL_HANDLE mono_null_value_handle

//XXX add functions to get/set raw, set field, set field to null, set array, set array to null
#define MONO_HANDLE_RAW(HANDLE) (HANDLE_INVARIANTS (HANDLE), ((HANDLE)->__obj))
#define MONO_HANDLE_DCL(TYPE, NAME) TYPED_HANDLE_NAME(TYPE) NAME = (TYPED_HANDLE_NAME(TYPE))(mono_handle_new ((MonoObject*)(NAME ## _raw)))
#define MONO_HANDLE_NEW(TYPE, VALUE) (TYPED_HANDLE_NAME(TYPE))( mono_handle_new ((MonoObject*)(VALUE)) )
#define MONO_HANDLE_CAST(TYPE, VALUE) (TYPED_HANDLE_NAME(TYPE))( VALUE )

/*
WARNING WARNING WARNING

The following functions require a particular evaluation ordering to ensure correctness.
We must not have exposed handles while any sort of evaluation is happening as that very evaluation might trigger
a safepoint and break us.

This is why we evaluate index and value before any call to MONO_HANDLE_RAW or other functions that deal with naked objects.
*/
#define MONO_HANDLE_SETRAW(HANDLE, FIELD, VALUE) do {	\
		MonoObject *__val = (MonoObject*)(VALUE);	\
		MONO_OBJECT_SETREF (MONO_HANDLE_RAW (HANDLE), FIELD, __val);	\
	} while (0);

#define MONO_HANDLE_SET(HANDLE, FIELD, VALUE) do {	\
		MonoObjectHandle __val = MONO_HANDLE_CAST (MonoObject, VALUE);	\
		MONO_OBJECT_SETREF (MONO_HANDLE_RAW (HANDLE), FIELD, MONO_HANDLE_RAW (__val));	\
	} while (0);

/* VS doesn't support typeof :( :( :( */
#define MONO_HANDLE_SETVAL(HANDLE, FIELD, TYPE, VALUE) do {	\
		TYPE __val = (VALUE);	\
		MONO_HANDLE_RAW (HANDLE)->FIELD = __val;	\
	 } while (0)

#define MONO_HANDLE_ARRAY_SETREF(HANDLE, IDX, VALUE) do {	\
		int __idx = (IDX);	\
   		MonoObjectHandle __val = MONO_HANDLE_CAST (MonoObject, VALUE);		\
		mono_array_setref_fast (MONO_HANDLE_RAW (HANDLE), __idx, MONO_HANDLE_RAW (__val));	\
	} while (0)

#define MONO_HANDLE_ARRAY_SETRAW(HANDLE, IDX, VALUE) do {	\
		int __idx = (IDX);	\
		MonoObject *__val = (MonoObject*)(VALUE);	\
		mono_array_setref_fast (MONO_HANDLE_RAW (HANDLE), __idx, __val);	\
	} while (0)


#define MONO_HANDLE_DOMAIN(HANDLE) (mono_object_domain (MONO_HANDLE_RAW (MONO_HANDLE_CAST (MonoObject, HANDLE))))

/* Baked typed handles we all want */
TYPED_HANDLE_DECL (MonoString);
TYPED_HANDLE_DECL (MonoArray);
TYPED_HANDLE_DECL (MonoObject);

#define NULL_HANDLE_STRING MONO_HANDLE_CAST(MonoString, NULL_HANDLE)

/*
This is the constant for a handle that points nowhere.
Init values to it.
*/
extern const MonoObjectHandle mono_null_value_handle;


//FIXME this should go somewhere else
MonoStringHandle mono_string_new_handle (MonoDomain *domain, const char *data, MonoError *error);
MonoArrayHandle mono_array_new_handle (MonoDomain *domain, MonoClass *eclass, uintptr_t n, MonoError *error);

G_END_DECLS

#endif /* __MONO_HANDLE_H__ */
