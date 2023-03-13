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

			// Also check the containing type
			if (member.IsStatic || member.IsConstructor ())
				return member.ContainingType.TryGetAttribute (requiresAttribute, out requiresAttributeData);

			return false;
		}

		// TODO: Consider sharing with ILLink IsInRequiresScope method
		/// <summary>
		/// True if the source of a call is considered to be annotated with the Requires... attribute
		/// </summary>
		public static bool IsInRequiresScope (this ISymbol member, string attributeName, [NotNullWhen (true)] out AttributeData? requiresAttribute)
		{
			return member.IsInRequiresScope (attributeName, true, out requiresAttribute);
		}

		/// <summary>
		/// True if member of a call is considered to be annotated with the Requires... attribute.
		/// Doesn't check the associated symbol for overrides and virtual methods because the analyzer should warn on mismatched between the property AND the accessors
		/// </summary>
		/// <param name="member">
		/// Symbol that is either an overriding member or an overriden/virtual member
		/// </param>
		public static bool IsOverrideInRequiresScope (this ISymbol member, string requiresAttribute)
		{
			return member.IsInRequiresScope (requiresAttribute, false, out _);
		}

		private static bool IsInRequiresScope (this ISymbol member, string attributeName, bool checkAssociatedSymbol, [NotNullWhen (true)] out AttributeData? requiresAttribute)
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

			// Only check associated symbol if not override or virtual method
			if (checkAssociatedSymbol && member is IMethodSymbol { AssociatedSymbol: { } associated } && associated.TryGetAttribute (attributeName, out requiresAttribute))
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
