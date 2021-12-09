// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	public static class RequiresISymbolExtensions
	{
		// TODO: Consider sharing with linker IsMethodInRequiresUnreferencedCodeScope method
		public static bool IsInRequiresScope (this ISymbol member, string requiresAttribute)
		{
			if (member is ISymbol containingSymbol) {
				if (containingSymbol.HasAttribute (requiresAttribute)
					|| (containingSymbol is not ITypeSymbol &&
						 containingSymbol.ContainingType.HasAttribute (requiresAttribute))) {
					return true;
				}
			}
			if (member is IMethodSymbol { AssociatedSymbol: { } associated } && associated.HasAttribute (requiresAttribute))
				return true;

			return false;
		}
	}
}
