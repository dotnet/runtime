// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	public static class IPropertySymbolExtensions
	{
		public static IMethodSymbol? GetGetMethod (this IPropertySymbol property)
		{
			IPropertySymbol? declaringProperty = property;
			IMethodSymbol? getMethod;
			while ((getMethod = declaringProperty.GetMethod) == null) {
				if ((declaringProperty = declaringProperty.OverriddenProperty) == null)
					break;
			}
			return getMethod;
		}

		public static IMethodSymbol? GetSetMethod (this IPropertySymbol property)
		{
			IPropertySymbol? declaringProperty = property;
			IMethodSymbol? setMethod;
			while ((setMethod = declaringProperty.SetMethod) == null) {
				if ((declaringProperty = declaringProperty.OverriddenProperty) == null)
					break;
			}
			return setMethod;
		}
	}
}