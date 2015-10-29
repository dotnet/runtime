/*
 * mono-threads-api.h: Low level access to thread state.
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2015 Xamarin
 */

#ifndef __MONO_THREADS_API_H__
#define __MONO_THREADS_API_H__

#include <mono/utils/mono-publib.h>
MONO_BEGIN_DECLS

/*
>>>> WARNING WARNING WARNING <<<<

This API is experimental. It will eventually be required to properly use the rest of the raw-omp embedding API.
*/

/* Don't use those directly, use the MONO_(BEGIN|END)_EFRAME */
MONO_API void* mono_threads_enter_gc_unsafe_region (void* stackdata);
MONO_API void mono_threads_exit_gc_unsafe_region (void *region_cookie, void* stackdata);

/*
Use those macros to limit regions of code that interact with managed memory or use the embedding API.
This will put the current thread in GC Unsafe mode.

For further explanation of what can and can't be done in GC unsafe mode:
http://www.mono-project.com/docs/advanced/runtime/docs/coop-suspend/#gc-unsafe-mode

*/
#define MONO_BEGIN_EFRAME { void *__dummy; void *__region_cookie = mono_threads_enter_gc_unsafe_region (&__dummy);
#define MONO_END_EFRAME mono_threads_exit_gc_unsafe_region (__region_cookie, &__dummy); }


MONO_END_DECLS

#endif /* __MONO_LOGGER_H__ */
