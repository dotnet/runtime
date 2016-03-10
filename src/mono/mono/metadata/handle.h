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

#include <mono/metadata/object.h>
#include <mono/metadata/class.h>
#include <mono/utils/mono-error.h>
#include <mono/utils/checked-build.h>

G_BEGIN_DECLS

/*
 * DO NOT ACCESS DIRECTLY
 * USE mono_handle_obj BELOW TO ACCESS OBJ
 *
 * The field obj is not private as there is no way to do that
 * in C, but using a C++ template would simplify that a lot
 */
typedef struct {
	MonoObject *__private_obj;
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
mono_handle_arena_init (MonoHandleArena **arena_stack);

void
mono_handle_arena_cleanup (MonoHandleArena **arena_stack);

MonoHandleArena*
mono_handle_arena_current (void);

MonoHandleArena**
mono_handle_arena_current_addr (void);

#define MONO_HANDLE_ARENA_PUSH()	\
	do {	\
		MonoHandleArena **__arena_stack = mono_handle_arena_current_addr ();	\
		MonoHandleArena *__arena = (MonoHandleArena*) g_alloca (mono_handle_arena_size ());	\
		mono_handle_arena_stack_push (__arena_stack, __arena)

#define MONO_HANDLE_ARENA_POP()	\
		mono_handle_arena_stack_pop (__arena_stack, __arena);	\
	} while (0)

#define MONO_HANDLE_ARENA_POP_RETURN_UNSAFE(handle,ret)	\
		(ret) = (handle)->__private_obj;	\
		mono_handle_arena_stack_pop (__arena_stack, __arena);	\
	} while (0)

#define MONO_HANDLE_ARENA_POP_RETURN(handle,ret_handle)	\
		*((MonoHandle**)(&(ret_handle))) = mono_handle_elevate ((MonoHandle*)(handle)); \
		mono_handle_arena_stack_pop(__arena_stack, __arena);	\
	} while (0)

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

#ifndef ENABLE_CHECKED_BUILD

#define mono_handle_obj(handle) ((handle)->__private_obj)

#define mono_handle_assign(handle,rawptr) do { (handle)->__private_obj = (rawptr); } while(0)

#else

static inline void
mono_handle_check_in_critical_section ()
{
	MONO_REQ_GC_CRITICAL;
}

#define mono_handle_obj(handle) (mono_handle_check_in_critical_section (), (handle)->__private_obj)

#define mono_handle_assign(handle,rawptr) do { mono_handle_check_in_critical_section (); (handle)->__private_obj = (rawptr); } while (0)

#endif

static inline MonoClass*
mono_handle_class (MonoHandle handle)
{
	return mono_object_get_class (handle->__private_obj);
}

static inline MonoDomain*
mono_handle_domain (MonoHandle handle)
{
	return mono_object_get_domain (handle->__private_obj);
}

#define mono_handle_obj_is_null(handle) ((handle)->__private_obj == NULL)

#define MONO_HANDLE_TYPE_DECL(type)      typedef struct { type *__private_obj; } type ## HandleStorage ; \
	typedef type ## HandleStorage * type ## Handle
#define MONO_HANDLE_TYPE(type)           type ## Handle
#define MONO_HANDLE_NEW(type,obj)        ((type ## Handle) mono_handle_new ((MonoObject*) (obj)))
#define MONO_HANDLE_ELEVATE(type,handle) ((type ## Handle) mono_handle_elevate ((MonoObject*) (handle)->__private_obj))

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

#define MONO_HANDLE_SETREF_NULL(handle,fieldname)			\
	do {								\
		MONO_PREPARE_GC_CRITICAL_REGION;			\
		MONO_OBJECT_SETREF (mono_handle_obj ((handle)), fieldname, NULL); \
		MONO_FINISH_GC_CRITICAL_REGION;				\
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

#define MONO_HANDLE_ARRAY_SETREF_NULL(handle,index)			\
	do {								\
		MONO_PREPARE_GC_CRITICAL_REGION;			\
		mono_array_setref (mono_handle_obj ((handle)), (index), NULL); \
		MONO_FINISH_GC_CRITICAL_REGION;				\
	} while (0)
	

#define MONO_HANDLE_ARRAY_SET(handle,type,index,value)	\
	do {	\
		MONO_PREPARE_GC_CRITICAL_REGION;	\
		mono_array_set (mono_handle_obj ((handle)), type, (index), (value));	\
		MONO_FINISH_GC_CRITICAL_REGION;	\
	} while (0)




/* Some common handle types */

MONO_HANDLE_TYPE_DECL (MonoArray);
MONO_HANDLE_TYPE_DECL (MonoString);

G_END_DECLS

#endif /* __MONO_HANDLE_H__ */
