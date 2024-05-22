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
add_compile_options($<$<COMPILE_LANG_AND_ID:C,MSVC>:/W3>) # D9025: overriding '/W4' with '/W3'

# TODO: DO we still need to disable /wd4131 ?

set(SKIP_INSTALL_ALL ON)
FetchContent_MakeAvailable(fetchzlibng)
set(SKIP_INSTALL_ALL OFF)
