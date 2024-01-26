// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;
using StateValue = ILLink.RoslynAnalyzer.DataFlow.LocalDataFlowState<
	ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>,
	ILLink.RoslynAnalyzer.DataFlow.FeatureContext,
	ILLink.Shared.DataFlow.ValueSetLattice<ILLink.Shared.DataFlow.SingleValue>,
	ILLink.RoslynAnalyzer.DataFlow.FeatureContextLattice
	>;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public class TrimAnalysisVisitor : LocalDataFlowVisitor<
		MultiValue,
		FeatureContext,
		ValueSetLattice<SingleValue>,
		FeatureContextLattice,
		FeatureChecksValue>
	{
		public readonly TrimAnalysisPatternStore TrimAnalysisPatterns;

		readonly ValueSetLattice<SingleValue> _multiValueLattice;

		// Limit tracking array values to 32 values for performance reasons.
		// There are many arrays much longer than 32 elements in .NET,
		// but the interesting ones for the ILLink are nearly always less than 32 elements.
		const int MaxTrackedArrayValues = 32;

		FeatureChecksVisitor _featureChecksVisitor;

		public TrimAnalysisVisitor (
			Compilation compilation,
			LocalStateAndContextLattice<MultiValue, FeatureContext, ValueSetLattice<SingleValue>, FeatureContextLattice> lattice,
			ISymbol owningSymbol,
			ControlFlowGraph methodCFG,
			ImmutableDictionary<CaptureId, FlowCaptureKind> lValueFlowCaptures,
			TrimAnalysisPatternStore trimAnalysisPatterns,
			InterproceduralState<MultiValue, ValueSetLattice<SingleValue>> interproceduralState,
			DataFlowAnalyzerContext dataFlowAnalyzerContext)
			: base (compilation, lattice, owningSymbol, methodCFG, lValueFlowCaptures, interproceduralState)
		{
			_multiValueLattice = lattice.LocalStateLattice.Lattice.ValueLattice;
			TrimAnalysisPatterns = trimAnalysisPatterns;
			_featureChecksVisitor = new FeatureChecksVisitor (dataFlowAnalyzerContext);
		}

		public override FeatureChecksValue? GetConditionValue (IOperation branchValueOperation, StateValue state)
		{
			return _featureChecksVisitor.Visit (branchValueOperation, state);
		}

		public override void ApplyCondition (FeatureChecksValue featureChecksValue,  ref LocalStateAndContext<MultiValue, FeatureContext> currentState)
		{
			currentState.Context = currentState.Context.Union (new FeatureContext (featureChecksValue.EnabledFeatures));
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
			foreach (var array in arrayValue.AsEnumerable ().Cast<ArrayValue> ()) {
				for (int i = 0; i < elements.Length; i++) {
					array.IndexValues.Add (i, ArrayValue.SanitizeArrayElementValue(elements[i]));
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
			// 'this' is not allowed in field/property initializers, so the owning symbol should be a method.
			// It can also happen that we see this for a static method - for example a delegate creation
			// over a local function does this, even thought the "this" makes no sense inside a static scope.
			if (OwningSymbol is IMethodSymbol method && !method.IsStatic)
				return new MethodParameterValue (method, (ParameterIndex) 0, method.GetDynamicallyAccessedMemberTypes ());

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

			var current = state.Current;
			return GetFieldTargetValue (fieldRef.Field, fieldRef, in current.Context);
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
				foreach (var left in leftValue.AsEnumerable ()) {
					if (left is UnknownValue)
						result = _multiValueLattice.Meet (result, left);
					else if (left is ConstIntValue leftConstInt) {
						foreach (var right in rightValue.AsEnumerable ()) {
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

		public override MultiValue GetFieldTargetValue (IFieldSymbol field, IFieldReferenceOperation fieldReferenceOperation, in FeatureContext featureContext)
		{
			TrimAnalysisPatterns.Add (
				new TrimAnalysisFieldAccessPattern (field, fieldReferenceOperation, OwningSymbol, featureContext)
			);

			ProcessGenericArgumentDataFlow (field, fieldReferenceOperation, featureContext);

			return new FieldValue (field);
		}

		public override MultiValue GetParameterTargetValue (IParameterSymbol parameter)
			=> new MethodParameterValue (parameter);

		public override void HandleAssignment (MultiValue source, MultiValue target, IOperation operation, in FeatureContext featureContext)
		{
			if (target.Equals (TopValue))
				return;

			// TODO: consider not tracking patterns unless the target is something
			// annotated with DAMT.
			TrimAnalysisPatterns.Add (
				// This will copy the values if necessary
				new TrimAnalysisAssignmentPattern (source, target, operation, OwningSymbol, featureContext),
				isReturnValue: false
			);
		}

		public override MultiValue HandleArrayElementRead (MultiValue arrayValue, MultiValue indexValue, IOperation operation)
		{
			if (indexValue.AsConstInt () is not int index)
				return UnknownValue.Instance;

			MultiValue result = TopValue;
			foreach (var value in arrayValue.AsEnumerable ()) {
				if (value is ArrayValue arr && arr.TryGetValueByIndex (index, out var elementValue))
					result = _multiValueLattice.Meet (result, elementValue);
				else
					return UnknownValue.Instance;
			}
			return result.Equals (TopValue) ? UnknownValue.Instance : result;
		}

		public override void HandleArrayElementWrite (MultiValue arrayValue, MultiValue indexValue, MultiValue valueToWrite, IOperation operation, bool merge)
		{
			int? index = indexValue.AsConstInt ();
			foreach (var arraySingleValue in arrayValue.AsEnumerable ()) {
				if (arraySingleValue is ArrayValue arr) {
					if (index == null) {
						// Reset the array to all unknowns - since we don't know which index is being assigned
						arr.IndexValues.Clear ();
					} else if (arr.IndexValues.TryGetValue (index.Value, out _) || arr.IndexValues.Count < MaxTrackedArrayValues) {
						var sanitizedValue = ArrayValue.SanitizeArrayElementValue(valueToWrite);
						arr.IndexValues[index.Value] = merge
							? _multiValueLattice.Meet (arr.IndexValues[index.Value], sanitizedValue)
							: sanitizedValue;
					}
				}
			}
		}

		public override MultiValue HandleMethodCall (
			IMethodSymbol calledMethod,
			MultiValue instance,
			ImmutableArray<MultiValue> arguments,
			IOperation operation,
			in FeatureContext featureContext)
		{
			// For .ctors:
			// - The instance value is empty (TopValue) and that's a bit wrong.
			//   Technically this is an instance call and the instance is some valid value, we just don't know which
			//   but for example it does have a static type. For now this is OK since we don't need the information
			//   for anything yet.
			// - The return here is also technically problematic, the return value is an instance of a known type,
			//   but currently we return empty (since the .ctor is declared as returning void).
			//   Especially with DAM on type, this can lead to incorrectly analyzed code (as in unknown type which leads
			//   to noise). ILLink has the same problem currently: https://github.com/dotnet/linker/issues/1952

			var diagnosticContext = DiagnosticContext.CreateDisabled ();
			HandleCall (operation, OwningSymbol, calledMethod, instance, arguments, diagnosticContext, _multiValueLattice, out MultiValue methodReturnValue);

			// This will copy the values if necessary
			TrimAnalysisPatterns.Add (new TrimAnalysisMethodCallPattern (
				calledMethod,
				instance,
				arguments,
				operation,
				OwningSymbol,
				featureContext));

			ProcessGenericArgumentDataFlow (calledMethod, operation, featureContext);

			foreach (var argument in arguments) {
				foreach (var argumentValue in argument.AsEnumerable ()) {
					if (argumentValue is ArrayValue arrayValue)
						arrayValue.IndexValues.Clear ();
				}
			}

			return methodReturnValue;
		}

		internal static void HandleCall(
			IOperation operation,
			ISymbol owningSymbol,
			IMethodSymbol calledMethod,
			MultiValue instance,
			ImmutableArray<MultiValue> arguments,
			DiagnosticContext diagnosticContext,
			ValueSetLattice<SingleValue> multiValueLattice,
			out MultiValue methodReturnValue)
		{
			var handleCallAction = new HandleCallAction (diagnosticContext, owningSymbol, operation);
			MethodProxy method = new (calledMethod);
			var intrinsicId = Intrinsics.GetIntrinsicIdForMethod (method);

			if (handleCallAction.Invoke (method, instance, arguments, intrinsicId, out methodReturnValue)) {
				return;
			}

			MultiValue? maybeMethodReturnValue = default;

			switch (intrinsicId) {
			case IntrinsicId.Array_Empty:
				AddReturnValue (ArrayValue.Create (0));
				break;

			case IntrinsicId.TypeDelegator_Ctor:
				if (operation is IObjectCreationOperation)
					AddReturnValue (arguments[0]);

				break;

			case IntrinsicId.Object_GetType: {
					foreach (var valueNode in instance.AsEnumerable ()) {
						// Note that valueNode can be statically typed as some generic argument type.
						// For example:
						//   void Method<T>(T instance) { instance.GetType().... }
						// But it could be that T is annotated with for example PublicMethods:
						//   void Method<[DAM(PublicMethods)] T>(T instance) { instance.GetType().GetMethod("Test"); }
						// In this case it's in theory possible to handle it, by treating the T basically as a base class
						// for the actual type of "instance". But the analysis for this would be pretty complicated (as the marking
						// has to happen on the callsite, which doesn't know that GetType() will be used...).
						// For now we're intentionally ignoring this case - it will produce a warning.
						// The counter example is:
						//   Method<Base>(new Derived);
						// In this case to get correct results, trimmer would have to mark all public methods on Derived. Which
						// currently it won't do.

						// To emulate IL tools behavior (trimmer, NativeAOT compiler), we're going to intentionally "forget" the static type
						// if it is a generic argument type.

						ITypeSymbol? staticType = (valueNode as IValueWithStaticType)?.StaticType?.Type;
						if (staticType?.TypeKind == TypeKind.TypeParameter)
							staticType = null;

						if (staticType is null) {
							// We don't know anything about the type GetType was called on. Track this as a usual "result of a method call without any annotations"
							AddReturnValue (FlowAnnotations.Instance.GetMethodReturnValue (new (calledMethod)));
						} else if (staticType.IsSealed || staticType.IsTypeOf ("System", "Delegate") || staticType.TypeKind == TypeKind.Array) {
							// We can treat this one the same as if it was a typeof() expression

							// We can allow Object.GetType to be modeled as System.Delegate because we keep all methods
							// on delegates anyway so reflection on something this approximation would miss is actually safe.

							// We can also treat all arrays as "sealed" since it's not legal to derive from Array type (even though it is not sealed itself)

							// We ignore the fact that the type can be annotated (see below for handling of annotated types)
							// This means the annotations (if any) won't be applied - instead we rely on the exact knowledge
							// of the type. So for example even if the type is annotated with PublicMethods
							// but the code calls GetProperties on it - it will work - mark properties, don't mark methods
							// since we ignored the fact that it's annotated.
							// This can be seen a little bit as a violation of the annotation, but we already have similar cases
							// where a parameter is annotated and if something in the method sets a specific known type to it
							// we will also make it just work, even if the annotation doesn't match the usage.
							AddReturnValue (new SystemTypeValue (new (staticType)));
						} else {
							var annotation = FlowAnnotations.GetTypeAnnotation (staticType);
							AddReturnValue (FlowAnnotations.Instance.GetMethodReturnValue (new (calledMethod), annotation));
						}
					}
				}

				break;

			default:
				Debug.Fail ($"Unexpected method {calledMethod.GetDisplayName ()} unhandled by HandleCallAction.");

				// Do nothing even if we reach a point which we didn't expect - the analyzer should never crash as it's a too disruptive experience for the user.
				break;
			}

			methodReturnValue = maybeMethodReturnValue ?? multiValueLattice.Top;

			void AddReturnValue (MultiValue value)
			{
				maybeMethodReturnValue = (maybeMethodReturnValue is null) ? value : multiValueLattice.Meet ((MultiValue) maybeMethodReturnValue, value);
			}
		}

		public override void HandleReturnValue (MultiValue returnValue, IOperation operation, in FeatureContext featureContext)
		{
			// Return statements should only happen inside of method bodies.
			Debug.Assert (OwningSymbol is IMethodSymbol);
			if (OwningSymbol is not IMethodSymbol method)
				return;

			if (method.ReturnType.IsTypeInterestingForDataflow ()) {
				var returnParameter = new MethodReturnValue (method);

				TrimAnalysisPatterns.Add (
					new TrimAnalysisAssignmentPattern (returnValue, returnParameter, operation, OwningSymbol, featureContext),
					isReturnValue: true
				);
			}
		}

		public override MultiValue HandleDelegateCreation (IMethodSymbol method, IOperation operation, in FeatureContext featureContext)
		{
			TrimAnalysisPatterns.Add (new TrimAnalysisReflectionAccessPattern (
				method,
				operation,
				OwningSymbol,
				featureContext
			));

			ProcessGenericArgumentDataFlow (method, operation, featureContext);

			return TopValue;
		}

		private void ProcessGenericArgumentDataFlow (IMethodSymbol method, IOperation operation, in FeatureContext featureContext)
		{
			// We only need to validate static methods and then all generic methods
			// Instance non-generic methods don't need validation because the creation of the instance
			// is the place where the validation will happen.
			if (!method.IsStatic && !method.IsGenericMethod && !method.IsConstructor ())
				return;

			if (GenericArgumentDataFlow.RequiresGenericArgumentDataFlow (method)) {
				TrimAnalysisPatterns.Add (new TrimAnalysisGenericInstantiationPattern (
					method,
					operation,
					OwningSymbol,
					featureContext));
			}
		}

		private void ProcessGenericArgumentDataFlow (IFieldSymbol field, IOperation operation, in FeatureContext featureContext)
		{
			// We only need to validate static field accesses, instance field accesses don't need generic parameter validation
			// because the create of the instance would do that instead.
			if (!field.IsStatic)
				return;

			if (GenericArgumentDataFlow.RequiresGenericArgumentDataFlow (field)) {
				TrimAnalysisPatterns.Add (new TrimAnalysisGenericInstantiationPattern (
					field,
					operation,
					OwningSymbol,
					featureContext));
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
					case SpecialType.System_Int16 when constantValue is short int16ConstantValue:
						constValue = new ConstIntValue (int16ConstantValue);
						return true;
					case SpecialType.System_UInt16 when constantValue is ushort uint16ConstantValue:
						constValue = new ConstIntValue (uint16ConstantValue);
						return true;
					case SpecialType.System_Int32 when constantValue is int int32ConstantValue:
						constValue = new ConstIntValue (int32ConstantValue);
						return true;
					case SpecialType.System_UInt32 when constantValue is uint uint32ConstantValue:
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
