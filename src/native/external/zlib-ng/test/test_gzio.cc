/* test_gzio.cc - Test read/write of .gz files */

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

#define TESTFILE "foo.gz"

TEST(gzip, readwrite) {
#ifdef NO_GZCOMPRESS
    fprintf(stderr, "NO_GZCOMPRESS -- gz* functions cannot compress\n");
    GTEST_SKIP();
#else
    uint8_t compr[128], uncompr[128];
    uint32_t compr_len = sizeof(compr), uncompr_len = sizeof(uncompr);
    size_t read;
    int64_t pos;
    gzFile file;
    int err;

    /* Write gz file with test data */
    file = PREFIX(gzopen)(TESTFILE, "wb");
    ASSERT_TRUE(file != NULL);
    /* Write hello, hello! using gzputs and gzprintf */
    PREFIX(gzputc)(file, 'h');
    EXPECT_EQ(PREFIX(gzputs)(file, "ello"), 4);
    EXPECT_EQ(PREFIX(gzprintf)(file, ", %s!", "hello"), 8);
    /* Write string null-teriminator using gzseek */
    EXPECT_GE(PREFIX(gzseek)(file, 1L, SEEK_CUR), 0);
    /* Write hello, hello! using gzfwrite using best compression level */
    EXPECT_EQ(PREFIX(gzsetparams)(file, Z_BEST_COMPRESSION, Z_DEFAULT_STRATEGY), Z_OK);
    EXPECT_NE(PREFIX(gzfwrite)(hello, hello_len, 1, file), 0UL);
    /* Flush compressed bytes to file */
    EXPECT_EQ(PREFIX(gzflush)(file, Z_SYNC_FLUSH), Z_OK);
    compr_len = (uint32_t)PREFIX(gzoffset)(file);
    EXPECT_GE(compr_len, 0UL);
    PREFIX(gzclose)(file);

    /* Open gz file we previously wrote */
    file = PREFIX(gzopen)(TESTFILE, "rb");
    ASSERT_TRUE(file != NULL);

    /* Read uncompressed data - hello, hello! string twice */
    strcpy((char*)uncompr, "garbages");
    EXPECT_EQ(PREFIX(gzread)(file, uncompr, (unsigned)uncompr_len), (int)(hello_len + hello_len));
    EXPECT_STREQ((char*)uncompr, hello);

    /* Check position at the end of the gz file */
    EXPECT_EQ(PREFIX(gzeof)(file), 1);

    /* Seek backwards mid-string and check char reading with gzgetc and gzungetc */
    pos = PREFIX(gzseek)(file, -22L, SEEK_CUR);
    EXPECT_EQ(pos, 6);
    EXPECT_EQ(PREFIX(gztell)(file), pos);
    EXPECT_EQ(PREFIX(gzgetc)(file), ' ');
    EXPECT_EQ(PREFIX(gzungetc)(' ', file), ' ');
    /* Read first hello, hello! string with gzgets */
    strcpy((char*)uncompr, "garbages");
    PREFIX(gzgets)(file, (char*)uncompr, (int)uncompr_len);
    EXPECT_EQ(strlen((char*)uncompr), 7UL); /* " hello!" */
    EXPECT_STREQ((char*)uncompr, hello + 6);

    /* Seek to second hello, hello! string */
    pos = PREFIX(gzseek)(file, 14L, SEEK_SET);
    EXPECT_EQ(pos, 14);
    EXPECT_EQ(PREFIX(gztell)(file), pos);

    /* Check position not at end of file */
    EXPECT_EQ(PREFIX(gzeof)(file), 0);
    /* Read first hello, hello! string with gzfread */
    strcpy((char*)uncompr, "garbages");
    read = PREFIX(gzfread)(uncompr, uncompr_len, 1, file);
    EXPECT_STREQ((const char *)uncompr, hello);

    pos = PREFIX(gzoffset)(file);
    EXPECT_GE(pos, 0);
    EXPECT_EQ(pos, (compr_len + 10));

    /* Trigger an error and clear it with gzclearerr */
    PREFIX(gzfread)(uncompr, (size_t)-1, (size_t)-1, file);
    PREFIX(gzerror)(file, &err);
    EXPECT_NE(err, 0);

    PREFIX(gzclearerr)(file);
    PREFIX(gzerror)(file, &err);
    EXPECT_EQ(err, 0);

    PREFIX(gzclose)(file);

    EXPECT_EQ(PREFIX(gzclose)(NULL), Z_STREAM_ERROR);
    Z_UNUSED(read);
#endif
}
