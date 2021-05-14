project(System.Security.Cryptography.Native.Browser C)

set(ANYOS_SOURCES_DIR ../../AnyOS/System.Security.Cryptography.Native.Browser)
include_directories("${ANYOS_SOURCES_DIR}")

set (NATIVE_SOURCES
    ${ANYOS_SOURCES_DIR}/pal_crypto_webworker.c
)

add_library (System.Security.Cryptography.Native.Browser-Static
    STATIC
    ${NATIVE_SOURCES}
)

set_target_properties(System.Security.Cryptography.Native.Browser-Static PROPERTIES OUTPUT_NAME System.Security.Cryptography.Native.Browser CLEAN_DIRECT_OUTPUT 1)

install (TARGETS System.Security.Cryptography.Native.Browser-Static DESTINATION ${STATIC_LIB_DESTINATION})