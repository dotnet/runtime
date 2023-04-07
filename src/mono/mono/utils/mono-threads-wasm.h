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

#if defined(HOST_BROWSER) && !defined(DISABLE_THREADS)
#include <emscripten/version.h>
/* for Emscripten < 3.1.33,
 * emscripten_runtime_keepalive_push()/emscripten_runtime_keepalive_pop()/emscripten_keepalive_check()
 * are no-ops when -sNO_EXIT_RUNTIME=1 (the default).  Do our own bookkeeping when we can.  Note
 * that this is a HACK that is very sensitive to code that actually cares about this bookkeeping.
 *
 * Specifically we need https://github.com/emscripten-core/emscripten/commit/0c2f5896b839e25fee9763a9ac9c619f359988f4
 */
#if (__EMSCRIPTEN_major__ < 3) || (__EMSCRIPTEN_major__ == 3 && __EMSCRIPTEN_minor__ < 1) || (__EMSCRIPTEN_major__ == 3 && __EMSCRIPTEN_minor__ == 1 && __EMSCRIPTEN_tiny__ < 33)
#define MONO_EMSCRIPTEN_KEEPALIVE_WORKAROUND_HACK 1
#endif
#endif /*HOST_BROWSER && !DISABLE_THREADS*/

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
