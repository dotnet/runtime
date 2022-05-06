// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/**
 * \file
 * Low-level threading for Webassembly
 */

#ifndef __MONO_THREADS_WASM_H__
#define __MONO_THREADS_WASM_H__

#include <glib.h>

#ifdef HOST_WASM

/*
 * declared in mono-threads.h
 *
 * gboolean
 * mono_threads_platform_is_main_thread (void);
 */

gboolean
mono_threads_wasm_is_browser_thread (void);

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
#endif /* DISABLE_THREADS */

#endif /* HOST_WASM*/

#endif /* __MONO_THREADS_WASM_H__ */
