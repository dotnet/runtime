// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_types.h"

#include <curl/curl.h>

enum PAL_CURLMcode : int32_t
{
    PAL_CURLM_CALL_MULTI_PERFORM = -1,
    PAL_CURLM_OK = 0,
    PAL_CURLM_BAD_HANDLE = 1,
    PAL_CURLM_BAD_EASY_HANDLE = 2,
    PAL_CURLM_OUT_OF_MEMORY = 3,
    PAL_CURLM_INTERNAL_ERROR = 4,
    PAL_CURLM_BAD_SOCKET = 5,
    PAL_CURLM_UNKNOWN_OPTION = 6,
    PAL_CURLM_ADDED_ALREADY = 7, // Added in libcurl 7.32.1
};

enum PAL_CURLMoption : int32_t
{
    PAL_CURLMOPT_PIPELINING = 3,
    PAL_CURLMOPT_MAX_HOST_CONNECTIONS = 7,
};

enum PAL_CurlPipe : int32_t
{
    PAL_CURLPIPE_MULTIPLEX = 2,
};

enum PAL_CURLMSG : int32_t
{
    PAL_CURLMSG_DONE = 1,
};

/*
Creates a new CURLM instance.

Returns the new CURLM instance or nullptr if something went wrong.
*/
extern "C" CURLM* HttpNative_MultiCreate();

/*
Cleans up and removes a whole multi stack.

Returns CURLM_OK on success, otherwise an error code.
*/
extern "C" int32_t HttpNative_MultiDestroy(CURLM* multiHandle);

/*
Shims the curl_multi_add_handle function.

Returns CURLM_OK on success, otherwise an error code.
*/
extern "C" int32_t HttpNative_MultiAddHandle(CURLM* multiHandle, CURL* easyHandle);

/*
Shims the curl_multi_remove_handle function.

Returns CURLM_OK on success, otherwise an error code.
*/
extern "C" int32_t HttpNative_MultiRemoveHandle(CURLM* multiHandle, CURL* easyHandle);

/*
Shims the curl_multi_wait function.

Returns CURLM_OK on success, otherwise an error code.

isExtraFileDescriptorActive is set to a value indicating whether extraFileDescriptor has new data received.
isTimeout is set to a value indicating whether a timeout was encountered before any file descriptors had events occur.
*/
extern "C" int32_t HttpNative_MultiWait(CURLM* multiHandle,
                                        intptr_t extraFileDescriptor,
                                        int32_t* isExtraFileDescriptorActive,
                                        int32_t* isTimeout);

/*
Reads/writes available data from each easy handle.
Shims the curl_multi_perform function.

Returns CURLM_OK on success, otherwise an error code.
*/
extern "C" int32_t HttpNative_MultiPerform(CURLM* multiHandle);

/*
Ask the multi handle if there are any messages/informationals from the individual transfers.
Shims the curl_multi_info_read function.

Returns 1 if a CURLMsg was retrieved and the out variables are set,
otherwise 0 when there are no more messages to retrieve.
*/
extern "C" int32_t HttpNative_MultiInfoRead(CURLM* multiHandle, int32_t* message, CURL** easyHandle, int32_t* result);

/*
Returns a string describing the CURLMcode error code.
*/
extern "C" const char* HttpNative_MultiGetErrorString(PAL_CURLMcode code);

/*
Shims the curl_multi_setopt function
*/
extern "C" int32_t HttpNative_MultiSetOptionLong(CURLM* handle, PAL_CURLMoption option, int64_t value);
