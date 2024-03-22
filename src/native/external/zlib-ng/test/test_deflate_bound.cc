/* test_deflate_bound.cc - Test deflateBound() with small buffers */

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

typedef struct {
    int32_t level;
    int32_t window_size;
    int32_t mem_level;
    bool after_init;
} deflate_bound_test;

static const deflate_bound_test tests[] = {
    {0, MAX_WBITS + 16, 1, true},
    {Z_BEST_SPEED, MAX_WBITS, MAX_MEM_LEVEL, true},
    {Z_BEST_COMPRESSION, MAX_WBITS, MAX_MEM_LEVEL, true},
    {Z_BEST_SPEED, MAX_WBITS, MAX_MEM_LEVEL, false},
    {Z_BEST_COMPRESSION, MAX_WBITS, MAX_MEM_LEVEL, false},
};

class deflate_bound_variant : public testing::TestWithParam<deflate_bound_test> {
public:
    void estimate(deflate_bound_test param) {
        PREFIX3(stream) c_stream;
        int estimate_len = 0;
        uint8_t *uncompressed = NULL;
        uint8_t *out_buf = NULL;
        int err;

        uncompressed = (uint8_t *)malloc(MAX_LENGTH);
        ASSERT_TRUE(uncompressed != NULL);
        memset(uncompressed, 'a', MAX_LENGTH);

        for (int32_t i = 0; i < MAX_LENGTH; i++) {
            memset(&c_stream, 0, sizeof(c_stream));

            c_stream.avail_in = i;
            c_stream.next_in = (z_const unsigned char *)uncompressed;
            c_stream.avail_out = 0;
            c_stream.next_out = out_buf;

            if (!param.after_init)
                estimate_len = PREFIX(deflateBound)(&c_stream, i);

            err = PREFIX(deflateInit2)(&c_stream, param.level, Z_DEFLATED,
                param.window_size, param.mem_level, Z_DEFAULT_STRATEGY);
            EXPECT_EQ(err, Z_OK);

            /* calculate actual output length and update structure */
            if (param.after_init)
                estimate_len = PREFIX(deflateBound)(&c_stream, i);
            out_buf = (uint8_t *)malloc(estimate_len);

            if (out_buf != NULL) {
                /* update zlib configuration */
                c_stream.avail_out = estimate_len;
                c_stream.next_out = out_buf;

                /* do the compression */
                err = PREFIX(deflate)(&c_stream, Z_FINISH);
                EXPECT_EQ(err, Z_STREAM_END) <<
                    "level: " << param.level << "\n" <<
                    "window_size: " << param.window_size << "\n" <<
                    "mem_level: " << param.mem_level << "\n" <<
                    "after_init: " << param.after_init << "\n" <<
                    "length: " << i;

                free(out_buf);
                out_buf = NULL;
            }

            err = PREFIX(deflateEnd)(&c_stream);
            EXPECT_EQ(err, Z_OK);
        }

        free(uncompressed);
    }
};

TEST_P(deflate_bound_variant, estimate) {
    estimate(GetParam());
}

INSTANTIATE_TEST_SUITE_P(deflate_bound, deflate_bound_variant, testing::ValuesIn(tests));
