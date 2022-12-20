include_directories(BEFORE "${CMAKE_CURRENT_LIST_DIR}/brotli/include")

set (BROTLI_SOURCES_BASE
    common/constants.c
    common/context.c
    common/dictionary.c
    common/platform.c
    common/transform.c
    dec/bit_reader.c
    dec/decode.c
    dec/huffman.c
    dec/state.c
    enc/backward_references.c
    enc/backward_references_hq.c
    enc/bit_cost.c
    enc/block_splitter.c
    enc/brotli_bit_stream.c
    enc/cluster.c
    enc/command.c
    enc/compress_fragment.c
    enc/compress_fragment_two_pass.c
    enc/dictionary_hash.c
    enc/encode.c
    enc/encoder_dict.c
    enc/entropy_encode.c
    enc/fast_log.c
    enc/histogram.c
    enc/literal_cost.c
    enc/memory.c
    enc/metablock.c
    enc/static_dict.c
    enc/utf8_util.c
)

addprefix(BROTLI_SOURCES "${CMAKE_CURRENT_LIST_DIR}/brotli" "${BROTLI_SOURCES_BASE}")
