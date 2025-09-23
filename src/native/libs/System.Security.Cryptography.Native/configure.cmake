include(CheckLibraryExists)
include(CheckFunctionExists)
include(CheckSourceCompiles)

set(CMAKE_REQUIRED_INCLUDES ${OPENSSL_INCLUDE_DIR})
set(CMAKE_REQUIRED_LIBRARIES ${OPENSSL_CRYPTO_LIBRARY} ${OPENSSL_SSL_LIBRARY})
set(CMAKE_REQUIRED_DEFINITIONS -DOPENSSL_API_COMPAT=0x10100000L)

check_function_exists(
    EC_GF2m_simple_method
    HAVE_OPENSSL_EC2M)

check_function_exists(
	SSL_get0_alpn_selected
	HAVE_OPENSSL_ALPN)

check_function_exists(
    EVP_sha3_256
    HAVE_OPENSSL_SHA3
)

check_function_exists(
    EVP_DigestSqueeze
    HAVE_OPENSSL_SHA3_SQUEEZE
)

check_function_exists(
    EVP_PKEY_sign_message_init
    HAVE_OPENSSL_EVP_PKEY_SIGN_MESSAGE_INIT
)

check_source_compiles(C "
#include <openssl/evp.h>
// CodeQL [SM01923] This is a CMake function detection script for the OpenSSL API used to implement the .NET API System.Security.Cryptography.ChaCha20Poly1305, it is not actually using the algorithm here
int main(void) { const EVP_CIPHER* cipher = EVP_chacha20_poly1305(); return 1; }"
HAVE_OPENSSL_CHACHA20POLY1305)

check_source_compiles(C "
#include <openssl/engine.h>
int main(void) { ENGINE_init(NULL); return 1; }"
HAVE_OPENSSL_ENGINE)

configure_file(
    ${CMAKE_CURRENT_SOURCE_DIR}/pal_crypto_config.h.in
    ${CMAKE_CURRENT_BINARY_DIR}/pal_crypto_config.h)
