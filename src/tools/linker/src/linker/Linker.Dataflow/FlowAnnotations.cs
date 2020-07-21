// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.Dataflow
{
	class FlowAnnotations
	{
		readonly LinkContext _context;
		readonly Dictionary<TypeDefinition, TypeAnnotations> _annotations = new Dictionary<TypeDefinition, TypeAnnotations> ();
		readonly TypeHierarchyCache _hierarchyInfo = new TypeHierarchyCache ();

		public FlowAnnotations (LinkContext context)
		{
			_context = context;
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

		public DynamicallyAccessedMemberTypes GetGenericParameterAnnotation (GenericParameter genericParameter)
		{
			TypeDefinition declaringType = genericParameter.DeclaringType?.Resolve ();
			if (declaringType != null) {
				if (GetAnnotations (declaringType).TryGetAnnotation (genericParameter, out var annotation))
					return annotation;

				return DynamicallyAccessedMemberTypes.None;
			}

			MethodDefinition declaringMethod = genericParameter.DeclaringMethod?.Resolve ();
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

		DynamicallyAccessedMemberTypes GetMemberTypesForDynamicallyAccessedMemberAttribute (ICustomAttributeProvider provider, IMemberDefinition locationMember = null)
		{
			if (!_context.CustomAttributes.HasCustomAttributes (provider))
				return DynamicallyAccessedMemberTypes.None;
			foreach (var attribute in _context.CustomAttributes.GetCustomAttributes (provider)) {
				if (!IsDynamicallyAccessedMembersAttribute (attribute))
					continue;
				if (attribute.ConstructorArguments.Count == 1)
					return (DynamicallyAccessedMemberTypes) (int) attribute.ConstructorArguments[0].Value;
				else if (attribute.ConstructorArguments.Count == 0)
					_context.LogWarning ($"DynamicallyAccessedMembersAttribute was specified but no argument was proportioned", 2020, locationMember ?? (provider as IMemberDefinition));
				else
					_context.LogWarning ($"DynamicallyAccessedMembersAttribute was specified but there is more than one argument", 2022, locationMember ?? (provider as IMemberDefinition));
			}
			return DynamicallyAccessedMemberTypes.None;
		}

		TypeAnnotations BuildTypeAnnotations (TypeDefinition type)
		{
			var annotatedFields = new ArrayBuilder<FieldAnnotation> ();

			// First go over all fields with an explicit annotation
			if (type.HasFields) {
				foreach (FieldDefinition field in type.Fields) {
					if (!IsTypeInterestingForDataflow (field.FieldType))
						continue;

					DynamicallyAccessedMemberTypes annotation = GetMemberTypesForDynamicallyAccessedMemberAttribute (field);
					if (annotation == DynamicallyAccessedMemberTypes.None) {
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


					DynamicallyAccessedMemberTypes methodMemberTypes = GetMemberTypesForDynamicallyAccessedMemberAttribute (method);

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
							_context.LogWarning ($"The DynamicallyAccessedMembersAttribute is only allowed on method parameters, return value or generic parameters.", 2041, method);
						}
					} else {
						offset = 0;
						if (methodMemberTypes != DynamicallyAccessedMemberTypes.None) {
							_context.LogWarning ($"The DynamicallyAccessedMembersAttribute is only allowed on method parameters, return value or generic parameters.", 2041, method);
						}
					}

					for (int i = 0; i < method.Parameters.Count; i++) {
						if (!IsTypeInterestingForDataflow (method.Parameters[i].ParameterType)) {
							continue;
						}

						DynamicallyAccessedMemberTypes pa = GetMemberTypesForDynamicallyAccessedMemberAttribute (method.Parameters[i], method);
						if (pa == DynamicallyAccessedMemberTypes.None) {
							continue;
						}

						if (paramAnnotations == null) {
							paramAnnotations = new DynamicallyAccessedMemberTypes[method.Parameters.Count + offset];
						}
						paramAnnotations[i + offset] = pa;
					}

					DynamicallyAccessedMemberTypes returnAnnotation = IsTypeInterestingForDataflow (method.ReturnType) ?
						GetMemberTypesForDynamicallyAccessedMemberAttribute (method.MethodReturnType, method) : DynamicallyAccessedMemberTypes.None;

					DynamicallyAccessedMemberTypes[] genericParameterAnnotations = null;
					if (method.HasGenericParameters) {
						for (int genericParameterIndex = 0; genericParameterIndex < method.GenericParameters.Count; genericParameterIndex++) {
							var genericParameter = method.GenericParameters[genericParameterIndex];
							var annotation = GetMemberTypesForDynamicallyAccessedMemberAttribute (genericParameter, method);
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

					if (!IsTypeInterestingForDataflow (property.PropertyType)) {
						continue;
					}

					DynamicallyAccessedMemberTypes annotation = GetMemberTypesForDynamicallyAccessedMemberAttribute (property);
					if (annotation == DynamicallyAccessedMemberTypes.None) {
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
							_context.LogWarning ($"Trying to propagate DynamicallyAccessedMemberAttribute from property '{property.FullName}' to its setter '{setMethod.GetDisplayName ()}', but it already has such attribute on the 'value' parameter.", 2043, setMethod);
						} else {
							int offset = setMethod.HasImplicitThis () ? 1 : 0;
							if (setMethod.Parameters.Count > 0) {
								DynamicallyAccessedMemberTypes[] paramAnnotations = new DynamicallyAccessedMemberTypes[setMethod.Parameters.Count + offset];
								paramAnnotations[offset] = annotation;
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
							_context.LogWarning ($"Trying to propagate DynamicallyAccessedMemberAttribute from property '{property.FullName}' to its getter '{getMethod.GetDisplayName ()}', but it already has such attribute on the return value.",
								2043, getMethod, subcategory: MessageSubCategory.TrimAnalysis);
						} else {
							annotatedMethods.Add (new MethodAnnotations (getMethod, null, annotation, null));
						}
					}

					FieldDefinition backingField;
					if (backingFieldFromGetter != null && backingFieldFromSetter != null &&
						backingFieldFromGetter != backingFieldFromSetter) {
						_context.LogWarning ($"Could not find a unique backing field for property '{property.FullName}' to propagate DynamicallyAccessedMembersAttribute. The backing fields from getter '{backingFieldFromGetter.FullName}' and setter '{backingFieldFromSetter.FullName}' are not the same.", 2042, property);
						backingField = null;
					} else {
						backingField = backingFieldFromGetter ?? backingFieldFromSetter;
					}

					if (backingField != null) {
						if (annotatedFields.Any (a => a.Field == backingField)) {
							_context.LogWarning ($"Trying to propagate DynamicallyAccessedMemberAttribute from property '{property.FullName}' to its field '{backingField}', but it already has such attribute.",
								2043, backingField, subcategory: MessageSubCategory.TrimAnalysis);
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
					var annotation = GetMemberTypesForDynamicallyAccessedMemberAttribute (genericParameter, type);
					if (annotation != DynamicallyAccessedMemberTypes.None) {
						if (typeGenericParameterAnnotations == null)
							typeGenericParameterAnnotations = new DynamicallyAccessedMemberTypes[type.GenericParameters.Count];
						typeGenericParameterAnnotations[genericParameterIndex] = annotation;
					}
				}
			}

			return new TypeAnnotations (type, annotatedMethods.ToArray (), annotatedFields.ToArray (), typeGenericParameterAnnotations);
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

			found = foundReference.Resolve ();

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
			return typeReference.MetadataType == MetadataType.String ||
				_hierarchyInfo.IsSystemType (typeReference) ||
				_hierarchyInfo.IsSystemReflectionIReflect (typeReference);
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
			_context.LogWarning (
				$"DynamicallyAccessedMemberTypes in DynamicallyAccessedMembersAttribute on {DiagnosticUtilities.GetMetadataTokenDescriptionForErrorMessage (provider)} " +
				$"don't match overridden {DiagnosticUtilities.GetMetadataTokenDescriptionForErrorMessage (baseProvider)}. " +
				$"All overridden members must have the same DynamicallyAccessedMembersAttribute usage.",
				2047,
				origin);
		}

		readonly struct TypeAnnotations
		{
			readonly TypeDefinition _type;
			readonly MethodAnnotations[] _annotatedMethods;
			readonly FieldAnnotation[] _annotatedFields;
			readonly DynamicallyAccessedMemberTypes[] _genericParameterAnnotations;

			public TypeAnnotations (
				TypeDefinition type,
				MethodAnnotations[] annotatedMethods,
				FieldAnnotation[] annotatedFields,
				DynamicallyAccessedMemberTypes[] genericParameterAnnotations)
				=> (_type, _annotatedMethods, _annotatedFields, _genericParameterAnnotations)
				 = (type, annotatedMethods, annotatedFields, genericParameterAnnotations);

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
