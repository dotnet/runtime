// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal static class Operators
    {
        private sealed class OperatorInfo
        {
            public readonly TokenKind TokenKind;
            public readonly PredefinedName MethodName;
            public readonly ExpressionKind ExpressionKind;

            public OperatorInfo(TokenKind kind, PredefinedName pn, ExpressionKind e)
            {
                TokenKind = kind;
                MethodName = pn;
                ExpressionKind = e;
            }
        }

        private static readonly OperatorInfo[] s_operatorInfos = {
            new OperatorInfo(TokenKind.Unknown         , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.Equal           , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.PlusEqual       , PredefinedName.PN_COUNT               , ExpressionKind.MultiOffset + (int)ExpressionKind.Add               ),
            new OperatorInfo(TokenKind.MinusEqual      , PredefinedName.PN_COUNT               , ExpressionKind.MultiOffset + (int)ExpressionKind.Subtract          ),
            new OperatorInfo(TokenKind.SplatEqual      , PredefinedName.PN_COUNT               , ExpressionKind.MultiOffset + (int)ExpressionKind.Multiply          ),
            new OperatorInfo(TokenKind.SlashEqual      , PredefinedName.PN_COUNT               , ExpressionKind.MultiOffset + (int)ExpressionKind.Divide            ),
            new OperatorInfo(TokenKind.PercentEqual    , PredefinedName.PN_COUNT               , ExpressionKind.MultiOffset + (int)ExpressionKind.Modulo            ),
            new OperatorInfo(TokenKind.AndEqual        , PredefinedName.PN_COUNT               , ExpressionKind.MultiOffset + (int)ExpressionKind.BitwiseAnd        ),
            new OperatorInfo(TokenKind.HatEqual        , PredefinedName.PN_COUNT               , ExpressionKind.MultiOffset + (int)ExpressionKind.BitwiseExclusiveOr),
            new OperatorInfo(TokenKind.BarEqual        , PredefinedName.PN_COUNT               , ExpressionKind.MultiOffset + (int)ExpressionKind.BitwiseOr         ),
            new OperatorInfo(TokenKind.LeftShiftEqual  , PredefinedName.PN_COUNT               , ExpressionKind.MultiOffset + (int)ExpressionKind.LeftShirt         ),
            new OperatorInfo(TokenKind.RightShiftEqual , PredefinedName.PN_COUNT               , ExpressionKind.MultiOffset + (int)ExpressionKind.RightShift        ),
            new OperatorInfo(TokenKind.Question        , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.QuestionQuestion, PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.LogicalOr       , PredefinedName.PN_COUNT               , ExpressionKind.LogicalOr                                           ),
            new OperatorInfo(TokenKind.LogicalAnd      , PredefinedName.PN_COUNT               , ExpressionKind.LogicalAnd                                          ),
            new OperatorInfo(TokenKind.Bar             , PredefinedName.PN_OPBITWISEOR         , ExpressionKind.BitwiseOr                                           ),
            new OperatorInfo(TokenKind.Hat             , PredefinedName.PN_OPXOR               , ExpressionKind.BitwiseExclusiveOr                                  ),
            new OperatorInfo(TokenKind.Ampersand       , PredefinedName.PN_OPBITWISEAND        , ExpressionKind.BitwiseAnd                                          ),
            new OperatorInfo(TokenKind.EqualEqual      , PredefinedName.PN_OPEQUALITY          , ExpressionKind.Eq                                                  ),
            new OperatorInfo(TokenKind.NotEqual        , PredefinedName.PN_OPINEQUALITY        , ExpressionKind.NotEq                                               ),
            new OperatorInfo(TokenKind.LessThan        , PredefinedName.PN_OPLESSTHAN          , ExpressionKind.LessThan                                            ),
            new OperatorInfo(TokenKind.LessThanEqual   , PredefinedName.PN_OPLESSTHANOREQUAL   , ExpressionKind.LessThanOrEqual                                     ),
            new OperatorInfo(TokenKind.GreaterThan     , PredefinedName.PN_OPGREATERTHAN       , ExpressionKind.GreaterThan                                         ),
            new OperatorInfo(TokenKind.GreaterThanEqual, PredefinedName.PN_OPGREATERTHANOREQUAL, ExpressionKind.GreaterThanOrEqual                                  ),
            new OperatorInfo(TokenKind.Is              , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.As              , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.LeftShift       , PredefinedName.PN_OPLEFTSHIFT         , ExpressionKind.LeftShirt                                           ),
            new OperatorInfo(TokenKind.RightShift      , PredefinedName.PN_OPRIGHTSHIFT        , ExpressionKind.RightShift                                          ),
            new OperatorInfo(TokenKind.Plus            , PredefinedName.PN_OPPLUS              , ExpressionKind.Add                                                 ),
            new OperatorInfo(TokenKind.Minus           , PredefinedName.PN_OPMINUS             , ExpressionKind.Subtract                                            ),
            new OperatorInfo(TokenKind.Splat           , PredefinedName.PN_OPMULTIPLY          , ExpressionKind.Multiply                                            ),
            new OperatorInfo(TokenKind.Slash           , PredefinedName.PN_OPDIVISION          , ExpressionKind.Divide                                              ),
            new OperatorInfo(TokenKind.Percent         , PredefinedName.PN_OPMODULUS           , ExpressionKind.Modulo                                              ),
            new OperatorInfo(TokenKind.Unknown         , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.Plus            , PredefinedName.PN_OPUNARYPLUS         , ExpressionKind.UnaryPlus                                           ),
            new OperatorInfo(TokenKind.Minus           , PredefinedName.PN_OPUNARYMINUS        , ExpressionKind.Negate                                              ),
            new OperatorInfo(TokenKind.Tilde           , PredefinedName.PN_OPCOMPLEMENT        , ExpressionKind.BitwiseNot                                          ),
            new OperatorInfo(TokenKind.Bang            , PredefinedName.PN_OPNEGATION          , ExpressionKind.LogicalNot                                          ),
            new OperatorInfo(TokenKind.PlusPlus        , PredefinedName.PN_OPINCREMENT         , ExpressionKind.Add                                                 ),
            new OperatorInfo(TokenKind.MinusMinus      , PredefinedName.PN_OPDECREMENT         , ExpressionKind.Subtract                                            ),
            new OperatorInfo(TokenKind.TypeOf          , PredefinedName.PN_COUNT               , ExpressionKind.TypeOf                                              ),
            new OperatorInfo(TokenKind.Checked         , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.Unchecked       , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.MakeRef         , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.RefValue        , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.RefType         , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.ArgList         , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.Unknown         , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.Splat           , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.Ampersand       , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.Colon           , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.This            , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.Base            , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.Null            , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.True            , PredefinedName.PN_OPTRUE              , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.False           , PredefinedName.PN_OPFALSE             , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.Unknown         , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.Unknown         , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.Unknown         , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.PlusPlus        , PredefinedName.PN_COUNT               , ExpressionKind.Add                                                 ),
            new OperatorInfo(TokenKind.MinusMinus      , PredefinedName.PN_COUNT               , ExpressionKind.Subtract                                            ),
            new OperatorInfo(TokenKind.Dot             , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.Implicit        , PredefinedName.PN_OPIMPLICITMN        , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.Explicit        , PredefinedName.PN_OPEXPLICITMN        , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.Unknown         , PredefinedName.PN_OPEQUALS            , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.Unknown         , PredefinedName.PN_OPCOMPARE           , ExpressionKind.ExpressionKindCount                                 ),
            new OperatorInfo(TokenKind.Unknown         , PredefinedName.PN_COUNT               , ExpressionKind.ExpressionKindCount                                 )
        };

        private static Dictionary<Name, string> s_operatorsByName;

        private static Dictionary<Name, string> GetOperatorByName()
        {
            Dictionary<Name, string> dict = new Dictionary<Name, string>(28)
            {
                {NameManager.GetPredefinedName(PredefinedName.PN_OPEQUALS), "equals"},
                {NameManager.GetPredefinedName(PredefinedName.PN_OPCOMPARE), "compare" }
            };

            foreach (OperatorInfo opInfo in s_operatorInfos)
            {
                PredefinedName predefName = opInfo.MethodName;
                TokenKind token = opInfo.TokenKind;
                if (predefName != PredefinedName.PN_COUNT && token != TokenKind.Unknown)
                {
                    dict.Add(NameManager.GetPredefinedName(predefName), TokenFacts.GetText(token));
                }
            }

            return dict;
        }

        private static OperatorInfo GetInfo(OperatorKind op) => s_operatorInfos[(int)op];

        public static string OperatorOfMethodName(Name name) =>
            (s_operatorsByName ?? (s_operatorsByName = GetOperatorByName()))[name];

        public static string GetDisplayName(OperatorKind op) => TokenFacts.GetText(GetInfo(op).TokenKind);

        public static ExpressionKind GetExpressionKind(OperatorKind op) => GetInfo(op).ExpressionKind;
    }
}
