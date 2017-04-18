// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Xunit;

namespace System.Runtime.ExceptionServices.Tests
{
    public partial class ExceptionDispatchInfoTests
    {
        [Fact]
        public static void StaticThrow_NullArgument_ThrowArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ExceptionDispatchInfo.Throw(null));
        }

        [Fact]
        public static void StaticThrow_UpdatesStackTraceAppropriately()
        {
            const string RethrowMessageSubstring = "End of stack trace";
            var e = new FormatException();
            for (int i = 0; i < 3; i++)
            {
                Assert.Same(e, Assert.Throws<FormatException>(() => ExceptionDispatchInfo.Throw(e)));
                Assert.Equal(i, Regex.Matches(e.StackTrace, RethrowMessageSubstring).Count);
            }
        }
    }
}
