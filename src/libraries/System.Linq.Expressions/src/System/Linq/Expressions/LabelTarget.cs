// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>Used to represent the target of a <see cref="System.Linq.Expressions.GotoExpression" />.</summary>
    /// <remarks></remarks>
    /// <example>The following example demonstrates how to create an expression that contains a <see cref="System.Linq.Expressions.LabelTarget" /> object by using the <see cref="O:System.Linq.Expressions.Expression.Label" /> method.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet43":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet43":::</example>
    public sealed class LabelTarget
    {
        internal LabelTarget(Type type, string? name)
        {
            Type = type;
            Name = name;
        }

        /// <summary>Gets the name of the label.</summary>
        /// <value>The name of the label.</value>
        /// <remarks>The label's name is provided for information purposes only.</remarks>
        public string? Name { get; }

        /// <summary>The type of value that is passed when jumping to the label (or <see cref="void" /> if no value should be passed).</summary>
        /// <value>The <see cref="System.Type" /> object representing the type of the value that is passed when jumping to the label or <see cref="void" /> if no value should be passed</value>
        public Type Type { get; }

        /// <summary>Returns a <see cref="string" /> that represents the current <see cref="object" />.</summary>
        /// <returns>A <see cref="string" /> that represents the current <see cref="object" />.</returns>
        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? "UnamedLabel" : Name;
        }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.LabelTarget" /> representing a label with void type and no name.</summary>
        /// <returns>The new <see cref="System.Linq.Expressions.LabelTarget" />.</returns>
        /// <remarks></remarks>
        /// <example>The following example demonstrates how to create an expression that contains a <see cref="System.Linq.Expressions.LabelTarget" /> object.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet43":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet43":::</example>
        public static LabelTarget Label()
        {
            return Label(typeof(void), name: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.LabelTarget" /> representing a label with void type and the given name.</summary>
        /// <param name="name">The name of the label.</param>
        /// <returns>The new <see cref="System.Linq.Expressions.LabelTarget" />.</returns>
        public static LabelTarget Label(string? name)
        {
            return Label(typeof(void), name);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.LabelTarget" /> representing a label with the given type.</summary>
        /// <param name="type">The type of value that is passed when jumping to the label.</param>
        /// <returns>The new <see cref="System.Linq.Expressions.LabelTarget" />.</returns>
        /// <remarks></remarks>
        /// <example>The following example demonstrates how to use a <see cref="System.Linq.Expressions.LabelTarget" /> object in a loop expression.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet44":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet44":::</example>
        public static LabelTarget Label(Type type)
        {
            return Label(type, name: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.LabelTarget" /> representing a label with the given type and name.</summary>
        /// <param name="type">The type of value that is passed when jumping to the label.</param>
        /// <param name="name">The name of the label.</param>
        /// <returns>The new <see cref="System.Linq.Expressions.LabelTarget" />.</returns>
        public static LabelTarget Label(Type type, string? name)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            TypeUtils.ValidateType(type, nameof(type));
            return new LabelTarget(type, name);
        }
    }
}
