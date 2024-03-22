/* test_test.cc - Main entry point for test framework */

#include <stdio.h>

#include "gtest/gtest.h"

extern "C" {
#  include "zbuild.h"
#  include "test_cpu_features.h"

    struct cpu_features test_cpu_features;
}

GTEST_API_ int main(int argc, char **argv) {
  printf("Running main() from %s\n", __FILE__);
  cpu_check_features(&test_cpu_features);
  testing::InitGoogleTest(&argc, argv);
  return RUN_ALL_TESTS();
}
