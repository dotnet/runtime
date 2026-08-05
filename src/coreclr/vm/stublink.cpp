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

#include "perfmap.h"

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
    m_pTargetMethod     = NULL;
    m_stackSize         = 0;
    m_fDataOnly         = FALSE;
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
VOID StubLinker::Emit8 (uint8_t  val)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CodeRun *pCodeRun = GetLastCodeRunIfAny();
    if (pCodeRun && (CODERUNSIZE - pCodeRun->m_numcodebytes) >= sizeof(val)) {
        *((uint8_t *)(pCodeRun->m_codebytes + pCodeRun->m_numcodebytes)) = val;
        pCodeRun->m_numcodebytes += sizeof(val);
    } else {
        EmitBytes((BYTE*)&val, sizeof(val));
    }
}

//---------------------------------------------------------------
// Append code bytes.
//---------------------------------------------------------------
VOID StubLinker::Emit16(uint16_t val)
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
VOID StubLinker::Emit32(uint32_t val)
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
VOID StubLinker::Emit64(uint64_t val)
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
// Throws exception on failure.
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
// Throws exception on failure.
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
// Throws exception on failure.
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
// Throws exception on failure.
//---------------------------------------------------------------
Stub *StubLinker::Link(LoaderAllocator *pLoaderAllocator, StubCodeBlockKind kind, const char *stubType)
{
    STANDARD_VM_CONTRACT;

    int globalsize = 0;
    int size = CalculateSize(&globalsize);
    DWORD flags = (m_pTargetMethod != NULL) ? NEWSTUB_FL_INSTANTIATING_METHOD : NEWSTUB_FL_NONE;
    if (kind == STUB_CODE_BLOCK_SHUFFLE_THUNK)
    {
        flags |= NEWSTUB_FL_SHUFFLE_THUNK;
    }

    StubHolder<Stub> pStub{ Stub::NewStub(
                pLoaderAllocator,
                size,
                flags) };
    ASSERT(pStub != NULL);

    EmitStub(pStub, pLoaderAllocator, kind, globalsize, size);

    PerfMap::LogStubs(__FUNCTION__, stubType, pStub->GetEntryPoint(), pStub->GetNumCodeBytes(), PerfMapStubType::Individual);

    return pStub.Detach();
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

void StubLinker::EmitStub(Stub* pStub, LoaderAllocator* pLoaderAllocator, StubCodeBlockKind kind, int globalsize, int totalSize)
{
    STANDARD_VM_CONTRACT;

    S_SIZE_T allocationSize(totalSize);
    allocationSize += CODE_SIZE_ALIGN;
    if (allocationSize.IsOverflow())
        COMPlusThrowArithmetic();

    BYTE* pBlock = reinterpret_cast<BYTE*>(ExecutionManager::GetEEJitManager()->AllocCodeFragmentBlock(
        allocationSize.Value(),
        CODE_SIZE_ALIGN,
        pLoaderAllocator,
        kind));
    size_t codeOffset = ALIGN_UP(reinterpret_cast<size_t>(pBlock) + sizeof(PTR_Stub), CODE_SIZE_ALIGN) - reinterpret_cast<size_t>(pBlock);
    BYTE* pCode = pBlock + codeOffset;
    pStub->m_entryPoint = pCode;

    ExecutableWriterHolder<BYTE> stubWriterHolder(pBlock, allocationSize.Value());
    BYTE* pBlockRW = stubWriterHolder.GetRW();
    BYTE* pCodeRW = pBlockRW + codeOffset;
    SET_UNALIGNED_PTR(pCodeRW - sizeof(PTR_Stub), reinterpret_cast<TADDR>(pStub));

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
                    int64_t fixupval;

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
                        fixupval = (int64_t)(size_t)targetglobaladdr;
                    } else
                        fixupval = (int64_t)(targetglobaladdr - srcglobaladdr);

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

    // Fill in the target method for the Instantiating stub.
    if (pStub->IsInstantiatingStub())
    {
        _ASSERTE(m_pTargetMethod != NULL);
        pStub->SetInstantiatedMethodDesc(m_pTargetMethod);

        LOG((LF_CORDB, LL_INFO100, "SL::ES: InstantiatedMethod fd:0x%x\n",
            pStub->GetInstantiatedMethodDesc()));
    }

    if (!m_fDataOnly)
    {
        FlushInstructionCache(GetCurrentProcess(), pCode, globalsize);
    }

    _ASSERTE(m_fDataOnly || DbgIsExecutable(pCode, globalsize));
}

#endif // #ifndef DACCESS_COMPILE

#ifndef DACCESS_COMPILE

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

    if ((m_numCodeBytesAndFlags & LOADER_HEAP_BIT) == 0)
    {
#ifdef _DEBUG
        m_signature = kFreedStub;
        FillMemory(this, sizeof(*this), 0xcc);
#endif

        delete [] reinterpret_cast<BYTE*>(this);
    }
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
    pStub->m_entryPoint = reinterpret_cast<PTR_CBYTE>(pCode);

    return pStub;
}

//-------------------------------------------------------------------
// Stub allocation done here.
//-------------------------------------------------------------------
/*static*/ Stub* Stub::NewStub(
        LoaderAllocator *pLoaderAllocator,
        UINT numCodeBytes,
        DWORD flags)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    S_SIZE_T size = S_SIZE_T(sizeof(Stub));

    if (flags & NEWSTUB_FL_EXTERNAL)
    {
        _ASSERTE(pLoaderAllocator == NULL);
        _ASSERTE(numCodeBytes == 0);
    }

    if (size.IsOverflow())
        COMPlusThrowArithmetic();

    size_t totalSize = size.Value();

    BYTE *pBlock;
    if (pLoaderAllocator == NULL)
    {
        pBlock = new BYTE[totalSize];
    }
    else
    {
        TaggedMemAllocPtr ptr = pLoaderAllocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(totalSize));
        pBlock = static_cast<BYTE*>(static_cast<void*>(ptr));
        flags |= NEWSTUB_FL_LOADERHEAP;
    }

    Stub* pStub = reinterpret_cast<Stub*>(pBlock);
    pStub->SetupStub(numCodeBytes, flags);

    return pStub;
}

void Stub::SetupStub(int numCodeBytes, DWORD flags)
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

    m_entryPoint = NULL;
    m_refcount = 1;
    m_data = {};

    if (flags != NEWSTUB_FL_NONE)
    {
        if((flags & NEWSTUB_FL_LOADERHEAP) != 0)
            m_numCodeBytesAndFlags |= LOADER_HEAP_BIT;
        if ((flags & NEWSTUB_FL_EXTERNAL) != 0)
            m_numCodeBytesAndFlags |= EXTERNAL_ENTRY_BIT;
        if ((flags & NEWSTUB_FL_INSTANTIATING_METHOD) != 0)
            m_numCodeBytesAndFlags |= INSTANTIATING_STUB_BIT;
        if ((flags & NEWSTUB_FL_SHUFFLE_THUNK) != 0)
            m_numCodeBytesAndFlags |= SHUFFLE_THUNK_BIT;
    }
}

#endif // #ifndef DACCESS_COMPILE
