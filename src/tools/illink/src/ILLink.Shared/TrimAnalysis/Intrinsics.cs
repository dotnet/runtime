// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared.TypeSystemProxy;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
	internal static class Intrinsics
	{
		public static IntrinsicId GetIntrinsicIdForMethod (MethodProxy calledMethod)
		{
			return calledMethod.Name switch {
				// static System.Reflection.IntrospectionExtensions.GetTypeInfo (Type type)
				"GetTypeInfo" when calledMethod.IsDeclaredOnType ("System.Reflection.IntrospectionExtensions") => IntrinsicId.IntrospectionExtensions_GetTypeInfo,

				// System.Reflection.TypeInfo.AsType ()
				"AsType" when calledMethod.IsDeclaredOnType ("System.Reflection.TypeInfo") => IntrinsicId.TypeInfo_AsType,

				// System.Type.GetTypeInfo (Type type)
				"GetTypeFromHandle" when calledMethod.IsDeclaredOnType ("System.Type") => IntrinsicId.Type_GetTypeFromHandle,

				// System.Type.TypeHandle getter
				"get_TypeHandle" when calledMethod.IsDeclaredOnType ("System.Type") => IntrinsicId.Type_get_TypeHandle,

				// static System.Reflection.MethodBase.GetMethodFromHandle (RuntimeMethodHandle handle)
				// static System.Reflection.MethodBase.GetMethodFromHandle (RuntimeMethodHandle handle, RuntimeTypeHandle declaringType)
				"GetMethodFromHandle" when calledMethod.IsDeclaredOnType ("System.Reflection.MethodBase")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 0, "System.RuntimeMethodHandle")
					&& (calledMethod.HasMetadataParametersCount (1) || calledMethod.HasMetadataParametersCount (2))
					=> IntrinsicId.MethodBase_GetMethodFromHandle,

				// System.Reflection.MethodBase.MethodHandle getter
				"get_MethodHandle" when calledMethod.IsDeclaredOnType ("System.Reflection.MethodBase") => IntrinsicId.MethodBase_get_MethodHandle,

				// static System.Type.MakeGenericType (Type [] typeArguments)
				"MakeGenericType" when calledMethod.IsDeclaredOnType ("System.Type") => IntrinsicId.Type_MakeGenericType,

				// static System.Reflection.RuntimeReflectionExtensions.GetMethodInfo (this Delegate del)
				"GetMethodInfo" when calledMethod.IsDeclaredOnType ("System.Reflection.RuntimeReflectionExtensions")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 0, "System.Delegate")
					=> IntrinsicId.RuntimeReflectionExtensions_GetMethodInfo,

				// static System.Reflection.RuntimeReflectionExtensions.GetRuntimeEvent (this Type type, string name)
				"GetRuntimeEvent" when calledMethod.IsDeclaredOnType ("System.Reflection.RuntimeReflectionExtensions")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 0, "System.Type")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					=> IntrinsicId.RuntimeReflectionExtensions_GetRuntimeEvent,

				// static System.Reflection.RuntimeReflectionExtensions.GetRuntimeField (this Type type, string name)
				"GetRuntimeField" when calledMethod.IsDeclaredOnType ("System.Reflection.RuntimeReflectionExtensions")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 0, "System.Type")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.,String")
					=> IntrinsicId.RuntimeReflectionExtensions_GetRuntimeField,

				// static System.Reflection.RuntimeReflectionExtensions.GetRuntimeMethod (this Type type, string name, Type[] parameters)
				"GetRuntimeMethod" when calledMethod.IsDeclaredOnType ("System.Reflection.RuntimeReflectionExtensions")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 0, "System.Type")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					=> IntrinsicId.RuntimeReflectionExtensions_GetRuntimeMethod,

				// static System.Reflection.RuntimeReflectionExtensions.GetRuntimeProperty (this Type type, string name)
				"GetRuntimeProperty" when calledMethod.IsDeclaredOnType ("System.Reflection.RuntimeReflectionExtensions")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 0, "System.Type")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					=> IntrinsicId.RuntimeReflectionExtensions_GetRuntimeProperty,

				// static System.Linq.Expressions.Expression.Call (Type, String, Type[], Expression[])
				"Call" when calledMethod.IsDeclaredOnType ("System.Linq.Expressions.Expression")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 0, "System.Type")
					&& calledMethod.HasMetadataParametersCount (4)
					=> IntrinsicId.Expression_Call,

				// static System.Linq.Expressions.Expression.Field (Expression, Type, String)
				"Field" when calledMethod.IsDeclaredOnType ("System.Linq.Expressions.Expression")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.Type")
					&& calledMethod.HasMetadataParametersCount (3)
					=> IntrinsicId.Expression_Field,

				// static System.Linq.Expressions.Expression.Property (Expression, Type, String)
				// static System.Linq.Expressions.Expression.Property (Expression, MethodInfo)
				"Property" when calledMethod.IsDeclaredOnType ("System.Linq.Expressions.Expression")
					&& ((calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.Type") && calledMethod.HasMetadataParametersCount (3))
					|| (calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.Reflection.MethodInfo") && calledMethod.HasMetadataParametersCount (2)))
					=> IntrinsicId.Expression_Property,

				// static System.Linq.Expressions.Expression.New (Type)
				"New" when calledMethod.IsDeclaredOnType ("System.Linq.Expressions.Expression")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 0, "System.Type")
					&& calledMethod.HasMetadataParametersCount (1)
					=> IntrinsicId.Expression_New,

				// static Array System.Enum.GetValues (Type)
				"GetValues" when calledMethod.IsDeclaredOnType ("System.Enum")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 0, "System.Type")
					&& calledMethod.HasMetadataParametersCount (1)
					=> IntrinsicId.Enum_GetValues,

				// static int System.Runtime.InteropServices.Marshal.SizeOf (Type)
				"SizeOf" when calledMethod.IsDeclaredOnType ("System.Runtime.InteropServices.Marshal")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 0, "System.Type")
					&& calledMethod.HasMetadataParametersCount (1)
					=> IntrinsicId.Marshal_SizeOf,

				// static int System.Runtime.InteropServices.Marshal.OffsetOf (Type, string)
				"OffsetOf" when calledMethod.IsDeclaredOnType ("System.Runtime.InteropServices.Marshal")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 0, "System.Type")
					&& calledMethod.HasMetadataParametersCount (2)
					=> IntrinsicId.Marshal_OffsetOf,

				// static object System.Runtime.InteropServices.Marshal.PtrToStructure (IntPtr, Type)
				"PtrToStructure" when calledMethod.IsDeclaredOnType ("System.Runtime.InteropServices.Marshal")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.Type")
					&& calledMethod.HasMetadataParametersCount (2)
					=> IntrinsicId.Marshal_PtrToStructure,

				// static void System.Runtime.InteropServices.Marshal.DestroyStructure (IntPtr, Type)
				"DestroyStructure" when calledMethod.IsDeclaredOnType ("System.Runtime.InteropServices.Marshal")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.Type")
					&& calledMethod.HasMetadataParametersCount (2)
					=> IntrinsicId.Marshal_DestroyStructure,

				// static Delegate System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer (IntPtr, Type)
				"GetDelegateForFunctionPointer" when calledMethod.IsDeclaredOnType ("System.Runtime.InteropServices.Marshal")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.Type")
					&& calledMethod.HasMetadataParametersCount (2)
					=> IntrinsicId.Marshal_GetDelegateForFunctionPointer,

				// static System.Type.GetType (string)
				// static System.Type.GetType (string, Boolean)
				// static System.Type.GetType (string, Boolean, Boolean)
				// static System.Type.GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>)
				// static System.Type.GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean)
				// static System.Type.GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean, Boolean)
				"GetType" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 0, "System.String")
					=> IntrinsicId.Type_GetType,

				// System.Type.GetConstructor (Type[])
				// System.Type.GetConstructor (BindingFlags, Type[])
				// System.Type.GetConstructor (BindingFlags, Binder, Type[], ParameterModifier [])
				// System.Type.GetConstructor (BindingFlags, Binder, CallingConventions, Type[], ParameterModifier [])
				"GetConstructor" when calledMethod.IsDeclaredOnType ("System.Type")
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetConstructor,

				// System.Type.GetConstructors (BindingFlags)
				"GetConstructors" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.Reflection.BindingFlags")
					&& calledMethod.HasMetadataParametersCount (1)
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetConstructors__BindingFlags,

				// System.Type.GetMethod (string)
				// System.Type.GetMethod (string, BindingFlags)
				// System.Type.GetMethod (string, Type[])
				// System.Type.GetMethod (string, Type[], ParameterModifier[])
				// System.Type.GetMethod (string, BindingFlags, Type[])
				// System.Type.GetMethod (string, BindingFlags, Binder, Type[], ParameterModifier[])
				// System.Type.GetMethod (string, BindingFlags, Binder, CallingConventions, Type[], ParameterModifier[])
				// System.Type.GetMethod (string, int, Type[])
				// System.Type.GetMethod (string, int, Type[], ParameterModifier[]?)
				// System.Type.GetMethod (string, int, BindingFlags, Binder?, Type[], ParameterModifier[]?)
				// System.Type.GetMethod (string, int, BindingFlags, Binder?, CallingConventions, Type[], ParameterModifier[]?)
				"GetMethod" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasImplicitThis ()
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					=> IntrinsicId.Type_GetMethod,

				// System.Type.GetMethods (BindingFlags)
				"GetMethods" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasImplicitThis ()
					&& calledMethod.HasMetadataParametersCount (1)
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.Reflection.BindingFlags")
					=> IntrinsicId.Type_GetMethods__BindingFlags,

				// System.Type.GetField (string)
				// System.Type.GetField (string, BindingFlags)
				"GetField" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasImplicitThis ()
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					=> IntrinsicId.Type_GetField,

				// System.Type.GetFields (BindingFlags)
				"GetFields" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasImplicitThis ()
					&& calledMethod.HasMetadataParametersCount (1)
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.Reflection.BindingFlags")
					=> IntrinsicId.Type_GetFields__BindingFlags,

				// System.Type.GetEvent (string)
				// System.Type.GetEvent (string, BindingFlags)
				"GetEvent" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasImplicitThis ()
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					=> IntrinsicId.Type_GetEvent,

				// System.Type.GetEvents (BindingFlags)
				"GetEvents" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasImplicitThis ()
					&& calledMethod.HasMetadataParametersCount (1)
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.Reflection.BindingFlags")
					=> IntrinsicId.Type_GetEvents__BindingFlags,

				// System.Type.GetNestedType (string)
				// System.Type.GetNestedType (string, BindingFlags)
				"GetNestedType" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasImplicitThis ()
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					=> IntrinsicId.Type_GetNestedType,

				// System.Type.GetNestedTypes (BindingFlags)
				"GetNestedTypes" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasImplicitThis ()
					&& calledMethod.HasMetadataParametersCount (1)
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.Reflection.BindingFlags")
					=> IntrinsicId.Type_GetNestedTypes__BindingFlags,

				// System.Type.GetMember (String)
				// System.Type.GetMember (String, BindingFlags)
				// System.Type.GetMember (String, MemberTypes, BindingFlags)
				"GetMember" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasImplicitThis ()
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					&& (calledMethod.HasMetadataParametersCount (1) ||
					(calledMethod.HasMetadataParametersCount (2) && calledMethod.HasParameterOfType ((ParameterIndex) 2, "System.Reflection.BindingFlags")) ||
					(calledMethod.HasMetadataParametersCount (3) && calledMethod.HasParameterOfType ((ParameterIndex) 3, "System.Reflection.BindingFlags")))
					=> IntrinsicId.Type_GetMember,

				// System.Type.GetMembers (BindingFlags)
				"GetMembers" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasImplicitThis ()
					&& calledMethod.HasMetadataParametersCount (1)
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.Reflection.BindingFlags")
					=> IntrinsicId.Type_GetMembers__BindingFlags,

				// System.Type.GetInterface (string)
				// System.Type.GetInterface (string, bool)
				"GetInterface" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasImplicitThis ()
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					&& (calledMethod.HasMetadataParametersCount (1) ||
					(calledMethod.HasMetadataParametersCount (2) && calledMethod.HasParameterOfType ((ParameterIndex) 2, "System.Boolean")))
					=> IntrinsicId.Type_GetInterface,

				// System.Type.AssemblyQualifiedName
				"get_AssemblyQualifiedName" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasImplicitThis ()
					&& !calledMethod.HasMetadataParameters ()
					=> IntrinsicId.Type_get_AssemblyQualifiedName,

				// System.Type.UnderlyingSystemType
				"get_UnderlyingSystemType" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasImplicitThis ()
					&& !calledMethod.HasMetadataParameters ()
					=> IntrinsicId.Type_get_UnderlyingSystemType,

				// System.Type.BaseType
				"get_BaseType" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasImplicitThis ()
					&& !calledMethod.HasMetadataParameters ()
					=> IntrinsicId.Type_get_BaseType,

				// System.Type.GetProperty (string)
				// System.Type.GetProperty (string, BindingFlags)
				// System.Type.GetProperty (string, Type)
				// System.Type.GetProperty (string, Type[])
				// System.Type.GetProperty (string, Type, Type[])
				// System.Type.GetProperty (string, Type, Type[], ParameterModifier[])
				// System.Type.GetProperty (string, BindingFlags, Binder, Type, Type[], ParameterModifier[])
				"GetProperty" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasImplicitThis ()
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					=> IntrinsicId.Type_GetProperty,

				// System.Type.GetProperties (BindingFlags)
				"GetProperties" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasImplicitThis ()
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.Reflection.BindingFlags")
					&& calledMethod.HasMetadataParametersCount (1)
					=> IntrinsicId.Type_GetProperties__BindingFlags,

				// static System.Object.GetType ()
				"GetType" when calledMethod.IsDeclaredOnType ("System.Object")
					=> IntrinsicId.Object_GetType,

				".ctor" when calledMethod.IsDeclaredOnType ("System.Reflection.TypeDelegator")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.Type")
					=> IntrinsicId.TypeDelegator_Ctor,

				"Empty" when calledMethod.IsDeclaredOnType ("System.Array")
					=> IntrinsicId.Array_Empty,

				// static System.Activator.CreateInstance (System.Type type)
				// static System.Activator.CreateInstance (System.Type type, bool nonPublic)
				// static System.Activator.CreateInstance (System.Type type, params object?[]? args)
				// static System.Activator.CreateInstance (System.Type type, object?[]? args, object?[]? activationAttributes)
				// static System.Activator.CreateInstance (System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture)
				// static System.Activator.CreateInstance (System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture, object?[]? activationAttributes) { throw null; }
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System.Activator")
					&& !calledMethod.HasGenericParameters ()
					&& calledMethod.HasParameterOfType ((ParameterIndex) 0, "System.Type")
					=> IntrinsicId.Activator_CreateInstance__Type,

				// static System.Activator.CreateInstance (string assemblyName, string typeName)
				// static System.Activator.CreateInstance (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture, object?[]? activationAttributes)
				// static System.Activator.CreateInstance (string assemblyName, string typeName, object?[]? activationAttributes)
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System.Activator")
					&& !calledMethod.HasGenericParameters ()
					&& calledMethod.HasParameterOfType ((ParameterIndex) 0, "System.String")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					=> IntrinsicId.Activator_CreateInstance__AssemblyName_TypeName,

				// static System.Activator.CreateInstanceFrom (string assemblyFile, string typeName)
				// static System.Activator.CreateInstanceFrom (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// static System.Activator.CreateInstanceFrom (string assemblyFile, string typeName, object? []? activationAttributes)
				"CreateInstanceFrom" when calledMethod.IsDeclaredOnType ("System.Activator")
					&& !calledMethod.HasGenericParameters ()
					&& calledMethod.HasParameterOfType ((ParameterIndex) 0, "System.String")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					=> IntrinsicId.Activator_CreateInstanceFrom,

				// System.AppDomain.CreateInstance (string assemblyName, string typeName)
				// System.AppDomain.CreateInstance (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// System.AppDomain.CreateInstance (string assemblyName, string typeName, object? []? activationAttributes)
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System.AppDomain")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 2, "System.String")
					=> IntrinsicId.AppDomain_CreateInstance,

				// System.AppDomain.CreateInstanceAndUnwrap (string assemblyName, string typeName)
				// System.AppDomain.CreateInstanceAndUnwrap (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// System.AppDomain.CreateInstanceAndUnwrap (string assemblyName, string typeName, object? []? activationAttributes)
				"CreateInstanceAndUnwrap" when calledMethod.IsDeclaredOnType ("System.AppDomain")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 2, "System.String")
					=> IntrinsicId.AppDomain_CreateInstanceAndUnwrap,

				// System.AppDomain.CreateInstanceFrom (string assemblyFile, string typeName)
				// System.AppDomain.CreateInstanceFrom (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// System.AppDomain.CreateInstanceFrom (string assemblyFile, string typeName, object? []? activationAttributes)
				"CreateInstanceFrom" when calledMethod.IsDeclaredOnType ("System.AppDomain")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 2, "System.String")
					=> IntrinsicId.AppDomain_CreateInstanceFrom,

				// System.AppDomain.CreateInstanceFromAndUnwrap (string assemblyFile, string typeName)
				// System.AppDomain.CreateInstanceFromAndUnwrap (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// System.AppDomain.CreateInstanceFromAndUnwrap (string assemblyFile, string typeName, object? []? activationAttributes)
				"CreateInstanceFromAndUnwrap" when calledMethod.IsDeclaredOnType ("System.AppDomain")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 2, "System.String")
					=> IntrinsicId.AppDomain_CreateInstanceFromAndUnwrap,

				// System.Reflection.Assembly.CreateInstance (string typeName)
				// System.Reflection.Assembly.CreateInstance (string typeName, bool ignoreCase)
				// System.Reflection.Assembly.CreateInstance (string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object []? args, CultureInfo? culture, object []? activationAttributes)
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System.Reflection.Assembly")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					=> IntrinsicId.Assembly_CreateInstance,

				// System.Reflection.Assembly.Location getter
				"get_Location" when calledMethod.IsDeclaredOnType ("System.Reflection.Assembly")
					=> IntrinsicId.Assembly_get_Location,

				// System.Reflection.Assembly.GetFile (string)
				"GetFile" when calledMethod.IsDeclaredOnType ("System.Reflection.Assembly")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.String")
					=> IntrinsicId.Assembly_GetFile,

				// System.Reflection.Assembly.GetFiles ()
				// System.Reflection.Assembly.GetFiles (bool)
				"GetFiles" when calledMethod.IsDeclaredOnType ("System.Reflection.Assembly")
					&& (calledMethod.HasMetadataParametersCount (0) || calledMethod.HasParameterOfType ((ParameterIndex) 1, "System.Boolean"))
					=> IntrinsicId.Assembly_GetFiles,

				// System.Reflection.AssemblyName.CodeBase getter
				"get_CodeBase" when calledMethod.IsDeclaredOnType ("System.Reflection.AssemblyName")
					=> IntrinsicId.AssemblyName_get_CodeBase,

				// System.Reflection.AssemblyName.EscapedCodeBase getter
				"get_EscapedCodeBase" when calledMethod.IsDeclaredOnType ("System.Reflection.AssemblyName")
					=> IntrinsicId.AssemblyName_get_EscapedCodeBase,

				// System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor (RuntimeTypeHandle type)
				"RunClassConstructor" when calledMethod.IsDeclaredOnType ("System.Runtime.CompilerServices.RuntimeHelpers")
					&& calledMethod.HasParameterOfType ((ParameterIndex) 0, "System.RuntimeTypeHandle")
					=> IntrinsicId.RuntimeHelpers_RunClassConstructor,

				// System.Reflection.MethodInfo.MakeGenericMethod (Type[] typeArguments)
				"MakeGenericMethod" when calledMethod.IsDeclaredOnType ("System.Reflection.MethodInfo")
					&& calledMethod.HasImplicitThis ()
					&& calledMethod.HasMetadataParametersCount (1)
					=> IntrinsicId.MethodInfo_MakeGenericMethod,

				// static System.Nullable.GetUnderlyingType
				"GetUnderlyingType" when calledMethod.IsDeclaredOnType ("System.Nullable")
					&& calledMethod.IsStatic ()
					&& calledMethod.HasParameterOfType ((ParameterIndex) 0, "System.Type")
					=> IntrinsicId.Nullable_GetUnderlyingType,

				// static System.Delegate.Method getter
				"get_Method" when calledMethod.IsDeclaredOnType ("System.Delegate")
					&& calledMethod.HasImplicitThis ()
					&& calledMethod.HasMetadataParametersCount (0)
					=> IntrinsicId.Delegate_get_Method,

				_ => IntrinsicId.None,
			};
		}
	}
}
