/*
 * rand.c: System.Security.Cryptography.RNGCryptoServiceProvider support
 *
 * Author:
 *      Mark Crichton (crichton@gimp.org)
 *
 * (C) 2001 Ximian, Inc.
 *
 */


/* Ok, the exception handling is bogus.  I need to work on that */

#include <config.h>
#include <glib.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <unistd.h>

#include <mono/metadata/object.h>
#include <mono/metadata/rand.h>
#include <mono/metadata/exception.h>

#if defined (NAME_DEV_RANDOM) && defined (HAVE_CRYPT_RNG)

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

    	/* This needs to be a crypto exception */
    	mono_raise_exception(mono_get_exception_not_implemented());
    }

    /* Read until the buffer is filled. This may block if using NAME_DEV_RANDOM. */
    count = 0;
    do {
	    err = read(file, buf + count, len - count);
	    count += err;
    } while (err >= 0 && count < len);

    if (err < 0) {
        g_warning("Entropy error! Error in read.");
        mono_raise_exception(mono_get_exception_not_implemented());
    }

    close(file);
}

void 
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_InternalGetNonZeroBytes (MonoObject *self, MonoArray *arry)
{
    guint32 len;
    gint file, i;
    gint err;
    guchar byte = 0;

    len = mono_array_length(arry);

    file = open(NAME_DEV_RANDOM, O_RDONLY);

    if (file < 0) {
        g_warning("Entropy problem! Can't open %s", NAME_DEV_RANDOM);

        /* This needs to be a crypto exception */
        mono_raise_exception(mono_get_exception_not_implemented());
    }

    for (i = 0; i < len; i++) {

        do {
            err = read(file, &byte, 1);
        } while (byte == 0);

        if (err < 0) {
            g_warning("Entropy error! Error in read.");
            mono_raise_exception(mono_get_exception_not_implemented());
        }

        mono_array_set(arry, guchar, i, byte);
    }

    close(file);
}

/* This needs to change when I do the Win32 support... */
#else
#warning "No Entropy Source Found"
void ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_InternalGetBytes(MonoObject *self, MonoArray *arry)
{
    g_warning("0K problem. We have no entropy. Badness will occur.");
    mono_raise_exception(mono_get_exception_not_implemented());
}

void ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_InternalGetNonZeroBytes(MonoObject *self, MonoArray *arry)
{
    g_warning("0K problem. We have no entropy. Badness will occur.");
    mono_raise_exception(mono_get_exception_not_implemented());
}

#endif
