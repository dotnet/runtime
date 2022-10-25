#ifndef __MONO_PROFILER_BROWSER_H__
#define __MONO_PROFILER_BROWSER_H__

#include <config.h>
#include <glib.h>
#include <mono/metadata/profiler.h>

#ifdef HOST_WASM

void
mono_wasm_profiler_enter ();

void
mono_wasm_profiler_leave (MonoMethod *method);

#endif /* HOST_WASM */
#endif /* __MONO_PROFILER_BROWSER_H__ */
