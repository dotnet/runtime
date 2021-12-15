// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	public static class RequiresUnreferencedCodeUtils
	{
		private const string RequiresUnreferencedCodeAttribute = nameof (RequiresUnreferencedCodeAttribute);

		// TODO: Consider sharing with linker DoesMethodRequireUnreferencedCode method
		/// <summary>
		/// True if the target of a call is considered to be annotated with the RequiresUnreferencedCode attribute
		/// </summary>
		public static bool TargetHasRequiresUnreferencedCodeAttribute (this ISymbol member, [NotNullWhen (returnValue: true)] out AttributeData? requiresAttributeData)
			=> member.TargetHasRequiresAttribute (RequiresUnreferencedCodeAttribute, out requiresAttributeData);

		// TODO: Consider sharing with linker IsMethodInRequiresUnreferencedCodeScope method
		/// <summary>
		/// True if the source of a call is considered to be annotated with the RequiresUnreferencedCode attribute
		/// </summary>
		public static bool IsInRequiresUnreferencedCodeAttributeScope (this ISymbol member)
			=> member.IsInRequiresScope (RequiresUnreferencedCodeAttribute);

		/// <summary>
		/// This method verifies that the arguments in an attribute have certain structure.
		/// </summary>
		/// <param name="attribute">Attribute data to compare.</param>
		/// <returns>True if the validation was successfull; otherwise, returns false.</returns>
		public static bool VerifyRequiresUnreferencedCodeAttributeArguments (AttributeData attribute)
			=> attribute.ConstructorArguments.Length >= 1 && attribute.ConstructorArguments[0] is { Type: { SpecialType: SpecialType.System_String } } ctorArg;
	}
}
