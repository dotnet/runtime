// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// DomainAssembly.h
//

// --------------------------------------------------------------------------------


#ifndef _DOMAINASSEMBLY_H_
#define _DOMAINASSEMBLY_H_

// --------------------------------------------------------------------------------
// Required headers
// --------------------------------------------------------------------------------

// --------------------------------------------------------------------------------
// Forward class declarations
// --------------------------------------------------------------------------------
class AppDomain;
class DomainAssembly;
class Assembly;
class Module;
class DynamicMethodTable;

enum FileLoadLevel
{
    // These states are tracked by FileLoadLock

    // Note: This enum must match the static array fileLoadLevelName[]
    //       which contains the printable names of the enum values

    // Note that semantics here are description is the LAST step done, not what is
    // currently being done.

    FILE_LOAD_CREATE,
    FILE_LOAD_BEGIN,
    FILE_LOAD_FIND_NATIVE_IMAGE,
    FILE_LOAD_VERIFY_NATIVE_IMAGE_DEPENDENCIES,
    FILE_LOAD_ALLOCATE,
    FILE_LOAD_ADD_DEPENDENCIES,
    FILE_LOAD_PRE_LOADLIBRARY,
    FILE_LOAD_LOADLIBRARY,
    FILE_LOAD_POST_LOADLIBRARY,
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

// --------------------------------------------------------------------------------
// DomainAssembly represents an assembly loaded (or being loaded) into an app domain.  It
// is guaranteed to be unique per file per app domain.
// --------------------------------------------------------------------------------

class DomainAssembly final
{
public:

    // ------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------

#ifndef DACCESS_COMPILE
    ~DomainAssembly();
    DomainAssembly() {LIMITED_METHOD_CONTRACT;};
#endif

    PEAssembly *GetPEAssembly()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return PTR_PEAssembly(m_pPEAssembly);
    }

    Assembly* GetAssembly()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        CONSISTENCY_CHECK(CheckLoaded());

        return m_pAssembly;
    }

    Module* GetModule()
    {
        LIMITED_METHOD_CONTRACT;
        CONSISTENCY_CHECK(CheckLoaded());

        return m_pModule;
    }

    IMDInternalImport *GetMDImport()
    {
        WRAPPER_NO_CONTRACT;
        return m_pPEAssembly->GetMDImport();
    }

    OBJECTREF GetExposedAssemblyObjectIfExists()
    {
        LIMITED_METHOD_CONTRACT;

        OBJECTREF objRet = NULL;
        GET_LOADERHANDLE_VALUE_FAST(GetLoaderAllocator(), m_hExposedAssemblyObject, &objRet);
        return objRet;
    }

    // Returns managed representation of the assembly (Assembly or AssemblyBuilder).
    // Returns NULL if the managed scout was already collected (see code:LoaderAllocator#AssemblyPhases).
    OBJECTREF GetExposedAssemblyObject();

    OBJECTREF GetExposedModuleObjectIfExists()
    {
        LIMITED_METHOD_CONTRACT;

        OBJECTREF objRet = NULL;
        GET_LOADERHANDLE_VALUE_FAST(GetLoaderAllocator(), m_hExposedModuleObject, &objRet);
        return objRet;
    }

    OBJECTREF GetExposedModuleObject();

    BOOL IsSystem()
    {
        WRAPPER_NO_CONTRACT;
        return GetPEAssembly()->IsSystem();
    }

    LPCUTF8 GetSimpleName()
    {
        WRAPPER_NO_CONTRACT;
        return GetPEAssembly()->GetSimpleName();
    }

#ifdef LOGGING
    LPCUTF8 GetDebugName()
    {
        WRAPPER_NO_CONTRACT;
        return GetPEAssembly()->GetDebugName();
    }
#endif

    BOOL IsCollectible()
    {
        LIMITED_METHOD_CONTRACT;
        return m_fCollectible;
    }

    // ------------------------------------------------------------
    // Loading state checks
    // ------------------------------------------------------------

    // Return the File's load level.  Note that this is the last level actually successfully completed.
    // Note that this is subtly different than the FileLoadLock's level, which is the last level
    // which was triggered (but potentially skipped if error or inappropriate.)
    FileLoadLevel GetLoadLevel() { LIMITED_METHOD_DAC_CONTRACT; return m_level; }

    // Error means that a permanent x-appdomain load error has occurred.
    BOOL IsError()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        DACCOP_IGNORE(FieldAccess, "No marshalling required");
        return m_pError != NULL;
    }

    // Loading means that the load is still being tracked by a FileLoadLock.
    BOOL IsLoading() { LIMITED_METHOD_CONTRACT; return m_loading; }

    // Loaded means that the file can be used passively.  This includes loading types, reflection, and
    // jitting.
    BOOL IsLoaded() { LIMITED_METHOD_DAC_CONTRACT; return m_level >= FILE_LOAD_DELIVER_EVENTS; }

    // Active means that the file can be used actively in the current app domain.  Note that a shared file
    // may conditionally not be able to be made active on a per app domain basis.
    BOOL IsActive() { LIMITED_METHOD_CONTRACT; return m_level >= FILE_ACTIVE; }

    // Checks if the load has reached the point where profilers may be notified
    // about the file. It's important that IF a profiler is notified, THEN this returns
    // TRUE, otherwise there can be profiler-attach races where the profiler doesn't see
    // the file via either enumeration or notification. As a result, this begins
    // returning TRUE just before the profiler is actually notified.  See
    // code:ProfilerFunctionEnum::Init#ProfilerEnumAssemblies
    BOOL IsAvailableToProfilers()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return IsProfilerNotified(); // despite the name, this function returns TRUE just before we notify the profiler
    }

    // CheckLoaded is appropriate for asserts that the assembly can be passively used.
    CHECK CheckLoaded();

    // CheckActivated is appropriate for asserts that the assembly can be actively used.  Note that
    // it is slightly different from IsActive in that it deals with reentrancy cases properly.
    CHECK CheckActivated();

    // Ensure that an assembly has reached at least the IsLoaded state.  Throw if not.
    void EnsureLoaded()
    {
        WRAPPER_NO_CONTRACT;
        return EnsureLoadLevel(FILE_LOADED);
    }

    // Ensure that an assembly has reached at least the IsActive state.  Throw if not.
    void EnsureActive()
    {
        WRAPPER_NO_CONTRACT;
        return EnsureLoadLevel(FILE_ACTIVE);
    }

    // Ensure that an assembly has reached at least the Allocated state.  Throw if not.
    void EnsureAllocated()
    {
        WRAPPER_NO_CONTRACT;
        return EnsureLoadLevel(FILE_LOAD_ALLOCATE);
    }

    void EnsureLibraryLoaded()
    {
        WRAPPER_NO_CONTRACT;
        return EnsureLoadLevel(FILE_LOAD_LOADLIBRARY);
    }

    // EnsureLoadLevel is a generic routine used to ensure that the file is not in a delay loaded
    // state (unless it needs to be.)  This should be used when a particular level of loading
    // is required for an operation.  Note that deadlocks are tolerated so the level may be one
    void EnsureLoadLevel(FileLoadLevel targetLevel) DAC_EMPTY();

    // CheckLoadLevel is an assert predicate used to verify the load level of an assembly.
    // deadlockOK indicates that the level is allowed to be one short if we are restricted
    // by loader reentrancy.
    CHECK CheckLoadLevel(FileLoadLevel requiredLevel, BOOL deadlockOK = TRUE) DAC_EMPTY_RET(CHECK::OK());

    // RequireLoadLevel throws an exception if the domain file isn't loaded enough.  Note
    // that this is intolerant of deadlock related failures so is only really appropriate for
    // checks inside the main loading loop.
    void RequireLoadLevel(FileLoadLevel targetLevel) DAC_EMPTY();

    // Throws if a load error has occurred
    void ThrowIfError(FileLoadLevel targetLevel) DAC_EMPTY();

    // Checks that a load error has not occurred before the given level
    CHECK CheckNoError(FileLoadLevel targetLevel) DAC_EMPTY_RET(CHECK::OK());

    // IsNotified means that the profiler API notification has been delivered
    BOOL IsProfilerNotified() { LIMITED_METHOD_CONTRACT; return m_notifyflags & PROFILER_NOTIFIED; }
    BOOL IsDebuggerNotified() { LIMITED_METHOD_CONTRACT; return m_notifyflags & DEBUGGER_NOTIFIED; }
    BOOL ShouldNotifyDebugger() { LIMITED_METHOD_CONTRACT; return m_notifyflags & DEBUGGER_NEEDNOTIFICATION; }


    // ------------------------------------------------------------
    // Other public APIs
    // ------------------------------------------------------------

#ifndef DACCESS_COMPILE
    BOOL Equals(DomainAssembly *pAssembly) { WRAPPER_NO_CONTRACT; return GetPEAssembly()->Equals(pAssembly->GetPEAssembly()); }
    BOOL Equals(PEAssembly *pPEAssembly) { WRAPPER_NO_CONTRACT; return GetPEAssembly()->Equals(pPEAssembly); }
#endif // DACCESS_COMPILE

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

#ifndef DACCESS_COMPILE
    // light code gen. Keep the list of MethodTables needed for creating dynamic methods
    DynamicMethodTable* GetDynamicMethodTable();
#endif

    DomainAssembly* GetNextDomainAssemblyInSameALC()
    {
        return m_NextDomainAssemblyInSameALC;
    }

    void SetNextDomainAssemblyInSameALC(DomainAssembly* domainAssembly)
    {
        _ASSERTE(m_NextDomainAssemblyInSameALC == NULL);
        m_NextDomainAssemblyInSameALC = domainAssembly;
    }

    LoaderAllocator* GetLoaderAllocator()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pLoaderAllocator;
    }

// ------------------------------------------------------------
// Resource access
// ------------------------------------------------------------

    BOOL GetResource(LPCSTR szName, DWORD* cbResource,
        PBYTE* pbInMemoryResource, DomainAssembly** pAssemblyRef,
        LPCSTR* szFileName, DWORD* dwLocation,
        BOOL fSkipRaiseResolveEvent);

 private:
    // ------------------------------------------------------------
    // Loader API
    // ------------------------------------------------------------

    friend class AppDomain;
    friend class Assembly;
    friend class Module;
    friend class FileLoadLock;

    DomainAssembly(AppDomain* pDomain, PEAssembly* pPEAssembly, LoaderAllocator* pLoaderAllocator);

    BOOL DoIncrementalLoad(FileLoadLevel targetLevel);
    void ClearLoading() { LIMITED_METHOD_CONTRACT; m_loading = FALSE; }
    void SetLoadLevel(FileLoadLevel level) { LIMITED_METHOD_CONTRACT; m_level = level; }

#ifndef DACCESS_COMPILE
    void Begin();
    void Allocate();
    void AddDependencies();
    void PreLoadLibrary();
    void LoadLibrary();
    void PostLoadLibrary();
    void EagerFixups();
    void VtableFixups();
    void DeliverSyncEvents();
    void DeliverAsyncEvents();
    void FinishLoad();
    void Activate();

    void RegisterWithHostAssembly();
    void UnregisterFromHostAssembly();
#endif

    // This should be used to permanently set the load to fail. Do not use with transient conditions
    void SetError(Exception *ex);
    void SetAssembly(Assembly* pAssembly);

    void SetProfilerNotified() { LIMITED_METHOD_CONTRACT; m_notifyflags|= PROFILER_NOTIFIED; }
    void SetDebuggerNotified() { LIMITED_METHOD_CONTRACT; m_notifyflags|=DEBUGGER_NOTIFIED; }
    void SetShouldNotifyDebugger() { LIMITED_METHOD_CONTRACT; m_notifyflags|=DEBUGGER_NEEDNOTIFICATION; }

    class ExInfo
    {
        enum
        {
            ExType_ClrEx,
            ExType_HR
        }
        m_type;
        union
        {
            Exception* m_pEx;
            HRESULT    m_hr;
        };

    public:
        void Throw()
        {
            CONTRACTL
            {
                THROWS;
                GC_TRIGGERS;
                MODE_ANY;
            }
            CONTRACTL_END;
            if (m_type == ExType_ClrEx)
            {
                PAL_CPP_THROW(Exception*, m_pEx->DomainBoundClone());
            }
            if (m_type == ExType_HR)
                ThrowHR(m_hr);
            _ASSERTE(!"Bad exception type");
            ThrowHR(E_UNEXPECTED);
        };

        ExInfo(Exception* pEx)
        {
            LIMITED_METHOD_CONTRACT;
            m_type = ExType_ClrEx;
            m_pEx = pEx;
        };

        ~ExInfo()
        {
            LIMITED_METHOD_CONTRACT;
            if (m_type == ExType_ClrEx)
                delete m_pEx;
        }
    };

public:
// ------------------------------------------------------------
// Debugger control API
// ------------------------------------------------------------

    DebuggerAssemblyControlFlags GetDebuggerInfoBits(void)
    {
        LIMITED_METHOD_CONTRACT;
        return m_debuggerFlags;
    }

    void SetDebuggerInfoBits(DebuggerAssemblyControlFlags newBits)
    {
        LIMITED_METHOD_CONTRACT;
        m_debuggerFlags = newBits;
    }

    void SetupDebuggingConfig(void);
    DWORD ComputeDebuggingConfig(void);

    HRESULT GetDebuggingCustomAttributes(DWORD* pdwFlags);

    BOOL IsVisibleToDebugger();
    BOOL NotifyDebuggerLoad(int flags, BOOL attaching);
    void NotifyDebuggerUnload();

private:
    // ------------------------------------------------------------
    // Instance data
    // ------------------------------------------------------------

    PTR_Assembly                m_pAssembly;
    PTR_PEAssembly              m_pPEAssembly;
    PTR_Module                  m_pModule;

    BOOL                        m_fCollectible;
    DomainAssembly*             m_NextDomainAssemblyInSameALC;
    PTR_LoaderAllocator         m_pLoaderAllocator;

    FileLoadLevel               m_level;
    BOOL                        m_loading;

    LOADERHANDLE                m_hExposedModuleObject;
    LOADERHANDLE                m_hExposedAssemblyObject;

    ExInfo*                     m_pError;

    BOOL                        m_bDisableActivationCheck;
    BOOL                        m_fHostAssemblyPublished;

    // m_pDynamicMethodTable is used by the light code generation to allow method
    // generation on the fly. They are lazily created when/if a dynamic method is requested
    // for this specific module
    DynamicMethodTable*         m_pDynamicMethodTable;


    DebuggerAssemblyControlFlags    m_debuggerFlags;
    DWORD                       m_notifyflags;
    BOOL                        m_fDebuggerUnloadStarted;
};

#endif  // _DOMAINASSEMBLY_H_
