// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// stublink.cpp
//



#include "common.h"

#include "threads.h"
#include "excep.h"
#include "stublink.h"
#include "stubgen.h"
#include "stublink.inl"

#include "rtlfunctions.h"

#define S_BYTEPTR(x)    S_SIZE_T((SIZE_T)(x))

#ifndef DACCESS_COMPILE


//************************************************************************
// CodeElement
//
// There are two types of CodeElements: CodeRuns (a stream of uninterpreted
// code bytes) and LabelRefs (an instruction containing
// a fixup.)
//************************************************************************
struct CodeElement
{
    enum CodeElementType {
        kCodeRun  = 0,
        kLabelRef = 1,
    };


    CodeElementType     m_type;  // kCodeRun or kLabelRef
    CodeElement        *m_next;  // ptr to next CodeElement

    // Used as workspace during Link(): holds the offset relative to
    // the start of the final stub.
    UINT                m_globaloffset;
    UINT                m_dataoffset;
};


//************************************************************************
// CodeRun: A run of uninterrupted code bytes.
//************************************************************************

#ifdef _DEBUG
#define CODERUNSIZE 3
#else
#define CODERUNSIZE 32
#endif

struct CodeRun : public CodeElement
{
    UINT    m_numcodebytes;       // how many bytes are actually used
    BYTE    m_codebytes[CODERUNSIZE];
};

//************************************************************************
// LabelRef: An instruction containing an embedded label reference
//************************************************************************
struct LabelRef : public CodeElement
{
    // provides platform-specific information about the instruction
    InstructionFormat    *m_pInstructionFormat;

    // a variation code (interpretation is specific to the InstructionFormat)
    //  typically used to customize an instruction (e.g. with a condition
    //  code.)
    UINT                 m_variationCode;


    CodeLabel           *m_target;

    // Workspace during the link phase
    UINT                 m_refsize;


    // Pointer to next LabelRef
    LabelRef            *m_nextLabelRef;
};


//************************************************************************
// IntermediateUnwindInfo
//************************************************************************

#ifdef STUBLINKER_GENERATES_UNWIND_INFO


#ifdef TARGET_AMD64
// List of unwind operations, queued in StubLinker::m_pUnwindInfoList.
struct IntermediateUnwindInfo
{
    IntermediateUnwindInfo *pNext;
    CodeRun *pCodeRun;
    UINT LocalOffset;
    UNWIND_CODE rgUnwindCode[1];    // variable length, depends on first entry's UnwindOp
};
#endif // TARGET_AMD64


StubUnwindInfoHeapSegment *g_StubHeapSegments;
CrstStatic g_StubUnwindInfoHeapSegmentsCrst;
#ifdef _DEBUG  // for unit test
void *__DEBUG__g_StubHeapSegments = &g_StubHeapSegments;
#endif


//
// Callback registered via RtlInstallFunctionTableCallback.  Called by
// RtlpLookupDynamicFunctionEntry to locate RUNTIME_FUNCTION entry for a PC
// found within a portion of a heap that contains stub code.
//
T_RUNTIME_FUNCTION*
FindStubFunctionEntry (
   BIT64_ONLY(IN ULONG64    ControlPc)
    NOT_BIT64(IN ULONG      ControlPc),
              IN PVOID      Context
    )
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    CONSISTENCY_CHECK(DYNFNTABLE_STUB == IdentifyDynamicFunctionTableTypeFromContext(Context));

    StubUnwindInfoHeapSegment *pStubHeapSegment = (StubUnwindInfoHeapSegment*)DecodeDynamicFunctionTableContext(Context);

    //
    // The RUNTIME_FUNCTION entry contains ULONG offsets relative to the
    // segment base.  Stub::EmitUnwindInfo ensures that this cast is valid.
    //
    ULONG RelativeAddress = (ULONG)((BYTE*)ControlPc - pStubHeapSegment->pbBaseAddress);

    LOG((LF_STUBS, LL_INFO100000, "ControlPc %p, RelativeAddress 0x%x, pStubHeapSegment %p, pStubHeapSegment->pbBaseAddress %p\n",
            ControlPc,
            RelativeAddress,
            pStubHeapSegment,
            pStubHeapSegment->pbBaseAddress));

    //
    // Search this segment's list of stubs for an entry that includes the
    // segment-relative offset.
    //
    for (StubUnwindInfoHeader *pHeader = pStubHeapSegment->pUnwindHeaderList;
         pHeader;
         pHeader = pHeader->pNext)
    {
        // The entry points are in increasing address order.
        if (RelativeAddress >= RUNTIME_FUNCTION__BeginAddress(&pHeader->FunctionEntry))
        {
            T_RUNTIME_FUNCTION *pCurFunction = &pHeader->FunctionEntry;
            T_RUNTIME_FUNCTION *pPrevFunction = NULL;

            LOG((LF_STUBS, LL_INFO100000, "pCurFunction %p, pCurFunction->BeginAddress 0x%x, pCurFunction->EndAddress 0x%x\n",
                    pCurFunction,
                    RUNTIME_FUNCTION__BeginAddress(pCurFunction),
                    RUNTIME_FUNCTION__EndAddress(pCurFunction, (TADDR)pStubHeapSegment->pbBaseAddress)));

            CONSISTENCY_CHECK((RUNTIME_FUNCTION__EndAddress(pCurFunction, (TADDR)pStubHeapSegment->pbBaseAddress) > RUNTIME_FUNCTION__BeginAddress(pCurFunction)));
            CONSISTENCY_CHECK((!pPrevFunction || RUNTIME_FUNCTION__EndAddress(pPrevFunction, (TADDR)pStubHeapSegment->pbBaseAddress) <= RUNTIME_FUNCTION__BeginAddress(pCurFunction)));

            // The entry points are in increasing address order.  They're
            // also contiguous, so after we're sure it's after the start of
            // the first function (checked above), we only need to test
            // the end address.
            if (RelativeAddress < RUNTIME_FUNCTION__EndAddress(pCurFunction, (TADDR)pStubHeapSegment->pbBaseAddress))
            {
                CONSISTENCY_CHECK((RelativeAddress >= RUNTIME_FUNCTION__BeginAddress(pCurFunction)));

                return pCurFunction;
            }
        }
    }

    //
    // Return NULL to indicate that there is no RUNTIME_FUNCTION/unwind
    // information for this offset.
    //
    return NULL;
}


bool UnregisterUnwindInfoInLoaderHeapCallback (PVOID pvArgs, PVOID pvAllocationBase, SIZE_T cbReserved)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    //
    // There may be multiple StubUnwindInfoHeapSegment's associated with a region.
    //

    LOG((LF_STUBS, LL_INFO1000, "Looking for stub unwind info for LoaderHeap segment %p size %p\n", pvAllocationBase, cbReserved));

    CrstHolder crst(&g_StubUnwindInfoHeapSegmentsCrst);

    StubUnwindInfoHeapSegment *pStubHeapSegment;
    for (StubUnwindInfoHeapSegment **ppPrevStubHeapSegment = &g_StubHeapSegments;
            (pStubHeapSegment = *ppPrevStubHeapSegment); )
    {
        LOG((LF_STUBS, LL_INFO10000, "    have unwind info for address %p size %p\n", pStubHeapSegment->pbBaseAddress, pStubHeapSegment->cbSegment));

        // If heap region ends before stub segment
        if ((BYTE*)pvAllocationBase + cbReserved <= pStubHeapSegment->pbBaseAddress)
        {
            // The list is ordered, so address range is between segments
            break;
        }

        // The given heap segment base address may fall within a prereserved
        // region that was given to the heap when the heap was constructed, so
        // pvAllocationBase may be > pbBaseAddress.  Also, there could be
        // multiple segments for each heap region, so pvAllocationBase may be
        // < pbBaseAddress.  So...there is no meaningful relationship between
        // pvAllocationBase and pbBaseAddress.

        // If heap region starts before end of stub segment
        if ((BYTE*)pvAllocationBase < pStubHeapSegment->pbBaseAddress + pStubHeapSegment->cbSegment)
        {
            _ASSERTE((BYTE*)pvAllocationBase + cbReserved <= pStubHeapSegment->pbBaseAddress + pStubHeapSegment->cbSegment);

            DeleteEEFunctionTable(pStubHeapSegment);
#ifdef TARGET_AMD64
            if (pStubHeapSegment->pUnwindInfoTable != 0)
                delete pStubHeapSegment->pUnwindInfoTable;
#endif
            *ppPrevStubHeapSegment = pStubHeapSegment->pNext;

            delete pStubHeapSegment;
        }
        else
        {
            ppPrevStubHeapSegment = &pStubHeapSegment->pNext;
        }
    }

    return false; // Keep enumerating
}


VOID UnregisterUnwindInfoInLoaderHeap (UnlockedLoaderHeap *pHeap)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        PRECONDITION(pHeap->m_fPermitStubsWithUnwindInfo);
    }
    CONTRACTL_END;

    pHeap->EnumPageRegions(&UnregisterUnwindInfoInLoaderHeapCallback, NULL /* pvArgs */);

#ifdef _DEBUG
    pHeap->m_fStubUnwindInfoUnregistered = TRUE;
#endif // _DEBUG
}


class StubUnwindInfoSegmentBoundaryReservationList
{
    struct ReservationList
    {
        ReservationList *pNext;

        static ReservationList *FromStub (Stub *pStub)
        {
            return (ReservationList*)(pStub+1);
        }

        Stub *GetStub ()
        {
            return (Stub*)this - 1;
        }
    };

    ReservationList *m_pList;

public:

    StubUnwindInfoSegmentBoundaryReservationList ()
    {
        LIMITED_METHOD_CONTRACT;

        m_pList = NULL;
    }

    ~StubUnwindInfoSegmentBoundaryReservationList ()
    {
        LIMITED_METHOD_CONTRACT;

        ReservationList *pList = m_pList;
        while (pList)
        {
            ReservationList *pNext = pList->pNext;

            ExecutableWriterHolder<Stub> stubWriterHolder(pList->GetStub(), sizeof(Stub));
            stubWriterHolder.GetRW()->DecRef();

            pList = pNext;
        }
    }

    void AddStub (Stub *pStub)
    {
        LIMITED_METHOD_CONTRACT;

        ReservationList *pList = ReservationList::FromStub(pStub);

        ExecutableWriterHolder<ReservationList> listWriterHolder(pList, sizeof(ReservationList));
        listWriterHolder.GetRW()->pNext = m_pList;
        m_pList = pList;
    }
};


#endif // STUBLINKER_GENERATES_UNWIND_INFO


//************************************************************************
// StubLinker
//************************************************************************

//---------------------------------------------------------------
// Construction
//---------------------------------------------------------------
StubLinker::StubLinker()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    m_pCodeElements     = NULL;
    m_pFirstCodeLabel   = NULL;
    m_pFirstLabelRef    = NULL;
    m_pPatchLabel       = NULL;
    m_pTargetMethod     = NULL;
    m_stackSize         = 0;
    m_fDataOnly         = FALSE;
#ifdef TARGET_ARM
    m_fProlog           = FALSE;
    m_cCalleeSavedRegs  = 0;
    m_cbStackFrame      = 0;
    m_fPushArgRegs      = FALSE;
#endif
#ifdef STUBLINKER_GENERATES_UNWIND_INFO
#ifdef _DEBUG
    m_pUnwindInfoCheckLabel = NULL;
#endif
#ifdef TARGET_AMD64
    m_pUnwindInfoList   = NULL;
    m_nUnwindSlots      = 0;
    m_fHaveFramePointer = FALSE;
#endif
#ifdef TARGET_ARM64
    m_fProlog           = FALSE;
    m_cIntRegArgs       = 0;
    m_cVecRegArgs       = 0;
    m_cCalleeSavedRegs  = 0;
    m_cbStackSpace      = 0;
#endif
#endif // STUBLINKER_GENERATES_UNWIND_INFO
}



//---------------------------------------------------------------
// Append code bytes.
//---------------------------------------------------------------
VOID StubLinker::EmitBytes(const BYTE *pBytes, UINT numBytes)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CodeElement *pLastCodeElement = GetLastCodeElement();
    while (numBytes != 0) {

        if (pLastCodeElement != NULL &&
            pLastCodeElement->m_type == CodeElement::kCodeRun) {
            CodeRun *pCodeRun = (CodeRun*)pLastCodeElement;
            UINT numbytessrc  = numBytes;
            UINT numbytesdst  = CODERUNSIZE - pCodeRun->m_numcodebytes;
            if (numbytesdst <= numbytessrc) {
                CopyMemory(&(pCodeRun->m_codebytes[pCodeRun->m_numcodebytes]),
                           pBytes,
                           numbytesdst);
                pCodeRun->m_numcodebytes = CODERUNSIZE;
                pLastCodeElement = NULL;
                pBytes += numbytesdst;
                numBytes -= numbytesdst;
            } else {
                CopyMemory(&(pCodeRun->m_codebytes[pCodeRun->m_numcodebytes]),
                           pBytes,
                           numbytessrc);
                pCodeRun->m_numcodebytes += numbytessrc;
                pBytes += numbytessrc;
                numBytes = 0;
            }

        } else {
            pLastCodeElement = AppendNewEmptyCodeRun();
        }
    }
}


//---------------------------------------------------------------
// Append code bytes.
//---------------------------------------------------------------
VOID StubLinker::Emit8 (unsigned __int8  val)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CodeRun *pCodeRun = GetLastCodeRunIfAny();
    if (pCodeRun && (CODERUNSIZE - pCodeRun->m_numcodebytes) >= sizeof(val)) {
        *((unsigned __int8 *)(pCodeRun->m_codebytes + pCodeRun->m_numcodebytes)) = val;
        pCodeRun->m_numcodebytes += sizeof(val);
    } else {
        EmitBytes((BYTE*)&val, sizeof(val));
    }
}

//---------------------------------------------------------------
// Append code bytes.
//---------------------------------------------------------------
VOID StubLinker::Emit16(unsigned __int16 val)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CodeRun *pCodeRun = GetLastCodeRunIfAny();
    if (pCodeRun && (CODERUNSIZE - pCodeRun->m_numcodebytes) >= sizeof(val)) {
        SET_UNALIGNED_16(pCodeRun->m_codebytes + pCodeRun->m_numcodebytes, val);
        pCodeRun->m_numcodebytes += sizeof(val);
    } else {
        EmitBytes((BYTE*)&val, sizeof(val));
    }
}

//---------------------------------------------------------------
// Append code bytes.
//---------------------------------------------------------------
VOID StubLinker::Emit32(unsigned __int32 val)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CodeRun *pCodeRun = GetLastCodeRunIfAny();
    if (pCodeRun && (CODERUNSIZE - pCodeRun->m_numcodebytes) >= sizeof(val)) {
        SET_UNALIGNED_32(pCodeRun->m_codebytes + pCodeRun->m_numcodebytes,  val);
        pCodeRun->m_numcodebytes += sizeof(val);
    } else {
        EmitBytes((BYTE*)&val, sizeof(val));
    }
}

//---------------------------------------------------------------
// Append code bytes.
//---------------------------------------------------------------
VOID StubLinker::Emit64(unsigned __int64 val)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CodeRun *pCodeRun = GetLastCodeRunIfAny();
    if (pCodeRun && (CODERUNSIZE - pCodeRun->m_numcodebytes) >= sizeof(val)) {
        SET_UNALIGNED_64(pCodeRun->m_codebytes + pCodeRun->m_numcodebytes, val);
        pCodeRun->m_numcodebytes += sizeof(val);
    } else {
        EmitBytes((BYTE*)&val, sizeof(val));
    }
}

//---------------------------------------------------------------
// Append pointer value.
//---------------------------------------------------------------
VOID StubLinker::EmitPtr(const VOID *val)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CodeRun *pCodeRun = GetLastCodeRunIfAny();
    if (pCodeRun && (CODERUNSIZE - pCodeRun->m_numcodebytes) >= sizeof(val)) {
        SET_UNALIGNED_PTR(pCodeRun->m_codebytes + pCodeRun->m_numcodebytes, (UINT_PTR)val);
        pCodeRun->m_numcodebytes += sizeof(val);
    } else {
        EmitBytes((BYTE*)&val, sizeof(val));
    }
}


//---------------------------------------------------------------
// Create a new undefined label. Label must be assigned to a code
// location using EmitLabel() prior to final linking.
// Throws COM+ exception on failure.
//---------------------------------------------------------------
CodeLabel* StubLinker::NewCodeLabel()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CodeLabel *pCodeLabel = (CodeLabel*)(m_quickHeap.Alloc(sizeof(CodeLabel)));
    _ASSERTE(pCodeLabel); // QuickHeap throws exceptions rather than returning NULL
    pCodeLabel->m_next       = m_pFirstCodeLabel;
    pCodeLabel->m_fExternal  = FALSE;
    pCodeLabel->m_fAbsolute = FALSE;
    pCodeLabel->i.m_pCodeRun = NULL;
    m_pFirstCodeLabel = pCodeLabel;
    return pCodeLabel;


}

CodeLabel* StubLinker::NewAbsoluteCodeLabel()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CodeLabel *pCodeLabel = NewCodeLabel();
    pCodeLabel->m_fAbsolute = TRUE;
    return pCodeLabel;
}


//---------------------------------------------------------------
// Sets the label to point to the current "instruction pointer".
// It is invalid to call EmitLabel() twice on
// the same label.
//---------------------------------------------------------------
VOID StubLinker::EmitLabel(CodeLabel* pCodeLabel)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(!(pCodeLabel->m_fExternal));       //can't emit an external label
    _ASSERTE(pCodeLabel->i.m_pCodeRun == NULL);  //must only emit label once
    CodeRun *pLastCodeRun = GetLastCodeRunIfAny();
    if (!pLastCodeRun) {
        pLastCodeRun = AppendNewEmptyCodeRun();
    }
    pCodeLabel->i.m_pCodeRun    = pLastCodeRun;
    pCodeLabel->i.m_localOffset = pLastCodeRun->m_numcodebytes;
}


//---------------------------------------------------------------
// Combines NewCodeLabel() and EmitLabel() for convenience.
// Throws COM+ exception on failure.
//---------------------------------------------------------------
CodeLabel* StubLinker::EmitNewCodeLabel()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CodeLabel* label = NewCodeLabel();
    EmitLabel(label);
    return label;
}


//---------------------------------------------------------------
// Creates & emits the patch offset label for the stub
//---------------------------------------------------------------
VOID StubLinker::EmitPatchLabel()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    //
    // Note that it's OK to have re-emit the patch label,
    // just use the later one.
    //

    m_pPatchLabel = EmitNewCodeLabel();
}

//---------------------------------------------------------------
// Returns final location of label as an offset from the start
// of the stub. Can only be called after linkage.
//---------------------------------------------------------------
UINT32 StubLinker::GetLabelOffset(CodeLabel *pLabel)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(!(pLabel->m_fExternal));
    return pLabel->i.m_localOffset + pLabel->i.m_pCodeRun->m_globaloffset;
}


//---------------------------------------------------------------
// Create a new label to an external address.
// Throws COM+ exception on failure.
//---------------------------------------------------------------
CodeLabel* StubLinker::NewExternalCodeLabel(LPVOID pExternalAddress)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;

        PRECONDITION(CheckPointer(pExternalAddress));
    }
    CONTRACTL_END;

    CodeLabel *pCodeLabel = (CodeLabel*)(m_quickHeap.Alloc(sizeof(CodeLabel)));
    _ASSERTE(pCodeLabel); // QuickHeap throws exceptions rather than returning NULL
    pCodeLabel->m_next       = m_pFirstCodeLabel;
    pCodeLabel->m_fExternal          = TRUE;
    pCodeLabel->m_fAbsolute  = FALSE;
    pCodeLabel->e.m_pExternalAddress = pExternalAddress;
    m_pFirstCodeLabel = pCodeLabel;
    return pCodeLabel;
}

//---------------------------------------------------------------
// Set the target method for Instantiating stubs.
//---------------------------------------------------------------
void StubLinker::SetTargetMethod(PTR_MethodDesc pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(pMD != NULL);
    }
    CONTRACTL_END;
    m_pTargetMethod = pMD;
}


//---------------------------------------------------------------
// Append an instruction containing a reference to a label.
//
//      target          - the label being referenced.
//      instructionFormat         - a platform-specific InstructionFormat object
//                        that gives properties about the reference.
//      variationCode   - uninterpreted data passed to the pInstructionFormat methods.
//---------------------------------------------------------------
VOID StubLinker::EmitLabelRef(CodeLabel* target, const InstructionFormat & instructionFormat, UINT variationCode)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LabelRef *pLabelRef = (LabelRef *)(m_quickHeap.Alloc(sizeof(LabelRef)));
    _ASSERTE(pLabelRef);      // m_quickHeap throws an exception rather than returning NULL
    pLabelRef->m_type               = LabelRef::kLabelRef;
    pLabelRef->m_pInstructionFormat = (InstructionFormat*)&instructionFormat;
    pLabelRef->m_variationCode      = variationCode;
    pLabelRef->m_target             = target;

    pLabelRef->m_nextLabelRef = m_pFirstLabelRef;
    m_pFirstLabelRef = pLabelRef;

    AppendCodeElement(pLabelRef);


}





//---------------------------------------------------------------
// Internal helper routine.
//---------------------------------------------------------------
CodeRun *StubLinker::GetLastCodeRunIfAny()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CodeElement *pLastCodeElem = GetLastCodeElement();
    if (pLastCodeElem == NULL || pLastCodeElem->m_type != CodeElement::kCodeRun) {
        return NULL;
    } else {
        return (CodeRun*)pLastCodeElem;
    }
}


//---------------------------------------------------------------
// Internal helper routine.
//---------------------------------------------------------------
CodeRun *StubLinker::AppendNewEmptyCodeRun()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CodeRun *pNewCodeRun = (CodeRun*)(m_quickHeap.Alloc(sizeof(CodeRun)));
    _ASSERTE(pNewCodeRun); // QuickHeap throws exceptions rather than returning NULL
    pNewCodeRun->m_type = CodeElement::kCodeRun;
    pNewCodeRun->m_numcodebytes = 0;
    AppendCodeElement(pNewCodeRun);
    return pNewCodeRun;

}

//---------------------------------------------------------------
// Internal helper routine.
//---------------------------------------------------------------
VOID StubLinker::AppendCodeElement(CodeElement *pCodeElement)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    pCodeElement->m_next = m_pCodeElements;
    m_pCodeElements = pCodeElement;
}



//---------------------------------------------------------------
// Is the current LabelRef's size big enough to reach the target?
//---------------------------------------------------------------
static BOOL LabelCanReach(LabelRef *pLabelRef)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    InstructionFormat *pIF  = pLabelRef->m_pInstructionFormat;

    if (pLabelRef->m_target->m_fExternal)
    {
        return pLabelRef->m_pInstructionFormat->CanReach(
                pLabelRef->m_refsize, pLabelRef->m_variationCode, TRUE, (INT_PTR)pLabelRef->m_target->e.m_pExternalAddress);
    }
    else
    {
        UINT targetglobaloffset = pLabelRef->m_target->i.m_pCodeRun->m_globaloffset +
                                  pLabelRef->m_target->i.m_localOffset;
        UINT srcglobaloffset = pLabelRef->m_globaloffset +
                               pIF->GetHotSpotOffset(pLabelRef->m_refsize,
                                                     pLabelRef->m_variationCode);
        INT offset = (INT)(targetglobaloffset - srcglobaloffset);

        return pLabelRef->m_pInstructionFormat->CanReach(
            pLabelRef->m_refsize, pLabelRef->m_variationCode, FALSE, offset);
    }
}

//---------------------------------------------------------------
// Generate the actual stub. The returned stub has a refcount of 1.
// No other methods (other than the destructor) should be called
// after calling Link().
//
// Throws COM+ exception on failure.
//---------------------------------------------------------------
Stub *StubLinker::Link(LoaderHeap *pHeap, DWORD flags)
{
    STANDARD_VM_CONTRACT;

    int globalsize = 0;
    int size = CalculateSize(&globalsize);

    _ASSERTE(!pHeap || pHeap->IsExecutable());

    StubHolder<Stub> pStub;

#ifdef STUBLINKER_GENERATES_UNWIND_INFO
    StubUnwindInfoSegmentBoundaryReservationList ReservedStubs;

    for (;;)
#endif
    {
        pStub = Stub::NewStub(
                pHeap,
                size,
                flags
#ifdef STUBLINKER_GENERATES_UNWIND_INFO
                , UnwindInfoSize(globalsize)
#endif
                );
        ASSERT(pStub != NULL);

        bool fSuccess = EmitStub(pStub, globalsize, size, pHeap);

#ifdef STUBLINKER_GENERATES_UNWIND_INFO
        if (fSuccess)
        {
            break;
        }
        else
        {
            ReservedStubs.AddStub(pStub);
            pStub.SuppressRelease();
        }
#else
        CONSISTENCY_CHECK_MSG(fSuccess, ("EmitStub should always return true"));
#endif
    }

    return pStub.Extract();
}

int StubLinker::CalculateSize(int* pGlobalSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(pGlobalSize);

#if defined(_DEBUG) && defined(STUBLINKER_GENERATES_UNWIND_INFO)
    if (m_pUnwindInfoCheckLabel)
    {
        EmitLabel(m_pUnwindInfoCheckLabel);
        EmitUnwindInfoCheckSubfunction();
        m_pUnwindInfoCheckLabel = NULL;
    }
#endif

#ifdef _DEBUG
    // Don't want any undefined labels
    for (CodeLabel *pCodeLabel = m_pFirstCodeLabel;
         pCodeLabel != NULL;
         pCodeLabel = pCodeLabel->m_next) {
        if ((!(pCodeLabel->m_fExternal)) && pCodeLabel->i.m_pCodeRun == NULL) {
            _ASSERTE(!"Forgot to define a label before asking StubLinker to link.");
        }
    }
#endif //_DEBUG

    //-------------------------------------------------------------------
    // Tentatively set all of the labelref sizes to their smallest possible
    // value.
    //-------------------------------------------------------------------
    for (LabelRef *pLabelRef = m_pFirstLabelRef;
         pLabelRef != NULL;
         pLabelRef = pLabelRef->m_nextLabelRef) {

        for (UINT bitmask = 1; bitmask <= InstructionFormat::kMax; bitmask = bitmask << 1) {
            if (pLabelRef->m_pInstructionFormat->m_allowedSizes & bitmask) {
                pLabelRef->m_refsize = bitmask;
                break;
            }
        }

    }

    UINT globalsize;
    UINT datasize;
    BOOL fSomethingChanged;
    do {
        fSomethingChanged = FALSE;


        // Layout each code element.
        globalsize = 0;
        datasize = 0;
        CodeElement *pCodeElem;
        for (pCodeElem = m_pCodeElements; pCodeElem; pCodeElem = pCodeElem->m_next) {

            switch (pCodeElem->m_type) {
                case CodeElement::kCodeRun:
                    globalsize += ((CodeRun*)pCodeElem)->m_numcodebytes;
                    break;

                case CodeElement::kLabelRef: {
                    LabelRef *pLabelRef = (LabelRef*)pCodeElem;
                    globalsize += pLabelRef->m_pInstructionFormat->GetSizeOfInstruction( pLabelRef->m_refsize,
                                                                                         pLabelRef->m_variationCode );
                    datasize += pLabelRef->m_pInstructionFormat->GetSizeOfData( pLabelRef->m_refsize,
                                                                                         pLabelRef->m_variationCode );
                    }
                    break;

                default:
                    _ASSERTE(0);
            }

            // Record a temporary global offset; this is actually
            // wrong by a fixed value. We'll fix up after we know the
            // size of the entire stub.
            pCodeElem->m_globaloffset = 0 - globalsize;

            // also record the data offset. Note the link-list we walk is in
            // *reverse* order so we visit the last instruction first
            // so what we record now is in fact the offset from the *end* of
            // the data block. We fix it up later.
            pCodeElem->m_dataoffset = 0 - datasize;
        }

        // Now fix up the global offsets.
        for (pCodeElem = m_pCodeElements; pCodeElem; pCodeElem = pCodeElem->m_next) {
            pCodeElem->m_globaloffset += globalsize;
            pCodeElem->m_dataoffset += datasize;
        }


        // Now, iterate thru the LabelRef's and check if any of them
        // have to be resized.
        for (LabelRef *pLabelRef = m_pFirstLabelRef;
             pLabelRef != NULL;
             pLabelRef = pLabelRef->m_nextLabelRef) {


            if (!LabelCanReach(pLabelRef)) {
                fSomethingChanged = TRUE;

                UINT bitmask = pLabelRef->m_refsize << 1;
                // Find the next largest size.
                // (we could be smarter about this and eliminate intermediate
                // sizes based on the tentative offset.)
                for (; bitmask <= InstructionFormat::kMax; bitmask = bitmask << 1) {
                    if (pLabelRef->m_pInstructionFormat->m_allowedSizes & bitmask) {
                        pLabelRef->m_refsize = bitmask;
                        break;
                    }
                }
#ifdef _DEBUG
                if (bitmask > InstructionFormat::kMax) {
                    // CANNOT REACH target even with kMax
                    _ASSERTE(!"Stub instruction cannot reach target: must choose a different instruction!");
                }
#endif
            }
        }


    } while (fSomethingChanged); // Keep iterating until all LabelRef's can reach


    // We now have the correct layout write out the stub.

    // Compute stub code+data size after aligning data correctly
    if(globalsize % DATA_ALIGNMENT)
        globalsize += (DATA_ALIGNMENT - (globalsize % DATA_ALIGNMENT));

    *pGlobalSize = globalsize;
    return globalsize + datasize;
}

bool StubLinker::EmitStub(Stub* pStub, int globalsize, int totalSize, LoaderHeap* pHeap)
{
    STANDARD_VM_CONTRACT;

    BYTE *pCode = (BYTE*)(pStub->GetBlob());

    ExecutableWriterHolder<Stub> stubWriterHolder(pStub, sizeof(Stub) + totalSize);
    Stub *pStubRW = stubWriterHolder.GetRW();

    BYTE *pCodeRW = (BYTE*)(pStubRW->GetBlob());
    BYTE *pDataRW = pCodeRW+globalsize; // start of data area
    {
        int lastCodeOffset = 0;

        // Write out each code element.
        for (CodeElement* pCodeElem = m_pCodeElements; pCodeElem; pCodeElem = pCodeElem->m_next) {
            int currOffset = 0;

            switch (pCodeElem->m_type) {
                case CodeElement::kCodeRun:
                    CopyMemory(pCodeRW + pCodeElem->m_globaloffset,
                               ((CodeRun*)pCodeElem)->m_codebytes,
                               ((CodeRun*)pCodeElem)->m_numcodebytes);
                    currOffset = pCodeElem->m_globaloffset + ((CodeRun *)pCodeElem)->m_numcodebytes;
                    break;

                case CodeElement::kLabelRef: {
                    LabelRef *pLabelRef = (LabelRef*)pCodeElem;
                    InstructionFormat *pIF  = pLabelRef->m_pInstructionFormat;
                    __int64 fixupval;

                    LPBYTE srcglobaladdr = pCode +
                                           pLabelRef->m_globaloffset +
                                           pIF->GetHotSpotOffset(pLabelRef->m_refsize,
                                                                 pLabelRef->m_variationCode);
                    LPBYTE targetglobaladdr;
                    if (!(pLabelRef->m_target->m_fExternal)) {
                        targetglobaladdr = pCode +
                                           pLabelRef->m_target->i.m_pCodeRun->m_globaloffset +
                                           pLabelRef->m_target->i.m_localOffset;
                    } else {
                        targetglobaladdr = (LPBYTE)(pLabelRef->m_target->e.m_pExternalAddress);
                    }
                    if ((pLabelRef->m_target->m_fAbsolute)) {
                        fixupval = (__int64)(size_t)targetglobaladdr;
                    } else
                        fixupval = (__int64)(targetglobaladdr - srcglobaladdr);

                    pLabelRef->m_pInstructionFormat->EmitInstruction(
                        pLabelRef->m_refsize,
                        fixupval,
                        pCode + pCodeElem->m_globaloffset,
                        pCodeRW + pCodeElem->m_globaloffset,
                        pLabelRef->m_variationCode,
                        pDataRW + pCodeElem->m_dataoffset);

                    currOffset =
                        pCodeElem->m_globaloffset +
                        pLabelRef->m_pInstructionFormat->GetSizeOfInstruction( pLabelRef->m_refsize,
                                                                               pLabelRef->m_variationCode );
                    }
                    break;

                default:
                    _ASSERTE(0);
            }
            lastCodeOffset = (currOffset > lastCodeOffset) ? currOffset : lastCodeOffset;
        }

        // Fill in zeros at the end, if necessary
        if (lastCodeOffset < globalsize)
            ZeroMemory(pCodeRW + lastCodeOffset, globalsize - lastCodeOffset);
    }

    // Set additional stub data.
    // - Fill in the target method for the Instantiating stub.
    //
    // - Fill in patch offset, if we have one
    //      Note that these offsets are relative to the start of the stub,
    //      not the code, so you'll have to add sizeof(Stub) to get to the
    //      right spot.
    if (pStubRW->IsInstantiatingStub())
    {
        _ASSERTE(m_pTargetMethod != NULL);
        _ASSERTE(m_pPatchLabel == NULL);
        pStubRW->SetInstantiatedMethodDesc(m_pTargetMethod);

        LOG((LF_CORDB, LL_INFO100, "SL::ES: InstantiatedMethod fd:0x%x\n",
            pStub->GetInstantiatedMethodDesc()));
    }
    else if (m_pPatchLabel != NULL)
    {
        UINT32 uLabelOffset = GetLabelOffset(m_pPatchLabel);
        _ASSERTE(FitsIn<USHORT>(uLabelOffset));
        pStubRW->SetPatchOffset(static_cast<USHORT>(uLabelOffset));

        LOG((LF_CORDB, LL_INFO100, "SL::ES: patch offset:0x%x\n",
            pStub->GetPatchOffset()));
    }

#ifdef STUBLINKER_GENERATES_UNWIND_INFO
    if (pStub->HasUnwindInfo())
    {
        if (!EmitUnwindInfo(pStub, pStubRW, globalsize, pHeap))
            return false;
    }
#endif // STUBLINKER_GENERATES_UNWIND_INFO

    if (!m_fDataOnly)
    {
        FlushInstructionCache(GetCurrentProcess(), pCode, globalsize);
    }

    _ASSERTE(m_fDataOnly || DbgIsExecutable(pCode, globalsize));

    return true;
}


#ifdef STUBLINKER_GENERATES_UNWIND_INFO
#if defined(TARGET_AMD64)

// See RtlVirtualUnwind in base\ntos\rtl\amd64\exdsptch.c

static_assert_no_msg(kRAX == (FIELD_OFFSET(CONTEXT, Rax) - FIELD_OFFSET(CONTEXT, Rax)) / sizeof(ULONG64));
static_assert_no_msg(kRCX == (FIELD_OFFSET(CONTEXT, Rcx) - FIELD_OFFSET(CONTEXT, Rax)) / sizeof(ULONG64));
static_assert_no_msg(kRDX == (FIELD_OFFSET(CONTEXT, Rdx) - FIELD_OFFSET(CONTEXT, Rax)) / sizeof(ULONG64));
static_assert_no_msg(kRBX == (FIELD_OFFSET(CONTEXT, Rbx) - FIELD_OFFSET(CONTEXT, Rax)) / sizeof(ULONG64));
static_assert_no_msg(kRBP == (FIELD_OFFSET(CONTEXT, Rbp) - FIELD_OFFSET(CONTEXT, Rax)) / sizeof(ULONG64));
static_assert_no_msg(kRSI == (FIELD_OFFSET(CONTEXT, Rsi) - FIELD_OFFSET(CONTEXT, Rax)) / sizeof(ULONG64));
static_assert_no_msg(kRDI == (FIELD_OFFSET(CONTEXT, Rdi) - FIELD_OFFSET(CONTEXT, Rax)) / sizeof(ULONG64));
static_assert_no_msg(kR8  == (FIELD_OFFSET(CONTEXT, R8 ) - FIELD_OFFSET(CONTEXT, Rax)) / sizeof(ULONG64));
static_assert_no_msg(kR9  == (FIELD_OFFSET(CONTEXT, R9 ) - FIELD_OFFSET(CONTEXT, Rax)) / sizeof(ULONG64));
static_assert_no_msg(kR10 == (FIELD_OFFSET(CONTEXT, R10) - FIELD_OFFSET(CONTEXT, Rax)) / sizeof(ULONG64));
static_assert_no_msg(kR11 == (FIELD_OFFSET(CONTEXT, R11) - FIELD_OFFSET(CONTEXT, Rax)) / sizeof(ULONG64));
static_assert_no_msg(kR12 == (FIELD_OFFSET(CONTEXT, R12) - FIELD_OFFSET(CONTEXT, Rax)) / sizeof(ULONG64));
static_assert_no_msg(kR13 == (FIELD_OFFSET(CONTEXT, R13) - FIELD_OFFSET(CONTEXT, Rax)) / sizeof(ULONG64));
static_assert_no_msg(kR14 == (FIELD_OFFSET(CONTEXT, R14) - FIELD_OFFSET(CONTEXT, Rax)) / sizeof(ULONG64));
static_assert_no_msg(kR15 == (FIELD_OFFSET(CONTEXT, R15) - FIELD_OFFSET(CONTEXT, Rax)) / sizeof(ULONG64));

VOID StubLinker::UnwindSavedReg (UCHAR reg, ULONG SPRelativeOffset)
{
    USHORT FrameOffset = (USHORT)(SPRelativeOffset / 8);

    if ((ULONG)FrameOffset == SPRelativeOffset)
    {
        UNWIND_CODE *pUnwindCode = AllocUnwindInfo(UWOP_SAVE_NONVOL);
        pUnwindCode->OpInfo = reg;
        pUnwindCode[1].FrameOffset = FrameOffset;
    }
    else
    {
        UNWIND_CODE *pUnwindCode = AllocUnwindInfo(UWOP_SAVE_NONVOL_FAR);
        pUnwindCode->OpInfo = reg;
        pUnwindCode[1].FrameOffset = (USHORT)SPRelativeOffset;
        pUnwindCode[2].FrameOffset = (USHORT)(SPRelativeOffset >> 16);
    }
}

VOID StubLinker::UnwindPushedReg (UCHAR reg)
{
    m_stackSize += sizeof(void*);

    if (m_fHaveFramePointer)
        return;

    UNWIND_CODE *pUnwindCode = AllocUnwindInfo(UWOP_PUSH_NONVOL);
    pUnwindCode->OpInfo = reg;
}

VOID StubLinker::UnwindAllocStack (SHORT FrameSizeIncrement)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (! ClrSafeInt<SHORT>::addition(m_stackSize, FrameSizeIncrement, m_stackSize))
        COMPlusThrowArithmetic();

    if (m_fHaveFramePointer)
        return;

    UCHAR OpInfo = (UCHAR)((FrameSizeIncrement - 8) / 8);

    if (OpInfo*8 + 8 == FrameSizeIncrement)
    {
        UNWIND_CODE *pUnwindCode = AllocUnwindInfo(UWOP_ALLOC_SMALL);
        pUnwindCode->OpInfo = OpInfo;
    }
    else
    {
        USHORT FrameOffset = (USHORT)FrameSizeIncrement;
        bool fNeedExtraSlot = ((ULONG)FrameOffset != (ULONG)FrameSizeIncrement);

        UNWIND_CODE *pUnwindCode = AllocUnwindInfo(UWOP_ALLOC_LARGE, fNeedExtraSlot ? 1 : 0);

        pUnwindCode->OpInfo = fNeedExtraSlot ? 1 : 0;

        pUnwindCode[1].FrameOffset = FrameOffset;

        if (fNeedExtraSlot)
            pUnwindCode[2].FrameOffset = (USHORT)(FrameSizeIncrement >> 16);
    }
}

VOID StubLinker::UnwindSetFramePointer (UCHAR reg)
{
    _ASSERTE(!m_fHaveFramePointer);

    UNWIND_CODE *pUnwindCode = AllocUnwindInfo(UWOP_SET_FPREG);
    pUnwindCode->OpInfo = reg;

    m_fHaveFramePointer = TRUE;
}

UNWIND_CODE *StubLinker::AllocUnwindInfo (UCHAR Op, UCHAR nExtraSlots /*= 0*/)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE(Op < sizeof(UnwindOpExtraSlotTable));

    UCHAR nSlotsAlloc = UnwindOpExtraSlotTable[Op] + nExtraSlots;

    IntermediateUnwindInfo *pUnwindInfo = (IntermediateUnwindInfo*)m_quickHeap.Alloc(  sizeof(IntermediateUnwindInfo)
                                                                                     + nSlotsAlloc * sizeof(UNWIND_CODE));
    m_nUnwindSlots += 1 + nSlotsAlloc;

    pUnwindInfo->pNext = m_pUnwindInfoList;
                         m_pUnwindInfoList = pUnwindInfo;

    UNWIND_CODE *pUnwindCode = &pUnwindInfo->rgUnwindCode[0];

    pUnwindCode->UnwindOp = Op;

    CodeRun *pCodeRun = GetLastCodeRunIfAny();
    _ASSERTE(pCodeRun != NULL);

    pUnwindInfo->pCodeRun = pCodeRun;
    pUnwindInfo->LocalOffset = pCodeRun->m_numcodebytes;

    EmitUnwindInfoCheck();

    return pUnwindCode;
}
#endif // defined(TARGET_AMD64)

struct FindBlockArgs
{
    BYTE *pCode;
    BYTE *pBlockBase;
    SIZE_T cbBlockSize;
};

bool FindBlockCallback (PTR_VOID pvArgs, PTR_VOID pvAllocationBase, SIZE_T cbReserved)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    FindBlockArgs* pArgs = (FindBlockArgs*)pvArgs;
    if (pArgs->pCode >= pvAllocationBase && (pArgs->pCode < ((BYTE *)pvAllocationBase + cbReserved)))
    {
        pArgs->pBlockBase = (BYTE*)pvAllocationBase;
        pArgs->cbBlockSize = cbReserved;
        return true;
    }

    return false;
}

bool StubLinker::EmitUnwindInfo(Stub* pStubRX, Stub* pStubRW, int globalsize, LoaderHeap* pHeap)
{
    STANDARD_VM_CONTRACT;

    BYTE *pCode = (BYTE*)(pStubRX->GetEntryPoint());

    //
    // Determine the lower bound of the address space containing the stub.
    //

    FindBlockArgs findBlockArgs;
    findBlockArgs.pCode = pCode;
    findBlockArgs.pBlockBase = NULL;

    pHeap->EnumPageRegions(&FindBlockCallback, &findBlockArgs);

    if (findBlockArgs.pBlockBase == NULL)
    {
        // REVISIT_TODO better exception
        COMPlusThrowOM();
    }

    BYTE *pbRegionBaseAddress = findBlockArgs.pBlockBase;

#ifdef _DEBUG
    static SIZE_T MaxSegmentSize = -1;
    if (MaxSegmentSize == (SIZE_T)-1)
        MaxSegmentSize = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MaxStubUnwindInfoSegmentSize, DYNAMIC_FUNCTION_TABLE_MAX_RANGE);
#else
    const SIZE_T MaxSegmentSize = DYNAMIC_FUNCTION_TABLE_MAX_RANGE;
#endif

    //
    // The RUNTIME_FUNCTION offsets are ULONGs.  If the region size is >
    // UINT32_MAX, then we'll shift the base address to the next 4gb and
    // register a separate function table.
    //
    // But...RtlInstallFunctionTableCallback has a 2gb restriction...so
    // make that INT32_MAX.
    //

    StubUnwindInfoHeader *pHeader = pStubRW->GetUnwindInfoHeader();
    _ASSERTE(IS_ALIGNED(pHeader, sizeof(void*)));

    BYTE *pbBaseAddress = pbRegionBaseAddress;

    while ((size_t)((BYTE*)pHeader - pbBaseAddress) > MaxSegmentSize)
    {
        pbBaseAddress += MaxSegmentSize;
    }

    //
    // If the unwind info/code straddle a 2gb boundary, then we're stuck.
    // Rather than add a lot more bit twiddling code to deal with this
    // exceptionally rare case, we'll signal the caller to keep this allocation
    // temporarily and allocate another.  This repeats until we eventually get
    // an allocation that doesn't straddle a 2gb boundary.  Afterwards the old
    // allocations are freed.
    //

    if ((size_t)(pCode + globalsize - pbBaseAddress) > MaxSegmentSize)
    {
        return false;
    }

    // Ensure that the first RUNTIME_FUNCTION struct ends up pointer aligned,
    // so that the StubUnwindInfoHeader struct is aligned.  UNWIND_INFO
    // includes one UNWIND_CODE.
    _ASSERTE(IS_ALIGNED(pStubRX, sizeof(void*)));
    _ASSERTE(0 == (FIELD_OFFSET(StubUnwindInfoHeader, FunctionEntry) % sizeof(void*)));

    StubUnwindInfoHeader * pUnwindInfoHeader = pStubRW->GetUnwindInfoHeader();

#ifdef TARGET_AMD64

    UNWIND_CODE *pDestUnwindCode = &pUnwindInfoHeader->UnwindInfo.UnwindCode[0];
#ifdef _DEBUG
    UNWIND_CODE *pDestUnwindCodeLimit = (UNWIND_CODE*)pStubRW->GetUnwindInfoHeaderSuffix();
#endif

    UINT FrameRegister = 0;

    //
    // Resolve the unwind operation offsets, and fill in the UNWIND_INFO and
    // RUNTIME_FUNCTION structs preceeding the stub.  The unwind codes are recorded
    // in decreasing address order.
    //

    for (IntermediateUnwindInfo *pUnwindInfoList = m_pUnwindInfoList; pUnwindInfoList != NULL; pUnwindInfoList = pUnwindInfoList->pNext)
    {
        UNWIND_CODE *pUnwindCode = &pUnwindInfoList->rgUnwindCode[0];
        UCHAR op = pUnwindCode[0].UnwindOp;

        if (UWOP_SET_FPREG == op)
        {
            FrameRegister = pUnwindCode[0].OpInfo;
        }

        //
        // Compute number of slots used by this encoding.
        //

        UINT nSlots;

        if (UWOP_ALLOC_LARGE == op)
        {
            nSlots = 2 + pUnwindCode[0].OpInfo;
        }
        else
        {
            _ASSERTE(UnwindOpExtraSlotTable[op] != (UCHAR)-1);
            nSlots = 1 + UnwindOpExtraSlotTable[op];
        }

        //
        // Compute offset and ensure that it will fit in the encoding.
        //

        SIZE_T CodeOffset =   pUnwindInfoList->pCodeRun->m_globaloffset
                            + pUnwindInfoList->LocalOffset;

        if (CodeOffset != (SIZE_T)(UCHAR)CodeOffset)
        {
            // REVISIT_TODO better exception
            COMPlusThrowOM();
        }

        //
        // Copy the encoding data, overwrite the new offset, and advance
        // to the next encoding.
        //

        _ASSERTE(pDestUnwindCode + nSlots <= pDestUnwindCodeLimit);

        CopyMemory(pDestUnwindCode, pUnwindCode, nSlots * sizeof(UNWIND_CODE));

        pDestUnwindCode->CodeOffset = (UCHAR)CodeOffset;

        pDestUnwindCode += nSlots;
    }

    //
    // Fill in the UNWIND_INFO struct
    //
    UNWIND_INFO *pUnwindInfo = &pUnwindInfoHeader->UnwindInfo;
    _ASSERTE(IS_ALIGNED(pUnwindInfo, sizeof(ULONG)));

    // PrologueSize may be 0 if all unwind directives at offset 0.
    SIZE_T PrologueSize =   m_pUnwindInfoList->pCodeRun->m_globaloffset
                            + m_pUnwindInfoList->LocalOffset;

    UINT nEntryPointSlots = m_nUnwindSlots;

    if (   PrologueSize != (SIZE_T)(UCHAR)PrologueSize
        || nEntryPointSlots > UCHAR_MAX)
    {
        // REVISIT_TODO better exception
        COMPlusThrowOM();
    }

    _ASSERTE(nEntryPointSlots);

    pUnwindInfo->Version = 1;
    pUnwindInfo->Flags = 0;
    pUnwindInfo->SizeOfProlog = (UCHAR)PrologueSize;
    pUnwindInfo->CountOfUnwindCodes = (UCHAR)nEntryPointSlots;
    pUnwindInfo->FrameRegister = FrameRegister;
    pUnwindInfo->FrameOffset = 0;

    //
    // Fill in the RUNTIME_FUNCTION struct for this prologue.
    //
    PT_RUNTIME_FUNCTION pCurFunction = &pUnwindInfoHeader->FunctionEntry;
    _ASSERTE(IS_ALIGNED(pCurFunction, sizeof(ULONG)));

    S_UINT32 sBeginAddress = S_BYTEPTR(pCode) - S_BYTEPTR(pbBaseAddress);
    if (sBeginAddress.IsOverflow())
        COMPlusThrowArithmetic();
    pCurFunction->BeginAddress = sBeginAddress.Value();

    S_UINT32 sEndAddress = S_BYTEPTR(pCode) + S_BYTEPTR(globalsize) - S_BYTEPTR(pbBaseAddress);
    if (sEndAddress.IsOverflow())
        COMPlusThrowArithmetic();
    pCurFunction->EndAddress = sEndAddress.Value();

    S_UINT32 sTemp = S_BYTEPTR(pUnwindInfo) - S_BYTEPTR(pbBaseAddress);
    if (sTemp.IsOverflow())
        COMPlusThrowArithmetic();
    RUNTIME_FUNCTION__SetUnwindInfoAddress(pCurFunction, sTemp.Value());
#elif defined(TARGET_ARM)
    //
    // Fill in the RUNTIME_FUNCTION struct for this prologue.
    //
    UNWIND_INFO *pUnwindInfo = &pUnwindInfoHeader->UnwindInfo;

    PT_RUNTIME_FUNCTION pCurFunction = &pUnwindInfoHeader->FunctionEntry;
    _ASSERTE(IS_ALIGNED(pCurFunction, sizeof(ULONG)));

    S_UINT32 sBeginAddress = S_BYTEPTR(pCode) - S_BYTEPTR(pbBaseAddress);
    if (sBeginAddress.IsOverflow())
        COMPlusThrowArithmetic();
    RUNTIME_FUNCTION__SetBeginAddress(pCurFunction, sBeginAddress.Value());

    S_UINT32 sTemp = S_BYTEPTR(pUnwindInfo) - S_BYTEPTR(pbBaseAddress);
    if (sTemp.IsOverflow())
        COMPlusThrowArithmetic();
    RUNTIME_FUNCTION__SetUnwindInfoAddress(pCurFunction, sTemp.Value());

    //Get the exact function Length. Cannot use globalsize as it is explicitly made to be
    // 4 byte aligned
    CodeRun *pLastCodeElem = GetLastCodeRunIfAny();
    _ASSERTE(pLastCodeElem != NULL);

    int functionLength = pLastCodeElem->m_numcodebytes + pLastCodeElem->m_globaloffset;

    // cannot encode functionLength greater than (2 * 0xFFFFF)
    if (functionLength > 2 * 0xFFFFF)
        COMPlusThrowArithmetic();

    _ASSERTE(functionLength <= globalsize);

    BYTE * pUnwindCodes = (BYTE *)pUnwindInfo + sizeof(DWORD);

    // Not emitting compact unwind info as there are very few (4) dynamic stubs with unwind info.
    // Benefit of the optimization does not outweigh the cost of adding the code for it.

    //UnwindInfo for prolog
    if (m_cbStackFrame != 0)
    {
        if(m_cbStackFrame < 512)
        {
            *pUnwindCodes++ = (BYTE)0xF8;                     // 16-bit sub/add sp,#x
            *pUnwindCodes++ = (BYTE)(m_cbStackFrame >> 18);
            *pUnwindCodes++ = (BYTE)(m_cbStackFrame >> 10);
            *pUnwindCodes++ = (BYTE)(m_cbStackFrame >> 2);
        }
        else
        {
            *pUnwindCodes++ = (BYTE)0xFA;                     // 32-bit sub/add sp,#x
            *pUnwindCodes++ = (BYTE)(m_cbStackFrame >> 18);
            *pUnwindCodes++ = (BYTE)(m_cbStackFrame >> 10);
            *pUnwindCodes++ = (BYTE)(m_cbStackFrame >> 2);
        }

        if(m_cbStackFrame >= 4096)
        {
            // r4 register is used as param to checkStack function and must have been saved in prolog
            _ASSERTE(m_cCalleeSavedRegs > 0);
            *pUnwindCodes++ = (BYTE)0xFB; // nop 16 bit for bl r12
            *pUnwindCodes++ = (BYTE)0xFC; // nop 32 bit for movt r12, checkStack
            *pUnwindCodes++ = (BYTE)0xFC; // nop 32 bit for movw r12, checkStack

            // Ensure that mov r4, m_cbStackFrame fits in a 32-bit instruction
            if(m_cbStackFrame > 65535)
                COMPlusThrow(kNotSupportedException);
            *pUnwindCodes++ = (BYTE)0xFC; // nop 32 bit for mov r4, m_cbStackFrame
        }
    }

    // Unwind info generated will be incorrect when m_cCalleeSavedRegs = 0.
    // The unwind code will say that the size of push/pop instruction
    // size is 16bits when actually the opcode generated by
    // ThumbEmitPop & ThumbEMitPush will be 32bits.
    // Currently no stubs has m_cCalleeSavedRegs as 0
    // therfore just adding the assert.
    _ASSERTE(m_cCalleeSavedRegs > 0);

    if (m_cCalleeSavedRegs <= 4)
    {
        *pUnwindCodes++ = (BYTE)(0xD4 + (m_cCalleeSavedRegs - 1)); // push/pop {r4-rX}
    }
    else
    {
        _ASSERTE(m_cCalleeSavedRegs <= 8);
        *pUnwindCodes++ = (BYTE)(0xDC + (m_cCalleeSavedRegs - 5)); // push/pop {r4-rX}
    }

    if (m_fPushArgRegs)
    {
        *pUnwindCodes++ = (BYTE)0x04; // push {r0-r3} / add sp,#16
        *pUnwindCodes++ = (BYTE)0xFD; // bx lr
    }
    else
    {
        *pUnwindCodes++ = (BYTE)0xFF; // end
    }

    ptrdiff_t epilogUnwindCodeIndex = 0;

    //epilog differs from prolog
    if(m_cbStackFrame >= 4096)
    {
        //Index of the first unwind code of the epilog
        epilogUnwindCodeIndex = pUnwindCodes - (BYTE *)pUnwindInfo - sizeof(DWORD);

        *pUnwindCodes++ = (BYTE)0xF8;                     // sub/add sp,#x
        *pUnwindCodes++ = (BYTE)(m_cbStackFrame >> 18);
        *pUnwindCodes++ = (BYTE)(m_cbStackFrame >> 10);
        *pUnwindCodes++ = (BYTE)(m_cbStackFrame >> 2);

        if (m_cCalleeSavedRegs <= 4)
        {
            *pUnwindCodes++ = (BYTE)(0xD4 + (m_cCalleeSavedRegs - 1)); // push/pop {r4-rX}
        }
        else
        {
            *pUnwindCodes++ = (BYTE)(0xDC + (m_cCalleeSavedRegs - 5)); // push/pop {r4-rX}
        }

        if (m_fPushArgRegs)
        {
            *pUnwindCodes++ = (BYTE)0x04; // push {r0-r3} / add sp,#16
            *pUnwindCodes++ = (BYTE)0xFD; // bx lr
        }
        else
        {
            *pUnwindCodes++ = (BYTE)0xFF; // end
        }

    }

    // Number of 32-bit unwind codes
    size_t codeWordsCount = (ALIGN_UP((size_t)pUnwindCodes, sizeof(void*)) - (size_t)pUnwindInfo - sizeof(DWORD))/4;

    _ASSERTE(epilogUnwindCodeIndex < 32);

    //Check that MAX_UNWIND_CODE_WORDS is sufficient to store all unwind Codes
    _ASSERTE(codeWordsCount <= MAX_UNWIND_CODE_WORDS);

    *(DWORD *)pUnwindInfo =
        ((functionLength) / 2) |
        (1 << 21) |
        ((int)epilogUnwindCodeIndex << 23)|
        ((int)codeWordsCount << 28);

#elif defined(TARGET_ARM64)
    if (!m_fProlog)
    {
        // If EmitProlog isn't called. This is a leaf function which doesn't need any unwindInfo
        T_RUNTIME_FUNCTION *pCurFunction = NULL;
    }
    else
    {

        //
        // Fill in the RUNTIME_FUNCTION struct for this prologue.
        //
        UNWIND_INFO *pUnwindInfo = &(pUnwindInfoHeader->UnwindInfo);

        T_RUNTIME_FUNCTION *pCurFunction = &(pUnwindInfoHeader->FunctionEntry);

        _ASSERTE(IS_ALIGNED(pCurFunction, sizeof(void*)));

        S_UINT32 sBeginAddress = S_BYTEPTR(pCode) - S_BYTEPTR(pbBaseAddress);
        if (sBeginAddress.IsOverflow())
            COMPlusThrowArithmetic();

        S_UINT32 sTemp = S_BYTEPTR(pUnwindInfo) - S_BYTEPTR(pbBaseAddress);
        if (sTemp.IsOverflow())
            COMPlusThrowArithmetic();

        RUNTIME_FUNCTION__SetBeginAddress(pCurFunction, sBeginAddress.Value());
        RUNTIME_FUNCTION__SetUnwindInfoAddress(pCurFunction, sTemp.Value());

        CodeRun *pLastCodeElem = GetLastCodeRunIfAny();
        _ASSERTE(pLastCodeElem != NULL);

        int functionLength = pLastCodeElem->m_numcodebytes + pLastCodeElem->m_globaloffset;

        // .xdata has 18 bits for function length and it is to store the total length of the function in bytes, divided by 4
        // If the function is larger than 1M, then multiple pdata and xdata records must be used, which we don't support right now.
        if (functionLength > 4 * 0x3FFFF)
            COMPlusThrowArithmetic();

        _ASSERTE(functionLength <= globalsize);

        // No support for extended code words and/or extended epilog.
        // ASSERTION: first 10 bits of the pUnwindInfo, which holds the #codewords and #epilogcount, cannot be 0
        // And no space for exception scope data also means that no support for exceptions for the stubs
        // generated with this stublinker.
        BYTE * pUnwindCodes = (BYTE *)pUnwindInfo + sizeof(DWORD);


        // Emitting the unwind codes:
        // The unwind codes are emited in Epilog order.
        //
        // 6. Integer argument registers
        // Although we might be saving the argument registers in the prolog we don't need
        // to report them to the OS. (they are not expressible anyways)

        // 5. Floating point argument registers:
        // Similar to Integer argument registers, no reporting
        //

        // 4. Set the frame pointer
        // ASSUMPTION: none of the Stubs generated with this stublinker change SP value outside of epilog and prolog
        // when that is the case we can skip reporting setting up the frame pointer

        // With skiping Step #4, #5 and #6 Prolog and Epilog becomes reversible. so they can share the unwind codes
        int epilogUnwindCodeIndex = 0;

        unsigned cStackFrameSizeInQWORDs = GetStackFrameSize()/16;
        // 3. Store FP/LR
        // save_fplr
        *pUnwindCodes++ = (BYTE)(0x40 | (m_cbStackSpace>>3));

        // 2. Callee-saved registers
        //
        if (m_cCalleeSavedRegs > 0)
        {
            unsigned offset = 2 + m_cbStackSpace/8; // 2 is for fp,lr
            if ((m_cCalleeSavedRegs %2) ==1)
            {
                // save_reg
                *pUnwindCodes++ = (BYTE) (0xD0 | ((m_cCalleeSavedRegs-1)>>2));
                *pUnwindCodes++ = (BYTE) ((BYTE)((m_cCalleeSavedRegs-1) << 6) | ((offset + m_cCalleeSavedRegs - 1) & 0x3F));
            }
            for (int i=(m_cCalleeSavedRegs/2)*2-2; i>=0; i-=2)
            {
                if (i!=0)
                {
                    // save_next
                    *pUnwindCodes++ = 0xE6;
                }
                else
                {
                    // save_regp
                    *pUnwindCodes++ = 0xC8;
                    *pUnwindCodes++ = (BYTE)(offset & 0x3F);
                }
            }
        }

        // 1. SP Relocation
        //
        // EmitProlog is supposed to reject frames larger than 504 bytes.
        // Assert that here.
        _ASSERTE(cStackFrameSizeInQWORDs <= 0x3F);
        if (cStackFrameSizeInQWORDs <= 0x1F)
        {
            // alloc_s
            *pUnwindCodes++ = (BYTE)(cStackFrameSizeInQWORDs);
        }
        else
        {
            // alloc_m
            *pUnwindCodes++ = (BYTE)(0xC0 | (cStackFrameSizeInQWORDs >> 8));
            *pUnwindCodes++ = (BYTE)(cStackFrameSizeInQWORDs);
        }

        // End
        *pUnwindCodes++ = 0xE4;

        // Number of 32-bit unwind codes
        int codeWordsCount = (int)(ALIGN_UP((size_t)pUnwindCodes, sizeof(DWORD)) - (size_t)pUnwindInfo - sizeof(DWORD))/4;

        //Check that MAX_UNWIND_CODE_WORDS is sufficient to store all unwind Codes
        _ASSERTE(codeWordsCount <= MAX_UNWIND_CODE_WORDS);

        *(DWORD *)pUnwindInfo =
            ((functionLength) / 4) |
            (1 << 21) |     // E bit
            (epilogUnwindCodeIndex << 22)|
            (codeWordsCount << 27);
    } // end else (!m_fProlog)
#else
    PORTABILITY_ASSERT("StubLinker::EmitUnwindInfo");
    T_RUNTIME_FUNCTION *pCurFunction = NULL;
#endif

    //
    // Get a StubUnwindInfoHeapSegment for this base address
    //

    CrstHolder crst(&g_StubUnwindInfoHeapSegmentsCrst);

    StubUnwindInfoHeapSegment *pStubHeapSegment;
    StubUnwindInfoHeapSegment **ppPrevStubHeapSegment;
    for (ppPrevStubHeapSegment = &g_StubHeapSegments;
         (pStubHeapSegment = *ppPrevStubHeapSegment);
         (ppPrevStubHeapSegment = &pStubHeapSegment->pNext))
    {
        if (pbBaseAddress < pStubHeapSegment->pbBaseAddress)
        {
            // The list is ordered, so address is between segments
            pStubHeapSegment = NULL;
            break;
        }

        if (pbBaseAddress == pStubHeapSegment->pbBaseAddress)
        {
            // Found an existing segment
            break;
        }
    }

    if (!pStubHeapSegment)
    {
        //
        // RtlInstallFunctionTableCallback will only accept a ULONG for the
        // region size.  We've already checked above that the RUNTIME_FUNCTION
        // offsets will work relative to pbBaseAddress.
        //

        SIZE_T cbSegment = findBlockArgs.cbBlockSize;

        if (cbSegment > MaxSegmentSize)
            cbSegment = MaxSegmentSize;

        NewHolder<StubUnwindInfoHeapSegment> pNewStubHeapSegment = new StubUnwindInfoHeapSegment();


        pNewStubHeapSegment->pbBaseAddress = pbBaseAddress;
        pNewStubHeapSegment->cbSegment = cbSegment;
        pNewStubHeapSegment->pUnwindHeaderList = NULL;
#ifdef TARGET_AMD64
        pNewStubHeapSegment->pUnwindInfoTable = NULL;
#endif

        // Insert the new stub into list
        pNewStubHeapSegment->pNext = *ppPrevStubHeapSegment;
        *ppPrevStubHeapSegment = pNewStubHeapSegment;
        pNewStubHeapSegment.SuppressRelease();

        // Use new segment for the stub
        pStubHeapSegment = pNewStubHeapSegment;

        InstallEEFunctionTable(
                pNewStubHeapSegment,
                pbBaseAddress,
                (ULONG)cbSegment,
                &FindStubFunctionEntry,
                pNewStubHeapSegment,
                DYNFNTABLE_STUB);
    }

    //
    // Link the new stub into the segment.
    //

    pHeader->pNext = pStubHeapSegment->pUnwindHeaderList;
                     pStubHeapSegment->pUnwindHeaderList = pHeader;

#ifdef TARGET_AMD64
    // Publish Unwind info to ETW stack crawler
    UnwindInfoTable::AddToUnwindInfoTable(
        &pStubHeapSegment->pUnwindInfoTable, pCurFunction,
        (TADDR) pStubHeapSegment->pbBaseAddress,
        (TADDR) pStubHeapSegment->pbBaseAddress + pStubHeapSegment->cbSegment);
#endif

#ifdef _DEBUG
    _ASSERTE(pHeader->IsRegistered());
    _ASSERTE(   &pHeader->FunctionEntry
             == FindStubFunctionEntry((ULONG64)pCode,                  EncodeDynamicFunctionTableContext(pStubHeapSegment, DYNFNTABLE_STUB)));
#endif

    return true;
}
#endif // STUBLINKER_GENERATES_UNWIND_INFO

#ifdef TARGET_ARM
void StubLinker::DescribeProlog(UINT cCalleeSavedRegs, UINT cbStackFrame, BOOL fPushArgRegs)
{
    m_fProlog = TRUE;
    m_cCalleeSavedRegs = cCalleeSavedRegs;
    m_cbStackFrame = cbStackFrame;
    m_fPushArgRegs = fPushArgRegs;
}
#elif defined(TARGET_ARM64)
void StubLinker::DescribeProlog(UINT cIntRegArgs, UINT cVecRegArgs, UINT cCalleeSavedRegs, UINT cbStackSpace)
{
    m_fProlog               = TRUE;
    m_cIntRegArgs           = cIntRegArgs;
    m_cVecRegArgs           = cVecRegArgs;
    m_cCalleeSavedRegs      = cCalleeSavedRegs;
    m_cbStackSpace          = cbStackSpace;
}

UINT StubLinker::GetSavedRegArgsOffset()
{
    _ASSERTE(m_fProlog);
    // This is the offset from SP
    // We're assuming that the stublinker will push the arg registers to the bottom of the stack frame
    return m_cbStackSpace +  (2+ m_cCalleeSavedRegs)*sizeof(void*); // 2 is for FP and LR
}

UINT StubLinker::GetStackFrameSize()
{
    _ASSERTE(m_fProlog);
    return m_cbStackSpace + (2 + m_cCalleeSavedRegs + m_cIntRegArgs + m_cVecRegArgs)*sizeof(void*);
}


#endif // ifdef TARGET_ARM, elif defined(TARGET_ARM64)

#endif // #ifndef DACCESS_COMPILE

#ifndef DACCESS_COMPILE

// Redeclaring the Stub type here and assert its size.
// The size assertion is done here because of where CODE_SIZE_ALIGN
// is defined - it is not included in all places where stublink.h
// is consumed.
class Stub;
static_assert_no_msg((sizeof(Stub) % CODE_SIZE_ALIGN) == 0);

//-------------------------------------------------------------------
// Inc the refcount.
//-------------------------------------------------------------------
VOID Stub::IncRef()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(m_signature == kUsedStub);
    InterlockedIncrement((LONG*)&m_refcount);
}

//-------------------------------------------------------------------
// Dec the refcount.
//-------------------------------------------------------------------
BOOL Stub::DecRef()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(m_signature == kUsedStub);
    int count = InterlockedDecrement((LONG*)&m_refcount);
    if (count <= 0) {
        DeleteStub();
        return TRUE;
    }
    return FALSE;
}

VOID Stub::DeleteStub()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

#ifdef STUBLINKER_GENERATES_UNWIND_INFO
    if (HasUnwindInfo())
    {
        StubUnwindInfoHeader *pHeader = GetUnwindInfoHeader();

        //
        // Check if the stub has been linked into a StubUnwindInfoHeapSegment.
        //
        if (pHeader->IsRegistered())
        {
            CrstHolder crst(&g_StubUnwindInfoHeapSegmentsCrst);

            //
            // Find the segment containing the stub.
            //
            StubUnwindInfoHeapSegment **ppPrevSegment = &g_StubHeapSegments;
            StubUnwindInfoHeapSegment *pSegment = *ppPrevSegment;

            if (pSegment)
            {
                PBYTE pbCode = (PBYTE)GetEntryPointInternal();
#ifdef TARGET_AMD64
                UnwindInfoTable::RemoveFromUnwindInfoTable(&pSegment->pUnwindInfoTable,
                    (TADDR) pSegment->pbBaseAddress, (TADDR) pbCode);
#endif
                for (StubUnwindInfoHeapSegment *pNextSegment = pSegment->pNext;
                     pNextSegment;
                     ppPrevSegment = &pSegment->pNext, pSegment = pNextSegment, pNextSegment = pSegment->pNext)
                {
                    // The segments are sorted by pbBaseAddress.
                    if (pbCode < pNextSegment->pbBaseAddress)
                        break;
                }
            }

            // The stub was marked as registered, so a segment should exist.
            _ASSERTE(pSegment);

            if (pSegment)
            {

                //
                // Find this stub's location in the segment's list.
                //
                StubUnwindInfoHeader *pCurHeader;
                StubUnwindInfoHeader **ppPrevHeaderList;
                for (ppPrevHeaderList = &pSegment->pUnwindHeaderList;
                     (pCurHeader = *ppPrevHeaderList);
                     (ppPrevHeaderList = &pCurHeader->pNext))
                {
                    if (pHeader == pCurHeader)
                        break;
                }

                // The stub was marked as registered, so we should find it in the segment's list.
                _ASSERTE(pCurHeader);

                if (pCurHeader)
                {
                    //
                    // Remove the stub from the segment's list.
                    //
                    *ppPrevHeaderList = pHeader->pNext;

                    //
                    // If the segment's list is now empty, delete the segment.
                    //
                    if (!pSegment->pUnwindHeaderList)
                    {
                        DeleteEEFunctionTable(pSegment);
#ifdef TARGET_AMD64
                        if (pSegment->pUnwindInfoTable != 0)
                            delete pSegment->pUnwindInfoTable;
#endif
                        *ppPrevSegment = pSegment->pNext;
                        delete pSegment;
                    }
                }
            }
        }
    }
#endif

    if ((m_numCodeBytesAndFlags & LOADER_HEAP_BIT) == 0)
    {
#ifdef _DEBUG
        m_signature = kFreedStub;
        FillMemory(this+1, GetNumCodeBytes(), 0xcc);
#endif

        delete [] (BYTE*)GetAllocationBase();
    }
}

TADDR Stub::GetAllocationBase()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    TADDR info = dac_cast<TADDR>(this);
    SIZE_T cbPrefix = 0;

#ifdef STUBLINKER_GENERATES_UNWIND_INFO
    if (HasUnwindInfo())
    {
        StubUnwindInfoHeaderSuffix *pSuffix =
            PTR_StubUnwindInfoHeaderSuffix(info - cbPrefix -
                                           sizeof(*pSuffix));

        cbPrefix += StubUnwindInfoHeader::ComputeAlignedSize(pSuffix->nUnwindInfoSize);
    }
#endif // STUBLINKER_GENERATES_UNWIND_INFO

    if (!HasExternalEntryPoint())
    {
        cbPrefix = ALIGN_UP(cbPrefix + sizeof(Stub), CODE_SIZE_ALIGN) - sizeof(Stub);
    }

    return info - cbPrefix;
}

Stub* Stub::NewStub(PTR_VOID pCode, DWORD flags)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Stub* pStub = NewStub(NULL, 0, flags | NEWSTUB_FL_EXTERNAL);

    // Passing NEWSTUB_FL_EXTERNAL requests the stub struct be
    // expanded in size by a single pointer. Insert the code point at this
    // location.
    *(PTR_VOID *)(pStub + 1) = pCode;

    return pStub;
}

//-------------------------------------------------------------------
// Stub allocation done here.
//-------------------------------------------------------------------
/*static*/ Stub* Stub::NewStub(
        LoaderHeap *pHeap,
        UINT numCodeBytes,
        DWORD flags
#ifdef STUBLINKER_GENERATES_UNWIND_INFO
        , UINT nUnwindInfoSize
#endif
        )
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // The memory layout of the allocated memory for the Stub instance is as follows:
    //  Offset:
    //  - 0
    //      optional: unwind info - see nUnwindInfoSize usage.
    //  - stubPayloadOffset
    //      Stub instance
    //      optional: external pointer | padding + code
    size_t stubPayloadOffset = 0;
    S_SIZE_T size = S_SIZE_T(sizeof(Stub));

#ifdef STUBLINKER_GENERATES_UNWIND_INFO
    _ASSERTE(!nUnwindInfoSize || !pHeap || pHeap->m_fPermitStubsWithUnwindInfo);

    if (nUnwindInfoSize != 0)
    {
        // The Unwind info precedes the Stub itself.
        stubPayloadOffset = StubUnwindInfoHeader::ComputeAlignedSize(nUnwindInfoSize);
        size += stubPayloadOffset;
    }
#endif // STUBLINKER_GENERATES_UNWIND_INFO

    if (flags & NEWSTUB_FL_EXTERNAL)
    {
        _ASSERTE(pHeap == NULL);
        _ASSERTE(numCodeBytes == 0);
        size += sizeof(PTR_PCODE);
    }
    else
    {
        size.AlignUp(CODE_SIZE_ALIGN);
        size += numCodeBytes;
    }

    if (size.IsOverflow())
        COMPlusThrowArithmetic();

    size_t totalSize = size.Value();

    BYTE *pBlock;
    if (pHeap == NULL)
    {
        pBlock = new BYTE[totalSize];
    }
    else
    {
        TaggedMemAllocPtr ptr = pHeap->AllocAlignedMem(totalSize, CODE_SIZE_ALIGN);
        pBlock = (BYTE*)(void*)ptr;
        flags |= NEWSTUB_FL_LOADERHEAP;
    }

    _ASSERTE((stubPayloadOffset % CODE_SIZE_ALIGN) == 0);
    Stub* pStubRX = (Stub*)(pBlock + stubPayloadOffset);
    Stub* pStubRW;
    ExecutableWriterHolderNoLog<Stub> stubWriterHolder;

    if (pHeap == NULL)
    {
        pStubRW = pStubRX;
    }
    else
    {
        stubWriterHolder.AssignExecutableWriterHolder(pStubRX, sizeof(Stub));
        pStubRW = stubWriterHolder.GetRW();
    }
    pStubRW->SetupStub(
            numCodeBytes,
            flags
#ifdef STUBLINKER_GENERATES_UNWIND_INFO
            , nUnwindInfoSize
#endif
            );

    _ASSERTE((BYTE *)pStubRX->GetAllocationBase() == pBlock);

    return pStubRX;
}

void Stub::SetupStub(int numCodeBytes, DWORD flags
#ifdef STUBLINKER_GENERATES_UNWIND_INFO
                     , UINT nUnwindInfoSize
#endif
                     )
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    m_signature = kUsedStub;
#ifdef HOST_64BIT
    m_pad_code_bytes1 = 0;
    m_pad_code_bytes2 = 0;
    m_pad_code_bytes3 = 0;
#endif
#endif

    if (((DWORD)numCodeBytes) >= MAX_CODEBYTES)
        COMPlusThrowHR(COR_E_OVERFLOW);

    m_numCodeBytesAndFlags = numCodeBytes;

    m_refcount = 1;
    m_data = {};

    if (flags != NEWSTUB_FL_NONE)
    {
        if((flags & NEWSTUB_FL_LOADERHEAP) != 0)
            m_numCodeBytesAndFlags |= LOADER_HEAP_BIT;
        if((flags & NEWSTUB_FL_MULTICAST) != 0)
            m_numCodeBytesAndFlags |= MULTICAST_DELEGATE_BIT;
        if ((flags & NEWSTUB_FL_EXTERNAL) != 0)
            m_numCodeBytesAndFlags |= EXTERNAL_ENTRY_BIT;
        if ((flags & NEWSTUB_FL_INSTANTIATING_METHOD) != 0)
            m_numCodeBytesAndFlags |= INSTANTIATING_STUB_BIT;
    }

#ifdef STUBLINKER_GENERATES_UNWIND_INFO
    if (nUnwindInfoSize)
    {
        m_numCodeBytesAndFlags |= UNWIND_INFO_BIT;

        StubUnwindInfoHeaderSuffix * pSuffix = GetUnwindInfoHeaderSuffix();
        pSuffix->nUnwindInfoSize = (BYTE)nUnwindInfoSize;

        StubUnwindInfoHeader * pHeader = GetUnwindInfoHeader();
        pHeader->Init();
    }
#endif
}

//-------------------------------------------------------------------
// One-time init
//-------------------------------------------------------------------
/*static*/ void Stub::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef STUBLINKER_GENERATES_UNWIND_INFO
    g_StubUnwindInfoHeapSegmentsCrst.Init(CrstStubUnwindInfoHeapSegments);
#endif
}

//-------------------------------------------------------------------
// Constructor
//-------------------------------------------------------------------
ArgBasedStubCache::ArgBasedStubCache(UINT fixedSlots)
        : m_numFixedSlots(fixedSlots),
          m_crst(CrstArgBasedStubCache)
{
    WRAPPER_NO_CONTRACT;

    m_aStub = new Stub * [m_numFixedSlots];
    _ASSERTE(m_aStub != NULL);

    for (unsigned __int32 i = 0; i < m_numFixedSlots; i++) {
        m_aStub[i] = NULL;
    }
    m_pSlotEntries = NULL;
}


//-------------------------------------------------------------------
// Destructor
//-------------------------------------------------------------------
ArgBasedStubCache::~ArgBasedStubCache()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    for (unsigned __int32 i = 0; i < m_numFixedSlots; i++) {
        Stub *pStub = m_aStub[i];
        if (pStub) {
            pStub->DecRef();
        }
    }
    // a size of 0 is a signal to Nirvana to flush the entire cache
    // not sure if this is needed, but should have no CLR perf impact since size is 0.
    FlushInstructionCache(GetCurrentProcess(),0,0);

    SlotEntry **ppSlotEntry = &m_pSlotEntries;
    SlotEntry *pCur;
    while (NULL != (pCur = *ppSlotEntry)) {
        Stub *pStub = pCur->m_pStub;
        pStub->DecRef();
        *ppSlotEntry = pCur->m_pNext;
        delete pCur;
    }
    delete [] m_aStub;
}



//-------------------------------------------------------------------
// Queries/retrieves a previously cached stub.
//
// If there is no stub corresponding to the given index,
//   this function returns NULL.
//
// Otherwise, this function returns the stub after
//   incrementing its refcount.
//-------------------------------------------------------------------
Stub *ArgBasedStubCache::GetStub(UINT_PTR key)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    Stub *pStub;

    CrstHolder ch(&m_crst);

    if (key < m_numFixedSlots) {
        pStub = m_aStub[key];
    } else {
        pStub = NULL;
        for (SlotEntry *pSlotEntry = m_pSlotEntries;
             pSlotEntry != NULL;
             pSlotEntry = pSlotEntry->m_pNext) {

            if (pSlotEntry->m_key == key) {
                pStub = pSlotEntry->m_pStub;
                break;
            }
        }
    }
    if (pStub) {
        ExecutableWriterHolder<Stub> stubWriterHolder(pStub, sizeof(Stub));
        stubWriterHolder.GetRW()->IncRef();
    }
    return pStub;
}


//-------------------------------------------------------------------
// Tries to associate a stub with a given index. This association
// may fail because some other thread may have beaten you to it
// just before you make the call.
//
// If the association succeeds, "pStub" is installed, and it is
// returned back to the caller. The stub's refcount is incremented
// twice (one to reflect the cache's ownership, and one to reflect
// the caller's ownership.)
//
// If the association fails because another stub is already installed,
// then the incumbent stub is returned to the caller and its refcount
// is incremented once (to reflect the caller's ownership.)
//
// If the association fails due to lack of memory, NULL is returned
// and no one's refcount changes.
//
// This routine is intended to be called like this:
//
//    Stub *pCandidate = MakeStub();  // after this, pCandidate's rc is 1
//    Stub *pWinner = cache->SetStub(idx, pCandidate);
//    pCandidate->DecRef();
//    pCandidate = 0xcccccccc;     // must not use pCandidate again.
//    if (!pWinner) {
//          OutOfMemoryError;
//    }
//    // If the association succeeded, pWinner's refcount is 2 and so
//    // is pCandidate's (because it *is* pWinner);.
//    // If the association failed, pWinner's refcount is still 2
//    // and pCandidate got destroyed by the last DecRef().
//    // Either way, pWinner is now the official index holder. It
//    // has a refcount of 2 (one for the cache's ownership, and
//    // one belonging to this code.)
//-------------------------------------------------------------------
Stub* ArgBasedStubCache::AttemptToSetStub(UINT_PTR key, Stub *pStub)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    CrstHolder ch(&m_crst);

    bool incRefForCache = false;

    if (key < m_numFixedSlots) {
        if (m_aStub[key]) {
            pStub = m_aStub[key];
        } else {
            m_aStub[key] = pStub;
            incRefForCache = true;
        }
    } else {
        SlotEntry *pSlotEntry;
        for (pSlotEntry = m_pSlotEntries;
             pSlotEntry != NULL;
             pSlotEntry = pSlotEntry->m_pNext) {

            if (pSlotEntry->m_key == key) {
                pStub = pSlotEntry->m_pStub;
                break;
            }
        }
        if (!pSlotEntry) {
            pSlotEntry = new SlotEntry;
            pSlotEntry->m_pStub = pStub;
            incRefForCache = true;
            pSlotEntry->m_key = key;
            pSlotEntry->m_pNext = m_pSlotEntries;
            m_pSlotEntries = pSlotEntry;
        }
    }
    if (pStub) {
        ExecutableWriterHolder<Stub> stubWriterHolder(pStub, sizeof(Stub));
        if (incRefForCache)
        {
            stubWriterHolder.GetRW()->IncRef();   // IncRef on cache's behalf
        }
        stubWriterHolder.GetRW()->IncRef();  // IncRef because we're returning it to caller
    }
    return pStub;
}



#ifdef _DEBUG
// Diagnostic dump
VOID ArgBasedStubCache::Dump()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    printf("--------------------------------------------------------------\n");
    printf("ArgBasedStubCache dump (%lu fixed entries):\n", m_numFixedSlots);
    for (UINT32 i = 0; i < m_numFixedSlots; i++) {

        printf("  Fixed slot %lu: ", (ULONG)i);
        Stub *pStub = m_aStub[i];
        if (!pStub) {
            printf("empty\n");
        } else {
            printf("%zxh   - refcount is %lu\n",
                   (size_t)(pStub->GetEntryPoint()),
                   (ULONG)( *( ( ((ULONG*)(pStub->GetEntryPoint())) - 1))));
        }
    }

    for (SlotEntry *pSlotEntry = m_pSlotEntries;
         pSlotEntry != NULL;
         pSlotEntry = pSlotEntry->m_pNext) {

        printf("  Dyna. slot %lu: ", (ULONG)(pSlotEntry->m_key));
        Stub *pStub = pSlotEntry->m_pStub;
        printf("%zxh   - refcount is %lu\n",
               (size_t)(pStub->GetEntryPoint()),
               (ULONG)( *( ( ((ULONG*)(pStub->GetEntryPoint())) - 1))));

    }


    printf("--------------------------------------------------------------\n");
}
#endif

#endif // #ifndef DACCESS_COMPILE

