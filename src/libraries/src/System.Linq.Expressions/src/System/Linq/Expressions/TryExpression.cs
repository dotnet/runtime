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
    /// Represents a try/catch/finally/fault block.
    /// 
    /// The body is protected by the try block.
    /// The handlers consist of a set of <see cref="CatchBlock"/>s that can either be catch or filters.
    /// The fault runs if an exception is thrown.
    /// The finally runs regardless of how control exits the body.
    /// Only one of fault or finally can be supplied.
    /// The return type of the try block must match the return type of any associated catch statements.
    /// </summary>
    [DebuggerTypeProxy(typeof(TryExpressionProxy))]
    public sealed class TryExpression : Expression
    {
        internal TryExpression(Type type, Expression body, Expression @finally, Expression fault, ReadOnlyCollection<CatchBlock> handlers)
        {
            Type = type;
            Body = body;
            Handlers = handlers;
            Finally = @finally;
            Fault = fault;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression"/> represents. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="System.Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type { get; }

        /// <summary>
        /// Returns the node type of this <see cref="Expression"/>. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> that represents this expression.</returns>
        public sealed override ExpressionType NodeType => ExpressionType.Try;

        /// <summary>
        /// Gets the <see cref="Expression"/> representing the body of the try block.
        /// </summary>
        public Expression Body { get; }

        /// <summary>
        /// Gets the collection of <see cref="CatchBlock"/>s associated with the try block.
        /// </summary>
        public ReadOnlyCollection<CatchBlock> Handlers { get; }

        /// <summary>
        /// Gets the <see cref="Expression"/> representing the finally block.
        /// </summary>
        public Expression Finally { get; }

        /// <summary>
        /// Gets the <see cref="Expression"/> representing the fault block.
        /// </summary>
        public Expression Fault { get; }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitTry(this);
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="body">The <see cref="Body"/> property of the result.</param>
        /// <param name="handlers">The <see cref="Handlers"/> property of the result.</param>
        /// <param name="finally">The <see cref="Finally"/> property of the result.</param>
        /// <param name="fault">The <see cref="Fault"/> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public TryExpression Update(Expression body, IEnumerable<CatchBlock> handlers, Expression @finally, Expression fault)
        {
            if (body == Body && handlers == Handlers && @finally == Finally && fault == Fault)
            {
                return this;
            }
            return Expression.MakeTry(Type, body, @finally, fault, handlers);
        }
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="TryExpression"/> representing a try block with a fault block and no catch statements.
        /// </summary>
        /// <param name="body">The body of the try block.</param>
        /// <param name="fault">The body of the fault block.</param>
        /// <returns>The created <see cref="TryExpression"/>.</returns>
        public static TryExpression TryFault(Expression body, Expression fault)
        {
            return MakeTry(null, body, null, fault, handlers: null);
        }

        /// <summary>
        /// Creates a <see cref="TryExpression"/> representing a try block with a finally block and no catch statements.
        /// </summary>
        /// <param name="body">The body of the try block.</param>
        /// <param name="finally">The body of the finally block.</param>
        /// <returns>The created <see cref="TryExpression"/>.</returns>
        public static TryExpression TryFinally(Expression body, Expression @finally)
        {
            return MakeTry(null, body, @finally, fault: null, handlers: null);
        }

        /// <summary>
        /// Creates a <see cref="TryExpression"/> representing a try block with any number of catch statements and neither a fault nor finally block.
        /// </summary>
        /// <param name="body">The body of the try block.</param>
        /// <param name="handlers">The array of zero or more <see cref="CatchBlock"/>s representing the catch statements to be associated with the try block.</param>
        /// <returns>The created <see cref="TryExpression"/>.</returns>
        public static TryExpression TryCatch(Expression body, params CatchBlock[] handlers)
        {
            return MakeTry(null, body, null, null, handlers);
        }

        /// <summary>
        /// Creates a <see cref="TryExpression"/> representing a try block with any number of catch statements and a finally block.
        /// </summary>
        /// <param name="body">The body of the try block.</param>
        /// <param name="finally">The body of the finally block.</param>
        /// <param name="handlers">The array of zero or more <see cref="CatchBlock"/>s representing the catch statements to be associated with the try block.</param>
        /// <returns>The created <see cref="TryExpression"/>.</returns>
        public static TryExpression TryCatchFinally(Expression body, Expression @finally, params CatchBlock[] handlers)
        {
            return MakeTry(null, body, @finally, null, handlers);
        }

        /// <summary>
        /// Creates a <see cref="TryExpression"/> representing a try block with the specified elements.
        /// </summary>
        /// <param name="type">The result type of the try expression. If null, body and all handlers must have identical type.</param>
        /// <param name="body">The body of the try block.</param>
        /// <param name="finally">The body of the finally block. Pass null if the try block has no finally block associated with it.</param>
        /// <param name="fault">The body of the t block. Pass null if the try block has no fault block associated with it.</param>
        /// <param name="handlers">A collection of <see cref="CatchBlock"/>s representing the catch statements to be associated with the try block.</param>
        /// <returns>The created <see cref="TryExpression"/>.</returns>
        public static TryExpression MakeTry(Type type, Expression body, Expression @finally, Expression fault, IEnumerable<CatchBlock> handlers)
        {
            RequiresCanRead(body, nameof(body));

            ReadOnlyCollection<CatchBlock> @catch = handlers.ToReadOnly();
            ContractUtils.RequiresNotNullItems(@catch, nameof(handlers));
            ValidateTryAndCatchHaveSameType(type, body, @catch);

            if (fault != null)
            {
                if (@finally != null || @catch.Count > 0)
                {
                    throw Error.FaultCannotHaveCatchOrFinally(nameof(fault));
                }
                RequiresCanRead(fault, nameof(fault));
            }
            else if (@finally != null)
            {
                RequiresCanRead(@finally, nameof(@finally));
            }
            else if (@catch.Count == 0)
            {
                throw Error.TryMustHaveCatchFinallyOrFault();
            }

            return new TryExpression(type ?? body.Type, body, @finally, fault, @catch);
        }

        //Validate that the body of the try expression must have the same type as the body of every try block.
        private static void ValidateTryAndCatchHaveSameType(Type type, Expression tryBody, ReadOnlyCollection<CatchBlock> handlers)
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
