// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class StringUtilsTests
    {
        [Fact]
        public void IsEqualAscii()
        {
            for (char a = '\0'; a <= 0x7f; a++)
            {
                for (char b = '\0'; b <= 0x7f; b++)
                {
                    if (a == b)
                    {
                        Assert.True(StringUtils.IsEqualAscii(a, b, StringUtils.IgnoreCaseMask(true)));
                        Assert.True(StringUtils.IsEqualAscii(a, b, StringUtils.IgnoreCaseMask(false)));
                    }
                    else if (char.ToLower(a) == char.ToLower(b))
                    {
                        Assert.True(StringUtils.IsEqualAscii(a, b, StringUtils.IgnoreCaseMask(true)));
                        Assert.False(StringUtils.IsEqualAscii(a, b, StringUtils.IgnoreCaseMask(false)));
                    }
                    else
                    {
                        Assert.False(StringUtils.IsEqualAscii(a, b, StringUtils.IgnoreCaseMask(true)));
                        Assert.False(StringUtils.IsEqualAscii(a, b, StringUtils.IgnoreCaseMask(false)));
                    }
                }
            }
        }
    }
}
