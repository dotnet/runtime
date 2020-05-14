project(System.Security.Cryptography.Native C)

# These are happening inside of OpenSSL-defined macros out of our control
add_compile_options(-Wno-cast-align)
add_compile_options(-Wno-reserved-id-macro)
add_compile_options(-Wno-documentation)
add_compile_options(-Wno-used-but-marked-unused)

add_definitions(-DOPENSSL_API_COMPAT=0x10100000L)

if(CMAKE_STATIC_LIB_LINK)
   set(CMAKE_FIND_LIBRARY_SUFFIXES .a)
endif(CMAKE_STATIC_LIB_LINK)

if(CLR_CMAKE_TARGET_ANDROID AND NOT CROSS_ROOTFS)
    # TEMP: consume OpenSSL dependencies from external sources via env. variables
    set(OPENSSL_FOUND 1)
    set(OPENSSL_INCLUDE_DIR $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/ssl/include)
    if(CLR_CMAKE_TARGET_ARCH_ARM64)
        set(OPENSSL_CRYPTO_LIBRARY $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/crypto/libs/android.arm64-v8a/libcrypto.so)
        set(OPENSSL_SSL_LIBRARY $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/ssl/libs/android.arm64-v8a/libssl.so)
    elseif(CLR_CMAKE_TARGET_ARCH_ARM)
        set(OPENSSL_CRYPTO_LIBRARY $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/crypto/libs/android.armeabi-v7a/libcrypto.so)
        set(OPENSSL_SSL_LIBRARY $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/ssl/libs/android.armeabi-v7a/libssl.so)
    elseif(CLR_CMAKE_TARGET_ARCH_I386)
        set(OPENSSL_CRYPTO_LIBRARY $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/crypto/libs/android.x86/libcrypto.so)
        set(OPENSSL_SSL_LIBRARY $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/ssl/libs/android.x86/libssl.so)
    else()
        set(OPENSSL_CRYPTO_LIBRARY $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/crypto/libs/android.x86_64/libcrypto.so)
        set(OPENSSL_SSL_LIBRARY $ENV{ANDROID_OPENSSL_AAR}/prefab/modules/ssl/libs/android.x86_64/libssl.so)
    endif()
else()
    find_package(OpenSSL)
endif()

if(NOT OPENSSL_FOUND)
    message(FATAL_ERROR "!!! Cannot find libssl and System.Security.Cryptography.Native cannot build without it. Try installing libssl-dev (on Linux, but this may vary by distro) or openssl (on macOS) !!!. See the requirements document for your specific operating system: https://github.com/dotnet/runtime/tree/master/docs/workflow/requirements.")
endif(NOT OPENSSL_FOUND)

include_directories(${OPENSSL_INCLUDE_DIR})

set(NATIVECRYPTO_SOURCES
    apibridge.c
    openssl.c
    pal_asn1.c
    pal_bignum.c
    pal_bio.c
    pal_dsa.c
    pal_ecdsa.c
    pal_ecc_import_export.c
    pal_eckey.c
    pal_err.c
    pal_evp.c
    pal_evp_pkey.c
    pal_evp_pkey_dsa.c
    pal_evp_pkey_ecdh.c
    pal_evp_pkey_eckey.c
    pal_evp_pkey_rsa.c
    pal_evp_cipher.c
    pal_hmac.c
    pal_ocsp.c
    pal_pkcs12.c
    pal_pkcs7.c
    pal_rsa.c
    pal_ssl.c
    pal_x509.c
    pal_x509_name.c
    pal_x509_root.c
    pal_x509ext.c
)

# Always build portable on macOS because OpenSSL is not a system component
# and our prebuilts should not assume a specific ABI version for the types
# that use OpenSSL at runtime.
if (CLR_CMAKE_TARGET_OSX)
    set(FEATURE_DISTRO_AGNOSTIC_SSL True)
endif()

if (FEATURE_DISTRO_AGNOSTIC_SSL)
    list(APPEND NATIVECRYPTO_SOURCES
        opensslshim.c
    )
    add_definitions(-DFEATURE_DISTRO_AGNOSTIC_SSL)
endif()

add_library(objlib OBJECT ${NATIVECRYPTO_SOURCES} ${VERSION_FILE_PATH})

add_library(System.Security.Cryptography.Native.OpenSsl
    SHARED
    $<TARGET_OBJECTS:objlib>
)

add_library(System.Security.Cryptography.Native.OpenSsl-Static
    STATIC
    $<TARGET_OBJECTS:objlib>
)

set_target_properties(System.Security.Cryptography.Native.OpenSsl-Static PROPERTIES OUTPUT_NAME System.Security.Cryptography.Native.OpenSsl CLEAN_DIRECT_OUTPUT 1)

if (FEATURE_DISTRO_AGNOSTIC_SSL)
    # on macOS the link step fails with undefined symbols, and the script doesn't run.
    # if the build succeeds, the script would succeed, except it uses a Linux-only command.
    #
    # on Linux, the build will succeed with undefined symbols, then the script reports them
    # and fails the build for us.
    if (NOT APPLE)
        add_custom_command(TARGET System.Security.Cryptography.Native.OpenSsl POST_BUILD
            COMMENT "Verifying System.Security.Cryptography.Native.OpenSsl.so dependencies"
            COMMAND ${CMAKE_CURRENT_SOURCE_DIR}/../verify-so.sh
                $<TARGET_FILE:System.Security.Cryptography.Native.OpenSsl>
                "Verification failed. System.Security.Cryptography.Native.OpenSsl.so has undefined dependencies. These are likely OpenSSL APIs that need to be added to opensslshim.h"
            VERBATIM
        )
    endif()

    # Link with libdl.so to get the dlopen / dlsym / dlclose
    target_link_libraries(System.Security.Cryptography.Native.OpenSsl
      dl
    )
else()
    target_link_libraries(System.Security.Cryptography.Native.OpenSsl
      ${OPENSSL_CRYPTO_LIBRARY}
      ${OPENSSL_SSL_LIBRARY}
    )
endif()

include(configure.cmake)

install_with_stripped_symbols (System.Security.Cryptography.Native.OpenSsl PROGRAMS .)
install (TARGETS System.Security.Cryptography.Native.OpenSsl-Static DESTINATION .)
