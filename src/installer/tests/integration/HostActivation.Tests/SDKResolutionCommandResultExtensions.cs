// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    internal static class SDKResolutionCommandResultExtensions
    {
        public static AndConstraint<CommandResultAssertions> FindAnySdk(this CommandResultAssertions assertion, bool shouldFindAnySdk)
        {
            string noSdkMessage = "No .NET SDKs were found";
            return shouldFindAnySdk
                ? assertion.NotHaveStdErrContaining(noSdkMessage)
                : assertion.HaveStdErrContaining(noSdkMessage)
                    .And.HaveStdErrContaining("Download a .NET SDK:");
        }

        public static AndConstraint<CommandResultAssertions> NotFindCompatibleSdk(this CommandResultAssertions assertion, string globalJsonPath = null, string requestedVersion = null)
        {
            var constraint = assertion.HaveStdErrContaining("compatible .NET SDK was not found");

            if (globalJsonPath is not null)
            {
                constraint = constraint.And.HaveStdErrContaining($"global.json file: {globalJsonPath}");
            }

            if (requestedVersion is not null)
            {
                constraint = constraint.And.HaveStdErrContaining($"Requested SDK version: {requestedVersion}");
            }

            if (globalJsonPath is not null && requestedVersion is not null)
            {
                constraint = constraint.And.HaveStdErrContaining($"Install the [{requestedVersion}] .NET SDK or update [{globalJsonPath}] to match an installed SDK.");
            }

            return constraint;
        }
    }
}
