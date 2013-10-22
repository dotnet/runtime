/*
 * mono-tls.h: Low-level TLS support
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Copyright 2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 */

#ifndef __MONO_TLS_H__
#define __MONO_TLS_H__

#include <glib.h>

/* TLS entries used by the runtime */
typedef enum {
	/* mono_thread_internal_current () */
	TLS_KEY_THREAD = 0,
	TLS_KEY_JIT_TLS = 1,
	/* mono_domain_get () */
	TLS_KEY_DOMAIN = 2,
	TLS_KEY_LMF = 3,
	TLS_KEY_SGEN_THREAD_INFO = 4,
	TLS_KEY_SGEN_TLAB_NEXT_ADDR = 5,
	TLS_KEY_SGEN_TLAB_TEMP_END = 6,
	TLS_KEY_BOEHM_GC_THREAD = 7,
	TLS_KEY_LMF_ADDR = 8,
	TLS_KEY_NUM = 9
} MonoTlsKey;

#ifdef HOST_WIN32

#include <windows.h>

#define MonoNativeTlsKey DWORD
#define mono_native_tls_alloc(key,destructor) ((*(key) = TlsAlloc ()) != TLS_OUT_OF_INDEXES && destructor == NULL)
#define mono_native_tls_free TlsFree
#define mono_native_tls_set_value TlsSetValue
#define mono_native_tls_get_value TlsGetValue

#else

#include <pthread.h>

#define MonoNativeTlsKey pthread_key_t
#define mono_native_tls_get_value pthread_getspecific

static inline int
mono_native_tls_alloc (MonoNativeTlsKey *key, void *destructor)
{
	return pthread_key_create (key, destructor) == 0;
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

int mono_tls_key_get_offset (MonoTlsKey key);
void mono_tls_key_set_offset (MonoTlsKey key, int offset);

#endif /* __MONO_TLS_H__ */
