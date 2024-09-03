include(FetchContent)

FetchContent_Declare(
    brotli
    SOURCE_DIR ${CMAKE_CURRENT_LIST_DIR}/brotli
)

set(BROTLI_DISABLE_TESTS ON)
set(__CURRENT_BUILD_SHARED_LIBS ${BUILD_SHARED_LIBS})
set(BUILD_SHARED_LIBS OFF)
FetchContent_MakeAvailable(brotli)
set(BUILD_SHARED_LIBS ${__CURRENT_BUILD_SHARED_LIBS})

target_compile_options(brotlicommon PRIVATE $<$<COMPILE_LANG_AND_ID:C,MSVC>:/guard:cf>)
target_compile_options(brotlienc PRIVATE $<$<COMPILE_LANG_AND_ID:C,MSVC>:/guard:cf>)
target_compile_options(brotlidec PRIVATE $<$<COMPILE_LANG_AND_ID:C,MSVC>:/guard:cf>)
target_compile_options(brotlienc PRIVATE $<$<COMPILE_LANG_AND_ID:C,Clang,AppleClang,GNU>:-Wno-implicit-fallthrough>)
target_compile_options(brotlidec PRIVATE $<$<COMPILE_LANG_AND_ID:C,Clang,AppleClang,GNU>:-Wno-implicit-fallthrough>)

# Even though we aren't building brotli as shared libraries, we still need to be able to export the symbols
# from the brotli libraries so that they can be used by System.IO.Compression.
target_compile_definitions(brotlicommon PRIVATE BROTLI_SHARED_COMPILATION)
target_compile_definitions(brotlienc PRIVATE BROTLI_SHARED_COMPILATION BROTLIENC_SHARED_COMPILATION)
target_compile_definitions(brotlidec PRIVATE BROTLI_SHARED_COMPILATION BROTLIDEC_SHARED_COMPILATION)
target_compile_definitions(brotlicommon PRIVATE BROTLI_SHARED_COMPILATION BROTLICOMMON_SHARED_COMPILATION)

# Don't build the brotli command line tool unless explicitly requested
# (which we never do)
set_target_properties(brotli PROPERTIES EXCLUDE_FROM_ALL ON)
