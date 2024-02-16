// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Linq.Expressions;

namespace System.Dynamic
{
    /// <summary>
    /// Represents the binary dynamic operation at the call site, providing the binding semantic and the details about the operation.
    /// </summary>
    [RequiresDynamicCode(Expression.CallSiteRequiresDynamicCode)]
    public abstract class BinaryOperationBinder : DynamicMetaObjectBinder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryOperationBinder"/> class.
        /// </summary>
        /// <param name="operation">The binary operation kind.</param>
        protected BinaryOperationBinder(ExpressionType operation)
        {
            ContractUtils.Requires(OperationIsValid(operation), nameof(operation));
            Operation = operation;
        }

        /// <summary>
        /// The result type of the operation.
        /// </summary>
        public sealed override Type ReturnType => typeof(object);

        /// <summary>
        /// The binary operation kind.
        /// </summary>
        public ExpressionType Operation { get; }

        /// <summary>
        /// Performs the binding of the binary dynamic operation if the target dynamic object cannot bind.
        /// </summary>
        /// <param name="target">The target of the dynamic binary operation.</param>
        /// <param name="arg">The right hand side operand of the dynamic binary operation.</param>
        /// <returns>The <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg)
        {
            return FallbackBinaryOperation(target, arg, null);
        }

        /// <summary>
        /// When overridden in the derived class, performs the binding of the binary dynamic operation if the target dynamic object cannot bind.
        /// </summary>
        /// <param name="target">The target of the dynamic binary operation.</param>
        /// <param name="arg">The right hand side operand of the dynamic binary operation.</param>
        /// <param name="errorSuggestion">The binding result in case the binding fails, or null.</param>
        /// <returns>The <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public abstract DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject? errorSuggestion);

        /// <summary>
        /// Performs the binding of the dynamic binary operation.
        /// </summary>
        /// <param name="target">The target of the dynamic operation.</param>
        /// <param name="args">An array of arguments of the dynamic operation.</param>
        /// <returns>The <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(args);
            ContractUtils.Requires(args.Length == 1, nameof(args));

            var arg0 = args[0];
            ArgumentNullException.ThrowIfNull(arg0, nameof(args));

            return target.BindBinaryOperation(this, arg0);
        }

        /// <summary>
        /// Always returns <c>true</c> because this is a standard <see cref="DynamicMetaObjectBinder"/>.
        /// </summary>
        internal sealed override bool IsStandardBinder => true;

        internal static bool OperationIsValid(ExpressionType operation)
        {
            switch (operation)
            {
                case ExpressionType.Add:
                case ExpressionType.And:
                case ExpressionType.Divide:
                case ExpressionType.Equal:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LeftShift:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.NotEqual:
                case ExpressionType.Or:
                case ExpressionType.Power:
                case ExpressionType.RightShift:
                case ExpressionType.Subtract:
                case ExpressionType.AddAssign:
                case ExpressionType.AndAssign:
                case ExpressionType.DivideAssign:
                case ExpressionType.ExclusiveOrAssign:
                case ExpressionType.LeftShiftAssign:
                case ExpressionType.ModuloAssign:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.OrAssign:
                case ExpressionType.PowerAssign:
                case ExpressionType.RightShiftAssign:
                case ExpressionType.SubtractAssign:
                case ExpressionType.Extension:
                    return true;

                default:
                    return false;
            }
        }
    }
}
