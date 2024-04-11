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
#include "stringarraylist.h"
#include "simplefilenamemap.h"

namespace BINDER_SPACE
{
    class AssemblyHashTraits;
    typedef SHash<AssemblyHashTraits> ExecutionContext;

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
        
        HRESULT SetupBindingPaths(/* in */ SString &sPlatformResourceRoots,
                                  /* in */ SString &sAppPaths,
                                  /* in */ BOOL     fAcquireLock);

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
        HRESULT AddAssemblyMapEntry(SString& simpleName, SString& fileName);

    private:
        Volatile<LONG>     m_cVersion;
        SString            m_applicationName;
        ExecutionContext  *m_pExecutionContext;
        FailureCache      *m_pFailureCache;
        CRITSEC_COOKIE     m_contextCS;

        StringArrayList    m_platformResourceRoots;
        StringArrayList    m_appPaths;

        SimpleNameToFileNameMap * m_pTrustedPlatformAssemblyMap;
    };

#include "applicationcontext.inl"

};

#endif
