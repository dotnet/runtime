// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ILLink.Shared;
using ILLink.Shared.TypeSystemProxy;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public sealed class RequiresAssemblyFilesAnalyzer : RequiresAnalyzerBase
	{
		private const string RequiresAssemblyFilesAttribute = nameof (RequiresAssemblyFilesAttribute);
		public const string RequiresAssemblyFilesAttributeFullyQualifiedName = "System.Diagnostics.CodeAnalysis." + RequiresAssemblyFilesAttribute;

		static readonly DiagnosticDescriptor s_locationRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.AvoidAssemblyLocationInSingleFile,
			helpLinkUri: "https://docs.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/il3000");

		static readonly DiagnosticDescriptor s_getFilesRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.AvoidAssemblyGetFilesInSingleFile,
			helpLinkUri: "https://docs.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/il3001");

		static readonly DiagnosticDescriptor s_requiresAssemblyFilesRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresAssemblyFiles,
			helpLinkUri: "https://docs.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/il3002");

		static readonly DiagnosticDescriptor s_requiresAssemblyFilesAttributeMismatch = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresAssemblyFilesAttributeMismatch);

		static readonly DiagnosticDescriptor s_requiresAssemblyFilesOnStaticCtor = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresAssemblyFilesOnStaticConstructor);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (s_locationRule, s_getFilesRule, s_requiresAssemblyFilesRule, s_requiresAssemblyFilesAttributeMismatch, s_requiresAssemblyFilesOnStaticCtor);

		private protected override string RequiresAttributeName => RequiresAssemblyFilesAttribute;

		internal override string RequiresAttributeFullyQualifiedName => RequiresAssemblyFilesAttributeFullyQualifiedName;

		private protected override DiagnosticTargets AnalyzerDiagnosticTargets => DiagnosticTargets.MethodOrConstructor | DiagnosticTargets.Property | DiagnosticTargets.Event;

		private protected override DiagnosticDescriptor RequiresDiagnosticRule => s_requiresAssemblyFilesRule;

		private protected override DiagnosticDescriptor RequiresAttributeMismatch => s_requiresAssemblyFilesAttributeMismatch;

		private protected override DiagnosticDescriptor RequiresOnStaticCtor => s_requiresAssemblyFilesOnStaticCtor;

		internal override bool IsAnalyzerEnabled (AnalyzerOptions options)
		{
			var isSingleFileAnalyzerEnabled = options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.EnableSingleFileAnalyzer);
			if (!string.Equals (isSingleFileAnalyzerEnabled?.Trim (), "true", StringComparison.OrdinalIgnoreCase))
				return false;

			var includesAllContent = options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.IncludeAllContentForSelfExtract);
			if (string.Equals (includesAllContent?.Trim (), "true", StringComparison.OrdinalIgnoreCase))
				return false;

			return true;
		}

		private protected override bool IsRequiresCheck (IPropertySymbol propertySymbol, Compilation compilation)
		{
			// "IsAssemblyFilesSupported" is treated as a requires check for testing purposes only, and
			// is not officially-supported product behavior.
			var runtimeFeaturesType = compilation.GetTypeByMetadataName ("ILLink.RoslynAnalyzer.TestFeatures");
			if (runtimeFeaturesType == null)
				return false;

			var isDynamicCodeSupportedProperty = runtimeFeaturesType.GetMembers ("IsAssemblyFilesSupported").OfType<IPropertySymbol> ().FirstOrDefault ();
			if (isDynamicCodeSupportedProperty == null)
				return false;

			return SymbolEqualityComparer.Default.Equals (propertySymbol, isDynamicCodeSupportedProperty);
		}

		internal override ImmutableArray<ISymbol> GetSpecialIncompatibleMembers (Compilation compilation)
		{
			var dangerousPatternsBuilder = ImmutableArray.CreateBuilder<ISymbol> ();

			var assemblyType = compilation.GetTypeByMetadataName ("System.Reflection.Assembly");
			if (assemblyType != null) {
				// Properties
				ImmutableArrayOperations.AddIfNotNull (dangerousPatternsBuilder, ImmutableArrayOperations.TryGetSingleSymbol<IPropertySymbol> (assemblyType.GetMembers ("Location")));

				// Methods
				dangerousPatternsBuilder.AddRange (assemblyType.GetMembers ("GetFile").OfType<IMethodSymbol> ());
				dangerousPatternsBuilder.AddRange (assemblyType.GetMembers ("GetFiles").OfType<IMethodSymbol> ());
			}

			var assemblyNameType = compilation.GetTypeByMetadataName ("System.Reflection.AssemblyName");
			if (assemblyNameType != null) {
				ImmutableArrayOperations.AddIfNotNull (dangerousPatternsBuilder, ImmutableArrayOperations.TryGetSingleSymbol<IPropertySymbol> (assemblyNameType.GetMembers ("CodeBase")));
				ImmutableArrayOperations.AddIfNotNull (dangerousPatternsBuilder, ImmutableArrayOperations.TryGetSingleSymbol<IPropertySymbol> (assemblyNameType.GetMembers ("EscapedCodeBase")));
			}

			return dangerousPatternsBuilder.ToImmutable ();
		}

		protected override bool CreateSpecialIncompatibleMembersDiagnostic (
			IOperation operation,
			ImmutableArray<ISymbol> dangerousPatterns,
			ISymbol member,
			out Diagnostic? diagnostic)
		{
			diagnostic = null;
			if (member is IMethodSymbol method) {
				if (ImmutableArrayOperations.Contains (dangerousPatterns, member, SymbolEqualityComparer.Default)) {
					diagnostic = Diagnostic.Create (s_getFilesRule, operation.Syntax.GetLocation (), member.GetDisplayName ());
					return true;
				}
				else if (method.AssociatedSymbol is ISymbol associatedSymbol &&
					ImmutableArrayOperations.Contains (dangerousPatterns, associatedSymbol, SymbolEqualityComparer.Default)) {
					diagnostic = Diagnostic.Create (s_locationRule, operation.Syntax.GetLocation (), member.GetDisplayName ());
					// The getters for CodeBase and EscapedCodeBase have RAF attribute on them
					// so our caller will produce the RAF warning (IL3002) by default. Since we handle these properties specifically
					// here and produce different warning (IL3000) we don't want the caller to produce IL3002.
					// So we need to return true from here for the getters, to suppress the RAF warning.
					return true;
				}
			}

			return false;
		}

		protected override bool VerifyAttributeArguments (AttributeData attribute) => attribute.ConstructorArguments.Length == 0 ||
			attribute.ConstructorArguments.Length >= 1 && attribute.ConstructorArguments is [ { Type.SpecialType: SpecialType.System_String }, ..];

		protected override string GetMessageFromAttribute (AttributeData requiresAttribute)
		{
			string message = "";
			if (requiresAttribute.ConstructorArguments.Length >= 1) {
				message = requiresAttribute.ConstructorArguments[0].Value?.ToString () ?? "";
				if (!string.IsNullOrEmpty (message))
					message = $" {message}{(message!.TrimEnd ().EndsWith (".") ? "" : ".")}";
			}
			return message;
		}
	}
}
