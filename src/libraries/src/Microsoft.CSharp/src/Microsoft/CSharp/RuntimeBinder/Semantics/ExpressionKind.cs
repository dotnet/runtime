// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal enum ExpressionKind
    {
        NoOp,
        // Now expressions. Keep BINOP first!
        BinaryOp,
        UnaryOp,
        Assignment,
        List,
        ArrayIndex,
        Call,
        Field,
        Local,
        Constant,
        Class,
        Property,
        Multi,
        MultiGet,
        Wrap,
        Concat,
        ArrayInit,
        Cast,
        UserDefinedConversion,
        TypeOf,
        ZeroInit,
        UserLogicalOp,
        MemberGroup,
        BoundLambda,
        FieldInfo,
        MethodInfo,
        PropertyInfo,
        NamedArgumentSpecification,

        /***************************************************************************************************
            Ones below here are not used to create actual expr types, only EK_ values.
        ***************************************************************************************************/
        ExpressionKindCount,
        EqualsParam,       // this is only used as a parameter, no actual exprs are constructed with it
        FirstOp = EqualsParam,
        Compare,      // this is only used as a parameter, no actual exprs are constructed with it
        True,
        False,
        Inc,
        Dec,
        LogicalNot,
        // keep Eq to GreaterThanOrEqual in the same sequence (ILGENREC::genCondBranch)
        Eq,
        RelationalMin = Eq,
        NotEq,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        RelationalMax = GreaterThanOrEqual,
        // keep Add to RightShift in the same sequence (ILGENREC::genBinopExpr)
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        Negate,
        UnaryPlus,
        BitwiseAnd,
        BitwiseOr,
        BitwiseExclusiveOr,
        BitwiseNot,
        LeftShirt,
        RightShift,
        // keep Add to RightShift in the same sequence (ILGENREC::genBinopExpr)
        LogicalAnd,
        LogicalOr,
        Sequence,     // p1 is side effects, p2 is values
        Save,         // p1 is expr, p2 is wrap to be saved into...
        Swap,
        Indir,
        Addr,
        // Next we have the predefined operator kinds. We have one EXPRKINDDEF for each of these.
        // So for example, we will have an EK_STRINGCOMPARISON, and an EK_DELEGATEADDITION etc.
        StringEq,
        StringNotEq,
        DelegateEq,
        DelegateNotEq,
        DelegateAdd,
        DelegateSubtract,
        DecimalNegate,
        DecimalInc,
        DecimalDec,
#if EERANGE
        EK_RANGE,
#endif
        MultiOffset,  // This has to be last!!! To deal /w multiops we add this to the op to obtain the ek in the op table
        // Statements are all before expressions and the first expression is EK_BINOP
        // EK types starting with EK_COUNT do not have associated EXPR structures,
        // and are all binary operators.
        TypeLimit = ExpressionKindCount,
    }

    internal static class ExpressionKindExtensions
    {
        public static bool IsRelational(this ExpressionKind kind)
        {
            return ExpressionKind.RelationalMin <= kind && kind <= ExpressionKind.RelationalMax;
        }

        public static bool IsUnaryOperator(this ExpressionKind kind)
        {
            switch (kind)
            {
                case ExpressionKind.True:
                case ExpressionKind.False:
                case ExpressionKind.Inc:
                case ExpressionKind.Dec:
                case ExpressionKind.LogicalNot:
                case ExpressionKind.Negate:
                case ExpressionKind.UnaryPlus:
                case ExpressionKind.BitwiseNot:
                case ExpressionKind.Addr:
                case ExpressionKind.DecimalNegate:
                case ExpressionKind.DecimalInc:
                case ExpressionKind.DecimalDec:
                    return true;
            }

            return false;
        }
    }
}
