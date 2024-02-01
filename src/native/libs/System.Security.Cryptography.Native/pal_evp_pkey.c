// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include "pal_evp_pkey.h"

EVP_PKEY* CryptoNative_EvpPkeyCreate(void)
{
    ERR_clear_error();
    return EVP_PKEY_new();
}

EVP_PKEY* CryptoNative_EvpPKeyDuplicate(EVP_PKEY* currentKey, int32_t algId)
{
    assert(currentKey != NULL);

    ERR_clear_error();

    int currentAlgId = EVP_PKEY_get_base_id(currentKey);

    if (algId != NID_undef && algId != currentAlgId)
    {
        ERR_put_error(ERR_LIB_EVP, 0, EVP_R_DIFFERENT_KEY_TYPES, __FILE__, __LINE__);
        return NULL;
    }

    EVP_PKEY* newKey = EVP_PKEY_new();

    if (newKey == NULL)
    {
        return NULL;
    }

    bool success = true;

    if (currentAlgId == EVP_PKEY_RSA)
    {
        const RSA* rsa = EVP_PKEY_get0_RSA(currentKey);

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wcast-qual"
        if (rsa == NULL || !EVP_PKEY_set1_RSA(newKey, (RSA*)rsa))
#pragma clang diagnostic pop
        {
            success = false;
        }
    }
    else
    {
        ERR_put_error(ERR_LIB_EVP, 0, EVP_R_UNSUPPORTED_ALGORITHM, __FILE__, __LINE__);
        success = false;
    }

    if (!success)
    {
        EVP_PKEY_free(newKey);
        newKey = NULL;
    }

    return newKey;
}

void CryptoNative_EvpPkeyDestroy(EVP_PKEY* pkey)
{
    if (pkey != NULL)
    {
        EVP_PKEY_free(pkey);
    }
}

int32_t CryptoNative_EvpPKeySize(EVP_PKEY* pkey)
{
    // This function is not expected to populate the error queue with
    // any errors, but it's technically possible that an external
    // ENGINE or OSSL_PROVIDER populate the queue in their implementation,
    // but the calling code does not check for one.
    assert(pkey != NULL);
    return EVP_PKEY_get_size(pkey);
}

int32_t CryptoNative_UpRefEvpPkey(EVP_PKEY* pkey)
{
    if (!pkey)
    {
        return 0;
    }

    // No error queue impact.
    return EVP_PKEY_up_ref(pkey);
}

static bool Lcm(const BIGNUM* num1, const BIGNUM* num2, BN_CTX* ctx, BIGNUM* result)
{
    assert(result);

    // lcm(num1, num2) = (num1 * num2) / gcd(num1, num2)
    BIGNUM* mul = NULL;
    BIGNUM* gcd = NULL;
    bool ret = false;

    if ((mul = BN_new()) == NULL ||
        (gcd = BN_new()) == NULL ||
        !BN_mul(mul, num1, num2, ctx) ||
        !BN_gcd(gcd, num1, num2, ctx) ||
        !BN_div(result, NULL, mul, gcd, ctx))
    {
        goto done;
    }

    ret = true;
done:
    BN_clear_free(mul);
    BN_clear_free(gcd);
    return ret;
}

static int32_t QuickRsaCheck(const RSA* rsa, bool isPublic)
{
    // This method does some lightweight key consistency checks on an RSA key to make sure all supplied values are
    // sensible. This is not intended to be a strict key check that verifies a key conforms to any particular set
    // of criteria or standards.

    const BIGNUM* n = NULL;
    const BIGNUM* e = NULL;
    const BIGNUM* d = NULL;
    const BIGNUM* p = NULL;
    const BIGNUM* q = NULL;
    const BIGNUM* dp = NULL;
    const BIGNUM* dq = NULL;
    const BIGNUM* inverseQ = NULL;
    BN_CTX* ctx = NULL;

    // x and y are scratch integers that receive the result of some operations.
    BIGNUM* x = NULL;
    BIGNUM* y = NULL;

    // p1 and q1 are to hold p-1 and q-1, respectively. We need these values a couple of times, so don't waste time
    // recomputing them in scratch integers.
    BIGNUM* p1 = NULL;
    BIGNUM* q1 = NULL;
    int ret = 0;

    RSA_get0_key(rsa, &n, &e, &d);

    // Always need public parameters.
    if (!n || !e)
    {
        ERR_PUT_error(ERR_LIB_RSA, 0, RSA_R_VALUE_MISSING, __FILE__, __LINE__);
        goto done;
    }

    // Compatibility: We put this error for OpenSSL 1.0.2 and 1.1.x when the modulus is zero because OpenSSL did not
    // handle this correctly. Continue to use the same error if the modulus is zero.
    if (BN_is_zero(n))
    {
        ERR_put_error(ERR_LIB_EVP, 0, EVP_R_DECODE_ERROR, __FILE__, __LINE__);
        goto done;
    }

    // OpenSSL has kept this value at 16,384 for all versions.
    if (BN_num_bits(n) > OPENSSL_RSA_MAX_MODULUS_BITS)
    {
        ERR_PUT_error(ERR_LIB_RSA, 0, RSA_R_MODULUS_TOO_LARGE, __FILE__, __LINE__);
        goto done;
    }

    // Exponent cannot be 1 and must be odd
    if (BN_is_one(e) || !BN_is_odd(e))
    {
        ERR_PUT_error(ERR_LIB_RSA, 0, RSA_R_BAD_E_VALUE, __FILE__, __LINE__);
        goto done;
    }

    // At this point everything that is public has been checked. Mark as successful and clean up.
    if (isPublic)
    {
        ret = 1;
        goto done;
    }

    // We do not support validating multi-prime RSA. If there are extra primes (more than two) then treat it as a
    // decoding failure.
    if (RSA_get_multi_prime_extra_count(rsa) != 0)
    {
        ERR_put_error(ERR_LIB_EVP, 0, EVP_R_DECODE_ERROR, __FILE__, __LINE__);
        goto done;
    }

    // Get the private components now that we've moved on to checking the private parameters.
    RSA_get0_factors(rsa, &p, &q);

    // Need all the private parameters now.
    if (!d || !p || !q)
    {
        ERR_PUT_error(ERR_LIB_RSA, 0, RSA_R_VALUE_MISSING, __FILE__, __LINE__);
        goto done;
    }

    ctx = BN_CTX_new();

    // Setup the scratch integers
    if ((x = BN_new()) == NULL || (y = BN_new()) == NULL || (p1 = BN_new()) == NULL || (q1 = BN_new()) == NULL)
    {
        goto done;
    }

    // multiply p and q and put the result in x.
    if (!BN_mul(x, p, q, ctx))
    {
        goto done;
    }

    // p * q == n
    if (BN_cmp(x, n) != 0)
    {
        ERR_PUT_error(ERR_LIB_RSA, 0, RSA_R_N_DOES_NOT_EQUAL_P_Q, __FILE__, __LINE__);
        goto done;
    }

    // Checking congruence of private parameters.
    // de = 1 % lambda(n)
    // lambda(n) = lcm(p-1, q-1)
    // lambda(n) is known to be lambda(pq) already.
    // p1 = p-1
    // q1 = q-1
    // x = lcm(x, y)
    if (!BN_sub(p1, p, BN_value_one()) || !BN_sub(q1, q, BN_value_one()) || !Lcm(p1, q1, ctx, x))
    {
        goto done;
    }

    // de % lambda(n)
    // put the result in y
    if (!BN_mod_mul(y, d, e, x, ctx))
    {
        goto done;
    }

    // Given de = 1 % lambda(n), de % lambda(n) should be one.
    if (!BN_is_one(y))
    {
        ERR_PUT_error(ERR_LIB_RSA, 0, RSA_R_D_E_NOT_CONGRUENT_TO_1, __FILE__, __LINE__);
        goto done;
    }

    // Move on to checking the CRT parameters. In compatibility with what OpenSSL does,
    // these are optional and only check them if all are present.
    RSA_get0_crt_params(rsa, &dp, &dq, &inverseQ);

    if (dp && dq && inverseQ)
    {
        // Check dp = d % (p-1)
        // compute d % (p-1) and put in x
        if (!BN_div(NULL, x, d, p1, ctx))
        {
            goto done;
        }

        if (BN_cmp(x, dp) != 0)
        {
            ERR_PUT_error(ERR_LIB_RSA, 0, RSA_R_DMP1_NOT_CONGRUENT_TO_D, __FILE__, __LINE__);
            goto done;
        }

        // Check dq = d % (q-1)
        // compute d % (q-1) and put in x
        if (!BN_div(NULL, x, d, q1, ctx))
        {
            goto done;
        }

        if (BN_cmp(x, dq) != 0)
        {
            ERR_PUT_error(ERR_LIB_RSA, 0, RSA_R_DMQ1_NOT_CONGRUENT_TO_D, __FILE__, __LINE__);
            goto done;
        }

        // Check inverseQ = q^-1 % p
        // Use mod_inverse and put the result in x.
        if (!BN_mod_inverse(x, q, p, ctx))
        {
            goto done;
        }

        if (BN_cmp(x, inverseQ) != 0)
        {
            ERR_PUT_error(ERR_LIB_RSA, 0, RSA_R_IQMP_NOT_INVERSE_OF_Q, __FILE__, __LINE__);
            goto done;
        }
    }

    // If we made it to the end, everything looks good.
    ret = 1;
done:
    if (x) BN_clear_free(x);
    if (y) BN_clear_free(y);
    if (p1) BN_clear_free(p1);
    if (q1) BN_clear_free(q1);
    if (ctx) BN_CTX_free(ctx);
    return ret;
}

static bool CheckKey(EVP_PKEY* key, int32_t algId, bool isPublic, int32_t (*check_func)(EVP_PKEY_CTX*))
{
    if (algId != NID_undef && EVP_PKEY_get_base_id(key) != algId)
    {
        ERR_put_error(ERR_LIB_EVP, 0, EVP_R_UNSUPPORTED_ALGORITHM, __FILE__, __LINE__);
        return false;
    }

    // OpenSSL 1.x does not fail when importing a key with a zero modulus. It fails at key-usage time with an
    // out-of-memory error. For RSA keys, check the modulus for zero and report an invalid key.
    // OpenSSL 3 correctly fails with with an invalid modulus error.
    if (algId == NID_rsaEncryption)
    {
        const RSA* rsa = EVP_PKEY_get0_RSA(key);

        // If we can get the RSA object, use that for a faster path to validating the key that skips primality tests.
        if (rsa != NULL)
        {
            return QuickRsaCheck(rsa, isPublic) == 1;
        }
    }

    EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new(key, NULL);

    if (ctx == NULL)
    {
        // The malloc error should have already been set.
        return false;
    }

    int check = check_func(ctx);
    EVP_PKEY_CTX_free(ctx);

    // 1: Success
    // -2: The key object had no check routine available.
    if (check == 1 || check == -2)
    {
        // We need to clear for -2, doesn't hurt for 1.
        ERR_clear_error();
        return true;
    }

    return false;
}

EVP_PKEY* CryptoNative_DecodeSubjectPublicKeyInfo(const uint8_t* buf, int32_t len, int32_t algId)
{
    assert(buf != NULL);
    assert(len > 0);

    ERR_clear_error();

    EVP_PKEY* key = d2i_PUBKEY(NULL, &buf, len);

    if (key != NULL && !CheckKey(key, algId, true, EVP_PKEY_public_check))
    {
        EVP_PKEY_free(key);
        key = NULL;
    }

    return key;
}

EVP_PKEY* CryptoNative_DecodePkcs8PrivateKey(const uint8_t* buf, int32_t len, int32_t algId)
{
    assert(buf != NULL);
    assert(len > 0);

    ERR_clear_error();

    PKCS8_PRIV_KEY_INFO* p8info = d2i_PKCS8_PRIV_KEY_INFO(NULL, &buf, len);

    if (p8info == NULL)
    {
        return NULL;
    }

    EVP_PKEY* key = EVP_PKCS82PKEY(p8info);
    PKCS8_PRIV_KEY_INFO_free(p8info);

    if (key != NULL && !CheckKey(key, algId, false, EVP_PKEY_check))
    {
        EVP_PKEY_free(key);
        key = NULL;
    }

    return key;
}

int32_t CryptoNative_GetPkcs8PrivateKeySize(EVP_PKEY* pkey, int32_t* p8size)
{
    assert(pkey != NULL);
    assert(p8size != NULL);

    *p8size = 0;
    ERR_clear_error();

    PKCS8_PRIV_KEY_INFO* p8 = EVP_PKEY2PKCS8(pkey);

    if (p8 == NULL)
    {
        // OpenSSL 1.1 and 3 have a behavioral change with EVP_PKEY2PKCS8
        // with regard to handling EVP_PKEYs that do not contain a private key.
        //
        // In OpenSSL 1.1, it would always succeed, but the private parameters
        // would be missing (thus making an invalid PKCS8 structure).
        // Over in the managed side, we detect these invalid PKCS8 blobs and
        // convert that to a "no private key" error.
        //
        // In OpenSSL 3, this now correctly errors, with the error
        // ASN1_R_ILLEGAL_ZERO_CONTENT. We want to preserve allocation failures
        // as OutOfMemoryException. So we peek at the error. If it's a malloc
        // failure, -1 is returned to indcate "throw what is on the error queue".
        // If the error is not a malloc failure, return -2 to mean "no private key".
        // If OpenSSL ever changes the error to something more to explicitly mean
        // "no private key" then we should test for that explicitly. Until then,
        // we treat all errors, except a malloc error, to mean "no private key".

        const char* file = NULL;
        int line = 0;
        unsigned long error = ERR_peek_error_line(&file, &line);

        // If it's not a malloc failure, assume it's because the private key is
        // missing.
        if (ERR_GET_REASON(error) != ERR_R_MALLOC_FAILURE)
        {
            ERR_clear_error();
            return -2;
        }

        // It is a malloc failure. Clear the error queue and set the error
        // as a malloc error so it's the only error in the queue.
        ERR_clear_error();
        ERR_put_error(ERR_GET_LIB(error), 0, ERR_R_MALLOC_FAILURE, file, line);

        // Since ERR_peek_error() matches what exception is thrown, leave the OOM on top.
        return -1;
    }

    *p8size = i2d_PKCS8_PRIV_KEY_INFO(p8, NULL);
    PKCS8_PRIV_KEY_INFO_free(p8);

    return *p8size < 0 ? -1 : 1;
}

int32_t CryptoNative_EncodePkcs8PrivateKey(EVP_PKEY* pkey, uint8_t* buf)
{
    assert(pkey != NULL);
    assert(buf != NULL);

    ERR_clear_error();

    PKCS8_PRIV_KEY_INFO* p8 = EVP_PKEY2PKCS8(pkey);

    if (p8 == NULL)
    {
        return -1;
    }

    int ret = i2d_PKCS8_PRIV_KEY_INFO(p8, &buf);
    PKCS8_PRIV_KEY_INFO_free(p8);
    return ret;
}

int32_t CryptoNative_GetSubjectPublicKeyInfoSize(EVP_PKEY* pkey)
{
    assert(pkey != NULL);

    ERR_clear_error();
    return i2d_PUBKEY(pkey, NULL);
}

int32_t CryptoNative_EncodeSubjectPublicKeyInfo(EVP_PKEY* pkey, uint8_t* buf)
{
    assert(pkey != NULL);
    assert(buf != NULL);

    ERR_clear_error();
    return i2d_PUBKEY(pkey, &buf);
}

static EVP_PKEY* LoadKeyFromEngine(
    const char* engineName,
    const char* keyName,
    ENGINE_LOAD_KEY_PTR load_func)
{
    ERR_clear_error();

    EVP_PKEY* ret = NULL;
    ENGINE* engine = NULL;

    // Per https://github.com/openssl/openssl/discussions/21427
    // using EVP_PKEY after freeing ENGINE is correct.
    engine = ENGINE_by_id(engineName);

    if (engine != NULL)
    {
        if (ENGINE_init(engine))
        {
            ret = load_func(engine, keyName, NULL, NULL);

            ENGINE_finish(engine);
        }

        ENGINE_free(engine);
    }

    return ret;
}

EVP_PKEY* CryptoNative_LoadPrivateKeyFromEngine(const char* engineName, const char* keyName)
{
    return LoadKeyFromEngine(engineName, keyName, ENGINE_load_private_key);
}

EVP_PKEY* CryptoNative_LoadPublicKeyFromEngine(const char* engineName, const char* keyName)
{
    return LoadKeyFromEngine(engineName, keyName, ENGINE_load_public_key);
}
