project(System.Security.Cryptography.Native.Android C)

add_compile_options(-Wno-gnu-zero-variadic-macro-arguments)

set(NATIVECRYPTO_SOURCES
    android_security.c
)

add_library(System.Security.Cryptography.Native.Android
    SHARED
    ${NATIVECRYPTO_SOURCES}
    ${VERSION_FILE_PATH}
)

add_library(System.Security.Cryptography.Native.Android-Static
    STATIC
    ${NATIVECRYPTO_SOURCES}
)

set_target_properties(System.Security.Cryptography.Native.Android-Static PROPERTIES OUTPUT_NAME System.Security.Cryptography.Native.Android CLEAN_DIRECT_OUTPUT 1)

target_link_libraries(System.Security.Cryptography.Native.Android
    -llog
)

install_with_stripped_symbols (System.Security.Cryptography.Native.Android PROGRAMS .)
install (TARGETS System.Security.Cryptography.Native.Android-Static DESTINATION .)
