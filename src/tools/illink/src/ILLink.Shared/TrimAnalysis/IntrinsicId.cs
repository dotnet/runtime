// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
	[StaticCs.Closed]
	internal enum IntrinsicId
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
		/// <see cref="object.GetType()"/>
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
		/// <list type="table">
		/// <item><see cref="System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)"/></item>
		/// <item><see cref="System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle, System.RuntimeTypeHandle)"/></item>
		/// </list>
		/// </summary>
		MethodBase_GetMethodFromHandle,
		/// <summary>
		/// <see cref="System.Reflection.MethodBase.MethodHandle"/>
		/// </summary>
		MethodBase_get_MethodHandle,

		// Anything above this marker will require the method to be run through
		// the reflection body scanner.
		RequiresReflectionBodyScanner_Sentinel = 1000,
		/// <summary>
		/// <see cref="System.Type.MakeGenericType(System.Type[])"/>
		/// </summary>
		Type_MakeGenericType,
		/// <summary>
		/// <list type="table">
		/// <item><see cref="System.Type.GetType(string)"/></item>
		/// <item><see cref= "System.Type.GetType(string, bool)" /></item>
		/// <item><see cref="System.Type.GetType(string, bool, bool)"/></item>
		/// <item><see cref="System.Type.GetType(string, System.Func{System.Reflection.AssemblyName, System.Reflection.Assembly?}?, System.Func{System.Reflection.Assembly?, string, bool, System.Type?}?)"/></item>
		/// <item><see cref="System.Type.GetType(string, System.Func{System.Reflection.AssemblyName, System.Reflection.Assembly?}?, System.Func{System.Reflection.Assembly?, string, bool, System.Type?}?, bool)"/></item>
		/// <item><see cref="System.Type.GetType(string, System.Func{System.Reflection.AssemblyName, System.Reflection.Assembly?}?, System.Func{System.Reflection.Assembly?, string, bool, System.Type?}?, bool, bool)"/></item>
		/// </list>
		/// </summary>
		Type_GetType,
		/// <summary>
		/// <item><see cref="System.Type.GetConstructor(System.Type[])"/></item>
		/// <item><see cref="System.Type.GetConstructor(System.Reflection.BindingFlags, System.Type[])"/></item>
		/// <item><see cref="System.Type.GetConstructor(System.Reflection.BindingFlags, System.Reflection.Binder?, System.Type[], System.Reflection.ParameterModifier[]?)"/></item>
		/// <item><see cref="System.Type.GetConstructor(System.Reflection.BindingFlags, System.Reflection.Binder?, System.Reflection.CallingConventions, System.Type[], System.Reflection.ParameterModifier[]?)"/></item>
		/// </list>
		/// </summary>
		Type_GetConstructor,
		/// <summary>
		/// <see cref="System.Type.GetConstructors(System.Reflection.BindingFlags)"/>
		/// </summary>
		Type_GetConstructors__BindingFlags,
		/// <summary>
		/// <item><see cref="System.Type.GetMethod(string)"/></item>
		/// <item><see cref="System.Type.GetMethod(string, System.Reflection.BindingFlags)"/></item>
		/// <item><see cref="System.Type.GetMethod(string, System.Type[])"/></item>
		/// <item><see cref="System.Type.GetMethod(string, System.Type[], System.Reflection.ParameterModifier[]?)"/></item>
		/// <item><see cref="System.Type.GetMethod(string, System.Reflection.BindingFlags, System.Type[])"/></item>
		/// <item><see cref="System.Type.GetMethod(string, System.Reflection.BindingFlags, System.Reflection.Binder, System.Type[], System.Reflection.ParameterModifier[]?)"/></item>
		/// <item><see cref="System.Type.GetMethod(string, System.Reflection.BindingFlags, System.Reflection.Binder, System.Reflection.CallingConventions, System.Type[], System.Reflection.ParameterModifier[]?)"/></item>
		/// <item><see cref="System.Type.GetMethod(string, int, System.Type[])"/></item>
		/// <item><see cref="System.Type.GetMethod(string, int, System.Type[], System.Reflection.ParameterModifier[]?)"/></item>
		/// <item><see cref="System.Type.GetMethod(string, int, System.Reflection.BindingFlags, System.Reflection.Binder?, System.Type[], System.Reflection.ParameterModifier[]?)"/></item>
		/// <item><see cref="System.Type.GetMethod(string, int, System.Reflection.BindingFlags, System.Reflection.Binder?, System.Reflection.CallingConventions, System.Type[], System.Reflection.ParameterModifier[]?)"/></item>
		/// </list>
		/// </summary>
		Type_GetMethod,
		/// <summary>
		/// <see cref="System.Type.GetMethod(System.Reflection.BindingFlags)"/>
		/// </summary>
		Type_GetMethods__BindingFlags,
		/// <summary>
		/// <list type="table">
		/// <item><see cref="System.Type.GetField(string)"/></item>
		/// <item><see cref="System.Type.GetField(string, System.Reflection.BindingFlags)"/></item>
		/// </list>
		/// </summary>
		Type_GetField,
		/// <summary>
		/// <see cref="System.Type.GetFields(System.Reflection.BindingFlags)"/>
		/// </summary>
		Type_GetFields__BindingFlags,
		/// <summary>
		/// <list type="table">
		/// <item><see cref="System.Type.GetProperty(string)"/></item>
		/// <item><see cref="System.Type.GetProperty(string, System.Reflection.BindingFlags)"/></item>
		/// <item><see cref="System.Type.GetProperty(string, System.Type?)"/></item>
		/// <item><see cref="System.Type.GetProperty(string, System.Type[])"/></item>
		/// <item><see cref="System.Type.GetProperty(string, System.Type?, System.Type[])"/></item>
		/// <item><see cref="System.Type.GetProperty(string, System.Type?, System.Type[], System.Reflection.ParameterModifier[])"/></item>
		/// <item><see cref="System.Type.GetProperty(string, System.Reflection.BindingFlags, System.Reflection.Binder?, System.Type?, System.Type[], System.Reflection.ParameterModifier[]?)"/></item>
		/// </list>
		/// </summary>
		Type_GetProperty,
		/// <summary>
		/// <see cref="System.Type.GetProperties(System.Reflection.BindingFlags)"/>
		/// </summary>
		Type_GetProperties__BindingFlags,
		/// <summary>
		/// <list type="table">
		/// <item><see cref="System.Type.GetEvent(string)"/></item>
		/// <item><see cref="System.Type.GetEvent(string, System.Reflection.BindingFlags)"/></item>
		/// </list>
		/// </summary>
		Type_GetEvent,
		/// <summary>
		/// <see cref="System.Type.GetEvents(System.Reflection.BindingFlags)"/>
		/// </summary>
		Type_GetEvents__BindingFlags,
		/// <summary>
		/// <list type="table">
		/// <item><see cref="System.Type.GetNestedType(string)"/></item>
		/// <item><see cref="System.Type.GetNestedType(string, System.Reflection.BindingFlags)"/></item>
		/// </list>
		/// </summary>
		Type_GetNestedType,
		/// <summary>
		/// <see cref="System.Type.GetNestedTypes(System.Reflection.BindingFlags)"/>
		/// </summary>
		Type_GetNestedTypes__BindingFlags,
		/// <summary>
		/// <list type="table">
		/// <item><see cref="System.Type.GetMember(string)"/></item>
		/// <item><see cref="System.Type.GetMember(string, System.Reflection.BindingFlags)"/></item>
		/// <item><see cref="System.Type.GetMember(string, System.Reflection.MemberTypes, System.Reflection.BindingFlags)"/></item>
		/// </list>
		/// </summary>
		Type_GetMember,
		/// <summary>
		/// <see cref="System.Type.GetMembers(System.Reflection.BindingFlags)"/>
		/// </summary>
		Type_GetMembers__BindingFlags,
		/// <summary>
		/// <list type="table">
		/// <item><see cref="System.Type.GetInterface(string)"/></item>
		/// <item><see cref="System.Type.GetInterface(string, bool)"/></item>
		/// </list>
		/// </summary>
		Type_GetInterface,
		/// <summary>
		/// <see cref="System.Type.AssemblyQualifiedName"/>
		/// </summary>
		Type_get_AssemblyQualifiedName,
		/// <summary>
		/// <see cref="System.Type.UnderlyingSystemType"/>
		/// </summary>
		Type_get_UnderlyingSystemType,
		/// <summary>
		/// <see cref="System.Type.BaseType"/>
		/// </summary>
		Type_get_BaseType,
		/// <summary>
		/// <see cref="System.Linq.Expressions.Expression.Call(System.Type, string, System.Type[]?, System.Linq.Expressions.Expression[]?))"/>
		/// </summary>
		Expression_Call,
		/// <summary>
		/// <see cref="System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression?, System.Type, string)"/>
		/// </summary>
		Expression_Field,
		/// <summary>
		/// <list type="table">
		/// <item><see cref="System.Linq.Expressions.Expression.Property(System.Linq.Expressions.Expression?, System.Reflection.MethodInfo)"/></item>
		/// <item><see cref="System.Linq.Expressions.Expression.Property(System.Linq.Expressions.Expression?, System.Type, string)"/></item>
		/// </list>
		/// </summary>
		Expression_Property,
		/// <summary>
		/// <see cref="System.Linq.Expressions.Expression.New(System.Type)"/>
		/// </summary>
		Expression_New,
		/// <summary>
		/// <see cref="System.Enum.GetValues(System.Type)"/>
		/// </summary>
		Enum_GetValues,
		/// <summary>
		/// <see cref="System.Runtime.InteropServices.Marshal.SizeOf(System.Type)"/>
		/// </summary>
		Marshal_SizeOf,
		/// <summary>
		/// <see cref="System.Runtime.InteropServices.Marshal.OffsetOf(System.Type, string)"/>
		/// </summary>
		Marshal_OffsetOf,
		/// <summary>
		/// <see cref="System.Runtime.InteropServices.Marshal.PtrToStructure(nint, System.Type)"/>
		/// </summary>
		Marshal_PtrToStructure,
		/// <summary>
		/// <see cref="System.Runtime.InteropServices.Marshal.DestroyStructure(nint, System.Type)"/>
		/// </summary>
		Marshal_DestroyStructure,
		/// <summary>
		/// <see cref="System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(nint, System.Type)"/>
		/// </summary>
		Marshal_GetDelegateForFunctionPointer,
		/// <list type="table">
		/// <item><see cref="System.Activator.CreateInstance(System.Type)"/></item>
		/// <item><see cref="System.Activator.CreateInstance(System.Type, bool)"/></item>
		/// <item><see cref="System.Activator.CreateInstance(System.Type, object[])"/></item>
		/// <item><see cref="System.Activator.CreateInstance(System.Type, object[], object[])"/></item>
		/// <item><see cref="System.Activator.CreateInstance(System.Type, System.Reflection.BindingFlags, System.Reflection.Binder, object[], System.Globalization.CultureInfo)"/></item>
		/// <item><see cref="System.Activator.CreateInstance(System.Type, System.Reflection.BindingFlags, System.Reflection.Binder, object[], System.Globalization.CultureInfo, object[])"/></item>
		/// </list>
		Activator_CreateInstance__Type,
		/// <summary>
		/// <list type="table">
		/// <item><see cref="System.Activator.CreateInstance(string, string)"/></item>
		/// <item><see cref="System.Activator.CreateInstance(string, string, bool, System.Reflection.BindingFlags, System.Reflection.Binder, object[], System.Globalization.CultureInfo, object[])"/></item>
		/// <item><see cref="System.Activator.CreateInstance(string, string, object[])"/></item>
		/// </list>
		/// </summary>
		Activator_CreateInstance__AssemblyName_TypeName,
		/// <summary>
		/// <list type="table">
		/// <item><see cref="System.Activator.CreateInstanceFrom(string, string)"/></item>
		/// <item><see cref="System.Activator.CreateInstanceFrom(string, string, bool, System.Reflection.BindingFlags, System.Reflection.Binder, object[], System.Globalization.CultureInfo, object[])"/></item>
		/// <item><see cref="System.Activator.CreateInstanceFrom(string, string, object[])"/></item>
		/// </list>
		/// </summary>
		Activator_CreateInstanceFrom,
		/// <summary>
		/// <list type="table">
		/// <item><see cref="System.AppDomain.CreateInstance(string, string)"/></item>
		/// <item><see cref="System.AppDomain.CreateInstance(string, string, bool, System.Reflection.BindingFlags, System.Reflection.Binder, object[], System.Globalization.CultureInfo, object[])"/></item>
		/// <item><see cref="System.AppDomain.CreateInstance(string, string, object[])"/></item>
		/// </list>
		/// </summary>
		AppDomain_CreateInstance,
		/// <summary>
		/// <list type="table">
		/// <item><see cref="System.AppDomain.CreateInstanceAndUnwrap(string, string)"/></item>
		/// <item><see cref="System.AppDomain.CreateInstanceAndUnwrap(string, string, bool, System.Reflection.BindingFlags, System.Reflection.Binder, object[], System.Globalization.CultureInfo, object[])"/></item>
		/// <item><see cref="System.AppDomain.CreateInstanceAndUnwrap(string, string, object[])"/></item>
		/// </list>
		/// </summary>
		AppDomain_CreateInstanceAndUnwrap,
		/// <summary>
		/// <list type="table">
		/// <item><see cref="System.AppDomain.CreateInstanceFrom(string, string)"/></item>
		/// <item><see cref="System.AppDomain.CreateInstanceFrom(string, string, bool, System.Reflection.BindingFlags, System.Reflection.Binder, object[], System.Globalization.CultureInfo, object[])"/></item>
		/// <item><see cref="System.AppDomain.CreateInstanceFrom(string, string, object[])"/></item>
		/// </list>
		/// </summary>
		AppDomain_CreateInstanceFrom,
		/// <summary>
		/// <list type="table">
		/// <item><see cref="System.AppDomain.CreateInstanceFromAndUnwrap(string, string)"/></item>
		/// <item><see cref="System.AppDomain.CreateInstanceFromAndUnwrap(string, string, bool, System.Reflection.BindingFlags, System.Reflection.Binder, object[], System.Globalization.CultureInfo, object[])"/></item>
		/// <item><see cref="System.AppDomain.CreateInstanceFromAndUnwrap(string, string, object[])"/></item>
		/// </list>
		/// </summary>
		AppDomain_CreateInstanceFromAndUnwrap,
		/// <summary>
		/// <list type="table">
		/// <item><see cref="System.Reflection.Assembly.CreateInstance(string)"/></item>
		/// <item><see cref="System.Reflection.Assembly.CreateInstance(string, bool)"/></item>
		/// <item><see cref="System.Reflection.Assembly.CreateInstance(string, bool, System.Reflection.BindingFlags, System.Reflection.Binder, object[], System.Globalization.CultureInfo, object[])"/></item>
		/// </list>
		/// </summary>
		Assembly_CreateInstance,
		/// <summary>
		/// <see cref="System.Reflection.Assembly.Location"/>
		/// </summary>
		Assembly_get_Location,
		/// <summary>
		/// <see cref="System.Reflection.Assembly.GetFile(string)"/>
		/// </summary>
		Assembly_GetFile,
		/// <summary>
		/// <see cref="System.Reflection.Assembly.GetFiles()"/>
		/// <see cref="System.Reflection.Assembly.GetFiles(bool)"/>
		/// </summary>
		Assembly_GetFiles,
		/// <summary>
		/// <see cref="System.Reflection.AssemblyName.CodeBase"/>
		/// </summary>
		AssemblyName_get_CodeBase,
		/// <summary>
		/// <see cref="System.Reflection.AssemblyName.EscapedCodeBase"/>
		/// </summary>
		AssemblyName_get_EscapedCodeBase,
		/// <summary>
		/// <see cref="System.Reflection.RuntimeReflectionExtensions.GetRuntimeEvent(System.Type, string)"/>
		/// </summary>
		RuntimeReflectionExtensions_GetRuntimeEvent,
		/// <summary>
		/// <see cref="System.Reflection.RuntimeReflectionExtensions.GetRuntimeField(System.Type, string)"/>
		/// </summary>
		RuntimeReflectionExtensions_GetRuntimeField,
		/// <summary>
		/// <see cref="System.Reflection.RuntimeReflectionExtensions.GetRuntimeMethod(System.Type, string, System.Type[])"/>
		/// </summary>
		RuntimeReflectionExtensions_GetRuntimeMethod,
		/// <summary>
		/// <see cref="System.Reflection.RuntimeReflectionExtensions.GetRuntimeProperty(System.Type, string)"/>
		/// </summary>
		RuntimeReflectionExtensions_GetRuntimeProperty,
		/// <summary>
		/// <see cref="System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(System.RuntimeTypeHandle)"/>
		/// </summary>
		RuntimeHelpers_RunClassConstructor,
		/// <summary>
		/// <see cref="System.Reflection.MethodInfo.MakeGenericMethod(System.Type[])"/>
		/// </summary>
		MethodInfo_MakeGenericMethod,
		/// <summary>
		/// <see cref="System.Nullable.GetUnderlyingType(System.Type)"/>
		/// </summary>
		Nullable_GetUnderlyingType
	}
}
