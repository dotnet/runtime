// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>Represents a catch statement in a try block.</summary>
    /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Catch" /> methods can be used to create a <see cref="System.Linq.Expressions.CatchBlock" />.</remarks>
    [DebuggerTypeProxy(typeof(Expression.CatchBlockProxy))]
    public sealed class CatchBlock
    {
        internal CatchBlock(Type test, ParameterExpression? variable, Expression body, Expression? filter)
        {
            Test = test;
            Variable = variable;
            Body = body;
            Filter = filter;
        }

        /// <summary>Gets a reference to the <see cref="System.Exception" /> object caught by this handler.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ParameterExpression" /> object representing a reference to the <see cref="System.Exception" /> object caught by this handler.</value>
        public ParameterExpression? Variable { get; }

        /// <summary>Gets the type of <see cref="System.Exception" /> this handler catches.</summary>
        /// <value>The <see cref="System.Type" /> object representing the type of <see cref="System.Exception" /> this handler catches.</value>
        public Type Test { get; }

        /// <summary>Gets the body of the catch block.</summary>
        /// <value>The <see cref="System.Linq.Expressions.Expression" /> object representing the catch body.</value>
        public Expression Body { get; }

        /// <summary>Gets the body of the <see cref="System.Linq.Expressions.CatchBlock" /> filter.</summary>
        /// <value>The <see cref="System.Linq.Expressions.Expression" /> object representing the body of the <see cref="System.Linq.Expressions.CatchBlock" /> filter.</value>
        public Expression? Filter { get; }

        /// <summary>Returns a <see cref="string" /> that represents the current <see cref="object" />.</summary>
        /// <returns>A <see cref="string" /> that represents the current <see cref="object" />.</returns>
        public override string ToString()
        {
            return ExpressionStringBuilder.CatchBlockToString(this);
        }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="variable">The <see cref="System.Linq.Expressions.CatchBlock.Variable" /> property of the result.</param>
        /// <param name="filter">The <see cref="System.Linq.Expressions.CatchBlock.Filter" /> property of the result.</param>
        /// <param name="body">The <see cref="System.Linq.Expressions.CatchBlock.Body" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public CatchBlock Update(ParameterExpression? variable, Expression? filter, Expression body)
        {
            if (variable == Variable && filter == Filter && body == Body)
            {
                return this;
            }
            return Expression.MakeCatchBlock(Test, variable, body, filter);
        }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.CatchBlock" /> representing a catch statement.</summary>
        /// <param name="type">The <see cref="System.Linq.Expressions.Expression.Type" /> of <see cref="System.Exception" /> this <see cref="System.Linq.Expressions.CatchBlock" /> will handle.</param>
        /// <param name="body">The body of the catch statement.</param>
        /// <returns>The created <see cref="System.Linq.Expressions.CatchBlock" />.</returns>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> of <see cref="System.Exception" /> to be caught can be specified but no reference to the <see cref="System.Exception" /> object will be available for use in the <see cref="System.Linq.Expressions.CatchBlock" />.</remarks>
        public static CatchBlock Catch(Type type, Expression body)
        {
            return MakeCatchBlock(type, null, body, filter: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.CatchBlock" /> representing a catch statement with a reference to the caught <see cref="System.Exception" /> object for use in the handler body.</summary>
        /// <param name="variable">A <see cref="System.Linq.Expressions.ParameterExpression" /> representing a reference to the <see cref="System.Exception" /> object caught by this handler.</param>
        /// <param name="body">The body of the catch statement.</param>
        /// <returns>The created <see cref="System.Linq.Expressions.CatchBlock" />.</returns>
        public static CatchBlock Catch(ParameterExpression variable, Expression body)
        {
            ContractUtils.RequiresNotNull(variable, nameof(variable));
            return MakeCatchBlock(variable.Type, variable, body, filter: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.CatchBlock" /> representing a catch statement with an <see cref="System.Exception" /> filter but no reference to the caught <see cref="System.Exception" /> object.</summary>
        /// <param name="type">The <see cref="System.Linq.Expressions.Expression.Type" /> of <see cref="System.Exception" /> this <see cref="System.Linq.Expressions.CatchBlock" /> will handle.</param>
        /// <param name="body">The body of the catch statement.</param>
        /// <param name="filter">The body of the <see cref="System.Exception" /> filter.</param>
        /// <returns>The created <see cref="System.Linq.Expressions.CatchBlock" />.</returns>
        public static CatchBlock Catch(Type type, Expression body, Expression? filter)
        {
            return MakeCatchBlock(type, null, body, filter);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.CatchBlock" /> representing a catch statement with an <see cref="System.Exception" /> filter and a reference to the caught <see cref="System.Exception" /> object.</summary>
        /// <param name="variable">A <see cref="System.Linq.Expressions.ParameterExpression" /> representing a reference to the <see cref="System.Exception" /> object caught by this handler.</param>
        /// <param name="body">The body of the catch statement.</param>
        /// <param name="filter">The body of the <see cref="System.Exception" /> filter.</param>
        /// <returns>The created <see cref="System.Linq.Expressions.CatchBlock" />.</returns>
        public static CatchBlock Catch(ParameterExpression variable, Expression body, Expression? filter)
        {
            ContractUtils.RequiresNotNull(variable, nameof(variable));
            return MakeCatchBlock(variable.Type, variable, body, filter);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.CatchBlock" /> representing a catch statement with the specified elements.</summary>
        /// <param name="type">The <see cref="System.Linq.Expressions.Expression.Type" /> of <see cref="System.Exception" /> this <see cref="System.Linq.Expressions.CatchBlock" /> will handle.</param>
        /// <param name="variable">A <see cref="System.Linq.Expressions.ParameterExpression" /> representing a reference to the <see cref="System.Exception" /> object caught by this handler.</param>
        /// <param name="body">The body of the catch statement.</param>
        /// <param name="filter">The body of the <see cref="System.Exception" /> filter.</param>
        /// <returns>The created <see cref="System.Linq.Expressions.CatchBlock" />.</returns>
        /// <remarks><paramref name="type" /> must be non-null and match the type of <paramref name="variable" /> (if it is supplied).</remarks>
        public static CatchBlock MakeCatchBlock(Type type, ParameterExpression? variable, Expression body, Expression? filter)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            ContractUtils.Requires(variable == null || TypeUtils.AreEquivalent(variable.Type, type), nameof(variable));
            if (variable == null)
            {
                TypeUtils.ValidateType(type, nameof(type));
            }
            else if (variable.IsByRef)
            {
                throw Error.VariableMustNotBeByRef(variable, variable.Type, nameof(variable));
            }
            ExpressionUtils.RequiresCanRead(body, nameof(body));
            if (filter != null)
            {
                ExpressionUtils.RequiresCanRead(filter, nameof(filter));
                if (filter.Type != typeof(bool)) throw Error.ArgumentMustBeBoolean(nameof(filter));
            }

            return new CatchBlock(type, variable, body, filter);
        }
    }
}
