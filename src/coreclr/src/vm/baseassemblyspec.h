//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
#ifdef FEATURE_FUSION
    LOADCTX_TYPE                m_fParentLoadContext;  // m_pParentAssembly->GetFusionLoadContext()
    ReleaseHolder<IAssemblyName> m_pNameAfterPolicy;
#endif
    LPCSTR                      m_szWinRtTypeNamespace;
    LPCSTR                      m_szWinRtTypeClassName;
#ifdef FEATURE_HOSTED_BINDER
    ICLRPrivBinder             *m_pHostBinder;
#endif
    int                         m_ownedFlags;
    BOOL                        m_fIntrospectionOnly;
#if defined(FEATURE_CORECLR)
    ICLRPrivBinder             *m_pBindingContext;
#endif // defined(FEATURE_CORECLR)

public:
    enum 
    {
        NAME_OWNED                  = 0x01,
        PUBLIC_KEY_OR_TOKEN_OWNED   = 0x02,
        CODE_BASE_OWNED             = 0x04,
        LOCALE_OWNED                = 0x08,
        CODEBASE_OWNED              = 0x10,
        WINRT_TYPE_NAME_OWNED       = 0x20,
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

#if defined(FEATURE_CORECLR)
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
    
    BOOL IsAssemblySpecForMscorlib();
#endif // defined(FEATURE_CORECLR)
    
    HRESULT ParseName();
    DWORD Hash();

    LPCSTR GetName() const;
    inline void GetName(SString & ssName) const { WRAPPER_NO_CONTRACT; ssName.SetUTF8(GetName()); }
    
    void SetName(LPCSTR szName);
    void SetName(SString const & ssName);

    LPCWSTR GetCodeBase();
    void SetCodeBase(LPCWSTR szCodeBase);

    VOID SetCulture(LPCSTR szCulture);

    VOID ConvertPublicKeyToToken();

    void  SetContext(ASSEMBLYMETADATA* assemblyData);

    inline AssemblyMetaDataInternal *GetContext() { LIMITED_METHOD_CONTRACT; return &m_context; }
    inline AssemblyMetaDataInternal const *GetContext() const { LIMITED_METHOD_CONTRACT; return &m_context; }
    
    BOOL IsStrongNamed() const;
    BOOL HasPublicKey() const;
    BOOL HasPublicKeyToken() const;
    BOOL IsMscorlibSatellite();
    BOOL IsMscorlibDebugSatellite();
    BOOL IsMscorlib();

    //
    // Windows Runtime functions that could not be refactored out to AssemblySpec
    //
    inline LPCSTR GetWinRtTypeNamespace() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_szWinRtTypeNamespace;
    }
    inline LPCSTR GetWinRtTypeClassName() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_szWinRtTypeClassName;
    }

    //****************************************************************************************
    //
    // Creates an IAssemblyName object representing this AssemblySpec.
    //
    //    fMustBeBindable - if set to TRUE, the resulting IAssemblyName may contain internal
    //                      encodings needed to make an identity bindable (this is the case
    //                      for WinRT assemblies: a representative type name is encoded as
    //                      part of the assembly simple name). Be careful to ensure that
    //                      encoded identities are not exposed to customers.
    HRESULT CreateFusionName(
        IAssemblyName **ppName,
        BOOL fIncludeCodeBase = TRUE, /* Used by fusion only */
        BOOL fMustBeBindable = FALSE) const;

#ifdef FEATURE_FUSION
    // for fusion binding
    virtual IAssembly* GetParentIAssembly() =0;

    // for identity comparison
    virtual LPCVOID GetParentAssemblyPtr() =0;

    inline LOADCTX_TYPE GetParentLoadContext()
    {
        LIMITED_METHOD_CONTRACT;
        return m_fParentLoadContext;
    }
#endif

    BOOL IsIntrospectionOnly()
    {
        LIMITED_METHOD_CONTRACT;

        // Important to ensure we return a normalized boolean (the introspection fields
        // of different AssemblySpecs can be compared.)
        return !!m_fIntrospectionOnly;
    }

    VOID SetIntrospectionOnly(BOOL fIntrospectionOnly)
    {
        LIMITED_METHOD_CONTRACT;
        m_fIntrospectionOnly = !!fIntrospectionOnly;
    }

    inline BOOL IsContentType_WindowsRuntime() const
    {
        LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_COMINTEROP
        return IsAfContentType_WindowsRuntime(m_dwFlags);
#else
        return FALSE;
#endif
    }

    void GetEncodedName(SString & ssEncodedName) const;
    
    // Returns true if this object uniquely identifies a single assembly;
    // false otherwise. This will return false for Windows Runtime assemblies,
    // as WinRT assembly names do not represent an identity. This method
    // does not take into account additional attributes such as type namespace
    // and name.
    inline BOOL HasUniqueIdentity() const
    {
        STATIC_CONTRACT_LIMITED_METHOD;
        return !IsContentType_WindowsRuntime();
    }
    
    enum CompareExFlags
    {
        ASC_Default                 = 0x00, // Default comparison policy.
        ASC_DefinitionEquality      = 0x01, // Will not treat non-bindable content types as equivalent.
    };

    BOOL CompareEx(BaseAssemblySpec *pSpec, DWORD dwCompareFlags = ASC_Default);
    static int CompareStrings(LPCUTF8 string1, LPCUTF8 string2);
    static BOOL RefMatchesDef(const BaseAssemblySpec* pRef, const BaseAssemblySpec* pDef);
    static BOOL VerifyBindingString(LPCWSTR pwStr); 

    void GetFileOrDisplayName(DWORD flags, SString &result) const;

    inline void GetPublicKey(
        PBYTE * ppbPublicKey,
        DWORD * pcbPublicKey) const
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION(HasPublicKey());
        if (ppbPublicKey != nullptr)
        {
            *ppbPublicKey = m_pbPublicKeyOrToken;
        }
        if (pcbPublicKey != nullptr)
        {
            *pcbPublicKey = m_cbPublicKeyOrToken;
        }
    }

    inline void GetPublicKeyToken(
        PBYTE * ppbPublicKeyToken,
        DWORD * pcbPublicKeyToken) const
    {
        LIMITED_METHOD_CONTRACT;
        PRECONDITION(HasPublicKeyToken());
        if (ppbPublicKeyToken != nullptr)
        {
            *ppbPublicKeyToken = m_pbPublicKeyOrToken;
        }
        if (pcbPublicKeyToken != nullptr)
        {
            *pcbPublicKeyToken = m_cbPublicKeyOrToken;
        }
    }

    inline BOOL IsRetargetable() const
    {
        LIMITED_METHOD_CONTRACT;
        return IsAfRetargetable(m_dwFlags);
    }

#ifdef FEATURE_FUSION
    inline IAssemblyName* GetNameAfterPolicy() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pNameAfterPolicy;
    }

    inline void ReleaseNameAfterPolicy()
    {
        LIMITED_METHOD_CONTRACT;
        m_pNameAfterPolicy=NULL;
    }

    inline void SetNameAfterPolicy(IAssemblyName* pName)
    {
        LIMITED_METHOD_CONTRACT;
        m_pNameAfterPolicy=pName;
    }


    void SetPEKIND(PEKIND peKind)
    {
        LIMITED_METHOD_CONTRACT;
        C_ASSERT(afPA_None == PAFlag(peNone));
        C_ASSERT(afPA_MSIL == PAFlag(peMSIL));
        C_ASSERT(afPA_x86 == PAFlag(peI386));
        C_ASSERT(afPA_IA64 == PAFlag(peIA64));
        C_ASSERT(afPA_AMD64 == PAFlag(peAMD64));
        C_ASSERT(afPA_ARM == PAFlag(peARM));
        
        _ASSERTE((peKind <= peARM) || (peKind == peInvalid));
        
        m_dwFlags &= ~afPA_FullMask;
        m_dwFlags |= PAFlag(peKind);
    }

    PEKIND GetPEKIND() const
    {
        LIMITED_METHOD_CONTRACT;
        return static_cast<PEKIND>(PAIndex(m_dwFlags));
    }
#endif //FEATURE_FUSION

protected:
    static BOOL CompareRefToDef(const BaseAssemblySpec *pRef, const BaseAssemblySpec *pDef);
};

#endif // __BASE_ASSEMBLY_SPEC_H__
