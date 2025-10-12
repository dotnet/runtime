// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;
using Xunit.Sdk;

namespace System.Text.RegularExpressions.Tests
{
    public partial class RegexParserTests
    {
        [Theory]

        // Avoid OutOfMemoryException
        [InlineData("a{2147483647}", RegexOptions.None, null)]
        [InlineData("a{2147483647,}", RegexOptions.None, null)]

        [InlineData(@"(?(?N))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 5)]
        [InlineData(@"(?(?i))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 5)]
        [InlineData(@"(?(?I))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 5)]
        [InlineData(@"(?(?M))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 5)]
        [InlineData(@"(?(?s))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 5)]
        [InlineData(@"(?(?S))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 5)]
        [InlineData(@"(?(?x))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 5)]
        [InlineData(@"(?(?X))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 5)]
        [InlineData(@"(?(?n))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 5)]
        [InlineData(@"(?(?m))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 5)]
        [InlineData("(?<-", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 3)]
        [InlineData("(?<-", RegexOptions.IgnorePatternWhitespace, RegexParseError.InvalidGroupingConstruct, 3)]
        [InlineData(@"^[^<>]*(((?'Open'<)[^<>]*)+((?'Close-Open'>)[^<>]*)+)*(?(Open)(?!))$", RegexOptions.None, null)]
        [InlineData(@"((?'Close-Open'>)[^<>]*)+", RegexOptions.None, RegexParseError.UndefinedNamedReference, 14)]
        [InlineData(@"(((?'Open'<)[^<>]*)+((?'Close-Open'>)[^<>]*)+)*", RegexOptions.None, null)]
        [InlineData(@"(?'Close-Open'>)", RegexOptions.None, RegexParseError.UndefinedNamedReference, 13)]
        [InlineData("(?<a-00>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<a>)()(?<-0>)(?<-1>)(?<-2>)(?<-3>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference, 34)]
        [InlineData("()(?<a>)(?<-0>)(?<-1>)(?<-2>)(?<-3>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference, 34)]
        [InlineData("()()(?<-0>)(?<-1>)(?<-2>)(?<-3>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference, 30)]
        [InlineData("(?<a>)(?<b>)(?<-1>)(?<-2>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<-4>)(?<4>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<4>)(?<-4>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<a>)(?<-a>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<-a>)(?<a>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<a-0>", RegexOptions.IgnorePatternWhitespace, RegexParseError.InsufficientClosingParentheses, 7)]
        [InlineData("(?<a-0>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<a-0 >)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid, 6)]
        [InlineData("(?<a- 0 >)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid, 5)]
        [InlineData("(?<a- 0>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid, 5)]
        [InlineData("(?<-1>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference, 5)]
        [InlineData("()(?<-1>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<-1>)()", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<-00>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<a-", RegexOptions.IgnorePatternWhitespace, RegexParseError.InvalidGroupingConstruct, 5)]
        [InlineData("(?<a-0", RegexOptions.IgnorePatternWhitespace, RegexParseError.InvalidGroupingConstruct, 6)]
        [InlineData("(?<a-0)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid, 6)]
        [InlineData("(?<a>)(?<b>)(?<-0>)(?<-1>)(?<-2>)(?<-3>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference, 38)]
        [InlineData("(?<-0>)(?<-1>)(?<-2>)(?<-3>)()()", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference, 26)]
        [InlineData("(?<-0>)(?<-1>)(?<-2>)(?<-3>)()(?", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference, 19)]
        [InlineData("(?<-0>)(?<-1>)(?<-2>)(?<-3>)()(?<a>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference, 26)]
        [InlineData("(?<-0>)(?<-1>)(?<-2>)(?<-3>)(?<a>)()", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference, 26)]
        [InlineData("(?<-0>)(?<-1>)(?<-2>)(?<-3>)(?<a>)(?", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference, 19)]
        [InlineData("(?<-0>)(?<-1>)(?<-2>)(?<-3>)(?<a>)(?<b>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference, 26)]
        [InlineData("(?<a-0>)(?<b-a>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<a-0>)(?<-a>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<a-a>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<-0>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<-0 >)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid, 5)]
        [InlineData("(?<- 0 >)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid, 4)]
        [InlineData("(?<- 0>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid, 4)]
        [InlineData("(?<a-0')", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid, 6)]
        [InlineData("(?'a-0>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid, 6)]
        [InlineData("(?'-0')", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?'a-0')", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<-0", RegexOptions.IgnorePatternWhitespace, RegexParseError.InvalidGroupingConstruct, 5)]
        [InlineData("(?<-0)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid, 5)]
        [InlineData("(?<-0>", RegexOptions.IgnorePatternWhitespace, RegexParseError.InsufficientClosingParentheses, 6)]
        [InlineData(@"(?<cat>cat)\w+(?<dog-()*!@>dog)", RegexOptions.None, RegexParseError.CaptureGroupNameInvalid, 21)]
        [InlineData(@"(?<cat>cat)\w+(?<dog-catdog>dog)", RegexOptions.None, RegexParseError.UndefinedNamedReference, 27)]
        [InlineData(@"(?<cat>cat)\w+(?<dog-1uosn>dog)", RegexOptions.None, RegexParseError.CaptureGroupNameInvalid, 22)]
        [InlineData(@"(?<cat>cat)\w+(?<dog-16>dog)", RegexOptions.None, RegexParseError.UndefinedNumberedReference, 23)]
        [InlineData(@"cat(?<->dog)", RegexOptions.None, RegexParseError.CaptureGroupNameInvalid, 7)]
        [InlineData("a{2147483648}", RegexOptions.None, RegexParseError.QuantifierOrCaptureGroupOutOfRange, 12)]
        [InlineData("a{2147483648,}", RegexOptions.None, RegexParseError.QuantifierOrCaptureGroupOutOfRange, 12)]
        [InlineData("a{0,2147483647}", RegexOptions.None, null)]
        [InlineData("a{0,2147483648}", RegexOptions.None, RegexParseError.QuantifierOrCaptureGroupOutOfRange, 14)]
        // Surrogate pair which is parsed as [char,char-char,char] as we operate on UTF-16 code units.
        [InlineData("[\uD82F\uDCA0-\uD82F\uDCA3]", RegexOptions.IgnoreCase, RegexParseError.ReversedCharacterRange, 5)]
        // [ inside a range is treated literally
        [InlineData(@"[[::]", RegexOptions.None, null)]
        [InlineData(@"[[:X:]", RegexOptions.None, null)]
        [InlineData(@"[[:ab:]", RegexOptions.None, null)]

        // Following are borrowed from Rust regex tests ============
        // https://github.com/rust-lang/regex/blob/master/tests/noparse.rs
        [InlineData(@"*", RegexOptions.None, RegexParseError.QuantifierAfterNothing, 1)]
        [InlineData(@"[A-", RegexOptions.None, RegexParseError.UnterminatedBracket, 3)]
        [InlineData(@"[A", RegexOptions.None, RegexParseError.UnterminatedBracket, 2)]
        [InlineData(@"[\A]", RegexOptions.None, RegexParseError.UnrecognizedEscape, 3)]
        [InlineData(@"[\z]", RegexOptions.None, RegexParseError.UnrecognizedEscape, 3)]
        [InlineData(@"(", RegexOptions.None, RegexParseError.InsufficientClosingParentheses, 1)]
        [InlineData(@")", RegexOptions.None, RegexParseError.InsufficientOpeningParentheses, 1)]
        [InlineData(@"[a-Z]", RegexOptions.None, RegexParseError.ReversedCharacterRange, 4)]
        [InlineData(@"(?P<>a)", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 3)]
        [InlineData(@"(?P<na-me>)", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 3)]
        [InlineData(@"(?a)a", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 3)]
        [InlineData(@"a{2,1}", RegexOptions.None, RegexParseError.ReversedQuantifierRange, 6)]
        [InlineData(@"(?", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 2)]
        [InlineData(@"\8", RegexOptions.None, RegexParseError.UndefinedNumberedReference, 2)]
        [InlineData(@"\xG0", RegexOptions.None, RegexParseError.InsufficientOrInvalidHexDigits, 3)]
        [InlineData(@"\xF", RegexOptions.None, RegexParseError.InsufficientOrInvalidHexDigits, 2)]
        [InlineData(@"\x{fffg}", RegexOptions.None, RegexParseError.InsufficientOrInvalidHexDigits, 3)]
        [InlineData(@"(?a)", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 3)]
        [InlineData(@"(?)", RegexOptions.None, RegexParseError.QuantifierAfterNothing, 2)]
        [InlineData(@"(?P<a>.)(?P<a>.)", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 3)]
        [InlineData(@"[a-\A]", RegexOptions.None, RegexParseError.UnrecognizedEscape, 5)]
        [InlineData(@"[a-\z]", RegexOptions.None, RegexParseError.UnrecognizedEscape, 5)]
        [InlineData(@"[a-\b]", RegexOptions.None, RegexParseError.ReversedCharacterRange, 5)] // Nim: not an error
        [InlineData(@"[a-\-]", RegexOptions.None, RegexParseError.ReversedCharacterRange, 5)]
        [InlineData(@"[a-\-b]", RegexOptions.None, RegexParseError.ReversedCharacterRange, 5)]
        [InlineData(@"[a-\-\-b]", RegexOptions.None, RegexParseError.ReversedCharacterRange, 5)]
        [InlineData(@"[a-\-\D]", RegexOptions.None, RegexParseError.ReversedCharacterRange, 5)]
        [InlineData(@"[a-\-\-\D]", RegexOptions.None, RegexParseError.ReversedCharacterRange, 5)]
        [InlineData(@"[a -\-\b]", RegexOptions.None, null)]
        [InlineData(@"[\b]", RegexOptions.None, null)] // errors in rust: class_no_boundary
        [InlineData(@"a{10000000}", RegexOptions.None, null)] // errors in rust: too_big
        [InlineData(@"a{1001", RegexOptions.None, null)] // errors in rust: counted_no_close
        [InlineData(@"a{-1,1}", RegexOptions.None, null)] // errors in rust: counted_nonnegative
        [InlineData(@"\\", RegexOptions.None, null)] // errors in rust: unfinished_escape
        [InlineData(@"(?-i-i)", RegexOptions.None, null)] // errors in rust: double_neg
        [InlineData(@"(?i-)", RegexOptions.None, null)] // errors in rust: neg_empty
        [InlineData(@"[a-[:lower:]]", RegexOptions.None, null)] // errors in rust: range_end_no_class
        // End of Rust parser tests ==============

        // Following are borrowed from Nim tests
        // https://github.com/nitely/nim-regex/blob/eeefb4f51264ff3bc3b36caf55672a74f52f5ef5/tests/tests.nim
        [InlineData(@"?", RegexOptions.None, RegexParseError.QuantifierAfterNothing, 1)]
        [InlineData(@"?|?", RegexOptions.None, RegexParseError.QuantifierAfterNothing, 1)]
        [InlineData(@"?abc", RegexOptions.None, RegexParseError.QuantifierAfterNothing, 1)]
        [InlineData(@"(?P<abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_>abc", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 3)] // Nim: not an error
        [InlineData(@"(?Pabc", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 3)]
        [InlineData(@"(?u-q)", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 3)]
        [InlineData(@"(?uq)", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 3)]
        [InlineData(@"(\b)", RegexOptions.None, null)]
        [InlineData(@"(+)", RegexOptions.None, RegexParseError.QuantifierAfterNothing, 2)]
        [InlineData(@"(a)b)", RegexOptions.None, RegexParseError.InsufficientOpeningParentheses, 5)]
        [InlineData(@"(b(a)", RegexOptions.None, RegexParseError.InsufficientClosingParentheses, 5)]
        [InlineData(@"[-", RegexOptions.None, RegexParseError.UnterminatedBracket, 2)]
        [InlineData(@"[-a", RegexOptions.None, RegexParseError.UnterminatedBracket, 3)]
        [InlineData(@"[[:abc:]]", RegexOptions.None, null)] // Nim: "Invalid ascii set. `abc` is not a valid name"
        [InlineData(@"[[:alnum:", RegexOptions.None, RegexParseError.UnterminatedBracket, 9)]
        [InlineData(@"[[:alnum]]", RegexOptions.None, null)] // Nim: "Invalid ascii set. Expected [:name:]"
        [InlineData(@"[]", RegexOptions.None, RegexParseError.UnterminatedBracket, 2)]
        [InlineData(@"[]a", RegexOptions.None, RegexParseError.UnterminatedBracket, 3)]
        [InlineData(@"[]abc", RegexOptions.None, RegexParseError.UnterminatedBracket, 5)]
        [InlineData(@"[\\", RegexOptions.None, RegexParseError.UnterminatedBracket, 3)]
        [InlineData(@"[^]", RegexOptions.None, RegexParseError.UnterminatedBracket, 3)]
        [InlineData(@"[a-", RegexOptions.None, RegexParseError.UnterminatedBracket, 3)]
        [InlineData(@"[a-\w]", RegexOptions.None, RegexParseError.ShorthandClassInCharacterRange, 5)]
        [InlineData(@"[a", RegexOptions.None, RegexParseError.UnterminatedBracket, 2)]
        [InlineData(@"[abc", RegexOptions.None, RegexParseError.UnterminatedBracket, 4)]
        [InlineData(@"[d-c]", RegexOptions.None, RegexParseError.ReversedCharacterRange, 4)]
        [InlineData(@"[z-[:alnum:]]", RegexOptions.None, null)] // Nim: "Invalid set range. Start must be lesser than end"
        [InlineData(@"{10}", RegexOptions.None, RegexParseError.QuantifierAfterNothing, 1)]
        [InlineData(@"*abc", RegexOptions.None, RegexParseError.QuantifierAfterNothing, 1)]
        [InlineData(@"\12", RegexOptions.None, null)] // Nim: "Invalid octal literal. Expected 3 octal digits, but found 2"
        [InlineData(@"\12@", RegexOptions.None, null)] // Nim: "Invalid octal literal. Expected octal digit, but found @"
        [InlineData(@"\b?", RegexOptions.None, null)]
        [InlineData(@"\b*", RegexOptions.None, null)]
        [InlineData(@"\b+", RegexOptions.None, null)]
        [InlineData(@"\p{11", RegexOptions.None, RegexParseError.InvalidUnicodePropertyEscape, 5)]
        [InlineData(@"\p{11}", RegexOptions.None, RegexParseError.UnrecognizedUnicodeProperty, 6)]
        [InlineData(@"\p{Bb}", RegexOptions.None, RegexParseError.UnrecognizedUnicodeProperty, 6)]
        [InlineData(@"\p11", RegexOptions.None, RegexParseError.InvalidUnicodePropertyEscape, 2)]
        [InlineData(@"\pB", RegexOptions.None, RegexParseError.InvalidUnicodePropertyEscape, 2)]
        [InlineData(@"\u123", RegexOptions.None, RegexParseError.InsufficientOrInvalidHexDigits, 2)]
        [InlineData(@"\U123", RegexOptions.None, RegexParseError.UnrecognizedEscape, 2)]
        [InlineData(@"\U123@a", RegexOptions.None, RegexParseError.UnrecognizedEscape, 2)]
        [InlineData(@"\u123@abc", RegexOptions.None, RegexParseError.InsufficientOrInvalidHexDigits, 6)]
        [InlineData(@"\UFFFFFFFF", RegexOptions.None, RegexParseError.UnrecognizedEscape, 2)]
        [InlineData(@"\x{00000000A}", RegexOptions.None, RegexParseError.InsufficientOrInvalidHexDigits, 3)]
        [InlineData(@"\x{2f894", RegexOptions.None, RegexParseError.InsufficientOrInvalidHexDigits, 3)]
        [InlineData(@"\x{61@}", RegexOptions.None, RegexParseError.InsufficientOrInvalidHexDigits, 3)]
        [InlineData(@"\x{7fffffff}", RegexOptions.None, RegexParseError.InsufficientOrInvalidHexDigits, 3)] // Nim: not an error (supports Unicode beyond basic multilingual plane)
        [InlineData(@"\x{FFFFFFFF}", RegexOptions.None, RegexParseError.InsufficientOrInvalidHexDigits, 3)]
        [InlineData(@"+", RegexOptions.None, RegexParseError.QuantifierAfterNothing, 1)]
        [InlineData(@"+abc", RegexOptions.None, RegexParseError.QuantifierAfterNothing, 1)]
        [InlineData(@"a???", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 4)]
        [InlineData(@"a??*", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 4)]
        [InlineData(@"a??+", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 4)]
        [InlineData(@"a?*", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 3)]
        [InlineData(@"a?+", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 3)]
        [InlineData(@"a(?P<>abc)", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 4)]
        [InlineData(@"a(?P<asd)", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 4)]
        [InlineData(@"a{,}", RegexOptions.None, null)] // Nim error
        [InlineData(@"a{,1}", RegexOptions.None, null)] // Nim error
        [InlineData(@"a{0,101}", RegexOptions.None, null)] // Nim error: "Invalid repetition range. Expected 100 repetitions or less, but found: 101"
        [InlineData(@"a{0,a}", RegexOptions.None, null)] // Nim error
        [InlineData(@"a{0,bad}", RegexOptions.None, null)] // Nim error: "Invalid repetition range. Range can only contain digits"
        [InlineData(@"a{1,,,2}", RegexOptions.None, null)] // Nim error
        [InlineData(@"a{1,,}", RegexOptions.None, null)] // Nim error
        [InlineData(@"a{1,,2}", RegexOptions.None, null)] // Nim error
        [InlineData(@"a{1,", RegexOptions.None, null)] // Nim error
        [InlineData(@"a{1,}??", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 7)]
        [InlineData(@"a{1,}?", RegexOptions.None, null)]
        [InlineData(@"a{1,}*", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 6)]
        [InlineData(@"a{1,}+", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 6)]
        [InlineData(@"a{1,101}", RegexOptions.None, null)]
        [InlineData(@"a{1,x}", RegexOptions.None, null)] // Nim error
        [InlineData(@"a{1", RegexOptions.None, null)] // Nim error
        [InlineData(@"a{1}??", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 6)]
        [InlineData(@"a{1}?", RegexOptions.None, null)]
        [InlineData(@"a{1}*", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 5)]
        [InlineData(@"a{1}+", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 5)]
        [InlineData(@"a{1111111111}", RegexOptions.None, null)] // Nim error: "Invalid repetition range. Max value is 32767."
        [InlineData(@"a{1x}", RegexOptions.None, null)] // Nim error
        [InlineData(@"a*??", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 4)]
        [InlineData(@"a*{,}", RegexOptions.None, null)] // Nim error
        [InlineData(@"a*{0}", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 3)]
        [InlineData(@"a*{1}", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 3)]
        [InlineData(@"a**", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 3)]
        [InlineData(@"a*****", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 3)]
        [InlineData(@"a*+", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 3)]
        [InlineData(@"a+??", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 4)]
        [InlineData(@"a+*", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 3)]
        [InlineData(@"a++", RegexOptions.None, RegexParseError.NestedQuantifiersNotParenthesized, 3)]
        [InlineData(@"a|?", RegexOptions.None, RegexParseError.QuantifierAfterNothing, 3)]
        [InlineData(@"a|?b", RegexOptions.None, RegexParseError.QuantifierAfterNothing, 3)]
        [InlineData(@"a|*", RegexOptions.None, RegexParseError.QuantifierAfterNothing, 3)]
        [InlineData(@"a|*b", RegexOptions.None, RegexParseError.QuantifierAfterNothing, 3)]
        [InlineData(@"a|+", RegexOptions.None, RegexParseError.QuantifierAfterNothing, 3)]
        [InlineData(@"a|+b", RegexOptions.None, RegexParseError.QuantifierAfterNothing, 3)]
        [InlineData(@"aaa(?Pabc", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 6)]
        [InlineData(@"abc(?P<abc", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 6)]
        [InlineData(@"abc(?Pabc)", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 6)]
        [InlineData(@"abc(?q)", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 6)]
        [InlineData(@"abc[]", RegexOptions.None, RegexParseError.UnterminatedBracket, 5)]
        [InlineData(@"abc\A{10}", RegexOptions.None, null)] // Nim error:  "Invalid repetition range, either char, shorthand (i.e: \\w), group, or set expected before repetition range"
        [InlineData(@"\uD87E\uDC94(?Pabc", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 15)]
        [InlineData(@"\uD87E\uDC94aaa(?Pabc", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 18)]
        [InlineData(@"\uD87E\uDC94\uD87E\uDC94\uD87E\uDC94(?Pabc", RegexOptions.None, RegexParseError.InvalidGroupingConstruct, 39)]
        // End of Nim parser tests ==============

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void Parse_Netcoreapp(string pattern, RegexOptions options, RegexParseError? error, int offset = -1)
        {
            Parse(pattern, options, error, offset);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        public void RegexParseException_Serializes()
        {
#pragma warning disable RE0001 // Regex issue: Not enough )'s
            ArgumentException e = Assert.ThrowsAny<ArgumentException>(() => new Regex("(abc|def"));
#pragma warning restore RE0001 // Regex issue: Not enough )'s

            var bf = new BinaryFormatter();
            var s = new MemoryStream();
            bf.Serialize(s, e);
            s.Position = 0;

            object deserialized = bf.Deserialize(s);
            Assert.IsType<ArgumentException>(deserialized);
            ArgumentException e2 = (ArgumentException)deserialized;
            Assert.Equal(e.Message, e2.Message);
        }

        /// <summary>
        /// Checks if action throws either a RegexParseException or an ArgumentException depending on the
        /// environment and the supplied error.
        /// </summary>
        /// <param name="error">The expected parse error</param>
        /// <param name="action">The action to invoke.</param>
        static partial void Throws(string pattern, RegexOptions options, RegexParseError error, int offset, Action action)
        {
            try
            {
                action();
            }
            catch (RegexParseException e)
            {
                RegexParseError regexParseError = e.Error;

                // Success if provided error matches and offset is correct.
                if (error == regexParseError)
                {
                    Assert.Equal(offset, e.Offset);
                    LogActual(pattern, options, regexParseError, e.Offset);
                    return;
                }

                LogActual(pattern, options, regexParseError, e.Offset);
                throw new XunitException($"Expected RegexParseException with error {error} offset {offset} -> Actual error: {regexParseError} offset {e.Offset})");
            }
            catch (Exception e)
            {
                throw new XunitException($"Expected RegexParseException for pattern '{pattern}' -> Actual: ({e})");
            }

            LogActual(pattern, options, RegexParseError.Unknown, -1);
            throw new XunitException($"Expected RegexParseException with error: ({error}) -> Actual: No exception thrown");
        }

        /// <summary>
        /// Checks that action succeeds or throws either a RegexParseException or an ArgumentException depending on the
        // environment and the action.
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        static partial void MayThrow(Action action)
        {
            if (Record.Exception(action) is Exception e && e is not RegexParseException)
            {
                throw new XunitException($"Expected RegexParseException or no exception -> Actual: ({e})");
            }
        }
    }
}
