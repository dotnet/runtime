/* inflate.c -- zlib decompression
 * Copyright (C) 1995-2016 Mark Adler
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#include "zbuild.h"
#include "zutil.h"
#include "inftrees.h"
#include "inflate.h"
#include "inffast.h"
#include "inflate_p.h"
#include "inffixed_tbl.h"
#include "functable.h"

/* Architecture-specific hooks. */
#ifdef S390_DFLTCC_INFLATE
#  include "arch/s390/dfltcc_inflate.h"
#else
/* Memory management for the inflate state. Useful for allocating arch-specific extension blocks. */
#  define ZALLOC_STATE(strm, items, size) ZALLOC(strm, items, size)
#  define ZFREE_STATE(strm, addr) ZFREE(strm, addr)
#  define ZCOPY_STATE(dst, src, size) memcpy(dst, src, size)
/* Memory management for the window. Useful for allocation the aligned window. */
#  define ZALLOC_WINDOW(strm, items, size) ZALLOC(strm, items, size)
#  define ZFREE_WINDOW(strm, addr) ZFREE(strm, addr)
/* Invoked at the end of inflateResetKeep(). Useful for initializing arch-specific extension blocks. */
#  define INFLATE_RESET_KEEP_HOOK(strm) do {} while (0)
/* Invoked at the beginning of inflatePrime(). Useful for updating arch-specific buffers. */
#  define INFLATE_PRIME_HOOK(strm, bits, value) do {} while (0)
/* Invoked at the beginning of each block. Useful for plugging arch-specific inflation code. */
#  define INFLATE_TYPEDO_HOOK(strm, flush) do {} while (0)
/* Returns whether zlib-ng should compute a checksum. Set to 0 if arch-specific inflation code already does that. */
#  define INFLATE_NEED_CHECKSUM(strm) 1
/* Returns whether zlib-ng should update a window. Set to 0 if arch-specific inflation code already does that. */
#  define INFLATE_NEED_UPDATEWINDOW(strm) 1
/* Invoked at the beginning of inflateMark(). Useful for updating arch-specific pointers and offsets. */
#  define INFLATE_MARK_HOOK(strm) do {} while (0)
/* Invoked at the beginning of inflateSyncPoint(). Useful for performing arch-specific state checks. */
#define INFLATE_SYNC_POINT_HOOK(strm) do {} while (0)
#endif

/* function prototypes */
static int inflateStateCheck(PREFIX3(stream) *strm);
static int updatewindow(PREFIX3(stream) *strm, const unsigned char *end, uint32_t copy);
static uint32_t syncsearch(uint32_t *have, const unsigned char *buf, uint32_t len);

static int inflateStateCheck(PREFIX3(stream) *strm) {
    struct inflate_state *state;
    if (strm == NULL || strm->zalloc == NULL || strm->zfree == NULL)
        return 1;
    state = (struct inflate_state *)strm->state;
    if (state == NULL || state->strm != strm || state->mode < HEAD || state->mode > SYNC)
        return 1;
    return 0;
}

int32_t Z_EXPORT PREFIX(inflateResetKeep)(PREFIX3(stream) *strm) {
    struct inflate_state *state;

    if (inflateStateCheck(strm))
        return Z_STREAM_ERROR;
    state = (struct inflate_state *)strm->state;
    strm->total_in = strm->total_out = state->total = 0;
    strm->msg = NULL;
    if (state->wrap)        /* to support ill-conceived Java test suite */
        strm->adler = state->wrap & 1;
    state->mode = HEAD;
    state->check = ADLER32_INITIAL_VALUE;
    state->last = 0;
    state->havedict = 0;
    state->flags = -1;
    state->dmax = 32768U;
    state->head = NULL;
    state->hold = 0;
    state->bits = 0;
    state->lencode = state->distcode = state->next = state->codes;
    state->sane = 1;
    state->back = -1;
    INFLATE_RESET_KEEP_HOOK(strm);  /* hook for IBM Z DFLTCC */
    Tracev((stderr, "inflate: reset\n"));
    return Z_OK;
}

int32_t Z_EXPORT PREFIX(inflateReset)(PREFIX3(stream) *strm) {
    struct inflate_state *state;

    if (inflateStateCheck(strm))
        return Z_STREAM_ERROR;
    state = (struct inflate_state *)strm->state;
    state->wsize = 0;
    state->whave = 0;
    state->wnext = 0;
    return PREFIX(inflateResetKeep)(strm);
}

int32_t Z_EXPORT PREFIX(inflateReset2)(PREFIX3(stream) *strm, int32_t windowBits) {
    int wrap;
    struct inflate_state *state;

    /* get the state */
    if (inflateStateCheck(strm))
        return Z_STREAM_ERROR;
    state = (struct inflate_state *)strm->state;

    /* extract wrap request from windowBits parameter */
    if (windowBits < 0) {
        wrap = 0;
        windowBits = -windowBits;
    } else {
        wrap = (windowBits >> 4) + 5;
#ifdef GUNZIP
        if (windowBits < 48)
            windowBits &= 15;
#endif
    }

    /* set number of window bits, free window if different */
    if (windowBits && (windowBits < 8 || windowBits > 15))
        return Z_STREAM_ERROR;
    if (state->window != NULL && state->wbits != (unsigned)windowBits) {
        ZFREE_WINDOW(strm, state->window);
        state->window = NULL;
    }

    /* update state and reset the rest of it */
    state->wrap = wrap;
    state->wbits = (unsigned)windowBits;
    return PREFIX(inflateReset)(strm);
}

int32_t Z_EXPORT PREFIX(inflateInit2_)(PREFIX3(stream) *strm, int32_t windowBits, const char *version, int32_t stream_size) {
    int32_t ret;
    struct inflate_state *state;

#if defined(X86_FEATURES)
    x86_check_features();
#elif defined(ARM_FEATURES)
    arm_check_features();
#endif

    if (version == NULL || version[0] != PREFIX2(VERSION)[0] || stream_size != (int)(sizeof(PREFIX3(stream))))
        return Z_VERSION_ERROR;
    if (strm == NULL)
        return Z_STREAM_ERROR;
    strm->msg = NULL;                   /* in case we return an error */
    if (strm->zalloc == NULL) {
        strm->zalloc = zng_calloc;
        strm->opaque = NULL;
    }
    if (strm->zfree == NULL)
        strm->zfree = zng_cfree;
    state = (struct inflate_state *) ZALLOC_STATE(strm, 1, sizeof(struct inflate_state));
    if (state == NULL)
        return Z_MEM_ERROR;
    Tracev((stderr, "inflate: allocated\n"));
    strm->state = (struct internal_state *)state;
    state->strm = strm;
    state->window = NULL;
    state->mode = HEAD;     /* to pass state test in inflateReset2() */
    state->chunksize = functable.chunksize();
    ret = PREFIX(inflateReset2)(strm, windowBits);
    if (ret != Z_OK) {
        ZFREE_STATE(strm, state);
        strm->state = NULL;
    }
    return ret;
}

int32_t Z_EXPORT PREFIX(inflateInit_)(PREFIX3(stream) *strm, const char *version, int32_t stream_size) {
    return PREFIX(inflateInit2_)(strm, DEF_WBITS, version, stream_size);
}

int32_t Z_EXPORT PREFIX(inflatePrime)(PREFIX3(stream) *strm, int32_t bits, int32_t value) {
    struct inflate_state *state;

    if (inflateStateCheck(strm))
        return Z_STREAM_ERROR;
    INFLATE_PRIME_HOOK(strm, bits, value);  /* hook for IBM Z DFLTCC */
    state = (struct inflate_state *)strm->state;
    if (bits < 0) {
        state->hold = 0;
        state->bits = 0;
        return Z_OK;
    }
    if (bits > 16 || state->bits + (unsigned int)bits > 32)
        return Z_STREAM_ERROR;
    value &= (1L << bits) - 1;
    state->hold += (unsigned)value << state->bits;
    state->bits += (unsigned int)bits;
    return Z_OK;
}

/*
   Return state with length and distance decoding tables and index sizes set to
   fixed code decoding.  This returns fixed tables from inffixed_tbl.h.
 */

void Z_INTERNAL fixedtables(struct inflate_state *state) {
    state->lencode = lenfix;
    state->lenbits = 9;
    state->distcode = distfix;
    state->distbits = 5;
}

int Z_INTERNAL inflate_ensure_window(struct inflate_state *state) {
    /* if it hasn't been done already, allocate space for the window */
    if (state->window == NULL) {
        unsigned wsize = 1U << state->wbits;
        state->window = (unsigned char *) ZALLOC_WINDOW(state->strm, wsize + state->chunksize, sizeof(unsigned char));
        if (state->window == Z_NULL)
            return 1;
        memset(state->window + wsize, 0, state->chunksize);
    }

    /* if window not in use yet, initialize */
    if (state->wsize == 0) {
        state->wsize = 1U << state->wbits;
        state->wnext = 0;
        state->whave = 0;
    }

    return 0;
}

/*
   Update the window with the last wsize (normally 32K) bytes written before
   returning.  If window does not exist yet, create it.  This is only called
   when a window is already in use, or when output has been written during this
   inflate call, but the end of the deflate stream has not been reached yet.
   It is also called to create a window for dictionary data when a dictionary
   is loaded.

   Providing output buffers larger than 32K to inflate() should provide a speed
   advantage, since only the last 32K of output is copied to the sliding window
   upon return from inflate(), and since all distances after the first 32K of
   output will fall in the output data, making match copies simpler and faster.
   The advantage may be dependent on the size of the processor's data caches.
 */
static int32_t updatewindow(PREFIX3(stream) *strm, const uint8_t *end, uint32_t copy) {
    struct inflate_state *state;
    uint32_t dist;

    state = (struct inflate_state *)strm->state;

    if (inflate_ensure_window(state)) return 1;

    /* copy state->wsize or less output bytes into the circular window */
    if (copy >= state->wsize) {
        memcpy(state->window, end - state->wsize, state->wsize);
        state->wnext = 0;
        state->whave = state->wsize;
    } else {
        dist = state->wsize - state->wnext;
        if (dist > copy)
            dist = copy;
        memcpy(state->window + state->wnext, end - copy, dist);
        copy -= dist;
        if (copy) {
            memcpy(state->window, end - copy, copy);
            state->wnext = copy;
            state->whave = state->wsize;
        } else {
            state->wnext += dist;
            if (state->wnext == state->wsize)
                state->wnext = 0;
            if (state->whave < state->wsize)
                state->whave += dist;
        }
    }
    return 0;
}


/*
   Private macros for inflate()
   Look in inflate_p.h for macros shared with inflateBack()
*/

/* Get a byte of input into the bit accumulator, or return from inflate() if there is no input available. */
#define PULLBYTE() \
    do { \
        if (have == 0) goto inf_leave; \
        have--; \
        hold += ((unsigned)(*next++) << bits); \
        bits += 8; \
    } while (0)

/*
   inflate() uses a state machine to process as much input data and generate as
   much output data as possible before returning.  The state machine is
   structured roughly as follows:

    for (;;) switch (state) {
    ...
    case STATEn:
        if (not enough input data or output space to make progress)
            return;
        ... make progress ...
        state = STATEm;
        break;
    ...
    }

   so when inflate() is called again, the same case is attempted again, and
   if the appropriate resources are provided, the machine proceeds to the
   next state.  The NEEDBITS() macro is usually the way the state evaluates
   whether it can proceed or should return.  NEEDBITS() does the return if
   the requested bits are not available.  The typical use of the BITS macros
   is:

        NEEDBITS(n);
        ... do something with BITS(n) ...
        DROPBITS(n);

   where NEEDBITS(n) either returns from inflate() if there isn't enough
   input left to load n bits into the accumulator, or it continues.  BITS(n)
   gives the low n bits in the accumulator.  When done, DROPBITS(n) drops
   the low n bits off the accumulator.  INITBITS() clears the accumulator
   and sets the number of available bits to zero.  BYTEBITS() discards just
   enough bits to put the accumulator on a byte boundary.  After BYTEBITS()
   and a NEEDBITS(8), then BITS(8) would return the next byte in the stream.

   NEEDBITS(n) uses PULLBYTE() to get an available byte of input, or to return
   if there is no input available.  The decoding of variable length codes uses
   PULLBYTE() directly in order to pull just enough bytes to decode the next
   code, and no more.

   Some states loop until they get enough input, making sure that enough
   state information is maintained to continue the loop where it left off
   if NEEDBITS() returns in the loop.  For example, want, need, and keep
   would all have to actually be part of the saved state in case NEEDBITS()
   returns:

    case STATEw:
        while (want < need) {
            NEEDBITS(n);
            keep[want++] = BITS(n);
            DROPBITS(n);
        }
        state = STATEx;
    case STATEx:

   As shown above, if the next state is also the next case, then the break
   is omitted.

   A state may also return if there is not enough output space available to
   complete that state.  Those states are copying stored data, writing a
   literal byte, and copying a matching string.

   When returning, a "goto inf_leave" is used to update the total counters,
   update the check value, and determine whether any progress has been made
   during that inflate() call in order to return the proper return code.
   Progress is defined as a change in either strm->avail_in or strm->avail_out.
   When there is a window, goto inf_leave will update the window with the last
   output written.  If a goto inf_leave occurs in the middle of decompression
   and there is no window currently, goto inf_leave will create one and copy
   output to the window for the next call of inflate().

   In this implementation, the flush parameter of inflate() only affects the
   return code (per zlib.h).  inflate() always writes as much as possible to
   strm->next_out, given the space available and the provided input--the effect
   documented in zlib.h of Z_SYNC_FLUSH.  Furthermore, inflate() always defers
   the allocation of and copying into a sliding window until necessary, which
   provides the effect documented in zlib.h for Z_FINISH when the entire input
   stream available.  So the only thing the flush parameter actually does is:
   when flush is set to Z_FINISH, inflate() cannot return Z_OK.  Instead it
   will return Z_BUF_ERROR if it has not reached the end of the stream.
 */

int32_t Z_EXPORT PREFIX(inflate)(PREFIX3(stream) *strm, int32_t flush) {
    struct inflate_state *state;
    const unsigned char *next;  /* next input */
    unsigned char *put;         /* next output */
    unsigned have, left;        /* available input and output */
    uint32_t hold;              /* bit buffer */
    unsigned bits;              /* bits in bit buffer */
    uint32_t in, out;           /* save starting available input and output */
    unsigned copy;              /* number of stored or match bytes to copy */
    unsigned char *from;        /* where to copy match bytes from */
    code here;                  /* current decoding table entry */
    code last;                  /* parent table entry */
    unsigned len;               /* length to copy for repeats, bits to drop */
    int32_t ret;                /* return code */
#ifdef GUNZIP
    unsigned char hbuf[4];      /* buffer for gzip header crc calculation */
#endif
    static const uint16_t order[19] = /* permutation of code lengths */
        {16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15};

    if (inflateStateCheck(strm) || strm->next_out == NULL ||
        (strm->next_in == NULL && strm->avail_in != 0))
        return Z_STREAM_ERROR;

    state = (struct inflate_state *)strm->state;
    if (state->mode == TYPE)      /* skip check */
        state->mode = TYPEDO;
    LOAD();
    in = have;
    out = left;
    ret = Z_OK;
    for (;;)
        switch (state->mode) {
        case HEAD:
            if (state->wrap == 0) {
                state->mode = TYPEDO;
                break;
            }
            NEEDBITS(16);
#ifdef GUNZIP
            if ((state->wrap & 2) && hold == 0x8b1f) {  /* gzip header */
                if (state->wbits == 0)
                    state->wbits = 15;
                state->check = PREFIX(crc32)(0L, NULL, 0);
                CRC2(state->check, hold);
                INITBITS();
                state->mode = FLAGS;
                break;
            }
            if (state->head != NULL)
                state->head->done = -1;
            if (!(state->wrap & 1) ||   /* check if zlib header allowed */
#else
            if (
#endif
                ((BITS(8) << 8) + (hold >> 8)) % 31) {
                SET_BAD("incorrect header check");
                break;
            }
            if (BITS(4) != Z_DEFLATED) {
                SET_BAD("unknown compression method");
                break;
            }
            DROPBITS(4);
            len = BITS(4) + 8;
            if (state->wbits == 0)
                state->wbits = len;
            if (len > 15 || len > state->wbits) {
                SET_BAD("invalid window size");
                break;
            }
            state->dmax = 1U << len;
            state->flags = 0;               /* indicate zlib header */
            Tracev((stderr, "inflate:   zlib header ok\n"));
            strm->adler = state->check = ADLER32_INITIAL_VALUE;
            state->mode = hold & 0x200 ? DICTID : TYPE;
            INITBITS();
            break;
#ifdef GUNZIP

        case FLAGS:
            NEEDBITS(16);
            state->flags = (int)(hold);
            if ((state->flags & 0xff) != Z_DEFLATED) {
                SET_BAD("unknown compression method");
                break;
            }
            if (state->flags & 0xe000) {
                SET_BAD("unknown header flags set");
                break;
            }
            if (state->head != NULL)
                state->head->text = (int)((hold >> 8) & 1);
            if ((state->flags & 0x0200) && (state->wrap & 4))
                CRC2(state->check, hold);
            INITBITS();
            state->mode = TIME;

        case TIME:
            NEEDBITS(32);
            if (state->head != NULL)
                state->head->time = hold;
            if ((state->flags & 0x0200) && (state->wrap & 4))
                CRC4(state->check, hold);
            INITBITS();
            state->mode = OS;

        case OS:
            NEEDBITS(16);
            if (state->head != NULL) {
                state->head->xflags = (int)(hold & 0xff);
                state->head->os = (int)(hold >> 8);
            }
            if ((state->flags & 0x0200) && (state->wrap & 4))
                CRC2(state->check, hold);
            INITBITS();
            state->mode = EXLEN;

        case EXLEN:
            if (state->flags & 0x0400) {
                NEEDBITS(16);
                state->length = (uint16_t)hold;
                if (state->head != NULL)
                    state->head->extra_len = (uint16_t)hold;
                if ((state->flags & 0x0200) && (state->wrap & 4))
                    CRC2(state->check, hold);
                INITBITS();
            } else if (state->head != NULL) {
                state->head->extra = NULL;
            }
            state->mode = EXTRA;

        case EXTRA:
            if (state->flags & 0x0400) {
                copy = state->length;
                if (copy > have)
                    copy = have;
                if (copy) {
                    if (state->head != NULL && state->head->extra != NULL) {
                        len = state->head->extra_len - state->length;
                        memcpy(state->head->extra + len, next,
                                len + copy > state->head->extra_max ?
                                state->head->extra_max - len : copy);
                    }
                    if ((state->flags & 0x0200) && (state->wrap & 4))
                        state->check = PREFIX(crc32)(state->check, next, copy);
                    have -= copy;
                    next += copy;
                    state->length -= copy;
                }
                if (state->length)
                    goto inf_leave;
            }
            state->length = 0;
            state->mode = NAME;

        case NAME:
            if (state->flags & 0x0800) {
                if (have == 0) goto inf_leave;
                copy = 0;
                do {
                    len = (unsigned)(next[copy++]);
                    if (state->head != NULL && state->head->name != NULL && state->length < state->head->name_max)
                        state->head->name[state->length++] = (unsigned char)len;
                } while (len && copy < have);
                if ((state->flags & 0x0200) && (state->wrap & 4))
                    state->check = PREFIX(crc32)(state->check, next, copy);
                have -= copy;
                next += copy;
                if (len)
                    goto inf_leave;
            } else if (state->head != NULL) {
                state->head->name = NULL;
            }
            state->length = 0;
            state->mode = COMMENT;

        case COMMENT:
            if (state->flags & 0x1000) {
                if (have == 0) goto inf_leave;
                copy = 0;
                do {
                    len = (unsigned)(next[copy++]);
                    if (state->head != NULL && state->head->comment != NULL
                        && state->length < state->head->comm_max)
                        state->head->comment[state->length++] = (unsigned char)len;
                } while (len && copy < have);
                if ((state->flags & 0x0200) && (state->wrap & 4))
                    state->check = PREFIX(crc32)(state->check, next, copy);
                have -= copy;
                next += copy;
                if (len)
                    goto inf_leave;
            } else if (state->head != NULL) {
                state->head->comment = NULL;
            }
            state->mode = HCRC;

        case HCRC:
            if (state->flags & 0x0200) {
                NEEDBITS(16);
                if ((state->wrap & 4) && hold != (state->check & 0xffff)) {
                    SET_BAD("header crc mismatch");
                    break;
                }
                INITBITS();
            }
            if (state->head != NULL) {
                state->head->hcrc = (int)((state->flags >> 9) & 1);
                state->head->done = 1;
            }
            strm->adler = state->check = PREFIX(crc32)(0L, NULL, 0);
            state->mode = TYPE;
            break;
#endif
        case DICTID:
            NEEDBITS(32);
            strm->adler = state->check = ZSWAP32(hold);
            INITBITS();
            state->mode = DICT;

        case DICT:
            if (state->havedict == 0) {
                RESTORE();
                return Z_NEED_DICT;
            }
            strm->adler = state->check = ADLER32_INITIAL_VALUE;
            state->mode = TYPE;

        case TYPE:
            if (flush == Z_BLOCK || flush == Z_TREES)
                goto inf_leave;

        case TYPEDO:
            /* determine and dispatch block type */
            INFLATE_TYPEDO_HOOK(strm, flush);  /* hook for IBM Z DFLTCC */
            if (state->last) {
                BYTEBITS();
                state->mode = CHECK;
                break;
            }
            NEEDBITS(3);
            state->last = BITS(1);
            DROPBITS(1);
            switch (BITS(2)) {
            case 0:                             /* stored block */
                Tracev((stderr, "inflate:     stored block%s\n", state->last ? " (last)" : ""));
                state->mode = STORED;
                break;
            case 1:                             /* fixed block */
                fixedtables(state);
                Tracev((stderr, "inflate:     fixed codes block%s\n", state->last ? " (last)" : ""));
                state->mode = LEN_;             /* decode codes */
                if (flush == Z_TREES) {
                    DROPBITS(2);
                    goto inf_leave;
                }
                break;
            case 2:                             /* dynamic block */
                Tracev((stderr, "inflate:     dynamic codes block%s\n", state->last ? " (last)" : ""));
                state->mode = TABLE;
                break;
            case 3:
                SET_BAD("invalid block type");
            }
            DROPBITS(2);
            break;

        case STORED:
            /* get and verify stored block length */
            BYTEBITS();                         /* go to byte boundary */
            NEEDBITS(32);
            if ((hold & 0xffff) != ((hold >> 16) ^ 0xffff)) {
                SET_BAD("invalid stored block lengths");
                break;
            }
            state->length = (uint16_t)hold;
            Tracev((stderr, "inflate:       stored length %u\n", state->length));
            INITBITS();
            state->mode = COPY_;
            if (flush == Z_TREES)
                goto inf_leave;

        case COPY_:
            state->mode = COPY;

        case COPY:
            /* copy stored block from input to output */
            copy = state->length;
            if (copy) {
                if (copy > have) copy = have;
                if (copy > left) copy = left;
                if (copy == 0) goto inf_leave;
                memcpy(put, next, copy);
                have -= copy;
                next += copy;
                left -= copy;
                put += copy;
                state->length -= copy;
                break;
            }
            Tracev((stderr, "inflate:       stored end\n"));
            state->mode = TYPE;
            break;

        case TABLE:
            /* get dynamic table entries descriptor */
            NEEDBITS(14);
            state->nlen = BITS(5) + 257;
            DROPBITS(5);
            state->ndist = BITS(5) + 1;
            DROPBITS(5);
            state->ncode = BITS(4) + 4;
            DROPBITS(4);
#ifndef PKZIP_BUG_WORKAROUND
            if (state->nlen > 286 || state->ndist > 30) {
                SET_BAD("too many length or distance symbols");
                break;
            }
#endif
            Tracev((stderr, "inflate:       table sizes ok\n"));
            state->have = 0;
            state->mode = LENLENS;

        case LENLENS:
            /* get code length code lengths (not a typo) */
            while (state->have < state->ncode) {
                NEEDBITS(3);
                state->lens[order[state->have++]] = (uint16_t)BITS(3);
                DROPBITS(3);
            }
            while (state->have < 19)
                state->lens[order[state->have++]] = 0;
            state->next = state->codes;
            state->lencode = (const code *)(state->next);
            state->lenbits = 7;
            ret = zng_inflate_table(CODES, state->lens, 19, &(state->next), &(state->lenbits), state->work);
            if (ret) {
                SET_BAD("invalid code lengths set");
                break;
            }
            Tracev((stderr, "inflate:       code lengths ok\n"));
            state->have = 0;
            state->mode = CODELENS;

        case CODELENS:
            /* get length and distance code code lengths */
            while (state->have < state->nlen + state->ndist) {
                for (;;) {
                    here = state->lencode[BITS(state->lenbits)];
                    if (here.bits <= bits) break;
                    PULLBYTE();
                }
                if (here.val < 16) {
                    DROPBITS(here.bits);
                    state->lens[state->have++] = here.val;
                } else {
                    if (here.val == 16) {
                        NEEDBITS(here.bits + 2);
                        DROPBITS(here.bits);
                        if (state->have == 0) {
                            SET_BAD("invalid bit length repeat");
                            break;
                        }
                        len = state->lens[state->have - 1];
                        copy = 3 + BITS(2);
                        DROPBITS(2);
                    } else if (here.val == 17) {
                        NEEDBITS(here.bits + 3);
                        DROPBITS(here.bits);
                        len = 0;
                        copy = 3 + BITS(3);
                        DROPBITS(3);
                    } else {
                        NEEDBITS(here.bits + 7);
                        DROPBITS(here.bits);
                        len = 0;
                        copy = 11 + BITS(7);
                        DROPBITS(7);
                    }
                    if (state->have + copy > state->nlen + state->ndist) {
                        SET_BAD("invalid bit length repeat");
                        break;
                    }
                    while (copy) {
                        --copy;
                        state->lens[state->have++] = (uint16_t)len;
                    }
                }
            }

            /* handle error breaks in while */
            if (state->mode == BAD)
                break;

            /* check for end-of-block code (better have one) */
            if (state->lens[256] == 0) {
                SET_BAD("invalid code -- missing end-of-block");
                break;
            }

            /* build code tables -- note: do not change the lenbits or distbits
               values here (9 and 6) without reading the comments in inftrees.h
               concerning the ENOUGH constants, which depend on those values */
            state->next = state->codes;
            state->lencode = (const code *)(state->next);
            state->lenbits = 9;
            ret = zng_inflate_table(LENS, state->lens, state->nlen, &(state->next), &(state->lenbits), state->work);
            if (ret) {
                SET_BAD("invalid literal/lengths set");
                break;
            }
            state->distcode = (const code *)(state->next);
            state->distbits = 6;
            ret = zng_inflate_table(DISTS, state->lens + state->nlen, state->ndist,
                            &(state->next), &(state->distbits), state->work);
            if (ret) {
                SET_BAD("invalid distances set");
                break;
            }
            Tracev((stderr, "inflate:       codes ok\n"));
            state->mode = LEN_;
            if (flush == Z_TREES)
                goto inf_leave;

        case LEN_:
            state->mode = LEN;

        case LEN:
            /* use inflate_fast() if we have enough input and output */
            if (have >= INFLATE_FAST_MIN_HAVE && left >= INFLATE_FAST_MIN_LEFT) {
                RESTORE();
                zng_inflate_fast(strm, out);
                LOAD();
                if (state->mode == TYPE)
                    state->back = -1;
                break;
            }
            state->back = 0;

            /* get a literal, length, or end-of-block code */
            for (;;) {
                here = state->lencode[BITS(state->lenbits)];
                if (here.bits <= bits)
                    break;
                PULLBYTE();
            }
            if (here.op && (here.op & 0xf0) == 0) {
                last = here;
                for (;;) {
                    here = state->lencode[last.val + (BITS(last.bits + last.op) >> last.bits)];
                    if ((unsigned)last.bits + (unsigned)here.bits <= bits)
                        break;
                    PULLBYTE();
                }
                DROPBITS(last.bits);
                state->back += last.bits;
            }
            DROPBITS(here.bits);
            state->back += here.bits;
            state->length = here.val;

            /* process literal */
            if ((int)(here.op) == 0) {
                Tracevv((stderr, here.val >= 0x20 && here.val < 0x7f ?
                        "inflate:         literal '%c'\n" :
                        "inflate:         literal 0x%02x\n", here.val));
                state->mode = LIT;
                break;
            }

            /* process end of block */
            if (here.op & 32) {
                Tracevv((stderr, "inflate:         end of block\n"));
                state->back = -1;
                state->mode = TYPE;
                break;
            }

            /* invalid code */
            if (here.op & 64) {
                SET_BAD("invalid literal/length code");
                break;
            }

            /* length code */
            state->extra = (here.op & 15);
            state->mode = LENEXT;

        case LENEXT:
            /* get extra bits, if any */
            if (state->extra) {
                NEEDBITS(state->extra);
                state->length += BITS(state->extra);
                DROPBITS(state->extra);
                state->back += state->extra;
            }
            Tracevv((stderr, "inflate:         length %u\n", state->length));
            state->was = state->length;
            state->mode = DIST;

        case DIST:
            /* get distance code */
            for (;;) {
                here = state->distcode[BITS(state->distbits)];
                if (here.bits <= bits)
                    break;
                PULLBYTE();
            }
            if ((here.op & 0xf0) == 0) {
                last = here;
                for (;;) {
                    here = state->distcode[last.val + (BITS(last.bits + last.op) >> last.bits)];
                    if ((unsigned)last.bits + (unsigned)here.bits <= bits)
                        break;
                    PULLBYTE();
                }
                DROPBITS(last.bits);
                state->back += last.bits;
            }
            DROPBITS(here.bits);
            state->back += here.bits;
            if (here.op & 64) {
                SET_BAD("invalid distance code");
                break;
            }
            state->offset = here.val;
            state->extra = (here.op & 15);
            state->mode = DISTEXT;

        case DISTEXT:
            /* get distance extra bits, if any */
            if (state->extra) {
                NEEDBITS(state->extra);
                state->offset += BITS(state->extra);
                DROPBITS(state->extra);
                state->back += state->extra;
            }
#ifdef INFLATE_STRICT
            if (state->offset > state->dmax) {
                SET_BAD("invalid distance too far back");
                break;
            }
#endif
            Tracevv((stderr, "inflate:         distance %u\n", state->offset));
            state->mode = MATCH;

        case MATCH:
            /* copy match from window to output */
            if (left == 0) goto inf_leave;
            copy = out - left;
            if (state->offset > copy) {         /* copy from window */
                copy = state->offset - copy;
                if (copy > state->whave) {
                    if (state->sane) {
                        SET_BAD("invalid distance too far back");
                        break;
                    }
#ifdef INFLATE_ALLOW_INVALID_DISTANCE_TOOFAR_ARRR
                    Trace((stderr, "inflate.c too far\n"));
                    copy -= state->whave;
                    if (copy > state->length)
                        copy = state->length;
                    if (copy > left)
                        copy = left;
                    left -= copy;
                    state->length -= copy;
                    do {
                        *put++ = 0;
                    } while (--copy);
                    if (state->length == 0)
                        state->mode = LEN;
                    break;
#endif
                }
                if (copy > state->wnext) {
                    copy -= state->wnext;
                    from = state->window + (state->wsize - copy);
                } else {
                    from = state->window + (state->wnext - copy);
                }
                if (copy > state->length)
                    copy = state->length;
                if (copy > left)
                    copy = left;

                put = functable.chunkcopy_safe(put, from, copy, put + left);
            } else {                             /* copy from output */
                copy = state->length;
                if (copy > left)
                    copy = left;

                put = functable.chunkmemset_safe(put, state->offset, copy, left);
            }
            left -= copy;
            state->length -= copy;
            if (state->length == 0)
                state->mode = LEN;
            break;

        case LIT:
            if (left == 0)
                goto inf_leave;
            *put++ = (unsigned char)(state->length);
            left--;
            state->mode = LEN;
            break;

        case CHECK:
            if (state->wrap) {
                NEEDBITS(32);
                out -= left;
                strm->total_out += out;
                state->total += out;
                if (INFLATE_NEED_CHECKSUM(strm) && (state->wrap & 4) && out)
                    strm->adler = state->check = UPDATE(state->check, put - out, out);
                out = left;
                if ((state->wrap & 4) && (
#ifdef GUNZIP
                     state->flags ? hold :
#endif
                     ZSWAP32(hold)) != state->check) {
                    SET_BAD("incorrect data check");
                    break;
                }
                INITBITS();
                Tracev((stderr, "inflate:   check matches trailer\n"));
            }
#ifdef GUNZIP
            state->mode = LENGTH;

        case LENGTH:
            if (state->wrap && state->flags) {
                NEEDBITS(32);
                if ((state->wrap & 4) && hold != (state->total & 0xffffffff)) {
                    SET_BAD("incorrect length check");
                    break;
                }
                INITBITS();
                Tracev((stderr, "inflate:   length matches trailer\n"));
            }
#endif
            state->mode = DONE;

        case DONE:
            /* inflate stream terminated properly */
            ret = Z_STREAM_END;
            goto inf_leave;

        case BAD:
            ret = Z_DATA_ERROR;
            goto inf_leave;

        case MEM:
            return Z_MEM_ERROR;

        case SYNC:

        default:                 /* can't happen, but makes compilers happy */
            return Z_STREAM_ERROR;
        }

    /*
       Return from inflate(), updating the total counts and the check value.
       If there was no progress during the inflate() call, return a buffer
       error.  Call updatewindow() to create and/or update the window state.
       Note: a memory error from inflate() is non-recoverable.
     */
  inf_leave:
    RESTORE();
    if (INFLATE_NEED_UPDATEWINDOW(strm) &&
            (state->wsize || (out != strm->avail_out && state->mode < BAD &&
                 (state->mode < CHECK || flush != Z_FINISH)))) {
        if (updatewindow(strm, strm->next_out, out - strm->avail_out)) {
            state->mode = MEM;
            return Z_MEM_ERROR;
        }
    }
    in -= strm->avail_in;
    out -= strm->avail_out;
    strm->total_in += in;
    strm->total_out += out;
    state->total += out;
    if (INFLATE_NEED_CHECKSUM(strm) && (state->wrap & 4) && out)
        strm->adler = state->check = UPDATE(state->check, strm->next_out - out, out);
    strm->data_type = (int)state->bits + (state->last ? 64 : 0) +
                      (state->mode == TYPE ? 128 : 0) + (state->mode == LEN_ || state->mode == COPY_ ? 256 : 0);
    if (((in == 0 && out == 0) || flush == Z_FINISH) && ret == Z_OK)
        ret = Z_BUF_ERROR;
    return ret;
}

int32_t Z_EXPORT PREFIX(inflateEnd)(PREFIX3(stream) *strm) {
    struct inflate_state *state;
    if (inflateStateCheck(strm))
        return Z_STREAM_ERROR;
    state = (struct inflate_state *)strm->state;
    if (state->window != NULL)
        ZFREE_WINDOW(strm, state->window);
    ZFREE_STATE(strm, strm->state);
    strm->state = NULL;
    Tracev((stderr, "inflate: end\n"));
    return Z_OK;
}

int32_t Z_EXPORT PREFIX(inflateGetDictionary)(PREFIX3(stream) *strm, uint8_t *dictionary, uint32_t *dictLength) {
    struct inflate_state *state;

    /* check state */
    if (inflateStateCheck(strm))
        return Z_STREAM_ERROR;
    state = (struct inflate_state *)strm->state;

    /* copy dictionary */
    if (state->whave && dictionary != NULL) {
        memcpy(dictionary, state->window + state->wnext, state->whave - state->wnext);
        memcpy(dictionary + state->whave - state->wnext, state->window, state->wnext);
    }
    if (dictLength != NULL)
        *dictLength = state->whave;
    return Z_OK;
}

int32_t Z_EXPORT PREFIX(inflateSetDictionary)(PREFIX3(stream) *strm, const uint8_t *dictionary, uint32_t dictLength) {
    struct inflate_state *state;
    unsigned long dictid;
    int32_t ret;

    /* check state */
    if (inflateStateCheck(strm))
        return Z_STREAM_ERROR;
    state = (struct inflate_state *)strm->state;
    if (state->wrap != 0 && state->mode != DICT)
        return Z_STREAM_ERROR;

    /* check for correct dictionary identifier */
    if (state->mode == DICT) {
        dictid = functable.adler32(ADLER32_INITIAL_VALUE, dictionary, dictLength);
        if (dictid != state->check)
            return Z_DATA_ERROR;
    }

    /* copy dictionary to window using updatewindow(), which will amend the
       existing dictionary if appropriate */
    ret = updatewindow(strm, dictionary + dictLength, dictLength);
    if (ret) {
        state->mode = MEM;
        return Z_MEM_ERROR;
    }
    state->havedict = 1;
    Tracev((stderr, "inflate:   dictionary set\n"));
    return Z_OK;
}

int32_t Z_EXPORT PREFIX(inflateGetHeader)(PREFIX3(stream) *strm, PREFIX(gz_headerp) head) {
    struct inflate_state *state;

    /* check state */
    if (inflateStateCheck(strm))
        return Z_STREAM_ERROR;
    state = (struct inflate_state *)strm->state;
    if ((state->wrap & 2) == 0)
        return Z_STREAM_ERROR;

    /* save header structure */
    state->head = head;
    head->done = 0;
    return Z_OK;
}

/*
   Search buf[0..len-1] for the pattern: 0, 0, 0xff, 0xff.  Return when found
   or when out of input.  When called, *have is the number of pattern bytes
   found in order so far, in 0..3.  On return *have is updated to the new
   state.  If on return *have equals four, then the pattern was found and the
   return value is how many bytes were read including the last byte of the
   pattern.  If *have is less than four, then the pattern has not been found
   yet and the return value is len.  In the latter case, syncsearch() can be
   called again with more data and the *have state.  *have is initialized to
   zero for the first call.
 */
static uint32_t syncsearch(uint32_t *have, const uint8_t *buf, uint32_t len) {
    uint32_t got, next;

    got = *have;
    next = 0;
    while (next < len && got < 4) {
        if ((int)(buf[next]) == (got < 2 ? 0 : 0xff))
            got++;
        else if (buf[next])
            got = 0;
        else
            got = 4 - got;
        next++;
    }
    *have = got;
    return next;
}

int32_t Z_EXPORT PREFIX(inflateSync)(PREFIX3(stream) *strm) {
    unsigned len;               /* number of bytes to look at or looked at */
    int flags;                  /* temporary to save header status */
    size_t in, out;             /* temporary to save total_in and total_out */
    unsigned char buf[4];       /* to restore bit buffer to byte string */
    struct inflate_state *state;

    /* check parameters */
    if (inflateStateCheck(strm))
        return Z_STREAM_ERROR;
    state = (struct inflate_state *)strm->state;
    if (strm->avail_in == 0 && state->bits < 8)
        return Z_BUF_ERROR;

    /* if first time, start search in bit buffer */
    if (state->mode != SYNC) {
        state->mode = SYNC;
        state->hold <<= state->bits & 7;
        state->bits -= state->bits & 7;
        len = 0;
        while (state->bits >= 8) {
            buf[len++] = (unsigned char)(state->hold);
            state->hold >>= 8;
            state->bits -= 8;
        }
        state->have = 0;
        syncsearch(&(state->have), buf, len);
    }

    /* search available input */
    len = syncsearch(&(state->have), strm->next_in, strm->avail_in);
    strm->avail_in -= len;
    strm->next_in += len;
    strm->total_in += len;

    /* return no joy or set up to restart inflate() on a new block */
    if (state->have != 4)
        return Z_DATA_ERROR;
    if (state->flags == -1)
        state->wrap = 0;    /* if no header yet, treat as raw */
    else
        state->wrap &= ~4;  /* no point in computing a check value now */
    flags = state->flags;
    in = strm->total_in;
    out = strm->total_out;
    PREFIX(inflateReset)(strm);
    strm->total_in = in;
    strm->total_out = out;
    state->flags = flags;
    state->mode = TYPE;
    return Z_OK;
}

/*
   Returns true if inflate is currently at the end of a block generated by
   Z_SYNC_FLUSH or Z_FULL_FLUSH. This function is used by one PPP
   implementation to provide an additional safety check. PPP uses
   Z_SYNC_FLUSH but removes the length bytes of the resulting empty stored
   block. When decompressing, PPP checks that at the end of input packet,
   inflate is waiting for these length bytes.
 */
int32_t Z_EXPORT PREFIX(inflateSyncPoint)(PREFIX3(stream) *strm) {
    struct inflate_state *state;

    if (inflateStateCheck(strm))
        return Z_STREAM_ERROR;
    INFLATE_SYNC_POINT_HOOK(strm);
    state = (struct inflate_state *)strm->state;
    return state->mode == STORED && state->bits == 0;
}

int32_t Z_EXPORT PREFIX(inflateCopy)(PREFIX3(stream) *dest, PREFIX3(stream) *source) {
    struct inflate_state *state;
    struct inflate_state *copy;
    unsigned char *window;
    unsigned wsize;

    /* check input */
    if (inflateStateCheck(source) || dest == NULL)
        return Z_STREAM_ERROR;
    state = (struct inflate_state *)source->state;

    /* allocate space */
    copy = (struct inflate_state *)ZALLOC_STATE(source, 1, sizeof(struct inflate_state));
    if (copy == NULL)
        return Z_MEM_ERROR;
    window = NULL;
    if (state->window != NULL) {
        window = (unsigned char *)ZALLOC_WINDOW(source, 1U << state->wbits, sizeof(unsigned char));
        if (window == NULL) {
            ZFREE_STATE(source, copy);
            return Z_MEM_ERROR;
        }
    }

    /* copy state */
    memcpy((void *)dest, (void *)source, sizeof(PREFIX3(stream)));
    ZCOPY_STATE((void *)copy, (void *)state, sizeof(struct inflate_state));
    copy->strm = dest;
    if (state->lencode >= state->codes && state->lencode <= state->codes + ENOUGH - 1) {
        copy->lencode = copy->codes + (state->lencode - state->codes);
        copy->distcode = copy->codes + (state->distcode - state->codes);
    }
    copy->next = copy->codes + (state->next - state->codes);
    if (window != NULL) {
        wsize = 1U << state->wbits;
        memcpy(window, state->window, wsize);
    }
    copy->window = window;
    dest->state = (struct internal_state *)copy;
    return Z_OK;
}

int32_t Z_EXPORT PREFIX(inflateUndermine)(PREFIX3(stream) *strm, int32_t subvert) {
    struct inflate_state *state;

    if (inflateStateCheck(strm))
        return Z_STREAM_ERROR;
    state = (struct inflate_state *)strm->state;
#ifdef INFLATE_ALLOW_INVALID_DISTANCE_TOOFAR_ARRR
    state->sane = !subvert;
    return Z_OK;
#else
    Z_UNUSED(subvert);
    state->sane = 1;
    return Z_DATA_ERROR;
#endif
}

int32_t Z_EXPORT PREFIX(inflateValidate)(PREFIX3(stream) *strm, int32_t check) {
    struct inflate_state *state;

    if (inflateStateCheck(strm))
        return Z_STREAM_ERROR;
    state = (struct inflate_state *)strm->state;
    if (check && state->wrap)
        state->wrap |= 4;
    else
        state->wrap &= ~4;
    return Z_OK;
}

long Z_EXPORT PREFIX(inflateMark)(PREFIX3(stream) *strm) {
    struct inflate_state *state;

    if (inflateStateCheck(strm))
        return -65536;
    INFLATE_MARK_HOOK(strm);  /* hook for IBM Z DFLTCC */
    state = (struct inflate_state *)strm->state;
    return (long)(((unsigned long)((long)state->back)) << 16) +
        (state->mode == COPY ? state->length :
            (state->mode == MATCH ? state->was - state->length : 0));
}

unsigned long Z_EXPORT PREFIX(inflateCodesUsed)(PREFIX3(stream) *strm) {
    struct inflate_state *state;
    if (strm == NULL || strm->state == NULL)
        return (unsigned long)-1;
    state = (struct inflate_state *)strm->state;
    return (unsigned long)(state->next - state->codes);
}
