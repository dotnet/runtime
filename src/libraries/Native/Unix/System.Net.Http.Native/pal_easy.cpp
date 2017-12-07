// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_easy.h"

#include <assert.h>
#include <memory>

static_assert(PAL_CURLOPT_INFILESIZE == CURLOPT_INFILESIZE, "");
static_assert(PAL_CURLOPT_SSLVERSION == CURLOPT_SSLVERSION, "");
static_assert(PAL_CURLOPT_VERBOSE == CURLOPT_VERBOSE, "");
static_assert(PAL_CURLOPT_NOBODY == CURLOPT_NOBODY, "");
static_assert(PAL_CURLOPT_UPLOAD == CURLOPT_UPLOAD, "");
static_assert(PAL_CURLOPT_POST == CURLOPT_POST, "");
static_assert(PAL_CURLOPT_FOLLOWLOCATION == CURLOPT_FOLLOWLOCATION, "");
static_assert(PAL_CURLOPT_PROXYPORT == CURLOPT_PROXYPORT, "");
static_assert(PAL_CURLOPT_POSTFIELDSIZE == CURLOPT_POSTFIELDSIZE, "");
static_assert(PAL_CURLOPT_SSL_VERIFYPEER == CURLOPT_SSL_VERIFYPEER, "");
static_assert(PAL_CURLOPT_MAXREDIRS == CURLOPT_MAXREDIRS, "");
static_assert(PAL_CURLOPT_SSL_VERIFYHOST == CURLOPT_SSL_VERIFYHOST, "");
static_assert(PAL_CURLOPT_HTTP_VERSION == CURLOPT_HTTP_VERSION, "");
static_assert(PAL_CURLOPT_DNS_CACHE_TIMEOUT == CURLOPT_DNS_CACHE_TIMEOUT, "");
static_assert(PAL_CURLOPT_NOSIGNAL == CURLOPT_NOSIGNAL, "");
static_assert(PAL_CURLOPT_PROXYTYPE == CURLOPT_PROXYTYPE, "");
static_assert(PAL_CURLOPT_HTTPAUTH == CURLOPT_HTTPAUTH, "");
static_assert(PAL_CURLOPT_TCP_NODELAY == CURLOPT_TCP_NODELAY, "");
static_assert(PAL_CURLOPT_CONNECTTIMEOUT_MS == CURLOPT_CONNECTTIMEOUT_MS, "");
static_assert(PAL_CURLOPT_ADDRESS_SCOPE == CURLOPT_ADDRESS_SCOPE, "");
static_assert(PAL_CURLOPT_PROTOCOLS == CURLOPT_PROTOCOLS, "");
static_assert(PAL_CURLOPT_REDIR_PROTOCOLS == CURLOPT_REDIR_PROTOCOLS, "");

static_assert(PAL_CURLOPT_URL == CURLOPT_URL, "");
static_assert(PAL_CURLOPT_PROXY == CURLOPT_PROXY, "");
static_assert(PAL_CURLOPT_PROXYUSERPWD == CURLOPT_PROXYUSERPWD, "");
static_assert(PAL_CURLOPT_COOKIE == CURLOPT_COOKIE, "");
static_assert(PAL_CURLOPT_HTTPHEADER == CURLOPT_HTTPHEADER, "");
static_assert(PAL_CURLOPT_CUSTOMREQUEST == CURLOPT_CUSTOMREQUEST, "");
static_assert(PAL_CURLOPT_ACCEPT_ENCODING == CURLOPT_ACCEPT_ENCODING, "");
static_assert(PAL_CURLOPT_PRIVATE == CURLOPT_PRIVATE, "");
static_assert(PAL_CURLOPT_COPYPOSTFIELDS == CURLOPT_COPYPOSTFIELDS, "");
static_assert(PAL_CURLOPT_USERNAME == CURLOPT_USERNAME, "");
static_assert(PAL_CURLOPT_PASSWORD == CURLOPT_PASSWORD, "");
static_assert(PAL_CURLOPT_CAPATH == CURLOPT_CAPATH, "");
#ifdef CURLOPT_PROXY_CAPATH
static_assert(PAL_CURLOPT_PROXY_CAPATH == CURLOPT_PROXY_CAPATH, "");
#endif
static_assert(PAL_CURLOPT_CAINFO == CURLOPT_CAINFO, "");
#ifdef CURLOPT_PROXY_CAINFO
static_assert(PAL_CURLOPT_PROXY_CAINFO == CURLOPT_PROXY_CAINFO, "");
#endif

static_assert(PAL_CURLOPT_INFILESIZE_LARGE == CURLOPT_INFILESIZE_LARGE, "");
static_assert(PAL_CURLOPT_POSTFIELDSIZE_LARGE == CURLOPT_POSTFIELDSIZE_LARGE, "");

static_assert(PAL_CURLE_OK == CURLE_OK, "");
static_assert(PAL_CURLE_UNSUPPORTED_PROTOCOL == CURLE_UNSUPPORTED_PROTOCOL, "");
static_assert(PAL_CURLE_FAILED_INIT == CURLE_FAILED_INIT, "");
static_assert(PAL_CURLE_NOT_BUILT_IN == CURLE_NOT_BUILT_IN, "");
static_assert(PAL_CURLE_COULDNT_RESOLVE_HOST == CURLE_COULDNT_RESOLVE_HOST, "");
static_assert(PAL_CURLE_OUT_OF_MEMORY == CURLE_OUT_OF_MEMORY, "");
static_assert(PAL_CURLE_OPERATION_TIMEDOUT == CURLE_OPERATION_TIMEDOUT, "");
static_assert(PAL_CURLE_ABORTED_BY_CALLBACK == CURLE_ABORTED_BY_CALLBACK, "");
static_assert(PAL_CURLE_UNKNOWN_OPTION == CURLE_UNKNOWN_OPTION, "");
static_assert(PAL_CURLE_RECV_ERROR == CURLE_RECV_ERROR, "");
static_assert(PAL_CURLE_SEND_FAIL_REWIND == CURLE_SEND_FAIL_REWIND, "");

static_assert(PAL_CURL_HTTP_VERSION_NONE == CURL_HTTP_VERSION_NONE, "");
static_assert(PAL_CURL_HTTP_VERSION_1_0 == CURL_HTTP_VERSION_1_0, "");
static_assert(PAL_CURL_HTTP_VERSION_1_1 == CURL_HTTP_VERSION_1_1, "");
#if HAVE_CURL_HTTP_VERSION_2_0
static_assert(PAL_CURL_HTTP_VERSION_2_0 == CURL_HTTP_VERSION_2_0, "");
#endif

static_assert(PAL_CURL_SSLVERSION_TLSv1 == CURL_SSLVERSION_TLSv1, "");
#if HAVE_CURL_SSLVERSION_TLSv1_012
static_assert(PAL_CURL_SSLVERSION_TLSv1_0 == CURL_SSLVERSION_TLSv1_0, "");
static_assert(PAL_CURL_SSLVERSION_TLSv1_1 == CURL_SSLVERSION_TLSv1_1, "");
static_assert(PAL_CURL_SSLVERSION_TLSv1_2 == CURL_SSLVERSION_TLSv1_2, "");
#endif

static_assert(PAL_CURLINFO_EFFECTIVE_URL == CURLINFO_EFFECTIVE_URL, "");
static_assert(PAL_CURLINFO_PRIVATE == CURLINFO_PRIVATE, "");
static_assert(PAL_CURLINFO_HTTPAUTH_AVAIL == CURLINFO_HTTPAUTH_AVAIL, "");

static_assert(PAL_CURLAUTH_None == CURLAUTH_NONE, "");
static_assert(PAL_CURLAUTH_Basic == CURLAUTH_BASIC, "");
static_assert(PAL_CURLAUTH_Digest == CURLAUTH_DIGEST, "");
static_assert(PAL_CURLAUTH_Negotiate == CURLAUTH_GSSNEGOTIATE, "");
static_assert(PAL_CURLAUTH_NTLM == CURLAUTH_NTLM, "");

static_assert(PAL_CURLPROXY_HTTP == CURLPROXY_HTTP, "");

static_assert(PAL_CURLPROTO_HTTP == CURLPROTO_HTTP, "");
static_assert(PAL_CURLPROTO_HTTPS == CURLPROTO_HTTPS, "");

static_assert(PAL_CURL_SEEKFUNC_OK == CURL_SEEKFUNC_OK, "");
static_assert(PAL_CURL_SEEKFUNC_FAIL == CURL_SEEKFUNC_FAIL, "");
static_assert(PAL_CURL_SEEKFUNC_CANTSEEK == CURL_SEEKFUNC_CANTSEEK, "");

static_assert(PAL_CURL_READFUNC_ABORT == CURL_READFUNC_ABORT, "");
static_assert(PAL_CURL_READFUNC_PAUSE == CURL_READFUNC_PAUSE, "");
static_assert(PAL_CURL_WRITEFUNC_PAUSE == CURL_WRITEFUNC_PAUSE, "");

static_assert(PAL_CURLINFO_TEXT == CURLINFO_TEXT, "");
static_assert(PAL_CURLINFO_HEADER_IN == CURLINFO_HEADER_IN, "");
static_assert(PAL_CURLINFO_HEADER_OUT == CURLINFO_HEADER_OUT, "");
static_assert(PAL_CURLINFO_DATA_IN == CURLINFO_DATA_IN, "");
static_assert(PAL_CURLINFO_DATA_OUT == CURLINFO_DATA_OUT, "");
static_assert(PAL_CURLINFO_SSL_DATA_IN == CURLINFO_SSL_DATA_IN, "");
static_assert(PAL_CURLINFO_SSL_DATA_OUT == CURLINFO_SSL_DATA_OUT, "");

static_assert(PAL_CURL_MAX_HTTP_HEADER == CURL_MAX_HTTP_HEADER, "");

extern "C" CURL* HttpNative_EasyCreate()
{
    return curl_easy_init();
}

extern "C" void HttpNative_EasyDestroy(CURL* handle)
{
    curl_easy_cleanup(handle);
}

inline static CURLoption ConvertOption(PAL_CURLoption option)
{
    return static_cast<CURLoption>(option);
}

extern "C" int32_t HttpNative_EasySetOptionString(CURL* handle, PAL_CURLoption option, const char* value)
{
    return curl_easy_setopt(handle, ConvertOption(option), value);
}

extern "C" int32_t HttpNative_EasySetOptionLong(CURL* handle, PAL_CURLoption option, int64_t value)
{
    CURLoption curlOpt = ConvertOption(option);

    // The HttpNative_EasySetOptionLong entrypoint is used for both curl_easy_setopt(..., long) and
    // curl_easy_setopt(..., curl_off_t).  As they'll likely be different sizes on 32-bit platforms, 
    // we map anything >= CurlOptionOffTBase to use curl_off_t.
    if (option >= CurlOptionOffTBase)
    {
        return curl_easy_setopt(handle, curlOpt, static_cast<curl_off_t>(value));
    }
    else
    {
        return curl_easy_setopt(handle, curlOpt, static_cast<long>(value));
    }
}

extern "C" int32_t HttpNative_EasySetOptionPointer(CURL* handle, PAL_CURLoption option, void* value)
{
    return curl_easy_setopt(handle, ConvertOption(option), value);
}

extern "C" const char* HttpNative_EasyGetErrorString(PAL_CURLcode code)
{
    return curl_easy_strerror(static_cast<CURLcode>(code));
}

inline static CURLINFO ConvertInfo(PAL_CURLINFO info)
{
    return static_cast<CURLINFO>(info);
}

extern "C" int32_t HttpNative_EasyGetInfoPointer(CURL* handle, PAL_CURLINFO info, void** value)
{
    return curl_easy_getinfo(handle, ConvertInfo(info), value);
}

extern "C" int32_t HttpNative_EasyGetInfoLong(CURL* handle, PAL_CURLINFO info, int64_t* value)
{
    return curl_easy_getinfo(handle, ConvertInfo(info), value);
}

extern "C" int32_t HttpNative_EasyPerform(CURL* handle)
{
    return curl_easy_perform(handle);
}

extern "C" int32_t HttpNative_EasyUnpause(CURL* handle)
{
    return curl_easy_pause(handle, CURLPAUSE_CONT);
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
    assert(callbackHandle != nullptr);

    if (*callbackHandle == nullptr)
    {
        *callbackHandle = new (std::nothrow) CallbackHandle();
    }

    return *callbackHandle != nullptr;
}

static int seek_callback(void* userp, curl_off_t offset, int origin)
{
    CallbackHandle* handle = static_cast<CallbackHandle*>(userp);
    return handle->seekCallback(handle->seekUserPointer, offset, origin);
}

extern "C" void
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
    CallbackHandle* handle = static_cast<CallbackHandle*>(instream);
    return static_cast<size_t>(
        handle->writeCallback(reinterpret_cast<uint8_t*>(buffer), size, nitems, handle->writeUserPointer));
}

static size_t read_callback(char* buffer, size_t size, size_t nitems, void* instream)
{
    CallbackHandle* handle = static_cast<CallbackHandle*>(instream);
    return static_cast<size_t>(
        handle->readCallback(reinterpret_cast<uint8_t*>(buffer), size, nitems, handle->readUserPointer));
}

static size_t header_callback(char* buffer, size_t size, size_t nitems, void* instream)
{
    CallbackHandle* handle = static_cast<CallbackHandle*>(instream);
    return static_cast<size_t>(
        handle->headerCallback(reinterpret_cast<uint8_t*>(buffer), size, nitems, handle->headerUserPointer));
}

extern "C" void HttpNative_RegisterReadWriteCallback(CURL* curl,
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
        case ReadWriteFunction::Write:
            handle->writeCallback = callback;
            handle->writeUserPointer = userPointer;
            curl_easy_setopt(curl, CURLOPT_WRITEDATA, handle);
            curl_easy_setopt(curl, CURLOPT_WRITEFUNCTION, &write_callback);
            break;

        case ReadWriteFunction::Read:
            handle->readCallback = callback;
            handle->readUserPointer = userPointer;
            curl_easy_setopt(curl, CURLOPT_READDATA, handle);
            curl_easy_setopt(curl, CURLOPT_READFUNCTION, &read_callback);
            break;

        case ReadWriteFunction::Header:
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
    CallbackHandle* handle = static_cast<CallbackHandle*>(userPointer);

    int32_t result = handle->sslCtxCallback(curl, sslCtx, handle->sslUserPointer);
    return static_cast<CURLcode>(result);
}

extern "C" int32_t HttpNative_RegisterSslCtxCallback(CURL* curl,
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
    return curl_easy_setopt(curl, CURLOPT_SSL_CTX_FUNCTION, &ssl_ctx_callback);
}

static int debug_callback(CURL* curl, curl_infotype type, char* data, size_t size, void* userPointer)
{
    assert(userPointer != nullptr);
    CallbackHandle* handle = static_cast<CallbackHandle*>(userPointer);
    handle->debugCallback(curl, static_cast<PAL_CurlInfoType>(type), data, size, handle->debugUserPointer);
    return 0;
}

extern "C" int32_t HttpNative_RegisterDebugCallback(CURL* curl, 
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
        curl_easy_setopt(curl, CURLOPT_DEBUGFUNCTION, &debug_callback) : 
        rv;
}

extern "C" void HttpNative_FreeCallbackHandle(CallbackHandle* callbackHandle)
{
    assert(callbackHandle != nullptr);
    if (callbackHandle != nullptr)
    {
        delete callbackHandle;
    }
}
