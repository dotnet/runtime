include(FetchContent)

FetchContent_Declare(
    fetchzlibng
    SOURCE_DIR "${CMAKE_CURRENT_LIST_DIR}/zlib-ng")

set(ZLIB_COMPAT ON)
set(ZLIB_ENABLE_TESTS OFF)
set(ZLIBNG_ENABLE_TESTS OFF)
set(Z_PREFIX ON)

add_compile_options($<$<COMPILE_LANG_AND_ID:C,Clang,AppleClang,GNU>:-Wno-error=unused-command-line-argument>) # clang : error : argument unused during compilation: '-fno-semantic-interposition'
add_compile_options($<$<COMPILE_LANG_AND_ID:C,MSVC>:/wd4127>) # warning C4127: conditional expression is constant
add_compile_options($<$<COMPILE_LANG_AND_ID:C,MSVC>:/wd4242>) # 'function': conversion from 'unsigned int' to 'Pos', possible loss of data, in various deflate_*.c files
add_compile_options($<$<COMPILE_LANG_AND_ID:C,MSVC>:/wd4244>) # 'function': conversion from 'unsigned int' to 'Pos', possible loss of data, in various deflate_*.c files

# TODO: DO we still need to disable /wd4131 ?

set(SKIP_INSTALL_ALL ON)
FetchContent_MakeAvailable(fetchzlibng)
set(SKIP_INSTALL_ALL OFF)

set_property(DIRECTORY ${CMAKE_CURRENT_LIST_DIR}/zlib-ng PROPERTY MSVC_WARNING_LEVEL 3) # Set the MSVC warning level for all zlib-ng targets to 3.

target_compile_options(zlibstatic PRIVATE $<$<COMPILE_LANG_AND_ID:C,Clang,AppleClang,GNU>:-Wno-error=unused-command-line-argument>) # Make sure MacOS respects ignoring unused CLI arguments
target_compile_options(zlibstatic PRIVATE $<$<COMPILE_LANG_AND_ID:C,MSVC>:/guard:cf>) # Enable CFG always for zlib-ng so we don't need to build two flavors.
