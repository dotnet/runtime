/* benchmark_slidehash.cc -- benchmark slide_hash variants
 * Copyright (C) 2022 Adam Stylinski, Nathan Moinvaziri
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#include <limits.h>

#include <benchmark/benchmark.h>

extern "C" {
#  include "zbuild.h"
#  include "zutil_p.h"
#  include "deflate.h"
#  include "arch_functions.h"
#  include "../test_cpu_features.h"
}

#define MAX_RANDOM_INTS 32768

class slide_hash: public benchmark::Fixture {
private:
    uint16_t *l0;
    uint16_t *l1;
    deflate_state *s_g;

public:
    void SetUp(const ::benchmark::State& state) {
        l0 = (uint16_t *)zng_alloc(HASH_SIZE * sizeof(uint16_t));

        for (uint32_t i = 0; i < HASH_SIZE; i++) {
            l0[i] = rand();
        }

        l1 = (uint16_t *)zng_alloc(MAX_RANDOM_INTS * sizeof(uint16_t));

        for (int32_t i = 0; i < MAX_RANDOM_INTS; i++) {
            l1[i] = rand();
        }

        deflate_state *s = (deflate_state*)malloc(sizeof(deflate_state));
        s->head = l0;
        s->prev = l1;
        s_g = s;
    }

    void Bench(benchmark::State& state, slide_hash_func slide_hash) {
        s_g->w_size = (uint32_t)state.range(0);

        for (auto _ : state) {
            slide_hash(s_g);
            benchmark::DoNotOptimize(s_g);
        }
    }

    void TearDown(const ::benchmark::State& state) {
        zng_free(l0);
        zng_free(l1);
    }
};

#define BENCHMARK_SLIDEHASH(name, fptr, support_flag) \
    BENCHMARK_DEFINE_F(slide_hash, name)(benchmark::State& state) { \
        if (!support_flag) { \
            state.SkipWithError("CPU does not support " #name); \
        } \
        Bench(state, fptr); \
    } \
    BENCHMARK_REGISTER_F(slide_hash, name)->RangeMultiplier(2)->Range(1024, MAX_RANDOM_INTS);

BENCHMARK_SLIDEHASH(c, slide_hash_c, 1);

#ifdef DISABLE_RUNTIME_CPU_DETECTION
BENCHMARK_SLIDEHASH(native, native_slide_hash, 1);
#else

#ifdef ARM_SIMD
BENCHMARK_SLIDEHASH(armv6, slide_hash_armv6, test_cpu_features.arm.has_simd);
#endif
#ifdef ARM_NEON
BENCHMARK_SLIDEHASH(neon, slide_hash_neon, test_cpu_features.arm.has_neon);
#endif
#ifdef POWER8_VSX
BENCHMARK_SLIDEHASH(power8, slide_hash_power8, test_cpu_features.power.has_arch_2_07);
#endif
#ifdef PPC_VMX
BENCHMARK_SLIDEHASH(vmx, slide_hash_vmx, test_cpu_features.power.has_altivec);
#endif
#ifdef RISCV_RVV
BENCHMARK_SLIDEHASH(rvv, slide_hash_rvv, test_cpu_features.riscv.has_rvv);
#endif
#ifdef X86_SSE2
BENCHMARK_SLIDEHASH(sse2, slide_hash_sse2, test_cpu_features.x86.has_sse2);
#endif
#ifdef X86_AVX2
BENCHMARK_SLIDEHASH(avx2, slide_hash_avx2, test_cpu_features.x86.has_avx2);
#endif

#endif
