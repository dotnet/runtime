// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#include "assemblyspecbase.h"
#include "domainfile.h"
#include "holder.h"

class AppDomain;
class Assembly;
class DomainAssembly;
enum FileLoadLevel;

class AssemblySpec  : public BaseAssemblySpec
{
  private:
    AppDomain       *m_pAppDomain;
    DWORD            m_dwHashAlg;
    DomainAssembly  *m_pParentAssembly;

    // Contains the reference to the fallback load context associated with RefEmitted assembly requesting the load of another assembly (static or dynamic)
    AssemblyBinder *m_pFallbackLoadContextBinder;

    // Flag to indicate if we should prefer the fallback load context binder for binding or not.
    bool m_fPreferFallbackLoadContextBinder;

    BOOL IsValidAssemblyName();

    HRESULT InitializeSpecInternal(mdToken kAssemblyRefOrDef,
                                   IMDInternalImport *pImport,
                                   DomainAssembly *pStaticParent,
                                   BOOL fAllowAllocation);

    // InitializeSpecInternal should be used very carefully so it's made private.
    // functions that take special care (and thus are allowed to use the function) are listed below
    friend Assembly * Module::GetAssemblyIfLoaded(
                mdAssemblyRef       kAssemblyRef,
                IMDInternalImport * pMDImportOverride,
                BOOL                fDoNotUtilizeExtraChecks,
                AssemblyBinder      *pBindingContextForLoadedAssembly);

  public:

#ifndef DACCESS_COMPILE
    AssemblySpec() : m_pAppDomain(::GetAppDomain())
    {
        LIMITED_METHOD_CONTRACT;
        m_pParentAssembly = NULL;

        m_pFallbackLoadContextBinder = NULL;
        m_fPreferFallbackLoadContextBinder = false;

    }
#endif //!DACCESS_COMPILE

    AssemblySpec(AppDomain *pAppDomain) : m_pAppDomain(pAppDomain)
    {
        LIMITED_METHOD_CONTRACT
        m_pParentAssembly = NULL;

        m_pFallbackLoadContextBinder = NULL;
        m_fPreferFallbackLoadContextBinder = false;

    }


    DomainAssembly* GetParentAssembly();

    AssemblyBinder* GetBindingContextFromParentAssembly(AppDomain *pDomain);

    bool HasParentAssembly()
    { WRAPPER_NO_CONTRACT; return GetParentAssembly() != NULL; }

    void InitializeSpec(mdToken kAssemblyRefOrDef,
                        IMDInternalImport *pImport,
                        DomainAssembly *pStaticParent = NULL)
    {
        CONTRACTL
        {
            INSTANCE_CHECK;
            GC_TRIGGERS;
            THROWS;
            MODE_ANY;
        }
        CONTRACTL_END;
        HRESULT hr=InitializeSpecInternal(kAssemblyRefOrDef, pImport,pStaticParent,TRUE);
        if(FAILED(hr))
            EEFileLoadException::Throw(this,hr);
    };


    void InitializeSpec(PEAssembly *pFile);
    HRESULT InitializeSpec(StackingAllocator* alloc,
                        ASSEMBLYNAMEREF* pName,
                        BOOL fParse = TRUE);

    void AssemblyNameInit(ASSEMBLYNAMEREF* pName, PEImage* pImageInfo); //[in,out], [in]


    void SetCodeBase(LPCWSTR szCodeBase)
    {
        WRAPPER_NO_CONTRACT;
        BaseAssemblySpec::SetCodeBase(szCodeBase);
    }

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
    }

    void SetFallbackLoadContextBinderForRequestingAssembly(AssemblyBinder *pFallbackLoadContextBinder)
    {
       LIMITED_METHOD_CONTRACT;

        m_pFallbackLoadContextBinder = pFallbackLoadContextBinder;
    }

    AssemblyBinder* GetFallbackLoadContextBinderForRequestingAssembly()
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

        SetParentAssembly(pSource->GetParentAssembly());

        // Copy the details of the fallback load context binder
        SetFallbackLoadContextBinderForRequestingAssembly(pSource->GetFallbackLoadContextBinderForRequestingAssembly());
        m_fPreferFallbackLoadContextBinder = pSource->GetPreferFallbackLoadContextBinder();

        m_dwHashAlg = pSource->m_dwHashAlg;
    }

    HRESULT CheckFriendAssemblyName();

    HRESULT EmitToken(IMetaDataAssemblyEmit *pEmit,
                      mdAssemblyRef *pToken);

    VOID Bind(
        AppDomain* pAppDomain,
        BOOL fThrowOnFileNotFound,
        CoreBindResult* pBindResult,
        BOOL fNgenExplicitBind = FALSE,
        BOOL fExplicitBindToNativeImage = FALSE);

    Assembly *LoadAssembly(FileLoadLevel targetLevel,
                           BOOL fThrowOnFileNotFound = TRUE);
    DomainAssembly *LoadDomainAssembly(FileLoadLevel targetLevel,
                                       BOOL fThrowOnFileNotFound = TRUE);

  public: // static
    // Creates and loads an assembly based on the name and context.
    static Assembly *LoadAssembly(LPCSTR pSimpleName,
                                  AssemblyMetaDataInternal* pContext,
                                  const BYTE * pbPublicKeyOrToken,
                                  DWORD cbPublicKeyOrToken,
                                  DWORD dwFlags);

    // Load an assembly based on an explicit path
    static Assembly *LoadAssembly(LPCWSTR pFilePath);

    // Initialize an AssemblyName managed object based on the specified assemblyName
    static void InitializeAssemblyNameRef(_In_ BINDER_SPACE::AssemblyName* assemblyName, _Out_ ASSEMBLYNAMEREF* assemblyNameRef);

  public:
    void MatchPublicKeys(Assembly *pAssembly);

    AppDomain *GetAppDomain()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pAppDomain;
    }

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
            // WinRT assemblies are not supported as direct references.
            return COR_E_PLATFORMNOTSUPPORTED;
        }
        else
        {
            _ASSERTE(!"Unexpected content type.");
            return E_UNEXPECTED;
        }
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

    AssemblySpecBindingCache::AssemblyBinding* LookupInternal(AssemblySpec* pSpec, BOOL fThrow = FALSE);

  public:

    AssemblySpecBindingCache() DAC_EMPTY();
    ~AssemblySpecBindingCache() DAC_EMPTY();

    void Init(CrstBase *pCrst, LoaderHeap *pHeap = NULL);
    void Clear();

    BOOL Contains(AssemblySpec *pSpec);

    DomainAssembly *LookupAssembly(AssemblySpec *pSpec, BOOL fThrow=TRUE);
    PEAssembly *LookupFile(AssemblySpec *pSpec, BOOL fThrow = TRUE);

    BOOL StoreAssembly(AssemblySpec *pSpec, DomainAssembly *pAssembly);
    BOOL StoreFile(AssemblySpec *pSpec, PEAssembly *pFile);

    BOOL StoreException(AssemblySpec *pSpec, Exception* pEx);

    BOOL RemoveAssembly(DomainAssembly* pAssembly);

    DWORD Hash(AssemblySpec *pSpec)
    {
        WRAPPER_NO_CONTRACT;
        return pSpec->Hash();
    }

#if !defined(DACCESS_COMPILE)
    void GetAllAssemblies(SetSHash<PTR_DomainAssembly>& assemblyList)
    {
        PtrHashMap::PtrIterator i = m_map.begin();
        while (!i.end())
        {
            AssemblyBinding *b = (AssemblyBinding*) i.GetValue();
            if(!b->IsError() && b->GetAssembly() != NULL)
                assemblyList.AddOrReplace(b->GetAssembly());
            ++i;
        }
    }
#endif // !defined(DACCESS_COMPILE)

    static BOOL CompareSpecs(UPTR u1, UPTR u2);
};

#endif
