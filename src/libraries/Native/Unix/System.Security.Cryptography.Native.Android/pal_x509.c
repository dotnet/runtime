// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_x509.h"

#include "pal_eckey.h"
#include "pal_rsa.h"

#include <assert.h>
#include <stdbool.h>
#include <string.h>

#define INIT_LOCALS(name, ...) \
    enum { __VA_ARGS__, count_##name }; \
    jobject name[count_##name] = { 0 }; \

#define RELEASE_LOCALS(name, env) \
{ \
    for (int i_##name = 0; i_##name < count_##name; ++i_##name) \
    { \
        jobject local = name[i_##name]; \
        if (local != NULL) \
            (*env)->DeleteLocalRef(env, local); \
    } \
} \

#define INSUFFICIENT_BUFFER -1

static int32_t PopulateByteArray(JNIEnv* env, jbyteArray source, uint8_t* dest, int32_t* len);

// Handles both DER and PEM formats
jobject /*X509Certificate*/ AndroidCryptoNative_X509Decode(const uint8_t* buf, int32_t len)
{
    assert(buf != NULL && len > 0);
    JNIEnv* env = GetJNIEnv();

    jobject ret = NULL;
    INIT_LOCALS(loc, bytes, stream, certType, certFactory)

    // byte[] bytes = new byte[] { ... }
    // InputStream stream = new ByteArrayInputStream(bytes);
    loc[bytes] = (*env)->NewByteArray(env, len);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->SetByteArrayRegion(env, loc[bytes], 0, len, (const jbyte*)buf);
    loc[stream] = (*env)->NewObject(env, g_ByteArrayInputStreamClass, g_ByteArrayInputStreamCtor, loc[bytes]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // CertificateFactory certFactory = CertificateFactory.getInstance("X.509");
    // return (X509Certificate)certFactory.generateCertificate(stream);
    loc[certType] = JSTRING("X.509");
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[certFactory] = (*env)->CallStaticObjectMethod(env, g_CertFactoryClass, g_CertFactoryGetInstance, loc[certType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    ret = (*env)->CallObjectMethod(env, loc[certFactory], g_CertFactoryGenerateCertificate, loc[stream]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    if (ret != NULL)
        ret = ToGRef(env, ret);

cleanup:
    RELEASE_LOCALS(loc, env)
    return ret;
}

// Encodes as DER format
int32_t AndroidCryptoNative_X509Encode(jobject /*X509Certificate*/ cert, uint8_t* out, int32_t* outLen)
{
    assert(cert != NULL);
    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;

    // byte[] encoded = cert.getEncoded();
    jbyteArray encoded = (*env)->CallObjectMethod(env, cert, g_X509CertGetEncoded);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    ret = PopulateByteArray(env, encoded, out, outLen);

cleanup:
    (*env)->DeleteLocalRef(env, encoded);
    return ret;
}

int32_t AndroidCryptoNative_X509DecodeCollection(const uint8_t* buf,
                                                 int32_t bufLen,
                                                 jobject /*X509Certificate*/* out,
                                                 int32_t* outLen)
{
    assert(buf != NULL && bufLen > 0);
    assert(outLen != NULL);
    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;
    INIT_LOCALS(loc, bytes, stream, certType, certFactory, certs, iter)

    // byte[] bytes = new byte[] { ... }
    // InputStream stream = new ByteArrayInputStream(bytes);
    loc[bytes] = (*env)->NewByteArray(env, bufLen);
    (*env)->SetByteArrayRegion(env, loc[bytes], 0, bufLen, (const jbyte*)buf);
    loc[stream] = (*env)->NewObject(env, g_ByteArrayInputStreamClass, g_ByteArrayInputStreamCtor, loc[bytes]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // CertificateFactory certFactory = CertificateFactory.getInstance("X.509");
    loc[certType] = JSTRING("X.509");
    loc[certFactory] = (*env)->CallStaticObjectMethod(env, g_CertFactoryClass, g_CertFactoryGetInstance, loc[certType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // Collection<Certificate> certs = certFactory.generateCertificates(stream);
    loc[certs] = (*env)->CallObjectMethod(env, loc[certFactory], g_CertFactoryGenerateCertificates, loc[stream]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    jint certCount = (*env)->CallIntMethod(env, loc[certs], g_CollectionSize);
    bool insufficientBuffer = *outLen < certCount;
    *outLen = certCount;

    if (certCount == 0)
    {
        ret = SUCCESS;
        goto cleanup;
    }

    if (insufficientBuffer)
    {
        ret = INSUFFICIENT_BUFFER;
        goto cleanup;
    }

    // int i = 0;
    // Iterator<Certificate> iter = certs.iterator();
    // while (iter.hasNext()) {
    //     Certificate cert = iter.next();
    //     < add global reference >
    //     out[i] = cert;
    //     i++;
    // }
    int32_t i = 0;
    loc[iter] = (*env)->CallObjectMethod(env, loc[certs], g_CollectionIterator);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    jboolean hasNext = (*env)->CallBooleanMethod(env, loc[iter], g_IteratorHasNext);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    while (hasNext)
    {
        jobject cert = (*env)->CallObjectMethod(env, loc[iter], g_IteratorNext);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        out[i] = ToGRef(env, cert);
        i++;

        hasNext = (*env)->CallBooleanMethod(env, loc[iter], g_IteratorHasNext);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }

    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env)
    return ret;
}

int32_t AndroidCryptoNative_X509ExportPkcs7(jobject* /*X509Certificate[]*/ certs,
                                            int32_t certsLen,
                                            uint8_t* out,
                                            int32_t* outLen)
{
    assert(certs != NULL && certsLen > 0);
    assert(outLen != NULL);
    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;
    INIT_LOCALS(loc, certList, certType, certFactory, certPath, pkcs7Type, encoded)

    // ArrayList<Certificate> certList = new ArrayList<Certificate>();
    // foreach (Certificate cert in certs)
    //     certList.add(cert);
    loc[certList] = (*env)->NewObject(env, g_ArrayListClass, g_ArrayListCtor, certsLen);
    for (int i = 0; i < certsLen; ++i)
    {
        (*env)->CallBooleanMethod(env, loc[certList], g_ArrayListAdd, certs[i]);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }

    // CertificateFactory certFactory = CertificateFactory.getInstance("X.509");
    loc[certType] = JSTRING("X.509");
    loc[certFactory] = (*env)->CallStaticObjectMethod(env, g_CertFactoryClass, g_CertFactoryGetInstance, loc[certType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // CertPath certPath = certFactory.generateCertPath(certList);
    // byte[] encoded = certPath.getEncoded("PKCS7");
    loc[certPath] = (*env)->CallObjectMethod(env, loc[certFactory], g_CertFactoryGenerateCertPathFromList, loc[certList]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[pkcs7Type] = JSTRING("PKCS7");
    loc[encoded] = (*env)->CallObjectMethod(env, loc[certPath], g_CertPathGetEncoded, loc[pkcs7Type]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    ret = PopulateByteArray(env, loc[encoded], out, outLen);

cleanup:
    RELEASE_LOCALS(loc, env)
    return ret;
}

PAL_X509ContentType AndroidCryptoNative_X509GetContentType(const uint8_t* buf, int32_t len)
{
    assert(buf != NULL && len > 0);
    JNIEnv* env = GetJNIEnv();

    PAL_X509ContentType ret = PAL_X509Unknown;
    INIT_LOCALS(loc, bytes, stream, certType, certFactory, pkcs7Type, certPath, cert)

    // This functin checks:
    // - PKCS7 DER/PEM
    // - X509 DER/PEM
    // The generateCertificate method used for the X509 DER/PEM check will succeed for some
    // PKCS7 blobs, so it is done after the PKCS7 check.

    // byte[] bytes = new byte[] { ... }
    // InputStream stream = new ByteArrayInputStream(bytes);
    loc[bytes] = (*env)->NewByteArray(env, len);
    (*env)->SetByteArrayRegion(env, loc[bytes], 0, len, (const jbyte*)buf);
    loc[stream] = (*env)->NewObject(env, g_ByteArrayInputStreamClass, g_ByteArrayInputStreamCtor, loc[bytes]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // CertificateFactory certFactory = CertificateFactory.getInstance("X.509");
    loc[certType] = JSTRING("X.509");
    loc[certFactory] = (*env)->CallStaticObjectMethod(env, g_CertFactoryClass, g_CertFactoryGetInstance, loc[certType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // CertPath certPath = certFactory.generateCertPath(stream, "PKCS7");
    loc[pkcs7Type] = JSTRING("PKCS7");
    loc[certPath] = (*env)->CallObjectMethod(env, loc[certFactory], g_CertFactoryGenerateCertPathFromStream, loc[stream], loc[pkcs7Type]);
    if (!CheckJNIExceptions(env))
    {
        ret = PAL_Pkcs7;
        goto cleanup;
    }

    // stream.reset();
    // Certificate cert = certFactory.generateCertificate(stream);
    (*env)->CallVoidMethod(env, loc[stream], g_ByteArrayInputStreamReset);
    loc[cert] = (*env)->CallObjectMethod(env, loc[certFactory], g_CertFactoryGenerateCertificate, loc[stream]);
    if (!CheckJNIExceptions(env))
    {
        ret = PAL_Certificate;
        goto cleanup;
    }

cleanup:
    RELEASE_LOCALS(loc, env)
    return ret;
}

void* AndroidCryptoNative_X509PublicKey(jobject /*X509Certificate*/ cert, PAL_KeyAlgorithm algorithm)
{
    assert(cert != NULL);

    JNIEnv* env = GetJNIEnv();

    void* keyHandle;
    jobject key = (*env)->CallObjectMethod(env, cert, g_X509CertGetPublicKey);
    switch (algorithm)
    {
        case PAL_EC:
            keyHandle = AndroidCryptoNative_NewEcKeyFromPublicKey(env, key);
            break;
        case PAL_DSA:
            keyHandle = NULL;
            break;
        case PAL_RSA:
            keyHandle = AndroidCryptoNative_NewRsaFromPublicKey(env, key);
            break;
        default:
            keyHandle = NULL;
            break;
    }

    (*env)->DeleteLocalRef(env, key);
    return keyHandle;
}

static int32_t PopulateByteArray(JNIEnv* env, jbyteArray source, uint8_t* dest, int32_t* len)
{
    jsize bytesLen = (*env)->GetArrayLength(env, source);

    bool insufficientBuffer = *len < bytesLen;
    *len = bytesLen;
    if (insufficientBuffer)
        return INSUFFICIENT_BUFFER;

    (*env)->GetByteArrayRegion(env, source, 0, bytesLen, (jbyte*)dest);
    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}
