// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

// Enable calling OpenSSL functions through shims to enable support for 
// different versioned so files naming and different configuration options
// on various Linux distributions.

#pragma once

// All the openssl includes need to be here to ensure that the APIs we use
// are overriden to be called through our function pointers.
#include <openssl/asn1.h>
#include <openssl/bio.h>
#include <openssl/bn.h>
#include <openssl/crypto.h>
#include <openssl/dsa.h>
#include <openssl/ecdsa.h>
#include <openssl/ec.h>
#include <openssl/err.h>
#include <openssl/evp.h>
#include <openssl/hmac.h>
#include <openssl/md5.h>
#include <openssl/objects.h>
#include <openssl/pem.h>
#include <openssl/pkcs12.h>
#include <openssl/pkcs7.h>
#include <openssl/rand.h>
#include <openssl/rsa.h>
#include <openssl/sha.h>
#include <openssl/ssl.h>
#include <openssl/tls1.h>
#include <openssl/x509.h>
#include <openssl/x509v3.h>

#include "pal_crypto_config.h"

#ifdef FEATURE_DISTRO_AGNOSTIC_SSL

#if !HAVE_OPENSSL_EC2M
// In portable build, we need to support the following functions even if they were not present
// on the build OS. The shim will detect their presence at runtime.
#undef HAVE_OPENSSL_EC2M
#define HAVE_OPENSSL_EC2M 1
const EC_METHOD *EC_GF2m_simple_method(void);
int EC_GROUP_get_curve_GF2m(const EC_GROUP *group, BIGNUM *p, BIGNUM *a, BIGNUM *b, BN_CTX *ctx);
int EC_GROUP_set_curve_GF2m(EC_GROUP *group, const BIGNUM *p, const BIGNUM *a, const BIGNUM *b, BN_CTX *ctx);
int EC_POINT_get_affine_coordinates_GF2m(const EC_GROUP *group,
        const EC_POINT *p, BIGNUM *x, BIGNUM *y, BN_CTX *ctx);
int EC_POINT_set_affine_coordinates_GF2m(const EC_GROUP *group, EC_POINT *p,
        const BIGNUM *x, const BIGNUM *y, BN_CTX *ctx);
#endif

#if !HAVE_OPENSSL_ALPN
#undef HAVE_OPENSSL_ALPN
#define HAVE_OPENSSL_ALPN 1
int SSL_CTX_set_alpn_protos(SSL_CTX* ctx, const unsigned char* protos, unsigned int protos_len);
void SSL_CTX_set_alpn_select_cb(SSL_CTX* ctx, int (*cb) (SSL *ssl,
                                            const unsigned char **out,
                                            unsigned char *outlen,
                                            const unsigned char *in,
                                            unsigned int inlen,
                                            void *arg), void *arg);
void SSL_get0_alpn_selected(const SSL* ssl, const unsigned char** protocol, unsigned int* len);
#endif

#define API_EXISTS(fn) (fn != nullptr)

// List of all functions from the libssl that are used in the System.Security.Cryptography.Native.
// Forgetting to add a function here results in build failure with message reporting the function
// that needs to be added.
#define FOR_ALL_OPENSSL_FUNCTIONS \
    PER_FUNCTION_BLOCK(ASN1_BIT_STRING_free, true) \
    PER_FUNCTION_BLOCK(ASN1_INTEGER_get, true) \
    PER_FUNCTION_BLOCK(ASN1_OBJECT_free, true) \
    PER_FUNCTION_BLOCK(ASN1_OCTET_STRING_free, true) \
    PER_FUNCTION_BLOCK(ASN1_OCTET_STRING_new, true) \
    PER_FUNCTION_BLOCK(ASN1_OCTET_STRING_set, true) \
    PER_FUNCTION_BLOCK(ASN1_STRING_free, true) \
    PER_FUNCTION_BLOCK(ASN1_STRING_print_ex, true) \
    PER_FUNCTION_BLOCK(BASIC_CONSTRAINTS_free, true) \
    PER_FUNCTION_BLOCK(BIO_ctrl, true) \
    PER_FUNCTION_BLOCK(BIO_ctrl_pending, true) \
    PER_FUNCTION_BLOCK(BIO_free, true) \
    PER_FUNCTION_BLOCK(BIO_gets, true) \
    PER_FUNCTION_BLOCK(BIO_new, true) \
    PER_FUNCTION_BLOCK(BIO_new_file, true) \
    PER_FUNCTION_BLOCK(BIO_read, true) \
    PER_FUNCTION_BLOCK(BIO_s_mem, true) \
    PER_FUNCTION_BLOCK(BIO_write, true) \
    PER_FUNCTION_BLOCK(BN_bin2bn, true) \
    PER_FUNCTION_BLOCK(BN_bn2bin, true) \
    PER_FUNCTION_BLOCK(BN_clear_free, true) \
    PER_FUNCTION_BLOCK(BN_free, true) \
    PER_FUNCTION_BLOCK(BN_new, true) \
    PER_FUNCTION_BLOCK(BN_num_bits, true) \
    PER_FUNCTION_BLOCK(CRYPTO_add_lock, true) \
    PER_FUNCTION_BLOCK(CRYPTO_num_locks, true) \
    PER_FUNCTION_BLOCK(CRYPTO_set_locking_callback, true) \
    PER_FUNCTION_BLOCK(d2i_ASN1_BIT_STRING, true) \
    PER_FUNCTION_BLOCK(d2i_ASN1_OCTET_STRING, true) \
    PER_FUNCTION_BLOCK(d2i_ASN1_type_bytes, true) \
    PER_FUNCTION_BLOCK(d2i_BASIC_CONSTRAINTS, true) \
    PER_FUNCTION_BLOCK(d2i_EXTENDED_KEY_USAGE, true) \
    PER_FUNCTION_BLOCK(d2i_PKCS12, true) \
    PER_FUNCTION_BLOCK(d2i_PKCS12_bio, true) \
    PER_FUNCTION_BLOCK(d2i_PKCS7, true) \
    PER_FUNCTION_BLOCK(d2i_PKCS7_bio, true) \
    PER_FUNCTION_BLOCK(d2i_RSAPublicKey, true) \
    PER_FUNCTION_BLOCK(d2i_X509, true) \
    PER_FUNCTION_BLOCK(d2i_X509_bio, true) \
    PER_FUNCTION_BLOCK(d2i_X509_CRL, true) \
    PER_FUNCTION_BLOCK(d2i_X509_NAME, true) \
    PER_FUNCTION_BLOCK(DSA_free, true) \
    PER_FUNCTION_BLOCK(DSA_generate_key, true) \
    PER_FUNCTION_BLOCK(DSA_generate_parameters_ex, true) \
    PER_FUNCTION_BLOCK(DSA_new, true) \
    PER_FUNCTION_BLOCK(DSA_OpenSSL, true) \
    PER_FUNCTION_BLOCK(DSA_sign, true) \
    PER_FUNCTION_BLOCK(DSA_size, true) \
    PER_FUNCTION_BLOCK(DSA_up_ref, true) \
    PER_FUNCTION_BLOCK(DSA_verify, true) \
    PER_FUNCTION_BLOCK(ECDSA_sign, true) \
    PER_FUNCTION_BLOCK(ECDSA_size, true) \
    PER_FUNCTION_BLOCK(ECDSA_verify, true) \
    PER_FUNCTION_BLOCK(EC_GFp_mont_method, true) \
    PER_FUNCTION_BLOCK(EC_GFp_simple_method, true) \
    PER_FUNCTION_BLOCK(EC_GROUP_check, true) \
    PER_FUNCTION_BLOCK(EC_GROUP_free, true) \
    PER_FUNCTION_BLOCK(EC_GROUP_get0_generator, true) \
    PER_FUNCTION_BLOCK(EC_GROUP_get0_seed, true) \
    PER_FUNCTION_BLOCK(EC_GROUP_get_cofactor, true) \
    PER_FUNCTION_BLOCK(EC_GROUP_get_curve_GFp, true) \
    PER_FUNCTION_BLOCK(EC_GROUP_get_curve_name, true) \
    PER_FUNCTION_BLOCK(EC_GROUP_get_degree, true) \
    PER_FUNCTION_BLOCK(EC_GROUP_get_order, true) \
    PER_FUNCTION_BLOCK(EC_GROUP_get_seed_len, true) \
    PER_FUNCTION_BLOCK(EC_GROUP_method_of, true) \
    PER_FUNCTION_BLOCK(EC_GROUP_new, true) \
    PER_FUNCTION_BLOCK(EC_GROUP_set_curve_GFp, true) \
    PER_FUNCTION_BLOCK(EC_GROUP_set_generator, true) \
    PER_FUNCTION_BLOCK(EC_GROUP_set_seed, true) \
    PER_FUNCTION_BLOCK(EC_KEY_check_key, true) \
    PER_FUNCTION_BLOCK(EC_KEY_free, true) \
    PER_FUNCTION_BLOCK(EC_KEY_generate_key, true) \
    PER_FUNCTION_BLOCK(EC_KEY_get0_group, true) \
    PER_FUNCTION_BLOCK(EC_KEY_get0_private_key, true) \
    PER_FUNCTION_BLOCK(EC_KEY_get0_public_key, true) \
    PER_FUNCTION_BLOCK(EC_KEY_new, true) \
    PER_FUNCTION_BLOCK(EC_KEY_new_by_curve_name, true) \
    PER_FUNCTION_BLOCK(EC_KEY_set_group, true) \
    PER_FUNCTION_BLOCK(EC_KEY_set_private_key, true) \
    PER_FUNCTION_BLOCK(EC_KEY_set_public_key_affine_coordinates, true) \
    PER_FUNCTION_BLOCK(EC_KEY_up_ref, true) \
    PER_FUNCTION_BLOCK(EC_METHOD_get_field_type, true) \
    PER_FUNCTION_BLOCK(EC_POINT_free, true) \
    PER_FUNCTION_BLOCK(EC_POINT_get_affine_coordinates_GFp, true) \
    PER_FUNCTION_BLOCK(EC_POINT_new, true) \
    PER_FUNCTION_BLOCK(EC_POINT_set_affine_coordinates_GFp, true) \
    PER_FUNCTION_BLOCK(ERR_clear_error, true) \
    PER_FUNCTION_BLOCK(ERR_error_string_n, true) \
    PER_FUNCTION_BLOCK(ERR_get_error, true) \
    PER_FUNCTION_BLOCK(ERR_load_crypto_strings, true) \
    PER_FUNCTION_BLOCK(ERR_put_error, true) \
    PER_FUNCTION_BLOCK(ERR_peek_error, true) \
    PER_FUNCTION_BLOCK(ERR_peek_last_error, true) \
    PER_FUNCTION_BLOCK(ERR_reason_error_string, true) \
    PER_FUNCTION_BLOCK(EVP_aes_128_cbc, true) \
    PER_FUNCTION_BLOCK(EVP_aes_128_ecb, true) \
    PER_FUNCTION_BLOCK(EVP_aes_192_cbc, true) \
    PER_FUNCTION_BLOCK(EVP_aes_192_ecb, true) \
    PER_FUNCTION_BLOCK(EVP_aes_256_cbc, true) \
    PER_FUNCTION_BLOCK(EVP_aes_256_ecb, true) \
    PER_FUNCTION_BLOCK(EVP_CIPHER_CTX_cleanup, true) \
    PER_FUNCTION_BLOCK(EVP_CIPHER_CTX_ctrl, true) \
    PER_FUNCTION_BLOCK(EVP_CIPHER_CTX_init, true) \
    PER_FUNCTION_BLOCK(EVP_CIPHER_CTX_set_key_length, true) \
    PER_FUNCTION_BLOCK(EVP_CIPHER_CTX_set_padding, true) \
    PER_FUNCTION_BLOCK(EVP_CipherFinal_ex, true) \
    PER_FUNCTION_BLOCK(EVP_CipherInit_ex, true) \
    PER_FUNCTION_BLOCK(EVP_CipherUpdate, true) \
    PER_FUNCTION_BLOCK(EVP_des_cbc, true) \
    PER_FUNCTION_BLOCK(EVP_des_ecb, true) \
    PER_FUNCTION_BLOCK(EVP_des_ede3, true) \
    PER_FUNCTION_BLOCK(EVP_des_ede3_cbc, true) \
    PER_FUNCTION_BLOCK(EVP_DigestFinal_ex, true) \
    PER_FUNCTION_BLOCK(EVP_DigestInit_ex, true) \
    PER_FUNCTION_BLOCK(EVP_DigestUpdate, true) \
    PER_FUNCTION_BLOCK(EVP_get_digestbyname, true) \
    PER_FUNCTION_BLOCK(EVP_md5, true) \
    PER_FUNCTION_BLOCK(EVP_MD_CTX_create, true) \
    PER_FUNCTION_BLOCK(EVP_MD_CTX_destroy, true) \
    PER_FUNCTION_BLOCK(EVP_MD_size, true) \
    PER_FUNCTION_BLOCK(EVP_PKEY_free, true) \
    PER_FUNCTION_BLOCK(EVP_PKEY_get1_DSA, true) \
    PER_FUNCTION_BLOCK(EVP_PKEY_get1_EC_KEY, true) \
    PER_FUNCTION_BLOCK(EVP_PKEY_get1_RSA, true) \
    PER_FUNCTION_BLOCK(EVP_PKEY_new, true) \
    PER_FUNCTION_BLOCK(EVP_PKEY_set1_DSA, true) \
    PER_FUNCTION_BLOCK(EVP_PKEY_set1_EC_KEY, true) \
    PER_FUNCTION_BLOCK(EVP_PKEY_set1_RSA, true) \
    PER_FUNCTION_BLOCK(EVP_rc2_cbc, true) \
    PER_FUNCTION_BLOCK(EVP_rc2_ecb, true) \
    PER_FUNCTION_BLOCK(EVP_sha1, true) \
    PER_FUNCTION_BLOCK(EVP_sha256, true) \
    PER_FUNCTION_BLOCK(EVP_sha384, true) \
    PER_FUNCTION_BLOCK(EVP_sha512, true) \
    PER_FUNCTION_BLOCK(EXTENDED_KEY_USAGE_free, true) \
    PER_FUNCTION_BLOCK(GENERAL_NAMES_free, true) \
    PER_FUNCTION_BLOCK(HMAC_CTX_cleanup, true) \
    PER_FUNCTION_BLOCK(HMAC_CTX_init, true) \
    PER_FUNCTION_BLOCK(HMAC_Final, true) \
    PER_FUNCTION_BLOCK(HMAC_Init_ex, true) \
    PER_FUNCTION_BLOCK(HMAC_Update, true) \
    PER_FUNCTION_BLOCK(i2d_ASN1_INTEGER, true) \
    PER_FUNCTION_BLOCK(i2d_ASN1_TYPE, true) \
    PER_FUNCTION_BLOCK(i2d_PKCS12, true) \
    PER_FUNCTION_BLOCK(i2d_PKCS7, true) \
    PER_FUNCTION_BLOCK(i2d_X509, true) \
    PER_FUNCTION_BLOCK(i2d_X509_PUBKEY, true) \
    PER_FUNCTION_BLOCK(OBJ_ln2nid, true) \
    PER_FUNCTION_BLOCK(OBJ_nid2ln, true) \
    PER_FUNCTION_BLOCK(OBJ_nid2sn, true) \
    PER_FUNCTION_BLOCK(OBJ_nid2obj, true) \
    PER_FUNCTION_BLOCK(OBJ_obj2nid, true) \
    PER_FUNCTION_BLOCK(OBJ_obj2txt, true) \
    PER_FUNCTION_BLOCK(OBJ_sn2nid, true) \
    PER_FUNCTION_BLOCK(OBJ_txt2nid, true) \
    PER_FUNCTION_BLOCK(OBJ_txt2obj, true) \
    PER_FUNCTION_BLOCK(OPENSSL_add_all_algorithms_conf, true) \
    PER_FUNCTION_BLOCK(PEM_read_bio_PKCS7, true) \
    PER_FUNCTION_BLOCK(PEM_read_bio_X509_AUX, true) \
    PER_FUNCTION_BLOCK(PEM_read_bio_X509_CRL, true) \
    PER_FUNCTION_BLOCK(PEM_write_bio_X509_CRL, true) \
    PER_FUNCTION_BLOCK(PKCS12_create, true) \
    PER_FUNCTION_BLOCK(PKCS12_free, true) \
    PER_FUNCTION_BLOCK(PKCS12_parse, true) \
    PER_FUNCTION_BLOCK(PKCS7_add_certificate, true) \
    PER_FUNCTION_BLOCK(PKCS7_content_new, true) \
    PER_FUNCTION_BLOCK(PKCS7_free, true) \
    PER_FUNCTION_BLOCK(PKCS7_new, true) \
    PER_FUNCTION_BLOCK(PKCS7_set_type, true) \
    PER_FUNCTION_BLOCK(RAND_bytes, true) \
    PER_FUNCTION_BLOCK(RAND_poll, true) \
    PER_FUNCTION_BLOCK(RSA_free, true) \
    PER_FUNCTION_BLOCK(RSA_generate_key_ex, true) \
    PER_FUNCTION_BLOCK(RSA_get_method, true) \
    PER_FUNCTION_BLOCK(RSA_new, true) \
    PER_FUNCTION_BLOCK(RSA_private_decrypt, true) \
    PER_FUNCTION_BLOCK(RSA_public_encrypt, true) \
    PER_FUNCTION_BLOCK(RSA_sign, true) \
    PER_FUNCTION_BLOCK(RSA_size, true) \
    PER_FUNCTION_BLOCK(RSA_up_ref, true) \
    PER_FUNCTION_BLOCK(RSA_verify, true) \
    PER_FUNCTION_BLOCK(sk_free, true) \
    PER_FUNCTION_BLOCK(sk_new_null, true) \
    PER_FUNCTION_BLOCK(sk_num, true) \
    PER_FUNCTION_BLOCK(sk_pop_free, true) \
    PER_FUNCTION_BLOCK(sk_push, true) \
    PER_FUNCTION_BLOCK(sk_value, true) \
    PER_FUNCTION_BLOCK(SSL_CIPHER_description, true) \
    PER_FUNCTION_BLOCK(SSL_ctrl, true) \
    PER_FUNCTION_BLOCK(SSL_set_quiet_shutdown, true) \
    PER_FUNCTION_BLOCK(SSL_CTX_check_private_key, true) \
    PER_FUNCTION_BLOCK(SSL_CTX_ctrl, true) \
    PER_FUNCTION_BLOCK(SSL_CTX_free, true) \
    PER_FUNCTION_BLOCK(SSL_CTX_new, true) \
    PER_FUNCTION_BLOCK(SSL_CTX_set_alpn_protos, false) \
    PER_FUNCTION_BLOCK(SSL_CTX_set_alpn_select_cb, false) \
    PER_FUNCTION_BLOCK(SSL_CTX_set_cert_verify_callback, true) \
    PER_FUNCTION_BLOCK(SSL_CTX_set_cipher_list, true) \
    PER_FUNCTION_BLOCK(SSL_CTX_set_client_cert_cb, true) \
    PER_FUNCTION_BLOCK(SSL_CTX_set_quiet_shutdown, true) \
    PER_FUNCTION_BLOCK(SSL_CTX_set_verify, true) \
    PER_FUNCTION_BLOCK(SSL_CTX_use_certificate, true) \
    PER_FUNCTION_BLOCK(SSL_CTX_use_PrivateKey, true) \
    PER_FUNCTION_BLOCK(SSL_do_handshake, true) \
    PER_FUNCTION_BLOCK(SSL_free, true) \
    PER_FUNCTION_BLOCK(SSL_get_client_CA_list, true) \
    PER_FUNCTION_BLOCK(SSL_get_current_cipher, true) \
    PER_FUNCTION_BLOCK(SSL_get_error, true) \
    PER_FUNCTION_BLOCK(SSL_get_finished, true) \
    PER_FUNCTION_BLOCK(SSL_get_peer_cert_chain, true) \
    PER_FUNCTION_BLOCK(SSL_get_peer_certificate, true) \
    PER_FUNCTION_BLOCK(SSL_get_peer_finished, true) \
    PER_FUNCTION_BLOCK(SSL_get_SSL_CTX, true) \
    PER_FUNCTION_BLOCK(SSL_get_version, true) \
    PER_FUNCTION_BLOCK(SSL_get0_alpn_selected, false) \
    PER_FUNCTION_BLOCK(SSL_library_init, true) \
    PER_FUNCTION_BLOCK(SSL_load_error_strings, true) \
    PER_FUNCTION_BLOCK(SSL_new, true) \
    PER_FUNCTION_BLOCK(SSL_read, true) \
    PER_FUNCTION_BLOCK(SSL_renegotiate_pending, true) \
    PER_FUNCTION_BLOCK(SSL_set_accept_state, true) \
    PER_FUNCTION_BLOCK(SSL_set_bio, true) \
    PER_FUNCTION_BLOCK(SSL_set_connect_state, true) \
    PER_FUNCTION_BLOCK(SSL_shutdown, true) \
    PER_FUNCTION_BLOCK(SSL_state, true) \
    PER_FUNCTION_BLOCK(SSLeay_version, true) \
    PER_FUNCTION_BLOCK(SSLv23_method, true) \
    PER_FUNCTION_BLOCK(SSL_write, true) \
    PER_FUNCTION_BLOCK(TLSv1_1_method, true) \
    PER_FUNCTION_BLOCK(TLSv1_2_method, true) \
    PER_FUNCTION_BLOCK(TLSv1_method, true) \
    PER_FUNCTION_BLOCK(X509_check_issued, true) \
    PER_FUNCTION_BLOCK(X509_check_purpose, true) \
    PER_FUNCTION_BLOCK(X509_CRL_free, true) \
    PER_FUNCTION_BLOCK(X509_digest, true) \
    PER_FUNCTION_BLOCK(X509_dup, true) \
    PER_FUNCTION_BLOCK(X509_EXTENSION_create_by_OBJ, true) \
    PER_FUNCTION_BLOCK(X509_EXTENSION_free, true) \
    PER_FUNCTION_BLOCK(X509_EXTENSION_get_critical, true) \
    PER_FUNCTION_BLOCK(X509_EXTENSION_get_data, true) \
    PER_FUNCTION_BLOCK(X509_EXTENSION_get_object, true) \
    PER_FUNCTION_BLOCK(X509_free, true) \
    PER_FUNCTION_BLOCK(X509_get_default_cert_dir, true) \
    PER_FUNCTION_BLOCK(X509_get_default_cert_dir_env, true) \
    PER_FUNCTION_BLOCK(X509_get_default_cert_file, true) \
    PER_FUNCTION_BLOCK(X509_get_default_cert_file_env, true) \
    PER_FUNCTION_BLOCK(X509_get_ext, true) \
    PER_FUNCTION_BLOCK(X509_get_ext_count, true) \
    PER_FUNCTION_BLOCK(X509_get_ext_d2i, true) \
    PER_FUNCTION_BLOCK(X509_get_issuer_name, true) \
    PER_FUNCTION_BLOCK(X509_get_serialNumber, true) \
    PER_FUNCTION_BLOCK(X509_get_subject_name, true) \
    PER_FUNCTION_BLOCK(X509_issuer_name_hash, true) \
    PER_FUNCTION_BLOCK(X509_NAME_entry_count, true) \
    PER_FUNCTION_BLOCK(X509_NAME_ENTRY_get_data, true) \
    PER_FUNCTION_BLOCK(X509_NAME_ENTRY_get_object, true) \
    PER_FUNCTION_BLOCK(X509_NAME_free, true) \
    PER_FUNCTION_BLOCK(X509_NAME_get_entry, true) \
    PER_FUNCTION_BLOCK(X509_NAME_get_index_by_NID, true) \
    PER_FUNCTION_BLOCK(X509_PUBKEY_get, true) \
    PER_FUNCTION_BLOCK(X509_STORE_add_cert, true) \
    PER_FUNCTION_BLOCK(X509_STORE_add_crl, true) \
    PER_FUNCTION_BLOCK(X509_STORE_CTX_free, true) \
    PER_FUNCTION_BLOCK(X509_STORE_CTX_get0_param, true) \
    PER_FUNCTION_BLOCK(X509_STORE_CTX_get1_chain, true) \
    PER_FUNCTION_BLOCK(X509_STORE_CTX_get_error, true) \
    PER_FUNCTION_BLOCK(X509_STORE_CTX_get_error_depth, true) \
    PER_FUNCTION_BLOCK(X509_STORE_CTX_init, true) \
    PER_FUNCTION_BLOCK(X509_STORE_CTX_new, true) \
    PER_FUNCTION_BLOCK(X509_STORE_CTX_set_flags, true) \
    PER_FUNCTION_BLOCK(X509_STORE_CTX_set_verify_cb, true) \
    PER_FUNCTION_BLOCK(X509_STORE_free, true) \
    PER_FUNCTION_BLOCK(X509_STORE_new, true) \
    PER_FUNCTION_BLOCK(X509_STORE_set_flags, true) \
    PER_FUNCTION_BLOCK(X509V3_EXT_print, true) \
    PER_FUNCTION_BLOCK(X509_verify_cert, true) \
    PER_FUNCTION_BLOCK(X509_verify_cert_error_string, true) \
    PER_FUNCTION_BLOCK(X509_VERIFY_PARAM_set_time, true) \
    PER_FUNCTION_BLOCK(EC_GF2m_simple_method, false) \
    PER_FUNCTION_BLOCK(EC_GROUP_get_curve_GF2m, false) \
    PER_FUNCTION_BLOCK(EC_GROUP_set_curve_GF2m, false) \
    PER_FUNCTION_BLOCK(EC_POINT_get_affine_coordinates_GF2m, false) \
    PER_FUNCTION_BLOCK(EC_POINT_set_affine_coordinates_GF2m, false) \
    
// Declare pointers to all the used OpenSSL functions
#define PER_FUNCTION_BLOCK(fn, isRequired) extern decltype(fn)* fn##_ptr;
FOR_ALL_OPENSSL_FUNCTIONS
#undef PER_FUNCTION_BLOCK

// Redefine all calls to OpenSSL functions as calls through pointers that are set
// to the functions from the libssl.so selected by the shim.
#define ASN1_BIT_STRING_free ASN1_BIT_STRING_free_ptr
#define ASN1_INTEGER_get ASN1_INTEGER_get_ptr
#define ASN1_OBJECT_free ASN1_OBJECT_free_ptr
#define ASN1_OCTET_STRING_free ASN1_OCTET_STRING_free_ptr
#define ASN1_OCTET_STRING_new ASN1_OCTET_STRING_new_ptr
#define ASN1_OCTET_STRING_set ASN1_OCTET_STRING_set_ptr
#define ASN1_STRING_free ASN1_STRING_free_ptr
#define ASN1_STRING_print_ex ASN1_STRING_print_ex_ptr
#define BASIC_CONSTRAINTS_free BASIC_CONSTRAINTS_free_ptr
#define BIO_ctrl BIO_ctrl_ptr
#define BIO_ctrl_pending BIO_ctrl_pending_ptr
#define BIO_free BIO_free_ptr
#define BIO_gets BIO_gets_ptr
#define BIO_new BIO_new_ptr
#define BIO_new_file BIO_new_file_ptr
#define BIO_read BIO_read_ptr
#define BIO_s_mem BIO_s_mem_ptr
#define BIO_write BIO_write_ptr
#define BN_bin2bn BN_bin2bn_ptr
#define BN_bn2bin BN_bn2bin_ptr
#define BN_clear_free BN_clear_free_ptr
#define BN_free BN_free_ptr
#define BN_new BN_new_ptr
#define BN_num_bits BN_num_bits_ptr
#define CRYPTO_add_lock CRYPTO_add_lock_ptr
#define CRYPTO_num_locks CRYPTO_num_locks_ptr
#define CRYPTO_set_locking_callback CRYPTO_set_locking_callback_ptr
#define d2i_ASN1_BIT_STRING d2i_ASN1_BIT_STRING_ptr
#define d2i_ASN1_OCTET_STRING d2i_ASN1_OCTET_STRING_ptr
#define d2i_ASN1_type_bytes d2i_ASN1_type_bytes_ptr
#define d2i_BASIC_CONSTRAINTS d2i_BASIC_CONSTRAINTS_ptr
#define d2i_EXTENDED_KEY_USAGE d2i_EXTENDED_KEY_USAGE_ptr
#define d2i_PKCS12 d2i_PKCS12_ptr
#define d2i_PKCS12_bio d2i_PKCS12_bio_ptr
#define d2i_PKCS7 d2i_PKCS7_ptr
#define d2i_PKCS7_bio d2i_PKCS7_bio_ptr
#define d2i_RSAPublicKey d2i_RSAPublicKey_ptr
#define d2i_X509 d2i_X509_ptr
#define d2i_X509_bio d2i_X509_bio_ptr
#define d2i_X509_CRL d2i_X509_CRL_ptr
#define d2i_X509_NAME d2i_X509_NAME_ptr
#define DSA_free DSA_free_ptr
#define DSA_generate_key DSA_generate_key_ptr
#define DSA_generate_parameters_ex DSA_generate_parameters_ex_ptr
#define DSA_new DSA_new_ptr
#define DSA_OpenSSL DSA_OpenSSL_ptr
#define DSA_sign DSA_sign_ptr
#define DSA_size DSA_size_ptr
#define DSA_up_ref DSA_up_ref_ptr
#define DSA_verify DSA_verify_ptr
#define ECDSA_sign ECDSA_sign_ptr
#define ECDSA_size ECDSA_size_ptr
#define ECDSA_verify ECDSA_verify_ptr
#define EC_GFp_mont_method EC_GFp_mont_method_ptr
#define EC_GFp_simple_method EC_GFp_simple_method_ptr
#define EC_GROUP_check EC_GROUP_check_ptr
#define EC_GROUP_free EC_GROUP_free_ptr
#define EC_GROUP_get0_generator EC_GROUP_get0_generator_ptr
#define EC_GROUP_get0_seed EC_GROUP_get0_seed_ptr
#define EC_GROUP_get_cofactor EC_GROUP_get_cofactor_ptr
#define EC_GROUP_get_curve_GFp EC_GROUP_get_curve_GFp_ptr
#define EC_GROUP_get_curve_name EC_GROUP_get_curve_name_ptr
#define EC_GROUP_get_degree EC_GROUP_get_degree_ptr
#define EC_GROUP_get_order EC_GROUP_get_order_ptr
#define EC_GROUP_get_seed_len EC_GROUP_get_seed_len_ptr
#define EC_GROUP_method_of EC_GROUP_method_of_ptr
#define EC_GROUP_new EC_GROUP_new_ptr
#define EC_GROUP_set_curve_GFp EC_GROUP_set_curve_GFp_ptr
#define EC_GROUP_set_generator EC_GROUP_set_generator_ptr
#define EC_GROUP_set_seed EC_GROUP_set_seed_ptr
#define EC_KEY_check_key EC_KEY_check_key_ptr
#define EC_KEY_free EC_KEY_free_ptr
#define EC_KEY_generate_key EC_KEY_generate_key_ptr
#define EC_KEY_get0_group EC_KEY_get0_group_ptr
#define EC_KEY_get0_private_key EC_KEY_get0_private_key_ptr
#define EC_KEY_get0_public_key EC_KEY_get0_public_key_ptr
#define EC_KEY_new EC_KEY_new_ptr
#define EC_KEY_new_by_curve_name EC_KEY_new_by_curve_name_ptr
#define EC_KEY_set_group EC_KEY_set_group_ptr
#define EC_KEY_set_private_key EC_KEY_set_private_key_ptr
#define EC_KEY_set_public_key_affine_coordinates EC_KEY_set_public_key_affine_coordinates_ptr
#define EC_KEY_up_ref EC_KEY_up_ref_ptr
#define EC_METHOD_get_field_type EC_METHOD_get_field_type_ptr
#define EC_POINT_free EC_POINT_free_ptr
#define EC_POINT_get_affine_coordinates_GFp EC_POINT_get_affine_coordinates_GFp_ptr
#define EC_POINT_new EC_POINT_new_ptr
#define EC_POINT_set_affine_coordinates_GFp EC_POINT_set_affine_coordinates_GFp_ptr
#define ERR_clear_error ERR_clear_error_ptr
#define ERR_error_string_n ERR_error_string_n_ptr
#define ERR_get_error ERR_get_error_ptr
#define ERR_load_crypto_strings ERR_load_crypto_strings_ptr
#define ERR_peek_error ERR_peek_error_ptr
#define ERR_peek_last_error ERR_peek_last_error_ptr
#define ERR_put_error ERR_put_error_ptr
#define ERR_reason_error_string ERR_reason_error_string_ptr
#define EVP_aes_128_cbc EVP_aes_128_cbc_ptr
#define EVP_aes_128_ecb EVP_aes_128_ecb_ptr
#define EVP_aes_192_cbc EVP_aes_192_cbc_ptr
#define EVP_aes_192_ecb EVP_aes_192_ecb_ptr
#define EVP_aes_256_cbc EVP_aes_256_cbc_ptr
#define EVP_aes_256_ecb EVP_aes_256_ecb_ptr
#define EVP_CIPHER_CTX_cleanup EVP_CIPHER_CTX_cleanup_ptr
#define EVP_CIPHER_CTX_ctrl EVP_CIPHER_CTX_ctrl_ptr
#define EVP_CIPHER_CTX_init EVP_CIPHER_CTX_init_ptr
#define EVP_CIPHER_CTX_set_key_length EVP_CIPHER_CTX_set_key_length_ptr
#define EVP_CIPHER_CTX_set_padding EVP_CIPHER_CTX_set_padding_ptr
#define EVP_CipherFinal_ex EVP_CipherFinal_ex_ptr
#define EVP_CipherInit_ex EVP_CipherInit_ex_ptr
#define EVP_CipherUpdate EVP_CipherUpdate_ptr
#define EVP_des_cbc EVP_des_cbc_ptr
#define EVP_des_ecb EVP_des_ecb_ptr
#define EVP_des_ede3 EVP_des_ede3_ptr
#define EVP_des_ede3_cbc EVP_des_ede3_cbc_ptr
#define EVP_DigestFinal_ex EVP_DigestFinal_ex_ptr
#define EVP_DigestInit_ex EVP_DigestInit_ex_ptr
#define EVP_DigestUpdate EVP_DigestUpdate_ptr
#define EVP_get_digestbyname EVP_get_digestbyname_ptr
#define EVP_md5 EVP_md5_ptr
#define EVP_MD_CTX_create EVP_MD_CTX_create_ptr
#define EVP_MD_CTX_destroy EVP_MD_CTX_destroy_ptr
#define EVP_MD_size EVP_MD_size_ptr
#define EVP_PKEY_free EVP_PKEY_free_ptr
#define EVP_PKEY_get1_DSA EVP_PKEY_get1_DSA_ptr
#define EVP_PKEY_get1_EC_KEY EVP_PKEY_get1_EC_KEY_ptr
#define EVP_PKEY_get1_RSA EVP_PKEY_get1_RSA_ptr
#define EVP_PKEY_new EVP_PKEY_new_ptr
#define EVP_PKEY_set1_DSA EVP_PKEY_set1_DSA_ptr
#define EVP_PKEY_set1_EC_KEY EVP_PKEY_set1_EC_KEY_ptr
#define EVP_PKEY_set1_RSA EVP_PKEY_set1_RSA_ptr
#define EVP_rc2_cbc EVP_rc2_cbc_ptr
#define EVP_rc2_ecb EVP_rc2_ecb_ptr
#define EVP_sha1 EVP_sha1_ptr
#define EVP_sha256 EVP_sha256_ptr
#define EVP_sha384 EVP_sha384_ptr
#define EVP_sha512 EVP_sha512_ptr
#define EXTENDED_KEY_USAGE_free EXTENDED_KEY_USAGE_free_ptr
#define GENERAL_NAMES_free GENERAL_NAMES_free_ptr
#define HMAC_CTX_cleanup HMAC_CTX_cleanup_ptr
#define HMAC_CTX_init HMAC_CTX_init_ptr
#define HMAC_Final HMAC_Final_ptr
#define HMAC_Init_ex HMAC_Init_ex_ptr
#define HMAC_Update HMAC_Update_ptr
#define i2d_ASN1_INTEGER i2d_ASN1_INTEGER_ptr
#define i2d_ASN1_TYPE i2d_ASN1_TYPE_ptr
#define i2d_PKCS12 i2d_PKCS12_ptr
#define i2d_PKCS7 i2d_PKCS7_ptr
#define i2d_X509 i2d_X509_ptr
#define i2d_X509_PUBKEY i2d_X509_PUBKEY_ptr
#define OBJ_ln2nid OBJ_ln2nid_ptr
#define OBJ_nid2ln OBJ_nid2ln_ptr
#define OBJ_nid2sn OBJ_nid2sn_ptr
#define OBJ_nid2obj OBJ_nid2obj_ptr
#define OBJ_obj2nid OBJ_obj2nid_ptr
#define OBJ_obj2txt OBJ_obj2txt_ptr
#define OBJ_sn2nid OBJ_sn2nid_ptr
#define OBJ_txt2nid OBJ_txt2nid_ptr
#define OBJ_txt2obj OBJ_txt2obj_ptr
#define OPENSSL_add_all_algorithms_conf OPENSSL_add_all_algorithms_conf_ptr
#define PEM_read_bio_PKCS7 PEM_read_bio_PKCS7_ptr
#define PEM_read_bio_X509_AUX PEM_read_bio_X509_AUX_ptr
#define PEM_read_bio_X509_CRL PEM_read_bio_X509_CRL_ptr
#define PEM_write_bio_X509_CRL PEM_write_bio_X509_CRL_ptr
#define PKCS12_create PKCS12_create_ptr
#define PKCS12_free PKCS12_free_ptr
#define PKCS12_parse PKCS12_parse_ptr
#define PKCS7_add_certificate PKCS7_add_certificate_ptr
#define PKCS7_content_new PKCS7_content_new_ptr
#define PKCS7_free PKCS7_free_ptr
#define PKCS7_new PKCS7_new_ptr
#define PKCS7_set_type PKCS7_set_type_ptr
#define RAND_bytes RAND_bytes_ptr
#define RAND_poll RAND_poll_ptr
#define RSA_free RSA_free_ptr
#define RSA_generate_key_ex RSA_generate_key_ex_ptr
#define RSA_get_method RSA_get_method_ptr
#define RSA_new RSA_new_ptr
#define RSA_private_decrypt RSA_private_decrypt_ptr
#define RSA_public_encrypt RSA_public_encrypt_ptr
#define RSA_sign RSA_sign_ptr
#define RSA_size RSA_size_ptr
#define RSA_up_ref RSA_up_ref_ptr
#define RSA_verify RSA_verify_ptr
#define sk_free sk_free_ptr
#define sk_new_null sk_new_null_ptr
#define sk_num sk_num_ptr
#define sk_pop_free sk_pop_free_ptr
#define sk_push sk_push_ptr
#define sk_value sk_value_ptr
#define SSL_CIPHER_description SSL_CIPHER_description_ptr
#define SSL_ctrl SSL_ctrl_ptr
#define SSL_set_quiet_shutdown SSL_set_quiet_shutdown_ptr
#define SSL_CTX_check_private_key SSL_CTX_check_private_key_ptr
#define SSL_CTX_ctrl SSL_CTX_ctrl_ptr
#define SSL_CTX_free SSL_CTX_free_ptr
#define SSL_CTX_new SSL_CTX_new_ptr
#define SSL_CTX_set_alpn_protos SSL_CTX_set_alpn_protos_ptr
#define SSL_CTX_set_alpn_select_cb SSL_CTX_set_alpn_select_cb_ptr
#define SSL_CTX_set_cert_verify_callback SSL_CTX_set_cert_verify_callback_ptr
#define SSL_CTX_set_cipher_list SSL_CTX_set_cipher_list_ptr
#define SSL_CTX_set_client_cert_cb SSL_CTX_set_client_cert_cb_ptr
#define SSL_CTX_set_quiet_shutdown SSL_CTX_set_quiet_shutdown_ptr
#define SSL_CTX_set_verify SSL_CTX_set_verify_ptr
#define SSL_CTX_use_certificate SSL_CTX_use_certificate_ptr
#define SSL_CTX_use_PrivateKey SSL_CTX_use_PrivateKey_ptr
#define SSL_do_handshake SSL_do_handshake_ptr
#define SSL_free SSL_free_ptr
#define SSL_get_client_CA_list SSL_get_client_CA_list_ptr
#define SSL_get_current_cipher SSL_get_current_cipher_ptr
#define SSL_get_error SSL_get_error_ptr
#define SSL_get_finished SSL_get_finished_ptr
#define SSL_get_peer_cert_chain SSL_get_peer_cert_chain_ptr
#define SSL_get_peer_certificate SSL_get_peer_certificate_ptr
#define SSL_get_peer_finished SSL_get_peer_finished_ptr
#define SSL_get_SSL_CTX SSL_get_SSL_CTX_ptr
#define SSL_get_version SSL_get_version_ptr
#define SSL_get0_alpn_selected SSL_get0_alpn_selected_ptr
#define SSL_library_init SSL_library_init_ptr
#define SSL_load_error_strings SSL_load_error_strings_ptr
#define SSL_new SSL_new_ptr
#define SSL_read SSL_read_ptr
#define SSL_renegotiate_pending SSL_renegotiate_pending_ptr
#define SSL_set_accept_state SSL_set_accept_state_ptr
#define SSL_set_bio SSL_set_bio_ptr
#define SSL_set_connect_state SSL_set_connect_state_ptr
#define SSL_shutdown SSL_shutdown_ptr
#define SSL_state SSL_state_ptr
#define SSLeay_version SSLeay_version_ptr
#define SSLv23_method SSLv23_method_ptr
#define SSL_write SSL_write_ptr
#define TLSv1_1_method TLSv1_1_method_ptr
#define TLSv1_2_method TLSv1_2_method_ptr
#define TLSv1_method TLSv1_method_ptr
#define X509_check_issued X509_check_issued_ptr
#define X509_check_purpose X509_check_purpose_ptr
#define X509_CRL_free X509_CRL_free_ptr
#define X509_digest X509_digest_ptr
#define X509_dup X509_dup_ptr
#define X509_EXTENSION_create_by_OBJ X509_EXTENSION_create_by_OBJ_ptr
#define X509_EXTENSION_free X509_EXTENSION_free_ptr
#define X509_EXTENSION_get_critical X509_EXTENSION_get_critical_ptr
#define X509_EXTENSION_get_data X509_EXTENSION_get_data_ptr
#define X509_EXTENSION_get_object X509_EXTENSION_get_object_ptr
#define X509_free X509_free_ptr
#define X509_get_default_cert_dir X509_get_default_cert_dir_ptr
#define X509_get_default_cert_dir_env X509_get_default_cert_dir_env_ptr
#define X509_get_default_cert_file X509_get_default_cert_file_ptr
#define X509_get_default_cert_file_env X509_get_default_cert_file_env_ptr
#define X509_get_ext X509_get_ext_ptr
#define X509_get_ext_count X509_get_ext_count_ptr
#define X509_get_ext_d2i X509_get_ext_d2i_ptr
#define X509_get_issuer_name X509_get_issuer_name_ptr
#define X509_get_serialNumber X509_get_serialNumber_ptr
#define X509_get_subject_name X509_get_subject_name_ptr
#define X509_issuer_name_hash X509_issuer_name_hash_ptr
#define X509_NAME_entry_count X509_NAME_entry_count_ptr
#define X509_NAME_ENTRY_get_data X509_NAME_ENTRY_get_data_ptr
#define X509_NAME_ENTRY_get_object X509_NAME_ENTRY_get_object_ptr
#define X509_NAME_free X509_NAME_free_ptr
#define X509_NAME_get_entry X509_NAME_get_entry_ptr
#define X509_NAME_get_index_by_NID X509_NAME_get_index_by_NID_ptr
#define X509_PUBKEY_get X509_PUBKEY_get_ptr
#define X509_STORE_add_cert X509_STORE_add_cert_ptr
#define X509_STORE_add_crl X509_STORE_add_crl_ptr
#define X509_STORE_CTX_free X509_STORE_CTX_free_ptr
#define X509_STORE_CTX_get0_param X509_STORE_CTX_get0_param_ptr
#define X509_STORE_CTX_get1_chain X509_STORE_CTX_get1_chain_ptr
#define X509_STORE_CTX_get_error X509_STORE_CTX_get_error_ptr
#define X509_STORE_CTX_get_error_depth X509_STORE_CTX_get_error_depth_ptr
#define X509_STORE_CTX_init X509_STORE_CTX_init_ptr
#define X509_STORE_CTX_new X509_STORE_CTX_new_ptr
#define X509_STORE_CTX_set_flags X509_STORE_CTX_set_flags_ptr
#define X509_STORE_CTX_set_verify_cb X509_STORE_CTX_set_verify_cb_ptr
#define X509_STORE_free X509_STORE_free_ptr
#define X509_STORE_new X509_STORE_new_ptr
#define X509_STORE_set_flags X509_STORE_set_flags_ptr
#define X509V3_EXT_print X509V3_EXT_print_ptr
#define X509_verify_cert X509_verify_cert_ptr
#define X509_verify_cert_error_string X509_verify_cert_error_string_ptr
#define X509_VERIFY_PARAM_set_time X509_VERIFY_PARAM_set_time_ptr
#define EC_GF2m_simple_method EC_GF2m_simple_method_ptr
#define EC_GROUP_get_curve_GF2m EC_GROUP_get_curve_GF2m_ptr
#define EC_GROUP_set_curve_GF2m EC_GROUP_set_curve_GF2m_ptr
#define EC_POINT_get_affine_coordinates_GF2m EC_POINT_get_affine_coordinates_GF2m_ptr
#define EC_POINT_set_affine_coordinates_GF2m EC_POINT_set_affine_coordinates_GF2m_ptr

#else // FEATURE_DISTRO_AGNOSTIC_SSL

#define API_EXISTS(fn) true

#endif // FEATURE_DISTRO_AGNOSTIC_SSL
