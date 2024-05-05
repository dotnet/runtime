// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET
using System.Linq;
#endif
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

internal static class Utils
{
    public static void VerifyValidateOptionsResult(ValidateOptionsResult vr, int expectedErrorCount, params string[] expectedErrorSubstrings)
    {
        Assert.NotNull(vr);

#if NET
        var failures = vr.Failures!.ToArray();
#else
        var failures = vr.FailureMessage!.Split(';');
#endif

        Assert.Equal(expectedErrorCount, failures.Length);

        for (int i = 0; i < expectedErrorSubstrings.Length; i++)
        {
            Assert.Contains(expectedErrorSubstrings[i], failures[i]);
        }
    }
}
