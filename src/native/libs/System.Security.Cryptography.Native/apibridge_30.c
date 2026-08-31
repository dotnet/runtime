// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "opensslshim.h"
#include "pal_crypto_types.h"
#include "pal_types.h"

#include "../Common/pal_safecrt.h"
#include <assert.h>

#if defined NEED_OPENSSL_1_0 || defined NEED_OPENSSL_1_1

#include "apibridge_30.h"

// 1.0 and 1.1 agree on the values of the EVP_PKEY_ values, but some of them changed in 3.0.
// If we're running on 3.0 we already call the real methods, not these fallbacks, so we need to always use
// the 1.0/1.1 values here.

// These values are in common.
c_static_assert(EVP_PKEY_CTRL_MD == 1);
c_static_assert(EVP_PKEY_CTRL_RSA_KEYGEN_BITS == 0x1003);
c_static_assert(EVP_PKEY_CTRL_RSA_OAEP_MD == 0x1009);
c_static_assert(EVP_PKEY_CTRL_RSA_PADDING == 0x1001);
c_static_assert(EVP_PKEY_CTRL_RSA_PSS_SALTLEN == 0x1002);
c_static_assert(EVP_PKEY_OP_KEYGEN == (1 << 2));
c_static_assert(EVP_PKEY_RSA == 6);

#if OPENSSL_VERSION_NUMBER < OPENSSL_VERSION_3_0_RTM

c_static_assert(EVP_PKEY_OP_SIGN == (1 << 3));
c_static_assert(EVP_PKEY_OP_VERIFY == (1 << 4));
c_static_assert(EVP_PKEY_OP_TYPE_CRYPT == ((1 << 8) | (1 << 9)));
c_static_assert(EVP_PKEY_OP_TYPE_SIG == 0xF8);

#else

#undef EVP_PKEY_OP_SIGN
#define EVP_PKEY_OP_SIGN (1 << 3)
#undef EVP_PKEY_OP_VERIFY
#define EVP_PKEY_OP_VERIFY (1 << 4)
#undef EVP_PKEY_OP_TYPE_CRYPT
#define EVP_PKEY_OP_TYPE_CRYPT ((1 << 8) | (1 << 9))
#undef EVP_PKEY_OP_TYPE_SIG
#define EVP_PKEY_OP_TYPE_SIG 0xF8 // OP_SIGN | OP_VERIFY | OP_VERIFYRECOVER | OP_SIGNCTX | OP_VERIFYCTX

#endif

int local_EVP_PKEY_CTX_set_rsa_keygen_bits(EVP_PKEY_CTX* ctx, int bits)
{
    return RSA_pkey_ctx_ctrl(ctx, EVP_PKEY_OP_KEYGEN, EVP_PKEY_CTRL_RSA_KEYGEN_BITS, bits, NULL);
}

int local_EVP_PKEY_CTX_set_rsa_oaep_md(EVP_PKEY_CTX* ctx, const EVP_MD* md)
{
    // set_rsa_oaep_md doesn't route through RSA_pkey_ctx_ctrl n 1.1, unlike the other set_rsa operations.
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wcast-qual"
    return EVP_PKEY_CTX_ctrl(ctx, EVP_PKEY_RSA, EVP_PKEY_OP_TYPE_CRYPT, EVP_PKEY_CTRL_RSA_OAEP_MD, 0, (void*)md);
#pragma clang diagnostic pop
}

int local_EVP_PKEY_CTX_set_rsa_padding(EVP_PKEY_CTX* ctx, int pad_mode)
{
    return RSA_pkey_ctx_ctrl(ctx, -1, EVP_PKEY_CTRL_RSA_PADDING, pad_mode, NULL);
}

int local_EVP_PKEY_CTX_set_rsa_pss_saltlen(EVP_PKEY_CTX* ctx, int saltlen)
{
    return RSA_pkey_ctx_ctrl(
        ctx, (EVP_PKEY_OP_SIGN | EVP_PKEY_OP_VERIFY), EVP_PKEY_CTRL_RSA_PSS_SALTLEN, saltlen, NULL);
}

int local_EVP_PKEY_CTX_set_signature_md(EVP_PKEY_CTX* ctx, const EVP_MD* md)
{
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wcast-qual"
    return EVP_PKEY_CTX_ctrl(ctx, -1, EVP_PKEY_OP_TYPE_SIG, EVP_PKEY_CTRL_MD, 0, (void*)md);
#pragma clang diagnostic pop
}

#endif // defined NEED_OPENSSL_1_0 || defined NEED_OPENSSL_1_1

#ifdef NEED_OPENSSL_3_0

#include "apibridge_30_rev.h"

void local_ERR_put_error(int32_t lib, int32_t func, int32_t reason, const char* file, int32_t line)
{
    // In portable builds, ensure that we found the 3.0 error reporting functions.
    // In non-portable builds, this is just assert(true), but then we call the functions,
    // so the compiler ensures they're there anyways.
    assert(API_EXISTS(ERR_new) && API_EXISTS(ERR_set_debug) && API_EXISTS(ERR_set_error));
    ERR_new();

    // ERR_set_debug saves only the pointer, not the value, as it expects constants.
    // So just ignore the legacy numeric code, and use the 3.0 "Uh, I don't know"
    // function name.
    (void)func;
    ERR_set_debug(file, line, "(unknown function)");

    ERR_set_error(lib, reason, NULL);
}

#endif // defined NEED_OPENSSL_3_0
