// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
	[StaticCs.Closed]
	enum IntrinsicId
	{
		None = 0,
		IntrospectionExtensions_GetTypeInfo,
		Type_GetTypeFromHandle,
		Type_get_TypeHandle,
		Object_GetType,
		TypeDelegator_Ctor,
		Array_Empty,
		TypeInfo_AsType,
		MethodBase_GetMethodFromHandle,
		MethodBase_get_MethodHandle,

		// Anything above this marker will require the method to be run through
		// the reflection body scanner.
		RequiresReflectionBodyScanner_Sentinel = 1000,
		Type_MakeGenericType,
		Type_GetType,
		Type_GetConstructor,
		Type_GetConstructors,
		Type_GetMethod,
		Type_GetMethods,
		Type_GetField,
		Type_GetFields,
		Type_GetProperty,
		Type_GetProperties,
		Type_GetEvent,
		Type_GetEvents,
		Type_GetNestedType,
		Type_GetNestedTypes,
		Type_GetMember,
		Type_GetMembers,
		Type_GetInterface,
		Type_get_AssemblyQualifiedName,
		Type_get_UnderlyingSystemType,
		Type_get_BaseType,
		Expression_Call,
		Expression_Field,
		Expression_Property,
		Expression_New,
		Enum_GetValues,
		Marshal_SizeOf,
		Marshal_OffsetOf,
		Marshal_PtrToStructure,
		Marshal_DestroyStructure,
		Marshal_GetDelegateForFunctionPointer,
		Activator_CreateInstance_Type,
		Activator_CreateInstance_AssemblyName_TypeName,
		Activator_CreateInstanceFrom,
		AppDomain_CreateInstance,
		AppDomain_CreateInstanceAndUnwrap,
		AppDomain_CreateInstanceFrom,
		AppDomain_CreateInstanceFromAndUnwrap,
		Assembly_CreateInstance,
		RuntimeReflectionExtensions_GetRuntimeEvent,
		RuntimeReflectionExtensions_GetRuntimeField,
		RuntimeReflectionExtensions_GetRuntimeMethod,
		RuntimeReflectionExtensions_GetRuntimeProperty,
		RuntimeHelpers_RunClassConstructor,
		MethodInfo_MakeGenericMethod,
		Nullable_GetUnderlyingType
	}
}
