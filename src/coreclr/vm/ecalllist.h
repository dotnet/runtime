// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ECallList.H
//
// This file contains definitions of FCall entrypoints
//




#ifndef FCFuncElement
#define FCFuncElement(name, impl)
#endif

#ifndef FCFuncElementSig
#define FCFuncElementSig(name,sig,impl)
#endif

#ifndef FCIntrinsic
#define FCIntrinsic(name,impl,intrinsicID)
#endif

#ifndef FCIntrinsicSig
#define FCIntrinsicSig(name,sig,impl,intrinsicID)
#endif

#ifndef QCFuncElement
#define QCFuncElement(name,impl)
#endif

#ifndef FCDynamic
#define FCDynamic(name,intrinsicID,dynamicID)
#endif

#ifndef FCDynamicSig
#define FCDynamicSig(name,sig,intrinsicID,dynamicID)
#endif

#ifndef FCUnreferenced
#define FCUnreferenced
#endif

#ifndef FCFuncStart
#define FCFuncStart(name)
#endif

#ifndef FCFuncEnd
#define FCFuncEnd()
#endif

#ifndef FCClassElement
#define FCClassElement(name,namespace,funcs)
#endif

//
//
// Entrypoint definitions
//
//



FCFuncStart(gDependentHandleFuncs)
    FCFuncElement("InternalInitialize",            DependentHandle::InternalInitialize)
    FCFuncElement("InternalGetTarget",             DependentHandle::InternalGetTarget)
    FCFuncElement("InternalGetDependent",          DependentHandle::InternalGetDependent)
    FCFuncElement("InternalGetTargetAndDependent", DependentHandle::InternalGetTargetAndDependent)
    FCFuncElement("InternalSetTargetToNull",       DependentHandle::InternalSetTargetToNull)
    FCFuncElement("InternalSetDependent",          DependentHandle::InternalSetDependent)
    FCFuncElement("InternalFree",                  DependentHandle::InternalFree)
FCFuncEnd()





FCFuncStart(gEnumFuncs)
    FCFuncElement("InternalGetUnderlyingType",  ReflectionEnum::InternalGetEnumUnderlyingType)
    FCFuncElement("InternalGetCorElementType",  ReflectionEnum::InternalGetCorElementType)
    QCFuncElement("GetEnumValuesAndNames",  ReflectionEnum::GetEnumValuesAndNames)
    FCFuncElement("InternalBoxEnum", ReflectionEnum::InternalBoxEnum)
    FCFuncElement("Equals", ReflectionEnum::InternalEquals)
    FCFuncElement("InternalHasFlag", ReflectionEnum::InternalHasFlag)
FCFuncEnd()


FCFuncStart(gSymWrapperCodePunkSafeHandleFuncs)
    FCFuncElement("nGetDReleaseTarget", COMPunkSafeHandle::nGetDReleaseTarget)
FCFuncEnd()


FCFuncStart(gObjectFuncs)
    FCIntrinsic("GetType", ObjectNative::GetClass, CORINFO_INTRINSIC_Object_GetType)
FCFuncEnd()

FCFuncStart(gStringFuncs)
    FCDynamic("FastAllocateString", CORINFO_INTRINSIC_Illegal, ECall::FastAllocateString)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_ArrChar_RetVoid, CORINFO_INTRINSIC_Illegal, ECall::CtorCharArrayManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_ArrChar_Int_Int_RetVoid, CORINFO_INTRINSIC_Illegal, ECall::CtorCharArrayStartLengthManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_PtrChar_RetVoid, CORINFO_INTRINSIC_Illegal, ECall::CtorCharPtrManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_PtrChar_Int_Int_RetVoid, CORINFO_INTRINSIC_Illegal, ECall::CtorCharPtrStartLengthManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_Char_Int_RetVoid, CORINFO_INTRINSIC_Illegal, ECall::CtorCharCountManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_ReadOnlySpanOfChar_RetVoid, CORINFO_INTRINSIC_Illegal, ECall::CtorReadOnlySpanOfCharManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_PtrSByt_RetVoid, CORINFO_INTRINSIC_Illegal, ECall::CtorSBytePtrManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_PtrSByt_Int_Int_RetVoid, CORINFO_INTRINSIC_Illegal, ECall::CtorSBytePtrStartLengthManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_PtrSByt_Int_Int_Encoding_RetVoid, CORINFO_INTRINSIC_Illegal, ECall::CtorSBytePtrStartLengthEncodingManaged)
    FCFuncElement("SetTrailByte", COMString::FCSetTrailByte)
    FCFuncElement("TryGetTrailByte", COMString::FCTryGetTrailByte)
    FCFuncElement("IsInterned", AppDomainNative::IsStringInterned)
    FCFuncElement("Intern", AppDomainNative::GetOrInternString)
FCFuncEnd()

FCFuncStart(gValueTypeFuncs)
    FCFuncElement("CanCompareBits", ValueTypeHelper::CanCompareBits)
    FCFuncElement("FastEqualsCheck", ValueTypeHelper::FastEqualsCheck)
    FCFuncElement("GetHashCode", ValueTypeHelper::GetHashCode)
    FCFuncElement("GetHashCodeOfPtr", ValueTypeHelper::GetHashCodeOfPtr)
FCFuncEnd()

FCFuncStart(gDiagnosticsDebugger)
    FCFuncElement("BreakInternal", DebugDebugger::Break)
    QCFuncElement("LaunchInternal", DebugDebugger::Launch)
    FCFuncElement("get_IsAttached", DebugDebugger::IsDebuggerAttached)
    QCFuncElement("LogInternal", DebugDebugger::Log)
    FCFuncElement("IsLogging", DebugDebugger::IsLogging)
    FCFuncElement("CustomNotification", DebugDebugger::CustomNotification)
FCFuncEnd()

FCFuncStart(gDiagnosticsStackTrace)
    FCFuncElement("GetStackFramesInternal", DebugStackTrace::GetStackFramesInternal)
FCFuncEnd()

FCFuncStart(gEnvironmentFuncs)
    FCFuncElement("get_CurrentManagedThreadId", JIT_GetCurrentManagedThreadId)
    FCFuncElement("get_TickCount", SystemNative::GetTickCount)
    FCFuncElement("get_TickCount64", SystemNative::GetTickCount64)
    QCFuncElement("_Exit", SystemNative::Exit)
    FCFuncElement("set_ExitCode", SystemNative::SetExitCode)
    FCFuncElement("get_ExitCode", SystemNative::GetExitCode)
    QCFuncElement("GetProcessorCount", SystemNative::GetProcessorCount)
    FCFuncElement("GetCommandLineArgsNative", SystemNative::GetCommandLineArgs)

    FCFuncElementSig("FailFast", &gsig_SM_Str_RetVoid, SystemNative::FailFast)
    FCFuncElementSig("FailFast", &gsig_SM_Str_Exception_RetVoid, SystemNative::FailFastWithException)
    FCFuncElementSig("FailFast", &gsig_SM_Str_Exception_Str_RetVoid, SystemNative::FailFastWithExceptionAndSource)
FCFuncEnd()

FCFuncStart(gExceptionFuncs)
    FCFuncElement("IsImmutableAgileException", ExceptionNative::IsImmutableAgileException)
    FCFuncElement("GetMethodFromStackTrace", SystemNative::GetMethodFromStackTrace)
    QCFuncElement("GetMessageFromNativeResources", ExceptionNative::GetMessageFromNativeResources)
    FCFuncElement("PrepareForForeignExceptionRaise", ExceptionNative::PrepareForForeignExceptionRaise)
    FCFuncElement("GetStackTracesDeepCopy", ExceptionNative::GetStackTracesDeepCopy)
    FCFuncElement("SaveStackTracesFromDeepCopy", ExceptionNative::SaveStackTracesFromDeepCopy)
    FCFuncElement("GetExceptionCount", ExceptionNative::GetExceptionCount)
FCFuncEnd()

FCFuncStart(gTypedReferenceFuncs)
    FCFuncElement("InternalToObject", ReflectionInvocation::TypedReferenceToObject)
    FCFuncElement("InternalMakeTypedReference", ReflectionInvocation::MakeTypedReference)
FCFuncEnd()

FCFuncStart(gSystem_Type)
    FCFuncElement("GetTypeFromHandle", RuntimeTypeHandle::GetTypeFromHandle)
    FCFuncElement("GetTypeFromHandleUnsafe", RuntimeTypeHandle::GetRuntimeType)
    FCFuncElement("op_Equality", RuntimeTypeHandle::TypeEQ)
    FCFuncElement("op_Inequality", RuntimeTypeHandle::TypeNEQ)
FCFuncEnd()

FCFuncStart(gSystem_RuntimeType)
    FCFuncElement("GetGUID", ReflectionInvocation::GetGUID)
    FCFuncElement("_CreateEnum", ReflectionInvocation::CreateEnum)
    FCFuncElement("CanValueSpecialCast", ReflectionInvocation::CanValueSpecialCast)
    FCFuncElement("AllocateValueType", ReflectionInvocation::AllocateValueType)
#if defined(FEATURE_COMINTEROP)
    FCFuncElement("InvokeDispMethod", ReflectionInvocation::InvokeDispMethod)
#endif // defined(FEATURE_COMINTEROP)
FCFuncEnd()

FCFuncStart(gCOMTypeHandleFuncs)
    QCFuncElement("CreateInstanceForAnotherGenericParameter", RuntimeTypeHandle::CreateInstanceForAnotherGenericParameter)
    QCFuncElement("GetGCHandle", RuntimeTypeHandle::GetGCHandle)
    QCFuncElement("FreeGCHandle", RuntimeTypeHandle::FreeGCHandle)

    FCFuncElement("IsInstanceOfType", RuntimeTypeHandle::IsInstanceOfType)
    FCFuncElement("GetDeclaringMethod", RuntimeTypeHandle::GetDeclaringMethod)
    FCFuncElement("GetDeclaringType", RuntimeTypeHandle::GetDeclaringType)
    QCFuncElement("MakePointer", RuntimeTypeHandle::MakePointer)
    QCFuncElement("MakeByRef", RuntimeTypeHandle::MakeByRef)
    QCFuncElement("MakeSZArray", RuntimeTypeHandle::MakeSZArray)
    QCFuncElement("MakeArray", RuntimeTypeHandle::MakeArray)
    QCFuncElement("IsCollectible", RuntimeTypeHandle::IsCollectible)
    FCFuncElement("GetFirstIntroducedMethod", RuntimeTypeHandle::GetFirstIntroducedMethod)
    FCFuncElement("GetNextIntroducedMethod", RuntimeTypeHandle::GetNextIntroducedMethod)
    FCFuncElement("GetCorElementType", RuntimeTypeHandle::GetCorElementType)
    FCFuncElement("GetAssembly", RuntimeTypeHandle::GetAssembly)
    FCFuncElement("GetModule", RuntimeTypeHandle::GetModule)
    FCFuncElement("GetBaseType", RuntimeTypeHandle::GetBaseType)
    FCFuncElement("GetElementType", RuntimeTypeHandle::GetElementType)
    FCFuncElement("GetArrayRank", RuntimeTypeHandle::GetArrayRank)
    FCFuncElement("GetToken", RuntimeTypeHandle::GetToken)
    FCFuncElement("_GetUtf8Name", RuntimeTypeHandle::GetUtf8Name)
    FCFuncElement("GetMethodAt", RuntimeTypeHandle::GetMethodAt)
    FCFuncElement("GetFields", RuntimeTypeHandle::GetFields)
    FCFuncElement("GetInterfaces", RuntimeTypeHandle::GetInterfaces)
    QCFuncElement("GetConstraints", RuntimeTypeHandle::GetConstraints)
    FCFuncElement("GetAttributes", RuntimeTypeHandle::GetAttributes)
    FCFuncElement("_GetMetadataImport", RuntimeTypeHandle::GetMetadataImport)
    FCFuncElement("GetNumVirtuals", RuntimeTypeHandle::GetNumVirtuals)
    FCFuncElement("GetNumVirtualsAndStaticVirtuals", RuntimeTypeHandle::GetNumVirtualsAndStaticVirtuals)
    QCFuncElement("VerifyInterfaceIsImplemented", RuntimeTypeHandle::VerifyInterfaceIsImplemented)
    QCFuncElement("GetInterfaceMethodImplementation", RuntimeTypeHandle::GetInterfaceMethodImplementation)
    FCFuncElement("IsValueType", RuntimeTypeHandle::IsValueType)
    FCFuncElement("IsInterface", RuntimeTypeHandle::IsInterface)
    FCFuncElement("IsByRefLike", RuntimeTypeHandle::IsByRefLike)
    QCFuncElement("_IsVisible", RuntimeTypeHandle::IsVisible)
    QCFuncElement("ConstructName", RuntimeTypeHandle::ConstructName)
    FCFuncElement("CanCastTo", RuntimeTypeHandle::CanCastTo)
    QCFuncElement("GetTypeByName", RuntimeTypeHandle::GetTypeByName)
    QCFuncElement("GetTypeByNameUsingCARules", RuntimeTypeHandle::GetTypeByNameUsingCARules)
    QCFuncElement("GetInstantiation", RuntimeTypeHandle::GetInstantiation)
    QCFuncElement("Instantiate", RuntimeTypeHandle::Instantiate)
    QCFuncElement("GetGenericTypeDefinition", RuntimeTypeHandle::GetGenericTypeDefinition)
    FCFuncElement("HasInstantiation", RuntimeTypeHandle::HasInstantiation)
    FCFuncElement("GetGenericVariableIndex", RuntimeTypeHandle::GetGenericVariableIndex)
    FCFuncElement("IsGenericVariable", RuntimeTypeHandle::IsGenericVariable)
    FCFuncElement("IsGenericTypeDefinition", RuntimeTypeHandle::IsGenericTypeDefinition)
    FCFuncElement("ContainsGenericVariables", RuntimeTypeHandle::ContainsGenericVariables)
    FCFuncElement("SatisfiesConstraints", RuntimeTypeHandle::SatisfiesConstraints)
    QCFuncElement("GetActivationInfo", RuntimeTypeHandle::GetActivationInfo)
#ifdef FEATURE_COMINTEROP
    FCFuncElement("AllocateComObject", RuntimeTypeHandle::AllocateComObject)
#endif // FEATURE_COMINTEROP
    FCFuncElement("CompareCanonicalHandles", RuntimeTypeHandle::CompareCanonicalHandles)
    FCIntrinsic("GetValueInternal", RuntimeTypeHandle::GetValueInternal, CORINFO_INTRINSIC_RTH_GetValueInternal)
    FCFuncElement("IsEquivalentTo", RuntimeTypeHandle::IsEquivalentTo)
FCFuncEnd()

FCFuncStart(gMetaDataImport)
    FCFuncElement("_GetDefaultValue", MetaDataImport::GetDefaultValue)
    FCFuncElement("_GetName", MetaDataImport::GetName)
    FCFuncElement("_GetUserString", MetaDataImport::GetUserString)
    FCFuncElement("_GetScopeProps", MetaDataImport::GetScopeProps)
    FCFuncElement("_GetClassLayout", MetaDataImport::GetClassLayout)
    FCFuncElement("_GetSignatureFromToken", MetaDataImport::GetSignatureFromToken)
    FCFuncElement("_GetNamespace", MetaDataImport::GetNamespace)
    FCFuncElement("_GetEventProps", MetaDataImport::GetEventProps)
    FCFuncElement("_GetFieldDefProps", MetaDataImport::GetFieldDefProps)
    FCFuncElement("_GetPropertyProps", MetaDataImport::GetPropertyProps)
    FCFuncElement("_GetParentToken", MetaDataImport::GetParentToken)
    FCFuncElement("_GetParamDefProps", MetaDataImport::GetParamDefProps)
    FCFuncElement("_GetGenericParamProps", MetaDataImport::GetGenericParamProps)

    FCFuncElement("_Enum", MetaDataImport::Enum)
    FCFuncElement("_GetMemberRefProps", MetaDataImport::GetMemberRefProps)
    FCFuncElement("_GetCustomAttributeProps", MetaDataImport::GetCustomAttributeProps)
    FCFuncElement("_GetFieldOffset", MetaDataImport::GetFieldOffset)

    FCFuncElement("_GetSigOfFieldDef", MetaDataImport::GetSigOfFieldDef)
    FCFuncElement("_GetSigOfMethodDef", MetaDataImport::GetSigOfMethodDef)
    FCFuncElement("_GetFieldMarshal", MetaDataImport::GetFieldMarshal)
    FCFuncElement("_GetPInvokeMap", MetaDataImport::GetPinvokeMap)
    FCFuncElement("_IsValidToken", MetaDataImport::IsValidToken)
    FCFuncElement("_GetMarshalAs", MetaDataImport::GetMarshalAs)
FCFuncEnd()

FCFuncStart(gSignatureNative)
    FCFuncElement("GetSignature", SignatureNative::GetSignature)
    FCFuncElement("GetCustomModifiers", SignatureNative::GetCustomModifiers)
    FCFuncElement("CompareSig", SignatureNative::CompareSig)
FCFuncEnd()

FCFuncStart(gRuntimeMethodHandle)
    QCFuncElement("ConstructInstantiation", RuntimeMethodHandle::ConstructInstantiation)
    FCFuncElement("_GetCurrentMethod", RuntimeMethodHandle::GetCurrentMethod)
    FCFuncElement("InvokeMethod", RuntimeMethodHandle::InvokeMethod)
    QCFuncElement("GetFunctionPointer", RuntimeMethodHandle::GetFunctionPointer)
    QCFuncElement("GetIsCollectible", RuntimeMethodHandle::GetIsCollectible)
    FCFuncElement("GetImplAttributes", RuntimeMethodHandle::GetImplAttributes)
    FCFuncElement("GetAttributes", RuntimeMethodHandle::GetAttributes)
    FCFuncElement("GetDeclaringType", RuntimeMethodHandle::GetDeclaringType)
    FCFuncElement("GetSlot", RuntimeMethodHandle::GetSlot)
    FCFuncElement("GetMethodDef", RuntimeMethodHandle::GetMethodDef)
    FCFuncElement("GetName", RuntimeMethodHandle::GetName)
    FCFuncElement("_GetUtf8Name", RuntimeMethodHandle::GetUtf8Name)
    FCFuncElement("MatchesNameHash", RuntimeMethodHandle::MatchesNameHash)
    QCFuncElement("GetMethodInstantiation", RuntimeMethodHandle::GetMethodInstantiation)
    FCFuncElement("HasMethodInstantiation", RuntimeMethodHandle::HasMethodInstantiation)
    FCFuncElement("IsGenericMethodDefinition", RuntimeMethodHandle::IsGenericMethodDefinition)
    FCFuncElement("GetGenericParameterCount", RuntimeMethodHandle::GetGenericParameterCount)
    FCFuncElement("IsTypicalMethodDefinition", RuntimeMethodHandle::IsTypicalMethodDefinition)
    QCFuncElement("GetTypicalMethodDefinition", RuntimeMethodHandle::GetTypicalMethodDefinition)
    QCFuncElement("StripMethodInstantiation", RuntimeMethodHandle::StripMethodInstantiation)
    FCFuncElement("GetStubIfNeeded", RuntimeMethodHandle::GetStubIfNeeded)
    FCFuncElement("GetMethodFromCanonical", RuntimeMethodHandle::GetMethodFromCanonical)
    FCFuncElement("IsDynamicMethod", RuntimeMethodHandle::IsDynamicMethod)
    FCFuncElement("GetMethodBody", RuntimeMethodHandle::GetMethodBody)
    QCFuncElement("IsCAVisibleFromDecoratedType", RuntimeMethodHandle::IsCAVisibleFromDecoratedType)
    FCFuncElement("IsConstructor", RuntimeMethodHandle::IsConstructor)
    QCFuncElement("Destroy", RuntimeMethodHandle::Destroy)
    FCFuncElement("GetResolver", RuntimeMethodHandle::GetResolver)
    FCFuncElement("GetLoaderAllocator", RuntimeMethodHandle::GetLoaderAllocator)
FCFuncEnd()


FCFuncStart(gCOMFieldHandleNewFuncs)
    FCFuncElement("GetValue", RuntimeFieldHandle::GetValue)
    FCFuncElement("SetValue", RuntimeFieldHandle::SetValue)
    FCFuncElement("GetValueDirect", RuntimeFieldHandle::GetValueDirect)
    FCFuncElement("SetValueDirect", RuntimeFieldHandle::SetValueDirect)
    FCFuncElement("GetName", RuntimeFieldHandle::GetName)
    FCFuncElement("_GetUtf8Name", RuntimeFieldHandle::GetUtf8Name)
    FCFuncElement("MatchesNameHash", RuntimeFieldHandle::MatchesNameHash)
    FCFuncElement("GetAttributes", RuntimeFieldHandle::GetAttributes)
    FCFuncElement("GetApproxDeclaringType", RuntimeFieldHandle::GetApproxDeclaringType)
    FCFuncElement("GetToken", RuntimeFieldHandle::GetToken)
    FCFuncElement("GetStaticFieldForGenericType", RuntimeFieldHandle::GetStaticFieldForGenericType)
    FCFuncElement("AcquiresContextFromThis", RuntimeFieldHandle::AcquiresContextFromThis)
FCFuncEnd()


FCFuncStart(gCOMModuleFuncs)
    QCFuncElement("GetType", COMModule::GetType)
    QCFuncElement("GetScopeName", COMModule::GetScopeName)
    FCFuncElement("GetTypes", COMModule::GetTypes)
    QCFuncElement("GetFullyQualifiedName", COMModule::GetFullyQualifiedName)
    FCFuncElement("IsResource", COMModule::IsResource)
FCFuncEnd()

FCFuncStart(gCOMModuleBuilderFuncs)
    QCFuncElement("GetStringConstant", COMModule::GetStringConstant)
    QCFuncElement("GetTypeRef", COMModule::GetTypeRef)
    QCFuncElement("GetTokenFromTypeSpec", COMModule::GetTokenFromTypeSpec)
    QCFuncElement("GetMemberRef", COMModule::GetMemberRef)
    QCFuncElement("GetMemberRefOfMethodInfo", COMModule::GetMemberRefOfMethodInfo)
    QCFuncElement("GetMemberRefOfFieldInfo", COMModule::GetMemberRefOfFieldInfo)
    QCFuncElement("GetMemberRefFromSignature", COMModule::GetMemberRefFromSignature)
    QCFuncElement("GetArrayMethodToken", COMModule::GetArrayMethodToken)
    QCFuncElement("SetFieldRVAContent", COMModule::SetFieldRVAContent)
FCFuncEnd()

FCFuncStart(gCOMModuleHandleFuncs)
    FCFuncElement("GetToken", ModuleHandle::GetToken)
    QCFuncElement("GetModuleType", ModuleHandle::GetModuleType)
    FCFuncElement("GetDynamicMethod", ModuleHandle::GetDynamicMethod)
    FCFuncElement("_GetMetadataImport", ModuleHandle::GetMetadataImport)
    QCFuncElement("ResolveType", ModuleHandle::ResolveType)
    QCFuncElement("ResolveMethod", ModuleHandle::ResolveMethod)
    QCFuncElement("_ContainsPropertyMatchingHash", ModuleHandle::ContainsPropertyMatchingHash)
    QCFuncElement("ResolveField", ModuleHandle::ResolveField)
    QCFuncElement("GetPEKind", ModuleHandle::GetPEKind)
    FCFuncElement("GetMDStreamVersion", ModuleHandle::GetMDStreamVersion)
FCFuncEnd()

FCFuncStart(gCustomAttributeEncodedArgument)
    FCFuncElement("ParseAttributeArguments", Attribute::ParseAttributeArguments)
FCFuncEnd()

FCFuncStart(gCOMCustomAttributeFuncs)
    FCFuncElement("_ParseAttributeUsageAttribute", COMCustomAttribute::ParseAttributeUsageAttribute)
    FCFuncElement("_CreateCaObject", COMCustomAttribute::CreateCaObject)
    FCFuncElement("_GetPropertyOrFieldData",  COMCustomAttribute::GetPropertyOrFieldData)
FCFuncEnd()

FCFuncStart(gCOMClassWriter)
    QCFuncElement("DefineGenericParam", COMDynamicWrite::DefineGenericParam)
    QCFuncElement("DefineType", COMDynamicWrite::DefineType)
    QCFuncElement("SetParentType", COMDynamicWrite::SetParentType)
    QCFuncElement("AddInterfaceImpl", COMDynamicWrite::AddInterfaceImpl)
    QCFuncElement("DefineMethod", COMDynamicWrite::DefineMethod)
    QCFuncElement("DefineMethodSpec", COMDynamicWrite::DefineMethodSpec)
    QCFuncElement("SetMethodIL", COMDynamicWrite::SetMethodIL)
    QCFuncElement("TermCreateClass", COMDynamicWrite::TermCreateClass)
    QCFuncElement("DefineField", COMDynamicWrite::DefineField)
    QCFuncElement("DefineProperty", COMDynamicWrite::DefineProperty)
    QCFuncElement("DefineEvent", COMDynamicWrite::DefineEvent)
    QCFuncElement("DefineMethodSemantics", COMDynamicWrite::DefineMethodSemantics)
    QCFuncElement("SetMethodImpl", COMDynamicWrite::SetMethodImpl)
    QCFuncElement("DefineMethodImpl", COMDynamicWrite::DefineMethodImpl)
    QCFuncElement("GetTokenFromSig",  COMDynamicWrite::GetTokenFromSig)
    QCFuncElement("SetFieldLayoutOffset", COMDynamicWrite::SetFieldLayoutOffset)
    QCFuncElement("SetClassLayout", COMDynamicWrite::SetClassLayout)
    QCFuncElement("SetParamInfo", COMDynamicWrite::SetParamInfo)
    QCFuncElement("SetPInvokeData", COMDynamicWrite::SetPInvokeData)
    QCFuncElement("SetConstantValue", COMDynamicWrite::SetConstantValue)
    QCFuncElement("DefineCustomAttribute", COMDynamicWrite::DefineCustomAttribute)
FCFuncEnd()

FCFuncStart(gCompatibilitySwitchFuncs)
    FCFuncElement("GetValueInternal", CompatibilitySwitch::GetValue)
FCFuncEnd()

FCFuncStart(gMdUtf8String)
    QCFuncElement("EqualsCaseInsensitive", MdUtf8String::EqualsCaseInsensitive)
    QCFuncElement("HashCaseInsensitive", MdUtf8String::HashCaseInsensitive)
FCFuncEnd()

FCFuncStart(gSafeTypeNameParserHandle)
    QCFuncElement("_ReleaseTypeNameParser", TypeName::QReleaseTypeNameParser)
FCFuncEnd()

FCFuncStart(gTypeNameParser)
    QCFuncElement("_CreateTypeNameParser",  TypeName::QCreateTypeNameParser)
    QCFuncElement("_GetNames",              TypeName::QGetNames)
    QCFuncElement("_GetTypeArguments",      TypeName::QGetTypeArguments)
    QCFuncElement("_GetModifiers",          TypeName::QGetModifiers)
    QCFuncElement("_GetAssemblyName",       TypeName::QGetAssemblyName)
FCFuncEnd()

FCFuncStart(gRuntimeAssemblyFuncs)
    QCFuncElement("GetFullName", AssemblyNative::GetFullName)
    QCFuncElement("GetLocation", AssemblyNative::GetLocation)
    QCFuncElement("GetResource", AssemblyNative::GetResource)
    QCFuncElement("GetCodeBase", AssemblyNative::GetCodeBase)
    QCFuncElement("GetFlags", AssemblyNative::GetFlags)
    QCFuncElement("GetHashAlgorithm", AssemblyNative::GetHashAlgorithm)
    QCFuncElement("GetLocale", AssemblyNative::GetLocale)
    QCFuncElement("GetPublicKey", AssemblyNative::GetPublicKey)
    QCFuncElement("GetSimpleName", AssemblyNative::GetSimpleName)
    QCFuncElement("GetVersion", AssemblyNative::GetVersion)
    FCFuncElement("FCallIsDynamic", AssemblyNative::IsDynamic)
    QCFuncElement("InternalLoad", AssemblyNative::InternalLoad)
    QCFuncElement("GetType", AssemblyNative::GetType)
    QCFuncElement("GetForwardedType", AssemblyNative::GetForwardedType)
    QCFuncElement("GetManifestResourceInfo", AssemblyNative::GetManifestResourceInfo)
    QCFuncElement("GetModules", AssemblyNative::GetModules)
    QCFuncElement("GetModule", AssemblyNative::GetModule)
    FCFuncElement("GetReferencedAssemblies", AssemblyNative::GetReferencedAssemblies)
    QCFuncElement("GetExportedTypes", AssemblyNative::GetExportedTypes)
    FCFuncElement("GetManifestResourceNames", AssemblyNative::GetManifestResourceNames)
    QCFuncElement("GetEntryPoint", AssemblyNative::GetEntryPoint)
    QCFuncElement("GetImageRuntimeVersion", AssemblyNative::GetImageRuntimeVersion)
    FCFuncElement("GetManifestModule", AssemblyHandle::GetManifestModule)
    FCFuncElement("GetToken", AssemblyHandle::GetToken)
    QCFuncElement("GetIsCollectible", AssemblyNative::GetIsCollectible)
FCFuncEnd()

FCFuncStart(gAssemblyExtensionsFuncs)
    QCFuncElement("InternalTryGetRawMetadata", AssemblyNative::InternalTryGetRawMetadata)
FCFuncEnd()

FCFuncStart(gMetadataUpdaterFuncs)
    QCFuncElement("ApplyUpdate", AssemblyNative::ApplyUpdate)
    QCFuncElement("IsApplyUpdateSupported", AssemblyNative::IsApplyUpdateSupported)
FCFuncEnd()

FCFuncStart(gAssemblyLoadContextFuncs)
    QCFuncElement("InitializeAssemblyLoadContext", AssemblyNative::InitializeAssemblyLoadContext)
    QCFuncElement("PrepareForAssemblyLoadContextRelease", AssemblyNative::PrepareForAssemblyLoadContextRelease)
    QCFuncElement("LoadFromPath", AssemblyNative::LoadFromPath)
    QCFuncElement("LoadFromStream", AssemblyNative::LoadFromStream)
#ifndef TARGET_UNIX
    QCFuncElement("LoadFromInMemoryModuleInternal", AssemblyNative::LoadFromInMemoryModule)
#endif
    QCFuncElement("GetLoadContextForAssembly", AssemblyNative::GetLoadContextForAssembly)
    FCFuncElement("GetLoadedAssemblies", AppDomainNative::GetLoadedAssemblies)
#if defined(FEATURE_MULTICOREJIT)
    QCFuncElement("InternalSetProfileRoot", MultiCoreJITNative::InternalSetProfileRoot)
    QCFuncElement("InternalStartProfile",   MultiCoreJITNative::InternalStartProfile)
#endif // defined(FEATURE_MULTICOREJIT)
    FCFuncElement("IsTracingEnabled", AssemblyNative::IsTracingEnabled)
    QCFuncElement("TraceResolvingHandlerInvoked", AssemblyNative::TraceResolvingHandlerInvoked)
    QCFuncElement("TraceAssemblyResolveHandlerInvoked", AssemblyNative::TraceAssemblyResolveHandlerInvoked)
    QCFuncElement("TraceAssemblyLoadFromResolveHandlerInvoked", AssemblyNative::TraceAssemblyLoadFromResolveHandlerInvoked)
    QCFuncElement("TraceSatelliteSubdirectoryPathProbed", AssemblyNative::TraceSatelliteSubdirectoryPathProbed)
FCFuncEnd()

FCFuncStart(gAssemblyNameFuncs)
    FCFuncElement("nInit", AssemblyNameNative::Init)
    FCFuncElement("ComputePublicKeyToken", AssemblyNameNative::GetPublicKeyToken)
    FCFuncElement("nGetFileInformation", AssemblyNameNative::GetFileInformation)
FCFuncEnd()

FCFuncStart(gLoaderAllocatorFuncs)
    QCFuncElement("Destroy", LoaderAllocator::Destroy)
FCFuncEnd()

FCFuncStart(gAssemblyFuncs)
    QCFuncElement("GetEntryAssemblyNative", AssemblyNative::GetEntryAssembly)
    QCFuncElement("GetExecutingAssemblyNative", AssemblyNative::GetExecutingAssembly)
    FCFuncElement("GetAssemblyCount", AssemblyNative::GetAssemblyCount)
FCFuncEnd()

FCFuncStart(gAssemblyBuilderFuncs)
    QCFuncElement("CreateDynamicAssembly", AppDomainNative::CreateDynamicAssembly)
    FCFuncElement("GetInMemoryAssemblyModule", AssemblyNative::GetInMemoryAssemblyModule)
FCFuncEnd()

FCFuncStart(gDelegateFuncs)
    FCFuncElement("BindToMethodName", COMDelegate::BindToMethodName)
    FCFuncElement("BindToMethodInfo", COMDelegate::BindToMethodInfo)
    FCFuncElement("GetMulticastInvoke", COMDelegate::GetMulticastInvoke)
    FCFuncElement("GetInvokeMethod", COMDelegate::GetInvokeMethod)
    FCFuncElement("InternalAlloc", COMDelegate::InternalAlloc)
    FCFuncElement("InternalAllocLike", COMDelegate::InternalAllocLike)
    FCFuncElement("InternalEqualTypes", COMDelegate::InternalEqualTypes)
    FCFuncElement("InternalEqualMethodHandles", COMDelegate::InternalEqualMethodHandles)
    FCFuncElement("FindMethodHandle", COMDelegate::FindMethodHandle)
    FCFuncElement("AdjustTarget", COMDelegate::AdjustTarget)
    FCFuncElement("GetCallStub", COMDelegate::GetCallStub)
    FCFuncElement("CompareUnmanagedFunctionPtrs", COMDelegate::CompareUnmanagedFunctionPtrs)

    // The FCall mechanism knows how to wire multiple different constructor calls into a
    // single entrypoint, without the following entry.  But we need this entry to satisfy
    // frame creation within the body:
    FCFuncElement("DelegateConstruct", COMDelegate::DelegateConstruct)
FCFuncEnd()

FCFuncStart(gMathFuncs)
    FCFuncElementSig("Abs", &gsig_SM_Dbl_RetDbl, COMDouble::Abs)
    FCFuncElementSig("Abs", &gsig_SM_Flt_RetFlt, COMSingle::Abs)
    FCFuncElement("Acos", COMDouble::Acos)
    FCFuncElement("Acosh", COMDouble::Acosh)
    FCFuncElement("Asin", COMDouble::Asin)
    FCFuncElement("Asinh", COMDouble::Asinh)
    FCFuncElement("Atan", COMDouble::Atan)
    FCFuncElement("Atanh", COMDouble::Atanh)
    FCFuncElement("Atan2", COMDouble::Atan2)
    FCFuncElement("Cbrt", COMDouble::Cbrt)
    FCFuncElement("Ceiling", COMDouble::Ceil)
    FCFuncElement("Cos", COMDouble::Cos)
    FCFuncElement("Cosh", COMDouble::Cosh)
    FCFuncElement("Exp", COMDouble::Exp)
    FCFuncElement("Floor", COMDouble::Floor)
    FCFuncElement("FMod", COMDouble::FMod)
    FCFuncElement("FusedMultiplyAdd", COMDouble::FusedMultiplyAdd)
    FCFuncElement("ILogB", COMDouble::ILogB)
    FCFuncElement("Log", COMDouble::Log)
    FCFuncElement("Log2", COMDouble::Log2)
    FCFuncElement("Log10", COMDouble::Log10)
    FCFuncElement("ModF", COMDouble::ModF)
    FCFuncElement("Pow", COMDouble::Pow)
    FCFuncElement("Sin", COMDouble::Sin)
    FCFuncElement("SinCos", COMDouble::SinCos)
    FCFuncElement("Sinh", COMDouble::Sinh)
    FCFuncElement("Sqrt", COMDouble::Sqrt)
    FCFuncElement("Tan", COMDouble::Tan)
    FCFuncElement("Tanh", COMDouble::Tanh)
FCFuncEnd()

FCFuncStart(gMathFFuncs)
    FCFuncElement("Acos", COMSingle::Acos)
    FCFuncElement("Acosh", COMSingle::Acosh)
    FCFuncElement("Asin", COMSingle::Asin)
    FCFuncElement("Asinh", COMSingle::Asinh)
    FCFuncElement("Atan", COMSingle::Atan)
    FCFuncElement("Atanh", COMSingle::Atanh)
    FCFuncElement("Atan2", COMSingle::Atan2)
    FCFuncElement("Cbrt", COMSingle::Cbrt)
    FCFuncElement("Ceiling", COMSingle::Ceil)
    FCFuncElement("Cos", COMSingle::Cos)
    FCFuncElement("Cosh", COMSingle::Cosh)
    FCFuncElement("Exp", COMSingle::Exp)
    FCFuncElement("Floor", COMSingle::Floor)
    FCFuncElement("FMod", COMSingle::FMod)
    FCFuncElement("FusedMultiplyAdd", COMSingle::FusedMultiplyAdd)
    FCFuncElement("ILogB", COMSingle::ILogB)
    FCFuncElement("Log", COMSingle::Log)
    FCFuncElement("Log2", COMSingle::Log2)
    FCFuncElement("Log10", COMSingle::Log10)
    FCFuncElement("ModF", COMSingle::ModF)
    FCFuncElement("Pow", COMSingle::Pow)
    FCFuncElement("Sin", COMSingle::Sin)
    FCFuncElement("SinCos", COMSingle::SinCos)
    FCFuncElement("Sinh", COMSingle::Sinh)
    FCFuncElement("Sqrt", COMSingle::Sqrt)
    FCFuncElement("Tan", COMSingle::Tan)
    FCFuncElement("Tanh", COMSingle::Tanh)
FCFuncEnd()

FCFuncStart(gThreadFuncs)
    FCFuncElement("InternalGetCurrentThread", GetThread)
    QCFuncElement("StartInternal", ThreadNative::Start)
#undef Sleep
    FCFuncElement("SleepInternal", ThreadNative::Sleep)
#define Sleep(a) Dont_Use_Sleep(a)
    QCFuncElement("UninterruptibleSleep0", ThreadNative::UninterruptibleSleep0)
    FCFuncElement("Initialize", ThreadNative::Initialize)
    QCFuncElement("InformThreadNameChange", ThreadNative::InformThreadNameChange)
    FCFuncElement("SpinWaitInternal", ThreadNative::SpinWait)
    QCFuncElement("YieldInternal", ThreadNative::YieldThread)
    FCFuncElement("GetCurrentThreadNative", ThreadNative::GetCurrentThread)
    FCFuncElement("get_ManagedThreadId", ThreadNative::GetManagedThreadId)
    FCFuncElement("InternalFinalize", ThreadNative::Finalize)
    FCFuncElement("get_IsAlive", ThreadNative::IsAlive)
    FCFuncElement("IsBackgroundNative", ThreadNative::IsBackground)
    FCFuncElement("SetBackgroundNative", ThreadNative::SetBackground)
    FCFuncElement("get_IsThreadPoolThread", ThreadNative::IsThreadpoolThread)
    FCFuncElement("set_IsThreadPoolThread", ThreadNative::SetIsThreadpoolThread)
    FCFuncElement("GetPriorityNative", ThreadNative::GetPriority)
    FCFuncElement("SetPriorityNative", ThreadNative::SetPriority)
    QCFuncElement("GetCurrentOSThreadId", ThreadNative::GetCurrentOSThreadId)
    FCFuncElement("GetThreadStateNative", ThreadNative::GetThreadState)
#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    FCFuncElement("GetApartmentStateNative", ThreadNative::GetApartmentState)
    FCFuncElement("SetApartmentStateNative", ThreadNative::SetApartmentState)
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT
#ifdef FEATURE_COMINTEROP
    FCFuncElement("DisableComObjectEagerCleanup", ThreadNative::DisableComObjectEagerCleanup)
#endif // FEATURE_COMINTEROP
    FCFuncElement("Interrupt", ThreadNative::Interrupt)
    FCFuncElement("Join", ThreadNative::Join)
    FCFuncElement("get_OptimalMaxSpinWaitsPerSpinIteration", ThreadNative::GetOptimalMaxSpinWaitsPerSpinIteration)
    FCFuncElement("GetCurrentProcessorNumber", ThreadNative::GetCurrentProcessorNumber)
FCFuncEnd()

FCFuncStart(gThreadPoolFuncs)
    FCFuncElement("GetNextConfigUInt32Value", ThreadPoolNative::GetNextConfigUInt32Value)
    FCFuncElement("PostQueuedCompletionStatus", ThreadPoolNative::CorPostQueuedCompletionStatus)
    FCFuncElement("GetAvailableThreadsNative", ThreadPoolNative::CorGetAvailableThreads)
    FCFuncElement("CanSetMinIOCompletionThreads", ThreadPoolNative::CorCanSetMinIOCompletionThreads)
    FCFuncElement("CanSetMaxIOCompletionThreads", ThreadPoolNative::CorCanSetMaxIOCompletionThreads)
    FCFuncElement("SetMinThreadsNative", ThreadPoolNative::CorSetMinThreads)
    FCFuncElement("GetMinThreadsNative", ThreadPoolNative::CorGetMinThreads)
    FCFuncElement("GetThreadCount", ThreadPoolNative::GetThreadCount)
    QCFuncElement("GetCompletedWorkItemCount", ThreadPoolNative::GetCompletedWorkItemCount)
    FCFuncElement("GetPendingUnmanagedWorkItemCount", ThreadPoolNative::GetPendingUnmanagedWorkItemCount)
    FCFuncElement("RegisterWaitForSingleObjectNative", ThreadPoolNative::CorRegisterWaitForSingleObject)
#ifdef TARGET_WINDOWS // the IO completion thread pool is currently only available on Windows
    FCFuncElement("QueueWaitCompletionNative", ThreadPoolNative::CorQueueWaitCompletion)
#endif
    FCFuncElement("BindIOCompletionCallbackNative", ThreadPoolNative::CorBindIoCompletionCallback)
    FCFuncElement("SetMaxThreadsNative", ThreadPoolNative::CorSetMaxThreads)
    FCFuncElement("GetMaxThreadsNative", ThreadPoolNative::CorGetMaxThreads)
    FCFuncElement("NotifyWorkItemCompleteNative", ThreadPoolNative::NotifyRequestComplete)
    FCFuncElement("NotifyWorkItemProgressNative", ThreadPoolNative::NotifyRequestProgress)
    FCFuncElement("GetEnableWorkerTrackingNative", ThreadPoolNative::GetEnableWorkerTracking)
    FCFuncElement("ReportThreadStatusNative", ThreadPoolNative::ReportThreadStatus)
    QCFuncElement("RequestWorkerThreadNative", ThreadPoolNative::RequestWorkerThread)
    QCFuncElement("PerformRuntimeSpecificGateActivitiesNative", ThreadPoolNative::PerformGateActivities)
FCFuncEnd()

FCFuncStart(gTimerFuncs)
    QCFuncElement("CreateAppDomainTimer", AppDomainTimerNative::CreateAppDomainTimer)
    QCFuncElement("ChangeAppDomainTimer", AppDomainTimerNative::ChangeAppDomainTimer)
    QCFuncElement("DeleteAppDomainTimer", AppDomainTimerNative::DeleteAppDomainTimer)
FCFuncEnd()

FCFuncStart(gRegisteredWaitHandleFuncs)
    FCFuncElement("UnregisterWaitNative", ThreadPoolNative::CorUnregisterWait)
    FCFuncElement("WaitHandleCleanupNative", ThreadPoolNative::CorWaitHandleCleanupNative)
FCFuncEnd()

FCFuncStart(gUnmanagedThreadPoolWorkItemFuncs)
    QCFuncElement("ExecuteUnmanagedThreadPoolWorkItem", ThreadPoolNative::ExecuteUnmanagedThreadPoolWorkItem)
FCFuncEnd()

FCFuncStart(gWaitHandleFuncs)
    FCFuncElement("WaitOneCore", WaitHandleNative::CorWaitOneNative)
    FCFuncElement("WaitMultipleIgnoringSyncContext", WaitHandleNative::CorWaitMultipleNative)
    FCFuncElement("SignalAndWaitNative", WaitHandleNative::CorSignalAndWaitOneNative)
FCFuncEnd()

#ifdef TARGET_UNIX
FCFuncStart(gLowLevelLifoSemaphoreFuncs)
    QCFuncElement("WaitNative", WaitHandleNative::CorWaitOnePrioritizedNative)
FCFuncEnd()
#endif

#ifdef FEATURE_COMINTEROP
FCFuncStart(gVariantFuncs)
    FCFuncElement("SetFieldsObject", COMVariant::SetFieldsObject)
    FCFuncElement("BoxEnum", COMVariant::BoxEnum)
FCFuncEnd()
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMINTEROP
FCFuncStart(gOAVariantFuncs)
    FCFuncElement("ChangeTypeEx", COMOAVariant::ChangeTypeEx)
FCFuncEnd()
#endif // FEATURE_COMINTEROP

FCFuncStart(gClrConfig)
    QCFuncElement("GetConfigBoolValue", ClrConfigNative::GetConfigBoolValue)
FCFuncEnd()

FCFuncStart(gCastHelpers)
    FCFuncElement("IsInstanceOfAny_NoCacheLookup", ::IsInstanceOfAny_NoCacheLookup)
    FCFuncElement("ChkCastAny_NoCacheLookup", ::ChkCastAny_NoCacheLookup)
    FCFuncElement("Unbox_Helper", ::Unbox_Helper)
    FCFuncElement("WriteBarrier", ::WriteBarrier_Helper)
FCFuncEnd()

FCFuncStart(gArrayFuncs)
    FCFuncElement("GetCorElementTypeOfElementType", ArrayNative::GetCorElementTypeOfElementType)
    FCFuncElement("Initialize", ArrayNative::Initialize)
    FCFuncElement("IsSimpleCopy", ArrayNative::IsSimpleCopy)
    FCFuncElement("CopySlow", ArrayNative::CopySlow)
    FCFuncElement("InternalCreate", ArrayNative::CreateInstance)
    FCFuncElement("InternalGetValue", ArrayNative::GetValue)
    FCFuncElement("InternalSetValue", ArrayNative::SetValue)
FCFuncEnd()

FCFuncStart(gBufferFuncs)
    QCFuncElement("__ZeroMemory", Buffer::Clear)
    FCFuncElement("__BulkMoveWithWriteBarrier", Buffer::BulkMoveWithWriteBarrier)
    QCFuncElement("__Memmove", Buffer::MemMove)
FCFuncEnd()

FCFuncStart(gGCInterfaceFuncs)
    FCFuncElement("GetGenerationWR", GCInterface::GetGenerationWR)
    FCFuncElement("_RegisterForFullGCNotification", GCInterface::RegisterForFullGCNotification)
    FCFuncElement("_CancelFullGCNotification", GCInterface::CancelFullGCNotification)
    FCFuncElement("_WaitForFullGCApproach", GCInterface::WaitForFullGCApproach)
    FCFuncElement("_WaitForFullGCComplete", GCInterface::WaitForFullGCComplete)
    FCFuncElement("_CollectionCount", GCInterface::CollectionCount)
    FCFuncElement("GetMemoryInfo", GCInterface::GetMemoryInfo)
    FCFuncElement("GetMemoryLoad", GCInterface::GetMemoryLoad)
    QCFuncElement("_StartNoGCRegion", GCInterface::StartNoGCRegion)
    QCFuncElement("_EndNoGCRegion", GCInterface::EndNoGCRegion)
    FCFuncElement("GetSegmentSize", GCInterface::GetSegmentSize)
    FCFuncElement("GetLastGCPercentTimeInGC", GCInterface::GetLastGCPercentTimeInGC)
    FCFuncElement("GetGenerationSize", GCInterface::GetGenerationSize)
    QCFuncElement("_AddMemoryPressure", GCInterface::_AddMemoryPressure)
    QCFuncElement("_RemoveMemoryPressure", GCInterface::_RemoveMemoryPressure)
    FCFuncElement("GetGeneration", GCInterface::GetGeneration)
    QCFuncElement("GetTotalMemory", GCInterface::GetTotalMemory)
    QCFuncElement("_Collect", GCInterface::Collect)
    FCFuncElement("GetMaxGeneration", GCInterface::GetMaxGeneration)
    QCFuncElement("_WaitForPendingFinalizers", GCInterface::WaitForPendingFinalizers)

    FCFuncElement("_SuppressFinalize", GCInterface::SuppressFinalize)
    FCFuncElement("_ReRegisterForFinalize", GCInterface::ReRegisterForFinalize)

    FCFuncElement("GetAllocatedBytesForCurrentThread", GCInterface::GetAllocatedBytesForCurrentThread)
    FCFuncElement("GetTotalAllocatedBytes", GCInterface::GetTotalAllocatedBytes)

    FCFuncElement("AllocateNewArray", GCInterface::AllocateNewArray)

#ifdef FEATURE_BASICFREEZE
    QCFuncElement("_RegisterFrozenSegment", GCInterface::RegisterFrozenSegment)
    QCFuncElement("_UnregisterFrozenSegment", GCInterface::UnregisterFrozenSegment)
#endif // FEATURE_BASICFREEZE

FCFuncEnd()

FCFuncStart(gGCSettingsFuncs)
    FCFuncElement("get_IsServerGC", SystemNative::IsServerGC)
    FCFuncElement("GetGCLatencyMode", GCInterface::GetGcLatencyMode)
    FCFuncElement("GetLOHCompactionMode", GCInterface::GetLOHCompactionMode)
    FCFuncElement("SetGCLatencyMode", GCInterface::SetGcLatencyMode)
    FCFuncElement("SetLOHCompactionMode", GCInterface::SetLOHCompactionMode)
FCFuncEnd()

FCFuncStart(gInteropMarshalFuncs)
    FCFuncElement("GetLastPInvokeError", MarshalNative::GetLastPInvokeError)
    FCFuncElement("SetLastPInvokeError", MarshalNative::SetLastPInvokeError)
    FCFuncElement("SizeOfHelper", MarshalNative::SizeOfClass)
    FCFuncElement("StructureToPtr", MarshalNative::StructureToPtr)
    FCFuncElement("PtrToStructureHelper", MarshalNative::PtrToStructureHelper)
    FCFuncElement("DestroyStructure", MarshalNative::DestroyStructure)
    FCFuncElement("IsPinnable", MarshalNative::IsPinnable)
    FCFuncElement("GetExceptionCode", ExceptionNative::GetExceptionCode)
    FCFuncElement("GetExceptionPointers", ExceptionNative::GetExceptionPointers)
#ifdef TARGET_WINDOWS
    QCFuncElement("GetHINSTANCE", COMModule::GetHINSTANCE)
#endif // TARGET_WINDOWS

    FCFuncElement("OffsetOfHelper", MarshalNative::OffsetOfHelper)

    QCFuncElement("InternalPrelink", MarshalNative::Prelink)
    QCFuncElement("IsBuiltInComSupportedInternal", MarshalNative::IsBuiltInComSupported)
    FCFuncElement("GetExceptionForHRInternal", MarshalNative::GetExceptionForHR)
    FCFuncElement("GetDelegateForFunctionPointerInternal", MarshalNative::GetDelegateForFunctionPointerInternal)
    FCFuncElement("GetFunctionPointerForDelegateInternal", MarshalNative::GetFunctionPointerForDelegateInternal)

#ifdef _DEBUG
    QCFuncElement("GetIsInCooperativeGCModeFunctionPointer", MarshalNative::GetIsInCooperativeGCModeFunctionPointer)
#endif // _DEBUG

#ifdef FEATURE_COMINTEROP
    FCFuncElement("GetHRForException", MarshalNative::GetHRForException)
    FCFuncElement("GetObjectForIUnknownNative", MarshalNative::GetObjectForIUnknownNative)
    FCFuncElement("GetUniqueObjectForIUnknownNative", MarshalNative::GetUniqueObjectForIUnknownNative)
    FCFuncElement("GetNativeVariantForObjectNative", MarshalNative::GetNativeVariantForObjectNative)
    FCFuncElement("GetObjectForNativeVariantNative", MarshalNative::GetObjectForNativeVariantNative)
    FCFuncElement("InternalFinalReleaseComObject", MarshalNative::FinalReleaseComObject)
    FCFuncElement("IsTypeVisibleFromCom", MarshalNative::IsTypeVisibleFromCom)
    FCFuncElement("CreateAggregatedObjectNative", MarshalNative::CreateAggregatedObjectNative)
    FCFuncElement("AreComObjectsAvailableForCleanup", MarshalNative::AreComObjectsAvailableForCleanup)
    FCFuncElement("InternalCreateWrapperOfType", MarshalNative::InternalCreateWrapperOfType)
    FCFuncElement("GetObjectsForNativeVariantsNative", MarshalNative::GetObjectsForNativeVariantsNative)
    FCFuncElement("GetStartComSlot", MarshalNative::GetStartComSlot)
    FCFuncElement("GetEndComSlot", MarshalNative::GetEndComSlot)
    FCFuncElement("GetIUnknownForObjectNative", MarshalNative::GetIUnknownForObjectNative)
    FCFuncElement("GetIDispatchForObjectNative", MarshalNative::GetIDispatchForObjectNative)
    FCFuncElement("GetComInterfaceForObjectNative", MarshalNative::GetComInterfaceForObjectNative)
    FCFuncElement("InternalReleaseComObject", MarshalNative::ReleaseComObject)
    FCFuncElement("GetTypedObjectForIUnknown", MarshalNative::GetTypedObjectForIUnknown)
    FCFuncElement("ChangeWrapperHandleStrength", MarshalNative::ChangeWrapperHandleStrength)
    FCFuncElement("CleanupUnusedObjectsInCurrentContext", MarshalNative::CleanupUnusedObjectsInCurrentContext)
    QCFuncElement("GetTypeFromCLSID", MarshalNative::GetTypeFromCLSID)
#endif // FEATURE_COMINTEROP
FCFuncEnd()

FCFuncStart(gInteropNativeLibraryFuncs)
    QCFuncElement("LoadFromPath", NativeLibraryNative::LoadFromPath)
    QCFuncElement("LoadByName", NativeLibraryNative::LoadByName)
    QCFuncElement("FreeLib", NativeLibraryNative::FreeLib)
    QCFuncElement("GetSymbol", NativeLibraryNative::GetSymbol)
FCFuncEnd()

FCFuncStart(gTypeLoadExceptionFuncs)
    QCFuncElement("GetTypeLoadExceptionMessage", GetTypeLoadExceptionMessage)
FCFuncEnd()

FCFuncStart(gFileLoadExceptionFuncs)
    QCFuncElement("GetFileLoadExceptionMessage", GetFileLoadExceptionMessage)
    QCFuncElement("GetMessageForHR", FileLoadException_GetMessageForHR)
FCFuncEnd()

FCFuncStart(gMissingMemberExceptionFuncs)
    FCFuncElement("FormatSignature", MissingMemberException_FormatSignature)
FCFuncEnd()

FCFuncStart(gInterlockedFuncs)
    FCIntrinsicSig("Exchange", &gsig_SM_RefInt_Int_RetInt, COMInterlocked::Exchange, CORINFO_INTRINSIC_InterlockedXchg32)
    FCIntrinsicSig("Exchange", &gsig_SM_RefLong_Long_RetLong, COMInterlocked::Exchange64, CORINFO_INTRINSIC_InterlockedXchg64)
    FCFuncElementSig("Exchange", &gsig_SM_RefDbl_Dbl_RetDbl, COMInterlocked::ExchangeDouble)
    FCFuncElementSig("Exchange", &gsig_SM_RefFlt_Flt_RetFlt, COMInterlocked::ExchangeFloat)
    FCFuncElementSig("Exchange", &gsig_SM_RefObj_Obj_RetObj, COMInterlocked::ExchangeObject)
    FCIntrinsicSig("CompareExchange", &gsig_SM_RefInt_Int_Int_RetInt, COMInterlocked::CompareExchange, CORINFO_INTRINSIC_InterlockedCmpXchg32)
    FCIntrinsicSig("CompareExchange", &gsig_SM_RefLong_Long_Long_RetLong, COMInterlocked::CompareExchange64, CORINFO_INTRINSIC_InterlockedCmpXchg64)
    FCFuncElementSig("CompareExchange", &gsig_SM_RefDbl_Dbl_Dbl_RetDbl, COMInterlocked::CompareExchangeDouble)
    FCFuncElementSig("CompareExchange", &gsig_SM_RefFlt_Flt_Flt_RetFlt, COMInterlocked::CompareExchangeFloat)
    FCFuncElementSig("CompareExchange", &gsig_SM_RefObj_Obj_Obj_RetObj, COMInterlocked::CompareExchangeObject)
    FCIntrinsicSig("ExchangeAdd", &gsig_SM_RefInt_Int_RetInt, COMInterlocked::ExchangeAdd32, CORINFO_INTRINSIC_InterlockedXAdd32)
    FCIntrinsicSig("ExchangeAdd", &gsig_SM_RefLong_Long_RetLong, COMInterlocked::ExchangeAdd64, CORINFO_INTRINSIC_InterlockedXAdd64)

    FCIntrinsic("MemoryBarrier", COMInterlocked::FCMemoryBarrier, CORINFO_INTRINSIC_MemoryBarrier)
    FCIntrinsic("ReadMemoryBarrier", COMInterlocked::FCMemoryBarrierLoad, CORINFO_INTRINSIC_MemoryBarrierLoad)
    QCFuncElement("_MemoryBarrierProcessWide", COMInterlocked::MemoryBarrierProcessWide)
FCFuncEnd()

FCFuncStart(gJitInfoFuncs)
    FCFuncElement("GetCompiledILBytes", GetCompiledILBytes)
    FCFuncElement("GetCompiledMethodCount", GetCompiledMethodCount)
    FCFuncElement("GetCompilationTimeInTicks", GetCompilationTimeInTicks)
FCFuncEnd()

FCFuncStart(gVarArgFuncs)
    FCFuncElementSig(COR_CTOR_METHOD_NAME, &gsig_IM_IntPtr_PtrVoid_RetVoid, VarArgsNative::Init2)
    FCFuncElementSig(COR_CTOR_METHOD_NAME, &gsig_IM_IntPtr_RetVoid, VarArgsNative::Init)
    FCFuncElement("GetRemainingCount", VarArgsNative::GetRemainingCount)
    FCFuncElement("_GetNextArgType", VarArgsNative::GetNextArgType)
    FCFuncElement("FCallGetNextArg", VarArgsNative::DoGetNextArg)
    FCFuncElement("InternalGetNextArg", VarArgsNative::GetNextArg2)
FCFuncEnd()

FCFuncStart(gMonitorFuncs)
    FCFuncElement("Enter", JIT_MonEnter)
    FCFuncElement("ReliableEnter", JIT_MonReliableEnter)
    FCFuncElement("ReliableEnterTimeout", JIT_MonTryEnter)
    FCFuncElement("Exit", JIT_MonExit)
    FCFuncElement("ObjWait", ObjectNative::WaitTimeout)
    FCFuncElement("ObjPulse", ObjectNative::Pulse)
    FCFuncElement("ObjPulseAll", ObjectNative::PulseAll)
    FCFuncElement("IsEnteredNative", ObjectNative::IsLockHeld)
    QCFuncElement("GetLockContentionCount", ObjectNative::GetMonitorLockContentionCount)
FCFuncEnd()

FCFuncStart(gOverlappedFuncs)
    FCFuncElement("AllocateNativeOverlapped", AllocateNativeOverlapped)
    FCFuncElement("FreeNativeOverlapped", FreeNativeOverlapped)
    FCFuncElement("CheckVMForIOPacket", CheckVMForIOPacket)
    FCFuncElement("GetOverlappedFromNative", GetOverlappedFromNative)
FCFuncEnd()

FCFuncStart(gRuntimeHelpers)
    FCFuncElement("GetObjectValue", ObjectNative::GetObjectValue)
    FCIntrinsic("InitializeArray", ArrayNative::InitializeArray, CORINFO_INTRINSIC_InitializeArray)
    QCFuncElement("RunClassConstructor", ReflectionInvocation::RunClassConstructor)
    QCFuncElement("RunModuleConstructor", ReflectionInvocation::RunModuleConstructor)
    QCFuncElement("CompileMethod", ReflectionInvocation::CompileMethod)
    QCFuncElement("PrepareMethod", ReflectionInvocation::PrepareMethod)
    FCFuncElement("PrepareDelegate", ReflectionInvocation::PrepareDelegate)
    FCFuncElement("GetHashCode", ObjectNative::GetHashCode)
    FCFuncElement("Equals", ObjectNative::Equals)
    FCFuncElement("AllocateUninitializedClone", ObjectNative::AllocateUninitializedClone)
    FCFuncElement("EnsureSufficientExecutionStack", ReflectionInvocation::EnsureSufficientExecutionStack)
    FCFuncElement("TryEnsureSufficientExecutionStack", ReflectionInvocation::TryEnsureSufficientExecutionStack)
    QCFuncElement("GetUninitializedObject", ReflectionSerialization::GetUninitializedObject)
    QCFuncElement("AllocateTypeAssociatedMemory", RuntimeTypeHandle::AllocateTypeAssociatedMemory)
    FCFuncElement("AllocTailCallArgBuffer", TailCallHelp::AllocTailCallArgBuffer)
    FCFuncElement("GetTailCallInfo", TailCallHelp::GetTailCallInfo)
FCFuncEnd()

FCFuncStart(gMngdFixedArrayMarshalerFuncs)
    FCFuncElement("CreateMarshaler", MngdFixedArrayMarshaler::CreateMarshaler)
    FCFuncElement("ConvertSpaceToNative", MngdFixedArrayMarshaler::ConvertSpaceToNative)
    FCFuncElement("ConvertContentsToNative", MngdFixedArrayMarshaler::ConvertContentsToNative)
    FCFuncElement("ConvertSpaceToManaged", MngdFixedArrayMarshaler::ConvertSpaceToManaged)
    FCFuncElement("ConvertContentsToManaged", MngdFixedArrayMarshaler::ConvertContentsToManaged)
    FCFuncElement("ClearNativeContents", MngdFixedArrayMarshaler::ClearNativeContents)
FCFuncEnd()

FCFuncStart(gMngdNativeArrayMarshalerFuncs)
    FCFuncElement("CreateMarshaler", MngdNativeArrayMarshaler::CreateMarshaler)
    FCFuncElement("ConvertSpaceToNative", MngdNativeArrayMarshaler::ConvertSpaceToNative)
    FCFuncElement("ConvertContentsToNative", MngdNativeArrayMarshaler::ConvertContentsToNative)
    FCFuncElement("ConvertSpaceToManaged", MngdNativeArrayMarshaler::ConvertSpaceToManaged)
    FCFuncElement("ConvertContentsToManaged", MngdNativeArrayMarshaler::ConvertContentsToManaged)
    FCFuncElement("ClearNative", MngdNativeArrayMarshaler::ClearNative)
    FCFuncElement("ClearNativeContents", MngdNativeArrayMarshaler::ClearNativeContents)
FCFuncEnd()

#ifdef FEATURE_COMINTEROP
FCFuncStart(gObjectMarshalerFuncs)
    FCFuncElement("ConvertToNative", StubHelpers::ObjectMarshaler__ConvertToNative)
    FCFuncElement("ConvertToManaged", StubHelpers::ObjectMarshaler__ConvertToManaged)
    FCFuncElement("ClearNative", StubHelpers::ObjectMarshaler__ClearNative)
FCFuncEnd()

FCFuncStart(gInterfaceMarshalerFuncs)
    FCFuncElement("ConvertToNative", StubHelpers::InterfaceMarshaler__ConvertToNative)
    FCFuncElement("ConvertToManaged", StubHelpers::InterfaceMarshaler__ConvertToManaged)
    QCFuncElement("ClearNative", StubHelpers::InterfaceMarshaler__ClearNative)
FCFuncEnd()

FCFuncStart(gMngdSafeArrayMarshalerFuncs)
    FCFuncElement("CreateMarshaler", MngdSafeArrayMarshaler::CreateMarshaler)
    FCFuncElement("ConvertSpaceToNative", MngdSafeArrayMarshaler::ConvertSpaceToNative)
    FCFuncElement("ConvertContentsToNative", MngdSafeArrayMarshaler::ConvertContentsToNative)
    FCFuncElement("ConvertSpaceToManaged", MngdSafeArrayMarshaler::ConvertSpaceToManaged)
    FCFuncElement("ConvertContentsToManaged", MngdSafeArrayMarshaler::ConvertContentsToManaged)
    FCFuncElement("ClearNative", MngdSafeArrayMarshaler::ClearNative)
FCFuncEnd()

#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMWRAPPERS
FCFuncStart(gComWrappersFuncs)
    QCFuncElement("GetIUnknownImplInternal", ComWrappersNative::GetIUnknownImpl)
    QCFuncElement("TryGetOrCreateComInterfaceForObjectInternal", ComWrappersNative::TryGetOrCreateComInterfaceForObject)
    QCFuncElement("TryGetOrCreateObjectForComInstanceInternal", ComWrappersNative::TryGetOrCreateObjectForComInstance)
    QCFuncElement("SetGlobalInstanceRegisteredForMarshalling", GlobalComWrappersForMarshalling::SetGlobalInstanceRegisteredForMarshalling)
    QCFuncElement("SetGlobalInstanceRegisteredForTrackerSupport", GlobalComWrappersForTrackerSupport::SetGlobalInstanceRegisteredForTrackerSupport)
FCFuncEnd()
#endif // FEATURE_COMWRAPPERS

#ifdef FEATURE_OBJCMARSHAL
FCFuncStart(gObjCMarshalFuncs)
    QCFuncElement("TrySetGlobalMessageSendCallback", ObjCMarshalNative::TrySetGlobalMessageSendCallback)
    QCFuncElement("TryInitializeReferenceTracker", ObjCMarshalNative::TryInitializeReferenceTracker)
    QCFuncElement("CreateReferenceTrackingHandleInternal", ObjCMarshalNative::CreateReferenceTrackingHandle)
FCFuncEnd()
#endif // FEATURE_OBJCMARSHAL

FCFuncStart(gMngdRefCustomMarshalerFuncs)
    FCFuncElement("CreateMarshaler", MngdRefCustomMarshaler::CreateMarshaler)
    FCFuncElement("ConvertContentsToNative", MngdRefCustomMarshaler::ConvertContentsToNative)
    FCFuncElement("ConvertContentsToManaged", MngdRefCustomMarshaler::ConvertContentsToManaged)
    FCFuncElement("ClearNative", MngdRefCustomMarshaler::ClearNative)
    FCFuncElement("ClearManaged", MngdRefCustomMarshaler::ClearManaged)
FCFuncEnd()

FCFuncStart(gStubHelperFuncs)
    FCFuncElement("InitDeclaringType", StubHelpers::InitDeclaringType)
    FCFuncElement("GetNDirectTarget", StubHelpers::GetNDirectTarget)
    FCFuncElement("GetDelegateTarget", StubHelpers::GetDelegateTarget)
    FCFuncElement("SetLastError", StubHelpers::SetLastError)
    FCFuncElement("ClearLastError", StubHelpers::ClearLastError)
    FCFuncElement("ThrowInteropParamException", StubHelpers::ThrowInteropParamException)
    FCFuncElement("InternalGetHRExceptionObject", StubHelpers::GetHRExceptionObject)
#ifdef FEATURE_COMINTEROP
    FCFuncElement("InternalGetCOMHRExceptionObject", StubHelpers::GetCOMHRExceptionObject)
    FCFuncElement("GetCOMIPFromRCW", StubHelpers::GetCOMIPFromRCW)
#endif // FEATURE_COMINTEROP
#ifdef PROFILING_SUPPORTED
    FCFuncElement("ProfilerBeginTransitionCallback", StubHelpers::ProfilerBeginTransitionCallback)
    FCFuncElement("ProfilerEndTransitionCallback", StubHelpers::ProfilerEndTransitionCallback)
#endif
    FCFuncElement("CreateCustomMarshalerHelper", StubHelpers::CreateCustomMarshalerHelper)
    FCFuncElement("FmtClassUpdateNativeInternal", StubHelpers::FmtClassUpdateNativeInternal)
    FCFuncElement("FmtClassUpdateCLRInternal", StubHelpers::FmtClassUpdateCLRInternal)
    FCFuncElement("LayoutDestroyNativeInternal", StubHelpers::LayoutDestroyNativeInternal)
    FCFuncElement("AllocateInternal", StubHelpers::AllocateInternal)
    FCFuncElement("MarshalToUnmanagedVaListInternal", StubHelpers::MarshalToUnmanagedVaListInternal)
    FCFuncElement("MarshalToManagedVaListInternal", StubHelpers::MarshalToManagedVaListInternal)
    FCFuncElement("CalcVaListSize", StubHelpers::CalcVaListSize)
    FCFuncElement("ValidateObject", StubHelpers::ValidateObject)
    FCFuncElement("ValidateByref", StubHelpers::ValidateByref)
    FCFuncElement("LogPinnedArgument", StubHelpers::LogPinnedArgument)
    FCIntrinsic("GetStubContext", StubHelpers::GetStubContext, CORINFO_INTRINSIC_StubHelpers_GetStubContext)
#ifdef TARGET_64BIT
    FCIntrinsic("GetStubContextAddr", StubHelpers::GetStubContextAddr, CORINFO_INTRINSIC_StubHelpers_GetStubContextAddr)
#endif // TARGET_64BIT
#ifdef FEATURE_ARRAYSTUB_AS_IL
    FCFuncElement("ArrayTypeCheck", StubHelpers::ArrayTypeCheck)
#endif //FEATURE_ARRAYSTUB_AS_IL
#ifdef FEATURE_MULTICASTSTUB_AS_IL
    FCFuncElement("MulticastDebuggerTraceHelper", StubHelpers::MulticastDebuggerTraceHelper)
#endif //FEATURE_MULTICASTSTUB_AS_IL
    FCIntrinsic("NextCallReturnAddress", StubHelpers::NextCallReturnAddress, CORINFO_INTRINSIC_StubHelpers_NextCallReturnAddress)
FCFuncEnd()

FCFuncStart(gGCHandleFuncs)
    FCFuncElement("InternalAlloc", MarshalNative::GCHandleInternalAlloc)
    FCFuncElement("InternalFree", MarshalNative::GCHandleInternalFree)
    FCFuncElement("InternalGet", MarshalNative::GCHandleInternalGet)
    FCFuncElement("InternalSet", MarshalNative::GCHandleInternalSet)
    FCFuncElement("InternalCompareExchange", MarshalNative::GCHandleInternalCompareExchange)
FCFuncEnd()


FCFuncStart(gStreamFuncs)
    FCFuncElement("HasOverriddenBeginEndRead", StreamNative::HasOverriddenBeginEndRead)
    FCFuncElement("HasOverriddenBeginEndWrite", StreamNative::HasOverriddenBeginEndWrite)
FCFuncEnd()


#if defined(FEATURE_EVENTSOURCE_XPLAT)
FCFuncStart(gEventLogger)
    QCFuncElement("IsEventSourceLoggingEnabled", XplatEventSourceLogger::IsEventSourceLoggingEnabled)
    QCFuncElement("LogEventSource", XplatEventSourceLogger::LogEventSource)
FCFuncEnd()
#endif // defined(FEATURE_EVENTSOURCE_XPLAT)

#if defined(FEATURE_PERFTRACING)
FCFuncStart(gNativeEventLogger)
    QCFuncElement("LogThreadPoolWorkerThreadStart", NativeEventLogger::LogThreadPoolWorkerThreadStart)
    QCFuncElement("LogThreadPoolWorkerThreadStop", NativeEventLogger::LogThreadPoolWorkerThreadStop)
    QCFuncElement("LogThreadPoolWorkerThreadWait", NativeEventLogger::LogThreadPoolWorkerThreadWait)
    QCFuncElement("LogThreadPoolWorkerThreadAdjustmentSample", NativeEventLogger::LogThreadPoolWorkerThreadAdjustmentSample)
    QCFuncElement("LogThreadPoolWorkerThreadAdjustmentAdjustment", NativeEventLogger::LogThreadPoolWorkerThreadAdjustmentAdjustment)
    QCFuncElement("LogThreadPoolWorkerThreadAdjustmentStats", NativeEventLogger::LogThreadPoolWorkerThreadAdjustmentStats)
    QCFuncElement("LogThreadPoolIOEnqueue", NativeEventLogger::LogThreadPoolIOEnqueue)
    QCFuncElement("LogThreadPoolIODequeue", NativeEventLogger::LogThreadPoolIODequeue)
    QCFuncElement("LogThreadPoolWorkingThreadCount", NativeEventLogger::LogThreadPoolWorkingThreadCount)
FCFuncEnd()
#endif // defined(FEATURE_PERFTRACING)

#ifdef FEATURE_PERFTRACING
FCFuncStart(gEventPipeInternalFuncs)
    QCFuncElement("Enable", EventPipeInternal::Enable)
    QCFuncElement("Disable", EventPipeInternal::Disable)
    QCFuncElement("GetSessionInfo", EventPipeInternal::GetSessionInfo)
    QCFuncElement("CreateProvider", EventPipeInternal::CreateProvider)
    QCFuncElement("DefineEvent", EventPipeInternal::DefineEvent)
    QCFuncElement("DeleteProvider", EventPipeInternal::DeleteProvider)
    QCFuncElement("EventActivityIdControl", EventPipeInternal::EventActivityIdControl)
    QCFuncElement("GetProvider", EventPipeInternal::GetProvider)
    QCFuncElement("WriteEventData", EventPipeInternal::WriteEventData)
    QCFuncElement("GetNextEvent", EventPipeInternal::GetNextEvent)
    QCFuncElement("GetWaitHandle", EventPipeInternal::GetWaitHandle)
FCFuncEnd()
#endif // FEATURE_PERFTRACING

FCFuncStart(gWeakReferenceFuncs)
    FCFuncElement("Create", WeakReferenceNative::Create)
    FCFuncElement("Finalize", WeakReferenceNative::Finalize)
    FCFuncElement("get_Target", WeakReferenceNative::GetTarget)
    FCFuncElement("set_Target", WeakReferenceNative::SetTarget)
    FCFuncElement("get_IsAlive", WeakReferenceNative::IsAlive)
    FCFuncElement("IsTrackResurrection", WeakReferenceNative::IsTrackResurrection)
FCFuncEnd()

FCFuncStart(gWeakReferenceOfTFuncs)
    FCFuncElement("Create", WeakReferenceOfTNative::Create)
    FCFuncElement("Finalize", WeakReferenceOfTNative::Finalize)
    FCFuncElement("get_Target", WeakReferenceOfTNative::GetTarget)
    FCFuncElement("set_Target", WeakReferenceOfTNative::SetTarget)
    FCFuncElement("IsTrackResurrection", WeakReferenceOfTNative::IsTrackResurrection)
FCFuncEnd()

#ifdef TARGET_UNIX
FCFuncStart(gPalKernel32Funcs)
    QCFuncElement("CloseHandle", CloseHandle)
    QCFuncElement("CreateEventEx", CreateEventExW)
    QCFuncElement("CreateMutexEx", CreateMutexExW)
    QCFuncElement("CreateSemaphoreEx", CreateSemaphoreExW)
    QCFuncElement("FormatMessage", FormatMessageW)
    QCFuncElement("FreeEnvironmentStringsW", FreeEnvironmentStringsW)
    QCFuncElement("GetEnvironmentStringsW", GetEnvironmentStringsW)
    QCFuncElement("GetEnvironmentVariable", GetEnvironmentVariableW)
    QCFuncElement("OpenEvent", OpenEventW)
    QCFuncElement("OpenMutex", OpenMutexW)
    QCFuncElement("OpenSemaphore", OpenSemaphoreW)
    QCFuncElement("OutputDebugString", OutputDebugStringW)
    QCFuncElement("ReleaseMutex", ReleaseMutex)
    QCFuncElement("ReleaseSemaphore", ReleaseSemaphore)
    QCFuncElement("ResetEvent", ResetEvent)
    QCFuncElement("SetEnvironmentVariable", SetEnvironmentVariableW)
    QCFuncElement("SetEvent", SetEvent)
FCFuncEnd()
#endif

#if defined(TARGET_X86) || defined(TARGET_AMD64)
FCFuncStart(gX86BaseFuncs)
    QCFuncElement("__cpuidex", SystemNative::X86BaseCpuId)
FCFuncEnd()
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)

#ifdef FEATURE_COMINTEROP

//
// ECall helpers for the standard managed interfaces.
//

#define MNGSTDITF_BEGIN_INTERFACE(FriendlyName, strMngItfName, strUCOMMngItfName, strCustomMarshalerName, strCustomMarshalerCookie, strManagedViewName, NativeItfIID, bCanCastOnNativeItfQI) \
FCFuncStart(g##FriendlyName##Funcs)

#define MNGSTDITF_DEFINE_METH_IMPL(FriendlyName, FCallMethName, MethName, MethSig, FcallDecl) \
    FCUnreferenced FCFuncElementSig(#MethName, MethSig, FriendlyName::FCallMethName)

#define MNGSTDITF_END_INTERFACE(FriendlyName) \
FCFuncEnd()

#include "mngstditflist.h"

#undef MNGSTDITF_BEGIN_INTERFACE
#undef MNGSTDITF_DEFINE_METH_IMPL
#undef MNGSTDITF_END_INTERFACE

#endif // FEATURE_COMINTEROP


//
//
// Class definitions
//
//

// Note these have to remain sorted by name:namespace pair (Assert will wack you if you don't)
// The sorting is case-sensitive

FCClassElement("ArgIterator", "System", gVarArgFuncs)
FCClassElement("Array", "System", gArrayFuncs)
FCClassElement("Assembly", "System.Reflection", gAssemblyFuncs)
FCClassElement("AssemblyBuilder", "System.Reflection.Emit", gAssemblyBuilderFuncs)
FCClassElement("AssemblyExtensions", "System.Reflection.Metadata", gAssemblyExtensionsFuncs)
FCClassElement("AssemblyLoadContext", "System.Runtime.Loader", gAssemblyLoadContextFuncs)
FCClassElement("AssemblyName", "System.Reflection", gAssemblyNameFuncs)
FCClassElement("Buffer", "System", gBufferFuncs)
FCClassElement("CLRConfig", "System", gClrConfig)
FCClassElement("CastHelpers", "System.Runtime.CompilerServices", gCastHelpers)
#ifdef FEATURE_COMWRAPPERS
FCClassElement("ComWrappers", "System.Runtime.InteropServices", gComWrappersFuncs)
#endif // FEATURE_COMWRAPPERS
FCClassElement("CompatibilitySwitch", "System.Runtime.Versioning", gCompatibilitySwitchFuncs)
FCClassElement("CustomAttribute", "System.Reflection", gCOMCustomAttributeFuncs)
FCClassElement("CustomAttributeEncodedArgument", "System.Reflection", gCustomAttributeEncodedArgument)
FCClassElement("Debugger", "System.Diagnostics", gDiagnosticsDebugger)
FCClassElement("Delegate", "System", gDelegateFuncs)
FCClassElement("DependentHandle", "System.Runtime", gDependentHandleFuncs)
FCClassElement("Enum", "System", gEnumFuncs)
FCClassElement("Environment", "System", gEnvironmentFuncs)
#if defined(FEATURE_PERFTRACING)
FCClassElement("EventPipeInternal", "System.Diagnostics.Tracing", gEventPipeInternalFuncs)
#endif // FEATURE_PERFTRACING
FCClassElement("Exception", "System", gExceptionFuncs)
FCClassElement("FileLoadException", "System.IO", gFileLoadExceptionFuncs)
FCClassElement("GC", "System", gGCInterfaceFuncs)
FCClassElement("GCHandle", "System.Runtime.InteropServices", gGCHandleFuncs)
FCClassElement("GCSettings", "System.Runtime", gGCSettingsFuncs)
#ifdef FEATURE_COMINTEROP
FCClassElement("IEnumerable", "System.Collections", gStdMngIEnumerableFuncs)
FCClassElement("IEnumerator", "System.Collections", gStdMngIEnumeratorFuncs)
FCClassElement("IReflect", "System.Reflection", gStdMngIReflectFuncs)
FCClassElement("InterfaceMarshaler", "System.StubHelpers", gInterfaceMarshalerFuncs)
#endif
FCClassElement("Interlocked", "System.Threading", gInterlockedFuncs)
FCClassElement("JitInfo", "System.Runtime", gJitInfoFuncs)
#if TARGET_UNIX
FCClassElement("Kernel32", "", gPalKernel32Funcs)
#endif
FCClassElement("LoaderAllocatorScout", "System.Reflection", gLoaderAllocatorFuncs)
#ifdef TARGET_UNIX
FCClassElement("LowLevelLifoSemaphore", "System.Threading", gLowLevelLifoSemaphoreFuncs)
#endif
FCClassElement("Marshal", "System.Runtime.InteropServices", gInteropMarshalFuncs)
FCClassElement("Math", "System", gMathFuncs)
FCClassElement("MathF", "System", gMathFFuncs)
FCClassElement("MdUtf8String", "System", gMdUtf8String)
FCClassElement("MetadataImport", "System.Reflection", gMetaDataImport)
FCClassElement("MetadataUpdater", "System.Reflection.Metadata", gMetadataUpdaterFuncs)
FCClassElement("MissingMemberException", "System",  gMissingMemberExceptionFuncs)
FCClassElement("MngdFixedArrayMarshaler", "System.StubHelpers", gMngdFixedArrayMarshalerFuncs)
FCClassElement("MngdNativeArrayMarshaler", "System.StubHelpers", gMngdNativeArrayMarshalerFuncs)
FCClassElement("MngdRefCustomMarshaler", "System.StubHelpers", gMngdRefCustomMarshalerFuncs)
#ifdef FEATURE_COMINTEROP
FCClassElement("MngdSafeArrayMarshaler", "System.StubHelpers", gMngdSafeArrayMarshalerFuncs)
#endif // FEATURE_COMINTEROP
FCClassElement("ModuleBuilder", "System.Reflection.Emit", gCOMModuleBuilderFuncs)
FCClassElement("ModuleHandle", "System", gCOMModuleHandleFuncs)
FCClassElement("Monitor", "System.Threading", gMonitorFuncs)
FCClassElement("NativeLibrary", "System.Runtime.InteropServices", gInteropNativeLibraryFuncs)
#if defined(FEATURE_PERFTRACING)
FCClassElement("NativeRuntimeEventSource", "System.Diagnostics.Tracing", gNativeEventLogger)
#endif //defined(FEATURE_PERFTRACING)
#ifdef FEATURE_COMINTEROP
FCClassElement("OAVariantLib", "Microsoft.Win32", gOAVariantFuncs)
#endif
FCClassElement("Object", "System", gObjectFuncs)
#ifdef FEATURE_COMINTEROP
FCClassElement("ObjectMarshaler", "System.StubHelpers", gObjectMarshalerFuncs)
#endif
#ifdef FEATURE_OBJCMARSHAL
FCClassElement("ObjectiveCMarshal", "System.Runtime.InteropServices.ObjectiveC", gObjCMarshalFuncs)
#endif // FEATURE_OBJCMARSHAL
FCClassElement("OverlappedData", "System.Threading", gOverlappedFuncs)


FCClassElement("PunkSafeHandle", "System.Reflection.Emit", gSymWrapperCodePunkSafeHandleFuncs)
FCClassElement("RegisteredWaitHandle", "System.Threading", gRegisteredWaitHandleFuncs)

FCClassElement("RuntimeAssembly", "System.Reflection", gRuntimeAssemblyFuncs)
FCClassElement("RuntimeFieldHandle", "System", gCOMFieldHandleNewFuncs)
FCClassElement("RuntimeHelpers", "System.Runtime.CompilerServices", gRuntimeHelpers)
FCClassElement("RuntimeMethodHandle", "System", gRuntimeMethodHandle)
FCClassElement("RuntimeModule", "System.Reflection", gCOMModuleFuncs)
FCClassElement("RuntimeType", "System", gSystem_RuntimeType)
FCClassElement("RuntimeTypeHandle", "System", gCOMTypeHandleFuncs)
FCClassElement("SafeTypeNameParserHandle", "System", gSafeTypeNameParserHandle)

FCClassElement("Signature", "System", gSignatureNative)
FCClassElement("StackTrace", "System.Diagnostics", gDiagnosticsStackTrace)
FCClassElement("Stream", "System.IO", gStreamFuncs)
FCClassElement("String", "System", gStringFuncs)
FCClassElement("StubHelpers", "System.StubHelpers", gStubHelperFuncs)
FCClassElement("Thread", "System.Threading", gThreadFuncs)
FCClassElement("ThreadPool", "System.Threading", gThreadPoolFuncs)
FCClassElement("TimerQueue", "System.Threading", gTimerFuncs)
FCClassElement("Type", "System", gSystem_Type)
FCClassElement("TypeBuilder", "System.Reflection.Emit", gCOMClassWriter)
FCClassElement("TypeLoadException", "System", gTypeLoadExceptionFuncs)
FCClassElement("TypeNameParser", "System", gTypeNameParser)
FCClassElement("TypedReference", "System", gTypedReferenceFuncs)
FCClassElement("UnmanagedThreadPoolWorkItem", "System.Threading", gUnmanagedThreadPoolWorkItemFuncs)
FCClassElement("ValueType", "System", gValueTypeFuncs)
#ifdef FEATURE_COMINTEROP
FCClassElement("Variant", "System", gVariantFuncs)
#endif
FCClassElement("WaitHandle", "System.Threading", gWaitHandleFuncs)
FCClassElement("WeakReference", "System", gWeakReferenceFuncs)
FCClassElement("WeakReference`1", "System", gWeakReferenceOfTFuncs)
#if defined(TARGET_X86) || defined(TARGET_AMD64)
FCClassElement("X86Base", "System.Runtime.Intrinsics.X86", gX86BaseFuncs)
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)

#if defined(FEATURE_EVENTSOURCE_XPLAT)
FCClassElement("XplatEventLogger", "System.Diagnostics.Tracing", gEventLogger)
#endif //defined(FEATURE_EVENTSOURCE_XPLAT)

#undef FCFuncElement
#undef FCFuncElementSig
#undef FCIntrinsic
#undef FCIntrinsicSig
#undef QCFuncElement
#undef FCDynamic
#undef FCDynamicSig
#undef FCUnreferenced
#undef FCFuncStart
#undef FCFuncEnd
#undef FCClassElement
