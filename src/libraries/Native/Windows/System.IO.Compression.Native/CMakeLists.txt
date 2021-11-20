project(System.IO.Compression.Native)

if("${CMAKE_SOURCE_DIR}" STREQUAL "${CMAKE_BINARY_DIR}")
    message(FATAL_ERROR "Binary directory isn't being correctly set before calling Cmake. Tree must be built in separate directory from source.")
endif()

set(CMAKE_INCLUDE_CURRENT_DIR ON)

if (GEN_SHARED_LIB)
    include (GenerateExportHeader)
endif()

if(${CLR_CMAKE_HOST_ARCH} STREQUAL arm OR ${CLR_CMAKE_HOST_ARCH} STREQUAL arm64)
    set(NATIVECOMPRESSION_SOURCES
        zlib-ng/adler32.c
        zlib-ng/chunkset.c
        zlib-ng/compare258.c
        zlib-ng/compress.c
        zlib-ng/crc32_comb.c
        zlib-ng/crc32.c
        zlib-ng/deflate_fast.c
        zlib-ng/deflate_medium.c
        zlib-ng/deflate_quick.c
        zlib-ng/deflate_slow.c
        zlib-ng/deflate.c
        zlib-ng/functable.c
        # Exclude files to fix error LNK2019: unresolved external symbol 'gzFile' open referenced in functions gz_open, gz_close, gz_load, gz_comp
        # zlib-ng/gzlib.c
        # zlib-ng/gzread.c
        # zlib-ng/gzwrite.c
        zlib-ng/infback.c
        zlib-ng/inffast.c
        zlib-ng/inflate.c
        zlib-ng/inftrees.c
        zlib-ng/insert_string.c
        zlib-ng/trees.c
        zlib-ng/uncompr.c
        zlib-ng/zutil.c
    )
elseif(${CLR_CMAKE_HOST_ARCH} STREQUAL x86 OR ${CLR_CMAKE_HOST_ARCH} STREQUAL x64)
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
    )
else()
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
    )
endif()

set(NATIVECOMPRESSION_SOURCES
    ${NATIVECOMPRESSION_SOURCES}
    ../../AnyOS/zlib/pal_zlib.c
    ../../AnyOS/brotli/common/constants.c
    ../../AnyOS/brotli/common/context.c
    ../../AnyOS/brotli/common/dictionary.c
    ../../AnyOS/brotli/common/platform.c
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
    ../../AnyOS/brotli/enc/command.c
    ../../AnyOS/brotli/enc/compress_fragment.c
    ../../AnyOS/brotli/enc/compress_fragment_two_pass.c
    ../../AnyOS/brotli/enc/dictionary_hash.c
    ../../AnyOS/brotli/enc/encode.c
    ../../AnyOS/brotli/enc/encoder_dict.c
    ../../AnyOS/brotli/enc/entropy_encode.c
    ../../AnyOS/brotli/enc/fast_log.c
    ../../AnyOS/brotli/enc/histogram.c
    ../../AnyOS/brotli/enc/literal_cost.c
    ../../AnyOS/brotli/enc/memory.c
    ../../AnyOS/brotli/enc/metablock.c
    ../../AnyOS/brotli/enc/static_dict.c
    ../../AnyOS/brotli/enc/utf8_util.c
)

#Include Brotli include files
include_directories("../../AnyOS/brotli/include")

if (GEN_SHARED_LIB)
    add_library(System.IO.Compression.Native
        SHARED
        ${NATIVECOMPRESSION_SOURCES}
        System.IO.Compression.Native.def
        # This will add versioning to the library
        ${VERSION_FILE_RC_PATH}
    )
endif()

if (NOT GEN_SHARED_LIB AND NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS AND NOT CLR_CMAKE_TARGET_ANDROID AND NOT CLR_CMAKE_TARGET_BROWSER)
    set(NATIVECOMPRESSION_SOURCES ${NATIVECOMPRESSION_SOURCES} ../../AnyOS/System.IO.Compression.Native/entrypoints.c)
endif()

add_library(System.IO.Compression.Native-Static
    STATIC
    ${NATIVECOMPRESSION_SOURCES}
)

# Allow specification of arguments that should be passed to the linker
if (GEN_SHARED_LIB)
    SET_TARGET_PROPERTIES(System.IO.Compression.Native PROPERTIES LINK_OPTIONS "${__LinkArgs};${__SharedLinkArgs}")
endif()
SET_TARGET_PROPERTIES(System.IO.Compression.Native-Static PROPERTIES STATIC_LIBRARY_OPTIONS "${__LinkArgs}")

# Allow specification of libraries that should be linked against
if (GEN_SHARED_LIB)
    target_link_libraries(System.IO.Compression.Native ${__LinkLibraries})
endif()
target_link_libraries(System.IO.Compression.Native-Static ${__LinkLibraries})

if (GEN_SHARED_LIB)
    GENERATE_EXPORT_HEADER( System.IO.Compression.Native
         BASE_NAME System.IO.Compression.Native
         EXPORT_MACRO_NAME System.IO.Compression.Native_EXPORT
         EXPORT_FILE_NAME System.IO.Compression.Native_Export.h
         STATIC_DEFINE System.IO.Compression.Native_BUILT_AS_STATIC
    )

    install (TARGETS System.IO.Compression.Native DESTINATION .)
    install (FILES $<TARGET_PDB_FILE:System.IO.Compression.Native> DESTINATION .)
endif()

install (TARGETS System.IO.Compression.Native-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
