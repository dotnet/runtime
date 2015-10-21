
project(System.Security.Cryptography.Native)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

# Issue 2546 - Many deprecation warnings in System.Security.Cryptography.Native on Mac OS X
add_compile_options(-Wno-deprecated-declarations)

# These are happening inside of OpenSSL-defined macros out of our control
add_compile_options(-Wno-cast-align)

add_definitions(-DPIC=1)

find_package(OpenSSL REQUIRED)

set(NATIVECRYPTO_SOURCES
    openssl.c
    pal_asn1.cpp
    pal_asn1_print.cpp
    pal_bignum.cpp
    pal_bio.cpp
    pal_ecdsa.cpp
    pal_eckey.cpp
    pal_err.cpp
    pal_evp.cpp
    pal_evp_pkey.cpp
    pal_evp_pkey_eckey.cpp
    pal_evp_pkey_rsa.cpp
    pal_evp_cipher.cpp
    pal_hmac.cpp
    pal_pkcs12.cpp
    pal_pkcs7.cpp
    pal_rsa.cpp
    pal_ssl.cpp
    pal_x509.cpp
    pal_x509_name.cpp
    pal_x509ext.cpp
)

add_library(System.Security.Cryptography.Native
    SHARED
    ${NATIVECRYPTO_SOURCES}
)

# Disable the "lib" prefix.
set_target_properties(System.Security.Cryptography.Native PROPERTIES PREFIX "")

target_link_libraries(System.Security.Cryptography.Native
  ${OPENSSL_CRYPTO_LIBRARY}
  ${OPENSSL_SSL_LIBRARY}
)

include(configure.cmake)

install (TARGETS System.Security.Cryptography.Native DESTINATION .)
