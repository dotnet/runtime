// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public sealed class RequiresDynamicCodeAnalyzer : RequiresAnalyzerBase
	{
		const string RequiresDynamicCodeAttribute = nameof (RequiresDynamicCodeAttribute);
		public const string FullyQualifiedRequiresDynamicCodeAttribute = "System.Diagnostics.CodeAnalysis." + RequiresDynamicCodeAttribute;

		static readonly DiagnosticDescriptor s_requiresDynamicCodeRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresDynamicCode);
		static readonly DiagnosticDescriptor s_requiresDynamicCodeAttributeMismatch = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresDynamicCodeAttributeMismatch);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create (s_requiresDynamicCodeRule, s_requiresDynamicCodeAttributeMismatch);

		private protected override string RequiresAttributeName => RequiresDynamicCodeAttribute;

		private protected override string RequiresAttributeFullyQualifiedName => FullyQualifiedRequiresDynamicCodeAttribute;

		private protected override DiagnosticTargets AnalyzerDiagnosticTargets => DiagnosticTargets.MethodOrConstructor | DiagnosticTargets.Class;

		private protected override DiagnosticDescriptor RequiresDiagnosticRule => s_requiresDynamicCodeRule;

		private protected override DiagnosticDescriptor RequiresAttributeMismatch => s_requiresDynamicCodeAttributeMismatch;

		protected override bool IsAnalyzerEnabled (AnalyzerOptions options, Compilation compilation) =>
			options.IsMSBuildPropertyValueTrue (MSBuildPropertyOptionNames.EnableAOTAnalyzer, compilation);

		protected override bool VerifyAttributeArguments (AttributeData attribute) =>
			attribute.ConstructorArguments.Length >= 1 && attribute.ConstructorArguments[0] is { Type: { SpecialType: SpecialType.System_String } } ctorArg;

		protected override string GetMessageFromAttribute (AttributeData? requiresAttribute)
		{
			var message = (string) requiresAttribute!.ConstructorArguments[0].Value!;
			return MessageFormat.FormatRequiresAttributeMessageArg (message);
		}
	}
}
