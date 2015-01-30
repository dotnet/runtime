//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    thread/procprivate.hpp

Abstract:

    Private process structures and routines
    
Revision History:



--*/

#ifndef _PAL_PROCPRIVATE_HPP_
#define _PAL_PROCPRIVATE_HPP_

#include "pal/thread.hpp"

namespace CorUnix
{

    /*++
    Function:
      PROCAddThread

    Abstract
      Add a thread to the thread list of the current process
    --*/
    void PROCAddThread(CPalThread *pCurrentThread, CPalThread *pTargetThread);

    extern CPalThread *pGThreadList;

    /*++
    Function:
      PROCRemoveThread

    Abstract
      Remove a thread form the thread list of the current process
    --*/
    void PROCRemoveThread(CPalThread *pCurrentThread, CPalThread *pTargetThread);

    /*++
    Function:
      PROCGetNumberOfThreads

    Abstract
      Return the number of threads in the thread list.
    --*/
    INT PROCGetNumberOfThreads(void);


    /*++
    Function:
      TerminateCurrentProcessNoExit

    Parameters:
        BOOL bTerminateUnconditionally - If this is set, the PAL will exit as
        quickly as possible. In particular, it will not unload DLLs.

    Abstract:
        Terminate Current Process, but leave the caller alive
    --*/
    void TerminateCurrentProcessNoExit(BOOL bTerminateUnconditionally);

}

#endif //_PAL_PROCPRIVATE_HPP_


