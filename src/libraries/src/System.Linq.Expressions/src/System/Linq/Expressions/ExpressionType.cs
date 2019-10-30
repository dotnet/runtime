// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Linq.Expressions
{
    /// <summary>
    /// Describes the node types for the nodes of an expression tree.
    /// </summary>
    public enum ExpressionType
    {
        /// <summary>
        /// A node that represents arithmetic addition without overflow checking.
        /// </summary>
        Add,
        /// <summary>
        /// A node that represents arithmetic addition with overflow checking.
        /// </summary>
        AddChecked,
        /// <summary>
        /// A node that represents a bitwise AND operation.
        /// </summary>
        And,
        /// <summary>
        /// A node that represents a short-circuiting conditional AND operation.
        /// </summary>
        AndAlso,
        /// <summary>
        /// A node that represents getting the length of a one-dimensional array.
        /// </summary>
        ArrayLength,
        /// <summary>
        /// A node that represents indexing into a one-dimensional array.
        /// </summary>
        ArrayIndex,
        /// <summary>
        /// A node that represents a method call.
        /// </summary>
        Call,
        /// <summary>
        /// A node that represents a null coalescing operation.
        /// </summary>
        Coalesce,
        /// <summary>
        /// A node that represents a conditional operation.
        /// </summary>
        Conditional,
        /// <summary>
        /// A node that represents an expression that has a constant value.
        /// </summary>
        Constant,
        /// <summary>
        /// A node that represents a cast or conversion operation. If the operation is a numeric conversion, it overflows silently if the converted value does not fit the target type.
        /// </summary>
        Convert,
        /// <summary>
        /// A node that represents a cast or conversion operation. If the operation is a numeric conversion, an exception is thrown if the converted value does not fit the target type.
        /// </summary>
        ConvertChecked,
        /// <summary>
        /// A node that represents arithmetic division.
        /// </summary>
        Divide,
        /// <summary>
        /// A node that represents an equality comparison.
        /// </summary>
        Equal,
        /// <summary>
        /// A node that represents a bitwise XOR operation.
        /// </summary>
        ExclusiveOr,
        /// <summary>
        /// A node that represents a "greater than" numeric comparison.
        /// </summary>
        GreaterThan,
        /// <summary>
        /// A node that represents a "greater than or equal" numeric comparison.
        /// </summary>
        GreaterThanOrEqual,
        /// <summary>
        /// A node that represents applying a delegate or lambda expression to a list of argument expressions.
        /// </summary>
        Invoke,
        /// <summary>
        /// A node that represents a lambda expression.
        /// </summary>
        Lambda,
        /// <summary>
        /// A node that represents a bitwise left-shift operation.
        /// </summary>
        LeftShift,
        /// <summary>
        /// A node that represents a "less than" numeric comparison.
        /// </summary>
        LessThan,
        /// <summary>
        /// A node that represents a "less than or equal" numeric comparison.
        /// </summary>
        LessThanOrEqual,
        /// <summary>
        /// A node that represents creating a new IEnumerable object and initializing it from a list of elements.
        /// </summary>
        ListInit,
        /// <summary>
        /// A node that represents reading from a field or property.
        /// </summary>
        MemberAccess,
        /// <summary>
        /// A node that represents creating a new object and initializing one or more of its members.
        /// </summary>
        MemberInit,
        /// <summary>
        /// A node that represents an arithmetic remainder operation.
        /// </summary>
        Modulo,
        /// <summary>
        /// A node that represents arithmetic multiplication without overflow checking.
        /// </summary>
        Multiply,
        /// <summary>
        /// A node that represents arithmetic multiplication with overflow checking.
        /// </summary>
        MultiplyChecked,
        /// <summary>
        /// A node that represents an arithmetic negation operation.
        /// </summary>
        Negate,
        /// <summary>
        /// A node that represents a unary plus operation. The result of a predefined unary plus operation is simply the value of the operand, but user-defined implementations may have non-trivial results.
        /// </summary>
        UnaryPlus,
        /// <summary>
        /// A node that represents an arithmetic negation operation that has overflow checking.
        /// </summary>
        NegateChecked,
        /// <summary>
        /// A node that represents calling a constructor to create a new object.
        /// </summary>
        New,
        /// <summary>
        /// A node that represents creating a new one-dimensional array and initializing it from a list of elements.
        /// </summary>
        NewArrayInit,
        /// <summary>
        /// A node that represents creating a new array where the bounds for each dimension are specified.
        /// </summary>
        NewArrayBounds,
        /// <summary>
        /// A node that represents a bitwise complement operation.
        /// </summary>
        Not,
        /// <summary>
        /// A node that represents an inequality comparison.
        /// </summary>
        NotEqual,
        /// <summary>
        /// A node that represents a bitwise OR operation.
        /// </summary>
        Or,
        /// <summary>
        /// A node that represents a short-circuiting conditional OR operation.
        /// </summary>
        OrElse,
        /// <summary>
        /// A node that represents a reference to a parameter or variable defined in the context of the expression.
        /// </summary>
        Parameter,
        /// <summary>
        /// A node that represents raising a number to a power.
        /// </summary>
        Power,
        /// <summary>
        /// A node that represents an expression that has a constant value of type Expression. A Quote node can contain references to parameters defined in the context of the expression it represents.
        /// </summary>
        Quote,
        /// <summary>
        /// A node that represents a bitwise right-shift operation.
        /// </summary>
        RightShift,
        /// <summary>
        /// A node that represents arithmetic subtraction without overflow checking.
        /// </summary>
        Subtract,
        /// <summary>
        /// A node that represents arithmetic subtraction with overflow checking.
        /// </summary>
        SubtractChecked,
        /// <summary>
        /// A node that represents an explicit reference or boxing conversion where null reference (Nothing in Visual Basic) is supplied if the conversion fails.
        /// </summary>
        TypeAs,
        /// <summary>
        /// A node that represents a type test.
        /// </summary>
        TypeIs,
        /// <summary>
        /// A node that represents an assignment.
        /// </summary>
        Assign,
        /// <summary>
        /// A node that represents a block of expressions.
        /// </summary>
        Block,
        /// <summary>
        /// A node that represents a debugging information.
        /// </summary>
        DebugInfo,
        /// <summary>
        /// A node that represents a unary decrement.
        /// </summary>
        Decrement,
        /// <summary>
        /// A node that represents a dynamic operation.
        /// </summary>
        Dynamic,
        /// <summary>
        /// A node that represents a default value.
        /// </summary>
        Default,
        /// <summary>
        /// A node that represents an extension expression.
        /// </summary>
        Extension,
        /// <summary>
        /// A node that represents a goto.
        /// </summary>
        Goto,
        /// <summary>
        /// A node that represents a unary increment.
        /// </summary>
        Increment,
        /// <summary>
        /// A node that represents an index operation.
        /// </summary>
        Index,
        /// <summary>
        /// A node that represents a label.
        /// </summary>
        Label,
        /// <summary>
        /// A node that represents a list of runtime variables.
        /// </summary>
        RuntimeVariables,
        /// <summary>
        /// A node that represents a loop.
        /// </summary>
        Loop,
        /// <summary>
        /// A node that represents a switch operation.
        /// </summary>
        Switch,
        /// <summary>
        /// A node that represents a throwing of an exception.
        /// </summary>
        Throw,
        /// <summary>
        /// A node that represents a try-catch expression.
        /// </summary>
        Try,
        /// <summary>
        /// A node that represents an unbox value type operation.
        /// </summary>
        Unbox,
        /// <summary>
        /// A node that represents an arithmetic addition compound assignment without overflow checking.
        /// </summary>
        AddAssign,
        /// <summary>
        /// A node that represents a bitwise AND compound assignment.
        /// </summary>
        AndAssign,
        /// <summary>
        /// A node that represents an arithmetic division compound assignment .
        /// </summary>
        DivideAssign,
        /// <summary>
        /// A node that represents a bitwise XOR compound assignment.
        /// </summary>
        ExclusiveOrAssign,
        /// <summary>
        /// A node that represents a bitwise left-shift compound assignment.
        /// </summary>
        LeftShiftAssign,
        /// <summary>
        /// A node that represents an arithmetic remainder compound assignment.
        /// </summary>
        ModuloAssign,
        /// <summary>
        /// A node that represents arithmetic multiplication compound assignment without overflow checking.
        /// </summary>
        MultiplyAssign,
        /// <summary>
        /// A node that represents a bitwise OR compound assignment.
        /// </summary>
        OrAssign,
        /// <summary>
        /// A node that represents raising a number to a power compound assignment.
        /// </summary>
        PowerAssign,
        /// <summary>
        /// A node that represents a bitwise right-shift compound assignment.
        /// </summary>
        RightShiftAssign,
        /// <summary>
        /// A node that represents arithmetic subtraction compound assignment without overflow checking.
        /// </summary>
        SubtractAssign,
        /// <summary>
        /// A node that represents an arithmetic addition compound assignment with overflow checking.
        /// </summary>
        AddAssignChecked,
        /// <summary>
        /// A node that represents arithmetic multiplication compound assignment with overflow checking.
        /// </summary>
        MultiplyAssignChecked,
        /// <summary>
        /// A node that represents arithmetic subtraction compound assignment with overflow checking.
        /// </summary>
        SubtractAssignChecked,
        /// <summary>
        /// A node that represents an unary prefix increment.
        /// </summary>
        PreIncrementAssign,
        /// <summary>
        /// A node that represents an unary prefix decrement.
        /// </summary>
        PreDecrementAssign,
        /// <summary>
        /// A node that represents an unary postfix increment.
        /// </summary>
        PostIncrementAssign,
        /// <summary>
        /// A node that represents an unary postfix decrement.
        /// </summary>
        PostDecrementAssign,
        /// <summary>
        /// A node that represents an exact type test.
        /// </summary>
        TypeEqual,
        /// <summary>
        /// A node that represents a ones complement.
        /// </summary>
        OnesComplement,
        /// <summary>
        /// A node that represents a true condition value.
        /// </summary>
        IsTrue,
        /// <summary>
        /// A node that represents a false condition value.
        /// </summary>
        IsFalse,
    }
}
