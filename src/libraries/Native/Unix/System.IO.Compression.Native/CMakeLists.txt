project(System.IO.Compression.Native C)

find_package(ZLIB REQUIRED)

set(NATIVECOMPRESSION_SOURCES
    ../../AnyOS/zlib/pal_zlib.c
)

add_definitions(-DBROTLI_SHARED_COMPILATION)

#Include Brotli include files
include_directories("../../AnyOS/brotli/include")

set (NATIVECOMPRESSION_SOURCES
    ${NATIVECOMPRESSION_SOURCES}
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

add_library(System.IO.Compression.Native
    SHARED
    ${NATIVECOMPRESSION_SOURCES}
    ${VERSION_FILE_PATH}
)

add_library(System.IO.Compression.Native-Static
    STATIC
    ${NATIVECOMPRESSION_SOURCES}
    ${VERSION_FILE_PATH}
)

# Disable the "lib" prefix and override default name
set_target_properties(System.IO.Compression.Native-Static PROPERTIES PREFIX "")
set_target_properties(System.IO.Compression.Native-Static PROPERTIES OUTPUT_NAME System.IO.Compression.Native  CLEAN_DIRECT_OUTPUT 1)

target_link_libraries(System.IO.Compression.Native
    ${ZLIB_LIBRARIES}
)

install_library_and_symbols (System.IO.Compression.Native)
install (TARGETS System.IO.Compression.Native-Static DESTINATION .)
