include(CheckLibraryExists)
include(CheckFunctionExists)

set(CMAKE_REQUIRED_INCLUDES ${OPENSSL_INCLUDE_DIR})
set(CMAKE_REQUIRED_LIBRARIES ${OPENSSL_CRYPTO_LIBRARY} ${OPENSSL_SSL_LIBRARY})

check_function_exists(
    EC_GF2m_simple_method
    HAVE_OPENSSL_EC2M)

check_function_exists(
	SSL_get0_alpn_selected
	HAVE_OPENSSL_ALPN)

configure_file(
    ${CMAKE_CURRENT_SOURCE_DIR}/pal_crypto_config.h.in
    ${CMAKE_CURRENT_BINARY_DIR}/pal_crypto_config.h)
