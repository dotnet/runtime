/*
 * mono-tls.c: Low-level TLS support
 *
 * Copyright 2013 Xamarin, Inc (http://www.xamarin.com)
 */

#include <config.h>

#include "mono-tls.h"

static int tls_offsets [TLS_KEY_NUM];
static gboolean tls_offset_set [TLS_KEY_NUM];

/*
 * mono_tls_key_get_offset:
 *
 *   Return the TLS offset used by the TLS var identified by KEY, previously initialized by a call to
 * mono_tls_key_set_offset (). Return -1 if the offset is not known.
 */
int
mono_tls_key_get_offset (MonoTlsKey key)
{
	g_assert (tls_offset_set [key]);
	return tls_offsets [key];
}

/*
 * mono_tls_key_set_offset:
 *
 *   Set the TLS offset used by the TLS var identified by KEY.
 */
void
mono_tls_key_set_offset (MonoTlsKey key, int offset)
{
	tls_offsets [key] = offset;
	tls_offset_set [key] = TRUE;
}
