// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.Dataflow
{
	class FlowAnnotations
	{
		readonly LinkContext _context;
		readonly Dictionary<TypeDefinition, TypeAnnotations> _annotations = new Dictionary<TypeDefinition, TypeAnnotations> ();
		readonly TypeHierarchyCache _hierarchyInfo;

		public FlowAnnotations (LinkContext context)
		{
			_context = context;
			_hierarchyInfo = new TypeHierarchyCache (context);
		}

		public bool RequiresDataFlowAnalysis (MethodDefinition method)
		{
			return GetAnnotations (method.DeclaringType).TryGetAnnotation (method, out _);
		}

		public bool RequiresDataFlowAnalysis (FieldDefinition field)
		{
			return GetAnnotations (field.DeclaringType).TryGetAnnotation (field, out _);
		}

		public bool RequiresDataFlowAnalysis (GenericParameter genericParameter)
		{
			return GetGenericParameterAnnotation (genericParameter) != DynamicallyAccessedMemberTypes.None;
		}

		/// <summary>
		/// Retrieves the annotations for the given parameter.
		/// </summary>
		/// <param name="parameterIndex">Parameter index in the IL sense. Parameter 0 on instance methods is `this`.</param>
		/// <returns></returns>
		public DynamicallyAccessedMemberTypes GetParameterAnnotation (MethodDefinition method, int parameterIndex)
		{
			if (GetAnnotations (method.DeclaringType).TryGetAnnotation (method, out var annotation) && annotation.ParameterAnnotations != null) {
				return annotation.ParameterAnnotations[parameterIndex];
			}

			return DynamicallyAccessedMemberTypes.None;
		}

		public DynamicallyAccessedMemberTypes GetReturnParameterAnnotation (MethodDefinition method)
		{
			if (GetAnnotations (method.DeclaringType).TryGetAnnotation (method, out var annotation)) {
				return annotation.ReturnParameterAnnotation;
			}

			return DynamicallyAccessedMemberTypes.None;
		}

		public DynamicallyAccessedMemberTypes GetFieldAnnotation (FieldDefinition field)
		{
			if (GetAnnotations (field.DeclaringType).TryGetAnnotation (field, out var annotation)) {
				return annotation.Annotation;
			}

			return DynamicallyAccessedMemberTypes.None;
		}

		public DynamicallyAccessedMemberTypes GetTypeAnnotation (TypeDefinition type)
		{
			return GetAnnotations (type).TypeAnnotation;
		}

		public DynamicallyAccessedMemberTypes GetGenericParameterAnnotation (GenericParameter genericParameter)
		{
			TypeDefinition declaringType = _context.ResolveTypeDefinition (genericParameter.DeclaringType);
			if (declaringType != null) {
				if (GetAnnotations (declaringType).TryGetAnnotation (genericParameter, out var annotation))
					return annotation;

				return DynamicallyAccessedMemberTypes.None;
			}

			MethodDefinition declaringMethod = _context.ResolveMethodDefinition (genericParameter.DeclaringMethod);
			if (declaringMethod != null && GetAnnotations (declaringMethod.DeclaringType).TryGetAnnotation (declaringMethod, out var methodTypeAnnotations) &&
				methodTypeAnnotations.TryGetAnnotation (genericParameter, out var methodAnnotation))
				return methodAnnotation;

			return DynamicallyAccessedMemberTypes.None;
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

		DynamicallyAccessedMemberTypes GetMemberTypesForDynamicallyAccessedMembersAttribute (ICustomAttributeProvider provider, IMemberDefinition locationMember = null)
		{
			if (!_context.CustomAttributes.HasAny (provider))
				return DynamicallyAccessedMemberTypes.None;
			foreach (var attribute in _context.CustomAttributes.GetCustomAttributes (provider)) {
				if (!IsDynamicallyAccessedMembersAttribute (attribute))
					continue;
				if (attribute.ConstructorArguments.Count == 1)
					return (DynamicallyAccessedMemberTypes) (int) attribute.ConstructorArguments[0].Value;
				else
					_context.LogWarning (
						$"Attribute 'System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute' doesn't have the required number of parameters specified",
						2028, locationMember ?? (provider as IMemberDefinition));
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
						_context.LogWarning (
							$"Field '{field.GetDisplayName ()}' has 'DynamicallyAccessedMembersAttribute', but that attribute can only be applied to fields of type 'System.Type' or 'System.String'",
							2097, field, subcategory: MessageSubCategory.TrimAnalysis);
						continue;
					}

					annotatedFields.Add (new FieldAnnotation (field, annotation));
				}
			}

			var annotatedMethods = new ArrayBuilder<MethodAnnotations> ();

			// Next go over all methods with an explicit annotation
			if (type.HasMethods) {
				foreach (MethodDefinition method in type.Methods) {
					DynamicallyAccessedMemberTypes[] paramAnnotations = null;

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
							_context.LogWarning (
								$"The 'DynamicallyAccessedMembersAttribute' is not allowed on methods. It is allowed on method return value or method parameters though.",
								2041, method, subcategory: MessageSubCategory.TrimAnalysis);
						}
					} else {
						offset = 0;
						if (methodMemberTypes != DynamicallyAccessedMemberTypes.None) {
							_context.LogWarning (
								$"The 'DynamicallyAccessedMembersAttribute' is not allowed on methods. It is allowed on method return value or method parameters though.",
								2041, method, subcategory: MessageSubCategory.TrimAnalysis);
						}
					}

					for (int i = 0; i < method.Parameters.Count; i++) {
						var methodParameter = method.Parameters[i];
						DynamicallyAccessedMemberTypes pa = GetMemberTypesForDynamicallyAccessedMembersAttribute (methodParameter, method);
						if (pa == DynamicallyAccessedMemberTypes.None)
							continue;

						if (!IsTypeInterestingForDataflow (methodParameter.ParameterType)) {
							_context.LogWarning (
								$"Parameter '{DiagnosticUtilities.GetParameterNameForErrorMessage (methodParameter)}' of method '{DiagnosticUtilities.GetMethodSignatureDisplayName (methodParameter.Method)}' has 'DynamicallyAccessedMembersAttribute', but that attribute can only be applied to parameters of type 'System.Type' or 'System.String'",
								2098, method, subcategory: MessageSubCategory.TrimAnalysis);
							continue;
						}

						if (paramAnnotations == null) {
							paramAnnotations = new DynamicallyAccessedMemberTypes[method.Parameters.Count + offset];
						}
						paramAnnotations[i + offset] = pa;
					}

					DynamicallyAccessedMemberTypes returnAnnotation = IsTypeInterestingForDataflow (method.ReturnType) ?
						GetMemberTypesForDynamicallyAccessedMembersAttribute (method.MethodReturnType, method) : DynamicallyAccessedMemberTypes.None;

					DynamicallyAccessedMemberTypes[] genericParameterAnnotations = null;
					if (method.HasGenericParameters) {
						for (int genericParameterIndex = 0; genericParameterIndex < method.GenericParameters.Count; genericParameterIndex++) {
							var genericParameter = method.GenericParameters[genericParameterIndex];
							var annotation = GetMemberTypesForDynamicallyAccessedMembersAttribute (genericParameter, method);
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
						_context.LogWarning (
							$"Property '{property.GetDisplayName ()}' has 'DynamicallyAccessedMembersAttribute', but that attribute can only be applied to properties of type 'System.Type' or 'System.String'",
							2099, property, subcategory: MessageSubCategory.TrimAnalysis);
						continue;
					}

					FieldDefinition backingFieldFromSetter = null;

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
							_context.LogWarning (
								$"'DynamicallyAccessedMembersAttribute' on property '{property.GetDisplayName ()}' conflicts with the same attribute on its accessor '{setMethod.GetDisplayName ()}'.",
								2043, setMethod, subcategory: MessageSubCategory.TrimAnalysis);
						} else {
							int offset = setMethod.HasImplicitThis () ? 1 : 0;
							if (setMethod.Parameters.Count > 0) {
								DynamicallyAccessedMemberTypes[] paramAnnotations = new DynamicallyAccessedMemberTypes[setMethod.Parameters.Count + offset];
								paramAnnotations[paramAnnotations.Length - 1] = annotation;
								annotatedMethods.Add (new MethodAnnotations (setMethod, paramAnnotations, DynamicallyAccessedMemberTypes.None, null));
							}
						}
					}

					FieldDefinition backingFieldFromGetter = null;

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
							_context.LogWarning (
								$"'DynamicallyAccessedMembersAttribute' on property '{property.GetDisplayName ()}' conflicts with the same attribute on its accessor '{getMethod.GetDisplayName ()}'.",
								2043, getMethod, subcategory: MessageSubCategory.TrimAnalysis);
						} else {
							annotatedMethods.Add (new MethodAnnotations (getMethod, null, annotation, null));
						}
					}

					FieldDefinition backingField;
					if (backingFieldFromGetter != null && backingFieldFromSetter != null &&
						backingFieldFromGetter != backingFieldFromSetter) {
						_context.LogWarning (
							$"Could not find a unique backing field for property '{property.GetDisplayName ()}' to propagate 'DynamicallyAccessedMembersAttribute'.",
							2042, property, subcategory: MessageSubCategory.TrimAnalysis);
						backingField = null;
					} else {
						backingField = backingFieldFromGetter ?? backingFieldFromSetter;
					}

					if (backingField != null) {
						if (annotatedFields.Any (a => a.Field == backingField)) {
							_context.LogWarning (
								$"'DynamicallyAccessedMemberAttribute' on property '{property.GetDisplayName ()}' conflicts with the same attribute on its backing field '{backingField.GetDisplayName ()}'.",
								2056, backingField, subcategory: MessageSubCategory.TrimAnalysis);
						} else {
							annotatedFields.Add (new FieldAnnotation (backingField, annotation));
						}
					}
				}
			}

			DynamicallyAccessedMemberTypes[] typeGenericParameterAnnotations = null;
			if (type.HasGenericParameters) {
				for (int genericParameterIndex = 0; genericParameterIndex < type.GenericParameters.Count; genericParameterIndex++) {
					var genericParameter = type.GenericParameters[genericParameterIndex];
					var annotation = GetMemberTypesForDynamicallyAccessedMembersAttribute (genericParameter, type);
					if (annotation != DynamicallyAccessedMemberTypes.None) {
						if (typeGenericParameterAnnotations == null)
							typeGenericParameterAnnotations = new DynamicallyAccessedMemberTypes[type.GenericParameters.Count];
						typeGenericParameterAnnotations[genericParameterIndex] = annotation;
					}
				}
			}

			return new TypeAnnotations (type, typeAnnotation, annotatedMethods.ToArray (), annotatedFields.ToArray (), typeGenericParameterAnnotations);
		}

		bool ScanMethodBodyForFieldAccess (MethodBody body, bool write, out FieldDefinition found)
		{
			// Tries to find the backing field for a property getter/setter.
			// Returns true if this is a method body that we can unambiguously analyze.
			// The found field could still be null if there's no backing store.

			FieldReference foundReference = null;

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

			found = _context.ResolveFieldDefinition (foundReference);

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

		bool IsTypeInterestingForDataflow (TypeReference typeReference)
		{
			if (typeReference.MetadataType == MetadataType.String)
				return true;

			TypeDefinition type = _context.TryResolveTypeDefinition (typeReference);
			return type != null && (
				_hierarchyInfo.IsSystemType (type) ||
				_hierarchyInfo.IsSystemReflectionIReflect (type));
		}

		internal void ValidateMethodAnnotationsAreSame (MethodDefinition method, MethodDefinition baseMethod)
		{
			GetAnnotations (method.DeclaringType).TryGetAnnotation (method, out var methodAnnotations);
			GetAnnotations (baseMethod.DeclaringType).TryGetAnnotation (baseMethod, out var baseMethodAnnotations);

			if (methodAnnotations.ReturnParameterAnnotation != baseMethodAnnotations.ReturnParameterAnnotation)
				LogValidationWarning (method.MethodReturnType, baseMethod.MethodReturnType, method);

			if (methodAnnotations.ParameterAnnotations != null || baseMethodAnnotations.ParameterAnnotations != null) {
				if (methodAnnotations.ParameterAnnotations == null)
					ValidateMethodParametersHaveNoAnnotations (ref baseMethodAnnotations, method, baseMethod, method);
				else if (baseMethodAnnotations.ParameterAnnotations == null)
					ValidateMethodParametersHaveNoAnnotations (ref methodAnnotations, method, baseMethod, method);
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
					ValidateMethodGenericParametersHaveNoAnnotations (ref baseMethodAnnotations, method, baseMethod, method);
				else if (baseMethodAnnotations.GenericParameterAnnotations == null)
					ValidateMethodGenericParametersHaveNoAnnotations (ref methodAnnotations, method, baseMethod, method);
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

		void ValidateMethodParametersHaveNoAnnotations (ref MethodAnnotations methodAnnotations, MethodDefinition method, MethodDefinition baseMethod, IMemberDefinition origin)
		{
			for (int parameterIndex = 0; parameterIndex < methodAnnotations.ParameterAnnotations.Length; parameterIndex++) {
				var annotation = methodAnnotations.ParameterAnnotations[parameterIndex];
				if (annotation != DynamicallyAccessedMemberTypes.None)
					LogValidationWarning (
						DiagnosticUtilities.GetMethodParameterFromIndex (method, parameterIndex),
						DiagnosticUtilities.GetMethodParameterFromIndex (baseMethod, parameterIndex),
						origin);
			}
		}

		void ValidateMethodGenericParametersHaveNoAnnotations (ref MethodAnnotations methodAnnotations, MethodDefinition method, MethodDefinition baseMethod, IMemberDefinition origin)
		{
			for (int genericParameterIndex = 0; genericParameterIndex < methodAnnotations.GenericParameterAnnotations.Length; genericParameterIndex++) {
				if (methodAnnotations.GenericParameterAnnotations[genericParameterIndex] != DynamicallyAccessedMemberTypes.None) {
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
				_context.LogWarning (
					$"'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the parameter '{DiagnosticUtilities.GetParameterNameForErrorMessage (parameterDefinition)}' of method '{DiagnosticUtilities.GetMethodSignatureDisplayName (parameterDefinition.Method)}' " +
					$"don't match overridden parameter '{DiagnosticUtilities.GetParameterNameForErrorMessage (baseParameterDefinition)}' of method '{DiagnosticUtilities.GetMethodSignatureDisplayName (baseParameterDefinition.Method)}'. " +
					$"All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.",
					2092, origin, subcategory: MessageSubCategory.TrimAnalysis);
				break;
			case MethodReturnType methodReturnType:
				_context.LogWarning (
					$"'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the return value of method '{DiagnosticUtilities.GetMethodSignatureDisplayName (methodReturnType.Method)}' " +
					$"don't match overridden return value of method '{DiagnosticUtilities.GetMethodSignatureDisplayName (((MethodReturnType) baseProvider).Method)}'. " +
					$"All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.",
					2093, origin, subcategory: MessageSubCategory.TrimAnalysis);
				break;
			// No fields - it's not possible to have a virtual field and override it
			case MethodDefinition methodDefinition:
				_context.LogWarning (
					$"'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the implicit 'this' parameter of method '{DiagnosticUtilities.GetMethodSignatureDisplayName (methodDefinition)}' " +
					$"don't match overridden implicit 'this' parameter of method '{DiagnosticUtilities.GetMethodSignatureDisplayName ((MethodDefinition) baseProvider)}'. " +
					$"All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.",
					2094, origin, subcategory: MessageSubCategory.TrimAnalysis);
				break;
			case GenericParameter genericParameterOverride:
				var genericParameterBase = (GenericParameter) baseProvider;
				_context.LogWarning (
					$"'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the generic parameter '{genericParameterOverride.Name}' of '{DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (genericParameterOverride)}' " +
					$"don't match overridden generic parameter '{genericParameterBase.Name}' of '{DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (genericParameterBase)}'. " +
					$"All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.",
					2095, origin, subcategory: MessageSubCategory.TrimAnalysis);
				break;
			default:
				throw new NotImplementedException ($"Unsupported provider type{provider.GetType ()}");
			}
		}

		readonly struct TypeAnnotations
		{
			readonly TypeDefinition _type;
			readonly DynamicallyAccessedMemberTypes _typeAnnotation;
			readonly MethodAnnotations[] _annotatedMethods;
			readonly FieldAnnotation[] _annotatedFields;
			readonly DynamicallyAccessedMemberTypes[] _genericParameterAnnotations;

			public TypeAnnotations (
				TypeDefinition type,
				DynamicallyAccessedMemberTypes typeAnnotation,
				MethodAnnotations[] annotatedMethods,
				FieldAnnotation[] annotatedFields,
				DynamicallyAccessedMemberTypes[] genericParameterAnnotations)
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
			public readonly DynamicallyAccessedMemberTypes[] ParameterAnnotations;
			public readonly DynamicallyAccessedMemberTypes ReturnParameterAnnotation;
			public readonly DynamicallyAccessedMemberTypes[] GenericParameterAnnotations;

			public MethodAnnotations (
				MethodDefinition method,
				DynamicallyAccessedMemberTypes[] paramAnnotations,
				DynamicallyAccessedMemberTypes returnParamAnnotations,
				DynamicallyAccessedMemberTypes[] genericParameterAnnotations)
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
	}
}
