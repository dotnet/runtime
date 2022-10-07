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
		/// <summary>
		/// <see cref="System.Reflection.IntrospectionExtensions.GetTypeInfo(System.Type)"/>
		/// </summary>
		IntrospectionExtensions_GetTypeInfo,
		/// <summary>
		/// <see cref="System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"/>
		/// </summary>
		Type_GetTypeFromHandle,
		/// <summary>
		/// <see cref="System.Type.TypeHandle"/>
		/// </summary>
		Type_get_TypeHandle,
		/// <summary>
		/// <see cref="System.Object.GetType()"/>
		/// </summary>
		Object_GetType,
		/// <summary>
		/// <see cref="System.Reflection.TypeDelegator()"/>
		/// </summary>
		TypeDelegator_Ctor,
		/// <summary>
		/// <see cref="System.Array.Empty{T}"/>
		/// </summary>
		Array_Empty,
		/// <summary>
		/// <see cref="System.Reflection.TypeInfo.AsType()"/>
		/// </summary>
		TypeInfo_AsType,
		/// <summary>
		/// <see cref="System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)"/> or 
		/// <see cref="System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle, System.RuntimeTypeHandle)"/>
		/// </summary>
		MethodBase_GetMethodFromHandle,
		/// <summary>
		/// <see cref="System.Reflection.MethodBase.MethodHandle"/>
		/// </summary>
		MethodBase_get_MethodHandle,

		// Anything above this marker will require the method to be run through
		// the reflection body scanner.
		RequiresReflectionBodyScanner_Sentinel = 1000,
		Type_MakeGenericType,
		Type_GetType,
		Type_GetConstructor,
		Type_GetConstructors__BindingFlags,
		Type_GetMethod,
		Type_GetMethods__BindingFlags,
		Type_GetField,
		Type_GetFields__BindingFlags,
		Type_GetProperty,
		Type_GetProperties__BindingFlags,
		Type_GetEvent,
		Type_GetEvents__BindingFlags,
		Type_GetNestedType,
		Type_GetNestedTypes__BindingFlags,
		Type_GetMember,
		Type_GetMembers__BindingFlags,
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
		Activator_CreateInstance__Type,
		Activator_CreateInstance__AssemblyName_TypeName,
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
