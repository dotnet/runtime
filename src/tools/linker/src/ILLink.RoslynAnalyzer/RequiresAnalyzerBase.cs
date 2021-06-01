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
	public abstract class RequiresAnalyzerBase : DiagnosticAnalyzer
	{
		private protected abstract string RequiresAttributeName { get; }

		private protected abstract string RequiresAttributeFullyQualifiedName { get; }

		private protected abstract DiagnosticTargets AnalyzerDiagnosticTargets { get; }

		private protected abstract DiagnosticDescriptor RequiresDiagnosticRule { get; }

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterCompilationStartAction (context => {
				var compilation = context.Compilation;
				if (!IsAnalyzerEnabled (context.Options, compilation))
					return;
				var incompatibleMembers = GetSpecialIncompatibleMembers (compilation);

				context.RegisterOperationAction (operationContext => {
					var methodInvocation = (IInvocationOperation) operationContext.Operation;
					CheckCalledMember (operationContext, methodInvocation.TargetMethod, incompatibleMembers);
				}, OperationKind.Invocation);

				context.RegisterOperationAction (operationContext => {
					var objectCreation = (IObjectCreationOperation) operationContext.Operation;
					var ctor = objectCreation.Constructor;
					if (ctor is not null) {
						CheckCalledMember (operationContext, ctor, incompatibleMembers);
					}
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
						CheckCalledMember (operationContext, prop.GetMethod, incompatibleMembers);

					if (usageInfo.HasFlag (ValueUsageInfo.Write) && prop.SetMethod != null)
						CheckCalledMember (operationContext, prop.SetMethod, incompatibleMembers);

					if (AnalyzerDiagnosticTargets.HasFlag (DiagnosticTargets.Property))
						CheckCalledMember (operationContext, prop, incompatibleMembers);
				}, OperationKind.PropertyReference);

				if (AnalyzerDiagnosticTargets.HasFlag (DiagnosticTargets.Event)) {
					context.RegisterOperationAction (operationContext => {
						var eventRef = (IEventReferenceOperation) operationContext.Operation;
						CheckCalledMember (operationContext, eventRef.Member, incompatibleMembers);
					}, OperationKind.EventReference);
				}

				context.RegisterOperationAction (operationContext => {
					var delegateCreation = (IDelegateCreationOperation) operationContext.Operation;
					IMethodSymbol methodSymbol;
					if (delegateCreation.Target is IMethodReferenceOperation methodRef)
						methodSymbol = methodRef.Method;
					else if (delegateCreation.Target is IAnonymousFunctionOperation lambda)
						methodSymbol = lambda.Symbol;
					else
						return;
					CheckCalledMember (operationContext, methodSymbol, incompatibleMembers);
				}, OperationKind.DelegateCreation);

				void CheckStaticConstructors (OperationAnalysisContext operationContext,
					ImmutableArray<IMethodSymbol> staticConstructors)
				{
					foreach (var staticConstructor in staticConstructors) {
						if (staticConstructor.HasAttribute (RequiresAttributeName) && TryGetRequiresAttribute (staticConstructor, out AttributeData? requiresAttribute))
							ReportRequiresDiagnostic (operationContext, staticConstructor, requiresAttribute);
					}
				}

				void CheckCalledMember (
					OperationAnalysisContext operationContext,
					ISymbol member,
					ImmutableArray<ISymbol> incompatibleMembers)
				{
					ISymbol containingSymbol = FindContainingSymbol (operationContext, AnalyzerDiagnosticTargets);

					// Do not emit any diagnostic if caller is annotated with the attribute too.
					if (containingSymbol.HasAttribute (RequiresAttributeName))
						return;
					// Check also for RequiresAttribute in the associated symbol
					if (containingSymbol is IMethodSymbol methodSymbol && methodSymbol.AssociatedSymbol is not null && methodSymbol.AssociatedSymbol!.HasAttribute (RequiresAttributeName)) {
						return;
					}
					// If calling an instance constructor, check first for any static constructor since it will be called implicitly
					if (member.ContainingType is { } containingType && operationContext.Operation is IObjectCreationOperation)
						CheckStaticConstructors (operationContext, containingType.StaticConstructors);

					if (ReportSpecialIncompatibleMembersDiagnostic (operationContext, incompatibleMembers, member))
						return;

					if (!member.HasAttribute (RequiresAttributeName))
						return;

					// Warn on the most derived base method taking into account covariant returns
					while (member is IMethodSymbol method && method.OverriddenMethod != null && SymbolEqualityComparer.Default.Equals (method.ReturnType, method.OverriddenMethod.ReturnType))
						member = method.OverriddenMethod;

					if (TryGetRequiresAttribute (member, out AttributeData? requiresAttribute)) {
						ReportRequiresDiagnostic (operationContext, member, requiresAttribute);
					}
				}
			});
		}

		[Flags]
		protected enum DiagnosticTargets
		{
			MethodOrConstructor = 0x0001,
			Property = 0x0002,
			Field = 0x0004,
			Event = 0x0008,
			All = MethodOrConstructor | Property | Field | Event
		}

		/// <summary>
		/// Finds the symbol of the caller to the current operation, helps to find out the symbol in cases where the operation passes
		/// through a lambda or a local function.
		/// </summary>
		/// <param name="operationContext">Analyzer operation context to retrieve the current operation.</param>
		/// <param name="targets">Scope of the attribute to search for callers.</param>
		/// <returns>The symbol of the caller to the operation</returns>
		private static ISymbol FindContainingSymbol (OperationAnalysisContext operationContext, DiagnosticTargets targets)
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
		private void ReportRequiresDiagnostic (OperationAnalysisContext operationContext, ISymbol member, AttributeData? requiresAttribute)
		{
			var message = GetMessageFromAttribute (requiresAttribute);
			var url = GetUrlFromAttribute (requiresAttribute);
			operationContext.ReportDiagnostic (Diagnostic.Create (
				RequiresDiagnosticRule,
				operationContext.Operation.Syntax.GetLocation (),
				member.GetDisplayName (),
				message,
				url));
		}

		protected abstract string GetMessageFromAttribute (AttributeData? requiresAssemblyFilesAttribute);

		private string GetUrlFromAttribute (AttributeData? requiresAssemblyFilesAttribute)
		{
			var url = requiresAssemblyFilesAttribute?.NamedArguments.FirstOrDefault (na => na.Key == "Url").Value.Value?.ToString ();
			return string.IsNullOrEmpty (url) ? "" : " " + url;
		}

		/// <summary>
		/// This method determines if the member has a Requires attribute and returns it in the variable requiresAttribute.
		/// </summary>
		/// <param name="member">Symbol of the member to search attribute.</param>
		/// <param name="requiresAttribute">Output variable in case of matching Requires attribute.</param>
		/// <returns>True if the member contains a Requires attribute; otherwise, returns false.</returns>
		private bool TryGetRequiresAttribute (ISymbol member, out AttributeData? requiresAttribute)
		{
			requiresAttribute = null;
			foreach (var _attribute in member.GetAttributes ()) {
				if (_attribute.AttributeClass is { } attrClass &&
					attrClass.HasName (RequiresAttributeFullyQualifiedName) &&
					VerifyAttributeArguments (_attribute)) {
					requiresAttribute = _attribute;
					return true;
				}
			}
			return false;
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
		protected virtual bool ReportSpecialIncompatibleMembersDiagnostic (OperationAnalysisContext operationContext, ImmutableArray<ISymbol> specialIncompatibleMembers, ISymbol member) => false;

		/// <summary>
		/// Creates a list of special incompatible members that can be used later on by the analyzer to generate diagnostics
		/// </summary>
		/// <param name="compilation">Compilation to search for members</param>
		/// <returns>A list of special incomptaible members</returns>
		protected virtual ImmutableArray<ISymbol> GetSpecialIncompatibleMembers (Compilation compilation) => new ImmutableArray<ISymbol> ();

		/// <summary>
		/// Verifies that the MSBuild requirements to run the analyzer are fulfilled
		/// </summary>
		/// <param name="options">Analyzer options</param>
		/// <param name="compilation">Analyzer compilation information</param>
		/// <returns>True if the requirements to run the analyzer are met; otherwise, returns false</returns>
		protected abstract bool IsAnalyzerEnabled (AnalyzerOptions options, Compilation compilation);
	}
}