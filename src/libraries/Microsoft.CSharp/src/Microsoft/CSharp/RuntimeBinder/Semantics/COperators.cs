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
{OperatorKind.OP_NONE,            new OPINFO(TokenKind.Unknown             , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 0)},
{OperatorKind.OP_ASSIGN,          new OPINFO(TokenKind.Equal               , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 2)},
{OperatorKind.OP_ADDEQ,           new OPINFO(TokenKind.PlusEqual           , PredefinedName.PN_COUNT                , ExpressionKind.EK_MULTIOFFSET + (int)ExpressionKind.EK_ADD   , 2)},
{OperatorKind.OP_SUBEQ,           new OPINFO(TokenKind.MinusEqual          , PredefinedName.PN_COUNT                , ExpressionKind.EK_MULTIOFFSET + (int)ExpressionKind.EK_SUB   , 2)},
{OperatorKind.OP_MULEQ,           new OPINFO(TokenKind.SplatEqual          , PredefinedName.PN_COUNT                , ExpressionKind.EK_MULTIOFFSET + (int)ExpressionKind.EK_MUL   , 2)},
{OperatorKind.OP_DIVEQ,           new OPINFO(TokenKind.SlashEqual          , PredefinedName.PN_COUNT                , ExpressionKind.EK_MULTIOFFSET + (int)ExpressionKind.EK_DIV   , 2)},
{OperatorKind.OP_MODEQ,           new OPINFO(TokenKind.PercentEqual        , PredefinedName.PN_COUNT                , ExpressionKind.EK_MULTIOFFSET + (int)ExpressionKind.EK_MOD   , 2)},
{OperatorKind.OP_ANDEQ,           new OPINFO(TokenKind.AndEqual            , PredefinedName.PN_COUNT                , ExpressionKind.EK_MULTIOFFSET + (int)ExpressionKind.EK_BITAND, 2)},
{OperatorKind.OP_XOREQ,           new OPINFO(TokenKind.HatEqual            , PredefinedName.PN_COUNT                , ExpressionKind.EK_MULTIOFFSET + (int)ExpressionKind.EK_BITXOR, 2)},
{OperatorKind.OP_OREQ,            new OPINFO(TokenKind.BarEqual            , PredefinedName.PN_COUNT                , ExpressionKind.EK_MULTIOFFSET + (int)ExpressionKind.EK_BITOR , 2)},
{OperatorKind.OP_LSHIFTEQ,        new OPINFO(TokenKind.LeftShiftEqual      , PredefinedName.PN_COUNT                , ExpressionKind.EK_MULTIOFFSET + (int)ExpressionKind.EK_LSHIFT, 2)},
{OperatorKind.OP_RSHIFTEQ,        new OPINFO(TokenKind.RightShiftEqual     , PredefinedName.PN_COUNT                , ExpressionKind.EK_MULTIOFFSET + (int)ExpressionKind.EK_RSHIFT, 2)},
{OperatorKind.OP_QUESTION,        new OPINFO(TokenKind.Question            , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 2)},
{OperatorKind.OP_VALORDEF,        new OPINFO(TokenKind.QuestionQuestion    , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 2)},
{OperatorKind.OP_LOGOR,           new OPINFO(TokenKind.LogicalOr           , PredefinedName.PN_COUNT                , ExpressionKind.EK_LOGOR                  , 2)},
{OperatorKind.OP_LOGAND,          new OPINFO(TokenKind.LogicalAnd          , PredefinedName.PN_COUNT                , ExpressionKind.EK_LOGAND                 , 2)},
{OperatorKind.OP_BITOR,           new OPINFO(TokenKind.Bar                 , PredefinedName.PN_OPBITWISEOR          , ExpressionKind.EK_BITOR                  , 2)},
{OperatorKind.OP_BITXOR,          new OPINFO(TokenKind.Hat                 , PredefinedName.PN_OPXOR                , ExpressionKind.EK_BITXOR                 , 2)},
{OperatorKind.OP_BITAND,          new OPINFO(TokenKind.Ampersand           , PredefinedName.PN_OPBITWISEAND         , ExpressionKind.EK_BITAND                 , 2)},
{OperatorKind.OP_EQ,              new OPINFO(TokenKind.EqualEqual          , PredefinedName.PN_OPEQUALITY           , ExpressionKind.EK_EQ                     , 2)},
{OperatorKind.OP_NEQ,             new OPINFO(TokenKind.NotEqual            , PredefinedName.PN_OPINEQUALITY         , ExpressionKind.EK_NE                     , 2)},
{OperatorKind.OP_LT,              new OPINFO(TokenKind.LessThan            , PredefinedName.PN_OPLESSTHAN           , ExpressionKind.EK_LT                     , 2)},
{OperatorKind.OP_LE,              new OPINFO(TokenKind.LessThanEqual       , PredefinedName.PN_OPLESSTHANOREQUAL    , ExpressionKind.EK_LE                     , 2)},
{OperatorKind.OP_GT,              new OPINFO(TokenKind.GreaterThan         , PredefinedName.PN_OPGREATERTHAN        , ExpressionKind.EK_GT                     , 2)},
{OperatorKind.OP_GE,              new OPINFO(TokenKind.GreaterThanEqual    , PredefinedName.PN_OPGREATERTHANOREQUAL , ExpressionKind.EK_GE                     , 2)},
{OperatorKind.OP_IS,              new OPINFO(TokenKind.Is                  , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 2)},
{OperatorKind.OP_AS,              new OPINFO(TokenKind.As                  , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 2)},
{OperatorKind.OP_LSHIFT,          new OPINFO(TokenKind.LeftShift           , PredefinedName.PN_OPLEFTSHIFT          , ExpressionKind.EK_LSHIFT                 , 2)},
{OperatorKind.OP_RSHIFT,          new OPINFO(TokenKind.RightShift          , PredefinedName.PN_OPRIGHTSHIFT         , ExpressionKind.EK_RSHIFT                 , 2)},
{OperatorKind.OP_ADD,             new OPINFO(TokenKind.Plus                , PredefinedName.PN_OPPLUS               , ExpressionKind.EK_ADD                    , 2)},
{OperatorKind.OP_SUB,             new OPINFO(TokenKind.Minus               , PredefinedName.PN_OPMINUS              , ExpressionKind.EK_SUB                    , 2)},
{OperatorKind.OP_MUL,             new OPINFO(TokenKind.Splat               , PredefinedName.PN_OPMULTIPLY           , ExpressionKind.EK_MUL                    , 2)},
{OperatorKind.OP_DIV,             new OPINFO(TokenKind.Slash               , PredefinedName.PN_OPDIVISION           , ExpressionKind.EK_DIV                    , 2)},
{OperatorKind.OP_MOD,             new OPINFO(TokenKind.Percent             , PredefinedName.PN_OPMODULUS            , ExpressionKind.EK_MOD                    , 2)},
{OperatorKind.OP_NOP,             new OPINFO(TokenKind.Unknown             , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 1)},
{OperatorKind.OP_UPLUS,           new OPINFO(TokenKind.Plus                , PredefinedName.PN_OPUNARYPLUS          , ExpressionKind.EK_UPLUS                  , 1)},
{OperatorKind.OP_NEG,             new OPINFO(TokenKind.Minus               , PredefinedName.PN_OPUNARYMINUS         , ExpressionKind.EK_NEG                    , 1)},
{OperatorKind.OP_BITNOT,          new OPINFO(TokenKind.Tilde               , PredefinedName.PN_OPCOMPLEMENT         , ExpressionKind.EK_BITNOT                 , 1)},
{OperatorKind.OP_LOGNOT,          new OPINFO(TokenKind.Bang                , PredefinedName.PN_OPNEGATION           , ExpressionKind.EK_LOGNOT                 , 1)},
{OperatorKind.OP_PREINC,          new OPINFO(TokenKind.PlusPlus            , PredefinedName.PN_OPINCREMENT          , ExpressionKind.EK_ADD                    , 1)},
{OperatorKind.OP_PREDEC,          new OPINFO(TokenKind.MinusMinus          , PredefinedName.PN_OPDECREMENT          , ExpressionKind.EK_SUB                    , 1)},
{OperatorKind.OP_TYPEOF,          new OPINFO(TokenKind.TypeOf              , PredefinedName.PN_COUNT                , ExpressionKind.EK_TYPEOF                 , 1)},
{OperatorKind.OP_CHECKED,         new OPINFO(TokenKind.Checked             , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 1)},
{OperatorKind.OP_UNCHECKED,       new OPINFO(TokenKind.Unchecked           , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 1)},
{OperatorKind.OP_MAKEREFANY,      new OPINFO(TokenKind.MakeRef             , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 1)},
{OperatorKind.OP_REFVALUE,        new OPINFO(TokenKind.RefValue            , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 2)},
{OperatorKind.OP_REFTYPE,         new OPINFO(TokenKind.RefType             , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 1)},
{OperatorKind.OP_ARGS,            new OPINFO(TokenKind.ArgList             , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 0)},
{OperatorKind.OP_CAST,            new OPINFO(TokenKind.Unknown             , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 2)},
{OperatorKind.OP_INDIR,           new OPINFO(TokenKind.Splat               , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 1)},
{OperatorKind.OP_ADDR,            new OPINFO(TokenKind.Ampersand           , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 1)},
{OperatorKind.OP_COLON,           new OPINFO(TokenKind.Colon               , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 2)},
{OperatorKind.OP_THIS,            new OPINFO(TokenKind.This                , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 0)},
{OperatorKind.OP_BASE,            new OPINFO(TokenKind.Base                , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 0)},
{OperatorKind.OP_NULL,            new OPINFO(TokenKind.Null                , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 0)},
{OperatorKind.OP_TRUE,            new OPINFO(TokenKind.True                , PredefinedName.PN_OPTRUE               , ExpressionKind.EK_COUNT                  , 1)},
{OperatorKind.OP_FALSE,           new OPINFO(TokenKind.False               , PredefinedName.PN_OPFALSE              , ExpressionKind.EK_COUNT                  , 1)},
{OperatorKind.OP_CALL,            new OPINFO(TokenKind.Unknown             , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 0)},
{OperatorKind.OP_DEREF,           new OPINFO(TokenKind.Unknown             , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 0)},
{OperatorKind.OP_PAREN,           new OPINFO(TokenKind.Unknown             , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 0)},
{OperatorKind.OP_POSTINC,         new OPINFO(TokenKind.PlusPlus            , PredefinedName.PN_COUNT                , ExpressionKind.EK_ADD                    , 1)},
{OperatorKind.OP_POSTDEC,         new OPINFO(TokenKind.MinusMinus          , PredefinedName.PN_COUNT                , ExpressionKind.EK_SUB                    , 1)},
{OperatorKind.OP_DOT,             new OPINFO(TokenKind.Dot                 , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 2)},
{OperatorKind.OP_IMPLICIT,        new OPINFO(TokenKind.Implicit            , PredefinedName.PN_OPIMPLICITMN         , ExpressionKind.EK_COUNT                  , 1)},
{OperatorKind.OP_EXPLICIT,        new OPINFO(TokenKind.Explicit            , PredefinedName.PN_OPEXPLICITMN         , ExpressionKind.EK_COUNT                  , 1)},
{OperatorKind.OP_EQUALS,          new OPINFO(TokenKind.Unknown             , PredefinedName.PN_OPEQUALS             , ExpressionKind.EK_COUNT                  , 2)},
{OperatorKind.OP_COMPARE,         new OPINFO(TokenKind.Unknown             , PredefinedName.PN_OPCOMPARE            , ExpressionKind.EK_COUNT                  , 2)},
{OperatorKind.OP_DEFAULT,         new OPINFO(TokenKind.Unknown             , PredefinedName.PN_COUNT                , ExpressionKind.EK_COUNT                  , 0)}
        };


        private static OPINFO GetInfo(OperatorKind op)
        {
            //Debug.Assert(IsValid(op));
            return s_rgOpInfo[op];
        }
        public static OperatorKind OperatorOfMethodName(NameManager namemgr, Name name)
        {
            Debug.Assert(name != null);

            for (OperatorKind i = OperatorKind.OP_NONE; i < OperatorKind.OP_LAST; i = (i + 1))
            {
                if (HasMethodName(i) && (name == GetMethodName(namemgr, i)))
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

        private static Name GetMethodName(NameManager namemgr, OperatorKind op)
        {
            Debug.Assert(HasMethodName(op));
            return namemgr.GetPredefName(GetMethodName(op));
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
