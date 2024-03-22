/* test_compress_bound.cc - Test compressBound() with small buffers */

#include "zbuild.h"
#ifdef ZLIB_COMPAT
#  include "zlib.h"
#else
#  include "zlib-ng.h"
#endif

#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#include <gtest/gtest.h>

#include "test_shared.h"

#define MAX_LENGTH (32)

class compress_bound_variant : public testing::TestWithParam<int32_t> {
public:
    void estimate(int32_t level) {
        z_size_t estimate_len = 0;
        uint8_t *uncompressed = NULL;
        uint8_t dest[128];
        int err;

        uncompressed = (uint8_t *)malloc(MAX_LENGTH);
        ASSERT_TRUE(uncompressed != NULL);

        /* buffer with values for worst case compression */
        for (int32_t j = 0; j < MAX_LENGTH; j++) {
            uncompressed[j] = (uint8_t)j;
        }

        for (z_size_t i = 0; i < MAX_LENGTH; i++) {
            z_uintmax_t dest_len = sizeof(dest);

            /* calculate actual output length */
            estimate_len = PREFIX(compressBound)(i);

            err = PREFIX(compress2)(dest, &dest_len, uncompressed, i, level);
            EXPECT_EQ(err, Z_OK);
            EXPECT_GE(estimate_len, dest_len) <<
                "level: " << level << "\n" <<
                "length: " << i;
        }

        free(uncompressed);
    }
};

TEST_P(compress_bound_variant, estimate) {
    estimate(GetParam());
}

INSTANTIATE_TEST_SUITE_P(compress_bound, compress_bound_variant,
    testing::Values(0, 1, 2, 3, 4, 5, 6, 7, 8, 9));
