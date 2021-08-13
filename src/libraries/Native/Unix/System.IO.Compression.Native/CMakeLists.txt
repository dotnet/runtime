project(System.IO.Compression.Native C)

if (CLR_CMAKE_TARGET_BROWSER)
    add_definitions(-s USE_ZLIB)
endif()

include(${CMAKE_CURRENT_LIST_DIR}/extra_libs.cmake)

set(NATIVE_LIBS_EXTRA)
append_extra_compression_libs(NATIVE_LIBS_EXTRA)

set(NATIVECOMPRESSION_SOURCES
    ../../AnyOS/zlib/pal_zlib.c
)

if (NOT CLR_CMAKE_TARGET_BROWSER)
    #Include Brotli include files
    include_directories("../../AnyOS/brotli/include")

    set (NATIVECOMPRESSION_SOURCES
        ${NATIVECOMPRESSION_SOURCES}
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
        ../../AnyOS/System.IO.Compression.Native/entrypoints.c
    )

    # Disable implicit fallthrough warning for Brotli
    set_source_files_properties(${NATIVECOMPRESSION_SOURCES} PROPERTIES COMPILE_FLAGS -Wno-implicit-fallthrough)
endif()

if (GEN_SHARED_LIB)
    add_definitions(-DBROTLI_SHARED_COMPILATION)

    add_library(System.IO.Compression.Native
        SHARED
        ${NATIVECOMPRESSION_SOURCES}
        ${VERSION_FILE_PATH}
    )

    target_link_libraries(System.IO.Compression.Native
        ${NATIVE_LIBS_EXTRA}
    )

    if (NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS AND NOT CLR_CMAKE_TARGET_ANDROID)
        set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/System.IO.Compression.Native_unixexports.src)
        set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/System.IO.Compression.Native.exports)
        generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})
        set_exports_linker_option(${EXPORTS_FILE})

        add_custom_target(System.IO.Compression.Native_exports DEPENDS ${EXPORTS_FILE})
        add_dependencies(System.IO.Compression.Native System.IO.Compression.Native_exports)

        set_property(TARGET System.IO.Compression.Native APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
        set_property(TARGET System.IO.Compression.Native APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})

        add_custom_command(TARGET System.IO.Compression.Native POST_BUILD
            COMMENT "Verifying System.IO.Compression.Native entry points against entrypoints.c "
            COMMAND ${CMAKE_CURRENT_SOURCE_DIR}/../verify-entrypoints.sh
                $<TARGET_FILE:System.IO.Compression.Native>
                ${CMAKE_CURRENT_SOURCE_DIR}/../../AnyOS/System.IO.Compression.Native/entrypoints.c
                ${CMAKE_NM}
            VERBATIM
        )
    endif()

    install_with_stripped_symbols (System.IO.Compression.Native PROGRAMS .)
endif ()

add_library(System.IO.Compression.Native-Static
    STATIC
    ${NATIVECOMPRESSION_SOURCES}
)

set_target_properties(System.IO.Compression.Native-Static PROPERTIES OUTPUT_NAME System.IO.Compression.Native  CLEAN_DIRECT_OUTPUT 1)

install (TARGETS System.IO.Compression.Native-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
