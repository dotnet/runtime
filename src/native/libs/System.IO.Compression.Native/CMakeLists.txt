project(System.IO.Compression.Native C)

include(${CMAKE_CURRENT_LIST_DIR}/extra_libs.cmake)

set(NATIVECOMPRESSION_SOURCES
    pal_zlib.c
)

if (NOT CLR_CMAKE_TARGET_BROWSER)
    include(${CLR_SRC_NATIVE_DIR}/external/brotli.cmake)

    set (NATIVECOMPRESSION_SOURCES
        ${NATIVECOMPRESSION_SOURCES}
        ${BROTLI_SOURCES}
        entrypoints.c
    )
endif ()

if (CLR_CMAKE_TARGET_UNIX OR CLR_CMAKE_TARGET_BROWSER)
    set(NATIVE_LIBS_EXTRA)
    append_extra_compression_libs(NATIVE_LIBS_EXTRA)

    if (CLR_CMAKE_TARGET_BROWSER)
        add_definitions(-s USE_ZLIB)
    else ()
        # Disable implicit fallthrough warning for Brotli
        set(FLAGS -Wno-implicit-fallthrough)

        # Delete this supression once brotli is upgraded to vNext (current latest v1.0.9
        # does not contain upstream fix: https://github.com/google/brotli/commit/0a3944c)
        set(FLAGS "${FLAGS} -Wno-vla-parameter")

        set_source_files_properties(${NATIVECOMPRESSION_SOURCES} PROPERTIES COMPILE_FLAGS ${FLAGS})
    endif ()

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
                    ${CMAKE_CURRENT_SOURCE_DIR}/entrypoints.c
                    ${CMAKE_NM}
                VERBATIM
            )
        endif ()

        install_with_stripped_symbols (System.IO.Compression.Native PROGRAMS .)
    endif ()

    add_library(System.IO.Compression.Native-Static
        STATIC
        ${NATIVECOMPRESSION_SOURCES}
    )

    set_target_properties(System.IO.Compression.Native-Static PROPERTIES OUTPUT_NAME System.IO.Compression.Native CLEAN_DIRECT_OUTPUT 1)
else ()
    if (GEN_SHARED_LIB)
        include (GenerateExportHeader)
    endif ()

    if (CLR_CMAKE_HOST_ARCH_I386 OR CLR_CMAKE_HOST_ARCH_AMD64)
        set(ZLIB_SOURCES
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
    else ()
        include(${CLR_SRC_NATIVE_DIR}/external/zlib.cmake)
    endif ()

    set(NATIVECOMPRESSION_SOURCES ${ZLIB_SOURCES} ${NATIVECOMPRESSION_SOURCES})

    if (GEN_SHARED_LIB)
        add_library(System.IO.Compression.Native
            SHARED
            ${NATIVECOMPRESSION_SOURCES}
            System.IO.Compression.Native.def
            # This will add versioning to the library
            ${VERSION_FILE_RC_PATH}
        )
    endif ()

    if (NOT GEN_SHARED_LIB AND NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS AND NOT CLR_CMAKE_TARGET_ANDROID AND NOT CLR_CMAKE_TARGET_BROWSER)
        set(NATIVECOMPRESSION_SOURCES ${NATIVECOMPRESSION_SOURCES} entrypoints.c)
    endif ()

    add_library(System.IO.Compression.Native-Static
        STATIC
        ${NATIVECOMPRESSION_SOURCES}
    )

    # Allow specification of libraries that should be linked against
    if (GEN_SHARED_LIB)
        target_link_libraries(System.IO.Compression.Native ${__LinkLibraries})
    endif ()
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
    endif ()
endif ()

install (TARGETS System.IO.Compression.Native-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
