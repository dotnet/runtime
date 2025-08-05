project(System.Security.Cryptography.Native.Android C)

add_compile_options(-Wno-gnu-zero-variadic-macro-arguments)
add_compile_options(-Wno-unused-parameter)
add_compile_options(-Wno-unused-function)
add_compile_options(-Wnonnull)

set(NATIVECRYPTO_SOURCES
    pal_bignum.c
    pal_dsa.c
    pal_ecdsa.c
    pal_ecc_import_export.c
    pal_eckey.c
    pal_ecdh.c
    pal_err.c
    pal_evp.c
    pal_cipher.c
    pal_hmac.c
    pal_jni.c
    pal_lifetime.c
    pal_memory.c
    pal_misc.c
    pal_pbkdf2.c
    pal_rsa.c
    pal_signature.c
    pal_ssl.c
    pal_sslstream.c
    pal_trust_manager.c
    pal_x509.c
    pal_x509chain.c
    pal_x509store.c
)

add_library(System.Security.Cryptography.Native.Android
    SHARED
    ${NATIVECRYPTO_SOURCES} pal_jni_onload.c pal_trust_manager_jni_export.c
    ${VERSION_FILE_PATH}
)

add_library(System.Security.Cryptography.Native.Android-Static
    STATIC
    ${NATIVECRYPTO_SOURCES}
)

#
# This is necessary so that dynamic linking of the .NET for Android runtime
# can hide all the other symbols in System.Security.Cryptography.Native.Android.
#
# .NET for Android dynamic runtime linking links all the relevant native BCL
# libraries into a single .so, using the .a archives built here.  clang allows
# hiding all the symbols in the .a archive, but there's no (working) way to
# exclude just select symbols from hiding.
#
# Java VM requires that all the functions implementing the `native` methods are
# exported from the shared libraries they are implemented in.  Therefore it is
# necessary to put this symbol in a separate .a archive so that we can exclude it
# from hiding described above.
#
add_library(System.Security.Cryptography.Native.Android-StaticJNIExport
    STATIC
    pal_trust_manager_jni_export.c
)

target_link_libraries(System.Security.Cryptography.Native.Android
    PRIVATE
    -llog
)

set_target_properties(System.Security.Cryptography.Native.Android PROPERTIES OUTPUT_NAME "System.Security.Cryptography.Native.Android")
set_target_properties(System.Security.Cryptography.Native.Android-Static PROPERTIES OUTPUT_NAME "System.Security.Cryptography.Native.Android" CLEAN_DIRECT_OUTPUT 1)
set_target_properties(System.Security.Cryptography.Native.Android-StaticJNIExport PROPERTIES OUTPUT_NAME "System.Security.Cryptography.Native.Android.JNIExport"  CLEAN_DIRECT_OUTPUT 1)

if (GEN_SHARED_LIB)
    install_with_stripped_symbols (System.Security.Cryptography.Native.Android PROGRAMS .)
endif()

install (TARGETS System.Security.Cryptography.Native.Android-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
install (TARGETS System.Security.Cryptography.Native.Android-StaticJNIExport DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)

if(CLR_CMAKE_HOST_ANDROID)
    install (TARGETS System.Security.Cryptography.Native.Android-Static DESTINATION sharedFramework COMPONENT runtime)
    install (TARGETS System.Security.Cryptography.Native.Android-StaticJNIExport DESTINATION sharedFramework COMPONENT runtime)
endif()
