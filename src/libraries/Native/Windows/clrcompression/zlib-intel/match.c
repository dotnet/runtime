/*
 * Set match_start to the longest match starting at the given string and
 * return its length. Matches shorter or equal to prev_length are discarded,
 * in which case the result is equal to prev_length and match_start is garbage.
 *
 * IN assertions: cur_match is the head of the hash chain for the current
 * 	string (strstart) and its distance is <= MAX_DIST, and prev_length >=1
 * OUT assertion: the match length is not greater than s->lookahead
 */

#include <stdint.h>

#include "deflate.h"

#ifdef FASTEST
#define longest_match fastest_longest_match
#elif (defined(UNALIGNED_OK) && MAX_MATCH == 258)
#define longest_match std2_longest_match
#else
#define longest_match std1_longest_match
#endif

/*
 * Standard longest_match
 *
 */
local unsigned std1_longest_match(deflate_state *z_const s, IPos cur_match)
{
	z_const unsigned wmask = s->w_mask;
    z_const Posf *prev = s->prev;

	unsigned chain_length;
	IPos limit;
	int len, best_len, nice_match;
	unsigned char *scan, *match, *strend, scan_end, scan_end1;
	
	/*
	 * The code is optimized for HASH_BITS >= 8 and MAX_MATCH-2 multiple
	 * of 16. It is easy to get rid of this optimization if necessary.
	 */
	Assert(s->hash_bits >= 8 && MAX_MATCH == 258, "Code too clever");

	/*
	 * Do not waste too much time if we already have a good match
	 */
	best_len = s->prev_length;
	chain_length = s->max_chain_length;
	if ((unsigned)best_len >= s->good_match)
		chain_length >>= 2;

	/*
	 * Do not looks for matches beyond the end of the input. This is
	 * necessary to make deflate deterministic
	 */
    nice_match = (uInt)s->nice_match > s->lookahead ? s->lookahead : s->nice_match;

	/*
	 * Stop when cur_match becomes <= limit. To simplify the code,
	 * we prevent matches with the string of window index 0
	 */
	limit = s->strstart > MAX_DIST(s) ? s->strstart - MAX_DIST(s) : 0;

	scan = s->window + s->strstart;
	strend = s->window + s->strstart + MAX_MATCH;
	scan_end1 = scan[best_len-1];
	scan_end = scan[best_len];

	Assert((unsigned long)s->strstart <= s->window_size - MIN_LOOKAHEAD,
		"need lookahead");
	do {
		Assert(cur_match < s->strstart, "no future");
		match = s->window + cur_match;

		/*
		 * Skip to next match if the match length cannot increase
		 * or if the match length is less than 2. Note that the checks
		 * below for insufficient lookahead only occur occasionally
		 * for performance reasons. Therefore uninitialized memory
		 * will be accessed and conditional jumps will be made that
		 * depend on those values. However the length of the match
		 * is limited to the lookahead, so the output of deflate is not
		 * affected by the uninitialized values.
		 */
		if (match[best_len] != scan_end ||
		match[best_len-1] != scan_end1 ||
		*match != *scan ||
		*++match != scan[1])
			continue;

		/*
		 * The check at best_len-1 can be removed because it will
		 * be made again later. (This heuristic is not always a win.)
		 * It is not necessary to compare scan[2] and match[2] since
		 * they are always equal when the other bytes match, given
		 * that the hash keys are equal and that HASH_BITS >= 8.
		 */
		scan += 2;
		match++;
		Assert(*scan == *match, "match[2]?");

		/*
		 * We check for insufficient lookahead only every 8th
		 * comparision; the 256th check will be made at strstart + 258.
		 */
		do {
		} while (*++scan == *++match && *++scan == *++match &&
			 *++scan == *++match && *++scan == *++match &&
			 *++scan == *++match && *++scan == *++match &&
			 *++scan == *++match && *++scan == *++match &&
			 scan < strend);

		Assert(scan <= s->window+(unsigned int)(s->window_size-1),
			"wild scan");

		len = MAX_MATCH - (int)(strend - scan);
		scan = strend - MAX_MATCH;

		if (len > best_len) {
			s->match_start = cur_match;
			best_len = len;
			if (len >= nice_match)
				break;
			scan_end1 = scan[best_len-1];
			scan_end = scan[best_len];
		} else {
			/*
			 * The probability of finding a match later if we here
			 * is pretty low, so for performance it's best to
			 * outright stop here for the lower compression levels
			 */
			if (s->level < 6)
				break;
		}
	} while ((cur_match = prev[cur_match & wmask]) > limit && --chain_length);

	if ((unsigned int)best_len <= s->lookahead)
		return best_len;
	return s->lookahead;
}

/*
 * UNALIGNED_OK longest_match
 *
 */
local unsigned std2_longest_match(deflate_state *z_const s, IPos cur_match)
{
	z_const unsigned wmask = s->w_mask;
    z_const Posf *prev = s->prev;

	unsigned short scan_start, scan_end;
	unsigned chain_length;
	IPos limit;
	int len, best_len, nice_match;
	unsigned char *scan, *strend;
	
	/*
	 * The code is optimized for HASH_BITS >= 8 and MAX_MATCH-2 multiple
	 * of 16. It is easy to get rid of this optimization if necessary.
	 */
	Assert(s->hash_bits >= 8 && MAX_MATCH == 258, "Code too clever");

	/*
	 * Do not waste too much time if we already have a good match
	 */
	best_len = s->prev_length;
	chain_length = s->max_chain_length;
	if ((unsigned)best_len >= s->good_match)
		chain_length >>= 2;

	/*
	 * Do not looks for matches beyond the end of the input. This is
	 * necessary to make deflate deterministic
	 */
    nice_match = (uInt)s->nice_match > s->lookahead ? s->lookahead : s->nice_match;

	/*
	 * Stop when cur_match becomes <= limit. To simplify the code,
	 * we prevent matches with the string of window index 0
	 */
	limit = s->strstart > MAX_DIST(s) ? s->strstart - MAX_DIST(s) : 0;

	scan = s->window + s->strstart;
	strend = s->window + s->strstart + MAX_MATCH - 1;
	scan_start = *(unsigned short *)scan;
	scan_end = *(unsigned short *)(scan + best_len-1);

	Assert((unsigned long)s->strstart <= s->window_size - MIN_LOOKAHEAD,
		"need lookahead");
	do {
        unsigned char *match;
		Assert(cur_match < s->strstart, "no future");
		match = s->window + cur_match;
		
		/*
		 * Skip to next match if the match length cannot increase
		 * or if the match length is less than 2. Note that the checks
		 * below for insufficient lookahead only occur occasionally
		 * for performance reasons. Therefore uninitialized memory
		 * will be accessed and conditional jumps will be made that
		 * depend on those values. However the length of the match
		 * is limited to the lookahead, so the output of deflate is not
		 * affected by the uninitialized values.
		 */
		if (zlikely((*(unsigned short *)(match + best_len - 1) != scan_end)))
			continue;
		if (*(unsigned short *)match != scan_start)
			continue;

		/* It is not necessary to compare scan[2] and match[2] since
		 * they are always equal when the other bytes match, given that
		 * the hash keys are equal and that HASH_BITS >= 8. Compare 2
		 * bytes at a time at strstart+3, +5, ... up to strstart+257.
		 * We check for insufficient lookahead only every 4th
		 * comparison; the 128th check will be made at strstart+257.
		 * If MAX_MATCH-2 is not a multiple of 8, it is necessary to
		 * put more guard bytes at the end of the window, or to check
		 * more often for insufficient lookahead.
		 */
		Assert(scan[2] == match[2], "scan[2]?");
		scan++;
		match++;

		do {
		} while (*(unsigned short *)(scan += 2)== *(unsigned short *)(match += 2)&&
			 *(unsigned short *)(scan += 2)== *(unsigned short *)(match += 2)&&
			 *(unsigned short *)(scan += 2)== *(unsigned short *)(match += 2)&&
			 *(unsigned short *)(scan += 2)== *(unsigned short *)(match += 2)&&
			 scan < strend);

		/*
		 * Here, scan <= window + strstart + 257
		 */
		Assert(scan <= s->window+(unsigned)(s->window_size-1), "wild scan");
		if (*scan == *match)
			scan++;

		len = (MAX_MATCH -1) - (int)(strend-scan);
		scan = strend - (MAX_MATCH-1);

		if (len > best_len) {
			s->match_start = cur_match;
			best_len = len;
			if (len >= nice_match)
				break;
			scan_end = *(unsigned short *)(scan + best_len - 1);
		} else {
			/*
			 * The probability of finding a match later if we here
			 * is pretty low, so for performance it's best to
			 * outright stop here for the lower compression levels
			 */
			if (s->level < 6)
                break;
		}
	} while (--chain_length && (cur_match = prev[cur_match & wmask]) > limit);

	if ((unsigned)best_len <= s->lookahead)
		return best_len;
	return s->lookahead;
}

/*
 * FASTEST-only longest_match
 *
 */
local unsigned fastest_longest_match(deflate_state *z_const s, IPos cur_match)
{
	unsigned char *scan, *match, *strend;
	int len;

	/*
	 * The code is optimized for HASH_BITS >= 8 and MAX_MATCH-2 multiple 
	 * of 16. It is easy to get rid of this optimization if necessary
	 */
	Assert(s->hash_bits >= 8 && MAX_MATCH == 258, "Code too clever");

	Assert((unsigned long)s->strstart <= s->window_size - MIN_LOOKAHEAD,
		"need lookahead");

	Assert(cur_match < s->strstart, "no future");

	match = s->window + cur_match;
	scan = s->window + s->strstart;
	strend = s->window + s->strstart + MAX_MATCH;

	if (*match++ != *scan++ || *match++ != *scan++)
		return MIN_MATCH-1;

	/*
	 * The check at best_len-1 can be removed because it will be made
	 * again later. (This heuristic is not always a win.) It is not
	 * necessary to compare scan[2] and match[2] since they are always
	 * equal when the other bytes match, given that the hash keys are equal
	 * and that HASH_BITS >= 8.
	 */
	Assert(*scan == *match, "match[2]?");

	do {
	} while (*++scan == *++match && *++scan == *++match &&
		 *++scan == *++match && *++scan == *++match &&
		 *++scan == *++match && *++scan == *++match &&
		 *++scan == *++match && *++scan == *++match &&
		 scan < strend);
	
	Assert(scan <= s->window+(unsigned int)(s->window_size-1), "wild scan");

	len = MAX_MATCH - (long)(strend - scan);
	if (len < MIN_MATCH)
		return MIN_MATCH-1;
	
	s->match_start = cur_match;
	return (unsigned)len <= s->lookahead ? len : s->lookahead;
}
