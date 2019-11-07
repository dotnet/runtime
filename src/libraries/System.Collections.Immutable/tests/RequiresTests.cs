// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Immutable.Tests
{
    public class RequiresTests : ImmutablesTestBase
    {
        [Fact]
        public void Argument()
        {
            Requires.Argument(true);
            Requires.Argument(true, "parameterName", "message");
            AssertExtensions.Throws<ArgumentException>(null, () => Requires.Argument(false));
            AssertExtensions.Throws<ArgumentException>("parameterName", () => Requires.Argument(false, "parameterName", "message"));
        }

        [Fact]
        public void FailRange()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("parameterName", () => Requires.FailRange("parameterName"));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("parameterName", () => Requires.FailRange("parameterName", "message"));
        }

        [Fact]
        public void Range()
        {
            Requires.Range(true, "parameterName");
            Requires.Range(true, "parameterName", "message");
            AssertExtensions.Throws<ArgumentOutOfRangeException>("parameterName", () => Requires.Range(false, "parameterName"));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("parameterName", () => Requires.Range(false, "parameterName", "message"));
        }

        [Fact]
        public void NotNull()
        {
            Requires.NotNull(new object(), "parameterName");
            AssertExtensions.Throws<ArgumentNullException>("parameterName", () => Requires.NotNull((object)null, "parameterName"));
        }

        [Fact]
        public void NotNullAllowStructs()
        {
            Requires.NotNullAllowStructs(0, "parameterName");
            Requires.NotNullAllowStructs(new object(), "parameterName");
            AssertExtensions.Throws<ArgumentNullException>("parameterName", () => Requires.NotNullAllowStructs((object)null, "parameterName"));
        }
    }
}
