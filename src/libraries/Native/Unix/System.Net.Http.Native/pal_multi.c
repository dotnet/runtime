// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_multi.h"
#include "pal_utilities.h"

#include <assert.h>
#include <poll.h>

c_static_assert(PAL_CURLM_CALL_MULTI_PERFORM == CURLM_CALL_MULTI_PERFORM);
c_static_assert(PAL_CURLM_OK == CURLM_OK);
c_static_assert(PAL_CURLM_BAD_HANDLE == CURLM_BAD_HANDLE);
c_static_assert(PAL_CURLM_BAD_EASY_HANDLE == CURLM_BAD_EASY_HANDLE);
c_static_assert(PAL_CURLM_OUT_OF_MEMORY == CURLM_OUT_OF_MEMORY);
c_static_assert(PAL_CURLM_INTERNAL_ERROR == CURLM_INTERNAL_ERROR);
c_static_assert(PAL_CURLM_BAD_SOCKET == CURLM_BAD_SOCKET);
c_static_assert(PAL_CURLM_UNKNOWN_OPTION == CURLM_UNKNOWN_OPTION);
#if HAVE_CURLM_ADDED_ALREADY
c_static_assert(PAL_CURLM_ADDED_ALREADY == CURLM_ADDED_ALREADY);
#endif
c_static_assert(PAL_CURLMOPT_PIPELINING == CURLMOPT_PIPELINING);
#ifdef CURLMOPT_MAX_HOST_CONNECTIONS
c_static_assert(PAL_CURLMOPT_MAX_HOST_CONNECTIONS == CURLMOPT_MAX_HOST_CONNECTIONS);
#endif
#if HAVE_CURLPIPE_MULTIPLEX
c_static_assert(PAL_CURLPIPE_MULTIPLEX == CURLPIPE_MULTIPLEX);
#endif

c_static_assert(PAL_CURLMSG_DONE == CURLMSG_DONE);

CURLM* HttpNative_MultiCreate()
{
    return curl_multi_init();
}

int32_t HttpNative_MultiDestroy(CURLM* multiHandle)
{
    return curl_multi_cleanup(multiHandle);
}

int32_t HttpNative_MultiAddHandle(CURLM* multiHandle, CURL* easyHandle)
{
    return curl_multi_add_handle(multiHandle, easyHandle);
}

int32_t HttpNative_MultiRemoveHandle(CURLM* multiHandle, CURL* easyHandle)
{
    return curl_multi_remove_handle(multiHandle, easyHandle);
}

int32_t HttpNative_MultiWait(CURLM* multiHandle,
                                        intptr_t extraFileDescriptor,
                                        int32_t* isExtraFileDescriptorActive,
                                        int32_t* isTimeout)
{
    assert(isExtraFileDescriptorActive != NULL);
    assert(isTimeout != NULL);

    struct curl_waitfd extraFds = {.fd = ToFileDescriptor(extraFileDescriptor), .events = CURL_WAIT_POLLIN, .revents = 0};

    // Even with our cancellation mechanism, we specify a timeout so that
    // just in case something goes wrong we can recover gracefully.  This timeout is relatively long.
    // Note, though, that libcurl has its own internal timeout, which can be requested separately
    // via curl_multi_timeout, but which is used implicitly by curl_multi_wait if it's shorter
    // than the value we provide.
    const int FailsafeTimeoutMilliseconds = 1000;

    int numFds;
    CURLMcode result = curl_multi_wait(multiHandle, &extraFds, 1, FailsafeTimeoutMilliseconds, &numFds);

    if (numFds == 0)
    {
        *isTimeout = true;
        *isExtraFileDescriptorActive = false;
    }
    else
    {
        *isTimeout = false;

        //
        // Prior to libcurl version 7.32.0, the revents field was not returned properly for "extra" file descriptors
        // passed to curl_multi_wait.  See https://github.com/dotnet/corefx/issues/9751.  So if we have a libcurl
        // prior to that version, we need to do our own poll to get the status of the extra file descriptor.
        //
        if (curl_version_info(CURLVERSION_NOW)->version_num >= 0x072000)
        {
            *isExtraFileDescriptorActive = (extraFds.revents & CURL_WAIT_POLLIN) != 0;
        }
        else
        {
            struct pollfd pfd = { .fd = ToFileDescriptor(extraFileDescriptor),.events = POLLIN,.revents = 0 };
            poll(&pfd, 1, 0);

            //
            // We ignore any failure in poll(), to preserve the result from curl_multi_wait.  If poll() fails, it should
            // leave revents cleared.
            //
            *isExtraFileDescriptorActive = (pfd.revents & POLLIN) != 0;
        }
    }

    return result;
}

int32_t HttpNative_MultiPerform(CURLM* multiHandle)
{
    int running_handles;
    return curl_multi_perform(multiHandle, &running_handles);
}

int32_t HttpNative_MultiInfoRead(CURLM* multiHandle, int32_t* message, CURL** easyHandle, int32_t* result)
{
    assert(message != NULL);
    assert(easyHandle != NULL);
    assert(result != NULL);

    int msgs_in_queue;
    CURLMsg* curlMessage = curl_multi_info_read(multiHandle, &msgs_in_queue);
    if (curlMessage == NULL)
    {
        *message = 0;
        *easyHandle = NULL;
        *result = 0;

        return 0;
    }

    *message = (int32_t)(curlMessage->msg);
    *easyHandle = curlMessage->easy_handle;
    *result = (int32_t)(curlMessage->data.result);

    return 1;
}

const char* HttpNative_MultiGetErrorString(PAL_CURLMcode code)
{
    return curl_multi_strerror((CURLMcode)code);
}

int32_t HttpNative_MultiSetOptionLong(CURLM* handle, PAL_CURLMoption option, int64_t value)
{
    return curl_multi_setopt(handle, (CURLMoption)option, value);
}
