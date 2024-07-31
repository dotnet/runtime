/* test_test.cc - Main entry point for test framework */

#include <stdio.h>

#include "gtest/gtest.h"

extern "C" {
#  include "zbuild.h"
#  include "test_cpu_features.h"
#  ifndef DISABLE_RUNTIME_CPU_DETECTION
    struct cpu_features test_cpu_features;
#  endif
}

GTEST_API_ int main(int argc, char **argv) {
  printf("Running main() from %s\n", __FILE__);
#ifndef DISABLE_RUNTIME_CPU_DETECTION
  cpu_check_features(&test_cpu_features);
#endif
  testing::InitGoogleTest(&argc, argv);
  return RUN_ALL_TESTS();
}
