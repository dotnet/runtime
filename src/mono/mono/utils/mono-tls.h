/**
 * \file
 * Low-level TLS support
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Copyright 2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_TLS_H__
#define __MONO_TLS_H__

#include <config.h>
#include <glib.h>
#include <mono/utils/mono-forward-internal.h>
#include <mono/utils/mach-support.h>

// FIXME: Make this more visible.
#if __cplusplus
#define MONO_INLINE inline
#elif _MSC_VER
#define MONO_INLINE __inline
#else
#define MONO_INLINE static inline
#endif

/* TLS entries used by the runtime */
// This ordering is mimiced in MONO_JIT_ICALLS (alphabetical).
typedef enum {
	TLS_KEY_DOMAIN		 = 0, // mono_domain_get ()
	TLS_KEY_JIT_TLS		 = 1,
	TLS_KEY_LMF_ADDR	 = 2,
	TLS_KEY_SGEN_THREAD_INFO = 3,
	TLS_KEY_THREAD		 = 4, // mono_thread_internal_current ()
	TLS_KEY_NUM		 = 5
} MonoTlsKey;

#if __cplusplus
g_static_assert (TLS_KEY_DOMAIN == 0);
#endif
// There are only JIT icalls to get TLS, not set TLS.
#define mono_get_tls_key_to_jit_icall_id(a)	((MonoJitICallId)((a) + MONO_JIT_ICALL_mono_tls_get_domain_extern))

#ifdef HOST_WIN32

#include <windows.h>

// Some Windows SDKs define TLS to be FLS.
// That is presumably catastrophic when combined with mono_amd64_emit_tls_get / mono_x86_emit_tls_get.
// It also is not consistent.
// FLS is a reasonable idea perhaps, but we would need to be consistent and to adjust JIT.
// And there is __declspec(fiber).
#undef TlsAlloc
#undef TlsFree
#undef TlsGetValue
#undef TlsSetValue

#define MonoNativeTlsKey DWORD
#define mono_native_tls_alloc(key,destructor) ((*(key) = TlsAlloc ()) != TLS_OUT_OF_INDEXES && destructor == NULL)
#define mono_native_tls_free TlsFree
#define mono_native_tls_set_value TlsSetValue

#ifdef HAVE_WINTERNL_H
#include <winternl.h>
#else
typedef struct _TEB {
	PVOID Reserved1[12];
	PVOID ProcessEnvironmentBlock;
	PVOID Reserved2[399];
	BYTE Reserved3[1952];
	PVOID TlsSlots[64];
	BYTE Reserved4[8];
	PVOID Reserved5[26];
	PVOID ReservedForOle;
	PVOID Reserved6[4];
	PVOID TlsExpansionSlots;
} TEB, *PTEB;
#endif

// TlsGetValue always writes 0 to LastError. Which can cause problems. This never changes LastError.
//
MONO_INLINE
void*
mono_native_tls_get_value (unsigned index)
{
	PTEB const teb = NtCurrentTeb ();

	if (index < TLS_MINIMUM_AVAILABLE)
		return teb->TlsSlots [index];

	void** const p = (void**)teb->TlsExpansionSlots;

	return p ? p [index - TLS_MINIMUM_AVAILABLE] : NULL;
}

#else

#include <pthread.h>

#define MonoNativeTlsKey pthread_key_t
#define mono_native_tls_get_value pthread_getspecific

MONO_INLINE int
mono_native_tls_alloc (MonoNativeTlsKey *key, void *destructor)
{
	return pthread_key_create (key, (void (*)(void*)) destructor) == 0;
}

MONO_INLINE void
mono_native_tls_free (MonoNativeTlsKey key)
{
	pthread_key_delete (key);
}

MONO_INLINE int
mono_native_tls_set_value (MonoNativeTlsKey key, gpointer value)
{
	return !pthread_setspecific (key, value);
}

#endif /* HOST_WIN32 */

void mono_tls_init_gc_keys (void);
void mono_tls_init_runtime_keys (void);
void mono_tls_free_keys (void);

G_EXTERN_C MonoInternalThread *mono_tls_get_thread_extern (void);
G_EXTERN_C MonoJitTlsData     *mono_tls_get_jit_tls_extern (void);
G_EXTERN_C MonoDomain *mono_tls_get_domain_extern (void);
G_EXTERN_C SgenThreadInfo *mono_tls_get_sgen_thread_info_extern (void);
G_EXTERN_C MonoLMF       **mono_tls_get_lmf_addr_extern (void);

/*
 * On all platforms we should be able to use either __thread or __declspec (thread)
 * or pthread/TlsGetValue.
 * Certain platforms will support fast tls only when using one of the thread local
 * storage backends. By default this is __thread if we have MONO_KEYWORD_THREAD defined.
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

#ifdef MONO_KEYWORD_THREAD

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

// Tls variables for each MonoTlsKey.
//
extern MONO_KEYWORD_THREAD MonoInternalThread *mono_tls_thread MONO_TLS_FAST;
extern MONO_KEYWORD_THREAD MonoJitTlsData     *mono_tls_jit_tls MONO_TLS_FAST;
extern MONO_KEYWORD_THREAD MonoDomain         *mono_tls_domain MONO_TLS_FAST;
extern MONO_KEYWORD_THREAD SgenThreadInfo     *mono_tls_sgen_thread_info MONO_TLS_FAST;
extern MONO_KEYWORD_THREAD MonoLMF           **mono_tls_lmf_addr MONO_TLS_FAST;

#elif defined(DISABLE_THREADS)

extern MonoInternalThread *mono_tls_thread;
extern MonoJitTlsData     *mono_tls_jit_tls;
extern MonoDomain         *mono_tls_domain;
extern SgenThreadInfo     *mono_tls_sgen_thread_info;
extern MonoLMF           **mono_tls_lmf_addr;

#else

extern MonoNativeTlsKey mono_tls_key_thread;
extern MonoNativeTlsKey mono_tls_key_jit_tls;
extern MonoNativeTlsKey mono_tls_key_domain;
extern MonoNativeTlsKey mono_tls_key_sgen_thread_info;
extern MonoNativeTlsKey mono_tls_key_lmf_addr;

#endif

extern gint32 mono_tls_offsets [TLS_KEY_NUM];

#if defined(MONO_KEYWORD_THREAD) || defined(DISABLE_THREADS)
#define MONO_TLS_GET_VALUE(tls_var,tls_key) (tls_var)
#define MONO_TLS_SET_VALUE(tls_var,tls_key,value) (tls_var = value)
#else
#define MONO_TLS_GET_VALUE(tls_var,tls_key) (mono_native_tls_get_value (tls_key))
#define MONO_TLS_SET_VALUE(tls_var,tls_key,value) (mono_native_tls_set_value (tls_key, value))
#endif

#endif /* __MONO_TLS_H__ */
