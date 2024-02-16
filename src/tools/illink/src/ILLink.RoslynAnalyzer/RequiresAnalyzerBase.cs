// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer
{
	public abstract class RequiresAnalyzerBase : DiagnosticAnalyzer
	{
		private protected abstract string RequiresAttributeName { get; }

		internal abstract string RequiresAttributeFullyQualifiedName { get; }

		private protected abstract DiagnosticTargets AnalyzerDiagnosticTargets { get; }

		private protected abstract DiagnosticDescriptor RequiresDiagnosticRule { get; }

		private protected abstract DiagnosticDescriptor RequiresAttributeMismatch { get; }
		private protected abstract DiagnosticDescriptor RequiresOnStaticCtor { get; }

		private protected virtual ImmutableArray<(Action<SyntaxNodeAnalysisContext> Action, SyntaxKind[] SyntaxKind)> ExtraSyntaxNodeActions { get; } = ImmutableArray<(Action<SyntaxNodeAnalysisContext> Action, SyntaxKind[] SyntaxKind)>.Empty;
		private protected virtual ImmutableArray<(Action<SymbolAnalysisContext> Action, SymbolKind[] SymbolKind)> ExtraSymbolActions { get; } = ImmutableArray<(Action<SymbolAnalysisContext> Action, SymbolKind[] SymbolKind)>.Empty;

		public override void Initialize (AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

			if (!System.Diagnostics.Debugger.IsAttached)
				context.EnableConcurrentExecution ();

			context.RegisterCompilationStartAction (context => {
				var compilation = context.Compilation;
				if (!IsAnalyzerEnabled (context.Options))
					return;

				var incompatibleMembers = GetSpecialIncompatibleMembers (compilation);
				context.RegisterSymbolAction (symbolAnalysisContext => {
					var methodSymbol = (IMethodSymbol) symbolAnalysisContext.Symbol;
					if (methodSymbol.IsStaticConstructor () && methodSymbol.HasAttribute (RequiresAttributeName))
						ReportRequiresOnStaticCtorDiagnostic (symbolAnalysisContext, methodSymbol);
					CheckMatchingAttributesInOverrides (symbolAnalysisContext, methodSymbol);
				}, SymbolKind.Method);

				context.RegisterSymbolAction (symbolAnalysisContext => {
					var typeSymbol = (INamedTypeSymbol) symbolAnalysisContext.Symbol;
					CheckMatchingAttributesInInterfaces (symbolAnalysisContext, typeSymbol);
				}, SymbolKind.NamedType);

				context.RegisterSyntaxNodeAction (syntaxNodeAnalysisContext => {
					var model = syntaxNodeAnalysisContext.SemanticModel;
					if (syntaxNodeAnalysisContext.ContainingSymbol is not ISymbol containingSymbol || containingSymbol.IsInRequiresScope (RequiresAttributeName, out _))
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
						if (!typeParam.HasConstructorConstraint ||
							typeArg is not INamedTypeSymbol { InstanceConstructors: { } typeArgCtors })
							continue;

						foreach (var instanceCtor in typeArgCtors) {
							if (instanceCtor.Arity > 0)
								continue;

							if (instanceCtor.DoesMemberRequire (RequiresAttributeName, out var requiresAttribute) &&
								VerifyAttributeArguments (requiresAttribute)) {
								syntaxNodeAnalysisContext.ReportDiagnostic (Diagnostic.Create (RequiresDiagnosticRule,
									syntaxNodeAnalysisContext.Node.GetLocation (),
									containingSymbol.GetDisplayName (),
									(string) requiresAttribute.ConstructorArguments[0].Value!,
									GetUrlFromAttribute (requiresAttribute)));
							}
						}
					}
				}, SyntaxKind.GenericName);

				foreach (var extraSyntaxNodeAction in ExtraSyntaxNodeActions)
					context.RegisterSyntaxNodeAction (extraSyntaxNodeAction.Action, extraSyntaxNodeAction.SyntaxKind);

				foreach (var extraSymbolAction in ExtraSymbolActions)
					context.RegisterSymbolAction (extraSymbolAction.Action, extraSymbolAction.SymbolKind);

				void CheckMatchingAttributesInOverrides (
					SymbolAnalysisContext symbolAnalysisContext,
					ISymbol member)
				{
					if ((member.IsVirtual || member.IsOverride) && member.TryGetOverriddenMember (out var overriddenMember) && HasMismatchingAttributes (member, overriddenMember))
						ReportMismatchInAttributesDiagnostic (symbolAnalysisContext, member, overriddenMember);
				}

				void CheckMatchingAttributesInInterfaces (
					SymbolAnalysisContext symbolAnalysisContext,
					INamedTypeSymbol type)
				{
					foreach (var memberpair in type.GetMemberInterfaceImplementationPairs ()) {
						if (HasMismatchingAttributes (memberpair.InterfaceMember, memberpair.ImplementationMember)) {
							ReportMismatchInAttributesDiagnostic (symbolAnalysisContext, memberpair.ImplementationMember, memberpair.InterfaceMember, isInterface: true);
						}
					}
				}
			});
		}

		public bool CheckAndCreateRequiresDiagnostic (
			IOperation operation,
			ISymbol member,
			ISymbol containingSymbol,
			ImmutableArray<ISymbol> incompatibleMembers,
			[NotNullWhen (true)] out Diagnostic? diagnostic)
		{
			diagnostic = null;
			// Do not emit any diagnostic if caller is annotated with the attribute too.
			if (containingSymbol.IsInRequiresScope (RequiresAttributeName, out _))
				return false;

			if (CreateSpecialIncompatibleMembersDiagnostic (operation, incompatibleMembers, member, out diagnostic))
				return diagnostic != null;

			// Warn on the most derived base method taking into account covariant returns
			while (member is IMethodSymbol method && method.OverriddenMethod != null && SymbolEqualityComparer.Default.Equals (method.ReturnType, method.OverriddenMethod.ReturnType))
				member = method.OverriddenMethod;

			if (!member.DoesMemberRequire (RequiresAttributeName, out var requiresAttribute))
				return false;

			if (!VerifyAttributeArguments (requiresAttribute))
				return false;

			diagnostic = CreateRequiresDiagnostic (operation, member, requiresAttribute);
			return true;
		}

		[Flags]
		protected enum DiagnosticTargets
		{
			MethodOrConstructor = 0x0001,
			Property = 0x0002,
			Field = 0x0004,
			Event = 0x0008,
			Class = 0x0010,
			All = MethodOrConstructor | Property | Field | Event | Class
		}

		/// <summary>
		/// Finds the symbol of the caller to the current operation, helps to find out the symbol in cases where the operation passes
		/// through a lambda or a local function.
		/// </summary>
		/// <param name="operationContext">Analyzer operation context to retrieve the current operation.</param>
		/// <param name="targets">Scope of the attribute to search for callers.</param>
		/// <returns>The symbol of the caller to the operation</returns>
		protected static ISymbol FindContainingSymbol (OperationAnalysisContext operationContext, DiagnosticTargets targets)
		{
			var parent = operationContext.Operation.Parent;
			while (parent is not null) {
				switch (parent) {
				case IAnonymousFunctionOperation lambda:
					return lambda.Symbol;

				case ILocalFunctionOperation local when targets.HasFlag (DiagnosticTargets.MethodOrConstructor):
					return local.Symbol;

				case IMethodBodyBaseOperation when targets.HasFlag (DiagnosticTargets.MethodOrConstructor):
				case IPropertyReferenceOperation when targets.HasFlag (DiagnosticTargets.Property):
				case IFieldReferenceOperation when targets.HasFlag (DiagnosticTargets.Field):
				case IEventReferenceOperation when targets.HasFlag (DiagnosticTargets.Event):
					return operationContext.ContainingSymbol;

				default:
					parent = parent.Parent;
					break;
				}
			}

			return operationContext.ContainingSymbol;
		}

		/// <summary>
		/// Creates a Requires diagnostic message based on the attribute data and RequiresDiagnosticRule.
		/// </summary>
		/// <param name="operationContext">Analyzer operation context to be able to report the diagnostic.</param>
		/// <param name="member">Information about the member that generated the diagnostic.</param>
		/// <param name="requiresAttribute">Requires attribute data to print attribute arguments.</param>
		private Diagnostic CreateRequiresDiagnostic (IOperation operation, ISymbol member, AttributeData requiresAttribute)
		{
			var message = GetMessageFromAttribute (requiresAttribute);
			var url = GetUrlFromAttribute (requiresAttribute);
			return Diagnostic.Create (
				RequiresDiagnosticRule,
				operation.Syntax.GetLocation (),
				member.GetDisplayName (),
				message,
				url);
		}

		private void ReportRequiresOnStaticCtorDiagnostic (SymbolAnalysisContext symbolAnalysisContext, IMethodSymbol ctor)
		{
			symbolAnalysisContext.ReportDiagnostic (Diagnostic.Create (
				RequiresOnStaticCtor,
				ctor.Locations[0],
				ctor.GetDisplayName ()));
		}

		private void ReportMismatchInAttributesDiagnostic (SymbolAnalysisContext symbolAnalysisContext, ISymbol member, ISymbol baseMember, bool isInterface = false)
		{
			string message = MessageFormat.FormatRequiresAttributeMismatch (member.HasAttribute (RequiresAttributeName), isInterface, RequiresAttributeName, member.GetDisplayName (), baseMember.GetDisplayName ());
			symbolAnalysisContext.ReportDiagnostic (Diagnostic.Create (
				RequiresAttributeMismatch,
				member.Locations[0],
				message));
		}

		private bool HasMismatchingAttributes (ISymbol member1, ISymbol member2)
		{
			bool member1CreatesRequirement = member1.DoesMemberRequire (RequiresAttributeName, out _);
			bool member2CreatesRequirement = member2.DoesMemberRequire (RequiresAttributeName, out _);
			bool member1FulfillsRequirement = member1.IsInRequiresScope (RequiresAttributeName);
			bool member2FulfillsRequirement = member2.IsInRequiresScope (RequiresAttributeName);
			return (member1CreatesRequirement && !member2FulfillsRequirement) || (member2CreatesRequirement && !member1FulfillsRequirement);
		}

		protected abstract string GetMessageFromAttribute (AttributeData requiresAttribute);

		public static string GetUrlFromAttribute (AttributeData? requiresAttribute)
		{
			var url = requiresAttribute?.NamedArguments.FirstOrDefault (na => na.Key == "Url").Value.Value?.ToString ();
			return MessageFormat.FormatRequiresAttributeUrlArg (url);
		}

		/// <summary>
		/// This method verifies that the arguments in an attribute have certain structure.
		/// </summary>
		/// <param name="attribute">Attribute data to compare.</param>
		/// <returns>True if the validation was successfull; otherwise, returns false.</returns>
		protected abstract bool VerifyAttributeArguments (AttributeData attribute);

		/// <summary>
		/// Compares the member against a list of incompatible members, if the member exist in the list then it generates a custom diagnostic declared inside the function.
		/// </summary>
		/// <param name="operationContext">Analyzer operation context.</param>
		/// <param name="specialIncompatibleMembers">List of incompatible members.</param>
		/// <param name="member">Member to compare.</param>
		/// <returns>True if the function generated a diagnostic; otherwise, returns false</returns>
		protected virtual bool CreateSpecialIncompatibleMembersDiagnostic (
			IOperation operation,
			ImmutableArray<ISymbol> specialIncompatibleMembers,
			ISymbol member,
			out Diagnostic? incompatibleMembersDiagnostic)
		{
			incompatibleMembersDiagnostic = null;
			return false;
		}

		/// <summary>
		/// Creates a list of special incompatible members that can be used later on by the analyzer to generate diagnostics
		/// </summary>
		/// <param name="compilation">Compilation to search for members</param>
		/// <returns>A list of special incomptaible members</returns>
		internal virtual ImmutableArray<ISymbol> GetSpecialIncompatibleMembers (Compilation compilation) => default;

		/// <summary>
		/// Verifies that the MSBuild requirements to run the analyzer are fulfilled
		/// </summary>
		/// <param name="options">Analyzer options</param>
		/// <returns>True if the requirements to run the analyzer are met; otherwise, returns false</returns>
		internal abstract bool IsAnalyzerEnabled (AnalyzerOptions options);

		// Check whether a given property serves as a check for the "feature" or "capability" associated with the attribute
		// understood by this analyzer. For now, this is only designed to support checks like
		// RuntimeFeatures.IsDynamicCodeSupported, where a true return value indicates that the feature is supported.
		// This doesn't support more general cases such as:
		// - false return value indicating that a feature is supported
		// - feature settings supplied by the project
		// - custom feature checks defined in library code
		private protected virtual bool IsRequiresCheck (IPropertySymbol propertySymbol, Compilation compilation) => false;

		internal static bool IsAnnotatedFeatureCheck (IPropertySymbol propertySymbol, DataFlowAnalyzerContext dataFlowAnalyzerContext, string featureName)
		{
			// Only respect FeatureCheckAttribute on static boolean properties.
			if (!propertySymbol.IsStatic || propertySymbol.Type.SpecialType != SpecialType.System_Boolean)
				return false;

			ValueSet<string> featureCheckAnnotations = propertySymbol.GetFeatureCheckAnnotations (dataFlowAnalyzerContext.EnabledRequiresAnalyzers);
			return featureCheckAnnotations.Contains (featureName);
		}

		internal bool IsFeatureCheck (IPropertySymbol propertySymbol, DataFlowAnalyzerContext dataFlowAnalyzerContext)
		{
			return IsAnnotatedFeatureCheck (propertySymbol, dataFlowAnalyzerContext, RequiresAttributeFullyQualifiedName)
				|| IsRequiresCheck (propertySymbol, dataFlowAnalyzerContext.Compilation);
		}

		internal bool CheckAndCreateRequiresDiagnostic (
			IOperation operation,
			ISymbol member,
			ISymbol owningSymbol,
			DataFlowAnalyzerContext context,
			FeatureContext featureContext,
			[NotNullWhen (true)] out Diagnostic? diagnostic)
		{
			// Warnings are not emitted if the featureContext says the feature is available.
			if (featureContext.IsEnabled (RequiresAttributeFullyQualifiedName)) {
				diagnostic = null;
				return false;
			}

			ISymbol containingSymbol = operation.FindContainingSymbol (owningSymbol);

			var incompatibleMembers = context.GetSpecialIncompatibleMembers (this);
			return CheckAndCreateRequiresDiagnostic (
				operation,
				member,
				containingSymbol,
				incompatibleMembers,
				out diagnostic);
		}
	}
}
