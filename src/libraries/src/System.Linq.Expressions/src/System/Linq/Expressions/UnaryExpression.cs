// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents an expression that has a unary operator.
    /// </summary>
    [DebuggerTypeProxy(typeof(UnaryExpressionProxy))]
    public sealed class UnaryExpression : Expression
    {
        internal UnaryExpression(ExpressionType nodeType, Expression expression, Type type, MethodInfo method)
        {
            Operand = expression;
            Method = method;
            NodeType = nodeType;
            Type = type;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression"/> represents. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="System.Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type { get; }

        /// <summary>
        /// Returns the node type of this <see cref="Expression"/>. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> that represents this expression.</returns>
        public sealed override ExpressionType NodeType { get; }

        /// <summary>
        /// Gets the operand of the unary operation.
        /// </summary>
        /// <returns> An <see cref="ExpressionType"/> that represents the operand of the unary operation.</returns>
        public Expression Operand { get; }

        /// <summary>
        /// Gets the implementing method for the unary operation.
        /// </summary>
        /// <returns>The <see cref="MethodInfo"/> that represents the implementing method.</returns>
        public MethodInfo Method { get; }

        /// <summary>
        /// Gets a value that indicates whether the expression tree node represents a lifted call to an operator.
        /// </summary>
        /// <returns>true if the node represents a lifted call; otherwise, false.</returns>
        public bool IsLifted
        {
            get
            {
                if (NodeType == ExpressionType.TypeAs || NodeType == ExpressionType.Quote || NodeType == ExpressionType.Throw)
                {
                    return false;
                }
                bool operandIsNullable = Operand.Type.IsNullableType();
                bool resultIsNullable = this.Type.IsNullableType();
                if (Method != null)
                {
                    return (operandIsNullable && !TypeUtils.AreEquivalent(Method.GetParametersCached()[0].ParameterType, Operand.Type)) ||
                           (resultIsNullable && !TypeUtils.AreEquivalent(Method.ReturnType, this.Type));
                }
                return operandIsNullable || resultIsNullable;
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the expression tree node represents a lifted call to an operator whose return type is lifted to a nullable type.
        /// </summary>
        /// <returns>true if the operator's return type is lifted to a nullable type; otherwise, false.</returns>
        public bool IsLiftedToNull => IsLifted && this.Type.IsNullableType();

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitUnary(this);
        }

        /// <summary>
        /// Gets a value that indicates whether the expression tree node can be reduced.
        /// </summary>
        public override bool CanReduce
        {
            get
            {
                switch (NodeType)
                {
                    case ExpressionType.PreIncrementAssign:
                    case ExpressionType.PreDecrementAssign:
                    case ExpressionType.PostIncrementAssign:
                    case ExpressionType.PostDecrementAssign:
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Reduces the expression node to a simpler expression.
        /// If CanReduce returns true, this should return a valid expression.
        /// This method is allowed to return another node which itself
        /// must be reduced.
        /// </summary>
        /// <returns>The reduced expression.</returns>
        public override Expression Reduce()
        {
            if (CanReduce)
            {
                switch (Operand.NodeType)
                {
                    case ExpressionType.Index:
                        return ReduceIndex();
                    case ExpressionType.MemberAccess:
                        return ReduceMember();
                    default:
                        Debug.Assert(Operand.NodeType == ExpressionType.Parameter);
                        return ReduceVariable();
                }
            }
            return this;
        }

        private bool IsPrefix
        {
            get { return NodeType == ExpressionType.PreIncrementAssign || NodeType == ExpressionType.PreDecrementAssign; }
        }

        private UnaryExpression FunctionalOp(Expression operand)
        {
            ExpressionType functional;
            if (NodeType == ExpressionType.PreIncrementAssign || NodeType == ExpressionType.PostIncrementAssign)
            {
                functional = ExpressionType.Increment;
            }
            else
            {
                Debug.Assert(NodeType == ExpressionType.PreDecrementAssign || NodeType == ExpressionType.PostDecrementAssign);
                functional = ExpressionType.Decrement;
            }
            return new UnaryExpression(functional, operand, operand.Type, Method);
        }

        private Expression ReduceVariable()
        {
            if (IsPrefix)
            {
                // (op) var
                // ... is reduced into ...
                // var = op(var)
                return Assign(Operand, FunctionalOp(Operand));
            }
            // var (op)
            // ... is reduced into ...
            // temp = var
            // var = op(var)
            // temp
            ParameterExpression temp = Parameter(Operand.Type, name: null);
            return Block(
                new TrueReadOnlyCollection<ParameterExpression>(temp),
                new TrueReadOnlyCollection<Expression>(
                    Assign(temp, Operand),
                    Assign(Operand, FunctionalOp(temp)),
                    temp
                )
            );
        }

        private Expression ReduceMember()
        {
            var member = (MemberExpression)Operand;
            if (member.Expression == null)
            {
                //static member, reduce the same as variable
                return ReduceVariable();
            }
            else
            {
                ParameterExpression temp1 = Parameter(member.Expression.Type, name: null);
                BinaryExpression initTemp1 = Assign(temp1, member.Expression);
                member = MakeMemberAccess(temp1, member.Member);

                if (IsPrefix)
                {
                    // (op) value.member
                    // ... is reduced into ...
                    // temp1 = value
                    // temp1.member = op(temp1.member)
                    return Block(
                        new TrueReadOnlyCollection<ParameterExpression>(temp1),
                        new TrueReadOnlyCollection<Expression>(
                            initTemp1,
                            Assign(member, FunctionalOp(member))
                        )
                    );
                }

                // value.member (op)
                // ... is reduced into ...
                // temp1 = value
                // temp2 = temp1.member
                // temp1.member = op(temp2)
                // temp2
                ParameterExpression temp2 = Parameter(member.Type, name: null);
                return Block(
                    new TrueReadOnlyCollection<ParameterExpression>(temp1, temp2),
                    new TrueReadOnlyCollection<Expression>(
                        initTemp1,
                        Assign(temp2, member),
                        Assign(member, FunctionalOp(temp2)),
                        temp2
                    )
                );
            }
        }

        private Expression ReduceIndex()
        {
            // left[a0, a1, ... aN] (op)
            //
            // ... is reduced into ...
            //
            // tempObj = left
            // tempArg0 = a0
            // ...
            // tempArgN = aN
            // tempValue = tempObj[tempArg0, ... tempArgN]
            // tempObj[tempArg0, ... tempArgN] = op(tempValue)
            // tempValue

            bool prefix = IsPrefix;
            var index = (IndexExpression)Operand;
            int count = index.ArgumentCount;
            var block = new Expression[count + (prefix ? 2 : 4)];
            var temps = new ParameterExpression[count + (prefix ? 1 : 2)];
            var args = new ParameterExpression[count];

            int i = 0;
            temps[i] = Parameter(index.Object.Type, name: null);
            block[i] = Assign(temps[i], index.Object);
            i++;
            while (i <= count)
            {
                Expression arg = index.GetArgument(i - 1);
                args[i - 1] = temps[i] = Parameter(arg.Type, name: null);
                block[i] = Assign(temps[i], arg);
                i++;
            }
            index = MakeIndex(temps[0], index.Indexer, new TrueReadOnlyCollection<Expression>(args));
            if (!prefix)
            {
                ParameterExpression lastTemp = temps[i] = Parameter(index.Type, name: null);
                block[i] = Assign(temps[i], index);
                i++;
                Debug.Assert(i == temps.Length);
                block[i++] = Assign(index, FunctionalOp(lastTemp));
                block[i++] = lastTemp;
            }
            else
            {
                Debug.Assert(i == temps.Length);
                block[i++] = Assign(index, FunctionalOp(index));
            }
            Debug.Assert(i == block.Length);
            return Block(new TrueReadOnlyCollection<ParameterExpression>(temps), new TrueReadOnlyCollection<Expression>(block));
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="operand">The <see cref="Operand"/> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public UnaryExpression Update(Expression operand)
        {
            if (operand == Operand)
            {
                return this;
            }
            return Expression.MakeUnary(NodeType, operand, Type, Method);
        }
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="UnaryExpression"/>, given an operand, by calling the appropriate factory method.
        /// </summary>
        /// <param name="unaryType">The <see cref="ExpressionType"/> that specifies the type of unary operation.</param>
        /// <param name="operand">An <see cref="Expression"/> that represents the operand.</param>
        /// <param name="type">The <see cref="Type"/> that specifies the type to be converted to (pass null if not applicable).</param>
        /// <returns>The <see cref="UnaryExpression"/> that results from calling the appropriate factory method.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="unaryType"/> does not correspond to a unary expression.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="operand"/> is null.</exception>
        public static UnaryExpression MakeUnary(ExpressionType unaryType, Expression operand, Type type)
        {
            return MakeUnary(unaryType, operand, type, method: null);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/>, given an operand and implementing method, by calling the appropriate factory method.
        /// </summary>
        /// <param name="unaryType">The <see cref="ExpressionType"/> that specifies the type of unary operation.</param>
        /// <param name="operand">An <see cref="Expression"/> that represents the operand.</param>
        /// <param name="type">The <see cref="Type"/> that specifies the type to be converted to (pass null if not applicable).</param>
        /// <param name="method">The <see cref="MethodInfo"/> that represents the implementing method.</param>
        /// <returns>The <see cref="UnaryExpression"/> that results from calling the appropriate factory method.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="unaryType"/> does not correspond to a unary expression.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="operand"/> is null.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static UnaryExpression MakeUnary(ExpressionType unaryType, Expression operand, Type type, MethodInfo method)
        {
            switch (unaryType)
            {
                case ExpressionType.Negate:
                    return Negate(operand, method);
                case ExpressionType.NegateChecked:
                    return NegateChecked(operand, method);
                case ExpressionType.Not:
                    return Not(operand, method);
                case ExpressionType.IsFalse:
                    return IsFalse(operand, method);
                case ExpressionType.IsTrue:
                    return IsTrue(operand, method);
                case ExpressionType.OnesComplement:
                    return OnesComplement(operand, method);
                case ExpressionType.ArrayLength:
                    return ArrayLength(operand);
                case ExpressionType.Convert:
                    return Convert(operand, type, method);
                case ExpressionType.ConvertChecked:
                    return ConvertChecked(operand, type, method);
                case ExpressionType.Throw:
                    return Throw(operand, type);
                case ExpressionType.TypeAs:
                    return TypeAs(operand, type);
                case ExpressionType.Quote:
                    return Quote(operand);
                case ExpressionType.UnaryPlus:
                    return UnaryPlus(operand, method);
                case ExpressionType.Unbox:
                    return Unbox(operand, type);
                case ExpressionType.Increment:
                    return Increment(operand, method);
                case ExpressionType.Decrement:
                    return Decrement(operand, method);
                case ExpressionType.PreIncrementAssign:
                    return PreIncrementAssign(operand, method);
                case ExpressionType.PostIncrementAssign:
                    return PostIncrementAssign(operand, method);
                case ExpressionType.PreDecrementAssign:
                    return PreDecrementAssign(operand, method);
                case ExpressionType.PostDecrementAssign:
                    return PostDecrementAssign(operand, method);
                default:
                    throw Error.UnhandledUnary(unaryType, nameof(unaryType));
            }
        }

        private static UnaryExpression GetUserDefinedUnaryOperatorOrThrow(ExpressionType unaryType, string name, Expression operand)
        {
            UnaryExpression u = GetUserDefinedUnaryOperator(unaryType, name, operand);
            if (u != null)
            {
                ValidateParamswithOperandsOrThrow(u.Method.GetParametersCached()[0].ParameterType, operand.Type, unaryType, name);
                return u;
            }
            throw Error.UnaryOperatorNotDefined(unaryType, operand.Type);
        }

        private static UnaryExpression GetUserDefinedUnaryOperator(ExpressionType unaryType, string name, Expression operand)
        {
            Type operandType = operand.Type;
            Type[] types = new Type[] { operandType };
            Type nnOperandType = operandType.GetNonNullableType();
            MethodInfo method = nnOperandType.GetAnyStaticMethodValidated(name, types);
            if (method != null)
            {
                return new UnaryExpression(unaryType, operand, method.ReturnType, method);
            }
            // try lifted call
            if (operandType.IsNullableType())
            {
                types[0] = nnOperandType;
                method = nnOperandType.GetAnyStaticMethodValidated(name, types);
                if (method != null && method.ReturnType.IsValueType && !method.ReturnType.IsNullableType())
                {
                    return new UnaryExpression(unaryType, operand, method.ReturnType.GetNullableType(), method);
                }
            }
            return null;
        }

        private static UnaryExpression GetMethodBasedUnaryOperator(ExpressionType unaryType, Expression operand, MethodInfo method)
        {
            Debug.Assert(method != null);
            ValidateOperator(method);
            ParameterInfo[] pms = method.GetParametersCached();
            if (pms.Length != 1)
                throw Error.IncorrectNumberOfMethodCallArguments(method, nameof(method));
            if (ParameterIsAssignable(pms[0], operand.Type))
            {
                ValidateParamswithOperandsOrThrow(pms[0].ParameterType, operand.Type, unaryType, method.Name);
                return new UnaryExpression(unaryType, operand, method.ReturnType, method);
            }
            // check for lifted call
            if (operand.Type.IsNullableType() &&
                ParameterIsAssignable(pms[0], operand.Type.GetNonNullableType()) &&
                method.ReturnType.IsValueType && !method.ReturnType.IsNullableType())
            {
                return new UnaryExpression(unaryType, operand, method.ReturnType.GetNullableType(), method);
            }

            throw Error.OperandTypesDoNotMatchParameters(unaryType, method.Name);
        }

        private static UnaryExpression GetUserDefinedCoercionOrThrow(ExpressionType coercionType, Expression expression, Type convertToType)
        {
            UnaryExpression u = GetUserDefinedCoercion(coercionType, expression, convertToType);
            if (u != null)
            {
                return u;
            }
            throw Error.CoercionOperatorNotDefined(expression.Type, convertToType);
        }

        private static UnaryExpression GetUserDefinedCoercion(ExpressionType coercionType, Expression expression, Type convertToType)
        {
            MethodInfo method = TypeUtils.GetUserDefinedCoercionMethod(expression.Type, convertToType);
            if (method != null)
            {
                return new UnaryExpression(coercionType, expression, convertToType, method);
            }
            else
            {
                return null;
            }
        }

        private static UnaryExpression GetMethodBasedCoercionOperator(ExpressionType unaryType, Expression operand, Type convertToType, MethodInfo method)
        {
            Debug.Assert(method != null);
            ValidateOperator(method);
            ParameterInfo[] pms = method.GetParametersCached();
            if (pms.Length != 1)
            {
                throw Error.IncorrectNumberOfMethodCallArguments(method, nameof(method));
            }
            if (ParameterIsAssignable(pms[0], operand.Type) && TypeUtils.AreEquivalent(method.ReturnType, convertToType))
            {
                return new UnaryExpression(unaryType, operand, method.ReturnType, method);
            }
            // check for lifted call
            if ((operand.Type.IsNullableType() || convertToType.IsNullableType()) &&
                ParameterIsAssignable(pms[0], operand.Type.GetNonNullableType()) &&
                (TypeUtils.AreEquivalent(method.ReturnType, convertToType.GetNonNullableType()) ||
                TypeUtils.AreEquivalent(method.ReturnType, convertToType)))
            {
                return new UnaryExpression(unaryType, operand, convertToType, method);
            }
            throw Error.OperandTypesDoNotMatchParameters(unaryType, method.Name);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents an arithmetic negation operation.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to set the <see cref="UnaryExpression.Operand"/> property equal to.</param>
        /// <returns>A <see cref="UnaryExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Negate"/> and the <see cref="UnaryExpression.Operand"/> properties set to the specified value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the unary minus operator is not defined for <paramref name="expression"/>.Type.</exception>
        public static UnaryExpression Negate(Expression expression)
        {
            return Negate(expression, method: null);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents an arithmetic negation operation.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to set the <see cref="UnaryExpression.Operand"/> property equal to.</param>
        /// <param name="method">A <see cref="MethodInfo"/> to set the <see cref="UnaryExpression.Method"/> property equal to.</param>
        /// <returns>A <see cref="UnaryExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Negate"/> and the <see cref="UnaryExpression.Operand"/> and <see cref="UnaryExpression.Method"/> properties set to the specified value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="method"/> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly one argument.</exception>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="method"/> is null and the unary minus operator is not defined for <paramref name="expression"/>.Type (or its corresponding non-nullable type if it is a nullable value type) is not assignable to the argument type of the method represented by method.</exception>
        public static UnaryExpression Negate(Expression expression, MethodInfo method)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            if (method == null)
            {
                if (expression.Type.IsArithmetic() && !expression.Type.IsUnsignedInt())
                {
                    return new UnaryExpression(ExpressionType.Negate, expression, expression.Type, null);
                }
                return GetUserDefinedUnaryOperatorOrThrow(ExpressionType.Negate, "op_UnaryNegation", expression);
            }
            return GetMethodBasedUnaryOperator(ExpressionType.Negate, expression, method);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents a unary plus operation.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to set the <see cref="UnaryExpression.Operand"/> property equal to.</param>
        /// <returns>A <see cref="UnaryExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.UnaryPlus"/> and the <see cref="UnaryExpression.Operand"/> property set to the specified value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the unary minus operator is not defined for <paramref name="expression"/>.Type.</exception>
        public static UnaryExpression UnaryPlus(Expression expression)
        {
            return UnaryPlus(expression, method: null);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents a unary plus operation.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to set the <see cref="UnaryExpression.Operand"/> property equal to.</param>
        /// <param name="method">A <see cref="MethodInfo"/> to set the <see cref="UnaryExpression.Method"/> property equal to.</param>
        /// <returns>A <see cref="UnaryExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.UnaryPlus"/> and the <see cref="UnaryExpression.Operand"/> and <see cref="UnaryExpression.Method"/>property set to the specified value.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="method"/> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly one argument.</exception>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="method"/> is null and the unary minus operator is not defined for <paramref name="expression"/>.Type (or its corresponding non-nullable type if it is a nullable value type) is not assignable to the argument type of the method represented by method.</exception>
        public static UnaryExpression UnaryPlus(Expression expression, MethodInfo method)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            if (method == null)
            {
                if (expression.Type.IsArithmetic())
                {
                    return new UnaryExpression(ExpressionType.UnaryPlus, expression, expression.Type, null);
                }
                return GetUserDefinedUnaryOperatorOrThrow(ExpressionType.UnaryPlus, "op_UnaryPlus", expression);
            }
            return GetMethodBasedUnaryOperator(ExpressionType.UnaryPlus, expression, method);
        }

        /// <summary>Creates a <see cref="UnaryExpression"/> that represents an arithmetic negation operation that has overflow checking.</summary>
        /// <returns>A <see cref="UnaryExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.NegateChecked"/> and the <see cref="UnaryExpression.Operand"/> property set to the specified value.</returns>
        /// <param name="expression">An <see cref="Expression"/> to set the <see cref="UnaryExpression.Operand"/> property equal to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the unary minus operator is not defined for <paramref name="expression"/>.Type.</exception>
        public static UnaryExpression NegateChecked(Expression expression)
        {
            return NegateChecked(expression, method: null);
        }

        /// <summary>Creates a <see cref="UnaryExpression"/> that represents an arithmetic negation operation that has overflow checking. The implementing method can be specified.</summary>
        /// <returns>A <see cref="UnaryExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.NegateChecked"/> and the <see cref="UnaryExpression.Operand"/> and <see cref="UnaryExpression.Method"/> properties set to the specified values.</returns>
        /// <param name="expression">An <see cref="Expression"/> to set the <see cref="UnaryExpression.Operand"/> property equal to.</param>
        /// <param name="method">A <see cref="MethodInfo"/> to set the <see cref="UnaryExpression.Method"/> property equal to.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expression"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="method"/> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly one argument.</exception>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="method"/> is null and the unary minus operator is not defined for <paramref name="expression"/>.Type.-or-<paramref name="expression"/>.Type (or its corresponding non-nullable type if it is a nullable value type) is not assignable to the argument type of the method represented by <paramref name="method"/>.</exception>
        public static UnaryExpression NegateChecked(Expression expression, MethodInfo method)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            if (method == null)
            {
                if (expression.Type.IsArithmetic() && !expression.Type.IsUnsignedInt())
                {
                    return new UnaryExpression(ExpressionType.NegateChecked, expression, expression.Type, null);
                }
                return GetUserDefinedUnaryOperatorOrThrow(ExpressionType.NegateChecked, "op_UnaryNegation", expression);
            }
            return GetMethodBasedUnaryOperator(ExpressionType.NegateChecked, expression, method);
        }

        /// <summary>Creates a <see cref="UnaryExpression"/> that represents a bitwise complement operation.</summary>
        /// <returns>A <see cref="UnaryExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Not"/> and the <see cref="UnaryExpression.Operand"/> property set to the specified value.</returns>
        /// <param name="expression">An <see cref="Expression"/> to set the <see cref="UnaryExpression.Operand"/> property equal to.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expression"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The unary not operator is not defined for <paramref name="expression"/>.Type.</exception>
        public static UnaryExpression Not(Expression expression)
        {
            return Not(expression, method: null);
        }

        /// <summary>Creates a <see cref="UnaryExpression"/> that represents a bitwise complement operation. The implementing method can be specified.</summary>
        /// <returns>A <see cref="UnaryExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Not"/> and the <see cref="UnaryExpression.Operand"/> and <see cref="UnaryExpression.Method"/> properties set to the specified values.</returns>
        /// <param name="expression">An <see cref="Expression"/> to set the <see cref="UnaryExpression.Operand"/> property equal to.</param>
        /// <param name="method">A <see cref="MethodInfo"/> to set the <see cref="UnaryExpression.Method"/> property equal to.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expression"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="method"/> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly one argument.</exception>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="method"/> is null and the unary not operator is not defined for <paramref name="expression"/>.Type.-or-<paramref name="expression"/>.Type (or its corresponding non-nullable type if it is a nullable value type) is not assignable to the argument type of the method represented by <paramref name="method"/>.</exception>
        public static UnaryExpression Not(Expression expression, MethodInfo method)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            if (method == null)
            {
                if (expression.Type.IsIntegerOrBool())
                {
                    return new UnaryExpression(ExpressionType.Not, expression, expression.Type, null);
                }
                UnaryExpression u = GetUserDefinedUnaryOperator(ExpressionType.Not, "op_LogicalNot", expression);
                if (u != null)
                {
                    return u;
                }
                return GetUserDefinedUnaryOperatorOrThrow(ExpressionType.Not, "op_OnesComplement", expression);
            }
            return GetMethodBasedUnaryOperator(ExpressionType.Not, expression, method);
        }

        /// <summary>
        /// Returns whether the expression evaluates to false.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to evaluate.</param>
        /// <returns>An instance of <see cref="UnaryExpression"/>.</returns>
        public static UnaryExpression IsFalse(Expression expression)
        {
            return IsFalse(expression, method: null);
        }

        /// <summary>
        /// Returns whether the expression evaluates to false.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to evaluate.</param>
        /// <param name="method">A <see cref="MethodInfo"/> that represents the implementing method.</param>
        /// <returns>An instance of <see cref="UnaryExpression"/>.</returns>
        public static UnaryExpression IsFalse(Expression expression, MethodInfo method)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            if (method == null)
            {
                if (expression.Type.IsBool())
                {
                    return new UnaryExpression(ExpressionType.IsFalse, expression, expression.Type, null);
                }
                return GetUserDefinedUnaryOperatorOrThrow(ExpressionType.IsFalse, "op_False", expression);
            }
            return GetMethodBasedUnaryOperator(ExpressionType.IsFalse, expression, method);
        }

        /// <summary>
        /// Returns whether the expression evaluates to true.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to evaluate.</param>
        /// <returns>An instance of <see cref="UnaryExpression"/>.</returns>
        public static UnaryExpression IsTrue(Expression expression)
        {
            return IsTrue(expression, method: null);
        }

        /// <summary>
        /// Returns whether the expression evaluates to true.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to evaluate.</param>
        /// <param name="method">A <see cref="MethodInfo"/> that represents the implementing method.</param>
        /// <returns>An instance of <see cref="UnaryExpression"/>.</returns>
        public static UnaryExpression IsTrue(Expression expression, MethodInfo method)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            if (method == null)
            {
                if (expression.Type.IsBool())
                {
                    return new UnaryExpression(ExpressionType.IsTrue, expression, expression.Type, null);
                }
                return GetUserDefinedUnaryOperatorOrThrow(ExpressionType.IsTrue, "op_True", expression);
            }
            return GetMethodBasedUnaryOperator(ExpressionType.IsTrue, expression, method);
        }

        /// <summary>
        /// Returns the expression representing the ones complement.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/>.</param>
        /// <returns>An instance of <see cref="UnaryExpression"/>.</returns>
        public static UnaryExpression OnesComplement(Expression expression)
        {
            return OnesComplement(expression, method: null);
        }

        /// <summary>
        /// Returns the expression representing the ones complement.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/>.</param>
        /// <param name="method">A <see cref="MethodInfo"/> that represents the implementing method.</param>
        /// <returns>An instance of <see cref="UnaryExpression"/>.</returns>
        public static UnaryExpression OnesComplement(Expression expression, MethodInfo method)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            if (method == null)
            {
                if (expression.Type.IsInteger())
                {
                    return new UnaryExpression(ExpressionType.OnesComplement, expression, expression.Type, null);
                }
                return GetUserDefinedUnaryOperatorOrThrow(ExpressionType.OnesComplement, "op_OnesComplement", expression);
            }
            return GetMethodBasedUnaryOperator(ExpressionType.OnesComplement, expression, method);
        }

        /// <summary>Creates a <see cref="UnaryExpression"/> that represents an explicit reference or boxing conversion where null is supplied if the conversion fails.</summary>
        /// <returns>A <see cref="UnaryExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.TypeAs"/> and the <see cref="UnaryExpression.Operand"/> and <see cref="Expression.Type"/> properties set to the specified values.</returns>
        /// <param name="expression">An <see cref="Expression"/> to set the <see cref="UnaryExpression.Operand"/> property equal to.</param>
        /// <param name="type">A <see cref="System.Type"/> to set the <see cref="Type"/> property equal to.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expression"/> or <paramref name="type"/> is null.</exception>
        public static UnaryExpression TypeAs(Expression expression, Type type)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            ContractUtils.RequiresNotNull(type, nameof(type));
            TypeUtils.ValidateType(type, nameof(type));
            if (type.IsValueType && !type.IsNullableType())
            {
                throw Error.IncorrectTypeForTypeAs(type, nameof(type));
            }

            return new UnaryExpression(ExpressionType.TypeAs, expression, type, null);
        }

        /// <summary>
        /// <summary>Creates a <see cref="UnaryExpression"/> that represents an explicit unboxing.</summary>
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to unbox.</param>
        /// <param name="type">The new <see cref="System.Type"/> of the expression.</param>
        /// <returns>An instance of <see cref="UnaryExpression"/>.</returns>
        public static UnaryExpression Unbox(Expression expression, Type type)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            ContractUtils.RequiresNotNull(type, nameof(type));
            if (!expression.Type.IsInterface && expression.Type != typeof(object))
            {
                throw Error.InvalidUnboxType(nameof(expression));
            }
            if (!type.IsValueType) throw Error.InvalidUnboxType(nameof(type));
            TypeUtils.ValidateType(type, nameof(type));
            return new UnaryExpression(ExpressionType.Unbox, expression, type, null);
        }

        /// <summary>Creates a <see cref="UnaryExpression"/> that represents a conversion operation.</summary>
        /// <returns>A <see cref="UnaryExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Convert"/> and the <see cref="UnaryExpression.Operand"/> and <see cref="Expression.Type"/> properties set to the specified values.</returns>
        /// <param name="expression">An <see cref="Expression"/> to set the <see cref="UnaryExpression.Operand"/> property equal to.</param>
        /// <param name="type">A <see cref="System.Type"/> to set the <see cref="Type"/> property equal to.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expression"/> or <paramref name="type"/> is null.</exception>
        /// <exception cref="InvalidOperationException">No conversion operator is defined between <paramref name="expression"/>.Type and <paramref name="type"/>.</exception>
        public static UnaryExpression Convert(Expression expression, Type type)
        {
            return Convert(expression, type, method: null);
        }

        /// <summary>Creates a <see cref="UnaryExpression"/> that represents a conversion operation for which the implementing method is specified.</summary>
        /// <returns>A <see cref="UnaryExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Convert"/> and the <see cref="UnaryExpression.Operand"/>, <see cref="Expression.Type"/>, and <see cref="UnaryExpression.Method"/> properties set to the specified values.</returns>
        /// <param name="expression">An <see cref="Expression"/> to set the <see cref="UnaryExpression.Operand"/> property equal to.</param>
        /// <param name="type">A <see cref="System.Type"/> to set the <see cref="Type"/> property equal to.</param>
        /// <param name="method">A <see cref="MethodInfo"/> to set the <see cref="UnaryExpression.Method"/> property equal to.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expression"/> or <paramref name="type"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="method"/> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly one argument.</exception>
        /// <exception cref="AmbiguousMatchException">More than one method that matches the <paramref name="method"/> description was found.</exception>
        /// <exception cref="InvalidOperationException">No conversion operator is defined between <paramref name="expression"/>.Type and <paramref name="type"/>.-or-<paramref name="expression"/>.Type is not assignable to the argument type of the method represented by <paramref name="method"/>.-or-The return type of the method represented by <paramref name="method"/> is not assignable to <paramref name="type"/>.-or-<paramref name="expression"/>.Type or <paramref name="type"/> is a nullable value type and the corresponding non-nullable value type does not equal the argument type or the return type, respectively, of the method represented by <paramref name="method"/>.</exception>
        public static UnaryExpression Convert(Expression expression, Type type, MethodInfo method)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            ContractUtils.RequiresNotNull(type, nameof(type));
            TypeUtils.ValidateType(type, nameof(type));
            if (method == null)
            {
                if (expression.Type.HasIdentityPrimitiveOrNullableConversionTo(type) ||
                    expression.Type.HasReferenceConversionTo(type))
                {
                    return new UnaryExpression(ExpressionType.Convert, expression, type, null);
                }
                return GetUserDefinedCoercionOrThrow(ExpressionType.Convert, expression, type);
            }
            return GetMethodBasedCoercionOperator(ExpressionType.Convert, expression, type, method);
        }

        /// <summary>Creates a <see cref="UnaryExpression"/> that represents a conversion operation that throws an exception if the target type is overflowed.</summary>
        /// <returns>A <see cref="UnaryExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.ConvertChecked"/> and the <see cref="UnaryExpression.Operand"/> and <see cref="Expression.Type"/> properties set to the specified values.</returns>
        /// <param name="expression">An <see cref="Expression"/> to set the <see cref="UnaryExpression.Operand"/> property equal to.</param>
        /// <param name="type">A <see cref="System.Type"/> to set the <see cref="Type"/> property equal to.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expression"/> or <paramref name="type"/> is null.</exception>
        /// <exception cref="InvalidOperationException">No conversion operator is defined between <paramref name="expression"/>.Type and <paramref name="type"/>.</exception>
        public static UnaryExpression ConvertChecked(Expression expression, Type type)
        {
            return ConvertChecked(expression, type, method: null);
        }

        /// <summary>Creates a <see cref="UnaryExpression"/> that represents a conversion operation that throws an exception if the target type is overflowed and for which the implementing method is specified.</summary>
        /// <returns>A <see cref="UnaryExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.ConvertChecked"/> and the <see cref="UnaryExpression.Operand"/>, <see cref="Expression.Type"/>, and <see cref="UnaryExpression.Method"/> properties set to the specified values.</returns>
        /// <param name="expression">An <see cref="Expression"/> to set the <see cref="UnaryExpression.Operand"/> property equal to.</param>
        /// <param name="type">A <see cref="System.Type"/> to set the <see cref="Type"/> property equal to.</param>
        /// <param name="method">A <see cref="MethodInfo"/> to set the <see cref="UnaryExpression.Method"/> property equal to.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expression"/> or <paramref name="type"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="method"/> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly one argument.</exception>
        /// <exception cref="AmbiguousMatchException">More than one method that matches the <paramref name="method"/> description was found.</exception>
        /// <exception cref="InvalidOperationException">No conversion operator is defined between <paramref name="expression"/>.Type and <paramref name="type"/>.-or-<paramref name="expression"/>.Type is not assignable to the argument type of the method represented by <paramref name="method"/>.-or-The return type of the method represented by <paramref name="method"/> is not assignable to <paramref name="type"/>.-or-<paramref name="expression"/>.Type or <paramref name="type"/> is a nullable value type and the corresponding non-nullable value type does not equal the argument type or the return type, respectively, of the method represented by <paramref name="method"/>.</exception>
        public static UnaryExpression ConvertChecked(Expression expression, Type type, MethodInfo method)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            ContractUtils.RequiresNotNull(type, nameof(type));
            TypeUtils.ValidateType(type, nameof(type));
            if (method == null)
            {
                if (expression.Type.HasIdentityPrimitiveOrNullableConversionTo(type))
                {
                    return new UnaryExpression(ExpressionType.ConvertChecked, expression, type, null);
                }
                if (expression.Type.HasReferenceConversionTo(type))
                {
                    return new UnaryExpression(ExpressionType.Convert, expression, type, null);
                }
                return GetUserDefinedCoercionOrThrow(ExpressionType.ConvertChecked, expression, type);
            }
            return GetMethodBasedCoercionOperator(ExpressionType.ConvertChecked, expression, type, method);
        }

        /// <summary>Creates a <see cref="UnaryExpression"/> that represents getting the length of a one-dimensional, zero-based array.</summary>
        /// <returns>A <see cref="UnaryExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.ArrayLength"/> and the <see cref="UnaryExpression.Operand"/> property equal to <paramref name="array"/>.</returns>
        /// <param name="array">An <see cref="Expression"/> to set the <see cref="UnaryExpression.Operand"/> property equal to.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="array"/>.Type does not represent a single-dimensional, zero-based array type.</exception>
        public static UnaryExpression ArrayLength(Expression array)
        {
            ExpressionUtils.RequiresCanRead(array, nameof(array));
            if (!array.Type.IsSZArray)
            {
                if (!array.Type.IsArray || !typeof(Array).IsAssignableFrom(array.Type))
                {
                    throw Error.ArgumentMustBeArray(nameof(array));
                }

                throw Error.ArgumentMustBeSingleDimensionalArrayType(nameof(array));
            }

            return new UnaryExpression(ExpressionType.ArrayLength, array, typeof(int), null);
        }

        /// <summary>Creates a <see cref="UnaryExpression"/> that represents an expression that has a constant value of type <see cref="Expression"/>.</summary>
        /// <returns>A <see cref="UnaryExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.Quote"/> and the <see cref="UnaryExpression.Operand"/> property set to the specified value.</returns>
        /// <param name="expression">An <see cref="Expression"/> to set the <see cref="UnaryExpression.Operand"/> property equal to.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expression"/> is null.</exception>
        public static UnaryExpression Quote(Expression expression)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            LambdaExpression lambda = expression as LambdaExpression;
            if (lambda == null)
            {
                throw Error.QuotedExpressionMustBeLambda(nameof(expression));
            }

            return new UnaryExpression(ExpressionType.Quote, lambda, lambda.PublicType, null);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents a rethrowing of an exception.
        /// </summary>
        /// <returns>A <see cref="UnaryExpression"/> that represents a rethrowing of an exception.</returns>
        public static UnaryExpression Rethrow()
        {
            return Throw(value: null);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents a rethrowing of an exception with a given type.
        /// </summary>
        /// <param name="type">The new <see cref="Type"/> of the expression.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents a rethrowing of an exception.</returns>
        public static UnaryExpression Rethrow(Type type)
        {
            return Throw(null, type);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents a throwing of an exception.
        /// </summary>
        /// <param name="value">An <see cref="Expression"/>.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents the exception.</returns>
        public static UnaryExpression Throw(Expression value)
        {
            return Throw(value, typeof(void));
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents a throwing of a value with a given type.
        /// </summary>
        /// <param name="value">An <see cref="Expression"/>.</param>
        /// <param name="type">The new <see cref="Type"/> of the expression.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents the exception.</returns>
        public static UnaryExpression Throw(Expression value, Type type)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            TypeUtils.ValidateType(type, nameof(type));
            if (value != null)
            {
                ExpressionUtils.RequiresCanRead(value, nameof(value));
                if (value.Type.IsValueType) throw Error.ArgumentMustNotHaveValueType(nameof(value));
            }
            return new UnaryExpression(ExpressionType.Throw, value, type, null);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents the incrementing of the expression by 1.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to increment.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents the incremented expression.</returns>
        public static UnaryExpression Increment(Expression expression)
        {
            return Increment(expression, method: null);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents the incrementing of the expression by 1.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to increment.</param>
        /// <param name="method">A <see cref="MethodInfo"/> that represents the implementing method.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents the incremented expression.</returns>
        public static UnaryExpression Increment(Expression expression, MethodInfo method)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            if (method == null)
            {
                if (expression.Type.IsArithmetic())
                {
                    return new UnaryExpression(ExpressionType.Increment, expression, expression.Type, null);
                }
                return GetUserDefinedUnaryOperatorOrThrow(ExpressionType.Increment, "op_Increment", expression);
            }
            return GetMethodBasedUnaryOperator(ExpressionType.Increment, expression, method);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents the decrementing of the expression by 1.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to decrement.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents the decremented expression.</returns>
        public static UnaryExpression Decrement(Expression expression)
        {
            return Decrement(expression, method: null);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents the decrementing of the expression by 1.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to decrement.</param>
        /// <param name="method">A <see cref="MethodInfo"/> that represents the implementing method.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents the decremented expression.</returns>
        public static UnaryExpression Decrement(Expression expression, MethodInfo method)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            if (method == null)
            {
                if (expression.Type.IsArithmetic())
                {
                    return new UnaryExpression(ExpressionType.Decrement, expression, expression.Type, null);
                }
                return GetUserDefinedUnaryOperatorOrThrow(ExpressionType.Decrement, "op_Decrement", expression);
            }
            return GetMethodBasedUnaryOperator(ExpressionType.Decrement, expression, method);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that increments the expression by 1
        /// and assigns the result back to the expression.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to apply the operations on.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents the resultant expression.</returns>
        public static UnaryExpression PreIncrementAssign(Expression expression)
        {
            return MakeOpAssignUnary(ExpressionType.PreIncrementAssign, expression, method: null);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that increments the expression by 1
        /// and assigns the result back to the expression.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to apply the operations on.</param>
        /// <param name="method">A <see cref="MethodInfo"/> that represents the implementing method.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents the resultant expression.</returns>
        public static UnaryExpression PreIncrementAssign(Expression expression, MethodInfo method)
        {
            return MakeOpAssignUnary(ExpressionType.PreIncrementAssign, expression, method);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that decrements the expression by 1
        /// and assigns the result back to the expression.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to apply the operations on.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents the resultant expression.</returns>
        public static UnaryExpression PreDecrementAssign(Expression expression)
        {
            return MakeOpAssignUnary(ExpressionType.PreDecrementAssign, expression, method: null);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that decrements the expression by 1
        /// and assigns the result back to the expression.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to apply the operations on.</param>
        /// <param name="method">A <see cref="MethodInfo"/> that represents the implementing method.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents the resultant expression.</returns>
        public static UnaryExpression PreDecrementAssign(Expression expression, MethodInfo method)
        {
            return MakeOpAssignUnary(ExpressionType.PreDecrementAssign, expression, method);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents the assignment of the expression
        /// followed by a subsequent increment by 1 of the original expression.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to apply the operations on.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents the resultant expression.</returns>
        public static UnaryExpression PostIncrementAssign(Expression expression)
        {
            return MakeOpAssignUnary(ExpressionType.PostIncrementAssign, expression, method: null);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents the assignment of the expression
        /// followed by a subsequent increment by 1 of the original expression.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to apply the operations on.</param>
        /// <param name="method">A <see cref="MethodInfo"/> that represents the implementing method.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents the resultant expression.</returns>
        public static UnaryExpression PostIncrementAssign(Expression expression, MethodInfo method)
        {
            return MakeOpAssignUnary(ExpressionType.PostIncrementAssign, expression, method);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents the assignment of the expression
        /// followed by a subsequent decrement by 1 of the original expression.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to apply the operations on.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents the resultant expression.</returns>
        public static UnaryExpression PostDecrementAssign(Expression expression)
        {
            return MakeOpAssignUnary(ExpressionType.PostDecrementAssign, expression, method: null);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents the assignment of the expression
        /// followed by a subsequent decrement by 1 of the original expression.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to apply the operations on.</param>
        /// <param name="method">A <see cref="MethodInfo"/> that represents the implementing method.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents the resultant expression.</returns>
        public static UnaryExpression PostDecrementAssign(Expression expression, MethodInfo method)
        {
            return MakeOpAssignUnary(ExpressionType.PostDecrementAssign, expression, method);
        }

        private static UnaryExpression MakeOpAssignUnary(ExpressionType kind, Expression expression, MethodInfo method)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            RequiresCanWrite(expression, nameof(expression));

            UnaryExpression result;
            if (method == null)
            {
                if (expression.Type.IsArithmetic())
                {
                    return new UnaryExpression(kind, expression, expression.Type, null);
                }
                string name;
                if (kind == ExpressionType.PreIncrementAssign || kind == ExpressionType.PostIncrementAssign)
                {
                    name = "op_Increment";
                }
                else
                {
                    Debug.Assert(kind == ExpressionType.PreDecrementAssign || kind == ExpressionType.PostDecrementAssign);
                    name = "op_Decrement";
                }
                result = GetUserDefinedUnaryOperatorOrThrow(kind, name, expression);
            }
            else
            {
                result = GetMethodBasedUnaryOperator(kind, expression, method);
            }
            // return type must be assignable back to the operand type
            if (!TypeUtils.AreReferenceAssignable(expression.Type, result.Type))
            {
                throw Error.UserDefinedOpMustHaveValidReturnType(kind, method.Name);
            }
            return result;
        }
    }
}
