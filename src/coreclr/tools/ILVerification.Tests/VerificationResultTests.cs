// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILVerify;
using Xunit;

namespace ILVerification.Tests
{
    public class VerificationResultTests
    {
        [Fact]
        public static void ArgumentLookupOnResultWithoutArguments()
        {
            var result = new VerificationResult();

            Assert.False(result.TryGetArgumentValue("Offset", out int offset));
            Assert.Equal(0, offset);
            Assert.Equal(0, result.GetArgumentValue<int>("Offset"));
        }
    }
}
