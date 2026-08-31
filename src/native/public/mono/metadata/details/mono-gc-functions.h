// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif

MONO_API_FUNCTION(void, mono_gc_collect, (int generation))
MONO_API_FUNCTION(int, mono_gc_max_generation, (void))
MONO_API_FUNCTION(int, mono_gc_get_generation, (MonoObject *object))
MONO_API_FUNCTION(int, mono_gc_collection_count, (int generation))
MONO_API_FUNCTION(int64_t, mono_gc_get_generation_size, (int generation))
MONO_API_FUNCTION(int64_t, mono_gc_get_used_size, (void))
MONO_API_FUNCTION(int64_t, mono_gc_get_heap_size, (void))
MONO_API_FUNCTION(MonoBoolean, mono_gc_pending_finalizers, (void))
MONO_API_FUNCTION(void, mono_gc_finalize_notify, (void))
MONO_API_FUNCTION(int, mono_gc_invoke_finalizers, (void))
/* heap walking is only valid in the pre-stop-world event callback */
MONO_API_FUNCTION(int, mono_gc_walk_heap, (int flags, MonoGCReferences callback, void *data))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_gc_init_finalizer_thread, (void))
