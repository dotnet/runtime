// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.RoslynAnalyzer;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace ILLink.CodeFix
{
	[ExportCodeFixProvider (LanguageNames.CSharp, Name = nameof (RequiresUnreferencedCodeCodeFixProvider)), Shared]
	public class RequiresDynamicCodeCodeFixProvider : BaseAttributeCodeFixProvider
	{
		public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresDynamicCode));

		public sealed override ImmutableArray<string> FixableDiagnosticIds => SupportedDiagnostics.Select (dd => dd.Id).ToImmutableArray ();

		private protected override LocalizableString CodeFixTitle => new LocalizableResourceString (nameof (Resources.RequiresDynamicCodeCodeFixTitle), Resources.ResourceManager, typeof (Resources));

		private protected override string FullyQualifiedAttributeName => RequiresDynamicCodeAnalyzer.FullyQualifiedRequiresDynamicCodeAttribute;

		private protected override AttributeableParentTargets AttributableParentTargets => AttributeableParentTargets.MethodOrConstructor;

		public sealed override Task RegisterCodeFixesAsync (CodeFixContext context) => BaseRegisterCodeFixesAsync (context);

		protected override SyntaxNode[] GetAttributeArguments (ISymbol? attributableSymbol, ISymbol targetSymbol, SyntaxGenerator syntaxGenerator, Diagnostic diagnostic) =>
			RequiresHelpers.GetAttributeArgumentsForRequires (targetSymbol, syntaxGenerator, HasPublicAccessibility (attributableSymbol));
	}
}
