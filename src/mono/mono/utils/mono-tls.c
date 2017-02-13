/**
 * \file
 * Low-level TLS support
 *
 * Thread local variables that are accessed both from native and managed code
 * are defined here and should be accessed only through this APIs
 *
 * Copyright 2013 Xamarin, Inc (http://www.xamarin.com)
 */

#include <mono/utils/mach-support.h>

#include "mono-tls.h"

/*
 * On all platforms we should be able to use either __thread or pthread/TlsGetValue.
 * Certain platforms will support fast tls only when using one of the thread local
 * storage backends. By default this is __thread if we have HAVE_KW_THREAD defined.
 *
 * By default all platforms will call into these native getters whenever they need
 * to get a tls value. On certain platforms we can try to be faster than this and
 * avoid the call. We call this fast tls and each platform defines its own way to
 * achieve this. For this, a platform has to define MONO_ARCH_HAVE_INLINED_TLS,
 * and provide alternative getters/setters for a MonoTlsKey. In order to have fast
 * getter/setters, the platform has to declare a way to fetch an internal offset
 * (MONO_THREAD_VAR_OFFSET) which is stored here, and in the arch specific file
 * probe the system to see if we can use the offset initialized here. If these
 * run-time checks don't succeed we just use the fallbacks.
 *
 * In case we would wish to provide fast inlined tls for aot code, we would need
 * to be sure that, at run-time, these two platform checks would never fail
 * otherwise the tls getter/setters that we emitted would not work. Normally,
 * there is little incentive to support this since tls access is most common in
 * wrappers and managed allocators, both of which are not aot-ed by default.
 * So far, we never supported inlined fast tls on full-aot systems.
 */

#ifdef USE_KW_THREAD

/* tls attribute */
#if HAVE_TLS_MODEL_ATTR

#if defined(__PIC__) && !defined(PIC)
/*
 * Must be compiling -fPIE, for executables.  Build PIC
 * but with initial-exec.
 * http://bugs.gentoo.org/show_bug.cgi?id=165547
 */
#define PIC
#define PIC_INITIAL_EXEC
#endif

/*
 * Define this if you want a faster libmono, which cannot be loaded dynamically as a
 * module.
 */
//#define PIC_INITIAL_EXEC

#if defined(PIC)

#ifdef PIC_INITIAL_EXEC
#define MONO_TLS_FAST __attribute__ ((__tls_model__("initial-exec")))
#else
#if defined (__powerpc__)
/* local dynamic requires a call to __tls_get_addr to look up the
   TLS block address via the Dynamic Thread Vector. In this case Thread
   Pointer relative offsets can't be used as this modules TLS was
   allocated separately (none contiguoiusly) from the initial TLS
   block.

   For now we will disable this. */
#define MONO_TLS_FAST
#else
#define MONO_TLS_FAST __attribute__ ((__tls_model__("local-dynamic")))
#endif
#endif

#else

#define MONO_TLS_FAST __attribute__ ((__tls_model__("local-exec")))

#endif

#else
#define MONO_TLS_FAST
#endif

/* Runtime offset detection */
#if defined(TARGET_AMD64) && !defined(TARGET_MACH) && !defined(HOST_WIN32) /* __thread likely not tested on mac/win */

#if defined(PIC)
// This only works if libmono is linked into the application
#define MONO_THREAD_VAR_OFFSET(var,offset) do { guint64 foo;  __asm ("movq " #var "@GOTTPOFF(%%rip), %0" : "=r" (foo)); offset = foo; } while (0)
#else
#define MONO_THREAD_VAR_OFFSET(var,offset) do { guint64 foo;  __asm ("movq $" #var "@TPOFF, %0" : "=r" (foo)); offset = foo; } while (0)
#endif

#elif defined(TARGET_X86) && !defined(TARGET_MACH) && !defined(HOST_WIN32) && defined(__GNUC__)

#if defined(PIC)
#define MONO_THREAD_VAR_OFFSET(var,offset) do { int tmp; __asm ("call 1f; 1: popl %0; addl $_GLOBAL_OFFSET_TABLE_+[.-1b], %0; movl " #var "@gotntpoff(%0), %1" : "=r" (tmp), "=r" (offset)); } while (0)
#else
#define MONO_THREAD_VAR_OFFSET(var,offset) __asm ("movl $" #var "@ntpoff, %0" : "=r" (offset))
#endif

#elif defined(TARGET_ARM64) && !defined(PIC)

#define MONO_THREAD_VAR_OFFSET(var,offset) \
	__asm ( "mov %0, #0\n add %0, %0, #:tprel_hi12:" #var "\n add %0, %0, #:tprel_lo12_nc:" #var "\n" \
		: "=r" (offset))

#elif defined(TARGET_ARM) && defined(__ARM_EABI__) && !defined(PIC)

#define MONO_THREAD_VAR_OFFSET(var,offset) __asm ("     ldr     %0, 1f; b 2f; 1: .word " #var "(tpoff); 2:" : "=r" (offset))

#elif defined(TARGET_S390X)
# if defined(__PIC__)
#  if !defined(__PIE__)
// This only works if libmono is linked into the application
#   define MONO_THREAD_VAR_OFFSET(var,offset) do { guint64 foo;  				\
						__asm__ ("basr  %%r1,0\n\t"			\
							 "j     0f\n\t"				\
							 ".quad " #var "@TLSGD\n"		\
							 "0:\n\t"				\
							 "lg    %%r2,4(%%r1)\n\t"		\
							 "brasl	%%r14,__tls_get_offset@PLT:tls_gdcall:"#var"\n\t" \
							 "lgr	%0,%%r2\n\t"			\
							: "=r" (foo) : 				\
							: "1", "2", "14", "cc");		\
						offset = foo; } while (0)
#  elif __PIE__ == 1
#   define MONO_THREAD_VAR_OFFSET(var,offset) do { guint64 foo;  					\
						__asm__ ("lg	%0," #var "@GOTNTPOFF(%%r12)\n\t"	\
							 : "=r" (foo));					\
						offset = foo; } while (0)
#  elif __PIE__ == 2
#   define MONO_THREAD_VAR_OFFSET(var,offset) do { guint64 foo;  				\
						__asm__ ("larl	%%r1," #var "@INDNTPOFF\n\t"	\
							 "lg	%0,0(%%r1)\n\t"			\
							 : "=r" (foo) :				\
							 : "1", "cc");				\
						offset = foo; } while (0)
#  endif
# else
#  define MONO_THREAD_VAR_OFFSET(var,offset) do { guint64 foo;  			\
						__asm__ ("basr  %%r1,0\n\t"		\
							 "j     0f\n\t"			\
							 ".quad " #var "@NTPOFF\n"	\
							 "0:\n\t"			\
							 "lg    %0,4(%%r1)\n\t"		\
							: "=r" (foo) : : "1");		\
						offset = foo; } while (0)
# endif
#else

#define MONO_THREAD_VAR_OFFSET(var,offset) (offset) = -1

#endif

/* Tls variables for each MonoTlsKey */

static __thread gpointer mono_tls_thread MONO_TLS_FAST;
static __thread gpointer mono_tls_jit_tls MONO_TLS_FAST;
static __thread gpointer mono_tls_domain MONO_TLS_FAST;
static __thread gpointer mono_tls_lmf MONO_TLS_FAST;
static __thread gpointer mono_tls_sgen_thread_info MONO_TLS_FAST;
static __thread gpointer mono_tls_lmf_addr MONO_TLS_FAST;

#else

#if defined(TARGET_AMD64) && (defined(TARGET_MACH) || defined(HOST_WIN32))
#define MONO_THREAD_VAR_OFFSET(key,offset) (offset) = (gint32)key
#elif defined(TARGET_X86) && (defined(TARGET_MACH) || defined(HOST_WIN32))
#define MONO_THREAD_VAR_OFFSET(key,offset) (offset) = (gint32)key
#else
#define MONO_THREAD_VAR_OFFSET(var,offset) (offset) = -1
#endif

static MonoNativeTlsKey mono_tls_key_thread;
static MonoNativeTlsKey mono_tls_key_jit_tls;
static MonoNativeTlsKey mono_tls_key_domain;
static MonoNativeTlsKey mono_tls_key_sgen_thread_info;
static MonoNativeTlsKey mono_tls_key_lmf_addr;

#endif

static gint32 tls_offsets [TLS_KEY_NUM];

#ifdef USE_KW_THREAD
#define MONO_TLS_GET_VALUE(tls_var,tls_key) (tls_var)
#define MONO_TLS_SET_VALUE(tls_var,tls_key,value) (tls_var = value)
#else
#define MONO_TLS_GET_VALUE(tls_var,tls_key) (mono_native_tls_get_value (tls_key))
#define MONO_TLS_SET_VALUE(tls_var,tls_key,value) (mono_native_tls_set_value (tls_key, value))
#endif

void
mono_tls_init_gc_keys (void)
{
#ifdef USE_KW_THREAD
	MONO_THREAD_VAR_OFFSET (mono_tls_sgen_thread_info, tls_offsets [TLS_KEY_SGEN_THREAD_INFO]);
#else
	mono_native_tls_alloc (&mono_tls_key_sgen_thread_info, NULL);
	MONO_THREAD_VAR_OFFSET (mono_tls_key_sgen_thread_info, tls_offsets [TLS_KEY_SGEN_THREAD_INFO]);
#endif
}

void
mono_tls_init_runtime_keys (void)
{
#ifdef USE_KW_THREAD
	MONO_THREAD_VAR_OFFSET (mono_tls_thread, tls_offsets [TLS_KEY_THREAD]);
	MONO_THREAD_VAR_OFFSET (mono_tls_jit_tls, tls_offsets [TLS_KEY_JIT_TLS]);
	MONO_THREAD_VAR_OFFSET (mono_tls_domain, tls_offsets [TLS_KEY_DOMAIN]);
	MONO_THREAD_VAR_OFFSET (mono_tls_lmf_addr, tls_offsets [TLS_KEY_LMF_ADDR]);
#else
	mono_native_tls_alloc (&mono_tls_key_thread, NULL);
	MONO_THREAD_VAR_OFFSET (mono_tls_key_thread, tls_offsets [TLS_KEY_THREAD]);
	mono_native_tls_alloc (&mono_tls_key_jit_tls, NULL);
	MONO_THREAD_VAR_OFFSET (mono_tls_key_jit_tls, tls_offsets [TLS_KEY_JIT_TLS]);
	mono_native_tls_alloc (&mono_tls_key_domain, NULL);
	MONO_THREAD_VAR_OFFSET (mono_tls_key_domain, tls_offsets [TLS_KEY_DOMAIN]);
	mono_native_tls_alloc (&mono_tls_key_lmf_addr, NULL);
	MONO_THREAD_VAR_OFFSET (mono_tls_key_lmf_addr, tls_offsets [TLS_KEY_LMF_ADDR]);
#endif
}

void
mono_tls_free_keys (void)
{
#ifndef USE_KW_THREAD
	mono_native_tls_free (mono_tls_key_thread);
	mono_native_tls_free (mono_tls_key_jit_tls);
	mono_native_tls_free (mono_tls_key_domain);
	mono_native_tls_free (mono_tls_key_sgen_thread_info);
	mono_native_tls_free (mono_tls_key_lmf_addr);
#endif
}


/*
 * Gets the tls offset associated with the key. This offset is set at key
 * initialization (at runtime). Certain targets can implement computing
 * this offset and using it at runtime for fast inlined tls access.
 */
gint32
mono_tls_get_tls_offset (MonoTlsKey key)
{
	g_assert (tls_offsets [key]);
	return tls_offsets [key];
}

/*
 * Returns the getter (gpointer (*)(void)) for the mono tls key.
 * Managed code will always get the value by calling this getter.
 */
gpointer
mono_tls_get_tls_getter (MonoTlsKey key, gboolean name)
{
	switch (key) {
	case TLS_KEY_THREAD:
		return name ? (gpointer)"mono_tls_get_thread" : (gpointer)mono_tls_get_thread;
	case TLS_KEY_JIT_TLS:
		return name ? (gpointer)"mono_tls_get_jit_tls" : (gpointer)mono_tls_get_jit_tls;
	case TLS_KEY_DOMAIN:
		return name ? (gpointer)"mono_tls_get_domain" : (gpointer)mono_tls_get_domain;
	case TLS_KEY_SGEN_THREAD_INFO:
		return name ? (gpointer)"mono_tls_get_sgen_thread_info" : (gpointer)mono_tls_get_sgen_thread_info;
	case TLS_KEY_LMF_ADDR:
		return name ? (gpointer)"mono_tls_get_lmf_addr" : (gpointer)mono_tls_get_lmf_addr;
	}
	g_assert_not_reached ();
	return NULL;
}

/* Returns the setter (void (*)(gpointer)) for the mono tls key */
gpointer
mono_tls_get_tls_setter (MonoTlsKey key, gboolean name)
{
	switch (key) {
	case TLS_KEY_THREAD:
		return name ? (gpointer)"mono_tls_set_thread" : (gpointer)mono_tls_set_thread;
	case TLS_KEY_JIT_TLS:
		return name ? (gpointer)"mono_tls_set_jit_tls" : (gpointer)mono_tls_set_jit_tls;
	case TLS_KEY_DOMAIN:
		return name ? (gpointer)"mono_tls_set_domain" : (gpointer)mono_tls_set_domain;
	case TLS_KEY_SGEN_THREAD_INFO:
		return name ? (gpointer)"mono_tls_set_sgen_thread_info" : (gpointer)mono_tls_set_sgen_thread_info;
	case TLS_KEY_LMF_ADDR:
		return name ? (gpointer)"mono_tls_set_lmf_addr" : (gpointer)mono_tls_set_lmf_addr;
	}
	g_assert_not_reached ();
	return NULL;
}

/* Getters for each tls key */
gpointer mono_tls_get_thread (void)
{
	return MONO_TLS_GET_VALUE (mono_tls_thread, mono_tls_key_thread);
}

gpointer mono_tls_get_jit_tls (void)
{
	return MONO_TLS_GET_VALUE (mono_tls_jit_tls, mono_tls_key_jit_tls);
}

gpointer mono_tls_get_domain (void)
{
	return MONO_TLS_GET_VALUE (mono_tls_domain, mono_tls_key_domain);
}

gpointer mono_tls_get_sgen_thread_info (void)
{
	return MONO_TLS_GET_VALUE (mono_tls_sgen_thread_info, mono_tls_key_sgen_thread_info);
}

gpointer mono_tls_get_lmf_addr (void)
{
	return MONO_TLS_GET_VALUE (mono_tls_lmf_addr, mono_tls_key_lmf_addr);
}

/* Setters for each tls key */
void mono_tls_set_thread (gpointer value)
{
	MONO_TLS_SET_VALUE (mono_tls_thread, mono_tls_key_thread, value);
}

void mono_tls_set_jit_tls (gpointer value)
{
	MONO_TLS_SET_VALUE (mono_tls_jit_tls, mono_tls_key_jit_tls, value);
}

void mono_tls_set_domain (gpointer value)
{
	MONO_TLS_SET_VALUE (mono_tls_domain, mono_tls_key_domain, value);
}

void mono_tls_set_sgen_thread_info (gpointer value)
{
	MONO_TLS_SET_VALUE (mono_tls_sgen_thread_info, mono_tls_key_sgen_thread_info, value);
}

void mono_tls_set_lmf_addr (gpointer value)
{
	MONO_TLS_SET_VALUE (mono_tls_lmf_addr, mono_tls_key_lmf_addr, value);
}
