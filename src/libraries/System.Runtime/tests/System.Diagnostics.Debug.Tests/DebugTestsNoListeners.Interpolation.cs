// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#define DEBUG
using System.Text;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class DebugTestsNoListeners : DebugTests
    {
        [Fact]
        public void Asserts_Interpolation()
        {
            Debug.AssertInterpolatedStringHandler message;
            Debug.AssertInterpolatedStringHandler detailedMessage;
            bool shouldAppend;

            message = new Debug.AssertInterpolatedStringHandler(0, 0, true, out shouldAppend);
            VerifyLogged(() => Debug.Assert(true, ref message), "");

            message = new Debug.AssertInterpolatedStringHandler(0, 0, true, out shouldAppend);
            detailedMessage = new Debug.AssertInterpolatedStringHandler(0, 0, true, out shouldAppend);
            VerifyLogged(() => Debug.Assert(true, ref message, ref detailedMessage), "");

            message = new Debug.AssertInterpolatedStringHandler(0, 0, false, out shouldAppend);
            message.AppendLiteral("uh oh");
            VerifyAssert(() => Debug.Assert(false, ref message), "uh oh");

            message = new Debug.AssertInterpolatedStringHandler(0, 0, false, out shouldAppend);
            message.AppendLiteral("uh oh");
            detailedMessage = new Debug.AssertInterpolatedStringHandler(0, 0, false, out shouldAppend);
            detailedMessage.AppendLiteral("something went wrong");
            VerifyAssert(() => Debug.Assert(false, ref message, ref detailedMessage), "uh oh", "something went wrong");
        }

        [Fact]
        public void Asserts_Interpolation_Syntax()
        {
            VerifyLogged(() => Debug.Assert(true, $"you won't see this {EmptyToString.Instance}"), "");
            VerifyLogged(() => Debug.Assert(true, $"you won't see this {EmptyToString.Instance}", $"you won't see this {EmptyToString.Instance}"), "");
            VerifyAssert(() => Debug.Assert(false, $"uh oh{EmptyToString.Instance}"), "uh oh");
            VerifyAssert(() => Debug.Assert(false, $"uh oh{EmptyToString.Instance}", $"something went wrong{EmptyToString.Instance}"), "uh oh", "something went wrong");
        }

        [Fact]
        public void WriteIf_Interpolation()
        {
            Debug.WriteIfInterpolatedStringHandler handler;
            bool shouldAppend;

            handler = new Debug.WriteIfInterpolatedStringHandler(0, 0, true, out shouldAppend);
            handler.AppendLiteral("logged");
            VerifyLogged(() => Debug.WriteIf(true, ref handler), "logged");

            handler = new Debug.WriteIfInterpolatedStringHandler(0, 0, false, out shouldAppend);
            VerifyLogged(() => Debug.WriteIf(false, ref handler), "");

            handler = new Debug.WriteIfInterpolatedStringHandler(0, 0, true, out shouldAppend);
            handler.AppendLiteral("logged");
            VerifyLogged(() => Debug.WriteIf(true, ref handler, "category"), "category: logged");

            handler = new Debug.WriteIfInterpolatedStringHandler(0, 0, false, out shouldAppend);
            VerifyLogged(() => Debug.WriteIf(false, ref handler, "category"), "");

            GoToNextLine();
        }

        [Fact]
        public void WriteIf_Interpolation_Syntax()
        {
            VerifyLogged(() => Debug.WriteIf(true, $"{EmptyToString.Instance}logged"), "logged");
            VerifyLogged(() => Debug.WriteIf(false, $"{EmptyToString.Instance}logged"), "");
            VerifyLogged(() => Debug.WriteIf(true, $"{EmptyToString.Instance}logged", "category"), "category: logged");
            VerifyLogged(() => Debug.WriteIf(false, $"{EmptyToString.Instance}logged", "category"), "");
            GoToNextLine();
        }

        [Fact]
        public void WriteLineIf_Interpolation()
        {
            Debug.WriteIfInterpolatedStringHandler handler;
            bool shouldAppend;

            handler = new Debug.WriteIfInterpolatedStringHandler(0, 0, true, out shouldAppend);
            handler.AppendLiteral("logged");
            VerifyLogged(() => Debug.WriteLineIf(true, ref handler), "logged" + Environment.NewLine);

            handler = new Debug.WriteIfInterpolatedStringHandler(0, 0, false, out shouldAppend);
            VerifyLogged(() => Debug.WriteLineIf(false, ref handler), "");

            handler = new Debug.WriteIfInterpolatedStringHandler(0, 0, true, out shouldAppend);
            handler.AppendLiteral("logged");
            VerifyLogged(() => Debug.WriteLineIf(true, ref handler, "category"), "category: logged" + Environment.NewLine);

            handler = new Debug.WriteIfInterpolatedStringHandler(0, 0, false, out shouldAppend);
            VerifyLogged(() => Debug.WriteLineIf(false, ref handler, "category"), "");
        }

        [Fact]
        public void WriteLineIf_Interpolation_Syntax()
        {
            VerifyLogged(() => Debug.WriteLineIf(true, $"{EmptyToString.Instance}logged"), "logged" + Environment.NewLine);
            VerifyLogged(() => Debug.WriteLineIf(false, $"{EmptyToString.Instance}logged"), "");
            VerifyLogged(() => Debug.WriteLineIf(true, $"{EmptyToString.Instance}logged", "category"), "category: logged" + Environment.NewLine);
            VerifyLogged(() => Debug.WriteLineIf(false, $"{EmptyToString.Instance}logged", "category"), "");
            GoToNextLine();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Condition_ShouldAppend_Matches(bool condition)
        {
            bool shouldAppend;

            new Debug.AssertInterpolatedStringHandler(1, 2, condition, out shouldAppend);
            Assert.Equal(!condition, shouldAppend);

            new Debug.WriteIfInterpolatedStringHandler(1, 2, condition, out shouldAppend);
            Assert.Equal(condition, shouldAppend);
        }

        [Fact]
        public void DebugHandler_AppendOverloads_MatchStringBuilderHandler()
        {
            var actual = new Debug.AssertInterpolatedStringHandler(0, 0, condition: false, out bool shouldAppend);
            Assert.True(shouldAppend);

            var sb = new StringBuilder();
            var expected = new StringBuilder.AppendInterpolatedStringHandler(0, 0, sb);

            actual.AppendLiteral("abcd");
            expected.AppendLiteral("abcd");

            actual.AppendFormatted(123);
            expected.AppendFormatted(123);

            actual.AppendFormatted(45.6, 10);
            expected.AppendFormatted(45.6, 10);

            actual.AppendFormatted(default(Guid), "X");
            expected.AppendFormatted(default(Guid), "X");

            DateTime dt = DateTime.UtcNow;
            actual.AppendFormatted(dt, -100, "r");
            expected.AppendFormatted(dt, -100, "r");

            actual.AppendFormatted("hello");
            expected.AppendFormatted("hello");

            actual.AppendFormatted("world", -10, null);
            expected.AppendFormatted("world", -10, null);

            actual.AppendFormatted((ReadOnlySpan<char>)"nice to");
            expected.AppendFormatted((ReadOnlySpan<char>)"nice to");

            actual.AppendFormatted((ReadOnlySpan<char>)"nice to", 0, "anything");
            expected.AppendFormatted((ReadOnlySpan<char>)"nice to", 0, "anything");

            actual.AppendFormatted((object)DayOfWeek.Monday, 42, null);
            expected.AppendFormatted((object)DayOfWeek.Monday, 42, null);

            VerifyAssert(() => Debug.Assert(false, ref actual), sb.ToString());
        }

        [Fact]
        public void WriteIfHandler_AppendOverloads_MatchStringBuilderHandler()
        {
            var actual = new Debug.WriteIfInterpolatedStringHandler(0, 0, condition: true, out bool shouldAppend);
            Assert.True(shouldAppend);

            var sb = new StringBuilder();
            var expected = new StringBuilder.AppendInterpolatedStringHandler(0, 0, sb);

            actual.AppendLiteral("abcd");
            expected.AppendLiteral("abcd");

            actual.AppendFormatted(123);
            expected.AppendFormatted(123);

            actual.AppendFormatted(45.6, 10);
            expected.AppendFormatted(45.6, 10);

            actual.AppendFormatted(default(Guid), "X");
            expected.AppendFormatted(default(Guid), "X");

            DateTime dt = DateTime.UtcNow;
            actual.AppendFormatted(dt, -100, "r");
            expected.AppendFormatted(dt, -100, "r");

            actual.AppendFormatted("hello");
            expected.AppendFormatted("hello");

            actual.AppendFormatted("world", -10, null);
            expected.AppendFormatted("world", -10, null);

            actual.AppendFormatted((ReadOnlySpan<char>)"nice to");
            expected.AppendFormatted((ReadOnlySpan<char>)"nice to");

            actual.AppendFormatted((ReadOnlySpan<char>)"nice to", 0, "anything");
            expected.AppendFormatted((ReadOnlySpan<char>)"nice to", 0, "anything");

            actual.AppendFormatted((object)DayOfWeek.Monday, 42, null);
            expected.AppendFormatted((object)DayOfWeek.Monday, 42, null);

            VerifyLogged(() => Debug.WriteIf(true, ref actual), sb.ToString());
        }

        private sealed class EmptyToString
        {
            public static EmptyToString Instance { get; } = new EmptyToString();
            public override string ToString() => "";
        }
    }
}
