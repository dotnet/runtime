 # IMPORTANT: do not use add_compile_options(), add_definitions() or similar functions here since it will leak to the including projects

include(FetchContent)

FetchContent_Declare(
    fetchzlibng
    SOURCE_DIR "${CMAKE_CURRENT_LIST_DIR}/zlib-ng")

set(ZLIB_COMPAT ON)
set(ZLIB_ENABLE_TESTS OFF)
set(ZLIBNG_ENABLE_TESTS OFF)
set(Z_PREFIX ON)

# TODO: Turn back on when Linux kernels with proper RISC-V extension detection (>= 6.5) are more commonplace
set(WITH_RVV OFF)
# We don't support ARMv6 and the check works incorrectly when compiling for ARMv7 w/ Thumb instruction set
set(WITH_ARMV6 OFF)
# The checks for NEON_AVAILABLE and NEON_HAS_LD4 work incorrectly when compiling for arm32.
if(CLR_CMAKE_TARGET_ARCH_ARM AND CLR_CMAKE_TARGET_LINUX)
    set(WITH_NEON OFF)
endif()

if (CLR_CMAKE_TARGET_BROWSER OR CLR_CMAKE_TARGET_WASI)
  # 'aligned_alloc' is not available in browser/wasi, yet it is set by zlib-ng/CMakeLists.txt.
  set(HAVE_ALIGNED_ALLOC FALSE CACHE BOOL "have aligned_alloc" FORCE)

  # zlib-ng uses atomics, so we need to enable threads when requested for browser/wasi, otherwise the wasm target won't have thread support.
  if (CMAKE_USE_PTHREADS)
      add_compile_options(-pthread)
      add_linker_flag(-pthread)
  endif()
endif()

if (MSVC)
  #zlib-ng sets /utf-8 which clashes with /source-charset:utf-8 that we set centrally
  get_directory_property(dirCompileOptions COMPILE_OPTIONS)
  string(REPLACE "/source-charset:utf-8" "" dirCompileOptions "${dirCompileOptions}")
  set_directory_properties(PROPERTIES COMPILE_OPTIONS "${dirCompileOptions}")
endif()

set(BUILD_SHARED_LIBS OFF) # Shared libraries aren't supported in wasm
set(SKIP_INSTALL_ALL ON)
FetchContent_MakeAvailable(fetchzlibng)
set(SKIP_INSTALL_ALL OFF)

set_property(DIRECTORY ${CMAKE_CURRENT_LIST_DIR}/zlib-ng PROPERTY MSVC_WARNING_LEVEL 3) # Set the MSVC warning level for all zlib-ng targets to 3.
target_compile_options(zlib PRIVATE $<$<COMPILE_LANG_AND_ID:C,Clang,AppleClang>:-Wno-unused-command-line-argument>) # Make sure MacOS respects ignoring unused CLI arguments
target_compile_options(zlib PRIVATE $<$<COMPILE_LANG_AND_ID:C,Clang,AppleClang>:-Wno-logical-op-parentheses>) # place parentheses around the '&&' expression to silence this warning
target_compile_options(zlib PRIVATE $<$<COMPILE_LANG_AND_ID:C,MSVC>:/wd4127>) # warning C4127: conditional expression is constant
target_compile_options(zlib PRIVATE $<$<COMPILE_LANG_AND_ID:C,MSVC>:/guard:cf>) # Enable CFG always for zlib-ng so we don't need to build two flavors.

set_target_properties(zlib PROPERTIES DEBUG_POSTFIX "") # Workaround: zlib's debug lib name is zlibd.lib
