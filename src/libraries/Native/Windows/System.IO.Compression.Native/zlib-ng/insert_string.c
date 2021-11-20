/* insert_string_c -- insert_string variant for c
 *
 * Copyright (C) 1995-2013 Jean-loup Gailly and Mark Adler
 * For conditions of distribution and use, see copyright notice in zlib.h
 *
 */

#include "zbuild.h"
#include "deflate.h"

/* ===========================================================================
 * Update a hash value with the given input byte
 * IN  assertion: all calls to to UPDATE_HASH are made with consecutive
 *    input characters, so that a running hash key can be computed from the
 *    previous key instead of complete recalculation each time.
 */
#define HASH_SLIDE 16  // Number of bits to slide hash

#define UPDATE_HASH(s, h, val) \
    h = ((val * 2654435761U) >> HASH_SLIDE);

#define INSERT_STRING       insert_string_c
#define QUICK_INSERT_STRING quick_insert_string_c

#include "insert_string_tpl.h"
