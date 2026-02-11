# IMPORTANT: do not use add_compile_options(), add_definitions() or similar functions here since it will leak to the including projects

include(FetchContent)

FetchContent_Declare(
    zstd
    SOURCE_DIR ${CMAKE_CURRENT_LIST_DIR}/zstd/build/cmake
    EXCLUDE_FROM_ALL
)

set(ZSTD_BUILD_PROGRAMS OFF)
set(ZSTD_BUILD_TESTS OFF)
set(ZSTD_BUILD_CONTRIB OFF)
set(ZSTD_BUILD_STATIC ON)
set(ZSTD_BUILD_SHARED OFF)

set(ZSTD_MULTITHREAD_SUPPORT OFF)
set(ZSTD_LEGACY_SUPPORT OFF)

# Pre-cache zstd CMake checks to speed up configure for browser
if (CLR_CMAKE_TARGET_BROWSER)
  set(C_FLAG_WALL 1 CACHE INTERNAL "")
  set(CXX_FLAG_WALL 1 CACHE INTERNAL "")
  set(C_FLAG_WEXTRA 1 CACHE INTERNAL "")
  set(CXX_FLAG_WEXTRA 1 CACHE INTERNAL "")
  set(C_FLAG_WUNDEF 1 CACHE INTERNAL "")
  set(CXX_FLAG_WUNDEF 1 CACHE INTERNAL "")
  set(C_FLAG_WSHADOW 1 CACHE INTERNAL "")
  set(CXX_FLAG_WSHADOW 1 CACHE INTERNAL "")
  set(C_FLAG_WCAST_ALIGN 1 CACHE INTERNAL "")
  set(CXX_FLAG_WCAST_ALIGN 1 CACHE INTERNAL "")
  set(C_FLAG_WCAST_QUAL 1 CACHE INTERNAL "")
  set(CXX_FLAG_WCAST_QUAL 1 CACHE INTERNAL "")
  set(C_FLAG_WSTRICT_PROTOTYPES 1 CACHE INTERNAL "")
  set(LD_FLAG_WL_Z_NOEXECSTACK 1 CACHE INTERNAL "")
  set(C_FLAG_QUNUSED_ARGUMENTS 1 CACHE INTERNAL "")
  set(CXX_FLAG_QUNUSED_ARGUMENTS 1 CACHE INTERNAL "")
  set(C_FLAG_WA_NOEXECSTACK 1 CACHE INTERNAL "")
  set(CXX_FLAG_WA_NOEXECSTACK 1 CACHE INTERNAL "")
endif()

set(__CURRENT_BUILD_SHARED_LIBS ${BUILD_SHARED_LIBS})
set(BUILD_SHARED_LIBS OFF)
FetchContent_MakeAvailable(zstd)
set(BUILD_SHARED_LIBS ${__CURRENT_BUILD_SHARED_LIBS})

if (ANDROID)
    # qsort_r is not available during android build and zstd's autodetection does not seem
    # to handle this case correctly
    # This should no longer be needed once we update to zstd 1.5.8 in the future
    set_property(TARGET libzstd_static APPEND PROPERTY COMPILE_DEFINITIONS "ZSTD_USE_C90_QSORT=1")
endif()

# disable warnings that occur in the zstd library
target_compile_options(libzstd_static PRIVATE $<$<COMPILE_LANG_AND_ID:C,Clang,AppleClang,GNU>:-Wno-implicit-fallthrough>)