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

    int GetExtraAssemblyRefCount()
    {
        return 0;
    }

    // Factory and destructor
    static HRESULT Create(IMDCommon *pRawMDCommon, /*[out]*/ WinMDAdapter **ppAdapter);
    ~WinMDAdapter();

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

    //
    // Support for extra assembly refs inserted into this assembly
    //
    ULONG GetRawAssemblyRefCount() { return m_rawAssemblyRefCount; }

    mdAssemblyRef GetAssemblyRefMscorlib() { return m_assemblyRefMscorlib; }

    LPCSTR GetExtraAssemblyRefName(mdAssemblyRef  mda)
    {
        _ASSERT(false);
    }

    LPCWSTR GetExtraAssemblyRefNameW(mdAssemblyRef  mda)
    {
        _ASSERT(false);
    }

    BOOL IsValidAssemblyRefToken(mdAssemblyRef tk)
    {
        _ASSERTE(TypeFromToken(tk) == mdtAssemblyRef);

        RID rid = RidFromToken(tk);
        if (rid > 0 &&
            rid <= m_rawAssemblyRefCount)
        {
            return TRUE;
        }

        return FALSE;
    }

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

    // Get TypeDefTreatment value for a typeDef
    HRESULT GetTypeDefTreatment(mdTypeDef typeDef, ULONG *ppTypeRefTreatment);

    // Get MethodTreatment value for a methodDef
    HRESULT GetMethodDefTreatment(mdMethodDef methodDef, ULONG *ppMethodDefTreatment);

    // Compute MethodTreatment value for a methodDef (unlike GetMethodDefTreatment, this
    //  does not cache.)
    HRESULT ComputeMethodDefTreatment(mdMethodDef tkMethodDef, mdTypeDef tkDeclaringTypeDef, ULONG *ppMethodDefTreatment);

    static HRESULT CreatePrefixedName(LPCSTR szPrefix, LPCSTR szName, LPCSTR *ppOut);

    template <typename T> static void Delete(T* ptr)
    {
        delete [] ptr;
    }

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

        kTdMarkAbstractFlag  = 0x10, // Type should be marked as abstract
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
        kMdtUnusedFlag              = 0x07, // UnUnsed flag.

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
