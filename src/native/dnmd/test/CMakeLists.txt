# Configure the compiler
include(../configure.cmake)
include(FindNetHost.cmake)

if (WIN32)
    # Test executables import dnmd_interfaces.dll and nethost.dll from their output directories.
    string(REPLACE "/DEPENDENTLOADFLAG:0x800" "" CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS}")
endif()

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
set_property(DIRECTORY ${googletest_SOURCE_DIR} PROPERTY SET_SOURCE_CHARSET OFF)

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

if (DNMD_ENABLE_FUZZING AND CMAKE_CXX_COMPILER_ID STREQUAL "Clang")
    # FuzzTest enables GTest's Abseil integration, which has sign-conversion warnings in v1.14.0.
    target_compile_options(gtest PRIVATE -Wno-error=sign-conversion)
endif()

if (WIN32)
    FetchContent_Declare(
        wil
        GIT_REPOSITORY
        https://github.com/microsoft/wil.git
        GIT_TAG
        v1.0.231216.1
    )

    set(WIL_BUILD_PACKAGING OFF CACHE BOOL "" FORCE)
    set(WIL_BUILD_TESTS OFF CACHE BOOL "" FORCE)

    FetchContent_MakeAvailable(wil)
endif()

include(GoogleTest)

if (WIN32)
    function(stage_dnmd_test_dependencies target)
        add_custom_target(${target}_runtime_dlls
            COMMAND ${CMAKE_COMMAND} -E make_directory $<TARGET_FILE_DIR:${target}>
            COMMAND ${CMAKE_COMMAND} -E copy_if_different $<TARGET_FILE:dnmd::interfaces> $<TARGET_FILE_DIR:${target}>
            COMMAND ${CMAKE_COMMAND} -E copy_if_different $<TARGET_FILE:nethost> $<TARGET_FILE_DIR:${target}>
            DEPENDS dnmd_interfaces
            VERBATIM)
        add_dependencies(${target} ${target}_runtime_dlls)
    endfunction()
endif()

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
add_subdirectory(regpal)
add_subdirectory(regperf)
add_subdirectory(regtest)
add_subdirectory(emit)
if (DNMD_ENABLE_INTERNAL_INTERFACES)
    add_subdirectory(internal)
endif()
if (DNMD_ENABLE_FUZZING)
    add_subdirectory(regfuzz)
endif()
