// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
#ifdef FEATURE_FUSION
#include "fusion.h"
#include "fusionbind.h"
#endif
#include "eehash.h"
#include "listlock.h"
#include "iceefilegen.h"
#include "cordbpriv.h"
#include "assemblyspec.hpp"

// A helper macro for the assembly's module hash (m_pAllowedFiles).
#define UTF8_TO_LOWER_CASE(str, qb)                                                             \
{                                                                                               \
    WRAPPER_NO_CONTRACT;                                                                           \
    INT32 allocBytes = InternalCasingHelper::InvariantToLower(NULL, 0, str);                    \
    qb.AllocThrows(allocBytes);                                                                 \
    InternalCasingHelper::InvariantToLower((LPUTF8) qb.Ptr(), allocBytes, str);                 \
}


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
#define ASSEMBLY_ACCESS_REFLECTION_ONLY    0x04
#define ASSEMBLY_ACCESS_COLLECT 0x8

// This must match System.Reflection.Emit.DynamicAssemblyFlags in AssemblyBuilder.cs
enum DynamicAssemblyFlags
{
    kAllCriticalAssembly        = 0x00000001,
    kAptcaAssembly              = 0x00000002,
    kCriticalAssembly           = 0x00000004,
    kTransparentAssembly        = 0x00000008,
    kTreatAsSafeAssembly        = 0x00000010
};

struct CreateDynamicAssemblyArgsGC
{
    APPDOMAINREF    refThis;
    OBJECTREF       refusedPset;
    OBJECTREF       optionalPset;
    OBJECTREF       requiredPset;
    OBJECTREF       identity;
    ASSEMBLYNAMEREF assemblyName;
    U1ARRAYREF      securityRulesBlob;
    U1ARRAYREF      aptcaBlob;
    LOADERALLOCATORREF loaderAllocator;
};

// This enumeration must be kept in sync with the managed enum System.Security.SecurityContextSource
typedef enum
{
    kCurrentAppDomain = 0,
    kCurrentAssembly
}
SecurityContextSource;

struct CreateDynamicAssemblyArgs : CreateDynamicAssemblyArgsGC
{
    INT32           access;
    DynamicAssemblyFlags  flags;
    StackCrawlMark* stackMark;
    SecurityContextSource securityContextSource;
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

#if defined(FEATURE_TRACELOGGING)
	void TelemetryLogTargetFrameworkAttribute();
#endif // FEATURE_TRACELOGGING

    void StartUnload();
    void Terminate( BOOL signalProfiler = TRUE );

    static Assembly *Create(BaseDomain *pDomain, PEAssembly *pFile, DebuggerAssemblyControlFlags debuggerFlags, BOOL fIsCollectible, AllocMemTracker *pamTracker, LoaderAllocator *pLoaderAllocator);

    BOOL IsSystem() { WRAPPER_NO_CONTRACT; return m_pManifestFile->IsSystem(); }

    static Assembly *CreateDynamic(AppDomain *pDomain, CreateDynamicAssemblyArgs *args);
#ifdef  FEATURE_MULTIMODULE_ASSEMBLIES    
    ReflectionModule *CreateDynamicModule(LPCWSTR szModuleName, LPCWSTR szFileName, BOOL fIsTransient, INT32* ptkFile = NULL);
#endif

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
            WRAPPER_NO_CONTRACT;
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
            WRAPPER_NO_CONTRACT;
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

#ifdef  FEATURE_MULTIMODULE_ASSEMBLIES
    //****************************************************************************************
    //
    // Find the module 
    Module* FindModule(PEFile *pFile, BOOL includeLoading = FALSE);
#endif //  FEATURE_MULTIMODULE_ASSEMBLIES

#ifdef  FEATURE_MIXEDMODE
    // Finds loading modules as well
    DomainFile* FindIJWDomainFile(HMODULE hMod, const SString &path);
#endif
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
    // If true, this assembly is loaded only for introspection. We can load modules, types, etc,
    // but no code execution or object instantiation is permitted.
    //-----------------------------------------------------------------------------------------
    BOOL IsIntrospectionOnly();

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

#if defined(FEATURE_APTCA) || defined(FEATURE_CORESYSTEM)
    BOOL AllowUntrustedCaller();
#endif // defined(FEATURE_APTCA) || defined(FEATURE_CORESYSTEM)
    
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


    // Level of strong name support (dynamic assemblies only).
    enum StrongNameLevel {
        SN_NONE = 0,
        SN_PUBLIC_KEY = 1,
        SN_FULL_KEYPAIR_IN_ARRAY = 2,
        SN_FULL_KEYPAIR_IN_CONTAINER = 3
    };

    StrongNameLevel GetStrongNameLevel()
    {
        LIMITED_METHOD_CONTRACT;
        return m_eStrongNameLevel;
    }

    void SetStrongNameLevel(StrongNameLevel eLevel)
    {
        LIMITED_METHOD_CONTRACT;
        m_eStrongNameLevel = eLevel;
    }

    // returns whether CAS policy needs to be resolved for this assembly
    // or whether it's safe to skip that step.
    BOOL CanSkipPolicyResolution()
    {
        WRAPPER_NO_CONTRACT;
        return IsSystem() || IsIntrospectionOnly() || (m_isDynamic && !(m_dwDynamicAssemblyAccess & ASSEMBLY_ACCESS_RUN));
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

    ReflectionModule* GetOnDiskManifestModule()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pOnDiskManifest;
    }

    BOOL NeedsToHideManifestForEmit()
    {
        return m_needsToHideManifestForEmit;
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

    void GetCodeBase(SString &result)
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

    BOOL IsDisabledPrivateReflection();

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
#ifdef  FEATURE_PREJIT
    void DeleteNativeCodeRanges();
#endif

    BOOL GetResource(LPCSTR szName, DWORD *cbResource,
                     PBYTE *pbInMemoryResource, Assembly **pAssemblyRef,
                     LPCSTR *szFileName, DWORD *dwLocation,
                     StackCrawlMark *pStackMark = NULL, BOOL fSkipSecurityCheck = FALSE,
                     BOOL fSkipRaiseResolveEvent = FALSE);

    //****************************************************************************************
#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    FORCEINLINE BOOL IsDynamic() { LIMITED_METHOD_CONTRACT; return m_isDynamic; }
    FORCEINLINE BOOL IsCollectible() { LIMITED_METHOD_DAC_CONTRACT; return m_isCollectible; }
    FORCEINLINE BOOL HasRunAccess() {
        LIMITED_METHOD_CONTRACT; 
        SUPPORTS_DAC;
        return m_dwDynamicAssemblyAccess & ASSEMBLY_ACCESS_RUN; 
    }
    FORCEINLINE BOOL HasSaveAccess() {LIMITED_METHOD_CONTRACT; return m_dwDynamicAssemblyAccess & ASSEMBLY_ACCESS_SAVE; }

    DWORD GetNextModuleIndex() { LIMITED_METHOD_CONTRACT; return m_nextAvailableModuleIndex++; }

    void AddType(Module* pModule,
                 mdTypeDef cl);
    void AddExportedType(mdExportedType cl);
#ifndef FEATURE_CORECLR
    void PrepareSavingManifest(ReflectionModule *pAssemblyModule);
    mdFile AddFile(LPCWSTR wszFileName);
    void SetFileHashValue(mdFile tkFile, LPCWSTR wszFullFileName);
#endif
    mdAssemblyRef AddAssemblyRef(Assembly *refedAssembly, IMetaDataAssemblyEmit *pAssemEmitter = NULL, BOOL fUsePublicKeyToken = TRUE);
#ifndef FEATURE_CORECLR
    mdExportedType AddExportedTypeOnDisk(LPCWSTR wszExportedType, mdToken tkImpl, mdToken tkTypeDef, CorTypeAttr flags);
    mdExportedType AddExportedTypeInMemory(LPCWSTR wszExportedType, mdToken tkImpl, mdToken tkTypeDef, CorTypeAttr flags);
    void AddStandAloneResource(LPCWSTR wszName, LPCWSTR wszDescription, LPCWSTR wszMimeType, LPCWSTR wszFileName, LPCWSTR wszFullFileName, int iAttribute);
    void SaveManifestToDisk(LPCWSTR wszFileName, int entrypoint, int fileKind, DWORD corhFlags, DWORD peFlags);
#endif // FEATURE_CORECLR
#ifndef FEATURE_CORECLR
    void AddDeclarativeSecurity(DWORD dwAction, void const *pValue, DWORD cbValue);

    IMetaDataAssemblyEmit *GetOnDiskMDAssemblyEmitter();
#endif // FEATURE_CORECLR

    //****************************************************************************************

    DomainAssembly *GetDomainAssembly(AppDomain *pDomain);
    void SetDomainAssembly(DomainAssembly *pAssembly);

    // Verison of GetDomainAssembly that uses the current AppDomain (N/A in DAC builds)
#ifndef DACCESS_COMPILE
    DomainAssembly *GetDomainAssembly()     { WRAPPER_NO_CONTRACT; return GetDomainAssembly(GetAppDomain()); }
#endif

    // FindDomainAssembly will return NULL if the assembly is not in the given domain
    DomainAssembly *FindDomainAssembly(AppDomain *pDomain);

#if defined(FEATURE_COLLECTIBLE_TYPES) && !defined(DACCESS_COMPILE)
    OBJECTHANDLE GetLoaderAllocatorObjectHandle() { WRAPPER_NO_CONTRACT; return GetLoaderAllocator()->GetLoaderAllocatorObjectHandle(); }
#endif // FEATURE_COLLECTIBLE_TYPES

    IAssemblySecurityDescriptor *GetSecurityDescriptor(AppDomain *pDomain = NULL);
    ISharedSecurityDescriptor *GetSharedSecurityDescriptor() { LIMITED_METHOD_CONTRACT; return m_pSharedSecurityDesc; }

#ifndef DACCESS_COMPILE
    const SecurityTransparencyBehavior *GetSecurityTransparencyBehavior();
    const SecurityTransparencyBehavior *TryGetSecurityTransparencyBehavior();
    void SetSecurityTransparencyBehavior(const SecurityTransparencyBehavior *pTransparencyBehavior);
#endif // !DACCESS_COMPILE


    BOOL CanBeShared(DomainAssembly *pAsAssembly);

#ifdef FEATURE_LOADER_OPTIMIZATION
    BOOL MissingDependenciesCheckDone();
    void SetMissingDependenciesCheckDone();
#ifdef FEATURE_FUSION
    void SetBindingClosure(IAssemblyBindingClosure* pClosure); // Addrefs. It is assumed the caller did not addref pClosure for us.
    IAssemblyBindingClosure* GetBindingClosure();
#endif
#endif // FEATURE_LOADER_OPTIMIZATION

    void SetDomainNeutral() { LIMITED_METHOD_CONTRACT; m_fIsDomainNeutral = TRUE; }
    BOOL IsDomainNeutral() { LIMITED_METHOD_DAC_CONTRACT; return m_fIsDomainNeutral; }

    BOOL IsSIMDVectorAssembly() { LIMITED_METHOD_DAC_CONTRACT; return m_fIsSIMDVectorAssembly; }

#ifdef FEATURE_PREJIT    
    BOOL IsInstrumented();
    BOOL IsInstrumentedHelper();
#endif // FEATURE_PREJIT

    HRESULT AllocateStrongNameSignature(ICeeFileGen  *pCeeFileGen,
                                        HCEEFILE      ceeFile);
    HRESULT SignWithStrongName(LPCWSTR wszFileName);

#ifdef FEATURE_FUSION
    IAssembly* GetFusionAssembly()
    {
        WRAPPER_NO_CONTRACT;
        return m_pManifestFile->GetFusionAssembly();
    }

    IAssemblyName* GetFusionAssemblyName()
    {
        WRAPPER_NO_CONTRACT;
        return m_pManifestFile->GetFusionAssemblyName();
    }

    IAssemblyName* GetFusionAssemblyNameNoCreate()
    {
        WRAPPER_NO_CONTRACT;
        return m_pManifestFile->GetFusionAssemblyNameNoCreate();
    }
    
    IHostAssembly* GetIHostAssembly()
    {
        WRAPPER_NO_CONTRACT;
        return m_pManifestFile->GetIHostAssembly();
    }
#endif// FEATURE_FUSION

#ifdef FEATURE_COMINTEROP
    // Get any cached ITypeLib* for the assembly.
    ITypeLib *GetTypeLib();
    // Cache the ITypeLib*, if one is not already cached.
    void SetTypeLib(ITypeLib *pITLB);
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

    UINT64 GetHostAssemblyId() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_HostAssemblyId;
    }

#endif // #ifndef DACCESS_COMPILE

    //****************************************************************************************
    //

#ifdef  FEATURE_MULTIMODULE_ASSEMBLIES
    PEModule * LoadModule_AddRef(mdFile kFile, BOOL fLoadResource);
    PEModule * RaiseModuleResolveEvent_AddRef(LPCSTR szName, mdFile kFile);
#endif //  FEATURE_MULTIMODULE_ASSEMBLIES
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

    // Does this assembly contain windows metadata
    bool IsWinMD();

    // Does this assembly contain windows metadata with managed implementation
    bool IsManagedWinMD();

    // Returns the IWinMDImport interface of the manifest module metadata or NULL if this assembly is not a .winmd
    IWinMDImport *GetManifestWinMDImport();
#endif

#ifndef FEATURE_CORECLR
    BOOL SupportsAutoNGen()
    {
        WRAPPER_NO_CONTRACT;
        return m_fSupportsAutoNGen;
    }
#endif

protected:

    enum {
        FREE_KEY_PAIR = 4,
        FREE_KEY_CONTAINER = 8,
    };

    void ReportAssemblyUse();

#ifdef FEATURE_COMINTEROP
    enum WinMDStatus
    {
        WinMDStatus_Unknown,
        WinMDStatus_IsPureWinMD,
        WinMDStatus_IsManagedWinMD,
        WinMDStatus_IsNotWinMD
    };

    // Determine if the assembly is a pure Windows Metadata file, contians managed implementation, or is not
    // Windows Metadata at all.
    WinMDStatus GetWinMDStatus();

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

        if (!IsWinMD()) // ignore classic COM interop CAs in .winmd
        {
            if (this->GetManifestImport()->GetCustomAttributeByName(TokenFromRid(1, mdtAssembly), INTEROP_IMPORTEDFROMTYPELIB_TYPE, 0, 0) == S_OK)
                mask |= INTEROP_ATTRIBUTE_IMPORTED_FROM_TYPELIB;
            if (this->GetManifestImport()->GetCustomAttributeByName(TokenFromRid(1, mdtAssembly), INTEROP_PRIMARYINTEROPASSEMBLY_TYPE, 0, 0) == S_OK)
                mask |= INTEROP_ATTRIBUTE_PRIMARY_INTEROP_ASSEMBLY;
        }
        
        if (!IsDynamic())
        {
            mask |= INTEROP_ATTRIBUTE_CACHED;
            m_InteropAttributeStatus = static_cast<InteropAttributeStatus>(mask);
        }

        return static_cast<InteropAttributeStatus>(mask);
    }
#endif // FEATURE_INTEROP

    // Keep track of the vars that need to be freed.
    short int m_FreeFlag;

#ifdef FEATURE_MULTIMODULE_ASSEMBLIES
    // Hash of files in manifest by name to File token
    PTR_EEUtf8StringHashTable m_pAllowedFiles;
    // Critical section guarding m_pAllowedFiles
    Crst m_crstAllowedFiles;
#endif 

private:

    //****************************************************************************************

    void CacheManifestExportedTypes(AllocMemTracker *pamTracker);
    void CacheManifestFiles();

    void CacheFriendAssemblyInfo();
   
#ifndef FEATURE_CORECLR
    void GenerateBreadcrumbForServicing();
    void WriteBreadcrumb(const SString &ssDisplayName);
    bool HasServiceableAttribute();
    bool IsExistingOobAssembly();
    void CheckDenyList(const SString &ssDisplayName);

    BOOL SupportsAutoNGenWorker();
#endif

    PTR_BaseDomain        m_pDomain;        // Parent Domain
    PTR_ClassLoader       m_pClassLoader;   // Single Loader



    PTR_MethodDesc        m_pEntryPoint;    // Method containing the entry point
    PTR_Module            m_pManifest;
    PTR_PEAssembly        m_pManifestFile;
    ReflectionModule*     m_pOnDiskManifest;  // This is the module containing the on disk manifest.
    BOOL                  m_fEmbeddedManifest;

    FriendAssemblyDescriptor *m_pFriendAssemblyDescriptor;

    // Strong name key info for reflection emit
    PBYTE                 m_pbStrongNameKeyPair;
    DWORD                 m_cbStrongNameKeyPair;
    LPWSTR                m_pwStrongNameKeyContainer;
    StrongNameLevel       m_eStrongNameLevel;

    BOOL                  m_isDynamic;
#ifdef FEATURE_COLLECTIBLE_TYPES
    BOOL                  m_isCollectible;
#endif // FEATURE_COLLECTIBLE_TYPES
    // this boolean is used by Reflection.Emit to determine when to hide m_pOnDiskManifest.
    // Via reflection emit m_pOnDiskManifest may be explicitly defined by the user and thus available
    // or created implicitly via Save in which case it needs to be hidden from the user for
    // backward compatibility reason.
    // This is a bit of a workaround however and that whole story should be understood a bit better...
    BOOL                  m_needsToHideManifestForEmit;
    DWORD                 m_dwDynamicAssemblyAccess;
    DWORD                 m_nextAvailableModuleIndex;
    PTR_LoaderAllocator   m_pLoaderAllocator;
    DWORD                 m_isDisabledPrivateReflection;

#ifdef FEATURE_COMINTEROP
    // If a TypeLib is ever required for this module, cache the pointer here.
    ITypeLib              *m_pITypeLib;
    InteropAttributeStatus m_InteropAttributeStatus;

    WinMDStatus            m_winMDStatus;
    IWinMDImport          *m_pManifestWinMDImport;
#endif // FEATURE_COMINTEROP

    ISharedSecurityDescriptor* m_pSharedSecurityDesc;    // Security descriptor (permission requests, signature etc)
    const SecurityTransparencyBehavior *m_pTransparencyBehavior; // Transparency implementation the assembly uses

    BOOL                   m_fIsDomainNeutral;
#ifdef FEATURE_LOADER_OPTIMIZATION
    BOOL m_bMissingDependenciesCheckDone;
#ifdef FEATURE_FUSION
    IAssemblyBindingClosure * m_pBindingClosure;
#endif
#endif // FEATURE_LOADER_OPTIMIZATION

    DebuggerAssemblyControlFlags m_debuggerFlags;

    BOOL                  m_fTerminated;

    BOOL                  m_fIsSIMDVectorAssembly;
    UINT64                m_HostAssemblyId;

    DWORD                 m_dwReliabilityContract;

#ifndef FEATURE_CORECLR
    BOOL                  m_fSupportsAutoNGen;
#endif
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
public:
    ~FriendAssemblyDescriptor();

    static
    FriendAssemblyDescriptor *CreateFriendAssemblyDescriptor(PEAssembly *pAssembly);

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

#ifdef FEATURE_FRAMEWORK_INTERNAL
        if (IsAssemblyOnList(pAccessingAssembly, m_alPartialAccessFriendAssemblies))
        {
            return FriendAccessAppliesTo(pMember) && IsMemberVisibleToFriends(pMember);
        }
        else
#endif // FEATURE_FRAMEWORK_INTERNAL
            if (IsAssemblyOnList(pAccessingAssembly, m_alFullAccessFriendAssemblies))
        {
            return true;
        }
#if defined(FEATURE_STRONGNAME_TESTKEY_ALLOWED) && defined(FEATURE_FRAMEWORK_INTERNAL)
        else if (pMember->GetModule()->GetFile()->GetAssembly()->IsProfileAssembly()&&
                 pAccessingAssembly->GetManifestFile() != NULL &&
                 pAccessingAssembly->GetManifestFile()->IsProfileTestAssembly())
        {
            // Test hook - All platoform assemblies consider any test assembly which is part of the profile to implicitly
            // be on the friends list.  This allows test access to the framework internal attributes, without
            // having to add test assemblies to the explicit friend assembly list.
            return FriendAccessAppliesTo(pMember) && IsMemberVisibleToFriends(pMember);
        }
#endif // FEATURE_STRONGNAME_TESTKEY_ALLOWED && FEATURE_FRAMEWORK_INTERNAL
        else
        {
            return false;
        }
    }

#ifndef FEATURE_CORECLR
    //------------------------------------------------------------------------------
    // It is undesirable to reintroduce the concept of inquiring about friendship without specifying a member or type
    // but necessary for TP. In case of doubt, it's safer to return "true" as this won't affect
    // correctness (but might cause unnecessary ngen's when updating assemblies.)
    //------------------------------------------------------------------------------
    bool MightGrantFriendAccessTo(PEAssembly *pAccessingAssembly)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            PRECONDITION(CheckPointer(pAccessingAssembly));
        }
        CONTRACTL_END;

#ifdef FEATURE_FRAMEWORK_INTERNAL
        if (IsAssemblyOnList(pAccessingAssembly, m_alPartialAccessFriendAssemblies))
        {
            return true;
        }
        else
#endif // FEATURE_FRAMEWORK_INTERNAL
        if (IsAssemblyOnList(pAccessingAssembly, m_alFullAccessFriendAssemblies))
        {
            return true;
        }
        return false;
    }
#endif // !FEATURE_CORECLR

    bool IgnoresAccessChecksTo(Assembly *pAccessedAssembly)
    {
        return IsAssemblyOnList(pAccessedAssembly, m_subjectAssemblies);
    }

private:
#ifdef FEATURE_FRAMEWORK_INTERNAL
    static const LPCSTR AllInternalsVisibleProperty;
    static const DWORD FriendMemberHashSize = 31;                   // Number of buckets in the friend member hash table
#endif // FEATURE_FRAMEWORK_INTERNAL

#ifdef FEATURE_FUSION
    typedef IAssemblyName FriendAssemblyName_t;
    typedef NonVMComHolder<IAssemblyName> FriendAssemblyNameHolder;
#else // FEATURE_FUSION
    typedef AssemblySpec FriendAssemblyName_t;
    typedef NewHolder<AssemblySpec> FriendAssemblyNameHolder;
#endif // FEATURE_FUSION

    ArrayList                  m_alFullAccessFriendAssemblies;      // Friend assemblies which have access to all internals
    ArrayList                  m_subjectAssemblies;                 // Subject assemblies which we will not perform access checks against

#ifdef FEATURE_FRAMEWORK_INTERNAL
    ArrayList                  m_alPartialAccessFriendAssemblies;   // Friend assemblies which have access to only specific internals
    EEPtrHashTable             m_htFriendMembers;                   // Cache of internal members checked for visibility to friend assemblies
    Crst                       m_crstFriendMembersCache;            // Critical section guarding m_htFriendMembers
#endif // FEATURE_FRAMEWORK_INTERNAL

    FriendAssemblyDescriptor();

#ifdef FEATURE_FRAMEWORK_INTERNAL
    void AddFriendAssembly(FriendAssemblyName_t *pFriendAssembly, bool fAllInternalsVisible);
#else // FEATURE_FRAMEWORK_INTERNAL
    void AddFriendAssembly(FriendAssemblyName_t *pFriendAssembly);
#endif // FEATURE_FRAMEWORK_INTERNAL
    void AddSubjectAssembly(FriendAssemblyName_t *pSubjectAssembly);

#ifdef FEATURE_FRAMEWORK_INTERNAL
    static
    bool FriendAccessAppliesTo(FieldDesc *pFD);

    static
    bool FriendAccessAppliesTo(MethodDesc *pMD);

    static
    bool FriendAccessAppliesTo(MethodTable *pMT);

    static
    mdToken GetMetadataToken(FieldDesc *pFD);

    static
    mdToken GetMetadataToken(MethodDesc *pMD);

    static
    mdToken GetMetadataToken(MethodTable *pMT);

    static
    bool HasFriendAccessAttribute(IMDInternalImport *pMDImport);
#endif // FEATURE_FRAMEWORK_INTERNAL

    static
    bool IsAssemblyOnList(Assembly *pAssembly, const ArrayList &alAssemblyNames)
    {
        return IsAssemblyOnList(pAssembly->GetManifestFile(), alAssemblyNames);
    }

    static
    bool IsAssemblyOnList(PEAssembly *pAssembly, const ArrayList &alAssemblyNames);

#ifdef FEATURE_FRAMEWORK_INTERNAL
    bool HasFriendAccessAttribute(IMDInternalImport *pMDImport, mdToken tkMember);

    //---------------------------------------------------------------------------------------
    //
    // Checks to see if a specific member has the FriendAccessAllowed attribute
    //
    //

    template<class T>
    bool IsMemberVisibleToFriends(T *pMember)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            PRECONDITION(CheckPointer(pMember));
            PRECONDITION(FriendAccessAppliesTo(pMember));
        }
        CONTRACTL_END;

        CrstHolder lock(&m_crstFriendMembersCache);

        HashDatum hd;
        if (!m_htFriendMembers.GetValue(pMember, &hd))
        {
            bool fAllowsAccess = HasFriendAccessAttribute(pMember->GetMDImport(), GetMetadataToken(pMember));
            hd = reinterpret_cast<HashDatum>(fAllowsAccess);
            
            m_htFriendMembers.InsertValue(pMember, hd);
        }

        return static_cast<bool>(!!hd);
    }
#endif // FEATURE_FRAMEWORK_INTERNAL
};

#endif // !DACCESS_COMPILE

#if !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE)
class ExistingOobAssemblyList
{
public:
#ifndef DACCESS_COMPILE
    ExistingOobAssemblyList();

    bool IsOnlist(Assembly *pAssembly);

    static void Init();
    static ExistingOobAssemblyList *Instance() { return s_pInstance; }
#endif

private:
    ArrayList m_alExistingOobAssemblies;

    // The single instance of this class:
    static ExistingOobAssemblyList *s_pInstance;
};
#endif // !defined(FEATURE_CORECLR) && !defined(CROSSGEN_COMPILE)

#endif
