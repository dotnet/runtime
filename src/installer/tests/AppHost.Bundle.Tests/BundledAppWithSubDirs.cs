// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.AppHost;
using Microsoft.NET.HostModel.Bundle;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class BundledAppWithSubDirs : IClassFixture<BundledAppWithSubDirs.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundledAppWithSubDirs(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        private FluentAssertions.AndConstraint<CommandResultAssertions> RunTheApp(string path, bool selfContained, bool deleteExtracted = true)
        {
            CommandResult result = Command.Create(path)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(selfContained ? null : TestContext.BuiltDotNet.BinPath)
                .MultilevelLookup(false)
                .Execute();
            if (deleteExtracted)
            {
                DeleteExtractionDirectory(result);
            }

            return result.Should().Pass()
                .And.HaveStdOutContaining("Wow! We now say hello to the big world and you.");
        }

        private static void DeleteExtractionDirectory(CommandResult result)
        {
            Assert.False(string.IsNullOrEmpty(result.StdErr), "Attempted to get extraction directory from empty stderr. Ensure tracing was enabled and stderr captured.");

            string pattern = @"Files embedded within the bundle will be extracted to \[(.*?)\]";
            var match = System.Text.RegularExpressions.Regex.Match(result.StdErr, pattern);
            if (!match.Success)
                return;

            string extractionDir = match.Groups[1].Value;
            try
            {
                Directory.Delete(extractionDir, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete extraction directory '{extractionDir}': {ex}");
            }
        }

        [Theory]
        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleAllContent)]
        public void FrameworkDependent(BundleOptions options)
        {
            var singleFile = sharedTestState.FrameworkDependentApp.Bundle(options);

            // Run the bundled app
            bool shouldExtract = options.HasFlag(BundleOptions.BundleAllContent);
            RunTheApp(singleFile, selfContained: false, deleteExtracted: !shouldExtract)
                .And.CreateExtraction(shouldExtract);

            if (shouldExtract)
            {
                // Run the bundled app again (reuse extracted files)
                RunTheApp(singleFile, selfContained: false)
                    .And.ReuseExtraction();
            }
        }

        [Theory]
        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleAllContent)]
        [InlineData(BundleOptions.EnableCompression)]
        [InlineData(BundleOptions.BundleAllContent | BundleOptions.EnableCompression)]
        public void SelfContained(BundleOptions options)
        {
            var singleFile = sharedTestState.SelfContainedApp.Bundle(options);

            // Run the bundled app
            bool shouldExtract = options.HasFlag(BundleOptions.BundleAllContent);
            RunTheApp(singleFile, selfContained: true, deleteExtracted: !shouldExtract)
                .And.CreateExtraction(shouldExtract);

            if (shouldExtract)
            {
                // Run the bundled app again (reuse extracted files)
                RunTheApp(singleFile, selfContained: true)
                    .And.ReuseExtraction();
            }
        }

        [Theory]
        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleAllContent)]
        public void SelfContained_Targeting50(BundleOptions options)
        {
            var singleFile = sharedTestState.SelfContainedApp.Bundle(options, new Version(5, 0));

            // Run the bundled app
            bool shouldExtract = options.HasFlag(BundleOptions.BundleAllContent);
            RunTheApp(singleFile, selfContained: true, deleteExtracted: !shouldExtract)
                .And.CreateExtraction(shouldExtract);

            if (shouldExtract)
            {
                // Run the bundled app again (reuse extracted files)
                RunTheApp(singleFile, selfContained: true)
                    .And.ReuseExtraction();
            }
        }

        [Theory]
        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleAllContent)]
        public void FrameworkDependent_Targeting50(BundleOptions options)
        {
            var singleFile = sharedTestState.FrameworkDependentApp.Bundle(options, new Version(5, 0));

            // Run the bundled app
            bool shouldExtract = options.HasFlag(BundleOptions.BundleAllContent);
            RunTheApp(singleFile, selfContained: false, deleteExtracted: !shouldExtract)
                .And.CreateExtraction(shouldExtract);

            if (shouldExtract)
            {
                // Run the bundled app again (reuse extracted files)
                RunTheApp(singleFile, selfContained: false)
                    .And.ReuseExtraction();
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/54234")]
        // NOTE: when enabling this test take a look at commented code marked by "ACTIVE ISSUE:" in SharedTestState
        public void SelfContained_R2R_Composite()
        {
            var singleFile = sharedTestState.SelfContainedCompositeApp.Bundle(BundleOptions.None);

            // Run the app
            RunTheApp(singleFile, selfContained: true);
        }

        public class SharedTestState : IDisposable
        {
            public SingleFileTestApp FrameworkDependentApp { get; }
            public SingleFileTestApp SelfContainedApp { get; }
            public SingleFileTestApp SelfContainedCompositeApp { get; }

            public SharedTestState()
            {
                FrameworkDependentApp = SingleFileTestApp.CreateFrameworkDependent("AppWithSubDirs");
                AddLongNameContent(FrameworkDependentApp.NonBundledLocation);

                SelfContainedApp = SingleFileTestApp.CreateSelfContained("AppWithSubDirs");
                AddLongNameContent(SelfContainedApp.NonBundledLocation);

                // ACTIVE ISSUE: https://github.com/dotnet/runtime/issues/54234
                //               This should be an app built with the equivalent of PublishReadyToRun=true and PublishReadyToRunComposite=true
                SelfContainedCompositeApp = SingleFileTestApp.CreateSelfContained("AppWithSubDirs");
                AddLongNameContent(SelfContainedCompositeApp.NonBundledLocation);
            }

            public void Dispose()
            {
                FrameworkDependentApp.Dispose();
                SelfContainedApp.Dispose();
                SelfContainedCompositeApp.Dispose();
            }

            public static void AddLongNameContent(string directory)
            {
                // For tests using the AppWithSubDirs, One of the sub-directories with a really long name
                // is generated during test-runs rather than being checked in as a test asset.
                // This prevents git-clone of the repo from failing if long-file-name support is not enabled on windows.
                var longDirName = "This is a really, really, really, really, really, really, really, really, really, really, really, really, really, really long file name for punctuation";
                var longDirPath = Path.Combine(directory, "Sentence", longDirName);
                Directory.CreateDirectory(longDirPath);
                using (var writer = File.CreateText(Path.Combine(longDirPath, "word")))
                {
                    writer.Write(".");
                }
            }
        }
    }

    public static class BundledAppResultExtensions
    {
        public static FluentAssertions.AndConstraint<CommandResultAssertions> CreateExtraction(this CommandResultAssertions assertion, bool shouldExtract)
        {
            string message = "Starting new extraction of application bundle";
            return shouldExtract
                ? assertion.HaveStdErrContaining(message)
                : assertion.NotHaveStdErrContaining(message);
        }

        public static FluentAssertions.AndConstraint<CommandResultAssertions> ReuseExtraction(this CommandResultAssertions assertion)
        {
            return assertion.HaveStdErrContaining("Reusing existing extraction of application bundle");
        }
    }
}
