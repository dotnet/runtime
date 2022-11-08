// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public sealed class RequiresUnreferencedCodeAnalyzer : RequiresAnalyzerBase
	{
		public const string RequiresUnreferencedCodeAttribute = nameof (RequiresUnreferencedCodeAttribute);
		public const string FullyQualifiedRequiresUnreferencedCodeAttribute = "System.Diagnostics.CodeAnalysis." + RequiresUnreferencedCodeAttribute;

		static readonly DiagnosticDescriptor s_requiresUnreferencedCodeRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCode);
		static readonly DiagnosticDescriptor s_requiresUnreferencedCodeAttributeMismatch = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCodeAttributeMismatch);
		static readonly DiagnosticDescriptor s_dynamicTypeInvocationRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCode,
			new LocalizableResourceString (nameof (SharedStrings.DynamicTypeInvocationTitle), SharedStrings.ResourceManager, typeof (SharedStrings)),
			new LocalizableResourceString (nameof (SharedStrings.DynamicTypeInvocationMessage), SharedStrings.ResourceManager, typeof (SharedStrings)));
		static readonly DiagnosticDescriptor s_makeGenericTypeRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.MakeGenericType);
		static readonly DiagnosticDescriptor s_makeGenericMethodRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.MakeGenericMethod);
		static readonly DiagnosticDescriptor s_requiresUnreferencedCodeOnStaticCtor = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCodeOnStaticConstructor);

		static readonly DiagnosticDescriptor s_typeDerivesFromRucClassRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCodeOnBaseClass);

		static readonly Action<OperationAnalysisContext> s_dynamicTypeInvocation = operationContext => {
			if (FindContainingSymbol (operationContext, DiagnosticTargets.All) is ISymbol containingSymbol &&
				containingSymbol.IsInRequiresScope (RequiresUnreferencedCodeAttribute))
				return;

			operationContext.ReportDiagnostic (Diagnostic.Create (s_dynamicTypeInvocationRule,
				operationContext.Operation.Syntax.GetLocation ()));
		};

		private Action<SymbolAnalysisContext> typeDerivesFromRucBase {
			get {
				return symbolAnalysisContext => {
					if (symbolAnalysisContext.Symbol is INamedTypeSymbol typeSymbol && !typeSymbol.HasAttribute (RequiresUnreferencedCodeAttribute)
						&& typeSymbol.BaseType is INamedTypeSymbol baseType
						&& baseType.TryGetAttribute (RequiresUnreferencedCodeAttribute, out var requiresUnreferencedCodeAttribute)) {
						var diag = Diagnostic.Create (s_typeDerivesFromRucClassRule,
							typeSymbol.Locations[0],
							typeSymbol,
							baseType.GetDisplayName (),
							GetMessageFromAttribute (requiresUnreferencedCodeAttribute),
							GetUrlFromAttribute (requiresUnreferencedCodeAttribute));
						symbolAnalysisContext.ReportDiagnostic (diag);
					}
				};
			}
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create (s_dynamicTypeInvocationRule, s_makeGenericMethodRule, s_makeGenericTypeRule, s_requiresUnreferencedCodeRule, s_requiresUnreferencedCodeAttributeMismatch, s_typeDerivesFromRucClassRule, s_requiresUnreferencedCodeOnStaticCtor);

		private protected override string RequiresAttributeName => RequiresUnreferencedCodeAttribute;

		private protected override string RequiresAttributeFullyQualifiedName => FullyQualifiedRequiresUnreferencedCodeAttribute;

		private protected override DiagnosticTargets AnalyzerDiagnosticTargets => DiagnosticTargets.MethodOrConstructor | DiagnosticTargets.Class;

		private protected override DiagnosticDescriptor RequiresDiagnosticRule => s_requiresUnreferencedCodeRule;

		private protected override DiagnosticDescriptor RequiresAttributeMismatch => s_requiresUnreferencedCodeAttributeMismatch;

		private protected override DiagnosticDescriptor RequiresOnStaticCtor => s_requiresUnreferencedCodeOnStaticCtor;

		protected override bool IsAnalyzerEnabled (AnalyzerOptions options, Compilation compilation) =>
			options.IsMSBuildPropertyValueTrue (MSBuildPropertyOptionNames.EnableTrimAnalyzer, compilation);

		protected override bool ReportSpecialIncompatibleMembersDiagnostic (OperationAnalysisContext operationContext, ImmutableArray<ISymbol> specialIncompatibleMembers, ISymbol member)
		{
			// Some RUC-annotated APIs are intrinsically handled by the trimmer
			if (member is IMethodSymbol method && Intrinsics.GetIntrinsicIdForMethod (new MethodProxy (method)) != IntrinsicId.None) {
				return true;
			}

			return false;
		}
		private protected override ImmutableArray<(Action<SymbolAnalysisContext> Action, SymbolKind[] SymbolKind)> ExtraSymbolActions =>
			ImmutableArray.Create<(Action<SymbolAnalysisContext> Action, SymbolKind[] SymbolKind)> ((typeDerivesFromRucBase, new SymbolKind[] { SymbolKind.NamedType }));



		private protected override ImmutableArray<(Action<OperationAnalysisContext> Action, OperationKind[] OperationKind)> ExtraOperationActions =>
				ImmutableArray.Create ((s_dynamicTypeInvocation, new OperationKind[] { OperationKind.DynamicInvocation }));

		protected override bool VerifyAttributeArguments (AttributeData attribute) =>
			RequiresUnreferencedCodeUtils.VerifyRequiresUnreferencedCodeAttributeArguments (attribute);

		protected override string GetMessageFromAttribute (AttributeData? requiresAttribute) =>
			RequiresUnreferencedCodeUtils.GetMessageFromAttribute (requiresAttribute);
	}
}
