// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header: DebugDebugger.h
**
** Purpose: Native methods on System.Debug.Debugger
**
**

===========================================================*/

#ifndef __DEBUG_DEBUGGER_h__
#define __DEBUG_DEBUGGER_h__
#include <object.h>


class DebugDebugger
{
public:
    static FCDECL0(void, Break);
    static FCDECL0(FC_BOOL_RET, IsDebuggerAttached);

    // receives a custom notification object from the target and sends it to the RS via
    // code:Debugger::SendCustomDebuggerNotification
    static FCDECL1(void, CustomNotification, Object * dataUNSAFE);

    static FCDECL0(FC_BOOL_RET, IsLogging);
};

extern "C" BOOL QCALLTYPE DebugDebugger_Launch();
extern "C" void QCALLTYPE DebugDebugger_Log(INT32 Level, PCWSTR pwzModule, PCWSTR pwzMessage);


class StackFrameHelper : public Object
{
    // READ ME:
    // Modifying the order or fields of this object may require other changes to the
    // classlib definition of the StackFrameHelper class.
public:
    THREADBASEREF targetThread;
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

#ifndef DACCESS_COMPILE
// the DAC directly uses the GetStackFramesData and DebugStackTraceElement types
private:
#endif // DACCESS_COMPILE
    struct DebugStackTraceElement {
        DWORD dwOffset;     // native offset
        DWORD dwILOffset;
        MethodDesc *pFunc;
        PCODE ip;
        INT flags;          // StackStackElementFlags
        PTR_VOID pExactGenericArgsToken;

        // Initialization done under TSL.
        // This is used when first collecting the stack frame data.
        void InitPass1(
            DWORD dwNativeOffset,
            MethodDesc *pFunc,
            PCODE ip,
            INT flags,       // StackStackElementFlags
            PTR_VOID pExactGenericArgsToken
        );

        // Initialization done outside the TSL.
        // This will init the dwILOffset field (and potentially anything else
        // that can't be done under the TSL).
        void InitPass2();
    };

public:

    struct GetStackFramesData {

        // Used for the integer-skip version
        INT32   skip;
        INT32   NumFramesRequested;
        INT32   cElementsAllocated;
        INT32   cElements;
        DebugStackTraceElement* pElements;
        THREADBASEREF   TargetThread;
        AppDomain *pDomain;
        BOOL fDoWeHaveAnyFramesFromForeignStackTrace;


        GetStackFramesData() :  skip(0),
                                NumFramesRequested (0),
                                cElementsAllocated(0),
                                cElements(0),
                                pElements(NULL),
                                TargetThread((THREADBASEREF)(TADDR)NULL)
        {
            LIMITED_METHOD_CONTRACT;
            fDoWeHaveAnyFramesFromForeignStackTrace = FALSE;

        }

        ~GetStackFramesData()
        {
            delete [] pElements;
        }
    };

    static FCDECL4(void,
                   GetStackFramesInternal,
                   StackFrameHelper* pStackFrameHelper,
                   INT32 iSkip,
                   CLR_BOOL fNeedFileInfo,
                   Object* pException
                  );

    static void GetStackFramesFromException(OBJECTREF * e, GetStackFramesData *pData, PTRARRAYREF * pDynamicMethodArray = NULL);

#ifndef DACCESS_COMPILE
// the DAC directly calls GetStackFramesFromException
private:
#endif

    static void GetStackFramesHelper(Frame *pStartFrame, void* pStopStack, GetStackFramesData *pData);

    static void GetStackFrames(Frame *pStartFrame, void* pStopStack, GetStackFramesData *pData);

    static StackWalkAction GetStackFramesCallback(CrawlFrame* pCf, VOID* data);

};

extern "C" MethodDesc* QCALLTYPE StackFrame_GetMethodDescFromNativeIP(LPVOID ip);

#endif  // __DEBUG_DEBUGGER_h__
