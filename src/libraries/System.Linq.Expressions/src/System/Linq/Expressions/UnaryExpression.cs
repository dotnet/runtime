// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    /// <summary>Represents an expression that has a unary operator.</summary>
    /// <remarks>The following table summarizes the factory methods that can be used to create a <see cref="System.Linq.Expressions.UnaryExpression" /> that has a specific node type.
    /// |<see cref="O:System.Linq.Expressions.Expression.NodeType" />|Factory Method|
    /// |----------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------|
    /// |<see cref="System.Linq.Expressions.ExpressionType.ArrayLength" />|<see cref="O:System.Linq.Expressions.Expression.ArrayLength" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.Convert" />|<see cref="O:System.Linq.Expressions.Expression.Convert" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.ConvertChecked" />|<see cref="O:System.Linq.Expressions.Expression.ConvertChecked" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.Negate" />|<see cref="O:System.Linq.Expressions.Expression.Negate" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.NegateChecked" />|<see cref="O:System.Linq.Expressions.Expression.NegateChecked" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.Not" />|<see cref="O:System.Linq.Expressions.Expression.Not" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.Quote" />|<see cref="O:System.Linq.Expressions.Expression.Quote" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.TypeAs" />|<see cref="O:System.Linq.Expressions.Expression.TypeAs" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.UnaryPlus" />|<see cref="O:System.Linq.Expressions.Expression.UnaryPlus" />|
    /// In addition, the <see cref="O:System.Linq.Expressions.Expression.MakeUnary" /> methods can also be used to create a <see cref="System.Linq.Expressions.UnaryExpression" />. These factory methods can be used to create a <see cref="System.Linq.Expressions.UnaryExpression" /> of any node type that represents a unary operation. The parameter of these methods that is of type <see cref="O:System.Linq.Expressions.Expression.NodeType" /> specifies the desired node type.</remarks>
    /// <example>The following example creates a <see cref="System.Linq.Expressions.UnaryExpression" /> object that represents the reference conversion of a non-nullable integer expression to the nullable integer type.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet11":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet11":::</example>
    [DebuggerTypeProxy(typeof(UnaryExpressionProxy))]
    public sealed class UnaryExpression : Expression
    {
        internal UnaryExpression(ExpressionType nodeType, Expression expression, Type type, MethodInfo? method)
        {
            Operand = expression;
            Method = method;
            NodeType = nodeType;
            Type = type;
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.UnaryExpression.Type" /> that represents the static type of the expression.</value>
        public sealed override Type Type { get; }

        /// <summary>Returns the node type of this <see cref="System.Linq.Expressions.Expression" />.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> that represents this expression.</value>
        public sealed override ExpressionType NodeType { get; }

        /// <summary>Gets the operand of the unary operation.</summary>
        /// <value>An <see cref="System.Linq.Expressions.Expression" /> that represents the operand of the unary operation.</value>
        public Expression Operand { get; }

        /// <summary>Gets the implementing method for the unary operation.</summary>
        /// <value>The <see cref="System.Reflection.MethodInfo" /> that represents the implementing method.</value>
        public MethodInfo? Method { get; }

        /// <summary>Gets a value that indicates whether the expression tree node represents a lifted call to an operator.</summary>
        /// <value><see langword="true" /> if the node represents a lifted call; otherwise, <see langword="false" />.</value>
        /// <remarks>An operator call is *lifted* if the operator expects a non-nullable operand but a nullable operand is passed to it.</remarks>
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

        /// <summary>Gets a value that indicates whether the expression tree node represents a lifted call to an operator whose return type is lifted to a nullable type.</summary>
        /// <value><see langword="true" /> if the operator's return type is lifted to a nullable type; otherwise, <see langword="false" />.</value>
        /// <remarks>An operator call is *lifted* if the operator expects a non-nullable operand but a nullable operand is passed to it. If the value of <see cref="O:System.Linq.Expressions.UnaryExpression.IsLiftedToNull" /> is <see langword="true" />, the operator returns a nullable type and if the nullable operand evaluates to <see langword="null" />, the operator returns <see langword="null" />.</remarks>
        public bool IsLiftedToNull => IsLifted && this.Type.IsNullableType();

        /// <summary>Dispatches to the specific visit method for this node type.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitUnary(this);
        }

        /// <summary>Gets a value that indicates whether the expression tree node can be reduced.</summary>
        /// <value><see langword="true" /> if a node can be reduced; otherwise, <see langword="false" />.</value>
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

        /// <summary>Reduces the expression node to a simpler expression.</summary>
        /// <returns>The reduced expression.</returns>
        /// <remarks>If the `CanReduce` method returns true, this should return a valid expression.
        /// This method can return another node which itself must be reduced.</remarks>
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
            temps[i] = Parameter(index.Object!.Type, name: null);
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

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="operand">The <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "A UnaryExpression has already been created. The original creator will get a warning that it is not trim compatible.")]
        public UnaryExpression Update(Expression operand)
        {
            if (operand == Operand)
            {
                return this;
            }
            return Expression.MakeUnary(NodeType, operand, Type, Method);
        }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" />, given an operand, by calling the appropriate factory method.</summary>
        /// <param name="unaryType">The <see cref="System.Linq.Expressions.ExpressionType" /> that specifies the type of unary operation.</param>
        /// <param name="operand">An <see cref="System.Linq.Expressions.Expression" /> that represents the operand.</param>
        /// <param name="type">The <see cref="System.Type" /> that specifies the type to be converted to (pass <see langword="null" /> if not applicable).</param>
        /// <returns>The <see cref="System.Linq.Expressions.UnaryExpression" /> that results from calling the appropriate factory method.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="operand" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="unaryType" /> does not correspond to a unary expression node.</exception>
        /// <remarks>The <paramref name="unaryType" /> parameter determines which <see cref="System.Linq.Expressions.UnaryExpression" /> factory method this method calls. For example, if <paramref name="unaryType" /> is equal to <see cref="System.Linq.Expressions.ExpressionType.Convert" />, this method invokes <see cref="O:System.Linq.Expressions.Expression.Convert" />. The <paramref name="type" />parameter is ignored if it does not apply to the factory method that is called.</remarks>
        [RequiresUnreferencedCode(ExpressionRequiresUnreferencedCode)]
        public static UnaryExpression MakeUnary(ExpressionType unaryType, Expression operand, Type type)
        {
            return MakeUnary(unaryType, operand, type, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" />, given an operand and implementing method, by calling the appropriate factory method.</summary>
        /// <param name="unaryType">The <see cref="System.Linq.Expressions.ExpressionType" /> that specifies the type of unary operation.</param>
        /// <param name="operand">An <see cref="System.Linq.Expressions.Expression" /> that represents the operand.</param>
        /// <param name="type">The <see cref="System.Type" /> that specifies the type to be converted to (pass <see langword="null" /> if not applicable).</param>
        /// <param name="method">The <see cref="System.Reflection.MethodInfo" /> that represents the implementing method.</param>
        /// <returns>The <see cref="System.Linq.Expressions.UnaryExpression" /> that results from calling the appropriate factory method.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="operand" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="unaryType" /> does not correspond to a unary expression node.</exception>
        /// <remarks>The <paramref name="unaryType" /> parameter determines which <see cref="System.Linq.Expressions.UnaryExpression" /> factory method this method calls. For example, if <paramref name="unaryType" /> is equal to <see cref="System.Linq.Expressions.ExpressionType.Convert" />, this method invokes <see cref="O:System.Linq.Expressions.Expression.Convert" />. The <paramref name="type" /> and <paramref name="method" /> parameters are ignored if they do not apply to the factory method that is called.</remarks>
        [RequiresUnreferencedCode(ExpressionRequiresUnreferencedCode)]
        public static UnaryExpression MakeUnary(ExpressionType unaryType, Expression operand, Type type, MethodInfo? method) =>
            unaryType switch
            {
                ExpressionType.Negate => Negate(operand, method),
                ExpressionType.NegateChecked => NegateChecked(operand, method),
                ExpressionType.Not => Not(operand, method),
                ExpressionType.IsFalse => IsFalse(operand, method),
                ExpressionType.IsTrue => IsTrue(operand, method),
                ExpressionType.OnesComplement => OnesComplement(operand, method),
                ExpressionType.ArrayLength => ArrayLength(operand),
                ExpressionType.Convert => Convert(operand, type, method),
                ExpressionType.ConvertChecked => ConvertChecked(operand, type, method),
                ExpressionType.Throw => Throw(operand, type),
                ExpressionType.TypeAs => TypeAs(operand, type),
                ExpressionType.Quote => Quote(operand),
                ExpressionType.UnaryPlus => UnaryPlus(operand, method),
                ExpressionType.Unbox => Unbox(operand, type),
                ExpressionType.Increment => Increment(operand, method),
                ExpressionType.Decrement => Decrement(operand, method),
                ExpressionType.PreIncrementAssign => PreIncrementAssign(operand, method),
                ExpressionType.PostIncrementAssign => PostIncrementAssign(operand, method),
                ExpressionType.PreDecrementAssign => PreDecrementAssign(operand, method),
                ExpressionType.PostDecrementAssign => PostDecrementAssign(operand, method),
                _ => throw Error.UnhandledUnary(unaryType, nameof(unaryType)),
            };

        private static UnaryExpression GetUserDefinedUnaryOperatorOrThrow(ExpressionType unaryType, string name, Expression operand)
        {
            UnaryExpression? u = GetUserDefinedUnaryOperator(unaryType, name, operand);
            if (u != null)
            {
                ValidateParamswithOperandsOrThrow(u.Method!.GetParametersCached()[0].ParameterType, operand.Type, unaryType, name);
                return u;
            }
            throw Error.UnaryOperatorNotDefined(unaryType, operand.Type);
        }

        private static UnaryExpression? GetUserDefinedUnaryOperator(ExpressionType unaryType, string name, Expression operand)
        {
            Type operandType = operand.Type;
            Type[] types = new Type[] { operandType };
            Type nnOperandType = operandType.GetNonNullableType();
            MethodInfo? method = nnOperandType.GetAnyStaticMethodValidated(name, types);
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

        [RequiresUnreferencedCode(Expression.ExpressionRequiresUnreferencedCode)]
        private static UnaryExpression GetUserDefinedCoercionOrThrow(ExpressionType coercionType, Expression expression, Type convertToType)
        {
            UnaryExpression? u = GetUserDefinedCoercion(coercionType, expression, convertToType);
            if (u != null)
            {
                return u;
            }
            throw Error.CoercionOperatorNotDefined(expression.Type, convertToType);
        }

        [RequiresUnreferencedCode(Expression.ExpressionRequiresUnreferencedCode)]
        private static UnaryExpression? GetUserDefinedCoercion(ExpressionType coercionType, Expression expression, Type convertToType)
        {
            MethodInfo? method = TypeUtils.GetUserDefinedCoercionMethod(expression.Type, convertToType);
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents an arithmetic negation operation.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Negate" /> and the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property set to the specified value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The unary minus operator is not defined for <paramref name="expression" />.Type.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.UnaryExpression.Method" /> property of the resulting <see cref="System.Linq.Expressions.UnaryExpression" /> is set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are false.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="expression" />.Type is a user-defined type that defines the unary minus operator, the <see cref="System.Reflection.MethodInfo" /> that represents that operator is the implementing method.
        /// -   Otherwise, if <paramref name="expression" />.Type is a numeric type, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="expression" />.Type is assignable to the argument type of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="expression" />.Type is a nullable value type and the corresponding non-nullable value type is equal to the argument type of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />, the type of the node is <paramref name="expression" />.Type. If <paramref name="expression" />.Type is non-nullable, the node is not lifted. Otherwise, the node is lifted.</remarks>
        /// <example>The following example demonstrates how to create an expression that represents an arithmetic negation operation.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet50":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet50":::</example>
        public static UnaryExpression Negate(Expression expression)
        {
            return Negate(expression, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents an arithmetic negation operation.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Negate" /> and the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> and <see cref="System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly one argument.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the unary minus operator is not defined for <paramref name="expression" />.Type.
        /// -or-
        /// <paramref name="expression" />.Type (or its corresponding non-nullable type if it is a nullable value type) is not assignable to the argument type of the method represented by <paramref name="method" />.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.UnaryExpression.Method" /> property of the resulting <see cref="System.Linq.Expressions.UnaryExpression" /> is set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are false.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes one argument, it is the implementing method for the node.
        /// -   If <paramref name="expression" />.Type is a user-defined type that defines the unary minus operator, the <see cref="System.Reflection.MethodInfo" /> that represents that operator is the implementing method.
        /// -   Otherwise, if <paramref name="expression" />.Type is a numeric type, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="expression" />.Type is assignable to the argument type of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="expression" />.Type is a nullable value type and the corresponding non-nullable value type is equal to the argument type of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />, the type of the node is <paramref name="expression" />.Type. If <paramref name="expression" />.Type is non-nullable, the node is not lifted. Otherwise, the node is lifted.</remarks>
        public static UnaryExpression Negate(Expression expression, MethodInfo? method)
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents a unary plus operation.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.UnaryPlus" /> and the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property set to the specified value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The unary plus operator is not defined for <paramref name="expression" />.Type.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.UnaryExpression.Method" /> property of the resulting <see cref="System.Linq.Expressions.UnaryExpression" /> is set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are false.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="expression" />.Type is a user-defined type that defines the unary plus operator, the <see cref="System.Reflection.MethodInfo" /> that represents that operator is the implementing method.
        /// -   Otherwise, if <paramref name="expression" />.Type is a numeric type, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="expression" />.Type is assignable to the argument type of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="expression" />.Type is a nullable value type and the corresponding non-nullable value type is equal to the argument type of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />, the type of the node is <paramref name="expression" />.Type. If <paramref name="expression" />.Type is non-nullable, the node is not lifted. Otherwise, the node is lifted.</remarks>
        public static UnaryExpression UnaryPlus(Expression expression)
        {
            return UnaryPlus(expression, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents a unary plus operation.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.UnaryPlus" /> and the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> and <see cref="System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly one argument.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the unary plus operator is not defined for <paramref name="expression" />.Type.
        /// -or-
        /// <paramref name="expression" />.Type (or its corresponding non-nullable type if it is a nullable value type) is not assignable to the argument type of the method represented by <paramref name="method" />.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.UnaryExpression.Method" /> property of the resulting <see cref="System.Linq.Expressions.UnaryExpression" /> is set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are false.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes one argument, it is the implementing method for the node.
        /// -   If <paramref name="expression" />.Type is a user-defined type that defines the unary plus operator, the <see cref="System.Reflection.MethodInfo" /> that represents that operator is the implementing method.
        /// -   Otherwise, if <paramref name="expression" />.Type is a numeric type, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="expression" />.Type is assignable to the argument type of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="expression" />.Type is a nullable value type and the corresponding non-nullable value type is equal to the argument type of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />, the type of the node is <paramref name="expression" />.Type. If <paramref name="expression" />.Type is non-nullable, the node is not lifted. Otherwise, the node is lifted.</remarks>
        public static UnaryExpression UnaryPlus(Expression expression, MethodInfo? method)
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents an arithmetic negation operation that has overflow checking.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.NegateChecked" /> and the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property set to the specified value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The unary minus operator is not defined for <paramref name="expression" />.Type.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.UnaryExpression.Method" /> property of the resulting <see cref="System.Linq.Expressions.UnaryExpression" /> is set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are false.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="expression" />.Type is a user-defined type that defines the unary minus operator, the <see cref="System.Reflection.MethodInfo" /> that represents that operator is the implementing method.
        /// -   Otherwise, if <paramref name="expression" />.Type is a numeric type, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="expression" />.Type is assignable to the argument type of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="expression" />.Type is a nullable value type and the corresponding non-nullable value type is equal to the argument type of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />, the type of the node is <paramref name="expression" />.Type. If <paramref name="expression" />.Type is non-nullable, the node is not lifted. Otherwise, the node is lifted.</remarks>
        public static UnaryExpression NegateChecked(Expression expression)
        {
            return NegateChecked(expression, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents an arithmetic negation operation that has overflow checking. The implementing method can be specified.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.NegateChecked" /> and the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> and <see cref="System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly one argument.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the unary minus operator is not defined for <paramref name="expression" />.Type.
        /// -or-
        /// <paramref name="expression" />.Type (or its corresponding non-nullable type if it is a nullable value type) is not assignable to the argument type of the method represented by <paramref name="method" />.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.UnaryExpression.Method" /> property of the resulting <see cref="System.Linq.Expressions.UnaryExpression" /> is set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are false.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes one argument, it is the implementing method for the node.
        /// -   If <paramref name="expression" />.Type is a user-defined type that defines the unary minus operator, the <see cref="System.Reflection.MethodInfo" /> that represents that operator is the implementing method.
        /// -   Otherwise, if <paramref name="expression" />.Type is a numeric type, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="expression" />.Type is assignable to the argument type of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="expression" />.Type is a nullable value type and the corresponding non-nullable value type is equal to the argument type of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />, the type of the node is <paramref name="expression" />.Type. If <paramref name="expression" />.Type is non-nullable, the node is not lifted. Otherwise, the node is lifted.</remarks>
        public static UnaryExpression NegateChecked(Expression expression, MethodInfo? method)
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents a bitwise complement operation.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Not" /> and the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property set to the specified value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The unary not operator is not defined for <paramref name="expression" />.Type.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.UnaryExpression.Method" /> property of the resulting <see cref="System.Linq.Expressions.UnaryExpression" /> is set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="expression" />.Type is a user-defined type that defines the unary not operator, the <see cref="System.Reflection.MethodInfo" /> that represents that operator is the implementing method.
        /// -   Otherwise, if <paramref name="expression" />.Type is a numeric or Boolean type, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="expression" />.Type is assignable to the argument type of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="expression" />.Type is a nullable value type and the corresponding non-nullable type is equal to the argument type of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />, the type of the node is <paramref name="expression" />.Type. If <paramref name="expression" />.Type is non-nullable, the node is not lifted. Otherwise, the node is lifted.</remarks>
        /// <example>The following example demonstrates how to create an expression that represents a logical NOT operation.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet51":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet51":::</example>
        public static UnaryExpression Not(Expression expression)
        {
            return Not(expression, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents a bitwise complement operation. The implementing method can be specified.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Not" /> and the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> and <see cref="System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly one argument.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the unary not operator is not defined for <paramref name="expression" />.Type.
        /// -or-
        /// <paramref name="expression" />.Type (or its corresponding non-nullable type if it is a nullable value type) is not assignable to the argument type of the method represented by <paramref name="method" />.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.UnaryExpression.Method" /> property of the resulting <see cref="System.Linq.Expressions.UnaryExpression" /> is set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes one argument, it is the implementing method for the node.
        /// -   If <paramref name="expression" />.Type is a user-defined type that defines the unary not operator, the <see cref="System.Reflection.MethodInfo" /> that represents that operator is the implementing method.
        /// -   Otherwise, if <paramref name="expression" />.Type is a numeric type, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="expression" />.Type is assignable to the argument type of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="expression" />.Type is a nullable value type and the corresponding non-nullable value type is equal to the argument type of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />, the type of the node is <paramref name="expression" />.Type. If <paramref name="expression" />.Type is non-nullable, the node is not lifted. Otherwise, the node is lifted.</remarks>
        public static UnaryExpression Not(Expression expression, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            if (method == null)
            {
                if (expression.Type.IsIntegerOrBool())
                {
                    return new UnaryExpression(ExpressionType.Not, expression, expression.Type, null);
                }
                UnaryExpression? u = GetUserDefinedUnaryOperator(ExpressionType.Not, "op_LogicalNot", expression);
                if (u != null)
                {
                    return u;
                }
                return GetUserDefinedUnaryOperatorOrThrow(ExpressionType.Not, "op_OnesComplement", expression);
            }
            return GetMethodBasedUnaryOperator(ExpressionType.Not, expression, method);
        }

        /// <summary>Returns whether the expression evaluates to false.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to evaluate.</param>
        /// <returns>An instance of <see cref="System.Linq.Expressions.UnaryExpression" />.</returns>
        public static UnaryExpression IsFalse(Expression expression)
        {
            return IsFalse(expression, method: null);
        }

        /// <summary>Returns whether the expression evaluates to false.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to evaluate.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> that represents the implementing method.</param>
        /// <returns>An instance of <see cref="System.Linq.Expressions.UnaryExpression" />.</returns>
        public static UnaryExpression IsFalse(Expression expression, MethodInfo? method)
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

        /// <summary>Returns whether the expression evaluates to true.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to evaluate.</param>
        /// <returns>An instance of <see cref="System.Linq.Expressions.UnaryExpression" />.</returns>
        public static UnaryExpression IsTrue(Expression expression)
        {
            return IsTrue(expression, method: null);
        }

        /// <summary>Returns whether the expression evaluates to true.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to evaluate.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> that represents the implementing method.</param>
        /// <returns>An instance of <see cref="System.Linq.Expressions.UnaryExpression" />.</returns>
        public static UnaryExpression IsTrue(Expression expression, MethodInfo? method)
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

        /// <summary>Returns the expression representing the ones complement.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" />.</param>
        /// <returns>An instance of <see cref="System.Linq.Expressions.UnaryExpression" />.</returns>
        public static UnaryExpression OnesComplement(Expression expression)
        {
            return OnesComplement(expression, method: null);
        }

        /// <summary>Returns the expression representing the ones complement.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" />.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> that represents the implementing method.</param>
        /// <returns>An instance of <see cref="System.Linq.Expressions.UnaryExpression" />.</returns>
        public static UnaryExpression OnesComplement(Expression expression, MethodInfo? method)
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents an explicit reference or boxing conversion where <see langword="null" /> is supplied if the conversion fails.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
        /// <param name="type">A <see cref="System.Type" /> to set the <see cref="System.Linq.Expressions.Expression.Type" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.TypeAs" /> and the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> and <see cref="System.Linq.Expressions.Expression.Type" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> or <paramref name="type" /> is <see langword="null" />.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.UnaryExpression.Method" /> property of the resulting <see cref="System.Linq.Expressions.UnaryExpression" /> is <see langword="null" />. The <see cref="O:System.Linq.Expressions.UnaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.UnaryExpression.IsLiftedToNull" /> properties are both <see langword="false" />.</remarks>
        /// <example>The following example demonstrates how to use the <see cref="System.Linq.Expressions.Expression.TypeAs(System.Linq.Expressions.Expression,System.Type)" /> method to create a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the reference conversion of a non-nullable integer expression to the nullable integer type.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet11":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet11":::</example>
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents an explicit unboxing.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to unbox.</param>
        /// <param name="type">The new <see cref="System.Type" /> of the expression.</param>
        /// <returns>An instance of <see cref="System.Linq.Expressions.UnaryExpression" />.</returns>
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents a type conversion operation.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
        /// <param name="type">A <see cref="System.Type" /> to set the <see cref="System.Linq.Expressions.Expression.Type" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Convert" /> and the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> and <see cref="System.Linq.Expressions.Expression.Type" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> or <paramref name="type" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">No conversion operator is defined between <paramref name="expression" />.Type and <paramref name="type" />.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.UnaryExpression.Method" /> property of the resulting <see cref="System.Linq.Expressions.UnaryExpression" /> is set to the implementing method. The <see cref="O:System.Linq.Expressions.UnaryExpression.IsLiftedToNull" /> property is <see langword="false" />. If the node is lifted, <see cref="O:System.Linq.Expressions.UnaryExpression.IsLifted" /> is <see langword="true" />. Otherwise, it is <see langword="false" />.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If either <paramref name="expression" />.Type or <paramref name="type" /> is a user-defined type that defines an implicit or explicit conversion operator, the <see cref="System.Reflection.MethodInfo" /> that represents that operator is the implementing method.
        /// -   Otherwise:
        /// -   If both <paramref name="expression" />.Type and <paramref name="type" /> represent numeric or Boolean types, or nullable or non-nullable enumeration types, the implementing method is <see langword="null" />.
        /// -   If either <paramref name="expression" />.Type or <paramref name="type" /> is a reference type and an explicit boxing, unboxing, or reference conversion exists from <paramref name="expression" />.Type to <paramref name="type" />, the implementing method is <see langword="null" />.
        /// #### Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="expression" />.Type is assignable to the argument type of the implementing method and the return type of the implementing method is assignable to <paramref name="type" />, the node is not lifted.
        /// -   If one or both of <paramref name="expression" />.Type or <paramref name="type" /> is a nullable value type and the corresponding non-nullable value types are equal to the argument type and the return type of the implementing method respectively, the node is lifted.
        /// If the implementing method is <see langword="null" />:
        /// -   If both <paramref name="expression" />.Type and <paramref name="type" /> are non-nullable, the node is not lifted.
        /// -   Otherwise the node is lifted.</remarks>
        /// <example>The following code example shows how to create an expression that represents a type conversion operation.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet23":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet23":::</example>
        public static UnaryExpression Convert(Expression expression, Type type)
        {
            return Convert(expression, type, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents a conversion operation for which the implementing method is specified.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
        /// <param name="type">A <see cref="System.Type" /> to set the <see cref="System.Linq.Expressions.Expression.Type" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Convert" /> and the <see cref="System.Linq.Expressions.UnaryExpression.Operand" />, <see cref="System.Linq.Expressions.Expression.Type" />, and <see cref="System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> or <paramref name="type" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly one argument.</exception>
        /// <exception cref="System.InvalidOperationException">No conversion operator is defined between <paramref name="expression" />.Type and <paramref name="type" />.
        /// -or-
        /// <paramref name="expression" />.Type is not assignable to the argument type of the method represented by <paramref name="method" />.
        /// -or-
        /// The return type of the method represented by <paramref name="method" /> is not assignable to <paramref name="type" />.
        /// -or-
        /// <paramref name="expression" />.Type or <paramref name="type" /> is a nullable value type and the corresponding non-nullable value type does not equal the argument type or the return type, respectively, of the method represented by <paramref name="method" />.</exception>
        /// <exception cref="System.Reflection.AmbiguousMatchException">More than one method that matches the <paramref name="method" /> description was found.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.UnaryExpression.Method" /> property of the resulting <see cref="System.Linq.Expressions.UnaryExpression" /> is set to the implementing method. The <see cref="O:System.Linq.Expressions.UnaryExpression.IsLiftedToNull" /> property is <see langword="false" />. If the node is lifted, <see cref="O:System.Linq.Expressions.UnaryExpression.IsLifted" /> is <see langword="true" />. Otherwise, it is <see langword="false" />.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If method is not <see langword="null" />, it is the implementing method. It must represent a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes one argument.
        /// -   Otherwise, if either <paramref name="expression" />.Type or <paramref name="type" /> is a user-defined type that defines an implicit or explicit conversion operator, the <see cref="System.Reflection.MethodInfo" /> that represents that operator is the implementing method.
        /// -   Otherwise:
        /// -   If both <paramref name="expression" />.Type and <paramref name="type" /> represent numeric or Boolean types, or nullable or non-nullable enumeration types, the implementing method is <see langword="null" />.
        /// -   If either <paramref name="expression" />.Type or <paramref name="type" /> is a reference type and an explicit boxing, unboxing, or reference conversion exists from <paramref name="expression" />.Type to <paramref name="type" />, the implementing method is <see langword="null" />.
        /// #### Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="expression" />.Type is assignable to the argument type of the implementing method and the return type of the implementing method is assignable to <paramref name="type" />, the node is not lifted.
        /// -   If either or both of <paramref name="expression" />.Type or <paramref name="type" /> are a nullable value type and the corresponding non-nullable value types are equal to the argument type and the return type of the implementing method respectively, the node is lifted.
        /// If the implementing method is <see langword="null" />:
        /// -   If both <paramref name="expression" />.Type and <paramref name="type" /> are non-nullable, the node is not lifted.
        /// -   Otherwise the node is lifted.</remarks>
        [RequiresUnreferencedCode(ExpressionRequiresUnreferencedCode)]
        public static UnaryExpression Convert(Expression expression, Type type, MethodInfo? method)
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents a conversion operation that throws an exception if the target type is overflowed.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
        /// <param name="type">A <see cref="System.Type" /> to set the <see cref="System.Linq.Expressions.Expression.Type" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ConvertChecked" /> and the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> and <see cref="System.Linq.Expressions.Expression.Type" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> or <paramref name="type" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">No conversion operator is defined between <paramref name="expression" />.Type and <paramref name="type" />.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.UnaryExpression.Method" /> property of the resulting <see cref="System.Linq.Expressions.UnaryExpression" /> is set to the implementing method. The <see cref="O:System.Linq.Expressions.UnaryExpression.IsLiftedToNull" /> property is <see langword="false" />. If the node is lifted, <see cref="O:System.Linq.Expressions.UnaryExpression.IsLifted" /> is <see langword="true" />. Otherwise, it is <see langword="false" />.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If either <paramref name="expression" />.Type or <paramref name="type" /> is a user-defined type that defines an implicit or explicit conversion operator, the <see cref="System.Reflection.MethodInfo" /> that represents that operator is the implementing method.
        /// -   Otherwise:
        /// -   If both <paramref name="expression" />.Type and <paramref name="type" /> represent numeric or Boolean types, or nullable or non-nullable enumeration types, the implementing method is <see langword="null" />.
        /// -   If either <paramref name="expression" />.Type or <paramref name="type" /> is a reference type and an explicit boxing, unboxing, or reference conversion exists from <paramref name="expression" />.Type to <paramref name="type" />, the implementing method is <see langword="null" />.
        /// #### Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="expression" />.Type is assignable to the argument type of the implementing method and the return type of the implementing method is assignable to <paramref name="type" />, the node is not lifted.
        /// -   If either or both of <paramref name="expression" />.Type or <paramref name="type" /> are a nullable value type and the corresponding non-nullable value types are equal to the argument type and the return type of the implementing method respectively, the node is lifted.
        /// If the implementing method is <see langword="null" />:
        /// -   If both <paramref name="expression" />.Type and <paramref name="type" /> are non-nullable, the node is not lifted.
        /// -   Otherwise the node is lifted.</remarks>
        [RequiresUnreferencedCode(ExpressionRequiresUnreferencedCode)]
        public static UnaryExpression ConvertChecked(Expression expression, Type type)
        {
            return ConvertChecked(expression, type, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents a conversion operation that throws an exception if the target type is overflowed and for which the implementing method is specified.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
        /// <param name="type">A <see cref="System.Type" /> to set the <see cref="System.Linq.Expressions.Expression.Type" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ConvertChecked" /> and the <see cref="System.Linq.Expressions.UnaryExpression.Operand" />, <see cref="System.Linq.Expressions.Expression.Type" />, and <see cref="System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> or <paramref name="type" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly one argument.</exception>
        /// <exception cref="System.InvalidOperationException">No conversion operator is defined between <paramref name="expression" />.Type and <paramref name="type" />.
        /// -or-
        /// <paramref name="expression" />.Type is not assignable to the argument type of the method represented by <paramref name="method" />.
        /// -or-
        /// The return type of the method represented by <paramref name="method" /> is not assignable to <paramref name="type" />.
        /// -or-
        /// <paramref name="expression" />.Type or <paramref name="type" /> is a nullable value type and the corresponding non-nullable value type does not equal the argument type or the return type, respectively, of the method represented by <paramref name="method" />.</exception>
        /// <exception cref="System.Reflection.AmbiguousMatchException">More than one method that matches the <paramref name="method" /> description was found.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.UnaryExpression.Method" /> property of the resulting <see cref="System.Linq.Expressions.UnaryExpression" /> is set to the implementing method. The <see cref="O:System.Linq.Expressions.UnaryExpression.IsLiftedToNull" /> property is <see langword="false" />. If the node is lifted, <see cref="O:System.Linq.Expressions.UnaryExpression.IsLifted" /> is <see langword="true" />. Otherwise, it is <see langword="false" />.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If method is not <see langword="null" />, it is the implementing method. It must represent a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes one argument.
        /// -   Otherwise, if either <paramref name="expression" />.Type or <paramref name="type" /> is a user-defined type that defines an implicit or explicit conversion operator, the <see cref="System.Reflection.MethodInfo" /> that represents that operator is the implementing method.
        /// -   Otherwise:
        /// -   If both <paramref name="expression" />.Type and <paramref name="type" /> represent numeric or Boolean types, or nullable or non-nullable enumeration types, the implementing method is <see langword="null" />.
        /// -   If either <paramref name="expression" />.Type or <paramref name="type" /> is a reference type and an explicit boxing, unboxing, or reference conversion exists from <paramref name="expression" />.Type to <paramref name="type" />, the implementing method is <see langword="null" />.
        /// #### Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="expression" />.Type is assignable to the argument type of the implementing method and the return type of the implementing method is assignable to <paramref name="type" />, the node is not lifted.
        /// -   If either or both of <paramref name="expression" />.Type or <paramref name="type" /> are a nullable value type and the corresponding non-nullable value types are equal to the argument type and the return type of the implementing method respectively, the node is lifted.
        /// If the implementing method is <see langword="null" />:
        /// -   If both <paramref name="expression" />.Type and <paramref name="type" /> are non-nullable, the node is not lifted.
        /// -   Otherwise the node is lifted.</remarks>
        [RequiresUnreferencedCode(ExpressionRequiresUnreferencedCode)]
        public static UnaryExpression ConvertChecked(Expression expression, Type type, MethodInfo? method)
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents an expression for obtaining the length of a one-dimensional array.</summary>
        /// <param name="array">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ArrayLength" /> and the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property equal to <paramref name="array" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="array" />.Type does not represent an array type.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="array" /> must represent an array type.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.UnaryExpression" /> is equal to <see cref="int" />. The <see cref="O:System.Linq.Expressions.UnaryExpression.Method" /> property is <see langword="null" />, and both <see cref="O:System.Linq.Expressions.UnaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.UnaryExpression.IsLiftedToNull" /> are set to <see langword="false" />.</remarks>
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents an expression that has a constant value of type <see cref="System.Linq.Expressions.Expression" />.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Quote" /> and the <see cref="System.Linq.Expressions.UnaryExpression.Operand" /> property set to the specified value.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> is <see langword="null" />.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.UnaryExpression" /> represents the constructed type <see cref="System.Linq.Expressions.Expression{T}" />, where the type argument is the type represented by <paramref name="expression" />.Type. The <see cref="O:System.Linq.Expressions.UnaryExpression.Method" /> property is <see langword="null" />. Both <see cref="O:System.Linq.Expressions.UnaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.UnaryExpression.IsLiftedToNull" /> are <see langword="false" />.</remarks>
        public static UnaryExpression Quote(Expression expression)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            LambdaExpression? lambda = expression as LambdaExpression;
            if (lambda == null)
            {
                throw Error.QuotedExpressionMustBeLambda(nameof(expression));
            }

            return new UnaryExpression(ExpressionType.Quote, lambda, lambda.PublicType, null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents a rethrowing of an exception.</summary>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that represents a rethrowing of an exception.</returns>
        public static UnaryExpression Rethrow()
        {
            return Throw(value: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents a rethrowing of an exception with a given type.</summary>
        /// <param name="type">The new <see cref="System.Type" /> of the expression.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that represents a rethrowing of an exception.</returns>
        public static UnaryExpression Rethrow(Type type)
        {
            return Throw(null, type);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents a throwing of an exception.</summary>
        /// <param name="value">An <see cref="System.Linq.Expressions.Expression" />.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the exception.</returns>
        /// <remarks></remarks>
        /// <example>The following example demonstrates how to create a <see cref="System.Linq.Expressions.TryExpression" /> object that uses the <see cref="O:System.Linq.Expressions.Expression.Throw" /> method.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet47":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet47":::</example>
        public static UnaryExpression Throw(Expression? value)
        {
            return Throw(value, typeof(void));
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents a throwing of an exception with a given type.</summary>
        /// <param name="value">An <see cref="System.Linq.Expressions.Expression" />.</param>
        /// <param name="type">The new <see cref="System.Type" /> of the expression.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the exception.</returns>
        public static UnaryExpression Throw(Expression? value, Type type)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            TypeUtils.ValidateType(type, nameof(type));
            if (value != null)
            {
                ExpressionUtils.RequiresCanRead(value, nameof(value));
                if (value.Type.IsValueType) throw Error.ArgumentMustNotHaveValueType(nameof(value));
            }
            return new UnaryExpression(ExpressionType.Throw, value!, type, null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the incrementing of the expression value by 1.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to increment.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the incremented expression.</returns>
        /// <remarks>This expression is functional and does not change the value of the object that is passed to it.</remarks>
        /// <example>The following code example shows how to create an expression that represents an increment operation.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet24":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet24":::</example>
        public static UnaryExpression Increment(Expression expression)
        {
            return Increment(expression, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the incrementing of the expression by 1.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to increment.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> that represents the implementing method.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the incremented expression.</returns>
        /// <remarks>This expression is functional and does not change the value of the object that is passed to it.</remarks>
        public static UnaryExpression Increment(Expression expression, MethodInfo? method)
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the decrementing of the expression by 1.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to decrement.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the decremented expression.</returns>
        /// <remarks>This expression is functional and does not change the value of the object passed to it.</remarks>
        /// <example>The following code example shows how to create an expression that subtracts 1 from a given value.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet5":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet5":::</example>
        public static UnaryExpression Decrement(Expression expression)
        {
            return Decrement(expression, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the decrementing of the expression by 1.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to decrement.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> that represents the implementing method.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the decremented expression.</returns>
        /// <remarks>This expression is functional and does not change the value of the object passed to it.</remarks>
        public static UnaryExpression Decrement(Expression expression, MethodInfo? method)
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

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that increments the expression by 1 and assigns the result back to the expression.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to apply the operations on.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the resultant expression.</returns>
        public static UnaryExpression PreIncrementAssign(Expression expression)
        {
            return MakeOpAssignUnary(ExpressionType.PreIncrementAssign, expression, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that increments the expression by 1 and assigns the result back to the expression.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to apply the operations on.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> that represents the implementing method.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the resultant expression.</returns>
        public static UnaryExpression PreIncrementAssign(Expression expression, MethodInfo? method)
        {
            return MakeOpAssignUnary(ExpressionType.PreIncrementAssign, expression, method);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that decrements the expression by 1 and assigns the result back to the expression.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to apply the operations on.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the resultant expression.</returns>
        public static UnaryExpression PreDecrementAssign(Expression expression)
        {
            return MakeOpAssignUnary(ExpressionType.PreDecrementAssign, expression, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that decrements the expression by 1 and assigns the result back to the expression.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to apply the operations on.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> that represents the implementing method.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the resultant expression.</returns>
        public static UnaryExpression PreDecrementAssign(Expression expression, MethodInfo? method)
        {
            return MakeOpAssignUnary(ExpressionType.PreDecrementAssign, expression, method);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the assignment of the expression followed by a subsequent increment by 1 of the original expression.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to apply the operations on.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the resultant expression.</returns>
        public static UnaryExpression PostIncrementAssign(Expression expression)
        {
            return MakeOpAssignUnary(ExpressionType.PostIncrementAssign, expression, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the assignment of the expression followed by a subsequent increment by 1 of the original expression.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to apply the operations on.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> that represents the implementing method.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the resultant expression.</returns>
        public static UnaryExpression PostIncrementAssign(Expression expression, MethodInfo? method)
        {
            return MakeOpAssignUnary(ExpressionType.PostIncrementAssign, expression, method);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the assignment of the expression followed by a subsequent decrement by 1 of the original expression.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to apply the operations on.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the resultant expression.</returns>
        public static UnaryExpression PostDecrementAssign(Expression expression)
        {
            return MakeOpAssignUnary(ExpressionType.PostDecrementAssign, expression, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the assignment of the expression followed by a subsequent decrement by 1 of the original expression.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to apply the operations on.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> that represents the implementing method.</param>
        /// <returns>A <see cref="System.Linq.Expressions.UnaryExpression" /> that represents the resultant expression.</returns>
        public static UnaryExpression PostDecrementAssign(Expression expression, MethodInfo? method)
        {
            return MakeOpAssignUnary(ExpressionType.PostDecrementAssign, expression, method);
        }

        private static UnaryExpression MakeOpAssignUnary(ExpressionType kind, Expression expression, MethodInfo? method)
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
                throw Error.UserDefinedOpMustHaveValidReturnType(kind, method!.Name);
            }
            return result;
        }
    }
}
