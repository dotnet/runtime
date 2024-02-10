// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class TestArtifact : IDisposable
    {
        private static readonly Lazy<bool> _preserveTestRuns = new Lazy<bool>(() =>
            TestContext.GetTestContextVariableOrNull("PRESERVE_TEST_RUNS") == "1");

        public static bool PreserveTestRuns() => _preserveTestRuns.Value;
        public static string TestArtifactsPath => TestContext.TestArtifactsPath;

        public string Location { get; }
        public string Name { get; }

        protected string DirectoryToDelete { get; init; }

        private readonly List<TestArtifact> _copies = new List<TestArtifact>();
        private readonly Mutex _subdirMutex = new Mutex();

        public TestArtifact(string location)
        {
            Location = location;
            Name = Path.GetFileName(Location);
            DirectoryToDelete = Location;
        }

        protected TestArtifact(TestArtifact source)
        {
            Name = source.Name;
            (Location, DirectoryToDelete) = GetNewTestArtifactPath(source.Name);

            CopyRecursive(source.Location, Location, overwrite: true);

            source._copies.Add(this);
        }

        /// <summary>
        /// Create a new test artifact.
        /// </summary>
        /// <param name="name">Name of the test artifact</param>
        /// <returns>Test artifact containing no files</returns>
        public static TestArtifact Create(string name)
        {
            var (location, parentPath) = GetNewTestArtifactPath(name);
            return new TestArtifact(location)
            {
                DirectoryToDelete = parentPath
            };
        }

        /// <summary>
        /// Create a new test artifact populated with a copy of <paramref name="sourceDirectory"/>.
        /// </summary>
        /// <param name="name">Name of the test artifact</param>
        /// <param name="sourceDirectory">Source directory to copy</param>
        /// <returns>Test artifact containing a copy of <paramref name="sourceDirectory"/></returns>
        public static TestArtifact CreateFromCopy(string name, string sourceDirectory)
        {
            var artifact = Create(name);
            CopyRecursive(sourceDirectory, artifact.Location, overwrite: true);
            return artifact;
        }

        /// <summary>
        /// Locate the first non-existent subdirectory of the form <name>-<count>
        /// </summary>
        /// <param name="name">Name of the directory</param>
        /// <returns>Path to the created directory</returns>
        public string GetUniqueSubdirectory(string name)
        {
            _subdirMutex.WaitOne();
            int count = 0;
            string dir;
            do
            {
                dir = Path.Combine(Location, $"{name}-{count}");
                count++;
            } while (Directory.Exists(dir));

            _subdirMutex.ReleaseMutex();
            return dir;
        }

        public virtual void Dispose()
        {
            if (!PreserveTestRuns() && Directory.Exists(DirectoryToDelete))
            {
                try
                {
                    Directory.Delete(DirectoryToDelete, true);
                    Debug.Assert(!Directory.Exists(DirectoryToDelete));

                    // Delete lock file last
                    File.Delete($"{DirectoryToDelete}.lock");
                } catch (Exception e)
                {
                    Console.WriteLine("delete failed" + e);
                }
            }

            foreach (TestArtifact copy in _copies)
            {
                copy.Dispose();
            }

            _copies.Clear();
        }

        protected static (string, string) GetNewTestArtifactPath(string artifactName)
        {
            Exception? lastException = null;
            for (int i = 0; i < 10; i++)
            {
                var parentPath = Path.Combine(TestArtifactsPath, Path.GetRandomFileName());
                // Create a lock file next to the target folder
                var lockPath = parentPath + ".lock";
                var artifactPath = Path.Combine(parentPath, artifactName);
                try
                {
                    File.Open(lockPath, FileMode.CreateNew, FileAccess.Write).Dispose();
                }
                catch (Exception e)
                {
                    // Lock file cannot be created, potential collision
                    lastException = e;
                    continue;
                }
                Directory.CreateDirectory(artifactPath);
                return (artifactPath, parentPath);
            }
            Debug.Assert(lastException != null);
            throw lastException;
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
