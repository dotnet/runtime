# IMPORTANT: do not use add_compile_options(), add_definitions() or similar functions here since it will leak to the including projects

include(FetchContent)

FetchContent_Declare(
    zstd
    SOURCE_DIR ${CMAKE_CURRENT_LIST_DIR}/zstd/build/cmake
)

set(ZSTD_BUILD_PROGRAMS OFF)
set(ZSTD_BUILD_TESTS OFF)
set(ZSTD_BUILD_CONTRIB OFF)
set(ZSTD_BUILD_STATIC ON)
set(ZSTD_BUILD_SHARED OFF)

cmake_policy(SET CMP0194 OLD)

set(__CURRENT_BUILD_SHARED_LIBS ${BUILD_SHARED_LIBS})
set(BUILD_SHARED_LIBS OFF)
FetchContent_MakeAvailable(zstd)
set(BUILD_SHARED_LIBS ${__CURRENT_BUILD_SHARED_LIBS})

if (MSVC)
    # Make the static library export its symbols so that we can P/Invoke them from Managed code
    set_property(TARGET libzstd_static APPEND PROPERTY COMPILE_DEFINITIONS "ZSTD_DLL_EXPORT=1")
endif()

# disable warnings about lossy conversions
target_compile_options(libzstd_static PRIVATE $<$<COMPILE_LANG_AND_ID:C,MSVC>:/wd4242>)