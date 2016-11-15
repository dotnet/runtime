// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents creating a new array and possibly initializing the elements of the new array.
    /// </summary>
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

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression"/> represents. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="System.Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type { get; }

        /// <summary>
        /// Gets the bounds of the array if the value of the <see cref="ExpressionType"/> property is NewArrayBounds, or the values to initialize the elements of the new array if the value of the <see cref="Expression.NodeType"/> property is NewArrayInit.
        /// </summary>
        public ReadOnlyCollection<Expression> Expressions { get; }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitNewArray(this);
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="expressions">The <see cref="Expressions"/> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public NewArrayExpression Update(IEnumerable<Expression> expressions)
        {
            if (expressions == Expressions)
            {
                return this;
            }
            if (NodeType == ExpressionType.NewArrayInit)
            {
                return Expression.NewArrayInit(Type.GetElementType(), expressions);
            }
            return Expression.NewArrayBounds(Type.GetElementType(), expressions);
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

    public partial class Expression
    {
        #region NewArrayInit

        /// <summary>
        /// Creates a <see cref="NewArrayExpression"/> of the specified type from the provided initializers.
        /// </summary>
        /// <param name="type">A Type that represents the element type of the array.</param>
        /// <param name="initializers">The expressions used to create the array elements.</param>
        /// <returns>A <see cref="NewArrayExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.NewArrayInit"/> and the <see cref="NewArrayExpression.Expressions"/> property set to the specified value.</returns>
        public static NewArrayExpression NewArrayInit(Type type, params Expression[] initializers)
        {
            return NewArrayInit(type, (IEnumerable<Expression>)initializers);
        }

        /// <summary>
        /// Creates a <see cref="NewArrayExpression"/> of the specified type from the provided initializers.
        /// </summary>
        /// <param name="type">A Type that represents the element type of the array.</param>
        /// <param name="initializers">The expressions used to create the array elements.</param>
        /// <returns>A <see cref="NewArrayExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.NewArrayInit"/> and the <see cref="NewArrayExpression.Expressions"/> property set to the specified value.</returns>
        public static NewArrayExpression NewArrayInit(Type type, IEnumerable<Expression> initializers)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            ContractUtils.RequiresNotNull(initializers, nameof(initializers));
            if (type == typeof(void))
            {
                throw Error.ArgumentCannotBeOfTypeVoid(nameof(type));
            }

            TypeUtils.ValidateType(type, nameof(type));
            if (type.IsByRef)
            {
                throw Error.TypeMustNotBeByRef(nameof(type));
            }

            if (type.IsPointer)
            {
                throw Error.TypeMustNotBePointer(nameof(type));
            }

            ReadOnlyCollection<Expression> initializerList = initializers.ToReadOnly();

            Expression[] newList = null;
            for (int i = 0, n = initializerList.Count; i < n; i++)
            {
                Expression expr = initializerList[i];
                RequiresCanRead(expr, nameof(initializers), i);

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

        #endregion

        #region NewArrayBounds

        /// <summary>
        /// Creates a <see cref="NewArrayExpression"/> that represents creating an array that has a specified rank.
        /// </summary>
        /// <param name="type">A <see cref="System.Type"/> that represents the element type of the array.</param>
        /// <param name="bounds">An array that contains Expression objects to use to populate the <see cref="NewArrayExpression.Expressions"/> collection.</param>
        /// <returns>A <see cref="NewArrayExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.NewArrayBounds"/> and the <see cref="NewArrayExpression.Expressions"/> property set to the specified value.</returns>
        public static NewArrayExpression NewArrayBounds(Type type, params Expression[] bounds)
        {
            return NewArrayBounds(type, (IEnumerable<Expression>)bounds);
        }

        /// <summary>
        /// Creates a <see cref="NewArrayExpression"/> that represents creating an array that has a specified rank.
        /// </summary>
        /// <param name="type">A <see cref="System.Type"/> that represents the element type of the array.</param>
        /// <param name="bounds">An <see cref="IEnumerable{T}"/> that contains <see cref="Expression"/> objects to use to populate the <see cref="NewArrayExpression.Expressions"/> collection.</param>
        /// <returns>A <see cref="NewArrayExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.NewArrayBounds"/> and the <see cref="NewArrayExpression.Expressions"/> property set to the specified value.</returns>
        public static NewArrayExpression NewArrayBounds(Type type, IEnumerable<Expression> bounds)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            ContractUtils.RequiresNotNull(bounds, nameof(bounds));

            if (type == typeof(void))
            {
                throw Error.ArgumentCannotBeOfTypeVoid(nameof(type));
            }

            TypeUtils.ValidateType(type, nameof(type));
            if (type.IsByRef)
            {
                throw Error.TypeMustNotBeByRef(nameof(type));
            }

            if (type.IsPointer)
            {
                throw Error.TypeMustNotBePointer(nameof(type));
            }

            ReadOnlyCollection<Expression> boundsList = bounds.ToReadOnly();

            int dimensions = boundsList.Count;
            if (dimensions <= 0) throw Error.BoundsCannotBeLessThanOne(nameof(bounds));

            for (int i = 0; i < dimensions; i++)
            {
                Expression expr = boundsList[i];
                RequiresCanRead(expr, nameof(bounds), i);
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

            return NewArrayExpression.Make(ExpressionType.NewArrayBounds, arrayType, bounds.ToReadOnly());
        }

        #endregion
    }
}
