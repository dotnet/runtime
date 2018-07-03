// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_types.h"
#include "pal_compiler.h"

#include <curl/curl.h>

enum
{
    CurlOptionLongBase = 0,
    CurlOptionObjectPointBase = 10000,
    CurlOptionOffTBase = 30000,
};

typedef enum
{
    PAL_CURLOPT_INFILESIZE = CurlOptionLongBase + 14,
    PAL_CURLOPT_SSLVERSION = CurlOptionLongBase + 32,
    PAL_CURLOPT_VERBOSE = CurlOptionLongBase + 41,
    PAL_CURLOPT_NOBODY = CurlOptionLongBase + 44,
    PAL_CURLOPT_UPLOAD = CurlOptionLongBase + 46,
    PAL_CURLOPT_POST = CurlOptionLongBase + 47,
    PAL_CURLOPT_FOLLOWLOCATION = CurlOptionLongBase + 52,
    PAL_CURLOPT_PROXYPORT = CurlOptionLongBase + 59,
    PAL_CURLOPT_POSTFIELDSIZE = CurlOptionLongBase + 60,
    PAL_CURLOPT_SSL_VERIFYPEER = CurlOptionLongBase + 64,
    PAL_CURLOPT_MAXREDIRS = CurlOptionLongBase + 68,
    PAL_CURLOPT_SSL_VERIFYHOST = CurlOptionLongBase + 81,
    PAL_CURLOPT_HTTP_VERSION = CurlOptionLongBase + 84,
    PAL_CURLOPT_DNS_CACHE_TIMEOUT = CurlOptionLongBase + 92,
    PAL_CURLOPT_NOSIGNAL = CurlOptionLongBase + 99,
    PAL_CURLOPT_PROXYTYPE = CurlOptionLongBase + 101,
    PAL_CURLOPT_HTTPAUTH = CurlOptionLongBase + 107,
    PAL_CURLOPT_TCP_NODELAY = CurlOptionLongBase + 121,
    PAL_CURLOPT_TCP_KEEPALIVE = CurlOptionLongBase + 213,
    PAL_CURLOPT_CONNECTTIMEOUT_MS = CurlOptionLongBase + 156,
    PAL_CURLOPT_ADDRESS_SCOPE = CurlOptionLongBase + 171,
    PAL_CURLOPT_PROTOCOLS = CurlOptionLongBase + 181,
    PAL_CURLOPT_REDIR_PROTOCOLS = CurlOptionLongBase + 182,

    PAL_CURLOPT_URL = CurlOptionObjectPointBase + 2,
    PAL_CURLOPT_PROXY = CurlOptionObjectPointBase + 4,
    PAL_CURLOPT_PROXYUSERPWD = CurlOptionObjectPointBase + 6,
    PAL_CURLOPT_COOKIE = CurlOptionObjectPointBase + 22,
    PAL_CURLOPT_HTTPHEADER = CurlOptionObjectPointBase + 23,
    PAL_CURLOPT_CUSTOMREQUEST = CurlOptionObjectPointBase + 36,
    PAL_CURLOPT_ACCEPT_ENCODING = CurlOptionObjectPointBase + 102,
    PAL_CURLOPT_PRIVATE = CurlOptionObjectPointBase + 103,
    PAL_CURLOPT_COPYPOSTFIELDS = CurlOptionObjectPointBase + 165,
    PAL_CURLOPT_USERNAME = CurlOptionObjectPointBase + 173,
    PAL_CURLOPT_PASSWORD = CurlOptionObjectPointBase + 174,
    PAL_CURLOPT_CAPATH = CurlOptionObjectPointBase + 97,
    PAL_CURLOPT_PROXY_CAPATH = CurlOptionObjectPointBase + 247,
    PAL_CURLOPT_CAINFO = CurlOptionObjectPointBase + 65,
    PAL_CURLOPT_PROXY_CAINFO = CurlOptionObjectPointBase + 246,

    PAL_CURLOPT_INFILESIZE_LARGE = CurlOptionOffTBase + 115,
    PAL_CURLOPT_POSTFIELDSIZE_LARGE = CurlOptionOffTBase + 120,
} PAL_CURLoption;

typedef enum
{
    Write = 0,
    Read = 1,
    Header = 2,
} ReadWriteFunction;

typedef enum
{
    PAL_CURLE_OK = 0,
    PAL_CURLE_UNSUPPORTED_PROTOCOL = 1,
    PAL_CURLE_FAILED_INIT = 2,
    PAL_CURLE_NOT_BUILT_IN = 4,
    PAL_CURLE_COULDNT_RESOLVE_HOST = 6,
    PAL_CURLE_OUT_OF_MEMORY = 27,
    PAL_CURLE_OPERATION_TIMEDOUT = 28,
    PAL_CURLE_ABORTED_BY_CALLBACK = 42,
    PAL_CURLE_UNKNOWN_OPTION = 48,
    PAL_CURLE_RECV_ERROR = 56,
    PAL_CURLE_SEND_FAIL_REWIND = 65,
} PAL_CURLcode;

enum
{
    CurlInfoStringBase = 0x100000,
    CurlInfoLongBase = 0x200000,
};

typedef enum
{
    PAL_CURL_HTTP_VERSION_NONE = 0,
    PAL_CURL_HTTP_VERSION_1_0 = 1,
    PAL_CURL_HTTP_VERSION_1_1 = 2,
    PAL_CURL_HTTP_VERSION_2TLS = 4
} PAL_CURL_HTTP_VERSION;

typedef enum
{
    PAL_CURL_SSLVERSION_TLSv1 = 1,
    PAL_CURL_SSLVERSION_SSLv2 = 2,
    PAL_CURL_SSLVERSION_SSLv3 = 3,
    PAL_CURL_SSLVERSION_TLSv1_0 = 4,
    PAL_CURL_SSLVERSION_TLSv1_1 = 5,
    PAL_CURL_SSLVERSION_TLSv1_2 = 6,
} PAL_CURL_SSLVERSION;

typedef enum
{
    PAL_CURLINFO_EFFECTIVE_URL = CurlInfoStringBase + 1,
    PAL_CURLINFO_PRIVATE = CurlInfoStringBase + 21,
    PAL_CURLINFO_HTTPAUTH_AVAIL = CurlInfoLongBase + 23,
} PAL_CURLINFO;

typedef enum
{
    PAL_CURLAUTH_None = 0,
    PAL_CURLAUTH_Basic = 1 << 0,
    PAL_CURLAUTH_Digest = 1 << 1,
    PAL_CURLAUTH_Negotiate = 1 << 2,
    PAL_CURLAUTH_NTLM = 1 << 3,
} PAL_CURLAUTH;

typedef enum
{
    PAL_CURLPROXY_HTTP = 0,
} PAL_CURLPROXYTYPE;

typedef enum
{
    PAL_CURLPROTO_HTTP = (1 << 0),
    PAL_CURLPROTO_HTTPS = (1 << 1),
} PAL_CURLPROTO;

typedef enum
{
    PAL_CURL_SEEKFUNC_OK = 0,
    PAL_CURL_SEEKFUNC_FAIL = 1,
    PAL_CURL_SEEKFUNC_CANTSEEK = 2,
} PAL_CurlSeekResult;

typedef enum
{
    PAL_CURLINFO_TEXT = 0,
    PAL_CURLINFO_HEADER_IN = 1,
    PAL_CURLINFO_HEADER_OUT = 2,
    PAL_CURLINFO_DATA_IN = 3,
    PAL_CURLINFO_DATA_OUT = 4,
    PAL_CURLINFO_SSL_DATA_IN = 5,
    PAL_CURLINFO_SSL_DATA_OUT = 6,
} PAL_CurlInfoType;

enum {
    PAL_CURL_READFUNC_ABORT = 0x10000000,
    PAL_CURL_READFUNC_PAUSE = 0x10000001,
    PAL_CURL_WRITEFUNC_PAUSE = 0x10000001,
    PAL_CURL_MAX_HTTP_HEADER = 100 * 1024
};

/*
Creates a new CURL instance.

Returns the new CURL instance or nullptr if something went wrong.
*/
DLLEXPORT CURL* HttpNative_EasyCreate(void);

/*
Cleans up and deletes a CURL instance.

No-op if handle is null.
The given CURL pointer is invalid after this call.
*/
DLLEXPORT void HttpNative_EasyDestroy(CURL* handle);

/*
Shims the curl_easy_setopt function, which takes a variable number of
arguments, but must be a long, a function pointer, an object pointer or a curl_off_t,
depending on what option is supplied.
*/
DLLEXPORT int32_t HttpNative_EasySetOptionString(CURL* handle, PAL_CURLoption option, const char* value);
DLLEXPORT int32_t HttpNative_EasySetOptionLong(CURL* handle, PAL_CURLoption option, int64_t value);
DLLEXPORT int32_t HttpNative_EasySetOptionPointer(CURL* handle, PAL_CURLoption option, void* value);

/*
Returns a string describing the CURLcode error code.
*/
DLLEXPORT const char* HttpNative_EasyGetErrorString(PAL_CURLcode code);

/*
Shims the curl_easy_setopt function, which takes a variable number of
arguments.
*/
DLLEXPORT int32_t HttpNative_EasyGetInfoPointer(CURL* handle, PAL_CURLINFO info, void** value);
DLLEXPORT int32_t HttpNative_EasyGetInfoLong(CURL* handle, PAL_CURLINFO info, int64_t* value);

/*
Shims the curl_easy_perform function.

Returns CURLE_OK (0) if everything was ok, non-zero means an error occurred.
*/
DLLEXPORT int32_t HttpNative_EasyPerform(CURL* handle);

/*
Unpauses the CURL request.

Returns CURLE_OK (0) if everything was ok, non-zero means an error occurred.
*/
DLLEXPORT int32_t HttpNative_EasyUnpause(CURL* handle);

// the function pointer definition for the callback used in RegisterSeekCallback
typedef int32_t (*SeekCallback)(void* userPointer, int64_t offset, int32_t origin);

// the function pointer definition for the callback used in RegisterReadWriteCallback
typedef uint64_t (*ReadWriteCallback)(uint8_t* buffer, uint64_t bufferSize, uint64_t nitems, void* userPointer);

// the function pointer definition for the callback used in RegisterSslCtxCallback
typedef int32_t (*SslCtxCallback)(CURL* curl, void* sslCtx, void* userPointer);

// the function pointer definition for the callback used for debugging callbacks
typedef void(*DebugCallback)(CURL* curl, PAL_CurlInfoType type, char* data, uint64_t size, void* userPointer);

/*
The object that is returned from RegisterXXXCallback functions.
This holds the data necessary to know what managed callback to invoke and with what args.
*/
typedef struct CallbackHandle CallbackHandle;

/*
Registers a callback in libcurl for seeking in an input stream.

This function gets called by libcurl to seek to a certain position in the input stream
and can be used to fast forward a file in a resumed upload.
*/
DLLEXPORT void
HttpNative_RegisterSeekCallback(CURL* curl, SeekCallback callback, void* userPointer, CallbackHandle** callbackHandle);

/*
Registers a callback in libcurl for reading/writing input/output streams.
*/
DLLEXPORT void HttpNative_RegisterReadWriteCallback(CURL* curl,
                                                    ReadWriteFunction functionType,
                                                    ReadWriteCallback callback,
                                                    void* userPointer,
                                                    CallbackHandle** callbackHandle);

/*
Registers a callback in libcurl for initializing SSL connections.

This callback function gets called by libcurl just before the initialization of an SSL connection
after having processed all other SSL related options to give a last chance to an application
to modify the behaviour of the SSL initialization.

Returns a CURLcode that describes whether registering the callback was successful or not.
*/
DLLEXPORT int32_t HttpNative_RegisterSslCtxCallback(CURL* curl,
                                                    SslCtxCallback callback,
                                                    void* userPointer,
                                                    CallbackHandle** callbackHandle);

/*
Registers a callback in libcurl for outputting debug information.

This callback function gets called by libcurl each time it has debug information to report.

Returns a CURLcode that describes whether registering the callback was successful or not.
*/
DLLEXPORT int32_t HttpNative_RegisterDebugCallback(CURL* curl,
                                                   DebugCallback callback,
                                                   void* userPointer,
                                                   CallbackHandle** callbackHandle);

/*
Frees the CallbackHandle created by a RegisterXXXCallback function.
*/
DLLEXPORT void HttpNative_FreeCallbackHandle(CallbackHandle* callbackHandle);
