// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public partial class HttpMethodTest
    {
        [Fact]
        public void Patch_VerifyValue_PropertyNameMatchesHttpMethodName()
        {
            Assert.Equal("PATCH", HttpMethod.Patch.Method);
        }

        static partial void AddStaticHttpMethods(List<object[]> staticHttpMethods)
        {
            staticHttpMethods.Add(new object[] { HttpMethod.Patch });
        }
    }
}
