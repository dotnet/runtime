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
    const char *szClrPortion = strchr(szVersion, ';');
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
    ULONG numAssemblyRefs = pNewAdapter->m_pRawMetaModelCommonRO->CommonGetRowCount(mdtAssemblyRef);
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
  , m_redirectedCABlobsMemoTable(pRawMDCommon->GetMetaModelCommonRO()->CommonGetRowCount(mdtCustomAttribute), NULL)
  , m_redirectedMethodDefSigMemoTable(pRawMDCommon->GetMetaModelCommonRO()->CommonGetRowCount(mdtMethodDef), NULL)
  , m_redirectedFieldDefSigMemoTable(pRawMDCommon->GetMetaModelCommonRO()->CommonGetRowCount(mdtFieldDef), NULL)
  , m_redirectedMemberRefSigMemoTable(pRawMDCommon->GetMetaModelCommonRO()->CommonGetRowCount(mdtMemberRef), NULL)
  , m_redirectedPropertySigMemoTable(pRawMDCommon->GetMetaModelCommonRO()->CommonGetRowCount(mdtProperty), NULL)
  , m_redirectedTypeSpecSigMemoTable(pRawMDCommon->GetMetaModelCommonRO()->CommonGetRowCount(mdtTypeSpec), NULL)
  , m_redirectedMethodSpecSigMemoTable(pRawMDCommon->GetMetaModelCommonRO()->CommonGetRowCount(mdtMethodSpec), NULL)
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
// Hides projected jupiter struct helper class & interfaces

struct HiddenWinRTTypeInfo
{
    LPCSTR szWinRTNamespace;
    LPCSTR szWinRTName;
};

#define DEFINE_PROJECTED_JUPITER_STRUCT(szWinRTNamespace, szWinRTName, szClrNamespace, szClrName, nClrAssemblyIndex, nContractAssemblyIndex, WinRTRedirectedTypeIndex, ClrRedirectedTypeIndex, fieldSizes) \
    {                                       \
        szWinRTNamespace,                   \
        szWinRTName "Helper"                \
    },

#define DEFINE_HIDDEN_WINRT_TYPE(szWinRTNamespace, szWinRTName) \
    {                                       \
        szWinRTNamespace,                   \
        szWinRTName                         \
    },

#define DEFINE_PROJECTED_TYPE(szWinRTNS, szWinRTName, szClrNS, szClrName, nClrAsmIdx, nContractAsmIdx, nWinRTIndex, nClrIndex, nWinMDTypeKind)

static const HiddenWinRTTypeInfo g_rgHiddenWinRTTypes[] = 
{
    #include "WinRTProjectedTypes.h"
};

#undef DEFINE_PROJECTED_JUPITER_STRUCT
#undef DEFINE_HIDDEN_WINRT_TYPE
#undef DEFINE_PROJECTED_TYPE

// Whether the WinRT type should be hidden from managed code
// Example: helper class for projected jupiter structs 
// (helper class interfaces are already private by default)
BOOL WinMDAdapter::IsHiddenWinRTType(LPCSTR szWinRTNamespace, LPCSTR szWinRTName)
{
    _ASSERTE(szWinRTNamespace && szWinRTName);
    
    for (UINT i = 0; i < _countof(g_rgHiddenWinRTTypes); i++)
    {
        if (0 == strcmp(szWinRTNamespace, g_rgHiddenWinRTTypes[i].szWinRTNamespace))
        {
            if (0 == strcmp(szWinRTName, g_rgHiddenWinRTTypes[i].szWinRTName))
            {
                return TRUE;
            }
        }
    }
    
    return FALSE;    
}

//------------------------------------------------------------------------------

struct RedirectedTypeInfo
{
    LPCSTR szWinRTNamespace;
    LPCSTR szWinRTName;
    LPCSTR szWinRTFullName;
    LPCWSTR wszWinRTFullName;
    LPCSTR szClrNamespace;
    LPCSTR szClrName;
    LPCSTR szClrFullName;
    LPCWSTR wszClrFullName;
    const WinMDAdapter::FrameworkAssemblyIndex nClrAssemblyIndex;
    const WinMDAdapter::ContractAssemblyIndex nContractAssemblyIndex;
    const WinMDAdapter::WinMDTypeKind nTypeKind;
#ifdef _DEBUG
    // Indexes for verification of constants RedirectedTypeIndex_*
    const UINT    dbg_nIndex1;
    const UINT    dbg_nIndex2;
#endif
};

#define DEFINE_PROJECTED_TYPE(szWinRTNS, szWinRTName, szClrNS, szClrName, nClrAsmIdx, nContractAsmIdx, nWinRTIndex, nClrIndex, nWinMDTypeKind) \
    { \
        szWinRTNS, \
        szWinRTName, \
        szWinRTNS "." szWinRTName, \
        L##szWinRTNS W(".") L##szWinRTName, \
        szClrNS, \
        szClrName, \
        szClrNS "." szClrName, \
        L##szClrNS W(".") L##szClrName, \
    	WinMDAdapter::FrameworkAssembly_ ## nClrAsmIdx, \
        WinMDAdapter::ContractAssembly_ ## nContractAsmIdx, \
        WinMDAdapter::WinMDTypeKind_ ## nWinMDTypeKind \
        COMMA_INDEBUG(WinMDAdapter::RedirectedTypeIndex_ ## nWinRTIndex) \
        COMMA_INDEBUG(WinMDAdapter::RedirectedTypeIndex_ ## nClrIndex) \
    },

static const RedirectedTypeInfo 
g_rgRedirectedTypes[WinMDAdapter::RedirectedTypeIndex_Count] = 
{
#include "WinRTProjectedTypes.h"


};
#undef SCAT
#undef DEFINE_PROJECTED_TYPE

//------------------------------------------------------------------------------

/*static*/ BOOL WinMDAdapter::ConvertWellKnownTypeNameFromWinRTToClr(LPCSTR *pszNamespace, LPCSTR *pszName)
{
    return ConvertWellKnownTypeNameFromWinRTToClr(pszNamespace, pszName, NULL);
}


// Maps well-known WinRT typenames to CLR typename. If the incoming name is not a well-known WinRT typename,
// this function leaves *pszNamespace and *pszName alone and returns FALSE. Otherwise, it overwrites
// them with pointers to const strings, returns the index into the rewrite table in *pIndex and returns TRUE.
//
// (Note: munged names from WinMDExp are not "well-known" names. Since the idea of munging them is to hide them,
// this function will not transform such names. Not to mention, it would be hard to return such strings
// in a function that no error return mechanism.)
//
/*static*/ BOOL WinMDAdapter::ConvertWellKnownTypeNameFromWinRTToClr(LPCSTR *pszNamespace, LPCSTR *pszName, UINT *pIndex)
{
    _ASSERTE(pszNamespace && pszName);
    for (UINT i = 0; i < RedirectedTypeIndex_Count; i++)
    {
        _ASSERTE(g_rgRedirectedTypes[i].dbg_nIndex1 == i);
        _ASSERTE(g_rgRedirectedTypes[i].dbg_nIndex2 == i);
        
        if (0 == strcmp(*pszNamespace, g_rgRedirectedTypes[i].szWinRTNamespace))
        {
            if (0 == strcmp(*pszName, g_rgRedirectedTypes[i].szWinRTName))
            {
                *pszNamespace = g_rgRedirectedTypes[i].szClrNamespace;
                *pszName      = g_rgRedirectedTypes[i].szClrName;
                if (pIndex)
                    *pIndex = i;
                return TRUE;
            }
        }
    }
    return FALSE;
}

/*static*/ BOOL WinMDAdapter::ConvertWellKnownFullTypeNameFromWinRTToClr(LPCWSTR *pwszFullName, RedirectedTypeIndex *pIndex)
{
    _ASSERTE(pwszFullName);
    for (UINT i = 0; i < RedirectedTypeIndex_Count; i++)
    {
        _ASSERTE(g_rgRedirectedTypes[i].dbg_nIndex1 == i);
        _ASSERTE(g_rgRedirectedTypes[i].dbg_nIndex2 == i);
        
        if (0 == wcscmp(*pwszFullName, g_rgRedirectedTypes[i].wszWinRTFullName))
        {
            *pwszFullName = g_rgRedirectedTypes[i].wszClrFullName;
             if (pIndex)
                *pIndex = static_cast<RedirectedTypeIndex>(i);
            return TRUE;
        }
    }
    return FALSE;
}

//------------------------------------------------------------------------------

// Maps well-known CLR typenames to WinRT typename. If the incoming name is not a well-known CLR typename,
// this function leaves *pszNamespace and *pszName alone and returns FALSE. Otherwise, it overwrites
// them with pointers to const strings and returns TRUE.
//
// (Note: munged names from WinMDExp are not "well-known" names. Since the idea of munging them is to hide them,
// this function will not transform such names. Not to mention, it would be hard to return such strings
// in a function that no error return mechanism.)
//
/*static*/ BOOL WinMDAdapter::ConvertWellKnownTypeNameFromClrToWinRT(LPCSTR *pszFullName)
{
    _ASSERTE(pszFullName);
   
    for (UINT i = 0; i < RedirectedTypeIndex_Count; i++)
    {
        if (0 == strcmp(g_rgRedirectedTypes[i].szClrFullName, *pszFullName))
        {
            *pszFullName = g_rgRedirectedTypes[i].szWinRTFullName;
            return TRUE;
        }
    }
    return FALSE;
}

/*static*/ BOOL WinMDAdapter::ConvertWellKnownTypeNameFromClrToWinRT(LPCSTR *pszNamespace, LPCSTR *pszName)
{
    _ASSERTE(pszNamespace);
    _ASSERTE(pszName);
   
    for (UINT i = 0; i < RedirectedTypeIndex_Count; i++)
    {
        if (0 == strcmp(g_rgRedirectedTypes[i].szClrNamespace, *pszNamespace) &&
            0 == strcmp(g_rgRedirectedTypes[i].szClrName, *pszName))
        {
            *pszNamespace   = g_rgRedirectedTypes[i].szWinRTNamespace;
            *pszName        = g_rgRedirectedTypes[i].szWinRTName;
            
            return TRUE;
        }
    }
    return FALSE;
}

//---------------------------------------------------------------------------------------
// 
// Returns names of redirected type 'index'.
// 
//static
void 
WinMDAdapter::GetRedirectedTypeInfo(
    RedirectedTypeIndex index, 
    LPCSTR *            pszClrNamespace, 
    LPCSTR *            pszClrName, 
    LPCSTR *            pszFullWinRTName,
    FrameworkAssemblyIndex * pFrameworkAssemblyIdx,
    ContractAssemblyIndex * pContractAssemblyIdx,
    WinMDTypeKind *     pWinMDTypeKind)
{
    _ASSERTE(index < RedirectedTypeIndex_Count);

    if (pszClrName != nullptr)
    {
        *pszClrName       = g_rgRedirectedTypes[index].szClrName;
    }
    if (pszClrNamespace != nullptr)
    {
        *pszClrNamespace  = g_rgRedirectedTypes[index].szClrNamespace;
    }
    if (pszFullWinRTName != nullptr)
    {
        *pszFullWinRTName = g_rgRedirectedTypes[index].szWinRTFullName;
    }	
    if (pFrameworkAssemblyIdx != nullptr)
    {
        *pFrameworkAssemblyIdx = g_rgRedirectedTypes[index].nClrAssemblyIndex;
    }
    if (pContractAssemblyIdx != nullptr)
    {
        *pContractAssemblyIdx = g_rgRedirectedTypes[index].nContractAssemblyIndex;
    }
    if (pWinMDTypeKind != nullptr)
    {
        *pWinMDTypeKind   = g_rgRedirectedTypes[index].nTypeKind;
    }
}

//---------------------------------------------------------------------------------------
// 
// Returns WinRT name of redirected type 'index'.
// 
//static 
LPCWSTR 
WinMDAdapter::GetRedirectedTypeFullWinRTName(
    RedirectedTypeIndex index)
{
    _ASSERTE(index < RedirectedTypeIndex_Count);
    
    return g_rgRedirectedTypes[index].wszWinRTFullName;
}

//---------------------------------------------------------------------------------------
// 
// Returns CLR name of redirected type 'index'.
// 
//static 
LPCSTR 
WinMDAdapter::GetRedirectedTypeFullCLRName(
    RedirectedTypeIndex index)
{
    _ASSERTE(index < RedirectedTypeIndex_Count);
    
    return g_rgRedirectedTypes[index].szClrFullName;
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
            if ((treatment != kTdOther) && ConvertWellKnownTypeNameFromWinRTToClr(&szTempNamespace, &szTempName))
            {
                if (treatment == kTdNormalNonAttribute)
                    treatment = kTdRedirectedToCLRType;
                else
                    treatment = kTdRedirectedToCLRAttribute;
            }
            else
            {
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

            if ((treatment == kTdNormalNonAttribute || treatment == kTdNormalAttribute) &&  // native WinMD, not redirected
                IsHiddenWinRTType(szTempNamespace, szTempName)      // hidden type
                )
            {
                // Hide those WinRT types. Examples of those WinRT types that we need to hide are Jupiter struct helpers
                treatment |= kTdMarkInternalFlag;
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

    case kTdRedirectedToCLRType:
        dwFlags &= ~tdPublic;
        dwFlags |= tdImport;
        break;

    case kTdRedirectedToCLRAttribute:
        dwFlags &= ~tdPublic;
        break;

        default:
            UNREACHABLE();
    }
    
    if ((treatment & kTdMarkAbstractFlag) == kTdMarkAbstractFlag)
    {
        dwFlags |= tdAbstract;
    }    
    if ((treatment & kTdMarkInternalFlag) == kTdMarkInternalFlag)
    {
        dwFlags &= ~tdPublic;
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
    IfFailGo(GetTypeRefTreatment(tkTypeRef, &treatment));
    _ASSERTE(treatment != kTrNotYetInitialized);

    ULONG treatmentClass = treatment & kTrClassMask;
    if (treatmentClass == kTrClassWellKnownRedirected)
    {
        ULONG nRewritePairIndex = treatment & ~kTrClassMask;
        if (pszNamespace != NULL)
            *pszNamespace = g_rgRedirectedTypes[nRewritePairIndex].szClrNamespace;
        if (pszName != NULL)
            *pszName = g_rgRedirectedTypes[nRewritePairIndex].szClrName;
        if (ptkResolutionScope != NULL)
        {
            ContractAssemblyIndex assemblyIndex  = g_rgRedirectedTypes[nRewritePairIndex].nContractAssemblyIndex;
            _ASSERTE(assemblyIndex < ContractAssembly_Count);
            *ptkResolutionScope = mdtAssemblyRef | (m_rawAssemblyRefCount + assemblyIndex + 1);
        }
    }
    else
    {
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
                if (ptkResolutionScope) *ptkResolutionScope = mdtAssemblyRef | (m_rawAssemblyRefCount + ContractAssembly_SystemRuntime + 1);
                break;
    
            case kTrSystemAttribute:
                if (pszNamespace != NULL)       *pszNamespace = "System";
                if (pszName != NULL)            *pszName = "Attribute";                
                if (ptkResolutionScope != NULL) *ptkResolutionScope = mdtAssemblyRef | (m_rawAssemblyRefCount + ContractAssembly_SystemRuntime + 1);
                break;
    
            default:
                _ASSERTE(!"Unknown treatment value.");
                break;
    
        }
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
        UINT redirIndex;
        if (ConvertWellKnownTypeNameFromWinRTToClr(&szFromNamespace, &szFromName, &redirIndex))
        {
            treatment = kTrClassWellKnownRedirected | redirIndex;
        }
        else
        {
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
// Get TypeRef's index in array code:g_rgRedirectedTypes.
// Returns S_OK if TypeRef is redirected and fills its index (*pIndex).
// Returns S_FALSE if type is not well known redirected type (*pIndex is not initialized).

// 
HRESULT 
WinMDAdapter::GetTypeRefRedirectedInfo(
    mdTypeRef             tkTypeRef, 
    RedirectedTypeIndex * pIndex)
{
    _ASSERTE(TypeFromToken(tkTypeRef) == mdtTypeRef);
    _ASSERTE(pIndex != NULL);
    
    HRESULT hr;
    ULONG   treatment;
    IfFailGo(GetTypeRefTreatment(tkTypeRef, &treatment));
    
    ULONG treatmentClass = treatment & kTrClassMask;
    if (treatmentClass == kTrClassWellKnownRedirected)
    {
        *pIndex = (RedirectedTypeIndex)(treatment & ~kTrClassMask);
        _ASSERTE(*pIndex < RedirectedTypeIndex_Count);
        hr = S_OK;
    }
    else
    {
        // Do not initialize *pIndex
        hr = S_FALSE;
    }
    
ErrExit:
    return hr;
}

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

//------------------------------------------------------------------------------

// We must optionaly add an assembly ref for System.Numerics.Vectors.dll since this assembly is not available
// on downlevel platforms. 
//
// This function assumes that System.Numerics.Vectors.dll is the last assembly that
// we add so if we find a reference then we return ContractAssembly_Count otherwise we return 
// ContractAssembly_Count - 1. 
int WinMDAdapter::GetExtraAssemblyRefCount()
{
    HRESULT hr;

    if (m_extraAssemblyRefCount == -1)
    {
        mdAssemblyRef tkSystemNumericsVectors = TokenFromRid(m_rawAssemblyRefCount + ContractAssembly_SystemNumericsVectors + 1, mdtAssemblyRef);
        ULONG cTypeRefRecs = m_pRawMetaModelCommonRO->CommonGetRowCount(mdtTypeRef);
        BOOL systemNumericsVectorsTypeFound = FALSE;

        for (ULONG i = 1; i <= cTypeRefRecs; i++)
        {
            mdToken tkResolutionScope;
            mdTypeRef tkTypeRef = TokenFromRid(i, mdtTypeRef);

            // Get the resolution scope(AssemblyRef) token for the type. GetTypeRefProps does the type redirection.
            IfFailGo(GetTypeRefProps(tkTypeRef, nullptr, nullptr, &tkResolutionScope));

            if (tkResolutionScope == tkSystemNumericsVectors)
            {
                systemNumericsVectorsTypeFound = TRUE;
                break;
            }
        }

        if (systemNumericsVectorsTypeFound)
        {
            m_extraAssemblyRefCount = ContractAssembly_Count;
        }
        else
        {
            m_extraAssemblyRefCount = ContractAssembly_Count - 1;
        }
    }

ErrExit:
    if (m_extraAssemblyRefCount == -1)
    {
        // Setting m_extraAssemblyRefCount to ContractAssembly_Count so that this function returns a stable value and
        // that if there is a System.Numerics type ref that it does not have a dangling assembly ref
        m_extraAssemblyRefCount = ContractAssembly_Count; 
    }

    return m_extraAssemblyRefCount;
}

//------------------------------------------------------------------------------

/*static*/
void WinMDAdapter::GetExtraAssemblyRefProps(FrameworkAssemblyIndex index,
                                            LPCSTR* ppName,
                                            AssemblyMetaDataInternal* pContext,
                                            PCBYTE * ppPublicKeytoken,
                                            DWORD* pTokenLength,
                                            DWORD* pdwFlags)
{
    _ASSERTE(index >= 0 && index < FrameworkAssembly_Count);
    _ASSERTE(index != FrameworkAssembly_Mscorlib);

    if (ppName)
    {
        *ppName = GetExtraAssemblyRefNameFromIndex((FrameworkAssemblyIndex)index);
    }

    if (pContext)
    {
        ::memset(pContext, 0, sizeof(AssemblyMetaDataInternal));

        pContext->usMajorVersion = VER_ASSEMBLYMAJORVERSION;
        pContext->usMinorVersion = VER_ASSEMBLYMINORVERSION;
        pContext->usBuildNumber = VER_ASSEMBLYBUILD;
        pContext->usRevisionNumber = VER_ASSEMBLYBUILD_QFE;

        pContext->szLocale = "";
    }

    if (ppPublicKeytoken)
    {
#ifdef FEATURE_CORECLR
        if (index == FrameworkAssembly_Mscorlib)
        {
            *ppPublicKeytoken = g_rbTheSilverlightPlatformKeyToken;
            *pTokenLength = sizeof(g_rbTheSilverlightPlatformKeyToken);
        }
        else
#endif
        {
            if (index == FrameworkAssembly_SystemNumericsVectors || index == FrameworkAssembly_SystemRuntime || index == FrameworkAssembly_SystemObjectModel)
            {
                *ppPublicKeytoken = s_pbContractPublicKeyToken;
                *pTokenLength = sizeof(s_pbContractPublicKeyToken);
            }
            else
            {
                *ppPublicKeytoken = s_pbFrameworkPublicKeyToken;
                *pTokenLength = sizeof(s_pbFrameworkPublicKeyToken);
            }
        }
    }

    if (pdwFlags)
    {
        // ppPublicKeytoken contains the public key token, not the whole key.
        *pdwFlags = 0;
    }
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
    IfFailGo(GetMethodDefTreatment(tkMethodDef, &mdTreatment));

    DWORD dwAttr = pdwAttr ? *pdwAttr: 0;
    DWORD dwImplFlags = pdwImplFlags ? *pdwImplFlags : 0;
    ULONG ulRVA = pulRVA ? *pulRVA : 0; 

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

        case kMdHiddenImpl:
            dwAttr &= ~mdMemberAccessMask;
            dwAttr |= mdPrivate;
            // fall-through

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

        case kMdRenameToDisposeMethod:
            ulRVA = 0;
            dwImplFlags |= miRuntime|miInternalCall;
            if(pszName)
            {
                *pszName = "Dispose";
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

    case mdtMemberRef:
    {
        //
        // We need to rename the MemberRef for IClosable.Close as well
        // so that the MethodImpl for Dispose method can correctly be shown as IDisposable.Dispose
        // instead of IDisposable.Close
        //
        UINT nIndex = WinMDAdapter::RedirectedTypeIndex_Invalid;
        if (pszNewName && 
            CheckIfMethodImplImplementsARedirectedInterface(tkMember, &nIndex) == S_OK &&
            nIndex == WinMDAdapter::RedirectedTypeIndex_Windows_Foundation_IClosable)
        {
            *pszNewName = "Dispose";
        }
    }
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
        if (((mdTreatment & kMdTreatmentMask) != kMdOther) && 
            ((mdTreatment & kMdTreatmentMask) != kMdHiddenImpl) &&
            ((mdTreatment & kMdTreatmentMask) != kMdRenameToDisposeMethod))
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
    if (mdTreatment == kMdOther)
    {
        // we want to hide the method if it implements only redirected interfaces
        // Also we want to check if the methodImpl is IClosable.Close then we will change the name.
        bool fSeenRedirectedInterfaces = false;
        bool fSeenNonRedirectedInterfaces = false;
        
        bool isIClosableCloseMethod = false;
        
        mdToken tkMethodImplFirst;
        ULONG count;
        mdTypeRef mtTypeRef;
        IfFailGo(m_pRawMetaModelCommonRO->CommonGetMethodImpls(tkDeclaringTypeDef, &tkMethodImplFirst, &count));
        for (ULONG i = 0; i < count; i++)
        {
            mdToken tkMethodImpl = tkMethodImplFirst + i;
            mdToken tkBody, tkDecl;
            IfFailGo(m_pRawMetaModelCommonRO->CommonGetMethodImplProps(tkMethodImpl, &tkBody, &tkDecl));

            if (tkBody == tkMethodDef)
            {
                // See if this MethodImpl implements a redirected interface
                UINT nIndex;
                IfFailGo(CheckIfMethodImplImplementsARedirectedInterface(tkDecl, &nIndex));
                if (hr == S_FALSE)
                {
                    // Now we know this implements a non-redirected interface
                    // But we need to keep looking, just in case we got a MethodImpl that implements
                    // the IClosable.Close method and needs to be renamed
                    fSeenNonRedirectedInterfaces = true;
                }
                else if (SUCCEEDED(hr))
                {
                    fSeenRedirectedInterfaces = true;
                    if (nIndex == WinMDAdapter::RedirectedTypeIndex_Windows_Foundation_IClosable)
                    {
                        // This method implements IClosable.Close 
                        // Let's rename it to Dispose later
                        // Once we know this implements IClosable.Close, we are done looking as we know 
                        // we won't hide it
                        isIClosableCloseMethod = true;
                        break;
                    }
                }
            }
        }

        if (isIClosableCloseMethod)
        {
            // Rename IClosable.Close to Dispose
            mdTreatment = kMdRenameToDisposeMethod;
        }
        else if (fSeenRedirectedInterfaces && !fSeenNonRedirectedInterfaces)
        {
            // Only hide if all the interfaces implemented are redirected
            mdTreatment = kMdHiddenImpl;
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
    HRESULT hr;
    _ASSERTE(szName);

    if (ConvertWellKnownTypeNameFromClrToWinRT(&szName))
    {
        mdCustomAttribute tkCA;
        IfFailGo(m_pRawMetaModelCommonRO->CommonGetCustomAttributeByNameEx(tkObj, szName, &tkCA, NULL, NULL));
        if (hr == S_FALSE)
            goto ErrExit;
        if (ptkCA)
            *ptkCA = tkCA;
        IfFailGo(GetCustomAttributeBlob(tkCA, ppData, pcbData));
    }
    else
    {
        IfFailGo(m_pRawMetaModelCommonRO->CommonGetCustomAttributeByNameEx(tkObj, szName, ptkCA, ppData, pcbData));
    }
  ErrExit:
    return hr;
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
    HRESULT hr;

    _ASSERTE(TypeFromToken(tkCA) == mdtCustomAttribute);
    ULONG index = RidFromToken(tkCA) - 1;

    // If someone already queried this CA, use the previous result.
    CABlob * pCABlob;
    IfFailGo(m_redirectedCABlobsMemoTable.GetEntry(index, &pCABlob));
    if (hr == S_FALSE)
    {
        // No, we're the first. Initialize the entry (keeping in mind we may be racing with other threads.)
        pCABlob = CABlob::NOREDIRECT;
        mdToken tkOwner;
        mdToken tkCtor;
        IfFailGo(m_pRawMetaModelCommonRO->CommonGetCustomAttributeProps(tkCA, &tkOwner, &tkCtor, NULL, NULL));
        if (TypeFromToken(tkOwner) == mdtTypeDef)   // AttributeUsageAttribute only goes on a typeDef, so if the owner isn't a typeDef, no point in going further.
        {
            if (TypeFromToken(tkCtor) == mdtMemberRef)  // REX has promised to use a memberRef (not Def) here.
            {
                mdToken tkCtorType;
                IfFailGo(m_pRawMetaModelCommonRO->CommonGetMemberRefProps(tkCtor, &tkCtorType));
                if (TypeFromToken(tkCtorType) == mdtTypeRef)  // REX has promised to use a typeRef (not a Def, or heavens forbid, a Spec) here
                {
                    RedirectedTypeIndex redirectedTypeIndex;
                    IfFailGo(GetTypeRefRedirectedInfo(tkCtorType, &redirectedTypeIndex));
                    _ASSERTE((hr == S_OK) || (hr == S_FALSE));
                    
                    if ((hr == S_OK) && (redirectedTypeIndex == RedirectedTypeIndex_Windows_Foundation_Metadata_AttributeUsageAttribute))
                    {
                        // We found a Windows.Foundation.Metadata.AttributeUsageAttribute. The TypeRef redirection already makes this 
                        // look like a System.AttributeUsageAttribute. Must munge the blob so that it matches the CLR expections.
                        BOOL  allowMultiple;
                        DWORD clrTargetValue;
                        IfFailGo(TranslateWinMDAttributeUsageAttribute(tkOwner, &clrTargetValue, &allowMultiple));
                        if (hr == S_OK)
                        {
                            CABlob *pNewCABlob = NULL;
                            IfFailGo(CreateClrAttributeUsageAttributeCABlob(clrTargetValue, allowMultiple, &pNewCABlob));
                            pCABlob = pNewCABlob;
                        }
                    }
                }
            }
        }

        IfFailGo(m_redirectedCABlobsMemoTable.InitEntry(index, &pCABlob));
    }

    _ASSERTE(pCABlob != NULL);
    const void *pData;
    ULONG      cbData;
    if (pCABlob == CABlob::NOREDIRECT)
    {
        // The normal case: don't rewrite the blob.
        IfFailGo(m_pRawMetaModelCommonRO->CommonGetCustomAttributeProps(tkCA, NULL, NULL, &pData, &cbData));
    }
    else
    {
        // The special cases: Blob was rewritten, return the rewritten blob.
        pData = pCABlob->data;
        cbData = pCABlob->cbBlob;
    }
    if (ppData)
        *ppData = pData;
    if (pcbData)
        *pcbData = cbData;

    hr = S_OK;
  ErrExit:
    return hr;
}

//------------------------------------------------------------------------------

// Note: This method will look in a cache for the reinterpreted signature, but does not add any values to 
// the cache or do any work on failure.  If we can't find it then it returns S_FALSE.
HRESULT WinMDAdapter::GetCachedSigForToken(
    mdToken          token,             // [IN] given token
    MemoTable<SigData*, SigData::Destroy> &memoTable, // [IN] the MemoTable to use
    ULONG           *pcbSigBlob,        // [OUT] count of bytes in the signature blob
    PCCOR_SIGNATURE *ppSig,             // [OUT] new signature
    BOOL            *pfPassThrough      // [OUT] did the cache say we don't need to reinterpret this sig?
)
{
    _ASSERTE(pfPassThrough != NULL);

    HRESULT hr;
    
    ULONG index = RidFromToken(token) - 1;

    // If someone already queried this method signature, use the previous result.
    SigData *pSigData = NULL;
    IfFailGo(memoTable.GetEntry(index, &pSigData));
    if (hr == S_FALSE)
    {
        *pfPassThrough = FALSE;
        return S_FALSE;
    }

    _ASSERTE(pSigData != NULL);

    if (pSigData == SigData::NOREDIRECT)
    {
        // The normal case: don't rewrite the signature.
        *pfPassThrough = TRUE;
        return S_FALSE;
    }
    else
    {
        *pfPassThrough = FALSE;
        // The signature was rewritten, return the rewritten sig.
        if (ppSig)
            *ppSig = (PCCOR_SIGNATURE) pSigData->data;
        if (pcbSigBlob)
            *pcbSigBlob = pSigData->cbSig;
    }

    hr = S_OK;

  ErrExit:
    return hr;
}

//------------------------------------------------------------------------------
// static
HRESULT WinMDAdapter::InsertCachedSigForToken(
    mdToken          token,             // [IN] given token
    MemoTable<SigData*, SigData::Destroy> &memoTable, // [IN] the MemoTable to use
    SigData        **ppSigData          // [IN, OUT] new signature or SigData::NOREDIRECT if the signature didn't need to be reparsed,
)                                       // will be updated with another (but identical) SigData* if this thread lost the race
{
    _ASSERTE(ppSigData != NULL && *ppSigData != NULL);

    HRESULT hr;
    ULONG index = RidFromToken(token) - 1;

    IfFailGo(memoTable.InitEntry(index, ppSigData));
    hr = S_OK;

  ErrExit:
    return hr;
}

//------------------------------------------------------------------------------
static HRESULT FinalizeSignatureRewrite(
    SigBuilder      & newSig,
    BOOL              fChangedSig,
    WinMDAdapter::SigData **ppSigData
    DEBUG_ARG(ULONG cbOrigSigBlob)
    )
{
    // Make sure we didn't lose anything, since we wrote out the full signature.
    ULONG  cbNewSigLen;
    BYTE * pNewSigBytes = (BYTE *) newSig.GetSignature(&cbNewSigLen);
    _ASSERTE(cbNewSigLen == cbOrigSigBlob);  // Didn't lose any data nor add anything

    // Set the output SigData appropriately.
    if (fChangedSig)
    {
        *ppSigData = WinMDAdapter::SigData::Create(cbNewSigLen, pNewSigBytes);
        
        if (*ppSigData == NULL)
        {
            return E_OUTOFMEMORY;
        }
    }
    else
    {
        *ppSigData = WinMDAdapter::SigData::NOREDIRECT;
    }
    return S_OK;
}

//------------------------------------------------------------------------------

// Purpose: Translate method signatures containing classes that we're projecting as value types, and vice versa.
// Example: ELEMENT_TYPE_CLASS [IReference<T>] to ELEMENT_TYPE_VALUETYPE [Nullable<T>]
// Example: ELEMENT_TYPE_VALUETYPE [Windows.Foundation.HResult] to ELEMENT_TYPE_CLASS [Exception]
HRESULT WinMDAdapter::ReinterpretMethodSignature(
    ULONG             cbOrigSigBlob,    // [IN] count of bytes in the original signature blob
    PCCOR_SIGNATURE   pOrigSig,         // [IN] original signature
    SigData         **ppSigData         // [OUT] new signature or SigData::NOREDIRECT
)
{
    _ASSERTE(pOrigSig != NULL);
    _ASSERTE(ppSigData != NULL);

    HRESULT hr;
    
    // @REVISIT_TODO: Need to allocate memory here.  We cannot take a lock though (such as any lock needed by 'new'), or we can get 
    // into deadlocks from profilers that need to inspect metadata to walk the stack.  Needs some help from some loader/ngen experts.

    BOOL fChangedSig = FALSE;

    // The following implements MethodDef signature parsing, per ECMA CLI spec, section 23.2.1.
    SigParser sigParser(pOrigSig, cbOrigSigBlob);
    SigBuilder newSig(cbOrigSigBlob);   // We will not change the signature size, just modify it a bit (E_T_CLASS <-> E_T_VALUETYPE)
    
    // Read calling convention info - Note: Calling convention is always one byte
    ULONG callingConvention;
    IfFailGo(sigParser.GetCallingConvInfo(&callingConvention));
    _ASSERTE((callingConvention & 0xff) == callingConvention);
    newSig.AppendByte((BYTE)callingConvention);

    // If it is generic, read the generic parameter count
    if ((callingConvention & CORINFO_CALLCONV_GENERIC) != 0)
    {
        ULONG genericArgsCount;
        IfFailGo(sigParser.GetData(&genericArgsCount));
        newSig.AppendData(genericArgsCount);
    }
    
    // Read number of locals / method parameters
    ULONG cParameters;
    IfFailGo(sigParser.GetData(&cParameters));
    newSig.AppendData(cParameters);
    
    if (callingConvention != CORINFO_CALLCONV_LOCAL_SIG)
    {
        // Read return type
        IfFailGo(RewriteTypeInSignature(&sigParser, &newSig, &fChangedSig));
    }

    // Visit each local / parameter
    for (ULONG i = 0; i < cParameters; i++)
    {
        IfFailGo(RewriteTypeInSignature(&sigParser, &newSig, &fChangedSig));
    }

    IfFailGo(FinalizeSignatureRewrite(newSig, fChangedSig, ppSigData DEBUG_ARG(cbOrigSigBlob)));
    return S_OK;
    
ErrExit:
    Debug_ReportError("Couldn't parse a signature in WinMDAdapter::ReinterpretMethodSignature!");
    return hr;
} // WinMDAdapter::ReinterpretMethodSignature


//------------------------------------------------------------------------------

// Purpose: Translate FieldDef signatures containing classes that we're projecting as value types, and vice versa.
// Example: ELEMENT_TYPE_VALUETYPE [Windows.Foundation.HResult] to ELEMENT_TYPE_CLASS [Exception]
HRESULT WinMDAdapter::ReinterpretFieldSignature(
    ULONG             cbOrigSigBlob,    // [IN] count of bytes in the original signature blob
    PCCOR_SIGNATURE   pOrigSig,         // [IN] original signature
    SigData         **ppSigData         // [OUT] new signature or SigData::NOREDIRECT
)
{
    _ASSERTE(pOrigSig != NULL);
    _ASSERTE(ppSigData != NULL);

    HRESULT hr = S_OK;
    BOOL fChangedSig = FALSE;
    
    // The following implements FieldDef signature parsing, per ECMA CLI spec, section 23.2.4.
    // Format is FIELD [custom modifiers]* Type
    SigParser sigParser(pOrigSig, cbOrigSigBlob);
    SigBuilder newSig(cbOrigSigBlob);   // We will not change the signature size, just modify it a bit (E_T_CLASS <-> E_T_VALUETYPE)
    
    // Read calling convention info - this should be IMAGE_CEE_CS_CALLCONV_FIELD.
    ULONG callingConvention;
    IfFailGo(sigParser.GetCallingConvInfo(&callingConvention));
    _ASSERTE((callingConvention & 0xff) == callingConvention);
    _ASSERTE(callingConvention == IMAGE_CEE_CS_CALLCONV_FIELD);
    newSig.AppendByte((BYTE)callingConvention);

    // Rewrite field type
    IfFailGo(RewriteTypeInSignature(&sigParser, &newSig, &fChangedSig));

    IfFailGo(FinalizeSignatureRewrite(newSig, fChangedSig, ppSigData DEBUG_ARG(cbOrigSigBlob)));
    return S_OK;
    
ErrExit:
    Debug_ReportError("Couldn't parse a signature in WinMDAdapter::ReinterpretFieldSignature!");
    return hr;
} // WinMDAdapter::ReinterpretFieldSignature


//------------------------------------------------------------------------------

// Purpose: Translate TypeSpec signatures containing classes that we're projecting as value types, and vice versa.
HRESULT WinMDAdapter::ReinterpretTypeSpecSignature(
    ULONG             cbOrigSigBlob,    // [IN] count of bytes in the original signature blob
    PCCOR_SIGNATURE   pOrigSig,         // [IN] original signature
    SigData         **ppSigData         // [OUT] new signature or SigData::NOREDIRECT
)
{
    _ASSERTE(pOrigSig != NULL);
    _ASSERTE(ppSigData != NULL);

    HRESULT hr = S_OK;
    BOOL fChangedSig = FALSE;

    // The following implements TypeSpec signature parsing, per ECMA CLI spec, section 23.2.14.
    // Format is [custom modifiers]* Type
    SigParser sigParser(pOrigSig, cbOrigSigBlob);
    SigBuilder newSig(cbOrigSigBlob);   // We will not change the signature size, just modify it a bit (E_T_CLASS <-> E_T_VALUETYPE)
    
    // Rewrite the type
    IfFailGo(RewriteTypeInSignature(&sigParser, &newSig, &fChangedSig));

    IfFailGo(FinalizeSignatureRewrite(newSig, fChangedSig, ppSigData DEBUG_ARG(cbOrigSigBlob)));
    return S_OK;
    
ErrExit:
    Debug_ReportError("Couldn't parse a signature in WinMDAdapter::ReinterpretTypeSpecSignature!");
    return hr;
} // WinMDAdapter::ReinterpretTypeSpecSignature


//------------------------------------------------------------------------------

// Purpose: Translate MethodSpec signatures containing classes that we're projecting as value types, and vice versa.
HRESULT WinMDAdapter::ReinterpretMethodSpecSignature(
    ULONG             cbOrigSigBlob,    // [IN] count of bytes in the original signature blob
    PCCOR_SIGNATURE   pOrigSig,         // [IN] original signature
    SigData         **ppSigData         // [OUT] new signature or SigData::NOREDIRECT
)
{
    _ASSERTE(pOrigSig != NULL);
    _ASSERTE(ppSigData != NULL);

    HRESULT hr = S_OK;
    BOOL fChangedSig = FALSE;

    // The following implements MethodSpec signature parsing, per ECMA CLI spec, section 23.2.15.
    // Format is GENERICINST GenArgCount Type+
    SigParser sigParser(pOrigSig, cbOrigSigBlob);
    SigBuilder newSig(cbOrigSigBlob);   // We will not change the signature size, just modify it a bit (E_T_CLASS <-> E_T_VALUETYPE)
    
    // Read calling convention info - this should be IMAGE_CEE_CS_CALLCONV_GENERICINST.
    ULONG callingConvention;
    IfFailGo(sigParser.GetCallingConvInfo(&callingConvention));
    _ASSERTE((callingConvention & 0xff) == callingConvention);
    _ASSERTE(callingConvention == IMAGE_CEE_CS_CALLCONV_GENERICINST);
    newSig.AppendByte((BYTE)callingConvention);

    // Read number of generic arguments
    ULONG cArguments;
    IfFailGo(sigParser.GetData(&cArguments));
    newSig.AppendData(cArguments);

    // Rewrite each argument
    for (ULONG i = 0; i < cArguments; i++)
    {
        IfFailGo(RewriteTypeInSignature(&sigParser, &newSig, &fChangedSig));
    }

    IfFailGo(FinalizeSignatureRewrite(newSig, fChangedSig, ppSigData DEBUG_ARG(cbOrigSigBlob)));
    return S_OK;
    
ErrExit:
    Debug_ReportError("Couldn't parse a signature in WinMDAdapter::ReinterpretMethodSpecSignature!");
    return hr;
} // WinMDAdapter::ReinterpretMethodSpecSignature


//------------------------------------------------------------------------------
// 
// We expose some WinRT types to managed as CLR types, while changing reference type <-> value type:
//  E_T_CLASS Windows.Foundation.IReference<T>                  ---> E_T_VALUETYPE System.Nullable<T>
//  E_T_CLASS Windows.Foundation.Collections.IKeyValuePair<U,V> ---> E_T_VALUETYPE System.Collections.Generic.KeyValuePair<U,V>
//  E_T_VALUETYPE Windows.UI.Xaml.Interop.TypeName              ---> E_T_CLASS System.Type
//  E_T_VALUETYPE Windows.Foundation.HResult                    ---> E_T_CLASS System.Exception
HRESULT WinMDAdapter::RewriteTypeInSignature(
    SigParser *  pSigParser, 
    SigBuilder * pSigBuilder, 
    BOOL *       pfChangedSig)
{
    HRESULT hr;
    
    BYTE elementType;
    IfFailGo(pSigParser->GetByte(&elementType));

    switch (elementType)
    {
    // Simple types with no additional data in the signature
    case ELEMENT_TYPE_VOID:
    case ELEMENT_TYPE_BOOLEAN:
    case ELEMENT_TYPE_CHAR:
    case ELEMENT_TYPE_I1:
    case ELEMENT_TYPE_U1:
    case ELEMENT_TYPE_I2:
    case ELEMENT_TYPE_U2:
    case ELEMENT_TYPE_I4:
    case ELEMENT_TYPE_U4:
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
    case ELEMENT_TYPE_R4:
    case ELEMENT_TYPE_R8:
    case ELEMENT_TYPE_STRING:
    case ELEMENT_TYPE_I:
    case ELEMENT_TYPE_U:
    case ELEMENT_TYPE_OBJECT:
    case ELEMENT_TYPE_TYPEDBYREF:     // TYPEDREF  (it takes no args) a typed reference to some other type
        {
            pSigBuilder->AppendByte(elementType);
            break;
        }

        // Read a token
    case ELEMENT_TYPE_CLASS:
        {
            mdToken token;
            IfFailGo(pSigParser->GetToken(&token));
            
            if (TypeFromToken(token) == mdtTypeRef)
            {
                RedirectedTypeIndex nRedirectedTypeIndex;
                IfFailGo(this->GetTypeRefRedirectedInfo(token, &nRedirectedTypeIndex));
                _ASSERTE((hr == S_OK) || (hr == S_FALSE));
                
                if (hr == S_OK)
                {   // TypeRef is well known redirectetd type (with index in array code:g_rgRedirectedTypes)
                    if (nRedirectedTypeIndex == RedirectedTypeIndex_Windows_Foundation_IReference ||
                        nRedirectedTypeIndex == RedirectedTypeIndex_Windows_Foundation_Collections_IKeyValuePair)
                    {   // The TypeRef name was changed to System.Nullable or System.Collections.Generic.KeyValuePair`2 (value type, not class)
                        elementType = ELEMENT_TYPE_VALUETYPE;
                        *pfChangedSig = TRUE;
                    }
                }
                // We do not want to return S_FALSE
                hr = S_OK;
            }
            
            pSigBuilder->AppendByte(elementType);
            pSigBuilder->AppendToken(token);
            
            break;
        }
    case ELEMENT_TYPE_VALUETYPE:
        {
            mdToken token;
            IfFailGo(pSigParser->GetToken(&token));
            
            if (TypeFromToken(token) == mdtTypeRef)
            {
                RedirectedTypeIndex nRedirectedTypeIndex;
                IfFailGo(this->GetTypeRefRedirectedInfo(token, &nRedirectedTypeIndex));
                _ASSERTE((hr == S_OK) || (hr == S_FALSE));
                
                if (hr == S_OK)
                {   // TypeRef is well known redirectetd type (with index in array code:g_rgRedirectedTypes)
                    if (nRedirectedTypeIndex == RedirectedTypeIndex_Windows_UI_Xaml_Interop_TypeName ||
                        nRedirectedTypeIndex == RedirectedTypeIndex_Windows_Foundation_HResult)
                    {
                        // TypeIdentifier and HResult are all value types in winmd and are mapped to reference
                        // types in CLR: Type, and Exception.
                        elementType = ELEMENT_TYPE_CLASS;
                        *pfChangedSig = TRUE;
                    }
                }
                // We do not want to return S_FALSE
                hr = S_OK;
            }
            
            pSigBuilder->AppendByte(elementType);
            pSigBuilder->AppendToken(token);
            
            break;
        }
        
        // Read a type
    case ELEMENT_TYPE_SZARRAY:  // SZARRAY <type>
    case ELEMENT_TYPE_PTR:      // PTR <type>
    case ELEMENT_TYPE_BYREF:    // BYREF <type>
    case ELEMENT_TYPE_SENTINEL: // sentinel for VARARGS ("..." in the parameter list), it behaves as prefix to next arg
        {
            pSigBuilder->AppendByte(elementType);
            
            IfFailGo(RewriteTypeInSignature(pSigParser, pSigBuilder, pfChangedSig));
            break;
        }

        // Read a token then a type.  That type could be another custom modifier.
    case ELEMENT_TYPE_CMOD_REQD:     // required C modifier : E_T_CMOD_REQD <mdTypeRef/mdTypeDef> [followed by a type, or another custom modifier]
    case ELEMENT_TYPE_CMOD_OPT:      // optional C modifier : E_T_CMOD_OPT <mdTypeRef/mdTypeDef>
        {
            pSigBuilder->AppendByte(elementType);
            
            mdToken token;
            IfFailGo(pSigParser->GetToken(&token));
            pSigBuilder->AppendToken(token);
            
            // Process next type or custom modifier
            IfFailGo(RewriteTypeInSignature(pSigParser, pSigBuilder, pfChangedSig));
            break;
        }

        // Read a number
    case ELEMENT_TYPE_VAR:     // a class type variable VAR <number>
    case ELEMENT_TYPE_MVAR:    // a method type variable MVAR <number>
        {
            pSigBuilder->AppendByte(elementType);
            
            ULONG number;
            IfFailGo(pSigParser->GetData(&number));
            pSigBuilder->AppendData(number);
            break;
        }

    case ELEMENT_TYPE_ARRAY:     // MDARRAY <type> <rank> <bcount> <bound1> ... <lbcount> <lb1> ...
        {
            pSigBuilder->AppendByte(elementType);
            
            // Read array type
            IfFailGo(RewriteTypeInSignature(pSigParser, pSigBuilder, pfChangedSig));
            
            // Read rank
            ULONG rank;
            IfFailGo(pSigParser->GetData(&rank));
            pSigBuilder->AppendData(rank);

            // If rank is 0, then there's nothing else in the array signature
            if (rank != 0)
            {
                // Read number of dimension sizes
                ULONG cDimensionSizes;
                IfFailGo(pSigParser->GetData(&cDimensionSizes));
                pSigBuilder->AppendData(cDimensionSizes);
                
                // Read all dimension sizes
                for (ULONG i = 0; i < cDimensionSizes; i++)
                {
                    ULONG dimensionSize;
                    IfFailGo(pSigParser->GetData(&dimensionSize));
                    pSigBuilder->AppendData(dimensionSize);
                }
                
                // Read number of lower bounds
                ULONG cLowerBounds;
                IfFailGo(pSigParser->GetData(&cLowerBounds));
                pSigBuilder->AppendData(cLowerBounds);

                // Read all lower bounds
                for (ULONG i = 0; i < cLowerBounds; i++)
                {
                    ULONG lowerBound;
                    IfFailGo(pSigParser->GetData(&lowerBound));
                    pSigBuilder->AppendData(lowerBound);
                }
            }
            break;
        }

    case ELEMENT_TYPE_GENERICINST:     // GENERICINST <generic type> <argCnt> <arg1> ... <argn>
        {
            pSigBuilder->AppendByte(elementType);
            
            // Read the generic type
            IfFailGo(RewriteTypeInSignature(pSigParser, pSigBuilder, pfChangedSig));
            
            // Read arg count
            ULONG cGenericTypeArguments;
            IfFailGo(pSigParser->GetData(&cGenericTypeArguments));
            pSigBuilder->AppendData(cGenericTypeArguments);
            
            // Read each type argument
            for (ULONG i = 0; i < cGenericTypeArguments; i++)
            {
                IfFailGo(RewriteTypeInSignature(pSigParser, pSigBuilder, pfChangedSig));
            }
            break;
        }
        
    case ELEMENT_TYPE_FNPTR:     // FNPTR <complete sig for the function including calling convention>
        /*
        // FNPTR is not supported in C#/VB, thefore this is not a main scenario.
        // This implementation was late during .NET 4.5, but may be useful in future releases when we decide to support more languages for managed WinMD implementation.
        {
            pSigBuilder->AppendByte(elementType);
            
            // Read calling convention
            DWORD callingConvention;
            IfFailGo(pSigParser->GetData(&callingConvention));
            pSigBuilder->AppendData(callingConvention);
            
            // Read arg count
            ULONG cArgs;
            IfFailGo(pSigParser->GetData(&cArgs));
            pSigBuilder->AppendData(cArgs);
            
            // Read return argument
            IfFailGo(RewriteTypeInSignature(pSigParser, pSigBuilder, pfChangedSig));
            
            // Read each argument
            for (ULONG i = 0; i < cArgs; i++)
            {
                IfFailGo(RewriteTypeInSignature(pSigParser, pSigBuilder, pfChangedSig));
            }
            break;
        }
        */
        Debug_ReportError("ELEMENT_TYPE_FNPTR signature parsing in WinMD Adapter is NYI.");
        IfFailGo(E_FAIL);
        
    case ELEMENT_TYPE_END:
    case ELEMENT_TYPE_INTERNAL:     // INTERNAL <typehandle>  (Only in ngen images, but not reachable from MetaData - no sig rewriting)
    case ELEMENT_TYPE_PINNED:       // PINNED <type>, used only in LocalSig (no sig rewriting)
        Debug_ReportError("Unexpected CorElementType in a signature.  Sig parsing failing.");
        IfFailGo(E_FAIL);
        
    default:
        Debug_ReportError("Unknown CorElementType.");
        IfFailGo(E_FAIL);
    }
    _ASSERTE(hr == S_OK);
    return hr;
    
ErrExit:
    Debug_ReportError("Sig parsing failed.");
    return hr;
} // WinMDAdapter::RewriteTypeInSignature

//------------------------------------------------------------------------------



//---------------------------------------------------------------------------------
// Windows.Foundation.Metadata.AttributeTarget and System.AttributeTarget enum
// define different bits for everything (@todo: Be nice to change that before we ship.)
// Do the conversion here.
//---------------------------------------------------------------------------------
static DWORD ConvertToClrAttributeTarget(DWORD winRTTarget)
{
    struct AttributeTargetsPair
    {
        DWORD WinRTValue;
        DWORD ClrValue;
    };
    
    static const AttributeTargetsPair s_attributeTargetPairs[] =
    {
#define DEFINE_PROJECTED_TYPE(a,b,c,d,e,f,g,h,i)
#define DEFINE_PROJECTED_ATTRIBUTETARGETS_VALUE(winrt, clr) { winrt, clr },
#include "WinRTProjectedTypes.h"
#undef DEFINE_PROJECTED_ATTRIBUTETARGETS_VALUES
#undef DEFINE_PROJECTED_TYPE
    };

    if (winRTTarget == 0xffffffff /* Windows.Foundation.Metadata.AttributeTargets.All */)
        return 0x00007fff;  /* System.AttributeTargets.All */
    DWORD clrTarget = 0;
    for (UINT i = 0; i < sizeof(s_attributeTargetPairs)/sizeof(*s_attributeTargetPairs); i++)
    {
        if (winRTTarget & s_attributeTargetPairs[i].WinRTValue)
        {
            clrTarget |= s_attributeTargetPairs[i].ClrValue;
        }
    }
    return clrTarget;
}


//------------------------------------------------------------------------------


// Search a typeDef for WF.AttributeUsageAttribute and WF.AllowMultipleAttribute, and compute
//
//     *pClrTargetValue - the equivalent System.AttributeTargets enum value
//     *pAllowMultiple  - where multiple instances of the CA are allowed.
//
// Returns:
//     S_OK:     a WF.AttributeUsageAttribute CA exists (this is the one that we will rewrite)
//     S_FALSE:  no WF.AttributeUsageAttribute CA exists
//
HRESULT WinMDAdapter::TranslateWinMDAttributeUsageAttribute(mdTypeDef tkTypeDefOfCA, /*[out]*/ DWORD *pClrTargetValue, /*[out]*/ BOOL *pAllowMultiple)
{
    HRESULT hr;
    _ASSERTE(TypeFromToken(tkTypeDefOfCA) == mdtTypeDef);
    _ASSERTE(pClrTargetValue != NULL);
    _ASSERTE(pAllowMultiple != NULL);

    const BYTE *pbWFUsageBlob;
    ULONG       cbWFUsageBlob;
    IfFailGo(m_pRawMetaModelCommonRO->CommonGetCustomAttributeByName(tkTypeDefOfCA, "Windows.Foundation.Metadata.AttributeUsageAttribute", (const void **)&pbWFUsageBlob, &cbWFUsageBlob));
    if (hr == S_FALSE)
        goto ErrExit;
    // Expected blob format:
    //    01 00        - Fixed prolog for CA's
    //    xx xx xx xx  - The Windows.Foundation.Metadata.AttributeTarget value
    //    00 00        - Indicates 0 name/value pairs following.
    if (cbWFUsageBlob != 2 + sizeof(DWORD) + 2)  
    {
        IfFailGo(COR_E_BADIMAGEFORMAT);
    }
    DWORD wfTargetValue = *(DWORD*)(pbWFUsageBlob + 2);
    *pClrTargetValue = ConvertToClrAttributeTarget(wfTargetValue);

    // add AttributeTargets.Method, AttributeTargets.Constructor , AttributeTargets.Property, and AttributeTargets.Event if this is the VersionAttribute
    LPCSTR  szNamespace;
    LPCSTR  szName;
    IfFailGo(m_pRawMetaModelCommonRO->CommonGetTypeDefProps(tkTypeDefOfCA, &szNamespace, &szName, NULL, NULL, NULL));

    if ((strcmp(szName, "VersionAttribute") == 0 || strcmp(szName, "DeprecatedAttribute") == 0) && strcmp(szNamespace, "Windows.Foundation.Metadata") == 0)
    {
        *pClrTargetValue |= 0x2E0;
    }

    const BYTE *pbWFAllowMultipleBlob;
    ULONG       cbWFAllowMultipleBlob;
    IfFailGo(m_pRawMetaModelCommonRO->CommonGetCustomAttributeByName(tkTypeDefOfCA, "Windows.Foundation.Metadata.AllowMultipleAttribute", (const void **)&pbWFAllowMultipleBlob, &cbWFAllowMultipleBlob));
    *pAllowMultiple = (hr == S_OK);
    hr = S_OK;

  ErrExit:
    return hr;
}

//------------------------------------------------------------------------------

/*static*/ HRESULT WinMDAdapter::CreateClrAttributeUsageAttributeCABlob(DWORD clrTargetValue, BOOL allowMultiple, CABlob **ppCABlob)
{
    // Emit the blob format corresponding to:
    //    [System.AttributeUsage(System.AttributeTargets.xx, AllowMultiple=yy)]
    //
    //    01 00         - Fixed prolog for CA's
    //    xx xx xx xx   - The System.AttributeTarget value
    //    01 00         - Indicates 1 name/value pair following.
    //    54            - SERIALIZATION_TYPE_PROPERTY
    //    02            - ELEMENT_TYPE_BOOLEAN
    //    0d            - strlen("AllowMultiple") - 1
    //    41 6c ... 65  - "A" "l" "l" "o" "w" "M" "u" "l" "t" "i" "p" "l" "e"
    //    yy            - The boolean selection for "AllowMultiple"

    BYTE blob[] = { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x54, 0x02, 0x0D, 0x41, 0x6C, 0x6C, 0x6F, 0x77, 0x4D, 0x75, 0x6C, 0x74, 0x69, 0x70, 0x6C, 0x65, 0x00 };
    blob[sizeof(blob) - 1] = allowMultiple ? 1 : 0;
    *((DWORD*)(blob+2)) = clrTargetValue;
    CABlob *pCABlob;
    IfNullRet(pCABlob = CABlob::Create(blob, sizeof(blob)));
    *ppCABlob = pCABlob;
    return S_OK;
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
        UINT nIndex;
        IfFailGo(CheckIfMethodImplImplementsARedirectedInterface(tkDecl, &nIndex));
        if (hr == S_FALSE || 
            (SUCCEEDED(hr) && nIndex == WinMDAdapter::RedirectedTypeIndex_Windows_Foundation_IClosable))
        {
            // Keep MethodImpl for IClosable methods and non-redirected interfaces
            IfFailGo(HENUMInternal::AddElementToEnum(henum, tkBody));
            IfFailGo(HENUMInternal::AddElementToEnum(henum, tkDecl));
        }
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


//------------------------------------------------------------------------------

//-------------------------------------------------------------------------------------
// Returns: S_OK if tkDecl of a methodImpl is a reference to a method on a redirected interface.
//-------------------------------------------------------------------------------------
HRESULT WinMDAdapter::CheckIfMethodImplImplementsARedirectedInterface(mdToken tkDecl, UINT *pIndex)
{

    HRESULT hr;
    if (TypeFromToken(tkDecl) != mdtMemberRef)
        return S_FALSE;   // REX will always use memberRef and typeRefs to refer to redirected interfaces, even if in same module.

    mdToken tkParent;
    IfFailRet(m_pRawMetaModelCommonRO->CommonGetMemberRefProps(tkDecl, &tkParent));

    mdTypeRef tkTypeRef;
    if (TypeFromToken(tkParent) == mdtTypeRef)
    {
        tkTypeRef = tkParent;
    }
    else if (TypeFromToken(tkParent) == mdtTypeSpec)
    {
        PCCOR_SIGNATURE pvSig;
        ULONG cbSig;
        IfFailRet(m_pRawMetaModelCommonRO->CommonGetTypeSpecProps(tkParent, &pvSig, &cbSig));
        static const BYTE expectedSigStart[] = {ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS};
        if (cbSig < sizeof(expectedSigStart) + 1)
            return S_FALSE;
        if (0 != memcmp(pvSig, expectedSigStart, sizeof(expectedSigStart)))
            return S_FALSE;
        const BYTE *pCodedToken = pvSig + sizeof(expectedSigStart);
        if (cbSig < sizeof(expectedSigStart) + CorSigUncompressedDataSize(pCodedToken))
            return S_FALSE;
        mdToken genericType = CorSigUncompressToken(/*modifies*/pCodedToken);
        if (TypeFromToken(genericType) != mdtTypeRef)
            return S_FALSE;
        tkTypeRef = genericType;
    }
    else
    {
        return S_FALSE;
    }

    ULONG treatment;
    IfFailRet(GetTypeRefTreatment(tkTypeRef, &treatment));
    if ((treatment & kTrClassMask) != kTrClassWellKnownRedirected)
        return S_FALSE;

    if (pIndex)
        *pIndex = (treatment & ~kTrClassMask);

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

// Sentinel value in m_redirectedCABlobsMemoTable table. Means "do no blob rewriting. Return the one from the underlying importer."
/*static*/ WinMDAdapter::CABlob * const WinMDAdapter::CABlob::NOREDIRECT = ((WinMDAdapter::CABlob *)(0x1));

//-----------------------------------------------------------------------------------------------------

/*static*/ WinMDAdapter::CABlob* WinMDAdapter::CABlob::Create(const BYTE *pBlob, ULONG cbBlob)
{
    // This can cause allocations (thus entering the host) during a profiler stackwalk.
    // But we're ok since we're not supporting SQL/F1 profiling with WinMDs. FUTURE:
    // Would be nice to eliminate allocations on stack walks regardless.
    PERMANENT_CONTRACT_VIOLATION(HostViolation, ReasonUnsupportedForSQLF1Profiling);

    size_t cbAlloc = sizeof(CABlob) + cbBlob;  // This overestimates the needed size a bit - no biggie
    if (cbAlloc < sizeof(CABlob))
        return NULL;

    CABlob *pNewCABlob = (CABlob*)(new (nothrow) BYTE[cbAlloc]);
    if (!pNewCABlob)
        return NULL;

    pNewCABlob->cbBlob = cbBlob;
    memcpy(pNewCABlob->data, pBlob, cbBlob);
    return pNewCABlob;
}

//-----------------------------------------------------------------------------------------------------

/*static*/ void WinMDAdapter::CABlob::Destroy(WinMDAdapter::CABlob *pCABlob)
{
    if (pCABlob != CABlob::NOREDIRECT)
        delete [] (BYTE*)pCABlob;
}

//-----------------------------------------------------------------------------------------------------

// Sentinel value in m_redirectedMethodSigMemoTable or m_redirectedFieldMemoTable. 
// Means "do no signature rewriting. Return the one from the underlying importer."
/*static*/ WinMDAdapter::SigData * const WinMDAdapter::SigData::NOREDIRECT = ((WinMDAdapter::SigData *)(0x1));

//-----------------------------------------------------------------------------------------------------

/*static*/ WinMDAdapter::SigData* WinMDAdapter::SigData::Create(ULONG cbSig, PCCOR_SIGNATURE pSig)
{
    // This can cause allocations (thus entering the host) during a profiler stackwalk.
    // But we're ok since we're not supporting SQL/F1 profiling with WinMDs. FUTURE:
    // Would be nice to eliminate allocations on stack walks regardless.
    PERMANENT_CONTRACT_VIOLATION(HostViolation, ReasonUnsupportedForSQLF1Profiling);

    _ASSERTE(pSig != NULL);
    size_t cbAlloc = sizeof(SigData) + cbSig;  // This overestimates the needed size a bit - no biggie
    if (cbAlloc < sizeof(SigData))
        return NULL;

    SigData *pNewSigData = (SigData*)(new (nothrow) BYTE[cbAlloc]);
    if (!pNewSigData)
        return NULL;

    pNewSigData->cbSig = cbSig;
    memcpy(pNewSigData->data, pSig, cbSig);
    return pNewSigData;
}

//-----------------------------------------------------------------------------------------------------

/*static*/ void WinMDAdapter::SigData::Destroy(WinMDAdapter::SigData *pSigData)
{
    if (pSigData != SigData::NOREDIRECT)
        delete [] (BYTE*)pSigData;
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



