// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>Represents a named parameter expression.</summary>
    /// <remarks>Use the <see cref="O:System.Linq.Expressions.Expression.Parameter" /> factory method to create a <see cref="System.Linq.Expressions.ParameterExpression" />.
    /// The value of the <see cref="O:System.Linq.Expressions.Expression.NodeType" /> property of a <see cref="System.Linq.Expressions.ParameterExpression" /> object is <see cref="System.Linq.Expressions.ExpressionType.Parameter" />.</remarks>
    /// <example>The following example demonstrates how to create a <see cref="System.Linq.Expressions.MethodCallExpression" /> object that prints the value of a <see cref="System.Linq.Expressions.ParameterExpression" /> object by using the <see cref="O:System.Linq.Expressions.Expression.Parameter" /> method.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet49":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet49":::</example>
    [DebuggerTypeProxy(typeof(ParameterExpressionProxy))]
    public class ParameterExpression : Expression
    {
        internal ParameterExpression(string? name)
        {
            Name = name;
        }

        internal static ParameterExpression Make(Type type, string? name, bool isByRef)
        {
            if (isByRef)
            {
                return new ByRefParameterExpression(type, name);
            }
            else
            {
                if (!type.IsEnum)
                {
                    switch (type.GetTypeCode())
                    {
                        case TypeCode.Boolean: return new PrimitiveParameterExpression<bool>(name);
                        case TypeCode.Byte: return new PrimitiveParameterExpression<byte>(name);
                        case TypeCode.Char: return new PrimitiveParameterExpression<char>(name);
                        case TypeCode.DateTime: return new PrimitiveParameterExpression<DateTime>(name);
                        case TypeCode.Decimal: return new PrimitiveParameterExpression<decimal>(name);
                        case TypeCode.Double: return new PrimitiveParameterExpression<double>(name);
                        case TypeCode.Int16: return new PrimitiveParameterExpression<short>(name);
                        case TypeCode.Int32: return new PrimitiveParameterExpression<int>(name);
                        case TypeCode.Int64: return new PrimitiveParameterExpression<long>(name);
                        case TypeCode.Object:
                            // common reference types which we optimize go here.  Of course object is in
                            // the list, the others are driven by profiling of various workloads.  This list
                            // should be kept short.
                            if (type == typeof(object))
                            {
                                return new ParameterExpression(name);
                            }
                            else if (type == typeof(Exception))
                            {
                                return new PrimitiveParameterExpression<Exception>(name);
                            }
                            else if (type == typeof(object[]))
                            {
                                return new PrimitiveParameterExpression<object[]>(name);
                            }
                            break;
                        case TypeCode.SByte: return new PrimitiveParameterExpression<sbyte>(name);
                        case TypeCode.Single: return new PrimitiveParameterExpression<float>(name);
                        case TypeCode.String: return new PrimitiveParameterExpression<string>(name);
                        case TypeCode.UInt16: return new PrimitiveParameterExpression<ushort>(name);
                        case TypeCode.UInt32: return new PrimitiveParameterExpression<uint>(name);
                        case TypeCode.UInt64: return new PrimitiveParameterExpression<ulong>(name);
                    }
                }
            }

            return new TypedParameterExpression(type, name);
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ParameterExpression.Type" /> that represents the static type of the expression.</value>
        public override Type Type => typeof(object);

        /// <summary>Returns the node type of this <see cref="System.Linq.Expressions.Expression" />.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> that represents this expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.Parameter;

        /// <summary>Gets the name of the parameter or variable.</summary>
        /// <value>A <see cref="string" /> that contains the name of the parameter.</value>
        public string? Name { get; }

        /// <summary>Indicates that this <c>ParameterExpression</c> is to be treated as a <see langword="ByRef" /> parameter.</summary>
        /// <value><see langword="true" /> if this <c>ParameterExpression</c> is a <see langword="ByRef" /> parameter; otherwise, <see langword="false" />.</value>
        public bool IsByRef => GetIsByRef();

        internal virtual bool GetIsByRef() => false;

        /// <summary>Dispatches to the specific visit method for this node type. For example, <see cref="System.Linq.Expressions.MethodCallExpression" /> calls the <see cref="System.Linq.Expressions.ExpressionVisitor.VisitMethodCall(System.Linq.Expressions.MethodCallExpression)" />.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        /// <remarks>This default implementation for <see cref="System.Linq.Expressions.ExpressionType.Extension" /> nodes calls <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />. Override this method to call into a more specific method on a derived visitor class of the <see cref="System.Linq.Expressions.ExpressionVisitor" /> class. However, it should still support unknown visitors by calling <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />.</remarks>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitParameter(this);
        }
    }

    /// <summary>
    /// Specialized subclass to avoid holding onto the byref flag in a
    /// parameter expression.  This version always holds onto the expression
    /// type explicitly and therefore derives from TypedParameterExpression.
    /// </summary>
    internal sealed class ByRefParameterExpression : TypedParameterExpression
    {
        internal ByRefParameterExpression(Type type, string? name)
            : base(type, name)
        {
        }

        internal override bool GetIsByRef() => true;
    }

    /// <summary>
    /// Specialized subclass which holds onto the type of the expression for
    /// uncommon types.
    /// </summary>
    internal class TypedParameterExpression : ParameterExpression
    {
        internal TypedParameterExpression(Type type, string? name)
            : base(name)
        {
            Type = type;
        }

        public sealed override Type Type { get; }
    }

    /// <summary>
    /// Generic type to avoid needing explicit storage for primitive data types
    /// which are commonly used.
    /// </summary>
    internal sealed class PrimitiveParameterExpression<T> : ParameterExpression
    {
        internal PrimitiveParameterExpression(string? name)
            : base(name)
        {
        }

        public sealed override Type Type => typeof(T);
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.ParameterExpression" /> node that can be used to identify a parameter or a variable in an expression tree.</summary>
        /// <param name="type">The type of the parameter or variable.</param>
        /// <returns>A <see cref="System.Linq.Expressions.ParameterExpression" /> node with the specified name and type.</returns>
        /// <remarks></remarks>
        /// <example>The following example demonstrates how to create a <see cref="System.Linq.Expressions.MethodCallExpression" /> object that prints the value of a <see cref="System.Linq.Expressions.ParameterExpression" /> object.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet49":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet49":::</example>
        public static ParameterExpression Parameter(Type type)
        {
            return Parameter(type, name: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.ParameterExpression" /> node that can be used to identify a parameter or a variable in an expression tree.</summary>
        /// <param name="type">The type of the parameter or variable.</param>
        /// <returns>A <see cref="System.Linq.Expressions.ParameterExpression" /> node with the specified name and type</returns>
        public static ParameterExpression Variable(Type type)
        {
            return Variable(type, name: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.ParameterExpression" /> node that can be used to identify a parameter or a variable in an expression tree.</summary>
        /// <param name="type">The type of the parameter or variable.</param>
        /// <param name="name">The name of the parameter or variable, used for debugging or printing purpose only.</param>
        /// <returns>A <see cref="System.Linq.Expressions.ParameterExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Parameter" /> and the <see cref="System.Linq.Expressions.Expression.Type" /> and <see cref="System.Linq.Expressions.ParameterExpression.Name" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
        public static ParameterExpression Parameter(Type type, string? name)
        {
            Validate(type, allowByRef: true);
            bool byref = type.IsByRef;
            if (byref)
            {
                type = type.GetElementType()!;
            }

            return ParameterExpression.Make(type, name, byref);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.ParameterExpression" /> node that can be used to identify a parameter or a variable in an expression tree.</summary>
        /// <param name="type">The type of the parameter or variable.</param>
        /// <param name="name">The name of the parameter or variable. This name is used for debugging or printing purpose only.</param>
        /// <returns>A <see cref="System.Linq.Expressions.ParameterExpression" /> node with the specified name and type.</returns>
        public static ParameterExpression Variable(Type type, string? name)
        {
            Validate(type, allowByRef: false);
            return ParameterExpression.Make(type, name, isByRef: false);
        }

        private static void Validate(Type type, bool allowByRef)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            TypeUtils.ValidateType(type, nameof(type), allowByRef, allowPointer: false);

            if (type == typeof(void))
            {
                throw Error.ArgumentCannotBeOfTypeVoid(nameof(type));
            }
        }
    }
}
