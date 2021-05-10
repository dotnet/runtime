// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// DomainFile.h
//

// --------------------------------------------------------------------------------


#ifndef _DOMAINFILE_H_
#define _DOMAINFILE_H_

// --------------------------------------------------------------------------------
// Required headers
// --------------------------------------------------------------------------------

// --------------------------------------------------------------------------------
// Forward class declarations
// --------------------------------------------------------------------------------
class AppDomain;
class DomainAssembly;
class DomainModule;
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
// DomainFile represents a file loaded (or being loaded) into an app domain.  It
// is guranteed to be unique per file per app domain.
// --------------------------------------------------------------------------------

class DomainFile
{
    VPTR_BASE_VTABLE_CLASS(DomainFile);

  public:

    // ------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------

#ifndef DACCESS_COMPILE
    virtual ~DomainFile();
    DomainFile() {LIMITED_METHOD_CONTRACT;};
#endif

    virtual LoaderAllocator *GetLoaderAllocator();

    PTR_AppDomain GetAppDomain()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_pDomain;
    }

    PEFile *GetFile()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pFile;
    }

    PEFile *GetOriginalFile()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pOriginalFile!= NULL ? m_pOriginalFile : m_pFile;
    }


    IMDInternalImport *GetMDImport()
    {
        WRAPPER_NO_CONTRACT;
        return m_pFile->GetPersistentMDImport();
    }

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
        return GetFile()->IsSystem();
    }

    LPCUTF8 GetSimpleName()
    {
        WRAPPER_NO_CONTRACT;
        return GetFile()->GetSimpleName();
    }

#ifdef LOGGING
    LPCWSTR GetDebugName()
    {
        WRAPPER_NO_CONTRACT;
        return GetFile()->GetDebugName();
    }
#endif

    virtual BOOL IsAssembly() = 0;

    DomainAssembly *GetDomainAssembly();

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

    // AttemptLoadLevel is a generic routine used to try to further load the file to a given level.
    // No guarantee is made about the load level resulting however.
    void AttemptLoadLevel(FileLoadLevel targetLevel) DAC_EMPTY();

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
    BOOL Equals(DomainFile *pFile) { WRAPPER_NO_CONTRACT; return GetFile()->Equals(pFile->GetFile()); }
    BOOL Equals(PEFile *pFile) { WRAPPER_NO_CONTRACT; return GetFile()->Equals(pFile); }
#endif // DACCESS_COMPILE

    Module* GetCurrentModule();
    Module* GetLoadedModule();
    Module* GetModule();

#ifdef FEATURE_PREJIT
    BOOL IsZapRequired(); // Are we absolutely required to use a native image?
#endif
    // The format string is intentionally unicode to avoid globalization bugs
#ifdef FEATURE_PREJIT
    void ExternalLog(DWORD level, const WCHAR *fmt, ...);
    void ExternalLog(DWORD level, const char *msg);
#endif
#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

#ifndef DACCESS_COMPILE
    // light code gen. Keep the list of MethodTables needed for creating dynamic methods
    DynamicMethodTable* GetDynamicMethodTable();
#endif

 protected:
    // ------------------------------------------------------------
    // Loader API
    // ------------------------------------------------------------

    friend class AppDomain;
    friend class Assembly;
    friend class Module;
    friend class FileLoadLock;

    DomainFile(AppDomain *pDomain, PEFile *pFile);

    BOOL DoIncrementalLoad(FileLoadLevel targetLevel);
    void ClearLoading() { LIMITED_METHOD_CONTRACT; m_loading = FALSE; }
    void SetLoadLevel(FileLoadLevel level) { LIMITED_METHOD_CONTRACT; m_level = level; }

#ifndef DACCESS_COMPILE
    virtual void Begin() = 0;
    virtual void Allocate() = 0;
    void AddDependencies();
    void PreLoadLibrary();
    void LoadLibrary();
    void PostLoadLibrary();
    void EagerFixups();
    void VtableFixups();
    virtual void DeliverSyncEvents() = 0;
    virtual void DeliverAsyncEvents() = 0;
    void FinishLoad();
    void Activate();
#endif

    // This should be used to permanently set the load to fail. Do not use with transient conditions
    void SetError(Exception *ex);

#ifdef FEATURE_PREJIT

#ifndef DACCESS_COMPILE
    virtual void FindNativeImage() = 0;
#endif
    void VerifyNativeImageDependencies(bool verifyOnly = FALSE);

    // Are we absolutely required to use a native image?
    void CheckZapRequired();

    void ClearNativeImageStress();

#endif // FEATURE_PREJIT

    void SetProfilerNotified() { LIMITED_METHOD_CONTRACT; m_notifyflags|= PROFILER_NOTIFIED; }
    void SetDebuggerNotified() { LIMITED_METHOD_CONTRACT; m_notifyflags|=DEBUGGER_NOTIFIED; }
    void SetShouldNotifyDebugger() { LIMITED_METHOD_CONTRACT; m_notifyflags|=DEBUGGER_NEEDNOTIFICATION; }
#ifndef DACCESS_COMPILE
    void UpdatePEFileWorker(PTR_PEFile pFile);
#endif

    // ------------------------------------------------------------
    // Instance data
    // ------------------------------------------------------------

    PTR_AppDomain               m_pDomain;
    PTR_PEFile                  m_pFile;
    PTR_PEFile                  m_pOriginalFile;  // keep file alive just in case someone is sitill using it. If this is not NULL then m_pFile contains reused file from the shared assembly
    PTR_Module                  m_pModule;
    FileLoadLevel               m_level;
    LOADERHANDLE                m_hExposedModuleObject;

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
            Exception                   *m_pEx;
            HRESULT                     m_hr;
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
            if (m_type==ExType_ClrEx)
            {
                PAL_CPP_THROW(Exception *, m_pEx->DomainBoundClone());
            }
            if (m_type==ExType_HR)
                ThrowHR(m_hr);
            _ASSERTE(!"Bad exception type");
            ThrowHR(E_UNEXPECTED);
        };
        ExInfo(Exception* pEx)
        {
            LIMITED_METHOD_CONTRACT;
            m_type=ExType_ClrEx;
            m_pEx=pEx;
        };

        void ConvertToHResult()
        {
            LIMITED_METHOD_CONTRACT;
            if(m_type==ExType_HR)
                return;
            _ASSERTE(m_type==ExType_ClrEx);
            HRESULT hr=m_pEx->GetHR();
            delete m_pEx;
            m_hr=hr;
            m_type=ExType_HR;
        };
        ~ExInfo()
        {
            LIMITED_METHOD_CONTRACT;
            if (m_type==ExType_ClrEx)
                delete m_pEx;
        }
    }* m_pError;

    void ReleaseManagedData()
    {
        if (m_pError)
            m_pError->ConvertToHResult();
    };

#ifdef FEATURE_PREJIT
    // Lock-free enumeration of DomainFiles in an AppDomain.
public:
    DomainFile *FindNextDomainFileWithNativeImage();
private:
    void InsertIntoDomainFileWithNativeImageList();
#endif // FEATURE_PREJIT

    DWORD                    m_notifyflags;
    BOOL                        m_loading;
    // m_pDynamicMethodTable is used by the light code generation to allow method
    // generation on the fly. They are lazily created when/if a dynamic method is requested
    // for this specific module
    DynamicMethodTable          *m_pDynamicMethodTable;
    class UMThunkHash *m_pUMThunkHash;
    BOOL m_bDisableActivationCheck;

    // This value is to make it easier to diagnose Assembly Loader "rejected native image" crashes.
    // See Dev11 bug 358184 for more details
public:
    DWORD m_dwReasonForRejectingNativeImage; // See code:g_dwLoaderReasonForNotSharing in Assembly.cpp for a similar variable.
private:

#ifdef FEATURE_PREJIT
    // This value is to allow lock-free enumeration of all native images in an AppDomain
    Volatile<DomainFile *> m_pNextDomainFileWithNativeImage;
#endif
};

// These will sometimes result in a crash with error code 0x80131506 COR_E_EXECUTIONENGINE
// "An internal error happened in the Common Language Runtime's Execution Engine"
// Cause: Incorrectly committed to using native image for <path to assembly>
enum ReasonForRejectingNativeImage
{
    ReasonForRejectingNativeImage_NoNiForManifestModule = 0x101,
    ReasonForRejectingNativeImage_DependencyNotNative = 0x102,
    ReasonForRejectingNativeImage_CoreLibNotNative = 0x103,
    ReasonForRejectingNativeImage_FailedSecurityCheck = 0x104,
    ReasonForRejectingNativeImage_DependencyIdentityMismatch = 0x105,
    ReasonForRejectingNativeImage_CannotShareNiAssemblyNotDomainNeutral = 0x106,
    ReasonForRejectingNativeImage_NiAlreadyUsedInAnotherSharedAssembly = 0x107,
};

//---------------------------------------------------------------------------------------
// One of these values is specified when requesting a module iterator to customize which
// modules should appear in the enumeration
enum ModuleIterationOption
{
    // include only modules that are already loaded (m_level >= FILE_LOAD_DELIVER_EVENTS)
    kModIterIncludeLoaded                = 1,

    // include all modules, even those that are still in the process of loading (all m_level values)
    kModIterIncludeLoading               = 2,

    // include only modules loaded just enough that profilers are notified of them.
    // (m_level >= FILE_LOAD_LOADLIBRARY).  See comment at code:DomainFile::IsAvailableToProfilers
    kModIterIncludeAvailableToProfilers  = 3,
};

// --------------------------------------------------------------------------------
// DomainAssembly is a subclass of DomainFile which specifically represents a assembly.
// --------------------------------------------------------------------------------

class DomainAssembly : public DomainFile
{
    VPTR_VTABLE_CLASS(DomainAssembly, DomainFile);

public:
    // ------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------

    PEAssembly *GetFile()
    {
        LIMITED_METHOD_CONTRACT;
        return PTR_PEAssembly(m_pFile);
    }

    LoaderAllocator *GetLoaderAllocator()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pLoaderAllocator;
    }

    // Finds only loaded hmods
    DomainFile *FindIJWModule(HMODULE hMod);

    void SetAssembly(Assembly* pAssembly);

    BOOL IsAssembly()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return TRUE;
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

    Assembly* GetCurrentAssembly();
    Assembly* GetLoadedAssembly();
    Assembly* GetAssembly();

#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    // ------------------------------------------------------------
    // Modules
    // ------------------------------------------------------------
    class ModuleIterator
    {
        ArrayList::Iterator m_i;
        ModuleIterationOption m_moduleIterationOption;

      public:
        BOOL Next()
        {
            WRAPPER_NO_CONTRACT;
            while (m_i.Next())
            {
                if (m_i.GetElement() == NULL)
                {
                    continue;
                }
                if (GetDomainFile()->IsError())
                {
                    continue;
                }
                if (m_moduleIterationOption == kModIterIncludeLoading)
                    return TRUE;
                if ((m_moduleIterationOption == kModIterIncludeLoaded) &&
                    GetDomainFile()->IsLoaded())
                    return TRUE;
                if ((m_moduleIterationOption == kModIterIncludeAvailableToProfilers) &&
                    GetDomainFile()->IsAvailableToProfilers())
                    return TRUE;
            }
            return FALSE;
        }
        Module *GetModule()
        {
            WRAPPER_NO_CONTRACT;
            return GetDomainFile()->GetModule();
        }
        Module  *GetLoadedModule()
        {
            WRAPPER_NO_CONTRACT;
            return GetDomainFile()->GetLoadedModule();
        }
        DomainFile *GetDomainFile()
        {
            WRAPPER_NO_CONTRACT;
            return dac_cast<PTR_DomainFile>(m_i.GetElement());
        }
        SIZE_T GetIndex()
        {
            WRAPPER_NO_CONTRACT;
            return m_i.GetIndex();
        }

      private:
        friend class DomainAssembly;
        // Cannot have constructor so this iterator can be used inside a union
        static ModuleIterator Create(DomainAssembly * pDomainAssembly, ModuleIterationOption moduleIterationOption)
        {
            WRAPPER_NO_CONTRACT;
            ModuleIterator i;

            i.m_i = pDomainAssembly->m_Modules.Iterate();
            i.m_moduleIterationOption = moduleIterationOption;

            return i;
        }
    };
    friend class ModuleIterator;

    ModuleIterator IterateModules(ModuleIterationOption moduleIterationOption)
    {
        WRAPPER_NO_CONTRACT;
        return ModuleIterator::Create(this, moduleIterationOption);
    }

    DomainFile *LookupDomainFile(DWORD index)
    {
        WRAPPER_NO_CONTRACT;
        if (index >= m_Modules.GetCount())
            return NULL;
        else
            return dac_cast<PTR_DomainFile>(m_Modules.Get(index));
    }

    Module *LookupModule(DWORD index)
    {
        WRAPPER_NO_CONTRACT;
        DomainFile *pModule = LookupDomainFile(index);
        if (pModule == NULL)
            return NULL;
        else
            return pModule->GetModule();
    }


    // ------------------------------------------------------------
    // Resource access
    // ------------------------------------------------------------

    BOOL GetResource(LPCSTR szName, DWORD *cbResource,
                     PBYTE *pbInMemoryResource, DomainAssembly** pAssemblyRef,
                     LPCSTR *szFileName, DWORD *dwLocation,
                     BOOL fSkipRaiseResolveEvent);

#ifdef FEATURE_PREJIT
    // ------------------------------------------------------------
    // Prejitting API
    // ------------------------------------------------------------

    void GetCurrentVersionInfo(CORCOMPILE_VERSION_INFO *pZapVersionInfo);

    void GetOptimizedIdentitySignature(CORCOMPILE_ASSEMBLY_SIGNATURE *pSignature);
    BOOL CheckZapDependencyIdentities(PEImage *pNativeImage);

#endif // FEATURE_PREJIT

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

    HRESULT GetDebuggingCustomAttributes(DWORD *pdwFlags);

    BOOL IsVisibleToDebugger();
    BOOL NotifyDebuggerLoad(int flags, BOOL attaching);
    void NotifyDebuggerUnload();

    inline BOOL IsCollectible();


 private:

    // ------------------------------------------------------------
    // Loader API
    // ------------------------------------------------------------

    friend class AppDomain;
    friend class Assembly;
    friend class AssemblyNameNative;

#ifndef DACCESS_COMPILE
public:
    ~DomainAssembly();
private:
    DomainAssembly(AppDomain *pDomain, PEFile *pFile, LoaderAllocator *pLoaderAllocator);
#endif

    // ------------------------------------------------------------
    // Internal routines
    // ------------------------------------------------------------

    void SetSecurityError(Exception *ex);

#ifndef DACCESS_COMPILE
    void Begin();
    void Allocate();
    void LoadSharers();
    void DeliverSyncEvents();
    void DeliverAsyncEvents();
#endif

    void UpdatePEFile(PTR_PEFile pFile);

#ifdef FEATURE_PREJIT
#ifndef DACCESS_COMPILE
    void FindNativeImage();
#endif
#endif // FEATURE_PREJIT

    BOOL IsInstrumented();

 public:
    ULONG HashIdentity();

    // ------------------------------------------------------------
    // Instance data
    // ------------------------------------------------------------

  private:
    LOADERHANDLE                            m_hExposedAssemblyObject;
    PTR_Assembly                            m_pAssembly;
    DebuggerAssemblyControlFlags            m_debuggerFlags;
    ArrayList                               m_Modules;
    BOOL                                    m_fDebuggerUnloadStarted;
    BOOL                                    m_fCollectible;
    Volatile<bool>                          m_fHostAssemblyPublished;
    PTR_LoaderAllocator                     m_pLoaderAllocator;
    DomainAssembly*                         m_NextDomainAssemblyInSameALC;

  public:
      DomainAssembly* GetNextDomainAssemblyInSameALC()
      {
          return m_NextDomainAssemblyInSameALC;
      }

      void SetNextDomainAssemblyInSameALC(DomainAssembly* domainAssembly)
      {
          _ASSERTE(m_NextDomainAssemblyInSameALC == NULL);
          m_NextDomainAssemblyInSameALC = domainAssembly;
      }
};

typedef DomainAssembly::ModuleIterator DomainModuleIterator;

// --------------------------------------------------------------------------------
// DomainModule is a subclass of DomainFile which specifically represents a module.
// --------------------------------------------------------------------------------
#endif  // _DOMAINFILE_H_
