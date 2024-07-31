/* test_deflate_dict.cc - Test deflateGetDictionary() with small buffers */

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

TEST(deflate, dictionary) {
    PREFIX3(stream) c_stream;
    uint8_t compr[128];
    uint32_t compr_len = sizeof(compr);
    uint8_t *dict_new = NULL;
    uint32_t *dict_len;
    int err;

    memset(&c_stream, 0, sizeof(c_stream));

    err = PREFIX(deflateInit)(&c_stream, Z_BEST_COMPRESSION);
    EXPECT_EQ(err, Z_OK);

    c_stream.next_out = compr;
    c_stream.avail_out = compr_len;

    c_stream.next_in = (z_const unsigned char *)hello;
    c_stream.avail_in = (uint32_t)hello_len;

    err = PREFIX(deflate)(&c_stream, Z_FINISH);
    EXPECT_EQ(err, Z_STREAM_END);

    dict_new = (uint8_t *)calloc(256, 1);
    ASSERT_TRUE(dict_new != NULL);
    dict_len = (uint32_t *)calloc(4, 1);
    ASSERT_TRUE(dict_len != NULL);

    err = PREFIX(deflateGetDictionary)(&c_stream, dict_new, dict_len);
    EXPECT_EQ(err, Z_OK);

    err = PREFIX(deflateEnd)(&c_stream);
    EXPECT_EQ(err, Z_OK);

    free(dict_new);
    free(dict_len);
}
