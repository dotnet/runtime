// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>Represents a try/catch/finally/fault block.</summary>
    /// <remarks>The body block is protected by the try block.
    /// The handlers consist of a set of <see cref="System.Linq.Expressions.CatchBlock" /> expressions that can be either catch statements or filters.
    /// The fault block runs if an exception is thrown.
    /// The finally block runs regardless of how control exits the body.
    /// Only one of fault or finally blocks can be supplied.
    /// The return type of the try block must match the return type of any associated catch statements.</remarks>
    /// <example>The following example demonstrates how to create a <see cref="System.Linq.Expressions.TryExpression" /> object that contains a catch statement by using the <see cref="O:System.Linq.Expressions.Expression.TryCatch" /> method.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet47":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet47":::</example>
    [DebuggerTypeProxy(typeof(TryExpressionProxy))]
    public sealed class TryExpression : Expression
    {
        internal TryExpression(Type type, Expression body, Expression? @finally, Expression? fault, ReadOnlyCollection<CatchBlock> handlers)
        {
            Type = type;
            Body = body;
            Handlers = handlers;
            Finally = @finally;
            Fault = fault;
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.TryExpression.Type" /> that represents the static type of the expression.</value>
        public sealed override Type Type { get; }

        /// <summary>Returns the node type of this <see cref="System.Linq.Expressions.Expression" />.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> that represents this expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.Try;

        /// <summary>Gets the <see cref="System.Linq.Expressions.Expression" /> representing the body of the try block.</summary>
        /// <value>The <see cref="System.Linq.Expressions.Expression" /> representing the body of the try block.</value>
        public Expression Body { get; }

        /// <summary>Gets the collection of <see cref="System.Linq.Expressions.CatchBlock" /> expressions associated with the try block.</summary>
        /// <value>The collection of <see cref="System.Linq.Expressions.CatchBlock" /> expressions associated with the try block.</value>
        public ReadOnlyCollection<CatchBlock> Handlers { get; }

        /// <summary>Gets the <see cref="System.Linq.Expressions.Expression" /> representing the finally block.</summary>
        /// <value>The <see cref="System.Linq.Expressions.Expression" /> representing the finally block.</value>
        public Expression? Finally { get; }

        /// <summary>Gets the <see cref="System.Linq.Expressions.Expression" /> representing the fault block.</summary>
        /// <value>The <see cref="System.Linq.Expressions.Expression" /> representing the fault block.</value>
        public Expression? Fault { get; }

        /// <summary>Dispatches to the specific visit method for this node type.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitTry(this);
        }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="body">The <see cref="System.Linq.Expressions.TryExpression.Body" /> property of the result.</param>
        /// <param name="handlers">The <see cref="System.Linq.Expressions.TryExpression.Handlers" /> property of the result.</param>
        /// <param name="finally">The <see cref="System.Linq.Expressions.TryExpression.Finally" /> property of the result.</param>
        /// <param name="fault">The <see cref="System.Linq.Expressions.TryExpression.Fault" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public TryExpression Update(Expression body, IEnumerable<CatchBlock>? handlers, Expression? @finally, Expression? fault)
        {
            if (body == Body & @finally == Finally & fault == Fault)
            {
                if (ExpressionUtils.SameElements(ref handlers!, Handlers))
                {
                    return this;
                }
            }

            return MakeTry(Type, body, @finally, fault, handlers);
        }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.TryExpression" /> representing a try block with a fault block and no catch statements.</summary>
        /// <param name="body">The body of the try block.</param>
        /// <param name="fault">The body of the fault block.</param>
        /// <returns>The created <see cref="System.Linq.Expressions.TryExpression" />.</returns>
        public static TryExpression TryFault(Expression body, Expression? fault)
        {
            return MakeTry(null, body, null, fault, handlers: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.TryExpression" /> representing a try block with a finally block and no catch statements.</summary>
        /// <param name="body">The body of the try block.</param>
        /// <param name="finally">The body of the finally block.</param>
        /// <returns>The created <see cref="System.Linq.Expressions.TryExpression" />.</returns>
        public static TryExpression TryFinally(Expression body, Expression? @finally)
        {
            return MakeTry(null, body, @finally, fault: null, handlers: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.TryExpression" /> representing a try block with any number of catch statements and neither a fault nor finally block.</summary>
        /// <param name="body">The body of the try block.</param>
        /// <param name="handlers">The array of zero or more <see cref="System.Linq.Expressions.CatchBlock" /> expressions representing the catch statements to be associated with the try block.</param>
        /// <returns>The created <see cref="System.Linq.Expressions.TryExpression" />.</returns>
        /// <remarks></remarks>
        /// <example>The following example demonstrates how to create a <see cref="System.Linq.Expressions.TryExpression" /> object that contains a catch statement.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet47":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet47":::</example>
        public static TryExpression TryCatch(Expression body, params CatchBlock[]? handlers)
        {
            return MakeTry(null, body, null, null, handlers);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.TryExpression" /> representing a try block with any number of catch statements and a finally block.</summary>
        /// <param name="body">The body of the try block.</param>
        /// <param name="finally">The body of the finally block.</param>
        /// <param name="handlers">The array of zero or more <see cref="System.Linq.Expressions.CatchBlock" /> expressions representing the catch statements to be associated with the try block.</param>
        /// <returns>The created <see cref="System.Linq.Expressions.TryExpression" />.</returns>
        /// <remarks></remarks>
        /// <example>The following example demonstrates how to create a <see cref="System.Linq.Expressions.TryExpression" /> object that contains a catch statement and a finally statement.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet48":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet48":::</example>
        public static TryExpression TryCatchFinally(Expression body, Expression? @finally, params CatchBlock[]? handlers)
        {
            return MakeTry(null, body, @finally, null, handlers);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.TryExpression" /> representing a try block with the specified elements.</summary>
        /// <param name="type">The result type of the try expression. If null, body and all handlers must have identical type.</param>
        /// <param name="body">The body of the try block.</param>
        /// <param name="finally">The body of the finally block. Pass null if the try block has no finally block associated with it.</param>
        /// <param name="fault">The body of the fault block. Pass null if the try block has no fault block associated with it.</param>
        /// <param name="handlers">A collection of <see cref="System.Linq.Expressions.CatchBlock" />s representing the catch statements to be associated with the try block.</param>
        /// <returns>The created <see cref="System.Linq.Expressions.TryExpression" />.</returns>
        public static TryExpression MakeTry(Type? type, Expression body, Expression? @finally, Expression? fault, IEnumerable<CatchBlock>? handlers)
        {
            ExpressionUtils.RequiresCanRead(body, nameof(body));

            ReadOnlyCollection<CatchBlock> @catch = handlers.ToReadOnly();
            ContractUtils.RequiresNotNullItems(@catch, nameof(handlers));
            ValidateTryAndCatchHaveSameType(type, body, @catch);

            if (fault != null)
            {
                if (@finally != null || @catch.Count > 0)
                {
                    throw Error.FaultCannotHaveCatchOrFinally(nameof(fault));
                }
                ExpressionUtils.RequiresCanRead(fault, nameof(fault));
            }
            else if (@finally != null)
            {
                ExpressionUtils.RequiresCanRead(@finally, nameof(@finally));
            }
            else if (@catch.Count == 0)
            {
                throw Error.TryMustHaveCatchFinallyOrFault();
            }

            return new TryExpression(type ?? body.Type, body, @finally, fault, @catch);
        }

        //Validate that the body of the try expression must have the same type as the body of every try block.
        private static void ValidateTryAndCatchHaveSameType(Type? type, Expression tryBody, ReadOnlyCollection<CatchBlock> handlers)
        {
            Debug.Assert(tryBody != null);
            // Type unification ... all parts must be reference assignable to "type"
            if (type != null)
            {
                if (type != typeof(void))
                {
                    if (!TypeUtils.AreReferenceAssignable(type, tryBody.Type))
                    {
                        throw Error.ArgumentTypesMustMatch();
                    }
                    foreach (CatchBlock cb in handlers)
                    {
                        if (!TypeUtils.AreReferenceAssignable(type, cb.Body.Type))
                        {
                            throw Error.ArgumentTypesMustMatch();
                        }
                    }
                }
            }
            else if (tryBody.Type == typeof(void))
            {
                //The body of every try block must be null or have void type.
                foreach (CatchBlock cb in handlers)
                {
                    Debug.Assert(cb.Body != null);
                    if (cb.Body.Type != typeof(void))
                    {
                        throw Error.BodyOfCatchMustHaveSameTypeAsBodyOfTry();
                    }
                }
            }
            else
            {
                //Body of every catch must have the same type of body of try.
                type = tryBody.Type;
                foreach (CatchBlock cb in handlers)
                {
                    Debug.Assert(cb.Body != null);
                    if (!TypeUtils.AreEquivalent(cb.Body.Type, type))
                    {
                        throw Error.BodyOfCatchMustHaveSameTypeAsBodyOfTry();
                    }
                }
            }
        }
    }
}
