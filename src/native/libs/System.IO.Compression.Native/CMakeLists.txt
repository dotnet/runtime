project(System.IO.Compression.Native C)

include(${CMAKE_CURRENT_LIST_DIR}/extra_libs.cmake)

if (NOT CLR_CMAKE_USE_SYSTEM_ZLIB)
    include(${CLR_SRC_NATIVE_DIR}/external/zlib-ng.cmake)
endif()

if (CLR_CMAKE_USE_SYSTEM_BROTLI)
    find_library(BROTLIDEC brotlidec REQUIRED)
    find_library(BROTLIENC brotlienc REQUIRED)
    list(APPEND ${BROTLI_LIBRARIES} ${BROTLIDEC} ${BROTLIENC})

    if (CLR_CMAKE_HOST_FREEBSD)
        set(CMAKE_REQUIRED_INCLUDES ${CROSS_ROOTFS}/usr/local/include)
    endif()
else()
    include(${CLR_SRC_NATIVE_DIR}/external/brotli.cmake)
endif()

if (STATIC_LIBS_ONLY)
    # For every vendored library that we're actually vendoring (and not referencing the system one)
    # mark it as "no interprocedural optimization" so that it's compatible with our NativeAOT shipping story.
    foreach(VENDORED_LIB IN LISTS BROTLI_LIBRARIES ITEMS zlib)
        if (TARGET ${VENDORED_LIB})
            set_target_properties(${VENDORED_LIB} PROPERTIES INTERPROCEDURAL_OPTIMIZATION OFF)
        endif()
    endforeach()
endif()

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
    set(NATIVE_LIBS_EXTRA)
    append_extra_compression_libs(NATIVE_LIBS_EXTRA)

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
            PRIVATE
            ${NATIVE_LIBS_EXTRA}
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

    if (NOT CLR_CMAKE_USE_SYSTEM_ZLIB)
        target_link_libraries(System.IO.Compression.Native-Static PRIVATE zlibstatic)
    endif()

    set_target_properties(System.IO.Compression.Native-Static PROPERTIES OUTPUT_NAME System.IO.Compression.Native CLEAN_DIRECT_OUTPUT 1)
    target_link_libraries(System.IO.Compression.Native-Static
        PUBLIC
        ${NATIVE_LIBS_EXTRA}
    )

    target_include_directories(System.IO.Compression.Native-Static PUBLIC ${BROTLI_INCLUDE_DIRS})
    target_link_libraries(System.IO.Compression.Native-Static PUBLIC ${BROTLI_LIBRARIES})

    foreach(BROTLI_LIB ${BROTLI_LIBRARIES})
        # Brotli's build scripts can add some system dependencies like libm
        # to BROTLI_LIBRARIES. Only install the libraries that are actually
        # defined as CMake targets.
        if (TARGET "${BROTLI_LIB}")
            install (TARGETS ${BROTLI_LIB} DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
        endif()
    endforeach(BROTLI_LIB ${BROTLI_LIBRARIES})

    if (NOT CLR_CMAKE_USE_SYSTEM_ZLIB)
        install (TARGETS zlib DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
    endif()
else ()
    set(NATIVE_LIBS_EXTRA)
    append_extra_compression_libs(NATIVE_LIBS_EXTRA)

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

        if (NOT CLR_CMAKE_USE_SYSTEM_ZLIB)
            target_link_libraries(System.IO.Compression.Native PRIVATE zlib)
        endif()

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

    if (NOT CLR_CMAKE_USE_SYSTEM_ZLIB)
        target_link_libraries(System.IO.Compression.Native-Static PRIVATE zlibstatic)
    endif()

    target_include_directories(System.IO.Compression.Native-Static PUBLIC ${BROTLI_INCLUDE_DIRS})
    target_link_libraries(System.IO.Compression.Native-Static PUBLIC ${BROTLI_LIBRARIES})

    if(STATIC_LIBS_ONLY)
        add_library(System.IO.Compression.Native.Aot
            STATIC
            ${NATIVECOMPRESSION_SOURCES}
        )

        if (NOT CLR_CMAKE_USE_SYSTEM_ZLIB)
            target_link_libraries(System.IO.Compression.Native.Aot PRIVATE zlibstatic)
        endif()

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
        foreach(BROTLI_LIB ${BROTLI_LIBRARIES})
            # Brotli's build scripts can add some system dependencies like libm
            # to BROTLI_LIBRARIES. Only install the libraries that are actually
            # defined as CMake targets.
            if (TARGET "${BROTLI_LIB}")
                install_static_library(${BROTLI_LIB} aotsdk nativeaot)
            endif()
        endforeach(BROTLI_LIB ${BROTLI_LIBRARIES})
        if (NOT CLR_CMAKE_USE_SYSTEM_ZLIB)
            install_static_library(zlib aotsdk nativeaot)
        endif()
    endif()

endif ()

install (TARGETS System.IO.Compression.Native-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
