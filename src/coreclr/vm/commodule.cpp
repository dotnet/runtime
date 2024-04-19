// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"
#include "commodule.h"
#include "comdynamic.h"
#include "reflectclasswriter.h"
#include "class.h"
#include "ceesectionstring.h"
#include <cor.h>
#include "typeparse.h"
#include "typekey.h"


//**************************************************
// GetTypeRef
// This function will return the type token given full qual name. If the type
// is defined locally, we will return the TypeDef token. Or we will return a TypeRef token
// with proper resolution scope calculated.
// wszFullName is escaped (TYPE_NAME_RESERVED_CHAR). It should not be byref or contain enclosing type name,
// assembly name, and generic argument list.
//**************************************************
extern "C" mdTypeRef QCALLTYPE ModuleBuilder_GetTypeRef(QCall::ModuleHandle pModule,
                                          LPCWSTR wszFullName,
                                          QCall::ModuleHandle pRefedModule,
                                          INT32 tkResolutionArg)
{
    QCALL_CONTRACT;

    mdTypeRef tr = 0;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    IMetaDataEmit * pEmit = pRCW->GetEmitter();
    IMetaDataImport * pImport = pRCW->GetRWImporter();

    if (wszFullName == NULL) {
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_String"));
    }

    InlineSString<128> ssNameUnescaped;
    LPCWSTR wszTemp = wszFullName;

    WCHAR c;
    while(0 != (c = *wszTemp++))
    {
        if ( c == W('\\') &&
             IsTypeNameReservedChar(*wszTemp) )
        {
            ssNameUnescaped.Append(*wszTemp++);
        }
        else
        {
            _ASSERTE( ! IsTypeNameReservedChar(c) );
            ssNameUnescaped.Append(c);
        }
    }

    LPCWSTR wszFullNameUnescaped = ssNameUnescaped.GetUnicode();

    Assembly * pThisAssembly = pModule->GetClassLoader()->GetAssembly();
    Assembly * pRefedAssembly = pRefedModule->GetClassLoader()->GetAssembly();

    if (pModule == pRefedModule)
    {
        // referenced type is from the same module so we must be able to find a TypeDef.
        IfFailThrow(pImport->FindTypeDefByName(
            wszFullNameUnescaped,
            RidFromToken(tkResolutionArg) ? tkResolutionArg : mdTypeDefNil,
            &tr));
    }
    else
    {
        mdToken tkResolution = mdTokenNil;
        if (RidFromToken(tkResolutionArg))
        {
            // reference to nested type
            tkResolution = tkResolutionArg;
        }
        else
        {
            // reference to top level type

            SafeComHolderPreemp<IMetaDataAssemblyEmit> pAssemblyEmit;

            // Generate AssemblyRef
            IfFailThrow( pEmit->QueryInterface(IID_IMetaDataAssemblyEmit, (void **) &pAssemblyEmit) );
            tkResolution = pThisAssembly->AddAssemblyRef(pRefedAssembly, pAssemblyEmit);

            // Add the assembly ref token and the manifest module it is referring to this module's rid map.
            // This is needed regardless of whether the dynamic assembly has run access. Even in Save-only
            // or Refleciton-only mode, CreateType() of the referencing type may still need the referenced
            // type to be resolved and loaded, e.g. if the referencing type is a subclass of the referenced type.
            //
            // Don't cache if there is assembly associated with the token already. The assembly ref resolution
            // can be ambiguous because of reflection emit does not require unique assembly names.
            // We always let the first association win. Ideally, we would disallow this situation by throwing
            // exception, but that would be a breaking change.
            if(pModule->LookupAssemblyRef(tkResolution) == NULL)
            {
                pModule->ForceStoreAssemblyRef(tkResolution, pRefedAssembly);
            }
        }

        IfFailThrow( pEmit->DefineTypeRefByName(tkResolution, wszFullNameUnescaped, &tr) );
    }

    END_QCALL;

    return tr;
}


/*=============================GetArrayMethodToken==============================
**Action:
**Returns:
**Arguments: REFLECTMODULEBASEREF refThis
**           U1ARRAYREF     sig
**           STRINGREF      methodName
**           int            tkTypeSpec
**Exceptions:
==============================================================================*/
extern "C" INT32 QCALLTYPE ModuleBuilder_GetArrayMethodToken(QCall::ModuleHandle pModule,
                                               INT32 tkTypeSpec,
                                               LPCWSTR wszMethodName,
                                               LPCBYTE pSignature,
                                               INT32 sigLength)
{
    QCALL_CONTRACT;

    mdMemberRef memberRefE = mdTokenNil;

    BEGIN_QCALL;

    if (!wszMethodName)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_String"));
    if (!tkTypeSpec)
        COMPlusThrow(kArgumentNullException, W("ArgumentNull_Type"));

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    HRESULT hr = pRCW->GetEmitter()->DefineMemberRef(tkTypeSpec, wszMethodName, (PCCOR_SIGNATURE)pSignature, sigLength, &memberRefE);
    if (FAILED(hr))
    {
        _ASSERTE(!"Failed on DefineMemberRef");
        COMPlusThrowHR(hr);
    }

    END_QCALL;

    return (INT32)memberRefE;
}

namespace
{
    //******************************************************************************
    //
    // Return a TypeRef token given a TypeDef token from the same emit scope
    //
    //******************************************************************************
    void DefineTypeRefHelper(
        IMetaDataEmit       *pEmit,         // given emit scope
        mdTypeDef           td,             // given typedef in the emit scope
        mdTypeRef           *ptr)           // return typeref
    {
        CONTRACTL  {
            STANDARD_VM_CHECK;

            PRECONDITION(CheckPointer(pEmit));
            PRECONDITION(CheckPointer(ptr));
        }
        CONTRACTL_END;

        CQuickBytes qb;
        WCHAR* szTypeDef = (WCHAR*) qb.AllocThrows((MAX_CLASSNAME_LENGTH+1) * sizeof(WCHAR));
        mdToken             rs;             // resolution scope
        DWORD               dwFlags;

        SafeComHolder<IMetaDataImport> pImport;
        IfFailThrow( pEmit->QueryInterface(IID_IMetaDataImport, (void **)&pImport) );
        IfFailThrow( pImport->GetTypeDefProps(td, szTypeDef, MAX_CLASSNAME_LENGTH, NULL, &dwFlags, NULL) );
        if ( IsTdNested(dwFlags) )
        {
            mdToken         tdNested;
            IfFailThrow( pImport->GetNestedClassProps(td, &tdNested) );
            DefineTypeRefHelper( pEmit, tdNested, &rs);
        }
        else
            rs = TokenFromRid( 1, mdtModule );

        IfFailThrow( pEmit->DefineTypeRefByName( rs, szTypeDef, ptr) );
    }   // DefineTypeRefHelper
}

//******************************************************************************
//
// GetMemberRefToken
// This function will return a MemberRef token given a MethodDef token and the module where the MethodDef/FieldDef is defined.
//
//******************************************************************************
extern "C" INT32 QCALLTYPE ModuleBuilder_GetMemberRef(QCall::ModuleHandle pModule, QCall::ModuleHandle pRefedModule, INT32 tr, INT32 token)
{
    QCALL_CONTRACT;

    mdMemberRef             memberRefE      = 0;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE( pRCW );

    LPCUTF8         szName;
    ULONG           cbComSig;
    PCCOR_SIGNATURE pvComSig;

    if (TypeFromToken(token) == mdtMethodDef)
    {
        IfFailThrow(pRefedModule->GetMDImport()->GetNameOfMethodDef(token, &szName));
        IfFailThrow(pRefedModule->GetMDImport()->GetSigOfMethodDef(token, &cbComSig, &pvComSig));
    }
    else
    {
        IfFailThrow(pRefedModule->GetMDImport()->GetNameOfFieldDef(token, &szName));
        IfFailThrow(pRefedModule->GetMDImport()->GetSigOfFieldDef(token, &cbComSig, &pvComSig));
    }

    MAKE_WIDEPTR_FROMUTF8(wzName, szName);

    // Translate the method sig into this scope
    //
    Assembly * pRefedAssembly = pRefedModule->GetAssembly();
    Assembly * pRefingAssembly = pModule->GetAssembly();

    if (pRefedAssembly->IsCollectible() && pRefedAssembly != pRefingAssembly)
    {
        if (pRefingAssembly->IsCollectible())
            pRefingAssembly->GetLoaderAllocator()->EnsureReference(pRefedAssembly->GetLoaderAllocator());
        else
            COMPlusThrow(kNotSupportedException, W("NotSupported_CollectibleBoundNonCollectible"));
    }

    SafeComHolderPreemp<IMetaDataAssemblyEmit> pAssemblyEmit;
    IfFailThrow( pRefingAssembly->GetModule()->GetEmitter()->QueryInterface(IID_IMetaDataAssemblyEmit, (void **) &pAssemblyEmit) );

    CQuickBytes             qbNewSig;
    ULONG                   cbNewSig;

    IfFailThrow( pRefedModule->GetMDImport()->TranslateSigWithScope(
        pRefedAssembly->GetMDImport(),
        NULL, 0,        // hash value
        pvComSig,
        cbComSig,
        pAssemblyEmit,  // Emit assembly scope.
        pRCW->GetEmitter(),
        &qbNewSig,
        &cbNewSig) );

    mdTypeRef               tref;

    if (TypeFromToken(tr) == mdtTypeDef)
    {
        // define a TypeRef using the TypeDef
        DefineTypeRefHelper(pRCW->GetEmitter(), tr, &tref);
    }
    else
        tref = tr;

    // Define the memberRef
    IfFailThrow( pRCW->GetEmitter()->DefineMemberRef(tref, wzName, (PCCOR_SIGNATURE) qbNewSig.Ptr(), cbNewSig, &memberRefE) );

    END_QCALL;

    // assign output parameter
    return (INT32)memberRefE;
}

//******************************************************************************
//
// Return a MemberRef token given a RuntimeMethodInfo
//
//******************************************************************************
extern "C" INT32 QCALLTYPE ModuleBuilder_GetMemberRefOfMethodInfo(QCall::ModuleHandle pModule, INT32 tr, MethodDesc * pMeth)
{
    QCALL_CONTRACT;

    mdMemberRef memberRefE = 0;

    BEGIN_QCALL;

    if (!pMeth)
        COMPlusThrow(kArgumentNullException);

    // Otherwise, we want to return memberref token.
    if (pMeth->IsArray())
    {
        _ASSERTE(!"Should not have come here!");
        COMPlusThrow(kNotSupportedException);
    }

    if (pMeth->GetMethodTable()->GetModule() == pModule)
    {
        // If the passed in method is defined in the same module, just return the MethodDef token
        memberRefE = pMeth->GetMemberDef();
    }
    else
    {
        RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
        _ASSERTE(pRCW);

        LPCUTF8 szName;
        IfFailThrow(pMeth->GetMDImport()->GetNameOfMethodDef(pMeth->GetMemberDef(), &szName));

        ULONG           cbComSig;
        PCCOR_SIGNATURE pvComSig;
        IfFailThrow(pMeth->GetMDImport()->GetSigOfMethodDef(pMeth->GetMemberDef(), &cbComSig, &pvComSig));

        // Translate the method sig into this scope
        Assembly * pRefedAssembly = pMeth->GetModule()->GetAssembly();
        Assembly * pRefingAssembly = pModule->GetAssembly();

        SafeComHolderPreemp<IMetaDataAssemblyEmit> pAssemblyEmit;
        IfFailThrow( pRefingAssembly->GetModule()->GetEmitter()->QueryInterface(IID_IMetaDataAssemblyEmit, (void **) &pAssemblyEmit) );

        CQuickBytes     qbNewSig;
        ULONG           cbNewSig;

        if (pRefedAssembly->IsCollectible() && pRefedAssembly != pRefingAssembly)
        {
            if (pRefingAssembly->IsCollectible())
                pRefingAssembly->GetLoaderAllocator()->EnsureReference(pRefedAssembly->GetLoaderAllocator());
            else
                COMPlusThrow(kNotSupportedException, W("NotSupported_CollectibleBoundNonCollectible"));
        }

        IfFailThrow( pMeth->GetMDImport()->TranslateSigWithScope(
            pRefedAssembly->GetMDImport(),
            NULL, 0,        // hash blob value
            pvComSig,
            cbComSig,
            pAssemblyEmit,  // Emit assembly scope.
            pRCW->GetEmitter(),
            &qbNewSig,
            &cbNewSig) );

        // translate the name to unicode string
        MAKE_WIDEPTR_FROMUTF8(wszName, szName);

        // Define the memberRef
        IfFailThrow( pRCW->GetEmitter()->DefineMemberRef(tr, wszName, (PCCOR_SIGNATURE) qbNewSig.Ptr(), cbNewSig, &memberRefE) );
    }

    END_QCALL;

    return memberRefE;
}


//******************************************************************************
//
// Return a MemberRef token given a RuntimeFieldInfo
//
//******************************************************************************
extern "C" mdMemberRef QCALLTYPE ModuleBuilder_GetMemberRefOfFieldInfo(QCall::ModuleHandle pModule, mdTypeDef tr, QCall::TypeHandle th, mdFieldDef tkField)
{
    QCALL_CONTRACT;

    mdMemberRef memberRefE = 0;

    BEGIN_QCALL;

    if (TypeFromToken(tr) == mdtTypeDef)
    {
        // If the passed in method is defined in the same module, just return the FieldDef token
        memberRefE = tkField;
    }
    else
    {
        TypeHandle typeHandle = th.AsTypeHandle();

        RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
        _ASSERTE(pRCW);

        // get the field name and sig
        Module * pRefedModule = typeHandle.GetModule();
        IMDInternalImport * pRefedMDImport = pRefedModule->GetMDImport();

        LPCUTF8 szName;
        IfFailThrow(pRefedMDImport->GetNameOfFieldDef(tkField, &szName));

        ULONG           cbComSig;
        PCCOR_SIGNATURE pvComSig;
        IfFailThrow(pRefedMDImport->GetSigOfFieldDef(tkField, &cbComSig, &pvComSig));

        // translate the name to unicode string
        MAKE_WIDEPTR_FROMUTF8(wszName, szName);

        Assembly * pRefedAssembly = pRefedModule->GetAssembly();
        Assembly * pRefingAssembly = pModule->GetAssembly();

        if (pRefedAssembly->IsCollectible() && pRefedAssembly != pRefingAssembly)
        {
            if (pRefingAssembly->IsCollectible())
                pRefingAssembly->GetLoaderAllocator()->EnsureReference(pRefedAssembly->GetLoaderAllocator());
            else
                COMPlusThrow(kNotSupportedException, W("NotSupported_CollectibleBoundNonCollectible"));
        }
        SafeComHolderPreemp<IMetaDataAssemblyEmit> pAssemblyEmit;
        IfFailThrow( pRefingAssembly->GetModule()->GetEmitter()->QueryInterface(IID_IMetaDataAssemblyEmit, (void **) &pAssemblyEmit) );

        // Translate the field signature this scope
        CQuickBytes     qbNewSig;
        ULONG           cbNewSig;

        IfFailThrow( pRefedMDImport->TranslateSigWithScope(
        pRefedAssembly->GetMDImport(),
        NULL, 0,            // hash value
        pvComSig,
        cbComSig,
        pAssemblyEmit,      // Emit assembly scope.
        pRCW->GetEmitter(),
        &qbNewSig,
        &cbNewSig) );

        IfFailThrow( pRCW->GetEmitter()->DefineMemberRef(tr, wszName, (PCCOR_SIGNATURE) qbNewSig.Ptr(), cbNewSig, &memberRefE) );
    }

    END_QCALL;

    return memberRefE;
}

//******************************************************************************
//
// Return a MemberRef token given a Signature
//
//******************************************************************************
extern "C" INT32 QCALLTYPE ModuleBuilder_GetMemberRefFromSignature(QCall::ModuleHandle pModule,
                                                     INT32 tr,
                                                     LPCWSTR wszMemberName,
                                                     LPCBYTE pSignature,
                                                     INT32 sigLength)
{
    QCALL_CONTRACT;

    mdMemberRef     memberRefE = mdTokenNil;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    IfFailThrow( pRCW->GetEmitter()->DefineMemberRef(tr,
                                                     wszMemberName,
                                                     pSignature,
                                                     sigLength,
                                                     &memberRefE) );

    END_QCALL;

    return memberRefE;
}

//******************************************************************************
//
// SetFieldRVAContent
// This function is used to set the FieldRVA with the content data
//
//******************************************************************************
extern "C" void QCALLTYPE ModuleBuilder_SetFieldRVAContent(QCall::ModuleHandle pModule, INT32 tkField, LPCBYTE pContent, INT32 length)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    ICeeGenInternal * pGen = pRCW->GetCeeGen();

    ReflectionModule * pReflectionModule = pModule->GetReflectionModule();

    // Create the .sdata section if not created
    if (pReflectionModule->m_sdataSection == 0)
        IfFailThrow( pGen->GetSectionCreate (".sdata", sdReadWrite, &pReflectionModule->m_sdataSection) );

    // Define the alignment that the rva will be set to. Since the CoreCLR runtime only has hard alignment requirements
    // up to 8 bytes, the highest alignment we may need is 8 byte alignment. This hard alignment requirement is only needed
    // by Runtime.Helpers.CreateSpan<T>. Since the previous alignment was 4 bytes before CreateSpan was implemented, if the
    // data isn't itself of size divisible by 8, just align to 4 to the memory cost of excess alignment.
    DWORD alignment = (length % 8 == 0) ? 8 : 4;

    // Get the size of current .sdata section. This will be the RVA for this field within the section
    DWORD dwRVA = 0;
    IfFailThrow( pGen->GetSectionDataLen(pReflectionModule->m_sdataSection, &dwRVA) );
    dwRVA = (dwRVA + alignment-1) & ~(alignment-1);

    // allocate the space in .sdata section
    void * pvBlob;
    IfFailThrow( pGen->GetSectionBlock(pReflectionModule->m_sdataSection, length, alignment, (void**) &pvBlob) );

    // copy over the initialized data if specified
    if (pContent != NULL)
        memcpy(pvBlob, pContent, length);

    if (pReflectionModule->IsCollectible())
    {
        GCX_COOP();
        LoaderAllocator::AssociateMemoryWithLoaderAllocator((BYTE*)pvBlob, ((BYTE*)pvBlob) + length, pReflectionModule->GetLoaderAllocator());
    }

    // set FieldRVA into metadata. Note that this is not final RVA in the image if save to disk. We will do another round of fix up upon save.
    IfFailThrow( pRCW->GetEmitter()->SetFieldRVA(tkField, dwRVA) );

    END_QCALL;
}


//******************************************************************************
//
// GetStringConstant
// If this is a dynamic module, this routine will define a new
//  string constant or return the token of an existing constant.
//
//******************************************************************************
extern "C" mdString QCALLTYPE ModuleBuilder_GetStringConstant(QCall::ModuleHandle pModule, LPCWSTR pwzValue, INT32 iLength)
{
    QCALL_CONTRACT;

    mdString strRef = mdTokenNil;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    _ASSERTE(pwzValue != NULL);

    HRESULT hr = pRCW->GetEmitter()->DefineUserString(pwzValue, iLength, &strRef);
    if (FAILED(hr)) {
        COMPlusThrowHR(hr);
    }

    END_QCALL;

    return strRef;
}


/*=============================SetModuleName====================================
// SetModuleName
==============================================================================*/
extern "C" void QCALLTYPE ModuleBuilder_SetModuleName(QCall::ModuleHandle pModule, LPCWSTR wszModuleName)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    IfFailThrow( pRCW->GetEmitter()->SetModuleProps(wszModuleName) );

    END_QCALL;
}

//******************************************************************************
//
// Return a type spec token given a byte array
//
//******************************************************************************
extern "C" mdTypeSpec QCALLTYPE ModuleBuilder_GetTokenFromTypeSpec(QCall::ModuleHandle pModule, LPCBYTE pSignature, INT32 sigLength)
{
    QCALL_CONTRACT;

    mdTypeSpec      ts = mdTokenNil;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    IfFailThrow(pRCW->GetEmitter()->GetTokenFromTypeSpec((PCCOR_SIGNATURE)pSignature, sigLength, &ts));

    END_QCALL;

    return ts;
}


// GetName
// This routine will return the name of the module as a String
extern "C" void QCALLTYPE RuntimeModule_GetScopeName(QCall::ModuleHandle pModule, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    if (!pModule->GetMDImport()->IsValidToken(pModule->GetMDImport()->GetModuleFromScope()))
    {
        ThrowHR(COR_E_BADIMAGEFORMAT);
    }

    LPCSTR    szName = NULL;
    IfFailThrow(pModule->GetMDImport()->GetScopeProps(&szName, 0));
    retString.Set(szName);

    END_QCALL;
}

/*============================GetFullyQualifiedName=============================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
extern "C" void QCALLTYPE RuntimeModule_GetFullyQualifiedName(QCall::ModuleHandle pModule, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    HRESULT hr = S_OK;

    if (pModule->IsPEFile())
    {
        LPCWSTR fileName = pModule->GetPath();
        if (*fileName != W('\0'))
        {
            retString.Set(fileName);
        }
        else
        {
            retString.Set(W("<Unknown>"));
        }
    }
    else
    {
        retString.Set(W("<In Memory Module>"));
    }

    END_QCALL;
}

/*===================================GetHINSTANCE===============================
**Action:  Returns the hinst for this module.
**Returns:
**Arguments: refThis
**Exceptions: None.
==============================================================================*/
extern "C" HINSTANCE QCALLTYPE MarshalNative_GetHINSTANCE(QCall::ModuleHandle pModule)
{
    QCALL_CONTRACT;

    HMODULE hMod = (HMODULE)0;

    BEGIN_QCALL;

    // This returns the base address
    // Other modules should have zero base
    PEAssembly *pPEAssembly = pModule->GetPEAssembly();
    if (!pPEAssembly->IsDynamic())
    {
        hMod = (HMODULE) pModule->GetPEAssembly()->GetManagedFileContents();
    }

    //If we don't have an hMod, set it to -1 so that they know that there's none
    //available
    if (!hMod) {
        hMod = (HMODULE)-1;
    }

    END_QCALL;

    return (HINSTANCE)hMod;
}

// Get class will return an array contain all of the classes
//  that are defined within this Module.
extern "C" void QCALLTYPE RuntimeModule_GetTypes(QCall::ModuleHandle pModule, QCall::ObjectHandleOnStack retTypes)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    struct
    {
        PTRARRAYREF refArrClasses;
        PTRARRAYREF xcept;
        PTRARRAYREF xceptRet;
        OBJECTREF throwable;
    } gc;

    ZeroMemory(&gc, sizeof(gc));

    GCPROTECT_BEGIN(gc);

    DWORD           cXcept = 0;

    IMDInternalImport* pInternalImport = pModule->GetMDImport();

    HENUMTypeDefInternalHolder hEnum(pInternalImport);
    // Get the count of typedefs
    hEnum.EnumTypeDefInit();

    DWORD dwNumTypeDefs = pInternalImport->EnumGetCount(&hEnum);

    // Allocate the COM+ array
    gc.refArrClasses = (PTRARRAYREF) AllocateObjectArray(dwNumTypeDefs, CoreLibBinder::GetClass(CLASS__CLASS));

    DWORD curPos = 0;
    mdTypeDef tdCur = mdTypeDefNil;

    // Now create each COM+ Method object and insert it into the array.
    while (pInternalImport->EnumNext(&hEnum, &tdCur))
    {
        // Get the VM class for the current class token
        TypeHandle curClass;

        EX_TRY {
            curClass = ClassLoader::LoadTypeDefOrRefThrowing(pModule, tdCur,
                                             ClassLoader::ThrowIfNotFound,
                                             ClassLoader::PermitUninstDefOrRef);
        }
        EX_CATCH_THROWABLE(&gc.throwable);

        if (gc.throwable != NULL) {
            // Lazily allocate an array to store the exceptions in
            if (gc.xcept == NULL)
                gc.xcept = (PTRARRAYREF) AllocateObjectArray(dwNumTypeDefs,g_pExceptionClass);

            _ASSERTE(cXcept < dwNumTypeDefs);
            gc.xcept->SetAt(cXcept++, gc.throwable);
            gc.throwable = 0;
            continue;
        }

        _ASSERTE("LoadClass failed." && !curClass.IsNull());

        MethodTable* pMT = curClass.GetMethodTable();
        PREFIX_ASSUME(pMT != NULL);

        // Get the COM+ Class object
        OBJECTREF refCurClass = pMT->GetManagedClassObject();
        _ASSERTE("GetManagedClassObject failed." && refCurClass != NULL);

        _ASSERTE(curPos < dwNumTypeDefs);
        gc.refArrClasses->SetAt(curPos++, refCurClass);
    }

    // check if there were exceptions thrown
    if (cXcept > 0) {

        gc.xceptRet = (PTRARRAYREF) AllocateObjectArray(cXcept,g_pExceptionClass);
        for (DWORD i=0;i<cXcept;i++) {
            gc.xceptRet->SetAt(i, gc.xcept->GetAt(i));
        }
        OBJECTREF except = InvokeUtil::CreateClassLoadExcept((OBJECTREF*) &gc.refArrClasses,(OBJECTREF*) &gc.xceptRet);
        COMPlusThrow(except);
    }

    // We should have filled the array exactly.
    _ASSERTE(curPos == dwNumTypeDefs);

    // Assign the return value to the COM+ array
    retTypes.Set(gc.refArrClasses);

    GCPROTECT_END();

    END_QCALL;
}
