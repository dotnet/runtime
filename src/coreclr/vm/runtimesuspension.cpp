// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "runtimesuspension.h"
#include "siginfo.hpp"

enum class TaskletReturnType : int32_t
{
    // These return types are OS/architecture specific. For instance, Arm64 supports returning structs in a register pair. This is also incomplete and doesn't handle floating point, vector registers, etc.
    Integer,
    ObjectReference,
    ByReference
};

enum RegisterToRestore
{
    Rbx,
    Rbp,
    Rdi,
    Rsi,
    R12,
    R13,
    R14,
    R15,
    Xmm6,
    Xmm7,
    Xmm8,
    Xmm9,
    Xmm10,
    Xmm11,
    Xmm12,
    Xmm13,
    Xmm14,
    Xmm15,
    ReturnRegisters, // End sequence marker. Associated offset is for the return register, but the actual return value is stashed after the register data
    ReturnRegistersNoFrame, // End sequence marker. Associated offset is for the return register, but the actual return value is stashed after the register data
};

struct RegRestore
{
    RegisterToRestore reg;
    uint32_t offset;
};

struct StackDataInfo
{
    void CleanupStackDataInfo()
    {
        if (ByRefOffsets != NULL)
            free(ByRefOffsets);
        if (ObjectRefOffsets != NULL)
            free(ObjectRefOffsets);
        if (RegistersToRestore != NULL)
            free(RegistersToRestore);
    }
    uint32_t StackRequirement;
    uint32_t UnrecordedDataSize; // From the restored RSP to the data chunk, how many bytes are skipped
    uint32_t StackDataSize;
    uint32_t ReturnAddressOffset;
    uint32_t cByRefs;
    uint32_t cObjectRefs;
    uint32_t CbArgs;
    int32_t* ByRefOffsets = NULL; // Negative numbers indicate pinned byrefs
    uint32_t* ObjectRefOffsets = NULL;
    RegRestore* RegistersToRestore = NULL;
};

struct Tasklet
{
    Tasklet* pTaskletNextInStack;
    Tasklet* pTaskletNextInLiveList;
    Tasklet* pTaskletPrevInLiveList;
    uint8_t* pStackData;
    uintptr_t restoreIPAddress;
    StackDataInfo* pStackDataInfo;
    TaskletReturnType taskletReturnType;
};

struct RuntimeAsyncReturnValue
{
    uintptr_t _obj;
    uintptr_t _ptr;
    TaskletReturnType _returnType;
};

struct ByRefAdjustment
{
    uint8_t* pOldLocation;
    uint32_t Size;
    uintptr_t Adjustment;
};

bool RelocAtAddress(ByRefAdjustment* pAdjustment, uintptr_t* pData);
void RelocAtAddress(ByRefAdjustment* pAdjustment, uint8_t* pDataToAdjustAddress)
{
    uintptr_t *pData = (uintptr_t*)pDataToAdjustAddress;
    RelocAtAddress(pAdjustment, pData);
}

bool RelocAtAddress(ByRefAdjustment* pAdjustment, uintptr_t* pData)
{
    if ((*pData >= (uintptr_t)pAdjustment->pOldLocation) && ((*pData - (uintptr_t)pAdjustment->pOldLocation) < (uintptr_t)pAdjustment->Size))
    {
        *pData += pAdjustment->Adjustment;
        return true;
    }
    return false;
}

struct RestoreFunctionLocals
{
    // Anonymous union of data for return value registers
    union 
    {
        uintptr_t integerRegister;
    };
    uintptr_t AddressInMethodToRestoreTo;
private:
    uint8_t *pFutureRSPLocation;
    uintptr_t ReturnAddress;
public:
    uint8_t *GetFutureRSPLocation() { return pFutureRSPLocation; }  // TODO This should be some simple math off of the this pointer
    uintptr_t GetReturnAddress() { return ReturnAddress; }  // TODO This should be some simple math off of the this pointer
};

// The actual restoration function which needs to be written in assembly, sets up enough of a frame on top of the current RSP to call this function
// Then, once this function returns, iterates through the RegRestore to change over the saved registers to the new values
extern "C" RegRestore* PlatformIndependentRestore(Tasklet* tasklet, RuntimeAsyncReturnValue* returnValueToFillIn, RestoreFunctionLocals *restoreLocals)
{
    // Compute what needs to be adjusted in terms of byrefs for 
    ByRefAdjustment adjustment;
    adjustment.pOldLocation = tasklet->pStackData;
    adjustment.Size = tasklet->pStackDataInfo->StackDataSize;

    uint8_t* pNewLocation = restoreLocals->GetFutureRSPLocation() + tasklet->pStackDataInfo->UnrecordedDataSize;
    adjustment.Adjustment = ((uintptr_t)pNewLocation) - ((uintptr_t)adjustment.pOldLocation);

    // Adjust all pointers to the stack data that we are about to move, both in this frame, and in the caller frames
    Tasklet *pTaskletToAdjustByrefsOn = tasklet;
    do
    {
        uint32_t cByRefs = pTaskletToAdjustByrefsOn->pStackDataInfo->cByRefs;
        uint32_t* byRefOffsets = (uint32_t*)pTaskletToAdjustByrefsOn->pStackDataInfo->ByRefOffsets;
        uint8_t* taskletData = pTaskletToAdjustByrefsOn->pStackData;
        for (uint32_t iByRef = 0; iByRef < cByRefs; iByRef++)
        {
            RelocAtAddress(&adjustment, taskletData + byRefOffsets[iByRef]);
        }
        pTaskletToAdjustByrefsOn = pTaskletToAdjustByrefsOn->pTaskletNextInStack;
    } while (pTaskletToAdjustByrefsOn != NULL);
    
    StackDataInfo *pStackDataInfo = tasklet->pStackDataInfo;

    // Copy most of the memory
    memcpy(restoreLocals->GetFutureRSPLocation() + pStackDataInfo->UnrecordedDataSize, tasklet->pStackData, pStackDataInfo->StackDataSize);

    // Update the return address on the stack
    *(uintptr_t *)(restoreLocals->GetFutureRSPLocation() + pStackDataInfo->ReturnAddressOffset) = restoreLocals->GetReturnAddress();

    restoreLocals->AddressInMethodToRestoreTo = tasklet->restoreIPAddress;

    switch (returnValueToFillIn->_returnType)
    {
        case TaskletReturnType::Integer:
            restoreLocals->integerRegister = returnValueToFillIn->_ptr;
            break;
        case TaskletReturnType::ObjectReference:
            restoreLocals->integerRegister = returnValueToFillIn->_obj;
            break;

        default:
            // Not yet implemented
            _ASSERTE(FALSE);
    }

    return pStackDataInfo->RegistersToRestore;

    // The rest of the code will do something like...
    // This is written for X86 which is ... not the initial target
/*
    mov ecx, [eax + offsetof(pStackData)]
    mov edx, [eax + offsetof(pStackDataInfo)]
    mov edx, [eax + offsetof(RegistersToRestore)] ; EDX now points at a list of registers to restore, terminated by the RegisterToRestore::ReturnRegisters value
    add ecx, [edx + offsetof(StackDataSize)] ; ECX now points at the data to copy into the various restored register
    ; We have eax as a scratch register if we need it
        .data
tbl     dq      ReturnRegisters, CheckEBX, CheckESI, ...            ;table 
        .code
RegRestoreLoop:
    mov eax, [edx]
    jmp [tbl + eax*4]
ReturnRegisters:
    mov eax, [esp-X]
    mov edx, [esp-X+4]
    MOVDQU xmm0, [esp-X]
    mov ecx, [esp-Y] ; Load address of where to jmp into ecx
    add esp, Z
    jmp ecx
    // Repeat for every saved register type
CheckEBX:
    cmp EBX, [edx]
    mov eax, [edx+4]
    mov [edx + 4], ebx
    mov ebx, eax
    add edx, 8
    jmp RegRestoreLoop

CheckRBX:

    ends with
    cmp 
*/
}

struct TaskletCaptureData
{
    bool inRunOfAsyncMethods;
    int framesCaptured = 0;
    StackCrawlMark* stackMark;
    Tasklet* firstTasklet = NULL;
    Tasklet* lastTasklet = NULL;
    uintptr_t stackLimit;
    uintptr_t stackToIgnoreFromPreviousFrame = 0; // Portions of the stack which have been copied into a previous frame
    uintptr_t returnStructSize = 0;
    CQuickArrayList<uintptr_t *> ActiveByRefsToStack;
    void AddCopiedByRef(uintptr_t currentStackTop, uintptr_t *newByRef)
    {
        if ((*newByRef >= currentStackTop) && (*newByRef < stackLimit))
        {
            ActiveByRefsToStack.PushNoThrow(newByRef);
            // TODO PushNoThrow has a return value
        }
    }

    void ApplyByRefRelocsToNewlyAllocatedTasklet(ByRefAdjustment adjustment)
    {
        if (ActiveByRefsToStack.Size() > 0)
        {
            for (SIZE_T iByRef = ActiveByRefsToStack.Size(); iByRef > 0;)
            {
                iByRef--;

                if (RelocAtAddress(&adjustment, ActiveByRefsToStack[iByRef]))
                {
                    // ByRef was reloc'd, and therefore doesn't need to be handled anymore
                    ActiveByRefsToStack[iByRef] = ActiveByRefsToStack[ActiveByRefsToStack.Size() - 1];
                    ActiveByRefsToStack.Pop();
                }
            }
        }
    }
};

struct RuntimeSuspensionEnumData
{
    RuntimeSuspensionEnumData(const CQuickArrayList<RegRestore>& restoreRegLocations, REGDISPLAY* pRD_, TaskletCaptureData *taskletCaptureData, CrawlFrame *_pCf, StackDataInfo* _pStackDataInfo, uint8_t* _pStackData) :
        RestoreRegLocations(restoreRegLocations),
        pRD(pRD_),
        pTaskletCaptureData(taskletCaptureData),
        pCf(_pCf),
        pStackDataInfo(_pStackDataInfo),
        pStackData(_pStackData)
    {
    }

    CQuickArrayList<uint32_t> ObjectRefOffsets;
    CQuickArrayList<int32_t> ByRefOffsets;
    const CQuickArrayList<RegRestore>& RestoreRegLocations;
    REGDISPLAY* pRD;
    TaskletCaptureData *pTaskletCaptureData;
    CrawlFrame *pCf;
    StackDataInfo* pStackDataInfo;
    uint8_t* pStackData;

    uint32_t GetOffsetForReg(RegisterToRestore nonVolatileReg)
    {
        for (SIZE_T i = 0; i < RestoreRegLocations.Size(); i++)
        {
            if (RestoreRegLocations[i].reg == nonVolatileReg)
            {
                return RestoreRegLocations[i].offset;
            }
        }
        _ASSERTE(!"Attempting to get offset for reg which doesn't have an offset recorded!");
        return 0;
    }
};

StackWalkAction CaptureTaskletsCore(CrawlFrame* pCf, VOID* data)
{
    CONTRACTL
    {
        THROWS;
;        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;


    MethodDesc *pFunc = pCf->GetFunction();

    /* We asked to be called back only for functions */
    _ASSERTE(pFunc);

    TaskletCaptureData* taskletCaptureData = (TaskletCaptureData*) data;
    if (taskletCaptureData->firstTasklet == NULL)
    {
        if (!IsInCurrentFrame(pCf->GetRegisterSet(), taskletCaptureData->stackMark))
        {
            // Move to next frame, we haven't found anything
            return SWA_CONTINUE;
        }
    }
    else if (!pCf->GetFunction()->IsAsync2Method())
    {
        // We must be in the wrapper thunk
        _ASSERTE(pCf->GetFunction()->IsAsyncThunkMethod() || (strcmp(pCf->GetFunction()->GetName(), "ResumptionFunc") == 0));

        if (taskletCaptureData->ActiveByRefsToStack.Size() != 0)
        {
            // We must have to deal with a return value managed as a byref
            _ASSERTE(!"This is not yet implemented, the thought is to require that the return address be stored somewhere with a pointer to the start of the return value, and then scan the ActiveByRefsToStack to find the lowest address, and treat that as where to find the start of the return buffer. Or maybe we should record it somewhere?");
        }
        return SWA_ABORT;
    }
    

    uint8_t* pTopOfStackInFunction = (uint8_t*)pCf->GetRegisterSet()->SP;
    uint32_t sizeofArgStack = (uint32_t)pCf->GetFunction()->SizeOfArgStack();
    uint8_t* pBottomOfStackInFunction = (uint8_t*)::GetSP(pCf->GetRegisterSet()->pCallerContext) + sizeofArgStack;
    uint32_t sizeofEntireMeaningfulStack = (uint32_t)(pBottomOfStackInFunction - pTopOfStackInFunction);
    sizeofEntireMeaningfulStack -= (uint32_t)taskletCaptureData->stackToIgnoreFromPreviousFrame;

    uint8_t *pStackData = (uint8_t*)malloc(sizeofEntireMeaningfulStack);
    memcpy(pStackData, pTopOfStackInFunction + taskletCaptureData->stackToIgnoreFromPreviousFrame, sizeofEntireMeaningfulStack);

    ByRefAdjustment byrefAdjustment;
    byrefAdjustment.pOldLocation = pTopOfStackInFunction + taskletCaptureData->stackToIgnoreFromPreviousFrame;
    byrefAdjustment.Adjustment = ((uintptr_t)pStackData) - ((uintptr_t)byrefAdjustment.pOldLocation);
    byrefAdjustment.Size = sizeofEntireMeaningfulStack;

    StackDataInfo stackDataInfo;
    stackDataInfo.StackRequirement = (uint32_t)(::GetSP(pCf->GetRegisterSet()->pCallerContext) - pCf->GetRegisterSet()->SP) + sizeofArgStack;
    stackDataInfo.StackDataSize = sizeofEntireMeaningfulStack;
    stackDataInfo.UnrecordedDataSize = (uint32_t)taskletCaptureData->stackToIgnoreFromPreviousFrame;

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    uintptr_t returnAddressLocation = (uintptr_t) (EECodeManager::GetCallerSp(pCf->GetRegisterSet()) - sizeof(void*));
#elif defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    uintptr_t returnAddressLocation = (uintptr_t) pCf->GetRegisterSet()->pCallerContextPointers->Ra;
#elif defined(TARGET_ARM) || defined(TARGET_ARM64)
    uintptr_t returnAddressLocation = (uintptr_t) pCf->GetRegisterSet()->pCallerContextPointers->Lr;
#else
    PORTABILITY_ASSERT("Platform NYI");
#endif
    stackDataInfo.ReturnAddressOffset = (uint32_t)(returnAddressLocation - (uintptr_t)pCf->GetRegisterSet()->SP);

    CQuickArrayList<RegRestore> savedRegRestoreData;
    bool hasRBPFrame = false;


// Find restored reg, and record its location in the frame, so that it can be properly restored. Then update the copied value to be the "current" value of the register, not what we saved off on entry to the function
#define DISCOVER_RESTORED_REG(REGNAME) \
    if (pCf->GetRegisterSet()->pCallerContextPointers->REGNAME != pCf->GetRegisterSet()->pCurrentContextPointers->REGNAME)                                                                                         \
    {                                                                                                                                                                                                              \
        if ((void*)&pCf->GetRegisterSet()->pCallerContextPointers->REGNAME == (void*)&pCf->GetRegisterSet()->pCallerContextPointers->Rbp)                                                                          \
            hasRBPFrame = true;                                                                                                                                                                                    \
        RegRestore restoreData; restoreData.reg = RegisterToRestore::REGNAME;                                                                                                                                      \
        restoreData.offset = (uint32_t)((uint8_t*)pCf->GetRegisterSet()->pCallerContextPointers->REGNAME - (uint8_t*)pCf->GetRegisterSet()->SP); savedRegRestoreData.Push(restoreData);                            \
        memcpy((pStackData - taskletCaptureData->stackToIgnoreFromPreviousFrame) + restoreData.offset, &pCf->GetRegisterSet()->pCurrentContext->REGNAME, sizeof(pCf->GetRegisterSet()->pCurrentContext->REGNAME)); \
    }


#include "restoreregs_for_runtimesuspension.h"
#undef DISCOVER_RESTORED_REG

    RegRestore returnData;
    if (hasRBPFrame)
    {
        returnData.reg = RegisterToRestore::ReturnRegisters;
    }
    else
    {
        returnData.reg = RegisterToRestore::ReturnRegistersNoFrame;
    }
    returnData.offset = 0;
    savedRegRestoreData.Push(returnData);


    ICodeManager * pCM = pCf->GetCodeManager();
    _ASSERTE(pCM != NULL);

    unsigned flags = pCf->GetCodeManagerFlags();

    GCEnumCallback enumGCRefs = []( 
        LPVOID          hCallback,      // callback data
        OBJECTREF*      pObject,        // address of object-reference we are reporting
        uint32_t        flags )
    {
        RuntimeSuspensionEnumData *pEnumData = (RuntimeSuspensionEnumData*)hCallback;

        // Determine if pObject points at a non-volatile register slot
        uint32_t offset;

        if (false) {}
#define DISCOVER_RESTORED_REG(REGNAME) else if ((void*)pEnumData->pRD->pCurrentContextPointers->REGNAME == (void*)pObject) { offset = pEnumData->GetOffsetForReg(RegisterToRestore::REGNAME); }
#include "restoreregs_for_runtimesuspension.h"
#undef DISCOVER_RESTORED_REG
        else
        {
            // Must be within the stack frame itself
            offset = (uint32_t)((uint8_t*)pObject - (uint8_t*)pEnumData->pRD->SP);
        }

        bool isInterior = !!(flags & GC_CALL_INTERIOR);
        bool isPinned = !!(flags & GC_CALL_PINNED);
        _ASSERTE(!isPinned || isInterior);

        if (!isInterior)
        {
            pEnumData->ObjectRefOffsets.PushNoThrow(offset);
            // TODO PushNoThrow has a return value
        }
        else
        {
            int32_t byRefOffset = (int32_t)offset;

            pEnumData->pTaskletCaptureData->AddCopiedByRef(pEnumData->pCf->GetRegisterSet()->SP, (uintptr_t*)(pEnumData->pStackData + offset - pEnumData->pStackDataInfo->UnrecordedDataSize));
            if (isPinned)
            {
                byRefOffset = -byRefOffset;
            }

            pEnumData->ByRefOffsets.PushNoThrow(offset);
            // TODO PushNoThrow has a return value
        }
    };

    RuntimeSuspensionEnumData enumData(savedRegRestoreData, pCf->GetRegisterSet(), taskletCaptureData, pCf, &stackDataInfo, pStackData);

    pCM->EnumGcRefs(pCf->GetRegisterSet(),
                    pCf->GetCodeInfo(),
                    flags | NoGcDecoderValidation,
                    enumGCRefs,
                    &enumData,
                    NO_OVERRIDE_OFFSET);

// HACK for frame pointer handling
    for (int iRegister = 0; iRegister < savedRegRestoreData.Size(); iRegister++)
    {
        if (savedRegRestoreData[iRegister].reg == RegisterToRestore::Rbp)
        {
            // If we have Rbp as a saved register, report it as a byref, as its probably the frame pointer and thus in need of adjusting
            enumGCRefs((void*)&enumData, (OBJECTREF*)(pCf->GetRegisterSet()->SP + savedRegRestoreData[iRegister].offset), GC_CALL_INTERIOR);
        }
    }

    stackDataInfo.ByRefOffsets = (int32_t*)malloc(enumData.ByRefOffsets.Size() * sizeof(int32_t));
    memcpy(stackDataInfo.ByRefOffsets, enumData.ByRefOffsets.Ptr(), enumData.ByRefOffsets.Size() * sizeof(int32_t));
    stackDataInfo.ObjectRefOffsets = (uint32_t*)malloc(enumData.ObjectRefOffsets.Size() * sizeof(uint32_t));
    memcpy(stackDataInfo.ObjectRefOffsets, enumData.ObjectRefOffsets.Ptr(), enumData.ObjectRefOffsets.Size() * sizeof(uint32_t));
    stackDataInfo.RegistersToRestore = (RegRestore*)malloc(savedRegRestoreData.Size() * sizeof(RegRestore));
    memcpy(stackDataInfo.RegistersToRestore, savedRegRestoreData.Ptr(), savedRegRestoreData.Size() * sizeof(RegRestore));

    stackDataInfo.cByRefs = (uint32_t)enumData.ByRefOffsets.Size();
    stackDataInfo.cObjectRefs = (uint32_t)enumData.ObjectRefOffsets.Size();
    stackDataInfo.CbArgs = sizeofArgStack;

    StackDataInfo *pStackDataInfo = (StackDataInfo*)malloc(sizeof(StackDataInfo));
    *pStackDataInfo = stackDataInfo;

    MetaSig msig(pCf->GetFunction());
    ArgIterator argit(&msig);
    TaskletReturnType taskletReturnType;
#if TARGET_AMD64
    if (argit.HasRetBuffArg())
    {
        taskletReturnType = TaskletReturnType::ByReference; // In this case, on Windows AMD64 the abi specifies that the return value in RAX is the address of the ret buffer
    }
    else
    {
        TypeHandle thRet = msig.GetRetTypeHandleThrowing();
        CorElementType corElementType = thRet.GetInternalCorElementType();
        if (thRet.IsTypeDesc())
        {
            taskletReturnType = TaskletReturnType::Integer;
        }
        else
        {
            if (thRet.AsMethodTable()->IsValueType() && thRet.AsMethodTable()->ContainsPointers())
            {
                taskletReturnType = TaskletReturnType::ObjectReference;
            }
            else
            {
                // These asserts don't check all the FP ret cases, but they cover at least some of it
                _ASSERTE(thRet.GetInternalCorElementType() != ELEMENT_TYPE_R4);
                _ASSERTE(thRet.GetInternalCorElementType() != ELEMENT_TYPE_R8);
                taskletReturnType = TaskletReturnType::Integer;
            }
        }
    }
#else
PORTABILIT_ASSERT()
#endif



    Tasklet *pTasklet = (Tasklet*)malloc(sizeof(Tasklet));
    memset(pTasklet, 0, sizeof(Tasklet));
    pTasklet->pStackData = pStackData;
    pTasklet->restoreIPAddress = (uintptr_t)GetControlPC(pCf->GetRegisterSet());
    pTasklet->pStackDataInfo = pStackDataInfo;
    pTasklet->taskletReturnType = taskletReturnType;

    if (taskletCaptureData->firstTasklet == NULL)
    {
        taskletCaptureData->firstTasklet = pTasklet;
        taskletCaptureData->lastTasklet = pTasklet;
    }
    else
    {
        taskletCaptureData->lastTasklet->pTaskletNextInStack = pTasklet;
        taskletCaptureData->lastTasklet = pTasklet;
    }
    taskletCaptureData->framesCaptured++;
    
    // Apply byrefs to newly allocated stuff
    taskletCaptureData->ApplyByRefRelocsToNewlyAllocatedTasklet(byrefAdjustment);

    return SWA_CONTINUE;
}


extern "C" Tasklet* QCALLTYPE RuntimeSuspension_CaptureTasklets(QCall::StackCrawlMarkHandle stackMark, uint8_t* returnValue, uint8_t useReturnValueHandle, void* taskAsyncData, Tasklet** lastTasklet, int32_t* pFramesCaptured)
{
    GCX_COOP();

    BEGINFORBIDGC();
#ifdef _DEBUG
    GCForbidLoaderUseHolder forbidLoaderUse;
#endif

    TaskletCaptureData cdata;
    cdata.stackMark = stackMark;
    cdata.stackLimit = (uintptr_t)taskAsyncData;
    GetThread()->StackWalkFrames(CaptureTaskletsCore, &cdata, FUNCTIONSONLY);

    // We should have captured a full stack here. 
    *lastTasklet = cdata.lastTasklet;
    *pFramesCaptured = cdata.framesCaptured;
    ENDFORBIDGC();
    return cdata.firstTasklet;
}

extern "C" void QCALLTYPE RuntimeSuspension_DeleteTasklet(Tasklet* tasklet)
{
    tasklet->pStackDataInfo->CleanupStackDataInfo();
    free(tasklet->pStackData);
    free(tasklet->pStackDataInfo);
    free(tasklet);
}
