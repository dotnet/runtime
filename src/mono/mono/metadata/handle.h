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

/* define MONO_HANDLE_TRACK_OWNER to store the file and line number of each call to MONO_HANDLE_NEW
 * in the handle stack.  (This doubles the amount of memory used for handles, so it's only useful for debugging).
 */
/*#define MONO_HANDLE_TRACK_OWNER*/

/* define MONO_HANDLE_TRACK_SP to record the C stack pointer at the time of each HANDLE_FUNCTION_ENTER and
 * to ensure that when a new handle is allocated the previous newest handle is not lower in the stack.
 * This is useful to catch missing HANDLE_FUNCTION_ENTER / HANDLE_FUNCTION_RETURN pairs which could cause
 * handle leaks.
 */
/*#define MONO_HANDLE_TRACK_SP*/

typedef struct {
	gpointer o; /* MonoObject ptr or interior ptr */
#ifdef MONO_HANDLE_TRACK_OWNER
	const char *owner;
#endif
#ifdef MONO_HANDLE_TRACK_SP
	gpointer alloc_sp; /* sp from HandleStack:stackmark_sp at time of allocation */
#endif
} HandleChunkElem;

/* number of guint32's needed to store the interior pointers bitmap */
#define INTERIOR_HANDLE_BITMAP_WORDS ((OBJECTS_PER_HANDLES_CHUNK + 31) / 32)

struct _HandleChunk {
	int size; //number of handles
	/* bits in the range 0..size-1 of interior_bitmap are valid; rest are ignored. */
	guint32 interior_bitmap [INTERIOR_HANDLE_BITMAP_WORDS];
	HandleChunk *prev, *next;
	HandleChunkElem elems [OBJECTS_PER_HANDLES_CHUNK];
};

typedef struct {
	HandleChunk *top; //alloc from here
	HandleChunk *bottom; //scan from here
#ifdef MONO_HANDLE_TRACK_SP
	gpointer stackmark_sp; // C stack pointer top when from most recent mono_stack_mark_init
#endif
} HandleStack;

typedef struct {
	int size;
	HandleChunk *chunk;
#ifdef MONO_HANDLE_TRACK_SP
	gpointer prev_sp; // C stack pointer from prior mono_stack_mark_init
#endif
} HandleStackMark;

typedef void *MonoRawHandle;

typedef void (*GcScanFunc) (gpointer*, gpointer);


#ifndef MONO_HANDLE_TRACK_OWNER
MonoRawHandle mono_handle_new (MonoObject *object);
MonoRawHandle mono_handle_new_full (gpointer rawptr, gboolean interior);
#else
MonoRawHandle mono_handle_new (MonoObject *object, const char* owner);
MonoRawHandle mono_handle_new_full (gpointer rawptr, gboolean interior, const char *owner);
#endif


void mono_handle_stack_scan (HandleStack *stack, GcScanFunc func, gpointer gc_data, gboolean precise);
gboolean mono_handle_stack_is_empty (HandleStack *stack);
HandleStack* mono_handle_stack_alloc (void);
void mono_handle_stack_free (HandleStack *handlestack);
MonoRawHandle mono_stack_mark_pop_value (MonoThreadInfo *info, HandleStackMark *stackmark, MonoRawHandle value);
void mono_stack_mark_record_size (MonoThreadInfo *info, HandleStackMark *stackmark, const char *func_name);

#ifdef MONO_HANDLE_TRACK_SP
void mono_handle_chunk_leak_check (HandleStack *handles);
#endif

static inline void
mono_stack_mark_init (MonoThreadInfo *info, HandleStackMark *stackmark)
{
#ifdef MONO_HANDLE_TRACK_SP
	gpointer sptop = (gpointer)(intptr_t)&stackmark;
#endif
	HandleStack *handles = (HandleStack *)info->handle_stack;
	stackmark->size = handles->top->size;
	stackmark->chunk = handles->top;
#ifdef MONO_HANDLE_TRACK_SP
	stackmark->prev_sp = handles->stackmark_sp;
	handles->stackmark_sp = sptop;
#endif
}

static inline void
mono_stack_mark_pop (MonoThreadInfo *info, HandleStackMark *stackmark)
{
	HandleStack *handles = (HandleStack *)info->handle_stack;
	HandleChunk *old_top = stackmark->chunk;
	old_top->size = stackmark->size;
	mono_memory_write_barrier ();
	handles->top = old_top;
#ifdef MONO_HANDLE_TRACK_SP
	mono_memory_write_barrier (); /* write to top before prev_sp */
	handles->stackmark_sp = stackmark->prev_sp;
#endif
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

#define CLEAR_ICALL_FRAME_VALUE(RESULT, HANDLE)				\
	mono_stack_mark_record_size (__info, &__mark, __FUNCTION__);	\
	(RESULT) = mono_stack_mark_pop_value (__info, &__mark, (HANDLE));


#define HANDLE_FUNCTION_ENTER() do {				\
	MonoThreadInfo *__info = mono_thread_info_current ();	\
	SETUP_ICALL_FRAME					\

#define HANDLE_FUNCTION_RETURN()		\
	CLEAR_ICALL_FRAME;			\
	} while (0)

#define HANDLE_FUNCTION_RETURN_VAL(VAL)		\
	CLEAR_ICALL_FRAME;			\
	return (VAL);				\
	} while (0)

#define HANDLE_FUNCTION_RETURN_OBJ(HANDLE)			\
	do {							\
		void* __result = (MONO_HANDLE_RAW (HANDLE));	\
		CLEAR_ICALL_FRAME;				\
		return __result;				\
	} while (0); } while (0);

#define HANDLE_FUNCTION_RETURN_REF(TYPE, HANDLE)			\
	do {								\
		MonoRawHandle __result;					\
		CLEAR_ICALL_FRAME_VALUE (__result, ((MonoRawHandle) (HANDLE))); \
		return MONO_HANDLE_CAST (TYPE, __result);		\
	} while (0); } while (0);

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
#define TYPED_OUT_HANDLE_NAME(TYPE) TYPE ## HandleOut

#ifdef MONO_HANDLE_TRACK_OWNER
#define STRINGIFY_(x) #x
#define STRINGIFY(x) STRINGIFY_(x)
#define HANDLE_OWNER_STRINGIFY(file,lineno) (const char*) (file ":" STRINGIFY(lineno))
#endif


/*
 * TYPED_HANDLE_DECL(SomeType):
 *   Expands to a decl for handles to SomeType and to an internal payload struct.
 *
 * For example, TYPED_HANDLE_DECL(MonoObject) (see below) expands to:
 *
 * typedef struct {
 *   MonoObject *__raw;
 * } MonoObjectHandlePayload;
 *
 * typedef MonoObjectHandlePayload* MonoObjectHandle;
 * typedef MonoObjectHandlePayload* MonoObjectHandleOut;
 */
#define TYPED_HANDLE_DECL(TYPE)						\
	typedef struct { TYPE *__raw; } TYPED_HANDLE_PAYLOAD_NAME (TYPE) ; \
	typedef TYPED_HANDLE_PAYLOAD_NAME (TYPE) * TYPED_HANDLE_NAME (TYPE); \
	typedef TYPED_HANDLE_PAYLOAD_NAME (TYPE) * TYPED_OUT_HANDLE_NAME (TYPE)
/*
 * TYPED_VALUE_HANDLE_DECL(SomeType):
 *   Expands to a decl for handles to SomeType (which is a managed valuetype (likely a struct) of some sort) and to an internal payload struct.
 * For example TYPED_HANDLE_DECL(MonoMethodInfo) expands to:
 *
 * typedef struct {
 *   MonoMethodInfo *__raw;
 * } MonoMethodInfoHandlePayload;
 * typedef MonoMethodInfoHandlePayload* MonoMethodInfoHandle;
 */
#define TYPED_VALUE_HANDLE_DECL(TYPE) TYPED_HANDLE_DECL(TYPE)

/* Have to double expand because MONO_STRUCT_OFFSET is doing token pasting on cross-compilers. */
#define MONO_HANDLE_PAYLOAD_OFFSET_(PayloadType) MONO_STRUCT_OFFSET(PayloadType, __raw)
#define MONO_HANDLE_PAYLOAD_OFFSET(TYPE) MONO_HANDLE_PAYLOAD_OFFSET_(TYPED_HANDLE_PAYLOAD_NAME (TYPE))

#define MONO_HANDLE_INIT ((void*) mono_null_value_handle)
#define NULL_HANDLE mono_null_value_handle

//XXX add functions to get/set raw, set field, set field to null, set array, set array to null
#define MONO_HANDLE_RAW(HANDLE) (HANDLE_INVARIANTS (HANDLE), ((HANDLE)->__raw))
#define MONO_HANDLE_DCL(TYPE, NAME) TYPED_HANDLE_NAME(TYPE) NAME = MONO_HANDLE_NEW (TYPE, (NAME ## _raw))

#ifndef MONO_HANDLE_TRACK_OWNER
#define MONO_HANDLE_NEW(TYPE, VALUE) (TYPED_HANDLE_NAME(TYPE))( mono_handle_new ((MonoObject*)(VALUE)) )
#else
#define MONO_HANDLE_NEW(TYPE, VALUE) (TYPED_HANDLE_NAME(TYPE))( mono_handle_new ((MonoObject*)(VALUE), HANDLE_OWNER_STRINGIFY(__FILE__, __LINE__)))
#endif

#define MONO_HANDLE_CAST(TYPE, VALUE) (TYPED_HANDLE_NAME(TYPE))( VALUE )

#define MONO_HANDLE_IS_NULL(HANDLE) (MONO_HANDLE_RAW(HANDLE) == NULL)


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
	} while (0)

#define MONO_HANDLE_SET(HANDLE, FIELD, VALUE) do {	\
		MonoObjectHandle __val = MONO_HANDLE_CAST (MonoObject, VALUE);	\
		MONO_OBJECT_SETREF (MONO_HANDLE_RAW (HANDLE), FIELD, MONO_HANDLE_RAW (__val));	\
	} while (0)

/* N.B. RESULT is evaluated before HANDLE */
#define MONO_HANDLE_GET(RESULT, HANDLE, FIELD) do {			\
		MonoObjectHandle __dest = MONO_HANDLE_CAST(MonoObject, RESULT);	\
		mono_gc_wbarrier_generic_store (&__dest->__raw,  (MonoObject*)(MONO_HANDLE_RAW(HANDLE)->FIELD)); \
	} while (0)

#define MONO_HANDLE_NEW_GET(TYPE,HANDLE,FIELD) (MONO_HANDLE_NEW(TYPE,MONO_HANDLE_RAW(HANDLE)->FIELD))

#define MONO_HANDLE_GETVAL(HANDLE, FIELD) (MONO_HANDLE_RAW(HANDLE)->FIELD)

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

#define MONO_HANDLE_ARRAY_SETVAL(HANDLE, TYPE, IDX, VALUE) do {		\
		int __idx = (IDX);					\
   		TYPE __val = (VALUE);			\
		mono_array_set (MONO_HANDLE_RAW (HANDLE), TYPE, __idx, __val); \
	} while (0)

#define MONO_HANDLE_ARRAY_SETRAW(HANDLE, IDX, VALUE) do {	\
		int __idx = (IDX);	\
		MonoObject *__val = (MonoObject*)(VALUE);	\
		mono_array_setref_fast (MONO_HANDLE_RAW (HANDLE), __idx, __val); \
	} while (0)

/* N.B. DEST is evaluated AFTER all the other arguments */
#define MONO_HANDLE_ARRAY_GETVAL(DEST, HANDLE, TYPE, IDX) do {		\
		MonoArrayHandle __arr = (HANDLE);			\
		int __idx = (IDX);					\
		TYPE __result = mono_array_get (MONO_HANDLE_RAW(__arr), TYPE, __idx); \
		(DEST) =  __result;					\
	} while (0)

#define MONO_HANDLE_ARRAY_GETREF(DEST, HANDLE, IDX) do {		\
		mono_handle_array_getref (MONO_HANDLE_CAST(MonoObject, (DEST)), (HANDLE), (IDX)); \
	} while (0)

/* Handles into the interior of objects.
 *
 * Typically when working with value types, we pass them by reference.  In the case where the value type
 * is a field in a managed class, the reference will be a pointer into the middle of a managed object.
 * We need to identify such pointers in order for SGen to scan them correctly.
 */

#ifndef MONO_HANDLE_TRACK_OWNER
#define MONO_HANDLE_NEW_GET_VALPTR(HANDLE,TYPE,FIELD) (TYPE_VALUE_HANDLE_NAME(TYPE))(mono_handle_new_full (&(HANDLE)->__raw->FIELD), TRUE))
#else
#define MONO_HANDLE_NEW_GET_VALPTR(HANDLE,TYPE,FIELD) (TYPE_VALUE_HANDLE_NAME(TYPE))(mono_handle_new_full (&(HANDLE)->__raw->FIELD), TRUE, HANDLE_OWNER_STRINGIFY(__FILE__, __LINE__))
#endif


#define MONO_HANDLE_ASSIGN(DESTH, SRCH)				\
	mono_handle_assign (MONO_HANDLE_CAST (MonoObject, (DESTH)), MONO_HANDLE_CAST(MonoObject, (SRCH)))

#define MONO_HANDLE_DOMAIN(HANDLE) (mono_object_domain (MONO_HANDLE_RAW (MONO_HANDLE_CAST (MonoObject, HANDLE))))


/* Baked typed handles we all want */
TYPED_HANDLE_DECL (MonoString);
TYPED_HANDLE_DECL (MonoArray);
TYPED_HANDLE_DECL (MonoObject);
TYPED_HANDLE_DECL (MonoException);
TYPED_HANDLE_DECL (MonoAppContext);

#define NULL_HANDLE_STRING MONO_HANDLE_CAST(MonoString, NULL_HANDLE)

/*
This is the constant for a handle that points nowhere.
Init values to it.
*/
extern const MonoObjectHandle mono_null_value_handle;

static inline void
mono_handle_assign (MonoObjectHandleOut dest, MonoObjectHandle src)
{
	mono_gc_wbarrier_generic_store (&dest->__raw, src ? MONO_HANDLE_RAW(src) : NULL);
}

//FIXME this should go somewhere else
MonoStringHandle mono_string_new_handle (MonoDomain *domain, const char *data, MonoError *error);
MonoArrayHandle mono_array_new_handle (MonoDomain *domain, MonoClass *eclass, uintptr_t n, MonoError *error);
MonoArrayHandle
mono_array_new_full_handle (MonoDomain *domain, MonoClass *array_class, uintptr_t *lengths, intptr_t *lower_bounds, MonoError *error);


uintptr_t mono_array_handle_length (MonoArrayHandle arr);

static inline void
mono_handle_array_getref (MonoObjectHandleOut dest, MonoArrayHandle array, uintptr_t index)
{
	mono_gc_wbarrier_generic_store (&dest->__raw, mono_array_get (MONO_HANDLE_RAW (array),gpointer, index));
}

#define mono_handle_class(o) mono_object_class (MONO_HANDLE_RAW (o))

/* Local handles to global GC handles and back */

uint32_t
mono_gchandle_from_handle (MonoObjectHandle handle, mono_bool pinned);

MonoObjectHandle
mono_gchandle_get_target_handle (uint32_t gchandle);

void
mono_array_handle_memcpy_refs (MonoArrayHandle dest, uintptr_t dest_idx, MonoArrayHandle src, uintptr_t src_idx, uintptr_t len);

/* Pins the MonoArray using a gchandle and returns a pointer to the
 * element with the given index (where each element is of the given
 * size.  Call mono_gchandle_free to unpin.
 */
gpointer
mono_array_handle_pin_with_size (MonoArrayHandle handle, int size, uintptr_t index, uint32_t *gchandle);

#define MONO_ARRAY_HANDLE_PIN(handle,type,index,gchandle_out) mono_array_handle_pin_with_size (MONO_HANDLE_CAST(MonoArray,(handle)), sizeof (type), (index), (gchandle_out))

void
mono_error_set_exception_handle (MonoError *error, MonoExceptionHandle exc);

MonoAppContextHandle
mono_context_get_handle (void);

void
mono_context_set_handle (MonoAppContextHandle new_context);


G_END_DECLS

#endif /* __MONO_HANDLE_H__ */
