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
	public class FeatureChecksVisitor : OperationVisitor<StateValue, FeatureChecksValue>
	{
		DataFlowAnalyzerContext _dataFlowAnalyzerContext;

		public FeatureChecksVisitor (DataFlowAnalyzerContext dataFlowAnalyzerContext)
		{
			_dataFlowAnalyzerContext = dataFlowAnalyzerContext;
		}

		public override FeatureChecksValue DefaultVisit (IOperation operation, StateValue state)
		{
			// Visiting a non-understood pattern should return the empty set of features, which will
			// prevent this check from acting as a guard for any feature.
			return FeatureChecksValue.None;
		}

		public override FeatureChecksValue VisitArgument (IArgumentOperation operation, StateValue state)
		{
			return Visit (operation.Value, state);
		}

		public override FeatureChecksValue VisitPropertyReference (IPropertyReferenceOperation operation, StateValue state)
		{
			// A single property may serve as a feature check for multiple features.
			FeatureChecksValue featureChecks = FeatureChecksValue.None;
			foreach (var analyzer in _dataFlowAnalyzerContext.EnabledRequiresAnalyzers) {
				if (analyzer.IsFeatureCheck (operation.Property, _dataFlowAnalyzerContext.Compilation)) {
					var featureCheck = new FeatureChecksValue (analyzer.RequiresAttributeFullyQualifiedName);
					featureChecks = featureChecks.And (featureCheck);
				}
			}

			return featureChecks;
		}

		public override FeatureChecksValue VisitUnaryOperator (IUnaryOperation operation, StateValue state)
		{
			if (operation.OperatorKind is not UnaryOperatorKind.Not)
				return FeatureChecksValue.None;

			FeatureChecksValue context = Visit (operation.Operand, state);
			return context.Negate ();
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

		public override FeatureChecksValue VisitBinaryOperator (IBinaryOperation operation, StateValue state)
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
					return FeatureChecksValue.None;
			}

			if (GetLiteralBool (operation.LeftOperand) is bool leftBool) {
				FeatureChecksValue rightValue = Visit (operation.RightOperand, state);
				return leftBool == expectEqual
					? rightValue
					: rightValue.Negate ();
			}

			if (GetLiteralBool (operation.RightOperand) is bool rightBool) {
				FeatureChecksValue leftValue = Visit (operation.LeftOperand, state);
				return rightBool == expectEqual
					? leftValue
					: leftValue.Negate ();
			}

			return FeatureChecksValue.None;
		}

		public override FeatureChecksValue VisitIsPattern (IIsPatternOperation operation, StateValue state)
		{
			if (GetExpectedValueFromPattern (operation.Pattern) is not bool patternValue)
				return FeatureChecksValue.None;

			FeatureChecksValue value = Visit (operation.Value, state);
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
