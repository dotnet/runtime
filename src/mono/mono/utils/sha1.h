/*	$OpenBSD: sha1.h,v 1.24 2012/12/05 23:19:57 deraadt Exp $	*/

/*
 * SHA-1 in C
 * By Steve Reid <steve@edmweb.com>
 * 100% Public Domain
 */

#ifndef _SHA1_H
#define _SHA1_H

#include <glib.h>

#define	SHA1_BLOCK_LENGTH		64
#define	SHA1_DIGEST_LENGTH		20
#define	SHA1_DIGEST_STRING_LENGTH	(SHA1_DIGEST_LENGTH * 2 + 1)

typedef struct {
    guint32 state[5];
    guint64 count;
    guint8 buffer[SHA1_BLOCK_LENGTH];
} SHA1_CTX;

G_BEGIN_DECLS
void mono_SHA1Init(SHA1_CTX *);
void mono_SHA1Pad(SHA1_CTX *);
void mono_SHA1Transform(guint32 [5], const guint8 [SHA1_BLOCK_LENGTH]);
void mono_SHA1Update(SHA1_CTX *, const guint8 *, size_t);
void mono_SHA1Final(guint8 [SHA1_DIGEST_LENGTH], SHA1_CTX *);
char *mono_SHA1End(SHA1_CTX *, char *);
G_END_DECLS

#define HTONDIGEST(x) do {                                              \
        x[0] = htonl(x[0]);                                             \
        x[1] = htonl(x[1]);                                             \
        x[2] = htonl(x[2]);                                             \
        x[3] = htonl(x[3]);                                             \
        x[4] = htonl(x[4]); } while (0)

#define NTOHDIGEST(x) do {                                              \
        x[0] = ntohl(x[0]);                                             \
        x[1] = ntohl(x[1]);                                             \
        x[2] = ntohl(x[2]);                                             \
        x[3] = ntohl(x[3]);                                             \
        x[4] = ntohl(x[4]); } while (0)

#endif /* _SHA1_H */
