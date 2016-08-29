// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Header:  AssemblySpec.hpp
**
** Purpose: Implements classes used to bind to assemblies
**
**


**
===========================================================*/
#ifndef _ASSEMBLYSPEC_H
#define _ASSEMBLYSPEC_H
#include "hash.h"
#include "memorypool.h"
#ifdef FEATURE_FUSION
#include "fusionbind.h"
#endif
#include "assemblyspecbase.h"
#include "domainfile.h"
#include "genericstackprobe.h"
#include "holder.h"

class AppDomain;
class Assembly;
class DomainAssembly;
enum FileLoadLevel;

class AssemblySpec  : public BaseAssemblySpec
{
  private:

    friend class AppDomain;
    friend class AssemblyNameNative;
    
    AppDomain       *m_pAppDomain;
    SBuffer          m_HashForControl;
    DWORD            m_dwHashAlg;
    DomainAssembly  *m_pParentAssembly;

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)
    // Contains the reference to the fallback load context associated with RefEmitted assembly requesting the load of another assembly (static or dynamic)
    ICLRPrivBinder *m_pFallbackLoadContextBinder;

    // Flag to indicate if we should prefer the fallback load context binder for binding or not.
    bool m_fPreferFallbackLoadContextBinder;
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)

    BOOL IsValidAssemblyName();
    
    HRESULT InitializeSpecInternal(mdToken kAssemblyRefOrDef, 
                                   IMDInternalImport *pImport, 
                                   DomainAssembly *pStaticParent,
                                   BOOL fIntrospectionOnly,
                                   BOOL fAllowAllocation);

    // InitializeSpecInternal should be used very carefully so it's made private.
    // functions that take special care (and thus are allowed to use the function) are listed below
    friend Assembly * Module::GetAssemblyIfLoaded(
                mdAssemblyRef       kAssemblyRef, 
                LPCSTR              szWinRtNamespace, 
                LPCSTR              szWinRtClassName, 
                IMDInternalImport * pMDImportOverride,
                BOOL                fDoNotUtilizeExtraChecks,
                ICLRPrivBinder      *pBindingContextForLoadedAssembly);
    
  public:

#ifndef DACCESS_COMPILE
    AssemblySpec() : m_pAppDomain(::GetAppDomain())
    {
        LIMITED_METHOD_CONTRACT;
        m_pParentAssembly = NULL;

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)
        m_pFallbackLoadContextBinder = NULL;     
        m_fPreferFallbackLoadContextBinder = false;   
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)

    }
#endif //!DACCESS_COMPILE

    AssemblySpec(AppDomain *pAppDomain) : m_pAppDomain(pAppDomain)
    { 
        LIMITED_METHOD_CONTRACT
        m_pParentAssembly = NULL;

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)
        m_pFallbackLoadContextBinder = NULL;
        m_fPreferFallbackLoadContextBinder = false;        
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)

    }

#ifdef FEATURE_FUSION
    virtual IAssembly* GetParentIAssembly();

    virtual LPCVOID GetParentAssemblyPtr();
#endif

    DomainAssembly* GetParentAssembly();
    
    ICLRPrivBinder* GetBindingContextFromParentAssembly(AppDomain *pDomain);
    
    bool HasParentAssembly()
    { WRAPPER_NO_CONTRACT; return GetParentAssembly() != NULL; }

    void InitializeSpec(mdToken kAssemblyRefOrDef, 
                        IMDInternalImport *pImport, 
                        DomainAssembly *pStaticParent = NULL,
                        BOOL fIntrospectionOnly = FALSE)
    {
        CONTRACTL
        {
            INSTANCE_CHECK;
            GC_TRIGGERS;
            THROWS;
            MODE_ANY;
        }
        CONTRACTL_END;
        HRESULT hr=InitializeSpecInternal(kAssemblyRefOrDef, pImport,pStaticParent,fIntrospectionOnly,TRUE);
        if(FAILED(hr))
            EEFileLoadException::Throw(this,hr);
#ifndef FEATURE_CORECLR
        CloneFields();
#endif
    };

#ifdef FEATURE_FUSION
    void InitializeSpec(IAssemblyName *pName,
                        DomainAssembly *pStaticParent = NULL,
                        BOOL fIntrospectionOnly = FALSE);
#endif // FEATURE_FUSION

    void InitializeSpec(PEAssembly *pFile);
    HRESULT InitializeSpec(StackingAllocator* alloc,
                        ASSEMBLYNAMEREF* pName,
                        BOOL fParse = TRUE,
                        BOOL fIntrospectionOnly = FALSE);

    void AssemblyNameInit(ASSEMBLYNAMEREF* pName, PEImage* pImageInfo); //[in,out], [in]

#ifdef FEATURE_MIXEDMODE
    void InitializeSpec(HINSTANCE hMod, BOOL fIntrospectionOnly = FALSE);
#endif // FEATURE_MIXEDMODE

    void SetCodeBase(LPCWSTR szCodeBase)
    {
        WRAPPER_NO_CONTRACT;
        BaseAssemblySpec::SetCodeBase(szCodeBase);
    }
    void SetCodeBase(StackingAllocator* alloc, STRINGREF *pCodeBase);

    void SetParentAssembly(DomainAssembly *pAssembly)
    {
        CONTRACTL
        {
            INSTANCE_CHECK;
            GC_NOTRIGGER;
            NOTHROW;
            MODE_ANY;
        }
        CONTRACTL_END;

        m_pParentAssembly = pAssembly;
#ifdef FEATURE_FUSION
        if (pAssembly)
        {
            _ASSERTE(GetHostBinder() == nullptr);
            m_fParentLoadContext=pAssembly->GetFile()->GetLoadContext();
        }
        else
            m_fParentLoadContext = LOADCTX_TYPE_DEFAULT;
#endif
    }

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)
    void SetFallbackLoadContextBinderForRequestingAssembly(ICLRPrivBinder *pFallbackLoadContextBinder)
    {
       LIMITED_METHOD_CONTRACT;

        m_pFallbackLoadContextBinder = pFallbackLoadContextBinder;
    }

    ICLRPrivBinder* GetFallbackLoadContextBinderForRequestingAssembly()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pFallbackLoadContextBinder;
    }

    void SetPreferFallbackLoadContextBinder()
    {
        LIMITED_METHOD_CONTRACT;

        m_fPreferFallbackLoadContextBinder = true;
    }

    bool GetPreferFallbackLoadContextBinder()
    {
        LIMITED_METHOD_CONTRACT;

        return m_fPreferFallbackLoadContextBinder;
    }
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)

    // Note that this method does not clone the fields!
    void CopyFrom(AssemblySpec* pSource)
    {
        CONTRACTL
        {
            INSTANCE_CHECK;
            THROWS;
            MODE_ANY;
        }
        CONTRACTL_END;

        BaseAssemblySpec::CopyFrom(pSource);

        SetIntrospectionOnly(pSource->IsIntrospectionOnly());
        SetParentAssembly(pSource->GetParentAssembly());

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)
        // Copy the details of the fallback load context binder
        SetFallbackLoadContextBinderForRequestingAssembly(pSource->GetFallbackLoadContextBinderForRequestingAssembly());
        m_fPreferFallbackLoadContextBinder = pSource->GetPreferFallbackLoadContextBinder();
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)

        m_HashForControl = pSource->m_HashForControl;
        m_dwHashAlg = pSource->m_dwHashAlg;
    }


#ifndef FEATURE_FUSION     
    HRESULT CheckFriendAssemblyName();
#endif // FEATURE_FUSION


    HRESULT EmitToken(IMetaDataAssemblyEmit *pEmit, 
                      mdAssemblyRef *pToken,
                      BOOL fUsePublicKeyToken = TRUE,
                      BOOL fMustBeBindable = FALSE /*(used only by FusionBind's implementation)*/);

    // Make sure this matches in the managed Assembly.DemandPermission()
    enum FilePermFlag {
        FILE_PATHDISCOVERY   = 0x0,
        FILE_READ            = 0x1,
        FILE_READANDPATHDISC = 0x2,
        FILE_WEBPERM         = 0x3
    };

#ifdef FEATURE_FUSION    
    static void DemandFileIOPermission(LPCWSTR wszCodeBase,
                                       BOOL fHavePath,
                                       DWORD dwDemandFlag);
    void DemandFileIOPermission(PEAssembly *pFile);
#endif


#ifndef FEATURE_FUSION
    VOID Bind(
        AppDomain* pAppDomain, 
        BOOL fThrowOnFileNotFound,
        CoreBindResult* pBindResult,
        BOOL fNgenExplicitBind = FALSE, 
        BOOL fExplicitBindToNativeImage = FALSE,
        StackCrawlMark *pCallerStackMark  = NULL );
#ifndef FEATURE_CORECLR
    static VOID BindToSystem(BINDER_SPACE::Assembly** ppAssembly);
#endif
#endif

    Assembly *LoadAssembly(FileLoadLevel targetLevel, 
                           AssemblyLoadSecurity *pLoadSecurity = NULL,
                           BOOL fThrowOnFileNotFound = TRUE,
                           BOOL fRaisePrebindEvents = TRUE,
                           StackCrawlMark *pCallerStackMark = NULL);
    DomainAssembly *LoadDomainAssembly(FileLoadLevel targetLevel,
                                       AssemblyLoadSecurity *pLoadSecurity = NULL,
                                       BOOL fThrowOnFileNotFound = TRUE,
                                       BOOL fRaisePrebindEvents = TRUE,
                                       StackCrawlMark *pCallerStackMark = NULL);

    //****************************************************************************************
    //
    // Creates and loads an assembly based on the name and context.
    static Assembly *LoadAssembly(LPCSTR pSimpleName, 
                                  AssemblyMetaDataInternal* pContext,
                                  const BYTE * pbPublicKeyOrToken,
                                  DWORD cbPublicKeyOrToken,
                                  DWORD dwFlags);

#ifdef FEATURE_FUSION
    //****************************************************************************************
    //
    HRESULT LoadAssembly(IApplicationContext *pFusionContext, 
                         FusionSink *pSink,
                         IAssembly** ppIAssembly,
                         IHostAssembly** ppIHostAssembly,
                         IBindResult **ppNativeFusionAssembly,
                         BOOL fForIntrospectionOnly,
                         BOOL fSuppressSecurityChecks);
#endif

    // Load an assembly based on an explicit path
    static Assembly *LoadAssembly(LPCWSTR pFilePath);

#ifdef FEATURE_FUSION
    BOOL FindAssemblyFile(AppDomain *pAppDomain, BOOL fThrowOnFileNotFound,
                          IAssembly** ppIAssembly, IHostAssembly **ppIHostAssembly, IBindResult** pNativeFusionAssembly,
                          IFusionBindLog **ppFusionLog, HRESULT *pHRBindResult, StackCrawlMark *pCallerStackMark = NULL,
                          AssemblyLoadSecurity *pLoadSecurity = NULL);
#endif // FEATURE_FUSION

  private:
    void MatchRetargetedPublicKeys(Assembly *pAssembly);
  public:
    void MatchPublicKeys(Assembly *pAssembly);
    PEAssembly *ResolveAssemblyFile(AppDomain *pAppDomain, BOOL fPreBind);

    AppDomain *GetAppDomain() 
    {
        LIMITED_METHOD_CONTRACT;
        return m_pAppDomain;
    }

    HRESULT SetHashForControl(PBYTE pHashForControl, DWORD dwHashForControl, DWORD dwHashAlg)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            PRECONDITION(CheckPointer(pHashForControl));
        }
        CONTRACTL_END;

        m_HashForControl.Set(pHashForControl, dwHashForControl);
        m_dwHashAlg=dwHashAlg; 
        return S_OK;
    }

    void ParseEncodedName();
    
    void SetWindowsRuntimeType(LPCUTF8 szNamespace, LPCUTF8 szClassName);
    void SetWindowsRuntimeType(SString const & _ssTypeName);

    inline HRESULT SetContentType(AssemblyContentType type)
    {
        LIMITED_METHOD_CONTRACT;
        if (type == AssemblyContentType_Default)
        {
            m_dwFlags = (m_dwFlags & ~afContentType_Mask) | afContentType_Default;
            return S_OK;
        }
        else if (type == AssemblyContentType_WindowsRuntime)
        {
            m_dwFlags = (m_dwFlags & ~afContentType_Mask) | afContentType_WindowsRuntime;
            return S_OK;
        }
        else
        {
            _ASSERTE(!"Unexpected content type.");
            return E_UNEXPECTED;
        }
    }
    
    // Returns true if the object can be used to bind to the target assembly.
    // One case in which this is not true is when the content type is WinRT
    // but no type name has been set.
    inline bool HasBindableIdentity() const
    {
        STATIC_CONTRACT_LIMITED_METHOD;
#ifdef FEATURE_COMINTEROP
        return (HasUniqueIdentity() ||
                (IsContentType_WindowsRuntime() && (GetWinRtTypeClassName() != NULL)));
#else
        return TRUE;
#endif
    }

    inline BOOL CanUseWithBindingCache() const
    {
        STATIC_CONTRACT_LIMITED_METHOD;
#if defined(FEATURE_APPX_BINDER)
        return (GetHostBinder() == nullptr) && HasUniqueIdentity();
#else
        return HasUniqueIdentity(); 
#endif
    }

    inline ICLRPrivBinder *GetHostBinder() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_pHostBinder;
    }

    inline void SetHostBinder(ICLRPrivBinder *pHostBinder)
    {
        LIMITED_METHOD_CONTRACT;
        m_pHostBinder = pHostBinder;
    }

};

#define INITIAL_ASM_SPEC_HASH_SIZE 7
class AssemblySpecHash
{
    LoaderHeap *m_pHeap;
    PtrHashMap m_map;

  public:

#ifndef DACCESS_COMPILE
    AssemblySpecHash(LoaderHeap *pHeap = NULL)
      : m_pHeap(pHeap)
    {
        CONTRACTL
        {
            CONSTRUCTOR_CHECK;
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;
   
        m_map.Init(INITIAL_ASM_SPEC_HASH_SIZE, CompareSpecs, FALSE, NULL);
    }

    ~AssemblySpecHash();
#endif

#ifndef DACCESS_COMPILE
    //
    // Returns TRUE if the spec was already in the table
    //

    BOOL Store(AssemblySpec *pSpec, AssemblySpec **ppStoredSpec = NULL)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
            INJECT_FAULT(COMPlusThrowOM());
        }
        CONTRACTL_END

        DWORD key = pSpec->Hash();

        AssemblySpec *entry = (AssemblySpec *) m_map.LookupValue(key, pSpec);

        if (entry == (AssemblySpec*) INVALIDENTRY)
        {
            if (m_pHeap != NULL)
                entry = new (m_pHeap->AllocMem(S_SIZE_T(sizeof(AssemblySpec)))) AssemblySpec;
            else
                entry = new AssemblySpec;

            GCX_PREEMP();
            entry->CopyFrom(pSpec);
            entry->CloneFields(AssemblySpec::ALL_OWNED);

            m_map.InsertValue(key, entry);

            if (ppStoredSpec != NULL)
                *ppStoredSpec = entry;

            return FALSE;
        }
        else
        {
            if (ppStoredSpec != NULL)
                *ppStoredSpec = entry;
            return TRUE;
        }
    }
#endif // DACCESS_COMPILE

    DWORD Hash(AssemblySpec *pSpec)
    {
        WRAPPER_NO_CONTRACT;
        return pSpec->Hash();
    }

    static BOOL CompareSpecs(UPTR u1, UPTR u2);
};


class AssemblySpecBindingCache
{
    friend class AssemblyBindingHolder;
    struct AssemblyBinding
    {
        public: 
        ~AssemblyBinding()
        {
            WRAPPER_NO_CONTRACT;

            if (m_pFile != NULL)
                m_pFile->Release();

            if (m_exceptionType==EXTYPE_EE)
                delete m_pException;
        };

        void OnAppDomainUnload()
        {
            LIMITED_METHOD_CONTRACT;
            if (m_exceptionType == EXTYPE_EE)
            {
                m_exceptionType = EXTYPE_NONE;
                delete m_pException;
                m_pException = NULL;
            }
        };

        inline DomainAssembly* GetAssembly(){ LIMITED_METHOD_CONTRACT; return m_pAssembly;};
        inline void SetAssembly(DomainAssembly* pAssembly){ LIMITED_METHOD_CONTRACT;  m_pAssembly=pAssembly;};        
        inline PEAssembly* GetFile(){ LIMITED_METHOD_CONTRACT; return m_pFile;};
        inline BOOL IsError(){ LIMITED_METHOD_CONTRACT; return (m_exceptionType!=EXTYPE_NONE);};

        // bound to the file, but failed later
        inline BOOL IsPostBindError(){ LIMITED_METHOD_CONTRACT; return IsError() && GetFile()!=NULL;};
        
        inline void ThrowIfError()
        {
            CONTRACTL
            {
                THROWS;
                GC_TRIGGERS;
                MODE_ANY;
            }
            CONTRACTL_END;

            switch(m_exceptionType)
            {
                case EXTYPE_NONE: return;
                case EXTYPE_HR: ThrowHR(m_hr);
                case EXTYPE_EE:  PAL_CPP_THROW(Exception *, m_pException->DomainBoundClone()); 
                default: _ASSERTE(!"Unexpected exception type");
            }
        };
        inline void Init(AssemblySpec* pSpec, PEAssembly* pFile, DomainAssembly* pAssembly, Exception* pEx, LoaderHeap *pHeap, AllocMemTracker *pamTracker)
        {
            CONTRACTL
            {
                THROWS;
                WRAPPER(GC_TRIGGERS);
                MODE_ANY;
            }
            CONTRACTL_END;

            InitInternal(pSpec,pFile,pAssembly);
            if (pHeap != NULL)
            {
                m_spec.CloneFieldsToLoaderHeap(AssemblySpec::ALL_OWNED,pHeap, pamTracker);
            }
            else
            {
                m_spec.CloneFields(m_spec.ALL_OWNED);
            }
            InitException(pEx);

        }

        inline HRESULT GetHR()
        {
            LIMITED_METHOD_CONTRACT;
            switch(m_exceptionType)
            {
                case EXTYPE_NONE: return S_OK;
                case EXTYPE_HR: return m_hr;
                case EXTYPE_EE:  return m_pException->GetHR(); 
                default: _ASSERTE(!"Unexpected exception type");
            }
            return E_UNEXPECTED;
        };
        
        inline void InitException(Exception* pEx)
        {
            CONTRACTL
            {
                THROWS;
                WRAPPER(GC_TRIGGERS);
                MODE_ANY;
            }
            CONTRACTL_END;

            _ASSERTE(m_exceptionType==EXTYPE_NONE);

            if (pEx==NULL)
                return;

            _ASSERTE(!pEx->IsTransient());

            EX_TRY
            {
                m_pException = pEx->DomainBoundClone();
                _ASSERTE(m_pException);
                m_exceptionType=EXTYPE_EE;
            }
            EX_CATCH
            {
                InitException(pEx->GetHR());
            }
            EX_END_CATCH(RethrowTransientExceptions);

        };

        inline void InitException(HRESULT hr)
        {
            LIMITED_METHOD_CONTRACT;
            _ASSERTE(m_exceptionType==EXTYPE_NONE);
            if (FAILED(hr))
            {
                m_exceptionType=EXTYPE_HR;
                m_hr=hr;
            }
        };
    protected:

        inline void InitInternal(AssemblySpec* pSpec, PEAssembly* pFile, DomainAssembly* pAssembly )            
        {
            WRAPPER_NO_CONTRACT;
            m_spec.CopyFrom(pSpec);
            m_pFile = pFile;
            if (m_pFile)
                m_pFile->AddRef();
            m_pAssembly = pAssembly;
            m_exceptionType=EXTYPE_NONE;
        }
            
        AssemblySpec    m_spec;
        PEAssembly      *m_pFile;
        DomainAssembly  *m_pAssembly;
        enum{
            EXTYPE_NONE               = 0x00000000,
            EXTYPE_HR                    = 0x00000001,
            EXTYPE_EE                    = 0x00000002,
        };      
        INT         m_exceptionType;
        union
        {
            HRESULT m_hr;
            Exception* m_pException;
        };
    };

    PtrHashMap m_map;
    LoaderHeap *m_pHeap;

#if defined(FEATURE_CORECLR)    
    AssemblySpecBindingCache::AssemblyBinding* GetAssemblyBindingEntryForAssemblySpec(AssemblySpec* pSpec, BOOL fThrow);
#endif // defined(FEATURE_CORECLR)
    
  public:

    AssemblySpecBindingCache() DAC_EMPTY();
    ~AssemblySpecBindingCache() DAC_EMPTY();

    void Init(CrstBase *pCrst, LoaderHeap *pHeap = NULL);
    void Clear();

    void OnAppDomainUnload();

    BOOL Contains(AssemblySpec *pSpec);

    DomainAssembly *LookupAssembly(AssemblySpec *pSpec, BOOL fThrow=TRUE);
    PEAssembly *LookupFile(AssemblySpec *pSpec, BOOL fThrow = TRUE);

    BOOL StoreAssembly(AssemblySpec *pSpec, DomainAssembly *pAssembly);
    BOOL StoreFile(AssemblySpec *pSpec, PEAssembly *pFile);
    
    BOOL StoreException(AssemblySpec *pSpec, Exception* pEx);

    DWORD Hash(AssemblySpec *pSpec)
    {
        WRAPPER_NO_CONTRACT;
        return pSpec->Hash();
    }

    static BOOL CompareSpecs(UPTR u1, UPTR u2);
};

#define INITIAL_DOMAIN_ASSEMBLY_CACHE_SIZE 17
class DomainAssemblyCache
{
    struct AssemblyEntry {
        AssemblySpec spec;
        LPVOID       pData[2];     // Can be an Assembly, PEAssembly, or an Unmanaged DLL
        
        DWORD Hash()
        {
            WRAPPER_NO_CONTRACT;
            return spec.Hash();
        }
    };
        
    PtrHashMap  m_Table;
    AppDomain*  m_pDomain;

public:

    static BOOL CompareBindingSpec(UPTR spec1, UPTR spec2);

    void InitializeTable(AppDomain* pDomain, CrstBase *pCrst)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(pDomain);
        m_pDomain = pDomain;

        LockOwner lock = {pCrst, IsOwnerOfCrst};
        m_Table.Init(INITIAL_DOMAIN_ASSEMBLY_CACHE_SIZE, &CompareBindingSpec, true, &lock);
    }
    
    AssemblyEntry* LookupEntry(AssemblySpec* pSpec);

    LPVOID  LookupEntry(AssemblySpec* pSpec, UINT index)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(index < 2);
        AssemblyEntry* ptr = LookupEntry(pSpec);
        if(ptr == NULL)
            return NULL;
        else
            return ptr->pData[index];
    }

    VOID InsertEntry(AssemblySpec* pSpec, LPVOID pData1, LPVOID pData2 = NULL);

private:

};

#endif
