// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal static class Operators
    {
        private sealed class OPINFO
        {
            public OPINFO(TokenKind t, PredefinedName pn, ExpressionKind e, int c)
            {
                iToken = t;
                methodName = pn;
                expressionKind = e;
            }
            public readonly TokenKind iToken;
            public readonly PredefinedName methodName;
            public readonly ExpressionKind expressionKind;
        }

        private static readonly Dictionary<OperatorKind, OPINFO> s_rgOpInfo = new Dictionary<OperatorKind, OPINFO>()
        {
{OperatorKind.OP_NONE,            new OPINFO(TokenKind.Unknown             , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 0)},
{OperatorKind.OP_ASSIGN,          new OPINFO(TokenKind.Equal               , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 2)},
{OperatorKind.OP_ADDEQ,           new OPINFO(TokenKind.PlusEqual           , PredefinedName.PN_COUNT                , ExpressionKind.MultiOffset + (int)ExpressionKind.Add   , 2)},
{OperatorKind.OP_SUBEQ,           new OPINFO(TokenKind.MinusEqual          , PredefinedName.PN_COUNT                , ExpressionKind.MultiOffset + (int)ExpressionKind.Subtract   , 2)},
{OperatorKind.OP_MULEQ,           new OPINFO(TokenKind.SplatEqual          , PredefinedName.PN_COUNT                , ExpressionKind.MultiOffset + (int)ExpressionKind.Multiply   , 2)},
{OperatorKind.OP_DIVEQ,           new OPINFO(TokenKind.SlashEqual          , PredefinedName.PN_COUNT                , ExpressionKind.MultiOffset + (int)ExpressionKind.Divide   , 2)},
{OperatorKind.OP_MODEQ,           new OPINFO(TokenKind.PercentEqual        , PredefinedName.PN_COUNT                , ExpressionKind.MultiOffset + (int)ExpressionKind.Modulo   , 2)},
{OperatorKind.OP_ANDEQ,           new OPINFO(TokenKind.AndEqual            , PredefinedName.PN_COUNT                , ExpressionKind.MultiOffset + (int)ExpressionKind.BitwiseAnd, 2)},
{OperatorKind.OP_XOREQ,           new OPINFO(TokenKind.HatEqual            , PredefinedName.PN_COUNT                , ExpressionKind.MultiOffset + (int)ExpressionKind.BitwiseExclusiveOr, 2)},
{OperatorKind.OP_OREQ,            new OPINFO(TokenKind.BarEqual            , PredefinedName.PN_COUNT                , ExpressionKind.MultiOffset + (int)ExpressionKind.BitwiseOr , 2)},
{OperatorKind.OP_LSHIFTEQ,        new OPINFO(TokenKind.LeftShiftEqual      , PredefinedName.PN_COUNT                , ExpressionKind.MultiOffset + (int)ExpressionKind.LeftShirt, 2)},
{OperatorKind.OP_RSHIFTEQ,        new OPINFO(TokenKind.RightShiftEqual     , PredefinedName.PN_COUNT                , ExpressionKind.MultiOffset + (int)ExpressionKind.RightShift, 2)},
{OperatorKind.OP_QUESTION,        new OPINFO(TokenKind.Question            , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 2)},
{OperatorKind.OP_VALORDEF,        new OPINFO(TokenKind.QuestionQuestion    , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 2)},
{OperatorKind.OP_LOGOR,           new OPINFO(TokenKind.LogicalOr           , PredefinedName.PN_COUNT                , ExpressionKind.LogicalOr                  , 2)},
{OperatorKind.OP_LOGAND,          new OPINFO(TokenKind.LogicalAnd          , PredefinedName.PN_COUNT                , ExpressionKind.LogicalAnd                 , 2)},
{OperatorKind.OP_BITOR,           new OPINFO(TokenKind.Bar                 , PredefinedName.PN_OPBITWISEOR          , ExpressionKind.BitwiseOr                  , 2)},
{OperatorKind.OP_BITXOR,          new OPINFO(TokenKind.Hat                 , PredefinedName.PN_OPXOR                , ExpressionKind.BitwiseExclusiveOr                 , 2)},
{OperatorKind.OP_BITAND,          new OPINFO(TokenKind.Ampersand           , PredefinedName.PN_OPBITWISEAND         , ExpressionKind.BitwiseAnd                 , 2)},
{OperatorKind.OP_EQ,              new OPINFO(TokenKind.EqualEqual          , PredefinedName.PN_OPEQUALITY           , ExpressionKind.Eq                     , 2)},
{OperatorKind.OP_NEQ,             new OPINFO(TokenKind.NotEqual            , PredefinedName.PN_OPINEQUALITY         , ExpressionKind.NotEq                     , 2)},
{OperatorKind.OP_LT,              new OPINFO(TokenKind.LessThan            , PredefinedName.PN_OPLESSTHAN           , ExpressionKind.LessThan                     , 2)},
{OperatorKind.OP_LE,              new OPINFO(TokenKind.LessThanEqual       , PredefinedName.PN_OPLESSTHANOREQUAL    , ExpressionKind.LessThanOrEqual                     , 2)},
{OperatorKind.OP_GT,              new OPINFO(TokenKind.GreaterThan         , PredefinedName.PN_OPGREATERTHAN        , ExpressionKind.GreaterThan                     , 2)},
{OperatorKind.OP_GE,              new OPINFO(TokenKind.GreaterThanEqual    , PredefinedName.PN_OPGREATERTHANOREQUAL , ExpressionKind.GreaterThanOrEqual                     , 2)},
{OperatorKind.OP_IS,              new OPINFO(TokenKind.Is                  , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 2)},
{OperatorKind.OP_AS,              new OPINFO(TokenKind.As                  , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 2)},
{OperatorKind.OP_LSHIFT,          new OPINFO(TokenKind.LeftShift           , PredefinedName.PN_OPLEFTSHIFT          , ExpressionKind.LeftShirt                 , 2)},
{OperatorKind.OP_RSHIFT,          new OPINFO(TokenKind.RightShift          , PredefinedName.PN_OPRIGHTSHIFT         , ExpressionKind.RightShift                 , 2)},
{OperatorKind.OP_ADD,             new OPINFO(TokenKind.Plus                , PredefinedName.PN_OPPLUS               , ExpressionKind.Add                    , 2)},
{OperatorKind.OP_SUB,             new OPINFO(TokenKind.Minus               , PredefinedName.PN_OPMINUS              , ExpressionKind.Subtract                    , 2)},
{OperatorKind.OP_MUL,             new OPINFO(TokenKind.Splat               , PredefinedName.PN_OPMULTIPLY           , ExpressionKind.Multiply                    , 2)},
{OperatorKind.OP_DIV,             new OPINFO(TokenKind.Slash               , PredefinedName.PN_OPDIVISION           , ExpressionKind.Divide                    , 2)},
{OperatorKind.OP_MOD,             new OPINFO(TokenKind.Percent             , PredefinedName.PN_OPMODULUS            , ExpressionKind.Modulo                    , 2)},
{OperatorKind.OP_NOP,             new OPINFO(TokenKind.Unknown             , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 1)},
{OperatorKind.OP_UPLUS,           new OPINFO(TokenKind.Plus                , PredefinedName.PN_OPUNARYPLUS          , ExpressionKind.UnaryPlus                  , 1)},
{OperatorKind.OP_NEG,             new OPINFO(TokenKind.Minus               , PredefinedName.PN_OPUNARYMINUS         , ExpressionKind.Negate                    , 1)},
{OperatorKind.OP_BITNOT,          new OPINFO(TokenKind.Tilde               , PredefinedName.PN_OPCOMPLEMENT         , ExpressionKind.BitwiseNot                 , 1)},
{OperatorKind.OP_LOGNOT,          new OPINFO(TokenKind.Bang                , PredefinedName.PN_OPNEGATION           , ExpressionKind.LogicalNot                 , 1)},
{OperatorKind.OP_PREINC,          new OPINFO(TokenKind.PlusPlus            , PredefinedName.PN_OPINCREMENT          , ExpressionKind.Add                    , 1)},
{OperatorKind.OP_PREDEC,          new OPINFO(TokenKind.MinusMinus          , PredefinedName.PN_OPDECREMENT          , ExpressionKind.Subtract                    , 1)},
{OperatorKind.OP_TYPEOF,          new OPINFO(TokenKind.TypeOf              , PredefinedName.PN_COUNT                , ExpressionKind.TypeOf                 , 1)},
{OperatorKind.OP_CHECKED,         new OPINFO(TokenKind.Checked             , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 1)},
{OperatorKind.OP_UNCHECKED,       new OPINFO(TokenKind.Unchecked           , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 1)},
{OperatorKind.OP_MAKEREFANY,      new OPINFO(TokenKind.MakeRef             , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 1)},
{OperatorKind.OP_REFVALUE,        new OPINFO(TokenKind.RefValue            , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 2)},
{OperatorKind.OP_REFTYPE,         new OPINFO(TokenKind.RefType             , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 1)},
{OperatorKind.OP_ARGS,            new OPINFO(TokenKind.ArgList             , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 0)},
{OperatorKind.OP_CAST,            new OPINFO(TokenKind.Unknown             , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 2)},
{OperatorKind.OP_INDIR,           new OPINFO(TokenKind.Splat               , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 1)},
{OperatorKind.OP_ADDR,            new OPINFO(TokenKind.Ampersand           , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 1)},
{OperatorKind.OP_COLON,           new OPINFO(TokenKind.Colon               , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 2)},
{OperatorKind.OP_THIS,            new OPINFO(TokenKind.This                , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 0)},
{OperatorKind.OP_BASE,            new OPINFO(TokenKind.Base                , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 0)},
{OperatorKind.OP_NULL,            new OPINFO(TokenKind.Null                , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 0)},
{OperatorKind.OP_TRUE,            new OPINFO(TokenKind.True                , PredefinedName.PN_OPTRUE               , ExpressionKind.ExpressionKindCount                  , 1)},
{OperatorKind.OP_FALSE,           new OPINFO(TokenKind.False               , PredefinedName.PN_OPFALSE              , ExpressionKind.ExpressionKindCount                  , 1)},
{OperatorKind.OP_CALL,            new OPINFO(TokenKind.Unknown             , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 0)},
{OperatorKind.OP_DEREF,           new OPINFO(TokenKind.Unknown             , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 0)},
{OperatorKind.OP_PAREN,           new OPINFO(TokenKind.Unknown             , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 0)},
{OperatorKind.OP_POSTINC,         new OPINFO(TokenKind.PlusPlus            , PredefinedName.PN_COUNT                , ExpressionKind.Add                    , 1)},
{OperatorKind.OP_POSTDEC,         new OPINFO(TokenKind.MinusMinus          , PredefinedName.PN_COUNT                , ExpressionKind.Subtract                    , 1)},
{OperatorKind.OP_DOT,             new OPINFO(TokenKind.Dot                 , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 2)},
{OperatorKind.OP_IMPLICIT,        new OPINFO(TokenKind.Implicit            , PredefinedName.PN_OPIMPLICITMN         , ExpressionKind.ExpressionKindCount                  , 1)},
{OperatorKind.OP_EXPLICIT,        new OPINFO(TokenKind.Explicit            , PredefinedName.PN_OPEXPLICITMN         , ExpressionKind.ExpressionKindCount                  , 1)},
{OperatorKind.OP_EQUALS,          new OPINFO(TokenKind.Unknown             , PredefinedName.PN_OPEQUALS             , ExpressionKind.ExpressionKindCount                  , 2)},
{OperatorKind.OP_COMPARE,         new OPINFO(TokenKind.Unknown             , PredefinedName.PN_OPCOMPARE            , ExpressionKind.ExpressionKindCount                  , 2)},
{OperatorKind.OP_DEFAULT,         new OPINFO(TokenKind.Unknown             , PredefinedName.PN_COUNT                , ExpressionKind.ExpressionKindCount                  , 0)}
        };


        private static OPINFO GetInfo(OperatorKind op)
        {
            //Debug.Assert(IsValid(op));
            return s_rgOpInfo[op];
        }
        public static OperatorKind OperatorOfMethodName(Name name)
        {
            Debug.Assert(name != null);

            for (OperatorKind i = OperatorKind.OP_NONE; i < OperatorKind.OP_LAST; i = (i + 1))
            {
                if (HasMethodName(i) && (name == NameManager.GetPredefinedName(GetMethodName(i))))
                {
                    return i;
                }
            }

            return OperatorKind.OP_NONE;
        }

        private static bool HasMethodName(OperatorKind op)
        {
            //Debug.Assert(IsValid(op));
            return GetMethodName(op) != PredefinedName.PN_COUNT;
        }

        private static PredefinedName GetMethodName(OperatorKind op)
        {
            //Debug.Assert(IsValid(op));
            return GetInfo(op).methodName;
        }

        public static bool HasDisplayName(OperatorKind op)
        {
            //Debug.Assert(IsValid(op));
            return GetInfo(op).iToken != TokenKind.Unknown;
        }
        public static string GetDisplayName(OperatorKind op)
        {
            Debug.Assert(HasDisplayName(op));
            return TokenFacts.GetText(GetInfo(op).iToken);
        }
        public static ExpressionKind GetExpressionKind(OperatorKind op)
        {
            //Debug.Assert(IsValid(op));
            return GetInfo(op).expressionKind;
        }
    }
}
