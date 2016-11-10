/*
 * mono-tls.c: Low-level TLS support
 *
 * Thread local variables that are accessed both from native and managed code
 * are defined here and should be accessed only through this APIs
 *
 * Copyright 2013 Xamarin, Inc (http://www.xamarin.com)
 */

#include <config.h>
#include <mono/utils/mach-support.h>

#include "mono-tls.h"

#ifdef HAVE_KW_THREAD
#define USE_KW_THREAD
#endif

/* Tls variables for each MonoTlsKey */
#ifdef USE_KW_THREAD
static __thread gpointer mono_tls_thread;
static __thread gpointer mono_tls_jit_tls;
static __thread gpointer mono_tls_domain;
static __thread gpointer mono_tls_lmf;
static __thread gpointer mono_tls_sgen_thread_info;
static __thread gpointer mono_tls_lmf_addr;
#else
static MonoNativeTlsKey mono_tls_key_thread;
static MonoNativeTlsKey mono_tls_key_jit_tls;
static MonoNativeTlsKey mono_tls_key_domain;
static MonoNativeTlsKey mono_tls_key_lmf;
static MonoNativeTlsKey mono_tls_key_sgen_thread_info;
static MonoNativeTlsKey mono_tls_key_lmf_addr;
#endif

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
#ifndef USE_KW_THREAD
	mono_native_tls_alloc (&mono_tls_key_sgen_thread_info, NULL);
#endif
}

void
mono_tls_init_runtime_keys (void)
{
#ifndef USE_KW_THREAD
	mono_native_tls_alloc (&mono_tls_key_thread, NULL);
	mono_native_tls_alloc (&mono_tls_key_jit_tls, NULL);
	mono_native_tls_alloc (&mono_tls_key_domain, NULL);
	mono_native_tls_alloc (&mono_tls_key_lmf, NULL);
	mono_native_tls_alloc (&mono_tls_key_lmf_addr, NULL);
#endif
}

void
mono_tls_free_keys (void)
{
#ifndef USE_KW_THREAD
	mono_native_tls_free (mono_tls_key_thread);
	mono_native_tls_free (mono_tls_key_jit_tls);
	mono_native_tls_free (mono_tls_key_domain);
	mono_native_tls_free (mono_tls_key_lmf);
	mono_native_tls_free (mono_tls_key_sgen_thread_info);
	mono_native_tls_free (mono_tls_key_lmf_addr);
#endif
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
	case TLS_KEY_LMF:
		return name ? (gpointer)"mono_tls_get_lmf" : (gpointer)mono_tls_get_lmf;
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
	case TLS_KEY_LMF:
		return name ? (gpointer)"mono_tls_set_lmf" : (gpointer)mono_tls_set_lmf;
	case TLS_KEY_SGEN_THREAD_INFO:
		return name ? (gpointer)"mono_tls_set_sgen_thread_info" : (gpointer)mono_tls_set_sgen_thread_info;
	case TLS_KEY_LMF_ADDR:
		return name ? (gpointer)"mono_tls_set_lmf_addr" : (gpointer)mono_tls_set_lmf_addr;
	}
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_tls_get_tls_addr (MonoTlsKey key)
{
	if (key == TLS_KEY_LMF) {
#if defined(USE_KW_THREAD)
		return &mono_tls_lmf;
#elif defined(TARGET_MACH)
		return mono_mach_get_tls_address_from_thread (pthread_self (), mono_tls_key_lmf);
#endif
	}
	/* Implement if we ever need for other targets/keys */
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

gpointer mono_tls_get_lmf (void)
{
	return MONO_TLS_GET_VALUE (mono_tls_lmf, mono_tls_key_lmf);
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

void mono_tls_set_lmf (gpointer value)
{
	MONO_TLS_SET_VALUE (mono_tls_lmf, mono_tls_key_lmf, value);
}

void mono_tls_set_sgen_thread_info (gpointer value)
{
	MONO_TLS_SET_VALUE (mono_tls_sgen_thread_info, mono_tls_key_sgen_thread_info, value);
}

void mono_tls_set_lmf_addr (gpointer value)
{
	MONO_TLS_SET_VALUE (mono_tls_lmf_addr, mono_tls_key_lmf_addr, value);
}
