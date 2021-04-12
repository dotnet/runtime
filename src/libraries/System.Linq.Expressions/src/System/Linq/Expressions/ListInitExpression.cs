// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    /// <summary>Represents a constructor call that has a collection initializer.</summary>
    /// <remarks>Use the <see cref="O:System.Linq.Expressions.Expression.ListInit" /> factory methods to create a <see cref="System.Linq.Expressions.ListInitExpression" />.
    /// The value of the <see cref="O:System.Linq.Expressions.Expression.NodeType" /> property of a <see cref="System.Linq.Expressions.ListInitExpression" /> is <see cref="System.Linq.Expressions.ExpressionType.ListInit" />.</remarks>
    /// <example>The following example creates a <see cref="System.Linq.Expressions.ListInitExpression" /> that represents the initialization of a new dictionary instance that has two key-value pairs.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet7":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet7":::</example>
    [DebuggerTypeProxy(typeof(ListInitExpressionProxy))]
    public sealed class ListInitExpression : Expression
    {
        internal ListInitExpression(NewExpression newExpression, ReadOnlyCollection<ElementInit> initializers)
        {
            NewExpression = newExpression;
            Initializers = initializers;
        }

        /// <summary>Returns the node type of this <see cref="System.Linq.Expressions.Expression" />.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> that represents this expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.ListInit;

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ListInitExpression.Type" /> that represents the static type of the expression.</value>
        public sealed override Type Type => NewExpression.Type;

        /// <summary>Gets a value that indicates whether the expression tree node can be reduced.</summary>
        /// <value><see langword="true" /> if the node can be reduced; otherwise, <see langword="false" />.</value>
        public override bool CanReduce => true;

        /// <summary>Gets the expression that contains a call to the constructor of a collection type.</summary>
        /// <value>A <see cref="System.Linq.Expressions.NewExpression" /> that represents the call to the constructor of a collection type.</value>
        public NewExpression NewExpression { get; }

        /// <summary>Gets the element initializers that are used to initialize a collection.</summary>
        /// <value>A <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" /> of <see cref="System.Linq.Expressions.ElementInit" /> objects which represent the elements that are used to initialize the collection.</value>
        public ReadOnlyCollection<ElementInit> Initializers { get; }

        /// <summary>Dispatches to the specific visit method for this node type.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitListInit(this);
        }

        /// <summary>Reduces the binary expression node to a simpler expression.</summary>
        /// <returns>The reduced expression.</returns>
        /// <remarks>If the `CanReduce` method returns true, this method should return a valid expression.
        /// This method is allowed to return another node which itself must be reduced.</remarks>
        public override Expression Reduce()
        {
            return MemberInitExpression.ReduceListInit(NewExpression, Initializers, keepOnStack: true);
        }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="newExpression">The <see cref="System.Linq.Expressions.ListInitExpression.NewExpression" /> property of the result.</param>
        /// <param name="initializers">The <see cref="System.Linq.Expressions.ListInitExpression.Initializers" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public ListInitExpression Update(NewExpression newExpression, IEnumerable<ElementInit> initializers)
        {
            if (newExpression == NewExpression && initializers != null)
            {
                if (ExpressionUtils.SameElements(ref initializers!, Initializers))
                {
                    return this;
                }
            }

            return ListInit(newExpression, initializers!);
        }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.ListInitExpression" /> that uses a method named "Add" to add elements to a collection.</summary>
        /// <param name="newExpression">A <see cref="System.Linq.Expressions.NewExpression" /> to set the <see cref="System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.</param>
        /// <param name="initializers">An array of <see cref="System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="System.Linq.Expressions.ListInitExpression.Initializers" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.ListInitExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ListInit" /> and the <see cref="System.Linq.Expressions.ListInitExpression.NewExpression" /> property set to the specified value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="newExpression" /> or <paramref name="initializers" /> is <see langword="null" />.
        /// -or-
        /// One or more elements of <paramref name="initializers" /> are <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="newExpression" />.Type does not implement <see cref="System.Collections.IEnumerable" />.</exception>
        /// <exception cref="System.InvalidOperationException">There is no instance method named "Add" (case insensitive) declared in <paramref name="newExpression" />.Type or its base type.
        /// -or-
        /// The add method on <paramref name="newExpression" />.Type or its base type does not take exactly one argument.
        /// -or-
        /// The type represented by the <see cref="System.Linq.Expressions.Expression.Type" /> property of the first element of <paramref name="initializers" /> is not assignable to the argument type of the add method on <paramref name="newExpression" />.Type or its base type.
        /// -or-
        /// More than one argument-compatible method named "Add" (case-insensitive) exists on <paramref name="newExpression" />.Type and/or its base type.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="newExpression" /> must represent a type that implements <see cref="System.Collections.IEnumerable" />.
        /// In order to use this overload of <see cref="System.Linq.Expressions.Expression.ListInit(System.Linq.Expressions.NewExpression,System.Linq.Expressions.Expression[])" />, <paramref name="newExpression" />.Type or its base type must declare a single method named "Add" (case insensitive) that takes exactly one argument. The type of the argument must be assignable from the type represented by the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the first element of <paramref name="initializers" />.
        /// The <see cref="O:System.Linq.Expressions.ListInitExpression.Initializers" /> property of the returned <see cref="System.Linq.Expressions.ListInitExpression" /> contains one element of type <see cref="System.Linq.Expressions.ElementInit" /> for each element of <paramref name="initializers" />. The <see cref="O:System.Linq.Expressions.ElementInit.Arguments" /> property of each element of <see cref="O:System.Linq.Expressions.ListInitExpression.Initializers" /> is a singleton collection that contains the corresponding element of <paramref name="initializers" />. The <see cref="O:System.Linq.Expressions.ElementInit.AddMethod" /> property of each element of <see cref="O:System.Linq.Expressions.ListInitExpression.Initializers" /> represents the add method that was discovered on <paramref name="newExpression" />.Type or its base type.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.ListInitExpression" /> is equal to <paramref name="newExpression" />.Type.</remarks>
        [RequiresUnreferencedCode(ExpressionRequiresUnreferencedCode)]
        public static ListInitExpression ListInit(NewExpression newExpression, params Expression[] initializers)
        {
            return ListInit(newExpression, initializers as IEnumerable<Expression>);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.ListInitExpression" /> that uses a method named "Add" to add elements to a collection.</summary>
        /// <param name="newExpression">A <see cref="System.Linq.Expressions.NewExpression" /> to set the <see cref="System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.</param>
        /// <param name="initializers">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="System.Linq.Expressions.ListInitExpression.Initializers" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.ListInitExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ListInit" /> and the <see cref="System.Linq.Expressions.ListInitExpression.NewExpression" /> property set to the specified value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="newExpression" /> or <paramref name="initializers" /> is <see langword="null" />.
        /// -or-
        /// One or more elements of <paramref name="initializers" /> are <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="newExpression" />.Type does not implement <see cref="System.Collections.IEnumerable" />.</exception>
        /// <exception cref="System.InvalidOperationException">There is no instance method named "Add" (case insensitive) declared in <paramref name="newExpression" />.Type or its base type.
        /// -or-
        /// The add method on <paramref name="newExpression" />.Type or its base type does not take exactly one argument.
        /// -or-
        /// The type represented by the <see cref="System.Linq.Expressions.Expression.Type" /> property of the first element of <paramref name="initializers" /> is not assignable to the argument type of the add method on <paramref name="newExpression" />.Type or its base type.
        /// -or-
        /// More than one argument-compatible method named "Add" (case-insensitive) exists on <paramref name="newExpression" />.Type and/or its base type.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="newExpression" /> must represent a type that implements <see cref="System.Collections.IEnumerable" />.
        /// In order to use this overload of <see cref="System.Linq.Expressions.Expression.ListInit(System.Linq.Expressions.NewExpression,System.Collections.Generic.IEnumerable{System.Linq.Expressions.Expression})" />, <paramref name="newExpression" />.Type or its base type must declare a single method named "Add" (case insensitive) that takes exactly one argument. The type of the argument must be assignable from the type represented by the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the first element of <paramref name="initializers" />.
        /// The <see cref="O:System.Linq.Expressions.ListInitExpression.Initializers" /> property of the returned <see cref="System.Linq.Expressions.ListInitExpression" /> contains one element of type <see cref="System.Linq.Expressions.ElementInit" /> for each element of <paramref name="initializers" />. The <see cref="O:System.Linq.Expressions.ElementInit.Arguments" /> property of each element of <see cref="O:System.Linq.Expressions.ListInitExpression.Initializers" /> is a singleton collection that contains the corresponding element of <paramref name="initializers" />. The <see cref="O:System.Linq.Expressions.ElementInit.AddMethod" /> property of each element of <see cref="O:System.Linq.Expressions.ListInitExpression.Initializers" /> represents the add method that was discovered on <paramref name="newExpression" />.Type or its base type.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.ListInitExpression" /> is equal to <paramref name="newExpression" />.Type.</remarks>
        [RequiresUnreferencedCode(ExpressionRequiresUnreferencedCode)]
        public static ListInitExpression ListInit(NewExpression newExpression, IEnumerable<Expression> initializers)
        {
            ContractUtils.RequiresNotNull(newExpression, nameof(newExpression));
            ContractUtils.RequiresNotNull(initializers, nameof(initializers));

            ReadOnlyCollection<Expression> initializerlist = initializers.ToReadOnly();
            if (initializerlist.Count == 0)
            {
                return new ListInitExpression(newExpression, EmptyReadOnlyCollection<ElementInit>.Instance);
            }

            MethodInfo? addMethod = FindMethod(newExpression.Type, "Add", null, new Expression[] { initializerlist[0] }, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return ListInit(newExpression, addMethod, initializerlist);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.ListInitExpression" /> that uses a specified method to add elements to a collection.</summary>
        /// <param name="newExpression">A <see cref="System.Linq.Expressions.NewExpression" /> to set the <see cref="System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.</param>
        /// <param name="addMethod">A <see cref="System.Reflection.MethodInfo" /> that represents an instance method that takes one argument, that adds an element to a collection.</param>
        /// <param name="initializers">An array of <see cref="System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="System.Linq.Expressions.ListInitExpression.Initializers" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.ListInitExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ListInit" /> and the <see cref="System.Linq.Expressions.ListInitExpression.NewExpression" /> property set to the specified value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="newExpression" /> or <paramref name="initializers" /> is <see langword="null" />.
        /// -or-
        /// One or more elements of <paramref name="initializers" /> are <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="newExpression" />.Type does not implement <see cref="System.Collections.IEnumerable" />.
        /// -or-
        /// <paramref name="addMethod" /> is not <see langword="null" /> and it does not represent an instance method named "Add" (case insensitive) that takes exactly one argument.
        /// -or-
        /// <paramref name="addMethod" /> is not <see langword="null" /> and the type represented by the <see cref="System.Linq.Expressions.Expression.Type" /> property of one or more elements of <paramref name="initializers" /> is not assignable to the argument type of the method that <paramref name="addMethod" /> represents.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="addMethod" /> is <see langword="null" /> and no instance method named "Add" that takes one type-compatible argument exists on <paramref name="newExpression" />.Type or its base type.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="newExpression" /> must represent a type that implements <see cref="System.Collections.IEnumerable" />.
        /// If <paramref name="addMethod" /> is <see langword="null" />, <paramref name="newExpression" />.Type or its base type must declare a single method named "Add" (case insensitive) that takes exactly one argument. If <paramref name="addMethod" /> is not <see langword="null" />, it must represent an instance method named "Add" (case insensitive) that has exactly one parameter. The type represented by the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of each element of <paramref name="initializers" /> must be assignable to the argument type of the add method.
        /// The <see cref="O:System.Linq.Expressions.ListInitExpression.Initializers" /> property of the returned <see cref="System.Linq.Expressions.ListInitExpression" /> contains one element of type <see cref="System.Linq.Expressions.ElementInit" /> for each element of <paramref name="initializers" />. The <see cref="O:System.Linq.Expressions.ElementInit.Arguments" /> property of each element of <see cref="O:System.Linq.Expressions.ListInitExpression.Initializers" /> is a singleton collection that contains the corresponding element of <paramref name="initializers" />. The <see cref="O:System.Linq.Expressions.ElementInit.AddMethod" /> property of each element of <see cref="O:System.Linq.Expressions.ListInitExpression.Initializers" /> is equal to <paramref name="addMethod" />.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.ListInitExpression" /> is equal to <paramref name="newExpression" />.Type.</remarks>
        [RequiresUnreferencedCode(ExpressionRequiresUnreferencedCode)]
        public static ListInitExpression ListInit(NewExpression newExpression, MethodInfo? addMethod, params Expression[] initializers)
        {
            return ListInit(newExpression, addMethod, initializers as IEnumerable<Expression>);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.ListInitExpression" /> that uses a specified method to add elements to a collection.</summary>
        /// <param name="newExpression">A <see cref="System.Linq.Expressions.NewExpression" /> to set the <see cref="System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.</param>
        /// <param name="addMethod">A <see cref="System.Reflection.MethodInfo" /> that represents an instance method named "Add" (case insensitive), that adds an element to a collection.</param>
        /// <param name="initializers">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="System.Linq.Expressions.ListInitExpression.Initializers" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.ListInitExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ListInit" /> and the <see cref="System.Linq.Expressions.ListInitExpression.NewExpression" /> property set to the specified value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="newExpression" /> or <paramref name="initializers" /> is <see langword="null" />.
        /// -or-
        /// One or more elements of <paramref name="initializers" /> are <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="newExpression" />.Type does not implement <see cref="System.Collections.IEnumerable" />.
        /// -or-
        /// <paramref name="addMethod" /> is not <see langword="null" /> and it does not represent an instance method named "Add" (case insensitive) that takes exactly one argument.
        /// -or-
        /// <paramref name="addMethod" /> is not <see langword="null" /> and the type represented by the <see cref="System.Linq.Expressions.Expression.Type" /> property of one or more elements of <paramref name="initializers" /> is not assignable to the argument type of the method that <paramref name="addMethod" /> represents.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="addMethod" /> is <see langword="null" /> and no instance method named "Add" that takes one type-compatible argument exists on <paramref name="newExpression" />.Type or its base type.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="newExpression" /> must represent a type that implements <see cref="System.Collections.IEnumerable" />.
        /// If <paramref name="addMethod" /> is <see langword="null" />, <paramref name="newExpression" />.Type or its base type must declare a single method named "Add" (case insensitive) that takes exactly one argument. If <paramref name="addMethod" /> is not <see langword="null" />, it must represent an instance method named "Add" (case insensitive) that has exactly one parameter. The type represented by the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of each element of <paramref name="initializers" /> must be assignable to the argument type of the add method.
        /// The <see cref="O:System.Linq.Expressions.ListInitExpression.Initializers" /> property of the returned <see cref="System.Linq.Expressions.ListInitExpression" /> contains one element of type <see cref="System.Linq.Expressions.ElementInit" /> for each element of <paramref name="initializers" />. The <see cref="O:System.Linq.Expressions.ElementInit.Arguments" /> property of each element of <see cref="O:System.Linq.Expressions.ListInitExpression.Initializers" /> is a singleton collection that contains the corresponding element of <paramref name="initializers" />. The <see cref="O:System.Linq.Expressions.ElementInit.AddMethod" /> property of each element of <see cref="O:System.Linq.Expressions.ListInitExpression.Initializers" /> is equal to <paramref name="addMethod" />.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.ListInitExpression" /> is equal to <paramref name="newExpression" />.Type.</remarks>
        [RequiresUnreferencedCode(ExpressionRequiresUnreferencedCode)]
        public static ListInitExpression ListInit(NewExpression newExpression, MethodInfo? addMethod, IEnumerable<Expression> initializers)
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.ListInitExpression" /> that uses specified <see cref="System.Linq.Expressions.ElementInit" /> objects to initialize a collection.</summary>
        /// <param name="newExpression">A <see cref="System.Linq.Expressions.NewExpression" /> to set the <see cref="System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.</param>
        /// <param name="initializers">An array of <see cref="System.Linq.Expressions.ElementInit" /> objects to use to populate the <see cref="System.Linq.Expressions.ListInitExpression.Initializers" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.ListInitExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ListInit" /> and the <see cref="System.Linq.Expressions.ListInitExpression.NewExpression" /> and <see cref="System.Linq.Expressions.ListInitExpression.Initializers" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="newExpression" /> or <paramref name="initializers" /> is <see langword="null" />.
        /// -or-
        /// One or more elements of <paramref name="initializers" /> are <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="newExpression" />.Type does not implement <see cref="System.Collections.IEnumerable" />.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="newExpression" /> must represent a type that implements <see cref="System.Collections.IEnumerable" />.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.ListInitExpression" /> is equal to <paramref name="newExpression" />.Type.</remarks>
        /// <example>The following example demonstrates how to use the <see cref="System.Linq.Expressions.Expression.ListInit(System.Linq.Expressions.NewExpression,System.Linq.Expressions.ElementInit[])" /> method to create a <see cref="System.Linq.Expressions.ListInitExpression" /> that represents the initialization of a new dictionary instance with two key-value pairs.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet7":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet7":::</example>
        public static ListInitExpression ListInit(NewExpression newExpression, params ElementInit[] initializers)
        {
            return ListInit(newExpression, (IEnumerable<ElementInit>)initializers);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.ListInitExpression" /> that uses specified <see cref="System.Linq.Expressions.ElementInit" /> objects to initialize a collection.</summary>
        /// <param name="newExpression">A <see cref="System.Linq.Expressions.NewExpression" /> to set the <see cref="System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.</param>
        /// <param name="initializers">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.ElementInit" /> objects to use to populate the <see cref="System.Linq.Expressions.ListInitExpression.Initializers" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.ListInitExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ListInit" /> and the <see cref="System.Linq.Expressions.ListInitExpression.NewExpression" /> and <see cref="System.Linq.Expressions.ListInitExpression.Initializers" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="newExpression" /> or <paramref name="initializers" /> is <see langword="null" />.
        /// -or-
        /// One or more elements of <paramref name="initializers" /> are <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="newExpression" />.Type does not implement <see cref="System.Collections.IEnumerable" />.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="newExpression" /> must represent a type that implements <see cref="System.Collections.IEnumerable" />.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.ListInitExpression" /> is equal to <paramref name="newExpression" />.Type.</remarks>
        /// <example>The following example demonstrates how to use the <see cref="System.Linq.Expressions.Expression.ListInit(System.Linq.Expressions.NewExpression,System.Linq.Expressions.ElementInit[])" /> method to create a <see cref="System.Linq.Expressions.ListInitExpression" /> that represents the initialization of a new dictionary instance with two key-value pairs.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet7":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet7":::</example>
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
