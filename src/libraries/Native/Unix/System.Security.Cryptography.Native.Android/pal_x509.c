// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_x509.h"

#include "pal_eckey.h"
#include "pal_misc.h"
#include "pal_rsa.h"

#include <assert.h>
#include <ctype.h>
#include <stdbool.h>
#include <string.h>

ARGS_NON_NULL(1,2,4) static int32_t PopulateByteArray(JNIEnv* env, jbyteArray source, uint8_t* dest, int32_t* len);

ARGS_NON_NULL_ALL static void FindCertStart(const uint8_t** buffer, int32_t* len);

// Handles both DER and PEM formats
jobject /*X509Certificate*/ AndroidCryptoNative_X509Decode(const uint8_t* buf, int32_t len)
{
    abort_if_invalid_pointer_argument (buf);
    abort_if_negative_integer_argument (len);

    JNIEnv* env = GetJNIEnv();

    jobject ret = NULL;
    INIT_LOCALS(loc, bytes, stream, certType, certFactory);

    FindCertStart(&buf, &len);

    // byte[] bytes = new byte[] { ... }
    // InputStream stream = new ByteArrayInputStream(bytes);
    loc[bytes] = make_java_byte_array(env, len);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->SetByteArrayRegion(env, loc[bytes], 0, len, (const jbyte*)buf);
    loc[stream] = (*env)->NewObject(env, g_ByteArrayInputStreamClass, g_ByteArrayInputStreamCtor, loc[bytes]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // CertificateFactory certFactory = CertificateFactory.getInstance("X.509");
    // return (X509Certificate)certFactory.generateCertificate(stream);
    loc[certType] = make_java_string(env, "X.509");
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[certFactory] = (*env)->CallStaticObjectMethod(env, g_CertFactoryClass, g_CertFactoryGetInstance, loc[certType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    ret = (*env)->CallObjectMethod(env, loc[certFactory], g_CertFactoryGenerateCertificate, loc[stream]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    if (ret != NULL)
        ret = ToGRef(env, ret);

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

// Encodes as DER format
int32_t AndroidCryptoNative_X509Encode(jobject /*X509Certificate*/ cert, uint8_t* out, int32_t* outLen)
{
    abort_if_invalid_pointer_argument (cert);
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
    abort_if_invalid_pointer_argument (buf);
    abort_if_negative_integer_argument (bufLen);
    abort_if_invalid_pointer_argument (outLen);

    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;
    INIT_LOCALS(loc, bytes, stream, certType, certFactory, certs, iter);

    // byte[] bytes = new byte[] { ... }
    // InputStream stream = new ByteArrayInputStream(bytes);
    loc[bytes] = make_java_byte_array(env, bufLen);
    (*env)->SetByteArrayRegion(env, loc[bytes], 0, bufLen, (const jbyte*)buf);
    loc[stream] = (*env)->NewObject(env, g_ByteArrayInputStreamClass, g_ByteArrayInputStreamCtor, loc[bytes]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // CertificateFactory certFactory = CertificateFactory.getInstance("X.509");
    loc[certType] = make_java_string(env, "X.509");
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
    RELEASE_LOCALS(loc, env);
    return ret;
}

int32_t AndroidCryptoNative_X509ExportPkcs7(jobject* /*X509Certificate[]*/ certs,
                                            int32_t certsLen,
                                            uint8_t* out,
                                            int32_t* outLen)
{
    abort_if_invalid_pointer_argument (certs);
    abort_if_negative_integer_argument (certsLen);

    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;
    INIT_LOCALS(loc, certList, certType, certFactory, certPath, pkcs7Type, encoded);

    // ArrayList<Certificate> certList = new ArrayList<Certificate>();
    // foreach (Certificate cert in certs)
    //     certList.add(cert);
    loc[certList] = (*env)->NewObject(env, g_ArrayListClass, g_ArrayListCtorWithCapacity, certsLen);
    for (int i = 0; i < certsLen; ++i)
    {
        (*env)->CallBooleanMethod(env, loc[certList], g_ArrayListAdd, certs[i]);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }

    // CertificateFactory certFactory = CertificateFactory.getInstance("X.509");
    loc[certType] = make_java_string(env, "X.509");
    loc[certFactory] = (*env)->CallStaticObjectMethod(env, g_CertFactoryClass, g_CertFactoryGetInstance, loc[certType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // CertPath certPath = certFactory.generateCertPath(certList);
    // byte[] encoded = certPath.getEncoded("PKCS7");
    loc[certPath] =
        (*env)->CallObjectMethod(env, loc[certFactory], g_CertFactoryGenerateCertPathFromList, loc[certList]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[pkcs7Type] = make_java_string(env, "PKCS7");
    loc[encoded] = (*env)->CallObjectMethod(env, loc[certPath], g_CertPathGetEncoded, loc[pkcs7Type]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    ret = PopulateByteArray(env, loc[encoded], out, outLen);

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

PAL_X509ContentType AndroidCryptoNative_X509GetContentType(const uint8_t* buf, int32_t len)
{
    abort_if_invalid_pointer_argument (buf);
    abort_if_negative_integer_argument (len);

    JNIEnv* env = GetJNIEnv();

    PAL_X509ContentType ret = PAL_X509Unknown;
    INIT_LOCALS(loc, bytes, stream, certType, certFactory, pkcs7Type, certPath, cert);

    // This function checks:
    // - PKCS7 DER/PEM
    // - X509 DER/PEM
    // The generateCertificate method used for the X509 DER/PEM check will succeed for some
    // PKCS7 blobs, so it is done after the PKCS7 check.

    FindCertStart(&buf, &len);

    // byte[] bytes = new byte[] { ... }
    // InputStream stream = new ByteArrayInputStream(bytes);
    loc[bytes] = make_java_byte_array(env, len);
    (*env)->SetByteArrayRegion(env, loc[bytes], 0, len, (const jbyte*)buf);
    loc[stream] = (*env)->NewObject(env, g_ByteArrayInputStreamClass, g_ByteArrayInputStreamCtor, loc[bytes]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // CertificateFactory certFactory = CertificateFactory.getInstance("X.509");
    loc[certType] = make_java_string(env, "X.509");
    loc[certFactory] = (*env)->CallStaticObjectMethod(env, g_CertFactoryClass, g_CertFactoryGetInstance, loc[certType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // CertPath certPath = certFactory.generateCertPath(stream, "PKCS7");
    loc[pkcs7Type] = make_java_string(env, "PKCS7");
    loc[certPath] = (*env)->CallObjectMethod(
        env, loc[certFactory], g_CertFactoryGenerateCertPathFromStream, loc[stream], loc[pkcs7Type]);
    if (!TryClearJNIExceptions(env))
    {
        ret = PAL_Pkcs7;
        goto cleanup;
    }

    // stream.reset();
    // Certificate cert = certFactory.generateCertificate(stream);
    (*env)->CallVoidMethod(env, loc[stream], g_ByteArrayInputStreamReset);
    loc[cert] = (*env)->CallObjectMethod(env, loc[certFactory], g_CertFactoryGenerateCertificate, loc[stream]);
    if (!TryClearJNIExceptions(env))
    {
        ret = PAL_Certificate;
        goto cleanup;
    }

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

void* AndroidCryptoNative_X509PublicKey(jobject /*X509Certificate*/ cert, PAL_KeyAlgorithm algorithm)
{
    abort_if_invalid_pointer_argument (cert);

    JNIEnv* env = GetJNIEnv();

    void* keyHandle;
    jobject key = (*env)->CallObjectMethod(env, cert, g_X509CertGetPublicKey);
    if (CheckJNIExceptions(env) || !key)
    {
        return NULL;
    }
    switch (algorithm)
    {
        case PAL_EC:
            keyHandle = AndroidCryptoNative_NewEcKeyFromKeys(env, key, NULL /*privateKey*/);
            break;
        case PAL_DSA:
            keyHandle = AndroidCryptoNative_CreateKeyPair(env, key, NULL /*privateKey*/);
            break;
        case PAL_RSA:
            keyHandle = AndroidCryptoNative_NewRsaFromKeys(env, key, NULL /*privateKey*/);
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
    abort_if_invalid_pointer_argument (source);
    abort_if_invalid_pointer_argument (len);

    jsize bytesLen = (*env)->GetArrayLength(env, source);

    bool insufficientBuffer = *len < bytesLen;
    *len = bytesLen;
    if (insufficientBuffer)
        return INSUFFICIENT_BUFFER;

    if(dest == NULL)
        return SUCCESS; // managed code calls us with `dest` == NULL if it needs to learn the buffer size, it's not an
                        // error
    (*env)->GetByteArrayRegion(env, source, 0, bytesLen, (jbyte*)dest);
    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

static void FindCertStart(const uint8_t** buffer, int32_t* len)
{
    if (iscntrl(**buffer) && !isspace(**buffer))
    {
        // If the character is a control character that isn't whitespace, then we're probably using a DER encoding
        // and not using a PEM encoding in ASCII.
        return;
    }

    const uint8_t* bufferLocal = *buffer;
    int32_t lengthLocal = *len;

    while (lengthLocal > 0)
    {
        const char pemHeader[] = "-----BEGIN ";
        int32_t pemHeaderLength = (int32_t)(sizeof(pemHeader) - 1); // Exclude the null-terminator
        // Skip until we see the - that could start a PEM block.
        while (lengthLocal >= pemHeaderLength && (!iscntrl(*bufferLocal) || isspace(*bufferLocal)) &&
               *bufferLocal != pemHeader[0])
        {
            bufferLocal += 1;
            lengthLocal -= 1;
        }

        if (lengthLocal < pemHeaderLength || (iscntrl(*bufferLocal) && !isspace(*bufferLocal)))
        {
            // Either the buffer doesn't have enough space to contain a PEM header
            // or we encountered a control character that isn't whitespace.
            // In the insufficient size case, we didn't find the PEM header, so we can't skip to it.
            // In the control character case, we know that this isn't explanatory info since that needs to
            // all be printable or whitespace characters, not non-whitespace control characters.
            return;
        }

        if (memcmp(bufferLocal, pemHeader, (size_t)pemHeaderLength) == 0)
        {
            // We found the PEM header.
            *buffer = bufferLocal;
            *len = lengthLocal;
            return;
        }
        else
        {
            // This PEM header is invalid. Skip it.
            bufferLocal += 1;
            lengthLocal -= 1;
        }
    }
}
