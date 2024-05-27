/* maketrees.c -- output static huffman trees
 * Copyright (C) 1995-2017 Jean-loup Gailly
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#include <stdio.h>
#include "zbuild.h"
#include "deflate.h"
#include "trees.h"

static ct_data static_ltree[L_CODES+2];
/* The static literal tree. Since the bit lengths are imposed, there is no
 * need for the L_CODES extra codes used during heap construction. However
 * The codes 286 and 287 are needed to build a canonical tree (see zng_tr_init).
 */

static ct_data static_dtree[D_CODES];
/* The static distance tree. (Actually a trivial tree since all codes use 5 bits.)
 */

static unsigned char dist_code[DIST_CODE_LEN];
/* Distance codes. The first 256 values correspond to the distances 3 .. 258,
 * the last 256 values correspond to the top 8 bits of the 15 bit distances.
 */

static unsigned char length_code[STD_MAX_MATCH-STD_MIN_MATCH+1];
/* length code for each normalized match length (0 == STD_MIN_MATCH) */

static int base_length[LENGTH_CODES];
/* First normalized length for each code (0 = STD_MIN_MATCH) */

static int base_dist[D_CODES];
/* First normalized distance for each code (0 = distance of 1) */


static void tr_static_init(void) {
    int n;        /* iterates over tree elements */
    int bits;     /* bit counter */
    int length;   /* length value */
    int code;     /* code value */
    int dist;     /* distance index */
    uint16_t bl_count[MAX_BITS+1];
    /* number of codes at each bit length for an optimal tree */

    /* Initialize the mapping length (0..255) -> length code (0..28) */
    length = 0;
    for (code = 0; code < LENGTH_CODES-1; code++) {
        base_length[code] = length;
        for (n = 0; n < (1 << extra_lbits[code]); n++) {
            length_code[length++] = (unsigned char)code;
        }
    }
    Assert(length == 256, "tr_static_init: length != 256");
    /* Note that the length 255 (match length 258) can be represented in two different
     * ways: code 284 + 5 bits or code 285, so we overwrite length_code[255] to use the best encoding:
     */
    length_code[length-1] = (unsigned char)code;

    /* Initialize the mapping dist (0..32K) -> dist code (0..29) */
    dist = 0;
    for (code = 0; code < 16; code++) {
        base_dist[code] = dist;
        for (n = 0; n < (1 << extra_dbits[code]); n++) {
            dist_code[dist++] = (unsigned char)code;
        }
    }
    Assert(dist == 256, "tr_static_init: dist != 256");
    dist >>= 7; /* from now on, all distances are divided by 128 */
    for ( ; code < D_CODES; code++) {
        base_dist[code] = dist << 7;
        for (n = 0; n < (1 << (extra_dbits[code]-7)); n++) {
            dist_code[256 + dist++] = (unsigned char)code;
        }
    }
    Assert(dist == 256, "tr_static_init: 256+dist != 512");

    /* Construct the codes of the static literal tree */
    for (bits = 0; bits <= MAX_BITS; bits++)
        bl_count[bits] = 0;
    n = 0;
    while (n <= 143) static_ltree[n++].Len = 8, bl_count[8]++;
    while (n <= 255) static_ltree[n++].Len = 9, bl_count[9]++;
    while (n <= 279) static_ltree[n++].Len = 7, bl_count[7]++;
    while (n <= 287) static_ltree[n++].Len = 8, bl_count[8]++;
    /* Codes 286 and 287 do not exist, but we must include them in the tree construction
     * to get a canonical Huffman tree (longest code all ones)
     */
    gen_codes((ct_data *)static_ltree, L_CODES+1, bl_count);

    /* The static distance tree is trivial: */
    for (n = 0; n < D_CODES; n++) {
        static_dtree[n].Len = 5;
        static_dtree[n].Code = PREFIX(bi_reverse)((unsigned)n, 5);
    }
}

#  define SEPARATOR(i, last, width) \
      ((i) == (last)? "\n};\n\n" :    \
       ((i) % (width) == (width)-1 ? ",\n" : ", "))

static void gen_trees_header(void) {
    int i;

    printf("#ifndef TREES_TBL_H_\n");
    printf("#define TREES_TBL_H_\n\n");

    printf("/* header created automatically with maketrees.c */\n\n");

    printf("Z_INTERNAL const ct_data static_ltree[L_CODES+2] = {\n");
    for (i = 0; i < L_CODES+2; i++) {
        printf("{{%3u},{%u}}%s", static_ltree[i].Code, static_ltree[i].Len, SEPARATOR(i, L_CODES+1, 5));
    }

    printf("Z_INTERNAL const ct_data static_dtree[D_CODES] = {\n");
    for (i = 0; i < D_CODES; i++) {
        printf("{{%2u},{%u}}%s", static_dtree[i].Code, static_dtree[i].Len, SEPARATOR(i, D_CODES-1, 5));
    }

    printf("const unsigned char Z_INTERNAL zng_dist_code[DIST_CODE_LEN] = {\n");
    for (i = 0; i < DIST_CODE_LEN; i++) {
        printf("%2u%s", dist_code[i], SEPARATOR(i, DIST_CODE_LEN-1, 20));
    }

    printf("const unsigned char Z_INTERNAL zng_length_code[STD_MAX_MATCH-STD_MIN_MATCH+1] = {\n");
    for (i = 0; i < STD_MAX_MATCH-STD_MIN_MATCH+1; i++) {
        printf("%2u%s", length_code[i], SEPARATOR(i, STD_MAX_MATCH-STD_MIN_MATCH, 20));
    }

    printf("Z_INTERNAL const int base_length[LENGTH_CODES] = {\n");
    for (i = 0; i < LENGTH_CODES; i++) {
        printf("%d%s", base_length[i], SEPARATOR(i, LENGTH_CODES-1, 20));
    }

    printf("Z_INTERNAL const int base_dist[D_CODES] = {\n");
    for (i = 0; i < D_CODES; i++) {
        printf("%5d%s", base_dist[i], SEPARATOR(i, D_CODES-1, 10));
    }

    printf("#endif /* TREES_TBL_H_ */\n");
}

// The output of this application can be piped out to recreate trees.h
int main(void) {
    tr_static_init();
    gen_trees_header();
    return 0;
}
