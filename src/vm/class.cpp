// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: CLASS.CPP
//


//

//
// ============================================================================

#include "common.h"

#include "dllimport.h"
#include "dllimportcallback.h"
#include "fieldmarshaler.h"
#include "customattribute.h"
#include "encee.h"
#include "typestring.h"

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
#include "nsenumhandleallcases.h"

#ifndef DACCESS_COMPILE 


//*******************************************************************************
EEClass::EEClass(DWORD cbFixedEEClassFields)
{
    LIMITED_METHOD_CONTRACT;

    // Cache size of fixed fields (this instance also contains a set of packed fields whose final size isn't
    // determined until the end of class loading). We store the size into a spare byte made available by
    // compiler field alignment, so we need to ensure we never allocate a flavor of EEClass more than 255
    // bytes long.
    _ASSERTE(cbFixedEEClassFields <= 0xff);
    m_cbFixedEEClassFields = (BYTE)cbFixedEEClassFields;

    // All other members are initialized to zero
}

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

    // EEClass (or sub-type) is always followed immediately by an EEClassPackedFields structure. This is
    // maximally sized at runtime but in the ngen scenario will be optimized into a smaller structure (which
    // is why it must go after all the fixed sized fields).
    S_SIZE_T safeSize = S_SIZE_T(size) + S_SIZE_T(sizeof(EEClassPackedFields));

    void *p = pamTracker->Track(pHeap->AllocMem(safeSize));

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

#ifndef CROSSGEN_COMPILE

    // Not expected to be called for array EEClass
    _ASSERTE(!pOwningMT->IsArray());

#ifdef _DEBUG
    _ASSERTE(!IsDestroyed());
    SetDestroyed();
#endif

#ifdef PROFILING_SUPPORTED
    // If profiling, then notify the class is getting unloaded.
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackClasses());
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

                g_profControlBlock.pProfInterface->ClassUnloadStarted((ClassID) pOwningMT);
            }
            EX_CATCH
            {
                // The exception here came from the profiler itself. We'll just
                // swallow the exception, since we don't want the profiler to bring
                // down the runtime.
            }
            EX_END_CATCH(RethrowTerminalExceptions);
        }
        END_PIN_PROFILER();
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
            BOOL fStubDeleted = pDelegateEEClass->m_pStaticCallStub->DecRef();
            if (fStubDeleted)
            {
                DelegateInvokeStubManager::g_pManager->RemoveStub(pDelegateEEClass->m_pStaticCallStub);
            }
        }
        if (pDelegateEEClass->m_pInstRetBuffCallStub)
        {
            pDelegateEEClass->m_pInstRetBuffCallStub->DecRef();
        }
        // While m_pMultiCastInvokeStub is also a member,
        // it is owned by the m_pMulticastStubCache, not by the class
        // - it is shared across classes. So we don't decrement
        // its ref count here
        delete pDelegateEEClass->m_pUMThunkMarshInfo;
    }

    // We should never get here for thunking proxy because we do not destroy
    // default appdomain and mscorlib.dll module during shutdown
    _ASSERTE(!pOwningMT->IsTransparentProxy());

  
#ifdef FEATURE_COMINTEROP 
    if (GetSparseCOMInteropVTableMap() != NULL && !pOwningMT->IsZapped())
        delete GetSparseCOMInteropVTableMap();
#endif // FEATURE_COMINTEROP

#ifdef PROFILING_SUPPORTED
    // If profiling, then notify the class is getting unloaded.
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackClasses());
        {
            // See comments in the call to ClassUnloadStarted for details on this
            // FAULT_NOT_FATAL marker and exception swallowing.
            FAULT_NOT_FATAL();
            EX_TRY
            {
                GCX_PREEMP();
                g_profControlBlock.pProfInterface->ClassUnloadFinished((ClassID) pOwningMT, S_OK);
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(RethrowTerminalExceptions);
        }
        END_PIN_PROFILER();
    }
#endif // PROFILING_SUPPORTED

#endif // CROSSGEN_COMPILE
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

    return new (pHeap, pamTracker) EEClass(sizeof(EEClass));
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

#ifdef EnC_SUPPORTED 

//*******************************************************************************
VOID EEClass::FixupFieldDescForEnC(MethodTable * pMT, EnCFieldDesc *pFD, mdFieldDef fieldDef)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
        WRAPPER(GC_TRIGGERS);
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
        LOG((LF_ENC, LL_INFO100, "EEClass::InitializeFieldDescForEnC %s\n", szFieldName));
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
    CheckPointHolder cph(GetThread()->m_MarshalAlloc.GetCheckpoint());

    MethodTableBuilder::bmtMetaDataInfo bmtMetaData;
    bmtMetaData.cFields = 1;
    bmtMetaData.pFields = (mdToken*)_alloca(sizeof(mdToken));
    bmtMetaData.pFields[0] = fieldDef;
    bmtMetaData.pFieldAttrs = (DWORD*)_alloca(sizeof(DWORD));
    IfFailThrow(pImport->GetFieldDefProps(fieldDef, &bmtMetaData.pFieldAttrs[0]));
    
    MethodTableBuilder::bmtMethAndFieldDescs bmtMFDescs;
    // We need to alloc the memory, but don't have to fill it in.  InitializeFieldDescs
    // will copy pFD (1st arg) into here.
    bmtMFDescs.ppFieldDescList = (FieldDesc**)_alloca(sizeof(FieldDesc*));

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
        bmtEnumFields.dwNumInstanceFields = 1;
    }
    
    // We shouldn't have to fill this in b/c we're not allowed to EnC value classes, or
    // anything else with layout info associated with it.
    LayoutRawFieldInfo *pLayoutRawFieldInfos = (LayoutRawFieldInfo*)_alloca((2) * sizeof(LayoutRawFieldInfo));

    // If not NULL, it means there are some by-value fields, and this contains an entry for each instance or static field,
    // which is NULL if not a by value field, and points to the EEClass of the field if a by value field.  Instance fields
    // come first, statics come second.
    MethodTable **pByValueClassCache = NULL;

    EEClass * pClass = pMT->GetClass();

    // InitializeFieldDescs are going to change these numbers to something wrong,
    // even though we already have the right numbers.  Save & restore after.
    WORD   wNumInstanceFields = pMT->GetNumInstanceFields();
    WORD   wNumStaticFields = pMT->GetNumStaticFields();
    unsigned totalDeclaredFieldSize = 0;

    AllocMemTracker dummyAmTracker;

    BaseDomain * pDomain = pMT->GetDomain();
    MethodTableBuilder builder(pMT, pClass,
                               &GetThread()->m_MarshalAlloc,
                               &dummyAmTracker);

    MethodTableBuilder::bmtGenericsInfo genericsInfo;

    OBJECTREF pThrowable = NULL;
    GCPROTECT_BEGIN(pThrowable);

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
                       &bmtEnumFields,
                       NULL);

    EX_TRY
    {
        GCX_PREEMP();
        builder.InitializeFieldDescs(pFD,
                                 pLayoutRawFieldInfos,
                                 &bmtInternal,
                                 &genericsInfo,
                                 &bmtMetaData,
                                 &bmtEnumFields,
                                 &bmtError,
                                 &pByValueClassCache,
                                 &bmtMFDescs,
                                 &bmtFP,
                                 NULL, // not needed as thread or context static are not allowed in EnC
                                 &totalDeclaredFieldSize);
    }
    EX_CATCH_THROWABLE(&pThrowable);

    dummyAmTracker.SuppressRelease();

    // Restore now
    pClass->SetNumInstanceFields(wNumInstanceFields);
    pClass->SetNumStaticFields(wNumStaticFields);

    // PERF: For now, we turn off the fast equality check for valuetypes when a
    // a field is modified by EnC. Consider doing a check and setting the bit only when
    // necessary.
    if (pMT->IsValueType())
    {
        pClass->SetIsNotTightlyPacked();
    }

    if (pThrowable != NULL)
    {
        COMPlusThrow(pThrowable);
    }

    GCPROTECT_END();

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
// runtime layout of the object to accomodate the new field.  Instead we hang the field
// off the syncblock (for instance fields) or in the FieldDesc for static fields.
//
// Here we just create the FieldDesc and link it to the class.  The actual storage will
// be created lazily on demand.
//
HRESULT EEClass::AddField(MethodTable * pMT, mdFieldDef fieldDef, EnCFieldDesc **ppNewFD)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

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
        LOG((LF_ENC, LL_INFO100, "EEClass::AddField %s\n", szFieldName));
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
    EnCEEClassData *pEnCClass = ((EditAndContinueModule*)pModule)->GetEnCEEClassData(pMT);
    if (! pEnCClass)
        return E_FAIL;

    // Add the field element to the list of added fields for this class
    pEnCClass->AddField(pAddedField);

    // Store the FieldDesc into the module's field list
    {
        CONTRACT_VIOLATION(ThrowsViolation); // B#25680 (Fix Enc violations): Must handle OOM's from Ensure
        pModule->EnsureFieldDefCanBeStored(fieldDef);
    }
    pModule->EnsuredStoreFieldDef(fieldDef, pNewFD);
    pNewFD->SetMethodTable(pMT);

    // Success, return the new FieldDesc
    if (ppNewFD)
    {
        *ppNewFD = pNewFD;
    }
    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// AddMethod - called when a new method is added by EnC
//
// The method has already been added to the metadata with token methodDef.
// Create a new MethodDesc for the method.
//
HRESULT EEClass::AddMethod(MethodTable * pMT, mdMethodDef methodDef, RVA newRVA, MethodDesc **ppMethod)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    Module * pModule = pMT->GetModule();
    IMDInternalImport *pImport = pModule->GetMDImport();

#ifdef LOGGING
    if (LoggingEnabled())
    {
        LPCSTR szMethodName;
        if (FAILED(pImport->GetNameOfMethodDef(methodDef, &szMethodName)))
        {
            szMethodName = "Invalid MethodDef record";
        }
        LOG((LF_ENC, LL_INFO100, "EEClass::AddMethod %s\n", szMethodName));
    }
#endif //LOGGING
    
    DWORD dwDescrOffset;
    DWORD dwImplFlags;
    HRESULT hr = S_OK;

    if (FAILED(pImport->GetMethodImplProps(methodDef, &dwDescrOffset, &dwImplFlags)))
    {
        return COR_E_BADIMAGEFORMAT;
    }
    
    DWORD dwMemberAttrs;
    IfFailThrow(pImport->GetMethodDefProps(methodDef, &dwMemberAttrs));

    // Refuse to add other special cases
    if (IsReallyMdPinvokeImpl(dwMemberAttrs)  ||
         (pMT->IsInterface() && !IsMdStatic(dwMemberAttrs)) ||
         IsMiRuntime(dwImplFlags))
    {
        _ASSERTE(! "**Error** EEClass::AddMethod only IL private non-virtual methods are supported");
        LOG((LF_ENC, LL_INFO100, "**Error** EEClass::AddMethod only IL private non-virtual methods are supported\n"));
        return CORDBG_E_ENC_EDIT_NOT_SUPPORTED;
    }

#ifdef _DEBUG 
    // Validate that this methodDef correctly has a parent typeDef
    mdTypeDef   parentTypeDef;
    if (FAILED(hr = pImport->GetParentToken(methodDef, &parentTypeDef)))
    {
        _ASSERTE(! "**Error** EEClass::AddMethod parent token not found");
        LOG((LF_ENC, LL_INFO100, "**Error** EEClass::AddMethod parent token not found\n"));
        return E_FAIL;
    }
#endif // _DEBUG

    EEClass * pClass = pMT->GetClass();

    // @todo: OOM: InitMethodDesc will allocate loaderheap memory but leak it
    //   on failure. This AllocMemTracker should be replaced with a real one.
    AllocMemTracker dummyAmTracker;

    LoaderAllocator* pAllocator = pMT->GetLoaderAllocator();

    // Create a new MethodDescChunk to hold the new MethodDesc
    // Create the chunk somewhere we'll know is within range of the VTable
    MethodDescChunk *pChunk = MethodDescChunk::CreateChunk(pAllocator->GetHighFrequencyHeap(),
                                                           1,               // methodDescCount
                                                           mcInstantiated,
                                                           TRUE /* fNonVtableSlot */,
                                                           TRUE /* fNativeCodeSlot */,
                                                           FALSE /* fComPlusCallInfo */,
                                                           pMT,
                                                           &dummyAmTracker);

    // Get the new MethodDesc (Note: The method desc memory is zero initialized)
    MethodDesc *pNewMD = pChunk->GetFirstMethodDesc();

    // Initialize the new MethodDesc
    MethodTableBuilder builder(pMT,
                               pClass,
                               &GetThread()->m_MarshalAlloc,
                               &dummyAmTracker);
    EX_TRY
    {
        INDEBUG(LPCSTR debug_szFieldName);
        INDEBUG(if (FAILED(pImport->GetNameOfMethodDef(methodDef, &debug_szFieldName))) { debug_szFieldName = "Invalid MethodDef record"; });
        builder.InitMethodDesc(pNewMD, 
                               mcInstantiated,  // Use instantiated methoddesc for EnC added methods to get space for slot
                               methodDef,
                               dwImplFlags,
                               dwMemberAttrs,
                               TRUE,            // fEnC
                               newRVA,
                               pImport,
                               NULL
                               COMMA_INDEBUG(debug_szFieldName)
                               COMMA_INDEBUG(pMT->GetDebugClassName())
                               COMMA_INDEBUG(NULL)
                              );
        
        pNewMD->SetTemporaryEntryPoint(pAllocator, &dummyAmTracker);
    }
    EX_CATCH_HRESULT(hr);
    if (S_OK != hr)
        return hr;

    dummyAmTracker.SuppressRelease();

    _ASSERTE(pNewMD->IsEnCAddedMethod());

    pNewMD->SetSlot(MethodTable::NO_SLOT);    // we can't ever use the slot for EnC methods

    pClass->AddChunk(pChunk);

    // Store the new MethodDesc into the collection for this class
    pModule->EnsureMethodDefCanBeStored(methodDef);
    pModule->EnsuredStoreMethodDef(methodDef, pNewMD);

    LOG((LF_ENC, LL_INFO100, "EEClass::AddMethod new methoddesc %p for token %p\n", pNewMD, methodDef));

    // Success - return the new MethodDesc
    _ASSERTE( SUCCEEDED(hr) );
    if (ppMethod)
    {
        *ppMethod = pNewMD;
    }
    return S_OK;
}

#endif // EnC_SUPPORTED

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
            DWORD index;
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
            DWORD ntypars;
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
                ULONG cArgs;
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
            *EnsureWritablePages(pMT->GetParentMethodTablePtr()) = pNewParentMT;

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
                *EnsureWritablePages(&pMT->GetPerInstInfo()[iDict]) = pParentMT->GetPerInstInfo()[iDict];
        }
    }

#ifdef FEATURE_PREJIT
    // Restore action, not in MethodTable::Restore because we may have had approx parents at that point
    if (pMT->IsZapped())
    {
        MethodTable::InterfaceMapIterator it = pMT->IterateInterfaceMap();
        while (it.Next())
        {
            Module::RestoreMethodTablePointer(&it.GetInterfaceInfo()->m_pMethodTable, pMT->GetLoaderModule(), CLASS_LOAD_EXACTPARENTS);
        }
    }
    else
#endif
    {
        MethodTableBuilder::LoadExactInterfaceMap(pMT);
    }
    
#ifdef _DEBUG
    if (g_pConfig->ShouldDumpOnClassLoad(pMT->GetDebugClassName()))
    {
        pMT->Debug_DumpInterfaceMap("Exact");
    }
#endif //_DEBUG
} // ClassLoader::LoadExactParentAndInterfacesTransitively

// CLASS_LOAD_EXACTPARENTS phase of loading:
// * Load the base class at exact instantiation
// * Recurse LoadExactParents up parent hierarchy
// * Load explicitly declared interfaces on this class at exact instantiation
// * Fixup vtable
//
/*static*/
void ClassLoader::LoadExactParents(MethodTable *pMT)
{
    CONTRACT_VOID
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMT));
        POSTCONDITION(pMT->CheckLoadLevel(CLASS_LOAD_EXACTPARENTS));
    }
    CONTRACT_END;

    MethodTable *pApproxParentMT = pMT->GetParentMethodTable();

    if (!pMT->IsCanonicalMethodTable())
    {
        EnsureLoaded(TypeHandle(pMT->GetCanonicalMethodTable()), CLASS_LOAD_EXACTPARENTS);
    }

    LoadExactParentAndInterfacesTransitively(pMT);

    MethodTableBuilder::CopyExactParentSlots(pMT, pApproxParentMT);

    // We can now mark this type as having exact parents
    pMT->SetHasExactParent();

    RETURN;
}

//*******************************************************************************
// This is the routine that computes the internal type of a given type.  It normalizes
// structs that have only one field (of int/ptr sized values), to be that underlying type.
// 
// * see code:MethodTable#KindsOfElementTypes for more
// * It get used by code:TypeHandle::GetInternalCorElementType
CorElementType EEClass::ComputeInternalCorElementTypeForValueType(MethodTable * pMT)
{
    CONTRACTL {
        THROWS;
        GC_TRIGGERS;
    } CONTRACTL_END;

    if (pMT->GetNumInstanceFields() == 1 && (!pMT->HasLayout()
        || pMT->GetNumInstanceFieldBytes() == 4
#ifdef _WIN64
        || pMT->GetNumInstanceFieldBytes() == 8
#endif // _WIN64
        )) // Don't do the optimization if we're getting specified anything but the trivial layout.
    {
        FieldDesc * pFD = pMT->GetApproxFieldDescListRaw();
        CorElementType type = pFD->GetFieldType();

        if (type == ELEMENT_TYPE_VALUETYPE)
        {
            //@todo: Is it more apropos to call LookupApproxFieldTypeHandle() here?
            TypeHandle fldHnd = pFD->GetApproxFieldTypeHandleThrowing();
            CONSISTENCY_CHECK(!fldHnd.IsNull());

            type = fldHnd.GetInternalCorElementType();
        }

        switch (type)
        {
            // "DDB 20951: vc8 unmanaged pointer bug."
            // If ELEMENT_TYPE_PTR were returned, Compiler::verMakeTypeInfo would have problem 
            // creating a TI_STRUCT out of CORINFO_TYPE_PTR. 
            // As a result, the importer would not be able to realize that the thing on the stack 
            // is an instance of a valuetype (that contains one single "void*" field), rather than 
            // a pointer to a valuetype.
            // Returning ELEMENT_TYPE_U allows verMakeTypeInfo to go down the normal code path
            // for creating a TI_STRUCT.
            case ELEMENT_TYPE_PTR:
                type = ELEMENT_TYPE_U;
                
            case ELEMENT_TYPE_I:
            case ELEMENT_TYPE_U:
            case ELEMENT_TYPE_I4:
            case ELEMENT_TYPE_U4:
#ifdef _WIN64 
            case ELEMENT_TYPE_I8:
            case ELEMENT_TYPE_U8:
#endif // _WIN64
            
            {
                return type;
            }

            default:
                break;
        }
    }

    return ELEMENT_TYPE_VALUETYPE;
}

#if defined(CHECK_APP_DOMAIN_LEAKS) || defined(_DEBUG)
//*******************************************************************************
void EEClass::GetPredefinedAgility(Module *pModule, mdTypeDef td,
                                   BOOL *pfIsAgile, BOOL *pfCheckAgile)
{

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    //
    // There are 4 settings possible:
    // IsAgile  CheckAgile
    // F        F               (default)   Use normal type logic to determine agility
    // T        F               "Proxy"     Treated as agile even though may not be.
    // F        T               "Maybe"     Not agile, but specific instances can be made agile.
    // T        T               "Force"     All instances are forced agile, even though not typesafe.
    //
    // Also, note that object arrays of agile or maybe agile types are made maybe agile.
    //

    static const struct PredefinedAgility
    {
        const char  *name;
        BOOL        isAgile;
        BOOL        checkAgile;
    }

    // Matches based on name with the first records having higher precedence than subsequent ones
    // so that when there is an ambiguity, the first one will be used:
    // System.Globalization.CultureNotFoundException
    // comes before
    // System.Globalization.*
    //
    // although System.Globalization.CultureNotFoundException matches both records, the first
    // is the one that will be used
    agility[] =
    {
        // The Thread leak across context boundaries.
        // We manage the leaks manually
        { g_ThreadClassName,                    TRUE,   FALSE },

        // The SharedStatics class is a container for process-wide data
        { g_SharedStaticsClassName,             FALSE,  TRUE },

        // The extra dot at the start is to accomodate the string comparison logic below 
        // when there is no namespace for a type
        {".StringMaker",                        FALSE, TRUE },

        {g_StringBufferClassName,               FALSE, TRUE },

        { "System.ActivationArguments",         FALSE,  TRUE },
        { "System.AppDomainSetup" ,             FALSE,  TRUE },
        { "System.AppDomainInitializerInfo",    FALSE,  TRUE },

        // Make all containers maybe agile
        { "System.Collections.*",               FALSE,  TRUE },
        { "System.Collections.Generic.*",               FALSE,  TRUE },

        // Make all globalization objects agile except for System.Globalization.CultureNotFoundException
        // The exception inherits from ArgumentException so needs the same agility
        // this must come before the more general declaration below so that it will match first
        { "System.Globalization.CultureNotFoundException",             FALSE,  FALSE },
        // We have CultureInfo objects on thread.  Because threads leak across
        // app domains, we have to be prepared for CultureInfo to leak across.
        // CultureInfo exposes all of the other globalization objects, so we
        // just make the entire namespace app domain agile.
        { "System.Globalization.*",             FALSE,  TRUE },

        // Remoting structures for legally smuggling messages across app domains
        { "System.Runtime.Remoting.Messaging.SmuggledMethodCallMessage", FALSE,  TRUE },
        { "System.Runtime.Remoting.Messaging.SmuggledMethodReturnMessage", FALSE,  TRUE },
        { "System.Runtime.Remoting.Messaging.SmuggledObjRef", FALSE, TRUE},
        { "System.Runtime.Remoting.ObjRef", FALSE,  TRUE },
        { "System.Runtime.Remoting.ChannelInfo", FALSE,  TRUE },
        { "System.Runtime.Remoting.Channels.CrossAppDomainData", FALSE,  TRUE },

        // Remoting cached data structures are all in mscorlib
        { "System.Runtime.Remoting.Metadata.RemotingCachedData",       FALSE,  TRUE },
        { "System.Runtime.Remoting.Metadata.RemotingFieldCachedData", FALSE,  TRUE },
        { "System.Runtime.Remoting.Metadata.RemotingParameterCachedData", FALSE,  TRUE },
        { "System.Runtime.Remoting.Metadata.RemotingMethodCachedData", FALSE,  TRUE },
        { "System.Runtime.Remoting.Metadata.RemotingTypeCachedData", FALSE,  TRUE },
        { "System.Runtime.Remoting.Metadata.SoapAttribute",      FALSE,  TRUE },
        { "System.Runtime.Remoting.Metadata.SoapFieldAttribute", FALSE,  TRUE },
        { "System.Runtime.Remoting.Metadata.SoapMethodAttribute",FALSE,  TRUE },
        { "System.Runtime.Remoting.Metadata.SoapParameterAttribute", FALSE,  TRUE },
        { "System.Runtime.Remoting.Metadata.SoapTypeAttribute",  FALSE,  TRUE },

        // Reflection types
        { g_ReflectionMemberInfoName,                            FALSE,  TRUE },
        { g_TypeClassName,                                       FALSE,  TRUE },
        { g_ReflectionClassName,                                 FALSE,  TRUE },
        { g_ReflectionConstructorInfoName,                       FALSE,  TRUE },
        { g_ReflectionConstructorName,                           FALSE,  TRUE },
        { g_ReflectionEventInfoName,                             FALSE,  TRUE },
        { g_ReflectionEventName,                                 FALSE,  TRUE },
        { g_ReflectionFieldInfoName,                             FALSE,  TRUE },
        { g_ReflectionFieldName,                                 FALSE,  TRUE },
        { g_MethodBaseName,                                      FALSE,  TRUE },
        { g_ReflectionMethodInfoName,                            FALSE,  TRUE },
        { g_ReflectionMethodName,                                FALSE,  TRUE },
        { g_ReflectionPropertyInfoName,                          FALSE,  TRUE },
        { g_ReflectionPropInfoName,                              FALSE,  TRUE },
        { g_ReflectionParamInfoName,                             FALSE,  TRUE },
        { g_ReflectionParamName,                                 FALSE,  TRUE },

        { "System.RuntimeType+RuntimeTypeCache",                 FALSE,  TRUE },
        { "System.RuntimeType+RuntimeTypeCache+MemberInfoCache`1", FALSE,  TRUE },
        { "System.RuntimeType+RuntimeTypeCache+MemberInfoCache`1+Filter", FALSE,  TRUE },
        { "System.Reflection.CerHashtable`2",                    FALSE,  TRUE },
        { "System.Reflection.CerHashtable`2+Table",              FALSE,  TRUE },
        { "System.Reflection.RtFieldInfo",                       FALSE,  TRUE },
        { "System.Reflection.MdFieldInfo",                       FALSE,  TRUE },
        { "System.Signature",                                    FALSE,  TRUE },
        { "System.Reflection.MetadataImport",                    FALSE,  TRUE },

        // LogSwitches are agile even though we can't prove it
        // <TODO>@todo: do they need really to be?</TODO>
        { "System.Diagnostics.LogSwitch",       FALSE,  TRUE },

        // There is a process global PermissionTokenFactory
        { "System.Security.PermissionToken",    FALSE,  TRUE },
        { g_PermissionTokenFactoryName,         FALSE,  TRUE },

        // Mark all the exceptions we throw agile.  This makes
        // most BVTs pass even though exceptions leak
        //
        // Note that making exception checked automatically
        // makes a bunch of subclasses checked as well.
        //
        // Pre-allocated exceptions
        { g_ExceptionClassName,                 FALSE,  TRUE },
        { g_OutOfMemoryExceptionClassName,      FALSE,  TRUE },
        { g_StackOverflowExceptionClassName,    FALSE,  TRUE },
        { g_ExecutionEngineExceptionClassName,  FALSE,  TRUE },

        // SecurityDocument contains pointers and other agile types
        { "System.Security.SecurityDocument",    TRUE, TRUE },

        // BinaryFormatter smuggles these across appdomains.
        { "System.Runtime.Serialization.Formatters.Binary.BinaryObjectWithMap", TRUE, FALSE},
        { "System.Runtime.Serialization.Formatters.Binary.BinaryObjectWithMapTyped", TRUE, FALSE},

        { NULL }
    };

    if (pModule == SystemDomain::SystemModule())
    {
        while (TRUE)
        {
            LPCUTF8 pszName;
            LPCUTF8 pszNamespace;
            HRESULT     hr;
            mdTypeDef   tdEnclosing;
            
            if (FAILED(pModule->GetMDImport()->GetNameOfTypeDef(td, &pszName, &pszNamespace)))
            {
                break;
            }
            
            // We rely the match algorithm matching the first items in the list before subsequent ones
            // so that when there is an ambiguity, the first one will be used:
            // System.Globalization.CultureNotFoundException
            // comes before
            // System.Globalization.*
            //
            // although System.Globalization.CultureNotFoundException matches both records, the first
            // is the one that will be used
            const PredefinedAgility *p = agility;
            while (p->name != NULL)
            {
                SIZE_T length = strlen(pszNamespace);
                if (strncmp(pszNamespace, p->name, length) == 0
                    && (strcmp(pszName, p->name + length + 1) == 0
                        || strcmp("*", p->name + length + 1) == 0))
                {
                    *pfIsAgile = p->isAgile;
                    *pfCheckAgile = p->checkAgile;
                    return;
                }

                p++;
            }

            // Perhaps we have a nested type like 'bucket' that is supposed to be
            // agile or checked agile by virtue of being enclosed in a type like
            // hashtable, which is itself inside "System.Collections".
            tdEnclosing = mdTypeDefNil;
            hr = pModule->GetMDImport()->GetNestedClassProps(td, &tdEnclosing);
            if (SUCCEEDED(hr))
            {
                BAD_FORMAT_NOTHROW_ASSERT(tdEnclosing != td && TypeFromToken(tdEnclosing) == mdtTypeDef);
                td = tdEnclosing;
            }
            else
                break;
        }
    }

    *pfIsAgile = FALSE;
    *pfCheckAgile = FALSE;
}

//*******************************************************************************
void EEClass::SetAppDomainAgileAttribute(MethodTable * pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        //        PRECONDITION(!IsAppDomainAgilityDone());
    }
    CONTRACTL_END

    EEClass * pClass = pMT->GetClass();

    //
    // The most general case for provably a agile class is
    // (1) No instance fields of non-sealed or non-agile types
    // (2) Class is in system domain (its type must be not unloadable
    //      & loaded in all app domains)
    // (3) The class can't have a finalizer
    // (4) The class can't be a COMClass
    //

    _ASSERTE(!pClass->IsAppDomainAgilityDone());

    BOOL    fCheckAgile     = FALSE;
    BOOL    fAgile          = FALSE;
    BOOL    fFieldsAgile    = TRUE;
    WORD        nFields         = 0;

    if (!pMT->GetModule()->IsSystem())
    {
        //
        // No types outside of the system domain can even think about
        // being agile
        //

        goto exit;
    }

    if (pMT->IsComObjectType())
    {
        //
        // No COM type is agile, as there is domain specific stuff in the sync block
        //

        goto exit;
    }

    if (pMT->IsInterface())
    {
        //
        // Don't mark interfaces agile
        //

        goto exit;
    }

    if (pMT->ContainsGenericVariables())
    {
        // Types containing formal type parameters aren't agile
        goto exit;
    }

    //
    // See if we need agile checking in the class
    //

    GetPredefinedAgility(pMT->GetModule(), pMT->GetCl(),
                         &fAgile, &fCheckAgile);

    if (pMT->HasFinalizer())
    {
        if (!fAgile && !fCheckAgile)
        {
            //
            // If we're finalizable, we need domain affinity.  Otherwise, we may appear
            // to a particular app domain not to call the finalizer (since it may run
            // in a different domain.)
            //
            // Note: do not change this assumption. The eager finalizaton code for
            // appdomain unloading assumes that no obects other than those in mscorlib
            // can be agile and finalizable  
            //
            goto exit;
        }
        else
        {

            // Note that a finalizable object will be considered potentially agile if it has one of the two
            // predefined agility bits set. This will cause an assert in the eager finalization code if you add
            // a finalizer to such a class - we don't want to have them as we can't run them eagerly and running
            // them after we've cleared the roots/handles means it can't do much safely. Right now thread is the
            // only one we allow.  
            _ASSERTE(g_pThreadClass == NULL || pMT->IsAgileAndFinalizable());
        }
    }

    //
    // Now see if the type is "naturally agile" - that is, it's type structure
    // guarantees agility.
    //

    if (pMT->GetParentMethodTable() != NULL)
    {
        EEClass * pParentClass = pMT->GetParentMethodTable()->GetClass();

        //
        // Make sure our parent was computed.  This should only happen
        // when we are prejitting - otherwise it is computed for each
        // class as its loaded.
        //

        _ASSERTE(pParentClass->IsAppDomainAgilityDone());

        if (!pParentClass->IsAppDomainAgile())
        {
            fFieldsAgile = FALSE;
            if (fCheckAgile)
                _ASSERTE(pParentClass->IsCheckAppDomainAgile());
        }

        //
        // To save having to list a lot of trivial (layout-wise) subclasses,
        // automatically check a subclass if its parent is checked and
        // it introduces no new fields.
        //

        if (!fCheckAgile
            && pParentClass->IsCheckAppDomainAgile()
            && pClass->GetNumInstanceFields() == pParentClass->GetNumInstanceFields())
            fCheckAgile = TRUE;
    }

    nFields = pMT->GetNumInstanceFields()
        - (pMT->GetParentMethodTable() == NULL ? 0 : pMT->GetParentMethodTable()->GetNumInstanceFields());

    if (fFieldsAgile || fCheckAgile)
    {
        FieldDesc *pFD = pClass->GetFieldDescList();
        FieldDesc *pFDEnd = pFD + nFields;
        while (pFD < pFDEnd)
        {
            switch (pFD->GetFieldType())
            {
            case ELEMENT_TYPE_CLASS:
                {
                    //
                    // There is a bit of a problem in computing the classes which are naturally agile -
                    // we don't want to load types of non-value type fields.  So for now we'll
                    // err on the side of conservatism and not allow any non-value type fields other than
                    // the forced agile types listed above.
                    //

                    MetaSig sig(pFD);
                    CorElementType type = sig.NextArg();
                    SigPointer sigPtr = sig.GetArgProps();

                    //
                    // Don't worry about strings
                    //

                    if (type == ELEMENT_TYPE_STRING)
                        break;

                    // Find our field's token so we can proceed cautiously
                    mdToken token = mdTokenNil;

                    if (type == ELEMENT_TYPE_CLASS)
                        IfFailThrow(sigPtr.GetToken(&token));

                    //
                    // First, a special check to see if the field is of our own type.
                    //

                    if (token == pMT->GetCl() && pMT->IsSealed())
                        break;

                    //
                    // Now, look for the field's TypeHandle.
                    //
                    // <TODO>@todo: there is some ifdef'd code here to to load the type if it's
                    // not already loading.  This code has synchronization problems, as well
                    // as triggering more aggressive loading than normal.  So it's disabled
                    // for now.
                    // </TODO>

                    TypeHandle th;
#if 0 
                    if (TypeFromToken(token) == mdTypeDef
                        && GetClassLoader()->FindUnresolvedClass(GetModule, token) == NULL)
                        th = pFD->GetFieldTypeHandleThrowing();
                    else
#endif // 0
                        th = pFD->LookupFieldTypeHandle();

                    //
                    // See if the referenced type is agile.  Note that there is a reasonable
                    // chance that the type hasn't been loaded yet.  If this is the case,
                    // we just have to assume that it's not agile, since we can't trigger
                    // extra loads here (for fear of circular recursion.)
                    //
                    // If you have an agile class which runs into this problem, you can solve it by
                    // setting the type manually to be agile.
                    //

                    if (th.IsNull()
                        || !th.IsAppDomainAgile()
                        || (!th.IsTypeDesc()
                            && !th.AsMethodTable()->IsSealed()))
                    {
                        //
                        // Treat the field as non-agile.
                        //

                        fFieldsAgile = FALSE;
                        if (fCheckAgile)
                            pFD->SetDangerousAppDomainAgileField();
                    }
                }

                break;

            case ELEMENT_TYPE_VALUETYPE:
                {
                    TypeHandle th;

                    {
                        // Loading a non-self-ref valuetype field.
                        OVERRIDE_TYPE_LOAD_LEVEL_LIMIT(CLASS_LOADED);

                        th = pFD->GetApproxFieldTypeHandleThrowing();
                    }

                    _ASSERTE(!th.IsNull());

                    if (!th.IsAppDomainAgile())
                    {
                        fFieldsAgile = FALSE;
                        if (fCheckAgile)
                            pFD->SetDangerousAppDomainAgileField();
                    }
                }

                break;

            default:
                break;
            }

            pFD++;
        }
    }

    if (fFieldsAgile || fAgile)
        pClass->SetAppDomainAgile();

    if (fCheckAgile && !fFieldsAgile)
        pClass->SetCheckAppDomainAgile();

exit:
    LOG((LF_CLASSLOADER, LL_INFO1000, "CLASSLOADER: AppDomainAgileAttribute for %s is %d\n", pClass->GetDebugClassName(), pClass->IsAppDomainAgile()));
    pClass->SetAppDomainAgilityDone();
}
#endif // defined(CHECK_APP_DOMAIN_LEAKS) || defined(_DEBUG)

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

#ifdef FEATURE_HFA
//*******************************************************************************
CorElementType MethodTable::GetHFAType()
{
    CONTRACTL
    {
        WRAPPER(THROWS);        // we end up in the class loader which has the conditional contracts
        WRAPPER(GC_TRIGGERS);
    }
    CONTRACTL_END;

    if (!IsHFA())
        return ELEMENT_TYPE_END;

    MethodTable * pMT = this;
    for (;;)
    {
        _ASSERTE(pMT->IsValueType());
        _ASSERTE(pMT->GetNumInstanceFields() > 0);

        PTR_FieldDesc pFirstField = pMT->GetApproxFieldDescListRaw();

        CorElementType fieldType = pFirstField->GetFieldType();
        
        // All HFA fields have to be of the same type, so we can just return the type of the first field
        switch (fieldType)
        {
        case ELEMENT_TYPE_VALUETYPE:
            pMT = pFirstField->LookupApproxFieldTypeHandle().GetMethodTable();
            break;
            
        case ELEMENT_TYPE_R4:
        case ELEMENT_TYPE_R8:
            return fieldType;

        default:
            // This should never happen. MethodTable::IsHFA() should be set only on types
            // that have a valid HFA type when the flag is used to track HFA status.
            _ASSERTE(false);
            return ELEMENT_TYPE_END;
        }
    }    
}

bool MethodTable::IsNativeHFA()
{
    LIMITED_METHOD_CONTRACT;
    return HasLayout() ? GetLayoutInfo()->IsNativeHFA() : IsHFA();
}

CorElementType MethodTable::GetNativeHFAType()
{
    LIMITED_METHOD_CONTRACT;
    return HasLayout() ? GetLayoutInfo()->GetNativeHFAType() : GetHFAType();
}
#endif // FEATURE_HFA

#ifdef FEATURE_64BIT_ALIGNMENT
// Returns true iff the native view of this type requires 64-bit aligment.
bool MethodTable::NativeRequiresAlign8()
{
    LIMITED_METHOD_CONTRACT;

    if (HasLayout())
    {
        return (GetLayoutInfo()->GetLargestAlignmentRequirementOfAllMembers() >= 8);
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

    if (!IsProjectedFromWinRT()) // ignore classic COM interop CA on WinRT types
    {
        HRESULT hr = GetMDImport()->GetCustomAttributeByName(GetCl(), INTEROP_COCLASS_TYPE , (const void **)&pVal, &cbVal);
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
            CoClassType = TypeName::GetTypeUsingCASearchRules(ss.GetUnicode(), GetAssembly());

            // Cache the coclass type
            g_IBCLogger.LogEEClassCOWTableAccess(this);
            GetClass_NoLogging()->SetCoClassForInterface(CoClassType);
        }
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
    SrcItfType = TypeName::GetTypeUsingCASearchRules(ss.GetUnicode(), GetAssembly());

    // Retrieve the COM event provider class name.
    IfFailThrow(cap.GetNonNullString(&szName, &cbName));

    // Copy the name to a temporary buffer and NULL terminate it.
    ss.SetUTF8(szName, cbName);

    // Try to load the class using its name as a fully qualified name. If that fails,
    // then we try to load it in the assembly of the current class.
    EventProvType = TypeName::GetTypeUsingCASearchRules(ss.GetUnicode(), GetAssembly());

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
        return TypeHandle(it.GetInterface());
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
                                GetApproxArrayElementTypeHandle(),
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
        SO_TOLERANT;
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

    if (IsProjectedFromWinRT())
    {
        // WinRT interfaces are always IInspectable-based
        ItfType = ifInspectable;
    }
    else
    {
        // Retrieve the interface type from the metadata.
        HRESULT hr = GetMDImport()->GetIfaceTypeOfTypeDef(GetCl(), (ULONG*)&ItfType);
        IfFailThrow(hr);

        if (hr != S_OK)
        {
            // if not found in metadata, use the default
            ItfType = ifDual;
        }
    }

    // Cache the interface type
    g_IBCLogger.LogEEClassCOWTableAccess(this);
    GetClass_NoLogging()->SetComInterfaceType(ItfType);

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
        
        ReadBestFitCustomAttribute(pMT->GetMDImport(), pMT->GetCl(), pfBestFitMapping, pfThrowOnUnmappableChar);

        DWORD flags = VMFLAG_BESTFITMAPPING_INITED;
        if (*pfBestFitMapping) flags |= VMFLAG_BESTFITMAPPING;
        if (*pfThrowOnUnmappableChar) flags |= VMFLAG_THROWONUNMAPPABLECHAR;

        FastInterlockOr(EnsureWritablePages(&pClass->m_VMFlags), flags);
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
                ssBuff.Printf(W("%S:\n"), pszClassName);
                WszOutputDebugString(ssBuff.GetUnicode());
            }
            else {
                 LOG((LF_CLASSLOADER, LL_ALWAYS, "%s:\n", pszClassName));
            }

            for (i = 0; i < (GetNumInstanceFields()-cParentInstanceFields); i++)
            {
                FieldDesc *pFD = &GetClass()->GetFieldDescList()[i];
#ifdef DEBUG_LAYOUT
                printf("offset %s%3d %s\n", pFD->IsByValue() ? "byvalue " : "", pFD->GetOffset_NoLogging(), pFD->GetName());
#endif
                if(debug) {
                    ssBuff.Printf(W("offset %3d %S\n"), pFD->GetOffset_NoLogging(), pFD->GetName());
                    WszOutputDebugString(ssBuff.GetUnicode());
                }
                else {
                    LOG((LF_CLASSLOADER, LL_ALWAYS, "offset %3d %s\n", pFD->GetOffset_NoLogging(), pFD->GetName()));
                }
            }
        }
    }
    EX_CATCH
    {
        if(debug)
        {
            WszOutputDebugString(W("<Exception Thrown>\n"));
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
            ssBuff.Printf(W("Field layout for '%S':\n\n"), pszClassName);
            WszOutputDebugString(ssBuff.GetUnicode());
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
                WszOutputDebugString(W("Static fields (stored at vtable offsets)\n"));
                WszOutputDebugString(W("----------------------------------------\n"));
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
                    ssBuff.Printf(W("offset %3d %S\n"), pFD->GetOffset_NoLogging(), pFD->GetName());
                    WszOutputDebugString(ssBuff.GetUnicode());
                }
                else
                {
                    //LF_ALWAYS allowed here because this is controlled by special env var ShouldDumpOnClassLoad
                    LOG((LF_ALWAYS, LL_ALWAYS, "offset %3d %s\n", pFD->GetOffset_NoLogging(), pFD->GetName()));
                }
            }
        }

        if (GetNumInstanceFields() > 0)
        {
            if (GetNumStaticFields()) {
                if(debug) {
                    WszOutputDebugString(W("\n"));
                }
                else
                {
                    //LF_ALWAYS allowed here because this is controlled by special env var ShouldDumpOnClassLoad
                    LOG((LF_ALWAYS, LL_ALWAYS, "\n"));
                }
            }
            
            if (debug)
            {
                WszOutputDebugString(W("Instance fields\n"));
                WszOutputDebugString(W("---------------\n"));
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
            WszOutputDebugString(W("\n"));
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
            WszOutputDebugString(W("<Exception Thrown>\n"));
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
            ssBuff.Printf(W("GC description for '%S':\n\n"), pszClassName);
            WszOutputDebugString(ssBuff.GetUnicode());
        }
        else
        {
            //LF_ALWAYS allowed here because this is controlled by special env var ShouldDumpOnClassLoad
            LOG((LF_ALWAYS, LL_ALWAYS, "GC description for '%s':\n\n", pszClassName));
        }
        
        if (ContainsPointersOrCollectible())
        {
            CGCDescSeries *pSeries;
            CGCDescSeries *pHighest;
            
            if (fDebug)
            {
                WszOutputDebugString(W("GCDesc:\n"));
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
                    ssBuff.Printf(W("   offset %5d (%d w/o Object), size %5d (%5d w/o BaseSize subtr)\n"),
                        pSeries->GetSeriesOffset(),
                        pSeries->GetSeriesOffset() - sizeof(Object),
                        pSeries->GetSeriesSize(),
                        pSeries->GetSeriesSize() + GetBaseSize() );
                    WszOutputDebugString(ssBuff.GetUnicode());
                }
                else
                {
                    //LF_ALWAYS allowed here because this is controlled by special env var ShouldDumpOnClassLoad
                    LOG((LF_ALWAYS, LL_ALWAYS, "   offset %5d (%d w/o Object), size %5d (%5d w/o BaseSize subtr)\n",
                         pSeries->GetSeriesOffset(),
                         pSeries->GetSeriesOffset() - sizeof(Object),
                         pSeries->GetSeriesSize(),
                         pSeries->GetSeriesSize() + GetBaseSize()
                         ));
                }
                pSeries++;
            }
            
            if (fDebug)
            {
                WszOutputDebugString(W("\n"));
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
            WszOutputDebugString(W("<Exception Thrown>\n"));
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

    // If the class does not support IClassX because it derives from or implements WinRT types,
    // then it is considered ClassInterfaceType.None unless explicitly overriden by the CA
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


//*******************************************************************************
#ifdef FEATURE_PREJIT
DWORD EEClass::GetSize()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    // Total instance size consists of the fixed ("normal") fields, cached at construction time and dependent
    // on whether we're a vanilla EEClass or DelegateEEClass etc., and a portion for the packed fields tacked on
    // the end. The size of the packed fields can be retrieved from the fields themselves or, if we were
    // unsuccessful in our attempts to compress the data, the full size of the EEClassPackedFields structure
    // (which is essentially just a DWORD array of all the field values).
    return m_cbFixedEEClassFields +
        (m_fFieldsArePacked ? GetPackedFields()->GetPackedSize() : sizeof(EEClassPackedFields));
}
#endif // FEATURE_PREJIT

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
        SO_TOLERANT;
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

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
//*******************************************************************************
void SparseVTableMap::Save(DataImage *image)
{
    STANDARD_VM_CONTRACT;

    image->StoreStructure(this, sizeof(SparseVTableMap),
                                    DataImage::ITEM_SPARSE_VTABLE_MAP_TABLE);

    // Trim unused portion of the table
    m_Allocated = m_MapEntries;

    image->StoreInternedStructure(m_MapList, m_Allocated * sizeof(Entry),
                                    DataImage::ITEM_SPARSE_VTABLE_MAP_ENTRIES);
}

//*******************************************************************************
void SparseVTableMap::Fixup(DataImage *image)
{
    STANDARD_VM_CONTRACT;

    image->FixupPointerField(this, offsetof(SparseVTableMap, m_MapList));
}
#endif //FEATURE_NATIVE_IMAGE_GENERATION
#endif //FEATURE_COMINTEROP

#ifdef FEATURE_NATIVE_IMAGE_GENERATION

//*******************************************************************************
void EEClass::Save(DataImage *image, MethodTable *pMT)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(this == pMT->GetClass());
        PRECONDITION(pMT->IsCanonicalMethodTable());
        PRECONDITION(pMT->IsFullyLoaded());
        PRECONDITION(!image->IsStored(this));
        PRECONDITION(image->GetModule()->GetAssembly() ==
                 GetAppDomain()->ToCompilationDomain()->GetTargetAssembly());
    }
    CONTRACTL_END;

    LOG((LF_ZAP, LL_INFO10000, "EEClass::Save %s (%p)\n", m_szDebugClassName, this));

    // Optimize packable fields before saving into ngen image (the packable fields are located at the end of
    // the EEClass or sub-type instance and packing will transform them into a space-efficient format which
    // should reduce the result returned by the GetSize() call below). Packing will fail if the compression
    // algorithm would result in an increase in size. We track this in the m_fFieldsArePacked data member
    // which we use to determine whether to access the fields in their packed or unpacked format.
    // Special case: we don't attempt to pack fields for the System.Threading.OverlappedData class since a
    // host can change the size of this at runtime. This requires modifying one of the packable fields and we
    // don't support updates to such fields if they were successfully packed.
    if (g_pOverlappedDataClass == NULL)
    {
        g_pOverlappedDataClass = MscorlibBinder::GetClass(CLASS__OVERLAPPEDDATA);
        _ASSERTE(g_pOverlappedDataClass);
    }
    if (this != g_pOverlappedDataClass->GetClass())
        m_fFieldsArePacked = GetPackedFields()->PackFields();

    DWORD cbSize = GetSize();

    // ***************************************************************
    // Only put new actions in this function if they really relate to EEClass
    // rather than MethodTable.  For example, if you need to allocate
    // a per-type entry in some table in the NGEN image, then you will probably
    // need to allocate one such entry per MethodTable, e.g. per generic
    // instantiation.  You probably don't want to allocate one that is common
    // to a group of shared instantiations.  
    // ***************************************************************

    DataImage::ItemKind item =
        (!pMT->IsGenericTypeDefinition() && pMT->ContainsGenericVariables())
        ? DataImage::ITEM_EECLASS_COLD
        // Until we get all the access paths for generics tidied up, many paths touch the EEClass, e.g. GetInstantiation()
        : pMT->HasInstantiation()
        ? DataImage::ITEM_EECLASS_WARM
        : DataImage::ITEM_EECLASS;

    // Save optional fields if we have any.
    if (HasOptionalFields())
        image->StoreStructure(GetOptionalFields(),
                              sizeof(EEClassOptionalFields),
                              item);

#ifdef _DEBUG 
    if (!image->IsStored(m_szDebugClassName))
        image->StoreStructure(m_szDebugClassName, (ULONG)(strlen(m_szDebugClassName)+1),
                              DataImage::ITEM_DEBUG,
                              1);
#endif // _DEBUG

#ifdef FEATURE_COMINTEROP 
    if (GetSparseCOMInteropVTableMap() != NULL)
        GetSparseCOMInteropVTableMap()->Save(image);
#endif // FEATURE_COMINTEROP

    //
    // Save MethodDescs
    //

    MethodDescChunk *chunk = GetChunks();
    if (chunk != NULL)
    {
        MethodDesc::SaveChunk methodDescSaveChunk(image);

        MethodTable::IntroducedMethodIterator it(pMT, TRUE);
        for (; it.IsValid(); it.Next())
        {
            MethodDesc * pMD = it.GetMethodDesc();

            // Do not save IL stubs that we have failed to generate code for
            if (pMD->IsILStub() && image->GetCodeAddress(pMD) == NULL)
                continue;

            methodDescSaveChunk.Append(pMD);
        }

        ZapStoredStructure * pChunksNode = methodDescSaveChunk.Save();
        if (pChunksNode != NULL)    
            image->BindPointer(chunk, pChunksNode, 0);

    }

    //
    // Save FieldDescs
    //

    SIZE_T fieldCount = FieldDescListSize(pMT);

    if (fieldCount != 0)
    {
        FieldDesc *pFDStart = GetFieldDescList();
        FieldDesc *pFDEnd = pFDStart + fieldCount;

        FieldDesc *pFD = pFDStart;
        while (pFD < pFDEnd)
        {
            pFD->PrecomputeNameHash();
            pFD++;
        }

        ZapStoredStructure * pFDNode = image->StoreStructure(pFDStart, (ULONG)(fieldCount * sizeof(FieldDesc)),
                                        DataImage::ITEM_FIELD_DESC_LIST);

        pFD = pFDStart;
        while (pFD < pFDEnd)
        {
            pFD->SaveContents(image);
            if (pFD != pFDStart)
                image->BindPointer(pFD, pFDNode, (BYTE *)pFD - (BYTE *)pFDStart);
            pFD++;
        }
    }

    //
    // Save MethodDescs
    //

    if (HasLayout())
    {
        EEClassLayoutInfo *pInfo = &((LayoutEEClass*)this)->m_LayoutInfo;

        if (pInfo->m_numCTMFields > 0)
        {
            ZapStoredStructure * pNode = image->StoreStructure(pInfo->m_pFieldMarshalers,
                                            pInfo->m_numCTMFields * MAXFIELDMARSHALERSIZE,
                                            DataImage::ITEM_FIELD_MARSHALERS);

            for (UINT iField = 0; iField < pInfo->m_numCTMFields; iField++)
            {
                FieldMarshaler *pFM = (FieldMarshaler*)((BYTE *)pInfo->m_pFieldMarshalers + iField * MAXFIELDMARSHALERSIZE);
                pFM->Save(image);

                if (iField > 0)
                    image->BindPointer(pFM, pNode, iField * MAXFIELDMARSHALERSIZE);
            }
        }
    }

    // Save dictionary layout information
    DictionaryLayout *pDictLayout = GetDictionaryLayout();
    if (pMT->IsSharedByGenericInstantiations() && pDictLayout != NULL)
    {
        pDictLayout->Save(image);
        LOG((LF_ZAP, LL_INFO10000, "ZAP: dictionary for %s has %d slots used out of possible %d\n", m_szDebugClassName,
             pDictLayout->GetNumUsedSlots(), pDictLayout->GetMaxSlots()));
    }

    if (GetVarianceInfo() != NULL)
        image->StoreInternedStructure(GetVarianceInfo(),
                              pMT->GetNumGenericArgs(),
                              DataImage::ITEM_CLASS_VARIANCE_INFO);

    image->StoreStructure(this, cbSize, item);

    if (pMT->IsInterface())
    {
        // Make sure our guid is computed

#ifdef FEATURE_COMINTEROP
        // Generic WinRT types can have their GUID computed only if the instantiation is WinRT-legal
        if (!pMT->IsProjectedFromWinRT() ||
            !pMT->SupportsGenericInterop(TypeHandle::Interop_NativeToManaged) ||
             pMT->IsLegalNonArrayWinRTType())
#endif // FEATURE_COMINTEROP
        {
            GUID dummy;
            if (SUCCEEDED(pMT->GetGuidNoThrow(&dummy, TRUE, FALSE)))
            {
                GuidInfo* pGuidInfo = pMT->GetGuidInfo();
                _ASSERTE(pGuidInfo != NULL);

                image->StoreStructure(pGuidInfo, sizeof(GuidInfo),
                                      DataImage::ITEM_GUID_INFO);

#ifdef FEATURE_COMINTEROP
                if (pMT->IsLegalNonArrayWinRTType())
                {
                    Module *pModule = pMT->GetModule();
                    if (pModule->CanCacheWinRTTypeByGuid(pMT))
                    {
                        pModule->CacheWinRTTypeByGuid(pMT, pGuidInfo);
                    }
                }
#endif // FEATURE_COMINTEROP
            }
            else
            {
                // make sure we don't store a GUID_NULL guid in the NGEN image
                // instead we'll compute the GUID at runtime, and throw, if appropriate
                m_pGuidInfo.SetValueMaybeNull(NULL);
            }
        }
    }

#ifdef FEATURE_COMINTEROP
    if (IsDelegate())
    {
        DelegateEEClass *pDelegateClass = (DelegateEEClass *)this;
        ComPlusCallInfo *pComInfo = pDelegateClass->m_pComPlusCallInfo;

        if (pComInfo != NULL && pComInfo->ShouldSave(image))
        {
            image->StoreStructure(pDelegateClass->m_pComPlusCallInfo,
                                  sizeof(ComPlusCallInfo),
                                  item);
        }
    }
#endif // FEATURE_COMINTEROP

    LOG((LF_ZAP, LL_INFO10000, "EEClass::Save %s (%p) complete.\n", m_szDebugClassName, this));
}

//*******************************************************************************
DWORD EEClass::FieldDescListSize(MethodTable * pMT)
{
    LIMITED_METHOD_CONTRACT;

    EEClass * pClass = pMT->GetClass();
    DWORD fieldCount = pClass->GetNumInstanceFields() + pClass->GetNumStaticFields();

    MethodTable * pParentMT = pMT->GetParentMethodTable();
    if (pParentMT != NULL)
        fieldCount -= pParentMT->GetNumInstanceFields();
    return fieldCount;
}

//*******************************************************************************
void EEClass::Fixup(DataImage *image, MethodTable *pMT)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(this == pMT->GetClass());
        PRECONDITION(pMT->IsCanonicalMethodTable());
        PRECONDITION(pMT->IsFullyLoaded());
        PRECONDITION(image->IsStored(this));
    }
    CONTRACTL_END;

    LOG((LF_ZAP, LL_INFO10000, "EEClass::Fixup %s (%p)\n", GetDebugClassName(), this));

    // Fixup pointer to optional fields if this class has any. This pointer is a relative pointer (to avoid
    // the need for base relocation fixups) and thus needs to use the IMAGE_REL_BASED_RELPTR fixup type.
    if (HasOptionalFields())
        image->FixupRelativePointerField(this, offsetof(EEClass, m_rpOptionalFields));

#ifdef _DEBUG 
    image->FixupPointerField(this, offsetof(EEClass, m_szDebugClassName));
#endif

#ifdef FEATURE_COMINTEROP 
    if (GetSparseCOMInteropVTableMap() != NULL)
    {
        image->FixupPointerField(GetOptionalFields(), offsetof(EEClassOptionalFields, m_pSparseVTableMap));
        GetSparseCOMInteropVTableMap()->Fixup(image);
    }
#endif // FEATURE_COMINTEROP

    DictionaryLayout *pDictLayout = GetDictionaryLayout();
    if (pDictLayout != NULL)
    {
        pDictLayout->Fixup(image, FALSE);
        image->FixupPointerField(GetOptionalFields(), offsetof(EEClassOptionalFields, m_pDictLayout));
    }

    if (HasOptionalFields())
        image->FixupRelativePointerField(GetOptionalFields(), offsetof(EEClassOptionalFields, m_pVarianceInfo));

    //
    // We pass in the method table, because some classes (e.g. remoting proxy)
    // have fake method tables set up in them & we want to restore the regular
    // one.
    //
    image->FixupField(this, offsetof(EEClass, m_pMethodTable), pMT, 0, IMAGE_REL_BASED_RelativePointer);

    //
    // Fixup MethodDescChunk and MethodDescs
    //
    MethodDescChunk* pChunks = GetChunks();

    if (pChunks!= NULL && image->IsStored(pChunks))
    {
        image->FixupRelativePointerField(this, offsetof(EEClass, m_pChunks));

        MethodTable::IntroducedMethodIterator it(pMT, TRUE);
        for (; it.IsValid(); it.Next())
        {
            MethodDesc * pMD = it.GetMethodDesc();

            // Skip IL stubs that were not saved into the image
            if (pMD->IsILStub() && !image->IsStored(pMD))
                continue;

            it.GetMethodDesc()->Fixup(image);
        }

    }
    else
    {
        image->ZeroPointerField(this, offsetof(EEClass, m_pChunks));
    }

    //
    // Fixup FieldDescs
    //

    SIZE_T fieldCount = FieldDescListSize(pMT);

    if (fieldCount != 0)
    {
        image->FixupRelativePointerField(this, offsetof(EEClass, m_pFieldDescList));

        FieldDesc *pField = GetFieldDescList();
        FieldDesc *pFieldEnd = pField + fieldCount;
        while (pField < pFieldEnd)
        {
            pField->Fixup(image);
            pField++;
        }
    }

#ifdef FEATURE_COMINTEROP 
    // These fields will be lazy inited if we zero them
    if (HasOptionalFields())
        image->ZeroPointerField(GetOptionalFields(), offsetof(EEClassOptionalFields, m_pCoClassForIntf));
#ifdef FEATURE_COMINTEROP_UNMANAGED_ACTIVATION 
    if (HasOptionalFields())
        image->ZeroPointerField(GetOptionalFields(), offsetof(EEClassOptionalFields, m_pClassFactory));
#endif
    image->ZeroPointerField(this, offsetof(EEClass, m_pccwTemplate));
#endif // FEATURE_COMINTEROP

    if (HasLayout())
    {
        image->FixupPointerField(this, offsetof(LayoutEEClass, m_LayoutInfo.m_pFieldMarshalers));

        EEClassLayoutInfo *pInfo = &((LayoutEEClass*)this)->m_LayoutInfo;

        FieldMarshaler *pFM = pInfo->m_pFieldMarshalers;
        FieldMarshaler *pFMEnd = (FieldMarshaler*) ((BYTE *)pFM + pInfo->m_numCTMFields*MAXFIELDMARSHALERSIZE);
        while (pFM < pFMEnd)
        {
            pFM->Fixup(image);
            ((BYTE*&)pFM) += MAXFIELDMARSHALERSIZE;
        }
    }
    else if (IsDelegate())
    {
        image->FixupRelativePointerField(this, offsetof(DelegateEEClass, m_pInvokeMethod));
        image->FixupRelativePointerField(this, offsetof(DelegateEEClass, m_pBeginInvokeMethod));
        image->FixupRelativePointerField(this, offsetof(DelegateEEClass, m_pEndInvokeMethod));

        image->ZeroPointerField(this, offsetof(DelegateEEClass, m_pUMThunkMarshInfo));
        image->ZeroPointerField(this, offsetof(DelegateEEClass, m_pStaticCallStub));
        image->ZeroPointerField(this, offsetof(DelegateEEClass, m_pMultiCastInvokeStub));
        image->ZeroPointerField(this, offsetof(DelegateEEClass, m_pSecureDelegateInvokeStub));
        image->ZeroPointerField(this, offsetof(DelegateEEClass, m_pMarshalStub));

#ifdef FEATURE_COMINTEROP
        DelegateEEClass *pDelegateClass = (DelegateEEClass *)this;
        ComPlusCallInfo *pComInfo = pDelegateClass->m_pComPlusCallInfo;

        if (image->IsStored(pComInfo))
        {
            image->FixupPointerField(this, offsetof(DelegateEEClass, m_pComPlusCallInfo));
            pComInfo->Fixup(image);
        }
        else
        {
            image->ZeroPointerField(this, offsetof(DelegateEEClass, m_pComPlusCallInfo));
        }
#endif // FEATURE_COMINTEROP

        image->FixupPointerField(this, offsetof(DelegateEEClass, m_pForwardStubMD));
        image->FixupPointerField(this, offsetof(DelegateEEClass, m_pReverseStubMD));
    }

    //
    // This field must be initialized at
    // load time
    //

    if (IsInterface() && GetGuidInfo() != NULL)
        image->FixupRelativePointerField(this, offsetof(EEClass, m_pGuidInfo));
    else
        image->ZeroPointerField(this, offsetof(EEClass, m_pGuidInfo));

    LOG((LF_ZAP, LL_INFO10000, "EEClass::Fixup %s (%p) complete.\n", GetDebugClassName(), this));
}
#endif // FEATURE_NATIVE_IMAGE_GENERATION


//*******************************************************************************
void EEClass::AddChunk (MethodDescChunk* pNewChunk)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    _ASSERTE(pNewChunk->GetNextChunk() == NULL);
    pNewChunk->SetNextChunk(GetChunks());
    SetChunks(pNewChunk);
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
        if (m_numClasses < (int)NumItems(m_classes))
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

    // The DAC_ENUM_DTHIS above won't have reported the packed fields tacked on the end of this instance (they
    // aren't part of the static class definition because the fields are variably sized and thus have to come
    // right at the end of the structure, even for sub-types such as LayoutEEClass or DelegateEEClass).
    DacEnumMemoryRegion(dac_cast<TADDR>(GetPackedFields()), sizeof(EEClassPackedFields));

    if (HasOptionalFields())
        DacEnumMemoryRegion(dac_cast<TADDR>(GetOptionalFields()), sizeof(EEClassOptionalFields));

    if (flags != CLRDATA_ENUM_MEM_MINI && flags != CLRDATA_ENUM_MEM_TRIAGE)
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

// Get pointer to the packed fields structure attached to this instance.
PTR_EEClassPackedFields EEClass::GetPackedFields()
{
    LIMITED_METHOD_DAC_CONTRACT;

    return dac_cast<PTR_EEClassPackedFields>(PTR_HOST_TO_TADDR(this) + m_cbFixedEEClassFields);
}

// Get the value of the given field. Works regardless of whether the field is currently in its packed or
// unpacked state.
DWORD EEClass::GetPackableField(EEClassFieldId eField)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    return m_fFieldsArePacked ?
        GetPackedFields()->GetPackedField(eField) :
        GetPackedFields()->GetUnpackedField(eField);
}

// Set the value of the given field. The field *must* be in the unpacked state for this to be legal (in
// practice all packable fields must be initialized during class construction and from then on remain
// immutable).
void EEClass::SetPackableField(EEClassFieldId eField, DWORD dwValue)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    _ASSERTE(!m_fFieldsArePacked);
    GetPackedFields()->SetUnpackedField(eField, dwValue);
}
