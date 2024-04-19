// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
namespace ILLink.RoslynAnalyzer.DataFlow
{
	// Visitor which tracks the values of locals in a block. It provides extension points that get called
	// whenever a value that comes from a tracked local reference flows into one of the following:
	// - field
	// - parameter
	// - method return
	public abstract class LocalDataFlowVisitor<TValue, TContext, TValueLattice, TContextLattice, TConditionValue> :
		OperationWalker<LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice>, TValue>,
		ITransfer<
			BlockProxy,
			LocalStateAndContext<TValue, TContext>,
			LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice>,
			LocalStateAndContextLattice<TValue, TContext, TValueLattice, TContextLattice>,
			TConditionValue>
		// This struct constraint prevents warnings due to possible null returns from the visitor methods.
		// Note that this assumes that default(TValue) is equal to the TopValue.
		where TValue : struct, IEquatable<TValue>
		where TContext : struct, IEquatable<TContext>
		where TValueLattice : ILattice<TValue>
		where TContextLattice : ILattice<TContext>
		where TConditionValue : struct, INegate<TConditionValue>
	{
		protected readonly LocalStateAndContextLattice<TValue, TContext, TValueLattice, TContextLattice> LocalStateAndContextLattice;

		protected readonly InterproceduralStateLattice<TValue, TValueLattice> InterproceduralStateLattice;

		protected readonly Compilation Compilation;

		protected readonly ISymbol OwningSymbol;

		private readonly ControlFlowGraph ControlFlowGraph;

		protected TValue TopValue => LocalStateAndContextLattice.LocalStateLattice.Lattice.ValueLattice.Top;

		private readonly ImmutableDictionary<CaptureId, FlowCaptureKind> lValueFlowCaptures;

		public InterproceduralState<TValue, TValueLattice> InterproceduralState;

		bool IsLValueFlowCapture (CaptureId captureId)
			=> lValueFlowCaptures.ContainsKey (captureId);

		bool IsRValueFlowCapture (CaptureId captureId)
			=> !lValueFlowCaptures.TryGetValue (captureId, out var captureKind) || captureKind != FlowCaptureKind.LValueCapture;

		public LocalDataFlowVisitor (
			Compilation compilation,
			LocalStateAndContextLattice<TValue, TContext, TValueLattice, TContextLattice> lattice,
			ISymbol owningSymbol,
			ControlFlowGraph cfg,
			ImmutableDictionary<CaptureId, FlowCaptureKind> lValueFlowCaptures,
			InterproceduralState<TValue, TValueLattice> interproceduralState)
		{
			Compilation = compilation;
			LocalStateAndContextLattice = lattice;
			InterproceduralStateLattice = default;
			OwningSymbol = owningSymbol;
			ControlFlowGraph = cfg;
			this.lValueFlowCaptures = lValueFlowCaptures;
			InterproceduralState = interproceduralState;
		}

		public abstract void ApplyCondition (TConditionValue condition, ref LocalStateAndContext<TValue, TContext> localContextState);

		public TConditionValue? Transfer (
			BlockProxy block,
			LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			foreach (IOperation operation in block.Block.Operations)
				Visit (operation, state);

			// Blocks may end with a BranchValue computation. Visit the BranchValue operation after all others.
			IOperation? branchValueOperation = block.Block.BranchValue;
			if (branchValueOperation == null)
				return null;

			var branchValue = Visit (branchValueOperation, state);
			TConditionValue conditionValue = GetConditionValue (branchValueOperation, state);
			if (block.Block.ConditionKind != ControlFlowConditionKind.None) {
				// BranchValue may represent a value used in a conditional branch to the ConditionalSuccessor.
				// If so, give the analysis an opportunity to model the checked condition, and return the model
				// of the condition back to the generic analysis. It will be applied to the state of each outgoing branch.
				return conditionValue;
			}

			// If not, the BranchValue represents a return or throw value associated with the FallThroughSuccessor of this block.
			// (ConditionalSuccessor == null iff ConditionKind == None).
			// If we get here, we should be analyzing code in a method or field/property initializer,
			// not an attribute instance, since attributes can't have throws or return statements
			Debug.Assert (OwningSymbol is IMethodSymbol or IFieldSymbol or IPropertySymbol,
				$"{OwningSymbol.GetType ()}: {branchValueOperation.Syntax.GetLocation ().GetLineSpan ()}");

			// The BranchValue for a thrown value is not involved in dataflow tracking.
			if (block.Block.FallThroughSuccessor?.Semantics == ControlFlowBranchSemantics.Throw)
				return null;

			// Field/property initializers can't have return statements.
			Debug.Assert (OwningSymbol is IMethodSymbol,
				$"{OwningSymbol.GetType ()}: {branchValueOperation.Syntax.GetLocation ().GetLineSpan ()}");

			// Return statements with return values are represented in the control flow graph as
			// a branch value operation that computes the return value.

			// Use the branch value operation as the key for the warning store and the location of the warning.
			// We don't want the return operation because this might have multiple possible return values in general.
			var current = state.Current;
			HandleReturnValue (branchValue, branchValueOperation, in current.Context);
			// Must be called for every return value even if it did not return an understood condition,
			// because the non-understood conditions will produce warnings for FeatureGuard properties.
			HandleReturnConditionValue (conditionValue, branchValueOperation);
			return null;
		}

		public abstract TConditionValue GetConditionValue (
			IOperation branchValueOperation,
			LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state);

		public abstract TValue GetFieldTargetValue (IFieldSymbol field, IFieldReferenceOperation fieldReferenceOperation, in TContext context);

		public abstract TValue GetParameterTargetValue (IParameterSymbol parameter);

		public abstract void HandleAssignment (
			TValue source,
			TValue target,
			IOperation operation,
			in TContext context);

		public abstract TValue HandleArrayElementRead (TValue arrayValue, TValue indexValue, IOperation operation);

		public abstract void HandleArrayElementWrite (TValue arrayValue, TValue indexValue, TValue valueToWrite, IOperation operation, bool merge);

		// This takes an IOperation rather than an IReturnOperation because the return value
		// may (must?) come from BranchValue of an operation whose FallThroughSuccessor is the exit block.
		public abstract void HandleReturnValue (
			TValue returnValue,
			IOperation operation,
			in TContext context);

		public abstract void HandleReturnConditionValue (
			TConditionValue returnConditionValue,
			IOperation branchValueOperation);

		// This is called for any method call, which includes:
		// - Normal invocation operation
		// - Accessing property value - which is treated as a call to the getter
		// - Setting a property value - which is treated as a call to the setter
		// All inputs are already visited and turned into values.
		// The return value should be a value representing the return value from the called method.
		public abstract TValue HandleMethodCall (
			IMethodSymbol calledMethod,
			TValue instance,
			ImmutableArray<TValue> arguments,
			IOperation operation,
			in TContext context);

		public override TValue VisitLocalReference (ILocalReferenceOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			return GetLocal (operation, state);
		}

		TValue ProcessBinderCall (IOperation operation, string methodName, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state) {
			var assemblyType = Compilation.GetTypeByMetadataName ("Microsoft.CSharp.RuntimeBinder.Binder");
			Debug.Assert (assemblyType != null);
			if (assemblyType == null)
				return TopValue;
			var method = assemblyType.GetMembers (methodName).OfType<IMethodSymbol> ().SingleOrDefault ();
			Debug.Assert (method != null);
			if (method == null)
				return TopValue;
			return ProcessMethodCall (operation, method, null, ImmutableArray<IArgumentOperation>.Empty, state);
		}

		public override TValue VisitDynamicInvocation (IDynamicInvocationOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
			=> ProcessBinderCall (operation, "InvokeMember", state);

		public override TValue VisitDynamicObjectCreation (IDynamicObjectCreationOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
			=> ProcessBinderCall (operation, "InvokeConstructor", state);

		public override TValue VisitDynamicMemberReference (IDynamicMemberReferenceOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
			=> ProcessBinderCall (operation, operation.GetValueUsageInfo (OwningSymbol).HasFlag (ValueUsageInfo.Write) ? "SetMember" : "GetMember", state);

		public override TValue VisitDynamicIndexerAccess (IDynamicIndexerAccessOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
			=> ProcessBinderCall (operation, operation.GetValueUsageInfo (OwningSymbol).HasFlag (ValueUsageInfo.Write) ? "SetIndex" : "GetIndex", state);

		bool IsReferenceToCapturedVariable (ILocalReferenceOperation localReference)
		{
			var local = localReference.Local;

			if (local.IsConst)
				return false;
			Debug.Assert (local.ContainingSymbol is IMethodSymbol or IFieldSymbol, // backing field for property initializers
				$"{local.ContainingSymbol.GetType ()}: {localReference.Syntax.GetLocation ().GetLineSpan ()}");
			return !ReferenceEquals (local.ContainingSymbol, OwningSymbol);
		}

		TValue GetLocal (ILocalReferenceOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			var local = new LocalKey (operation.Local);
			if (IsReferenceToCapturedVariable (operation))
				InterproceduralState.TrackHoistedLocal (local);

			// Get the value from the hoisted locals, if it's tracked there.
			if (InterproceduralState.TryGetHoistedLocal (local, out TValue? value))
				return value.Value;

			return state.Get (local);
		}

		void SetLocal (ILocalReferenceOperation operation, TValue value, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state, bool merge = false)
		{
			var local = new LocalKey (operation.Local);
			if (IsReferenceToCapturedVariable (operation))
				InterproceduralState.TrackHoistedLocal (local);

			// Update the value stored in the hoisted locals, if it's tracked there.
			if (InterproceduralState.TrySetHoistedLocal (local, value))
				return;

			var newValue = merge
				? state.Lattice.LocalStateLattice.Lattice.ValueLattice.Meet (state.Get (local), value)
				: value;
			state.Set (local, newValue);
		}

		TValue ProcessSingleTargetAssignment (IOperation targetOperation, IAssignmentOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state, bool merge)
		{
			switch (targetOperation) {
			case IFieldReferenceOperation:
			case IParameterReferenceOperation: {
					var current = state.Current;
					TValue targetValue = targetOperation switch {
						IFieldReferenceOperation fieldRef => GetFieldTargetValue (fieldRef.Field, fieldRef, in current.Context),
						IParameterReferenceOperation parameterRef => GetParameterTargetValue (parameterRef.Parameter),
						_ => throw new InvalidOperationException ()
					};
					TValue value = Visit (operation.Value, state);
					HandleAssignment (value, targetValue, operation, in current.Context);
					return value;
				}

			// The remaining cases don't have a dataflow value that represents LValues, so we need
			// to handle the LHS specially.
			case IPropertyReferenceOperation propertyRef: {
					// Avoid visiting the property reference because for captured properties, we can't
					// correctly detect whether it is used for reading or writing inside of VisitPropertyReference.
					// https://github.com/dotnet/roslyn/issues/25057
					TValue instanceValue = Visit (propertyRef.Instance, state);
					TValue value = Visit (operation.Value, state);
					IMethodSymbol? setMethod = propertyRef.Property.GetSetMethod ();
					if (setMethod == null) {
						// This can happen in a constructor - there it is possible to assign to a property
						// without a setter. This turns into an assignment to the compiler-generated backing field.
						// To match the linker, this should warn about the compiler-generated backing field.
						// For now, just don't warn. https://github.com/dotnet/runtime/issues/93277
						break;
					}
					// Even if the property has a set method, if the assignment takes place in a property initializer,
					// the write becomes a direct write to the underlying field. This should be treated the same as
					// the case where there is no set method.
					if (OwningSymbol is IPropertySymbol && (ControlFlowGraph.OriginalOperation is not IAttributeOperation))
						break;

					// Property may be an indexer, in which case there will be one or more index arguments followed by a value argument
					ImmutableArray<TValue>.Builder arguments = ImmutableArray.CreateBuilder<TValue> ();
					foreach (var val in propertyRef.Arguments)
						arguments.Add (Visit (val, state));
					arguments.Add (value);

					HandleMethodCallHelper (setMethod, instanceValue, arguments.ToImmutableArray (), operation, state);
					// The return value of a property set expression is the value,
					// even though a property setter has no return value.
					return value;
				}
			case IEventReferenceOperation eventRef: {
					// Handles assignment to an event like 'Event = Handler;', which is a write to the underlying field,
					// not a call to an event accessor method. There is no Roslyn API to access the field,
					// so just visit the instance and the value. https://github.com/dotnet/roslyn/issues/40103
					Visit (eventRef.Instance, state);
					return Visit (operation.Value, state);
				}
			case IImplicitIndexerReferenceOperation indexerRef: {
					// An implicit reference to an indexer where the argument is a System.Index
					TValue instanceValue = Visit (indexerRef.Instance, state);
					TValue indexArgumentValue = Visit (indexerRef.Argument, state);
					TValue value = Visit (operation.Value, state);

					var property = (IPropertySymbol) indexerRef.IndexerSymbol;

					var argumentsBuilder = ImmutableArray.CreateBuilder<TValue> ();
					argumentsBuilder.Add (indexArgumentValue);
					argumentsBuilder.Add (value);

					IMethodSymbol? setMethod = property.GetSetMethod ();
					if (setMethod == null) {
						// It might actually be a call to a ref-returning get method,
						// like Span<T>.this[int].get. We don't handle ref returns yet.
						break;
					}

					HandleMethodCallHelper (setMethod, instanceValue, argumentsBuilder.ToImmutableArray (), operation, state);
					return value;
				}

			// TODO: when setting a property in an attribute, target is an IPropertyReference.
			case ILocalReferenceOperation localRef: {
					TValue value = Visit (operation.Value, state);
					SetLocal (localRef, value, state, merge);
					return value;
				}
			case IArrayElementReferenceOperation arrayElementRef: {
					if (arrayElementRef.Indices.Length != 1)
						break;

					TValue arrayRef = Visit (arrayElementRef.ArrayReference, state);
					TValue index = Visit (arrayElementRef.Indices[0], state);
					TValue value = Visit (operation.Value, state);
					HandleArrayElementWrite (arrayRef, index, value, operation, merge: merge);
					return value;
				}
			case IInlineArrayAccessOperation inlineArrayAccess: {
					TValue arrayRef = Visit (inlineArrayAccess.Instance, state);
					TValue index = Visit (inlineArrayAccess.Argument, state);
					TValue value = Visit (operation.Value, state);
					HandleArrayElementWrite (arrayRef, index, value, operation, merge: merge);
					return value;
				}
			case IDiscardOperation:
				// Assignments like "_ = SomeMethod();" don't need dataflow tracking.
				// Seems like this can't happen with a flow capture operation.
				Debug.Assert (operation.Target is not IFlowCaptureReferenceOperation);
				break;
			case IInstanceReferenceOperation:
				// Assignment to 'this' is not tracked currently.
				// Not relevant for trimming dataflow.
			case IInvocationOperation:
				// This can happen for an assignment to a ref return. Skip for now.
				// The analyzer doesn't handle refs yet. This should be fixed once the analyzer
				// also produces warnings for ref params/locals/returns.
				// https://github.com/dotnet/linker/issues/2632
				// https://github.com/dotnet/linker/issues/2158
			case IDynamicMemberReferenceOperation:
			case IDynamicIndexerAccessOperation:
				// Assignment to dynamic member/indexer will translate into a call to runtime binder methods
				// which should produce warnings, but isn't relevant for dataflow.
				Visit (targetOperation, state);
				break;

			// Keep these cases in sync with those in CapturedReferenceValue, for any that
			// can show up in a flow capture reference (for example, where the right-hand side
			// is a null-coalescing operator).
			default:
				UnexpectedOperationHandler.Handle (targetOperation);
				break;
			}
			return Visit (operation.Value, state);
		}

		public override TValue VisitSimpleAssignment (ISimpleAssignmentOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			return ProcessAssignment (operation, state);
		}

		public override TValue VisitCompoundAssignment (ICompoundAssignmentOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			return ProcessAssignment (operation, state);
		}

		// Note: this is called both for normal assignments and ICompoundAssignmentOperation.
		// The resulting value of a compound assignment isn't important for our dataflow analysis
		// (we don't model addition of integers, for example), so we just treat these the same
		// as normal assignments.
		TValue ProcessAssignment (IAssignmentOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			var targetOperation = operation.Target;
			if (targetOperation is not IFlowCaptureReferenceOperation flowCaptureReference)
				return ProcessSingleTargetAssignment (targetOperation, operation, state, merge: false);

			// Note: technically we should avoid visiting the target operation in ProcessNonCapturedAssignment when assigning
			// to a flow capture reference, because this should be done when the capture is created.
			// For example, a flow capture used as both an LValue and a RValue should only evaluate the expression that
			// computes the object instance of a property reference once. However, we just visit the instance again below
			// for simplicity. This could be generalized if we encounter a dataflow behavior where this makes a difference.

			Debug.Assert (IsLValueFlowCapture (flowCaptureReference.Id));
			Debug.Assert (flowCaptureReference.GetValueUsageInfo (OwningSymbol).HasFlag (ValueUsageInfo.Write));
			var capturedReferences = state.Current.LocalState.CapturedReferences.Get (flowCaptureReference.Id);
			Debug.Assert (!capturedReferences.IsUnknown ());
			if (!capturedReferences.HasMultipleValues) {
				// Single captured reference. Treat this as an overwriting assignment.
				var enumerator = capturedReferences.GetKnownValues ().GetEnumerator ();
				enumerator.MoveNext ();
				targetOperation = enumerator.Current.Reference;
				return ProcessSingleTargetAssignment (targetOperation, operation, state, merge: false);
			}

			// The capture id may have captured multiple references, as in:
			// (b ? ref v1 : ref v2) = value;
			// We treat this as a possible write to each of the captured references,
			// which requires merging with the previous values of each.

			// Note: technically this should only visit the RHS of the assignment once.
			// For now we visit the RHS in ProcessSingleTargetAssignment for simplicity, and
			// rely on the warning deduplication to prevent this from producing multiple warnings
			// if the RHS has dataflow warnings.

			TValue value = TopValue;
			foreach (var capturedReference in capturedReferences.GetKnownValues ()) {
				targetOperation = capturedReference.Reference;
				var singleValue = ProcessSingleTargetAssignment (targetOperation, operation, state, merge: true);
				value = LocalStateAndContextLattice.LocalStateLattice.Lattice.ValueLattice.Meet (value, singleValue);
			}

			return value;
		}

		public override TValue VisitEventAssignment (IEventAssignmentOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			var eventReference = (IEventReferenceOperation) operation.EventReference;
			TValue instanceValue = Visit (eventReference.Instance, state);
			TValue value = Visit (operation.HandlerValue, state);
			if (operation.Adds) {
				IMethodSymbol? addMethod = eventReference.Event.AddMethod;
				Debug.Assert (addMethod != null);
				if (addMethod != null)
					HandleMethodCallHelper (addMethod, instanceValue, ImmutableArray.Create (value), operation, state);
				return value;
			} else {
				IMethodSymbol? removeMethod = eventReference.Event.RemoveMethod;
				Debug.Assert (removeMethod != null);
				if (removeMethod != null)
					HandleMethodCallHelper (removeMethod, instanceValue, ImmutableArray.Create (value), operation, state);
				return value;
			}
		}

		TValue GetFlowCaptureValue (IFlowCaptureReferenceOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			Debug.Assert (!IsLValueFlowCapture (operation.Id),
				$"{operation.Syntax.GetLocation ().GetLineSpan ()}");
			Debug.Assert (operation.GetValueUsageInfo (OwningSymbol).HasFlag (ValueUsageInfo.Read),
				$"{operation.Syntax.GetLocation ().GetLineSpan ()}");

			return state.Get (new LocalKey (operation.Id));
		}

		// Similar to VisitLocalReference
		public override TValue VisitFlowCaptureReference (IFlowCaptureReferenceOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			if (operation.IsInitialization) {
				// This capture reference is a temporary byref. This can happen for string
				// interpolation handlers: https://github.com/dotnet/roslyn/issues/57484.
				// Should really be treated as creating a new l-value flow capture,
				// but this is likely irrelevant for dataflow analysis.

				// LValueFlowCaptureProvider doesn't take into account IsInitialization = true,
				// so it doesn't properly detect this as an l-value capture.
				// Context: https://github.com/dotnet/roslyn/issues/60757
				// Debug.Assert (IsLValueFlowCapture (operation.Id));
				Debug.Assert (operation.GetValueUsageInfo (OwningSymbol).HasFlag (ValueUsageInfo.Write),
					$"{operation.Syntax.GetLocation ().GetLineSpan ()}");
				Debug.Assert (operation.GetValueUsageInfo (OwningSymbol).HasFlag (ValueUsageInfo.Reference),
					$"{operation.Syntax.GetLocation ().GetLineSpan ()}");
				return TopValue;
			}

			if (operation.GetValueUsageInfo (OwningSymbol).HasFlag (ValueUsageInfo.Write)) {
				// If we get here, it means we're visiting a flow capture reference that may be
				// assigned to. Similar to the IsInitialization case, this can happen for an out param
				// where the variable is declared before being passed as an out param, for example:

				// string s;
				// Method (out s, b ? 0 : 1);

				// The second argument is necessary to create multiple branches so that the compiler
				// turns both arguments into flow capture references, instead of just passing a local
				// reference for s.

				// This can also happen for a deconstruction assignments, where the write is not to a byref.
				// Once the analyzer implements support for deconstruction assignments (https://github.com/dotnet/linker/issues/3158),
				// we can try enabling this assert to ensure that this case is only hit for byrefs.
				// Debug.Assert (operation.GetValueUsageInfo (OwningSymbol).HasFlag (ValueUsageInfo.Reference),
				//     $"{operation.Syntax.GetLocation ().GetLineSpan ()}");
				return TopValue;
			}

			return GetFlowCaptureValue (operation, state);
		}

		// Similar to VisitSimpleAssignment when assigning to a local, but for values which are captured without a
		// corresponding local variable. The "flow capture" is like a local assignment, and the "flow capture reference"
		// is like a local reference.
		public override TValue VisitFlowCapture (IFlowCaptureOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			if (IsLValueFlowCapture (operation.Id)) {
				// Should never see an l-value flow capture of another flow capture.
				Debug.Assert (operation.Value is not IFlowCaptureReferenceOperation);
				if (operation.Value is IFlowCaptureReferenceOperation)
					return TopValue;

				// Note: technically we should save some information about the value for LValue flow captures
				// (for example, the object instance of a property reference) and avoid re-computing it when
				// assigning to the FlowCaptureReference.
				var capturedRef = new CapturedReferenceValue (operation.Value);
				var currentState = state.Current;
				currentState.LocalState.CapturedReferences.Set (operation.Id, capturedRef);
				state.Current = currentState;
				return TopValue;
			} else {
				TValue capturedValue;
				if (operation.Value is IFlowCaptureReferenceOperation captureRef) {
					if (IsLValueFlowCapture (captureRef.Id)) {
						// If an r-value captures an l-value, we must dereference the l-value
						// and copy out the value to capture.
						capturedValue = TopValue;
						var capturedReferences = state.Current.LocalState.CapturedReferences.Get (captureRef.Id);
						Debug.Assert (!capturedReferences.IsUnknown ());
						foreach (var capturedReference in capturedReferences.GetKnownValues ()) {
							var value = Visit (capturedReference.Reference, state);
							capturedValue = LocalStateAndContextLattice.LocalStateLattice.Lattice.ValueLattice.Meet (capturedValue, value);
						}
					} else {
						capturedValue = state.Get (new LocalKey (captureRef.Id));
					}
				} else {
					capturedValue = Visit (operation.Value, state);
				}

				state.Set (new LocalKey (operation.Id), capturedValue);
				return capturedValue;
			}
		}

		public override TValue VisitExpressionStatement (IExpressionStatementOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			Visit (operation.Operation, state);
			return TopValue;
		}

		public override TValue VisitInvocation (IInvocationOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
			=> ProcessMethodCall (operation, operation.TargetMethod, operation.Instance, operation.Arguments, state);

		public override TValue VisitDelegateCreation (IDelegateCreationOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			Visit (operation.Target, state);

			IMethodSymbol? targetMethodSymbol = null;
			switch (operation.Target) {
				case IFlowAnonymousFunctionOperation lambda:
					// Tracking lambdas is handled by normal visiting logic for IFlowAnonymousFunctionOperation.

					// Instance of a lambda or local function should be the instance of the containing method.
					// Don't need to track a dataflow value, since the delegate creation will warn if the
					// lambda or local function has an annotated this parameter.
					targetMethodSymbol = lambda.Symbol;
					break;
				case IMethodReferenceOperation methodReference:
					IMethodSymbol methodDefinition = methodReference.Method.OriginalDefinition;
					if (methodDefinition.ContainingSymbol is IMethodSymbol) {
						// Track references to local functions
						var localFunction = methodDefinition;
						Debug.Assert (localFunction.MethodKind == MethodKind.LocalFunction);
						var localFunctionCFG = ControlFlowGraph.GetLocalFunctionControlFlowGraphInScope (localFunction);
						InterproceduralState.TrackMethod (new MethodBodyValue (localFunction, localFunctionCFG));
					}
					targetMethodSymbol = methodReference.Method;
					break;
				case IMemberReferenceOperation:
				case IInvocationOperation:
					// No method symbol.
					break;
				default:
					UnexpectedOperationHandler.Handle (operation.Target);
					break;
			}

			if (targetMethodSymbol == null)
				return TopValue;

			return HandleDelegateCreation (targetMethodSymbol, operation, state.Current.Context);
		}

		public abstract TValue HandleDelegateCreation (IMethodSymbol methodReference, IOperation operation, in TContext context);

		public override TValue VisitPropertyReference (IPropertyReferenceOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			if (operation.GetValueUsageInfo (OwningSymbol).HasFlag (ValueUsageInfo.Write)) {
				// Property references may be passed as ref/out parameters.
				// Enable this assert once we have support for deconstruction assignments.
				// https://github.com/dotnet/linker/issues/3158
				// Debug.Assert (operation.GetValueUsageInfo (OwningSymbol).HasFlag (ValueUsageInfo.Reference),
				// $"{operation.Syntax.GetLocation ().GetLineSpan ()}");
				return TopValue;
			}

			// Accessing property for reading is really a call to the getter
			// The setter case is handled in assignment operation since here we don't have access to the value to pass to the setter
			TValue instanceValue = Visit (operation.Instance, state);
			IMethodSymbol? getMethod = operation.Property.GetGetMethod ();

			// Property may be an indexer, in which case there will be one or more index arguments
			ImmutableArray<TValue>.Builder arguments = ImmutableArray.CreateBuilder<TValue> ();
			foreach (var val in operation.Arguments)
				arguments.Add (Visit (val, state));

			return HandleMethodCallHelper (getMethod!, instanceValue, arguments.ToImmutableArray (), operation, state);
		}

		public override TValue VisitEventReference (IEventReferenceOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			// Writing to an event should not go through this path.
			Debug.Assert (operation.GetValueUsageInfo (OwningSymbol).HasFlag (ValueUsageInfo.Read));
			if (!operation.GetValueUsageInfo (OwningSymbol).HasFlag (ValueUsageInfo.Read))
				return TopValue;

			Visit (operation.Instance, state);
			// Accessing event for reading retrieves the event delegate from the event's backing field,
			// so there is no method call to handle.
			return TopValue;
		}

		public override TValue VisitImplicitIndexerReference (IImplicitIndexerReferenceOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			if (operation.GetValueUsageInfo (OwningSymbol).HasFlag (ValueUsageInfo.Write)) {
				// Implicit indexer references may be passed as ref/out parameters.
				Debug.Assert (operation.GetValueUsageInfo (OwningSymbol).HasFlag (ValueUsageInfo.Reference));
				return TopValue;
			}

			TValue instanceValue = Visit (operation.Instance, state);
			TValue indexArgumentValue = Visit (operation.Argument, state);

			if (operation.IndexerSymbol is not IPropertySymbol indexerProperty) {
				// For example, System.Span<T>.Slice(int, int).
				// Don't try to handle it for now.
				return TopValue;
			}

			IMethodSymbol getMethod = indexerProperty.GetGetMethod ()!;
			return HandleMethodCallHelper (getMethod, instanceValue, ImmutableArray.Create (indexArgumentValue), operation, state);
		}

		public override TValue VisitArrayElementReference (IArrayElementReferenceOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			if (!operation.GetValueUsageInfo (OwningSymbol).HasFlag (ValueUsageInfo.Read))
				return TopValue;

			// Accessing an array element for reading is a call to the indexer
			// or a plain array access. Just handle plain array access for now.

			// Only handle simple index access
			if (operation.Indices.Length != 1)
				return TopValue;

			return HandleArrayElementRead (Visit (operation.ArrayReference, state), Visit (operation.Indices[0], state), operation);
		}

		public override TValue VisitInlineArrayAccess (IInlineArrayAccessOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			Debug.Assert (operation.GetValueUsageInfo (OwningSymbol).HasFlag (ValueUsageInfo.Read));
			if (!operation.GetValueUsageInfo (OwningSymbol).HasFlag (ValueUsageInfo.Read))
				return TopValue;

			return HandleArrayElementRead (Visit (operation.Instance, state), Visit (operation.Argument, state), operation);
		}

		public override TValue VisitArgument (IArgumentOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			return Visit (operation.Value, state);
		}

		public override TValue VisitReturn (IReturnOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			if (operation.ReturnedValue != null) {
				var value = Visit (operation.ReturnedValue, state);
				var current = state.Current;
				HandleReturnValue (value, operation, in current.Context);
				return value;
			}

			return TopValue;
		}

		public override TValue VisitConversion (IConversionOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			var operandValue = Visit (operation.Operand, state);
			return operation.OperatorMethod == null ? operandValue : TopValue;
		}

		public override TValue VisitObjectCreation (IObjectCreationOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			if (operation.Constructor == null)
				return TopValue;

			return ProcessMethodCall (operation, operation.Constructor, null, operation.Arguments, state);
		}

		public override TValue VisitFlowAnonymousFunction (IFlowAnonymousFunctionOperation operation, LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			// The containing symbol of a lambda is either another method, or a field (for field initializers).
			// For property initializers, the containing symbol is the compiler-generated backing field.
			// For property accessors, the containing symbol is the accessor method.
			Debug.Assert (operation.Symbol.ContainingSymbol is IMethodSymbol or IFieldSymbol);
			var lambda = operation.Symbol;
			Debug.Assert (lambda.MethodKind == MethodKind.LambdaMethod);
			var lambdaCFG = ControlFlowGraph.GetAnonymousFunctionControlFlowGraphInScope (operation);
			InterproceduralState.TrackMethod (new MethodBodyValue (lambda, lambdaCFG));
			return TopValue;
		}

		TValue HandleMethodCallHelper (
			IMethodSymbol calledMethod,
			TValue instance,
			ImmutableArray<TValue> arguments,
			IOperation operation,
			LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
		{
			var value = HandleMethodCall (calledMethod, instance, arguments, operation, state.Current.Context);

			if (calledMethod.TryGetAttribute (nameof (DoesNotReturnAttribute), out var doesNotReturnAttributeData)) {
				// If it doesn't return, then after the called method we are free to assume that we
				// are in the Top state. This will have the correct behavior if meeting with another
				// state in the CFG. For example:

				// var x = SomeTrackedValue;
				// if (SomeCondition)
				//   DoesNotReturn();
				// WitnessState(x);

				// The state at 'WitnessState(x)' and 'WitnessContext()' is computed as the meet of:
				// - the output at the end of the block inside of the if (Top), and
				// - the fall-through output of the block with the check (x: SomeTrackedValue).
				// Because Top is the identity of meet, the state from before the if block is preserved,
				// so 'WitnessState(x)' sees that x has 'SomeTrackedValue' in this case.
				state.Current = LocalStateAndContextLattice.Top;
				return value;
			}

			foreach (var parameterProxy in calledMethod.GetParameters ()) {
				if (parameterProxy.ParameterSymbol is not IParameterSymbol parameter)
					continue;

				if (!parameter.TryGetAttribute (nameof (DoesNotReturnIfAttribute), out var attributeData))
					continue;

				if (attributeData.ConstructorArguments.Length != 1)
					continue;

				var attributeArgument = attributeData.ConstructorArguments[0];
				if (attributeArgument.Kind != TypedConstantKind.Primitive)
					continue;

				if (attributeArgument.Value is not bool doesNotReturnIfConditionValue)
					continue;

				var argumentIndex = parameterProxy.MetadataIndex;
				var argument = arguments[argumentIndex];

				IArgumentOperation argumentOperation;
				switch (operation) {
				case IInvocationOperation callOperation:
					argumentOperation = callOperation.Arguments[argumentIndex];
					break;
				case IObjectCreationOperation callOperation:
					argumentOperation = callOperation.Arguments[argumentIndex];
					break;
				default:
					UnexpectedOperationHandler.Handle (operation);
					continue;
				};

				// Get the condition value that is being asserted. If the attribute is DoesNotReturnIf(true),
				// the condition value needs to be negated so that we can assert the false condition.
				TConditionValue conditionValue = GetConditionValue (argumentOperation, state);
				var current = state.Current;
				ApplyCondition (
					doesNotReturnIfConditionValue == false
						? conditionValue
						: conditionValue.Negate (),
					ref current);
				state.Current = current;
			}

			return value;
		}

		TValue ProcessMethodCall (
			IOperation operation,
			IMethodSymbol method,
			IOperation? instance,
			ImmutableArray<IArgumentOperation> arguments,
			LocalDataFlowState<TValue, TContext, TValueLattice, TContextLattice> state)
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

			// For local functions with generic arguments, the substituted method symbol's containing
			// symbol is not the containing method, so we need to check the OriginalDefinition.
			if (method.OriginalDefinition.ContainingSymbol is IMethodSymbol) {
				var localFunction = method.OriginalDefinition;
				Debug.Assert (localFunction.MethodKind == MethodKind.LocalFunction);
				var localFunctionCFG = ControlFlowGraph.GetLocalFunctionControlFlowGraphInScope (localFunction);
				InterproceduralState.TrackMethod (new MethodBodyValue (localFunction, localFunctionCFG));
			}

			return HandleMethodCallHelper (
				method,
				instanceValue,
				argumentsBuilder.ToImmutableArray (),
				operation,
				state);
		}
	}
}
