// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using System.Resources;
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

        [Fact]
        public static void EveryErrorHasMessage()
        {
            // Verifier looks up the message of an error by its name, and falls back to the name.
            var strings = new ResourceManager("ILVerification.Strings", typeof(Verifier).Assembly);
            VerifierError[] missing = Enum.GetValues<VerifierError>()
                .Where(e => e != VerifierError.None && string.IsNullOrEmpty(strings.GetString(e.ToString(), CultureInfo.InvariantCulture)))
                .ToArray();
            Assert.Empty(missing);
        }
    }
}
