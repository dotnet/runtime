// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents a catch statement in a try block.
    /// This must have the same return type (i.e., the type of <see cref="Body"/>) as the try block it is associated with.
    /// </summary>
    [DebuggerTypeProxy(typeof(Expression.CatchBlockProxy))]
    public sealed class CatchBlock
    {
        internal CatchBlock(Type test, ParameterExpression variable, Expression body, Expression filter)
        {
            Test = test;
            Variable = variable;
            Body = body;
            Filter = filter;
        }

        /// <summary>
        /// Gets a reference to the <see cref="Exception"/> object caught by this handler.
        /// </summary>
        public ParameterExpression Variable { get; }

        /// <summary>
        /// Gets the type of <see cref="Exception"/> this handler catches.
        /// </summary>
        public Type Test { get; }

        /// <summary>
        /// Gets the body of the catch block.
        /// </summary>
        public Expression Body { get; }

        /// <summary>
        /// Gets the body of the <see cref="CatchBlock"/>'s filter.
        /// </summary>
        public Expression Filter { get; }

        /// <summary>
        /// Returns a <see cref="String"/> that represents the current <see cref="Object"/>.
        /// </summary>
        /// <returns>A <see cref="String"/> that represents the current <see cref="Object"/>.</returns>
        public override string ToString()
        {
            return ExpressionStringBuilder.CatchBlockToString(this);
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="variable">The <see cref="Variable"/> property of the result.</param>
        /// <param name="filter">The <see cref="Filter"/> property of the result.</param>
        /// <param name="body">The <see cref="Body"/> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public CatchBlock Update(ParameterExpression variable, Expression filter, Expression body)
        {
            if (variable == Variable && filter == Filter && body == Body)
            {
                return this;
            }
            return Expression.MakeCatchBlock(Test, variable, body, filter);
        }
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="CatchBlock"/> representing a catch statement.
        /// The <see cref="Type"/> of object to be caught can be specified but no reference to the object
        /// will be available for use in the <see cref="CatchBlock"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="Exception"/> this <see cref="CatchBlock"/> will handle.</param>
        /// <param name="body">The body of the catch statement.</param>
        /// <returns>The created <see cref="CatchBlock"/>.</returns>
        public static CatchBlock Catch(Type type, Expression body)
        {
            return MakeCatchBlock(type, null, body, filter: null);
        }

        /// <summary>
        /// Creates a <see cref="CatchBlock"/> representing a catch statement with a reference to the caught object for use in the handler body.
        /// </summary>
        /// <param name="variable">A <see cref="ParameterExpression"/> representing a reference to the <see cref="Exception"/> object caught by this handler.</param>
        /// <param name="body">The body of the catch statement.</param>
        /// <returns>The created <see cref="CatchBlock"/>.</returns>
        public static CatchBlock Catch(ParameterExpression variable, Expression body)
        {
            ContractUtils.RequiresNotNull(variable, nameof(variable));
            return MakeCatchBlock(variable.Type, variable, body, filter: null);
        }

        /// <summary>
        /// Creates a <see cref="CatchBlock"/> representing a catch statement with
        /// an <see cref="Exception"/> filter but no reference to the caught <see cref="Exception"/> object.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="Exception"/> this <see cref="CatchBlock"/> will handle.</param>
        /// <param name="body">The body of the catch statement.</param>
        /// <param name="filter">The body of the <see cref="Exception"/> filter.</param>
        /// <returns>The created <see cref="CatchBlock"/>.</returns>
        public static CatchBlock Catch(Type type, Expression body, Expression filter)
        {
            return MakeCatchBlock(type, null, body, filter);
        }

        /// <summary>
        /// Creates a <see cref="CatchBlock"/> representing a catch statement with
        /// an <see cref="Exception"/> filter and a reference to the caught <see cref="Exception"/> object.
        /// </summary>
        /// <param name="variable">A <see cref="ParameterExpression"/> representing a reference to the <see cref="Exception"/> object caught by this handler.</param>
        /// <param name="body">The body of the catch statement.</param>
        /// <param name="filter">The body of the <see cref="Exception"/> filter.</param>
        /// <returns>The created <see cref="CatchBlock"/>.</returns>
        public static CatchBlock Catch(ParameterExpression variable, Expression body, Expression filter)
        {
            ContractUtils.RequiresNotNull(variable, nameof(variable));
            return MakeCatchBlock(variable.Type, variable, body, filter);
        }

        /// <summary>
        /// Creates a <see cref="CatchBlock"/> representing a catch statement with the specified elements.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="Exception"/> this <see cref="CatchBlock"/> will handle.</param>
        /// <param name="variable">A <see cref="ParameterExpression"/> representing a reference to the <see cref="Exception"/> object caught by this handler.</param>
        /// <param name="body">The body of the catch statement.</param>
        /// <param name="filter">The body of the <see cref="Exception"/> filter.</param>
        /// <returns>The created <see cref="CatchBlock"/>.</returns>
        /// <remarks><paramref name="type"/> must be non-null and match the type of <paramref name="variable"/> (if it is supplied).</remarks>
        public static CatchBlock MakeCatchBlock(Type type, ParameterExpression variable, Expression body, Expression filter)
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
