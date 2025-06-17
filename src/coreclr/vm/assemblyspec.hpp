// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header:  AssemblySpec.hpp
**
** Purpose: Implements classes used to bind to assemblies
**
===========================================================*/

#ifndef _ASSEMBLYSPEC_H
#define _ASSEMBLYSPEC_H

#include "hash.h"
#include "assemblyspecbase.h"
#include "domainassembly.h"
#include "holder.h"

enum FileLoadLevel
{
    // These states are tracked by FileLoadLock

    // Note: This enum must match the static array fileLoadLevelName[]
    //       which contains the printable names of the enum values

    // Note that semantics here are description is the LAST step done, not what is
    // currently being done.

    FILE_LOAD_CREATE,
    FILE_LOAD_BEGIN,
    FILE_LOAD_BEFORE_TYPE_LOAD,
    FILE_LOAD_EAGER_FIXUPS,
    FILE_LOAD_DELIVER_EVENTS,
    FILE_LOAD_VTABLE_FIXUPS,
    FILE_LOADED,                    // Loaded by not yet active
    FILE_ACTIVE                     // Fully active (constructors run & security checked)
};

enum NotificationStatus
{
    NOT_NOTIFIED=0,
    PROFILER_NOTIFIED=1,
    DEBUGGER_NEEDNOTIFICATION=2,
    DEBUGGER_NOTIFIED=4
};

class AssemblySpec  : public BaseAssemblySpec
{
  private:
    AppDomain       *m_pAppDomain;
    Assembly  *m_pParentAssembly;

    // Contains the reference to the fallback load context associated with RefEmitted assembly requesting the load of another assembly (static or dynamic)
    AssemblyBinder *m_pFallbackBinder;

    // Flag to indicate if we should prefer the fallback load context binder for binding or not.
    bool m_fPreferFallbackBinder;

    HRESULT InitializeSpecInternal(mdToken kAssemblyRefOrDef,
                                   IMDInternalImport *pImport,
                                   Assembly *pStaticParent);

    // InitializeSpecInternal should be used very carefully so it's made private.
    // functions that take special care (and thus are allowed to use the function) are listed below
    friend Assembly * Module::GetAssemblyIfLoaded(
                mdAssemblyRef       kAssemblyRef,
                IMDInternalImport * pMDImportOverride,
                AssemblyBinder      *pBinderForLoadedAssembly);

  public:

#ifndef DACCESS_COMPILE
    AssemblySpec() : m_pAppDomain(::GetAppDomain())
    {
        LIMITED_METHOD_CONTRACT;
        m_pParentAssembly = NULL;

        m_pFallbackBinder = NULL;
        m_fPreferFallbackBinder = false;

    }
#endif //!DACCESS_COMPILE

    AssemblySpec(AppDomain *pAppDomain) : m_pAppDomain(pAppDomain)
    {
        LIMITED_METHOD_CONTRACT
        m_pParentAssembly = NULL;

        m_pFallbackBinder = NULL;
        m_fPreferFallbackBinder = false;

    }


    Assembly* GetParentAssembly();

    AssemblyBinder* GetBinderFromParentAssembly(AppDomain *pDomain);

    bool HasParentAssembly()
    { WRAPPER_NO_CONTRACT; return GetParentAssembly() != NULL; }

    void InitializeSpec(mdToken kAssemblyRefOrDef,
                        IMDInternalImport *pImport,
                        Assembly *pStaticParent = NULL)
    {
        CONTRACTL
        {
            INSTANCE_CHECK;
            GC_NOTRIGGER;
            THROWS;
            MODE_ANY;
        }
        CONTRACTL_END;
        HRESULT hr = InitializeSpecInternal(kAssemblyRefOrDef, pImport, pStaticParent);
        if(FAILED(hr))
            EEFileLoadException::Throw(this,hr);
    };


    void InitializeSpec(PEAssembly* pPEAssembly);

    void AssemblyNameInit(ASSEMBLYNAMEREF* pName); //[in,out]

    void SetParentAssembly(Assembly *pAssembly)
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

    void SetFallbackBinderForRequestingAssembly(AssemblyBinder *pFallbackBinder)
    {
       LIMITED_METHOD_CONTRACT;

        m_pFallbackBinder = pFallbackBinder;
    }

    AssemblyBinder* GetFallbackBinderForRequestingAssembly()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pFallbackBinder;
    }

    void SetPreferFallbackBinder()
    {
        LIMITED_METHOD_CONTRACT;

        m_fPreferFallbackBinder = true;
    }

    bool GetPreferFallbackBinder()
    {
        LIMITED_METHOD_CONTRACT;

        return m_fPreferFallbackBinder;
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
        SetFallbackBinderForRequestingAssembly(pSource->GetFallbackBinderForRequestingAssembly());
        m_fPreferFallbackBinder = pSource->GetPreferFallbackBinder();
    }

    HRESULT CheckFriendAssemblyName();

    HRESULT EmitToken(IMetaDataAssemblyEmit *pEmit,
                      mdAssemblyRef *pToken);

    HRESULT Bind(
        AppDomain* pAppDomain,
        BINDER_SPACE::Assembly** ppAssembly);

    Assembly *LoadAssembly(FileLoadLevel targetLevel,
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
            entry->CloneFields();

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

            if (m_pPEAssembly != NULL)
                m_pPEAssembly->Release();

            if (m_exceptionType==EXTYPE_EE)
                delete m_pException;
        };

        inline Assembly* GetAssembly(){ LIMITED_METHOD_CONTRACT; return m_pAssembly; };
        inline void SetAssembly(Assembly* pAssembly){ LIMITED_METHOD_CONTRACT; m_pAssembly = pAssembly; };
        inline PEAssembly* GetFile(){ LIMITED_METHOD_CONTRACT; return m_pPEAssembly;};
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
        inline void Init(AssemblySpec* pSpec, PEAssembly* pPEAssembly, Assembly* pAssembly, Exception* pEx, LoaderHeap *pHeap, AllocMemTracker *pamTracker)
        {
            CONTRACTL
            {
                THROWS;
                WRAPPER(GC_TRIGGERS);
                MODE_ANY;
            }
            CONTRACTL_END;

            InitInternal(pSpec, pPEAssembly, pAssembly);
            if (pHeap != NULL)
            {
                m_spec.CloneFieldsToLoaderHeap(pHeap, pamTracker);
            }
            else
            {
                m_spec.CloneFields();
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
                RethrowTransientExceptions();
            }
            EX_END_CATCH

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

        inline void InitInternal(AssemblySpec* pSpec, PEAssembly* pPEAssembly, Assembly* pAssembly )
        {
            WRAPPER_NO_CONTRACT;
            m_spec.CopyFrom(pSpec);
            m_pPEAssembly = pPEAssembly;
            if (m_pPEAssembly)
                m_pPEAssembly->AddRef();
            m_pAssembly = pAssembly;
            m_exceptionType=EXTYPE_NONE;
        }

        AssemblySpec    m_spec;
        PEAssembly      *m_pPEAssembly;
        Assembly        *m_pAssembly;
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

    Assembly *LookupAssembly(AssemblySpec *pSpec, BOOL fThrow=TRUE);
    PEAssembly *LookupFile(AssemblySpec *pSpec, BOOL fThrow = TRUE);

    BOOL StoreAssembly(AssemblySpec *pSpec, Assembly *pAssembly);
    BOOL StorePEAssembly(AssemblySpec *pSpec, PEAssembly *pPEAssembly);

    BOOL StoreException(AssemblySpec *pSpec, Exception* pEx);

    BOOL RemoveAssembly(Assembly* pAssembly);

    DWORD Hash(AssemblySpec *pSpec)
    {
        WRAPPER_NO_CONTRACT;
        return pSpec->Hash();
    }

#if !defined(DACCESS_COMPILE)
    void GetAllAssemblies(SetSHash<PTR_Assembly>& assemblyList)
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
