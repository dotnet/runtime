project(System.Security.Cryptography.Native.Browser C)

set (NATIVE_SOURCES
    pal_crypto_webworker.c
)

add_library (System.Security.Cryptography.Native.Browser-Static
    STATIC
    ${NATIVE_SOURCES}
)

set_target_properties(System.Security.Cryptography.Native.Browser-Static PROPERTIES OUTPUT_NAME System.Security.Cryptography.Native.Browser CLEAN_DIRECT_OUTPUT 1)

install (TARGETS System.Security.Cryptography.Native.Browser-Static DESTINATION ${STATIC_LIB_DESTINATION})
