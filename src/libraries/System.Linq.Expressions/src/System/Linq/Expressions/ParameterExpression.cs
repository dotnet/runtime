// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents a named parameter expression.
    /// </summary>
    [DebuggerTypeProxy(typeof(ParameterExpressionProxy))]
    public class ParameterExpression : Expression
    {
        internal ParameterExpression(string name)
        {
            Name = name;
        }

        internal static ParameterExpression Make(Type type, string name, bool isByRef)
        {
            if (isByRef)
            {
                return new ByRefParameterExpression(type, name);
            }
            else
            {
                if (!type.GetTypeInfo().IsEnum)
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

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression"/> represents. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="System.Type"/> that represents the static type of the expression.</returns>
        public override Type Type => typeof(object);

        /// <summary>
        /// Returns the node type of this <see cref="Expression"/>. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> that represents this expression.</returns>
        public sealed override ExpressionType NodeType => ExpressionType.Parameter;

        /// <summary>
        /// The Name of the parameter or variable.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Indicates that this <see cref="ParameterExpression"/> is to be treated as a ByRef parameter.
        /// </summary>
        public bool IsByRef => GetIsByRef();

        internal virtual bool GetIsByRef() => false;

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
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
        internal ByRefParameterExpression(Type type, string name)
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
        internal TypedParameterExpression(Type type, string name)
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
        internal PrimitiveParameterExpression(string name)
            : base(name)
        {
        }

        public sealed override Type Type => typeof(T);
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="ParameterExpression"/> node that can be used to identify a parameter or a variable in an expression tree.
        /// </summary>
        /// <param name="type">The type of the parameter or variable.</param>
        /// <returns>A <see cref="ParameterExpression"/> node with the specified name and type.</returns>
        public static ParameterExpression Parameter(Type type)
        {
            return Parameter(type, name: null);
        }

        /// <summary>
        /// Creates a <see cref="ParameterExpression"/> node that can be used to identify a parameter or a variable in an expression tree.
        /// </summary>
        /// <param name="type">The type of the parameter or variable.</param>
        /// <returns>A <see cref="ParameterExpression"/> node with the specified name and type.</returns>
        public static ParameterExpression Variable(Type type)
        {
            return Variable(type, name: null);
        }

        /// <summary>
        /// Creates a <see cref="ParameterExpression"/> node that can be used to identify a parameter or a variable in an expression tree.
        /// </summary>
        /// <param name="type">The type of the parameter or variable.</param>
        /// <param name="name">The name of the parameter or variable, used for debugging or pretty printing purpose only.</param>
        /// <returns>A <see cref="ParameterExpression"/> node with the specified name and type.</returns>
        public static ParameterExpression Parameter(Type type, string name)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));

            if (type == typeof(void))
            {
                throw Error.ArgumentCannotBeOfTypeVoid(nameof(type));
            }

            if (type.IsPointer)
            {
                throw Error.TypeMustNotBePointer(nameof(type));
            }

            bool byref = type.IsByRef;
            if (byref)
            {
                type = type.GetElementType();
            }

            return ParameterExpression.Make(type, name, byref);
        }

        /// <summary>
        /// Creates a <see cref="ParameterExpression"/> node that can be used to identify a parameter or a variable in an expression tree.
        /// </summary>
        /// <param name="type">The type of the parameter or variable.</param>
        /// <param name="name">The name of the parameter or variable, used for debugging or pretty printing purpose only.</param>
        /// <returns>A <see cref="ParameterExpression"/> node with the specified name and type.</returns>
        public static ParameterExpression Variable(Type type, string name)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            if (type == typeof(void)) throw Error.ArgumentCannotBeOfTypeVoid(nameof(type));
            if (type.IsByRef) throw Error.TypeMustNotBeByRef(nameof(type));

            if (type.IsPointer)
            {
                throw Error.TypeMustNotBePointer(nameof(type));
            }

            return ParameterExpression.Make(type, name, isByRef: false);
        }
    }
}
