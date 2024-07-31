/* test_compress.cc - Test compress() and uncompress() using hello world string */

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

TEST(compress, basic) {
    uint8_t compr[128], uncompr[128];
    z_uintmax_t compr_len = sizeof(compr), uncompr_len = sizeof(uncompr);
    int err;

    err = PREFIX(compress)(compr, &compr_len, (const unsigned char *)hello, hello_len);
    EXPECT_EQ(err, Z_OK);

    strcpy((char*)uncompr, "garbage");

    err = PREFIX(uncompress)(uncompr, &uncompr_len, compr, compr_len);
    EXPECT_EQ(err, Z_OK);

    EXPECT_STREQ((char *)uncompr, (char *)hello);
}
