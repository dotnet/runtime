// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker;
using Mono.Linker.Dataflow;

namespace ILLink.Shared.TrimAnalysis
{
	internal sealed partial class FlowAnnotations
	{
		readonly LinkContext _context;
		readonly Dictionary<TypeDefinition, TypeAnnotations> _annotations = new Dictionary<TypeDefinition, TypeAnnotations> ();
		readonly TypeHierarchyCache _hierarchyInfo;

		public FlowAnnotations (LinkContext context)
		{
			_context = context;
			_hierarchyInfo = new TypeHierarchyCache (context);
		}

		public bool RequiresDataFlowAnalysis (MethodDefinition method) =>
			GetAnnotations (method.DeclaringType).TryGetAnnotation (method, out var methodAnnotations)
				&& (methodAnnotations.ReturnParameterAnnotation != DynamicallyAccessedMemberTypes.None || methodAnnotations.ParameterAnnotations != null);

		public bool RequiresVirtualMethodDataFlowAnalysis (MethodDefinition method) =>
			GetAnnotations (method.DeclaringType).TryGetAnnotation (method, out _);

		public bool RequiresDataFlowAnalysis (FieldDefinition field) =>
			GetAnnotations (field.DeclaringType).TryGetAnnotation (field, out _);

		public bool RequiresGenericArgumentDataFlowAnalysis (GenericParameter genericParameter) =>
			GetGenericParameterAnnotation (genericParameter) != DynamicallyAccessedMemberTypes.None;

		internal DynamicallyAccessedMemberTypes GetParameterAnnotation (ParameterProxy param)
		{
			if (GetAnnotations (param.Method.Method.DeclaringType).TryGetAnnotation (param.Method.Method, out var annotation) &&
				annotation.ParameterAnnotations != null)
				return annotation.ParameterAnnotations[(int) param.Index];

			return DynamicallyAccessedMemberTypes.None;
		}

		public DynamicallyAccessedMemberTypes GetReturnParameterAnnotation (MethodDefinition method)
		{
			if (GetAnnotations (method.DeclaringType).TryGetAnnotation (method, out var annotation))
				return annotation.ReturnParameterAnnotation;

			return DynamicallyAccessedMemberTypes.None;
		}

		public DynamicallyAccessedMemberTypes GetFieldAnnotation (FieldDefinition field)
		{
			if (GetAnnotations (field.DeclaringType).TryGetAnnotation (field, out var annotation))
				return annotation.Annotation;

			return DynamicallyAccessedMemberTypes.None;
		}

		public DynamicallyAccessedMemberTypes GetTypeAnnotation (TypeDefinition type) =>
			GetAnnotations (type).TypeAnnotation;

		public bool ShouldWarnWhenAccessedForReflection (IMemberDefinition provider) =>
			provider switch {
				MethodDefinition method => ShouldWarnWhenAccessedForReflection (method),
				FieldDefinition field => ShouldWarnWhenAccessedForReflection (field),
				_ => false
			};

		public DynamicallyAccessedMemberTypes GetGenericParameterAnnotation (GenericParameter genericParameter)
		{
			TypeDefinition? declaringType = _context.Resolve (genericParameter.DeclaringType);
			if (declaringType != null) {
				if (GetAnnotations (declaringType).TryGetAnnotation (genericParameter, out var annotation))
					return annotation;

				return DynamicallyAccessedMemberTypes.None;
			}

			MethodDefinition? declaringMethod = _context.Resolve (genericParameter.DeclaringMethod);
			if (declaringMethod != null && GetAnnotations (declaringMethod.DeclaringType).TryGetAnnotation (declaringMethod, out var methodTypeAnnotations) &&
				methodTypeAnnotations.TryGetAnnotation (genericParameter, out var methodAnnotation))
				return methodAnnotation;

			return DynamicallyAccessedMemberTypes.None;
		}

		public bool ShouldWarnWhenAccessedForReflection (MethodDefinition method)
		{
			if (!GetAnnotations (method.DeclaringType).TryGetAnnotation (method, out var annotation))
				return false;

			if (annotation.ParameterAnnotations == null && annotation.ReturnParameterAnnotation == DynamicallyAccessedMemberTypes.None)
				return false;

			// If the method only has annotation on the return value and it's not virtual avoid warning.
			// Return value annotations are "consumed" by the caller of a method, and as such there is nothing
			// wrong calling these dynamically. The only problem can happen if something overrides a virtual
			// method with annotated return value at runtime - in this case the trimmer can't validate
			// that the method will return only types which fulfill the annotation's requirements.
			// For example:
			//   class BaseWithAnnotation
			//   {
			//       [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
			//       public abstract Type GetTypeWithFields();
			//   }
			//
			//   class UsingTheBase
			//   {
			//       public void PrintFields(Base base)
			//       {
			//            // No warning here - GetTypeWithFields is correctly annotated to allow GetFields on the return value.
			//            Console.WriteLine(string.Join(" ", base.GetTypeWithFields().GetFields().Select(f => f.Name)));
			//       }
			//   }
			//
			// If at runtime (through ref emit) something generates code like this:
			//   class DerivedAtRuntimeFromBase
			//   {
			//       // No point in adding annotation on the return value - nothing will look at it anyway
			//       // Trimming will not see this code, so there are no checks
			//       public override Type GetTypeWithFields() { return typeof(TestType); }
			//   }
			//
			// If TestType from above is trimmed, it may note have all its fields, and there would be no warnings generated.
			// But there has to be code like this somewhere in the app, in order to generate the override:
			//   class RuntimeTypeGenerator
			//   {
			//       public MethodInfo GetBaseMethod()
			//       {
			//            // This must warn - that the GetTypeWithFields has annotation on the return value
			//            return typeof(BaseWithAnnotation).GetMethod("GetTypeWithFields");
			//       }
			//   }
			return method.IsVirtual || annotation.ParameterAnnotations != null;
		}

		public bool ShouldWarnWhenAccessedForReflection (FieldDefinition field) =>
			GetAnnotations (field.DeclaringType).TryGetAnnotation (field, out _);

		public bool IsTypeInterestingForDataflow (TypeReference typeReference)
		{
			if (typeReference.MetadataType == MetadataType.String)
				return true;

			// ByRef over an interesting type is itself interesting
			if (typeReference.IsByReference)
				typeReference = ((ByReferenceType) typeReference).ElementType;

			if (!typeReference.IsNamedType ())
				return false;

			TypeDefinition? type = typeReference.ResolveToTypeDefinition (_context);
			return type != null && (
				_hierarchyInfo.IsSystemType (type) ||
				_hierarchyInfo.IsSystemReflectionIReflect (type));
		}

		TypeAnnotations GetAnnotations (TypeDefinition type)
		{
			if (!_annotations.TryGetValue (type, out TypeAnnotations value)) {
				value = BuildTypeAnnotations (type);
				_annotations.Add (type, value);
			}

			return value;
		}

		static bool IsDynamicallyAccessedMembersAttribute (CustomAttribute attribute)
		{
			var attributeType = attribute.AttributeType;
			return attributeType.Name == "DynamicallyAccessedMembersAttribute" && attributeType.Namespace == "System.Diagnostics.CodeAnalysis";
		}

		DynamicallyAccessedMemberTypes GetMemberTypesForDynamicallyAccessedMembersAttribute (IMemberDefinition member, ICustomAttributeProvider? providerIfNotMember = null)
		{
			ICustomAttributeProvider provider = providerIfNotMember ?? member;
			if (!_context.CustomAttributes.HasAny (provider))
				return DynamicallyAccessedMemberTypes.None;
			foreach (var attribute in _context.CustomAttributes.GetCustomAttributes (provider)) {
				if (!IsDynamicallyAccessedMembersAttribute (attribute))
					continue;
				if (attribute.ConstructorArguments.Count == 1)
					return (DynamicallyAccessedMemberTypes) (int) attribute.ConstructorArguments[0].Value;
				else
					_context.LogWarning (member, DiagnosticId.AttributeDoesntHaveTheRequiredNumberOfParameters, "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute");
			}
			return DynamicallyAccessedMemberTypes.None;
		}

		TypeAnnotations BuildTypeAnnotations (TypeDefinition type)
		{
			// class, interface, struct can have annotations
			DynamicallyAccessedMemberTypes typeAnnotation = GetMemberTypesForDynamicallyAccessedMembersAttribute (type);

			ArrayBuilder<FieldAnnotation> annotatedFields = default;

			// First go over all fields with an explicit annotation
			if (type.HasFields) {
				foreach (FieldDefinition field in type.Fields) {
					DynamicallyAccessedMemberTypes annotation = GetMemberTypesForDynamicallyAccessedMembersAttribute (field);
					if (annotation == DynamicallyAccessedMemberTypes.None) {
						continue;
					}

					if (!IsTypeInterestingForDataflow (field.FieldType)) {
						// Already know that there's a non-empty annotation on a field which is not System.Type/String and we're about to ignore it
						_context.LogWarning (field, DiagnosticId.DynamicallyAccessedMembersOnFieldCanOnlyApplyToTypesOrStrings, field.GetDisplayName ());
						continue;
					}

					annotatedFields.Add (new FieldAnnotation (field, annotation));
				}
			}

			var annotatedMethods = new List<MethodAnnotations> ();

			// Next go over all methods with an explicit annotation
			if (type.HasMethods) {
				foreach (MethodDefinition method in type.Methods) {
					DynamicallyAccessedMemberTypes[]? paramAnnotations = null;

					// Warn if there is an annotation on a method without a `this` parameter -- we won't catch it in the for loop if there's no parameters
					if (GetMemberTypesForDynamicallyAccessedMembersAttribute (method) != DynamicallyAccessedMemberTypes.None
						&& !method.HasImplicitThis ()) {
						_context.LogWarning (method, DiagnosticId.DynamicallyAccessedMembersIsNotAllowedOnMethods);
					}

					foreach (var param in method.GetParameters ()) {
						DynamicallyAccessedMemberTypes pa = GetMemberTypesForDynamicallyAccessedMembersAttribute (method, param.GetCustomAttributeProvider ());
						if (pa == DynamicallyAccessedMemberTypes.None)
							continue;

						if (!IsTypeInterestingForDataflow (param.ParameterType)) {
							if (param.IsImplicitThis)
								_context.LogWarning (method, DiagnosticId.DynamicallyAccessedMembersIsNotAllowedOnMethods);
							else
								_context.LogWarning (method, DiagnosticId.DynamicallyAccessedMembersOnMethodParameterCanOnlyApplyToTypesOrStrings,
									param.GetDisplayName (), DiagnosticUtilities.GetMethodSignatureDisplayName (method));
							continue;
						}
						paramAnnotations ??= new DynamicallyAccessedMemberTypes[method.GetParametersCount ()];
						paramAnnotations[(int) param.Index] = pa;
					}

					DynamicallyAccessedMemberTypes returnAnnotation = GetMemberTypesForDynamicallyAccessedMembersAttribute (method, providerIfNotMember: method.MethodReturnType);
					if (returnAnnotation != DynamicallyAccessedMemberTypes.None && !IsTypeInterestingForDataflow (method.ReturnType)) {
						_context.LogWarning (method, DiagnosticId.DynamicallyAccessedMembersOnMethodReturnValueCanOnlyApplyToTypesOrStrings, method.GetDisplayName ());
					}

					DynamicallyAccessedMemberTypes[]? genericParameterAnnotations = null;
					if (method.HasGenericParameters) {
						for (int genericParameterIndex = 0; genericParameterIndex < method.GenericParameters.Count; genericParameterIndex++) {
							var genericParameter = method.GenericParameters[genericParameterIndex];
							var annotation = GetMemberTypesForDynamicallyAccessedMembersAttribute (method, providerIfNotMember: genericParameter);
							if (annotation != DynamicallyAccessedMemberTypes.None) {
								genericParameterAnnotations ??= new DynamicallyAccessedMemberTypes[method.GenericParameters.Count];
								genericParameterAnnotations[genericParameterIndex] = annotation;
							}
						}
					}

					if (returnAnnotation != DynamicallyAccessedMemberTypes.None || paramAnnotations != null || genericParameterAnnotations != null) {
						annotatedMethods.Add (new MethodAnnotations (method, paramAnnotations, returnAnnotation, genericParameterAnnotations));
					}
				}
			}

			// Next up are properties. Annotations on properties are kind of meta because we need to
			// map them to annotations on methods/fields. They're syntactic sugar - what they do is expressible
			// by placing attribute on the accessor/backing field. For complex properties, that's what people
			// will need to do anyway. Like so:
			//
			// [field: Attribute]
			// Type MyProperty {
			//     [return: Attribute]
			//     get;
			//     [value: Attribute]
			//     set;
			//  }
			//

			if (type.HasProperties) {
				foreach (PropertyDefinition property in type.Properties) {
					DynamicallyAccessedMemberTypes annotation = GetMemberTypesForDynamicallyAccessedMembersAttribute (property);
					if (annotation == DynamicallyAccessedMemberTypes.None)
						continue;

					if (!IsTypeInterestingForDataflow (property.PropertyType)) {
						_context.LogWarning (property, DiagnosticId.DynamicallyAccessedMembersOnPropertyCanOnlyApplyToTypesOrStrings, property.GetDisplayName ());
						continue;
					}

					FieldDefinition? backingFieldFromSetter = null;

					// Propagate the annotation to the setter method
					MethodDefinition setMethod = property.SetMethod;
					if (setMethod != null) {

						// Abstract property backing field propagation doesn't make sense, and any derived property will be validated
						// to have the exact same annotations on getter/setter, and thus if it has a detectable backing field that will be validated as well.
						if (setMethod.HasBody) {
							// Look for the compiler generated backing field. If it doesn't work out simply move on. In such case we would still
							// propagate the annotation to the setter/getter and later on when analyzing the setter/getter we will warn
							// that the field (which ever it is) must be annotated as well.
							ScanMethodBodyForFieldAccess (setMethod.Body, write: true, out backingFieldFromSetter);
						}

						MethodAnnotations? setterAnnotation = null;
						foreach (var annotatedMethod in annotatedMethods) {
							if (annotatedMethod.Method == setMethod)
								setterAnnotation = annotatedMethod;
						}

						// If 'value' parameter is annotated, then warn. Other parameters can be annotated for indexable properties
						if (setterAnnotation?.ParameterAnnotations?[^1] is not (null or DynamicallyAccessedMemberTypes.None)) {
							_context.LogWarning (setMethod, DiagnosticId.DynamicallyAccessedMembersConflictsBetweenPropertyAndAccessor, property.GetDisplayName (), setMethod.GetDisplayName ());
						} else {
							if (setterAnnotation is not null)
								annotatedMethods.Remove (setterAnnotation.Value);

							DynamicallyAccessedMemberTypes[] paramAnnotations;
							if (setterAnnotation?.ParameterAnnotations is null)
								paramAnnotations = new DynamicallyAccessedMemberTypes[setMethod.GetParametersCount ()];
							else
								paramAnnotations = setterAnnotation.Value.ParameterAnnotations;

							paramAnnotations[paramAnnotations.Length - 1] = annotation;
							annotatedMethods.Add (new MethodAnnotations (setMethod, paramAnnotations, DynamicallyAccessedMemberTypes.None, null));
						}
					}

					FieldDefinition? backingFieldFromGetter = null;

					// Propagate the annotation to the getter method
					MethodDefinition getMethod = property.GetMethod;
					if (getMethod != null) {

						// Abstract property backing field propagation doesn't make sense, and any derived property will be validated
						// to have the exact same annotations on getter/setter, and thus if it has a detectable backing field that will be validated as well.
						if (getMethod.HasBody) {
							// Look for the compiler generated backing field. If it doesn't work out simply move on. In such case we would still
							// propagate the annotation to the setter/getter and later on when analyzing the setter/getter we will warn
							// that the field (which ever it is) must be annotated as well.
							ScanMethodBodyForFieldAccess (getMethod.Body, write: false, out backingFieldFromGetter);
						}
						MethodAnnotations? getterAnnotation = null;
						foreach (var annotatedMethod in annotatedMethods) {
							if (annotatedMethod.Method == getMethod)
								getterAnnotation = annotatedMethod;
						}

						// If return value is annotated, then warn. Otherwise, parameters can be annotated for indexable properties
						if (getterAnnotation?.ReturnParameterAnnotation is not (null or DynamicallyAccessedMemberTypes.None)) {
							_context.LogWarning (getMethod, DiagnosticId.DynamicallyAccessedMembersConflictsBetweenPropertyAndAccessor, property.GetDisplayName (), getMethod.GetDisplayName ());
						} else {
							if (getterAnnotation is not null)
								annotatedMethods.Remove (getterAnnotation.Value);

							annotatedMethods.Add (new MethodAnnotations (getMethod, getterAnnotation?.ParameterAnnotations, annotation, null));
						}
					}

					FieldDefinition? backingField;
					if (backingFieldFromGetter != null && backingFieldFromSetter != null &&
						backingFieldFromGetter != backingFieldFromSetter) {
						_context.LogWarning (property, DiagnosticId.DynamicallyAccessedMembersCouldNotFindBackingField, property.GetDisplayName ());
						backingField = null;
					} else {
						backingField = backingFieldFromGetter ?? backingFieldFromSetter;
					}

					if (backingField != null) {
						if (annotatedFields.Any (a => a.Field == backingField)) {
							_context.LogWarning (backingField, DiagnosticId.DynamicallyAccessedMembersOnPropertyConflictsWithBackingField, property.GetDisplayName (), backingField.GetDisplayName ());
						} else {
							annotatedFields.Add (new FieldAnnotation (backingField, annotation));
						}
					}
				}
			}

			DynamicallyAccessedMemberTypes[]? typeGenericParameterAnnotations = null;
			if (type.HasGenericParameters) {
				var attrs = GetGeneratedTypeAttributes (type);
				for (int genericParameterIndex = 0; genericParameterIndex < type.GenericParameters.Count; genericParameterIndex++) {
					var provider = attrs?[genericParameterIndex] ?? type.GenericParameters[genericParameterIndex];
					var annotation = GetMemberTypesForDynamicallyAccessedMembersAttribute (type, providerIfNotMember: provider);
					if (annotation != DynamicallyAccessedMemberTypes.None) {
						typeGenericParameterAnnotations ??= new DynamicallyAccessedMemberTypes[type.GenericParameters.Count];
						typeGenericParameterAnnotations[genericParameterIndex] = annotation;
					}
				}
			}

			return new TypeAnnotations (type, typeAnnotation, annotatedMethods.ToArray (), annotatedFields.ToArray (), typeGenericParameterAnnotations);
		}

		private IReadOnlyList<ICustomAttributeProvider>? GetGeneratedTypeAttributes (TypeDefinition typeDef)
		{
			if (!CompilerGeneratedNames.IsStateMachineOrDisplayClass (typeDef.Name)) {
				return null;
			}
			var attrs = _context.CompilerGeneratedState.GetGeneratedTypeAttributes (typeDef);
			Debug.Assert (attrs is null || attrs.Count == typeDef.GenericParameters.Count);
			return attrs;
		}

		bool ScanMethodBodyForFieldAccess (MethodBody body, bool write, out FieldDefinition? found)
		{
			// Tries to find the backing field for a property getter/setter.
			// Returns true if this is a method body that we can unambiguously analyze.
			// The found field could still be null if there's no backing store.

			FieldReference? foundReference = null;

			foreach (Instruction instruction in _context.GetMethodIL (body).Instructions) {
				switch (instruction.OpCode.Code) {
				case Code.Ldsfld when !write:
				case Code.Ldfld when !write:
				case Code.Stsfld when write:
				case Code.Stfld when write:

					if (foundReference != null) {
						// This writes/reads multiple fields - can't guess which one is the backing store.
						// Return failure.
						found = null;
						return false;
					}

					foundReference = (FieldReference) instruction.Operand;
					break;
				}
			}

			if (foundReference == null) {
				// Doesn't access any fields. Could be e.g. "Type Foo => typeof(Bar);"
				// Return success.
				found = null;
				return true;
			}

			found = _context.Resolve (foundReference);

			if (found == null) {
				// If the field doesn't resolve, it can't be a field on the current type
				// anyway. Return failure.
				return false;
			}

			if (found.DeclaringType != body.Method.DeclaringType ||
				found.IsStatic != body.Method.IsStatic ||
				!found.IsCompilerGenerated ()) {
				// A couple heuristics to make sure we got the right field.
				// Return failure.
				found = null;
				return false;
			}

			return true;
		}

		internal void ValidateMethodAnnotationsAreSame (OverrideInformation ov)
		{
			var method = ov.Override;
			var baseMethod = ov.Base;
			GetAnnotations (method.DeclaringType).TryGetAnnotation (method, out var methodAnnotations);
			GetAnnotations (baseMethod.DeclaringType).TryGetAnnotation (baseMethod, out var baseMethodAnnotations);

			if (methodAnnotations.ReturnParameterAnnotation != baseMethodAnnotations.ReturnParameterAnnotation)
				LogValidationWarning (method.MethodReturnType, baseMethod.MethodReturnType, ov);

			if (methodAnnotations.ParameterAnnotations != null || baseMethodAnnotations.ParameterAnnotations != null) {
				if (methodAnnotations.ParameterAnnotations == null)
					ValidateMethodParametersHaveNoAnnotations (baseMethodAnnotations.ParameterAnnotations!, ov);
				else if (baseMethodAnnotations.ParameterAnnotations == null)
					ValidateMethodParametersHaveNoAnnotations (methodAnnotations.ParameterAnnotations, ov);
				else {
					if (methodAnnotations.ParameterAnnotations.Length != baseMethodAnnotations.ParameterAnnotations.Length)
						return;

					for (int parameterIndex = 0; parameterIndex < methodAnnotations.ParameterAnnotations.Length; parameterIndex++) {
						if (methodAnnotations.ParameterAnnotations[parameterIndex] != baseMethodAnnotations.ParameterAnnotations[parameterIndex])
							LogValidationWarning (
								method.TryGetParameter ((ParameterIndex) parameterIndex)?.GetCustomAttributeProvider ()!,
								baseMethod.TryGetParameter ((ParameterIndex) parameterIndex)?.GetCustomAttributeProvider ()!,
								ov);
					}
				}
			}

			if (methodAnnotations.GenericParameterAnnotations != null || baseMethodAnnotations.GenericParameterAnnotations != null) {
				if (methodAnnotations.GenericParameterAnnotations == null)
					ValidateMethodGenericParametersHaveNoAnnotations (baseMethodAnnotations.GenericParameterAnnotations!, ov);
				else if (baseMethodAnnotations.GenericParameterAnnotations == null)
					ValidateMethodGenericParametersHaveNoAnnotations (methodAnnotations.GenericParameterAnnotations, ov);
				else {
					if (methodAnnotations.GenericParameterAnnotations.Length != baseMethodAnnotations.GenericParameterAnnotations.Length)
						return;

					for (int genericParameterIndex = 0; genericParameterIndex < methodAnnotations.GenericParameterAnnotations.Length; genericParameterIndex++) {
						if (methodAnnotations.GenericParameterAnnotations[genericParameterIndex] != baseMethodAnnotations.GenericParameterAnnotations[genericParameterIndex]) {
							LogValidationWarning (
								method.GenericParameters[genericParameterIndex],
								baseMethod.GenericParameters[genericParameterIndex],
								ov);
						}
					}
				}
			}
		}

		void ValidateMethodParametersHaveNoAnnotations (DynamicallyAccessedMemberTypes[] parameterAnnotations, OverrideInformation ov)
		{
			for (int parameterIndex = 0; parameterIndex < parameterAnnotations.Length; parameterIndex++) {
				var annotation = parameterAnnotations[parameterIndex];
				if (annotation != DynamicallyAccessedMemberTypes.None)
					LogValidationWarning (
						ov.Override.GetParameter ((ParameterIndex) parameterIndex).GetCustomAttributeProvider ()!,
						ov.Base.GetParameter ((ParameterIndex) parameterIndex).GetCustomAttributeProvider ()!,
						ov);
			}
		}

		void ValidateMethodGenericParametersHaveNoAnnotations (DynamicallyAccessedMemberTypes[] genericParameterAnnotations, OverrideInformation ov)
		{
			for (int genericParameterIndex = 0; genericParameterIndex < genericParameterAnnotations.Length; genericParameterIndex++) {
				if (genericParameterAnnotations[genericParameterIndex] != DynamicallyAccessedMemberTypes.None) {
					LogValidationWarning (
						ov.Override.GenericParameters[genericParameterIndex],
						ov.Base.GenericParameters[genericParameterIndex],
						ov);
				}
			}
		}

		void LogValidationWarning (IMetadataTokenProvider provider, IMetadataTokenProvider baseProvider, OverrideInformation ov)
		{
			IMemberDefinition origin = (ov.IsOverrideOfInterfaceMember && ov.InterfaceImplementor.Implementor != ov.Override.DeclaringType)
				? ov.InterfaceImplementor.Implementor
				: ov.Override;
			Debug.Assert (provider.GetType () == baseProvider.GetType ());
			Debug.Assert (!(provider is GenericParameter genericParameter) || genericParameter.DeclaringMethod != null);
			switch (provider) {
			case ParameterDefinition parameterDefinition:
				var baseParameterDefinition = (ParameterDefinition) baseProvider;
				_context.LogWarning (origin, DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides,
					DiagnosticUtilities.GetParameterNameForErrorMessage (parameterDefinition), DiagnosticUtilities.GetMethodSignatureDisplayName (parameterDefinition.Method),
					DiagnosticUtilities.GetParameterNameForErrorMessage (baseParameterDefinition), DiagnosticUtilities.GetMethodSignatureDisplayName (baseParameterDefinition.Method));
				break;
			case MethodReturnType methodReturnType:
				_context.LogWarning (origin, DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides,
					DiagnosticUtilities.GetMethodSignatureDisplayName (methodReturnType.Method), DiagnosticUtilities.GetMethodSignatureDisplayName (((MethodReturnType) baseProvider).Method));
				break;
			// No fields - it's not possible to have a virtual field and override it
			case MethodDefinition methodDefinition:
				_context.LogWarning (origin, DiagnosticId.DynamicallyAccessedMembersMismatchOnImplicitThisBetweenOverrides,
					DiagnosticUtilities.GetMethodSignatureDisplayName (methodDefinition), DiagnosticUtilities.GetMethodSignatureDisplayName ((MethodDefinition) baseProvider));
				break;
			case GenericParameter genericParameterOverride:
				var genericParameterBase = (GenericParameter) baseProvider;
				_context.LogWarning (origin, DiagnosticId.DynamicallyAccessedMembersMismatchOnGenericParameterBetweenOverrides,
					genericParameterOverride.Name, DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (genericParameterOverride),
					genericParameterBase.Name, DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (genericParameterBase));
				break;
			default:
				throw new NotImplementedException ($"Unsupported provider type '{provider.GetType ()}'.");
			}
		}

		readonly struct TypeAnnotations
		{
			readonly TypeDefinition _type;
			readonly DynamicallyAccessedMemberTypes _typeAnnotation;
			readonly MethodAnnotations[]? _annotatedMethods;
			readonly FieldAnnotation[]? _annotatedFields;
			readonly DynamicallyAccessedMemberTypes[]? _genericParameterAnnotations;

			public TypeAnnotations (
				TypeDefinition type,
				DynamicallyAccessedMemberTypes typeAnnotation,
				MethodAnnotations[]? annotatedMethods,
				FieldAnnotation[]? annotatedFields,
				DynamicallyAccessedMemberTypes[]? genericParameterAnnotations)
				=> (_type, _typeAnnotation, _annotatedMethods, _annotatedFields, _genericParameterAnnotations)
				 = (type, typeAnnotation, annotatedMethods, annotatedFields, genericParameterAnnotations);

			public DynamicallyAccessedMemberTypes TypeAnnotation { get => _typeAnnotation; }

			public bool TryGetAnnotation (MethodDefinition method, out MethodAnnotations annotations)
			{
				annotations = default;

				if (_annotatedMethods == null) {
					return false;
				}

				foreach (var m in _annotatedMethods) {
					if (m.Method == method) {
						annotations = m;
						return true;
					}
				}

				return false;
			}

			public bool TryGetAnnotation (FieldDefinition field, out FieldAnnotation annotation)
			{
				annotation = default;

				if (_annotatedFields == null) {
					return false;
				}

				foreach (var f in _annotatedFields) {
					if (f.Field == field) {
						annotation = f;
						return true;
					}
				}

				return false;
			}

			public bool TryGetAnnotation (GenericParameter genericParameter, out DynamicallyAccessedMemberTypes annotation)
			{
				annotation = default;

				if (_genericParameterAnnotations == null)
					return false;

				for (int genericParameterIndex = 0; genericParameterIndex < _genericParameterAnnotations.Length; genericParameterIndex++) {
					if (_type.GenericParameters[genericParameterIndex] == genericParameter) {
						annotation = _genericParameterAnnotations[genericParameterIndex];
						return true;
					}
				}

				return false;
			}
		}

		readonly struct MethodAnnotations
		{
			public readonly MethodDefinition Method;
			public readonly DynamicallyAccessedMemberTypes[]? ParameterAnnotations;
			public readonly DynamicallyAccessedMemberTypes ReturnParameterAnnotation;
			public readonly DynamicallyAccessedMemberTypes[]? GenericParameterAnnotations;

			public MethodAnnotations (
				MethodDefinition method,
				DynamicallyAccessedMemberTypes[]? paramAnnotations,
				DynamicallyAccessedMemberTypes returnParamAnnotations,
				DynamicallyAccessedMemberTypes[]? genericParameterAnnotations)
				=> (Method, ParameterAnnotations, ReturnParameterAnnotation, GenericParameterAnnotations) =
					(method, paramAnnotations, returnParamAnnotations, genericParameterAnnotations);

			public bool TryGetAnnotation (GenericParameter genericParameter, out DynamicallyAccessedMemberTypes annotation)
			{
				annotation = default;

				if (GenericParameterAnnotations == null)
					return false;

				for (int genericParameterIndex = 0; genericParameterIndex < GenericParameterAnnotations.Length; genericParameterIndex++) {
					if (Method.GenericParameters[genericParameterIndex] == genericParameter) {
						annotation = GenericParameterAnnotations[genericParameterIndex];
						return true;
					}
				}

				return false;
			}
		}

		readonly struct FieldAnnotation
		{
			public readonly FieldDefinition Field;
			public readonly DynamicallyAccessedMemberTypes Annotation;

			public FieldAnnotation (FieldDefinition field, DynamicallyAccessedMemberTypes annotation)
				=> (Field, Annotation) = (field, annotation);
		}

		internal partial bool MethodRequiresDataFlowAnalysis (MethodProxy method)
			=> RequiresDataFlowAnalysis (method.Method);

#pragma warning disable CA1822 // Mark members as static - Should be an instance method for consistency
		internal partial MethodReturnValue GetMethodReturnValue (MethodProxy method, bool isNewObj, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> MethodReturnValue.Create (method.Method, isNewObj, dynamicallyAccessedMemberTypes);
#pragma warning restore CA1822 // Mark members as static

		internal partial MethodReturnValue GetMethodReturnValue (MethodProxy method, bool isNewObj)
			=> GetMethodReturnValue (method, isNewObj, GetReturnParameterAnnotation (method.Method));

#pragma warning disable CA1822 // Mark members as static - Should be an instance method for consistency
		internal partial GenericParameterValue GetGenericParameterValue (GenericParameterProxy genericParameter, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new GenericParameterValue (genericParameter.GenericParameter, dynamicallyAccessedMemberTypes);
#pragma warning restore CA1822 // Mark members as static

		internal partial GenericParameterValue GetGenericParameterValue (GenericParameterProxy genericParameter)
			=> new GenericParameterValue (genericParameter.GenericParameter, GetGenericParameterAnnotation (genericParameter.GenericParameter));

#pragma warning disable CA1822 // Mark members as static - Should be an instance method for consistency
		internal partial MethodParameterValue GetMethodParameterValue (ParameterProxy param, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new (param.ParameterType, param, dynamicallyAccessedMemberTypes);
#pragma warning restore CA1822 // Mark members as static

		internal partial MethodParameterValue GetMethodParameterValue (ParameterProxy param)
			=> GetMethodParameterValue (param, GetParameterAnnotation (param));

#pragma warning disable CA1822 // Mark members as static - Should be an instance method for consistency
		internal partial MethodParameterValue GetMethodThisParameterValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			if (!method.HasImplicitThis ())
				throw new InvalidOperationException ($"Cannot get 'this' parameter of method {method.GetDisplayName ()} with no 'this' parameter.");
			return new MethodParameterValue (method.Method.DeclaringType, new ParameterProxy (method, (ParameterIndex) 0), dynamicallyAccessedMemberTypes);
		}
#pragma warning restore CA1822 // Mark members as static

		internal partial MethodParameterValue GetMethodThisParameterValue (MethodProxy method)
		{
			if (!method.HasImplicitThis ())
				throw new InvalidOperationException ($"Cannot get 'this' parameter of method {method.GetDisplayName ()} with no 'this' parameter.");
			ParameterProxy param = new (method, (ParameterIndex) 0);
			var damt = GetParameterAnnotation (param);
			return GetMethodParameterValue (new ParameterProxy (method, (ParameterIndex) 0), damt);
		}

		// Trimming dataflow value creation. Eventually more of these should be shared.
		internal SingleValue GetFieldValue (FieldDefinition field)
			=> field.Name switch {
				"EmptyTypes" when field.DeclaringType.IsTypeOf (WellKnownType.System_Type) => ArrayValue.Create (0, field.DeclaringType),
				"Empty" when field.DeclaringType.IsTypeOf (WellKnownType.System_String) => new KnownStringValue (string.Empty),
				_ => new FieldValue (field.FieldType, field, GetFieldAnnotation (field))
			};

		internal SingleValue GetTypeValueFromGenericArgument (TypeReference genericArgument)
		{
			if (genericArgument is GenericParameter inputGenericParameter) {
				// Technically this should be a new value node type as it's not a System.Type instance representation, but just the generic parameter
				// That said we only use it to perform the dynamically accessed members checks and for that purpose treating it as System.Type is perfectly valid.
				return GetGenericParameterValue (inputGenericParameter);
			} else if (genericArgument.ResolveToTypeDefinition (_context) is TypeDefinition genericArgumentType) {
				if (genericArgumentType.IsTypeOf (WellKnownType.System_Nullable_T)) {
					var innerGenericArgument = (genericArgument as IGenericInstance)?.GenericArguments.FirstOrDefault ();
					switch (innerGenericArgument) {
					case GenericParameter gp:
						return new NullableValueWithDynamicallyAccessedMembers (genericArgumentType,
							new GenericParameterValue (gp, _context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (gp)));

					case TypeReference underlyingType:
						if (underlyingType.ResolveToTypeDefinition (_context) is TypeDefinition underlyingTypeDefinition)
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
	}
}
