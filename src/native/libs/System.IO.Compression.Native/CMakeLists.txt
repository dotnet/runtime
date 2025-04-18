project(System.IO.Compression.Native C)

find_library(BROTLIDEC brotlidec REQUIRED)
find_library(BROTLIENC brotlienc REQUIRED)
find_library(BROTLICOMMON brotlicommon)
find_path(BROTLI_INCLUDE_DIRS "brotli/decode.h")

set(BROTLI_LIBRARIES ${BROTLIDEC} ${BROTLIENC})
if (BROTLICOMMON)
    list(APPEND BROTLI_LIBRARIES ${BROTLICOMMON})
endif()

if (NOT CLR_CMAKE_USE_SYSTEM_ZLIB)
    set(ZLIB_USE_STATIC_LIBS ON)
endif()
find_package(ZLIB REQUIRED)

configure_file(
    ${CMAKE_CURRENT_SOURCE_DIR}/../Common/pal_config.h.in
    ${CMAKE_CURRENT_BINARY_DIR}/pal_config.h)

set(NATIVECOMPRESSION_SOURCES
    pal_zlib.c
)

if (NOT CLR_CMAKE_TARGET_BROWSER AND NOT CLR_CMAKE_TARGET_WASI)
    set (NATIVECOMPRESSION_SOURCES
        ${NATIVECOMPRESSION_SOURCES}
        entrypoints.c
    )
endif ()

if (CLR_CMAKE_TARGET_UNIX OR CLR_CMAKE_TARGET_BROWSER OR CLR_CMAKE_TARGET_WASI)
    # Disable implicit fallthrough warning for Zlib and Brotli
    set(FLAGS -Wno-implicit-fallthrough)

    set_source_files_properties(${NATIVECOMPRESSION_SOURCES} PROPERTIES COMPILE_FLAGS ${FLAGS})

    if (GEN_SHARED_LIB)
        add_definitions(-DBROTLI_SHARED_COMPILATION)

        add_library(System.IO.Compression.Native
            SHARED
            ${NATIVECOMPRESSION_SOURCES}
            ${VERSION_FILE_PATH}
        )

        target_link_libraries(System.IO.Compression.Native
            PUBLIC
            ZLIB::ZLIB
        )

        target_include_directories(System.IO.Compression.Native PRIVATE ${BROTLI_INCLUDE_DIRS})
        target_link_libraries(System.IO.Compression.Native PRIVATE ${BROTLI_LIBRARIES})

        if (NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS AND NOT CLR_CMAKE_TARGET_ANDROID AND NOT CLR_CMAKE_USE_SYSTEM_BROTLI)
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

    target_link_libraries(System.IO.Compression.Native-Static
        PUBLIC
        ZLIB::ZLIB)

    set_target_properties(System.IO.Compression.Native-Static PROPERTIES OUTPUT_NAME System.IO.Compression.Native CLEAN_DIRECT_OUTPUT 1)

    target_include_directories(System.IO.Compression.Native-Static PUBLIC ${BROTLI_INCLUDE_DIRS})
    target_link_libraries(System.IO.Compression.Native-Static PUBLIC ${BROTLI_LIBRARIES})
else ()

    if (GEN_SHARED_LIB)
        include (GenerateExportHeader)
    endif ()

    if (GEN_SHARED_LIB)
        add_definitions(-DVER_FILEDESCRIPTION_STR="System.IO.Compression.Native")
        add_library(System.IO.Compression.Native
            SHARED
            ${NATIVECOMPRESSION_SOURCES}
            System.IO.Compression.Native.def
            ${VERSION_FILE_RC_PATH}
        )

        target_link_libraries(System.IO.Compression.Native-Static
            PUBLIC
            ZLIB::ZLIB)

        target_include_directories(System.IO.Compression.Native PUBLIC ${BROTLI_INCLUDE_DIRS})
        target_link_libraries(System.IO.Compression.Native PUBLIC ${BROTLI_LIBRARIES})
    endif ()

    if (NOT GEN_SHARED_LIB AND NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS AND NOT CLR_CMAKE_TARGET_ANDROID AND NOT CLR_CMAKE_TARGET_BROWSER AND NOT CLR_CMAKE_TARGET_WASI)
        set(NATIVECOMPRESSION_SOURCES ${NATIVECOMPRESSION_SOURCES} entrypoints.c)
    endif ()

    add_library(System.IO.Compression.Native-Static
        STATIC
        ${NATIVECOMPRESSION_SOURCES}
    )

    target_link_libraries(System.IO.Compression.Native-Static
        PUBLIC
        ZLIB::ZLIB)
    target_include_directories(System.IO.Compression.Native-Static PUBLIC ${BROTLI_INCLUDE_DIRS})
    target_link_libraries(System.IO.Compression.Native-Static PUBLIC ${BROTLI_LIBRARIES})

    if(STATIC_LIBS_ONLY)
        add_library(System.IO.Compression.Native.Aot
            STATIC
            ${NATIVECOMPRESSION_SOURCES}
        )

        target_link_libraries(System.IO.Compression.Native-Static
            PUBLIC
            ZLIB::ZLIB)

        target_include_directories(System.IO.Compression.Native.Aot PUBLIC ${BROTLI_INCLUDE_DIRS})
        target_link_libraries(System.IO.Compression.Native.Aot PUBLIC ${BROTLI_LIBRARIES})
        set_target_properties(System.IO.Compression.Native.Aot PROPERTIES INTERPROCEDURAL_OPTIMIZATION OFF)
    endif()

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

    if(STATIC_LIBS_ONLY)
        install_static_library(System.IO.Compression.Native.Aot aotsdk nativeaot)
        foreach(BROTLI_LIB ${BROTLI_LIBRARIES_NOIPO})
            install (PROGRAMS ${BROTLI_LIB} DESTINATION aotsdk COMPONENT nativeaot)
        endforeach()
        if (${ZLIB_NOIPO})
            install (PROGRAMS ${ZLIB_NOIPO} RENAME zlibstatic.lib DESTINATION aotsdk COMPONENT nativeaot)
        endif()
    endif()

endif ()

install (TARGETS System.IO.Compression.Native-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
