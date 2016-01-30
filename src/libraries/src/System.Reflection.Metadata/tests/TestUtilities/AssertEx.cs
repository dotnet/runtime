// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Internal.Tests
{
    /// <summary>
    /// Assert style type to deal with the lack of features in xUnit's Assert type
    /// </summary>
    public static class AssertEx
    {
        public static unsafe void Equal(byte* expected, byte* actual)
        {
            Assert.Equal((IntPtr)expected, (IntPtr)actual);
        }
    }
}
