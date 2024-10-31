# Configure the compiler
include(../configure.cmake)
include(FindNetHost.cmake)

if (POLICY CMP0135)
  cmake_policy(SET CMP0135 NEW) # Set timestamps in downloaded archives to the time of download.
endif()

include(FetchContent)
FetchContent_Declare(
  googletest
  GIT_REPOSITORY
  https://github.com/google/googletest.git
  GIT_TAG
  v1.14.0
)
# For Windows: Prevent overriding the parent project's compiler/linker settings
set(gtest_force_shared_crt ON CACHE BOOL "" FORCE)
set(INSTALL_GTEST OFF CACHE BOOL "" FORCE)

FetchContent_Declare(
  benchmark
  GIT_REPOSITORY
  https://github.com/google/benchmark.git
  GIT_TAG
  v1.8.3
)

# Don't build the tests for the benchmark library.
set(BENCHMARK_ENABLE_TESTING OFF CACHE BOOL "" FORCE)
set(BENCHMARK_ENABLE_INSTALL OFF CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(googletest benchmark)

include(GoogleTest)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
add_subdirectory(regpal)
add_subdirectory(regperf)
add_subdirectory(regtest)
add_subdirectory(emit)
