/* benchmark_adler32.cc -- benchmark adler32 variants
 * Copyright (C) 2022 Nathan Moinvaziri, Adam Stylinski
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

class adler32: public benchmark::Fixture {
private:
    uint32_t *random_ints;

public:
    void SetUp(const ::benchmark::State& state) {
        /* Control the alignment so that we have the best case scenario for loads. With
         * AVX512, unaligned loads can mean we're crossing a cacheline boundary at every load.
         * And while this is a realistic scenario, it makes it difficult to compare benchmark
         * to benchmark because one allocation could have been aligned perfectly for the loads
         * while the subsequent one happened to not be. This is not to be advantageous to AVX512
         * (indeed, all lesser SIMD implementations benefit from this aligned allocation), but to
         * control the _consistency_ of the results */
        random_ints = (uint32_t *)zng_alloc(MAX_RANDOM_INTS_SIZE);
        assert(random_ints != NULL);

        for (int32_t i = 0; i < MAX_RANDOM_INTS; i++) {
            random_ints[i] = rand();
        }
    }

    void Bench(benchmark::State& state, adler32_func adler32) {
        uint32_t hash = 0;

        for (auto _ : state) {
            hash = adler32(hash, (const unsigned char *)random_ints, (size_t)state.range(0));
        }

        benchmark::DoNotOptimize(hash);
    }

    void TearDown(const ::benchmark::State& state) {
        zng_free(random_ints);
    }
};

#define BENCHMARK_ADLER32(name, fptr, support_flag) \
    BENCHMARK_DEFINE_F(adler32, name)(benchmark::State& state) { \
        if (!support_flag) { \
            state.SkipWithError("CPU does not support " #name); \
        } \
        Bench(state, fptr); \
    } \
    BENCHMARK_REGISTER_F(adler32, name)->Arg(1)->Arg(8)->Arg(12)->Arg(16)->Arg(32)->Arg(64)->Arg(512)->Arg(4<<10)->Arg(32<<10)->Arg(256<<10)->Arg(4096<<10)

BENCHMARK_ADLER32(c, adler32_c, 1);

#ifdef DISABLE_RUNTIME_CPU_DETECTION
BENCHMARK_ADLER32(native, native_adler32, 1);
#else

#ifdef ARM_NEON
BENCHMARK_ADLER32(neon, adler32_neon, test_cpu_features.arm.has_neon);
#endif

#ifdef PPC_VMX
BENCHMARK_ADLER32(vmx, adler32_vmx, test_cpu_features.power.has_altivec);
#endif
#ifdef POWER8_VSX
BENCHMARK_ADLER32(power8, adler32_power8, test_cpu_features.power.has_arch_2_07);
#endif

#ifdef RISCV_RVV
BENCHMARK_ADLER32(rvv, adler32_rvv, test_cpu_features.riscv.has_rvv);
#endif

#ifdef X86_SSSE3
BENCHMARK_ADLER32(ssse3, adler32_ssse3, test_cpu_features.x86.has_ssse3);
#endif
#ifdef X86_AVX2
BENCHMARK_ADLER32(avx2, adler32_avx2, test_cpu_features.x86.has_avx2);
#endif
#ifdef X86_AVX512
BENCHMARK_ADLER32(avx512, adler32_avx512, test_cpu_features.x86.has_avx512_common);
#endif
#ifdef X86_AVX512VNNI
BENCHMARK_ADLER32(avx512_vnni, adler32_avx512_vnni, test_cpu_features.x86.has_avx512vnni);
#endif

#endif
