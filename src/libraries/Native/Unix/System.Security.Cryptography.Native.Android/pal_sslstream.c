// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_sslstream.h"
#include "pal_ssl.h"

// javax/net/ssl/SSLEngineResult$HandshakeStatus
enum
{
    HANDSHAKE_STATUS__NOT_HANDSHAKING = 0,
    HANDSHAKE_STATUS__FINISHED = 1,
    HANDSHAKE_STATUS__NEED_TASK = 2,
    HANDSHAKE_STATUS__NEED_WRAP = 3,
    HANDSHAKE_STATUS__NEED_UNWRAP = 4,
};

// javax/net/ssl/SSLEngineResult$Status
enum
{
    STATUS__BUFFER_UNDERFLOW = 0,
    STATUS__BUFFER_OVERFLOW = 1,
    STATUS__OK = 2,
    STATUS__CLOSED = 3,
};

struct ApplicationProtocolData_t
{
    uint8_t* data;
    int32_t length;
};

ARGS_NON_NULL(1) static uint16_t* AllocateString(JNIEnv* env, jstring source);

ARGS_NON_NULL_ALL static PAL_SSLStreamStatus DoHandshake(JNIEnv* env, SSLStream* sslStream);
ARGS_NON_NULL_ALL static PAL_SSLStreamStatus DoWrap(JNIEnv* env, SSLStream* sslStream, int* handshakeStatus);
ARGS_NON_NULL_ALL static PAL_SSLStreamStatus DoUnwrap(JNIEnv* env, SSLStream* sslStream, int* handshakeStatus);

static bool IsHandshaking(int handshakeStatus)
{
    return handshakeStatus != HANDSHAKE_STATUS__NOT_HANDSHAKING && handshakeStatus != HANDSHAKE_STATUS__FINISHED;
}

ARGS_NON_NULL_ALL static PAL_SSLStreamStatus Close(JNIEnv* env, SSLStream* sslStream)
{
    // Call wrap to clear any remaining data before closing
    int unused;
    PAL_SSLStreamStatus ret = DoWrap(env, sslStream, &unused);

    // sslEngine.closeOutbound();
    (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineCloseOutbound);
    if (ret != SSLStreamStatus_OK)
        return ret;

    // Flush any remaining data (e.g. sending close notification)
    return DoWrap(env, sslStream, &unused);
}

ARGS_NON_NULL_ALL static PAL_SSLStreamStatus Flush(JNIEnv* env, SSLStream* sslStream)
{
    /*
        netOutBuffer.flip();
        byte[] data = new byte[netOutBuffer.limit()];
        netOutBuffer.get(data);
        streamWriter(data, 0, data.length);
        netOutBuffer.compact();
    */

    PAL_SSLStreamStatus ret = SSLStreamStatus_Error;

    IGNORE_RETURN((*env)->CallObjectMethod(env, sslStream->netOutBuffer, g_ByteBufferFlip));
    int32_t bufferLimit = (*env)->CallIntMethod(env, sslStream->netOutBuffer, g_ByteBufferLimit);
    jbyteArray data = make_java_byte_array(env, bufferLimit);

    IGNORE_RETURN((*env)->CallObjectMethod(env, sslStream->netOutBuffer, g_ByteBufferGet, data));
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    uint8_t* dataPtr = (uint8_t*)xmalloc((size_t)bufferLimit);
    (*env)->GetByteArrayRegion(env, data, 0, bufferLimit, (jbyte*)dataPtr);
    sslStream->streamWriter(dataPtr, bufferLimit);
    free(dataPtr);

    IGNORE_RETURN((*env)->CallObjectMethod(env, sslStream->netOutBuffer, g_ByteBufferCompact));
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    ret = SSLStreamStatus_OK;

cleanup:
    (*env)->DeleteLocalRef(env, data);
    return ret;
}

ARGS_NON_NULL_ALL static jobject ExpandBuffer(JNIEnv* env, jobject oldBuffer, int32_t newCapacity)
{
    // oldBuffer.flip();
    // ByteBuffer newBuffer = ByteBuffer.allocate(newCapacity);
    // newBuffer.put(oldBuffer);
    IGNORE_RETURN((*env)->CallObjectMethod(env, oldBuffer, g_ByteBufferFlip));
    jobject newBuffer =
        ToGRef(env, (*env)->CallStaticObjectMethod(env, g_ByteBuffer, g_ByteBufferAllocate, newCapacity));
    IGNORE_RETURN((*env)->CallObjectMethod(env, newBuffer, g_ByteBufferPutBuffer, oldBuffer));
    ReleaseGRef(env, oldBuffer);
    return newBuffer;
}

ARGS_NON_NULL_ALL static jobject EnsureRemaining(JNIEnv* env, jobject oldBuffer, int32_t newRemaining)
{
    int32_t oldRemaining = (*env)->CallIntMethod(env, oldBuffer, g_ByteBufferRemaining);
    if (oldRemaining < newRemaining)
    {
        return ExpandBuffer(env, oldBuffer, oldRemaining + newRemaining);
    }
    else
    {
        return oldBuffer;
    }
}

ARGS_NON_NULL_ALL static PAL_SSLStreamStatus DoWrap(JNIEnv* env, SSLStream* sslStream, int* handshakeStatus)
{
    // appOutBuffer.flip();
    // SSLEngineResult result = sslEngine.wrap(appOutBuffer, netOutBuffer);
    IGNORE_RETURN((*env)->CallObjectMethod(env, sslStream->appOutBuffer, g_ByteBufferFlip));
    jobject result = (*env)->CallObjectMethod(
        env, sslStream->sslEngine, g_SSLEngineWrap, sslStream->appOutBuffer, sslStream->netOutBuffer);
    if (CheckJNIExceptions(env))
        return SSLStreamStatus_Error;

    // appOutBuffer.compact();
    IGNORE_RETURN((*env)->CallObjectMethod(env, sslStream->appOutBuffer, g_ByteBufferCompact));

    // handshakeStatus = result.getHandshakeStatus();
    // SSLEngineResult.Status status = result.getStatus();
    *handshakeStatus = GetEnumAsInt(env, (*env)->CallObjectMethod(env, result, g_SSLEngineResultGetHandshakeStatus));
    int status = GetEnumAsInt(env, (*env)->CallObjectMethod(env, result, g_SSLEngineResultGetStatus));
    (*env)->DeleteLocalRef(env, result);

    switch (status)
    {
        case STATUS__OK:
        {
            return Flush(env, sslStream);
        }
        case STATUS__CLOSED:
        {
            (void)Flush(env, sslStream);
            (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineCloseOutbound);
            return SSLStreamStatus_Closed;
        }
        case STATUS__BUFFER_OVERFLOW:
        {
            // Expand buffer
            // int newCapacity = sslSession.getPacketBufferSize() + netOutBuffer.remaining();
            int32_t newCapacity = (*env)->CallIntMethod(env, sslStream->sslSession, g_SSLSessionGetPacketBufferSize) +
                                  (*env)->CallIntMethod(env, sslStream->netOutBuffer, g_ByteBufferRemaining);
            sslStream->netOutBuffer = ExpandBuffer(env, sslStream->netOutBuffer, newCapacity);
            return SSLStreamStatus_OK;
        }
        default:
        {
            LOG_ERROR("Unknown SSLEngineResult status: %d", status);
            return SSLStreamStatus_Error;
        }
    }
}

ARGS_NON_NULL_ALL static PAL_SSLStreamStatus DoUnwrap(JNIEnv* env, SSLStream* sslStream, int* handshakeStatus)
{
    // if (netInBuffer.position() == 0)
    // {
    //     byte[] tmp = new byte[netInBuffer.limit()];
    //     int count = streamReader(tmp, 0, tmp.length);
    //     netInBuffer.put(tmp, 0, count);
    // }
    if ((*env)->CallIntMethod(env, sslStream->netInBuffer, g_ByteBufferPosition) == 0)
    {
        int netInBufferLimit = (*env)->CallIntMethod(env, sslStream->netInBuffer, g_ByteBufferLimit);
        jbyteArray tmp = make_java_byte_array(env, netInBufferLimit);
        uint8_t* tmpNative = (uint8_t*)xmalloc((size_t)netInBufferLimit);
        int count = netInBufferLimit;
        PAL_SSLStreamStatus status = sslStream->streamReader(tmpNative, &count);
        if (status != SSLStreamStatus_OK)
        {
            (*env)->DeleteLocalRef(env, tmp);
            return status;
        }

        (*env)->SetByteArrayRegion(env, tmp, 0, count, (jbyte*)(tmpNative));
        IGNORE_RETURN(
            (*env)->CallObjectMethod(env, sslStream->netInBuffer, g_ByteBufferPutByteArrayWithLength, tmp, 0, count));
        free(tmpNative);
        (*env)->DeleteLocalRef(env, tmp);
    }

    // netInBuffer.flip();
    // SSLEngineResult result = sslEngine.unwrap(netInBuffer, appInBuffer);
    IGNORE_RETURN((*env)->CallObjectMethod(env, sslStream->netInBuffer, g_ByteBufferFlip));
    jobject result = (*env)->CallObjectMethod(
        env, sslStream->sslEngine, g_SSLEngineUnwrap, sslStream->netInBuffer, sslStream->appInBuffer);
    if (CheckJNIExceptions(env))
        return SSLStreamStatus_Error;

    // netInBuffer.compact();
    IGNORE_RETURN((*env)->CallObjectMethod(env, sslStream->netInBuffer, g_ByteBufferCompact));

    // handshakeStatus = result.getHandshakeStatus();
    // SSLEngineResult.Status status = result.getStatus();
    *handshakeStatus = GetEnumAsInt(env, (*env)->CallObjectMethod(env, result, g_SSLEngineResultGetHandshakeStatus));
    int status = GetEnumAsInt(env, (*env)->CallObjectMethod(env, result, g_SSLEngineResultGetStatus));
    (*env)->DeleteLocalRef(env, result);
    switch (status)
    {
        case STATUS__OK:
        {
            return SSLStreamStatus_OK;
        }
        case STATUS__CLOSED:
        {
            return Close(env, sslStream);
        }
        case STATUS__BUFFER_UNDERFLOW:
        {
            // Expand buffer
            // int newRemaining = sslSession.getPacketBufferSize();
            int32_t newRemaining = (*env)->CallIntMethod(env, sslStream->sslSession, g_SSLSessionGetPacketBufferSize);
            sslStream->netInBuffer = EnsureRemaining(env, sslStream->netInBuffer, newRemaining);
            return SSLStreamStatus_OK;
        }
        case STATUS__BUFFER_OVERFLOW:
        {
            // Expand buffer
            // int newCapacity = sslSession.getApplicationBufferSize() + appInBuffer.remaining();
            int32_t newCapacity =
                (*env)->CallIntMethod(env, sslStream->sslSession, g_SSLSessionGetApplicationBufferSize) +
                (*env)->CallIntMethod(env, sslStream->appInBuffer, g_ByteBufferRemaining);
            sslStream->appInBuffer = ExpandBuffer(env, sslStream->appInBuffer, newCapacity);
            return SSLStreamStatus_OK;
        }
        default:
        {
            LOG_ERROR("Unknown SSLEngineResult status: %d", status);
            return SSLStreamStatus_Error;
        }
    }
}

ARGS_NON_NULL_ALL static PAL_SSLStreamStatus DoHandshake(JNIEnv* env, SSLStream* sslStream)
{
    PAL_SSLStreamStatus status = SSLStreamStatus_OK;
    int handshakeStatus =
        GetEnumAsInt(env, (*env)->CallObjectMethod(env, sslStream->sslEngine, g_SSLEngineGetHandshakeStatus));
    while (IsHandshaking(handshakeStatus) && status == SSLStreamStatus_OK)
    {
        switch (handshakeStatus)
        {
            case HANDSHAKE_STATUS__NEED_WRAP:
                status = DoWrap(env, sslStream, &handshakeStatus);
                break;
            case HANDSHAKE_STATUS__NEED_UNWRAP:
                status = DoUnwrap(env, sslStream, &handshakeStatus);
                break;
            case HANDSHAKE_STATUS__NOT_HANDSHAKING:
            case HANDSHAKE_STATUS__FINISHED:
                status = SSLStreamStatus_OK;
                break;
            case HANDSHAKE_STATUS__NEED_TASK:
                assert(0 && "unexpected NEED_TASK handshake status");
        }
    }

    return status;
}

ARGS_NON_NULL_ALL static void FreeSSLStream(JNIEnv* env, SSLStream* sslStream)
{
    ReleaseGRef(env, sslStream->sslContext);
    ReleaseGRef(env, sslStream->sslEngine);
    ReleaseGRef(env, sslStream->sslSession);
    ReleaseGRef(env, sslStream->appOutBuffer);
    ReleaseGRef(env, sslStream->netOutBuffer);
    ReleaseGRef(env, sslStream->netInBuffer);
    ReleaseGRef(env, sslStream->appInBuffer);
    free(sslStream);
}

SSLStream* AndroidCryptoNative_SSLStreamCreate(void)
{
    JNIEnv* env = GetJNIEnv();

    // SSLContext sslContext = SSLContext.getDefault();
    jobject sslContext = (*env)->CallStaticObjectMethod(env, g_SSLContext, g_SSLContextGetDefault);
    if (CheckJNIExceptions(env))
        return NULL;

    SSLStream* sslStream = xcalloc(1, sizeof(SSLStream));
    sslStream->sslContext = ToGRef(env, sslContext);
    return sslStream;
}

ARGS_NON_NULL_ALL
static int32_t AddCertChainToStore(JNIEnv* env,
                                   jobject store,
                                   uint8_t* pkcs8PrivateKey,
                                   int32_t pkcs8PrivateKeyLen,
                                   PAL_KeyAlgorithm algorithm,
                                   jobject* /*X509Certificate[]*/ certs,
                                   int32_t certsLen)
{
    abort_if_invalid_pointer_argument (pkcs8PrivateKey);
    abort_if_invalid_pointer_argument (certs);

    int32_t ret = FAIL;
    INIT_LOCALS(loc, keyBytes, keySpec, algorithmName, keyFactory, privateKey, certArray, alias);

    // byte[] keyBytes = new byte[] { <pkcs8PrivateKey> };
    // PKCS8EncodedKeySpec keySpec = new PKCS8EncodedKeySpec(keyBytes);
    loc[keyBytes] = make_java_byte_array(env, pkcs8PrivateKeyLen);
    (*env)->SetByteArrayRegion(env, loc[keyBytes], 0, pkcs8PrivateKeyLen, (jbyte*)pkcs8PrivateKey);
    loc[keySpec] = (*env)->NewObject(env, g_PKCS8EncodedKeySpec, g_PKCS8EncodedKeySpecCtor, loc[keyBytes]);

    switch (algorithm)
    {
        case PAL_DSA:
            loc[algorithmName] = make_java_string(env, "DSA");
            break;
        case PAL_EC:
            loc[algorithmName] = make_java_string(env, "EC");
            break;
        case PAL_RSA:
            loc[algorithmName] = make_java_string(env, "RSA");
            break;
        default:
            LOG_ERROR("Unknown key algorithm: %d", algorithm);
            goto cleanup;
    }

    // KeyFactory keyFactory = KeyFactory.getInstance(algorithmName);
    // PrivateKey privateKey = keyFactory.generatePrivate(spec);
    loc[keyFactory] =
        (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, loc[algorithmName]);
    loc[privateKey] = (*env)->CallObjectMethod(env, loc[keyFactory], g_KeyFactoryGenPrivateMethod, loc[keySpec]);

    // X509Certificate[] certArray = new X509Certificate[certsLen];
    loc[certArray] = make_java_object_array(env, certsLen, g_X509CertClass, NULL);
    for (int32_t i = 0; i < certsLen; ++i)
    {
        (*env)->SetObjectArrayElement(env, loc[certArray], i, certs[i]);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }

    // store.setKeyEntry("SSLCertificateContext", privateKey, null, certArray);
    loc[alias] = make_java_string(env, "SSLCertificateContext");
    (*env)->CallVoidMethod(env, store, g_KeyStoreSetKeyEntry, loc[alias], loc[privateKey], NULL, loc[certArray]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

SSLStream* AndroidCryptoNative_SSLStreamCreateWithCertificates(uint8_t* pkcs8PrivateKey,
                                                               int32_t pkcs8PrivateKeyLen,
                                                               PAL_KeyAlgorithm algorithm,
                                                               jobject* /*X509Certificate[]*/ certs,
                                                               int32_t certsLen)
{
    SSLStream* sslStream = NULL;
    JNIEnv* env = GetJNIEnv();

    INIT_LOCALS(loc, tls13, sslContext, ksType, keyStore, kmfType, kmf, keyManagers);

    // SSLContext sslContext = SSLContext.getInstance("TLSv1.3");
    loc[tls13] = make_java_string(env, "TLSv1.3");
    loc[sslContext] = (*env)->CallStaticObjectMethod(env, g_SSLContext, g_SSLContextGetInstanceMethod, loc[tls13]);
    if (TryClearJNIExceptions(env))
    {
        // TLSv1.3 is only supported on API level 29+ - fall back to TLSv1.2 (which is supported on API level 16+)
        // sslContext = SSLContext.getInstance("TLSv1.2");
        jobject tls12 = make_java_string(env, "TLSv1.2");
        loc[sslContext] = (*env)->CallStaticObjectMethod(env, g_SSLContext, g_SSLContextGetInstanceMethod, tls12);
        ReleaseLRef(env, tls12);
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    }

    // String ksType = KeyStore.getDefaultType();
    // KeyStore keyStore = KeyStore.getInstance(ksType);
    // keyStore.load(null, null);
    loc[ksType] = (*env)->CallStaticObjectMethod(env, g_KeyStoreClass, g_KeyStoreGetDefaultType);
    loc[keyStore] = (*env)->CallStaticObjectMethod(env, g_KeyStoreClass, g_KeyStoreGetInstance, loc[ksType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallVoidMethod(env, loc[keyStore], g_KeyStoreLoad, NULL, NULL);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    int32_t status =
        AddCertChainToStore(env, loc[keyStore], pkcs8PrivateKey, pkcs8PrivateKeyLen, algorithm, certs, certsLen);
    if (status != SUCCESS)
        goto cleanup;

    // String kmfType = "PKIX";
    // KeyManagerFactory kmf = KeyManagerFactory.getInstance(kmfType);
    loc[kmfType] = make_java_string(env, "PKIX");
    loc[kmf] = (*env)->CallStaticObjectMethod(env, g_KeyManagerFactory, g_KeyManagerFactoryGetInstance, loc[kmfType]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // kmf.init(keyStore, null);
    (*env)->CallVoidMethod(env, loc[kmf], g_KeyManagerFactoryInit, loc[keyStore], NULL);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // KeyManager[] keyManagers = kmf.getKeyManagers();
    // sslContext.init(keyManagers, null, null);
    loc[keyManagers] = (*env)->CallObjectMethod(env, loc[kmf], g_KeyManagerFactoryGetKeyManagers);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallVoidMethod(env, loc[sslContext], g_SSLContextInitMethod, loc[keyManagers], NULL, NULL);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    sslStream = xcalloc(1, sizeof(SSLStream));
    sslStream->sslContext = ToGRef(env, loc[sslContext]);
    loc[sslContext] = NULL;

cleanup:
    RELEASE_LOCALS(loc, env);
    return sslStream;
}

int32_t AndroidCryptoNative_SSLStreamInitialize(
    SSLStream* sslStream, bool isServer, STREAM_READER streamReader, STREAM_WRITER streamWriter, int32_t appBufferSize)
{
    abort_if_invalid_pointer_argument (sslStream);
    abort_unless(sslStream->sslContext != NULL, "sslContext is NULL in SSL stream");
    abort_unless(sslStream->sslEngine == NULL, "sslEngine is NOT NULL in SSL stream");
    abort_unless(sslStream->sslSession == NULL, "sslSession is NOT NULL in SSL stream");

    int32_t ret = FAIL;
    JNIEnv* env = GetJNIEnv();

    // SSLEngine sslEngine = sslContext.createSSLEngine();
    // sslEngine.setUseClientMode(!isServer);
    jobject sslEngine = (*env)->CallObjectMethod(env, sslStream->sslContext, g_SSLContextCreateSSLEngineMethod);
    ON_EXCEPTION_PRINT_AND_GOTO(exit);
    sslStream->sslEngine = ToGRef(env, sslEngine);
    (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineSetUseClientMode, !isServer);
    ON_EXCEPTION_PRINT_AND_GOTO(exit);

    // SSLSession sslSession = sslEngine.getSession();
    sslStream->sslSession = ToGRef(env, (*env)->CallObjectMethod(env, sslStream->sslEngine, g_SSLEngineGetSession));

    // int applicationBufferSize = sslSession.getApplicationBufferSize();
    // int packetBufferSize = sslSession.getPacketBufferSize();
    int32_t applicationBufferSize =
        (*env)->CallIntMethod(env, sslStream->sslSession, g_SSLSessionGetApplicationBufferSize);
    int32_t packetBufferSize = (*env)->CallIntMethod(env, sslStream->sslSession, g_SSLSessionGetPacketBufferSize);

    // ByteBuffer appInBuffer =  ByteBuffer.allocate(Math.max(applicationBufferSize, appBufferSize));
    // ByteBuffer appOutBuffer = ByteBuffer.allocate(appBufferSize);
    // ByteBuffer netOutBuffer = ByteBuffer.allocate(packetBufferSize);
    // ByteBuffer netInBuffer =  ByteBuffer.allocate(packetBufferSize);
    int32_t appInBufferSize = applicationBufferSize > appBufferSize ? applicationBufferSize : appBufferSize;
    sslStream->appInBuffer =
        ToGRef(env, (*env)->CallStaticObjectMethod(env, g_ByteBuffer, g_ByteBufferAllocate, appInBufferSize));
    sslStream->appOutBuffer =
        ToGRef(env, (*env)->CallStaticObjectMethod(env, g_ByteBuffer, g_ByteBufferAllocate, appBufferSize));
    sslStream->netOutBuffer =
        ToGRef(env, (*env)->CallStaticObjectMethod(env, g_ByteBuffer, g_ByteBufferAllocate, packetBufferSize));
    sslStream->netInBuffer =
        ToGRef(env, (*env)->CallStaticObjectMethod(env, g_ByteBuffer, g_ByteBufferAllocate, packetBufferSize));

    sslStream->streamReader = streamReader;
    sslStream->streamWriter = streamWriter;

    ret = SUCCESS;

exit:
    return ret;
}

int32_t AndroidCryptoNative_SSLStreamSetTargetHost(SSLStream* sslStream, char* targetHost)
{
    abort_if_invalid_pointer_argument (sslStream);
    abort_if_invalid_pointer_argument (targetHost);

    if (g_SNIHostName == NULL || g_SSLParametersSetServerNames == NULL)
    {
        // SSL not supported below API Level 24
        return UNSUPPORTED_API_LEVEL;
    }

    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;
    INIT_LOCALS(loc, hostStr, nameList, hostName, params);

    // ArrayList<SNIServerName> nameList = new ArrayList<SNIServerName>();
    // SNIHostName hostName = new SNIHostName(targetHost);
    // nameList.add(hostName);
    loc[hostStr] = make_java_string(env, targetHost);
    loc[nameList] = (*env)->NewObject(env, g_ArrayListClass, g_ArrayListCtor);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[hostName] = (*env)->NewObject(env, g_SNIHostName, g_SNIHostNameCtor, loc[hostStr]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallBooleanMethod(env, loc[nameList], g_ArrayListAdd, loc[hostName]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // SSLParameters params = sslEngine.getSSLParameters();
    // params.setServerNames(nameList);
    // sslEngine.setSSLParameters(params);
    loc[params] = (*env)->CallObjectMethod(env, sslStream->sslEngine, g_SSLEngineGetSSLParameters);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallVoidMethod(env, loc[params], g_SSLParametersSetServerNames, loc[nameList]);
    (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineSetSSLParameters, loc[params]);

    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

PAL_SSLStreamStatus AndroidCryptoNative_SSLStreamHandshake(SSLStream* sslStream)
{
    abort_if_invalid_pointer_argument (sslStream);
    JNIEnv* env = GetJNIEnv();

    // sslEngine.beginHandshake();
    (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineBeginHandshake);
    if (CheckJNIExceptions(env))
        return SSLStreamStatus_Error;

    return DoHandshake(env, sslStream);
}

PAL_SSLStreamStatus
AndroidCryptoNative_SSLStreamRead(SSLStream* sslStream, uint8_t* buffer, int32_t length, int32_t* read)
{
    abort_if_invalid_pointer_argument (sslStream);
    abort_if_invalid_pointer_argument (read);

    jbyteArray data = NULL;
    JNIEnv* env = GetJNIEnv();
    PAL_SSLStreamStatus ret = SSLStreamStatus_Error;
    *read = 0;

    /*
        appInBuffer.flip();
        if (appInBuffer.remaining() == 0) {
            appInBuffer.compact();
            DoUnwrap();
            appInBuffer.flip();
        }
        if (appInBuffer.remaining() > 0) {
            byte[] data = new byte[appInBuffer.remaining()];
            appInBuffer.get(data);
            appInBuffer.compact();
            return SSLStreamStatus_OK;
        } else {
            return SSLStreamStatus_NeedData;
        }
    */

    IGNORE_RETURN((*env)->CallObjectMethod(env, sslStream->appInBuffer, g_ByteBufferFlip));
    int32_t rem = (*env)->CallIntMethod(env, sslStream->appInBuffer, g_ByteBufferRemaining);
    if (rem == 0)
    {
        IGNORE_RETURN((*env)->CallObjectMethod(env, sslStream->appInBuffer, g_ByteBufferCompact));
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

        int handshakeStatus;
        PAL_SSLStreamStatus unwrapStatus = DoUnwrap(env, sslStream, &handshakeStatus);
        if (unwrapStatus != SSLStreamStatus_OK)
        {
            ret = unwrapStatus;
            goto cleanup;
        }

        IGNORE_RETURN((*env)->CallObjectMethod(env, sslStream->appInBuffer, g_ByteBufferFlip));

        if (IsHandshaking(handshakeStatus))
        {
            ret = SSLStreamStatus_Renegotiate;
            goto cleanup;
        }

        rem = (*env)->CallIntMethod(env, sslStream->appInBuffer, g_ByteBufferRemaining);
    }

    if (rem > 0)
    {
        data = make_java_byte_array(env, rem);
        IGNORE_RETURN((*env)->CallObjectMethod(env, sslStream->appInBuffer, g_ByteBufferGet, data));
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        IGNORE_RETURN((*env)->CallObjectMethod(env, sslStream->appInBuffer, g_ByteBufferCompact));
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        (*env)->GetByteArrayRegion(env, data, 0, rem, (jbyte*)buffer);
        *read = rem;
        ret = SSLStreamStatus_OK;
    }
    else
    {
        ret = SSLStreamStatus_NeedData;
    }

cleanup:
    ReleaseLRef(env, data);
    return ret;
}

PAL_SSLStreamStatus AndroidCryptoNative_SSLStreamWrite(SSLStream* sslStream, uint8_t* buffer, int32_t length)
{
    abort_if_invalid_pointer_argument (sslStream);

    JNIEnv* env = GetJNIEnv();
    PAL_SSLStreamStatus ret = SSLStreamStatus_Error;

    // int remaining = appOutBuffer.remaining();
    // int arraySize = length > remaining ? remaining : length;
    // byte[] data = new byte[arraySize];
    int32_t remaining = (*env)->CallIntMethod(env, sslStream->appOutBuffer, g_ByteBufferRemaining);
    int32_t arraySize = length > remaining ? remaining : length;
    jbyteArray data = make_java_byte_array(env, arraySize);

    int32_t written = 0;
    while (written < length)
    {
        int32_t toWrite = length - written > arraySize ? arraySize : length - written;
        (*env)->SetByteArrayRegion(env, data, 0, toWrite, (jbyte*)(buffer + written));

        // appOutBuffer.put(data, 0, toWrite);
        IGNORE_RETURN((*env)->CallObjectMethod(env, sslStream->appOutBuffer, g_ByteBufferPutByteArrayWithLength, data, 0, toWrite));
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        written += toWrite;

        int handshakeStatus;
        ret = DoWrap(env, sslStream, &handshakeStatus);
        if (ret != SSLStreamStatus_OK)
        {
            goto cleanup;
        }
        else if (IsHandshaking(handshakeStatus))
        {
            ret = SSLStreamStatus_Renegotiate;
            goto cleanup;
        }
    }

cleanup:
    (*env)->DeleteLocalRef(env, data);
    return ret;
}

void AndroidCryptoNative_SSLStreamRelease(SSLStream* sslStream)
{
    if (sslStream == NULL)
        return;

    JNIEnv* env = GetJNIEnv();
    FreeSSLStream(env, sslStream);
}

int32_t AndroidCryptoNative_SSLStreamGetApplicationProtocol(SSLStream* sslStream, uint8_t* out, int32_t* outLen)
{
    if (g_SSLEngineGetApplicationProtocol == NULL)
    {
        // SSLEngine.getApplicationProtocol() is only supported from API level 29 and above
        return FAIL;
    }

    abort_if_invalid_pointer_argument (sslStream);
    abort_if_invalid_pointer_argument (outLen);

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;

    // String protocol = sslEngine.getApplicationProtocol();
    jstring protocol = (*env)->CallObjectMethod(env, sslStream->sslEngine, g_SSLEngineGetApplicationProtocol);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    if (protocol == NULL)
        goto cleanup;

    jsize len = (*env)->GetStringUTFLength(env, protocol);
    bool insufficientBuffer = *outLen < len;
    *outLen = len;
    if (insufficientBuffer)
        return INSUFFICIENT_BUFFER;

    (*env)->GetStringUTFRegion(env, protocol, 0, len, (char*)out);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    ret = SUCCESS;

cleanup:
    (*env)->DeleteLocalRef(env, protocol);
    return ret;
}

int32_t AndroidCryptoNative_SSLStreamGetCipherSuite(SSLStream* sslStream, uint16_t** out)
{
    abort_if_invalid_pointer_argument (sslStream);
    abort_if_invalid_pointer_argument (out);

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;
    *out = NULL;

    // String cipherSuite = sslSession.getCipherSuite();
    jstring cipherSuite = (*env)->CallObjectMethod(env, sslStream->sslSession, g_SSLSessionGetCipherSuite);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    *out = AllocateString(env, cipherSuite);

    ret = SUCCESS;

cleanup:
    (*env)->DeleteLocalRef(env, cipherSuite);
    return ret;
}

int32_t AndroidCryptoNative_SSLStreamGetProtocol(SSLStream* sslStream, uint16_t** out)
{
    abort_if_invalid_pointer_argument (sslStream);
    abort_if_invalid_pointer_argument (out);

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;
    *out = NULL;

    // String protocol = sslSession.getProtocol();
    jstring protocol = (*env)->CallObjectMethod(env, sslStream->sslSession, g_SSLSessionGetProtocol);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    *out = AllocateString(env, protocol);

    ret = SUCCESS;

cleanup:
    (*env)->DeleteLocalRef(env, protocol);
    return ret;
}

jobject /*X509Certificate*/ AndroidCryptoNative_SSLStreamGetPeerCertificate(SSLStream* sslStream)
{
    abort_if_invalid_pointer_argument (sslStream);

    JNIEnv* env = GetJNIEnv();
    jobject ret = NULL;

    // Certificate[] certs = sslSession.getPeerCertificates();
    // out = certs[0];
    jobjectArray certs = (*env)->CallObjectMethod(env, sslStream->sslSession, g_SSLSessionGetPeerCertificates);

    // If there are no peer certificates, getPeerCertificates will throw. Return null to indicate no certificate.
    if (TryClearJNIExceptions(env))
        goto cleanup;

    jsize len = (*env)->GetArrayLength(env, certs);
    if (len > 0)
    {
        // First element is the peer's own certificate
        jobject cert = (*env)->GetObjectArrayElement(env, certs, 0);
        ret = ToGRef(env, cert);
    }

cleanup:
    (*env)->DeleteLocalRef(env, certs);
    return ret;
}

void AndroidCryptoNative_SSLStreamGetPeerCertificates(SSLStream* sslStream, jobject** out, int32_t* outLen)
{
    abort_if_invalid_pointer_argument (sslStream);
    abort_if_invalid_pointer_argument (out);
    abort_if_invalid_pointer_argument (outLen);

    JNIEnv* env = GetJNIEnv();
    *out = NULL;
    *outLen = 0;

    // Certificate[] certs = sslSession.getPeerCertificates();
    // for (int i = 0; i < certs.length; i++) {
    //     out[i] = certs[i];
    // }
    jobjectArray certs = (*env)->CallObjectMethod(env, sslStream->sslSession, g_SSLSessionGetPeerCertificates);

    // If there are no peer certificates, getPeerCertificates will throw. Return null and length of zero to indicate no certificates.
    if (TryClearJNIExceptions(env))
        goto cleanup;

    jsize len = (*env)->GetArrayLength(env, certs);
    *outLen = len;
    if (len > 0)
    {
        *out = xmalloc(sizeof(jobject) * (size_t)len);
        for (int32_t i = 0; i < len; i++)
        {
            jobject cert = (*env)->GetObjectArrayElement(env, certs, i);
            (*out)[i] = ToGRef(env, cert);
        }
    }

cleanup:
    (*env)->DeleteLocalRef(env, certs);
}

void AndroidCryptoNative_SSLStreamRequestClientAuthentication(SSLStream* sslStream)
{
    abort_if_invalid_pointer_argument (sslStream);
    JNIEnv* env = GetJNIEnv();

    // sslEngine.setWantClientAuth(true);
    (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineSetWantClientAuth, true);
}

int32_t AndroidCryptoNative_SSLStreamSetApplicationProtocols(SSLStream* sslStream,
                                                             ApplicationProtocolData* protocolData,
                                                             int32_t count)
{
    abort_if_invalid_pointer_argument (sslStream);
    abort_if_invalid_pointer_argument (protocolData);

    if (!AndroidCryptoNative_SSLSupportsApplicationProtocolsConfiguration()) {
        LOG_ERROR ("SSL does not support application protocols configuration");
        return FAIL;
    }

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;
    INIT_LOCALS(loc, protocols, params);

    // String[] protocols = new String[count];
    loc[protocols] = make_java_object_array(env, count, g_String, NULL);
    for (int32_t i = 0; i < count; ++i)
    {
        // Data length + 1 for null terminator
        int32_t len = protocolData[i].length;
        char* data = (char*)xmalloc((size_t)(len + 1) * sizeof(char));
        memcpy(data, protocolData[i].data, (size_t)len);
        data[len] = '\0';

        jstring protocol = make_java_string(env, data);
        free(data);
        (*env)->SetObjectArrayElement(env, loc[protocols], i, protocol);
        (*env)->DeleteLocalRef(env, protocol);
    }

    // SSLParameters params = sslEngine.getSSLParameters();
    // params.setApplicationProtocols(protocols);
    // sslEngine.setSSLParameters(params);
    loc[params] = (*env)->CallObjectMethod(env, sslStream->sslEngine, g_SSLEngineGetSSLParameters);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallVoidMethod(env, loc[params], g_SSLParametersSetApplicationProtocols, loc[protocols]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineSetSSLParameters, loc[params]);

    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

static jstring GetSslProtocolAsString(JNIEnv* env, PAL_SslProtocol protocol)
{
    switch (protocol)
    {
        case PAL_SslProtocol_Tls10:
            return make_java_string(env, "TLSv1");
        case PAL_SslProtocol_Tls11:
            return make_java_string(env, "TLSv1.1");
        case PAL_SslProtocol_Tls12:
            return make_java_string(env, "TLSv1.2");
        case PAL_SslProtocol_Tls13:
            return make_java_string(env, "TLSv1.3");
        default:
            LOG_ERROR("Unsupported SslProtocols value: %d", protocol);
            return NULL;
    }
}

int32_t
AndroidCryptoNative_SSLStreamSetEnabledProtocols(SSLStream* sslStream, PAL_SslProtocol* protocols, int32_t count)
{
    abort_if_invalid_pointer_argument (sslStream);
    abort_if_invalid_pointer_argument (protocols);

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;

    // String[] protocolsArray = new String[count];
    jobjectArray protocolsArray = make_java_object_array(env, count, g_String, NULL);
    for (int32_t i = 0; i < count; ++i)
    {
        jstring protocol = GetSslProtocolAsString(env, protocols[i]);
        (*env)->SetObjectArrayElement(env, protocolsArray, i, protocol);
        (*env)->DeleteLocalRef(env, protocol);
    }

    // sslEngine.setEnabledProtocols(protocolsArray);
    (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineSetEnabledProtocols, protocolsArray);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    ret = SUCCESS;

cleanup:
    (*env)->DeleteLocalRef(env, protocolsArray);
    return ret;
}

bool AndroidCryptoNative_SSLStreamVerifyHostname(SSLStream* sslStream, char* hostname)
{
    abort_if_invalid_pointer_argument (sslStream);
    abort_if_invalid_pointer_argument (hostname);
    JNIEnv* env = GetJNIEnv();

    bool ret = false;
    INIT_LOCALS(loc, name, verifier);

    // HostnameVerifier verifier = HttpsURLConnection.getDefaultHostnameVerifier();
    // return verifier.verify(hostname, sslSession);
    loc[name] = make_java_string(env, hostname);
    loc[verifier] =
        (*env)->CallStaticObjectMethod(env, g_HttpsURLConnection, g_HttpsURLConnectionGetDefaultHostnameVerifier);
    ret = (*env)->CallBooleanMethod(env, loc[verifier], g_HostnameVerifierVerify, loc[name], sslStream->sslSession);

    RELEASE_LOCALS(loc, env);
    return ret;
}

bool AndroidCryptoNative_SSLStreamShutdown(SSLStream* sslStream)
{
    abort_if_invalid_pointer_argument (sslStream);
    JNIEnv* env = GetJNIEnv();

    PAL_SSLStreamStatus status = Close(env, sslStream);
    return status == SSLStreamStatus_Closed;
}

static uint16_t* AllocateString(JNIEnv* env, jstring source)
{
    if (source == NULL)
        return NULL;

    // Length with null terminator
    jsize len = (*env)->GetStringLength(env, source);

    // +1 for null terminator.
    uint16_t* buffer = xmalloc(sizeof(uint16_t) * (size_t)(len + 1));
    buffer[len] = '\0';

    (*env)->GetStringRegion(env, source, 0, len, (jchar*)buffer);
    return buffer;
}
