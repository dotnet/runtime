/* test_inflate_sync.cc - Test inflateSync using full flush deflate and corrupted data */

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

#include "test_shared.h"

#include <gtest/gtest.h>

TEST(inflate, sync) {
    PREFIX3(stream) c_stream, d_stream;
    uint8_t compr[128], uncompr[128];
    z_size_t compr_len = sizeof(compr), uncompr_len = sizeof(uncompr);
    int err;

    memset(&c_stream, 0, sizeof(c_stream));

    /* build compressed stream with full flush */
    err = PREFIX(deflateInit)(&c_stream, Z_DEFAULT_COMPRESSION);
    EXPECT_EQ(err, Z_OK);

    c_stream.next_in  = (z_const unsigned char *)hello;
    c_stream.next_out = compr;
    c_stream.avail_in = 3;
    c_stream.avail_out = (uint32_t)compr_len;

    err = PREFIX(deflate)(&c_stream, Z_FULL_FLUSH);
    EXPECT_EQ(err, Z_OK);

    /* force an error in first compressed block */
    compr[3]++;
    c_stream.avail_in = hello_len-3;

    err = PREFIX(deflate)(&c_stream, Z_FINISH);
    EXPECT_EQ(err, Z_STREAM_END);

    err = PREFIX(deflateEnd)(&c_stream);
    EXPECT_EQ(err, Z_OK);

    compr_len = (z_size_t)c_stream.total_out;

    memset(&d_stream, 0, sizeof(d_stream));
    /* just read the zlib header */
    d_stream.next_in = compr;
    d_stream.avail_in = 2;

    err = PREFIX(inflateInit)(&d_stream);
    EXPECT_EQ(err, Z_OK);

    d_stream.next_out = uncompr;
    d_stream.avail_out = (uint32_t)uncompr_len;

    err = PREFIX(inflate)(&d_stream, Z_NO_FLUSH);
    EXPECT_EQ(err, Z_OK);

    /* read all compressed data, but skip damaged part */
    d_stream.avail_in = (uint32_t)compr_len-2;
    err = PREFIX(inflateSync)(&d_stream);
    EXPECT_EQ(err, Z_OK);

    err = PREFIX(inflate)(&d_stream, Z_FINISH);
    EXPECT_EQ(err, Z_STREAM_END);

    err = PREFIX(inflateEnd)(&d_stream);
    EXPECT_EQ(err, Z_OK);
}
