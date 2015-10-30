// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_multi.h"

static_assert(PAL_CURLM_OK == CURLM_OK, "");
static_assert(PAL_CURLM_BAD_HANDLE == CURLM_BAD_HANDLE, "");
static_assert(PAL_CURLM_BAD_EASY_HANDLE == CURLM_BAD_EASY_HANDLE, "");
static_assert(PAL_CURLM_OUT_OF_MEMORY == CURLM_OUT_OF_MEMORY, "");
static_assert(PAL_CURLM_INTERNAL_ERROR == CURLM_INTERNAL_ERROR, "");
static_assert(PAL_CURLM_BAD_SOCKET == CURLM_BAD_SOCKET, "");
static_assert(PAL_CURLM_UNKNOWN_OPTION == CURLM_UNKNOWN_OPTION, "");
static_assert(PAL_CURLM_ADDED_ALREADY == CURLM_ADDED_ALREADY, "");

static_assert(PAL_CURLMSG_DONE == CURLMSG_DONE, "");

extern "C" CURLM* MultiCreate()
{
    return curl_multi_init();
}

extern "C" int32_t MultiDestroy(CURLM* multi_handle)
{
    return curl_multi_cleanup(multi_handle);
}

extern "C" int32_t MultiAddHandle(CURLM* multi_handle, CURL* easy_handle)
{
    return curl_multi_add_handle(multi_handle, easy_handle);
}

extern "C" int32_t MultiRemoveHandle(CURLM* multi_handle, CURL* easy_handle)
{
    return curl_multi_remove_handle(multi_handle, easy_handle);
}

extern "C" int32_t MultiWait(CURLM* multi_handle, int32_t extraFileDescriptor, int32_t* isExtraFileDescriptorActive, int32_t* isTimeout)
{
    if (!isExtraFileDescriptorActive || !isTimeout)
    {
        if (isExtraFileDescriptorActive)
            *isExtraFileDescriptorActive = 0;
        if (isTimeout)
            *isTimeout = 0;

        return CURLM_INTERNAL_ERROR;
    }

    curl_waitfd extraFds =
    {
        .fd = extraFileDescriptor,
        .events = CURL_WAIT_POLLIN,
        .revents = 0
    };

    // Even with our cancellation mechanism, we specify a timeout so that
    // just in case something goes wrong we can recover gracefully.  This timeout is relatively long.
    // Note, though, that libcurl has its own internal timeout, which can be requested separately
    // via curl_multi_timeout, but which is used implicitly by curl_multi_wait if it's shorter
    // than the value we provide.
    const int FailsafeTimeoutMilliseconds = 1000;

    int numFds;
    CURLMcode result = curl_multi_wait(multi_handle, &extraFds, 1, FailsafeTimeoutMilliseconds, &numFds);

    *isExtraFileDescriptorActive = (extraFds.revents & CURL_WAIT_POLLIN) != 0;
    *isTimeout = numFds == 0;

    return result;
}

extern "C" int32_t MultiPerform(CURLM* multi_handle)
{
    int running_handles;
    return curl_multi_perform(multi_handle, &running_handles);
}

static int32_t DefaultMessageInfo(int32_t* message, CURL** easy_handle, int32_t* result)
{
    if (message)
        *message = 0;
    if (easy_handle)
        *easy_handle = nullptr;
    if (result)
        *result = 0;

    return 0;
}

extern "C" int32_t MultiInfoRead(CURLM* multi_handle, int32_t* message, CURL** easy_handle, int32_t* result)
{
    if (!message || !easy_handle || !result)
    {
        return DefaultMessageInfo(message, easy_handle, result);
    }

    int msgs_in_queue;
    CURLMsg* curlMessage = curl_multi_info_read(multi_handle, &msgs_in_queue);
    if (!curlMessage)
    {
        return DefaultMessageInfo(message, easy_handle, result);
    }
 
    *message = curlMessage->msg;
    *easy_handle = curlMessage->easy_handle;
    *result = curlMessage->data.result;

    return 1;
}

extern "C" const char* MultiGetErrorString(PAL_CURLMcode code)
{
    return curl_multi_strerror(static_cast<CURLMcode>(code));
}
