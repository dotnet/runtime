// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
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

		static readonly DiagnosticDescriptor s_requiresDynamicCodeOnStaticCtor = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresDynamicCodeOnStaticConstructor);
		static readonly DiagnosticDescriptor s_requiresDynamicCodeRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresDynamicCode);
		static readonly DiagnosticDescriptor s_requiresDynamicCodeAttributeMismatch = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresDynamicCodeAttributeMismatch);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create (s_requiresDynamicCodeRule, s_requiresDynamicCodeAttributeMismatch, s_requiresDynamicCodeOnStaticCtor);

		private protected override string RequiresAttributeName => RequiresDynamicCodeAttribute;

		internal override string RequiresAttributeFullyQualifiedName => FullyQualifiedRequiresDynamicCodeAttribute;

		private protected override DiagnosticTargets AnalyzerDiagnosticTargets => DiagnosticTargets.MethodOrConstructor | DiagnosticTargets.Class;

		private protected override DiagnosticDescriptor RequiresDiagnosticRule => s_requiresDynamicCodeRule;

		private protected override DiagnosticDescriptor RequiresAttributeMismatch => s_requiresDynamicCodeAttributeMismatch;

		private protected override DiagnosticDescriptor RequiresOnStaticCtor => s_requiresDynamicCodeOnStaticCtor;

		internal override bool IsAnalyzerEnabled (AnalyzerOptions options) =>
			options.IsMSBuildPropertyValueTrue (MSBuildPropertyOptionNames.EnableAotAnalyzer);

		private protected override bool IsRequiresCheck (IPropertySymbol propertySymbol, Compilation compilation) {
			var runtimeFeaturesType = compilation.GetTypeByMetadataName ("System.Runtime.CompilerServices.RuntimeFeature");
			if (runtimeFeaturesType == null)
				return false;

			var isDynamicCodeSupportedProperty = runtimeFeaturesType.GetMembers ("IsDynamicCodeSupported").OfType<IPropertySymbol> ().FirstOrDefault ();
			if (isDynamicCodeSupportedProperty == null)
				return false;

			return SymbolEqualityComparer.Default.Equals (propertySymbol, isDynamicCodeSupportedProperty);
		}

		protected override bool VerifyAttributeArguments (AttributeData attribute) =>
			attribute.ConstructorArguments.Length >= 1 && attribute.ConstructorArguments is [ { Type.SpecialType: SpecialType.System_String }, ..];

		protected override string GetMessageFromAttribute (AttributeData? requiresAttribute)
		{
			var message = (string) requiresAttribute!.ConstructorArguments[0].Value!;
			return MessageFormat.FormatRequiresAttributeMessageArg (message);
		}
	}
}
