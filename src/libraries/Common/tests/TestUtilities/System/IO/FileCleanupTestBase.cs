// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Microsoft.Win32.SafeHandles;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Text;

namespace System.IO
{
    /// <summary>Base class for test classes the use temporary files that need to be cleaned up.</summary>
    public abstract partial class FileCleanupTestBase : IDisposable
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
                // Prefix with "#" to help spot leaked files
                TestDirectory = Path.Combine(tempDirectory, "#" + GetType().Name + "_" + Path.GetRandomFileName());
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
            try
            {
                try
                {
                    Directory.Delete(TestDirectory, recursive: true);
                }
                catch (UnauthorizedAccessException)
                {
                    DirectoryInfo di = new DirectoryInfo(TestDirectory);
                    foreach (FileSystemInfo fsi in di.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
                    {
                        fsi.Attributes = FileAttributes.Normal;
                    }

                    Directory.Delete(TestDirectory, recursive: true);
                }
            }
            catch {  } // avoid exceptions escaping Dispose
        }

        /// <summary>
        /// Gets the test directory into which all files and directories created by tests should be stored.
        /// This directory is isolated per test class.
        /// </summary>
        protected string TestDirectory { get; }

        protected string GetRandomFileName([CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0) => GetTestFileName(index: null, memberName, lineNumber) + ".txt";
        protected string GetRandomLinkName([CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0) => GetTestFileName(index: null, memberName, lineNumber) + ".link";
        protected string GetRandomDirName([CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0) => GetTestFileName(index: null, memberName, lineNumber) + "_dir";

        protected string GetRandomFilePath([CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0) => Path.Combine(TestDirectoryActualCasing, GetRandomFileName(memberName, lineNumber));
        protected string GetRandomLinkPath([CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0) => Path.Combine(TestDirectoryActualCasing, GetRandomLinkName(memberName, lineNumber));
        protected string GetRandomDirPath([CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)  => Path.Combine(TestDirectoryActualCasing, GetRandomDirName(memberName, lineNumber));

        private string _testDirectoryActualCasing;
        private string TestDirectoryActualCasing => _testDirectoryActualCasing ??= GetTestDirectoryActualCasing();

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
            string testFileName = PathGenerator.GenerateTestFileName(index, memberName, lineNumber);
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

                    testFileName = PathGenerator.GenerateTestFileName(index, memberName, lineNumber);
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

        protected static string GetNamedPipeServerStreamName()
        {
            if (PlatformDetection.IsInAppContainer)
            {
                return @"LOCAL\" + Guid.NewGuid().ToString("N");
            }

            if (PlatformDetection.IsWindows)
            {
                return Guid.NewGuid().ToString("N");
            }

            if (!PlatformDetection.IsCaseSensitiveOS)
            {
                return $"/tmp/{Guid.NewGuid().ToString("N")}";
            }

            const int MinUdsPathLength = 104; // required min is 92, but every platform we currently target is at least 104
            const int MinAvailableForSufficientRandomness = 5; // we want enough randomness in the name to avoid conflicts between concurrent tests
            string prefix = Path.Combine(Path.GetTempPath(), "CoreFxPipe_");
            int availableLength = MinUdsPathLength - prefix.Length - 1; // 1 - for possible null terminator
            Assert.True(availableLength >= MinAvailableForSufficientRandomness, $"UDS prefix {prefix} length {prefix.Length} is too long");

            StringBuilder sb = new(availableLength);
            Random random = new Random();
            for (int i = 0; i < availableLength; i++)
            {
                sb.Append((char)('a' + random.Next(0, 26)));
            }
            return sb.ToString();
        }

        // Some Windows versions like Windows Nano Server have the %TEMP% environment variable set to "C:\TEMP" but the
        // actual folder name is "C:\Temp", which prevents asserting path values using Assert.Equal due to case sensitiveness.
        // So instead of using TestDirectory directly, we retrieve the real path with proper casing of the initial folder path.
        private unsafe string GetTestDirectoryActualCasing()
        {
            if (!PlatformDetection.IsWindows)
                return TestDirectory;

            try
            {
                using SafeFileHandle handle = Interop.Kernel32.CreateFile(
                            TestDirectory,
                            dwDesiredAccess: 0,
                            dwShareMode: FileShare.ReadWrite | FileShare.Delete,
                            dwCreationDisposition: FileMode.Open,
                            dwFlagsAndAttributes:
                                Interop.Kernel32.FileOperations.OPEN_EXISTING |
                                Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS // Necessary to obtain a handle to a directory
                            );

                if (!handle.IsInvalid)
                {
                    const int InitialBufferSize = 4096;
                    char[]? buffer = ArrayPool<char>.Shared.Rent(InitialBufferSize);
                    uint result = GetFinalPathNameByHandle(handle, buffer);

                    // Remove extended prefix
                    int skip = PathInternal.IsExtended(buffer) ? 4 : 0;

                    return new string(
                        buffer,
                        skip,
                        (int)result - skip);
                }
            }
            catch { }

            return TestDirectory;
        }

        private unsafe uint GetFinalPathNameByHandle(SafeFileHandle handle, char[] buffer)
        {
            fixed (char* bufPtr = buffer)
            {
                return Interop.Kernel32.GetFinalPathNameByHandle(handle, bufPtr, (uint)buffer.Length, Interop.Kernel32.FILE_NAME_NORMALIZED);
            }
        }

        protected string CreateTestDirectory(params string[] paths)
        {
            string dir = Path.Combine(paths);
            Assert.True(Path.IsPathRooted(dir));
            Directory.CreateDirectory(dir);
            return dir;
        }

        protected string CreateTestDirectory() => CreateTestDirectory(GetTestFilePath());

        protected string CreateTestFile(params string[] paths)
        {
            string file = Path.Combine(paths);
            Assert.True(Path.IsPathRooted(file));
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.Create(file).Dispose();
            return file;
        }

        protected string CreateTestFile() => CreateTestFile(GetTestFilePath());

    }
}
