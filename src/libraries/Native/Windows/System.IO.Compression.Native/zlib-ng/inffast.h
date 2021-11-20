#ifndef INFFAST_H_
#define INFFAST_H_
/* inffast.h -- header to use inffast.c
 * Copyright (C) 1995-2003, 2010 Mark Adler
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

/* WARNING: this file should *not* be used by applications. It is
   part of the implementation of the compression library and is
   subject to change. Applications should only use zlib.h.
 */

void Z_INTERNAL zng_inflate_fast(PREFIX3(stream) *strm, unsigned long start);

#define INFLATE_FAST_MIN_HAVE 8
#define INFLATE_FAST_MIN_LEFT 258

#endif /* INFFAST_H_ */
