// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    internal static class FrameworkResolutionCommandResultExtensions
    {
        public static AndConstraint<CommandResultAssertions> HaveResolvedFramework(this CommandResultAssertions assertion, string name, string version)
        {
            return assertion.HaveStdOutContaining($"mock frameworks: {name} {version}");
        }

        public static AndConstraint<CommandResultAssertions> DidNotFindCompatibleFrameworkVersion(this CommandResultAssertions assertion)
        {
            return assertion.HaveStdErrContaining("It was not possible to find any compatible framework version");
        }

        public static AndConstraint<CommandResultAssertions> FailedToSoftRollForward(this CommandResultAssertions assertion, string frameworkName, string newVersion, string previousVersion)
        {
            return assertion.HaveStdErrMatching($".*The specified framework '{frameworkName}', version '{newVersion}', patch_roll_fwd=[0-1], roll_fwd_on_no_candidate_fx=[0-2] cannot roll-forward to the previously referenced version '{previousVersion}'.*");
        }

        public static AndConstraint<CommandResultAssertions> RestartedFrameworkResolution(this CommandResultAssertions assertion, string resolvedVersion, string newVersion)
        {
            return assertion.HaveStdErrContaining($"--- Restarting all framework resolution because the previously resolved framework 'Microsoft.NETCore.App', version '{resolvedVersion}' must be re-resolved with the new version '{newVersion}'");
        }
    }
}
