// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    // Contains helper methods that are shared by all symbolic link test classes.
    public abstract partial class BaseSymbolicLinks : FileSystemTest
    {
        public BaseSymbolicLinks()
        {
            Assert.True(MountHelper.CanCreateSymbolicLinks);
        }

        protected DirectoryInfo CreateDirectoryContainingSelfReferencingSymbolicLink()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetRandomDirPath());
            string pathToLink = Path.Join(testDirectory.FullName, GetRandomDirName() + ".link");
            Assert.True(MountHelper.CreateSymbolicLink(pathToLink, pathToLink, isDirectory: true)); // Create a symlink cycle
            return testDirectory;
        }

        protected DirectoryInfo CreateSelfReferencingSymbolicLink()
        {
            string path = GetRandomDirPath();
            return (DirectoryInfo)Directory.CreateSymbolicLink(path, path);
        }

        /// <summary>
        /// Changes the current working directory path to a new temporary directory.
        /// Important: Make sure to call this inside a remote executor to avoid changing the cwd for all tests in same process.
        /// </summary>
        /// <returns>The path of the new cwd.</returns>
        protected string ChangeCurrentDirectory()
        {
            string tempCwd = GetRandomDirPath();
            Directory.CreateDirectory(tempCwd);
            Directory.SetCurrentDirectory(tempCwd);
            return tempCwd;
        }

        public static IEnumerable<object[]> SymbolicLink_LinkTarget_PathToTarget_Data
        {
            get
            {
                foreach (string path in PathToTargetData.Union(PathToTargetUncData))
                {
                    yield return new object[] { path };
                }
            }
        }

        public static IEnumerable<object[]> SymbolicLink_ResolveLinkTarget_PathToTarget_Data
        {
            get
            {
                foreach (string path in PathToTargetData.Union(PathToTargetUncData))
                {
                    yield return new object[] { path, false };
                    yield return new object[] { path, true };
                }
            }
        }

        // Junctions doesn't support remote shares.
        public static IEnumerable<object[]> Junction_LinkTarget_PathToTarget_Data
        {
            get
            {
                foreach (string path in PathToTargetData)
                {
                    yield return new object[] { path };
                }
            }
        }

        public static IEnumerable<object[]> Junction_ResolveLinkTarget_PathToTarget_Data
        {
            get
            {
                foreach (string path in PathToTargetData)
                {
                    yield return new object[] { path, false };
                    yield return new object[] { path, true };
                }
            }
        }

        internal static IEnumerable<string> PathToTargetData
        {
            get
            {
                if (OperatingSystem.IsWindows())
                {
                    //Non-rooted relative
                    yield return "foo";
                    yield return @".\foo";
                    yield return @"..\foo";
                    // Rooted relative
                    yield return @"\foo";
                    // Rooted absolute
                    yield return Path.Combine(Path.GetTempPath(), "foo");
                    // Extended DOS
                    yield return Path.Combine(@"\\?\", Path.GetTempPath(), "foo");
                }
                else
                {
                    //Non-rooted relative
                    yield return "foo";
                    yield return "./foo";
                    yield return "../foo";
                    // Rooted relative
                    yield return "/foo";
                    // Rooted absolute
                    Path.Combine(Path.GetTempPath(), "foo");
                }
            }
        }

        internal static IEnumerable<string> PathToTargetUncData
        {
            get
            {
                if (OperatingSystem.IsWindows())
                {
                    // UNC/Remote Share
                    yield return @"\\LOCALHOST\share\path";
                }
            }
        }
    }
}
