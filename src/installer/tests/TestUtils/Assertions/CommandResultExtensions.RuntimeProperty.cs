// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static partial class CommandResultExtensions
    {
        public static AndConstraint<CommandResultAssertions> HaveProperty(this CommandResultAssertions assertion, string name, string value)
        {
            // Expected trace message from hostpolicy
            return assertion.HaveStdErrContaining($"Property {name} = {value}");
        }

        public static AndConstraint<CommandResultAssertions> NotHaveProperty(this CommandResultAssertions assertion, string name)
        {
            return assertion.NotHaveStdErrContaining($"Property {name} =");
        }
    }
}
