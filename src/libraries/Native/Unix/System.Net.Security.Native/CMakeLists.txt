project(System.Net.Security.Native C)

add_compile_options(-Wno-incompatible-pointer-types-discards-qualifiers)

include(${CMAKE_CURRENT_LIST_DIR}/extra_libs.cmake)

set(NATIVE_LIBS_EXTRA)
append_extra_security_libs(NATIVE_LIBS_EXTRA)

set(NATIVEGSS_SOURCES
    pal_gssapi.c
)

if (GEN_SHARED_LIB)
    add_library(System.Net.Security.Native
        SHARED
        ${NATIVEGSS_SOURCES}
        ${VERSION_FILE_PATH}
    )
endif()

add_library(System.Net.Security.Native-Static
    STATIC
    ${NATIVEGSS_SOURCES}
    entrypoints.c
)

set_target_properties(System.Net.Security.Native-Static PROPERTIES OUTPUT_NAME System.Net.Security.Native CLEAN_DIRECT_OUTPUT 1)

if (GEN_SHARED_LIB)
    target_link_libraries(System.Net.Security.Native
        ${NATIVE_LIBS_EXTRA}
    )

    install_with_stripped_symbols (System.Net.Security.Native PROGRAMS .)
endif()

install (TARGETS System.Net.Security.Native-Static DESTINATION ${STATIC_LIB_DESTINATION})
