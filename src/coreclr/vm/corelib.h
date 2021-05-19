// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// This file contains the classes, methods, and field used by the EE from corelib

//
// To use this, define one of the following macros & include the file like so:
//
// #define DEFINE_CLASS(id, nameSpace, stringName)         CLASS__ ## id,
// #define DEFINE_METHOD(classId, id, stringName, gSign)
// #define DEFINE_FIELD(classId, id, stringName)
// #include "corelib.h"
//
// Note: To determine if the namespace you want to use in DEFINE_CLASS is supported or not,
//       examine vm\namespace.h. If it is not present, define it there and then proceed to use it below.
//

//
// Note: This file gets parsed by the Mono IL Linker (https://github.com/mono/linker/) which may throw an exception during parsing.
// Specifically, this (https://github.com/mono/linker/blob/main/corebuild/integration/ILLink.Tasks/CreateRuntimeRootDescriptorFile.cs) will try to
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
DEFINE_METHOD(ACTIVATOR,            CREATE_INSTANCE_OF_T,   CreateInstance, GM_RetT)
DEFINE_METHOD(ACTIVATOR,            CREATE_DEFAULT_INSTANCE_OF_T,   CreateDefaultInstance,  GM_RetT)

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

DEFINE_CLASS(ARRAY_WITH_OFFSET,     Interop,                ArrayWithOffset)
DEFINE_FIELD(ARRAY_WITH_OFFSET,     M_ARRAY,                m_array)
DEFINE_FIELD(ARRAY_WITH_OFFSET,     M_OFFSET,               m_offset)
DEFINE_FIELD(ARRAY_WITH_OFFSET,     M_COUNT,                m_count)


DEFINE_CLASS(ASSEMBLY_BUILDER,      ReflectionEmit,         AssemblyBuilder)
DEFINE_CLASS(INTERNAL_ASSEMBLY_BUILDER,      ReflectionEmit,         InternalAssemblyBuilder)
#if FOR_ILLINK
DEFINE_METHOD(INTERNAL_ASSEMBLY_BUILDER,     CTOR,          .ctor,                      IM_RetVoid)
#endif

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
DEFINE_METHOD(ASSEMBLY_NAME,        CTOR,                   .ctor,                     IM_Str_ArrB_ArrB_Ver_CI_AHA_AVC_Str_ANF_RetV)
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
#ifdef FOR_ILLINK
DEFINE_METHOD(COM_OBJECT,           CTOR,                   .ctor,                      IM_RetVoid)
#endif

DEFINE_CLASS(LICENSE_INTEROP_PROXY,  InternalInteropServices, LicenseInteropProxy)
DEFINE_METHOD(LICENSE_INTEROP_PROXY, CREATE,                  Create,                  SM_RetObj)
DEFINE_METHOD(LICENSE_INTEROP_PROXY, GETCURRENTCONTEXTINFO,   GetCurrentContextInfo,   IM_RuntimeTypeHandle_RefBool_RefIntPtr_RetVoid)
DEFINE_METHOD(LICENSE_INTEROP_PROXY, SAVEKEYINCURRENTCONTEXT, SaveKeyInCurrentContext, IM_IntPtr_RetVoid)

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
DEFINE_METHOD(CULTURE_INFO,         INT_CTOR,               .ctor,                      IM_Int_RetVoid)
DEFINE_PROPERTY(CULTURE_INFO,       ID,                     LCID,                       Int)
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
DEFINE_FIELD_U(_stackTrace,        ExceptionObject,    _stackTrace)
DEFINE_FIELD_U(_watsonBuckets,     ExceptionObject,    _watsonBuckets)
DEFINE_FIELD_U(_stackTraceString,  ExceptionObject,    _stackTraceString)
DEFINE_FIELD_U(_remoteStackTraceString, ExceptionObject, _remoteStackTraceString)
DEFINE_FIELD_U(_dynamicMethods,    ExceptionObject,    _dynamicMethods)
DEFINE_FIELD_U(_source,            ExceptionObject,    _source)
DEFINE_FIELD_U(_ipForWatsonBuckets,ExceptionObject,    _ipForWatsonBuckets)
DEFINE_FIELD_U(_xptrs,             ExceptionObject,    _xptrs)
DEFINE_FIELD_U(_xcode,             ExceptionObject,    _xcode)
DEFINE_FIELD_U(_HResult,           ExceptionObject,    _HResult)
DEFINE_CLASS(EXCEPTION,             System,                 Exception)
DEFINE_METHOD(EXCEPTION,            GET_CLASS_NAME,         GetClassName,               IM_RetStr)
DEFINE_PROPERTY(EXCEPTION,          MESSAGE,                Message,                    Str)
DEFINE_PROPERTY(EXCEPTION,          SOURCE,                 Source,                     Str)
DEFINE_PROPERTY(EXCEPTION,          HELP_LINK,              HelpLink,                   Str)
DEFINE_METHOD(EXCEPTION,            INTERNAL_PRESERVE_STACK_TRACE, InternalPreserveStackTrace, IM_RetVoid)


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
#ifdef FEATURE_COMINTEROP
DEFINE_METHOD(RT_TYPE_HANDLE,       ALLOCATECOMOBJECT,      AllocateComObject,          SM_VoidPtr_RetObj)
#endif
DEFINE_FIELD(RT_TYPE_HANDLE,        M_TYPE,                 m_type)

DEFINE_CLASS_U(Reflection,             RtFieldInfo,         NoClass)
DEFINE_FIELD_U(m_fieldHandle,              ReflectFieldObject, m_pFD)
DEFINE_CLASS(RT_FIELD_INFO,         Reflection,             RtFieldInfo)
DEFINE_FIELD(RT_FIELD_INFO,         HANDLE,                 m_fieldHandle)

DEFINE_CLASS_U(System,                 RuntimeFieldInfoStub,       ReflectFieldObject)
DEFINE_FIELD_U(m_fieldHandle,              ReflectFieldObject, m_pFD)
DEFINE_CLASS(STUBFIELDINFO,         System,                 RuntimeFieldInfoStub)
#if FOR_ILLINK
DEFINE_METHOD(STUBFIELDINFO,        CTOR,                   .ctor,                      IM_RetVoid)
#endif

DEFINE_CLASS(FIELD,                 Reflection,             RuntimeFieldInfo)

DEFINE_CLASS(FIELD_HANDLE,          System,                 RuntimeFieldHandle)
DEFINE_FIELD(FIELD_HANDLE,          M_FIELD,                m_ptr)

DEFINE_CLASS(I_RT_FIELD_INFO,       System,                 IRuntimeFieldInfo)

DEFINE_CLASS(FIELD_INFO,            Reflection,             FieldInfo)
DEFINE_METHOD(FIELD_INFO,           SET_VALUE,              SetValue,                   IM_Obj_Obj_BindingFlags_Binder_CultureInfo_RetVoid)
DEFINE_METHOD(FIELD_INFO,           GET_VALUE,              GetValue,                   IM_Obj_RetObj)


DEFINE_CLASS(GUID,                  System,                 Guid)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(VARIANT,               System,                 Variant)
DEFINE_METHOD(VARIANT,              CONVERT_OBJECT_TO_VARIANT,MarshalHelperConvertObjectToVariant,SM_Obj_RefVariant_RetVoid)
DEFINE_METHOD(VARIANT,              CAST_VARIANT,           MarshalHelperCastVariant,   SM_Obj_Int_RefVariant_RetVoid)
DEFINE_METHOD(VARIANT,              CONVERT_VARIANT_TO_OBJECT,MarshalHelperConvertVariantToObject,SM_RefVariant_RetObject)

DEFINE_CLASS_U(System,              Variant,                VariantData)
DEFINE_FIELD_U(_objref,             VariantData,            m_objref)
DEFINE_FIELD_U(_data,               VariantData,            m_data)
DEFINE_FIELD_U(_flags,              VariantData,            m_flags)
#endif // FEATURE_COMINTEROP

DEFINE_CLASS(IASYNCRESULT,          System,                 IAsyncResult)

DEFINE_CLASS(ICUSTOM_ATTR_PROVIDER, Reflection,             ICustomAttributeProvider)
DEFINE_METHOD(ICUSTOM_ATTR_PROVIDER,GET_CUSTOM_ATTRIBUTES,  GetCustomAttributes,        IM_Type_RetArrObj)

DEFINE_CLASS(ICUSTOM_MARSHALER,     Interop,                ICustomMarshaler)
DEFINE_METHOD(ICUSTOM_MARSHALER,    MARSHAL_NATIVE_TO_MANAGED,MarshalNativeToManaged,   IM_IntPtr_RetObj)
DEFINE_METHOD(ICUSTOM_MARSHALER,    MARSHAL_MANAGED_TO_NATIVE,MarshalManagedToNative,   IM_Obj_RetIntPtr)
DEFINE_METHOD(ICUSTOM_MARSHALER,    CLEANUP_NATIVE_DATA,    CleanUpNativeData,          IM_IntPtr_RetVoid)
DEFINE_METHOD(ICUSTOM_MARSHALER,    CLEANUP_MANAGED_DATA,   CleanUpManagedData,         IM_Obj_RetVoid)
DEFINE_METHOD(ICUSTOM_MARSHALER,    GET_NATIVE_DATA_SIZE,   GetNativeDataSize,          IM_RetInt)

DEFINE_CLASS(IDYNAMICINTERFACECASTABLE,         Interop,   IDynamicInterfaceCastable)
DEFINE_CLASS(DYNAMICINTERFACECASTABLEHELPERS,   Interop,   DynamicInterfaceCastableHelpers)
DEFINE_METHOD(DYNAMICINTERFACECASTABLEHELPERS,  IS_INTERFACE_IMPLEMENTED,       IsInterfaceImplemented,     SM_IDynamicInterfaceCastable_RuntimeType_Bool_RetBool)
DEFINE_METHOD(DYNAMICINTERFACECASTABLEHELPERS,  GET_INTERFACE_IMPLEMENTATION,   GetInterfaceImplementation, SM_IDynamicInterfaceCastable_RuntimeType_RetRtType)

#ifdef FEATURE_COMINTEROP
DEFINE_CLASS(ICUSTOM_QUERYINTERFACE,      Interop,          ICustomQueryInterface)
DEFINE_METHOD(ICUSTOM_QUERYINTERFACE,     GET_INTERFACE,    GetInterface,               IM_RefGuid_OutIntPtr_RetCustomQueryInterfaceResult)
DEFINE_CLASS(CUSTOMQUERYINTERFACERESULT,  Interop,          CustomQueryInterfaceResult)
#endif //FEATURE_COMINTEROP

#ifdef FEATURE_COMWRAPPERS
DEFINE_CLASS(COMWRAPPERS,                 Interop,          ComWrappers)
DEFINE_CLASS(CREATECOMINTERFACEFLAGS,     Interop,          CreateComInterfaceFlags)
DEFINE_CLASS(CREATEOBJECTFLAGS,           Interop,          CreateObjectFlags)
DEFINE_CLASS(COMWRAPPERSSCENARIO,         Interop,          ComWrappersScenario)
DEFINE_METHOD(COMWRAPPERS,                COMPUTE_VTABLES,  CallComputeVtables,         SM_Scenario_ComWrappers_Obj_CreateFlags_RefInt_RetPtrVoid)
DEFINE_METHOD(COMWRAPPERS,                CREATE_OBJECT,    CallCreateObject,           SM_Scenario_ComWrappers_IntPtr_CreateFlags_RetObj)
DEFINE_METHOD(COMWRAPPERS,                RELEASE_OBJECTS,  CallReleaseObjects,         SM_ComWrappers_IEnumerable_RetVoid)
DEFINE_METHOD(COMWRAPPERS,     CALL_ICUSTOMQUERYINTERFACE,  CallICustomQueryInterface,  SM_Obj_RefGuid_RefIntPtr_RetInt)
#endif //FEATURE_COMWRAPPERS

#ifdef FEATURE_OBJCMARSHAL
DEFINE_CLASS(OBJCMARSHAL,    ObjectiveC, ObjectiveCMarshal)
DEFINE_METHOD(OBJCMARSHAL,   AVAILABLEUNHANDLEDEXCEPTIONPROPAGATION, AvailableUnhandledExceptionPropagation, SM_RetBool)
DEFINE_METHOD(OBJCMARSHAL,   INVOKEUNHANDLEDEXCEPTIONPROPAGATION,    InvokeUnhandledExceptionPropagation,    SM_Exception_Obj_RefIntPtr_RetVoidPtr)
#endif // FEATURE_OBJCMARSHAL

DEFINE_CLASS(IENUMERATOR,           Collections,            IEnumerator)

DEFINE_CLASS(IENUMERABLE,           Collections,            IEnumerable)
DEFINE_CLASS(ICOLLECTION,           Collections,            ICollection)
DEFINE_CLASS(ILIST,                 Collections,            IList)
DEFINE_CLASS(IDISPOSABLE,           System,                 IDisposable)

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
DEFINE_METHOD(MARSHAL,              GET_FUNCTION_POINTER_FOR_DELEGATE,          GetFunctionPointerForDelegate,          SM_Delegate_RetIntPtr)
DEFINE_METHOD(MARSHAL,              GET_DELEGATE_FOR_FUNCTION_POINTER,          GetDelegateForFunctionPointer,          SM_IntPtr_Type_RetDelegate)
DEFINE_METHOD(MARSHAL,              GET_DELEGATE_FOR_FUNCTION_POINTER_INTERNAL, GetDelegateForFunctionPointerInternal,  SM_IntPtr_Type_RetDelegate)

DEFINE_METHOD(MARSHAL,              ALLOC_CO_TASK_MEM,                 AllocCoTaskMem,                SM_Int_RetIntPtr)
DEFINE_METHOD(MARSHAL,              FREE_CO_TASK_MEM,                  FreeCoTaskMem,                 SM_IntPtr_RetVoid)
DEFINE_FIELD(MARSHAL,               SYSTEM_MAX_DBCS_CHAR_SIZE,         SystemMaxDBCSCharSize)

DEFINE_METHOD(MARSHAL,              STRUCTURE_TO_PTR,                  StructureToPtr,                SM_Obj_IntPtr_Bool_RetVoid)
DEFINE_METHOD(MARSHAL,              PTR_TO_STRUCTURE,                  PtrToStructure,                SM_IntPtr_Obj_RetVoid)
DEFINE_METHOD(MARSHAL,              DESTROY_STRUCTURE,                 DestroyStructure,              SM_IntPtr_Type_RetVoid)
DEFINE_METHOD(MARSHAL,              SIZEOF_TYPE,                       SizeOf,                        SM_Type_RetInt)

DEFINE_CLASS(NATIVELIBRARY, Interop, NativeLibrary)
DEFINE_METHOD(NATIVELIBRARY,        LOADLIBRARYCALLBACKSTUB, LoadLibraryCallbackStub, SM_Str_AssemblyBase_Bool_UInt_RetIntPtr)

DEFINE_CLASS(VECTOR64T,             Intrinsics,             Vector64`1)
DEFINE_CLASS(VECTOR128T,            Intrinsics,             Vector128`1)
DEFINE_CLASS(VECTOR256T,            Intrinsics,             Vector256`1)

#ifndef CROSSGEN_COMPILE
DEFINE_CLASS(VECTORT,               Numerics,               Vector`1)
#endif // !CROSSGEN_COMPILE

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
#if FOR_ILLINK
DEFINE_METHOD(RUNTIME_EH_CLAUSE,            CTOR,                   .ctor,              IM_RetVoid)
#endif

DEFINE_CLASS_U(Reflection,             RuntimeLocalVariableInfo,        RuntimeLocalVariableInfo)
DEFINE_FIELD_U(_type,                  RuntimeLocalVariableInfo,        _type)
DEFINE_FIELD_U(_localIndex,            RuntimeLocalVariableInfo,        _localIndex)
DEFINE_FIELD_U(_isPinned,              RuntimeLocalVariableInfo,        _isPinned)
DEFINE_CLASS(RUNTIME_LOCAL_VARIABLE_INFO,   Reflection,             RuntimeLocalVariableInfo)
#if FOR_ILLINK
DEFINE_METHOD(RUNTIME_LOCAL_VARIABLE_INFO,  CTOR,                   .ctor,              IM_RetVoid)
#endif

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
#if FOR_ILLINK
DEFINE_METHOD(MODULE_BUILDER,       CTOR,                   .ctor,                      IM_RetVoid)
#endif
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
DEFINE_METHOD(BYREFERENCE,          CTOR,                   .ctor, NoSig)
DEFINE_METHOD(BYREFERENCE,          GET_VALUE,              get_Value, NoSig)
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
DEFINE_METHOD(RUNTIME_HELPERS,      IS_REFERENCE_OR_CONTAINS_REFERENCES, IsReferenceOrContainsReferences, NoSig)
DEFINE_METHOD(RUNTIME_HELPERS,      IS_BITWISE_EQUATABLE,    IsBitwiseEquatable, NoSig)
DEFINE_METHOD(RUNTIME_HELPERS,      GET_METHOD_TABLE,        GetMethodTable,     NoSig)
DEFINE_METHOD(RUNTIME_HELPERS,      GET_RAW_DATA,            GetRawData,         NoSig)
DEFINE_METHOD(RUNTIME_HELPERS,      GET_RAW_ARRAY_DATA,      GetRawArrayData, NoSig)
DEFINE_METHOD(RUNTIME_HELPERS,      GET_UNINITIALIZED_OBJECT, GetUninitializedObject, SM_Type_RetObj)
DEFINE_METHOD(RUNTIME_HELPERS,      ENUM_EQUALS,            EnumEquals, NoSig)
DEFINE_METHOD(RUNTIME_HELPERS,      ENUM_COMPARE_TO,        EnumCompareTo, NoSig)
DEFINE_METHOD(RUNTIME_HELPERS,      ALLOC_TAILCALL_ARG_BUFFER, AllocTailCallArgBuffer,  SM_Int_IntPtr_RetIntPtr)
DEFINE_METHOD(RUNTIME_HELPERS,      GET_TAILCALL_INFO,      GetTailCallInfo, NoSig)
DEFINE_METHOD(RUNTIME_HELPERS,      DISPATCH_TAILCALLS,     DispatchTailCalls,          NoSig)

DEFINE_CLASS(UNSAFE,                InternalCompilerServices,       Unsafe)
DEFINE_METHOD(UNSAFE,               AS_POINTER,             AsPointer, NoSig)
DEFINE_METHOD(UNSAFE,               BYREF_IS_NULL,          IsNullRef, NoSig)
DEFINE_METHOD(UNSAFE,               BYREF_NULLREF,          NullRef, NoSig)
DEFINE_METHOD(UNSAFE,               AS_REF_IN,              AsRef, GM_RefT_RetRefT)
DEFINE_METHOD(UNSAFE,               AS_REF_POINTER,         AsRef, GM_VoidPtr_RetRefT)
DEFINE_METHOD(UNSAFE,               SIZEOF,                 SizeOf, NoSig)
DEFINE_METHOD(UNSAFE,               BYREF_AS,               As, GM_RefTFrom_RetRefTTo)
DEFINE_METHOD(UNSAFE,               OBJECT_AS,              As, GM_Obj_RetT)
DEFINE_METHOD(UNSAFE,               BYREF_ADD,              Add, GM_RefT_Int_RetRefT)
DEFINE_METHOD(UNSAFE,               BYREF_INTPTR_ADD,       Add, GM_RefT_IntPtr_RetRefT)
DEFINE_METHOD(UNSAFE,               PTR_ADD,                Add, GM_PtrVoid_Int_RetPtrVoid)
DEFINE_METHOD(UNSAFE,               BYREF_BYTE_OFFSET,      ByteOffset, NoSig)
DEFINE_METHOD(UNSAFE,               BYREF_ADD_BYTE_OFFSET,  AddByteOffset, GM_RefT_IntPtr_RetRefT)
DEFINE_METHOD(UNSAFE,               BYREF_ARE_SAME,         AreSame, NoSig)
DEFINE_METHOD(UNSAFE,               BYREF_IS_ADDRESS_GREATER_THAN, IsAddressGreaterThan, NoSig)
DEFINE_METHOD(UNSAFE,               BYREF_IS_ADDRESS_LESS_THAN, IsAddressLessThan, NoSig)
DEFINE_METHOD(UNSAFE,               BYREF_INIT_BLOCK_UNALIGNED, InitBlockUnaligned, NoSig)
DEFINE_METHOD(UNSAFE,               BYREF_READ_UNALIGNED,   ReadUnaligned, GM_RefByte_RetT)
DEFINE_METHOD(UNSAFE,               BYREF_WRITE_UNALIGNED,  WriteUnaligned, GM_RefByte_T_RetVoid)
DEFINE_METHOD(UNSAFE,               PTR_READ_UNALIGNED,     ReadUnaligned, GM_PtrVoid_RetT)
DEFINE_METHOD(UNSAFE,               PTR_WRITE_UNALIGNED,    WriteUnaligned, GM_PtrVoid_T_RetVoid)
DEFINE_METHOD(UNSAFE,               SKIPINIT,               SkipInit, GM_RefT_RetVoid)

DEFINE_CLASS(MEMORY_MARSHAL,        Interop,                MemoryMarshal)
DEFINE_METHOD(MEMORY_MARSHAL,       GET_ARRAY_DATA_REFERENCE, GetArrayDataReference, NoSig)

DEFINE_CLASS(INTERLOCKED,           Threading,              Interlocked)
DEFINE_METHOD(INTERLOCKED,          COMPARE_EXCHANGE_T,     CompareExchange, GM_RefT_T_T_RetT)
DEFINE_METHOD(INTERLOCKED,          COMPARE_EXCHANGE_OBJECT,CompareExchange, SM_RefObject_Object_Object_RetObject)

DEFINE_CLASS(RAW_DATA,              CompilerServices,       RawData)
DEFINE_FIELD(RAW_DATA,              DATA,                   Data)

DEFINE_CLASS(RAW_ARRAY_DATA,        CompilerServices,       RawArrayData)
DEFINE_FIELD(RAW_ARRAY_DATA,        LENGTH,                 Length)
#ifdef TARGET_64BIT
DEFINE_FIELD(RAW_ARRAY_DATA,        PADDING,                Padding)
#endif
DEFINE_FIELD(RAW_ARRAY_DATA,        DATA,                   Data)

DEFINE_CLASS(PORTABLE_TAIL_CALL_FRAME, CompilerServices,              PortableTailCallFrame)
DEFINE_FIELD(PORTABLE_TAIL_CALL_FRAME, PREV,                          Prev)
DEFINE_FIELD(PORTABLE_TAIL_CALL_FRAME, TAILCALL_AWARE_RETURN_ADDRESS, TailCallAwareReturnAddress)
DEFINE_FIELD(PORTABLE_TAIL_CALL_FRAME, NEXT_CALL,                     NextCall)

DEFINE_CLASS(TAIL_CALL_TLS,            CompilerServices,              TailCallTls)
DEFINE_FIELD(TAIL_CALL_TLS,            FRAME,                         Frame)
DEFINE_FIELD(TAIL_CALL_TLS,            ARG_BUFFER,                    ArgBuffer)

DEFINE_CLASS_U(CompilerServices,           PortableTailCallFrame, PortableTailCallFrame)
DEFINE_FIELD_U(Prev,                       PortableTailCallFrame, Prev)
DEFINE_FIELD_U(TailCallAwareReturnAddress, PortableTailCallFrame, TailCallAwareReturnAddress)
DEFINE_FIELD_U(NextCall,                   PortableTailCallFrame, NextCall)

DEFINE_CLASS_U(CompilerServices,           TailCallTls,           TailCallTls)
DEFINE_FIELD_U(Frame,                      TailCallTls,           m_frame)
DEFINE_FIELD_U(ArgBuffer,                  TailCallTls,           m_argBuffer)

DEFINE_CLASS(RUNTIME_WRAPPED_EXCEPTION, CompilerServices,   RuntimeWrappedException)
DEFINE_METHOD(RUNTIME_WRAPPED_EXCEPTION, OBJ_CTOR,          .ctor,                      IM_Obj_RetVoid)
DEFINE_FIELD(RUNTIME_WRAPPED_EXCEPTION, WRAPPED_EXCEPTION,  _wrappedException)

DEFINE_CLASS(CALLCONV_CDECL,                 CompilerServices,       CallConvCdecl)
DEFINE_CLASS(CALLCONV_STDCALL,               CompilerServices,       CallConvStdcall)
DEFINE_CLASS(CALLCONV_THISCALL,              CompilerServices,       CallConvThiscall)
DEFINE_CLASS(CALLCONV_FASTCALL,              CompilerServices,       CallConvFastcall)
DEFINE_CLASS(CALLCONV_SUPPRESSGCTRANSITION,  CompilerServices,       CallConvSuppressGCTransition)
DEFINE_CLASS(CALLCONV_MEMBERFUNCTION,        CompilerServices,       CallConvMemberFunction)

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
DEFINE_METHOD(SAFE_TYPENAMEPARSER_HANDLE,   CTOR,   .ctor,  IM_RetVoid)

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

DEFINE_CLASS(STRING_BUILDER,        Text,                   StringBuilder)
DEFINE_PROPERTY(STRING_BUILDER,     LENGTH,                 Length,                     Int)
DEFINE_PROPERTY(STRING_BUILDER,     CAPACITY,               Capacity,                   Int)
DEFINE_METHOD(STRING_BUILDER,       CTOR_INT,               .ctor,                      IM_Int_RetVoid)
DEFINE_METHOD(STRING_BUILDER,       TO_STRING,              ToString,                   IM_RetStr)
DEFINE_METHOD(STRING_BUILDER,       INTERNAL_COPY,          InternalCopy,               IM_IntPtr_Int_RetVoid)
DEFINE_METHOD(STRING_BUILDER,       REPLACE_BUFFER_INTERNAL,ReplaceBufferInternal,      IM_PtrChar_Int_RetVoid)
DEFINE_METHOD(STRING_BUILDER,       REPLACE_BUFFER_ANSI_INTERNAL,ReplaceBufferAnsiInternal, IM_PtrSByt_Int_RetVoid)

DEFINE_CLASS_U(Threading,              SynchronizationContext, SynchronizationContextObject)
DEFINE_FIELD_U(_requireWaitNotification, SynchronizationContextObject, _requireWaitNotification)
DEFINE_CLASS(SYNCHRONIZATION_CONTEXT,    Threading,              SynchronizationContext)
DEFINE_METHOD(SYNCHRONIZATION_CONTEXT,  INVOKE_WAIT_METHOD_HELPER, InvokeWaitMethodHelper, SM_SyncCtx_ArrIntPtr_Bool_Int_RetInt)

#ifdef DEBUG
DEFINE_CLASS(STACKCRAWMARK,         Threading,       StackCrawlMark)
#endif

DEFINE_CLASS_U(Threading,              Thread,                     ThreadBaseObject)
DEFINE_FIELD_U(_name,                     ThreadBaseObject,   m_Name)
DEFINE_FIELD_U(_startHelper,              ThreadBaseObject,   m_StartHelper)
DEFINE_FIELD_U(_DONT_USE_InternalThread,  ThreadBaseObject,   m_InternalThread)
DEFINE_FIELD_U(_priority,                 ThreadBaseObject,   m_Priority)
DEFINE_CLASS(THREAD,                Threading,              Thread)
DEFINE_METHOD(THREAD,               INTERNAL_GET_CURRENT_THREAD,             InternalGetCurrentThread,                    SM_RetIntPtr)
DEFINE_METHOD(THREAD,               START_CALLBACK,                          StartCallback,                               IM_RetVoid)
#ifdef FEATURE_OBJCMARSHAL
DEFINE_CLASS(AUTORELEASEPOOL,       Threading,              AutoreleasePool)
DEFINE_METHOD(AUTORELEASEPOOL,      CREATEAUTORELEASEPOOL,  CreateAutoreleasePool,  SM_RetVoid)
DEFINE_METHOD(AUTORELEASEPOOL,      DRAINAUTORELEASEPOOL,   DrainAutoreleasePool,   SM_RetVoid)
#endif // FEATURE_OBJCMARSHAL

DEFINE_CLASS(IOCB_HELPER,              Threading,            _IOCompletionCallback)
DEFINE_METHOD(IOCB_HELPER,             PERFORM_IOCOMPLETION_CALLBACK,        PerformIOCompletionCallback,          SM_UInt_UInt_PtrNativeOverlapped_RetVoid)

DEFINE_CLASS(TPWAITORTIMER_HELPER,              Threading,            _ThreadPoolWaitOrTimerCallback)
DEFINE_METHOD(TPWAITORTIMER_HELPER,             PERFORM_WAITORTIMER_CALLBACK,        PerformWaitOrTimerCallback,          SM__ThreadPoolWaitOrTimerCallback_Bool_RetVoid)

DEFINE_CLASS(TP_WAIT_CALLBACK,         Threading,              _ThreadPoolWaitCallback)
DEFINE_METHOD(TP_WAIT_CALLBACK,        PERFORM_WAIT_CALLBACK,               PerformWaitCallback,                   SM_RetBool)

DEFINE_CLASS(TIMER_QUEUE,           Threading,                TimerQueue)
DEFINE_METHOD(TIMER_QUEUE,          APPDOMAIN_TIMER_CALLBACK, AppDomainTimerCallback,   SM_Int_RetVoid)

DEFINE_CLASS(THREAD_POOL,           Threading,                          ThreadPool)
DEFINE_METHOD(THREAD_POOL,          ENSURE_GATE_THREAD_RUNNING,         EnsureGateThreadRunning,        SM_RetVoid)
DEFINE_METHOD(THREAD_POOL,          UNSAFE_QUEUE_UNMANAGED_WORK_ITEM,   UnsafeQueueUnmanagedWorkItem,   SM_IntPtr_IntPtr_RetVoid)

DEFINE_CLASS(COMPLETE_WAIT_THREAD_POOL_WORK_ITEM,   Threading,      CompleteWaitThreadPoolWorkItem)
DEFINE_METHOD(COMPLETE_WAIT_THREAD_POOL_WORK_ITEM,  COMPLETE_WAIT,  CompleteWait,                   IM_RetVoid)

DEFINE_CLASS(TIMESPAN,              System,                 TimeSpan)


DEFINE_CLASS(TYPE,                  System,                 Type)
DEFINE_METHOD(TYPE,                 GET_TYPE_FROM_HANDLE,   GetTypeFromHandle,          SM_RuntimeTypeHandle_RetType)
DEFINE_PROPERTY(TYPE,               IS_IMPORT,              IsImport,                   Bool)

DEFINE_CLASS(TYPE_DELEGATOR,        Reflection,             TypeDelegator)

DEFINE_CLASS(UNHANDLED_EVENTARGS,   System,                 UnhandledExceptionEventArgs)
DEFINE_METHOD(UNHANDLED_EVENTARGS,  CTOR,                   .ctor,                      IM_Obj_Bool_RetVoid)

DEFINE_CLASS(FIRSTCHANCE_EVENTARGS,   ExceptionServices,      FirstChanceExceptionEventArgs)
DEFINE_METHOD(FIRSTCHANCE_EVENTARGS,  CTOR,                   .ctor,                      IM_Exception_RetVoid)

DEFINE_CLASS(EXCEPTION_DISPATCH_INFO, ExceptionServices,      ExceptionDispatchInfo)
DEFINE_METHOD(EXCEPTION_DISPATCH_INFO, CAPTURE, Capture, NoSig)
DEFINE_METHOD(EXCEPTION_DISPATCH_INFO, THROW, Throw, IM_RetVoid)

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
DEFINE_FIELD(ASSEMBLYLOADCONTEXT,   ASSEMBLY_LOAD,              AssemblyLoad)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  ON_ASSEMBLY_LOAD,           OnAssemblyLoad,             SM_Assembly_RetVoid)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  ON_RESOURCE_RESOLVE,        OnResourceResolve,          SM_Assembly_Str_RetAssembly)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  ON_TYPE_RESOLVE,            OnTypeResolve,              SM_Assembly_Str_RetAssembly)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  ON_ASSEMBLY_RESOLVE,        OnAssemblyResolve,          SM_Assembly_Str_RetAssembly)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  START_ASSEMBLY_LOAD,        StartAssemblyLoad,          SM_RefGuid_RefGuid_RetVoid)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  STOP_ASSEMBLY_LOAD,         StopAssemblyLoad,           SM_RefGuid_RetVoid)
DEFINE_METHOD(ASSEMBLYLOADCONTEXT,  INITIALIZE_DEFAULT_CONTEXT, InitializeDefaultContext,   SM_RetVoid)

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
DEFINE_METHOD(DEBUGGER,             BREAK,                  Break,                  SM_RetVoid)

DEFINE_CLASS(BUFFER,                System,                 Buffer)
DEFINE_METHOD(BUFFER,               MEMCPY_PTRBYTE_ARRBYTE, Memcpy,                 SM_PtrByte_Int_ArrByte_Int_Int_RetVoid)
DEFINE_METHOD(BUFFER,               MEMCPY,                 Memcpy,                 SM_PtrByte_PtrByte_Int_RetVoid)

DEFINE_CLASS(STUBHELPERS,           StubHelpers,            StubHelpers)
DEFINE_METHOD(STUBHELPERS,          INIT_DECLARING_TYPE,    InitDeclaringType,          SM_IntPtr_RetVoid)
DEFINE_METHOD(STUBHELPERS,          GET_NDIRECT_TARGET,     GetNDirectTarget,           SM_IntPtr_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          GET_DELEGATE_TARGET,    GetDelegateTarget,          SM_Delegate_RefIntPtr_RetIntPtr)
#ifdef FEATURE_COMINTEROP
DEFINE_METHOD(STUBHELPERS,          GET_COM_HR_EXCEPTION_OBJECT,              GetCOMHRExceptionObject,            SM_Int_IntPtr_Obj_RetException)
DEFINE_METHOD(STUBHELPERS,          GET_COM_IP_FROM_RCW,                      GetCOMIPFromRCW,                    SM_Obj_IntPtr_RefIntPtr_RefBool_RetIntPtr)
#endif // FEATURE_COMINTEROP
DEFINE_METHOD(STUBHELPERS,          SET_LAST_ERROR,         SetLastError,               SM_RetVoid)
DEFINE_METHOD(STUBHELPERS,          CLEAR_LAST_ERROR,       ClearLastError,             SM_RetVoid)

DEFINE_METHOD(STUBHELPERS,          THROW_INTEROP_PARAM_EXCEPTION, ThrowInteropParamException,   SM_Int_Int_RetVoid)
DEFINE_METHOD(STUBHELPERS,          ADD_TO_CLEANUP_LIST_SAFEHANDLE,    AddToCleanupList,           SM_RefCleanupWorkListElement_SafeHandle_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          KEEP_ALIVE_VIA_CLEANUP_LIST,    KeepAliveViaCleanupList,       SM_RefCleanupWorkListElement_Obj_RetVoid)
DEFINE_METHOD(STUBHELPERS,          DESTROY_CLEANUP_LIST,   DestroyCleanupList,         SM_RefCleanupWorkListElement_RetVoid)
DEFINE_METHOD(STUBHELPERS,          GET_HR_EXCEPTION_OBJECT, GetHRExceptionObject,      SM_Int_RetException)
DEFINE_METHOD(STUBHELPERS,          GET_PENDING_EXCEPTION_OBJECT, GetPendingExceptionObject,      SM_RetException)
DEFINE_METHOD(STUBHELPERS,          CREATE_CUSTOM_MARSHALER_HELPER, CreateCustomMarshalerHelper, SM_IntPtr_Int_IntPtr_RetIntPtr)

DEFINE_METHOD(STUBHELPERS,          CHECK_STRING_LENGTH,    CheckStringLength,          SM_Int_RetVoid)

DEFINE_METHOD(STUBHELPERS,          FMT_CLASS_UPDATE_NATIVE_INTERNAL,   FmtClassUpdateNativeInternal,   SM_Obj_PtrByte_RefCleanupWorkListElement_RetVoid)
DEFINE_METHOD(STUBHELPERS,          FMT_CLASS_UPDATE_CLR_INTERNAL,      FmtClassUpdateCLRInternal,      SM_Obj_PtrByte_RetVoid)
DEFINE_METHOD(STUBHELPERS,          LAYOUT_DESTROY_NATIVE_INTERNAL,     LayoutDestroyNativeInternal,    SM_Obj_PtrByte_RetVoid)
DEFINE_METHOD(STUBHELPERS,          ALLOCATE_INTERNAL,                  AllocateInternal,               SM_IntPtr_RetObj)
DEFINE_METHOD(STUBHELPERS,          MARSHAL_TO_MANAGED_VA_LIST_INTERNAL,MarshalToManagedVaListInternal, SM_IntPtr_IntPtr_RetVoid)
DEFINE_METHOD(STUBHELPERS,          MARSHAL_TO_UNMANAGED_VA_LIST_INTERNAL,MarshalToUnmanagedVaListInternal,SM_IntPtr_UInt_IntPtr_RetVoid)
DEFINE_METHOD(STUBHELPERS,          CALC_VA_LIST_SIZE,                  CalcVaListSize,                 SM_IntPtr_RetUInt)
DEFINE_METHOD(STUBHELPERS,          VALIDATE_OBJECT,                    ValidateObject,                 SM_Obj_IntPtr_Obj_RetVoid)
DEFINE_METHOD(STUBHELPERS,          VALIDATE_BYREF,                     ValidateByref,                  SM_IntPtr_IntPtr_Obj_RetVoid)
DEFINE_METHOD(STUBHELPERS,          GET_STUB_CONTEXT,                   GetStubContext,                 SM_RetIntPtr)
DEFINE_METHOD(STUBHELPERS,          LOG_PINNED_ARGUMENT,                LogPinnedArgument,              SM_IntPtr_IntPtr_RetVoid)
#ifdef TARGET_64BIT
DEFINE_METHOD(STUBHELPERS,          GET_STUB_CONTEXT_ADDR,              GetStubContextAddr,             SM_RetIntPtr)
#endif // TARGET_64BIT
DEFINE_METHOD(STUBHELPERS,          NEXT_CALL_RETURN_ADDRESS,           NextCallReturnAddress,          SM_RetIntPtr)
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

DEFINE_CLASS(ANSICHARMARSHALER,     StubHelpers,            AnsiCharMarshaler)
DEFINE_METHOD(ANSICHARMARSHALER,    CONVERT_TO_NATIVE,      ConvertToNative,            SM_Char_Bool_Bool_RetByte)
DEFINE_METHOD(ANSICHARMARSHALER,    CONVERT_TO_MANAGED,     ConvertToManaged,           SM_Byte_RetChar)
DEFINE_METHOD(ANSICHARMARSHALER,    DO_ANSI_CONVERSION,     DoAnsiConversion,           SM_Str_Bool_Bool_RefInt_RetArrByte)

DEFINE_CLASS(CSTRMARSHALER,         StubHelpers,            CSTRMarshaler)
DEFINE_METHOD(CSTRMARSHALER,        CONVERT_TO_NATIVE,      ConvertToNative,            SM_Int_Str_IntPtr_RetIntPtr)
DEFINE_METHOD(CSTRMARSHALER,        CONVERT_FIXED_TO_NATIVE,ConvertFixedToNative,       SM_Int_Str_IntPtr_Int_RetVoid)
DEFINE_METHOD(CSTRMARSHALER,        CONVERT_TO_MANAGED,     ConvertToManaged,           SM_IntPtr_RetStr)
DEFINE_METHOD(CSTRMARSHALER,        CONVERT_FIXED_TO_MANAGED,ConvertFixedToManaged,     SM_IntPtr_Int_RetStr)
DEFINE_METHOD(CSTRMARSHALER,        CLEAR_NATIVE,           ClearNative,                SM_IntPtr_RetVoid)

DEFINE_CLASS(FIXEDWSTRMARSHALER,   StubHelpers,            FixedWSTRMarshaler)
DEFINE_METHOD(FIXEDWSTRMARSHALER,  CONVERT_TO_NATIVE,      ConvertToNative,            SM_Str_IntPtr_Int_RetVoid)

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

DEFINE_CLASS(INTERFACEMARSHALER,    StubHelpers,            InterfaceMarshaler)
DEFINE_METHOD(INTERFACEMARSHALER,   CONVERT_TO_NATIVE,      ConvertToNative,            SM_Obj_IntPtr_IntPtr_Int_RetIntPtr)
DEFINE_METHOD(INTERFACEMARSHALER,   CONVERT_TO_MANAGED,     ConvertToManaged,           SM_RefIntPtr_IntPtr_IntPtr_Int_RetObj)
DEFINE_METHOD(INTERFACEMARSHALER,   CLEAR_NATIVE,           ClearNative,                SM_IntPtr_RetVoid)


DEFINE_CLASS(MNGD_SAFE_ARRAY_MARSHALER,  StubHelpers,                 MngdSafeArrayMarshaler)
DEFINE_METHOD(MNGD_SAFE_ARRAY_MARSHALER, CREATE_MARSHALER,            CreateMarshaler,            SM_IntPtr_IntPtr_Int_Int_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_SAFE_ARRAY_MARSHALER, CONVERT_SPACE_TO_NATIVE,     ConvertSpaceToNative,       SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_SAFE_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_NATIVE,  ConvertContentsToNative,    SM_IntPtr_RefObj_IntPtr_Obj_RetVoid)
DEFINE_METHOD(MNGD_SAFE_ARRAY_MARSHALER, CONVERT_SPACE_TO_MANAGED,    ConvertSpaceToManaged,      SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_SAFE_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_MANAGED, ConvertContentsToManaged,   SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_SAFE_ARRAY_MARSHALER, CLEAR_NATIVE,                ClearNative,                SM_IntPtr_RefObj_IntPtr_RetVoid)
#endif // FEATURE_COMINTEROP

DEFINE_CLASS(DATEMARSHALER,         StubHelpers,            DateMarshaler)
DEFINE_METHOD(DATEMARSHALER,        CONVERT_TO_NATIVE,      ConvertToNative,            SM_DateTime_RetDbl)
DEFINE_METHOD(DATEMARSHALER,        CONVERT_TO_MANAGED,     ConvertToManaged,           SM_Dbl_RetLong)

DEFINE_CLASS(VBBYVALSTRMARSHALER,   StubHelpers,            VBByValStrMarshaler)
DEFINE_METHOD(VBBYVALSTRMARSHALER,  CONVERT_TO_NATIVE,      ConvertToNative,            SM_Str_Bool_Bool_RefInt_RetIntPtr)
DEFINE_METHOD(VBBYVALSTRMARSHALER,  CONVERT_TO_MANAGED,     ConvertToManaged,           SM_IntPtr_Int_RetStr)
DEFINE_METHOD(VBBYVALSTRMARSHALER,  CLEAR_NATIVE,           ClearNative,                SM_IntPtr_RetVoid)

DEFINE_CLASS(MNGD_NATIVE_ARRAY_MARSHALER,  StubHelpers,                 MngdNativeArrayMarshaler)
DEFINE_METHOD(MNGD_NATIVE_ARRAY_MARSHALER, CREATE_MARSHALER,            CreateMarshaler,            SM_IntPtr_IntPtr_Int_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_NATIVE_ARRAY_MARSHALER, CONVERT_SPACE_TO_NATIVE,     ConvertSpaceToNative,       SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_NATIVE_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_NATIVE,  ConvertContentsToNative,    SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_NATIVE_ARRAY_MARSHALER, CONVERT_SPACE_TO_MANAGED,    ConvertSpaceToManaged,      SM_IntPtr_RefObj_IntPtr_Int_RetVoid)
DEFINE_METHOD(MNGD_NATIVE_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_MANAGED, ConvertContentsToManaged,   SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_NATIVE_ARRAY_MARSHALER, CLEAR_NATIVE,                ClearNative,                SM_IntPtr_RefObj_IntPtr_Int_RetVoid)
DEFINE_METHOD(MNGD_NATIVE_ARRAY_MARSHALER, CLEAR_NATIVE_CONTENTS,       ClearNativeContents,        SM_IntPtr_RefObj_IntPtr_Int_RetVoid)

DEFINE_CLASS(MNGD_FIXED_ARRAY_MARSHALER,  StubHelpers,                 MngdFixedArrayMarshaler)
DEFINE_METHOD(MNGD_FIXED_ARRAY_MARSHALER, CREATE_MARSHALER,            CreateMarshaler,            SM_IntPtr_IntPtr_Int_Int_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_FIXED_ARRAY_MARSHALER, CONVERT_SPACE_TO_NATIVE,     ConvertSpaceToNative,       SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_FIXED_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_NATIVE,  ConvertContentsToNative,    SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_FIXED_ARRAY_MARSHALER, CONVERT_SPACE_TO_MANAGED,    ConvertSpaceToManaged,      SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_FIXED_ARRAY_MARSHALER, CONVERT_CONTENTS_TO_MANAGED, ConvertContentsToManaged,   SM_IntPtr_RefObj_IntPtr_RetVoid)
DEFINE_METHOD(MNGD_FIXED_ARRAY_MARSHALER, CLEAR_NATIVE_CONTENTS,       ClearNativeContents,        SM_IntPtr_RefObj_IntPtr_RetVoid)

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

DEFINE_CLASS(HANDLE_MARSHALER,           StubHelpers,                 HandleMarshaler)
DEFINE_METHOD(HANDLE_MARSHALER,          CONVERT_SAFEHANDLE_TO_NATIVE,ConvertSafeHandleToNative,  SM_SafeHandle_RefCleanupWorkListElement_RetIntPtr)
DEFINE_METHOD(HANDLE_MARSHALER,          THROW_SAFEHANDLE_FIELD_CHANGED, ThrowSafeHandleFieldChanged, SM_RetVoid)
DEFINE_METHOD(HANDLE_MARSHALER,          THROW_CRITICALHANDLE_FIELD_CHANGED, ThrowCriticalHandleFieldChanged, SM_RetVoid)

DEFINE_CLASS(NATIVEVARIANT,         StubHelpers,            NativeVariant)
DEFINE_CLASS(NATIVEDECIMAL,         StubHelpers,            NativeDecimal)

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

DEFINE_CLASS(MODULEBASE,        Reflection,         Module)

#ifdef FEATURE_ICASTABLE
DEFINE_CLASS(ICASTABLE,         CompilerServices,   ICastable)

DEFINE_CLASS(ICASTABLEHELPERS,  CompilerServices,   ICastableHelpers)
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

// Classes referenced in Comparer<T>.Default optimization

DEFINE_CLASS(GENERIC_COMPARER, CollectionsGeneric, GenericComparer`1)
DEFINE_CLASS(OBJECT_COMPARER, CollectionsGeneric, ObjectComparer`1)
DEFINE_CLASS(ENUM_COMPARER, CollectionsGeneric, EnumComparer`1)
DEFINE_CLASS(NULLABLE_COMPARER, CollectionsGeneric, NullableComparer`1)

DEFINE_CLASS(INATTRIBUTE, Interop, InAttribute)

DEFINE_CLASS_U(CompilerServices,           GCHeapHash,                      GCHeapHashObject)
DEFINE_FIELD_U(_data,                      GCHeapHashObject,                _data)
DEFINE_FIELD_U(_count,                     GCHeapHashObject,                _count)
DEFINE_FIELD_U(_deletedCount,              GCHeapHashObject,                _deletedCount)

DEFINE_CLASS(GCHEAPHASH, CompilerServices, GCHeapHash)

DEFINE_CLASS(CASTHELPERS, CompilerServices, CastHelpers)
DEFINE_FIELD(CASTHELPERS, TABLE, s_table)
DEFINE_METHOD(CASTHELPERS, ISINSTANCEOFANY,  IsInstanceOfAny,             SM_PtrVoid_Obj_RetObj)
DEFINE_METHOD(CASTHELPERS, ISINSTANCEOFCLASS,IsInstanceOfClass,           SM_PtrVoid_Obj_RetObj)
DEFINE_METHOD(CASTHELPERS, ISINSTANCEOFINTERFACE,  IsInstanceOfInterface, SM_PtrVoid_Obj_RetObj)
DEFINE_METHOD(CASTHELPERS, CHKCASTANY,       ChkCastAny,                  SM_PtrVoid_Obj_RetObj)
DEFINE_METHOD(CASTHELPERS, CHKCASTINTERFACE, ChkCastInterface,            SM_PtrVoid_Obj_RetObj)
DEFINE_METHOD(CASTHELPERS, CHKCASTCLASS,     ChkCastClass,                SM_PtrVoid_Obj_RetObj)
DEFINE_METHOD(CASTHELPERS, CHKCASTCLASSSPECIAL, ChkCastClassSpecial,      SM_PtrVoid_Obj_RetObj)
DEFINE_METHOD(CASTHELPERS, UNBOX,            Unbox,                       SM_PtrVoid_Obj_RetRefByte)
DEFINE_METHOD(CASTHELPERS, STELEMREF,        StelemRef,                   SM_Array_Int_Obj_RetVoid)
DEFINE_METHOD(CASTHELPERS, LDELEMAREF,       LdelemaRef,                  SM_Array_Int_PtrVoid_RetRefObj)

DEFINE_CLASS_U(CompilerServices,           LAHashDependentHashTracker,      LAHashDependentHashTrackerObject)
DEFINE_FIELD_U(_dependentHandle,           LAHashDependentHashTrackerObject,_dependentHandle)
DEFINE_FIELD_U(_loaderAllocator,           LAHashDependentHashTrackerObject,_loaderAllocator)

DEFINE_CLASS(LAHASHDEPENDENTHASHTRACKER, CompilerServices, LAHashDependentHashTracker)
#if FOR_ILLINK
DEFINE_METHOD(LAHASHDEPENDENTHASHTRACKER,  CTOR,                            .ctor,        IM_RetVoid)
#endif

DEFINE_CLASS_U(CompilerServices,           LAHashKeyToTrackers,             LAHashKeyToTrackersObject)
DEFINE_FIELD_U(_trackerOrTrackerSet,       LAHashKeyToTrackersObject,       _trackerOrTrackerSet)
DEFINE_FIELD_U(_laLocalKeyValueStore,      LAHashKeyToTrackersObject,       _laLocalKeyValueStore)

DEFINE_CLASS(LAHASHKEYTOTRACKERS, CompilerServices, LAHashKeyToTrackers)

DEFINE_CLASS_U(System, GCMemoryInfoData, GCMemoryInfoData)
DEFINE_FIELD_U(_highMemoryLoadThresholdBytes, GCMemoryInfoData, highMemLoadThresholdBytes)
DEFINE_FIELD_U(_totalAvailableMemoryBytes, GCMemoryInfoData, totalAvailableMemoryBytes)
DEFINE_FIELD_U(_memoryLoadBytes, GCMemoryInfoData, lastRecordedMemLoadBytes)
DEFINE_FIELD_U(_heapSizeBytes, GCMemoryInfoData, lastRecordedHeapSizeBytes)
DEFINE_FIELD_U(_fragmentedBytes, GCMemoryInfoData, lastRecordedFragmentationBytes)
DEFINE_FIELD_U(_totalCommittedBytes, GCMemoryInfoData, totalCommittedBytes)
DEFINE_FIELD_U(_promotedBytes, GCMemoryInfoData, promotedBytes)
DEFINE_FIELD_U(_pinnedObjectsCount, GCMemoryInfoData, pinnedObjectCount)
DEFINE_FIELD_U(_finalizationPendingCount, GCMemoryInfoData, finalizationPendingCount)
DEFINE_FIELD_U(_index, GCMemoryInfoData, index)
DEFINE_FIELD_U(_generation, GCMemoryInfoData, generation)
DEFINE_FIELD_U(_pauseTimePercentage, GCMemoryInfoData, pauseTimePercent)
DEFINE_FIELD_U(_compacted, GCMemoryInfoData, isCompaction)
DEFINE_FIELD_U(_concurrent, GCMemoryInfoData, isConcurrent)
DEFINE_FIELD_U(_pauseDuration0, GCMemoryInfoData, pauseDuration0)
DEFINE_FIELD_U(_pauseDuration1, GCMemoryInfoData, pauseDuration1)
DEFINE_FIELD_U(_generationInfo0, GCMemoryInfoData, generationInfo0)
DEFINE_FIELD_U(_generationInfo1, GCMemoryInfoData, generationInfo1)
DEFINE_FIELD_U(_generationInfo2, GCMemoryInfoData, generationInfo2)
DEFINE_FIELD_U(_generationInfo3, GCMemoryInfoData, generationInfo3)
DEFINE_FIELD_U(_generationInfo4, GCMemoryInfoData, generationInfo4)


#undef DEFINE_CLASS
#undef DEFINE_METHOD
#undef DEFINE_FIELD
#undef DEFINE_CLASS_U
#undef DEFINE_FIELD_U
