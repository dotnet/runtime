/*
 * mono-rand.c: 
 *
 * Authors:
 *      Mark Crichton (crichton@gimp.org)
 *      Patrik Torstensson (p@rxc.se)
 *      Sebastien Pouliot (sebastien@ximian.com)
 *      Ludovic Henry (ludovic.henry@xamarin.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2001 Xamarin, Inc (http://www.novell.com)
 */


#include <glib.h>
#include <config.h>

#include "atomic.h"
#include "mono-error.h"
#include "mono-error-internals.h"
#include "mono-rand.h"
#include "mono-threads.h"
#include "metadata/exception.h"
#include "metadata/object.h"

#ifdef HOST_WIN32

#include <windows.h>
#include <wincrypt.h>

#ifndef PROV_INTEL_SEC
#define PROV_INTEL_SEC		22
#endif
#ifndef CRYPT_VERIFY_CONTEXT
#define CRYPT_VERIFY_CONTEXT	0xF0000000
#endif

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
 * @seed: A string containing seed data
 * @seed_size: Length of seed string
 *
 * Returns: On success, a non-NULL handle which can be used to fetch random data from mono_rand_try_get_bytes. On failure, NULL.
 *
 * Initializes an RNG client.
 */
gpointer
mono_rand_init (guchar *seed, gint seed_size)
{
	HCRYPTPROV provider = 0;

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

	/* seed the CSP with the supplied buffer (if present) */
	if (provider != 0 && seed) {
		/* the call we replace the seed with random - this isn't what is
		 * expected from the class library user */
		guchar *data = g_malloc (seed_size);
		if (data) {
			memcpy (data, seed, seed_size);
			/* add seeding material to the RNG */
			CryptGenRandom (provider, seed_size, data);
			/* zeroize and free */
			memset (data, 0, seed_size);
			g_free (data);
		}
	}

	return (gpointer) provider;
}

/**
 * mono_rand_try_get_bytes:
 * @handle: A pointer to an RNG handle. Handle is set to NULL on failure.
 * @buffer: A buffer into which to write random data.
 * @buffer_size: Number of bytes to write into buffer.
 * @error: Set on error.
 *
 * Returns: FALSE on failure and sets @error, TRUE on success.
 *
 * Extracts bytes from an RNG handle.
 */
gboolean
mono_rand_try_get_bytes (gpointer *handle, guchar *buffer, gint buffer_size, MonoError *error)
{
	HCRYPTPROV provider;

	mono_error_init (error);

	g_assert (handle);
	provider = (HCRYPTPROV) *handle;

	if (!CryptGenRandom (provider, buffer_size, buffer)) {
		CryptReleaseContext (provider, 0);
		/* we may have lost our context with CryptoAPI, but all hope isn't lost yet! */
		provider = (HCRYPTPROV) mono_rand_init (NULL, 0);
		if (!CryptGenRandom (provider, buffer_size, buffer)) {
			/* exception will be thrown in managed code */
			CryptReleaseContext (provider, 0);
			*handle = 0;
			mono_error_set_generic_error (error, "System", "ExecutionEngineException", "Failed to gen random bytes (%d)", GetLastError ());
			return FALSE;
		}
	}
	return TRUE;
}

/**
 * mono_rand_close:
 * @handle: An RNG handle.
 * @buffer: A buffer into which to write random data.
 * @buffer_size: Number of bytes to write into buffer.
 *
 * Releases an RNG handle.
 */
void
mono_rand_close (gpointer handle)
{
	CryptReleaseContext ((HCRYPTPROV) handle, 0);
}

#elif defined (HAVE_SYS_UN_H) && !defined(__native_client__)

#include <errno.h>
#include <fcntl.h>
#include <unistd.h>
#include <sys/socket.h>
#include <sys/types.h>
#include <sys/un.h>

#ifndef NAME_DEV_URANDOM
#define NAME_DEV_URANDOM "/dev/urandom"
#endif

static gboolean use_egd = FALSE;
static gint file = -1;

static void
get_entropy_from_egd (const char *path, guchar *buffer, int buffer_size, MonoError *error)
{
	struct sockaddr_un egd_addr;
	gint file;
	gint ret;
	guint offset = 0;
	int err = 0;

	mono_error_init (error);
	
	file = socket (PF_UNIX, SOCK_STREAM, 0);
	if (file < 0) {
		ret = -1;
		err = errno;
	} else {
		egd_addr.sun_family = AF_UNIX;
		strncpy (egd_addr.sun_path, path, sizeof (egd_addr.sun_path) - 1);
		egd_addr.sun_path [sizeof (egd_addr.sun_path) - 1] = '\0';
		ret = connect (file, (struct sockaddr*) &egd_addr, sizeof (egd_addr));
		err = errno;
	}
	if (ret == -1) {
		if (file >= 0)
			close (file);
		g_warning ("Entropy problem! Can't create or connect to egd socket %s", path);
		mono_error_set_generic_error (error, "System", "ExecutionEngineException", "Failed to open egd socket %s: %s", path, strerror (err));
		return;
	}

	while (buffer_size > 0) {
		guchar request [2];
		gint count = 0;

		/* block until daemon can return enough entropy */
		request [0] = 2;
		request [1] = buffer_size < 255 ? buffer_size : 255;
		while (count < 2) {
			int sent = write (file, request + count, 2 - count);
			err = errno;
			if (sent >= 0) {
				count += sent;
			} else if (err == EINTR) {
				continue;
			} else {
				close (file);
				g_warning ("Send egd request failed %d", err);
				mono_error_set_generic_error (error, "System", "ExecutionEngineException", "Failed to send request to egd socket: %s", strerror (err));
				return;
			}
		}

		count = 0;
		while (count != request [1]) {
			int received;
			received = read (file, buffer + offset, request [1] - count);
			err = errno;
			if (received > 0) {
				count += received;
				offset += received;
			} else if (received < 0 && err == EINTR) {
				continue;
			} else {
				close (file);
				g_warning ("Receive egd request failed %d", err);
				mono_error_set_generic_error (error, "System", "ExecutionEngineException", "Failed to get response from egd socket: %s", strerror(err));
				return;
			}
		}

		buffer_size -= request [1];
	}

	close (file);
}

gboolean
mono_rand_open (void)
{
	static gint32 status = 0;
	if (status != 0 || InterlockedCompareExchange (&status, 1, 0) != 0) {
		while (status != 2)
			mono_thread_info_yield ();
		return TRUE;
	}

#ifdef NAME_DEV_URANDOM
	file = open (NAME_DEV_URANDOM, O_RDONLY);
#endif
#ifdef NAME_DEV_RANDOM
	if (file < 0)
		file = open (NAME_DEV_RANDOM, O_RDONLY);
#endif
	if (file < 0)
		use_egd = g_getenv("MONO_EGD_SOCKET") != NULL;

	status = 2;

	return TRUE;
}

gpointer
mono_rand_init (guchar *seed, gint seed_size)
{
	// file < 0 is expected in the egd case
	return (!use_egd && file < 0) ? NULL : GINT_TO_POINTER (file);
}

gboolean
mono_rand_try_get_bytes (gpointer *handle, guchar *buffer, gint buffer_size, MonoError *error)
{
	g_assert (handle);

	mono_error_init (error);

	if (use_egd) {
		const char *socket_path = g_getenv ("MONO_EGD_SOCKET");
		/* exception will be thrown in managed code */
		if (socket_path == NULL) {
			*handle = NULL;
			return FALSE;
		}
		get_entropy_from_egd (socket_path, buffer, buffer_size, error);
	} else {
		/* Read until the buffer is filled. This may block if using NAME_DEV_RANDOM. */
		gint count = 0;
		gint err;

		do {
			err = read (file, buffer + count, buffer_size - count);
			if (err < 0) {
				if (errno == EINTR)
					continue;
				g_warning("Entropy error! Error in read (%s).", strerror (errno));
				/* exception will be thrown in managed code */
				mono_error_set_generic_error (error, "System", "ExecutionEngineException", "Entropy error! Error in read (%s).", strerror (errno));
				return FALSE;
			}
			count += err;
		} while (count < buffer_size);
	}
	return TRUE;
}

void
mono_rand_close (gpointer provider)
{
}

#else

#include <stdlib.h>
#include <time.h>

gboolean
mono_rand_open (void)
{
	static gint32 status = 0;
	if (status != 0 || InterlockedCompareExchange (&status, 1, 0) != 0) {
		while (status != 2)
			mono_thread_info_yield ();
		return TRUE;
	}

	srand (time (NULL));

	status = 2;

	return TRUE;
}

gpointer
mono_rand_init (guchar *seed, gint seed_size)
{
	return "srand"; // NULL will be interpreted as failure; return arbitrary nonzero pointer
}

gboolean
mono_rand_try_get_bytes (gpointer *handle, guchar *buffer, gint buffer_size, MonoError *error)
{
	gint count = 0;

	mono_error_init (error);
	
	do {
		if (buffer_size - count >= sizeof (gint32) && RAND_MAX >= 0xFFFFFFFF) {
			*(gint32*) buffer = rand();
			count += sizeof (gint32);
			buffer += sizeof (gint32) / sizeof (guchar);
		} else if (buffer_size - count >= sizeof (gint16) && RAND_MAX >= 0xFFFF) {
			*(gint16*) buffer = rand();
			count += sizeof (gint16);
			buffer += sizeof (gint16) / sizeof (guchar);
		} else if (buffer_size - count >= sizeof (gint8) && RAND_MAX >= 0xFF) {
			*(gint8*) buffer = rand();
			count += sizeof (gint8);
			buffer += sizeof (gint8) / sizeof (guchar);
		}
	} while (count < buffer_size);

	return TRUE;
}

void
mono_rand_close (gpointer provider)
{
}

#endif

/**
 * mono_rand_try_get_uint32:
 * @handle: A pointer to an RNG handle. Handle is set to NULL on failure.
 * @val: A pointer to a 32-bit unsigned int, to which the result will be written.
 * @min: Result will be greater than or equal to this value.
 * @max: Result will be less than or equal to this value.
 *
 * Returns: FALSE on failure, TRUE on success.
 *
 * Extracts one 32-bit unsigned int from an RNG handle.
 */
gboolean
mono_rand_try_get_uint32 (gpointer *handle, guint32 *val, guint32 min, guint32 max, MonoError *error)
{
	g_assert (val);
	if (!mono_rand_try_get_bytes (handle, (guchar*) val, sizeof (guint32), error))
		return FALSE;

	double randomDouble = ((gdouble) *val) / ( ((double)G_MAXUINT32) + 1 ); // Range is [0,1)
	*val = (guint32) (randomDouble * (max - min + 1) + min);

	g_assert (*val >= min);
	g_assert (*val <= max);

	return TRUE;
}
