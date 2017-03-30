// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents a constructor call that has a collection initializer.
    /// </summary>
    /// <remarks>
    /// Use the <see cref="Expression.ListInit"/> factory methods to create a ListInitExpression.
    /// The value of the <see cref="NodeType" /> property of a ListInitExpression is ListInit.
    /// </remarks>
    [DebuggerTypeProxy(typeof(ListInitExpressionProxy))]
    public sealed class ListInitExpression : Expression
    {
        internal ListInitExpression(NewExpression newExpression, ReadOnlyCollection<ElementInit> initializers)
        {
            NewExpression = newExpression;
            Initializers = initializers;
        }

        /// <summary>
        /// Returns the node type of this <see cref="Expression"/>. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> that represents this expression.</returns>
        public sealed override ExpressionType NodeType => ExpressionType.ListInit;

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression"/> represents. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="System.Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type => NewExpression.Type;

        /// <summary>
        /// Gets a value that indicates whether the expression tree node can be reduced.
        /// </summary>
        public override bool CanReduce => true;

        /// <summary>
        /// Gets the expression that contains a call to the constructor of a collection type.
        /// </summary>
        public NewExpression NewExpression { get; }

        /// <summary>
        /// Gets the element initializers that are used to initialize a collection.
        /// </summary>
        public ReadOnlyCollection<ElementInit> Initializers { get; }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitListInit(this);
        }

        /// <summary>
        /// Reduces the binary expression node to a simpler expression.
        /// If CanReduce returns true, this should return a valid expression.
        /// This method is allowed to return another node which itself
        /// must be reduced.
        /// </summary>
        /// <returns>The reduced expression.</returns>
        public override Expression Reduce()
        {
            return MemberInitExpression.ReduceListInit(NewExpression, Initializers, keepOnStack: true);
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="newExpression">The <see cref="NewExpression"/> property of the result.</param>
        /// <param name="initializers">The <see cref="Initializers"/> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public ListInitExpression Update(NewExpression newExpression, IEnumerable<ElementInit> initializers)
        {
            if (newExpression == NewExpression & initializers != null)
            {
                if (ExpressionUtils.SameElements(ref initializers, Initializers))
                {
                    return this;
                }
            }

            return ListInit(newExpression, initializers);
        }
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="ListInitExpression"/> that uses a method named "Add" to add elements to a collection.
        /// </summary>
        /// <param name="newExpression">A <see cref="NewExpression"/> to set the <see cref="ListInitExpression.NewExpression"/> property equal to.</param>
        /// <param name="initializers">An array of <see cref="Expression"/> objects to use to populate the <see cref="ListInitExpression.Initializers"/> collection.</param>
        /// <returns>A <see cref="ListInitExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.ListInit"/> and the <see cref="ListInitExpression.NewExpression"/> property set to the specified value.</returns>
        public static ListInitExpression ListInit(NewExpression newExpression, params Expression[] initializers)
        {
            return ListInit(newExpression, initializers as IEnumerable<Expression>);
        }

        /// <summary>
        /// Creates a <see cref="ListInitExpression"/> that uses a method named "Add" to add elements to a collection.
        /// </summary>
        /// <param name="newExpression">A <see cref="NewExpression"/> to set the <see cref="ListInitExpression.NewExpression"/> property equal to.</param>
        /// <param name="initializers">An <see cref="IEnumerable{T}"/> that contains <see cref="Expressions.ElementInit"/> objects to use to populate the <see cref="ListInitExpression.Initializers"/> collection.</param>
        /// <returns>A <see cref="ListInitExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.ListInit"/> and the <see cref="ListInitExpression.NewExpression"/> property set to the specified value.</returns>
        public static ListInitExpression ListInit(NewExpression newExpression, IEnumerable<Expression> initializers)
        {
            ContractUtils.RequiresNotNull(newExpression, nameof(newExpression));
            ContractUtils.RequiresNotNull(initializers, nameof(initializers));

            ReadOnlyCollection<Expression> initializerlist = initializers.ToReadOnly();
            if (initializerlist.Count == 0)
            {
                return new ListInitExpression(newExpression, EmptyReadOnlyCollection<ElementInit>.Instance);
            }

            MethodInfo addMethod = FindMethod(newExpression.Type, "Add", null, new Expression[] { initializerlist[0] }, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return ListInit(newExpression, addMethod, initializerlist);
        }

        /// <summary>
        /// Creates a <see cref="ListInitExpression"/> that uses a specified method to add elements to a collection.
        /// </summary>
        /// <param name="newExpression">A <see cref="NewExpression"/> to set the <see cref="ListInitExpression.NewExpression"/> property equal to.</param>
        /// <param name="addMethod">A <see cref="MethodInfo"/> that represents an instance method named "Add" (case insensitive), that adds an element to a collection.</param>
        /// <param name="initializers">An array of <see cref="Expression"/> objects to use to populate the <see cref="ListInitExpression.Initializers"/> collection.</param>
        /// <returns>A <see cref="ListInitExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.ListInit"/> and the <see cref="ListInitExpression.NewExpression"/> property set to the specified value.</returns>
        public static ListInitExpression ListInit(NewExpression newExpression, MethodInfo addMethod, params Expression[] initializers)
        {
            return ListInit(newExpression, addMethod, initializers as IEnumerable<Expression>);
        }

        /// <summary>
        /// Creates a <see cref="ListInitExpression"/> that uses a specified method to add elements to a collection.
        /// </summary>
        /// <param name="newExpression">A <see cref="NewExpression"/> to set the <see cref="ListInitExpression.NewExpression"/> property equal to.</param>
        /// <param name="addMethod">A <see cref="MethodInfo"/> that represents an instance method named "Add" (case insensitive), that adds an element to a collection.</param>
        /// <param name="initializers">An <see cref="IEnumerable{T}"/> that contains <see cref="Expression"/> objects to use to populate the Initializers collection.</param>
        /// <returns>A <see cref="ListInitExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.ListInit"/> and the <see cref="ListInitExpression.NewExpression"/> property set to the specified value.</returns>
        public static ListInitExpression ListInit(NewExpression newExpression, MethodInfo addMethod, IEnumerable<Expression> initializers)
        {
            if (addMethod == null)
            {
                return ListInit(newExpression, initializers);
            }
            ContractUtils.RequiresNotNull(newExpression, nameof(newExpression));
            ContractUtils.RequiresNotNull(initializers, nameof(initializers));

            ReadOnlyCollection<Expression> initializerlist = initializers.ToReadOnly();
            ElementInit[] initList = new ElementInit[initializerlist.Count];
            for (int i = 0; i < initializerlist.Count; i++)
            {
                initList[i] = ElementInit(addMethod, initializerlist[i]);
            }
            return ListInit(newExpression, new TrueReadOnlyCollection<ElementInit>(initList));
        }

        /// <summary>
        /// Creates a <see cref="ListInitExpression"/> that uses specified <see cref="Expressions.ElementInit"/> objects to initialize a collection.
        /// </summary>
        /// <param name="newExpression">A <see cref="NewExpression"/> to set the <see cref="ListInitExpression.NewExpression"/> property equal to.</param>
        /// <param name="initializers">An array that contains <see cref="Expressions.ElementInit"/> objects to use to populate the <see cref="ListInitExpression.Initializers"/> collection.</param>
        /// <returns>
        /// A <see cref="ListInitExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.ListInit"/>
        /// and the <see cref="ListInitExpression.NewExpression"/> and <see cref="ListInitExpression.Initializers"/> properties set to the specified values.
        /// </returns>
        /// <remarks>
        /// The <see cref="Type"/> property of <paramref name="newExpression"/> must represent a type that implements <see cref="Collections.IEnumerable"/>.
        /// The <see cref="Type"/> property of the resulting <see cref="ListInitExpression"/> is equal to <paramref name="newExpression"/>.Type.
        /// </remarks>
        public static ListInitExpression ListInit(NewExpression newExpression, params ElementInit[] initializers)
        {
            return ListInit(newExpression, (IEnumerable<ElementInit>)initializers);
        }

        /// <summary>
        /// Creates a <see cref="ListInitExpression"/> that uses specified <see cref="Expressions.ElementInit"/> objects to initialize a collection.
        /// </summary>
        /// <param name="newExpression">A <see cref="NewExpression"/> to set the <see cref="ListInitExpression.NewExpression"/> property equal to.</param>
        /// <param name="initializers">An <see cref="IEnumerable{T}"/> that contains <see cref="Expressions.ElementInit"/> objects to use to populate the <see cref="ListInitExpression.Initializers"/> collection.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> that contains <see cref="Expressions.ElementInit"/> objects to use to populate the <see cref="ListInitExpression.Initializers"/> collection.</returns>
        /// <remarks>
        /// The <see cref="Type"/> property of <paramref name="newExpression"/> must represent a type that implements <see cref="Collections.IEnumerable"/>.
        /// The <see cref="Type"/> property of the resulting <see cref="ListInitExpression"/> is equal to <paramref name="newExpression"/>.Type.
        /// </remarks>
        public static ListInitExpression ListInit(NewExpression newExpression, IEnumerable<ElementInit> initializers)
        {
            ContractUtils.RequiresNotNull(newExpression, nameof(newExpression));
            ContractUtils.RequiresNotNull(initializers, nameof(initializers));
            ReadOnlyCollection<ElementInit> initializerlist = initializers.ToReadOnly();
            ValidateListInitArgs(newExpression.Type, initializerlist, nameof(newExpression));
            return new ListInitExpression(newExpression, initializerlist);
        }
    }
}
