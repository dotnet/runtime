// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.CoreSetup.Test.HostActivation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class TestArtifact : IDisposable
    {
        private static readonly Lazy<RepoDirectoriesProvider> _repoDirectoriesProvider =
            new Lazy<RepoDirectoriesProvider>(() => new RepoDirectoriesProvider());

        private static readonly Lazy<bool> _preserveTestRuns = new Lazy<bool>(() =>
            _repoDirectoriesProvider.Value.GetTestContextVariableOrNull("PRESERVE_TEST_RUNS") == "1");

        private static readonly string TestArtifactDirectoryEnvironmentVariable = "TEST_ARTIFACTS";
        private static readonly Lazy<string> _testArtifactsPath = new Lazy<string>(() =>
        {
            return _repoDirectoriesProvider.Value.GetTestContextVariable(TestArtifactDirectoryEnvironmentVariable)
                   ?? Path.Combine(AppContext.BaseDirectory, TestArtifactDirectoryEnvironmentVariable);
        }, isThreadSafe: true);

        public static bool PreserveTestRuns() => _preserveTestRuns.Value;
        public static string TestArtifactsPath => _testArtifactsPath.Value;

        public string Location { get; }
        public string Name { get; }

        private readonly List<TestArtifact> _copies = new List<TestArtifact>();

        public TestArtifact(string location, string name = null)
        {
            Location = location;
            Name = name ?? Path.GetFileName(Location);
        }

        protected TestArtifact(TestArtifact source)
        {
            Name = source.Name;
            Location = GetNewTestArtifactPath(Name);

            CopyRecursive(source.Location, Location, overwrite: true);

            source._copies.Add(this);
        }

        protected void RegisterCopy(TestArtifact artifact)
        {
            _copies.Add(artifact);
        }

        public virtual void Dispose()
        {
            if (!PreserveTestRuns() && Directory.Exists(Location))
            {
                Directory.Delete(Location, true);
            }

            foreach (TestArtifact copy in _copies)
            {
                copy.Dispose();
            }

            _copies.Clear();
        }

        protected static string GetNewTestArtifactPath(string artifactName)
        {
            var rand = new Random();

            string path;
            while (true)
            {
                path = Path.Combine(TestArtifactsPath, Path.GetRandomFileName(), artifactName);
                Directory.CreateDirectory(path);
                var lockPath = Path.Combine(path, ".lock");
                try
                {
                    File.Open(lockPath, FileMode.CreateNew, FileAccess.Write).Dispose();
                    return path;
                } catch
                {
                    // Lock file cannot be created, potential collision
                }
            }
        }

        protected static void CopyRecursive(string sourceDirectory, string destinationDirectory, bool overwrite = false)
        {
            FileUtils.EnsureDirectoryExists(destinationDirectory);

            foreach (var dir in Directory.EnumerateDirectories(sourceDirectory))
            {
                CopyRecursive(dir, Path.Combine(destinationDirectory, Path.GetFileName(dir)), overwrite);
            }

            foreach (var file in Directory.EnumerateFiles(sourceDirectory))
            {
                var dest = Path.Combine(destinationDirectory, Path.GetFileName(file));
                if (!File.Exists(dest) || overwrite)
                {
                    // We say overwrite true, because we only get here if the file didn't exist (thus it doesn't matter) or we
                    // wanted to overwrite :)
                    File.Copy(file, dest, overwrite: true);
                }
            }
        }
    }
}
