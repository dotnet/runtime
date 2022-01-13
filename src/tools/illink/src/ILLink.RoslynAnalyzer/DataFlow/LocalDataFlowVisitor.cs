// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

		// This is called to handle instance method invocations, where "receiver" is the
		// analyzed value for the object on which the instance method is called, and similarly
		// for property references.
		public abstract void HandleReceiverArgument (TValue receiver, IMethodSymbol targetMethod, IOperation operation);

		public abstract void HandleArgument (TValue argument, IArgumentOperation operation);

		// Called for property setters which are essentially like arguments passed to a method.
		public abstract void HandlePropertySetterArgument (TValue value, IMethodSymbol setMethod, ISimpleAssignmentOperation operation);

		// This takes an IOperation rather than an IReturnOperation because the return value
		// may (must?) come from BranchValue of an operation whose FallThroughSuccessor is the exit block.
		public abstract void HandleReturnValue (TValue returnValue, IOperation operation);

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
				var setMethod = propertyRef.Property.SetMethod;
				Debug.Assert (setMethod != null);
				HandlePropertySetterArgument (value, setMethod!, operation);
				break;
			// TODO: when setting a property in an attribute, target is an IPropertyReference.
			case IArrayElementReferenceOperation:
				// TODO
				break;
			case IDiscardOperation:
				// Assignments like "_ = SomeMethod();" don't need dataflow tracking.
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
		{
			if (operation.Instance != null) {
				var instanceValue = Visit (operation.Instance, state);
				HandleReceiverArgument (instanceValue, operation.TargetMethod, operation);
			}

			foreach (var argument in operation.Arguments)
				VisitArgument (argument, state);

			return TopValue;
		}

		public override TValue VisitPropertyReference (IPropertyReferenceOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			if (operation.Instance != null) {
				var instanceValue = Visit (operation.Instance, state);
				var usage = operation.GetValueUsageInfo(Context.OwningSymbol);
				if (usage.HasFlag(ValueUsageInfo.Read) && operation.Property.GetMethod is {} getMethod)
				{
					HandleReceiverArgument(instanceValue, getMethod, operation);
				}
				if (usage.HasFlag(ValueUsageInfo.Write) && operation.Property.SetMethod is {} setMethod)
				{
					HandleReceiverArgument(instanceValue, setMethod, operation);
				}
			}

			return TopValue;
		}

		public override TValue VisitArgument (IArgumentOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			var value = Visit (operation.Value, state);
			HandleArgument (value, operation);
			return value;
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
	}
}