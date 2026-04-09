// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    internal static class FrameworkResolutionCommandResultExtensions
    {
        public static AndConstraint<CommandResultAssertions> HaveResolvedFramework(this CommandResultAssertions assertion, string name, string version, string resolvedFrameworkBasePath = null)
        {
            string expectedOutput = $"mock frameworks: {name} {version}";
            if (resolvedFrameworkBasePath is not null)
                expectedOutput += $" [path: {Path.Combine(resolvedFrameworkBasePath, "shared", name, version)}]";

            return assertion.HaveStdOutContaining(expectedOutput);
        }

        public static AndConstraint<CommandResultAssertions> ShouldHaveResolvedFramework(this CommandResult result, string resolvedFrameworkName, string resolvedFrameworkVersion, string resolvedFrameworkBasePath = null)
        {
            return result.Should().Pass()
                .And.HaveResolvedFramework(resolvedFrameworkName, resolvedFrameworkVersion, resolvedFrameworkBasePath);
        }

        /// <summary>
        /// Verifies that the command result either passes with a resolved framework or fails with inability to find compatible framework version.
        /// </summary>
        /// <param name="result">The result to verify.</param>
        /// <param name="resolvedFrameworkName">The name of the framework to verify.</param>
        /// <param name="resolvedFrameworkVersion">
        ///     Either null in which case the command result is expected to fail and not find compatible framework version,
        ///     or the framework versions in which case the command result is expected to succeed and resolve the specified framework version.</param>
        /// <returns>Constraint</returns>
        public static AndConstraint<CommandResultAssertions> ShouldHaveResolvedFrameworkOrFailToFind(this CommandResult result, string resolvedFrameworkName, string resolvedFrameworkVersion, string resolvedFrameworkBasePath = null)
        {
            if (resolvedFrameworkName == null || resolvedFrameworkVersion == null ||
                resolvedFrameworkVersion == FrameworkResolutionBase.ResolvedFramework.NotFound)
            {
                return result.ShouldFailToFindCompatibleFrameworkVersion(resolvedFrameworkName);
            }
            else
            {
                return result.ShouldHaveResolvedFramework(resolvedFrameworkName, resolvedFrameworkVersion, resolvedFrameworkBasePath);
            }
        }

        public static AndConstraint<CommandResultAssertions> DidNotFindCompatibleFrameworkVersion(this CommandResultAssertions assertion, string frameworkName, string requestedVersion)
        {
            var constraint = assertion.HaveStdErrContaining("You must install or update .NET to run this application.");
            if (frameworkName is not null)
            {
                constraint = constraint.And.HaveStdErrContaining($"Framework: '{frameworkName}', {(requestedVersion is null ? "" : $"version '{requestedVersion}'")}");
            }

            return constraint;
        }

        public static AndConstraint<CommandResultAssertions> ShouldFailToFindCompatibleFrameworkVersion(this CommandResult result, string frameworkName, string requestedVersion = null)
        {
            return result.Should().Fail()
                .And.DidNotFindCompatibleFrameworkVersion(frameworkName, requestedVersion);
        }

        public static AndConstraint<CommandResultAssertions> FailedToReconcileFrameworkReference(
            this CommandResultAssertions assertion,
            string frameworkName,
            string newVersion,
            string previousVersion)
        {
            return assertion.HaveStdErrMatching($".*The specified framework '{frameworkName}', version '{newVersion}', apply_patches=[0-1], version_compatibility_range=[^ ]* cannot roll-forward to the previously referenced version '{previousVersion}'.*");
        }

        public static AndConstraint<CommandResultAssertions> ShouldHaveResolvedFrameworkOrFailedToReconcileFrameworkReference(
            this CommandResult result,
            string frameworkName,
            string resolvedVersion,
            string lowerVersion,
            string higherVersion)
        {
            if (resolvedVersion == null || resolvedVersion == FrameworkResolutionBase.ResolvedFramework.FailedToReconcile)
            {
                return result.Should().Fail().And.FailedToReconcileFrameworkReference(frameworkName, lowerVersion, higherVersion);
            }
            else
            {
                return result.ShouldHaveResolvedFramework(frameworkName, resolvedVersion);
            }
        }

        public static AndConstraint<CommandResultAssertions> ShouldHaveResolvedFrameworkOrFail(
            this CommandResult result,
            string frameworkName,
            string resolvedVersion,
            string lowerVersion,
            string higherVersion)
        {
            if (resolvedVersion == FrameworkResolutionBase.ResolvedFramework.FailedToReconcile)
            {
                return result.Should().Fail().And.FailedToReconcileFrameworkReference(frameworkName, lowerVersion, higherVersion);
            }
            else if (resolvedVersion == FrameworkResolutionBase.ResolvedFramework.NotFound)
            {
                return result.ShouldFailToFindCompatibleFrameworkVersion(frameworkName, null);
            }
            else
            {
                return result.ShouldHaveResolvedFramework(frameworkName, resolvedVersion);
            }
        }

        public static AndConstraint<CommandResultAssertions> RestartedFrameworkResolution(this CommandResultAssertions assertion, string resolvedVersion, string newVersion)
        {
            return assertion.HaveStdErrContaining($"--- Restarting all framework resolution because the previously resolved framework 'Microsoft.NETCore.App', version '{resolvedVersion}' must be re-resolved with the new version '{newVersion}'");
        }

        public static AndConstraint<CommandResultAssertions> DidNotRecognizeRollForwardValue(this CommandResultAssertions assertion, string value)
        {
            return assertion.HaveStdErrContaining($"Unrecognized roll forward setting value '{value}'.");
        }
    }
}
