// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Reflection.Tests
{
    public class LocationTests
    {
        // This test applies on all platforms including .NET Native. Location must at least be non-null (it can be empty).
        // System.Reflection.CoreCLR.Tests adds tests that expect more than that.
        [Fact]
        public void CurrentAssemblyHasNonNullLocation()
        {
            Assert.NotNull(typeof(LocationTests).GetTypeInfo().Assembly.Location);
        }
    }
}
