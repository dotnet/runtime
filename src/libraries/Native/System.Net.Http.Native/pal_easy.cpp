// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_easy.h"

#include <assert.h>

static_assert(PAL_CURLOPT_INFILESIZE == CURLOPT_INFILESIZE, "");
static_assert(PAL_CURLOPT_VERBOSE == CURLOPT_VERBOSE, "");
static_assert(PAL_CURLOPT_NOBODY == CURLOPT_NOBODY, "");
static_assert(PAL_CURLOPT_UPLOAD == CURLOPT_UPLOAD, "");
static_assert(PAL_CURLOPT_POST == CURLOPT_POST, "");
static_assert(PAL_CURLOPT_FOLLOWLOCATION == CURLOPT_FOLLOWLOCATION, "");
static_assert(PAL_CURLOPT_PROXYPORT == CURLOPT_PROXYPORT, "");
static_assert(PAL_CURLOPT_POSTFIELDSIZE == CURLOPT_POSTFIELDSIZE, "");
static_assert(PAL_CURLOPT_MAXREDIRS == CURLOPT_MAXREDIRS, "");
static_assert(PAL_CURLOPT_NOSIGNAL == CURLOPT_NOSIGNAL, "");
static_assert(PAL_CURLOPT_PROXYTYPE == CURLOPT_PROXYTYPE, "");
static_assert(PAL_CURLOPT_HTTPAUTH == CURLOPT_HTTPAUTH, "");
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

static_assert(PAL_CURLE_OK == CURLE_OK, "");
static_assert(PAL_CURLE_UNSUPPORTED_PROTOCOL == CURLE_UNSUPPORTED_PROTOCOL, "");
static_assert(PAL_CURLE_NOT_BUILT_IN == CURLE_NOT_BUILT_IN, "");
static_assert(PAL_CURLE_COULDNT_RESOLVE_HOST == CURLE_COULDNT_RESOLVE_HOST, "");

static_assert(PAL_CURLINFO_PRIVATE == CURLINFO_PRIVATE, "");
static_assert(PAL_CURLINFO_HTTPAUTH_AVAIL == CURLINFO_HTTPAUTH_AVAIL, "");

static_assert(PAL_CURLAUTH_None == CURLAUTH_NONE, "");
static_assert(PAL_CURLAUTH_Basic == CURLAUTH_BASIC, "");
static_assert(PAL_CURLAUTH_Digest == CURLAUTH_DIGEST, "");
static_assert(PAL_CURLAUTH_Negotiate == CURLAUTH_GSSNEGOTIATE, "");

static_assert(PAL_CURLPROXY_HTTP == CURLPROXY_HTTP, "");

static_assert(PAL_CURLPROTO_HTTP == CURLPROTO_HTTP, "");
static_assert(PAL_CURLPROTO_HTTPS == CURLPROTO_HTTPS, "");

static_assert(PAL_CURL_SEEKFUNC_OK == CURL_SEEKFUNC_OK, "");
static_assert(PAL_CURL_SEEKFUNC_FAIL == CURL_SEEKFUNC_FAIL, "");
static_assert(PAL_CURL_SEEKFUNC_CANTSEEK == CURL_SEEKFUNC_CANTSEEK, "");

static_assert(PAL_CURL_READFUNC_ABORT == CURL_READFUNC_ABORT, "");
static_assert(PAL_CURL_READFUNC_PAUSE == CURL_READFUNC_PAUSE, "");
static_assert(PAL_CURL_WRITEFUNC_PAUSE == CURL_WRITEFUNC_PAUSE, "");

extern "C" CURL* EasyCreate()
{
    return curl_easy_init();
}

extern "C" void EasyDestroy(CURL* handle)
{
    curl_easy_cleanup(handle);
}

inline static CURLoption ConvertOption(PAL_CURLoption option)
{
    return static_cast<CURLoption>(option);
}

extern "C" int32_t EasySetOptionString(CURL* handle, PAL_CURLoption option, const char* value)
{
    return curl_easy_setopt(handle, ConvertOption(option), value);
}

extern "C" int32_t EasySetOptionLong(CURL* handle, PAL_CURLoption option, int64_t value)
{
    return curl_easy_setopt(handle, ConvertOption(option), value);
}

extern "C" int32_t EasySetOptionPointer(CURL* handle, PAL_CURLoption option, void* value)
{
    return curl_easy_setopt(handle, ConvertOption(option), value);
}

extern "C" const char* EasyGetErrorString(PAL_CURLcode code)
{
    return curl_easy_strerror(static_cast<CURLcode>(code));
}

inline static CURLINFO ConvertInfo(PAL_CURLINFO info)
{
    return static_cast<CURLINFO>(info);
}

extern "C" int32_t EasyGetInfoPointer(CURL* handle, PAL_CURLINFO info, void** value)
{
    return curl_easy_getinfo(handle, ConvertInfo(info), value);
}

extern "C" int32_t EasyGetInfoLong(CURL* handle, PAL_CURLINFO info, int64_t* value)
{
    return curl_easy_getinfo(handle, ConvertInfo(info), value);
}

extern "C" int32_t EasyPerform(CURL* handle)
{
    return curl_easy_perform(handle);
}

extern "C" int32_t EasyUnpause(CURL* handle)
{
    return curl_easy_pause(handle, CURLPAUSE_CONT);
}

static int seek_callback(void* userp, curl_off_t offset, int origin)
{
    CallbackHandle* callbackHandle = static_cast<CallbackHandle*>(userp);
    SeekCallback callback = reinterpret_cast<SeekCallback>(callbackHandle->callback);

    return callback(callbackHandle->userPointer, offset, origin);
}

extern "C" CallbackHandle* RegisterSeekCallback(CURL* handle, SeekCallback callback, void* userPointer)
{
    CallbackHandle* callbackHandle = new CallbackHandle();
    callbackHandle->callback = reinterpret_cast<FunctionPointer>(callback);
    callbackHandle->userPointer = userPointer;

    curl_easy_setopt(handle, CURLOPT_SEEKDATA, callbackHandle);
    curl_easy_setopt(handle, CURLOPT_SEEKFUNCTION, &seek_callback);

    return callbackHandle;
}

static size_t readwrite_callback(char* buffer, size_t size, size_t nitems, void* instream)
{
    CallbackHandle* callbackHandle = static_cast<CallbackHandle*>(instream);
    ReadWriteCallback callback = reinterpret_cast<ReadWriteCallback>(callbackHandle->callback);

    return callback(reinterpret_cast<uint8_t*>(buffer), size, nitems, callbackHandle->userPointer);
}

extern "C" CallbackHandle* RegisterReadWriteCallback(CURL* handle, ReadWriteFunction functionType, ReadWriteCallback callback, void* userPointer)
{
    CallbackHandle* callbackHandle = new CallbackHandle();
    callbackHandle->callback = reinterpret_cast<FunctionPointer>(callback);
    callbackHandle->userPointer = userPointer;

    switch (functionType)
    {
        case ReadWriteFunction::Write:
            curl_easy_setopt(handle, CURLOPT_WRITEDATA, callbackHandle);
            curl_easy_setopt(handle, CURLOPT_WRITEFUNCTION, &readwrite_callback);
            break;

        case ReadWriteFunction::Read:
            curl_easy_setopt(handle, CURLOPT_READDATA, callbackHandle);
            curl_easy_setopt(handle, CURLOPT_READFUNCTION, &readwrite_callback);
            break;

        case ReadWriteFunction::Header:
            curl_easy_setopt(handle, CURLOPT_HEADERDATA, callbackHandle);
            curl_easy_setopt(handle, CURLOPT_HEADERFUNCTION, &readwrite_callback);
            break;
    }

    return callbackHandle;
}

static CURLcode ssl_ctx_callback(CURL* curl, void* sslCtx, void* userPointer)
{
    CallbackHandle* callbackHandle = static_cast<CallbackHandle*>(userPointer);
    SslCtxCallback callback = reinterpret_cast<SslCtxCallback>(callbackHandle->callback);

    int32_t result = callback(curl, sslCtx);

    return static_cast<CURLcode>(result);
}

extern "C" CallbackHandle* RegisterSslCtxCallback(CURL* handle, SslCtxCallback callback, int32_t* result)
{
    assert(result != nullptr);

    CallbackHandle* callbackHandle = new CallbackHandle();
    callbackHandle->callback = reinterpret_cast<FunctionPointer>(callback);

    curl_easy_setopt(handle, CURLOPT_SSL_CTX_DATA, callbackHandle);
    *result = curl_easy_setopt(handle, CURLOPT_SSL_CTX_FUNCTION, &ssl_ctx_callback);

    return callbackHandle;
}

extern "C" void FreeCallbackHandle(CallbackHandle* callbackHandle)
{
    delete callbackHandle;
}
