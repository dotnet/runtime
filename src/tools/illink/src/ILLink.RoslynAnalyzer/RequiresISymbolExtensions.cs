// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.RoslynAnalyzer
{
	public static class RequiresISymbolExtensions
	{
		// TODO: Consider sharing with linker IsMethodInRequiresUnreferencedCodeScope method
		/// <summary>
		/// True if the source of a call is considered to be annotated with the Requires... attribute
		/// </summary>
		public static bool IsInRequiresScope (this ISymbol member, string requiresAttribute)
		{
			return member.IsInRequiresScope (requiresAttribute, true);
		}

		/// <summary>
		/// True if member of a call is considered to be annotated with the Requires... attribute.
		/// Doesn't check the associated symbol for overrides and virtual methods because the analyzer should warn on mismatched between the property AND the accessors
		/// </summary>
		/// <param name="containingSymbol">
		///	Symbol that is either an overriding member or an overriden/virtual member
		/// </param>
		public static bool IsOverrideInRequiresScope(this ISymbol member, string requiresAttribute) 
		{
			return member.IsInRequiresScope(requiresAttribute, false);
		}

		private static bool IsInRequiresScope (this ISymbol member, string requiresAttribute, bool checkAssociatedSymbol) 
		{
			if (member is ISymbol containingSymbol) {
				if (containingSymbol.HasAttribute (requiresAttribute)
					|| (containingSymbol is not ITypeSymbol &&
						 containingSymbol.ContainingType.HasAttribute (requiresAttribute))) {
					return true;
				}
			}
			// Only check associated symbol if not override or virtual method
			if (checkAssociatedSymbol && member is IMethodSymbol { AssociatedSymbol: { } associated } && associated.HasAttribute (requiresAttribute))
				return true;

			return false;
		}
	}
}
