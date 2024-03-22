cmake_minimum_required(VERSION 3.5.1)

macro(configure_test_executable target)
    target_include_directories(${target} PRIVATE ${PROJECT_BINARY_DIR} ${PROJECT_SOURCE_DIR})
    set_target_properties(${target} PROPERTIES RUNTIME_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR})
    if(NOT WITH_GZFILEOP)
        target_compile_definitions(${target} PRIVATE -DWITH_GZFILEOP)
        target_sources(${target} PRIVATE ${ZLIB_GZFILE_PRIVATE_HDRS} ${ZLIB_GZFILE_SRCS})
    endif()
endmacro()

if(ZLIBNG_ENABLE_TESTS)
    add_definitions(-DZLIBNG_ENABLE_TESTS)
endif()

add_executable(example example.c)
configure_test_executable(example)
target_link_libraries(example zlib)
add_test(NAME example COMMAND ${CMAKE_CROSSCOMPILING_EMULATOR} $<TARGET_FILE:example>)

add_executable(minigzip minigzip.c)
configure_test_executable(minigzip)
if(NOT DEFINED BUILD_SHARED_LIBS)
    target_link_libraries(minigzip zlibstatic)
else()
    target_link_libraries(minigzip zlib)
endif()
if(BASEARCH_S360_FOUND)
    if(WITH_DFLTCC_DEFLATE OR WITH_DFLTCC_INFLATE)
        set_source_files_properties(minigzip.c PROPERTIES COMPILE_DEFINITIONS BUFLEN=262144)
    endif()
endif()
set(MINIGZIP_COMMAND ${CMAKE_CROSSCOMPILING_EMULATOR} $<TARGET_FILE:minigzip>)

add_executable(minideflate minideflate.c)
configure_test_executable(minideflate)
target_link_libraries(minideflate zlib)
set(MINIDEFLATE_COMMAND ${CMAKE_CROSSCOMPILING_EMULATOR} $<TARGET_FILE:minideflate>)

if(INSTALL_UTILS)
    install(TARGETS minigzip minideflate
        RUNTIME DESTINATION "${CMAKE_INSTALL_BINDIR}"
        ARCHIVE DESTINATION "${CMAKE_INSTALL_LIBDIR}"
        LIBRARY DESTINATION "${CMAKE_INSTALL_LIBDIR}")
endif()

add_executable(switchlevels switchlevels.c)
configure_test_executable(switchlevels)
target_link_libraries(switchlevels zlib)
set(SWITCHLEVELS_COMMAND ${CMAKE_CROSSCOMPILING_EMULATOR} $<TARGET_FILE:switchlevels>)

add_executable(infcover infcover.c)
configure_test_executable(infcover)
target_link_libraries(infcover zlib)
if(NOT DEFINED BUILD_SHARED_LIBS OR BUILD_SHARED_LIBS)
    target_sources(infcover PRIVATE ${PROJECT_SOURCE_DIR}/inftrees.c)
endif()
# infcover references zng_inflate_table() and struct inflate_state, which are internal to zlib-ng.
if(ZLIBNG_ENABLE_TESTS)
    add_test(NAME infcover COMMAND ${CMAKE_CROSSCOMPILING_EMULATOR} $<TARGET_FILE:infcover>)
endif()

add_executable(makefixed ${PROJECT_SOURCE_DIR}/tools/makefixed.c ${PROJECT_SOURCE_DIR}/inftrees.c)
configure_test_executable(makefixed)
set(MAKEFIXED_COMMAND ${CMAKE_CROSSCOMPILING_EMULATOR} $<TARGET_FILE:makefixed>)

add_executable(maketrees ${PROJECT_SOURCE_DIR}/tools/maketrees.c ${PROJECT_SOURCE_DIR}/trees.c ${PROJECT_SOURCE_DIR}/zutil.c)
configure_test_executable(maketrees)
set(MAKETREES_COMMAND ${CMAKE_CROSSCOMPILING_EMULATOR} $<TARGET_FILE:maketrees>)

add_executable(makecrct ${PROJECT_SOURCE_DIR}/tools/makecrct.c)
configure_test_executable(makecrct)
set(MAKECRCT_COMMAND ${CMAKE_CROSSCOMPILING_EMULATOR} $<TARGET_FILE:makecrct>)

# Emscripten does not support large amounts of data via stdin/out
# https://github.com/emscripten-core/emscripten/issues/16755#issuecomment-1102732849
if(NOT BASEARCH_WASM32_FOUND)
    # Runs tests targeting CVEs
    include(cmake/test-cves.cmake)

    # Run tests with data files
    include(cmake/test-data.cmake)

    # Run tests targeting GitHub issues
    include(cmake/test-issues.cmake)

    # Run tests targeting tools
    include(cmake/test-tools.cmake)
endif()

if(WITH_FUZZERS)
    add_subdirectory(fuzz)
endif()

if(WITH_GTEST OR WITH_BENCHMARKS)
    if(CMAKE_VERSION VERSION_LESS 3.12)
        message(WARNING "Minimum cmake version of 3.12 not met for Google benchmark!")
        set(WITH_BENCHMARKS OFF)
        set(WITH_BENCHMARKS OFF PARENT_SCOPE)
    endif()
    enable_language(CXX)
endif()

if(WITH_BENCHMARKS)
    add_subdirectory(benchmarks)
endif()

if(WITH_GTEST)
    # Google test requires at least C++11
    set(CMAKE_CXX_STANDARD 11)

    # Google test requires MSAN instrumented LLVM C++ libraries
    if(WITH_SANITIZER STREQUAL "Memory")
        if(NOT DEFINED ENV{LLVM_BUILD_DIR})
            message(FATAL_ERROR "MSAN instrumented C++ libraries required!")
        endif()

        # Must set include and compile options before fetching googletest
        include_directories($ENV{LLVM_BUILD_DIR}/include $ENV{LLVM_BUILD_DIR}/include/c++/v1)
        add_compile_options(-stdlib=libc++ -g)
    elseif(NOT TARGET GTest::GTest)
        find_package(GTest)
    endif()

    if(NOT TARGET GTest::GTest AND NOT CMAKE_VERSION VERSION_LESS 3.11)
        include(FetchContent)

        # Prevent overriding the parent project's compiler/linker settings for Windows
        set(gtest_force_shared_crt ON CACHE BOOL
            "Use shared (DLL) run-time lib even when Google Test is built as static lib." FORCE)

        # Allow specifying alternative Google test repository
        if(NOT DEFINED GTEST_REPOSITORY)
            set(GTEST_REPOSITORY https://github.com/google/googletest.git)
        endif()
        if(NOT DEFINED GTEST_TAG)
            # Use older version of Google test to support older versions of GCC
            if (CMAKE_CXX_COMPILER_ID MATCHES "GNU" AND CMAKE_CXX_COMPILER_VERSION VERSION_LESS_EQUAL 5.3)
                set(GTEST_TAG release-1.10.0)
            else()
                set(GTEST_TAG release-1.12.1)
            endif()
        endif()

        # Fetch Google test source code from official repository
        message(STATUS "Git checking out GoogleTest ${GTEST_TAG}")
        FetchContent_Declare(googletest
            GIT_REPOSITORY ${GTEST_REPOSITORY}
            GIT_TAG ${GTEST_TAG})

        FetchContent_GetProperties(googletest)
        if(NOT googletest_POPULATED)
            FetchContent_Populate(googletest)
            add_subdirectory(${googletest_SOURCE_DIR} ${googletest_BINARY_DIR} EXCLUDE_FROM_ALL)
        endif()
        add_library(GTest::GTest ALIAS gtest)
        add_library(GTest::Main ALIAS gtest_main)
    endif()

    if(TARGET GTest::GTest)
        set(TEST_SRCS
            test_compress.cc
            test_compress_bound.cc
            test_cve-2003-0107.cc
            test_deflate_bound.cc
            test_deflate_copy.cc
            test_deflate_dict.cc
            test_deflate_hash_head_0.cc
            test_deflate_header.cc
            test_deflate_params.cc
            test_deflate_pending.cc
            test_deflate_prime.cc
            test_deflate_quick_bi_valid.cc
            test_deflate_quick_block_open.cc
            test_deflate_tune.cc
            test_dict.cc
            test_inflate_adler32.cc
            test_large_buffers.cc
            test_raw.cc
            test_small_buffers.cc
            test_small_window.cc
            )

        if(WITH_GZFILEOP)
            list(APPEND TEST_SRCS test_gzio.cc)
        endif()

        if(ZLIBNG_ENABLE_TESTS)
            list(APPEND TEST_SRCS
                test_adler32.cc             # adler32_neon(), etc
                test_aligned_alloc.cc       # zng_alloc_aligned()
                test_compare256.cc          # compare256_neon(), etc
                test_crc32.cc               # crc32_acle(), etc
                test_inflate_sync.cc        # expects a certain compressed block layout
                test_main.cc                # cpu_check_features()
                test_version.cc             # expects a fixed version string
                )
        endif()

        add_executable(gtest_zlib ${TEST_SRCS})
        configure_test_executable(gtest_zlib)

        if(WITH_SANITIZER STREQUAL "Memory")
            target_link_directories(gtest_zlib PRIVATE $ENV{LLVM_BUILD_DIR}/lib)
            target_link_options(gtest_zlib PRIVATE
                -stdlib=libc++
                -lc++abi
                -fsanitize=memory
                -fsanitize-memory-track-origins)
        endif()

        if(NOT ZLIB_COMPAT AND DEFINED ZLIB_LIBRARIES AND DEFINED ZLIB_INCLUDE_DIRS)
            if(NOT IS_ABSOLUTE ${ZLIB_LIBRARIES})
                get_filename_component(ZLIB_ABSOLUTE_PATH
                    "${CMAKE_CURRENT_SOURCE_DIR}/${ZLIB_LIBRARIES}"
                    ABSOLUTE)
            else()
                set(ZLIB_ABSOLUTE_PATH ${ZLIB_LIBRARIES})
            endif()

            add_library(external_zlib STATIC IMPORTED)
            set_property(TARGET external_zlib PROPERTY IMPORTED_LOCATION ${ZLIB_ABSOLUTE_PATH})
            message(STATUS "Added dual linking tests against zlib")
            message(STATUS "  Zlib include dirs: ${ZLIB_INCLUDE_DIRS}")
            message(STATUS "  Zlib libraries: ${ZLIB_ABSOLUTE_PATH}")

            target_sources(gtest_zlib PRIVATE test_compress_dual.cc)
            target_include_directories(gtest_zlib PRIVATE ${ZLIB_INCLUDE_DIRS})
            target_link_libraries(gtest_zlib external_zlib)
        endif()

        if(NOT DEFINED BUILD_SHARED_LIBS)
            # Link statically in order to test internal zlib-ng functions.
            target_link_libraries(gtest_zlib zlibstatic)
        else()
            target_link_libraries(gtest_zlib zlib)
        endif()

        if(BUILD_SHARED_LIBS)
            target_link_libraries(gtest_zlib GTest::Main)
        endif()
        target_link_libraries(gtest_zlib GTest::GTest)

        find_package(Threads)
        if(Threads_FOUND AND NOT BASEARCH_WASM32_FOUND)
            target_sources(gtest_zlib PRIVATE test_deflate_concurrency.cc)
            if(UNIX AND NOT APPLE)
                # On Linux, use a workaround for https://gcc.gnu.org/bugzilla/show_bug.cgi?id=52590
                target_link_libraries(gtest_zlib -Wl,--whole-archive -lpthread -Wl,--no-whole-archive)
            endif()
            target_link_libraries(gtest_zlib Threads::Threads)
        endif()

        add_test(NAME gtest_zlib
            COMMAND ${CMAKE_CROSSCOMPILING_EMULATOR} $<TARGET_FILE:gtest_zlib>)
    endif()
endif()
