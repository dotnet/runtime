// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/**
 * \file
 * Low-level threading for Webassembly
 */

#ifndef __MONO_THREADS_WASM_H__
#define __MONO_THREADS_WASM_H__

#include <glib.h>
#include <mono/utils/mono-threads.h>

#ifdef HOST_WASM

/*
 * declared in mono-threads.h
 *
 * gboolean
 * mono_threads_platform_is_main_thread (void);
 */

gboolean
mono_threads_wasm_is_browser_thread (void);

MonoNativeThreadId
mono_threads_wasm_browser_thread_tid (void);

#ifndef DISABLE_THREADS
/**
 * Runs the given function asynchronously on the main thread.
 * See emscripten/threading.h emscripten_async_run_in_main_runtime_thread
 */
void
mono_threads_wasm_async_run_in_main_thread (void (*func) (void));

/*
 * Variant that takes an argument. Add more variants as needed.
 */
void
mono_threads_wasm_async_run_in_main_thread_vi (void (*func)(gpointer), gpointer user_data);

void
mono_threads_wasm_async_run_in_main_thread_vii (void (*func)(gpointer, gpointer), gpointer user_data1, gpointer user_data2);

static inline
int32_t
mono_wasm_atomic_wait_i32 (volatile int32_t *addr, int32_t expected, int32_t timeout_ns)
{
	// Don't call this on the main thread!
	// See https://github.com/WebAssembly/threads/issues/174
	// memory.atomic.wait32
	//
	// timeout_ns == -1 means infinite wait
	//
	// return values:
	// 0 == "ok", thread blocked and was woken up
	// 1 == "not-equal", value at addr was not equal to expected
	// 2 == "timed-out", timeout expired before thread was woken up
	return __builtin_wasm_memory_atomic_wait32((int32_t*)addr, expected, timeout_ns);
}
#endif /* DISABLE_THREADS */

// Called from register_thread when a pthread attaches to the runtime
void
mono_threads_wasm_on_thread_attached (void);

#endif /* HOST_WASM*/

#endif /* __MONO_THREADS_WASM_H__ */
