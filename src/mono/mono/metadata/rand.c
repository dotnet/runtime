/*
 * rand.c: System.Security.Cryptography.RNGCryptoServiceProvider support
 *
 * Authors:
 *      Mark Crichton (crichton@gimp.org)
 *      Patrik Torstensson (p@rxc.se)
 *	Sebastien Pouliot (sebastien@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 */

#include <config.h>
#include <glib.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_STRING_H
#include <string.h>
#endif

#include <mono/metadata/object.h>
#include <mono/metadata/rand.h>
#include <mono/metadata/exception.h>

#if !defined(__native_client__) && !defined(HOST_WIN32)
#include <sys/socket.h>
#include <sys/un.h>
#include <errno.h>

static void
get_entropy_from_server (const char *path, guchar *buf, int len)
{
    int file;
    gint ret;
    guint offset = 0;
    struct sockaddr_un egd_addr;

    file = socket (PF_UNIX, SOCK_STREAM, 0);
    if (file < 0)
        ret = -1;
    else {
        egd_addr.sun_family = AF_UNIX;
        strncpy (egd_addr.sun_path, path, MONO_SIZEOF_SUNPATH - 1);
        egd_addr.sun_path [MONO_SIZEOF_SUNPATH-1] = '\0';
        ret = connect (file, (struct sockaddr *)&egd_addr, sizeof(egd_addr));
    }
    if (ret == -1) {
        if (file >= 0)
            close (file);
    	g_warning ("Entropy problem! Can't create or connect to egd socket %s", path);
        mono_raise_exception (mono_get_exception_execution_engine ("Failed to open egd socket"));
    }

    while (len > 0) {
        guchar request[2];
        gint count = 0;

        request [0] = 2; /* block until daemon can return enough entropy */
        request [1] = len < 255 ? len : 255;
        while (count < 2) {
            int sent = write (file, request + count, 2 - count);
            if (sent >= 0)
                count += sent;
            else if (errno == EINTR)
                continue;
            else {
                close (file);
                g_warning ("Send egd request failed %d", errno);
                mono_raise_exception (mono_get_exception_execution_engine ("Failed to send request to egd socket"));
            }
        }

        count = 0;
        while (count != request [1]) {
            int received;
            received = read(file, buf + offset, request [1] - count);
            if (received > 0) {
                count += received;
                offset += received;
            } else if (received < 0 && errno == EINTR) {
                continue;
            } else {
                close (file);
                g_warning ("Receive egd request failed %d", errno);
                mono_raise_exception (mono_get_exception_execution_engine ("Failed to get response from egd socket"));
            }
        }

        len -= request [1];
    }

    close (file);
}
#endif

#if defined (HOST_WIN32)

#include <windows.h>
#include <wincrypt.h>

#ifndef PROV_INTEL_SEC
#define PROV_INTEL_SEC		22
#endif
#ifndef CRYPT_VERIFY_CONTEXT
#define CRYPT_VERIFY_CONTEXT	0xF0000000
#endif

MonoBoolean
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngOpen (void)
{
	/* FALSE == Local (instance) handle for randomness */
	return FALSE;
}

gpointer
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngInitialize (MonoArray *seed)
{
	HCRYPTPROV provider = 0;

	/* There is no need to create a container for just random data,
	   so we can use CRYPT_VERIFY_CONTEXT (one call) see: 
	   http://blogs.msdn.com/dangriff/archive/2003/11/19/51709.aspx */

	/* We first try to use the Intel PIII RNG if drivers are present */
	if (!CryptAcquireContext (&provider, NULL, NULL, PROV_INTEL_SEC, CRYPT_VERIFY_CONTEXT)) {
		/* not a PIII or no drivers available, use default RSA CSP */
		if (!CryptAcquireContext (&provider, NULL, NULL, PROV_RSA_FULL, CRYPT_VERIFY_CONTEXT)) {
			provider = 0;
			/* exception will be thrown in managed code */
		}
	}

	/* seed the CSP with the supplied buffer (if present) */
	if ((provider != 0) && (seed)) {
		guint32 len = mono_array_length (seed);
		guchar *buf = mono_array_addr (seed, guchar, 0);
		/* the call we replace the seed with random - this isn't what is
		   expected from the class library user */
		guchar *data = g_malloc (len);
		if (data) {
			memcpy (data, buf, len);
			/* add seeding material to the RNG */
			CryptGenRandom (provider, len, data);
			/* zeroize and free */
			memset (data, 0, len);
			g_free (data);
		}
	}

	return (gpointer) provider;	
}

gpointer
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngGetBytes (gpointer handle, MonoArray *arry)
{
	HCRYPTPROV provider = (HCRYPTPROV) handle;
	guint32 len = mono_array_length (arry);
	guchar *buf = mono_array_addr (arry, guchar, 0);

	if (!CryptGenRandom (provider, len, buf)) {
		CryptReleaseContext (provider, 0);
		/* we may have lost our context with CryptoAPI, but all hope isn't lost yet! */
		provider = (HCRYPTPROV) ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngInitialize (NULL);
		if (!CryptGenRandom (provider, len, buf)) {
			CryptReleaseContext (provider, 0);
			provider = 0;
			/* exception will be thrown in managed code */
		}
	}
	return (gpointer) provider;
}

void
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngClose (gpointer handle) 
{
	CryptReleaseContext ((HCRYPTPROV) handle, 0);
}

#elif defined (__native_client__)

#include <time.h>

MonoBoolean
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngOpen (void)
{
	srand (time (NULL));
	return TRUE;
}

gpointer
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngInitialize (MonoArray *seed)
{
	return -1;
}

gpointer 
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngGetBytes (gpointer handle, MonoArray *arry)
{	
	guint32 len = mono_array_length (arry);
	guchar *buf = mono_array_addr (arry, guchar, 0);

	/* Read until the buffer is filled. This may block if using NAME_DEV_RANDOM. */
	gint count = 0;
	gint err;

	do {
		if (len - count >= sizeof (long))
		{
			*(long*)buf = rand();
			count += sizeof (long);
		}
		else if (len - count >= sizeof (short))
		{
			*(short*)buf = rand();
			count += sizeof (short);
		}
		else if (len - count >= sizeof (char))
		{
			*buf = rand();
			count += sizeof (char);
		}
	} while (count < len);

	return (gpointer)-1L;
}

void
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngClose (gpointer handle) 
{
}
#else

#ifndef NAME_DEV_URANDOM
#define NAME_DEV_URANDOM "/dev/urandom"
#endif

static gboolean egd = FALSE;
static gint file = -1;

MonoBoolean
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngOpen (void)
{
	if (egd || (file >= 0))
		return TRUE;

#if defined (NAME_DEV_URANDOM)
	file = open (NAME_DEV_URANDOM, O_RDONLY);
#endif

#if defined (NAME_DEV_RANDOM)
	if (file < 0)
		file = open (NAME_DEV_RANDOM, O_RDONLY);
#endif

	if (file < 0) {
		const char *socket_path = g_getenv("MONO_EGD_SOCKET");
		egd = (socket_path != NULL);
	}

	/* TRUE == Global handle for randomness */
	return TRUE;
}

gpointer
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngInitialize (MonoArray *seed)
{
	/* if required exception will be thrown in managed code */
	return ((!egd && (file < 0)) ? NULL : GINT_TO_POINTER (file));
}

gpointer 
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngGetBytes (gpointer handle, MonoArray *arry)
{
	gint file = GPOINTER_TO_INT (handle);
	guint32 len = mono_array_length (arry);
	guchar *buf = mono_array_addr (arry, guchar, 0);

	if (egd) {
		const char *socket_path = g_getenv ("MONO_EGD_SOCKET");
		/* exception will be thrown in managed code */
		if (socket_path == NULL)
			return NULL; 
		get_entropy_from_server (socket_path, mono_array_addr (arry, guchar, 0), mono_array_length (arry));
		return (gpointer) -1;
	} else {
		/* Read until the buffer is filled. This may block if using NAME_DEV_RANDOM. */
		gint count = 0;
		gint err;

		do {
			err = read (file, buf + count, len - count);
			if (err < 0) {
				if (errno == EINTR)
					continue;
				break;
			}
			count += err;
		} while (count < len);

		if (err < 0) {
			g_warning("Entropy error! Error in read (%s).", strerror (errno));
			/* exception will be thrown in managed code */
			return NULL;
		}
	}

	/* We do not support PRNG seeding right now but the class library is this */

	return handle;	
}

void
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngClose (gpointer handle) 
{
}

#endif /* OS definition */
