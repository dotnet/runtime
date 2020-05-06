// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace Mono.Linker.Dataflow
{
	class AttributeFlowAnnotationSource : IFlowAnnotationSource
	{
		public DynamicallyAccessedMemberTypes GetFieldAnnotation (FieldDefinition field)
		{
			return Get (field);
		}

		public DynamicallyAccessedMemberTypes GetParameterAnnotation (MethodDefinition method, int index)
		{
			return Get (method.Parameters[index]);
		}

		public DynamicallyAccessedMemberTypes GetPropertyAnnotation (PropertyDefinition property)
		{
			return Get (property);
		}

		public DynamicallyAccessedMemberTypes GetReturnParameterAnnotation (MethodDefinition method)
		{
			return Get (method.MethodReturnType);
		}

		public DynamicallyAccessedMemberTypes GetThisParameterAnnotation (MethodDefinition method)
		{
			// We take the annotation from the attribute on the method itself for "this"
			return Get (method);
		}

		static bool IsDynamicallyAccessedMembersAttribute (CustomAttribute attribute)
		{
			var attributeType = attribute.AttributeType;
			return attributeType.Name == "DynamicallyAccessedMembersAttribute" && attributeType.Namespace == "System.Diagnostics.CodeAnalysis";
		}

		static DynamicallyAccessedMemberTypes GetFromAttribute (CustomAttribute attribute)
		{
			Debug.Assert (IsDynamicallyAccessedMembersAttribute (attribute));

			if (attribute.HasConstructorArguments) {
				return (DynamicallyAccessedMemberTypes) (int) attribute.ConstructorArguments[0].Value;
			}

			return DynamicallyAccessedMemberTypes.None;
		}

		static DynamicallyAccessedMemberTypes Get (ICustomAttributeProvider attributeProvider)
		{
			if (!attributeProvider.HasCustomAttributes)
				return DynamicallyAccessedMemberTypes.None;

			foreach (var attribute in attributeProvider.CustomAttributes) {
				if (IsDynamicallyAccessedMembersAttribute (attribute)) {
					return GetFromAttribute (attribute);
				}
			}

			return DynamicallyAccessedMemberTypes.None;
		}
	}
}
