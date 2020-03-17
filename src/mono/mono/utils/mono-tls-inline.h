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
static inline
MonoInternalThread *mono_tls_get_thread (void)
{
	return (MonoInternalThread*)MONO_TLS_GET_VALUE (mono_tls_thread, mono_tls_key_thread);
}

#define mono_get_jit_tls mono_tls_get_jit_tls

static inline
MonoJitTlsData *mono_tls_get_jit_tls (void)
{
	return (MonoJitTlsData*)MONO_TLS_GET_VALUE (mono_tls_jit_tls, mono_tls_key_jit_tls);
}

static inline
MonoDomain *mono_tls_get_domain (void)
{
	return (MonoDomain*)MONO_TLS_GET_VALUE (mono_tls_domain, mono_tls_key_domain);
}

static inline
SgenThreadInfo *mono_tls_get_sgen_thread_info (void)
{
	return (SgenThreadInfo*)MONO_TLS_GET_VALUE (mono_tls_sgen_thread_info, mono_tls_key_sgen_thread_info);
}

#define mono_get_lmf_addr mono_tls_get_lmf_addr

static inline
MonoLMF **mono_tls_get_lmf_addr (void)
{
	return (MonoLMF**)MONO_TLS_GET_VALUE (mono_tls_lmf_addr, mono_tls_key_lmf_addr);
}

/* Setters for each tls key */
static inline
void mono_tls_set_thread (MonoInternalThread *value)
{
	MONO_TLS_SET_VALUE (mono_tls_thread, mono_tls_key_thread, value);
}

static inline
void mono_tls_set_jit_tls (MonoJitTlsData *value)
{
	MONO_TLS_SET_VALUE (mono_tls_jit_tls, mono_tls_key_jit_tls, value);
}

static inline
void mono_tls_set_domain (MonoDomain *value)
{
	MONO_TLS_SET_VALUE (mono_tls_domain, mono_tls_key_domain, value);
}

static inline
void mono_tls_set_sgen_thread_info (SgenThreadInfo *value)
{
	MONO_TLS_SET_VALUE (mono_tls_sgen_thread_info, mono_tls_key_sgen_thread_info, value);
}

static inline
void mono_tls_set_lmf_addr (MonoLMF **value)
{
	MONO_TLS_SET_VALUE (mono_tls_lmf_addr, mono_tls_key_lmf_addr, value);
}

#endif /* __MONO_TLS_INLINE_H__ */
