// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	// This class only handles static methods (all the unsafe accessors should be static)
	// so there's no problem with forgetting the implicit "this".
#pragma warning disable RS0030 // MethodReference.Parameters is banned

	internal struct UnsafeAccessorMarker (LinkContext context, MarkStep markStep)
	{
		readonly LinkContext _context = context;
		readonly MarkStep _markStep = markStep;

		public void ProcessUnsafeAccessorMethod (MethodDefinition method)
		{
			if (!method.IsStatic || !method.HasCustomAttributes)
				return;

			foreach (var customAttribute in method.CustomAttributes) {
				if (customAttribute.Constructor.DeclaringType.FullName == "System.Runtime.CompilerServices.UnsafeAccessorAttribute") {
					if (customAttribute.HasConstructorArguments && customAttribute.ConstructorArguments[0].Value is int kindValue) {
						UnsafeAccessorKind kind = (UnsafeAccessorKind) kindValue;
						string? name = null;
						if (customAttribute.HasProperties) {
							foreach (var prop in customAttribute.Properties) {
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
			// TODO - test specifying name but using empty string
			// A return type is required for a constructor, otherwise
			// we don't know the type to construct.
			// Types should not be parameterized (that is, byref).
			// The name is defined by the runtime and should be empty.
			if (method.ReturnsVoid () || method.ReturnType.IsByRefOrPointer () || !string.IsNullOrEmpty (name))
				return;

			// Construct a method reference with the .ctor signature
			MethodReference targetMethodReference = new MethodReference (".ctor", BCL.FindPredefinedType (WellKnownType.System_Void, _context), method.ReturnType) {
				CallingConvention = method.CallingConvention
			};

			foreach (ParameterDefinition? parameter in method.Parameters) {
				targetMethodReference.Parameters.Add (new ParameterDefinition (parameter.Name, parameter.Attributes, parameter.ParameterType));
			}

			MethodDefinition targetMethodDefinition = ResolveMethodReference (targetMethodReference);
			_markStep.MarkMethodVisibleToReflection (targetMethodDefinition, new DependencyInfo (DependencyKind.ReferencedBySpecialAttribute, method), new MessageOrigin (method));
		}

		static MethodDefinition ResolveMethodReference(MethodReference reference)
		{
			// TODO: Compare calling conventions
			// TODO: Generics
			// TODO: Custom modifiers
			return reference.Module.MetadataResolver.Resolve (reference);
		}
	}
}
