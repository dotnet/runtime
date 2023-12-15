// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	public static class RequiresISymbolExtensions
	{
		// TODO: Consider sharing with ILLink DoesMemberRequire method
		/// <summary>
		/// True if the target of a call is considered to be annotated with the Requires... attribute
		/// </summary>
		public static bool DoesMemberRequire (this ISymbol member, string requiresAttribute, [NotNullWhen (returnValue: true)] out AttributeData? requiresAttributeData)
		{
			requiresAttributeData = null;
			if (!member.IsStaticConstructor () && member.TryGetAttribute (requiresAttribute, out requiresAttributeData))
				return true;

			if (member is IMethodSymbol { AssociatedSymbol: { } associated } && associated.TryGetAttribute (requiresAttribute, out requiresAttributeData))
				return true;

			// Also check the containing type
			if (member.IsStatic || member.IsConstructor ())
				return member.ContainingType.TryGetAttribute (requiresAttribute, out requiresAttributeData);

			return false;
		}

		public static bool IsInRequiresScope (this ISymbol member, string attributeName)
		{
			return member.IsInRequiresScope (attributeName, out _);
		}

		// TODO: Consider sharing with ILLink IsInRequiresScope method
		/// <summary>
		/// True if the source of a call is considered to be annotated with the Requires... attribute
		/// </summary>
		public static bool IsInRequiresScope (this ISymbol member, string attributeName, [NotNullWhen (true)] out AttributeData? requiresAttribute)
		{
			// Requires attribute on a type does not silence warnings that originate
			// from the type directly. We also only check the containing type for members
			// below, not of nested types.
			if (member is ITypeSymbol) {
				requiresAttribute = null;
				return false;
			}

			while (true) {
				if (member.TryGetAttribute (attributeName, out requiresAttribute) && !member.IsStaticConstructor ())
					return true;
				if (member.ContainingSymbol is not IMethodSymbol method)
					break;
				member = method;
			}

			if (member.ContainingType is ITypeSymbol containingType && containingType.TryGetAttribute (attributeName, out requiresAttribute))
				return true;

			if (member is IMethodSymbol { AssociatedSymbol: { } associated } && associated.TryGetAttribute (attributeName, out requiresAttribute))
				return true;

			// When using instance fields suppress the warning if the constructor has already the Requires annotation
			if (member is IFieldSymbol field && !field.IsStatic) {
				foreach (var constructor in field.ContainingType.InstanceConstructors) {
					if (!constructor.TryGetAttribute (attributeName, out requiresAttribute)) {
						requiresAttribute = null;
						return false;
					}
				}
				return requiresAttribute != null;
			}

			requiresAttribute = null;
			return false;
		}
	}
}
