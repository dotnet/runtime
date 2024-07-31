/* test_inflate_copy.cc - Test copying inflate stream */

#include "zbuild.h"
#ifdef ZLIB_COMPAT
#  include "zlib.h"
#else
#  include "zlib-ng.h"
#endif

#include "test_shared.h"

#include <gtest/gtest.h>

TEST(inflate, copy_back_and_forth) {
    PREFIX3(stream) d1_stream, d2_stream;
    int err;

    memset(&d1_stream, 0, sizeof(d1_stream));
    err = PREFIX(inflateInit2)(&d1_stream, MAX_WBITS + 14);
    ASSERT_EQ(err, Z_OK);
    err = PREFIX(inflateCopy)(&d2_stream, &d1_stream);
    ASSERT_EQ(err, Z_OK);
    err = PREFIX(inflateEnd)(&d1_stream);
    ASSERT_EQ(err, Z_OK);
    err = PREFIX(inflateCopy)(&d1_stream, &d2_stream);
    ASSERT_EQ(err, Z_OK);
    err = PREFIX(inflateEnd)(&d1_stream);
    ASSERT_EQ(err, Z_OK);
    err = PREFIX(inflateEnd)(&d2_stream);
    ASSERT_EQ(err, Z_OK);
}
