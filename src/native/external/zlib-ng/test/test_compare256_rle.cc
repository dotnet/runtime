/* test_compare256_rle.cc -- compare256_rle unit tests
 * Copyright (C) 2022 Nathan Moinvaziri
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#include <stdio.h>
#include <string.h>
#include <stdlib.h>

extern "C" {
#  include "zbuild.h"
#  include "zutil.h"
#  include "compare256_rle.h"
}

#include <gtest/gtest.h>

#define MAX_COMPARE_SIZE (256)

/* Ensure that compare256_rle returns the correct match length */
static inline void compare256_rle_match_check(compare256_rle_func compare256_rle) {
    int32_t match_len, i;
    uint8_t str1[] = {'a', 'a', 0};
    uint8_t *str2;

    str2 = (uint8_t *)PREFIX(zcalloc)(NULL, 1, MAX_COMPARE_SIZE);
    ASSERT_TRUE(str2 != NULL);
    memset(str2, 'a', MAX_COMPARE_SIZE);

    for (i = 0; i <= MAX_COMPARE_SIZE; i++) {
        if (i < MAX_COMPARE_SIZE)
            str2[i] = 0;

        match_len = compare256_rle(str1, str2);
        EXPECT_EQ(match_len, i);

        if (i < MAX_COMPARE_SIZE)
            str2[i] = 'a';
    }

    PREFIX(zcfree)(NULL, str2);
}

#define TEST_COMPARE256_RLE(name, func, support_flag) \
    TEST(compare256_rle, name) { \
        if (!support_flag) { \
            GTEST_SKIP(); \
            return; \
        } \
        compare256_rle_match_check(func); \
    }

TEST_COMPARE256_RLE(c, compare256_rle_c, 1)

#ifdef UNALIGNED_OK
TEST_COMPARE256_RLE(unaligned_16, compare256_rle_unaligned_16, 1)
#ifdef HAVE_BUILTIN_CTZ
TEST_COMPARE256_RLE(unaligned_32, compare256_rle_unaligned_32, 1)
#endif
#if defined(UNALIGNED64_OK) && defined(HAVE_BUILTIN_CTZLL)
TEST_COMPARE256_RLE(unaligned_64, compare256_rle_unaligned_64, 1)
#endif
#endif
