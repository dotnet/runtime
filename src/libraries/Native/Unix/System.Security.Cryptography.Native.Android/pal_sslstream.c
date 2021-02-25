// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_sslstream.h"

#define INSUFFICIENT_BUFFER -1

static uint16_t* AllocateString(JNIEnv *env, jstring source);
static int32_t PopulateByteArray(JNIEnv *env, jbyteArray source, uint8_t *dest, int32_t *len);

static void checkHandshakeStatus(JNIEnv* env, SSLStream* sslStream, int handshakeStatus);

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

static void close(JNIEnv* env, SSLStream* sslStream) {
    /*
        sslEngine.closeOutbound();
        checkHandshakeStatus();
    */

    AssertOnJNIExceptions(env);
    (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineCloseOutboundMethod);
    checkHandshakeStatus(env, sslStream, getHandshakeStatus(env, sslStream, NULL));
}

static void handleEndOfStream(JNIEnv* env, SSLStream* sslStream)  {
    /*
        sslEngine.closeInbound();
        close();
    */

    AssertOnJNIExceptions(env);
    (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineCloseInboundMethod);
    close(env, sslStream);
    AssertOnJNIExceptions(env);
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
    sslStream->streamWriter(dataPtr, 0, (uint32_t)bufferLimit);
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

static void doWrap(JNIEnv* env, SSLStream* sslStream)
{
    LOG_DEBUG("doWwrap");
    /*
        appOutBuffer.flip();
        final SSLEngineResult result;
        try {
            result = sslEngine.wrap(appOutBuffer, netOutBuffer);
        } catch (SSLException e) {
            return;
        }
        appOutBuffer.compact();

        final SSLEngineResult.Status status = result.getStatus();
        switch (status) {
            case OK:
                flush();
                checkHandshakeStatus(result.getHandshakeStatus());
                if (appOutBuffer.position() > 0) doWrap();
                break;
            case CLOSED:
                flush();
                checkHandshakeStatus(result.getHandshakeStatus());
                close();
                break;
            case BUFFER_OVERFLOW:
                netOutBuffer = ensureRemaining(netOutBuffer, sslEngine.getSession().getPacketBufferSize());
                doWrap();
                break;
        }
    */

    AssertOnJNIExceptions(env);

    (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->appOutBuffer, g_ByteBufferFlipMethod));
    jobject sslEngineResult = (*env)->CallObjectMethod(env, sslStream->sslEngine, g_SSLEngineWrapMethod, sslStream->appOutBuffer, sslStream->netOutBuffer);
    (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->appOutBuffer, g_ByteBufferCompactMethod));

    int status = GetEnumAsInt(env, (*env)->CallObjectMethod(env, sslEngineResult, g_SSLEngineResultGetStatusMethod));
    LOG_DEBUG("doWwrap status: %d", status);
    switch (status)
    {
        case STATUS__OK:
            flush(env, sslStream);
            checkHandshakeStatus(env, sslStream, getHandshakeStatus(env, sslStream, sslEngineResult));
            if ((*env)->CallIntMethod(env, sslStream->appOutBuffer, g_ByteBufferPositionMethod) > 0)
                doWrap(env, sslStream);
            break;
        case STATUS__CLOSED:
            flush(env, sslStream);
            checkHandshakeStatus(env, sslStream, getHandshakeStatus(env, sslStream, sslEngineResult));
            close(env, sslStream);
            break;
        case STATUS__BUFFER_OVERFLOW:
            sslStream->netOutBuffer = ensureRemaining(env, sslStream, sslStream->netOutBuffer, (*env)->CallIntMethod(env, sslStream->sslSession, g_SSLSessionGetPacketBufferSizeMethod));
            doWrap(env, sslStream);
            break;
    }
}

static void doUnwrap(JNIEnv* env, SSLStream* sslStream)
{
    LOG_DEBUG("doUnwrap");
    /*
        if (netInBuffer.position() == 0)
        {
            byte[] tmp = new byte[netInBuffer.limit()];

            int count = ReadFromInputStream(tmp, 0, tmp.length);
            if (count == -1) {
                handleEndOfStream();
                return;
            }
            netInBuffer.put(tmp, 0, count);
        }

        netInBuffer.flip();
        final SSLEngineResult result;
        try {
            result = sslEngine.unwrap(netInBuffer, appInBuffer);
        } catch (SSLException e) {
            return;
        }
        netInBuffer.compact();
        final SSLEngineResult.Status status = result.getStatus();
        switch (status) {
            case OK:
                checkHandshakeStatus(result.getHandshakeStatus());
                break;
            case CLOSED:
                checkHandshakeStatus(result.getHandshakeStatus());
                close();
                break;
            case BUFFER_UNDERFLOW:
                netInBuffer = ensureRemaining(netInBuffer, sslEngine.getSession().getPacketBufferSize());
                doUnwrap();
                break;
            case BUFFER_OVERFLOW:
                appInBuffer = ensureRemaining(appInBuffer, sslEngine.getSession().getApplicationBufferSize());
                doUnwrap();
                break;
        }
    */

    AssertOnJNIExceptions(env);

    if ((*env)->CallIntMethod(env, sslStream->netInBuffer, g_ByteBufferPositionMethod) == 0)
    {
        int netInBufferLimit = (*env)->CallIntMethod(env, sslStream->netInBuffer, g_ByteBufferLimitMethod);
        jbyteArray tmp = (*env)->NewByteArray(env, netInBufferLimit);
        uint8_t* tmpNative = (uint8_t*)malloc((size_t)netInBufferLimit);
        int count = sslStream->streamReader(tmpNative, 0, (uint32_t)netInBufferLimit);
        if (count == -1)
        {
            handleEndOfStream(env, sslStream);
            return;
        }
        LOG_DEBUG("streamReader return count: %d", count);
        (*env)->SetByteArrayRegion(env, tmp, 0, count, (jbyte*)(tmpNative));
        (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->netInBuffer, g_ByteBufferPut3Method, tmp, 0, count));
        free(tmpNative);
        (*env)->DeleteLocalRef(env, tmp);
    }

    (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->netInBuffer, g_ByteBufferFlipMethod));
    jobject sslEngineResult = (*env)->CallObjectMethod(env, sslStream->sslEngine, g_SSLEngineUnwrapMethod, sslStream->netInBuffer, sslStream->appInBuffer);
    (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->netInBuffer, g_ByteBufferCompactMethod));

    int status = GetEnumAsInt(env, (*env)->CallObjectMethod(env, sslEngineResult, g_SSLEngineResultGetStatusMethod));
    LOG_DEBUG("doUnwwrap status: %d", status);
    switch (status)
    {
        case STATUS__OK:
            checkHandshakeStatus(env, sslStream, getHandshakeStatus(env, sslStream, sslEngineResult));
            break;
        case STATUS__CLOSED:
            checkHandshakeStatus(env, sslStream, getHandshakeStatus(env, sslStream, sslEngineResult));
            close(env, sslStream);
            break;
        case STATUS__BUFFER_UNDERFLOW:
            sslStream->netInBuffer = ensureRemaining(env, sslStream, sslStream->netInBuffer, (*env)->CallIntMethod(env, sslStream->sslSession, g_SSLSessionGetPacketBufferSizeMethod));
            doUnwrap(env, sslStream);
            break;
        case STATUS__BUFFER_OVERFLOW:
            sslStream->appInBuffer = ensureRemaining(env, sslStream, sslStream->appInBuffer, (*env)->CallIntMethod(env, sslStream->sslSession, g_SSLSessionGetApplicationBufferSizeMethod));
            doUnwrap(env, sslStream);
            break;
    }
}

static void checkHandshakeStatus(JNIEnv* env, SSLStream* sslStream, int handshakeStatus)
{
    /*
        switch (handshakeStatus) {
            case NEED_WRAP:
                doWrap();
                break;
            case NEED_UNWRAP:
                doUnwrap();
                break;
            case NEED_TASK:
                Runnable task;
                while ((task = sslEngine.getDelegatedTask()) != null) task.run();
                checkHandshakeStatus();
                break;
        }
    */

    LOG_DEBUG("Handshake status: %d", handshakeStatus);
    AssertOnJNIExceptions(env);
    switch (handshakeStatus)
    {
        case HANDSHAKE_STATUS__NEED_WRAP:
            doWrap(env, sslStream);
            break;
        case HANDSHAKE_STATUS__NEED_UNWRAP:
            doUnwrap(env, sslStream);
            break;
        case HANDSHAKE_STATUS__NEED_TASK:
            assert(0 && "unexpected NEED_TASK handshake status");
    }
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

    // SSLContext sslContext = SSLContext.getInstance("TLSv1.2");
    // sslContext.init(null, null, null);
    // TODO: set TrustManager[] argument in init to be able to intercept cert validation process (and callback to C#).
    // jstring protocol = JSTRING("TLSv1.2");
    // jobject sslContext = (*env)->CallStaticObjectMethod(env, g_SSLContext, g_SSLContextGetInstanceMethod, protocol);
    // ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    // sslStream->sslContext = ToGRef(env, sslContext);
    // (*env)->CallVoidMethod(env, sslStream->sslContext, g_SSLContextInitMethod, NULL, NULL, NULL);
    // ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // SSLContext sslContext = SSLContext.getDefault();
    jobject sslContext = (*env)->CallStaticObjectMethod(env, g_SSLContext, g_SSLContextGetDefault);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    sslStream->sslContext = ToGRef(env, sslContext);

    // SSLEngine sslEngine = sslContext.createSSLEngine();
    // sslEngine.setUseClientMode(!isServer);
    jobject sslEngine = (*env)->CallObjectMethod(env, sslStream->sslContext, g_SSLContextCreateSSLEngineMethod);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    sslStream->sslEngine  = ToGRef(env, sslEngine);
    (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineSetUseClientModeMethod, !isServer);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

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

cleanup:
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

int32_t AndroidCryptoNative_SSLStreamHandshake(SSLStream *sslStream)
{
    assert(sslStream != NULL);
    JNIEnv* env = GetJNIEnv();

    (*env)->CallVoidMethod(env, sslStream->sslEngine, g_SSLEngineBeginHandshakeMethod);
    if (CheckJNIExceptions(env))
        return FAIL;

    checkHandshakeStatus(env, sslStream, getHandshakeStatus(env, sslStream, NULL));
    return SUCCESS;
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

    checkHandshakeStatus(env, sslStream, getHandshakeStatus(env, sslStream, NULL));
    (*env)->DeleteLocalRef(env, tlsVerStr);
    AssertOnJNIExceptions(env);
    return sslStream;
}

int AndroidCryptoNative_SSLStreamRead(SSLStream* sslStream, uint8_t* buffer, int offset, int length)
{
    JNIEnv* env = GetJNIEnv();

    /*
        while (true) {
            appInBuffer.flip();
            try {
                if (appInBuffer.remaining() > 0) {
                    byte[] data = new byte[appInBuffer.remaining()];
                    appInBuffer.get(data);
                    return data;
                }
            } finally {
                appInBuffer.compact();
            }
            doUnwrap();
        }
    */

    AssertOnJNIExceptions(env);
    (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->appInBuffer, g_ByteBufferFlipMethod));
    int rem = (*env)->CallIntMethod(env, sslStream->appInBuffer, g_ByteBufferRemainingMethod);
    if (rem > 0)
    {
        jbyteArray data = (*env)->NewByteArray(env, rem);
        (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->appInBuffer, g_ByteBufferGetMethod, data));
        (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->appInBuffer, g_ByteBufferCompactMethod));
        (*env)->GetByteArrayRegion(env, data, 0, rem, (jbyte*) buffer);
        AssertOnJNIExceptions(env);
        return rem;
    }
    else
    {
        (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->appInBuffer, g_ByteBufferCompactMethod));
        doUnwrap(env, sslStream);
        AssertOnJNIExceptions(env);
        return AndroidCryptoNative_SSLStreamRead(sslStream, buffer, offset, length);
    }
}

void AndroidCryptoNative_SSLStreamWrite(SSLStream* sslStream, uint8_t* buffer, int offset, int length)
{
    /*
        appOutBuffer.put(message);
        doWrap();
    */

    JNIEnv* env = GetJNIEnv();
    jbyteArray data = (*env)->NewByteArray(env, length);
    (*env)->SetByteArrayRegion(env, data, 0, length, (jbyte*)(buffer + offset));
    (*env)->DeleteLocalRef(env, (*env)->CallObjectMethod(env, sslStream->appOutBuffer, g_ByteBufferPut2Method, data));
    (*env)->DeleteLocalRef(env, data);
    doWrap(env, sslStream);
    AssertOnJNIExceptions(env);
}

void AndroidCryptoNative_SSLStreamRelease(SSLStream* sslStream)
{
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
