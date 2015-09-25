// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
