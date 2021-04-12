// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>Represents an initializer for a single element of an <see cref="System.Collections.IEnumerable" /> collection.</summary>
    /// <remarks></remarks>
    /// <example>The following example creates an <see cref="System.Linq.Expressions.ElementInit" /> that represents the initialization of an element of a dictionary collection.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet4":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet4":::</example>
    public sealed class ElementInit : IArgumentProvider
    {
        internal ElementInit(MethodInfo addMethod, ReadOnlyCollection<Expression> arguments)
        {
            AddMethod = addMethod;
            Arguments = arguments;
        }

        /// <summary>Gets the instance method that is used to add an element to an <see cref="System.Collections.IEnumerable" /> collection.</summary>
        /// <value>A <see cref="System.Reflection.MethodInfo" /> that represents an instance method that adds an element to a collection.</value>
        public MethodInfo AddMethod { get; }

        /// <summary>Gets the collection of arguments that are passed to a method that adds an element to an <see cref="System.Collections.IEnumerable" /> collection.</summary>
        /// <value>A <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" /> of <see cref="System.Linq.Expressions.Expression" /> objects that represent the arguments for a method that adds an element to a collection.</value>
        public ReadOnlyCollection<Expression> Arguments { get; }

        /// <summary>
        /// Gets the argument expression with the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the argument expression to get.</param>
        /// <returns>The expression representing the argument at the specified <paramref name="index"/>.</returns>
        public Expression GetArgument(int index) => Arguments[index];

        /// <summary>
        /// Gets the number of argument expressions of the node.
        /// </summary>
        public int ArgumentCount => Arguments.Count;

        /// <summary>Returns a textual representation of an <see cref="System.Linq.Expressions.ElementInit" /> object.</summary>
        /// <returns>A textual representation of the <see cref="System.Linq.Expressions.ElementInit" /> object.</returns>
        public override string ToString()
        {
            return ExpressionStringBuilder.ElementInitBindingToString(this);
        }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="arguments">The <see cref="System.Linq.Expressions.ElementInit.Arguments" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public ElementInit Update(IEnumerable<Expression> arguments)
        {
            if (arguments == Arguments)
            {
                return this;
            }
            return Expression.ElementInit(AddMethod, arguments);
        }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates an <see cref="System.Linq.Expressions.ElementInit" />, given an array of values as the second argument.</summary>
        /// <param name="addMethod">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.ElementInit.AddMethod" /> property equal to.</param>
        /// <param name="arguments">An array of <see cref="System.Linq.Expressions.Expression" /> objects to set the <see cref="System.Linq.Expressions.ElementInit.Arguments" /> property equal to.</param>
        /// <returns>An <see cref="System.Linq.Expressions.ElementInit" /> that has the <see cref="System.Linq.Expressions.ElementInit.AddMethod" /> and <see cref="System.Linq.Expressions.ElementInit.Arguments" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="addMethod" /> or <paramref name="arguments" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">The method that addMethod represents is not named "Add" (case insensitive).
        /// -or-
        /// The method that addMethod represents is not an instance method.
        /// -or-
        /// arguments does not contain the same number of elements as the number of parameters for the method that addMethod represents.
        /// -or-
        /// The <see cref="System.Linq.Expressions.Expression.Type" /> property of one or more elements of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the method that <paramref name="addMethod" /> represents.</exception>
        /// <remarks>The <paramref name="addMethod" /> parameter must represent an instance method named "Add" (case insensitive). The add method must have the same number of parameters as the number of elements in <paramref name="arguments" />. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of each element in <paramref name="arguments" /> must be assignable to the type of the corresponding parameter of the add method, possibly after *quoting*.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  An element will be quoted only if the corresponding method parameter is of type <xref:System.Linq.Expressions.Expression>. Quoting means the element is wrapped in a <xref:System.Linq.Expressions.ExpressionType.Quote> node. The resulting node is a <xref:System.Linq.Expressions.UnaryExpression> whose <xref:System.Linq.Expressions.UnaryExpression.Operand%2A> property is the element of `arguments`.
        /// ]]></format></remarks>
        /// <example>The following example demonstrates how to use the <see cref="System.Linq.Expressions.Expression.ElementInit(System.Reflection.MethodInfo,System.Linq.Expressions.Expression[])" /> method to create an <see cref="System.Linq.Expressions.ElementInit" /> that represents calling the <see cref="O:System.Collections.Generic.Dictionary{T1,T2}.Add" /> method to initialize an element of a dictionary collection.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet4":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet4":::</example>
        public static ElementInit ElementInit(MethodInfo addMethod, params Expression[] arguments)
        {
            return ElementInit(addMethod, arguments as IEnumerable<Expression>);
        }

        /// <summary>Creates an <see cref="System.Linq.Expressions.ElementInit" />, given an <see cref="System.Collections.Generic.IEnumerable{T}" /> as the second argument.</summary>
        /// <param name="addMethod">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.ElementInit.AddMethod" /> property equal to.</param>
        /// <param name="arguments">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.Expression" /> objects to set the <see cref="System.Linq.Expressions.ElementInit.Arguments" /> property equal to.</param>
        /// <returns>An <see cref="System.Linq.Expressions.ElementInit" /> that has the <see cref="System.Linq.Expressions.ElementInit.AddMethod" /> and <see cref="System.Linq.Expressions.ElementInit.Arguments" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="addMethod" /> or <paramref name="arguments" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">The method that <paramref name="addMethod" /> represents is not named "Add" (case insensitive).
        /// -or-
        /// The method that <paramref name="addMethod" /> represents is not an instance method.
        /// -or-
        /// <paramref name="arguments" /> does not contain the same number of elements as the number of parameters for the method that <paramref name="addMethod" /> represents.
        /// -or-
        /// The <see cref="System.Linq.Expressions.Expression.Type" /> property of one or more elements of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the method that <paramref name="addMethod" /> represents.</exception>
        /// <remarks>The <paramref name="addMethod" /> parameter must represent an instance method named "Add" (case insensitive). The add method must have the same number of parameters as the number of elements in <paramref name="arguments" />. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of each element in <paramref name="arguments" /> must be assignable to the type of the corresponding parameter of the add method, possibly after *quoting*.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  An element will be quoted only if the corresponding method parameter is of type <xref:System.Linq.Expressions.Expression>. Quoting means the element is wrapped in a <xref:System.Linq.Expressions.ExpressionType.Quote> node. The resulting node is a <xref:System.Linq.Expressions.UnaryExpression> whose <xref:System.Linq.Expressions.UnaryExpression.Operand%2A> property is the element of `arguments`.
        /// ]]></format></remarks>
        /// <example>The following example demonstrates how to use the <see cref="System.Linq.Expressions.Expression.ElementInit(System.Reflection.MethodInfo,System.Linq.Expressions.Expression[])" /> method to create an <see cref="System.Linq.Expressions.ElementInit" /> that represents calling the <see cref="O:System.Collections.Generic.Dictionary{T1,T2}.Add" /> method to initialize an element of a dictionary collection.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet4":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet4":::</example>
        public static ElementInit ElementInit(MethodInfo addMethod, IEnumerable<Expression> arguments)
        {
            ContractUtils.RequiresNotNull(addMethod, nameof(addMethod));
            ContractUtils.RequiresNotNull(arguments, nameof(arguments));

            ReadOnlyCollection<Expression> argumentsRO = arguments.ToReadOnly();

            RequiresCanRead(argumentsRO, nameof(arguments));
            ValidateElementInitAddMethodInfo(addMethod, nameof(addMethod));
            ValidateArgumentTypes(addMethod, ExpressionType.Call, ref argumentsRO, nameof(addMethod));
            return new ElementInit(addMethod, argumentsRO);
        }

        private static void ValidateElementInitAddMethodInfo(MethodInfo addMethod, string paramName)
        {
            ValidateMethodInfo(addMethod, paramName);
            ParameterInfo[] pis = addMethod.GetParametersCached();
            if (pis.Length == 0)
            {
                throw Error.ElementInitializerMethodWithZeroArgs(paramName);
            }
            if (!addMethod.Name.Equals("Add", StringComparison.OrdinalIgnoreCase))
            {
                throw Error.ElementInitializerMethodNotAdd(paramName);
            }
            if (addMethod.IsStatic)
            {
                throw Error.ElementInitializerMethodStatic(paramName);
            }
            foreach (ParameterInfo pi in pis)
            {
                if (pi.ParameterType.IsByRef)
                {
                    throw Error.ElementInitializerMethodNoRefOutParam(pi.Name, addMethod.Name, paramName);
                }
            }
        }
    }
}
