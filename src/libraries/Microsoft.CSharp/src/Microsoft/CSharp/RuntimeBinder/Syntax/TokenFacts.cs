// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CSharp.RuntimeBinder.Syntax
{
    internal static class TokenFacts
    {
        internal static string GetText(TokenKind kind) =>
            kind switch
            {
                TokenKind.ArgList => "__arglist",
                TokenKind.MakeRef => "__makeref",
                TokenKind.RefType => "__reftype",
                TokenKind.RefValue => "__refvalue",
                TokenKind.As => "as",
                TokenKind.Base => "base",
                TokenKind.Checked => "checked",
                TokenKind.Explicit => "explicit",
                TokenKind.False => "false",
                TokenKind.Implicit => "implicit",
                TokenKind.Is => "is",
                TokenKind.Null => "null",
                TokenKind.This => "this",
                TokenKind.True => "true",
                TokenKind.TypeOf => "typeof",
                TokenKind.Unchecked => "unchecked",
                TokenKind.Void => "void",
                TokenKind.Equal => "=",
                TokenKind.PlusEqual => "+=",
                TokenKind.MinusEqual => "-=",
                TokenKind.SplatEqual => "*=",
                TokenKind.SlashEqual => "/=",
                TokenKind.PercentEqual => "%=",
                TokenKind.AndEqual => "&=",
                TokenKind.HatEqual => "^=",
                TokenKind.BarEqual => "|=",
                TokenKind.LeftShiftEqual => "<<=",
                TokenKind.RightShiftEqual => ">>=",
                TokenKind.Question => "?",
                TokenKind.Colon => ":",
                TokenKind.ColonColon => "::",
                TokenKind.LogicalOr => "||",
                TokenKind.LogicalAnd => "&&",
                TokenKind.Bar => "|",
                TokenKind.Hat => "^",
                TokenKind.Ampersand => "&",
                TokenKind.EqualEqual => "==",
                TokenKind.NotEqual => "!=",
                TokenKind.LessThan => "<",
                TokenKind.LessThanEqual => "<=",
                TokenKind.GreaterThan => ">",
                TokenKind.GreaterThanEqual => ">=",
                TokenKind.LeftShift => "<<",
                TokenKind.RightShift => ">>",
                TokenKind.Plus => "+",
                TokenKind.Minus => "-",
                TokenKind.Splat => "*",
                TokenKind.Slash => "/",
                TokenKind.Percent => "%",
                TokenKind.Tilde => "~",
                TokenKind.Bang => "!",
                TokenKind.PlusPlus => "++",
                TokenKind.MinusMinus => "--",
                TokenKind.Dot => ".",
                TokenKind.QuestionQuestion => "??",
                _ => throw Error.InternalCompilerError(),
            };
    }
}
