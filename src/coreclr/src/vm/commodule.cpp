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
#include "ildbsymlib.h"


//===============================================================================================
// CreateISymWriterforDynamicModule:
//    Helper to create a ISymUnmanagedWriter instance and hook it up to a newly created dynamic
//    module.  This object is used to capture debugging information (source line info, etc.)
//    for the dynamic module.  This function determines the appropriate symbol format type
//    (ILDB or PDB), and in the case of PDB (Windows desktop only) loads diasymreader.dll.
//
// Arguments:
//   mod - The ReflectionModule for the new dynamic module
//   filenameTemp - the filename at which the module may be saved (ignored if no save access)
//
// Return value:
//   The address where the new writer instance has been stored
//===============================================================================================
static ISymUnmanagedWriter **CreateISymWriterForDynamicModule(ReflectionModule *mod, const WCHAR *wszFilename)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(mod->IsReflection());

    // Determine which symbol format to use. For Silverlight 2.0 RTM we use ILDB mode to address security
    // and portability issues with diasymreader.
    //
    // For desktop builds we'll eventually want to make ILDB is the default, but we need to emit PDB format if
    // the symbols can be saved to disk to preserve back compat.
    //
    ESymbolFormat symFormatToUse = eSymbolFormatILDB;


    static ConfigDWORD dbgForcePDBSymbols;
    if(dbgForcePDBSymbols.val_DontUse_(W("DbgForcePDBSymbols"), 0) == 1)
    {
        symFormatToUse = eSymbolFormatPDB;
    }

    // Create a stream for the symbols to be emitted into. This
    // lives on the Module for the life of the Module.
    SafeComHolder<CGrowableStream> pStream(new CGrowableStream());

    mod->SetInMemorySymbolStream(pStream, symFormatToUse);

    // Create an ISymUnmanagedWriter and initialize it with the
    // stream and the proper file name. This symbol writer will be
    // replaced with new ones periodically as the symbols get
    // retrieved by the debugger.
    SafeComHolder<ISymUnmanagedWriter> pWriter;

    HRESULT hr;
    if (symFormatToUse == eSymbolFormatILDB)
    {
        // Create an ILDB symbol writer from the ildbsymbols library statically linked in
        hr = IldbSymbolsCreateInstance(CLSID_CorSymWriter_SxS,
                                          IID_ISymUnmanagedWriter,
                                          (void**)&pWriter);
    }
    else
    {
        _ASSERTE(symFormatToUse == eSymbolFormatPDB);
        hr = FakeCoCreateInstanceEx(CLSID_CorSymWriter_SxS,
                                    GetInternalSystemDirectory(),
                                    IID_ISymUnmanagedWriter,
                                    (void**)&pWriter,
                                    NULL);
    }

    if (SUCCEEDED(hr))
    {
        {
            GCX_PREEMP();

            // The other reference is given to the Sym Writer
            // But, the writer takes it's own reference.
            hr = pWriter->Initialize(mod->GetEmitter(),
                                     wszFilename,
                                     (IStream*)pStream,
                                     TRUE);
        }
        if (SUCCEEDED(hr))
        {
            mod->GetReflectionModule()->SetISymUnmanagedWriter(pWriter.Extract());

            // Return the address of where we've got our
            // ISymUnmanagedWriter stored so we can pass it over
            // to the managed symbol writer object that most of
            // reflection emit will use to write symbols.
            return mod->GetISymUnmanagedWriterAddr();
        }
        else
        {
            COMPlusThrowHR(hr);
        }
    }
    else
    {
        COMPlusThrowHR(hr);
    }
}

//===============================================================================================
// Attaches an unmanaged symwriter to a newly created dynamic module.
//===============================================================================================
FCIMPL2(LPVOID, COMModule::nCreateISymWriterForDynamicModule, ReflectModuleBaseObject* reflectionModuleUNSAFE, StringObject* filenameUNSAFE)
{
    FCALL_CONTRACT;

    REFLECTMODULEBASEREF refModule = (REFLECTMODULEBASEREF)ObjectToOBJECTREF(reflectionModuleUNSAFE);

    ReflectionModule *mod = (ReflectionModule*)refModule->GetModule();
    STRINGREF filename = (STRINGREF)filenameUNSAFE;

    LPVOID pInternalSymWriter = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_2(filename, refModule);

    SString name;
    if (filename != NULL)
    {
        filename->GetSString(name);
    }

    GCX_PREEMP();
    pInternalSymWriter = CreateISymWriterForDynamicModule(mod, name.GetUnicode());

    HELPER_METHOD_FRAME_END();

    return pInternalSymWriter;

} // COMModule::nCreateISymWriterForDynamicModule
FCIMPLEND

//**************************************************
// GetTypeRef
// This function will return the type token given full qual name. If the type
// is defined locally, we will return the TypeDef token. Or we will return a TypeRef token
// with proper resolution scope calculated.
// wszFullName is escaped (TYPE_NAME_RESERVED_CHAR). It should not be byref or contain enclosing type name,
// assembly name, and generic argument list.
//**************************************************
mdTypeRef QCALLTYPE COMModule::GetTypeRef(QCall::ModuleHandle pModule,
                                          LPCWSTR wszFullName,
                                          QCall::ModuleHandle pRefedModule,
                                          LPCWSTR wszRefedModuleFileName,
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
            if ( pThisAssembly != pRefedAssembly )
            {
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
            else
            {
                _ASSERTE(pModule != pRefedModule);
                _ASSERTE(wszRefedModuleFileName != NULL);

                // Generate ModuleRef
                IfFailThrow(pEmit->DefineModuleRef(wszRefedModuleFileName, &tkResolution));
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
INT32 QCALLTYPE COMModule::GetArrayMethodToken(QCall::ModuleHandle pModule,
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


//******************************************************************************
//
// GetMemberRefToken
// This function will return a MemberRef token given a MethodDef token and the module where the MethodDef/FieldDef is defined.
//
//******************************************************************************
INT32 QCALLTYPE COMModule::GetMemberRef(QCall::ModuleHandle pModule, QCall::ModuleHandle pRefedModule, INT32 tr, INT32 token)
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
    IfFailThrow( pRefingAssembly->GetManifestModule()->GetEmitter()->QueryInterface(IID_IMetaDataAssemblyEmit, (void **) &pAssemblyEmit) );

    CQuickBytes             qbNewSig;
    ULONG                   cbNewSig;

    IfFailThrow( pRefedModule->GetMDImport()->TranslateSigWithScope(
        pRefedAssembly->GetManifestImport(),
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
// Return a TypeRef token given a TypeDef token from the same emit scope
//
//******************************************************************************
void COMModule::DefineTypeRefHelper(
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


//******************************************************************************
//
// Return a MemberRef token given a RuntimeMethodInfo
//
//******************************************************************************
INT32 QCALLTYPE COMModule::GetMemberRefOfMethodInfo(QCall::ModuleHandle pModule, INT32 tr, MethodDesc * pMeth)
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
        IfFailThrow( pRefingAssembly->GetManifestModule()->GetEmitter()->QueryInterface(IID_IMetaDataAssemblyEmit, (void **) &pAssemblyEmit) );

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
            pRefedAssembly->GetManifestImport(),
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
mdMemberRef QCALLTYPE COMModule::GetMemberRefOfFieldInfo(QCall::ModuleHandle pModule, mdTypeDef tr, QCall::TypeHandle th, mdFieldDef tkField)
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
        IfFailThrow( pRefingAssembly->GetManifestModule()->GetEmitter()->QueryInterface(IID_IMetaDataAssemblyEmit, (void **) &pAssemblyEmit) );

        // Translate the field signature this scope
        CQuickBytes     qbNewSig;
        ULONG           cbNewSig;

        IfFailThrow( pRefedMDImport->TranslateSigWithScope(
        pRefedAssembly->GetManifestImport(),
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
INT32 QCALLTYPE COMModule::GetMemberRefFromSignature(QCall::ModuleHandle pModule,
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
void QCALLTYPE COMModule::SetFieldRVAContent(QCall::ModuleHandle pModule, INT32 tkField, LPCBYTE pContent, INT32 length)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    RefClassWriter * pRCW = pModule->GetReflectionModule()->GetClassWriter();
    _ASSERTE(pRCW);

    ICeeGen * pGen = pRCW->GetCeeGen();

    ReflectionModule * pReflectionModule = pModule->GetReflectionModule();

    // Create the .sdata section if not created
    if (pReflectionModule->m_sdataSection == 0)
        IfFailThrow( pGen->GetSectionCreate (".sdata", sdReadWrite, &pReflectionModule->m_sdataSection) );

    // Get the size of current .sdata section. This will be the RVA for this field within the section
    DWORD dwRVA = 0;
    IfFailThrow( pGen->GetSectionDataLen(pReflectionModule->m_sdataSection, &dwRVA) );
    dwRVA = (dwRVA + sizeof(DWORD)-1) & ~(sizeof(DWORD)-1);

    // allocate the space in .sdata section
    void * pvBlob;
    IfFailThrow( pGen->GetSectionBlock(pReflectionModule->m_sdataSection, length, sizeof(DWORD), (void**) &pvBlob) );

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
mdString QCALLTYPE COMModule::GetStringConstant(QCall::ModuleHandle pModule, LPCWSTR pwzValue, INT32 iLength)
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
void QCALLTYPE COMModule::SetModuleName(QCall::ModuleHandle pModule, LPCWSTR wszModuleName)
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
BOOL QCALLTYPE COMModule::IsTransient(QCall::ModuleHandle pModule)
{
    QCALL_CONTRACT;

    BOOL fIsTransient = FALSE;

    BEGIN_QCALL;

    /* Only reflection modules can be transient */
    if (pModule->IsReflection())
        fIsTransient = pModule->GetReflectionModule()->IsTransient();

    END_QCALL;

    return fIsTransient;
}

//******************************************************************************
//
// Return a type spec token given a byte array
//
//******************************************************************************
mdTypeSpec QCALLTYPE COMModule::GetTokenFromTypeSpec(QCall::ModuleHandle pModule, LPCBYTE pSignature, INT32 sigLength)
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


// GetType
// Given a class name, this method will look for that class
//  with in the module.
void QCALLTYPE COMModule::GetType(QCall::ModuleHandle pModule, LPCWSTR wszName, BOOL bThrowOnError, BOOL bIgnoreCase, QCall::ObjectHandleOnStack retType, QCall::ObjectHandleOnStack keepAlive)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(CheckPointer(wszName));
    }
    CONTRACTL_END;

    TypeHandle retTypeHandle;

    BEGIN_QCALL;

    DomainAssembly *pAssembly = pModule->GetDomainAssembly();
    _ASSERTE(pAssembly);

    BOOL prohibitAsmQualifiedName = TRUE;

    // Load the class from this assembly (fail if it is in a different one).
    retTypeHandle = TypeName::GetTypeManaged(wszName, pAssembly, bThrowOnError, bIgnoreCase, prohibitAsmQualifiedName, NULL, (OBJECTREF*)keepAlive.m_ppObject);

    // Verify that it's in 'this' module
    // But, if it's in a different assembly than expected, that's okay, because
    // it just means that it's been type forwarded.
    if (!retTypeHandle.IsNull())
    {
        if ( (retTypeHandle.GetModule() != pModule) &&
             (retTypeHandle.GetModule()->GetAssembly() == pModule->GetAssembly()) )
            retTypeHandle = TypeHandle();
    }

    if (!retTypeHandle.IsNull())
    {
        GCX_COOP();
        retType.Set(retTypeHandle.GetManagedClassObject());
    }

    END_QCALL;

    return;
}


// GetName
// This routine will return the name of the module as a String
void QCALLTYPE COMModule::GetScopeName(QCall::ModuleHandle pModule, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    LPCSTR    szName = NULL;

    if (pModule->IsResource())
    {
        IfFailThrow(pModule->GetAssembly()->GetManifestImport()->GetFileProps(
            pModule->GetModuleRef(),
            &szName,
            NULL,
            NULL,
            NULL));
    }
    else
    {
        if (!pModule->GetMDImport()->IsValidToken(pModule->GetMDImport()->GetModuleFromScope()))
        {
            ThrowHR(COR_E_BADIMAGEFORMAT);
        }
        IfFailThrow(pModule->GetMDImport()->GetScopeProps(&szName, 0));
    }

    retString.Set(szName);

    END_QCALL;
}

static void ReplaceNiExtension(SString& fileName, PCWSTR pwzOldSuffix, PCWSTR pwzNewSuffix)
{
    STANDARD_VM_CONTRACT;

    if (fileName.EndsWithCaseInsensitive(pwzOldSuffix))
    {
        COUNT_T oldSuffixLen = (COUNT_T)wcslen(pwzOldSuffix);
        fileName.Replace(fileName.End() - oldSuffixLen, oldSuffixLen, pwzNewSuffix);
    }
}

/*============================GetFullyQualifiedName=============================
**Action:
**Returns:
**Arguments:
**Exceptions:
==============================================================================*/
void QCALLTYPE COMModule::GetFullyQualifiedName(QCall::ModuleHandle pModule, QCall::StringHandleOnStack retString)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    HRESULT hr = S_OK;

    WCHAR wszBuffer[64];

    if (pModule->IsPEFile())
    {
        LPCWSTR fileName = pModule->GetPath();
        if (*fileName != 0) {
                retString.Set(fileName);
        } else {
            hr = UtilLoadStringRC(IDS_EE_NAME_UNKNOWN, wszBuffer, sizeof( wszBuffer ) / sizeof( WCHAR ), true );
            if (FAILED(hr))
                COMPlusThrowHR(hr);
            retString.Set(wszBuffer);
        }
    }
    else
    {
        hr = UtilLoadStringRC(IDS_EE_NAME_INMEMORYMODULE, wszBuffer, sizeof( wszBuffer ) / sizeof( WCHAR ), true );
        if (FAILED(hr))
            COMPlusThrowHR(hr);
        retString.Set(wszBuffer);
    }

    END_QCALL;
}

/*===================================GetHINSTANCE===============================
**Action:  Returns the hinst for this module.
**Returns:
**Arguments: refThis
**Exceptions: None.
==============================================================================*/
HINSTANCE QCALLTYPE COMModule::GetHINSTANCE(QCall::ModuleHandle pModule)
{
    QCALL_CONTRACT;

    HMODULE hMod = (HMODULE)0;

    BEGIN_QCALL;

    // This returns the base address
    // Other modules should have zero base
    PEFile *pPEFile = pModule->GetFile();
    if (!pPEFile->IsDynamic() && !pPEFile->IsResource())
    {
        hMod = (HMODULE) pModule->GetFile()->GetManagedFileContents();
    }

    //If we don't have an hMod, set it to -1 so that they know that there's none
    //available
    if (!hMod) {
        hMod = (HMODULE)-1;
    }

    END_QCALL;

    return (HINSTANCE)hMod;
}

static Object* GetTypesInner(Module* pModule);

// Get class will return an array contain all of the classes
//  that are defined within this Module.
FCIMPL1(Object*, COMModule::GetTypes, ReflectModuleBaseObject* pModuleUNSAFE)
{
    FCALL_CONTRACT;

    OBJECTREF   refRetVal   = NULL;
    REFLECTMODULEBASEREF refModule = (REFLECTMODULEBASEREF)ObjectToOBJECTREF(pModuleUNSAFE);
    if (refModule == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    Module *pModule = refModule->GetModule();

    HELPER_METHOD_FRAME_BEGIN_RET_2(refRetVal, refModule);

    refRetVal = (OBJECTREF) GetTypesInner(pModule);

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(refRetVal);
}
FCIMPLEND

Object* GetTypesInner(Module* pModule)
{
    CONTRACT(Object*) {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM());

        PRECONDITION(CheckPointer(pModule));

        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    DWORD           dwNumTypeDefs = 0;
    DWORD           i;
    IMDInternalImport *pInternalImport;
    PTRARRAYREF     refArrClasses = NULL;
    PTRARRAYREF     xcept = NULL;
    DWORD           cXcept = 0;
    HENUMInternal   hEnum;
    bool            bSystemAssembly;    // Don't expose transparent proxy
    int             AllocSize = 0;
    MethodTable* pMT = NULL;

    if (pModule->IsResource())
    {
        refArrClasses = (PTRARRAYREF) AllocateObjectArray(0, CoreLibBinder::GetClass(CLASS__CLASS));
        RETURN(OBJECTREFToObject(refArrClasses));
    }

    GCPROTECT_BEGIN(refArrClasses);
    GCPROTECT_BEGIN(xcept);

    pInternalImport = pModule->GetMDImport();

    HENUMTypeDefInternalHolder hEnum(pInternalImport);
    // Get the count of typedefs
    hEnum.EnumTypeDefInit();

    dwNumTypeDefs = pInternalImport->EnumGetCount(&hEnum);

    // Allocate the COM+ array
    bSystemAssembly = (pModule->GetAssembly() == SystemDomain::SystemAssembly());
    AllocSize = dwNumTypeDefs;
    refArrClasses = (PTRARRAYREF) AllocateObjectArray(AllocSize, CoreLibBinder::GetClass(CLASS__CLASS));

    int curPos = 0;
    OBJECTREF throwable = 0;
    mdTypeDef tdCur = mdTypeDefNil;

    GCPROTECT_BEGIN(throwable);
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
        EX_CATCH_THROWABLE(&throwable);

        if (throwable != 0) {
            // Lazily allocate an array to store the exceptions in
            if (xcept == NULL)
                xcept = (PTRARRAYREF) AllocateObjectArray(dwNumTypeDefs,g_pExceptionClass);

            _ASSERTE(cXcept < dwNumTypeDefs);
            xcept->SetAt(cXcept++, throwable);
            throwable = 0;
            continue;
        }

        _ASSERTE("LoadClass failed." && !curClass.IsNull());

        pMT = curClass.GetMethodTable();
        PREFIX_ASSUME(pMT != NULL);

        // Get the COM+ Class object
        OBJECTREF refCurClass = pMT->GetManagedClassObject();
        _ASSERTE("GetManagedClassObject failed." && refCurClass != NULL);

        _ASSERTE(curPos < AllocSize);
        refArrClasses->SetAt(curPos++, refCurClass);
    }
    GCPROTECT_END();    //throwable

    // check if there were exceptions thrown
    if (cXcept > 0) {
        PTRARRAYREF xceptRet = NULL;
        GCPROTECT_BEGIN(xceptRet);

        xceptRet = (PTRARRAYREF) AllocateObjectArray(cXcept,g_pExceptionClass);
        for (i=0;i<cXcept;i++) {
            xceptRet->SetAt(i, xcept->GetAt(i));
        }
        OBJECTREF except = InvokeUtil::CreateClassLoadExcept((OBJECTREF*) &refArrClasses,(OBJECTREF*) &xceptRet);
        COMPlusThrow(except);

        GCPROTECT_END();
    }

    // We should have filled the array exactly.
    _ASSERTE(curPos == AllocSize);

    // Assign the return value to the COM+ array
    GCPROTECT_END();
    GCPROTECT_END();

    RETURN(OBJECTREFToObject(refArrClasses));
}


FCIMPL1(FC_BOOL_RET, COMModule::IsResource, ReflectModuleBaseObject* pModuleUNSAFE)
{
    FCALL_CONTRACT;

    if (pModuleUNSAFE == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    FC_RETURN_BOOL(pModuleUNSAFE->GetModule()->IsResource());
}
FCIMPLEND


//---------------------------------------------------------------------
// Helper code for PunkSafeHandle class. This does the Release in the
// safehandle's critical finalizer.
//---------------------------------------------------------------------
static VOID __stdcall DReleaseTarget(IUnknown *punk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    if (punk)
    {
        punk->Release();
    }
}


//---------------------------------------------------------------------
// Helper code for PunkSafeHandle class. This returns the function that performs
// the Release() for the safehandle's critical finalizer.
//---------------------------------------------------------------------
FCIMPL0(void*, COMPunkSafeHandle::nGetDReleaseTarget)
{
    FCALL_CONTRACT;

    return (void*)DReleaseTarget;
}
FCIMPLEND

