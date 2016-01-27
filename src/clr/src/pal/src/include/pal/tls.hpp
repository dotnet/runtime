// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    include/pal/tls.hpp

Abstract:
    Header file for thread local storage



--*/

#ifndef _PAL_TLS_HPP
#define _PAL_TLS_HPP

#include "threadinfo.hpp"

namespace CorUnix
{
    /* This is the number of slots available for use in TlsAlloc().
    sTlsSlotFields in thread/localstorage.c must be this number
    of bits. */
#define TLS_SLOT_SIZE   64

    class CThreadTLSInfo : public CThreadInfoInitializer
    {
    public:
        LPVOID tlsSlots[TLS_SLOT_SIZE];

        virtual
        PAL_ERROR
        InitializePostCreate(
            CPalThread *pThread,
            SIZE_T threadId,
            DWORD dwLwpId
            );

        CThreadTLSInfo()
        {
            ZeroMemory(tlsSlots, sizeof(tlsSlots));
        };        
    };

    //
    // InternalGetCurrentThread obtains the CPalThread instance for the
    // calling thread. That instance should only be used by the calling
    // thread. If another thread will at some point need access to this
    // thread information it should be given a referenced pointer to
    // the IPalObject stored within the CPalThread.
    //

    extern pthread_key_t thObjKey;

    CPalThread *InternalGetCurrentThread();
}

#endif // _PAL_TLS_HPP

