// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public sealed class RequiresUnreferencedCodeAnalyzer : RequiresAnalyzerBase
	{
		public const string IL2026 = nameof (IL2026);
		const string RequiresUnreferencedCodeAttribute = nameof (RequiresUnreferencedCodeAttribute);
		public const string FullyQualifiedRequiresUnreferencedCodeAttribute = "System.Diagnostics.CodeAnalysis." + RequiresUnreferencedCodeAttribute;

		static readonly DiagnosticDescriptor s_requiresUnreferencedCodeRule = new DiagnosticDescriptor (
			IL2026,
			new LocalizableResourceString (nameof (Resources.RequiresUnreferencedCodeTitle),
			Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.RequiresUnreferencedCodeMessage),
			Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (s_requiresUnreferencedCodeRule);

		private protected override string RequiresAttributeName => RequiresUnreferencedCodeAttribute;

		private protected override string RequiresAttributeFullyQualifiedName => FullyQualifiedRequiresUnreferencedCodeAttribute;

		private protected override DiagnosticTargets AnalyzerDiagnosticTargets => DiagnosticTargets.MethodOrConstructor;

		private protected override DiagnosticDescriptor RequiresDiagnosticRule => s_requiresUnreferencedCodeRule;

		protected override bool IsAnalyzerEnabled (AnalyzerOptions options, Compilation compilation)
		{
			var isTrimAnalyzerEnabled = options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.EnableTrimAnalyzer, compilation);
			if (!string.Equals (isTrimAnalyzerEnabled?.Trim (), "true", StringComparison.OrdinalIgnoreCase))
				return false;
			return true;
		}

		protected override bool VerifyAttributeArguments (AttributeData attribute) =>
			attribute.ConstructorArguments.Length >= 1 && attribute.ConstructorArguments[0] is { Type: { SpecialType: SpecialType.System_String } } ctorArg;

		protected override string GetMessageFromAttribute (AttributeData? requiresAttribute)
		{
			var message = (string) requiresAttribute!.ConstructorArguments[0].Value!;
			if (!string.IsNullOrEmpty (message))
				message = $" {message}{(message.TrimEnd ().EndsWith (".") ? "" : ".")}";

			return message;
		}
	}
}
