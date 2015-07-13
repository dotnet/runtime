// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace System.IO
{
    /// <summary>Base class for test classes the use temporary files that need to be cleaned up.</summary>
    public abstract class TemporaryFilesCleanupTestBase : IDisposable
    {
        /// <summary>Initialize the test class base.</summary>
        protected TemporaryFilesCleanupTestBase()
        {
            // Use a unique test directory per test class
            TestDirectory = Path.Combine(Path.GetTempPath(), GetType().Name + "_" + Guid.NewGuid().ToString("N"));
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

        ~TemporaryFilesCleanupTestBase()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            // No managed resources to clean up.

            try { Directory.Delete(TestDirectory, recursive: true); }
            catch { } // avoid exceptions from Dispose
        }

        /// <summary>Gets the test directory into which all files and directories created by tests should be stored.</summary>
        protected string TestDirectory { get; private set; }

        /// <summary>Generates a test file full path that is unique to the call site.</summary>
        protected string GetTestFilePath([CallerMemberName]string fileName = null, [CallerLineNumber] int lineNumber = 0)
        {
            return Path.Combine(TestDirectory, GetTestFileName(fileName, lineNumber));
        }

        /// <summary>Generates a test file name that is unique to the call site.</summary>
        protected string GetTestFileName([CallerMemberName]string fileName = null, [CallerLineNumber] int lineNumber = 0)
        {
            return string.Format("{0}_{1}", fileName ?? "TestBase", lineNumber);
        }
    }
}
