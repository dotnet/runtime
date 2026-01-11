// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.CoreSetup.Test;

namespace HostActivation.Tests
{
    internal static class SDKResolutionCommandResultExtensions
    {
        public static CommandResultAssertions FindAnySdk(this CommandResultAssertions assertion, bool shouldFindAnySdk)
        {
            string noSdkMessage = "No .NET SDKs were found";
            return shouldFindAnySdk
                ? assertion.NotHaveStdErrContaining(noSdkMessage)
                : assertion.HaveStdErrContaining(noSdkMessage)
                    .HaveStdErrContaining("Download a .NET SDK:");
        }

        public static CommandResultAssertions NotFindCompatibleSdk(this CommandResultAssertions assertion, string globalJsonPath = null, string requestedVersion = null)
        {
            var constraint = assertion.HaveStdErrContaining("compatible .NET SDK was not found");

            if (globalJsonPath is not null)
            {
                constraint = constraint.HaveStdErrContaining($"global.json file: {globalJsonPath}");
            }

            if (requestedVersion is not null)
            {
                constraint = constraint.HaveStdErrContaining($"Requested SDK version: {requestedVersion}");
            }

            if (globalJsonPath is not null && requestedVersion is not null)
            {
                constraint = constraint.HaveStdErrContaining($"Install the [{requestedVersion}] .NET SDK or update [{globalJsonPath}] to match an installed SDK.");
            }

            return constraint;
        }
    }
}
