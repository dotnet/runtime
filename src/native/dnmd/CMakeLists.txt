cmake_minimum_required(VERSION 3.10)
project(dnmd
    LANGUAGES C CXX)

option(INCLUDE_VENDORED_LIBS "Include vendored libraries (submodules) instead of discovering dependencies through packages." ON)

option(DNMD_ENABLE_FUZZING "Build with fuzzing support." OFF)
option(DNMD_ENABLE_TESTING "Enable building and running tests." ON)

set(CMAKE_INSTALL_PREFIX ${CMAKE_BINARY_DIR})

include(FetchContent)

if (POLICY CMP0135)
  cmake_policy(SET CMP0135 NEW) # Set timestamps in downloaded archives to the time of download.
endif()

if (DNMD_ENABLE_FUZZING)
    message(STATUS "Enabling fuzzing support.")
    FetchContent_Declare(
        fuzztest
        GIT_REPOSITORY
        https://github.com/google/fuzztest.git
        GIT_TAG
        2025-08-05
    )

    set(ANTLR_BUILD_CPP_TESTS OFF CACHE BOOL "" FORCE)
    set(FUZZTEST_FUZZING_MODE ON)

    FetchContent_MakeAvailable(fuzztest)

    fuzztest_setup_fuzzing_flags()
endif()

if (INCLUDE_VENDORED_LIBS)
    add_subdirectory(external/dncp)
else()
    find_package(dncp REQUIRED)
endif()

include_directories(src/inc)
include_directories(src/inc/external) # Hiding the "external" subdirectory due to uses of <...> in cor.h.

add_subdirectory(src/)

if (DNMD_ENABLE_TESTING OR DNMD_ENABLE_FUZZING)
    message(STATUS "Enabling tests.")
    enable_testing()

    add_subdirectory(test/)
endif()
