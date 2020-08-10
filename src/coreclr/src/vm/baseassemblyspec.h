// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// BaseAssemblySpec.h
//


//
// Declares the BaseAssemblySpec class
//
// ============================================================

#ifndef __BASE_ASSEMBLY_SPEC_H__
#define __BASE_ASSEMBLY_SPEC_H__

class StackingAllocator;

// a class representing assembly name in Loader
class BaseAssemblySpec
{
protected:
    AssemblyMetaDataInternal    m_context;
    LPCSTR                      m_pAssemblyName;
    PBYTE                       m_pbPublicKeyOrToken;
    DWORD                       m_cbPublicKeyOrToken;
    DWORD                       m_dwFlags;             // CorAssemblyFlags
    LPCWSTR                     m_wszCodeBase;         // URL to the code
    int                         m_ownedFlags;
    ICLRPrivBinder             *m_pBindingContext;

public:
    enum
    {
        NAME_OWNED                  = 0x01,
        PUBLIC_KEY_OR_TOKEN_OWNED   = 0x02,
        CODE_BASE_OWNED             = 0x04,
        LOCALE_OWNED                = 0x08,
        CODEBASE_OWNED              = 0x10,
        // unused                   = 0x20,
        // Set if ParseName() returned illegal textual identity.
        // Cannot process the string any further.
        BAD_NAME_OWNED              = 0x40,
        ALL_OWNED                   = 0xFF,
    };

    BaseAssemblySpec();
    ~BaseAssemblySpec();

    HRESULT Init(LPCSTR pAssemblyName,
                 const AssemblyMetaDataInternal* pContext,
                 const BYTE * pbPublicKeyOrToken, DWORD cbPublicKeyOrToken,
                 DWORD dwFlags);

    HRESULT Init(mdToken tkAssemblyRef, IMDInternalImport *pImport);
    HRESULT Init(mdAssembly tkAssemblyRef, IMetaDataAssemblyImport* pImport);
    HRESULT Init(LPCSTR pAssemblyDisplayName);

    HRESULT Init(IAssemblyName *pName);

    // Note that this method does not clone the fields!
    VOID CopyFrom(const BaseAssemblySpec *pSpec);

    VOID    CloneFields(int flags=ALL_OWNED);
    VOID    CloneFieldsToLoaderHeap(int flags, LoaderHeap *pHeap, AllocMemTracker *pamTracker);
    VOID    CloneFieldsToStackingAllocator(StackingAllocator* alloc);

    inline void SetBindingContext(ICLRPrivBinder *pBindingContext)
    {
        LIMITED_METHOD_CONTRACT;

        m_pBindingContext = pBindingContext;
    }

    inline ICLRPrivBinder* GetBindingContext()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pBindingContext;
    }

    BOOL IsAssemblySpecForCoreLib();

    HRESULT ParseName();
    DWORD Hash();

    LPCSTR GetName() const;
    inline void GetName(SString & ssName) const { WRAPPER_NO_CONTRACT; ssName.SetUTF8(GetName()); }

    void SetName(LPCSTR szName);
    void SetName(SString const & ssName);

    LPCWSTR GetCodeBase() const;
    void SetCodeBase(LPCWSTR szCodeBase);

    VOID SetCulture(LPCSTR szCulture);
    bool IsNeutralCulture();

    VOID ConvertPublicKeyToToken();

    void  SetContext(ASSEMBLYMETADATA* assemblyData);

    inline AssemblyMetaDataInternal *GetContext() { LIMITED_METHOD_CONTRACT; return &m_context; }
    inline AssemblyMetaDataInternal const *GetContext() const { LIMITED_METHOD_CONTRACT; return &m_context; }

    BOOL IsStrongNamed() const;
    BOOL HasPublicKey() const;
    BOOL HasPublicKeyToken() const;
    BOOL IsCoreLibSatellite() const;
    BOOL IsCoreLib();

    // Returns true
    inline BOOL HasUniqueIdentity() const
    {
        STATIC_CONTRACT_LIMITED_METHOD;
        return TRUE;
    }

    enum CompareExFlags
    {
        ASC_Default                 = 0x00, // Default comparison policy.
        ASC_DefinitionEquality      = 0x01, // Will not treat non-bindable content types as equivalent.
    };

    BOOL CompareEx(BaseAssemblySpec *pSpec, DWORD dwCompareFlags = ASC_Default);
    static int CompareStrings(LPCUTF8 string1, LPCUTF8 string2);
    static BOOL RefMatchesDef(const BaseAssemblySpec* pRef, const BaseAssemblySpec* pDef);

    void GetFileOrDisplayName(DWORD flags, SString &result) const;
    void GetDisplayName(DWORD flags, SString &result) const;

protected:
    static BOOL CompareRefToDef(const BaseAssemblySpec *pRef, const BaseAssemblySpec *pDef);

private:
    void GetDisplayNameInternal(DWORD flags, SString &result) const;
};

#endif // __BASE_ASSEMBLY_SPEC_H__
