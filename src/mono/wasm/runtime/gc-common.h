#define PVOLATILE(T) T* volatile
#define PPVOLATILE(T) T* volatile *

#define gpointer void*

MONO_API MONO_RT_EXTERNAL_ONLY gpointer
mono_threads_enter_gc_unsafe_region (gpointer* stackdata);

MONO_API MONO_RT_EXTERNAL_ONLY void
mono_threads_exit_gc_unsafe_region (gpointer cookie, gpointer* stackdata);

MONO_API MONO_RT_EXTERNAL_ONLY void
mono_threads_assert_gc_unsafe_region (void);

MONO_API MONO_RT_EXTERNAL_ONLY gpointer
mono_threads_enter_gc_safe_region (gpointer *stackdata);

MONO_API MONO_RT_EXTERNAL_ONLY void
mono_threads_exit_gc_safe_region (gpointer cookie, gpointer *stackdata);

MONO_API void
mono_threads_assert_gc_safe_region (void);

#ifndef DISABLE_THREADS
#define MONO_ENTER_GC_UNSAFE	\
	do {	\
		gpointer __dummy;	\
		gpointer __gc_unsafe_cookie = mono_threads_enter_gc_unsafe_region (&__dummy)	\

#define MONO_EXIT_GC_UNSAFE	\
		mono_threads_exit_gc_unsafe_region	(__gc_unsafe_cookie, &__dummy);	\
	} while (0)

#define MONO_ENTER_GC_SAFE	\
	do {	\
		gpointer __dummy;	\
		gpointer __gc_safe_cookie = mono_threads_enter_gc_safe_region (&__dummy)	\

#define MONO_EXIT_GC_SAFE	\
		mono_threads_exit_gc_safe_region (__gc_safe_cookie, &__dummy);	\
	} while (0)

#else /* DISABLE_THREADS */

#define MONO_ENTER_GC_UNSAFE	do {

#define MONO_EXIT_GC_UNSAFE	(void)0; } while (0)

#define MONO_ENTER_GC_SAFE	do {

#define MONO_EXIT_GC_SAFE	(void)0; } while (0)

#endif /* DISABLE_THREADS */

static void
store_volatile (PPVOLATILE(MonoObject) destination, PVOLATILE(MonoObject) source) {
	mono_gc_wbarrier_generic_store_atomic((void*)destination, (MonoObject*)source);
}

static void
copy_volatile (PPVOLATILE(MonoObject) destination, PPVOLATILE(MonoObject) source) {
	mono_gc_wbarrier_generic_store_atomic((void*)destination, (MonoObject*)(*source));
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_register_root (char *start, size_t size, const char *name);

EMSCRIPTEN_KEEPALIVE void
mono_wasm_deregister_root (char *addr);