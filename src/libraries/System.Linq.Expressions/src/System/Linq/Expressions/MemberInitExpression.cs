// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>Represents calling a constructor and initializing one or more members of the new object.</summary>
    /// <remarks>Use the <see cref="O:System.Linq.Expressions.Expression.MemberInit" /> factory methods to create a <see cref="System.Linq.Expressions.MemberInitExpression" />.
    /// The value of the <see cref="O:System.Linq.Expressions.Expression.NodeType" /> property of a <see cref="System.Linq.Expressions.MemberInitExpression" /> is <see cref="System.Linq.Expressions.ExpressionType.MemberInit" />.</remarks>
    /// <example>The following example creates a <see cref="System.Linq.Expressions.MemberInitExpression" /> that represents the initialization of two members of a new object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet9":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet9":::</example>
    [DebuggerTypeProxy(typeof(MemberInitExpressionProxy))]
    public sealed class MemberInitExpression : Expression
    {
        internal MemberInitExpression(NewExpression newExpression, ReadOnlyCollection<MemberBinding> bindings)
        {
            NewExpression = newExpression;
            Bindings = bindings;
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.MemberInitExpression.Type" /> that represents the static type of the expression.</value>
        public sealed override Type Type => NewExpression.Type;

        /// <summary>Gets a value that indicates whether the expression tree node can be reduced.</summary>
        /// <value><see langword="true" /> if the node can be reduced; otherwise, <see langword="false" />.</value>
        public override bool CanReduce => true;

        /// <summary>Returns the node type of this Expression. Extension nodes should return <see cref="System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> of the expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.MemberInit;

        /// <summary>Gets the expression that represents the constructor call.</summary>
        /// <value>A <see cref="System.Linq.Expressions.NewExpression" /> that represents the constructor call.</value>
        public NewExpression NewExpression { get; }

        /// <summary>Gets the bindings that describe how to initialize the members of the newly created object.</summary>
        /// <value>A <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" /> of <see cref="System.Linq.Expressions.MemberBinding" /> objects which describe how to initialize the members.</value>
        public ReadOnlyCollection<MemberBinding> Bindings { get; }

        /// <summary>Dispatches to the specific visit method for this node type.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitMemberInit(this);
        }

        /// <summary>Reduces the <see cref="System.Linq.Expressions.MemberInitExpression" /> to a simpler expression.</summary>
        /// <returns>The reduced expression.</returns>
        /// <remarks>If the `CanReduce` method returns true, this method should return a valid expression.
        /// This method is allowed to return another node which itself must be reduced.</remarks>
        public override Expression Reduce()
        {
            return ReduceMemberInit(NewExpression, Bindings, keepOnStack: true);
        }

        private static Expression ReduceMemberInit(
            Expression objExpression, ReadOnlyCollection<MemberBinding> bindings, bool keepOnStack)
        {
            ParameterExpression objVar = Variable(objExpression.Type);
            int count = bindings.Count;
            Expression[] block = new Expression[count + 2];
            block[0] = Assign(objVar, objExpression);
            for (int i = 0; i < count; i++)
            {
                block[i + 1] = ReduceMemberBinding(objVar, bindings[i]);
            }

            block[count + 1] = keepOnStack ? (Expression)objVar : Utils.Empty;
            return Block(new[] { objVar }, block);
        }

        internal static Expression ReduceListInit(
            Expression listExpression, ReadOnlyCollection<ElementInit> initializers, bool keepOnStack)
        {
            ParameterExpression listVar = Variable(listExpression.Type);
            int count = initializers.Count;
            Expression[] block = new Expression[count + 2];
            block[0] = Assign(listVar, listExpression);
            for (int i = 0; i < count; i++)
            {
                ElementInit element = initializers[i];
                block[i + 1] = Call(listVar, element.AddMethod, element.Arguments);
            }

            block[count + 1] = keepOnStack ? (Expression)listVar : Utils.Empty;
            return Block(new[] { listVar }, block);
        }

        internal static Expression ReduceMemberBinding(ParameterExpression objVar, MemberBinding binding)
        {
            MemberExpression member = Expression.MakeMemberAccess(objVar, binding.Member);
            return binding.BindingType switch
            {
                MemberBindingType.Assignment => Expression.Assign(member, ((MemberAssignment)binding).Expression),
                MemberBindingType.ListBinding => ReduceListInit(member, ((MemberListBinding)binding).Initializers, keepOnStack: false),
                MemberBindingType.MemberBinding => ReduceMemberInit(member, ((MemberMemberBinding)binding).Bindings, keepOnStack: false),
                _ => throw ContractUtils.Unreachable,
            };
        }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="newExpression">The <see cref="System.Linq.Expressions.MemberInitExpression.NewExpression" /> property of the result.</param>
        /// <param name="bindings">The <see cref="System.Linq.Expressions.MemberInitExpression.Bindings" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public MemberInitExpression Update(NewExpression newExpression, IEnumerable<MemberBinding> bindings)
        {
            if (newExpression == NewExpression && bindings != null)
            {
                if (ExpressionUtils.SameElements(ref bindings!, Bindings))
                {
                    return this;
                }
            }

            return Expression.MemberInit(newExpression, bindings!);
        }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.MemberInitExpression" />.</summary>
        /// <param name="newExpression">A <see cref="System.Linq.Expressions.NewExpression" /> to set the <see cref="System.Linq.Expressions.MemberInitExpression.NewExpression" /> property equal to.</param>
        /// <param name="bindings">An array of <see cref="System.Linq.Expressions.MemberBinding" /> objects to use to populate the <see cref="System.Linq.Expressions.MemberInitExpression.Bindings" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberInitExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.MemberInit" /> and the <see cref="System.Linq.Expressions.MemberInitExpression.NewExpression" /> and <see cref="System.Linq.Expressions.MemberInitExpression.Bindings" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="newExpression" /> or <paramref name="bindings" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">The <see cref="System.Linq.Expressions.MemberBinding.Member" /> property of an element of <paramref name="bindings" /> does not represent a member of the type that <paramref name="newExpression" />.Type represents.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.MemberInitExpression" /> is equal to the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="newExpression" />.</remarks>
        /// <example>The following example demonstrates how to use the <see cref="System.Linq.Expressions.Expression.MemberInit(System.Linq.Expressions.NewExpression,System.Linq.Expressions.MemberBinding[])" /> method to create a <see cref="System.Linq.Expressions.MemberInitExpression" /> that represents the initialization of two members of a new object.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet9":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet9":::</example>
        public static MemberInitExpression MemberInit(NewExpression newExpression, params MemberBinding[] bindings)
        {
            return MemberInit(newExpression, (IEnumerable<MemberBinding>)bindings);
        }

        /// <summary>Represents an expression that creates a new object and initializes a property of the object.</summary>
        /// <param name="newExpression">A <see cref="System.Linq.Expressions.NewExpression" /> to set the <see cref="System.Linq.Expressions.MemberInitExpression.NewExpression" /> property equal to.</param>
        /// <param name="bindings">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.MemberBinding" /> objects to use to populate the <see cref="System.Linq.Expressions.MemberInitExpression.Bindings" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.MemberInitExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.MemberInit" /> and the <see cref="System.Linq.Expressions.MemberInitExpression.NewExpression" /> and <see cref="System.Linq.Expressions.MemberInitExpression.Bindings" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="newExpression" /> or <paramref name="bindings" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">The <see cref="System.Linq.Expressions.MemberBinding.Member" /> property of an element of <paramref name="bindings" /> does not represent a member of the type that <paramref name="newExpression" />.Type represents.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.MemberInitExpression" /> is equal to the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="newExpression" />.</remarks>
        /// <example>The following example demonstrates an expression that creates a new object and initializes a property of the object.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet40":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet40":::</example>
        public static MemberInitExpression MemberInit(NewExpression newExpression, IEnumerable<MemberBinding> bindings)
        {
            ContractUtils.RequiresNotNull(newExpression, nameof(newExpression));
            ContractUtils.RequiresNotNull(bindings, nameof(bindings));
            ReadOnlyCollection<MemberBinding> roBindings = bindings.ToReadOnly();
            ValidateMemberInitArgs(newExpression.Type, roBindings);
            return new MemberInitExpression(newExpression, roBindings);
        }
    }
}
