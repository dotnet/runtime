// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
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
			TrimAnalysisPatterns = new TrimAnalysisPatternStore ();
		}

		// Override visitor methods to create tracked values when visiting operations
		// which reference possibly annotated source locations:
		// - invocations (for annotated method returns)
		// - parameters
		// - 'this' parameter (for annotated methods)
		// - field reference

		public override MultiValue VisitInvocation (IInvocationOperation operation, StateValue state)
		{
			// Base logic takes care of visiting arguments, etc.
			base.VisitInvocation (operation, state);

			// TODO: don't track values for unsupported types. Can be done when adding warnings
			// for annotations on unsupported types.
			// https://github.com/dotnet/linker/issues/2273
			return new MethodReturnValue (operation.TargetMethod);
		}

		// Just like VisitInvocation for a method call, we need to visit a property method invocation
		// in case it has an annotated return value.
		public override MultiValue VisitPropertyReference (IPropertyReferenceOperation operation, StateValue state)
		{
			// Base logic visits the receiver
			base.VisitPropertyReference (operation, state);

			var propertyMethod = GetPropertyMethod (operation);
			// Only the getter has a return value that may be annotated.
			if (propertyMethod.MethodKind == MethodKind.PropertyGet)
				return new MethodReturnValue (propertyMethod);

			return TopValue;
		}

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

			return TopValue;
		}

		// Override handlers for situations where annotated locations may be involved in reflection access:
		// - assignments
		// - arguments passed to method parameters (or implicitly passed to property setters)
		//   this also needs to create the annotated value for parameters, because they are not represented
		//   as 'IParameterReferenceOperation' when passing arguments
		// - instance passed as explicit or implicit receiver to a method invocation
		//   this also needs to create the annotation for the implicit receiver parameter.
		// - value returned from a method

		public override void HandleAssignment (MultiValue source, MultiValue target, IOperation operation)
		{
			if (target.Equals (TopValue))
				return;

			// TODO: consider not tracking patterns unless the target is something
			// annotated with DAMT.
			TrimAnalysisPatterns.Add (new TrimAnalysisPattern (source, target, operation));
		}

		public override void HandleArgument (MultiValue argumentValue, IArgumentOperation operation)
		{
			// Parameter may be null for __arglist arguments. Skip these.
			if (operation.Parameter == null)
				return;

			var parameter = new MethodParameterValue (operation.Parameter);

			TrimAnalysisPatterns.Add (new TrimAnalysisPattern (
				argumentValue,
				parameter,
				operation
			));
		}

		// Similar to HandleArgument, for an assignment operation that is really passing an argument to a property setter.
		public override void HandlePropertySetterArgument (MultiValue value, IMethodSymbol setMethod, ISimpleAssignmentOperation operation)
		{
			var parameter = new MethodParameterValue (setMethod.Parameters[0]);

			TrimAnalysisPatterns.Add (new TrimAnalysisPattern (value, parameter, operation));
		}

		// Can be called for an invocation or a propertyreference
		// where the receiver is not null (so an instance method/property).
		public override void HandleReceiverArgument (MultiValue receiverValue, IMethodSymbol targetMethod, IOperation operation)
		{
			MultiValue thisParameter = new MethodThisParameterValue (targetMethod!);

			TrimAnalysisPatterns.Add (new TrimAnalysisPattern (
				receiverValue,
				thisParameter,
				operation
			));
		}

		public override void HandleReturnValue (MultiValue returnValue, IOperation operation)
		{
			var returnParameter = new MethodReturnValue ((IMethodSymbol) Context.OwningSymbol);

			TrimAnalysisPatterns.Add (new TrimAnalysisPattern (
				returnValue,
				returnParameter,
				operation
			));
		}
	}
}