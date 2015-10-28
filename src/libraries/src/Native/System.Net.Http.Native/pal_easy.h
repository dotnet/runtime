// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "pal_types.h"

#include <curl/curl.h>

#define CurlOptionLongBase 0
#define CurlOptionObjectPointBase 10000
#define CurlOptionFunctionPointBase 20000

enum PAL_CURLoption : int32_t
{
    PAL_CURLOPT_INFILESIZE = CurlOptionLongBase + 14,
    PAL_CURLOPT_VERBOSE = CurlOptionLongBase + 41,
    PAL_CURLOPT_NOBODY = CurlOptionLongBase + 44,
    PAL_CURLOPT_UPLOAD = CurlOptionLongBase + 46,
    PAL_CURLOPT_POST = CurlOptionLongBase + 47,
    PAL_CURLOPT_FOLLOWLOCATION = CurlOptionLongBase + 52,
    PAL_CURLOPT_PROXYPORT = CurlOptionLongBase + 59,
    PAL_CURLOPT_POSTFIELDSIZE = CurlOptionLongBase + 60,
    PAL_CURLOPT_MAXREDIRS = CurlOptionLongBase + 68,
    PAL_CURLOPT_NOSIGNAL = CurlOptionLongBase + 99,
    PAL_CURLOPT_PROXYTYPE = CurlOptionLongBase + 101,
    PAL_CURLOPT_HTTPAUTH = CurlOptionLongBase + 107,
    PAL_CURLOPT_PROTOCOLS = CurlOptionLongBase + 181,
    PAL_CURLOPT_REDIR_PROTOCOLS = CurlOptionLongBase + 182,

    PAL_CURLOPT_WRITEDATA = CurlOptionObjectPointBase + 1,
    PAL_CURLOPT_URL = CurlOptionObjectPointBase + 2,
    PAL_CURLOPT_PROXY = CurlOptionObjectPointBase + 4,
    PAL_CURLOPT_PROXYUSERPWD = CurlOptionObjectPointBase + 6,
    PAL_CURLOPT_READDATA = CurlOptionObjectPointBase + 9,
    PAL_CURLOPT_COOKIE = CurlOptionObjectPointBase + 22,
    PAL_CURLOPT_HTTPHEADER = CurlOptionObjectPointBase + 23,
    PAL_CURLOPT_HEADERDATA = CurlOptionObjectPointBase + 29,
    PAL_CURLOPT_CUSTOMREQUEST = CurlOptionObjectPointBase + 36,
    PAL_CURLOPT_ACCEPT_ENCODING = CurlOptionObjectPointBase + 102,
    PAL_CURLOPT_PRIVATE = CurlOptionObjectPointBase + 103,
    PAL_CURLOPT_COPYPOSTFIELDS = CurlOptionObjectPointBase + 165,
    PAL_CURLOPT_SEEKDATA = CurlOptionObjectPointBase + 168,
    PAL_CURLOPT_USERNAME = CurlOptionObjectPointBase + 173,
    PAL_CURLOPT_PASSWORD = CurlOptionObjectPointBase + 174,

    PAL_CURLOPT_WRITEFUNCTION = CurlOptionFunctionPointBase + 11,
    PAL_CURLOPT_READFUNCTION = CurlOptionFunctionPointBase + 12,
    PAL_CURLOPT_HEADERFUNCTION = CurlOptionFunctionPointBase + 79,
    PAL_CURLOPT_SSL_CTX_FUNCTION = CurlOptionFunctionPointBase + 108,
    PAL_CURLOPT_SEEKFUNCTION = CurlOptionFunctionPointBase + 167,
};

enum PAL_CURLcode : int32_t
{
    PAL_CURLE_OK = 0,
    PAL_CURLE_UNSUPPORTED_PROTOCOL = 1,
    PAL_CURLE_NOT_BUILT_IN = 4,
    PAL_CURLE_COULDNT_RESOLVE_HOST = 6,
};

#define CurlInfoStringBase 0x100000
#define CurlInfoLongBase 0x200000

enum PAL_CURLINFO : int32_t
{
    PAL_CURLINFO_PRIVATE = CurlInfoStringBase + 21,
    PAL_CURLINFO_HTTPAUTH_AVAIL = CurlInfoLongBase + 23,
};

enum PAL_CURLAUTH : int64_t
{
    PAL_CURLAUTH_None = 0,
    PAL_CURLAUTH_Basic = 1 << 0,
    PAL_CURLAUTH_Digest = 1 << 1,
    PAL_CURLAUTH_Negotiate = 1 << 2,
};

enum PAL_CURLPROXYTYPE : int32_t
{
    PAL_CURLPROXY_HTTP = 0,
};

enum PAL_CURLPROTO : int32_t
{
    PAL_CURLPROTO_HTTP = (1 << 0),
    PAL_CURLPROTO_HTTPS = (1 << 1),
};

/*
Creates a new CURL instance.

Returns the new CURL instance or nullptr if something went wrong.
*/
extern "C" CURL* EasyCreate();

/*
Cleans up and deletes a CURL instance.

No-op if handle is null.
The given CURL pointer is invalid after this call.
*/
extern "C" void EasyDestroy(CURL* handle);

/*
Shims the curl_easy_setopt function, which takes a variable number of
arguments, but must be a long, a function pointer, an object pointer or a curl_off_t,
depending on what option is supplied.
*/
extern "C" int32_t EasySetOptionString(CURL* handle, PAL_CURLoption option, const char* value);
extern "C" int32_t EasySetOptionLong(CURL* handle, PAL_CURLoption option, int64_t value);
extern "C" int32_t EasySetOptionPointer(CURL* handle, PAL_CURLoption option, void* value);

/*
Returns a string describing the CURLcode error code.
*/
extern "C" const char* EasyGetErrorString(PAL_CURLcode code);

/*
Shims the curl_easy_setopt function, which takes a variable number of
arguments.
*/
extern "C" int32_t EasyGetInfoPointer(CURL* handle, PAL_CURLINFO info, void** value);
extern "C" int32_t EasyGetInfoLong(CURL* handle, PAL_CURLINFO info, int64_t* value);

/*
Shims the curl_easy_perform function.

Returns CURLE_OK (0) if everything was ok, non-zero means an error occurred.
*/
extern "C" int32_t EasyPerform(CURL* handle);

/*
Shims the curl_easy_pause function.

Returns CURLE_OK (0) if everything was ok, non-zero means an error occurred.
*/
extern "C" int32_t EasyPause(CURL* handle);
