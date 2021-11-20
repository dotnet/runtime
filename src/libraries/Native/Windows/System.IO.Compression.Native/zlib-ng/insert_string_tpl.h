#ifndef INSERT_STRING_H_
#define INSERT_STRING_H_

/* insert_string.h -- Private insert_string functions shared with more than
 *                    one insert string implementation
 *
 * Copyright (C) 1995-2013 Jean-loup Gailly and Mark Adler
 *
 * Copyright (C) 2013 Intel Corporation. All rights reserved.
 * Authors:
 *  Wajdi Feghali   <wajdi.k.feghali@intel.com>
 *  Jim Guilford    <james.guilford@intel.com>
 *  Vinodh Gopal    <vinodh.gopal@intel.com>
 *  Erdinc Ozturk   <erdinc.ozturk@intel.com>
 *  Jim Kukunas     <james.t.kukunas@linux.intel.com>
 *
 * Portions are Copyright (C) 2016 12Sided Technology, LLC.
 * Author:
 *  Phil Vachon     <pvachon@12sidedtech.com>
 *
 * For conditions of distribution and use, see copyright notice in zlib.h
 *
 */

/* ===========================================================================
 * Quick insert string str in the dictionary and set match_head to the previous head
 * of the hash chain (the most recent string with same hash key). Return
 * the previous length of the hash chain.
 */
Z_INTERNAL Pos QUICK_INSERT_STRING(deflate_state *const s, const uint32_t str) {
    Pos head;
    uint8_t *strstart = s->window + str;
    uint32_t val, hm, h = 0;

#ifdef UNALIGNED_OK
    val = *(uint32_t *)(strstart);
#else
    val  = ((uint32_t)(strstart[0]));
    val |= ((uint32_t)(strstart[1]) << 8);
    val |= ((uint32_t)(strstart[2]) << 16);
    val |= ((uint32_t)(strstart[3]) << 24);
#endif

    UPDATE_HASH(s, h, val);
    hm = h & HASH_MASK;

    head = s->head[hm];
    if (LIKELY(head != str)) {
        s->prev[str & s->w_mask] = head;
        s->head[hm] = (Pos)str;
    }
    return head;
}

/* ===========================================================================
 * Insert string str in the dictionary and set match_head to the previous head
 * of the hash chain (the most recent string with same hash key). Return
 * the previous length of the hash chain.
 * IN  assertion: all calls to to INSERT_STRING are made with consecutive
 *    input characters and the first MIN_MATCH bytes of str are valid
 *    (except for the last MIN_MATCH-1 bytes of the input file).
 */
Z_INTERNAL void INSERT_STRING(deflate_state *const s, const uint32_t str, uint32_t count) {
    uint8_t *strstart = s->window + str;
    uint8_t *strend = strstart + count - 1; /* last position */

    for (Pos idx = (Pos)str; strstart <= strend; idx++, strstart++) {
        uint32_t val, hm, h = 0;

#ifdef UNALIGNED_OK
        val = *(uint32_t *)(strstart);
#else
        val  = ((uint32_t)(strstart[0]));
        val |= ((uint32_t)(strstart[1]) << 8);
        val |= ((uint32_t)(strstart[2]) << 16);
        val |= ((uint32_t)(strstart[3]) << 24);
#endif

        UPDATE_HASH(s, h, val);
        hm = h & HASH_MASK;

        Pos head = s->head[hm];
        if (LIKELY(head != idx)) {
            s->prev[idx & s->w_mask] = head;
            s->head[hm] = idx;
        }
    }
}
#endif
