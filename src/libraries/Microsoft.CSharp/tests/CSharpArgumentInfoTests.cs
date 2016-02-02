// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class CSharpArgumentInfoTests
    {
        [Fact]
        public void Create_ResultNotNull()
        {
            CSharpArgumentInfo ai = CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, "argName");
            Assert.NotNull(ai);
        }
    }
}
