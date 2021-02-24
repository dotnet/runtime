// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_x509.h"

#include <assert.h>
#include <stdbool.h>
#include <string.h>

#define INIT_LOCALS(name, ...) \
    enum { __VA_ARGS__, count_##name }; \
    jobject name[count_##name] = { 0 }; \

#define RELEASE_LOCALS(name, env) \
{ \
    for (int i = 0; i < count_##name; ++i) \
    { \
        jobject local = name[i]; \
        if (local != NULL) \
            (*env)->DeleteLocalRef(env, local); \
    } \
} \

#define BUFFER_FAIL FAIL

static int32_t PopulateByteArray(JNIEnv *env, jbyteArray source, uint8_t *dest, int32_t len);
static int32_t PopulateString(JNIEnv *env, jstring source, char *dest, int32_t len);

// Handles both DER and PEM formats
jobject /*X509Certificate*/ AndroidCryptoNative_DecodeX509(const uint8_t *buf, int32_t len)
{
    assert(buf != NULL && len > 0);
    JNIEnv *env = GetJNIEnv();

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
int32_t AndroidCryptoNative_EncodeX509(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len)
{
    assert(cert != NULL);
    JNIEnv *env = GetJNIEnv();
    int32_t ret = BUFFER_FAIL;

    // byte[] encoded = cert.getEncoded();
    // return encoded.length
    jbyteArray encoded = (*env)->CallObjectMethod(env, cert, g_X509CertGetEncoded);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    ret = PopulateByteArray(env, encoded, buf, len);

cleanup:
    (*env)->DeleteLocalRef(env, encoded);
    return ret;
}

void CryptoNative_X509Destroy(jobject /*X509Certificate*/ cert)
{
    ReleaseGRef(GetJNIEnv(), cert);
}

jobject /*X509Certificate*/ CryptoNative_X509UpRef(jobject /*X509Certificate*/ cert)
{
    return AddGRef(GetJNIEnv(), cert);
}

bool AndroidCryptoNative_X509GetBasicInformation(jobject /*X509Certificate*/ cert, struct X509BasicInformation *info)
{
    assert(cert != NULL);
    JNIEnv *env = GetJNIEnv();

    bool success = false;
    INIT_LOCALS(loc, notAfter, notBefore)

    // int version = cert.getVersion();
    jint ver = (*env)->CallIntMethod(env, cert, g_X509CertGetVersion);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    int32_t version = (int32_t)ver;

    // Date notAfter = cert.getNotAfter()
    // long notAfterTime = notAfter.getTime()
    loc[notAfter] = (*env)->CallObjectMethod(env, cert, g_X509CertGetNotAfter);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    int64_t notAfterTime = (int64_t)(*env)->CallLongMethod(env, loc[notAfter], g_DateGetTime);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // Date notBefore = cert.getNotBefore()
    // long notBeforeTime = notBefore.getTime()
    loc[notBefore] = (*env)->CallObjectMethod(env, cert, g_X509CertGetNotBefore);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    int64_t notBeforeTime = (int64_t)(*env)->CallLongMethod(env, loc[notBefore], g_DateGetTime);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    info->Version = version;
    info->NotAfter = notAfterTime;
    info->NotBefore = notBeforeTime;
    success = true;

cleanup:
    RELEASE_LOCALS(loc, env)
    return success;
}

int32_t AndroidCryptoNative_X509GetPublicKeyAlgorithm(jobject /*X509Certificate*/ cert, char *buf, int32_t len)
{
    assert(cert != NULL);
    JNIEnv *env = GetJNIEnv();

    int32_t ret = BUFFER_FAIL;
    INIT_LOCALS(loc, key, algorithm)

    // PublicKey key = cert.getPublicKey();
    // String algorithm = key.getAlgorithm();
    // return encoded.length;
    loc[key] = (*env)->CallObjectMethod(env, cert, g_X509CertGetPublicKey);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[algorithm] = (*env)->CallObjectMethod(env, loc[key], g_KeyGetAlgorithm);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    ret = PopulateString(env, loc[algorithm], buf, len);

cleanup:
    RELEASE_LOCALS(loc, env)
    return ret;
}

int32_t AndroidCryptoNative_X509GetPublicKeyBytes(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len)
{
    assert(cert != NULL);
    JNIEnv *env = GetJNIEnv();

    int32_t ret = BUFFER_FAIL;
    INIT_LOCALS(loc, key, keyInfoBytes)

    // PublicKey key = cert.getPublicKey();
    // byte[] keyInfoBytes = key.getEncoded();
    loc[key] = (*env)->CallObjectMethod(env, cert, g_X509CertGetPublicKey);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[keyInfoBytes] = (*env)->CallObjectMethod(env, loc[key], g_KeyGetEncoded);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // return keyInfoBytes.length;
    ret = PopulateByteArray(env, loc[keyInfoBytes], buf, len);

cleanup:
    RELEASE_LOCALS(loc, env)
    return ret;
}

int32_t AndroidCryptoNative_X509GetPublicKeyParameterBytes(jobject /*X509Certificate*/ cert, uint8_t *pBuf, int32_t cBuf)
{
    // [TODO]
    // PublicKey key = cert.publicKey()
    // String algorithm = key.getAlgorithm()
    // if (algorithm == "...") ...
    // getParams()
    return BUFFER_FAIL;
}

// Serial number as a byte array in big-endian byte-order
int32_t AndroidCryptoNative_X509GetSerialNumber(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len)
{
    assert(cert != NULL);
    JNIEnv *env = GetJNIEnv();

    int32_t ret = BUFFER_FAIL;
    INIT_LOCALS(loc, serial, bytes)

    // BigInteger serial = cert.getSerialNumber();
    // buf = serial.toByteArray();
    // return buf.length;
    loc[serial] = (*env)->CallObjectMethod(env, cert, g_X509CertGetSerialNumber);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[bytes] = (*env)->CallObjectMethod(env, loc[serial], g_toByteArrayMethod);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    ret = PopulateByteArray(env, loc[bytes], buf, len);

cleanup:
    RELEASE_LOCALS(loc, env)
    return ret;
}

int32_t AndroidCryptoNative_X509GetSignatureAlgorithm(jobject /*X509Certificate*/ cert, char *buf, int32_t len)
{
    assert(cert != NULL);
    JNIEnv *env = GetJNIEnv();
    int32_t ret = BUFFER_FAIL;

    // String oid = cert.getSigAlgOID()
    // return oid.length;
    jstring oid = (jstring)(*env)->CallObjectMethod(env, cert, g_X509CertGetSigAlgOID);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    ret = PopulateString(env, oid, buf, len);

cleanup:
    (*env)->DeleteLocalRef(env, oid);
    return ret;
}

int32_t AndroidCryptoNative_X509GetThumbprint(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len)
{
    assert(cert != NULL);
    JNIEnv *env = GetJNIEnv();

    int32_t ret = BUFFER_FAIL;
    INIT_LOCALS(loc, algorithm, md, encoded, thumbprint)

    // MessageDigest md = MessageDigest.getInstance("SHA-1");
    loc[algorithm] = JSTRING("SHA-1");
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[md] = (*env)->CallStaticObjectMethod(env, g_mdClass, g_mdGetInstanceMethod, loc[algorithm]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // byte[] encoded = cert.getEncoded();
    loc[encoded] = (*env)->CallObjectMethod(env, cert, g_X509CertGetEncoded);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // byte[] thumbprint = md.digest(encoded);
    // return thumbprint.length;
    loc[thumbprint] = (*env)->CallObjectMethod(env, loc[md], g_mdDigestMethod, loc[encoded]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    ret = PopulateByteArray(env, loc[thumbprint], buf, len);

cleanup:
    RELEASE_LOCALS(loc, env)
    return ret;
}

static int32_t GetNameBytes(JNIEnv *env, jobject /*X500Principal*/ name, uint8_t *buf, int32_t len)
{
    assert(name != NULL);
    int32_t ret = BUFFER_FAIL;

    // byte[] raw = name.getEncoded();
    // return raw.length
    jbyteArray encoded = (*env)->CallObjectMethod(env, name, g_X500PrincipalGetEncoded);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    ret = PopulateByteArray(env, encoded, buf, len);

cleanup:
    (*env)->DeleteLocalRef(env, encoded);
    return ret;
}

int32_t AndroidCryptoNative_X509GetIssuerNameBytes(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len)
{
    assert(cert != NULL);
    JNIEnv *env = GetJNIEnv();
    int32_t ret = BUFFER_FAIL;

    // X500Principal name = cert.getIssuerX500Principal()
    jobject name = (*env)->CallObjectMethod(env, cert, g_X509CertGetIssuerX500Principal);
    if (!CheckJNIExceptions(env) && name != NULL)
        ret = GetNameBytes(env, name, buf, len);

    (*env)->DeleteLocalRef(env, name);
    return ret;
}

int32_t AndroidCryptoNative_X509GetSubjectNameBytes(jobject /*X509Certificate*/ cert, uint8_t *buf, int32_t len)
{
    assert(cert != NULL);
    JNIEnv *env = GetJNIEnv();
    int32_t ret = BUFFER_FAIL;

    // X500Principal name = cert.getSubjectX500Principal()
    jobject name = (*env)->CallObjectMethod(env, cert, g_X509CertGetSubjectX500Principal);
    if (!CheckJNIExceptions(env) && name != NULL)
        ret = GetNameBytes(env, name, buf, len);

    (*env)->DeleteLocalRef(env, name);
    return ret;
}

static void EnumExtensions(JNIEnv *env, jobject /*X509Certificate*/ cert, bool critical, EnumX509ExtensionsCallback cb, void *context)
{
    INIT_LOCALS(loc, exts, iter)

    // Set<string> crit = critical ? cert.getCriticalExtensionOIDs() : cert.getNonCriticalExtensionOIDs();
    loc[exts] = critical
        ? (*env)->CallObjectMethod(env, cert, g_X509CertGetCriticalExtensionOIDs)
        : (*env)->CallObjectMethod(env, cert, g_X509CertGetNonCriticalExtensionOIDs);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // Iterator<string> iter = collection.iterator();
    // while (iter.hasNext()) {
    //     string oid = iter.next();
    //     byte[] data = cert.getExtensionValue(oid);
    //     cb(oid, oid.length, data, data.length, /*critical*/ true, context);
    // }
    loc[iter] = (*env)->CallObjectMethod(env, loc[exts], g_SetIterator);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    jboolean hasNext = (*env)->CallBooleanMethod(env, loc[iter], g_IteratorHasNext);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    while (hasNext)
    {
        INIT_LOCALS(locLoop, oid, data)
        const char *oidPtr = NULL;
        jbyte *dataPtr = NULL;

        locLoop[oid] = (*env)->CallObjectMethod(env, loc[iter], g_IteratorNext);
        ON_EXCEPTION_PRINT_AND_GOTO(loop_cleanup);
        jsize oidLen = (*env)->GetStringUTFLength(env, locLoop[oid]);
        oidPtr = (*env)->GetStringUTFChars(env, locLoop[oid], NULL);

        locLoop[data] = (*env)->CallObjectMethod(env, cert, g_X509CertGetExtensionValue, locLoop[oid]);
        ON_EXCEPTION_PRINT_AND_GOTO(loop_cleanup);
        jsize dataLen = (*env)->GetArrayLength(env, locLoop[data]);
        dataPtr = (*env)->GetByteArrayElements(env, locLoop[data], NULL);

        // X509Certificate.getExtensionValue returns the full DER-encoded data.
        cb(oidPtr, oidLen, (const uint8_t *)dataPtr, dataLen, critical, context);

        hasNext = (*env)->CallBooleanMethod(env, loc[iter], g_IteratorHasNext);
        if (CheckJNIExceptions(env))
            hasNext = false;

    loop_cleanup:
        if (oidPtr != NULL)
            (*env)->ReleaseStringUTFChars(env, locLoop[oid], oidPtr);

        if (dataPtr != NULL)
            (*env)->ReleaseByteArrayElements(env, locLoop[data], dataPtr, JNI_ABORT);

        RELEASE_LOCALS(locLoop, env)
    }

cleanup:
    RELEASE_LOCALS(loc, env)
}

int32_t AndroidCryptoNative_X509EnumExtensions(jobject /*X509Certificate*/ cert, EnumX509ExtensionsCallback cb, void *context)
{
    assert(cert != NULL);
    JNIEnv *env = GetJNIEnv();

    // [TODO] We're skipping over any errors - do we care to propagate?
    EnumExtensions(env, cert, true /*critical*/, cb, context);
    EnumExtensions(env, cert, false /*critical*/, cb, context);
    return SUCCESS;
}

int32_t AndroidCryptoNative_X509FindExtensionData(jobject /*X509Certificate*/ cert, const char *oid, uint8_t *buf, int32_t len)
{
    LOG_ERROR("Not yet implemented");

    // byte[] data = cert.getExtensionValue(oid);
    // return data.length;
    return BUFFER_FAIL;
}

static int32_t PopulateByteArray(JNIEnv *env, jbyteArray source, uint8_t *dest, int32_t len)
{
    jsize bytesLen = (*env)->GetArrayLength(env, source);

    // Insufficient buffer
    if (len < bytesLen)
        return -bytesLen;

    (*env)->GetByteArrayRegion(env, source, 0, bytesLen, (jbyte*)dest);
    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

static int32_t PopulateString(JNIEnv *env, jstring source, char *dest, int32_t len)
{
    // Length with null terminator
    jsize bytesLen = (*env)->GetStringUTFLength(env, source) + 1;

    // Insufficient buffer
    if (len < bytesLen)
        return -bytesLen;

    jsize strLen = (*env)->GetStringLength(env, source);
    (*env)->GetStringUTFRegion(env, source, 0, strLen, dest);
    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}
