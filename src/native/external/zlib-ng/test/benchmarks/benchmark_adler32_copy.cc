/* benchmark_adler32_copy.cc -- benchmark adler32 (elided copy) variants
 * Copyright (C) 2022 Nathan Moinvaziri, Adam Stylinski
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#include <stdio.h>
#include <assert.h>
#include <string.h>

#include <benchmark/benchmark.h>

extern "C" {
#  include "zbuild.h"
#  include "zutil_p.h"
#  include "arch_functions.h"
#  include "../test_cpu_features.h"
}

#define MAX_RANDOM_INTS (1024 * 1024)
#define MAX_RANDOM_INTS_SIZE (MAX_RANDOM_INTS * sizeof(uint32_t))

typedef uint32_t (*adler32_cpy_func)(uint32_t adler, unsigned char *dst, const uint8_t *buf, size_t len);

class adler32_copy: public benchmark::Fixture {
private:
    uint32_t *random_ints_src;
    uint32_t *random_ints_dst;

public:
    void SetUp(const ::benchmark::State& state) {
        /* Control the alignment so that we have the best case scenario for loads. With
         * AVX512, unaligned loads can mean we're crossing a cacheline boundary at every load.
         * And while this is a realistic scenario, it makes it difficult to compare benchmark
         * to benchmark because one allocation could have been aligned perfectly for the loads
         * while the subsequent one happened to not be. This is not to be advantageous to AVX512
         * (indeed, all lesser SIMD implementations benefit from this aligned allocation), but to
         * control the _consistency_ of the results */
        random_ints_src = (uint32_t *)zng_alloc(MAX_RANDOM_INTS_SIZE);
        random_ints_dst = (uint32_t *)zng_alloc(MAX_RANDOM_INTS_SIZE);
        assert(random_ints_src != NULL);
        assert(random_ints_dst != NULL);

        for (int32_t i = 0; i < MAX_RANDOM_INTS; i++) {
            random_ints_src[i] = rand();
        }
    }

    void Bench(benchmark::State& state, adler32_cpy_func adler32_func) {
        uint32_t hash = 0;

        for (auto _ : state) {
            hash = adler32_func(hash, (unsigned char *)random_ints_dst,
                                (const unsigned char*)random_ints_src, (size_t)state.range(0));
        }

        benchmark::DoNotOptimize(hash);
    }

    void TearDown(const ::benchmark::State& state) {
        zng_free(random_ints_src);
        zng_free(random_ints_dst);
    }
};

#define BENCHMARK_ADLER32_COPY(name, fptr, support_flag) \
    BENCHMARK_DEFINE_F(adler32_copy, name)(benchmark::State& state) { \
        if (!support_flag) { \
            state.SkipWithError("CPU does not support " #name); \
        } \
        Bench(state, fptr); \
    } \
    BENCHMARK_REGISTER_F(adler32_copy, name)->Range(8192, MAX_RANDOM_INTS_SIZE);

#define BENCHMARK_ADLER32_BASELINE_COPY(name, fptr, support_flag) \
    BENCHMARK_DEFINE_F(adler32_copy, name)(benchmark::State& state) { \
        if (!support_flag) { \
            state.SkipWithError("CPU does not support " #name); \
        } \
        Bench(state, [](uint32_t init_sum, unsigned char *dst, \
                        const uint8_t *buf, size_t len) -> uint32_t { \
            memcpy(dst, buf, (size_t)len); \
            return fptr(init_sum, buf, len); \
        }); \
    } \
    BENCHMARK_REGISTER_F(adler32_copy, name)->Range(8192, MAX_RANDOM_INTS_SIZE);

BENCHMARK_ADLER32_BASELINE_COPY(c, adler32_c, 1);

#ifdef DISABLE_RUNTIME_CPU_DETECTION
BENCHMARK_ADLER32_BASELINE_COPY(native, native_adler32, 1);
#else

#ifdef ARM_NEON
/* If we inline this copy for neon, the function would go here */
//BENCHMARK_ADLER32_COPY(neon, adler32_neon, test_cpu_features.arm.has_neon);
BENCHMARK_ADLER32_BASELINE_COPY(neon_copy_baseline, adler32_neon, test_cpu_features.arm.has_neon);
#endif

#ifdef PPC_VMX
//BENCHMARK_ADLER32_COPY(vmx_inline_copy, adler32_fold_copy_vmx, test_cpu_features.power.has_altivec);
BENCHMARK_ADLER32_BASELINE_COPY(vmx_copy_baseline, adler32_vmx, test_cpu_features.power.has_altivec);
#endif
#ifdef POWER8_VSX
//BENCHMARK_ADLER32_COPY(power8_inline_copy, adler32_fold_copy_power8, test_cpu_features.power.has_arch_2_07);
BENCHMARK_ADLER32_BASELINE_COPY(power8, adler32_power8, test_cpu_features.power.has_arch_2_07);
#endif

#ifdef RISCV_RVV
//BENCHMARK_ADLER32_COPY(rvv, adler32_rvv, test_cpu_features.riscv.has_rvv);
BENCHMARK_ADLER32_BASELINE_COPY(rvv, adler32_rvv, test_cpu_features.riscv.has_rvv);
#endif

#ifdef X86_SSE42
BENCHMARK_ADLER32_BASELINE_COPY(sse42_baseline, adler32_ssse3, test_cpu_features.x86.has_ssse3);
BENCHMARK_ADLER32_COPY(sse42, adler32_fold_copy_sse42, test_cpu_features.x86.has_sse42);
#endif
#ifdef X86_AVX2
BENCHMARK_ADLER32_BASELINE_COPY(avx2_baseline, adler32_avx2, test_cpu_features.x86.has_avx2);
BENCHMARK_ADLER32_COPY(avx2, adler32_fold_copy_avx2, test_cpu_features.x86.has_avx2);
#endif
#ifdef X86_AVX512
BENCHMARK_ADLER32_BASELINE_COPY(avx512_baseline, adler32_avx512, test_cpu_features.x86.has_avx512_common);
BENCHMARK_ADLER32_COPY(avx512, adler32_fold_copy_avx512, test_cpu_features.x86.has_avx512_common);
#endif
#ifdef X86_AVX512VNNI
BENCHMARK_ADLER32_BASELINE_COPY(avx512_vnni_baseline, adler32_avx512_vnni, test_cpu_features.x86.has_avx512vnni);
BENCHMARK_ADLER32_COPY(avx512_vnni, adler32_fold_copy_avx512_vnni, test_cpu_features.x86.has_avx512vnni);
#endif

#endif
