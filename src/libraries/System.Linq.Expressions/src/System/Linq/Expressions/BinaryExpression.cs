// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;
using static System.Linq.Expressions.CachedReflectionInfo;

namespace System.Linq.Expressions
{
    /// <summary>Represents an expression that has a binary operator.</summary>
    /// <remarks>The following tables summarize the factory methods that can be used to create a <see cref="System.Linq.Expressions.BinaryExpression" /> that has a specific node type, represented by the <see cref="O:System.Linq.Expressions.Expression.NodeType" /> property. Each table contains information for a specific class of operations such as arithmetic or bitwise.
    /// ## Binary Arithmetic Operations
    /// |Node Type|Factory Method|
    /// |---------------|--------------------|
    /// |<see cref="System.Linq.Expressions.ExpressionType.Add" />|<see cref="O:System.Linq.Expressions.Expression.Add" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.AddChecked" />|<see cref="O:System.Linq.Expressions.Expression.AddChecked" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.Divide" />|<see cref="O:System.Linq.Expressions.Expression.Divide" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.Modulo" />|<see cref="O:System.Linq.Expressions.Expression.Modulo" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.Multiply" />|<see cref="O:System.Linq.Expressions.Expression.Multiply" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.MultiplyChecked" />|<see cref="O:System.Linq.Expressions.Expression.MultiplyChecked" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.Power" />|<see cref="O:System.Linq.Expressions.Expression.Power" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.Subtract" />|<see cref="O:System.Linq.Expressions.Expression.Subtract" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.SubtractChecked" />|<see cref="O:System.Linq.Expressions.Expression.SubtractChecked" />|
    /// ## Bitwise Operations
    /// |Node Type|Factory Method|
    /// |---------------|--------------------|
    /// |<see cref="System.Linq.Expressions.ExpressionType.And" />|<see cref="O:System.Linq.Expressions.Expression.And" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.Or" />|<see cref="O:System.Linq.Expressions.Expression.Or" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.ExclusiveOr" />|<see cref="O:System.Linq.Expressions.Expression.ExclusiveOr" />|
    /// ## Shift Operations
    /// |Node Type|Factory Method|
    /// |---------------|--------------------|
    /// |<see cref="System.Linq.Expressions.ExpressionType.LeftShift" />|<see cref="O:System.Linq.Expressions.Expression.LeftShift" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.RightShift" />|<see cref="O:System.Linq.Expressions.Expression.RightShift" />|
    /// ## Conditional Boolean Operations
    /// |Node Type|Factory Method|
    /// |---------------|--------------------|
    /// |<see cref="System.Linq.Expressions.ExpressionType.AndAlso" />|<see cref="O:System.Linq.Expressions.Expression.AndAlso" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.OrElse" />|<see cref="O:System.Linq.Expressions.Expression.OrElse" />|
    /// ## Comparison Operations
    /// |Node Type|Factory Method|
    /// |---------------|--------------------|
    /// |<see cref="System.Linq.Expressions.ExpressionType.Equal" />|<see cref="O:System.Linq.Expressions.Expression.Equal" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.NotEqual" />|<see cref="O:System.Linq.Expressions.Expression.NotEqual" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.GreaterThanOrEqual" />|<see cref="O:System.Linq.Expressions.Expression.GreaterThanOrEqual" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.GreaterThan" />|<see cref="O:System.Linq.Expressions.Expression.GreaterThan" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.LessThan" />|<see cref="O:System.Linq.Expressions.Expression.LessThan" />|
    /// |<see cref="System.Linq.Expressions.ExpressionType.LessThanOrEqual" />|<see cref="O:System.Linq.Expressions.Expression.LessThanOrEqual" />|
    /// ## Coalescing Operations
    /// |Node Type|Factory Method|
    /// |---------------|--------------------|
    /// |<see cref="System.Linq.Expressions.ExpressionType.Coalesce" />|<see cref="O:System.Linq.Expressions.Expression.Coalesce" />|
    /// ## Array Indexing Operations
    /// |Node Type|Factory Method|
    /// |---------------|--------------------|
    /// |<see cref="System.Linq.Expressions.ExpressionType.ArrayIndex" />|<see cref="O:System.Linq.Expressions.Expression.ArrayIndex" />|
    /// In addition, the <see cref="O:System.Linq.Expressions.Expression.MakeBinary" /> methods can also be used to create a <see cref="System.Linq.Expressions.BinaryExpression" />. These factory methods can be used to create a <see cref="System.Linq.Expressions.BinaryExpression" /> of any node type that represents a binary operation. The parameter of these methods that is of type <see cref="O:System.Linq.Expressions.Expression.NodeType" /> specifies the desired node type.</remarks>
    /// <example>The following example creates a <see cref="System.Linq.Expressions.BinaryExpression" /> object that represents the subtraction of one number from another.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet8":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet8":::</example>
    [DebuggerTypeProxy(typeof(BinaryExpressionProxy))]
    public class BinaryExpression : Expression
    {
        internal BinaryExpression(Expression left, Expression right)
        {
            Left = left;
            Right = right;
        }

        /// <summary>Gets a value that indicates whether the expression tree node can be reduced.</summary>
        /// <value><see langword="true" /> if the expression tree node can be reduced; otherwise, <see langword="false" />.</value>
        public override bool CanReduce
        {
            get
            {
                // Only OpAssignments are reducible.
                return IsOpAssignment(NodeType);
            }
        }

        private static bool IsOpAssignment(ExpressionType op)
        {
            switch (op)
            {
                case ExpressionType.AddAssign:
                case ExpressionType.SubtractAssign:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.AddAssignChecked:
                case ExpressionType.SubtractAssignChecked:
                case ExpressionType.MultiplyAssignChecked:
                case ExpressionType.DivideAssign:
                case ExpressionType.ModuloAssign:
                case ExpressionType.PowerAssign:
                case ExpressionType.AndAssign:
                case ExpressionType.OrAssign:
                case ExpressionType.RightShiftAssign:
                case ExpressionType.LeftShiftAssign:
                case ExpressionType.ExclusiveOrAssign:
                    return true;
            }
            return false;
        }

        /// <summary>Gets the right operand of the binary operation.</summary>
        /// <value>An <see cref="System.Linq.Expressions.Expression" /> that represents the right operand of the binary operation.</value>
        public Expression Right { get; }

        /// <summary>Gets the left operand of the binary operation.</summary>
        /// <value>An <see cref="System.Linq.Expressions.Expression" /> that represents the left operand of the binary operation.</value>
        public Expression Left { get; }

        /// <summary>Gets the implementing method for the binary operation.</summary>
        /// <value>The <see cref="System.Reflection.MethodInfo" /> that represents the implementing method.</value>
        /// <remarks>If a <see cref="System.Linq.Expressions.BinaryExpression" /> represents an operation that uses a predefined operator, the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property is <see langword="null" />.</remarks>
        public MethodInfo? Method => GetMethod();

        internal virtual MethodInfo? GetMethod() => null;

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="left">The <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property of the result.</param>
        /// <param name="conversion">The <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> property of the result.</param>
        /// <param name="right">The <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        // Note: takes children in evaluation order, which is also the order
        // that ExpressionVisitor visits them. Having them this way reduces the
        // chances people will make a mistake and use an inconsistent order in
        // derived visitors.
        public BinaryExpression Update(Expression left, LambdaExpression? conversion, Expression right)
        {
            if (left == Left && right == Right && conversion == Conversion)
            {
                return this;
            }
            if (IsReferenceComparison)
            {
                if (NodeType == ExpressionType.Equal)
                {
                    return Expression.ReferenceEqual(left, right);
                }
                else
                {
                    return Expression.ReferenceNotEqual(left, right);
                }
            }
            return Expression.MakeBinary(NodeType, left, right, IsLiftedToNull, Method, conversion);
        }

        /// <summary>Reduces the binary expression node to a simpler expression.</summary>
        /// <returns>The reduced expression.</returns>
        /// <remarks>If CanReduce returns true, this should return a valid expression.
        /// This method can return another node which itself must be reduced.</remarks>
        public override Expression Reduce()
        {
            // Only reduce OpAssignment expressions.
            if (IsOpAssignment(NodeType))
            {
                return Left.NodeType switch
                {
                    ExpressionType.MemberAccess => ReduceMember(),
                    ExpressionType.Index => ReduceIndex(),
                    _ => ReduceVariable(),
                };
            }
            return this;
        }

        // Return the corresponding Op of an assignment op.
        private static ExpressionType GetBinaryOpFromAssignmentOp(ExpressionType op)
        {
            Debug.Assert(IsOpAssignment(op));
            return op switch
            {
                ExpressionType.AddAssign => ExpressionType.Add,
                ExpressionType.AddAssignChecked => ExpressionType.AddChecked,
                ExpressionType.SubtractAssign => ExpressionType.Subtract,
                ExpressionType.SubtractAssignChecked => ExpressionType.SubtractChecked,
                ExpressionType.MultiplyAssign => ExpressionType.Multiply,
                ExpressionType.MultiplyAssignChecked => ExpressionType.MultiplyChecked,
                ExpressionType.DivideAssign => ExpressionType.Divide,
                ExpressionType.ModuloAssign => ExpressionType.Modulo,
                ExpressionType.PowerAssign => ExpressionType.Power,
                ExpressionType.AndAssign => ExpressionType.And,
                ExpressionType.OrAssign => ExpressionType.Or,
                ExpressionType.RightShiftAssign => ExpressionType.RightShift,
                ExpressionType.LeftShiftAssign => ExpressionType.LeftShift,
                ExpressionType.ExclusiveOrAssign => ExpressionType.ExclusiveOr,
                _ => throw ContractUtils.Unreachable,
            };
        }

        private Expression ReduceVariable()
        {
            // v (op)= r
            // ... is reduced into ...
            // v = v (op) r
            ExpressionType op = GetBinaryOpFromAssignmentOp(NodeType);
            Expression r = Expression.MakeBinary(op, Left, Right, false, Method);
            LambdaExpression? conversion = GetConversion();
            if (conversion != null)
            {
                r = Expression.Invoke(conversion, r);
            }
            return Expression.Assign(Left, r);
        }

        private Expression ReduceMember()
        {
            MemberExpression member = (MemberExpression)Left;

            if (member.Expression == null)
            {
                // static member, reduce the same as variable
                return ReduceVariable();
            }
            else
            {
                // left.b (op)= r
                // ... is reduced into ...
                // temp1 = left
                // temp2 = temp1.b (op) r
                // temp1.b = temp2
                // temp2
                ParameterExpression temp1 = Variable(member.Expression.Type, "temp1");

                // 1. temp1 = left
                Expression e1 = Expression.Assign(temp1, member.Expression);

                // 2. temp2 = temp1.b (op) r
                ExpressionType op = GetBinaryOpFromAssignmentOp(NodeType);
                Expression e2 = Expression.MakeBinary(op, Expression.MakeMemberAccess(temp1, member.Member), Right, false, Method);
                LambdaExpression? conversion = GetConversion();
                if (conversion != null)
                {
                    e2 = Expression.Invoke(conversion, e2);
                }
                ParameterExpression temp2 = Variable(e2.Type, "temp2");
                e2 = Expression.Assign(temp2, e2);

                // 3. temp1.b = temp2
                Expression e3 = Expression.Assign(Expression.MakeMemberAccess(temp1, member.Member), temp2);

                // 3. temp2
                Expression e4 = temp2;

                return Expression.Block(
                    new TrueReadOnlyCollection<ParameterExpression>(temp1, temp2),
                    new TrueReadOnlyCollection<Expression>(e1, e2, e3, e4)
                );
            }
        }

        private Expression ReduceIndex()
        {
            // left[a0, a1, ... aN] (op)= r
            //
            // ... is reduced into ...
            //
            // tempObj = left
            // tempArg0 = a0
            // ...
            // tempArgN = aN
            // tempValue = tempObj[tempArg0, ... tempArgN] (op) r
            // tempObj[tempArg0, ... tempArgN] = tempValue

            var index = (IndexExpression)Left;

            var vars = new ArrayBuilder<ParameterExpression>(index.ArgumentCount + 2);
            var exprs = new ArrayBuilder<Expression>(index.ArgumentCount + 3);

            ParameterExpression tempObj = Expression.Variable(index.Object!.Type, "tempObj");
            vars.UncheckedAdd(tempObj);
            exprs.UncheckedAdd(Expression.Assign(tempObj, index.Object));

            int n = index.ArgumentCount;
            var tempArgs = new ArrayBuilder<Expression>(n);
            for (var i = 0; i < n; i++)
            {
                Expression arg = index.GetArgument(i);
                ParameterExpression tempArg = Expression.Variable(arg.Type, "tempArg" + i);
                vars.UncheckedAdd(tempArg);
                tempArgs.UncheckedAdd(tempArg);
                exprs.UncheckedAdd(Expression.Assign(tempArg, arg));
            }

            IndexExpression tempIndex = Expression.MakeIndex(tempObj, index.Indexer, tempArgs.ToReadOnly());

            // tempValue = tempObj[tempArg0, ... tempArgN] (op) r
            ExpressionType binaryOp = GetBinaryOpFromAssignmentOp(NodeType);
            Expression op = Expression.MakeBinary(binaryOp, tempIndex, Right, false, Method);
            LambdaExpression? conversion = GetConversion();
            if (conversion != null)
            {
                op = Expression.Invoke(conversion, op);
            }
            ParameterExpression tempValue = Expression.Variable(op.Type, "tempValue");
            vars.UncheckedAdd(tempValue);
            exprs.UncheckedAdd(Expression.Assign(tempValue, op));

            // tempObj[tempArg0, ... tempArgN] = tempValue
            exprs.UncheckedAdd(Expression.Assign(tempIndex, tempValue));

            return Expression.Block(vars.ToReadOnly(), exprs.ToReadOnly());
        }

        /// <summary>Gets the type conversion function that is used by a coalescing or compound assignment operation.</summary>
        /// <value>A <see cref="System.Linq.Expressions.LambdaExpression" /> that represents a type conversion function.</value>
        /// <remarks>The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" /> for any <see cref="System.Linq.Expressions.BinaryExpression" /> whose <see cref="O:System.Linq.Expressions.Expression.NodeType" /> property is not <see cref="System.Linq.Expressions.ExpressionType.Coalesce" />.</remarks>
        public LambdaExpression? Conversion => GetConversion();

        internal virtual LambdaExpression? GetConversion() => null;

        /// <summary>Gets a value that indicates whether the expression tree node represents a *lifted* call to an operator.</summary>
        /// <value><see langword="true" /> if the node represents a lifted call; otherwise, <see langword="false" />.</value>
        /// <remarks>An operator call is lifted if the operator expects non-nullable operands but nullable operands are passed to it.</remarks>
        public bool IsLifted
        {
            get
            {
                if (NodeType == ExpressionType.Coalesce || NodeType == ExpressionType.Assign)
                {
                    return false;
                }
                if (Left.Type.IsNullableType())
                {
                    MethodInfo? method = GetMethod();
                    return method == null ||
                        !TypeUtils.AreEquivalent(method.GetParametersCached()[0].ParameterType.GetNonRefType(), Left.Type);
                }
                return false;
            }
        }

        /// <summary>Gets a value that indicates whether the expression tree node represents a *lifted* call to an operator whose return type is lifted to a nullable type.</summary>
        /// <value><see langword="true" /> if the operator's return type is lifted to a nullable type; otherwise, <see langword="false" />.</value>
        /// <remarks>An operator call is lifted if the operator expects non-nullable operands but nullable operands are passed to it. If the value of <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> is <see langword="true" />, the operator returns a nullable type, and if a nullable operand evaluates to <see langword="null" />, the operator returns <see langword="null" />.</remarks>
        public bool IsLiftedToNull => IsLifted && Type.IsNullableType();

        /// <summary>Dispatches to the specific visit method for this node type. For example, <see cref="System.Linq.Expressions.MethodCallExpression" /> calls the <see cref="System.Linq.Expressions.ExpressionVisitor.VisitMethodCall(System.Linq.Expressions.MethodCallExpression)" />.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        /// <remarks>This default implementation for <see cref="System.Linq.Expressions.ExpressionType.Extension" /> nodes calls <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />. Override this method to call into a more specific method on a derived visitor class of the <see cref="System.Linq.Expressions.ExpressionVisitor" /> class. However, it should still support unknown visitors by calling <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />.</remarks>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitBinary(this);
        }

        internal static BinaryExpression Create(ExpressionType nodeType, Expression left, Expression right, Type type, MethodInfo? method, LambdaExpression? conversion)
        {
            Debug.Assert(nodeType != ExpressionType.Assign);
            if (conversion != null)
            {
                Debug.Assert(method == null && TypeUtils.AreEquivalent(type, right.Type) && nodeType == ExpressionType.Coalesce);
                return new CoalesceConversionBinaryExpression(left, right, conversion);
            }
            if (method != null)
            {
                return new MethodBinaryExpression(nodeType, left, right, type, method);
            }
            if (type == typeof(bool))
            {
                return new LogicalBinaryExpression(nodeType, left, right);
            }
            return new SimpleBinaryExpression(nodeType, left, right, type);
        }

        internal bool IsLiftedLogical
        {
            get
            {
                Type left = Left.Type;
                Type right = Right.Type;
                MethodInfo? method = GetMethod();
                ExpressionType kind = NodeType;

                return
                    (kind == ExpressionType.AndAlso || kind == ExpressionType.OrElse) &&
                    TypeUtils.AreEquivalent(right, left) &&
                    left.IsNullableType() &&
                    method != null &&
                    TypeUtils.AreEquivalent(method.ReturnType, left.GetNonNullableType());
            }
        }

        internal bool IsReferenceComparison
        {
            get
            {
                Type left = Left.Type;
                Type right = Right.Type;
                MethodInfo? method = GetMethod();
                ExpressionType kind = NodeType;

                return (kind == ExpressionType.Equal || kind == ExpressionType.NotEqual) &&
                    method == null && !left.IsValueType && !right.IsValueType;
            }
        }

        //
        // For a user-defined type T which has op_False defined and L, R are
        // nullable, (L AndAlso R) is computed as:
        //
        // L.HasValue
        //     ? T.op_False(L.GetValueOrDefault())
        //         ? L
        //         : R.HasValue
        //             ? (T?)(T.op_BitwiseAnd(L.GetValueOrDefault(), R.GetValueOrDefault()))
        //             : null
        //     : null
        //
        // For a user-defined type T which has op_True defined and L, R are
        // nullable, (L OrElse R)  is computed as:
        //
        // L.HasValue
        //     ? T.op_True(L.GetValueOrDefault())
        //         ? L
        //         : R.HasValue
        //             ? (T?)(T.op_BitwiseOr(L.GetValueOrDefault(), R.GetValueOrDefault()))
        //             : null
        //     : null
        //
        //
        // This is the same behavior as VB. If you think about it, it makes
        // sense: it's combining the normal pattern for short-circuiting
        // operators, with the normal pattern for lifted operations: if either
        // of the operands is null, the result is also null.
        //
        internal Expression ReduceUserdefinedLifted()
        {
            Debug.Assert(IsLiftedLogical);

            ParameterExpression left = Parameter(Left.Type, "left");
            ParameterExpression right = Parameter(Right.Type, "right");
            string opName = NodeType == ExpressionType.AndAlso ? "op_False" : "op_True";
            MethodInfo? opTrueFalse = TypeUtils.GetBooleanOperator(Method!.DeclaringType!, opName);
            Debug.Assert(opTrueFalse != null);

            return Block(
                new TrueReadOnlyCollection<ParameterExpression>(left),
                new TrueReadOnlyCollection<Expression>(
                    Assign(left, Left),
                    Condition(
                        GetHasValueProperty(left),
                        Condition(
                            Call(opTrueFalse, CallGetValueOrDefault(left)),
                            left,
                            Block(
                                new TrueReadOnlyCollection<ParameterExpression>(right),
                                new TrueReadOnlyCollection<Expression>(
                                    Assign(right, Right),
                                    Condition(
                                        GetHasValueProperty(right),
                                        Convert(
                                            Call(
                                                Method,
                                                CallGetValueOrDefault(left),
                                                CallGetValueOrDefault(right)
                                            ),
                                            Type
                                        ),
                                        Constant(null, Type)
                                    )
                                )
                            )
                        ),
                        Constant(null, Type)
                    )
                )
            );
        }

        [DynamicDependency("GetValueOrDefault", typeof(Nullable<>))]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The method will be preserved by the DynamicDependency.")]
        private static MethodCallExpression CallGetValueOrDefault(ParameterExpression nullable)
        {
            return Call(nullable, "GetValueOrDefault", null);
        }

        [DynamicDependency("HasValue", typeof(Nullable<>))]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "The property will be preserved by the DynamicDependency.")]
        private static MemberExpression GetHasValueProperty(ParameterExpression nullable)
        {
            return Property(nullable, "HasValue");
        }
    }

    // Optimized representation of simple logical expressions:
    // && || == != > < >= <=
    internal sealed class LogicalBinaryExpression : BinaryExpression
    {
        internal LogicalBinaryExpression(ExpressionType nodeType, Expression left, Expression right)
            : base(left, right)
        {
            NodeType = nodeType;
        }

        public sealed override Type Type => typeof(bool);

        public sealed override ExpressionType NodeType { get; }
    }

    // Optimized assignment node, only holds onto children
    internal class AssignBinaryExpression : BinaryExpression
    {
        internal AssignBinaryExpression(Expression left, Expression right)
            : base(left, right)
        {
        }

        public static AssignBinaryExpression Make(Expression left, Expression right, bool byRef)
        {
            if (byRef)
            {
                return new ByRefAssignBinaryExpression(left, right);
            }
            else
            {
                return new AssignBinaryExpression(left, right);
            }
        }

        internal virtual bool IsByRef => false;

        public sealed override Type Type => Left.Type;

        public sealed override ExpressionType NodeType => ExpressionType.Assign;
    }

    internal sealed class ByRefAssignBinaryExpression : AssignBinaryExpression
    {
        internal ByRefAssignBinaryExpression(Expression left, Expression right)
            : base(left, right)
        {
        }

        internal override bool IsByRef => true;
    }

    // Coalesce with conversion
    // This is not a frequently used node, but rather we want to save every
    // other BinaryExpression from holding onto the null conversion lambda
    internal sealed class CoalesceConversionBinaryExpression : BinaryExpression
    {
        private readonly LambdaExpression _conversion;

        internal CoalesceConversionBinaryExpression(Expression left, Expression right, LambdaExpression conversion)
            : base(left, right)
        {
            _conversion = conversion;
        }

        internal override LambdaExpression GetConversion() => _conversion;

        public sealed override ExpressionType NodeType => ExpressionType.Coalesce;

        public sealed override Type Type => Right.Type;
    }

    // OpAssign with conversion
    // This is not a frequently used node, but rather we want to save every
    // other BinaryExpression from holding onto the null conversion lambda
    internal sealed class OpAssignMethodConversionBinaryExpression : MethodBinaryExpression
    {
        private readonly LambdaExpression _conversion;

        internal OpAssignMethodConversionBinaryExpression(ExpressionType nodeType, Expression left, Expression right, Type type, MethodInfo method, LambdaExpression conversion)
            : base(nodeType, left, right, type, method)
        {
            _conversion = conversion;
        }

        internal override LambdaExpression GetConversion() => _conversion;
    }

    // Class that handles most binary expressions
    // If needed, it can be optimized even more (often Type == left.Type)
    internal class SimpleBinaryExpression : BinaryExpression
    {
        internal SimpleBinaryExpression(ExpressionType nodeType, Expression left, Expression right, Type type)
            : base(left, right)
        {
            NodeType = nodeType;
            Type = type;
        }

        public sealed override ExpressionType NodeType { get; }

        public sealed override Type Type { get; }
    }

    // Class that handles binary expressions with a method
    // If needed, it can be optimized even more (often Type == method.ReturnType)
    internal class MethodBinaryExpression : SimpleBinaryExpression
    {
        private readonly MethodInfo _method;

        internal MethodBinaryExpression(ExpressionType nodeType, Expression left, Expression right, Type type, MethodInfo method)
            : base(nodeType, left, right, type)
        {
            _method = method;
        }

        internal override MethodInfo GetMethod() => _method;
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        #region Assign
        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an assignment operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Assign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <remarks>The `Assign` expression copies a value for value types, and it copies a reference for reference types.</remarks>
        /// <example>The following code example shows how to create an expression that represents an assignment operation.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet12":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet12":::</example>
        public static BinaryExpression Assign(Expression left, Expression right)
        {
            RequiresCanWrite(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            TypeUtils.ValidateType(left.Type, nameof(left), allowByRef: true, allowPointer: true);
            TypeUtils.ValidateType(right.Type, nameof(right), allowByRef: true, allowPointer: true);
            if (!TypeUtils.AreReferenceAssignable(left.Type, right.Type))
            {
                throw Error.ExpressionTypeDoesNotMatchAssignment(right.Type, left.Type);
            }
            return new AssignBinaryExpression(left, right);
        }

        #endregion

        private static BinaryExpression? GetUserDefinedBinaryOperator(ExpressionType binaryType, string name, Expression left, Expression right, bool liftToNull)
        {
            // try exact match first
            MethodInfo? method = GetUserDefinedBinaryOperator(binaryType, left.Type, right.Type, name);
            if (method != null)
            {
                return new MethodBinaryExpression(binaryType, left, right, method.ReturnType, method);
            }
            // try lifted call
            if (left.Type.IsNullableType() && right.Type.IsNullableType())
            {
                Type nnLeftType = left.Type.GetNonNullableType();
                Type nnRightType = right.Type.GetNonNullableType();
                method = GetUserDefinedBinaryOperator(binaryType, nnLeftType, nnRightType, name);
                if (method != null && method.ReturnType.IsValueType && !method.ReturnType.IsNullableType())
                {
                    if (method.ReturnType != typeof(bool) || liftToNull)
                    {
                        return new MethodBinaryExpression(binaryType, left, right, method.ReturnType.GetNullableType(), method);
                    }
                    else
                    {
                        return new MethodBinaryExpression(binaryType, left, right, typeof(bool), method);
                    }
                }
            }
            return null;
        }

        private static BinaryExpression GetMethodBasedBinaryOperator(ExpressionType binaryType, Expression left, Expression right, MethodInfo method, bool liftToNull)
        {
            System.Diagnostics.Debug.Assert(method != null);
            ValidateOperator(method);
            ParameterInfo[] pms = method.GetParametersCached();
            if (pms.Length != 2)
                throw Error.IncorrectNumberOfMethodCallArguments(method, nameof(method));
            if (ParameterIsAssignable(pms[0], left.Type) && ParameterIsAssignable(pms[1], right.Type))
            {
                ValidateParamswithOperandsOrThrow(pms[0].ParameterType, left.Type, binaryType, method.Name);
                ValidateParamswithOperandsOrThrow(pms[1].ParameterType, right.Type, binaryType, method.Name);
                return new MethodBinaryExpression(binaryType, left, right, method.ReturnType, method);
            }
            // check for lifted call
            if (left.Type.IsNullableType() && right.Type.IsNullableType() &&
                ParameterIsAssignable(pms[0], left.Type.GetNonNullableType()) &&
                ParameterIsAssignable(pms[1], right.Type.GetNonNullableType()) &&
                method.ReturnType.IsValueType && !method.ReturnType.IsNullableType())
            {
                if (method.ReturnType != typeof(bool) || liftToNull)
                {
                    return new MethodBinaryExpression(binaryType, left, right, method.ReturnType.GetNullableType(), method);
                }
                else
                {
                    return new MethodBinaryExpression(binaryType, left, right, typeof(bool), method);
                }
            }
            throw Error.OperandTypesDoNotMatchParameters(binaryType, method.Name);
        }

        private static BinaryExpression GetMethodBasedAssignOperator(ExpressionType binaryType, Expression left, Expression right, MethodInfo method, LambdaExpression? conversion, bool liftToNull)
        {
            BinaryExpression b = GetMethodBasedBinaryOperator(binaryType, left, right, method, liftToNull);
            if (conversion == null)
            {
                // return type must be assignable back to the left type
                if (!TypeUtils.AreReferenceAssignable(left.Type, b.Type))
                {
                    throw Error.UserDefinedOpMustHaveValidReturnType(binaryType, b.Method!.Name);
                }
            }
            else
            {
                // add the conversion to the result
                ValidateOpAssignConversionLambda(conversion, b.Left, b.Method!, b.NodeType);
                b = new OpAssignMethodConversionBinaryExpression(b.NodeType, b.Left, b.Right, b.Left.Type, b.Method!, conversion);
            }
            return b;
        }

        private static BinaryExpression GetUserDefinedBinaryOperatorOrThrow(ExpressionType binaryType, string name, Expression left, Expression right, bool liftToNull)
        {
            BinaryExpression? b = GetUserDefinedBinaryOperator(binaryType, name, left, right, liftToNull);
            if (b != null)
            {
                ParameterInfo[] pis = b.Method!.GetParametersCached();
                ValidateParamswithOperandsOrThrow(pis[0].ParameterType, left.Type, binaryType, name);
                ValidateParamswithOperandsOrThrow(pis[1].ParameterType, right.Type, binaryType, name);
                return b;
            }
            throw Error.BinaryOperatorNotDefined(binaryType, left.Type, right.Type);
        }

        private static BinaryExpression GetUserDefinedAssignOperatorOrThrow(ExpressionType binaryType, string name, Expression left, Expression right, LambdaExpression? conversion, bool liftToNull)
        {
            BinaryExpression b = GetUserDefinedBinaryOperatorOrThrow(binaryType, name, left, right, liftToNull);
            if (conversion == null)
            {
                // return type must be assignable back to the left type
                if (!TypeUtils.AreReferenceAssignable(left.Type, b.Type))
                {
                    throw Error.UserDefinedOpMustHaveValidReturnType(binaryType, b.Method!.Name);
                }
            }
            else
            {
                // add the conversion to the result
                ValidateOpAssignConversionLambda(conversion, b.Left, b.Method!, b.NodeType);
                b = new OpAssignMethodConversionBinaryExpression(b.NodeType, b.Left, b.Right, b.Left.Type, b.Method!, conversion);
            }
            return b;
        }

        private static MethodInfo? GetUserDefinedBinaryOperator(ExpressionType binaryType, Type leftType, Type rightType, string name)
        {
            // This algorithm is wrong, we should be checking for uniqueness and erroring if
            // it is defined on both types.
            Type[] types = new Type[] { leftType, rightType };
            Type nnLeftType = leftType.GetNonNullableType();
            Type nnRightType = rightType.GetNonNullableType();
            MethodInfo? method = nnLeftType.GetAnyStaticMethodValidated(name, types);
            if (method == null && !TypeUtils.AreEquivalent(leftType, rightType))
            {
                method = nnRightType.GetAnyStaticMethodValidated(name, types);
            }

            if (IsLiftingConditionalLogicalOperator(leftType, rightType, method, binaryType))
            {
                method = GetUserDefinedBinaryOperator(binaryType, nnLeftType, nnRightType, name);
            }
            return method;
        }

        private static bool IsLiftingConditionalLogicalOperator(Type left, Type right, MethodInfo? method, ExpressionType binaryType)
        {
            return right.IsNullableType() &&
                    left.IsNullableType() &&
                    method == null &&
                    (binaryType == ExpressionType.AndAlso || binaryType == ExpressionType.OrElse);
        }

        internal static bool ParameterIsAssignable(ParameterInfo pi, Type argType)
        {
            Type pType = pi.ParameterType;
            if (pType.IsByRef)
                pType = pType.GetElementType()!;
            return TypeUtils.AreReferenceAssignable(pType, argType);
        }

        private static void ValidateParamswithOperandsOrThrow(Type paramType, Type operandType, ExpressionType exprType, string name)
        {
            if (paramType.IsNullableType() && !operandType.IsNullableType())
            {
                throw Error.OperandTypesDoNotMatchParameters(exprType, name);
            }
        }

        private static void ValidateOperator(MethodInfo method)
        {
            Debug.Assert(method != null);
            ValidateMethodInfo(method, nameof(method));
            if (!method.IsStatic)
                throw Error.UserDefinedOperatorMustBeStatic(method, nameof(method));
            if (method.ReturnType == typeof(void))
                throw Error.UserDefinedOperatorMustNotBeVoid(method, nameof(method));
        }

        private static void ValidateMethodInfo(MethodInfo method, string paramName)
        {
            if (method.ContainsGenericParameters)
                throw method.IsGenericMethodDefinition ? Error.MethodIsGeneric(method, paramName) : Error.MethodContainsGenericParameters(method, paramName);
        }

        private static bool IsNullComparison(Expression left, Expression right)
        {
            // If we have x==null, x!=null, null==x or null!=x where x is
            // nullable but not null, then this is treated as a call to x.HasValue
            // and is legal even if there is no equality operator defined on the
            // type of x.
            return IsNullConstant(left)
                ? !IsNullConstant(right) && right.Type.IsNullableType()
                : IsNullConstant(right) && left.Type.IsNullableType();
        }

        // Note: this has different meaning than ConstantCheck.IsNull
        // That function attempts to determine if the result of a tree will be
        // null at runtime. This function is used at tree construction time and
        // only looks for a ConstantExpression with a null Value. It can't
        // become "smarter" or that would break tree construction.
        private static bool IsNullConstant(Expression e)
        {
            var c = e as ConstantExpression;
            return c != null && c.Value == null;
        }

        private static void ValidateUserDefinedConditionalLogicOperator(ExpressionType nodeType, Type left, Type right, MethodInfo method)
        {
            ValidateOperator(method);
            ParameterInfo[] pms = method.GetParametersCached();
            if (pms.Length != 2)
                throw Error.IncorrectNumberOfMethodCallArguments(method, nameof(method));
            if (!ParameterIsAssignable(pms[0], left))
            {
                if (!(left.IsNullableType() && ParameterIsAssignable(pms[0], left.GetNonNullableType())))
                    throw Error.OperandTypesDoNotMatchParameters(nodeType, method.Name);
            }
            if (!ParameterIsAssignable(pms[1], right))
            {
                if (!(right.IsNullableType() && ParameterIsAssignable(pms[1], right.GetNonNullableType())))
                    throw Error.OperandTypesDoNotMatchParameters(nodeType, method.Name);
            }
            if (pms[0].ParameterType != pms[1].ParameterType)
            {
                throw Error.UserDefinedOpMustHaveConsistentTypes(nodeType, method.Name);
            }
            if (method.ReturnType != pms[0].ParameterType)
            {
                throw Error.UserDefinedOpMustHaveConsistentTypes(nodeType, method.Name);
            }
            if (IsValidLiftedConditionalLogicalOperator(left, right, pms))
            {
                left = left.GetNonNullableType();
            }
            Type? declaringType = method.DeclaringType;
            if (declaringType == null)
            {
                throw Error.LogicalOperatorMustHaveBooleanOperators(nodeType, method.Name);
            }
            MethodInfo? opTrue = TypeUtils.GetBooleanOperator(declaringType, "op_True");
            MethodInfo? opFalse = TypeUtils.GetBooleanOperator(declaringType, "op_False");
            if (opTrue == null || opTrue.ReturnType != typeof(bool) ||
                opFalse == null || opFalse.ReturnType != typeof(bool))
            {
                throw Error.LogicalOperatorMustHaveBooleanOperators(nodeType, method.Name);
            }
            VerifyOpTrueFalse(nodeType, left, opFalse, nameof(method));
            VerifyOpTrueFalse(nodeType, left, opTrue, nameof(method));
        }

        private static void VerifyOpTrueFalse(ExpressionType nodeType, Type left, MethodInfo opTrue, string paramName)
        {
            ParameterInfo[] pmsOpTrue = opTrue.GetParametersCached();
            if (pmsOpTrue.Length != 1)
                throw Error.IncorrectNumberOfMethodCallArguments(opTrue, paramName);

            if (!ParameterIsAssignable(pmsOpTrue[0], left))
            {
                if (!(left.IsNullableType() && ParameterIsAssignable(pmsOpTrue[0], left.GetNonNullableType())))
                    throw Error.OperandTypesDoNotMatchParameters(nodeType, opTrue.Name);
            }
        }

        private static bool IsValidLiftedConditionalLogicalOperator(Type left, Type right, ParameterInfo[] pms)
        {
            return TypeUtils.AreEquivalent(left, right) &&
                   right.IsNullableType() &&
                   TypeUtils.AreEquivalent(pms[1].ParameterType, right.GetNonNullableType());
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" />, given the left and right operands, by calling an appropriate factory method.</summary>
        /// <param name="binaryType">The <see cref="System.Linq.Expressions.ExpressionType" /> that specifies the type of binary operation.</param>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> that represents the left operand.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> that represents the right operand.</param>
        /// <returns>The <see cref="System.Linq.Expressions.BinaryExpression" /> that results from calling the appropriate factory method.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="binaryType" /> does not correspond to a binary expression node.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <remarks>The <paramref name="binaryType" /> parameter determines which <see cref="System.Linq.Expressions.BinaryExpression" /> factory method this method calls. For example, if <paramref name="binaryType" /> is <see cref="System.Linq.Expressions.ExpressionType.Subtract" />, this method invokes <see cref="O:System.Linq.Expressions.Expression.Subtract" />.</remarks>
        /// <example>The following example demonstrates how to use the <see cref="System.Linq.Expressions.Expression.MakeBinary(System.Linq.Expressions.ExpressionType,System.Linq.Expressions.Expression,System.Linq.Expressions.Expression)" /> method to create a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents the subtraction of one number from another.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet8":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet8":::</example>
        public static BinaryExpression MakeBinary(ExpressionType binaryType, Expression left, Expression right)
        {
            return MakeBinary(binaryType, left, right, liftToNull: false, method: null, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" />, given the left operand, right operand and implementing method, by calling the appropriate factory method.</summary>
        /// <param name="binaryType">The <see cref="System.Linq.Expressions.ExpressionType" /> that specifies the type of binary operation.</param>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> that represents the left operand.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> that represents the right operand.</param>
        /// <param name="liftToNull"><see langword="true" /> to set <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to <see langword="true" />; <see langword="false" /> to set <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to <see langword="false" />.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> that specifies the implementing method.</param>
        /// <returns>The <see cref="System.Linq.Expressions.BinaryExpression" /> that results from calling the appropriate factory method.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="binaryType" /> does not correspond to a binary expression node.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <remarks>The <paramref name="binaryType" /> parameter determines which <see cref="System.Linq.Expressions.BinaryExpression" /> factory method this method will call. For example, if <paramref name="binaryType" /> is <see cref="System.Linq.Expressions.ExpressionType.Subtract" />, this method invokes <see cref="O:System.Linq.Expressions.Expression.Subtract" />. The <paramref name="liftToNull" /> and <paramref name="method" /> parameters are ignored if the appropriate factory method does not have a corresponding parameter.</remarks>
        public static BinaryExpression MakeBinary(ExpressionType binaryType, Expression left, Expression right, bool liftToNull, MethodInfo? method)
        {
            return MakeBinary(binaryType, left, right, liftToNull, method, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" />, given the left operand, right operand, implementing method and type conversion function, by calling the appropriate factory method.</summary>
        /// <param name="binaryType">The <see cref="System.Linq.Expressions.ExpressionType" /> that specifies the type of binary operation.</param>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> that represents the left operand.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> that represents the right operand.</param>
        /// <param name="liftToNull"><see langword="true" /> to set <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to <see langword="true" />; <see langword="false" /> to set <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to <see langword="false" />.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> that specifies the implementing method.</param>
        /// <param name="conversion">A <see cref="System.Linq.Expressions.LambdaExpression" /> that represents a type conversion function. This parameter is used only if <paramref name="binaryType" /> is <see cref="System.Linq.Expressions.ExpressionType.Coalesce" /> or compound assignment.</param>
        /// <returns>The <see cref="System.Linq.Expressions.BinaryExpression" /> that results from calling the appropriate factory method.</returns>
        /// <exception cref="System.ArgumentException"><paramref name="binaryType" /> does not correspond to a binary expression node.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <remarks>The <paramref name="binaryType" /> parameter determines which <see cref="System.Linq.Expressions.BinaryExpression" /> factory method this method will call. For example, if <paramref name="binaryType" /> is <see cref="System.Linq.Expressions.ExpressionType.Subtract" />, this method invokes <see cref="O:System.Linq.Expressions.Expression.Subtract" />. The <paramref name="liftToNull" />, <paramref name="method" /> and <paramref name="conversion" /> parameters are ignored if the appropriate factory method does not have a corresponding parameter.</remarks>
        public static BinaryExpression MakeBinary(ExpressionType binaryType, Expression left, Expression right, bool liftToNull, MethodInfo? method, LambdaExpression? conversion) =>
            binaryType switch
            {
                ExpressionType.Add => Add(left, right, method),
                ExpressionType.AddChecked => AddChecked(left, right, method),
                ExpressionType.Subtract => Subtract(left, right, method),
                ExpressionType.SubtractChecked => SubtractChecked(left, right, method),
                ExpressionType.Multiply => Multiply(left, right, method),
                ExpressionType.MultiplyChecked => MultiplyChecked(left, right, method),
                ExpressionType.Divide => Divide(left, right, method),
                ExpressionType.Modulo => Modulo(left, right, method),
                ExpressionType.Power => Power(left, right, method),
                ExpressionType.And => And(left, right, method),
                ExpressionType.AndAlso => AndAlso(left, right, method),
                ExpressionType.Or => Or(left, right, method),
                ExpressionType.OrElse => OrElse(left, right, method),
                ExpressionType.LessThan => LessThan(left, right, liftToNull, method),
                ExpressionType.LessThanOrEqual => LessThanOrEqual(left, right, liftToNull, method),
                ExpressionType.GreaterThan => GreaterThan(left, right, liftToNull, method),
                ExpressionType.GreaterThanOrEqual => GreaterThanOrEqual(left, right, liftToNull, method),
                ExpressionType.Equal => Equal(left, right, liftToNull, method),
                ExpressionType.NotEqual => NotEqual(left, right, liftToNull, method),
                ExpressionType.ExclusiveOr => ExclusiveOr(left, right, method),
                ExpressionType.Coalesce => Coalesce(left, right, conversion),
                ExpressionType.ArrayIndex => ArrayIndex(left, right),
                ExpressionType.RightShift => RightShift(left, right, method),
                ExpressionType.LeftShift => LeftShift(left, right, method),
                ExpressionType.Assign => Assign(left, right),
                ExpressionType.AddAssign => AddAssign(left, right, method, conversion),
                ExpressionType.AndAssign => AndAssign(left, right, method, conversion),
                ExpressionType.DivideAssign => DivideAssign(left, right, method, conversion),
                ExpressionType.ExclusiveOrAssign => ExclusiveOrAssign(left, right, method, conversion),
                ExpressionType.LeftShiftAssign => LeftShiftAssign(left, right, method, conversion),
                ExpressionType.ModuloAssign => ModuloAssign(left, right, method, conversion),
                ExpressionType.MultiplyAssign => MultiplyAssign(left, right, method, conversion),
                ExpressionType.OrAssign => OrAssign(left, right, method, conversion),
                ExpressionType.PowerAssign => PowerAssign(left, right, method, conversion),
                ExpressionType.RightShiftAssign => RightShiftAssign(left, right, method, conversion),
                ExpressionType.SubtractAssign => SubtractAssign(left, right, method, conversion),
                ExpressionType.AddAssignChecked => AddAssignChecked(left, right, method, conversion),
                ExpressionType.SubtractAssignChecked => SubtractAssignChecked(left, right, method, conversion),
                ExpressionType.MultiplyAssignChecked => MultiplyAssignChecked(left, right, method, conversion),
                _ => throw Error.UnhandledBinary(binaryType, nameof(binaryType)),
            };

        #region Equality Operators

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an equality comparison.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Equal" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The equality operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> property is <see langword="true" />. Otherwise, it is <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> property is always <see langword="false" />. The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the equality operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is <see cref="bool" />:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is <see cref="bool" />.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is <see cref="bool" />.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is <see cref="bool" />.</remarks>
        /// <example>The following code example shows how to create an expression that checks whether the values of its two arguments are equal.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet8":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet8":::</example>
        public static BinaryExpression Equal(Expression left, Expression right)
        {
            return Equal(left, right, liftToNull: false, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an equality comparison. The implementing method can be specified.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="liftToNull"><see langword="true" /> to set <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to <see langword="true" />; <see langword="false" /> to set <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to <see langword="false" />.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Equal" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the equality operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> property is <see langword="true" /> and the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> property is equal to <paramref name="liftToNull" />. Otherwise, they are both <see langword="false" />. The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the equality operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted; also, the type of the node is nullable <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="true" /> or <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="false" />:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is <see cref="bool" />.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is <see cref="bool" />.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is nullable <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="true" /> or <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="false" />.</remarks>
        public static BinaryExpression Equal(Expression left, Expression right, bool liftToNull, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                return GetEqualityComparisonOperator(ExpressionType.Equal, "op_Equality", left, right, liftToNull);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.Equal, left, right, method, liftToNull);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a reference equality comparison.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Equal" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        public static BinaryExpression ReferenceEqual(Expression left, Expression right)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (TypeUtils.HasReferenceEquality(left.Type, right.Type))
            {
                return new LogicalBinaryExpression(ExpressionType.Equal, left, right);
            }
            throw Error.ReferenceEqualityNotDefined(left.Type, right.Type);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an inequality comparison.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.NotEqual" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The inequality operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> property is <see langword="true" />. Otherwise, it is <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> property is always <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the inequality operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is <see cref="bool" />:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is <see cref="bool" />.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is <see cref="bool" />.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is <see cref="bool" />.</remarks>
        public static BinaryExpression NotEqual(Expression left, Expression right)
        {
            return NotEqual(left, right, liftToNull: false, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an inequality comparison.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="liftToNull"><see langword="true" /> to set <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to <see langword="true" />; <see langword="false" /> to set <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to <see langword="false" />.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.NotEqual" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the inequality operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> property is <see langword="true" /> and the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> property is equal to <paramref name="liftToNull" />. Otherwise, they are both <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the inequality operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted; also, the type of the node is nullable <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="true" /> or <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="false" />:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is <see cref="bool" />.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is <see cref="bool" />.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is nullable <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="true" /> or <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="false" />.</remarks>
        public static BinaryExpression NotEqual(Expression left, Expression right, bool liftToNull, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                return GetEqualityComparisonOperator(ExpressionType.NotEqual, "op_Inequality", left, right, liftToNull);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.NotEqual, left, right, method, liftToNull);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a reference inequality comparison.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.NotEqual" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        public static BinaryExpression ReferenceNotEqual(Expression left, Expression right)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (TypeUtils.HasReferenceEquality(left.Type, right.Type))
            {
                return new LogicalBinaryExpression(ExpressionType.NotEqual, left, right);
            }
            throw Error.ReferenceEqualityNotDefined(left.Type, right.Type);
        }

        private static BinaryExpression GetEqualityComparisonOperator(ExpressionType binaryType, string opName, Expression left, Expression right, bool liftToNull)
        {
            // known comparison - numeric types, bools, object, enums
            if (left.Type == right.Type && (left.Type.IsNumeric() ||
                left.Type == typeof(object) ||
                left.Type.IsBool() ||
                left.Type.GetNonNullableType().IsEnum))
            {
                if (left.Type.IsNullableType() && liftToNull)
                {
                    return new SimpleBinaryExpression(binaryType, left, right, typeof(bool?));
                }
                else
                {
                    return new LogicalBinaryExpression(binaryType, left, right);
                }
            }
            // look for user defined operator
            BinaryExpression? b = GetUserDefinedBinaryOperator(binaryType, opName, left, right, liftToNull);
            if (b != null)
            {
                return b;
            }
            if (TypeUtils.HasBuiltInEqualityOperator(left.Type, right.Type) || IsNullComparison(left, right))
            {
                if (left.Type.IsNullableType() && liftToNull)
                {
                    return new SimpleBinaryExpression(binaryType, left, right, typeof(bool?));
                }
                else
                {
                    return new LogicalBinaryExpression(binaryType, left, right);
                }
            }
            throw Error.BinaryOperatorNotDefined(binaryType, left.Type, right.Type);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a "greater than" numeric comparison.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.GreaterThan" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The "greater than" operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> property is <see langword="true" />. Otherwise, it is <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> property is always <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the "greater than" operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is <see cref="bool" />:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is <see cref="bool" />.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is <see cref="bool" />.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is <see cref="bool" />.</remarks>
        /// <example>The following code example shows how to create an expression that compares two integers.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet10":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet10":::</example>
        public static BinaryExpression GreaterThan(Expression left, Expression right)
        {
            return GreaterThan(left, right, liftToNull: false, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a "greater than" numeric comparison. The implementing method can be specified.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="liftToNull"><see langword="true" /> to set <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to <see langword="true" />; <see langword="false" /> to set <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to <see langword="false" />.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.GreaterThan" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the "greater than" operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> property is <see langword="true" /> and the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> property is equal to <paramref name="liftToNull" />. Otherwise, they are both <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation :
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the "greater than" operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted; also, the type of the node is nullable <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="true" /> or <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="false" />:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is <see cref="bool" />.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is <see cref="bool" />.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is nullable <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="true" /> or <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="false" />.</remarks>
        public static BinaryExpression GreaterThan(Expression left, Expression right, bool liftToNull, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                return GetComparisonOperator(ExpressionType.GreaterThan, "op_GreaterThan", left, right, liftToNull);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.GreaterThan, left, right, method, liftToNull);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a "less than" numeric comparison.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.LessThan" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The "less than" operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> property is <see langword="true" />. Otherwise, it is <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> property is always <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The implementing method for the operation is chosen based on the following rules:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the "less than" operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is <see cref="bool" />:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is <see cref="bool" />.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is <see cref="bool" />.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is <see cref="bool" />.</remarks>
        /// <example>The following code example shows how to create an expression that compares two integers.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet25":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet25":::</example>
        public static BinaryExpression LessThan(Expression left, Expression right)
        {
            return LessThan(left, right, liftToNull: false, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a "less than" numeric comparison.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="liftToNull"><see langword="true" /> to set <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to <see langword="true" />; <see langword="false" /> to set <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to <see langword="false" />.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.LessThan" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the "less than" operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> property is <see langword="true" /> and the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> property is equal to <paramref name="liftToNull" />. Otherwise, they are both <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the "less than" operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted; also, the type of the node is nullable <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="true" /> or <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="false" />:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is <see cref="bool" />.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is <see cref="bool" />.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is nullable <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="true" /> or <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="false" />.</remarks>
        public static BinaryExpression LessThan(Expression left, Expression right, bool liftToNull, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                return GetComparisonOperator(ExpressionType.LessThan, "op_LessThan", left, right, liftToNull);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.LessThan, left, right, method, liftToNull);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a "greater than or equal" numeric comparison.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.GreaterThanOrEqual" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The "greater than or equal" operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> property is <see langword="true" />. Otherwise, it is <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> property is always <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the "greater than or equal" operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is <see cref="bool" />:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is <see cref="bool" />.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is <see cref="bool" />.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is <see cref="bool" />.</remarks>
        /// <example>The following code example shows how to create an expression that compares two integers.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet11":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet11":::</example>
        public static BinaryExpression GreaterThanOrEqual(Expression left, Expression right)
        {
            return GreaterThanOrEqual(left, right, liftToNull: false, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a "greater than or equal" numeric comparison.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="liftToNull"><see langword="true" /> to set <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to <see langword="true" />; <see langword="false" /> to set <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to <see langword="false" />.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.GreaterThanOrEqual" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the "greater than or equal" operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> property is <see langword="true" /> and the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> property is equal to <paramref name="liftToNull" />. Otherwise, they are both <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the "greater than or equal" operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted; also, the type of the node is nullable <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="true" /> or <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="false" />:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is <see cref="bool" />.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is <see cref="bool" />.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is nullable <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="true" /> or <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="false" />.</remarks>
        public static BinaryExpression GreaterThanOrEqual(Expression left, Expression right, bool liftToNull, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                return GetComparisonOperator(ExpressionType.GreaterThanOrEqual, "op_GreaterThanOrEqual", left, right, liftToNull);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.GreaterThanOrEqual, left, right, method, liftToNull);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a " less than or equal" numeric comparison.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.LessThanOrEqual" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The "less than or equal" operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> property is <see langword="true" />. Otherwise, it is <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> property is always <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the "less than or equal" operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is <see cref="bool" />:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is <see cref="bool" />.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is <see cref="bool" />.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is <see cref="bool" />.</remarks>
        /// <example>The following code example shows how to create an expression that compares two integers.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet26":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet26":::</example>
        public static BinaryExpression LessThanOrEqual(Expression left, Expression right)
        {
            return LessThanOrEqual(left, right, liftToNull: false, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a "less than or equal" numeric comparison.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="liftToNull"><see langword="true" /> to set <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to <see langword="true" />; <see langword="false" /> to set <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to <see langword="false" />.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.LessThanOrEqual" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the "less than or equal" operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> property is <see langword="true" /> and the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> property is equal to <paramref name="liftToNull" />. Otherwise, they are both <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the "less than or equal" operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted; also, the type of the node is nullable <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="true" /> or <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="false" />:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is <see cref="bool" />.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is <see cref="bool" />.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is nullable <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="true" /> or <see cref="bool" /> if <paramref name="liftToNull" /> is <see langword="false" />.</remarks>
        public static BinaryExpression LessThanOrEqual(Expression left, Expression right, bool liftToNull, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                return GetComparisonOperator(ExpressionType.LessThanOrEqual, "op_LessThanOrEqual", left, right, liftToNull);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.LessThanOrEqual, left, right, method, liftToNull);
        }

        private static BinaryExpression GetComparisonOperator(ExpressionType binaryType, string opName, Expression left, Expression right, bool liftToNull)
        {
            if (left.Type == right.Type && left.Type.IsNumeric())
            {
                if (left.Type.IsNullableType() && liftToNull)
                {
                    return new SimpleBinaryExpression(binaryType, left, right, typeof(bool?));
                }
                else
                {
                    return new LogicalBinaryExpression(binaryType, left, right);
                }
            }
            return GetUserDefinedBinaryOperatorOrThrow(binaryType, opName, left, right, liftToNull);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a conditional <see langword="AND" /> operation that evaluates the second operand only if the first operand evaluates to <see langword="true" />.</summary>
        /// <param name="left">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.AndAlso" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The bitwise <see langword="AND" /> operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.
        /// -or-
        /// <paramref name="left" />.Type and <paramref name="right" />.Type are not the same Boolean type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the bitwise `AND` operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  The conditional `AND` operator cannot be overloaded in C# or Visual Basic. However, the conditional `AND` operator is evaluated by using the bitwise `AND` operator. Thus, a user-defined overload of the bitwise `AND` operator can be the implementing method for this node type.
        /// ]]></format>
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are Boolean types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable, and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are the same Boolean type.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are non-nullable, the node is not lifted. The type of the node is the result type of the predefined conditional `AND` operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined conditional `AND` operator.</remarks>
        /// <example>The following code example shows how to create an expression that performs a logical AND operation on its two operands only if the first operand evaluates to <see langword="true" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet19":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet19":::</example>
        public static BinaryExpression AndAlso(Expression left, Expression right)
        {
            return AndAlso(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a conditional <see langword="AND" /> operation that evaluates the second operand only if the first operand is resolved to true. The implementing method can be specified.</summary>
        /// <param name="left">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.AndAlso" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the bitwise <see langword="AND" /> operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.
        /// -or-
        /// <paramref name="method" /> is <see langword="null" /> and <paramref name="left" />.Type and <paramref name="right" />.Type are not the same Boolean type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The implementing method for the operation is chosen based on the following rules:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method for the node.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the bitwise `AND` operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  The conditional `AND` operator cannot be overloaded in C# or Visual Basic. However, the conditional `AND` operator is evaluated by using the bitwise `AND` operator. Thus, a user-defined overload of the bitwise `AND` operator can be the implementing method for this node type.
        /// ]]></format>
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are Boolean types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable, and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are the same Boolean type.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are non-nullable, the node is not lifted. The type of the node is the result type of the predefined conditional `AND` operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined conditional `AND` operator.</remarks>
        public static BinaryExpression AndAlso(Expression left, Expression right, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            Type returnType;
            if (method == null)
            {
                if (left.Type == right.Type)
                {
                    if (left.Type == typeof(bool))
                    {
                        return new LogicalBinaryExpression(ExpressionType.AndAlso, left, right);
                    }
                    else if (left.Type == typeof(bool?))
                    {
                        return new SimpleBinaryExpression(ExpressionType.AndAlso, left, right, left.Type);
                    }
                }
                method = GetUserDefinedBinaryOperator(ExpressionType.AndAlso, left.Type, right.Type, "op_BitwiseAnd");
                if (method != null)
                {
                    ValidateUserDefinedConditionalLogicOperator(ExpressionType.AndAlso, left.Type, right.Type, method);
                    returnType = (left.Type.IsNullableType() && TypeUtils.AreEquivalent(method.ReturnType, left.Type.GetNonNullableType())) ? left.Type : method.ReturnType;
                    return new MethodBinaryExpression(ExpressionType.AndAlso, left, right, returnType, method);
                }
                throw Error.BinaryOperatorNotDefined(ExpressionType.AndAlso, left.Type, right.Type);
            }
            ValidateUserDefinedConditionalLogicOperator(ExpressionType.AndAlso, left.Type, right.Type, method);
            returnType = (left.Type.IsNullableType() && TypeUtils.AreEquivalent(method.ReturnType, left.Type.GetNonNullableType())) ? left.Type : method.ReturnType;
            return new MethodBinaryExpression(ExpressionType.AndAlso, left, right, returnType, method);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a conditional <see langword="OR" /> operation that evaluates the second operand only if the first operand evaluates to <see langword="false" />.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.OrElse" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The bitwise <see langword="OR" /> operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.
        /// -or-
        /// <paramref name="left" />.Type and <paramref name="right" />.Type are not the same Boolean type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the bitwise `OR` operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  The conditional `OR` operator cannot be overloaded in C# or Visual Basic. However, the conditional `OR` operator is evaluated by using the bitwise `OR` operator. Thus, a user-defined overload of the bitwise `OR` operator can be the implementing method for this node type.
        /// ]]></format>
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are Boolean types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable, and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are the same Boolean type.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are non-nullable, the node is not lifted. The type of the node is the result type of the predefined conditional `OR` operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined conditional `OR` operator.</remarks>
        /// <example>The following code example shows how to create an expression that represents a logical `OR` operation that evaluates the second operand only if the first operand evaluates to <see langword="false" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet29":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet29":::</example>
        public static BinaryExpression OrElse(Expression left, Expression right)
        {
            return OrElse(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a conditional <see langword="OR" /> operation that evaluates the second operand only if the first operand evaluates to <see langword="false" />.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.OrElse" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the bitwise <see langword="OR" /> operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.
        /// -or-
        /// <paramref name="method" /> is <see langword="null" /> and <paramref name="left" />.Type and <paramref name="right" />.Type are not the same Boolean type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method for the node.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the bitwise `OR` operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  The conditional `OR` operator cannot be overloaded in C# or Visual Basic. However, the conditional `OR` operator is evaluated by using the bitwise `OR` operator. Thus, a user-defined overload of the bitwise `OR` operator can be the implementing method for this node type.
        /// ]]></format>
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are Boolean types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable, and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are the same Boolean type.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are non-nullable, the node is not lifted. The type of the node is the result type of the predefined conditional `OR` operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined conditional `OR` operator.</remarks>
        public static BinaryExpression OrElse(Expression left, Expression right, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            Type returnType;
            if (method == null)
            {
                if (left.Type == right.Type)
                {
                    if (left.Type == typeof(bool))
                    {
                        return new LogicalBinaryExpression(ExpressionType.OrElse, left, right);
                    }
                    else if (left.Type == typeof(bool?))
                    {
                        return new SimpleBinaryExpression(ExpressionType.OrElse, left, right, left.Type);
                    }
                }
                method = GetUserDefinedBinaryOperator(ExpressionType.OrElse, left.Type, right.Type, "op_BitwiseOr");
                if (method != null)
                {
                    ValidateUserDefinedConditionalLogicOperator(ExpressionType.OrElse, left.Type, right.Type, method);
                    returnType = (left.Type.IsNullableType() && method.ReturnType == left.Type.GetNonNullableType()) ? left.Type : method.ReturnType;
                    return new MethodBinaryExpression(ExpressionType.OrElse, left, right, returnType, method);
                }
                throw Error.BinaryOperatorNotDefined(ExpressionType.OrElse, left.Type, right.Type);
            }
            ValidateUserDefinedConditionalLogicOperator(ExpressionType.OrElse, left.Type, right.Type, method);
            returnType = (left.Type.IsNullableType() && method.ReturnType == left.Type.GetNonNullableType()) ? left.Type : method.ReturnType;
            return new MethodBinaryExpression(ExpressionType.OrElse, left, right, returnType, method);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a coalescing operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Coalesce" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The <see cref="System.Linq.Expressions.Expression.Type" /> property of <paramref name="left" /> does not represent a reference type or a nullable value type.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="left" />.Type and <paramref name="right" />.Type are not convertible to each other.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property of the resulting <see cref="System.Linq.Expressions.BinaryExpression" /> is <see langword="null" /> and both <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> are set to <see langword="false" />. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is equal to the result type of the coalescing operation. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// #### Result Type
        /// The following rules determine the result type:
        /// -   If <paramref name="left" />.Type represents a nullable type and <paramref name="right" />.Type is implicitly convertible to the corresponding non-nullable type, the result type is the non-nullable equivalent of <paramref name="left" />.Type.
        /// -   Otherwise, if <paramref name="right" />.Type is implicitly convertible to <paramref name="left" />.Type, the result type is <paramref name="left" />.Type.
        /// -   Otherwise, if the non-nullable equivalent of <paramref name="left" />.Type is implicitly convertible to <paramref name="right" />.Type, the result type is <paramref name="right" />.Type.</remarks>
        /// <related type="Article" href="/dotnet/csharp/language-reference/operators/null-coalescing-operator">?? Operator (C# Reference)</related>
        public static BinaryExpression Coalesce(Expression left, Expression right)
        {
            return Coalesce(left, right, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a coalescing operation, given a conversion function.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="conversion">A <see cref="System.Linq.Expressions.LambdaExpression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Coalesce" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" /> and <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="left" />.Type and <paramref name="right" />.Type are not convertible to each other.
        /// -or-
        /// <paramref name="conversion" /> is not <see langword="null" /> and <paramref name="conversion" />.Type is a delegate type that does not take exactly one argument.</exception>
        /// <exception cref="System.InvalidOperationException">The <see cref="System.Linq.Expressions.Expression.Type" /> property of <paramref name="left" /> does not represent a reference type or a nullable value type.
        /// -or-
        /// The <see cref="System.Linq.Expressions.Expression.Type" /> property of <paramref name="left" /> represents a type that is not assignable to the parameter type of the delegate type <paramref name="conversion" />.Type.
        /// -or-
        /// The <see cref="System.Linq.Expressions.Expression.Type" /> property of <paramref name="right" /> is not equal to the return type of the delegate type <paramref name="conversion" />.Type.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property of the resulting <see cref="System.Linq.Expressions.BinaryExpression" /> is <see langword="null" /> and both <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> are set to <see langword="false" />.
        /// The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.BinaryExpression" /> is equal to the result type of the coalescing operation.
        /// The following rules determine the result type:
        /// -   If <paramref name="left" />.Type represents a nullable type and <paramref name="right" />.Type is implicitly convertible to the corresponding non-nullable type, the result type is the non-nullable equivalent of <paramref name="left" />.Type.
        /// -   Otherwise, if <paramref name="right" />.Type is implicitly convertible to <paramref name="left" />.Type, the result type is <paramref name="left" />.Type.
        /// -   Otherwise, if the non-nullable equivalent of <paramref name="left" />.Type is implicitly convertible to <paramref name="right" />.Type, the result type is <paramref name="right" />.Type.</remarks>
        public static BinaryExpression Coalesce(Expression left, Expression right, LambdaExpression? conversion)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));

            if (conversion == null)
            {
                Type resultType = ValidateCoalesceArgTypes(left.Type, right.Type);
                return new SimpleBinaryExpression(ExpressionType.Coalesce, left, right, resultType);
            }

            if (left.Type.IsValueType && !left.Type.IsNullableType())
            {
                throw Error.CoalesceUsedOnNonNullType();
            }

            Type delegateType = conversion.Type;
            Debug.Assert(typeof(System.MulticastDelegate).IsAssignableFrom(delegateType) && delegateType != typeof(System.MulticastDelegate));
            MethodInfo method = delegateType.GetInvokeMethod();
            if (method.ReturnType == typeof(void))
            {
                throw Error.UserDefinedOperatorMustNotBeVoid(conversion, nameof(conversion));
            }
            ParameterInfo[] pms = method.GetParametersCached();
            Debug.Assert(pms.Length == conversion.ParameterCount);
            if (pms.Length != 1)
            {
                throw Error.IncorrectNumberOfMethodCallArguments(conversion, nameof(conversion));
            }
            // The return type must match exactly.
            // We could weaken this restriction and
            // say that the return type must be assignable to from
            // the return type of the lambda.
            if (!TypeUtils.AreEquivalent(method.ReturnType, right.Type))
            {
                throw Error.OperandTypesDoNotMatchParameters(ExpressionType.Coalesce, conversion.ToString());
            }
            // The parameter of the conversion lambda must either be assignable
            // from the erased or unerased type of the left hand side.
            if (!ParameterIsAssignable(pms[0], left.Type.GetNonNullableType()) &&
                !ParameterIsAssignable(pms[0], left.Type))
            {
                throw Error.OperandTypesDoNotMatchParameters(ExpressionType.Coalesce, conversion.ToString());
            }
            return new CoalesceConversionBinaryExpression(left, right, conversion);
        }

        private static Type ValidateCoalesceArgTypes(Type left, Type right)
        {
            Type leftStripped = left.GetNonNullableType();
            if (left.IsValueType && !left.IsNullableType())
            {
                throw Error.CoalesceUsedOnNonNullType();
            }
            else if (left.IsNullableType() && right.IsImplicitlyConvertibleTo(leftStripped))
            {
                return leftStripped;
            }
            else if (right.IsImplicitlyConvertibleTo(left))
            {
                return left;
            }
            else if (leftStripped.IsImplicitlyConvertibleTo(right))
            {
                return right;
            }
            else
            {
                throw Error.ArgumentTypesMustMatch();
            }
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic addition operation that does not have overflow checking.</summary>
        /// <param name="left">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Add" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The addition operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the selected implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the addition operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined addition operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined addition operator.</remarks>
        /// <example>The following code example shows how to create an expression that adds two integers.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet1":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet1":::</example>
        public static BinaryExpression Add(Expression left, Expression right)
        {
            return Add(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic addition operation that does not have overflow checking. The implementing method can be specified.</summary>
        /// <param name="left">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Add" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" /> and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the addition operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method for the node.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the addition operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined addition operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined addition operator.</remarks>
        public static BinaryExpression Add(Expression left, Expression right, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsArithmetic())
                {
                    return new SimpleBinaryExpression(ExpressionType.Add, left, right, left.Type);
                }
                return GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Add, "op_Addition", left, right, liftToNull: true);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.Add, left, right, method, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an addition assignment operation that does not have overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.AddAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <remarks></remarks>
        /// <example>The following code example shows how to create an expression that adds a value to an integer variable and then assigns the result of the operation to the variable.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet18":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet18":::</example>
        public static BinaryExpression AddAssign(Expression left, Expression right)
        {
            return AddAssign(left, right, method: null, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an addition assignment operation that does not have overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.AddAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        public static BinaryExpression AddAssign(Expression left, Expression right, MethodInfo? method)
        {
            return AddAssign(left, right, method, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an addition assignment operation that does not have overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <param name="conversion">A <see cref="System.Linq.Expressions.LambdaExpression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.AddAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.Method" />, and <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.</returns>
        public static BinaryExpression AddAssign(Expression left, Expression right, MethodInfo? method, LambdaExpression? conversion)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            RequiresCanWrite(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsArithmetic())
                {
                    // conversion is not supported for binary ops on arithmetic types without operator overloading
                    if (conversion != null)
                    {
                        throw Error.ConversionIsNotSupportedForArithmeticTypes();
                    }
                    return new SimpleBinaryExpression(ExpressionType.AddAssign, left, right, left.Type);
                }
                return GetUserDefinedAssignOperatorOrThrow(ExpressionType.AddAssign, "op_Addition", left, right, conversion, liftToNull: true);
            }
            return GetMethodBasedAssignOperator(ExpressionType.AddAssign, left, right, method, conversion, liftToNull: true);
        }

        private static void ValidateOpAssignConversionLambda(LambdaExpression conversion, Expression left, MethodInfo method, ExpressionType nodeType)
        {
            Type delegateType = conversion.Type;
            Debug.Assert(typeof(System.MulticastDelegate).IsAssignableFrom(delegateType) && delegateType != typeof(System.MulticastDelegate));
            MethodInfo mi = delegateType.GetInvokeMethod();
            ParameterInfo[] pms = mi.GetParametersCached();
            Debug.Assert(pms.Length == conversion.ParameterCount);
            if (pms.Length != 1)
            {
                throw Error.IncorrectNumberOfMethodCallArguments(conversion, nameof(conversion));
            }
            if (!TypeUtils.AreEquivalent(mi.ReturnType, left.Type))
            {
                throw Error.OperandTypesDoNotMatchParameters(nodeType, conversion.ToString());
            }
            Debug.Assert(method != null);
            // The parameter type of conversion lambda must be the same as the return type of the overload method
            if (!TypeUtils.AreEquivalent(pms[0].ParameterType, method.ReturnType))
            {
                throw Error.OverloadOperatorTypeDoesNotMatchConversionType(nodeType, conversion.ToString());
            }
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an addition assignment operation that has overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.AddAssignChecked" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        public static BinaryExpression AddAssignChecked(Expression left, Expression right)
        {
            return AddAssignChecked(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an addition assignment operation that has overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.AddAssignChecked" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        public static BinaryExpression AddAssignChecked(Expression left, Expression right, MethodInfo? method)
        {
            return AddAssignChecked(left, right, method, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an addition assignment operation that has overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <param name="conversion">A <see cref="System.Linq.Expressions.LambdaExpression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.AddAssignChecked" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.Method" />, and <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.</returns>
        public static BinaryExpression AddAssignChecked(Expression left, Expression right, MethodInfo? method, LambdaExpression? conversion)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            RequiresCanWrite(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));

            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsArithmetic())
                {
                    // conversion is not supported for binary ops on arithmetic types without operator overloading
                    if (conversion != null)
                    {
                        throw Error.ConversionIsNotSupportedForArithmeticTypes();
                    }
                    return new SimpleBinaryExpression(ExpressionType.AddAssignChecked, left, right, left.Type);
                }
                return GetUserDefinedAssignOperatorOrThrow(ExpressionType.AddAssignChecked, "op_Addition", left, right, conversion, liftToNull: true);
            }
            return GetMethodBasedAssignOperator(ExpressionType.AddAssignChecked, left, right, method, conversion, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic addition operation that has overflow checking.</summary>
        /// <param name="left">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.AddChecked" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The addition operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the addition operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined addition operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined addition operator.</remarks>
        public static BinaryExpression AddChecked(Expression left, Expression right)
        {
            return AddChecked(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic addition operation that has overflow checking. The implementing method can be specified.</summary>
        /// <param name="left">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.AddChecked" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" /> and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the addition operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The implementing method for the operation is chosen based on the following rules:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method for the node.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the addition operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined addition operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined addition operator.</remarks>
        public static BinaryExpression AddChecked(Expression left, Expression right, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsArithmetic())
                {
                    return new SimpleBinaryExpression(ExpressionType.AddChecked, left, right, left.Type);
                }
                return GetUserDefinedBinaryOperatorOrThrow(ExpressionType.AddChecked, "op_Addition", left, right, liftToNull: true);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.AddChecked, left, right, method, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic subtraction operation that does not have overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Subtract" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The subtraction operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the selected implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the subtraction operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined subtraction operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined subtraction operator.</remarks>
        /// <example>The following code example shows how to create an expression that subtracts the argument from the first argument.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet30":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet30":::</example>
        public static BinaryExpression Subtract(Expression left, Expression right)
        {
            return Subtract(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic subtraction operation that does not have overflow checking.</summary>
        /// <param name="left">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Subtract" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the subtraction operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method for the node.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the subtraction operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined subtraction operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined subtraction operator.</remarks>
        public static BinaryExpression Subtract(Expression left, Expression right, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsArithmetic())
                {
                    return new SimpleBinaryExpression(ExpressionType.Subtract, left, right, left.Type);
                }
                return GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Subtract, "op_Subtraction", left, right, liftToNull: true);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.Subtract, left, right, method, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a subtraction assignment operation that does not have overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.SubtractAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        public static BinaryExpression SubtractAssign(Expression left, Expression right)
        {
            return SubtractAssign(left, right, method: null, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a subtraction assignment operation that does not have overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.SubtractAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        public static BinaryExpression SubtractAssign(Expression left, Expression right, MethodInfo? method)
        {
            return SubtractAssign(left, right, method, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a subtraction assignment operation that does not have overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <param name="conversion">A <see cref="System.Linq.Expressions.LambdaExpression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.SubtractAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.Method" />, and <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.</returns>
        public static BinaryExpression SubtractAssign(Expression left, Expression right, MethodInfo? method, LambdaExpression? conversion)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            RequiresCanWrite(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsArithmetic())
                {
                    // conversion is not supported for binary ops on arithmetic types without operator overloading
                    if (conversion != null)
                    {
                        throw Error.ConversionIsNotSupportedForArithmeticTypes();
                    }
                    return new SimpleBinaryExpression(ExpressionType.SubtractAssign, left, right, left.Type);
                }
                return GetUserDefinedAssignOperatorOrThrow(ExpressionType.SubtractAssign, "op_Subtraction", left, right, conversion, liftToNull: true);
            }
            return GetMethodBasedAssignOperator(ExpressionType.SubtractAssign, left, right, method, conversion, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a subtraction assignment operation that has overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.SubtractAssignChecked" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        public static BinaryExpression SubtractAssignChecked(Expression left, Expression right)
        {
            return SubtractAssignChecked(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a subtraction assignment operation that has overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.SubtractAssignChecked" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        public static BinaryExpression SubtractAssignChecked(Expression left, Expression right, MethodInfo? method)
        {
            return SubtractAssignChecked(left, right, method, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a subtraction assignment operation that has overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <param name="conversion">A <see cref="System.Linq.Expressions.LambdaExpression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.SubtractAssignChecked" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.Method" />, and <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.</returns>
        public static BinaryExpression SubtractAssignChecked(Expression left, Expression right, MethodInfo? method, LambdaExpression? conversion)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            RequiresCanWrite(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsArithmetic())
                {
                    // conversion is not supported for binary ops on arithmetic types without operator overloading
                    if (conversion != null)
                    {
                        throw Error.ConversionIsNotSupportedForArithmeticTypes();
                    }
                    return new SimpleBinaryExpression(ExpressionType.SubtractAssignChecked, left, right, left.Type);
                }
                return GetUserDefinedAssignOperatorOrThrow(ExpressionType.SubtractAssignChecked, "op_Subtraction", left, right, conversion, liftToNull: true);
            }
            return GetMethodBasedAssignOperator(ExpressionType.SubtractAssignChecked, left, right, method, conversion, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic subtraction operation that has overflow checking.</summary>
        /// <param name="left">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.SubtractChecked" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The subtraction operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the selected implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the subtraction operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined subtraction operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined subtraction operator.</remarks>
        public static BinaryExpression SubtractChecked(Expression left, Expression right)
        {
            return SubtractChecked(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic subtraction operation that has overflow checking.</summary>
        /// <param name="left">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.SubtractChecked" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the subtraction operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation :
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method for the node.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the subtraction operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined subtraction operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined subtraction operator.</remarks>
        public static BinaryExpression SubtractChecked(Expression left, Expression right, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsArithmetic())
                {
                    return new SimpleBinaryExpression(ExpressionType.SubtractChecked, left, right, left.Type);
                }
                return GetUserDefinedBinaryOperatorOrThrow(ExpressionType.SubtractChecked, "op_Subtraction", left, right, liftToNull: true);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.SubtractChecked, left, right, method, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic division operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Divide" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The division operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the division operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined division operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined division operator.</remarks>
        /// <example>The following code example shows how to create an expression that divides its first argument by its second argument.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet7":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet7":::</example>
        public static BinaryExpression Divide(Expression left, Expression right)
        {
            return Divide(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic division operation. The implementing method can be specified.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Divide" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the division operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method for the node.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the division operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined division operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined division operator.</remarks>
        public static BinaryExpression Divide(Expression left, Expression right, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsArithmetic())
                {
                    return new SimpleBinaryExpression(ExpressionType.Divide, left, right, left.Type);
                }
                return GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Divide, "op_Division", left, right, liftToNull: true);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.Divide, left, right, method, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a division assignment operation that does not have overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.DivideAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        public static BinaryExpression DivideAssign(Expression left, Expression right)
        {
            return DivideAssign(left, right, method: null, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a division assignment operation that does not have overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.DivideAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        public static BinaryExpression DivideAssign(Expression left, Expression right, MethodInfo? method)
        {
            return DivideAssign(left, right, method, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a division assignment operation that does not have overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <param name="conversion">A <see cref="System.Linq.Expressions.LambdaExpression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.DivideAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.Method" />, and <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.</returns>
        public static BinaryExpression DivideAssign(Expression left, Expression right, MethodInfo? method, LambdaExpression? conversion)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            RequiresCanWrite(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsArithmetic())
                {
                    // conversion is not supported for binary ops on arithmetic types without operator overloading
                    if (conversion != null)
                    {
                        throw Error.ConversionIsNotSupportedForArithmeticTypes();
                    }
                    return new SimpleBinaryExpression(ExpressionType.DivideAssign, left, right, left.Type);
                }
                return GetUserDefinedAssignOperatorOrThrow(ExpressionType.DivideAssign, "op_Division", left, right, conversion, liftToNull: true);
            }
            return GetMethodBasedAssignOperator(ExpressionType.DivideAssign, left, right, method, conversion, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic remainder operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Modulo" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The modulus operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the selected implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the modulus operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined modulus operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined modulus operator.</remarks>
        public static BinaryExpression Modulo(Expression left, Expression right)
        {
            return Modulo(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic remainder operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Modulo" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the modulus operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The implementing method for the operation is chosen based on the following rules:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method for the node.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the modulus operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined modulus operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined modulus operator.</remarks>
        public static BinaryExpression Modulo(Expression left, Expression right, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsArithmetic())
                {
                    return new SimpleBinaryExpression(ExpressionType.Modulo, left, right, left.Type);
                }
                return GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Modulo, "op_Modulus", left, right, liftToNull: true);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.Modulo, left, right, method, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a remainder assignment operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ModuloAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        public static BinaryExpression ModuloAssign(Expression left, Expression right)
        {
            return ModuloAssign(left, right, method: null, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a remainder assignment operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ModuloAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        public static BinaryExpression ModuloAssign(Expression left, Expression right, MethodInfo? method)
        {
            return ModuloAssign(left, right, method, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a remainder assignment operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <param name="conversion">A <see cref="System.Linq.Expressions.LambdaExpression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ModuloAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.Method" />, and <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.</returns>
        public static BinaryExpression ModuloAssign(Expression left, Expression right, MethodInfo? method, LambdaExpression? conversion)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            RequiresCanWrite(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsArithmetic())
                {
                    // conversion is not supported for binary ops on arithmetic types without operator overloading
                    if (conversion != null)
                    {
                        throw Error.ConversionIsNotSupportedForArithmeticTypes();
                    }
                    return new SimpleBinaryExpression(ExpressionType.ModuloAssign, left, right, left.Type);
                }
                return GetUserDefinedAssignOperatorOrThrow(ExpressionType.ModuloAssign, "op_Modulus", left, right, conversion, liftToNull: true);
            }
            return GetMethodBasedAssignOperator(ExpressionType.ModuloAssign, left, right, method, conversion, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic multiplication operation that does not have overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Multiply" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The multiplication operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the selected implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the multiplication operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined multiplication operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined multiplication operator.</remarks>
        /// <example>The following code example shows how to create an expression that multiplies two values.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet27":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet27":::</example>
        public static BinaryExpression Multiply(Expression left, Expression right)
        {
            return Multiply(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic multiplication operation that does not have overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Multiply" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the multiplication operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method for the node.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the multiplication operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined multiplication operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined multiplication operator.</remarks>
        public static BinaryExpression Multiply(Expression left, Expression right, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsArithmetic())
                {
                    return new SimpleBinaryExpression(ExpressionType.Multiply, left, right, left.Type);
                }
                return GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Multiply, "op_Multiply", left, right, liftToNull: true);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.Multiply, left, right, method, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a multiplication assignment operation that does not have overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.MultiplyAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        public static BinaryExpression MultiplyAssign(Expression left, Expression right)
        {
            return MultiplyAssign(left, right, method: null, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a multiplication assignment operation that does not have overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.MultiplyAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        public static BinaryExpression MultiplyAssign(Expression left, Expression right, MethodInfo? method)
        {
            return MultiplyAssign(left, right, method, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a multiplication assignment operation that does not have overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <param name="conversion">A <see cref="System.Linq.Expressions.LambdaExpression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.MultiplyAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.Method" />, and <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.</returns>
        public static BinaryExpression MultiplyAssign(Expression left, Expression right, MethodInfo? method, LambdaExpression? conversion)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            RequiresCanWrite(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsArithmetic())
                {
                    // conversion is not supported for binary ops on arithmetic types without operator overloading
                    if (conversion != null)
                    {
                        throw Error.ConversionIsNotSupportedForArithmeticTypes();
                    }
                    return new SimpleBinaryExpression(ExpressionType.MultiplyAssign, left, right, left.Type);
                }
                return GetUserDefinedAssignOperatorOrThrow(ExpressionType.MultiplyAssign, "op_Multiply", left, right, conversion, liftToNull: true);
            }
            return GetMethodBasedAssignOperator(ExpressionType.MultiplyAssign, left, right, method, conversion, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a multiplication assignment operation that has overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.MultiplyAssignChecked" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        public static BinaryExpression MultiplyAssignChecked(Expression left, Expression right)
        {
            return MultiplyAssignChecked(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a multiplication assignment operation that has overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.MultiplyAssignChecked" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        public static BinaryExpression MultiplyAssignChecked(Expression left, Expression right, MethodInfo? method)
        {
            return MultiplyAssignChecked(left, right, method, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a multiplication assignment operation that has overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <param name="conversion">A <see cref="System.Linq.Expressions.LambdaExpression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.MultiplyAssignChecked" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.Method" />, and <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.</returns>
        public static BinaryExpression MultiplyAssignChecked(Expression left, Expression right, MethodInfo? method, LambdaExpression? conversion)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            RequiresCanWrite(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsArithmetic())
                {
                    // conversion is not supported for binary ops on arithmetic types without operator overloading
                    if (conversion != null)
                    {
                        throw Error.ConversionIsNotSupportedForArithmeticTypes();
                    }
                    return new SimpleBinaryExpression(ExpressionType.MultiplyAssignChecked, left, right, left.Type);
                }
                return GetUserDefinedAssignOperatorOrThrow(ExpressionType.MultiplyAssignChecked, "op_Multiply", left, right, conversion, liftToNull: true);
            }
            return GetMethodBasedAssignOperator(ExpressionType.MultiplyAssignChecked, left, right, method, conversion, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic multiplication operation that has overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.MultiplyChecked" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The multiplication operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the selected implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the multiplication operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined multiplication operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined multiplication operator.</remarks>
        public static BinaryExpression MultiplyChecked(Expression left, Expression right)
        {
            return MultiplyChecked(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic multiplication operation that has overflow checking.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.MultiplyChecked" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the multiplication operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method for the node.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the multiplication operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are numeric types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined multiplication operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined multiplication operator.</remarks>
        public static BinaryExpression MultiplyChecked(Expression left, Expression right, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsArithmetic())
                {
                    return new SimpleBinaryExpression(ExpressionType.MultiplyChecked, left, right, left.Type);
                }
                return GetUserDefinedBinaryOperatorOrThrow(ExpressionType.MultiplyChecked, "op_Multiply", left, right, liftToNull: true);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.MultiplyChecked, left, right, method, liftToNull: true);
        }

        private static bool IsSimpleShift(Type left, Type right)
        {
            return left.IsInteger()
                && right.GetNonNullableType() == typeof(int);
        }

        private static Type GetResultTypeOfShift(Type left, Type right)
        {
            if (!left.IsNullableType() && right.IsNullableType())
            {
                // lift the result type to Nullable<T>
                return typeof(Nullable<>).MakeGenericType(left);
            }
            return left;
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise left-shift operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.LeftShift" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The left-shift operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the selected implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the left-shift operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type is an integral type (one of <see cref="byte" />, <see cref="sbyte" />, <see cref="short" />, <see cref="ushort" />, <see cref="int" />, <see cref="uint" />, <see cref="long" />, <see cref="ulong" />, or the corresponding nullable types) and <paramref name="right" />.Type is <see cref="int" />, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined left-shift operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined left-shift operator.</remarks>
        public static BinaryExpression LeftShift(Expression left, Expression right)
        {
            return LeftShift(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise left-shift operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.LeftShift" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the left-shift operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the selected implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method for the node.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the left-shift operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type is an integral type (one of <see cref="byte" />, <see cref="sbyte" />, <see cref="short" />, <see cref="ushort" />, <see cref="int" />, <see cref="uint" />, <see cref="long" />, <see cref="ulong" />, or the corresponding nullable types) and <paramref name="right" />.Type is <see cref="int" />, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined left-shift operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined left-shift operator.</remarks>
        public static BinaryExpression LeftShift(Expression left, Expression right, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (IsSimpleShift(left.Type, right.Type))
                {
                    Type resultType = GetResultTypeOfShift(left.Type, right.Type);
                    return new SimpleBinaryExpression(ExpressionType.LeftShift, left, right, resultType);
                }
                return GetUserDefinedBinaryOperatorOrThrow(ExpressionType.LeftShift, "op_LeftShift", left, right, liftToNull: true);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.LeftShift, left, right, method, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise left-shift assignment operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.LeftShiftAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        public static BinaryExpression LeftShiftAssign(Expression left, Expression right)
        {
            return LeftShiftAssign(left, right, method: null, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise left-shift assignment operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.LeftShiftAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        public static BinaryExpression LeftShiftAssign(Expression left, Expression right, MethodInfo? method)
        {
            return LeftShiftAssign(left, right, method, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise left-shift assignment operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <param name="conversion">A <see cref="System.Linq.Expressions.LambdaExpression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.LeftShiftAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.Method" />, and <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.</returns>
        public static BinaryExpression LeftShiftAssign(Expression left, Expression right, MethodInfo? method, LambdaExpression? conversion)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            RequiresCanWrite(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (IsSimpleShift(left.Type, right.Type))
                {
                    // conversion is not supported for binary ops on arithmetic types without operator overloading
                    if (conversion != null)
                    {
                        throw Error.ConversionIsNotSupportedForArithmeticTypes();
                    }
                    Type resultType = GetResultTypeOfShift(left.Type, right.Type);
                    return new SimpleBinaryExpression(ExpressionType.LeftShiftAssign, left, right, resultType);
                }
                return GetUserDefinedAssignOperatorOrThrow(ExpressionType.LeftShiftAssign, "op_LeftShift", left, right, conversion, liftToNull: true);
            }
            return GetMethodBasedAssignOperator(ExpressionType.LeftShiftAssign, left, right, method, conversion, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise right-shift operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.RightShift" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The right-shift operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the selected implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the right-shift operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type is an integral type (one of <see cref="byte" />, <see cref="sbyte" />, <see cref="short" />, <see cref="ushort" />, <see cref="int" />, <see cref="uint" />, <see cref="long" />, <see cref="ulong" />, or the corresponding nullable types) and <paramref name="right" />.Type is <see cref="int" />, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined right-shift operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined right-shift operator.</remarks>
        public static BinaryExpression RightShift(Expression left, Expression right)
        {
            return RightShift(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise right-shift operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.RightShift" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the right-shift operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the selected implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method for the node.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the right-shift operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type is an integral type (one of <see cref="byte" />, <see cref="sbyte" />, <see cref="short" />, <see cref="ushort" />, <see cref="int" />, <see cref="uint" />, <see cref="long" />, <see cref="ulong" />, or the corresponding nullable types) and <paramref name="right" />.Type is <see cref="int" />, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined right-shift operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined right-shift operator.</remarks>
        public static BinaryExpression RightShift(Expression left, Expression right, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (IsSimpleShift(left.Type, right.Type))
                {
                    Type resultType = GetResultTypeOfShift(left.Type, right.Type);
                    return new SimpleBinaryExpression(ExpressionType.RightShift, left, right, resultType);
                }
                return GetUserDefinedBinaryOperatorOrThrow(ExpressionType.RightShift, "op_RightShift", left, right, liftToNull: true);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.RightShift, left, right, method, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise right-shift assignment operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.RightShiftAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        public static BinaryExpression RightShiftAssign(Expression left, Expression right)
        {
            return RightShiftAssign(left, right, method: null, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise right-shift assignment operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.RightShiftAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        public static BinaryExpression RightShiftAssign(Expression left, Expression right, MethodInfo? method)
        {
            return RightShiftAssign(left, right, method, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise right-shift assignment operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <param name="conversion">A <see cref="System.Linq.Expressions.LambdaExpression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.RightShiftAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.Method" />, and <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.</returns>
        public static BinaryExpression RightShiftAssign(Expression left, Expression right, MethodInfo? method, LambdaExpression? conversion)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            RequiresCanWrite(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (IsSimpleShift(left.Type, right.Type))
                {
                    // conversion is not supported for binary ops on arithmetic types without operator overloading
                    if (conversion != null)
                    {
                        throw Error.ConversionIsNotSupportedForArithmeticTypes();
                    }
                    Type resultType = GetResultTypeOfShift(left.Type, right.Type);
                    return new SimpleBinaryExpression(ExpressionType.RightShiftAssign, left, right, resultType);
                }
                return GetUserDefinedAssignOperatorOrThrow(ExpressionType.RightShiftAssign, "op_RightShift", left, right, conversion, liftToNull: true);
            }
            return GetMethodBasedAssignOperator(ExpressionType.RightShiftAssign, left, right, method, conversion, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise <see langword="AND" /> operation.</summary>
        /// <param name="left">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.And" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The bitwise <see langword="AND" /> operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the bitwise `AND` operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are integral or Boolean types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined bitwise `AND` operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined bitwise `AND` operator.</remarks>
        /// <example>The following code example shows how to create an expression that represents a logical AND operation on two Boolean values.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet2":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet2":::</example>
        public static BinaryExpression And(Expression left, Expression right)
        {
            return And(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise <see langword="AND" /> operation. The implementing method can be specified.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.And" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the bitwise <see langword="AND" /> operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The implementing method for the operation is chosen based on the following rules:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method for the node.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the bitwise `AND` operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are integral or Boolean types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined bitwise `AND` operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined bitwise `AND` operator.</remarks>
        public static BinaryExpression And(Expression left, Expression right, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsIntegerOrBool())
                {
                    return new SimpleBinaryExpression(ExpressionType.And, left, right, left.Type);
                }
                return GetUserDefinedBinaryOperatorOrThrow(ExpressionType.And, "op_BitwiseAnd", left, right, liftToNull: true);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.And, left, right, method, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise AND assignment operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.AndAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        public static BinaryExpression AndAssign(Expression left, Expression right)
        {
            return AndAssign(left, right, method: null, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise AND assignment operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.AndAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        public static BinaryExpression AndAssign(Expression left, Expression right, MethodInfo? method)
        {
            return AndAssign(left, right, method, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise AND assignment operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <param name="conversion">A <see cref="System.Linq.Expressions.LambdaExpression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.AndAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.Method" />, and <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.</returns>
        public static BinaryExpression AndAssign(Expression left, Expression right, MethodInfo? method, LambdaExpression? conversion)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            RequiresCanWrite(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsIntegerOrBool())
                {
                    // conversion is not supported for binary ops on arithmetic types without operator overloading
                    if (conversion != null)
                    {
                        throw Error.ConversionIsNotSupportedForArithmeticTypes();
                    }
                    return new SimpleBinaryExpression(ExpressionType.AndAssign, left, right, left.Type);
                }
                return GetUserDefinedAssignOperatorOrThrow(ExpressionType.AndAssign, "op_BitwiseAnd", left, right, conversion, liftToNull: true);
            }
            return GetMethodBasedAssignOperator(ExpressionType.AndAssign, left, right, method, conversion, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise <see langword="OR" /> operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Or" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The bitwise <see langword="OR" /> operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the bitwise `OR` operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are integral or Boolean types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined bitwise `OR` operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined bitwise `OR` operator.</remarks>
        /// <example>The following code example shows how to create an expression that represents a logical OR operation.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet28":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet28":::</example>
        public static BinaryExpression Or(Expression left, Expression right)
        {
            return Or(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise <see langword="OR" /> operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Or" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the bitwise <see langword="OR" /> operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the bitwise `OR` operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are integral or Boolean types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined bitwise `OR` operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined bitwise `OR` operator.</remarks>
        public static BinaryExpression Or(Expression left, Expression right, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsIntegerOrBool())
                {
                    return new SimpleBinaryExpression(ExpressionType.Or, left, right, left.Type);
                }
                return GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Or, "op_BitwiseOr", left, right, liftToNull: true);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.Or, left, right, method, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise OR assignment operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.OrAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        public static BinaryExpression OrAssign(Expression left, Expression right)
        {
            return OrAssign(left, right, method: null, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise OR assignment operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.OrAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        public static BinaryExpression OrAssign(Expression left, Expression right, MethodInfo? method)
        {
            return OrAssign(left, right, method, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise OR assignment operation.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <param name="conversion">A <see cref="System.Linq.Expressions.LambdaExpression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.OrAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.Method" />, and <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.</returns>
        public static BinaryExpression OrAssign(Expression left, Expression right, MethodInfo? method, LambdaExpression? conversion)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            RequiresCanWrite(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsIntegerOrBool())
                {
                    // conversion is not supported for binary ops on arithmetic types without operator overloading
                    if (conversion != null)
                    {
                        throw Error.ConversionIsNotSupportedForArithmeticTypes();
                    }
                    return new SimpleBinaryExpression(ExpressionType.OrAssign, left, right, left.Type);
                }
                return GetUserDefinedAssignOperatorOrThrow(ExpressionType.OrAssign, "op_BitwiseOr", left, right, conversion, liftToNull: true);
            }
            return GetMethodBasedAssignOperator(ExpressionType.OrAssign, left, right, method, conversion, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise <see langword="XOR" /> operation, using <c>op_ExclusiveOr</c> for user-defined types.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ExclusiveOr" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The <see langword="XOR" /> operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the `XOR` operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are integral or Boolean types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined `XOR` operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined `XOR` operator.</remarks>
        /// <example>The following code example shows how to create an expression that represents the logical XOR operation.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet9":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet9":::</example>
        public static BinaryExpression ExclusiveOr(Expression left, Expression right)
        {
            return ExclusiveOr(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise <see langword="XOR" /> operation, using <c>op_ExclusiveOr</c> for user-defined types. The implementing method can be specified.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ExclusiveOr" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the <see langword="XOR" /> operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the chosen implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the `XOR` operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are integral or Boolean types, the implementing method is <see langword="null" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// If the implementing method is not <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.
        /// If the implementing method is <see langword="null" />:
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both non-nullable, the node is not lifted. The type of the node is the result type of the predefined `XOR` operator.
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are both nullable, the node is lifted. The type of the node is the nullable type that corresponds to the result type of the predefined `XOR` operator.</remarks>
        public static BinaryExpression ExclusiveOr(Expression left, Expression right, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsIntegerOrBool())
                {
                    return new SimpleBinaryExpression(ExpressionType.ExclusiveOr, left, right, left.Type);
                }
                return GetUserDefinedBinaryOperatorOrThrow(ExpressionType.ExclusiveOr, "op_ExclusiveOr", left, right, liftToNull: true);
            }
            return GetMethodBasedBinaryOperator(ExpressionType.ExclusiveOr, left, right, method, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise XOR assignment operation, using <c>op_ExclusiveOr</c> for user-defined types.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ExclusiveOrAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        public static BinaryExpression ExclusiveOrAssign(Expression left, Expression right)
        {
            return ExclusiveOrAssign(left, right, method: null, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise XOR assignment operation, using <c>op_ExclusiveOr</c> for user-defined types.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ExclusiveOrAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        public static BinaryExpression ExclusiveOrAssign(Expression left, Expression right, MethodInfo? method)
        {
            return ExclusiveOrAssign(left, right, method, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents a bitwise XOR assignment operation, using <c>op_ExclusiveOr</c> for user-defined types.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <param name="conversion">A <see cref="System.Linq.Expressions.LambdaExpression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ExclusiveOrAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.Method" />, and <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.</returns>
        public static BinaryExpression ExclusiveOrAssign(Expression left, Expression right, MethodInfo? method, LambdaExpression? conversion)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            RequiresCanWrite(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsIntegerOrBool())
                {
                    // conversion is not supported for binary ops on arithmetic types without operator overloading
                    if (conversion != null)
                    {
                        throw Error.ConversionIsNotSupportedForArithmeticTypes();
                    }
                    return new SimpleBinaryExpression(ExpressionType.ExclusiveOrAssign, left, right, left.Type);
                }
                return GetUserDefinedAssignOperatorOrThrow(ExpressionType.ExclusiveOrAssign, "op_ExclusiveOr", left, right, conversion, liftToNull: true);
            }
            return GetMethodBasedAssignOperator(ExpressionType.ExclusiveOrAssign, left, right, method, conversion, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents raising a number to a power.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Power" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The exponentiation operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.
        /// -or-
        /// <paramref name="left" />.Type and/or <paramref name="right" />.Type are not <see cref="double" />.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the exponentiation operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are both <see cref="double" />, the implementing method is <see cref="O:System.Math.Pow" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.</remarks>
        public static BinaryExpression Power(Expression left, Expression right)
        {
            return Power(left, right, method: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents raising a number to a power.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Power" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="left" /> or <paramref name="right" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="method" /> is not <see langword="null" /> and the method it represents returns <see langword="void" />, is not <see langword="static" /> (<see langword="Shared" /> in Visual Basic), or does not take exactly two arguments.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="method" /> is <see langword="null" /> and the exponentiation operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.
        /// -or-
        /// <paramref name="method" /> is <see langword="null" /> and <paramref name="left" />.Type and/or <paramref name="right" />.Type are not <see cref="double" />.</exception>
        /// <remarks>The resulting <see cref="System.Linq.Expressions.BinaryExpression" /> has the <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property set to the implementing method. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is set to the type of the node. If the node is lifted, the <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> properties are both <see langword="true" />. Otherwise, they are <see langword="false" />. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.
        /// The following information describes the implementing method, the node type, and whether a node is lifted.
        /// #### Implementing Method
        /// The following rules determine the implementing method for the operation:
        /// -   If <paramref name="method" /> is not <see langword="null" /> and it represents a non-void, <see langword="static" /> (`Shared` in Visual Basic) method that takes two arguments, it is the implementing method.
        /// -   Otherwise, if the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of either <paramref name="left" /> or <paramref name="right" /> represents a user-defined type that overloads the exponentiation operator, the <see cref="System.Reflection.MethodInfo" /> that represents that method is the implementing method.
        /// -   Otherwise, if <paramref name="left" />.Type and <paramref name="right" />.Type are both <see cref="double" />, the implementing method is <see cref="O:System.Math.Pow" />.
        /// #### Node Type and Lifted versus Non-Lifted
        /// -   If <paramref name="left" />.Type and <paramref name="right" />.Type are assignable to the corresponding argument types of the implementing method, the node is not lifted. The type of the node is the return type of the implementing method.
        /// -   If the following two conditions are satisfied, the node is lifted and the type of the node is the nullable type that corresponds to the return type of the implementing method:
        /// -   <paramref name="left" />.Type and <paramref name="right" />.Type are both value types of which at least one is nullable and the corresponding non-nullable types are equal to the corresponding argument types of the implementing method.
        /// -   The return type of the implementing method is a non-nullable value type.</remarks>
        public static BinaryExpression Power(Expression left, Expression right, MethodInfo? method)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                if (left.Type == right.Type && left.Type.IsArithmetic())
                {
                    method = Math_Pow_Double_Double;
                    Debug.Assert(method != null);
                }
                else
                {
                    // VB uses op_Exponent, F# uses op_Exponentiation. This inconsistency is unfortunate, but we can
                    // test for either.
                    string name = "op_Exponent";
                    BinaryExpression? b = GetUserDefinedBinaryOperator(ExpressionType.Power, name, left, right, liftToNull: true);
                    if (b == null)
                    {
                        name = "op_Exponentiation";
                        b = GetUserDefinedBinaryOperator(ExpressionType.Power, name, left, right, liftToNull: true);
                        if (b == null)
                        {
                            throw Error.BinaryOperatorNotDefined(ExpressionType.Power, left.Type, right.Type);
                        }
                    }

                    ParameterInfo[] pis = b.Method!.GetParametersCached();
                    ValidateParamswithOperandsOrThrow(pis[0].ParameterType, left.Type, ExpressionType.Power, name);
                    ValidateParamswithOperandsOrThrow(pis[1].ParameterType, right.Type, ExpressionType.Power, name);
                    return b;
                }
            }

            return GetMethodBasedBinaryOperator(ExpressionType.Power, left, right, method, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents raising an expression to a power and assigning the result back to the expression.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.PowerAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        public static BinaryExpression PowerAssign(Expression left, Expression right)
        {
            return PowerAssign(left, right, method: null, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents raising an expression to a power and assigning the result back to the expression.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.PowerAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
        public static BinaryExpression PowerAssign(Expression left, Expression right, MethodInfo? method)
        {
            return PowerAssign(left, right, method, conversion: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents raising an expression to a power and assigning the result back to the expression.</summary>
        /// <param name="left">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="right">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <param name="method">A <see cref="System.Reflection.MethodInfo" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
        /// <param name="conversion">A <see cref="System.Linq.Expressions.LambdaExpression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.PowerAssign" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" />, <see cref="System.Linq.Expressions.BinaryExpression.Right" />, <see cref="System.Linq.Expressions.BinaryExpression.Method" />, and <see cref="System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.</returns>
        public static BinaryExpression PowerAssign(Expression left, Expression right, MethodInfo? method, LambdaExpression? conversion)
        {
            ExpressionUtils.RequiresCanRead(left, nameof(left));
            RequiresCanWrite(left, nameof(left));
            ExpressionUtils.RequiresCanRead(right, nameof(right));
            if (method == null)
            {
                method = Math_Pow_Double_Double;
                if (method == null)
                {
                    throw Error.BinaryOperatorNotDefined(ExpressionType.PowerAssign, left.Type, right.Type);
                }
            }
            return GetMethodBasedAssignOperator(ExpressionType.PowerAssign, left, right, method, conversion, liftToNull: true);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.BinaryExpression" /> that represents applying an array index operator to an array of rank one.</summary>
        /// <param name="array">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
        /// <param name="index">A <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.BinaryExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.ArrayIndex" /> and the <see cref="System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="array" /> or <paramref name="index" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="array" />.Type does not represent an array type.
        /// -or-
        /// <paramref name="array" />.Type represents an array type whose rank is not 1.
        /// -or-
        /// <paramref name="index" />.Type does not represent the <see cref="int" /> type.</exception>
        /// <remarks><paramref name="index" /> must represent an index of type <see cref="int" />.
        /// The <see cref="O:System.Linq.Expressions.BinaryExpression.Method" /> property of the resulting <see cref="System.Linq.Expressions.BinaryExpression" /> is <see langword="null" />, and both <see cref="O:System.Linq.Expressions.BinaryExpression.IsLifted" /> and <see cref="O:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> are set to <see langword="false" />. The <see cref="O:System.Linq.Expressions.Expression.Type" /> property is equal to the element type of <paramref name="array" />.Type. The <see cref="O:System.Linq.Expressions.BinaryExpression.Conversion" /> property is <see langword="null" />.</remarks>
        public static BinaryExpression ArrayIndex(Expression array, Expression index)
        {
            ExpressionUtils.RequiresCanRead(array, nameof(array));
            ExpressionUtils.RequiresCanRead(index, nameof(index));
            if (index.Type != typeof(int))
            {
                throw Error.ArgumentMustBeArrayIndexType(nameof(index));
            }

            Type arrayType = array.Type;
            if (!arrayType.IsArray)
            {
                throw Error.ArgumentMustBeArray(nameof(array));
            }
            if (arrayType.GetArrayRank() != 1)
            {
                throw Error.IncorrectNumberOfIndexes();
            }

            return new SimpleBinaryExpression(ExpressionType.ArrayIndex, array, index, arrayType.GetElementType()!);
        }

        #endregion
    }
}
