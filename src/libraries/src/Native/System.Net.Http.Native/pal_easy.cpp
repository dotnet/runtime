// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_easy.h"

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

static_assert(PAL_CURLOPT_WRITEDATA == CURLOPT_WRITEDATA, "");
static_assert(PAL_CURLOPT_URL == CURLOPT_URL, "");
static_assert(PAL_CURLOPT_PROXY == CURLOPT_PROXY, "");
static_assert(PAL_CURLOPT_PROXYUSERPWD == CURLOPT_PROXYUSERPWD, "");
static_assert(PAL_CURLOPT_READDATA == CURLOPT_READDATA, "");
static_assert(PAL_CURLOPT_COOKIE == CURLOPT_COOKIE, "");
static_assert(PAL_CURLOPT_HTTPHEADER == CURLOPT_HTTPHEADER, "");
static_assert(PAL_CURLOPT_HEADERDATA == CURLOPT_HEADERDATA, "");
static_assert(PAL_CURLOPT_CUSTOMREQUEST == CURLOPT_CUSTOMREQUEST, "");
static_assert(PAL_CURLOPT_ACCEPT_ENCODING == CURLOPT_ACCEPT_ENCODING, "");
static_assert(PAL_CURLOPT_PRIVATE == CURLOPT_PRIVATE, "");
static_assert(PAL_CURLOPT_COPYPOSTFIELDS == CURLOPT_COPYPOSTFIELDS, "");
static_assert(PAL_CURLOPT_SEEKDATA == CURLOPT_SEEKDATA, "");
static_assert(PAL_CURLOPT_USERNAME == CURLOPT_USERNAME, "");
static_assert(PAL_CURLOPT_PASSWORD == CURLOPT_PASSWORD, "");

static_assert(PAL_CURLOPT_WRITEFUNCTION == CURLOPT_WRITEFUNCTION, "");
static_assert(PAL_CURLOPT_READFUNCTION == CURLOPT_READFUNCTION, "");
static_assert(PAL_CURLOPT_HEADERFUNCTION == CURLOPT_HEADERFUNCTION, "");
static_assert(PAL_CURLOPT_SSL_CTX_FUNCTION == CURLOPT_SSL_CTX_FUNCTION, "");
static_assert(PAL_CURLOPT_SEEKFUNCTION == CURLOPT_SEEKFUNCTION, "");

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

extern "C" CURL* EasyCreate()
{
    return curl_easy_init();
}

extern "C" void EasyDestroy(CURL* handle)
{
    curl_easy_cleanup(handle);
}

#define convertOption(option) static_cast<CURLoption>(option)

extern "C" int32_t EasySetOptionString(CURL* handle, PAL_CURLoption option, const char* value)
{
    return curl_easy_setopt(handle, convertOption(option), value);
}

extern "C" int32_t EasySetOptionLong(CURL* handle, PAL_CURLoption option, int64_t value)
{
    return curl_easy_setopt(handle, convertOption(option), value);
}

extern "C" int32_t EasySetOptionPointer(CURL* handle, PAL_CURLoption option, void* value)
{
    return curl_easy_setopt(handle, convertOption(option), value);
}

extern "C" const char* EasyGetErrorString(PAL_CURLcode code)
{
    return curl_easy_strerror(static_cast<CURLcode>(code));
}

#define convertInfo(info) static_cast<CURLINFO>(info)

extern "C" int32_t EasyGetInfoPointer(CURL* handle, PAL_CURLINFO info, void** value)
{
    return curl_easy_getinfo(handle, convertInfo(info), value);
}

extern "C" int32_t EasyGetInfoLong(CURL* handle, PAL_CURLINFO info, int64_t* value)
{
    return curl_easy_getinfo(handle, convertInfo(info), value);
}

extern "C" int32_t EasyPerform(CURL* handle)
{
    return curl_easy_perform(handle);
}

extern "C" int32_t EasyPause(CURL* handle)
{
    return curl_easy_pause(handle, CURLPAUSE_CONT);
}
