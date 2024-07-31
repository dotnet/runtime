/* test_raw.cc - Test raw streams. */

#include "zbuild.h"
#ifdef ZLIB_COMPAT
#  include "zlib.h"
#else
#  include "zlib-ng.h"
#endif

#include <gtest/gtest.h>

TEST(raw, basic) {
    PREFIX3(stream) stream;
    int err;
    unsigned char plain[512];
    size_t i;
    unsigned char compr[sizeof(plain)];
    unsigned int compr_len;
    unsigned char plain_again[sizeof(plain)];

    memset(&stream, 0, sizeof(stream));
    err = PREFIX(deflateInit2)(&stream, Z_BEST_SPEED, Z_DEFLATED, -15, 8, Z_DEFAULT_STRATEGY);
    EXPECT_EQ(err, Z_OK);

    for (i = 0; i < sizeof(plain); i++)
        plain[i] = (unsigned char)i;
    stream.adler = 0x12345678;
    stream.next_in = plain;
    stream.avail_in = (uint32_t)sizeof(plain);
    stream.next_out = compr;
    stream.avail_out = (uint32_t)sizeof(compr);
    err = PREFIX(deflate)(&stream, Z_FINISH);
    EXPECT_EQ(err, Z_STREAM_END);
    EXPECT_EQ(stream.adler, 0x12345678);
    compr_len = sizeof(compr) - stream.avail_out;

    err = PREFIX(deflateEnd)(&stream);
    EXPECT_EQ(err, Z_OK);

    memset(&stream, 0, sizeof(stream));
    err = PREFIX(inflateInit2)(&stream, -MAX_WBITS);
    EXPECT_EQ(err, Z_OK);

    stream.adler = 0x87654321;
    stream.next_in = compr;
    stream.avail_in = compr_len;
    stream.next_out = plain_again;
    stream.avail_out = (unsigned int)sizeof(plain_again);

    err = PREFIX(inflate)(&stream, Z_NO_FLUSH);
    EXPECT_EQ(err, Z_STREAM_END);
    EXPECT_EQ(stream.adler, 0x87654321);

    err = PREFIX(inflateEnd)(&stream);
    EXPECT_EQ(err, Z_OK);

    EXPECT_TRUE(memcmp(plain_again, plain, sizeof(plain)) == 0);
}
