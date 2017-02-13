/**
 * \file
 * UWP rand support for Mono.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#include <config.h>
#include <glib.h>
#include "mono/utils/mono-compiler.h"

#if G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT)
#include <windows.h>
#include "mono/utils/mono-rand-windows-internals.h"

MONO_WIN32_CRYPT_PROVIDER_HANDLE
mono_rand_win_open_provider (void)
{
	MONO_WIN32_CRYPT_PROVIDER_HANDLE provider = 0;

	if (!BCRYPT_SUCCESS (BCryptOpenAlgorithmProvider (&provider, BCRYPT_RNG_ALGORITHM, NULL, 0)))
		provider = 0;

	return provider;
}

gboolean
mono_rand_win_gen (MONO_WIN32_CRYPT_PROVIDER_HANDLE provider, guchar *buffer, size_t buffer_size)
{
	g_assert (provider != 0 && buffer != 0);
	return (BCRYPT_SUCCESS (BCryptGenRandom (provider, buffer, (ULONG) buffer_size, 0))) ? TRUE : FALSE;
}

gboolean
mono_rand_win_seed (MONO_WIN32_CRYPT_PROVIDER_HANDLE provider, guchar *seed, size_t seed_size)
{
	g_assert (provider != 0 && seed != 0);
	return (BCRYPT_SUCCESS (BCryptGenRandom (provider, seed, (ULONG) seed_size, BCRYPT_RNG_USE_ENTROPY_IN_BUFFER))) ? TRUE : FALSE;
}

void
mono_rand_win_close_provider (MONO_WIN32_CRYPT_PROVIDER_HANDLE provider)
{
	g_assert (provider != 0);
	BCryptCloseAlgorithmProvider (provider, 0);
}

#else /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

MONO_EMPTY_SOURCE_FILE (mono_rand_windows_uwp);
#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */
