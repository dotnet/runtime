// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: clsload.hpp
//



//

//
// ============================================================================

#ifndef _H_CLSLOAD
#define _H_CLSLOAD

#include "crst.h"
#include "eehash.h"
#include "vars.hpp"
#include "stubmgr.h"
#include "typehandle.h"
#include "object.h" // only needed for def. of PTRARRAYREF
#include "classloadlevel.h"
#include "specstrings.h"
#include "simplerwlock.hpp"
#include "classhash.h"

// SystemDomain is a friend of ClassLoader.
class SystemDomain;
class Assembly;
class ClassLoader;
class TypeKey;
class PendingTypeLoadEntry;
class PendingTypeLoadTable;
class EEClass;
class Thread;
class EETypeHashTable;
class DynamicResolver;
class SigPointer;

// Hash table parameter for unresolved class hash
#define UNRESOLVED_CLASS_HASH_BUCKETS 8

// This is information required to look up a type in the loader. Besides the
// basic name there is the meta data information for the type, whether the
// the name is case sensitive, and tokens not to load. This last item allows
// the loader to prevent a type from being recursively loaded.
typedef enum NameHandleTable
{
    nhCaseSensitive = 0,
    nhCaseInsensitive = 1
} NameHandleTable;

class HashedTypeEntry
{
public:
    typedef enum
    {
        IsNullEntry,            // Uninitialized HashedTypeEntry
        IsHashedTokenEntry,     // Entry is a token value in a R2R hashtable in from the R2R module
        IsHashedClassEntry      // Entry is a EEClassHashEntry_t from the hashtable constructed at
                                // module load time (or from the hashtable loaded from the native image)
    } EntryType;

    typedef struct
    {
        mdToken     m_TypeToken;
        Module *    m_pModule;
    } TokenTypeEntry;

private:
    EntryType               m_EntryType;
    PTR_EEClassHashEntry    m_pClassHashEntry;
    TokenTypeEntry          m_TokenAndModulePair;

public:
    HashedTypeEntry()
    {
        m_EntryType = EntryType::IsNullEntry;
        m_pClassHashEntry = PTR_NULL;
    }

    EntryType GetEntryType() const { return m_EntryType; }
    bool IsNull() const { return m_EntryType == EntryType::IsNullEntry; }

    const HashedTypeEntry& SetClassHashBasedEntryValue(EEClassHashEntry_t * pClassHashEntry)
    {
        LIMITED_METHOD_CONTRACT;

        m_EntryType = EntryType::IsHashedClassEntry;
        m_pClassHashEntry = dac_cast<PTR_EEClassHashEntry>(pClassHashEntry);
        return *this;
    }
    EEClassHashEntry_t * GetClassHashBasedEntryValue() const
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERT(m_EntryType == EntryType::IsHashedClassEntry);
        return m_pClassHashEntry;
    }

    const HashedTypeEntry& SetTokenBasedEntryValue(mdTypeDef typeToken, Module * pModule)
    {
        LIMITED_METHOD_CONTRACT;

        m_EntryType = EntryType::IsHashedTokenEntry;
        m_TokenAndModulePair.m_TypeToken = typeToken;
        m_TokenAndModulePair.m_pModule = pModule;
        return *this;
    }
    const TokenTypeEntry& GetTokenBasedEntryValue() const
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERT(m_EntryType == EntryType::IsHashedTokenEntry);
        return m_TokenAndModulePair;
    }
};

class NameHandle
{
    friend class ClassLoader;

    LPCUTF8 m_nameSpace;
    LPCUTF8 m_name;

    PTR_Module m_pTypeScope;
    mdToken m_mdType;
    mdToken m_mdTokenNotToLoad;
    NameHandleTable m_WhichTable;
    HashedTypeEntry m_Bucket;

public:

    NameHandle()
    {
        LIMITED_METHOD_CONTRACT;
        memset((void*) this, NULL, sizeof(*this));
    }

    NameHandle(LPCUTF8 name) :
        m_nameSpace(NULL),
        m_name(name),
        m_pTypeScope(PTR_NULL),
        m_mdType(mdTokenNil),
        m_mdTokenNotToLoad(tdNoTypes),
        m_WhichTable(nhCaseSensitive),
        m_Bucket()
    {
        LIMITED_METHOD_CONTRACT;
    }

    NameHandle(LPCUTF8 nameSpace, LPCUTF8 name) :
        m_nameSpace(nameSpace),
        m_name(name),
        m_pTypeScope(PTR_NULL),
        m_mdType(mdTokenNil),
        m_mdTokenNotToLoad(tdNoTypes),
        m_WhichTable(nhCaseSensitive),
        m_Bucket()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
    }

    NameHandle(Module* pModule, mdToken token) :
        m_nameSpace(NULL),
        m_name(NULL),
        m_pTypeScope(pModule),
        m_mdType(token),
        m_mdTokenNotToLoad(tdNoTypes),
        m_WhichTable(nhCaseSensitive),
        m_Bucket()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
    }

    NameHandle(const NameHandle & p)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        m_nameSpace = p.m_nameSpace;
        m_name = p.m_name;
        m_pTypeScope = p.m_pTypeScope;
        m_mdType = p.m_mdType;
        m_mdTokenNotToLoad = p.m_mdTokenNotToLoad;
        m_WhichTable = p.m_WhichTable;
        m_Bucket = p.m_Bucket;
    }

    void SetName(LPCUTF8 pName)
    {
        LIMITED_METHOD_CONTRACT;
        m_name = pName;
    }

    void SetName(LPCUTF8 pNameSpace, LPCUTF8 pName)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC_HOST_ONLY;

        m_nameSpace = pNameSpace;
        m_name = pName;
    }

    LPCUTF8 GetName() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_name;
    }

    LPCUTF8 GetNameSpace() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_nameSpace;
    }

    void SetTypeToken(Module* pModule, mdToken mdToken)
    {
        LIMITED_METHOD_CONTRACT;
        m_pTypeScope = dac_cast<PTR_Module>(pModule);
        m_mdType = mdToken;
    }

    PTR_Module GetTypeModule() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_pTypeScope;
    }

    mdToken GetTypeToken() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_mdType;
    }

    void SetTokenNotToLoad(mdToken mdtok)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;   // "this" must be a host address
        m_mdTokenNotToLoad = mdtok;
    }

    mdToken GetTokenNotToLoad() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_mdTokenNotToLoad;
    }

    void SetCaseInsensitive()
    {
        LIMITED_METHOD_CONTRACT;
        m_WhichTable = nhCaseInsensitive;
    }

    NameHandleTable GetTable() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_WhichTable;
    }

    void SetBucket(const HashedTypeEntry& bucket)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;   // "this" must be a host address
        m_Bucket = bucket;
    }


    const HashedTypeEntry& GetBucket() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_Bucket;
    }

    static BOOL OKToLoad(mdToken token, mdToken tokenNotToLoad)
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return (token == 0 || token != tokenNotToLoad) && tokenNotToLoad != tdAllTypes;
    }

    BOOL OKToLoad() const
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        return OKToLoad(m_mdType, m_mdTokenNotToLoad);
    }

};

//******************************************************************************
// AccessCheckContext encapsulates input of an accessibility check

class AccessCheckContext
{
public:

    AccessCheckContext(MethodDesc* pCallerMethod, MethodTable* pCallerType, Assembly* pCallerAssembly)
        : m_pCallerMethod(pCallerMethod),
          m_pCallerMT(pCallerType),
          m_pCallerAssembly(pCallerAssembly)
    {
        CONTRACTL
        {
            LIMITED_METHOD_CONTRACT;
            PRECONDITION(CheckPointer(pCallerMethod, NULL_OK));
            PRECONDITION(CheckPointer(pCallerType, NULL_OK));
            PRECONDITION(CheckPointer(pCallerAssembly));
        }
        CONTRACTL_END;
    }

    AccessCheckContext(MethodDesc* pCallerMethod);

    AccessCheckContext(MethodDesc* pCallerMethod, MethodTable* pCallerType);

    MethodDesc* GetCallerMethod()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pCallerMethod;
    }

    MethodTable* GetCallerMT()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pCallerMT;
    }

    Assembly* GetCallerAssembly()
    {
        WRAPPER_NO_CONTRACT;
        return m_pCallerAssembly;
    }

private:
    MethodDesc*     m_pCallerMethod;
    MethodTable*    m_pCallerMT;
    Assembly*       m_pCallerAssembly;
};

//******************************************************************************
// This type specifies the kind of accessibility checks to perform.
// On failure, it can be configured to either return FALSE or to throw an exception.
class AccessCheckOptions
{
public:
    enum AccessCheckType
    {
        // Used by statically compiled code.
        // Desktop: Just do normal accessibility checks. No security demands.
        // CoreCLR: Just do normal accessibility checks.
        kNormalAccessibilityChecks,

        // Used only for resource loading and reflection inovcation when the target is remoted.
        // Desktop: If normal accessiblity checks fail, return TRUE if a demand for MemberAccess succeeds
        // CoreCLR: If normal accessiblity checks fail, return TRUE if a the caller is Security(Safe)Critical
        kMemberAccess,

        // Used by Reflection invocation and DynamicMethod with RestrictedSkipVisibility.
        // Desktop: If normal accessiblity checks fail, return TRUE if a demand for RestrictedMemberAccess
        //          and grant set of the target assembly succeeds.
        // CoreCLR: If normal accessiblity checks fail, return TRUE if the callee is App transparent code (in a user assembly)
        kRestrictedMemberAccess,

        // Used by normal DynamicMethods in full trust CoreCLR
        // CoreCLR: Do normal visibility checks but bypass transparency checks.
        kNormalAccessNoTransparency,

        // Used by DynamicMethods with restrictedSkipVisibility in full trust CoreCLR
        // CoreCLR: Do RestrictedMemberAcess visibility checks but bypass transparency checks.
        kRestrictedMemberAccessNoTransparency,

    };

    AccessCheckOptions(
        AccessCheckType      accessCheckType,
        DynamicResolver *    pAccessContext,
        BOOL                 throwIfTargetIsInaccessible,
        MethodTable *        pTargetMT);

    AccessCheckOptions(
        AccessCheckType      accessCheckType,
        DynamicResolver *    pAccessContext,
        BOOL                 throwIfTargetIsInaccessible,
        MethodDesc *         pTargetMD);

    AccessCheckOptions(
        AccessCheckType      accessCheckType,
        DynamicResolver *    pAccessContext,
        BOOL                 throwIfTargetIsInaccessible,
        FieldDesc *          pTargetFD);

    AccessCheckOptions(
        const AccessCheckOptions & templateAccessCheckOptions,
        BOOL                       throwIfTargetIsInaccessible);

    // Follow standard rules for doing accessability
    BOOL DoNormalAccessibilityChecks() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_accessCheckType == kNormalAccessibilityChecks;
    }

    BOOL Throws() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_fThrowIfTargetIsInaccessible;
    }

    BOOL DemandMemberAccessOrFail(AccessCheckContext *pContext, MethodTable * pTargetMT, BOOL visibilityCheck) const;
    BOOL FailOrThrow(AccessCheckContext *pContext) const;

    static AccessCheckOptions* s_pNormalAccessChecks;

    static void Startup();

private:
    void Initialize(
        AccessCheckType     accessCheckType,
        BOOL                throwIfTargetIsInaccessible,
        MethodTable *       pTargetMT,
        MethodDesc *        pTargetMD,
        FieldDesc *         pTargetFD);

    BOOL DemandMemberAccess(AccessCheckContext *pContext, MethodTable * pTargetMT, BOOL visibilityCheck) const;

    void ThrowAccessException(
        AccessCheckContext* pContext,
        MethodTable*        pFailureMT = NULL,
        Exception*          pInnerException = NULL) const;

    MethodTable *           m_pTargetMT;
    MethodDesc *            m_pTargetMethod;
    FieldDesc *             m_pTargetField;

    AccessCheckType         m_accessCheckType;
    // The context used to determine if access is allowed. It is the resolver that carries the compressed-stack used to do the Demand.
    // If this is NULL, the access is checked against the current call-stack.
    // This is non-NULL only for m_accessCheckType==kRestrictedMemberAccess
    DynamicResolver *       m_pAccessContext;
    // If the target is not accessible, should the API return FALSE, or should it throw an exception?
    BOOL                    m_fThrowIfTargetIsInaccessible;
};

void DECLSPEC_NORETURN ThrowFieldAccessException(MethodDesc *pCallerMD,
                                                 FieldDesc *pFD,
                                                 UINT messageID = 0,
                                                 Exception *pInnerException = NULL);

void DECLSPEC_NORETURN ThrowMethodAccessException(MethodDesc *pCallerMD,
                                                  MethodDesc *pCalleeMD,
                                                  UINT messageID = 0,
                                                  Exception *pInnerException = NULL);

void DECLSPEC_NORETURN ThrowTypeAccessException(MethodDesc *pCallerMD,
                                                MethodTable *pMT,
                                                UINT messageID = 0,
                                                Exception *pInnerException = NULL);

void DECLSPEC_NORETURN ThrowFieldAccessException(AccessCheckContext* pContext,
                                                 FieldDesc *pFD,
                                                 UINT messageID = 0,
                                                 Exception *pInnerException = NULL);

void DECLSPEC_NORETURN ThrowMethodAccessException(AccessCheckContext* pContext,
                                                  MethodDesc *pCalleeMD,
                                                  UINT messageID = 0,
                                                  Exception *pInnerException = NULL);

void DECLSPEC_NORETURN ThrowTypeAccessException(AccessCheckContext* pContext,
                                                MethodTable *pMT,
                                                UINT messageID = 0,
                                                Exception *pInnerException = NULL);


//---------------------------------------------------------------------------------------
//
class ClassLoader
{
    friend class PendingTypeLoadEntry;
    friend class MethodTableBuilder;
    friend class AppDomain;
    friend class Assembly;
    friend class Module;
    friend class InstantiatedMethodDesc;

    // the following two classes are friends because they will call LoadTypeHandleForTypeKey by token directly
    friend class COMDynamicWrite;
    friend class COMModule;

private:
    // Classes for which load is in progress
    PendingTypeLoadTable  * m_pUnresolvedClassHash;
    CrstExplicitInit        m_UnresolvedClassLock;

    // Protects addition of elements to module's m_pAvailableClasses.
    // (indeed thus protects addition of elements to any m_pAvailableClasses in any
    // of the modules managed by this loader)
    CrstExplicitInit        m_AvailableClassLock;

    CrstExplicitInit        m_AvailableTypesLock;

    // Do we have any modules which need to have their classes added to
    // the available list?
    Volatile<LONG>       m_cUnhashedModules;

    // Back reference to the assembly
    PTR_Assembly        m_pAssembly;

public:

#ifdef _DEBUG
    DWORD               m_dwDebugMethods;
    DWORD               m_dwDebugFieldDescs; // Doesn't include anything we don't allocate a FieldDesc for
    DWORD               m_dwDebugClasses;
    DWORD               m_dwDebugDuplicateInterfaceSlots;
    DWORD               m_dwGCSize;
    DWORD               m_dwInterfaceMapSize;
    DWORD               m_dwMethodTableSize;
    DWORD               m_dwVtableData;
    DWORD               m_dwStaticFieldData;
    DWORD               m_dwFieldDescData;
    DWORD               m_dwMethodDescData;
    size_t              m_dwEEClassData;
#endif

public:
    ClassLoader(Assembly *pAssembly);
    ~ClassLoader();

private:

    VOID PopulateAvailableClassHashTable(Module *pModule,
                                         AllocMemTracker *pamTracker);

    void LazyPopulateCaseSensitiveHashTablesDontHaveLock();
    void LazyPopulateCaseSensitiveHashTables();
    void LazyPopulateCaseInsensitiveHashTables();

    // Lookup the hash table entry from the hash table
    void GetClassValue(NameHandleTable nhTable,
                                      const NameHandle *pName,
                                      HashDatum *pData,
                                      EEClassHashTable **ppTable,
                                      Module* pLookInThisModuleOnly,
                                      HashedTypeEntry* pFoundEntry,
                                      Loader::LoadFlag loadFlag,
                                      BOOL& needsToBuildHashtable);


public:
    //#LoaderModule
    // LoaderModule determines in which module an item gets placed.
    // For everything except paramaterized types and methods the choice is easy.
    //
    // If NGEN'ing we may choose to place the item into the current module (which is different from runtime behavior).
    //
    // The rule for determining the loader module must ensure that a type or method never outlives its loader module
    // with respect to app-domain unloading
    static Module * ComputeLoaderModule(MethodTable * pMT,
                                       mdToken        token,        // the token of the method
                                       Instantiation  methodInst);  // the type arguments to the method (if any)
    static Module * ComputeLoaderModule(TypeKey * typeKey);
    inline static PTR_Module ComputeLoaderModuleForFunctionPointer(TypeHandle * pRetAndArgTypes, DWORD NumArgsPlusRetType);
    inline static PTR_Module ComputeLoaderModuleForParamType(TypeHandle paramType);

private:
    static PTR_Module ComputeLoaderModuleWorker(Module *pDefinitionModule,      // the module that declares the generic type or method
                                          mdToken token,
                                          Instantiation classInst,        // the type arguments to the type (if any)
                                          Instantiation methodInst);      // the type arguments to the method (if any)

    BOOL FindClassModuleThrowing(
        const NameHandle *    pName,
        TypeHandle *          pType,
        mdToken *             pmdClassToken,
        Module **             ppModule,
        mdToken *             pmdFoundExportedType,
        HashedTypeEntry *     pEntry,
        Module *              pLookInThisModuleOnly,
        Loader::LoadFlag      loadFlag);

    static PTR_Module ComputeLoaderModuleForCompilation(Module *pDefinitionModule,      // the module that declares the generic type or method
                                                        mdToken token,
                                                        Instantiation classInst,        // the type arguments to the type (if any)
                                                        Instantiation methodInst);      // the type arguments to the method (if any)

public:
    void Init(AllocMemTracker *pamTracker);

    PTR_Assembly GetAssembly();
    DomainAssembly* GetDomainAssembly();

    void    FreeModules();

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    //==================================================================================
    // Main entry points to class loader
    // Organized as follows:
    //   by token:
    //       TypeDef
    //       TypeDefOrRef
    //       TypeDefOrRefOrSpec
    //   by constructed type:
    //       ArrayType
    //       PointerOrByrefType
    //       FnPtrType
    //       GenericInstantiation
    //   by name:
    //       ByName
    // Each takes a parameter comes, with the following semantics:
    //       fLoadTypes=DontLoadTypes:  if type isn't already in the loader's table, return NULL
    //       fLoadTypes=LoadTypes: if type isn't already in the loader's table, then create it
    // Each comes in two variants, LoadXThrowing and LoadXNoThrow, the latter being just
    // an exception-handling wrapper around the former.
    //
    // Each also allows types to be loaded only up to a particular level (see classloadlevel.h).
    // The class loader itself makes use of these levels to "break" recursion across
    // generic instantiations. External clients should leave the parameter at its default
    // value (CLASS_LOADED).
    //==================================================================================

public:

    // We use enums for these flags so that we can easily search the codebase to
    // determine where the flags are set to their non-default values.
    //
    // This enum tells us what to do if the load fails.  If ThrowIfNotFound is used
    // with a HRESULT-returning NOTHROW function then it actually indicates that
    // an error-HRESULT will be returned.
    // The ThrowButNullV11McppWorkaround value means ThrowIfNotFound, except when the case
    // of a Nil ResolutionScope for a value type (erroneously generated by Everett MCPP
    // compiler.)
    typedef enum { ThrowIfNotFound, ReturnNullIfNotFound, ThrowButNullV11McppWorkaround } NotFoundAction;

    // This flag indicates whether we should accept an uninstantiatednaked TypeDef or TypeRef
    // for a generic type definition, where "uninstantiated" means "not used as part of
    // a TypeSpec"
    typedef enum { FailIfUninstDefOrRef, PermitUninstDefOrRef } PermitUninstantiatedFlag;

    // This flag indicates whether we want to "load" the type if it isn't already in the
    // loader's tables and has reached the load level desired.
    typedef enum { LoadTypes, DontLoadTypes } LoadTypesFlag;


    // Load types by token (Def, Ref and Spec)
    static TypeHandle LoadTypeDefThrowing(Module *pModule,
                                          mdToken typeDef,
                                          NotFoundAction fNotFound = ThrowIfNotFound,
                                          PermitUninstantiatedFlag fUninstantiated = FailIfUninstDefOrRef,
                                          mdToken tokenNotToLoad = tdNoTypes,
                                          ClassLoadLevel level = CLASS_LOADED,
                                          Instantiation * pTargetInstantiation = NULL /* used to verify arity of the loaded type */);

    static TypeHandle LoadTypeDefOrRefThrowing(Module *pModule,
                                               mdToken typeRefOrDef,
                                               NotFoundAction fNotFound = ThrowIfNotFound,
                                               PermitUninstantiatedFlag fUninstantiated = FailIfUninstDefOrRef,
                                               mdToken tokenNotToLoad = tdNoTypes,
                                               ClassLoadLevel level = CLASS_LOADED);

    static TypeHandle LoadTypeDefOrRefOrSpecThrowing(Module *pModule,
                                                     mdToken typeRefOrDefOrSpec,
                                                     const SigTypeContext *pTypeContext,
                                                     NotFoundAction fNotFound = ThrowIfNotFound,
                                                     PermitUninstantiatedFlag fUninstantiated = FailIfUninstDefOrRef,
                                                     LoadTypesFlag fLoadTypes = LoadTypes,
                                                     ClassLoadLevel level = CLASS_LOADED,
                                                     BOOL dropGenericArgumentLevel = FALSE,
                                                     const Substitution *pSubst = NULL /* substitution to apply if the token is a type spec with generic variables */,
                                                     MethodTable *pMTInterfaceMapOwner = NULL);

    // Load constructed types by providing their constituents
    static TypeHandle LoadPointerOrByrefTypeThrowing(CorElementType typ,
                                                     TypeHandle baseType,
                                                     LoadTypesFlag fLoadTypes = LoadTypes,
                                                     ClassLoadLevel level = CLASS_LOADED);

    // The resulting type behaves like the unmanaged view of a given value type.
    static TypeHandle LoadNativeValueTypeThrowing(TypeHandle baseType,
                                                  LoadTypesFlag fLoadTypes = LoadTypes,
                                                  ClassLoadLevel level = CLASS_LOADED);

    static TypeHandle LoadArrayTypeThrowing(TypeHandle baseType,
                                            CorElementType typ = ELEMENT_TYPE_SZARRAY,
                                            unsigned rank = 0,
                                            LoadTypesFlag fLoadTypes = LoadTypes,
                                            ClassLoadLevel level = CLASS_LOADED);

    static TypeHandle LoadFnptrTypeThrowing(BYTE callConv,
                                            DWORD numArgs,
                                            TypeHandle* retAndArgTypes,
                                            LoadTypesFlag fLoadTypes = LoadTypes,
                                            ClassLoadLevel level = CLASS_LOADED);

    // Load types by name
    static TypeHandle LoadTypeByNameThrowing(Assembly *pAssembly,
                                             LPCUTF8 nameSpace,
                                             LPCUTF8 name,
                                             NotFoundAction fNotFound = ThrowIfNotFound,
                                             LoadTypesFlag fLoadTypes = LoadTypes,
                                             ClassLoadLevel level = CLASS_LOADED);

    // Resolve a TypeRef to a TypeDef
    // (Just a no-op on TypeDefs)
    // Return FALSE if operation failed (e.g. type does not exist)
    // *pfUsesTypeForwarder is set to TRUE if a type forwarder is found. It is never set to FALSE.
    static BOOL ResolveTokenToTypeDefThrowing(Module *         pTypeRefModule,
                                              mdTypeRef        typeRefToken,
                                              Module **        ppTypeDefModule,
                                              mdTypeDef *      pTypeDefToken,
                                              Loader::LoadFlag loadFlag = Loader::Load,
                                              BOOL *           pfUsesTypeForwarder = NULL);

    // Resolve a name to a TypeDef
    // Return FALSE if operation failed (e.g. type does not exist)
    // *pfUsesTypeForwarder is set to TRUE if a type forwarder is found. It is never set to FALSE.
    static BOOL ResolveNameToTypeDefThrowing(Module *         pTypeRefModule,
                                             const NameHandle * pName,
                                             Module **        ppTypeDefModule,
                                             mdTypeDef *      pTypeDefToken,
                                             Loader::LoadFlag loadFlag = Loader::Load,
                                             BOOL *           pfUsesTypeForwarder = NULL);

    static void EnsureLoaded(TypeHandle typeHnd, ClassLoadLevel level = CLASS_LOADED);
    static void TryEnsureLoaded(TypeHandle typeHnd, ClassLoadLevel level = CLASS_LOADED);

public:
    // Look up a class by name
    //
    // Guaranteed to only return NULL if pName->OKToLoad() returns FALSE.
    // Thus when type loads are enabled this will return non-null.
    TypeHandle LoadTypeHandleThrowIfFailed(NameHandle* pName, ClassLoadLevel level = CLASS_LOADED,
                                           Module* pLookInThisModuleOnly=NULL);

public:
    // Looks up class in the local module table, if it is there it succeeds,
    // Otherwise it fails, This is meant only for optimizations etc
    static TypeHandle LookupTypeDefOrRefInModule(Module *pModule, mdToken cl, ClassLoadLevel *pLoadLevel = NULL);

private:

    VOID AddAvailableClassDontHaveLock(Module *pModule,
                                       mdTypeDef classdef,
                                       AllocMemTracker *pamTracker);

    VOID AddAvailableClassHaveLock(Module *          pModule,
                                   mdTypeDef         classdef,
                                   AllocMemTracker * pamTracker);

    VOID AddExportedTypeDontHaveLock(Module *pManifestModule,
                                     mdExportedType cl,
                                     AllocMemTracker *pamTracker);

    VOID AddExportedTypeHaveLock(Module *pManifestModule,
                                 mdExportedType cl,
                                 AllocMemTracker *pamTracker);

public:

    // For an generic type instance return the representative within the class of
    // all type handles that share code.  For example,
    //    <int> --> <int>,
    //    <object> --> <__Canon>,
    //    <string> --> <__Canon>,
    //    <List<string>> --> <__Canon>,
    //    <Struct<string>> --> <Struct<__Canon>>
    //
    // If the code for the type handle is not shared then return
    // the type handle itself.
    static TypeHandle CanonicalizeGenericArg(TypeHandle genericArg);

    // Determine if the specified type representation induces a sharable
    // set of compatible instantiations when used as a type parameter to
    // a generic type or method.
    //
    // For example, when sharing at reference types "object" and "Struct<object>"
    // both induce sets of compatible instantiations, e.g. when used to build types
    // "List<object>" and "List<Struct<object>>" respectively.
    static BOOL IsSharableInstantiation(Instantiation inst);

    // Determine if it is normalized canonical generic instantiation.
    //      Dictionary<__Canon, __Canon> -> TRUE
    //      Dictionary<__Canon, int> -> TRUE
    //      Dictionary<__Canon, String> -> FALSE
    static BOOL IsCanonicalGenericInstantiation(Instantiation inst);

    // Determine if it is the entirely-canonical generic instantiation
    //      Dictionary<__Canon, __Canon> -> TRUE
    //      Dictionary<anything else> -> FALSE
    static BOOL IsTypicalSharedInstantiation(Instantiation inst);

    // Return TRUE if inst is the typical instantiation for the type or method specified by pModule/token
    static BOOL IsTypicalInstantiation(Module *pModule, mdToken token, Instantiation inst);

    // Load canonical shared instantiation for type key (each instantiation argument is
    // substituted by CanonicalizeGenericArg)
    static TypeHandle LoadCanonicalGenericInstantiation(TypeKey *pTypeKey,
                                                        LoadTypesFlag fLoadTypes/*=LoadTypes*/,
                                                        ClassLoadLevel level/*=CLASS_LOADED*/);

    // Create a generic instantiation.
    // If typeDef is not a generic type then throw an exception
    // If its arity does not match nGenericClassArgCount then throw an exception
    // The pointer to the instantiation is not persisted e.g. the type parameters can be stack-allocated.
    // If inst=NULL then <__Canon,...,__Canon> is assumed
    // If fLoadTypes=DontLoadTypes then the type handle is not created if it is not
    // already present in the tables.
    static TypeHandle LoadGenericInstantiationThrowing(Module *pModule,
                                                       mdTypeDef typeDef,
                                                       Instantiation inst,
                                                       LoadTypesFlag fLoadTypes = LoadTypes,
                                                       ClassLoadLevel level = CLASS_LOADED,
                                                       const InstantiationContext *pInstContext = NULL,
                                                       BOOL fFromNativeImage = FALSE);

// Public access Check APIs
public:

    static BOOL CanAccessClass(
        AccessCheckContext*     pContext,
        MethodTable*            pTargetClass,
        Assembly*               pTargetAssembly,
        const AccessCheckOptions &  accessCheckOptions = *AccessCheckOptions::s_pNormalAccessChecks);

    static BOOL CanAccess(
        AccessCheckContext*     pContext,
        MethodTable*            pTargetClass,
        Assembly*               pTargetAssembly,
        DWORD                   dwMemberAttrs,
        MethodDesc*             pOptionalTargetMethod,
        FieldDesc*              pOptionalTargetField,
        const AccessCheckOptions &  accessCheckOptions = *AccessCheckOptions::s_pNormalAccessChecks);

    static BOOL CanAccessFamilyVerification(
        TypeHandle              thCurrentClass,
        TypeHandle              thInstanceClass);

private:
    // Access check helpers
    static BOOL CanAccessMethodInstantiation(
        AccessCheckContext*     pContext,
        MethodDesc*             pOptionalTargetMethod,
        const AccessCheckOptions & accessCheckOptions);

    static BOOL CanAccessFamily(
        MethodTable*            pCurrentClass,
        MethodTable*            pTargetClass);

    static BOOL CheckAccessMember(
        AccessCheckContext*     pContext,
        MethodTable*            pTargetClass,
        Assembly*               pTargetAssembly,
        DWORD                   dwMemberAttrs,
        MethodDesc*             pOptionalTargetMethod,
        FieldDesc*              pOptionalTargetField,
        const AccessCheckOptions &  accessCheckOptions = *AccessCheckOptions::s_pNormalAccessChecks);


public:
    //Creates a key with both the namespace and name converted to lowercase and
    //made into a proper namespace-path.
    VOID CreateCanonicallyCasedKey(LPCUTF8 pszNameSpace, LPCUTF8 pszName,
                                      __out LPUTF8 *ppszOutNameSpace, __out LPUTF8 *ppszOutName);

    static HRESULT FindTypeDefByExportedType(IMDInternalImport *pCTImport,
                                             mdExportedType mdCurrent,
                                             IMDInternalImport *pTDImport,
                                             mdTypeDef *mtd);

    class AvailableClasses_LockHolder : public CrstHolder
    {
    public:
        AvailableClasses_LockHolder(ClassLoader *classLoader)
            : CrstHolder(&classLoader->m_AvailableClassLock)
        {
            WRAPPER_NO_CONTRACT;
        }
    };

    friend class AvailableClasses_LockHolder;

private:
    static TypeHandle LoadConstructedTypeThrowing(TypeKey *pKey,
                                                  LoadTypesFlag fLoadTypes = LoadTypes,
                                                  ClassLoadLevel level = CLASS_LOADED,
                                                  const InstantiationContext *pInstContext = NULL);

    static TypeHandle LookupTypeKeyUnderLock(TypeKey *pKey,
                                             EETypeHashTable *pTable,
                                             CrstBase *pLock);

    static TypeHandle LookupTypeKey(TypeKey *pKey,
                                    EETypeHashTable *pTable,
                                    CrstBase *pLock,
                                    BOOL fCheckUnderLock);

    static TypeHandle LookupInLoaderModule(TypeKey* pKey, BOOL fCheckUnderLock);

    // Lookup a handle in the appropriate table
    // (declaring module for TypeDef or loader-module for constructed types)
    static TypeHandle LookupTypeHandleForTypeKey(TypeKey *pTypeKey);
    static TypeHandle LookupTypeHandleForTypeKeyInner(TypeKey *pTypeKey, BOOL fCheckUnderLock);

    static void DECLSPEC_NORETURN  ThrowTypeLoadException(TypeKey *pKey, UINT resIDWhy);


    BOOL IsNested(const NameHandle* pName, mdToken *mdEncloser);
    static BOOL IsNested(Module *pModude, mdToken typeDefOrRef, mdToken *mdEncloser);

public:
    // Helpers for FindClassModule()
    BOOL CompareNestedEntryWithTypeDef(IMDInternalImport *pImport,
                                       mdTypeDef mdCurrent,
                                       EEClassHashTable *pClassHash,
                                       PTR_EEClassHashEntry pEntry);
    BOOL CompareNestedEntryWithTypeRef(IMDInternalImport *pImport,
                                       mdTypeRef mdCurrent,
                                       EEClassHashTable *pClassHash,
                                       PTR_EEClassHashEntry pEntry);
    BOOL CompareNestedEntryWithExportedType(IMDInternalImport *pImport,
                                            mdExportedType mdCurrent,
                                            EEClassHashTable *pClassHash,
                                            PTR_EEClassHashEntry pEntry);

    //Attempts to find/load/create a type handle but does not throw
    // if used in "find" mode.
    TypeHandle LoadTypeHandleThrowing(NameHandle* pName, ClassLoadLevel level = CLASS_LOADED,
                                      Module* pLookInThisModuleOnly=NULL);

    static void ValidateMethodsWithCovariantReturnTypes(MethodTable* pMT);

private:

#ifndef DACCESS_COMPILE
    // Perform a single phase of class loading
    // If no type handle has yet been created, typeHnd is null.
    static TypeHandle DoIncrementalLoad(TypeKey *pTypeKey,
                                        TypeHandle typeHnd,
                                        ClassLoadLevel workLevel);

    // Phase CLASS_LOAD_CREATE of class loading
    static TypeHandle CreateTypeHandleForTypeKey(TypeKey *pTypeKey,
                                                 AllocMemTracker *pamTracker);

    // Publish the type in the loader's tables
    static TypeHandle PublishType(TypeKey *pTypeKey, TypeHandle typeHnd);

    // Notify profiler and debugger that a type load has completed
    // Also update perf counters
    static void Notify(TypeHandle typeHnd);

    // Phase CLASS_LOAD_EXACTPARENTS of class loading
    // Load exact parents and interfaces and dependent structures (generics dictionary, vtable fixes)
    static void LoadExactParents(MethodTable* pMT);

    static void LoadExactParentAndInterfacesTransitively(MethodTable *pMT);

    static void PropagateCovariantReturnMethodImplSlots(MethodTable* pMT);

    static bool IsCompatibleWith(TypeHandle hType1, TypeHandle hType2);
    static CorElementType GetReducedTypeElementType(TypeHandle hType);
    static CorElementType GetVerificationTypeElementType(TypeHandle hType);
    static bool AreVerificationTypesEqual(TypeHandle hType1, TypeHandle hType2);
    static bool IsMethodSignatureCompatibleWith(FnPtrTypeDesc* fn1TD, FnPtrTypeDesc* fn2TD);

    // Create a non-canonical instantiation of a generic type based off the canonical instantiation
    // (For example, MethodTable for List<string> is based on the MethodTable for List<__Canon>)
    static TypeHandle CreateTypeHandleForNonCanonicalGenericInstantiation(TypeKey *pTypeKey,
                                                                          AllocMemTracker *pamTracker);

    // Loads a class. This is the inner call from the multi-threaded load. This load must
    // be protected in some manner.
    // If we're attempting to load a fresh instantiated type then genericArgs should be filled in

    static TypeHandle CreateTypeHandleForTypeDefThrowing(Module *pModule,
                                                         mdTypeDef cl,
                                                         Instantiation inst,
                                                         AllocMemTracker *pamTracker);

    // The token must be a type def.  GC must be enabled.
    // If we're attempting to load a fresh instantiated type then genericArgs should be filled in
    TypeHandle LoadTypeHandleForTypeKey(TypeKey *pTypeKey,
                                        TypeHandle typeHnd,
                                        ClassLoadLevel level = CLASS_LOADED,
                                        const InstantiationContext *pInstContext = NULL);

    TypeHandle LoadTypeHandleForTypeKeyNoLock(TypeKey *pTypeKey,
                                              ClassLoadLevel level = CLASS_LOADED,
                                              const InstantiationContext *pInstContext = NULL);

    // Used for initial loading of parent class and implemented interfaces
    // When tok represents an instantiated type return an *approximate* instantiated
    // type (where reference type arguments are replaced by Object)
    static
    TypeHandle
    LoadApproxTypeThrowing(
        Module *               pModule,
        mdToken                tok,
        SigPointer *           pSigInst,
        const SigTypeContext * pClassTypeContext);

    // Returns the parent of a token. The token must be a typedef.
    // If the parent is a shared constructed type (e.g. class C : List<string>) then
    // only the canonical instantiation is loaded at this point.
    // This is to avoid cycles in the loader e.g. on class C : D<C> or class C<T> : D<C<T>>
    // We fix up the exact parent later in LoadInstantiatedInfo.
    static
    MethodTable *
    LoadApproxParentThrowing(
        Module *               pModule,
        mdToken                cl,
        SigPointer *           pParentInst,
        const SigTypeContext * pClassTypeContext);

    // Locates the enclosing class of a token if any. The token must be a typedef.
    static VOID GetEnclosingClassThrowing(IMDInternalImport *pInternalImport,
                                          Module *pModule,
                                          mdTypeDef cl,
                                          mdTypeDef *tdEnclosing);

    // Insert the class in the classes hash table and if needed in the case insensitive one
    EEClassHashEntry_t *InsertValue(EEClassHashTable *pClassHash,
                                    EEClassHashTable *pClassCaseInsHash,
                                    LPCUTF8 pszNamespace,
                                    LPCUTF8 pszClassName,
                                    HashDatum Data,
                                    EEClassHashEntry_t *pEncloser,
                                    AllocMemTracker *pamTracker);

    // don't call this directly.
    TypeHandle LoadTypeHandleForTypeKey_Body(TypeKey *pTypeKey,
                                             TypeHandle typeHnd,
                                             ClassLoadLevel targetLevel);
#endif //!DACCESS_COMPILE

};  // class ClassLoader

#endif /* _H_CLSLOAD */
