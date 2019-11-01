// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_easy.h"

#include <stdlib.h>
#include <stdbool.h>
#include <assert.h>

c_static_assert(PAL_CURLOPT_INFILESIZE == CURLOPT_INFILESIZE);
c_static_assert(PAL_CURLOPT_SSLVERSION == CURLOPT_SSLVERSION);
c_static_assert(PAL_CURLOPT_VERBOSE == CURLOPT_VERBOSE);
c_static_assert(PAL_CURLOPT_NOBODY == CURLOPT_NOBODY);
c_static_assert(PAL_CURLOPT_UPLOAD == CURLOPT_UPLOAD);
c_static_assert(PAL_CURLOPT_POST == CURLOPT_POST);
c_static_assert(PAL_CURLOPT_FOLLOWLOCATION == CURLOPT_FOLLOWLOCATION);
c_static_assert(PAL_CURLOPT_PROXYPORT == CURLOPT_PROXYPORT);
c_static_assert(PAL_CURLOPT_POSTFIELDSIZE == CURLOPT_POSTFIELDSIZE);
c_static_assert(PAL_CURLOPT_SSL_VERIFYPEER == CURLOPT_SSL_VERIFYPEER);
c_static_assert(PAL_CURLOPT_MAXREDIRS == CURLOPT_MAXREDIRS);
c_static_assert(PAL_CURLOPT_SSL_VERIFYHOST == CURLOPT_SSL_VERIFYHOST);
c_static_assert(PAL_CURLOPT_HTTP_VERSION == CURLOPT_HTTP_VERSION);
c_static_assert(PAL_CURLOPT_DNS_CACHE_TIMEOUT == CURLOPT_DNS_CACHE_TIMEOUT);
c_static_assert(PAL_CURLOPT_NOSIGNAL == CURLOPT_NOSIGNAL);
c_static_assert(PAL_CURLOPT_PROXYTYPE == CURLOPT_PROXYTYPE);
c_static_assert(PAL_CURLOPT_HTTPAUTH == CURLOPT_HTTPAUTH);
c_static_assert(PAL_CURLOPT_TCP_NODELAY == CURLOPT_TCP_NODELAY);
c_static_assert(PAL_CURLOPT_TCP_KEEPALIVE == CURLOPT_TCP_KEEPALIVE);
c_static_assert(PAL_CURLOPT_CONNECTTIMEOUT_MS == CURLOPT_CONNECTTIMEOUT_MS);
c_static_assert(PAL_CURLOPT_ADDRESS_SCOPE == CURLOPT_ADDRESS_SCOPE);
c_static_assert(PAL_CURLOPT_PROTOCOLS == CURLOPT_PROTOCOLS);
c_static_assert(PAL_CURLOPT_REDIR_PROTOCOLS == CURLOPT_REDIR_PROTOCOLS);

c_static_assert(PAL_CURLOPT_URL == CURLOPT_URL);
c_static_assert(PAL_CURLOPT_PROXY == CURLOPT_PROXY);
c_static_assert(PAL_CURLOPT_PROXYUSERPWD == CURLOPT_PROXYUSERPWD);
c_static_assert(PAL_CURLOPT_COOKIE == CURLOPT_COOKIE);
c_static_assert(PAL_CURLOPT_HTTPHEADER == CURLOPT_HTTPHEADER);
c_static_assert(PAL_CURLOPT_CUSTOMREQUEST == CURLOPT_CUSTOMREQUEST);
c_static_assert(PAL_CURLOPT_ACCEPT_ENCODING == CURLOPT_ACCEPT_ENCODING);
c_static_assert(PAL_CURLOPT_PRIVATE == CURLOPT_PRIVATE);
c_static_assert(PAL_CURLOPT_COPYPOSTFIELDS == CURLOPT_COPYPOSTFIELDS);
c_static_assert(PAL_CURLOPT_USERNAME == CURLOPT_USERNAME);
c_static_assert(PAL_CURLOPT_PASSWORD == CURLOPT_PASSWORD);
c_static_assert(PAL_CURLOPT_CAPATH == CURLOPT_CAPATH);
#ifdef CURLOPT_PROXY_CAPATH
c_static_assert(PAL_CURLOPT_PROXY_CAPATH == CURLOPT_PROXY_CAPATH);
#endif
c_static_assert(PAL_CURLOPT_CAINFO == CURLOPT_CAINFO);
#ifdef CURLOPT_PROXY_CAINFO
c_static_assert(PAL_CURLOPT_PROXY_CAINFO == CURLOPT_PROXY_CAINFO);
#endif

c_static_assert(PAL_CURLOPT_INFILESIZE_LARGE == CURLOPT_INFILESIZE_LARGE);
c_static_assert(PAL_CURLOPT_POSTFIELDSIZE_LARGE == CURLOPT_POSTFIELDSIZE_LARGE);

c_static_assert(PAL_CURLE_OK == CURLE_OK);
c_static_assert(PAL_CURLE_UNSUPPORTED_PROTOCOL == CURLE_UNSUPPORTED_PROTOCOL);
c_static_assert(PAL_CURLE_FAILED_INIT == CURLE_FAILED_INIT);
c_static_assert(PAL_CURLE_NOT_BUILT_IN == CURLE_NOT_BUILT_IN);
c_static_assert(PAL_CURLE_COULDNT_RESOLVE_HOST == CURLE_COULDNT_RESOLVE_HOST);
c_static_assert(PAL_CURLE_OUT_OF_MEMORY == CURLE_OUT_OF_MEMORY);
c_static_assert(PAL_CURLE_OPERATION_TIMEDOUT == CURLE_OPERATION_TIMEDOUT);
c_static_assert(PAL_CURLE_ABORTED_BY_CALLBACK == CURLE_ABORTED_BY_CALLBACK);
c_static_assert(PAL_CURLE_UNKNOWN_OPTION == CURLE_UNKNOWN_OPTION);
c_static_assert(PAL_CURLE_RECV_ERROR == CURLE_RECV_ERROR);
c_static_assert(PAL_CURLE_SEND_FAIL_REWIND == CURLE_SEND_FAIL_REWIND);

c_static_assert(PAL_CURL_HTTP_VERSION_NONE == CURL_HTTP_VERSION_NONE);
c_static_assert(PAL_CURL_HTTP_VERSION_1_0 == CURL_HTTP_VERSION_1_0);
c_static_assert(PAL_CURL_HTTP_VERSION_1_1 == CURL_HTTP_VERSION_1_1);
#if HAVE_CURL_HTTP_VERSION_2TLS
c_static_assert(PAL_CURL_HTTP_VERSION_2TLS == CURL_HTTP_VERSION_2TLS);
#endif

c_static_assert(PAL_CURL_SSLVERSION_SSLv2 == CURL_SSLVERSION_SSLv2);
c_static_assert(PAL_CURL_SSLVERSION_SSLv3 == CURL_SSLVERSION_SSLv3);
c_static_assert(PAL_CURL_SSLVERSION_TLSv1 == CURL_SSLVERSION_TLSv1);
#if HAVE_CURL_SSLVERSION_TLSv1_012
c_static_assert(PAL_CURL_SSLVERSION_TLSv1_0 == CURL_SSLVERSION_TLSv1_0);
c_static_assert(PAL_CURL_SSLVERSION_TLSv1_1 == CURL_SSLVERSION_TLSv1_1);
c_static_assert(PAL_CURL_SSLVERSION_TLSv1_2 == CURL_SSLVERSION_TLSv1_2);
#endif

c_static_assert(PAL_CURLINFO_EFFECTIVE_URL == CURLINFO_EFFECTIVE_URL);
c_static_assert(PAL_CURLINFO_PRIVATE == CURLINFO_PRIVATE);
c_static_assert(PAL_CURLINFO_HTTPAUTH_AVAIL == CURLINFO_HTTPAUTH_AVAIL);

c_static_assert(PAL_CURLAUTH_None == CURLAUTH_NONE);
c_static_assert(PAL_CURLAUTH_Basic == CURLAUTH_BASIC);
c_static_assert(PAL_CURLAUTH_Digest == CURLAUTH_DIGEST);
c_static_assert(PAL_CURLAUTH_Negotiate == CURLAUTH_GSSNEGOTIATE);
c_static_assert(PAL_CURLAUTH_NTLM == CURLAUTH_NTLM);

c_static_assert(PAL_CURLPROXY_HTTP == CURLPROXY_HTTP);

c_static_assert(PAL_CURLPROTO_HTTP == CURLPROTO_HTTP);
c_static_assert(PAL_CURLPROTO_HTTPS == CURLPROTO_HTTPS);

c_static_assert(PAL_CURL_SEEKFUNC_OK == CURL_SEEKFUNC_OK);
c_static_assert(PAL_CURL_SEEKFUNC_FAIL == CURL_SEEKFUNC_FAIL);
c_static_assert(PAL_CURL_SEEKFUNC_CANTSEEK == CURL_SEEKFUNC_CANTSEEK);

c_static_assert(PAL_CURL_READFUNC_ABORT == CURL_READFUNC_ABORT);
c_static_assert(PAL_CURL_READFUNC_PAUSE == CURL_READFUNC_PAUSE);
c_static_assert(PAL_CURL_WRITEFUNC_PAUSE == CURL_WRITEFUNC_PAUSE);

c_static_assert(PAL_CURLINFO_TEXT == CURLINFO_TEXT);
c_static_assert(PAL_CURLINFO_HEADER_IN == CURLINFO_HEADER_IN);
c_static_assert(PAL_CURLINFO_HEADER_OUT == CURLINFO_HEADER_OUT);
c_static_assert(PAL_CURLINFO_DATA_IN == CURLINFO_DATA_IN);
c_static_assert(PAL_CURLINFO_DATA_OUT == CURLINFO_DATA_OUT);
c_static_assert(PAL_CURLINFO_SSL_DATA_IN == CURLINFO_SSL_DATA_IN);
c_static_assert(PAL_CURLINFO_SSL_DATA_OUT == CURLINFO_SSL_DATA_OUT);

c_static_assert(PAL_CURL_MAX_HTTP_HEADER == CURL_MAX_HTTP_HEADER);

CURL* HttpNative_EasyCreate()
{
    return curl_easy_init();
}

void HttpNative_EasyDestroy(CURL* handle)
{
    curl_easy_cleanup(handle);
}

inline static CURLoption ConvertOption(PAL_CURLoption option)
{
    return (CURLoption)option;
}

int32_t HttpNative_EasySetOptionString(CURL* handle, PAL_CURLoption option, const char* value)
{
    return (int32_t)(curl_easy_setopt(handle, ConvertOption(option), value));
}

int32_t HttpNative_EasySetOptionLong(CURL* handle, PAL_CURLoption option, int64_t value)
{
    CURLoption curlOpt = ConvertOption(option);

    // The HttpNative_EasySetOptionLong entrypoint is used for both curl_easy_setopt(..., long) and
    // curl_easy_setopt(..., curl_off_t).  As they'll likely be different sizes on 32-bit platforms, 
    // we map anything >= CurlOptionOffTBase to use curl_off_t.
    if (option >= CurlOptionOffTBase)
    {
        return (int32_t)(curl_easy_setopt(handle, curlOpt, (curl_off_t)value));
    }
    else
    {
        return (int32_t)(curl_easy_setopt(handle, curlOpt, (long)value));
    }
}

int32_t HttpNative_EasySetOptionPointer(CURL* handle, PAL_CURLoption option, void* value)
{
    return (int32_t)(curl_easy_setopt(handle, ConvertOption(option), value));
}

const char* HttpNative_EasyGetErrorString(PAL_CURLcode code)
{
    return curl_easy_strerror((CURLcode)code);
}

inline static CURLINFO ConvertInfo(PAL_CURLINFO info)
{
    return (CURLINFO)info;
}

int32_t HttpNative_EasyGetInfoPointer(CURL* handle, PAL_CURLINFO info, void** value)
{
    return (int32_t)(curl_easy_getinfo(handle, ConvertInfo(info), value));
}

int32_t HttpNative_EasyGetInfoLong(CURL* handle, PAL_CURLINFO info, int64_t* value)
{
    return (int32_t)(curl_easy_getinfo(handle, ConvertInfo(info), value));
}

int32_t HttpNative_EasyPerform(CURL* handle)
{
    return (int32_t)(curl_easy_perform(handle));
}

int32_t HttpNative_EasyUnpause(CURL* handle)
{
    return (int32_t)(curl_easy_pause(handle, CURLPAUSE_CONT));
}

struct CallbackHandle
{
    SeekCallback seekCallback;
    void* seekUserPointer;

    ReadWriteCallback writeCallback;
    void* writeUserPointer;

    ReadWriteCallback readCallback;
    void* readUserPointer;

    ReadWriteCallback headerCallback;
    void* headerUserPointer;

    SslCtxCallback sslCtxCallback;
    void* sslUserPointer;

    DebugCallback debugCallback;
    void* debugUserPointer;
};

static inline bool EnsureCallbackHandle(CallbackHandle** callbackHandle)
{
    assert(callbackHandle != NULL);

    if (*callbackHandle == NULL)
    {
        *callbackHandle = (CallbackHandle*)malloc(sizeof(CallbackHandle));
    }

    return *callbackHandle != NULL;
}

static int seek_callback(void* userp, curl_off_t offset, int origin)
{
    CallbackHandle* handle = (CallbackHandle*)userp;
    return handle->seekCallback(handle->seekUserPointer, offset, origin);
}

void
HttpNative_RegisterSeekCallback(CURL* curl, SeekCallback callback, void* userPointer, CallbackHandle** callbackHandle)
{
    if (EnsureCallbackHandle(callbackHandle))
    {
        CallbackHandle* handle = *callbackHandle;
        handle->seekCallback = callback;
        handle->seekUserPointer = userPointer;

        curl_easy_setopt(curl, CURLOPT_SEEKDATA, handle);
        curl_easy_setopt(curl, CURLOPT_SEEKFUNCTION, &seek_callback);
    }
}

static size_t write_callback(char* buffer, size_t size, size_t nitems, void* instream)
{
    CallbackHandle* handle = (CallbackHandle*)instream;
    return (size_t)(handle->writeCallback((uint8_t*)buffer, size, nitems, handle->writeUserPointer));
}

static size_t read_callback(char* buffer, size_t size, size_t nitems, void* instream)
{
    CallbackHandle* handle = (CallbackHandle*)instream;
    return (size_t)(handle->readCallback((uint8_t*)buffer, size, nitems, handle->readUserPointer));
}

static size_t header_callback(char* buffer, size_t size, size_t nitems, void* instream)
{
    CallbackHandle* handle = (CallbackHandle*)instream;
    return (size_t)(handle->headerCallback((uint8_t*)buffer, size, nitems, handle->headerUserPointer));
}

void HttpNative_RegisterReadWriteCallback(CURL* curl,
                                                     ReadWriteFunction functionType,
                                                     ReadWriteCallback callback,
                                                     void* userPointer,
                                                     CallbackHandle** callbackHandle)
{
    if (EnsureCallbackHandle(callbackHandle))
    {
        CallbackHandle* handle = *callbackHandle;

        switch (functionType)
        {
        case Write:
            handle->writeCallback = callback;
            handle->writeUserPointer = userPointer;
            curl_easy_setopt(curl, CURLOPT_WRITEDATA, handle);
            curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, &write_callback);
            break;

        case Read:
            handle->readCallback = callback;
            handle->readUserPointer = userPointer;
            curl_easy_setopt(curl, CURLOPT_READDATA, handle);
            curl_easy_setopt(curl, CURLOPT_READFUNCTION, &read_callback);
            break;

        case Header:
            handle->headerCallback = callback;
            handle->headerUserPointer = userPointer;
            curl_easy_setopt(curl, CURLOPT_HEADERDATA, handle);
            curl_easy_setopt(curl, CURLOPT_HEADERFUNCTION, &header_callback);
            break;
        }
    }
}

static CURLcode ssl_ctx_callback(CURL* curl, void* sslCtx, void* userPointer)
{
    CallbackHandle* handle = (CallbackHandle*)userPointer;

    int32_t result = handle->sslCtxCallback(curl, sslCtx, handle->sslUserPointer);
    return (CURLcode)result;
}

int32_t HttpNative_RegisterSslCtxCallback(CURL* curl,
                                                     SslCtxCallback callback,
                                                     void* userPointer,
                                                     CallbackHandle** callbackHandle)
{
    if (!EnsureCallbackHandle(callbackHandle))
    {
        return CURLE_OUT_OF_MEMORY;
    }

    CallbackHandle* handle = *callbackHandle;
    handle->sslCtxCallback = callback;
    handle->sslUserPointer = userPointer;

    curl_easy_setopt(curl, CURLOPT_SSL_CTX_DATA, handle);
    return (int32_t)(curl_easy_setopt(curl, CURLOPT_SSL_CTX_FUNCTION, &ssl_ctx_callback));
}

static int debug_callback(CURL* curl, curl_infotype type, char* data, size_t size, void* userPointer)
{
    assert(userPointer != NULL);
    CallbackHandle* handle = (CallbackHandle*)userPointer;
    handle->debugCallback(curl, (PAL_CurlInfoType)type, data, size, handle->debugUserPointer);
    return 0;
}

int32_t HttpNative_RegisterDebugCallback(CURL* curl, 
                                                    DebugCallback callback, 
                                                    void* userPointer, 
                                                    CallbackHandle** callbackHandle)
{
    if (!EnsureCallbackHandle(callbackHandle))
    {
        return CURLE_OUT_OF_MEMORY;
    }

    CallbackHandle* handle = *callbackHandle;
    handle->debugCallback = callback;
    handle->debugUserPointer = userPointer;

    CURLcode rv = curl_easy_setopt(curl, CURLOPT_DEBUGDATA, handle);
    return rv == CURLE_OK ?
        (int32_t)(curl_easy_setopt(curl, CURLOPT_DEBUGFUNCTION, &debug_callback)) :
        (int32_t)rv;
}

void HttpNative_FreeCallbackHandle(CallbackHandle* callbackHandle)
{
    assert(callbackHandle != NULL);
    if (callbackHandle != NULL)
    {
        free(callbackHandle);
    }
}
