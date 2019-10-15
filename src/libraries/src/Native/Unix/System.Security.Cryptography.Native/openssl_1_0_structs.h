// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Definitions of structures from OpenSSL 1.0.2, modified as relevant to
// building .NET Core.

// The CRYPTO_EX_DATA struct is smaller in 1.1, which changes the packing of
// dsa_st
struct crypto_ex_data_10_st
{
    STACK_OF(void) * sk;
    int dummy;
};

struct dsa_st
{
    int _ignored0;
    long _ignored1;
    int _ignored2;
    BIGNUM* p;
    BIGNUM* q;
    BIGNUM* g;
    BIGNUM* pub_key;
    BIGNUM* priv_key;
    const void* _ignored3;
    const void* _ignored4;
    int _ignored5;
    const void* _ignored6;
    int _ignored7;
    struct crypto_ex_data_10_st ex_data;
    const DSA_METHOD* meth;
};

struct evp_cipher_ctx_st
{
    // 0xA8 is the sizeof value when building against OpenSSL 1.0.2 on
    // Ubuntu 16.04
    unsigned char _ignored0[0xA8];
};

struct evp_pkey_st
{
    int _ignored0;
    int _ignored1;
    int references;
};

struct hmac_ctx_st
{
    // 0x120 is the sizeof value when building against OpenSSL 1.0.2 on
    // Ubuntu 16.04
    unsigned char _ignored0[0x120];
};

struct rsa_meth_st
{
    const void* _ignored0;
    const void* _ignored1;
    const void* _ignored2;
    const void* _ignored3;
    const void* _ignored4;
    const void* _ignored5;
    const void* _ignored6;
    const void* _ignored7;
    const void* _ignored8;
    int flags;
};

struct rsa_st
{
    int _ignored0;
    long _ignored1;
    const RSA_METHOD* meth;
    const void* _ignored2;
    BIGNUM* n;
    BIGNUM* e;
    BIGNUM* d;
    BIGNUM* p;
    BIGNUM* q;
    BIGNUM* dmp1;
    BIGNUM* dmq1;
    BIGNUM* iqmp;
};

struct x509_cinf_st
{
    ASN1_INTEGER* version;
    ASN1_INTEGER* serialNumber;
    X509_ALGOR* signature;
    X509_NAME* issuer;
    X509_VAL* validity;
    X509_NAME* subject;
    X509_PUBKEY* key;
};

struct X509_crl_info_st
{
    const void* _ignored0;
    const void* _ignored1;
    const void* _ignored2;
    const void* _ignored3;
    ASN1_TIME* nextUpdate;
};

struct X509_crl_st
{
    X509_CRL_INFO* crl;
};

struct X509_name_st
{
    STACK_OF(X509_NAME_ENTRY) * entries;
    int _ignored0;
    BUF_MEM* bytes;
};

struct X509_pubkey_st
{
    X509_ALGOR* algor;
    ASN1_BIT_STRING* public_key;
};

struct x509_st
{
    X509_CINF* cert_info;
    const void* _ignored0;
    const void* _ignored1;
    int _ignored2;
    int references;
};

struct x509_store_ctx_st
{
    X509_STORE* ctx;
    int _ignored1;
    X509* cert;
    STACK_OF(X509*) untrusted;
    const void* _ignored2;
    const void* _ignored3;
    const void* _ignored4;
    // For comparison purposes to the 1.0.x headers:
    // BEGIN FUNCTION POINTERS
    const void* _ignored5;
    const void* _ignored6;
    const void* _ignored7;
    const void* _ignored8;
    const void* _ignored9;
    const void* _ignored10;
    const void* _ignored11;
    const void* _ignored12;
    const void* _ignored13;
    const void* _ignored14;
    const void* _ignored15;
    const void* _ignored16;
    // END FUNCTION POINTERS
    int _ignored17;
    int _ignored18;
    STACK_OF(X509*) chain;
};

struct x509_store_st
{
    int _ignored0;
    const void* _ignored1;
    const void* _ignored2;
    X509_VERIFY_PARAM* param;
};
