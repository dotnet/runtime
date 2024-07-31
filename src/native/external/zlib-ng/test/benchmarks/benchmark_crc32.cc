/* benchmark_crc32.cc -- benchmark crc32 variants
 * Copyright (C) 2022 Nathan Moinvaziri
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#include <stdio.h>
#include <assert.h>

#include <benchmark/benchmark.h>

extern "C" {
#  include "zbuild.h"
#  include "zutil_p.h"
#  include "arch_functions.h"
#  include "../test_cpu_features.h"
}

#define MAX_RANDOM_INTS (1024 * 1024)
#define MAX_RANDOM_INTS_SIZE (MAX_RANDOM_INTS * sizeof(uint32_t))

class crc32: public benchmark::Fixture {
private:
    uint32_t *random_ints;

public:
    void SetUp(const ::benchmark::State& state) {
        random_ints = (uint32_t *)zng_alloc(MAX_RANDOM_INTS_SIZE);
        assert(random_ints != NULL);

        for (int32_t i = 0; i < MAX_RANDOM_INTS; i++) {
            random_ints[i] = rand();
        }
    }

    void Bench(benchmark::State& state, crc32_func crc32) {
        uint32_t hash = 0;

        for (auto _ : state) {
            hash = crc32(hash, (const unsigned char *)random_ints, (size_t)state.range(0));
        }

        benchmark::DoNotOptimize(hash);
    }

    void TearDown(const ::benchmark::State& state) {
        zng_free(random_ints);
    }
};

#define BENCHMARK_CRC32(name, fptr, support_flag) \
    BENCHMARK_DEFINE_F(crc32, name)(benchmark::State& state) { \
        if (!support_flag) { \
            state.SkipWithError("CPU does not support " #name); \
        } \
        Bench(state, fptr); \
    } \
    BENCHMARK_REGISTER_F(crc32, name)->Arg(1)->Arg(8)->Arg(12)->Arg(16)->Arg(32)->Arg(64)->Arg(512)->Arg(4<<10)->Arg(32<<10)->Arg(256<<10)->Arg(4096<<10);

BENCHMARK_CRC32(braid, PREFIX(crc32_braid), 1);

#ifdef DISABLE_RUNTIME_CPU_DETECTION
BENCHMARK_CRC32(native, native_crc32, 1);
#else

#ifdef ARM_ACLE
BENCHMARK_CRC32(acle, crc32_acle, test_cpu_features.arm.has_crc32);
#endif
#ifdef POWER8_VSX_CRC32
BENCHMARK_CRC32(power8, crc32_power8, test_cpu_features.power.has_arch_2_07);
#endif
#ifdef S390_CRC32_VX
BENCHMARK_CRC32(vx, crc32_s390_vx, test_cpu_features.s390.has_vx);
#endif
#ifdef X86_PCLMULQDQ_CRC
/* CRC32 fold does a memory copy while hashing */
BENCHMARK_CRC32(pclmulqdq, crc32_pclmulqdq, test_cpu_features.x86.has_pclmulqdq);
#endif
#ifdef X86_VPCLMULQDQ_CRC
/* CRC32 fold does a memory copy while hashing */
BENCHMARK_CRC32(vpclmulqdq, crc32_vpclmulqdq, (test_cpu_features.x86.has_pclmulqdq && test_cpu_features.x86.has_avx512_common && test_cpu_features.x86.has_vpclmulqdq));
#endif

#endif
