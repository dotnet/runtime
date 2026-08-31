/* makecrct.c -- output crc32 tables
 * Copyright (C) 1995-2022 Mark Adler
 * For conditions of distribution and use, see copyright notice in zlib.h
*/

#include <stdio.h>
#include <inttypes.h>
#include "zbuild.h"
#include "zutil.h"

/*
    The crc32 table header file contains tables for both 32-bit and 64-bit
    z_word_t's, and so requires a 64-bit type be available. In that case,
    z_word_t must be defined to be 64-bits. This code then also generates
    and writes out the tables for the case that z_word_t is 32 bits.
*/

#define W 8 /* Need a 64-bit integer type in order to generate crc32 tables. */

#include "crc32_braid_p.h"

static uint32_t crc_table[256];
static z_word_t crc_big_table[256];
static uint32_t x2n_table[32];

#include "crc32_braid_comb_p.h"

static void make_crc_table(void);
static void print_crc_table(void);

static void braid(uint32_t ltl[][256], z_word_t big[][256], int n, int w);

static void write_table(const uint32_t *table, int k);
static void write_table32hi(const z_word_t *table, int k);
static void write_table64(const z_word_t *table, int k);

/* ========================================================================= */
/*
  Generate tables for a byte-wise 32-bit CRC calculation on the polynomial:
  x^32+x^26+x^23+x^22+x^16+x^12+x^11+x^10+x^8+x^7+x^5+x^4+x^2+x+1.

  Polynomials over GF(2) are represented in binary, one bit per coefficient,
  with the lowest powers in the most significant bit. Then adding polynomials
  is just exclusive-or, and multiplying a polynomial by x is a right shift by
  one. If we call the above polynomial p, and represent a byte as the
  polynomial q, also with the lowest power in the most significant bit (so the
  byte 0xb1 is the polynomial x^7+x^3+x^2+1), then the CRC is (q*x^32) mod p,
  where a mod b means the remainder after dividing a by b.

  This calculation is done using the shift-register method of multiplying and
  taking the remainder. The register is initialized to zero, and for each
  incoming bit, x^32 is added mod p to the register if the bit is a one (where
  x^32 mod p is p+x^32 = x^26+...+1), and the register is multiplied mod p by x
  (which is shifting right by one and adding x^32 mod p if the bit shifted out
  is a one). We start with the highest power (least significant bit) of q and
  repeat for all eight bits of q.

  The table is simply the CRC of all possible eight bit values. This is all the
  information needed to generate CRCs on data a byte at a time for all
  combinations of CRC register values and incoming bytes.
*/
static void make_crc_table(void) {
    unsigned i, j, n;
    uint32_t p;

    /* initialize the CRC of bytes tables */
    for (i = 0; i < 256; i++) {
        p = i;
        for (j = 0; j < 8; j++)
            p = p & 1 ? (p >> 1) ^ POLY : p >> 1;
        crc_table[i] = p;
        crc_big_table[i] = ZSWAP64(p);
    }

    /* initialize the x^2^n mod p(x) table */
    p = (uint32_t)1 << 30;         /* x^1 */
    x2n_table[0] = p;
    for (n = 1; n < 32; n++)
        x2n_table[n] = p = multmodp(p, p);
}

/*
  Generate the little and big-endian braid tables for the given n and z_word_t
  size w. Each array must have room for w blocks of 256 elements.
 */
static void braid(uint32_t ltl[][256], z_word_t big[][256], int n, int w) {
    int k;
    uint32_t i, p, q;
    for (k = 0; k < w; k++) {
        p = x2nmodp(((z_off64_t)n * w + 3 - k) << 3, 0);
        ltl[k][0] = 0;
        big[w - 1 - k][0] = 0;
        for (i = 1; i < 256; i++) {
            ltl[k][i] = q = multmodp(i << 24, p);
            big[w - 1 - k][i] = ZSWAP64(q);
        }
    }
}

/*
   Write the 32-bit values in table[0..k-1] to out, five per line in
   hexadecimal separated by commas.
 */
static void write_table(const uint32_t *table, int k) {
    int n;

    for (n = 0; n < k; n++)
        printf("%s0x%08" PRIx32 "%s", n == 0 || n % 5 ? "" : "    ",
                (uint32_t)(table[n]),
                n == k - 1 ? "" : (n % 5 == 4 ? ",\n" : ", "));
}

/*
   Write the high 32-bits of each value in table[0..k-1] to out, five per line
   in hexadecimal separated by commas.
 */
static void write_table32hi(const z_word_t *table, int k) {
    int n;

    for (n = 0; n < k; n++)
        printf("%s0x%08" PRIx32 "%s", n == 0 || n % 5 ? "" : "    ",
                (uint32_t)(table[n] >> 32),
                n == k - 1 ? "" : (n % 5 == 4 ? ",\n" : ", "));
}

/*
  Write the 64-bit values in table[0..k-1] to out, three per line in
  hexadecimal separated by commas. This assumes that if there is a 64-bit
  type, then there is also a long long integer type, and it is at least 64
  bits. If not, then the type cast and format string can be adjusted
  accordingly.
 */
static void write_table64(const z_word_t *table, int k) {
    int n;

    for (n = 0; n < k; n++)
        printf("%s0x%016" PRIx64 "%s", n == 0 || n % 3 ? "" : "    ",
                (uint64_t)(table[n]),
                n == k - 1 ? "" : (n % 3 == 2 ? ",\n" : ", "));
}

static void print_crc_table(void) {
    int k, n;
    uint32_t ltl[8][256];
    z_word_t big[8][256];

    printf("#ifndef CRC32_BRAID_TBL_H_\n");
    printf("#define CRC32_BRAID_TBL_H_\n\n");
    printf("/* crc32_braid_tbl.h -- tables for braided CRC calculation\n");
    printf(" * Generated automatically by makecrct.c\n */\n\n");

    /* print little-endian CRC table */
    printf("static const uint32_t crc_table[] = {\n");
    printf("    ");
    write_table(crc_table, 256);
    printf("};\n\n");

    /* print big-endian CRC table for 64-bit z_word_t */
    printf("#ifdef W\n\n");
    printf("#if W == 8\n\n");
    printf("static const z_word_t crc_big_table[] = {\n");
    printf("    ");
    write_table64(crc_big_table, 256);
    printf("};\n\n");

    /* print big-endian CRC table for 32-bit z_word_t */
    printf("#else /* W == 4 */\n\n");
    printf("static const z_word_t crc_big_table[] = {\n");
    printf("    ");
    write_table32hi(crc_big_table, 256);
    printf("};\n\n");
    printf("#endif\n\n");
    printf("#endif /* W */\n\n");

    /* write out braid tables for each value of N */
    for (n = 1; n <= 6; n++) {
        printf("#if N == %d\n", n);

        /* compute braid tables for this N and 64-bit word_t */
        braid(ltl, big, n, 8);

        /* write out braid tables for 64-bit z_word_t */
        printf("\n");
        printf("#if W == 8\n\n");
        printf("static const uint32_t crc_braid_table[][256] = {\n");
        for (k = 0; k < 8; k++) {
            printf("   {");
            write_table(ltl[k], 256);
            printf("}%s", k < 7 ? ",\n" : "");
        }
        printf("};\n\n");
        printf("static const z_word_t crc_braid_big_table[][256] = {\n");
        for (k = 0; k < 8; k++) {
            printf("   {");
            write_table64(big[k], 256);
            printf("}%s", k < 7 ? ",\n" : "");
        }
        printf("};\n");

        /* compute braid tables for this N and 32-bit word_t */
        braid(ltl, big, n, 4);

        /* write out braid tables for 32-bit z_word_t */
        printf("\n");
        printf("#else /* W == 4 */\n\n");
        printf("static const uint32_t crc_braid_table[][256] = {\n");
        for (k = 0; k < 4; k++) {
            printf("   {");
            write_table(ltl[k], 256);
            printf("}%s", k < 3 ? ",\n" : "");
        }
        printf("};\n\n");
        printf("static const z_word_t crc_braid_big_table[][256] = {\n");
        for (k = 0; k < 4; k++) {
            printf("   {");
            write_table32hi(big[k], 256);
            printf("}%s", k < 3 ? ",\n" : "");
        }
        printf("};\n\n");
        printf("#endif /* W */\n\n");

        printf("#endif /* N == %d */\n", n);
    }
    printf("\n");

    /* write out zeros operator table */
    printf("static const uint32_t x2n_table[] = {\n");
    printf("    ");
    write_table(x2n_table, 32);
    printf("};\n");

    printf("\n");
    printf("#endif /* CRC32_BRAID_TBL_H_ */\n");
}

// The output of this application can be piped out to recreate crc32 tables
int main(int argc, char *argv[]) {
    Z_UNUSED(argc);
    Z_UNUSED(argv);

    make_crc_table();
    print_crc_table();
    return 0;
}
