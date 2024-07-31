/* test_deflate_pending.cc - Test deflatePending() with small buffers */

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

TEST(deflate, pending) {
    PREFIX3(stream) c_stream;
    uint8_t compr[128];
    z_size_t compr_len = sizeof(compr);
    int *bits;
    unsigned *ped;
    int err;


    bits = (int *)calloc(256, 1);
    ASSERT_TRUE(bits != NULL);
    ped = (unsigned *)calloc(256, 1);
    ASSERT_TRUE(ped != NULL);

    memset(&c_stream, 0, sizeof(c_stream));

    err = PREFIX(deflateInit)(&c_stream, Z_DEFAULT_COMPRESSION);
    EXPECT_EQ(err, Z_OK);

    c_stream.next_in = (z_const unsigned char *)hello;
    c_stream.next_out = compr;

    while (c_stream.total_in != hello_len && c_stream.total_out < compr_len) {
        c_stream.avail_in = c_stream.avail_out = 1; /* force small buffers */
        err = PREFIX(deflate)(&c_stream, Z_NO_FLUSH);
        EXPECT_EQ(err, Z_OK);
    }

    err = PREFIX(deflatePending)(&c_stream, ped, bits);
    EXPECT_EQ(err, Z_OK);

    EXPECT_GE(*bits, 0);
    EXPECT_LE(*bits, 7);

    /* Finish the stream, still forcing small buffers: */
    for (;;) {
        c_stream.avail_out = 1;
        err = PREFIX(deflate)(&c_stream, Z_FINISH);
        if (err == Z_STREAM_END) break;
        EXPECT_EQ(err, Z_OK);
    }

    err = PREFIX(deflateEnd)(&c_stream);
    EXPECT_EQ(err, Z_OK);

    free(bits);
    free(ped);
}
