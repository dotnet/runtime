// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
#include "stringarraylist.h"

namespace BINDER_SPACE
{
    //=============================================================================================
    // Data structures for Simple Name -> File Name hash

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
        static count_t Hash(const key_t &str)
        {
            SString ssKey(SString::Literal, str);
            return ssKey.HashCaseInsensitive();
        }
        static BOOL Equals(const key_t &lhs, const key_t &rhs) { LIMITED_METHOD_CONTRACT; return (SString::_wcsicmp(lhs, rhs) == 0); }

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
    {
    public:
        // ApplicationContext methods
        ApplicationContext();
        ~ApplicationContext();
        HRESULT Init();

        inline SString &GetApplicationName();

        HRESULT SetupBindingPaths(/* in */ SString &sTrustedPlatformAssemblies,
                                  /* in */ SString &sPlatformResourceRoots,
                                  /* in */ SString &sAppPaths,
                                  /* in */ BOOL     fAcquireLock);

        HRESULT GetAssemblyIdentity(/* in */ LPCSTR                szTextualIdentity,
                                    /* in */ AssemblyIdentityUTF8 **ppAssemblyIdentity);

        // Getters/Setter
        inline ExecutionContext *GetExecutionContext();
        inline FailureCache *GetFailureCache();
        inline HRESULT AddToFailureCache(SString &assemblyNameOrPath,
                                         HRESULT  hrBindResult);
        inline StringArrayList *GetAppPaths();
        inline SimpleNameToFileNameMap *GetTpaList();
        inline StringArrayList *GetPlatformResourceRoots();

        // Using a host-configured Trusted Platform Assembly list
        bool IsTpaListProvided();
        inline CRITSEC_COOKIE GetCriticalSectionCookie();
        inline LONG GetVersion();
        inline void IncrementVersion();

    private:
        Volatile<LONG>     m_cVersion;
        SString            m_applicationName;
        ExecutionContext  *m_pExecutionContext;
        FailureCache      *m_pFailureCache;
        CRITSEC_COOKIE     m_contextCS;

        AssemblyIdentityCache m_assemblyIdentityCache;

        StringArrayList    m_platformResourceRoots;
        StringArrayList    m_appPaths;

        SimpleNameToFileNameMap * m_pTrustedPlatformAssemblyMap;
    };

#include "applicationcontext.inl"

};

#endif
