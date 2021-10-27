// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	public static class ISymbolExtensions
	{
		/// <summary>
		/// Returns true if symbol <see paramref="symbol"/> has an attribute with name <see paramref="attributeName"/>.
		/// </summary>
		internal static bool HasAttribute (this ISymbol symbol, string attributeName)
		{
			foreach (var attr in symbol.GetAttributes ())
				if (attr.AttributeClass?.Name == attributeName)
					return true;

			return false;
		}

		internal static bool TryGetAttribute (this ISymbol member, string attributeName, [NotNullWhen (returnValue: true)] out AttributeData? attribute)
		{
			attribute = null;
			foreach (var attr in member.GetAttributes ()) {
				if (attr.AttributeClass is { } attrClass && attrClass.HasName (attributeName)) {
					attribute = attr;
					return true;
				}
			}

			return false;
		}

		internal static bool TryGetOverriddenMember (this ISymbol? symbol, [NotNullWhen (returnValue: true)] out ISymbol? overridenMember)
		{
			overridenMember = symbol switch {
				IMethodSymbol method => method.OverriddenMethod,
				IPropertySymbol property => property.OverriddenProperty,
				IEventSymbol @event => @event.OverriddenEvent,
				_ => null,
			};
			return overridenMember != null;
		}

		public static string GetDisplayName (this ISymbol symbol)
		{
			var sb = new StringBuilder ();
			switch (symbol) {
			case IFieldSymbol fieldSymbol:
				sb.Append (fieldSymbol.Type);
				sb.Append (" ");
				sb.Append (fieldSymbol.ContainingSymbol.ToDisplayString ());
				sb.Append ("::");
				sb.Append (fieldSymbol.MetadataName);
				break;

			case IParameterSymbol parameterSymbol:
				sb.Append (parameterSymbol.Name);
				break;

			default:
				sb.Append (symbol.ToDisplayString ());
				break;
			}

			return sb.ToString ();
		}

		public static bool IsInterface (this ISymbol symbol)
		{
			if (symbol is not INamedTypeSymbol namedTypeSymbol)
				return false;

			var typeSymbol = namedTypeSymbol as ITypeSymbol;
			return typeSymbol.TypeKind == TypeKind.Interface;
		}

		public static bool IsSubclassOf (this ISymbol symbol, string ns, string type)
		{
			if (symbol is not ITypeSymbol typeSymbol)
				return false;

			while (typeSymbol != null) {
				if (typeSymbol.ContainingNamespace.Name == ns &&
					typeSymbol.ContainingType.Name == type)
					return true;

				typeSymbol = typeSymbol.ContainingType;
			}

			return false;
		}
	}
}
