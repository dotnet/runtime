/* test_dict.cc - Test deflate() and inflate() with preset dictionary */

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

/* Maximum dictionary size, according to inflateGetDictionary() description. */
#define MAX_DICTIONARY_SIZE 32768

static const char dictionary[] = "hello";

TEST(dictionary, basic) {
    PREFIX3(stream) c_stream, d_stream;
    uint8_t compr[128], uncompr[128];
    z_size_t compr_len = sizeof(compr), uncompr_len = sizeof(uncompr);
    uint32_t dict_adler = 0;
    uint8_t check_dict[MAX_DICTIONARY_SIZE];
    uint32_t check_dict_len = 0;
    int err;

    memset(&c_stream, 0, sizeof(c_stream));
    memset(&d_stream, 0, sizeof(d_stream));

    err = PREFIX(deflateInit)(&c_stream, Z_BEST_COMPRESSION);
    EXPECT_EQ(err, Z_OK);

    err = PREFIX(deflateSetDictionary)(&c_stream,
        (const unsigned char *)dictionary, (int)sizeof(dictionary));
    EXPECT_EQ(err, Z_OK);

    dict_adler = c_stream.adler;
    c_stream.next_out = compr;
    c_stream.avail_out = (uint32_t)compr_len;

    c_stream.next_in = (z_const unsigned char *)hello;
    c_stream.avail_in = (uint32_t)hello_len;

    err = PREFIX(deflate)(&c_stream, Z_FINISH);
    EXPECT_EQ(err, Z_STREAM_END);

    err = PREFIX(deflateEnd)(&c_stream);
    EXPECT_EQ(err, Z_OK);

    strcpy((char*)uncompr, "garbage garbage garbage");

    d_stream.next_in  = compr;
    d_stream.avail_in = (unsigned int)compr_len;

    err = PREFIX(inflateInit)(&d_stream);
    EXPECT_EQ(err, Z_OK);

    d_stream.next_out = uncompr;
    d_stream.avail_out = (unsigned int)uncompr_len;

    for (;;) {
        err = PREFIX(inflate)(&d_stream, Z_NO_FLUSH);
        if (err == Z_STREAM_END)
            break;
        if (err == Z_NEED_DICT) {
            EXPECT_EQ(d_stream.adler, dict_adler);
            err = PREFIX(inflateSetDictionary)(&d_stream, (const unsigned char*)dictionary,
                (uint32_t)sizeof(dictionary));
            EXPECT_EQ(d_stream.adler, dict_adler);
        }
        EXPECT_EQ(err, Z_OK);
    }

    err = PREFIX(inflateGetDictionary)(&d_stream, NULL, &check_dict_len);
    EXPECT_EQ(err, Z_OK);
#ifndef S390_DFLTCC_INFLATE
    EXPECT_GE(check_dict_len, sizeof(dictionary));
#endif

    err = PREFIX(inflateGetDictionary)(&d_stream, check_dict, &check_dict_len);
    EXPECT_EQ(err, Z_OK);
#ifndef S390_DFLTCC_INFLATE
    EXPECT_TRUE(memcmp(dictionary, check_dict, sizeof(dictionary)) == 0);
#endif

    err = PREFIX(inflateEnd)(&d_stream);
    EXPECT_EQ(err, Z_OK);

    EXPECT_TRUE(strncmp((char*)uncompr, hello, sizeof(hello)) == 0);
}
