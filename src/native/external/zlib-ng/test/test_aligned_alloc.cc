/* test_aligned_alloc.cc - Test zng_alloc_aligned and zng_free_aligned */

#include <stdlib.h>
#include <errno.h>
#include <stdio.h>

extern "C" {
#  include "zbuild.h"
#  include "zutil.h"
}

#include <gtest/gtest.h>

#include "test_shared.h"

void *zng_calloc_unaligned(void *opaque, unsigned items, unsigned size) {
    uint8_t *pointer = (uint8_t *)calloc(1, (items * size) + 2);
    Z_UNUSED(opaque);
    if (pointer == NULL)
        return pointer;
    /* Store whether or not our allocation is aligned */
    *pointer = ((uint64_t)(intptr_t)pointer + 1) % 2 == 0;
    pointer++;
    if (*pointer) {
        /* Return pointer that is off by one */
        pointer++;
    }
    return (void *)pointer;
}

void zng_cfree_unaligned(void *opaque, void *ptr) {
    uint8_t *pointer = (uint8_t *)ptr;
    Z_UNUSED(opaque);
    pointer--;
    /* Get whether or not our original memory pointer was aligned */
    if (*pointer) {
        /* Return original aligned pointer to free() */
        pointer--;
    }
    free(pointer);
}

TEST(zalloc, aligned_64) {
    void *return_ptr = PREFIX3(alloc_aligned)(zng_calloc_unaligned, 0, 1, 100, 64);
    ASSERT_TRUE(return_ptr != NULL);
    EXPECT_EQ((intptr_t)return_ptr % 64, 0);
    PREFIX3(free_aligned)(zng_cfree_unaligned, 0, return_ptr);
}
