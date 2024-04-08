// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	// This class only handles static methods (all the unsafe accessors should be static)
	// so there's no problem with forgetting the implicit "this".
#pragma warning disable RS0030 // MethodReference.Parameters is banned

	readonly struct UnsafeAccessorMarker (LinkContext context, MarkStep markStep)
	{
		readonly LinkContext _context = context;
		readonly MarkStep _markStep = markStep;

		// We don't perform method overload resolution based on list of parameters (or return type) for now
		// Mono.Cecil's method resolution is problematic and has bugs. It's also not extensible
		// and we would need that to correctly implement the desired behavior around custom modifiers. So for now we decided to not
		// duplicate the logic to tweak it and will just mark entire method groups.

		public void ProcessUnsafeAccessorMethod (MethodDefinition method)
		{
			if (!method.IsStatic || !method.HasCustomAttributes)
				return;

			foreach (CustomAttribute customAttribute in method.CustomAttributes) {
				if (customAttribute.Constructor.DeclaringType.FullName == "System.Runtime.CompilerServices.UnsafeAccessorAttribute") {
					if (customAttribute.HasConstructorArguments && customAttribute.ConstructorArguments[0].Value is int kindValue) {
						UnsafeAccessorKind kind = (UnsafeAccessorKind) kindValue;
						string? name = null;
						if (customAttribute.HasProperties) {
							foreach (CustomAttributeNamedArgument prop in customAttribute.Properties) {
								if (prop.Name == "Name") {
									name = prop.Argument.Value as string;
									break;
								}
							}
						}

						switch (kind) {
						case UnsafeAccessorKind.Constructor:
							ProcessConstructorAccessor (method, name);
							break;
						case UnsafeAccessorKind.StaticMethod:
							ProcessMethodAccessor (method, name, isStatic: true);
							break;
						case UnsafeAccessorKind.Method:
							ProcessMethodAccessor (method, name, isStatic: false);
							break;
						case UnsafeAccessorKind.StaticField:
							ProcessFieldAccessor (method, name, isStatic: true);
							break;
						case UnsafeAccessorKind.Field:
							ProcessFieldAccessor (method, name, isStatic: false);
							break;
						default:
							break;
						}

						// Intentionally only process the first such attribute
						// if there's more than one runtime will fail on it anyway.
						break;
					}
				}
			}
		}

		void ProcessConstructorAccessor (MethodDefinition method, string? name)
		{
			// A return type is required for a constructor, otherwise
			// we don't know the type to construct.
			// Types should not be parameterized (that is, by-ref).
			// The name is defined by the runtime and should be empty.
			if (method.ReturnsVoid () || method.ReturnType.IsByRefOrPointer () || !string.IsNullOrEmpty (name))
				return;

			if (_context.TryResolve (method.ReturnType) is not TypeDefinition targetType)
				return;

			foreach (MethodDefinition targetMethod in targetType.Methods) {
				if (!targetMethod.IsConstructor || targetMethod.IsStatic)
					continue;

				_markStep.MarkMethodVisibleToReflection (targetMethod, new DependencyInfo (DependencyKind.UnsafeAccessorTarget, method), new MessageOrigin (method));
			}
		}

		void ProcessMethodAccessor (MethodDefinition method, string? name, bool isStatic)
		{
			// Method access requires a target type.
			if (method.Parameters.Count == 0)
				return;

			if (string.IsNullOrEmpty (name))
				name = method.Name;

			TypeReference targetTypeReference = method.Parameters[0].ParameterType;
			if (_context.TryResolve (targetTypeReference) is not TypeDefinition targetType)
				return;

			if (!isStatic && targetType.IsValueType && !targetTypeReference.IsByReference)
				return;

			foreach (MethodDefinition targetMethod in targetType.Methods) {
				if (targetMethod.Name != name || targetMethod.IsStatic != isStatic)
					continue;

				_markStep.MarkMethodVisibleToReflection (targetMethod, new DependencyInfo (DependencyKind.UnsafeAccessorTarget, method), new MessageOrigin (method));
			}
		}

		void ProcessFieldAccessor (MethodDefinition method, string? name, bool isStatic)
		{
			// Field access requires exactly one parameter
			if (method.Parameters.Count != 1)
				return;

			if (string.IsNullOrEmpty (name))
				name = method.Name;

			if (!method.ReturnType.IsByReference)
				return;

			TypeReference targetTypeReference = method.Parameters[0].ParameterType;
			if (_context.TryResolve (targetTypeReference) is not TypeDefinition targetType)
				return;

			if (!isStatic && targetType.IsValueType && !targetTypeReference.IsByReference)
				return;

			foreach (FieldDefinition targetField in targetType.Fields) {
				if (targetField.Name != name || targetField.IsStatic != isStatic)
					continue;

				_markStep.MarkFieldVisibleToReflection (targetField, new DependencyInfo (DependencyKind.UnsafeAccessorTarget, method), new MessageOrigin (method));
			}
		}
	}
}
