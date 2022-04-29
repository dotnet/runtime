// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
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

		readonly ValueSetLattice<SingleValue> _multiValueLattice;

		// Limit tracking array values to 32 values for performance reasons.
		// There are many arrays much longer than 32 elements in .NET,
		// but the interesting ones for the linker are nearly always less than 32 elements.
		const int MaxTrackedArrayValues = 32;

		public TrimAnalysisVisitor (
			LocalStateLattice<MultiValue, ValueSetLattice<SingleValue>> lattice,
			OperationBlockAnalysisContext context,
			ImmutableDictionary<CaptureId, FlowCaptureKind> lValueFlowCaptures
		) : base (lattice, context, lValueFlowCaptures)
		{
			_multiValueLattice = lattice.Lattice.ValueLattice;
			TrimAnalysisPatterns = new TrimAnalysisPatternStore (_multiValueLattice);
		}

		// Override visitor methods to create tracked values when visiting operations
		// which reference possibly annotated source locations:
		// - parameters
		// - 'this' parameter (for annotated methods)
		// - field reference

		public override MultiValue Visit (IOperation? operation, StateValue argument)
		{
			var returnValue = base.Visit (operation, argument);

			// If the return value is empty (TopValue basically) and the Operation tree
			// reports it as having a constant value, use that as it will automatically cover
			// cases we don't need/want to handle.
			if (operation != null && returnValue.IsEmpty () && operation.ConstantValue.HasValue) {
				object? constantValue = operation.ConstantValue.Value;
				if (constantValue == null)
					return NullValue.Instance;
				else if (operation.Type?.SpecialType == SpecialType.System_String && constantValue is string stringConstantValue)
					return new KnownStringValue (stringConstantValue);
				else if (operation.Type?.TypeKind == TypeKind.Enum && constantValue is int enumConstantValue)
					return new ConstIntValue (enumConstantValue);
				else if (operation.Type?.SpecialType == SpecialType.System_Int32 && constantValue is int intConstantValue)
					return new ConstIntValue (intConstantValue);
				else if (operation.Type?.SpecialType == SpecialType.System_Boolean && constantValue is bool boolConstantValue)
					return new ConstIntValue (boolConstantValue ? 1 : 0);
			}

			return returnValue;
		}

		public override MultiValue VisitArrayCreation (IArrayCreationOperation operation, StateValue state)
		{
			var value = base.VisitArrayCreation (operation, state);

			// Don't track multi-dimensional arrays
			if (operation.DimensionSizes.Length != 1)
				return TopValue;

			// Don't track large arrays for performance reasons
			if (operation.Initializer?.ElementValues.Length >= MaxTrackedArrayValues)
				return TopValue;

			var arrayValue = ArrayValue.Create (Visit (operation.DimensionSizes[0], state));
			var elements = operation.Initializer?.ElementValues.Select (val => Visit (val, state)).ToArray () ?? System.Array.Empty<MultiValue> ();
			foreach (var array in arrayValue.Cast<ArrayValue> ()) {
				for (int i = 0; i < elements.Length; i++) {
					array.IndexValues.Add (i, elements[i]);
				}
			}

			return arrayValue;
		}

		public override MultiValue VisitConversion (IConversionOperation operation, StateValue state)
		{
			var value = base.VisitConversion (operation, state);

			if (operation.OperatorMethod != null)
				return operation.OperatorMethod.ReturnType.IsTypeInterestingForDataflow () ? new MethodReturnValue (operation.OperatorMethod) : value;

			// TODO - is it possible to have annotation on the operator method parameters?
			// if so, will these be checked here?

			return value;
		}

		public override MultiValue VisitParameterReference (IParameterReferenceOperation paramRef, StateValue state)
		{
			return paramRef.Parameter.Type.IsTypeInterestingForDataflow () ? new MethodParameterValue (paramRef.Parameter) : TopValue;
		}

		public override MultiValue VisitInstanceReference (IInstanceReferenceOperation instanceRef, StateValue state)
		{
			if (instanceRef.ReferenceKind != InstanceReferenceKind.ContainingTypeInstance)
				return TopValue;

			// The instance reference operation represents a 'this' or 'base' reference to the containing type,
			// so we get the annotation from the containing method.
			// TODO: Check whether the Context.OwningSymbol is the containing type in case we are in a lambda.
			if (instanceRef.Type != null && instanceRef.Type.IsTypeInterestingForDataflow ())
				return new MethodThisParameterValue ((IMethodSymbol) Context.OwningSymbol);

			return TopValue;
		}

		public override MultiValue VisitFieldReference (IFieldReferenceOperation fieldRef, StateValue state)
		{
			var field = fieldRef.Field;
			switch (field.Name) {
			case "EmptyTypes" when field.ContainingType.IsTypeOf ("System", "Type"): {
					return ArrayValue.Create (0);
				}
			case "Empty" when field.ContainingType.IsTypeOf ("System", "String"): {
					return new KnownStringValue (string.Empty);
				}
			}

			if (fieldRef.Field.Type.IsTypeInterestingForDataflow ())
				return new FieldValue (fieldRef.Field);

			return TopValue;
		}

		public override MultiValue VisitTypeOf (ITypeOfOperation typeOfOperation, StateValue state)
		{
			return SingleValueExtensions.FromTypeSymbol (typeOfOperation.TypeOperand) ?? TopValue;
		}

		public override MultiValue VisitBinaryOperator (IBinaryOperation operation, StateValue argument)
		{
			if (!operation.ConstantValue.HasValue && // Optimization - if there is already a constant value available, rely on the Visit(IOperation) instead
				operation.OperatorKind == BinaryOperatorKind.Or &&
				operation.OperatorMethod is null &&
				(operation.Type?.TypeKind == TypeKind.Enum || operation.Type?.SpecialType == SpecialType.System_Int32)) {
				MultiValue leftValue = Visit (operation.LeftOperand, argument);
				MultiValue rightValue = Visit (operation.RightOperand, argument);

				MultiValue result = TopValue;
				foreach (var left in leftValue) {
					if (left is UnknownValue)
						result = _multiValueLattice.Meet (result, left);
					else if (left is ConstIntValue leftConstInt) {
						foreach (var right in rightValue) {
							if (right is UnknownValue)
								result = _multiValueLattice.Meet (result, right);
							else if (right is ConstIntValue rightConstInt) {
								result = _multiValueLattice.Meet (result, new ConstIntValue (leftConstInt.Value | rightConstInt.Value));
							}
						}
					}
				}

				return result;
			}

			return base.VisitBinaryOperator (operation, argument);
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
				// This will copy the values if necessary
				new TrimAnalysisAssignmentPattern (source, target, operation),
				isReturnValue: false
			);
		}

		public override MultiValue HandleArrayElementRead (MultiValue arrayValue, MultiValue indexValue, IOperation operation)
		{
			if (indexValue.AsConstInt () is not int index)
				return UnknownValue.Instance;

			MultiValue result = TopValue;
			foreach (var value in arrayValue) {
				if (value is ArrayValue arr && arr.TryGetValueByIndex (index, out var elementValue))
					result = _multiValueLattice.Meet (result, elementValue);
				else
					return UnknownValue.Instance;
			}
			return result.Equals (TopValue) ? UnknownValue.Instance : result;
		}

		public override void HandleArrayElementWrite (MultiValue arrayValue, MultiValue indexValue, MultiValue valueToWrite, IOperation operation)
		{
			int? index = indexValue.AsConstInt ();
			foreach (var arraySingleValue in arrayValue) {
				if (arraySingleValue is ArrayValue arr) {
					if (index == null) {
						// Reset the array to all unknowns - since we don't know which index is being assigned
						arr.IndexValues.Clear ();
					} else {
						if (arr.IndexValues.TryGetValue (index.Value, out _)) {
							arr.IndexValues[index.Value] = valueToWrite;
						} else if (arr.IndexValues.Count < MaxTrackedArrayValues) {
							arr.IndexValues[index.Value] = valueToWrite;
						}
					}
				}
			}
		}

		public override MultiValue HandleMethodCall (IMethodSymbol calledMethod, MultiValue instance, ImmutableArray<MultiValue> arguments, IOperation operation)
		{
			// For .ctors:
			// - The instance value is empty (TopValue) and that's a bit wrong.
			//   Technically this is an instance call and the instance is some valid value, we just don't know which
			//   but for example it does have a static type. For now this is OK since we don't need the information
			//   for anything yet.
			// - The return here is also technically problematic, the return value is an instance of a known type,
			//   but currently we return empty (since the .ctor is declared as returning void).
			//   Especially with DAM on type, this can lead to incorrectly analyzed code (as in unknown type which leads
			//   to noise). Linker has the same problem currently: https://github.com/dotnet/linker/issues/1952

			var diagnosticContext = DiagnosticContext.CreateDisabled ();
			var handleCallAction = new HandleCallAction (diagnosticContext, Context.OwningSymbol, operation);

			if (!handleCallAction.Invoke (new MethodProxy (calledMethod), instance, arguments, out MultiValue methodReturnValue, out var intrinsicId)) {
				switch (intrinsicId) {
				case IntrinsicId.Array_Empty:
					methodReturnValue = ArrayValue.Create (0);
					break;

				case IntrinsicId.TypeDelegator_Ctor:
					if (operation is IObjectCreationOperation)
						methodReturnValue = arguments[0];
					else
						methodReturnValue = TopValue;

					break;

				default:
					throw new InvalidOperationException ($"Unexpected method {calledMethod.GetDisplayName ()} unhandled by HandleCallAction.");
				}
			}

			// This will copy the values if necessary
			TrimAnalysisPatterns.Add (new TrimAnalysisMethodCallPattern (
				calledMethod,
				instance,
				arguments,
				operation,
				Context.OwningSymbol));

			foreach (var argument in arguments) {
				foreach (var argumentValue in argument) {
					if (argumentValue is ArrayValue arrayValue)
						arrayValue.IndexValues.Clear ();
				}
			}

			return methodReturnValue;
		}

		public override void HandleReturnValue (MultiValue returnValue, IOperation operation)
		{
			var associatedMethod = (IMethodSymbol) Context.OwningSymbol;
			if (associatedMethod.ReturnType.IsTypeInterestingForDataflow ()) {
				var returnParameter = new MethodReturnValue (associatedMethod);

				TrimAnalysisPatterns.Add (
					new TrimAnalysisAssignmentPattern (returnValue, returnParameter, operation),
					isReturnValue: true
				);
			}
		}
	}
}
