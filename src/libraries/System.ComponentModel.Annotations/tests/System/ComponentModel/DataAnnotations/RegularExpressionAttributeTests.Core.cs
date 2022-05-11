// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Xunit;

namespace System.ComponentModel.Annotations.Tests.System.ComponentModel.DataAnnotations
{
    public sealed partial class RegularExpressionAttributeTests
    {
        [Theory]
        [InlineData(12345)]
        [InlineData(-1)]
        public static void MatchTimeout_Get_ReturnsExpected(int newValue)
        {
            var attribute = new RegularExpressionAttribute("SomePattern") { MatchTimeoutInMilliseconds = newValue };
            Assert.Equal(TimeSpan.FromMilliseconds(newValue), attribute.MatchTimeout);
        }
    }
}
