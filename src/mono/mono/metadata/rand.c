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
       mono_raise_exception (mono_get_exception_execution_engine ("Failed to generate random bytes from CryptAPI"));
}

#elif defined (NAME_DEV_RANDOM) && defined (HAVE_CRYPT_RNG)

#ifndef NAME_DEV_URANDOM
#define NAME_DEV_URANDOM "/dev/urandom"
#endif

void 
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_InternalGetBytes (MonoObject *self, MonoArray *arry)
{
    guint32 len;
    gint file;
    gint err;
    gint count;
    guchar *buf;

    len = mono_array_length(arry);
    buf = mono_array_addr(arry, guchar, 0);

    file = open (NAME_DEV_URANDOM, O_RDONLY);

    if (file < 0)
	    file = open (NAME_DEV_RANDOM, O_RDONLY);

    if (file < 0) {
    	g_warning ("Entropy problem! Can't open %s or %s", NAME_DEV_URANDOM, NAME_DEV_RANDOM);

        mono_raise_exception (mono_get_exception_execution_engine ("Failed to open /dev/urandom or /dev/random device"));
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

#else

void ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_InternalGetBytes(MonoObject *self, MonoArray *arry)
{
    mono_raise_exception(mono_get_exception_not_implemented(NULL));
}

#endif /* OS definition */
