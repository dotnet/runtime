// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.IO
{
    /// <summary>Base class for test classes the use temporary files that need to be cleaned up.</summary>
    public abstract class FileCleanupTestBase : IDisposable
    {
        /// <summary>Initialize the test class base.  This creates the associated test directory.</summary>
        protected FileCleanupTestBase()
        {
            // Use a unique test directory per test class.  The test directory lives in the user's temp directory,
            // and includes both the name of the test class and a random string.  The test class name is included 
            // so that it can be easily correlated if necessary, and the random string to helps avoid conflicts if
            // the same test should be run concurrently with itself (e.g. if a [Fact] method lives on a base class) 
            // or if some stray files were left over from a previous run.
            TestDirectory = Path.Combine(Path.GetTempPath(), GetType().Name + "_" + Path.GetRandomFileName());
            try
            {
                Directory.CreateDirectory(TestDirectory);
            }
            catch
            {
                // Don't throw exceptions during test class construction.  Attempts to use paths
                // under this directory will instead appropriately fail later.
            }
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

        /// <summary>Gets the test directory into which all files and directories created by tests should be stored.</summary>
        protected string TestDirectory { get; private set; }

        /// <summary>Gets a test file full path that is associated with the call site.</summary>
        /// <param name="index">An optional index value to use as a suffix on the file name.  Typically a loop index.</param>
        /// <param name="memberName">The member name of the function calling this method.</param>
        /// <param name="lineNumber">The line number of the function calling this method.</param>
        protected string GetTestFilePath(int? index = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            return Path.Combine(TestDirectory, GetTestFileName(index, memberName, lineNumber));
        }

        /// <summary>Gets a test file name that is associated with the call site.</summary>
        /// <param name="index">An optional index value to use as a suffix on the file name.  Typically a loop index.</param>
        /// <param name="memberName">The member name of the function calling this method.</param>
        /// <param name="lineNumber">The line number of the function calling this method.</param>
        protected string GetTestFileName(int? index = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            return string.Format(
                index.HasValue ? "{0}_{1}_{2}" : "{0}_{1}",
                memberName ?? "TestBase", lineNumber, index.GetValueOrDefault());
        }
    }
}
