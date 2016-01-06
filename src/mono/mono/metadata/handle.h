/*
 * handle.h: Handle to object in native code
 *
 * Authors:
 *  - Ludovic Henry <ludovic@xamarin.com>
 *
 * Copyright 2015 Xamarin, Inc. (www.xamarin.com)
 */

#ifndef __MONO_HANDLE_H__
#define __MONO_HANDLE_H__

#include <config.h>
#include <glib.h>

#include "object.h"
#include "class.h"
#include "class-internals.h"
#include "threads-types.h"
#include "handle-arena.h"

#include "mono/utils/mono-threads-coop.h"

G_BEGIN_DECLS

/*
 * DO NOT ACCESS DIRECTLY
 * USE mono_handle_obj BELOW TO ACCESS OBJ
 *
 * The field obj is not private as there is no way to do that
 * in C, but using a C++ template would simplify that a lot
 */
typedef struct {
	MonoObject *obj;
} MonoHandleStorage;

typedef MonoHandleStorage* MonoHandle;

typedef struct _MonoHandleArena MonoHandleArena;

gsize
mono_handle_arena_size (void);

MonoHandle
mono_handle_arena_new (MonoHandleArena *arena, MonoObject *obj);

MonoHandle
mono_handle_arena_elevate (MonoHandleArena *arena, MonoHandle handle);

void
mono_handle_arena_stack_push (MonoHandleArena **arena_stack, MonoHandleArena *arena);

void
mono_handle_arena_stack_pop (MonoHandleArena **arena_stack, MonoHandleArena *arena);

void
mono_handle_arena_initialize (MonoHandleArena **arena_stack);

void
mono_handle_arena_deinitialize (MonoHandleArena **arena_stack);

MonoHandleArena*
mono_handle_arena_current (void);

MonoHandleArena**
mono_handle_arena_current_addr (void);

static inline MonoHandle
mono_handle_new (MonoObject *obj)
{
	return mono_handle_arena_new (mono_handle_arena_current (), obj);
}

static inline MonoHandle
mono_handle_elevate (MonoHandle handle)
{
	return mono_handle_arena_elevate (mono_handle_arena_current (), handle);
}

#ifndef CHECKED_BUILD

#define mono_handle_obj(handle) ((handle)->obj)

#define mono_handle_assign(handle,rawptr) do { (handle)->obj = (rawptr); } while(0)

#else

static inline void
mono_handle_check_in_critical_section ()
{
	MONO_REQ_GC_UNSAFE_MODE;
}

#define mono_handle_obj(handle) (mono_handle_check_in_critical_section (), (handle)->obj)

#define mono_handle_assign(handle,rawptr) do { mono_handle_check_in_critical_section (); (handle)->obj = (rawptr); } while (0)

#endif

static inline MonoClass*
mono_handle_class (MonoHandle handle)
{
	return handle->obj->vtable->klass;
}

static inline MonoDomain*
mono_handle_domain (MonoHandle handle)
{
	return handle->obj->vtable->domain;
}

#define MONO_HANDLE_TYPE_DECL(type)      typedef struct { type *obj; } type ## HandleStorage ; \
	typedef type ## HandleStorage * type ## Handle
#define MONO_HANDLE_TYPE(type)           type ## Handle
#define MONO_HANDLE_NEW(type,obj)        ((type ## Handle) mono_handle_new ((MonoObject*) (obj)))
#define MONO_HANDLE_ELEVATE(type,handle) ((type ## Handle) mono_handle_elevate ((MonoObject*) (handle)->obj))

#define MONO_HANDLE_ASSIGN(handle,rawptr)	\
	do {	\
		mono_handle_assign ((handle), (rawptr));	\
	} while (0)

#define MONO_HANDLE_SETREF(handle,fieldname,value)			\
	do {								\
		MonoHandle __value = (MonoHandle) (value);		\
		MONO_PREPARE_GC_CRITICAL_REGION;					\
		MONO_OBJECT_SETREF (mono_handle_obj ((handle)), fieldname, mono_handle_obj (__value)); \
		MONO_FINISH_GC_CRITICAL_REGION;					\
	} while (0)

#define MONO_HANDLE_SET(handle,fieldname,value)	\
	do {	\
		MONO_PREPARE_GC_CRITICAL_REGION;	\
		mono_handle_obj ((handle))->fieldname = (value);	\
		MONO_FINISH_GC_CRITICAL_REGION;	\
	} while (0)

#define MONO_HANDLE_ARRAY_SETREF(handle,index,value)			\
	do {								\
		MonoHandle __value = (MonoHandle) (value);		\
		MONO_PREPARE_GC_CRITICAL_REGION;					\
		mono_array_setref (mono_handle_obj ((handle)), (index), mono_handle_obj (__value)); \
		MONO_FINISH_GC_CRITICAL_REGION;					\
	} while (0)

#define MONO_HANDLE_ARRAY_SET(handle,type,index,value)	\
	do {	\
		MONO_PREPARE_GC_CRITICAL_REGION;	\
		mono_array_set (mono_handle_obj ((handle)), (type), (index), (value));	\
		MONO_FINISH_GC_CRITICAL_REGION;	\
	} while (0)




/* Some common handle types */

MONO_HANDLE_TYPE_DECL (MonoArray);
MONO_HANDLE_TYPE_DECL (MonoString);

G_END_DECLS

#endif /* __MONO_HANDLE_H__ */
