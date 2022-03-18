// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	// Visitor which tracks the values of locals in a block. It provides extension points that get called
	// whenever a value that comes from a tracked local reference flows into one of the following:
	// - field
	// - parameter
	// - method return
	public abstract class LocalDataFlowVisitor<TValue, TValueLattice> : OperationWalker<LocalDataFlowState<TValue, TValueLattice>, TValue>,
		ITransfer<BlockProxy, LocalState<TValue>, LocalDataFlowState<TValue, TValueLattice>, LocalStateLattice<TValue, TValueLattice>>
		// This struct constraint prevents warnings due to possible null returns from the visitor methods.
		// Note that this assumes that default(TValue) is equal to the TopValue.
		where TValue : struct, IEquatable<TValue>
		where TValueLattice : ILattice<TValue>
	{
		protected readonly LocalStateLattice<TValue, TValueLattice> LocalStateLattice;

		protected readonly OperationBlockAnalysisContext Context;

		protected TValue TopValue => LocalStateLattice.Lattice.ValueLattice.Top;

		public LocalDataFlowVisitor (LocalStateLattice<TValue, TValueLattice> lattice, OperationBlockAnalysisContext context) =>
			(LocalStateLattice, Context) = (lattice, context);

		public void Transfer (BlockProxy block, LocalDataFlowState<TValue, TValueLattice> state)
		{
			foreach (IOperation operation in block.Block.Operations)
				Visit (operation, state);

			// Blocks may end with a BranchValue computation. Visit the BranchValue operation after all others.
			IOperation? branchValueOperation = block.Block.BranchValue;
			if (branchValueOperation == null)
				return;

			var branchValue = Visit (branchValueOperation, state);

			// BranchValue may represent a value used in a conditional branch to the ConditionalSuccessor - if so, we are done.
			if (block.Block.ConditionKind != ControlFlowConditionKind.None)
				return;

			// If not, the BranchValue represents a return or throw value associated with the FallThroughSuccessor of this block.
			// (ConditionalSuccessor == null iff ConditionKind == None).

			// The BranchValue for a thrown value is not involved in dataflow tracking.
			if (block.Block.FallThroughSuccessor?.Semantics == ControlFlowBranchSemantics.Throw)
				return;

			// Return statements with return values are represented in the control flow graph as
			// a branch value operation that computes the return value.

			// Use the branch value operation as the key for the warning store and the location of the warning.
			// We don't want the return operation because this might have multiple possible return values in general.
			HandleReturnValue (branchValue, branchValueOperation);
		}

		public abstract void HandleAssignment (TValue source, TValue target, IOperation operation);

		public abstract TValue HandleArrayElementRead (TValue arrayValue, TValue indexValue, IOperation operation);

		public abstract void HandleArrayElementWrite (TValue arrayValue, TValue indexValue, TValue valueToWrite, IOperation operation);

		// This takes an IOperation rather than an IReturnOperation because the return value
		// may (must?) come from BranchValue of an operation whose FallThroughSuccessor is the exit block.
		public abstract void HandleReturnValue (TValue returnValue, IOperation operation);

		// This is called for any method call, which includes:
		// - Normal invocation operation
		// - Accessing property value - which is treated as a call to the getter
		// - Setting a property value - which is treated as a call to the setter
		// All inputs are already visited and turned into values.
		// The return value should be a value representing the return value from the called method.
		public abstract TValue HandleMethodCall (IMethodSymbol calledMethod, TValue instance, ImmutableArray<TValue> arguments, IOperation operation);

		public override TValue VisitLocalReference (ILocalReferenceOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			return state.Get (new LocalKey (operation.Local));
		}

		public override TValue VisitSimpleAssignment (ISimpleAssignmentOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			var targetValue = Visit (operation.Target, state);
			var value = Visit (operation.Value, state);
			switch (operation.Target) {
			case ILocalReferenceOperation localRef:
				state.Set (new LocalKey (localRef.Local), value);
				break;
			case IFieldReferenceOperation:
			case IParameterReferenceOperation:
				// Extension point for assignments to "interesting" targets.
				// Doesn't get called for assignments to locals, which are handled above.
				HandleAssignment (value, targetValue, operation);
				break;
			case IPropertyReferenceOperation propertyRef:
				// A property assignment is really a call to the property setter.
				var setMethod = propertyRef.Property.SetMethod!;
				TValue instanceValue = Visit (propertyRef.Instance, state);
				// The return value of a property set expression is the value,
				// even though a property setter has no return value.
				HandleMethodCall (
					setMethod,
					instanceValue,
					ImmutableArray.Create (value),
					operation);
				break;
			// TODO: when setting a property in an attribute, target is an IPropertyReference.
			case IArrayElementReferenceOperation arrayElementRef:
				if (arrayElementRef.Indices.Length != 1)
					break;

				HandleArrayElementWrite (Visit (arrayElementRef.ArrayReference, state), Visit (arrayElementRef.Indices[0], state), value, operation);
				break;
			case IDiscardOperation:
				// Assignments like "_ = SomeMethod();" don't need dataflow tracking.
				break;
			case IInvalidOperation:
				// This can happen for a field assignment in an attribute instance.
				// TODO: validate against the field attributes.
				break;
			default:
				throw new NotImplementedException (operation.Target.GetType ().ToString ());
			}
			return value;
		}

		// Similar to VisitLocalReference
		public override TValue VisitFlowCaptureReference (IFlowCaptureReferenceOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			return state.Get (new LocalKey (operation.Id));
		}

		// Similar to VisitSimpleAssignment when assigning to a local, but for values which are captured without a
		// corresponding local variable. The "flow capture" is like a local assignment, and the "flow capture reference"
		// is like a local reference.
		public override TValue VisitFlowCapture (IFlowCaptureOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			TValue value = Visit (operation.Value, state);
			state.Set (new LocalKey (operation.Id), value);
			return value;
		}

		public override TValue VisitExpressionStatement (IExpressionStatementOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			Visit (operation.Operation, state);
			return TopValue;
		}

		public override TValue VisitInvocation (IInvocationOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
			=> ProcessMethodCall (operation, operation.TargetMethod, operation.Instance, operation.Arguments, state);

		public static IMethodSymbol GetPropertyMethod (IPropertyReferenceOperation operation)
		{
			// The IPropertyReferenceOperation doesn't tell us whether this reference is to the getter or setter.
			// For this we need to look at the containing operation.
			var parent = operation.Parent;
			if (parent?.Kind == OperationKind.SimpleAssignment) {
				var assignment = (ISimpleAssignmentOperation) parent;
				if (assignment.Target == operation) {
					var setMethod = operation.Property.SetMethod;
					Debug.Assert (setMethod != null);
					return setMethod!;
				}
				Debug.Assert (assignment.Value == operation);
			}

			var getMethod = operation.Property.GetMethod;
			Debug.Assert (getMethod != null);
			return getMethod!;
		}

		public override TValue VisitPropertyReference (IPropertyReferenceOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			if (operation.GetValueUsageInfo (Context.OwningSymbol).HasFlag (ValueUsageInfo.Read)) {
				// Accessing property for reading is really a call to the getter
				// The setter case is handled in assignment operation since here we don't have access to the value to pass to the setter
				TValue instanceValue = Visit (operation.Instance, state);
				return HandleMethodCall (
					operation.Property.GetMethod!,
					instanceValue,
					ImmutableArray<TValue>.Empty,
					operation);
			}

			return TopValue;
		}

		public override TValue VisitArrayElementReference (IArrayElementReferenceOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			if (operation.GetValueUsageInfo (Context.OwningSymbol).HasFlag (ValueUsageInfo.Read)) {
				// Accessing an array element for reading is a call to the indexer
				// or a plain array access. Just handle plain array access for now.

				// Only handle simple index access
				if (operation.Indices.Length != 1)
					return TopValue;

				return HandleArrayElementRead (Visit (operation.ArrayReference, state), Visit (operation.Indices[0], state), operation);
			}

			return TopValue;
		}

		public override TValue VisitArgument (IArgumentOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			return Visit (operation.Value, state);
		}

		public override TValue VisitReturn (IReturnOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			if (operation.ReturnedValue != null) {
				var value = Visit (operation.ReturnedValue, state);
				HandleReturnValue (value, operation);
				return value;
			}

			return TopValue;
		}

		public override TValue VisitConversion (IConversionOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			var operandValue = Visit (operation.Operand, state);
			return operation.OperatorMethod == null ? operandValue : TopValue;
		}

		public override TValue VisitObjectCreation (IObjectCreationOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			if (operation.Constructor == null)
				return TopValue;

			return ProcessMethodCall (operation, operation.Constructor, null, operation.Arguments, state);
		}

		TValue ProcessMethodCall (
			IOperation operation,
			IMethodSymbol method,
			IOperation? instance,
			ImmutableArray<IArgumentOperation> arguments,
			LocalDataFlowState<TValue, TValueLattice> state)
		{
			TValue instanceValue = Visit (instance, state);

			var argumentsBuilder = ImmutableArray.CreateBuilder<TValue> ();
			foreach (var argument in arguments) {
				// For __arglist argument there might not be any parameter
				// __arglist is only legal as the last argument to a method and there's also no supported
				// way for it to carry annotations or participate in data flow in any way, so it's OK to ignore it.
				// Since it's always last, it's also OK to simply pass a shorter arguments array to the call.
				if (argument?.Parameter == null)
					break;

				argumentsBuilder.Add (VisitArgument (argument, state));
			}

			return HandleMethodCall (
				method,
				instanceValue,
				argumentsBuilder.ToImmutableArray (),
				operation);
		}
	}
}