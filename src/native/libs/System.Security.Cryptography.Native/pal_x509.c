// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_x509.h"

#include "../Common/pal_safecrt.h"
#include <assert.h>
#include <dirent.h>
#include <stdbool.h>
#include <string.h>
#include <unistd.h>

c_static_assert(PAL_X509_V_OK == X509_V_OK);
c_static_assert(PAL_X509_V_ERR_UNABLE_TO_GET_ISSUER_CERT == X509_V_ERR_UNABLE_TO_GET_ISSUER_CERT);
c_static_assert(PAL_X509_V_ERR_UNABLE_TO_GET_CRL == X509_V_ERR_UNABLE_TO_GET_CRL);
c_static_assert(PAL_X509_V_ERR_UNABLE_TO_DECRYPT_CRL_SIGNATURE == X509_V_ERR_UNABLE_TO_DECRYPT_CRL_SIGNATURE);
c_static_assert(PAL_X509_V_ERR_UNABLE_TO_DECODE_ISSUER_PUBLIC_KEY == X509_V_ERR_UNABLE_TO_DECODE_ISSUER_PUBLIC_KEY);
c_static_assert(PAL_X509_V_ERR_CERT_SIGNATURE_FAILURE == X509_V_ERR_CERT_SIGNATURE_FAILURE);
c_static_assert(PAL_X509_V_ERR_CRL_SIGNATURE_FAILURE == X509_V_ERR_CRL_SIGNATURE_FAILURE);
c_static_assert(PAL_X509_V_ERR_CERT_NOT_YET_VALID == X509_V_ERR_CERT_NOT_YET_VALID);
c_static_assert(PAL_X509_V_ERR_CERT_HAS_EXPIRED == X509_V_ERR_CERT_HAS_EXPIRED);
c_static_assert(PAL_X509_V_ERR_CRL_NOT_YET_VALID == X509_V_ERR_CRL_NOT_YET_VALID);
c_static_assert(PAL_X509_V_ERR_CRL_HAS_EXPIRED == X509_V_ERR_CRL_HAS_EXPIRED);
c_static_assert(PAL_X509_V_ERR_ERROR_IN_CERT_NOT_BEFORE_FIELD == X509_V_ERR_ERROR_IN_CERT_NOT_BEFORE_FIELD);
c_static_assert(PAL_X509_V_ERR_ERROR_IN_CERT_NOT_AFTER_FIELD == X509_V_ERR_ERROR_IN_CERT_NOT_AFTER_FIELD);
c_static_assert(PAL_X509_V_ERR_ERROR_IN_CRL_LAST_UPDATE_FIELD == X509_V_ERR_ERROR_IN_CRL_LAST_UPDATE_FIELD);
c_static_assert(PAL_X509_V_ERR_ERROR_IN_CRL_NEXT_UPDATE_FIELD == X509_V_ERR_ERROR_IN_CRL_NEXT_UPDATE_FIELD);
c_static_assert(PAL_X509_V_ERR_OUT_OF_MEM == X509_V_ERR_OUT_OF_MEM);
c_static_assert(PAL_X509_V_ERR_DEPTH_ZERO_SELF_SIGNED_CERT == X509_V_ERR_DEPTH_ZERO_SELF_SIGNED_CERT);
c_static_assert(PAL_X509_V_ERR_SELF_SIGNED_CERT_IN_CHAIN == X509_V_ERR_SELF_SIGNED_CERT_IN_CHAIN);
c_static_assert(PAL_X509_V_ERR_UNABLE_TO_GET_ISSUER_CERT_LOCALLY == X509_V_ERR_UNABLE_TO_GET_ISSUER_CERT_LOCALLY);
c_static_assert(PAL_X509_V_ERR_UNABLE_TO_VERIFY_LEAF_SIGNATURE == X509_V_ERR_UNABLE_TO_VERIFY_LEAF_SIGNATURE);
c_static_assert(PAL_X509_V_ERR_CERT_CHAIN_TOO_LONG == X509_V_ERR_CERT_CHAIN_TOO_LONG);
c_static_assert(PAL_X509_V_ERR_CERT_REVOKED == X509_V_ERR_CERT_REVOKED);
c_static_assert(PAL_X509_V_ERR_PATH_LENGTH_EXCEEDED == X509_V_ERR_PATH_LENGTH_EXCEEDED);
c_static_assert(PAL_X509_V_ERR_INVALID_PURPOSE == X509_V_ERR_INVALID_PURPOSE);
c_static_assert(PAL_X509_V_ERR_CERT_UNTRUSTED == X509_V_ERR_CERT_UNTRUSTED);
c_static_assert(PAL_X509_V_ERR_CERT_REJECTED == X509_V_ERR_CERT_REJECTED);
c_static_assert(PAL_X509_V_ERR_KEYUSAGE_NO_CERTSIGN == X509_V_ERR_KEYUSAGE_NO_CERTSIGN);
c_static_assert(PAL_X509_V_ERR_UNABLE_TO_GET_CRL_ISSUER == X509_V_ERR_UNABLE_TO_GET_CRL_ISSUER);
c_static_assert(PAL_X509_V_ERR_UNHANDLED_CRITICAL_EXTENSION == X509_V_ERR_UNHANDLED_CRITICAL_EXTENSION);
c_static_assert(PAL_X509_V_ERR_KEYUSAGE_NO_CRL_SIGN == X509_V_ERR_KEYUSAGE_NO_CRL_SIGN);
c_static_assert(PAL_X509_V_ERR_UNHANDLED_CRITICAL_CRL_EXTENSION == X509_V_ERR_UNHANDLED_CRITICAL_CRL_EXTENSION);
c_static_assert(PAL_X509_V_ERR_INVALID_NON_CA == X509_V_ERR_INVALID_NON_CA);
c_static_assert(PAL_X509_V_ERR_KEYUSAGE_NO_DIGITAL_SIGNATURE == X509_V_ERR_KEYUSAGE_NO_DIGITAL_SIGNATURE);
c_static_assert(PAL_X509_V_ERR_INVALID_EXTENSION == X509_V_ERR_INVALID_EXTENSION);
c_static_assert(PAL_X509_V_ERR_INVALID_POLICY_EXTENSION == X509_V_ERR_INVALID_POLICY_EXTENSION);
c_static_assert(PAL_X509_V_ERR_NO_EXPLICIT_POLICY == X509_V_ERR_NO_EXPLICIT_POLICY);
c_static_assert(PAL_X509_V_ERR_DIFFERENT_CRL_SCOPE == X509_V_ERR_DIFFERENT_CRL_SCOPE);
c_static_assert(PAL_X509_V_ERR_UNSUPPORTED_EXTENSION_FEATURE == X509_V_ERR_UNSUPPORTED_EXTENSION_FEATURE);
c_static_assert(PAL_X509_V_ERR_UNNESTED_RESOURCE == X509_V_ERR_UNNESTED_RESOURCE);
c_static_assert(PAL_X509_V_ERR_PERMITTED_VIOLATION == X509_V_ERR_PERMITTED_VIOLATION);
c_static_assert(PAL_X509_V_ERR_EXCLUDED_VIOLATION == X509_V_ERR_EXCLUDED_VIOLATION);
c_static_assert(PAL_X509_V_ERR_SUBTREE_MINMAX == X509_V_ERR_SUBTREE_MINMAX);
c_static_assert(PAL_X509_V_ERR_APPLICATION_VERIFICATION == X509_V_ERR_APPLICATION_VERIFICATION);
c_static_assert(PAL_X509_V_ERR_UNSUPPORTED_CONSTRAINT_TYPE == X509_V_ERR_UNSUPPORTED_CONSTRAINT_TYPE);
c_static_assert(PAL_X509_V_ERR_UNSUPPORTED_CONSTRAINT_SYNTAX == X509_V_ERR_UNSUPPORTED_CONSTRAINT_SYNTAX);
c_static_assert(PAL_X509_V_ERR_UNSUPPORTED_NAME_SYNTAX == X509_V_ERR_UNSUPPORTED_NAME_SYNTAX);
c_static_assert(PAL_X509_V_ERR_CRL_PATH_VALIDATION_ERROR == X509_V_ERR_CRL_PATH_VALIDATION_ERROR);
c_static_assert(PAL_X509_V_ERR_SUITE_B_INVALID_VERSION == X509_V_ERR_SUITE_B_INVALID_VERSION);
c_static_assert(PAL_X509_V_ERR_SUITE_B_INVALID_ALGORITHM == X509_V_ERR_SUITE_B_INVALID_ALGORITHM);
c_static_assert(PAL_X509_V_ERR_SUITE_B_INVALID_CURVE == X509_V_ERR_SUITE_B_INVALID_CURVE);
c_static_assert(PAL_X509_V_ERR_SUITE_B_INVALID_SIGNATURE_ALGORITHM == X509_V_ERR_SUITE_B_INVALID_SIGNATURE_ALGORITHM);
c_static_assert(PAL_X509_V_ERR_SUITE_B_LOS_NOT_ALLOWED == X509_V_ERR_SUITE_B_LOS_NOT_ALLOWED);
c_static_assert(PAL_X509_V_ERR_SUITE_B_CANNOT_SIGN_P_384_WITH_P_256 == X509_V_ERR_SUITE_B_CANNOT_SIGN_P_384_WITH_P_256);
c_static_assert(PAL_X509_V_ERR_HOSTNAME_MISMATCH == X509_V_ERR_HOSTNAME_MISMATCH);
c_static_assert(PAL_X509_V_ERR_EMAIL_MISMATCH == X509_V_ERR_EMAIL_MISMATCH);
c_static_assert(PAL_X509_V_ERR_IP_ADDRESS_MISMATCH == X509_V_ERR_IP_ADDRESS_MISMATCH);

EVP_PKEY* CryptoNative_GetX509EvpPublicKey(X509* x509)
{
    if (!x509)
    {
        return NULL;
    }

    // X509_get_X509_PUBKEY returns an interior pointer, so should not be freed
    return X509_PUBKEY_get(X509_get_X509_PUBKEY(x509));
}

X509_CRL* CryptoNative_DecodeX509Crl(const uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return NULL;
    }

    return d2i_X509_CRL(NULL, &buf, len);
}

X509* CryptoNative_DecodeX509(const uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return NULL;
    }

    return d2i_X509(NULL, &buf, len);
}

int32_t CryptoNative_GetX509DerSize(X509* x)
{
    return i2d_X509(x, NULL);
}

int32_t CryptoNative_EncodeX509(X509* x, uint8_t* buf)
{
    return i2d_X509(x, &buf);
}

void CryptoNative_X509Destroy(X509* a)
{
    if (a != NULL)
    {
        X509_free(a);
    }
}

X509* CryptoNative_X509Duplicate(X509* x509)
{
    return X509_dup(x509);
}

X509* CryptoNative_PemReadX509FromBio(BIO* bio)
{
    return PEM_read_bio_X509(bio, NULL, NULL, NULL);
}

X509* CryptoNative_PemReadX509FromBioAux(BIO* bio)
{
    return PEM_read_bio_X509_AUX(bio, NULL, NULL, NULL);
}

ASN1_INTEGER* CryptoNative_X509GetSerialNumber(X509* x509)
{
    return X509_get_serialNumber(x509);
}

X509_NAME* CryptoNative_X509GetIssuerName(X509* x509)
{
    return X509_get_issuer_name(x509);
}

X509_NAME* CryptoNative_X509GetSubjectName(X509* x509)
{
    return X509_get_subject_name(x509);
}

int32_t CryptoNative_X509CheckPurpose(X509* x, int32_t id, int32_t ca)
{
    return X509_check_purpose(x, id, ca);
}

uint64_t CryptoNative_X509IssuerNameHash(X509* x)
{
    return X509_issuer_name_hash(x);
}

int32_t CryptoNative_X509GetExtCount(X509* x)
{
    return X509_get_ext_count(x);
}

X509_EXTENSION* CryptoNative_X509GetExt(X509* x, int32_t loc)
{
    return X509_get_ext(x, loc);
}

ASN1_OBJECT* CryptoNative_X509ExtensionGetOid(X509_EXTENSION* x)
{
    return X509_EXTENSION_get_object(x);
}

ASN1_OCTET_STRING* CryptoNative_X509ExtensionGetData(X509_EXTENSION* x)
{
    return X509_EXTENSION_get_data(x);
}

int32_t CryptoNative_X509ExtensionGetCritical(X509_EXTENSION* x)
{
    return X509_EXTENSION_get_critical(x);
}

ASN1_OCTET_STRING* CryptoNative_X509FindExtensionData(X509* x, int32_t nid)
{
    if (x == NULL || nid == NID_undef)
    {
        return NULL;
    }

    int idx = X509_get_ext_by_NID(x, nid, -1);

    if (idx < 0)
    {
        return NULL;
    }

    X509_EXTENSION* ext = X509_get_ext(x, idx);

    if (ext == NULL)
    {
        return NULL;
    }

    return X509_EXTENSION_get_data(ext);
}

void CryptoNative_X509StoreDestory(X509_STORE* v)
{
    if (v != NULL)
    {
        X509_STORE_free(v);
    }
}

int32_t CryptoNative_X509StoreAddCrl(X509_STORE* ctx, X509_CRL* x)
{
    return X509_STORE_add_crl(ctx, x);
}

int32_t CryptoNative_X509StoreSetRevocationFlag(X509_STORE* ctx, X509RevocationFlag revocationFlag)
{
    unsigned long verifyFlags = X509_V_FLAG_CRL_CHECK;

    if (revocationFlag != EndCertificateOnly)
    {
        verifyFlags |= X509_V_FLAG_CRL_CHECK_ALL;
    }

    return X509_STORE_set_flags(ctx, verifyFlags);
}

X509_STORE_CTX* CryptoNative_X509StoreCtxCreate()
{
    return X509_STORE_CTX_new();
}

void CryptoNative_X509StoreCtxDestroy(X509_STORE_CTX* v)
{
    if (v != NULL)
    {
        X509_STORE_CTX_free(v);
    }
}

int32_t CryptoNative_X509StoreCtxInit(X509_STORE_CTX* ctx, X509_STORE* store, X509* x509, X509Stack* extraStore)
{
    int32_t val = X509_STORE_CTX_init(ctx, store, x509, extraStore);

    if (val != 0)
    {
        X509_STORE_CTX_set_flags(ctx, X509_V_FLAG_CHECK_SS_SIGNATURE);
    }

    return val;
}

int32_t CryptoNative_X509VerifyCert(X509_STORE_CTX* ctx)
{
    return X509_verify_cert(ctx);
}

X509Stack* CryptoNative_X509StoreCtxGetChain(X509_STORE_CTX* ctx)
{
    return X509_STORE_CTX_get1_chain(ctx);
}

X509* CryptoNative_X509StoreCtxGetCurrentCert(X509_STORE_CTX* ctx)
{
    if (ctx == NULL)
    {
        return NULL;
    }

    X509* cert = X509_STORE_CTX_get_current_cert(ctx);

    if (cert != NULL)
    {
        X509_up_ref(cert);
    }

    return cert;
}

X509Stack* CryptoNative_X509StoreCtxGetSharedUntrusted(X509_STORE_CTX* ctx)
{
    if (ctx)
    {
        return X509_STORE_CTX_get0_untrusted(ctx);
    }

    return NULL;
}

int32_t CryptoNative_X509StoreCtxGetError(X509_STORE_CTX* ctx)
{
    return (int32_t)X509_STORE_CTX_get_error(ctx);
}

int32_t CryptoNative_X509StoreCtxReset(X509_STORE_CTX* ctx)
{
    X509* leaf = X509_STORE_CTX_get0_cert(ctx);
    X509Stack* untrusted = X509_STORE_CTX_get0_untrusted(ctx);
    X509_STORE* store = X509_STORE_CTX_get0_store(ctx);

    X509_STORE_CTX_cleanup(ctx);
    return CryptoNative_X509StoreCtxInit(ctx, store, leaf, untrusted);
}

int32_t CryptoNative_X509StoreCtxRebuildChain(X509_STORE_CTX* ctx)
{
    if (!CryptoNative_X509StoreCtxReset(ctx))
    {
        return -1;
    }

    return X509_verify_cert(ctx);
}

void CryptoNative_X509StoreCtxSetVerifyCallback(X509_STORE_CTX* ctx, X509StoreVerifyCallback callback)
{
    X509_STORE_CTX_set_verify_cb(ctx, callback);
}

int32_t CryptoNative_X509StoreCtxGetErrorDepth(X509_STORE_CTX* ctx)
{
    return X509_STORE_CTX_get_error_depth(ctx);
}

const char* CryptoNative_X509VerifyCertErrorString(int32_t n)
{
    return X509_verify_cert_error_string((long)n);
}

void CryptoNative_X509CrlDestroy(X509_CRL* a)
{
    if (a != NULL)
    {
        X509_CRL_free(a);
    }
}

int32_t CryptoNative_PemWriteBioX509Crl(BIO* bio, X509_CRL* crl)
{
    return PEM_write_bio_X509_CRL(bio, crl);
}

X509_CRL* CryptoNative_PemReadBioX509Crl(BIO* bio)
{
    return PEM_read_bio_X509_CRL(bio, NULL, NULL, NULL);
}

int32_t CryptoNative_GetX509SubjectPublicKeyInfoDerSize(X509* x509)
{
    if (!x509)
    {
        return 0;
    }

    // X509_get_X509_PUBKEY returns an interior pointer, so should not be freed
    return i2d_X509_PUBKEY(X509_get_X509_PUBKEY(x509), NULL);
}

int32_t CryptoNative_EncodeX509SubjectPublicKeyInfo(X509* x509, uint8_t* buf)
{
    if (!x509)
    {
        return 0;
    }

    // X509_get_X509_PUBKEY returns an interior pointer, so should not be freed
    return i2d_X509_PUBKEY(X509_get_X509_PUBKEY(x509), &buf);
}

X509* CryptoNative_X509UpRef(X509* x509)
{
    if (x509 != NULL)
    {
        X509_up_ref(x509);
    }

    return x509;
}

static DIR* OpenUserStore(const char* storePath, char** pathTmp, size_t* pathTmpSize, char** nextFileWrite)
{
    DIR* trustDir = opendir(storePath);

    if (trustDir == NULL)
    {
        *pathTmp = NULL;
        *nextFileWrite = NULL;
        return NULL;
    }

    struct dirent* ent = NULL;
    size_t storePathLen = strlen(storePath);

    // d_name is a fixed length char[], not a char*.
    // Leave one byte for '\0' and one for '/'
    size_t allocSize = storePathLen + sizeof(ent->d_name) + 2;
    char* tmp = (char*)calloc(allocSize, sizeof(char));
    memcpy_s(tmp, allocSize, storePath, storePathLen);
    tmp[storePathLen] = '/';
    *pathTmp = tmp;
    *pathTmpSize = allocSize;
    *nextFileWrite = (tmp + storePathLen + 1);
    return trustDir;
}

static X509* ReadNextPublicCert(DIR* dir, X509Stack* tmpStack, char* pathTmp, size_t pathTmpSize, char* nextFileWrite)
{
    struct dirent* next;
    ptrdiff_t offset = nextFileWrite - pathTmp;
    assert(offset > 0);
    assert((size_t)offset < pathTmpSize);
    size_t remaining = pathTmpSize - (size_t)offset;

    while ((next = readdir(dir)) != NULL)
    {
        size_t len = strnlen(next->d_name, sizeof(next->d_name));

        if (len > 4 && 0 == strncasecmp(".pfx", next->d_name + len - 4, 4))
        {
            memcpy_s(nextFileWrite, remaining, next->d_name, len);
            // if d_name was full-length it might not have a trailing null.
            nextFileWrite[len] = 0;

            FILE* fp = fopen(pathTmp, "r");

            if (fp != NULL)
            {
                PKCS12* p12 = d2i_PKCS12_fp(fp, NULL);

                if (p12 != NULL)
                {
                    EVP_PKEY* key;
                    X509* cert = NULL;

                    if (PKCS12_parse(p12, NULL, &key, &cert, &tmpStack))
                    {
                        if (key != NULL)
                        {
                            EVP_PKEY_free(key);
                        }

                        if (cert == NULL && sk_X509_num(tmpStack) > 0)
                        {
                            cert = sk_X509_value(tmpStack, 0);
                            X509_up_ref(cert);
                        }
                    }

                    fclose(fp);

                    X509* popTmp;
                    while ((popTmp = sk_X509_pop(tmpStack)) != NULL)
                    {
                        X509_free(popTmp);
                    }

                    PKCS12_free(p12);

                    if (cert != NULL)
                    {
                        return cert;
                    }
                }
            }
        }
    }

    return NULL;
}

X509_STORE* CryptoNative_X509ChainNew(X509Stack* systemTrust, X509Stack* userTrust)
{
    X509_STORE* store = X509_STORE_new();

    if (store == NULL)
    {
        return NULL;
    }

    if (systemTrust != NULL)
    {
        int count = sk_X509_num(systemTrust);

        for (int i = 0; i < count; i++)
        {
            if (!X509_STORE_add_cert(store, sk_X509_value(systemTrust, i)))
            {
                X509_STORE_free(store);
                return NULL;
            }
        }
    }


    if (userTrust != NULL)
    {
        int count = sk_X509_num(userTrust);
        int clearError = 0;

        for (int i = 0; i < count; i++)
        {
            if (!X509_STORE_add_cert(store, sk_X509_value(userTrust, i)))
            {
                unsigned long error = ERR_peek_last_error();

                if (error != ERR_PACK(ERR_LIB_X509, X509_F_X509_STORE_ADD_CERT, X509_R_CERT_ALREADY_IN_HASH_TABLE))
                {
                    X509_STORE_free(store);
                    return NULL;
                }

                clearError = 1;
            }
        }

        if (clearError)
        {
            ERR_clear_error();
        }
    }

    return store;
}

int32_t CryptoNative_X509StackAddDirectoryStore(X509Stack* stack, char* storePath)
{
    if (stack == NULL || storePath == NULL)
    {
        return -1;
    }

    int clearError = 1;
    char* pathTmp;
    size_t pathTmpSize;
    char* nextFileWrite;
    DIR* storeDir = OpenUserStore(storePath, &pathTmp, &pathTmpSize, &nextFileWrite);

    if (storeDir != NULL)
    {
        X509* cert;
        X509Stack* tmpStack = sk_X509_new_null();

        while ((cert = ReadNextPublicCert(storeDir, tmpStack, pathTmp, pathTmpSize, nextFileWrite)) != NULL)
        {
            if (!sk_X509_push(stack, cert))
            {
                clearError = 0;
                X509_free(cert);
                break;
            }

            // Don't free the cert here, it'll get freed by sk_X509_pop_free later (push doesn't call up_ref)
        }

        sk_X509_free(tmpStack);
        free(pathTmp);
        closedir(storeDir);

        if (clearError)
        {
            // PKCS12_parse can cause spurious errors.
            // d2i_PKCS12_fp may have failed for invalid files.
            // Just clear it all.
            ERR_clear_error();
        }
    }

    return clearError;
}

int32_t CryptoNative_X509StackAddMultiple(X509Stack* dest, X509Stack* src)
{
    if (dest == NULL)
    {
        return -1;
    }

    int success = 1;

    if (src != NULL)
    {
        int count = sk_X509_num(src);

        for (int i = 0; i < count; i++)
        {
            X509* cert = sk_X509_value(src, i);
            X509_up_ref(cert);

            if (!sk_X509_push(dest, cert))
            {
                success = 0;
                break;
            }
        }
    }

    return success;
}

int32_t CryptoNative_X509StoreCtxCommitToChain(X509_STORE_CTX* storeCtx)
{
    if (storeCtx == NULL)
    {
        return -1;
    }

    X509Stack* chain = X509_STORE_CTX_get1_chain(storeCtx);

    if (chain == NULL)
    {
        return 0;
    }

    X509* cur = NULL;
    X509Stack* untrusted = X509_STORE_CTX_get0_untrusted(storeCtx);
    X509* leaf = X509_STORE_CTX_get0_cert(storeCtx);

    while ((cur = sk_X509_pop(untrusted)) != NULL)
    {
        X509_free(cur);
    }

    while ((cur = sk_X509_pop(chain)) != NULL)
    {
        if (cur == leaf)
        {
            // Undo the up-ref from get1_chain
            X509_free(cur);
        }
        else
        {
            // For intermediates which were already in untrusted this puts them back.
            //
            // For a fully trusted chain this will add the trust root redundantly to the
            // untrusted lookup set, but the resulting extra work is small compared to the
            // risk of being wrong about promoting trust or losing the chain at this point.
            if (!sk_X509_push(untrusted, cur))
            {
                X509err(X509_F_X509_VERIFY_CERT, ERR_R_MALLOC_FAILURE);
                X509_free(cur);
                sk_X509_pop_free(chain, X509_free);
                return 0;
            }
        }
    }

    // Since we've already drained out this collection there's no difference between free
    // and pop_free, other than free saves a bit of work.
    sk_X509_free(chain);
    return 1;
}

int32_t CryptoNative_X509StoreCtxResetForSignatureError(X509_STORE_CTX* storeCtx, X509_STORE** newStore)
{
    if (storeCtx == NULL || newStore == NULL)
    {
        return -1;
    }

    *newStore = NULL;

    int errorDepth = X509_STORE_CTX_get_error_depth(storeCtx);
    X509Stack* chain = X509_STORE_CTX_get0_chain(storeCtx);
    int chainLength = sk_X509_num(chain);
    X509_STORE* store = X509_STORE_CTX_get0_store(storeCtx);

    // If the signature error was reported at the last element
    if (chainLength - 1 == errorDepth)
    {
        X509* root;
        X509* last = sk_X509_value(chain, errorDepth);

        // If the last element is in the trust store we need to build a new trust store.
        if (X509_STORE_CTX_get1_issuer(&root, storeCtx, last))
        {
            if (root == last)
            {
                // We know it's a non-zero refcount after this because last has one, too.
                // So go ahead and undo the get1.
                X509_free(root);

                X509_STORE* tmpNew = X509_STORE_new();

                if (tmpNew == NULL)
                {
                    return 0;
                }

                X509* duplicate = X509_dup(last);

                if (duplicate == NULL)
                {
                    X509_STORE_free(tmpNew);
                    return 0;
                }

                if (!X509_STORE_add_cert(tmpNew, duplicate))
                {
                    X509_free(duplicate);
                    X509_STORE_free(tmpNew);
                    return 0;
                }

                *newStore = tmpNew;
                store = tmpNew;
                chainLength--;
            }
            else
            {
                // This really shouldn't happen, since if we could have resolved it now
                // it should have resolved during chain walk.
                //
                // But better safe than sorry.
                X509_free(root);
            }
        }
    }

    X509Stack* untrusted = X509_STORE_CTX_get0_untrusted(storeCtx);
    X509* cur;

    while ((cur = sk_X509_pop(untrusted)) != NULL)
    {
        X509_free(cur);
    }

    for (int i = chainLength - 1; i > 0; --i)
    {
        cur = sk_X509_value(chain, i);

        // errorDepth and lower need to be duplicated to avoid x->valid taint.
        if (i <= errorDepth)
        {
            X509* duplicate = X509_dup(cur);

            if (duplicate == NULL)
            {
                return 0;
            }

            if (!sk_X509_push(untrusted, duplicate))
            {
                X509err(X509_F_X509_VERIFY_CERT, ERR_R_MALLOC_FAILURE);
                X509_free(duplicate);
                return 0;
            }
        }
        else
        {
            if (sk_X509_push(untrusted, cur))
            {
                X509_up_ref(cur);
            }
            else
            {
                X509err(X509_F_X509_VERIFY_CERT, ERR_R_MALLOC_FAILURE);
                return 0;
            }
        }
    }

    X509* leafDup = X509_dup(X509_STORE_CTX_get0_cert(storeCtx));

    if (leafDup == NULL)
    {
        return 0;
    }

    X509_STORE_CTX_cleanup(storeCtx);
    return CryptoNative_X509StoreCtxInit(storeCtx, store, leafDup, untrusted);
}

static char* BuildOcspCacheFilename(char* cachePath, X509* subject)
{
    assert(cachePath != NULL);
    assert(subject != NULL);

    size_t len = strlen(cachePath);
    // path plus '/', '.', ".ocsp", '\0' and two 8 character hex strings
    size_t allocSize = len + 24;
    char* fullPath = (char*)calloc(allocSize, sizeof(char));

    if (fullPath != NULL)
    {
        unsigned long issuerHash = X509_issuer_name_hash(subject);
        unsigned long subjectHash = X509_subject_name_hash(subject);

        size_t written =
            (size_t)snprintf(fullPath, allocSize, "%s/%08lx.%08lx.ocsp", cachePath, issuerHash, subjectHash);
        assert(written == allocSize - 1);
        (void)written;

        if (issuerHash == 0 || subjectHash == 0)
        {
            ERR_clear_error();
        }
    }

    return fullPath;
}

static OCSP_CERTID* MakeCertId(X509* subject, X509* issuer)
{
    assert(subject != NULL);
    assert(issuer != NULL);

    // SHA-1 is being used because that's really the only thing supported by current OCSP responders
    return OCSP_cert_to_id(EVP_sha1(), subject, issuer);
}

static X509VerifyStatusCode CheckOcsp(OCSP_REQUEST* req,
                                      OCSP_RESPONSE* resp,
                                      X509* subject,
                                      X509* issuer,
                                      X509_STORE_CTX* storeCtx,
                                      ASN1_GENERALIZEDTIME** thisUpdate,
                                      ASN1_GENERALIZEDTIME** nextUpdate)
{
    if (thisUpdate != NULL)
    {
        *thisUpdate = NULL;
    }

    if (nextUpdate != NULL)
    {
        *nextUpdate = NULL;
    }

    assert(resp != NULL);
    assert(subject != NULL);
    assert(issuer != NULL);

    OCSP_CERTID* certId = MakeCertId(subject, issuer);

    if (certId == NULL)
    {
        return (X509VerifyStatusCode)-1;
    }

    OCSP_BASICRESP* basicResp = OCSP_response_get1_basic(resp);
    int status = V_OCSP_CERTSTATUS_UNKNOWN;
    X509VerifyStatusCode ret = PAL_X509_V_ERR_UNABLE_TO_GET_CRL;

    if (basicResp != NULL)
    {
        X509_STORE* store = X509_STORE_CTX_get0_store(storeCtx);
        X509_VERIFY_PARAM* param = X509_STORE_get0_param(store);
        unsigned long currentFlags = X509_VERIFY_PARAM_get_flags(param);
        // Reset the flags so the OCSP_basic_verify doesn't do a CRL lookup
        X509_VERIFY_PARAM_clear_flags(param, currentFlags);
        X509Stack* untrusted = X509_STORE_CTX_get0_untrusted(storeCtx);

        // From the documentation:
        // -1: Request has nonce, response does not.
        // 0: Request and response both have nonce, nonces do not match.
        // 1: Request and response both have nonce, nonces match.
        // 2: Neither request nor response have nonce.
        // 3: Response has a nonce, request does not.
        //
        int nonceCheck = req == NULL ? 1 : OCSP_check_nonce(req, basicResp);

        // Treat "response has no nonce" as success, since not all responders set the nonce.
        if (nonceCheck == -1)
        {
            nonceCheck = 1;
        }

        if (nonceCheck == 1 && OCSP_basic_verify(basicResp, untrusted, store, OCSP_TRUSTOTHER))
        {
            ASN1_GENERALIZEDTIME* thisupd = NULL;
            ASN1_GENERALIZEDTIME* nextupd = NULL;

            if (OCSP_resp_find_status(basicResp, certId, &status, NULL, NULL, &thisupd, &nextupd))
            {
                if (thisUpdate != NULL && thisupd != NULL)
                {
                    *thisUpdate = ASN1_STRING_dup(thisupd);
                }

                if (nextUpdate != NULL && nextupd != NULL)
                {
                    *nextUpdate = ASN1_STRING_dup(nextupd);
                }

                if (status == V_OCSP_CERTSTATUS_GOOD)
                {
                    ret = PAL_X509_V_OK;
                }
                else if (status == V_OCSP_CERTSTATUS_REVOKED)
                {
                    ret = PAL_X509_V_ERR_CERT_REVOKED;
                }
            }
        }

        // Restore the flags
        X509_STORE_set_flags(store, currentFlags);

        OCSP_BASICRESP_free(basicResp);
        basicResp = NULL;
    }

    OCSP_CERTID_free(certId);
    return ret;
}

static int Get0CertAndIssuer(X509_STORE_CTX* storeCtx, int chainDepth, X509** subject, X509** issuer)
{
    assert(storeCtx != NULL);
    assert(subject != NULL);
    assert(issuer != NULL);

    // get0 => don't free.
    X509Stack* chain = X509_STORE_CTX_get0_chain(storeCtx);
    int chainSize = chain == NULL ? 0 : sk_X509_num(chain);

    if (chainSize <= chainDepth)
    {
        return 0;
    }

    *subject = sk_X509_value(chain, chainDepth);
    *issuer = sk_X509_value(chain, chainSize == chainDepth + 1 ? chainDepth : chainDepth + 1);
    return 1;
}

static time_t GetIssuanceWindowStart()
{
    // time_t granularity is seconds, so subtract 4 days worth of seconds.
    // The 4 day policy is based on the CA/Browser Forum Baseline Requirements
    // (version 1.6.3) section 4.9.10 (On-Line Revocation Checking Requirements)
    time_t t = time(NULL);
    t -= 4 * 24 * 60 * 60;
    return t;
}

int32_t CryptoNative_X509ChainGetCachedOcspStatus(X509_STORE_CTX* storeCtx, char* cachePath, int chainDepth)
{
    if (storeCtx == NULL || cachePath == NULL)
    {
        return -1;
    }

    X509* subject;
    X509* issuer;

    if (!Get0CertAndIssuer(storeCtx, chainDepth, &subject, &issuer))
    {
        return -2;
    }

    X509VerifyStatusCode ret = PAL_X509_V_ERR_UNABLE_TO_GET_CRL;
    char* fullPath = BuildOcspCacheFilename(cachePath, subject);

    if (fullPath == NULL)
    {
        return (int32_t)ret;
    }

    BIO* bio = BIO_new_file(fullPath, "rb");
    OCSP_RESPONSE* resp = NULL;

    if (bio != NULL)
    {
        resp = d2i_OCSP_RESPONSE_bio(bio, NULL);
        BIO_free(bio);
    }

    if (resp != NULL)
    {
        ASN1_GENERALIZEDTIME* thisUpdate = NULL;
        ASN1_GENERALIZEDTIME* nextUpdate = NULL;
        ret = CheckOcsp(NULL, resp, subject, issuer, storeCtx, &thisUpdate, &nextUpdate);

        if (ret != PAL_X509_V_ERR_UNABLE_TO_GET_CRL)
        {
            time_t oldest = GetIssuanceWindowStart();

            // If either the thisUpdate or nextUpdate is missing we can't determine policy, so reject it.
            // oldest = now - window;
            //
            // if thisUpdate < oldest || nextUpdate < now, reject.
            //
            // Since X509_cmp(_current)_time returns 0 on error, do a <= 0 check.
            if (nextUpdate == NULL || thisUpdate == NULL || X509_cmp_current_time(nextUpdate) <= 0 ||
                X509_cmp_time(thisUpdate, &oldest) <= 0)
            {
                ret = PAL_X509_V_ERR_UNABLE_TO_GET_CRL;
            }
        }

        if (nextUpdate != NULL)
        {
            ASN1_GENERALIZEDTIME_free(nextUpdate);
        }

        if (thisUpdate != NULL)
        {
            ASN1_GENERALIZEDTIME_free(thisUpdate);
        }
    }

    // If the file failed to parse, or failed to match the certificate, or was outside of the policy window,
    // (or any other "this file has no further value" condition), delete the file and clear the errors that
    // may have been reported while determining we want to delete it and ask again fresh.
    if (ret == PAL_X509_V_ERR_UNABLE_TO_GET_CRL)
    {
        unlink(fullPath);
        ERR_clear_error();
    }

    free(fullPath);

    if (resp != NULL)
    {
        OCSP_RESPONSE_free(resp);
    }

    return (int32_t)ret;
}

OCSP_REQUEST* CryptoNative_X509ChainBuildOcspRequest(X509_STORE_CTX* storeCtx, int chainDepth)
{
    if (storeCtx == NULL)
    {
        return NULL;
    }

    X509* subject;
    X509* issuer;

    if (!Get0CertAndIssuer(storeCtx, chainDepth, &subject, &issuer))
    {
        return NULL;
    }

    OCSP_CERTID* certId = MakeCertId(subject, issuer);

    if (certId == NULL)
    {
        return NULL;
    }

    OCSP_REQUEST* req = OCSP_REQUEST_new();

    if (req == NULL)
    {
        OCSP_CERTID_free(certId);
        return NULL;
    }

    if (!OCSP_request_add0_id(req, certId))
    {
        OCSP_CERTID_free(certId);
        OCSP_REQUEST_free(req);
        return NULL;
    }

    // Ownership was successfully transferred to req
    certId = NULL;

    // Add a random nonce.
    OCSP_request_add1_nonce(req, NULL, -1);
    return req;
}

int32_t
CryptoNative_X509ChainVerifyOcsp(X509_STORE_CTX* storeCtx, OCSP_REQUEST* req, OCSP_RESPONSE* resp, char* cachePath, int chainDepth)
{
    if (storeCtx == NULL || req == NULL || resp == NULL)
    {
        return -1;
    }

    X509* subject;
    X509* issuer;

    if (!Get0CertAndIssuer(storeCtx, chainDepth, &subject, &issuer))
    {
        return -2;
    }

    X509VerifyStatusCode ret = PAL_X509_V_ERR_UNABLE_TO_GET_CRL;
    OCSP_CERTID* certId = MakeCertId(subject, issuer);

    if (certId == NULL)
    {
        return -3;
    }

    ASN1_GENERALIZEDTIME* thisUpdate = NULL;
    ASN1_GENERALIZEDTIME* nextUpdate = NULL;
    ret = CheckOcsp(req, resp, subject, issuer, storeCtx, &thisUpdate, &nextUpdate);

    if (ret == PAL_X509_V_OK || ret == PAL_X509_V_ERR_CERT_REVOKED)
    {
        // If the nextUpdate time is in the past (or corrupt), report either REVOKED or CRL_EXPIRED
        if (nextUpdate != NULL && X509_cmp_current_time(nextUpdate) <= 0)
        {
            if (ret == PAL_X509_V_OK)
            {
                ret = PAL_X509_V_ERR_CRL_HAS_EXPIRED;
            }
        }
        else
        {
            time_t oldest = GetIssuanceWindowStart();

            // If the response is within our caching policy (which requires a nextUpdate value)
            // then try to cache it.
            if (nextUpdate != NULL && thisUpdate != NULL && X509_cmp_time(thisUpdate, &oldest) > 0)
            {
                char* fullPath = BuildOcspCacheFilename(cachePath, subject);

                if (fullPath != NULL)
                {
                    int clearErr = 1;
                    BIO* bio = BIO_new_file(fullPath, "wb");

                    if (bio != NULL)
                    {
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wcast-qual"
                        if (i2d_OCSP_RESPONSE_bio(bio, resp))
#pragma clang diagnostic pop
                        {
                            clearErr = 0;
                        }

                        BIO_free(bio);
                    }

                    if (clearErr)
                    {
                        ERR_clear_error();
                        unlink(fullPath);
                    }

                    free(fullPath);
                }
            }
        }
    }

    if (nextUpdate != NULL)
    {
        ASN1_GENERALIZEDTIME_free(nextUpdate);
    }

    if (thisUpdate != NULL)
    {
        ASN1_GENERALIZEDTIME_free(thisUpdate);
    }

    return (int32_t)ret;
}
