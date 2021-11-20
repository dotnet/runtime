/* compress.c -- compress a memory buffer
 * Copyright (C) 1995-2005, 2014, 2016 Jean-loup Gailly, Mark Adler
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#define ZLIB_INTERNAL
#include "zbuild.h"
#if defined(ZLIB_COMPAT)
#  include "zlib.h"
#else
#  include "zlib-ng.h"
#endif

/* ===========================================================================
     Compresses the source buffer into the destination buffer. The level
   parameter has the same meaning as in deflateInit.  sourceLen is the byte
   length of the source buffer. Upon entry, destLen is the total size of the
   destination buffer, which must be at least 0.1% larger than sourceLen plus
   12 bytes. Upon exit, destLen is the actual size of the compressed buffer.

     compress2 returns Z_OK if success, Z_MEM_ERROR if there was not enough
   memory, Z_BUF_ERROR if there was not enough room in the output buffer,
   Z_STREAM_ERROR if the level parameter is invalid.
*/
int Z_EXPORT PREFIX(compress2)(unsigned char *dest, z_size_t *destLen, const unsigned char *source,
                        z_size_t sourceLen, int level) {
    PREFIX3(stream) stream;
    int err;
    const unsigned int max = (unsigned int)-1;
    z_size_t left;

    left = *destLen;
    *destLen = 0;

    stream.zalloc = NULL;
    stream.zfree = NULL;
    stream.opaque = NULL;

    err = PREFIX(deflateInit)(&stream, level);
    if (err != Z_OK)
        return err;

    stream.next_out = dest;
    stream.avail_out = 0;
    stream.next_in = (z_const unsigned char *)source;
    stream.avail_in = 0;

    do {
        if (stream.avail_out == 0) {
            stream.avail_out = left > (unsigned long)max ? max : (unsigned int)left;
            left -= stream.avail_out;
        }
        if (stream.avail_in == 0) {
            stream.avail_in = sourceLen > (unsigned long)max ? max : (unsigned int)sourceLen;
            sourceLen -= stream.avail_in;
        }
        err = PREFIX(deflate)(&stream, sourceLen ? Z_NO_FLUSH : Z_FINISH);
    } while (err == Z_OK);

    *destLen = (z_size_t)stream.total_out;
    PREFIX(deflateEnd)(&stream);
    return err == Z_STREAM_END ? Z_OK : err;
}

/* ===========================================================================
 */
int Z_EXPORT PREFIX(compress)(unsigned char *dest, z_size_t *destLen, const unsigned char *source, z_size_t sourceLen) {
    return PREFIX(compress2)(dest, destLen, source, sourceLen, Z_DEFAULT_COMPRESSION);
}

/* ===========================================================================
   If the default memLevel or windowBits for deflateInit() is changed, then
   this function needs to be updated.
 */
z_size_t Z_EXPORT PREFIX(compressBound)(z_size_t sourceLen) {
#ifndef NO_QUICK_STRATEGY
    /* Quick deflate strategy worse case is 9 bits per literal, rounded to nearest byte,
       plus the size of block & gzip headers and footers */
    return sourceLen + ((sourceLen + 13 + 7) >> 3) + 18;
#else
    return sourceLen + (sourceLen >> 12) + (sourceLen >> 14) + (sourceLen >> 25) + 13;
#endif
}
