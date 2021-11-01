// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public class DynamicallyAccessedMembersAnalyzer : DiagnosticAnalyzer
	{
		internal const string DynamicallyAccessedMembers = nameof (DynamicallyAccessedMembers);
		internal const string DynamicallyAccessedMembersAttribute = nameof (DynamicallyAccessedMembersAttribute);

		static ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnostics ()
		{
			var diagDescriptorsArrayBuilder = ImmutableArray.CreateBuilder<DiagnosticDescriptor> (23);
			for (int i = (int) DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter;
				i <= (int) DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter; i++) {
				diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor ((DiagnosticId) i));
			}

			return diagDescriptorsArrayBuilder.ToImmutable ();
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => GetSupportedDiagnostics ();

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterOperationAction (operationAnalysisContext => {
				var assignmentOperation = (IAssignmentOperation) operationAnalysisContext.Operation;
				ProcessAssignmentOperation (operationAnalysisContext, assignmentOperation);
			}, OperationKind.SimpleAssignment);

			context.RegisterOperationAction (operationAnalysisContext => {
				var invocationOperation = (IInvocationOperation) operationAnalysisContext.Operation;
				ProcessInvocationOperation (operationAnalysisContext, invocationOperation);
			}, OperationKind.Invocation);

			context.RegisterOperationAction (operationAnalysisContext => {
				var returnOperation = (IReturnOperation) operationAnalysisContext.Operation;
				ProcessReturnOperation (operationAnalysisContext, returnOperation);
			}, OperationKind.Return);
		}

		static void CheckAndReportDynamicallyAccessedMembers (IOperation sourceOperation, IOperation targetOperation, OperationAnalysisContext context, Location location)
		{
			if (TryGetSymbolFromOperation (targetOperation, context) is not ISymbol target ||
				TryGetSymbolFromOperation (sourceOperation, context) is not ISymbol source)
				return;

			CheckAndReportDynamicallyAccessedMembers (source, target, context, location, targetIsMethodReturn: false);
		}

		static void CheckAndReportDynamicallyAccessedMembers (IOperation sourceOperation, ISymbol target, OperationAnalysisContext context, Location location, bool targetIsMethodReturn)
		{
			if (TryGetSymbolFromOperation (sourceOperation, context) is not ISymbol source)
				return;

			CheckAndReportDynamicallyAccessedMembers (source, target, context, location, targetIsMethodReturn);
		}

		static void CheckAndReportDynamicallyAccessedMembers (ISymbol source, ISymbol target, OperationAnalysisContext context, Location location, bool targetIsMethodReturn)
		{
			// For the target symbol, a method symbol may represent either a "this" parameter or a method return.
			// The target symbol should never be a named type.
			Debug.Assert (target.Kind is not SymbolKind.NamedType);
			var damtOnTarget = targetIsMethodReturn
				? ((IMethodSymbol) target).GetDynamicallyAccessedMemberTypesOnReturnType ()
				: target.GetDynamicallyAccessedMemberTypes ();
			// For the source symbol, a named type represents a "this" parameter and a method symbol represents a method return.
			var damtOnSource = source.Kind switch {
				SymbolKind.NamedType => context.ContainingSymbol.GetDynamicallyAccessedMemberTypes (),
				SymbolKind.Method => ((IMethodSymbol) source).GetDynamicallyAccessedMemberTypesOnReturnType (),
				_ => source.GetDynamicallyAccessedMemberTypes ()
			};

			if (Annotations.SourceHasRequiredAnnotations (damtOnSource, damtOnTarget, out var missingAnnotations))
				return;

			var diag = GetDiagnosticId (source.Kind, target.Kind, targetIsMethodReturn);
			var diagArgs = GetDiagnosticArguments (source.Kind == SymbolKind.NamedType ? context.ContainingSymbol : source, target, missingAnnotations);
			context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (diag), location, diagArgs));
		}

		static void ProcessAssignmentOperation (OperationAnalysisContext context, IAssignmentOperation assignmentOperation)
		{
			if (context.ContainingSymbol.HasAttribute (RequiresUnreferencedCodeAnalyzer.FullyQualifiedRequiresUnreferencedCodeAttribute))
				return;

			CheckAndReportDynamicallyAccessedMembers (assignmentOperation.Value, assignmentOperation.Target, context, assignmentOperation.Syntax.GetLocation ());
		}

		static void ProcessInvocationOperation (OperationAnalysisContext context, IInvocationOperation invocationOperation)
		{
			if (context.ContainingSymbol.HasAttribute (RequiresUnreferencedCodeAnalyzer.FullyQualifiedRequiresUnreferencedCodeAttribute))
				return;

			ProcessTypeArguments (context, invocationOperation);
			ProcessArguments (context, invocationOperation);
			if (invocationOperation.Instance != null)
				CheckAndReportDynamicallyAccessedMembers (invocationOperation.Instance, invocationOperation.TargetMethod, context, invocationOperation.Syntax.GetLocation (), targetIsMethodReturn: false);
		}

		static void ProcessReturnOperation (OperationAnalysisContext context, IReturnOperation returnOperation)
		{
			if (context.ContainingSymbol.HasAttribute (RequiresUnreferencedCodeAnalyzer.FullyQualifiedRequiresUnreferencedCodeAttribute))
				return;

			CheckAndReportDynamicallyAccessedMembers (returnOperation, context.ContainingSymbol, context, returnOperation.Syntax.GetLocation (), targetIsMethodReturn: true);
		}

		static void ProcessArguments (OperationAnalysisContext context, IInvocationOperation invocationOperation)
		{
			foreach (var argument in invocationOperation.Arguments) {
				var sourceSymbol = TryGetSymbolFromOperation (argument.Value, context);
				if (argument.Parameter == null || sourceSymbol == null)
					continue;
				CheckAndReportDynamicallyAccessedMembers (sourceSymbol, argument.Parameter, context, argument.Syntax.GetLocation (), targetIsMethodReturn: false);
			}
		}

		static void ProcessTypeArguments (OperationAnalysisContext context, IInvocationOperation invocationOperation)
		{
			var targetMethod = invocationOperation.TargetMethod;
			if (targetMethod.HasAttribute (RequiresUnreferencedCodeAnalyzer.FullyQualifiedRequiresUnreferencedCodeAttribute))
				return;

			for (int i = 0; i < targetMethod.TypeParameters.Length; i++) {
				CheckAndReportDynamicallyAccessedMembers (targetMethod.TypeArguments[i], targetMethod.TypeParameters[i], context, invocationOperation.Syntax.GetLocation (), targetIsMethodReturn: false);
			}
		}

		static DiagnosticId GetDiagnosticId (SymbolKind source, SymbolKind target, bool targetIsMethodReturnType = false)
			=> (source, target) switch {
				(SymbolKind.Parameter, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsField,
				(SymbolKind.Parameter, SymbolKind.Method) => targetIsMethodReturnType ?
					DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType :
					DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter,
				(SymbolKind.Parameter, SymbolKind.Parameter) => DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter,
				(SymbolKind.Field, SymbolKind.Parameter) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsParameter,
				(SymbolKind.Field, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsField,
				(SymbolKind.Field, SymbolKind.Method) => targetIsMethodReturnType ?
					DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsMethodReturnType :
					DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter,
				(SymbolKind.Field, SymbolKind.TypeParameter) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsGenericParameter,
				(SymbolKind.NamedType, SymbolKind.Method) => targetIsMethodReturnType ?
					DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsMethodReturnType :
					DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsThisParameter,
				(SymbolKind.Method, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsField,
				(SymbolKind.Method, SymbolKind.Method) => targetIsMethodReturnType ?
					DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType :
					DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter,
				// Source here will always be a method's return type.
				(SymbolKind.Method, SymbolKind.Parameter) => DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter,
				(SymbolKind.NamedType, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsField,
				(SymbolKind.NamedType, SymbolKind.Parameter) => DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsParameter,
				(SymbolKind.TypeParameter, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsField,
				(SymbolKind.TypeParameter, SymbolKind.Method) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethodReturnType,
				(SymbolKind.TypeParameter, SymbolKind.Parameter) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter,
				(SymbolKind.TypeParameter, SymbolKind.TypeParameter) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter,
				_ => throw new NotImplementedException ()
			};

		static string[] GetDiagnosticArguments (ISymbol source, ISymbol target, string missingAnnotations)
		{
			var args = new List<string> ();
			args.AddRange (GetDiagnosticArguments (target));
			args.AddRange (GetDiagnosticArguments (source));
			args.Add (missingAnnotations);
			return args.ToArray ();
		}

		static IEnumerable<string> GetDiagnosticArguments (ISymbol symbol)
		{
			var args = new List<string> ();
			args.AddRange (symbol.Kind switch {
				SymbolKind.Parameter => new string[] { symbol.GetDisplayName (), symbol.ContainingSymbol.GetDisplayName () },
				SymbolKind.NamedType => new string[] { symbol.GetDisplayName () },
				SymbolKind.Field => new string[] { symbol.GetDisplayName () },
				SymbolKind.Method => new string[] { symbol.GetDisplayName () },
				SymbolKind.TypeParameter => new string[] { symbol.GetDisplayName (), symbol.ContainingSymbol.GetDisplayName () },
				_ => throw new NotImplementedException ($"Unsupported source or target symbol {symbol}.")
			});

			return args;
		}

		static ISymbol? TryGetSymbolFromOperation (IOperation? operation, OperationAnalysisContext context) =>
			operation switch {
				IArgumentOperation argument => TryGetSymbolFromOperation (argument.Value, context),
				IAssignmentOperation assignment => TryGetSymbolFromOperation (assignment.Value, context),
				IConversionOperation conversion => conversion.OperatorMethod ?? TryGetSymbolFromOperation (conversion.Operand, context),
				IInstanceReferenceOperation instanceReference => instanceReference.ReferenceKind switch {
					InstanceReferenceKind.ContainingTypeInstance =>
						context.ContainingSymbol.Kind == SymbolKind.Method
							? ((IMethodSymbol) context.ContainingSymbol).ContainingType
							: null,
					_ => null
				},
				// The target method of an invocation represents the called method. If this invocation is found within
				// a return operation, this representes the returned value, which might be annotated.
				IInvocationOperation invocation => invocation.TargetMethod,
				IMemberReferenceOperation memberReference => memberReference.Member,
				IParameterReferenceOperation parameterReference => parameterReference.Parameter,
				IReturnOperation returnOp => TryGetSymbolFromOperation (returnOp.ReturnedValue, context),
				// We only need to find the symbol for generic types here
				ITypeOfOperation typeOf => typeOf.TypeOperand is ITypeParameterSymbol ? typeOf.TypeOperand : null,
				_ => null
			};
	}
}
