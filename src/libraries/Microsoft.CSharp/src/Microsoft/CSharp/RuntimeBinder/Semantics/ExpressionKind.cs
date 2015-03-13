// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal enum ExpressionKind
    {
        EK_BLOCK,
        //EK_STMTAS,
        EK_RETURN,
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
        EK_NOOP,
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
        EK_BINOP,
        EK_UNARYOP,
        EK_ASSIGNMENT,
        EK_LIST,
        EK_QUESTIONMARK,
        //EK_MAKEREFANY,
        //EK_TYPEREFANY,
        EK_ARRAYINDEX,
        EK_ARRAYLENGTH,
        EK_ARGUMENTHANDLE,
        EK_CALL,
        EK_EVENT,
        EK_FIELD,
        EK_LOCAL,
        //EK_BASE,
        EK_THISPOINTER,
        EK_CONSTANT,
        //EK_TYPEORSIMPLENAME,
        // The following exprs are used to represent the results of typebinding.
        // Look in exprnodes.h for a more detailed description.
        EK_TYPEARGUMENTS,
        EK_TYPEORNAMESPACE,
        //EK_TYPEORNAMESPACEERROR,
        //EK_ARRAYTYPE,
        //EK_POINTERTYPE,
        //EK_NULLABLETYPE,
        EK_CLASS,
        //EK_NSPACE,
        EK_ALIAS,
        // End type exprs.
        //EK_ERROR,
        EK_FUNCPTR,
        EK_PROP,
        EK_MULTI,
        EK_MULTIGET,
        //EK_STTMP,
        //EK_LDTMP,
        //EK_FREETMP,
        EK_WRAP,
        EK_CONCAT,
        EK_ARRINIT,
        //EK_ARRAYCREATION,
        EK_CAST,
        //EK_EXPLICITCAST,
        EK_USERDEFINEDCONVERSION,
        //EK_ARGLIST,
        //EK_NEWTYVAR,
        EK_TYPEOF,
        //EK_SIZEOF,
        EK_ZEROINIT,
        EK_USERLOGOP,
        EK_MEMGRP,
        EK_BOUNDLAMBDA,
        EK_UNBOUNDLAMBDA,
        //EK_LAMBDAPARAMETER,
        EK_HOISTEDLOCALEXPR,
        EK_FIELDINFO,
        EK_METHODINFO,
        EK_PROPERTYINFO,
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
        EK_NamedArgumentSpecification,

        /***************************************************************************************************
            Ones below here are not used to create actual expr types, only EK_ values.
        ***************************************************************************************************/
        EK_COUNT,
        EK_EQUALS,       // this is only used as a parameter, no actual exprs are constructed with it
        EK_FIRSTOP = EK_EQUALS,
        EK_COMPARE,      // this is only used as a parameter, no actual exprs are constructed with it
        EK_TRUE,
        EK_FALSE,
        EK_INC,
        EK_DEC,
        EK_LOGNOT,
        // keep EK_EQ to EK_GE in the same sequence (ILGENREC::genCondBranch)
        EK_EQ,
        EK_RELATIONAL_MIN = EK_EQ,
        EK_NE,
        EK_LT,
        EK_LE,
        EK_GT,
        EK_GE,
        EK_RELATIONAL_MAX = EK_GE,
        // keep EK_ADD to EK_RSHIFT in the same sequence (ILGENREC::genBinopExpr)
        EK_ADD,
        EK_ARITH_MIN = EK_ADD,
        EK_SUB,
        EK_MUL,
        EK_DIV,
        EK_MOD,
        EK_NEG,
        EK_UPLUS,
        EK_ARITH_MAX = EK_UPLUS,
        EK_BITAND,
        EK_BIT_MIN = EK_BITAND,
        EK_BITOR,
        EK_BITXOR,
        EK_BITNOT,
        EK_BIT_MAX = EK_BITNOT,
        EK_LSHIFT,
        EK_RSHIFT,
        // keep EK_ADD to EK_RSHIFT in the same sequence (ILGENREC::genBinopExpr)
        EK_LOGAND,
        EK_LOGOR,
        EK_SEQUENCE,     // p1 is side effects, p2 is values
        EK_SEQREV,       // p1 is values, p2 is side effects
        EK_SAVE,         // p1 is expr, p2 is wrap to be saved into...
        EK_SWAP,
        EK_INDIR,
        EK_ADDR,
        // Next we have the predefined operator kinds. We have one EXPRKINDDEF for each of these.
        // So for example, we will have an EK_STRINGCOMPARISON, and an EK_DELEGATEADDITION etc.
        EK_STRINGEQ,
        EK_STRINGNE,
        EK_DELEGATEEQ,
        EK_DELEGATENE,
        EK_DELEGATEADD,
        EK_DELEGATESUB,
        EK_DECIMALNEG,
        EK_DECIMALINC,
        EK_DECIMALDEC,
#if EERANGE
        EK_RANGE,
#endif
        EK_MULTIOFFSET,  // This has to be last!!! To deal /w multiops we add this to the op to obtain the ek in the op table
        // Statements are all before expressions and the first expression is EK_BINOP
        EK_ExprMin = EK_BINOP,
        EK_StmtLim = EK_ExprMin,
        // EK types starting with EK_COUNT do not have associated EXPR structures,
        // and are all binary operators.
        EK_TypeLim = EK_COUNT,
    }

    internal static class ExpressionKindExtensions
    {
        public static bool isRelational(this ExpressionKind kind)
        {
            return ExpressionKind.EK_RELATIONAL_MIN <= kind && kind <= ExpressionKind.EK_RELATIONAL_MAX;
        }
        public static bool isUnaryOperator(this ExpressionKind kind)
        {
            switch (kind)
            {
                case ExpressionKind.EK_TRUE:
                case ExpressionKind.EK_FALSE:
                case ExpressionKind.EK_INC:
                case ExpressionKind.EK_DEC:
                case ExpressionKind.EK_LOGNOT:
                case ExpressionKind.EK_NEG:
                case ExpressionKind.EK_UPLUS:
                case ExpressionKind.EK_BITNOT:
                case ExpressionKind.EK_ADDR:
                case ExpressionKind.EK_DECIMALNEG:
                case ExpressionKind.EK_DECIMALINC:
                case ExpressionKind.EK_DECIMALDEC:
                    return true;
            }
            return false;
        }
    }
}
