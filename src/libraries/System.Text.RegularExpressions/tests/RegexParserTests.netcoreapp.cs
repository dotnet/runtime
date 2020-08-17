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
        [InlineData(@"[a-\-]", RegexOptions.None, RegexParseError.ReversedCharacterRange)]
        [InlineData(@"[a-\-b]", RegexOptions.None, RegexParseError.ReversedCharacterRange)]
        [InlineData(@"[a-\-\-b]", RegexOptions.None, RegexParseError.ReversedCharacterRange)]
        [InlineData(@"[a-\-\D]", RegexOptions.None, RegexParseError.ReversedCharacterRange)]
        [InlineData(@"[a-\-\-\D]", RegexOptions.None, RegexParseError.ReversedCharacterRange)]
        [InlineData(@"[a -\-\b]", RegexOptions.None, null)]
        // OutOfMemoryException
        [InlineData("a{2147483647}", RegexOptions.None, null)]
        [InlineData("a{2147483647,}", RegexOptions.None, null)]
        [InlineData(@"(?(?N))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct)]
        [InlineData(@"(?(?i))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct)]
        [InlineData(@"(?(?I))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct)]
        [InlineData(@"(?(?M))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct)]
        [InlineData(@"(?(?s))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct)]
        [InlineData(@"(?(?S))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct)]
        [InlineData(@"(?(?x))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct)]
        [InlineData(@"(?(?X))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct)]
        [InlineData(@"(?(?n))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct)]
        [InlineData(" (?(?n))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct)]
        [InlineData(@"(?(?m))", RegexOptions.None, RegexParseError.InvalidGroupingConstruct)]
        // IndexOutOfRangeException
        [InlineData("(?<-", RegexOptions.None, RegexParseError.InvalidGroupingConstruct)]
        [InlineData("(?<-", RegexOptions.IgnorePatternWhitespace, RegexParseError.InvalidGroupingConstruct)]
        [InlineData(@"^[^<>]*(((?'Open'<)[^<>]*)+((?'Close-Open'>)[^<>]*)+)*(?(Open)(?!))$", RegexOptions.None, null)]
        [InlineData(@"((?'Close-Open'>)[^<>]*)+", RegexOptions.None, RegexParseError.UndefinedNamedReference)]
        [InlineData(@"(((?'Open'<)[^<>]*)+((?'Close-Open'>)[^<>]*)+)*", RegexOptions.None, null)]
        [InlineData(@"(?'Close-Open'>)", RegexOptions.None, RegexParseError.UndefinedNamedReference)]
        [InlineData("(?<a-00>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<a>)()(?<-0>)(?<-1>)(?<-2>)(?<-3>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference)]
        [InlineData("()(?<a>)(?<-0>)(?<-1>)(?<-2>)(?<-3>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference)]
        [InlineData("()()(?<-0>)(?<-1>)(?<-2>)(?<-3>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference)]
        [InlineData("(?<a>)(?<b>)(?<-1>)(?<-2>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<-4>)(?<4>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<4>)(?<-4>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<a>)(?<-a>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<-a>)(?<a>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<a-0>", RegexOptions.IgnorePatternWhitespace, RegexParseError.InsufficientClosingParentheses)]
        [InlineData("(?<a-0>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<a-0 >)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid)]
        [InlineData("(?<a- 0 >)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid)]
        [InlineData("(?<a- 0>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid)]
        [InlineData("(?<-1>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference)]
        [InlineData("()(?<-1>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<-1>)()", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<-00>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<a-", RegexOptions.IgnorePatternWhitespace, RegexParseError.InvalidGroupingConstruct)]
        [InlineData("(?<a-0", RegexOptions.IgnorePatternWhitespace, RegexParseError.InvalidGroupingConstruct)]
        [InlineData("(?<a-0)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid)]
        [InlineData("(?<a>)(?<b>)(?<-0>)(?<-1>)(?<-2>)(?<-3>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference)]
        [InlineData("(?<-0>)(?<-1>)(?<-2>)(?<-3>)()()", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference)]
        [InlineData("(?<-0>)(?<-1>)(?<-2>)(?<-3>)()(?", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference)]
        [InlineData("(?<-0>)(?<-1>)(?<-2>)(?<-3>)()(?<a>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference)]
        [InlineData("(?<-0>)(?<-1>)(?<-2>)(?<-3>)(?<a>)()", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference)]
        [InlineData("(?<-0>)(?<-1>)(?<-2>)(?<-3>)(?<a>)(?", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference)]
        [InlineData("(?<-0>)(?<-1>)(?<-2>)(?<-3>)(?<a>)(?<b>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.UndefinedNumberedReference)]
        [InlineData("(?<a-0>)(?<b-a>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<a-0>)(?<-a>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<a-a>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<-0>)", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<-0 >)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid)]
        [InlineData("(?<- 0 >)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid)]
        [InlineData("(?<- 0>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid)]
        [InlineData("(?<a-0')", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid)]
        [InlineData("(?'a-0>)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid)]
        [InlineData("(?'-0')", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?'a-0')", RegexOptions.IgnorePatternWhitespace, null)]
        [InlineData("(?<-0", RegexOptions.IgnorePatternWhitespace, RegexParseError.InvalidGroupingConstruct)]
        [InlineData("(?<-0)", RegexOptions.IgnorePatternWhitespace, RegexParseError.CaptureGroupNameInvalid)]
        [InlineData("(?<-0>", RegexOptions.IgnorePatternWhitespace, RegexParseError.InsufficientClosingParentheses)]
        [InlineData(@"(?<cat>cat)\w+(?<dog-()*!@>dog)", RegexOptions.None, RegexParseError.CaptureGroupNameInvalid)]
        [InlineData(@"(?<cat>cat)\w+(?<dog-catdog>dog)", RegexOptions.None, RegexParseError.UndefinedNamedReference)]
        [InlineData(@"(?<cat>cat)\w+(?<dog-1uosn>dog)", RegexOptions.None, RegexParseError.CaptureGroupNameInvalid)]
        [InlineData(@"(?<cat>cat)\w+(?<dog-16>dog)", RegexOptions.None, RegexParseError.UndefinedNumberedReference)]
        [InlineData(@"cat(?<->dog)", RegexOptions.None, RegexParseError.CaptureGroupNameInvalid)]
        [InlineData("a{2147483648}", RegexOptions.None, RegexParseError.QuantifierOrCaptureGroupOutOfRange)]
        [InlineData("a{2147483648,}", RegexOptions.None, RegexParseError.QuantifierOrCaptureGroupOutOfRange)]
        [InlineData("a{0,2147483647}", RegexOptions.None, null)]
        [InlineData("a{0,2147483648}", RegexOptions.None, RegexParseError.QuantifierOrCaptureGroupOutOfRange)]
        // Surrogate pair which is parsed as [char,char-char,char] as we operate on UTF-16 code units.
        [InlineData("[\uD82F\uDCA0-\uD82F\uDCA3]", RegexOptions.IgnoreCase, RegexParseError.ReversedCharacterRange)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void Parse_NotNetFramework(string pattern, RegexOptions options, object error)
        {
            Parse(pattern, options, error);
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

                // Success if provided error matches.
                if (error == regexParseError)
                {
                    // only test offset if provided and greater than zero
                    if (offset > 0)
                    {
                        Assert.Equal(offset, e.Offset);
                    }
                    return;
                }

                throw new XunitException($"Expected RegexParseException with error: ({error}) -> Actual error: {regexParseError})");
            }
            catch(Exception e)
            { 
                throw new XunitException($"Expected RegexParseException -> Actual: ({e.GetType()})");
            }

            throw new XunitException($"Expected RegexParseException with error: ({error}) -> Actual: No exception thrown");
        }
    }
}
