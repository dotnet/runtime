// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.IO
{
    /// <summary>Base class for test classes the use temporary files that need to be cleaned up.</summary>
    public abstract class FileCleanupTestBase : IDisposable
    {
        private static readonly Lazy<bool> s_isElevated = new Lazy<bool>(() => AdminHelpers.IsProcessElevated());

        private string fallbackGuid = Guid.NewGuid().ToString("N").Substring(0, 10);

        protected static bool IsProcessElevated => s_isElevated.Value;

        /// <summary>Initialize the test class base.  This creates the associated test directory.</summary>
        protected FileCleanupTestBase(string tempDirectory = null)
        {
            tempDirectory ??= Path.GetTempPath();

            // Use a unique test directory per test class.  The test directory lives in the user's temp directory,
            // and includes both the name of the test class and a random string.  The test class name is included
            // so that it can be easily correlated if necessary, and the random string to helps avoid conflicts if
            // the same test should be run concurrently with itself (e.g. if a [Fact] method lives on a base class)
            // or if some stray files were left over from a previous run.

            // Make 3 attempts since we have seen this on rare occasions fail with access denied, perhaps due to machine
            // configuration, and it doesn't make sense to fail arbitrary tests for this reason.
            string failure = string.Empty;
            for (int i = 0; i <= 2; i++)
            {
                TestDirectory = Path.Combine(tempDirectory, GetType().Name + "_" + Path.GetRandomFileName());
                try
                {
                    Directory.CreateDirectory(TestDirectory);
                    break;
                }
                catch (Exception ex)
                {
                    failure += ex.ToString() + Environment.NewLine;
                    Thread.Sleep(10); // Give a transient condition like antivirus/indexing a chance to go away
                }
            }

            Assert.True(Directory.Exists(TestDirectory), $"FileCleanupTestBase failed to create {TestDirectory}. {failure}");
        }

        /// <summary>Delete the associated test directory.</summary>
        ~FileCleanupTestBase()
        {
            Dispose(false);
        }

        /// <summary>Delete the associated test directory.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Delete the associated test directory.</summary>
        protected virtual void Dispose(bool disposing)
        {
            // No managed resources to clean up, so disposing is ignored.

            try { Directory.Delete(TestDirectory, recursive: true); }
            catch { } // avoid exceptions escaping Dispose
        }

        /// <summary>
        /// Gets the test directory into which all files and directories created by tests should be stored.
        /// This directory is isolated per test class.
        /// </summary>
        protected string TestDirectory { get; }

        /// <summary>Gets a test file full path that is associated with the call site.</summary>
        /// <param name="index">An optional index value to use as a suffix on the file name.  Typically a loop index.</param>
        /// <param name="memberName">The member name of the function calling this method.</param>
        /// <param name="lineNumber">The line number of the function calling this method.</param>
        protected virtual string GetTestFilePath(int? index = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0) =>
            Path.Combine(TestDirectory, GetTestFileName(index, memberName, lineNumber));

        /// <summary>Gets a test file name that is associated with the call site.</summary>
        /// <param name="index">An optional index value to use as a suffix on the file name.  Typically a loop index.</param>
        /// <param name="memberName">The member name of the function calling this method.</param>
        /// <param name="lineNumber">The line number of the function calling this method.</param>
        protected string GetTestFileName(int? index = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string testFileName = GenerateTestFileName(index, memberName, lineNumber);
            string testFilePath = Path.Combine(TestDirectory, testFileName);

            const int maxLength = 260 - 5; // Windows MAX_PATH minus a bit

            int excessLength = testFilePath.Length - maxLength;

            if (excessLength > 0)
            {
                // The path will be too long for Windows -- can we
                // trim memberName to fix it?
                if (excessLength < memberName.Length + "...".Length)
                {
                    // Take a chunk out of the middle as perhaps it's the least interesting part of the name
                    int halfMemberNameLength = (int)Math.Floor((double)memberName.Length / 2);
                    int halfExcessLength = (int)Math.Ceiling((double)excessLength / 2);
                    memberName = memberName.Substring(0, halfMemberNameLength - halfExcessLength) + "..." + memberName.Substring(halfMemberNameLength + halfExcessLength);

                    testFileName = GenerateTestFileName(index, memberName, lineNumber);
                    testFilePath = Path.Combine(TestDirectory, testFileName);
                }
                else
                {
                    return fallbackGuid;
                }
            }

            Debug.Assert(testFilePath.Length <= maxLength + "...".Length);

            return testFileName;
        }

        private string GenerateTestFileName(int? index, string memberName, int lineNumber) =>
            string.Format(
                index.HasValue ? "{0}_{1}_{2}_{3}" : "{0}_{1}_{3}",
                memberName ?? "TestBase",
                lineNumber,
                index.GetValueOrDefault(),
                Guid.NewGuid().ToString("N").Substring(0, 8)); // randomness to avoid collisions between derived test classes using same base method concurrently

        /// <summary>
        /// In some cases (such as when running without elevated privileges),
        /// the symbolic link may fail to create. Only run this test if it creates
        /// links successfully.
        /// </summary>
        protected static bool CanCreateSymbolicLinks => s_canCreateSymbolicLinks.Value;

        private static readonly Lazy<bool> s_canCreateSymbolicLinks = new Lazy<bool>(() =>
        {
            bool success = true;

            // Verify file symlink creation
            string path = Path.GetTempFileName();
            string linkPath = path + ".link";
            success = CreateSymLink(path, linkPath, isDirectory: false);
            try { File.Delete(path); } catch { }
            try { File.Delete(linkPath); } catch { }

            // Verify directory symlink creation
            path = Path.GetTempFileName();
            linkPath = path + ".link";
            success = success && CreateSymLink(path, linkPath, isDirectory: true);
            try { Directory.Delete(path); } catch { }
            try { Directory.Delete(linkPath); } catch { }

            return success;
        });

        protected static bool CreateSymLink(string targetPath, string linkPath, bool isDirectory)
        {
#if NETFRAMEWORK
            bool isWindows = true;
#else
            if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsBrowser()) // OSes that don't support Process.Start()
            {
                return false;
            }
            bool isWindows = OperatingSystem.IsWindows();
#endif
            Process symLinkProcess = new Process();
            if (isWindows)
            {
                symLinkProcess.StartInfo.FileName = "cmd";
                symLinkProcess.StartInfo.Arguments = string.Format("/c mklink{0} \"{1}\" \"{2}\"", isDirectory ? " /D" : "", Path.GetFullPath(linkPath), Path.GetFullPath(targetPath));
            }
            else
            {
                symLinkProcess.StartInfo.FileName = "/bin/ln";
                symLinkProcess.StartInfo.Arguments = string.Format("-s \"{0}\" \"{1}\"", Path.GetFullPath(targetPath), Path.GetFullPath(linkPath));
            }
            symLinkProcess.StartInfo.RedirectStandardOutput = true;
            symLinkProcess.Start();

            symLinkProcess.WaitForExit();
            return (0 == symLinkProcess.ExitCode);
        }

    }
}
