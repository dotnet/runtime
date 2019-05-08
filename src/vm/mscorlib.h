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
// Note: This file gets parsed by the Mono IL Linker (https://github.com/mono/linker/) which may throw an exception during parsing.
// Specifically, this (https://github.com/mono/linker/blob/master/corebuild/integration/ILLink.Tasks/CreateRuntimeRootDescriptorFile.cs) will try to 
// parse this header, and it may throw an exception while doing that. If you edit this file and get a build failure on msbuild.exe D:\repos\coreclr\build.proj
// you might want to check out the parser linked above.
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

#ifndef DEFINE_STATIC_SET_PROPERTY
#define DEFINE_STATIC_SET_PROPERTY(classId, id, stringName, gSign) \
    DEFINE_STATIC_PROPERTY(classId, id, stringName, gSign) \
    DEFINE_METHOD(classId, SET_ ## id, set_ ## stringName, SM_## gSign ## _RetVoid)
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

DEFINE_CLASS(APPCONTEXT,            System,                 AppContext)
DEFINE_METHOD(APPCONTEXT,   SETUP,              Setup,          SM_PtrPtrChar_PtrPtrChar_Int_RetVoid)
DEFINE_METHOD(APPCONTEXT,   ON_PROCESS_EXIT,    OnProcessExit,  SM_RetVoid)
DEFINE_FIELD(APPCONTEXT, UNHANDLED_EXCEPTION,           UnhandledException)
DEFINE_FIELD(APPCONTEXT, FIRST_CHANCE_EXCEPTION,        FirstChanceException)

DEFINE_CLASS(ARG_ITERATOR,          System,                 ArgIterator)
DEFINE_CLASS_U(System,              ArgIterator,            VARARGS)  // Includes a SigPointer.
DEFINE_METHOD(ARG_ITERATOR,         CTOR2,                  .ctor,                      IM_RuntimeArgumentHandle_PtrVoid_RetVoid)

DEFINE_CLASS(ARGUMENT_HANDLE,       System,                 RuntimeArgumentHandle)

DEFINE_CLASS(ARRAY,                 System,                 Array)
DEFINE_PROPERTY(ARRAY,              LENGTH,                 Length,                     Int)
DEFINE_METHOD(ARRAY,                GET_RAW_ARRAY_DATA,     GetRawArrayData, IM_RetRefByte)

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
DEFINE_FIELD_U(_name,                      AssemblyNameBaseObject, _name)
DEFINE_FIELD_U(_publicKey,                 AssemblyNameBaseObject, _publicKey)
DEFINE_FIELD_U(_publicKeyToken,            AssemblyNameBaseObject, _publicKeyToken)
DEFINE_FIELD_U(_cultureInfo,               AssemblyNameBaseObject, _cultureInfo)
DEFINE_FIELD_U(_codeBase,                  AssemblyNameBaseObject, _codeBase)
DEFINE_FIELD_U(_version,                   AssemblyNameBaseObject, _version)
DEFINE_FIELD_U(_hashAlgorithm,             AssemblyNameBaseObject, _hashAlgorithm)
DEFINE_FIELD_U(_versionCompatibility,      AssemblyNameBaseObject, _versionCompatibility)
DEFINE_FIELD_U(_flags,                     AssemblyNameBaseObject, _flags)
DEFINE_CLASS(ASSEMBLY_NAME,         Reflection,             AssemblyName)
DEFINE_METHOD(ASSEMBLY_NAME,        CTOR,                   .ctor,                     IM_Str_ArrB_ArrB_Ver_CI_AHA_AVC_Str_ANF_SNKP_RetV)
DEFINE_METHOD(ASSEMBLY_NAME,        SET_PROC_ARCH_INDEX,    SetProcArchIndex,          IM_PEK_IFM_RetV)

DEFINE_CLASS_U(System,                 Version,                    VersionBaseObject)
DEFINE_FIELD_U(_Major,                     VersionBaseObject,    m_Major)
DEFINE_FIELD_U(_Minor,                     VersionBaseObject,    m_Minor)
DEFINE_FIELD_U(_Build,                     VersionBaseObject,    m_Build)
DEFINE_FIELD_U(_Revision,                  VersionBaseObject,    m_Revision)
DEFINE_CLASS(VERSION,               System,                 Version)
DEFINE_METHOD(VERSION,              CTOR_Ix2,               .ctor,                      IM_Int_Int_RetVoid)
DEFINE_METHOD(VERSION,              CTOR_Ix3,               .ctor,                      IM_Int_Int_Int_RetVoid)
DEFINE_METHOD(VERSION,              CTOR_Ix4,               .ctor,                      IM_Int_Int_Int_Int_RetVoid)

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
DEFINE_CLASS(ASSEMBLY,              Reflection,             RuntimeAssembly)
DEFINE_FIELD(ASSEMBLY,              HANDLE,                 m_assembly)
DEFINE_METHOD(ASSEMBLY,             GET_NAME,               GetName,                    IM_RetAssemblyName)
DEFINE_METHOD(ASSEMBLY,             ON_MODULE_RESOLVE,      OnModuleResolveEvent,       IM_Str_RetModule)


DEFINE_CLASS(ASYNCCALLBACK,         System,                 AsyncCallback)
DEFINE_CLASS(ATTRIBUTE,             System,                 Attribute)


DEFINE_CLASS(BINDER,                Reflection,             Binder)
DEFINE_METHOD(BINDER,               CHANGE_TYPE,            ChangeType,                 IM_Obj_Type_CultureInfo_RetObj)

DEFINE_CLASS(BINDING_FLAGS,         Reflection,             BindingFlags)

DEFINE_CLASS_U(System,                 RuntimeType,            ReflectClassBaseObject)
DEFINE_FIELD_U(m_cache,                ReflectClassBaseObject,        m_cache)
DEFINE_FIELD_U(m_handle,               ReflectClassBaseObject,        m_typeHandle)
DEFINE_FIELD_U(m_keepalive,            ReflectClassBaseObject,        m_keepalive)
DEFINE_CLASS(CLASS,                 System,                 RuntimeType)
DEFINE_FIELD(CLASS,                 TYPEHANDLE,             m_handle)
DEFINE_METHOD(CLASS,                GET_PROPERTIES,         GetProperties,              IM_BindingFlags_RetArrPropertyInfo)
DEFINE_METHOD(CLASS,                GET_FIELDS,             GetFields,                  IM_BindingFlags_RetArrFieldInfo)
DEFINE_METHOD(CLASS,                GET_METHODS,            GetMethods,                 IM_BindingFlags_RetArrMethodInfo)
DEFINE_METHOD(CLASS,                INVOKE_MEMBER,          InvokeMember,               IM_Str_BindingFlags_Binder_Obj_ArrObj_ArrParameterModifier_CultureInfo_ArrStr_RetObj)
DEFINE_METHOD(CLASS,                GET_METHOD_BASE,        GetMethodBase,              SM_RuntimeType_RuntimeMethodHandleInternal_RetMethodBase)
DEFINE_METHOD(CLASS,                GET_FIELD_INFO,         GetFieldInfo,               SM_RuntimeType_IRuntimeFieldInfo_RetFieldInfo)
DEFINE_METHOD(CLASS,                GET_PROPERTY_INFO,      GetPropertyInfo,            SM_RuntimeType_Int_RetPropertyInfo)

#ifdef FEATURE_COMINTEROP
DEFINE_METHOD(CLASS,                FORWARD_CALL_TO_INVOKE, ForwardCallToInvokeMember,  IM_Str_BindingFlags_Obj_ArrObj_ArrBool_ArrInt_ArrType_Type_RetObj)
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(BSTR_WRAPPER,          Interop,                BStrWrapper)
DEFINE_CLASS(CURRENCY_WRAPPER,      Interop,                CurrencyWrapper)
DEFINE_CLASS(DISPATCH_WRAPPER,      Interop,                DispatchWrapper)
DEFINE_CLASS(ERROR_WRAPPER,         Interop,                ErrorWrapper)
DEFINE_CLASS(UNKNOWN_WRAPPER,       Interop,                UnknownWrapper)
DEFINE_CLASS(VARIANT_WRAPPER,       Interop,                VariantWrapper)
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS_U(System,                 __ComObject,            ComObject)
DEFINE_FIELD_U(m_ObjectToDataMap,      ComObject,              m_ObjectToDataMap)
DEFINE_CLASS(COM_OBJECT,            System,                 __ComObject)
DEFINE_METHOD(COM_OBJECT,           RELEASE_ALL_DATA,       ReleaseAllData,             IM_RetVoid)
DEFINE_METHOD(COM_OBJECT,           GET_EVENT_PROVIDER,     GetEventProvider,           IM_Class_RetObj)

DEFINE_CLASS(LICENSE_INTEROP_PROXY,  InternalInteropServices, LicenseInteropProxy)
DEFINE_METHOD(LICENSE_INTEROP_PROXY, CREATE,                  Create,                  SM_RetObj)
DEFINE_METHOD(LICENSE_INTEROP_PROXY, GETCURRENTCONTEXTINFO,   GetCurrentContextInfo,   IM_RuntimeTypeHandle_RefBool_RefIntPtr_RetVoid)
DEFINE_METHOD(LICENSE_INTEROP_PROXY, SAVEKEYINCURRENTCONTEXT, SaveKeyInCurrentContext, IM_IntPtr_RetVoid)

DEFINE_CLASS(RUNTIME_CLASS,                  WinRT,         RuntimeClass)

#endif // FEATURE_COMINTEROP

DEFINE_CLASS_U(Interop,                CriticalHandle,             CriticalHandle)
DEFINE_FIELD_U(handle,                     CriticalHandle,     m_handle)
DEFINE_FIELD_U(_isClosed,                  CriticalHandle,     m_isClosed)
DEFINE_CLASS(CRITICAL_HANDLE,       Interop,                CriticalHandle)
DEFINE_FIELD(CRITICAL_HANDLE,       HANDLE,                 handle)
DEFINE_METHOD(CRITICAL_HANDLE,      RELEASE_HANDLE,         ReleaseHandle,              IM_RetBool)
DEFINE_METHOD(CRITICAL_HANDLE,      GET_IS_INVALID,         get_IsInvalid,              IM_RetBool)
DEFINE_METHOD(CRITICAL_HANDLE,      DISPOSE,                Dispose,                    IM_RetVoid)
DEFINE_METHOD(CRITICAL_HANDLE,      DISPOSE_BOOL,           Dispose,                    IM_Bool_RetVoid)

DEFINE_CLASS(HANDLE_REF,            Interop,                HandleRef)
DEFINE_FIELD(HANDLE_REF,            WRAPPER,                _wrapper)
DEFINE_FIELD(HANDLE_REF,            HANDLE,                 _handle)

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

DEFINE_CLASS_U(Globalization,          CultureInfo,        CultureInfoBaseObject)
DEFINE_FIELD_U(_compareInfo,       CultureInfoBaseObject,  _compareInfo)
DEFINE_FIELD_U(_textInfo,          CultureInfoBaseObject,  _textInfo)
DEFINE_FIELD_U(_numInfo,           CultureInfoBaseObject,  _numInfo)
DEFINE_FIELD_U(_dateTimeInfo,      CultureInfoBaseObject,  _dateTimeInfo)
DEFINE_FIELD_U(_calendar,          CultureInfoBaseObject,  _calendar)
DEFINE_FIELD_U(_consoleFallbackCulture, CultureInfoBaseObject, _consoleFallbackCulture)
DEFINE_FIELD_U(_name,             CultureInfoBaseObject,  _name)
DEFINE_FIELD_U(_nonSortName,      CultureInfoBaseObject,  _nonSortName)
DEFINE_FIELD_U(_sortName,         CultureInfoBaseObject,  _sortName)
DEFINE_FIELD_U(_parent,           CultureInfoBaseObject,  _parent)
DEFINE_FIELD_U(_isReadOnly,        CultureInfoBaseObject,  _isReadOnly)
DEFINE_FIELD_U(_isInherited,      CultureInfoBaseObject,  _isInherited)
DEFINE_CLASS(CULTURE_INFO,          Globalization,          CultureInfo)
DEFINE_METHOD(CULTURE_INFO,         STR_CTOR,               .ctor,                      IM_Str_RetVoid)
DEFINE_FIELD(CULTURE_INFO,          CURRENT_CULTURE,        s_userDefaultCulture)
DEFINE_PROPERTY(CULTURE_INFO,       NAME,                   Name,                       Str)
#ifdef FEATURE_USE_LCID
DEFINE_METHOD(CULTURE_INFO,         INT_CTOR,               .ctor,                      IM_Int_RetVoid)
DEFINE_PROPERTY(CULTURE_INFO,       ID,                     LCID,                       Int)
#endif
DEFINE_FIELD(CULTURE_INFO,          CULTURE,                s_currentThreadCulture)
DEFINE_FIELD(CULTURE_INFO,          UI_CULTURE,             s_currentThreadUICulture)
DEFINE_STATIC_SET_PROPERTY(CULTURE_INFO, CURRENT_CULTURE,      CurrentCulture,     CultureInfo)
DEFINE_STATIC_SET_PROPERTY(CULTURE_INFO, CURRENT_UI_CULTURE,   CurrentUICulture,   CultureInfo)

DEFINE_CLASS(CURRENCY,              System,                 Currency)
DEFINE_METHOD(CURRENCY,             DECIMAL_CTOR,           .ctor,                      IM_Dec_RetVoid)

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

DEFINE_CLASS(DYNAMICMETHOD,         ReflectionEmit,         DynamicMethod)

DEFINE_CLASS(DYNAMICRESOLVER,       ReflectionEmit,         DynamicResolver)
DEFINE_FIELD(DYNAMICRESOLVER,       DYNAMIC_METHOD,         m_method)

DEFINE_CLASS(EMPTY,                 System,                 Empty)

DEFINE_CLASS(ENC_HELPER,            Diagnostics,            EditAndContinueHelper)
DEFINE_FIELD(ENC_HELPER,            OBJECT_REFERENCE,       _objectReference)

DEFINE_CLASS(ENCODING,              Text,                   Encoding)

DEFINE_CLASS(RUNE,                  Text,                   Rune)

#ifdef FEATURE_UTF8STRING
DEFINE_CLASS(CHAR8,                 System,                 Char8)
#endif // FEATURE_UTF8STRING

DEFINE_CLASS(ENUM,                  System,                 Enum)

DEFINE_CLASS(ENVIRONMENT,           System,                 Environment)
DEFINE_METHOD(ENVIRONMENT,       GET_RESOURCE_STRING_LOCAL, GetResourceStringLocal,     SM_Str_RetStr)
DEFINE_METHOD(ENVIRONMENT,       SET_COMMAND_LINE_ARGS,     SetCommandLineArgs,         SM_ArrStr_RetVoid)

DEFINE_CLASS(EVENT,                 Reflection,             RuntimeEventInfo)

DEFINE_CLASS(EVENT_ARGS,            System,                 EventArgs)

DEFINE_CLASS(EVENT_HANDLERGENERIC,  System,                 EventHandler`1)

DEFINE_CLASS(EVENT_INFO,            Reflection,             EventInfo)

DEFINE_CLASS_U(System,                 Exception,      ExceptionObject)
DEFINE_FIELD_U(_exceptionMethod,   ExceptionObject,    _exceptionMethod)
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
DEFINE_FIELD_U(_HResult,           ExceptionObject,    _HResult)
DEFINE_FIELD_U(_xcode,             ExceptionObject,    _xcode)
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


DEFINE_CLASS(ISERIALIZABLE,         Serialization,          ISerializable)
DEFINE_CLASS(IOBJECTREFERENCE,      Serialization,          IObjectReference)
DEFINE_CLASS(IDESERIALIZATIONCB,    Serialization,          IDeserializationCallback)
DEFINE_CLASS(STREAMING_CONTEXT,     Serialization,          StreamingContext)
DEFINE_CLASS(SERIALIZATION_INFO,    Serialization,          SerializationInfo)
DEFINE_CLASS(DESERIALIZATION_TRACKER, Serialization, DeserializationTracker)


DEFINE_CLASS(IENUMERATOR,           Collections,            IEnumerator)

DEFINE_CLASS(IENUMERABLE,           Collections,            IEnumerable)
DEFINE_CLASS(ICOLLECTION,           Collections,            ICollection)
DEFINE_CLASS(ILIST,                 Collections,            IList)
DEFINE_CLASS(IDISPOSABLE,           System,                 IDisposable)

DEFINE_CLASS(IEXPANDO,              Expando,                IExpando)
DEFINE_METHOD(IEXPANDO,             ADD_FIELD,              AddField,                   IM_Str_RetFieldInfo)
DEFINE_METHOD(IEXPANDO,             REMOVE_MEMBER,          RemoveMember,               IM_MemberInfo_RetVoid)

DEFINE_CLASS(IREFLECT,              Reflection,             IReflect)
DEFINE_METHOD(IREFLECT,             GET_PROPERTIES,         GetProperties,              IM_BindingFlags_RetArrPropertyInfo)
DEFINE_METHOD(IREFLECT,             GET_FIELDS,             GetFields,                  IM_BindingFlags_RetArrFieldInfo)
DEFINE_METHOD(IREFLECT,             GET_METHODS,            GetMethods,                 IM_BindingFlags_RetArrMethodInfo)
DEFINE_METHOD(IREFLECT,             INVOKE_MEMBER,          InvokeMember,               IM_Str_BindingFlags_Binder_Obj_ArrObj_ArrParameterModifier_CultureInfo_ArrStr_RetObj)


#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(LCID_CONVERSION_TYPE,  Interop,                LCIDConversionAttribute)
#endif // FEATURE_COMINTEROP


DEFINE_CLASS(MARSHAL,               Interop,                Marshal)
#ifdef FEATURE_COMINTEROP
DEFINE_METHOD(MARSHAL,              GET_HR_FOR_EXCEPTION,              GetHRForException,             SM_Exception_RetInt)
#endif // FEATURE_COMINTEROP
DEFINE_METHOD(MARSHAL,              GET_FUNCTION_POINTER_FOR_DELEGATE, GetFunctionPointerForDelegate, SM_Delegate_RetIntPtr)
DEFINE_METHOD(MARSHAL,              GET_DELEGATE_FOR_FUNCTION_POINTER, GetDelegateForFunctionPointer, SM_IntPtr_Type_RetDelegate)
DEFINE_METHOD(MARSHAL,              ALLOC_CO_TASK_MEM,                 AllocCoTaskMem,                SM_Int_RetIntPtr)
DEFINE_METHOD(MARSHAL,              FREE_CO_TASK_MEM,                  FreeCoTaskMem,                 SM_IntPtr_RetVoid)
DEFINE_FIELD(MARSHAL,               SYSTEM_MAX_DBCS_CHAR_SIZE,         SystemMaxDBCSCharSize)

DEFINE_CLASS(NATIVELIBRARY, Interop, NativeLibrary)
DEFINE_METHOD(NATIVELIBRARY,        LOADLIBRARYCALLBACKSTUB, LoadLibraryCallbackStub, SM_Str_AssemblyBase_Bool_UInt_RetIntPtr)

DEFINE_CLASS(MEMBER,                Reflection,             MemberInfo)


DEFINE_CLASS_U(Reflection,             RuntimeMethodInfo,  NoClass)
DEFINE_FIELD_U(m_handle,                   ReflectMethodObject, m_pMD)
DEFINE_CLASS(METHOD,                Reflection,             RuntimeMethodInfo)
DEFINE_METHOD(METHOD,               INVOKE,                 Invoke,                     IM_Obj_BindingFlags_Binder_ArrObj_CultureInfo_RetObj)
DEFINE_METHOD(METHOD,               GET_PARAMETERS,         GetParameters,              IM_RetArrParameterInfo)

DEFINE_CLASS(METHOD_BASE,           Reflection,             MethodBase)
DEFINE_METHOD(METHOD_BASE,          GET_METHODDESC,         GetMethodDesc,              IM_RetIntPtr)

DEFINE_CLASS_U(Reflection,             RuntimeExceptionHandlingClause,    RuntimeExceptionHandlingClause)
DEFINE_FIELD_U(_methodBody,            RuntimeExceptionHandlingClause,        _methodBody)
DEFINE_FIELD_U(_flags,                 RuntimeExceptionHandlingClause,        _flags)
DEFINE_FIELD_U(_tryOffset,             RuntimeExceptionHandlingClause,        _tryOffset)
DEFINE_FIELD_U(_tryLength,             RuntimeExceptionHandlingClause,        _tryLength)
DEFINE_FIELD_U(_handlerOffset,         RuntimeExceptionHandlingClause,        _handlerOffset)
DEFINE_FIELD_U(_handlerLength,         RuntimeExceptionHandlingClause,        _handlerLength)
DEFINE_FIELD_U(_catchMetadataToken,    RuntimeExceptionHandlingClause,        _catchToken)
DEFINE_FIELD_U(_filterOffset,          RuntimeExceptionHandlingClause,        _filterOffset)
DEFINE_CLASS(RUNTIME_EH_CLAUSE,             Reflection,             RuntimeExceptionHandlingClause)

DEFINE_CLASS_U(Reflection,             RuntimeLocalVariableInfo,        RuntimeLocalVariableInfo)
DEFINE_FIELD_U(_type,                  RuntimeLocalVariableInfo,        _type)
DEFINE_FIELD_U(_localIndex,            RuntimeLocalVariableInfo,        _localIndex)
DEFINE_FIELD_U(_isPinned,              RuntimeLocalVariableInfo,        _isPinned)
DEFINE_CLASS(RUNTIME_LOCAL_VARIABLE_INFO,   Reflection,             RuntimeLocalVariableInfo)

DEFINE_CLASS_U(Reflection,             RuntimeMethodBody,           RuntimeMethodBody)
DEFINE_FIELD_U(_IL,                    RuntimeMethodBody,         _IL)
DEFINE_FIELD_U(_exceptionHandlingClauses, RuntimeMethodBody,         _exceptionClauses)
DEFINE_FIELD_U(_localVariables,           RuntimeMethodBody,         _localVariables)
DEFINE_FIELD_U(_methodBase,               RuntimeMethodBody,         _methodBase)
DEFINE_FIELD_U(_localSignatureMetadataToken, RuntimeMethodBody,      _localVarSigToken)
DEFINE_FIELD_U(_maxStackSize,             RuntimeMethodBody,         _maxStackSize)
DEFINE_FIELD_U(_initLocals,               RuntimeMethodBody,         _initLocals)
DEFINE_CLASS(RUNTIME_METHOD_BODY,           Reflection,             RuntimeMethodBody)

DEFINE_CLASS(METHOD_INFO,           Reflection,             MethodInfo)

DEFINE_CLASS(METHOD_HANDLE_INTERNAL,System,                 RuntimeMethodHandleInternal)

DEFINE_CLASS(METHOD_HANDLE,         System,                 RuntimeMethodHandle)
DEFINE_FIELD(METHOD_HANDLE,         METHOD,                 m_value)
DEFINE_METHOD(METHOD_HANDLE,        GETVALUEINTERNAL,       GetValueInternal,           SM_RuntimeMethodHandle_RetIntPtr)

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
DEFINE_METHOD(MULTICAST_DELEGATE,   CTOR_VIRTUAL_DISPATCH,  CtorVirtualDispatch,        IM_Obj_IntPtr_IntPtr_RetVoid)
DEFINE_METHOD(MULTICAST_DELEGATE,   CTOR_COLLECTIBLE_CLOSED_STATIC,     CtorCollectibleClosedStatic,           IM_Obj_IntPtr_IntPtr_RetVoid)
DEFINE_METHOD(MULTICAST_DELEGATE,   CTOR_COLLECTIBLE_OPENED,            CtorCollectibleOpened,                 IM_Obj_IntPtr_IntPtr_IntPtr_RetVoid)
DEFINE_METHOD(MULTICAST_DELEGATE,   CTOR_COLLECTIBLE_VIRTUAL_DISPATCH,  CtorCollectibleVirtualDispatch,        IM_Obj_IntPtr_IntPtr_IntPtr_RetVoid)

DEFINE_CLASS(NULL,                  System,                 DBNull)
DEFINE_FIELD(NULL,                  VALUE,          Value)

DEFINE_CLASS(NULLABLE,              System,                 Nullable`1)

DEFINE_CLASS(BYREFERENCE,           System,                 ByReference`1)
DEFINE_CLASS(SPAN,                  System,                 Span`1)
DEFINE_METHOD(SPAN,                 GET_ITEM,               get_Item, IM_Int_RetRefT)
DEFINE_CLASS(READONLY_SPAN,         System,                 ReadOnlySpan`1)
DEFINE_METHOD(READONLY_SPAN,        GET_ITEM,               get_Item, IM_Int_RetReadOnlyRefT)

// Defined as element type alias
// DEFINE_CLASS(OBJECT,                System,                 Object)
DEFINE_METHOD(OBJECT,               CTOR,                   .ctor,                      IM_RetVoid)
DEFINE_METHOD(OBJECT,               FINALIZE,               Finalize,                   IM_RetVoid)
DEFINE_METHOD(OBJECT,               TO_STRING,              ToString,                   IM_RetStr)
DEFINE_METHOD(OBJECT,               GET_TYPE,               GetType,                    IM_RetType)
DEFINE_METHOD(OBJECT,               GET_HASH_CODE,          GetHashCode,                IM_RetInt)
DEFINE_METHOD(OBJECT,               EQUALS,                 Equals,                     IM_Obj_RetBool)

// DEFINE_CLASS(DOUBLE,                System,                 Double)
DEFINE_METHOD(DOUBLE,               GET_HASH_CODE,          GetHashCode, IM_RetInt)

// DEFINE_CLASS(SINGLE,                System,                 Single)
DEFINE_METHOD(SINGLE,               GET_HASH_CODE,          GetHashCode, IM_RetInt)

DEFINE_CLASS(__CANON,              System,                 __Canon)


#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(OLE_AUT_BINDER,        System,                 OleAutBinder)    
#endif // FEATURE_COMINTEROP

DEFINE_CLASS(MONITOR,               Threading,              Monitor)
DEFINE_METHOD(MONITOR,              ENTER,                  Enter,                      SM_Obj_RetVoid)

DEFINE_CLASS_U(Threading,              OverlappedData, OverlappedDataObject)
DEFINE_FIELD_U(_asyncResult,            OverlappedDataObject,       m_asyncResult)
DEFINE_FIELD_U(_callback,               OverlappedDataObject,       m_callback)
DEFINE_FIELD_U(_overlapped,             OverlappedDataObject,       m_overlapped)
DEFINE_FIELD_U(_userObject,             OverlappedDataObject,       m_userObject)
DEFINE_FIELD_U(_pNativeOverlapped,      OverlappedDataObject,       m_pNativeOverlapped)
DEFINE_FIELD_U(_offsetLow,              OverlappedDataObject,       m_offsetLow)
DEFINE_FIELD_U(_offsetHigh,             OverlappedDataObject,       m_offsetHigh)
DEFINE_FIELD_U(_eventHandle,            OverlappedDataObject,       m_eventHandle)
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
DEFINE_METHOD(RUNTIME_HELPERS,      IS_REFERENCE_OR_CONTAINS_REFERENCES, IsReferenceOrContainsReferences, NoSig)
DEFINE_METHOD(RUNTIME_HELPERS,      IS_BITWISE_EQUATABLE,    IsBitwiseEquatable, NoSig)

DEFINE_CLASS(JIT_HELPERS,           CompilerServices,       JitHelpers)
DEFINE_METHOD(JIT_HELPERS,          ENUM_EQUALS,            EnumEquals, NoSig)
DEFINE_METHOD(JIT_HELPERS,          ENUM_COMPARE_TO,        EnumCompareTo, NoSig)
DEFINE_METHOD(JIT_HELPERS,          GET_RAW_SZ_ARRAY_DATA,  GetRawSzArrayData, NoSig)

DEFINE_CLASS(UNSAFE,                InternalCompilerServices,       Unsafe)
DEFINE_METHOD(UNSAFE,               AS_POINTER,             AsPointer, NoSig)
DEFINE_METHOD(UNSAFE,               AS_REF_IN,              AsRef, GM_RefT_RetRefT)
DEFINE_METHOD(UNSAFE,               AS_REF_POINTER,         AsRef, GM_VoidPtr_RetRefT)
DEFINE_METHOD(UNSAFE,               SIZEOF,                 SizeOf, NoSig)
DEFINE_METHOD(UNSAFE,               BYREF_AS,               As, GM_RefTFrom_RetRefTTo)
DEFINE_METHOD(UNSAFE,               OBJECT_AS,              As, GM_Obj_RetT)
DEFINE_METHOD(UNSAFE,               BYREF_ADD,              Add, GM_RefT_Int_RetRefT)
DEFINE_METHOD(UNSAFE,               BYREF_INTPTR_ADD,       Add, GM_RefT_IntPtr_RetRefT)
DEFINE_METHOD(UNSAFE,               PTR_ADD,                Add, GM_PtrVoid_Int_RetPtrVoid)
DEFINE_METHOD(UNSAFE,               BYREF_BYTE_OFFSET,      ByteOffset, NoSig)
DEFINE_METHOD(UNSAFE,               BYREF_ADD_BYTE_OFFSET,  AddByteOffset, NoSig)
DEFINE_METHOD(UNSAFE,               BYREF_ARE_SAME,         AreSame, NoSig)
DEFINE_METHOD(UNSAFE,               BYREF_IS_ADDRESS_GREATER_THAN, IsAddressGreaterThan, NoSig)
DEFINE_METHOD(UNSAFE,               BYREF_IS_ADDRESS_LESS_THAN, IsAddressLessThan, NoSig)
DEFINE_METHOD(UNSAFE,               BYREF_INIT_BLOCK_UNALIGNED, InitBlockUnaligned, NoSig)
DEFINE_METHOD(UNSAFE,               BYREF_READ_UNALIGNED,   ReadUnaligned, GM_RefByte_RetT)
DEFINE_METHOD(UNSAFE,               BYREF_WRITE_UNALIGNED,  WriteUnaligned, GM_RefByte_T_RetVoid)
DEFINE_METHOD(UNSAFE,               PTR_READ_UNALIGNED,     ReadUnaligned, GM_PtrVoid_RetT)
DEFINE_METHOD(UNSAFE,               PTR_WRITE_UNALIGNED,    WriteUnaligned, GM_PtrVoid_T_RetVoid)

DEFINE_CLASS(INTERLOCKED,           Threading,              Interlocked)
DEFINE_METHOD(INTERLOCKED,          COMPARE_EXCHANGE_T,     CompareExchange, GM_RefT_T_T_RetT)
DEFINE_METHOD(INTERLOCKED,          COMPARE_EXCHANGE_OBJECT,CompareExchange, SM_RefObject_Object_Object_RetObject)

DEFINE_CLASS(RAW_DATA,              CompilerServices,       RawData)
DEFINE_FIELD(RAW_DATA,              DATA,                   Data)

DEFINE_CLASS(RAW_SZARRAY_DATA,      CompilerServices,       RawSzArrayData)
DEFINE_FIELD(RAW_SZARRAY_DATA,      COUNT,                  Count)
DEFINE_FIELD(RAW_SZARRAY_DATA,      DATA,                   Data)

DEFINE_CLASS(RUNTIME_WRAPPED_EXCEPTION, CompilerServices,   RuntimeWrappedException)
DEFINE_METHOD(RUNTIME_WRAPPED_EXCEPTION, OBJ_CTOR,          .ctor,                      IM_Obj_RetVoid)
DEFINE_FIELD(RUNTIME_WRAPPED_EXCEPTION, WRAPPED_EXCEPTION,  _wrappedException)

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


DEFINE_CLASS(SAFE_TYPENAMEPARSER_HANDLE,    System,         SafeTypeNameParserHandle)

DEFINE_CLASS(SECURITY_EXCEPTION,    Security,               SecurityException)

DEFINE_CLASS_U(Diagnostics,                StackFrameHelper,   StackFrameHelper)
DEFINE_FIELD_U(targetThread,               StackFrameHelper,   targetThread)
DEFINE_FIELD_U(rgiOffset,                  StackFrameHelper,   rgiOffset)
DEFINE_FIELD_U(rgiILOffset,                StackFrameHelper,   rgiILOffset)
DEFINE_FIELD_U(dynamicMethods,             StackFrameHelper,   dynamicMethods)
DEFINE_FIELD_U(rgMethodHandle,             StackFrameHelper,   rgMethodHandle)
DEFINE_FIELD_U(rgAssemblyPath,             StackFrameHelper,   rgAssemblyPath)
DEFINE_FIELD_U(rgAssembly,                 StackFrameHelper,   rgAssembly)
DEFINE_FIELD_U(rgLoadedPeAddress,          StackFrameHelper,   rgLoadedPeAddress)
DEFINE_FIELD_U(rgiLoadedPeSize,            StackFrameHelper,   rgiLoadedPeSize)
DEFINE_FIELD_U(rgInMemoryPdbAddress,       StackFrameHelper,   rgInMemoryPdbAddress)
DEFINE_FIELD_U(rgiInMemoryPdbSize,         StackFrameHelper,   rgiInMemoryPdbSize)
DEFINE_FIELD_U(rgiMethodToken,             StackFrameHelper,   rgiMethodToken)
DEFINE_FIELD_U(rgFilename,                 StackFrameHelper,   rgFilename)
DEFINE_FIELD_U(rgiLineNumber,              StackFrameHelper,   rgiLineNumber)
DEFINE_FIELD_U(rgiColumnNumber,            StackFrameHelper,   rgiColumnNumber)
DEFINE_FIELD_U(rgiLastFrameFromForeignExceptionStackTrace,            StackFrameHelper,   rgiLastFrameFromForeignExceptionStackTrace)
DEFINE_FIELD_U(iFrameCount,                StackFrameHelper,   iFrameCount)

DEFINE_CLASS(STARTUP_HOOK_PROVIDER,  System,                StartupHookProvider)
DEFINE_METHOD(STARTUP_HOOK_PROVIDER, PROCESS_STARTUP_HOOKS, ProcessStartupHooks, SM_RetVoid)

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

DEFINE_CLASS(BITCONVERTER,          System,                 BitConverter)
DEFINE_FIELD(BITCONVERTER,          ISLITTLEENDIAN,         IsLittleEndian)

// Defined as element type alias
// DEFINE_CLASS(STRING,                System,                 String)
DEFINE_FIELD(STRING,                M_FIRST_CHAR,           _firstChar)
DEFINE_FIELD(STRING,                EMPTY,                  Empty)
DEFINE_METHOD(STRING,               CTOR_CHARPTR,           .ctor,                      IM_PtrChar_RetVoid)
DEFINE_METHOD(STRING,               CTORF_CHARARRAY,        Ctor,                       IM_ArrChar_RetStr)
DEFINE_METHOD(STRING,               CTORF_CHARARRAY_START_LEN,Ctor,                     IM_ArrChar_Int_Int_RetStr)
DEFINE_METHOD(STRING,               CTORF_CHAR_COUNT,       Ctor,                       IM_Char_Int_RetStr)
DEFINE_METHOD(STRING,               CTORF_CHARPTR,          Ctor,                       IM_PtrChar_RetStr)
DEFINE_METHOD(STRING,               CTORF_CHARPTR_START_LEN,Ctor,                       IM_PtrChar_Int_Int_RetStr)
DEFINE_METHOD(STRING,               CTORF_READONLYSPANOFCHAR,Ctor,                      IM_ReadOnlySpanOfChar_RetStr)
DEFINE_METHOD(STRING,               CTORF_SBYTEPTR,         Ctor,                       IM_PtrSByt_RetStr)
DEFINE_METHOD(STRING,               CTORF_SBYTEPTR_START_LEN, Ctor,                     IM_PtrSByt_Int_Int_RetStr)
DEFINE_METHOD(STRING,               CTORF_SBYTEPTR_START_LEN_ENCODING, Ctor,            IM_PtrSByt_Int_Int_Encoding_RetStr)
DEFINE_METHOD(STRING,               INTERNAL_COPY,          InternalCopy,               SM_Str_IntPtr_Int_RetVoid)
DEFINE_METHOD(STRING,               WCSLEN,                 wcslen,                     SM_PtrChar_RetInt)
DEFINE_METHOD(STRING,               STRLEN,                 strlen,                     SM_PtrByte_RetInt)
DEFINE_PROPERTY(STRING,             LENGTH,                 Length,                     Int)

#ifdef FEATURE_UTF8STRING
DEFINE_CLASS(UTF8_STRING,           System,                 Utf8String)
DEFINE_METHOD(UTF8_STRING,          CTORF_READONLYSPANOFBYTE,Ctor,                      IM_ReadOnlySpanOfByte_RetUtf8Str)
DEFINE_METHOD(UTF8_STRING,          CTORF_READONLYSPANOFCHAR,Ctor,                      IM_ReadOnlySpanOfChar_RetUtf8Str)
DEFINE_METHOD(UTF8_STRING,          CTORF_BYTEARRAY_START_LEN,Ctor,                     IM_ArrByte_Int_Int_RetUtf8Str)
DEFINE_METHOD(UTF8_STRING,          CTORF_BYTEPTR,           Ctor,                      IM_PtrByte_RetUtf8Str)
DEFINE_METHOD(UTF8_STRING,          CTORF_CHARARRAY_START_LEN,Ctor,                     IM_ArrChar_Int_Int_RetUtf8Str)
DEFINE_METHOD(UTF8_STRING,          CTORF_CHARPTR,           Ctor,                      IM_PtrChar_RetUtf8Str)
DEFINE_METHOD(UTF8_STRING,          CTORF_STRING,            Ctor,                      IM_String_RetUtf8Str)
#endif // FEATURE_UTF8STRING

DEFINE_CLASS(STRING_BUILDER,        Text,                   StringBuilder)
DEFINE_PROPERTY(STRING_BUILDER,     LENGTH,                 Length,                     Int)
DEFINE_PROPERTY(STRING_BUILDER,     CAPACITY,               Capacity,                   Int)
DEFINE_METHOD(STRING_BUILDER,       CTOR_INT,               .ctor,                      IM_Int_RetVoid)
DEFINE_METHOD(STRING_BUILDER,       TO_STRING,              ToString,                   IM_RetStr)
DEFINE_METHOD(STRING_BUILDER,       INTERNAL_COPY,          InternalCopy,               IM_IntPtr_Int_RetVoid)
DEFINE_METHOD(STRING_BUILDER,       REPLACE_BUFFER_INTERNAL,ReplaceBufferInternal,      IM_PtrChar_Int_RetVoid)
DEFINE_METHOD(STRING_BUILDER,       REPLACE_BUFFER_ANSI_INTERNAL,ReplaceBufferAnsiInternal, IM_PtrSByt_Int_RetVoid)

DEFINE_CLASS(STRONG_NAME_KEY_PAIR,  Reflection,             StrongNameKeyPair)

DEFINE_CLASS_U(Threading,              SynchronizationContext, SynchronizationContextObject)
DEFINE_FIELD_U(_requireWaitNotification, SynchronizationContextObject, _requireWaitNotification)
DEFINE_CLASS(SYNCHRONIZATION_CONTEXT,    Threading,              SynchronizationContext)
DEFINE_METHOD(SYNCHRONIZATION_CONTEXT,  INVOKE_WAIT_METHOD_HELPER, InvokeWaitMethodHelper, SM_SyncCtx_ArrIntPtr_Bool_Int_RetInt)

DEFINE_CLASS(CONTEXTCALLBACK,       Threading,       ContextCallback)

#ifdef _DEBUG
DEFINE_CLASS(STACKCRAWMARK,         Threading,       StackCrawlMark)
#endif

DEFINE_CLASS_U(Threading,              Thread,                     ThreadBaseObject)
DEFINE_FIELD_U(_name,                     ThreadBaseObject,   m_Name)
DEFINE_FIELD_U(_delegate,                 ThreadBaseObject,   m_Delegate)
DEFINE_FIELD_U(_threadStartArg,           ThreadBaseObject,   m_ThreadStartArg)
DEFINE_FIELD_U(_DONT_USE_InternalThread,  ThreadBaseObject,   m_InternalThread)
DEFINE_FIELD_U(_priority,                 ThreadBaseObject,   m_Priority)
DEFINE_CLASS(THREAD,                Threading,              Thread)
DEFINE_METHOD(THREAD,               INTERNAL_GET_CURRENT_THREAD,             InternalGetCurrentThread,                    SM_RetIntPtr)

DEFINE_CLASS(PARAMETERIZEDTHREADSTART,     Threading,                 ParameterizedThreadStart)

DEFINE_CLASS(IOCB_HELPER,              Threading,            _IOCompletionCallback)
DEFINE_METHOD(IOCB_HELPER,             PERFORM_IOCOMPLETION_CALLBACK,        PerformIOCompletionCallback,          SM_UInt_UInt_PtrNativeOverlapped_RetVoid)

DEFINE_CLASS(TPWAITORTIMER_HELPER,              Threading,            _ThreadPoolWaitOrTimerCallback)
DEFINE_METHOD(TPWAITORTIMER_HELPER,             PERFORM_WAITORTIMER_CALLBACK,        PerformWaitOrTimerCallback,          SM__ThreadPoolWaitOrTimerCallback_Bool_RetVoid)

DEFINE_CLASS(TP_WAIT_CALLBACK,         Threading,              _ThreadPoolWaitCallback)
DEFINE_METHOD(TP_WAIT_CALLBACK,        PERFORM_WAIT_CALLBACK,               PerformWaitCallback,                   SM_RetBool)

DEFINE_CLASS(TIMER_QUEUE,           Threading,                TimerQueue)
DEFINE_METHOD(TIMER_QUEUE,          APPDOMAIN_TIMER_CALLBACK, AppDomainTimerCallback,   SM_Int_RetVoid)

DEFINE_CLASS(TIMESPAN,              System,                 TimeSpan)


DEFINE_CLASS(TYPE,                  System,                 Type)
DEFINE_METHOD(TYPE,                 GET_TYPE_FROM_HANDLE,   GetTypeFromHandle,          SM_RuntimeTypeHandle_RetType)
DEFINE_PROPERTY(TYPE,               IS_IMPORT,              IsImport,                   Bool)

DEFINE_CLASS(TYPE_DELEGATOR,        Reflection,             TypeDelegator)

DEFINE_CLASS(UNHANDLED_EVENTARGS,   System,                 UnhandledExceptionEventArgs)
DEFINE_METHOD(UNHANDLED_EVENTARGS,  CTOR,                   .ctor,                      IM_Obj_Bool_RetVoid)

DEFINE_CLASS(FIRSTCHANCE_EVENTARGS,   ExceptionServices,      FirstChanceExceptionEventArgs)
DEFINE_METHOD(FIRSTCHANCE_EVENTARGS,  CTOR,                   .ctor,                      IM_Exception_RetVoid)

DEFINE_CLASS_U(Loader,             AssemblyLoadContext,           AssemblyLoadContextBaseObject)
DEFINE_FIELD_U(_unloadLock,                 AssemblyLoadContextBaseObject, _unloadLock)
DEFINE_FIELD_U(_resolvingUnmanagedDll,      AssemblyLoadContextBaseObject, _resovlingUnmanagedDll)
DEFINE_FIELD_U(_resolving,                  AssemblyLoadContextBaseObject, _resolving)
DEFINE_FIELD_U(_unloading,                  AssemblyLoadContextBaseObject, _unloading)
DEFINE_FIELD_U(_name,                       AssemblyLoadContextBaseObject, _name)
DEFINE_FIELD_U(_nativeAssemblyLoadContext,  AssemblyLoadContextBaseObject, _nativeAssemblyLoadContext)
DEFINE_FIELD_U(_id,                         AssemblyLoadContextBaseObject, _id)
DEFINE_FIELD_U(_state,                      AssemblyLoadContextBaseObject, _state)
DEFINE_FIELD_U(_isCollectible,              AssemblyLoadContextBaseObject, _isCollectible)
DEFINE_CLASS(ASSEMBLYLOADCONTEXT,  Loader,                AssemblyLoadContext)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  RESOLVE,          Resolve,                      SM_IntPtr_AssemblyName_RetAssemblyBase)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  RESOLVEUNMANAGEDDLL,           ResolveUnmanagedDll,           SM_Str_IntPtr_RetIntPtr)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  RESOLVEUNMANAGEDDLLUSINGEVENT, ResolveUnmanagedDllUsingEvent, SM_Str_AssemblyBase_IntPtr_RetIntPtr)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  RESOLVEUSINGEVENT,             ResolveUsingResolvingEvent,    SM_IntPtr_AssemblyName_RetAssemblyBase)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  RESOLVESATELLITEASSEMBLY,      ResolveSatelliteAssembly,      SM_IntPtr_AssemblyName_RetAssemblyBase)
DEFINE_FIELD(ASSEMBLYLOADCONTEXT,   ASSEMBLY_LOAD,          AssemblyLoad)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  ON_ASSEMBLY_LOAD,       OnAssemblyLoad, SM_Assembly_RetVoid)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  ON_RESOURCE_RESOLVE,    OnResourceResolve, SM_Assembly_Str_RetAssembly)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  ON_TYPE_RESOLVE,        OnTypeResolve, SM_Assembly_Str_RetAssembly)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  ON_ASSEMBLY_RESOLVE,    OnAssemblyResolve, SM_Assembly_Str_RetAssembly)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(WINDOWSRUNTIMEMETATADA, WinRT, WindowsRuntimeMetadata) 
DEFINE_METHOD(WINDOWSRUNTIMEMETATADA,  ON_DESIGNER_NAMESPACE_RESOLVE, OnDesignerNamespaceResolve, SM_Str_RetArrStr)
#endif //FEATURE_COMINTEROP

DEFINE_CLASS(LAZY,              System,     Lazy`1)

DEFINE_CLASS(LAZY_INITIALIZER,  Threading,  LazyInitializer)

DEFINE_CLASS(VALUE_TYPE,            System,                 ValueType)
DEFINE_METHOD(VALUE_TYPE,           GET_HASH_CODE,          GetHashCode,            IM_RetInt)
DEFINE_METHOD(VALUE_TYPE,           EQUALS,                 Equals,                 IM_Obj_RetBool)

DEFINE_CLASS(GC,                    System,                 GC)
DEFINE_METHOD(GC,                   KEEP_ALIVE,             KeepAlive,                  SM_Obj_RetVoid)
DEFINE_METHOD(GC,                   COLLECT,                Collect,                    SM_RetVoid)
DEFINE_METHOD(GC,                   WAIT_FOR_PENDING_FINALIZERS, WaitForPendingFinalizers, SM_RetVoid)

DEFINE_CLASS_U(System,                 WeakReference,          WeakReferenceObject)
DEFINE_FIELD_U(m_handle,               WeakReferenceObject,    m_Handle)
DEFINE_CLASS(WEAKREFERENCE,         System,                 WeakReference)

DEFINE_CLASS_U(Threading,              WaitHandle,             WaitHandleBase)
DEFINE_FIELD_U(_waitHandle,         WaitHandleBase,         m_safeHandle)

DEFINE_CLASS(DEBUGGER,              Diagnostics,            Debugger)
DEFINE_METHOD(DEBUGGER,             BREAK_CAN_THROW,        BreakCanThrow,          SM_RetVoid)

DEFINE_CLASS(BUFFER,                System,                 Buffer)
DEFINE_METHOD(BUFFER,               MEMCPY_PTRBYTE_ARRBYTE, Memcpy,                 SM_PtrByte_Int_ArrByte_Int_Int_RetVoid)
DEFINE_METHOD(BUFFER,               MEMCPY,                 Memcpy,                 SM_PtrByte_PtrByte_Int_RetVoid)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(WINDOWSRUNTIMEMARSHAL, WinRT,  WindowsRuntimeMarshal)
DEFINE_METHOD(WINDOWSRUNTIMEMARSHAL, GET_HR_FOR_EXCEPTION, GetHRForException, SM_Exception_RetInt)
DEFINE_METHOD(WINDOWSRUNTIMEMARSHAL, INITIALIZE_WRAPPER, InitializeWrapper, SM_Obj_RefIntPtr_RetVoid)
#ifdef FEATURE_COMINTEROP_WINRT_MANAGED_ACTIVATION
DEFINE_METHOD(WINDOWSRUNTIMEMARSHAL, GET_ACTIVATION_FACTORY_FOR_TYPE, GetActivationFactoryForType, SM_Type_RetIntPtr)
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
#ifdef FEATURE_COMINTEROP
DEFINE_METHOD(STUBHELPERS,          GET_COM_HR_EXCEPTION_OBJECT,              GetCOMHRExceptionObject,            SM_Int_IntPtr_Obj_RetException)
DEFINE_METHOD(STUBHELPERS,          GET_COM_HR_EXCEPTION_OBJECT_WINRT,        GetCOMHRExceptionObject_WinRT,      SM_Int_IntPtr_Obj_RetException)
DEFINE_METHOD(STUBHELPERS,          GET_COM_IP_FROM_RCW,                      GetCOMIPFromRCW,                    SM_Obj_IntPtr_RefIntPtr_RefBool_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          GET_COM_IP_FROM_RCW_WINRT,                GetCOMIPFromRCW_WinRT,              SM_Obj_IntPtr_RefIntPtr_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          GET_COM_IP_FROM_RCW_WINRT_SHARED_GENERIC, GetCOMIPFromRCW_WinRTSharedGeneric, SM_Obj_IntPtr_RefIntPtr_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          GET_COM_IP_FROM_RCW_WINRT_DELEGATE,       GetCOMIPFromRCW_WinRTDelegate,      SM_Obj_IntPtr_RefIntPtr_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          SHOULD_CALL_WINRT_INTERFACE,              ShouldCallWinRTInterface,           SM_Obj_IntPtr_RetBool)
DEFINE_METHOD(STUBHELPERS,          GET_WINRT_FACTORY_OBJECT,                 GetWinRTFactoryObject,              SM_IntPtr_RetObj)
DEFINE_METHOD(STUBHELPERS,          GET_DELEGATE_INVOKE_METHOD,               GetDelegateInvokeMethod,            SM_Delegate_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          GET_WINRT_FACTORY_RETURN_VALUE,           GetWinRTFactoryReturnValue,         SM_Obj_IntPtr_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          GET_OUTER_INSPECTABLE,                    GetOuterInspectable,                SM_Obj_IntPtr_RetIntPtr)
#endif // FEATURE_COMINTEROP
DEFINE_METHOD(STUBHELPERS,          SET_LAST_ERROR,         SetLastError,               SM_RetVoid)
DEFINE_METHOD(STUBHELPERS,          CLEAR_LAST_ERROR,       ClearLastError,             SM_RetVoid)

DEFINE_METHOD(STUBHELPERS,          THROW_INTEROP_PARAM_EXCEPTION, ThrowInteropParamException,   SM_Int_Int_RetVoid)
DEFINE_METHOD(STUBHELPERS,          ADD_TO_CLEANUP_LIST_SAFEHANDLE,    AddToCleanupList,           SM_RefCleanupWorkListElement_SafeHandle_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          ADD_TO_CLEANUP_LIST_DELEGATE,    AddToCleanupList,             SM_RefCleanupWorkListElement_Delegate_RetVoid)
DEFINE_METHOD(STUBHELPERS,          DESTROY_CLEANUP_LIST,   DestroyCleanupList,         SM_RefCleanupWorkListElement_RetVoid)
DEFINE_METHOD(STUBHELPERS,          GET_HR_EXCEPTION_OBJECT, GetHRExceptionObject,      SM_Int_RetException)
DEFINE_METHOD(STUBHELPERS,          CREATE_CUSTOM_MARSHALER_HELPER, CreateCustomMarshalerHelper, SM_IntPtr_Int_IntPtr_RetIntPtr)

DEFINE_METHOD(STUBHELPERS,          CHECK_STRING_LENGTH,    CheckStringLength,          SM_Int_RetVoid)

DEFINE_METHOD(STUBHELPERS,          FMT_CLASS_UPDATE_NATIVE_INTERNAL,   FmtClassUpdateNativeInternal,   SM_Obj_PtrByte_RefCleanupWorkListElement_RetVoid)
DEFINE_METHOD(STUBHELPERS,          FMT_CLASS_UPDATE_CLR_INTERNAL,      FmtClassUpdateCLRInternal,      SM_Obj_PtrByte_RetVoid)
DEFINE_METHOD(STUBHELPERS,          LAYOUT_DESTROY_NATIVE_INTERNAL,     LayoutDestroyNativeInternal,    SM_PtrByte_IntPtr_RetVoid)
DEFINE_METHOD(STUBHELPERS,          ALLOCATE_INTERNAL,                  AllocateInternal,               SM_IntPtr_RetObj)
DEFINE_METHOD(STUBHELPERS,          MARSHAL_TO_MANAGED_VA_LIST_INTERNAL,MarshalToManagedVaListInternal, SM_IntPtr_IntPtr_RetVoid)
DEFINE_METHOD(STUBHELPERS,          MARSHAL_TO_UNMANAGED_VA_LIST_INTERNAL,MarshalToUnmanagedVaListInternal,SM_IntPtr_UInt_IntPtr_RetVoid)
DEFINE_METHOD(STUBHELPERS,          CALC_VA_LIST_SIZE,                  CalcVaListSize,                 SM_IntPtr_RetUInt)
DEFINE_METHOD(STUBHELPERS,          VALIDATE_OBJECT,                    ValidateObject,                 SM_Obj_IntPtr_Obj_RetVoid)
DEFINE_METHOD(STUBHELPERS,          VALIDATE_BYREF,                     ValidateByref,                  SM_IntPtr_IntPtr_Obj_RetVoid)
DEFINE_METHOD(STUBHELPERS,          GET_STUB_CONTEXT,                   GetStubContext,                 SM_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          LOG_PINNED_ARGUMENT,                LogPinnedArgument,              SM_IntPtr_IntPtr_RetVoid)
#ifdef _TARGET_64BIT_
DEFINE_METHOD(STUBHELPERS,          GET_STUB_CONTEXT_ADDR,              GetStubContextAddr,             SM_RetIntPtr)
#endif // _TARGET_64BIT_
DEFINE_METHOD(STUBHELPERS,          SAFE_HANDLE_ADD_REF,    SafeHandleAddRef,           SM_SafeHandle_RefBool_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          SAFE_HANDLE_RELEASE,    SafeHandleRelease,          SM_SafeHandle_RetVoid)

#ifdef PROFILING_SUPPORTED
DEFINE_METHOD(STUBHELPERS,          PROFILER_BEGIN_TRANSITION_CALLBACK, ProfilerBeginTransitionCallback, SM_IntPtr_IntPtr_Obj_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          PROFILER_END_TRANSITION_CALLBACK,   ProfilerEndTransitionCallback,   SM_IntPtr_IntPtr_RetVoid)
#endif

#ifdef FEATURE_ARRAYSTUB_AS_IL
DEFINE_METHOD(STUBHELPERS,          ARRAY_TYPE_CHECK,    ArrayTypeCheck,          SM_Obj_ArrObject_RetVoid)
#endif

#ifdef FEATURE_MULTICASTSTUB_AS_IL
DEFINE_METHOD(STUBHELPERS,          MULTICAST_DEBUGGER_TRACE_HELPER,    MulticastDebuggerTraceHelper,    SM_Obj_Int_RetVoid)
#endif

DEFINE_CLASS(CLEANUP_WORK_LIST_ELEMENT,     StubHelpers,            CleanupWorkListElement)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(DATETIMENATIVE,   StubHelpers,        DateTimeNative)
DEFINE_CLASS(TYPENAMENATIVE,   StubHelpers,        TypeNameNative)

DEFINE_CLASS_U(StubHelpers,     TypeNameNative,             TypeNameNative)
DEFINE_FIELD_U(typeName,        TypeNameNative,             typeName)
DEFINE_FIELD_U(typeKind,        TypeNameNative,             typeKind)
#endif

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

DEFINE_CLASS(BSTRMARSHALER,         StubHelpers,            BSTRMarshaler)
DEFINE_METHOD(BSTRMARSHALER,        CONVERT_TO_NATIVE,      ConvertToNative,            SM_Str_IntPtr_RetIntPtr)
DEFINE_METHOD(BSTRMARSHALER,        CONVERT_TO_MANAGED,     ConvertToManaged,           SM_IntPtr_RetStr)
DEFINE_METHOD(BSTRMARSHALER,        CLEAR_NATIVE,           ClearNative,                SM_IntPtr_RetVoid)

DEFINE_CLASS(ANSIBSTRMARSHALER,     StubHelpers,            AnsiBSTRMarshaler)
DEFINE_METHOD(ANSIBSTRMARSHALER,    CONVERT_TO_NATIVE,      ConvertToNative,            SM_Int_Str_RetIntPtr)
DEFINE_METHOD(ANSIBSTRMARSHALER,    CONVERT_TO_MANAGED,     ConvertToManaged,           SM_IntPtr_RetStr)
DEFINE_METHOD(ANSIBSTRMARSHALER,    CLEAR_NATIVE,           ClearNative,                SM_IntPtr_RetVoid)

#ifdef FEATURE_COMINTEROP
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
DEFINE_METHOD(VALUECLASSMARSHALER,  CONVERT_TO_NATIVE,      ConvertToNative,            SM_IntPtrIntPtrIntPtr_RefCleanupWorkListElement_RetVoid)
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
DEFINE_METHOD(ICOMPARABLEGENERIC,   COMPARE_TO,             CompareTo,                  NoSig)

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
DEFINE_FIELD_U(_kind,               ContractExceptionObject,    _Kind)
DEFINE_FIELD_U(_userMessage,        ContractExceptionObject,    _UserMessage)
DEFINE_FIELD_U(_condition,          ContractExceptionObject,    _Condition)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(CAUSALITY_TRACE_LEVEL, WindowsFoundationDiag,   CausalityTraceLevel)
DEFINE_CLASS(ASYNC_TRACING_EVENT_ARGS,       WindowsFoundationDiag,         TracingStatusChangedEventArgs)
DEFINE_PROPERTY(ASYNC_TRACING_EVENT_ARGS, ENABLED, Enabled, Bool)
DEFINE_PROPERTY(ASYNC_TRACING_EVENT_ARGS, TRACELEVEL, TraceLevel, CausalityTraceLevel)
DEFINE_CLASS(IASYNC_TRACING_EVENT_ARGS,      WindowsFoundationDiag,         ITracingStatusChangedEventArgs)
DEFINE_PROPERTY(IASYNC_TRACING_EVENT_ARGS, ENABLED, Enabled, Bool)
DEFINE_PROPERTY(IASYNC_TRACING_EVENT_ARGS, TRACELEVEL, TraceLevel, CausalityTraceLevel)
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

// Classes referenced in EqualityComparer<T>.Default optimization

DEFINE_CLASS(BYTE_EQUALITYCOMPARER, CollectionsGeneric, ByteEqualityComparer)
DEFINE_CLASS(ENUM_EQUALITYCOMPARER, CollectionsGeneric, EnumEqualityComparer`1)
DEFINE_CLASS(NULLABLE_EQUALITYCOMPARER, CollectionsGeneric, NullableEqualityComparer`1)
DEFINE_CLASS(GENERIC_EQUALITYCOMPARER, CollectionsGeneric, GenericEqualityComparer`1)
DEFINE_CLASS(OBJECT_EQUALITYCOMPARER, CollectionsGeneric, ObjectEqualityComparer`1)

DEFINE_CLASS(INATTRIBUTE, Interop, InAttribute)

DEFINE_CLASS_U(CompilerServices,           GCHeapHash,                      GCHeapHashObject)
DEFINE_FIELD_U(_data,                      GCHeapHashObject,                _data)
DEFINE_FIELD_U(_count,                     GCHeapHashObject,                _count)
DEFINE_FIELD_U(_deletedCount,              GCHeapHashObject,                _deletedCount)

DEFINE_CLASS(GCHEAPHASH, CompilerServices, GCHeapHash)

DEFINE_CLASS_U(CompilerServices,           LAHashDependentHashTracker,      LAHashDependentHashTrackerObject)
DEFINE_FIELD_U(_dependentHandle,           LAHashDependentHashTrackerObject,_dependentHandle)
DEFINE_FIELD_U(_loaderAllocator,           LAHashDependentHashTrackerObject,_loaderAllocator)

DEFINE_CLASS(LAHASHDEPENDENTHASHTRACKER, CompilerServices, LAHashDependentHashTracker)

DEFINE_CLASS_U(CompilerServices,           LAHashKeyToTrackers,             LAHashKeyToTrackersObject)
DEFINE_FIELD_U(_trackerOrTrackerSet,       LAHashKeyToTrackersObject,       _trackerOrTrackerSet)
DEFINE_FIELD_U(_laLocalKeyValueStore,      LAHashKeyToTrackersObject,       _laLocalKeyValueStore)

DEFINE_CLASS(LAHASHKEYTOTRACKERS, CompilerServices, LAHashKeyToTrackers)

#undef DEFINE_CLASS
#undef DEFINE_METHOD
#undef DEFINE_FIELD
#undef DEFINE_CLASS_U
#undef DEFINE_FIELD_U
