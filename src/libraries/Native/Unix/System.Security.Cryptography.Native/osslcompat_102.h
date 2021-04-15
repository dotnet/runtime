// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#pragma once

// Function prototypes unique to OpenSSL 1.0.2

typedef struct stack_st _STACK;

#undef CRYPTO_num_locks
#undef CRYPTO_set_locking_callback
#undef ERR_load_crypto_strings
#undef EVP_CIPHER_CTX_cleanup
#undef EVP_CIPHER_CTX_init
#undef OPENSSL_add_all_algorithms_conf
#undef SSL_library_init
#undef SSL_load_error_strings
#undef SSL_state
#undef SSLeay

int CRYPTO_add_lock(int* pointer, int amount, int type, const char* file, int line);
int CRYPTO_num_locks(void);
void CRYPTO_set_locking_callback(void (*func)(int mode, int type, const char* file, int line));
void ERR_load_crypto_strings(void);
int EVP_CIPHER_CTX_cleanup(EVP_CIPHER_CTX* a);
int EVP_CIPHER_CTX_init(EVP_CIPHER_CTX* a);
void HMAC_CTX_cleanup(HMAC_CTX* ctx);
void HMAC_CTX_init(HMAC_CTX* ctx);
void OPENSSL_add_all_algorithms_conf(void);
int SSL_library_init(void);
void SSL_load_error_strings(void);
int SSL_state(const SSL* ssl);
unsigned long SSLeay(void);
