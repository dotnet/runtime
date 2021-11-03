// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
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
		static readonly DiagnosticDescriptor s_makeGenericTypeRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.MakeGenericType);
		static readonly DiagnosticDescriptor s_makeGenericMethodRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.MakeGenericMethod);

		static readonly Action<OperationAnalysisContext> s_dynamicTypeInvocation = operationContext => {
			if (FindContainingSymbol (operationContext, DiagnosticTargets.All) is ISymbol containingSymbol &&
				containingSymbol.HasAttribute (RequiresUnreferencedCodeAttribute))
				return;

			operationContext.ReportDiagnostic (Diagnostic.Create (s_dynamicTypeInvocationRule,
				operationContext.Operation.Syntax.GetLocation ()));
		};

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create (s_dynamicTypeInvocationRule, s_makeGenericMethodRule, s_makeGenericTypeRule, s_requiresUnreferencedCodeRule, s_requiresUnreferencedCodeAttributeMismatch);

		private protected override string RequiresAttributeName => RequiresUnreferencedCodeAttribute;

		private protected override string RequiresAttributeFullyQualifiedName => FullyQualifiedRequiresUnreferencedCodeAttribute;

		private protected override DiagnosticTargets AnalyzerDiagnosticTargets => DiagnosticTargets.MethodOrConstructor | DiagnosticTargets.Class;

		private protected override DiagnosticDescriptor RequiresDiagnosticRule => s_requiresUnreferencedCodeRule;

		private protected override DiagnosticDescriptor RequiresAttributeMismatch => s_requiresUnreferencedCodeAttributeMismatch;

		protected override bool IsAnalyzerEnabled (AnalyzerOptions options, Compilation compilation) =>
			options.IsMSBuildPropertyValueTrue (MSBuildPropertyOptionNames.EnableTrimAnalyzer, compilation);

		protected override ImmutableArray<ISymbol> GetSpecialIncompatibleMembers (Compilation compilation)
		{
			var incompatibleMembers = ImmutableArray.CreateBuilder<ISymbol> ();
			var typeType = compilation.GetTypeByMetadataName ("System.Type");
			if (typeType != null) {
				incompatibleMembers.AddRange (typeType.GetMembers ("MakeGenericType").OfType<IMethodSymbol> ());
			}

			var methodInfoType = compilation.GetTypeByMetadataName ("System.Reflection.MethodInfo");
			if (methodInfoType != null) {
				incompatibleMembers.AddRange (methodInfoType.GetMembers ("MakeGenericMethod").OfType<IMethodSymbol> ());
			}

			return incompatibleMembers.ToImmutable ();
		}

		protected override bool ReportSpecialIncompatibleMembersDiagnostic (OperationAnalysisContext operationContext, ImmutableArray<ISymbol> specialIncompatibleMembers, ISymbol member)
		{
			if (member is IMethodSymbol method && ImmutableArrayOperations.Contains (specialIncompatibleMembers, member, SymbolEqualityComparer.Default) &&
				(method.Name == "MakeGenericMethod" || method.Name == "MakeGenericType")) {
				// These two RUC-annotated APIs are intrinsically handled by the trimmer, which will not produce any
				// RUC warning related to them. For unrecognized reflection patterns realted to generic type/method
				// creation IL2055/IL2060 should be used instead.
				return true;
			}

			return false;
		}

		private protected override ImmutableArray<(Action<OperationAnalysisContext> Action, OperationKind[] OperationKind)> ExtraOperationActions =>
				ImmutableArray.Create ((s_dynamicTypeInvocation, new OperationKind[] { OperationKind.DynamicInvocation }));

		protected override bool VerifyAttributeArguments (AttributeData attribute) =>
			attribute.ConstructorArguments.Length >= 1 && attribute.ConstructorArguments[0] is { Type: { SpecialType: SpecialType.System_String } } ctorArg;

		protected override string GetMessageFromAttribute (AttributeData? requiresAttribute)
		{
			var message = (string) requiresAttribute!.ConstructorArguments[0].Value!;
			return MessageFormat.FormatRequiresAttributeMessageArg (message);
		}
	}
}
