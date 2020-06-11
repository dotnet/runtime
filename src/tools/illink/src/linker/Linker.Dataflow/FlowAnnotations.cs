// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.Dataflow
{
	// TODO: enforce virtual methods have consistent annotations

	class FlowAnnotations
	{
		readonly LinkContext _context;
		readonly Dictionary<TypeDefinition, TypeAnnotations> _annotations = new Dictionary<TypeDefinition, TypeAnnotations> ();

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
							// If there an annotation on the method itself and it's one of the special types (System.Type for example)
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
					if (returnAnnotation != DynamicallyAccessedMemberTypes.None || paramAnnotations != null) {
						annotatedMethods.Add (new MethodAnnotations (method, paramAnnotations, returnAnnotation));
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

						// TODO: Handle abstract properties - can't do any propagation on the base property, should rely on validation
						//  that overrides also correctly annotate.
						if (setMethod.HasBody) {
							// Look for the compiler generated backing field. If it doesn't work out simply move on. In such case we would still
							// propagate the annotation to the setter/getter and later on when analyzing the setter/getter we will warn
							// that the field (which ever it is) must be annotated as well.
							ScanMethodBodyForFieldAccess (setMethod.Body, write: true, out backingFieldFromSetter);
						}

						if (annotatedMethods.Any (a => a.Method == setMethod)) {
							_context.LogWarning ($"Trying to propagate DynamicallyAccessedMemberAttribute from property '{property.FullName}' to its setter '{setMethod}', but it already has such attribute on the 'value' parameter.", 2043, setMethod);
						} else {
							int offset = setMethod.HasImplicitThis () ? 1 : 0;
							if (setMethod.Parameters.Count > 0) {
								DynamicallyAccessedMemberTypes[] paramAnnotations = new DynamicallyAccessedMemberTypes[setMethod.Parameters.Count + offset];
								paramAnnotations[offset] = annotation;
								annotatedMethods.Add (new MethodAnnotations (setMethod, paramAnnotations, DynamicallyAccessedMemberTypes.None));
							}
						}
					}

					FieldDefinition backingFieldFromGetter = null;

					// Propagate the annotation to the getter method
					MethodDefinition getMethod = property.GetMethod;
					if (getMethod != null) {

						// TODO: Handle abstract properties - can't do any propagation on the base property, should rely on validation
						//  that overrides also correctly annotate.
						if (getMethod.HasBody) {
							// Look for the compiler generated backing field. If it doesn't work out simply move on. In such case we would still
							// propagate the annotation to the setter/getter and later on when analyzing the setter/getter we will warn
							// that the field (which ever it is) must be annotated as well.
							ScanMethodBodyForFieldAccess (getMethod.Body, write: false, out backingFieldFromGetter);
						}

						if (annotatedMethods.Any (a => a.Method == getMethod)) {
							_context.LogWarning ($"Trying to propagate DynamicallyAccessedMemberAttribute from property '{property.FullName}' to its getter '{getMethod}', but it already has such attribute on the return value.", 2043, getMethod);
						} else {
							annotatedMethods.Add (new MethodAnnotations (getMethod, null, annotation));
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
							_context.LogWarning ($"Trying to propagate DynamicallyAccessedMemberAttribute from property '{property.FullName}' to its field '{backingField}', but it already has such attribute.", 2043, backingField);
						} else {
							annotatedFields.Add (new FieldAnnotation (backingField, annotation));
						}
					}
				}
			}

			return new TypeAnnotations (annotatedMethods.ToArray (), annotatedFields.ToArray ());
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

		static bool IsTypeInterestingForDataflow (TypeReference typeReference)
		{
			// We will accept any System.Type* as interesting to enable testing
			// It's necessary to be able to implement a custom "Type" type in tests to validate
			// the correct propagation of the annotations on "this" parameter. And tests can't really
			// override System.Type - as it creates too many issues.
			// It also covers TypeBuilder and RuntimeType.
			return typeReference.MetadataType == MetadataType.String ||
				(typeReference.Name.Contains ("Type") && typeReference.Namespace.StartsWith ("System"));
		}

		readonly struct TypeAnnotations
		{
			readonly MethodAnnotations[] _annotatedMethods;
			readonly FieldAnnotation[] _annotatedFields;

			public TypeAnnotations (MethodAnnotations[] annotatedMethods, FieldAnnotation[] annotatedFields)
				=> (_annotatedMethods, _annotatedFields) = (annotatedMethods, annotatedFields);

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
		}

		readonly struct MethodAnnotations
		{
			public readonly MethodDefinition Method;
			public readonly DynamicallyAccessedMemberTypes[] ParameterAnnotations;
			public readonly DynamicallyAccessedMemberTypes ReturnParameterAnnotation;

			public MethodAnnotations (MethodDefinition method, DynamicallyAccessedMemberTypes[] paramAnnotations, DynamicallyAccessedMemberTypes returnParamAnnotations)
				=> (Method, ParameterAnnotations, ReturnParameterAnnotation) = (method, paramAnnotations, returnParamAnnotations);
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
