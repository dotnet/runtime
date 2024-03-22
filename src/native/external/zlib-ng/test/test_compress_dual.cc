/* test_compress_dual.cc - Test linking against both zlib and zlib-ng */

#include "zlib.h"

#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>

#include "test_shared.h"

#include <gtest/gtest.h>

TEST(compress, basic_zlib) {
    Byte compr[128], uncompr[128];
    uLong compr_len = sizeof(compr), uncompr_len = sizeof(uncompr);
    int err;

    err = compress(compr, &compr_len, (const unsigned char *)hello, hello_len);
    EXPECT_EQ(err, Z_OK);

    strcpy((char*)uncompr, "garbage");

    err = uncompress(uncompr, &uncompr_len, compr, compr_len);
    EXPECT_EQ(err, Z_OK);

    EXPECT_STREQ((char *)uncompr, (char *)hello);
}
