project(System.Security.Cryptography.Native.Android C)

add_compile_options(-Wno-gnu-zero-variadic-macro-arguments)
add_compile_options(-Wno-unused-parameter)
add_compile_options(-Wno-unused-function)

set(NATIVECRYPTO_SOURCES
    pal_jni.c
    pal_misc.c
    pal_evp.c
    pal_evp_cipher.c
    pal_hmac.c
    pal_bignum.c
    pal_ssl.c
    pal_rsa.c
    pal_err.c
    pal_sslstream.c
    pal_x509.c
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

# TODO: Use "System.Security.Cryptography.Native.Android" name (will require a lot of csproj changes here and there)
set_target_properties(System.Security.Cryptography.Native.Android PROPERTIES OUTPUT_NAME "System.Security.Cryptography.Native.OpenSsl")
set_target_properties(System.Security.Cryptography.Native.Android-Static PROPERTIES OUTPUT_NAME "System.Security.Cryptography.Native.OpenSsl")

install_with_stripped_symbols (System.Security.Cryptography.Native.Android PROGRAMS .)
install (TARGETS System.Security.Cryptography.Native.Android-Static DESTINATION .)
