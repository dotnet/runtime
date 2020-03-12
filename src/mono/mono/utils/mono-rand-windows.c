/**
 * \file
 * Windows rand support for Mono.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#include <config.h>
#include <glib.h>
#ifdef HOST_WIN32
#include "mono-error.h"
#include "mono-error-internals.h"
#include "mono-rand.h"
#include <windows.h>
#include <bcrypt.h>
#include <limits.h>

// This implementation requires Windows 7 or newer.

#define BCRYPT_USE_SYSTEM_PREFERRED_RNG 0x00000002
const static char mono_rand_provider [ ] = "BCryptGenRandom";

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
	return TRUE;
}

/**
 * mono_rand_init:
 * \param seed A string containing seed data
 * \param seed_size Length of seed string
 * Initializes an RNG client.
 * \returns On success, a non-NULL handle which can be used to fetch random data from \c mono_rand_try_get_bytes. On failure, NULL.
 */
gpointer
mono_rand_init (const guchar *seed, gssize seed_size)
{
	// NULL will be interpreted as failure; return arbitrary nonzero pointer
	return (gpointer)mono_rand_provider;
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
mono_rand_try_get_bytes (gpointer *handle, guchar *buffer, gssize buffer_size, MonoError *error)
{
	g_assert (buffer || !buffer_size);
	error_init (error);
	g_assert (handle);
	gpointer const handle_value = *handle;
	g_assert (handle_value == 0 || handle_value == mono_rand_provider);
	if (!handle_value)
		return FALSE;
	while (buffer_size > 0) {
		ULONG const size = (ULONG)MIN (buffer_size, ULONG_MAX);
		NTSTATUS const status = BCryptGenRandom (0, buffer, size, BCRYPT_USE_SYSTEM_PREFERRED_RNG);
		if (!BCRYPT_SUCCESS (status)) {
			mono_error_set_execution_engine (error, "Failed to gen random bytes (%ld)", status);
			// failure, clear provider for future attempts
			*handle = 0;
			return FALSE;
		}
		buffer += size;
		buffer_size -= size;
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
	g_assert (handle == 0 || handle == mono_rand_provider);
}

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mono_rand_windows);

#endif /* HOST_WIN32 */
