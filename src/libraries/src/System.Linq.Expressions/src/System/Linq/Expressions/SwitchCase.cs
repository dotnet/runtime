// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents one case of a <see cref="SwitchExpression"/>.
    /// </summary>
    [DebuggerTypeProxy(typeof(Expression.SwitchCaseProxy))]
    public sealed class SwitchCase
    {
        internal SwitchCase(Expression body, ReadOnlyCollection<Expression> testValues)
        {
            Body = body;
            TestValues = testValues;
        }

        /// <summary>
        /// Gets the values of this case. This case is selected for execution when the <see cref="SwitchExpression.SwitchValue"/> matches any of these values.
        /// </summary>
        public ReadOnlyCollection<Expression> TestValues { get; }

        /// <summary>
        /// Gets the body of this case.
        /// </summary>
        public Expression Body { get; }

        /// <summary>
        /// Returns a <see cref="String"/> that represents the current <see cref="Object"/>.
        /// </summary>
        /// <returns>A <see cref="String"/> that represents the current <see cref="Object"/>.</returns>
        public override string ToString()
        {
            return ExpressionStringBuilder.SwitchCaseToString(this);
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="testValues">The <see cref="TestValues"/> property of the result.</param>
        /// <param name="body">The <see cref="Body"/> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public SwitchCase Update(IEnumerable<Expression> testValues, Expression body)
        {
            if (testValues == TestValues && body == Body)
            {
                return this;
            }
            return Expression.SwitchCase(body, testValues);
        }
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="Expressions.SwitchCase"/> for use in a <see cref="SwitchExpression"/>.
        /// </summary>
        /// <param name="body">The body of the case.</param>
        /// <param name="testValues">The test values of the case.</param>
        /// <returns>The created <see cref="Expressions.SwitchCase"/>.</returns>
        public static SwitchCase SwitchCase(Expression body, params Expression[] testValues)
        {
            return SwitchCase(body, (IEnumerable<Expression>)testValues);
        }

        /// <summary>
        /// Creates a <see cref="Expressions.SwitchCase"/> for use in a <see cref="SwitchExpression"/>.
        /// </summary>
        /// <param name="body">The body of the case.</param>
        /// <param name="testValues">The test values of the case.</param>
        /// <returns>The created <see cref="Expressions.SwitchCase"/>.</returns>
        public static SwitchCase SwitchCase(Expression body, IEnumerable<Expression> testValues)
        {
            RequiresCanRead(body, nameof(body));

            ReadOnlyCollection<Expression> values = testValues.ToReadOnly();
            ContractUtils.RequiresNotEmpty(values, nameof(testValues));
            RequiresCanRead(values, nameof(testValues));

            return new SwitchCase(body, values);
        }
    }
}
