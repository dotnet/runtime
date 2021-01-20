// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer
{
	/// <summary>
	/// IL3000, IL3001: Do not use Assembly file path in single-file publish
	/// </summary>
	[DiagnosticAnalyzer (LanguageNames.CSharp, LanguageNames.VisualBasic)]
	public sealed class AvoidAssemblyLocationInSingleFile : DiagnosticAnalyzer
	{
		public const string IL3000 = nameof (IL3000);
		public const string IL3001 = nameof (IL3001);

		static readonly DiagnosticDescriptor s_locationRule = new DiagnosticDescriptor (
			IL3000,
			new LocalizableResourceString (nameof (Resources.AvoidAssemblyLocationInSingleFileTitle),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.AvoidAssemblyLocationInSingleFileMessage),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.SingleFile,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			helpLinkUri: "https://docs.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/il3000");

		static readonly DiagnosticDescriptor s_getFilesRule = new DiagnosticDescriptor (
			IL3001,
			new LocalizableResourceString (nameof (Resources.AvoidAssemblyGetFilesInSingleFileTitle),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.AvoidAssemblyGetFilesInSingleFileMessage),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.SingleFile,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			helpLinkUri: "https://docs.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/il3001");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (s_locationRule, s_getFilesRule);

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);

			context.RegisterCompilationStartAction (context => {
				var compilation = context.Compilation;

				var isSingleFilePublish = context.Options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.PublishSingleFile, compilation);
				if (!string.Equals (isSingleFilePublish?.Trim (), "true", StringComparison.OrdinalIgnoreCase)) {
					return;
				}
				var includesAllContent = context.Options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.IncludeAllContentForSelfExtract, compilation);
				if (string.Equals (includesAllContent?.Trim (), "true", StringComparison.OrdinalIgnoreCase)) {
					return;
				}

				var propertiesBuilder = ImmutableArray.CreateBuilder<IPropertySymbol> ();
				var methodsBuilder = ImmutableArray.CreateBuilder<IMethodSymbol> ();

				var assemblyType = compilation.GetTypeByMetadataName ("System.Reflection.Assembly");
				if (assemblyType != null) {
					// properties
					AddIfNotNull (propertiesBuilder, TryGetSingleSymbol<IPropertySymbol> (assemblyType.GetMembers ("Location")));

					// methods
					methodsBuilder.AddRange (assemblyType.GetMembers ("GetFile").OfType<IMethodSymbol> ());
					methodsBuilder.AddRange (assemblyType.GetMembers ("GetFiles").OfType<IMethodSymbol> ());
				}

				var assemblyNameType = compilation.GetTypeByMetadataName ("System.Reflection.AssemblyName");
				if (assemblyNameType != null) {
					AddIfNotNull (propertiesBuilder, TryGetSingleSymbol<IPropertySymbol> (assemblyNameType.GetMembers ("CodeBase")));
					AddIfNotNull (propertiesBuilder, TryGetSingleSymbol<IPropertySymbol> (assemblyNameType.GetMembers ("EscapedCodeBase")));
				}

				var properties = propertiesBuilder.ToImmutable ();
				var methods = methodsBuilder.ToImmutable ();

				context.RegisterOperationAction (operationContext => {
					var access = (IPropertyReferenceOperation) operationContext.Operation;
					var property = access.Property;
					if (!Contains (properties, property, SymbolEqualityComparer.Default)) {
						return;
					}

					operationContext.ReportDiagnostic (Diagnostic.Create (s_locationRule, access.Syntax.GetLocation (), property));
				}, OperationKind.PropertyReference);

				context.RegisterOperationAction (operationContext => {
					var invocation = (IInvocationOperation) operationContext.Operation;
					var targetMethod = invocation.TargetMethod;
					if (!Contains (methods, targetMethod, SymbolEqualityComparer.Default)) {
						return;
					}

					operationContext.ReportDiagnostic (Diagnostic.Create (s_getFilesRule, invocation.Syntax.GetLocation (), targetMethod));
				}, OperationKind.Invocation);

				return;

				static bool Contains<T, TComp> (ImmutableArray<T> list, T elem, TComp comparer)
					where TComp : IEqualityComparer<T>
				{
					foreach (var e in list) {
						if (comparer.Equals (e, elem)) {
							return true;
						}
					}
					return false;
				}

				static TSymbol? TryGetSingleSymbol<TSymbol> (ImmutableArray<ISymbol> members) where TSymbol : class, ISymbol
				{
					TSymbol? candidate = null;
					foreach (var m in members) {
						if (m is TSymbol tsym) {
							if (candidate is null) {
								candidate = tsym;
							} else {
								return null;
							}
						}
					}
					return candidate;
				}

				static void AddIfNotNull<TSymbol> (ImmutableArray<TSymbol>.Builder properties, TSymbol? p) where TSymbol : class, ISymbol
				{
					if (p != null) {
						properties.Add (p);
					}
				}
			});
		}
	}
}
