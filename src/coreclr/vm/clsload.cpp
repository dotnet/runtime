// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: clsload.cpp
//
// ============================================================================

#include "common.h"
#include "winwrap.h"
#include "ceeload.h"
#include "siginfo.hpp"
#include "vars.hpp"
#include "clsload.hpp"
#include "classhash.inl"
#include "class.h"
#include "method.hpp"
#include "ecall.h"
#include "stublink.h"
#include "object.h"
#include "excep.h"
#include "threads.h"
#include "comsynchronizable.h"
#include "threads.h"
#include "dllimport.h"
#include "dbginterface.h"
#include "log.h"
#include "eeconfig.h"
#include "fieldmarshaler.h"
#include "jitinterface.h"
#include "vars.hpp"
#include "assembly.hpp"
#include "eeprofinterfaces.h"
#include "eehash.h"
#include "typehash.h"
#include "comdelegate.h"
#include "array.h"
#include "posterror.h"
#include "wrappers.h"
#include "generics.h"
#include "typestring.h"
#include "typedesc.h"
#include "cgencpu.h"
#include "eventtrace.h"
#include "typekey.h"
#include "pendingload.h"
#include "proftoeeinterfaceimpl.h"
#include "virtualcallstub.h"
#include "stringarraylist.h"


// This method determines the "loader module" for an instantiated type
// or method. The rule must ensure that any types involved in the
// instantiated type or method do not outlive the loader module itself
// with respect to app-domain unloading (e.g. MyList<MyType> can't be
// put in the module of MyList if MyList's assembly is
// app-domain-neutral but MyType's assembly is app-domain-specific).
// The rule we use is:
//
// * Pick the first type in the class instantiation, followed by
//   method instantiation, whose loader module is non-shared (app-domain-bound)
// * If no type is app-domain-bound, return the module containing the generic type itself
//
// Some useful effects of this rule (for ngen purposes) are:
//
// * G<object,...,object> lives in the module defining G
// * non-CoreLib instantiations of CoreLib-defined generic types live in the module
//   of the instantiation (when only one module is invloved in the instantiation)
//

/* static */
PTR_Module ClassLoader::ComputeLoaderModuleWorker(
    Module *     pDefinitionModule,  // the module that declares the generic type or method
    mdToken      token,              // method or class token for this item
    Instantiation classInst,         // the type arguments to the type (if any)
    Instantiation methodInst)        // the type arguments to the method (if any)
{
    CONTRACT(Module*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        PRECONDITION(CheckPointer(pDefinitionModule, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL));
        SUPPORTS_DAC;
    }
    CONTRACT_END

    if (classInst.IsEmpty() && methodInst.IsEmpty())
        RETURN PTR_Module(pDefinitionModule);

#ifndef DACCESS_COMPILE
#ifdef FEATURE_NATIVE_IMAGE_GENERATION
    //
    // Use special loader module placement during compilation of fragile native images.
    //
    // ComputeLoaderModuleForCompilation algorithm assumes that we are using fragile native image
    // for CoreLib (or compiling CoreLib itself). It is not the case for ReadyToRun compilation because
    // CoreLib as always treated as IL there (see code:PEFile::ShouldTreatNIAsMSIL for details).
    //
    if (IsCompilationProcess() && !IsReadyToRunCompilation())
    {
        RETURN(ComputeLoaderModuleForCompilation(pDefinitionModule, token, classInst, methodInst));
    }
#endif // FEATURE_PREJIT
#endif // #ifndef DACCESS_COMPILE

    Module *pLoaderModule = NULL;

    if (pDefinitionModule)
    {
        if (pDefinitionModule->IsCollectible())
            goto ComputeCollectibleLoaderModule;
        pLoaderModule = pDefinitionModule;
    }

    for (DWORD i = 0; i < classInst.GetNumArgs(); i++)
    {
        TypeHandle classArg = classInst[i];
        _ASSERTE(!classArg.IsEncodedFixup());
        Module* pModule = classArg.GetLoaderModule();
        if (pModule->IsCollectible())
            goto ComputeCollectibleLoaderModule;
        if (pLoaderModule == NULL)
            pLoaderModule = pModule;
    }

    for (DWORD i = 0; i < methodInst.GetNumArgs(); i++)
    {
        TypeHandle methodArg = methodInst[i];
        _ASSERTE(!methodArg.IsEncodedFixup());
        Module *pModule = methodArg.GetLoaderModule();
        if (pModule->IsCollectible())
            goto ComputeCollectibleLoaderModule;
        if (pLoaderModule == NULL)
            pLoaderModule = pModule;
    }

    if (pLoaderModule == NULL)
    {
        CONSISTENCY_CHECK(CoreLibBinder::GetModule() && CoreLibBinder::GetModule()->IsSystem());

        pLoaderModule = CoreLibBinder::GetModule();
    }

    if (FALSE)
    {
ComputeCollectibleLoaderModule:
        LoaderAllocator *pLoaderAllocatorOfDefiningType = NULL;
        LoaderAllocator *pOldestLoaderAllocator = NULL;
        Module *pOldestLoaderModule = NULL;
        UINT64 oldestFoundAge = 0;
        DWORD classArgsCount = classInst.GetNumArgs();
        DWORD totalArgsCount = classArgsCount + methodInst.GetNumArgs();

        if (pDefinitionModule != NULL) pLoaderAllocatorOfDefiningType = pDefinitionModule->GetLoaderAllocator();

        for (DWORD i = 0; i < totalArgsCount; i++) {

            TypeHandle arg;

            if (i < classArgsCount)
                arg = classInst[i];
            else
                arg = methodInst[i - classArgsCount];

            Module *pModuleCheck = arg.GetLoaderModule();
            LoaderAllocator *pLoaderAllocatorCheck = pModuleCheck->GetLoaderAllocator();

            if (pLoaderAllocatorCheck != pLoaderAllocatorOfDefiningType &&
                pLoaderAllocatorCheck->IsCollectible() &&
                pLoaderAllocatorCheck->GetCreationNumber() > oldestFoundAge)
            {
                pOldestLoaderModule = pModuleCheck;
                pOldestLoaderAllocator = pLoaderAllocatorCheck;
                oldestFoundAge = pLoaderAllocatorCheck->GetCreationNumber();
            }
        }

        // Only if we didn't find a different loader allocator than the defining loader allocator do we
        // use the defining loader allocator
        if (pOldestLoaderModule != NULL)
            pLoaderModule = pOldestLoaderModule;
        else
            pLoaderModule = pDefinitionModule;
    }
    RETURN PTR_Module(pLoaderModule);
}

#ifndef DACCESS_COMPILE
#ifdef FEATURE_NATIVE_IMAGE_GENERATION
/* static */
PTR_Module ClassLoader::ComputeLoaderModuleForCompilation(
    Module *     pDefinitionModule,  // the module that declares the generic type or method
    mdToken      token,              // method or class token for this item
    Instantiation classInst,         // the type arguments to the type (if any)
    Instantiation methodInst)        // the type arguments to the method (if any)
{
    CONTRACT(Module*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        PRECONDITION(CheckPointer(pDefinitionModule, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END

    // The NGEN rule for compiling constructed types and instantiated methods
    // into modules other than their "natural" LoaderModule. This is at the heart of
    // "full generics NGEN".
    //
    // If this instantiation doesn't have a unique home then use the ngen module

    // OK, we're certainly NGEN'ing.  And if we're NGEN'ing then we're not on the debugger thread.
    CONSISTENCY_CHECK(((GetThreadNULLOk() && GetAppDomain()) || IsGCThread()) &&
        "unexpected: running a load on debug thread but IsCompilationProcess() returned TRUE");

    // Save it into its PreferredZapModule if it's always going to be saved there.
    // This is a stable choice - no need to record it in the table (as we do for others below)
    if (Module::IsAlwaysSavedInPreferredZapModule(classInst, methodInst))
    {
        RETURN (Module::ComputePreferredZapModule(pDefinitionModule, classInst, methodInst));
    }

    // Check if this compilation process has already decided on an adjustment.  Once we decide
    // on the LoaderModule for an item it must be stable for the duration of a
    // compilation process, no matter how many modules get NGEN'd.

    ZapperLoaderModuleTableKey key(pDefinitionModule,
                                   token,
                                   classInst,
                                   methodInst);

    Module * pZapperLoaderModule = g_pCEECompileInfo->LookupZapperLoaderModule(&key);
    if (pZapperLoaderModule != NULL)
    {
        RETURN (pZapperLoaderModule);
    }

    // OK, we need to compute a non-standard zapping module.

    Module * pPreferredZapModule = Module::ComputePreferredZapModule(pDefinitionModule, classInst, methodInst);

    // Check if we're NGEN'ing but where perhaps the compilation domain
    // isn't set up yet.  This can happen in following situations:
    // - Managed code running during startup before compilation domain is setup.
    // - Exceptions (e.g. invalid program exceptions) thrown from compilation domain and caught in default domain

    // We're a little stuck - we can't force the item into an NGEN image at this point.  So just bail out
    // and use the loader module we've computed without recording the choice. The loader module should always
    // be CoreLib in this case.
    AppDomain * pAppDomain = GetAppDomain();
    if (!pAppDomain->IsCompilationDomain() ||
        !pAppDomain->ToCompilationDomain()->GetTargetModule())
    {
        _ASSERTE(pPreferredZapModule->IsSystem() || IsNgenPDBCompilationProcess());
        RETURN (pPreferredZapModule);
    }

    Module * pTargetModule = pAppDomain->ToCompilationDomain()->GetTargetModule();

    // If it is multi-module assembly and we have not saved PZM yet, do not create
    // speculative instantiation - just save it in PZM.
    if (pTargetModule->GetAssembly() == pPreferredZapModule->GetAssembly() &&
        !pPreferredZapModule->IsModuleSaved())
    {
        pZapperLoaderModule = pPreferredZapModule;
    }
    else
    {
        // Everything else can be saved into the current module.
        pZapperLoaderModule = pTargetModule;
    }

    // Record this choice just in case we're NGEN'ing multiple modules
    // to make sure we always do the same thing if we're asked to compute
    // the loader module again.

    // Note this whole code path only happens while NGEN'ing, so this violation
    // is not so bad.  It is needed since we allocate stuff on the heap.
    CONTRACT_VIOLATION(ThrowsViolation|FaultViolation);

    // Copy the instantiation arrays so they can escape the scope of this method.
    // Since this is a permanent entry in a table for this compilation process
    // we do not need to collect these.  If we did have to we would do it when we deleteed the
    // ZapperLoaderModuleTable.
    NewArrayHolder<TypeHandle> pClassArgs = NULL;
    if (!classInst.IsEmpty())
    {
        pClassArgs = new TypeHandle[classInst.GetNumArgs()];
        for (unsigned int i = 0; i < classInst.GetNumArgs(); i++)
            pClassArgs[i] = classInst[i];
    }

    NewArrayHolder<TypeHandle> pMethodArgs = NULL;
    if (!methodInst.IsEmpty())
    {
        pMethodArgs = new TypeHandle[methodInst.GetNumArgs()];
        for (unsigned int i = 0; i < methodInst.GetNumArgs(); i++)
            pMethodArgs[i] = methodInst[i];
    }

    ZapperLoaderModuleTableKey key2(pDefinitionModule,
                                    token,
                                    Instantiation(pClassArgs, classInst.GetNumArgs()),
                                    Instantiation(pMethodArgs, methodInst.GetNumArgs()));
    g_pCEECompileInfo->RecordZapperLoaderModule(&key2, pZapperLoaderModule);

    pClassArgs.SuppressRelease();
    pMethodArgs.SuppressRelease();

    RETURN (pZapperLoaderModule);
}
#endif // FEATURE_NATIVE_IMAGE_GENERATION
#endif // #ifndef DACCESS_COMPILE

/*static*/
Module * ClassLoader::ComputeLoaderModule(MethodTable * pMT,
                                          mdToken       token,
                                          Instantiation methodInst)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    return ComputeLoaderModuleWorker(pMT->GetModule(),
                               token,
                               pMT->GetInstantiation(),
                               methodInst);
}
/*static*/
Module *ClassLoader::ComputeLoaderModule(TypeKey *typeKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;


    if (typeKey->GetKind() == ELEMENT_TYPE_CLASS)
        return ComputeLoaderModuleWorker(typeKey->GetModule(),
                                   typeKey->GetTypeToken(),
                                   typeKey->GetInstantiation(),
                                   Instantiation());
    else if (typeKey->GetKind() == ELEMENT_TYPE_FNPTR)
        return ComputeLoaderModuleForFunctionPointer(typeKey->GetRetAndArgTypes(), typeKey->GetNumArgs() + 1);
    else
        return ComputeLoaderModuleForParamType(typeKey->GetElementType());
}

/*static*/
BOOL ClassLoader::IsTypicalInstantiation(Module *pModule, mdToken token, Instantiation inst)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(TypeFromToken(token) == mdtTypeDef || TypeFromToken(token) == mdtMethodDef);
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    for (DWORD i = 0; i < inst.GetNumArgs(); i++)
    {
        TypeHandle thArg = inst[i];

        if (thArg.IsGenericVariable())
        {
            TypeVarTypeDesc* tyvar = thArg.AsGenericVariable();

            PREFIX_ASSUME(tyvar!=NULL);
            if ((tyvar->GetTypeOrMethodDef() != token) ||
                (tyvar->GetModule() != dac_cast<PTR_Module>(pModule)) ||
                (tyvar->GetIndex() != i))
                return FALSE;
        }
        else
        {
            return FALSE;
        }
    }
    return TRUE;
}

// External class loader entry point: load a type by name
/*static*/
TypeHandle ClassLoader::LoadTypeByNameThrowing(Assembly *pAssembly,
                                               LPCUTF8 nameSpace,
                                               LPCUTF8 name,
                                               NotFoundAction fNotFound,
                                               ClassLoader::LoadTypesFlag fLoadTypes,
                                               ClassLoadLevel level)
{
    CONTRACT(TypeHandle)
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;

        if (FORBIDGC_LOADER_USE_ENABLED() || fLoadTypes != LoadTypes) { LOADS_TYPE(CLASS_LOAD_BEGIN); } else { LOADS_TYPE(level); }

        PRECONDITION(CheckPointer(pAssembly));
        PRECONDITION(level > CLASS_LOAD_BEGIN && level <= CLASS_LOADED);
        POSTCONDITION(CheckPointer(RETVAL,
                     (fNotFound == ThrowIfNotFound && fLoadTypes == LoadTypes )? NULL_NOT_OK : NULL_OK));
        POSTCONDITION(RETVAL.IsNull() || RETVAL.CheckLoadLevel(level));
        SUPPORTS_DAC;
#ifdef DACCESS_COMPILE
        PRECONDITION((fNotFound == ClassLoader::ReturnNullIfNotFound) && (fLoadTypes == DontLoadTypes));
#endif
    }
    CONTRACT_END

    NameHandle nameHandle(nameSpace, name);
    if (fLoadTypes == DontLoadTypes)
        nameHandle.SetTokenNotToLoad(tdAllTypes);
    if (fNotFound == ThrowIfNotFound)
        RETURN pAssembly->GetLoader()->LoadTypeHandleThrowIfFailed(&nameHandle, level);
    else
        RETURN pAssembly->GetLoader()->LoadTypeHandleThrowing(&nameHandle, level);
}

#ifndef DACCESS_COMPILE

#define DAC_LOADS_TYPE(level, expression) \
    if (FORBIDGC_LOADER_USE_ENABLED() || (expression)) \
        { LOADS_TYPE(CLASS_LOAD_BEGIN); } else { LOADS_TYPE(level); }
#else

#define DAC_LOADS_TYPE(level, expression) { LOADS_TYPE(CLASS_LOAD_BEGIN); }
#endif // #ifndef DACCESS_COMPILE

//
// Find a class given name, using the classloader's global list of known classes.
// If the type is found, it will be restored unless pName->GetTokenNotToLoad() prohibits that
// Returns NULL if class not found AND pName->OKToLoad returns false
TypeHandle ClassLoader::LoadTypeHandleThrowIfFailed(NameHandle* pName, ClassLoadLevel level,
                                                    Module* pLookInThisModuleOnly/*=NULL*/)
{
    CONTRACT(TypeHandle)
    {
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        DAC_LOADS_TYPE(level, !pName->OKToLoad());
        MODE_ANY;
        PRECONDITION(CheckPointer(pName));
        PRECONDITION(level > CLASS_LOAD_BEGIN && level <= CLASS_LOADED);
        POSTCONDITION(CheckPointer(RETVAL, pName->OKToLoad() ? NULL_NOT_OK : NULL_OK));
        POSTCONDITION(RETVAL.IsNull() || RETVAL.CheckLoadLevel(level));
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    // Lookup in the classes that this class loader knows about
    TypeHandle typeHnd = LoadTypeHandleThrowing(pName, level, pLookInThisModuleOnly);

    if(typeHnd.IsNull()) {

        if ( pName->OKToLoad() ) {
#ifdef _DEBUG_IMPL
            {
                LPCUTF8 szName = pName->GetName();
                if (szName == NULL)
                    szName = "<UNKNOWN>";

                StackSString codeBase;
                GetAssembly()->GetCodeBase(codeBase);

                LOG((LF_CLASSLOADER, LL_INFO10, "Failed to find class \"%s\" in the manifest for assembly \"%ws\"\n", szName, (LPCWSTR)codeBase));
            }
#endif

#ifndef DACCESS_COMPILE
            m_pAssembly->ThrowTypeLoadException(pName, IDS_CLASSLOAD_GENERAL);
#else
            DacNotImpl();
#endif
        }
    }

    RETURN(typeHnd);
}

#ifndef DACCESS_COMPILE

//<TODO>@TODO: Need to allow exceptions to be thrown when classloader is cleaned up</TODO>
EEClassHashEntry_t* ClassLoader::InsertValue(EEClassHashTable *pClassHash, EEClassHashTable *pClassCaseInsHash, LPCUTF8 pszNamespace, LPCUTF8 pszClassName, HashDatum Data, EEClassHashEntry_t *pEncloser, AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    LPUTF8 pszLowerCaseNS = NULL;
    LPUTF8 pszLowerCaseName = NULL;
    EEClassHashEntry_t *pCaseInsEntry = NULL;

    EEClassHashEntry_t *pEntry = pClassHash->AllocNewEntry(pamTracker);

    if (pClassCaseInsHash) {
        CreateCanonicallyCasedKey(pszNamespace, pszClassName, &pszLowerCaseNS, &pszLowerCaseName);
        pCaseInsEntry = pClassCaseInsHash->AllocNewEntry(pamTracker);
    }


    {
        // ! We cannot fail after this point.
        CANNOTTHROWCOMPLUSEXCEPTION();
        FAULT_FORBID();


        pClassHash->InsertValueUsingPreallocatedEntry(pEntry, pszNamespace, pszClassName, Data, pEncloser);

        //If we're keeping a table for case-insensitive lookup, keep that up to date
        if (pClassCaseInsHash)
            pClassCaseInsHash->InsertValueUsingPreallocatedEntry(pCaseInsEntry, pszLowerCaseNS, pszLowerCaseName, pEntry, pEncloser);

        return pEntry;
    }

}

#endif // #ifndef DACCESS_COMPILE

BOOL ClassLoader::CompareNestedEntryWithExportedType(IMDInternalImport *  pImport,
                                                     mdExportedType       mdCurrent,
                                                     EEClassHashTable *   pClassHash,
                                                     PTR_EEClassHashEntry pEntry)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    LPCUTF8 Key[2];

    do
    {
        if (FAILED(pImport->GetExportedTypeProps(
            mdCurrent,
            &Key[0],
            &Key[1],
            &mdCurrent,
            NULL,   //binding (type def)
            NULL))) //flags
        {
            return FALSE;
        }

        if (pClassHash->CompareKeys(pEntry, Key))
        {
            // Reached top level class for mdCurrent - return whether
            // or not pEntry is a top level class
            // (pEntry is a top level class if its pEncloser is NULL)
            if ((TypeFromToken(mdCurrent) != mdtExportedType) ||
                (mdCurrent == mdExportedTypeNil))
            {
                return pEntry->GetEncloser() == NULL;
            }
        }
        else // Keys don't match - wrong entry
        {
            return FALSE;
        }
    }
    while ((pEntry = pEntry->GetEncloser()) != NULL);

    // Reached the top level class for pEntry, but mdCurrent is nested
    return FALSE;
}


BOOL ClassLoader::CompareNestedEntryWithTypeDef(IMDInternalImport *  pImport,
                                                mdTypeDef            mdCurrent,
                                                EEClassHashTable *   pClassHash,
                                                PTR_EEClassHashEntry pEntry)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    LPCUTF8 Key[2];

    do {
        if (FAILED(pImport->GetNameOfTypeDef(mdCurrent, &Key[1], &Key[0])))
        {
            return FALSE;
        }

        if (pClassHash->CompareKeys(pEntry, Key)) {
            // Reached top level class for mdCurrent - return whether
            // or not pEntry is a top level class
            // (pEntry is a top level class if its pEncloser is NULL)
            if (FAILED(pImport->GetNestedClassProps(mdCurrent, &mdCurrent)))
                return pEntry->GetEncloser() == NULL;
        }
        else // Keys don't match - wrong entry
            return FALSE;
    }
    while ((pEntry = pEntry->GetEncloser()) != NULL);

    // Reached the top level class for pEntry, but mdCurrent is nested
    return FALSE;
}


BOOL ClassLoader::CompareNestedEntryWithTypeRef(IMDInternalImport *  pImport,
                                                mdTypeRef            mdCurrent,
                                                EEClassHashTable *   pClassHash,
                                                PTR_EEClassHashEntry pEntry)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    LPCUTF8 Key[2];

    do {
        if (FAILED(pImport->GetNameOfTypeRef(mdCurrent, &Key[0], &Key[1])))
        {
            return FALSE;
        }

        if (pClassHash->CompareKeys(pEntry, Key))
        {
            if (FAILED(pImport->GetResolutionScopeOfTypeRef(mdCurrent, &mdCurrent)))
            {
                return FALSE;
            }
            // Reached top level class for mdCurrent - return whether
            // or not pEntry is a top level class
            // (pEntry is a top level class if its pEncloser is NULL)
            if ((TypeFromToken(mdCurrent) != mdtTypeRef) ||
                (mdCurrent == mdTypeRefNil))
                return pEntry->GetEncloser() == NULL;
        }
        else // Keys don't match - wrong entry
            return FALSE;
    }
    while ((pEntry = pEntry->GetEncloser())!=NULL);

    // Reached the top level class for pEntry, but mdCurrent is nested
    return FALSE;
}


/*static*/
BOOL ClassLoader::IsNested(Module *pModule, mdToken token, mdToken *mdEncloser)
{
    CONTRACTL
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    switch(TypeFromToken(token)) {
        case mdtTypeDef:
            return (SUCCEEDED(pModule->GetMDImport()->GetNestedClassProps(token, mdEncloser)));

        case mdtTypeRef:
            IfFailThrow(pModule->GetMDImport()->GetResolutionScopeOfTypeRef(token, mdEncloser));
            return ((TypeFromToken(*mdEncloser) == mdtTypeRef) &&
                    (*mdEncloser != mdTypeRefNil));

        case mdtExportedType:
            IfFailThrow(pModule->GetAssembly()->GetManifestImport()->GetExportedTypeProps(
                token,
                NULL,   // namespace
                NULL,   // name
                mdEncloser,
                NULL,   //binding (type def)
                NULL)); //flags
            return ((TypeFromToken(*mdEncloser) == mdtExportedType) &&
                    (*mdEncloser != mdExportedTypeNil));

        default:
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_TOKEN_TYPE);
    }
}

BOOL ClassLoader::IsNested(const NameHandle* pName, mdToken *mdEncloser)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (pName->GetTypeModule()) {
        if (TypeFromToken(pName->GetTypeToken()) == mdtBaseType)
        {
            if (!pName->GetBucket().IsNull())
                return TRUE;
            return FALSE;
        }
        else
            return IsNested(pName->GetTypeModule(), pName->GetTypeToken(), mdEncloser);
    }
    else
        return FALSE;
}

void ClassLoader::GetClassValue(NameHandleTable nhTable,
                                    const NameHandle *pName,
                                    HashDatum *pData,
                                    EEClassHashTable **ppTable,
                                    Module* pLookInThisModuleOnly,
                                    HashedTypeEntry* pFoundEntry,
                                    Loader::LoadFlag loadFlag,
                                    BOOL& needsToBuildHashtable)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        MODE_ANY;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        PRECONDITION(CheckPointer(pName));
        SUPPORTS_DAC;
    }
    CONTRACTL_END


    mdToken             mdEncloser;
    EEClassHashEntry_t  *pBucket = NULL;

    needsToBuildHashtable = FALSE;

#if _DEBUG
    if (pName->GetName()) {
        if (pName->GetNameSpace() == NULL)
            LOG((LF_CLASSLOADER, LL_INFO1000, "Looking up %s by name.\n",
                 pName->GetName()));
        else
            LOG((LF_CLASSLOADER, LL_INFO1000, "Looking up %s.%s by name.\n",
                 pName->GetNameSpace(), pName->GetName()));
    }
#endif

    BOOL isNested = IsNested(pName, &mdEncloser);

    PTR_Assembly assembly = GetAssembly();
    PREFIX_ASSUME(assembly != NULL);
    ModuleIterator i = assembly->IterateModules();

    while (i.Next())
    {
        Module * pCurrentClsModule = i.GetModule();
        PREFIX_ASSUME(pCurrentClsModule != NULL);

        if (pCurrentClsModule->IsResource())
            continue;
        if (pLookInThisModuleOnly && (pCurrentClsModule != pLookInThisModuleOnly))
            continue;

#ifdef FEATURE_READYTORUN
        if (nhTable == nhCaseSensitive && pCurrentClsModule->IsReadyToRun() && pCurrentClsModule->GetReadyToRunInfo()->HasHashtableOfTypes() &&
            pCurrentClsModule->GetAvailableClassHash() == NULL)
        {
            // For R2R modules, we only search the hashtable of token types stored in the module's image, and don't fallback
            // to searching m_pAvailableClasses or m_pAvailableClassesCaseIns (in fact, we don't even allocate them for R2R modules).
            // Also note that type lookups in R2R modules only support case sensitive lookups.

            mdToken mdFoundTypeToken;
            if (pCurrentClsModule->GetReadyToRunInfo()->TryLookupTypeTokenFromName(pName, &mdFoundTypeToken))
            {
                if (TypeFromToken(mdFoundTypeToken) == mdtExportedType)
                {
                    mdToken mdUnused;
                    Module * pTargetModule = GetAssembly()->FindModuleByExportedType(mdFoundTypeToken, loadFlag, mdTypeDefNil, &mdUnused);

                    pFoundEntry->SetTokenBasedEntryValue(mdFoundTypeToken, pTargetModule);
                }
                else
                {
                    pFoundEntry->SetTokenBasedEntryValue(mdFoundTypeToken, pCurrentClsModule);
                }

                return; // Return on the first success
            }
        }
        else
#endif
        {
            EEClassHashTable* pTable = NULL;
            if (nhTable == nhCaseSensitive)
            {
                *ppTable = pTable = pCurrentClsModule->GetAvailableClassHash();

#ifdef FEATURE_READYTORUN
                if (pTable == NULL && pCurrentClsModule->IsReadyToRun() && !pCurrentClsModule->GetReadyToRunInfo()->HasHashtableOfTypes())
                {
                    // Old R2R image generated without the hashtable of types.
                    // We fallback to the slow path of creating the hashtable dynamically
                    // at execution time in that scenario. The caller will handle
                    pFoundEntry->SetClassHashBasedEntryValue(NULL);
                    needsToBuildHashtable = TRUE;
                    return;
                }
#endif
            }
            else
            {
                // currently we expect only these two kinds--for DAC builds, nhTable will be nhCaseSensitive
                _ASSERTE(nhTable == nhCaseInsensitive);
                *ppTable = pTable = pCurrentClsModule->GetAvailableClassCaseInsHash();

                if (pTable == NULL)
                {
                    // We have not built the table yet - the caller will handle
                    pFoundEntry->SetClassHashBasedEntryValue(NULL);
                    needsToBuildHashtable = TRUE;
                    return;
                }
            }
            _ASSERTE(pTable);

            if (isNested)
            {
                Module *pNameModule = pName->GetTypeModule();
                PREFIX_ASSUME(pNameModule != NULL);

                EEClassHashTable::LookupContext sContext;
                if ((pBucket = pTable->GetValue(pName, pData, TRUE, &sContext)) != NULL)
                {
                    switch (TypeFromToken(pName->GetTypeToken()))
                    {
                    case mdtTypeDef:
                        while ((!CompareNestedEntryWithTypeDef(pNameModule->GetMDImport(),
                                                               mdEncloser,
                                                               pCurrentClsModule->GetAvailableClassHash(),
                                                               pBucket->GetEncloser())) &&
                                                               (pBucket = pTable->FindNextNestedClass(pName, pData, &sContext)) != NULL);
                        break;
                    case mdtTypeRef:
                        while ((!CompareNestedEntryWithTypeRef(pNameModule->GetMDImport(),
                                                               mdEncloser,
                                                               pCurrentClsModule->GetAvailableClassHash(),
                                                               pBucket->GetEncloser())) &&
                                                               (pBucket = pTable->FindNextNestedClass(pName, pData, &sContext)) != NULL);
                        break;
                    case mdtExportedType:
                        while ((!CompareNestedEntryWithExportedType(pNameModule->GetAssembly()->GetManifestImport(),
                                                                    mdEncloser,
                                                                    pCurrentClsModule->GetAvailableClassHash(),
                                                                    pBucket->GetEncloser())) &&
                                                                    (pBucket = pTable->FindNextNestedClass(pName, pData, &sContext)) != NULL);
                        break;
                    default:
                        while ((pBucket->GetEncloser() != pName->GetBucket().GetClassHashBasedEntryValue()) &&
                            (pBucket = pTable->FindNextNestedClass(pName, pData, &sContext)) != NULL);
                    }
                }
            }
            else
            {
                pBucket = pTable->GetValue(pName, pData, FALSE, NULL);
            }

            if (pBucket) // Return on the first success
            {
                pFoundEntry->SetClassHashBasedEntryValue(pBucket);
                return;
            }
        }
    }

    // No results found: default to a NULL EEClassHashEntry_t result
    pFoundEntry->SetClassHashBasedEntryValue(NULL);
}

#ifndef DACCESS_COMPILE

VOID ClassLoader::PopulateAvailableClassHashTable(Module* pModule,
                                                  AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    mdTypeDef           td;
    HENUMInternal       hTypeDefEnum;
    IMDInternalImport * pImport = pModule->GetMDImport();

    IfFailThrow(pImport->EnumTypeDefInit(&hTypeDefEnum));

    // Now loop through all the classdefs adding the CVID and scope to the hash
    while(pImport->EnumNext(&hTypeDefEnum, &td)) {

        AddAvailableClassHaveLock(pModule,
                                  td,
                                  pamTracker);
    }
    pImport->EnumClose(&hTypeDefEnum);
}


void ClassLoader::LazyPopulateCaseSensitiveHashTablesDontHaveLock()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;


    CrstHolder ch(&m_AvailableClassLock);
    LazyPopulateCaseSensitiveHashTables();
}

void ClassLoader::LazyPopulateCaseSensitiveHashTables()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    AllocMemTracker amTracker;
    ModuleIterator i = GetAssembly()->IterateModules();

    // Create a case-sensitive hashtable for each module, and fill it with the module's typedef entries
    while (i.Next())
    {
        Module *pModule = i.GetModule();
        PREFIX_ASSUME(pModule != NULL);
        if (pModule->IsResource() || pModule->GetAvailableClassHash() != NULL)
            continue;

        // Lazy construction of the case-sensitive hashtable of types is *only* a scenario for ReadyToRun images
        // (either images compiled with an old version of crossgen, or for case-insensitive type lookups in R2R modules)
        _ASSERT(pModule->IsReadyToRun());

        EEClassHashTable * pNewClassHash = EEClassHashTable::Create(pModule, AVAILABLE_CLASSES_HASH_BUCKETS, FALSE /* bCaseInsensitive */, &amTracker);
        pModule->SetAvailableClassHash(pNewClassHash);

        PopulateAvailableClassHashTable(pModule, &amTracker);
    }

    // Add exported types of the manifest module to the hashtable
    if (!GetAssembly()->GetManifestModule()->IsResource())
    {
        IMDInternalImport * pManifestImport = GetAssembly()->GetManifestImport();
        HENUMInternalHolder phEnum(pManifestImport);
        phEnum.EnumInit(mdtExportedType, mdTokenNil);

        mdToken mdExportedType;
        while (pManifestImport->EnumNext(&phEnum, &mdExportedType))
            AddExportedTypeHaveLock(GetAssembly()->GetManifestModule(), mdExportedType, &amTracker);
    }

    amTracker.SuppressRelease();
}

void ClassLoader::LazyPopulateCaseInsensitiveHashTables()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    if (!GetAssembly()->GetManifestModule()->IsResource() && GetAssembly()->GetManifestModule()->GetAvailableClassHash() == NULL)
    {
        // This is a R2R assembly, and a case insensitive type lookup was triggered.
        // Construct the case-sensitive table first, since the case-insensitive table
        // create piggy-backs on the first.
        LazyPopulateCaseSensitiveHashTables();
    }

    // Add any unhashed modules into our hash tables, and try again.

    AllocMemTracker amTracker;
    ModuleIterator i = GetAssembly()->IterateModules();

    while (i.Next())
    {
        Module *pModule = i.GetModule();
        if (pModule->IsResource())
            continue;

        if (pModule->GetAvailableClassCaseInsHash() == NULL)
        {
            EEClassHashTable *pNewClassCaseInsHash = pModule->GetAvailableClassHash()->MakeCaseInsensitiveTable(pModule, &amTracker);

            LOG((LF_CLASSLOADER, LL_INFO10, "%s's classes being added to case insensitive hash table\n",
                 pModule->GetSimpleName()));

            {
                CANNOTTHROWCOMPLUSEXCEPTION();
                FAULT_FORBID();

                amTracker.SuppressRelease();
                pModule->SetAvailableClassCaseInsHash(pNewClassCaseInsHash);
                FastInterlockDecrement((LONG*)&m_cUnhashedModules);
            }
        }
    }
}

/*static*/
void DECLSPEC_NORETURN ClassLoader::ThrowTypeLoadException(TypeKey *pKey,
                                                           UINT resIDWhy)
{
    STATIC_CONTRACT_THROWS;

    StackSString fullName;
    StackSString assemblyName;
    TypeString::AppendTypeKey(fullName, pKey);
    pKey->GetModule()->GetAssembly()->GetDisplayName(assemblyName);
    ::ThrowTypeLoadException(fullName, assemblyName, NULL, resIDWhy);
}

#endif


TypeHandle ClassLoader::LoadConstructedTypeThrowing(TypeKey *pKey,
                                                    LoadTypesFlag fLoadTypes /*= LoadTypes*/,
                                                    ClassLoadLevel level /*=CLASS_LOADED*/,
                                                    const InstantiationContext *pInstContext /*=NULL*/)
{
    CONTRACT(TypeHandle)
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        if (FORBIDGC_LOADER_USE_ENABLED() || fLoadTypes != LoadTypes) { LOADS_TYPE(CLASS_LOAD_BEGIN); } else { LOADS_TYPE(level); }
        PRECONDITION(CheckPointer(pKey));
        PRECONDITION(level > CLASS_LOAD_BEGIN && level <= CLASS_LOADED);
        PRECONDITION(CheckPointer(pInstContext, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL, fLoadTypes==DontLoadTypes ? NULL_OK : NULL_NOT_OK));
        POSTCONDITION(RETVAL.IsNull() || RETVAL.GetLoadLevel() >= level);
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END

    TypeHandle typeHnd;
    ClassLoadLevel existingLoadLevel = CLASS_LOAD_BEGIN;

    // Lookup in the classes that this class loader knows about

    if (pKey->HasInstantiation() && ClassLoader::IsTypicalSharedInstantiation(pKey->GetInstantiation()))
    {
        _ASSERTE(pKey->GetModule() == ComputeLoaderModule(pKey));
        typeHnd = pKey->GetModule()->LookupFullyCanonicalInstantiation(pKey->GetTypeToken(), &existingLoadLevel);
    }

    if (typeHnd.IsNull())
    {
        typeHnd = LookupTypeHandleForTypeKey(pKey);
        if (!typeHnd.IsNull())
        {
            existingLoadLevel = typeHnd.GetLoadLevel();
            if (existingLoadLevel >= level)
                g_IBCLogger.LogTypeHashTableAccess(&typeHnd);
        }
    }

    // If something has been published in the tables, and it's at the right level, just return it
    if (!typeHnd.IsNull() && existingLoadLevel >= level)
    {
        RETURN typeHnd;
    }

#ifndef DACCESS_COMPILE
    if (typeHnd.IsNull() && pKey->HasInstantiation())
    {
        if (!Generics::CheckInstantiation(pKey->GetInstantiation()))
            pKey->GetModule()->GetAssembly()->ThrowTypeLoadException(pKey->GetModule()->GetMDImport(), pKey->GetTypeToken(), IDS_CLASSLOAD_INVALIDINSTANTIATION);
    }
#endif

    // If we're not loading any types at all, then we're not creating
    // instantiations either because we're in FORBIDGC_LOADER_USE mode, so
    // we should bail out here.
    if (fLoadTypes == DontLoadTypes)
        RETURN TypeHandle();

#ifndef DACCESS_COMPILE
    // If we got here, we now have to allocate a new parameterized type.
    // By definition, forbidgc-users aren't allowed to reach this point.
    CONSISTENCY_CHECK(!FORBIDGC_LOADER_USE_ENABLED());

    Module *pLoaderModule = ComputeLoaderModule(pKey);
    RETURN(pLoaderModule->GetClassLoader()->LoadTypeHandleForTypeKey(pKey, typeHnd, level, pInstContext));
#else
    DacNotImpl();
    RETURN(typeHnd);
#endif
}


/*static*/
void ClassLoader::EnsureLoaded(TypeHandle typeHnd, ClassLoadLevel level)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(typeHnd));
        PRECONDITION(level > CLASS_LOAD_BEGIN && level <= CLASS_LOADED);
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        if (FORBIDGC_LOADER_USE_ENABLED()) { LOADS_TYPE(CLASS_LOAD_BEGIN); } else { LOADS_TYPE(level); }
        SUPPORTS_DAC;

        MODE_ANY;
    }
    CONTRACTL_END

#ifndef DACCESS_COMPILE // Nothing to do for the DAC case

    if (typeHnd.GetLoadLevel() < level)
    {
#ifdef FEATURE_PREJIT
        if (typeHnd.GetLoadLevel() == CLASS_LOAD_UNRESTOREDTYPEKEY)
        {
            typeHnd.DoRestoreTypeKey();
        }
#endif
        if (level > CLASS_LOAD_UNRESTORED)
        {
            TypeKey typeKey = typeHnd.GetTypeKey();

            Module *pLoaderModule = ComputeLoaderModule(&typeKey);
            pLoaderModule->GetClassLoader()->LoadTypeHandleForTypeKey(&typeKey, typeHnd, level);
        }
    }

#endif // DACCESS_COMPILE
}

/*static*/
void ClassLoader::TryEnsureLoaded(TypeHandle typeHnd, ClassLoadLevel level)
{
    WRAPPER_NO_CONTRACT;

#ifndef DACCESS_COMPILE // Nothing to do for the DAC case

    EX_TRY
    {
        ClassLoader::EnsureLoaded(typeHnd, level);
    }
    EX_CATCH
    {
        // Some type may not load successfully. For eg. generic instantiations
        // that do not satisfy the constraints of the type arguments.
    }
    EX_END_CATCH(RethrowTerminalExceptions);

#endif // DACCESS_COMPILE
}

// This is separated out to avoid the overhead of C++ exception handling in the non-locking case.
/* static */
TypeHandle ClassLoader::LookupTypeKeyUnderLock(TypeKey *pKey,
                                               EETypeHashTable *pTable,
                                               CrstBase *pLock)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    // m_AvailableTypesLock has to be taken in cooperative mode to avoid deadlocks during GC
    GCX_MAYBE_COOP_NO_THREAD_BROKEN(!IsGCThread());

    CrstHolder ch(pLock);
    return pTable->GetValue(pKey);
}

/* static */
TypeHandle ClassLoader::LookupTypeKey(TypeKey *pKey,
                                      EETypeHashTable *pTable,
                                      CrstBase *pLock,
                                      BOOL fCheckUnderLock)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        PRECONDITION(CheckPointer(pKey));
        PRECONDITION(pKey->IsConstructed());
        PRECONDITION(CheckPointer(pTable));
        PRECONDITION(!fCheckUnderLock || CheckPointer(pLock));
        MODE_ANY;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    TypeHandle th;

    if (fCheckUnderLock)
    {
        th = LookupTypeKeyUnderLock(pKey, pTable, pLock);
    }
    else
    {
        th = pTable->GetValue(pKey);
    }
    return th;
}


#ifdef FEATURE_PREJIT
/* static */
TypeHandle ClassLoader::LookupInPreferredZapModule(TypeKey *pKey, BOOL fCheckUnderLock)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        PRECONDITION(CheckPointer(pKey));
        PRECONDITION(pKey->IsConstructed());
        MODE_ANY;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    // First look for an NGEN'd type in the preferred ngen module
    TypeHandle th;
    PTR_Module pPreferredZapModule = Module::ComputePreferredZapModule(pKey);

    if (pPreferredZapModule != NULL && pPreferredZapModule->HasNativeImage())
    {
        th = LookupTypeKey(pKey,
                           pPreferredZapModule->GetAvailableParamTypes(),
                           &pPreferredZapModule->GetClassLoader()->m_AvailableTypesLock,
                           fCheckUnderLock);
    }

    return th;
}
#endif // FEATURE_PREJIT


/* static */
TypeHandle ClassLoader::LookupInLoaderModule(TypeKey *pKey, BOOL fCheckUnderLock)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        PRECONDITION(CheckPointer(pKey));
        PRECONDITION(pKey->IsConstructed());
        MODE_ANY;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    Module *pLoaderModule = ComputeLoaderModule(pKey);
    PREFIX_ASSUME(pLoaderModule!=NULL);

    return LookupTypeKey(pKey,
                         pLoaderModule->GetAvailableParamTypes(),
                         &pLoaderModule->GetClassLoader()->m_AvailableTypesLock,
                         fCheckUnderLock);
}


/* static */
TypeHandle ClassLoader::LookupTypeHandleForTypeKey(TypeKey *pKey)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    // Make an initial lookup without taking any locks.
    TypeHandle th = LookupTypeHandleForTypeKeyInner(pKey, FALSE);

    // A non-null TypeHandle for the above lookup indicates success
    // A null TypeHandle only indicates "well, it might have been there,
    // try again with a lock".  This kind of negative result will
    // only happen while accessing the underlying EETypeHashTable
    // during a resize, i.e. very rarely. In such a case, we just
    // perform the lookup again, but indicate that appropriate locks
    // should be taken.

    if (th.IsNull())
    {
        th = LookupTypeHandleForTypeKeyInner(pKey, TRUE);
    }

    return th;
}
/* static */
TypeHandle ClassLoader::LookupTypeHandleForTypeKeyInner(TypeKey *pKey, BOOL fCheckUnderLock)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        PRECONDITION(CheckPointer(pKey));
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    // Check if it's the typical instantiation.  In this case it's not stored in the same
    // way as other constructed types.
    if (!pKey->IsConstructed() ||
        (pKey->GetKind() == ELEMENT_TYPE_CLASS && ClassLoader::IsTypicalInstantiation(pKey->GetModule(),
                                                                                      pKey->GetTypeToken(),
                                                                                      pKey->GetInstantiation())))
    {
        return TypeHandle(pKey->GetModule()->LookupTypeDef(pKey->GetTypeToken()));
    }

#ifdef FEATURE_PREJIT
    // The following ways of finding a constructed type should be mutually exclusive!
    //  1. Look for a zapped item in the PreferredZapModule
    //  2. Look for a unzapped (JIT-loaded) item in the LoaderModule

    TypeHandle thPZM = LookupInPreferredZapModule(pKey, fCheckUnderLock);
    if (!thPZM.IsNull())
    {
        return thPZM;
    }
#endif // FEATURE_PREJIT

    // Next look in the loader module.  This is where the item is guaranteed to live if
    // it is not latched from an NGEN image, i.e. if it is JIT loaded.
    // If the thing is not NGEN'd then this may
    // be different to pPreferredZapModule.  If they are the same then
    // we can reuse the results of the lookup above.
    TypeHandle thLM = LookupInLoaderModule(pKey, fCheckUnderLock);
    if (!thLM.IsNull())
    {
        return thLM;
    }

    return TypeHandle();
}

// FindClassModuleThrowing discovers which module the type you're looking for is in and loads the Module if necessary.
// Basically, it iterates through all of the assembly's modules until a name match is found in a module's
// AvailableClassHashTable.
//
// The possible outcomes are:
//
//    - Function returns TRUE   - class exists and we successfully found/created the containing Module. See below
//                                for how to deconstruct the results.
//    - Function returns FALSE  - class affirmatively NOT found (that means it doesn't exist as a regular type although
//                                  it could also be a parameterized type)
//    - Function throws         - OOM or some other reason we couldn't do the job (if it's a case-sensitive search
//                                  and you're looking for already loaded type or you've set the TokenNotToLoad.
//                                  we are guaranteed not to find a reason to throw.)
//
//
// If it succeeds (returns TRUE), one of the following will occur. Check (*pType)->IsNull() to discriminate.
//
//     1. *pType: set to the null TypeHandle()
//        *ppModule: set to the owning Module
//        *pmdClassToken: set to the typedef
//        *pmdFoundExportedType: if this name bound to an ExportedType, this contains the mdtExportedType token (otherwise,
//                               it's set to mdTokenNil.) You need this because in this case, *pmdClassToken is just
//                               a best guess and you need to verify it. (The division of labor between this
//                               and LoadTypeHandle could definitely be better!)
//
//     2. *pType: set to non-null TypeHandle()
//        This means someone else had already done this same lookup before you and caused the actual
//        TypeHandle to be cached. Since we know that's what you *really* wanted, we'll just forget the
//        Module/typedef stuff and give you the actual TypeHandle.
//
//
BOOL ClassLoader::FindClassModuleThrowing(
    const NameHandle *    pName,
    TypeHandle *          pType,
    mdToken *             pmdClassToken,
    Module **             ppModule,
    mdToken *             pmdFoundExportedType,
    HashedTypeEntry *     pFoundEntry,
    Module *              pLookInThisModuleOnly,
    Loader::LoadFlag      loadFlag)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        PRECONDITION(CheckPointer(pName));
        PRECONDITION(CheckPointer(ppModule));
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    // Note that the type name is expected to be lower-cased by the caller for case-insensitive lookups

    NameHandleTable nhTable = pName->GetTable();

    // Remember if there are any unhashed modules.  We must do this before
    // the actual look to avoid a race condition with other threads doing lookups.
#ifdef LOGGING
    BOOL incomplete = (m_cUnhashedModules > 0);
#endif

    HashDatum Data;
    EEClassHashTable * pTable = NULL;
    HashedTypeEntry foundEntry;
    BOOL needsToBuildHashtable;
    GetClassValue(nhTable, pName, &Data, &pTable, pLookInThisModuleOnly, &foundEntry, loadFlag, needsToBuildHashtable);

    // In the case of R2R modules, the search is only performed in the hashtable saved in the
    // R2R image, and this is why we return (whether we found a valid typedef token or not).
    // Note: case insensitive searches are not used/supported in R2R images.
    if (foundEntry.GetEntryType() == HashedTypeEntry::EntryType::IsHashedTokenEntry)
    {
        *pType = TypeHandle();
        HashedTypeEntry::TokenTypeEntry tokenAndModulePair = foundEntry.GetTokenBasedEntryValue();
        switch (TypeFromToken(tokenAndModulePair.m_TypeToken))
        {
        case mdtTypeDef:
            *pmdClassToken = tokenAndModulePair.m_TypeToken;
            *pmdFoundExportedType = mdTokenNil;
            break;
        case mdtExportedType:
            *pmdClassToken = mdTokenNil;
            *pmdFoundExportedType = tokenAndModulePair.m_TypeToken;
            break;
        default:
            _ASSERT(false);
            return FALSE;
        }
        *ppModule = tokenAndModulePair.m_pModule;
        if (pFoundEntry != NULL)
            *pFoundEntry = foundEntry;

        return TRUE;
    }

    EEClassHashEntry_t * pBucket = foundEntry.GetClassHashBasedEntryValue();

    if (pBucket == NULL)
    {
        AvailableClasses_LockHolder lh(this);

        // Try again with the lock.  This will protect against another thread reallocating
        // the hash table underneath us
        GetClassValue(nhTable, pName, &Data, &pTable, pLookInThisModuleOnly, &foundEntry, loadFlag, needsToBuildHashtable);
        pBucket = foundEntry.GetClassHashBasedEntryValue();

#ifndef DACCESS_COMPILE
        if (needsToBuildHashtable && (pBucket == NULL) && (m_cUnhashedModules > 0))
        {
            _ASSERT(needsToBuildHashtable);

            if (nhTable == nhCaseInsensitive)
            {
                LazyPopulateCaseInsensitiveHashTables();
            }
            else
            {
                // Note: This codepath is only valid for R2R scenarios
                LazyPopulateCaseSensitiveHashTables();
            }

            // Try yet again with the new classes added
            GetClassValue(nhTable, pName, &Data, &pTable, pLookInThisModuleOnly, &foundEntry, loadFlag, needsToBuildHashtable);
            pBucket = foundEntry.GetClassHashBasedEntryValue();
            _ASSERT(!needsToBuildHashtable);
        }
#endif
    }

    // Same check as above, but this time we've checked with the lock so the table will be populated
    if (pBucket == NULL)
    {
#if defined(_DEBUG_IMPL) && !defined(DACCESS_COMPILE)
        LPCUTF8 szName = pName->GetName();
        if (szName == NULL)
            szName = "<UNKNOWN>";
        LOG((LF_CLASSLOADER, LL_INFO10, "Failed to find type \"%s\", assembly \"%ws\" in hash table. Incomplete = %d\n",
            szName, GetAssembly()->GetDebugName(), incomplete));
#endif
        return FALSE;
    }

    if (pName->GetTable() == nhCaseInsensitive)
    {
        _ASSERTE(Data);
        pBucket = PTR_EEClassHashEntry(Data);
        Data = pBucket->GetData();
    }

    // Lower bit is a discriminator.  If the lower bit is NOT SET, it means we have
    // a TypeHandle. Otherwise, we have a Module/CL.
    if ((dac_cast<TADDR>(Data) & EECLASSHASH_TYPEHANDLE_DISCR) == 0)
    {
        TypeHandle t = TypeHandle::FromPtr(Data);
        _ASSERTE(!t.IsNull());

        *pType = t;
        if (pFoundEntry != NULL)
        {
            pFoundEntry->SetClassHashBasedEntryValue(pBucket);
        }
        return TRUE;
    }

    // We have a Module/CL
    if (!pTable->UncompressModuleAndClassDef(Data,
                                             loadFlag,
                                             ppModule,
                                             pmdClassToken,
                                             pmdFoundExportedType))
    {
        _ASSERTE(loadFlag != Loader::Load);
        return FALSE;
    }

    *pType = TypeHandle();
    if (pFoundEntry != NULL)
    {
        pFoundEntry->SetClassHashBasedEntryValue(pBucket);
    }
    return TRUE;
} // ClassLoader::FindClassModuleThrowing

#ifndef DACCESS_COMPILE
// Returns true if the full name (namespace+name) of pName matches that
// of typeHnd; otherwise false. Because this is nothrow, it will default
// to false for all exceptions (such as OOM).
bool CompareNameHandleWithTypeHandleNoThrow(
    const NameHandle * pName,
    TypeHandle         typeHnd)
{
    bool fRet = false;

    EX_TRY
    {
        // This block is specifically designed to handle transient faults such
        // as OOM exceptions.
        CONTRACT_VIOLATION(FaultViolation | ThrowsViolation);
        StackSString ssBuiltName;
        ns::MakePath(ssBuiltName,
                     StackSString(SString::Utf8, pName->GetNameSpace()),
                     StackSString(SString::Utf8, pName->GetName()));
        StackSString ssName;
        typeHnd.GetName(ssName);
        fRet = ssName.Equals(ssBuiltName) == TRUE;
    }
    EX_CATCH
    {
        // Technically, the above operations should never result in a non-OOM
        // exception, but we'll put the rethrow line in there just in case.
        CONSISTENCY_CHECK(!GET_EXCEPTION()->IsTerminal());
        RethrowTerminalExceptions;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return fRet;
}
#endif // #ifndef DACCESS_COMPILE

// 1024 seems like a good bet at detecting a loop in the type forwarding.
static const UINT32 const_cMaxTypeForwardingChainSize = 1024;

// Does not throw an exception if the type was not found.  Use LoadTypeHandleThrowIfFailed()
// instead if you need that.
//
// Returns:
//  pName->m_pBucket
//    Will be set to the 'final' TypeDef bucket if pName->GetTokenType() is mdtBaseType.
//
TypeHandle
ClassLoader::LoadTypeHandleThrowing(
    NameHandle * pName,
    ClassLoadLevel level,
    Module *       pLookInThisModuleOnly /*=NULL*/)
{
    CONTRACT(TypeHandle) {
        INSTANCE_CHECK;
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        DAC_LOADS_TYPE(level, !pName->OKToLoad());
        PRECONDITION(level > CLASS_LOAD_BEGIN && level <= CLASS_LOADED);
        PRECONDITION(CheckPointer(pName));
        POSTCONDITION(RETVAL.IsNull() || RETVAL.GetLoadLevel() >= level);
        MODE_ANY;
        SUPPORTS_DAC;
    } CONTRACT_END

    TypeHandle typeHnd;
    Module * pFoundModule = NULL;
    mdToken FoundCl;
    HashedTypeEntry foundEntry;
    mdExportedType FoundExportedType = mdTokenNil;

    UINT32 cLoopIterations = 0;

    ClassLoader * pClsLdr = this;

    while (true)
    {
        if (cLoopIterations++ >= const_cMaxTypeForwardingChainSize)
        {   // If we've looped too many times due to type forwarding, return null TypeHandle
            // Would prefer to return a format exception, but the original behaviour
            // was to detect a stack overflow possibility and return a null, and
            // so we need to maintain this.
            typeHnd = TypeHandle();
            break;
        }

        // Look outside the lock (though we're actually still a long way from the
        // lock at this point...).  This may discover that the type is actually
        // defined in another module...

        if (!pClsLdr->FindClassModuleThrowing(
                pName,
                &typeHnd,
                &FoundCl,
                &pFoundModule,
                &FoundExportedType,
                &foundEntry,
                pLookInThisModuleOnly,
                pName->OKToLoad() ? Loader::Load
                                  : Loader::DontLoad))
        {   // Didn't find anything, no point looping indefinitely
            break;
        }
        _ASSERTE(!foundEntry.IsNull());

        if (pName->GetTypeToken() == mdtBaseType)
        {   // We should return the found bucket in the pName
            pName->SetBucket(foundEntry);
        }

        if (!typeHnd.IsNull())
        {   // Found the cached value, or a constructedtype
            if (typeHnd.GetLoadLevel() < level)
            {
                typeHnd = pClsLdr->LoadTypeDefThrowing(
                    typeHnd.GetModule(),
                    typeHnd.GetCl(),
                    ClassLoader::ReturnNullIfNotFound,
                    ClassLoader::PermitUninstDefOrRef, // When loading by name we always permit naked type defs/refs
                    pName->GetTokenNotToLoad(),
                    level);
            }
            break;
        }

        // Found a cl, pModule pair

        // If the found module's class loader is not the same as the current class loader,
        // then this is a forwarded type and we want to do something else (see
        // code:#LoadTypeHandle_TypeForwarded).
        if (pFoundModule->GetClassLoader() == pClsLdr)
        {
            BOOL fTrustTD = TRUE;
#ifndef DACCESS_COMPILE
            CONTRACT_VIOLATION(ThrowsViolation);
            BOOL fVerifyTD = FALSE;

            // If this is an exported type with a mdTokenNil class token, then then
            // exported type did not give a typedefID hint. We won't be able to trust the typedef
            // here.
            if ((FoundExportedType != mdTokenNil) && (FoundCl == mdTokenNil))
            {
                fVerifyTD = TRUE;
                fTrustTD = FALSE;
            }
            // verify that FoundCl is a valid token for pFoundModule, because
            // it may be just the hint saved in an ExportedType in another scope
            else if (fVerifyTD)
            {
                fTrustTD = pFoundModule->GetMDImport()->IsValidToken(FoundCl);
            }
#endif // #ifndef DACCESS_COMPILE

            if (fTrustTD)
            {
                typeHnd = pClsLdr->LoadTypeDefThrowing(
                    pFoundModule,
                    FoundCl,
                    ClassLoader::ReturnNullIfNotFound,
                    ClassLoader::PermitUninstDefOrRef, // when loading by name we always permit naked type defs/refs
                    pName->GetTokenNotToLoad(),
                    level);
            }
#ifndef DACCESS_COMPILE
            // If we used a TypeDef saved in a ExportedType, if we didn't verify
            // the hash for this internal module, don't trust the TD value.
            if (fVerifyTD)
            {
                if (typeHnd.IsNull() || !CompareNameHandleWithTypeHandleNoThrow(pName, typeHnd))
                {
                    if (SUCCEEDED(pClsLdr->FindTypeDefByExportedType(
                            pClsLdr->GetAssembly()->GetManifestImport(),
                            FoundExportedType,
                            pFoundModule->GetMDImport(),
                            &FoundCl)))
                    {
                        typeHnd = pClsLdr->LoadTypeDefThrowing(
                            pFoundModule,
                            FoundCl,
                            ClassLoader::ReturnNullIfNotFound,
                            ClassLoader::PermitUninstDefOrRef,
                            pName->GetTokenNotToLoad(),
                            level);
                    }
                    else
                    {
                        typeHnd = TypeHandle();
                    }
                }
            }
#endif // #ifndef DACCESS_COMPILE
            break;
        }
        else
        {   //#LoadTypeHandle_TypeForwarded
            // pName is a host instance so it's okay to set fields in it in a DAC build
            const HashedTypeEntry& bucket = pName->GetBucket();

            // Reset pName's bucket entry
            if (bucket.GetEntryType() == HashedTypeEntry::IsHashedClassEntry && bucket.GetClassHashBasedEntryValue()->GetEncloser())
            {
                // We will be searching for the type name again, so set the nesting/context type to the
                // encloser of just found type
                pName->SetBucket(HashedTypeEntry().SetClassHashBasedEntryValue(bucket.GetClassHashBasedEntryValue()->GetEncloser()));
            }
            else
            {
                pName->SetBucket(HashedTypeEntry());
            }

            // Update the class loader for the new module/token pair.
            pClsLdr = pFoundModule->GetClassLoader();
            pLookInThisModuleOnly = NULL;
        }

#ifndef DACCESS_COMPILE
        // Replace AvailableClasses Module entry with found TypeHandle
        if (!typeHnd.IsNull() &&
            typeHnd.IsRestored() &&
            foundEntry.GetEntryType() == HashedTypeEntry::EntryType::IsHashedClassEntry &&
            (foundEntry.GetClassHashBasedEntryValue() != NULL) &&
            (foundEntry.GetClassHashBasedEntryValue()->GetData() != typeHnd.AsPtr()))
        {
            foundEntry.GetClassHashBasedEntryValue()->SetData(typeHnd.AsPtr());
        }
#endif // !DACCESS_COMPILE
    }

    RETURN typeHnd;
} // ClassLoader::LoadTypeHandleThrowing

/* static */
TypeHandle ClassLoader::LoadPointerOrByrefTypeThrowing(CorElementType typ,
                                                       TypeHandle baseType,
                                                       LoadTypesFlag fLoadTypes/*=LoadTypes*/,
                                                       ClassLoadLevel level/*=CLASS_LOADED*/)
{
    CONTRACT(TypeHandle)
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        if (FORBIDGC_LOADER_USE_ENABLED() || fLoadTypes != LoadTypes) { LOADS_TYPE(CLASS_LOAD_BEGIN); } else { LOADS_TYPE(level); }
        MODE_ANY;
        PRECONDITION(CheckPointer(baseType));
        PRECONDITION(typ == ELEMENT_TYPE_BYREF || typ == ELEMENT_TYPE_PTR);
        PRECONDITION(level > CLASS_LOAD_BEGIN && level <= CLASS_LOADED);
        POSTCONDITION(CheckPointer(RETVAL, ((fLoadTypes == LoadTypes) ? NULL_NOT_OK : NULL_OK)));
        SUPPORTS_DAC;
    }
    CONTRACT_END

    TypeKey key(typ, baseType);
    RETURN(LoadConstructedTypeThrowing(&key, fLoadTypes, level));
}

/* static */
TypeHandle ClassLoader::LoadNativeValueTypeThrowing(TypeHandle baseType,
                                                    LoadTypesFlag fLoadTypes/*=LoadTypes*/,
                                                    ClassLoadLevel level/*=CLASS_LOADED*/)
{
    CONTRACT(TypeHandle)
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        MODE_ANY;
        PRECONDITION(CheckPointer(baseType));
        PRECONDITION(baseType.AsMethodTable()->IsValueType());
        PRECONDITION(level > CLASS_LOAD_BEGIN && level <= CLASS_LOADED);
        POSTCONDITION(CheckPointer(RETVAL, ((fLoadTypes == LoadTypes) ? NULL_NOT_OK : NULL_OK)));
    }
    CONTRACT_END

    TypeKey key(ELEMENT_TYPE_VALUETYPE, baseType);
    RETURN(LoadConstructedTypeThrowing(&key, fLoadTypes, level));
}

/* static */
TypeHandle ClassLoader::LoadFnptrTypeThrowing(BYTE callConv,
                                              DWORD ntypars,
                                              TypeHandle* inst,
                                              LoadTypesFlag fLoadTypes/*=LoadTypes*/,
                                              ClassLoadLevel level/*=CLASS_LOADED*/)
{
    CONTRACT(TypeHandle)
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        if (FORBIDGC_LOADER_USE_ENABLED() || fLoadTypes != LoadTypes) { LOADS_TYPE(CLASS_LOAD_BEGIN); } else { LOADS_TYPE(level); }
        PRECONDITION(level > CLASS_LOAD_BEGIN && level <= CLASS_LOADED);
        POSTCONDITION(CheckPointer(RETVAL, ((fLoadTypes == LoadTypes) ? NULL_NOT_OK : NULL_OK)));
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACT_END

    TypeKey key(callConv, ntypars, inst);
    RETURN(LoadConstructedTypeThrowing(&key, fLoadTypes, level));
}

// Find an instantiation of a generic type if it has already been created.
// If typeDef is not a generic type or is already instantiated then throw an exception.
// If its arity does not match ntypars then throw an exception.
// Value will be non-null if we're loading types.
/* static */
TypeHandle ClassLoader::LoadGenericInstantiationThrowing(Module *pModule,
                                                         mdTypeDef typeDef,
                                                         Instantiation inst,
                                                         LoadTypesFlag fLoadTypes/*=LoadTypes*/,
                                                         ClassLoadLevel level/*=CLASS_LOADED*/,
                                                         const InstantiationContext *pInstContext/*=NULL*/,
                                                         BOOL fFromNativeImage /*=FALSE*/)
{
    // This can be called in FORBIDGC_LOADER_USE mode by the debugger to find
    // a particular generic type instance that is already loaded.
    CONTRACT(TypeHandle)
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED() || fLoadTypes != LoadTypes) { LOADS_TYPE(CLASS_LOAD_BEGIN); } else { LOADS_TYPE(level); }
        PRECONDITION(CheckPointer(pModule));
        MODE_ANY;
        PRECONDITION(level > CLASS_LOAD_BEGIN && level <= CLASS_LOADED);
        PRECONDITION(CheckPointer(pInstContext, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL, ((fLoadTypes == LoadTypes) ? NULL_NOT_OK : NULL_OK)));
        SUPPORTS_DAC;
    }
    CONTRACT_END

    // Essentially all checks to determine if a generic instantiation of a type
    // is well-formed go in this method, i.e. this is the
    // "choke" point through which all attempts
    // to create an instantiation flow.  There is a similar choke point for generic
    // methods in genmeth.cpp.

    if (inst.IsEmpty() || ClassLoader::IsTypicalInstantiation(pModule, typeDef, inst))
    {
        TypeHandle th = LoadTypeDefThrowing(pModule, typeDef,
                                            ThrowIfNotFound,
                                            PermitUninstDefOrRef,
                                            fLoadTypes == DontLoadTypes ? tdAllTypes : tdNoTypes,
                                            level,
                                            fFromNativeImage ? NULL : &inst);
        _ASSERTE(th.GetNumGenericArgs() == inst.GetNumArgs());
        RETURN th;
    }

    if (!fFromNativeImage)
    {
        TypeHandle th = ClassLoader::LoadTypeDefThrowing(pModule, typeDef,
                                         ThrowIfNotFound,
                                         PermitUninstDefOrRef,
                                         fLoadTypes == DontLoadTypes ? tdAllTypes : tdNoTypes,
                                         level,
                                         fFromNativeImage ? NULL : &inst);
        _ASSERTE(th.GetNumGenericArgs() == inst.GetNumArgs());
    }

    TypeKey key(pModule, typeDef, inst);

#ifndef DACCESS_COMPILE
    // To avoid loading useless shared instantiations, normalize shared instantiations to the canonical form
    // (e.g. Dictionary<String,_Canon> -> Dictionary<_Canon,_Canon>)
    // The denormalized shared instantiations should be needed only during JITing, so it is fine to skip this
    // for DACCESS_COMPILE.
    if (TypeHandle::IsCanonicalSubtypeInstantiation(inst) && !IsCanonicalGenericInstantiation(inst))
    {
        RETURN(ClassLoader::LoadCanonicalGenericInstantiation(&key, fLoadTypes, level));
    }
#endif

    RETURN(LoadConstructedTypeThrowing(&key, fLoadTypes, level, pInstContext));
}

//   For non-nested classes, gets the ExportedType name and finds the corresponding
// TypeDef.
//   For nested classes, gets the name of the ExportedType and its encloser.
// Recursively gets and keeps the name for each encloser until we have the top
// level one.  Gets the TypeDef token for that.  Then, returns from the
// recursion, using the last found TypeDef token in order to find the
// next nested level down TypeDef token.  Finally, returns the TypeDef
// token for the type we care about.
/*static*/
HRESULT ClassLoader::FindTypeDefByExportedType(IMDInternalImport *pCTImport, mdExportedType mdCurrent,
                                               IMDInternalImport *pTDImport, mdTypeDef *mtd)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END

    mdToken mdImpl;
    LPCSTR szcNameSpace;
    LPCSTR szcName;
    HRESULT hr;

    IfFailRet(pCTImport->GetExportedTypeProps(
        mdCurrent,
        &szcNameSpace,
        &szcName,
        &mdImpl,
        NULL, //binding
        NULL)); //flags

    if ((TypeFromToken(mdImpl) == mdtExportedType) &&
        (mdImpl != mdExportedTypeNil)) {
        // mdCurrent is a nested ExportedType
        IfFailRet(FindTypeDefByExportedType(pCTImport, mdImpl, pTDImport, mtd));

        // Get TypeDef token for this nested type
        return pTDImport->FindTypeDef(szcNameSpace, szcName, *mtd, mtd);
    }

    // Get TypeDef token for this top-level type
    return pTDImport->FindTypeDef(szcNameSpace, szcName, mdTokenNil, mtd);
}

#ifndef DACCESS_COMPILE

VOID ClassLoader::CreateCanonicallyCasedKey(LPCUTF8 pszNameSpace, LPCUTF8 pszName, __out LPUTF8 *ppszOutNameSpace, __out LPUTF8 *ppszOutName)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
        MODE_ANY;
    }
    CONTRACTL_END

    StackSString nameSpace(SString::Utf8, pszNameSpace);
    nameSpace.LowerCase();

    StackScratchBuffer nameSpaceBuffer;
    pszNameSpace = nameSpace.GetUTF8(nameSpaceBuffer);


    StackSString name(SString::Utf8, pszName);
    name.LowerCase();

    StackScratchBuffer nameBuffer;
    pszName = name.GetUTF8(nameBuffer);


   size_t iNSLength = strlen(pszNameSpace);
   size_t iNameLength = strlen(pszName);

    //Calc & allocate path length
    //Includes terminating null
    S_SIZE_T allocSize = S_SIZE_T(iNSLength) + S_SIZE_T(iNameLength) + S_SIZE_T(2);
    AllocMemHolder<char> alloc(GetAssembly()->GetHighFrequencyHeap()->AllocMem(allocSize));

    memcpy(*ppszOutNameSpace = (char*)alloc, pszNameSpace, iNSLength + 1);
    memcpy(*ppszOutName = (char*)alloc + iNSLength + 1, pszName, iNameLength + 1);

    alloc.SuppressRelease();
}

#endif // #ifndef DACCESS_COMPILE


//
// Return a class that is already loaded
// Only for type refs and type defs (not type specs)
//
/*static*/
TypeHandle ClassLoader::LookupTypeDefOrRefInModule(Module *pModule, mdToken cl, ClassLoadLevel *pLoadLevel)
{
    CONTRACT(TypeHandle)
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        MODE_ANY;
        PRECONDITION(CheckPointer(pModule));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        SUPPORTS_DAC;
    }
    CONTRACT_END

    BAD_FORMAT_NOTHROW_ASSERT((TypeFromToken(cl) == mdtTypeRef ||
                       TypeFromToken(cl) == mdtTypeDef ||
                       TypeFromToken(cl) == mdtTypeSpec));

    TypeHandle typeHandle;

    if (TypeFromToken(cl) == mdtTypeDef)
        typeHandle = pModule->LookupTypeDef(cl, pLoadLevel);
    else if (TypeFromToken(cl) == mdtTypeRef)
    {
        typeHandle = pModule->LookupTypeRef(cl);

        if (pLoadLevel && !typeHandle.IsNull())
        {
            *pLoadLevel = typeHandle.GetLoadLevel();
        }
    }

    RETURN(typeHandle);
}

DomainAssembly *ClassLoader::GetDomainAssembly()
{
    WRAPPER_NO_CONTRACT;
    return GetAssembly()->GetDomainAssembly();
}

#ifndef DACCESS_COMPILE

//
// Free all modules associated with this loader
//
void ClassLoader::FreeModules()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        DISABLED(FORBID_FAULT);  //Lots of crud to clean up to make this work
    }
    CONTRACTL_END;

    Module *pManifest = NULL;
    if (GetAssembly() && (NULL != (pManifest = GetAssembly()->GetManifestModule()))) {
        // Unload the manifest last, since it contains the module list in its rid map
        ModuleIterator i = GetAssembly()->IterateModules();
        while (i.Next()) {
            // Have the module free its various tables and some of the EEClass links
            if (i.GetModule() != pManifest)
                i.GetModule()->Destruct();
        }

        // Now do the manifest module.
        pManifest->Destruct();
    }

}

ClassLoader::~ClassLoader()
{
    CONTRACTL
    {
        NOTHROW;
        DESTRUCTOR_CHECK;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        DISABLED(FORBID_FAULT);  //Lots of crud to clean up to make this work
    }
    CONTRACTL_END

#ifdef _DEBUG
    // Do not walk m_pUnresolvedClassHash at destruct time as it is loaderheap allocated memory
    // and may already have been deallocated via an AllocMemTracker.
    m_pUnresolvedClassHash = (PendingTypeLoadTable*)(UINT_PTR)0xcccccccc;
#endif

#ifdef _DEBUG
//     LOG((
//         LF_CLASSLOADER,
//         INFO3,
//         "Deleting classloader %x\n"
//         "  >EEClass data:     %10d bytes\n"
//         "  >Classname hash:   %10d bytes\n"
//         "  >FieldDesc data:   %10d bytes\n"
//         "  >MethodDesc data:  %10d bytes\n"
//         "  >GCInfo:           %10d bytes\n"
//         "  >Interface maps:   %10d bytes\n"
//         "  >MethodTables:     %10d bytes\n"
//         "  >Vtables:          %10d bytes\n"
//         "  >Static fields:    %10d bytes\n"
//         "# methods:           %10d\n"
//         "# field descs:       %10d\n"
//         "# classes:           %10d\n"
//         "# dup intf slots:    %10d\n"
//         "# array classrefs:   %10d\n"
//         "Array class overhead:%10d bytes\n",
//         this,
//             m_dwEEClassData,
//             m_pAvailableClasses->m_dwDebugMemory,
//             m_dwFieldDescData,
//             m_dwMethodDescData,
//             m_dwGCSize,
//             m_dwInterfaceMapSize,
//             m_dwMethodTableSize,
//             m_dwVtableData,
//             m_dwStaticFieldData,
//         m_dwDebugMethods,
//         m_dwDebugFieldDescs,
//         m_dwDebugClasses,
//         m_dwDebugDuplicateInterfaceSlots,
//     ));
#endif

    FreeModules();

    m_UnresolvedClassLock.Destroy();
    m_AvailableClassLock.Destroy();
    m_AvailableTypesLock.Destroy();
}


//----------------------------------------------------------------------------
// The constructor should only initialize enough to ensure that the destructor doesn't
// crash. It cannot allocate or do anything that might fail as that would leave
// the ClassLoader undestructable. Any such tasks should be done in ClassLoader::Init().
//----------------------------------------------------------------------------
ClassLoader::ClassLoader(Assembly *pAssembly)
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END

    m_pAssembly = pAssembly;

    m_pUnresolvedClassHash          = NULL;
    m_cUnhashedModules              = 0;

#ifdef _DEBUG
    m_dwDebugMethods        = 0;
    m_dwDebugFieldDescs     = 0;
    m_dwDebugClasses        = 0;
    m_dwDebugDuplicateInterfaceSlots = 0;
    m_dwGCSize              = 0;
    m_dwInterfaceMapSize    = 0;
    m_dwMethodTableSize     = 0;
    m_dwVtableData          = 0;
    m_dwStaticFieldData     = 0;
    m_dwFieldDescData       = 0;
    m_dwMethodDescData      = 0;
    m_dwEEClassData         = 0;
#endif
}


//----------------------------------------------------------------------------
// This function completes the initialization of the ClassLoader. It can
// assume the constructor is run and that the function is entered with
// ClassLoader in a safely destructable state. This function can throw
// but whether it throws or succeeds, it must leave the ClassLoader in a safely
// destructable state.
//----------------------------------------------------------------------------
VOID ClassLoader::Init(AllocMemTracker *pamTracker)
{
    STANDARD_VM_CONTRACT;

    m_pUnresolvedClassHash = PendingTypeLoadTable::Create(GetAssembly()->GetLowFrequencyHeap(),
                                                          UNRESOLVED_CLASS_HASH_BUCKETS,
                                                          pamTracker);

    m_UnresolvedClassLock.Init(CrstUnresolvedClassLock);

    // This lock is taken within the classloader whenever we have to enter a
    // type in one of the modules governed by the loader.
    // The process of creating these types may be reentrant.  The ordering has
    // not yet been sorted out, and when we sort it out we should also modify the
    // ordering for m_AvailableTypesLock in BaseDomain.
    m_AvailableClassLock.Init(
                             CrstAvailableClass,
                             CRST_REENTRANCY);

    // This lock is taken within the classloader whenever we have to insert a new param. type into the table
    // This lock also needs to be taken for a read operation in a GC_NOTRIGGER scope, thus the ANYMODE flag.
    m_AvailableTypesLock.Init(
                                  CrstAvailableParamTypes,
                                  (CrstFlags)(CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD));

#ifdef _DEBUG
    CorTypeInfo::CheckConsistency();
#endif

}

#endif // #ifndef DACCESS_COMPILE

/*static*/
TypeHandle ClassLoader::LoadTypeDefOrRefOrSpecThrowing(Module *pModule,
                                                       mdToken typeDefOrRefOrSpec,
                                                       const SigTypeContext *pTypeContext,
                                                       NotFoundAction fNotFoundAction /* = ThrowIfNotFound */ ,
                                                       PermitUninstantiatedFlag fUninstantiated /* = FailIfUninstDefOrRef */,
                                                       LoadTypesFlag fLoadTypes/*=LoadTypes*/ ,
                                                       ClassLoadLevel level /* = CLASS_LOADED */,
                                                       BOOL dropGenericArgumentLevel /* = FALSE */,
                                                       const Substitution *pSubst)
{
    CONTRACT(TypeHandle)
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        MODE_ANY;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        if (FORBIDGC_LOADER_USE_ENABLED() || fLoadTypes != LoadTypes) { LOADS_TYPE(CLASS_LOAD_BEGIN); } else { LOADS_TYPE(level); }
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(level > CLASS_LOAD_BEGIN && level <= CLASS_LOADED);
        PRECONDITION(FORBIDGC_LOADER_USE_ENABLED() || GetAppDomain()->CheckCanLoadTypes(pModule->GetAssembly()));
        POSTCONDITION(CheckPointer(RETVAL, (fNotFoundAction == ThrowIfNotFound)? NULL_NOT_OK : NULL_OK));
    }
    CONTRACT_END

    if (TypeFromToken(typeDefOrRefOrSpec) == mdtTypeSpec)
    {
        ULONG cSig;
        PCCOR_SIGNATURE pSig;

        IMDInternalImport *pInternalImport = pModule->GetMDImport();
        if (FAILED(pInternalImport->GetTypeSpecFromToken(typeDefOrRefOrSpec, &pSig, &cSig)))
        {
#ifndef DACCESS_COMPILE
            if (fNotFoundAction == ThrowIfNotFound)
            {
                pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport, typeDefOrRefOrSpec, IDS_CLASSLOAD_BADFORMAT);
            }
#endif //!DACCESS_COMPILE
            RETURN (TypeHandle());
        }
        SigPointer sigptr(pSig, cSig);
        TypeHandle typeHnd = sigptr.GetTypeHandleThrowing(pModule, pTypeContext, fLoadTypes,
                                                          level, dropGenericArgumentLevel, pSubst);
#ifndef DACCESS_COMPILE
        if ((fNotFoundAction == ThrowIfNotFound) && typeHnd.IsNull())
            pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport, typeDefOrRefOrSpec,
                                                           IDS_CLASSLOAD_GENERAL);
#endif
        RETURN (typeHnd);
    }
    else
    {
        RETURN (LoadTypeDefOrRefThrowing(pModule, typeDefOrRefOrSpec,
                                         fNotFoundAction,
                                         fUninstantiated,
                                         ((fLoadTypes == LoadTypes) ? tdNoTypes : tdAllTypes),
                                         level));
    }
} // ClassLoader::LoadTypeDefOrRefOrSpecThrowing

// Given a token specifying a typeDef, and a module in which to
// interpret that token, find or load the corresponding type handle.
//
//
/*static*/
TypeHandle ClassLoader::LoadTypeDefThrowing(Module *pModule,
                                            mdToken typeDef,
                                            NotFoundAction fNotFoundAction /* = ThrowIfNotFound */ ,
                                            PermitUninstantiatedFlag fUninstantiated /* = FailIfUninstDefOrRef */,
                                            mdToken tokenNotToLoad,
                                            ClassLoadLevel level,
                                            Instantiation * pTargetInstantiation)
{

    CONTRACT(TypeHandle)
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        MODE_ANY;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        DAC_LOADS_TYPE(level, !NameHandle::OKToLoad(typeDef, tokenNotToLoad));
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(level > CLASS_LOAD_BEGIN && level <= CLASS_LOADED);
        PRECONDITION(FORBIDGC_LOADER_USE_ENABLED()
                     || GetAppDomain()->CheckCanLoadTypes(pModule->GetAssembly()));

        POSTCONDITION(CheckPointer(RETVAL, NameHandle::OKToLoad(typeDef, tokenNotToLoad) && (fNotFoundAction == ThrowIfNotFound) ? NULL_NOT_OK : NULL_OK));
        POSTCONDITION(RETVAL.IsNull() || RETVAL.GetCl() == typeDef);
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    TypeHandle typeHnd;

    // First, attempt to find the class if it is already loaded
    ClassLoadLevel existingLoadLevel = CLASS_LOAD_BEGIN;
    typeHnd = pModule->LookupTypeDef(typeDef, &existingLoadLevel);
    if (!typeHnd.IsNull())
    {
#ifndef DACCESS_COMPILE
        // If the type is loaded, we can do cheap arity verification
        if (pTargetInstantiation != NULL && pTargetInstantiation->GetNumArgs() != typeHnd.AsMethodTable()->GetNumGenericArgs())
            pModule->GetAssembly()->ThrowTypeLoadException(pModule->GetMDImport(), typeDef, IDS_CLASSLOAD_TYPEWRONGNUMGENERICARGS);
#endif

        if (existingLoadLevel >= level)
            RETURN(typeHnd);
    }

    IMDInternalImport *pInternalImport = pModule->GetMDImport();

#ifndef DACCESS_COMPILE
    if (typeHnd.IsNull() && pTargetInstantiation != NULL)
    {
        // If the type is not loaded yet, we have to do heavy weight arity verification based on metadata
        HENUMInternal hEnumGenericPars;
        HRESULT hr = pInternalImport->EnumInit(mdtGenericParam, typeDef, &hEnumGenericPars);
        if (FAILED(hr))
            pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport, typeDef, IDS_CLASSLOAD_BADFORMAT);
        DWORD nGenericClassParams = pInternalImport->EnumGetCount(&hEnumGenericPars);
        pInternalImport->EnumClose(&hEnumGenericPars);

        if (pTargetInstantiation->GetNumArgs() != nGenericClassParams)
            pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport, typeDef, IDS_CLASSLOAD_TYPEWRONGNUMGENERICARGS);
    }
#endif

    if (IsNilToken(typeDef) || TypeFromToken(typeDef) != mdtTypeDef || !pInternalImport->IsValidToken(typeDef) )
    {
        LOG((LF_CLASSLOADER, LL_INFO10, "Bogus class token to load: 0x%08x\n", typeDef));
        typeHnd = TypeHandle();
    }
    else
    {
        // *****************************************************************************
        //
        //             Important invariant:
        //
        // The rule here is that we never go to LoadTypeHandleForTypeKey if a Find should succeed.
        // This is vital, because otherwise a stack crawl will open up opportunities for
        // GC.  Since operations like setting up a GCFrame will trigger a crawl in stress
        // mode, a GC at that point would be disastrous.  We can't assert this, because
        // of race conditions.  (In other words, the type could suddently be find-able
        // because another thread loaded it while we were in this method.

        // Not found - try to load it unless we are told not to

#ifndef DACCESS_COMPILE
        if ( !NameHandle::OKToLoad(typeDef, tokenNotToLoad) )
        {
            typeHnd = TypeHandle();
        }
        else
        {
            // Anybody who puts himself in a FORBIDGC_LOADER state has promised
            // to use us only for resolving, not loading. We are now transitioning into
            // loading.
#ifdef _DEBUG_IMPL
            _ASSERTE(!FORBIDGC_LOADER_USE_ENABLED());
#endif
            TRIGGERSGC();

            if (pModule->IsReflection())
            {
                // Don't try to load types that are not in available table, when this
                // is an in-memory module.  Raise the type-resolve event instead.
                typeHnd = TypeHandle();

                // Avoid infinite recursion
                if (tokenNotToLoad != tdAllAssemblies)
                {
                    AppDomain* pDomain = SystemDomain::GetCurrentDomain();

                    LPUTF8 pszFullName;
                    LPCUTF8 className;
                    LPCUTF8 nameSpace;
                    if (FAILED(pInternalImport->GetNameOfTypeDef(typeDef, &className, &nameSpace)))
                    {
                        LOG((LF_CLASSLOADER, LL_INFO10, "Bogus TypeDef record while loading: 0x%08x\n", typeDef));
                        typeHnd = TypeHandle();
                    }
                    else
                    {
                        MAKE_FULL_PATH_ON_STACK_UTF8(pszFullName,
                                                        nameSpace,
                                                        className);
                        GCX_COOP();
                        ASSEMBLYREF asmRef = NULL;
                        DomainAssembly *pDomainAssembly = NULL;
                        GCPROTECT_BEGIN(asmRef);

                        pDomainAssembly = pDomain->RaiseTypeResolveEventThrowing(
                            pModule->GetAssembly()->GetDomainAssembly(),
                            pszFullName, &asmRef);

                        if (asmRef != NULL)
                        {
                            _ASSERTE(pDomainAssembly != NULL);
                            if (pDomainAssembly->GetAssembly()->GetLoaderAllocator()->IsCollectible())
                            {
                                if (!pModule->GetLoaderAllocator()->IsCollectible())
                                {
                                    LOG((LF_CLASSLOADER, LL_INFO10, "Bad result from TypeResolveEvent while loader TypeDef record: 0x%08x\n", typeDef));
                                    COMPlusThrow(kNotSupportedException, W("NotSupported_CollectibleBoundNonCollectible"));
                                }

                                pModule->GetLoaderAllocator()->EnsureReference(pDomainAssembly->GetAssembly()->GetLoaderAllocator());
                            }
                        }
                        GCPROTECT_END();
                        if (pDomainAssembly != NULL)
                        {
                            Assembly *pAssembly = pDomainAssembly->GetAssembly();

                            NameHandle name(nameSpace, className);
                            name.SetTypeToken(pModule, typeDef);
                            name.SetTokenNotToLoad(tdAllAssemblies);
                            typeHnd = pAssembly->GetLoader()->LoadTypeHandleThrowing(&name, level);
                        }
                    }
                }
            }
            else
            {
                TypeKey typeKey(pModule, typeDef);
                typeHnd = pModule->GetClassLoader()->LoadTypeHandleForTypeKey(&typeKey,
                                                                              typeHnd,
                                                                              level);
            }
        }
#endif // !DACCESS_COMPILE
    }

#ifndef DACCESS_COMPILE
    if ((fUninstantiated == FailIfUninstDefOrRef) && !typeHnd.IsNull() && typeHnd.IsGenericTypeDefinition())
    {
        typeHnd = TypeHandle();
    }

    if ((fNotFoundAction == ThrowIfNotFound) && typeHnd.IsNull() && (tokenNotToLoad != tdAllTypes))
    {
        pModule->GetAssembly()->ThrowTypeLoadException(pModule->GetMDImport(),
                                                       typeDef,
                                                       IDS_CLASSLOAD_GENERAL);
    }
#endif
    ;

    RETURN(typeHnd);
}

// Given a token specifying a typeDef or typeRef, and a module in
// which to interpret that token, find or load the corresponding type
// handle.
//
/*static*/
TypeHandle ClassLoader::LoadTypeDefOrRefThrowing(Module *pModule,
                                                 mdToken typeDefOrRef,
                                                 NotFoundAction fNotFoundAction /* = ThrowIfNotFound */ ,
                                                 PermitUninstantiatedFlag fUninstantiated /* = FailIfUninstDefOrRef */,
                                                 mdToken tokenNotToLoad,
                                                 ClassLoadLevel level)
{

    CONTRACT(TypeHandle)
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        MODE_ANY;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(level > CLASS_LOAD_BEGIN && level <= CLASS_LOADED);
        PRECONDITION(FORBIDGC_LOADER_USE_ENABLED()
                     || GetAppDomain()->CheckCanLoadTypes(pModule->GetAssembly()));

        POSTCONDITION(CheckPointer(RETVAL, NameHandle::OKToLoad(typeDefOrRef, tokenNotToLoad) && (fNotFoundAction == ThrowIfNotFound) ? NULL_NOT_OK : NULL_OK));
        POSTCONDITION(level <= CLASS_LOAD_UNRESTORED || RETVAL.IsNull() || RETVAL.IsRestored());
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    // NotFoundAction could be the bizarre 'ThrowButNullV11McppWorkaround',
    //  which means ThrowIfNotFound EXCEPT if this might be the Everett MCPP
    //  Nil-token ResolutionScope for value type.  In that case, it means
    //  ReturnNullIfNotFound.
    // If we have ThrowButNullV11McppWorkaround, remember that NULL *might*
    //  be OK if there is no resolution scope, but change the value to
    //  ThrowIfNotFound.
    BOOLEAN bReturnNullOkWhenNoResolutionScope = false;
    if (fNotFoundAction == ThrowButNullV11McppWorkaround)
    {
        bReturnNullOkWhenNoResolutionScope = true;
        fNotFoundAction = ThrowIfNotFound;
    }

    // First, attempt to find the class if it is already loaded
    ClassLoadLevel existingLoadLevel = CLASS_LOAD_BEGIN;
    TypeHandle typeHnd = LookupTypeDefOrRefInModule(pModule, typeDefOrRef, &existingLoadLevel);
    if (!typeHnd.IsNull())
    {
        if (existingLoadLevel < level)
        {
            pModule = typeHnd.GetModule();
            typeDefOrRef = typeHnd.GetCl();
        }
    }

    if (!typeHnd.IsNull() && existingLoadLevel >= level)
    {
        // perform the check that it's not an uninstantiated TypeDef/TypeRef
        // being used inappropriately.
        if (!((fUninstantiated == FailIfUninstDefOrRef) && !typeHnd.IsNull() && typeHnd.IsGenericTypeDefinition()))
        {
            RETURN(typeHnd);
        }
    }
    else
    {
        // otherwise try to resolve the TypeRef and/or load the corresponding TypeDef
        IMDInternalImport *pInternalImport = pModule->GetMDImport();
        mdToken tokType = TypeFromToken(typeDefOrRef);

        if (IsNilToken(typeDefOrRef) || ((tokType != mdtTypeDef)&&(tokType != mdtTypeRef))
            || !pInternalImport->IsValidToken(typeDefOrRef) )
        {
#ifdef _DEBUG
            LOG((LF_CLASSLOADER, LL_INFO10, "Bogus class token to load: 0x%08x\n", typeDefOrRef));
#endif

            typeHnd = TypeHandle();
        }

        else if (tokType == mdtTypeRef)
        {
            BOOL fNoResolutionScope;
            Module *pFoundModule = Assembly::FindModuleByTypeRef(pModule, typeDefOrRef,
                                                                  tokenNotToLoad==tdAllTypes ?
                                                                                  Loader::DontLoad :
                                                                                  Loader::Load,
                                                                 &fNoResolutionScope);

            if (pFoundModule != NULL)
            {

                // Not in my module, have to look it up by name.  This is the primary path
                // taken by the TypeRef case, i.e. we've resolve a TypeRef to a TypeDef/Module
                // pair.
                LPCUTF8 pszNameSpace;
                LPCUTF8 pszClassName;
                if (FAILED(pInternalImport->GetNameOfTypeRef(
                    typeDefOrRef,
                    &pszNameSpace,
                    &pszClassName)))
                {
                    typeHnd = TypeHandle();
                }
                else
                {
                    if (fNoResolutionScope)
                    {
                        // Everett C++ compiler can generate a TypeRef with RS=0
                        // without respective TypeDef for unmanaged valuetypes,
                        // referenced only by pointers to them,
                        // so we can fail to load legally w/ no exception
                        typeHnd = ClassLoader::LoadTypeByNameThrowing(pFoundModule->GetAssembly(),
                                                                      pszNameSpace,
                                                                      pszClassName,
                                                                      ClassLoader::ReturnNullIfNotFound,
                                                                      tokenNotToLoad==tdAllTypes ? ClassLoader::DontLoadTypes : ClassLoader::LoadTypes,
                                                                      level);

                        if(typeHnd.IsNull() && bReturnNullOkWhenNoResolutionScope)
                        {
                            fNotFoundAction = ReturnNullIfNotFound;
                            RETURN(typeHnd);
                        }
                    }
                    else
                    {
                        NameHandle nameHandle(pModule, typeDefOrRef);
                        nameHandle.SetName(pszNameSpace, pszClassName);
                        nameHandle.SetTokenNotToLoad(tokenNotToLoad);
                        typeHnd = pFoundModule->GetClassLoader()->
                            LoadTypeHandleThrowIfFailed(&nameHandle, level,
                                                        pFoundModule->IsReflection() ? NULL : pFoundModule);
                    }
                }

#ifndef DACCESS_COMPILE
                if (!(typeHnd.IsNull()))
                    pModule->StoreTypeRef(typeDefOrRef, typeHnd);
#endif
            }
        }
        else
        {
            // This is the mdtTypeDef case...
            typeHnd = LoadTypeDefThrowing(pModule, typeDefOrRef,
                                          fNotFoundAction,
                                          fUninstantiated,
                                          tokenNotToLoad,
                                          level);
        }
    }
    TypeHandle thRes = typeHnd;

    // reject the load if it's an uninstantiated TypeDef/TypeRef
    // being used inappropriately.
    if ((fUninstantiated == FailIfUninstDefOrRef) && !typeHnd.IsNull() && typeHnd.IsGenericTypeDefinition())
        thRes = TypeHandle();

    // perform the check to throw when the thing is not found
    if ((fNotFoundAction == ThrowIfNotFound) && thRes.IsNull() && (tokenNotToLoad != tdAllTypes))
    {
#ifndef DACCESS_COMPILE
        pModule->GetAssembly()->ThrowTypeLoadException(pModule->GetMDImport(),
                                                       typeDefOrRef,
                                                       IDS_CLASSLOAD_GENERAL);
#else
        DacNotImpl();
#endif
    }

    RETURN(thRes);
}

/*static*/
BOOL
ClassLoader::ResolveTokenToTypeDefThrowing(
    Module *         pTypeRefModule,
    mdTypeRef        typeRefToken,
    Module **        ppTypeDefModule,
    mdTypeDef *      pTypeDefToken,
    Loader::LoadFlag loadFlag,
    BOOL *           pfUsesTypeForwarder) // The semantic of this parameter: TRUE if a type forwarder is found. It is never set to FALSE.
{
    CONTRACT(BOOL)
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        MODE_ANY;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        PRECONDITION(CheckPointer(pTypeRefModule));
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    // It's a TypeDef already
    if (TypeFromToken(typeRefToken) == mdtTypeDef)
    {
        if (ppTypeDefModule != NULL)
            *ppTypeDefModule = pTypeRefModule;
        if (pTypeDefToken != NULL)
            *pTypeDefToken = typeRefToken;
        RETURN TRUE;
    }

    TypeHandle typeHnd = pTypeRefModule->LookupTypeRef(typeRefToken);

    // Type is already (partially) loaded and cached in the module's TypeRef table
    // Do not return here if we are checking for type forwarders
    if (!typeHnd.IsNull() && (pfUsesTypeForwarder == NULL))
    {
        if (ppTypeDefModule != NULL)
            *ppTypeDefModule = typeHnd.GetModule();
        if (pTypeDefToken != NULL)
            *pTypeDefToken = typeHnd.GetCl();
        RETURN TRUE;
    }

    BOOL fNoResolutionScope; //not used
    Module * pFoundRefModule = Assembly::FindModuleByTypeRef(
        pTypeRefModule,
        typeRefToken,
        loadFlag,
        &fNoResolutionScope);

    if (pFoundRefModule == NULL)
    {   // We didn't find the TypeRef anywhere
        RETURN FALSE;
    }

    // If checking for type forwarders, then we can see if a type forwarder was used based on the output of
    // pFoundRefModule and typeHnd (if typeHnd is set)
    if (!typeHnd.IsNull() && (pfUsesTypeForwarder != NULL))
    {
        if (typeHnd.GetModule() != pFoundRefModule)
        {
            *pfUsesTypeForwarder = TRUE;
        }

        if (ppTypeDefModule != NULL)
            *ppTypeDefModule = typeHnd.GetModule();
        if (pTypeDefToken != NULL)
            *pTypeDefToken = typeHnd.GetCl();
        RETURN TRUE;
    }

    // Not in my module, have to look it up by name
    LPCUTF8 pszNameSpace;
    LPCUTF8 pszClassName;
    if (FAILED(pTypeRefModule->GetMDImport()->GetNameOfTypeRef(typeRefToken, &pszNameSpace, &pszClassName)))
    {
        RETURN FALSE;
    }
    NameHandle nameHandle(pTypeRefModule, typeRefToken);
    nameHandle.SetName(pszNameSpace, pszClassName);
    if (loadFlag != Loader::Load)
    {
        nameHandle.SetTokenNotToLoad(tdAllTypes);
    }

    return ResolveNameToTypeDefThrowing(pFoundRefModule, &nameHandle, ppTypeDefModule, pTypeDefToken, loadFlag, pfUsesTypeForwarder);
}

/*static*/
BOOL
ClassLoader::ResolveNameToTypeDefThrowing(
    Module *         pModule,
    const NameHandle * pName,
    Module **        ppTypeDefModule,
    mdTypeDef *      pTypeDefToken,
    Loader::LoadFlag loadFlag,
    BOOL *           pfUsesTypeForwarder) // The semantic of this parameter: TRUE if a type forwarder is found. It is never set to FALSE.
{
    CONTRACT(BOOL)
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        MODE_ANY;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(CheckPointer(pName));
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    TypeHandle typeHnd;
    mdToken  foundTypeDef;
    Module * pFoundModule;
    mdExportedType foundExportedType;
    Module * pSourceModule = pModule;
    Module * pFoundRefModule = pModule;

    for (UINT32 nTypeForwardingChainSize = 0; nTypeForwardingChainSize < const_cMaxTypeForwardingChainSize; nTypeForwardingChainSize++)
    {
        foundTypeDef = mdTokenNil;
        pFoundModule = NULL;
        foundExportedType = mdTokenNil;
        if (!pSourceModule->GetClassLoader()->FindClassModuleThrowing(
            pName,
            &typeHnd,
            &foundTypeDef,
            &pFoundModule,
            &foundExportedType,
            NULL,
            pSourceModule->IsReflection() ? NULL : pSourceModule,
            loadFlag))
        {
            RETURN FALSE;
        }

        // Type is already loaded and cached in the loader's by-name table
        if (!typeHnd.IsNull())
        {
            if ((typeHnd.GetModule() != pFoundRefModule) && (pfUsesTypeForwarder != NULL))
            {   // We followed at least one type forwarder to resolve the type
                *pfUsesTypeForwarder = TRUE;
            }
            if (ppTypeDefModule != NULL)
                *ppTypeDefModule = typeHnd.GetModule();
            if (pTypeDefToken != NULL)
                *pTypeDefToken = typeHnd.GetCl();
            RETURN TRUE;
        }

        if (pFoundModule == NULL)
        {   // Module was probably not loaded
            RETURN FALSE;
        }

        if (TypeFromToken(foundExportedType) != mdtExportedType)
        {   // It's not exported type
            _ASSERTE(foundExportedType == mdTokenNil);

            if ((pFoundModule != pFoundRefModule) && (pfUsesTypeForwarder != NULL))
            {   // We followed at least one type forwarder to resolve the type
                *pfUsesTypeForwarder = TRUE;
            }
            if (pTypeDefToken != NULL)
                *pTypeDefToken = foundTypeDef;
            if (ppTypeDefModule != NULL)
                *ppTypeDefModule = pFoundModule;
            RETURN TRUE;
        }
        // It's exported type

        // Repeat the search for the type in the newly found module
        pSourceModule = pFoundModule;
    }
    // Type forwarding chain is too long
    RETURN FALSE;
} // ClassLoader::ResolveTokenToTypeDefThrowing

#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
//static
VOID
ClassLoader::GetEnclosingClassThrowing(
    IMDInternalImport * pInternalImport,
    Module *            pModule,
    mdTypeDef           cl,
    mdTypeDef *         tdEnclosing)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(tdEnclosing);
    *tdEnclosing = mdTypeDefNil;

    HRESULT hr = pInternalImport->GetNestedClassProps(cl, tdEnclosing);

    if (FAILED(hr))
    {
        if (hr != CLDB_E_RECORD_NOTFOUND)
            COMPlusThrowHR(hr);
        return;
    }

    if (TypeFromToken(*tdEnclosing) != mdtTypeDef)
        pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_ENCLOSING);
} // ClassLoader::GetEnclosingClassThrowing


//---------------------------------------------------------------------------------------
//
// Load a parent type or implemented interface type.
//
// If this is an instantiated type represented by a type spec, then instead of attempting to load the
// exact type, load an approximate instantiation in which all reference types are replaced by Object.
// The exact instantiated types will be loaded later by LoadInstantiatedInfo.
// We do this to avoid cycles early in class loading caused by definitions such as
//   struct M : ICloneable<M>                     // load ICloneable<object>
//   class C<T> : D<C<T>,int> for any T           // load D<object,int>
//
//static
TypeHandle
ClassLoader::LoadApproxTypeThrowing(
    Module *               pModule,
    mdToken                tok,
    SigPointer *           pSigInst,
    const SigTypeContext * pClassTypeContext)
{
    CONTRACT(TypeHandle)
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
        PRECONDITION(CheckPointer(pSigInst, NULL_OK));
        PRECONDITION(CheckPointer(pModule));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    IMDInternalImport * pInternalImport = pModule->GetMDImport();

    if (TypeFromToken(tok) == mdtTypeSpec)
    {
        ULONG cSig;
        PCCOR_SIGNATURE pSig;
        IfFailThrowBF(pInternalImport->GetTypeSpecFromToken(tok, &pSig, &cSig), BFA_METADATA_CORRUPT, pModule);

        SigPointer sigptr = SigPointer(pSig, cSig);
        CorElementType type = ELEMENT_TYPE_END;
        IfFailThrowBF(sigptr.GetElemType(&type), BFA_BAD_SIGNATURE, pModule);

        // The only kind of type specs that we recognise are instantiated types
        if (type != ELEMENT_TYPE_GENERICINST)
            pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport, tok, IDS_CLASSLOAD_GENERAL);

        // Of these, we outlaw instantiated value classes (they can't be interfaces and can't be subclassed)
        IfFailThrowBF(sigptr.GetElemType(&type), BFA_BAD_SIGNATURE, pModule);

        if (type != ELEMENT_TYPE_CLASS)
            pModule->GetAssembly()->ThrowTypeLoadException(pInternalImport, tok, IDS_CLASSLOAD_GENERAL);

        mdToken genericTok = 0;
        IfFailThrowBF(sigptr.GetToken(&genericTok), BFA_BAD_SIGNATURE, pModule);
        IfFailThrowBF(sigptr.GetData(NULL), BFA_BAD_SIGNATURE, pModule);

        if (pSigInst != NULL)
            *pSigInst = sigptr;

        // Try to load the generic type itself
        THROW_BAD_FORMAT_MAYBE(
            ((TypeFromToken(genericTok) == mdtTypeRef) || (TypeFromToken(genericTok) == mdtTypeDef)),
            BFA_UNEXPECTED_GENERIC_TOKENTYPE,
            pModule);
        TypeHandle genericTypeTH = LoadTypeDefOrRefThrowing(
            pModule,
            genericTok,
            ClassLoader::ThrowIfNotFound,
            ClassLoader::PermitUninstDefOrRef,
            tdNoTypes,
            CLASS_LOAD_APPROXPARENTS);

        // We load interfaces at very approximate types - the generic
        // interface itself.  We fix this up in LoadInstantiatedInfo.
        // This allows us to load recursive interfaces on structs such
        // as "struct VC : I<VC>".  The details of the interface
        // are not currently needed during the first phase
        // of setting up the method table.
        if (genericTypeTH.IsInterface())
        {
            RETURN genericTypeTH;
        }
        else
        {
            // approxTypes, i.e. approximate reference types by Object, i.e. load the canonical type
            RETURN SigPointer(pSig, cSig).GetTypeHandleThrowing(
                pModule,
                pClassTypeContext,
                ClassLoader::LoadTypes,
                CLASS_LOAD_APPROXPARENTS,
                TRUE /*dropGenericArgumentLevel*/);
        }
    }
    else
    {
        if (pSigInst != NULL)
            *pSigInst = SigPointer();
        RETURN LoadTypeDefOrRefThrowing(
            pModule,
            tok,
            ClassLoader::ThrowIfNotFound,
            ClassLoader::FailIfUninstDefOrRef,
            tdNoTypes,
            CLASS_LOAD_APPROXPARENTS);
    }
} // ClassLoader::LoadApproxTypeThrowing


//---------------------------------------------------------------------------------------
//
//static
MethodTable *
ClassLoader::LoadApproxParentThrowing(
    Module *               pModule,
    mdToken                cl,
    SigPointer *           pParentInst,
    const SigTypeContext * pClassTypeContext)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
        MODE_ANY;
    }
    CONTRACTL_END;

    mdTypeRef     crExtends;
    MethodTable * pParentMethodTable = NULL;
    TypeHandle    parentType;
    DWORD         dwAttrClass;
    Assembly *    pAssembly = pModule->GetAssembly();
    IMDInternalImport * pInternalImport = pModule->GetMDImport();

    // Initialize the return value;
    *pParentInst = SigPointer();

    // Now load all dependencies of this class
    if (FAILED(pInternalImport->GetTypeDefProps(
        cl,
        &dwAttrClass, // AttrClass
        &crExtends)))
    {
        pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_BADFORMAT);
    }

    if (RidFromToken(crExtends) != mdTokenNil)
    {
        // Do an "approximate" load of the parent, replacing reference types in the instantiation by Object
        // This is to avoid cycles in the loader e.g. on class C : D<C> or class C<T> : D<C<T>>
        // We fix up the exact parent later in LoadInstantiatedInfo
        parentType = LoadApproxTypeThrowing(pModule, crExtends, pParentInst, pClassTypeContext);

        pParentMethodTable = parentType.GetMethodTable();

        if (pParentMethodTable == NULL)
            pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_PARENTNULL);

        // cannot inherit from an interface
        if (pParentMethodTable->IsInterface())
            pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_PARENTINTERFACE);

        if (IsTdInterface(dwAttrClass))
        {
            // Interfaces must extend from Object
            if (! pParentMethodTable->IsObjectClass())
                pAssembly->ThrowTypeLoadException(pInternalImport, cl, IDS_CLASSLOAD_INTERFACEOBJECT);
        }
    }

    return pParentMethodTable;
} // ClassLoader::LoadApproxParentThrowing

// Perform a single phase of class loading
// It is the caller's responsibility to lock
/*static*/
TypeHandle ClassLoader::DoIncrementalLoad(TypeKey *pTypeKey, TypeHandle typeHnd, ClassLoadLevel currentLevel)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pTypeKey));
        PRECONDITION(currentLevel >= CLASS_LOAD_BEGIN && currentLevel < CLASS_LOADED);
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    if (LoggingOn(LF_CLASSLOADER, LL_INFO10000))
    {
        SString name;
        TypeString::AppendTypeKeyDebug(name, pTypeKey);
        LOG((LF_CLASSLOADER, LL_INFO10000, "PHASEDLOAD: About to do incremental load of type %S (%p) from level %s\n", name.GetUnicode(), typeHnd.AsPtr(), classLoadLevelName[currentLevel]));
    }
#endif

    // Level is BEGIN if and only if type handle is null
    CONSISTENCY_CHECK((currentLevel == CLASS_LOAD_BEGIN) == typeHnd.IsNull());

    switch (currentLevel)
    {
        // Attain at least level CLASS_LOAD_UNRESTORED (if just locating type in ngen image)
        // or at least level CLASS_LOAD_APPROXPARENTS (if creating type for the first time)
        case CLASS_LOAD_BEGIN :
            {
                IBCLoggerAwareAllocMemTracker amTracker;
                typeHnd = CreateTypeHandleForTypeKey(pTypeKey, &amTracker);
                CONSISTENCY_CHECK(!typeHnd.IsNull());
                TypeHandle published = PublishType(pTypeKey, typeHnd);
                if (published == typeHnd)
                    amTracker.SuppressRelease();
                typeHnd = published;
            }
            break;

        case CLASS_LOAD_UNRESTOREDTYPEKEY :
#ifdef FEATURE_PREJIT
            typeHnd.DoRestoreTypeKey();
#endif
            break;

        // Attain level CLASS_LOAD_APPROXPARENTS, starting with unrestored class
        case CLASS_LOAD_UNRESTORED :
#ifdef FEATURE_PREJIT
            {
                CONSISTENCY_CHECK(!typeHnd.IsRestored_NoLogging());
                if (typeHnd.IsTypeDesc())
                    typeHnd.AsTypeDesc()->Restore();
                else
                    typeHnd.AsMethodTable()->Restore();
            }
#endif
            break;

        // Attain level CLASS_LOAD_EXACTPARENTS
        case CLASS_LOAD_APPROXPARENTS :
            if (!typeHnd.IsTypeDesc())
            {
                LoadExactParents(typeHnd.AsMethodTable());
            }
            break;

        case CLASS_LOAD_EXACTPARENTS :
        case CLASS_DEPENDENCIES_LOADED :
        case CLASS_LOADED :
            break;

    }

    if (typeHnd.GetLoadLevel() >= CLASS_LOAD_EXACTPARENTS)
    {
        Notify(typeHnd);
    }

    return typeHnd;
}

/*static*/
// For non-canonical instantiations of generic types, create a fresh type by replicating the canonical instantiation
// For canonical instantiations of generic types, create a brand new method table
// For other constructed types, create a type desc and template method table if necessary
// For all other types, create a method table
TypeHandle ClassLoader::CreateTypeHandleForTypeKey(TypeKey* pKey, AllocMemTracker* pamTracker)
{
    CONTRACT(TypeHandle)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pKey));

        POSTCONDITION(RETVAL.CheckMatchesKey(pKey));
        MODE_ANY;
    }
    CONTRACT_END

    TypeHandle typeHnd = TypeHandle();

    if (!pKey->IsConstructed())
    {
        typeHnd = CreateTypeHandleForTypeDefThrowing(pKey->GetModule(),
                                                     pKey->GetTypeToken(),
                                                     pKey->GetInstantiation(),
                                                     pamTracker);
    }
    else if (pKey->HasInstantiation())
    {
        if (IsCanonicalGenericInstantiation(pKey->GetInstantiation()))
        {
            typeHnd = CreateTypeHandleForTypeDefThrowing(pKey->GetModule(),
                                                         pKey->GetTypeToken(),
                                                         pKey->GetInstantiation(),
                                                         pamTracker);
        }
        else
        {
            typeHnd = CreateTypeHandleForNonCanonicalGenericInstantiation(pKey, pamTracker);
        }
#if defined(_DEBUG) && !defined(CROSSGEN_COMPILE)
        if (Nullable::IsNullableType(typeHnd))
            Nullable::CheckFieldOffsets(typeHnd);
#endif
    }
    else if (pKey->GetKind() == ELEMENT_TYPE_FNPTR)
    {
        Module *pLoaderModule = ComputeLoaderModule(pKey);
        pLoaderModule->GetLoaderAllocator()->EnsureInstantiation(NULL, Instantiation(pKey->GetRetAndArgTypes(), pKey->GetNumArgs() + 1));

        PREFIX_ASSUME(pLoaderModule!=NULL);
        DWORD numArgs = pKey->GetNumArgs();
        BYTE* mem = (BYTE*) pamTracker->Track(pLoaderModule->GetAssembly()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(FnPtrTypeDesc)) + S_SIZE_T(sizeof(TypeHandle)) * S_SIZE_T(numArgs)));

        typeHnd = TypeHandle(new(mem)  FnPtrTypeDesc(pKey->GetCallConv(), numArgs, pKey->GetRetAndArgTypes()));
    }
    else
    {
        Module *pLoaderModule = ComputeLoaderModule(pKey);
        PREFIX_ASSUME(pLoaderModule!=NULL);

        CorElementType kind = pKey->GetKind();
        TypeHandle paramType = pKey->GetElementType();
        MethodTable *templateMT;

        // Create a new type descriptor and insert into constructed type table
        if (CorTypeInfo::IsArray(kind))
        {
            DWORD rank = pKey->GetRank();
            THROW_BAD_FORMAT_MAYBE((kind != ELEMENT_TYPE_ARRAY) || rank > 0, BFA_MDARRAY_BADRANK, pLoaderModule);
            THROW_BAD_FORMAT_MAYBE((kind != ELEMENT_TYPE_SZARRAY) || rank == 1, BFA_SDARRAY_BADRANK, pLoaderModule);

            // Arrays of BYREFS not allowed
            if (paramType.GetInternalCorElementType() == ELEMENT_TYPE_BYREF)
            {
                ThrowTypeLoadException(pKey, IDS_CLASSLOAD_BYREFARRAY);
            }

            // Arrays of ByRefLike types not allowed
            MethodTable* pMT = paramType.GetMethodTable();
            if (pMT != NULL)
            {
                if (pMT->IsByRefLike())
                {
                    ThrowTypeLoadException(pKey, IDS_CLASSLOAD_BYREFLIKEARRAY);
                }
            }

            if (rank > MAX_RANK)
            {
                ThrowTypeLoadException(pKey, IDS_CLASSLOAD_RANK_TOOLARGE);
            }

            templateMT = pLoaderModule->CreateArrayMethodTable(paramType, kind, rank, pamTracker);
            typeHnd = TypeHandle(templateMT);
        }
        else
        {
            // no parameterized type allowed on a reference
            if (paramType.GetInternalCorElementType() == ELEMENT_TYPE_BYREF)
            {
                ThrowTypeLoadException(pKey, IDS_CLASSLOAD_GENERAL);
            }

            // We do allow parametrized types of ByRefLike types. Languages may restrict them to produce safe or verifiable code,
            // but there is not a good reason for restricting them in the runtime.

            // let <Type>* type have a method table
            // System.UIntPtr's method table is used for types like int*, void *, string * etc.
            if (kind == ELEMENT_TYPE_PTR)
                templateMT = CoreLibBinder::GetElementType(ELEMENT_TYPE_U);
            else
                templateMT = NULL;

            BYTE* mem = (BYTE*) pamTracker->Track(pLoaderModule->GetAssembly()->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(ParamTypeDesc))));
            typeHnd = TypeHandle(new(mem)  ParamTypeDesc(kind, templateMT, paramType));
        }
    }

    RETURN typeHnd;
}

// Publish a type (and possibly member information) in the loader's
// tables Types are published before they are fully loaded. In
// particular, exact parent info (base class and interfaces) is loaded
// in a later phase
/*static*/
TypeHandle ClassLoader::PublishType(TypeKey *pTypeKey, TypeHandle typeHnd)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(typeHnd));
        PRECONDITION(CheckPointer(pTypeKey));

        // Key must match that of the handle
        PRECONDITION(typeHnd.CheckMatchesKey(pTypeKey));
    }
    CONTRACTL_END;


    if (pTypeKey->IsConstructed())
    {
        Module *pLoaderModule = ComputeLoaderModule(pTypeKey);
        EETypeHashTable *pTable = pLoaderModule->GetAvailableParamTypes();

        // m_AvailableTypesLock has to be taken in cooperative mode to avoid deadlocks during GC
        GCX_COOP();

        CrstHolder ch(&pLoaderModule->GetClassLoader()->m_AvailableTypesLock);

        // The type could have been loaded by a different thread as side-effect of avoiding deadlocks caused by LoadsTypeViolation
        TypeHandle existing = pTable->GetValue(pTypeKey);
        if (!existing.IsNull())
            return existing;

        pTable->InsertValue(typeHnd);

#ifdef _DEBUG
        // Checks to help ensure that the CoreLib in the ngen process does not get contaminated with pointers to the compilation domains.
        if (pLoaderModule->IsSystem() && IsCompilationProcess() && pLoaderModule->HasNativeImage())
        {
            CorElementType kind = pTypeKey->GetKind();
            MethodTable *typeHandleMethodTable = typeHnd.GetMethodTable();
            if ((typeHandleMethodTable != NULL) && (typeHandleMethodTable->GetLoaderAllocator() != pLoaderModule->GetLoaderAllocator()))
            {
                _ASSERTE(!"MethodTable of type loaded into CoreLib during NGen is not from CoreLib!");
            }
            if ((kind != ELEMENT_TYPE_FNPTR) && (kind != ELEMENT_TYPE_VAR) && (kind != ELEMENT_TYPE_MVAR))
            {
                if ((kind == ELEMENT_TYPE_SZARRAY) || (kind == ELEMENT_TYPE_ARRAY) || (kind == ELEMENT_TYPE_BYREF) || (kind == ELEMENT_TYPE_PTR) || (kind == ELEMENT_TYPE_VALUETYPE))
                {
                    // Check to ensure param value is also part of CoreLib.
                    if (pTypeKey->GetElementType().GetLoaderAllocator() != pLoaderModule->GetLoaderAllocator())
                    {
                        _ASSERTE(!"Param value of type key used to load type during NGEN not located within CoreLib yet type is placed into CoreLib");
                    }
                }
                else if (kind == ELEMENT_TYPE_FNPTR)
                {
                    // Check to ensure the parameter types of fnptr are in CoreLib
                    for (DWORD i = 0; i <= pTypeKey->GetNumArgs(); i++)
                    {
                        if (pTypeKey->GetRetAndArgTypes()[i].GetLoaderAllocator() != pLoaderModule->GetLoaderAllocator())
                        {
                            _ASSERTE(!"Ret or Arg type of function pointer type key used to load type during NGEN not located within CoreLib yet type is placed into CoreLib");
                        }
                    }
                }
                else if (kind == ELEMENT_TYPE_CLASS)
                {
                    // Check to ensure that the generic parameters are all within CoreLib
                    for (DWORD i = 0; i < pTypeKey->GetNumGenericArgs(); i++)
                    {
                        if (pTypeKey->GetInstantiation()[i].GetLoaderAllocator() != pLoaderModule->GetLoaderAllocator())
                        {
                            _ASSERTE(!"Instantiation parameter of generic class type key used to load type during NGEN not located within CoreLib yet type is placed into CoreLib");
                        }
                    }
                }
                else
                {
                    // Should not be able to get here
                    _ASSERTE(!"Unknown type key type");
                }
            }
        }
#endif // DEBUG
    }
    else
    {
        Module *pModule = pTypeKey->GetModule();
        mdTypeDef typeDef = pTypeKey->GetTypeToken();

        // m_AvailableTypesLock has to be taken in cooperative mode to avoid deadlocks during GC
        GCX_COOP();

        CrstHolder ch(&pModule->GetClassLoader()->m_AvailableTypesLock);

        // ! We cannot fail after this point.
        CANNOTTHROWCOMPLUSEXCEPTION();
        FAULT_FORBID();

        // The type could have been loaded by a different thread as side-effect of avoiding deadlocks caused by LoadsTypeViolation
        TypeHandle existing = pModule->LookupTypeDef(typeDef);
        if (!existing.IsNull())
            return existing;

        MethodTable *pMT = typeHnd.AsMethodTable();

        MethodTable::IntroducedMethodIterator it(pMT);
        for (; it.IsValid(); it.Next())
        {
            MethodDesc * pMD = it.GetMethodDesc();
            CONSISTENCY_CHECK(pMD != NULL && pMD->GetMethodTable() == pMT);
            if (!pMD->IsUnboxingStub())
            {
                pModule->EnsuredStoreMethodDef(pMD->GetMemberDef(), pMD);
            }
        }

        ApproxFieldDescIterator fdIterator(pMT, ApproxFieldDescIterator::ALL_FIELDS);
        FieldDesc* pFD;

        while ((pFD = fdIterator.Next()) != NULL)
        {
            if (pFD->GetEnclosingMethodTable() == pMT)
            {
                pModule->EnsuredStoreFieldDef(pFD->GetMemberDef(), pFD);
            }
        }

        // Publish the type last - to ensure that nobody can see it until all the method and field RID maps are filled in
        pModule->EnsuredStoreTypeDef(typeDef, typeHnd);
    }

    return typeHnd;
}

// Notify profiler and debugger that a type load has completed
// Also adjust perf counters
/*static*/
void ClassLoader::Notify(TypeHandle typeHnd)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(typeHnd));
    }
    CONTRACTL_END;

    LOG((LF_CLASSLOADER, LL_INFO1000, "Notify: %p %s\n", typeHnd.AsPtr(), typeHnd.IsTypeDesc() ? "typedesc" : typeHnd.AsMethodTable()->GetDebugClassName()));

    if (typeHnd.IsTypeDesc())
        return;

    MethodTable * pMT = typeHnd.AsMethodTable();

#ifdef PROFILING_SUPPORTED
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackClasses());
        // We don't tell profilers about typedescs, as per IF above.  Also, we don't
        // tell profilers about:
        if (
            // ...generics with unbound variables
            (!pMT->ContainsGenericVariables()) &&
            // ...or array method tables
            // (This check is mainly for NGEN restore, as JITted code won't hit
            // this code path for array method tables anyway)
            (!pMT->IsArray()))
        {
            LOG((LF_CLASSLOADER, LL_INFO1000, "Notifying profiler of Started1 %p %s\n", pMT, pMT->GetDebugClassName()));
            // Record successful load of the class for the profiler
            (&g_profControlBlock)->ClassLoadStarted(TypeHandleToClassID(typeHnd));

            //
            // Profiler can turn off TrackClasses during the Started() callback.  Need to
            // retest the flag here.
            //
            if (CORProfilerTrackClasses())
            {
                LOG((LF_CLASSLOADER, LL_INFO1000, "Notifying profiler of Finished1 %p %s\n", pMT, pMT->GetDebugClassName()));
                (&g_profControlBlock)->ClassLoadFinished(TypeHandleToClassID(typeHnd),
                    S_OK);
            }
        }
        END_PROFILER_CALLBACK();
    }
#endif //PROFILING_SUPPORTED

    g_IBCLogger.LogMethodTableAccess(pMT);

    if (pMT->IsTypicalTypeDefinition())
    {
        LOG((LF_CLASSLOADER, LL_INFO100, "Successfully loaded class %s\n", pMT->GetDebugClassName()));

#ifdef DEBUGGING_SUPPORTED
        {
            Module * pModule = pMT->GetModule();
            // Update metadata for dynamic module.
            pModule->UpdateDynamicMetadataIfNeeded();
        }

        if (CORDebuggerAttached())
        {
            LOG((LF_CORDB, LL_EVERYTHING, "NotifyDebuggerLoad clsload 2239 class %s\n", pMT->GetDebugClassName()));
            typeHnd.NotifyDebuggerLoad(NULL, FALSE);
        }
#endif // DEBUGGING_SUPPORTED
    }
}


//-----------------------------------------------------------------------------
// Common helper for LoadTypeHandleForTypeKey and LoadTypeHandleForTypeKeyNoLock.
// Makes the root level call to kick off the transitive closure walk for
// the final level pushes.
//-----------------------------------------------------------------------------
static void PushFinalLevels(TypeHandle typeHnd, ClassLoadLevel targetLevel, const InstantiationContext *pInstContext)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        LOADS_TYPE(targetLevel);
    }
    CONTRACTL_END

    // This phase brings the type and all its transitive dependencies to their
    // final state, sans the IsFullyLoaded bit.
    if (targetLevel >= CLASS_DEPENDENCIES_LOADED)
    {
        BOOL fBailed = FALSE;
        typeHnd.DoFullyLoad(NULL, CLASS_DEPENDENCIES_LOADED, NULL, &fBailed, pInstContext);
    }

    // This phase does access/constraint and other type-safety checks on the type
    // and on its transitive dependencies.
    if (targetLevel == CLASS_LOADED)
    {
        DFLPendingList pendingList;
        BOOL           fBailed = FALSE;

        typeHnd.DoFullyLoad(NULL, CLASS_LOADED, &pendingList, &fBailed, pInstContext);

        // In the case of a circular dependency, one or more types will have
        // had their promotions deferred.
        //
        // If we got to this point, all checks have successfully passed on
        // the transitive closure (otherwise, DoFullyLoad would have thrown.)
        //
        // So we can go ahead and mark everyone as fully loaded.
        //
        UINT numTH = pendingList.Count();
        TypeHandle *pTHPending = pendingList.Table();
        for (UINT i = 0; i < numTH; i++)
        {
            // NOTE: It is possible for duplicates to appear in this list so
            // don't do any operation that isn't idempodent.

            pTHPending[i].SetIsFullyLoaded();
        }
    }
}


//
TypeHandle ClassLoader::LoadTypeHandleForTypeKey(TypeKey *pTypeKey,
                                                 TypeHandle typeHnd,
                                                 ClassLoadLevel targetLevel/*=CLASS_LOADED*/,
                                                 const InstantiationContext *pInstContext/*=NULL*/)
{

    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        LOADS_TYPE(targetLevel);
    }
    CONTRACTL_END

    GCX_PREEMP();

#ifdef _DEBUG
    if (LoggingOn(LF_CLASSLOADER, LL_INFO1000))
    {
        SString name;
        TypeString::AppendTypeKeyDebug(name, pTypeKey);
        LOG((LF_CLASSLOADER, LL_INFO10000, "PHASEDLOAD: LoadTypeHandleForTypeKey for type %S to level %s\n", name.GetUnicode(), classLoadLevelName[targetLevel]));
        CrstHolder unresolvedClassLockHolder(&m_UnresolvedClassLock);
        m_pUnresolvedClassHash->Dump();
    }
#endif

#if defined(FEATURE_EVENT_TRACE)
    UINT32 typeLoad = ETW::TypeSystemLog::TypeLoadBegin();
#endif

    // When using domain neutral assemblies (and not eagerly propagating dependency loads),
    // it's possible to get here without having injected the module into the current app domain.
    // GetDomainFile will accomplish that.

    if (!pTypeKey->IsConstructed())
    {
        pTypeKey->GetModule()->GetDomainFile();
    }

    ClassLoadLevel currentLevel = typeHnd.IsNull() ? CLASS_LOAD_BEGIN : typeHnd.GetLoadLevel();
    ClassLoadLevel targetLevelUnderLock = targetLevel < CLASS_DEPENDENCIES_LOADED ? targetLevel : (ClassLoadLevel) (CLASS_DEPENDENCIES_LOADED-1);
    if (currentLevel < targetLevelUnderLock)
    {
        typeHnd = LoadTypeHandleForTypeKey_Body(pTypeKey,
                                                typeHnd,
                                                targetLevelUnderLock);
        _ASSERTE(!typeHnd.IsNull());
    }
    _ASSERTE(typeHnd.GetLoadLevel() >= targetLevelUnderLock);

    PushFinalLevels(typeHnd, targetLevel, pInstContext);

#if defined(FEATURE_EVENT_TRACE)
    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, TypeLoadStop))
    {
        ETW::TypeSystemLog::TypeLoadEnd(typeLoad, typeHnd, (UINT16)targetLevel);
    }
#endif

    return typeHnd;
}

//
TypeHandle ClassLoader::LoadTypeHandleForTypeKeyNoLock(TypeKey *pTypeKey,
                                                       ClassLoadLevel targetLevel/*=CLASS_LOADED*/,
                                                       const InstantiationContext *pInstContext/*=NULL*/)
{

    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        LOADS_TYPE(targetLevel);
        PRECONDITION(CheckPointer(pTypeKey));
        PRECONDITION(targetLevel >= 0 && targetLevel <= CLASS_LOADED);
    }
    CONTRACTL_END

    GCX_PREEMP();

    TypeHandle typeHnd = TypeHandle();

    ClassLoadLevel currentLevel = CLASS_LOAD_BEGIN;
    ClassLoadLevel targetLevelUnderLock = targetLevel < CLASS_DEPENDENCIES_LOADED ? targetLevel : (ClassLoadLevel) (CLASS_DEPENDENCIES_LOADED-1);
    while (currentLevel < targetLevelUnderLock)
    {
        typeHnd = DoIncrementalLoad(pTypeKey, typeHnd, currentLevel);
        CONSISTENCY_CHECK(typeHnd.GetLoadLevel() > currentLevel);
        currentLevel = typeHnd.GetLoadLevel();
    }

    PushFinalLevels(typeHnd, targetLevel, pInstContext);

    return typeHnd;
}

//---------------------------------------------------------------------------------------
//
class PendingTypeLoadHolder
{
    Thread * m_pThread;
    PendingTypeLoadEntry * m_pEntry;
    PendingTypeLoadHolder * m_pPrevious;

public:
    PendingTypeLoadHolder(PendingTypeLoadEntry * pEntry)
    {
        LIMITED_METHOD_CONTRACT;

        m_pThread = GetThread();
        m_pEntry = pEntry;

        m_pPrevious = m_pThread->GetPendingTypeLoad();
        m_pThread->SetPendingTypeLoad(this);
    }

    ~PendingTypeLoadHolder()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(m_pThread->GetPendingTypeLoad() == this);
        m_pThread->SetPendingTypeLoad(m_pPrevious);
    }

    static bool CheckForDeadLockOnCurrentThread(PendingTypeLoadEntry * pEntry)
    {
        LIMITED_METHOD_CONTRACT;

        PendingTypeLoadHolder * pCurrent = GetThread()->GetPendingTypeLoad();

        while (pCurrent != NULL)
        {
            if (pCurrent->m_pEntry == pEntry)
                return true;

            pCurrent = pCurrent->m_pPrevious;
        }

        return false;
    }
};

//---------------------------------------------------------------------------------------
//
TypeHandle
ClassLoader::LoadTypeHandleForTypeKey_Body(
    TypeKey *                         pTypeKey,
    TypeHandle                        typeHnd,
    ClassLoadLevel                    targetLevel)
{
    CONTRACT(TypeHandle)
    {
        STANDARD_VM_CHECK;
        POSTCONDITION(!typeHnd.IsNull() && typeHnd.GetLoadLevel() >= targetLevel);
    }
    CONTRACT_END

    if (!pTypeKey->IsConstructed())
    {
        Module *pModule = pTypeKey->GetModule();
        mdTypeDef cl = pTypeKey->GetTypeToken();

        STRESS_LOG2(LF_CLASSLOADER,  LL_INFO100000, "LoadTypeHandle: Loading Class from Module %p token %x\n", pModule, cl);

#ifdef _DEBUG
        IMDInternalImport* pInternalImport = pModule->GetMDImport();
        LPCUTF8 className;
        LPCUTF8 nameSpace;
        if (FAILED(pInternalImport->GetNameOfTypeDef(cl, &className, &nameSpace)))
        {
            className = nameSpace = "Invalid TypeDef record";
        }
        if (g_pConfig->ShouldBreakOnClassLoad(className))
            CONSISTENCY_CHECK_MSGF(false, ("BreakOnClassLoad: typename '%s' ", className));
#endif
    }

    ReleaseHolder<PendingTypeLoadEntry> pLoadingEntry;
    CrstHolderWithState unresolvedClassLockHolder(&m_UnresolvedClassLock, false);

retry:
    unresolvedClassLockHolder.Acquire();

    // Is it in the hash of classes currently being loaded?
    pLoadingEntry = m_pUnresolvedClassHash->GetValue(pTypeKey);
    if (pLoadingEntry)
    {
        pLoadingEntry->AddRef();

        // It is in the hash, which means that another thread is waiting for it (or that we are
        // already loading this class on this thread, which should never happen, since that implies
        // a recursive dependency).
        unresolvedClassLockHolder.Release();

        //
        // Check one last time before waiting that the type handle is not sufficiently loaded to
        // prevent deadlocks
        //
        {
            if (typeHnd.IsNull())
            {
                typeHnd = LookupTypeHandleForTypeKey(pTypeKey);
            }

            if (!typeHnd.IsNull())
            {
                if (typeHnd.GetLoadLevel() >= targetLevel)
                    RETURN typeHnd;
            }
        }

        if (PendingTypeLoadHolder::CheckForDeadLockOnCurrentThread(pLoadingEntry))
        {
            // Attempting recursive load
            ClassLoader::ThrowTypeLoadException(pTypeKey, IDS_CLASSLOAD_GENERAL);
        }

        //
        // Violation of type loadlevel ordering rules depends on type load failing in case of cyclic dependency that would
        // otherwise lead to deadlock. We will speculatively proceed with the type load to make it fail in the right spot,
        // in backward compatible way. In case the type load succeeds, we will only let one type win in PublishType.
        //
        if (typeHnd.IsNull() && GetThread()->HasThreadStateNC(Thread::TSNC_LoadsTypeViolation))
        {
            PendingTypeLoadHolder ptlh(pLoadingEntry);
            typeHnd = DoIncrementalLoad(pTypeKey, TypeHandle(), CLASS_LOAD_BEGIN);
            goto retry;
        }

        {
            // Wait for class to be loaded by another thread.  This is where we start tracking the
            // entry, so there is an implicit Acquire in our use of Assign here.
            CrstHolder loadingEntryLockHolder(&pLoadingEntry->m_Crst);
            _ASSERTE(pLoadingEntry->HasLock());
        }

        // Result of other thread loading the class
        HRESULT hr = pLoadingEntry->m_hrResult;

        if (FAILED(hr)) {

            //
            // Redo the lookup one more time and return a valid type if possible. The other thread could
            // have hit error while loading the type to higher level than we need.
            //
            {
                if (typeHnd.IsNull())
                {
                    typeHnd = LookupTypeHandleForTypeKey(pTypeKey);
                }

                if (!typeHnd.IsNull())
                {
                    if (typeHnd.GetLoadLevel() >= targetLevel)
                        RETURN typeHnd;
                }
            }

            if (hr == E_ABORT) {
                LOG((LF_CLASSLOADER, LL_INFO10, "need to retry LoadTypeHandle: %x\n", hr));
                goto retry;
            }

            LOG((LF_CLASSLOADER, LL_INFO10, "Failed to load in other entry: %x\n", hr));

            if (hr == E_OUTOFMEMORY) {
                COMPlusThrowOM();
            }

            pLoadingEntry->ThrowException();
        }

        // Get a pointer to the EEClass being loaded
        typeHnd = pLoadingEntry->m_typeHandle;

        if (!typeHnd.IsNull())
        {
            // If the type load on the other thread loaded the type to the needed level, return it here.
            if (typeHnd.GetLoadLevel() >= targetLevel)
                RETURN typeHnd;
        }

        // The type load on the other thread did not load the type "enough". Begin the type load
        // process again to cause us to load to the needed level.
        goto retry;
    }

    if (typeHnd.IsNull())
    {
        // The class was not being loaded.  However, it may have already been loaded after our
        // first LoadTypeHandleThrowIfFailed() and before taking the lock.
        typeHnd = LookupTypeHandleForTypeKey(pTypeKey);
    }

    ClassLoadLevel currentLevel = CLASS_LOAD_BEGIN;
    if (!typeHnd.IsNull())
    {
        currentLevel = typeHnd.GetLoadLevel();
        if (currentLevel >= targetLevel)
            RETURN typeHnd;
    }

    // It was not loaded, and it is not being loaded, so we must load it.  Create a new LoadingEntry
    // and acquire it immediately so that other threads will block.
    pLoadingEntry = new PendingTypeLoadEntry(*pTypeKey, typeHnd);  // this atomically creates a crst and acquires it

    if (!(m_pUnresolvedClassHash->InsertValue(pLoadingEntry)))
    {
        COMPlusThrowOM();
    }

    // Leave the global lock, so that other threads may now start waiting on our class's lock
    unresolvedClassLockHolder.Release();

    EX_TRY
    {
        PendingTypeLoadHolder ptlh(pLoadingEntry);

        TRIGGERS_TYPELOAD();

        while (currentLevel < targetLevel)
        {
            typeHnd = DoIncrementalLoad(pTypeKey, typeHnd, currentLevel);
            CONSISTENCY_CHECK(typeHnd.GetLoadLevel() > currentLevel);
            currentLevel = typeHnd.GetLoadLevel();

            // If other threads are waiting for this load, unblock them as soon as possible to prevent deadlocks.
            if (pLoadingEntry->HasWaiters())
                break;
        }

        _ASSERTE(!typeHnd.IsNull());
        pLoadingEntry->SetResult(typeHnd);
    }
    EX_HOOK
    {
        LOG((LF_CLASSLOADER, LL_INFO10, "Caught an exception loading: %x, %0x (Module)\n", pTypeKey->IsConstructed() ? pTypeKey->ComputeHash() : pTypeKey->GetTypeToken(), pTypeKey->GetModule()));

        if (!GetThread()->HasThreadStateNC(Thread::TSNC_LoadsTypeViolation))
        {
            // Fix up the loading entry.
            Exception *pException = GET_EXCEPTION();
            pLoadingEntry->SetException(pException);
        }

        // Unlink this class from the unresolved class list.
        unresolvedClassLockHolder.Acquire();
        m_pUnresolvedClassHash->DeleteValue(pTypeKey);

        // Release the lock before proceeding. The unhandled exception filters take number of locks that
        // have ordering violations with this lock.
        unresolvedClassLockHolder.Release();

        // Unblock any thread waiting to load same type as in TypeLoadEntry
        pLoadingEntry->UnblockWaiters();
    }
    EX_END_HOOK;

    // Unlink this class from the unresolved class list.
    unresolvedClassLockHolder.Acquire();
    m_pUnresolvedClassHash->DeleteValue(pTypeKey);
    unresolvedClassLockHolder.Release();

    // Unblock any thread waiting to load same type as in TypeLoadEntry. This should be done
    // after pLoadingEntry is removed from m_pUnresolvedClassHash. Otherwise the other thread
    // (which was waiting) will keep spinning for a while after waking up, till the current thread removes
    //  pLoadingEntry from m_pUnresolvedClassHash. This can cause hang in situation when the current
    // thread is a background thread and so will get very less processor cycle to perform subsequent
    // operations to remove the entry from hash later.
    pLoadingEntry->UnblockWaiters();

    if (currentLevel < targetLevel)
        goto retry;

    RETURN typeHnd;
} // ClassLoader::LoadTypeHandleForTypeKey_Body

#endif //!DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
//static
TypeHandle
ClassLoader::LoadArrayTypeThrowing(
    TypeHandle     elemType,
    CorElementType arrayKind,
    unsigned       rank,        //=0
    LoadTypesFlag  fLoadTypes,  //=LoadTypes
    ClassLoadLevel level)
{
    CONTRACT(TypeHandle)
    {
        if (FORBIDGC_LOADER_USE_ENABLED()) NOTHROW; else THROWS;
        if (FORBIDGC_LOADER_USE_ENABLED()) GC_NOTRIGGER; else GC_TRIGGERS;
        if (FORBIDGC_LOADER_USE_ENABLED()) FORBID_FAULT; else { INJECT_FAULT(COMPlusThrowOM()); }
        if (FORBIDGC_LOADER_USE_ENABLED() || fLoadTypes != LoadTypes) { LOADS_TYPE(CLASS_LOAD_BEGIN); } else { LOADS_TYPE(level); }
        MODE_ANY;
        SUPPORTS_DAC;
        POSTCONDITION(CheckPointer(RETVAL, ((fLoadTypes == LoadTypes) ? NULL_NOT_OK : NULL_OK)));
    }
    CONTRACT_END

    CorElementType predefinedElementType = ELEMENT_TYPE_END;

    // Try finding it in our cache of primitive SD arrays
    if (arrayKind == ELEMENT_TYPE_SZARRAY) {
        predefinedElementType = elemType.GetSignatureCorElementType();
        if (predefinedElementType <= ELEMENT_TYPE_R8) {
            TypeHandle th = g_pPredefinedArrayTypes[predefinedElementType];
            if (th != 0)
                RETURN(th);
        }
        // This call to AsPtr is somewhat bogus and only used
        // as an optimization.  If the TypeHandle is really a TypeDesc
        // then the equality checks for the optimizations below will
        // fail.  Thus ArrayMT should not be used elsewhere in this function
        else if (elemType.AsPtr() == PTR_VOID(g_pObjectClass)) {
            // Code duplicated because Object[]'s SigCorElementType is E_T_CLASS, not OBJECT
            TypeHandle th = g_pPredefinedArrayTypes[ELEMENT_TYPE_OBJECT];
            if (th != 0)
                RETURN(th);
            predefinedElementType = ELEMENT_TYPE_OBJECT;
        }
        else if (elemType.AsPtr() == PTR_VOID(g_pStringClass)) {
            // Code duplicated because String[]'s SigCorElementType is E_T_CLASS, not STRING
            TypeHandle th = g_pPredefinedArrayTypes[ELEMENT_TYPE_STRING];
            if (th != 0)
                RETURN(th);
            predefinedElementType = ELEMENT_TYPE_STRING;
        }
        else {
            predefinedElementType = ELEMENT_TYPE_END;
        }
        rank = 1;
    }

#ifndef DACCESS_COMPILE
    // To avoid loading useless shared instantiations, normalize shared instantiations to the canonical form
    // (e.g. List<_Canon>[] -> _Canon[])
    // The denormalized shared instantiations should be needed only during JITing, so it is fine to skip this
    // for DACCESS_COMPILE.
    if (elemType.IsCanonicalSubtype())
    {
        elemType = ClassLoader::CanonicalizeGenericArg(elemType);
    }
#endif

    TypeKey key(arrayKind, elemType, rank);
    TypeHandle th = LoadConstructedTypeThrowing(&key, fLoadTypes, level);

    if (predefinedElementType != ELEMENT_TYPE_END && !th.IsNull() && th.IsFullyLoaded())
    {
        g_pPredefinedArrayTypes[predefinedElementType] = th;
    }

    RETURN(th);
} // ClassLoader::LoadArrayTypeThrowing

#ifndef DACCESS_COMPILE

VOID ClassLoader::AddAvailableClassDontHaveLock(Module *pModule,
                                                mdTypeDef classdef,
                                                AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    CrstHolder ch(&m_AvailableClassLock);

    AddAvailableClassHaveLock(
        pModule,
        classdef,
        pamTracker);
}

// This routine must be single threaded!  The reason is that there are situations which allow
// the same class name to have two different mdTypeDef tokens (for example, we load two different DLLs
// simultaneously, and they have some common class files, or we convert the same class file
// simultaneously on two threads).  The problem is that we do not want to overwrite the old
// <classname> -> pModule mapping with the new one, because this may cause identity problems.
//
// This routine assumes you already have the lock.  Use AddAvailableClassDontHaveLock() if you
// don't have it.
//
VOID ClassLoader::AddAvailableClassHaveLock(
    Module *          pModule,
    mdTypeDef         classdef,
    AllocMemTracker * pamTracker)  // Optimization for faster prefix comparison implementation
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    EEClassHashTable *pClassHash = pModule->GetAvailableClassHash();
    EEClassHashTable *pClassCaseInsHash = pModule->GetAvailableClassCaseInsHash();

    LPCUTF8        pszName;
    LPCUTF8        pszNameSpace;
    HashDatum      ThrowawayData;
    IMDInternalImport *pMDImport = pModule->GetMDImport();
    if (FAILED(pMDImport->GetNameOfTypeDef(classdef, &pszName, &pszNameSpace)))
    {
        pszName = pszNameSpace = "Invalid TypeDef token";
        pModule->GetAssembly()->ThrowBadImageException(pszNameSpace, pszName, BFA_INVALID_TOKEN);
    }

    EEClassHashEntry_t *pBucket;
    mdTypeDef      enclosing;
    if (SUCCEEDED(pMDImport->GetNestedClassProps(classdef, &enclosing))) {
        // nested type

        LPCUTF8 pszEnclosingName;
        LPCUTF8 pszEnclosingNameSpace;
        mdTypeDef enclEnclosing;

        // Find this type's encloser's entry in the available table.
        // We'll save a pointer to it in the new hash entry for this type.
        BOOL fNestedEncl = SUCCEEDED(pMDImport->GetNestedClassProps(enclosing, &enclEnclosing));

        EEClassHashTable::LookupContext sContext;
        if (FAILED(pMDImport->GetNameOfTypeDef(enclosing, &pszEnclosingName, &pszEnclosingNameSpace)))
        {
            pszName = pszNameSpace = "Invalid TypeDef token";
            pModule->GetAssembly()->ThrowBadImageException(pszNameSpace, pszName, BFA_INVALID_TOKEN);
        }
        if ((pBucket = pClassHash->GetValue(pszEnclosingNameSpace,
                                            pszEnclosingName,
                                            &ThrowawayData,
                                            fNestedEncl,
                                            &sContext)) != NULL) {
            if (fNestedEncl) {
                // Find entry for enclosing class - NOTE, this assumes that the
                // enclosing class's TypeDef or ExportedType was inserted previously,
                // which assumes that, when enuming TD's, we get the enclosing class first
                while ((!CompareNestedEntryWithTypeDef(pMDImport,
                                                       enclEnclosing,
                                                       pClassHash,
                                                       pBucket->GetEncloser())) &&
                       (pBucket = pClassHash->FindNextNestedClass(pszEnclosingNameSpace,
                                                                  pszEnclosingName,
                                                                  &ThrowawayData,
                                                                  &sContext)) != NULL);
            }

            if (!pBucket) // Enclosing type not found in hash table
                pModule->GetAssembly()->ThrowBadImageException(pszNameSpace, pszName, BFA_ENCLOSING_TYPE_NOT_FOUND);

            // In this hash table, if the lower bit is set, it means a Module, otherwise it means EEClass*
            ThrowawayData = EEClassHashTable::CompressClassDef(classdef);
            InsertValue(pClassHash, pClassCaseInsHash, pszNameSpace, pszName, ThrowawayData, pBucket, pamTracker);
        }
    }
    else {
        // Don't add duplicate top-level classes.  Top-level classes are
        // added to the beginning of the bucket, while nested classes are
        // added to the end.  So, a duplicate top-level class could hide
        // the previous type's EEClass* entry in the hash table.
        EEClassHashEntry_t *pCaseInsEntry = NULL;
        LPUTF8 pszLowerCaseNS = NULL;
        LPUTF8 pszLowerCaseName = NULL;

        if (pClassCaseInsHash) {
            CreateCanonicallyCasedKey(pszNameSpace, pszName, &pszLowerCaseNS, &pszLowerCaseName);
            pCaseInsEntry = pClassCaseInsHash->AllocNewEntry(pamTracker);
        }

        EEClassHashEntry_t *pEntry = pClassHash->FindItem(pszNameSpace, pszName, FALSE, NULL);
        if (pEntry) {
            HashDatum Data = pEntry->GetData();

            if (((size_t)Data & EECLASSHASH_TYPEHANDLE_DISCR) &&
                ((size_t)Data & EECLASSHASH_MDEXPORT_DISCR)) {

                // it's an ExportedType - check the 'already seen' bit and if on, report a class loading exception
                // otherwise, set it
                if ((size_t)Data & EECLASSHASH_ALREADYSEEN)
                    pModule->GetAssembly()->ThrowBadImageException(pszNameSpace, pszName, BFA_MULT_TYPE_SAME_NAME);
                else {
                    Data = (HashDatum)((size_t)Data | EECLASSHASH_ALREADYSEEN);
                    pEntry->SetData(Data);
                }
            }
            else {
                // We want to throw an exception for a duplicate typedef.
                // However, this used to be allowed in 1.0/1.1, and some third-party DLLs have
                // been obfuscated so that they have duplicate private typedefs.
                // We must allow this for old assemblies for app compat reasons
                pModule->GetAssembly()->ThrowBadImageException(pszNameSpace, pszName, BFA_MULT_TYPE_SAME_NAME);
            }
        }
        else {
            pEntry = pClassHash->AllocNewEntry(pamTracker);

            CANNOTTHROWCOMPLUSEXCEPTION();
            FAULT_FORBID();

            pClassHash->InsertValueUsingPreallocatedEntry(pEntry, pszNameSpace, pszName, EEClassHashTable::CompressClassDef(classdef), NULL);

            if (pClassCaseInsHash)
                pClassCaseInsHash->InsertValueUsingPreallocatedEntry(pCaseInsEntry, pszLowerCaseNS, pszLowerCaseName, pEntry, pEntry->GetEncloser());
        }
    }
}

VOID ClassLoader::AddExportedTypeDontHaveLock(Module *pManifestModule,
    mdExportedType cl,
    AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    CrstHolder ch(&m_AvailableClassLock);

    AddExportedTypeHaveLock(
        pManifestModule,
        cl,
        pamTracker);
}

VOID ClassLoader::AddExportedTypeHaveLock(Module *pManifestModule,
                                          mdExportedType cl,
                                          AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END


    mdToken mdImpl;
    LPCSTR pszName;
    LPCSTR pszNameSpace;
    IMDInternalImport* pAsmImport = pManifestModule->GetMDImport();
    if (FAILED(pAsmImport->GetExportedTypeProps(
        cl,
        &pszNameSpace,
        &pszName,
        &mdImpl,
        NULL,   // type def
        NULL))) // flags
    {
        pManifestModule->GetAssembly()->ThrowBadImageException(pszNameSpace, pszName, BFA_INVALID_TOKEN);
    }

    HashDatum ThrowawayData;

    if (TypeFromToken(mdImpl) == mdtExportedType)
    {
        // nested class
        LPCUTF8 pszEnclosingNameSpace;
        LPCUTF8 pszEnclosingName;
        mdToken nextImpl;
        if (FAILED(pAsmImport->GetExportedTypeProps(
            mdImpl,
            &pszEnclosingNameSpace,
            &pszEnclosingName,
            &nextImpl,
            NULL,   // type def
            NULL))) // flags
        {
            pManifestModule->GetAssembly()->ThrowBadImageException(pszNameSpace, pszName, BFA_INVALID_TOKEN);
        }

        // Find entry for enclosing class - NOTE, this assumes that the
        // enclosing class's ExportedType was inserted previously, which assumes that,
        // when enuming ExportedTypes, we get the enclosing class first
        EEClassHashEntry_t *pBucket;
        EEClassHashTable::LookupContext sContext;
        if ((pBucket = pManifestModule->GetAvailableClassHash()->GetValue(pszEnclosingNameSpace,
                                                                          pszEnclosingName,
                                                                          &ThrowawayData,
                                                                          TypeFromToken(nextImpl) == mdtExportedType,
                                                                          &sContext)) != NULL) {
            do {
                // check to see if this is the correct class
                if (EEClassHashTable::UncompressModuleAndClassDef(ThrowawayData) == mdImpl) {
                    ThrowawayData = EEClassHashTable::CompressClassDef(cl);

                    // we explicitly don't check for the case insensitive hash table because we know it can't have been created yet
                    pManifestModule->GetAvailableClassHash()->InsertValue(pszNameSpace, pszName, ThrowawayData, pBucket, pamTracker);
                }
                pBucket = pManifestModule->GetAvailableClassHash()->FindNextNestedClass(pszEnclosingNameSpace, pszEnclosingName, &ThrowawayData, &sContext);
            } while (pBucket);
        }

        // If the encloser is not in the hash table, this nested class
        // was defined in the manifest module, so it doesn't need to be added
        return;
    }
    else {
        // Defined in the manifest module - add to the hash table by TypeDef instead
        if (mdImpl == mdFileNil)
            return;

        // Don't add duplicate top-level classes
        // In this hash table, if the lower bit is set, it means a Module, otherwise it means EEClass*
        ThrowawayData = EEClassHashTable::CompressClassDef(cl);
        // ThrowawayData is an IN OUT param. Going in its the pointer to the new value if the entry needs
        // to be inserted. The OUT param points to the value stored in the hash table.
        BOOL bFound;
        pManifestModule->GetAvailableClassHash()->InsertValueIfNotFound(pszNameSpace, pszName, &ThrowawayData, NULL, FALSE, &bFound, pamTracker);
        if (bFound) {

            // Check for duplicate ExportedTypes
            // Let it slide if it's pointing to the same type
            mdToken foundTypeImpl;
            if ((size_t)ThrowawayData & EECLASSHASH_MDEXPORT_DISCR)
            {
                mdExportedType foundExportedType = EEClassHashTable::UncompressModuleAndClassDef(ThrowawayData);
                if (FAILED(pAsmImport->GetExportedTypeProps(
                    foundExportedType,
                    NULL,   // namespace
                    NULL,   // name
                    &foundTypeImpl,
                    NULL,   // TypeDef
                    NULL))) // flags
                {
                    pManifestModule->GetAssembly()->ThrowBadImageException(pszNameSpace, pszName, BFA_INVALID_TOKEN);
                }
            }
            else
            {
                foundTypeImpl = mdFileNil;
            }

            if (mdImpl != foundTypeImpl)
            {
                pManifestModule->GetAssembly()->ThrowBadImageException(pszNameSpace, pszName, BFA_MULT_TYPE_SAME_NAME);
            }
        }
    }
}

static MethodTable* GetEnclosingMethodTable(MethodTable *pMT)
{
    CONTRACT(MethodTable*)
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
        POSTCONDITION(RETVAL == NULL || RETVAL->IsTypicalTypeDefinition());
    }
    CONTRACT_END;

    RETURN pMT->LoadEnclosingMethodTable();
}

AccessCheckContext::AccessCheckContext(MethodDesc* pCallerMethod)
{
    CONTRACTL
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION(CheckPointer(pCallerMethod));
    }
    CONTRACTL_END;

    m_pCallerMethod = pCallerMethod;
    m_pCallerMT = m_pCallerMethod->GetMethodTable();
    m_pCallerAssembly = m_pCallerMT->GetAssembly();
}

AccessCheckContext::AccessCheckContext(MethodDesc* pCallerMethod, MethodTable* pCallerType)
{
    CONTRACTL
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION(CheckPointer(pCallerMethod, NULL_OK));
        PRECONDITION(CheckPointer(pCallerType));
    }
    CONTRACTL_END;

    m_pCallerMethod = pCallerMethod;
    m_pCallerMT = pCallerType;
    m_pCallerAssembly = pCallerType->GetAssembly();
}

//******************************************************************************

// static
AccessCheckOptions* AccessCheckOptions::s_pNormalAccessChecks;

//******************************************************************************

void AccessCheckOptions::Startup()
{
    STANDARD_VM_CONTRACT;

    s_pNormalAccessChecks = new AccessCheckOptions(
                                    AccessCheckOptions::kNormalAccessibilityChecks,
                                    NULL,
                                    FALSE,
                                    (MethodTable *)NULL);
}

//******************************************************************************
AccessCheckOptions::AccessCheckOptions(
    const AccessCheckOptions & templateOptions,
    BOOL                       throwIfTargetIsInaccessible) :
    m_pAccessContext(templateOptions.m_pAccessContext)
{
    WRAPPER_NO_CONTRACT;

    Initialize(
        templateOptions.m_accessCheckType,
        throwIfTargetIsInaccessible,
        templateOptions.m_pTargetMT,
        templateOptions.m_pTargetMethod,
        templateOptions.m_pTargetField);
}

//******************************************************************************
// This function should only be called when normal accessibility is not possible.
// It returns TRUE if the target can be accessed.
// Otherwise, it either returns FALSE or throws an exception, depending on the value of throwIfTargetIsInaccessible.

BOOL AccessCheckOptions::DemandMemberAccess(AccessCheckContext *pContext, MethodTable * pTargetMT, BOOL visibilityCheck) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(m_accessCheckType != kNormalAccessibilityChecks);
        PRECONDITION(CheckPointer(pContext));
    }
    CONTRACTL_END;

    _ASSERTE(m_accessCheckType != kNormalAccessibilityChecks);

    if (NingenEnabled())
    {
        // NinGen should always perform normal accessibility checks
        _ASSERTE(false);

        if (m_fThrowIfTargetIsInaccessible)
        {
            ThrowAccessException(pContext, pTargetMT, NULL);
        }

        return FALSE;
    }

    BOOL canAccessTarget = FALSE;

#ifndef CROSSGEN_COMPILE

    // In CoreCLR kRestrictedMemberAccess means that one can access private/internal
    // classes/members in app code.
    if (m_accessCheckType != kMemberAccess && pTargetMT)
    {
        // We allow all transparency checks to succeed in LCG methods and reflection invocation.
        if (m_accessCheckType == kNormalAccessNoTransparency || m_accessCheckType == kRestrictedMemberAccessNoTransparency)
            return TRUE;
    }

    // No Access
    if (m_fThrowIfTargetIsInaccessible)
    {
        ThrowAccessException(pContext, pTargetMT, NULL);
    }

#endif // CROSSGEN_COMPILE

    return canAccessTarget;
}

//******************************************************************************
// pFailureMT - the MethodTable that we were trying to access. It can be null
//              if the failure is not because of a specific type. This will be a
//              a component of the instantiation of m_pTargetMT/m_pTargetMethod/m_pTargetField.

void AccessCheckOptions::ThrowAccessException(
    AccessCheckContext* pContext,
    MethodTable*        pFailureMT,             /* = NULL  */
    Exception*          pInnerException         /* = NULL  */) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pContext));
        PRECONDITION(CheckPointer(pInnerException, NULL_OK));
        PRECONDITION(m_fThrowIfTargetIsInaccessible);
    }
    CONTRACTL_END;

    GCX_COOP();

    MethodDesc* pCallerMD = pContext->GetCallerMethod();

    if (m_pTargetMT != NULL)
    {
        // If we know the specific type that caused the failure, display it.
        // Else display the whole type that we are trying to access.
        MethodTable * pMT = (pFailureMT != NULL) ? pFailureMT : m_pTargetMT;
        ThrowTypeAccessException(pContext, pMT, 0, pInnerException);
    }
    else if (m_pTargetMethod != NULL)
    {
        // If the caller and target method are non-null and the same, then this means that we're checking to see
        // if the method has access to itself in order to validate that it has access to its parameter types,
        // containing type, and return type.  In this case, throw a more informative TypeAccessException to
        // describe the error that occurred (for instance, "this method doesn't have access to one of its
        // parameter types", rather than "this method doesn't have access to itself").
        // We only want to do this if we know the exact type that caused the problem, otherwise fall back to
        // throwing the standard MethodAccessException.
        if (pCallerMD != NULL && m_pTargetMethod == pCallerMD && pFailureMT != NULL)
        {
            ThrowTypeAccessException(pContext, pFailureMT, 0, pInnerException);
        }
        else
        {
            ThrowMethodAccessException(pContext, m_pTargetMethod, 0, pInnerException);
        }
    }
    else
    {
        _ASSERTE(m_pTargetField != NULL);
        ThrowFieldAccessException(pContext, m_pTargetField, 0, pInnerException);
    }
}

//******************************************************************************
// This will do a security demand if appropriate.
// If access is not possible, this will either throw an exception or return FALSE
BOOL AccessCheckOptions::DemandMemberAccessOrFail(AccessCheckContext *pContext, MethodTable * pTargetMT, BOOL visibilityCheck) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (DoNormalAccessibilityChecks())
    {
        if (pContext->GetCallerAssembly()->IgnoresAccessChecksTo(pTargetMT->GetAssembly()))
        {
            return TRUE;
        }

        if (m_fThrowIfTargetIsInaccessible)
        {
            ThrowAccessException(pContext, pTargetMT);
        }

        return FALSE;
    }

    return DemandMemberAccess(pContext, pTargetMT, visibilityCheck);
}

//******************************************************************************
// This should be called if access to the target is not possible.
// This will either throw an exception or return FALSE.
BOOL AccessCheckOptions::FailOrThrow(AccessCheckContext *pContext) const
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pContext));
    }
    CONTRACTL_END;

    if (m_fThrowIfTargetIsInaccessible)
    {
        ThrowAccessException(pContext);
    }

    return FALSE;
}

void DECLSPEC_NORETURN ThrowFieldAccessException(AccessCheckContext* pContext,
                                                 FieldDesc *pFD,
                                                 UINT messageID /* = 0 */,
                                                 Exception *pInnerException /* = NULL */)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pContext));
        PRECONDITION(CheckPointer(pFD));
    }
    CONTRACTL_END;

    MethodDesc* pCallerMD = pContext->GetCallerMethod();

    ThrowFieldAccessException(pCallerMD,
                              pFD,
                              messageID,
                              pInnerException);
}

void DECLSPEC_NORETURN ThrowFieldAccessException(MethodDesc* pCallerMD,
                                                 FieldDesc *pFD,
                                                 UINT messageID /* = 0 */,
                                                 Exception *pInnerException /* = NULL */)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pCallerMD, NULL_OK));
        PRECONDITION(CheckPointer(pFD));
    }
    CONTRACTL_END;

    if (pCallerMD != NULL)
    {
        if (messageID == 0)
        {
            messageID = IDS_E_FIELDACCESS;
        }

        EX_THROW_WITH_INNER(EEFieldException, (pFD, pCallerMD, SString::Empty(), messageID), pInnerException);
    }
    else
    {
        EX_THROW_WITH_INNER(EEFieldException, (pFD), pInnerException);
    }
}

void DECLSPEC_NORETURN ThrowMethodAccessException(AccessCheckContext* pContext,
                                                  MethodDesc *pCalleeMD,
                                                  UINT messageID /* = 0 */,
                                                  Exception *pInnerException /* = NULL */)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pContext));
        PRECONDITION(CheckPointer(pCalleeMD));
    }
    CONTRACTL_END;

    MethodDesc* pCallerMD = pContext->GetCallerMethod();

    ThrowMethodAccessException(pCallerMD,
                               pCalleeMD,
                               messageID,
                               pInnerException);
}

void DECLSPEC_NORETURN ThrowMethodAccessException(MethodDesc* pCallerMD,
                                                  MethodDesc *pCalleeMD,
                                                  UINT messageID /* = 0 */,
                                                  Exception *pInnerException /* = NULL */)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pCallerMD, NULL_OK));
        PRECONDITION(CheckPointer(pCalleeMD));
    }
    CONTRACTL_END;

    if (pCallerMD != NULL)
    {
        if (messageID == 0)
        {
            messageID = IDS_E_METHODACCESS;
        }

        EX_THROW_WITH_INNER(EEMethodException, (pCalleeMD, pCallerMD, SString::Empty(), messageID), pInnerException);
    }
    else
    {
        EX_THROW_WITH_INNER(EEMethodException, (pCalleeMD), pInnerException);
    }
}

void DECLSPEC_NORETURN ThrowTypeAccessException(AccessCheckContext* pContext,
                                                MethodTable *pMT,
                                                UINT messageID /* = 0 */,
                                                Exception *pInnerException /* = NULL */)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pContext));
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    MethodDesc* pCallerMD = pContext->GetCallerMethod();

    ThrowTypeAccessException(pCallerMD,
                             pMT,
                             messageID,
                             pInnerException);
}

void DECLSPEC_NORETURN ThrowTypeAccessException(MethodDesc* pCallerMD,
                                                MethodTable *pMT,
                                                UINT messageID /* = 0 */,
                                                Exception *pInnerException /* = NULL */)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pCallerMD, NULL_OK));
        PRECONDITION(CheckPointer(pMT));
    }
    CONTRACTL_END;

    if (pCallerMD != NULL)
    {
        if (messageID == 0)
        {
            messageID = IDS_E_TYPEACCESS;
        }

        EX_THROW_WITH_INNER(EETypeAccessException, (pMT, pCallerMD, SString::Empty(), messageID), pInnerException);
    }
    else
    {
        EX_THROW_WITH_INNER(EETypeAccessException, (pMT), pInnerException);
    }
}

//---------------------------------------------------------------------------------------
//
// Checks to see if access to a member with assembly visiblity is allowed.
//
// Arguments:
//    pAccessingAssembly    - The assembly requesting access to the internal member
//    pTargetAssembly       - The assembly which contains the target member
//    pOptionalTargetField  - Internal field being accessed OR
//    pOptionalTargetMethod - Internal type being accessed OR
//    pOptionalTargetType   - Internal type being accessed
//
// Return Value:
//    TRUE if pTargetAssembly is pAccessingAssembly, or if pTargetAssembly allows
//    pAccessingAssembly friend access to the target. FALSE otherwise.
//

static BOOL AssemblyOrFriendAccessAllowed(Assembly       *pAccessingAssembly,
                                          Assembly       *pTargetAssembly,
                                          FieldDesc      *pOptionalTargetField,
                                          MethodDesc     *pOptionalTargetMethod,
                                          MethodTable    *pOptionalTargetType)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pAccessingAssembly));
        PRECONDITION(CheckPointer(pTargetAssembly));
        PRECONDITION(pOptionalTargetField != NULL || pOptionalTargetMethod != NULL || pOptionalTargetType != NULL);
        PRECONDITION(pOptionalTargetField == NULL || pOptionalTargetMethod == NULL);
    }
    CONTRACTL_END;

    if (pAccessingAssembly == pTargetAssembly)
    {
        return TRUE;
    }

    if (pAccessingAssembly->IgnoresAccessChecksTo(pTargetAssembly))
    {
        return TRUE;
    }

    else if (pOptionalTargetField != NULL)
    {
        return pTargetAssembly->GrantsFriendAccessTo(pAccessingAssembly, pOptionalTargetField);
    }
    else if (pOptionalTargetMethod != NULL)
    {
        return pTargetAssembly->GrantsFriendAccessTo(pAccessingAssembly, pOptionalTargetMethod);
    }
    else
    {
        return pTargetAssembly->GrantsFriendAccessTo(pAccessingAssembly, pOptionalTargetType);
    }
}

//******************************************************************************
// This function determines whether a target class is accessible from
//  some given class.
/* static */
BOOL ClassLoader::CanAccessMethodInstantiation( // True if access is legal, false otherwise.
    AccessCheckContext* pContext,
    MethodDesc*         pOptionalTargetMethod,  // The desired method; if NULL, return TRUE (or)
    const AccessCheckOptions & accessCheckOptions)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        MODE_ANY;
        PRECONDITION(CheckPointer(pContext));
    }
    CONTRACTL_END

    // If there is no target method just allow access.
    // NB: the caller may just be checking access to a field or class, so we allow for NULL.
    if (!pOptionalTargetMethod)
        return TRUE;

    // Is the desired target an instantiated generic method?
    if (pOptionalTargetMethod->HasMethodInstantiation())
    {   // check that the current class has access
        // to all of the instantiating classes.
        Instantiation inst = pOptionalTargetMethod->GetMethodInstantiation();
        for (DWORD i = 0; i < inst.GetNumArgs(); i++)
        {
            TypeHandle th = inst[i];

            MethodTable* pMT = th.GetMethodTableOfRootTypeParam();

            // Either a TypeVarTypeDesc or a FnPtrTypeDesc. No access check needed.
            if (pMT == NULL)
                continue;

            if (!CanAccessClass(
                    pContext,
                    pMT,
                    th.GetAssembly(),
                    accessCheckOptions))
            {
                return FALSE;
            }
        }
        //  If we are here, the current class has access to all of the target's instantiating args,
    }
    return TRUE;
}

//******************************************************************************
// This function determines whether a target class is accessible from
//  some given class.
// CanAccessClass does the following checks:
//   1. Transparency check on the target class
//   2. Recursively calls CanAccessClass on the generic arguments of the target class if it is generic.
//   3. Visibility check on the target class, if the target class is nested, this will be translated
//      to a member access check on the enclosing type (calling CanAccess with appropriate dwProtection.
//
/* static */
BOOL ClassLoader::CanAccessClass(                   // True if access is legal, false otherwise.
    AccessCheckContext* pContext,                   // The caller context
    MethodTable*        pTargetClass,               // The desired target class.
    Assembly*           pTargetAssembly,            // Assembly containing the target class.
    const AccessCheckOptions & accessCheckOptions)// = TRUE
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        MODE_ANY;
        PRECONDITION(CheckPointer(pContext));
        PRECONDITION(CheckPointer(pTargetClass));
    }
    CONTRACTL_END

    // If there is no target class, allow access.
    // @todo: what does that mean?
    //if (!pTargetClass)
    //    return TRUE;

    // Step 2: Recursively call CanAccessClass on the generic type arguments
    // Is the desired target a generic instantiation?
    if (pTargetClass->HasInstantiation())
    {   // Yes, so before going any further, check that the current class has access
        //  to all of the instantiating classes.
        Instantiation inst = pTargetClass->GetInstantiation();
        for (DWORD i = 0; i < inst.GetNumArgs(); i++)
        {
            TypeHandle th = inst[i];

            MethodTable* pMT = th.GetMethodTableOfRootTypeParam();

            // Either a TypeVarTypeDesc or a FnPtrTypeDesc. No access check needed.
            if (pMT == NULL)
                continue;

            if (!CanAccessClass(
                    pContext,
                    pMT,
                    th.GetAssembly(),
                    accessCheckOptions))
            {
                // no need to call accessCheckOptions.DemandMemberAccessOrFail here because the base case in
                // CanAccessClass does that already
                return FALSE;
            }
        }
        // If we are here, the current class has access to all of the desired target's instantiating args.
        //  Now, check whether the current class has access to the desired target itself.
    }

    // Step 3: Visibility Check
    if (!pTargetClass->GetClass()->IsNested())
    {   // a non-nested class can be either all public or accessible only from its own assembly (and friends).
        if (IsTdPublic(pTargetClass->GetClass()->GetProtection()))
        {
            return TRUE;
        }
        else
        {
            Assembly* pCurrentAssembly = pContext->GetCallerAssembly();
            _ASSERTE(pCurrentAssembly != NULL);

            if (AssemblyOrFriendAccessAllowed(pCurrentAssembly,
                                              pTargetAssembly,
                                              NULL,
                                              NULL,
                                              pTargetClass))
            {
                return TRUE;
            }
            else
            {
                return accessCheckOptions.DemandMemberAccessOrFail(pContext, pTargetClass, TRUE /*visibilityCheck*/);
            }
        }
    }

    // If we are here, the desired target class is nested.  Translate the type flags
    //  to corresponding method access flags. We need to make a note if friend access was allowed to the
    //  type being checked since we're not passing it directly to the recurisve call to CanAccess, and
    //  instead are just passing in the dwProtectionFlags.
    DWORD dwProtection = pTargetClass->GetClass()->GetProtection();

    switch(dwProtection) {
        case tdNestedPublic:
            dwProtection = mdPublic;
            break;
        case tdNestedFamily:
            dwProtection = mdFamily;
            break;
        case tdNestedPrivate:
            dwProtection = mdPrivate;
            break;
        case tdNestedFamORAssem:
            // If we can access the class because we have assembly or friend access, we have satisfied the
            // FamORAssem accessibility, so we we can simplify it down to public. Otherwise we require that
            // family access be allowed to grant access.
        case tdNestedFamANDAssem:
            // If we don't grant assembly or friend access to the target class, then there is no way we
            // could satisfy the FamANDAssem requirement.  Otherwise, since we have satsified the Assm
            // portion, we only need to check for the Fam portion.
        case tdNestedAssembly:
            // If we don't grant assembly or friend access to the target class, and that class has assembly
            // protection, we can fail the request now.  Otherwise we can check to make sure a public member
            // of the outer class is allowed, since we have satisfied the target's accessibility rules.

            if (AssemblyOrFriendAccessAllowed(pContext->GetCallerAssembly(), pTargetAssembly, NULL, NULL, pTargetClass))
                dwProtection = (dwProtection == tdNestedFamANDAssem) ? mdFamily : mdPublic;
            else if (dwProtection == tdNestedFamORAssem)
                dwProtection = mdFamily;
            else
                return accessCheckOptions.DemandMemberAccessOrFail(pContext, pTargetClass, TRUE /*visibilityCheck*/);

            break;

        default:
            THROW_BAD_FORMAT_MAYBE(!"Unexpected class visibility flag value", BFA_BAD_VISIBILITY, pTargetClass);
    }

    // The desired target class is nested, so translate the class access request into
    //  a member access request.  That is, if the current class is trying to access A::B,
    //  check if it can access things in A with the visibility of B.
    // So, pass A as the desired target class and visibility of B within A as the member access
    // We've already done transparency check above. No need to do it again.
    return ClassLoader::CanAccess(
        pContext,
        GetEnclosingMethodTable(pTargetClass),
        pTargetAssembly,
        dwProtection,
        NULL,
        NULL,
        accessCheckOptions);
} // BOOL ClassLoader::CanAccessClass()

//******************************************************************************
// This is a front-end to CheckAccessMember that handles the nested class scope. If can't access
// from the current point and are a nested class, then try from the enclosing class.
// In addition to CanAccessMember, if the caller class doesn't have access to the caller, see if the enclosing class does.
//
/* static */
BOOL ClassLoader::CanAccess(                            // TRUE if access is allowed, FALSE otherwise.
    AccessCheckContext* pContext,                       // The caller context
    MethodTable*        pTargetMT,                      // The class containing the desired target member.
    Assembly*           pTargetAssembly,                // Assembly containing that class.
    DWORD               dwMemberAccess,                 // Member access flags of the desired target member (as method bits).
    MethodDesc*         pOptionalTargetMethod,          // The target method; NULL if the target is a not a method or
                                                        // there is no need to check the method's instantiation.
    FieldDesc*          pOptionalTargetField,           // or The desired field; if NULL, return TRUE
    const AccessCheckOptions & accessCheckOptions)      // = s_NormalAccessChecks
{
    CONTRACT(BOOL)
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(pContext));
        MODE_ANY;
    }
    CONTRACT_END;

    AccessCheckOptions accessCheckOptionsNoThrow(accessCheckOptions, FALSE);

    if (!CheckAccessMember(pContext,
                           pTargetMT,
                           pTargetAssembly,
                           dwMemberAccess,
                           pOptionalTargetMethod,
                           pOptionalTargetField,
                           // Suppress exceptions for nested classes since this is not a hard-failure,
                           // and we can do additional checks
                           accessCheckOptionsNoThrow))
    {
        // If we're here, CheckAccessMember didn't allow access.
        BOOL canAccess = FALSE;

        // If the current class is nested, there may be an enclosing class that might have access
        // to the target. And if the pCurrentMT == NULL, the current class is global, and so there
        // is no enclosing class.
        MethodTable* pCurrentMT = pContext->GetCallerMT();

        BOOL isNestedClass = (pCurrentMT && pCurrentMT->GetClass()->IsNested());

        if (isNestedClass)
        {
            // A nested class also has access to anything that the enclosing class does, so
            //  recursively check whether the enclosing class can access the desired target member.
            MethodTable * pEnclosingMT = GetEnclosingMethodTable(pCurrentMT);

            AccessCheckContext accessContext(pContext->GetCallerMethod(),
                                                   pEnclosingMT,
                                                   pContext->GetCallerAssembly());

            // On failure, do not throw from inside this call since that will cause the exception message
            // to refer to the enclosing type.
            canAccess = ClassLoader::CanAccess(
                                 &accessContext,
                                 pTargetMT,
                                 pTargetAssembly,
                                 dwMemberAccess,
                                 pOptionalTargetMethod,
                                 pOptionalTargetField,
                                 accessCheckOptionsNoThrow);
        }

        if (!canAccess)
        {
            BOOL fail = accessCheckOptions.FailOrThrow(pContext);
            RETURN(fail);
        }
    }

    RETURN(TRUE);
} // BOOL ClassLoader::CanAccess()

//******************************************************************************
// This is the helper function for the corresponding CanAccess()
// It does the following checks:
//   1. CanAccessClass on pTargetMT
//   2. CanAccessMethodInstantiation if the pOptionalTargetMethod is provided and is generic.
//   3. Transparency check on pTargetMT, pOptionalTargetMethod and pOptionalTargetField.
//   4. Visibility check on dwMemberAccess (on pTargetMT)

/* static */
BOOL ClassLoader::CheckAccessMember(                // TRUE if access is allowed, false otherwise.
    AccessCheckContext*     pContext,
    MethodTable*            pTargetMT,              // The class containing the desired target member.
    Assembly*               pTargetAssembly,        // Assembly containing that class.
    DWORD                   dwMemberAccess,         // Member access flags of the desired target member (as method bits).
    MethodDesc*             pOptionalTargetMethod,  // The target method; NULL if the target is a not a method or
                                                    // there is no need to check the method's instantiation.
    FieldDesc*              pOptionalTargetField,   // target field, NULL if there is no Target field
    const AccessCheckOptions & accessCheckOptions
    )
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(pContext));
        MODE_ANY;
    }
    CONTRACTL_END

    // we're trying to access a member that is contained in the class pTargetClass, so need to
    // check if have access to pTargetClass itself from the current point before worry about
    // having access to the member within the class
    if (!CanAccessClass(pContext,
                        pTargetMT,
                        pTargetAssembly,
                        accessCheckOptions))
    {
        return FALSE;
    }

    // If we are trying to access a generic method, we have to ensure its instantiation is accessible.
    // Note that we need to perform transparency checks on the instantiation even if we have
    if (!CanAccessMethodInstantiation(
            pContext,
            pOptionalTargetMethod,
            accessCheckOptions))
    {
        return FALSE;
    }

    // pOptionalTargetMethod and pOptionalTargetField can never be NULL at the same time.
    _ASSERTE(pOptionalTargetMethod == NULL || pOptionalTargetField == NULL);

    // Perform transparency checks
    // We don't need to do transparency check against pTargetMT here because
    // it was already done in CanAccessClass above.

    if (IsMdPublic(dwMemberAccess))
    {
        return TRUE;
    }

    MethodTable* pCurrentMT = pContext->GetCallerMT();

    if (IsMdPrivateScope(dwMemberAccess))
    {
        if (pCurrentMT != NULL && pCurrentMT->GetModule() == pTargetMT->GetModule())
        {
            return TRUE;
        }
        else
        {
            return accessCheckOptions.DemandMemberAccessOrFail(pContext, pTargetMT, TRUE /*visibilityCheck*/);
        }
    }


#ifdef _DEBUG
    if (pTargetMT == NULL &&
        (IsMdFamORAssem(dwMemberAccess) ||
         IsMdFamANDAssem(dwMemberAccess) ||
         IsMdFamily(dwMemberAccess))) {
        THROW_BAD_FORMAT_MAYBE(!"Family flag is not allowed on global functions", BFA_FAMILY_ON_GLOBAL, pTargetMT);
    }
#endif

    if (pTargetMT == NULL ||
        IsMdAssem(dwMemberAccess) ||
        IsMdFamORAssem(dwMemberAccess) ||
        IsMdFamANDAssem(dwMemberAccess))
    {
        // If the member has Assembly accessibility, grant access if the current
        //  class is in the same assembly as the desired target member, or if the
        //  desired target member's assembly grants friend access to the current
        //  assembly.
        // @todo: What does it mean for the target class to be NULL?

        Assembly* pCurrentAssembly = pContext->GetCallerAssembly();

        // pCurrentAssembly should never be NULL, unless we are called from interop,
        // in which case we should have already returned TRUE.
        _ASSERTE(pCurrentAssembly != NULL);

        const BOOL fAssemblyOrFriendAccessAllowed = AssemblyOrFriendAccessAllowed(pCurrentAssembly,
                                                                                  pTargetAssembly,
                                                                                  pOptionalTargetField,
                                                                                  pOptionalTargetMethod,
                                                                                  pTargetMT);

        if ((pTargetMT == NULL || IsMdAssem(dwMemberAccess) || IsMdFamORAssem(dwMemberAccess)) &&
            fAssemblyOrFriendAccessAllowed)
        {
            return TRUE;
        }
        else if (IsMdFamANDAssem(dwMemberAccess) &&
                 !fAssemblyOrFriendAccessAllowed)
        {
            return accessCheckOptions.DemandMemberAccessOrFail(pContext, pTargetMT, TRUE /*visibilityCheck*/);
        }
    }

    // Nested classes can access all members of the parent class.
    while(pCurrentMT != NULL)
    {
        //@GENERICSVER:
        if (pTargetMT->HasSameTypeDefAs(pCurrentMT))
            return TRUE;

        if (IsMdPrivate(dwMemberAccess))
        {
            if (!pCurrentMT->GetClass()->IsNested())
            {
                return accessCheckOptions.DemandMemberAccessOrFail(pContext, pTargetMT, TRUE /*visibilityCheck*/);
            }
        }
        else if (IsMdFamORAssem(dwMemberAccess) || IsMdFamily(dwMemberAccess) || IsMdFamANDAssem(dwMemberAccess))
        {
            if (CanAccessFamily(pCurrentMT, pTargetMT))
            {
                return TRUE;
            }
        }

        pCurrentMT = GetEnclosingMethodTable(pCurrentMT);
    }

    return accessCheckOptions.DemandMemberAccessOrFail(pContext, pTargetMT, TRUE /*visibilityCheck*/);
}

// The family check is actually in two parts (Partition I, 8.5.3.2).  The first part:
//
//              ...accessible to referents that support the same type
//              (i.e., an exact type and all of the types that inherit
//              from it).
//
// Translation: pCurrentClass must be the same type as pTargetClass or a derived class.  (i.e. Derived
// can access Base.protected but Unrelated cannot access Base.protected).
//
// The second part:
//
//              For verifiable code (see Section 8.8), there is an additional
//              requirement that can require a runtime check: the reference
//              shall be made through an item whose exact type supports
//              the exact type of the referent. That is, the item whose
//              member is being accessed shall inherit from the type
//              performing the access.
//
// Translation: The C++ protected rule.  For those unfamiliar, it means that:
//  if you have:
//  GrandChild : Child
//      and
//  Child : Parent
//      and
//  Parent {
//  protected:
//      int protectedField;
//  }
//
//  Child::function(GrandChild * o) {
//      o->protectedField; //This access is legal.
//  }
//
//  GrandChild:function2(Child * o) {
//      o->protectedField; //This access is illegal.
//  }
//
//  The reason for this rule is that if you had:
//  Sibling : Parent
//
//  Child::function3( Sibling * o ) {
//      o->protectedField; //This access is illegal
//  }
//
//  This is intuitively correct.  However, you need to prevent:
//  Child::function4( Sibling * o ) {
//      ((Parent*)o)->protectedField;
//  }
//
//  Which means that you must access protected fields through a type that is yourself or one of your
//  derived types.

//This checks the first part of the rule above.
/* static */
BOOL ClassLoader::CanAccessFamily(
                                 MethodTable *pCurrentClass,
                                 MethodTable *pTargetClass)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        MODE_ANY;
        PRECONDITION(CheckPointer(pTargetClass));
    }
    CONTRACTL_END

    _ASSERTE(pCurrentClass);
    _ASSERTE(pTargetClass);

    BOOL bIsInterface = pTargetClass->IsInterface();

    //Look to see if Current is a child of the Target.
    while (pCurrentClass) {
        if (bIsInterface)
        {
            // Calling a protected interface member
            MethodTable::InterfaceMapIterator it = pCurrentClass->IterateInterfaceMap();
            while (it.Next())
            {
                // We only loosely check if they are of the same generic type
                if (it.GetInterface()->HasSameTypeDefAs(pTargetClass))
                    return TRUE;
            }
        }
        else
        {
            MethodTable *pCurInstance = pCurrentClass;

            while (pCurInstance) {
                //This is correct.  csc is incredibly lax about generics.  Essentially if you are a subclass of
                //any type of generic it lets you access it.  Since the standard is totally unclear, mirror that
                //behavior here.
                if (pCurInstance->HasSameTypeDefAs(pTargetClass)) {
                    return TRUE;
                }

                pCurInstance = pCurInstance->GetParentMethodTable();
            }
        }

        ///Looking at 8.5.3, it looks like a protected member of a nested class in a parent type is also
        //accessible.
        pCurrentClass = GetEnclosingMethodTable(pCurrentClass);
    }

    return FALSE;
}

//If instance is an inner class, this also succeeds if the outer class conforms to 8.5.3.2.  A nested class
//is enclosed inside of the enclosing class' open type.  So we need to ignore generic variables.  That also
//helps us with:
/*
class Base {
    protected int m_family;
}
class Derived<T> : Base {
    class Inner {
        public int function(Derived<T> d) {
            return d.m_family;
        }
    }
}
*/

//Since the inner T is not the same T as the enclosing T (since accessing generic variables is a CLS rule,
//not a CLI rule), we see that as a comparison between Derived<T> and Derived<T'>.  CanCastTo rejects that.
//Instead we just check against the typedef of the two types.  This ignores all generic parameters (formal
//or not).

BOOL CanAccessFamilyVerificationEnclosingHelper(MethodTable * pMTCurrentEnclosingClass,
                                                TypeHandle thInstanceClass)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END

    _ASSERTE(pMTCurrentEnclosingClass);

    if (thInstanceClass.IsGenericVariable())
    {
        //In this case it is a TypeVarTypeDesc (i.e. T).  If this access would be legal due to a
        //constraint:
        //
        /*
        public class My<T>
        {
            public class Inner<U> where U : My<T>
            {
                public int foo(U u)
                {
                    return u.field;
                }
            }
            protected int field;
        }
        */
        //We need to find the generic class constraint.  (The above is legal because U must be a My<T> which makes this
        //legal by 8.5.3.2)
        // There may only be 1 class constraint on a generic parameter

        // Get the constraints on this generic variable
        // At most 1 of them is a class constraint.
        // That class constraint methodtable can go through the normal search for matching typedef logic below
        TypeVarTypeDesc *tyvar = thInstanceClass.AsGenericVariable();
        DWORD numConstraints;
        TypeHandle *constraints = tyvar->GetConstraints(&numConstraints, CLASS_DEPENDENCIES_LOADED);
        if (constraints == NULL)
        {
            // If we did not find a class constraint, we cannot generate a methodtable to search for
            return FALSE;
        }
        else
        {
            for (DWORD i = 0; i < numConstraints; i++)
            {
                if (!constraints[i].IsInterface())
                {
                    // We have found the class constraint on this TypeVarTypeDesc
                    // Recurse on the found class constraint. It is possible that this constraint may also be a TypeVarTypeDesc
//class Outer4<T>
//{
//    protected int field;
//
//    public class Inner<U,V> where V:U where U : Outer4<T>
//    {
//        public int Method(V param) { return (++param.field); }
//    }
//}
                    return CanAccessFamilyVerificationEnclosingHelper(pMTCurrentEnclosingClass, constraints[i]);
                }
            }
            // If we did not find a class constraint, we cannot generate a methodtable to search for
            return FALSE;
        }
    }
    do
    {
        MethodTable * pAccessor = pMTCurrentEnclosingClass;
        //If thInstanceClass is a MethodTable, we should only be doing the TypeDef comparison (see
        //above).
        if (!thInstanceClass.IsTypeDesc())
        {
            MethodTable *pInstanceMT = thInstanceClass.AsMethodTable();

            // This is a CanCastTo implementation for classes, assuming we should ignore generic instantiation parameters.
            do
            {
                if (pAccessor->HasSameTypeDefAs(pInstanceMT))
                    return TRUE;
                pInstanceMT = pInstanceMT->GetParentMethodTable();
            }while(pInstanceMT);
        }
        else
        {
            // Leave this logic in place for now, as I'm not fully confident it can't happen, and we are very close to RTM
            // This logic was originally written to handle TypeVarTypeDescs, but those are now handled above.
            _ASSERTE(FALSE);
            if (thInstanceClass.CanCastTo(TypeHandle(pAccessor)))
                return TRUE;
        }

        pMTCurrentEnclosingClass = GetEnclosingMethodTable(pMTCurrentEnclosingClass);
    }while(pMTCurrentEnclosingClass);
    return FALSE;
}


//This checks the verification only part of the rule above.
//From the example above:
//  GrandChild::function2(Child * o) {
//      o->protectedField; //This access is illegal.
//  }
// pCurrentClass is GrandChild and pTargetClass is Child.  This check is completely unnecessary for statics,
// but by legacy convention you can use GrandChild for pTargetClass in that case.

BOOL ClassLoader::CanAccessFamilyVerification(TypeHandle thCurrentClass,
                                              TypeHandle thInstanceClass)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM(););
        MODE_ANY;
        PRECONDITION(!thCurrentClass.IsNull());
        PRECONDITION(!thCurrentClass.IsTypeDesc());
    }
    CONTRACTL_END

    //Check to see if Instance is equal to or derived from pCurrentClass.
    //
    //In some cases the type we have for the instance type is actually a TypeVarTypeDesc.  In those cases we
    //need to check against the constraints (You're accessing a member through a 'T' with a type constraint
    //that makes this legal).  For those cases, CanCastTo does what I want.
    MethodTable * pAccessor = thCurrentClass.GetMethodTable();
    if (thInstanceClass.CanCastTo(TypeHandle(pAccessor)))
        return TRUE;

    //TypeDescs don't have methods so only run this on MethodTables.
    if (!thInstanceClass.IsNull())
    {
        return CanAccessFamilyVerificationEnclosingHelper(pAccessor, thInstanceClass);
    }
    return FALSE;
}

#endif // #ifndef DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void
ClassLoader::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    DAC_ENUM_DTHIS();

    EMEM_OUT(("MEM: %p ClassLoader\n", dac_cast<TADDR>(this)));

    if (m_pAssembly.IsValid())
    {
        ModuleIterator modIter = GetAssembly()->IterateModules();

        while (modIter.Next())
        {
            modIter.GetModule()->EnumMemoryRegions(flags, true);
        }
    }
}

#endif // #ifdef DACCESS_COMPILE
