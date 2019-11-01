// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#ifndef __MDWinMDAdapter__h__
#define __MDWinMDAdapter__h__

#include "memotable.h"
#include "../../inc/winmdinterfaces.h"
#include "thekey.h"
#include "ecmakey.h"

// Instantiation of template in holder.h
template void DoNothing<ULONG>(ULONG);

class SigBuilder;
class SigParser;

typedef const BYTE  * PCBYTE;
static const BYTE s_pbContractPublicKeyToken[] =  {0xB0,0x3F,0x5F,0x7F,0x11,0xD5,0x0A,0x3A};
static const BYTE s_pbContractPublicKey[] = {0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00, 0x06, 0x02, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x52, 0x53, 0x41, 0x31, 0x00, 0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x07, 0xD1, 0xFA, 0x57, 0xC4, 0xAE, 0xD9, 0xF0, 0xA3, 0x2E, 0x84, 0xAA, 0x0F, 0xAE, 0xFD, 0x0D, 0xE9, 0xE8, 0xFD, 0x6A, 0xEC, 0x8F, 0x87, 0xFB, 0x03, 0x76, 0x6C, 0x83, 0x4C, 0x99, 0x92, 0x1E, 0xB2, 0x3B, 0xE7, 0x9A, 0xD9, 0xD5, 0xDC, 0xC1, 0xDD, 0x9A, 0xD2, 0x36, 0x13, 0x21, 0x02, 0x90, 0x0B, 0x72, 0x3C, 0xF9, 0x80, 0x95, 0x7F, 0xC4, 0xE1, 0x77, 0x10, 0x8F, 0xC6, 0x07, 0x77, 0x4F, 0x29, 0xE8, 0x32, 0x0E, 0x92, 0xEA, 0x05, 0xEC, 0xE4, 0xE8, 0x21, 0xC0, 0xA5, 0xEF, 0xE8, 0xF1, 0x64, 0x5C, 0x4C, 0x0C, 0x93, 0xC1, 0xAB, 0x99, 0x28, 0x5D, 0x62, 0x2C, 0xAA, 0x65, 0x2C, 0x1D, 0xFA, 0xD6, 0x3D, 0x74, 0x5D, 0x6F, 0x2D, 0xE5, 0xF1, 0x7E, 0x5E, 0xAF, 0x0F, 0xC4, 0x96, 0x3D, 0x26, 0x1C, 0x8A, 0x12, 0x43, 0x65, 0x18, 0x20, 0x6D, 0xC0, 0x93, 0x34, 0x4D, 0x5A, 0xD2, 0x93};

class DECLSPEC_UUID("996AA908-5606-476d-9985-48607B2DA076") IWinMDImport : public IUnknown
{
public :
    STDMETHOD(IsScenarioWinMDExp)(BOOL *pbResult) = 0;
    STDMETHOD(IsRuntimeClassImplementation)(mdTypeDef tkTypeDef, BOOL *pbResult) = 0;
};

//========================================================================================
// Only IWinMDImport and IWinMDImportInternalRO QI successfully for this guid - for cases where we need to
// tell the difference between the classic MD importers and the WinMD wrappers.
//========================================================================================
// {996AA908-5606-476d-9985-48607B2DA076}
extern const IID DECLSPEC_SELECTANY IID_IWinMDImport = __uuidof(IWinMDImport);

//========================================================================================
// Popup an assert box if COMPLUS_MD_WinMD_AssertOnIllegalUsage=1
//========================================================================================
#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
#define WINMD_COMPAT_ASSERT(assertMsg) if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MD_WinMD_AssertOnIllegalUsage)) DbgAssertDialog(__FILE__, __LINE__, assertMsg)
#else
#define WINMD_COMPAT_ASSERT(assertMsg)
#endif


//========================================================================================
// class WinMDAdapter
//
//   This object performs typeref redirection and any other chores involved in
//   masquerading WinMD files as .NET assemblies.
//
//   WinMDAdapters act as internal helper objects to WinMDImport and WinMDInternalImportRO
//   and factor out as much common code as is practical given that these two importers
//   expose wildly different interfaces.
//
//   The main input to this object is a standard .NET metadata importer (the "raw" importer.) Since
//   the two importers have no common public interface, we use the internal (non-COM) IMetaModelCommon
//   interface that both imports have internally and expose through the private IMDCommon
//   interface.
//
//   Methods on this class follow the IMDInternalImport philosophy (i.e. validation? what validation?,
//   return strings as direct UTF8 pointers to internal strings allocated for the lifetime
//   of WinMDAdapter.) When used by the public IMetaDataImport adapter, it's important that the caller
//   validate parameters before invoking WinMDAdapter.
//========================================================================================
class WinMDAdapter
{
public:
#define DEFINE_PROJECTED_TYPE(szWinRTNS, szWinRTName, szClrNS, szClrName, ClrAsmIdx, nContractAsmIdx, WinRTIndex, ClrIndex, TypeKind) \
    RedirectedTypeIndex_ ## WinRTIndex,                                    \
    RedirectedTypeIndex_ ## ClrIndex = RedirectedTypeIndex_ ## WinRTIndex, \

    // Indexes of well-known redirected types into array code:g_rgRedirectedTypes
    enum RedirectedTypeIndex
    {
#include "WinRTProjectedTypes.h"
        RedirectedTypeIndex_Count,
        RedirectedTypeIndex_Invalid = -1,
    };
#undef DEFINE_PROJECTED_TYPE

    enum FrameworkAssemblyIndex
    {
        FrameworkAssembly_Mscorlib,
        FrameworkAssembly_SystemObjectModel,
        FrameworkAssembly_SystemRuntime,
        FrameworkAssembly_SystemRuntimeWindowsRuntime,
        FrameworkAssembly_SystemRuntimeWindowsRuntimeUIXaml,
        FrameworkAssembly_SystemNumericsVectors,

        FrameworkAssembly_Count,
    };

    // If new contract assemblies need to be added, they must be appended to the end of the following list.
    // Also, don't remove or change any existing assemblies in this list.
    // Not following these rules will break existing MDIL images generated in the Store.
    enum ContractAssemblyIndex
    {
        ContractAssembly_SystemRuntime,
        ContractAssembly_SystemRuntimeInteropServicesWindowsRuntime,
        ContractAssembly_SystemObjectModel,
        ContractAssembly_SystemRuntimeWindowsRuntime,
        ContractAssembly_SystemRuntimeWindowsRuntimeUIXaml,
        ContractAssembly_SystemNumericsVectors, // GetExtraAssemblyRefCount assumes SystemNumericsVectors is the last assembly.
                                                // If you add an assembly you must update GetActualExtraAssemblyRefCount.

        ContractAssembly_Count,
    };


    enum WinMDTypeKind
    {
        WinMDTypeKind_Attribute,
        WinMDTypeKind_Enum,
        WinMDTypeKind_Delegate,
        WinMDTypeKind_Interface,
        WinMDTypeKind_PDelegate,
        WinMDTypeKind_PInterface,
        WinMDTypeKind_Struct,
        WinMDTypeKind_Runtimeclass,
    };

    int GetExtraAssemblyRefCount();

    // Factory and destructor
    static HRESULT Create(IMDCommon *pRawMDCommon, /*[out]*/ WinMDAdapter **ppAdapter);
    ~WinMDAdapter();

    // Map a well-known WinRT typename to CLR typename
    static BOOL ConvertWellKnownTypeNameFromWinRTToClr(LPCSTR *pszNamespace, LPCSTR *pszName);

    // Map a well-known WinRT full typename to CLR full typename
    static BOOL ConvertWellKnownFullTypeNameFromWinRTToClr(LPCWSTR *pszFullName, RedirectedTypeIndex *pIndex);
    
    // Map a well-known CLR typename to WinRT typename
    static BOOL ConvertWellKnownTypeNameFromClrToWinRT(LPCSTR *pszFullName);

    // Map a well-known CLR typename to WinRT typename
    static BOOL ConvertWellKnownTypeNameFromClrToWinRT(LPCSTR *pszNamespace, LPCSTR *pszName);
        
    // Returns names of redirected type 'index'.
    static void GetRedirectedTypeInfo(
        RedirectedTypeIndex index, 
        LPCSTR *            pszClrNamespace, 
        LPCSTR *            pszClrName, 
        LPCSTR *            pszFullWinRTName,
        FrameworkAssemblyIndex * pFrameworkAssemblyIdx,
        ContractAssemblyIndex * pContractAssemblyIdx,
        WinMDTypeKind *     pWinMDTypeKind);

    // Returns name of redirected type 'index'.
    static LPCWSTR GetRedirectedTypeFullWinRTName(RedirectedTypeIndex index);
    static LPCSTR GetRedirectedTypeFullCLRName(RedirectedTypeIndex index);

    // Returns renamed typedefs
    HRESULT GetTypeDefProps(
        mdTypeDef    typeDef,               // [IN] given typedef
        LPCUTF8     *pszNameSpace,          // [OUT] return typedef namespace
        LPCUTF8     *pszName,               // [OUT] return typedef name
        DWORD       *pdwFlags,              // [OUT] return typedef flags
        mdToken     *ptkExtends             // [OUT] Put base class TypeDef/TypeRef here.
    );
    
    // Find TypeDef by name
    HRESULT FindTypeDef(
        LPCSTR      szTypeDefNamespace, // [IN] Namespace for the TypeDef.
        LPCSTR      szTypeDefName,      // [IN] Name of the TypeDef.
        mdToken     tkEnclosingClass,   // [IN] TypeDef/TypeRef of enclosing class.
        mdTypeDef * ptkTypeDef          // [OUT] return typedef
    );

    // Returns redirected typerefs
    HRESULT GetTypeRefProps(
        mdTypeRef   typeref,               // [IN] given typeref
        LPCSTR      *psznamespace,         // [OUT] return typeref namespace
        LPCSTR      *pszname,              // [OUT] return typeref name
        mdToken     *ptkResolutionScope    // [OUT] return typeref resolutionscope
    );

    // Find TypeRef by name
    HRESULT FindTypeRef(
        LPCSTR      szNamespace,           // [IN] Namespace for the TypeRef (NULL for standalone names)
        LPCSTR      szName,                // [IN] Name of the TypeRef.
        mdToken     tkResolutionScope,     // [IN] Resolution Scope fo the TypeRef.
        mdTypeRef  *ptk                    // [OUT] TypeRef token returned.
    );

    // Modify an ExportedType name
    HRESULT ModifyExportedTypeName(
        mdExportedType tkExportedType,     // [IN] exportedType token
        LPCSTR     *pszNamespace,          // [IN,OUT,OPTIONAL] namespace to modify
        LPCSTR     *pszName                // [IN,OUT,OPTIONAL] name to modify
    );

    // Find ExportedType by name
    HRESULT FindExportedType(
        LPCUTF8     szNamespace,           // [IN] expected namespace
        LPCUTF8     szName,                // [IN] expected name
        mdToken     tkEnclosingType,       // [IN] expected tkEnclosingType
        mdExportedType   *ptkExportedType  // [OUT] ExportedType token returned.
    );

    // Returns rewritten metadata version string 
    HRESULT GetVersionString(
        LPCSTR       *pszVersion           // [OUT] return metadata version string
    )
    {
        _ASSERTE(pszVersion != NULL);
        *pszVersion = m_pRedirectedVersionString;
        return S_OK;
    }

    void ModifyAssemblyRefProps(
        mdAssemblyRef mdar,
        const void **ppbPublicKeyOrToken,
        ULONG *pcbPublicKeyOrToken,
        LPCSTR *pszName,
        USHORT *pusMajorVersion,
        USHORT *pusMinorVersion,
        USHORT *pusBuildNumber,
        USHORT *pusRevisionNumber,
        const void **ppbHashValue,
        ULONG *pcbHashValue)
    {
        _ASSERTE(TypeFromToken(mdar) == mdtAssemblyRef);

        // The version of the mscorlib should be 4.0.0.0
        if (m_assemblyRefMscorlib == mdar)
        {
            if (pusMajorVersion != nullptr)
                *pusMajorVersion = VER_ASSEMBLYMAJORVERSION;
            if (pusMinorVersion != nullptr)
                *pusMinorVersion = VER_ASSEMBLYMINORVERSION;
            if (pusBuildNumber != nullptr)
                *pusBuildNumber = VER_ASSEMBLYBUILD;
            if (pusRevisionNumber != nullptr)
                *pusRevisionNumber = VER_ASSEMBLYBUILD_QFE;

            // Under CoreCLR, we replace the ECMA key in the mscorlib assembly ref with the CoreCLR platform public key token
            if (ppbPublicKeyOrToken != nullptr)
            {
                *ppbPublicKeyOrToken = g_rbTheSilverlightPlatformKeyToken;
                *pcbPublicKeyOrToken = _countof(g_rbTheSilverlightPlatformKeyToken);
            }
        }
        else if (RidFromToken(mdar) > m_rawAssemblyRefCount)
        {
            // This is one of the assemblies that we inject
            UINT index = RidFromToken(mdar) - m_rawAssemblyRefCount - 1;

            if (ppbPublicKeyOrToken != nullptr)
            {
                if (index != ContractAssemblyIndex::ContractAssembly_SystemRuntimeWindowsRuntime &&
                    index != ContractAssemblyIndex::ContractAssembly_SystemRuntimeWindowsRuntimeUIXaml)
                {
                    // The assembly ref is a contract/facade assembly. System.Runtime.WindowsRuntime and 
                    // System.Runtime.WindowsRuntime.UI.Xaml are special cased because the contract and the implementation 
                    // assembly share the same identity and use mscorlib's public key/token that ppbPublicKeyOrToken 
                    // alredy contains since the raw GetAssemblyRefProps was called with mscorlib's token before this 
                    // function was called. 
                    if (*pcbPublicKeyOrToken == sizeof(s_pbContractPublicKeyToken))
                        *ppbPublicKeyOrToken = s_pbContractPublicKeyToken;
                    else if (*pcbPublicKeyOrToken == sizeof(s_pbContractPublicKey))
                        *ppbPublicKeyOrToken = s_pbContractPublicKey;
                }
                else 
                {
                    // System.Runtime.WindowsRuntime uses the ECMA key.
                    // The WinRT adapter's policy of using mscorlib's assembly references for all the additional
                    // assembly references doesn't work here since mscorlib uses the Silverlight Platform key.
                    if (*pcbPublicKeyOrToken == sizeof(g_rbNeutralPublicKeyToken))
                        *ppbPublicKeyOrToken = g_rbNeutralPublicKeyToken;
                    else if (*pcbPublicKeyOrToken == sizeof(g_rbNeutralPublicKey))
                        *ppbPublicKeyOrToken = g_rbNeutralPublicKey;
                }
            }

            if (pszName != nullptr)
                *pszName = GetExtraAssemblyRefName(mdar);

            if (pusMajorVersion != nullptr)
                *pusMajorVersion = VER_ASSEMBLYMAJORVERSION;
            if (pusMinorVersion != nullptr)
                *pusMinorVersion = VER_ASSEMBLYMINORVERSION;
            if (pusBuildNumber != nullptr)
                *pusBuildNumber = VER_ASSEMBLYBUILD;
            if (pusRevisionNumber != nullptr)
                *pusRevisionNumber = VER_ASSEMBLYBUILD_QFE;

            if (ppbHashValue)
                *ppbHashValue = NULL;

            if (pcbHashValue)
                *pcbHashValue = 0;
        }
    }

    // Modifes the FieldDefProps.
    HRESULT ModifyFieldDefProps (mdFieldDef tkFielddDef, DWORD *pdwFlags);

    // Modifies FieldProps
    HRESULT ModifyFieldProps (mdToken tkField, mdToken tkParent, LPCSTR szFieldName, DWORD *pdwFlags);

    // Modifies methodDef flags and RVA
    HRESULT ModifyMethodProps(mdMethodDef tkMethodDef, /*[in, out]*/ DWORD *pdwAttr, /* [in,out] */ DWORD *pdwImplFlags, /* [in,out] */ ULONG *pulRVA, LPCSTR *pszName);

    // Modifies member flags and RVA
    HRESULT ModifyMemberProps(mdToken tkMember, /*[in, out]*/ DWORD *pdwAttr, /* [in,out] */ DWORD *pdwImplFlags, /* [in,out] */ ULONG *pulRVA, LPCSTR *pszNewName);

    // Modifies CA's
    HRESULT GetCustomAttributeByName( // S_OK or error.
        mdToken            tkObj,      // [IN] Object with Custom Attribute.
        LPCUTF8            szName,     // [IN] Name of desired Custom Attribute.
        mdCustomAttribute *ptkCA,      // [OUT] Put custom attribute token here
        const void       **ppData,     // [OUT] Put pointer to data here.
        ULONG             *pcbData);   // [OUT] Put size of data here.

    // Modify CA blobs
    HRESULT GetCustomAttributeBlob(
        mdCustomAttribute tkCA,
        const void  **ppData,         // [OUT] Put pointer to data here.
        ULONG       *pcbData);        // [OUT] Put size of data here.

    // Gets the GUID used for COM interop purposes.
    HRESULT GetItemGuid(mdToken tkObj, CLSID *pGuid);

    // Gets filtered methodImpl list
    HRESULT AddMethodImplsToEnum(mdTypeDef tkTypeDef, HENUMInternal *henum);

    //-----------------------------------------------------------------------------------
    // For each token, we cache the signature that the adapter reports to callers.
    //-----------------------------------------------------------------------------------
    struct SigData
    {
        ULONG cbSig;           // Length of sig in bytes
        BYTE  data[1];         // Signature

        static SigData* Create(ULONG cbSig, PCCOR_SIGNATURE pSig);
        static void Destroy(SigData *pSigData);

        // Sentinel value meaning we did not need to rewrite the signature; use the underlying importer's signature
        static SigData* const NOREDIRECT;
    };

    // Gets a method/field/TypeSpec/MethodSpec signature, with appropriate WinMD changes.
    HRESULT ReinterpretMethodSignature     (ULONG cbOrigSigBlob, PCCOR_SIGNATURE pOrigSig, SigData **ppSigData);
    HRESULT ReinterpretFieldSignature      (ULONG cbOrigSigBlob, PCCOR_SIGNATURE pOrigSig, SigData **ppSigData);
    HRESULT ReinterpretTypeSpecSignature   (ULONG cbOrigSigBlob, PCCOR_SIGNATURE pOrigSig, SigData **ppSigData);
    HRESULT ReinterpretMethodSpecSignature (ULONG cbOrigSigBlob, PCCOR_SIGNATURE pOrigSig, SigData **ppSigData);
    
    template<mdToken TOKENTYPE>
    HRESULT ReinterpretSignature(
        ULONG            cbOrigSigBlob,     // [IN] count of bytes in original signature blob
        PCCOR_SIGNATURE  pOrigSig,          // [IN] original signature
        SigData        **ppSigData          // [OUT] new signature or SigData::NOREDIRECT
    )
    {
        UNREACHABLE_MSG("You should create a specialized version of ReinterpretSignature for this token type");
    }

    // Explicit specializations of ReinterpretSignature for all supported token types
    template<> // mdMethodDef
    HRESULT ReinterpretSignature<mdtMethodDef>(ULONG cbOrigSigBlob, PCCOR_SIGNATURE pOrigSig, SigData **ppSigData)
    {
        return ReinterpretMethodSignature(cbOrigSigBlob, pOrigSig, ppSigData);
    }

    template<> // mdFieldDef
    HRESULT ReinterpretSignature<mdtFieldDef>(ULONG cbOrigSigBlob, PCCOR_SIGNATURE pOrigSig, SigData **ppSigData)
    {
        return ReinterpretFieldSignature(cbOrigSigBlob, pOrigSig, ppSigData);
    }

    template<> // mdMemberRef
    HRESULT ReinterpretSignature<mdtMemberRef>(ULONG cbOrigSigBlob, PCCOR_SIGNATURE pOrigSig, SigData **ppSigData)
    {
        if (cbOrigSigBlob == 0)
        {
            *ppSigData = SigData::NOREDIRECT;

            return META_E_BAD_SIGNATURE;
        }

        // MemberRef references either a field or a method
        return (*pOrigSig == IMAGE_CEE_CS_CALLCONV_FIELD) ?
            ReinterpretFieldSignature(cbOrigSigBlob, pOrigSig, ppSigData) :
            ReinterpretMethodSignature(cbOrigSigBlob, pOrigSig, ppSigData);
    }

    template<> // mdProperty
    HRESULT ReinterpretSignature<mdtProperty>(ULONG cbOrigSigBlob, PCCOR_SIGNATURE pOrigSig, SigData **ppSigData)
    {
        // Per ECMA CLI spec, section 23.2.5 PropertySig is just an ordinary method (getter) signature
        return ReinterpretMethodSignature(cbOrigSigBlob, pOrigSig, ppSigData);
    }

    template<> // mdTypeSpec
    HRESULT ReinterpretSignature<mdtTypeSpec>(ULONG cbOrigSigBlob, PCCOR_SIGNATURE pOrigSig, SigData **ppSigData)
    {
        return ReinterpretTypeSpecSignature(cbOrigSigBlob, pOrigSig, ppSigData);
    }

    template<> // mdMethodSpec
    HRESULT ReinterpretSignature<mdtMethodSpec>(ULONG cbOrigSigBlob, PCCOR_SIGNATURE pOrigSig, SigData **ppSigData)
    {
        return ReinterpretMethodSpecSignature(cbOrigSigBlob, pOrigSig, ppSigData);
    }

    // Note: This method will look in a cache for the reinterpreted signature, but does not add any values to 
    // the cache or do any work on failure.  If we can't find it then it returns S_FALSE.
    static HRESULT GetCachedSigForToken(
        mdToken          token,             // [IN] given token
        MemoTable<SigData*, SigData::Destroy> &memoTable, // [IN] the MemoTable to use
        ULONG           *pcbSigBlob,        // [OUT] count of bytes in the signature blob
        PCCOR_SIGNATURE *ppSig,             // [OUT] new signature
        BOOL            *pfPassThrough      // [OUT] did the cache say we don't need to reinterpret this sig?
    );

    static HRESULT InsertCachedSigForToken(
        mdToken          token,             // [IN] given token
        MemoTable<SigData*, SigData::Destroy> &memoTable, // [IN] the MemoTable to use
        SigData        **ppSigData          // [IN, OUT] new signature or SigData::NOREDIRECT if the signature didn't need to be reparsed,
    );                                      // will be updated with another (but identical) SigData* if this thread lost the race

    template<typename T, mdToken TOKENTYPE>
    HRESULT GetOriginalSigForToken(
        T                *pImport,
        mdToken           token,            // [IN] Token.
        PCCOR_SIGNATURE  *ppvSig,           // [OUT] return pointer to signature.
        ULONG            *pcbSig            // [OUT] return size of signature.
        )
    {
        UNREACHABLE_MSG("You should create a specialized version of GetOriginalSigForToken for this interface/token type");
    }

    template<mdToken TOKENTYPE>
    MemoTable<SigData*, SigData::Destroy> &GetSignatureMemoTable()
    {
        UNREACHABLE_MSG("You should create a specialized version of GetSignatureMemoTable for this token type");
    }

    // Explicit specializations of GetOriginalSigForToken for all supported token types (IMetaDataImport2)
    template<> // mdMethodDef
    HRESULT GetOriginalSigForToken<IMetaDataImport2, mdtMethodDef>(IMetaDataImport2 *pImport, mdMethodDef tk, PCCOR_SIGNATURE *ppvSig, ULONG *pcbSig)
    {
        return pImport->GetMethodProps(tk, NULL, NULL, 0, NULL, NULL, ppvSig, pcbSig, NULL, NULL);
    }

    template<> // mdFieldDef
    HRESULT GetOriginalSigForToken<IMetaDataImport2, mdtFieldDef>(IMetaDataImport2 *pImport, mdFieldDef tk, PCCOR_SIGNATURE *ppvSig, ULONG *pcbSig)
    {
        return pImport->GetFieldProps(tk, NULL, NULL, 0, NULL, NULL, ppvSig, pcbSig, NULL, NULL, NULL);
    }

    template<> // mdMemberRef
    HRESULT GetOriginalSigForToken<IMetaDataImport2, mdtMemberRef>(IMetaDataImport2 *pImport, mdMemberRef tk, PCCOR_SIGNATURE *ppvSig, ULONG *pcbSig)
    {
        return pImport->GetMemberRefProps(tk, NULL, NULL, 0, NULL, ppvSig, pcbSig);
    }

    template<> // mdProperty
    HRESULT GetOriginalSigForToken<IMetaDataImport2, mdtProperty>(IMetaDataImport2 *pImport, mdProperty tk, PCCOR_SIGNATURE *ppvSig, ULONG *pcbSig)
    {
        return pImport->GetPropertyProps(tk, NULL, NULL, 0, NULL, NULL, ppvSig, pcbSig, NULL, NULL, NULL, NULL, NULL, NULL, 0, NULL);
    }

    template<> // mdTypeSpec
    HRESULT GetOriginalSigForToken<IMetaDataImport2, mdtTypeSpec>(IMetaDataImport2 *pImport, mdTypeSpec tk, PCCOR_SIGNATURE *ppvSig, ULONG *pcbSig)
    {
        return pImport->GetTypeSpecFromToken(tk, ppvSig, pcbSig);
    }

    template<> // mdMethodSpec
    HRESULT GetOriginalSigForToken<IMetaDataImport2, mdtMethodSpec>(IMetaDataImport2 *pImport, mdMethodSpec tk, PCCOR_SIGNATURE *ppvSig, ULONG *pcbSig)
    {
        return pImport->GetMethodSpecProps(tk, NULL, ppvSig, pcbSig);
    }
 
    // Explicit specializations of GetOriginalSigForToken for all supported token types (IMDInternalImport)
    template<> // mdMethodDef
    HRESULT GetOriginalSigForToken<IMDInternalImport, mdtMethodDef>(IMDInternalImport *pImport, mdMethodDef tk, PCCOR_SIGNATURE *ppvSig, ULONG *pcbSig)
    {
        return pImport->GetSigOfMethodDef(tk, pcbSig, ppvSig);
    }

    template<> // mdFieldDef
    HRESULT GetOriginalSigForToken<IMDInternalImport, mdtFieldDef>(IMDInternalImport *pImport, mdFieldDef tk, PCCOR_SIGNATURE *ppvSig, ULONG *pcbSig)
    {
        return pImport->GetSigOfFieldDef(tk, pcbSig, ppvSig);
    }

    template<> // mdMemberRef
    HRESULT GetOriginalSigForToken<IMDInternalImport, mdtMemberRef>(IMDInternalImport *pImport, mdMemberRef tk, PCCOR_SIGNATURE *ppvSig, ULONG *pcbSig)
    {
        LPCSTR szMemberRefName;
        return pImport->GetNameAndSigOfMemberRef(tk, ppvSig, pcbSig, &szMemberRefName);
    }

    template<> // mdProperty
    HRESULT GetOriginalSigForToken<IMDInternalImport, mdtProperty>(IMDInternalImport *pImport, mdProperty tk, PCCOR_SIGNATURE *ppvSig, ULONG *pcbSig)
    {
        return pImport->GetPropertyProps(tk, NULL, NULL, ppvSig, pcbSig);
    }

    template<> // mdTypeSpec
    HRESULT GetOriginalSigForToken<IMDInternalImport, mdtTypeSpec>(IMDInternalImport *pImport, mdTypeSpec tk, PCCOR_SIGNATURE *ppvSig, ULONG *pcbSig)
    {
        return pImport->GetSigFromToken(tk, pcbSig, ppvSig);
    }

    template<> // mdMethodSpec
    HRESULT GetOriginalSigForToken<IMDInternalImport, mdtMethodSpec>(IMDInternalImport *pImport, mdMethodSpec tk, PCCOR_SIGNATURE *ppvSig, ULONG *pcbSig)
    {
        return pImport->GetMethodSpecProps(tk, NULL, ppvSig, pcbSig);
    }


    // Returns signature for the given MethodDef, FieldDef, MemberRef, standalone Signature, Property, TypeSpec, or MethodSpec token.
    // Performs cache lookup, computes the new reinterpreted signature if needed, and inserts it into the cache. Be sure to instantiate
    // the method with right template arguments. T is the underlying metadata interface that should be used to retrieve the original
    // signature if not passed in ppOrigSig/pcbOrigSigBlob. TOKENTYPE an mdt* constants corresponding to the type of the token.
    template<typename T, mdToken TOKENTYPE>
    HRESULT GetSignatureForToken(
      mdToken          token,
      PCCOR_SIGNATURE *ppOrigSig,        // [IN] pointer to count of bytes in the original signature blob, NULL if we need to retrieve from GetOriginalSigForToken
      ULONG           *pcbOrigSigBlob,   // [IN] pointer to original signature, NULL if we need to retrieve from GetOriginalSigForToken
      PCCOR_SIGNATURE *ppSig,            // [OUT] new signature
      ULONG           *pcbSigBlob,       // [OUT] count of bytes in the signature blob
      T               *pImport)
    {
        _ASSERTE(TypeFromToken(token) == TOKENTYPE);
        if ((ppSig == NULL) && (pcbSigBlob == NULL))
        {
            return S_OK;
        }
        
        // When loading NGen images we go through code paths that expect no faults and no
        // throws.  We will need to take a look at how we use the winmd metadata with ngen,
        // potentially storing the post-mangled metadata in the NI because as the adapter grows
        // we'll see more of these.
        CONTRACT_VIOLATION(ThrowsViolation | FaultViolation);
        
        HRESULT hr = S_OK;        
        ULONG cbOrigSigBlob = (ULONG)(-1);
        PCCOR_SIGNATURE pOrigSig = NULL;
        BOOL fPassThrough = FALSE;

        MemoTable<SigData*, SigData::Destroy> &memoTable = GetSignatureMemoTable<TOKENTYPE>();

        // Get from cache
        IfFailRet(GetCachedSigForToken(token, memoTable, pcbSigBlob, ppSig, &fPassThrough));
        if (hr == S_FALSE)
        {
            // We do not want to leak S_FALSE from this function
            hr = S_OK;
            
            // Original signature has already been provided?
            if ((pcbOrigSigBlob == NULL) || (ppOrigSig == NULL))
            {
                // Not provided, we need to get one by ourselves
                IfFailRet((GetOriginalSigForToken<T, TOKENTYPE>(pImport, token, &pOrigSig, &cbOrigSigBlob)));
            }
            else
            {
                // Provided, use that
                pOrigSig = *ppOrigSig;
                cbOrigSigBlob = *pcbOrigSigBlob;
            }
            
            if (fPassThrough)  // We cached that we don't need to reinterpret anything.
            {
                if (ppSig != NULL)
                    *ppSig = pOrigSig;
                if (pcbSigBlob != NULL)
                    *pcbSigBlob = cbOrigSigBlob;
            }
            else
            {
                SigData *pSigData;
                IfFailRet(ReinterpretSignature<TOKENTYPE>(cbOrigSigBlob, pOrigSig, &pSigData));
                IfFailRet(InsertCachedSigForToken(token, memoTable, &pSigData));
                
                fPassThrough = (pSigData == SigData::NOREDIRECT);

                if (ppSig != NULL)
                    *ppSig = (fPassThrough ? pOrigSig : (PCCOR_SIGNATURE)pSigData->data);
                if (pcbSigBlob != NULL)
                    *pcbSigBlob = (fPassThrough ? cbOrigSigBlob : pSigData->cbSig);
            }
        }
        else
        {
            _ASSERTE(!fPassThrough);
            // Already wrote to our output parameters.
        }
        // We should return error (via IfFailRet macro) or S_OK here
        _ASSERTE(hr == S_OK);
        
        return hr;
    }  

    //
    // Support for extra assembly refs inserted into this assembly
    //    
    ULONG GetRawAssemblyRefCount() { return m_rawAssemblyRefCount; }

    mdAssemblyRef GetAssemblyRefMscorlib() { return m_assemblyRefMscorlib; }

    LPCSTR GetExtraAssemblyRefName(mdAssemblyRef  mda)
    {
        UINT index = RidFromToken(mda) - m_rawAssemblyRefCount - 1;
        return WinMDAdapter::GetExtraAssemblyRefNameFromIndex((ContractAssemblyIndex)index);
    }


    static LPCSTR GetExtraAssemblyRefNameFromIndex(FrameworkAssemblyIndex index)
    {
        _ASSERTE(index >= 0 && index < FrameworkAssembly_Count);
        _ASSERTE(index != FrameworkAssembly_Mscorlib);
        switch(index)
        {
            case FrameworkAssembly_SystemObjectModel:
                return "System.ObjectModel";
            case FrameworkAssembly_SystemRuntime:
                return "System.Runtime";
            case FrameworkAssembly_SystemRuntimeWindowsRuntime:
                return "System.Runtime.WindowsRuntime";
            case FrameworkAssembly_SystemRuntimeWindowsRuntimeUIXaml:
                return "System.Runtime.WindowsRuntime.UI.Xaml";
            case FrameworkAssembly_SystemNumericsVectors:
                return "System.Numerics.Vectors";
            default:
                _ASSERTE(!"Invalid AssemblyRef token!");
                return NULL;
        }
    }

    static LPCSTR GetExtraAssemblyRefNameFromIndex(ContractAssemblyIndex index)
    {
        _ASSERTE(index >= 0 && index < ContractAssembly_Count);
        switch(index)
        {
            case ContractAssembly_SystemRuntime:
                return "System.Runtime";
            case ContractAssembly_SystemRuntimeInteropServicesWindowsRuntime:
                return "System.Runtime.InteropServices.WindowsRuntime";
            case ContractAssembly_SystemObjectModel:
                return "System.ObjectModel";
            case ContractAssembly_SystemRuntimeWindowsRuntime:
                return "System.Runtime.WindowsRuntime";
            case ContractAssembly_SystemRuntimeWindowsRuntimeUIXaml:
                return "System.Runtime.WindowsRuntime.UI.Xaml";
            case ContractAssembly_SystemNumericsVectors:
                return "System.Numerics.Vectors";
            default:
                _ASSERTE(!"Invalid AssemblyRef token!");
                return NULL;
        }
    }

    LPCWSTR GetExtraAssemblyRefNameW(mdAssemblyRef  mda)
    {
        UINT index = RidFromToken(mda) - m_rawAssemblyRefCount - 1;
        return WinMDAdapter::GetExtraAssemblyRefNameFromIndexW((ContractAssemblyIndex)index);
    }

    static LPCWSTR GetExtraAssemblyRefNameFromIndexW(ContractAssemblyIndex index)
    {
        _ASSERTE(index >= 0 && index < ContractAssembly_Count);
        switch(index)
        {
            case ContractAssembly_SystemRuntime:
                return W("System.Runtime");
            case ContractAssembly_SystemRuntimeInteropServicesWindowsRuntime:
                return W("System.Runtime.InteropServices.WindowsRuntime");
            case ContractAssembly_SystemObjectModel:
                return W("System.ObjectModel");
            case ContractAssembly_SystemRuntimeWindowsRuntime:
                return W("System.Runtime.WindowsRuntime");
            case ContractAssembly_SystemRuntimeWindowsRuntimeUIXaml:
                return W("System.Runtime.WindowsRuntime.UI.Xaml");
            case ContractAssembly_SystemNumericsVectors:
                return W("System.Numerics.Vectors");
            default:
                _ASSERTE(!"Invalid AssemblyRef token!");
                return NULL;
        }
    }

    BOOL IsValidAssemblyRefToken(mdAssemblyRef tk)
    {
        _ASSERTE(TypeFromToken(tk) == mdtAssemblyRef);

        RID rid = RidFromToken(tk);
        if (rid > 0 && 
            rid <= m_rawAssemblyRefCount + GetExtraAssemblyRefCount())
        {
            return TRUE;
        }

        return FALSE;
    }

    static void GetExtraAssemblyRefProps(FrameworkAssemblyIndex index,
                                         LPCSTR* ppName,
                                         AssemblyMetaDataInternal* pContext,
                                         PCBYTE * ppPublicKeytoken,
                                         DWORD* pTokenLength,
                                         DWORD* pdwFlags);

    BOOL IsScenarioWinMDExp()
    {
        return (m_scenario == kWinMDExp);
    }

    HRESULT IsRuntimeClassImplementation(mdTypeDef tkTypeDef, BOOL *pbResult)
    {
        _ASSERTE(pbResult != NULL);

        ULONG typeDefTreatment;
        HRESULT hr = GetTypeDefTreatment(tkTypeDef, &typeDefTreatment);

        if (SUCCEEDED(hr))
        {
            // kTdUnmangleWinRTName treatment means it is a <CLR> implementation class
            *pbResult = (typeDefTreatment == kTdUnmangleWinRTName);
        }

        return hr;
    }

private:
    struct CABlob;

private:
    
    WinMDAdapter(IMDCommon * pRawMDCommon);

    // S_OK if this is a CLR implementation type that was mangled and hidden by WinMDExp
    HRESULT CheckIfClrImplementationType(LPCSTR szName, DWORD dwAttr, LPCSTR *pszUnmangledName);

    // Get TypeRefTreatment value for a typeRef
    HRESULT GetTypeRefTreatment(mdTypeRef typeRef, ULONG *ppTypeRefTreatment);
    
    // Get TypeRef's index in array code:g_rgRedirectedTypes or return S_FALSE.
    HRESULT GetTypeRefRedirectedInfo(
        mdTypeRef             tkTypeRef, 
        RedirectedTypeIndex * pIndex);

    // Get TypeDefTreatment value for a typeDef
    HRESULT GetTypeDefTreatment(mdTypeDef typeDef, ULONG *ppTypeRefTreatment);

    // Get MethodTreatment value for a methodDef
    HRESULT GetMethodDefTreatment(mdMethodDef methodDef, ULONG *ppMethodDefTreatment);

    // Compute MethodTreatment value for a methodDef (unlike GetMethodDefTreatment, this
    //  does not cache.)
    HRESULT ComputeMethodDefTreatment(mdMethodDef tkMethodDef, mdTypeDef tkDeclaringTypeDef, ULONG *ppMethodDefTreatment);

    HRESULT CheckIfMethodImplImplementsARedirectedInterface(mdToken tkDecl, UINT *pIndex);

    HRESULT TranslateWinMDAttributeUsageAttribute(mdTypeDef tkTypeDefOfCA, DWORD *pClrTargetValue, BOOL *pAllowMultiple);

    static HRESULT CreateClrAttributeUsageAttributeCABlob(DWORD clrTargetValue, BOOL allowMultiple, CABlob **ppCABlob);

    // Whether the WinRT type should be hidden from managed code
    // Example: helper class/interface for projected jupiter structs   
    static BOOL IsHiddenWinRTType(LPCSTR szWinRTNamespace, LPCSTR szWinRTName);

    // Map a WinRT typename to CLR typename
    static BOOL ConvertWellKnownTypeNameFromWinRTToClr(LPCSTR *pszNamespace, LPCSTR *pszName, UINT *pIndex);

    static HRESULT CreatePrefixedName(LPCSTR szPrefix, LPCSTR szName, LPCSTR *ppOut);

    template <typename T> static void Delete(T* ptr)
    {
        delete [] ptr;
    }

    HRESULT RewriteTypeInSignature(SigParser * pSigParser, SigBuilder * pSigBuilder, BOOL * pfChangedSig);
    
  private:
    //-----------------------------------------------------------------------------------
    // Pointer to the raw view of the metadata.
    //-----------------------------------------------------------------------------------
    IMetaModelCommonRO *m_pRawMetaModelCommonRO;  


  private:
    //-----------------------------------------------------------------------------------
    // Stores whether the file is a pure .winmd file or one that combines WinRT and CLR code.
    //-----------------------------------------------------------------------------------
    enum WinMDScenario
    {
        kWinMDNormal = 1,   // File is normal Windows .winmd file  (Version string = "Windows Runtime nnn")
        kWinMDExp    = 2,   // File is output of winmdexp          (Version string = "Windows Runtime nnn;<dotnetVersion>")
    };

    WinMDScenario       m_scenario;
  

  private:

    //-----------------------------------------------------------------------------------
    // Every WinMD file is required to have an assemblyRef to mscorlib - this field caches that assemblyRef
    //-----------------------------------------------------------------------------------     
    mdAssemblyRef       m_assemblyRefMscorlib;
    BOOL                m_fReferencesMscorlibV4;    // m_assemblyRefMscorlib is a version=4.0.0.0 AssemblyRef
    ULONG               m_rawAssemblyRefCount;      // the raw assembly ref count not including the extra ones.
    LONG                m_extraAssemblyRefCount;    // the assembly ref count to return from IMetaDataAssemblyImport::EnumAssemblyRefs


  private:
    //-----------------------------------------------------------------------------------
    // For each typeref token, we cache an enum that determines how the adapter treats it.
    //-----------------------------------------------------------------------------------
    enum TypeRefTreatment
    {
        // The upper 8 bits determine how to interpret the lower 24-bits:
        kTrClassMask                   = 0xff000000,

        // Lower 24-bits represent fixed values (defined in rest of enum)
        kTrClassMisc                   = 0x00000000,  

        // TypeRef is one of a small # of hard-coded Windows.Foundation types that we redirect to mscorlib counterparts.
        // Lower 24-bits is index into typeref redirection table.
        kTrClassWellKnownRedirected    = 0x01000000,

        kTrNotYetInitialized = kTrClassMisc|0x000000, // Entry has not yet been initialized.
        kTrNoRewriteNeeded   = kTrClassMisc|0x000001, // Do not mangle the name. 
        kTrSystemDelegate    = kTrClassMisc|0x000002, // Fast-recognition code for System.Delegate
        kTrSystemAttribute   = kTrClassMisc|0x000003, // Fast-recognition code for System.Attribute
        kTrSystemEnum        = kTrClassMisc|0x000004, // Fast-recognition code for System.Enum
        kTrSystemValueType   = kTrClassMisc|0x000005, // Fast-recognition code for System.ValueType
    };
    MemoTable<ULONG, DoNothing<ULONG> > m_typeRefTreatmentMemoTable;  // Holds index into typeRef rename array or member of TypeRefTreatment enum

  private:
    //-----------------------------------------------------------------------------------
    // For each typedef token, we cache an enum that determines how the adapter treats it.
    //-----------------------------------------------------------------------------------
    enum TypeDefTreatment
    {
        kTdNotYetInitialized = 0x00,

        kTdTreatmentMask            = 0x0f,
        kTdOther                    = 0x01, // Anything not covered below, and not affected by the treatment flags
        kTdNormalNonAttribute       = 0x02, // non-attribute TypeDef from non-managed winmd assembly
        kTdNormalAttribute          = 0x03, // Attribute TypeDef from non-managed winmd assembly
        kTdUnmangleWinRTName        = 0x04, // Type in managed winmd that should be renamed from <CLR>Foo to Foo
        kTdPrefixWinRTName          = 0x05, // Type in managed winmd that should be renamed from Foo to <WinRT>Foo
        kTdRedirectedToCLRType      = 0x06, // Type is redirected to a CLR type implementation.
        kTdRedirectedToCLRAttribute = 0x07, // Type is redirected to a CLR type implementation that is an attribute.

        kTdMarkAbstractFlag  = 0x10, // Type should be marked as abstract
        kTdMarkInternalFlag  = 0x20, // Type should be hidden from managed code - examples are struct helpers
                                     // for redirected Jupiter structs
        kTdEnum              = 0x40, // Type is an enum from managed\non-managed winmd assembly.
    };
    MemoTable<ULONG, DoNothing<ULONG> > m_typeDefTreatmentMemoTable; // Holds member of TypeDefTreatment enum

  private:
    //-----------------------------------------------------------------------------------
    // For each methoddef token, we cache an enum that determines how the adapter treats it.
    //-----------------------------------------------------------------------------------
    enum MethodDefTreatment
    {
        kMdNotYetInitialized        = 0x00,
        kMdTreatmentMask            = 0x0f, // Mask of various options
        kMdOther                    = 0x01, // Anything not covered below
        kMdDelegate                 = 0x02, // Declared by delegate
        kMdAttribute                = 0x03, // Declared by an attribute
        kMdInterface                = 0x04, // Is member of interface
        kMdImplementation           = 0x05, // CLR implementation of RuntimeClass
        kMdHiddenImpl               = 0x06, // Implements a redirected (hidden) interface
        kMdtUnusedFlag              = 0x07, // UnUnsed flag.
        kMdRenameToDisposeMethod    = 0x08, // Rename IClosable.Close to Dispose

        kMdMarkAbstractFlag         = 0x10, // Method should be marked as abstract
        kMdMarkPublicFlag           = 0x20, // Method visibility should be marked as public
    };
    MemoTable<ULONG, DoNothing<ULONG> > m_methodDefTreatmentMemoTable; // Holds member of MethodDefTreatment enum

  private:
    //-----------------------------------------------------------------------------------
    // The version string we report to callers.
    //-----------------------------------------------------------------------------------
    LPSTR               m_pRedirectedVersionString;

  private:
    //-----------------------------------------------------------------------------------
    // For each customattribute token, we cache the blob that the adapter reports to callers.
    // The special pointer value CABlob::NOREDIRECT indicates that the raw blob is to be
    // passed unchanged.
    //-----------------------------------------------------------------------------------

    // Represents a custom attribute blob
    struct CABlob
    {
        ULONG cbBlob;    // Length of blob in bytes.
        BYTE  data[1];   // Start of variable-length blob (cbBlob indicates length in bytes.)

        static CABlob* Create(const BYTE *pBlob, ULONG cbBlob);
        static void Destroy(CABlob *pCABlob);

        // Sentinel value in m_redirectedCABlobsMemoTable table. Means "do no blob rewriting. Return the one from the underlying importer."
        static CABlob* const NOREDIRECT;
    };

    MemoTable<CABlob*, CABlob::Destroy> m_redirectedCABlobsMemoTable; // Array of rewritten CA blobs

  private:
    //-----------------------------------------------------------------------------------
    // For each token, we cache the signature that the adapter reports to callers.
    //-----------------------------------------------------------------------------------
    MemoTable<SigData*, SigData::Destroy> m_redirectedMethodDefSigMemoTable;  // Array of rewritten MethodDef signatures
    MemoTable<SigData*, SigData::Destroy> m_redirectedFieldDefSigMemoTable;   // Array of rewritten FieldDef signatures
    MemoTable<SigData*, SigData::Destroy> m_redirectedMemberRefSigMemoTable;  // Array of rewritten MemberRef signatures
    MemoTable<SigData*, SigData::Destroy> m_redirectedPropertySigMemoTable;   // Array of rewritten Property signatures
    MemoTable<SigData*, SigData::Destroy> m_redirectedTypeSpecSigMemoTable;   // Array of rewritten TypeSpec signatures
    MemoTable<SigData*, SigData::Destroy> m_redirectedMethodSpecSigMemoTable; // Array of rewritten MethodSpec signatures

    // Explicit specializations of GetSignatureMemoTable for all supported token types
    template<> MemoTable<SigData*, SigData::Destroy> &GetSignatureMemoTable<mdtMethodDef>()  { return m_redirectedMethodDefSigMemoTable;  }
    template<> MemoTable<SigData*, SigData::Destroy> &GetSignatureMemoTable<mdtFieldDef>()   { return m_redirectedFieldDefSigMemoTable;   }
    template<> MemoTable<SigData*, SigData::Destroy> &GetSignatureMemoTable<mdtMemberRef>()  { return m_redirectedMemberRefSigMemoTable;  }
    template<> MemoTable<SigData*, SigData::Destroy> &GetSignatureMemoTable<mdtProperty>()   { return m_redirectedPropertySigMemoTable;   }
    template<> MemoTable<SigData*, SigData::Destroy> &GetSignatureMemoTable<mdtTypeSpec>()   { return m_redirectedTypeSpecSigMemoTable;   }
    template<> MemoTable<SigData*, SigData::Destroy> &GetSignatureMemoTable<mdtMethodSpec>() { return m_redirectedMethodSpecSigMemoTable; }

  private:
    //-----------------------------------------------------------------------------------
    // For each typedef whose name we mangle, we cache the mangled name that we report to callers. (The "name"
    // is the "name" half of the namespace/name pair. We don't mangle namespaces.)
    //
    // Currently, the adapter used the tdAttr value to determine whether a typedef name needs
    // be mangled at all - thus, we don't have a sentinel for "don't mangle."
    //-----------------------------------------------------------------------------------
    MemoTable<LPCSTR, Delete<const char> > m_mangledTypeNameTable;  // Array of mangled typedef names
};


#endif // __MDWinMDAdapter__h__
