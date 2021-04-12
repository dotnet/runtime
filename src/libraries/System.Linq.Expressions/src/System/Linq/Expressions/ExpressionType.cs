// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Linq.Expressions
{
    /// <summary>Describes the node types for the nodes of an expression tree.</summary>
    /// <remarks>For more information about each enumeration value of this type, see [Dynamic Language Runtime Overview](/dotnet/framework/reflection-and-codedom/dynamic-language-runtime-overview).</remarks>
    public enum ExpressionType
    {
        /// <summary>An addition operation, such as <c>a + b</c>, without overflow checking, for numeric operands.</summary>
        Add,
        /// <summary>An addition operation, such as <c>(a + b)</c>, with overflow checking, for numeric operands.</summary>
        AddChecked,
        /// <summary>A bitwise or logical <see langword="AND" /> operation, such as <c>(a &amp; b)</c> in C# and <c>(a And b)</c> in Visual Basic.</summary>
        And,
        /// <summary>A conditional <see langword="AND" /> operation that evaluates the second operand only if the first operand evaluates to <see langword="true" />. It corresponds to <c>(a &amp;&amp; b)</c> in C# and <c>(a AndAlso b)</c> in Visual Basic.</summary>
        AndAlso,
        /// <summary>An operation that obtains the length of a one-dimensional array, such as <c>array.Length</c>.</summary>
        ArrayLength,
        /// <summary>An indexing operation in a one-dimensional array, such as <c>array[index]</c> in C# or <c>array(index)</c> in Visual Basic.</summary>
        ArrayIndex,
        /// <summary>A method call, such as in the <c>obj.sampleMethod()</c> expression.</summary>
        Call,
        /// <summary>A node that represents a null coalescing operation, such as <c>(a ?? b)</c> in C# or <c>If(a, b)</c> in Visual Basic.</summary>
        Coalesce,
        /// <summary>A conditional operation, such as <c>a &gt; b ? a : b</c> in C# or <c>If(a &gt; b, a, b)</c> in Visual Basic.</summary>
        Conditional,
        /// <summary>A constant value.</summary>
        Constant,
        /// <summary>A cast or conversion operation, such as <c>(SampleType)obj</c> in C#or <c>CType(obj, SampleType)</c> in Visual Basic. For a numeric conversion, if the converted value is too large for the destination type, no exception is thrown.</summary>
        Convert,
        /// <summary>A cast or conversion operation, such as <c>(SampleType)obj</c> in C#or <c>CType(obj, SampleType)</c> in Visual Basic. For a numeric conversion, if the converted value does not fit the destination type, an exception is thrown.</summary>
        ConvertChecked,
        /// <summary>A division operation, such as <c>(a / b)</c>, for numeric operands.</summary>
        Divide,
        /// <summary>A node that represents an equality comparison, such as <c>(a == b)</c> in C# or <c>(a = b)</c> in Visual Basic.</summary>
        Equal,
        /// <summary>A bitwise or logical <see langword="XOR" /> operation, such as <c>(a ^ b)</c> in C# or <c>(a Xor b)</c> in Visual Basic.</summary>
        ExclusiveOr,
        /// <summary>A "greater than" comparison, such as <c>(a &gt; b)</c>.</summary>
        GreaterThan,
        /// <summary>A "greater than or equal to" comparison, such as <c>(a &gt;= b)</c>.</summary>
        GreaterThanOrEqual,
        /// <summary>An operation that invokes a delegate or lambda expression, such as <c>sampleDelegate.Invoke()</c>.</summary>
        Invoke,
        /// <summary>A lambda expression, such as <c>a =&gt; a + a</c> in C# or <c>Function(a) a + a</c> in Visual Basic.</summary>
        Lambda,
        /// <summary>A bitwise left-shift operation, such as <c>(a &lt;&lt; b)</c>.</summary>
        LeftShift,
        /// <summary>A "less than" comparison, such as <c>(a &lt; b)</c>.</summary>
        LessThan,
        /// <summary>A "less than or equal to" comparison, such as <c>(a &lt;= b)</c>.</summary>
        LessThanOrEqual,
        /// <summary>An operation that creates a new <see cref="System.Collections.IEnumerable" /> object and initializes it from a list of elements, such as <c>new List&lt;SampleType&gt;(){ a, b, c }</c> in C# or <c>Dim sampleList = { a, b, c }</c> in Visual Basic.</summary>
        ListInit,
        /// <summary>An operation that reads from a field or property, such as <c>obj.SampleProperty</c>.</summary>
        MemberAccess,
        /// <summary>An operation that creates a new object and initializes one or more of its members, such as <c>new Point { X = 1, Y = 2 }</c> in C# or <c>New Point With {.X = 1, .Y = 2}</c> in Visual Basic.</summary>
        MemberInit,
        /// <summary>An arithmetic remainder operation, such as <c>(a % b)</c> in C# or <c>(a Mod b)</c> in Visual Basic.</summary>
        Modulo,
        /// <summary>A multiplication operation, such as <c>(a * b)</c>, without overflow checking, for numeric operands.</summary>
        Multiply,
        /// <summary>An multiplication operation, such as <c>(a * b)</c>, that has overflow checking, for numeric operands.</summary>
        MultiplyChecked,
        /// <summary>An arithmetic negation operation, such as <c>(-a)</c>. The object <c>a</c> should not be modified in place.</summary>
        Negate,
        /// <summary>A unary plus operation, such as <c>(+a)</c>. The result of a predefined unary plus operation is the value of the operand, but user-defined implementations might have unusual results.</summary>
        UnaryPlus,
        /// <summary>An arithmetic negation operation, such as <c>(-a)</c>, that has overflow checking. The object <c>a</c> should not be modified in place.</summary>
        NegateChecked,
        /// <summary>An operation that calls a constructor to create a new object, such as <c>new SampleType()</c>.</summary>
        New,
        /// <summary>An operation that creates a new one-dimensional array and initializes it from a list of elements, such as <c>new SampleType[]{a, b, c}</c> in C# or <c>New SampleType(){a, b, c}</c> in Visual Basic.</summary>
        NewArrayInit,
        /// <summary>An operation that creates a new array, in which the bounds for each dimension are specified, such as <c>new SampleType[dim1, dim2]</c> in C# or <c>New SampleType(dim1, dim2)</c> in Visual Basic.</summary>
        NewArrayBounds,
        /// <summary>A bitwise complement or logical negation operation. In C#, it is equivalent to <c>(~a)</c> for integral types and to <c>(!a)</c> for Boolean values. In Visual Basic, it is equivalent to <c>(Not a)</c>. The object <c>a</c> should not be modified in place.</summary>
        Not,
        /// <summary>An inequality comparison, such as <c>(a != b)</c> in C# or <c>(a &lt;&gt; b)</c> in Visual Basic.</summary>
        NotEqual,
        /// <summary>A bitwise or logical <see langword="OR" /> operation, such as <c>(a | b)</c> in C# or <c>(a Or b)</c> in Visual Basic.</summary>
        Or,
        /// <summary>A short-circuiting conditional <see langword="OR" /> operation, such as <c>(a || b)</c> in C# or <c>(a OrElse b)</c> in Visual Basic.</summary>
        OrElse,
        /// <summary>A reference to a parameter or variable that is defined in the context of the expression. For more information, see <see cref="System.Linq.Expressions.ParameterExpression" />.</summary>
        Parameter,
        /// <summary>A mathematical operation that raises a number to a power, such as <c>(a ^ b)</c> in Visual Basic.</summary>
        Power,
        /// <summary>An expression that has a constant value of type <see cref="System.Linq.Expressions.Expression" />. A <see cref="System.Linq.Expressions.ExpressionType.Quote" /> node can contain references to parameters that are defined in the context of the expression it represents.</summary>
        Quote,
        /// <summary>A bitwise right-shift operation, such as <c>(a &gt;&gt; b)</c>.</summary>
        RightShift,
        /// <summary>A subtraction operation, such as <c>(a - b)</c>, without overflow checking, for numeric operands.</summary>
        Subtract,
        /// <summary>An arithmetic subtraction operation, such as <c>(a - b)</c>, that has overflow checking, for numeric operands.</summary>
        SubtractChecked,
        /// <summary>An explicit reference or boxing conversion in which <see langword="null" /> is supplied if the conversion fails, such as <c>(obj as SampleType)</c> in C# or <c>TryCast(obj, SampleType)</c> in Visual Basic.</summary>
        TypeAs,
        /// <summary>A type test, such as <c>obj is SampleType</c> in C# or <c>TypeOf obj is SampleType</c> in Visual Basic.</summary>
        TypeIs,
        /// <summary>An assignment operation, such as <c>(a = b)</c>.</summary>
        Assign,
        /// <summary>A block of expressions.</summary>
        Block,
        /// <summary>Debugging information.</summary>
        DebugInfo,
        /// <summary>A unary decrement operation, such as <c>(a - 1)</c> in C# and Visual Basic. The object <c>a</c> should not be modified in place.</summary>
        Decrement,
        /// <summary>A dynamic operation.</summary>
        Dynamic,
        /// <summary>A default value.</summary>
        Default,
        /// <summary>An extension expression.</summary>
        Extension,
        /// <summary>A "go to" expression, such as <c>goto Label</c> in C# or <c>GoTo Label</c> in Visual Basic.</summary>
        Goto,
        /// <summary>A unary increment operation, such as <c>(a + 1)</c> in C# and Visual Basic. The object <c>a</c> should not be modified in place.</summary>
        Increment,
        /// <summary>An index operation or an operation that accesses a property that takes arguments.</summary>
        Index,
        /// <summary>A label.</summary>
        Label,
        /// <summary>A list of run-time variables. For more information, see <see cref="System.Linq.Expressions.RuntimeVariablesExpression" />.</summary>
        RuntimeVariables,
        /// <summary>A loop, such as <c>for</c> or <c>while</c>.</summary>
        Loop,
        /// <summary>A switch operation, such as <see langword="switch" /> in C# or <see langword="Select Case" /> in Visual Basic.</summary>
        Switch,
        /// <summary>An operation that throws an exception, such as <c>throw new Exception()</c>.</summary>
        Throw,
        /// <summary>A <see langword="try-catch" /> expression.</summary>
        Try,
        /// <summary>An unbox value type operation, such as <see langword="unbox" /> and <see langword="unbox.any" /> instructions in MSIL.</summary>
        Unbox,
        /// <summary>An addition compound assignment operation, such as <c>(a += b)</c>, without overflow checking, for numeric operands.</summary>
        AddAssign,
        /// <summary>A bitwise or logical <see langword="AND" /> compound assignment operation, such as <c>(a &amp;= b)</c> in C#.</summary>
        AndAssign,
        /// <summary>An division compound assignment operation, such as <c>(a /= b)</c>, for numeric operands.</summary>
        DivideAssign,
        /// <summary>A bitwise or logical <see langword="XOR" /> compound assignment operation, such as <c>(a ^= b)</c> in C#.</summary>
        ExclusiveOrAssign,
        /// <summary>A bitwise left-shift compound assignment, such as <c>(a &lt;&lt;= b)</c>.</summary>
        LeftShiftAssign,
        /// <summary>An arithmetic remainder compound assignment operation, such as <c>(a %= b)</c> in C#.</summary>
        ModuloAssign,
        /// <summary>A multiplication compound assignment operation, such as <c>(a *= b)</c>, without overflow checking, for numeric operands.</summary>
        MultiplyAssign,
        /// <summary>A bitwise or logical <see langword="OR" /> compound assignment, such as <c>(a |= b)</c> in C#.</summary>
        OrAssign,
        /// <summary>A compound assignment operation that raises a number to a power, such as <c>(a ^= b)</c> in Visual Basic.</summary>
        PowerAssign,
        /// <summary>A bitwise right-shift compound assignment operation, such as <c>(a &gt;&gt;= b)</c>.</summary>
        RightShiftAssign,
        /// <summary>A subtraction compound assignment operation, such as <c>(a -= b)</c>, without overflow checking, for numeric operands.</summary>
        SubtractAssign,
        /// <summary>An addition compound assignment operation, such as <c>(a += b)</c>, with overflow checking, for numeric operands.</summary>
        AddAssignChecked,
        /// <summary>A multiplication compound assignment operation, such as <c>(a *= b)</c>, that has overflow checking, for numeric operands.</summary>
        MultiplyAssignChecked,
        /// <summary>A subtraction compound assignment operation, such as <c>(a -= b)</c>, that has overflow checking, for numeric operands.</summary>
        SubtractAssignChecked,
        /// <summary>A unary prefix increment, such as <c>(++a)</c>. The object <c>a</c> should be modified in place.</summary>
        PreIncrementAssign,
        /// <summary>A unary prefix decrement, such as <c>(--a)</c>. The object <c>a</c> should be modified in place.</summary>
        PreDecrementAssign,
        /// <summary>A unary postfix increment, such as <c>(a++)</c>. The object <c>a</c> should be modified in place.</summary>
        PostIncrementAssign,
        /// <summary>A unary postfix decrement, such as <c>(a--)</c>. The object <c>a</c> should be modified in place.</summary>
        PostDecrementAssign,
        /// <summary>An exact type test.</summary>
        TypeEqual,
        /// <summary>A ones complement operation, such as <c>(~a)</c> in C#.</summary>
        OnesComplement,
        /// <summary>A <see langword="true" /> condition value.</summary>
        IsTrue,
        /// <summary>A <see langword="false" /> condition value.</summary>
        IsFalse,
    }
}
