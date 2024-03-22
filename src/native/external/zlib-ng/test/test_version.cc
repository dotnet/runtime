/* test_version.cc - Test zVersion() and zlibCompileFlags() */

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

TEST(version, basic) {
    static const char *my_version = PREFIX2(VERSION);

    EXPECT_EQ(zVersion()[0], my_version[0]);
    EXPECT_STREQ(zVersion(), PREFIX2(VERSION));

    printf("zlib-ng version %s = 0x%08lx, compile flags = 0x%lx\n",
            ZLIBNG_VERSION, ZLIBNG_VERNUM, PREFIX(zlibCompileFlags)());
}
