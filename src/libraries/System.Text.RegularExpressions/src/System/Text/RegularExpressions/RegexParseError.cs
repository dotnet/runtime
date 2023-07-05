// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// Specifies the detailed underlying reason why a <see cref="RegexParseException"/> is thrown when a
    /// regular expression contains a parsing error.
    /// </summary>
    /// <remarks>
    /// This information is made available through <see cref="RegexParseException.Error"/>.
    /// </remarks>
#if SYSTEM_TEXT_REGULAREXPRESSIONS
    public
#else
    internal
#endif
    enum RegexParseError
    {
        /// <summary>
        /// An unknown regular expression parse error.
        /// </summary>
        Unknown,
        /// <summary>
        /// An alternation in a regular expression has too many conditions.
        /// </summary>
        AlternationHasTooManyConditions,
        /// <summary>
        /// An alternation in a regular expression has a malformed condition.
        /// </summary>
        AlternationHasMalformedCondition,
        /// <summary>
        /// A Unicode property escape in a regular expression is invalid or unknown.
        /// </summary>
        InvalidUnicodePropertyEscape,
        /// <summary>
        /// A Unicode property escape is malformed.
        /// </summary>
        MalformedUnicodePropertyEscape,
        /// <summary>
        /// An escape character or sequence in a regular expression is invalid.
        /// </summary>
        UnrecognizedEscape,
        /// <summary>
        /// A control character in a regular expression is not recognized.
        /// </summary>
        UnrecognizedControlCharacter,
        /// <summary>
        /// A control character in a regular expression is missing.
        /// </summary>
        MissingControlCharacter,
        /// <summary>
        /// A hexadecimal escape sequence in a regular expression does not have enough digits, or contains invalid digits.
        /// </summary>
        InsufficientOrInvalidHexDigits,
        /// <summary>
        /// A captured group or a quantifier in a regular expression is not within range, that is, it is larger than <see cref="int.MaxValue"/>.
        /// </summary>
        QuantifierOrCaptureGroupOutOfRange,
        /// <summary>
        /// A used named reference in a regular expression is not defined.
        /// </summary>
        UndefinedNamedReference,
        /// <summary>
        /// A used numbered reference in a regular expression is not defined.
        /// </summary>
        UndefinedNumberedReference,
        /// <summary>
        /// A named reference in a regular expression is malformed.
        /// </summary>
        MalformedNamedReference,
        /// <summary>
        /// A regular expression ends with a non-escaped ending backslash.
        /// </summary>
        UnescapedEndingBackslash,
        /// <summary>
        /// A comment in a regular expression is not terminated.
        /// </summary>
        UnterminatedComment,
        /// <summary>
        /// A grouping construct in a regular expression is invalid or malformed.
        /// </summary>
        InvalidGroupingConstruct,
        /// <summary>
        /// An alternation construct in a regular expression uses a named capture.
        /// </summary>
        AlternationHasNamedCapture,
        /// <summary>
        /// An alternation construct in a regular expression contains a comment.
        /// </summary>
        AlternationHasComment,
        /// <summary>
        /// An alternation construct in a regular expression contains a malformed reference.
        /// </summary>
        AlternationHasMalformedReference,
        /// <summary>
        /// An alternation construct in a regular expression contains an undefined reference.
        /// </summary>
        AlternationHasUndefinedReference,
        /// <summary>
        /// The group name of a captured group in a regular expression is invalid.
        /// </summary>
        CaptureGroupNameInvalid,
        /// <summary>
        /// A regular expression defines a numbered subexpression named zero.
        /// </summary>
        CaptureGroupOfZero,
        /// <summary>
        /// A regular expression has a non-escaped left square bracket, or misses a closing right square bracket.
        /// </summary>
        UnterminatedBracket,
        /// <summary>
        /// A character class in a regular expression with an exclusion group is not the last part of the character class.
        /// </summary>
        ExclusionGroupNotLast,
        /// <summary>
        /// A character class in a regular expression contains an inverse character range, like z-a instead of a-z.
        /// </summary>
        ReversedCharacterRange,
        /// <summary>
        /// A character-class in a regular expression contains a short-hand class that is not allowed inside a character class.
        /// </summary>
        ShorthandClassInCharacterRange,
        /// <summary>
        /// A regular expression has a non-escaped left parenthesis, or misses a closing right parenthesis.
        /// </summary>
        InsufficientClosingParentheses,
        /// <summary>
        /// A quantifier range in a regular expression is inverse, like <code>{10,1}</code> instead of <code>(1,10}</code>.
        /// </summary>
        ReversedQuantifierRange,
        /// <summary>
        /// Repeated quantifiers on another quantifier inside a regular expression are not grouped in parentheses.
        /// </summary>
        NestedQuantifiersNotParenthesized,
        /// <summary>
        /// A quantifier in a regular expression is in a position where it cannot quantify anything, like at the beginning of a regular expression or in a group.
        /// </summary>
        QuantifierAfterNothing,
        /// <summary>
        /// A regular expression has a non-escaped right parenthesis, or misses an opening left parenthesis.
        /// </summary>
        InsufficientOpeningParentheses,
        /// <summary>
        /// A unicode property in a regular expression is not recognized, or invalid.
        /// </summary>
        UnrecognizedUnicodeProperty
    }
}
