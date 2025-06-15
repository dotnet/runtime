// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    public partial class MLDsaCngTests_AllPlatforms
    {
        [Fact]
        public void MLDsaCng_Ctor_ArgValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", static () => new MLDsaCng(null));
        }
    }
}
