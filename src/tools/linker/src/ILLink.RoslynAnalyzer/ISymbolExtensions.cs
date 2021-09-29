// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
			foreach (var _attribute in member.GetAttributes ()) {
				if (_attribute.AttributeClass is { } attrClass &&
					attrClass.HasName (attributeName)) {
					attribute = _attribute;
					return true;
				}
			}

			return false;
		}

		internal static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypes (this ISymbol symbol)
		{
			if (!TryGetAttribute (symbol, DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute, out var dynamicallyAccessedMembers))
				return DynamicallyAccessedMemberTypes.None;

			return (DynamicallyAccessedMemberTypes) dynamicallyAccessedMembers!.ConstructorArguments[0].Value!;
		}

		internal static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypesOnReturnType (this IMethodSymbol methodSymbol)
		{
			AttributeData? dynamicallyAccessedMembers = null;
			foreach (var returnTypeAttribute in methodSymbol.GetReturnTypeAttributes ())
				if (returnTypeAttribute.AttributeClass is var attrClass && attrClass != null &&
					attrClass.HasName (DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute)) {
					dynamicallyAccessedMembers = returnTypeAttribute;
					break;
				}

			if (dynamicallyAccessedMembers == null)
				return DynamicallyAccessedMemberTypes.None;

			return (DynamicallyAccessedMemberTypes) dynamicallyAccessedMembers.ConstructorArguments[0].Value!;
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
	}
}
