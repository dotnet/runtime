/* deflate_p.h -- Private inline functions and macros shared with more than
 *                one deflate method
 *
 * Copyright (C) 1995-2013 Jean-loup Gailly and Mark Adler
 * For conditions of distribution and use, see copyright notice in zlib.h
 *
 */

#ifndef DEFLATE_P_H
#define DEFLATE_P_H

/* Forward declare common non-inlined functions declared in deflate.c */

#ifdef ZLIB_DEBUG
void check_match(deflate_state *s, Pos start, Pos match, int length);
#else
#define check_match(s, start, match, length)
#endif
void flush_pending(PREFIX3(stream) *strm);

/* ===========================================================================
 * Save the match info and tally the frequency counts. Return true if
 * the current block must be flushed.
 */

extern const unsigned char Z_INTERNAL zng_length_code[];
extern const unsigned char Z_INTERNAL zng_dist_code[];

static inline int zng_tr_tally_lit(deflate_state *s, unsigned char c) {
    /* c is the unmatched char */
    s->sym_buf[s->sym_next++] = 0;
    s->sym_buf[s->sym_next++] = 0;
    s->sym_buf[s->sym_next++] = c;
    s->dyn_ltree[c].Freq++;
    Tracevv((stderr, "%c", c));
    Assert(c <= (MAX_MATCH-MIN_MATCH), "zng_tr_tally: bad literal");
    return (s->sym_next == s->sym_end);
}

static inline int zng_tr_tally_dist(deflate_state *s, uint32_t dist, uint32_t len) {
    /* dist: distance of matched string */
    /* len: match length-MIN_MATCH */
    s->sym_buf[s->sym_next++] = (uint8_t)(dist);
    s->sym_buf[s->sym_next++] = (uint8_t)(dist >> 8);
    s->sym_buf[s->sym_next++] = (uint8_t)len;
    s->matches++;
    dist--;
    Assert(dist < MAX_DIST(s) && (uint16_t)d_code(dist) < (uint16_t)D_CODES, 
        "zng_tr_tally: bad match");

    s->dyn_ltree[zng_length_code[len]+LITERALS+1].Freq++;
    s->dyn_dtree[d_code(dist)].Freq++;
    return (s->sym_next == s->sym_end);
}

/* ===========================================================================
 * Flush the current block, with given end-of-file flag.
 * IN assertion: strstart is set to the end of the current match.
 */
#define FLUSH_BLOCK_ONLY(s, last) { \
    zng_tr_flush_block(s, (s->block_start >= 0 ? \
                   (char *)&s->window[(unsigned)s->block_start] : \
                   NULL), \
                   (uint32_t)((int)s->strstart - s->block_start), \
                   (last)); \
    s->block_start = (int)s->strstart; \
    flush_pending(s->strm); \
}

/* Same but force premature exit if necessary. */
#define FLUSH_BLOCK(s, last) { \
    FLUSH_BLOCK_ONLY(s, last); \
    if (s->strm->avail_out == 0) return (last) ? finish_started : need_more; \
}

/* Maximum stored block length in deflate format (not including header). */
#define MAX_STORED 65535

#endif
