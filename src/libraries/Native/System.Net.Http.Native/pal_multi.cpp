// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_multi.h"
#include "pal_utilities.h"

#include <assert.h>

static_assert(PAL_CURLM_CALL_MULTI_PERFORM == CURLM_CALL_MULTI_PERFORM, "");
static_assert(PAL_CURLM_OK == CURLM_OK, "");
static_assert(PAL_CURLM_BAD_HANDLE == CURLM_BAD_HANDLE, "");
static_assert(PAL_CURLM_BAD_EASY_HANDLE == CURLM_BAD_EASY_HANDLE, "");
static_assert(PAL_CURLM_OUT_OF_MEMORY == CURLM_OUT_OF_MEMORY, "");
static_assert(PAL_CURLM_INTERNAL_ERROR == CURLM_INTERNAL_ERROR, "");
static_assert(PAL_CURLM_BAD_SOCKET == CURLM_BAD_SOCKET, "");
static_assert(PAL_CURLM_UNKNOWN_OPTION == CURLM_UNKNOWN_OPTION, "");
#if HAVE_CURLM_ADDED_ALREADY
static_assert(PAL_CURLM_ADDED_ALREADY == CURLM_ADDED_ALREADY, "");
#endif
static_assert(PAL_CURLMOPT_PIPELINING == CURLMOPT_PIPELINING, "");
#if HAVE_CURLPIPE_MULTIPLEX
static_assert(PAL_CURLPIPE_MULTIPLEX == CURLPIPE_MULTIPLEX, "");
#endif

static_assert(PAL_CURLMSG_DONE == CURLMSG_DONE, "");

extern "C" CURLM* HttpNative_MultiCreate()
{
    return curl_multi_init();
}

extern "C" int32_t HttpNative_MultiDestroy(CURLM* multiHandle)
{
    return curl_multi_cleanup(multiHandle);
}

extern "C" int32_t HttpNative_MultiAddHandle(CURLM* multiHandle, CURL* easyHandle)
{
    return curl_multi_add_handle(multiHandle, easyHandle);
}

extern "C" int32_t HttpNative_MultiRemoveHandle(CURLM* multiHandle, CURL* easyHandle)
{
    return curl_multi_remove_handle(multiHandle, easyHandle);
}

extern "C" int32_t HttpNative_MultiWait(CURLM* multiHandle,
                                        intptr_t extraFileDescriptor,
                                        int32_t* isExtraFileDescriptorActive,
                                        int32_t* isTimeout)
{
    assert(isExtraFileDescriptorActive != nullptr);
    assert(isTimeout != nullptr);

    curl_waitfd extraFds = {.fd = ToFileDescriptor(extraFileDescriptor), .events = CURL_WAIT_POLLIN, .revents = 0};

    // Even with our cancellation mechanism, we specify a timeout so that
    // just in case something goes wrong we can recover gracefully.  This timeout is relatively long.
    // Note, though, that libcurl has its own internal timeout, which can be requested separately
    // via curl_multi_timeout, but which is used implicitly by curl_multi_wait if it's shorter
    // than the value we provide.
    const int FailsafeTimeoutMilliseconds = 1000;

    int numFds;
    CURLMcode result = curl_multi_wait(multiHandle, &extraFds, 1, FailsafeTimeoutMilliseconds, &numFds);

    *isExtraFileDescriptorActive = (extraFds.revents & CURL_WAIT_POLLIN) != 0;
    *isTimeout = numFds == 0;

    return result;
}

extern "C" int32_t HttpNative_MultiPerform(CURLM* multiHandle)
{
    int running_handles;
    return curl_multi_perform(multiHandle, &running_handles);
}

extern "C" int32_t HttpNative_MultiInfoRead(CURLM* multiHandle, int32_t* message, CURL** easyHandle, int32_t* result)
{
    assert(message != nullptr);
    assert(easyHandle != nullptr);
    assert(result != nullptr);

    int msgs_in_queue;
    CURLMsg* curlMessage = curl_multi_info_read(multiHandle, &msgs_in_queue);
    if (curlMessage == nullptr)
    {
        *message = 0;
        *easyHandle = nullptr;
        *result = 0;

        return 0;
    }

    *message = curlMessage->msg;
    *easyHandle = curlMessage->easy_handle;
    *result = curlMessage->data.result;

    return 1;
}

extern "C" const char* HttpNative_MultiGetErrorString(PAL_CURLMcode code)
{
    return curl_multi_strerror(static_cast<CURLMcode>(code));
}

extern "C" int32_t HttpNative_MultiSetOptionLong(CURLM* handle, PAL_CURLMoption option, int64_t value)
{
    return curl_multi_setopt(handle, static_cast<CURLMoption>(option), value);
}
