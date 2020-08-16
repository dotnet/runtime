// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions
{
    public enum RegexParseError
    {
        Unknown,
        AlternationHasTooManyConditions,
        AlternationHasMalformedCondition,
        InvalidUnicodePropertyEscape,
        MalformedUnicodePropertyEscape,
        UnrecognizedEscape,
        UnrecognizedControlCharacter,
        MissingControlCharacter,
        InsufficientOrInvalidHexDigits,
        QuantifierOrCaptureGroupOutOfRange,
        UndefinedNamedReference,
        UndefinedNumberedReference,
        MalformedNamedReference,
        UnescapedEndingBackslash,
        UnterminatedComment,
        InvalidGroupingConstruct,
        AlternationHasNamedCapture,
        AlternationHasComment,
        AlternationHasMalformedReference,
        AlternationHasUndefinedReference,
        CaptureGroupNameInvalid,
        CaptureGroupOfZero,
        UnterminatedBracket,
        ExclusionGroupNotLast,
        ReversedCharacterRange,
        ShorthandClassInCharacterRange,
        InsufficientClosingParentheses,
        ReversedQuantifierRange,
        NestedQuantifiersNotParenthesized,
        QuantifierAfterNothing,
        InsufficientOpeningParentheses,
        UnrecognizedUnicodeProperty
    }
}
