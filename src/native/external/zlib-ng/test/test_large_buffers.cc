/* test_large_buffers.cc - Test deflate() and inflate() with large buffers */

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
#include <time.h>

#include <gtest/gtest.h>

#include "test_shared.h"

#define COMPR_BUFFER_SIZE (48 * 1024)
#define UNCOMPR_BUFFER_SIZE (32 * 1024)
#define UNCOMPR_RAND_SIZE (8 * 1024)

TEST(deflate, large_buffers) {
    PREFIX3(stream) c_stream, d_stream;
    uint8_t *compr, *uncompr;
    uint32_t compr_len, uncompr_len;
    int32_t i;
    time_t now;
    int err;

    memset(&c_stream, 0, sizeof(c_stream));
    memset(&d_stream, 0, sizeof(d_stream));

    compr = (uint8_t *)calloc(1, COMPR_BUFFER_SIZE);
    ASSERT_TRUE(compr != NULL);
    uncompr = (uint8_t *)calloc(1, UNCOMPR_BUFFER_SIZE);
    ASSERT_TRUE(uncompr != NULL);

    compr_len = COMPR_BUFFER_SIZE;
    uncompr_len = UNCOMPR_BUFFER_SIZE;

    srand((unsigned)time(&now));
    for (i = 0; i < UNCOMPR_RAND_SIZE; i++)
        uncompr[i] = (uint8_t)(rand() % 256);

    err = PREFIX(deflateInit)(&c_stream, Z_DEFAULT_COMPRESSION);
    EXPECT_EQ(err, Z_OK);

    c_stream.next_out = compr;
    c_stream.avail_out = compr_len;
    c_stream.next_in = uncompr;
    c_stream.avail_in = uncompr_len;

    err = PREFIX(deflate)(&c_stream, Z_NO_FLUSH);
    EXPECT_EQ(err, Z_OK);
    EXPECT_EQ(c_stream.avail_in, 0);

    err = PREFIX(deflate)(&c_stream, Z_FINISH);
    EXPECT_EQ(err, Z_STREAM_END);

    err = PREFIX(deflateEnd)(&c_stream);
    EXPECT_EQ(err, Z_OK);

    d_stream.next_in  = compr;
    d_stream.avail_in = compr_len;
    d_stream.next_out = uncompr;

    err = PREFIX(inflateInit)(&d_stream);
    EXPECT_EQ(err, Z_OK);

    for (;;) {
        d_stream.next_out = uncompr;            /* discard the output */
        d_stream.avail_out = uncompr_len;
        err = PREFIX(inflate)(&d_stream, Z_NO_FLUSH);
        if (err == Z_STREAM_END) break;
        EXPECT_EQ(err, Z_OK);
    }

    err = PREFIX(inflateEnd)(&d_stream);
    EXPECT_EQ(err, Z_OK);

    EXPECT_EQ(d_stream.total_out, uncompr_len);

    free(compr);
    free(uncompr);
}
