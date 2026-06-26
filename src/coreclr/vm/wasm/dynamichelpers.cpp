// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include "callingconvention.h"
#include "cgensys.h"
#include "readytorun.h"
#include "readytoruninfo.h"
#include "jitinterface.h"
#include "loaderallocator.hpp"

extern "C" SIZE_T STDCALL DynamicHelperWorker(TransitionBlock * pTransitionBlock, TADDR * pCell, DWORD sectionIndex, Module * pModule, INT frameFlags);

extern "C" SIZE_T STDCALL DelayLoad_HelperImpl(TransitionBlock* pTransitionBlock, READYTORUN_IMPORT_THUNK_PORTABLE_ENTRYPOINT* pImportThunkEntry, uint8_t *moduleBase, int32_t rvaOfModuleFixup, INT frameFlags)
{
    Module** ppModule = (Module**)(moduleBase + rvaOfModuleFixup);
    return DynamicHelperWorker(pTransitionBlock, (TADDR*)(moduleBase + pImportThunkEntry->RelocOffset), (DWORD)-1, *ppModule, frameFlags);
}

extern "C" __attribute__((naked)) SIZE_T STDCALL DelayLoad_Helper(TransitionBlock* pTransitionBlock, READYTORUN_IMPORT_THUNK_PORTABLE_ENTRYPOINT* pImportThunkEntry, uint8_t *moduleBase, int32_t rvaOfModuleFixup)
{
    asm ("local.get 0\n" /* Capture pTransitionBlock onto the stack for calling DelayLoad_MethodCallImpl function. This also happens to be the callersFramePointer */
         "local.get 0\n" /* Capture callersFramePointer onto the stack for setting the __stack_pointer */
         "global.get __stack_pointer\n" /* Get current value of stack global */
         "local.set 0\n"  /* Overwrite local 0 with the previous __stack_pointer value so it can be restored after the call */
         "global.set __stack_pointer\n" /* Set stack global to the initial value of callersFramePointer, which is the current stack pointer for the interpreter call */
         "local.get 1\n" /* Load pImportThunkEntry argument onto the stack for calling DelayLoad_MethodCallImpl function*/
         "local.get 2\n" /* Load moduleBase argument onto the stack for calling DelayLoad_MethodCallImpl function*/
         "local.get 3\n" /* Load rvaOfModuleFixup argument onto the stack for calling DelayLoad_MethodCallImpl function*/
         "i32.const 0\n" /* Load frameFlags argument onto the stack for calling DelayLoad_MethodCallImpl function. For this variant we want 0 as the flag */
         "call %0\n" /* Call the actual implementation function */
         "local.get 0\n" /* Reload the saved previous __stack_pointer value for restoration into the stack global */
         "global.set __stack_pointer\n"
         "return" :: "i" (DelayLoad_HelperImpl));
}

extern "C" void STDCALL DelayLoad_Helper_Obj()
{
    PORTABILITY_ASSERT("DelayLoad_Helper_Obj is not implemented on wasm");
}

extern "C" void STDCALL DelayLoad_Helper_ObjObj()
{
    PORTABILITY_ASSERT("DelayLoad_Helper_ObjObj is not implemented on wasm");
}

extern "C" void DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull();
extern "C" void DynamicHelper_GenericDictionaryLookup_Class_TestForNull();
extern "C" void DynamicHelper_GenericDictionaryLookup_Class();
extern "C" void DynamicHelper_GenericDictionaryLookup_Class_0_0();
extern "C" void DynamicHelper_GenericDictionaryLookup_Class_0_1();
extern "C" void DynamicHelper_GenericDictionaryLookup_Class_0_2();
extern "C" void DynamicHelper_GenericDictionaryLookup_Class_0_3();
extern "C" void DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull();
extern "C" void DynamicHelper_GenericDictionaryLookup_Method_TestForNull();
extern "C" void DynamicHelper_GenericDictionaryLookup_Method();
extern "C" void DynamicHelper_GenericDictionaryLookup_Method_0();
extern "C" void DynamicHelper_GenericDictionaryLookup_Method_1();
extern "C" void DynamicHelper_GenericDictionaryLookup_Method_2();
extern "C" void DynamicHelper_GenericDictionaryLookup_Method_3();
extern "C" void DynamicHelper_GenericDictionaryLookup_Class_UseHelper();
extern "C" void DynamicHelper_GenericDictionaryLookup_Method_UseHelper();

extern "C" PCODE g_pMethodWithSlotAndModule;
extern "C" PCODE g_pClassWithSlotAndModule;

PCODE DynamicHelpers::CreateDictionaryLookupHelper(LoaderAllocator * pAllocator, CORINFO_RUNTIME_LOOKUP * pLookup, DWORD dictionaryIndexAndSlot, Module * pModule)
{
    STANDARD_VM_CONTRACT;

    AllocMemTracker amTracker;

    PCODE helperAddress = GetDictionaryLookupHelper(pLookup->helper);

    WORD slotOffset = (WORD)(dictionaryIndexAndSlot & 0xFFFF) * sizeof(Dictionary*);

    // It's available only via the run-time helper function
    PCODE helper = (PCODE)NULL;
    if (pLookup->indirections == CORINFO_USEHELPER)
    {
        GenericHandleArgs * pArgs = (GenericHandleArgs *)amTracker.Track(pAllocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(GenericHandleArgs))));
        pArgs->dictionaryIndexAndSlot = dictionaryIndexAndSlot;
        pArgs->signature = pLookup->signature;
        pArgs->module = (CORINFO_MODULE_HANDLE)pModule;

        PCODE helperFunc;
        if (helperAddress == g_pClassWithSlotAndModule)
        {
            helperFunc = (PCODE)DynamicHelper_GenericDictionaryLookup_Class_UseHelper;
        }
        else
        {
            _ASSERTE(helperAddress == g_pMethodWithSlotAndModule);
            helperFunc = (PCODE)DynamicHelper_GenericDictionaryLookup_Method_UseHelper;
        }

        // The UseHelper stubs only ever read the HandleArgs field of the stub data, but the rest of the
        // structure is zero-initialized for consistency with the other stubs.
        GenericDictionaryDynamicHelperStubData dictLookupData = {0};
        dictLookupData.HandleArgs = pArgs;

        GenericDictionaryDynamicHelperStubData_PortableEntryPoint *pDictLookupData = (GenericDictionaryDynamicHelperStubData_PortableEntryPoint *)amTracker.Track(pAllocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(GenericDictionaryDynamicHelperStubData_PortableEntryPoint))));
        pDictLookupData->stubData = dictLookupData;
        pDictLookupData->HelperFunctionTableIndex = helperFunc;

        PCODE result = (PCODE)pDictLookupData;
        amTracker.SuppressRelease();
        return result;
    }
    else
    {
        PCODE result;
        GenericDictionaryDynamicHelperStubData dictLookupData = {0};
        dictLookupData.SizeOffset = (UINT32)pLookup->sizeOffset;
        dictLookupData.SlotOffset = slotOffset;
        bool needsDictLookupData = false;

        if (pLookup->indirections == 3)
        {
            // Class!
            _ASSERTE(helperAddress == g_pClassWithSlotAndModule);
            _ASSERTE(pLookup->offsets[0] == offsetof(MethodTable, m_pPerInstInfo));
            dictLookupData.SecondIndir = (UINT32)pLookup->offsets[1];
            dictLookupData.LastIndir = (UINT32)pLookup->offsets[2];
            if (pLookup->testForNull && pLookup->sizeOffset != CORINFO_NO_SIZE_CHECK)
            {
                helper = (PCODE)DynamicHelper_GenericDictionaryLookup_Class_SizeCheck_TestForNull;
                needsDictLookupData = true;
            }
            else if (pLookup->testForNull)
            {
                helper = (PCODE)DynamicHelper_GenericDictionaryLookup_Class_TestForNull;
                needsDictLookupData = true;
            }
            else
            {
                _ASSERTE(pLookup->sizeOffset == CORINFO_NO_SIZE_CHECK);
                // SecondIndir is in bytes, but actual indirections into the table are always pointer aligned. 
                // A value of 0 indicates that the second indirection is into the first generic dictionary of
                // the type, which is the most common access pattern for generics. For Dictionary<TKey,TValue>,
                // a SecondIndir of 0, and a LastIndir of 0 would indicate the MethodTable pointer of TKey,
                // and if LastIndir was sizeof(TADDR) it would access the MethodTable pointer of TValue and so on.
                if ((dictLookupData.SecondIndir == 0) && (dictLookupData.LastIndir <= sizeof(TADDR) * 3))
                {
                    needsDictLookupData = false;
                    // Since LastIndir is in bytes, but actual indirections into the table are always pointer
                    // aligned, we can divide by sizeof(TADDR) to compute the possible cases here.
                    switch (dictLookupData.LastIndir / sizeof(TADDR))
                    {
                        case 0:
                            helper = (PCODE)DynamicHelper_GenericDictionaryLookup_Class_0_0;
                            break;
                        case 1:
                            helper = (PCODE)DynamicHelper_GenericDictionaryLookup_Class_0_1;
                            break;
                        case 2:
                            helper = (PCODE)DynamicHelper_GenericDictionaryLookup_Class_0_2;
                            break;
                        case 3:
                            helper = (PCODE)DynamicHelper_GenericDictionaryLookup_Class_0_3;
                            break;
                    }
                }
                else
                {
                    helper = (PCODE)DynamicHelper_GenericDictionaryLookup_Class;
                    needsDictLookupData = true;
                }
            }
        }
        else if (pLookup->indirections == 2)
        {
            // Method!
            _ASSERTE(helperAddress == g_pMethodWithSlotAndModule);
            _ASSERTE(pLookup->offsets[0] == offsetof(InstantiatedMethodDesc, m_pPerInstInfo));
            dictLookupData.LastIndir = (UINT32)pLookup->offsets[1];
            _ASSERTE(dictLookupData.SecondIndir == 0); // There are only 2 indirections, so there is no "SecondIndir" value to set, and it should be 0.
            if (pLookup->testForNull && pLookup->sizeOffset != CORINFO_NO_SIZE_CHECK)
            {
                helper = (PCODE)DynamicHelper_GenericDictionaryLookup_Method_SizeCheck_TestForNull;
                needsDictLookupData = true;
            }
            else if (pLookup->testForNull)
            {
                helper = (PCODE)DynamicHelper_GenericDictionaryLookup_Method_TestForNull;
                needsDictLookupData = true;
            }
            else
            {
                _ASSERTE(pLookup->sizeOffset == CORINFO_NO_SIZE_CHECK);
                if (dictLookupData.LastIndir <= sizeof(TADDR) * 3)
                {
                    needsDictLookupData = false;
                    // Since LastIndir is in bytes, but actual indirections into the table are always pointer aligned, we can divide by sizeof(TADDR) to compute the possible cases here.
                    switch (dictLookupData.LastIndir / sizeof(TADDR))
                    {
                        case 0:
                            helper = (PCODE)DynamicHelper_GenericDictionaryLookup_Method_0;
                            break;
                        case 1:
                            helper = (PCODE)DynamicHelper_GenericDictionaryLookup_Method_1;
                            break;
                        case 2:
                            helper = (PCODE)DynamicHelper_GenericDictionaryLookup_Method_2;
                            break;
                        case 3:
                            helper = (PCODE)DynamicHelper_GenericDictionaryLookup_Method_3;
                            break;
                    }
                }
                else
                {
                    helper = (PCODE)DynamicHelper_GenericDictionaryLookup_Method;
                    needsDictLookupData = true;
                }
            }
        }

        if (needsDictLookupData)
        {
            GenericHandleArgs * pArgs = (GenericHandleArgs *)amTracker.Track(pAllocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(GenericHandleArgs))));
            pArgs->dictionaryIndexAndSlot = dictionaryIndexAndSlot;
            pArgs->signature = pLookup->signature;
            pArgs->module = (CORINFO_MODULE_HANDLE)pModule;

            dictLookupData.HandleArgs = pArgs;

            GenericDictionaryDynamicHelperStubData_PortableEntryPoint *pDictLookupData = (GenericDictionaryDynamicHelperStubData_PortableEntryPoint *)amTracker.Track(pAllocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(GenericDictionaryDynamicHelperStubData_PortableEntryPoint))));

            pDictLookupData->stubData = dictLookupData;
            pDictLookupData->HelperFunctionTableIndex = helper;
            result = (PCODE)pDictLookupData;
        }
        else
        {
            // The simple helpers do not need any stub data, but the portable entrypoint calling convention
            // requires the result to point at a location whose first word is the helper's function table index.
            PCODE *pHelperEntryPoint = (PCODE *)amTracker.Track(pAllocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(PCODE))));
            *pHelperEntryPoint = helper;
            result = (PCODE)pHelperEntryPoint;
        }

        amTracker.SuppressRelease();
        return result;
    }
}
