// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Steps;

using BindingFlags = System.Reflection.BindingFlags;

namespace Mono.Linker.Dataflow
{
	class ReflectionMethodBodyScanner : MethodBodyScanner
	{
		readonly LinkContext _context;
		readonly MarkStep _markStep;

		public static bool RequiresReflectionMethodBodyScannerForCallSite (LinkContext context, MethodReference calledMethod)
		{
			MethodDefinition methodDefinition = calledMethod.Resolve ();
			if (methodDefinition != null) {
				return
					GetIntrinsicIdForMethod (methodDefinition) > IntrinsicId.RequiresReflectionBodyScanner_Sentinel ||
					context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (methodDefinition) ||
					context.Annotations.HasLinkerAttribute<RequiresUnreferencedCodeAttribute> (methodDefinition);
			}

			return false;
		}

		public static bool RequiresReflectionMethodBodyScannerForMethodBody (FlowAnnotations flowAnnotations, MethodReference method)
		{
			MethodDefinition methodDefinition = method.Resolve ();
			if (methodDefinition != null) {
				return
					GetIntrinsicIdForMethod (methodDefinition) > IntrinsicId.RequiresReflectionBodyScanner_Sentinel ||
					flowAnnotations.RequiresDataFlowAnalysis (methodDefinition);
			}

			return false;
		}

		public static bool RequiresReflectionMethodBodyScannerForAccess (FlowAnnotations flowAnnotations, FieldReference field)
		{
			FieldDefinition fieldDefinition = field.Resolve ();
			if (fieldDefinition != null)
				return flowAnnotations.RequiresDataFlowAnalysis (fieldDefinition);

			return false;
		}

		bool ShouldEnableReflectionPatternReporting (MethodDefinition method)
		{
			return !_context.Annotations.HasLinkerAttribute<RequiresUnreferencedCodeAttribute> (method);
		}

		public ReflectionMethodBodyScanner (LinkContext context, MarkStep parent)
		{
			_context = context;
			_markStep = parent;
		}

		public void ScanAndProcessReturnValue (MethodBody methodBody)
		{
			Scan (methodBody);

			if (methodBody.Method.ReturnType.MetadataType != MetadataType.Void) {
				var method = methodBody.Method;
				var requiredMemberTypes = _context.Annotations.FlowAnnotations.GetReturnParameterAnnotation (method);
				if (requiredMemberTypes != 0) {
					var reflectionContext = new ReflectionPatternContext (_context, ShouldEnableReflectionPatternReporting (method), method, method.MethodReturnType);
					reflectionContext.AnalyzingPattern ();
					RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberTypes, MethodReturnValue, method.MethodReturnType);
				}
			}
		}

		public void ProcessAttributeDataflow (IMemberDefinition source, MethodDefinition method, IList<CustomAttributeArgument> arguments)
		{
			int paramOffset = method.HasImplicitThis () ? 1 : 0;

			for (int i = 0; i < method.Parameters.Count; i++) {
				var annotation = _context.Annotations.FlowAnnotations.GetParameterAnnotation (method, i + paramOffset);
				if (annotation != DynamicallyAccessedMemberTypes.None) {
					ValueNode valueNode = GetValueNodeForCustomAttributeArgument (arguments[i]);
					var methodParameter = method.Parameters[i];
					var reflectionContext = new ReflectionPatternContext (_context, true, source, methodParameter);
					reflectionContext.AnalyzingPattern ();
					RequireDynamicallyAccessedMembers (ref reflectionContext, annotation, valueNode, methodParameter);
				}
			}
		}

		public void ProcessAttributeDataflow (IMemberDefinition source, FieldDefinition field, CustomAttributeArgument value)
		{
			var annotation = _context.Annotations.FlowAnnotations.GetFieldAnnotation (field);
			Debug.Assert (annotation != DynamicallyAccessedMemberTypes.None);

			ValueNode valueNode = GetValueNodeForCustomAttributeArgument (value);
			var reflectionContext = new ReflectionPatternContext (_context, true, source, field);
			reflectionContext.AnalyzingPattern ();
			RequireDynamicallyAccessedMembers (ref reflectionContext, annotation, valueNode, field);
		}

		static ValueNode GetValueNodeForCustomAttributeArgument (CustomAttributeArgument argument)
		{
			ValueNode valueNode;
			if (argument.Type.Name == "Type") {
				TypeDefinition referencedType = ((TypeReference) argument.Value).ResolveToMainTypeDefinition ();
				if (referencedType == null)
					valueNode = UnknownValue.Instance;
				else
					valueNode = new SystemTypeValue (referencedType);
			} else if (argument.Type.MetadataType == MetadataType.String) {
				valueNode = new KnownStringValue ((string) argument.Value);
			} else {
				// We shouldn't have gotten a non-null annotation for this from GetParameterAnnotation
				throw new InvalidOperationException ();
			}

			Debug.Assert (valueNode != null);
			return valueNode;
		}

		public void ProcessGenericArgumentDataFlow (GenericParameter genericParameter, TypeReference genericArgument, IMemberDefinition source)
		{
			var annotation = _context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (genericParameter);
			Debug.Assert (annotation != DynamicallyAccessedMemberTypes.None);

			ValueNode valueNode = GetTypeValueNodeFromGenericArgument (genericArgument);
			bool enableReflectionPatternReporting = !(source is MethodDefinition sourceMethod) || ShouldEnableReflectionPatternReporting (sourceMethod);

			var reflectionContext = new ReflectionPatternContext (_context, enableReflectionPatternReporting, source, genericParameter);
			reflectionContext.AnalyzingPattern ();
			RequireDynamicallyAccessedMembers (ref reflectionContext, annotation, valueNode, genericParameter);
		}

		ValueNode GetTypeValueNodeFromGenericArgument (TypeReference genericArgument)
		{
			if (genericArgument is GenericParameter inputGenericParameter) {
				// Technically this should be a new value node type as it's not a System.Type instance representation, but just the generic parameter
				// That said we only use it to perform the dynamically accessed members checks and for that purpose treating it as System.Type is perfectly valid.
				return new SystemTypeForGenericParameterValue (inputGenericParameter, _context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (inputGenericParameter));
			} else {
				TypeDefinition genericArgumentTypeDef = genericArgument.ResolveToMainTypeDefinition ();
				if (genericArgumentTypeDef != null) {
					return new SystemTypeValue (genericArgumentTypeDef);
				} else {
					// If we can't resolve the generic argument, it means we can't apply potential requirements on it
					// so track it as unknown value. If we later on hit this unknown value as being used somewhere
					// where we need to apply requirements on it, it will generate a warning.
					return UnknownValue.Instance;
				}
			}
		}

		protected override void WarnAboutInvalidILInMethod (MethodBody method, int ilOffset)
		{
			// Serves as a debug helper to make sure valid IL is not considered invalid.
			//
			// The .NET Native compiler used to warn if it detected invalid IL during treeshaking,
			// but the warnings were often triggered in autogenerated dead code of a major game engine
			// and resulted in support calls. No point in warning. If the code gets exercised at runtime,
			// an InvalidProgramException will likely be raised.
			Debug.Fail ("Invalid IL or a bug in the scanner");
		}

		protected override ValueNode GetMethodParameterValue (MethodDefinition method, int parameterIndex)
		{
			DynamicallyAccessedMemberTypes memberTypes = _context.Annotations.FlowAnnotations.GetParameterAnnotation (method, parameterIndex);
			return new MethodParameterValue (parameterIndex, memberTypes, DiagnosticUtilities.GetMethodParameterFromIndex (method, parameterIndex));
		}

		protected override ValueNode GetFieldValue (MethodDefinition method, FieldDefinition field)
		{
			switch (field.Name) {
			case "EmptyTypes" when field.DeclaringType.IsTypeOf ("System", "Type"): {
					return new ArrayValue (new ConstIntValue (0));
				}
			case "Empty" when field.DeclaringType.IsTypeOf ("System", "String"): {
					return new KnownStringValue (string.Empty);
				}

			default: {
					DynamicallyAccessedMemberTypes memberTypes = _context.Annotations.FlowAnnotations.GetFieldAnnotation (field);
					return new LoadFieldValue (field, memberTypes);
				}
			}
		}

		protected override void HandleStoreField (MethodDefinition method, FieldDefinition field, Instruction operation, ValueNode valueToStore)
		{
			var requiredMemberTypes = _context.Annotations.FlowAnnotations.GetFieldAnnotation (field);
			if (requiredMemberTypes != 0) {
				var reflectionContext = new ReflectionPatternContext (_context, ShouldEnableReflectionPatternReporting (method), method, field, operation);
				reflectionContext.AnalyzingPattern ();
				RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberTypes, valueToStore, field);
			}
		}

		protected override void HandleStoreParameter (MethodDefinition method, int index, Instruction operation, ValueNode valueToStore)
		{
			var requiredMemberTypes = _context.Annotations.FlowAnnotations.GetParameterAnnotation (method, index);
			if (requiredMemberTypes != 0) {
				ParameterDefinition parameter = method.Parameters[index - (method.HasImplicitThis () ? 1 : 0)];
				var reflectionContext = new ReflectionPatternContext (_context, ShouldEnableReflectionPatternReporting (method), method, parameter, operation);
				reflectionContext.AnalyzingPattern ();
				RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberTypes, valueToStore, parameter);
			}
		}

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
			Type_get_AssemblyQualifiedName,
			Type_get_UnderlyingSystemType,
			Expression_Call,
			Expression_Field,
			Expression_Property,
			Expression_New,
			Activator_CreateInstance_Type,
			Activator_CreateInstance_AssemblyName_TypeName,
			Activator_CreateInstanceFrom,
			Activator_CreateInstanceOfT,
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
		}

		static IntrinsicId GetIntrinsicIdForMethod (MethodDefinition calledMethod)
		{
			return calledMethod.Name switch {
				// static System.Reflection.IntrospectionExtensions.GetTypeInfo (Type type)
				"GetTypeInfo" when calledMethod.IsDeclaredOnType ("System.Reflection", "IntrospectionExtensions") => IntrinsicId.IntrospectionExtensions_GetTypeInfo,

				// System.Reflection.TypeInfo.AsType ()
				"AsType" when calledMethod.IsDeclaredOnType ("System.Reflection", "TypeInfo") => IntrinsicId.TypeInfo_AsType,

				// System.Type.GetTypeInfo (Type type)
				"GetTypeFromHandle" when calledMethod.IsDeclaredOnType ("System", "Type") => IntrinsicId.Type_GetTypeFromHandle,

				// System.Type.GetTypeHandle (Type type)
				"get_TypeHandle" when calledMethod.IsDeclaredOnType ("System", "Type") => IntrinsicId.Type_get_TypeHandle,

				// static System.Type.MakeGenericType (Type [] typeArguments)
				"MakeGenericType" when calledMethod.IsDeclaredOnType ("System", "Type") => IntrinsicId.Type_MakeGenericType,

				// static System.Reflection.RuntimeReflectionExtensions.GetRuntimeEvent (this Type type, string name)
				"GetRuntimeEvent" when calledMethod.IsDeclaredOnType ("System.Reflection", "RuntimeReflectionExtensions")
					&& calledMethod.HasParameterOfType (0, "System", "Type")
					&& calledMethod.HasParameterOfType (1, "System", "String")
					=> IntrinsicId.RuntimeReflectionExtensions_GetRuntimeEvent,

				// static System.Reflection.RuntimeReflectionExtensions.GetRuntimeField (this Type type, string name)
				"GetRuntimeField" when calledMethod.IsDeclaredOnType ("System.Reflection", "RuntimeReflectionExtensions")
					&& calledMethod.HasParameterOfType (0, "System", "Type")
					&& calledMethod.HasParameterOfType (1, "System", "String")
					=> IntrinsicId.RuntimeReflectionExtensions_GetRuntimeField,

				// static System.Reflection.RuntimeReflectionExtensions.GetRuntimeMethod (this Type type, string name, Type[] parameters)
				"GetRuntimeMethod" when calledMethod.IsDeclaredOnType ("System.Reflection", "RuntimeReflectionExtensions")
					&& calledMethod.HasParameterOfType (0, "System", "Type")
					&& calledMethod.HasParameterOfType (1, "System", "String")
					=> IntrinsicId.RuntimeReflectionExtensions_GetRuntimeMethod,

				// static System.Reflection.RuntimeReflectionExtensions.GetRuntimeProperty (this Type type, string name)
				"GetRuntimeProperty" when calledMethod.IsDeclaredOnType ("System.Reflection", "RuntimeReflectionExtensions")
					&& calledMethod.HasParameterOfType (0, "System", "Type")
					&& calledMethod.HasParameterOfType (1, "System", "String")
					=> IntrinsicId.RuntimeReflectionExtensions_GetRuntimeProperty,

				// static System.Linq.Expressions.Expression.Call (Type, String, Type[], Expression[])
				"Call" when calledMethod.IsDeclaredOnType ("System.Linq.Expressions", "Expression")
					&& calledMethod.HasParameterOfType (0, "System", "Type")
					&& calledMethod.Parameters.Count == 4
					=> IntrinsicId.Expression_Call,

				// static System.Linq.Expressions.Expression.Field (Expression, Type, String)
				"Field" when calledMethod.IsDeclaredOnType ("System.Linq.Expressions", "Expression")
					&& calledMethod.HasParameterOfType (1, "System", "Type")
					&& calledMethod.Parameters.Count == 3
					=> IntrinsicId.Expression_Field,

				// static System.Linq.Expressions.Expression.Property (Expression, Type, String)
				"Property" when calledMethod.IsDeclaredOnType ("System.Linq.Expressions", "Expression")
					&& calledMethod.HasParameterOfType (1, "System", "Type")
					&& calledMethod.Parameters.Count == 3
					=> IntrinsicId.Expression_Property,

				// static System.Linq.Expressions.Expression.New (Type)
				"New" when calledMethod.IsDeclaredOnType ("System.Linq.Expressions", "Expression")
					&& calledMethod.HasParameterOfType (0, "System", "Type")
					&& calledMethod.Parameters.Count == 1
					=> IntrinsicId.Expression_New,

				// static System.Type.GetType (string)
				// static System.Type.GetType (string, Boolean)
				// static System.Type.GetType (string, Boolean, Boolean)
				// static System.Type.GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>)
				// static System.Type.GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean)
				// static System.Type.GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean, Boolean)
				"GetType" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& calledMethod.HasParameterOfType (0, "System", "String")
					=> IntrinsicId.Type_GetType,

				// System.Type.GetConstructor (Type[])
				// System.Type.GetConstructor (BindingFlags, Binder, Type[], ParameterModifier [])
				// System.Type.GetConstructor (BindingFlags, Binder, CallingConventions, Type[], ParameterModifier [])
				"GetConstructor" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& calledMethod.HasThis
					=> IntrinsicId.Type_GetConstructor,

				// System.Type.GetConstructors (BindingFlags)
				"GetConstructors" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& calledMethod.HasParameterOfType (0, "System.Reflection", "BindingFlags")
					&& calledMethod.Parameters.Count == 1
					&& calledMethod.HasThis
					=> IntrinsicId.Type_GetConstructors,

				// System.Type.GetMethod (string)
				// System.Type.GetMethod (string, BindingFlags)
				// System.Type.GetMethod (string, Type[])
				// System.Type.GetMethod (string, Type[], ParameterModifier[])
				// System.Type.GetMethod (string, BindingFlags, Binder, Type[], ParameterModifier[]) 6
				// System.Type.GetMethod (string, BindingFlags, Binder, CallingConventions, Type[], ParameterModifier[]) 7
				// System.Type.GetMethod (string, int, Type[])
				// System.Type.GetMethod (string, int, Type[], ParameterModifier[]?)
				// System.Type.GetMethod (string, int, BindingFlags, Binder?, Type[], ParameterModifier[]?)
				// System.Type.GetMethod (string, int, BindingFlags, Binder?, CallingConventions, Type[], ParameterModifier[]?)
				"GetMethod" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& calledMethod.HasParameterOfType (0, "System", "String")
					&& calledMethod.HasThis
					=> IntrinsicId.Type_GetMethod,

				// System.Type.GetMethods (BindingFlags)
				"GetMethods" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& calledMethod.HasParameterOfType (0, "System.Reflection", "BindingFlags")
					&& calledMethod.Parameters.Count == 1
					&& calledMethod.HasThis
					=> IntrinsicId.Type_GetMethods,

				// System.Type.GetField (string)
				// System.Type.GetField (string, BindingFlags)
				"GetField" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& calledMethod.HasParameterOfType (0, "System", "String")
					&& calledMethod.HasThis
					=> IntrinsicId.Type_GetField,

				// System.Type.GetFields (BindingFlags)
				"GetFields" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& calledMethod.HasParameterOfType (0, "System.Reflection", "BindingFlags")
					&& calledMethod.Parameters.Count == 1
					&& calledMethod.HasThis
					=> IntrinsicId.Type_GetFields,

				// System.Type.GetEvent (string)
				// System.Type.GetEvent (string, BindingFlags)
				"GetEvent" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& calledMethod.HasParameterOfType (0, "System", "String")
					&& calledMethod.HasThis
					=> IntrinsicId.Type_GetEvent,

				// System.Type.GetEvents (BindingFlags)
				"GetEvents" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& calledMethod.HasParameterOfType (0, "System.Reflection", "BindingFlags")
					&& calledMethod.Parameters.Count == 1
					&& calledMethod.HasThis
					=> IntrinsicId.Type_GetEvents,

				// System.Type.GetNestedType (string)
				// System.Type.GetNestedType (string, BindingFlags)
				"GetNestedType" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& calledMethod.HasParameterOfType (0, "System", "String")
					&& calledMethod.HasThis
					=> IntrinsicId.Type_GetNestedType,

				// System.Type.GetNestedTypes (BindingFlags)
				"GetNestedTypes" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& calledMethod.HasParameterOfType (0, "System.Reflection", "BindingFlags")
					&& calledMethod.Parameters.Count == 1
					&& calledMethod.HasThis
					=> IntrinsicId.Type_GetNestedTypes,

				// System.Type.GetMember (String)
				// System.Type.GetMember (String, BindingFlags)
				// System.Type.GetMember (String, MemberTypes, BindingFlags)
				"GetMember" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& calledMethod.HasParameterOfType (0, "System", "String")
					&& calledMethod.HasThis
					&& (calledMethod.Parameters.Count == 1 ||
					(calledMethod.Parameters.Count == 2 && calledMethod.HasParameterOfType (1, "System.Reflection", "BindingFlags")) ||
					(calledMethod.Parameters.Count == 3 && calledMethod.HasParameterOfType (2, "System.Reflection", "BindingFlags")))
					=> IntrinsicId.Type_GetMember,

				// System.Type.GetMembers (BindingFlags)
				"GetMembers" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& calledMethod.HasParameterOfType (0, "System.Reflection", "BindingFlags")
					&& calledMethod.Parameters.Count == 1
					&& calledMethod.HasThis
					=> IntrinsicId.Type_GetMembers,

				// System.Type.AssemblyQualifiedName
				"get_AssemblyQualifiedName" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& !calledMethod.HasParameters
					&& calledMethod.HasThis
					=> IntrinsicId.Type_get_AssemblyQualifiedName,

				// System.Type.UnderlyingSystemType
				"get_UnderlyingSystemType" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& !calledMethod.HasParameters
					&& calledMethod.HasThis
					=> IntrinsicId.Type_get_UnderlyingSystemType,

				// System.Type.GetProperty (string)
				// System.Type.GetProperty (string, BindingFlags)
				// System.Type.GetProperty (string, Type)
				// System.Type.GetProperty (string, Type[])
				// System.Type.GetProperty (string, Type, Type[])
				// System.Type.GetProperty (string, Type, Type[], ParameterModifier[])
				// System.Type.GetProperty (string, BindingFlags, Binder, Type, Type[], ParameterModifier[])
				"GetProperty" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& calledMethod.HasParameterOfType (0, "System", "String")
					&& calledMethod.HasThis
					=> IntrinsicId.Type_GetProperty,

				// System.Type.GetProperties (BindingFlags)
				"GetProperties" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& calledMethod.HasParameterOfType (0, "System.Reflection", "BindingFlags")
					&& calledMethod.Parameters.Count == 1
					&& calledMethod.HasThis
					=> IntrinsicId.Type_GetProperties,

				// static System.Object.GetType ()
				"GetType" when calledMethod.IsDeclaredOnType ("System", "Object")
					=> IntrinsicId.Object_GetType,

				".ctor" when calledMethod.IsDeclaredOnType ("System.Reflection", "TypeDelegator")
					&& calledMethod.HasParameterOfType (0, "System", "Type")
					=> IntrinsicId.TypeDelegator_Ctor,

				"Empty" when calledMethod.IsDeclaredOnType ("System", "Array")
					=> IntrinsicId.Array_Empty,

				// static System.Activator.CreateInstance (System.Type type)
				// static System.Activator.CreateInstance (System.Type type, bool nonPublic)
				// static System.Activator.CreateInstance (System.Type type, params object?[]? args)
				// static System.Activator.CreateInstance (System.Type type, object?[]? args, object?[]? activationAttributes)
				// static System.Activator.CreateInstance (System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture)
				// static System.Activator.CreateInstance (System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture, object?[]? activationAttributes) { throw null; }
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System", "Activator")
					&& !calledMethod.ContainsGenericParameter
					&& calledMethod.HasParameterOfType (0, "System", "Type")
					=> IntrinsicId.Activator_CreateInstance_Type,

				// static System.Activator.CreateInstance (string assemblyName, string typeName)
				// static System.Activator.CreateInstance (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture, object?[]? activationAttributes)
				// static System.Activator.CreateInstance (string assemblyName, string typeName, object?[]? activationAttributes)
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System", "Activator")
					&& !calledMethod.ContainsGenericParameter
					&& calledMethod.HasParameterOfType (0, "System", "String")
					&& calledMethod.HasParameterOfType (1, "System", "String")
					=> IntrinsicId.Activator_CreateInstance_AssemblyName_TypeName,

				// static System.Activator.CreateInstanceFrom (string assemblyFile, string typeName)
				// static System.Activator.CreateInstanceFrom (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// static System.Activator.CreateInstanceFrom (string assemblyFile, string typeName, object? []? activationAttributes)
				"CreateInstanceFrom" when calledMethod.IsDeclaredOnType ("System", "Activator")
					&& !calledMethod.ContainsGenericParameter
					&& calledMethod.HasParameterOfType (0, "System", "String")
					&& calledMethod.HasParameterOfType (1, "System", "String")
					=> IntrinsicId.Activator_CreateInstanceFrom,

				// static T System.Activator.CreateInstance<T> ()
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System", "Activator")
					&& calledMethod.ContainsGenericParameter
					&& calledMethod.GenericParameters.Count == 1
					&& calledMethod.Parameters.Count == 0
					=> IntrinsicId.Activator_CreateInstanceOfT,

				// System.AppDomain.CreateInstance (string assemblyName, string typeName)
				// System.AppDomain.CreateInstance (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// System.AppDomain.CreateInstance (string assemblyName, string typeName, object? []? activationAttributes)
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System", "AppDomain")
					&& calledMethod.HasParameterOfType (0, "System", "String")
					&& calledMethod.HasParameterOfType (1, "System", "String")
					=> IntrinsicId.AppDomain_CreateInstance,

				// System.AppDomain.CreateInstanceAndUnwrap (string assemblyName, string typeName)
				// System.AppDomain.CreateInstanceAndUnwrap (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// System.AppDomain.CreateInstanceAndUnwrap (string assemblyName, string typeName, object? []? activationAttributes)
				"CreateInstanceAndUnwrap" when calledMethod.IsDeclaredOnType ("System", "AppDomain")
					&& calledMethod.HasParameterOfType (0, "System", "String")
					&& calledMethod.HasParameterOfType (1, "System", "String")
					=> IntrinsicId.AppDomain_CreateInstanceAndUnwrap,

				// System.AppDomain.CreateInstanceFrom (string assemblyFile, string typeName)
				// System.AppDomain.CreateInstanceFrom (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// System.AppDomain.CreateInstanceFrom (string assemblyFile, string typeName, object? []? activationAttributes)
				"CreateInstanceFrom" when calledMethod.IsDeclaredOnType ("System", "AppDomain")
					&& calledMethod.HasParameterOfType (0, "System", "String")
					&& calledMethod.HasParameterOfType (1, "System", "String")
					=> IntrinsicId.AppDomain_CreateInstanceFrom,

				// System.AppDomain.CreateInstanceFromAndUnwrap (string assemblyFile, string typeName)
				// System.AppDomain.CreateInstanceFromAndUnwrap (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// System.AppDomain.CreateInstanceFromAndUnwrap (string assemblyFile, string typeName, object? []? activationAttributes)
				"CreateInstanceFromAndUnwrap" when calledMethod.IsDeclaredOnType ("System", "AppDomain")
					&& calledMethod.HasParameterOfType (0, "System", "String")
					&& calledMethod.HasParameterOfType (1, "System", "String")
					=> IntrinsicId.AppDomain_CreateInstanceFromAndUnwrap,

				// System.Reflection.Assembly.CreateInstance (string typeName)
				// System.Reflection.Assembly.CreateInstance (string typeName, bool ignoreCase)
				// System.Reflection.Assembly.CreateInstance (string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object []? args, CultureInfo? culture, object []? activationAttributes)
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System.Reflection", "Assembly")
					&& calledMethod.HasParameterOfType (0, "System", "String")
					=> IntrinsicId.Assembly_CreateInstance,

				// System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor (RuntimeTypeHandle type)
				"RunClassConstructor" when calledMethod.IsDeclaredOnType ("System.Runtime.CompilerServices", "RuntimeHelpers")
					&& calledMethod.HasParameterOfType (0, "System", "RuntimeTypeHandle")
					=> IntrinsicId.RuntimeHelpers_RunClassConstructor,

				// System.Reflection.MethodInfo.MakeGenericMethod (Type[] typeArguments)
				"MakeGenericMethod" when calledMethod.IsDeclaredOnType ("System.Reflection", "MethodInfo")
					&& calledMethod.HasThis
					&& calledMethod.Parameters.Count == 1
					=> IntrinsicId.MethodInfo_MakeGenericMethod,

				_ => IntrinsicId.None,
			};
		}

		public override bool HandleCall (MethodBody callingMethodBody, MethodReference calledMethod, Instruction operation, ValueNodeList methodParams, out ValueNode methodReturnValue)
		{
			methodReturnValue = null;

			var reflectionProcessed = _markStep.ProcessReflectionDependency (callingMethodBody, operation);
			if (reflectionProcessed)
				return false;

			var callingMethodDefinition = callingMethodBody.Method;
			var reflectionContext = new ReflectionPatternContext (
				_context,
				ShouldEnableReflectionPatternReporting (callingMethodDefinition),
				callingMethodDefinition,
				calledMethod.Resolve (),
				operation);

			DynamicallyAccessedMemberTypes returnValueDynamicallyAccessedMemberTypes = 0;

			var calledMethodDefinition = calledMethod.Resolve ();
			if (calledMethodDefinition == null)
				return false;

			try {

				bool requiresDataFlowAnalysis = _context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (calledMethodDefinition);
				returnValueDynamicallyAccessedMemberTypes = requiresDataFlowAnalysis ?
					_context.Annotations.FlowAnnotations.GetReturnParameterAnnotation (calledMethodDefinition) : 0;

				switch (GetIntrinsicIdForMethod (calledMethodDefinition)) {
				case IntrinsicId.IntrospectionExtensions_GetTypeInfo: {
						// typeof(Foo).GetTypeInfo()... will be commonly present in code targeting
						// the dead-end reflection refactoring. The call doesn't do anything and we
						// don't want to lose the annotation.
						methodReturnValue = methodParams[0];
					}
					break;

				case IntrinsicId.TypeInfo_AsType: {
						// someType.AsType()... will be commonly present in code targeting
						// the dead-end reflection refactoring. The call doesn't do anything and we
						// don't want to lose the annotation.
						methodReturnValue = methodParams[0];
					}
					break;

				case IntrinsicId.TypeDelegator_Ctor: {
						// This is an identity function for analysis purposes
						if (operation.OpCode == OpCodes.Newobj)
							methodReturnValue = methodParams[1];
					}
					break;

				case IntrinsicId.Array_Empty: {
						methodReturnValue = new ArrayValue (new ConstIntValue (0));
					}
					break;

				case IntrinsicId.Type_GetTypeFromHandle: {
						// Infrastructure piece to support "typeof(Foo)"
						if (methodParams[0] is RuntimeTypeHandleValue typeHandle)
							methodReturnValue = new SystemTypeValue (typeHandle.TypeRepresented);
						else if (methodParams[0] is RuntimeTypeHandleForGenericParameterValue typeHandleForGenericParameter) {
							methodReturnValue = new SystemTypeForGenericParameterValue (
								typeHandleForGenericParameter.GenericParameter,
								_context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (typeHandleForGenericParameter.GenericParameter));
						}
					}
					break;

				case IntrinsicId.Type_get_TypeHandle: {
						foreach (var value in methodParams[0].UniqueValues ()) {
							if (value is SystemTypeValue typeValue)
								methodReturnValue = MergePointValue.MergeValues (methodReturnValue, new RuntimeTypeHandleValue (typeValue.TypeRepresented));
							else if (value == NullValue.Instance)
								methodReturnValue = MergePointValue.MergeValues (methodReturnValue, value);
							else
								methodReturnValue = MergePointValue.MergeValues (methodReturnValue, UnknownValue.Instance);
						}
					}
					break;

				case IntrinsicId.Type_MakeGenericType: {
						reflectionContext.AnalyzingPattern ();
						foreach (var value in methodParams[0].UniqueValues ()) {
							if (value is SystemTypeValue typeValue) {
								foreach (var genericParameter in typeValue.TypeRepresented.GenericParameters) {
									if (_context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (genericParameter) != DynamicallyAccessedMemberTypes.None) {
										// There is a generic parameter which has some requirements on the input types.
										// For now we don't support tracking actual array elements, so we can't validate that the requirements are fulfilled.
										reflectionContext.RecordUnrecognizedPattern (
											2055,
											$"Call to '{calledMethodDefinition.GetDisplayName ()}' can not be statically analyzed. " +
											$"It's not possible to guarantee the availability of requirements of the generic type.");
									}
								}

								// We haven't found any generic parameters with annotations, so there's nothing to validate.
								reflectionContext.RecordHandledPattern ();
							} else if (value == NullValue.Instance)
								reflectionContext.RecordHandledPattern ();
							else {
								// We have no way to "include more" to fix this if we don't know, so we have to warn
								reflectionContext.RecordUnrecognizedPattern (
									2055,
									$"Call to '{calledMethodDefinition.GetDisplayName ()}' can not be statically analyzed. " +
									$"It's not possible to guarantee the availability of requirements of the generic type.");
							}
						}

						// We don't want to lose track of the type
						// in case this is e.g. Activator.CreateInstance(typeof(Foo<>).MakeGenericType(...));
						methodReturnValue = methodParams[0];
					}
					break;

				//
				// System.Reflection.RuntimeReflectionExtensions
				//
				// static GetRuntimeEvent (this Type type, string name)
				// static GetRuntimeField (this Type type, string name)
				// static GetRuntimeMethod (this Type type, string name, Type[] parameters)
				// static GetRuntimeProperty (this Type type, string name)
				//
				case var getRuntimeMember when getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeEvent
					|| getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeField
					|| getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeMethod
					|| getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeProperty: {

						reflectionContext.AnalyzingPattern ();
						BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
						DynamicallyAccessedMemberTypes requiredMemberTypes = getRuntimeMember switch {
							IntrinsicId.RuntimeReflectionExtensions_GetRuntimeEvent => DynamicallyAccessedMemberTypes.PublicEvents,
							IntrinsicId.RuntimeReflectionExtensions_GetRuntimeField => DynamicallyAccessedMemberTypes.PublicFields,
							IntrinsicId.RuntimeReflectionExtensions_GetRuntimeMethod => DynamicallyAccessedMemberTypes.PublicMethods,
							IntrinsicId.RuntimeReflectionExtensions_GetRuntimeProperty => DynamicallyAccessedMemberTypes.PublicProperties,
							_ => throw new InternalErrorException ($"Reflection call '{calledMethod.GetDisplayName ()}' inside '{callingMethodDefinition.GetDisplayName ()}' is of unexpected member type."),
						};

						foreach (var value in methodParams[0].UniqueValues ()) {
							if (value is SystemTypeValue systemTypeValue) {
								foreach (var stringParam in methodParams[1].UniqueValues ()) {
									if (stringParam is KnownStringValue stringValue) {
										switch (getRuntimeMember) {
										case IntrinsicId.RuntimeReflectionExtensions_GetRuntimeEvent:
											MarkEventsOnTypeHierarchy (ref reflectionContext, systemTypeValue.TypeRepresented, e => e.Name == stringValue.Contents, bindingFlags);
											reflectionContext.RecordHandledPattern ();
											break;
										case IntrinsicId.RuntimeReflectionExtensions_GetRuntimeField:
											MarkFieldsOnTypeHierarchy (ref reflectionContext, systemTypeValue.TypeRepresented, f => f.Name == stringValue.Contents, bindingFlags);
											reflectionContext.RecordHandledPattern ();
											break;
										case IntrinsicId.RuntimeReflectionExtensions_GetRuntimeMethod:
											MarkMethodsOnTypeHierarchy (ref reflectionContext, systemTypeValue.TypeRepresented, m => m.Name == stringValue.Contents, bindingFlags);
											reflectionContext.RecordHandledPattern ();
											break;
										case IntrinsicId.RuntimeReflectionExtensions_GetRuntimeProperty:
											MarkPropertiesOnTypeHierarchy (ref reflectionContext, systemTypeValue.TypeRepresented, p => p.Name == stringValue.Contents, bindingFlags);
											reflectionContext.RecordHandledPattern ();
											break;
										default:
											throw new InternalErrorException ($"Error processing reflection call '{calledMethod.GetDisplayName ()}' inside {callingMethodDefinition.GetDisplayName ()}. Unexpected member kind.");
										}
									} else {
										RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberTypes, value, calledMethod.Parameters[0]);
									}
								}
							} else {
								RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberTypes, value, calledMethod.Parameters[0]);
							}
						}
					}
					break;
				//
				// System.Linq.Expressions.Expression
				// 
				// static Call (Type, String, Type[], Expression[])
				//
				case IntrinsicId.Expression_Call: {
						reflectionContext.AnalyzingPattern ();
						BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

						foreach (var value in methodParams[0].UniqueValues ()) {
							if (value is SystemTypeValue systemTypeValue) {
								foreach (var stringParam in methodParams[1].UniqueValues ()) {
									// TODO: Change this as needed after deciding whether or not we are to keep
									// all methods on a type that was accessed via reflection.
									if (stringParam is KnownStringValue stringValue) {
										MarkMethodsOnTypeHierarchy (ref reflectionContext, systemTypeValue.TypeRepresented, m => m.Name == stringValue.Contents, bindingFlags);
										reflectionContext.RecordHandledPattern ();
									} else {
										RequireDynamicallyAccessedMembers (
											ref reflectionContext,
											GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (bindingFlags),
											value,
											calledMethod.Parameters[0]);
									}
								}
							} else {
								RequireDynamicallyAccessedMembers (
									ref reflectionContext,
									GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (bindingFlags),
									value,
									calledMethod.Parameters[0]);
							}
						}
					}
					break;

				//
				// System.Linq.Expressions.Expression
				// 
				// static Field (Expression, Type, String)
				// static Property (Expression, Type, String)
				//
				case var fieldOrPropertyInstrinsic when fieldOrPropertyInstrinsic == IntrinsicId.Expression_Field || fieldOrPropertyInstrinsic == IntrinsicId.Expression_Property: {
						reflectionContext.AnalyzingPattern ();
						DynamicallyAccessedMemberTypes memberTypes = fieldOrPropertyInstrinsic == IntrinsicId.Expression_Property
							? DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties
							: DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields;

						foreach (var value in methodParams[1].UniqueValues ()) {
							if (value is SystemTypeValue systemTypeValue) {
								foreach (var stringParam in methodParams[2].UniqueValues ()) {
									if (stringParam is KnownStringValue stringValue) {
										BindingFlags bindingFlags = methodParams[0].Kind == ValueNodeKind.Null ? BindingFlags.Static : BindingFlags.Default;
										// TODO: Change this as needed after deciding if we are to keep all fields/properties on a type
										// that is accessed via reflection. For now, let's only keep the field/property that is retrieved.
										if (fieldOrPropertyInstrinsic == IntrinsicId.Expression_Property) {
											MarkPropertiesOnTypeHierarchy (ref reflectionContext, systemTypeValue.TypeRepresented, filter: p => p.Name == stringValue.Contents, bindingFlags);
										} else {
											MarkFieldsOnTypeHierarchy (ref reflectionContext, systemTypeValue.TypeRepresented, filter: f => f.Name == stringValue.Contents, bindingFlags);
										}

										reflectionContext.RecordHandledPattern ();
									} else {
										RequireDynamicallyAccessedMembers (ref reflectionContext, memberTypes, value, calledMethod.Parameters[2]);
									}
								}
							} else {
								RequireDynamicallyAccessedMembers (ref reflectionContext, memberTypes, value, calledMethod.Parameters[1]);
							}
						}
					}
					break;

				//
				// System.Linq.Expressions.Expression
				// 
				// static New (Type)
				//
				case IntrinsicId.Expression_New: {
						reflectionContext.AnalyzingPattern ();

						foreach (var value in methodParams[0].UniqueValues ()) {
							if (value is SystemTypeValue systemTypeValue) {
								MarkConstructorsOnType (ref reflectionContext, systemTypeValue.TypeRepresented, null, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
								reflectionContext.RecordHandledPattern ();
							} else {
								RequireDynamicallyAccessedMembers (ref reflectionContext, DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, value, calledMethod.Parameters[0]);
							}
						}
					}
					break;

				//
				// System.Object
				// 
				// GetType()
				//
				case IntrinsicId.Object_GetType: {
						// We could do better here if we start tracking the static types of values within the method body.
						// Right now, this can only analyze a couple cases for which we have static information for.
						TypeDefinition staticType = null;
						if (methodParams[0] is MethodParameterValue methodParam) {
							if (callingMethodDefinition.HasThis) {
								if (methodParam.ParameterIndex == 0) {
									staticType = callingMethodDefinition.DeclaringType;
								} else {
									staticType = callingMethodDefinition.Parameters[methodParam.ParameterIndex - 1].ParameterType.ResolveToMainTypeDefinition ();
								}
							} else {
								staticType = callingMethodDefinition.Parameters[methodParam.ParameterIndex].ParameterType.ResolveToMainTypeDefinition ();
							}
						} else if (methodParams[0] is LoadFieldValue loadedField) {
							staticType = loadedField.Field.FieldType.ResolveToMainTypeDefinition ();
						}

						if (staticType != null) {
							// We can only analyze the Object.GetType call with the precise type if the type is sealed.
							// The type could be a descendant of the type in question, making us miss reflection.
							bool canUse = staticType.IsSealed;

							if (!canUse) {
								// We can allow Object.GetType to be modeled as System.Delegate because we keep all methods
								// on delegates anyway so reflection on something this approximation would miss is actually safe.
								canUse = staticType.IsTypeOf ("System", "Delegate");
							}

							if (canUse) {
								methodReturnValue = new SystemTypeValue (staticType);
							}
						}
					}
					break;

				//
				// System.Type
				//
				// GetType (string)
				// GetType (string, Boolean)
				// GetType (string, Boolean, Boolean)
				// GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>)
				// GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean)
				// GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean, Boolean)
				//
				case IntrinsicId.Type_GetType: {
						reflectionContext.AnalyzingPattern ();

						var parameters = calledMethod.Parameters;
						if ((parameters.Count == 3 && parameters[2].ParameterType.MetadataType == MetadataType.Boolean && methodParams[2].AsConstInt () != 0) ||
							(parameters.Count == 5 && methodParams[4].AsConstInt () != 0)) {
							reflectionContext.RecordUnrecognizedPattern (2096, $"Call to '{calledMethod.GetDisplayName ()}' can perform case insensitive lookup of the type, currently ILLink can not guarantee presence of all the matching types");
							break;
						}
						foreach (var typeNameValue in methodParams[0].UniqueValues ()) {
							if (typeNameValue is KnownStringValue knownStringValue) {
								TypeReference foundTypeRef = _context.TypeNameResolver.ResolveTypeName (knownStringValue.Contents);
								TypeDefinition foundType = foundTypeRef?.ResolveToMainTypeDefinition ();
								if (foundType == null) {
									// Intentionally ignore - it's not wrong for code to call Type.GetType on non-existing name, the code might expect null/exception back.
									reflectionContext.RecordHandledPattern ();
								} else {
									reflectionContext.RecordRecognizedPattern (foundType, () => _markStep.MarkTypeVisibleToReflection (foundTypeRef, new DependencyInfo (DependencyKind.AccessedViaReflection, callingMethodDefinition), callingMethodDefinition));
									methodReturnValue = MergePointValue.MergeValues (methodReturnValue, new SystemTypeValue (foundType));
								}
							} else if (typeNameValue == NullValue.Instance) {
								reflectionContext.RecordHandledPattern ();
							} else if (typeNameValue is LeafValueWithDynamicallyAccessedMemberNode valueWithDynamicallyAccessedMember && valueWithDynamicallyAccessedMember.DynamicallyAccessedMemberTypes != 0) {
								// Propagate the annotation from the type name to the return value. Annotation on a string value will be fullfilled whenever a value is assigned to the string with annotation.
								// So while we don't know which type it is, we can guarantee that it will fulfill the annotation.
								reflectionContext.RecordHandledPattern ();
								methodReturnValue = MergePointValue.MergeValues (methodReturnValue, new MethodReturnValue (calledMethodDefinition.MethodReturnType, valueWithDynamicallyAccessedMember.DynamicallyAccessedMemberTypes));
							} else {
								reflectionContext.RecordUnrecognizedPattern (2057, $"Unrecognized value passed to the parameter 'typeName' of method '{calledMethod.GetDisplayName ()}'. It's not possible to guarantee the availability of the target type.");
							}
						}

					}
					break;

				//
				// GetConstructor (Type[])
				// GetConstructor (BindingFlags, Binder, Type[], ParameterModifier [])
				// GetConstructor (BindingFlags, Binder, CallingConventions, Type[], ParameterModifier [])
				//
				case IntrinsicId.Type_GetConstructor: {
						reflectionContext.AnalyzingPattern ();

						var parameters = calledMethod.Parameters;
						BindingFlags? bindingFlags;
						if (parameters.Count > 1 && calledMethod.Parameters[0].ParameterType.Name == "BindingFlags")
							bindingFlags = GetBindingFlagsFromValue (methodParams[1]);
						else
							// Assume a default value for BindingFlags for methods that don't use BindingFlags as a parameter
							bindingFlags = BindingFlags.Public | BindingFlags.Instance;

						int? ctorParameterCount = parameters.Count switch {
							1 => (methodParams[1] as ArrayValue)?.Size.AsConstInt (),
							4 => (methodParams[3] as ArrayValue)?.Size.AsConstInt (),
							5 => (methodParams[4] as ArrayValue)?.Size.AsConstInt (),
							_ => null,
						};

						// Go over all types we've seen
						foreach (var value in methodParams[0].UniqueValues ()) {
							if (value is SystemTypeValue systemTypeValue) {
								if (BindingFlagsAreUnsupported (bindingFlags))
									RequireDynamicallyAccessedMembers (ref reflectionContext, DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors, value, calledMethodDefinition);
								else
									MarkConstructorsOnType (ref reflectionContext, systemTypeValue.TypeRepresented, null, bindingFlags);
								reflectionContext.RecordHandledPattern ();
							} else {
								// Otherwise fall back to the bitfield requirements
								var requiredMemberTypes = HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicConstructors : DynamicallyAccessedMemberTypes.None;
								requiredMemberTypes |= HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicConstructors : DynamicallyAccessedMemberTypes.None;
								// We can scope down the public constructors requirement if we know the number of parameters is 0
								if (requiredMemberTypes == DynamicallyAccessedMemberTypes.PublicConstructors && ctorParameterCount == 0)
									requiredMemberTypes = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;
								RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberTypes, value, calledMethodDefinition);
							}
						}
					}
					break;

				//
				// GetMethod (string)
				// GetMethod (string, BindingFlags)
				// GetMethod (string, Type[])
				// GetMethod (string, Type[], ParameterModifier[])
				// GetMethod (string, BindingFlags, Binder, Type[], ParameterModifier[])
				// GetMethod (string, BindingFlags, Binder, CallingConventions, Type[], ParameterModifier[])
				// GetMethod (string, int, Type[])
				// GetMethod (string, int, Type[], ParameterModifier[]?)
				// GetMethod (string, int, BindingFlags, Binder?, Type[], ParameterModifier[]?)
				// GetMethod (string, int, BindingFlags, Binder?, CallingConventions, Type[], ParameterModifier[]?)
				//
				case IntrinsicId.Type_GetMethod: {
						reflectionContext.AnalyzingPattern ();

						BindingFlags? bindingFlags;
						if (calledMethod.Parameters.Count > 1 && calledMethod.Parameters[1].ParameterType.Name == "BindingFlags")
							bindingFlags = GetBindingFlagsFromValue (methodParams[2]);
						else if (calledMethod.Parameters.Count > 2 && calledMethod.Parameters[2].ParameterType.Name == "BindingFlags")
							bindingFlags = GetBindingFlagsFromValue (methodParams[3]);
						else
							// Assume a default value for BindingFlags for methods that don't use BindingFlags as a parameter
							bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
						var requiredMemberTypes = GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (bindingFlags);
						foreach (var value in methodParams[0].UniqueValues ()) {
							if (value is SystemTypeValue systemTypeValue) {
								foreach (var stringParam in methodParams[1].UniqueValues ()) {
									if (stringParam is KnownStringValue stringValue) {
										if (BindingFlagsAreUnsupported (bindingFlags))
											RequireDynamicallyAccessedMembers (ref reflectionContext, DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods, value, calledMethodDefinition);
										else
											MarkMethodsOnTypeHierarchy (ref reflectionContext, systemTypeValue.TypeRepresented, m => m.Name == stringValue.Contents, bindingFlags);
										reflectionContext.RecordHandledPattern ();
									} else {
										// Otherwise fall back to the bitfield requirements
										RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberTypes, value, calledMethodDefinition);
									}
								}
							} else {
								// Otherwise fall back to the bitfield requirements
								RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberTypes, value, calledMethodDefinition);
							}
						}
					}
					break;

				//
				// GetNestedType (string)
				// GetNestedType (string, BindingFlags)
				//
				case IntrinsicId.Type_GetNestedType: {
						reflectionContext.AnalyzingPattern ();

						BindingFlags? bindingFlags;
						if (calledMethod.Parameters.Count > 1 && calledMethod.Parameters[1].ParameterType.Name == "BindingFlags")
							bindingFlags = GetBindingFlagsFromValue (methodParams[2]);
						else
							// Assume a default value for BindingFlags for methods that don't use BindingFlags as a parameter
							bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

						var requiredMemberTypes = GetDynamicallyAccessedMemberTypesFromBindingFlagsForNestedTypes (bindingFlags);
						bool everyParentTypeHasAll = true;
						foreach (var value in methodParams[0].UniqueValues ()) {
							if (value is SystemTypeValue systemTypeValue) {
								foreach (var stringParam in methodParams[1].UniqueValues ()) {
									if (stringParam is KnownStringValue stringValue) {
										if (BindingFlagsAreUnsupported (bindingFlags))
											// We have chosen not to populate the methodReturnValue for now
											RequireDynamicallyAccessedMembers (ref reflectionContext, DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes, value, calledMethodDefinition);
										else {
											TypeDefinition[] matchingNestedTypes = MarkNestedTypesOnType (ref reflectionContext, systemTypeValue.TypeRepresented, m => m.Name == stringValue.Contents, bindingFlags);

											if (matchingNestedTypes != null) {
												for (int i = 0; i < matchingNestedTypes.Length; i++)
													methodReturnValue = MergePointValue.MergeValues (methodReturnValue, new SystemTypeValue (matchingNestedTypes[i]));
											}
										}
										reflectionContext.RecordHandledPattern ();
									} else {
										// Otherwise fall back to the bitfield requirements
										RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberTypes, value, calledMethodDefinition);
									}
								}
							} else {
								// Otherwise fall back to the bitfield requirements
								RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberTypes, value, calledMethodDefinition);
							}

							if (value is LeafValueWithDynamicallyAccessedMemberNode leafValueWithDynamicallyAccessedMember) {
								if (leafValueWithDynamicallyAccessedMember.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.All)
									everyParentTypeHasAll = false;
							} else if (!(value is NullValue || value is SystemTypeValue)) {
								// Known Type values are always OK - either they're fully resolved above and thus the return value
								// is set to the known resolved type, or if they're not resolved, they won't exist at runtime
								// and will cause exceptions - and thus don't introduce new requirements on marking.
								// nulls are intentionally ignored as they will lead to exceptions at runtime
								// and thus don't introduce new requirements on marking.
								everyParentTypeHasAll = false;
							}
						}

						// If the parent type (all the possible values) has DynamicallyAccessedMemberTypes.All it means its nested types are also fully marked
						// (see MarkStep.MarkEntireType - it will recursively mark entire type on nested types). In that case we can annotate 
						// the returned type (the nested type) with DynamicallyAccessedMemberTypes.All as well.
						// Note it's OK to blindly overwrite any potential annotation on the return value from the method definition
						// since DynamicallyAccessedMemberTypes.All is a superset of any other annotation.
						if (everyParentTypeHasAll && methodReturnValue == null)
							methodReturnValue = new MethodReturnValue (calledMethodDefinition.MethodReturnType, DynamicallyAccessedMemberTypes.All);
					}
					break;

				//
				// AssemblyQualifiedName
				//
				case IntrinsicId.Type_get_AssemblyQualifiedName: {

						ValueNode transformedResult = null;
						foreach (var value in methodParams[0].UniqueValues ()) {
							if (value is LeafValueWithDynamicallyAccessedMemberNode dynamicallyAccessedThing) {
								var annotatedString = new AnnotatedStringValue (dynamicallyAccessedThing.SourceContext, dynamicallyAccessedThing.DynamicallyAccessedMemberTypes);
								transformedResult = MergePointValue.MergeValues (transformedResult, annotatedString);
							} else {
								transformedResult = null;
								break;
							}
						}

						if (transformedResult != null) {
							methodReturnValue = transformedResult;
						}
					}
					break;

				//
				// UnderlyingSystemType
				//
				case IntrinsicId.Type_get_UnderlyingSystemType: {
						// This is identity for the purposes of the analysis.
						methodReturnValue = methodParams[0];
					}
					break;

				//
				// GetField (string)
				// GetField (string, BindingFlags)
				// GetEvent (string)
				// GetEvent (string, BindingFlags)
				// GetProperty (string)
				// GetProperty (string, BindingFlags)
				// GetProperty (string, Type)
				// GetProperty (string, Type[])
				// GetProperty (string, Type, Type[])
				// GetProperty (string, Type, Type[], ParameterModifier[])
				// GetProperty (string, BindingFlags, Binder, Type, Type[], ParameterModifier[])
				//
				case var fieldPropertyOrEvent when (fieldPropertyOrEvent == IntrinsicId.Type_GetField || fieldPropertyOrEvent == IntrinsicId.Type_GetProperty || fieldPropertyOrEvent == IntrinsicId.Type_GetEvent)
					&& calledMethod.DeclaringType.Namespace == "System"
					&& calledMethod.DeclaringType.Name == "Type"
					&& calledMethod.Parameters[0].ParameterType.FullName == "System.String"
					&& calledMethod.HasThis: {

						reflectionContext.AnalyzingPattern ();
						BindingFlags? bindingFlags;
						if (calledMethod.Parameters.Count > 1 && calledMethod.Parameters[1].ParameterType.Name == "BindingFlags")
							bindingFlags = GetBindingFlagsFromValue (methodParams[2]);
						else
							// Assume a default value for BindingFlags for methods that don't use BindingFlags as a parameter
							bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

						DynamicallyAccessedMemberTypes memberTypes = fieldPropertyOrEvent switch {
							IntrinsicId.Type_GetEvent => GetDynamicallyAccessedMemberTypesFromBindingFlagsForEvents (bindingFlags),
							IntrinsicId.Type_GetField => GetDynamicallyAccessedMemberTypesFromBindingFlagsForFields (bindingFlags),
							IntrinsicId.Type_GetProperty => GetDynamicallyAccessedMemberTypesFromBindingFlagsForProperties (bindingFlags),
							_ => throw new ArgumentException ($"Reflection call '{calledMethod.GetDisplayName ()}' inside '{callingMethodDefinition.GetDisplayName ()}' is of unexpected member type."),
						};

						foreach (var value in methodParams[0].UniqueValues ()) {
							if (value is SystemTypeValue systemTypeValue) {
								foreach (var stringParam in methodParams[1].UniqueValues ()) {
									if (stringParam is KnownStringValue stringValue) {
										switch (fieldPropertyOrEvent) {
										case IntrinsicId.Type_GetEvent:
											if (BindingFlagsAreUnsupported (bindingFlags))
												RequireDynamicallyAccessedMembers (ref reflectionContext, DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents, value, calledMethodDefinition);
											else
												MarkEventsOnTypeHierarchy (ref reflectionContext, systemTypeValue.TypeRepresented, filter: e => e.Name == stringValue.Contents, bindingFlags);
											break;
										case IntrinsicId.Type_GetField:
											if (BindingFlagsAreUnsupported (bindingFlags))
												RequireDynamicallyAccessedMembers (ref reflectionContext, DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields, value, calledMethodDefinition);
											else
												MarkFieldsOnTypeHierarchy (ref reflectionContext, systemTypeValue.TypeRepresented, filter: f => f.Name == stringValue.Contents, bindingFlags);
											break;
										case IntrinsicId.Type_GetProperty:
											if (BindingFlagsAreUnsupported (bindingFlags))
												RequireDynamicallyAccessedMembers (ref reflectionContext, DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties, value, calledMethodDefinition);
											else
												MarkPropertiesOnTypeHierarchy (ref reflectionContext, systemTypeValue.TypeRepresented, filter: p => p.Name == stringValue.Contents, bindingFlags);
											break;
										default:
											Debug.Fail ("Unreachable.");
											break;
										}
										reflectionContext.RecordHandledPattern ();
									} else {
										RequireDynamicallyAccessedMembers (ref reflectionContext, memberTypes, value, calledMethodDefinition);
									}
								}
							} else {
								RequireDynamicallyAccessedMembers (ref reflectionContext, memberTypes, value, calledMethodDefinition);
							}
						}
					}
					break;

				//
				// GetConstructors (BindingFlags)
				// GetMethods (BindingFlags)
				// GetFields (BindingFlags)
				// GetEvents (BindingFlags)
				// GetProperties (BindingFlags)
				// GetNestedTypes (BindingFlags)
				// GetMembers (BindingFlags)
				//
				case var callType when (callType == IntrinsicId.Type_GetConstructors || callType == IntrinsicId.Type_GetMethods || callType == IntrinsicId.Type_GetFields ||
					callType == IntrinsicId.Type_GetProperties || callType == IntrinsicId.Type_GetEvents || callType == IntrinsicId.Type_GetNestedTypes || callType == IntrinsicId.Type_GetMembers)
					&& calledMethod.DeclaringType.Namespace == "System"
					&& calledMethod.DeclaringType.Name == "Type"
					&& calledMethod.Parameters[0].ParameterType.FullName == "System.Reflection.BindingFlags"
					&& calledMethod.HasThis: {

						reflectionContext.AnalyzingPattern ();
						BindingFlags? bindingFlags;
						bindingFlags = GetBindingFlagsFromValue (methodParams[1]);
						DynamicallyAccessedMemberTypes memberTypes = DynamicallyAccessedMemberTypes.None;
						if (BindingFlagsAreUnsupported (bindingFlags)) {
							memberTypes = callType switch {
								IntrinsicId.Type_GetConstructors => DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors,
								IntrinsicId.Type_GetMethods => DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods,
								IntrinsicId.Type_GetEvents => DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents,
								IntrinsicId.Type_GetFields => DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields,
								IntrinsicId.Type_GetProperties => DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties,
								IntrinsicId.Type_GetNestedTypes => DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes,
								IntrinsicId.Type_GetMembers => DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors |
									DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents |
									DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields |
									DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods |
									DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties |
									DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes,
								_ => throw new ArgumentException ($"Reflection call '{calledMethod.GetDisplayName ()}' inside '{callingMethodDefinition.GetDisplayName ()}' is of unexpected member type."),
							};
						} else {
							memberTypes = callType switch {
								IntrinsicId.Type_GetConstructors => GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors (bindingFlags),
								IntrinsicId.Type_GetMethods => GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (bindingFlags),
								IntrinsicId.Type_GetEvents => GetDynamicallyAccessedMemberTypesFromBindingFlagsForEvents (bindingFlags),
								IntrinsicId.Type_GetFields => GetDynamicallyAccessedMemberTypesFromBindingFlagsForFields (bindingFlags),
								IntrinsicId.Type_GetProperties => GetDynamicallyAccessedMemberTypesFromBindingFlagsForProperties (bindingFlags),
								IntrinsicId.Type_GetNestedTypes => GetDynamicallyAccessedMemberTypesFromBindingFlagsForNestedTypes (bindingFlags),
								IntrinsicId.Type_GetMembers => GetDynamicallyAccessedMemberTypesFromBindingFlagsForMembers (bindingFlags),
								_ => throw new ArgumentException ($"Reflection call '{calledMethod.GetDisplayName ()}' inside '{callingMethodDefinition.GetDisplayName ()}' is of unexpected member type."),
							};
						}

						foreach (var value in methodParams[0].UniqueValues ()) {
							RequireDynamicallyAccessedMembers (ref reflectionContext, memberTypes, value, calledMethodDefinition);
						}
					}
					break;


				//
				// GetMember (String)
				// GetMember (String, BindingFlags)
				// GetMember (String, MemberTypes, BindingFlags)
				//
				case IntrinsicId.Type_GetMember: {
						reflectionContext.AnalyzingPattern ();
						var parameters = calledMethod.Parameters;
						BindingFlags? bindingFlags;
						if (parameters.Count == 1) {
							// Assume a default value for BindingFlags for methods that don't use BindingFlags as a parameter
							bindingFlags = BindingFlags.Public | BindingFlags.Instance;
						} else if (parameters.Count == 2 && calledMethod.Parameters[1].ParameterType.Name == "BindingFlags")
							bindingFlags = GetBindingFlagsFromValue (methodParams[2]);
						else if (parameters.Count == 3 && calledMethod.Parameters[2].ParameterType.Name == "BindingFlags") {
							bindingFlags = GetBindingFlagsFromValue (methodParams[3]);
						} else // Non recognized intrinsic
							throw new ArgumentException ($"Reflection call '{calledMethod.GetDisplayName ()}' inside '{callingMethodDefinition.GetDisplayName ()}' is an unexpected intrinsic.");

						DynamicallyAccessedMemberTypes requiredMemberTypes = DynamicallyAccessedMemberTypes.None;
						if (BindingFlagsAreUnsupported (bindingFlags)) {
							requiredMemberTypes = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors |
								DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents |
								DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields |
								DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods |
								DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties |
								DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes;
						} else {
							requiredMemberTypes = GetDynamicallyAccessedMemberTypesFromBindingFlagsForMembers (bindingFlags);
						}
						// Go over all types we've seen
						foreach (var value in methodParams[0].UniqueValues ()) {
							// Mark based on bitfield requirements
							RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberTypes, value, calledMethodDefinition);
						}
					}
					break;

				//
				// System.Activator
				// 
				// static CreateInstance (System.Type type)
				// static CreateInstance (System.Type type, bool nonPublic)
				// static CreateInstance (System.Type type, params object?[]? args)
				// static CreateInstance (System.Type type, object?[]? args, object?[]? activationAttributes)
				// static CreateInstance (System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture)
				// static CreateInstance (System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture, object?[]? activationAttributes) { throw null; }
				//
				case IntrinsicId.Activator_CreateInstance_Type: {
						var parameters = calledMethod.Parameters;

						reflectionContext.AnalyzingPattern ();

						int? ctorParameterCount = null;
						BindingFlags bindingFlags = BindingFlags.Instance;
						if (parameters.Count > 1) {
							if (parameters[1].ParameterType.MetadataType == MetadataType.Boolean) {
								// The overload that takes a "nonPublic" bool
								bool nonPublic = true;
								if (methodParams[1] is ConstIntValue constInt) {
									nonPublic = constInt.Value != 0;
								}

								if (nonPublic)
									bindingFlags |= BindingFlags.NonPublic | BindingFlags.Public;
								else
									bindingFlags |= BindingFlags.Public;
								ctorParameterCount = 0;
							} else {
								// Overload that has the parameters as the second or fourth argument
								int argsParam = parameters.Count == 2 || parameters.Count == 3 ? 1 : 3;

								if (methodParams.Count > argsParam &&
									methodParams[argsParam] is ArrayValue arrayValue &&
									arrayValue.Size.AsConstInt () != null) {
									ctorParameterCount = arrayValue.Size.AsConstInt ();
								}

								if (parameters.Count > 3) {
									if (methodParams[1].AsConstInt () != null)
										bindingFlags |= (BindingFlags) methodParams[1].AsConstInt ();
									else
										bindingFlags |= BindingFlags.NonPublic | BindingFlags.Public;
								} else {
									bindingFlags |= BindingFlags.Public;
								}
							}
						} else {
							// The overload with a single System.Type argument
							ctorParameterCount = 0;
							bindingFlags |= BindingFlags.Public;
						}

						// Go over all types we've seen
						foreach (var value in methodParams[0].UniqueValues ()) {
							if (value is SystemTypeValue systemTypeValue) {
								// Special case known type values as we can do better by applying exact binding flags and parameter count.
								MarkConstructorsOnType (ref reflectionContext, systemTypeValue.TypeRepresented,
									ctorParameterCount == null ? null : m => m.Parameters.Count == ctorParameterCount, bindingFlags);
								reflectionContext.RecordHandledPattern ();
							} else {
								// Otherwise fall back to the bitfield requirements
								var requiredMemberTypes = ctorParameterCount == 0
									? DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
									: GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors (bindingFlags);
								RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberTypes, value, calledMethod.Parameters[0]);
							}
						}
					}
					break;

				//
				// System.Activator
				// 
				// static CreateInstance (string assemblyName, string typeName)
				// static CreateInstance (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture, object?[]? activationAttributes)
				// static CreateInstance (string assemblyName, string typeName, object?[]? activationAttributes)
				//
				case IntrinsicId.Activator_CreateInstance_AssemblyName_TypeName:
					ProcessCreateInstanceByName (ref reflectionContext, calledMethodDefinition, methodParams);
					break;

				//
				// System.Activator
				// 
				// static CreateInstanceFrom (string assemblyFile, string typeName)
				// static CreateInstanceFrom (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// static CreateInstanceFrom (string assemblyFile, string typeName, object? []? activationAttributes)
				//
				case IntrinsicId.Activator_CreateInstanceFrom:
					ProcessCreateInstanceByName (ref reflectionContext, calledMethodDefinition, methodParams);
					break;

				//
				// System.Activator
				// 
				// static T CreateInstance<T> ()
				//
				// Note: If the when condition returns false it would be an overload which we don't recognize, so just fall through to the default case
				case IntrinsicId.Activator_CreateInstanceOfT when
					calledMethod is GenericInstanceMethod genericCalledMethod && genericCalledMethod.GenericArguments.Count == 1: {
						reflectionContext.AnalyzingPattern ();

						if (genericCalledMethod.GenericArguments[0] is GenericParameter genericParameter &&
							genericParameter.HasDefaultConstructorConstraint) {
							// This is safe, the linker would have marked the default .ctor already
							reflectionContext.RecordHandledPattern ();
							break;
						}

						RequireDynamicallyAccessedMembers (
							ref reflectionContext,
							DynamicallyAccessedMemberTypes.PublicParameterlessConstructor,
							GetTypeValueNodeFromGenericArgument (genericCalledMethod.GenericArguments[0]),
							calledMethodDefinition.GenericParameters[0]);
					}
					break;

				//
				// System.AppDomain
				//
				// CreateInstance (string assemblyName, string typeName)
				// CreateInstance (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// CreateInstance (string assemblyName, string typeName, object? []? activationAttributes)
				//
				// CreateInstanceAndUnwrap (string assemblyName, string typeName)
				// CreateInstanceAndUnwrap (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// CreateInstanceAndUnwrap (string assemblyName, string typeName, object? []? activationAttributes)
				//
				// CreateInstanceFrom (string assemblyFile, string typeName)
				// CreateInstanceFrom (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// CreateInstanceFrom (string assemblyFile, string typeName, object? []? activationAttributes)
				//
				// CreateInstanceFromAndUnwrap (string assemblyFile, string typeName)
				// CreateInstanceFromAndUnwrap (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// CreateInstanceFromAndUnwrap (string assemblyFile, string typeName, object? []? activationAttributes)
				//
				case var appDomainCreateInstance when appDomainCreateInstance == IntrinsicId.AppDomain_CreateInstance
						|| appDomainCreateInstance == IntrinsicId.AppDomain_CreateInstanceAndUnwrap
						|| appDomainCreateInstance == IntrinsicId.AppDomain_CreateInstanceFrom
						|| appDomainCreateInstance == IntrinsicId.AppDomain_CreateInstanceFromAndUnwrap:
					ProcessCreateInstanceByName (ref reflectionContext, calledMethodDefinition, methodParams);
					break;

				//
				// System.Reflection.Assembly
				//
				// CreateInstance (string typeName)
				// CreateInstance (string typeName, bool ignoreCase)
				// CreateInstance (string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object []? args, CultureInfo? culture, object []? activationAttributes)
				//
				case IntrinsicId.Assembly_CreateInstance:
					//
					// TODO: This could be supported for "this" only calls
					//
					reflectionContext.AnalyzingPattern ();
					reflectionContext.RecordUnrecognizedPattern (2058, $"Parameters passed to method '{calledMethodDefinition.GetDisplayName ()}' cannot be analyzed. Consider using methods 'System.Type.GetType' and `System.Activator.CreateInstance` instead.");
					break;

				//
				// System.Runtime.CompilerServices.RuntimeHelpers
				//
				// RunClassConstructor (RuntimeTypeHandle type)
				//
				case IntrinsicId.RuntimeHelpers_RunClassConstructor: {
						reflectionContext.AnalyzingPattern ();
						foreach (var typeHandleValue in methodParams[0].UniqueValues ()) {
							if (typeHandleValue is RuntimeTypeHandleValue runtimeTypeHandleValue) {
								_markStep.MarkStaticConstructor (runtimeTypeHandleValue.TypeRepresented, new DependencyInfo (DependencyKind.AccessedViaReflection, reflectionContext.Source), reflectionContext.Source);
								reflectionContext.RecordHandledPattern ();
							} else if (typeHandleValue == NullValue.Instance)
								reflectionContext.RecordHandledPattern ();
							else {
								reflectionContext.RecordUnrecognizedPattern (2059, $"Unrecognized value passed to the parameter 'type' of method '{calledMethodDefinition.GetDisplayName ()}'. It's not possible to guarantee the availability of the target static constructor.");
							}
						}
					}
					break;

				//
				// System.Reflection.MethodInfo
				//
				// MakeGenericMethod (Type[] typeArguments)
				//
				case IntrinsicId.MethodInfo_MakeGenericMethod: {
						reflectionContext.AnalyzingPattern ();

						// We don't track MethodInfo values, so we can't determine if the MakeGenericMethod is problematic or not.
						// Since some of the generic parameters may have annotations, all calls are potentially dangerous.
						reflectionContext.RecordUnrecognizedPattern (2060, $"Call to `{calledMethod.GetDisplayName ()}` can not be statically analyzed. It's not possible to guarantee the availability of requirements of the generic method.");
					}
					break;

				default:
					if (requiresDataFlowAnalysis) {
						reflectionContext.AnalyzingPattern ();
						for (int parameterIndex = 0; parameterIndex < methodParams.Count; parameterIndex++) {
							var requiredMemberTypes = _context.Annotations.FlowAnnotations.GetParameterAnnotation (calledMethodDefinition, parameterIndex);
							if (requiredMemberTypes != 0) {
								IMetadataTokenProvider targetContext;
								if (calledMethodDefinition.HasImplicitThis ()) {
									if (parameterIndex == 0)
										targetContext = calledMethodDefinition;
									else
										targetContext = calledMethodDefinition.Parameters[parameterIndex - 1];
								} else {
									targetContext = calledMethodDefinition.Parameters[parameterIndex];
								}

								RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberTypes, methodParams[parameterIndex], targetContext);
							}
						}

						reflectionContext.RecordHandledPattern ();
					}

					_markStep.CheckAndReportRequiresUnreferencedCode (calledMethodDefinition, new MessageOrigin (callingMethodDefinition, operation.Offset));

					// To get good reporting of errors we need to track the origin of the value for all method calls
					// but except Newobj as those are special.
					if (calledMethodDefinition.ReturnType.MetadataType != MetadataType.Void) {
						methodReturnValue = new MethodReturnValue (calledMethodDefinition.MethodReturnType, returnValueDynamicallyAccessedMemberTypes);

						return true;
					}

					return false;
				}
			} finally {
				reflectionContext.Dispose ();
			}

			// If we get here, we handled this as an intrinsic.  As a convenience, if the code above
			// didn't set the return value (and the method has a return value), we will set it to be an
			// unknown value with the return type of the method.
			if (methodReturnValue == null) {
				if (calledMethod.ReturnType.MetadataType != MetadataType.Void) {
					methodReturnValue = new MethodReturnValue (calledMethodDefinition.MethodReturnType, returnValueDynamicallyAccessedMemberTypes);
				}
			}

			// Validate that the return value has the correct annotations as per the method return value annotations
			if (returnValueDynamicallyAccessedMemberTypes != 0 && methodReturnValue != null) {
				if (methodReturnValue is LeafValueWithDynamicallyAccessedMemberNode methodReturnValueWithMemberTypes) {
					if (!methodReturnValueWithMemberTypes.DynamicallyAccessedMemberTypes.HasFlag (returnValueDynamicallyAccessedMemberTypes))
						throw new InvalidOperationException ($"Internal linker error: processing of call from {callingMethodDefinition.GetDisplayName ()} to {calledMethod.GetDisplayName ()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
				} else if (methodReturnValue is SystemTypeValue) {
					// SystemTypeValue can fullfill any requirement, so it's always valid
					// The requirements will be applied at the point where it's consumed (passed as a method parameter, set as field value, returned from the method)
				} else {
					throw new InvalidOperationException ($"Internal linker error: processing of call from {callingMethodDefinition.GetDisplayName ()} to {calledMethod.GetDisplayName ()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
				}
			}

			return true;
		}

		void ProcessCreateInstanceByName (ref ReflectionPatternContext reflectionContext, MethodDefinition calledMethod, ValueNodeList methodParams)
		{
			reflectionContext.AnalyzingPattern ();

			BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
			bool parameterlessConstructor = true;
			if (calledMethod.Parameters.Count == 8 && calledMethod.Parameters[2].ParameterType.MetadataType == MetadataType.Boolean &&
				methodParams[3].AsConstInt () != null) {
				parameterlessConstructor = false;
				bindingFlags = BindingFlags.Instance | (BindingFlags) methodParams[3].AsConstInt ();
			} else if (calledMethod.Parameters.Count == 8 && calledMethod.Parameters[2].ParameterType.MetadataType == MetadataType.Boolean &&
				  methodParams[3].AsConstInt () == null) {
				parameterlessConstructor = false;
				bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			}

			int methodParamsOffset = calledMethod.HasImplicitThis () ? 1 : 0;

			foreach (var assemblyNameValue in methodParams[methodParamsOffset].UniqueValues ()) {
				if (assemblyNameValue is KnownStringValue assemblyNameStringValue) {
					foreach (var typeNameValue in methodParams[methodParamsOffset + 1].UniqueValues ()) {
						if (typeNameValue is KnownStringValue typeNameStringValue) {
							var resolvedAssembly = _context.TryResolve (assemblyNameStringValue.Contents);
							if (resolvedAssembly == null) {
								reflectionContext.RecordUnrecognizedPattern (2061, $"The assembly name '{assemblyNameStringValue.Contents}' passed to method '{calledMethod.GetDisplayName ()}' references assembly which is not available.");
								continue;
							}

							var typeRef = _context.TypeNameResolver.ResolveTypeName (resolvedAssembly, typeNameStringValue.Contents);
							var resolvedType = typeRef?.Resolve ();
							if (resolvedType == null || typeRef is ArrayType) {
								// It's not wrong to have a reference to non-existing type - the code may well expect to get an exception in this case
								// Note that we did find the assembly, so it's not a linker config problem, it's either intentional, or wrong versions of assemblies
								// but linker can't know that. In case a user tries to create an array using System.Activator we should simply ignore it, the user
								// might expect an exception to be thrown.
								reflectionContext.RecordHandledPattern ();
								continue;
							}

							MarkConstructorsOnType (ref reflectionContext, resolvedType, parameterlessConstructor ? m => m.Parameters.Count == 0 : null, bindingFlags);
						} else {
							reflectionContext.RecordUnrecognizedPattern (2032, $"Unrecognized value passed to the parameter '{calledMethod.Parameters[1].Name}' of method '{calledMethod.GetDisplayName ()}'. It's not possible to guarantee the availability of the target type.");
						}
					}
				} else {
					reflectionContext.RecordUnrecognizedPattern (2032, $"Unrecognized value passed to the parameter '{calledMethod.Parameters[0].Name}' of method '{calledMethod.GetDisplayName ()}'. It's not possible to guarantee the availability of the target type.");
				}
			}
		}

		void RequireDynamicallyAccessedMembers (ref ReflectionPatternContext reflectionContext, DynamicallyAccessedMemberTypes requiredMemberTypes, ValueNode value, IMetadataTokenProvider targetContext)
		{
			foreach (var uniqueValue in value.UniqueValues ()) {
				if (requiredMemberTypes == DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
					&& uniqueValue is SystemTypeForGenericParameterValue genericParam
					&& genericParam.GenericParameter.HasDefaultConstructorConstraint) {
					// We allow a new() constraint on a generic parameter to satisfy DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
					reflectionContext.RecordHandledPattern ();
				} else if (uniqueValue is LeafValueWithDynamicallyAccessedMemberNode valueWithDynamicallyAccessedMember) {
					if (!valueWithDynamicallyAccessedMember.DynamicallyAccessedMemberTypes.HasFlag (requiredMemberTypes)) {
						string missingMemberTypes = $"'{nameof (DynamicallyAccessedMemberTypes.All)}'";
						if (requiredMemberTypes != DynamicallyAccessedMemberTypes.All) {
							var missingMemberTypesList = Enum.GetValues (typeof (DynamicallyAccessedMemberTypes))
								.Cast<DynamicallyAccessedMemberTypes> ()
								.Where (damt => (requiredMemberTypes & ~valueWithDynamicallyAccessedMember.DynamicallyAccessedMemberTypes & damt) == damt && damt != DynamicallyAccessedMemberTypes.None)
								.Select (damt => damt.ToString ()).ToList ();

							if (missingMemberTypesList.Contains (nameof (DynamicallyAccessedMemberTypes.PublicConstructors)) &&
								missingMemberTypesList.SingleOrDefault (x => x == nameof (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)) is var ppc &&
								ppc != null)
								missingMemberTypesList.Remove (ppc);

							missingMemberTypes = string.Join (", ", missingMemberTypesList.Select (mmt => $"'DynamicallyAccessedMemberTypes.{mmt}'"));
						}

						switch ((valueWithDynamicallyAccessedMember.SourceContext, targetContext)) {
						case (ParameterDefinition sourceParameter, ParameterDefinition targetParameter):
							reflectionContext.RecordUnrecognizedPattern (2067, string.Format (Resources.Strings.IL2067,
								DiagnosticUtilities.GetParameterNameForErrorMessage (targetParameter),
								DiagnosticUtilities.GetMethodSignatureDisplayName (targetParameter.Method),
								DiagnosticUtilities.GetParameterNameForErrorMessage (sourceParameter),
								DiagnosticUtilities.GetMethodSignatureDisplayName (sourceParameter.Method),
								missingMemberTypes));
							break;
						case (ParameterDefinition sourceParameter, MethodReturnType targetMethodReturnType):
							reflectionContext.RecordUnrecognizedPattern (2068, string.Format (Resources.Strings.IL2068,
								DiagnosticUtilities.GetMethodSignatureDisplayName (targetMethodReturnType.Method),
								DiagnosticUtilities.GetParameterNameForErrorMessage (sourceParameter),
								DiagnosticUtilities.GetMethodSignatureDisplayName (sourceParameter.Method),
								missingMemberTypes));
							break;
						case (ParameterDefinition sourceParameter, FieldDefinition targetField):
							reflectionContext.RecordUnrecognizedPattern (2069, string.Format (Resources.Strings.IL2069,
								targetField.GetDisplayName (),
								DiagnosticUtilities.GetParameterNameForErrorMessage (sourceParameter),
								DiagnosticUtilities.GetMethodSignatureDisplayName (sourceParameter.Method),
								missingMemberTypes));
							break;
						case (ParameterDefinition sourceParameter, MethodDefinition targetMethod):
							reflectionContext.RecordUnrecognizedPattern (2070, string.Format (Resources.Strings.IL2070,
								targetMethod.GetDisplayName (),
								DiagnosticUtilities.GetParameterNameForErrorMessage (sourceParameter),
								DiagnosticUtilities.GetMethodSignatureDisplayName (sourceParameter.Method),
								missingMemberTypes));
							break;
						case (ParameterDefinition sourceParameter, GenericParameter targetGenericParameter):
							// Currently this is never generated, once ILLink supports full analysis of MakeGenericType/MakeGenericMethod this will be used
							reflectionContext.RecordUnrecognizedPattern (2071, string.Format (Resources.Strings.IL2071,
								targetGenericParameter.Name,
								DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (targetGenericParameter),
								DiagnosticUtilities.GetParameterNameForErrorMessage (sourceParameter),
								DiagnosticUtilities.GetMethodSignatureDisplayName (sourceParameter.Method),
								missingMemberTypes));
							break;

						case (MethodReturnType sourceMethodReturnType, ParameterDefinition targetParameter):
							reflectionContext.RecordUnrecognizedPattern (2072, string.Format (Resources.Strings.IL2072,
								DiagnosticUtilities.GetParameterNameForErrorMessage (targetParameter),
								DiagnosticUtilities.GetMethodSignatureDisplayName (targetParameter.Method),
								DiagnosticUtilities.GetMethodSignatureDisplayName (sourceMethodReturnType.Method),
								missingMemberTypes));
							break;
						case (MethodReturnType sourceMethodReturnType, MethodReturnType targetMethodReturnType):
							reflectionContext.RecordUnrecognizedPattern (2073, string.Format (Resources.Strings.IL2073,
								DiagnosticUtilities.GetMethodSignatureDisplayName (targetMethodReturnType.Method),
								DiagnosticUtilities.GetMethodSignatureDisplayName (sourceMethodReturnType.Method),
								missingMemberTypes));
							break;
						case (MethodReturnType sourceMethodReturnType, FieldDefinition targetField):
							reflectionContext.RecordUnrecognizedPattern (2074, string.Format (Resources.Strings.IL2074,
								targetField.GetDisplayName (),
								DiagnosticUtilities.GetMethodSignatureDisplayName (sourceMethodReturnType.Method),
								missingMemberTypes));
							break;
						case (MethodReturnType sourceMethodReturnType, MethodDefinition targetMethod):
							reflectionContext.RecordUnrecognizedPattern (2075, string.Format (Resources.Strings.IL2075,
								targetMethod.GetDisplayName (),
								DiagnosticUtilities.GetMethodSignatureDisplayName (sourceMethodReturnType.Method),
								missingMemberTypes));
							break;
						case (MethodReturnType sourceMethodReturnType, GenericParameter targetGenericParameter):
							// Currently this is never generated, once ILLink supports full analysis of MakeGenericType/MakeGenericMethod this will be used
							reflectionContext.RecordUnrecognizedPattern (2076, string.Format (Resources.Strings.IL2076,
								targetGenericParameter.Name,
								DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (targetGenericParameter),
								DiagnosticUtilities.GetMethodSignatureDisplayName (sourceMethodReturnType.Method),
								missingMemberTypes));
							break;

						case (FieldDefinition sourceField, ParameterDefinition targetParameter):
							reflectionContext.RecordUnrecognizedPattern (2077, string.Format (Resources.Strings.IL2077,
								DiagnosticUtilities.GetParameterNameForErrorMessage (targetParameter),
								DiagnosticUtilities.GetMethodSignatureDisplayName (targetParameter.Method),
								sourceField.GetDisplayName (),
								missingMemberTypes));
							break;
						case (FieldDefinition sourceField, MethodReturnType targetMethodReturnType):
							reflectionContext.RecordUnrecognizedPattern (2078, string.Format (Resources.Strings.IL2078,
								DiagnosticUtilities.GetMethodSignatureDisplayName (targetMethodReturnType.Method),
								sourceField.GetDisplayName (),
								missingMemberTypes));
							break;
						case (FieldDefinition sourceField, FieldDefinition targetField):
							reflectionContext.RecordUnrecognizedPattern (2079, string.Format (Resources.Strings.IL2079,
								targetField.GetDisplayName (),
								sourceField.GetDisplayName (),
								missingMemberTypes));
							break;
						case (FieldDefinition sourceField, MethodDefinition targetMethod):
							reflectionContext.RecordUnrecognizedPattern (2080, string.Format (Resources.Strings.IL2080,
								targetMethod.GetDisplayName (),
								sourceField.GetDisplayName (),
								missingMemberTypes));
							break;
						case (FieldDefinition sourceField, GenericParameter targetGenericParameter):
							// Currently this is never generated, once ILLink supports full analysis of MakeGenericType/MakeGenericMethod this will be used
							reflectionContext.RecordUnrecognizedPattern (2081, string.Format (Resources.Strings.IL2081,
								targetGenericParameter.Name,
								DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (targetGenericParameter),
								sourceField.GetDisplayName (),
								missingMemberTypes));
							break;

						case (MethodDefinition sourceMethod, ParameterDefinition targetParameter):
							reflectionContext.RecordUnrecognizedPattern (2082, string.Format (Resources.Strings.IL2082,
								DiagnosticUtilities.GetParameterNameForErrorMessage (targetParameter),
								DiagnosticUtilities.GetMethodSignatureDisplayName (targetParameter.Method),
								sourceMethod.GetDisplayName (),
								missingMemberTypes));
							break;
						case (MethodDefinition sourceMethod, MethodReturnType targetMethodReturnType):
							reflectionContext.RecordUnrecognizedPattern (2083, string.Format (Resources.Strings.IL2083,
								DiagnosticUtilities.GetMethodSignatureDisplayName (targetMethodReturnType.Method),
								sourceMethod.GetDisplayName (),
								missingMemberTypes));
							break;
						case (MethodDefinition sourceMethod, FieldDefinition targetField):
							reflectionContext.RecordUnrecognizedPattern (2084, string.Format (Resources.Strings.IL2084,
								targetField.GetDisplayName (),
								sourceMethod.GetDisplayName (),
								missingMemberTypes));
							break;
						case (MethodDefinition sourceMethod, MethodDefinition targetMethod):
							reflectionContext.RecordUnrecognizedPattern (2085, string.Format (Resources.Strings.IL2085,
								targetMethod.GetDisplayName (),
								sourceMethod.GetDisplayName (),
								missingMemberTypes));
							break;
						case (MethodDefinition sourceMethod, GenericParameter targetGenericParameter):
							// Currently this is never generated, once ILLink supports full analysis of MakeGenericType/MakeGenericMethod this will be used
							reflectionContext.RecordUnrecognizedPattern (2086, string.Format (Resources.Strings.IL2086,
								targetGenericParameter.Name,
								DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (targetGenericParameter),
								sourceMethod.GetDisplayName (),
								missingMemberTypes));
							break;

						case (GenericParameter sourceGenericParameter, ParameterDefinition targetParameter):
							reflectionContext.RecordUnrecognizedPattern (2087, string.Format (Resources.Strings.IL2087,
								DiagnosticUtilities.GetParameterNameForErrorMessage (targetParameter),
								DiagnosticUtilities.GetMethodSignatureDisplayName (targetParameter.Method),
								sourceGenericParameter.Name,
								DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (sourceGenericParameter),
								missingMemberTypes));
							break;
						case (GenericParameter sourceGenericParameter, MethodReturnType targetMethodReturnType):
							reflectionContext.RecordUnrecognizedPattern (2088, string.Format (Resources.Strings.IL2088,
								DiagnosticUtilities.GetMethodSignatureDisplayName (targetMethodReturnType.Method),
								sourceGenericParameter.Name,
								DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (sourceGenericParameter),
								missingMemberTypes));
							break;
						case (GenericParameter sourceGenericParameter, FieldDefinition targetField):
							reflectionContext.RecordUnrecognizedPattern (2089, string.Format (Resources.Strings.IL2089,
								targetField.GetDisplayName (),
								sourceGenericParameter.Name,
								DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (sourceGenericParameter),
								missingMemberTypes));
							break;
						case (GenericParameter sourceGenericParameter, MethodDefinition targetMethod):
							// Currently this is never generated, it might be possible one day if we try to validate annotations on results of reflection
							// For example code like this should ideally one day generate the warning
							// void TestMethod<T>()
							// {
							//    // This passes the T as the "this" parameter to Type.GetMethods()
							//    typeof(Type).GetMethod("GetMethods").Invoke(typeof(T), new object[] {});
							// }
							reflectionContext.RecordUnrecognizedPattern (2090, string.Format (Resources.Strings.IL2090,
								targetMethod.GetDisplayName (),
								sourceGenericParameter.Name,
								DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (sourceGenericParameter),
								missingMemberTypes));
							break;
						case (GenericParameter sourceGenericParameter, GenericParameter targetGenericParameter):
							reflectionContext.RecordUnrecognizedPattern (2091, string.Format (Resources.Strings.IL2091,
								targetGenericParameter.Name,
								DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (targetGenericParameter),
								sourceGenericParameter.Name,
								DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (sourceGenericParameter),
								missingMemberTypes));
							break;

						default:
							throw new NotImplementedException ($"unsupported source context {valueWithDynamicallyAccessedMember.SourceContext} or target context {targetContext}");
						};
					} else {
						reflectionContext.RecordHandledPattern ();
					}
				} else if (uniqueValue is SystemTypeValue systemTypeValue) {
					MarkTypeForDynamicallyAccessedMembers (ref reflectionContext, systemTypeValue.TypeRepresented, requiredMemberTypes);
				} else if (uniqueValue is KnownStringValue knownStringValue) {
					TypeReference typeRef = _context.TypeNameResolver.ResolveTypeName (knownStringValue.Contents);
					TypeDefinition foundType = typeRef?.ResolveToMainTypeDefinition ();
					if (foundType == null) {
						// Intentionally ignore - it's not wrong for code to call Type.GetType on non-existing name, the code might expect null/exception back.
						reflectionContext.RecordHandledPattern ();
					} else {
						MarkType (ref reflectionContext, typeRef);
						MarkTypeForDynamicallyAccessedMembers (ref reflectionContext, foundType, requiredMemberTypes);
					}
				} else if (uniqueValue == NullValue.Instance) {
					// Ignore - probably unreachable path as it would fail at runtime anyway.
				} else {
					switch (targetContext) {
					case ParameterDefinition parameterDefinition:
						reflectionContext.RecordUnrecognizedPattern (
							2062,
							$"Value passed to parameter '{DiagnosticUtilities.GetParameterNameForErrorMessage (parameterDefinition)}' of method '{DiagnosticUtilities.GetMethodSignatureDisplayName (parameterDefinition.Method)}' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.");
						break;
					case MethodReturnType methodReturnType:
						reflectionContext.RecordUnrecognizedPattern (
							2063,
							$"Value returned from method '{DiagnosticUtilities.GetMethodSignatureDisplayName (methodReturnType.Method)}' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.");
						break;
					case FieldDefinition fieldDefinition:
						reflectionContext.RecordUnrecognizedPattern (
							2064,
							$"Value assigned to {fieldDefinition.GetDisplayName ()} can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.");
						break;
					case MethodDefinition methodDefinition:
						reflectionContext.RecordUnrecognizedPattern (
							2065,
							$"Value passed to implicit 'this' parameter of method '{methodDefinition.GetDisplayName ()}' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.");
						break;
					case GenericParameter genericParameter:
						// Unknown value to generic parameter - this is possible if the generic argumnet fails to resolve
						reflectionContext.RecordUnrecognizedPattern (
							2066,
							$"Type passed to generic parameter '{genericParameter.Name}' of '{DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (genericParameter)}' can not be statically determined and may not meet 'DynamicallyAccessedMembersAttribute' requirements.");
						break;
					default: throw new NotImplementedException ($"unsupported target context {targetContext.GetType ()}");
					};
				}
			}

			reflectionContext.RecordHandledPattern ();
		}

		static BindingFlags? GetBindingFlagsFromValue (ValueNode parameter) => (BindingFlags?) parameter.AsConstInt ();

		static bool BindingFlagsAreUnsupported (BindingFlags? bindingFlags) => bindingFlags == null || (bindingFlags & BindingFlags.IgnoreCase) == BindingFlags.IgnoreCase || (int) bindingFlags > 255;

		static bool HasBindingFlag (BindingFlags? bindingFlags, BindingFlags? search) => bindingFlags != null && (bindingFlags & search) == search;

		void MarkTypeForDynamicallyAccessedMembers (ref ReflectionPatternContext reflectionContext, TypeDefinition typeDefinition, DynamicallyAccessedMemberTypes requiredMemberTypes)
		{
			foreach (var member in typeDefinition.GetDynamicallyAccessedMembers (requiredMemberTypes)) {
				switch (member) {
				case MethodDefinition method:
					MarkMethod (ref reflectionContext, method, DependencyKind.DynamicallyAccessedMember);
					break;
				case FieldDefinition field:
					MarkField (ref reflectionContext, field, DependencyKind.DynamicallyAccessedMember);
					break;
				case TypeDefinition nestedType:
					MarkNestedType (ref reflectionContext, nestedType, DependencyKind.DynamicallyAccessedMember);
					break;
				case PropertyDefinition property:
					MarkProperty (ref reflectionContext, property, DependencyKind.DynamicallyAccessedMember);
					break;
				case EventDefinition @event:
					MarkEvent (ref reflectionContext, @event, DependencyKind.DynamicallyAccessedMember);
					break;
				case null:
					var source = reflectionContext.Source;
					reflectionContext.RecordRecognizedPattern (typeDefinition, () => _markStep.MarkEntireType (typeDefinition, includeBaseTypes: true, includeInterfaceTypes: true, new DependencyInfo (DependencyKind.DynamicallyAccessedMember, source), source));
					break;
				}
			}
		}

		void MarkType (ref ReflectionPatternContext reflectionContext, TypeReference typeReference)
		{
			var source = reflectionContext.Source;
			reflectionContext.RecordRecognizedPattern (typeReference?.Resolve (), () => _markStep.MarkTypeVisibleToReflection (typeReference, new DependencyInfo (DependencyKind.AccessedViaReflection, source), source));
		}

		void MarkMethod (ref ReflectionPatternContext reflectionContext, MethodDefinition method, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			var source = reflectionContext.Source;
			var offset = reflectionContext.Instruction?.Offset;
			reflectionContext.RecordRecognizedPattern (method, () => _markStep.MarkIndirectlyCalledMethod (method, new DependencyInfo (dependencyKind, source), new MessageOrigin (source, offset)));
		}

		void MarkNestedType (ref ReflectionPatternContext reflectionContext, TypeDefinition nestedType, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			var source = reflectionContext.Source;
			reflectionContext.RecordRecognizedPattern (nestedType, () => _markStep.MarkTypeVisibleToReflection (nestedType, new DependencyInfo (dependencyKind, source), source));
		}

		void MarkField (ref ReflectionPatternContext reflectionContext, FieldDefinition field, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			var source = reflectionContext.Source;
			reflectionContext.RecordRecognizedPattern (field, () => _markStep.MarkField (field, new DependencyInfo (dependencyKind, source)));
		}

		void MarkProperty (ref ReflectionPatternContext reflectionContext, PropertyDefinition property, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			var source = reflectionContext.Source;
			var dependencyInfo = new DependencyInfo (dependencyKind, source);
			reflectionContext.RecordRecognizedPattern (property, () => {
				// Marking the property itself actually doesn't keep it (it only marks its attributes and records the dependency), we have to mark the methods on it
				_markStep.MarkProperty (property, dependencyInfo);
				// TODO - this is sort of questionable - when somebody asks for a property they probably want to call either get or set
				// but linker tracks those separately, and so accessing the getter/setter will raise a warning as it's potentially trimmed.
				// So including them here doesn't actually remove the warning even if the code is written correctly.
				_markStep.MarkMethodIfNotNull (property.GetMethod, dependencyInfo, source);
				_markStep.MarkMethodIfNotNull (property.SetMethod, dependencyInfo, source);
				_markStep.MarkMethodsIf (property.OtherMethods, m => true, dependencyInfo, source);
			});
		}

		void MarkEvent (ref ReflectionPatternContext reflectionContext, EventDefinition @event, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			var source = reflectionContext.Source;
			var dependencyInfo = new DependencyInfo (dependencyKind, reflectionContext.Source);
			reflectionContext.RecordRecognizedPattern (@event, () => {
				// MarkEvent actually marks the add/remove/invoke methods as well, so no need to mark those explicitly
				_markStep.MarkEvent (@event, dependencyInfo);
				_markStep.MarkMethodsIf (@event.OtherMethods, m => true, dependencyInfo, source);
			});
		}

		void MarkConstructorsOnType (ref ReflectionPatternContext reflectionContext, TypeDefinition type, Func<MethodDefinition, bool> filter, BindingFlags? bindingFlags = null)
		{
			foreach (var ctor in type.GetConstructorsOnType (filter, bindingFlags))
				MarkMethod (ref reflectionContext, ctor);
		}

		void MarkMethodsOnTypeHierarchy (ref ReflectionPatternContext reflectionContext, TypeDefinition type, Func<MethodDefinition, bool> filter, BindingFlags? bindingFlags = null)
		{
			foreach (var method in type.GetMethodsOnTypeHierarchy (filter, bindingFlags))
				MarkMethod (ref reflectionContext, method);
		}

		void MarkFieldsOnTypeHierarchy (ref ReflectionPatternContext reflectionContext, TypeDefinition type, Func<FieldDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			foreach (var field in type.GetFieldsOnTypeHierarchy (filter, bindingFlags))
				MarkField (ref reflectionContext, field);
		}

		TypeDefinition[] MarkNestedTypesOnType (ref ReflectionPatternContext reflectionContext, TypeDefinition type, Func<TypeDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			var result = new ArrayBuilder<TypeDefinition> ();

			foreach (var nestedType in type.GetNestedTypesOnType (filter, bindingFlags)) {
				result.Add (nestedType);
				MarkNestedType (ref reflectionContext, nestedType);
			}

			return result.ToArray ();
		}

		void MarkPropertiesOnTypeHierarchy (ref ReflectionPatternContext reflectionContext, TypeDefinition type, Func<PropertyDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			foreach (var property in type.GetPropertiesOnTypeHierarchy (filter, bindingFlags))
				MarkProperty (ref reflectionContext, property);
		}

		void MarkEventsOnTypeHierarchy (ref ReflectionPatternContext reflectionContext, TypeDefinition type, Func<EventDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			foreach (var @event in type.GetEventsOnTypeHierarchy (filter, bindingFlags))
				MarkEvent (ref reflectionContext, @event);
		}

		static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForNestedTypes (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicNestedTypes : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicNestedTypes : DynamicallyAccessedMemberTypes.None);

		static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicConstructors : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicConstructors : DynamicallyAccessedMemberTypes.None);

		static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicMethods : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicMethods : DynamicallyAccessedMemberTypes.None);

		static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForFields (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicFields : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicFields : DynamicallyAccessedMemberTypes.None);

		static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForProperties (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicProperties : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicProperties : DynamicallyAccessedMemberTypes.None);

		static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForEvents (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicEvents : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicEvents : DynamicallyAccessedMemberTypes.None);
		static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForMembers (BindingFlags? bindingFlags) =>
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors (bindingFlags) |
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForEvents (bindingFlags) |
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForFields (bindingFlags) |
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (bindingFlags) |
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForProperties (bindingFlags) |
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForNestedTypes (bindingFlags);
	}
}
