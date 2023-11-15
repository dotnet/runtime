// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using ILLink.Shared.TypeSystemProxy;

using StateValue = ILLink.RoslynAnalyzer.DataFlow.LocalDataFlowState<
	ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>,
	ILLink.RoslynAnalyzer.DataFlow.FeatureContext,
	ILLink.Shared.DataFlow.ValueSetLattice<ILLink.Shared.DataFlow.SingleValue>,
	ILLink.RoslynAnalyzer.DataFlow.FeatureContextLattice
	>;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	// Visits a conditional expression to optionally produce a 'FeatureChecksValue'
	// (a set features that are checked to be enabled or disabled).
	// The visitor takes a LocalDataFlowState as an argument, allowing for checks that
	// depend on the current dataflow state.
	public class FeatureChecksVisitor : OperationVisitor<StateValue, FeatureChecksValue?>
	{
		DataFlowAnalyzerContext _dataFlowAnalyzerContext;

		public FeatureChecksVisitor (DataFlowAnalyzerContext dataFlowAnalyzerContext)
		{
			_dataFlowAnalyzerContext = dataFlowAnalyzerContext;
		}

		public override FeatureChecksValue? VisitArgument (IArgumentOperation operation, StateValue state)
		{
			return Visit (operation.Value, state);
		}

		public override FeatureChecksValue? VisitPropertyReference (IPropertyReferenceOperation operation, StateValue state)
		{
			foreach (var analyzer in _dataFlowAnalyzerContext.EnabledRequiresAnalyzers) {
				if (analyzer.IsRequiresCheck (_dataFlowAnalyzerContext.Compilation, operation.Property)) {
					return new FeatureChecksValue (analyzer.FeatureName);
				}
			}

			return null;
		}

		public override FeatureChecksValue? VisitUnaryOperator (IUnaryOperation operation, StateValue state)
		{
			if (operation.OperatorKind is not UnaryOperatorKind.Not)
				return null;

			FeatureChecksValue? context = Visit (operation.Operand, state);
			if (context == null)
				return null;

			return context.Value.Negate ();
		}

		public bool? GetLiteralBool (IOperation operation)
		{
			if (operation is not ILiteralOperation literal)
				return null;

			return GetConstantBool (literal.ConstantValue);
		}

		static bool? GetConstantBool (Optional<object?> constantValue)
		{
			if (!constantValue.HasValue || constantValue.Value is not bool value)
				return null;

			return value;
		}

		public override FeatureChecksValue? VisitBinaryOperator (IBinaryOperation operation, StateValue state)
		{
			bool expectEqual;
			switch (operation.OperatorKind) {
				case BinaryOperatorKind.Equals:
					expectEqual = true;
					break;
				case BinaryOperatorKind.NotEquals:
					expectEqual = false;
					break;
				default:
					return null;
			}

			if (GetLiteralBool (operation.LeftOperand) is bool leftBool) {
				if (Visit (operation.RightOperand, state) is not FeatureChecksValue rightValue)
					return null;
				return leftBool == expectEqual
					? rightValue
					: rightValue.Negate ();
			}

			if (GetLiteralBool (operation.RightOperand) is bool rightBool) {
				if (Visit (operation.LeftOperand, state) is not FeatureChecksValue leftValue)
					return null;
				return rightBool == expectEqual
					? leftValue
					: leftValue.Negate ();
			}

			return null;
		}

		public override FeatureChecksValue? VisitIsPattern (IIsPatternOperation operation, StateValue state)
		{
			if (GetExpectedValueFromPattern (operation.Pattern) is not bool patternValue)
				return null;

			if (Visit (operation.Value, state) is not FeatureChecksValue value)
				return null;

			return patternValue
				? value
				: value.Negate ();


			static bool? GetExpectedValueFromPattern (IPatternOperation pattern)
			{
				switch (pattern) {
					case IConstantPatternOperation constantPattern:
						return GetConstantBool (constantPattern.Value.ConstantValue);
					case INegatedPatternOperation negatedPattern:
						return !GetExpectedValueFromPattern (negatedPattern.Pattern);
					default:
						return null;
				}
			}
		}
	}
}
