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
set(__CURRENT_BUILD_SHARED_LIBS ${BUILD_SHARED_LIBS})
set(BUILD_SHARED_LIBS OFF)
FetchContent_MakeAvailable(zstd)
set(BUILD_SHARED_LIBS ${__CURRENT_BUILD_SHARED_LIBS})

# Set up the variables to match the brotli pattern
set(ZSTD_INCLUDE_DIRS "${CMAKE_CURRENT_LIST_DIR}/zstd/lib")
set(ZSTD_LIBRARIES_CORE libzstd_static)
set(ZSTD_LIBRARIES ${ZSTD_LIBRARIES_CORE})
mark_as_advanced(ZSTD_INCLUDE_DIRS)
mark_as_advanced(ZSTD_LIBRARIES)

# disable warnings about lossy conversions
target_compile_options(libzstd_shared PRIVATE $<$<COMPILE_LANG_AND_ID:C,MSVC>:/wd4242>)
target_compile_options(libzstd_static PRIVATE $<$<COMPILE_LANG_AND_ID:C,MSVC>:/wd4242>)