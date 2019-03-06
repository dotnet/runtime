﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class TestArtifact : IDisposable
    {
        private static readonly string TestArtifactDirectoryEnvironmentVariable = "TEST_ARTIFACTS";
        private static Lazy<string> _testArtifactsPath = new Lazy<string>(() =>
        {
            return Environment.GetEnvironmentVariable(TestArtifactDirectoryEnvironmentVariable)
                   ?? Path.Combine(AppContext.BaseDirectory, TestArtifactDirectoryEnvironmentVariable);
        });

        public static string TestArtifactsPath
        {
            get
            {
                return _testArtifactsPath.Value;
            }
        }

        public string Location { get; }
        public string Name { get; }

        private List<TestArtifact> _copies = new List<TestArtifact>();

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

        public void Dispose()
        {
            if (!PreserveTestRuns())
            {
                Directory.Delete(Location, true);
            }

            foreach (TestArtifact copy in _copies)
            {
                copy.Dispose();
            }

            _copies.Clear();
        }

        public static bool PreserveTestRuns()
        {
            return Environment.GetEnvironmentVariable("PRESERVE_TEST_RUNS") == "1";
        }

        protected static string GetNewTestArtifactPath(string artifactName)
        {
            int projectCount = 0;
            string projectDirectory = Path.Combine(TestArtifactsPath, projectCount.ToString(), artifactName);

            while (Directory.Exists(projectDirectory))
            {
                projectDirectory = Path.Combine(TestArtifactsPath, (++projectCount).ToString(), artifactName);
            }

            return projectDirectory;
        }

        protected static void CopyRecursive(string sourceDirectory, string destinationDirectory, bool overwrite = false)
        {
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

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
