// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.RegularExpressions
{
    internal enum RegexParseError
    {
        TooManyAlternates,
        IllegalCondition,
        IncompleteSlashP,
        MalformedSlashP,
        UnrecognizedEscape,
        UnrecognizedControl,
        MissingControl,
        TooFewHex,
        CaptureGroupOutOfRange,
        UndefinedNameRef,
        UndefinedBackref,
        MalformedNameRef,
        IllegalEndEscape,
        UnterminatedComment,
        UnrecognizedGrouping,
        AlternationCantCapture,
        AlternationCantHaveComment,
        MalformedReference,
        UndefinedReference,
        InvalidGroupName,
        CapnumNotZero,
        UnterminatedBracket,
        SubtractionMustBeLast,
        ReversedCharRange,
        BadClassInCharRange,
        NotEnoughParentheses,
        IllegalRange,
        NestedQuantify,
        QuantifyAfterNothing,
        TooManyParentheses,
        UnknownUnicodeProperty
    }
}
