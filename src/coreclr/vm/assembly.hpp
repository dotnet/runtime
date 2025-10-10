// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header:  Assembly.hpp
**
** Purpose: Implements assembly (loader domain) architecture
**
===========================================================*/

#ifndef _ASSEMBLY_H
#define _ASSEMBLY_H

#include "ceeload.h"
#include "exceptmacros.h"
#include "clsload.hpp"
#include "eehash.h"
#include "listlock.h"
#include "iceefilegen.h"
#include "cordbpriv.h"
#include "assemblyspec.hpp"

class FriendAssemblyDescriptor;

// Bits in m_dwDynamicAssemblyAccess (see System.Reflection.Emit.AssemblyBuilderAccess.cs)
#define ASSEMBLY_ACCESS_RUN     0x01
#define ASSEMBLY_ACCESS_SAVE    0x02
#define ASSEMBLY_ACCESS_COLLECT 0x8

// An assembly is the unit of deployment for managed code.
// Assemblies are one to one with files since coreclr does not support multimodule assemblies.
//
class Assembly
{
    friend class ClassLoader;
    friend class AssemblyNative;
    friend class AssemblySpec;
    friend class ClrDataAccess;

private:
    Assembly(PEAssembly *pPEAssembly, LoaderAllocator* pLoaderAllocator);
    void Init(AllocMemTracker *pamTracker);

// Load state tracking
public:
    // Return the File's load level.  Note that this is the last level actually successfully completed.
    // Note that this is subtly different than the FileLoadLock's level, which is the last level
    // which was triggered (but potentially skipped if error or inappropriate.)
    FileLoadLevel GetLoadLevel() { LIMITED_METHOD_DAC_CONTRACT; return m_level; }

    // Error means that a permanent load error has occurred.
    bool IsError()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        DACCOP_IGNORE(FieldAccess, "No marshalling required");
        return m_pError != NULL;
    }

    // Loading means that the load is still being tracked by a FileLoadLock.
    bool IsLoading() { LIMITED_METHOD_CONTRACT; return m_isLoading; }

    // Loaded means that the file can be used passively. This includes loading types, reflection,
    // and jitting.
    bool IsLoaded() { LIMITED_METHOD_DAC_CONTRACT; return m_level >= FILE_LOAD_DELIVER_EVENTS; }

    // Active means that the file can be used actively. This includes code execution, static field
    // access, and instance allocation.
    bool IsActive() { LIMITED_METHOD_CONTRACT; return m_level >= FILE_ACTIVE; }

    // Checks if the load has reached the point where profilers may be notified
    // about the file. It's important that IF a profiler is notified, THEN this returns
    // TRUE, otherwise there can be profiler-attach races where the profiler doesn't see
    // the file via either enumeration or notification. As a result, this begins
    // returning TRUE just before the profiler is actually notified.  See
    // code:ProfilerFunctionEnum::Init#ProfilerEnumAssemblies
    bool IsAvailableToProfilers()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return IsProfilerNotified(); // despite the name, this function returns TRUE just before we notify the profiler
    }

    BOOL DoIncrementalLoad(FileLoadLevel targetLevel);

    void ClearLoading() { LIMITED_METHOD_CONTRACT; m_isLoading = false; }
    void SetLoadLevel(FileLoadLevel level) { LIMITED_METHOD_CONTRACT; m_level = level; }

    BOOL NotifyDebuggerLoad(int flags, BOOL attaching);
    void NotifyDebuggerUnload();

    // Ensure that an assembly has reached at least the IsActive state. Throw if not
    void EnsureActive()
    {
        WRAPPER_NO_CONTRACT;
        return EnsureLoadLevel(FILE_ACTIVE);
    }

    // CheckActivated is appropriate for asserts that the assembly can be actively used.
    // Note that this is slightly different from IsActive in that it deals with reentrancy cases properly.
    CHECK CheckActivated();

    // EnsureLoadLevel is a generic routine used to ensure that the file is not in a delay loaded
    // state (unless it needs to be.)  This should be used when a particular level of loading
    // is required for an operation.  Note that deadlocks are tolerated so the level may be one
    void EnsureLoadLevel(FileLoadLevel targetLevel) DAC_EMPTY();

    // RequireLoadLevel throws an exception if the domain file isn't loaded enough.  Note
    // that this is intolerant of deadlock related failures so is only really appropriate for
    // checks inside the main loading loop.
    void RequireLoadLevel(FileLoadLevel targetLevel) DAC_EMPTY();

    // This should be used to permanently set the load to fail. Do not use with transient conditions
    void SetError(Exception *ex);

    // Throws if a load error has occurred
    void ThrowIfError(FileLoadLevel targetLevel) DAC_EMPTY();

    // Checks that a load error has not occurred before the given level
    CHECK CheckNoError(FileLoadLevel targetLevel) DAC_EMPTY_RET(CHECK::OK());

private:
    friend class AppDomain;
    friend class FileLoadLock;

#ifndef DACCESS_COMPILE
    void Begin();
    void BeforeTypeLoad();
    void EagerFixups();
#ifdef FEATURE_IJW
    void VtableFixups();
#endif // FEATURE_IJW
    void DeliverSyncEvents();
    void DeliverAsyncEvents();
    void FinishLoad();
    void Activate();

    void RegisterWithHostAssembly();
    void UnregisterFromHostAssembly();
#endif

    void SetProfilerNotified() { LIMITED_METHOD_CONTRACT; m_notifyFlags |= PROFILER_NOTIFIED; }
    void SetDebuggerNotified() { LIMITED_METHOD_CONTRACT; m_notifyFlags |= DEBUGGER_NOTIFIED; }
    void SetShouldNotifyDebugger() { LIMITED_METHOD_CONTRACT; m_notifyFlags |= DEBUGGER_NEEDNOTIFICATION; }

    // IsNotified means that the profiler API notification has been delivered
    bool IsProfilerNotified() { LIMITED_METHOD_CONTRACT; return (m_notifyFlags & PROFILER_NOTIFIED) == PROFILER_NOTIFIED; }
    bool IsDebuggerNotified() { LIMITED_METHOD_CONTRACT; return (m_notifyFlags & DEBUGGER_NOTIFIED) == DEBUGGER_NOTIFIED; }
    bool ShouldNotifyDebugger() { LIMITED_METHOD_CONTRACT; return (m_notifyFlags & DEBUGGER_NEEDNOTIFICATION) == DEBUGGER_NEEDNOTIFICATION; }

    // CheckLoadLevel is an assert predicate used to verify the load level of an assembly.
    // deadlockOK indicates that the level is allowed to be one short if we are restricted
    // by loader reentrancy.
    CHECK CheckLoadLevel(FileLoadLevel requiredLevel, BOOL deadlockOK = TRUE) DAC_EMPTY_RET(CHECK::OK());

public:
    void StartUnload();
    void Terminate( BOOL signalProfiler = TRUE );

    static Assembly *Create(PEAssembly *pPEAssembly, AllocMemTracker *pamTracker, LoaderAllocator *pLoaderAllocator);
    static void Initialize();

    BOOL IsSystem() { WRAPPER_NO_CONTRACT; return m_pPEAssembly->IsSystem(); }

    static Assembly* CreateDynamic(AssemblyBinder* pBinder, NativeAssemblyNameParts* pAssemblyNameParts, INT32 hashAlgorithm, INT32 access, LOADERALLOCATORREF* pKeepAlive);

    MethodDesc *GetEntryPoint();

    //****************************************************************************************
    //
    // Additional init tasks for Modules. This should probably be part of Module::Initialize()
    // but there's at least one call to ReflectionModule::Create that is *not* followed by a
    // PrepareModule call.
    void PrepareModuleForAssembly(Module* module, AllocMemTracker *pamTracker);

#ifndef DACCESS_COMPILE
    void SetIsTenured()
    {
        WRAPPER_NO_CONTRACT;
        m_pModule->SetIsTenured();
    }
#endif // DACCESS_COMPILE

    //****************************************************************************************
    //
    // Returns the class loader associated with the assembly.
    ClassLoader* GetLoader()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_pClassLoader;
    }

    PTR_LoaderAllocator GetLoaderAllocator() { LIMITED_METHOD_DAC_CONTRACT; return m_pLoaderAllocator; }

#ifdef LOGGING
    LPCUTF8 GetDebugName()
    {
        WRAPPER_NO_CONTRACT;
        return GetPEAssembly()->GetDebugName();
    }
#endif

    LPCUTF8 GetSimpleName()
    {
        WRAPPER_NO_CONTRACT;
        return GetPEAssembly()->GetSimpleName();
    }

    BOOL IsStrongNamed()
    {
        WRAPPER_NO_CONTRACT;
        return GetPEAssembly()->IsStrongNamed();
    }

    const void *GetPublicKey(DWORD *pcbPK)
    {
        WRAPPER_NO_CONTRACT;
        return GetPEAssembly()->GetPublicKey(pcbPK);
    }

    ULONG GetHashAlgId()
    {
        WRAPPER_NO_CONTRACT;
        return GetPEAssembly()->GetHashAlgId();
    }

    HRESULT GetVersion(USHORT *pMajor, USHORT *pMinor, USHORT *pBuild, USHORT *pRevision)
    {
        WRAPPER_NO_CONTRACT;
        return GetPEAssembly()->GetVersion(pMajor, pMinor, pBuild, pRevision);
    }

    LPCUTF8 GetLocale()
    {
        WRAPPER_NO_CONTRACT;
        return GetPEAssembly()->GetLocale();
    }

    DWORD GetFlags()
    {
        WRAPPER_NO_CONTRACT;
        return GetPEAssembly()->GetFlags();
    }

    PTR_LoaderHeap GetLowFrequencyHeap();
    PTR_LoaderHeap GetHighFrequencyHeap();
    PTR_LoaderHeap GetStubHeap();

    PTR_Module GetModule()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_pModule;
    }

    PTR_PEAssembly GetPEAssembly()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_pPEAssembly;
    }

    IMDInternalImport* GetMDImport()
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return m_pPEAssembly->GetMDImport();
    }

    HRESULT GetCustomAttribute(mdToken parentToken,
                               WellKnownAttribute attribute,
                               const void  **ppData,
                               ULONG *pcbData)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return GetModule()->GetCustomAttribute(parentToken, attribute, ppData, pcbData);
    }

    mdAssembly GetManifestToken()
    {
        LIMITED_METHOD_CONTRACT;

        return TokenFromRid(1, mdtAssembly);
    }

#ifndef DACCESS_COMPILE
    void GetDisplayName(SString &result, DWORD flags = 0)
    {
        WRAPPER_NO_CONTRACT;

        return m_pPEAssembly->GetDisplayName(result, flags);
    }
#endif // DACCESS_COMPILE

    BOOL GetCodeBase(SString &result)
    {
        WRAPPER_NO_CONTRACT;

        return m_pPEAssembly->GetCodeBase(result);
    }

    OBJECTREF GetExposedObject();
    OBJECTREF GetExposedObjectIfExists();

    DebuggerAssemblyControlFlags GetDebuggerInfoBits(void)
    {
        LIMITED_METHOD_CONTRACT;

        return m_debuggerFlags;
    }

    void SetDebuggerInfoBits(DebuggerAssemblyControlFlags flags)
    {
        LIMITED_METHOD_CONTRACT;

        m_debuggerFlags = flags;
    }

    DomainAssembly* GetNextAssemblyInSameALC()
    {
        return m_NextAssemblyInSameALC;
    }

    void SetNextAssemblyInSameALC(DomainAssembly* assembly)
    {
        _ASSERTE(m_NextAssemblyInSameALC == NULL);
        m_NextAssemblyInSameALC = assembly;
    }

private:
    DebuggerAssemblyControlFlags ComputeDebuggingConfig(void);

#ifdef DEBUGGING_SUPPORTED
    HRESULT GetDebuggingCustomAttributes(DWORD* pdwFlags);
#endif
public:
    // On failure:
    //      if loadFlag == Loader::Load => throw
    //      if loadFlag != Loader::Load => return NULL
    Module *FindModuleByExportedType(mdExportedType mdType,
                                     Loader::LoadFlag loadFlag,
                                     mdTypeDef mdNested,
                                     mdTypeDef *pCL);

    static Module * FindModuleByTypeRef(ModuleBase *     pModule,
                                        mdTypeRef        typeRef,
                                        Loader::LoadFlag loadFlag,
                                        BOOL *           pfNoResolutionScope);

    //****************************************************************************************
    //
    INT32 ExecuteMainMethod(PTRARRAYREF *stringArgs, BOOL waitForOtherThreads);

    //****************************************************************************************

private:
    Assembly();

public:
    ~Assembly();

    BOOL GetResource(LPCSTR szName, DWORD *cbResource,
                    PBYTE *pbInMemoryResource, Assembly **pAssemblyRef,
                    LPCSTR *szFileName, DWORD *dwLocation);

    //****************************************************************************************
#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    FORCEINLINE bool IsDynamic() { LIMITED_METHOD_CONTRACT; return m_isDynamic; }
    FORCEINLINE bool IsCollectible() { LIMITED_METHOD_DAC_CONTRACT; return m_isCollectible; }

    void AddType(Module* pModule,
                 mdTypeDef cl);
    void AddExportedType(mdExportedType cl);
    mdAssemblyRef AddAssemblyRef(Assembly *refedAssembly, IMetaDataAssemblyEmit *pAssemEmitter);

    //****************************************************************************************

    DomainAssembly *GetDomainAssembly();
    void SetDomainAssembly(DomainAssembly *pAssembly);

#if defined(FEATURE_COLLECTIBLE_TYPES) && !defined(DACCESS_COMPILE)
    OBJECTHANDLE GetLoaderAllocatorObjectHandle() { WRAPPER_NO_CONTRACT; return GetLoaderAllocator()->GetLoaderAllocatorObjectHandle(); }
#endif // FEATURE_COLLECTIBLE_TYPES

#ifdef FEATURE_COMINTEROP
    static ITypeLib * const InvalidTypeLib;

    // Get any cached ITypeLib* for the assembly.
    ITypeLib *GetTypeLib();

    // Try to set the ITypeLib*, if one is not already cached.
    bool TrySetTypeLib(_In_ ITypeLib *pTlb);
#endif // FEATURE_COMINTEROP

#ifndef DACCESS_COMPILE

    void DECLSPEC_NORETURN ThrowTypeLoadException(LPCUTF8 pszFullName, UINT resIDWhy);

    void DECLSPEC_NORETURN ThrowTypeLoadException(LPCUTF8 pszNameSpace, LPCUTF8 pTypeName,
                                                  UINT resIDWhy);

    void DECLSPEC_NORETURN ThrowTypeLoadException(NameHandle *pName, UINT resIDWhy);

    void DECLSPEC_NORETURN ThrowTypeLoadException(IMDInternalImport *pInternalImport,
                                                  mdToken token,
                                                  UINT resIDWhy);

    void DECLSPEC_NORETURN ThrowTypeLoadException(IMDInternalImport *pInternalImport,
                                                  mdToken token,
                                                  LPCUTF8 pszFieldOrMethodName,
                                                  UINT resIDWhy);

    void DECLSPEC_NORETURN ThrowTypeLoadException(LPCUTF8 pszNameSpace,
                                                  LPCUTF8 pszTypeName,
                                                  LPCUTF8 pszMethodName,
                                                  UINT resIDWhy);

    void DECLSPEC_NORETURN ThrowBadImageException(LPCUTF8 pszNameSpace,
                                                  LPCUTF8 pszTypeName,
                                                  UINT resIDWhy);

#endif // #ifndef DACCESS_COMPILE

    //****************************************************************************************
    //

    static BOOL FileNotFound(HRESULT hr);

    //****************************************************************************************
    // Is the given assembly a friend of this assembly?
    bool GrantsFriendAccessTo(Assembly *pAccessingAssembly, FieldDesc *pFD);
    bool GrantsFriendAccessTo(Assembly *pAccessingAssembly, MethodDesc *pMD);
    bool GrantsFriendAccessTo(Assembly *pAccessingAssembly, MethodTable *pMT);
    bool IgnoresAccessChecksTo(Assembly *pAccessedAssembly);

#ifdef FEATURE_COMINTEROP
    bool IsImportedFromTypeLib()
    {
        WRAPPER_NO_CONTRACT;
        return ((GetInteropAttributeMask() & INTEROP_ATTRIBUTE_IMPORTED_FROM_TYPELIB) != 0);
    }

    bool IsPIAOrImportedFromTypeLib()
    {
        WRAPPER_NO_CONTRACT;
        return ((GetInteropAttributeMask() & (INTEROP_ATTRIBUTE_IMPORTED_FROM_TYPELIB | INTEROP_ATTRIBUTE_PRIMARY_INTEROP_ASSEMBLY)) != 0);
    }

    bool IsPIA()
    {
        WRAPPER_NO_CONTRACT;
        return ((GetInteropAttributeMask() & INTEROP_ATTRIBUTE_PRIMARY_INTEROP_ASSEMBLY) != 0);
    }
#endif

    static void AddDiagnosticStartupHookPath(LPCWSTR wszPath);

protected:
#ifdef FEATURE_COMINTEROP

    enum InteropAttributeStatus {
        INTEROP_ATTRIBUTE_UNSET                    = 0,
        INTEROP_ATTRIBUTE_CACHED                   = 1,
        INTEROP_ATTRIBUTE_IMPORTED_FROM_TYPELIB    = 2,
        INTEROP_ATTRIBUTE_PRIMARY_INTEROP_ASSEMBLY = 4,
    };

    InteropAttributeStatus GetInteropAttributeMask()
    {
        LIMITED_METHOD_CONTRACT;

        if (m_InteropAttributeStatus & INTEROP_ATTRIBUTE_CACHED)
            return m_InteropAttributeStatus;

        int mask = INTEROP_ATTRIBUTE_UNSET;

        if (GetModule()->GetCustomAttribute(TokenFromRid(1, mdtAssembly), WellKnownAttribute::ImportedFromTypeLib, NULL, 0) == S_OK)
            mask |= INTEROP_ATTRIBUTE_IMPORTED_FROM_TYPELIB;
        if (GetModule()->GetCustomAttribute(TokenFromRid(1, mdtAssembly), WellKnownAttribute::PrimaryInteropAssembly, NULL, 0) == S_OK)
            mask |= INTEROP_ATTRIBUTE_PRIMARY_INTEROP_ASSEMBLY;

        if (!IsDynamic())
        {
            mask |= INTEROP_ATTRIBUTE_CACHED;
            m_InteropAttributeStatus = static_cast<InteropAttributeStatus>(mask);
        }

        return static_cast<InteropAttributeStatus>(mask);
    }
#endif // FEATURE_COMINTEROP

private:

    //****************************************************************************************

    void CacheFriendAssemblyInfo();
#ifndef DACCESS_COMPILE
    ReleaseHolder<FriendAssemblyDescriptor> GetFriendAssemblyInfo();
#endif
public:
    void UpdateCachedFriendAssemblyInfo();

private:
    PTR_ClassLoader       m_pClassLoader;   // Single Loader

    PTR_MethodDesc        m_pEntryPoint;    // Method containing the entry point
    PTR_Module            m_pModule;
    PTR_PEAssembly        m_pPEAssembly;

    FriendAssemblyDescriptor *m_pFriendAssemblyDescriptor;

#ifdef FEATURE_COMINTEROP
    // If a TypeLib is ever required for this module, cache the pointer here.
    ITypeLib              *m_pITypeLib;
    InteropAttributeStatus m_InteropAttributeStatus;
#endif // FEATURE_COMINTEROP

    PTR_LoaderAllocator   m_pLoaderAllocator;
#ifdef FEATURE_COLLECTIBLE_TYPES
    BYTE                  m_isCollectible;
#endif // FEATURE_COLLECTIBLE_TYPES
    bool                  m_isDynamic;

    // Load state tracking
    bool            m_isLoading;
    bool            m_isTerminated;
    FileLoadLevel   m_level;
    DWORD           m_notifyFlags;
    Exception*      m_pError;

#ifdef _DEBUG
    bool            m_bDisableActivationCheck;
#endif

    DebuggerAssemblyControlFlags m_debuggerFlags;

    LOADERHANDLE          m_hExposedObject;

    DomainAssembly*             m_NextAssemblyInSameALC;

    friend struct ::cdac_data<Assembly>;
};

template<>
struct cdac_data<Assembly>
{
#ifdef FEATURE_COLLECTIBLE_TYPES
    static constexpr size_t IsCollectible = offsetof(Assembly, m_isCollectible);
#endif
    static constexpr size_t IsDynamic = offsetof(Assembly, m_isDynamic);
    static constexpr size_t Module = offsetof(Assembly, m_pModule);
    static constexpr size_t Error = offsetof(Assembly, m_pError);
    static constexpr size_t NotifyFlags = offsetof(Assembly, m_notifyFlags);
    static constexpr size_t Level = offsetof(Assembly, m_level);
};

#ifndef DACCESS_COMPILE

//---------------------------------------------------------------------------------------
//
// FriendSecurityDescriptor contains information on which assemblies are friends of an assembly, as well as
// which individual internals are visible to those friend assemblies.
//
class FriendAssemblyDescriptor
{
    friend class Assembly;
public:
    ~FriendAssemblyDescriptor();

    static
    ReleaseHolder<FriendAssemblyDescriptor> CreateFriendAssemblyDescriptor(PEAssembly *pAssembly);

    //---------------------------------------------------------------------------------------
    //
    // Checks to see if an assembly has friend access to a particular member.
    //
    // Arguments:
    //    pAccessingAssembly - the assembly requesting friend access
    //    pMember            - the member that is attempting to be accessed
    //
    // Return Value:
    //    true if friend access is allowed, false otherwise
    //
    // Notes:
    //    Template type T should be either FieldDesc, MethodDesc, or MethodTable.
    //

    template <class T>
    bool GrantsFriendAccessTo(Assembly *pAccessingAssembly, T *pMember)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            PRECONDITION(CheckPointer(pAccessingAssembly));
            PRECONDITION(CheckPointer(pMember));
        }
        CONTRACTL_END;

        return IsAssemblyOnList(pAccessingAssembly, m_alFullAccessFriendAssemblies);
    }


    bool IgnoresAccessChecksTo(Assembly *pAccessedAssembly)
    {
        return IsAssemblyOnList(pAccessedAssembly, m_subjectAssemblies);
    }

    void AddRef()
    {
        InterlockedIncrement(&m_refCount);
    }

    void Release()
    {
        if (InterlockedDecrement(&m_refCount) == 0)
        {
            delete this;
        }
    }

private:
    typedef AssemblySpec FriendAssemblyName_t;
    typedef NewHolder<AssemblySpec> FriendAssemblyNameHolder;

    ArrayList                  m_alFullAccessFriendAssemblies;      // Friend assemblies which have access to all internals
    ArrayList                  m_subjectAssemblies;                 // Subject assemblies which we will not perform access checks against
    LONG                       m_refCount = 1;

    FriendAssemblyDescriptor();

    void AddFriendAssembly(FriendAssemblyName_t *pFriendAssembly);
    void AddSubjectAssembly(FriendAssemblyName_t *pSubjectAssembly);

    static
    bool IsAssemblyOnList(Assembly *pAssembly, const ArrayList &alAssemblyNames)
    {
        return IsAssemblyOnList(pAssembly->GetPEAssembly(), alAssemblyNames);
    }

    static
    bool IsAssemblyOnList(PEAssembly *pAssembly, const ArrayList &alAssemblyNames);
};

#endif // !DACCESS_COMPILE


#endif
