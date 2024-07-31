#include <stdio.h>
#include <assert.h>

#include "zbuild.h"
#ifdef ZLIB_COMPAT
#  include "zlib.h"
#else
#  include "zlib-ng.h"
#endif

#define CHECK_ERR(err, msg) { \
    if (err != Z_OK) { \
        fprintf(stderr, "%s error: %d\n", msg, err); \
        exit(1); \
    } \
}

static const uint8_t *data;
static size_t dataLen;
static alloc_func zalloc = NULL;
static free_func zfree = NULL;

/* ===========================================================================
 * Test deflate() with small buffers
 */
void test_deflate(unsigned char *compr, size_t comprLen) {
    PREFIX3(stream) c_stream; /* compression stream */
    int err;
    unsigned long len = (unsigned long)dataLen;

    c_stream.zalloc = zalloc;
    c_stream.zfree = zfree;
    c_stream.opaque = (void *)0;

    err = PREFIX(deflateInit)(&c_stream, Z_DEFAULT_COMPRESSION);
    CHECK_ERR(err, "deflateInit");

    c_stream.next_in = (z_const unsigned char *)data;
    c_stream.next_out = compr;

    while (c_stream.total_in != len && c_stream.total_out < comprLen) {
        c_stream.avail_in = c_stream.avail_out = 1; /* force small buffers */
        err = PREFIX(deflate)(&c_stream, Z_NO_FLUSH);
        CHECK_ERR(err, "deflate small 1");
    }
    /* Finish the stream, still forcing small buffers: */
    for (;;) {
        c_stream.avail_out = 1;
        err = PREFIX(deflate)(&c_stream, Z_FINISH);
        if (err == Z_STREAM_END)
            break;
        CHECK_ERR(err, "deflate small 2");
    }

    err = PREFIX(deflateEnd)(&c_stream);
    CHECK_ERR(err, "deflateEnd");
}

/* ===========================================================================
 * Test inflate() with small buffers
 */
void test_inflate(unsigned char *compr, size_t comprLen, unsigned char *uncompr, size_t uncomprLen) {
    int err;
    PREFIX3(stream) d_stream; /* decompression stream */

    d_stream.zalloc = zalloc;
    d_stream.zfree = zfree;
    d_stream.opaque = (void *)0;

    d_stream.next_in = compr;
    d_stream.avail_in = 0;
    d_stream.next_out = uncompr;

    err = PREFIX(inflateInit)(&d_stream);
    CHECK_ERR(err, "inflateInit");

    while (d_stream.total_out < uncomprLen && d_stream.total_in < comprLen) {
        d_stream.avail_in = d_stream.avail_out = 1; /* force small buffers */
        err = PREFIX(inflate)(&d_stream, Z_NO_FLUSH);
        if (err == Z_STREAM_END)
            break;
        CHECK_ERR(err, "inflate");
    }

    err = PREFIX(inflateEnd)(&d_stream);
    CHECK_ERR(err, "inflateEnd");

    if (memcmp(uncompr, data, dataLen)) {
        fprintf(stderr, "bad inflate\n");
        exit(1);
    }
}

int LLVMFuzzerTestOneInput(const uint8_t *d, size_t size) {
    size_t comprLen = PREFIX(compressBound)(size);
    size_t uncomprLen = size;
    uint8_t *compr, *uncompr;

    /* Discard inputs larger than 1Mb. */
    static size_t kMaxSize = 1024 * 1024;

    if (size < 1 || size > kMaxSize)
        return 0;

    data = d;
    dataLen = size;
    compr = (uint8_t *)calloc(1, comprLen);
    uncompr = (uint8_t *)calloc(1, uncomprLen);

    test_deflate(compr, comprLen);
    test_inflate(compr, comprLen, uncompr, uncomprLen);

    free(compr);
    free(uncompr);

    /* This function must return 0. */
    return 0;
}
