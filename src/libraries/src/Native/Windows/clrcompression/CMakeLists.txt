project(clrcompression)

if("${CMAKE_SOURCE_DIR}" STREQUAL "${CMAKE_BINARY_DIR}")
    message(FATAL_ERROR "Binary directory isn't being correctly set before calling Cmake. Tree must be built in separate directory from source.")
endif()

set(CMAKE_INCLUDE_CURRENT_DIR ON)
include (GenerateExportHeader)

if($ENV{__BuildArch} STREQUAL x86 OR $ENV{__BuildArch} STREQUAL x64)
    set(NATIVECOMPRESSION_SOURCES
        zlib-intel/adler32.c
        zlib-intel/compress.c
        zlib-intel/crc_folding.c
        zlib-intel/crc32.c
        zlib-intel/deflate_medium.c
        zlib-intel/deflate_quick.c
        zlib-intel/deflate.c
        zlib-intel/inffast.c
        zlib-intel/inflate.c
        zlib-intel/inftrees.c
        zlib-intel/match.c
        zlib-intel/slide_sse.c
        zlib-intel/trees.c
        zlib-intel/x86.c
        zlib-intel/zutil.c
        ../../AnyOS/zlib/pal_zlib.c
)
elseif($ENV{__BuildArch} STREQUAL arm OR $ENV{__BuildArch} STREQUAL arm64)
    set(NATIVECOMPRESSION_SOURCES
        zlib/adler32.c
        zlib/compress.c
        zlib/crc32.c
        zlib/deflate.c
        zlib/inffast.c
        zlib/inflate.c
        zlib/inftrees.c
        zlib/trees.c
        zlib/zutil.c
        ../../AnyOS/zlib/pal_zlib.c
    )
endif()

if($ENV{__appContainer} STREQUAL true)
    set (NATIVECOMPRESSION_SOURCES
        ${NATIVECOMPRESSION_SOURCES}
        clrcompression.Uap.def
    )
else()
    set (NATIVECOMPRESSION_SOURCES
        ${NATIVECOMPRESSION_SOURCES}
        clrcompression.def
        ../../AnyOS/brotli/common/dictionary.c
        ../../AnyOS/brotli/common/transform.c
        ../../AnyOS/brotli/dec/bit_reader.c
        ../../AnyOS/brotli/dec/decode.c
        ../../AnyOS/brotli/dec/huffman.c
        ../../AnyOS/brotli/dec/state.c
        ../../AnyOS/brotli/enc/backward_references.c
        ../../AnyOS/brotli/enc/backward_references_hq.c
        ../../AnyOS/brotli/enc/bit_cost.c
        ../../AnyOS/brotli/enc/block_splitter.c
        ../../AnyOS/brotli/enc/brotli_bit_stream.c
        ../../AnyOS/brotli/enc/cluster.c
        ../../AnyOS/brotli/enc/compress_fragment.c
        ../../AnyOS/brotli/enc/compress_fragment_two_pass.c
        ../../AnyOS/brotli/enc/dictionary_hash.c
        ../../AnyOS/brotli/enc/encode.c
        ../../AnyOS/brotli/enc/encoder_dict.c
        ../../AnyOS/brotli/enc/entropy_encode.c
        ../../AnyOS/brotli/enc/histogram.c
        ../../AnyOS/brotli/enc/literal_cost.c
        ../../AnyOS/brotli/enc/memory.c
        ../../AnyOS/brotli/enc/metablock.c
        ../../AnyOS/brotli/enc/static_dict.c
        ../../AnyOS/brotli/enc/utf8_util.c
    )
endif()

#Include Brotli include files
include_directories("../../AnyOS/brotli/include")

add_library(clrcompression
    SHARED
    ${NATIVECOMPRESSION_SOURCES}
    # This will add versioning to the library
    ${CMAKE_SOURCE_DIR}/../../../artifacts/obj/NativeVersion.rc
)

# Allow specification of arguments that should be passed to the linker
SET_TARGET_PROPERTIES(clrcompression PROPERTIES LINK_FLAGS ${__LinkArgs})

# Allow specification of libraries that should be linked against
# CMake doesn't like space delimiters as input to target_link_libraries
separate_arguments(linker_libs_sanitized WINDOWS_COMMAND ${__LinkLibraries})
target_link_libraries(clrcompression ${linker_libs_sanitized})

GENERATE_EXPORT_HEADER( clrcompression
     BASE_NAME clrcompression
     EXPORT_MACRO_NAME clrcompression_EXPORT
     EXPORT_FILE_NAME clrcompression_Export.h
     STATIC_DEFINE clrcompression_BUILT_AS_STATIC
)

install (TARGETS clrcompression DESTINATION .)
install (FILES ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_BUILD_TYPE}/clrcompression.pdb DESTINATION .)

