/* benchmark_main.cc -- benchmark suite main entry point
 * Copyright (C) 2022 Nathan Moinvaziri
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#include <stdio.h>

#include <benchmark/benchmark.h>

#ifndef BUILD_ALT
extern "C" {
#  include "zbuild.h"
#  include "../test_cpu_features.h"

#  ifndef DISABLE_RUNTIME_CPU_DETECTION
    struct cpu_features test_cpu_features;
#  endif
}
#endif

int main(int argc, char** argv) {
#ifndef BUILD_ALT
#  ifndef DISABLE_RUNTIME_CPU_DETECTION
    cpu_check_features(&test_cpu_features);
#  endif
#endif

    ::benchmark::Initialize(&argc, argv);
    ::benchmark::RunSpecifiedBenchmarks();

    return EXIT_SUCCESS;
}
