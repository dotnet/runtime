/* crc32.c -- compute the CRC-32 of a data stream
 * Copyright (C) 1995-2006, 2010, 2011, 2012, 2016, 2018 Mark Adler
 * For conditions of distribution and use, see copyright notice in zlib.h
 *
 * Thanks to Rodney Brown <rbrown64@csc.com.au> for his contribution of faster
 * CRC methods: exclusive-oring 32 bits of data at a time, and pre-computing
 * tables for updating the shift register in one step with three exclusive-ors
 * instead of four steps with four exclusive-ors.  This results in about a
 * factor of two increase in speed on a Power PC G4 (PPC7455) using gcc -O3.
 */

#include "zbuild.h"
#include "zendian.h"
#include <inttypes.h>
#include "deflate.h"
#include "functable.h"
#include "crc32_tbl.h"

/* =========================================================================
 * This function can be used by asm versions of crc32()
 */
const uint32_t * Z_EXPORT PREFIX(get_crc_table)(void) {
    return (const uint32_t *)crc_table;
}

#ifdef ZLIB_COMPAT
unsigned long Z_EXPORT PREFIX(crc32_z)(unsigned long crc, const unsigned char *buf, size_t len) {
    if (buf == NULL) return 0;

    return (unsigned long)functable.crc32((uint32_t)crc, buf, len);
}
#else
uint32_t Z_EXPORT PREFIX(crc32_z)(uint32_t crc, const unsigned char *buf, size_t len) {
    if (buf == NULL) return 0;

    return functable.crc32(crc, buf, len);
}
#endif
/* ========================================================================= */
#define DO1 crc = crc_table[0][((int)crc ^ (*buf++)) & 0xff] ^ (crc >> 8)
#define DO8 DO1; DO1; DO1; DO1; DO1; DO1; DO1; DO1
#define DO4 DO1; DO1; DO1; DO1

/* ========================================================================= */
Z_INTERNAL uint32_t crc32_generic(uint32_t crc, const unsigned char *buf, uint64_t len) {
    crc = crc ^ 0xffffffff;

#ifdef UNROLL_MORE
    while (len >= 8) {
        DO8;
        len -= 8;
    }
#else
    while (len >= 4) {
        DO4;
        len -= 4;
    }
#endif

    if (len) do {
        DO1;
    } while (--len);
    return crc ^ 0xffffffff;
}

#ifdef ZLIB_COMPAT
unsigned long Z_EXPORT PREFIX(crc32)(unsigned long crc, const unsigned char *buf, unsigned int len) {
    return (unsigned long)PREFIX(crc32_z)((uint32_t)crc, buf, len);
}
#else
uint32_t Z_EXPORT PREFIX(crc32)(uint32_t crc, const unsigned char *buf, uint32_t len) {
    return PREFIX(crc32_z)(crc, buf, len);
}
#endif

/*
   This BYFOUR code accesses the passed unsigned char * buffer with a 32-bit
   integer pointer type. This violates the strict aliasing rule, where a
   compiler can assume, for optimization purposes, that two pointers to
   fundamentally different types won't ever point to the same memory. This can
   manifest as a problem only if one of the pointers is written to. This code
   only reads from those pointers. So long as this code remains isolated in
   this compilation unit, there won't be a problem. For this reason, this code
   should not be copied and pasted into a compilation unit in which other code
   writes to the buffer that is passed to these routines.
 */

/* ========================================================================= */
#if BYTE_ORDER == LITTLE_ENDIAN
#define DOLIT4 c ^= *buf4++; \
        c = crc_table[3][c & 0xff] ^ crc_table[2][(c >> 8) & 0xff] ^ \
            crc_table[1][(c >> 16) & 0xff] ^ crc_table[0][c >> 24]
#define DOLIT32 DOLIT4; DOLIT4; DOLIT4; DOLIT4; DOLIT4; DOLIT4; DOLIT4; DOLIT4

/* ========================================================================= */
Z_INTERNAL uint32_t crc32_little(uint32_t crc, const unsigned char *buf, uint64_t len) {
    Z_REGISTER uint32_t c;
    Z_REGISTER const uint32_t *buf4;

    c = crc;
    c = ~c;
    while (len && ((ptrdiff_t)buf & 3)) {
        c = crc_table[0][(c ^ *buf++) & 0xff] ^ (c >> 8);
        len--;
    }

    buf4 = (const uint32_t *)(const void *)buf;

#ifdef UNROLL_MORE
    while (len >= 32) {
        DOLIT32;
        len -= 32;
    }
#endif

    while (len >= 4) {
        DOLIT4;
        len -= 4;
    }
    buf = (const unsigned char *)buf4;

    if (len) do {
        c = crc_table[0][(c ^ *buf++) & 0xff] ^ (c >> 8);
    } while (--len);
    c = ~c;
    return c;
}
#endif /* BYTE_ORDER == LITTLE_ENDIAN */

/* ========================================================================= */
#if BYTE_ORDER == BIG_ENDIAN
#define DOBIG4 c ^= *buf4++; \
        c = crc_table[4][c & 0xff] ^ crc_table[5][(c >> 8) & 0xff] ^ \
            crc_table[6][(c >> 16) & 0xff] ^ crc_table[7][c >> 24]
#define DOBIG32 DOBIG4; DOBIG4; DOBIG4; DOBIG4; DOBIG4; DOBIG4; DOBIG4; DOBIG4

/* ========================================================================= */
Z_INTERNAL uint32_t crc32_big(uint32_t crc, const unsigned char *buf, uint64_t len) {
    Z_REGISTER uint32_t c;
    Z_REGISTER const uint32_t *buf4;

    c = ZSWAP32(crc);
    c = ~c;
    while (len && ((ptrdiff_t)buf & 3)) {
        c = crc_table[4][(c >> 24) ^ *buf++] ^ (c << 8);
        len--;
    }

    buf4 = (const uint32_t *)(const void *)buf;

#ifdef UNROLL_MORE
    while (len >= 32) {
        DOBIG32;
        len -= 32;
    }
#endif

    while (len >= 4) {
        DOBIG4;
        len -= 4;
    }
    buf = (const unsigned char *)buf4;

    if (len) do {
        c = crc_table[4][(c >> 24) ^ *buf++] ^ (c << 8);
    } while (--len);
    c = ~c;
    return ZSWAP32(c);
}
#endif /* BYTE_ORDER == BIG_ENDIAN */

#ifdef X86_PCLMULQDQ_CRC
#include "arch/x86/x86.h"
#include "arch/x86/crc_folding.h"

Z_INTERNAL void crc_finalize(deflate_state *const s) {
    if (x86_cpu_has_pclmulqdq)
        s->strm->adler = crc_fold_512to32(s);
}
#endif

Z_INTERNAL void crc_reset(deflate_state *const s) {
#ifdef X86_PCLMULQDQ_CRC
    x86_check_features();
    if (x86_cpu_has_pclmulqdq) {
        crc_fold_init(s);
        return;
    }
#endif
    s->strm->adler = PREFIX(crc32)(0L, NULL, 0);
}

Z_INTERNAL void copy_with_crc(PREFIX3(stream) *strm, unsigned char *dst, unsigned long size) {
#ifdef X86_PCLMULQDQ_CRC
    if (x86_cpu_has_pclmulqdq) {
        crc_fold_copy(strm->state, dst, strm->next_in, size);
        return;
    }
#endif
    memcpy(dst, strm->next_in, size);
    strm->adler = PREFIX(crc32)(strm->adler, dst, size);
}
