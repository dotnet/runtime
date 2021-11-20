/* crc32_comb.c -- compute the CRC-32 of a data stream
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
#include <inttypes.h>
#include "deflate.h"
#include "crc32_p.h"
#include "crc32_comb_tbl.h"


/* Local functions for crc concatenation */
static uint32_t crc32_combine_(uint32_t crc1, uint32_t crc2, z_off64_t len2);
static void crc32_combine_gen_(uint32_t *op, z_off64_t len2);

/* ========================================================================= */
static uint32_t crc32_combine_(uint32_t crc1, uint32_t crc2, z_off64_t len2) {
    int n;

    if (len2 > 0)
        /* operator for 2^n zeros repeats every GF2_DIM n values */
        for (n = 0; len2; n = (n + 1) % GF2_DIM, len2 >>= 1)
            if (len2 & 1)
                crc1 = gf2_matrix_times(crc_comb[n], crc1);
    return crc1 ^ crc2;
}

/* ========================================================================= */
#ifdef ZLIB_COMPAT
unsigned long Z_EXPORT PREFIX(crc32_combine)(unsigned long crc1, unsigned long crc2, z_off_t len2) {
    return (unsigned long)crc32_combine_((uint32_t)crc1, (uint32_t)crc2, len2);
}

unsigned long Z_EXPORT PREFIX4(crc32_combine)(unsigned long crc1, unsigned long crc2, z_off64_t len2) {
    return (unsigned long)crc32_combine_((uint32_t)crc1, (uint32_t)crc2, len2);
}
#else
uint32_t Z_EXPORT PREFIX4(crc32_combine)(uint32_t crc1, uint32_t crc2, z_off64_t len2) {
    return crc32_combine_(crc1, crc2, len2);
}
#endif

/* ========================================================================= */

static void crc32_combine_gen_(uint32_t *op, z_off64_t len2) {
    uint32_t row;
    int j;
    unsigned i;

    /* if len2 is zero or negative, return the identity matrix */
    if (len2 <= 0) {
        row = 1;
        for (j = 0; j < GF2_DIM; j++) {
            op[j] = row;
            row <<= 1;
        }
        return;
    }

    /* at least one bit in len2 is set -- find it, and copy the operator
       corresponding to that position into op */
    i = 0;
    for (;;) {
        if (len2 & 1) {
            for (j = 0; j < GF2_DIM; j++)
                op[j] = crc_comb[i][j];
            break;
        }
        len2 >>= 1;
        i = (i + 1) % GF2_DIM;
    }

    /* for each remaining bit set in len2 (if any), multiply op by the operator
       corresponding to that position */
    for (;;) {
        len2 >>= 1;
        i = (i + 1) % GF2_DIM;
        if (len2 == 0)
            break;
        if (len2 & 1)
            for (j = 0; j < GF2_DIM; j++)
                op[j] = gf2_matrix_times(crc_comb[i], op[j]);
    }
}

/* ========================================================================= */

#ifdef ZLIB_COMPAT
void Z_EXPORT PREFIX(crc32_combine_gen)(uint32_t *op, z_off_t len2) {
    crc32_combine_gen_(op, len2);
}
#endif

void Z_EXPORT PREFIX4(crc32_combine_gen)(uint32_t *op, z_off64_t len2) {
    crc32_combine_gen_(op, len2);
}

/* ========================================================================= */
uint32_t Z_EXPORT PREFIX(crc32_combine_op)(uint32_t crc1, uint32_t crc2, const uint32_t *op) {
    return gf2_matrix_times(op, crc1) ^ crc2;
}
