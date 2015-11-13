include(CheckLibraryExists)
include(CheckStructHasMember)

set(CMAKE_REQUIRED_INCLUDES ${OPENSSL_INCLUDE_DIR})

# Check which versions of TLS the OpenSSL/ssl library supports
check_library_exists(${OPENSSL_SSL_LIBRARY} "TLSv1_1_method" "" HAVE_TLS_V1_1)
check_library_exists(${OPENSSL_SSL_LIBRARY} "TLSv1_2_method" "" HAVE_TLS_V1_2)

check_struct_has_member ("SSL_CIPHER" algorithm_enc openssl/ssl.h HAVE_SSL_CIPHER_SPLIT_ALGORITHMS)

configure_file(
    ${CMAKE_CURRENT_SOURCE_DIR}/pal_crypto_config.h.in
    ${CMAKE_CURRENT_BINARY_DIR}/pal_crypto_config.h)
