/* test_deflate_header.cc - Test deflateSetHeader() with small buffers */

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

TEST(deflate, header) {
    PREFIX3(stream) c_stream;
    PREFIX(gz_header) *head;
    uint8_t compr[128];
    z_size_t compr_len = sizeof(compr);
    int err;

    head = (PREFIX(gz_header) *)calloc(1, sizeof(PREFIX(gz_header)));
    ASSERT_TRUE(head != NULL);

    memset(&c_stream, 0, sizeof(c_stream));

    /* gzip */
    err = PREFIX(deflateInit2)(&c_stream, Z_DEFAULT_COMPRESSION, Z_DEFLATED, MAX_WBITS + 16, 8, Z_DEFAULT_STRATEGY);
    EXPECT_EQ(err, Z_OK);

    head->text = 1;
    head->comment = (uint8_t *)"comment";
    head->name = (uint8_t *)"name";
    head->hcrc = 1;
    head->extra = (uint8_t *)"extra";
    head->extra_len = (uint32_t)strlen((const char *)head->extra);

    err = PREFIX(deflateSetHeader)(&c_stream, head);
    EXPECT_EQ(err, Z_OK);

    PREFIX(deflateBound)(&c_stream, (unsigned long)compr_len);

    c_stream.next_in  = (unsigned char *)hello;
    c_stream.next_out = compr;

    while (c_stream.total_in != hello_len && c_stream.total_out < compr_len) {
        c_stream.avail_in = c_stream.avail_out = 1; /* force small buffers */
        err = PREFIX(deflate)(&c_stream, Z_NO_FLUSH);
        EXPECT_EQ(err, Z_OK);
    }

    /* Finish the stream, still forcing small buffers: */
    for (;;) {
        c_stream.avail_out = 1;
        err = PREFIX(deflate)(&c_stream, Z_FINISH);
        if (err == Z_STREAM_END) break;
        EXPECT_EQ(err, Z_OK);
    }

    /* Check CRC32. */
    EXPECT_EQ(c_stream.adler, 0xb56c3f9dU);

    err = PREFIX(deflateEnd)(&c_stream);
    EXPECT_EQ(err, Z_OK);

    free(head);
}
