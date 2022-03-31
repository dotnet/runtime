// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions.Execution;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class AssertionScopeExtensions
    {
        public static Continuation FailWithPreformatted(this AssertionScope assertionScope, string message)
        {
            if (!assertionScope.Succeeded)
            {
                assertionScope.AddFailure(message);
            }

            return new Continuation(assertionScope, assertionScope.Succeeded);
        }
    }
}
