project(System.Security.Cryptography.Native.Browser C)

set(NATIVECRYPTO_SOURCES
    pal_digest.c
)


if (GEN_SHARED_LIB)
    add_library(System.Security.Cryptography.Native.Browser
        SHARED
        ${NATIVECRYPTO_SOURCES}
        ${VERSION_FILE_PATH}
    )

    #target_link_libraries(System.Security.Cryptography.Native.Browser
    #    ${ZLIB_LIBRARIES}
    #)
    install_with_stripped_symbols (System.Security.Cryptography.Native.Browser PROGRAMS .)
endif ()

add_library(System.Security.Cryptography.Native.Browser-Static
    STATIC
    ${NATIVECRYPTO_SOURCES}
)

set_target_properties(System.Security.Cryptography.Native.Browser-Static PROPERTIES OUTPUT_NAME System.Security.Cryptography.Native.Browser  CLEAN_DIRECT_OUTPUT 1)

install (TARGETS System.Security.Cryptography.Native.Browser-Static DESTINATION .)
