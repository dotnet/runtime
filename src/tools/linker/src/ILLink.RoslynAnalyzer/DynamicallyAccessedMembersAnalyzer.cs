// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using ILLink.Shared;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
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
			context.RegisterOperationBlockAction (context => {
				if (context.OwningSymbol.HasAttribute (RequiresUnreferencedCodeAnalyzer.RequiresUnreferencedCodeAttribute))
					return;

				foreach (var operationBlock in context.OperationBlocks) {
					ControlFlowGraph cfg = context.GetControlFlowGraph (operationBlock);
					TrimDataFlowAnalysis trimDataFlowAnalysis = new (context, cfg);

					foreach (TrimAnalysisPattern trimAnalysisPattern in trimDataFlowAnalysis.ComputeTrimAnalysisPatterns ()) {
						foreach (var diagnostic in GetDynamicallyAccessedMembersDiagnostics (trimAnalysisPattern.Source, trimAnalysisPattern.Target, trimAnalysisPattern.Operation.Syntax.GetLocation ()))
							context.ReportDiagnostic (diagnostic);
					}
				}
			});
			// TODO: fix reporting for generic type substitutions. This should happen not only for method invocations,
			// but for any reference to an instantiated method or type.
			context.RegisterOperationAction (context => {
				var invocationOperation = (IInvocationOperation) context.Operation;
				ProcessInvocationOperation (context, invocationOperation);
			}, OperationKind.Invocation);
		}

		static void ProcessInvocationOperation (OperationAnalysisContext context, IInvocationOperation invocationOperation)
		{
			if (context.ContainingSymbol.HasAttribute (RequiresUnreferencedCodeAnalyzer.RequiresUnreferencedCodeAttribute))
				return;

			ProcessTypeArguments (context, invocationOperation);
		}

		static void ProcessTypeArguments (OperationAnalysisContext context, IInvocationOperation invocationOperation)
		{
			var targetMethod = invocationOperation.TargetMethod;
			if (targetMethod.HasAttribute (RequiresUnreferencedCodeAnalyzer.RequiresUnreferencedCodeAttribute))
				return;

			for (int i = 0; i < targetMethod.TypeParameters.Length; i++) {
				var sourceValue = new SymbolValue (targetMethod.TypeArguments[i]);
				var targetValue = new SymbolValue (targetMethod.TypeParameters[i]);
				foreach (var diagnostic in GetDynamicallyAccessedMembersDiagnostics (sourceValue, targetValue, invocationOperation.Syntax.GetLocation ()))
					context.ReportDiagnostic (diagnostic);
			}
		}

		static IEnumerable<Diagnostic> GetDynamicallyAccessedMembersDiagnostics (ValueSet<SingleValue> source, ValueSet<SingleValue> target, Location location)
		{
			foreach (var targetValue in target) {
				foreach (var diagnostic in GetDynamicallyAccessedMembersDiagnostics (source, targetValue, location))
					yield return diagnostic;
			}
		}

		static IEnumerable<Diagnostic> GetDynamicallyAccessedMembersDiagnostics (ValueSet<SingleValue> source, SingleValue target, Location location)
		{
			foreach (var sourceValue in source) {
				foreach (var diagnostic in GetDynamicallyAccessedMembersDiagnostics (sourceValue, target, location))
					yield return diagnostic;
			}
		}

		static IEnumerable<Diagnostic> GetDynamicallyAccessedMembersDiagnostics (SingleValue sourceValue, SingleValue targetValue, Location location)
		{
			if (sourceValue is not SymbolValue source || targetValue is not SymbolValue target)
				yield break;

			Debug.Assert (target.Source.Kind is not SymbolKind.NamedType);
			var damtOnTarget = target.DynamicallyAccessedMemberTypes;
			var damtOnSource = source.DynamicallyAccessedMemberTypes;

			if (Annotations.SourceHasRequiredAnnotations (damtOnSource, damtOnTarget, out var missingAnnotations))
				yield break;

			var diag = GetDiagnosticId (source, target);
			var diagArgs = GetDiagnosticArguments (source, target, missingAnnotations);

			yield return Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (diag), location, diagArgs);
		}

		static DiagnosticId GetDiagnosticId (SymbolValue source, SymbolValue target)
			=> (source.Source.Kind, target.Source.Kind) switch {
				(SymbolKind.Parameter, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsField,
				(SymbolKind.Parameter, SymbolKind.Method) => target.IsMethodReturn ?
					DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType :
					DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter,
				(SymbolKind.Parameter, SymbolKind.Parameter) => DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter,
				(SymbolKind.Field, SymbolKind.Parameter) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsParameter,
				(SymbolKind.Field, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsField,
				(SymbolKind.Field, SymbolKind.Method) => target.IsMethodReturn ?
					DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsMethodReturnType :
					DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter,
				(SymbolKind.Field, SymbolKind.TypeParameter) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsGenericParameter,
				(SymbolKind.Method, SymbolKind.Field) => source.IsMethodReturn
					? DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsField
					: DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsField,
				(SymbolKind.Method, SymbolKind.Method) => (source.IsMethodReturn, target.IsMethodReturn) switch {
					(true, true) => DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType,
					(true, false) => DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter,
					(false, true) => DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsMethodReturnType,
					(false, false) => DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsThisParameter
				},
				(SymbolKind.Method, SymbolKind.Parameter) => source.IsMethodReturn
					? DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter
					: DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsParameter,
				(SymbolKind.TypeParameter, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsField,
				(SymbolKind.TypeParameter, SymbolKind.Method) => target.IsMethodReturn
					? DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethodReturnType
					: DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsThisParameter,
				(SymbolKind.TypeParameter, SymbolKind.Parameter) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter,
				(SymbolKind.TypeParameter, SymbolKind.TypeParameter) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter,
				// AnnotatedSymbol never stores a NamedType (yet). NamedType will only be used for known types which aren't implemented yet.
				(SymbolKind.NamedType, _) => throw new NotImplementedException (),
				(_, SymbolKind.NamedType) => throw new NotImplementedException (),
				_ => throw new NotImplementedException ()
			};

		static string[] GetDiagnosticArguments (SymbolValue source, SymbolValue target, string missingAnnotations)
		{
			var args = new List<string> ();
			args.AddRange (GetDiagnosticArguments (target));
			args.AddRange (GetDiagnosticArguments (source));
			args.Add (missingAnnotations);
			return args.ToArray ();
		}

		static IEnumerable<string> GetDiagnosticArguments (SymbolValue annotatedSymbol)
		{
			ISymbol symbol = annotatedSymbol.Source;
			var args = new List<string> ();
			args.AddRange (symbol.Kind switch {
				SymbolKind.Parameter => new string[] { symbol.GetDisplayName (), symbol.ContainingSymbol.GetDisplayName () },
				SymbolKind.NamedType => throw new NotImplementedException (),
				SymbolKind.Field => new string[] { symbol.GetDisplayName () },
				SymbolKind.Method => new string[] { symbol.GetDisplayName () },
				SymbolKind.TypeParameter => new string[] { symbol.GetDisplayName (), symbol.ContainingSymbol.GetDisplayName () },
				_ => throw new NotImplementedException ($"Unsupported source or target symbol {symbol}.")
			});

			return args;
		}
	}
}
