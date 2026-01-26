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

// Validate that the IL offsets in the method returned by WalkILOffsets matches that as reported by the debugger.
void ValidateILOffsets(MethodDesc *pFunc, uint8_t* ipColdStart, size_t coldLen, uint8_t* ipHotStart, size_t hotLen);

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


class ILToNativeMapArrays
{
private:
    // This is the limit on how big the il-to-native map can get, as measured by number
    // of entries in each parallel array (IL offset array and native offset array).
    // The logic is used to by tracing to ensure that the size of the event does not exceed the Windows event size limit.
    uint32_t m_cMapEntriesMax;
    bool m_fHasLastMapping = false;
    bool m_fNextReplacesLast = false;
    int callInstrSequence = 0;
    InlineSArray<uint32_t, 128> m_rguiNativeOffset;
    InlineSArray<uint32_t, 128> m_rguiILOffset;

    static int32_t CompareILOffsets(uint32_t ilOffsetA, uint32_t ilOffsetB);
    static bool CompareLessOffsets(uint32_t ilOffsetA, uint32_t nativeOffsetA, uint32_t ilOffsetB, uint32_t nativeOffsetB);
    static bool CompareGreaterOffsets(uint32_t ilOffsetA, uint32_t nativeOffsetA, uint32_t ilOffsetB, uint32_t nativeOffsetB);
    static void Swap(uint32_t* rguiNativeOffset, uint32_t *rguiILOffset, int32_t i, int32_t j);
    static void Copy(uint32_t* rguiNativeOffset, uint32_t *rguiILOffset, int32_t i, int32_t j);
    static int32_t Partition(uint32_t* rguiNativeOffset, uint32_t *rguiILOffset, int32_t low, int32_t high);
    static void QuickSort(uint32_t* rguiNativeOffset, uint32_t *rguiILOffset, int32_t low, int32_t high, int32_t stackDepth = 0);

    void Sort()
    {
        LIMITED_METHOD_CONTRACT;

        if (m_rguiNativeOffset.GetCount() > 1)
        {
            // Sort the arrays in place.
            QuickSort(m_rguiNativeOffset.GetElements(), m_rguiILOffset.GetElements(), 0, m_rguiNativeOffset.GetCount() - 1);
        }
    }

public:

    ILToNativeMapArrays(uint32_t cMapEntriesMax)
        : m_cMapEntriesMax(cMapEntriesMax)
    {
        LIMITED_METHOD_CONTRACT;
    }

    void GetArrays(uint32_t *pcMap, uint32_t **prguiNativeOffset, uint32_t **prguiILOffset)
    {
        LIMITED_METHOD_CONTRACT;
        Sort();
        if (m_rguiNativeOffset.GetCount() > 0)
        {
            static_assert(sizeof(uint32_t) == sizeof(COUNT_T));
            if (m_rguiNativeOffset.GetCount() > m_cMapEntriesMax)
            {
                // If the number of entries exceeds the maximum, we cap it to the maximum.
                // This is to ensure that we do not exceed the Windows event size limit.
                // Note that this is a rare case, as the JIT should not produce more than
                // m_cMapEntriesMax entries in a method in common scenarios, unless code is
                // extremely large
                *pcMap = m_cMapEntriesMax;
            }
            else
            {
                *pcMap = m_rguiNativeOffset.GetCount();
            }
            *prguiNativeOffset = m_rguiNativeOffset.GetElements();
            *prguiILOffset = m_rguiILOffset.GetElements();
        }
        else
        {
            *pcMap = 0;
            *prguiNativeOffset = NULL;
            *prguiILOffset = NULL;
        }
    }

    void AddEntry(ICorDebugInfo::OffsetMapping *pOffsetMapping);
};


size_t ComputeILOffsetArrays(ICorDebugInfo::OffsetMapping *pOffsetMapping, void *pContext);

#endif  // __DEBUG_DEBUGGER_h__
