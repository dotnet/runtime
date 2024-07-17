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

add_compile_options($<$<COMPILE_LANG_AND_ID:C,Clang,AppleClang>:-Wno-unused-command-line-argument>) # clang : error : argument unused during compilation: '-fno-semantic-interposition'
add_compile_options($<$<COMPILE_LANG_AND_ID:C,Clang,AppleClang>:-Wno-logical-op-parentheses>) # place parentheses around the '&&' expression to silence this warning
add_compile_options($<$<COMPILE_LANG_AND_ID:C,MSVC>:/wd4127>) # warning C4127: conditional expression is constant
add_compile_options($<$<COMPILE_LANG_AND_ID:C,MSVC>:/wd4242>) # 'function': conversion from 'unsigned int' to 'Pos', possible loss of data, in various deflate_*.c files
add_compile_options($<$<COMPILE_LANG_AND_ID:C,MSVC>:/wd4244>) # 'function': conversion from 'unsigned int' to 'Pos', possible loss of data, in various deflate_*.c files

# 'aligned_alloc' is not available in browser/wasi, yet it is set by zlib-ng/CMakeLists.txt.
if (CLR_CMAKE_TARGET_BROWSER OR CLR_CMAKE_TARGET_WASI)
  set(HAVE_ALIGNED_ALLOC FALSE CACHE BOOL "have aligned_alloc" FORCE)
endif()

set(BUILD_SHARED_LIBS OFF) # Shared libraries aren't supported in wasm
set(SKIP_INSTALL_ALL ON)
FetchContent_MakeAvailable(fetchzlibng)
set(SKIP_INSTALL_ALL OFF)

set_property(DIRECTORY ${CMAKE_CURRENT_LIST_DIR}/zlib-ng PROPERTY MSVC_WARNING_LEVEL 3) # Set the MSVC warning level for all zlib-ng targets to 3.
target_compile_options(zlib PRIVATE $<$<COMPILE_LANG_AND_ID:C,Clang,AppleClang>:-Wno-unused-command-line-argument>) # Make sure MacOS respects ignoring unused CLI arguments
target_compile_options(zlib PRIVATE $<$<COMPILE_LANG_AND_ID:C,Clang,AppleClang>:-Wno-logical-op-parentheses>) # place parentheses around the '&&' expression to silence this warning
target_compile_options(zlib PRIVATE $<$<COMPILE_LANG_AND_ID:C,MSVC>:/guard:cf>) # Enable CFG always for zlib-ng so we don't need to build two flavors.

set_target_properties(zlib PROPERTIES DEBUG_POSTFIX "") # Workaround: zlib's debug lib name is zlibd.lib
