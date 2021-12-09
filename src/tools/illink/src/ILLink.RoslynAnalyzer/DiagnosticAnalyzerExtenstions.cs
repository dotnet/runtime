// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
	static class RoslynDiagnosticAnalyzerExtenstions 
	{
		public static bool IsMemberInRequiresScope(this DiagnosticAnalyzer _, ISymbol member, string requiresAttribute)
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
