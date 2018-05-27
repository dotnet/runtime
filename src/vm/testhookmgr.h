// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// 

#ifndef CLR_TESTHOOKMGR_H
#define  CLR_TESTHOOKMGR_H

#include "testhook.h"

#if defined(_DEBUG) && !defined(CROSSGEN_COMPILE)
#define FEATURE_TESTHOOKS
#endif

#ifdef FEATURE_TESTHOOKS
#define TESTHOOKCALL(function) \
    do { if(CLRTestHookManager::Enabled())       \
             CLRTestHookManager::TestHook()->function;} while(0)

#define TESTHOOKENABLED CLRTestHookManager::Enabled()
#define TESTHOOKSLOWPATH CLRTestHookManager::SlowPathEnabled()

#define MAX_TEST_HOOKS 32

//Forward declarations
template <typename T> class Volatile;
extern "C" Volatile<LONG> g_TrapReturningThreads;


struct CLRTestHookInfo
{
protected:
    int m_Version;
    union
    {
        ICLRTestHook* v1;
        ICLRTestHook2* v2;
        ICLRTestHook3* v3;
    } m_Hook;
public:    
    void Set(ICLRTestHook*);
    ICLRTestHook* v1();
    ICLRTestHook2* v2();
    ICLRTestHook3* v3();
};

class  CLRTestHookManager: public ICLRTestHookManager2, public ICLRTestHook3
{
protected:
    static CLRTestHookManager* g_pManager;
    Volatile<LONG> m_nHooks; 
    Volatile<LONG> m_cRef;
    CLRTestHookInfo m_pHooks[MAX_TEST_HOOKS];
    virtual ~CLRTestHookManager();
public:
    CLRTestHookManager();
    STDMETHOD(AddTestHook)(ICLRTestHook* hook);
    static ICLRTestHookManager* Start();
    static BOOL Enabled() {return g_pManager!=NULL;};
    static BOOL  SlowPathEnabled() {return Enabled() && g_TrapReturningThreads;};
    static CLRTestHookManager* TestHook() {return g_pManager;};
    static HRESULT CheckConfig();
    STDMETHOD_(ULONG,AddRef) ();
    STDMETHOD_(ULONG, Release());
    STDMETHOD (QueryInterface)(REFIID riid, void  * *ppvObject);
    STDMETHOD(AppDomainStageChanged)(DWORD adid,DWORD oldstage,DWORD newstage);
    STDMETHOD(NextFileLoadLevel)(DWORD adid, LPVOID domainfile,DWORD newlevel);
    STDMETHOD(CompletingFileLoadLevel)(DWORD adid, LPVOID domainfile,DWORD newlevel);
    STDMETHOD(CompletedFileLoadLevel)(DWORD adid, LPVOID domainfile,DWORD newlevel);
    STDMETHOD(EnteringAppDomain)(DWORD adid);
    STDMETHOD(EnteredAppDomain)(DWORD adid);
    STDMETHOD(LeavingAppDomain)(DWORD adid);
    STDMETHOD(LeftAppDomain)(DWORD adid);
    STDMETHOD(UnwindingThreads)(DWORD adid);
    STDMETHOD(UnwoundThreads)(DWORD adid);	
    STDMETHOD(AppDomainCanBeUnloaded)(DWORD adid, BOOL bUnsafePoint); 
    STDMETHOD(AppDomainDestroyed)(DWORD adid);
    STDMETHOD(EnableSlowPath) (BOOL bEnable);	
    STDMETHOD(UnloadAppDomain)(DWORD adid,DWORD flags);
    STDMETHOD(StartingNativeImageBind)(LPCWSTR wszAsmName, BOOL bIsCompilationProcess);
    STDMETHOD(CompletedNativeImageBind)(LPVOID pFile,LPCUTF8 simpleName, BOOL hasNativeImage);
    STDMETHOD(AboutToLockImage)(LPCWSTR wszPath, BOOL bIsCompilationProcess);
    STDMETHOD_(VOID,DoAppropriateWait)( int cObjs, HANDLE *pObjs, INT32 iTimeout, BOOL bWaitAll, int* res);	
    STDMETHOD(GC)(int generation);
    STDMETHOD(GetSimpleName)(LPVOID domainfile,LPCUTF8* name);	
    STDMETHOD(RuntimeStarted)(DWORD code);	
    STDMETHOD_(INT_PTR,GetCurrentThreadType)(VOID);
    STDMETHOD_(INT_PTR,GetCurrentThreadLockCount) (VOID);        
    STDMETHOD_(BOOL,IsPreemptiveGC)(VOID);        
    STDMETHOD_(BOOL,ThreadCanBeAborted) (VOID);
    STDMETHOD(ImageMapped(LPCWSTR wszPath, LPCVOID pBaseAddress,DWORD flags));    
    STDMETHOD(HasNativeImage)(LPVOID domainfile,BOOL* pHasNativeImage);	
};
#else
#define TESTHOOKCALL(function) 
#define TESTHOOKENABLED FALSE
#define TESTHOOKSLOWPATH FALSE
#endif
#endif
