// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//*****************************************************************************
//*****************************************************************************

#ifndef _EEDBGINTERFACEIMPL_INL_
#define _EEDBGINTERFACEIMPL_INL_

#include "common.h"


// This class only serves as a wrapper for the debugger callbacks.
// Using this class eliminates the need to check "#ifdef DEBUGGING_SUPPORTED"
// and "CORDebuggerAttached()".
class EEToDebuggerExceptionInterfaceWrapper
{
  public:

#if defined(DEBUGGING_SUPPORTED) && !defined(DACCESS_COMPILE)
    static inline bool FirstChanceManagedException(Thread* pThread, SIZE_T currentIP, SIZE_T currentSP)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        ThreadExceptionState* pExState = pThread->GetExceptionState();
        pExState->GetDebuggerState()->SetDebuggerIndicatedFramePointer((LPVOID)currentSP);

        if (CORDebuggerAttached())
        {
            // Notfiy the debugger that we are on the first pass for a managed exception.
            // Note that this callback is made for every managed frame.
            return g_pDebugInterface->FirstChanceManagedException(pThread, currentIP, currentSP);
        }
        else
        {
            return false;
        }
    }

    static inline void FirstChanceManagedExceptionCatcherFound(Thread* pThread, MethodDesc* pMD, TADDR pMethodAddr, SIZE_T currentSP,
                                                               EE_ILEXCEPTION_CLAUSE* pEHClause)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;


        ThreadExceptionState* pExState = pThread->GetExceptionState();
        pExState->GetDebuggerState()->SetDebuggerIndicatedFramePointer((LPVOID)currentSP);

        if (CORDebuggerAttached())
        {
            g_pDebugInterface->FirstChanceManagedExceptionCatcherFound(pThread, pMD, pMethodAddr, (PBYTE)currentSP,
                                                                       pEHClause);
        }
    }

    static inline void NotifyOfCHFFilter(EXCEPTION_POINTERS * pExceptionInfo, Frame * pFrame)
    {
        WRAPPER_NO_CONTRACT;

        if (CORDebuggerAttached())
        {
            g_pDebugInterface->NotifyOfCHFFilter(pExceptionInfo, pFrame);
        }
    }

    static inline void ManagedExceptionUnwindBegin(Thread* pThread)
    {
        WRAPPER_NO_CONTRACT;

        if (CORDebuggerAttached())
        {
            g_pDebugInterface->ManagedExceptionUnwindBegin(pThread);
        }
    }

    static inline void ExceptionFilter(MethodDesc* pMD, TADDR pMethodAddr, SIZE_T offset, BYTE* pStack)
    {
        WRAPPER_NO_CONTRACT;

        if (CORDebuggerAttached())
        {
            g_pDebugInterface->ExceptionFilter(pMD, pMethodAddr, offset, pStack);
        }
    }

    static inline void ExceptionHandle(MethodDesc* pMD, TADDR pMethodAddr, SIZE_T offset, BYTE* pStack)
    {
        WRAPPER_NO_CONTRACT;

        if (CORDebuggerAttached())
        {
            g_pDebugInterface->ExceptionHandle(pMD, pMethodAddr, offset, pStack);
        }
    }

#else  // !defined(DEBUGGING_SUPPORTED) || defined(DACCESS_COMPILE)
    static inline bool FirstChanceManagedException(Thread* pThread, SIZE_T currentIP, SIZE_T currentSP) {LIMITED_METHOD_CONTRACT; return false;}
    static inline void FirstChanceManagedExceptionCatcherFound(Thread* pThread, MethodDesc* pMD, TADDR pMethodAddr, BYTE* currentSP,
                                                               EE_ILEXCEPTION_CLAUSE* pEHClause) {LIMITED_METHOD_CONTRACT;}
    static inline void ManagedExceptionUnwindBegin(Thread* pThread) {LIMITED_METHOD_CONTRACT;}
    static inline void ExceptionFilter(MethodDesc* pMD, TADDR pMethodAddr, SIZE_T offset, BYTE* pStack) {LIMITED_METHOD_CONTRACT;}
    static inline void ExceptionHandle(MethodDesc* pMD, TADDR pMethodAddr, SIZE_T offset, BYTE* pStack) {LIMITED_METHOD_CONTRACT;}
#endif // !defined(DEBUGGING_SUPPORTED) || defined(DACCESS_COMPILE)
};


#endif // _EEDBGINTERFACEIMPL_INL_
