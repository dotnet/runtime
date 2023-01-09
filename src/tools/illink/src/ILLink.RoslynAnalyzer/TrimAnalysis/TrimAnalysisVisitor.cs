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
			IMethodSymbol method,
			ControlFlowGraph methodCFG,
			ImmutableDictionary<CaptureId, FlowCaptureKind> lValueFlowCaptures,
			TrimAnalysisPatternStore trimAnalysisPatterns,
			InterproceduralState<MultiValue, ValueSetLattice<SingleValue>> interproceduralState
		) : base (lattice, method, methodCFG, lValueFlowCaptures, interproceduralState)
		{
			_multiValueLattice = lattice.Lattice.ValueLattice;
			TrimAnalysisPatterns = trimAnalysisPatterns;
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
			if (operation != null && returnValue.IsEmpty () && TryGetConstantValue (operation, out var constValue))
				return constValue;

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
			// Reading from a parameter always returns the same annotated value. We don't track modifications.
			return GetParameterTargetValue (paramRef.Parameter);
		}

		public override MultiValue VisitInstanceReference (IInstanceReferenceOperation instanceRef, StateValue state)
		{
			if (instanceRef.ReferenceKind != InstanceReferenceKind.ContainingTypeInstance)
				return TopValue;

			// The instance reference operation represents a 'this' or 'base' reference to the containing type,
			// so we get the annotation from the containing method.
			if (instanceRef.Type != null && instanceRef.Type.IsTypeInterestingForDataflow ())
				return new MethodParameterValue (Method, (ParameterIndex) 0, Method.GetDynamicallyAccessedMemberTypes ());

			return TopValue;
		}

		public override MultiValue VisitFieldReference (IFieldReferenceOperation fieldRef, StateValue state)
		{
			var field = fieldRef.Field;
			switch (field.Name) {
			case "EmptyTypes" when field.ContainingType.IsTypeOf ("System", "Type"):
#if DEBUG
			case "ArrayField" when field.ContainingType.IsTypeOf ("Mono.Linker.Tests.Cases.DataFlow", "WriteArrayField"):
#endif
				{
					return ArrayValue.Create (0);
				}
			case "Empty" when field.ContainingType.IsTypeOf ("System", "String"): {
					return new KnownStringValue (string.Empty);
				}
			}

			if (TryGetConstantValue (fieldRef, out var constValue))
				return constValue;

			return GetFieldTargetValue (fieldRef.Field);
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

		public override MultiValue GetFieldTargetValue (IFieldSymbol field)
		{
			return field.Type.IsTypeInterestingForDataflow () ? new FieldValue (field) : TopValue;
		}

		public override MultiValue GetParameterTargetValue (IParameterSymbol parameter)
		{
			return parameter.Type.IsTypeInterestingForDataflow () ? new MethodParameterValue (parameter) : TopValue;
		}

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
			var handleCallAction = new HandleCallAction (diagnosticContext, Method, operation);
			MethodProxy method = new (calledMethod);
			var intrinsicId = Intrinsics.GetIntrinsicIdForMethod (method);

			if (!handleCallAction.Invoke (method, instance, arguments, intrinsicId, out MultiValue methodReturnValue)) {
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
				Method));

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
			if (Method.ReturnType.IsTypeInterestingForDataflow ()) {
				var returnParameter = new MethodReturnValue (Method);

				TrimAnalysisPatterns.Add (
					new TrimAnalysisAssignmentPattern (returnValue, returnParameter, operation),
					isReturnValue: true
				);
			}
		}

		static bool TryGetConstantValue (IOperation operation, out MultiValue constValue)
		{
			if (operation.ConstantValue.HasValue) {
				object? constantValue = operation.ConstantValue.Value;
				if (constantValue == null) {
					constValue = NullValue.Instance;
					return true;
				} else if (operation.Type?.TypeKind == TypeKind.Enum && constantValue is int enumConstantValue) {
					constValue = new ConstIntValue (enumConstantValue);
					return true;
				} else {
					switch (operation.Type?.SpecialType) {
					case SpecialType.System_String when constantValue is string stringConstantValue:
						constValue = new KnownStringValue (stringConstantValue);
						return true;
					case SpecialType.System_Boolean when constantValue is bool boolConstantValue:
						constValue = new ConstIntValue (boolConstantValue ? 1 : 0);
						return true;
					case SpecialType.System_SByte when constantValue is sbyte sbyteConstantValue:
						constValue = new ConstIntValue (sbyteConstantValue);
						return true;
					case SpecialType.System_Byte when constantValue is byte byteConstantValue:
						constValue = new ConstIntValue (byteConstantValue);
						return true;
					case SpecialType.System_Int16 when constantValue is Int16 int16ConstantValue:
						constValue = new ConstIntValue (int16ConstantValue);
						return true;
					case SpecialType.System_UInt16 when constantValue is UInt16 uint16ConstantValue:
						constValue = new ConstIntValue (uint16ConstantValue);
						return true;
					case SpecialType.System_Int32 when constantValue is Int32 int32ConstantValue:
						constValue = new ConstIntValue (int32ConstantValue);
						return true;
					case SpecialType.System_UInt32 when constantValue is UInt32 uint32ConstantValue:
						constValue = new ConstIntValue ((int) uint32ConstantValue);
						return true;
					}
				}
			}

			constValue = default;
			return false;
		}
	}
}
