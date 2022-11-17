// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "common.h"

#include <windows.h>

#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "regdisplay.h"
#include "ICodeManager.h"
#include "CoffNativeCodeManager.h"
#include "varint.h"
#include "holder.h"

#include "CommonMacros.inl"

#define GCINFODECODER_NO_EE
#include "gcinfodecoder.cpp"

#define UBF_FUNC_KIND_MASK      0x03
#define UBF_FUNC_KIND_ROOT      0x00
#define UBF_FUNC_KIND_HANDLER   0x01
#define UBF_FUNC_KIND_FILTER    0x02

#define UBF_FUNC_HAS_EHINFO             0x04
#define UBF_FUNC_REVERSE_PINVOKE        0x08
#define UBF_FUNC_HAS_ASSOCIATED_DATA    0x10

#ifdef TARGET_X86
//
// x86 ABI does not define RUNTIME_FUNCTION. Define our own to allow unification between x86 and other platforms.
//
typedef struct _RUNTIME_FUNCTION {
    DWORD BeginAddress;
    DWORD EndAddress;
    DWORD UnwindData;
} RUNTIME_FUNCTION, *PRUNTIME_FUNCTION;

typedef struct _KNONVOLATILE_CONTEXT_POINTERS {

    // The ordering of these fields should be aligned with that
    // of corresponding fields in CONTEXT
    //
    // (See REGDISPLAY in Runtime/regdisp.h for details)
    PDWORD Edi;
    PDWORD Esi;
    PDWORD Ebx;
    PDWORD Edx;
    PDWORD Ecx;
    PDWORD Eax;

    PDWORD Ebp;

} KNONVOLATILE_CONTEXT_POINTERS, *PKNONVOLATILE_CONTEXT_POINTERS;

typedef struct _UNWIND_INFO {
    ULONG FunctionLength;
} UNWIND_INFO, *PUNWIND_INFO;

#elif defined(TARGET_AMD64)

#define UNW_FLAG_NHANDLER 0x0
#define UNW_FLAG_EHANDLER 0x1
#define UNW_FLAG_UHANDLER 0x2
#define UNW_FLAG_CHAININFO 0x4

//
// The following structures are defined in Windows x64 unwind info specification
// http://www.bing.com/search?q=msdn+Exception+Handling+x64
//
typedef union _UNWIND_CODE {
    struct {
        uint8_t CodeOffset;
        uint8_t UnwindOp : 4;
        uint8_t OpInfo : 4;
    };

    uint16_t FrameOffset;
} UNWIND_CODE, *PUNWIND_CODE;

typedef struct _UNWIND_INFO {
    uint8_t Version : 3;
    uint8_t Flags : 5;
    uint8_t SizeOfProlog;
    uint8_t CountOfUnwindCodes;
    uint8_t FrameRegister : 4;
    uint8_t FrameOffset : 4;
    UNWIND_CODE UnwindCode[1];
} UNWIND_INFO, *PUNWIND_INFO;

#endif // TARGET_X86

typedef DPTR(struct _UNWIND_INFO)      PTR_UNWIND_INFO;
typedef DPTR(union _UNWIND_CODE)       PTR_UNWIND_CODE;

static PTR_VOID GetUnwindDataBlob(TADDR moduleBase, PTR_RUNTIME_FUNCTION pRuntimeFunction, /* out */ size_t * pSize)
{
#if defined(TARGET_AMD64)
    PTR_UNWIND_INFO pUnwindInfo(dac_cast<PTR_UNWIND_INFO>(moduleBase + pRuntimeFunction->UnwindInfoAddress));

    size_t size = offsetof(UNWIND_INFO, UnwindCode) + sizeof(UNWIND_CODE) * pUnwindInfo->CountOfUnwindCodes;

    // Chained unwind info is not supported at this time
    ASSERT((pUnwindInfo->Flags & UNW_FLAG_CHAININFO) == 0);

    if (pUnwindInfo->Flags & (UNW_FLAG_EHANDLER | UNW_FLAG_UHANDLER))
    {
        // Personality routine
        size = ALIGN_UP(size, sizeof(DWORD)) + sizeof(DWORD);
    }

    *pSize = size;

    return pUnwindInfo;

#elif defined(TARGET_X86)

    PTR_UNWIND_INFO pUnwindInfo(dac_cast<PTR_UNWIND_INFO>(moduleBase + pRuntimeFunction->UnwindInfoAddress));

    *pSize = sizeof(UNWIND_INFO);

    return pUnwindInfo;

#elif defined(TARGET_ARM) || defined(TARGET_ARM64)

    // if this function uses packed unwind data then at least one of the two least significant bits
    // will be non-zero.  if this is the case then there will be no xdata record to enumerate.
    ASSERT((pRuntimeFunction->UnwindData & 0x3) == 0);

    // compute the size of the unwind info
    PTR_UInt32 xdata = dac_cast<PTR_UInt32>(pRuntimeFunction->UnwindData + moduleBase);
    int size = 4;

#if defined(TARGET_ARM)
    // See https://docs.microsoft.com/en-us/cpp/build/arm-exception-handling
    int unwindWords = xdata[0] >> 28;
    int epilogScopes = (xdata[0] >> 23) & 0x1f;
#else
    // See https://docs.microsoft.com/en-us/cpp/build/arm64-exception-handling
    int unwindWords = xdata[0] >> 27;
    int epilogScopes = (xdata[0] >> 22) & 0x1f;
#endif

    if (unwindWords == 0 && epilogScopes == 0)
    {
        size += 4;
        unwindWords = (xdata[1] >> 16) & 0xff;
        epilogScopes = xdata[1] & 0xffff;
    }

    if (!(xdata[0] & (1 << 21)))
        size += 4 * epilogScopes;

    size += 4 * unwindWords;

    if ((xdata[0] & (1 << 20)) != 0)
    {
        // Personality routine
        size += 4;
    }

    *pSize = size;
    return xdata;
#else
    PORTABILITY_ASSERT("GetUnwindDataBlob");
    *pSize = 0;
    return NULL;
#endif
}


CoffNativeCodeManager::CoffNativeCodeManager(TADDR moduleBase,
                                             PTR_VOID pvManagedCodeStartRange, uint32_t cbManagedCodeRange,
                                             PTR_RUNTIME_FUNCTION pRuntimeFunctionTable, uint32_t nRuntimeFunctionTable,
                                             PTR_PTR_VOID pClasslibFunctions, uint32_t nClasslibFunctions)
    : m_moduleBase(moduleBase),
      m_pvManagedCodeStartRange(pvManagedCodeStartRange), m_cbManagedCodeRange(cbManagedCodeRange),
      m_pRuntimeFunctionTable(pRuntimeFunctionTable), m_nRuntimeFunctionTable(nRuntimeFunctionTable),
      m_pClasslibFunctions(pClasslibFunctions), m_nClasslibFunctions(nClasslibFunctions)
{
}

CoffNativeCodeManager::~CoffNativeCodeManager()
{
}

static int LookupUnwindInfoForMethod(uint32_t relativePc,
                                     PTR_RUNTIME_FUNCTION pRuntimeFunctionTable,
                                     int low,
                                     int high)
{
#ifdef TARGET_ARM
    relativePc |= THUMB_CODE;
#endif

    // Binary search the RUNTIME_FUNCTION table
    // Use linear search once we get down to a small number of elements
    // to avoid Binary search overhead.
    while (high - low > 10)
    {
       int middle = low + (high - low) / 2;

       PTR_RUNTIME_FUNCTION pFunctionEntry = pRuntimeFunctionTable + middle;
       if (relativePc < pFunctionEntry->BeginAddress)
       {
           high = middle - 1;
       }
       else
       {
           low = middle;
       }
    }

    for (int i = low; i < high; i++)
    {
        PTR_RUNTIME_FUNCTION pNextFunctionEntry = pRuntimeFunctionTable + (i + 1);
        if (relativePc < pNextFunctionEntry->BeginAddress)
        {
            high = i;
            break;
        }
    }

    PTR_RUNTIME_FUNCTION pFunctionEntry = pRuntimeFunctionTable + high;
    if (relativePc >= pFunctionEntry->BeginAddress)
    {
        return high;
    }

    ASSERT_UNCONDITIONALLY("Invalid code address");
    return -1;
}

struct CoffNativeMethodInfo
{
    PTR_RUNTIME_FUNCTION mainRuntimeFunction;
    PTR_RUNTIME_FUNCTION runtimeFunction;
    bool executionAborted;
};

// Ensure that CoffNativeMethodInfo fits into the space reserved by MethodInfo
static_assert(sizeof(CoffNativeMethodInfo) <= sizeof(MethodInfo), "CoffNativeMethodInfo too big");

bool CoffNativeCodeManager::FindMethodInfo(PTR_VOID        ControlPC,
                                           MethodInfo *    pMethodInfoOut)
{
    // Stackwalker may call this with ControlPC that does not belong to this code manager
    if (dac_cast<TADDR>(ControlPC) < dac_cast<TADDR>(m_pvManagedCodeStartRange) ||
        dac_cast<TADDR>(m_pvManagedCodeStartRange) + m_cbManagedCodeRange <= dac_cast<TADDR>(ControlPC))
    {
        return false;
    }

    CoffNativeMethodInfo * pMethodInfo = (CoffNativeMethodInfo *)pMethodInfoOut;

    TADDR relativePC = dac_cast<TADDR>(ControlPC) - m_moduleBase;

    int MethodIndex = LookupUnwindInfoForMethod((uint32_t)relativePC, m_pRuntimeFunctionTable,
        0, m_nRuntimeFunctionTable - 1);
    if (MethodIndex < 0)
        return false;

    PTR_RUNTIME_FUNCTION pRuntimeFunction = m_pRuntimeFunctionTable + MethodIndex;

    pMethodInfo->runtimeFunction = pRuntimeFunction;

    // The runtime function could correspond to a funclet.  We need to get to the
    // runtime function of the main method.
    for (;;)
    {
        size_t unwindDataBlobSize;
        PTR_VOID pUnwindDataBlob = GetUnwindDataBlob(m_moduleBase, pRuntimeFunction, &unwindDataBlobSize);

        uint8_t unwindBlockFlags = *(dac_cast<DPTR(uint8_t)>(pUnwindDataBlob) + unwindDataBlobSize);
        if ((unwindBlockFlags & UBF_FUNC_KIND_MASK) == UBF_FUNC_KIND_ROOT)
            break;

        pRuntimeFunction--;
    }

    pMethodInfo->mainRuntimeFunction = pRuntimeFunction;

    pMethodInfo->executionAborted = false;

    return true;
}

bool CoffNativeCodeManager::IsFunclet(MethodInfo * pMethInfo)
{
    CoffNativeMethodInfo * pMethodInfo = (CoffNativeMethodInfo *)pMethInfo;

    size_t unwindDataBlobSize;
    PTR_VOID pUnwindDataBlob = GetUnwindDataBlob(m_moduleBase, pMethodInfo->runtimeFunction, &unwindDataBlobSize);

    uint8_t unwindBlockFlags = *(dac_cast<DPTR(uint8_t)>(pUnwindDataBlob) + unwindDataBlobSize);

    // A funclet will have an entry in funclet to main method map
    return (unwindBlockFlags & UBF_FUNC_KIND_MASK) != UBF_FUNC_KIND_ROOT;
}

bool CoffNativeCodeManager::IsFilter(MethodInfo * pMethInfo)
{
    CoffNativeMethodInfo * pMethodInfo = (CoffNativeMethodInfo *)pMethInfo;

    size_t unwindDataBlobSize;
    PTR_VOID pUnwindDataBlob = GetUnwindDataBlob(m_moduleBase, pMethodInfo->runtimeFunction, &unwindDataBlobSize);

    uint8_t unwindBlockFlags = *(dac_cast<DPTR(uint8_t)>(pUnwindDataBlob) + unwindDataBlobSize);

    return (unwindBlockFlags & UBF_FUNC_KIND_MASK) == UBF_FUNC_KIND_FILTER;
}

PTR_VOID CoffNativeCodeManager::GetFramePointer(MethodInfo *   pMethInfo,
                                         REGDISPLAY *   pRegisterSet)
{
    CoffNativeMethodInfo * pMethodInfo = (CoffNativeMethodInfo *)pMethInfo;

    size_t unwindDataBlobSize;
    PTR_VOID pUnwindDataBlob = GetUnwindDataBlob(m_moduleBase, pMethodInfo->runtimeFunction, &unwindDataBlobSize);

    uint8_t unwindBlockFlags = *(dac_cast<DPTR(uint8_t)>(pUnwindDataBlob) + unwindDataBlobSize);

    // Return frame pointer for methods with EH and funclets
    if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) != 0 || (unwindBlockFlags & UBF_FUNC_KIND_MASK) != UBF_FUNC_KIND_ROOT)
    {
        return (PTR_VOID)pRegisterSet->GetFP();
    }

    return NULL;
}

uint32_t CoffNativeCodeManager::GetCodeOffset(MethodInfo* pMethodInfo, PTR_VOID address, /*out*/ PTR_UInt8* gcInfo)
{
    CoffNativeMethodInfo * pNativeMethodInfo = (CoffNativeMethodInfo *)pMethodInfo;

    _ASSERTE(FindMethodInfo(address, pMethodInfo) && (MethodInfo*)pNativeMethodInfo == pMethodInfo);

    size_t unwindDataBlobSize;
    PTR_VOID pUnwindDataBlob = GetUnwindDataBlob(m_moduleBase, pNativeMethodInfo->mainRuntimeFunction, &unwindDataBlobSize);

    PTR_UInt8 p = dac_cast<PTR_UInt8>(pUnwindDataBlob) + unwindDataBlobSize;

    uint8_t unwindBlockFlags = *p++;

    if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) != 0)
        p += sizeof(int32_t);

    if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) != 0)
        p += sizeof(int32_t);

    *gcInfo = p;

    TADDR methodStartAddress = m_moduleBase + pNativeMethodInfo->mainRuntimeFunction->BeginAddress;
    return (uint32_t)(dac_cast<TADDR>(address) - methodStartAddress);
}

bool CoffNativeCodeManager::IsSafePoint(PTR_VOID pvAddress)
{
    MethodInfo pMethodInfo;
    if (!FindMethodInfo(pvAddress, &pMethodInfo))
    {
        return false;
    }

    PTR_UInt8 gcInfo;
    uint32_t codeOffset = GetCodeOffset(&pMethodInfo, pvAddress, &gcInfo);

    GcInfoDecoder decoder(
        GCInfoToken(gcInfo),
        GcInfoDecoderFlags(DECODE_INTERRUPTIBILITY),
        codeOffset
    );

    return decoder.IsInterruptible();
}

void CoffNativeCodeManager::EnumGcRefs(MethodInfo *    pMethodInfo,
                                       PTR_VOID        safePointAddress,
                                       REGDISPLAY *    pRegisterSet,
                                       GCEnumContext * hCallback,
                                       bool            isActiveStackFrame)
{
    PTR_UInt8 gcInfo;
    uint32_t codeOffset = GetCodeOffset(pMethodInfo, safePointAddress, &gcInfo);

    if (!isActiveStackFrame)
    {
        // If we are not in the active method, we are currently pointing
        // to the return address. That may not be reachable after a call (if call does not return)
        // or reachable via a jump and thus have a different live set.
        // Therefore we simply adjust the offset to inside of call instruction.
        // NOTE: The GcInfoDecoder depends on this; if you change it, you must
        // revisit the GcInfoEncoder/Decoder
        codeOffset--;
    }

    GcInfoDecoder decoder(
        GCInfoToken(gcInfo),
        GcInfoDecoderFlags(DECODE_GC_LIFETIMES | DECODE_SECURITY_OBJECT | DECODE_VARARG),
        codeOffset
        );

    ICodeManagerFlags flags = (ICodeManagerFlags)0;
    if (((CoffNativeMethodInfo *)pMethodInfo)->executionAborted)
        flags = ICodeManagerFlags::ExecutionAborted;

    if (IsFilter(pMethodInfo))
        flags = (ICodeManagerFlags)(flags | ICodeManagerFlags::NoReportUntracked);

    if (isActiveStackFrame)
        flags = (ICodeManagerFlags)(flags | ICodeManagerFlags::ActiveStackFrame);

    if (!decoder.EnumerateLiveSlots(
        pRegisterSet,
        isActiveStackFrame /* reportScratchSlots */,
        flags,
        hCallback->pCallback,
        hCallback
        ))
    {
        assert(false);
    }
}

uintptr_t CoffNativeCodeManager::GetConservativeUpperBoundForOutgoingArgs(MethodInfo * pMethodInfo, REGDISPLAY * pRegisterSet)
{
    // Return value
    TADDR upperBound;
    CoffNativeMethodInfo* pNativeMethodInfo = (CoffNativeMethodInfo *) pMethodInfo;

    size_t unwindDataBlobSize;
    PTR_VOID pUnwindDataBlob = GetUnwindDataBlob(m_moduleBase, pNativeMethodInfo->runtimeFunction, &unwindDataBlobSize);
    PTR_UInt8 p = dac_cast<PTR_UInt8>(pUnwindDataBlob) + unwindDataBlobSize;
    uint8_t unwindBlockFlags = *p++;

    if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) != 0)
        p += sizeof(int32_t);

    if ((unwindBlockFlags & UBF_FUNC_REVERSE_PINVOKE) != 0)
    {
        // Reverse PInvoke transition should be on the main function body only
        assert(pNativeMethodInfo->mainRuntimeFunction == pNativeMethodInfo->runtimeFunction);

        if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) != 0)
            p += sizeof(int32_t);

        GcInfoDecoder decoder(GCInfoToken(p), DECODE_REVERSE_PINVOKE_VAR);
        INT32 slot = decoder.GetReversePInvokeFrameStackSlot();
        assert(slot != NO_REVERSE_PINVOKE_FRAME);

        TADDR basePointer;
        UINT32 stackBasedRegister = decoder.GetStackBaseRegister();

        if (stackBasedRegister == NO_STACK_BASE_REGISTER)
        {
            basePointer = dac_cast<TADDR>(pRegisterSet->GetSP());
        }
        else
        {
            // REVIEW: Verify that stackBasedRegister is FP
            basePointer = dac_cast<TADDR>(pRegisterSet->GetFP());
        }

        // Reverse PInvoke case.  The embedded reverse PInvoke frame is guaranteed to reside above
        // all outgoing arguments.
        upperBound = dac_cast<TADDR>(basePointer + slot);
    }
    else
    {
#if defined(TARGET_AMD64)
        // Check for a pushed RBP value
        if (GetFramePointer(pMethodInfo, pRegisterSet) == NULL)
        {
            // Unwind the current method context to get the caller's stack pointer
            // and use it as the upper bound for the callee
            SIZE_T  EstablisherFrame;
            PVOID   HandlerData;
            CONTEXT context;
            context.Rsp = pRegisterSet->GetSP();
            context.Rbp = pRegisterSet->GetFP();
            context.Rip = pRegisterSet->GetIP();

            RtlVirtualUnwind(NULL,
                            dac_cast<TADDR>(m_moduleBase),
                            pRegisterSet->IP,
                            (PRUNTIME_FUNCTION)pNativeMethodInfo->runtimeFunction,
                            &context,
                            &HandlerData,
                            &EstablisherFrame,
                            NULL);

            // Skip the return address immediately below the stack pointer
            upperBound = dac_cast<TADDR>(context.Rsp - sizeof(TADDR));
        }
        else
        {
            // In amd64, it is guaranteed that if there is a pushed RBP
            // value at the top of the frame it resides above all outgoing arguments.  Unlike x86,
            // the frame pointer generally points to a location that is separated from the pushed RBP
            // value by an offset that is recorded in the info header.  Recover the address of the
            // pushed RBP value by subtracting this offset.
            upperBound = dac_cast<TADDR>(pRegisterSet->GetFP() - ((PTR_UNWIND_INFO) pUnwindDataBlob)->FrameOffset);
        }

#elif defined(TARGET_ARM) || defined(TARGET_ARM64)
        // Unwind the current method context to get the caller's stack pointer
        // and use it as the upper bound for the callee
        SIZE_T  EstablisherFrame;
        PVOID   HandlerData;
        CONTEXT context;
        context.Sp = pRegisterSet->GetSP();
        context.Fp = pRegisterSet->GetFP();
        context.Pc = pRegisterSet->GetIP();

        RtlVirtualUnwind(NULL,
                        dac_cast<TADDR>(m_moduleBase),
                        pRegisterSet->IP,
                        (PRUNTIME_FUNCTION)pNativeMethodInfo->runtimeFunction,
                        &context,
                        &HandlerData,
                        &EstablisherFrame,
                        NULL);

        upperBound = dac_cast<TADDR>(context.Sp);

#else
        PORTABILITY_ASSERT("GetConservativeUpperBoundForOutgoingArgs");
        upperBound = NULL;
        RhFailFast();
#endif
    }
    return upperBound;
}

bool CoffNativeCodeManager::UnwindStackFrame(MethodInfo *    pMethodInfo,
                                      REGDISPLAY *    pRegisterSet,                 // in/out
                                      PInvokeTransitionFrame**      ppPreviousTransitionFrame)    // out
{
    CoffNativeMethodInfo * pNativeMethodInfo = (CoffNativeMethodInfo *)pMethodInfo;

    size_t unwindDataBlobSize;
    PTR_VOID pUnwindDataBlob = GetUnwindDataBlob(m_moduleBase, pNativeMethodInfo->runtimeFunction, &unwindDataBlobSize);

    PTR_UInt8 p = dac_cast<PTR_UInt8>(pUnwindDataBlob) + unwindDataBlobSize;

    uint8_t unwindBlockFlags = *p++;

    if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) != 0)
        p += sizeof(int32_t);

    if ((unwindBlockFlags & UBF_FUNC_REVERSE_PINVOKE) != 0)
    {
        // Reverse PInvoke transition should be on the main function body only
        assert(pNativeMethodInfo->mainRuntimeFunction == pNativeMethodInfo->runtimeFunction);

        if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) != 0)
            p += sizeof(int32_t);

        GcInfoDecoder decoder(GCInfoToken(p), DECODE_REVERSE_PINVOKE_VAR);
        INT32 slot = decoder.GetReversePInvokeFrameStackSlot();
        assert(slot != NO_REVERSE_PINVOKE_FRAME);

        TADDR basePointer = NULL;
        UINT32 stackBasedRegister = decoder.GetStackBaseRegister();
        if (stackBasedRegister == NO_STACK_BASE_REGISTER)
        {
            basePointer = dac_cast<TADDR>(pRegisterSet->GetSP());
        }
        else
        {
            basePointer = dac_cast<TADDR>(pRegisterSet->GetFP());
        }

        *ppPreviousTransitionFrame = *(PInvokeTransitionFrame**)(basePointer + slot);
        return true;
    }

    *ppPreviousTransitionFrame = NULL;

    CONTEXT context;
    KNONVOLATILE_CONTEXT_POINTERS contextPointers;

#ifdef _DEBUG
    memset(&context, 0xDD, sizeof(context));
    memset(&contextPointers, 0xDD, sizeof(contextPointers));
#endif

#if defined(TARGET_X86)
    #define FOR_EACH_NONVOLATILE_REGISTER(F) \
        F(Eax, pRax) F(Ecx, pRcx) F(Edx, pRdx) F(Ebx, pRbx) F(Ebp, pRbp) F(Esi, pRsi) F(Edi, pRdi)
    #define WORDPTR PDWORD
#elif defined(TARGET_AMD64)
    #define FOR_EACH_NONVOLATILE_REGISTER(F) \
        F(Rax, pRax) F(Rcx, pRcx) F(Rdx, pRdx) F(Rbx, pRbx) F(Rbp, pRbp) F(Rsi, pRsi) F(Rdi, pRdi) \
        F(R8, pR8) F(R9, pR9) F(R10, pR10) F(R11, pR11) F(R12, pR12) F(R13, pR13) F(R14, pR14) F(R15, pR15)
    #define WORDPTR PDWORD64
#elif defined(TARGET_ARM64)
    #define FOR_EACH_NONVOLATILE_REGISTER(F) \
        F(X19, pX19) F(X20, pX20) F(X21, pX21) F(X22, pX22) F(X23, pX23) F(X24, pX24) \
        F(X25, pX25) F(X26, pX26) F(X27, pX27) F(X28, pX28) F(Fp, pFP) F(Lr, pLR)
    #define WORDPTR PDWORD64
#endif // defined(TARGET_X86)

#define REGDISPLAY_TO_CONTEXT(contextField, regDisplayField) \
    contextPointers.contextField = (WORDPTR) pRegisterSet->regDisplayField; \
    if (pRegisterSet->regDisplayField != NULL) context.contextField = *pRegisterSet->regDisplayField;

#define CONTEXT_TO_REGDISPLAY(contextField, regDisplayField) \
    pRegisterSet->regDisplayField = (PTR_UIntNative) contextPointers.contextField;

    FOR_EACH_NONVOLATILE_REGISTER(REGDISPLAY_TO_CONTEXT);

#if defined(TARGET_X86)
    PORTABILITY_ASSERT("CoffNativeCodeManager::UnwindStackFrame");
#elif defined(TARGET_AMD64)
    memcpy(&context.Xmm6, pRegisterSet->Xmm, sizeof(pRegisterSet->Xmm));

    context.Rsp = pRegisterSet->SP;
    context.Rip = pRegisterSet->IP;

    SIZE_T  EstablisherFrame;
    PVOID   HandlerData;

    RtlVirtualUnwind(NULL,
                    dac_cast<TADDR>(m_moduleBase),
                    pRegisterSet->IP,
                    (PRUNTIME_FUNCTION)pNativeMethodInfo->runtimeFunction,
                    &context,
                    &HandlerData,
                    &EstablisherFrame,
                    &contextPointers);

    pRegisterSet->SP = context.Rsp;
    pRegisterSet->IP = context.Rip;

    pRegisterSet->pIP = PTR_PCODE(pRegisterSet->SP - sizeof(TADDR));

    memcpy(pRegisterSet->Xmm, &context.Xmm6, sizeof(pRegisterSet->Xmm));
#elif defined(TARGET_ARM64)
    for (int i = 8; i < 16; i++)
        context.V[i].Low = pRegisterSet->D[i - 8];

    context.Sp = pRegisterSet->SP;
    context.Pc = pRegisterSet->IP;

    SIZE_T  EstablisherFrame;
    PVOID   HandlerData;

    RtlVirtualUnwind(NULL,
                    dac_cast<TADDR>(m_moduleBase),
                    pRegisterSet->IP,
                    (PRUNTIME_FUNCTION)pNativeMethodInfo->runtimeFunction,
                    &context,
                    &HandlerData,
                    &EstablisherFrame,
                    &contextPointers);

    pRegisterSet->SP = context.Sp;
    pRegisterSet->IP = context.Pc;

    pRegisterSet->pIP = contextPointers.Lr;

    for (int i = 8; i < 16; i++)
        pRegisterSet->D[i - 8] = context.V[i].Low;
#endif // defined(TARGET_X86)

    FOR_EACH_NONVOLATILE_REGISTER(CONTEXT_TO_REGDISPLAY);

#undef FOR_EACH_NONVOLATILE_REGISTER
#undef WORDPTR
#undef REGDISPLAY_TO_CONTEXT
#undef CONTEXT_TO_REGDISPLAY

    return true;
}

bool CoffNativeCodeManager::IsUnwindable(PTR_VOID pvAddress)
{
    // RtlVirtualUnwind always can unwind.
    return true;
}

// Convert the return kind that was encoded by RyuJIT to the
// enum used by the runtime.
GCRefKind GetGcRefKind(ReturnKind returnKind)
{
#ifdef TARGET_ARM64
    ASSERT((returnKind >= RT_Scalar) && (returnKind <= RT_ByRef_ByRef));
#else
    ASSERT((returnKind >= RT_Scalar) && (returnKind <= RT_ByRef));
#endif

    return (GCRefKind)returnKind;
}

bool CoffNativeCodeManager::GetReturnAddressHijackInfo(MethodInfo *    pMethodInfo,
                                                REGDISPLAY *    pRegisterSet,       // in
                                                PTR_PTR_VOID *  ppvRetAddrLocation, // out
                                                GCRefKind *     pRetValueKind)      // out
{
    CoffNativeMethodInfo * pNativeMethodInfo = (CoffNativeMethodInfo *)pMethodInfo;

    size_t unwindDataBlobSize;
    PTR_VOID pUnwindDataBlob = GetUnwindDataBlob(m_moduleBase, pNativeMethodInfo->runtimeFunction, &unwindDataBlobSize);

    PTR_UInt8 p = dac_cast<PTR_UInt8>(pUnwindDataBlob) + unwindDataBlobSize;

    uint8_t unwindBlockFlags = *p++;

    if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) != 0)
        p += sizeof(int32_t);

    // Check whether this is a funclet
    if ((unwindBlockFlags & UBF_FUNC_KIND_MASK) != UBF_FUNC_KIND_ROOT)
        return false;

    // Skip hijacking a reverse-pinvoke method - it doesn't get us much because we already synchronize
    // with the GC on the way back to native code.
    if ((unwindBlockFlags & UBF_FUNC_REVERSE_PINVOKE) != 0)
        return false;

    if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) != 0)
        p += sizeof(int32_t);

    // Decode the GC info for the current method to determine its return type
    GcInfoDecoderFlags flags = DECODE_RETURN_KIND;
#if defined(TARGET_ARM) || defined(TARGET_ARM64)
    flags = (GcInfoDecoderFlags)(flags | DECODE_HAS_TAILCALLS);
#endif // TARGET_ARM || TARGET_ARM64
    GcInfoDecoder decoder(GCInfoToken(p), flags);

    *pRetValueKind = GetGcRefKind(decoder.GetReturnKind());

    // Unwind the current method context to the caller's context to get its stack pointer
    // and obtain the location of the return address on the stack
    SIZE_T  EstablisherFrame;
    PVOID   HandlerData;
    CONTEXT context;
#ifdef _DEBUG
    memset(&context, 0xDD, sizeof(context));
#endif

#if defined(TARGET_AMD64)
    context.Rsp = pRegisterSet->GetSP();
    context.Rbp = pRegisterSet->GetFP();
    context.Rip = pRegisterSet->GetIP();

    RtlVirtualUnwind(NULL,
                    dac_cast<TADDR>(m_moduleBase),
                    pRegisterSet->IP,
                    (PRUNTIME_FUNCTION)pNativeMethodInfo->runtimeFunction,
                    &context,
                    &HandlerData,
                    &EstablisherFrame,
                    NULL);

    *ppvRetAddrLocation = (PTR_PTR_VOID)(context.Rsp - sizeof (PVOID));
    return true;
#elif defined(TARGET_ARM64)

    if (decoder.HasTailCalls())
    {
        // Do not hijack functions that have tail calls, since there are two problems:
        // 1. When a function that tail calls another one is hijacked, the LR may be
        //    stored at a different location in the stack frame of the tail call target.
        //    So just by performing tail call, the hijacked location becomes invalid and
        //    unhijacking would corrupt stack by writing to that location.
        // 2. There is a small window after the caller pops LR from the stack in its
        //    epilog and before the tail called function pushes LR in its prolog when
        //    the hijacked return address would not be not on the stack and so we would
        //    not be able to unhijack.
        return false;
    }

    context.Sp = pRegisterSet->GetSP();
    context.Fp = pRegisterSet->GetFP();
    context.Pc = pRegisterSet->GetIP();
    context.Lr = *pRegisterSet->pLR;

    KNONVOLATILE_CONTEXT_POINTERS contextPointers;
#ifdef _DEBUG
    memset(&contextPointers, 0xDD, sizeof(contextPointers));
#endif
    contextPointers.Lr = pRegisterSet->pLR;

    RtlVirtualUnwind(NULL,
        dac_cast<TADDR>(m_moduleBase),
        pRegisterSet->IP,
        (PRUNTIME_FUNCTION)pNativeMethodInfo->runtimeFunction,
        &context,
        &HandlerData,
        &EstablisherFrame,
        &contextPointers);

    if (contextPointers.Lr == pRegisterSet->pLR)
    {
        // This is the case when we are either:
        //
        // 1) In a leaf method that does not push LR on stack, OR
        // 2) In the prolog/epilog of a non-leaf method that has not yet pushed LR on stack
        //    or has LR already popped off.
        return false;
    }

    *ppvRetAddrLocation = (PTR_PTR_VOID)contextPointers.Lr;
    return true;
#else
    return false;
#endif // defined(TARGET_AMD64)
}

PTR_VOID CoffNativeCodeManager::RemapHardwareFaultToGCSafePoint(MethodInfo * pMethodInfo, PTR_VOID controlPC)
{
    // GCInfo decoder needs to know whether execution of the method is aborted
    // while querying for gc-info.  But ICodeManager::EnumGCRef() doesn't receive any
    // flags from mrt. Call to this method is used as a cue to mark the method info
    // as execution aborted. Note - if pMethodInfo was cached, this scheme would not work.
    //
    // If the method has EH, then JIT will make sure the method is fully interruptible
    // and we will have GC-info available at the faulting address as well.

    CoffNativeMethodInfo * pNativeMethodInfo = (CoffNativeMethodInfo *)pMethodInfo;
    pNativeMethodInfo->executionAborted = true;

    return controlPC;
}

struct CoffEHEnumState
{
    PTR_UInt8 pMethodStartAddress;
    PTR_UInt8 pEHInfo;
    uint32_t uClause;
    uint32_t nClauses;
};

// Ensure that CoffEHEnumState fits into the space reserved by EHEnumState
static_assert(sizeof(CoffEHEnumState) <= sizeof(EHEnumState), "CoffEHEnumState too big");

bool CoffNativeCodeManager::EHEnumInit(MethodInfo * pMethodInfo, PTR_VOID * pMethodStartAddress, EHEnumState * pEHEnumStateOut)
{
    assert(pMethodInfo != NULL);
    assert(pMethodStartAddress != NULL);
    assert(pEHEnumStateOut != NULL);

    CoffNativeMethodInfo * pNativeMethodInfo = (CoffNativeMethodInfo *)pMethodInfo;
    CoffEHEnumState * pEnumState = (CoffEHEnumState *)pEHEnumStateOut;

    size_t unwindDataBlobSize;
    PTR_VOID pUnwindDataBlob = GetUnwindDataBlob(m_moduleBase, pNativeMethodInfo->mainRuntimeFunction, &unwindDataBlobSize);

    PTR_UInt8 p = dac_cast<PTR_UInt8>(pUnwindDataBlob) + unwindDataBlobSize;

    uint8_t unwindBlockFlags = *p++;

    if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) != 0)
        p += sizeof(int32_t);

    // return if there is no EH info associated with this method
    if ((unwindBlockFlags & UBF_FUNC_HAS_EHINFO) == 0)
    {
        return false;
    }

    *pMethodStartAddress = dac_cast<PTR_VOID>(m_moduleBase + pNativeMethodInfo->mainRuntimeFunction->BeginAddress);

    pEnumState->pMethodStartAddress = dac_cast<PTR_UInt8>(*pMethodStartAddress);
    pEnumState->pEHInfo = dac_cast<PTR_UInt8>(m_moduleBase + *dac_cast<PTR_Int32>(p));
    pEnumState->uClause = 0;
    pEnumState->nClauses = VarInt::ReadUnsigned(pEnumState->pEHInfo);

    return true;
}

bool CoffNativeCodeManager::EHEnumNext(EHEnumState * pEHEnumState, EHClause * pEHClauseOut)
{
    assert(pEHEnumState != NULL);
    assert(pEHClauseOut != NULL);

    CoffEHEnumState * pEnumState = (CoffEHEnumState *)pEHEnumState;
    if (pEnumState->uClause >= pEnumState->nClauses)
        return false;
    pEnumState->uClause++;

    pEHClauseOut->m_tryStartOffset = VarInt::ReadUnsigned(pEnumState->pEHInfo);

    uint32_t tryEndDeltaAndClauseKind = VarInt::ReadUnsigned(pEnumState->pEHInfo);
    pEHClauseOut->m_clauseKind = (EHClauseKind)(tryEndDeltaAndClauseKind & 0x3);
    pEHClauseOut->m_tryEndOffset = pEHClauseOut->m_tryStartOffset + (tryEndDeltaAndClauseKind >> 2);

    // For each clause, we have up to 4 integers:
    //      1)  try start offset
    //      2)  (try length << 2) | clauseKind
    //      3)  if (typed || fault || filter)    { handler start offset }
    //      4a) if (typed)                       { type RVA }
    //      4b) if (filter)                      { filter start offset }
    //
    // The first two integers have already been decoded

    switch (pEHClauseOut->m_clauseKind)
    {
    case EH_CLAUSE_TYPED:
        pEHClauseOut->m_handlerAddress = pEnumState->pMethodStartAddress + VarInt::ReadUnsigned(pEnumState->pEHInfo);

        // Read target type
        {
            // @TODO: Compress EHInfo using type table index scheme
            // https://github.com/dotnet/corert/issues/972
            uint32_t typeRVA = *((PTR_UInt32&)pEnumState->pEHInfo)++;
            pEHClauseOut->m_pTargetType = dac_cast<PTR_VOID>(m_moduleBase + typeRVA);
        }
        break;
    case EH_CLAUSE_FAULT:
        pEHClauseOut->m_handlerAddress = pEnumState->pMethodStartAddress + VarInt::ReadUnsigned(pEnumState->pEHInfo);
        break;
    case EH_CLAUSE_FILTER:
        pEHClauseOut->m_handlerAddress = pEnumState->pMethodStartAddress + VarInt::ReadUnsigned(pEnumState->pEHInfo);
        pEHClauseOut->m_filterAddress = pEnumState->pMethodStartAddress + VarInt::ReadUnsigned(pEnumState->pEHInfo);
        break;
    default:
        UNREACHABLE_MSG("unexpected EHClauseKind");
    }

    return true;
}

PTR_VOID CoffNativeCodeManager::GetOsModuleHandle()
{
    return dac_cast<PTR_VOID>(m_moduleBase);
}

PTR_VOID CoffNativeCodeManager::GetMethodStartAddress(MethodInfo * pMethodInfo)
{
    CoffNativeMethodInfo * pNativeMethodInfo = (CoffNativeMethodInfo *)pMethodInfo;
    return dac_cast<PTR_VOID>(m_moduleBase + pNativeMethodInfo->mainRuntimeFunction->BeginAddress);
}

void * CoffNativeCodeManager::GetClasslibFunction(ClasslibFunctionId functionId)
{
    uint32_t id = (uint32_t)functionId;

    if (id >= m_nClasslibFunctions)
        return nullptr;

    return m_pClasslibFunctions[id];
}

PTR_VOID CoffNativeCodeManager::GetAssociatedData(PTR_VOID ControlPC)
{
    if (dac_cast<TADDR>(ControlPC) < dac_cast<TADDR>(m_pvManagedCodeStartRange) ||
        dac_cast<TADDR>(m_pvManagedCodeStartRange) + m_cbManagedCodeRange <= dac_cast<TADDR>(ControlPC))
    {
        return NULL;
    }

    TADDR relativePC = dac_cast<TADDR>(ControlPC) - m_moduleBase;

    int MethodIndex = LookupUnwindInfoForMethod((uint32_t)relativePC, m_pRuntimeFunctionTable, 0, m_nRuntimeFunctionTable - 1);
    if (MethodIndex < 0)
        return NULL;

    PTR_RUNTIME_FUNCTION pRuntimeFunction = m_pRuntimeFunctionTable + MethodIndex;

    size_t unwindDataBlobSize;
    PTR_VOID pUnwindDataBlob = GetUnwindDataBlob(m_moduleBase, pRuntimeFunction, &unwindDataBlobSize);

    PTR_UInt8 p = dac_cast<PTR_UInt8>(pUnwindDataBlob) + unwindDataBlobSize;

    uint8_t unwindBlockFlags = *p++;
    if ((unwindBlockFlags & UBF_FUNC_HAS_ASSOCIATED_DATA) == 0)
        return NULL;

    uint32_t dataRVA = *(uint32_t*)p;
    return dac_cast<PTR_VOID>(m_moduleBase + dataRVA);
}

extern "C" void __stdcall RegisterCodeManager(ICodeManager * pCodeManager, PTR_VOID pvStartRange, uint32_t cbRange);
extern "C" bool __stdcall RegisterUnboxingStubs(PTR_VOID pvStartRange, uint32_t cbRange);

extern "C"
bool RhRegisterOSModule(void * pModule,
                        void * pvManagedCodeStartRange, uint32_t cbManagedCodeRange,
                        void * pvUnboxingStubsStartRange, uint32_t cbUnboxingStubsRange,
                        void ** pClasslibFunctions, uint32_t nClasslibFunctions)
{
    PIMAGE_DOS_HEADER pDosHeader = (PIMAGE_DOS_HEADER)pModule;
    PIMAGE_NT_HEADERS pNTHeaders = (PIMAGE_NT_HEADERS)((TADDR)pModule + pDosHeader->e_lfanew);

    IMAGE_DATA_DIRECTORY * pRuntimeFunctions = &(pNTHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXCEPTION]);

    NewHolder<CoffNativeCodeManager> pCoffNativeCodeManager = new (nothrow) CoffNativeCodeManager((TADDR)pModule,
        pvManagedCodeStartRange, cbManagedCodeRange,
        dac_cast<PTR_RUNTIME_FUNCTION>((TADDR)pModule + pRuntimeFunctions->VirtualAddress),
        pRuntimeFunctions->Size / sizeof(RUNTIME_FUNCTION),
        pClasslibFunctions, nClasslibFunctions);

    if (pCoffNativeCodeManager == nullptr)
        return false;

    RegisterCodeManager(pCoffNativeCodeManager, pvManagedCodeStartRange, cbManagedCodeRange);

    if (!RegisterUnboxingStubs(pvUnboxingStubsStartRange, cbUnboxingStubsRange))
    {
        return false;
    }

    pCoffNativeCodeManager.SuppressRelease();

    return true;
}
