// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CLASS.CPP
//

#include "common.h"

#include "dllimport.h"
#include "dllimportcallback.h"
#include "fieldmarshaler.h"
#include "customattribute.h"
#include "encee.h"
#include "typestring.h"
#include "dbginterface.h"

#ifdef FEATURE_COMINTEROP
#include "comcallablewrapper.h"
#include "clrtocomcall.h"
#include "runtimecallablewrapper.h"
#endif // FEATURE_COMINTEROP

//#define DEBUG_LAYOUT
#define SORT_BY_RID

#ifndef DACCESS_COMPILE
#include "methodtablebuilder.h"
#endif

#ifndef DACCESS_COMPILE



//*******************************************************************************
void *EEClass::operator new(
    size_t size,
    LoaderHeap *pHeap,
    AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    void *p = pamTracker->Track(pHeap->AllocMem(S_SIZE_T(size)));

    // No need to memset since this memory came from VirtualAlloc'ed memory
    // memset (p, 0, size);

    return p;
}

//*******************************************************************************
void EEClass::Destruct(MethodTable * pOwningMT)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        FORBID_FAULT;
        PRECONDITION(pOwningMT != NULL);
    }
    CONTRACTL_END

#ifdef PROFILING_SUPPORTED
    // If profiling, then notify the class is getting unloaded.
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackClasses());
        {
            // Calls to the profiler callback may throw, or otherwise fail, if
            // the profiler AVs/throws an unhandled exception/etc. We don't want
            // those failures to affect the runtime, so we'll ignore them.
            //
            // Note that the profiler callback may turn around and make calls into
            // the profiling runtime that may throw. This try/catch block doesn't
            // protect the profiler against such failures. To protect the profiler
            // against that, we will need try/catch blocks around all calls into the
            // profiling API.
            //
            // (Bug #26467)
            //

            FAULT_NOT_FATAL();

            EX_TRY
            {
                GCX_PREEMP();

                (&g_profControlBlock)->ClassUnloadStarted((ClassID) pOwningMT);
            }
            EX_CATCH
            {
                // The exception here came from the profiler itself. We'll just
                // swallow the exception, since we don't want the profiler to bring
                // down the runtime.
            }
            EX_END_CATCH(RethrowTerminalExceptions);
        }
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED

#ifdef FEATURE_COMINTEROP
    // clean up any COM Data
    if (m_pccwTemplate)
    {
        m_pccwTemplate->Release();
        m_pccwTemplate = NULL;
    }


#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
    if (GetComClassFactory())
    {
        GetComClassFactory()->Cleanup();
    }
#endif // FEATURE_COMINTEROP_UNMANAGED_ACTIVATION
#endif // FEATURE_COMINTEROP


    if (IsDelegate())
    {
        DelegateEEClass* pDelegateEEClass = (DelegateEEClass*)this;

        if (pDelegateEEClass->m_pStaticCallStub)
        {
            // Collect data to remove stub entry from StubManager if
            // stub is deleted.
            BYTE* entry = (BYTE*)pDelegateEEClass->m_pStaticCallStub->GetEntryPoint();
            UINT length = pDelegateEEClass->m_pStaticCallStub->GetNumCodeBytes();

            ExecutableWriterHolder<Stub> stubWriterHolder(pDelegateEEClass->m_pStaticCallStub, sizeof(Stub));
            BOOL fStubDeleted = stubWriterHolder.GetRW()->DecRef();
            if (fStubDeleted)
            {
                StubLinkStubManager::g_pManager->RemoveStubRange(entry, length);
            }
        }
        if (pDelegateEEClass->m_pInstRetBuffCallStub)
        {
            ExecutableWriterHolder<Stub> stubWriterHolder(pDelegateEEClass->m_pInstRetBuffCallStub, sizeof(Stub));
            stubWriterHolder.GetRW()->DecRef();
        }
        // While m_pMultiCastInvokeStub is also a member,
        // it is owned by the m_pMulticastStubCache, not by the class
        // - it is shared across classes. So we don't decrement
        // its ref count here
    }

#ifdef FEATURE_COMINTEROP
    if (GetSparseCOMInteropVTableMap() != NULL)
        delete GetSparseCOMInteropVTableMap();
#endif // FEATURE_COMINTEROP

#ifdef PROFILING_SUPPORTED
    // If profiling, then notify the class is getting unloaded.
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackClasses());
        {
            // See comments in the call to ClassUnloadStarted for details on this
            // FAULT_NOT_FATAL marker and exception swallowing.
            FAULT_NOT_FATAL();
            EX_TRY
            {
                GCX_PREEMP();
                (&g_profControlBlock)->ClassUnloadFinished((ClassID) pOwningMT, S_OK);
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(RethrowTerminalExceptions);
        }
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED

}

//*******************************************************************************
/*static*/ EEClass *
EEClass::CreateMinimalClass(LoaderHeap *pHeap, AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    return new (pHeap, pamTracker) EEClass();
}


//*******************************************************************************

//-----------------------------------------------------------------------------------
// Note: this only loads the type to CLASS_DEPENDENCIES_LOADED as this can be called
// indirectly from DoFullyLoad() as part of accessibility checking.
//-----------------------------------------------------------------------------------
MethodTable *MethodTable::LoadEnclosingMethodTable(ClassLoadLevel targetLevel)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        MODE_ANY;
    }
    CONTRACTL_END

    mdTypeDef tdEnclosing = GetEnclosingCl();

    if (tdEnclosing == mdTypeDefNil)
    {
        return NULL;
    }

    return ClassLoader::LoadTypeDefThrowing(GetModule(),
                                            tdEnclosing,
                                            ClassLoader::ThrowIfNotFound,
                                            ClassLoader::PermitUninstDefOrRef,
                                            tdNoTypes,
                                            targetLevel
                                            ).GetMethodTable();

}

#ifdef FEATURE_METADATA_UPDATER

//*******************************************************************************
VOID EEClass::FixupFieldDescForEnC(MethodTable * pMT, EnCFieldDesc *pFD, mdFieldDef fieldDef)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    Module * pModule = pMT->GetModule();
    IMDInternalImport *pImport = pModule->GetMDImport();

#ifdef LOGGING
    if (LoggingEnabled())
    {
        LPCSTR szFieldName;
        if (FAILED(pImport->GetNameOfFieldDef(fieldDef, &szFieldName)))
        {
            szFieldName = "Invalid FieldDef record";
        }
        LOG((LF_ENC, LL_INFO100, "EEClass::FixupFieldDescForEnC '%s' (0x%08x)\n", szFieldName, fieldDef));
    }
#endif //LOGGING


#ifdef _DEBUG
    BOOL shouldBreak = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EncFixupFieldBreak);
    if (shouldBreak > 0) {
        _ASSERTE(!"EncFixupFieldBreak");
    }
#endif // _DEBUG

    // MethodTableBuilder uses the stacking allocator for most of it's
    // working memory requirements, so this makes sure to free the memory
    // once this function is out of scope.
    ACQUIRE_STACKING_ALLOCATOR(pStackingAllocator);

    // Collect the attributes for the field
    mdToken fieldDefs[1] = { fieldDef };
    DWORD fieldAttrs[1];
    IfFailThrow(pImport->GetFieldDefProps(fieldDefs[0], &fieldAttrs[0]));

    MethodTableBuilder::bmtMetaDataInfo bmtMetaData;
    bmtMetaData.cFields = ARRAY_SIZE(fieldDefs);
    bmtMetaData.pFields = fieldDefs;
    bmtMetaData.pFieldAttrs = fieldAttrs;

    // We need to alloc the memory, but don't have to fill it in.  InitializeFieldDescs
    // will copy pFD (1st arg) into here.
    FieldDesc* fieldDescs[1];
    MethodTableBuilder::bmtMethAndFieldDescs bmtMFDescs;
    bmtMFDescs.ppFieldDescList = fieldDescs;

    MethodTableBuilder::bmtFieldPlacement bmtFP;

    // This simulates the environment that BuildMethodTableThrowing creates
    // just enough to run InitializeFieldDescs
    MethodTableBuilder::bmtErrorInfo bmtError;
    bmtError.pModule = pModule;
    bmtError.cl = pMT->GetCl();
    bmtError.dMethodDefInError = mdTokenNil;
    bmtError.szMethodNameForError = NULL;

    MethodTableBuilder::bmtInternalInfo bmtInternal;
    bmtInternal.pModule = pModule;
    bmtInternal.pInternalImport = pImport;
    bmtInternal.pParentMT = pMT->GetParentMethodTable();

    MethodTableBuilder::bmtProperties bmtProp;
    bmtProp.fIsValueClass = !!pMT->IsValueType();

    MethodTableBuilder::bmtEnumFieldInfo bmtEnumFields(bmtInternal.pInternalImport);

    if (pFD->IsStatic())
    {
        bmtEnumFields.dwNumStaticFields = 1;
    }
    else
    {
        _ASSERTE(!pMT->IsValueType());
        bmtEnumFields.dwNumInstanceFields = 1;
    }

    // We shouldn't have to fill this in b/c we're not allowed to EnC value classes, or
    // anything else with layout info associated with it.
    // Provide 2, 1 placeholder and 1 for the actual field - see BuildMethodTableThrowing().
    LayoutRawFieldInfo layoutRawFieldInfos[2];

    // If not NULL, it means there are some by-value fields, and this contains an entry for each instance or static field,
    // which is NULL if not a by value field, and points to the EEClass of the field if a by value field.  Instance fields
    // come first, statics come second.
    MethodTable** pByValueClassCache = NULL;

    AllocMemTracker dummyAmTracker;

    EEClass* pClass = pMT->GetClass();
    MethodTableBuilder builder(pMT, pClass,
                               pStackingAllocator,
                               &dummyAmTracker);

    TypeHandle thisTH(pMT);
    SigTypeContext typeContext(thisTH);
    MethodTableBuilder::bmtGenericsInfo genericsInfo;
    genericsInfo.typeContext = typeContext;

    builder.SetBMTData(pMT->GetLoaderAllocator(),
                       &bmtError,
                       &bmtProp,
                       NULL,
                       NULL,
                       NULL,
                       &bmtMetaData,
                       NULL,
                       &bmtMFDescs,
                       &bmtFP,
                       &bmtInternal,
                       NULL,
                       NULL,
                       &genericsInfo,
                       &bmtEnumFields);

    {
        GCX_PREEMP();
        unsigned totalDeclaredFieldSize = 0;
        builder.InitializeFieldDescs(pFD,
                                 layoutRawFieldInfos,
                                 &bmtInternal,
                                 &genericsInfo,
                                 &bmtMetaData,
                                 &bmtEnumFields,
                                 &bmtError,
                                 &pByValueClassCache,
                                 &bmtMFDescs,
                                 &bmtFP,
                                 &totalDeclaredFieldSize);
    }

    dummyAmTracker.SuppressRelease();

    pFD->SetMethodTable(pMT);

    // We set this when we first created the FieldDesc, but initializing the FieldDesc
    // may have overwritten it so we need to set it again.
    pFD->SetEnCNew();

    return;
}

//---------------------------------------------------------------------------------------
//
// AddField - called when a new field is added by EnC
//
// Since instances of this class may already exist on the heap, we can't change the
// runtime layout of the object to accommodate the new field.  Instead we hang the field
// off the syncblock (for instance fields) or in the FieldDesc for static fields.
//
// Here we just create the FieldDesc and link it to the class.  The actual storage will
// be created lazily on demand.
//
HRESULT EEClass::AddField(MethodTable* pMT, mdFieldDef fieldDef, FieldDesc** ppNewFD)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(pMT != NULL);
        PRECONDITION(ppNewFD != NULL);
    }
    CONTRACTL_END;

    HRESULT hr;
    Module * pModule = pMT->GetModule();
    IMDInternalImport *pImport = pModule->GetMDImport();

#ifdef LOGGING
    if (LoggingEnabled())
    {
        LPCSTR szFieldName;
        if (FAILED(pImport->GetNameOfFieldDef(fieldDef, &szFieldName)))
        {
            szFieldName = "Invalid FieldDef record";
        }
        LOG((LF_ENC, LL_INFO100, "EEClass::AddField '%s' tok:0x%08x\n", szFieldName, fieldDef));
    }
#endif //LOGGING

    // We can only add fields to normal classes
    if (pMT->HasLayout() || pMT->IsValueType())
    {
        return CORDBG_E_ENC_CANT_ADD_FIELD_TO_VALUE_OR_LAYOUT_CLASS;
    }

    // We only add private fields.
    // This may not be strictly necessary, but helps avoid any semantic confusion with
    // existing code etc.
    DWORD dwFieldAttrs;
    IfFailThrow(pImport->GetFieldDefProps(fieldDef, &dwFieldAttrs));

    FieldDesc* pNewFD;
    if (FAILED(hr = AddFieldDesc(pMT, fieldDef, dwFieldAttrs, &pNewFD)))
    {
        LOG((LF_ENC, LL_INFO100, "EEClass::AddField failed: 0x%08x\n", hr));
        return hr;
    }

    // Store the FieldDesc into the module's field list
    // This should not be done for instantiated types. Only fields on the
    // open type are added to the module directly. This check is a
    // consequence of calling AddField() for EnC static fields on generics.
    if (!pMT->HasInstantiation())
    {
        pModule->EnsureFieldDefCanBeStored(fieldDef);
        pModule->EnsuredStoreFieldDef(fieldDef, pNewFD);
    }
    LOG((LF_ENC, LL_INFO100, "EEClass::AddField Added pFD:%p for token 0x%08x\n",
        pNewFD, fieldDef));

    // If the type is generic, then we need to update all existing instantiated types
    if (pMT->IsGenericTypeDefinition())
    {
        LOG((LF_ENC, LL_INFO100, "EEClass::AddField Looking for existing instantiations in all assemblies\n"));

        PTR_AppDomain pDomain = AppDomain::GetCurrentDomain();
        AppDomain::AssemblyIterator appIt = pDomain->IterateAssembliesEx((AssemblyIterationFlags)(kIncludeLoaded | kIncludeExecution));

        bool isStaticField = !!pNewFD->IsStatic();
        CollectibleAssemblyHolder<DomainAssembly*> pDomainAssembly;
        while (appIt.Next(pDomainAssembly.This()) && SUCCEEDED(hr))
        {
            Module* pMod = pDomainAssembly->GetModule();
            LOG((LF_ENC, LL_INFO100, "EEClass::AddField Checking: %s mod:%p\n", pMod->GetDebugName(), pMod));

            EETypeHashTable* paramTypes = pMod->GetAvailableParamTypes();
            EETypeHashTable::Iterator it(paramTypes);
            EETypeHashEntry* pEntry;
            while (paramTypes->FindNext(&it, &pEntry))
            {
                TypeHandle th = pEntry->GetTypeHandle();
                if (th.IsTypeDesc())
                    continue;

                // For instance fields we only update instantiations of the generic MethodTable we updated above.
                // For static fields we update the the canonical version and instantiations.
                MethodTable* pMTMaybe = th.AsMethodTable();
                if ((!isStaticField && !pMTMaybe->IsCanonicalMethodTable())
                    || !pMT->HasSameTypeDefAs(pMTMaybe))
                {
                    continue;
                }

                FieldDesc* pNewFDUnused;
                if (FAILED(AddFieldDesc(pMTMaybe, fieldDef, dwFieldAttrs, &pNewFDUnused)))
                {
                    LOG((LF_ENC, LL_INFO100, "EEClass::AddField failed: 0x%08x\n", hr));
                    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_FAILFAST,
                        W("Failed to add field to existing instantiated type instance"));
                    return E_FAIL;
                }
            }
        }
    }

    // Success, return the new FieldDesc
    *ppNewFD = pNewFD;

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// AddFieldDesc - called when a new FieldDesc needs to be created and added for EnC
//
HRESULT EEClass::AddFieldDesc(
    MethodTable* pMT,
    mdMethodDef fieldDef,
    DWORD dwFieldAttrs,
    FieldDesc** ppNewFD)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(pMT != NULL);
        PRECONDITION(ppNewFD != NULL);
    }
    CONTRACTL_END;

    LOG((LF_ENC, LL_INFO100, "EEClass::AddFieldDesc pMT:%p, %s <- tok:0x%08x attrs:%u\n",
        pMT, pMT->debug_m_szClassName, fieldDef, dwFieldAttrs));

    Module* pModule = pMT->GetModule();
    IMDInternalImport* pImport = pModule->GetMDImport();
    LoaderAllocator* pAllocator = pMT->GetLoaderAllocator();

    // Here we allocate a FieldDesc and set just enough info to be able to fix it up later
    // when we're running in managed code.
    EnCAddedFieldElement *pAddedField = (EnCAddedFieldElement *)
        (void*)pAllocator->GetHighFrequencyHeap()->AllocMem_NoThrow(S_SIZE_T(sizeof(EnCAddedFieldElement)));
    if (!pAddedField)
    {
        return E_OUTOFMEMORY;
    }
    pAddedField->Init( fieldDef, IsFdStatic(dwFieldAttrs) );

    EnCFieldDesc *pNewFD = &pAddedField->m_fieldDesc;

    // Get the EnCEEClassData for this class
    // Don't adjust EEClass stats b/c EnC fields shouldn't touch EE data structures.
    // We'll just update our private EnC structures instead.
    _ASSERTE(pModule->IsEditAndContinueEnabled());
    EnCEEClassData* pEnCClass = ((EditAndContinueModule*)pModule)->GetEnCEEClassData(pMT);
    if (!pEnCClass)
        return E_FAIL;

    // Add the field element to the list of added fields for this class
    pEnCClass->AddField(pAddedField);
    pNewFD->SetMethodTable(pMT);

    // Record that we are adding a new static field. Static generic fields
    // are added for currently non-instantiated types during type construction.
    // We want to limit the cost of making the check at that time so we use
    // a bit on the EEClass to indicate we've added a static field and it should
    // be checked.
    if (IsFdStatic(dwFieldAttrs))
        pMT->GetClass()->SetHasEnCStaticFields();

    // Success, return the new FieldDesc
    *ppNewFD = pNewFD;

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// AddMethod - called when a new method is added by EnC
//
// The method has already been added to the metadata with token methodDef.
// Create a new MethodDesc for the method, add to the associated EEClass and
// update any existing Generic instantiations if the MethodTable represents a
// generic type.
//
HRESULT EEClass::AddMethod(MethodTable* pMT, mdMethodDef methodDef, MethodDesc** ppMethod)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(pMT != NULL);
        PRECONDITION(methodDef != mdTokenNil);
    }
    CONTRACTL_END;

    HRESULT hr;
    Module* pModule = pMT->GetModule();
    IMDInternalImport* pImport = pModule->GetMDImport();

#ifdef LOGGING
    if (LoggingEnabled())
    {
        LPCSTR szMethodName;
        if (FAILED(pImport->GetNameOfMethodDef(methodDef, &szMethodName)))
        {
            szMethodName = "Invalid MethodDef record";
        }
        LOG((LF_ENC, LL_INFO100, "EEClass::AddMethod '%s' tok:0x%08x\n", szMethodName, methodDef));
    }
#endif //LOGGING

    DWORD dwDescrOffset;
    DWORD dwImplFlags;
    if (FAILED(pImport->GetMethodImplProps(methodDef, &dwDescrOffset, &dwImplFlags)))
        return COR_E_BADIMAGEFORMAT;

    DWORD dwMemberAttrs;
    if (FAILED(pImport->GetMethodDefProps(methodDef, &dwMemberAttrs)))
        return COR_E_BADIMAGEFORMAT;

    // Refuse to add other special cases
    if (IsReallyMdPinvokeImpl(dwMemberAttrs)
        || (pMT->IsInterface() && !IsMdStatic(dwMemberAttrs))
        || IsMiRuntime(dwImplFlags))
    {
        _ASSERTE(! "**Error** EEClass::AddMethod only IL private non-virtual methods are supported");
        LOG((LF_ENC, LL_INFO100, "**Error** EEClass::AddMethod only IL private non-virtual methods are supported\n"));
        return CORDBG_E_ENC_EDIT_NOT_SUPPORTED;
    }

#ifdef _DEBUG
    // Validate that this methodDef correctly has a parent typeDef
    mdTypeDef   parentTypeDef;
    if (FAILED(pImport->GetParentToken(methodDef, &parentTypeDef)))
    {
        _ASSERTE(! "**Error** EEClass::AddMethod parent token not found");
        LOG((LF_ENC, LL_INFO100, "**Error** EEClass::AddMethod parent token not found\n"));
        return E_FAIL;
    }
#endif // _DEBUG

    MethodDesc* pNewMD;
    if (FAILED(hr = AddMethodDesc(pMT, methodDef, dwImplFlags, dwMemberAttrs, &pNewMD)))
    {
        LOG((LF_ENC, LL_INFO100, "EEClass::AddMethod failed: 0x%08x\n", hr));
        return hr;
    }

    // Store the new MethodDesc into the collection for this class
    pModule->EnsureMethodDefCanBeStored(methodDef);
    pModule->EnsuredStoreMethodDef(methodDef, pNewMD);

    LOG((LF_ENC, LL_INFO100, "EEClass::AddMethod Added pMD:%p for token 0x%08x\n",
        pNewMD, methodDef));

    // If the type is generic, then we need to update all existing instantiated types
    if (pMT->IsGenericTypeDefinition())
    {
        LOG((LF_ENC, LL_INFO100, "EEClass::AddMethod Looking for existing instantiations in all assemblies\n"));

        PTR_AppDomain pDomain = AppDomain::GetCurrentDomain();
        AppDomain::AssemblyIterator appIt = pDomain->IterateAssembliesEx((AssemblyIterationFlags)(kIncludeLoaded | kIncludeExecution));

        CollectibleAssemblyHolder<DomainAssembly*> pDomainAssembly;
        while (appIt.Next(pDomainAssembly.This()) && SUCCEEDED(hr))
        {
            Module* pMod = pDomainAssembly->GetModule();
            LOG((LF_ENC, LL_INFO100, "EEClass::AddMethod Checking: %s mod:%p\n", pMod->GetDebugName(), pMod));

            EETypeHashTable* paramTypes = pMod->GetAvailableParamTypes();
            EETypeHashTable::Iterator it(paramTypes);
            EETypeHashEntry* pEntry;
            while (paramTypes->FindNext(&it, &pEntry))
            {
                TypeHandle th = pEntry->GetTypeHandle();
                if (th.IsTypeDesc())
                    continue;

                // Only update instantiations of the generic MethodTable we updated above.
                MethodTable* pMTMaybe = th.AsMethodTable();
                if (!pMTMaybe->IsCanonicalMethodTable() || !pMT->HasSameTypeDefAs(pMTMaybe))
                {
                    continue;
                }

                MethodDesc* pNewMDUnused;
                if (FAILED(AddMethodDesc(pMTMaybe, methodDef, dwImplFlags, dwMemberAttrs, &pNewMDUnused)))
                {
                    LOG((LF_ENC, LL_INFO100, "EEClass::AddMethod failed: 0x%08x\n", hr));
                    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_FAILFAST,
                        W("Failed to add method to existing instantiated type instance"));
                    return E_FAIL;
                }
            }
        }
    }

    // Success - return the new MethodDesc
    if (ppMethod)
        *ppMethod = pNewMD;

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// AddMethodDesc - called when a new MethodDesc needs to be created and added for EnC
//
HRESULT EEClass::AddMethodDesc(
    MethodTable* pMT,
    mdMethodDef methodDef,
    DWORD dwImplFlags,
    DWORD dwMemberAttrs,
    MethodDesc** ppNewMD)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        PRECONDITION(pMT != NULL);
        PRECONDITION(methodDef != mdTokenNil);
        PRECONDITION(ppNewMD != NULL);
    }
    CONTRACTL_END;

    LOG((LF_ENC, LL_INFO100, "EEClass::AddMethodDesc pMT:%p, %s <- tok:0x%08x flags:%u attrs:%u\n",
        pMT, pMT->debug_m_szClassName, methodDef, dwImplFlags, dwMemberAttrs));

    HRESULT hr;
    Module* pModule = pMT->GetModule();
    IMDInternalImport* pImport = pModule->GetMDImport();

    // Check if signature is generic.
    ULONG sigLen;
    PCCOR_SIGNATURE sig;
    if (FAILED(hr = pImport->GetSigOfMethodDef(methodDef, &sigLen, &sig)))
        return hr;
    uint32_t callConv = CorSigUncompressData(sig);
    DWORD classification = (callConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
        ? mcInstantiated
        : mcIL;

    LoaderAllocator* pAllocator = pMT->GetLoaderAllocator();

    // [TODO] OOM: InitMethodDesc will allocate loaderheap memory but leak it
    //   on failure. This AllocMemTracker should be replaced with a real one.
    AllocMemTracker dummyAmTracker;

    // Create a new MethodDescChunk to hold the new MethodDesc.
    // Create the chunk somewhere we'll know is within range of the VTable.
    MethodDescChunk *pChunk = MethodDescChunk::CreateChunk(pAllocator->GetHighFrequencyHeap(),
                                                            1,   // methodDescCount
                                                            classification,
                                                            TRUE, // fNonVtableSlot
                                                            TRUE, // fNativeCodeSlot
                                                            pMT,
                                                            &dummyAmTracker);

    // Get the new MethodDesc (Note: The method desc memory is zero initialized)
    MethodDesc* pNewMD = pChunk->GetFirstMethodDesc();

    EEClass* pClass = pMT->GetClass();

        // This method runs on a debugger thread. Debugger threads do not have Thread object
        // that caches StackingAllocator, use a local StackingAllocator instead.
    StackingAllocator stackingAllocator;

    MethodTableBuilder::bmtInternalInfo bmtInternal;
    bmtInternal.pModule = pModule;
    bmtInternal.pInternalImport = NULL;
    bmtInternal.pParentMT = NULL;

    MethodTableBuilder builder(pMT,
                                pClass,
                                &stackingAllocator,
                                &dummyAmTracker);

    builder.SetBMTData(pMT->GetLoaderAllocator(),
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        NULL,
                        &bmtInternal);

    // Initialize the new MethodDesc
    EX_TRY
    {
        INDEBUG(LPCSTR debug_szMethodName);
        INDEBUG(if (FAILED(pImport->GetNameOfMethodDef(methodDef, &debug_szMethodName))) { debug_szMethodName = "Invalid MethodDef record"; });
        builder.InitMethodDesc(pNewMD,
                                classification,
                                methodDef,
                                dwImplFlags,
                                dwMemberAttrs,
                                TRUE,   // fEnC
                                0,      // RVA - non-zero only for NDirect
                                pImport,
                                NULL
                                COMMA_INDEBUG(debug_szMethodName)
                                COMMA_INDEBUG(pMT->GetDebugClassName())
                                COMMA_INDEBUG(NULL)
                                );

        pNewMD->SetTemporaryEntryPoint(pAllocator, &dummyAmTracker);

        // [TODO] if an exception is thrown, asserts will fire in EX_CATCH_HRESULT()
        // during an EnC operation due to the debugger thread not being able to
        // transition to COOP mode.
    }
    EX_CATCH_HRESULT(hr);
    if (S_OK != hr)
        return hr;

    dummyAmTracker.SuppressRelease();

    _ASSERTE(pNewMD->IsEnCAddedMethod());

    pNewMD->SetSlot(MethodTable::NO_SLOT);    // we can't ever use the slot for EnC methods

    pClass->AddChunk(pChunk);

    *ppNewMD = pNewMD;

    return S_OK;
}

#endif // FEATURE_METADATA_UPDATER

//---------------------------------------------------------------------------------------
//
// Check that the class type parameters are used consistently in this signature blob
// in accordance with their variance annotations
// The signature is assumed to be well-formed but indices and arities might not be correct
//
BOOL
EEClass::CheckVarianceInSig(
    DWORD               numGenericArgs,
    BYTE *              pVarianceInfo,
    Module *            pModule,
    SigPointer          psig,
    CorGenericParamAttr position)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pVarianceInfo == NULL)
        return TRUE;

    CorElementType typ;
    IfFailThrow(psig.GetElemType(&typ));

    switch (typ)
    {
        case ELEMENT_TYPE_STRING:
        case ELEMENT_TYPE_U:
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R4:
        case ELEMENT_TYPE_R8:
        case ELEMENT_TYPE_VOID:
        case ELEMENT_TYPE_OBJECT:
        case ELEMENT_TYPE_TYPEDBYREF:
        case ELEMENT_TYPE_MVAR:
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_VALUETYPE:
            return TRUE;

        case ELEMENT_TYPE_VAR:
        {
            uint32_t index;
            IfFailThrow(psig.GetData(&index));

            // This will be checked later anyway; so give up and don't indicate a variance failure
            if (index < 0 || index >= numGenericArgs)
                return TRUE;

            // Non-variant parameters are allowed to appear anywhere
            if (pVarianceInfo[index] == gpNonVariant)
                return TRUE;

            // Covariant and contravariant parameters can *only* appear in resp. covariant and contravariant positions
            return ((CorGenericParamAttr) (pVarianceInfo[index]) == position);
        }

        case ELEMENT_TYPE_GENERICINST:
        {
            IfFailThrow(psig.GetElemType(&typ));
            mdTypeRef typeref;
            IfFailThrow(psig.GetToken(&typeref));

            // The number of type parameters follows
            uint32_t ntypars;
            IfFailThrow(psig.GetData(&ntypars));

            // If this is a value type, or position == gpNonVariant, then
            // we're disallowing covariant and contravariant completely
            if (typ == ELEMENT_TYPE_VALUETYPE || position == gpNonVariant)
            {
                for (unsigned i = 0; i < ntypars; i++)
                {
                    if (!CheckVarianceInSig(numGenericArgs, pVarianceInfo, pModule, psig, gpNonVariant))
                        return FALSE;

                    IfFailThrow(psig.SkipExactlyOne());
                }
            }
            // Otherwise we need to take notice of the variance annotation on each type parameter to the generic type
            else
            {
                mdTypeDef typeDef;
                Module *  pDefModule;
                // This will also be resolved later; so, give up and don't indicate a variance failure
                if (!ClassLoader::ResolveTokenToTypeDefThrowing(pModule, typeref, &pDefModule, &typeDef))
                    return TRUE;

                bool foundHasVarianceResult;

                if (!pDefModule->m_pTypeGenericInfoMap->HasVariance(typeDef, &foundHasVarianceResult) && foundHasVarianceResult)
                {
                    // Fast path, now that we know there isn't variance
                    uint32_t genericArgCount = pDefModule->m_pTypeGenericInfoMap->GetGenericArgumentCount(typeDef, pDefModule->GetMDImport());
                    for (uint32_t iGenericArgCount = 0; iGenericArgCount < genericArgCount; iGenericArgCount++)
                    {
                        if (!CheckVarianceInSig(numGenericArgs, pVarianceInfo, pModule, psig, gpNonVariant))
                            return FALSE;

                        IfFailThrow(psig.SkipExactlyOne());
                    }
                }
                else
                {
                    HENUMInternal   hEnumGenericPars;
                    if (FAILED(pDefModule->GetMDImport()->EnumInit(mdtGenericParam, typeDef, &hEnumGenericPars)))
                    {
                        pDefModule->GetAssembly()->ThrowTypeLoadException(pDefModule->GetMDImport(), typeDef, IDS_CLASSLOAD_BADFORMAT);
                    }

                    for (unsigned i = 0; i < ntypars; i++)
                    {
                        mdGenericParam tkTyPar;
                        pDefModule->GetMDImport()->EnumNext(&hEnumGenericPars, &tkTyPar);
                        DWORD flags;
                        if (FAILED(pDefModule->GetMDImport()->GetGenericParamProps(tkTyPar, NULL, &flags, NULL, NULL, NULL)))
                        {
                            pDefModule->GetAssembly()->ThrowTypeLoadException(pDefModule->GetMDImport(), typeDef, IDS_CLASSLOAD_BADFORMAT);
                        }
                        CorGenericParamAttr genPosition = (CorGenericParamAttr) (flags & gpVarianceMask);
                        // If the surrounding context is contravariant then we need to flip the variance of this parameter
                        if (position == gpContravariant)
                        {
                            genPosition = genPosition == gpCovariant ? gpContravariant
                                        : genPosition == gpContravariant ? gpCovariant
                                        : gpNonVariant;
                        }
                        if (!CheckVarianceInSig(numGenericArgs, pVarianceInfo, pModule, psig, genPosition))
                            return FALSE;

                        IfFailThrow(psig.SkipExactlyOne());
                    }
                    pDefModule->GetMDImport()->EnumClose(&hEnumGenericPars);
                }
            }

            return TRUE;
        }

        // Arrays behave covariantly
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:
            return CheckVarianceInSig(numGenericArgs, pVarianceInfo, pModule, psig, position);

        // Pointers behave non-variantly
        case ELEMENT_TYPE_BYREF:
        case ELEMENT_TYPE_PTR:
            return CheckVarianceInSig(numGenericArgs, pVarianceInfo, pModule, psig, gpNonVariant);

        case ELEMENT_TYPE_FNPTR:
            {
                // Calling convention
                IfFailThrow(psig.GetData(NULL));

                // Get arg count;
                uint32_t cArgs;
                IfFailThrow(psig.GetData(&cArgs));

                // Conservatively, assume non-variance of function pointer types
                if (!CheckVarianceInSig(numGenericArgs, pVarianceInfo, pModule, psig, gpNonVariant))
                    return FALSE;

                IfFailThrow(psig.SkipExactlyOne());

                for (unsigned i = 0; i < cArgs; i++)
                {
                    if (!CheckVarianceInSig(numGenericArgs, pVarianceInfo, pModule, psig, gpNonVariant))
                        return FALSE;

                    IfFailThrow(psig.SkipExactlyOne());
                }

                return TRUE;
            }

        default:
            THROW_BAD_FORMAT(IDS_CLASSLOAD_BAD_VARIANCE_SIG, pModule);
    }

    return FALSE;
} // EEClass::CheckVarianceInSig

void
ClassLoader::LoadExactParentAndInterfacesTransitively(MethodTable *pMT)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;


    TypeHandle thisTH(pMT);
    SigTypeContext typeContext(thisTH);
    IMDInternalImport* pInternalImport = pMT->GetMDImport();
    MethodTable *pParentMT = pMT->GetParentMethodTable();

    if (pParentMT != NULL && pParentMT->HasInstantiation())
    {
        // Fill in exact parent if it's instantiated
        mdToken crExtends;
        IfFailThrow(pInternalImport->GetTypeDefProps(
            pMT->GetCl(),
            NULL,
            &crExtends));

        _ASSERTE(!IsNilToken(crExtends));
        _ASSERTE(TypeFromToken(crExtends) == mdtTypeSpec);

        TypeHandle newParent = ClassLoader::LoadTypeDefOrRefOrSpecThrowing(pMT->GetModule(), crExtends, &typeContext,
                                                                           ClassLoader::ThrowIfNotFound,
                                                                           ClassLoader::FailIfUninstDefOrRef,
                                                                           ClassLoader::LoadTypes,
                                                                           CLASS_LOAD_EXACTPARENTS,
                                                                           TRUE);

        MethodTable* pNewParentMT = newParent.AsMethodTable();
        if (pNewParentMT != pParentMT)
        {
            LOG((LF_CLASSLOADER, LL_INFO1000, "GENERICS: Replaced approximate parent %s with exact parent %s from token %x\n", pParentMT->GetDebugClassName(), pNewParentMT->GetDebugClassName(), crExtends));

            // SetParentMethodTable is not used here since we want to update the indirection cell in the NGen case
            *pMT->GetParentMethodTableValuePtr() = pNewParentMT;
            pParentMT = pNewParentMT;
        }
    }

    if (pParentMT != NULL)
    {
        EnsureLoaded(pParentMT, CLASS_LOAD_EXACTPARENTS);
    }


    if (pParentMT != NULL && pParentMT->HasPerInstInfo())
    {
        // Copy down all inherited dictionary pointers which we
        // could not embed.
        DWORD nDicts = pParentMT->GetNumDicts();
        for (DWORD iDict = 0; iDict < nDicts; iDict++)
        {
            if (pMT->GetPerInstInfo()[iDict] != pParentMT->GetPerInstInfo()[iDict])
            {
                pMT->GetPerInstInfo()[iDict] = pParentMT->GetPerInstInfo()[iDict];
            }
        }
    }

    MethodTableBuilder::LoadExactInterfaceMap(pMT);

#ifdef _DEBUG
    if (g_pConfig->ShouldDumpOnClassLoad(pMT->GetDebugClassName()))
    {
        pMT->Debug_DumpInterfaceMap("Exact");
    }
#endif //_DEBUG
} // ClassLoader::LoadExactParentAndInterfacesTransitively

namespace
{
#ifdef FEATURE_METADATA_UPDATER
    void CreateAllEnCStaticFields(MethodTable* pMT, MethodTable* pMTCanon, EditAndContinueModule* pModule)
    {
        CONTRACTL
        {
            STANDARD_VM_CHECK;
            PRECONDITION(CheckPointer(pMT));
            PRECONDITION(pMT->HasInstantiation());
            PRECONDITION(CheckPointer(pMTCanon));
            PRECONDITION(pMTCanon->IsCanonicalMethodTable());
            PRECONDITION(CheckPointer(pModule));
        }
        CONTRACTL_END;

        LOG((LF_ENC, LL_INFO100, "CreateAllEnCStaticFields: pMT:%p pMTCanon:%p\n", pMT, pMTCanon));

#ifdef _DEBUG
        // Sanity check there is relevant EnC data.
        EnCEEClassData* pEnCClass = pModule->GetEnCEEClassData(pMTCanon);
        _ASSERTE(pEnCClass != NULL && pEnCClass->GetAddedStaticFields() > 0);
#endif // _DEBUG

        // Iterate over the Canonical MethodTable and see if there are any EnC static fields
        // we need to add to the current MethodTable.
        EncApproxFieldDescIterator canonFieldIter(
            pMTCanon,
            ApproxFieldDescIterator::STATIC_FIELDS,
            (EncApproxFieldDescIterator::FixUpEncFields | EncApproxFieldDescIterator::OnlyEncFields));
        PTR_FieldDesc pCanonFD;
        while ((pCanonFD = canonFieldIter.Next()) != NULL)
        {
            mdFieldDef canonTok = pCanonFD->GetMemberDef();

            // Check if the current MethodTable already has an entry for
            // this FieldDesc.
            bool shouldAdd = true;
            EncApproxFieldDescIterator mtFieldIter(
                pMT,
                ApproxFieldDescIterator::STATIC_FIELDS,
                (EncApproxFieldDescIterator::FixUpEncFields | EncApproxFieldDescIterator::OnlyEncFields));
            PTR_FieldDesc pFD;
            while ((pFD = mtFieldIter.Next()) != NULL)
            {
                mdFieldDef tok = pFD->GetMemberDef();
                if (tok == canonTok)
                {
                    shouldAdd = false;
                    break;
                }
            }

            // The FieldDesc already exists, no need to add.
            if (!shouldAdd)
                continue;

            LOG((LF_ENC, LL_INFO100, "CreateAllEnCStaticFields: Must add pCanonFD:%p\n", pCanonFD));

            {
                GCX_COOP();
                PTR_FieldDesc pNewFD;
                HRESULT hr = EEClass::AddField(pMT, canonTok, &pNewFD);
                if (FAILED(hr))
                {
                    LOG((LF_ENC, LL_INFO100, "CreateAllEnCStaticFields failed: 0x%08x\n", hr));
                    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_FAILFAST,
                        W("Failed to add static field to instantiated type instance"));
                }
            }
        }
    }
#endif // FEATURE_METADATA_UPDATER
}

// CLASS_LOAD_EXACTPARENTS phase of loading:
// * Load the base class at exact instantiation
// * Recurse LoadExactParents up parent hierarchy
// * Load explicitly declared interfaces on this class at exact instantiation
// * Fixup vtable
//
/*static*/
void ClassLoader::LoadExactParents(MethodTable* pMT)
{
    CONTRACT_VOID
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMT));
        POSTCONDITION(pMT->CheckLoadLevel(CLASS_LOAD_EXACTPARENTS));
    }
    CONTRACT_END;

    if (!pMT->IsCanonicalMethodTable())
    {
        EnsureLoaded(TypeHandle(pMT->GetCanonicalMethodTable()), CLASS_LOAD_EXACTPARENTS);
    }

    LoadExactParentAndInterfacesTransitively(pMT);

    if (pMT->GetClass()->HasVTableMethodImpl())
    {
        MethodTableBuilder::CopyExactParentSlots(pMT);
        PropagateCovariantReturnMethodImplSlots(pMT);
    }

#ifdef FEATURE_METADATA_UPDATER
    // Generics for EnC - create static FieldDescs.
    // Instance FieldDescs don't need to be created here because they
    // are added during type load by reading the updated metadata tables.
    if (pMT->HasInstantiation())
    {
        // Check if the MethodTable has any EnC static fields
        PTR_MethodTable pMTCanon = pMT->GetCanonicalMethodTable();
        if (pMTCanon->GetClass()->HasEnCStaticFields())
        {
            Module* pModule = pMT->GetModule();
            if (pModule->IsEditAndContinueEnabled())
                CreateAllEnCStaticFields(pMT, pMTCanon, (EditAndContinueModule*)pModule);
        }
    }
#endif // FEATURE_METADATA_UPDATER

    // We can now mark this type as having exact parents
    pMT->SetHasExactParent();

#ifdef TARGET_RISCV64
    if (pMT->HasLayout() &&
        pMT->GetVerifierCorElementType() == ELEMENT_TYPE_VALUETYPE &&
        pMT->GetNumInstanceFieldBytes() <= ENREGISTERED_RETURNTYPE_MAXSIZE)
    {
        // To calculate how a small struct is enregistered RISC-V needs native layout info.
        // Initializalize now so ArgIterator doesn't trigger undue class loads during GC stack walks.
        pMT->EnsureNativeLayoutInfoInitialized();
    }
#endif

    RETURN;
}

// Get CorElementType of the reduced type of a type.
// The reduced type concept is described in ECMA 335 chapter I.8.7
//
/*static*/
CorElementType ClassLoader::GetReducedTypeElementType(TypeHandle hType)
{
    CorElementType elemType = hType.GetVerifierCorElementType();
    switch (elemType)
    {
        case ELEMENT_TYPE_U1:
            return ELEMENT_TYPE_I1;
        case ELEMENT_TYPE_U2:
            return ELEMENT_TYPE_I2;
        case ELEMENT_TYPE_U4:
            return ELEMENT_TYPE_I4;
        case ELEMENT_TYPE_U8:
            return ELEMENT_TYPE_I8;
        case ELEMENT_TYPE_U:
            return ELEMENT_TYPE_I;
        default:
            return elemType;
    }
}

// Get CorElementType of the verification type of a type.
// The verification type concepts is described in ECMA 335 chapter I.8.7
//
/*static*/
CorElementType ClassLoader::GetVerificationTypeElementType(TypeHandle hType)
{
    CorElementType reducedTypeElementType = GetReducedTypeElementType(hType);

    switch (reducedTypeElementType)
    {
        case ELEMENT_TYPE_BOOLEAN:
            return ELEMENT_TYPE_I1;
        case ELEMENT_TYPE_CHAR:
            return ELEMENT_TYPE_I2;
        default:
            return reducedTypeElementType;
    }
}

// Check if verification types of two types are equal
//
/*static*/
bool ClassLoader::AreVerificationTypesEqual(TypeHandle hType1, TypeHandle hType2)
{
    if (hType1 == hType2)
    {
        return true;
    }

    CorElementType e1 = GetVerificationTypeElementType(hType1);
    if (!CorIsPrimitiveType(e1))
    {
        return false;
    }

    CorElementType e2 = GetVerificationTypeElementType(hType2);

    return e1 == e2;
}

// Check if signatures of two function pointers are compatible
// Note - this is a simplified version of what's described in the ECMA spec and it considers
// pointers to be method-signature-compatible-with only if the signatures are the same.
//
/*static*/
bool ClassLoader::IsMethodSignatureCompatibleWith(FnPtrTypeDesc* fn1TD, FnPtrTypeDesc* fn2TD)
{
    if (fn1TD->GetCallConv() != fn1TD->GetCallConv())
    {
        return false;
    }

    if (fn1TD->GetNumArgs() != fn2TD->GetNumArgs())
    {
        return false;
    }

    TypeHandle* pFn1ArgTH = fn1TD->GetRetAndArgTypes();
    TypeHandle* pFn2ArgTH = fn2TD->GetRetAndArgTypes();
    for (DWORD i = 0; i < fn1TD->GetNumArgs() + 1; i++)
    {
        if (pFn1ArgTH[i] != pFn2ArgTH[i])
        {
            return false;
        }
    }

    return true;
}

// Checks if two types are compatible according to compatible-with as described in ECMA 335 I.8.7.1
// Most of the checks are performed by the CanCastTo, but with some cases pre-filtered out.
//
/*static*/
bool ClassLoader::IsCompatibleWith(TypeHandle hType1, TypeHandle hType2)
{
    // Structs can be cast to the interfaces they implement, but they are not compatible according to ECMA I.8.7.1
    bool isCastFromValueTypeToReferenceType = hType2.IsValueType() && !hType1.IsValueType();
    if (isCastFromValueTypeToReferenceType)
    {
        return false;
    }

    // Managed pointers are compatible only if they are pointer-element-compatible-with as described in ECMA I.8.7.2
    if (hType1.IsByRef() && hType2.IsByRef())
    {
        return AreVerificationTypesEqual(hType1.GetTypeParam(), hType2.GetTypeParam());
    }

    // Unmanaged pointers are handled the same way as managed pointers
    if (hType1.IsPointer() && hType2.IsPointer())
    {
        return AreVerificationTypesEqual(hType1.GetTypeParam(), hType2.GetTypeParam());
    }

    // Function pointers are compatible only if they are method-signature-compatible-with as described in ECMA I.8.7.1
    if (hType1.IsFnPtrType() && hType2.IsFnPtrType())
    {
        return IsMethodSignatureCompatibleWith(hType1.AsFnPtrType(), hType2.AsFnPtrType());
    }

    // None of the types can be a managed pointer, a pointer or a function pointer here,
    // all the valid cases were handled above.
    if (hType1.IsByRef() || hType2.IsByRef() ||
        hType1.IsPointer() || hType2.IsPointer() ||
        hType1.IsFnPtrType() || hType2.IsFnPtrType())
    {
        return false;
    }

    MethodTable* pMT1 = hType1.GetMethodTable();
    if (pMT1 != NULL)
    {
        // Nullable<T> can be cast to T, but this is not compatible according to ECMA I.8.7.1
        bool isCastFromNullableOfTtoT = pMT1->IsNullable() && hType2.IsEquivalentTo(pMT1->GetInstantiation()[0]);
        if (isCastFromNullableOfTtoT)
        {
            return false;
        }
    }

    return hType2.CanCastTo(hType1, NULL);
}

/*static*/
void ClassLoader::ValidateMethodsWithCovariantReturnTypes(MethodTable* pMT)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    // Validate that the return types on overriding methods with covariant return types are
    // compatible with the return type of the method being overridden. Compatibility rules are defined by
    // ECMA I.8.7.1, which is what the CanCastTo() API checks.
    //

    // Validation not applicable to interface types and value types, since these are not currently
    // supported with the covariant return feature

    if (pMT->IsInterface() || pMT->IsValueType())
        return;

    MethodTable* pParentMT = pMT->GetParentMethodTable();
    if (pParentMT == NULL)
        return;

    // Step 1: Validate compatibility of return types on overriding methods
    if (pMT->GetClass()->HasCovariantOverride() && (!pMT->GetModule()->IsReadyToRun() || !pMT->GetModule()->GetReadyToRunInfo()->SkipTypeValidation()))
    {
        for (WORD i = 0; i < pParentMT->GetNumVirtuals(); i++)
        {
            if (pMT->GetRestoredSlot(i) == pParentMT->GetRestoredSlot(i))
            {
                // The real check is that the MethodDesc's must not match, but a simple VTable check will
                // work most of the time, and is far faster than the GetMethodDescForSlot method.
                _ASSERTE(pMT->GetMethodDescForSlot(i) == pParentMT->GetMethodDescForSlot(i));
                continue;
            }
            MethodDesc* pMD = pMT->GetMethodDescForSlot(i);
            MethodDesc* pParentMD = pParentMT->GetMethodDescForSlot(i);

            if (pMD == pParentMD)
                continue;

            if (!pMD->RequiresCovariantReturnTypeChecking() && !pParentMD->RequiresCovariantReturnTypeChecking())
                continue;

            // Locate the MethodTable defining the pParentMD.
            MethodTable* pDefinitionParentMT = pParentMT;
            while (pDefinitionParentMT->GetCanonicalMethodTable() != pParentMD->GetMethodTable())
            {
                pDefinitionParentMT = pDefinitionParentMT->GetParentMethodTable();
            }

            SigTypeContext context1(pDefinitionParentMT->GetInstantiation(), pMD->GetMethodInstantiation());
            MetaSig methodSig1(pParentMD);
            TypeHandle hType1 = methodSig1.GetReturnProps().GetTypeHandleThrowing(pParentMD->GetModule(), &context1, ClassLoader::LoadTypesFlag::LoadTypes, CLASS_LOAD_EXACTPARENTS);

            SigTypeContext context2(pMT->GetInstantiation(), pMD->GetMethodInstantiation());
            MetaSig methodSig2(pMD);
            TypeHandle hType2 = methodSig2.GetReturnProps().GetTypeHandleThrowing(pMD->GetModule(), &context2, ClassLoader::LoadTypesFlag::LoadTypes, CLASS_LOAD_EXACTPARENTS);

            if (!IsCompatibleWith(hType1, hType2))
            {
                SString strAssemblyName;
                pMD->GetAssembly()->GetDisplayName(strAssemblyName);

                SString strInvalidTypeName;
                TypeString::AppendType(strInvalidTypeName, TypeHandle(pMD->GetMethodTable()));

                SString strInvalidMethodName;
                SString strParentMethodName;
                {
                    CONTRACT_VIOLATION(LoadsTypeViolation);
                    TypeString::AppendMethod(strInvalidMethodName, pMD, pMD->GetMethodInstantiation());
                    TypeString::AppendMethod(strParentMethodName, pParentMD, pParentMD->GetMethodInstantiation());
                }

                COMPlusThrow(
                    kTypeLoadException,
                    IDS_CLASSLOAD_MI_BADRETURNTYPE,
                    strInvalidMethodName,
                    strInvalidTypeName,
                    strAssemblyName,
                    strParentMethodName);
            }
        }
    }
}

/*static*/
void ClassLoader::PropagateCovariantReturnMethodImplSlots(MethodTable* pMT)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    // Propagate an overriding MethodImpl to all applicable vtable slots if the MethodImpl
    // has the PreserveBaseOverrides attribute. This is to ensure that if we use the signature of one of
    // the base type methods to call the overriding method, we still execute the overriding method.
    //
    // Consider this case:
    //
    //      class A {
    //          RetType VirtualFunction() { }
    //      }
    //      class B : A {
    //          [PreserveBaseOverrides]
    //          DerivedRetType VirtualFunction() { .override A.VirtualFunction }
    //      }
    //      class C : B {
    //          MoreDerivedRetType VirtualFunction() { .override A.VirtualFunction }
    //      }
    //
    // NOTE: Typically the attribute would be added to the MethodImpl on C, but was omitted in this example to
    //       illustrate how its presence on a MethodImpl on the base type can propagate as well. In other words,
    //       think of it as applying to the vtable slot itself, so any MethodImpl that overrides this slot on a
    //       derived type will propagate to all other applicable vtable slots.
    //
    // Given an object of type C, the attribute will ensure that:
    //      callvirt RetType A::VirtualFunc()               -> executes the MethodImpl on C
    //      callvirt DerivedRetType B::VirtualFunc()        -> executes the MethodImpl on C
    //      callvirt MoreDerivedRetType C::VirtualFunc()    -> executes the MethodImpl on C
    //
    // Without the attribute, the second callvirt would normally execute the MethodImpl on B (the MethodImpl on
    // C does not override the vtable slot of B's MethodImpl, but only overrides the declaring method's vtable slot.
    //

    // Validation not applicable to interface types and value types, since these are not currently
    // supported with the covariant return feature

    if (pMT->IsInterface() || pMT->IsValueType())
        return;

    MethodTable* pParentMT = pMT->GetParentMethodTable();
    if (pParentMT == NULL)
        return;

    // Propagate overriding MethodImpls to applicable vtable slots if the declaring method has the attribute

    if (pMT->GetClass()->HasVTableMethodImpl())
    {
        MethodTable::MethodDataWrapper hMTData(MethodTable::GetMethodData(pMT, MethodDataComputeOptions::CacheOnly));

        for (WORD i = 0; i < pParentMT->GetNumVirtuals(); i++)
        {
            if (pMT->GetRestoredSlot(i) == pParentMT->GetRestoredSlot(i))
            {
                // The real check is that the MethodDesc's must not match, but a simple VTable check will
                // work most of the time, and is far faster than the GetMethodDescForSlot method.
                _ASSERTE(pMT->GetMethodDescForSlot(i) == pParentMT->GetMethodDescForSlot(i));
                continue;
            }

            MethodDesc* pMD = pMT->GetMethodDescForSlot(i);
            MethodDesc* pParentMD = pParentMT->GetMethodDescForSlot(i);
            if (pMD == pParentMD)
                continue;

            // If the bit is not set on this method, but we reach here because it's been set on the method at the same slot on
            // the base type, set the bit for the current method to ensure any future overriding method down the chain gets checked.
            if (!pMD->RequiresCovariantReturnTypeChecking() && pParentMD->RequiresCovariantReturnTypeChecking())
                pMD->SetRequiresCovariantReturnTypeChecking();

            // The attribute is only applicable to MethodImpls. For anything else, it will be treated as a no-op
            if (!pMD->IsMethodImpl())
                continue;

            // Search if the attribute has been applied on this vtable slot, either by the current MethodImpl, or by a previous
            // MethodImpl somewhere in the base type hierarchy.
            bool foundAttribute = false;
            MethodTable* pCurrentMT = pMT;
            while (!foundAttribute && pCurrentMT != NULL && i < pCurrentMT->GetNumVirtuals())
            {
                MethodDesc* pCurrentMD = pCurrentMT->GetMethodDescForSlot(i);

                // The attribute is only applicable to MethodImpls. For anything else, it will be treated as a no-op
                if (pCurrentMD->IsMethodImpl())
                {
                    BYTE* pVal = NULL;
                    ULONG cbVal = 0;
                    if (pCurrentMD->GetCustomAttribute(WellKnownAttribute::PreserveBaseOverridesAttribute, (const void**)&pVal, &cbVal) == S_OK)
                        foundAttribute = true;
                }

                pCurrentMT = pCurrentMT->GetParentMethodTable();
            }

            if (!foundAttribute)
                continue;

            // Search for any vtable slot still pointing at the parent method, and update it with the current overriding method
            for (WORD j = i; j < pParentMT->GetNumVirtuals(); j++)
            {
                MethodDesc* pCurrentMD = pMT->GetMethodDescForSlot(j);
                if (pCurrentMD == pParentMD)
                {
                    // This is a vtable slot that needs to be updated to the new overriding method because of the
                    // presence of the attribute.
                    pMT->SetSlot(j, pMT->GetSlot(i));
                    _ASSERT(pMT->GetMethodDescForSlot(j) == pMD);

                    if (!hMTData.IsNull())
                        hMTData->UpdateImplMethodDesc(pMD, j);
                }
            }
        }
    }
}


//*******************************************************************************
//
// Debugger notification
//
BOOL TypeHandle::NotifyDebuggerLoad(AppDomain *pDomain, BOOL attaching) const
{
    LIMITED_METHOD_CONTRACT;

    if (!CORDebuggerAttached())
    {
        return FALSE;
    }

    if (!GetModule()->IsVisibleToDebugger())
    {
        return FALSE;
    }

    return g_pDebugInterface->LoadClass(
        *this, GetCl(), GetModule(), pDomain);
}

//*******************************************************************************
void TypeHandle::NotifyDebuggerUnload(AppDomain *pDomain) const
{
    LIMITED_METHOD_CONTRACT;

    if (!GetModule()->IsVisibleToDebugger())
        return;

    if (!pDomain->IsDebuggerAttached())
        return;

    g_pDebugInterface->UnloadClass(GetCl(), GetModule(), pDomain);
}

//*******************************************************************************
// Given the (generics-shared or generics-exact) value class method, find the
// (generics-shared) unboxing Stub for the given method .  We search the vtable.
//
// This is needed when creating a delegate to an instance method in a value type
MethodDesc* MethodTable::GetBoxedEntryPointMD(MethodDesc *pMD)
{
    CONTRACT (MethodDesc *) {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(IsValueType());
        PRECONDITION(!pMD->ContainsGenericVariables());
        PRECONDITION(!pMD->IsUnboxingStub());
        POSTCONDITION(RETVAL->IsUnboxingStub());
    } CONTRACT_END;

    RETURN MethodDesc::FindOrCreateAssociatedMethodDesc(pMD,
                                                        pMD->GetMethodTable(),
                                                        TRUE /* get unboxing entry point */,
                                                        pMD->GetMethodInstantiation(),
                                                        FALSE /* no allowInstParam */ );

}

//*******************************************************************************
// Given the unboxing value class method, find the non-unboxing method
// This is used when generating the code for an BoxedEntryPointStub.
MethodDesc* MethodTable::GetUnboxedEntryPointMD(MethodDesc *pMD)
{
    CONTRACT (MethodDesc *) {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(IsValueType());
        // reflection needs to call this for methods in non instantiated classes,
        // so move the assert to the caller when needed
        //PRECONDITION(!pMD->ContainsGenericVariables());
        PRECONDITION(pMD->IsUnboxingStub());
        POSTCONDITION(!RETVAL->IsUnboxingStub());
    } CONTRACT_END;

    BOOL allowInstParam = (pMD->GetNumGenericMethodArgs() == 0);
    RETURN MethodDesc::FindOrCreateAssociatedMethodDesc(pMD,
                                                        this,
                                                        FALSE /* don't get unboxing entry point */,
                                                        pMD->GetMethodInstantiation(),
                                                        allowInstParam);
}


//*******************************************************************************
// Given the unboxing value class method, find the non-unboxing method
// This is used when generating the code for an BoxedEntryPointStub.
MethodDesc* MethodTable::GetExistingUnboxedEntryPointMD(MethodDesc *pMD)
{
    CONTRACT (MethodDesc *) {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(IsValueType());
        // reflection needs to call this for methods in non instantiated classes,
        // so move the assert to the caller when needed
        //PRECONDITION(!pMD->ContainsGenericVariables());
        PRECONDITION(pMD->IsUnboxingStub());
        POSTCONDITION(!RETVAL->IsUnboxingStub());
    } CONTRACT_END;

    BOOL allowInstParam = (pMD->GetNumGenericMethodArgs() == 0);
    RETURN MethodDesc::FindOrCreateAssociatedMethodDesc(pMD,
                                                        this,
                                                        FALSE /* don't get unboxing entry point */,
                                                        pMD->GetMethodInstantiation(),
                                                        allowInstParam,
                                                        FALSE, /* forceRemotableMethod */
                                                        FALSE  /* allowCreate */
                                                       );
}

#endif // !DACCESS_COMPILE

//*******************************************************************************
#if !defined(FEATURE_HFA)
bool MethodTable::IsHFA()
{
    LIMITED_METHOD_CONTRACT;
#ifdef DACCESS_COMPILE
    return false;
#else
    if (GetClass()->GetMethodTable()->IsValueType())
    {
        return GetClass()->CheckForHFA();
    }
    else
    {
        return false;
    }
#endif
}
#endif // !FEATURE_HFA

//*******************************************************************************
int MethodTable::GetVectorSize()
{
    // This is supported for finding HVA types for Arm64. In order to support the altjit,
    // we support this on 64-bit platforms (i.e. Arm64 and X64).
#ifdef TARGET_64BIT
    if (IsIntrinsicType())
    {
        LPCUTF8 namespaceName;
        LPCUTF8 className = GetFullyQualifiedNameInfo(&namespaceName);
        int vectorSize = 0;

        if (strcmp(className, "Vector`1") == 0)
        {
            _ASSERTE(strcmp(namespaceName, "System.Numerics") == 0);
            vectorSize = GetNumInstanceFieldBytes();
        }
        else if (strcmp(className, "Vector128`1") == 0)
        {
            _ASSERTE(strcmp(namespaceName, "System.Runtime.Intrinsics") == 0);
            vectorSize = 16;
        }
        else if (strcmp(className, "Vector64`1") == 0)
        {
            _ASSERTE(strcmp(namespaceName, "System.Runtime.Intrinsics") == 0);
            vectorSize = 8;
        }
        if (vectorSize != 0)
        {
            // We need to verify that T (the element or "base" type) is a primitive type.
            TypeHandle typeArg = GetInstantiation()[0];
            CorElementType corType = typeArg.GetSignatureCorElementType();
            if (((corType >= ELEMENT_TYPE_I1) && (corType <= ELEMENT_TYPE_R8)) || (corType == ELEMENT_TYPE_I) || (corType == ELEMENT_TYPE_U))
            {
                return vectorSize;
            }
        }
    }
#endif // TARGET_64BIT
    return 0;
}

//*******************************************************************************
CorInfoHFAElemType MethodTable::GetHFAType()
{
    CONTRACTL
    {
        WRAPPER(THROWS);        // we end up in the class loader which has the conditional contracts
        WRAPPER(GC_TRIGGERS);
    }
    CONTRACTL_END;

    if (!IsHFA())
        return CORINFO_HFA_ELEM_NONE;

    MethodTable * pMT = this;
    for (;;)
    {
        _ASSERTE(pMT->IsValueType());
        _ASSERTE(pMT->GetNumInstanceFields() > 0);

        int vectorSize = pMT->GetVectorSize();
        if (vectorSize != 0)
        {
            return (vectorSize == 8) ? CORINFO_HFA_ELEM_VECTOR64 : CORINFO_HFA_ELEM_VECTOR128;
        }

        PTR_FieldDesc pFirstField = pMT->GetApproxFieldDescListRaw();

        CorElementType fieldType = pFirstField->GetFieldType();

        // All HFA fields have to be of the same type, so we can just return the type of the first field
        switch (fieldType)
        {
        case ELEMENT_TYPE_VALUETYPE:
            pMT = pFirstField->LookupApproxFieldTypeHandle().GetMethodTable();
            break;

        case ELEMENT_TYPE_R4:
            return CORINFO_HFA_ELEM_FLOAT;
        case ELEMENT_TYPE_R8:
            return CORINFO_HFA_ELEM_DOUBLE;

        default:
            // This should never happen. MethodTable::IsHFA() should be set only on types
            // that have a valid HFA type when the flag is used to track HFA status.
            _ASSERTE(false);
            return CORINFO_HFA_ELEM_NONE;
        }
    }
}

bool MethodTable::IsNativeHFA()
{
    LIMITED_METHOD_CONTRACT;
    if (!HasLayout() || IsBlittable())
    {
        return IsHFA();
    }

    return GetNativeLayoutInfo()->IsNativeHFA();
}

CorInfoHFAElemType MethodTable::GetNativeHFAType()
{
    LIMITED_METHOD_CONTRACT;
    if (!HasLayout() || IsBlittable())
    {
        return GetHFAType();
    }

    return GetNativeLayoutInfo()->GetNativeHFAType();
}

//---------------------------------------------------------------------------------------
//
// When FEATURE_HFA is defined, we cache the value; otherwise we recompute it with each
// call. The latter is only for the armaltjit and the arm64altjit.
//
bool
#if defined(FEATURE_HFA)
EEClass::CheckForHFA(MethodTable ** pByValueClassCache)
#else
EEClass::CheckForHFA()
#endif
{
    STANDARD_VM_CONTRACT;

    // This method should be called for valuetypes only
    _ASSERTE(GetMethodTable()->IsValueType());

    // The opaque Vector types appear to have multiple fields, but need to be treated
    // as an opaque type of a single vector.
    if (GetMethodTable()->GetVectorSize() != 0)
    {
#if defined(FEATURE_HFA)
        GetMethodTable()->SetIsHFA();
#endif
        return true;
    }

    CorInfoHFAElemType hfaType = CORINFO_HFA_ELEM_NONE;

    FieldDesc *pFieldDescList = GetFieldDescList();

    bool hasZeroOffsetField = false;

    for (UINT i = 0; i < GetNumInstanceFields(); i++)
    {
        FieldDesc *pFD = &pFieldDescList[i];
        hasZeroOffsetField |= (pFD->GetOffset() == 0);

        CorElementType fieldType = pFD->GetFieldType();
        CorInfoHFAElemType fieldHFAType = CORINFO_HFA_ELEM_NONE;

        switch (fieldType)
        {
        case ELEMENT_TYPE_VALUETYPE:
            {
#ifdef TARGET_ARM64
                MethodTable* pMT;
#if defined(FEATURE_HFA)
                pMT = pByValueClassCache[i];
#else
                pMT = pFD->LookupApproxFieldTypeHandle().AsMethodTable();
#endif
                int thisElemSize = pMT->GetVectorSize();
                if (thisElemSize != 0)
                {
                    fieldHFAType = (thisElemSize == 8) ? CORINFO_HFA_ELEM_VECTOR64 : CORINFO_HFA_ELEM_VECTOR128;
                }
                else
#endif // TARGET_ARM64
                {
#if defined(FEATURE_HFA)
                    fieldHFAType = pByValueClassCache[i]->GetHFAType();
#else
                    fieldHFAType = pFD->LookupApproxFieldTypeHandle().AsMethodTable()->GetHFAType();
#endif
                }
            }
            break;

        case ELEMENT_TYPE_R4:
            {
                static const int REQUIRED_FLOAT_ALIGNMENT = 4;
                if (pFD->GetOffset() % REQUIRED_FLOAT_ALIGNMENT != 0) // HFAs don't have unaligned fields.
                {
                    return false;
                }
                fieldHFAType = CORINFO_HFA_ELEM_FLOAT;
            }
            break;
        case ELEMENT_TYPE_R8:
            {
                static const int REQUIRED_DOUBLE_ALIGNMENT = 8;
                if (pFD->GetOffset() % REQUIRED_DOUBLE_ALIGNMENT != 0) // HFAs don't have unaligned fields.
                {
                    return false;
                }
                fieldHFAType = CORINFO_HFA_ELEM_DOUBLE;
            }
            break;
        default:
            // Not HFA
            return false;
        }

        // Field type should be a valid HFA type.
        if (fieldHFAType == CORINFO_HFA_ELEM_NONE)
        {
            return false;
        }

        // Initialize with a valid HFA type.
        if (hfaType == CORINFO_HFA_ELEM_NONE)
        {
            hfaType = fieldHFAType;
        }
        // All field types should be equal.
        else if (fieldHFAType != hfaType)
        {
            return false;
        }
    }

    int elemSize = 0;
    switch (hfaType)
    {
    case CORINFO_HFA_ELEM_FLOAT:
        elemSize = 4;
        break;
    case CORINFO_HFA_ELEM_DOUBLE:
    case CORINFO_HFA_ELEM_VECTOR64:
        elemSize = 8;
        break;
#ifdef TARGET_ARM64
    case CORINFO_HFA_ELEM_VECTOR128:
        elemSize = 16;
        break;
#endif
    default:
        // ELEMENT_TYPE_END
        return false;
    }

    if (!hasZeroOffsetField) // If the struct doesn't have a zero-offset field, it's not an HFA.
        return false;

    // Note that we check the total size, but do not perform any checks on number of fields:
    // - Type of fields can be HFA valuetype itself
    // - Managed C++ HFA valuetypes have just one <alignment member> of type float to signal that
    //   the valuetype is HFA and explicitly specified size

    DWORD totalSize = GetMethodTable()->GetNumInstanceFieldBytes();

    if (totalSize % elemSize != 0)
        return false;

    // On ARM, HFAs can have a maximum of four fields regardless of whether those are float or double.
    if (totalSize / elemSize > 4)
        return false;

    // All the above tests passed. It's HFA(/HVA)!
#if defined(FEATURE_HFA)
    GetMethodTable()->SetIsHFA();
#endif
    return true;
}

#ifdef FEATURE_64BIT_ALIGNMENT
// Returns true iff the native view of this type requires 64-bit alignment.
bool MethodTable::NativeRequiresAlign8()
{
    LIMITED_METHOD_CONTRACT;

    if (HasLayout() && !IsBlittable())
    {
        return (GetNativeLayoutInfo()->GetLargestAlignmentRequirement() >= 8);
    }
    return RequiresAlign8();
}
#endif // FEATURE_64BIT_ALIGNMENT

#ifndef DACCESS_COMPILE

#ifdef FEATURE_COMINTEROP
//==========================================================================================
TypeHandle MethodTable::GetCoClassForInterface()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    EEClass * pClass = GetClass();

    if (!pClass->IsComClassInterface())
        return TypeHandle();

    _ASSERTE(IsInterface());

    TypeHandle th = pClass->GetCoClassForInterface();
    if (!th.IsNull())
        return th;

    return SetupCoClassForInterface();
}

//*******************************************************************************
TypeHandle MethodTable::SetupCoClassForInterface()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(IsComClassInterface());

    }
    CONTRACTL_END

    TypeHandle CoClassType;
    const BYTE *pVal = NULL;
    ULONG cbVal = 0;

    HRESULT hr = GetCustomAttribute(WellKnownAttribute::CoClass, (const void **)&pVal, &cbVal);
    if (hr == S_OK)
    {
        CustomAttributeParser cap(pVal, cbVal);

        IfFailThrow(cap.SkipProlog());

        // Retrieve the COM source interface class name.
        ULONG       cbName;
        LPCUTF8     szName;
        IfFailThrow(cap.GetNonNullString(&szName, &cbName));

        // Copy the name to a temporary buffer and NULL terminate it.
        StackSString ss(SString::Utf8, szName, cbName);

        // Try to load the class using its name as a fully qualified name. If that fails,
        // then we try to load it in the assembly of the current class.
        CoClassType = TypeName::GetTypeReferencedByCustomAttribute(ss.GetUnicode(), GetAssembly());

        // Cache the coclass type
        GetClass()->SetCoClassForInterface(CoClassType);
    }
    return CoClassType;
}

//*******************************************************************************
void MethodTable::GetEventInterfaceInfo(MethodTable **ppSrcItfClass, MethodTable **ppEvProvClass)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END


    TypeHandle EventProvType;
    TypeHandle SrcItfType;
    const BYTE *pVal = NULL;
    ULONG cbVal = 0;

    // Retrieve the ComEventProviderAttribute CA.
    HRESULT hr = GetMDImport()->GetCustomAttributeByName(GetCl(), INTEROP_COMEVENTINTERFACE_TYPE, (const void**)&pVal, &cbVal);
    if (FAILED(hr))
    {
        COMPlusThrowHR(hr);
    }

    CustomAttributeParser cap(pVal, cbVal);

    // Skip the CA type prefix.
    IfFailThrow(cap.SkipProlog());

    // Retrieve the COM source interface class name.
    LPCUTF8 szName;
    ULONG   cbName;
    IfFailThrow(cap.GetNonNullString(&szName, &cbName));

    // Copy the name to a temporary buffer and NULL terminate it.
    StackSString ss(SString::Utf8, szName, cbName);

    // Try to load the class using its name as a fully qualified name. If that fails,
    // then we try to load it in the assembly of the current class.
    SrcItfType = TypeName::GetTypeReferencedByCustomAttribute(ss.GetUnicode(), GetAssembly());

    // Retrieve the COM event provider class name.
    IfFailThrow(cap.GetNonNullString(&szName, &cbName));

    // Copy the name to a temporary buffer and NULL terminate it.
    ss.SetUTF8(szName, cbName);

    // Try to load the class using its name as a fully qualified name. If that fails,
    // then we try to load it in the assembly of the current class.
    EventProvType = TypeName::GetTypeReferencedByCustomAttribute(ss.GetUnicode(), GetAssembly());

    // Set the source interface and event provider classes.
    *ppSrcItfClass = SrcItfType.GetMethodTable();
    *ppEvProvClass = EventProvType.GetMethodTable();
}

//*******************************************************************************
TypeHandle MethodTable::GetDefItfForComClassItf()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    BAD_FORMAT_NOTHROW_ASSERT(GetClass()->IsComClassInterface());

    // The COM class interface uses the normal scheme which is to have no
    // methods and to implement default interface and optionnally the
    // default source interface. In this scheme, the first implemented
    // interface is the default interface which we return.
    InterfaceMapIterator it = IterateInterfaceMap();
    if (it.Next())
    {
        // Can use GetInterfaceApprox, as there are no generic default interfaces
        return TypeHandle(it.GetInterfaceApprox());
    }
    else
    {
        // The COM class interface has the methods directly on the itself.
        // Because of this we need to consider it to be the default interface.
        return TypeHandle(this);
    }
}

#endif // FEATURE_COMINTEROP


#endif // !DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
// Get the metadata token of the outer type for a nested type
//
// Return Value:
//    The token of the outer class if this EEClass is nested, or mdTypeDefNil if the
//    EEClass is not a nested type
//

mdTypeDef MethodTable::GetEnclosingCl()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    mdTypeDef tdEnclosing = mdTypeDefNil;

    if (GetClass()->IsNested())
    {
        HRESULT hr = GetMDImport()->GetNestedClassProps(GetCl(), &tdEnclosing);
        if (FAILED(hr))
        {
            ThrowHR(hr, BFA_UNABLE_TO_GET_NESTED_PROPS);
        }
    }

    return tdEnclosing;
}

CorNativeLinkType MethodTable::GetCharSet()
{
    IMDInternalImport* pInternalImport = GetModule()->GetMDImport();

    DWORD clFlags;

    CorNativeLinkType charSet = nltAnsi; // Initialize to ANSI to make the compiler happy for the case that always asserts
    bool success = true;

    if (FAILED(pInternalImport->GetTypeDefProps(GetTypeDefRid(), &clFlags, NULL)))
    {
        success = false;
    }

    if (IsTdAnsiClass(clFlags))
    {
        charSet = nltAnsi;
    }
    else if (IsTdUnicodeClass(clFlags))
    {
        charSet = nltUnicode;
    }
    else if (IsTdAutoClass(clFlags))
    {
#ifdef TARGET_WINDOWS
        charSet = nltUnicode;
#else
        charSet = nltAnsi; // We don't have a utf8 charset in metadata yet, but ANSI == UTF-8 off-Windows
#endif
    }
    else
    {
        success = false;
    }

    _ASSERTE_MSG(success, "Charset metadata for this type should have already been verified at type-load time");

    return charSet;
}

//*******************************************************************************
//
// Helper routines for the macros defined at the top of this class.
// You probably should not use these functions directly.
//
template<typename RedirectFunctor>
SString &MethodTable::_GetFullyQualifiedNameForClassNestedAwareInternal(SString &ssBuf)
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
    } CONTRACTL_END;

    ssBuf.Clear();

    LPCUTF8 pszNamespace;
    LPCUTF8 pszName;
    pszName = GetFullyQualifiedNameInfo(&pszNamespace);
    if (pszName == NULL)
    {
        return ssBuf;
    }

    StackSString ssName(SString::Utf8, pszName);

    mdTypeDef mdEncl = GetCl();
    IMDInternalImport *pImport = GetMDImport();

    // Check if the type is nested
    DWORD dwAttr;
    IfFailThrow(pImport->GetTypeDefProps(GetCl(), &dwAttr, NULL));

    RedirectFunctor redirectFunctor;
    if (IsTdNested(dwAttr))
    {
        StackSString ssFullyQualifiedName;
        StackSString ssPath;

        // Build the nesting chain.
        while (SUCCEEDED(pImport->GetNestedClassProps(mdEncl, &mdEncl)))
        {
            LPCUTF8 szEnclName;
            LPCUTF8 szEnclNameSpace;
            IfFailThrow(pImport->GetNameOfTypeDef(
                mdEncl,
                &szEnclName,
                &szEnclNameSpace));

            ns::MakePath(ssPath,
                StackSString(SString::Utf8, redirectFunctor(szEnclNameSpace)),
                StackSString(SString::Utf8, szEnclName));
            ns::MakeNestedTypeName(ssFullyQualifiedName, ssPath, ssName);

            ssName = ssFullyQualifiedName;
        }
    }

    ns::MakePath(
        ssBuf,
        StackSString(SString::Utf8, redirectFunctor(pszNamespace)), ssName);

    return ssBuf;
}

class PassThrough
{
public :
    LPCUTF8 operator() (LPCUTF8 szEnclNamespace)
    {
        LIMITED_METHOD_CONTRACT;

        return szEnclNamespace;
    }
};

SString &MethodTable::_GetFullyQualifiedNameForClassNestedAware(SString &ssBuf)
{
    LIMITED_METHOD_CONTRACT;

    return _GetFullyQualifiedNameForClassNestedAwareInternal<PassThrough>(ssBuf);
}

//*******************************************************************************
SString &MethodTable::_GetFullyQualifiedNameForClass(SString &ssBuf)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    ssBuf.Clear();

    if (IsArray())
    {
        TypeDesc::ConstructName(GetInternalCorElementType(),
                                GetArrayElementTypeHandle(),
                                GetRank(),
                                ssBuf);
    }
    else if (!IsNilToken(GetCl()))
    {
        LPCUTF8 szNamespace;
        LPCUTF8 szName;
        IfFailThrow(GetMDImport()->GetNameOfTypeDef(GetCl(), &szName, &szNamespace));

        ns::MakePath(ssBuf,
                     StackSString(SString::Utf8, szNamespace),
                     StackSString(SString::Utf8, szName));
    }

    return ssBuf;
}

//*******************************************************************************
//
// Gets the namespace and class name for the class.  The namespace
// can legitimately come back NULL, however a return value of NULL indicates
// an error.
//
// NOTE: this used to return array class names, which were sometimes squirreled away by the
// class loader hash table.  It's been removed because it wasted space and was basically broken
// in general (sometimes wasn't set, sometimes set wrong).  If you need array class names,
// use GetFullyQualifiedNameForClass instead.
//
LPCUTF8 MethodTable::GetFullyQualifiedNameInfo(LPCUTF8 *ppszNamespace)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    if (IsArray())
    {
        *ppszNamespace = NULL;
        return NULL;
    }
    else
    {
        LPCUTF8 szName;
        if (FAILED(GetMDImport()->GetNameOfTypeDef(GetCl(), &szName, ppszNamespace)))
        {
            *ppszNamespace = NULL;
            return NULL;
        }
        return szName;
    }
}

#ifndef DACCESS_COMPILE

#ifdef FEATURE_COMINTEROP

//*******************************************************************************
CorIfaceAttr MethodTable::GetComInterfaceType()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    // This should only be called on interfaces.
    BAD_FORMAT_NOTHROW_ASSERT(IsInterface());

    // Check to see if we have already determined the COM interface type
    // of this interface.
    CorIfaceAttr ItfType = GetClass()->GetComInterfaceType();

    if (ItfType != (CorIfaceAttr)-1)
        return ItfType;

    // Retrieve the interface type from the metadata.
    HRESULT hr = GetMDImport()->GetIfaceTypeOfTypeDef(GetCl(), (ULONG*)&ItfType);
    IfFailThrow(hr);

    if (hr != S_OK)
    {
        // if not found in metadata, use the default
        ItfType = ifDual;
    }

    if (ItfType == ifInspectable)
    {
        COMPlusThrow(kPlatformNotSupportedException, IDS_EE_NO_IINSPECTABLE);
    }

    // Cache the interface type
    GetClass()->SetComInterfaceType(ItfType);

    return ItfType;
}

#endif // FEATURE_COMINTEROP

//*******************************************************************************
void EEClass::GetBestFitMapping(MethodTable * pMT, BOOL *pfBestFitMapping, BOOL *pfThrowOnUnmappableChar)
{
    CONTRACTL
    {
        THROWS; // OOM only
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    EEClass * pClass = pMT->GetClass();

    // lazy init
    if (!(pClass->m_VMFlags & VMFLAG_BESTFITMAPPING_INITED))
    {
        *pfBestFitMapping = FALSE;
        *pfThrowOnUnmappableChar = FALSE;

        ReadBestFitCustomAttribute(pMT->GetModule(), pMT->GetCl(), pfBestFitMapping, pfThrowOnUnmappableChar);

        DWORD flags = VMFLAG_BESTFITMAPPING_INITED;
        if (*pfBestFitMapping) flags |= VMFLAG_BESTFITMAPPING;
        if (*pfThrowOnUnmappableChar) flags |= VMFLAG_THROWONUNMAPPABLECHAR;

        InterlockedOr((LONG*)&pClass->m_VMFlags, flags);
    }
    else
    {
        *pfBestFitMapping = (pClass->m_VMFlags & VMFLAG_BESTFITMAPPING);
        *pfThrowOnUnmappableChar = (pClass->m_VMFlags & VMFLAG_THROWONUNMAPPABLECHAR);
    }
}

#ifdef _DEBUG

//*******************************************************************************
void MethodTable::DebugRecursivelyDumpInstanceFields(LPCUTF8 pszClassName, BOOL debug)
{
    WRAPPER_NO_CONTRACT;  // It's a dev helper, who cares about contracts

    EX_TRY
    {
        StackSString ssBuff;

        DWORD cParentInstanceFields;
        DWORD i;

        CONSISTENCY_CHECK(CheckLoadLevel(CLASS_LOAD_APPROXPARENTS));

        MethodTable *pParentMT = GetParentMethodTable();
        if (pParentMT != NULL)
        {
            cParentInstanceFields = pParentMT->GetClass()->GetNumInstanceFields();
            DefineFullyQualifiedNameForClass();
            LPCUTF8 name = GetFullyQualifiedNameForClass(pParentMT);
            pParentMT->DebugRecursivelyDumpInstanceFields(name, debug);
        }
        else
        {
            cParentInstanceFields = 0;
        }

        // Are there any new instance fields declared by this class?
        if (GetNumInstanceFields() > cParentInstanceFields)
        {
            // Display them
            if(debug) {
                ssBuff.Printf("%s:\n", pszClassName);
                OutputDebugStringUtf8(ssBuff.GetUTF8());
            }
            else {
                 LOG((LF_CLASSLOADER, LL_ALWAYS, "%s:\n", pszClassName));
            }

            for (i = 0; i < (GetNumInstanceFields()-cParentInstanceFields); i++)
            {
                FieldDesc *pFD = &GetClass()->GetFieldDescList()[i];
#ifdef DEBUG_LAYOUT
                printf("offset %s%3d %s\n", pFD->IsByValue() ? "byvalue " : "", pFD->GetOffset(), pFD->GetName());
#endif
                if(debug) {
                    ssBuff.Printf("offset %3d %s\n", pFD->GetOffset(), pFD->GetName());
                    OutputDebugStringUtf8(ssBuff.GetUTF8());
                }
                else {
                    LOG((LF_CLASSLOADER, LL_ALWAYS, "offset %3d %s\n", pFD->GetOffset(), pFD->GetName()));
                }
            }
        }
    }
    EX_CATCH
    {
        if(debug)
        {
            OutputDebugStringUtf8("<Exception Thrown>\n");
        }
        else
        {
             LOG((LF_CLASSLOADER, LL_ALWAYS, "<Exception Thrown>\n"));
        }
    }
    EX_END_CATCH(SwallowAllExceptions);
}

//*******************************************************************************
void MethodTable::DebugDumpFieldLayout(LPCUTF8 pszClassName, BOOL debug)
{
    WRAPPER_NO_CONTRACT;   // It's a dev helper, who cares about contracts

    if (GetNumStaticFields() == 0 && GetNumInstanceFields() == 0)
        return;

    EX_TRY
    {
        StackSString ssBuff;

        DWORD i;
        DWORD cParentInstanceFields;

        CONSISTENCY_CHECK(CheckLoadLevel(CLASS_LOAD_APPROXPARENTS));

        if (GetParentMethodTable() != NULL)
            cParentInstanceFields = GetParentMethodTable()->GetNumInstanceFields();
        else
        {
            cParentInstanceFields = 0;
        }

        if (debug)
        {
            ssBuff.Printf("Field layout for '%s':\n\n", pszClassName);
            OutputDebugStringUtf8(ssBuff.GetUTF8());
        }
        else
        {
            //LF_ALWAYS allowed here because this is controlled by special env var ShouldDumpOnClassLoad
            LOG((LF_ALWAYS, LL_ALWAYS, "Field layout for '%s':\n\n", pszClassName));
        }

        if (GetNumStaticFields() > 0)
        {
            if (debug)
            {
                OutputDebugStringUtf8("Static fields (stored at vtable offsets)\n");
                OutputDebugStringUtf8("----------------------------------------\n");
            }
            else
            {
                //LF_ALWAYS allowed here because this is controlled by special env var ShouldDumpOnClassLoad
                LOG((LF_ALWAYS, LL_ALWAYS, "Static fields (stored at vtable offsets)\n"));
                LOG((LF_ALWAYS, LL_ALWAYS, "----------------------------------------\n"));
            }

            for (i = 0; i < GetNumStaticFields(); i++)
            {
                FieldDesc *pFD = GetClass()->GetFieldDescList() + ((GetNumInstanceFields()-cParentInstanceFields) + i);
                if(debug) {
                    ssBuff.Printf("offset %3d %s\n", pFD->GetOffset(), pFD->GetName());
                    OutputDebugStringUtf8(ssBuff.GetUTF8());
                }
                else
                {
                    //LF_ALWAYS allowed here because this is controlled by special env var ShouldDumpOnClassLoad
                    LOG((LF_ALWAYS, LL_ALWAYS, "offset %3d %s\n", pFD->GetOffset(), pFD->GetName()));
                }
            }
        }

        if (GetNumInstanceFields() > 0)
        {
            if (GetNumStaticFields()) {
                if(debug) {
                    OutputDebugStringUtf8("\n");
                }
                else
                {
                    //LF_ALWAYS allowed here because this is controlled by special env var ShouldDumpOnClassLoad
                    LOG((LF_ALWAYS, LL_ALWAYS, "\n"));
                }
            }

            if (debug)
            {
                OutputDebugStringUtf8("Instance fields\n");
                OutputDebugStringUtf8("---------------\n");
            }
            else
            {
                //LF_ALWAYS allowed here because this is controlled by special env var ShouldDumpOnClassLoad
                LOG((LF_ALWAYS, LL_ALWAYS, "Instance fields\n"));
                LOG((LF_ALWAYS, LL_ALWAYS, "---------------\n"));
            }

            DebugRecursivelyDumpInstanceFields(pszClassName, debug);
        }

        if (debug)
        {
            OutputDebugStringUtf8("\n");
        }
        else
        {
            //LF_ALWAYS allowed here because this is controlled by special env var ShouldDumpOnClassLoad
            LOG((LF_ALWAYS, LL_ALWAYS, "\n"));
        }
    }
    EX_CATCH
    {
        if (debug)
        {
            OutputDebugStringUtf8("<Exception Thrown>\n");
        }
        else
        {
            //LF_ALWAYS allowed here because this is controlled by special env var ShouldDumpOnClassLoad
             LOG((LF_ALWAYS, LL_ALWAYS, "<Exception Thrown>\n"));
        }
    }
    EX_END_CATCH(SwallowAllExceptions);
} // MethodTable::DebugDumpFieldLayout

//*******************************************************************************
void
MethodTable::DebugDumpGCDesc(
    LPCUTF8 pszClassName,
    BOOL    fDebug)
{
    WRAPPER_NO_CONTRACT;   // It's a dev helper, who cares about contracts

    EX_TRY
    {
        StackSString ssBuff;

        if (fDebug)
        {
            ssBuff.Printf("GC description for '%s':\n\n", pszClassName);
            OutputDebugStringUtf8(ssBuff.GetUTF8());
        }
        else
        {
            //LF_ALWAYS allowed here because this is controlled by special env var ShouldDumpOnClassLoad
            LOG((LF_ALWAYS, LL_ALWAYS, "GC description for '%s':\n\n", pszClassName));
        }

        if (ContainsPointers())
        {
            CGCDescSeries *pSeries;
            CGCDescSeries *pHighest;

            if (fDebug)
            {
                OutputDebugStringUtf8("GCDesc:\n");
            } else
            {
                //LF_ALWAYS allowed here because this is controlled by special env var ShouldDumpOnClassLoad
                LOG((LF_ALWAYS, LL_ALWAYS, "GCDesc:\n"));
            }

            pSeries  = CGCDesc::GetCGCDescFromMT(this)->GetLowestSeries();
            pHighest = CGCDesc::GetCGCDescFromMT(this)->GetHighestSeries();

            while (pSeries <= pHighest)
            {
                if (fDebug)
                {
                    ssBuff.Printf("   offset %5d (%d w/o Object), size %5d (%5d w/o BaseSize subtr)\n",
                        pSeries->GetSeriesOffset(),
                        pSeries->GetSeriesOffset() - OBJECT_SIZE,
                        pSeries->GetSeriesSize(),
                        pSeries->GetSeriesSize() + GetBaseSize() );
                    OutputDebugStringUtf8(ssBuff.GetUTF8());
                }
                else
                {
                    //LF_ALWAYS allowed here because this is controlled by special env var ShouldDumpOnClassLoad
                    LOG((LF_ALWAYS, LL_ALWAYS, "   offset %5d (%d w/o Object), size %5d (%5d w/o BaseSize subtr)\n",
                         pSeries->GetSeriesOffset(),
                         pSeries->GetSeriesOffset() - OBJECT_SIZE,
                         pSeries->GetSeriesSize(),
                         pSeries->GetSeriesSize() + GetBaseSize()
                         ));
                }
                pSeries++;
            }

            if (fDebug)
            {
                OutputDebugStringUtf8("\n");
            } else
            {
                //LF_ALWAYS allowed here because this is controlled by special env var ShouldDumpOnClassLoad
                LOG((LF_ALWAYS, LL_ALWAYS, "\n"));
            }
        }
    }
    EX_CATCH
    {
        if (fDebug)
        {
            OutputDebugStringUtf8("<Exception Thrown>\n");
        }
        else
        {
            //LF_ALWAYS allowed here because this is controlled by special env var ShouldDumpOnClassLoad
            LOG((LF_ALWAYS, LL_ALWAYS, "<Exception Thrown>\n"));
        }
    }
    EX_END_CATCH(SwallowAllExceptions);
} // MethodTable::DebugDumpGCDesc

#endif // _DEBUG

#ifdef FEATURE_COMINTEROP
//*******************************************************************************
CorClassIfaceAttr MethodTable::GetComClassInterfaceType()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!IsInterface());
    }
    CONTRACTL_END

    // If the type is an open generic type, then it is considered ClassInterfaceType.None.
    if (ContainsGenericVariables())
        return clsIfNone;

    // Classes that either have generic instantiations (G<int>) or derive from classes
    // with generic instantiations (D : B<int>) are always considered ClassInterfaceType.None.
    if (HasGenericClassInstantiationInHierarchy())
        return clsIfNone;

    // If the class does not support IClassX,
    // then it is considered ClassInterfaceType.None unless explicitly overridden by the CA
    if (!ClassSupportsIClassX(this))
        return clsIfNone;

    return ReadClassInterfaceTypeCustomAttribute(TypeHandle(this));
}
#endif // FEATURE_COMINTEROP

//---------------------------------------------------------------------------------------
//
Substitution
MethodTable::GetSubstitutionForParent(
    const Substitution * pSubst)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    mdToken crExtends;
    DWORD   dwAttrClass;

    if (IsArray())
    {
        return Substitution(GetModule(), SigPointer(), pSubst);
    }

    IfFailThrow(GetMDImport()->GetTypeDefProps(
        GetCl(),
        &dwAttrClass,
        &crExtends));

    return Substitution(crExtends, GetModule(), pSubst);
} // MethodTable::GetSubstitutionForParent

#endif //!DACCESS_COMPILE

#ifndef DACCESS_COMPILE
#ifdef FEATURE_COMINTEROP

//
// Implementations of SparseVTableMap methods.
//

//*******************************************************************************
SparseVTableMap::SparseVTableMap()
{
    LIMITED_METHOD_CONTRACT;

    // Note that this will also zero out all gaps. It is important for NGen determinism.
    ZeroMemory(this, sizeof(*this));
}

//*******************************************************************************
SparseVTableMap::~SparseVTableMap()
{
    LIMITED_METHOD_CONTRACT;

    if (m_MapList != NULL)
    {
        delete [] m_MapList;
        m_MapList = NULL;
    }
}

//*******************************************************************************
// Allocate or expand the mapping list for a new entry.
void SparseVTableMap::AllocOrExpand()
{
    STANDARD_VM_CONTRACT;

    if (m_MapEntries == m_Allocated) {

        Entry *maplist = new Entry[m_Allocated + MapGrow];

        if (m_MapList != NULL)
            memcpy(maplist, m_MapList, m_MapEntries * sizeof(Entry));

        m_Allocated += MapGrow;
        delete [] m_MapList;
        m_MapList = maplist;
    }
}

//*******************************************************************************
// While building mapping list, record a gap in VTable slot numbers.
void SparseVTableMap::RecordGap(WORD StartMTSlot, WORD NumSkipSlots)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE((StartMTSlot == 0) || (StartMTSlot > m_MTSlot));
    _ASSERTE(NumSkipSlots > 0);

    // We use the information about the current gap to complete a map entry for
    // the last non-gap. There is a special case where the vtable begins with a
    // gap, so we don't have a non-gap to record.
    if (StartMTSlot == 0) {
        _ASSERTE((m_MTSlot == 0) && (m_VTSlot == 0));
        m_VTSlot = NumSkipSlots;
        return;
    }

    // We need an entry, allocate or expand the list as necessary.
    AllocOrExpand();

    // Update the list with an entry describing the last non-gap in vtable
    // entries.
    m_MapList[m_MapEntries].m_Start = m_MTSlot;
    m_MapList[m_MapEntries].m_Span = StartMTSlot - m_MTSlot;
    m_MapList[m_MapEntries].m_MapTo = m_VTSlot;

    m_VTSlot += (StartMTSlot - m_MTSlot) + NumSkipSlots;
    m_MTSlot = StartMTSlot;

    m_MapEntries++;
}

//*******************************************************************************
// Finish creation of mapping list.
void SparseVTableMap::FinalizeMapping(WORD TotalMTSlots)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(TotalMTSlots >= m_MTSlot);

    // If mapping ended with a gap, we have nothing else to record.
    if (TotalMTSlots == m_MTSlot)
        return;

    // Allocate or expand the list as necessary.
    AllocOrExpand();

    // Update the list with an entry describing the last non-gap in vtable
    // entries.
    m_MapList[m_MapEntries].m_Start = m_MTSlot;
    m_MapList[m_MapEntries].m_Span = TotalMTSlots - m_MTSlot;
    m_MapList[m_MapEntries].m_MapTo = m_VTSlot;

    // Update VT slot cursor, because we use it to determine total number of
    // vtable slots for GetNumVirtuals
    m_VTSlot += TotalMTSlots - m_MTSlot;

    m_MapEntries++;
}

//*******************************************************************************
// Lookup a VTable slot number from a method table slot number.
WORD SparseVTableMap::LookupVTSlot(WORD MTSlot)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    // As an optimization, check the last entry which yielded a correct result.
    if ((MTSlot >= m_MapList[m_LastUsed].m_Start) &&
        (MTSlot < (m_MapList[m_LastUsed].m_Start + m_MapList[m_LastUsed].m_Span)))
        return (MTSlot - m_MapList[m_LastUsed].m_Start) + m_MapList[m_LastUsed].m_MapTo;

    // Check all MT slots spans to see which one our input slot lies in.
    for (WORD i = 0; i < m_MapEntries; i++) {
        if ((MTSlot >= m_MapList[i].m_Start) &&
            (MTSlot < (m_MapList[i].m_Start + m_MapList[i].m_Span))) {
            m_LastUsed = i;
            return (MTSlot - m_MapList[i].m_Start) + m_MapList[i].m_MapTo;
        }
    }

    _ASSERTE(!"Invalid MethodTable slot");
    return ~0;
}

//*******************************************************************************
// Retrieve the number of slots in the vtable (both empty and full).
WORD SparseVTableMap::GetNumVTableSlots()
{
    LIMITED_METHOD_CONTRACT;

    return m_VTSlot;
}

#endif //FEATURE_COMINTEROP

//*******************************************************************************
void EEClass::AddChunk (MethodDescChunk* pNewChunk)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    _ASSERTE(pNewChunk->GetNextChunk() == NULL);

    MethodDescChunk* head = GetChunks();

    if (head == NULL)
    {
        SetChunks(pNewChunk);
    }
    else
    {
        // Current chunk needs to be added to the end of the list so that
        // when reflection is iterating all methods, they would come in declared order
        while (head->GetNextChunk() != NULL)
            head = head->GetNextChunk();

        head->SetNextChunk(pNewChunk);
    }
}

//*******************************************************************************
void EEClass::AddChunkIfItHasNotBeenAdded (MethodDescChunk* pNewChunk)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    // return if the chunk has been added
    if (pNewChunk->GetNextChunk() != NULL)
        return;

    // even if pNewChunk->GetNextChunk() is NULL, this may still be the first chunk we added
    // (last in the list) so find the end of the list and verify that
    MethodDescChunk *chunk = GetChunks();
    if (chunk != NULL)
    {
        while (chunk->GetNextChunk() != NULL)
            chunk = chunk->GetNextChunk();

        if (chunk == pNewChunk)
            return;
    }

    pNewChunk->SetNextChunk(GetChunks());
    SetChunks(pNewChunk);
}

#endif // !DACCESS_COMPILE

//*******************************************************************************
// ApproxFieldDescIterator is used to iterate over fields in a given class.
// It does not includes EnC fields, and not inherited fields.
// <NICE> ApproxFieldDescIterator is only used to iterate over static fields in one place,
// and this will probably change anyway.  After
// we clean this up we should make ApproxFieldDescIterator work
// over instance fields only </NICE>
ApproxFieldDescIterator::ApproxFieldDescIterator()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    m_iteratorType = 0;
    m_pFieldDescList = NULL;
    m_currField = -1;
    m_totalFields = 0;
}

//*******************************************************************************
void ApproxFieldDescIterator::Init(MethodTable *pMT, int iteratorType)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    m_iteratorType = iteratorType;
    m_pFieldDescList = pMT->GetApproxFieldDescListRaw();
    m_currField = -1;

    // This gets non-EnC fields.
    m_totalFields = pMT->GetNumIntroducedInstanceFields();

    if (!(iteratorType & (int)INSTANCE_FIELDS))
    {
        // if not handling instances then skip them by setting curr to last one
        m_currField = m_totalFields - 1;
    }

    if (iteratorType & (int)STATIC_FIELDS)
    {
        m_totalFields += pMT->GetNumStaticFields();
    }
}

//*******************************************************************************
PTR_FieldDesc ApproxFieldDescIterator::Next()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    // This will iterate through all non-inherited and non-EnC fields.
    ++m_currField;
    if (m_currField >= m_totalFields)
    {
        return NULL;
    }

    return m_pFieldDescList + m_currField;
}

//*******************************************************************************
bool
DeepFieldDescIterator::NextClass()
{
    WRAPPER_NO_CONTRACT;

    if (m_curClass <= 0)
    {
        return false;
    }

    if (m_numClasses <= 0) {
        _ASSERTE(m_numClasses > 0);
        return false;
    }

    MethodTable * pMT;

    //
    // If we're in the cache just grab the cache entry.
    //
    // If we're deeper in the hierarchy than the
    // portion we cached we need to take the
    // deepest cache entry and search down manually.
    //

    if (--m_curClass < m_numClasses)
    {
        pMT = m_classes[m_curClass];
    }
    else
    {
        pMT = m_classes[m_numClasses - 1];
        int depthDiff = m_curClass - m_numClasses + 1;
        while (depthDiff--)
        {
            pMT = pMT->GetParentMethodTable();
        }
    }

    m_fieldIter.Init(pMT, m_fieldIter.GetIteratorType());
    return true;
}

//*******************************************************************************
void
DeepFieldDescIterator::Init(MethodTable* pMT, int iteratorType,
                            bool includeParents)
{
    WRAPPER_NO_CONTRACT;

    MethodTable * lastClass = NULL;
    int numClasses;

    //
    // Walk up the parent chain, collecting
    // parent pointers and counting fields.
    //

    numClasses = 0;
    m_numClasses = 0;
    m_deepTotalFields = 0;
    m_lastNextFromParentClass = false;

    while (pMT)
    {
        if (m_numClasses < (int)ARRAY_SIZE(m_classes))
        {
            m_classes[m_numClasses++] = pMT;
        }

        if ((iteratorType & ApproxFieldDescIterator::INSTANCE_FIELDS) != 0)
        {
            m_deepTotalFields += pMT->GetNumIntroducedInstanceFields();
        }
        if ((iteratorType & ApproxFieldDescIterator::STATIC_FIELDS) != 0)
        {
            m_deepTotalFields += pMT->GetNumStaticFields();
        }

        numClasses++;
        lastClass = pMT;

        if (includeParents)
        {
            pMT = pMT->GetParentMethodTable();
        }
        else
        {
            break;
        }
    }

    // Start the per-class field iterator on the base-most parent.
    if (numClasses)
    {
        m_curClass = numClasses - 1;
        m_fieldIter.Init(lastClass, iteratorType);
    }
    else
    {
        m_curClass = 0;
    }
}

//*******************************************************************************
FieldDesc*
DeepFieldDescIterator::Next()
{
    WRAPPER_NO_CONTRACT;

    FieldDesc* field;

    do
    {
        m_lastNextFromParentClass = m_curClass > 0;

        field = m_fieldIter.Next();

        if (!field && !NextClass())
        {
            return NULL;
        }
    }
    while (!field);

    return field;
}

//*******************************************************************************
bool
DeepFieldDescIterator::Skip(int numSkip)
{
    WRAPPER_NO_CONTRACT;

    while (numSkip >= m_fieldIter.CountRemaining())
    {
        numSkip -= m_fieldIter.CountRemaining();

        if (!NextClass())
        {
            return false;
        }
    }

    while (numSkip--)
    {
        m_fieldIter.Next();
    }

    return true;
}

#ifdef DACCESS_COMPILE

//*******************************************************************************
void
EEClass::EnumMemoryRegions(CLRDataEnumMemoryFlags flags, MethodTable * pMT)
{
    SUPPORTS_DAC;
    DAC_ENUM_DTHIS();
    EMEM_OUT(("MEM: %p EEClass\n", dac_cast<TADDR>(this)));

    if (HasOptionalFields())
        DacEnumMemoryRegion(dac_cast<TADDR>(GetOptionalFields()), sizeof(EEClassOptionalFields));

    if (flags != CLRDATA_ENUM_MEM_MINI && flags != CLRDATA_ENUM_MEM_TRIAGE && flags != CLRDATA_ENUM_MEM_HEAP2)
    {
        PTR_Module pModule = pMT->GetModule();
        if (pModule.IsValid())
        {
            pModule->EnumMemoryRegions(flags, true);
        }
        PTR_MethodDescChunk chunk = GetChunks();
        while (chunk.IsValid())
        {
            chunk->EnumMemoryRegions(flags);
            chunk = chunk->GetNextChunk();
        }
    }

    PTR_FieldDesc pFieldDescList = GetFieldDescList();
    if (pFieldDescList.IsValid())
    {
        // add one to make sos's code happy.
        DacEnumMemoryRegion(dac_cast<TADDR>(pFieldDescList),
                            (pMT->GetNumIntroducedInstanceFields() +
                             GetNumStaticFields() + 1) *
                            sizeof(FieldDesc));
    }

}

#endif // DACCESS_COMPILE

