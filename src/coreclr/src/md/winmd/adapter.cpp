// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#include "stdafx.h"
#include "sigparser.h"
#include "sigbuilder.h"
#include "inc/adapter.h"

//#CLRRuntimeHostInternal_GetImageVersionString
// External implementation of call to code:ICLRRuntimeHostInternal::GetImageVersionString.
// Implemented in clr.dll and mscordbi.dll.
HRESULT
CLRRuntimeHostInternal_GetImageVersionString(
  __out_ecount(*pcchBuffer)
    LPWSTR  wszBuffer,
    DWORD * pcchBuffer);

//----------------------------------------------------------------------------------------------------
// The name prefixes used by WinMD to hide/unhide WinRT and CLR versions of RuntimeClasses.
//----------------------------------------------------------------------------------------------------

static const char s_szWinRTPrefix[] = "<WinRT>";
//static const size_t s_ncWinRTPrefix = sizeof(s_szWinRTPrefix) - 1;

static const char s_szCLRPrefix[] = "<CLR>";
static const size_t s_ncCLRPrefix = sizeof(s_szCLRPrefix) - 1;

// the public key token of the ecma key used by some framework assemblies (mscorlib, system, etc).
// note that some framework assemblies use a different key: b03f5f7f11d50a3a
static const BYTE s_pbFrameworkPublicKeyToken[] = {0xB7,0x7A,0x5C,0x56,0x19,0x34,0xE0,0x89};


//-----------------------------------------------------------------------------------------------------
// Returns:
//    S_OK:    if WinMD adapter should be used.
//    S_FALSE: if not
//-----------------------------------------------------------------------------------------------------
HRESULT CheckIfWinMDAdapterNeeded(IMDCommon *pRawMDCommon)
{
    HRESULT hr;
    _ASSERTE(pRawMDCommon != NULL);

    LPCSTR szMetadataVersionString = NULL;

#ifndef DACCESS_COMPILE
#ifdef _DEBUG
    //---------------------------------------------------------------------------------------------------------
    // set COMPLUS_INTERNAL_MD_WinMD_AssertOnIllegalUsage=1
    //
    //    to turn the WinMD adapter off universally.
    //---------------------------------------------------------------------------------------------------------
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MD_WinMD_Disable))
    {
        hr = S_FALSE;
        goto ErrExit;
    }
#endif //_DEBUG
#endif

    //---------------------------------------------------------------------------------------------------------
    // This is the real check: activate WinMD based on metadata version string.
    //---------------------------------------------------------------------------------------------------------
    static const LPCSTR g_szWindowsRuntimeVersion = "WindowsRuntime ";
    IfFailGo(pRawMDCommon->GetVersionString(&szMetadataVersionString));
    if (0 == strncmp(szMetadataVersionString, g_szWindowsRuntimeVersion, strlen(g_szWindowsRuntimeVersion)))
    {
        hr = S_OK;
        goto ErrExit;
    }
    else
    {
        hr = S_FALSE;
        goto ErrExit;
    }

  ErrExit:
    return hr;
}


//------------------------------------------------------------------------------

//
// Factory for WinMDAdapters. Caller must use "delete" to destroy adapter when done.
//
/*static*/ HRESULT WinMDAdapter::Create(IMDCommon *pRawMDCommon, /*[out]*/ WinMDAdapter **ppAdapter)
{
    HRESULT hr;
    LPWSTR        wszCorVersion = NULL;
    WinMDAdapter* pNewAdapter = NULL;
    ULONG numAssemblyRefs = 0;
    const char *szClrPortion = NULL;

    *ppAdapter = NULL;

    pNewAdapter = new (nothrow) WinMDAdapter(pRawMDCommon);
    if (!pNewAdapter)
    {
        IfFailGo(E_OUTOFMEMORY);
    }

    //------------------------------------------------------------------------------------------------
    // Create a stored string to hold our phantom metadata version string.
    //------------------------------------------------------------------------------------------------
    LPCSTR szVersion;
    IfFailGo(pRawMDCommon->GetVersionString(&szVersion));
    szClrPortion = strchr(szVersion, ';');
    if (szClrPortion)
    {
        pNewAdapter->m_scenario = kWinMDExp;
        szClrPortion++;

        // skip the "CLR" prefix if present
        if ((szClrPortion[0] == 'c' || szClrPortion[0] == 'C') &&
            (szClrPortion[1] == 'l' || szClrPortion[1] == 'L') &&
            (szClrPortion[2] == 'r' || szClrPortion[2] == 'R'))
        {
            szClrPortion += 3;
        }
        while (szClrPortion[0] == ' ')
        {
            szClrPortion++;
        }

        size_t ncClrPortion = strlen(szClrPortion);
        pNewAdapter->m_pRedirectedVersionString = new (nothrow) char[ncClrPortion + 1];
        IfNullGo(pNewAdapter->m_pRedirectedVersionString);
        memcpy(pNewAdapter->m_pRedirectedVersionString, szClrPortion, ncClrPortion + 1);
    }
    else
    {
        pNewAdapter->m_scenario = kWinMDNormal;
#ifndef DACCESS_COMPILE
        WCHAR wszCorVersion[_MAX_PATH];
        DWORD cchWszCorVersion = _countof(wszCorVersion);
        IfFailGo(CLRRuntimeHostInternal_GetImageVersionString (wszCorVersion, &cchWszCorVersion));
        MAKE_UTF8PTR_FROMWIDE_NOTHROW(szCorVersion, wszCorVersion);
        IfNullGo(szCorVersion);
        size_t nch = strlen(szCorVersion) + 1;
        pNewAdapter->m_pRedirectedVersionString = new (nothrow) char[nch];
        IfNullGo(pNewAdapter->m_pRedirectedVersionString);
        memcpy(pNewAdapter->m_pRedirectedVersionString, szCorVersion, nch);
#else
        pNewAdapter->m_pRedirectedVersionString = new (nothrow) char[1];
        pNewAdapter->m_pRedirectedVersionString[0] = 0;
#endif
    }


    //------------------------------------------------------------------------------------------------
    // Find an assemblyRef to mscorlib (required to exist in .winmd files precisely to make the adapter's job easier.
    //------------------------------------------------------------------------------------------------
    numAssemblyRefs = pNewAdapter->m_pRawMetaModelCommonRO->CommonGetRowCount(mdtAssemblyRef);
    pNewAdapter->m_assemblyRefMscorlib = 0;
    pNewAdapter->m_fReferencesMscorlibV4 = FALSE;
    for (ULONG rid = 1; rid <= numAssemblyRefs; rid++)
    {
        mdAssemblyRef mdar = mdtAssemblyRef|rid;
        LPCSTR arefName;
        USHORT usMajorVersion;
        IfFailGo(pNewAdapter->m_pRawMetaModelCommonRO->CommonGetAssemblyRefProps(mdar, &usMajorVersion, NULL, NULL, NULL, NULL, NULL, NULL, &arefName, NULL, NULL, NULL));

        // We check for legacy Core library name since Windows.winmd references mscorlib and not System.Private.CoreLib
        if (0 == strcmp(arefName, LegacyCoreLibName_A))
        {
            pNewAdapter->m_assemblyRefMscorlib = mdar;

            if (usMajorVersion == 4)
            {
                // Older WinMDExp used to incorrectly generate an assemblyRef to 4.0.0.0 mscorlib.
                // We use this flag to implement back-compat quirks.
                pNewAdapter->m_fReferencesMscorlibV4 = TRUE;
            }

            break;
        }
    }
    if (pNewAdapter->m_assemblyRefMscorlib == 0)
    {
        // .winmd files are required to have an assemblyRef to mscorlib.
        IfFailGo(COR_E_BADIMAGEFORMAT);
    }


    //------------------------------------------------------------------------------------------------
    // All initialization tasks done.
    //------------------------------------------------------------------------------------------------
    *ppAdapter = pNewAdapter;
    hr = S_OK;

  ErrExit:
    delete wszCorVersion;
    if (FAILED(hr))
        delete pNewAdapter;
    return hr;
}


//------------------------------------------------------------------------------

WinMDAdapter::WinMDAdapter(IMDCommon *pRawMDCommon)
  : m_typeRefTreatmentMemoTable(pRawMDCommon->GetMetaModelCommonRO()->CommonGetRowCount(mdtTypeRef), kTrNotYetInitialized)
  , m_typeDefTreatmentMemoTable(pRawMDCommon->GetMetaModelCommonRO()->CommonGetRowCount(mdtTypeDef), kTdNotYetInitialized)
  , m_methodDefTreatmentMemoTable(pRawMDCommon->GetMetaModelCommonRO()->CommonGetRowCount(mdtMethodDef), kMdNotYetInitialized)
  , m_mangledTypeNameTable(pRawMDCommon->GetMetaModelCommonRO()->CommonGetRowCount(mdtTypeDef), NULL)
  , m_extraAssemblyRefCount(-1)
{
    m_rawAssemblyRefCount = pRawMDCommon->GetMetaModelCommonRO()->CommonGetRowCount(mdtAssemblyRef);
    m_pRedirectedVersionString = NULL;
    m_pRawMetaModelCommonRO = pRawMDCommon->GetMetaModelCommonRO();
}

//------------------------------------------------------------------------------

WinMDAdapter::~WinMDAdapter()
{
    delete m_pRedirectedVersionString;
}

//------------------------------------------------------------------------------
//
// Get TypeRefTreatment value for a TypeRef
//
HRESULT
    WinMDAdapter::GetTypeDefTreatment(
    mdTypeDef tkTypeDef,
    ULONG *   pTypeDefTreatment)
{
    HRESULT hr;

    _ASSERTE(pTypeDefTreatment != NULL);
    _ASSERTE(TypeFromToken(tkTypeDef) == mdtTypeDef);

    ULONG typeDefIndex = RidFromToken(tkTypeDef) - 1;
    ULONG treatment;
    IfFailGo(m_typeDefTreatmentMemoTable.GetEntry(typeDefIndex, &treatment));
    if (hr == S_FALSE)
    {
        LPCSTR  szNamespace;
        LPCSTR  szName;
        ULONG   dwFlags;
        mdToken extends;
        IfFailGo(m_pRawMetaModelCommonRO->CommonGetTypeDefProps(tkTypeDef, &szNamespace, &szName, &dwFlags, &extends, NULL));
        treatment = kTdOther;
        ULONG baseTypeRefTreatment = kTrNotYetInitialized;

        // We only want to treat this type special if it has metadata that was encoded as WinRT metadata
        if (IsTdWindowsRuntime(dwFlags))
        {
            // We need to know whether the typeDef is an Enum/Struct/Delegate/Attribute
            if (TypeFromToken(extends) == mdtTypeRef)
            {
                IfFailGo(GetTypeRefTreatment(extends, &baseTypeRefTreatment));
            }
            // Force tdWindowsRuntime flag on (@todo: WinMDExp already does this on its own - if AppModel could do so, we could get rid of this code)
            if (m_scenario == kWinMDNormal)
            {
                BOOL isAttribute = FALSE;
                if (TypeFromToken(extends) == mdtTypeRef)
                {
                    ULONG treatment;
                    IfFailGo(GetTypeRefTreatment(extends, &treatment));
                    if (treatment == kTrSystemAttribute)
                    {
                        isAttribute = TRUE;
                    }
                }

                if (!isAttribute)
                {
                    treatment = kTdNormalNonAttribute;
                }
                else
                {
                    treatment = kTdNormalAttribute;
                }
            }

            // Windows.Foundation.WinMD: Hide any type defs that define well-known types that we'd redirect to mscorlib equivalents.
            LPCSTR szTempNamespace = szNamespace;
            LPCSTR szTempName      = szName;

            // WinMDExp emits two versions of RuntimeClasses and Enums:
            //
            //    public class Foo {}            // the WinRT reference class
            //    internal class <CLR>Foo {}     // the implementation class that we want WinRT consumers to ignore
            //
            // The adapter's job is to undo WinMDExp's transformations. I.e. turn the above into:
            //
            //    internal class <WinRT>Foo {}   // the WinRT reference class that we want CLR consumers to ignore
            //    public class Foo {}            // the implementation class
            //
            // We only add the <WinRT> prefix here since the WinRT view is the only view that is marked tdWindowsRuntime
            // De-mangling the CLR name is done below.
            if (m_scenario == kWinMDExp && !IsTdNested(dwFlags) && IsTdPublic(dwFlags) && !IsTdInterface(dwFlags))
            {
                switch (baseTypeRefTreatment)
                {
                    case kTrSystemDelegate:
                    case kTrSystemAttribute:
                    case kTrSystemValueType:
                    {
                        // Delegates, Attributes, and Structs have only one version
                        break;
                    }

                    case kTrSystemEnum:
                    {
                        if (m_fReferencesMscorlibV4 && !IsTdSpecialName(dwFlags))
                        {
                            // This is a back-compat quirk. Enums exported with an older WinMDExp have only one version
                            // not marked with tdSpecialName. These enums should *not* be mangled and flipped to private.
                            break;
                        }
                        // fall-thru
                    }
                    default:
                    {
                        // Prepend "<WinRT>" and flip the visibility to private
                        treatment = kTdPrefixWinRTName;
                    }
                }
            }

            // Scan through Custom Attributes on type, looking for interesting bits. We only need to do this for RuntimeClasses. (The if check below is conservative.)
            if (!IsTdInterface(dwFlags) && ((treatment == kTdPrefixWinRTName) || (treatment == kTdNormalNonAttribute)))
            {
                HRESULT hrCA;
                hrCA = m_pRawMetaModelCommonRO->CommonGetCustomAttributeByNameEx(tkTypeDef, "Windows.UI.Xaml.TreatAsAbstractComposableClassAttribute", NULL, NULL, NULL);
                if (hrCA == S_OK)
                {
                    treatment |= kTdMarkAbstractFlag;
                }
            }
        }
        else if (m_scenario == kWinMDExp && !IsTdNested(dwFlags))
        {
            // <CLR> implementation classes are not marked tdWindowsRuntime, but still need to be modified
            // by the adapter.  See if we have one of those here.
            LPCSTR szUnmangledName;
            IfFailGo(CheckIfClrImplementationType(szName, dwFlags, &szUnmangledName));
            if (hr == S_OK)
            {
                treatment = kTdUnmangleWinRTName;
            }
        }

        if (baseTypeRefTreatment == kTrSystemEnum)
        {
            // The typeDef is an Enum. We need to store the treatment.
            treatment |= kTdEnum;
        }

        IfFailGo(m_typeDefTreatmentMemoTable.InitEntry(typeDefIndex, &treatment));
    }
    _ASSERTE(treatment != kTdNotYetInitialized);

    *pTypeDefTreatment = treatment;
    hr = S_OK;

ErrExit:
    return hr;
} // WinMDAdapter::GetTypeDefTreatment

//------------------------------------------------------------------------------
// Returns renamed typedefs
HRESULT WinMDAdapter::GetTypeDefProps(mdTypeDef    tkTypeDef,           // [IN] given typedef
    LPCUTF8     *pszNamespace,        // [OUT] return typedef namespace
    LPCUTF8     *pszName,             // [OUT] return typedef name
    DWORD       *pdwFlags,            // [OUT] return typedef flags
    mdToken     *ptkExtends           // [OUT] Put base class TypeDef/TypeRef here.
    )
{
    HRESULT hr;

    _ASSERTE(TypeFromToken(tkTypeDef) == mdtTypeDef);

    LPCSTR  szNamespace;
    LPCSTR  szName;
    ULONG   dwFlags;
    mdToken extends;
    ULONG treatment;
    IfFailGo(m_pRawMetaModelCommonRO->CommonGetTypeDefProps(tkTypeDef, &szNamespace, &szName, &dwFlags, &extends, NULL));
    IfFailGo(GetTypeDefTreatment(tkTypeDef, &treatment));

    switch (treatment & kTdTreatmentMask)
    {
    case kTdOther:
        break;

    case kTdNormalNonAttribute:
        // Force tdWindowsRuntime flag on (@todo: WinMDExp already does this on its own - if AppModel could do so, we could get rid of this code)
        dwFlags |= tdWindowsRuntime | tdImport;
        break;

    case kTdNormalAttribute:
        // Attribute types don't really exist, so we don't want to allow derivation from them either
        dwFlags |= tdWindowsRuntime | tdSealed;
        break;

    case kTdUnmangleWinRTName:
        szName = szName + s_ncCLRPrefix;
        dwFlags |= tdPublic;
        dwFlags &= ~tdSpecialName;
        break;

    case kTdPrefixWinRTName:
        {
            // Prepend "<WinRT>" and flip the visibility to private
            LPCSTR szPrefixedName;
            ULONG index = RidFromToken(tkTypeDef) - 1;
            IfFailGo(m_mangledTypeNameTable.GetEntry(index, &szPrefixedName));
            if (hr == S_FALSE)
            {
                IfFailGo(CreatePrefixedName(s_szWinRTPrefix, szName, &szPrefixedName));
                IfFailGo(m_mangledTypeNameTable.InitEntry(index, &szPrefixedName));
            }
            szName = szPrefixedName;
            dwFlags &= ~tdPublic;
            dwFlags |= tdImport;
            break;
        }

        default:
            UNREACHABLE();
    }

    if ((treatment & kTdMarkAbstractFlag) == kTdMarkAbstractFlag)
    {
        dwFlags |= tdAbstract;
    }

    if (pszNamespace)
        *pszNamespace = szNamespace;
    if (pszName)
        *pszName = szName;
    if (pdwFlags)
        *pdwFlags = dwFlags;
    if (ptkExtends)
        *ptkExtends = extends;

    hr = S_OK;

 ErrExit:
    return hr;
}

//------------------------------------------------------------------------------

// Find TypeDef by name
HRESULT WinMDAdapter::FindTypeDef(
    LPCSTR      szTypeDefNamespace, // [IN] Namespace for the TypeDef.
    LPCSTR      szTypeDefName,      // [IN] Name of the TypeDef.
    mdToken     tkEnclosingClass,   // [IN] TypeDef/TypeRef of enclosing class.
    mdTypeDef * ptkTypeDef          // [OUT] return typedef
)
{
    HRESULT hr = S_OK;

    _ASSERTE((szTypeDefName != NULL) && (ptkTypeDef != NULL));
    _ASSERTE((TypeFromToken(tkEnclosingClass) == mdtTypeRef) ||
             (TypeFromToken(tkEnclosingClass) == mdtTypeDef) ||
             IsNilToken(tkEnclosingClass));

    // initialize the output parameter
    *ptkTypeDef = mdTypeDefNil;

    // Treat no namespace as empty string.
    if (szTypeDefNamespace == NULL)
        szTypeDefNamespace = "";

    // Do a linear search
    ULONG        cTypeDefRecs = m_pRawMetaModelCommonRO->CommonGetRowCount(mdtTypeDef);

    // Get TypeDef of the tkEnclosingClass passed in
    if (TypeFromToken(tkEnclosingClass) == mdtTypeRef)
    {
        LPCUTF8  szNamespace;
        LPCUTF8  szName;
        mdToken  tkResolutionScope;

        IfFailRet(this->GetTypeRefProps(tkEnclosingClass, &szNamespace, &szName, &tkResolutionScope));
        // Update tkEnclosingClass to TypeDef
        IfFailRet(this->FindTypeDef(
                    szNamespace,
                    szName,
                    (TypeFromToken(tkResolutionScope) == mdtTypeRef) ? tkResolutionScope : mdTokenNil,
                    &tkEnclosingClass));
        _ASSERTE(TypeFromToken(tkEnclosingClass) == mdtTypeDef);
    }

    // Search for the TypeDef
    for (ULONG i = 1; i <= cTypeDefRecs; i++)
    {
        LPCUTF8   szNamespace;
        LPCUTF8   szName;
        ULONG     dwFlags;

        mdTypeDef tkTypeDefCandidate = TokenFromRid(i, mdtTypeDef);

        IfFailRet(this->GetTypeDefProps(tkTypeDefCandidate, &szNamespace, &szName, &dwFlags, NULL));
        if (!IsTdNested(dwFlags) && !IsNilToken(tkEnclosingClass))
        {
            // If the class is not Nested and EnclosingClass passed in is not nil
            continue;
        }
        else if (IsTdNested(dwFlags) && IsNilToken(tkEnclosingClass))
        {
            // If the class is nested and EnclosingClass passed is nil
            continue;
        }
        else if (!IsNilToken(tkEnclosingClass))
        {
            _ASSERTE(TypeFromToken(tkEnclosingClass) == mdtTypeDef);

            mdTypeDef enclosingTypeDef;
            IfFailRet(m_pRawMetaModelCommonRO->CommonGetEnclosingClassOfTypeDef(tkTypeDefCandidate, &enclosingTypeDef));
            if (enclosingTypeDef != tkEnclosingClass)
            {
                // Type was not nested by tkEnclosingClass
                continue;
            }
        }

        if (strcmp(szTypeDefName, szName) == 0)
        {
            if (strcmp(szTypeDefNamespace, szNamespace) == 0)
            {
                *ptkTypeDef = TokenFromRid(i, mdtTypeDef);
                return S_OK;
            }
        }
    }
    // Cannot find the TypeDef by name
    return CLDB_E_RECORD_NOTFOUND;
}

//------------------------------------------------------------------------------
//
// Modifies TypeRef names and resolution scope.
//
HRESULT
WinMDAdapter::GetTypeRefProps(
    mdTypeRef tkTypeRef,
    LPCSTR *  pszNamespace,
    LPCSTR *  pszName,
    mdToken * ptkResolutionScope)
{

    HRESULT hr;
    ULONG   treatment;
    ULONG treatmentClass;
    IfFailGo(GetTypeRefTreatment(tkTypeRef, &treatment));
    _ASSERTE(treatment != kTrNotYetInitialized);

    treatmentClass = treatment & kTrClassMask;
    _ASSERTE(treatmentClass == kTrClassMisc);
    switch (treatment)
    {
        case kTrNoRewriteNeeded:
        case kTrSystemValueType:
        case kTrSystemEnum:
            IfFailGo(m_pRawMetaModelCommonRO->CommonGetTypeRefProps(tkTypeRef, pszNamespace, pszName, ptkResolutionScope));
            break;

        case kTrSystemDelegate:
            if (pszNamespace != NULL)       *pszNamespace = "System";
            if (pszName != NULL)            *pszName = "MulticastDelegate";
            if (ptkResolutionScope) *ptkResolutionScope = GetAssemblyRefMscorlib();
            break;

        case kTrSystemAttribute:
            if (pszNamespace != NULL)       *pszNamespace = "System";
            if (pszName != NULL)            *pszName = "Attribute";
            if (ptkResolutionScope != NULL) *ptkResolutionScope = GetAssemblyRefMscorlib();
            break;

        default:
            _ASSERTE(!"Unknown treatment value.");
            break;
    }
    hr = S_OK;

ErrExit:
    return hr;
} // WinMDAdapter::GetTypeRefProps

//------------------------------------------------------------------------------
//
// Get TypeRefTreatment value for a TypeRef
//
HRESULT
WinMDAdapter::GetTypeRefTreatment(
    mdTypeRef tkTypeRef,
    ULONG *   pTypeRefTreatment)
{
    HRESULT hr;

    _ASSERTE(pTypeRefTreatment != NULL);
    _ASSERTE(TypeFromToken(tkTypeRef) == mdtTypeRef);

    ULONG typeRefIndex = RidFromToken(tkTypeRef) - 1;
    ULONG treatment;
    IfFailGo(m_typeRefTreatmentMemoTable.GetEntry(typeRefIndex, &treatment));
    if (hr == S_FALSE)
    {
        LPCSTR szFromNamespace;
        LPCSTR szFromName;
        mdToken tkResolutionScope;
        IfFailGo(m_pRawMetaModelCommonRO->CommonGetTypeRefProps(tkTypeRef, &szFromNamespace, &szFromName, &tkResolutionScope));
        treatment = kTrNoRewriteNeeded;
        if (0 == strcmp(szFromNamespace, "System"))
        {
            if (0 == strcmp(szFromName, "MulticastDelegate"))
            {
                treatment = kTrSystemDelegate;
            }
            else if (0 == strcmp(szFromName, "Attribute"))
            {
                treatment = kTrSystemAttribute;
            }
            else if (0 == strcmp(szFromName, "Enum"))
            {
                treatment = kTrSystemEnum;
            }
            else if (0 == strcmp(szFromName, "ValueType"))
            {
                treatment = kTrSystemValueType;
            }
        }

        // Note that we intentionally do not mangle names of TypeRef's pointing to mangled TypeDef's
        // in the same module. This allows WinMDExp to generate "conditional TypeRef's", i.e. references
        // to the WinMD view or CLR view, depending on whether the adapter is on.

        IfFailGo(m_typeRefTreatmentMemoTable.InitEntry(typeRefIndex, &treatment));
    }
    _ASSERTE(treatment != kTrNotYetInitialized);

    *pTypeRefTreatment = treatment;
    hr = S_OK;

ErrExit:
    return hr;
} // WinMDAdapter::GetTypeRefTreatment

//------------------------------------------------------------------------------
//
// Finds a typeref by its (transformed) name
//
HRESULT WinMDAdapter::FindTypeRef(  // S_OK or error.
        LPCSTR      szNamespace,            // [IN] Namespace for the TypeRef.
        LPCSTR      szName,                 // [IN] Name of the TypeRef.
        mdToken     tkResolutionScope,      // [IN] Resolution Scope fo the TypeRef.
        mdTypeRef   *ptk)                   // [OUT] TypeRef token returned.
{
    HRESULT     hr;

    _ASSERTE(szName);  // Crash on NULL pszName (just like the real metadata importer)
    _ASSERTE(ptk);     // Crash on NULL ptk (just like the real metadata importer)


    // initialize the output parameter
    *ptk = mdTypeRefNil;

    // Treat no namespace as empty string.
    if (!szNamespace)
        szNamespace = "";

    ULONG       cTypeRefRecs = m_pRawMetaModelCommonRO->CommonGetRowCount(mdtTypeRef);

    for (ULONG i = 1; i <= cTypeRefRecs; i++)
    {
        LPCUTF8     szNamespaceTmp;
        LPCUTF8     szNameTmp;
        mdToken     tkRes;

        mdTypeRef tr = TokenFromRid(i, mdtTypeRef);
        IfFailGo(GetTypeRefProps(tr, &szNamespaceTmp, &szNameTmp, &tkRes));
        if (IsNilToken(tkRes))
        {
            if (!IsNilToken(tkResolutionScope))
                continue;
        }
        else if (tkRes != tkResolutionScope)
            continue;

        if (strcmp(szNamespace, szNamespaceTmp))
            continue;

        if (!strcmp(szNameTmp, szName))
        {
            *ptk = tr;
            hr = S_OK;
            goto ErrExit;
        }
    }

    // cannot find the typedef
    hr = CLDB_E_RECORD_NOTFOUND;
ErrExit:
    return hr;
}

//------------------------------------------------------------------------------

HRESULT WinMDAdapter::ModifyExportedTypeName(
    mdExportedType tkExportedType,     // [IN] exportedType token
    LPCSTR     *pszNamespace,          // [IN,OUT,OPTIONAL] namespace to modify
    LPCSTR     *pszName                // [IN,OUT,OPTIONAL] name to modify
)
{
    _ASSERTE(TypeFromToken(tkExportedType) == mdtExportedType);

    if (m_scenario == kWinMDExp)
    {
        if (pszName && 0 == strncmp(*pszName, s_szCLRPrefix, s_ncCLRPrefix))
        {
            (*pszName) += s_ncCLRPrefix;
        }
    }
    return S_OK;
}

//--------------------------------------------------------------------------------

HRESULT WinMDAdapter::FindExportedType(
    LPCUTF8           szNamespace,           // [IN] expected namespace
    LPCUTF8           szName,                // [IN] expected name
    mdToken           tkEnclosingType,       // [IN] expected tkEnclosingType
    mdExportedType   *ptkExportedType  // [OUT] ExportedType token returned.
)
{
    HRESULT hr;

    _ASSERTE(szName && ptkExportedType);

    // Set NULL namespace to empty string.
    if (!szNamespace)
        szNamespace = "";

    // Set output to Nil.
    *ptkExportedType = mdTokenNil;

    ULONG ulCount = m_pRawMetaModelCommonRO->CommonGetRowCount(mdtExportedType);
    while (ulCount)
    {
        mdExportedType tkCandidateExportedType = TokenFromRid(ulCount--, mdtExportedType);

        LPCSTR      szCandidateNamespace;
        LPCSTR      szCandidateName;
        mdToken     tkCandidateImpl;

        IfFailRet(m_pRawMetaModelCommonRO->CommonGetExportedTypeProps(tkCandidateExportedType, &szCandidateNamespace, &szCandidateName, &tkCandidateImpl));
        IfFailRet(this->ModifyExportedTypeName(tkCandidateExportedType, &szCandidateNamespace, &szCandidateName));
        // Handle the case of nested vs. non-nested classes.
        if (TypeFromToken(tkCandidateImpl) == mdtExportedType && !IsNilToken(tkCandidateImpl))
        {
            // Current ExportedType being looked at is a nested type, so
            // comparing the implementation token.
            if (tkCandidateImpl != tkEnclosingType)
                continue;
        }
        else if (TypeFromToken(tkEnclosingType) == mdtExportedType &&
                 !IsNilToken(tkEnclosingType))
        {
            // ExportedType passed in is nested but the current ExportedType is not.
            continue;
        }

        if (0 != strcmp(szName, szCandidateName))
            continue;

        if (0 != strcmp(szNamespace, szCandidateNamespace))
            continue;

        *ptkExportedType = tkCandidateExportedType;
        return S_OK;
    }
    return CLDB_E_RECORD_NOTFOUND;
}


//------------------------------------------------------------------------------

// Modifies methodDef flags and RVA
HRESULT WinMDAdapter::ModifyMethodProps(mdMethodDef tkMethodDef, /*[in, out]*/ DWORD *pdwAttr, /* [in,out] */ DWORD *pdwImplFlags, /* [in,out] */ ULONG *pulRVA, /* [in, out] */ LPCSTR *pszName)
{
    HRESULT hr;
    _ASSERTE(TypeFromToken(tkMethodDef) == mdtMethodDef);

    ULONG mdTreatment;
    DWORD dwAttr;
    DWORD dwImplFlags;
    ULONG ulRVA;
    IfFailGo(GetMethodDefTreatment(tkMethodDef, &mdTreatment));

    dwAttr = pdwAttr ? *pdwAttr: 0;
    dwImplFlags = pdwImplFlags ? *pdwImplFlags : 0;
    ulRVA = pulRVA ? *pulRVA : 0;

    switch (mdTreatment & kMdTreatmentMask)
    {
        case kMdInterface:
            // Method is declared on an interface
            dwImplFlags |= miRuntime|miInternalCall;
            break;

        case kMdDelegate:
            // Method is declared on a delegate
            dwAttr &= ~mdMemberAccessMask;
            dwAttr |= mdPublic;
            ulRVA = 0;
            dwImplFlags |= miRuntime;
            break;

        case kMdAttribute:
            // Method is declared on an attribute
            ulRVA = 0;
            dwImplFlags |= miRuntime|miInternalCall;
            break;

        case kMdImplementation:
            // CLR implementation class. Needs no adjustment.
            break;

        case kMdOther:

            // All other cases
            ulRVA = 0;
            dwImplFlags |= miRuntime|miInternalCall;

            if (mdTreatment & kMdMarkAbstractFlag)
            {
                dwAttr |= mdAbstract;
            }

            if (mdTreatment & kMdMarkPublicFlag)
            {
                dwAttr &= ~mdMemberAccessMask;
                dwAttr |= mdPublic;
            }

            break;

        default:
            UNREACHABLE();

    }

    dwAttr |= mdHideBySig;

    if (pdwAttr)
        (*pdwAttr) = dwAttr;
    if (pdwImplFlags)
        (*pdwImplFlags) = dwImplFlags;
    if (pulRVA)
        *pulRVA = ulRVA;
    hr = S_OK;

  ErrExit:
    return hr;
}

HRESULT WinMDAdapter::ModifyFieldProps (mdToken tkField, mdToken tkParent, LPCSTR szFieldName, DWORD *pdwFlags)
{
    _ASSERTE(szFieldName != NULL);

    HRESULT hr;
    if (pdwFlags && IsFdPrivate(*pdwFlags) && (0 == strcmp(szFieldName, "value__")))
    {
        ULONG treatment;
        BOOL isEnum = FALSE;
        if(TypeFromToken(tkParent) == mdtTypeDef)
        {
            IfFailGo(GetTypeDefTreatment(tkParent, &treatment));

            if ((treatment & kTdEnum) == kTdEnum)
            {
                isEnum = TRUE;
            }
        }

        if (isEnum)
        {
            // We have found the value__ field of the enum.
            // We need to change its flags from private to public
            *pdwFlags = (*pdwFlags & ~fdPrivate) | fdPublic;
        }
    }

    hr = S_OK;

ErrExit:
    return hr;
}


HRESULT WinMDAdapter::ModifyFieldDefProps (mdFieldDef tkFielddDef, DWORD *pdwFlags)
{
    HRESULT hr;

    if (pdwFlags)
    {
        mdTypeDef tkParent;
        LPCSTR szName;
        IfFailGo(m_pRawMetaModelCommonRO->CommonGetFieldDefProps(tkFielddDef, &tkParent, &szName, pdwFlags));
        IfFailGo(ModifyFieldProps(tkFielddDef, tkParent, szName, pdwFlags));
    }

    hr = S_OK;
ErrExit:
    return hr;
}

//------------------------------------------------------------------------------

HRESULT WinMDAdapter::ModifyMemberProps(mdToken tkMember, /*[in, out]*/ DWORD *pdwAttr, /* [in,out] */ DWORD *pdwImplFlags, /* [in,out] */ ULONG *pulRVA, LPCSTR *pszNewName)
{
    HRESULT hr;
    switch(TypeFromToken(tkMember))
    {
    case mdtMethodDef: IfFailGo(ModifyMethodProps(tkMember, pdwAttr, pdwImplFlags, pulRVA, pszNewName));
        break;

    case mdtFieldDef:
        IfFailGo(ModifyFieldDefProps(tkMember, pdwAttr));
        break;
    }

    hr = S_OK;
ErrExit:
    return hr;
}

//------------------------------------------------------------------------------

// Get MethodTreatment value for a methodDef
HRESULT WinMDAdapter::GetMethodDefTreatment(mdMethodDef tkMethodDef, ULONG *ppMethodDefTreatment)
{
    HRESULT hr;

    _ASSERTE(TypeFromToken(tkMethodDef) == mdtMethodDef);
    ULONG index = RidFromToken(tkMethodDef) - 1;

    // Thread-safety: No lock is needed to update this table as we're monotonically advancing a kMdNotYetInitialized to
    //    some other fixed byte. The work to decide this value is idempotent and side-effect free so
    //    there's no harm if two threads do it concurrently.
    ULONG mdTreatment;
    IfFailGo(m_methodDefTreatmentMemoTable.GetEntry(index, &mdTreatment));
    if (hr == S_FALSE)
    {
        mdTypeDef declaringTypeDef;
        IfFailGo(m_pRawMetaModelCommonRO->FindParentOfMethodHelper(tkMethodDef, &declaringTypeDef));
        ULONG firstMethodRid;
        IfFailGo(m_pRawMetaModelCommonRO->CommonGetTypeDefProps(declaringTypeDef, NULL, NULL, NULL, NULL, &firstMethodRid));
        IfFailGo(ComputeMethodDefTreatment(tkMethodDef, declaringTypeDef, &mdTreatment));
        _ASSERTE(mdTreatment != kMdNotYetInitialized);
        IfFailGo(m_methodDefTreatmentMemoTable.InitEntry(index, &mdTreatment));

        // Since the mdTreatment is only a function of the declaring type in most cases, cache it for all the declared method since
        // we took the time to look up the declaring type. Do enough validity checks to avoid corrupting the heap
        // or table but otherwise, swallow validity errors as this is just an optimization.
        // Methods on RuntimeClasses need to get treatment data per method.
        if (((mdTreatment & kMdTreatmentMask) != kMdOther))
        {
            const ULONG methodDefCount = m_pRawMetaModelCommonRO->CommonGetRowCount(mdtMethodDef);
            const ULONG startIndex = RidFromToken(firstMethodRid) - 1;
            if (startIndex < methodDefCount)
            {
                const ULONG typeDefCount = m_pRawMetaModelCommonRO->CommonGetRowCount(mdtTypeDef);
                if (RidFromToken(declaringTypeDef) < typeDefCount)
                {
                    ULONG stopMethodRid;
                    if (S_OK == m_pRawMetaModelCommonRO->CommonGetTypeDefProps(declaringTypeDef + 1, NULL, NULL, NULL, NULL, &stopMethodRid))
                    {
                        ULONG stopIndex = RidFromToken(stopMethodRid) - 1;
                        if (startIndex < methodDefCount && stopIndex <= methodDefCount)
                        {
                            ULONG walkIndex = startIndex;
                            while (walkIndex < stopIndex)
                            {
                                IfFailGo(m_methodDefTreatmentMemoTable.InitEntry(walkIndex++, &mdTreatment));
                            }
                        }
                    }
                }
                else
                {
                    // This was final typeDef - blast the mdTreatment into the rest of the table.
                    for (ULONG i = startIndex; i < methodDefCount; i++)
                    {
                        IfFailGo(m_methodDefTreatmentMemoTable.InitEntry(i, &mdTreatment));
                    }
                }
            }
        }
    }

    *ppMethodDefTreatment = mdTreatment;
    hr = S_OK;

  ErrExit:
    return hr;
}

//------------------------------------------------------------------------------

// Compute MethodTreatment value for a methodDef (unlike GetMethodDefTreatment, this
//  does not cache.)
//
HRESULT WinMDAdapter::ComputeMethodDefTreatment(mdMethodDef tkMethodDef, mdTypeDef tkDeclaringTypeDef, ULONG *ppMethodDefTreatment)
{
    HRESULT hr;

    BYTE mdTreatment = kMdImplementation;

    LPCSTR  szDeclaringTypeName;
    DWORD   parentTdAttr;
    mdToken extends;
    IfFailGo(m_pRawMetaModelCommonRO->CommonGetTypeDefProps(tkDeclaringTypeDef, NULL, &szDeclaringTypeName, &parentTdAttr, &extends, NULL));

    // We only want to treat this method special if it has metadata exposed to WinRT
    if (IsTdWindowsRuntime(parentTdAttr))
    {
        LPCSTR szUnmangledName;
        IfFailGo(CheckIfClrImplementationType(szDeclaringTypeName, parentTdAttr, &szUnmangledName));
        if (hr == S_OK)
        {
            mdTreatment = kMdImplementation;
        }
        else if (IsTdNested(parentTdAttr))
        {
            // nested types are implementation
            mdTreatment = kMdImplementation;
        }
        else if (parentTdAttr & tdInterface)
        {
            // Method is declared on an interface.
            mdTreatment = kMdInterface;
        }
        else if (m_scenario == kWinMDExp && (parentTdAttr & tdPublic) == 0)
        {
            // internal classes generated by WinMDExp are implementation
            mdTreatment = kMdImplementation;
        }
        else
        {
            mdTreatment = kMdOther;

            if (TypeFromToken(extends) == mdtTypeRef)
            {
                ULONG trTreatment;
                IfFailGo(GetTypeRefTreatment(extends, &trTreatment));
                if (trTreatment == kTrSystemDelegate)
                {
                    // Method is declared on a delegate
                    mdTreatment = kMdDelegate;
                }
                else if (trTreatment == kTrSystemAttribute)
                {
                    // Method is declared on a attribute
                    mdTreatment = kMdAttribute;
                }
            }
        }
    }

    // If treatment is other, then this is a non-managed WinRT runtime class definition. Find out about various bits that we apply via attrubtes and name parsing.
    if (mdTreatment == kMdOther)
    {
        // Scan through Custom Attributes on type, looking for interesting bits.
        HRESULT hrCA;
        hrCA = m_pRawMetaModelCommonRO->CommonGetCustomAttributeByNameEx(tkMethodDef, "Windows.UI.Xaml.TreatAsPublicMethodAttribute", NULL, NULL, NULL);
        if (hrCA == S_OK)
        {
            mdTreatment |= kMdMarkPublicFlag;
        }


        hrCA = m_pRawMetaModelCommonRO->CommonGetCustomAttributeByNameEx(tkMethodDef, "Windows.UI.Xaml.TreatAsAbstractMethodAttribute", NULL, NULL, NULL);
        if (hrCA == S_OK)
        {
            mdTreatment |= kMdMarkAbstractFlag;
        }

        LPCSTR szName;
        DWORD dwFlags;
        IfFailRet(m_pRawMetaModelCommonRO->CommonGetMethodDefProps(tkMethodDef, &szName, &dwFlags, NULL, NULL));
    }
    *ppMethodDefTreatment = mdTreatment;
    hr = S_OK;

ErrExit:
    return hr;
}

//------------------------------------------------------------------------------

//
// Finds a CA by its (transformed) name
//
HRESULT WinMDAdapter::GetCustomAttributeByName( // S_OK or error.
    mdToken            tkObj,      // [IN] Object with Custom Attribute.
    LPCUTF8            szName,     // [IN] Name of desired Custom Attribute.
    mdCustomAttribute *ptkCA,      // [OUT] Put custom attribute token here
    const void       **ppData,     // [OUT] Put pointer to data here.
    ULONG             *pcbData)    // [OUT] Put size of data here.
{
    return m_pRawMetaModelCommonRO->CommonGetCustomAttributeByNameEx(tkObj, szName, ptkCA, ppData, pcbData);
}

//------------------------------------------------------------------------------

//
// Modify CA blobs
//
HRESULT WinMDAdapter::GetCustomAttributeBlob(
    mdCustomAttribute tkCA,
    const void  **ppData,         // [OUT] Put pointer to data here.
    ULONG        *pcbData)        // [OUT] Put size of data here.
{
    return m_pRawMetaModelCommonRO->CommonGetCustomAttributeProps(tkCA, NULL, NULL, ppData, pcbData);
}

//------------------------------------------------------------------------------

// Gets Guid from Windows.Foundation.Metadata.Guid
HRESULT WinMDAdapter::GetItemGuid(mdToken tkObj, CLSID *pGuid)
{
    HRESULT hr;
    _ASSERTE(pGuid);
    *pGuid = GUID_NULL;
    const BYTE *pBlob;
    ULONG cbBlob;
    IfFailGo(m_pRawMetaModelCommonRO->CommonGetCustomAttributeByName(tkObj, "Windows.Foundation.Metadata.GuidAttribute", (const void**)&pBlob, &cbBlob));
    if (hr == S_OK)
    {
        if (cbBlob == 2 + sizeof(GUID) + 2)
        {
            memcpy(pGuid, pBlob + 2, sizeof(GUID));
            hr = S_OK;
            goto ErrExit;
        }
    }
    hr = S_FALSE;
  ErrExit:
    return hr;

}


//------------------------------------------------------------------------------

//----------------------------------------------------------------------------
// Gets filtered methodImpl list and adds it to an existing DynamicArray enum.
//
// This is used to hide implementations of methods on redirected interfaces after
// we've replaced them with their CLR counterparts in the interface implementation list.
//
// Each filtered methodImpl adds two elements to the enum: the body and decl values
// in that order.
//----------------------------------------------------------------------------
HRESULT WinMDAdapter::AddMethodImplsToEnum(mdTypeDef tkTypeDef, HENUMInternal *henum)
{
    _ASSERTE(henum != NULL);
    _ASSERTE(henum->m_EnumType == MDDynamicArrayEnum);
    _ASSERTE(TypeFromToken(tkTypeDef) == mdtTypeDef);

    HRESULT hr;

    mdToken tkMethodImplFirst;
    ULONG count;
    IfFailGo(m_pRawMetaModelCommonRO->CommonGetMethodImpls(tkTypeDef, &tkMethodImplFirst, &count));
    for (ULONG i = 0; i < count; i++)
    {
        mdToken tkMethodImpl = tkMethodImplFirst + i;
        mdToken tkBody, tkDecl;
        IfFailGo(m_pRawMetaModelCommonRO->CommonGetMethodImplProps(tkMethodImpl, &tkBody, &tkDecl));
        IfFailGo(HENUMInternal::AddElementToEnum(henum, tkBody));
        IfFailGo(HENUMInternal::AddElementToEnum(henum, tkDecl));
    }

    hr = S_OK;
  ErrExit:
    return hr;
}

//------------------------------------------------------------------------------

// S_OK if this is a CLR implementation type that was mangled and hidden by WinMDExp
//
// Logically, this function takes a mdTypeDef, but since the caller has already extracted the
// row data for other purposes, we'll take the row data.
HRESULT WinMDAdapter::CheckIfClrImplementationType(LPCSTR szName, DWORD dwAttr, LPCSTR *pszUnmangledName)
{
    if (m_scenario != kWinMDExp)
        return S_FALSE;                                       // Input file not produced by WinMDExp
    if (IsTdNested(dwAttr))
        return S_FALSE;                                       // Type is nested in another type
    if ((dwAttr & (tdPublic|tdSpecialName)) != tdSpecialName)
        return S_FALSE;                                       // Type public or not SpecialName
    if (0 != strncmp(szName, s_szCLRPrefix, s_ncCLRPrefix))
        return S_FALSE;                                       // Name does not begin with "<CLR>"

    // Ran out of reasons.
    *pszUnmangledName = szName + s_ncCLRPrefix;
    return S_OK;
}

//-----------------------------------------------------------------------------------------------------

/*static*/ HRESULT WinMDAdapter::CreatePrefixedName(LPCSTR szPrefix, LPCSTR szName, LPCSTR *ppOut)
{
    // This can cause allocations (thus entering the host) during a profiler stackwalk.
    // But we're ok since we're not supporting SQL/F1 profiling with WinMDs. FUTURE:
    // Would be nice to eliminate allocations on stack walks regardless.
    PERMANENT_CONTRACT_VIOLATION(HostViolation, ReasonUnsupportedForSQLF1Profiling);

    size_t ncPrefix = strlen(szPrefix);
    size_t ncName = strlen(szName);
    if (ncPrefix + ncName < ncPrefix || ncPrefix + ncName + 1 < ncPrefix + ncName)
        return E_OUTOFMEMORY;
    LPSTR szResult = new (nothrow) char[ncPrefix + ncName + 1];
    IfNullRet(szResult);
    memcpy(szResult, szPrefix, ncPrefix);
    memcpy(szResult + ncPrefix, szName, ncName);
    szResult[ncPrefix + ncName] = '\0';
    *ppOut = szResult;
    return S_OK;
}

//-----------------------------------------------------------------------------------------------------

//-----------------------------------------------------------------------------------------------------
// S_OK if pUnknown is really a WinMD wrapper. This is just a polite way of asking "is it bad to
//   to static cast pUnknown to RegMeta/MDInternalRO."
//-----------------------------------------------------------------------------------------------------
HRESULT CheckIfImportingWinMD(IUnknown *pUnknown)
{
    IUnknown *pIgnore = NULL;
    HRESULT hr = pUnknown->QueryInterface(IID_IWinMDImport, (void**)&pIgnore);
    if (hr == S_OK)
    {
        pIgnore->Release();
    }
    if (hr == E_NOINTERFACE)
    {
        hr = S_FALSE;
    }
    return hr;
}


//-----------------------------------------------------------------------------------------------------
// E_NOTIMPL if pUnknown is really a WinMD wrapper.
//-----------------------------------------------------------------------------------------------------
HRESULT VerifyNotWinMDHelper(IUnknown *pUnknown
#ifdef _DEBUG
                            ,LPCSTR    assertMsg
                            ,LPCSTR    file
                            ,int       line
#endif //_DEBUG
                            )
{
    HRESULT hr = CheckIfImportingWinMD(pUnknown);
    if (FAILED(hr))
        return hr;
    if (hr == S_FALSE)
    {
        return S_OK;
    }
#ifdef _DEBUG
#ifndef DACCESS_COMPILE
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MD_WinMD_AssertOnIllegalUsage))
    {
        DbgAssertDialog(file, line, assertMsg);
    }
#endif
#endif
    return E_NOTIMPL;
}



