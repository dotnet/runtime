// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;

namespace Mono.Linker.Dataflow
{
	// TODO: enforce virtual methods have consistent annotations

	class FlowAnnotations
	{
		readonly IFlowAnnotationSource _source;
		readonly Dictionary<TypeDefinition, TypeAnnotations> _annotations = new Dictionary<TypeDefinition, TypeAnnotations>();

		public FlowAnnotations (IFlowAnnotationSource annotationSource)
		{
			_source = annotationSource;
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
		public DynamicallyAccessedMemberKinds GetParameterAnnotation (MethodDefinition method, int parameterIndex)
		{
			if (GetAnnotations (method.DeclaringType).TryGetAnnotation (method, out var annotation) && annotation.ParameterAnnotations != null) {
				return annotation.ParameterAnnotations [parameterIndex];
			}

			return 0;
		}

		public DynamicallyAccessedMemberKinds GetReturnParameterAnnotation (MethodDefinition method)
		{
			if (GetAnnotations (method.DeclaringType).TryGetAnnotation (method, out var annotation)) {
				return annotation.ReturnParameterAnnotation;
			}

			return 0;
		}

		public DynamicallyAccessedMemberKinds GetFieldAnnotation (FieldDefinition field)
		{
			if (GetAnnotations (field.DeclaringType).TryGetAnnotation (field, out var annotation)) {
				return annotation.Annotation;
			}

			return 0;
		}

		private TypeAnnotations GetAnnotations(TypeDefinition type)
		{
			if (!_annotations.TryGetValue(type, out TypeAnnotations value)) {
				value = BuildTypeAnnotations (type);
				_annotations.Add (type, value);
			}

			return value;
		}

		private TypeAnnotations BuildTypeAnnotations (TypeDefinition type)
		{
			var annotatedFields = new ArrayBuilder<FieldAnnotation> ();

			// First go over all fields with an explicit annotation
			if (type.HasFields) {
				foreach (FieldDefinition field in type.Fields) {
					if (!IsTypeInterestingForDataflow (field.FieldType))
						continue;

					DynamicallyAccessedMemberKinds annotation = _source.GetFieldAnnotation (field);
					if (annotation == 0) {
						continue;
					}

					annotatedFields.Add (new FieldAnnotation (field, annotation));
				}
			}

			var annotatedMethods = new ArrayBuilder<MethodAnnotations> ();

			// Next go over all methods with an explicit annotation
			if (type.HasMethods) {
				foreach (MethodDefinition method in type.Methods) {
					DynamicallyAccessedMemberKinds [] paramAnnotations = null;

					// We convert indices from metadata space to IL space here.
					// IL space assigns index 0 to the `this` parameter on instance methods.
					int offset;
					if (method.HasImplicitThis ()) {
						offset = 1;
						if (IsTypeInterestingForDataflow (method.DeclaringType)) {
							DynamicallyAccessedMemberKinds ta = _source.GetThisParameterAnnotation (method);
							if (ta != 0) {
								paramAnnotations = new DynamicallyAccessedMemberKinds [method.Parameters.Count + offset];
								paramAnnotations [0] = ta;
							}
						}
					}
					else {
						offset = 0;
					}

					for (int i = 0; i < method.Parameters.Count; i++) {						
						if (!IsTypeInterestingForDataflow (method.Parameters [i].ParameterType)) {
							continue;
						}

						DynamicallyAccessedMemberKinds pa = _source.GetParameterAnnotation (method, i);
						if (pa == 0) {
							continue;
						}

						if (paramAnnotations == null) {
							paramAnnotations = new DynamicallyAccessedMemberKinds [method.Parameters.Count + offset];
						}
						paramAnnotations [i + offset] = pa;
					}

					DynamicallyAccessedMemberKinds returnAnnotation = IsTypeInterestingForDataflow(method.ReturnType) ?
						_source.GetReturnParameterAnnotation (method) : 0;
					if (returnAnnotation != 0 || paramAnnotations != null) {
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

					DynamicallyAccessedMemberKinds annotation = _source.GetPropertyAnnotation (property);
					if (annotation == 0) {
						continue;
					}

					FieldDefinition backingFieldFromSetter = null;

					// Propagate the annotation to the setter method
					MethodDefinition setMethod = property.SetMethod;
					if (setMethod != null) {

						// TODO: Handle abstract properties - no way to propagate the annotation to the field
						if (!setMethod.HasBody || !ScanMethodBodyForFieldAccess (setMethod.Body, write: true, out backingFieldFromSetter)) {
							// TODO: warn we couldn't find a unique backing field
						}

						if (annotatedMethods.Any (a => a.Method == setMethod)) {
							// TODO: warn: duplicate annotation. not propagating.
						} else {
							int offset = setMethod.HasImplicitThis () ? 1 : 0;
							if (setMethod.Parameters.Count > 0) {
								DynamicallyAccessedMemberKinds [] paramAnnotations = new DynamicallyAccessedMemberKinds [setMethod.Parameters.Count + offset];
								paramAnnotations [offset] = annotation;
								annotatedMethods.Add (new MethodAnnotations (setMethod, paramAnnotations, 0));
							}
						}
					}

					FieldDefinition backingFieldFromGetter = null;

					// Propagate the annotation to the getter method
					MethodDefinition getMethod = property.GetMethod;
					if (getMethod != null) {

						// TODO: Handle abstract properties - no way to propagate the annotation to the field
						if (!getMethod.HasBody || !ScanMethodBodyForFieldAccess (getMethod.Body, write: false, out backingFieldFromGetter)) {
							// TODO: warn we couldn't find a unique backing field
						}

						if (annotatedMethods.Any (a => a.Method == getMethod)) {
							// TODO: warn: duplicate annotation. not propagating.
						} else {
							annotatedMethods.Add (new MethodAnnotations (getMethod, null, annotation));
						}
					}

					FieldDefinition backingField;
					if (backingFieldFromGetter != null && backingFieldFromSetter != null &&
						backingFieldFromGetter != backingFieldFromSetter) {
						// TODO: warn we couldn't find a unique backing field
						backingField = null;
					} else {
						backingField = backingFieldFromGetter ?? backingFieldFromSetter;
					}

					if (backingField != null) {
						if (annotatedFields.Any (a => a.Field == backingField)) {
							// TODO: warn about duplicate annotations
						} else {
							annotatedFields.Add (new FieldAnnotation (backingField, annotation));
						}
					}
				}
			}

			return new TypeAnnotations (annotatedMethods.ToArray(), annotatedFields.ToArray());
		}

		private bool ScanMethodBodyForFieldAccess (MethodBody body, bool write, out FieldDefinition found)
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

						foundReference = (FieldReference)instruction.Operand;
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
				!found.IsCompilerGenerated()) {
				// A couple heuristics to make sure we got the right field.
				// Return failure.
				found = null;
				return false;
			}

			return true;
		}

		private static bool IsTypeInterestingForDataflow(TypeReference typeReference)
		{
			// We will accept any System.Type* as interesting to enable testing
			// It's necessary to be able to implement a custom "Type" type in tests to validate
			// the correct propagation of the annotations on "this" parameter. And tests can't really
			// override System.Type - as it creates too many issues.
			return (typeReference.Name.StartsWith ("Type") || typeReference.Name == "String") &&
				typeReference.Namespace == "System";
		}

		readonly struct TypeAnnotations
		{
			readonly MethodAnnotations [] _annotatedMethods;
			readonly FieldAnnotation [] _annotatedFields;

			public TypeAnnotations (MethodAnnotations [] annotatedMethods, FieldAnnotation [] annotatedFields)
				=> (_annotatedMethods, _annotatedFields) = (annotatedMethods, annotatedFields);

			public bool TryGetAnnotation(MethodDefinition method, out MethodAnnotations annotations)
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
			public readonly DynamicallyAccessedMemberKinds [] ParameterAnnotations;
			public readonly DynamicallyAccessedMemberKinds ReturnParameterAnnotation;

			public MethodAnnotations (MethodDefinition method, DynamicallyAccessedMemberKinds [] paramAnnotations, DynamicallyAccessedMemberKinds returnParamAnnotations)
				=> (Method, ParameterAnnotations, ReturnParameterAnnotation) = (method, paramAnnotations, returnParamAnnotations);
		}

		readonly struct FieldAnnotation
		{
			public readonly FieldDefinition Field;
			public readonly DynamicallyAccessedMemberKinds Annotation;

			public FieldAnnotation (FieldDefinition field, DynamicallyAccessedMemberKinds annotation)
				=> (Field, Annotation) = (field, annotation);
		}
	}
}
