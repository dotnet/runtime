// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    /// <summary>Represents creating a new array and possibly initializing the elements of the new array.</summary>
    /// <remarks>The following table shows the different factory methods that you can use to create a <see cref="System.Linq.Expressions.NewArrayExpression" /> depending on the <see cref="O:System.Linq.Expressions.Expression.NodeType" /> you require.
    /// |<see cref="O:System.Linq.Expressions.Expression.NodeType" />|Factory Methods|
    /// |----------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------|
    /// |<see cref="System.Linq.Expressions.ExpressionType.NewArrayBounds" />|<see cref="O:System.Linq.Expressions.Expression.NewArrayBounds" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.NewArrayInit" />|<see cref="O:System.Linq.Expressions.Expression.NewArrayInit" />|</remarks>
    /// <example>The following example creates a <see cref="System.Linq.Expressions.NewArrayExpression" /> object that represents creating and initializing a one-dimensional array of strings.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet1":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet1":::
    /// The next example creates a <see cref="System.Linq.Expressions.NewArrayExpression" /> object that represents creating a two-dimensional array of strings.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet2":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet2":::</example>
    [DebuggerTypeProxy(typeof(NewArrayExpressionProxy))]
    public class NewArrayExpression : Expression
    {
        internal NewArrayExpression(Type type, ReadOnlyCollection<Expression> expressions)
        {
            Expressions = expressions;
            Type = type;
        }

        internal static NewArrayExpression Make(ExpressionType nodeType, Type type, ReadOnlyCollection<Expression> expressions)
        {
            Debug.Assert(type.IsArray);
            if (nodeType == ExpressionType.NewArrayInit)
            {
                return new NewArrayInitExpression(type, expressions);
            }
            else
            {
                return new NewArrayBoundsExpression(type, expressions);
            }
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.NewArrayExpression.Type" /> that represents the static type of the expression.</value>
        public sealed override Type Type { get; }

        /// <summary>Gets the bounds of the array if the value of the <see cref="System.Linq.Expressions.Expression.NodeType" /> property is <see cref="System.Linq.Expressions.ExpressionType.NewArrayBounds" />, or the values to initialize the elements of the new array if the value of the <see cref="System.Linq.Expressions.Expression.NodeType" /> property is <see cref="System.Linq.Expressions.ExpressionType.NewArrayInit" />.</summary>
        /// <value>A <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" /> of <see cref="System.Linq.Expressions.Expression" /> objects which represent either the bounds of the array or the initialization values.</value>
        public ReadOnlyCollection<Expression> Expressions { get; }

        /// <summary>Dispatches to the specific visit method for this node type. For example, <see cref="System.Linq.Expressions.MethodCallExpression" /> calls the <see cref="System.Linq.Expressions.ExpressionVisitor.VisitMethodCall(System.Linq.Expressions.MethodCallExpression)" />.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        /// <remarks>This default implementation for <see cref="System.Linq.Expressions.ExpressionType.Extension" /> nodes calls <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />. Override this method to call into a more specific method on a derived visitor class of the <see cref="System.Linq.Expressions.ExpressionVisitor" /> class. However, it should still support unknown visitors by calling <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />.</remarks>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitNewArray(this);
        }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="expressions">The <see cref="System.Linq.Expressions.NewArrayExpression.Expressions" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public NewArrayExpression Update(IEnumerable<Expression> expressions)
        {
            // Explicit null check here as otherwise wrong parameter name will be used.
            ContractUtils.RequiresNotNull(expressions, nameof(expressions));

            if (ExpressionUtils.SameElements(ref expressions!, Expressions))
            {
                return this;
            }

            return NodeType == ExpressionType.NewArrayInit
                ? NewArrayInit(Type.GetElementType()!, expressions)
                : NewArrayBounds(Type.GetElementType()!, expressions);
        }
    }

    internal sealed class NewArrayInitExpression : NewArrayExpression
    {
        internal NewArrayInitExpression(Type type, ReadOnlyCollection<Expression> expressions)
            : base(type, expressions)
        {
        }


        /// <summary>
        /// Returns the node type of this <see cref="Expression"/>. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> that represents this expression.</returns>
        public sealed override ExpressionType NodeType => ExpressionType.NewArrayInit;
    }

    internal sealed class NewArrayBoundsExpression : NewArrayExpression
    {
        internal NewArrayBoundsExpression(Type type, ReadOnlyCollection<Expression> expressions)
            : base(type, expressions)
        {
        }

        /// <summary>
        /// Returns the node type of this <see cref="Expression"/>. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> that represents this expression.</returns>
        public sealed override ExpressionType NodeType => ExpressionType.NewArrayBounds;
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        #region NewArrayInit

        /// <summary>Creates a <see cref="System.Linq.Expressions.NewArrayExpression" /> that represents creating a one-dimensional array and initializing it from a list of elements.</summary>
        /// <param name="type">A <see cref="System.Type" /> that represents the element type of the array.</param>
        /// <param name="initializers">An array of <see cref="System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="System.Linq.Expressions.NewArrayExpression.Expressions" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.NewArrayExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.NewArrayInit" /> and the <see cref="System.Linq.Expressions.NewArrayExpression.Expressions" /> property set to the specified value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="type" /> or <paramref name="initializers" /> is <see langword="null" />.
        /// -or-
        /// An element of <paramref name="initializers" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The <see cref="System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="initializers" /> represents a type that is not assignable to the type <paramref name="type" />.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of each element of <paramref name="initializers" /> must represent a type that is assignable to the type represented by <paramref name="type" />, possibly after it is *quoted*.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  An element will be quoted only if `type` is <xref:System.Linq.Expressions.Expression>. Quoting means the element is wrapped in a <xref:System.Linq.Expressions.ExpressionType.Quote> node. The resulting node is a <xref:System.Linq.Expressions.UnaryExpression> whose <xref:System.Linq.Expressions.UnaryExpression.Operand%2A> property is the element of `initializers`.
        /// ]]></format>
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.NewArrayExpression" /> represents an array type whose rank is 1 and whose element type is <paramref name="type" />.</remarks>
        /// <example>The following example demonstrates how to use the <see cref="O:System.Linq.Expressions.Expression.NewArrayInit" /> method to create an expression tree that represents creating a one-dimensional string array that is initialized with a list of string expressions.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet1":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet1":::</example>
        public static NewArrayExpression NewArrayInit(Type type, params Expression[] initializers)
        {
            return NewArrayInit(type, (IEnumerable<Expression>)initializers);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.NewArrayExpression" /> that represents creating a one-dimensional array and initializing it from a list of elements.</summary>
        /// <param name="type">A <see cref="System.Type" /> that represents the element type of the array.</param>
        /// <param name="initializers">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="System.Linq.Expressions.NewArrayExpression.Expressions" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.NewArrayExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.NewArrayInit" /> and the <see cref="System.Linq.Expressions.NewArrayExpression.Expressions" /> property set to the specified value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="type" /> or <paramref name="initializers" /> is <see langword="null" />.
        /// -or-
        /// An element of <paramref name="initializers" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The <see cref="System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="initializers" /> represents a type that is not assignable to the type that <paramref name="type" /> represents.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of each element of <paramref name="initializers" /> must represent a type that is assignable to the type represented by <paramref name="type" />, possibly after it is *quoted*.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  An element will be quoted only if `type` is <xref:System.Linq.Expressions.Expression>. Quoting means the element is wrapped in a <xref:System.Linq.Expressions.ExpressionType.Quote> node. The resulting node is a <xref:System.Linq.Expressions.UnaryExpression> whose <xref:System.Linq.Expressions.UnaryExpression.Operand%2A> property is the element of `initializers`.
        /// ]]></format>
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.NewArrayExpression" /> represents an array type whose rank is 1 and whose element type is <paramref name="type" />.</remarks>
        /// <example>The following example demonstrates how to use the <see cref="O:System.Linq.Expressions.Expression.NewArrayInit" /> method to create an expression tree that represents creating a one-dimensional string array that is initialized with a list of string expressions.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet1":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet1":::</example>
        public static NewArrayExpression NewArrayInit(Type type, IEnumerable<Expression> initializers)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            ContractUtils.RequiresNotNull(initializers, nameof(initializers));
            if (type == typeof(void))
            {
                throw Error.ArgumentCannotBeOfTypeVoid(nameof(type));
            }

            TypeUtils.ValidateType(type, nameof(type));
            ReadOnlyCollection<Expression> initializerList = initializers.ToReadOnly();

            Expression[]? newList = null;
            for (int i = 0, n = initializerList.Count; i < n; i++)
            {
                Expression expr = initializerList[i];
                ExpressionUtils.RequiresCanRead(expr, nameof(initializers), i);

                if (!TypeUtils.AreReferenceAssignable(type, expr.Type))
                {
                    if (!TryQuote(type, ref expr))
                    {
                        throw Error.ExpressionTypeCannotInitializeArrayType(expr.Type, type);
                    }
                    if (newList == null)
                    {
                        newList = new Expression[initializerList.Count];
                        for (int j = 0; j < i; j++)
                        {
                            newList[j] = initializerList[j];
                        }
                    }
                }
                if (newList != null)
                {
                    newList[i] = expr;
                }
            }
            if (newList != null)
            {
                initializerList = new TrueReadOnlyCollection<Expression>(newList);
            }

            return NewArrayExpression.Make(ExpressionType.NewArrayInit, type.MakeArrayType(), initializerList);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.NewArrayExpression" /> that represents creating an array that has a specified rank.</summary>
        /// <param name="type">A <see cref="System.Type" /> that represents the element type of the array.</param>
        /// <param name="bounds">An array of <see cref="System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="System.Linq.Expressions.NewArrayExpression.Expressions" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.NewArrayExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.NewArrayBounds" /> and the <see cref="System.Linq.Expressions.NewArrayExpression.Expressions" /> property set to the specified value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="type" /> or <paramref name="bounds" /> is <see langword="null" />.
        /// -or-
        /// An element of <paramref name="bounds" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">The <see cref="System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="bounds" /> does not represent an integral type.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.NewArrayExpression" /> represents an array type whose rank is equal to the length of <paramref name="bounds" /> and whose element type is <paramref name="type" />.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of each element of <paramref name="bounds" /> must represent an integral type.</remarks>
        /// <example>The following example demonstrates how to use the <see cref="O:System.Linq.Expressions.Expression.NewArrayBounds" /> method to create an expression tree that represents creating a string array that has a rank of 2.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet2":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet2":::</example>
        public static NewArrayExpression NewArrayBounds(Type type, params Expression[] bounds)
        {
            return NewArrayBounds(type, (IEnumerable<Expression>)bounds);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.NewArrayExpression" /> that represents creating an array that has a specified rank.</summary>
        /// <param name="type">A <see cref="System.Type" /> that represents the element type of the array.</param>
        /// <param name="bounds">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="System.Linq.Expressions.NewArrayExpression.Expressions" /> collection.</param>
        /// <returns>A <see cref="System.Linq.Expressions.NewArrayExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.NewArrayBounds" /> and the <see cref="System.Linq.Expressions.NewArrayExpression.Expressions" /> property set to the specified value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="type" /> or <paramref name="bounds" /> is <see langword="null" />.
        /// -or-
        /// An element of <paramref name="bounds" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">The <see cref="System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="bounds" /> does not represent an integral type.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.NewArrayExpression" /> represents an array type whose rank is equal to the length of <paramref name="bounds" /> and whose element type is <paramref name="type" />.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of each element of <paramref name="bounds" /> must represent an integral type.</remarks>
        /// <example>The following example demonstrates how to use the <see cref="O:System.Linq.Expressions.Expression.NewArrayBounds" /> method to create an expression tree that represents creating a string array that has a rank of 2.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet2":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet2":::</example>
        public static NewArrayExpression NewArrayBounds(Type type, IEnumerable<Expression> bounds)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            ContractUtils.RequiresNotNull(bounds, nameof(bounds));

            if (type == typeof(void))
            {
                throw Error.ArgumentCannotBeOfTypeVoid(nameof(type));
            }

            TypeUtils.ValidateType(type, nameof(type));

            ReadOnlyCollection<Expression> boundsList = bounds.ToReadOnly();

            int dimensions = boundsList.Count;
            if (dimensions <= 0) throw Error.BoundsCannotBeLessThanOne(nameof(bounds));

            for (int i = 0; i < dimensions; i++)
            {
                Expression expr = boundsList[i];
                ExpressionUtils.RequiresCanRead(expr, nameof(bounds), i);
                if (!expr.Type.IsInteger())
                {
                    throw Error.ArgumentMustBeInteger(nameof(bounds), i);
                }
            }

            Type arrayType;
            if (dimensions == 1)
            {
                //To get a vector, need call Type.MakeArrayType().
                //Type.MakeArrayType(1) gives a non-vector array, which will cause type check error.
                arrayType = type.MakeArrayType();
            }
            else
            {
                arrayType = type.MakeArrayType(dimensions);
            }

            return NewArrayExpression.Make(ExpressionType.NewArrayBounds, arrayType, boundsList);
        }

        #endregion
    }
}
