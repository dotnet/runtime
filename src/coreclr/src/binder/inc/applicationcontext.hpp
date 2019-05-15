// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// ApplicationContext.hpp
//


//
// Defines the ApplicationContext class
//
// ============================================================

#ifndef __BINDER__APPLICATION_CONTEXT_HPP__
#define __BINDER__APPLICATION_CONTEXT_HPP__

#include "bindertypes.hpp"
#include "failurecache.hpp"
#include "assemblyidentitycache.hpp"
#ifdef FEATURE_VERSIONING_LOG
#include "bindinglog.hpp"
#endif // FEATURE_VERSIONING_LOG
#include "stringarraylist.h"

namespace BINDER_SPACE
{
    //=============================================================================================
    // Data structures for Simple Name -> File Name hash
    struct FileNameMapEntry
    {
        LPWSTR m_wszFileName;
    };
    
    class FileNameHashTraits : public NoRemoveSHashTraits< DefaultSHashTraits< FileNameMapEntry > >
    {
     public:
        typedef PCWSTR key_t;
        static const FileNameMapEntry Null() { FileNameMapEntry e; e.m_wszFileName = nullptr; return e; }
        static bool IsNull(const FileNameMapEntry & e) { return e.m_wszFileName == nullptr; }
        static key_t GetKey(const FileNameMapEntry & e)
        {
            key_t key;
            key = e.m_wszFileName;
            return key;
        }
        static count_t Hash(const key_t &str) { return HashiString(str); }
        static BOOL Equals(const key_t &lhs, const key_t &rhs) { LIMITED_METHOD_CONTRACT; return (_wcsicmp(lhs, rhs) == 0); }
    };

    typedef SHash<FileNameHashTraits> TpaFileNameHash;
    
    // Entry in SHash table that maps namespace to list of files
    struct SimpleNameToFileNameMapEntry
    {
        LPWSTR m_wszSimpleName;
        LPWSTR m_wszILFileName;
        LPWSTR m_wszNIFileName;
    };
    
    // SHash traits for Namespace -> FileNameList hash
    class SimpleNameToFileNameMapTraits : public NoRemoveSHashTraits< DefaultSHashTraits< SimpleNameToFileNameMapEntry > >
    {
     public:
        typedef PCWSTR key_t;
        static const SimpleNameToFileNameMapEntry Null() { SimpleNameToFileNameMapEntry e; e.m_wszSimpleName = nullptr; return e; }
        static bool IsNull(const SimpleNameToFileNameMapEntry & e) { return e.m_wszSimpleName == nullptr; }
        static key_t GetKey(const SimpleNameToFileNameMapEntry & e)
        {
            key_t key;
            key = e.m_wszSimpleName;
            return key;
        }
        static count_t Hash(const key_t &str) { return HashiString(str); }
        static BOOL Equals(const key_t &lhs, const key_t &rhs) { LIMITED_METHOD_CONTRACT; return (_wcsicmp(lhs, rhs) == 0); }
        
        void OnDestructPerEntryCleanupAction(const SimpleNameToFileNameMapEntry & e)
        {
            if (e.m_wszILFileName == nullptr && e.m_wszNIFileName == nullptr)
            {
                // Don't delete simple name here since it's a filename only entry and will be cleaned up
                // by the SimpleName -> FileName entry which reuses the same filename pointer.
                return;
            }
            
            if (e.m_wszSimpleName != nullptr)
            {
                delete [] e.m_wszSimpleName;
            }
            if (e.m_wszILFileName != nullptr)
            {
                delete [] e.m_wszILFileName;
            }
            if (e.m_wszNIFileName != nullptr)
            {
                delete [] e.m_wszNIFileName;
            }
        }
        static const bool s_DestructPerEntryCleanupAction = true;
    };

    typedef SHash<SimpleNameToFileNameMapTraits> SimpleNameToFileNameMap;
    
    class ApplicationContext
        : public IUnknown
    {
    public:
        // IUnknown methods
        STDMETHOD(QueryInterface)(REFIID   riid,
                                  void   **ppv);
        STDMETHOD_(ULONG, AddRef)();
        STDMETHOD_(ULONG, Release)();

        // ApplicationContext methods
        ApplicationContext();
        virtual ~ApplicationContext();
        HRESULT Init();

        inline SString &GetApplicationName();
        inline DWORD GetAppDomainId();
        inline void SetAppDomainId(DWORD dwAppDomainId);

        HRESULT SetupBindingPaths(/* in */ SString &sTrustedPlatformAssemblies,
                                  /* in */ SString &sPlatformResourceRoots,
                                  /* in */ SString &sAppPaths,
                                  /* in */ SString &sAppNiPaths,
                                  /* in */ BOOL     fAcquireLock);

        HRESULT GetAssemblyIdentity(/* in */ LPCSTR                szTextualIdentity,
                                    /* in */ AssemblyIdentityUTF8 **ppAssemblyIdentity);

        // Getters/Setter
        inline ExecutionContext *GetExecutionContext();
        inline InspectionContext *GetInspectionContext();
        inline FailureCache *GetFailureCache();
        inline HRESULT AddToFailureCache(SString &assemblyNameOrPath,
                                         HRESULT  hrBindResult);
        inline StringArrayList *GetAppPaths();
        inline SimpleNameToFileNameMap *GetTpaList();
        inline TpaFileNameHash *GetTpaFileNameList();
        inline StringArrayList *GetPlatformResourceRoots();
        inline StringArrayList *GetAppNiPaths();
        
        // Using a host-configured Trusted Platform Assembly list
        bool IsTpaListProvided();
        inline CRITSEC_COOKIE GetCriticalSectionCookie();
        inline LONG GetVersion();
        inline void IncrementVersion();

#ifdef FEATURE_VERSIONING_LOG
        inline BindingLog *GetBindingLog();
        inline void ClearBindingLog();
#endif // FEATURE_VERSIONING_LOG

    protected:
        LONG               m_cRef;
        Volatile<LONG>     m_cVersion;
        SString            m_applicationName;
        DWORD              m_dwAppDomainId;
        ExecutionContext  *m_pExecutionContext;
        InspectionContext *m_pInspectionContext;
        FailureCache      *m_pFailureCache;
        CRITSEC_COOKIE     m_contextCS;
#ifdef FEATURE_VERSIONING_LOG
        BindingLog         m_bindingLog;
#endif // FEATURE_VERSIONING_LOG

        AssemblyIdentityCache m_assemblyIdentityCache;

        StringArrayList    m_platformResourceRoots;
        StringArrayList    m_appPaths;
        StringArrayList    m_appNiPaths;

        SimpleNameToFileNameMap * m_pTrustedPlatformAssemblyMap;
        TpaFileNameHash    * m_pFileNameHash;
    };

#include "applicationcontext.inl"

};

#endif
