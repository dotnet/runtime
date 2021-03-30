// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_sslstream.h"

#define INSUFFICIENT_BUFFER -1

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

static uint16_t* AllocateString(JNIEnv *env, jstring source);
static int32_t PopulateByteArray(JNIEnv *env, jbyteArray source, uint8_t *dest, int32_t *len);

static PAL_SSLStreamStatus DoHandshake(JNIEnv* env, SSLStream* sslStream);
static PAL_SSLStreamStatus doWrap(JNIEnv* env, SSLStream* sslStream, int* hs);
static void flush(JNIEnv* env, SSLStream* sslStream);

static int getHandshakeStatus(JNIEnv* env, SSLStream* sslStream, jobject engineResult)
{
    AssertOnJNIExceptions(env);
    int status = -1;
    if (engineResult)
        status = GetEnumAsInt(env, (*env)->CallObjectMethod(env, engineResult, g_SSLEngineResultGetHandshakeStatusMethod));
    else
        status = GetEnumAsInt(env, (*env)->CallObjectMethod(env, sslStream->sslEngine, g_SSLEngineGetHandshakeStatusMethod));
    AssertOnJNIExceptions(env);
    return status;
}

static bool IsHandshaking(int handshakeStatus)
{
    return handshakeStatus != HANDSHAKE_STATUS__NOT_HANDSHAKING
        && handshakeStatus != HANDSHAKE_STATUS__FINISHED;
}

static PAL_SSLStreamStatus close(JNIEnv* env, SSLStream* sslStream)
{
    // sslEngine.closeOutbound();
    (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineCloseOutboundMethod);

    // Call wrap to clear any remaining data
    int unused;
    return doWrap(env, sslStream, &unused);
}

static void flush(JNIEnv* env, SSLStream* sslStream)
{
    /*
        netOutBuffer.flip();
        byte[] data = new byte[netOutBuffer.limit()];
        netOutBuffer.get(data);
        WriteToOutputStream(data, 0, data.length);
        netOutBuffer.compact();
    */

    AssertOnJNIExceptions(env);

    // DeleteLocalRef because we don't need the return value (Buffer)
    (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->netOutBuffer, g_ByteBufferFlipMethod));
    int bufferLimit = (*env)->CallIntMethod(env, sslStream->netOutBuffer, g_ByteBufferLimitMethod);
    jbyteArray data = (*env)->NewByteArray(env, bufferLimit);

    // DeleteLocalRef because we don't need the return value (Buffer)
    (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->netOutBuffer, g_ByteBufferGetMethod, data));

    uint8_t* dataPtr = (uint8_t*)malloc((size_t)bufferLimit);
    (*env)->GetByteArrayRegion(env, data, 0, bufferLimit, (jbyte*) dataPtr);
    sslStream->streamWriter(dataPtr, bufferLimit);
    free(dataPtr);
    // DeleteLocalRef because we don't need the return value (Buffer)
    (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->netOutBuffer, g_ByteBufferCompactMethod));
    AssertOnJNIExceptions(env);
}

static jobject ensureRemaining(JNIEnv* env, SSLStream* sslStream, jobject oldBuffer, int newRemaining)
{
    /*
        if (oldBuffer.remaining() < newRemaining) {
            oldBuffer.flip();
            final ByteBuffer newBuffer = ByteBuffer.allocate(oldBuffer.remaining() + newRemaining);
            newBuffer.put(oldBuffer);
            return newBuffer;
        } else {
            return oldBuffer;
        }
    */

    AssertOnJNIExceptions(env);

    int oldRemaining = (*env)->CallIntMethod(env, oldBuffer, g_ByteBufferRemainingMethod);
    if (oldRemaining < newRemaining)
    {
        // DeleteLocalRef because we don't need the return value (Buffer)
        (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, oldBuffer, g_ByteBufferFlipMethod));
        jobject newBuffer = ToGRef(env, (*env)->CallStaticObjectMethod(env, g_ByteBuffer, g_ByteBufferAllocateMethod, oldRemaining + newRemaining));
        // DeleteLocalRef because we don't need the return value (Buffer)
        (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, newBuffer, g_ByteBufferPutBufferMethod, oldBuffer));
        ReleaseGRef(env, oldBuffer);
        return newBuffer;
    }
    else
    {
        return oldBuffer;
    }
}

static PAL_SSLStreamStatus doWrap(JNIEnv* env, SSLStream* sslStream, int* handshakeStatus)
{
    // appOutBuffer.flip();
    // SSLEngineResult result = sslEngine.wrap(appOutBuffer, netOutBuffer);
    (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->appOutBuffer, g_ByteBufferFlipMethod));
    jobject sslEngineResult = (*env)->CallObjectMethod(env, sslStream->sslEngine, g_SSLEngineWrapMethod, sslStream->appOutBuffer, sslStream->netOutBuffer);
    if (CheckJNIExceptions(env))
        return SSLStreamStatus_Error;

    // appOutBuffer.compact();
    (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->appOutBuffer, g_ByteBufferCompactMethod));

    // handshakeStatus = result.getHandshakeStatus();
    // SSLEngineResult.Status status = result.getStatus();
    *handshakeStatus = getHandshakeStatus(env, sslStream, sslEngineResult);
    int status = GetEnumAsInt(env, (*env)->CallObjectMethod(env, sslEngineResult, g_SSLEngineResultGetStatusMethod));
    switch (status)
    {
        case STATUS__OK:
        {
            flush(env, sslStream);
            return SSLStreamStatus_OK;
        }
        case STATUS__CLOSED:
        {
            flush(env, sslStream);
            (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineCloseOutboundMethod);
            return SSLStreamStatus_Closed;
        }
        case STATUS__BUFFER_OVERFLOW:
        {
            // Expand buffer
            // int newRemaining = sslSession.getPacketBufferSize();
            int newRemaining = (*env)->CallIntMethod(env, sslStream->sslSession, g_SSLSessionGetPacketBufferSizeMethod);
            sslStream->netOutBuffer = ensureRemaining(env, sslStream, sslStream->netOutBuffer, newRemaining);
            return SSLStreamStatus_OK;
        }
        default:
        {
            LOG_ERROR("Unknown SSLEngineResult status: %d", status);
            return SSLStreamStatus_Error;
        }
    }
}

static PAL_SSLStreamStatus doUnwrap(JNIEnv* env, SSLStream* sslStream, int *handshakeStatus)
{
    // if (netInBuffer.position() == 0)
    // {
    //     byte[] tmp = new byte[netInBuffer.limit()];
    //     int count = ReadFromInputStream(tmp, 0, tmp.length);
    //     netInBuffer.put(tmp, 0, count);
    // }
    if ((*env)->CallIntMethod(env, sslStream->netInBuffer, g_ByteBufferPositionMethod) == 0)
    {
        int netInBufferLimit = (*env)->CallIntMethod(env, sslStream->netInBuffer, g_ByteBufferLimitMethod);
        jbyteArray tmp = (*env)->NewByteArray(env, netInBufferLimit);
        uint8_t* tmpNative = (uint8_t*)malloc((size_t)netInBufferLimit);
        int count = netInBufferLimit;
        PAL_SSLStreamStatus status = sslStream->streamReader(tmpNative, &count);
        if (status != SSLStreamStatus_OK)
        {
            return status;
        }

        (*env)->SetByteArrayRegion(env, tmp, 0, count, (jbyte*)(tmpNative));
        (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->netInBuffer, g_ByteBufferPutWythByteArrayLength, tmp, 0, count));
        free(tmpNative);
        (*env)->DeleteLocalRef(env, tmp);
    }

    // netInBuffer.flip();
    // SSLEngineResult result = sslEngine.unwrap(netInBuffer, appInBuffer);
    (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->netInBuffer, g_ByteBufferFlipMethod));
    jobject sslEngineResult = (*env)->CallObjectMethod(env, sslStream->sslEngine, g_SSLEngineUnwrapMethod, sslStream->netInBuffer, sslStream->appInBuffer);
    if (CheckJNIExceptions(env))
        return SSLStreamStatus_Error;

    // netInBuffer.compact();
    (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->netInBuffer, g_ByteBufferCompactMethod));

    // handshakeStatus = result.getHandshakeStatus();
    // SSLEngineResult.Status status = result.getStatus();
    *handshakeStatus = getHandshakeStatus(env, sslStream, sslEngineResult);
    int status = GetEnumAsInt(env, (*env)->CallObjectMethod(env, sslEngineResult, g_SSLEngineResultGetStatusMethod));
    switch (status)
    {
        case STATUS__OK:
        {
            return SSLStreamStatus_OK;
        }
        case STATUS__CLOSED:
        {
            return close(env, sslStream);
        }
        case STATUS__BUFFER_UNDERFLOW:
        {
            // Expand buffer
            // int newRemaining = sslSession.getPacketBufferSize();
            int newRemaining = (*env)->CallIntMethod(env, sslStream->sslSession, g_SSLSessionGetPacketBufferSizeMethod);
            sslStream->netInBuffer = ensureRemaining(env, sslStream, sslStream->netInBuffer, newRemaining);
            return SSLStreamStatus_OK;
        }
        case STATUS__BUFFER_OVERFLOW:
        {
            // Expand buffer
            // int newRemaining = sslSession.getApplicationBufferSize();
            int newRemaining = (*env)->CallIntMethod(env, sslStream->sslSession, g_SSLSessionGetApplicationBufferSizeMethod);
            sslStream->appInBuffer = ensureRemaining(env, sslStream, sslStream->appInBuffer, newRemaining);
            return SSLStreamStatus_OK;
        }
        default:
        {
            LOG_ERROR("Unknown SSLEngineResult status: %d", status);
            return SSLStreamStatus_Error;
        }
    }
}

static PAL_SSLStreamStatus DoHandshake(JNIEnv* env, SSLStream* sslStream)
{
    assert(env != NULL);
    assert(sslStream != NULL);

    PAL_SSLStreamStatus status = SSLStreamStatus_OK;
    int handshakeStatus = getHandshakeStatus(env, sslStream, NULL);
    while (IsHandshaking(handshakeStatus) && status == SSLStreamStatus_OK)
    {
        switch (handshakeStatus)
        {
            case HANDSHAKE_STATUS__NEED_WRAP:
                status = doWrap(env, sslStream, &handshakeStatus);
                break;
            case HANDSHAKE_STATUS__NEED_UNWRAP:
                status = doUnwrap(env, sslStream, &handshakeStatus);
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

static void FreeSSLStream(JNIEnv *env, SSLStream *sslStream)
{
    assert(sslStream != NULL);
    ReleaseGRef(env, sslStream->sslContext);
    ReleaseGRef(env, sslStream->sslEngine);
    ReleaseGRef(env, sslStream->sslSession);
    ReleaseGRef(env, sslStream->appOutBuffer);
    ReleaseGRef(env, sslStream->netOutBuffer);
    ReleaseGRef(env, sslStream->netInBuffer);
    ReleaseGRef(env, sslStream->appInBuffer);
    free(sslStream);
}

SSLStream* AndroidCryptoNative_SSLStreamCreate(
    bool isServer,
    STREAM_READER streamReader,
    STREAM_WRITER streamWriter,
    int appOutBufferSize,
    int appInBufferSize)
{
    JNIEnv* env = GetJNIEnv();

    SSLStream* sslStream = malloc(sizeof(SSLStream));
    memset(sslStream, 0, sizeof(SSLStream));

    // TODO: [AndroidCrypto] If we have certificates, get an SSLContext instance with the highest available
    // protocol - TLSv1.2 (API level 16+) or TLSv1.3 (API level 29+), use KeyManagerFactory to create key
    // managers that will return the certificates, and initialize the SSLContext with the key managers.

    // SSLContext sslContext = SSLContext.getDefault();
    jobject sslContext = (*env)->CallStaticObjectMethod(env, g_SSLContext, g_SSLContextGetDefault);
    ON_EXCEPTION_PRINT_AND_GOTO(fail);
    sslStream->sslContext = ToGRef(env, sslContext);

    // SSLEngine sslEngine = sslContext.createSSLEngine();
    // sslEngine.setUseClientMode(!isServer);
    jobject sslEngine = (*env)->CallObjectMethod(env, sslStream->sslContext, g_SSLContextCreateSSLEngineMethod);
    ON_EXCEPTION_PRINT_AND_GOTO(fail);
    sslStream->sslEngine  = ToGRef(env, sslEngine);
    (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineSetUseClientModeMethod, !isServer);
    ON_EXCEPTION_PRINT_AND_GOTO(fail);

    // SSLSession sslSession = sslEngine.getSession();
    sslStream->sslSession = ToGRef(env, (*env)->CallObjectMethod(env, sslStream->sslEngine, g_SSLEngineGetSessionMethod));

    // final int applicationBufferSize = sslSession.getApplicationBufferSize();
    // final int packetBufferSize = sslSession.getPacketBufferSize();
    int applicationBufferSize = (*env)->CallIntMethod(env, sslStream->sslSession, g_SSLSessionGetApplicationBufferSizeMethod);
    int packetBufferSize = (*env)->CallIntMethod(env, sslStream->sslSession, g_SSLSessionGetPacketBufferSizeMethod);

    // ByteBuffer appOutBuffer = ByteBuffer.allocate(appOutBufferSize);
    // ByteBuffer netOutBuffer = ByteBuffer.allocate(packetBufferSize);
    // ByteBuffer netInBuffer =  ByteBuffer.allocate(packetBufferSize);
    // ByteBuffer appInBuffer =  ByteBuffer.allocate(Math.max(applicationBufferSize, appInBufferSize));
    sslStream->appOutBuffer = ToGRef(env, (*env)->CallStaticObjectMethod(env, g_ByteBuffer, g_ByteBufferAllocateMethod, appOutBufferSize));
    sslStream->netOutBuffer = ToGRef(env, (*env)->CallStaticObjectMethod(env, g_ByteBuffer, g_ByteBufferAllocateMethod, packetBufferSize));
    sslStream->appInBuffer =  ToGRef(env, (*env)->CallStaticObjectMethod(env, g_ByteBuffer, g_ByteBufferAllocateMethod,
            applicationBufferSize > appInBufferSize ? applicationBufferSize : appInBufferSize));
    sslStream->netInBuffer =  ToGRef(env, (*env)->CallStaticObjectMethod(env, g_ByteBuffer, g_ByteBufferAllocateMethod, packetBufferSize));

    sslStream->streamReader = streamReader;
    sslStream->streamWriter = streamWriter;

    return sslStream;

fail:
    if (sslStream != NULL)
        FreeSSLStream(env, sslStream);

    return NULL;
}

int32_t AndroidCryptoNative_SSLStreamConfigureParameters(SSLStream *sslStream, char* targetHost)
{
    assert(sslStream != NULL);
    assert(targetHost != NULL);

    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;
    INIT_LOCALS(loc, hostStr, nameList, hostName, params);

    // ArrayList<SNIServerName> nameList = new ArrayList<SNIServerName>();
    // SNIHostName hostName = new SNIHostName(targetHost);
    // nameList.add(hostName);
    loc[hostStr] = JSTRING(targetHost);
    loc[nameList] = (*env)->NewObject(env, g_ArrayListClass, g_ArrayListCtor);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[hostName] = (*env)->NewObject(env, g_SNIHostName, g_SNIHostNameCtor, loc[hostStr]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallBooleanMethod(env, loc[nameList], g_ArrayListAdd, loc[hostName]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // SSLParameters params = new SSLParameters();
    // params.setServerNames(nameList);
    // sslEngine.setSSLParameters(params);
    loc[params] = (*env)->NewObject(env, g_sslParamsClass, g_sslParamsCtor);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallVoidMethod(env, loc[params], g_sslParamsSetServerNames, loc[nameList]);
    (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineSetSSLParameters, loc[params]);

    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

PAL_SSLStreamStatus AndroidCryptoNative_SSLStreamHandshake(SSLStream *sslStream)
{
    assert(sslStream != NULL);
    JNIEnv* env = GetJNIEnv();

    (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineBeginHandshakeMethod);
    if (CheckJNIExceptions(env))
        return SSLStreamStatus_Error;

    return DoHandshake(env, sslStream);
}

SSLStream* AndroidCryptoNative_SSLStreamCreateAndStartHandshake(
    STREAM_READER streamReader,
    STREAM_WRITER streamWriter,
    int tlsVersion,
    int appOutBufferSize,
    int appInBufferSize)
{
    JNIEnv* env = GetJNIEnv();
    /*
        SSLContext sslContext = SSLContext.getInstance("TLSv1.2");
        sslContext.init(null, new TrustManage r[]{trustAllCerts}, null);
        this.sslEngine = sslContext.createSSLEngine();
        this.sslEngine.setUseClientMode(true);
        SSLSession sslSession = sslEngine.getSession();
        final int applicationBufferSize = sslSession.getApplicationBufferSize();
        final int packetBufferSize = sslSession.getPacketBufferSize();
        this.appOutBuffer = ByteBuffer.allocate(appOutBufferSize);
        this.netOutBuffer = ByteBuffer.allocate(packetBufferSize);
        this.netInBuffer =  ByteBuffer.allocate(packetBufferSize);
        this.appInBuffer =  ByteBuffer.allocate(Math.max(applicationBufferSize, appInBufferSize));
        sslEngine.beginHandshake();
    */

    SSLStream* sslStream = malloc(sizeof(SSLStream));

    jobject tlsVerStr = NULL;
    if (tlsVersion == 11)
        tlsVerStr = JSTRING("TLSv1.1");
    else if (tlsVersion == 12)
        tlsVerStr = JSTRING("TLSv1.2");
    else if (tlsVersion == 13)
        tlsVerStr = JSTRING("TLSv1.3");
    else
        assert(0 && "unknown tlsVersion");

    sslStream->sslContext = ToGRef(env, (*env)->CallStaticObjectMethod(env, g_SSLContext, g_SSLContextGetInstanceMethod, tlsVerStr));

    // TODO: set TrustManager[] argument to be able to intercept cert validation process (and callback to C#).
    (*env)->CallVoidMethod(env, sslStream->sslContext, g_SSLContextInitMethod, NULL, NULL, NULL);
    sslStream->sslEngine  = ToGRef(env, (*env)->CallObjectMethod(env, sslStream->sslContext, g_SSLContextCreateSSLEngineMethod));
    sslStream->sslSession = ToGRef(env, (*env)->CallObjectMethod(env, sslStream->sslEngine, g_SSLEngineGetSessionMethod));

    int applicationBufferSize = (*env)->CallIntMethod(env, sslStream->sslSession, g_SSLSessionGetApplicationBufferSizeMethod);
    int packetBufferSize = (*env)->CallIntMethod(env, sslStream->sslSession, g_SSLSessionGetPacketBufferSizeMethod);

    (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineSetUseClientModeMethod, true);

    sslStream->appOutBuffer = ToGRef(env, (*env)->CallStaticObjectMethod(env, g_ByteBuffer, g_ByteBufferAllocateMethod, appOutBufferSize));
    sslStream->netOutBuffer = ToGRef(env, (*env)->CallStaticObjectMethod(env, g_ByteBuffer, g_ByteBufferAllocateMethod, packetBufferSize));
    sslStream->appInBuffer =  ToGRef(env, (*env)->CallStaticObjectMethod(env, g_ByteBuffer, g_ByteBufferAllocateMethod,
            applicationBufferSize > appInBufferSize ? applicationBufferSize : appInBufferSize));
    sslStream->netInBuffer =  ToGRef(env, (*env)->CallStaticObjectMethod(env, g_ByteBuffer, g_ByteBufferAllocateMethod, packetBufferSize));

    sslStream->streamReader = streamReader;
    sslStream->streamWriter = streamWriter;

    (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineBeginHandshakeMethod);

    DoHandshake(env, sslStream);
    (*env)->DeleteLocalRef(env, tlsVerStr);
    AssertOnJNIExceptions(env);
    return sslStream;
}

PAL_SSLStreamStatus AndroidCryptoNative_SSLStreamRead(SSLStream* sslStream, uint8_t* buffer, int length, int* read)
{
    assert(sslStream != NULL);
    assert(read != NULL);

    jbyteArray data = NULL;
    JNIEnv* env = GetJNIEnv();
    PAL_SSLStreamStatus ret = SSLStreamStatus_Error;
    *read = 0;

    /*
        appInBuffer.flip();
        if (appInBuffer.remaining() == 0) {
            appInBuffer.compact();
            doUnwrap();
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

    (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->appInBuffer, g_ByteBufferFlipMethod));
    int rem = (*env)->CallIntMethod(env, sslStream->appInBuffer, g_ByteBufferRemainingMethod);
    if (rem == 0)
    {
        (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->appInBuffer, g_ByteBufferCompactMethod));
        int handshakeStatus;
        PAL_SSLStreamStatus unwrapStatus = doUnwrap(env, sslStream, &handshakeStatus);
        if (unwrapStatus != SSLStreamStatus_OK)
        {
            ret = unwrapStatus;
            goto cleanup;
        }

        (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->appInBuffer, g_ByteBufferFlipMethod));

        if (IsHandshaking(handshakeStatus))
        {
            ret = SSLStreamStatus_Renegotiate;
            goto cleanup;
        }

        rem = (*env)->CallIntMethod(env, sslStream->appInBuffer, g_ByteBufferRemainingMethod);
    }

    if (rem > 0)
    {
        data = (*env)->NewByteArray(env, rem);
        (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->appInBuffer, g_ByteBufferGetMethod, data));
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->appInBuffer, g_ByteBufferCompactMethod));
        ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
        (*env)->GetByteArrayRegion(env, data, 0, rem, (jbyte*) buffer);
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

PAL_SSLStreamStatus AndroidCryptoNative_SSLStreamWrite(SSLStream* sslStream, uint8_t* buffer, int length)
{
    assert(sslStream != NULL);

    JNIEnv* env = GetJNIEnv();
    PAL_SSLStreamStatus ret = SSLStreamStatus_Error;

    // byte[] data = new byte[] { <buffer> }
    // appOutBuffer.put(data);
    jbyteArray data = (*env)->NewByteArray(env, length);
    (*env)->SetByteArrayRegion(env, data, 0, length, (jbyte*)buffer);
    (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->appOutBuffer, g_ByteBufferPutWithByteArray, data));
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    int handshakeStatus;
    ret = doWrap(env, sslStream, &handshakeStatus);
    if (ret == SSLStreamStatus_OK && IsHandshaking(handshakeStatus))
    {
        ret = SSLStreamStatus_Renegotiate;
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

int32_t AndroidCryptoNative_SSLStreamGetApplicationProtocol(SSLStream* sslStream, uint8_t* out, int* outLen)
{
    assert(sslStream != NULL);

    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;
    INIT_LOCALS(loc, protocol, bytes);

    // String protocol = sslEngine.getApplicationProtocol();
    loc[protocol] = (*env)->CallObjectMethod(env, sslStream->sslEngine, g_SSLEngineGetApplicationProtocol);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    if (loc[protocol] == NULL)
        goto cleanup;

    // byte[] bytes = protocol.getBytes();
    loc[bytes] = (*env)->CallObjectMethod(env, loc[protocol], g_StringGetBytes);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    ret = PopulateByteArray(env, loc[bytes], out, outLen);

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

int32_t AndroidCryptoNative_SSLStreamGetCipherSuite(SSLStream *sslStream, uint16_t** out)
{
    assert(sslStream != NULL);
    assert(out != NULL);

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

int32_t AndroidCryptoNative_SSLStreamGetProtocol(SSLStream *sslStream, uint16_t** out)
{
    assert(sslStream != NULL);
    assert(out != NULL);

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

int32_t AndroidCryptoNative_SSLStreamGetPeerCertificate(SSLStream *sslStream, jobject* out)
{
    assert(sslStream != NULL);
    assert(out != NULL);

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;
    *out = NULL;

    // Certificate[] certs = sslSession.getPeerCertificates();
    jobjectArray certs = (*env)->CallObjectMethod(env, sslStream->sslSession, g_SSLSessionGetPeerCertificates);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    jsize len = (*env)->GetArrayLength(env, certs);
    if (len > 0)
    {
        // First element is the peer's own certificate
        jobject cert =(*env)->GetObjectArrayElement(env, certs, 0);
        *out = ToGRef(env, cert);
    }

    ret = SUCCESS;

cleanup:
    (*env)->DeleteLocalRef(env, certs);
    return ret;
}

int32_t AndroidCryptoNative_SSLStreamGetPeerCertificates(SSLStream *sslStream, jobject** out, int* outLen)
{
    assert(sslStream != NULL);
    assert(out != NULL);

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;
    *out = NULL;
    *outLen = 0;

    // Certificate[] certs = sslSession.getPeerCertificates();
    // for (int i = 0; i < certs.length; i++) {
    //     out[i] = certs[i];
    // }
    jobjectArray certs = (*env)->CallObjectMethod(env, sslStream->sslSession, g_SSLSessionGetPeerCertificates);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    jsize len = (*env)->GetArrayLength(env, certs);
    *outLen = len;
    if (len > 0)
    {
        *out = malloc(sizeof(jobject) * (size_t)len);
        for (int i = 0; i < len; i++)
        {
            jobject cert =(*env)->GetObjectArrayElement(env, certs, i);
            (*out)[i] = ToGRef(env, cert);
        }
    }

    ret = SUCCESS;

cleanup:
    (*env)->DeleteLocalRef(env, certs);
    return ret;
}

bool AndroidCryptoNative_SSLStreamVerifyHostname(SSLStream *sslStream, char* hostname)
{
    assert(sslStream != NULL);
    assert(hostname != NULL);
    JNIEnv* env = GetJNIEnv();

    bool ret = false;
    INIT_LOCALS(loc, name, verifier);

    // HostnameVerifier verifier = HttpsURLConnection.getDefaultHostnameVerifier();
    // return verifier.verify(hostname, sslSession);
    loc[name] = JSTRING(hostname);
    loc[verifier] = (*env)->CallStaticObjectMethod(env, g_HttpsURLConnection, g_HttpsURLConnectionGetDefaultHostnameVerifier);
    ret = (*env)->CallBooleanMethod(env, loc[verifier], g_HostnameVerifierVerify, loc[name], sslStream->sslSession);

    RELEASE_LOCALS(loc, env);
    return ret;
}

bool AndroidCryptoNative_SSLStreamShutdown(SSLStream *sslStream)
{
    assert(sslStream != NULL);
    JNIEnv* env = GetJNIEnv();

    PAL_SSLStreamStatus status = close(env, sslStream);
    return status == SSLStreamStatus_Closed;
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

static uint16_t* AllocateString(JNIEnv *env, jstring source)
{
    if (source == NULL)
        return NULL;

    // Length with null terminator
    jsize len = (*env)->GetStringLength(env, source);

    // +1 for null terminator.
    uint16_t* buffer = malloc(sizeof(uint16_t) * (size_t)(len + 1));
    buffer[len] = '\0';

    (*env)->GetStringRegion(env, source, 0, len, (jchar*)buffer);
    return buffer;
}
