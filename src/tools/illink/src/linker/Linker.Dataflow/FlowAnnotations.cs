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
	sealed partial class FlowAnnotations
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

		/// <summary>
		/// Retrieves the annotations for the given parameter.
		/// </summary>
		/// <param name="parameterIndex">Parameter index in the IL sense. Parameter 0 on instance methods is `this`.</param>
		/// <returns></returns>
		public DynamicallyAccessedMemberTypes GetParameterAnnotation (MethodDefinition method, int parameterIndex)
		{
			if (GetAnnotations (method.DeclaringType).TryGetAnnotation (method, out var annotation) &&
				annotation.ParameterAnnotations != null)
				return annotation.ParameterAnnotations[parameterIndex];

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
			//       // Linker will not see this code, so there are no checks
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

			TypeDefinition? type = _context.TryResolve (typeReference);
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

			var annotatedFields = new ArrayBuilder<FieldAnnotation> ();

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

			var annotatedMethods = new ArrayBuilder<MethodAnnotations> ();

			// Next go over all methods with an explicit annotation
			if (type.HasMethods) {
				foreach (MethodDefinition method in type.Methods) {
					DynamicallyAccessedMemberTypes[]? paramAnnotations = null;

					// We convert indices from metadata space to IL space here.
					// IL space assigns index 0 to the `this` parameter on instance methods.


					DynamicallyAccessedMemberTypes methodMemberTypes = GetMemberTypesForDynamicallyAccessedMembersAttribute (method);

					int offset;
					if (method.HasImplicitThis ()) {
						offset = 1;
						if (IsTypeInterestingForDataflow (method.DeclaringType)) {
							// If there's an annotation on the method itself and it's one of the special types (System.Type for example)
							// treat that annotation as annotating the "this" parameter.
							if (methodMemberTypes != DynamicallyAccessedMemberTypes.None) {
								paramAnnotations = new DynamicallyAccessedMemberTypes[method.Parameters.Count + offset];
								paramAnnotations[0] = methodMemberTypes;
							}
						} else if (methodMemberTypes != DynamicallyAccessedMemberTypes.None) {
							_context.LogWarning (method, DiagnosticId.DynamicallyAccessedMembersIsNotAllowedOnMethods);
						}
					} else {
						offset = 0;
						if (methodMemberTypes != DynamicallyAccessedMemberTypes.None) {
							_context.LogWarning (method, DiagnosticId.DynamicallyAccessedMembersIsNotAllowedOnMethods);
						}
					}

					for (int i = 0; i < method.Parameters.Count; i++) {
						var methodParameter = method.Parameters[i];
						DynamicallyAccessedMemberTypes pa = GetMemberTypesForDynamicallyAccessedMembersAttribute (method, providerIfNotMember: methodParameter);
						if (pa == DynamicallyAccessedMemberTypes.None)
							continue;

						if (!IsTypeInterestingForDataflow (methodParameter.ParameterType)) {
							_context.LogWarning (method, DiagnosticId.DynamicallyAccessedMembersOnMethodParameterCanOnlyApplyToTypesOrStrings,
								DiagnosticUtilities.GetParameterNameForErrorMessage (methodParameter), DiagnosticUtilities.GetMethodSignatureDisplayName (methodParameter.Method));
							continue;
						}

						if (paramAnnotations == null) {
							paramAnnotations = new DynamicallyAccessedMemberTypes[method.Parameters.Count + offset];
						}
						paramAnnotations[i + offset] = pa;
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
								if (genericParameterAnnotations == null)
									genericParameterAnnotations = new DynamicallyAccessedMemberTypes[method.GenericParameters.Count];
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

						if (annotatedMethods.Any (a => a.Method == setMethod)) {
							_context.LogWarning (setMethod, DiagnosticId.DynamicallyAccessedMembersConflictsBetweenPropertyAndAccessor, property.GetDisplayName (), setMethod.GetDisplayName ());
						} else {
							int offset = setMethod.HasImplicitThis () ? 1 : 0;
							if (setMethod.Parameters.Count > 0) {
								DynamicallyAccessedMemberTypes[] paramAnnotations = new DynamicallyAccessedMemberTypes[setMethod.Parameters.Count + offset];
								paramAnnotations[paramAnnotations.Length - 1] = annotation;
								annotatedMethods.Add (new MethodAnnotations (setMethod, paramAnnotations, DynamicallyAccessedMemberTypes.None, null));
							}
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

						if (annotatedMethods.Any (a => a.Method == getMethod)) {
							_context.LogWarning (getMethod, DiagnosticId.DynamicallyAccessedMembersConflictsBetweenPropertyAndAccessor, property.GetDisplayName (), getMethod.GetDisplayName ());
						} else {
							annotatedMethods.Add (new MethodAnnotations (getMethod, null, annotation, null));
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
						if (typeGenericParameterAnnotations == null)
							typeGenericParameterAnnotations = new DynamicallyAccessedMemberTypes[type.GenericParameters.Count];
						typeGenericParameterAnnotations[genericParameterIndex] = annotation;
					}
				}
			}

			return new TypeAnnotations (type, typeAnnotation, annotatedMethods.ToArray (), annotatedFields.ToArray (), typeGenericParameterAnnotations);
		}

		private IReadOnlyList<ICustomAttributeProvider>? GetGeneratedTypeAttributes (TypeDefinition typeDef)
		{
			if (!CompilerGeneratedNames.IsGeneratedType (typeDef.Name)) {
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

			foreach (Instruction instruction in body.Instructions) {
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

		internal void ValidateMethodAnnotationsAreSame (MethodDefinition method, MethodDefinition baseMethod)
		{
			GetAnnotations (method.DeclaringType).TryGetAnnotation (method, out var methodAnnotations);
			GetAnnotations (baseMethod.DeclaringType).TryGetAnnotation (baseMethod, out var baseMethodAnnotations);

			if (methodAnnotations.ReturnParameterAnnotation != baseMethodAnnotations.ReturnParameterAnnotation)
				LogValidationWarning (method.MethodReturnType, baseMethod.MethodReturnType, method);

			if (methodAnnotations.ParameterAnnotations != null || baseMethodAnnotations.ParameterAnnotations != null) {
				if (methodAnnotations.ParameterAnnotations == null)
					ValidateMethodParametersHaveNoAnnotations (baseMethodAnnotations.ParameterAnnotations!, method, baseMethod, method);
				else if (baseMethodAnnotations.ParameterAnnotations == null)
					ValidateMethodParametersHaveNoAnnotations (methodAnnotations.ParameterAnnotations, method, baseMethod, method);
				else {
					if (methodAnnotations.ParameterAnnotations.Length != baseMethodAnnotations.ParameterAnnotations.Length)
						return;

					for (int parameterIndex = 0; parameterIndex < methodAnnotations.ParameterAnnotations.Length; parameterIndex++) {
						if (methodAnnotations.ParameterAnnotations[parameterIndex] != baseMethodAnnotations.ParameterAnnotations[parameterIndex])
							LogValidationWarning (
								DiagnosticUtilities.GetMethodParameterFromIndex (method, parameterIndex),
								DiagnosticUtilities.GetMethodParameterFromIndex (baseMethod, parameterIndex),
								method);
					}
				}
			}

			if (methodAnnotations.GenericParameterAnnotations != null || baseMethodAnnotations.GenericParameterAnnotations != null) {
				if (methodAnnotations.GenericParameterAnnotations == null)
					ValidateMethodGenericParametersHaveNoAnnotations (baseMethodAnnotations.GenericParameterAnnotations!, method, baseMethod, method);
				else if (baseMethodAnnotations.GenericParameterAnnotations == null)
					ValidateMethodGenericParametersHaveNoAnnotations (methodAnnotations.GenericParameterAnnotations, method, baseMethod, method);
				else {
					if (methodAnnotations.GenericParameterAnnotations.Length != baseMethodAnnotations.GenericParameterAnnotations.Length)
						return;

					for (int genericParameterIndex = 0; genericParameterIndex < methodAnnotations.GenericParameterAnnotations.Length; genericParameterIndex++) {
						if (methodAnnotations.GenericParameterAnnotations[genericParameterIndex] != baseMethodAnnotations.GenericParameterAnnotations[genericParameterIndex]) {
							LogValidationWarning (
								method.GenericParameters[genericParameterIndex],
								baseMethod.GenericParameters[genericParameterIndex],
								method);
						}
					}
				}
			}
		}

		void ValidateMethodParametersHaveNoAnnotations (DynamicallyAccessedMemberTypes[] parameterAnnotations, MethodDefinition method, MethodDefinition baseMethod, IMemberDefinition origin)
		{
			for (int parameterIndex = 0; parameterIndex < parameterAnnotations.Length; parameterIndex++) {
				var annotation = parameterAnnotations[parameterIndex];
				if (annotation != DynamicallyAccessedMemberTypes.None)
					LogValidationWarning (
						DiagnosticUtilities.GetMethodParameterFromIndex (method, parameterIndex),
						DiagnosticUtilities.GetMethodParameterFromIndex (baseMethod, parameterIndex),
						origin);
			}
		}

		void ValidateMethodGenericParametersHaveNoAnnotations (DynamicallyAccessedMemberTypes[] genericParameterAnnotations, MethodDefinition method, MethodDefinition baseMethod, IMemberDefinition origin)
		{
			for (int genericParameterIndex = 0; genericParameterIndex < genericParameterAnnotations.Length; genericParameterIndex++) {
				if (genericParameterAnnotations[genericParameterIndex] != DynamicallyAccessedMemberTypes.None) {
					LogValidationWarning (
						method.GenericParameters[genericParameterIndex],
						baseMethod.GenericParameters[genericParameterIndex],
						origin);
				}
			}
		}

		void LogValidationWarning (IMetadataTokenProvider provider, IMetadataTokenProvider baseProvider, IMemberDefinition origin)
		{
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

		internal partial MethodReturnValue GetMethodReturnValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new MethodReturnValue (method.Method.ReturnType.ResolveToTypeDefinition (_context), method.Method, dynamicallyAccessedMemberTypes);

		internal partial MethodReturnValue GetMethodReturnValue (MethodProxy method)
			=> GetMethodReturnValue (method, GetReturnParameterAnnotation (method.Method));

		internal partial GenericParameterValue GetGenericParameterValue (GenericParameterProxy genericParameter)
			=> new GenericParameterValue (genericParameter.GenericParameter, GetGenericParameterAnnotation (genericParameter.GenericParameter));

#pragma warning disable CA1822 // Mark members as static - keep this an instance method for consistency with the others
		internal partial MethodThisParameterValue GetMethodThisParameterValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new MethodThisParameterValue (method.Method, dynamicallyAccessedMemberTypes);
#pragma warning restore CA1822

		internal partial MethodThisParameterValue GetMethodThisParameterValue (MethodProxy method)
			=> GetMethodThisParameterValue (method, GetParameterAnnotation (method.Method, 0));

		internal partial MethodParameterValue GetMethodParameterValue (MethodProxy method, int parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new (method.Method.Parameters[parameterIndex].ParameterType.ResolveToTypeDefinition (_context), method.Method, parameterIndex, dynamicallyAccessedMemberTypes);

		internal partial MethodParameterValue GetMethodParameterValue (MethodProxy method, int parameterIndex)
			=> GetMethodParameterValue (method, parameterIndex, GetParameterAnnotation (method.Method, parameterIndex + (method.IsStatic () ? 0 : 1)));

		// Linker-specific dataflow value creation. Eventually more of these should be shared.
		internal SingleValue GetFieldValue (FieldDefinition field)
			=> field.Name switch {
				"EmptyTypes" when field.DeclaringType.IsTypeOf (WellKnownType.System_Type) => ArrayValue.Create (0, field.DeclaringType),
				"Empty" when field.DeclaringType.IsTypeOf (WellKnownType.System_String) => new KnownStringValue (string.Empty),
				_ => new FieldValue (field.FieldType.ResolveToTypeDefinition (_context), field, GetFieldAnnotation (field))
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
