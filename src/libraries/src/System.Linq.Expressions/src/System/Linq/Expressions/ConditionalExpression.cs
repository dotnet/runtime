// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Dynamic.Utils;
using System.Diagnostics;

using AstUtils = System.Linq.Expressions.Utils;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents an expression that has a conditional operator.
    /// </summary>
    [DebuggerTypeProxy(typeof(Expression.ConditionalExpressionProxy))]
    public class ConditionalExpression : Expression
    {
        private readonly Expression _test;
        private readonly Expression _true;

        internal ConditionalExpression(Expression test, Expression ifTrue)
        {
            _test = test;
            _true = ifTrue;
        }

        internal static ConditionalExpression Make(Expression test, Expression ifTrue, Expression ifFalse, Type type)
        {
            if (ifTrue.Type != type || ifFalse.Type != type)
            {
                return new FullConditionalExpressionWithType(test, ifTrue, ifFalse, type);
            }
            if (ifFalse is DefaultExpression && ifFalse.Type == typeof(void))
            {
                return new ConditionalExpression(test, ifTrue);
            }
            else
            {
                return new FullConditionalExpression(test, ifTrue, ifFalse);
            }
        }

        /// <summary>
        /// Returns the node type of this Expression. Extension nodes should return
        /// ExpressionType.Extension when overriding this method.
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> of the expression.</returns>
        public sealed override ExpressionType NodeType
        {
            get { return ExpressionType.Conditional; }
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression" /> represents.
        /// </summary>
        /// <returns>The <see cref="Type"/> that represents the static type of the expression.</returns>
        public override Type Type
        {
            get { return IfTrue.Type; }
        }

        /// <summary>
        /// Gets the test of the conditional operation.
        /// </summary>
        public Expression Test
        {
            get { return _test; }
        }
        /// <summary>
        /// Gets the expression to execute if the test evaluates to true.
        /// </summary>
        public Expression IfTrue
        {
            get { return _true; }
        }
        /// <summary>
        /// Gets the expression to execute if the test evaluates to false.
        /// </summary>
        public Expression IfFalse
        {
            get { return GetFalse(); }
        }

        internal virtual Expression GetFalse()
        {
            // Using a singleton here to ensure a stable object identity for IfFalse, which Update relies on.
            return AstUtils.Empty();
        }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitConditional(this);
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="test">The <see cref="Test" /> property of the result.</param>
        /// <param name="ifTrue">The <see cref="IfTrue" /> property of the result.</param>
        /// <param name="ifFalse">The <see cref="IfFalse" /> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public ConditionalExpression Update(Expression test, Expression ifTrue, Expression ifFalse)
        {
            if (test == Test && ifTrue == IfTrue && ifFalse == IfFalse)
            {
                return this;
            }
            return Expression.Condition(test, ifTrue, ifFalse, Type);
        }
    }

    internal class FullConditionalExpression : ConditionalExpression
    {
        private readonly Expression _false;

        internal FullConditionalExpression(Expression test, Expression ifTrue, Expression ifFalse)
            : base(test, ifTrue)
        {
            _false = ifFalse;
        }

        internal override Expression GetFalse()
        {
            return _false;
        }
    }

    internal class FullConditionalExpressionWithType : FullConditionalExpression
    {
        private readonly Type _type;

        internal FullConditionalExpressionWithType(Expression test, Expression ifTrue, Expression ifFalse, Type type)
            : base(test, ifTrue, ifFalse)
        {
            _type = type;
        }

        public sealed override Type Type
        {
            get { return _type; }
        }
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="ConditionalExpression"/>.
        /// </summary>
        /// <param name="test">An <see cref="Expression"/> to set the <see cref="P:ConditionalExpression.Test"/> property equal to.</param>
        /// <param name="ifTrue">An <see cref="Expression"/> to set the <see cref="P:ConditionalExpression.IfTrue"/> property equal to.</param>
        /// <param name="ifFalse">An <see cref="Expression"/> to set the <see cref="P:ConditionalExpression.IfFalse"/> property equal to.</param>
        /// <returns>A <see cref="ConditionalExpression"/> that has the <see cref="P:Expression.NodeType"/> property equal to 
        /// <see cref="F:ExpressionType.Conditional"/> and the <see cref="P:ConditionalExpression.Test"/>, <see cref="P:ConditionalExpression.IfTrue"/>, 
        /// and <see cref="P:ConditionalExpression.IfFalse"/> properties set to the specified values.</returns>
        public static ConditionalExpression Condition(Expression test, Expression ifTrue, Expression ifFalse)
        {
            RequiresCanRead(test, "test");
            RequiresCanRead(ifTrue, "ifTrue");
            RequiresCanRead(ifFalse, "ifFalse");

            if (test.Type != typeof(bool))
            {
                throw Error.ArgumentMustBeBoolean();
            }
            if (!TypeUtils.AreEquivalent(ifTrue.Type, ifFalse.Type))
            {
                throw Error.ArgumentTypesMustMatch();
            }

            return ConditionalExpression.Make(test, ifTrue, ifFalse, ifTrue.Type);
        }


        /// <summary>
        /// Creates a <see cref="ConditionalExpression"/>.
        /// </summary>
        /// <param name="test">An <see cref="Expression"/> to set the <see cref="P:ConditionalExpression.Test"/> property equal to.</param>
        /// <param name="ifTrue">An <see cref="Expression"/> to set the <see cref="P:ConditionalExpression.IfTrue"/> property equal to.</param>
        /// <param name="ifFalse">An <see cref="Expression"/> to set the <see cref="P:ConditionalExpression.IfFalse"/> property equal to.</param>
        /// <param name="type">A <see cref="Type"/> to set the <see cref="P:Expression.Type"/> property equal to.</param>
        /// <returns>A <see cref="ConditionalExpression"/> that has the <see cref="P:Expression.NodeType"/> property equal to 
        /// <see cref="F:ExpressionType.Conditional"/> and the <see cref="P:ConditionalExpression.Test"/>, <see cref="P:ConditionalExpression.IfTrue"/>, 
        /// and <see cref="P:ConditionalExpression.IfFalse"/> properties set to the specified values.</returns>
        /// <remarks>This method allows explicitly unifying the result type of the conditional expression in cases where the types of <paramref name="ifTrue"/>
        /// and <paramref name="ifFalse"/> expressions are not equal. Types of both <paramref name="ifTrue"/> and <paramref name="ifFalse"/> must be implicitly
        /// reference assignable to the result type. The <paramref name="type"/> is allowed to be <see cref="System.Void"/>.</remarks>
        public static ConditionalExpression Condition(Expression test, Expression ifTrue, Expression ifFalse, Type type)
        {
            RequiresCanRead(test, "test");
            RequiresCanRead(ifTrue, "ifTrue");
            RequiresCanRead(ifFalse, "ifFalse");
            ContractUtils.RequiresNotNull(type, "type");

            if (test.Type != typeof(bool))
            {
                throw Error.ArgumentMustBeBoolean();
            }

            if (type != typeof(void))
            {
                if (!TypeUtils.AreReferenceAssignable(type, ifTrue.Type) ||
                    !TypeUtils.AreReferenceAssignable(type, ifFalse.Type))
                {
                    throw Error.ArgumentTypesMustMatch();
                }
            }

            return ConditionalExpression.Make(test, ifTrue, ifFalse, type);
        }

        /// <summary>
        /// Creates a <see cref="ConditionalExpression"/>.
        /// </summary>
        /// <param name="test">An <see cref="Expression"/> to set the <see cref="P:ConditionalExpression.Test"/> property equal to.</param>
        /// <param name="ifTrue">An <see cref="Expression"/> to set the <see cref="P:ConditionalExpression.IfTrue"/> property equal to.</param>
        /// <returns>A <see cref="ConditionalExpression"/> that has the <see cref="P:Expression.NodeType"/> property equal to 
        /// <see cref="F:ExpressionType.Conditional"/> and the <see cref="P:ConditionalExpression.Test"/>, <see cref="P:ConditionalExpression.IfTrue"/>, 
        /// properties set to the specified values. The <see cref="P:ConditionalExpression.IfFalse"/> property is set to default expression and
        /// the type of the resulting <see cref="ConditionalExpression"/> returned by this method is <see cref="System.Void"/>.</returns>
        public static ConditionalExpression IfThen(Expression test, Expression ifTrue)
        {
            return Condition(test, ifTrue, Expression.Empty(), typeof(void));
        }

        /// <summary>
        /// Creates a <see cref="ConditionalExpression"/>.
        /// </summary>
        /// <param name="test">An <see cref="Expression"/> to set the <see cref="P:ConditionalExpression.Test"/> property equal to.</param>
        /// <param name="ifTrue">An <see cref="Expression"/> to set the <see cref="P:ConditionalExpression.IfTrue"/> property equal to.</param>
        /// <param name="ifFalse">An <see cref="Expression"/> to set the <see cref="P:ConditionalExpression.IfFalse"/> property equal to.</param>
        /// <returns>A <see cref="ConditionalExpression"/> that has the <see cref="P:Expression.NodeType"/> property equal to 
        /// <see cref="F:ExpressionType.Conditional"/> and the <see cref="P:ConditionalExpression.Test"/>, <see cref="P:ConditionalExpression.IfTrue"/>, 
        /// and <see cref="P:ConditionalExpression.IfFalse"/> properties set to the specified values. The type of the resulting <see cref="ConditionalExpression"/>
        /// returned by this method is <see cref="System.Void"/>.</returns>
        public static ConditionalExpression IfThenElse(Expression test, Expression ifTrue, Expression ifFalse)
        {
            return Condition(test, ifTrue, ifFalse, typeof(void));
        }
    }
}
