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
#define mono_get_tls_key_to_jit_icall_id(a)	((MonoJitICallId)((a) + MONO_JIT_ICALL_mono_tls_get_domain))

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

#include <winternl.h>

// TlsGetValue always writes 0 to LastError. Which can cause problems. This never changes LastError.
//
static inline
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

static inline int
mono_native_tls_alloc (MonoNativeTlsKey *key, void *destructor)
{
	return pthread_key_create (key, (void (*)(void*)) destructor) == 0;
}

static inline void
mono_native_tls_free (MonoNativeTlsKey key)
{
	pthread_key_delete (key);
}

static inline int
mono_native_tls_set_value (MonoNativeTlsKey key, gpointer value)
{
	return !pthread_setspecific (key, value);
}

#endif /* HOST_WIN32 */

void mono_tls_init_gc_keys (void);
void mono_tls_init_runtime_keys (void);
void mono_tls_free_keys (void);
gint32 mono_tls_get_tls_offset (MonoTlsKey key);

G_EXTERN_C MonoInternalThread *mono_tls_get_thread (void);
G_EXTERN_C MonoJitTlsData     *mono_tls_get_jit_tls (void);
G_EXTERN_C MonoDomain *mono_tls_get_domain (void);
G_EXTERN_C SgenThreadInfo     *mono_tls_get_sgen_thread_info (void);
G_EXTERN_C MonoLMF           **mono_tls_get_lmf_addr (void);

void mono_tls_set_thread 	   (MonoInternalThread *value);
void mono_tls_set_jit_tls 	   (MonoJitTlsData     *value);
void mono_tls_set_domain 	   (MonoDomain         *value);
void mono_tls_set_sgen_thread_info (SgenThreadInfo     *value);
void mono_tls_set_lmf_addr 	   (MonoLMF           **value);

#endif /* __MONO_TLS_H__ */
