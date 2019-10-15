project(System.Security.Cryptography.Native C)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

# These are happening inside of OpenSSL-defined macros out of our control
add_compile_options(-Wno-cast-align)
add_compile_options(-Wno-reserved-id-macro)
add_compile_options(-Wno-documentation)
add_compile_options(-Wno-used-but-marked-unused)

add_definitions(-DPIC=1 -DOPENSSL_API_COMPAT=0x10100000L)

if(CMAKE_STATIC_LIB_LINK)
   set(CMAKE_FIND_LIBRARY_SUFFIXES .a)
endif(CMAKE_STATIC_LIB_LINK)

find_package(OpenSSL)
if(NOT OPENSSL_FOUND)
    message(FATAL_ERROR "!!! Cannot find libssl and System.Security.Cryptography.Native cannot build without it. Try installing libssl-dev (or the appropriate package for your platform) !!!")
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

if (FEATURE_DISTRO_AGNOSTIC_SSL)
    if (NOT CMAKE_SYSTEM_NAME STREQUAL Linux)
        message(FATAL_ERROR "FEATURE_DISTRO_AGNOSTIC_SSL can only be enabled for Linux")
    endif()

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

# Disable the "lib" prefix.
set_target_properties(System.Security.Cryptography.Native.OpenSsl PROPERTIES PREFIX "")

# Disable the "lib" prefix and override default name
set_target_properties(System.Security.Cryptography.Native.OpenSsl-Static PROPERTIES PREFIX "")
set_target_properties(System.Security.Cryptography.Native.OpenSsl-Static PROPERTIES OUTPUT_NAME System.Security.Cryptography.Native.OpenSsl CLEAN_DIRECT_OUTPUT 1)

if (FEATURE_DISTRO_AGNOSTIC_SSL)
    add_custom_command(TARGET System.Security.Cryptography.Native.OpenSsl POST_BUILD
        COMMENT "Verifying System.Security.Cryptography.Native.OpenSsl.so dependencies"
        COMMAND ${CMAKE_CURRENT_SOURCE_DIR}/../verify-so.sh 
            $<TARGET_FILE:System.Security.Cryptography.Native.OpenSsl> 
            "Verification failed. System.Security.Cryptography.Native.OpenSsl.so has undefined dependencies. These are likely OpenSSL APIs that need to be added to opensslshim.h"
        VERBATIM
    )

    # Link with libdl.so to get the dlopen / dlsym / dlclose
    target_link_libraries(System.Security.Cryptography.Native.OpenSsl
      dl
    )
else()
    target_link_libraries(System.Security.Cryptography.Native.OpenSsl
      ${OPENSSL_CRYPTO_LIBRARY}
      ${OPENSSL_SSL_LIBRARY}
    )
  
    # On OS X every library emits the manner in which it should be referenced.
    # All of our libraries are referenced via @rpath, which is similar to how Linux and Windows
    # libraries are loaded. The homebrew installation of OpenSSL (libcrypto, libssl) uses the
    # full path to the library installation. This means that this library is not flexible to
    # users installing newer libcrypto in the working directory, or to systems which do not
    # install to the same path as homebrew does.
    #
    # So, after compiling, rewrite the references to libcrypto to be more flexible.
    if (APPLE)
        add_custom_command(TARGET System.Security.Cryptography.Native.OpenSsl POST_BUILD
            COMMAND ${CMAKE_INSTALL_NAME_TOOL} -change /usr/local/opt/openssl/lib/libcrypto.1.0.0.dylib @rpath/libcrypto.1.0.0.dylib $<TARGET_FILE:System.Security.Cryptography.Native.OpenSsl>
            COMMAND ${CMAKE_INSTALL_NAME_TOOL} -change /usr/local/opt/openssl/lib/libssl.1.0.0.dylib @rpath/libssl.1.0.0.dylib $<TARGET_FILE:System.Security.Cryptography.Native.OpenSsl>
            COMMAND ${CMAKE_INSTALL_NAME_TOOL} -add_rpath @loader_path $<TARGET_FILE:System.Security.Cryptography.Native.OpenSsl>
            )
    endif()
endif()

include(configure.cmake)

install_library_and_symbols (System.Security.Cryptography.Native.OpenSsl)
install (TARGETS System.Security.Cryptography.Native.OpenSsl-Static DESTINATION .)
