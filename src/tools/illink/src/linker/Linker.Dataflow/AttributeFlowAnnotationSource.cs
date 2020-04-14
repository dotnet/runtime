// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Cecil;
using System.Diagnostics;

namespace Mono.Linker.Dataflow
{
	class AttributeFlowAnnotationSource : IFlowAnnotationSource
	{
		public DynamicallyAccessedMemberKinds GetFieldAnnotation (FieldDefinition field)
		{
			return Get (field);
		}

		public DynamicallyAccessedMemberKinds GetParameterAnnotation (MethodDefinition method, int index)
		{
			return Get (method.Parameters [index]);
		}

		public DynamicallyAccessedMemberKinds GetPropertyAnnotation (PropertyDefinition property)
		{
			return Get (property);
		}

		public DynamicallyAccessedMemberKinds GetReturnParameterAnnotation (MethodDefinition method)
		{
			return Get (method.MethodReturnType);
		}

		public DynamicallyAccessedMemberKinds GetThisParameterAnnotation (MethodDefinition method)
		{
			// We take the annotation from the attribute on the method itself for "this"
			return Get (method);
		}

		static bool IsDynamicallyAccessedMembersAttribute (CustomAttribute attribute)
		{
			var attributeType = attribute.AttributeType;
			return attributeType.Name == "DynamicallyAccessedMembersAttribute" && attributeType.Namespace == "System.Runtime.CompilerServices";
		}

		static DynamicallyAccessedMemberKinds GetFromAttribute (CustomAttribute attribute)
		{
			Debug.Assert (IsDynamicallyAccessedMembersAttribute (attribute));

			if (attribute.HasConstructorArguments) {
				return (DynamicallyAccessedMemberKinds)(int)attribute.ConstructorArguments [0].Value;
			}

			return 0;
		}

		static DynamicallyAccessedMemberKinds Get (ICustomAttributeProvider attributeProvider)
		{
			if (!attributeProvider.HasCustomAttributes)
				return 0;

			foreach (var attribute in attributeProvider.CustomAttributes) {
				if (IsDynamicallyAccessedMembersAttribute (attribute)) {
					return GetFromAttribute (attribute);
				}
			}

			return 0;
		}
	}
}
