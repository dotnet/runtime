// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal enum ExpressionKind
    {
        Block,
        //EK_STMTAS,
        Return,
        //EK_DECL,
        //EK_LABEL,
        //EK_PLACEHOLDERLABEL,
        //EK_GOTO,
        //EK_GOTOIF,
        //EK_FlatSwitch,
        //EK_SWITCHLABEL,
        //EK_ENDSWITCHLABEL,
        //EK_TRY,
        //EK_HANDLER,
        //EK_THROW,
        NoOp,
        //EK_DEBUGNOOP,

        //EK_For,
        //EK_Foreach,
        //EK_Using,
        //EK_Switch,
        //EK_SwitchSection,
        //EK_SwitchSectionLabel,
        //EK_Lock,

        //EK_Attribute,
        // Now expressions. Keep BINOP first!
        BinaryOp,
        UnaryOp,
        Assignment,
        List,
        //QuestionMark,
        //EK_MAKEREFANY,
        //EK_TYPEREFANY,
        ArrayIndex,
        //ArrayLength,
        //EK_ARGUMENTHANDLE,
        Call,
        //Event,
        Field,
        Local,
        //EK_BASE,
        //ThisPointer,
        Constant,
        //EK_TYPEORSIMPLENAME,
        // The following exprs are used to represent the results of typebinding.
        // Look in exprnodes.h for a more detailed description.
        //TypeArguments,
        //EK_TYPEORNAMESPACE,
        //EK_TYPEORNAMESPACEERROR,
        //EK_ARRAYTYPE,
        //EK_POINTERTYPE,
        //EK_NULLABLETYPE,
        Class,
        //EK_NSPACE,
        //EK_ALIAS,
        // End type exprs.
        //EK_ERROR,
        //FunctionPointer,
        Property,
        Multi,
        MultiGet,
        //EK_STTMP,
        //EK_LDTMP,
        //EK_FREETMP,
        Wrap,
        Concat,
        ArrayInit,
        //EK_ARRAYCREATION,
        Cast,
        //EK_EXPLICITCAST,
        UserDefinedConversion,
        //EK_ARGLIST,
        //EK_NEWTYVAR,
        TypeOf,
        //EK_SIZEOF,
        ZeroInit,
        UserLogicalOp,
        MemberGroup,
        BoundLambda,
        //UnboundLambda,
        //EK_LAMBDAPARAMETER,
        HoistedLocalExpression,
        FieldInfo,
        MethodInfo,
        PropertyInfo,
        //EK_DBLQMARK,
        //EK_VALUERA,
        //EK_INITIALIZER,
        //EK_INITASSIGN,
        //EK_LOCALLOC,
        //EK_IS,
        //EK_AS,
        //EK_DELEGATECREATION,
        //EK_COLLECTIONELEMENT,
        //EK_METHODBODY,
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
        SequenceReverse,       // p1 is values, p2 is side effects
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
