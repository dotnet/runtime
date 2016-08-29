// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// This file contains the classes, methods, and field used by the EE from mscorlib

//
// To use this, define one of the following macros & include the file like so:
//
// #define DEFINE_CLASS(id, nameSpace, stringName)         CLASS__ ## id,
// #define DEFINE_METHOD(classId, id, stringName, gSign)
// #define DEFINE_FIELD(classId, id, stringName)
// #include "mscorlib.h"
//
// Note: To determine if the namespace you want to use in DEFINE_CLASS is supported or not, 
//       examine vm\namespace.h. If it is not present, define it there and then proceed to use it below.
//


// 
// Note: The SM_* and IM_* are signatures defined in file:metasig.h using IM() and SM() macros.
// 

#ifndef DEFINE_CLASS
#define DEFINE_CLASS(id, nameSpace, stringName)
#endif

#ifndef DEFINE_METHOD
#define DEFINE_METHOD(classId, id, stringName, gSign)
#endif

#ifndef DEFINE_FIELD
#define DEFINE_FIELD(classId, id, stringName)
#endif

#ifndef DEFINE_PROPERTY
#define DEFINE_PROPERTY(classId, id, stringName, gSign) DEFINE_METHOD(classId, GET_ ## id, get_ ## stringName, IM_Ret ## gSign)
#endif

#ifndef DEFINE_STATIC_PROPERTY
#define DEFINE_STATIC_PROPERTY(classId, id, stringName, gSign) DEFINE_METHOD(classId, GET_ ## id, get_ ## stringName, SM_Ret ## gSign)
#endif

#ifndef DEFINE_SET_PROPERTY
#define DEFINE_SET_PROPERTY(classId, id, stringName, gSign) \
    DEFINE_PROPERTY(classId, id, stringName, gSign) \
    DEFINE_METHOD(classId, SET_ ## id, set_ ## stringName, IM_## gSign ## _RetVoid)
#endif

//
// DEFINE_CLASS_U and DEFINE_FIELD_U are debug-only checks to verify that the managed and unmanaged layouts are in sync
//
#ifndef DEFINE_CLASS_U
#define DEFINE_CLASS_U(nameSpace, stringName, unmanagedType)
#endif

#ifndef DEFINE_FIELD_U
#define DEFINE_FIELD_U(stringName, unmanagedContainingType, unmanagedOffset)
#endif

// NOTE: Make this window really wide if you want to read the table...

DEFINE_CLASS(ACTIVATOR,             System,                 Activator)

DEFINE_CLASS(ACCESS_VIOLATION_EXCEPTION, System,            AccessViolationException)
DEFINE_FIELD(ACCESS_VIOLATION_EXCEPTION, IP,                _ip)
DEFINE_FIELD(ACCESS_VIOLATION_EXCEPTION, TARGET,            _target)
DEFINE_FIELD(ACCESS_VIOLATION_EXCEPTION, ACCESSTYPE,        _accessType)

DEFINE_CLASS_U(System,                 AppDomain,      AppDomainBaseObject)
DEFINE_FIELD_U(_domainManager,             AppDomainBaseObject, m_pDomainManager)
DEFINE_FIELD_U(_LocalStore,                AppDomainBaseObject, m_LocalStore)
DEFINE_FIELD_U(_FusionStore,               AppDomainBaseObject, m_FusionTable)
DEFINE_FIELD_U(_SecurityIdentity,          AppDomainBaseObject, m_pSecurityIdentity)
DEFINE_FIELD_U(_Policies,                  AppDomainBaseObject, m_pPolicies)
DEFINE_FIELD_U(AssemblyLoad,               AppDomainBaseObject, m_pAssemblyEventHandler)
DEFINE_FIELD_U(_TypeResolve,               AppDomainBaseObject, m_pTypeEventHandler)
DEFINE_FIELD_U(_ResourceResolve,           AppDomainBaseObject, m_pResourceEventHandler)
DEFINE_FIELD_U(_AssemblyResolve,           AppDomainBaseObject, m_pAsmResolveEventHandler)
#ifdef FEATURE_REFLECTION_ONLY_LOAD
DEFINE_FIELD_U(ReflectionOnlyAssemblyResolve,  AppDomainBaseObject, m_pReflectionAsmResolveEventHandler)
#endif
#ifdef FEATURE_REMOTING
DEFINE_FIELD_U(_DefaultContext,            AppDomainBaseObject, m_pDefaultContext)
#endif
#if defined(FEATURE_CLICKONCE)
DEFINE_FIELD_U(_activationContext,         AppDomainBaseObject, m_pActivationContext)
DEFINE_FIELD_U(_applicationIdentity,       AppDomainBaseObject, m_pApplicationIdentity)
#endif
DEFINE_FIELD_U(_applicationTrust,          AppDomainBaseObject, m_pApplicationTrust)
#ifdef FEATURE_IMPERSONATION
DEFINE_FIELD_U(_DefaultPrincipal,          AppDomainBaseObject, m_pDefaultPrincipal)
#endif // FEATURE_IMPERSONATION
#ifdef FEATURE_REMOTING
DEFINE_FIELD_U(_RemotingData,              AppDomainBaseObject, m_pURITable)
#endif
DEFINE_FIELD_U(_processExit,               AppDomainBaseObject, m_pProcessExitEventHandler)
DEFINE_FIELD_U(_domainUnload,              AppDomainBaseObject, m_pDomainUnloadEventHandler)
DEFINE_FIELD_U(_unhandledException,        AppDomainBaseObject, m_pUnhandledExceptionEventHandler)
#ifdef FEATURE_APTCA
DEFINE_FIELD_U(_aptcaVisibleAssemblies,  AppDomainBaseObject, m_aptcaVisibleAssemblies)
#endif
DEFINE_FIELD_U(_compatFlags,              AppDomainBaseObject, m_compatFlags)
#ifdef FEATURE_EXCEPTION_NOTIFICATIONS
DEFINE_FIELD_U(_firstChanceException,      AppDomainBaseObject, m_pFirstChanceExceptionHandler)
#endif // FEATURE_EXCEPTION_NOTIFICATIONS
DEFINE_FIELD_U(_pDomain,                   AppDomainBaseObject, m_pDomain)
#ifdef FEATURE_CAS_POLICY
DEFINE_FIELD_U(_PrincipalPolicy,           AppDomainBaseObject, m_iPrincipalPolicy)
#endif 
DEFINE_FIELD_U(_HasSetPolicy,                     AppDomainBaseObject, m_bHasSetPolicy)
DEFINE_FIELD_U(_IsFastFullTrustDomain,            AppDomainBaseObject, m_bIsFastFullTrustDomain)
DEFINE_FIELD_U(_compatFlagsInitialized,           AppDomainBaseObject, m_compatFlagsInitialized)

DEFINE_CLASS(APP_DOMAIN,            System,                 AppDomain)
DEFINE_METHOD(APP_DOMAIN,           PREPARE_DATA_FOR_SETUP,PrepareDataForSetup,SM_Str_AppDomainSetup_Evidence_Evidence_IntPtr_Str_ArrStr_ArrStr_RetObj)
DEFINE_METHOD(APP_DOMAIN,           SETUP,Setup,SM_Obj_RetObj)
DEFINE_METHOD(APP_DOMAIN,           ON_ASSEMBLY_LOAD,       OnAssemblyLoadEvent,        IM_Assembly_RetVoid)
DEFINE_METHOD(APP_DOMAIN,           ON_RESOURCE_RESOLVE,    OnResourceResolveEvent,     IM_Assembly_Str_RetAssembly)
DEFINE_METHOD(APP_DOMAIN,           ON_TYPE_RESOLVE,        OnTypeResolveEvent,         IM_Assembly_Str_RetAssembly)
DEFINE_METHOD(APP_DOMAIN,           ON_ASSEMBLY_RESOLVE,    OnAssemblyResolveEvent,     IM_Assembly_Str_RetAssembly)
#ifdef FEATURE_REFLECTION_ONLY_LOAD
DEFINE_METHOD(APP_DOMAIN,           ON_REFLECTION_ONLY_ASSEMBLY_RESOLVE, OnReflectionOnlyAssemblyResolveEvent, IM_Assembly_Str_RetAssembly) 
#ifdef FEATURE_COMINTEROP
DEFINE_METHOD(APP_DOMAIN,           ON_REFLECTION_ONLY_NAMESPACE_RESOLVE, OnReflectionOnlyNamespaceResolveEvent, IM_Assembly_Str_RetArrAssembly)
#endif //FEATURE_COMINTEROP
DEFINE_METHOD(APP_DOMAIN,           ENABLE_RESOLVE_ASSEMBLIES_FOR_INTROSPECTION, EnableResolveAssembliesForIntrospection, IM_Str_RetVoid)
#endif //FEATURE_REFLECTION_ONLY_LOAD
#ifdef FEATURE_COMINTEROP
DEFINE_METHOD(APP_DOMAIN,           ON_DESIGNER_NAMESPACE_RESOLVE, OnDesignerNamespaceResolveEvent, IM_Str_RetArrStr)
#endif //FEATURE_COMINTEROP
DEFINE_METHOD(APP_DOMAIN,           SETUP_DOMAIN,           SetupDomain,                IM_Bool_Str_Str_ArrStr_ArrStr_RetVoid)
#ifdef FEATURE_FUSION
DEFINE_METHOD(APP_DOMAIN,           SETUP_LOADER_OPTIMIZATION,SetupLoaderOptimization,  IM_LoaderOptimization_RetVoid)
DEFINE_METHOD(APP_DOMAIN,           SET_DOMAIN_CONTEXT,     InternalSetDomainContext,       IM_Str_RetVoid)
#endif // FEATURE_FUSION
#ifdef FEATURE_REMOTING
DEFINE_METHOD(APP_DOMAIN,           CREATE_DOMAIN,          CreateDomain,               SM_Str_Evidence_AppDomainSetup_RetAppDomain)
#ifdef FEATURE_CAS_POLICY
DEFINE_METHOD(APP_DOMAIN,           CREATE_DOMAINEX,        CreateDomain,               SM_Str_Evidence_Str_Str_Bool_RetAppDomain)
#endif // FEATURE_CAS_POLICY
DEFINE_METHOD(APP_DOMAIN,           VAL_CREATE_DOMAIN,      InternalCreateDomain,       SM_Str_RetAppDomain)
#endif
#ifdef FEATURE_REMOTING
DEFINE_METHOD(APP_DOMAIN,           MARSHAL_OBJECT,         MarshalObject,              SM_Obj_RetArrByte)
DEFINE_METHOD(APP_DOMAIN,           MARSHAL_OBJECTS,        MarshalObjects,             SM_Obj_Obj_RefArrByte_RetArrByte)
DEFINE_METHOD(APP_DOMAIN,           UNMARSHAL_OBJECT,       UnmarshalObject,            SM_ArrByte_RetObj)
DEFINE_METHOD(APP_DOMAIN,           UNMARSHAL_OBJECTS,      UnmarshalObjects,           SM_ArrByte_ArrByte_RefObj_RetObj)
#endif
#ifdef FEATURE_FUSION
DEFINE_METHOD(APP_DOMAIN,           TURN_ON_BINDING_REDIRECTS, TurnOnBindingRedirects,     IM_RetVoid)
#endif // FEATURE_FUSION
DEFINE_METHOD(APP_DOMAIN,           CREATE_APP_DOMAIN_MANAGER, CreateAppDomainManager,  IM_RetVoid)
DEFINE_METHOD(APP_DOMAIN,           INITIALIZE_COMPATIBILITY_FLAGS, InitializeCompatibilityFlags,  IM_RetVoid)
DEFINE_METHOD(APP_DOMAIN,           INITIALIZE_DOMAIN_SECURITY, InitializeDomainSecurity, IM_Evidence_Evidence_Bool_IntPtr_Bool_RetVoid)
#ifdef FEATURE_CLICKONCE
DEFINE_METHOD(APP_DOMAIN,           SETUP_DEFAULT_CLICKONCE_DOMAIN, SetupDefaultClickOnceDomain, IM_Str_ArrStr_ArrStr_RetVoid)
DEFINE_METHOD(APP_DOMAIN,           ACTIVATE_APPLICATION,   ActivateApplication,        IM_RetInt)
#endif // FEATURE_CLICKONCE
#ifdef FEATURE_APTCA
DEFINE_METHOD(APP_DOMAIN,           IS_ASSEMBLY_ON_APTCA_VISIBLE_LIST, IsAssemblyOnAptcaVisibleList, IM_Assembly_RetBool)
DEFINE_METHOD(APP_DOMAIN,           IS_ASSEMBLY_ON_APTCA_VISIBLE_LIST_RAW, IsAssemblyOnAptcaVisibleListRaw, IM_PtrChar_Int_PtrByte_Int_RetBool)
#endif // FEATURE_APTCA
#ifndef FEATURE_CORECLR
DEFINE_METHOD(APP_DOMAIN,           PAUSE, Pause, SM_RetVoid)
DEFINE_METHOD(APP_DOMAIN,           RESUME, Resume, SM_RetVoid)
DEFINE_CLASS(APPDOMAIN_MANAGER,     System,                 AppDomainManager)
DEFINE_PROPERTY(APPDOMAIN_MANAGER,  ENTRY_ASSEMBLY,         EntryAssembly,          AssemblyBase)
#endif // FEATURE_CORECLR

DEFINE_CLASS(CLEANUP_WORK_LIST,     StubHelpers,            CleanupWorkList)

#ifdef FEATURE_COMINTEROP
// Define earlier in mscorlib.h to avoid BinderClassID to const BYTE truncation warning
DEFINE_CLASS(DATETIMENATIVE,   StubHelpers,        DateTimeNative)
DEFINE_CLASS(TYPENAMENATIVE,   StubHelpers,        TypeNameNative)

DEFINE_CLASS_U(StubHelpers,     TypeNameNative,             TypeNameNative)
DEFINE_FIELD_U(typeName,        TypeNameNative,             typeName)
DEFINE_FIELD_U(typeKind,        TypeNameNative,             typeKind)

#endif

DEFINE_CLASS_U(Policy,                 ApplicationTrust,            ApplicationTrustObject)

#ifdef FEATURE_CLICKONCE
DEFINE_FIELD_U(m_appId,                ApplicationTrustObject,     _appId)
DEFINE_FIELD_U(m_extraInfo,            ApplicationTrustObject,     _extraInfo)
DEFINE_FIELD_U(m_elExtraInfo,          ApplicationTrustObject,     _elExtraInfo)
#endif // FEATURE_CLICKONCE

DEFINE_FIELD_U(m_psDefaultGrant,       ApplicationTrustObject,     _psDefaultGrant)
DEFINE_FIELD_U(m_fullTrustAssemblies,  ApplicationTrustObject,     _fullTrustAssemblies)
DEFINE_FIELD_U(m_grantSetSpecialFlags, ApplicationTrustObject,     _grantSetSpecialFlags)

#ifdef FEATURE_CLICKONCE
DEFINE_FIELD_U(m_appTrustedToRun,      ApplicationTrustObject,     _appTrustedToRun)
DEFINE_FIELD_U(m_persist,              ApplicationTrustObject,     _persist)
#endif // FEATURE_CLICKONCE

DEFINE_CLASS_U(Policy,                 PolicyStatement,             PolicyStatementObject)
DEFINE_FIELD_U(m_permSet,              PolicyStatementObject,      _permSet)
DEFINE_FIELD_U(m_attributes,           PolicyStatementObject,      _attributes)

DEFINE_CLASS(APPDOMAIN_SETUP,       System,                 AppDomainSetup)
DEFINE_CLASS_U(System,       AppDomainSetup,                 AppDomainSetupObject)
DEFINE_FIELD_U(_Entries,                           AppDomainSetupObject,   m_Entries)
DEFINE_FIELD_U(_AppBase,                           AppDomainSetupObject,   m_AppBase)
DEFINE_FIELD_U(_AppDomainInitializer,              AppDomainSetupObject,   m_AppDomainInitializer)
DEFINE_FIELD_U(_AppDomainInitializerArguments,     AppDomainSetupObject,   m_AppDomainInitializerArguments)
#ifdef FEATURE_CLICKONCE
DEFINE_FIELD_U(_ActivationArguments,               AppDomainSetupObject,   m_ActivationArguments)
#endif // FEATURE_CLICKONCE
DEFINE_FIELD_U(_ApplicationTrust,                  AppDomainSetupObject,   m_ApplicationTrust)
DEFINE_FIELD_U(_ConfigurationBytes,                AppDomainSetupObject,   m_ConfigurationBytes)
DEFINE_FIELD_U(_AppDomainManagerAssembly,          AppDomainSetupObject,   m_AppDomainManagerAssembly)
DEFINE_FIELD_U(_AppDomainManagerType,              AppDomainSetupObject,   m_AppDomainManagerType)
#if FEATURE_APTCA
DEFINE_FIELD_U(_AptcaVisibleAssemblies,            AppDomainSetupObject,   m_AptcaVisibleAssemblies)
#endif
DEFINE_FIELD_U(_CompatFlags,                       AppDomainSetupObject,   m_CompatFlags)
DEFINE_FIELD_U(_TargetFrameworkName,               AppDomainSetupObject,   m_TargetFrameworkName)
DEFINE_FIELD_U(_LoaderOptimization,                AppDomainSetupObject,   m_LoaderOptimization)
#ifndef FEATURE_CORECLR
DEFINE_FIELD_U(_AppDomainSortingSetupInfo,         AppDomainSetupObject,   m_AppDomainSortingSetupInfo)
#endif // FEATURE_CORECLR
#ifdef FEATURE_COMINTEROP
DEFINE_FIELD_U(_DisableInterfaceCache,             AppDomainSetupObject,   m_DisableInterfaceCache)
#endif // FEATURE_COMINTEROP
DEFINE_FIELD_U(_CheckedForTargetFrameworkName,     AppDomainSetupObject,   m_CheckedForTargetFrameworkName)
#ifdef FEATURE_RANDOMIZED_STRING_HASHING
DEFINE_FIELD_U(_UseRandomizedStringHashing,        AppDomainSetupObject,   m_UseRandomizedStringHashing)
#endif

DEFINE_CLASS(ARG_ITERATOR,          System,                 ArgIterator)
DEFINE_CLASS_U(System,              ArgIterator,            VARARGS)  // Includes a SigPointer.
DEFINE_METHOD(ARG_ITERATOR,         CTOR2,                  .ctor,                      IM_RuntimeArgumentHandle_PtrVoid_RetVoid)

DEFINE_CLASS(ARGUMENT_HANDLE,       System,                 RuntimeArgumentHandle)

DEFINE_CLASS(ARRAY,                 System,                 Array)
DEFINE_PROPERTY(ARRAY,              LENGTH,                 Length,                     Int)
DEFINE_METHOD(ARRAY,                GET_DATA_PTR_OFFSET_INTERNAL, GetDataPtrOffsetInternal, IM_RetInt)

#ifdef FEATURE_NONGENERIC_COLLECTIONS 
DEFINE_CLASS(ARRAY_LIST,            Collections,            ArrayList)
DEFINE_METHOD(ARRAY_LIST,           CTOR,                   .ctor,                      IM_RetVoid)
DEFINE_METHOD(ARRAY_LIST,           ADD,                    Add,                        IM_Obj_RetInt)
#endif // FEATURE_NONGENERIC_COLLECTIONS 

DEFINE_CLASS(ARRAY_WITH_OFFSET,     Interop,                ArrayWithOffset)                 
DEFINE_FIELD(ARRAY_WITH_OFFSET,     M_ARRAY,                m_array)
DEFINE_FIELD(ARRAY_WITH_OFFSET,     M_OFFSET,               m_offset)
DEFINE_FIELD(ARRAY_WITH_OFFSET,     M_COUNT,                m_count)


DEFINE_CLASS(ASSEMBLY_BUILDER,      ReflectionEmit,         AssemblyBuilder)
DEFINE_CLASS(INTERNAL_ASSEMBLY_BUILDER,      ReflectionEmit,         InternalAssemblyBuilder)

DEFINE_CLASS(ASSEMBLY_HASH_ALGORITHM,   Assemblies,         AssemblyHashAlgorithm)
DEFINE_CLASS(PORTABLE_EXECUTABLE_KINDS, Reflection,         PortableExecutableKinds)
DEFINE_CLASS(IMAGE_FILE_MACHINE,        Reflection,         ImageFileMachine)

DEFINE_CLASS_U(Reflection,             AssemblyName,           AssemblyNameBaseObject)
DEFINE_FIELD_U(_Name,                      AssemblyNameBaseObject, m_pSimpleName)
DEFINE_FIELD_U(_PublicKey,                 AssemblyNameBaseObject, m_pPublicKey)
DEFINE_FIELD_U(_PublicKeyToken,            AssemblyNameBaseObject, m_pPublicKeyToken)
DEFINE_FIELD_U(_CultureInfo,               AssemblyNameBaseObject, m_pCultureInfo)
DEFINE_FIELD_U(_CodeBase,                  AssemblyNameBaseObject, m_pCodeBase)
DEFINE_FIELD_U(_Version,                   AssemblyNameBaseObject, m_pVersion)
DEFINE_FIELD_U(m_siInfo,                   AssemblyNameBaseObject, m_siInfo)
DEFINE_FIELD_U(_HashForControl,            AssemblyNameBaseObject, m_HashForControl)
DEFINE_FIELD_U(_HashAlgorithm,             AssemblyNameBaseObject, m_HashAlgorithm)
DEFINE_FIELD_U(_HashAlgorithmForControl, AssemblyNameBaseObject, m_HashAlgorithmForControl)
DEFINE_FIELD_U(_VersionCompatibility,      AssemblyNameBaseObject, m_VersionCompatibility)
DEFINE_FIELD_U(_Flags,                     AssemblyNameBaseObject, m_Flags)
DEFINE_CLASS(ASSEMBLY_NAME,         Reflection,             AssemblyName)
DEFINE_METHOD(ASSEMBLY_NAME,        INIT,                   Init,                      IM_Str_ArrB_ArrB_Ver_CI_AHA_AVC_Str_ANF_SNKP_RetV)
DEFINE_METHOD(ASSEMBLY_NAME,        SET_PROC_ARCH_INDEX,    SetProcArchIndex,          IM_PEK_IFM_RetV)
#ifdef FEATURE_APTCA
DEFINE_METHOD(ASSEMBLY_NAME,        GET_NAME_WITH_PUBLIC_KEY, GetNameWithPublicKey,    IM_RetStr)
#endif // FEATURE_APTCA

DEFINE_CLASS_U(System,                 Version,                    VersionBaseObject)
DEFINE_FIELD_U(_Major,                     VersionBaseObject,    m_Major)
DEFINE_FIELD_U(_Minor,                     VersionBaseObject,    m_Minor)
DEFINE_FIELD_U(_Build,                     VersionBaseObject,    m_Build)
DEFINE_FIELD_U(_Revision,                  VersionBaseObject,    m_Revision)
DEFINE_CLASS(VERSION,               System,                 Version)
DEFINE_METHOD(VERSION,              CTOR,                   .ctor,                      IM_Int_Int_Int_Int_RetVoid)

DEFINE_CLASS(ASSEMBLY_VERSION_COMPATIBILITY, Assemblies,    AssemblyVersionCompatibility)

DEFINE_CLASS(ASSEMBLY_NAME_FLAGS,   Reflection,             AssemblyNameFlags)

// ASSEMBLYBASE is System.ReflectionAssembly while ASSEMBLY is System.Reflection.RuntimeAssembly
// Maybe we should reverse these two names
DEFINE_CLASS(ASSEMBLYBASE,          Reflection,             Assembly)

DEFINE_CLASS_U(Reflection,             RuntimeAssembly,            AssemblyBaseObject)
DEFINE_FIELD_U(_ModuleResolve,             AssemblyBaseObject,     m_pModuleEventHandler)
DEFINE_FIELD_U(m_fullname,                 AssemblyBaseObject,     m_fullname)
DEFINE_FIELD_U(m_syncRoot,                 AssemblyBaseObject,     m_pSyncRoot)
DEFINE_FIELD_U(m_assembly,                 AssemblyBaseObject,     m_pAssembly)
#ifndef FEATURE_CORECLR
DEFINE_FIELD_U(m_flags,                    AssemblyBaseObject,     m_flags)
#endif
DEFINE_CLASS(ASSEMBLY,              Reflection,             RuntimeAssembly)
DEFINE_FIELD(ASSEMBLY,              HANDLE,                 m_assembly)
DEFINE_METHOD(ASSEMBLY,             GET_NAME,               GetName,                    IM_RetAssemblyName)
#ifdef FEATURE_APTCA
DEFINE_METHOD(ASSEMBLY,             GET_NAME_FOR_CONDITIONAL_APTCA, GetNameForConditionalAptca, IM_RetStr)
#endif // FEATURE_APTCA
#ifdef FEATURE_FUSION
DEFINE_METHOD(ASSEMBLY,             LOAD_WITH_PARTIAL_NAME_HACK,  LoadWithPartialNameHack, SM_Str_Bool_RetAssembly)
#endif // FEATURE_FUSION
DEFINE_METHOD(ASSEMBLY,             ON_MODULE_RESOLVE,      OnModuleResolveEvent,       IM_Str_RetModule)
#ifdef FEATURE_FUSION
DEFINE_METHOD(ASSEMBLY,             DEMAND_PERMISSION,      DemandPermission,           SM_Str_Bool_Int_RetV)
#endif

#ifdef FEATURE_CAS_POLICY
DEFINE_CLASS(ASSEMBLY_EVIDENCE_FACTORY, Policy,             AssemblyEvidenceFactory)
DEFINE_METHOD(ASSEMBLY_EVIDENCE_FACTORY, UPGRADE_SECURITY_IDENTITY, UpgradeSecurityIdentity, SM_Evidence_Asm_RetEvidence)
#endif // FEATURE_CAS_POLICY

#ifdef FEATURE_COMINTEROP_REGISTRATION
DEFINE_CLASS(ASSEMBLY_REGISTRATION_FLAGS, Interop,          AssemblyRegistrationFlags)
#endif // FEATURE_COMINTEROP_REGISTRATION

#ifdef FEATURE_REMOTING
DEFINE_CLASS(ACTIVATION_SERVICES,   Activation,             ActivationServices)
DEFINE_METHOD(ACTIVATION_SERVICES,  IS_CURRENT_CONTEXT_OK,  IsCurrentContextOK,         SM_Class_ArrObject_Bool_RetMarshalByRefObject)

#ifdef FEATURE_CLASSIC_COMINTEROP
DEFINE_METHOD(ACTIVATION_SERVICES,  CREATE_OBJECT_FOR_COM,  CreateObjectForCom,         SM_Class_ArrObject_Bool_RetMarshalByRefObject)

#endif // FEATURE_CLASSIC_COMINTEROP
#endif // FEATURE_REMOTING

DEFINE_CLASS(ASYNCCALLBACK,         System,                 AsyncCallback)
DEFINE_CLASS(ATTRIBUTE,             System,                 Attribute)


DEFINE_CLASS(BINDER,                Reflection,             Binder)
DEFINE_METHOD(BINDER,               CHANGE_TYPE,            ChangeType,                 IM_Obj_Type_CultureInfo_RetObj)

DEFINE_CLASS(BINDING_FLAGS,         Reflection,             BindingFlags)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(BSTR_WRAPPER,          Interop,                BStrWrapper)
#endif // FEATURE_COMINTEROP

DEFINE_CLASS_U(System,                 RuntimeType,            ReflectClassBaseObject)
DEFINE_FIELD_U(m_cache,                ReflectClassBaseObject,        m_cache)
DEFINE_FIELD_U(m_handle,               ReflectClassBaseObject,        m_typeHandle)
DEFINE_FIELD_U(m_keepalive,            ReflectClassBaseObject,        m_keepalive)
#ifdef FEATURE_APPX
DEFINE_FIELD_U(m_invocationFlags,      ReflectClassBaseObject,        m_invocationFlags)
#endif
DEFINE_CLASS(CLASS,                 System,                 RuntimeType)
DEFINE_FIELD(CLASS,                 TYPEHANDLE,             m_handle)
DEFINE_METHOD(CLASS,                GET_PROPERTIES,         GetProperties,              IM_BindingFlags_RetArrPropertyInfo)
DEFINE_METHOD(CLASS,                GET_FIELDS,             GetFields,                  IM_BindingFlags_RetArrFieldInfo)
DEFINE_METHOD(CLASS,                GET_METHODS,            GetMethods,                 IM_BindingFlags_RetArrMethodInfo)
DEFINE_METHOD(CLASS,                INVOKE_MEMBER,          InvokeMember,               IM_Str_BindingFlags_Binder_Obj_ArrObj_ArrParameterModifier_CultureInfo_ArrStr_RetObj)
#if defined(FEATURE_CLASSIC_COMINTEROP) && defined(FEATURE_REMOTING)
DEFINE_METHOD(CLASS,                FORWARD_CALL_TO_INVOKE, ForwardCallToInvokeMember,  IM_Str_BindingFlags_Obj_ArrInt_RefMessageData_RetObj)
#endif
DEFINE_METHOD(CLASS,                GET_METHOD_BASE,        GetMethodBase,              SM_RuntimeType_RuntimeMethodHandleInternal_RetMethodBase)
DEFINE_METHOD(CLASS,                GET_FIELD_INFO,         GetFieldInfo,               SM_RuntimeType_IRuntimeFieldInfo_RetFieldInfo)
DEFINE_METHOD(CLASS,                GET_PROPERTY_INFO,      GetPropertyInfo,            SM_RuntimeType_Int_RetPropertyInfo)

DEFINE_CLASS(CLASS_INTROSPECTION_ONLY, System,              ReflectionOnlyType)

DEFINE_CLASS(CODE_ACCESS_PERMISSION, Security,              CodeAccessPermission)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS_U(System,                 __ComObject,            ComObject)
DEFINE_FIELD_U(m_ObjectToDataMap,      ComObject,              m_ObjectToDataMap)
DEFINE_CLASS(COM_OBJECT,            System,                 __ComObject)
DEFINE_METHOD(COM_OBJECT,           RELEASE_ALL_DATA,       ReleaseAllData,             IM_RetVoid)
DEFINE_METHOD(COM_OBJECT,           GET_EVENT_PROVIDER,     GetEventProvider,           IM_Class_RetObj)

DEFINE_CLASS(RUNTIME_CLASS,                  WinRT,         RuntimeClass)

#ifdef FEATURE_COMINTEROP_TLB_SUPPORT
DEFINE_CLASS(ITYPE_LIB_IMPORTER_NOTIFY_SINK, Interop,       ITypeLibImporterNotifySink)
DEFINE_CLASS(ITYPE_LIB_EXPORTER_NOTIFY_SINK, Interop,       ITypeLibExporterNotifySink)
#endif //FEATURE_COMINTEROP_TLB_SUPPORT

#endif // FEATURE_COMINTEROP

DEFINE_CLASS_U(Interop,                CriticalHandle,             CriticalHandle)
#ifdef _DEBUG
DEFINE_FIELD_U(_stackTrace,                CriticalHandle,     m_debugStackTrace)
#endif
DEFINE_FIELD_U(handle,                     CriticalHandle,     m_handle)
DEFINE_FIELD_U(_isClosed,                  CriticalHandle,     m_isClosed)
DEFINE_CLASS(CRITICAL_HANDLE,       Interop,                CriticalHandle)
DEFINE_FIELD(CRITICAL_HANDLE,       HANDLE,                 handle)
DEFINE_METHOD(CRITICAL_HANDLE,      RELEASE_HANDLE,         ReleaseHandle,              IM_RetBool)
DEFINE_METHOD(CRITICAL_HANDLE,      GET_IS_INVALID,         get_IsInvalid,              IM_RetBool)
DEFINE_METHOD(CRITICAL_HANDLE,      DISPOSE,                Dispose,                    IM_RetVoid)
DEFINE_METHOD(CRITICAL_HANDLE,      DISPOSE_BOOL,           Dispose,                    IM_Bool_RetVoid)

DEFINE_CLASS(CRITICAL_FINALIZER_OBJECT, ConstrainedExecution, CriticalFinalizerObject)
DEFINE_METHOD(CRITICAL_FINALIZER_OBJECT, FINALIZE,          Finalize,                   IM_RetVoid)

DEFINE_CLASS_U(Reflection,             RuntimeConstructorInfo,  NoClass)
DEFINE_FIELD_U(m_handle,                   ReflectMethodObject, m_pMD)
DEFINE_CLASS(CONSTRUCTOR,           Reflection,             RuntimeConstructorInfo)

DEFINE_CLASS_U(System,                 RuntimeMethodInfoStub,     ReflectMethodObject)
DEFINE_FIELD_U(m_value,                   ReflectMethodObject, m_pMD)
DEFINE_CLASS(STUBMETHODINFO,      System,                 RuntimeMethodInfoStub)
DEFINE_FIELD(STUBMETHODINFO,      HANDLE,                 m_value)

DEFINE_CLASS(CONSTRUCTOR_INFO,      Reflection,             ConstructorInfo)

DEFINE_CLASS_U(Reflection, CustomAttributeEncodedArgument, CustomAttributeValue)
DEFINE_FIELD_U(m_primitiveValue,   CustomAttributeValue,           m_rawValue)
DEFINE_FIELD_U(m_arrayValue,       CustomAttributeValue,           m_value)
DEFINE_FIELD_U(m_stringValue,      CustomAttributeValue,           m_enumOrTypeName)
DEFINE_FIELD_U(m_type,             CustomAttributeValue,           m_type)
DEFINE_CLASS(CUSTOM_ATTRIBUTE_ENCODED_ARGUMENT, Reflection, CustomAttributeEncodedArgument)

DEFINE_CLASS_U(Reflection, CustomAttributeNamedParameter, CustomAttributeNamedArgument)
DEFINE_FIELD_U(m_argumentName,     CustomAttributeNamedArgument,   m_argumentName)
DEFINE_FIELD_U(m_fieldOrProperty,  CustomAttributeNamedArgument,   m_propertyOrField)
DEFINE_FIELD_U(m_padding,          CustomAttributeNamedArgument,   m_padding)
DEFINE_FIELD_U(m_type,             CustomAttributeNamedArgument,   m_type)
DEFINE_FIELD_U(m_encodedArgument,  CustomAttributeNamedArgument,   m_value)

DEFINE_CLASS_U(Reflection, CustomAttributeCtorParameter, CustomAttributeArgument)
DEFINE_FIELD_U(m_type,             CustomAttributeArgument,        m_type)
DEFINE_FIELD_U(m_encodedArgument,  CustomAttributeArgument,        m_value)

DEFINE_CLASS_U(Reflection, CustomAttributeType, CustomAttributeType)
DEFINE_FIELD_U(m_enumName,         CustomAttributeType,            m_enumName)
DEFINE_FIELD_U(m_encodedType,      CustomAttributeType,            m_tag)
DEFINE_FIELD_U(m_encodedEnumType,  CustomAttributeType,            m_enumType)
DEFINE_FIELD_U(m_encodedArrayType, CustomAttributeType,            m_arrayType)
DEFINE_FIELD_U(m_padding,          CustomAttributeType,            m_padding)

#ifdef FEATURE_REMOTING
DEFINE_CLASS_U(Contexts,               Context,        ContextBaseObject)
DEFINE_FIELD_U(_ctxProps,                  ContextBaseObject, m_ctxProps)
DEFINE_FIELD_U(_dphCtx,                    ContextBaseObject, m_dphCtx)
DEFINE_FIELD_U(_localDataStore,            ContextBaseObject, m_localDataStore)
DEFINE_FIELD_U(_serverContextChain,        ContextBaseObject, m_serverContextChain)
DEFINE_FIELD_U(_clientContextChain,        ContextBaseObject, m_clientContextChain)
DEFINE_FIELD_U(_appDomain,                 ContextBaseObject, m_exposedAppDomain)
DEFINE_FIELD_U(_ctxStatics,                ContextBaseObject, m_ctxStatics)
DEFINE_FIELD_U(_internalContext,           ContextBaseObject, m_internalContext)
DEFINE_FIELD_U(_ctxID,                     ContextBaseObject, _ctxID)
DEFINE_FIELD_U(_ctxFlags,                  ContextBaseObject, _ctxFlags)
DEFINE_FIELD_U(_numCtxProps,               ContextBaseObject, _numCtxProps)
DEFINE_FIELD_U(_ctxStaticsCurrentBucket,   ContextBaseObject, _ctxStaticsCurrentBucket)
DEFINE_FIELD_U(_ctxStaticsFreeIndex,       ContextBaseObject, _ctxStaticsFreeIndex)
DEFINE_CLASS(CONTEXT,             Contexts,               Context)
DEFINE_METHOD(CONTEXT,              CALLBACK,               DoCallBackFromEE,           SM_IntPtr_IntPtr_Int_RetVoid)
DEFINE_METHOD(CONTEXT,              RESERVE_SLOT,           ReserveSlot,                IM_RetInt)
#endif

DEFINE_CLASS(CONTEXT_BOUND_OBJECT,  System,                 ContextBoundObject)


#ifdef FEATURE_CRYPTO
DEFINE_CLASS(CSP_PARAMETERS,        Cryptography,           CspParameters)

DEFINE_FIELD(CSP_PARAMETERS,        PROVIDER_TYPE,          ProviderType)
DEFINE_FIELD(CSP_PARAMETERS,        PROVIDER_NAME,          ProviderName)
DEFINE_FIELD(CSP_PARAMETERS,        KEY_CONTAINER_NAME,     KeyContainerName)
DEFINE_FIELD(CSP_PARAMETERS,        FLAGS,                  m_flags)
#endif //FEATURE_CRYPTO

#if defined(FEATURE_X509) || defined(FEATURE_CRYPTO)
DEFINE_CLASS(CRYPTO_EXCEPTION,      Cryptography,           CryptographicException)
DEFINE_METHOD(CRYPTO_EXCEPTION,     THROW,                  ThrowCryptographicException, SM_Int_RetVoid)
#endif // FEATURE_X509 || FEATURE_CRYPTO

#ifndef FEATURE_CORECLR
DEFINE_CLASS_U(Globalization,          AppDomainSortingSetupInfo,           AppDomainSortingSetupInfoObject)
DEFINE_FIELD_U(_pfnIsNLSDefinedString,             AppDomainSortingSetupInfoObject,   m_pfnIsNLSDefinedString)
DEFINE_FIELD_U(_pfnCompareStringEx,                AppDomainSortingSetupInfoObject,   m_pfnCompareStringEx)
DEFINE_FIELD_U(_pfnLCMapStringEx,                  AppDomainSortingSetupInfoObject,   m_pfnLCMapStringEx)
DEFINE_FIELD_U(_pfnFindNLSStringEx,                AppDomainSortingSetupInfoObject,   m_pfnFindNLSStringEx)
DEFINE_FIELD_U(_pfnCompareStringOrdinal,           AppDomainSortingSetupInfoObject,   m_pfnCompareStringOrdinal)
DEFINE_FIELD_U(_pfnGetNLSVersionEx,                AppDomainSortingSetupInfoObject,   m_pfnGetNLSVersionEx)
DEFINE_FIELD_U(_pfnFindStringOrdinal,              AppDomainSortingSetupInfoObject,   m_pfnFindStringOrdinal)
DEFINE_FIELD_U(_useV2LegacySorting,                AppDomainSortingSetupInfoObject,   m_useV2LegacySorting)
DEFINE_FIELD_U(_useV4LegacySorting,                AppDomainSortingSetupInfoObject,   m_useV4LegacySorting)
#endif // FEATURE_CORECLR

#ifndef FEATURE_COREFX_GLOBALIZATION
DEFINE_CLASS_U(Globalization,          CultureData,           CultureDataBaseObject)
DEFINE_FIELD_U(sRealName,             CultureDataBaseObject,  sRealName)
DEFINE_FIELD_U(sWindowsName,          CultureDataBaseObject,  sWindowsName)
DEFINE_FIELD_U(sName,                 CultureDataBaseObject,  sName)
DEFINE_FIELD_U(sParent,               CultureDataBaseObject,  sParent)
DEFINE_FIELD_U(sLocalizedDisplayName, CultureDataBaseObject,  sLocalizedDisplayName)
DEFINE_FIELD_U(sEnglishDisplayName,   CultureDataBaseObject,  sEnglishDisplayName)
DEFINE_FIELD_U(sNativeDisplayName,    CultureDataBaseObject,  sNativeDisplayName)
DEFINE_FIELD_U(sSpecificCulture,      CultureDataBaseObject,  sSpecificCulture)
DEFINE_FIELD_U(sISO639Language,       CultureDataBaseObject,  sISO639Language)
DEFINE_FIELD_U(sLocalizedLanguage,    CultureDataBaseObject,  sLocalizedLanguage)
DEFINE_FIELD_U(sEnglishLanguage,      CultureDataBaseObject,  sEnglishLanguage)
DEFINE_FIELD_U(sNativeLanguage,       CultureDataBaseObject,  sNativeLanguage)
DEFINE_FIELD_U(sRegionName,           CultureDataBaseObject,  sRegionName)
//DEFINE_FIELD_U(iCountry,              CultureDataBaseObject,  iCountry)
DEFINE_FIELD_U(iGeoId,                CultureDataBaseObject,  iGeoId)
DEFINE_FIELD_U(sLocalizedCountry,     CultureDataBaseObject,  sLocalizedCountry)
DEFINE_FIELD_U(sEnglishCountry,       CultureDataBaseObject,  sEnglishCountry)
DEFINE_FIELD_U(sNativeCountry,        CultureDataBaseObject,  sNativeCountry)
DEFINE_FIELD_U(sISO3166CountryName,   CultureDataBaseObject,  sISO3166CountryName)
DEFINE_FIELD_U(sPositiveSign,         CultureDataBaseObject,  sPositiveSign)
DEFINE_FIELD_U(sNegativeSign,         CultureDataBaseObject,  sNegativeSign)
DEFINE_FIELD_U(saNativeDigits,        CultureDataBaseObject,  saNativeDigits)
DEFINE_FIELD_U(iDigitSubstitution,    CultureDataBaseObject,  iDigitSubstitution)
DEFINE_FIELD_U(iLeadingZeros,         CultureDataBaseObject,  iLeadingZeros)
DEFINE_FIELD_U(iDigits,               CultureDataBaseObject,  iDigits)
DEFINE_FIELD_U(iNegativeNumber,       CultureDataBaseObject,  iNegativeNumber)
DEFINE_FIELD_U(waGrouping,            CultureDataBaseObject,  waGrouping)
DEFINE_FIELD_U(sDecimalSeparator,     CultureDataBaseObject,  sDecimalSeparator)
DEFINE_FIELD_U(sThousandSeparator,    CultureDataBaseObject,  sThousandSeparator)
DEFINE_FIELD_U(sNaN,                  CultureDataBaseObject,  sNaN)
DEFINE_FIELD_U(sPositiveInfinity,     CultureDataBaseObject,  sPositiveInfinity)
DEFINE_FIELD_U(sNegativeInfinity,     CultureDataBaseObject,  sNegativeInfinity)
DEFINE_FIELD_U(iNegativePercent,      CultureDataBaseObject,  iNegativePercent)
DEFINE_FIELD_U(iPositivePercent,      CultureDataBaseObject,  iPositivePercent)
DEFINE_FIELD_U(sPercent,              CultureDataBaseObject,  sPercent)
DEFINE_FIELD_U(sPerMille,             CultureDataBaseObject,  sPerMille)
DEFINE_FIELD_U(sCurrency,             CultureDataBaseObject,  sCurrency)
DEFINE_FIELD_U(sIntlMonetarySymbol,   CultureDataBaseObject,  sIntlMonetarySymbol)
DEFINE_FIELD_U(sEnglishCurrency,      CultureDataBaseObject,  sEnglishCurrency)
DEFINE_FIELD_U(sNativeCurrency,       CultureDataBaseObject,  sNativeCurrency)
DEFINE_FIELD_U(iCurrencyDigits,       CultureDataBaseObject,  iCurrencyDigits)
DEFINE_FIELD_U(iCurrency,             CultureDataBaseObject,  iCurrency)
DEFINE_FIELD_U(iNegativeCurrency,     CultureDataBaseObject,  iNegativeCurrency)
DEFINE_FIELD_U(waMonetaryGrouping,    CultureDataBaseObject,  waMonetaryGrouping)
DEFINE_FIELD_U(sMonetaryDecimal,      CultureDataBaseObject,  sMonetaryDecimal)
DEFINE_FIELD_U(sMonetaryThousand,     CultureDataBaseObject,  sMonetaryThousand)
DEFINE_FIELD_U(iMeasure,              CultureDataBaseObject,  iMeasure)
DEFINE_FIELD_U(sListSeparator,        CultureDataBaseObject,  sListSeparator)
//DEFINE_FIELD_U(iPaperSize,            CultureDataBaseObject,  iPaperSize)
//DEFINE_FIELD_U(waFontSignature,       CultureDataBaseObject,  waFontSignature)
DEFINE_FIELD_U(sAM1159,               CultureDataBaseObject,  sAM1159)
DEFINE_FIELD_U(sPM2359,               CultureDataBaseObject,  sPM2359)
DEFINE_FIELD_U(sTimeSeparator,        CultureDataBaseObject,  sTimeSeparator)
DEFINE_FIELD_U(saLongTimes,           CultureDataBaseObject,  saLongTimes)
DEFINE_FIELD_U(saShortTimes,          CultureDataBaseObject,  saShortTimes)
DEFINE_FIELD_U(saDurationFormats,     CultureDataBaseObject,  saDurationFormats)
DEFINE_FIELD_U(iFirstDayOfWeek,       CultureDataBaseObject,  iFirstDayOfWeek)
DEFINE_FIELD_U(iFirstWeekOfYear,      CultureDataBaseObject,  iFirstWeekOfYear)
DEFINE_FIELD_U(waCalendars,           CultureDataBaseObject,  waCalendars)
DEFINE_FIELD_U(calendars,             CultureDataBaseObject,  calendars)
DEFINE_FIELD_U(iReadingLayout,        CultureDataBaseObject,  iReadingLayout)
DEFINE_FIELD_U(sTextInfo,             CultureDataBaseObject,  sTextInfo)
DEFINE_FIELD_U(sCompareInfo,          CultureDataBaseObject,  sCompareInfo)
DEFINE_FIELD_U(sScripts,              CultureDataBaseObject,  sScripts)
DEFINE_FIELD_U(bUseOverrides,         CultureDataBaseObject,  bUseOverrides)
DEFINE_FIELD_U(bNeutral,              CultureDataBaseObject,  bNeutral)
DEFINE_FIELD_U(bWin32Installed,       CultureDataBaseObject,  bWin32Installed)
DEFINE_FIELD_U(bFramework,            CultureDataBaseObject,  bFramework)
#endif
#ifndef FEATURE_COREFX_GLOBALIZATION
DEFINE_CLASS_U(Globalization,          CalendarData,           CalendarDataBaseObject)
DEFINE_FIELD_U(sNativeName,            CalendarDataBaseObject, sNativeName)
DEFINE_FIELD_U(saShortDates,           CalendarDataBaseObject, saShortDates)
DEFINE_FIELD_U(saYearMonths,           CalendarDataBaseObject, saYearMonths)
DEFINE_FIELD_U(saLongDates,            CalendarDataBaseObject, saLongDates)
DEFINE_FIELD_U(sMonthDay,              CalendarDataBaseObject, sMonthDay)
DEFINE_FIELD_U(saEraNames,             CalendarDataBaseObject, saEraNames)
DEFINE_FIELD_U(saAbbrevEraNames,       CalendarDataBaseObject, saAbbrevEraNames)
DEFINE_FIELD_U(saAbbrevEnglishEraNames,CalendarDataBaseObject, saAbbrevEnglishEraNames)
DEFINE_FIELD_U(saDayNames,             CalendarDataBaseObject, saDayNames)
DEFINE_FIELD_U(saAbbrevDayNames,       CalendarDataBaseObject, saAbbrevDayNames)
DEFINE_FIELD_U(saSuperShortDayNames,   CalendarDataBaseObject, saSuperShortDayNames)
DEFINE_FIELD_U(saMonthNames,           CalendarDataBaseObject, saMonthNames)
DEFINE_FIELD_U(saAbbrevMonthNames,     CalendarDataBaseObject, saAbbrevMonthNames)
DEFINE_FIELD_U(saMonthGenitiveNames,   CalendarDataBaseObject, saMonthGenitiveNames)
DEFINE_FIELD_U(saAbbrevMonthGenitiveNames, CalendarDataBaseObject, saAbbrevMonthGenitiveNames)
DEFINE_FIELD_U(saLeapYearMonthNames,   CalendarDataBaseObject, saLeapYearMonthNames)
DEFINE_FIELD_U(iTwoDigitYearMax,       CalendarDataBaseObject, iTwoDigitYearMax)
DEFINE_FIELD_U(iCurrentEra,            CalendarDataBaseObject, iCurrentEra)
DEFINE_FIELD_U(bUseUserOverrides,      CalendarDataBaseObject, bUseUserOverrides)
#endif

DEFINE_CLASS_U(Globalization,          CultureInfo,        CultureInfoBaseObject)
DEFINE_FIELD_U(compareInfo,        CultureInfoBaseObject,  compareInfo)
DEFINE_FIELD_U(textInfo,           CultureInfoBaseObject,  textInfo)
DEFINE_FIELD_U(numInfo,            CultureInfoBaseObject,  numInfo)
DEFINE_FIELD_U(dateTimeInfo,       CultureInfoBaseObject,  dateTimeInfo)
DEFINE_FIELD_U(calendar,           CultureInfoBaseObject,  calendar)
#ifndef FEATURE_CORECLR
DEFINE_FIELD_U(m_consoleFallbackCulture, CultureInfoBaseObject, m_consoleFallbackCulture)
#endif // FEATURE_CORECLR
DEFINE_FIELD_U(m_name,             CultureInfoBaseObject,  m_name)
DEFINE_FIELD_U(m_nonSortName,      CultureInfoBaseObject,  m_nonSortName)
DEFINE_FIELD_U(m_sortName,         CultureInfoBaseObject,  m_sortName)
DEFINE_FIELD_U(m_parent,           CultureInfoBaseObject,  m_parent)
#ifdef FEATURE_LEAK_CULTURE_INFO
DEFINE_FIELD_U(m_createdDomainID,  CultureInfoBaseObject,  m_createdDomainID)
#endif // FEATURE_LEAK_CULTURE_INFO
DEFINE_FIELD_U(m_isReadOnly,       CultureInfoBaseObject,  m_isReadOnly)
DEFINE_FIELD_U(m_isInherited,      CultureInfoBaseObject,  m_isInherited)
#ifdef FEATURE_LEAK_CULTURE_INFO
DEFINE_FIELD_U(m_isSafeCrossDomain, CultureInfoBaseObject, m_isSafeCrossDomain)
#endif // FEATURE_LEAK_CULTURE_INFO
#ifndef FEATURE_COREFX_GLOBALIZATION
DEFINE_FIELD_U(m_useUserOverride,  CultureInfoBaseObject,  m_useUserOverride)
#endif
DEFINE_CLASS(CULTURE_INFO,          Globalization,          CultureInfo)
DEFINE_METHOD(CULTURE_INFO,         STR_CTOR,               .ctor,                      IM_Str_RetVoid)
DEFINE_FIELD(CULTURE_INFO,          CURRENT_CULTURE,        s_userDefaultCulture)
DEFINE_PROPERTY(CULTURE_INFO,       NAME,                   Name,                       Str)
#ifdef FEATURE_USE_LCID
DEFINE_METHOD(CULTURE_INFO,         INT_CTOR,               .ctor,                      IM_Int_RetVoid)
DEFINE_PROPERTY(CULTURE_INFO,       ID,                     LCID,                       Int)
#endif
DEFINE_PROPERTY(CULTURE_INFO,       PARENT,                 Parent,                     CultureInfo)

DEFINE_CLASS(CURRENCY,              System,                 Currency)
DEFINE_METHOD(CURRENCY,             DECIMAL_CTOR,           .ctor,                      IM_Dec_RetVoid)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(CURRENCY_WRAPPER,      Interop,                CurrencyWrapper)
#endif

DEFINE_CLASS(DATE_TIME,             System,                 DateTime)
DEFINE_METHOD(DATE_TIME,            LONG_CTOR,              .ctor,                      IM_Long_RetVoid)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(DATE_TIME_OFFSET,      System,                 DateTimeOffset)
#endif // FEATURE_COMINTEROP

DEFINE_CLASS(DECIMAL,               System,                 Decimal)      
DEFINE_METHOD(DECIMAL,              CURRENCY_CTOR,          .ctor,                      IM_Currency_RetVoid)

DEFINE_CLASS_U(System,                 Delegate,            NoClass)
DEFINE_FIELD_U(_target,                    DelegateObject,   _target)
DEFINE_FIELD_U(_methodBase,                DelegateObject,   _methodBase)
DEFINE_FIELD_U(_methodPtr,                 DelegateObject,   _methodPtr)
DEFINE_FIELD_U(_methodPtrAux,              DelegateObject,   _methodPtrAux)
DEFINE_CLASS(DELEGATE,              System,                 Delegate)
DEFINE_FIELD(DELEGATE,            TARGET,                 _target)
DEFINE_FIELD(DELEGATE,            METHOD_PTR,             _methodPtr)
DEFINE_FIELD(DELEGATE,            METHOD_PTR_AUX,         _methodPtrAux)
DEFINE_METHOD(DELEGATE,             CONSTRUCT_DELEGATE,     DelegateConstruct,          IM_Obj_IntPtr_RetVoid)
DEFINE_METHOD(DELEGATE,             GET_INVOKE_METHOD,      GetInvokeMethod,            IM_RetIntPtr)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(DISPATCH_WRAPPER,      Interop,                DispatchWrapper)
#endif // FEATURE_COMINTEROP

DEFINE_CLASS(DYNAMICMETHOD,         ReflectionEmit,         DynamicMethod)

DEFINE_CLASS(DYNAMICRESOLVER,       ReflectionEmit,         DynamicResolver)
DEFINE_FIELD(DYNAMICRESOLVER,       DYNAMIC_METHOD,         m_method)

DEFINE_CLASS(EMPTY,                 System,                 Empty)

DEFINE_CLASS(ENC_HELPER,            Diagnostics,            EditAndContinueHelper)
DEFINE_FIELD(ENC_HELPER,            OBJECT_REFERENCE,       _objectReference)

DEFINE_CLASS(ENCODING,              Text,                   Encoding)

DEFINE_CLASS(ENUM,                  System,                 Enum)

DEFINE_CLASS(ENVIRONMENT,           System,                 Environment)
DEFINE_METHOD(ENVIRONMENT,       GET_RESOURCE_STRING_LOCAL, GetResourceStringLocal,     SM_Str_RetStr)
#ifdef FEATURE_CORECLR
DEFINE_METHOD(ENVIRONMENT,       SET_COMMAND_LINE_ARGS,     SetCommandLineArgs,         SM_ArrStr_RetVoid)
#endif

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(ERROR_WRAPPER,         Interop,                ErrorWrapper)
#endif

DEFINE_CLASS(EVENT,                 Reflection,             RuntimeEventInfo)

DEFINE_CLASS(EVENT_ARGS,            System,                 EventArgs)

DEFINE_CLASS(EVENT_HANDLERGENERIC,  System,                 EventHandler`1)

DEFINE_CLASS(EVENT_INFO,            Reflection,             EventInfo)

DEFINE_CLASS(EVIDENCE,              Policy,                 Evidence)
#ifdef FEATURE_CAS_POLICY
// .ctor support for ICorRuntimeHost::CreateEvidence
DEFINE_METHOD(EVIDENCE,             CTOR,                   .ctor, IM_RetVoid)
DEFINE_METHOD(EVIDENCE,             WAS_STRONGNAME_EVIDENCE_USED, WasStrongNameEvidenceUsed, IM_RetBool)
#endif // FEATURE_CAS_POLICY

DEFINE_CLASS_U(System,                 Exception,      ExceptionObject)
DEFINE_FIELD_U(_className,         ExceptionObject,    _className)
DEFINE_FIELD_U(_exceptionMethod,   ExceptionObject,    _exceptionMethod)
DEFINE_FIELD_U(_exceptionMethodString,ExceptionObject, _exceptionMethodString)
DEFINE_FIELD_U(_message,           ExceptionObject,    _message)
DEFINE_FIELD_U(_data,              ExceptionObject,    _data)
DEFINE_FIELD_U(_innerException,    ExceptionObject,    _innerException)
DEFINE_FIELD_U(_helpURL,           ExceptionObject,    _helpURL)
DEFINE_FIELD_U(_source,            ExceptionObject,    _source)
DEFINE_FIELD_U(_stackTrace,        ExceptionObject,    _stackTrace)
DEFINE_FIELD_U(_watsonBuckets,     ExceptionObject,    _watsonBuckets)
DEFINE_FIELD_U(_stackTraceString,  ExceptionObject,    _stackTraceString)
DEFINE_FIELD_U(_remoteStackTraceString, ExceptionObject, _remoteStackTraceString)
DEFINE_FIELD_U(_dynamicMethods,    ExceptionObject,    _dynamicMethods)
DEFINE_FIELD_U(_xptrs,             ExceptionObject,    _xptrs)
#ifdef FEATURE_SERIALIZATION
DEFINE_FIELD_U(_safeSerializationManager, ExceptionObject, _safeSerializationManager)
#endif // FEATURE_SERIALIZATION
DEFINE_FIELD_U(_HResult,           ExceptionObject,    _HResult)
DEFINE_FIELD_U(_xcode,             ExceptionObject,    _xcode)
DEFINE_FIELD_U(_remoteStackIndex,  ExceptionObject,    _remoteStackIndex)
DEFINE_FIELD_U(_ipForWatsonBuckets,ExceptionObject,    _ipForWatsonBuckets)
DEFINE_CLASS(EXCEPTION,             System,                 Exception)
DEFINE_METHOD(EXCEPTION,            GET_CLASS_NAME,         GetClassName,               IM_RetStr)
DEFINE_PROPERTY(EXCEPTION,          MESSAGE,                Message,                    Str)
DEFINE_PROPERTY(EXCEPTION,          SOURCE,                 Source,                     Str)
DEFINE_PROPERTY(EXCEPTION,          HELP_LINK,              HelpLink,                   Str)
DEFINE_METHOD(EXCEPTION,            INTERNAL_TO_STRING,     InternalToString,           IM_RetStr)
DEFINE_METHOD(EXCEPTION,            TO_STRING,              ToString,                   IM_Bool_Bool_RetStr)
DEFINE_METHOD(EXCEPTION,            INTERNAL_PRESERVE_STACK_TRACE, InternalPreserveStackTrace, IM_RetVoid)
#ifdef FEATURE_COMINTEROP
DEFINE_METHOD(EXCEPTION,            ADD_EXCEPTION_DATA_FOR_RESTRICTED_ERROR_INFO, AddExceptionDataForRestrictedErrorInfo, IM_Str_Str_Str_Obj_Bool_RetVoid)
DEFINE_METHOD(EXCEPTION,            TRY_GET_RESTRICTED_LANGUAGE_ERROR_OBJECT,     TryGetRestrictedLanguageErrorObject, IM_RefObject_RetBool)
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_CORECLR

DEFINE_CLASS(CROSSAPPDOMAINMARSHALEDEXCEPTION,  System,      CrossAppDomainMarshaledException)
DEFINE_METHOD(CROSSAPPDOMAINMARSHALEDEXCEPTION, STR_INT_CTOR, .ctor, IM_Str_Int_RetVoid)

#endif //FEATURE_CORECLR


DEFINE_CLASS(SYSTEM_EXCEPTION,      System,                 SystemException)
DEFINE_METHOD(SYSTEM_EXCEPTION,     STR_EX_CTOR,            .ctor,                      IM_Str_Exception_RetVoid)


DEFINE_CLASS(TYPE_INIT_EXCEPTION,   System,                 TypeInitializationException)
DEFINE_METHOD(TYPE_INIT_EXCEPTION,  STR_EX_CTOR,            .ctor,                      IM_Str_Exception_RetVoid)

DEFINE_CLASS(THREAD_START_EXCEPTION,Threading,              ThreadStartException)
DEFINE_METHOD(THREAD_START_EXCEPTION,EX_CTOR,               .ctor,                      IM_Exception_RetVoid)

DEFINE_CLASS(TYPE_HANDLE,           System,                 RuntimeTypeHandle)
DEFINE_CLASS(RT_TYPE_HANDLE,        System,                 RuntimeTypeHandle)
DEFINE_METHOD(RT_TYPE_HANDLE,       GET_TYPE_HELPER,        GetTypeHelper,              SM_Type_ArrType_IntPtr_int_RetType)
DEFINE_METHOD(RT_TYPE_HANDLE,       PVOID_CTOR,             .ctor,                      IM_RuntimeType_RetVoid)
DEFINE_METHOD(RT_TYPE_HANDLE,       GETVALUEINTERNAL,       GetValueInternal,           SM_RuntimeTypeHandle_RetIntPtr)
DEFINE_FIELD(RT_TYPE_HANDLE,        M_TYPE,                 m_type)

DEFINE_CLASS_U(Reflection,             RtFieldInfo,         NoClass)
DEFINE_FIELD_U(m_fieldHandle,              ReflectFieldObject, m_pFD)
DEFINE_CLASS(RT_FIELD_INFO,         Reflection,             RtFieldInfo)
DEFINE_FIELD(RT_FIELD_INFO,         HANDLE,                 m_fieldHandle)

DEFINE_CLASS_U(System,                 RuntimeFieldInfoStub,       ReflectFieldObject)
DEFINE_FIELD_U(m_fieldHandle,              ReflectFieldObject, m_pFD)
DEFINE_CLASS(STUBFIELDINFO,         System,                 RuntimeFieldInfoStub)

DEFINE_CLASS(FIELD,                 Reflection,             RuntimeFieldInfo)
DEFINE_METHOD(FIELD,                SET_VALUE,              SetValue,                   IM_Obj_Obj_BindingFlags_Binder_CultureInfo_RetVoid)
DEFINE_METHOD(FIELD,                GET_VALUE,              GetValue,                   IM_Obj_RetObj)

DEFINE_CLASS(FIELD_HANDLE,          System,                 RuntimeFieldHandle)
DEFINE_FIELD(FIELD_HANDLE,          M_FIELD,                m_ptr)

DEFINE_CLASS(I_RT_FIELD_INFO,       System,                 IRuntimeFieldInfo)

DEFINE_CLASS(FIELD_INFO,            Reflection,             FieldInfo)

DEFINE_CLASS_U(IO,               FileStreamAsyncResult, AsyncResultBase)
DEFINE_FIELD_U(_userCallback,          AsyncResultBase,    _userCallback)
DEFINE_FIELD_U(_userStateObject,       AsyncResultBase,    _userStateObject)
DEFINE_FIELD_U(_waitHandle,            AsyncResultBase,    _waitHandle)
DEFINE_FIELD_U(_handle,                AsyncResultBase,    _fileHandle)
DEFINE_FIELD_U(_overlapped,            AsyncResultBase,    _overlapped)
DEFINE_FIELD_U(_EndXxxCalled,          AsyncResultBase,    _EndXxxCalled)
DEFINE_FIELD_U(_numBytes,              AsyncResultBase,    _numBytes)
DEFINE_FIELD_U(_errorCode,             AsyncResultBase,    _errorCode)
DEFINE_FIELD_U(_numBufferedBytes,      AsyncResultBase,    _numBufferedBytes)
DEFINE_FIELD_U(_isWrite,               AsyncResultBase,    _isWrite)
DEFINE_FIELD_U(_isComplete,            AsyncResultBase,    _isComplete)
DEFINE_FIELD_U(_completedSynchronously, AsyncResultBase, _completedSynchronously)
DEFINE_CLASS(FILESTREAM_ASYNCRESULT, IO,               FileStreamAsyncResult)

DEFINE_CLASS_U(Security,           FrameSecurityDescriptor, FrameSecurityDescriptorBaseObject)
DEFINE_FIELD_U(m_assertions,       FrameSecurityDescriptorBaseObject,  m_assertions)
DEFINE_FIELD_U(m_denials,          FrameSecurityDescriptorBaseObject,  m_denials)
DEFINE_FIELD_U(m_restriction,      FrameSecurityDescriptorBaseObject,  m_restriction)
DEFINE_FIELD_U(m_AssertFT,         FrameSecurityDescriptorBaseObject,  m_assertFT)
DEFINE_FIELD_U(m_assertAllPossible,FrameSecurityDescriptorBaseObject,  m_assertAllPossible)
DEFINE_FIELD_U(m_DeclarativeAssertions,       FrameSecurityDescriptorBaseObject,  m_DeclarativeAssertions)
DEFINE_FIELD_U(m_DeclarativeDenials,          FrameSecurityDescriptorBaseObject,  m_DeclarativeDenials)
DEFINE_FIELD_U(m_DeclarativeRestrictions,      FrameSecurityDescriptorBaseObject,  m_DeclarativeRestrictions)
#ifndef FEATURE_PAL
DEFINE_FIELD_U(m_callerToken,      FrameSecurityDescriptorBaseObject,  m_callerToken)
DEFINE_FIELD_U(m_impToken,         FrameSecurityDescriptorBaseObject,  m_impToken)
#endif
DEFINE_CLASS(FRAME_SECURITY_DESCRIPTOR, Security,           FrameSecurityDescriptor)

DEFINE_CLASS(GUID,                  System,                 Guid)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(HSTRING_HEADER_MANAGED, WinRT,                 HSTRING_HEADER)

DEFINE_CLASS(ICUSTOMPROPERTY,                 WinRT,                    ICustomProperty)
DEFINE_CLASS(ICUSTOMPROPERTYPROVIDERIMPL,     WinRT,                    ICustomPropertyProviderImpl)
DEFINE_METHOD(ICUSTOMPROPERTYPROVIDERIMPL,    CREATE_PROPERTY,          CreateProperty,           SM_Obj_Str_RetICustomProperty)
DEFINE_METHOD(ICUSTOMPROPERTYPROVIDERIMPL,    CREATE_INDEXED_PROPERTY,  CreateIndexedProperty,    SM_Obj_Str_PtrTypeName_RetICustomProperty)
DEFINE_METHOD(ICUSTOMPROPERTYPROVIDERIMPL,    GET_TYPE,                 GetType,                  SM_Obj_PtrTypeName_RetVoid)
DEFINE_CLASS(ICUSTOMPROPERTYPROVIDERPROXY,    WinRT,                    ICustomPropertyProviderProxy`2)
DEFINE_METHOD(ICUSTOMPROPERTYPROVIDERPROXY,   CREATE_INSTANCE,          CreateInstance,           SM_Obj_RetObj)

DEFINE_CLASS(FACTORYFORIREFERENCE,   WinRT,                 IReferenceFactory)
DEFINE_METHOD(FACTORYFORIREFERENCE,  CREATE_IREFERENCE,     CreateIReference,           SM_Obj_RetObj)
DEFINE_CLASS(CLRIREFERENCEIMPL,      WinRT,                 CLRIReferenceImpl`1)
DEFINE_METHOD(CLRIREFERENCEIMPL,     UNBOXHELPER,           UnboxHelper,                SM_Obj_RetObj)
DEFINE_CLASS(CLRIREFERENCEARRAYIMPL, WinRT,                 CLRIReferenceArrayImpl`1)
DEFINE_METHOD(CLRIREFERENCEARRAYIMPL,UNBOXHELPER,           UnboxHelper,                SM_Obj_RetObj)
DEFINE_CLASS(IREFERENCE,             WinRT,                 IReference`1)
DEFINE_CLASS(CLRIKEYVALUEPAIRIMPL,   WinRT,                 CLRIKeyValuePairImpl`2)
DEFINE_METHOD(CLRIKEYVALUEPAIRIMPL,  BOXHELPER,             BoxHelper,                  SM_Obj_RetObj)
DEFINE_METHOD(CLRIKEYVALUEPAIRIMPL,  UNBOXHELPER,           UnboxHelper,                SM_Obj_RetObj)

DEFINE_CLASS(WINDOWS_FOUNDATION_EVENTHANDLER,   WinRT,                 WindowsFoundationEventHandler`1)

DEFINE_CLASS(VARIANT,               System,                 Variant)
DEFINE_METHOD(VARIANT,              CONVERT_OBJECT_TO_VARIANT,MarshalHelperConvertObjectToVariant,SM_Obj_RefVariant_RetVoid)
DEFINE_METHOD(VARIANT,              CAST_VARIANT,           MarshalHelperCastVariant,   SM_Obj_Int_RefVariant_RetVoid)
DEFINE_METHOD(VARIANT,              CONVERT_VARIANT_TO_OBJECT,MarshalHelperConvertVariantToObject,SM_RefVariant_RetObject)
#endif // FEATURE_COMINTEROP

DEFINE_CLASS(IASYNCRESULT,          System,                 IAsyncResult)

DEFINE_CLASS(ICUSTOM_ATTR_PROVIDER, Reflection,             ICustomAttributeProvider)
DEFINE_METHOD(ICUSTOM_ATTR_PROVIDER,GET_CUSTOM_ATTRIBUTES,  GetCustomAttributes,        IM_Type_RetArrObj)

DEFINE_CLASS(ICUSTOM_MARSHALER,     Interop,                ICustomMarshaler)
DEFINE_METHOD(ICUSTOM_MARSHALER,    MARSHAL_NATIVE_TO_MANAGED,MarshalNativeToManaged,   IM_IntPtr_RetObj)
DEFINE_METHOD(ICUSTOM_MARSHALER,    MARSHAL_MANAGED_TO_NATIVE,MarshalManagedToNative,   IM_Obj_RetIntPtr)
DEFINE_METHOD(ICUSTOM_MARSHALER,    CLEANUP_NATIVE_DATA,    CleanUpNativeData,          IM_IntPtr_RetVoid)
DEFINE_METHOD(ICUSTOM_MARSHALER,    CLEANUP_MANAGED_DATA,   CleanUpManagedData,         IM_Obj_RetVoid)
DEFINE_METHOD(ICUSTOM_MARSHALER,    GET_NATIVE_DATA_SIZE,   GetNativeDataSize,         IM_RetInt)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(ICUSTOM_QUERYINTERFACE,      Interop,          ICustomQueryInterface)
DEFINE_METHOD(ICUSTOM_QUERYINTERFACE,     GET_INTERFACE,    GetInterface,                IM_RefGuid_OutIntPtr_RetCustomQueryInterfaceResult)
DEFINE_CLASS(CUSTOMQUERYINTERFACERESULT,  Interop,          CustomQueryInterfaceResult)
#endif //FEATURE_COMINTEROP

#ifdef FEATURE_REMOTING
DEFINE_CLASS(IDENTITY,              Remoting,               Identity)
DEFINE_FIELD(IDENTITY,              TP_OR_OBJECT,           _tpOrObject)
DEFINE_FIELD(IDENTITY,              LEASE,                  _lease)
DEFINE_FIELD(IDENTITY,              OBJURI,                 _ObjURI)
#endif

DEFINE_CLASS(ISERIALIZABLE,         Serialization,          ISerializable)
DEFINE_CLASS(IOBJECTREFERENCE,      Serialization,          IObjectReference)
DEFINE_CLASS(IDESERIALIZATIONCB,    Serialization,          IDeserializationCallback)
DEFINE_CLASS(STREAMING_CONTEXT,     Serialization,          StreamingContext)
DEFINE_CLASS(SERIALIZATION_INFO,    Serialization,          SerializationInfo)

#ifdef FEATURE_REMOTING
DEFINE_CLASS(OBJECTCLONEHELPER,     Serialization,          ObjectCloneHelper)
DEFINE_METHOD(OBJECTCLONEHELPER,    GET_OBJECT_DATA,        GetObjectData,              SM_Obj_OutStr_OutStr_OutArrStr_OutArrObj_RetObj)
DEFINE_METHOD(OBJECTCLONEHELPER,    PREPARE_DATA,           PrepareConstructorArgs,     SM_Obj_ArrStr_ArrObj_OutStreamingContext_RetSerializationInfo)
#endif


DEFINE_CLASS(IENUMERATOR,           Collections,            IEnumerator)

DEFINE_CLASS(IENUMERABLE,           Collections,            IEnumerable)
DEFINE_CLASS(ICOLLECTION,           Collections,            ICollection)
DEFINE_CLASS(ILIST,                 Collections,            IList)
DEFINE_CLASS(IDISPOSABLE,           System,                 IDisposable)

DEFINE_CLASS(IEXPANDO,              Expando,                IExpando)
DEFINE_METHOD(IEXPANDO,             ADD_FIELD,              AddField,                   IM_Str_RetFieldInfo)
DEFINE_METHOD(IEXPANDO,             REMOVE_MEMBER,          RemoveMember,               IM_MemberInfo_RetVoid)

DEFINE_CLASS(IPERMISSION,           Security,               IPermission)

DEFINE_CLASS(IPRINCIPAL,            Principal,              IPrincipal)

DEFINE_CLASS(IREFLECT,              Reflection,             IReflect)
DEFINE_METHOD(IREFLECT,             GET_PROPERTIES,         GetProperties,              IM_BindingFlags_RetArrPropertyInfo)
DEFINE_METHOD(IREFLECT,             GET_FIELDS,             GetFields,                  IM_BindingFlags_RetArrFieldInfo)
DEFINE_METHOD(IREFLECT,             GET_METHODS,            GetMethods,                 IM_BindingFlags_RetArrMethodInfo)
DEFINE_METHOD(IREFLECT,             INVOKE_MEMBER,          InvokeMember,               IM_Str_BindingFlags_Binder_Obj_ArrObj_ArrParameterModifier_CultureInfo_ArrStr_RetObj)

#ifdef FEATURE_ISOSTORE
#ifndef FEATURE_ISOSTORE_LIGHT
DEFINE_CLASS(ISS_STORE,             IsolatedStorage,        IsolatedStorage)
#endif // !FEATURE_ISOSTORE_LIGHT
DEFINE_CLASS(ISS_STORE_FILE,        IsolatedStorage,        IsolatedStorageFile)
DEFINE_CLASS(ISS_STORE_FILE_STREAM, IsolatedStorage,        IsolatedStorageFileStream)
#endif 

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(LCID_CONVERSION_TYPE,  Interop,                LCIDConversionAttribute)
#endif // FEATURE_COMINTEROP

DEFINE_CLASS(LOADER_OPTIMIZATION,   System,                 LoaderOptimization)

#ifdef FEATURE_REMOTING
DEFINE_CLASS_U(Messaging,            LogicalCallContext,      LogicalCallContextObject)
DEFINE_FIELD_U(m_Datastore,             LogicalCallContextObject,      m_Datastore)
DEFINE_FIELD_U(m_RemotingData,          LogicalCallContextObject,      m_RemotingData)
DEFINE_FIELD_U(m_SecurityData,          LogicalCallContextObject,      m_SecurityData)
DEFINE_FIELD_U(m_HostContext,           LogicalCallContextObject,      m_HostContext)
DEFINE_FIELD_U(m_IsCorrelationMgr,      LogicalCallContextObject,      m_IsCorrelationMgr)
DEFINE_FIELD_U(_sendHeaders,            LogicalCallContextObject,      _sendHeaders)
DEFINE_FIELD_U(_recvHeaders,            LogicalCallContextObject,      _recvHeaders)
#endif

DEFINE_CLASS(MARSHAL,               Interop,                Marshal)
#ifdef FEATURE_COMINTEROP
DEFINE_METHOD(MARSHAL,              LOAD_LICENSE_MANAGER,   LoadLicenseManager,         SM_Void_RetIntPtr)
DEFINE_METHOD(MARSHAL,              INITIALIZE_WRAPPER_FOR_WINRT, InitializeWrapperForWinRT, SM_Obj_RefIntPtr_RetVoid)
DEFINE_METHOD(MARSHAL,              GET_HR_FOR_EXCEPTION,              GetHRForException,             SM_Exception_RetInt)
DEFINE_METHOD(MARSHAL,              GET_HR_FOR_EXCEPTION_WINRT,        GetHRForException_WinRT,       SM_Exception_RetInt)
#endif // FEATURE_COMINTEROP
DEFINE_METHOD(MARSHAL,              GET_FUNCTION_POINTER_FOR_DELEGATE, GetFunctionPointerForDelegate, SM_Delegate_RetIntPtr)
DEFINE_METHOD(MARSHAL,              GET_DELEGATE_FOR_FUNCTION_POINTER, GetDelegateForFunctionPointer, SM_IntPtr_Type_RetDelegate)
DEFINE_METHOD(MARSHAL,              ALLOC_CO_TASK_MEM,                 AllocCoTaskMem,                SM_Int_RetIntPtr)
DEFINE_FIELD(MARSHAL,               SYSTEM_MAX_DBCS_CHAR_SIZE,         SystemMaxDBCSCharSize)

#ifdef FEATURE_REMOTING
DEFINE_CLASS_U(System,                 MarshalByRefObject,   MarshalByRefObjectBaseObject)
DEFINE_FIELD_U(__identity,               MarshalByRefObjectBaseObject,   m_ServerIdentity)
DEFINE_CLASS(MARSHAL_BY_REF_OBJECT, System,                 MarshalByRefObject)
#endif

DEFINE_CLASS(MEMBER,                Reflection,             MemberInfo)

#ifdef FEATURE_REMOTING
DEFINE_CLASS_U(Messaging,              Message,                    MessageObject)
DEFINE_FIELD_U(_MethodName,                MessageObject,       pMethodName)
DEFINE_FIELD_U(_MethodSignature,           MessageObject,       pMethodSig)
DEFINE_FIELD_U(_MethodBase,                MessageObject,       pMethodBase)
DEFINE_FIELD_U(_properties,                MessageObject,       pHashTable)
DEFINE_FIELD_U(_URI,                       MessageObject,       pURI)
DEFINE_FIELD_U(_typeName,                  MessageObject,       pTypeName)
DEFINE_FIELD_U(_Fault,                     MessageObject,       pFault)
DEFINE_FIELD_U(_ID,                        MessageObject,       pID)
DEFINE_FIELD_U(_srvID,                     MessageObject,       pSrvID)
DEFINE_FIELD_U(_argMapper,                 MessageObject,       pArgMapper)
DEFINE_FIELD_U(_callContext,               MessageObject,       pCallCtx)
DEFINE_FIELD_U(_frame,                     MessageObject,       pFrame)
DEFINE_FIELD_U(_methodDesc,                MessageObject,       pMethodDesc)
DEFINE_FIELD_U(_metaSigHolder,             MessageObject,       pMetaSigHolder)
DEFINE_FIELD_U(_delegateMD,                MessageObject,       pDelegateMD)
DEFINE_FIELD_U(_governingType,             MessageObject,       thGoverningType)
DEFINE_FIELD_U(_flags,                     MessageObject,       iFlags)
DEFINE_FIELD_U(_initDone,                  MessageObject,       initDone)

DEFINE_CLASS(MESSAGE_DATA,          Proxies,              MessageData)
#endif // FEATURE_REMOTING

DEFINE_CLASS_U(Reflection,             RuntimeMethodInfo,  NoClass)
DEFINE_FIELD_U(m_handle,                   ReflectMethodObject, m_pMD)
DEFINE_CLASS(METHOD,                Reflection,             RuntimeMethodInfo)
DEFINE_METHOD(METHOD,               INVOKE,                 Invoke,                     IM_Obj_BindingFlags_Binder_ArrObj_CultureInfo_RetObj)
DEFINE_METHOD(METHOD,               GET_PARAMETERS,         GetParameters,              IM_RetArrParameterInfo)

DEFINE_CLASS(METHOD_BASE,           Reflection,             MethodBase)
DEFINE_METHOD(METHOD_BASE,          GET_METHODDESC,         GetMethodDesc,              IM_RetIntPtr)

DEFINE_CLASS_U(Reflection,             ExceptionHandlingClause,    ExceptionHandlingClause)
DEFINE_FIELD_U(m_methodBody,               ExceptionHandlingClause,        m_methodBody)
DEFINE_FIELD_U(m_flags,                    ExceptionHandlingClause,        m_flags)
DEFINE_FIELD_U(m_tryOffset,                ExceptionHandlingClause,        m_tryOffset)
DEFINE_FIELD_U(m_tryLength,                ExceptionHandlingClause,        m_tryLength)
DEFINE_FIELD_U(m_handlerOffset,            ExceptionHandlingClause,        m_handlerOffset)
DEFINE_FIELD_U(m_handlerLength,            ExceptionHandlingClause,        m_handlerLength)
DEFINE_FIELD_U(m_catchMetadataToken,       ExceptionHandlingClause,        m_catchToken)
DEFINE_FIELD_U(m_filterOffset,             ExceptionHandlingClause,        m_filterOffset)
DEFINE_CLASS(EH_CLAUSE,             Reflection,             ExceptionHandlingClause)

DEFINE_CLASS_U(Reflection,             LocalVariableInfo,          LocalVariableInfo)
DEFINE_FIELD_U(m_type,                     LocalVariableInfo,        m_type)
DEFINE_FIELD_U(m_isPinned,                 LocalVariableInfo,        m_bIsPinned)
DEFINE_FIELD_U(m_localIndex,               LocalVariableInfo,        m_localIndex)
DEFINE_CLASS(LOCAL_VARIABLE_INFO,   Reflection,             LocalVariableInfo)

DEFINE_CLASS_U(Reflection,             MethodBody,                 MethodBody)
DEFINE_FIELD_U(m_IL,                       MethodBody,         m_IL)
DEFINE_FIELD_U(m_exceptionHandlingClauses, MethodBody,         m_exceptionClauses)
DEFINE_FIELD_U(m_localVariables,           MethodBody,         m_localVariables)
DEFINE_FIELD_U(m_methodBase,               MethodBody,         m_methodBase)
DEFINE_FIELD_U(m_localSignatureMetadataToken, MethodBody,      m_localVarSigToken)
DEFINE_FIELD_U(m_maxStackSize,             MethodBody,         m_maxStackSize)
DEFINE_FIELD_U(m_initLocals,               MethodBody,         m_initLocals)
DEFINE_CLASS(METHOD_BODY,           Reflection,             MethodBody)

DEFINE_CLASS(METHOD_INFO,           Reflection,             MethodInfo)

DEFINE_CLASS(METHOD_HANDLE_INTERNAL,System,                 RuntimeMethodHandleInternal)

DEFINE_CLASS(METHOD_HANDLE,         System,                 RuntimeMethodHandle)
DEFINE_FIELD(METHOD_HANDLE,         METHOD,                 m_value)
DEFINE_METHOD(METHOD_HANDLE,        GETVALUEINTERNAL,       GetValueInternal,           SM_RuntimeMethodHandle_RetIntPtr)

#ifdef FEATURE_METHOD_RENTAL
DEFINE_CLASS(METHOD_RENTAL,         ReflectionEmit,         MethodRental)
#endif // FEATURE_METHOD_RENTAL

DEFINE_CLASS(MISSING,               Reflection,             Missing)
DEFINE_FIELD(MISSING,               VALUE,                  Value)

DEFINE_CLASS_U(Reflection,             RuntimeModule,               ReflectModuleBaseObject)
DEFINE_FIELD_U(m_runtimeType,               ReflectModuleBaseObject,    m_runtimeType)
DEFINE_FIELD_U(m_pRefClass,                 ReflectModuleBaseObject,    m_ReflectClass)
DEFINE_FIELD_U(m_pData,                     ReflectModuleBaseObject,    m_pData)
DEFINE_FIELD_U(m_pGlobals,                  ReflectModuleBaseObject,    m_pGlobals)
DEFINE_FIELD_U(m_pFields,                   ReflectModuleBaseObject,    m_pGlobalsFlds)
DEFINE_CLASS(MODULE,                Reflection,             RuntimeModule)
DEFINE_FIELD(MODULE,                DATA,                   m_pData)

DEFINE_CLASS(MODULE_BUILDER,        ReflectionEmit,         InternalModuleBuilder)
DEFINE_CLASS(TYPE_BUILDER,          ReflectionEmit,         TypeBuilder)
DEFINE_CLASS(ENUM_BUILDER,          ReflectionEmit,         EnumBuilder)

DEFINE_CLASS_U(System,                 MulticastDelegate,          DelegateObject)
DEFINE_FIELD_U(_invocationList,            DelegateObject,   _invocationList)
DEFINE_FIELD_U(_invocationCount,           DelegateObject,   _invocationCount)
DEFINE_CLASS(MULTICAST_DELEGATE,    System,                 MulticastDelegate)
DEFINE_FIELD(MULTICAST_DELEGATE,    INVOCATION_LIST,        _invocationList)
DEFINE_FIELD(MULTICAST_DELEGATE,    INVOCATION_COUNT,       _invocationCount)
DEFINE_METHOD(MULTICAST_DELEGATE,   CTOR_CLOSED,            CtorClosed,                 IM_Obj_IntPtr_RetVoid)
DEFINE_METHOD(MULTICAST_DELEGATE,   CTOR_CLOSED_STATIC,     CtorClosedStatic,           IM_Obj_IntPtr_RetVoid)
DEFINE_METHOD(MULTICAST_DELEGATE,   CTOR_RT_CLOSED,         CtorRTClosed,               IM_Obj_IntPtr_RetVoid)
DEFINE_METHOD(MULTICAST_DELEGATE,   CTOR_OPENED,            CtorOpened,                 IM_Obj_IntPtr_IntPtr_RetVoid)
DEFINE_METHOD(MULTICAST_DELEGATE,   CTOR_SECURE_CLOSED,     CtorSecureClosed,           IM_Obj_IntPtr_IntPtr_IntPtr_RetVoid)
DEFINE_METHOD(MULTICAST_DELEGATE,   CTOR_SECURE_CLOSED_STATIC,CtorSecureClosedStatic,   IM_Obj_IntPtr_IntPtr_IntPtr_RetVoid)
DEFINE_METHOD(MULTICAST_DELEGATE,   CTOR_SECURE_RT_CLOSED,  CtorSecureRTClosed,         IM_Obj_IntPtr_IntPtr_IntPtr_RetVoid)
DEFINE_METHOD(MULTICAST_DELEGATE,   CTOR_SECURE_OPENED,     CtorSecureOpened,           IM_Obj_IntPtr_IntPtr_IntPtr_IntPtr_RetVoid)
DEFINE_METHOD(MULTICAST_DELEGATE,   CTOR_VIRTUAL_DISPATCH,  CtorVirtualDispatch,        IM_Obj_IntPtr_IntPtr_RetVoid)
DEFINE_METHOD(MULTICAST_DELEGATE,   CTOR_SECURE_VIRTUAL_DISPATCH,  CtorSecureVirtualDispatch, IM_Obj_IntPtr_IntPtr_IntPtr_IntPtr_RetVoid)
DEFINE_METHOD(MULTICAST_DELEGATE,   CTOR_COLLECTIBLE_CLOSED_STATIC,     CtorCollectibleClosedStatic,           IM_Obj_IntPtr_IntPtr_RetVoid)
DEFINE_METHOD(MULTICAST_DELEGATE,   CTOR_COLLECTIBLE_OPENED,            CtorCollectibleOpened,                 IM_Obj_IntPtr_IntPtr_IntPtr_RetVoid)
DEFINE_METHOD(MULTICAST_DELEGATE,   CTOR_COLLECTIBLE_VIRTUAL_DISPATCH,  CtorCollectibleVirtualDispatch,        IM_Obj_IntPtr_IntPtr_IntPtr_RetVoid)

DEFINE_CLASS(NULL,                  System,                 DBNull)
DEFINE_FIELD(NULL,                  VALUE,          Value)

DEFINE_CLASS(NULLABLE,              System,                 Nullable`1)

// Keep this in sync with System.Globalization.NumberFormatInfo
DEFINE_CLASS_U(Globalization,       NumberFormatInfo,   NumberFormatInfo)
DEFINE_FIELD_U(numberGroupSizes,       NumberFormatInfo,   cNumberGroup)
DEFINE_FIELD_U(currencyGroupSizes,     NumberFormatInfo,   cCurrencyGroup)
DEFINE_FIELD_U(percentGroupSizes,      NumberFormatInfo,   cPercentGroup)
DEFINE_FIELD_U(positiveSign,           NumberFormatInfo,   sPositive)
DEFINE_FIELD_U(negativeSign,           NumberFormatInfo,   sNegative)
DEFINE_FIELD_U(numberDecimalSeparator, NumberFormatInfo,   sNumberDecimal)
DEFINE_FIELD_U(numberGroupSeparator,   NumberFormatInfo,   sNumberGroup)
DEFINE_FIELD_U(currencyGroupSeparator, NumberFormatInfo,   sCurrencyGroup)
DEFINE_FIELD_U(currencyDecimalSeparator,NumberFormatInfo,   sCurrencyDecimal)
DEFINE_FIELD_U(currencySymbol,         NumberFormatInfo,   sCurrency)
#ifndef FEATURE_COREFX_GLOBALIZATION
DEFINE_FIELD_U(ansiCurrencySymbol,     NumberFormatInfo,   sAnsiCurrency)
#endif
DEFINE_FIELD_U(nanSymbol,              NumberFormatInfo,   sNaN)
DEFINE_FIELD_U(positiveInfinitySymbol, NumberFormatInfo,   sPositiveInfinity)
DEFINE_FIELD_U(negativeInfinitySymbol, NumberFormatInfo,   sNegativeInfinity)
DEFINE_FIELD_U(percentDecimalSeparator,NumberFormatInfo,   sPercentDecimal)
DEFINE_FIELD_U(percentGroupSeparator,  NumberFormatInfo,   sPercentGroup)
DEFINE_FIELD_U(percentSymbol,          NumberFormatInfo,   sPercent)
DEFINE_FIELD_U(perMilleSymbol,         NumberFormatInfo,   sPerMille)
DEFINE_FIELD_U(nativeDigits,           NumberFormatInfo,   sNativeDigits)
#ifndef FEATURE_COREFX_GLOBALIZATION
DEFINE_FIELD_U(m_dataItem,             NumberFormatInfo,   iDataItem)
#endif
DEFINE_FIELD_U(numberDecimalDigits,    NumberFormatInfo,   cNumberDecimals)
DEFINE_FIELD_U(currencyDecimalDigits, NumberFormatInfo,   cCurrencyDecimals)
DEFINE_FIELD_U(currencyPositivePattern,NumberFormatInfo,   cPosCurrencyFormat)
DEFINE_FIELD_U(currencyNegativePattern,NumberFormatInfo,   cNegCurrencyFormat)
DEFINE_FIELD_U(numberNegativePattern,  NumberFormatInfo,   cNegativeNumberFormat)
DEFINE_FIELD_U(percentPositivePattern, NumberFormatInfo,   cPositivePercentFormat)
DEFINE_FIELD_U(percentNegativePattern, NumberFormatInfo,   cNegativePercentFormat)
DEFINE_FIELD_U(percentDecimalDigits,   NumberFormatInfo,   cPercentDecimals)
#ifndef FEATURE_COREFX_GLOBALIZATION
DEFINE_FIELD_U(digitSubstitution,      NumberFormatInfo,   iDigitSubstitution)
#endif
DEFINE_FIELD_U(isReadOnly,             NumberFormatInfo,   bIsReadOnly)
#ifndef FEATURE_COREFX_GLOBALIZATION
DEFINE_FIELD_U(m_useUserOverride,      NumberFormatInfo,   bUseUserOverride)
#endif
DEFINE_FIELD_U(m_isInvariant,          NumberFormatInfo,   bIsInvariant)
#ifndef FEATURE_COREFX_GLOBALIZATION
DEFINE_FIELD_U(validForParseAsNumber,  NumberFormatInfo,   bvalidForParseAsNumber)
DEFINE_FIELD_U(validForParseAsCurrency,NumberFormatInfo,   bvalidForParseAsCurrency)
#endif

// Defined as element type alias
// DEFINE_CLASS(OBJECT,                System,                 Object)
DEFINE_METHOD(OBJECT,               CTOR,                   .ctor,                      IM_RetVoid)
DEFINE_METHOD(OBJECT,               FINALIZE,               Finalize,                   IM_RetVoid)
DEFINE_METHOD(OBJECT,               TO_STRING,              ToString,                   IM_RetStr)
DEFINE_METHOD(OBJECT,               GET_TYPE,               GetType,                    IM_RetType)
DEFINE_METHOD(OBJECT,               GET_HASH_CODE,          GetHashCode,                IM_RetInt)
DEFINE_METHOD(OBJECT,               EQUALS,                 Equals,                     IM_Obj_RetBool)
DEFINE_METHOD(OBJECT,               FIELD_SETTER,           FieldSetter,                IM_Str_Str_Obj_RetVoid)
DEFINE_METHOD(OBJECT,               FIELD_GETTER,           FieldGetter,                IM_Str_Str_RefObj_RetVoid)

DEFINE_CLASS(__CANON,              System,                 __Canon)


#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(OLE_AUT_BINDER,        System,                 OleAutBinder)    
#endif // FEATURE_COMINTEROP

DEFINE_CLASS(MONITOR,               Threading,              Monitor)
DEFINE_METHOD(MONITOR,              ENTER,                  Enter,                      SM_Obj_RetVoid)

// Note: The size of the OverlappedData can be inflated by the CLR host
DEFINE_CLASS_U(Threading,              OverlappedData, NoClass)
DEFINE_FIELD_U(m_asyncResult,              OverlappedDataObject,       m_asyncResult)
DEFINE_FIELD_U(m_iocb,                     OverlappedDataObject,       m_iocb)
DEFINE_FIELD_U(m_iocbHelper,               OverlappedDataObject,       m_iocbHelper)
DEFINE_FIELD_U(m_overlapped,               OverlappedDataObject,       m_overlapped)
DEFINE_FIELD_U(m_userObject,               OverlappedDataObject,       m_userObject)
DEFINE_FIELD_U(m_pinSelf,                  OverlappedDataObject,       m_pinSelf)
DEFINE_FIELD_U(m_AppDomainId,              OverlappedDataObject,       m_AppDomainId)
DEFINE_FIELD_U(m_isArray,                  OverlappedDataObject,       m_isArray)
DEFINE_CLASS(OVERLAPPEDDATA,            Threading,              OverlappedData)

DEFINE_CLASS(NATIVEOVERLAPPED,            Threading,              NativeOverlapped)


DEFINE_CLASS(VOLATILE, Threading, Volatile)

#define DEFINE_VOLATILE_METHODS(methodType, paramType) \
    DEFINE_METHOD(VOLATILE, READ_##paramType, Read, methodType##_Ref##paramType##_Ret##paramType) \
    DEFINE_METHOD(VOLATILE, WRITE_##paramType, Write, methodType##_Ref##paramType##_##paramType)

DEFINE_VOLATILE_METHODS(SM,Bool)
DEFINE_VOLATILE_METHODS(SM,SByt)
DEFINE_VOLATILE_METHODS(SM,Byte)
DEFINE_VOLATILE_METHODS(SM,Shrt)
DEFINE_VOLATILE_METHODS(SM,UShrt)
DEFINE_VOLATILE_METHODS(SM,Int)
DEFINE_VOLATILE_METHODS(SM,UInt)
DEFINE_VOLATILE_METHODS(SM,Long)
DEFINE_VOLATILE_METHODS(SM,ULong)
DEFINE_VOLATILE_METHODS(SM,IntPtr)
DEFINE_VOLATILE_METHODS(SM,UIntPtr)
DEFINE_VOLATILE_METHODS(SM,Flt)
DEFINE_VOLATILE_METHODS(SM,Dbl)
DEFINE_VOLATILE_METHODS(GM,T)

#undef DEFINE_VOLATILE_METHODS

DEFINE_CLASS(PARAMETER,             Reflection,             ParameterInfo)

DEFINE_CLASS(PARAMETER_MODIFIER,    Reflection,             ParameterModifier)

// Keep this in sync with System.Security.PermissionSet
DEFINE_CLASS_U(Security,               PermissionSet,      PermissionSetObject)
DEFINE_FIELD_U(m_permSet,                  PermissionSetObject, _permSet)
DEFINE_FIELD_U(m_Unrestricted,             PermissionSetObject, _Unrestricted)
DEFINE_FIELD_U(m_allPermissionsDecoded,    PermissionSetObject, _allPermissionsDecoded)
#ifdef FEATURE_CAS_POLICY
DEFINE_FIELD_U(m_canUnrestrictedOverride,PermissionSetObject, _canUnrestrictedOverride)
#endif // FEATURE_CAS_POLICY
DEFINE_FIELD_U(m_ignoreTypeLoadFailures, PermissionSetObject, _ignoreTypeLoadFailures)
DEFINE_FIELD_U(m_CheckedForNonCas,         PermissionSetObject, _CheckedForNonCas)
DEFINE_FIELD_U(m_ContainsCas,              PermissionSetObject, _ContainsCas)
DEFINE_FIELD_U(m_ContainsNonCas,           PermissionSetObject, _ContainsNonCas)

DEFINE_CLASS(PERMISSION_SET,        Security,               PermissionSet)
DEFINE_METHOD(PERMISSION_SET,       CTOR,                   .ctor,                      IM_Bool_RetVoid)
DEFINE_METHOD(PERMISSION_SET,       CREATE_SERIALIZED,      CreateSerialized,           SM_ArrObj_Bool_RefArrByte_OutPMS_HostProtectionResource_Bool_RetArrByte)
#ifdef FEATURE_CAS_POLICY
DEFINE_METHOD(PERMISSION_SET,       SETUP_SECURITY,         SetupSecurity,              SM_RetVoid)
#endif // FEATURE_CAS_POLICY
#ifdef FEATURE_CAS_POLICY
DEFINE_METHOD(PERMISSION_SET,       DECODE_XML,             DecodeXml,                  IM_ArrByte_HostProtectionResource_HostProtectionResource_RetBool)
DEFINE_METHOD(PERMISSION_SET,       ENCODE_XML,             EncodeXml,                  IM_RetArrByte)
#endif // FEATURE_CAS_POLICY
DEFINE_METHOD(PERMISSION_SET,       CONTAINS,               Contains,                   IM_IPermission_RetBool)
DEFINE_METHOD(PERMISSION_SET,       DEMAND,                 Demand,                     IM_RetVoid)
DEFINE_METHOD(PERMISSION_SET,       DEMAND_NON_CAS,         DemandNonCAS,               IM_RetVoid)
DEFINE_METHOD(PERMISSION_SET,       IS_UNRESTRICTED,        IsUnrestricted,             IM_RetBool)
#ifdef FEATURE_CAS_POLICY
DEFINE_METHOD(PERMISSION_SET,       IS_SUBSET_OF,           IsSubsetOf,                 IM_PMS_RetBool)
DEFINE_METHOD(PERMISSION_SET,       INTERSECT,              Intersect,                  IM_PMS_RetPMS)
#endif // #ifdef FEATURE_CAS_POLICY
DEFINE_METHOD(PERMISSION_SET,       INPLACE_UNION,          InplaceUnion,               IM_PMS_RetVoid)
DEFINE_METHOD(PERMISSION_SET,       UNION,                  Union,                      IM_PMS_RetPMS)
DEFINE_METHOD(PERMISSION_SET,       IS_EMPTY,               IsEmpty,                    IM_RetBool)
DEFINE_METHOD(PERMISSION_SET,       ADD_PERMISSION,         AddPermission,              IM_IPermission_RetIPermission)

DEFINE_CLASS(NAMEDPERMISSION_SET,      Security,               NamedPermissionSet)

#ifdef FEATURE_CAS_POLICY
DEFINE_CLASS(PEFILE_EVIDENCE_FACTORY,   Policy,             PEFileEvidenceFactory)
DEFINE_METHOD(PEFILE_EVIDENCE_FACTORY,  CREATE_SECURITY_IDENTITY, CreateSecurityIdentity, SM_PEFile_Evidence_RetEvidence)
#endif // FEATURE_CAS_POLICY

DEFINE_CLASS_U(Security,             PermissionListSet,     PermissionListSetObject)
DEFINE_FIELD_U(m_firstPermSetTriple,  PermissionListSetObject, _firstPermSetTriple)
DEFINE_FIELD_U(m_permSetTriples,      PermissionListSetObject, _permSetTriples)
#ifdef FEATURE_COMPRESSEDSTACK
DEFINE_FIELD_U(m_zoneList,            PermissionListSetObject, _zoneList)
DEFINE_FIELD_U(m_originList,          PermissionListSetObject, _originList)
#endif // FEAUTRE_COMPRESSEDSTACK
DEFINE_CLASS(PERMISSION_LIST_SET,   Security,               PermissionListSet)
DEFINE_METHOD(PERMISSION_LIST_SET,  CTOR,                   .ctor,                      IM_RetVoid)
DEFINE_METHOD(PERMISSION_LIST_SET,  CHECK_DEMAND_NO_THROW,  CheckDemandNoThrow,         IM_CodeAccessPermission_RetBool)
DEFINE_METHOD(PERMISSION_LIST_SET,  CHECK_SET_DEMAND_NO_THROW, CheckSetDemandNoThrow,   IM_PMS_RetBool)
DEFINE_METHOD(PERMISSION_LIST_SET,  UPDATE,                  Update,                     IM_PMS_RetVoid)

DEFINE_CLASS(PERMISSION_STATE,      Permissions,            PermissionState)

DEFINE_CLASS(PERMISSION_TOKEN,      Security,               PermissionToken)

DEFINE_CLASS(POINTER,               Reflection,             Pointer)

DEFINE_CLASS_U(Reflection, Pointer, ReflectionPointer)
DEFINE_FIELD_U(_ptr,                ReflectionPointer, _ptr)
DEFINE_FIELD_U(_ptrType,            ReflectionPointer, _ptrType)

DEFINE_CLASS(PROPERTY,              Reflection,             RuntimePropertyInfo)
DEFINE_METHOD(PROPERTY,             SET_VALUE,              SetValue,                   IM_Obj_Obj_BindingFlags_Binder_ArrObj_CultureInfo_RetVoid)
DEFINE_METHOD(PROPERTY,             GET_VALUE,              GetValue,                   IM_Obj_BindingFlags_Binder_ArrObj_CultureInfo_RetObj)
DEFINE_METHOD(PROPERTY,             GET_INDEX_PARAMETERS,   GetIndexParameters,         IM_RetArrParameterInfo)
DEFINE_METHOD(PROPERTY,             GET_TOKEN,              get_MetadataToken,          IM_RetInt)
DEFINE_METHOD(PROPERTY,             GET_MODULE,             GetRuntimeModule,           IM_RetModule)
DEFINE_METHOD(PROPERTY,             GET_SETTER,             GetSetMethod,               IM_Bool_RetMethodInfo)
DEFINE_METHOD(PROPERTY,             GET_GETTER,             GetGetMethod,               IM_Bool_RetMethodInfo)

DEFINE_CLASS(PROPERTY_INFO,         Reflection,             PropertyInfo)

#ifdef FEATURE_REMOTING
DEFINE_CLASS(PROXY_ATTRIBUTE,       Proxies,                ProxyAttribute)

DEFINE_CLASS_U(Proxies,                RealProxy,      RealProxyObject)
DEFINE_FIELD_U(_tp,                        RealProxyObject,    _tp)
DEFINE_FIELD_U(_identity,                  RealProxyObject,    _identity)
DEFINE_FIELD_U(_serverObject,              RealProxyObject,    _serverObject)
DEFINE_FIELD_U(_flags,                     RealProxyObject,    _flags)
DEFINE_FIELD_U(_optFlags,                  RealProxyObject,    _optFlags)
DEFINE_FIELD_U(_domainID,                  RealProxyObject,    _domainID)
DEFINE_FIELD_U(_srvIdentity,               RealProxyObject,    _srvIdentity)
DEFINE_CLASS(REAL_PROXY,          Proxies,                RealProxy)
DEFINE_METHOD(REAL_PROXY,           PRIVATE_INVOKE,         PrivateInvoke,              IM_RefMessageData_Int_RetVoid)
#ifdef FEATURE_COMINTEROP
DEFINE_METHOD(REAL_PROXY,           GETDCOMPROXY,           GetCOMIUnknown,             IM_Bool_RetIntPtr)
DEFINE_METHOD(REAL_PROXY,           SETDCOMPROXY,           SetCOMIUnknown,             IM_IntPtr_RetVoid)
DEFINE_METHOD(REAL_PROXY,           SUPPORTSINTERFACE,      SupportsInterface,          IM_RefGuid_RetIntPtr)

#endif // FEATURE_COMINTEROP
#endif // FEATURE_REMOTING

DEFINE_CLASS(REFLECTION_PERMISSION, Permissions,            ReflectionPermission)
DEFINE_METHOD(REFLECTION_PERMISSION,  CTOR,                   .ctor,                    IM_ReflectionPermissionFlag_RetVoid)

DEFINE_CLASS(REFLECTION_PERMISSION_FLAG, Permissions,       ReflectionPermissionFlag)

#ifdef FEATURE_COMINTEROP_REGISTRATION
DEFINE_CLASS(REGISTRATION_SERVICES, Interop,                RegistrationServices)
DEFINE_METHOD(REGISTRATION_SERVICES,REGISTER_ASSEMBLY,      RegisterAssembly,           IM_AssemblyBase_AssemblyRegistrationFlags_RetBool)
DEFINE_METHOD(REGISTRATION_SERVICES,UNREGISTER_ASSEMBLY,    UnregisterAssembly,         IM_AssemblyBase_RetBool)
#endif // FEATURE_COMINTEROP_REGISTRATION

#ifdef FEATURE_RWLOCK
DEFINE_CLASS_U(Threading,              ReaderWriterLock,           CRWLock)
DEFINE_FIELD_U(_hWriterEvent,              CRWLock, _hWriterEvent)
DEFINE_FIELD_U(_hReaderEvent,              CRWLock, _hReaderEvent)
DEFINE_FIELD_U(_hObjectHandle,             CRWLock, _hObjectHandle)
DEFINE_FIELD_U(_dwState,                   CRWLock, _dwState)
DEFINE_FIELD_U(_dwULockID,                 CRWLock, _dwULockID)
DEFINE_FIELD_U(_dwLLockID,                 CRWLock, _dwLLockID)
DEFINE_FIELD_U(_dwWriterID,                CRWLock, _dwWriterID)
DEFINE_FIELD_U(_dwWriterSeqNum,            CRWLock, _dwWriterSeqNum)
DEFINE_FIELD_U(_wWriterLevel,              CRWLock, _wWriterLevel)
#endif  // FEATURE_RWLOCK

#ifdef FEATURE_REMOTING
DEFINE_CLASS(LEASE,                 Lifetime,               Lease)
DEFINE_METHOD(LEASE,                RENEW_ON_CALL,          RenewOnCall,                IM_RetVoid)

DEFINE_CLASS(REMOTING_PROXY,        Proxies,                RemotingProxy)
DEFINE_METHOD(REMOTING_PROXY,       INVOKE,                 Invoke,                     SM_Obj_RefMessageData_RetVoid)

DEFINE_CLASS(REMOTING_SERVICES,     Remoting,               RemotingServices)
DEFINE_METHOD(REMOTING_SERVICES,    CHECK_CAST,             CheckCast,                  SM_RealProxy_Class_RetBool)
DEFINE_METHOD(REMOTING_SERVICES,    GET_TYPE,               GetType,                    SM_Obj_RetObj)
DEFINE_METHOD(REMOTING_SERVICES,    WRAP,                   Wrap,                       SM_ContextBoundObject_RetObj)
DEFINE_METHOD(REMOTING_SERVICES,    CREATE_PROXY_FOR_DOMAIN,CreateProxyForDomain,       SM_Int_IntPtr_RetObj)
DEFINE_METHOD(REMOTING_SERVICES,    GET_SERVER_CONTEXT_FOR_PROXY,GetServerContextForProxy,  SM_Obj_RetIntPtr)        
DEFINE_METHOD(REMOTING_SERVICES,    GET_SERVER_DOMAIN_ID_FOR_PROXY,GetServerDomainIdForProxy,  SM_Obj_RetInt)        
DEFINE_METHOD(REMOTING_SERVICES,    MARSHAL_TO_BUFFER,      MarshalToBuffer,            SM_Obj_Bool_RetArrByte)
DEFINE_METHOD(REMOTING_SERVICES,    UNMARSHAL_FROM_BUFFER,  UnmarshalFromBuffer,        SM_ArrByte_Bool_RetObj)
DEFINE_METHOD(REMOTING_SERVICES,    DOMAIN_UNLOADED,        DomainUnloaded,             SM_Int_RetVoid)
#endif // FEATURE_REMOTING


DEFINE_CLASS(METADATA_IMPORT,       Reflection,             MetadataImport)
DEFINE_METHOD(METADATA_IMPORT,      THROW_ERROR,            ThrowError,                 SM_Int_RetVoid)

DEFINE_CLASS(RESOLVER,              System,                 Resolver)
DEFINE_METHOD(RESOLVER,             GET_JIT_CONTEXT,        GetJitContext,              IM_RefInt_RetRuntimeType)
DEFINE_METHOD(RESOLVER,             GET_CODE_INFO,          GetCodeInfo,                IM_RefInt_RefInt_RefInt_RetArrByte)
DEFINE_METHOD(RESOLVER,             GET_LOCALS_SIGNATURE,   GetLocalsSignature,         IM_RetArrByte)
DEFINE_METHOD(RESOLVER,             GET_EH_INFO,            GetEHInfo,                  IM_Int_VoidPtr_RetVoid)
DEFINE_METHOD(RESOLVER,             GET_RAW_EH_INFO,        GetRawEHInfo,               IM_RetArrByte)
DEFINE_METHOD(RESOLVER,             GET_STRING_LITERAL,     GetStringLiteral,           IM_Int_RetStr)
DEFINE_METHOD(RESOLVER,             RESOLVE_TOKEN,          ResolveToken,               IM_Int_RefIntPtr_RefIntPtr_RefIntPtr_RetVoid)
DEFINE_METHOD(RESOLVER,             RESOLVE_SIGNATURE,      ResolveSignature,           IM_IntInt_RetArrByte)

DEFINE_CLASS(RESOURCE_MANAGER,      Resources,              ResourceManager)

DEFINE_CLASS(RTFIELD,               Reflection,             RtFieldInfo)
DEFINE_METHOD(RTFIELD,              GET_FIELDHANDLE,        GetFieldHandle,            IM_RetIntPtr)

DEFINE_CLASS(RUNTIME_HELPERS,       CompilerServices,       RuntimeHelpers)
DEFINE_METHOD(RUNTIME_HELPERS,      PREPARE_CONSTRAINED_REGIONS, PrepareConstrainedRegions, SM_RetVoid)
DEFINE_METHOD(RUNTIME_HELPERS,      PREPARE_CONSTRAINED_REGIONS_NOOP, PrepareConstrainedRegionsNoOP, SM_RetVoid)
DEFINE_METHOD(RUNTIME_HELPERS,      EXECUTE_BACKOUT_CODE_HELPER, ExecuteBackoutCodeHelper, SM_Obj_Obj_Bool_RetVoid)

DEFINE_CLASS(JIT_HELPERS,           CompilerServices,       JitHelpers)
#ifdef _DEBUG
DEFINE_METHOD(JIT_HELPERS,          UNSAFE_CAST,            UnsafeCastInternal, NoSig)
DEFINE_METHOD(JIT_HELPERS,          UNSAFE_ENUM_CAST,       UnsafeEnumCastInternal, NoSig)
DEFINE_METHOD(JIT_HELPERS,          UNSAFE_ENUM_CAST_LONG,  UnsafeEnumCastLongInternal, NoSig)
DEFINE_METHOD(JIT_HELPERS,          UNSAFE_CAST_TO_STACKPTR,UnsafeCastToStackPointerInternal, NoSig)
#else // _DEBUG
DEFINE_METHOD(JIT_HELPERS,          UNSAFE_CAST,            UnsafeCast, NoSig)
DEFINE_METHOD(JIT_HELPERS,          UNSAFE_ENUM_CAST,       UnsafeEnumCast, NoSig)
DEFINE_METHOD(JIT_HELPERS,          UNSAFE_ENUM_CAST_LONG,  UnsafeEnumCastLong, NoSig)
DEFINE_METHOD(JIT_HELPERS,          UNSAFE_CAST_TO_STACKPTR,UnsafeCastToStackPointer, NoSig)
#endif // _DEBUG

DEFINE_CLASS(INTERLOCKED,           Threading,              Interlocked)
DEFINE_METHOD(INTERLOCKED,          COMPARE_EXCHANGE_T,     CompareExchange, GM_RefT_T_T_RetT)
DEFINE_METHOD(INTERLOCKED,          COMPARE_EXCHANGE_OBJECT,CompareExchange, SM_RefObject_Object_Object_RetObject)

DEFINE_CLASS(PINNING_HELPER,        CompilerServices,       PinningHelper)
DEFINE_FIELD(PINNING_HELPER,        M_DATA,                 m_data)

DEFINE_CLASS(RUNTIME_WRAPPED_EXCEPTION, CompilerServices,   RuntimeWrappedException)
DEFINE_METHOD(RUNTIME_WRAPPED_EXCEPTION, OBJ_CTOR,          .ctor,                      IM_Obj_RetVoid)
DEFINE_FIELD(RUNTIME_WRAPPED_EXCEPTION, WRAPPED_EXCEPTION,  m_wrappedException)

DEFINE_CLASS_U(Interop,                SafeHandle,         SafeHandle)
DEFINE_FIELD_U(handle,                     SafeHandle,            m_handle)
DEFINE_FIELD_U(_state,                     SafeHandle,            m_state)
DEFINE_FIELD_U(_ownsHandle,                SafeHandle,            m_ownsHandle)
DEFINE_FIELD_U(_fullyInitialized,          SafeHandle,            m_fullyInitialized)
DEFINE_CLASS(SAFE_HANDLE,         Interop,                SafeHandle)
DEFINE_FIELD(SAFE_HANDLE,           HANDLE,                 handle)
DEFINE_METHOD(SAFE_HANDLE,          GET_IS_INVALID,         get_IsInvalid,              IM_RetBool)
DEFINE_METHOD(SAFE_HANDLE,          RELEASE_HANDLE,         ReleaseHandle,              IM_RetBool)
DEFINE_METHOD(SAFE_HANDLE,          DISPOSE,                Dispose,                    IM_RetVoid)
DEFINE_METHOD(SAFE_HANDLE,          DISPOSE_BOOL,           Dispose,                    IM_Bool_RetVoid)

#ifdef FEATURE_CAS_POLICY
DEFINE_CLASS(SAFE_PEFILE_HANDLE,    SafeHandles,            SafePEFileHandle)
#endif // FEATURE_CAS_POLICY

#ifndef FEATURE_CORECLR
DEFINE_CLASS(SAFE_TOKENHANDLE, SafeHandles, SafeAccessTokenHandle)
#endif

#ifndef FEATURE_CORECLR
DEFINE_CLASS(SAFE_TYPENAMEPARSER_HANDLE,    System,         SafeTypeNameParserHandle)
#endif //!FEATURE_CORECLR

#ifdef FEATURE_COMPRESSEDSTACK
DEFINE_CLASS(SAFE_CSHANDLE, Threading, SafeCompressedStackHandle)
#endif // #ifdef FEATURE_COMPRESSEDSTACK


DEFINE_CLASS(SECURITY_ACTION,       Permissions,            SecurityAction)
DEFINE_CLASS(HOST_PROTECTION_RESOURCE, Permissions,         HostProtectionResource)

DEFINE_CLASS(SECURITY_ATTRIBUTE,    Permissions,            SecurityAttribute)
DEFINE_METHOD(SECURITY_ATTRIBUTE, FIND_SECURITY_ATTRIBUTE_TYPE_HANDLE, FindSecurityAttributeTypeHandle, SM_Str_RetIntPtr)

#ifdef FEATURE_CAS_POLICY
DEFINE_CLASS(SECURITY_ELEMENT,      Security,               SecurityElement)
DEFINE_METHOD(SECURITY_ELEMENT,     TO_STRING,              ToString,                   IM_RetStr)
#endif // FEATURE_CAS_POLICY

DEFINE_CLASS(SECURITY_ENGINE,       Security,               CodeAccessSecurityEngine)
DEFINE_METHOD(SECURITY_ENGINE,      CHECK_HELPER,           CheckHelper,                SM_CS_PMS_PMS_CodeAccessPermission_PermissionToken_RuntimeMethodHandleInternal_Assembly_SecurityAction_RetVoid)
DEFINE_METHOD(SECURITY_ENGINE,      CHECK_SET_HELPER,       CheckSetHelper,             SM_CS_PMS_PMS_PMS_RuntimeMethodHandleInternal_Assembly_SecurityAction_RetVoid)
#ifdef FEATURE_APTCA
DEFINE_METHOD(SECURITY_ENGINE,      THROW_SECURITY_EXCEPTION, ThrowSecurityException,   SM_Assembly_PMS_PMS_RuntimeMethodHandleInternal_SecurityAction_Obj_IPermission_RetVoid)
#endif // FEATURE_APTCA
#ifdef FEATURE_CAS_POLICY
DEFINE_METHOD(SECURITY_ENGINE,      RESOLVE_GRANT_SET,      ResolveGrantSet,            SM_Evidence_RefInt_Bool_RetPMS)
DEFINE_METHOD(SECURITY_ENGINE,      PRE_RESOLVE,            PreResolve,                 SM_RefBool_RefBool_RetVoid)
#endif // FEATURE_CAS_POLICY

#ifdef FEATURE_PLS
DEFINE_METHOD(SECURITY_ENGINE,      UPDATE_APPDOMAIN_PLS,   UpdateAppDomainPLS,         SM_PermissionListSet_PMS_PMS_RetPermissionListSet)
#endif // FEATURE_PLS

#ifdef FEATURE_CAS_POLICY
#ifdef FEATURE_NONGENERIC_COLLECTIONS 
DEFINE_METHOD(SECURITY_ENGINE,      GET_ZONE_AND_ORIGIN_HELPER, GetZoneAndOriginHelper, SM_CS_PMS_PMS_ArrayList_ArrayList_RetVoid)
#else
#error Need replacement for GetZoneAndOriginHelper
#endif // FEATURE_NONGENERIC_COLLECTIONS 
DEFINE_METHOD(SECURITY_ENGINE,      REFLECTION_TARGET_DEMAND_HELPER,    ReflectionTargetDemandHelper,    SM_Int_PMS_RetVoid)
DEFINE_METHOD(SECURITY_ENGINE,      REFLECTION_TARGET_DEMAND_HELPER_WITH_CONTEXT, ReflectionTargetDemandHelper,    SM_Int_PMS_Resolver_RetVoid)
DEFINE_METHOD(SECURITY_ENGINE,      CHECK_GRANT_SET_HELPER, CheckGrantSetHelper,        SM_PMS_RetVoid)
#endif // FEATURE_CAS_POLICY

DEFINE_CLASS(SECURITY_EXCEPTION,    Security,               SecurityException)
#ifdef FEATURE_CAS_POLICY
DEFINE_METHOD(SECURITY_EXCEPTION,   CTOR,                   .ctor,                      IM_Str_Type_Str_RetVoid)
#endif // FEATURE_CAS_POLICY

#ifdef FEATURE_CAS_POLICY
DEFINE_CLASS(HOST_PROTECTION_EXCEPTION, Security,         HostProtectionException)
DEFINE_METHOD(HOST_PROTECTION_EXCEPTION, CTOR,            .ctor,                        IM_HPR_HPR_RetVoid)
#endif // FEATURE_CAS_POLICY

DEFINE_CLASS(SECURITY_MANAGER,      Security,               SecurityManager)
#ifdef FEATURE_CAS_POLICY
DEFINE_METHOD(SECURITY_MANAGER,     RESOLVE_CAS_POLICY,     ResolveCasPolicy,           SM_Evidence_PMS_PMS_PMS_PMS_int_Bool_RetPMS)
#endif

DEFINE_CLASS(SECURITY_PERMISSION,   Permissions,            SecurityPermission)
DEFINE_METHOD(SECURITY_PERMISSION,  CTOR,                   .ctor,                      IM_SecurityPermissionFlag_RetVoid)
#ifdef FEATURE_CAS_POLICY
DEFINE_METHOD(SECURITY_PERMISSION,  TOXML,                  ToXml,                      IM_RetSecurityElement)
#endif // FEATURE_CAS_POLICY

DEFINE_CLASS(SECURITY_PERMISSION_FLAG,Permissions,          SecurityPermissionFlag)

DEFINE_CLASS(SECURITY_RUNTIME,      Security,               SecurityRuntime)
DEFINE_METHOD(SECURITY_RUNTIME,     FRAME_DESC_HELPER,      FrameDescHelper,            SM_FrameSecurityDescriptor_IPermission_PermissionToken_RuntimeMethodHandleInternal_RetBool)
DEFINE_METHOD(SECURITY_RUNTIME,     FRAME_DESC_SET_HELPER,  FrameDescSetHelper,         SM_FrameSecurityDescriptor_PMS_OutPMS_RuntimeMethodHandleInternal_RetBool)
#ifdef FEATURE_COMPRESSEDSTACK
DEFINE_METHOD(SECURITY_RUNTIME,     CHECK_DYNAMIC_METHOD_HELPER,      CheckDynamicMethodHelper,    SM_DynamicResolver_IPermission_PermissionToken_RuntimeMethodHandleInternal_RetBool)
DEFINE_METHOD(SECURITY_RUNTIME,     CHECK_DYNAMIC_METHOD_SET_HELPER,  CheckDynamicMethodSetHelper, SM_DynamicResolver_PMS_OutPMS_RuntimeMethodHandleInternal_RetBool)
#endif // FEATURE_COMPRESSEDSTACK

#ifdef FEATURE_REMOTING
DEFINE_CLASS(SERVER_IDENTITY,       Remoting,               ServerIdentity)
DEFINE_FIELD(SERVER_IDENTITY,       SERVER_CONTEXT,         _srvCtx)
#endif // FEATURE_REMOTING
#ifdef FEATURE_COMPRESSEDSTACK
DEFINE_CLASS(DOMAIN_COMPRESSED_STACK, Threading, DomainCompressedStack)
DEFINE_METHOD(DOMAIN_COMPRESSED_STACK, CREATE_MANAGED_OBJECT, CreateManagedObject, SM_IntPtr_RetDCS)
DEFINE_CLASS(COMPRESSED_STACK, Threading, CompressedStack)
DEFINE_METHOD(COMPRESSED_STACK,    RUN,                   Run,                SM_CompressedStack_ContextCallback_Object_RetVoid)
#endif // FEATURE_COMPRESSEDSTACK            

DEFINE_CLASS(SHARED_STATICS,        System,                 SharedStatics)
DEFINE_FIELD(SHARED_STATICS,        SHARED_STATICS,         _sharedStatics)

#ifdef FEATURE_REMOTING
DEFINE_CLASS(STACK_BUILDER_SINK,    Messaging,              StackBuilderSink)
DEFINE_METHOD(STACK_BUILDER_SINK,   PRIVATE_PROCESS_MESSAGE,_PrivateProcessMessage,     IM_IntPtr_ArrObj_Obj_RefArrObj_RetObj)
#endif

DEFINE_CLASS_U(Diagnostics,                StackFrameHelper,   StackFrameHelper)
DEFINE_FIELD_U(targetThread,               StackFrameHelper,   targetThread)
DEFINE_FIELD_U(rgiOffset,                  StackFrameHelper,   rgiOffset)
DEFINE_FIELD_U(rgiILOffset,                StackFrameHelper,   rgiILOffset)
DEFINE_FIELD_U(rgMethodBase,               StackFrameHelper,   rgMethodBase)
DEFINE_FIELD_U(dynamicMethods,             StackFrameHelper,   dynamicMethods)
DEFINE_FIELD_U(rgMethodHandle,             StackFrameHelper,   rgMethodHandle)
DEFINE_FIELD_U(rgAssemblyPath,             StackFrameHelper,   rgAssemblyPath)
DEFINE_FIELD_U(rgLoadedPeAddress,          StackFrameHelper,   rgLoadedPeAddress)
DEFINE_FIELD_U(rgiLoadedPeSize,            StackFrameHelper,   rgiLoadedPeSize)
DEFINE_FIELD_U(rgInMemoryPdbAddress,       StackFrameHelper,   rgInMemoryPdbAddress)
DEFINE_FIELD_U(rgiInMemoryPdbSize,         StackFrameHelper,   rgiInMemoryPdbSize)
DEFINE_FIELD_U(rgiMethodToken,             StackFrameHelper,   rgiMethodToken)
DEFINE_FIELD_U(rgFilename,                 StackFrameHelper,   rgFilename)
DEFINE_FIELD_U(rgiLineNumber,              StackFrameHelper,   rgiLineNumber)
DEFINE_FIELD_U(rgiColumnNumber,            StackFrameHelper,   rgiColumnNumber)
#if defined(FEATURE_EXCEPTIONDISPATCHINFO)
DEFINE_FIELD_U(rgiLastFrameFromForeignExceptionStackTrace,            StackFrameHelper,   rgiLastFrameFromForeignExceptionStackTrace)
#endif // defined(FEATURE_EXCEPTIONDISPATCHINFO)
DEFINE_FIELD_U(getSourceLineInfo,          StackFrameHelper,   getSourceLineInfo)
DEFINE_FIELD_U(iFrameCount,                StackFrameHelper,   iFrameCount)

DEFINE_CLASS(STACK_TRACE,           Diagnostics,            StackTrace)
DEFINE_METHOD(STACK_TRACE,          GET_MANAGED_STACK_TRACE_HELPER, GetManagedStackTraceStringHelper, SM_Bool_RetStr)

DEFINE_CLASS(STREAM,                IO,                     Stream)
DEFINE_METHOD(STREAM,               BEGIN_READ,             BeginRead,  IM_ArrByte_Int_Int_AsyncCallback_Object_RetIAsyncResult)
DEFINE_METHOD(STREAM,               END_READ,               EndRead,    IM_IAsyncResult_RetInt)
DEFINE_METHOD(STREAM,               BEGIN_WRITE,            BeginWrite, IM_ArrByte_Int_Int_AsyncCallback_Object_RetIAsyncResult)
DEFINE_METHOD(STREAM,               END_WRITE,              EndWrite,   IM_IAsyncResult_RetVoid)

// Defined as element type alias
// DEFINE_CLASS(INTPTR,                System,                 IntPtr)
DEFINE_FIELD(INTPTR,                ZERO,                   Zero)

// Defined as element type alias
// DEFINE_CLASS(UINTPTR,                System,                UIntPtr)
DEFINE_FIELD(UINTPTR,               ZERO,                   Zero)

// Defined as element type alias
// DEFINE_CLASS(STRING,                System,                 String)
DEFINE_FIELD(STRING,                M_FIRST_CHAR,           m_firstChar)
DEFINE_FIELD(STRING,                EMPTY,                  Empty)
DEFINE_METHOD(STRING,               CREATE_STRING,          CreateString,               SM_PtrSByt_Int_Int_Encoding_RetStr)
DEFINE_METHOD(STRING,               CTOR_CHARPTR,           .ctor,                      IM_PtrChar_RetVoid)
DEFINE_METHOD(STRING,               CTORF_CHARARRAY,        CtorCharArray,              IM_ArrChar_RetStr)
DEFINE_METHOD(STRING,               CTORF_CHARARRAY_START_LEN,CtorCharArrayStartLength, IM_ArrChar_Int_Int_RetStr)
DEFINE_METHOD(STRING,               CTORF_CHAR_COUNT,       CtorCharCount,              IM_Char_Int_RetStr)
DEFINE_METHOD(STRING,               CTORF_CHARPTR,          CtorCharPtr,                IM_PtrChar_RetStr)
DEFINE_METHOD(STRING,               CTORF_CHARPTR_START_LEN,CtorCharPtrStartLength,     IM_PtrChar_Int_Int_RetStr)
DEFINE_METHOD(STRING,               INTERNAL_COPY,          InternalCopy,               SM_Str_IntPtr_Int_RetVoid)
DEFINE_METHOD(STRING,               WCSLEN,                 wcslen,                     SM_PtrChar_RetInt)
DEFINE_PROPERTY(STRING,             LENGTH,                 Length,                     Int)

DEFINE_CLASS_U(Text,                   StringBuilder,              StringBufferObject)
DEFINE_FIELD_U(m_ChunkPrevious,            StringBufferObject,     m_ChunkPrevious)
DEFINE_FIELD_U(m_MaxCapacity,              StringBufferObject,     m_MaxCapacity)
DEFINE_FIELD_U(m_ChunkLength,              StringBufferObject,     m_ChunkLength)
DEFINE_FIELD_U(m_ChunkOffset,              StringBufferObject,     m_ChunkOffset)
DEFINE_CLASS(STRING_BUILDER,        Text,                   StringBuilder)
DEFINE_PROPERTY(STRING_BUILDER,     LENGTH,                 Length,                     Int)
DEFINE_PROPERTY(STRING_BUILDER,     CAPACITY,               Capacity,                   Int)
DEFINE_METHOD(STRING_BUILDER,       CTOR_INT,               .ctor,                      IM_Int_RetVoid)
DEFINE_METHOD(STRING_BUILDER,       TO_STRING,              ToString,                   IM_RetStr)
DEFINE_METHOD(STRING_BUILDER,       INTERNAL_COPY,          InternalCopy,               IM_IntPtr_Int_RetVoid)
DEFINE_METHOD(STRING_BUILDER,       REPLACE_BUFFER_INTERNAL,ReplaceBufferInternal,      IM_PtrChar_Int_RetVoid)
DEFINE_METHOD(STRING_BUILDER,       REPLACE_BUFFER_ANSI_INTERNAL,ReplaceBufferAnsiInternal, IM_PtrSByt_Int_RetVoid)

DEFINE_CLASS(STRONG_NAME_KEY_PAIR,  Reflection,             StrongNameKeyPair)
#ifndef FEATURE_CORECLR
DEFINE_METHOD(STRONG_NAME_KEY_PAIR, GET_KEY_PAIR,           GetKeyPair,                 IM_RefObject_RetBool) 
#endif

#ifdef FEATURE_SYNCHRONIZATIONCONTEXT_WAIT
DEFINE_CLASS_U(Threading,              SynchronizationContext, SynchronizationContextObject)
DEFINE_FIELD_U(_props, SynchronizationContextObject, _props)
DEFINE_CLASS(SYNCHRONIZATION_CONTEXT,    Threading,              SynchronizationContext)
DEFINE_METHOD(SYNCHRONIZATION_CONTEXT,  INVOKE_WAIT_METHOD_HELPER, InvokeWaitMethodHelper, SM_SyncCtx_ArrIntPtr_Bool_Int_RetInt)
#endif // FEATURE_SYNCHRONIZATIONCONTEXT_WAIT

#ifdef FEATURE_COMINTEROP_TLB_SUPPORT
DEFINE_CLASS(TCE_EVENT_ITF_INFO,    InteropTCE,             EventItfInfo)
DEFINE_METHOD(TCE_EVENT_ITF_INFO,   CTOR,                   .ctor,                      IM_Str_Str_Str_Assembly_Assembly_RetVoid)
#endif // FEATURE_COMINTEROP_TLB_SUPPORT

DEFINE_CLASS(CONTEXTCALLBACK,       Threading,       ContextCallback)

#if defined(FEATURE_IMPERSONATION) || defined(FEATURE_COMPRESSEDSTACK)
DEFINE_CLASS_U(Security,           SecurityContext,        SecurityContextObject)
DEFINE_FIELD_U(_executionContext,      SecurityContextObject,      _executionContext)
#if defined(FEATURE_IMPERSONATION)
DEFINE_FIELD_U(_windowsIdentity,       SecurityContextObject,      _windowsIdentity)
#endif
DEFINE_FIELD_U(_compressedStack,       SecurityContextObject,      _compressedStack)
DEFINE_FIELD_U(_disableFlow,           SecurityContextObject,      _disableFlow)
DEFINE_FIELD_U(isNewCapture,           SecurityContextObject,      _isNewCapture)
DEFINE_CLASS(SECURITYCONTEXT,     Security,           SecurityContext)
DEFINE_METHOD(SECURITYCONTEXT,               RUN,                   Run,                SM_SecurityContext_ContextCallback_Object_RetVoid)
#endif // #if defined(FEATURE_IMPERSONATION) || defined(FEATURE_COMPRESSEDSTACK)

#ifndef FEATURE_CORECLR
DEFINE_CLASS_U(Threading,                  ExecutionContext,       ExecutionContextObject)
#ifdef FEATURE_CAS_POLICY
DEFINE_FIELD_U(_hostExecutionContext,  ExecutionContextObject,     _hostExecutionContext)
#endif // FEATURE_CAS_POLICY
DEFINE_FIELD_U(_syncContext,           ExecutionContextObject,     _syncContext)
#if defined(FEATURE_IMPERSONATION) || defined(FEATURE_COMPRESSEDSTACK)
DEFINE_FIELD_U(_securityContext,       ExecutionContextObject,     _securityContext)
#endif // #if defined(FEATURE_IMPERSONATION) || defined(FEATURE_COMPRESSEDSTACK)
#ifdef FEATURE_REMOTING
DEFINE_FIELD_U(_logicalCallContext,    ExecutionContextObject,     _logicalCallContext)
DEFINE_FIELD_U(_illogicalCallContext,  ExecutionContextObject,     _illogicalCallContext)
#endif // #ifdef FEATURE_REMOTING
DEFINE_CLASS(EXECUTIONCONTEXT,          Threading,                  ExecutionContext)
DEFINE_METHOD(EXECUTIONCONTEXT,               RUN,                   Run,                SM_ExecutionContext_ContextCallback_Object_Bool_RetVoid)
#endif //FEATURE_CORECLR

#ifdef _DEBUG
DEFINE_CLASS(STACKCRAWMARK,         Threading,       StackCrawlMark)
#endif

DEFINE_CLASS(CROSS_CONTEXT_DELEGATE, Threading, InternalCrossContextDelegate)

DEFINE_CLASS_U(Threading,              Thread,                     ThreadBaseObject)
#ifdef FEATURE_REMOTING
DEFINE_FIELD_U(m_Context,                  ThreadBaseObject,   m_ExposedContext)
#endif
#ifndef FEATURE_CORECLR
DEFINE_FIELD_U(m_ExecutionContext,         ThreadBaseObject,   m_ExecutionContext)
#endif
DEFINE_FIELD_U(m_Name,                     ThreadBaseObject,   m_Name)
DEFINE_FIELD_U(m_Delegate,                 ThreadBaseObject,   m_Delegate)
#ifdef FEATURE_LEAK_CULTURE_INFO 
DEFINE_FIELD_U(m_CurrentCulture,           ThreadBaseObject,   m_CurrentUserCulture)
DEFINE_FIELD_U(m_CurrentUICulture,         ThreadBaseObject,   m_CurrentUICulture)
#endif
DEFINE_FIELD_U(m_ThreadStartArg,           ThreadBaseObject,   m_ThreadStartArg)
DEFINE_FIELD_U(DONT_USE_InternalThread,    ThreadBaseObject,   m_InternalThread)
DEFINE_FIELD_U(m_Priority,                 ThreadBaseObject,   m_Priority)
DEFINE_CLASS(THREAD,                Threading,              Thread)
#ifndef FEATURE_LEAK_CULTURE_INFO 
DEFINE_FIELD(THREAD,                CULTURE,                m_CurrentCulture)
DEFINE_FIELD(THREAD,                UI_CULTURE,             m_CurrentUICulture)
#endif
#ifdef FEATURE_IMPERSONATION
DEFINE_METHOD(THREAD,               SET_PRINCIPAL_INTERNAL, SetPrincipalInternal,       IM_IPrincipal_RetVoid)
#endif
#ifdef FEATURE_REMOTING
DEFINE_STATIC_PROPERTY(THREAD,      CURRENT_CONTEXT,        CurrentContext,             Context)
#endif
DEFINE_SET_PROPERTY(THREAD,         CULTURE,                CurrentCulture,             CultureInfo)
DEFINE_SET_PROPERTY(THREAD,         UI_CULTURE,             CurrentUICulture,           CultureInfo)
DEFINE_STATIC_PROPERTY(THREAD,      CURRENT_THREAD,         CurrentThread,              Thread)
#ifdef FEATURE_REMOTING
DEFINE_METHOD(THREAD,               COMPLETE_CROSSCONTEXTCALLBACK,           CompleteCrossContextCallback,                SM_CrossContextDelegate_ArrObj_RetObj)
#endif
DEFINE_METHOD(THREAD,               INTERNAL_GET_CURRENT_THREAD,             InternalGetCurrentThread,                    SM_RetIntPtr)

DEFINE_CLASS(PARAMETERIZEDTHREADSTART,     Threading,                 ParameterizedThreadStart)

DEFINE_CLASS(IOCB_HELPER,              Threading,            _IOCompletionCallback)
DEFINE_METHOD(IOCB_HELPER,             PERFORM_IOCOMPLETION_CALLBACK,        PerformIOCompletionCallback,          SM_UInt_UInt_PtrNativeOverlapped_RetVoid)

DEFINE_CLASS(TPWAITORTIMER_HELPER,              Threading,            _ThreadPoolWaitOrTimerCallback)
DEFINE_METHOD(TPWAITORTIMER_HELPER,             PERFORM_WAITORTIMER_CALLBACK,        PerformWaitOrTimerCallback,          SM_Obj_Bool_RetVoid)

DEFINE_CLASS(TP_WAIT_CALLBACK,         Threading,              _ThreadPoolWaitCallback)
DEFINE_METHOD(TP_WAIT_CALLBACK,        PERFORM_WAIT_CALLBACK,               PerformWaitCallback,                   SM_RetBool)

DEFINE_CLASS(TIMER_QUEUE,           Threading,                TimerQueue)
DEFINE_METHOD(TIMER_QUEUE,          APPDOMAIN_TIMER_CALLBACK, AppDomainTimerCallback,   SM_RetVoid)

DEFINE_CLASS(TIMESPAN,              System,                 TimeSpan)

#ifdef FEATURE_REMOTING
DEFINE_CLASS_U(Proxies,                __TransparentProxy,         TransparentProxyObject)
DEFINE_FIELD_U(_rp,                        TransparentProxyObject, _rp)
DEFINE_FIELD_U(_pMT,                       TransparentProxyObject, _pMT)
DEFINE_FIELD_U(_pInterfaceMT,              TransparentProxyObject, _pInterfaceMT)
DEFINE_FIELD_U(_stub,                      TransparentProxyObject, _stub)
DEFINE_FIELD_U(_stubData,                  TransparentProxyObject, _stubData)
DEFINE_CLASS(TRANSPARENT_PROXY,   Proxies,                __TransparentProxy)
#endif

DEFINE_CLASS(TYPE,                  System,                 Type)
DEFINE_METHOD(TYPE,                 GET_TYPE_FROM_HANDLE,   GetTypeFromHandle,          SM_RuntimeTypeHandle_RetType)
DEFINE_PROPERTY(TYPE,               IS_IMPORT,              IsImport,                   Bool)

DEFINE_CLASS(TYPE_DELEGATOR,        Reflection,             TypeDelegator)

DEFINE_CLASS(UI_PERMISSION,         Permissions,            UIPermission)
DEFINE_METHOD(UI_PERMISSION,        CTOR,                   .ctor,                      IM_PermissionState_RetVoid)

DEFINE_CLASS(UNHANDLED_EVENTARGS,   System,                 UnhandledExceptionEventArgs)
DEFINE_METHOD(UNHANDLED_EVENTARGS,  CTOR,                   .ctor,                      IM_Obj_Bool_RetVoid)

#ifdef FEATURE_EXCEPTION_NOTIFICATIONS
DEFINE_CLASS(FIRSTCHANCE_EVENTARGS,   ExceptionServices,      FirstChanceExceptionEventArgs)
DEFINE_METHOD(FIRSTCHANCE_EVENTARGS,  CTOR,                   .ctor,                      IM_Exception_RetVoid)
#endif // FEATURE_EXCEPTION_NOTIFICATIONS

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)

DEFINE_CLASS(ASSEMBLYLOADCONTEXT,  Loader,                AssemblyLoadContext)    
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  RESOLVE,          Resolve,                      SM_IntPtr_AssemblyName_RetAssemblyBase)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  RESOLVEUNMANAGEDDLL,          ResolveUnmanagedDll,                      SM_Str_IntPtr_RetIntPtr)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  RESOLVEUSINGEVENT,          ResolveUsingResolvingEvent,                      SM_IntPtr_AssemblyName_RetAssemblyBase)

#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)

DEFINE_CLASS(LAZY,              System,     Lazy`1)

DEFINE_CLASS(LAZY_INITIALIZER,  Threading,  LazyInitializer)
DEFINE_CLASS(LAZY_HELPERS,      Threading,  LazyHelpers`1)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(UNKNOWN_WRAPPER,       Interop,                UnknownWrapper)
#endif

DEFINE_CLASS(VALUE_TYPE,            System,                 ValueType)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(VARIANT_WRAPPER,       Interop,                VariantWrapper)
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_IMPERSONATION
DEFINE_CLASS(WINDOWS_IDENTITY,      Principal,              WindowsIdentity)
DEFINE_METHOD(WINDOWS_IDENTITY,     SERIALIZATION_CTOR,     .ctor,                      IM_SerInfo_RetVoid)
#endif
#ifdef FEATURE_X509
DEFINE_CLASS(X509_CERTIFICATE,      X509,                   X509Certificate)
DEFINE_METHOD(X509_CERTIFICATE,     CTOR,                   .ctor,                      IM_ArrByte_RetVoid)
#endif // FEATURE_X509

DEFINE_CLASS(GC,                    System,                 GC)
DEFINE_METHOD(GC,                   KEEP_ALIVE,             KeepAlive,                  SM_Obj_RetVoid)
DEFINE_METHOD(GC,                   COLLECT,                Collect,                    SM_RetVoid)
DEFINE_METHOD(GC,                   WAIT_FOR_PENDING_FINALIZERS, WaitForPendingFinalizers, SM_RetVoid)

DEFINE_CLASS_U(System,                 WeakReference,          WeakReferenceObject)
DEFINE_FIELD_U(m_handle,               WeakReferenceObject,    m_Handle)
DEFINE_CLASS(WEAKREFERENCE,         System,                 WeakReference)

DEFINE_CLASS_U(Threading,              WaitHandle,             WaitHandleBase)
DEFINE_FIELD_U(safeWaitHandle,         WaitHandleBase,         m_safeHandle)
DEFINE_FIELD_U(waitHandle,             WaitHandleBase,         m_handle)
DEFINE_FIELD_U(hasThreadAffinity,      WaitHandleBase,         m_hasThreadAffinity)

DEFINE_CLASS(DEBUGGER,              Diagnostics,            Debugger)
DEFINE_METHOD(DEBUGGER,             BREAK_CAN_THROW,        BreakCanThrow,          SM_RetVoid)

DEFINE_CLASS(BUFFER,                System,                 Buffer)
DEFINE_METHOD(BUFFER,               MEMCPY_PTRBYTE_ARRBYTE, Memcpy,                 SM_PtrByte_Int_ArrByte_Int_Int_RetVoid)
DEFINE_METHOD(BUFFER,               MEMCPY,                 Memcpy,                 SM_PtrByte_PtrByte_Int_RetVoid)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(WINDOWSRUNTIMEMARSHAL, WinRT,  WindowsRuntimeMarshal)
#ifdef FEATURE_COMINTEROP_WINRT_MANAGED_ACTIVATION
DEFINE_METHOD(WINDOWSRUNTIMEMARSHAL, GET_ACTIVATION_FACTORY_FOR_TYPE, GetActivationFactoryForType, SM_Type_RetIntPtr)
#ifdef FEATURE_COMINTEROP_WINRT_DESKTOP_HOST
DEFINE_METHOD(WINDOWSRUNTIMEMARSHAL, GET_CLASS_ACTIVATOR_FOR_APPLICATION, GetClassActivatorForApplication, SM_Str_RetIntPtr)
#endif // FEATURE_COMINTEROP_WINRT_DESKTOP_HOST
#endif // FEATURE_COMINTEROP_WINRT_MANAGED_ACTIVATION

DEFINE_CLASS(IACTIVATIONFACTORY,    WinRT,  IActivationFactory)
DEFINE_METHOD(IACTIVATIONFACTORY,   ACTIVATE_INSTANCE, ActivateInstance, IM_RetObj)
DEFINE_CLASS(ISTRINGABLEHELPER,     WinRT,  IStringableHelper)
DEFINE_METHOD(ISTRINGABLEHELPER,    TO_STRING, ToString, SM_Obj_RetStr)
#endif // FEATURE_COMINTEROP

DEFINE_CLASS(STUBHELPERS,           StubHelpers,            StubHelpers)
DEFINE_METHOD(STUBHELPERS,          IS_QCALL,               IsQCall,                    SM_IntPtr_RetBool)
DEFINE_METHOD(STUBHELPERS,          INIT_DECLARING_TYPE,    InitDeclaringType,          SM_IntPtr_RetVoid)
DEFINE_METHOD(STUBHELPERS,          GET_NDIRECT_TARGET,     GetNDirectTarget,           SM_IntPtr_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          GET_DELEGATE_TARGET,    GetDelegateTarget,          SM_Delegate_RefIntPtr_RetIntPtr)
#ifndef FEATURE_CORECLR // CAS
DEFINE_METHOD(STUBHELPERS,          DEMAND_PERMISSION,      DemandPermission,           SM_IntPtr_RetVoid)
#ifdef _TARGET_X86_
DEFINE_METHOD(STUBHELPERS,          SET_COPY_CTOR_COOKIE_CHAIN, SetCopyCtorCookieChain, SM_IntPtr_IntPtr_Int_IntPtr_RetVoid)
DEFINE_FIELD(STUBHELPERS,           COPY_CTOR_STUB_DESC,    s_copyCtorStubDesc)
#endif // _TARGET_X86_
#endif // !FEATURE_CORECLR
#ifdef FEATURE_COMINTEROP
DEFINE_METHOD(STUBHELPERS,          GET_COM_HR_EXCEPTION_OBJECT,              GetCOMHRExceptionObject,            SM_Int_IntPtr_Obj_RetException)
DEFINE_METHOD(STUBHELPERS,          GET_COM_HR_EXCEPTION_OBJECT_WINRT,        GetCOMHRExceptionObject_WinRT,      SM_Int_IntPtr_Obj_RetException)
DEFINE_METHOD(STUBHELPERS,          GET_COM_IP_FROM_RCW,                      GetCOMIPFromRCW,                    SM_Obj_IntPtr_RefIntPtr_RefBool_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          GET_COM_IP_FROM_RCW_WINRT,                GetCOMIPFromRCW_WinRT,              SM_Obj_IntPtr_RefIntPtr_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          GET_COM_IP_FROM_RCW_WINRT_SHARED_GENERIC, GetCOMIPFromRCW_WinRTSharedGeneric, SM_Obj_IntPtr_RefIntPtr_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          GET_COM_IP_FROM_RCW_WINRT_DELEGATE,       GetCOMIPFromRCW_WinRTDelegate,      SM_Obj_IntPtr_RefIntPtr_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          SHOULD_CALL_WINRT_INTERFACE,              ShouldCallWinRTInterface,           SM_Obj_IntPtr_RetBool)
DEFINE_METHOD(STUBHELPERS,          STUB_REGISTER_RCW,                        StubRegisterRCW,                    SM_Obj_RetVoid)
DEFINE_METHOD(STUBHELPERS,          STUB_UNREGISTER_RCW,                      StubUnregisterRCW,                  SM_Obj_RetVoid)
DEFINE_METHOD(STUBHELPERS,          GET_WINRT_FACTORY_OBJECT,                 GetWinRTFactoryObject,              SM_IntPtr_RetObj)
DEFINE_METHOD(STUBHELPERS,          GET_DELEGATE_INVOKE_METHOD,               GetDelegateInvokeMethod,            SM_Delegate_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          GET_WINRT_FACTORY_RETURN_VALUE,           GetWinRTFactoryReturnValue,         SM_Obj_IntPtr_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          GET_OUTER_INSPECTABLE,                    GetOuterInspectable,                SM_Obj_IntPtr_RetIntPtr)
#ifdef MDA_SUPPORTED
DEFINE_METHOD(STUBHELPERS,          TRIGGER_EXCEPTION_SWALLOWED_MDA,          TriggerExceptionSwallowedMDA,       SM_Exception_IntPtr_RetException)
#endif // MDA_SUPPORTED
#endif // FEATURE_COMINTEROP
#if defined(MDA_SUPPORTED) || (defined(CROSSGEN_COMPILE) && !defined(FEATURE_CORECLR))
DEFINE_METHOD(STUBHELPERS,          CHECK_COLLECTED_DELEGATE_MDA, CheckCollectedDelegateMDA, SM_IntPtr_RetVoid)
#endif // MDA_SUPPORTED
DEFINE_METHOD(STUBHELPERS,          SET_LAST_ERROR,         SetLastError,               SM_RetVoid)
#ifdef FEATURE_CORECLR
DEFINE_METHOD(STUBHELPERS,          CLEAR_LAST_ERROR,       ClearLastError,             SM_RetVoid)
#endif

DEFINE_METHOD(STUBHELPERS,          THROW_INTEROP_PARAM_EXCEPTION, ThrowInteropParamException,   SM_Int_Int_RetVoid)
DEFINE_METHOD(STUBHELPERS,          ADD_TO_CLEANUP_LIST,    AddToCleanupList,           SM_RefCleanupWorkList_SafeHandle_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          DESTROY_CLEANUP_LIST,   DestroyCleanupList,         SM_RefCleanupWorkList_RetVoid)
DEFINE_METHOD(STUBHELPERS,          GET_HR_EXCEPTION_OBJECT, GetHRExceptionObject,      SM_Int_RetException)
DEFINE_METHOD(STUBHELPERS,          CREATE_CUSTOM_MARSHALER_HELPER, CreateCustomMarshalerHelper, SM_IntPtr_Int_IntPtr_RetIntPtr)

DEFINE_METHOD(STUBHELPERS,          CHECK_STRING_LENGTH,    CheckStringLength,          SM_Int_RetVoid)
DEFINE_METHOD(STUBHELPERS,          DECIMAL_CANONICALIZE_INTERNAL, DecimalCanonicalizeInternal,   SM_RefDec_RetVoid)

DEFINE_METHOD(STUBHELPERS,          FMT_CLASS_UPDATE_NATIVE_INTERNAL,   FmtClassUpdateNativeInternal,   SM_Obj_PtrByte_RefCleanupWorkList_RetVoid)
DEFINE_METHOD(STUBHELPERS,          FMT_CLASS_UPDATE_CLR_INTERNAL,      FmtClassUpdateCLRInternal,      SM_Obj_PtrByte_RetVoid)
DEFINE_METHOD(STUBHELPERS,          LAYOUT_DESTROY_NATIVE_INTERNAL,     LayoutDestroyNativeInternal,    SM_PtrByte_IntPtr_RetVoid)
DEFINE_METHOD(STUBHELPERS,          ALLOCATE_INTERNAL,                  AllocateInternal,               SM_IntPtr_RetObj)
DEFINE_METHOD(STUBHELPERS,          STRLEN,                             strlen,                         SM_PtrSByt_RetInt)
DEFINE_METHOD(STUBHELPERS,          MARSHAL_TO_MANAGED_VA_LIST_INTERNAL,MarshalToManagedVaListInternal, SM_IntPtr_IntPtr_RetVoid)
DEFINE_METHOD(STUBHELPERS,          MARSHAL_TO_UNMANAGED_VA_LIST_INTERNAL,MarshalToUnmanagedVaListInternal,SM_IntPtr_UInt_IntPtr_RetVoid)
DEFINE_METHOD(STUBHELPERS,          CALC_VA_LIST_SIZE,                  CalcVaListSize,                 SM_IntPtr_RetUInt)
DEFINE_METHOD(STUBHELPERS,          VALIDATE_OBJECT,                    ValidateObject,                 SM_Obj_IntPtr_Obj_RetVoid)
DEFINE_METHOD(STUBHELPERS,          VALIDATE_BYREF,                     ValidateByref,                  SM_IntPtr_IntPtr_Obj_RetVoid)
DEFINE_METHOD(STUBHELPERS,          GET_STUB_CONTEXT,                   GetStubContext,                 SM_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          LOG_PINNED_ARGUMENT,                LogPinnedArgument,              SM_IntPtr_IntPtr_RetVoid)
#ifdef _WIN64
DEFINE_METHOD(STUBHELPERS,          GET_STUB_CONTEXT_ADDR,              GetStubContextAddr,             SM_RetIntPtr)
#endif // _WIN64
#ifdef MDA_SUPPORTED
DEFINE_METHOD(STUBHELPERS,          TRIGGER_GC_FOR_MDA,                 TriggerGCForMDA,                SM_RetVoid)
#endif
DEFINE_METHOD(STUBHELPERS,          SAFE_HANDLE_ADD_REF,    SafeHandleAddRef,           SM_SafeHandle_RefBool_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          SAFE_HANDLE_RELEASE,    SafeHandleRelease,          SM_SafeHandle_RetVoid)

#ifdef PROFILING_SUPPORTED
DEFINE_METHOD(STUBHELPERS,          PROFILER_BEGIN_TRANSITION_CALLBACK, ProfilerBeginTransitionCallback, SM_IntPtr_IntPtr_Obj_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          PROFILER_END_TRANSITION_CALLBACK,   ProfilerEndTransitionCallback,   SM_IntPtr_IntPtr_RetVoid)
#endif

#ifdef FEATURE_ARRAYSTUB_AS_IL
DEFINE_METHOD(STUBHELPERS,          ARRAY_TYPE_CHECK,    ArrayTypeCheck,          SM_Obj_ArrObject_RetVoid)
#endif

#ifdef FEATURE_STUBS_AS_IL
DEFINE_METHOD(STUBHELPERS,          MULTICAST_DEBUGGER_TRACE_HELPER,    MulticastDebuggerTraceHelper,    SM_Obj_Int_RetVoid)
#endif

#if defined(_TARGET_X86_) && !defined(FEATURE_CORECLR)
DEFINE_CLASS(COPYCTORSTUBCOOKIE,    StubHelpers,            CopyCtorStubCookie)
DEFINE_METHOD(COPYCTORSTUBCOOKIE,   SET_DATA,               SetData,                    IM_IntPtr_UInt_IntPtr_IntPtr_RetVoid)
DEFINE_METHOD(COPYCTORSTUBCOOKIE,   SET_NEXT,               SetNext,                    IM_IntPtr_RetVoid)
#endif // _TARGET_X86_ && !FEATURE_CORECLR

DEFINE_CLASS(ANSICHARMARSHALER,     StubHelpers,            AnsiCharMarshaler)
DEFINE_METHOD(ANSICHARMARSHALER,    CONVERT_TO_NATIVE,      ConvertToNative,            SM_Char_Bool_Bool_RetByte)
DEFINE_METHOD(ANSICHARMARSHALER,    CONVERT_TO_MANAGED,     ConvertToManaged,           SM_Byte_RetChar)
DEFINE_METHOD(ANSICHARMARSHALER,    DO_ANSI_CONVERSION,     DoAnsiConversion,           SM_Str_Bool_Bool_RefInt_RetArrByte)

DEFINE_CLASS(CSTRMARSHALER,         StubHelpers,            CSTRMarshaler)
DEFINE_METHOD(CSTRMARSHALER,        CONVERT_TO_NATIVE,      ConvertToNative,            SM_Int_Str_IntPtr_RetIntPtr)
DEFINE_METHOD(CSTRMARSHALER,        CONVERT_TO_MANAGED,     ConvertToManaged,           SM_IntPtr_RetStr)
DEFINE_METHOD(CSTRMARSHALER,        CLEAR_NATIVE,           ClearNative,                SM_IntPtr_RetVoid)

DEFINE_CLASS(WSTRBUFFERMARSHALER,   StubHelpers,            WSTRBufferMarshaler)
DEFINE_METHOD(WSTRBUFFERMARSHALER,  CONVERT_TO_NATIVE,      ConvertToNative,            SM_Str_RetIntPtr)
DEFINE_METHOD(WSTRBUFFERMARSHALER,  CONVERT_TO_MANAGED,     ConvertToManaged,           SM_IntPtr_RetStr)
DEFINE_METHOD(WSTRBUFFERMARSHALER,  CLEAR_NATIVE,           ClearNative,                SM_IntPtr_RetVoid)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(BSTRMARSHALER,         StubHelpers,            BSTRMarshaler)
DEFINE_METHOD(BSTRMARSHALER,        CONVERT_TO_NATIVE,      ConvertToNative,            SM_Str_IntPtr_RetIntPtr)
DEFINE_METHOD(BSTRMARSHALER,        CONVERT_TO_MANAGED,     ConvertToManaged,           SM_IntPtr_RetStr)
DEFINE_METHOD(BSTRMARSHALER,        CLEAR_NATIVE,           ClearNative,                SM_IntPtr_RetVoid)

DEFINE_CLASS(ANSIBSTRMARSHALER,     StubHelpers,            AnsiBSTRMarshaler)
DEFINE_METHOD(ANSIBSTRMARSHALER,    CONVERT_TO_NATIVE,      ConvertToNative,            SM_Int_Str_RetIntPtr)
DEFINE_METHOD(ANSIBSTRMARSHALER,    CONVERT_TO_MANAGED,     ConvertToManaged,           SM_IntPtr_RetStr)
DEFINE_METHOD(ANSIBSTRMARSHALER,    CLEAR_NATIVE,           ClearNative,                SM_IntPtr_RetVoid)

DEFINE_CLASS(OBJECTMARSHALER,       StubHelpers,            ObjectMarshaler)
DEFINE_METHOD(OBJECTMARSHALER,      CONVERT_TO_NATIVE,      ConvertToNative,            SM_ObjIntPtr_RetVoid)
DEFINE_METHOD(OBJECTMARSHALER,      CONVERT_TO_MANAGED,     ConvertToManaged,           SM_IntPtr_RetObj)
DEFINE_METHOD(OBJECTMARSHALER,      CLEAR_NATIVE,           ClearNative,                SM_IntPtr_RetVoid)

DEFINE_CLASS(HSTRINGMARSHALER,      StubHelpers,            HStringMarshaler)
DEFINE_METHOD(HSTRINGMARSHALER,     CONVERT_TO_NATIVE_REFERENCE,    ConvertToNativeReference,   SM_Str_PtrHStringHeader_RetIntPtr)
DEFINE_METHOD(HSTRINGMARSHALER,     CONVERT_TO_NATIVE,              ConvertToNative,            SM_Str_RetIntPtr)
DEFINE_METHOD(HSTRINGMARSHALER,     CONVERT_TO_MANAGED,             ConvertToManaged,           SM_IntPtr_RetStr)
DEFINE_METHOD(HSTRINGMARSHALER,     CLEAR_NATIVE,                   ClearNative,                SM_IntPtr_RetVoid)

DEFINE_CLASS(URIMARSHALER,          StubHelpers,                UriMarshaler)
DEFINE_METHOD(URIMARSHALER,         GET_RAWURI_FROM_NATIVE,     GetRawUriFromNative,        SM_IntPtr_RetStr)
DEFINE_METHOD(URIMARSHALER,         CREATE_NATIVE_URI_INSTANCE, CreateNativeUriInstance,    SM_Str_RetIntPtr)

DEFINE_CLASS(INTERFACEMARSHALER,    StubHelpers,            InterfaceMarshaler)
DEFINE_METHOD(INTERFACEMARSHALER,   CONVERT_TO_NATIVE,      ConvertToNative,            SM_Obj_IntPtr_IntPtr_Int_RetIntPtr)
DEFINE_METHOD(INTERFACEMARSHALER,   CONVERT_TO_MANAGED,     ConvertToManaged,           SM_IntPtr_IntPtr_IntPtr_Int_RetObj)
DEFINE_METHOD(INTERFACEMARSHALER,   CLEAR_NATIVE,           ClearNative,                SM_IntPtr_RetVoid)


DEFINE_CLASS(MNGD_SAFE_ARRAY_MARSHALER,  StubHelpers,                 MngdSafeArrayMarshaler)
DEFINE_METHOD(MNGD_SAFE_ARRAY_MARSHALER, CREATE_MARSHALER,            CreateMarshaler,            SM_IntPtr_IntPtr_Int_Int_RetVoid)
DEFINE_METHOD(MNGD_SAFE_ARRAY_MARSHALER, CONVERT_SPACE_TO_NATIVE,     ConvertSpaceToNative,       SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_SAFE_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_NATIVE,  ConvertContentsToNative,    SM_IntPtr_RefObj_IntPtr_Obj_RetVoid)
DEFINE_METHOD(MNGD_SAFE_ARRAY_MARSHALER, CONVERT_SPACE_TO_MANAGED,    ConvertSpaceToManaged,      SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_SAFE_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_MANAGED, ConvertContentsToManaged,   SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_SAFE_ARRAY_MARSHALER, CLEAR_NATIVE,                ClearNative,                SM_IntPtr_RefObj_IntPtr_RetVoid)

DEFINE_CLASS(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, StubHelpers,         MngdHiddenLengthArrayMarshaler)
DEFINE_METHOD(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, CREATE_MARSHALER,                 CreateMarshaler,            SM_IntPtr_IntPtr_IntPtr_UShrt_RetVoid)
DEFINE_METHOD(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, CONVERT_SPACE_TO_MANAGED,         ConvertSpaceToManaged,      SM_IntPtr_RefObj_IntPtr_Int_RetVoid)
DEFINE_METHOD(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_MANAGED,      ConvertContentsToManaged,   SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, CONVERT_SPACE_TO_NATIVE,          ConvertSpaceToNative,       SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_NATIVE,       ConvertContentsToNative,    SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, CLEAR_NATIVE_CONTENTS,            ClearNativeContents,        SM_IntPtr_IntPtr_Int_RetVoid)
DEFINE_METHOD(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, CLEAR_NATIVE_CONTENTS_TYPE,       ClearNativeContents_Type,   NoSig)

DEFINE_METHOD(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_MANAGED_DATETIME,     ConvertContentsToManaged_DateTime,     NoSig)
DEFINE_METHOD(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_MANAGED_TYPE,         ConvertContentsToManaged_Type,         NoSig)
DEFINE_METHOD(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_MANAGED_EXCEPTION,    ConvertContentsToManaged_Exception,    NoSig)
DEFINE_METHOD(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_MANAGED_NULLABLE,     ConvertContentsToManaged_Nullable,     NoSig)
DEFINE_METHOD(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_MANAGED_KEYVALUEPAIR, ConvertContentsToManaged_KeyValuePair, NoSig)

DEFINE_METHOD(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_NATIVE_DATETIME,     ConvertContentsToNative_DateTime,     NoSig)
DEFINE_METHOD(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_NATIVE_TYPE,         ConvertContentsToNative_Type,         NoSig)
DEFINE_METHOD(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_NATIVE_EXCEPTION,    ConvertContentsToNative_Exception,    NoSig)
DEFINE_METHOD(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_NATIVE_NULLABLE,     ConvertContentsToNative_Nullable,     NoSig)
DEFINE_METHOD(MNGD_HIDDEN_LENGTH_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_NATIVE_KEYVALUEPAIR, ConvertContentsToNative_KeyValuePair, NoSig)

DEFINE_CLASS(DATETIMEOFFSETMARSHALER,     StubHelpers,           DateTimeOffsetMarshaler)
DEFINE_METHOD(DATETIMEOFFSETMARSHALER,    CONVERT_TO_NATIVE,     ConvertToNative,     SM_RefDateTimeOffset_RefDateTimeNative_RetVoid)
DEFINE_METHOD(DATETIMEOFFSETMARSHALER,    CONVERT_TO_MANAGED,    ConvertToManaged,    SM_RefDateTimeOffset_RefDateTimeNative_RetVoid)

DEFINE_CLASS(NULLABLEMARSHALER,           StubHelpers,           NullableMarshaler)
DEFINE_METHOD(NULLABLEMARSHALER,          CONVERT_TO_NATIVE,     ConvertToNative,     NoSig)
DEFINE_METHOD(NULLABLEMARSHALER,          CONVERT_TO_MANAGED,    ConvertToManaged,    NoSig)
DEFINE_METHOD(NULLABLEMARSHALER,          CONVERT_TO_MANAGED_RET_VOID,    ConvertToManagedRetVoid,    NoSig)

DEFINE_CLASS(SYSTEMTYPEMARSHALER,   StubHelpers,        SystemTypeMarshaler)

DEFINE_METHOD(SYSTEMTYPEMARSHALER,  CONVERT_TO_NATIVE,  ConvertToNative,    SM_Type_PtrTypeName_RetVoid)
DEFINE_METHOD(SYSTEMTYPEMARSHALER,  CONVERT_TO_MANAGED, ConvertToManaged,   SM_PtrTypeName_RefType_RetVoid)
DEFINE_METHOD(SYSTEMTYPEMARSHALER,  CLEAR_NATIVE,       ClearNative,        SM_PtrTypeName_RetVoid)

DEFINE_CLASS(KEYVALUEPAIRMARSHALER,  StubHelpers,            KeyValuePairMarshaler)
DEFINE_METHOD(KEYVALUEPAIRMARSHALER, CONVERT_TO_NATIVE,      ConvertToNative,     NoSig)
DEFINE_METHOD(KEYVALUEPAIRMARSHALER, CONVERT_TO_MANAGED,     ConvertToManaged,    NoSig)
DEFINE_METHOD(KEYVALUEPAIRMARSHALER, CONVERT_TO_MANAGED_BOX, ConvertToManagedBox, NoSig)

DEFINE_CLASS(HRESULTEXCEPTIONMARSHALER,   StubHelpers,           HResultExceptionMarshaler)
DEFINE_METHOD(HRESULTEXCEPTIONMARSHALER,  CONVERT_TO_NATIVE,     ConvertToNative,     SM_Exception_RetInt)
DEFINE_METHOD(HRESULTEXCEPTIONMARSHALER,  CONVERT_TO_MANAGED,    ConvertToManaged,    SM_Int_RetException)

#endif // FEATURE_COMINTEROP

DEFINE_CLASS(VALUECLASSMARSHALER,   StubHelpers,            ValueClassMarshaler)
DEFINE_METHOD(VALUECLASSMARSHALER,  CONVERT_TO_NATIVE,      ConvertToNative,            SM_IntPtrIntPtrIntPtr_RefCleanupWorkList_RetVoid)
DEFINE_METHOD(VALUECLASSMARSHALER,  CONVERT_TO_MANAGED,     ConvertToManaged,           SM_IntPtrIntPtrIntPtr_RetVoid)
DEFINE_METHOD(VALUECLASSMARSHALER,  CLEAR_NATIVE,           ClearNative,                SM_IntPtr_IntPtr_RetVoid)

DEFINE_CLASS(DATEMARSHALER,         StubHelpers,            DateMarshaler)
DEFINE_METHOD(DATEMARSHALER,        CONVERT_TO_NATIVE,      ConvertToNative,            SM_DateTime_RetDbl)
DEFINE_METHOD(DATEMARSHALER,        CONVERT_TO_MANAGED,     ConvertToManaged,           SM_Dbl_RetLong)

DEFINE_CLASS(VBBYVALSTRMARSHALER,   StubHelpers,            VBByValStrMarshaler)
DEFINE_METHOD(VBBYVALSTRMARSHALER,  CONVERT_TO_NATIVE,      ConvertToNative,            SM_Str_Bool_Bool_RefInt_RetIntPtr)
DEFINE_METHOD(VBBYVALSTRMARSHALER,  CONVERT_TO_MANAGED,     ConvertToManaged,           SM_IntPtr_Int_RetStr)
DEFINE_METHOD(VBBYVALSTRMARSHALER,  CLEAR_NATIVE,           ClearNative,                SM_IntPtr_RetVoid)

DEFINE_CLASS(MNGD_NATIVE_ARRAY_MARSHALER,  StubHelpers,                 MngdNativeArrayMarshaler)
DEFINE_METHOD(MNGD_NATIVE_ARRAY_MARSHALER, CREATE_MARSHALER,            CreateMarshaler,            SM_IntPtr_IntPtr_Int_RetVoid)
DEFINE_METHOD(MNGD_NATIVE_ARRAY_MARSHALER, CONVERT_SPACE_TO_NATIVE,     ConvertSpaceToNative,       SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_NATIVE_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_NATIVE,  ConvertContentsToNative,    SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_NATIVE_ARRAY_MARSHALER, CONVERT_SPACE_TO_MANAGED,    ConvertSpaceToManaged,      SM_IntPtr_RefObj_IntPtr_Int_RetVoid)
DEFINE_METHOD(MNGD_NATIVE_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_MANAGED, ConvertContentsToManaged,   SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_NATIVE_ARRAY_MARSHALER, CLEAR_NATIVE,                ClearNative,                SM_IntPtr_IntPtr_Int_RetVoid)
DEFINE_METHOD(MNGD_NATIVE_ARRAY_MARSHALER, CLEAR_NATIVE_CONTENTS,       ClearNativeContents,        SM_IntPtr_IntPtr_Int_RetVoid)

DEFINE_CLASS(MNGD_REF_CUSTOM_MARSHALER,  StubHelpers,                 MngdRefCustomMarshaler)
DEFINE_METHOD(MNGD_REF_CUSTOM_MARSHALER, CREATE_MARSHALER,            CreateMarshaler,            SM_IntPtr_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_REF_CUSTOM_MARSHALER, CONVERT_CONTENTS_TO_NATIVE,  ConvertContentsToNative,    SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_REF_CUSTOM_MARSHALER, CONVERT_CONTENTS_TO_MANAGED, ConvertContentsToManaged,   SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_REF_CUSTOM_MARSHALER, CLEAR_NATIVE,                ClearNative,                SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_REF_CUSTOM_MARSHALER, CLEAR_MANAGED,               ClearManaged,               SM_IntPtr_RefObj_IntPtr_RetVoid)

DEFINE_CLASS(ASANY_MARSHALER,            StubHelpers,                 AsAnyMarshaler)
DEFINE_METHOD(ASANY_MARSHALER,           CTOR,                        .ctor,                      IM_IntPtr_RetVoid)
DEFINE_METHOD(ASANY_MARSHALER,           CONVERT_TO_NATIVE,           ConvertToNative,            IM_Obj_Int_RetIntPtr)
DEFINE_METHOD(ASANY_MARSHALER,           CONVERT_TO_MANAGED,          ConvertToManaged,           IM_Obj_IntPtr_RetVoid)
DEFINE_METHOD(ASANY_MARSHALER,           CLEAR_NATIVE,                ClearNative,                IM_IntPtr_RetVoid)

DEFINE_CLASS(NATIVEVARIANT,         StubHelpers,            NativeVariant)

DEFINE_CLASS(WIN32NATIVE,           Win32,                  Win32Native)
DEFINE_METHOD(WIN32NATIVE,          COTASKMEMALLOC,         CoTaskMemAlloc,         SM_UIntPtr_RetIntPtr)
DEFINE_METHOD(WIN32NATIVE,          COTASKMEMFREE,          CoTaskMemFree,          SM_IntPtr_RetVoid)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(IITERABLE,              WinRT,                 IIterable`1)
DEFINE_CLASS(IVECTOR,                WinRT,                 IVector`1)
DEFINE_CLASS(IMAP,                   WinRT,                 IMap`2)
DEFINE_CLASS(IKEYVALUEPAIR,          WinRT,                 IKeyValuePair`2)
DEFINE_CLASS(IVECTORVIEW,            WinRT,                 IVectorView`1)
DEFINE_CLASS(IMAPVIEW,               WinRT,                 IMapView`2)
DEFINE_CLASS(IITERATOR,              WinRT,                 IIterator`1)
DEFINE_CLASS(IPROPERTYVALUE,         WinRT,                 IPropertyValue)
DEFINE_CLASS(IBINDABLEITERABLE,      WinRT,                 IBindableIterable)
DEFINE_CLASS(IBINDABLEITERATOR,      WinRT,                 IBindableIterator)
DEFINE_CLASS(IBINDABLEVECTOR,        WinRT,                 IBindableVector)
DEFINE_CLASS(ICLOSABLE,             WinRT,                  IClosable)

DEFINE_CLASS(GET_ENUMERATOR_DELEGATE,        WinRT,                            GetEnumerator_Delegate`1)
DEFINE_CLASS(ITERABLE_TO_ENUMERABLE_ADAPTER, WinRT,                            IterableToEnumerableAdapter)
DEFINE_METHOD(ITERABLE_TO_ENUMERABLE_ADAPTER, GET_ENUMERATOR_STUB,             GetEnumerator_Stub, NoSig)
DEFINE_METHOD(ITERABLE_TO_ENUMERABLE_ADAPTER, GET_ENUMERATOR_VARIANCE_STUB,    GetEnumerator_Variance_Stub, NoSig)

DEFINE_CLASS(VECTOR_TO_LIST_ADAPTER,        WinRT,                   VectorToListAdapter)
DEFINE_METHOD(VECTOR_TO_LIST_ADAPTER,       INDEXER_GET,             Indexer_Get, NoSig)
DEFINE_METHOD(VECTOR_TO_LIST_ADAPTER,       INDEXER_SET,             Indexer_Set, NoSig)
DEFINE_METHOD(VECTOR_TO_LIST_ADAPTER,       INDEX_OF,                IndexOf, NoSig)
DEFINE_METHOD(VECTOR_TO_LIST_ADAPTER,       INSERT,                  Insert, NoSig)
DEFINE_METHOD(VECTOR_TO_LIST_ADAPTER,       REMOVE_AT,               RemoveAt, NoSig)

DEFINE_CLASS(MAP_TO_DICTIONARY_ADAPTER,     WinRT,                   MapToDictionaryAdapter)
DEFINE_METHOD(MAP_TO_DICTIONARY_ADAPTER,    INDEXER_GET,             Indexer_Get, NoSig)
DEFINE_METHOD(MAP_TO_DICTIONARY_ADAPTER,    INDEXER_SET,             Indexer_Set, NoSig)
DEFINE_METHOD(MAP_TO_DICTIONARY_ADAPTER,    KEYS,                    Keys, NoSig)
DEFINE_METHOD(MAP_TO_DICTIONARY_ADAPTER,    VALUES,                  Values, NoSig)
DEFINE_METHOD(MAP_TO_DICTIONARY_ADAPTER,    CONTAINS_KEY,            ContainsKey, NoSig)
DEFINE_METHOD(MAP_TO_DICTIONARY_ADAPTER,    ADD,                     Add, NoSig)
DEFINE_METHOD(MAP_TO_DICTIONARY_ADAPTER,    REMOVE,                  Remove, NoSig)
DEFINE_METHOD(MAP_TO_DICTIONARY_ADAPTER,    TRY_GET_VALUE,           TryGetValue, NoSig)

DEFINE_CLASS(VECTOR_TO_COLLECTION_ADAPTER,  WinRT,                   VectorToCollectionAdapter)
DEFINE_METHOD(VECTOR_TO_COLLECTION_ADAPTER, COUNT,                   Count, NoSig)
DEFINE_METHOD(VECTOR_TO_COLLECTION_ADAPTER, IS_READ_ONLY,            IsReadOnly, NoSig)
DEFINE_METHOD(VECTOR_TO_COLLECTION_ADAPTER, ADD,                     Add, NoSig)
DEFINE_METHOD(VECTOR_TO_COLLECTION_ADAPTER, CLEAR,                   Clear, NoSig)
DEFINE_METHOD(VECTOR_TO_COLLECTION_ADAPTER, CONTAINS,                Contains, NoSig)
DEFINE_METHOD(VECTOR_TO_COLLECTION_ADAPTER, COPY_TO,                 CopyTo, NoSig)
DEFINE_METHOD(VECTOR_TO_COLLECTION_ADAPTER, REMOVE,                  Remove, NoSig)

DEFINE_CLASS(MAP_TO_COLLECTION_ADAPTER,     WinRT,                   MapToCollectionAdapter)
DEFINE_METHOD(MAP_TO_COLLECTION_ADAPTER,    COUNT,                   Count, NoSig)
DEFINE_METHOD(MAP_TO_COLLECTION_ADAPTER,    IS_READ_ONLY,            IsReadOnly, NoSig)
DEFINE_METHOD(MAP_TO_COLLECTION_ADAPTER,    ADD,                     Add, NoSig)
DEFINE_METHOD(MAP_TO_COLLECTION_ADAPTER,    CLEAR,                   Clear, NoSig)
DEFINE_METHOD(MAP_TO_COLLECTION_ADAPTER,    CONTAINS,                Contains, NoSig)
DEFINE_METHOD(MAP_TO_COLLECTION_ADAPTER,    COPY_TO,                 CopyTo, NoSig)
DEFINE_METHOD(MAP_TO_COLLECTION_ADAPTER,    REMOVE,                  Remove, NoSig)

DEFINE_CLASS(BINDABLEITERABLE_TO_ENUMERABLE_ADAPTER, WinRT,          BindableIterableToEnumerableAdapter)
DEFINE_METHOD(BINDABLEITERABLE_TO_ENUMERABLE_ADAPTER, GET_ENUMERATOR_STUB, GetEnumerator_Stub, NoSig)

DEFINE_CLASS(BINDABLEVECTOR_TO_LIST_ADAPTER,       WinRT,            BindableVectorToListAdapter)
DEFINE_METHOD(BINDABLEVECTOR_TO_LIST_ADAPTER,      INDEXER_GET,      Indexer_Get, NoSig)
DEFINE_METHOD(BINDABLEVECTOR_TO_LIST_ADAPTER,      INDEXER_SET,      Indexer_Set, NoSig)
DEFINE_METHOD(BINDABLEVECTOR_TO_LIST_ADAPTER,      ADD,              Add, NoSig)
DEFINE_METHOD(BINDABLEVECTOR_TO_LIST_ADAPTER,      CONTAINS,         Contains, NoSig)
DEFINE_METHOD(BINDABLEVECTOR_TO_LIST_ADAPTER,      CLEAR,            Clear, NoSig)
DEFINE_METHOD(BINDABLEVECTOR_TO_LIST_ADAPTER,      IS_READ_ONLY,     IsReadOnly, NoSig)
DEFINE_METHOD(BINDABLEVECTOR_TO_LIST_ADAPTER,      IS_FIXED_SIZE,    IsFixedSize, NoSig)
DEFINE_METHOD(BINDABLEVECTOR_TO_LIST_ADAPTER,      INDEX_OF,         IndexOf, NoSig)
DEFINE_METHOD(BINDABLEVECTOR_TO_LIST_ADAPTER,      INSERT,           Insert, NoSig)
DEFINE_METHOD(BINDABLEVECTOR_TO_LIST_ADAPTER,      REMOVE,           Remove, NoSig)
DEFINE_METHOD(BINDABLEVECTOR_TO_LIST_ADAPTER,      REMOVE_AT,        RemoveAt, NoSig)

DEFINE_CLASS(BINDABLEVECTOR_TO_COLLECTION_ADAPTER,  WinRT,           BindableVectorToCollectionAdapter)
DEFINE_METHOD(BINDABLEVECTOR_TO_COLLECTION_ADAPTER, COPY_TO,         CopyTo, NoSig)
DEFINE_METHOD(BINDABLEVECTOR_TO_COLLECTION_ADAPTER, COUNT,           Count, NoSig)
DEFINE_METHOD(BINDABLEVECTOR_TO_COLLECTION_ADAPTER, SYNC_ROOT,       SyncRoot, NoSig)
DEFINE_METHOD(BINDABLEVECTOR_TO_COLLECTION_ADAPTER, IS_SYNCHRONIZED, IsSynchronized, NoSig)

DEFINE_CLASS(ENUMERABLE_TO_ITERABLE_ADAPTER, WinRT,                  EnumerableToIterableAdapter)
DEFINE_METHOD(ENUMERABLE_TO_ITERABLE_ADAPTER, FIRST_STUB,            First_Stub, NoSig)

DEFINE_CLASS(LIST_TO_VECTOR_ADAPTER,       WinRT,                    ListToVectorAdapter)
DEFINE_METHOD(LIST_TO_VECTOR_ADAPTER,      GET_AT,                   GetAt, NoSig)
DEFINE_METHOD(LIST_TO_VECTOR_ADAPTER,      SIZE,                     Size, NoSig)
DEFINE_METHOD(LIST_TO_VECTOR_ADAPTER,      GET_VIEW,                 GetView, NoSig)
DEFINE_METHOD(LIST_TO_VECTOR_ADAPTER,      INDEX_OF,                 IndexOf, NoSig)
DEFINE_METHOD(LIST_TO_VECTOR_ADAPTER,      SET_AT,                   SetAt, NoSig)
DEFINE_METHOD(LIST_TO_VECTOR_ADAPTER,      INSERT_AT,                InsertAt, NoSig)
DEFINE_METHOD(LIST_TO_VECTOR_ADAPTER,      REMOVE_AT,                RemoveAt, NoSig)
DEFINE_METHOD(LIST_TO_VECTOR_ADAPTER,      APPEND,                   Append, NoSig)
DEFINE_METHOD(LIST_TO_VECTOR_ADAPTER,      REMOVE_AT_END,            RemoveAtEnd, NoSig)
DEFINE_METHOD(LIST_TO_VECTOR_ADAPTER,      CLEAR,                    Clear, NoSig)
DEFINE_METHOD(LIST_TO_VECTOR_ADAPTER,      GET_MANY,                 GetMany, NoSig)
DEFINE_METHOD(LIST_TO_VECTOR_ADAPTER,      REPLACE_ALL,              ReplaceAll, NoSig)

DEFINE_CLASS(DICTIONARY_TO_MAP_ADAPTER,    WinRT,                    DictionaryToMapAdapter)
DEFINE_METHOD(DICTIONARY_TO_MAP_ADAPTER,   LOOKUP,                   Lookup, NoSig)
DEFINE_METHOD(DICTIONARY_TO_MAP_ADAPTER,   SIZE,                     Size, NoSig)
DEFINE_METHOD(DICTIONARY_TO_MAP_ADAPTER,   HAS_KEY,                  HasKey, NoSig)
DEFINE_METHOD(DICTIONARY_TO_MAP_ADAPTER,   GET_VIEW,                 GetView, NoSig)
DEFINE_METHOD(DICTIONARY_TO_MAP_ADAPTER,   INSERT,                   Insert, NoSig)
DEFINE_METHOD(DICTIONARY_TO_MAP_ADAPTER,   REMOVE,                   Remove, NoSig)
DEFINE_METHOD(DICTIONARY_TO_MAP_ADAPTER,   CLEAR,                    Clear, NoSig)

DEFINE_CLASS(IVECTORVIEW_TO_IREADONLYCOLLECTION_ADAPTER,  WinRT,     VectorViewToReadOnlyCollectionAdapter)
DEFINE_METHOD(IVECTORVIEW_TO_IREADONLYCOLLECTION_ADAPTER, COUNT,     Count, NoSig)

DEFINE_CLASS(IMAPVIEW_TO_IREADONLYCOLLECTION_ADAPTER,  WinRT,        MapViewToReadOnlyCollectionAdapter)
DEFINE_METHOD(IMAPVIEW_TO_IREADONLYCOLLECTION_ADAPTER, COUNT,        Count, NoSig)

DEFINE_CLASS(IREADONLYLIST_TO_IVECTORVIEW_ADAPTER,     WinRT,        IReadOnlyListToIVectorViewAdapter)
DEFINE_METHOD(IREADONLYLIST_TO_IVECTORVIEW_ADAPTER,    GETAT,        GetAt, NoSig)
DEFINE_METHOD(IREADONLYLIST_TO_IVECTORVIEW_ADAPTER,    GETMANY,      GetMany, NoSig)
DEFINE_METHOD(IREADONLYLIST_TO_IVECTORVIEW_ADAPTER,    INDEXOF,      IndexOf, NoSig)
DEFINE_METHOD(IREADONLYLIST_TO_IVECTORVIEW_ADAPTER,    SIZE,         Size, NoSig)

DEFINE_CLASS(INDEXER_GET_DELEGATE,                     WinRT,        Indexer_Get_Delegate`1)
DEFINE_CLASS(IVECTORVIEW_TO_IREADONLYLIST_ADAPTER,     WinRT,        IVectorViewToIReadOnlyListAdapter)
DEFINE_METHOD(IVECTORVIEW_TO_IREADONLYLIST_ADAPTER,    INDEXER_GET,  Indexer_Get, NoSig)
DEFINE_METHOD(IVECTORVIEW_TO_IREADONLYLIST_ADAPTER,    INDEXER_GET_VARIANCE, Indexer_Get_Variance, NoSig)

DEFINE_CLASS(IREADONLYDICTIONARY_TO_IMAPVIEW_ADAPTER,  WinRT,        IReadOnlyDictionaryToIMapViewAdapter)
DEFINE_METHOD(IREADONLYDICTIONARY_TO_IMAPVIEW_ADAPTER, HASKEY,       HasKey, NoSig)
DEFINE_METHOD(IREADONLYDICTIONARY_TO_IMAPVIEW_ADAPTER, LOOKUP,       Lookup, NoSig)
DEFINE_METHOD(IREADONLYDICTIONARY_TO_IMAPVIEW_ADAPTER, SIZE,         Size, NoSig)
DEFINE_METHOD(IREADONLYDICTIONARY_TO_IMAPVIEW_ADAPTER, SPLIT,        Split, NoSig)

DEFINE_CLASS(IMAPVIEW_TO_IREADONLYDICTIONARY_ADAPTER,  WinRT,        IMapViewToIReadOnlyDictionaryAdapter)
DEFINE_METHOD(IMAPVIEW_TO_IREADONLYDICTIONARY_ADAPTER, CONTAINSKEY,  ContainsKey, NoSig)
DEFINE_METHOD(IMAPVIEW_TO_IREADONLYDICTIONARY_ADAPTER, INDEXER_GET,  Indexer_Get, NoSig)
DEFINE_METHOD(IMAPVIEW_TO_IREADONLYDICTIONARY_ADAPTER, TRYGETVALUE,  TryGetValue, NoSig)
DEFINE_METHOD(IMAPVIEW_TO_IREADONLYDICTIONARY_ADAPTER, KEYS,         Keys, NoSig)
DEFINE_METHOD(IMAPVIEW_TO_IREADONLYDICTIONARY_ADAPTER, VALUES,       Values, NoSig)

DEFINE_CLASS(ENUMERABLE_TO_BINDABLEITERABLE_ADAPTER,   WinRT,        EnumerableToBindableIterableAdapter)
DEFINE_METHOD(ENUMERABLE_TO_BINDABLEITERABLE_ADAPTER,  FIRST_STUB,   First_Stub, NoSig)

DEFINE_CLASS(LIST_TO_BINDABLEVECTOR_ADAPTER,       WinRT,            ListToBindableVectorAdapter)
DEFINE_METHOD(LIST_TO_BINDABLEVECTOR_ADAPTER,      GET_AT,           GetAt, NoSig)
DEFINE_METHOD(LIST_TO_BINDABLEVECTOR_ADAPTER,      SIZE,             Size, NoSig)
DEFINE_METHOD(LIST_TO_BINDABLEVECTOR_ADAPTER,      GET_VIEW,         GetView, NoSig)
DEFINE_METHOD(LIST_TO_BINDABLEVECTOR_ADAPTER,      INDEX_OF,         IndexOf, NoSig)
DEFINE_METHOD(LIST_TO_BINDABLEVECTOR_ADAPTER,      SET_AT,           SetAt, NoSig)
DEFINE_METHOD(LIST_TO_BINDABLEVECTOR_ADAPTER,      INSERT_AT,        InsertAt, NoSig)
DEFINE_METHOD(LIST_TO_BINDABLEVECTOR_ADAPTER,      REMOVE_AT,        RemoveAt, NoSig)
DEFINE_METHOD(LIST_TO_BINDABLEVECTOR_ADAPTER,      APPEND,           Append, NoSig)
DEFINE_METHOD(LIST_TO_BINDABLEVECTOR_ADAPTER,      REMOVE_AT_END,    RemoveAtEnd, NoSig)
DEFINE_METHOD(LIST_TO_BINDABLEVECTOR_ADAPTER,      CLEAR,            Clear, NoSig)

DEFINE_CLASS(IDISPOSABLE_TO_ICLOSABLE_ADAPTER,     WinRT,            IDisposableToIClosableAdapter)
DEFINE_METHOD(IDISPOSABLE_TO_ICLOSABLE_ADAPTER,    CLOSE,            Close, NoSig)

DEFINE_CLASS(ICLOSABLE_TO_IDISPOSABLE_ADAPTER,     WinRT,            IClosableToIDisposableAdapter)
DEFINE_METHOD(ICLOSABLE_TO_IDISPOSABLE_ADAPTER,    DISPOSE,          Dispose, NoSig)

#endif // FEATURE_COMINTEROP

DEFINE_CLASS(SZARRAYHELPER,         System,                        SZArrayHelper)
// Note: The order of methods here has to match order they are implemented on the interfaces in
// IEnumerable`1
DEFINE_METHOD(SZARRAYHELPER,        GETENUMERATOR,          GetEnumerator,              NoSig)
// ICollection`1/IReadOnlyCollection`1
DEFINE_METHOD(SZARRAYHELPER,        GET_COUNT,              get_Count,                  NoSig)
DEFINE_METHOD(SZARRAYHELPER,        ISREADONLY,             get_IsReadOnly,             NoSig)
DEFINE_METHOD(SZARRAYHELPER,        ADD,                    Add,                        NoSig)
DEFINE_METHOD(SZARRAYHELPER,        CLEAR,                  Clear,                      NoSig)
DEFINE_METHOD(SZARRAYHELPER,        CONTAINS,               Contains,                   NoSig)
DEFINE_METHOD(SZARRAYHELPER,        COPYTO,                 CopyTo,                     NoSig)
DEFINE_METHOD(SZARRAYHELPER,        REMOVE,                 Remove,                     NoSig)
// IList`1/IReadOnlyList`1
DEFINE_METHOD(SZARRAYHELPER,        GET_ITEM,               get_Item,                   NoSig)
DEFINE_METHOD(SZARRAYHELPER,        SET_ITEM,               set_Item,                   NoSig)
DEFINE_METHOD(SZARRAYHELPER,        INDEXOF,                IndexOf,                    NoSig)
DEFINE_METHOD(SZARRAYHELPER,        INSERT,                 Insert,                     NoSig)
DEFINE_METHOD(SZARRAYHELPER,        REMOVEAT,               RemoveAt,                   NoSig)

DEFINE_CLASS(IENUMERABLEGENERIC,    CollectionsGeneric,     IEnumerable`1)
DEFINE_CLASS(IENUMERATORGENERIC,    CollectionsGeneric,     IEnumerator`1)
DEFINE_CLASS(ICOLLECTIONGENERIC,    CollectionsGeneric,     ICollection`1)
DEFINE_CLASS(ILISTGENERIC,          CollectionsGeneric,     IList`1)
DEFINE_CLASS(IREADONLYCOLLECTIONGENERIC,CollectionsGeneric, IReadOnlyCollection`1)
DEFINE_CLASS(IREADONLYLISTGENERIC,  CollectionsGeneric,     IReadOnlyList`1)
DEFINE_CLASS(IREADONLYDICTIONARYGENERIC,CollectionsGeneric, IReadOnlyDictionary`2)
DEFINE_CLASS(IDICTIONARYGENERIC,    CollectionsGeneric,     IDictionary`2)
DEFINE_CLASS(KEYVALUEPAIRGENERIC,   CollectionsGeneric,     KeyValuePair`2)

DEFINE_CLASS(ICOMPARABLEGENERIC,    System,                 IComparable`1)
DEFINE_CLASS(IEQUATABLEGENERIC,     System,                 IEquatable`1)

DEFINE_CLASS_U(Reflection,             LoaderAllocator,          LoaderAllocatorObject)
DEFINE_FIELD_U(m_slots,                  LoaderAllocatorObject,      m_pSlots)
DEFINE_FIELD_U(m_slotsUsed,              LoaderAllocatorObject,      m_slotsUsed)
DEFINE_CLASS(LOADERALLOCATOR,           Reflection,             LoaderAllocator)
DEFINE_METHOD(LOADERALLOCATOR,          CTOR,                   .ctor,                    IM_RetVoid)

DEFINE_CLASS_U(Reflection,             LoaderAllocatorScout,     LoaderAllocatorScoutObject)
DEFINE_FIELD_U(m_nativeLoaderAllocator,  LoaderAllocatorScoutObject,      m_nativeLoaderAllocator)
DEFINE_CLASS(LOADERALLOCATORSCOUT,      Reflection,             LoaderAllocatorScout)

DEFINE_CLASS(CONTRACTEXCEPTION,     CodeContracts,  ContractException)

DEFINE_CLASS_U(CodeContracts,       ContractException,          ContractExceptionObject)
DEFINE_FIELD_U(_Kind,               ContractExceptionObject,    _Kind)
DEFINE_FIELD_U(_UserMessage,        ContractExceptionObject,    _UserMessage)
DEFINE_FIELD_U(_Condition,          ContractExceptionObject,    _Condition)

// The COM interfaces for the reflection types.
#if defined(FEATURE_COMINTEROP) && !defined(FEATURE_CORECLR)
DEFINE_CLASS(IAPPDOMAIN,        System,              _AppDomain)
DEFINE_CLASS(ITYPE,             InteropServices,     _Type)
DEFINE_CLASS(IASSEMBLY,         InteropServices,     _Assembly)
DEFINE_CLASS(IMEMBERINFO,       InteropServices,     _MemberInfo)
DEFINE_CLASS(IMETHODBASE,       InteropServices,     _MethodBase)
DEFINE_CLASS(IMETHODINFO,       InteropServices,     _MethodInfo)
DEFINE_CLASS(ICONSTRUCTORINFO,  InteropServices,     _ConstructorInfo)
DEFINE_CLASS(IFIELDINFO,        InteropServices,     _FieldInfo)
DEFINE_CLASS(IPROPERTYINFO,     InteropServices,     _PropertyInfo)
DEFINE_CLASS(IEVENTINFO,        InteropServices,     _EventInfo)
DEFINE_CLASS(IPARAMETERINFO,    InteropServices,     _ParameterInfo)
DEFINE_CLASS(IMODULE,           InteropServices,     _Module)
#endif // FEATURE_COMINTEROP && !FEATURE_CORECLR

#ifdef FEATURE_COMPRESSEDSTACK
DEFINE_CLASS_U(Security,           FrameSecurityDescriptorWithResolver, FrameSecurityDescriptorWithResolverBaseObject)
DEFINE_FIELD_U(m_resolver,         FrameSecurityDescriptorWithResolverBaseObject,  m_resolver)
DEFINE_CLASS(FRAME_SECURITY_DESCRIPTOR_WITH_RESOLVER, Security, FrameSecurityDescriptorWithResolver)
#endif // FEATURE_COMPRESSEDSTACK

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(ASYNC_TRACING_EVENT_ARGS,       WindowsFoundationDiag,         TracingStatusChangedEventArgs)
DEFINE_CLASS(IASYNC_TRACING_EVENT_ARGS,      WindowsFoundationDiag,         ITracingStatusChangedEventArgs)
#endif // FEATURE_COMINTEROP

DEFINE_CLASS(MODULEBASE,        Reflection,         Module)

#ifdef FEATURE_ICASTABLE
DEFINE_CLASS(ICASTABLE,         CompilerServices,   ICastable)

DEFINE_CLASS(ICASTABLEHELPERS,         CompilerServices,   ICastableHelpers)
DEFINE_METHOD(ICASTABLEHELPERS,        ISINSTANCEOF,       IsInstanceOfInterface, SM_ICastable_RtType_RefException_RetBool)
DEFINE_METHOD(ICASTABLEHELPERS,        GETIMPLTYPE,        GetImplType, SM_ICastable_RtType_RetRtType)

#endif // FEATURE_ICASTABLE

DEFINE_CLASS(CUTF8MARSHALER, StubHelpers, UTF8Marshaler)
DEFINE_METHOD(CUTF8MARSHALER, CONVERT_TO_NATIVE, ConvertToNative, SM_Int_Str_IntPtr_RetIntPtr)
DEFINE_METHOD(CUTF8MARSHALER, CONVERT_TO_MANAGED, ConvertToManaged, SM_IntPtr_RetStr)
DEFINE_METHOD(CUTF8MARSHALER, CLEAR_NATIVE, ClearNative, SM_IntPtr_RetVoid)

DEFINE_CLASS(UTF8BUFFERMARSHALER, StubHelpers, UTF8BufferMarshaler)
DEFINE_METHOD(UTF8BUFFERMARSHALER, CONVERT_TO_NATIVE, ConvertToNative, NoSig)
DEFINE_METHOD(UTF8BUFFERMARSHALER, CONVERT_TO_MANAGED, ConvertToManaged, NoSig)

#undef DEFINE_CLASS
#undef DEFINE_METHOD
#undef DEFINE_FIELD
#undef DEFINE_CLASS_U
#undef DEFINE_FIELD_U
