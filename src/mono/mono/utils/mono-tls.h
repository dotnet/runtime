/*
 * mono-tls.h: Low-level TLS support
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2011 Novell, Inc
 */

#ifndef __MONO_TLS_H__
#define __MONO_TLS_H__


#ifdef HOST_WIN32

#include <windows.h>

#define MonoNativeTlsKey DWORD
#define mono_native_tls_alloc(key,destructor) ((key = TlsAlloc ()) != TLS_OUT_OF_INDEXES && destructor == NULL)
#define mono_native_tls_free TlsFree
#define mono_native_tls_set_value TlsSetValue
#define mono_native_tls_get_value TlsGetValue

#else

#include <pthread.h>

#define MonoNativeTlsKey pthread_key_t
#define mono_native_tls_alloc(key,destructor) (pthread_key_create (&key, destructor) == 0) 
#define mono_native_tls_free pthread_key_delete
#define mono_native_tls_set_value(k,v) (!pthread_setspecific ((k), (v)))
#define mono_native_tls_get_value pthread_getspecific

#endif /* HOST_WIN32 */


#endif /* __MONO_TLS_H__ */
