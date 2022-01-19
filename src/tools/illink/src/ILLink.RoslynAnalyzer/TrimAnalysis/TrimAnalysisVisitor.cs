// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;
using StateValue = ILLink.RoslynAnalyzer.DataFlow.LocalDataFlowState<
	ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>,
	ILLink.Shared.DataFlow.ValueSetLattice<ILLink.Shared.DataFlow.SingleValue>
	>;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public class TrimAnalysisVisitor : LocalDataFlowVisitor<MultiValue, ValueSetLattice<SingleValue>>
	{
		public readonly TrimAnalysisPatternStore TrimAnalysisPatterns;

		public TrimAnalysisVisitor (
			LocalStateLattice<MultiValue, ValueSetLattice<SingleValue>> lattice,
			OperationBlockAnalysisContext context
		) : base (lattice, context)
		{
			TrimAnalysisPatterns = new TrimAnalysisPatternStore (lattice.Lattice.ValueLattice);
		}

		// Override visitor methods to create tracked values when visiting operations
		// which reference possibly annotated source locations:
		// - parameters
		// - 'this' parameter (for annotated methods)
		// - field reference

		public override MultiValue VisitConversion (IConversionOperation operation, StateValue state)
		{
			var value = base.VisitConversion (operation, state);

			if (operation.OperatorMethod != null)
				return new MethodReturnValue (operation.OperatorMethod);

			// TODO - is it possible to have annotation on the operator method parameters?
			// if so, will these be checked here?

			return value;
		}

		public override MultiValue VisitParameterReference (IParameterReferenceOperation paramRef, StateValue state)
		{
			return new MethodParameterValue (paramRef.Parameter);
		}

		public override MultiValue VisitInstanceReference (IInstanceReferenceOperation instanceRef, StateValue state)
		{
			if (instanceRef.ReferenceKind != InstanceReferenceKind.ContainingTypeInstance)
				return TopValue;

			// The instance reference operation represents a 'this' or 'base' reference to the containing type,
			// so we get the annotation from the containing method.
			// TODO: Check whether the Context.OwningSymbol is the containing type in case we are in a lambda.
			var value = new MethodThisParameterValue ((IMethodSymbol) Context.OwningSymbol);
			return value;
		}

		public override MultiValue VisitFieldReference (IFieldReferenceOperation fieldRef, StateValue state)
		{
			return new FieldValue (fieldRef.Field);
		}

		public override MultiValue VisitTypeOf (ITypeOfOperation typeOfOperation, StateValue state)
		{
			// TODO: track known types too!

			if (typeOfOperation.TypeOperand is ITypeParameterSymbol typeParameter)
				return new GenericParameterValue (typeParameter);
			else if (typeOfOperation.TypeOperand is INamedTypeSymbol namedType)
				return new SystemTypeValue (namedType);

			return TopValue;
		}

		// Override handlers for situations where annotated locations may be involved in reflection access:
		// - assignments
		// - method calls
		// - value returned from a method

		public override void HandleAssignment (MultiValue source, MultiValue target, IOperation operation)
		{
			if (target.Equals (TopValue))
				return;

			// TODO: consider not tracking patterns unless the target is something
			// annotated with DAMT.
			TrimAnalysisPatterns.Add (
				new TrimAnalysisPattern (source, target, operation),
				isReturnValue: false
			);
		}

		public override MultiValue HandleMethodCall (IMethodSymbol calledMethod, ValueOfOperation instance, ImmutableArray<ValueOfOperation> arguments, IOperation operation)
		{
			Intrinsics intrinsics = new (Context, operation);
			if (intrinsics.HandleMethodCall (new MethodProxy (calledMethod), instance.Value, arguments.Select (a => a.Value).ToImmutableList (), out MultiValue methodReturnValue))
				return methodReturnValue;

			// If the intrinsic handling didn't work we have to:
			//   Handle the instance value
			//   Handle argument passing
			//   Construct the return value
			// Note: this is temporary as eventually the handling of all method calls should be done in the shared code (not just intrinsics)
			if (!calledMethod.IsStatic) {
				Debug.Assert (instance.Operation != null);
				TrimAnalysisPatterns.Add (
					new TrimAnalysisPattern (
						instance.Value,
						new MethodThisParameterValue (calledMethod),
						instance.Operation!),
					isReturnValue: false);
			}

			for (int argumentIndex = 0; argumentIndex < arguments.Length; argumentIndex++) {
				// For __arglist arguments, there may not be a parameter, so skip these as there can't be any annotations on the parameter
				if (arguments[argumentIndex].Operation is IArgumentOperation argumentOperation &&
					argumentOperation.Parameter == null)
					continue;

				TrimAnalysisPatterns.Add (
					new TrimAnalysisPattern (
						arguments[argumentIndex].Value,
						new MethodParameterValue (calledMethod.Parameters[argumentIndex]),
						arguments[argumentIndex].Operation!),
					isReturnValue: false);
			}

			return calledMethod.ReturnsVoid ? TopValue : new MethodReturnValue (calledMethod);
		}

		public override void HandleReturnValue (MultiValue returnValue, IOperation operation)
		{
			var returnParameter = new MethodReturnValue ((IMethodSymbol) Context.OwningSymbol);

			TrimAnalysisPatterns.Add (
				new TrimAnalysisPattern (returnValue, returnParameter, operation),
				isReturnValue: true
			);
		}
	}
}