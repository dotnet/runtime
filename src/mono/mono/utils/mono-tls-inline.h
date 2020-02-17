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

#ifndef __MONO_TLS_INLINE_H__
#define __MONO_TLS_INLINE_H__

#include "mono-tls.h"

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

 /*
 * Gets the tls offset associated with the key. This offset is set at key
 * initialization (at runtime). Certain targets can implement computing
 * this offset and using it at runtime for fast inlined tls access.
 */
MONO_INLINE
gint32
mono_tls_get_tls_offset (MonoTlsKey key)
{
	g_assert (mono_tls_offsets [key]);
	return mono_tls_offsets [key];
}

// Casts on getters are for the !MONO_KEYWORD_THREAD case.

/* Getters for each tls key */
MONO_INLINE
MonoInternalThread *mono_tls_get_thread (void)
{
	return (MonoInternalThread*)MONO_TLS_GET_VALUE (mono_tls_thread, mono_tls_key_thread);
}

#define mono_get_jit_tls mono_tls_get_jit_tls

MONO_INLINE
MonoJitTlsData *mono_tls_get_jit_tls (void)
{
	return (MonoJitTlsData*)MONO_TLS_GET_VALUE (mono_tls_jit_tls, mono_tls_key_jit_tls);
}

MONO_INLINE
MonoDomain *mono_tls_get_domain (void)
{
	return (MonoDomain*)MONO_TLS_GET_VALUE (mono_tls_domain, mono_tls_key_domain);
}

MONO_INLINE
SgenThreadInfo *mono_tls_get_sgen_thread_info (void)
{
	return (SgenThreadInfo*)MONO_TLS_GET_VALUE (mono_tls_sgen_thread_info, mono_tls_key_sgen_thread_info);
}

#define mono_get_lmf_addr mono_tls_get_lmf_addr

MONO_INLINE
MonoLMF **mono_tls_get_lmf_addr (void)
{
	return (MonoLMF**)MONO_TLS_GET_VALUE (mono_tls_lmf_addr, mono_tls_key_lmf_addr);
}

/* Setters for each tls key */
MONO_INLINE
void mono_tls_set_thread (MonoInternalThread *value)
{
	MONO_TLS_SET_VALUE (mono_tls_thread, mono_tls_key_thread, value);
}

MONO_INLINE
void mono_tls_set_jit_tls (MonoJitTlsData *value)
{
	MONO_TLS_SET_VALUE (mono_tls_jit_tls, mono_tls_key_jit_tls, value);
}

MONO_INLINE
void mono_tls_set_domain (MonoDomain *value)
{
	MONO_TLS_SET_VALUE (mono_tls_domain, mono_tls_key_domain, value);
}

MONO_INLINE
void mono_tls_set_sgen_thread_info (SgenThreadInfo *value)
{
	MONO_TLS_SET_VALUE (mono_tls_sgen_thread_info, mono_tls_key_sgen_thread_info, value);
}

MONO_INLINE
void mono_tls_set_lmf_addr (MonoLMF **value)
{
	MONO_TLS_SET_VALUE (mono_tls_lmf_addr, mono_tls_key_lmf_addr, value);
}

#endif /* __MONO_TLS_INLINE_H__ */
