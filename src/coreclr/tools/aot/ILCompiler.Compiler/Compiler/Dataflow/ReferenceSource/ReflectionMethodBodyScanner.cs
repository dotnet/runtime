// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ILLink.Shared;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Steps;

using BindingFlags = System.Reflection.BindingFlags;

namespace Mono.Linker.Dataflow
{
	class ReflectionMethodBodyScanner : MethodBodyScanner
	{
		readonly MarkStep _markStep;
		readonly MarkScopeStack _scopeStack;
		static readonly DynamicallyAccessedMemberTypes[] AllDynamicallyAccessedMemberTypes = GetAllDynamicallyAccessedMemberTypes ();

		static DynamicallyAccessedMemberTypes[] GetAllDynamicallyAccessedMemberTypes ()
		{
			HashSet<DynamicallyAccessedMemberTypes> values = Enum.GetValues (typeof (DynamicallyAccessedMemberTypes))
								.Cast<DynamicallyAccessedMemberTypes> ()
								.ToHashSet ();
			if (!values.Contains (DynamicallyAccessedMemberTypesOverlay.Interfaces))
				values.Add (DynamicallyAccessedMemberTypesOverlay.Interfaces);
			return values.ToArray ();
		}

		public static bool RequiresReflectionMethodBodyScannerForCallSite (LinkContext context, MethodReference calledMethod)
		{
			MethodDefinition? methodDefinition = context.TryResolve (calledMethod);
			if (methodDefinition == null)
				return false;

			return GetIntrinsicIdForMethod (methodDefinition) > IntrinsicId.RequiresReflectionBodyScanner_Sentinel ||
				context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (methodDefinition) ||
				context.Annotations.DoesMethodRequireUnreferencedCode (methodDefinition, out _) ||
				methodDefinition.IsPInvokeImpl;
		}

		public static bool RequiresReflectionMethodBodyScannerForMethodBody (FlowAnnotations flowAnnotations, MethodDefinition methodDefinition)
		{
			return
				GetIntrinsicIdForMethod (methodDefinition) > IntrinsicId.RequiresReflectionBodyScanner_Sentinel ||
				flowAnnotations.RequiresDataFlowAnalysis (methodDefinition);
		}

		public static bool RequiresReflectionMethodBodyScannerForAccess (LinkContext context, FieldReference field)
		{
			FieldDefinition? fieldDefinition = context.TryResolve (field);
			if (fieldDefinition == null)
				return false;

			return context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (fieldDefinition);
		}

		bool ShouldEnableReflectionPatternReporting ()
		{
			if (_markStep.ShouldSuppressAnalysisWarningsForRequiresUnreferencedCode ())
				return false;

			return true;
		}

		public ReflectionMethodBodyScanner (LinkContext context, MarkStep parent, MarkScopeStack scopeStack)
			: base (context)
		{
			_markStep = parent;
			_scopeStack = scopeStack;
		}

		public void ScanAndProcessReturnValue (MethodBody methodBody)
		{
			Scan (methodBody);

			if (GetReturnTypeWithoutModifiers (methodBody.Method.ReturnType).MetadataType != MetadataType.Void) {
				var method = methodBody.Method;
				var requiredMemberTypes = _context.Annotations.FlowAnnotations.GetReturnParameterAnnotation (method);
				if (requiredMemberTypes != 0) {
					var reflectionContext = new ReflectionPatternContext (_context, ShouldEnableReflectionPatternReporting (), _scopeStack.CurrentScope.Origin, method.MethodReturnType);
					reflectionContext.AnalyzingPattern ();
					RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberTypes, MethodReturnValue, method.MethodReturnType);
					reflectionContext.Dispose ();
				}
			}
		}

		public void ProcessAttributeDataflow (MethodDefinition method, IList<CustomAttributeArgument> arguments)
		{
			int paramOffset = method.HasImplicitThis () ? 1 : 0;

			for (int i = 0; i < method.Parameters.Count; i++) {
				var annotation = _context.Annotations.FlowAnnotations.GetParameterAnnotation (method, i + paramOffset);
				if (annotation != DynamicallyAccessedMemberTypes.None) {
					ValueNode valueNode = GetValueNodeForCustomAttributeArgument (arguments[i]);
					var methodParameter = method.Parameters[i];
					var reflectionContext = new ReflectionPatternContext (_context, true, _scopeStack.CurrentScope.Origin, methodParameter);
					reflectionContext.AnalyzingPattern ();
					RequireDynamicallyAccessedMembers (ref reflectionContext, annotation, valueNode, methodParameter);
					reflectionContext.Dispose ();
				}
			}
		}

		public void ProcessAttributeDataflow (FieldDefinition field, CustomAttributeArgument value)
		{
			var annotation = _context.Annotations.FlowAnnotations.GetFieldAnnotation (field);
			Debug.Assert (annotation != DynamicallyAccessedMemberTypes.None);

			ValueNode valueNode = GetValueNodeForCustomAttributeArgument (value);
			var reflectionContext = new ReflectionPatternContext (_context, true, _scopeStack.CurrentScope.Origin, field);
			reflectionContext.AnalyzingPattern ();
			RequireDynamicallyAccessedMembers (ref reflectionContext, annotation, valueNode, field);
			reflectionContext.Dispose ();
		}

		ValueNode GetValueNodeForCustomAttributeArgument (CustomAttributeArgument argument)
		{
			ValueNode valueNode;
			if (argument.Type.Name == "Type") {
				TypeDefinition? referencedType = ResolveToTypeDefinition ((TypeReference) argument.Value);
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

		public void ProcessGenericArgumentDataFlow (GenericParameter genericParameter, TypeReference genericArgument)
		{
			var annotation = _context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (genericParameter);
			Debug.Assert (annotation != DynamicallyAccessedMemberTypes.None);

			ValueNode valueNode = GetTypeValueNodeFromGenericArgument (genericArgument);
			var currentScopeOrigin = _scopeStack.CurrentScope.Origin;

			var reflectionContext = new ReflectionPatternContext (_context, ShouldEnableReflectionPatternReporting (), currentScopeOrigin, genericParameter);
			reflectionContext.AnalyzingPattern ();
			RequireDynamicallyAccessedMembers (ref reflectionContext, annotation, valueNode, genericParameter);
			reflectionContext.Dispose ();
		}

		ValueNode GetTypeValueNodeFromGenericArgument (TypeReference genericArgument)
		{
			if (genericArgument is GenericParameter inputGenericParameter) {
				// Technically this should be a new value node type as it's not a System.Type instance representation, but just the generic parameter
				// That said we only use it to perform the dynamically accessed members checks and for that purpose treating it as System.Type is perfectly valid.
				return new SystemTypeForGenericParameterValue (inputGenericParameter, _context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (inputGenericParameter));
			} else {
				TypeDefinition? genericArgumentTypeDef = ResolveToTypeDefinition (genericArgument);
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

		MethodReturnValue CreateMethodReturnValue (MethodReference method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes = DynamicallyAccessedMemberTypes.None)
		{
			return new MethodReturnValue (ResolveToTypeDefinition (method.ReturnType), dynamicallyAccessedMemberTypes, method.MethodReturnType);
		}

		protected override ValueNode GetMethodParameterValue (MethodDefinition method, int parameterIndex)
		{
			DynamicallyAccessedMemberTypes memberTypes = _context.Annotations.FlowAnnotations.GetParameterAnnotation (method, parameterIndex);

			var staticType =
				!method.HasImplicitThis () ? ResolveToTypeDefinition (method.Parameters[parameterIndex].ParameterType) :
				parameterIndex == 0 ? method.DeclaringType :
				ResolveToTypeDefinition (method.Parameters[parameterIndex - 1].ParameterType);

			return new MethodParameterValue (staticType, parameterIndex, memberTypes, DiagnosticUtilities.GetMethodParameterFromIndex (method, parameterIndex));
		}

		protected override ValueNode GetFieldValue (MethodDefinition method, FieldDefinition field)
		{
			switch (field.Name) {
			case "EmptyTypes" when field.DeclaringType.IsTypeOf ("System", "Type"): {
					return new ArrayValue (new ConstIntValue (0), field.DeclaringType);
				}
			case "Empty" when field.DeclaringType.IsTypeOf ("System", "String"): {
					return new KnownStringValue (string.Empty);
				}

			default: {
					DynamicallyAccessedMemberTypes memberTypes = _context.Annotations.FlowAnnotations.GetFieldAnnotation (field);
					return new LoadFieldValue (ResolveToTypeDefinition (field.FieldType), field, memberTypes);
				}
			}
		}

		protected override void HandleStoreField (MethodDefinition method, FieldDefinition field, Instruction operation, ValueNode? valueToStore)
		{
			var requiredMemberTypes = _context.Annotations.FlowAnnotations.GetFieldAnnotation (field);
			if (requiredMemberTypes != 0) {
				_scopeStack.UpdateCurrentScopeInstructionOffset (operation.Offset);
				var reflectionContext = new ReflectionPatternContext (_context, ShouldEnableReflectionPatternReporting (), _scopeStack.CurrentScope.Origin, field, operation);
				reflectionContext.AnalyzingPattern ();
				RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberTypes, valueToStore, field);
				reflectionContext.Dispose ();
			}
		}

		protected override void HandleStoreParameter (MethodDefinition method, int index, Instruction operation, ValueNode? valueToStore)
		{
			var requiredMemberTypes = _context.Annotations.FlowAnnotations.GetParameterAnnotation (method, index);
			if (requiredMemberTypes != 0) {
				ParameterDefinition parameter = method.Parameters[index - (method.HasImplicitThis () ? 1 : 0)];
				_scopeStack.UpdateCurrentScopeInstructionOffset (operation.Offset);
				var reflectionContext = new ReflectionPatternContext (_context, ShouldEnableReflectionPatternReporting (), _scopeStack.CurrentScope.Origin, parameter, operation);
				reflectionContext.AnalyzingPattern ();
				RequireDynamicallyAccessedMembers (ref reflectionContext, requiredMemberTypes, valueToStore, parameter);
				reflectionContext.Dispose ();
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
			MethodBase_GetMethodFromHandle,

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

				// System.Reflection.MethodBase.GetMethodFromHandle (RuntimeMethodHandle handle)
				// System.Reflection.MethodBase.GetMethodFromHandle (RuntimeMethodHandle handle, RuntimeTypeHandle declaringType)
				"GetMethodFromHandle" when calledMethod.IsDeclaredOnType ("System.Reflection", "MethodBase")
					&& calledMethod.HasParameterOfType (0, "System", "RuntimeMethodHandle")
					&& (calledMethod.Parameters.Count == 1 || calledMethod.Parameters.Count == 2)
					=> IntrinsicId.MethodBase_GetMethodFromHandle,

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
				// static System.Linq.Expressions.Expression.Property (Expression, MethodInfo)
				"Property" when calledMethod.IsDeclaredOnType ("System.Linq.Expressions", "Expression")
					&& ((calledMethod.HasParameterOfType (1, "System", "Type") && calledMethod.Parameters.Count == 3)
					|| (calledMethod.HasParameterOfType (1, "System.Reflection", "MethodInfo") && calledMethod.Parameters.Count == 2))
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
				// System.Type.GetConstructor (BindingFlags, Type[])
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
				// System.Type.GetMethod (string, BindingFlags, Type[])
				// System.Type.GetMethod (string, BindingFlags, Binder, Type[], ParameterModifier[])
				// System.Type.GetMethod (string, BindingFlags, Binder, CallingConventions, Type[], ParameterModifier[])
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

				// System.Type.GetInterface (string)
				// System.Type.GetInterface (string, bool)
				"GetInterface" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& calledMethod.HasParameterOfType (0, "System", "String")
					&& calledMethod.HasThis
					&& (calledMethod.Parameters.Count == 1 ||
					(calledMethod.Parameters.Count == 2 && calledMethod.HasParameterOfType (1, "System", "Boolean")))
					=> IntrinsicId.Type_GetInterface,

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

				// System.Type.BaseType
				"get_BaseType" when calledMethod.IsDeclaredOnType ("System", "Type")
					&& !calledMethod.HasParameters
					&& calledMethod.HasThis
					=> IntrinsicId.Type_get_BaseType,

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

		public override bool HandleCall (MethodBody callingMethodBody, MethodReference calledMethod, Instruction operation, ValueNodeList methodParams, out ValueNode? methodReturnValue)
		{
			methodReturnValue = null;

			var reflectionProcessed = _markStep.ProcessReflectionDependency (callingMethodBody, operation);
			if (reflectionProcessed)
				return false;

			var callingMethodDefinition = callingMethodBody.Method;
			var calledMethodDefinition = _context.TryResolve (calledMethod);
			if (calledMethodDefinition == null)
				return false;

			_scopeStack.UpdateCurrentScopeInstructionOffset (operation.Offset);
			var reflectionContext = new ReflectionPatternContext (
				_context,
				ShouldEnableReflectionPatternReporting (),
				_scopeStack.CurrentScope.Origin,
				calledMethodDefinition,
				operation);

			DynamicallyAccessedMemberTypes returnValueDynamicallyAccessedMemberTypes = 0;

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
						methodReturnValue = new ArrayValue (new ConstIntValue (0), ((GenericInstanceMethod) calledMethod).GenericArguments[0]);
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

				// System.Reflection.MethodBase.GetMethodFromHandle (RuntimeMethodHandle handle)
				// System.Reflection.MethodBase.GetMethodFromHandle (RuntimeMethodHandle handle, RuntimeTypeHandle declaringType)
				case IntrinsicId.MethodBase_GetMethodFromHandle: {
						// Infrastructure piece to support "ldtoken method -> GetMethodFromHandle"
						if (methodParams[0] is RuntimeMethodHandleValue methodHandle)
							methodReturnValue = new SystemReflectionMethodBaseValue (methodHandle.MethodRepresented);
					}
					break;

				//
				// System.Type
				//
				// Type MakeGenericType (params Type[] typeArguments)
				//
				case IntrinsicId.Type_MakeGenericType: {
						reflectionContext.AnalyzingPattern ();
						foreach (var value in methodParams[0].UniqueValues ()) {
							if (value is SystemTypeValue typeValue) {
								if (AnalyzeGenericInstantiationTypeArray (methodParams[1], ref reflectionContext, calledMethodDefinition, typeValue.TypeRepresented.GenericParameters)) {
									reflectionContext.RecordHandledPattern ();
								} else {
									bool hasUncheckedAnnotation = false;
									foreach (var genericParameter in typeValue.TypeRepresented.GenericParameters) {
										if (_context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (genericParameter) != DynamicallyAccessedMemberTypes.None ||
											(genericParameter.HasDefaultConstructorConstraint && !typeValue.TypeRepresented.IsTypeOf ("System", "Nullable`1"))) {
											// If we failed to analyze the array, we go through the analyses again
											// and intentionally ignore one particular annotation:
											// Special case: Nullable<T> where T : struct
											//  The struct constraint in C# implies new() constraints, but Nullable doesn't make a use of that part.
											//  There are several places even in the framework where typeof(Nullable<>).MakeGenericType would warn
											//  without any good reason to do so.
											hasUncheckedAnnotation = true;
											break;
										}
									}
									if (hasUncheckedAnnotation) {
										reflectionContext.RecordUnrecognizedPattern ((int) DiagnosticId.MakeGenericType,
											new DiagnosticString (DiagnosticId.MakeGenericType).GetMessage (calledMethodDefinition.GetDisplayName ()));
									}
								}

								// We haven't found any generic parameters with annotations, so there's nothing to validate.
								reflectionContext.RecordHandledPattern ();
							} else if (value == NullValue.Instance)
								reflectionContext.RecordHandledPattern ();
							else {
								// We have no way to "include more" to fix this if we don't know, so we have to warn
								reflectionContext.RecordUnrecognizedPattern ((int) DiagnosticId.MakeGenericType,
											new DiagnosticString (DiagnosticId.MakeGenericType).GetMessage (calledMethodDefinition.GetDisplayName ()));
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
											ProcessGetMethodByName (ref reflectionContext, systemTypeValue.TypeRepresented, stringValue.Contents, bindingFlags, ref methodReturnValue);
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

						bool hasTypeArguments = (methodParams[2] as ArrayValue)?.Size.AsConstInt () != 0;
						foreach (var value in methodParams[0].UniqueValues ()) {
							if (value is SystemTypeValue systemTypeValue) {
								foreach (var stringParam in methodParams[1].UniqueValues ()) {
									if (stringParam is KnownStringValue stringValue) {
										foreach (var method in systemTypeValue.TypeRepresented.GetMethodsOnTypeHierarchy (_context, m => m.Name == stringValue.Contents, bindingFlags)) {
											ValidateGenericMethodInstantiation (ref reflectionContext, method, methodParams[2], calledMethod);
											MarkMethod (ref reflectionContext, method);
										}

										reflectionContext.RecordHandledPattern ();
									} else {
										if (hasTypeArguments) {
											// We don't know what method the `MakeGenericMethod` was called on, so we have to assume
											// that the method may have requirements which we can't fullfil -> warn.
											reflectionContext.RecordUnrecognizedPattern ((int) DiagnosticId.MakeGenericMethod,
												new DiagnosticString (DiagnosticId.MakeGenericMethod).GetMessage (DiagnosticUtilities.GetMethodSignatureDisplayName (calledMethod)));
										}

										RequireDynamicallyAccessedMembers (
											ref reflectionContext,
											GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (bindingFlags),
											value,
											calledMethod.Parameters[0]);
									}
								}
							} else {
								if (hasTypeArguments) {
									// We don't know what method the `MakeGenericMethod` was called on, so we have to assume
									// that the method may have requirements which we can't fullfil -> warn.
									reflectionContext.RecordUnrecognizedPattern ((int) DiagnosticId.MakeGenericMethod,
										new DiagnosticString (DiagnosticId.MakeGenericMethod).GetMessage (DiagnosticUtilities.GetMethodSignatureDisplayName (calledMethod)));
								}

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
				// static Property (Expression, MethodInfo)
				//
				case IntrinsicId.Expression_Property when calledMethod.HasParameterOfType (1, "System.Reflection", "MethodInfo"): {
						reflectionContext.AnalyzingPattern ();

						foreach (var value in methodParams[1].UniqueValues ()) {
							if (value is SystemReflectionMethodBaseValue methodBaseValue) {
								// We have one of the accessors for the property. The Expression.Property will in this case search
								// for the matching PropertyInfo and store that. So to be perfectly correct we need to mark the
								// respective PropertyInfo as "accessed via reflection".
								if (methodBaseValue.MethodRepresented.TryGetProperty (out PropertyDefinition? propertyDefinition)) {
									MarkProperty (ref reflectionContext, propertyDefinition);
									continue;
								}
							} else if (value == NullValue.Instance) {
								reflectionContext.RecordHandledPattern ();
								continue;
							}

							// In all other cases we may not even know which type this is about, so there's nothing we can do
							// report it as a warning.
							reflectionContext.RecordUnrecognizedPattern (
								2103, string.Format (Resources.Strings.IL2103,
									DiagnosticUtilities.GetParameterNameForErrorMessage (calledMethod.Parameters[1]),
									DiagnosticUtilities.GetMethodSignatureDisplayName (calledMethod)));
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
										BindingFlags bindingFlags = methodParams[0]?.Kind == ValueNodeKind.Null ? BindingFlags.Static : BindingFlags.Default;
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
						foreach (var valueNode in methodParams[0].UniqueValues ()) {
							// Note that valueNode can be statically typed in IL as some generic argument type.
							// For example:
							//   void Method<T>(T instance) { instance.GetType().... }
							// Currently this case will end up with null StaticType - since there's no typedef for the generic argument type.
							// But it could be that T is annotated with for example PublicMethods:
							//   void Method<[DAM(PublicMethods)] T>(T instance) { instance.GetType().GetMethod("Test"); }
							// In this case it's in theory possible to handle it, by treating the T basically as a base class
							// for the actual type of "instance". But the analysis for this would be pretty complicated (as the marking
							// has to happen on the callsite, which doesn't know that GetType() will be used...).
							// For now we're intentionally ignoring this case - it will produce a warning.
							// The counter example is:
							//   Method<Base>(new Derived);
							// In this case to get correct results, trimmer would have to mark all public methods on Derived. Which
							// currently it won't do.

							TypeDefinition? staticType = valueNode.StaticType;
							if (staticType is null) {
								// We don't know anything about the type GetType was called on. Track this as a usual result of a method call without any annotations
								methodReturnValue = MergePointValue.MergeValues (methodReturnValue, CreateMethodReturnValue (calledMethod));
							} else if (staticType.IsSealed || staticType.IsTypeOf ("System", "Delegate")) {
								// We can treat this one the same as if it was a typeof() expression

								// We can allow Object.GetType to be modeled as System.Delegate because we keep all methods
								// on delegates anyway so reflection on something this approximation would miss is actually safe.

								// We ignore the fact that the type can be annotated (see below for handling of annotated types)
								// This means the annotations (if any) won't be applied - instead we rely on the exact knowledge
								// of the type. So for example even if the type is annotated with PublicMethods
								// but the code calls GetProperties on it - it will work - mark properties, don't mark methods
								// since we ignored the fact that it's annotated.
								// This can be seen a little bit as a violation of the annotation, but we already have similar cases
								// where a parameter is annotated and if something in the method sets a specific known type to it
								// we will also make it just work, even if the annotation doesn't match the usage.
								methodReturnValue = MergePointValue.MergeValues (methodReturnValue, new SystemTypeValue (staticType));
							} else {
								reflectionContext.AnalyzingPattern ();

								// Make sure the type is marked (this will mark it as used via reflection, which is sort of true)
								// This should already be true for most cases (method params, fields, ...), but just in case
								MarkType (ref reflectionContext, staticType);

								var annotation = _markStep.DynamicallyAccessedMembersTypeHierarchy
									.ApplyDynamicallyAccessedMembersToTypeHierarchy (this, staticType);

								reflectionContext.RecordHandledPattern ();

								// Return a value which is "unknown type" with annotation. For now we'll use the return value node
								// for the method, which means we're loosing the information about which staticType this
								// started with. For now we don't need it, but we can add it later on.
								methodReturnValue = MergePointValue.MergeValues (methodReturnValue, CreateMethodReturnValue (calledMethod, annotation));
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
								if (!_context.TypeNameResolver.TryResolveTypeName (knownStringValue.Contents, callingMethodDefinition, out TypeReference? foundTypeRef, out AssemblyDefinition? typeAssembly, false)
									|| ResolveToTypeDefinition (foundTypeRef) is not TypeDefinition foundType) {
									// Intentionally ignore - it's not wrong for code to call Type.GetType on non-existing name, the code might expect null/exception back.
									reflectionContext.RecordHandledPattern ();
								} else {
									reflectionContext.RecordRecognizedPattern (foundType, () => _markStep.MarkTypeVisibleToReflection (foundTypeRef, foundType, new DependencyInfo (DependencyKind.AccessedViaReflection, callingMethodDefinition)));
									methodReturnValue = MergePointValue.MergeValues (methodReturnValue, new SystemTypeValue (foundType));
									_context.MarkingHelpers.MarkMatchingExportedType (foundType, typeAssembly, new DependencyInfo (DependencyKind.AccessedViaReflection, foundType), reflectionContext.Origin);
								}
							} else if (typeNameValue == NullValue.Instance) {
								reflectionContext.RecordHandledPattern ();
							} else if (typeNameValue is LeafValueWithDynamicallyAccessedMemberNode valueWithDynamicallyAccessedMember && valueWithDynamicallyAccessedMember.DynamicallyAccessedMemberTypes != 0) {
								// Propagate the annotation from the type name to the return value. Annotation on a string value will be fullfilled whenever a value is assigned to the string with annotation.
								// So while we don't know which type it is, we can guarantee that it will fulfill the annotation.
								reflectionContext.RecordHandledPattern ();
								methodReturnValue = MergePointValue.MergeValues (methodReturnValue, CreateMethodReturnValue (calledMethodDefinition, valueWithDynamicallyAccessedMember.DynamicallyAccessedMemberTypes));
							} else {
								reflectionContext.RecordUnrecognizedPattern (2057, $"Unrecognized value passed to the parameter 'typeName' of method '{calledMethod.GetDisplayName ()}'. It's not possible to guarantee the availability of the target type.");
							}
						}

					}
					break;

				//
				// GetConstructor (Type[])
				// GetConstructor (BindingFlags, Type[])
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
							2 => (methodParams[2] as ArrayValue)?.Size.AsConstInt (),
							4 => (methodParams[3] as ArrayValue)?.Size.AsConstInt (),
							5 => (methodParams[4] as ArrayValue)?.Size.AsConstInt (),
							_ => null,
						};

						// Go over all types we've seen
						foreach (var value in methodParams[0].UniqueValues ()) {
							if (value is SystemTypeValue systemTypeValue) {
								if (BindingFlagsAreUnsupported (bindingFlags)) {
									RequireDynamicallyAccessedMembers (ref reflectionContext, DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors, value, calledMethodDefinition);
								} else {
									if (HasBindingFlag (bindingFlags, BindingFlags.Public) && !HasBindingFlag (bindingFlags, BindingFlags.NonPublic)
										&& ctorParameterCount == 0) {
										MarkConstructorsOnType (ref reflectionContext, systemTypeValue.TypeRepresented, m => m.IsPublic && m.Parameters.Count == 0, bindingFlags);
									} else {
										MarkConstructorsOnType (ref reflectionContext, systemTypeValue.TypeRepresented, null, bindingFlags);
									}
								}
								reflectionContext.RecordHandledPattern ();
							} else {
								// Otherwise fall back to the bitfield requirements
								var requiredMemberTypes = GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors (bindingFlags);
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
				// GetMethod (string, BindingFlags, Type[])
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
										if (BindingFlagsAreUnsupported (bindingFlags)) {
											RequireDynamicallyAccessedMembers (ref reflectionContext, DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods, value, calledMethodDefinition);
										} else {
											ProcessGetMethodByName (ref reflectionContext, systemTypeValue.TypeRepresented, stringValue.Contents, bindingFlags, ref methodReturnValue);
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
											TypeDefinition[]? matchingNestedTypes = MarkNestedTypesOnType (ref reflectionContext, systemTypeValue.TypeRepresented, m => m.Name == stringValue.Contents, bindingFlags);

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
							methodReturnValue = CreateMethodReturnValue (calledMethodDefinition, DynamicallyAccessedMemberTypes.All);
					}
					break;

				//
				// AssemblyQualifiedName
				//
				case IntrinsicId.Type_get_AssemblyQualifiedName: {
						ValueNode? transformedResult = null;
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
				// Type.BaseType
				//
				case IntrinsicId.Type_get_BaseType: {
						foreach (var value in methodParams[0].UniqueValues ()) {
							if (value is LeafValueWithDynamicallyAccessedMemberNode dynamicallyAccessedMemberNode) {
								DynamicallyAccessedMemberTypes propagatedMemberTypes = DynamicallyAccessedMemberTypes.None;
								if (dynamicallyAccessedMemberNode.DynamicallyAccessedMemberTypes == DynamicallyAccessedMemberTypes.All)
									propagatedMemberTypes = DynamicallyAccessedMemberTypes.All;
								else {
									// PublicConstructors are not propagated to base type

									if (dynamicallyAccessedMemberNode.DynamicallyAccessedMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicEvents))
										propagatedMemberTypes |= DynamicallyAccessedMemberTypes.PublicEvents;

									if (dynamicallyAccessedMemberNode.DynamicallyAccessedMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicFields))
										propagatedMemberTypes |= DynamicallyAccessedMemberTypes.PublicFields;

									if (dynamicallyAccessedMemberNode.DynamicallyAccessedMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicMethods))
										propagatedMemberTypes |= DynamicallyAccessedMemberTypes.PublicMethods;

									// PublicNestedTypes are not propagated to base type

									// PublicParameterlessConstructor is not propagated to base type

									if (dynamicallyAccessedMemberNode.DynamicallyAccessedMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicProperties))
										propagatedMemberTypes |= DynamicallyAccessedMemberTypes.PublicProperties;
								}

								methodReturnValue = MergePointValue.MergeValues (methodReturnValue, CreateMethodReturnValue (calledMethod, propagatedMemberTypes));
							} else if (value is SystemTypeValue systemTypeValue) {
								if (systemTypeValue.TypeRepresented.BaseType is TypeReference baseTypeRef && _context.TryResolve (baseTypeRef) is TypeDefinition baseTypeDefinition)
									methodReturnValue = MergePointValue.MergeValues (methodReturnValue, new SystemTypeValue (baseTypeDefinition));
								else
									methodReturnValue = MergePointValue.MergeValues (methodReturnValue, CreateMethodReturnValue (calledMethod));
							} else if (value == NullValue.Instance) {
								// Ignore nulls - null.BaseType will fail at runtime, but it has no effect on static analysis
								continue;
							} else {
								// Unknown input - propagate a return value without any annotation - we know it's a Type but we know nothing about it
								methodReturnValue = MergePointValue.MergeValues (methodReturnValue, CreateMethodReturnValue (calledMethod));
							}
						}
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
				// GetInterface (String)
				// GetInterface (String, bool)
				//
				case IntrinsicId.Type_GetInterface: {
						reflectionContext.AnalyzingPattern ();

						foreach (var value in methodParams[0].UniqueValues ()) {
							// For now no support for marking a single interface by name. We would have to correctly support
							// mangled names for generics to do that correctly. Simply mark all interfaces on the type for now.

							// Require Interfaces annotation
							RequireDynamicallyAccessedMembers (ref reflectionContext, DynamicallyAccessedMemberTypesOverlay.Interfaces, value, calledMethodDefinition);

							// Interfaces is transitive, so the return values will always have at least Interfaces annotation
							DynamicallyAccessedMemberTypes returnMemberTypes = DynamicallyAccessedMemberTypesOverlay.Interfaces;

							// Propagate All annotation across the call - All is a superset of Interfaces
							if (value is LeafValueWithDynamicallyAccessedMemberNode annotatedNode
								&& annotatedNode.DynamicallyAccessedMemberTypes == DynamicallyAccessedMemberTypes.All)
								returnMemberTypes = DynamicallyAccessedMemberTypes.All;

							methodReturnValue = MergePointValue.MergeValues (methodReturnValue, CreateMethodReturnValue (calledMethod, returnMemberTypes));
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

								if (methodParams.Count > argsParam) {
									if (methodParams[argsParam] is ArrayValue arrayValue &&
										arrayValue.Size.AsConstInt () != null)
										ctorParameterCount = arrayValue.Size.AsConstInt ();
									else if (methodParams[argsParam] is NullValue)
										ctorParameterCount = 0;
								}

								if (parameters.Count > 3) {
									if (methodParams[1].AsConstInt () is int constInt)
										bindingFlags |= (BindingFlags) constInt;
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
								var requiredMemberTypes = GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors (bindingFlags);

								// Special case the public parameterless constructor if we know that there are 0 args passed in
								if (ctorParameterCount == 0 && requiredMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicConstructors)) {
									requiredMemberTypes &= ~DynamicallyAccessedMemberTypes.PublicConstructors;
									requiredMemberTypes |= DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;
								}

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
					// For now always fail since we don't track assemblies (dotnet/linker/issues/1947)
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
								_markStep.MarkStaticConstructorVisibleToReflection (runtimeTypeHandleValue.TypeRepresented, new DependencyInfo (DependencyKind.AccessedViaReflection, reflectionContext.Source));
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

						foreach (var methodValue in methodParams[0].UniqueValues ()) {
							if (methodValue is SystemReflectionMethodBaseValue methodBaseValue) {
								ValidateGenericMethodInstantiation (ref reflectionContext, methodBaseValue.MethodRepresented, methodParams[1], calledMethod);
							} else if (methodValue == NullValue.Instance) {
								reflectionContext.RecordHandledPattern ();
							} else {
								// We don't know what method the `MakeGenericMethod` was called on, so we have to assume
								// that the method may have requirements which we can't fullfil -> warn.
								reflectionContext.RecordUnrecognizedPattern ((int) DiagnosticId.MakeGenericMethod,
									new DiagnosticString (DiagnosticId.MakeGenericMethod).GetMessage (DiagnosticUtilities.GetMethodSignatureDisplayName (calledMethod)));
							}
						}

						// MakeGenericMethod doesn't change the identity of the MethodBase we're tracking so propagate to the return value
						methodReturnValue = methodParams[0];
					}
					break;

				default:

					if (calledMethodDefinition.IsPInvokeImpl) {
						// Is the PInvoke dangerous?
						bool comDangerousMethod = IsComInterop (calledMethodDefinition.MethodReturnType, calledMethodDefinition.ReturnType);
						foreach (ParameterDefinition pd in calledMethodDefinition.Parameters) {
							comDangerousMethod |= IsComInterop (pd, pd.ParameterType);
						}

						if (comDangerousMethod) {
							reflectionContext.AnalyzingPattern ();
							reflectionContext.RecordUnrecognizedPattern (2050, $"P/invoke method '{calledMethodDefinition.GetDisplayName ()}' declares a parameter with COM marshalling. Correctness of COM interop cannot be guaranteed after trimming. Interfaces and interface members might be removed.");
						}
					}

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

					_markStep.CheckAndReportRequiresUnreferencedCode (calledMethodDefinition);

					// To get good reporting of errors we need to track the origin of the value for all method calls
					// but except Newobj as those are special.
					if (GetReturnTypeWithoutModifiers (calledMethodDefinition.ReturnType).MetadataType != MetadataType.Void) {
						methodReturnValue = CreateMethodReturnValue (calledMethodDefinition, returnValueDynamicallyAccessedMemberTypes);

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
				if (GetReturnTypeWithoutModifiers (calledMethod.ReturnType).MetadataType != MetadataType.Void) {
					methodReturnValue = CreateMethodReturnValue (calledMethodDefinition, returnValueDynamicallyAccessedMemberTypes);
				}
			}

			// Validate that the return value has the correct annotations as per the method return value annotations
			if (returnValueDynamicallyAccessedMemberTypes != 0) {
				foreach (var uniqueValue in methodReturnValue.UniqueValues ()) {
					if (uniqueValue is LeafValueWithDynamicallyAccessedMemberNode methodReturnValueWithMemberTypes) {
						if (!methodReturnValueWithMemberTypes.DynamicallyAccessedMemberTypes.HasFlag (returnValueDynamicallyAccessedMemberTypes))
							throw new InvalidOperationException ($"Internal linker error: processing of call from {callingMethodDefinition.GetDisplayName ()} to {calledMethod.GetDisplayName ()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
					} else if (uniqueValue is SystemTypeValue) {
						// SystemTypeValue can fullfill any requirement, so it's always valid
						// The requirements will be applied at the point where it's consumed (passed as a method parameter, set as field value, returned from the method)
					} else {
						throw new InvalidOperationException ($"Internal linker error: processing of call from {callingMethodDefinition.GetDisplayName ()} to {calledMethod.GetDisplayName ()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
					}
				}
			}

			return true;
		}

		bool IsComInterop (IMarshalInfoProvider marshalInfoProvider, TypeReference parameterType)
		{
			// This is best effort. One can likely find ways how to get COM without triggering these alarms.
			// AsAny marshalling of a struct with an object-typed field would be one, for example.

			// This logic roughly corresponds to MarshalInfo::MarshalInfo in CoreCLR,
			// not trying to handle invalid cases and distinctions that are not interesting wrt
			// "is this COM?" question.

			NativeType nativeType = NativeType.None;
			if (marshalInfoProvider.HasMarshalInfo) {
				nativeType = marshalInfoProvider.MarshalInfo.NativeType;
			}

			if (nativeType == NativeType.IUnknown || nativeType == NativeType.IDispatch || nativeType == NativeType.IntF) {
				// This is COM by definition
				return true;
			}

			if (nativeType == NativeType.None) {
				// Resolve will look at the element type
				var parameterTypeDef = _context.TryResolve (parameterType);

				if (parameterTypeDef != null) {
					if (parameterTypeDef.IsTypeOf ("System", "Array")) {
						// System.Array marshals as IUnknown by default
						return true;
					} else if (parameterTypeDef.IsTypeOf ("System", "String") ||
						parameterTypeDef.IsTypeOf ("System.Text", "StringBuilder")) {
						// String and StringBuilder are special cased by interop
						return false;
					}

					if (parameterTypeDef.IsValueType) {
						// Value types don't marshal as COM
						return false;
					} else if (parameterTypeDef.IsInterface) {
						// Interface types marshal as COM by default
						return true;
					} else if (parameterTypeDef.IsMulticastDelegate ()) {
						// Delegates are special cased by interop
						return false;
					} else if (parameterTypeDef.IsSubclassOf ("System.Runtime.InteropServices", "CriticalHandle", _context)) {
						// Subclasses of CriticalHandle are special cased by interop
						return false;
					} else if (parameterTypeDef.IsSubclassOf ("System.Runtime.InteropServices", "SafeHandle", _context)) {
						// Subclasses of SafeHandle are special cased by interop
						return false;
					} else if (!parameterTypeDef.IsSequentialLayout && !parameterTypeDef.IsExplicitLayout) {
						// Rest of classes that don't have layout marshal as COM
						return true;
					}
				}
			}

			return false;
		}

		bool AnalyzeGenericInstantiationTypeArray (ValueNode? arrayParam, ref ReflectionPatternContext reflectionContext, MethodReference calledMethod, IList<GenericParameter> genericParameters)
		{
			bool hasRequirements = false;
			foreach (var genericParameter in genericParameters) {
				if (_context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (genericParameter) != DynamicallyAccessedMemberTypes.None) {
					hasRequirements = true;
					break;
				}
			}

			// If there are no requirements, then there's no point in warning
			if (!hasRequirements)
				return true;

			foreach (var typesValue in arrayParam.UniqueValues ()) {
				if (typesValue.Kind != ValueNodeKind.Array) {
					return false;
				}
				ArrayValue array = (ArrayValue) typesValue;
				int? size = array.Size.AsConstInt ();
				if (size == null || size != genericParameters.Count) {
					return false;
				}
				bool allIndicesKnown = true;
				for (int i = 0; i < size.Value; i++) {
					if (!array.IndexValues.TryGetValue (i, out ValueBasicBlockPair value) || value.Value is null or { Kind: ValueNodeKind.Unknown }) {
						allIndicesKnown = false;
						break;
					}
				}

				if (!allIndicesKnown) {
					return false;
				}

				for (int i = 0; i < size.Value; i++) {
					if (array.IndexValues.TryGetValue (i, out ValueBasicBlockPair value)) {
						RequireDynamicallyAccessedMembers (
							ref reflectionContext,
							_context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (genericParameters[i]),
							value.Value,
							calledMethod.Resolve ());
					}
				}
			}
			return true;
		}

		void ProcessCreateInstanceByName (ref ReflectionPatternContext reflectionContext, MethodDefinition calledMethod, ValueNodeList methodParams)
		{
			reflectionContext.AnalyzingPattern ();

			BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
			bool parameterlessConstructor = true;
			if (calledMethod.Parameters.Count == 8 && calledMethod.Parameters[2].ParameterType.MetadataType == MetadataType.Boolean) {
				parameterlessConstructor = false;
				bindingFlags = BindingFlags.Instance;
				if (methodParams[3].AsConstInt () is int bindingFlagsInt)
					bindingFlags |= (BindingFlags) bindingFlagsInt;
				else
					bindingFlags |= BindingFlags.Public | BindingFlags.NonPublic;
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

							if (!_context.TypeNameResolver.TryResolveTypeName (resolvedAssembly, typeNameStringValue.Contents, out TypeReference? typeRef)
								|| _context.TryResolve (typeRef) is not TypeDefinition resolvedType
								|| typeRef is ArrayType) {
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

		void ProcessGetMethodByName (
			ref ReflectionPatternContext reflectionContext,
			TypeDefinition typeDefinition,
			string methodName,
			BindingFlags? bindingFlags,
			ref ValueNode? methodReturnValue)
		{
			bool foundAny = false;
			foreach (var method in typeDefinition.GetMethodsOnTypeHierarchy (_context, m => m.Name == methodName, bindingFlags)) {
				MarkMethod (ref reflectionContext, method);
				methodReturnValue = MergePointValue.MergeValues (methodReturnValue, new SystemReflectionMethodBaseValue (method));
				foundAny = true;
			}

			// If there were no methods found the API will return null at runtime, so we should
			// track the null as a return value as well.
			// This also prevents warnings in such case, since if we don't set the return value it will be
			// "unknown" and consumers may warn.
			if (!foundAny)
				methodReturnValue = MergePointValue.MergeValues (methodReturnValue, NullValue.Instance);
		}

		public static DynamicallyAccessedMemberTypes GetMissingMemberTypes (DynamicallyAccessedMemberTypes requiredMemberTypes, DynamicallyAccessedMemberTypes availableMemberTypes)
		{
			if (availableMemberTypes.HasFlag (requiredMemberTypes))
				return DynamicallyAccessedMemberTypes.None;

			if (requiredMemberTypes == DynamicallyAccessedMemberTypes.All)
				return DynamicallyAccessedMemberTypes.All;

			var missingMemberTypes = requiredMemberTypes & ~availableMemberTypes;

			// PublicConstructors is a special case since its value is 3 - so PublicParameterlessConstructor (1) | _PublicConstructor_WithMoreThanOneParameter_ (2)
			// The above bit logic only works for value with single bit set.
			if (requiredMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicConstructors) &&
				!availableMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicConstructors))
				missingMemberTypes |= DynamicallyAccessedMemberTypes.PublicConstructors;

			return missingMemberTypes;
		}

		string GetMemberTypesString (DynamicallyAccessedMemberTypes memberTypes)
		{
			Debug.Assert (memberTypes != DynamicallyAccessedMemberTypes.None);

			if (memberTypes == DynamicallyAccessedMemberTypes.All)
				return $"'{nameof (DynamicallyAccessedMemberTypes)}.{nameof (DynamicallyAccessedMemberTypes.All)}'";

			var memberTypesList = AllDynamicallyAccessedMemberTypes
				.Where (damt => (memberTypes & damt) == damt && damt != DynamicallyAccessedMemberTypes.None)
				.ToList ();

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicConstructors))
				memberTypesList.Remove (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor);

			return string.Join (", ", memberTypesList.Select (mt => {
				string mtName = mt == DynamicallyAccessedMemberTypesOverlay.Interfaces
					? nameof (DynamicallyAccessedMemberTypesOverlay.Interfaces)
					: mt.ToString ();

				return $"'{nameof (DynamicallyAccessedMemberTypes)}.{mtName}'";
			}));
		}

		void RequireDynamicallyAccessedMembers (ref ReflectionPatternContext reflectionContext, DynamicallyAccessedMemberTypes requiredMemberTypes, ValueNode? value, IMetadataTokenProvider targetContext)
		{
			foreach (var uniqueValue in value.UniqueValues ()) {
				if (requiredMemberTypes == DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
					&& uniqueValue is SystemTypeForGenericParameterValue genericParam
					&& genericParam.GenericParameter.HasDefaultConstructorConstraint) {
					// We allow a new() constraint on a generic parameter to satisfy DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
					reflectionContext.RecordHandledPattern ();
				} else if (uniqueValue is LeafValueWithDynamicallyAccessedMemberNode valueWithDynamicallyAccessedMember) {
					var availableMemberTypes = valueWithDynamicallyAccessedMember.DynamicallyAccessedMemberTypes;
					var missingMemberTypesValue = GetMissingMemberTypes (requiredMemberTypes, availableMemberTypes);
					if (missingMemberTypesValue != DynamicallyAccessedMemberTypes.None) {
						var missingMemberTypes = GetMemberTypesString (missingMemberTypesValue);

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
					MarkTypeForDynamicallyAccessedMembers (ref reflectionContext, systemTypeValue.TypeRepresented, requiredMemberTypes, DependencyKind.DynamicallyAccessedMember);
				} else if (uniqueValue is KnownStringValue knownStringValue) {
					if (!_context.TypeNameResolver.TryResolveTypeName (knownStringValue.Contents, reflectionContext.Source, out TypeReference? typeRef, out AssemblyDefinition? typeAssembly)
						|| ResolveToTypeDefinition (typeRef) is not TypeDefinition foundType) {
						// Intentionally ignore - it's not wrong for code to call Type.GetType on non-existing name, the code might expect null/exception back.
						reflectionContext.RecordHandledPattern ();
					} else {
						MarkType (ref reflectionContext, typeRef);
						MarkTypeForDynamicallyAccessedMembers (ref reflectionContext, foundType, requiredMemberTypes, DependencyKind.DynamicallyAccessedMember);
						_context.MarkingHelpers.MarkMatchingExportedType (foundType, typeAssembly, new DependencyInfo (DependencyKind.DynamicallyAccessedMember, foundType), reflectionContext.Origin);
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
						// Unknown value to generic parameter - this is possible if the generic argument fails to resolve
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

		static BindingFlags? GetBindingFlagsFromValue (ValueNode? parameter) => (BindingFlags?) parameter.AsConstInt ();

		static bool BindingFlagsAreUnsupported (BindingFlags? bindingFlags)
		{
			if (bindingFlags == null)
				return true;

			// Binding flags we understand
			const BindingFlags UnderstoodBindingFlags =
				BindingFlags.DeclaredOnly |
				BindingFlags.Instance |
				BindingFlags.Static |
				BindingFlags.Public |
				BindingFlags.NonPublic |
				BindingFlags.FlattenHierarchy |
				BindingFlags.ExactBinding;

			// Binding flags that don't affect binding outside InvokeMember (that we don't analyze).
			const BindingFlags IgnorableBindingFlags =
				BindingFlags.InvokeMethod |
				BindingFlags.CreateInstance |
				BindingFlags.GetField |
				BindingFlags.SetField |
				BindingFlags.GetProperty |
				BindingFlags.SetProperty;

			BindingFlags flags = bindingFlags.Value;
			return (flags & ~(UnderstoodBindingFlags | IgnorableBindingFlags)) != 0;
		}

		static bool HasBindingFlag (BindingFlags? bindingFlags, BindingFlags? search) => bindingFlags != null && (bindingFlags & search) == search;

		public void MarkTypeForDynamicallyAccessedMembers (ref ReflectionPatternContext reflectionContext, TypeDefinition typeDefinition, DynamicallyAccessedMemberTypes requiredMemberTypes, DependencyKind dependencyKind, bool declaredOnly = false)
		{
			foreach (var member in typeDefinition.GetDynamicallyAccessedMembers (_context, requiredMemberTypes, declaredOnly)) {
				switch (member) {
				case MethodDefinition method:
					MarkMethod (ref reflectionContext, method, dependencyKind);
					break;
				case FieldDefinition field:
					MarkField (ref reflectionContext, field, dependencyKind);
					break;
				case TypeDefinition nestedType:
					MarkType (ref reflectionContext, nestedType, dependencyKind);
					break;
				case PropertyDefinition property:
					MarkProperty (ref reflectionContext, property, dependencyKind);
					break;
				case EventDefinition @event:
					MarkEvent (ref reflectionContext, @event, dependencyKind);
					break;
				case InterfaceImplementation interfaceImplementation:
					MarkInterfaceImplementation (ref reflectionContext, interfaceImplementation, dependencyKind);
					break;
				}
			}
		}

		void MarkType (ref ReflectionPatternContext reflectionContext, TypeReference typeReference, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			var dependencyInfo = new DependencyInfo (dependencyKind, reflectionContext.Source);
			if (_context.TryResolve (typeReference) is TypeDefinition type)
				reflectionContext.RecordRecognizedPattern (type, () => _markStep.MarkTypeVisibleToReflection (typeReference, type, dependencyInfo));
		}

		void MarkMethod (ref ReflectionPatternContext reflectionContext, MethodDefinition method, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			var dependencyInfo = new DependencyInfo (dependencyKind, reflectionContext.Source);
			reflectionContext.RecordRecognizedPattern (method, () => _markStep.MarkMethodVisibleToReflection (method, dependencyInfo));
		}

		void MarkField (ref ReflectionPatternContext reflectionContext, FieldDefinition field, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			var dependencyInfo = new DependencyInfo (dependencyKind, reflectionContext.Source);
			reflectionContext.RecordRecognizedPattern (field, () => _markStep.MarkFieldVisibleToReflection (field, dependencyInfo));
		}

		void MarkProperty (ref ReflectionPatternContext reflectionContext, PropertyDefinition property, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			var dependencyInfo = new DependencyInfo (dependencyKind, reflectionContext.Source);
			reflectionContext.RecordRecognizedPattern (property, () => { _markStep.MarkPropertyVisibleToReflection (property, dependencyInfo); });
		}

		void MarkEvent (ref ReflectionPatternContext reflectionContext, EventDefinition @event, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			var dependencyInfo = new DependencyInfo (dependencyKind, reflectionContext.Source);
			reflectionContext.RecordRecognizedPattern (@event, () => { _markStep.MarkEventVisibleToReflection (@event, dependencyInfo); });
		}

		void MarkInterfaceImplementation (ref ReflectionPatternContext reflectionContext, InterfaceImplementation interfaceImplementation, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			var dependencyInfo = new DependencyInfo (dependencyKind, reflectionContext.Source);
			reflectionContext.RecordRecognizedPattern (interfaceImplementation, () => _markStep.MarkInterfaceImplementation (interfaceImplementation, null, dependencyInfo));
		}

		void MarkConstructorsOnType (ref ReflectionPatternContext reflectionContext, TypeDefinition type, Func<MethodDefinition, bool>? filter, BindingFlags? bindingFlags = null)
		{
			foreach (var ctor in type.GetConstructorsOnType (filter, bindingFlags))
				MarkMethod (ref reflectionContext, ctor);
		}

		void MarkFieldsOnTypeHierarchy (ref ReflectionPatternContext reflectionContext, TypeDefinition type, Func<FieldDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			foreach (var field in type.GetFieldsOnTypeHierarchy (_context, filter, bindingFlags))
				MarkField (ref reflectionContext, field);
		}

		TypeDefinition[]? MarkNestedTypesOnType (ref ReflectionPatternContext reflectionContext, TypeDefinition type, Func<TypeDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			var result = new ArrayBuilder<TypeDefinition> ();

			foreach (var nestedType in type.GetNestedTypesOnType (filter, bindingFlags)) {
				result.Add (nestedType);
				MarkType (ref reflectionContext, nestedType);
			}

			return result.ToArray ();
		}

		void MarkPropertiesOnTypeHierarchy (ref ReflectionPatternContext reflectionContext, TypeDefinition type, Func<PropertyDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			foreach (var property in type.GetPropertiesOnTypeHierarchy (_context, filter, bindingFlags))
				MarkProperty (ref reflectionContext, property);
		}

		void MarkEventsOnTypeHierarchy (ref ReflectionPatternContext reflectionContext, TypeDefinition type, Func<EventDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			foreach (var @event in type.GetEventsOnTypeHierarchy (_context, filter, bindingFlags))
				MarkEvent (ref reflectionContext, @event);
		}

		void ValidateGenericMethodInstantiation (
			ref ReflectionPatternContext reflectionContext,
			MethodDefinition genericMethod,
			ValueNode? genericParametersArray,
			MethodReference reflectionMethod)
		{
			if (!genericMethod.HasGenericParameters) {
				reflectionContext.RecordHandledPattern ();
				return;
			}

			if (!AnalyzeGenericInstantiationTypeArray (genericParametersArray, ref reflectionContext, reflectionMethod, genericMethod.GenericParameters)) {
				reflectionContext.RecordUnrecognizedPattern ((int) DiagnosticId.MakeGenericMethod,
					new DiagnosticString (DiagnosticId.MakeGenericMethod).GetMessage (DiagnosticUtilities.GetMethodSignatureDisplayName (reflectionMethod)));
			} else {
				reflectionContext.RecordHandledPattern ();
			}
		}

		static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForNestedTypes (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicNestedTypes : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicNestedTypes : DynamicallyAccessedMemberTypes.None) |
			(BindingFlagsAreUnsupported (bindingFlags) ? DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes : DynamicallyAccessedMemberTypes.None);

		static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicConstructors : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicConstructors : DynamicallyAccessedMemberTypes.None) |
			(BindingFlagsAreUnsupported (bindingFlags) ? DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors : DynamicallyAccessedMemberTypes.None);

		static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicMethods : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicMethods : DynamicallyAccessedMemberTypes.None) |
			(BindingFlagsAreUnsupported (bindingFlags) ? DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods : DynamicallyAccessedMemberTypes.None);

		static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForFields (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicFields : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicFields : DynamicallyAccessedMemberTypes.None) |
			(BindingFlagsAreUnsupported (bindingFlags) ? DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields : DynamicallyAccessedMemberTypes.None);

		static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForProperties (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicProperties : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicProperties : DynamicallyAccessedMemberTypes.None) |
			(BindingFlagsAreUnsupported (bindingFlags) ? DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties : DynamicallyAccessedMemberTypes.None);

		static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForEvents (BindingFlags? bindingFlags) =>
			(HasBindingFlag (bindingFlags, BindingFlags.Public) ? DynamicallyAccessedMemberTypes.PublicEvents : DynamicallyAccessedMemberTypes.None) |
			(HasBindingFlag (bindingFlags, BindingFlags.NonPublic) ? DynamicallyAccessedMemberTypes.NonPublicEvents : DynamicallyAccessedMemberTypes.None) |
			(BindingFlagsAreUnsupported (bindingFlags) ? DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents : DynamicallyAccessedMemberTypes.None);
		static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForMembers (BindingFlags? bindingFlags) =>
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors (bindingFlags) |
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForEvents (bindingFlags) |
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForFields (bindingFlags) |
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (bindingFlags) |
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForProperties (bindingFlags) |
			GetDynamicallyAccessedMemberTypesFromBindingFlagsForNestedTypes (bindingFlags);
	}
}
