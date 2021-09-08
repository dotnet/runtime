// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public sealed class RequiresUnreferencedCodeAnalyzer : RequiresAnalyzerBase
	{
		const string RequiresUnreferencedCodeAttribute = nameof (RequiresUnreferencedCodeAttribute);
		public const string FullyQualifiedRequiresUnreferencedCodeAttribute = "System.Diagnostics.CodeAnalysis." + RequiresUnreferencedCodeAttribute;

		static readonly DiagnosticDescriptor s_requiresUnreferencedCodeRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCode);
		static readonly DiagnosticDescriptor s_requiresUnreferencedCodeAttributeMismatch = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCodeAttributeMismatch);
		static readonly DiagnosticDescriptor s_dynamicTypeInvocationRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCode,
			new LocalizableResourceString (nameof (SharedStrings.DynamicTypeInvocationTitle), SharedStrings.ResourceManager, typeof (SharedStrings)),
			new LocalizableResourceString (nameof (SharedStrings.DynamicTypeInvocationMessage), SharedStrings.ResourceManager, typeof (SharedStrings)));

		static readonly Action<OperationAnalysisContext> s_dynamicTypeInvocation = operationContext => {
			if (FindContainingSymbol (operationContext, DiagnosticTargets.All) is ISymbol containingSymbol &&
				containingSymbol.HasAttribute (RequiresUnreferencedCodeAttribute))
				return;

			operationContext.ReportDiagnostic (Diagnostic.Create (s_dynamicTypeInvocationRule,
				operationContext.Operation.Syntax.GetLocation ()));
		};

		[SuppressMessage ("MicrosoftCodeAnalysisPerformance", "RS1008",
			Justification = "Storing per-compilation data inside a diagnostic analyzer might cause stale compilations to remain alive." +
				"This action is registered through a compilation start action, so that instances that register this syntax" +
				" node action will not outlive a compilation's lifetime, avoiding the possibility of the locals stored in" +
				" this function to cause for any stale compilations to remain in memory.")]
		static readonly Action<SyntaxNodeAnalysisContext> s_constructorConstraint = syntaxNodeAnalysisContext => {
			var model = syntaxNodeAnalysisContext.SemanticModel;
			if (syntaxNodeAnalysisContext.ContainingSymbol is not ISymbol containingSymbol || containingSymbol.HasAttribute (RequiresUnreferencedCodeAttribute))
				return;

			GenericNameSyntax genericNameSyntaxNode = (GenericNameSyntax) syntaxNodeAnalysisContext.Node;
			var typeParams = ImmutableArray<ITypeParameterSymbol>.Empty;
			var typeArgs = ImmutableArray<ITypeSymbol>.Empty;
			switch (model.GetSymbolInfo (genericNameSyntaxNode).Symbol) {
			case INamedTypeSymbol typeSymbol:
				typeParams = typeSymbol.TypeParameters;
				typeArgs = typeSymbol.TypeArguments;
				break;

			case IMethodSymbol methodSymbol:
				typeParams = methodSymbol.TypeParameters;
				typeArgs = methodSymbol.TypeArguments;
				break;

			default:
				return;
			}

			for (int i = 0; i < typeParams.Length; i++) {
				var typeParam = typeParams[i];
				var typeArg = typeArgs[i];
				if (!typeParam.HasConstructorConstraint)
					continue;

				var typeArgCtors = ((INamedTypeSymbol) typeArg).InstanceConstructors;
				foreach (var instanceCtor in typeArgCtors) {
					if (instanceCtor.Arity > 0)
						continue;

					if (instanceCtor.TryGetAttribute (RequiresUnreferencedCodeAttribute, out var requiresUnreferencedCodeAttribute)) {
						syntaxNodeAnalysisContext.ReportDiagnostic (Diagnostic.Create (s_requiresUnreferencedCodeRule,
							syntaxNodeAnalysisContext.Node.GetLocation (),
							containingSymbol.GetDisplayName (),
							(string) requiresUnreferencedCodeAttribute.ConstructorArguments[0].Value!,
							GetUrlFromAttribute (requiresUnreferencedCodeAttribute)));
					}
				}
			}
		};

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create (s_dynamicTypeInvocationRule, s_requiresUnreferencedCodeRule, s_requiresUnreferencedCodeAttributeMismatch);

		private protected override string RequiresAttributeName => RequiresUnreferencedCodeAttribute;

		private protected override string RequiresAttributeFullyQualifiedName => FullyQualifiedRequiresUnreferencedCodeAttribute;

		private protected override DiagnosticTargets AnalyzerDiagnosticTargets => DiagnosticTargets.MethodOrConstructor;

		private protected override DiagnosticDescriptor RequiresDiagnosticRule => s_requiresUnreferencedCodeRule;

		private protected override DiagnosticDescriptor RequiresAttributeMismatch => s_requiresUnreferencedCodeAttributeMismatch;

		protected override bool IsAnalyzerEnabled (AnalyzerOptions options, Compilation compilation) =>
			options.IsMSBuildPropertyValueTrue (MSBuildPropertyOptionNames.EnableTrimAnalyzer, compilation);

		private protected override ImmutableArray<(Action<OperationAnalysisContext> Action, OperationKind[] OperationKind)> ExtraOperationActions =>
			ImmutableArray.Create ((s_dynamicTypeInvocation, new OperationKind[] { OperationKind.DynamicInvocation }));

		private protected override ImmutableArray<(Action<SyntaxNodeAnalysisContext> Action, SyntaxKind[] SyntaxKind)> ExtraSyntaxNodeActions =>
			ImmutableArray.Create ((s_constructorConstraint, new SyntaxKind[] { SyntaxKind.GenericName }));

		protected override bool VerifyAttributeArguments (AttributeData attribute) =>
			attribute.ConstructorArguments.Length >= 1 && attribute.ConstructorArguments[0] is { Type: { SpecialType: SpecialType.System_String } } ctorArg;

		protected override string GetMessageFromAttribute (AttributeData? requiresAttribute)
		{
			var message = (string) requiresAttribute!.ConstructorArguments[0].Value!;
			return MessageFormat.FormatRequiresAttributeMessageArg (message);
		}
	}
}
