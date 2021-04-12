// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>Represents one case of a <see cref="System.Linq.Expressions.SwitchExpression" />.</summary>
    /// <remarks></remarks>
    /// <example>The following example demonstrates how to create an expression that represents a switch statement without a default case by using the <see cref="O:System.Linq.Expressions.Expression.SwitchCase" /> method.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet34":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet34":::</example>
    [DebuggerTypeProxy(typeof(Expression.SwitchCaseProxy))]
    public sealed class SwitchCase
    {
        internal SwitchCase(Expression body, ReadOnlyCollection<Expression> testValues)
        {
            Body = body;
            TestValues = testValues;
        }

        /// <summary>Gets the values of this case. This case is selected for execution when the <see cref="System.Linq.Expressions.SwitchExpression.SwitchValue" /> matches any of these values.</summary>
        /// <value>The read-only collection of the values for this case block.</value>
        public ReadOnlyCollection<Expression> TestValues { get; }

        /// <summary>Gets the body of this case.</summary>
        /// <value>The <see cref="System.Linq.Expressions.Expression" /> object that represents the body of the case block.</value>
        public Expression Body { get; }

        /// <summary>Returns a <see cref="string" /> that represents the current <see cref="object" />.</summary>
        /// <returns>A <see cref="string" /> that represents the current <see cref="object" />.</returns>
        public override string ToString()
        {
            return ExpressionStringBuilder.SwitchCaseToString(this);
        }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="testValues">The <see cref="System.Linq.Expressions.SwitchCase.TestValues" /> property of the result.</param>
        /// <param name="body">The <see cref="System.Linq.Expressions.SwitchCase.Body" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public SwitchCase Update(IEnumerable<Expression> testValues, Expression body)
        {
            if (body == Body & testValues != null)
            {
                if (ExpressionUtils.SameElements(ref testValues!, TestValues))
                {
                    return this;
                }
            }

            return Expression.SwitchCase(body, testValues!);
        }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.SwitchCase" /> for use in a <see cref="System.Linq.Expressions.SwitchExpression" />.</summary>
        /// <param name="body">The body of the case.</param>
        /// <param name="testValues">The test values of the case.</param>
        /// <returns>The created <see cref="System.Linq.Expressions.SwitchCase" />.</returns>
        public static SwitchCase SwitchCase(Expression body, params Expression[] testValues)
        {
            return SwitchCase(body, (IEnumerable<Expression>)testValues);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.SwitchCase" /> object to be used in a <see cref="System.Linq.Expressions.SwitchExpression" /> object.</summary>
        /// <param name="body">The body of the case.</param>
        /// <param name="testValues">The test values of the case.</param>
        /// <returns>The created <see cref="System.Linq.Expressions.SwitchCase" />.</returns>
        /// <remarks>All <see cref="System.Linq.Expressions.SwitchCase" /> objects in a <see cref="System.Linq.Expressions.SwitchExpression" /> object must have the same type, unless the <see cref="System.Linq.Expressions.SwitchExpression" /> has the type `void`.
        /// Each <see cref="System.Linq.Expressions.SwitchCase" /> object has an implicit `break` statement, which means that there is no implicit fall through from one case label to another.</remarks>
        /// <example>The following example demonstrates how to create an expression that represents a switch statement that has a default case.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet35":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet35":::</example>
        public static SwitchCase SwitchCase(Expression body, IEnumerable<Expression> testValues)
        {
            ExpressionUtils.RequiresCanRead(body, nameof(body));

            ReadOnlyCollection<Expression> values = testValues.ToReadOnly();
            ContractUtils.RequiresNotEmpty(values, nameof(testValues));
            RequiresCanRead(values, nameof(testValues));

            return new SwitchCase(body, values);
        }
    }
}
