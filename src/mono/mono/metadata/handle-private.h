#ifndef __MONO_HANDLE_PRIVATE_H__
#define __MONO_HANDLE_PRIVATE_H__

#include <mono/metadata/handle.h>

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

#endif/*__MONO_HANDLE_PRIVATE_H__*/
