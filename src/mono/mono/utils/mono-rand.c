/**
 * \file
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
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
// Windows specific implementation in mono-rand-windows.c
#elif defined (HAVE_SYS_UN_H)

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
	gint socket_fd;
	gint ret;
	guint offset = 0;
	int err = 0;

	error_init (error);
	
	socket_fd = socket (PF_UNIX, SOCK_STREAM, 0);
	if (socket_fd < 0) {
		ret = -1;
		err = errno;
	} else {
		egd_addr.sun_family = AF_UNIX;
		memcpy (egd_addr.sun_path, path, sizeof (egd_addr.sun_path) - 1);
		egd_addr.sun_path [sizeof (egd_addr.sun_path) - 1] = '\0';
		ret = connect (socket_fd, (struct sockaddr*) &egd_addr, sizeof (egd_addr));
		err = errno;
	}
	if (ret == -1) {
		if (socket_fd >= 0)
			close (socket_fd);
		g_warning ("Entropy problem! Can't create or connect to egd socket %s", path);
		mono_error_set_execution_engine (error, "Failed to open egd socket %s: %s", path, strerror (err));
		return;
	}

	while (buffer_size > 0) {
		guchar request [2];
		gint count = 0;

		/* block until daemon can return enough entropy */
		request [0] = 2;
		request [1] = buffer_size < 255 ? buffer_size : 255;
		while (count < 2) {
			int sent = write (socket_fd, request + count, 2 - count);
			err = errno;
			if (sent >= 0) {
				count += sent;
			} else if (err == EINTR) {
				continue;
			} else {
				close (socket_fd);
				g_warning ("Send egd request failed %d", err);
				mono_error_set_execution_engine (error, "Failed to send request to egd socket: %s", strerror (err));
				return;
			}
		}

		count = 0;
		while (count != request [1]) {
			int received;
			received = read (socket_fd, buffer + offset, request [1] - count);
			err = errno;
			if (received > 0) {
				count += received;
				offset += received;
			} else if (received < 0 && err == EINTR) {
				continue;
			} else {
				close (socket_fd);
				g_warning ("Receive egd request failed %d", err);
				mono_error_set_execution_engine (error, "Failed to get response from egd socket: %s", strerror(err));
				return;
			}
		}

		buffer_size -= request [1];
	}

	close (socket_fd);
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
		use_egd = g_hasenv ("MONO_EGD_SOCKET");

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

	error_init (error);

	if (use_egd) {
		char *socket_path = g_getenv ("MONO_EGD_SOCKET");
		/* exception will be thrown in managed code */
		if (socket_path == NULL) {
			*handle = NULL;
			return FALSE;
		}
		get_entropy_from_egd (socket_path, buffer, buffer_size, error);
		g_free (socket_path);
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
				mono_error_set_execution_engine (error, "Entropy error! Error in read (%s).", strerror (errno));
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

	error_init (error);
	
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
 * \param handle A pointer to an RNG handle. Handle is set to NULL on failure.
 * \param val A pointer to a 32-bit unsigned int, to which the result will be written.
 * \param min Result will be greater than or equal to this value.
 * \param max Result will be less than or equal to this value.
 * Extracts one 32-bit unsigned int from an RNG handle.
 * \returns FALSE on failure, TRUE on success.
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
