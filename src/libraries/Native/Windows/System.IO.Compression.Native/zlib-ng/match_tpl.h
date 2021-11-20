
#include "zbuild.h"
#include "deflate.h"
#include "functable.h"

#ifndef MATCH_TPL_H
#define MATCH_TPL_H

#ifdef UNALIGNED_OK
#  ifdef UNALIGNED64_OK
typedef uint64_t        bestcmp_t;
#  else
typedef uint32_t        bestcmp_t;
#  endif
#else
typedef uint8_t         bestcmp_t;
#endif

#define EARLY_EXIT_TRIGGER_LEVEL 5

#endif

/* Set match_start to the longest match starting at the given string and
 * return its length. Matches shorter or equal to prev_length are discarded,
 * in which case the result is equal to prev_length and match_start is garbage.
 *
 * IN assertions: cur_match is the head of the hash chain for the current
 * string (strstart) and its distance is <= MAX_DIST, and prev_length >=1
 * OUT assertion: the match length is not greater than s->lookahead
 */
Z_INTERNAL uint32_t LONGEST_MATCH(deflate_state *const s, Pos cur_match) {
    unsigned int strstart = s->strstart;
    const unsigned wmask = s->w_mask;
    unsigned char *window = s->window;
    unsigned char *scan = window + strstart;
    Z_REGISTER unsigned char *mbase_start = window;
    Z_REGISTER unsigned char *mbase_end;
    const Pos *prev = s->prev;
    Pos limit;
    int32_t early_exit;
    uint32_t chain_length, nice_match, best_len, offset;
    uint32_t lookahead = s->lookahead;
    bestcmp_t scan_end;
#ifndef UNALIGNED_OK
    bestcmp_t scan_end0;
#else
    bestcmp_t scan_start;
#endif

#define GOTO_NEXT_CHAIN \
    if (--chain_length && (cur_match = prev[cur_match & wmask]) > limit) \
        continue; \
    return best_len;

    /* The code is optimized for MAX_MATCH-2 multiple of 16. */
    Assert(MAX_MATCH == 258, "Code too clever");

    best_len = s->prev_length ? s->prev_length : 1;

    /* Calculate read offset which should only extend an extra byte
     * to find the next best match length.
     */
    offset = best_len-1;
#ifdef UNALIGNED_OK
    if (best_len >= sizeof(uint32_t)) {
        offset -= 2;
#ifdef UNALIGNED64_OK
        if (best_len >= sizeof(uint64_t))
            offset -= 4;
#endif
    }
#endif

    scan_end   = *(bestcmp_t *)(scan+offset);
#ifndef UNALIGNED_OK
    scan_end0  = *(bestcmp_t *)(scan+offset+1);
#else
    scan_start = *(bestcmp_t *)(scan);
#endif
    mbase_end  = (mbase_start+offset);

    /* Do not waste too much time if we already have a good match */
    chain_length = s->max_chain_length;
    early_exit = s->level < EARLY_EXIT_TRIGGER_LEVEL;
    if (best_len >= s->good_match)
        chain_length >>= 2;
    nice_match = (uint32_t)s->nice_match;

    /* Stop when cur_match becomes <= limit. To simplify the code,
     * we prevent matches with the string of window index 0
     */
    limit = strstart > MAX_DIST(s) ? (Pos)(strstart - MAX_DIST(s)) : 0;

    Assert((unsigned long)strstart <= s->window_size - MIN_LOOKAHEAD, "need lookahead");
    for (;;) {
        if (cur_match >= strstart)
            break;

        /* Skip to next match if the match length cannot increase or if the match length is
         * less than 2. Note that the checks below for insufficient lookahead only occur
         * occasionally for performance reasons.
         * Therefore uninitialized memory will be accessed and conditional jumps will be made
         * that depend on those values. However the length of the match is limited to the
         * lookahead, so the output of deflate is not affected by the uninitialized values.
         */
#ifdef UNALIGNED_OK
        if (best_len < sizeof(uint32_t)) {
            for (;;) {
                if (*(uint16_t *)(mbase_end+cur_match) == (uint16_t)scan_end &&
                    *(uint16_t *)(mbase_start+cur_match) == (uint16_t)scan_start)
                    break;
                GOTO_NEXT_CHAIN;
            }
#  ifdef UNALIGNED64_OK
        } else if (best_len >= sizeof(uint64_t)) {
            for (;;) {
                if (*(uint64_t *)(mbase_end+cur_match) == (uint64_t)scan_end &&
                    *(uint64_t *)(mbase_start+cur_match) == (uint64_t)scan_start)
                    break;
                GOTO_NEXT_CHAIN;
            }
#  endif
        } else {
            for (;;) {
                if (*(uint32_t *)(mbase_end+cur_match) == (uint32_t)scan_end &&
                    *(uint32_t *)(mbase_start+cur_match) == (uint32_t)scan_start)
                    break;
                GOTO_NEXT_CHAIN;
            }
        }
#else
        for (;;) {
            if (mbase_end[cur_match] == scan_end && mbase_end[cur_match+1] == scan_end0 &&
                mbase_start[cur_match] == scan[0] && mbase_start[cur_match+1] == scan[1])
                break;
            GOTO_NEXT_CHAIN;
        }
#endif
        uint32_t len = COMPARE256(scan+2, mbase_start+cur_match+2) + 2;
        Assert(scan+len <= window+(unsigned)(s->window_size-1), "wild scan");

        if (len > best_len) {
            s->match_start = cur_match;
            /* Do not look for matches beyond the end of the input. */
            if (len > lookahead)
                return lookahead;
            best_len = len;
            if (best_len >= nice_match)
                return best_len;

            offset = best_len-1;
#ifdef UNALIGNED_OK
            if (best_len >= sizeof(uint32_t)) {
                offset -= 2;
#ifdef UNALIGNED64_OK
                if (best_len >= sizeof(uint64_t))
                    offset -= 4;
#endif
            }
#endif
            scan_end = *(bestcmp_t *)(scan+offset);
#ifndef UNALIGNED_OK
            scan_end0 = *(bestcmp_t *)(scan+offset+1);
#endif
            mbase_end = (mbase_start+offset);
        } else if (UNLIKELY(early_exit)) {
            /* The probability of finding a match later if we here is pretty low, so for
             * performance it's best to outright stop here for the lower compression levels
             */
            break;
        }
        GOTO_NEXT_CHAIN;
    }

    return best_len;
}

#undef LONGEST_MATCH
#undef COMPARE256
#undef COMPARE258
