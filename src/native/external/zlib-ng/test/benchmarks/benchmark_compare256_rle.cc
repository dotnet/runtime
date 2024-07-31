/* benchmark_compare256_rle.cc -- benchmark compare256_rle variants
 * Copyright (C) 2022 Nathan Moinvaziri
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#include <stdio.h>

#include <benchmark/benchmark.h>

extern "C" {
#  include "zbuild.h"
#  include "zutil_p.h"
#  include "compare256_rle.h"
}

#define MAX_COMPARE_SIZE (256)

class compare256_rle: public benchmark::Fixture {
private:
    uint8_t *str1;
    uint8_t *str2;

public:
    void SetUp(const ::benchmark::State& state) {
        str1 = (uint8_t *)zng_alloc(MAX_COMPARE_SIZE);
        assert(str1 != NULL);
        memset(str1, 'a', MAX_COMPARE_SIZE);

        str2 = (uint8_t *)zng_alloc(MAX_COMPARE_SIZE);
        assert(str2 != NULL);
        memset(str2, 'a', MAX_COMPARE_SIZE);
    }

    void Bench(benchmark::State& state, compare256_rle_func compare256_rle) {
        int32_t match_len = (int32_t)state.range(0) - 1;
        uint32_t len;

        str2[match_len] = 0;
        for (auto _ : state) {
            len = compare256_rle((const uint8_t *)str1, (const uint8_t *)str2);
        }
        str2[match_len] = 'a';

        benchmark::DoNotOptimize(len);
    }

    void TearDown(const ::benchmark::State& state) {
        zng_free(str1);
        zng_free(str2);
    }
};

#define BENCHMARK_COMPARE256_RLE(name, fptr, support_flag) \
    BENCHMARK_DEFINE_F(compare256_rle, name)(benchmark::State& state) { \
        if (!support_flag) { \
            state.SkipWithError("CPU does not support " #name); \
        } \
        Bench(state, fptr); \
    } \
    BENCHMARK_REGISTER_F(compare256_rle, name)->Range(1, MAX_COMPARE_SIZE);

BENCHMARK_COMPARE256_RLE(c, compare256_rle_c, 1);

#ifdef UNALIGNED_OK
BENCHMARK_COMPARE256_RLE(unaligned_16, compare256_rle_unaligned_16, 1);
#ifdef HAVE_BUILTIN_CTZ
BENCHMARK_COMPARE256_RLE(unaligned_32, compare256_rle_unaligned_32, 1);
#endif
#if defined(UNALIGNED64_OK) && defined(HAVE_BUILTIN_CTZLL)
BENCHMARK_COMPARE256_RLE(unaligned_64, compare256_rle_unaligned_64, 1);
#endif
#endif
