// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header:  Assembly.hpp
**

**
** Purpose: Implements assembly (loader domain) architecture
**
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

class BaseDomain;
class AppDomain;
class DomainAssembly;
class DomainModule;
class SystemDomain;
class ClassLoader;
class ComDynamicWrite;
class AssemblySink;
class AssemblyNative;
class AssemblySpec;
class ISharedSecurityDescriptor;
class SecurityTransparencyBehavior;
class Pending;
class AllocMemTracker;
class FriendAssemblyDescriptor;

// Bits in m_dwDynamicAssemblyAccess (see System.Reflection.Emit.AssemblyBuilderAccess.cs)
#define ASSEMBLY_ACCESS_RUN     0x01
#define ASSEMBLY_ACCESS_SAVE    0x02
#define ASSEMBLY_ACCESS_COLLECT 0x8

struct CreateDynamicAssemblyArgsGC
{
    ASSEMBLYNAMEREF assemblyName;
    LOADERALLOCATORREF loaderAllocator;
};

struct CreateDynamicAssemblyArgs : CreateDynamicAssemblyArgsGC
{
    INT32           access;
    StackCrawlMark* stackMark;
};

// An assembly is the unit of deployment for managed code.  Typically Assemblies are one to one with files
// (Modules), however this is not necessary, as an assembly can contain serveral files (typically you only
// do this so that you can resource-only modules that are national language specific)
//
// Conceptually Assemblies are loaded into code:AppDomain
//
// So in general an assemly is a list of code:Module, where a code:Module is 1-1 with a DLL or EXE file.
//
// One of the modules the code:Assembly.m_pManifest is special in that it knows about all the other
// modules in an assembly (often it is the only one).
//
class Assembly
{
    friend class BaseDomain;
    friend class SystemDomain;
    friend class ClassLoader;
    friend class AssemblyNative;
    friend class AssemblySpec;
    friend class NDirect;
    friend class AssemblyNameNative;
    friend class ClrDataAccess;

public:
    Assembly(BaseDomain *pDomain, PEAssembly *pFile, DebuggerAssemblyControlFlags debuggerFlags, BOOL fIsCollectible);
    void Init(AllocMemTracker *pamTracker, LoaderAllocator *pLoaderAllocator);

    void StartUnload();
    void Terminate( BOOL signalProfiler = TRUE );

    static Assembly *Create(BaseDomain *pDomain, PEAssembly *pFile, DebuggerAssemblyControlFlags debuggerFlags, BOOL fIsCollectible, AllocMemTracker *pamTracker, LoaderAllocator *pLoaderAllocator);
    static void Initialize();

    BOOL IsSystem() { WRAPPER_NO_CONTRACT; return m_pManifestFile->IsSystem(); }

    static Assembly *CreateDynamic(AppDomain *pDomain, AssemblyBinder* pBinder, CreateDynamicAssemblyArgs *args);

    MethodDesc *GetEntryPoint();

    //****************************************************************************************
    //
    // Additional init tasks for Modules. This should probably be part of Module::Initialize()
    // but there's at least one call to ReflectionModule::Create that is *not* followed by a
    // PrepareModule call.
    void PrepareModuleForAssembly(Module* module, AllocMemTracker *pamTracker);

    // This is the final step of publishing a Module into an Assembly. This step cannot fail.
    void PublishModuleIntoAssembly(Module *module);

#ifndef DACCESS_COMPILE
    void SetIsTenured()
    {
        WRAPPER_NO_CONTRACT;
        m_pManifest->SetIsTenured();
    }

    // CAUTION: This should only be used as backout code if an assembly is unsuccessfully
    //          added to the shared domain assembly map.
    void UnsetIsTenured()
    {
        WRAPPER_NO_CONTRACT;
        m_pManifest->UnsetIsTenured();
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

    // ------------------------------------------------------------
    // Modules
    // ------------------------------------------------------------

    class ModuleIterator
    {
        Module* m_pManifest;
        DWORD m_i;

      public:
        // The preferred constructor.  If you use this, you don't have to
        // call Start() yourself
        ModuleIterator(Assembly *pAssembly)
        {
            WRAPPER_NO_CONTRACT;
            Start(pAssembly);
        }

        // When you don't have the Assembly at contruction time, use this
        // constructor, and explicitly call Start() to begin the iteration.
        ModuleIterator()
        {
            LIMITED_METHOD_CONTRACT;
            SUPPORTS_DAC;

            m_pManifest = NULL;
            m_i = (DWORD) -1;
        }

        void Start(Assembly * pAssembly)
        {
            LIMITED_METHOD_CONTRACT;
            SUPPORTS_DAC;

            m_pManifest = pAssembly->GetManifestModule();
            m_i = (DWORD) -1;
        }

        BOOL Next()
        {
            LIMITED_METHOD_CONTRACT;
            SUPPORTS_DAC;
            while (++m_i <= m_pManifest->GetFileMax())
            {
                if (GetModule() != NULL)
                    return TRUE;
            }
            return FALSE;
        }

        Module *GetModule()
        {
            LIMITED_METHOD_CONTRACT;
            SUPPORTS_DAC;
            return m_pManifest->LookupFile(TokenFromRid(m_i, mdtFile));
        }
    };

    ModuleIterator IterateModules()
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return ModuleIterator(this);
    }


    //****************************************************************************************
    //
    // Get the domain the assembly lives in.
    PTR_BaseDomain Parent()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pDomain;
    }

    // Sets the assemblies domain.
    void SetParent(BaseDomain* pParent);

    //-----------------------------------------------------------------------------------------
    // EnsureActive ensures that the assembly is properly prepped in the current app domain
    // for active uses like code execution, static field access, and instance allocation
    //-----------------------------------------------------------------------------------------
#ifndef DACCESS_COMPILE
    VOID EnsureActive();
#endif

    //-----------------------------------------------------------------------------------------
    // CheckActivated is a check predicate which should be used in active use paths like code
    // execution, static field access, and instance allocation
    //-----------------------------------------------------------------------------------------
    CHECK CheckActivated();

    // Returns the parent domain if it is not the system area. Returns NULL if it is the
    // system domain
    PTR_BaseDomain GetDomain();
    PTR_LoaderAllocator GetLoaderAllocator() { LIMITED_METHOD_DAC_CONTRACT; return m_pLoaderAllocator; }

    BOOL GetModuleZapFile(LPCWSTR name, SString &path);

#ifdef LOGGING
    LPCWSTR GetDebugName()
    {
        WRAPPER_NO_CONTRACT;
        return GetManifestFile()->GetDebugName();
    }
#endif

    LPCUTF8 GetSimpleName()
    {
        WRAPPER_NO_CONTRACT;
        return GetManifestFile()->GetSimpleName();
    }

    BOOL IsStrongNamed()
    {
        WRAPPER_NO_CONTRACT;
        return GetManifestFile()->IsStrongNamed();
    }

    const void *GetPublicKey(DWORD *pcbPK)
    {
        WRAPPER_NO_CONTRACT;
        return GetManifestFile()->GetPublicKey(pcbPK);
    }

    ULONG GetHashAlgId()
    {
        WRAPPER_NO_CONTRACT;
        return GetManifestFile()->GetHashAlgId();
    }

    HRESULT GetVersion(USHORT *pMajor, USHORT *pMinor, USHORT *pBuild, USHORT *pRevision)
    {
        WRAPPER_NO_CONTRACT;
        return GetManifestFile()->GetVersion(pMajor, pMinor, pBuild, pRevision);
    }

    LPCUTF8 GetLocale()
    {
        WRAPPER_NO_CONTRACT;
        return GetManifestFile()->GetLocale();
    }

    DWORD GetFlags()
    {
        WRAPPER_NO_CONTRACT;
        return GetManifestFile()->GetFlags();
    }

    PTR_LoaderHeap GetLowFrequencyHeap();
    PTR_LoaderHeap GetHighFrequencyHeap();
    PTR_LoaderHeap GetStubHeap();

    PTR_Module GetManifestModule()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_pManifest;
    }

    PTR_PEAssembly GetManifestFile()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_pManifestFile;
    }

    IMDInternalImport* GetManifestImport()
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return m_pManifestFile->GetPersistentMDImport();
    }

    HRESULT GetCustomAttribute(mdToken parentToken,
                               WellKnownAttribute attribute,
                               const void  **ppData,
                               ULONG *pcbData)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return GetManifestModule()->GetCustomAttribute(parentToken, attribute, ppData, pcbData);
    }

#ifndef DACCESS_COMPILE
    IMetaDataAssemblyImport* GetManifestAssemblyImporter()
    {
        WRAPPER_NO_CONTRACT;
        return m_pManifestFile->GetAssemblyImporter();
    }
#endif // DACCESS_COMPILE

    mdAssembly GetManifestToken()
    {
        LIMITED_METHOD_CONTRACT;

        return TokenFromRid(1, mdtAssembly);
    }

#ifndef DACCESS_COMPILE
    void GetDisplayName(SString &result, DWORD flags = 0)
    {
        WRAPPER_NO_CONTRACT;

        return m_pManifestFile->GetDisplayName(result, flags);
    }
#endif // DACCESS_COMPILE

    BOOL GetCodeBase(SString &result)
    {
        WRAPPER_NO_CONTRACT;

        return m_pManifestFile->GetCodeBase(result);
    }

    OBJECTREF GetExposedObject();

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

    void SetCopiedPDBs()
    {
        LIMITED_METHOD_CONTRACT;

        m_debuggerFlags = (DebuggerAssemblyControlFlags) (m_debuggerFlags | DACF_PDBS_COPIED);
    }

    ULONG HashIdentity()
    {
        return GetManifestFile()->HashIdentity();
    }

    //****************************************************************************************
    //
    // Uses the given token to load a module or another assembly. Returns the module in
    // which the implementation resides.

    mdFile GetManifestFileToken(IMDInternalImport *pImport, mdFile kFile);
    mdFile GetManifestFileToken(LPCSTR name);

    // On failure:
    //      if loadFlag == Loader::Load => throw
    //      if loadFlag != Loader::Load => return NULL
    Module *FindModuleByExportedType(mdExportedType mdType,
                                     Loader::LoadFlag loadFlag,
                                     mdTypeDef mdNested,
                                     mdTypeDef *pCL);

    static Module * FindModuleByTypeRef(Module *         pModule,
                                        mdTypeRef        typeRef,
                                        Loader::LoadFlag loadFlag,
                                        BOOL *           pfNoResolutionScope);

    Module *FindModuleByName(LPCSTR moduleName);

    //****************************************************************************************
    //
    INT32 ExecuteMainMethod(PTRARRAYREF *stringArgs, BOOL waitForOtherThreads);

    //****************************************************************************************

    Assembly();
    ~Assembly();

    BOOL GetResource(LPCSTR szName, DWORD *cbResource,
                     PBYTE *pbInMemoryResource, Assembly **pAssemblyRef,
                     LPCSTR *szFileName, DWORD *dwLocation,
                     BOOL fSkipRaiseResolveEvent = FALSE);

    //****************************************************************************************
#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    FORCEINLINE BOOL IsDynamic() { LIMITED_METHOD_CONTRACT; return m_isDynamic; }
    FORCEINLINE BOOL IsCollectible() { LIMITED_METHOD_DAC_CONTRACT; return m_isCollectible; }

    DWORD GetNextModuleIndex() { LIMITED_METHOD_CONTRACT; return m_nextAvailableModuleIndex++; }

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

#ifdef FEATURE_READYTORUN
    BOOL IsInstrumented();
    BOOL IsInstrumentedHelper();
#endif // FEATURE_READYTORUN

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

        if (GetManifestModule()->GetCustomAttribute(TokenFromRid(1, mdtAssembly), WellKnownAttribute::ImportedFromTypeLib, NULL, 0) == S_OK)
            mask |= INTEROP_ATTRIBUTE_IMPORTED_FROM_TYPELIB;
        if (GetManifestModule()->GetCustomAttribute(TokenFromRid(1, mdtAssembly), WellKnownAttribute::PrimaryInteropAssembly, NULL, 0) == S_OK)
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

    void CacheManifestExportedTypes(AllocMemTracker *pamTracker);
    void CacheManifestFiles();

    void CacheFriendAssemblyInfo();
#ifndef DACCESS_COMPILE
    ReleaseHolder<FriendAssemblyDescriptor> GetFriendAssemblyInfo();
#endif
public:
    void UpdateCachedFriendAssemblyInfo();
private:


    PTR_BaseDomain        m_pDomain;        // Parent Domain
    PTR_ClassLoader       m_pClassLoader;   // Single Loader



    PTR_MethodDesc        m_pEntryPoint;    // Method containing the entry point
    PTR_Module            m_pManifest;
    PTR_PEAssembly        m_pManifestFile;

    FriendAssemblyDescriptor *m_pFriendAssemblyDescriptor;

    BOOL                  m_isDynamic;
#ifdef FEATURE_COLLECTIBLE_TYPES
    BOOL                  m_isCollectible;
#endif // FEATURE_COLLECTIBLE_TYPES
    DWORD                 m_nextAvailableModuleIndex;
    PTR_LoaderAllocator   m_pLoaderAllocator;

#ifdef FEATURE_COMINTEROP
    // If a TypeLib is ever required for this module, cache the pointer here.
    ITypeLib              *m_pITypeLib;
    InteropAttributeStatus m_InteropAttributeStatus;
#endif // FEATURE_COMINTEROP

    DebuggerAssemblyControlFlags m_debuggerFlags;

    BOOL                  m_fTerminated;

#ifdef FEATURE_READYTORUN
    enum IsInstrumentedStatus {
        IS_INSTRUMENTED_UNSET = 0,
        IS_INSTRUMENTED_FALSE = 1,
        IS_INSTRUMENTED_TRUE = 2,
    };
    IsInstrumentedStatus    m_isInstrumentedStatus;
#endif // FEATURE_READYTORUN

};

typedef Assembly::ModuleIterator ModuleIterator;

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
        return IsAssemblyOnList(pAssembly->GetManifestFile(), alAssemblyNames);
    }

    static
    bool IsAssemblyOnList(PEAssembly *pAssembly, const ArrayList &alAssemblyNames);
};

#endif // !DACCESS_COMPILE


#endif
