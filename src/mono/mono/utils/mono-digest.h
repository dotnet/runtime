/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/**
 * \file
 */


#ifndef __MONO_DIGEST_H__
#define __MONO_DIGEST_H__

#include <config.h>
#include <glib.h>
#include <mono/utils/mono-publib.h>

#if HAVE_COMMONCRYPTO_COMMONDIGEST_H

#include <CommonCrypto/CommonDigest.h>

#define MonoSHA1Context	CC_SHA1_CTX

#else

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

#endif	/* __MONO_DIGEST_H__ */
