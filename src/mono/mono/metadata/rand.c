/*
 * rand.c: System.Security.Cryptography.RNGCryptoServiceProvider support
 *
 * Author:
 *      Mark Crichton (crichton@gimp.org)
 *      Patrik Torstensson (p@rxc.se)
 *
 * (C) 2001 Ximian, Inc.
 *
 */

#include <config.h>
#include <glib.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <unistd.h>

#include <mono/metadata/object.h>
#include <mono/metadata/rand.h>
#include <mono/metadata/exception.h>

#if !defined(PLATFORM_WIN32)
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

#if defined (PLATFORM_WIN32)

#include <WinCrypt.h>

static int s_providerInitialized = 0;
static HCRYPTPROV s_provider;

static HCRYPTPROV GetProvider()
{
    if (s_providerInitialized == 1)
        return s_provider;

    if (!CryptAcquireContext (&s_provider, NULL, NULL, PROV_RSA_FULL, 0))  
    {
        if (GetLastError() != NTE_BAD_KEYSET)
            mono_raise_exception (mono_get_exception_execution_engine ("Failed to acquire crypt context"));

		// Generate a new keyset if needed
        if (!CryptAcquireContext (&s_provider, NULL, NULL, PROV_RSA_FULL, CRYPT_NEWKEYSET))
            mono_raise_exception (mono_get_exception_execution_engine ("Failed to acquire crypt context (new keyset)"));
    }
    
    s_providerInitialized =  1;

    return s_provider;
}

void ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_InternalGetBytes(MonoObject *self, MonoArray *arry)
{
    guint32 len;
    guchar *buf;

    len = mono_array_length (arry);
    buf = mono_array_addr (arry, guchar, 0);

    if (0 == CryptGenRandom (GetProvider(), len, buf))
       mono_raise_exception (mono_get_exception_execution_engine ("Failed to generate random bytes from CryptoAPI"));
}

#else

#ifndef NAME_DEV_URANDOM
#define NAME_DEV_URANDOM "/dev/urandom"
#endif

void 
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_InternalGetBytes (MonoObject *self, MonoArray *arry)
{
    guint32 len;
    gint file = -1;
    gint err;
    gint count;
    guchar *buf;

    len = mono_array_length(arry);
    buf = mono_array_addr(arry, guchar, 0);

#if defined (NAME_DEV_URANDOM)
    file = open (NAME_DEV_URANDOM, O_RDONLY);
#endif

#if defined (NAME_DEV_RANDOM)
    if (file < 0)
	    file = open (NAME_DEV_RANDOM, O_RDONLY);
#endif

    if (file < 0) {
        const char *socket_path = getenv("MONO_EGD_SOCKET");

        if (socket_path == NULL)
            mono_raise_exception (mono_get_exception_execution_engine ("Failed to open /dev/urandom or /dev/random device, or find egd socket"));

        get_entropy_from_server (socket_path, mono_array_addr(arry, guchar, 0), mono_array_length(arry));
        return;
    }

    /* Read until the buffer is filled. This may block if using NAME_DEV_RANDOM. */
    count = 0;
    do {
	    err = read(file, buf + count, len - count);
	    count += err;
    } while (err >= 0 && count < len);

    if (err < 0) {
        g_warning("Entropy error! Error in read.");
        mono_raise_exception (mono_get_exception_execution_engine ("Failed to read a random byte from /dev/urandom or /dev/random device"));
    }

    close(file);
}

#endif /* OS definition */

void
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_Seed (MonoArray *seed)
{
	/* actually we do not support any PRNG requiring seeding right now but
	the class library is ready for such possibility - so this empty 
	function is needed (e.g. a new or modified runtime) */
}

