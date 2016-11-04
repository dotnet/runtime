// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents an expression that has a constant value.
    /// </summary>
    [DebuggerTypeProxy(typeof(ConstantExpressionProxy))]
    public class ConstantExpression : Expression
    {
        internal ConstantExpression(object value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression"/> represents.
        /// </summary>
        /// <returns>The <see cref="System.Type"/> that represents the static type of the expression.</returns>
        public override Type Type
        {
            get
            {
                if (Value == null)
                {
                    return typeof(object);
                }

                return Value.GetType();
            }
        }

        /// <summary>
        /// Returns the node type of this Expression. Extension nodes should return
        /// ExpressionType.Extension when overriding this method.
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> of the expression.</returns>
        public sealed override ExpressionType NodeType => ExpressionType.Constant;

        /// <summary>
        /// Gets the value of the constant expression.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitConstant(this);
        }
    }

    internal class TypedConstantExpression : ConstantExpression
    {
        internal TypedConstantExpression(object value, Type type)
            : base(value)
        {
            Type = type;
        }

        public sealed override Type Type { get; }
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="ConstantExpression"/> that has the <see cref="ConstantExpression.Value"/> property set to the specified value. .
        /// </summary>
        /// <param name="value">An <see cref="Object"/> to set the <see cref="ConstantExpression.Value"/> property equal to.</param>
        /// <returns>
        /// A <see cref="ConstantExpression"/> that has the <see cref="NodeType"/> property equal to 
        /// <see cref="ExpressionType.Constant"/> and the <see cref="ConstantExpression.Value"/> property set to the specified value.
        /// </returns>
        public static ConstantExpression Constant(object value)
        {
            return new ConstantExpression(value);
        }

        /// <summary>
        /// Creates a <see cref="ConstantExpression"/> that has the <see cref="ConstantExpression.Value"/> 
        /// and <see cref="ConstantExpression.Type"/> properties set to the specified values. .
        /// </summary>
        /// <param name="value">An <see cref="Object"/> to set the <see cref="ConstantExpression.Value"/> property equal to.</param>
        /// <param name="type">A <see cref="Type"/> to set the <see cref="Type"/> property equal to.</param>
        /// <returns>
        /// A <see cref="ConstantExpression"/> that has the <see cref="NodeType"/> property equal to 
        /// <see cref="ExpressionType.Constant"/> and the <see cref="ConstantExpression.Value"/> and 
        /// <see cref="Type"/> properties set to the specified values.
        /// </returns>
        public static ConstantExpression Constant(object value, Type type)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            TypeUtils.ValidateType(type, nameof(type));
            if (type.IsByRef)
            {
                throw Error.TypeMustNotBeByRef(nameof(type));
            }

            if (type.IsPointer)
            {
                throw Error.TypeMustNotBePointer(nameof(type));
            }

            if (value == null)
            {
                if (type == typeof(object))
                {
                    return new ConstantExpression(null);
                }

                if (!type.GetTypeInfo().IsValueType || type.IsNullableType())
                {
                    return new TypedConstantExpression(null, type);
                }
            }
            else
            {
                Type valueType = value.GetType();
                if (type == valueType)
                {
                    return new ConstantExpression(value);
                }

                if (type.IsAssignableFrom(valueType))
                {
                    return new TypedConstantExpression(value, type);
                }
            }

            throw Error.ArgumentTypesMustMatch();
        }
    }
}
