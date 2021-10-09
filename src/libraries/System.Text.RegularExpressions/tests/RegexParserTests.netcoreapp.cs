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
        [InlineData(@"[a-\b]", RegexOptions.None, RegexParseError.ReversedCharacterRange, 5)]
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
        static partial void Throws(RegexParseError error, int offset, Action action)
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
                    return;
                }

                throw new XunitException($"Expected RegexParseException with error {error} offset {offset} -> Actual error: {regexParseError} offset {e.Offset})");
            }
            catch (Exception e)
            {
                throw new XunitException($"Expected RegexParseException -> Actual: ({e})");
            }

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
