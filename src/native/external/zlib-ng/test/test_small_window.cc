/* test_small_window.cc - Test deflate() and inflate() with a small window and a preset dictionary */

#include "zbuild.h"
#ifdef ZLIB_COMPAT
#  include "zlib.h"
#else
#  include "zlib-ng.h"
#endif

#include <gtest/gtest.h>

TEST(small_window, basic) {
    PREFIX3(stream) stream;
    int err;
    unsigned char plain[128];
    unsigned char dictionary1[(1 << 9) - sizeof(plain) / 2];
    size_t i;
    unsigned char compr[sizeof(plain)];
    unsigned int compr_len;
    unsigned char plain_again[sizeof(plain)];

    memset(&stream, 0, sizeof(stream));
    err = PREFIX(deflateInit2)(&stream, Z_BEST_COMPRESSION, Z_DEFLATED, -9, 8, Z_DEFAULT_STRATEGY);
    EXPECT_EQ(err, Z_OK);

    /* Use a large dictionary that is loaded in two parts */
    memset(dictionary1, 'a', sizeof(dictionary1));
    err = PREFIX(deflateSetDictionary)(&stream, dictionary1, (unsigned int)sizeof(dictionary1));
    EXPECT_EQ(err, Z_OK);
    for (i = 0; i < sizeof(plain); i++)
        plain[i] = (unsigned char)i;
    err = PREFIX(deflateSetDictionary)(&stream, plain, (unsigned int)sizeof(plain));
    EXPECT_EQ(err, Z_OK);

    stream.next_in = plain;
    stream.avail_in = (uint32_t)sizeof(plain);
    stream.next_out = compr;
    stream.avail_out = (uint32_t)sizeof(compr);
    err = PREFIX(deflate)(&stream, Z_FINISH);
    EXPECT_EQ(err, Z_STREAM_END);
    compr_len = sizeof(compr) - stream.avail_out;

    err = PREFIX(deflateEnd)(&stream);
    EXPECT_EQ(err, Z_OK);

    memset(&stream, 0, sizeof(stream));
    err = PREFIX(inflateInit2)(&stream, -9);
    EXPECT_EQ(err, Z_OK);

    err = PREFIX(inflateSetDictionary)(&stream, dictionary1, (unsigned int)sizeof(dictionary1));
    EXPECT_EQ(err, Z_OK);
    err = PREFIX(inflateSetDictionary)(&stream, plain, (unsigned int)sizeof(plain));
    EXPECT_EQ(err, Z_OK);

    stream.next_in = compr;
    stream.avail_in = compr_len;
    stream.next_out = plain_again;
    stream.avail_out = (unsigned int)sizeof(plain_again);

    err = PREFIX(inflate)(&stream, Z_NO_FLUSH);
    EXPECT_EQ(err, Z_STREAM_END);

    err = PREFIX(inflateEnd)(&stream);
    EXPECT_EQ(err, Z_OK);

    EXPECT_TRUE(memcmp(plain_again, plain, sizeof(plain)) == 0);
}
