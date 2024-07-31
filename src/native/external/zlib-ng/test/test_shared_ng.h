#ifndef TEST_SHARED_NG_H
#define TEST_SHARED_NG_H

#include "test_shared.h"

/* Test definitions that can only be used in the zlib-ng build environment. */

static inline int deflate_prime_32(PREFIX3(stream) *stream, uint32_t value) {
    int err;

#ifdef ZLIBNG_ENABLE_TESTS
    err = PREFIX(deflatePrime)(stream, 32, value);
#else
    /* zlib's deflatePrime() takes at most 16 bits */
    err = PREFIX(deflatePrime)(stream, 16, value & 0xffff);
    if (err != Z_OK) return err;
    err = PREFIX(deflatePrime)(stream, 16, value >> 16);
#endif

    return err;
}

#endif
