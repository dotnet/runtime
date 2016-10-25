// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents a control expression that handles multiple selections by passing control to a <see cref="SwitchCase"/>.
    /// </summary>
    [DebuggerTypeProxy(typeof(SwitchExpressionProxy))]
    public sealed class SwitchExpression : Expression
    {
        internal SwitchExpression(Type type, Expression switchValue, Expression defaultBody, MethodInfo comparison, ReadOnlyCollection<SwitchCase> cases)
        {
            Type = type;
            SwitchValue = switchValue;
            DefaultBody = defaultBody;
            Comparison = comparison;
            Cases = cases;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression"/> represents.
        /// </summary>
        /// <returns>The <see cref="System.Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type { get; }

        /// <summary>
        /// Returns the node type of this Expression. Extension nodes should return
        /// ExpressionType.Extension when overriding this method.
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> of the expression.</returns>
        public sealed override ExpressionType NodeType => ExpressionType.Switch;

        /// <summary>
        /// Gets the test for the switch.
        /// </summary>
        public Expression SwitchValue { get; }

        /// <summary>
        /// Gets the collection of <see cref="SwitchCase"/> objects for the switch.
        /// </summary>
        public ReadOnlyCollection<SwitchCase> Cases { get; }

        /// <summary>
        /// Gets the test for the switch.
        /// </summary>
        public Expression DefaultBody { get; }

        /// <summary>
        /// Gets the equality comparison method, if any.
        /// </summary>
        public MethodInfo Comparison { get; }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitSwitch(this);
        }

        internal bool IsLifted
        {
            get
            {
                if (SwitchValue.Type.IsNullableType())
                {
                    return (Comparison == null) ||
                        !TypeUtils.AreEquivalent(SwitchValue.Type, Comparison.GetParametersCached()[0].ParameterType.GetNonRefType());
                }
                return false;
            }
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="switchValue">The <see cref="SwitchValue"/> property of the result.</param>
        /// <param name="cases">The <see cref="Cases"/> property of the result.</param>
        /// <param name="defaultBody">The <see cref="DefaultBody"/> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public SwitchExpression Update(Expression switchValue, IEnumerable<SwitchCase> cases, Expression defaultBody)
        {
            if (switchValue == SwitchValue && cases == Cases && defaultBody == DefaultBody)
            {
                return this;
            }
            return Expression.Switch(Type, switchValue, defaultBody, Comparison, cases);
        }
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="SwitchExpression"/>.
        /// </summary>
        /// <param name="switchValue">The value to be tested against each case.</param>
        /// <param name="cases">The valid cases for this switch.</param>
        /// <returns>The created <see cref="SwitchExpression"/>.</returns>
        public static SwitchExpression Switch(Expression switchValue, params SwitchCase[] cases)
        {
            return Switch(switchValue, null, null, (IEnumerable<SwitchCase>)cases);
        }

        /// <summary>
        /// Creates a <see cref="SwitchExpression"/>.
        /// </summary>
        /// <param name="switchValue">The value to be tested against each case.</param>
        /// <param name="defaultBody">The result of the switch if no cases are matched.</param>
        /// <param name="cases">The valid cases for this switch.</param>
        /// <returns>The created <see cref="SwitchExpression"/>.</returns>
        public static SwitchExpression Switch(Expression switchValue, Expression defaultBody, params SwitchCase[] cases)
        {
            return Switch(switchValue, defaultBody, null, (IEnumerable<SwitchCase>)cases);
        }

        /// <summary>
        /// Creates a <see cref="SwitchExpression"/>.
        /// </summary>
        /// <param name="switchValue">The value to be tested against each case.</param>
        /// <param name="defaultBody">The result of the switch if no cases are matched.</param>
        /// <param name="comparison">The equality comparison method to use.</param>
        /// <param name="cases">The valid cases for this switch.</param>
        /// <returns>The created <see cref="SwitchExpression"/>.</returns>
        public static SwitchExpression Switch(Expression switchValue, Expression defaultBody, MethodInfo comparison, params SwitchCase[] cases)
        {
            return Switch(switchValue, defaultBody, comparison, (IEnumerable<SwitchCase>)cases);
        }

        /// <summary>
        /// Creates a <see cref="SwitchExpression"/>.
        /// </summary>
        /// <param name="type">The result type of the switch.</param>
        /// <param name="switchValue">The value to be tested against each case.</param>
        /// <param name="defaultBody">The result of the switch if no cases are matched.</param>
        /// <param name="comparison">The equality comparison method to use.</param>
        /// <param name="cases">The valid cases for this switch.</param>
        /// <returns>The created <see cref="SwitchExpression"/>.</returns>
        public static SwitchExpression Switch(Type type, Expression switchValue, Expression defaultBody, MethodInfo comparison, params SwitchCase[] cases)
        {
            return Switch(type, switchValue, defaultBody, comparison, (IEnumerable<SwitchCase>)cases);
        }

        /// <summary>
        /// Creates a <see cref="SwitchExpression"/>.
        /// </summary>
        /// <param name="switchValue">The value to be tested against each case.</param>
        /// <param name="defaultBody">The result of the switch if no cases are matched.</param>
        /// <param name="comparison">The equality comparison method to use.</param>
        /// <param name="cases">The valid cases for this switch.</param>
        /// <returns>The created <see cref="SwitchExpression"/>.</returns>
        public static SwitchExpression Switch(Expression switchValue, Expression defaultBody, MethodInfo comparison, IEnumerable<SwitchCase> cases)
        {
            return Switch(null, switchValue, defaultBody, comparison, cases);
        }

        /// <summary>
        /// Creates a <see cref="SwitchExpression"/>.
        /// </summary>
        /// <param name="type">The result type of the switch.</param>
        /// <param name="switchValue">The value to be tested against each case.</param>
        /// <param name="defaultBody">The result of the switch if no cases are matched.</param>
        /// <param name="comparison">The equality comparison method to use.</param>
        /// <param name="cases">The valid cases for this switch.</param>
        /// <returns>The created <see cref="SwitchExpression"/>.</returns>
        public static SwitchExpression Switch(Type type, Expression switchValue, Expression defaultBody, MethodInfo comparison, IEnumerable<SwitchCase> cases)
        {
            RequiresCanRead(switchValue, nameof(switchValue));
            if (switchValue.Type == typeof(void)) throw Error.ArgumentCannotBeOfTypeVoid(nameof(switchValue));

            ReadOnlyCollection<SwitchCase> caseList = cases.ToReadOnly();
            ContractUtils.RequiresNotNullItems(caseList, nameof(cases));

            // Type of the result. Either provided, or it is type of the branches.
            Type resultType;
            if (type != null)
                resultType = type;
            else if (caseList.Count != 0)
                resultType = caseList[0].Body.Type;
            else if (defaultBody != null)
                resultType = defaultBody.Type;
            else
                resultType = typeof(void);
            bool customType = type != null;

            if (comparison != null)
            {
                ParameterInfo[] pms = comparison.GetParametersCached();
                if (pms.Length != 2)
                {
                    throw Error.IncorrectNumberOfMethodCallArguments(comparison, nameof(comparison));
                }
                // Validate that the switch value's type matches the comparison method's 
                // left hand side parameter type.
                ParameterInfo leftParam = pms[0];
                bool liftedCall = false;
                if (!ParameterIsAssignable(leftParam, switchValue.Type))
                {
                    liftedCall = ParameterIsAssignable(leftParam, switchValue.Type.GetNonNullableType());
                    if (!liftedCall)
                    {
                        throw Error.SwitchValueTypeDoesNotMatchComparisonMethodParameter(switchValue.Type, leftParam.ParameterType);
                    }
                }

                ParameterInfo rightParam = pms[1];
                foreach (SwitchCase c in caseList)
                {
                    ContractUtils.RequiresNotNull(c, nameof(cases));
                    ValidateSwitchCaseType(c.Body, customType, resultType, nameof(cases));
                    for (int i = 0; i < c.TestValues.Count; i++)
                    {
                        // When a comparison method is provided, test values can have different type but have to
                        // be reference assignable to the right hand side parameter of the method.
                        Type rightOperandType = c.TestValues[i].Type;
                        if (liftedCall)
                        {
                            if (!rightOperandType.IsNullableType())
                            {
                                throw Error.TestValueTypeDoesNotMatchComparisonMethodParameter(rightOperandType, rightParam.ParameterType);
                            }
                            rightOperandType = rightOperandType.GetNonNullableType();
                        }
                        if (!ParameterIsAssignable(rightParam, rightOperandType))
                        {
                            throw Error.TestValueTypeDoesNotMatchComparisonMethodParameter(rightOperandType, rightParam.ParameterType);
                        }
                    }
                }

                // if we have a non-boolean user-defined equals, we don't want it.
                if (comparison.ReturnType != typeof(bool))
                {
                    throw Error.EqualityMustReturnBoolean(comparison, nameof(comparison));
                }
            }
            else if (caseList.Count != 0)
            {
                // When comparison method is not present, all the test values must have
                // the same type. Use the first test value's type as the baseline.
                Expression firstTestValue = caseList[0].TestValues[0];
                foreach (SwitchCase c in caseList)
                {
                    ContractUtils.RequiresNotNull(c, nameof(cases));
                    ValidateSwitchCaseType(c.Body, customType, resultType, nameof(cases));
                    // When no comparison method is provided, require all test values to have the same type.
                    for (int i = 0; i < c.TestValues.Count; i++)
                    {
                        if (!TypeUtils.AreEquivalent(firstTestValue.Type, c.TestValues[i].Type))
                        {
                            throw Error.AllTestValuesMustHaveSameType(nameof(cases));
                        }
                    }
                }

                // Now we need to validate that switchValue.Type and testValueType
                // make sense in an Equal node. Fortunately, Equal throws a
                // reasonable error, so just call it.
                BinaryExpression equal = Equal(switchValue, firstTestValue, false, comparison);

                // Get the comparison function from equals node.
                comparison = equal.Method;
            }

            if (defaultBody == null)
            {
                if (resultType != typeof(void)) throw Error.DefaultBodyMustBeSupplied(nameof(defaultBody));
            }
            else
            {
                ValidateSwitchCaseType(defaultBody, customType, resultType, nameof(defaultBody));
            }

            return new SwitchExpression(resultType, switchValue, defaultBody, comparison, caseList);
        }


        /// <summary>
        /// If custom type is provided, all branches must be reference assignable to the result type.
        /// If no custom type is provided, all branches must have the same type - resultType.
        /// </summary>
        private static void ValidateSwitchCaseType(Expression @case, bool customType, Type resultType, string parameterName)
        {
            if (customType)
            {
                if (resultType != typeof(void))
                {
                    if (!TypeUtils.AreReferenceAssignable(resultType, @case.Type))
                    {
                        throw Error.ArgumentTypesMustMatch(parameterName);
                    }
                }
            }
            else
            {
                if (!TypeUtils.AreEquivalent(resultType, @case.Type))
                {
                    throw Error.AllCaseBodiesMustHaveSameType(parameterName);
                }
            }
        }
    }
}
