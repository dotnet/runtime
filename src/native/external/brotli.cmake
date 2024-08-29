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
