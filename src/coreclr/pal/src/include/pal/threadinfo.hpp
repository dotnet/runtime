// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/threadinfo.hpp

Abstract:
    Header file for thread info initializer



--*/

#ifndef _PAL_THREADINFO_H_
#define _PAL_THREADINFO_H_

#include "corunix.hpp"

namespace CorUnix
{
    //
    // There are a number of different functional areas for which we need to
    // store per-thread data:
    // * synchronization
    // * structure exception handling
    // * asynchronous procedure calls
    // * thread suspension
    // * thread-local storage
    // * CRT per-thread buffers
    //
    // For each of the above functional areas we build a class that stores
    // the necessary data. An instance of each of these classes is embedded
    // in the main thread class. The classes must not have any failure paths
    // in their constructors. Each class inherits from a common parent class
    // that exposes two virtual initialization routines (which may return an
    // error). The first initialization routine is called after the thread
    // object is allocated, but before the new thread is created. Any
    // initialization that is not dependant on knowledge of the new thread's
    // ID (and by extension need not run in the context of the new thread)
    // should take place in the first routine. Work that must run in the
    // context of the new thread or that must know the new thread's ID
    // should take place in the second initialization routine.
    //

    class CThreadInfoInitializer
    {
    public:

        //
        // InitializePreCreate is called before the new thread is started.
        // Any allocations or other initializations that may fail that do
        // not need to run in the context of the new thread (or know the
        // new thread's ID) should take place in this routine.
        //

        virtual
        PAL_ERROR
        InitializePreCreate(
            void
            )
        {
            return NO_ERROR;
        };

        //
        // InitializePostCreate is called from within the context of the
        // new thread.
        //

        virtual
        PAL_ERROR
        InitializePostCreate(
            CPalThread *pThread,
            SIZE_T threadId,
            DWORD dwLwpId
            )
        {
            return NO_ERROR;
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

#endif // _PAL_THREADINFO_H_
