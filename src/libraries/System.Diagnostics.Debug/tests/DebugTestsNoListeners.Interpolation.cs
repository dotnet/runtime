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
            VerifyLogged(() => Debug.Assert(true, message), "");

            message = new Debug.AssertInterpolatedStringHandler(0, 0, true, out shouldAppend);
            detailedMessage = new Debug.AssertInterpolatedStringHandler(0, 0, true, out shouldAppend);
            VerifyLogged(() => Debug.Assert(true, message, detailedMessage), "");

            message = new Debug.AssertInterpolatedStringHandler(0, 0, false, out shouldAppend);
            message.AppendLiteral("assert passed");
            VerifyAssert(() => Debug.Assert(false, message), "assert passed");

            message = new Debug.AssertInterpolatedStringHandler(0, 0, false, out shouldAppend);
            message.AppendLiteral("assert passed");
            detailedMessage = new Debug.AssertInterpolatedStringHandler(0, 0, false, out shouldAppend);
            detailedMessage.AppendLiteral("nothing is wrong");
            VerifyAssert(() => Debug.Assert(false, message, detailedMessage), "assert passed", "nothing is wrong");
        }

        [Fact]
        public void WriteIf_Interpolation()
        {
            Debug.WriteIfInterpolatedStringHandler handler;
            bool shouldAppend;

            handler = new Debug.WriteIfInterpolatedStringHandler(0, 0, true, out shouldAppend);
            handler.AppendLiteral("logged");
            VerifyLogged(() => Debug.WriteIf(true, handler), "logged");

            handler = new Debug.WriteIfInterpolatedStringHandler(0, 0, false, out shouldAppend);
            VerifyLogged(() => Debug.WriteIf(false, handler), "");

            handler = new Debug.WriteIfInterpolatedStringHandler(0, 0, true, out shouldAppend);
            handler.AppendLiteral("logged");
            VerifyLogged(() => Debug.WriteIf(true, handler, "category"), "category: logged");

            handler = new Debug.WriteIfInterpolatedStringHandler(0, 0, false, out shouldAppend);
            VerifyLogged(() => Debug.WriteIf(false, handler, "category"), "");

            GoToNextLine();
        }

        [Fact]
        public void WriteLineIf_Interpolation()
        {
            Debug.WriteIfInterpolatedStringHandler handler;
            bool shouldAppend;

            handler = new Debug.WriteIfInterpolatedStringHandler(0, 0, true, out shouldAppend);
            handler.AppendLiteral("logged");
            VerifyLogged(() => Debug.WriteLineIf(true, handler), "logged" + Environment.NewLine);

            handler = new Debug.WriteIfInterpolatedStringHandler(0, 0, false, out shouldAppend);
            VerifyLogged(() => Debug.WriteLineIf(false, handler), "");

            handler = new Debug.WriteIfInterpolatedStringHandler(0, 0, true, out shouldAppend);
            handler.AppendLiteral("logged");
            VerifyLogged(() => Debug.WriteLineIf(true, handler, "category"), "category: logged" + Environment.NewLine);

            handler = new Debug.WriteIfInterpolatedStringHandler(0, 0, false, out shouldAppend);
            VerifyLogged(() => Debug.WriteLineIf(false, handler, "category"), "");
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

            VerifyAssert(() => Debug.Assert(false, actual), sb.ToString());
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

            VerifyLogged(() => Debug.WriteIf(true, actual), sb.ToString());
        }
    }
}
