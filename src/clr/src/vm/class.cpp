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
#include "constrainedexecutionregion.h"
#include "customattribute.h"
#include "encee.h"

#ifdef FEATURE_COMINTEROP 
#include "comcallablewrapper.h"
#include "clrtocomcall.h"
#include "runtimecallablewrapper.h"
#endif // FEATURE_COMINTEROP

#ifdef MDIL
#include "security.h"
#endif

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

#if defined(FEATURE_REMOTING) && !defined(HAS_REMOTING_PRECODE)
    // Destruct the method descs by walking the chunks.
    MethodTable::IntroducedMethodIterator it(pOwningMT);
    for (; it.IsValid(); it.Next())
    {
        MethodDesc * pMD = it.GetMethodDesc();
        pMD->Destruct();
    }
#endif
  
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
DWORD EEClass::GetReliabilityContract()
{
    LIMITED_METHOD_CONTRACT;
    return HasOptionalFields() ? GetOptionalFields()->m_dwReliabilityContract : RC_NULL;
}

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
                m_pGuidInfo = NULL;
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
        image->FixupPointerField(GetOptionalFields(), offsetof(EEClassOptionalFields, m_pVarianceInfo));

    //
    // We pass in the method table, because some classes (e.g. remoting proxy)
    // have fake method tables set up in them & we want to restore the regular
    // one.
    //
    image->FixupField(this, offsetof(EEClass, m_pMethodTable), pMT);

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
        image->FixupPointerField(this, offsetof(DelegateEEClass, m_pInvokeMethod));
        image->FixupPointerField(this, offsetof(DelegateEEClass, m_pBeginInvokeMethod));
        image->FixupPointerField(this, offsetof(DelegateEEClass, m_pEndInvokeMethod));

        image->ZeroPointerField(this, offsetof(DelegateEEClass, m_pUMThunkMarshInfo));
        image->ZeroPointerField(this, offsetof(DelegateEEClass, m_pStaticCallStub));
        image->ZeroPointerField(this, offsetof(DelegateEEClass, m_pMultiCastInvokeStub));
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
        image->FixupPointerField(this, offsetof(EEClass, m_pGuidInfo));
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

#ifndef DACCESS_COMPILE 
#ifdef MDIL
//-------------------------------------------------------------------------------
void EEClass::WriteCompactLayout(ICompactLayoutWriter *pICLW, ZapImage *pZapImage)
{
    STANDARD_VM_CONTRACT;

    EX_TRY
    {
        IfFailThrow(WriteCompactLayoutHelper(pICLW));
    }
    EX_CATCH
    {
        // This catch will prevent type load/assembly load failures that occur during CTL generation to 
        // not bring down the MDIL generation phase.
        SString message;
        GET_EXCEPTION()->GetMessage(message);
        GetSvcLogger()->Printf(LogLevel_Warning, W("%s while generating CTL for typedef 0x%x\n"), message.GetUnicode(), GetMethodTable()->GetCl());
    }
    EX_END_CATCH(RethrowCorruptingExceptions)
}

//-------------------------------------------------------------------------------
HRESULT EEClass::WriteCompactLayoutHelper(ICompactLayoutWriter *pICLW)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;
    MethodTable * pMT = GetMethodTable();
    Module *pModule = pMT->GetModule();
    IMDInternalImport *pMDImport = pModule->GetMDImport();

    // Prepare the CTL writer for writing a type
    pICLW->Reset();

    //
    // Gather high level information about the type: flags, and tokens for
    // the type, it's base, and it's enclosing type (if any).
    //

    DWORD flags = 0;
    mdToken tkType = pMT->GetCl();
    mdToken tkBaseType = mdTokenNil;
    pMDImport->GetTypeDefProps(tkType, &flags, &tkBaseType);

    mdTypeDef tkEnclosingType = mdTokenNil;
    pMDImport->GetNestedClassProps(tkType, &tkEnclosingType);

    //
    // Get the count for the number of interfaces from metadata
    //

    HENUMInternalHolder hEnumInterfaceImpl(pMDImport);
    hEnumInterfaceImpl.EnumInit(mdtInterfaceImpl, tkType);
    DWORD interfaceCount = hEnumInterfaceImpl.EnumGetCount();

    //
    // Get the count of fields introduced by this type.
    //

    DWORD fieldCount = pMT->GetNumIntroducedInstanceFields() + GetNumStaticFields();

    //
    // Count the total number of declared methods for this class
    //

    DWORD declaredMethodCount = 0;
    DWORD unboxingStubCount = 0;
    DWORD declaredVirtualMethodCount = 0;
    {   // If this in any way proves to be a speed issue it could
        // be done more efficiently by just iterating the MethodDescChunks
        // and just adding the counts of each chunk together. For now this
        // is the preferred abstraction to use.
        MethodTable::IntroducedMethodIterator it(GetMethodTable());
        for (; it.IsValid(); it.Next())
        {
            MethodDesc *pMD = it.GetMethodDesc();

            // unboxing stubs need to be handled specially
            // we don't want to report them  - the fact that they are
            // in the method table and have method descs is a CLR
            // implementation detail.
            // however, we need to know their number so we can correct
            // internal counts that include them
            if (pMD->IsUnboxingStub())
                ++unboxingStubCount;
            else
            {
                if (pMD->IsVirtual())
                    declaredVirtualMethodCount++;
                ++declaredMethodCount;
            }
        }
    }

    //
    // Calculate how many virtual methods contribute to overrides and how
    // many contribute to new slots
    //

    DWORD nonVirtualMethodCount = pMT->GetNumMethods() - unboxingStubCount - pMT->GetNumVirtuals();
    DWORD newVirtualMethodCount = pMT->GetNumVirtuals() - pMT->GetNumParentVirtuals();
    if (newVirtualMethodCount > declaredVirtualMethodCount)
    {
        // this should only happen for transparent proxy, which has special rules
        _ASSERTE(pMT->IsTransparentProxy());
        newVirtualMethodCount = declaredVirtualMethodCount;
    }
    DWORD overrideVirtualMethodCount = declaredMethodCount - nonVirtualMethodCount - newVirtualMethodCount;
    if (overrideVirtualMethodCount > declaredVirtualMethodCount)
    {
        // this should only happen for transparent proxy, which has special rules
        _ASSERTE(pMT->IsTransparentProxy());
        overrideVirtualMethodCount = declaredVirtualMethodCount;
    }

    //
    // Generic types are prefixed by their number of type arguments
    if (pMT->HasInstantiation())
    {
        pICLW->GenericType(pMT->GetNumGenericArgs());
        Instantiation inst = GetMethodTable()->GetInstantiation();
        BYTE *varianceInfo = GetVarianceInfo();
        for (DWORD i = 0; i < inst.GetNumArgs(); i++)
        {
            CorGenericParamAttr flags = GetVarianceOfTypeParameter(varianceInfo, i);
            pICLW->GenericParameter(inst[i].AsGenericVariable()->GetToken(), flags);
        }
    }

    _ASSERTE((pMT == GetMethodTable()));
    if (GetMethodTable()->IsComObjectType())
    {
//        printf("Com object type: %08x\n", tkType);
        flags |= ICompactLayoutWriter::CF_COMOBJECTTYPE;
    }

    if (IsEquivalentType())
    {
        flags |= ICompactLayoutWriter::CF_TYPE_EQUIVALENT;
    }

#ifdef FEATURE_COMINTEROP
    if (IsComClassInterface())
    {
//        printf("Com class interface type: %08x\n", tkType);
        flags |= ICompactLayoutWriter::CF_COMCLASSINTERFACE;
    }

    if (IsComEventItfType())
    {
//        printf("Com event interface type: %08x\n", tkType);
        flags |= ICompactLayoutWriter::CF_COMEVENTINTERFACE;
    }
#endif // FEATURE_COMINTEROP

    if (GetMethodTable()->HasFixedAddressVTStatics())
    {
        flags |= ICompactLayoutWriter::CF_FIXED_ADDRESS_VT_STATICS;
    }

#ifdef FEATURE_COMINTEROP
    if (IsInterface())
    {
        switch (GetMethodTable()->GetComInterfaceType())
        {
        case    ifDual:     flags |= ICompactLayoutWriter::CF_DUAL;     break;
        case    ifVtable:   flags |= ICompactLayoutWriter::CF_VTABLE;   break;
        case    ifDispatch: flags |= ICompactLayoutWriter::CF_DISPATCH; break;
        case    ifInspectable: flags |= ICompactLayoutWriter::CF_INSPECTABLE; break;
        default: (!"assert unexpected com interface type");             break;
        }
    }
#endif // FEATURE_COMINTEROP

    if (GetMethodTable()->DependsOnEquivalentOrForwardedStructs())
    {
        flags |= ICompactLayoutWriter::CF_DEPENDS_ON_COM_IMPORT_STRUCTS;
    }

    if (GetMethodTable()->HasFinalizer())
    {
        _ASSERTE(!IsInterface());
        flags |= ICompactLayoutWriter::CF_FINALIZER;
        if (GetMethodTable()->HasCriticalFinalizer())
            flags |= ICompactLayoutWriter::CF_CRITICALFINALIZER;
    }


    // Force computation of transparency bits into EEClass->m_VMFlags
    Security::IsTypeTransparent(GetMethodTable());

    if ((m_VMFlags & VMFLAG_TRANSPARENCY_MASK) == VMFLAG_TRANSPARENCY_UNKNOWN)
        printf("Transparency unknown of type: %08x unknown?????\n", tkType);


    if (m_VMFlags & VMFLAG_CONTAINS_STACK_PTR)
        flags |= ICompactLayoutWriter::CF_CONTAINS_STACK_PTR;

    // If the class is marked as unsafe value class we need to filter out those classes
    // that get marked only "by inheritance" (they contain a field of a type that is marked).
    // In CTL we will mark only the classes that are marked expicitly via a custom attribute.
    // The binder will propagate this state again during field layout - thereby avoiding
    // potentially stale bits.

    // Check that this bit is not already used by somebody else
    _ASSERTE((flags & ICompactLayoutWriter::CF_UNSAFEVALUETYPE) == 0);

    if (IsUnsafeValueClass())
    {
        // If the class is marked as unsafe value class we need to filter out those classes
        // that get the mark only "by inheritance". In CTL we will mark only the classes
        // that are marked expicitly in meta-data.

        //printf("%s ", IsMdPublic(flags) ? "Public" : "Intern");
        //printf("Type 0x%08X is unsafe valuetype", tkType);

        HRESULT hr = pMT->GetMDImport()->GetCustomAttributeByName(tkType,
                                                             g_CompilerServicesUnsafeValueTypeAttribute,
                                                             NULL, NULL);
        IfFailThrow(hr);
        if (hr == S_OK)
        {
            //printf(" (directly marked)", tkType);
            flags |= ICompactLayoutWriter::CF_UNSAFEVALUETYPE;
        }
        //printf("\n");
    }

    //
    // Now have enough information to start serializing the type.
    //

    pICLW->StartType(flags,                          // CorTypeAttr plus perhaps other flags
                     tkType,                         // typedef token for this type
                     tkBaseType,                     // type this type is derived from, if any
                     tkEnclosingType,                // type this type is nested in, if any
                     interfaceCount,                 // how many times ImplementInterface() will be called
                     fieldCount,                     // how many times Field() will be called
                     declaredMethodCount,            // how many times Method() will be called
                     newVirtualMethodCount,          // how many new virtuals this type defines
                     overrideVirtualMethodCount );

    DWORD dwPackSize;
    hr = pMDImport->GetClassPackSize(GetMethodTable()->GetCl(), &dwPackSize);
    if (!FAILED(hr) && dwPackSize != 0)
    {
        _ASSERTE(dwPackSize == 1 || dwPackSize == 2 || dwPackSize == 4 || dwPackSize == 8 || dwPackSize == 16 || dwPackSize == 32 || dwPackSize == 64 || dwPackSize == 128);
        pICLW->PackType(dwPackSize);
    }

    IfFailRet(WriteCompactLayoutTypeFlags(pICLW));
    IfFailRet(WriteCompactLayoutSpecialType(pICLW));

    if (IsInterface() && !HasNoGuid())
    {
        GUID guid;
        GetMethodTable()->GetGuid(&guid, TRUE);
        GuidInfo *guidInfo = GetGuidInfo();
        if (guidInfo != NULL)
            pICLW->GuidInformation(guidInfo);
    }

    IfFailRet(WriteCompactLayoutFields(pICLW));

    IfFailRet(WriteCompactLayoutMethods(pICLW));
    IfFailRet(WriteCompactLayoutMethodImpls(pICLW));

    IfFailRet(WriteCompactLayoutInterfaces(pICLW));
    IfFailRet(WriteCompactLayoutInterfaceImpls(pICLW));


    pICLW->EndType();

    return hr;
}

//-------------------------------------------------------------------------------
HRESULT EEClass::WriteCompactLayoutTypeFlags(ICompactLayoutWriter *pICLW)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;
    
    DWORD flags = m_VMFlags & VMFLAG_TRANSPARENCY_MASK;
    DWORD extendedTypeFlags = 0;
    bool needsExtendedTypeFlagsOutput = false;

    if (flags != VMFLAG_TRANSPARENCY_TRANSPARENT)
    {
        _ASSERTE((VMFLAG_TRANSPARENCY_MASK == 0x1C));
        flags = (flags >> 2);
        extendedTypeFlags |= flags;
        needsExtendedTypeFlagsOutput = true;
    }
    else
    {
        extendedTypeFlags |= EXTENDED_TYPE_FLAG_SF_TRANSPARENT;
    }

#ifdef FEATURE_COMINTEROP
    // Handle EXTENDED_TYPE_FLAG_PLATFORM_NEEDS_PER_TYPE_RCW_DATA
    // This flag should only be set for platform types (In Windows.winmd, and in mscorlib/system.dll)
    bool fBinderHandledNeedsPerTypeRCWDataCase = IsInterface() && GetMethodTable()->GetModule()->GetAssembly()->IsWinMD() && (GetVarianceInfo() != NULL);

    if (!fBinderHandledNeedsPerTypeRCWDataCase && GetMethodTable()->HasRCWPerTypeData())
    {
        // This should only happen for runtime components that ship in box. Assert that this is the case. The flag is not a versionable flag.

        // This checks that the assembly is either part of the tpa list, or a winmd file.
#ifdef FEATURE_CORECLR
        _ASSERTE("MDIL Compiler has determined that a winrt type needs per-type-RCW data, but is not a platform type." && 
            (GetMethodTable()->GetModule()->GetAssembly()->GetManifestFile()->IsProfileAssembly() || 
             GetMethodTable()->GetModule()->GetAssembly()->IsWinMD() ||
             GetWinRTRedirectedTypeIndex() != WinMDAdapter::RedirectedTypeIndex_Invalid));
#endif
#ifdef _DEBUG
        if (GetMethodTable()->GetModule()->GetAssembly()->IsWinMD())
        {
            // If this is a WinMD file, verify the namespace is Windows. something.
            DefineFullyQualifiedNameForClass();
            const char * pszFullyQualifiedName = GetFullyQualifiedNameForClass(this->GetMethodTable());

            if (strncmp(pszFullyQualifiedName, "Windows.", 8) != 0)
            {
                _ASSERTE(!"MDIL Compiler has determined that a winrt type needs per-type-RCW data, but that the binder will not generate it, and the flag to generate it is not part of versionable MDIL.");
            }
        }
#endif
        extendedTypeFlags |= EXTENDED_TYPE_FLAG_PLATFORM_NEEDS_PER_TYPE_RCW_DATA;
        needsExtendedTypeFlagsOutput = true;
    }
#endif // FEATURE_COMINTEROP

    if (needsExtendedTypeFlagsOutput)
        pICLW->ExtendedTypeFlags(extendedTypeFlags);

    return hr;
}

#ifdef FEATURE_COMINTEROP
struct RedirectedTypeToSpecialTypeConversion
{
    SPECIAL_TYPE type;
};

#define DEFINE_PROJECTED_TYPE(szWinRTNS, szWinRTName, szClrNS, szClrName, nClrAsmIdx, nContractAsmIdx, nWinRTIndex, nClrIndex, nWinMDTypeKind) \
{ SPECIAL_TYPE_ ## nClrIndex },

static const RedirectedTypeToSpecialTypeConversion g_redirectedSpecialTypeInfo[] =
{
#include "winrtprojectedtypes.h"
};
#undef DEFINE_PROJECTED_TYPE
#endif

//-------------------------------------------------------------------------------
HRESULT EEClass::WriteCompactLayoutSpecialType(ICompactLayoutWriter *pICLW)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;
    SPECIAL_TYPE type = SPECIAL_TYPE_INVALID;
#ifdef FEATURE_COMINTEROP
    // All types with winrt redirection indices are special types
    WinMDAdapter::RedirectedTypeIndex typeIndex = GetWinRTRedirectedTypeIndex();
    if (typeIndex != WinMDAdapter::RedirectedTypeIndex_Invalid)
    {
        type = g_redirectedSpecialTypeInfo[typeIndex].type;
    }

    // Additionally System.Collections.ICollection and System.Collections.Generics.ICollection<T> are special types
    if (this->GetMethodTable()->GetModule()->IsSystem())
    {
        DefineFullyQualifiedNameForClass();
        const char * pszFullyQualifiedName = GetFullyQualifiedNameForClass(this->GetMethodTable());

        if (strcmp(pszFullyQualifiedName, g_CollectionsGenericCollectionItfName) == 0)
        {
            type = SPECIAL_TYPE_System_Collections_Generic_ICollection;
        }
        else if (::strcmp(pszFullyQualifiedName, g_CollectionsCollectionItfName) == 0)
        {
            type = SPECIAL_TYPE_System_Collections_ICollection;
        }
    }
#endif

    if (type != SPECIAL_TYPE_INVALID)
    {
        pICLW->SpecialType(type);
    }

    return hr;
}

//-------------------------------------------------------------------------------
HRESULT EEClass::WriteCompactLayoutInterfaces(ICompactLayoutWriter *pICLW)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;

    MethodTable *pMT = GetMethodTable();
    IMDInternalImport *pMDImport = pMT->GetModule()->GetMDImport();
    HENUMInternalHolder hEnumInterfaceImpl(pMDImport);
    hEnumInterfaceImpl.EnumInit(mdtInterfaceImpl, pMT->GetCl());
    DWORD interfaceCount = hEnumInterfaceImpl.EnumGetCount();

    for (DWORD i = 0; i < interfaceCount; ++i)
    {
        mdInterfaceImpl ii;

        if (!hEnumInterfaceImpl.EnumNext(&ii))
        {   // Less interfaces than count reports is an error
            return E_FAIL;
        }

        mdToken tkInterface;
        IfFailThrow(pMDImport->GetTypeOfInterfaceImpl(ii, &tkInterface));

        pICLW->ImplementInterface(tkInterface);
    }

    return hr;
}

//-------------------------------------------------------------------------------
HRESULT EEClass::WriteCompactLayoutInterfaceImpls(ICompactLayoutWriter *pICLW)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;

    MethodTable *pMT = GetMethodTable();

    if (pMT->HasDispatchMap())
    {
        DispatchMap::Iterator it(pMT);
        for (; it.IsValid(); it.Next())
        {
            DispatchMapEntry *pEntry = it.Entry();
            CONSISTENCY_CHECK(pEntry->GetTypeID().IsImplementedInterface());

            CONSISTENCY_CHECK(pEntry->GetTypeID().GetInterfaceNum() < pMT->GetNumInterfaces());
            MethodTable * pMTItf =
                pMT->GetInterfaceMap()[pEntry->GetTypeID().GetInterfaceNum()].GetMethodTable();

            //
            // Determine the interface method token
            //

            MethodDesc *pMDItf = pMTItf->GetMethodDescForSlot(pEntry->GetSlotNumber());
            mdToken tkItf = pICLW->GetTokenForMethodDesc(pMDItf, pMTItf);

            //
            // Determine the implementation method token
            //

//            CONSISTENCY_CHECK(!pEntry->IsVirtuallyMapped());
            MethodDesc *pMDImpl = pMT->GetMethodDescForSlot(pEntry->GetTargetSlotNumber());
            mdToken tkImpl = pICLW->GetTokenForMethodDesc(pMDImpl);

            //
            // Serialize
            //

            pICLW->ImplementInterfaceMethod(tkItf, tkImpl);
        }
    }

    return hr;
}

//-------------------------------------------------------------------------------
struct SortField
{
    int origIndex;
    ULONG offset;
};

//-------------------------------------------------------------------------------
int _cdecl FieldCmpOffsets(const void *a, const void *b)
{
    LIMITED_METHOD_CONTRACT;

    const SortField *fa = (const SortField *)a;
    const SortField *fb = (const SortField *)b;
    if (fa->offset < fb->offset)
        return -1;
    if (fa->offset > fb->offset)
        return 1;
    return 0;
}

#ifdef SORT_BY_RID
//-------------------------------------------------------------------------------
struct SortFieldRid
{
    int origIndex;
    ULONG rid;
};

//-------------------------------------------------------------------------------
int _cdecl FieldCmpRids(const void *a, const void *b)
{
    LIMITED_METHOD_CONTRACT;

    const SortFieldRid *fa = (const SortFieldRid *)a;
    const SortFieldRid *fb = (const SortFieldRid *)b;
    if (fa->rid < fb->rid)
        return -1;
    if (fa->rid > fb->rid)
        return 1;
    return 0;
}

#endif //SORT_BY_RID
//-------------------------------------------------------------------------------
inline PTR_FieldDesc EEClass::GetFieldDescByIndex(DWORD fieldIndex)
{
    STANDARD_VM_CONTRACT;

    WRAPPER_NO_CONTRACT;
    MethodTable * pMT = GetMethodTable();
    CONSISTENCY_CHECK(fieldIndex < (DWORD)(pMT->GetNumIntroducedInstanceFields()) + (DWORD)GetNumStaticFields());

    // MDIL_NEEDS_REVIEW
    // was previously: return GetApproxFieldDescListPtr() + fieldIndex;

    return pMT->GetApproxFieldDescListRaw() + fieldIndex;
}

HRESULT EEClass::WriteCompactLayoutFields(ICompactLayoutWriter *pICLW)
{
    STANDARD_VM_CONTRACT;

    HRESULT hr = S_OK;

    DWORD dwFieldCount = GetMethodTable()->GetNumIntroducedInstanceFields() + GetNumStaticFields();


#ifdef SORT_BY_RID
    typedef CQuickArray<SortFieldRid> SortFieldArray;
    SortFieldArray fields;
    fields.AllocThrows(dwFieldCount);
    for (DWORD i = 0; i < dwFieldCount; i++)
    {
        FieldDesc *fieldDesc = GetFieldDescByIndex(i);
        fields[i].origIndex = i;
        fields[i].rid = fieldDesc->GetMemberDef();
    }

    qsort(fields.Ptr(), dwFieldCount, sizeof(SortFieldRid), FieldCmpRids);
#else
    //
    // Build an index for the fields sorted by offset so that they are serialized
    // in the same order as they should be deserialized.
    //

    typedef CQuickArray<SortField> SortFieldArray;
    SortFieldArray fields;
    fields.AllocThrows(dwFieldCount);
    for (DWORD i = 0; i < dwFieldCount; i++)
    {
        FieldDesc *fieldDesc = GetFieldDescByIndex(i);
        fields[i].origIndex = i;
        fields[i].offset = fieldDesc->GetOffset();
    }

    qsort(fields.Ptr(), dwFieldCount, sizeof(SortField), FieldCmpOffsets);
#endif

    //
    // For each field, gather information and then serialize
    //
#ifdef DEBUG_LAYOUT
    printf("%s %08x (baseSize = %x  instance field bytes = %x  number of virtuals = %x):\n", GetMethodTable()->IsValueType() ? "Struct" : "Class", GetMethodTable()->GetCl(), GetMethodTable()->GetBaseSize(), GetMethodTable()->GetNumInstanceFieldBytes(), GetMethodTable()->GetNumVirtuals());
#endif

    for (DWORD i = 0; i < dwFieldCount; i++)
    {
        FieldDesc *pFD = GetFieldDescByIndex(fields[i].origIndex);

        mdFieldDef tkField = pFD->GetMemberDef();

        //
        // Determine storage type of the field
        //

        ICompactLayoutWriter::FieldStorage fieldStorage = ICompactLayoutWriter::FS_INSTANCE;
        if (pFD->IsStatic())
        {
            if (pFD->IsThreadStatic())
            {
                fieldStorage = ICompactLayoutWriter::FS_THREADLOCAL;
            }
            else if (pFD->IsContextStatic())
            {
                fieldStorage = ICompactLayoutWriter::FS_CONTEXTLOCAL;
            }
            else if (pFD->IsRVA())
            {
                fieldStorage = ICompactLayoutWriter::FS_RVA;
            }
            else
            {
                fieldStorage = ICompactLayoutWriter::FS_STATIC;
            }
        }

        //
        // Determine protection of the field
        //

        ICompactLayoutWriter::FieldProtection fieldProtection;
        switch (pFD->GetFieldProtection())
        {
        case fdPrivateScope:
            fieldProtection = ICompactLayoutWriter::FP_PRIVATE_SCOPE;
            break;
        case fdPrivate:
            fieldProtection = ICompactLayoutWriter::FP_PRIVATE;
            break;
        case fdFamANDAssem:
            fieldProtection = ICompactLayoutWriter::FP_FAM_AND_ASSEM;
            break;
        case fdAssembly:
            fieldProtection = ICompactLayoutWriter::FP_ASSEMBLY;
            break;
        case fdFamily:
            fieldProtection = ICompactLayoutWriter::FP_FAMILY;
            break;
        case fdFamORAssem:
            fieldProtection = ICompactLayoutWriter::FP_FAM_OR_ASSEM;
            break;
        case fdPublic:
            fieldProtection = ICompactLayoutWriter::FP_PUBLIC;
            break;
        default:
            UNREACHABLE();
        }

        //
        // If the field is a ValueType, retrieve the token for it.
        //
        // NOTE: can't just grab the TypeHandle for the field and return
        //       that token because the type could reside in another
        //       metadata scope.
        //

        mdToken tkValueType = mdTokenNil;
        CorElementType fieldType = pFD->GetFieldType();
        PCCOR_SIGNATURE pSig;
        DWORD           cbSig;
        pFD->GetSig(&pSig, &cbSig);

        SigPointer sigPointer(pSig, cbSig);
        sigPointer.GetCallingConv(NULL);
        CorElementType elType;
        sigPointer.GetElemType(&elType);
        switch (elType)
        {
        case    ELEMENT_TYPE_BOOLEAN:
        case    ELEMENT_TYPE_CHAR:
        case    ELEMENT_TYPE_I1:
        case    ELEMENT_TYPE_U1:
        case    ELEMENT_TYPE_I2:
        case    ELEMENT_TYPE_U2:
        case    ELEMENT_TYPE_I4:
        case    ELEMENT_TYPE_U4:
        case    ELEMENT_TYPE_I8:
        case    ELEMENT_TYPE_U8:
        case    ELEMENT_TYPE_R4:
        case    ELEMENT_TYPE_R8:
        case    ELEMENT_TYPE_PTR:
        case    ELEMENT_TYPE_BYREF:
        case    ELEMENT_TYPE_I:
        case    ELEMENT_TYPE_U:
        case    ELEMENT_TYPE_FNPTR:
            _ASSERTE(fieldType == elType);
            break;

        case    ELEMENT_TYPE_VALUETYPE:
            sigPointer.GetToken(&tkValueType);
            if (TypeFromToken(tkValueType) != mdtTypeDef)
                fieldType = ELEMENT_TYPE_VALUETYPE;
            break;

        case    ELEMENT_TYPE_VAR:
            fieldType = ELEMENT_TYPE_VALUETYPE;
            // fall thru
        case    ELEMENT_TYPE_GENERICINST:
            if (fieldType != ELEMENT_TYPE_VALUETYPE)
            {
                // Force valuetypes not defined in this module from tokens instead of taking advantage of the knowledge
                // that this is an enum type.
                CorElementType elemTypeGeneric;
                IfFailThrow(sigPointer.GetElemType(&elemTypeGeneric));
                if (elemTypeGeneric == ELEMENT_TYPE_VALUETYPE)
                {
                    mdToken tkValueTypeSig;
                    IfFailThrow(sigPointer.GetToken(&tkValueTypeSig));
                    if (TypeFromToken(tkValueTypeSig) != mdtTypeDef)
                    {
                        fieldType = ELEMENT_TYPE_VALUETYPE;
                    }
                }
            }
            tkValueType = pICLW->GetTypeSpecToken(pSig+1, cbSig-1);
            break;

        case    ELEMENT_TYPE_STRING:
        case    ELEMENT_TYPE_CLASS:
        case    ELEMENT_TYPE_OBJECT:
        case    ELEMENT_TYPE_SZARRAY:
        case    ELEMENT_TYPE_ARRAY:
            _ASSERTE(fieldType == ELEMENT_TYPE_CLASS);
            break;

        case    ELEMENT_TYPE_MVAR:
            printf("elType = %d\n", elType);
            _ASSERTE(!"unexpected field type");
            break;

        default:
            printf("elType = %d\n", elType);
            break;
        }

        //
        // Record this field
        //

        pICLW->Field(tkField,
                     fieldStorage,
                     fieldProtection,
                     fieldType,
                     (HasExplicitFieldOffsetLayout() || pFD->IsRVA()) ? pFD->GetOffset() : ~0,
                     tkValueType);
    }

#ifdef DEBUG_LAYOUT
    // dump field offsets in token order
    mdFieldDef lowestFieldToken = ~0;
    for (DWORD i = 0; i < dwFieldCount; i++)
    {
        FieldDesc *pFD = GetFieldDescByIndex(i);
        mdFieldDef tkField = pFD->GetMemberDef();
        if (lowestFieldToken >= tkField)
            lowestFieldToken = tkField;
    }
    // print fields in token order - this is quadratic in the number of fields,
    // but it's just debug output after all

    for (DWORD i = 0; i < dwFieldCount; i++)
    {
        mdFieldDef tkField = lowestFieldToken + i;
        bool fFound = false;
        for (DWORD i = 0; i < dwFieldCount; i++)
        {
            FieldDesc *pFD = GetFieldDescByIndex(i);
            if (tkField == pFD->GetMemberDef())
            {
                printf("  Field %08x of type %x has offset %x\n", tkField, pFD->GetFieldType(), pFD->GetOffset());
                fFound = true;
            }
        }
        if (!fFound)
        {
            printf("  >>>> Gap for field token %08x\n", tkField);
        }
    }
#endif

    if (HasLayout())
    {
        // see if we have a field marshaler for this field
        EEClassLayoutInfo *eeClassLayoutInfo = GetLayoutInfo();
        
        FieldMarshaler *pFM = eeClassLayoutInfo->m_pFieldMarshalers;
        FieldMarshaler *pFMEnd = (FieldMarshaler*) ((BYTE *)pFM + eeClassLayoutInfo->m_numCTMFields*MAXFIELDMARSHALERSIZE);
        while (pFM < pFMEnd)
        {
//          printf("Field %08x  native type = %x  external offset = %x\n", tkField, pFM->GetNStructFieldType(), pFM->GetExternalOffset());

            NStructFieldType type = pFM->GetNStructFieldType();
            DWORD count = 0;
            DWORD flags = 0;
            DWORD typeToken1 = 0;
            DWORD typeToken2 = 0;

#define NFT_CASE_VERIFICATION_TYPE_NAME(type) nftMissingFromEEClass_WriteCompactLayoutFields_ ## type

            switch (type)
            {
            NFT_CASE(NFT_NONE)
            NFT_CASE(NFT_STRINGUNI)
            NFT_CASE(NFT_COPY1)
            NFT_CASE(NFT_COPY2)
            NFT_CASE(NFT_COPY4)
            NFT_CASE(NFT_COPY8)
            NFT_CASE(NFT_CBOOL)
            NFT_CASE(NFT_DATE)
            NFT_CASE(NFT_DECIMAL)
            NFT_CASE(NFT_WINBOOL)
            NFT_CASE(NFT_SAFEHANDLE)
            NFT_CASE(NFT_CRITICALHANDLE)
#ifdef FEATURE_COMINTEROP
            NFT_CASE(NFT_BSTR)
            NFT_CASE(NFT_VARIANT)
            NFT_CASE(NFT_VARIANTBOOL)
            NFT_CASE(NFT_CURRENCY)
            NFT_CASE(NFT_DATETIMEOFFSET)
            NFT_CASE(NFT_HSTRING)
            NFT_CASE(NFT_WINDOWSFOUNDATIONHRESULT)
            NFT_CASE(NFT_SYSTEMTYPE)
#endif // FEATURE_COMINTEROP
                // no additional info for these
                break;

            NFT_CASE(NFT_STRINGANSI)
                {
                    FieldMarshaler_StringAnsi *pFM_StringAnsi = (FieldMarshaler_StringAnsi*)pFM;
                    if (pFM_StringAnsi->GetBestFit())
                        flags |= ICompactLayoutWriter::NF_BESTFITMAP;
                    if (pFM_StringAnsi->GetThrowOnUnmappableChar())
                        flags |= ICompactLayoutWriter::NF_THROWONUNMAPPABLECHAR;
                }
                break;

            NFT_CASE(NFT_FIXEDSTRINGUNI)
                {
                    count = pFM->NativeSize()/sizeof(WCHAR);
                }
                break;

            NFT_CASE(NFT_FIXEDSTRINGANSI)
                {
                    FieldMarshaler_FixedStringAnsi *pFM_FixedStringAnsi = (FieldMarshaler_FixedStringAnsi*)pFM;
                    if (pFM_FixedStringAnsi->GetBestFit())
                        flags |= ICompactLayoutWriter::NF_BESTFITMAP;
                    if (pFM_FixedStringAnsi->GetThrowOnUnmappableChar())
                        flags |= ICompactLayoutWriter::NF_THROWONUNMAPPABLECHAR;
                    count = pFM->NativeSize()/sizeof(CHAR);
                }
                break;

            NFT_CASE(NFT_FIXEDCHARARRAYANSI)
                {
                    FieldMarshaler_FixedCharArrayAnsi *pFM_FixedCharArrayAnsi = (FieldMarshaler_FixedCharArrayAnsi*)pFM;
                    if (pFM_FixedCharArrayAnsi->GetBestFit())
                        flags |= ICompactLayoutWriter::NF_BESTFITMAP;
                    if (pFM_FixedCharArrayAnsi->GetThrowOnUnmappableChar())
                        flags |= ICompactLayoutWriter::NF_THROWONUNMAPPABLECHAR;
                    count = pFM->NativeSize()/sizeof(CHAR);
                }
                break;

            NFT_CASE(NFT_FIXEDARRAY)
                {
                    FieldMarshaler_FixedArray *pFM_FixedArray = (FieldMarshaler_FixedArray*)pFM;
                    MethodTable *pMT = pFM_FixedArray->GetElementTypeHandle().AsMethodTable();
                    typeToken1 = pICLW->GetTokenForType(pMT);

                    /* do we need this information? there are no accessors...
                    if (pFM_FixedArray->GetBestFit())
                        flags |= ICompactLayoutWriter::NF_BESTFITMAP;
                    if (pFM_FixedArray->GetThrowOnUnmappableChar())
                        flags |= ICompactLayoutWriter::NF_THROWONUNMAPPABLECHAR;
                    */
                    flags |= pFM_FixedArray->GetElementVT() << ICompactLayoutWriter::NF_VARTYPE_SHIFT;
                    count = pFM->NativeSize()/OleVariant::GetElementSizeForVarType(pFM_FixedArray->GetElementVT(), pMT);
                }
                break;

            NFT_CASE(NFT_DELEGATE)
                {
                    MethodTable *pMT = ((FieldMarshaler_Delegate*)pFM)->GetMethodTable();
                    typeToken1 = pICLW->GetTokenForType(pMT);
                }
                break;

            NFT_CASE(NFT_ANSICHAR)
                {
                    FieldMarshaler_Ansi *pFM_Ansi = (FieldMarshaler_Ansi*)pFM;
                    if (pFM_Ansi->GetBestFit())
                        flags |= ICompactLayoutWriter::NF_BESTFITMAP;
                    if (pFM_Ansi->GetThrowOnUnmappableChar())
                        flags |= ICompactLayoutWriter::NF_THROWONUNMAPPABLECHAR;
                }
                break;

            NFT_CASE(NFT_NESTEDLAYOUTCLASS)
                {
                    MethodTable *pMT = ((FieldMarshaler_NestedLayoutClass*)pFM)->GetMethodTable();
                    typeToken1 = pICLW->GetTokenForType(pMT);
                }
                break;

            NFT_CASE(NFT_NESTEDVALUECLASS)
                {
                    MethodTable *pMT = ((FieldMarshaler_NestedValueClass*)pFM)->GetMethodTable();
                    typeToken1 = pICLW->GetTokenForType(pMT);
                }
                break;

#ifdef FEATURE_COMINTEROP
            NFT_CASE(NFT_INTERFACE)
                {
                    FieldMarshaler_Interface *pFM_Interface = (FieldMarshaler_Interface*)pFM;
                    MethodTable *pMT = pFM_Interface->GetMethodTable();
                    typeToken1 = pICLW->GetTokenForType(pMT);
                    MethodTable *ppItfMT = NULL;
                    pFM_Interface->GetInterfaceInfo(&ppItfMT, &flags);
                    typeToken2 = pICLW->GetTokenForType(ppItfMT);
                }
                break;

                NFT_CASE(NFT_WINDOWSFOUNDATIONIREFERENCE)
                {
                    FieldMarshaler_Nullable *pFM_Nullable = (FieldMarshaler_Nullable*)pFM;
                    MethodTable *pMT = pFM_Nullable->GetMethodTable();
                    typeToken1 = pICLW->GetTokenForType(pMT);
                }
                break;

#ifdef FEATURE_CLASSIC_COMINTEROP
            NFT_CASE(NFT_SAFEARRAY)
                {
                    FieldMarshaler_SafeArray *pFM_SafeArray = (FieldMarshaler_SafeArray*)pFM;
                    MethodTable *pMT = pFM_SafeArray->GetElementTypeHandle().AsMethodTable();
                    typeToken1 = pICLW->GetTokenForType(pMT);
                    flags = pFM_SafeArray->GetElementVT() << ICompactLayoutWriter::NF_VARTYPE_SHIFT;
                }
                break;
#endif //FEATURE_CLASSIC_COMINTEROP

#endif // FEATURE_COMINTEROP
            NFT_CASE(NFT_ILLEGAL)
                // do we need this one even? do we need additional info?
                break;
#ifndef FEATURE_COMINTEROP
            NFT_CASE(NFT_INTERFACE)
#endif
            default:
#define NFT_VERIFY_ALL_CASES
#include "nsenumhandleallcases.h"
                _ASSERTE(!"unexpected native type");
                break;

            }

            pICLW->NativeField(pFM->GetFieldDesc()->GetMemberDef(),
                               type,
                               pFM->GetExternalOffset(),
                               count,
                               flags,
                               typeToken1,
                               typeToken2);

            ((BYTE*&)pFM) += MAXFIELDMARSHALERSIZE;
        }
    }

    if (HasExplicitFieldOffsetLayout() || HasLayout() && GetLayoutInfo()->HasExplicitSize())
    {
        pICLW->SizeType(GetMethodTable()->GetNumInstanceFieldBytes());
    }

    return hr;
}

//-------------------------------------------------------------------------------
HRESULT EEClass::WriteCompactLayoutMethods(ICompactLayoutWriter *pICLW)
{
    // we need this iterator because we want the method descs in declaration order,
    // but the chunks are in reverse order
    class ReversedChunkMethoditerator
    {
    private:
        MethodDesc *m_methodDesc;
    public:
        ReversedChunkMethoditerator(MethodTable *pMT)
        {
            m_methodDesc = NULL;
            MethodDescChunk *pChunk = pMT->GetClass()->GetChunks();
            if (pChunk == NULL)
                return;
            while (pChunk->GetNextChunk() != NULL)
                pChunk = pChunk->GetNextChunk();
            m_methodDesc = pChunk->GetFirstMethodDesc();
        }

        bool IsValid()
        {
            return m_methodDesc != NULL;
        }

        MethodDesc *GetMethodDesc()
        {
            return m_methodDesc;
        }

        void Next()
        {
            MethodDescChunk * pChunk = m_methodDesc->GetMethodDescChunk();

            // Check whether the next MethodDesc is still within the bounds of the current chunk
            TADDR pNext = dac_cast<TADDR>(m_methodDesc) + m_methodDesc->SizeOf();
            TADDR pEnd = dac_cast<TADDR>(pChunk) + pChunk->SizeOf();

            if (pNext < pEnd)
            {
                // Just skip to the next method in the same chunk
                m_methodDesc = PTR_MethodDesc(pNext);
            }
            else
            {
                _ASSERTE(pNext == pEnd);

                // We have walked all the methods in the current chunk. Move on
                // to the previous chunk.
                MethodDescChunk *pPrevChunk = m_methodDesc->GetClass()->GetChunks();
                if (pPrevChunk == pChunk)
                    m_methodDesc = NULL;
                else
                {
                    while (pPrevChunk->GetNextChunk() != pChunk)
                        pPrevChunk = pPrevChunk->GetNextChunk();
                    m_methodDesc = pPrevChunk->GetFirstMethodDesc();
                }
            }
        }
    };

    HRESULT hr = S_OK;

//    printf("New virtuals of class %08x\n", GetCl());

    MethodTable *pMT = GetMethodTable();
    DWORD dwNumParentVirtuals = pMT->GetNumParentVirtuals();
    IMDInternalImport *pMDImport = pMT->GetModule()->GetMDImport();

    ReversedChunkMethoditerator it(GetMethodTable());
    WORD lastNewSlotIndex = 0;
    mdMethodDef tkUnboxingStubNeedsImpl = 0;
    for (; it.IsValid(); it.Next())
    {
        MethodDesc *pMD = it.GetMethodDesc();

        // skip unboxing stubs
        if (pMD->IsUnboxingStub())
        {
            if (pMD->IsMethodImpl())
                tkUnboxingStubNeedsImpl = pMD->GetMemberDef();
            continue;
        }

        mdMethodDef tkMethod = pMD->GetMemberDef();

        //
        // Gather method information
        //

        DWORD dwDeclFlags = pMD->GetAttrs();
        ULONG ulCodeRVA;
        DWORD dwImplFlags;
        pMDImport->GetMethodImplProps(tkMethod, &ulCodeRVA, &dwImplFlags);

        //
        // Figure out if this method overrides a parent method, and
        // if so find or generate the corresponding token.
        //

        mdToken tkOverrideMethod = mdTokenNil;
        WORD slotIndex = pMD->GetSlot();
        if (pMT->IsValueType() && pMD->IsVirtual())
        {
            MethodDesc *pBoxedMD = MethodDesc::FindOrCreateAssociatedMethodDesc(pMD,
                                                                                pMD->GetMethodTable(),
                                                                                TRUE /* get unboxing entry point */,
                                                                                pMD->GetMethodInstantiation(),
                                                                                FALSE /* no allowInstParam */ );
            if (pBoxedMD != NULL)
                slotIndex = pBoxedMD->GetSlot();
        }

#ifdef DEBUG_LAYOUT
        if (pMD->IsVirtual())
            printf("  virtual method %08x has slot %x\n", tkMethod, slotIndex);
#endif
        if (slotIndex < dwNumParentVirtuals)
        {
            MethodTable *pParentMT = pMT->GetParentMethodTable();
            MethodDesc *pParentMD = pParentMT->GetMethodDescForSlot(slotIndex)->GetDeclMethodDesc(slotIndex);
            tkOverrideMethod = pICLW->GetTokenForMethodDesc(pParentMD);
        }

        //
        // Figure out the implHints - they consist of the classification
        // and various flags for special methods
        //

        DWORD dwImplHints = pMD->GetClassification();
        if (pMT->HasDefaultConstructor() && slotIndex == pMT->GetDefaultConstructorSlot())
            dwImplHints |= ICompactLayoutWriter::IH_DEFAULT_CTOR;
        else if (pMT->HasClassConstructor() && slotIndex == pMT->GetClassConstructorSlot())
            dwImplHints |= ICompactLayoutWriter::IH_CCTOR;

        if (pMD->IsCtor())
            dwImplHints |= ICompactLayoutWriter::IH_CTOR;

        if (IsDelegate())
        {
            DelegateEEClass *delegateEEClass = (DelegateEEClass *)this;
            if (pMD == delegateEEClass->m_pInvokeMethod)
                dwImplHints |= ICompactLayoutWriter::IH_DELEGATE_INVOKE;
            else if (pMD == delegateEEClass->m_pBeginInvokeMethod)
                dwImplHints |= ICompactLayoutWriter::IH_DELEGATE_BEGIN_INVOKE;
            else if (pMD == delegateEEClass->m_pEndInvokeMethod)
                dwImplHints |= ICompactLayoutWriter::IH_DELEGATE_END_INVOKE;
        }


        _ASSERTE((tkUnboxingStubNeedsImpl == 0) || (tkUnboxingStubNeedsImpl == tkMethod) 
                 || !"This depends on unboxing stubs always being processed directly before the method they invoke");
        // cannot use pMD->HasMethodImplSlot() because it has some false positives
        // (virtual methods on valuetypes implementing interfaces have the flag bit set
        //  but an empty list of replaced methods)
        if (pMD->IsMethodImpl() || (tkUnboxingStubNeedsImpl == tkMethod))
        {
            dwImplHints |= ICompactLayoutWriter::IH_HASMETHODIMPL;
        }

        tkUnboxingStubNeedsImpl = 0;

        // Make sure that the transparency is cached in the NGen image
        //   (code copied from MethodDesc::Save (ngen code path))
        // A side effect of this call is caching the transparency bits
        // in the MethodDesc

        Security::IsMethodTransparent(pMD);

        if (pMD->HasCriticalTransparentInfo())
        {
            if (pMD->IsTreatAsSafe())
            {
                dwImplHints |= ICompactLayoutWriter::IH_TRANSPARENCY_TREAT_AS_SAFE;
//                printf("  method %08x is treat as safe\n", tkMethod);
            }
            else if (pMD->IsTransparent())
            {
                dwImplHints |= ICompactLayoutWriter::IH_TRANSPARENCY_TRANSPARENT;
//                printf("  method %08x is transparent\n", tkMethod);
            }
            else if (pMD->IsCritical())
            {
                dwImplHints |= ICompactLayoutWriter::IH_TRANSPARENCY_CRITICAL;
//                printf("  method %08x is critical\n", tkMethod);
            }
            else
               _ASSERTE(!"one of the above must be true, no?");
        }
        else
        {
            _ASSERTE((dwImplHints & ICompactLayoutWriter::IH_TRANSPARENCY_MASK) == ICompactLayoutWriter::IH_TRANSPARENCY_NO_INFO);
//            printf("  method %08x has no critical transparent info\n", tkMethod);
        }
        
        if (!pMD->IsAbstract() && !pMD->IsILStub() && !pMD->IsUnboxingStub() 
            && pMD->IsIL())
        {
            EX_TRY 
            {
                if (pMD->IsVerifiable()) 
                {
                    dwImplHints |= ICompactLayoutWriter::IH_IS_VERIFIABLE;
                }
            }
            EX_CATCH
            {
                // If the method has a security exception, it will fly through IsVerifiable. 
                // We only expect to see internal CLR exceptions here, so use RethrowCorruptingExceptions
            }
            EX_END_CATCH(RethrowCorruptingExceptions)
        }

        if (pMD->IsVerified())
        {
            dwImplHints |= ICompactLayoutWriter::IH_IS_VERIFIED;
        }

        //
        // Serialize the method
        //

        if (IsMdPinvokeImpl(dwDeclFlags))
        {
            _ASSERTE(tkOverrideMethod == mdTokenNil);
            NDirectMethodDesc *pNMD = (NDirectMethodDesc *)pMD;
            PInvokeStaticSigInfo pInvokeStaticSigInfo;
            NDirect::PopulateNDirectMethodDesc(pNMD, &pInvokeStaticSigInfo);

            pICLW->PInvokeMethod(dwDeclFlags,
                          dwImplFlags,
                          dwImplHints,
                          tkMethod,
                          pNMD->GetLibName(),
                          pNMD->GetEntrypointName(),
                          pNMD->ndirect.m_wFlags);
        }
        else
        {
            if (pMD->IsVirtual() && !tkOverrideMethod)
            {
//                printf("  Method %08x has new virtual slot %u\n", tkMethod, slotIndex);
                // make sure new virtual slot indices are ascending - otherwise, the virtual slot indices
                // created by the binder won't be consistent with ngen, which makes mix and match impossible
                _ASSERTE(lastNewSlotIndex <= slotIndex);
                lastNewSlotIndex = slotIndex;
            }

            pICLW->Method(dwDeclFlags,
                          dwImplFlags,
                          dwImplHints,
                          tkMethod,
                          tkOverrideMethod);

            if (pMD->IsGenericMethodDefinition())
            {
                InstantiatedMethodDesc *pIMD = pMD->AsInstantiatedMethodDesc();
                Instantiation inst = pIMD->GetMethodInstantiation();                
                for (DWORD i = 0; i < inst.GetNumArgs(); i++)
                {
                    pICLW->GenericParameter(inst[i].AsGenericVariable()->GetToken(), 0);
                }
            }
        }
    }

    return hr;
}

//-------------------------------------------------------------------------------
HRESULT EEClass::WriteCompactLayoutMethodImpls(ICompactLayoutWriter *pICLW)
{
    HRESULT hr = S_OK;

    MethodTable::IntroducedMethodIterator it(GetMethodTable());
    for (; it.IsValid(); it.Next())
    {
        MethodDesc *pMDImpl = it.GetMethodDesc();
        if (pMDImpl->IsMethodImpl())
        {   // If this is a methodImpl, then iterate all implemented slots
            // and serialize the (decl,impl) pair.
            // This guarantees that all methodImpls for a particular method
            // are "clustered" (if there is more than one)
            MethodImpl::Iterator implIt(pMDImpl);
            for (; implIt.IsValid(); implIt.Next())
            {
                MethodDesc *pMDDecl = implIt.GetMethodDesc();
                // MethodImpls should no longer cover interface methodImpls, as that
                // should be captured in the interface dispatch map.
                CONSISTENCY_CHECK(!pMDDecl->IsInterface());

                mdToken tkDecl = pICLW->GetTokenForMethodDesc(pMDDecl);
                pICLW->MethodImpl(tkDecl, pMDImpl->GetMemberDef());
            }
        }
    }

    return hr;
}
#endif //MDIL
#endif
