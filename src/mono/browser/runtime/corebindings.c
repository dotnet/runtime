// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <emscripten.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <assert.h>
#include <sys/types.h>
#include <uchar.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/class.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/assembly.h>
#include <mono/jit/jit.h>

#include "wasm-config.h"
#include "gc-common.h"

//JS funcs
extern void SystemInteropJS_ReleaseCSOwnedObject (int js_handle);
extern void SystemInteropJS_ResolveOrRejectPromise (void *args);
extern void SystemInteropJS_CancelPromise (int task_holder_gc_handle);
extern void SystemJS_ConsoleClear ();
extern void mono_wasm_set_entrypoint_breakpoint (int entry_point_metadata_token);
extern void mono_wasm_trace_logger (const char *log_domain, const char *log_level, const char *message, mono_bool fatal, void *user_data);
extern void SystemInteropJS_InvokeJSFunction (int function_js_handle, void *args);

extern int mono_runtime_run_module_cctor (MonoImage *image, MonoError *error);

typedef void (*background_job_cb)(void);
typedef int (*ds_job_cb)(void* data);

#ifndef DISABLE_THREADS
void SystemInteropJS_ReleaseCSOwnedObjectPost (pthread_t target_tid, int js_handle);
void SystemInteropJS_ResolveOrRejectPromisePost (pthread_t target_tid, void *args);
void SystemInteropJS_CancelPromisePost (pthread_t target_tid, int task_holder_gc_handle);

extern void SystemInteropJS_InstallWebWorkerInteropImpl (int context_gc_handle);
void SystemInteropJS_InstallWebWorkerInterop (int context_gc_handle, void* beforeSyncJSImport, void* afterSyncJSImport, void* pumpHandler);
extern void SystemInteropJS_UninstallWebWorkerInterop ();
extern void SystemInteropJS_InvokeJSImportSync (void* signature, void* args);
void SystemInteropJS_InvokeJSImportAsyncPost (pthread_t target_tid, void* signature, void* args);
void SystemInteropJS_InvokeJSImportSyncSend (pthread_t target_tid, void* signature, void* args);
void SystemInteropJS_InvokeJSFunctionSend (pthread_t target_tid, int function_js_handle, void *args);
extern void mono_threads_wasm_async_run_in_target_thread_vi (pthread_t target_thread, void (*func) (gpointer), gpointer user_data1);
extern void mono_threads_wasm_async_run_in_target_thread_vii (pthread_t target_thread, void (*func) (gpointer, gpointer), gpointer user_data1, gpointer user_data2);
extern void mono_threads_wasm_sync_run_in_target_thread_vii (pthread_t target_thread, void (*func) (gpointer, gpointer), gpointer user_data1, gpointer args);
extern void SystemJS_WarnAboutBlockingWait (void* ptr, int32_t length);
#else
extern void* SystemInteropJS_BindJSImportST (void *signature);
extern void SystemInteropJS_InvokeJSImportST (int function_handle, void *args);
#endif /* DISABLE_THREADS */

// JS-based globalization
extern char16_t* SystemJS_GetLocaleInfo (const uint16_t* locale, int32_t localeLength, const uint16_t* culture, int32_t cultureLength, const uint16_t* result, int32_t resultMaxLength, int *resultLength);

static MonoAssembly* _mono_wasm_assembly_load (char *assembly_name)
{
	assert (assembly_name);
	MonoImageOpenStatus status;
	MonoAssemblyName* aname = mono_assembly_name_new (assembly_name);
	assert (aname);

	MonoAssembly *res = mono_assembly_load (aname, NULL, &status);
	mono_assembly_name_free (aname);
	free (assembly_name);

	return res;
}

#ifndef DISABLE_THREADS

void* before_sync_js_import;
void* after_sync_js_import;
void* synchronization_context_pump_handler;

void SystemInteropJS_InstallWebWorkerInterop (int context_gc_handle, void* beforeSyncJSImport, void* afterSyncJSImport, void* pumpHandler)
{
	before_sync_js_import = beforeSyncJSImport;
	after_sync_js_import = afterSyncJSImport;
	synchronization_context_pump_handler = pumpHandler;
	SystemInteropJS_InstallWebWorkerInteropImpl (context_gc_handle);
}

// async
void SystemInteropJS_ReleaseCSOwnedObjectPost (pthread_t target_tid, int js_handle)
{
	mono_threads_wasm_async_run_in_target_thread_vi (target_tid, (void (*) (gpointer))SystemInteropJS_ReleaseCSOwnedObject, (gpointer)js_handle);
}

// async
void SystemInteropJS_ResolveOrRejectPromisePost (pthread_t target_tid, void* args)
{
	mono_threads_wasm_async_run_in_target_thread_vi (target_tid, (void (*) (gpointer))SystemInteropJS_ResolveOrRejectPromise, (gpointer)args);
}

// async
void SystemInteropJS_CancelPromisePost (pthread_t target_tid, int task_holder_gc_handle)
{
	mono_threads_wasm_async_run_in_target_thread_vi (target_tid, (void (*) (gpointer))SystemInteropJS_CancelPromise, (gpointer)task_holder_gc_handle);
}

// async
void SystemInteropJS_InvokeJSImportAsyncPost (pthread_t target_tid, void* signature, void* args)
{
	mono_threads_wasm_async_run_in_target_thread_vii (target_tid, (void (*) (gpointer, gpointer))SystemInteropJS_InvokeJSImportSync, (gpointer)signature, (gpointer)args);
}

// sync
void SystemInteropJS_InvokeJSImportSyncSend (pthread_t target_tid, void* signature, void* args)
{
	mono_threads_wasm_sync_run_in_target_thread_vii (target_tid, (void (*) (gpointer, gpointer))SystemInteropJS_InvokeJSImportSync, (gpointer)signature, (gpointer)args);
}

// sync
void SystemInteropJS_InvokeJSFunctionSend (pthread_t target_tid, int function_js_handle, void *args)
{
	mono_threads_wasm_sync_run_in_target_thread_vii (target_tid, (void (*) (gpointer, gpointer))SystemInteropJS_InvokeJSFunction, (gpointer)function_js_handle, (gpointer)args);
}

#endif /* DISABLE_THREADS */
