/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/**
 * \file
 * This code implements the MD5 message-digest algorithm.
 * The algorithm is due to Ron Rivest.  This code was
 * written by Colin Plumb in 1993, no copyright is claimed.
 * This code is in the public domain; do with it what you wish.
 *
 * Equivalent code is available from RSA Data Security, Inc.
 * This code has been tested against that, and is equivalent,
 * except that you don't need to include two pages of legalese
 * with every copy.
 *
 * To compute the message digest of a chunk of bytes, declare an
 * MD5Context structure, pass it to rpmMD5Init, call rpmMD5Update as
 * needed on buffers full of bytes, and then call rpmMD5Final, which
 * will fill a supplied 16-byte array with the digest.
 */

/* parts of this file are :
 * Written March 1993 by Branko Lankester
 * Modified June 1993 by Colin Plumb for altered md5.c.
 * Modified October 1995 by Erik Troan for RPM
 */


#ifndef __MONO_DIGEST_H__
#define __MONO_DIGEST_H__

#include <config.h>
#include <glib.h>
#include <mono/utils/mono-publib.h>

G_BEGIN_DECLS

#if HAVE_COMMONCRYPTO_COMMONDIGEST_H

#include <CommonCrypto/CommonDigest.h>

#define MonoSHA1Context	CC_SHA1_CTX
#define MonoMD5Context	CC_MD5_CTX

#else

typedef struct {
	guint32 buf[4];
	guint32 bits[2];
	guchar in[64];
	gint doByteReverse;
} MonoMD5Context;

#endif

MONO_API void mono_md5_get_digest (const guchar *buffer, gint buffer_size, guchar digest[16]);

/* use this one when speed is needed */
/* for use in provider code only */
MONO_API void mono_md5_get_digest_from_file (const gchar *filename, guchar digest[16]);

/* raw routines */
MONO_API void mono_md5_init   (MonoMD5Context *ctx);
MONO_API void mono_md5_update (MonoMD5Context *ctx, const guchar *buf, guint32 len);
MONO_API void mono_md5_final  (MonoMD5Context *ctx, guchar digest[16]);

#if !HAVE_COMMONCRYPTO_COMMONDIGEST_H

typedef struct {
    guint32 state[5];
    guint32 count[2];
    unsigned char buffer[64];
} MonoSHA1Context;

#endif

MONO_API void mono_sha1_get_digest (const guchar *buffer, gint buffer_size, guchar digest [20]);
MONO_API void mono_sha1_get_digest_from_file (const gchar *filename, guchar digest [20]);

MONO_API void mono_sha1_init   (MonoSHA1Context* context);
MONO_API void mono_sha1_update (MonoSHA1Context* context, const guchar* data, guint32 len);
MONO_API void mono_sha1_final  (MonoSHA1Context* context, unsigned char digest[20]);

MONO_API void mono_digest_get_public_token (guchar* token, const guchar *pubkey, guint32 len);

G_END_DECLS
#endif	/* __MONO_DIGEST_H__ */
