// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class MarshalByRefObjectTests
    {
        [Fact]
        public static void CanConstructDerivedObject()
        {
            new DerivedMarshalByRefObject();
        }

        private sealed class DerivedMarshalByRefObject : MarshalByRefObject { }
    }
}
