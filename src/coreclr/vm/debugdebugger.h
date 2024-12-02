// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header: DebugDebugger.h
**
** Purpose: Native methods on System.Debug.Debugger
**
===========================================================*/

#ifndef __DEBUG_DEBUGGER_h__
#define __DEBUG_DEBUGGER_h__
#include <object.h>

extern "C" void QCALLTYPE DebugDebugger_Break();
extern "C" BOOL QCALLTYPE DebugDebugger_Launch();
extern "C" void QCALLTYPE DebugDebugger_Log(INT32 Level, PCWSTR pwzModule, PCWSTR pwzMessage);
extern "C" void QCALLTYPE DebugDebugger_CustomNotification(QCall::ObjectHandleOnStack data);
extern "C" BOOL QCALLTYPE DebugDebugger_IsLoggingHelper();
extern "C" BOOL QCALLTYPE DebugDebugger_IsManagedDebuggerAttached();

class StackFrameHelper : public Object
{
    // READ ME:
    // Modifying the order or fields of this object may require other changes to the
    // classlib definition of the StackFrameHelper class.
public:
    I4ARRAYREF rgiOffset;
    I4ARRAYREF rgiILOffset;
    PTRARRAYREF dynamicMethods;
    BASEARRAYREF rgMethodHandle;
    PTRARRAYREF rgAssemblyPath;
    PTRARRAYREF rgAssembly;
    BASEARRAYREF rgLoadedPeAddress;
    I4ARRAYREF rgiLoadedPeSize;
    BOOLARRAYREF rgiIsFileLayout;
    BASEARRAYREF rgInMemoryPdbAddress;
    I4ARRAYREF rgiInMemoryPdbSize;
    // if rgiMethodToken[i] == 0, then don't attempt to get the portable PDB source/info
    I4ARRAYREF rgiMethodToken;
    PTRARRAYREF rgFilename;
    I4ARRAYREF rgiLineNumber;
    I4ARRAYREF rgiColumnNumber;

    BOOLARRAYREF rgiLastFrameFromForeignExceptionStackTrace;

    int iFrameCount;

protected:
    StackFrameHelper() {}
    ~StackFrameHelper() {}

public:
    void SetFrameCount(int iCount)
    {
        iFrameCount = iCount;
    }

    int  GetFrameCount(void)
    {
        return iFrameCount;
    }

};

#ifdef USE_CHECKED_OBJECTREFS
typedef REF <StackFrameHelper> STACKFRAMEHELPERREF;
#else
typedef StackFrameHelper* STACKFRAMEHELPERREF;
#endif


class DebugStackTrace
{
public:
    struct Element {
        DWORD dwOffset;     // native offset
        DWORD dwILOffset;
        MethodDesc *pFunc;
        PCODE ip;
        INT flags;          // StackStackElementFlags

        // Initialization done under TSL.
        // This is used when first collecting the stack frame data.
        void InitPass1(
            DWORD dwNativeOffset,
            MethodDesc *pFunc,
            PCODE ip,
            INT flags       // StackStackElementFlags
        );

        // Initialization done outside the TSL.
        // This will init the dwILOffset field (and potentially anything else
        // that can't be done under the TSL).
        void InitPass2();
    };

public:

    struct GetStackFramesData
    {
        INT32   NumFramesRequested;
        INT32   cElementsAllocated;
        INT32   cElements;
        Element* pElements;
        THREADBASEREF   TargetThread;
        AppDomain *pDomain;
        BOOL fDoWeHaveAnyFramesFromForeignStackTrace;

        GetStackFramesData()
            : NumFramesRequested (0)
            , cElementsAllocated(0)
            , cElements(0)
            , pElements(NULL)
            , TargetThread((THREADBASEREF)(TADDR)NULL)
            , fDoWeHaveAnyFramesFromForeignStackTrace(FALSE)
        {
            LIMITED_METHOD_CONTRACT;
        }

        ~GetStackFramesData()
        {
            delete [] pElements;
        }
    };

    static void GetStackFramesFromException(OBJECTREF * e, GetStackFramesData *pData, PTRARRAYREF * pDynamicMethodArray = NULL);
};

extern "C" void QCALLTYPE StackTrace_GetStackFramesInternal(
    QCall::ObjectHandleOnStack stackFrameHelper,
    BOOL fNeedFileInfo,
    QCall::ObjectHandleOnStack exception);

extern "C" MethodDesc* QCALLTYPE StackFrame_GetMethodDescFromNativeIP(LPVOID ip);

#endif  // __DEBUG_DEBUGGER_h__
