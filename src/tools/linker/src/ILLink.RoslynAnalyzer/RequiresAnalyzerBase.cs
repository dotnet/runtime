// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ILLink.Shared;
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

		private protected abstract string RequiresAttributeFullyQualifiedName { get; }

		private protected abstract DiagnosticTargets AnalyzerDiagnosticTargets { get; }

		private protected abstract DiagnosticDescriptor RequiresDiagnosticRule { get; }

		private protected abstract DiagnosticDescriptor RequiresAttributeMismatch { get; }

		private protected virtual ImmutableArray<(Action<OperationAnalysisContext> Action, OperationKind[] OperationKind)> ExtraOperationActions { get; } = ImmutableArray<(Action<OperationAnalysisContext> Action, OperationKind[] OperationKind)>.Empty;

		private protected virtual ImmutableArray<(Action<SyntaxNodeAnalysisContext> Action, SyntaxKind[] SyntaxKind)> ExtraSyntaxNodeActions { get; } = ImmutableArray<(Action<SyntaxNodeAnalysisContext> Action, SyntaxKind[] SyntaxKind)>.Empty;

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterCompilationStartAction (context => {
				var compilation = context.Compilation;
				if (!IsAnalyzerEnabled (context.Options, compilation))
					return;

				var incompatibleMembers = GetSpecialIncompatibleMembers (compilation);
				context.RegisterSymbolAction (symbolAnalysisContext => {
					var methodSymbol = (IMethodSymbol) symbolAnalysisContext.Symbol;
					CheckMatchingAttributesInOverrides (symbolAnalysisContext, methodSymbol);
					CheckAttributeInstantiation (symbolAnalysisContext, methodSymbol);
					foreach (var typeParameter in methodSymbol.TypeParameters)
						CheckAttributeInstantiation (symbolAnalysisContext, typeParameter);

				}, SymbolKind.Method);

				context.RegisterSymbolAction (symbolAnalysisContext => {
					var typeSymbol = (INamedTypeSymbol) symbolAnalysisContext.Symbol;
					CheckMatchingAttributesInInterfaces (symbolAnalysisContext, typeSymbol);
					CheckAttributeInstantiation (symbolAnalysisContext, typeSymbol);
					foreach (var typeParameter in typeSymbol.TypeParameters)
						CheckAttributeInstantiation (symbolAnalysisContext, typeParameter);

				}, SymbolKind.NamedType);


				context.RegisterSymbolAction (symbolAnalysisContext => {
					var propertySymbol = (IPropertySymbol) symbolAnalysisContext.Symbol;
					if (AnalyzerDiagnosticTargets.HasFlag (DiagnosticTargets.Property)) {
						CheckMatchingAttributesInOverrides (symbolAnalysisContext, propertySymbol);
					}

					CheckAttributeInstantiation (symbolAnalysisContext, propertySymbol);
				}, SymbolKind.Property);

				context.RegisterSymbolAction (symbolAnalysisContext => {
					var eventSymbol = (IEventSymbol) symbolAnalysisContext.Symbol;
					if (AnalyzerDiagnosticTargets.HasFlag (DiagnosticTargets.Event)) {
						CheckMatchingAttributesInOverrides (symbolAnalysisContext, eventSymbol);
					}

					CheckAttributeInstantiation (symbolAnalysisContext, eventSymbol);
				}, SymbolKind.Event);

				context.RegisterSymbolAction (symbolAnalysisContext => {
					var fieldSymbol = (IFieldSymbol) symbolAnalysisContext.Symbol;
					CheckAttributeInstantiation (symbolAnalysisContext, fieldSymbol);
				}, SymbolKind.Field);

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
					var fieldReference = (IFieldReferenceOperation) operationContext.Operation;
					CheckCalledMember (operationContext, fieldReference.Field, incompatibleMembers);
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

				context.RegisterOperationAction (operationContext => {
					var eventRef = (IEventReferenceOperation) operationContext.Operation;
					var eventSymbol = (IEventSymbol) eventRef.Member;
					var assignmentOperation = eventRef.Parent as IEventAssignmentOperation;

					if (assignmentOperation != null && assignmentOperation.Adds && eventSymbol.AddMethod is IMethodSymbol eventAddMethod)
						CheckCalledMember (operationContext, eventAddMethod, incompatibleMembers);

					if (assignmentOperation != null && !assignmentOperation.Adds && eventSymbol.RemoveMethod is IMethodSymbol eventRemoveMethod)
						CheckCalledMember (operationContext, eventRemoveMethod, incompatibleMembers);

					if (eventSymbol.RaiseMethod is IMethodSymbol eventRaiseMethod)
						CheckCalledMember (operationContext, eventRaiseMethod, incompatibleMembers);

					if (AnalyzerDiagnosticTargets.HasFlag (DiagnosticTargets.Event))
						CheckCalledMember (operationContext, eventSymbol, incompatibleMembers);
				}, OperationKind.EventReference);

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

				context.RegisterSyntaxNodeAction (syntaxNodeAnalysisContext => {
					var model = syntaxNodeAnalysisContext.SemanticModel;
					if (syntaxNodeAnalysisContext.ContainingSymbol is not ISymbol containingSymbol || containingSymbol.HasAttribute (RequiresAttributeName))
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
						if (!typeParam.HasConstructorConstraint)
							continue;

						var typeArgCtors = ((INamedTypeSymbol) typeArg).InstanceConstructors;
						foreach (var instanceCtor in typeArgCtors) {
							if (instanceCtor.Arity > 0)
								continue;

							if (instanceCtor.TryGetAttribute (RequiresAttributeName, out var requiresUnreferencedCodeAttribute)) {
								syntaxNodeAnalysisContext.ReportDiagnostic (Diagnostic.Create (RequiresDiagnosticRule,
									syntaxNodeAnalysisContext.Node.GetLocation (),
									containingSymbol.GetDisplayName (),
									(string) requiresUnreferencedCodeAttribute.ConstructorArguments[0].Value!,
									GetUrlFromAttribute (requiresUnreferencedCodeAttribute)));
							}
						}
					}
				}, SyntaxKind.GenericName);

				// Register any extra operation actions supported by the analyzer.
				foreach (var extraOperationAction in ExtraOperationActions)
					context.RegisterOperationAction (extraOperationAction.Action, extraOperationAction.OperationKind);

				foreach (var extraSyntaxNodeAction in ExtraSyntaxNodeActions)
					context.RegisterSyntaxNodeAction (extraSyntaxNodeAction.Action, extraSyntaxNodeAction.SyntaxKind);

				void CheckAttributeInstantiation (
					SymbolAnalysisContext symbolAnalysisContext,
					ISymbol symbol)
				{
					if (symbol.HasAttribute (RequiresAttributeName))
						return;

					foreach (var attr in symbol.GetAttributes ()) {
						if (TryGetRequiresAttribute (attr.AttributeConstructor, out var requiresAttribute)) {
							symbolAnalysisContext.ReportDiagnostic (Diagnostic.Create (RequiresDiagnosticRule,
								symbol.Locations[0], attr.AttributeConstructor!.Name, GetMessageFromAttribute (requiresAttribute), GetUrlFromAttribute (requiresAttribute)));
						}
					}
				}

				void CheckCalledMember (
					OperationAnalysisContext operationContext,
					ISymbol member,
					ImmutableArray<ISymbol> incompatibleMembers)
				{
					ISymbol containingSymbol = FindContainingSymbol (operationContext, AnalyzerDiagnosticTargets);

					// Do not emit any diagnostic if caller is annotated with the attribute too.
					if (IsMemberInRequiresScope (containingSymbol))
						return;

					if (ReportSpecialIncompatibleMembersDiagnostic (operationContext, incompatibleMembers, member))
						return;

					// Warn on the most derived base method taking into account covariant returns
					while (member is IMethodSymbol method && method.OverriddenMethod != null && SymbolEqualityComparer.Default.Equals (method.ReturnType, method.OverriddenMethod.ReturnType))
						member = method.OverriddenMethod;

					if (!TargetHasRequiresAttribute (member, out var requiresAttribute))
						return;

					ReportRequiresDiagnostic (operationContext, member, requiresAttribute);
				}

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
					ImmutableArray<INamedTypeSymbol> interfaces = type.Interfaces;
					foreach (INamespaceOrTypeSymbol iface in interfaces) {
						var members = iface.GetMembers ();
						foreach (var member in members) {
							var implementation = type.FindImplementationForInterfaceMember (member);
							// In case the implementation is null because the user code is missing an implementation, we dont provide diagnostics.
							// The compiler will provide an error
							if (implementation != null && HasMismatchingAttributes (member, implementation))
								ReportMismatchInAttributesDiagnostic (symbolAnalysisContext, implementation, member, isInterface: true);
						}
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
		private void ReportRequiresDiagnostic (OperationAnalysisContext operationContext, ISymbol member, AttributeData requiresAttribute)
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

		private void ReportMismatchInAttributesDiagnostic (SymbolAnalysisContext symbolAnalysisContext, ISymbol member, ISymbol baseMember, bool isInterface = false)
		{
			string message = MessageFormat.FormatRequiresAttributeMismatch (member.HasAttribute (RequiresAttributeName), isInterface, RequiresAttributeName, member.GetDisplayName (), baseMember.GetDisplayName ());
			symbolAnalysisContext.ReportDiagnostic (Diagnostic.Create (
				RequiresAttributeMismatch,
				member.Locations[0],
				message));
		}

		private bool HasMismatchingAttributes (ISymbol member1, ISymbol member2) => member1.HasAttribute (RequiresAttributeName) ^ member2.HasAttribute (RequiresAttributeName);

		// TODO: Consider sharing with linker IsMethodInRequiresUnreferencedCodeScope method
		/// <summary>
		/// True if the source of a call is considered to be annotated with the Requires... attribute
		/// </summary>
		protected bool IsMemberInRequiresScope (ISymbol member)
		{
			if (member.HasAttribute (RequiresAttributeName) ||
				(member is not ITypeSymbol &&
				member.ContainingType.HasAttribute (RequiresAttributeName))) {
				return true;
			}

			// Check also for RequiresAttribute in the associated symbol
			if (member is IMethodSymbol { AssociatedSymbol: { } associated } && associated.HasAttribute (RequiresAttributeName))
				return true;

			return false;
		}

		// TODO: Consider sharing with linker DoesMethodRequireUnreferencedCode method
		/// <summary>
		/// True if the target of a call is considered to be annotated with the Requires... attribute
		/// </summary>
		protected bool TargetHasRequiresAttribute (ISymbol member, [NotNullWhen (returnValue: true)] out AttributeData? requiresAttribute)
		{
			requiresAttribute = null;
			if (member.IsStaticConstructor ()) {
				return false;
			}

			if (TryGetRequiresAttribute (member, out requiresAttribute)) {
				return true;
			}

			// Also check the containing type
			if ((member.IsStatic || member.IsConstructor ()) && member is not ITypeSymbol) {
				return TryGetRequiresAttribute (member.ContainingType, out requiresAttribute);
			}
			return false;
		}

		protected abstract string GetMessageFromAttribute (AttributeData requiresAttribute);

		public static string GetUrlFromAttribute (AttributeData? requiresAttribute)
		{
			var url = requiresAttribute?.NamedArguments.FirstOrDefault (na => na.Key == "Url").Value.Value?.ToString ();
			return MessageFormat.FormatRequiresAttributeUrlArg (url);
		}

		/// <summary>
		/// This method determines if the member has a Requires attribute and returns it in the variable requiresAttribute.
		/// </summary>
		/// <param name="member">Symbol of the member to search attribute.</param>
		/// <param name="requiresAttribute">Output variable in case of matching Requires attribute.</param>
		/// <returns>True if the member contains a Requires attribute; otherwise, returns false.</returns>
		private bool TryGetRequiresAttribute (ISymbol? member, [NotNullWhen (returnValue: true)] out AttributeData? requiresAttribute)
		{
			requiresAttribute = null;
			if (member == null)
				return false;

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