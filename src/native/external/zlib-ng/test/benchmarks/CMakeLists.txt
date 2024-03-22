cmake_minimum_required(VERSION 3.12)

include(FetchContent)

set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS ON)

enable_language(CXX)

# Search for Google benchmark package
find_package(benchmark QUIET)
if(NOT benchmark_FOUND)
    # Fetch google benchmark source code from official repository
    set(BENCHMARK_ENABLE_TESTING OFF)

    # Allow specifying alternative Google benchmark repository
    if(NOT DEFINED GBENCHMARK_REPOSITORY)
        set(GBENCHMARK_REPOSITORY https://github.com/google/benchmark.git)
    endif()
    if(NOT DEFINED GBENCHMARK_TAG)
        set(GBENCHMARK_TAG v1.7.1)
    endif()

    FetchContent_Declare(benchmark
        GIT_REPOSITORY ${GBENCHMARK_REPOSITORY}
        GIT_TAG ${GBENCHMARK_TAG})

    FetchContent_GetProperties(benchmark)
    if(NOT benchmark_POPULATED)
        FetchContent_Populate(benchmark)
        add_subdirectory(${benchmark_SOURCE_DIR} ${benchmark_BINARY_DIR} EXCLUDE_FROM_ALL)
    endif()
endif()

add_executable(benchmark_zlib
    benchmark_adler32.cc
    benchmark_adler32_copy.cc
    benchmark_compare256.cc
    benchmark_compare256_rle.cc
    benchmark_crc32.cc
    benchmark_main.cc
    benchmark_slidehash.cc
    )

target_compile_definitions(benchmark_zlib PRIVATE -DBENCHMARK_STATIC_DEFINE)
target_include_directories(benchmark_zlib PRIVATE
    ${PROJECT_SOURCE_DIR}
    ${PROJECT_BINARY_DIR}
    ${benchmark_SOURCE_DIR}/benchmark/include)

target_link_libraries(benchmark_zlib zlibstatic benchmark::benchmark)
if(WIN32)
    target_link_libraries(benchmark_zlib shlwapi)
endif()

add_test(NAME benchmark_zlib
    COMMAND ${CMAKE_CROSSCOMPILING_EMULATOR} $<TARGET_FILE:benchmark_zlib>)

if(WITH_BENCHMARK_APPS)
    option(BUILD_ALT_BENCH "Link against alternative zlib implementation" OFF)

    # Search for libpng package
    find_package(PNG QUIET)

    if(NOT PNG_FOUND)
        FetchContent_Declare(PNG
            GIT_REPOSITORY https://github.com/glennrp/libpng.git)

        FetchContent_GetProperties(PNG)
        if(NOT PNG_POPULATED)
            FetchContent_Populate(PNG)
            set(PNG_INCLUDE_DIR ${png_SOURCE_DIR})
            add_subdirectory(${png_SOURCE_DIR} ${png_BINARY_DIR})
        endif()
    endif()

    set(BENCH_APP_SRCS
        benchmark_png_encode.cc
        benchmark_png_decode.cc
        benchmark_main.cc
    )

    add_executable(benchmark_zlib_apps ${BENCH_APP_SRCS})

    if(DEFINED BUILD_ALT_BENCH)
        set(ZLIB_ALT_LIB "libz.a" CACHE FILEPATH "Optional alternative zlib implementation (defaults to stock zlib)")
        add_executable(benchmark_zlib_apps_alt ${BENCH_APP_SRCS})
        target_link_libraries(benchmark_zlib_apps_alt libpng.a ${ZLIB_ALT_LIB} benchmark::benchmark)
        target_compile_definitions(benchmark_zlib_apps_alt PRIVATE BUILD_ALT=1)
        target_include_directories(benchmark_zlib_apps_alt PRIVATE
            ${PROJECT_SOURCE_DIR}
            ${PROJECT_BINARY_DIR}
            ${PNG_INCLUDE_DIR}
            ${benchmark_SOURCE_DIR}/benchmark/include)
    endif()

    target_include_directories(benchmark_zlib_apps PRIVATE
        ${PROJECT_SOURCE_DIR}
        ${PROJECT_BINARY_DIR}
        ${PNG_INCLUDE_DIR}
        ${benchmark_SOURCE_DIR}/benchmark/include)

    # We need the static png library if we're statically linking to zlib,
    # otherwise it will resolve these things in the system provided dynamic
    # libraries (likely linked to stock zlib)
    target_link_libraries(benchmark_zlib_apps libpng.a zlibstatic benchmark::benchmark)
endif()
