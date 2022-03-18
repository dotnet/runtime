// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ILLink.Shared;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Steps;

using BindingFlags = System.Reflection.BindingFlags;

using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace Mono.Linker.Dataflow
{
	class ReflectionMethodBodyScanner : MethodBodyScanner
	{
		readonly MarkStep _markStep;
		readonly MarkScopeStack _scopeStack;

		public static bool RequiresReflectionMethodBodyScannerForCallSite (LinkContext context, MethodReference calledMethod)
		{
			MethodDefinition? methodDefinition = context.TryResolve (calledMethod);
			if (methodDefinition == null)
				return false;

			return Intrinsics.GetIntrinsicIdForMethod (methodDefinition) > IntrinsicId.RequiresReflectionBodyScanner_Sentinel ||
				context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (methodDefinition) ||
				context.Annotations.DoesMethodRequireUnreferencedCode (methodDefinition, out _) ||
				methodDefinition.IsPInvokeImpl;
		}

		public static bool RequiresReflectionMethodBodyScannerForMethodBody (LinkContext context, MethodDefinition methodDefinition)
		{
			return Intrinsics.GetIntrinsicIdForMethod (methodDefinition) > IntrinsicId.RequiresReflectionBodyScanner_Sentinel ||
				context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (methodDefinition);
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
				var methodReturnValue = GetMethodReturnValue (method);
				if (methodReturnValue.DynamicallyAccessedMemberTypes != 0) {
					var analysisContext = new AnalysisContext (_scopeStack.CurrentScope.Origin, ShouldEnableReflectionPatternReporting (), _context);
					RequireDynamicallyAccessedMembers (analysisContext, ReturnValue, methodReturnValue);
				}
			}
		}

		public void ProcessAttributeDataflow (MethodDefinition method, IList<CustomAttributeArgument> arguments)
		{
			int paramOffset = method.HasImplicitThis () ? 1 : 0;

			for (int i = 0; i < method.Parameters.Count; i++) {
				var parameterValue = GetMethodParameterValue (method, i + paramOffset);
				if (parameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None) {
					MultiValue value = GetValueNodeForCustomAttributeArgument (arguments[i]);
					var analysisContext = new AnalysisContext (_scopeStack.CurrentScope.Origin, diagnosticsEnabled: true, _context);
					RequireDynamicallyAccessedMembers (analysisContext, value, parameterValue);
				}
			}
		}

		public void ProcessAttributeDataflow (FieldDefinition field, CustomAttributeArgument value)
		{
			MultiValue valueNode = GetValueNodeForCustomAttributeArgument (value);
			foreach (var fieldValueCandidate in GetFieldValue (field)) {
				if (fieldValueCandidate is not ValueWithDynamicallyAccessedMembers fieldValue)
					continue;

				var analysisContext = new AnalysisContext (_scopeStack.CurrentScope.Origin, diagnosticsEnabled: true, _context);
				RequireDynamicallyAccessedMembers (analysisContext, valueNode, fieldValue);
			}
		}

		MultiValue GetValueNodeForCustomAttributeArgument (CustomAttributeArgument argument)
		{
			SingleValue value;
			if (argument.Type.Name == "Type") {
				TypeDefinition? referencedType = ResolveToTypeDefinition ((TypeReference) argument.Value);
				if (referencedType == null)
					value = UnknownValue.Instance;
				else
					value = new SystemTypeValue (referencedType);
			} else if (argument.Type.MetadataType == MetadataType.String) {
				value = new KnownStringValue ((string) argument.Value);
			} else {
				// We shouldn't have gotten a non-null annotation for this from GetParameterAnnotation
				throw new InvalidOperationException ();
			}

			Debug.Assert (value != null);
			return value;
		}

		public void ProcessGenericArgumentDataFlow (GenericParameter genericParameter, TypeReference genericArgument)
		{
			var annotation = _context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (genericParameter);
			Debug.Assert (annotation != DynamicallyAccessedMemberTypes.None);

			var genericParameterValue = new GenericParameterValue (genericParameter, annotation);
			MultiValue genericArgumentValue = GetTypeValueNodeFromGenericArgument (genericArgument);

			var analysisContext = new AnalysisContext (_scopeStack.CurrentScope.Origin, ShouldEnableReflectionPatternReporting (), _context);
			RequireDynamicallyAccessedMembers (analysisContext, genericArgumentValue, genericParameterValue);
		}

		MultiValue GetTypeValueNodeFromGenericArgument (TypeReference genericArgument)
		{
			if (genericArgument is GenericParameter inputGenericParameter) {
				// Technically this should be a new value node type as it's not a System.Type instance representation, but just the generic parameter
				// That said we only use it to perform the dynamically accessed members checks and for that purpose treating it as System.Type is perfectly valid.
				return new GenericParameterValue (inputGenericParameter, _context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (inputGenericParameter));
			} else if (ResolveToTypeDefinition (genericArgument) is TypeDefinition genericArgumentType) {
				if (genericArgumentType.IsTypeOf ("System", "Nullable`1")) {
					var innerGenericArgument = (genericArgument as IGenericInstance)?.GenericArguments.FirstOrDefault ();
					switch (innerGenericArgument) {
					case GenericParameter gp:
						return new NullableValueWithDynamicallyAccessedMembers (genericArgumentType,
							new GenericParameterValue (gp, _context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (gp)));

					case TypeReference underlyingType:
						if (ResolveToTypeDefinition (underlyingType) is TypeDefinition underlyingTypeDefinition)
							return new NullableSystemTypeValue (genericArgumentType, new SystemTypeValue (underlyingTypeDefinition));
						else
							return UnknownValue.Instance;
					}
				}
				// All values except for Nullable<T>, including Nullable<> (with no type arguments)
				return new SystemTypeValue (genericArgumentType);
			} else {
				return UnknownValue.Instance;
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

		MethodReturnValue GetMethodReturnValue (MethodDefinition method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new (ResolveToTypeDefinition (method.ReturnType), method, dynamicallyAccessedMemberTypes);

		MethodReturnValue GetMethodReturnValue (MethodDefinition method)
			=> new (
				ResolveToTypeDefinition (method.ReturnType),
				method,
				_context.Annotations.FlowAnnotations.GetReturnParameterAnnotation (method));

		ValueWithDynamicallyAccessedMembers GetMethodParameterValue (MethodDefinition method, int parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> GetMethodParameterValueInternal (method, parameterIndex, dynamicallyAccessedMemberTypes);

		protected override ValueWithDynamicallyAccessedMembers GetMethodParameterValue (MethodDefinition method, int parameterIndex)
			=> GetMethodParameterValueInternal (method, parameterIndex, _context.Annotations.FlowAnnotations.GetParameterAnnotation (method, parameterIndex));

		ValueWithDynamicallyAccessedMembers GetMethodParameterValueInternal (MethodDefinition method, int parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			if (method.HasImplicitThis ()) {
				if (parameterIndex == 0)
					return new MethodThisParameterValue (method, dynamicallyAccessedMemberTypes);

				parameterIndex--;
			}

			return new MethodParameterValue (
				ResolveToTypeDefinition (method.Parameters[parameterIndex].ParameterType),
				method,
				parameterIndex,
				dynamicallyAccessedMemberTypes);
		}

		protected override MultiValue GetFieldValue (FieldDefinition field)
		{
			switch (field.Name) {
			case "EmptyTypes" when field.DeclaringType.IsTypeOf ("System", "Type"): {
					return ArrayValue.Create (0, field.DeclaringType);
				}
			case "Empty" when field.DeclaringType.IsTypeOf ("System", "String"): {
					return new KnownStringValue (string.Empty);
				}

			default: {
					DynamicallyAccessedMemberTypes memberTypes = _context.Annotations.FlowAnnotations.GetFieldAnnotation (field);
					return new FieldValue (ResolveToTypeDefinition (field.FieldType), field, memberTypes);
				}
			}
		}

		protected override void HandleStoreField (MethodDefinition method, FieldValue field, Instruction operation, MultiValue valueToStore)
		{
			if (field.DynamicallyAccessedMemberTypes != 0) {
				_scopeStack.UpdateCurrentScopeInstructionOffset (operation.Offset);
				var analysisContext = new AnalysisContext (_scopeStack.CurrentScope.Origin, ShouldEnableReflectionPatternReporting (), _context);
				RequireDynamicallyAccessedMembers (analysisContext, valueToStore, field);
			}
		}

		protected override void HandleStoreParameter (MethodDefinition method, MethodParameterValue parameter, Instruction operation, MultiValue valueToStore)
		{
			if (parameter.DynamicallyAccessedMemberTypes != 0) {
				_scopeStack.UpdateCurrentScopeInstructionOffset (operation.Offset);
				var analysisContext = new AnalysisContext (_scopeStack.CurrentScope.Origin, ShouldEnableReflectionPatternReporting (), _context);
				RequireDynamicallyAccessedMembers (analysisContext, valueToStore, parameter);
			}
		}

		public override bool HandleCall (MethodBody callingMethodBody, MethodReference calledMethod, Instruction operation, ValueNodeList methodParams, out MultiValue methodReturnValue)
		{
			methodReturnValue = new ();
			MultiValue? maybeMethodReturnValue = null;

			var reflectionProcessed = _markStep.ProcessReflectionDependency (callingMethodBody, operation);
			if (reflectionProcessed)
				return false;

			var callingMethodDefinition = callingMethodBody.Method;
			var calledMethodDefinition = _context.TryResolve (calledMethod);
			if (calledMethodDefinition == null)
				return false;

			_scopeStack.UpdateCurrentScopeInstructionOffset (operation.Offset);
			var analysisContext = new AnalysisContext (
				_scopeStack.CurrentScope.Origin,
				ShouldEnableReflectionPatternReporting (),
				_context);

			DynamicallyAccessedMemberTypes returnValueDynamicallyAccessedMemberTypes = 0;

			bool requiresDataFlowAnalysis = _context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (calledMethodDefinition);
			returnValueDynamicallyAccessedMemberTypes = requiresDataFlowAnalysis ?
				_context.Annotations.FlowAnnotations.GetReturnParameterAnnotation (calledMethodDefinition) : 0;

			var handleCallAction = new HandleCallAction (_context, this, analysisContext, callingMethodDefinition);
			switch (Intrinsics.GetIntrinsicIdForMethod (calledMethodDefinition)) {
			case IntrinsicId.IntrospectionExtensions_GetTypeInfo:
			case IntrinsicId.TypeInfo_AsType:
			case IntrinsicId.Type_get_UnderlyingSystemType:
			case IntrinsicId.Type_GetTypeFromHandle:
			case IntrinsicId.Type_get_TypeHandle:
			case IntrinsicId.Type_GetInterface:
			case IntrinsicId.Type_get_AssemblyQualifiedName:
			case IntrinsicId.RuntimeHelpers_RunClassConstructor:
			case var callType when (callType == IntrinsicId.Type_GetConstructors || callType == IntrinsicId.Type_GetMethods || callType == IntrinsicId.Type_GetFields ||
				callType == IntrinsicId.Type_GetProperties || callType == IntrinsicId.Type_GetEvents || callType == IntrinsicId.Type_GetNestedTypes || callType == IntrinsicId.Type_GetMembers)
				&& calledMethod.DeclaringType.Namespace == "System"
				&& calledMethod.DeclaringType.Name == "Type"
				&& calledMethod.Parameters[0].ParameterType.FullName == "System.Reflection.BindingFlags"
				&& calledMethod.HasThis:
			case var fieldPropertyOrEvent when (fieldPropertyOrEvent == IntrinsicId.Type_GetField || fieldPropertyOrEvent == IntrinsicId.Type_GetProperty || fieldPropertyOrEvent == IntrinsicId.Type_GetEvent)
				&& calledMethod.DeclaringType.Namespace == "System"
				&& calledMethod.DeclaringType.Name == "Type"
				&& calledMethod.Parameters[0].ParameterType.FullName == "System.String"
				&& calledMethod.HasThis:
			case var getRuntimeMember when getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeEvent
				|| getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeField
				|| getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeMethod
				|| getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeProperty:
			case IntrinsicId.Type_GetMember:
			case IntrinsicId.Type_GetMethod:
			case IntrinsicId.Type_GetNestedType:
			case IntrinsicId.Nullable_GetUnderlyingType:
			case IntrinsicId.Expression_Property when calledMethod.HasParameterOfType (1, "System.Reflection.MethodInfo"):
			case var fieldOrPropertyInstrinsic when fieldOrPropertyInstrinsic == IntrinsicId.Expression_Field || fieldOrPropertyInstrinsic == IntrinsicId.Expression_Property:
			case IntrinsicId.Type_get_BaseType: {
					var instanceValue = MultiValueLattice.Top;
					IReadOnlyList<MultiValue> parameterValues = methodParams;
					if (calledMethodDefinition.HasImplicitThis ()) {
						instanceValue = methodParams[0];
						parameterValues = parameterValues.Skip (1).ToImmutableList ();
					}
					return handleCallAction.Invoke (calledMethodDefinition, instanceValue, parameterValues, out methodReturnValue);
				}

			case IntrinsicId.TypeDelegator_Ctor: {
					// This is an identity function for analysis purposes
					if (operation.OpCode == OpCodes.Newobj)
						AddReturnValue (methodParams[1]);
				}
				break;

			case IntrinsicId.Array_Empty: {
					AddReturnValue (ArrayValue.Create (0, ((GenericInstanceMethod) calledMethod).GenericArguments[0]));
				}
				break;

			// System.Reflection.MethodBase.GetMethodFromHandle (RuntimeMethodHandle handle)
			// System.Reflection.MethodBase.GetMethodFromHandle (RuntimeMethodHandle handle, RuntimeTypeHandle declaringType)
			case IntrinsicId.MethodBase_GetMethodFromHandle: {
					// Infrastructure piece to support "ldtoken method -> GetMethodFromHandle"
					if (methodParams[0].AsSingleValue () is RuntimeMethodHandleValue methodHandle)
						AddReturnValue (new SystemReflectionMethodBaseValue (methodHandle.MethodRepresented));
				}
				break;

			//
			// System.Type
			//
			// Type MakeGenericType (params Type[] typeArguments)
			//
			case IntrinsicId.Type_MakeGenericType: {
					// Shared HandleCallAction doesn't cover all the same functionality as this does
					// We don't yet handle the case where you can create a nullable type with typeof(Nullable<>).MakeGenericType(T)
					foreach (var value in methodParams[0]) {
						if (value is SystemTypeValue typeValue) {
							if (!AnalyzeGenericInstantiationTypeArray (analysisContext, methodParams[1], calledMethodDefinition, typeValue.RepresentedType.Type.GenericParameters)) {
								bool hasUncheckedAnnotation = false;
								foreach (var genericParameter in typeValue.RepresentedType.Type.GenericParameters) {
									if (_context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (genericParameter) != DynamicallyAccessedMemberTypes.None ||
										(genericParameter.HasDefaultConstructorConstraint && !typeValue.RepresentedType.Type.IsTypeOf ("System", "Nullable`1"))) {
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
									analysisContext.ReportWarning (DiagnosticId.MakeGenericType, calledMethodDefinition.GetDisplayName ());
								}
							}

							// Nullables without a type argument are considered SystemTypeValues
							if (typeValue.RepresentedType.IsTypeOf ("System", "Nullable`1")) {
								foreach (var argumentValue in methodParams[1]) {
									if ((argumentValue as ArrayValue)?.TryGetValueByIndex (0, out var underlyingMultiValue) == true) {
										foreach (var underlyingValue in underlyingMultiValue) {
											switch (underlyingValue) {
											// Don't warn on these types - it will throw instead
											case NullableValueWithDynamicallyAccessedMembers:
											case NullableSystemTypeValue:
											case SystemTypeValue maybeArrayValue when maybeArrayValue.RepresentedType.IsTypeOf ("System", "Array"):
												AddReturnValue (MultiValueLattice.Top);
												break;
											case SystemTypeValue systemTypeValue:
												AddReturnValue (new NullableSystemTypeValue (typeValue.RepresentedType, new SystemTypeValue (systemTypeValue.RepresentedType)));
												break;
											// Generic Parameters and method parameters with annotations
											case ValueWithDynamicallyAccessedMembers damValue:
												AddReturnValue (new NullableValueWithDynamicallyAccessedMembers (typeValue.RepresentedType, damValue));
												break;
											// Everything else assume it has no annotations
											default:
												AddReturnValue (GetMethodReturnValue (calledMethodDefinition, returnValueDynamicallyAccessedMemberTypes));
												break;
											}
										}
									}
								}
								// We want to skip adding the `value` to the return Value because we have already added Nullable<value>
								continue;
							}
							// We haven't found any generic parameters with annotations, so there's nothing to validate.
						} else if (value == NullValue.Instance) {
							// Do nothing - null value is valid and should not cause warnings nor marking
						} else {
							// We have no way to "include more" to fix this if we don't know, so we have to warn
							analysisContext.ReportWarning (DiagnosticId.MakeGenericType, calledMethodDefinition.GetDisplayName ());
						}
						// We don't want to lose track of the type
						// in case this is e.g. Activator.CreateInstance(typeof(Foo<>).MakeGenericType(...));
						AddReturnValue (value);
					}
				}
				break;

			//
			// System.Linq.Expressions.Expression
			//
			// static Call (Type, String, Type[], Expression[])
			//
			case IntrinsicId.Expression_Call: {
					BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

					var targetValue = GetMethodParameterValue (
						calledMethodDefinition,
						0,
						GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (bindingFlags));

					bool hasTypeArguments = (methodParams[2].AsSingleValue () as ArrayValue)?.Size.AsConstInt () != 0;
					foreach (var value in methodParams[0]) {
						if (value is SystemTypeValue systemTypeValue) {
							foreach (var stringParam in methodParams[1]) {
								if (stringParam is KnownStringValue stringValue) {
									foreach (var method in systemTypeValue.RepresentedType.Type.GetMethodsOnTypeHierarchy (_context, m => m.Name == stringValue.Contents, bindingFlags)) {
										ValidateGenericMethodInstantiation (analysisContext, method, methodParams[2], calledMethodDefinition);
										MarkMethod (analysisContext, method);
									}
								} else {
									if (hasTypeArguments) {
										// We don't know what method the `MakeGenericMethod` was called on, so we have to assume
										// that the method may have requirements which we can't fullfil -> warn.
										analysisContext.ReportWarning (DiagnosticId.MakeGenericMethod, DiagnosticUtilities.GetMethodSignatureDisplayName (calledMethod));
									}

									RequireDynamicallyAccessedMembers (
										analysisContext,
										value,
										targetValue);
								}
							}
						} else {
							if (hasTypeArguments) {
								// We don't know what method the `MakeGenericMethod` was called on, so we have to assume
								// that the method may have requirements which we can't fullfil -> warn.
								analysisContext.ReportWarning (DiagnosticId.MakeGenericMethod, DiagnosticUtilities.GetMethodSignatureDisplayName (calledMethod));
							}

							RequireDynamicallyAccessedMembers (
								analysisContext,
								value,
								targetValue);
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
					var targetValue = GetMethodParameterValue (calledMethodDefinition, 0, DynamicallyAccessedMemberTypes.PublicParameterlessConstructor);
					foreach (var value in methodParams[0]) {
						if (value is SystemTypeValue systemTypeValue) {
							MarkConstructorsOnType (analysisContext, systemTypeValue.RepresentedType.Type, null, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						} else {
							RequireDynamicallyAccessedMembers (analysisContext, value, targetValue);
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
					foreach (var valueNode in methodParams[0]) {
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

						TypeDefinition? staticType = (valueNode as IValueWithStaticType)?.StaticType;
						if (staticType is null) {
							// We don't know anything about the type GetType was called on. Track this as a usual result of a method call without any annotations
							AddReturnValue (GetMethodReturnValue (calledMethodDefinition));
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
							AddReturnValue (new SystemTypeValue (staticType));
						} else {
							// Make sure the type is marked (this will mark it as used via reflection, which is sort of true)
							// This should already be true for most cases (method params, fields, ...), but just in case
							MarkType (analysisContext, staticType);

							var annotation = _markStep.DynamicallyAccessedMembersTypeHierarchy
								.ApplyDynamicallyAccessedMembersToTypeHierarchy (this, staticType);

							// Return a value which is "unknown type" with annotation. For now we'll use the return value node
							// for the method, which means we're loosing the information about which staticType this
							// started with. For now we don't need it, but we can add it later on.
							AddReturnValue (GetMethodReturnValue (calledMethodDefinition, annotation));
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
					var parameters = calledMethod.Parameters;
					if ((parameters.Count == 3 && parameters[2].ParameterType.MetadataType == MetadataType.Boolean && methodParams[2].AsConstInt () != 0) ||
						(parameters.Count == 5 && methodParams[4].AsConstInt () != 0)) {
						analysisContext.ReportWarning (DiagnosticId.CaseInsensitiveTypeGetTypeCallIsNotSupported, calledMethod.GetDisplayName ());
						break;
					}
					foreach (var typeNameValue in methodParams[0]) {
						if (typeNameValue is KnownStringValue knownStringValue) {
							if (!_context.TypeNameResolver.TryResolveTypeName (knownStringValue.Contents, callingMethodDefinition, out TypeReference? foundTypeRef, out AssemblyDefinition? typeAssembly, false)
								|| ResolveToTypeDefinition (foundTypeRef) is not TypeDefinition foundType) {
								// Intentionally ignore - it's not wrong for code to call Type.GetType on non-existing name, the code might expect null/exception back.
							} else {
								_markStep.MarkTypeVisibleToReflection (foundTypeRef, foundType, new DependencyInfo (DependencyKind.AccessedViaReflection, callingMethodDefinition));
								AddReturnValue (new SystemTypeValue (foundType));
								_context.MarkingHelpers.MarkMatchingExportedType (foundType, typeAssembly, new DependencyInfo (DependencyKind.AccessedViaReflection, foundType), analysisContext.Origin);
							}
						} else if (typeNameValue == NullValue.Instance) {
							// Nothing to do
						} else if (typeNameValue is ValueWithDynamicallyAccessedMembers valueWithDynamicallyAccessedMembers && valueWithDynamicallyAccessedMembers.DynamicallyAccessedMemberTypes != 0) {
							// Propagate the annotation from the type name to the return value. Annotation on a string value will be fullfilled whenever a value is assigned to the string with annotation.
							// So while we don't know which type it is, we can guarantee that it will fulfill the annotation.
							AddReturnValue (GetMethodReturnValue (calledMethodDefinition, valueWithDynamicallyAccessedMembers.DynamicallyAccessedMemberTypes));
						} else {
							analysisContext.ReportWarning (DiagnosticId.UnrecognizedTypeNameInTypeGetType, calledMethod.GetDisplayName ());
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
					var parameters = calledMethod.Parameters;
					BindingFlags? bindingFlags;
					if (parameters.Count > 1 && calledMethod.Parameters[0].ParameterType.Name == "BindingFlags")
						bindingFlags = GetBindingFlagsFromValue (methodParams[1]);
					else
						// Assume a default value for BindingFlags for methods that don't use BindingFlags as a parameter
						bindingFlags = BindingFlags.Public | BindingFlags.Instance;

					int? ctorParameterCount = parameters.Count switch {
						1 => (methodParams[1].AsSingleValue () as ArrayValue)?.Size.AsConstInt (),
						2 => (methodParams[2].AsSingleValue () as ArrayValue)?.Size.AsConstInt (),
						4 => (methodParams[3].AsSingleValue () as ArrayValue)?.Size.AsConstInt (),
						5 => (methodParams[4].AsSingleValue () as ArrayValue)?.Size.AsConstInt (),
						_ => null,
					};

					// Go over all types we've seen
					foreach (var value in methodParams[0]) {
						if (value is SystemTypeValue systemTypeValue && !BindingFlagsAreUnsupported (bindingFlags)) {
							if (HasBindingFlag (bindingFlags, BindingFlags.Public) && !HasBindingFlag (bindingFlags, BindingFlags.NonPublic)
								&& ctorParameterCount == 0) {
								MarkConstructorsOnType (analysisContext, systemTypeValue.RepresentedType.Type, m => m.IsPublic && m.Parameters.Count == 0, bindingFlags);
							} else {
								MarkConstructorsOnType (analysisContext, systemTypeValue.RepresentedType.Type, null, bindingFlags);
							}
						} else {
							// Otherwise fall back to the bitfield requirements
							var requiredMemberTypes = GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors (bindingFlags);
							// We can scope down the public constructors requirement if we know the number of parameters is 0
							if (requiredMemberTypes == DynamicallyAccessedMemberTypes.PublicConstructors && ctorParameterCount == 0)
								requiredMemberTypes = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;

							var targetValue = GetMethodParameterValue (calledMethodDefinition, 0, requiredMemberTypes);
							RequireDynamicallyAccessedMembers (analysisContext, value, targetValue);
						}
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

					int? ctorParameterCount = null;
					BindingFlags bindingFlags = BindingFlags.Instance;
					if (parameters.Count > 1) {
						if (parameters[1].ParameterType.MetadataType == MetadataType.Boolean) {
							// The overload that takes a "nonPublic" bool
							bool nonPublic = methodParams[1].AsConstInt () != 0;

							if (nonPublic)
								bindingFlags |= BindingFlags.NonPublic | BindingFlags.Public;
							else
								bindingFlags |= BindingFlags.Public;
							ctorParameterCount = 0;
						} else {
							// Overload that has the parameters as the second or fourth argument
							int argsParam = parameters.Count == 2 || parameters.Count == 3 ? 1 : 3;

							if (methodParams.Count > argsParam) {
								if (methodParams[argsParam].AsSingleValue () is ArrayValue arrayValue &&
									arrayValue.Size.AsConstInt () != null)
									ctorParameterCount = arrayValue.Size.AsConstInt ();
								else if (methodParams[argsParam].AsSingleValue () is NullValue)
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
					foreach (var value in methodParams[0]) {
						if (value is SystemTypeValue systemTypeValue) {
							// Special case known type values as we can do better by applying exact binding flags and parameter count.
							MarkConstructorsOnType (analysisContext, systemTypeValue.RepresentedType.Type,
								ctorParameterCount == null ? null : m => m.Parameters.Count == ctorParameterCount, bindingFlags);
						} else {
							// Otherwise fall back to the bitfield requirements
							var requiredMemberTypes = GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors (bindingFlags);

							// Special case the public parameterless constructor if we know that there are 0 args passed in
							if (ctorParameterCount == 0 && requiredMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicConstructors)) {
								requiredMemberTypes &= ~DynamicallyAccessedMemberTypes.PublicConstructors;
								requiredMemberTypes |= DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;
							}

							var targetValue = GetMethodParameterValue (calledMethodDefinition, 0, requiredMemberTypes);

							RequireDynamicallyAccessedMembers (analysisContext, value, targetValue);
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
				ProcessCreateInstanceByName (analysisContext, calledMethodDefinition, methodParams);
				break;

			//
			// System.Activator
			//
			// static CreateInstanceFrom (string assemblyFile, string typeName)
			// static CreateInstanceFrom (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
			// static CreateInstanceFrom (string assemblyFile, string typeName, object? []? activationAttributes)
			//
			case IntrinsicId.Activator_CreateInstanceFrom:
				ProcessCreateInstanceByName (analysisContext, calledMethodDefinition, methodParams);
				break;

			//
			// System.Activator
			//
			// static T CreateInstance<T> ()
			//
			// Note: If the when condition returns false it would be an overload which we don't recognize, so just fall through to the default case
			case IntrinsicId.Activator_CreateInstanceOfT when
				calledMethod is GenericInstanceMethod genericCalledMethod && genericCalledMethod.GenericArguments.Count == 1: {

					if (genericCalledMethod.GenericArguments[0] is GenericParameter genericParameter &&
						genericParameter.HasDefaultConstructorConstraint) {
						// This is safe, the linker would have marked the default .ctor already
						break;
					}

					var targetValue = new GenericParameterValue (calledMethodDefinition.GenericParameters[0], DynamicallyAccessedMemberTypes.PublicParameterlessConstructor);
					RequireDynamicallyAccessedMembers (
						analysisContext,
						GetTypeValueNodeFromGenericArgument (genericCalledMethod.GenericArguments[0]),
						targetValue);
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
				ProcessCreateInstanceByName (analysisContext, calledMethodDefinition, methodParams);
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
				analysisContext.ReportWarning (DiagnosticId.ParametersOfAssemblyCreateInstanceCannotBeAnalyzed, calledMethodDefinition.GetDisplayName ());
				break;

			//
			// System.Reflection.MethodInfo
			//
			// MakeGenericMethod (Type[] typeArguments)
			//
			case IntrinsicId.MethodInfo_MakeGenericMethod: {

					foreach (var methodValue in methodParams[0]) {
						if (methodValue is SystemReflectionMethodBaseValue methodBaseValue) {
							ValidateGenericMethodInstantiation (analysisContext, methodBaseValue.MethodRepresented.Method, methodParams[1], calledMethodDefinition);
						} else if (methodValue == NullValue.Instance) {
							// Nothing to do
						} else {
							// We don't know what method the `MakeGenericMethod` was called on, so we have to assume
							// that the method may have requirements which we can't fullfil -> warn.
							analysisContext.ReportWarning (DiagnosticId.MakeGenericMethod, DiagnosticUtilities.GetMethodSignatureDisplayName (calledMethodDefinition));
						}
					}

					// MakeGenericMethod doesn't change the identity of the MethodBase we're tracking so propagate to the return value
					AddReturnValue (methodParams[0]);
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
						analysisContext.ReportWarning (DiagnosticId.CorrectnessOfCOMCannotBeGuaranteed, calledMethodDefinition.GetDisplayName ());
					}
				}

				if (requiresDataFlowAnalysis) {
					for (int parameterIndex = 0; parameterIndex < methodParams.Count; parameterIndex++) {
						var targetValue = GetMethodParameterValue (calledMethodDefinition, parameterIndex);

						if (targetValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None) {
							RequireDynamicallyAccessedMembers (analysisContext, methodParams[parameterIndex], targetValue);
						}
					}
				}

				_markStep.CheckAndReportRequiresUnreferencedCode (calledMethodDefinition);

				// To get good reporting of errors we need to track the origin of the value for all method calls
				// but except Newobj as those are special.
				if (GetReturnTypeWithoutModifiers (calledMethodDefinition.ReturnType).MetadataType != MetadataType.Void) {
					methodReturnValue = GetMethodReturnValue (calledMethodDefinition, returnValueDynamicallyAccessedMemberTypes);

					return true;
				}

				return false;
			}

			// If we get here, we handled this as an intrinsic.  As a convenience, if the code above
			// didn't set the return value (and the method has a return value), we will set it to be an
			// unknown value with the return type of the method.
			bool returnsVoid = GetReturnTypeWithoutModifiers (calledMethod.ReturnType).MetadataType == MetadataType.Void;
			methodReturnValue = maybeMethodReturnValue ?? (returnsVoid ?
				MultiValueLattice.Top :
				GetMethodReturnValue (calledMethodDefinition, returnValueDynamicallyAccessedMemberTypes));

			// Validate that the return value has the correct annotations as per the method return value annotations
			if (returnValueDynamicallyAccessedMemberTypes != 0) {
				foreach (var uniqueValue in methodReturnValue) {
					if (uniqueValue is ValueWithDynamicallyAccessedMembers methodReturnValueWithMemberTypes) {
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

			void AddReturnValue (MultiValue value)
			{
				maybeMethodReturnValue = (maybeMethodReturnValue is null) ? value : MultiValueLattice.Meet ((MultiValue) maybeMethodReturnValue, value);
			}
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
					if (parameterTypeDef.IsTypeOf (WellKnownType.System_Array)) {
						// System.Array marshals as IUnknown by default
						return true;
					} else if (parameterTypeDef.IsTypeOf (WellKnownType.System_String) ||
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

		bool AnalyzeGenericInstantiationTypeArray (in AnalysisContext analysisContext, in MultiValue arrayParam, MethodDefinition calledMethod, IList<GenericParameter> genericParameters)
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

			foreach (var typesValue in arrayParam) {
				if (typesValue is not ArrayValue array) {
					return false;
				}

				int? size = array.Size.AsConstInt ();
				if (size == null || size != genericParameters.Count) {
					return false;
				}

				bool allIndicesKnown = true;
				for (int i = 0; i < size.Value; i++) {
					if (!array.TryGetValueByIndex (i, out MultiValue value) || value.AsSingleValue () is UnknownValue) {
						allIndicesKnown = false;
						break;
					}
				}

				if (!allIndicesKnown) {
					return false;
				}

				for (int i = 0; i < size.Value; i++) {
					if (array.TryGetValueByIndex (i, out MultiValue value)) {
						// https://github.com/dotnet/linker/issues/2428
						// We need to report the target as "this" - as that was the previous behavior
						// but with the annotation from the generic parameter.
						var targetValue = GetMethodParameterValue (calledMethod, 0, _context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (genericParameters[i]));
						RequireDynamicallyAccessedMembers (
							analysisContext,
							value,
							targetValue);
					}
				}
			}
			return true;
		}

		void ProcessCreateInstanceByName (in AnalysisContext analysisContext, MethodDefinition calledMethod, ValueNodeList methodParams)
		{
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

			foreach (var assemblyNameValue in methodParams[methodParamsOffset]) {
				if (assemblyNameValue is KnownStringValue assemblyNameStringValue) {
					if (assemblyNameStringValue.Contents is string assemblyName && assemblyName.Length == 0) {
						// Throws exception for zero-length assembly name.
						continue;
					}
					foreach (var typeNameValue in methodParams[methodParamsOffset + 1]) {
						if (typeNameValue is NullValue) {
							// Throws exception for null type name.
							continue;
						}
						if (typeNameValue is KnownStringValue typeNameStringValue) {
							var resolvedAssembly = _context.TryResolve (assemblyNameStringValue.Contents);
							if (resolvedAssembly == null) {
								analysisContext.ReportWarning (DiagnosticId.UnresolvedAssemblyInCreateInstance,
									assemblyNameStringValue.Contents,
									calledMethod.GetDisplayName ());
								continue;
							}

							if (!_context.TypeNameResolver.TryResolveTypeName (resolvedAssembly, typeNameStringValue.Contents, out TypeReference? typeRef)
								|| _context.TryResolve (typeRef) is not TypeDefinition resolvedType
								|| typeRef is ArrayType) {
								// It's not wrong to have a reference to non-existing type - the code may well expect to get an exception in this case
								// Note that we did find the assembly, so it's not a linker config problem, it's either intentional, or wrong versions of assemblies
								// but linker can't know that. In case a user tries to create an array using System.Activator we should simply ignore it, the user
								// might expect an exception to be thrown.
								continue;
							}

							MarkConstructorsOnType (analysisContext, resolvedType, parameterlessConstructor ? m => m.Parameters.Count == 0 : null, bindingFlags);
						} else {
							analysisContext.ReportWarning (DiagnosticId.UnrecognizedParameterInMethodCreateInstance, calledMethod.Parameters[1].Name, calledMethod.GetDisplayName ());
						}
					}
				} else {
					analysisContext.ReportWarning (DiagnosticId.UnrecognizedParameterInMethodCreateInstance, calledMethod.Parameters[0].Name, calledMethod.GetDisplayName ());
				}
			}
		}

		void RequireDynamicallyAccessedMembers (in AnalysisContext analysisContext, in MultiValue value, ValueWithDynamicallyAccessedMembers targetValue)
		{
			var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction (_context, this, analysisContext);
			requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);
		}

		static BindingFlags? GetBindingFlagsFromValue (in MultiValue parameter) => HandleCallAction.GetBindingFlagsFromValue (parameter);

		static bool BindingFlagsAreUnsupported (BindingFlags? bindingFlags) => HandleCallAction.BindingFlagsAreUnsupported (bindingFlags);

		static bool HasBindingFlag (BindingFlags? bindingFlags, BindingFlags? search) => HandleCallAction.HasBindingFlag (bindingFlags, search);

		internal void MarkTypeForDynamicallyAccessedMembers (in AnalysisContext analysisContext, TypeDefinition typeDefinition, DynamicallyAccessedMemberTypes requiredMemberTypes, DependencyKind dependencyKind, bool declaredOnly = false)
		{
			foreach (var member in typeDefinition.GetDynamicallyAccessedMembers (_context, requiredMemberTypes, declaredOnly)) {
				switch (member) {
				case MethodDefinition method:
					MarkMethod (analysisContext, method, dependencyKind);
					break;
				case FieldDefinition field:
					MarkField (analysisContext, field, dependencyKind);
					break;
				case TypeDefinition nestedType:
					MarkType (analysisContext, nestedType, dependencyKind);
					break;
				case PropertyDefinition property:
					MarkProperty (analysisContext, property, dependencyKind);
					break;
				case EventDefinition @event:
					MarkEvent (analysisContext, @event, dependencyKind);
					break;
				case InterfaceImplementation interfaceImplementation:
					MarkInterfaceImplementation (analysisContext, interfaceImplementation, dependencyKind);
					break;
				}
			}
		}

		internal void MarkType (in AnalysisContext analysisContext, TypeReference typeReference, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			if (_context.TryResolve (typeReference) is TypeDefinition type)
				_markStep.MarkTypeVisibleToReflection (typeReference, type, new DependencyInfo (dependencyKind, analysisContext.Origin.Provider));
		}

		internal void MarkMethod (in AnalysisContext analysisContext, MethodDefinition method, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			_markStep.MarkMethodVisibleToReflection (method, new DependencyInfo (dependencyKind, analysisContext.Origin.Provider));
		}

		void MarkField (in AnalysisContext analysisContext, FieldDefinition field, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			_markStep.MarkFieldVisibleToReflection (field, new DependencyInfo (dependencyKind, analysisContext.Origin.Provider));
		}

		internal void MarkProperty (in AnalysisContext analysisContext, PropertyDefinition property, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			_markStep.MarkPropertyVisibleToReflection (property, new DependencyInfo (dependencyKind, analysisContext.Origin.Provider));
		}

		void MarkEvent (in AnalysisContext analysisContext, EventDefinition @event, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			_markStep.MarkEventVisibleToReflection (@event, new DependencyInfo (dependencyKind, analysisContext.Origin.Provider));
		}

		void MarkInterfaceImplementation (in AnalysisContext analysisContext, InterfaceImplementation interfaceImplementation, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			_markStep.MarkInterfaceImplementation (interfaceImplementation, null, new DependencyInfo (dependencyKind, analysisContext.Origin.Provider));
		}

		void MarkConstructorsOnType (in AnalysisContext analysisContext, TypeDefinition type, Func<MethodDefinition, bool>? filter, BindingFlags? bindingFlags = null)
		{
			foreach (var ctor in type.GetConstructorsOnType (filter, bindingFlags))
				MarkMethod (analysisContext, ctor);
		}

		internal void MarkFieldsOnTypeHierarchy (in AnalysisContext analysisContext, TypeDefinition type, Func<FieldDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			foreach (var field in type.GetFieldsOnTypeHierarchy (_context, filter, bindingFlags))
				MarkField (analysisContext, field);
		}

		internal void MarkPropertiesOnTypeHierarchy (in AnalysisContext analysisContext, TypeDefinition type, Func<PropertyDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			foreach (var property in type.GetPropertiesOnTypeHierarchy (_context, filter, bindingFlags))
				MarkProperty (analysisContext, property);
		}

		internal void MarkEventsOnTypeHierarchy (in AnalysisContext analysisContext, TypeDefinition type, Func<EventDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			foreach (var @event in type.GetEventsOnTypeHierarchy (_context, filter, bindingFlags))
				MarkEvent (analysisContext, @event);
		}

		internal void MarkStaticConstructor (in AnalysisContext analysisContext, TypeDefinition type)
		{
			_markStep.MarkStaticConstructorVisibleToReflection (type, new DependencyInfo (DependencyKind.AccessedViaReflection, analysisContext.Origin.Provider));
		}

		void ValidateGenericMethodInstantiation (
			in AnalysisContext analysisContext,
			MethodDefinition genericMethod,
			in MultiValue genericParametersArray,
			MethodDefinition reflectionMethod)
		{
			if (!genericMethod.HasGenericParameters) {
				return;
			}

			if (!AnalyzeGenericInstantiationTypeArray (analysisContext, genericParametersArray, reflectionMethod, genericMethod.GenericParameters)) {
				analysisContext.ReportWarning (DiagnosticId.MakeGenericMethod, DiagnosticUtilities.GetMethodSignatureDisplayName (reflectionMethod));
			}
		}

		static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors (BindingFlags? bindingFlags) =>
			HandleCallAction.GetDynamicallyAccessedMemberTypesFromBindingFlagsForConstructors (bindingFlags);

		static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (BindingFlags? bindingFlags) =>
			HandleCallAction.GetDynamicallyAccessedMemberTypesFromBindingFlagsForMethods (bindingFlags);

		internal readonly struct AnalysisContext
		{
			public readonly MessageOrigin Origin;
			public readonly bool DiagnosticsEnabled;
			readonly LinkContext _context;

			public AnalysisContext (in MessageOrigin origin, bool diagnosticsEnabled, LinkContext context)
				=> (Origin, DiagnosticsEnabled, _context) = (origin, diagnosticsEnabled, context);

			public void ReportWarning (string message, int messageCode)
			{
				if (DiagnosticsEnabled)
					_context.LogWarning (message, messageCode, Origin, MessageSubCategory.TrimAnalysis);
			}

			public void ReportWarning (DiagnosticId id, params string[] args)
			{
				if (DiagnosticsEnabled)
					_context.LogWarning (Origin, id, args);
			}
		}
	}
}
