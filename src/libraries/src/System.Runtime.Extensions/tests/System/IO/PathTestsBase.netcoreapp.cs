// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Tests
{
    public partial class PathTestsBase
    {
        protected static class PathAssert
        {
            public static void Equal(ReadOnlySpan<char> expected, ReadOnlySpan<char> actual)
            {
                if (!actual.SequenceEqual(expected))
                    throw new Xunit.Sdk.EqualException(new string(expected), new string(actual));
            }

            public static void Empty(ReadOnlySpan<char> actual)
            {
                if (actual.Length > 0)
                    throw new Xunit.Sdk.NotEmptyException();
            }
        }
    }
}
