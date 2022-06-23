// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif

MONO_API_FUNCTION(void, mono_thread_init, (MonoThreadStartCB start_cb, MonoThreadAttachCB attach_cb))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_thread_cleanup, (void))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_thread_manage, (void))

MONO_API_FUNCTION(MonoThread *, mono_thread_current, (void))

MONO_API_FUNCTION(void, mono_thread_set_main, (MonoThread *thread))
MONO_API_FUNCTION(MonoThread *, mono_thread_get_main, (void))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_thread_stop, (MonoThread *thread))

MONO_API_FUNCTION(void, mono_thread_new_init, (intptr_t tid, void* stack_start, void* func))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_thread_create, (MonoDomain *domain, void* func, void* arg))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoThread *, mono_thread_attach, (MonoDomain *domain))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_thread_detach, (MonoThread *thread))
MONO_API_FUNCTION(void, mono_thread_exit, (void))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_threads_attach_tools_thread, (void))

MONO_API_FUNCTION(char *, mono_thread_get_name_utf8, (MonoThread *thread))
MONO_API_FUNCTION(int32_t, mono_thread_get_managed_id, (MonoThread *thread))

MONO_API_FUNCTION(void, mono_thread_set_manage_callback, (MonoThread *thread, MonoThreadManageCallback func))

MONO_API_FUNCTION(void, mono_threads_set_default_stacksize, (uint32_t stacksize))
MONO_API_FUNCTION(uint32_t, mono_threads_get_default_stacksize, (void))

MONO_API_FUNCTION(void, mono_threads_request_thread_dump, (void))

MONO_API_FUNCTION(mono_bool, mono_thread_is_foreign, (MonoThread *thread))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY mono_bool, mono_thread_detach_if_exiting, (void))
