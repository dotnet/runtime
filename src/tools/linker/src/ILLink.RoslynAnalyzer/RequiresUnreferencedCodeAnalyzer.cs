// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public class RequiresUnreferencedCodeAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "IL2026";
		const string RequiresUnreferencedCodeAttribute = nameof (RequiresUnreferencedCodeAttribute);
		const string FullyQualifiedRequiresUnreferencedCodeAttribute = "System.Diagnostics.CodeAnalysis." + RequiresUnreferencedCodeAttribute;

		static readonly DiagnosticDescriptor s_requiresUnreferencedCodeRule = new DiagnosticDescriptor (
			DiagnosticId,
			new LocalizableResourceString (nameof (Resources.RequiresUnreferencedCodeTitle),
			Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.RequiresUnreferencedCodeMessage),
			Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (s_requiresUnreferencedCodeRule);

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);

			context.RegisterCompilationStartAction (context => {
				var compilation = context.Compilation;

				var isTrimAnalyzerEnabled = context.Options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.EnableTrimAnalyzer, compilation);
				if (!string.Equals (isTrimAnalyzerEnabled?.Trim (), "true", StringComparison.OrdinalIgnoreCase)) {
					return;
				}

				context.RegisterOperationAction (operationContext => {
					var call = (IInvocationOperation) operationContext.Operation;
					CheckMethodOrCtorCall (operationContext, call.TargetMethod);
				}, OperationKind.Invocation);

				context.RegisterOperationAction (operationContext => {
					var call = (IObjectCreationOperation) operationContext.Operation;
					CheckMethodOrCtorCall (operationContext, call.Constructor);
				}, OperationKind.ObjectCreation);

				context.RegisterOperationAction (operationContext => {
					var fieldAccess = (IFieldReferenceOperation) operationContext.Operation;
					if (fieldAccess.Field.ContainingType is INamedTypeSymbol { StaticConstructors: var ctors } &&
						!SymbolEqualityComparer.Default.Equals (operationContext.ContainingSymbol.ContainingType, fieldAccess.Field.ContainingType)) {
						CheckStaticConstructors (operationContext, ctors);
					}
				}, OperationKind.FieldReference);

				context.RegisterOperationAction (operationContext => {
					var propAccess = (IPropertyReferenceOperation) operationContext.Operation;
					var prop = propAccess.Property;
					var usageInfo = propAccess.GetValueUsageInfo (prop);
					if (usageInfo.HasFlag (ValueUsageInfo.Read) && prop.GetMethod != null)
						CheckMethodOrCtorCall (operationContext, prop.GetMethod);

					if (usageInfo.HasFlag (ValueUsageInfo.Write) && prop.SetMethod != null)
						CheckMethodOrCtorCall (operationContext, prop.SetMethod);
				}, OperationKind.PropertyReference);

				static void CheckStaticConstructors (OperationAnalysisContext operationContext,
					ImmutableArray<IMethodSymbol> constructors)
				{
					foreach (var constructor in constructors) {
						if (constructor.Parameters.Length == 0 && constructor.HasAttribute (RequiresUnreferencedCodeAttribute) && constructor.MethodKind == MethodKind.StaticConstructor) {
							if (constructor.TryGetAttributeWithMessageOnCtor (FullyQualifiedRequiresUnreferencedCodeAttribute, out AttributeData? requiresUnreferencedCode)) {
								operationContext.ReportDiagnostic (Diagnostic.Create (
									s_requiresUnreferencedCodeRule,
									operationContext.Operation.Syntax.GetLocation (),
									constructor.ToString (),
									(string) requiresUnreferencedCode!.ConstructorArguments[0].Value!,
									requiresUnreferencedCode!.NamedArguments.FirstOrDefault (na => na.Key == "Url").Value.Value?.ToString ()));
							}
						}
					}
				}

				static void CheckMethodOrCtorCall (
					OperationAnalysisContext operationContext,
					IMethodSymbol method)
				{
					// If parent method contains RequiresUnreferencedCodeAttribute then we shouldn't report diagnostics for this method
					if (operationContext.ContainingSymbol is IMethodSymbol &&
						operationContext.ContainingSymbol.HasAttribute (RequiresUnreferencedCodeAttribute))
						return;

					// If calling an instance constructor, check first for any static constructor since it will be called implicitly
					if (method.ContainingType is { } containingType && operationContext.Operation is IObjectCreationOperation)
						CheckStaticConstructors (operationContext, containingType.StaticConstructors);

					if (!method.HasAttribute (RequiresUnreferencedCodeAttribute))
						return;

					// Warn on the most derived base method taking into account covariant returns
					while (method.OverriddenMethod != null && SymbolEqualityComparer.Default.Equals (method.ReturnType, method.OverriddenMethod.ReturnType))
						method = method.OverriddenMethod;

					if (method.TryGetAttributeWithMessageOnCtor (FullyQualifiedRequiresUnreferencedCodeAttribute, out AttributeData? requiresUnreferencedCode)) {
						operationContext.ReportDiagnostic (Diagnostic.Create (
							s_requiresUnreferencedCodeRule,
							operationContext.Operation.Syntax.GetLocation (),
							method.OriginalDefinition.ToString (),
							(string) requiresUnreferencedCode!.ConstructorArguments[0].Value!,
							requiresUnreferencedCode!.NamedArguments.FirstOrDefault (na => na.Key == "Url").Value.Value?.ToString ()));
					}
				}
			});
		}
	}
}
