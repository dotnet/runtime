/**
 * \file
 * Windows rand support for Mono.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#include <config.h>
#include <glib.h>
#include "mono-error.h"
#include "mono-error-internals.h"
#include "mono-rand.h"

#if defined(HOST_WIN32)
#include <windows.h>
#include "mono/utils/mono-rand-windows-internals.h"

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
#ifndef PROV_INTEL_SEC
#define PROV_INTEL_SEC		22
#endif
#ifndef CRYPT_VERIFY_CONTEXT
#define CRYPT_VERIFY_CONTEXT	0xF0000000
#endif

MONO_WIN32_CRYPT_PROVIDER_HANDLE
mono_rand_win_open_provider (void)
{
	MONO_WIN32_CRYPT_PROVIDER_HANDLE provider = 0;

	/* There is no need to create a container for just random data,
	 * so we can use CRYPT_VERIFY_CONTEXT (one call) see:
	 * http://blogs.msdn.com/dangriff/archive/2003/11/19/51709.aspx */

	/* We first try to use the Intel PIII RNG if drivers are present */
	if (!CryptAcquireContext (&provider, NULL, NULL, PROV_INTEL_SEC, CRYPT_VERIFY_CONTEXT)) {
		/* not a PIII or no drivers available, use default RSA CSP */
		if (!CryptAcquireContext (&provider, NULL, NULL, PROV_RSA_FULL, CRYPT_VERIFY_CONTEXT)) {
			/* exception will be thrown in managed code */
			provider = 0;
		}
	}

	return provider;
}

void
mono_rand_win_close_provider (MONO_WIN32_CRYPT_PROVIDER_HANDLE provider)
{
	CryptReleaseContext (provider, 0);
}

gboolean
mono_rand_win_gen (MONO_WIN32_CRYPT_PROVIDER_HANDLE provider, guchar *buffer, size_t buffer_size)
{
	return CryptGenRandom (provider, (DWORD) buffer_size, buffer);
}

gboolean
mono_rand_win_seed (MONO_WIN32_CRYPT_PROVIDER_HANDLE provider, guchar *seed, size_t seed_size)
{
	/* add seeding material to the RNG */
	return CryptGenRandom (provider, (DWORD) seed_size, seed);
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

/**
 * mono_rand_open:
 *
 * Returns: True if random source is global, false if mono_rand_init can be called repeatedly to get randomness instances.
 *
 * Initializes entire RNG system. Must be called once per process before calling mono_rand_init.
 */
gboolean
mono_rand_open (void)
{
	return FALSE;
}

/**
 * mono_rand_init:
 * \param seed A string containing seed data
 * \param seed_size Length of seed string
 * Initializes an RNG client.
 * \returns On success, a non-NULL handle which can be used to fetch random data from \c mono_rand_try_get_bytes. On failure, NULL.
 */
gpointer
mono_rand_init (guchar *seed, gint seed_size)
{
	MONO_WIN32_CRYPT_PROVIDER_HANDLE provider = 0;

	/* try to open crypto provider. */
	provider = mono_rand_win_open_provider ();

	/* seed the CSP with the supplied buffer (if present) */
	if (provider != 0 && seed != NULL) {
		/* the call we replace the seed with random - this isn't what is
		 * expected from the class library user */
		guchar *data = g_malloc (seed_size);
		if (data != NULL) {
			memcpy (data, seed, seed_size);
			/* add seeding material to the RNG */
			mono_rand_win_seed (provider, data, seed_size);
			/* zeroize and free */
			memset (data, 0, seed_size);
			g_free (data);
		}
	}

	return (gpointer) provider;
}

/**
 * mono_rand_try_get_bytes:
 * \param handle A pointer to an RNG handle. Handle is set to NULL on failure.
 * \param buffer A buffer into which to write random data.
 * \param buffer_size Number of bytes to write into buffer.
 * \param error Set on error.
 * Extracts bytes from an RNG handle.
 * \returns FALSE on failure and sets \p error, TRUE on success.
 */
gboolean
mono_rand_try_get_bytes (gpointer *handle, guchar *buffer, gint buffer_size, MonoError *error)
{
	MONO_WIN32_CRYPT_PROVIDER_HANDLE provider;

	error_init (error);

	g_assert (handle);
	provider = (MONO_WIN32_CRYPT_PROVIDER_HANDLE) *handle;

	/* generate random bytes */
	if (!mono_rand_win_gen (provider, buffer, buffer_size)) {
		mono_rand_win_close_provider (provider);
		/* we may have lost our context with CryptoAPI, but all hope isn't lost yet! */
		provider = mono_rand_win_open_provider ();
		if (provider != 0) {

			/* retry generate of random bytes */
			if (!mono_rand_win_gen (provider, buffer, buffer_size)) {
				/* failure, close provider */
				mono_rand_win_close_provider (provider);
				provider = 0;
			}
		}

		/* make sure client gets new opened provider handle or NULL on failure */
		*handle = (gpointer) provider;
		if (*handle == 0) {
			/* exception will be thrown in managed code */
			mono_error_set_execution_engine (error, "Failed to gen random bytes (%d)", GetLastError ());
			return FALSE;
		}
	}
	return TRUE;
}

/**
 * mono_rand_close:
 * \param handle An RNG handle.
 * Releases an RNG handle.
 */
void
mono_rand_close (gpointer handle)
{
	mono_rand_win_close_provider ((MONO_WIN32_CRYPT_PROVIDER_HANDLE) handle);
}
#endif /* HOST_WIN32 */
