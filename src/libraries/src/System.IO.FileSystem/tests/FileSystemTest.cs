// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace System.IO.FileSystem.Tests
{
    public abstract class FileSystemTest : IDisposable
    {
        public string TestDirectory { get; private set; }

        public FileSystemTest()
        {
            // Use a unique test directory per test class
            TestDirectory = Path.Combine(Directory.GetCurrentDirectory(), GetType().Name);

            try
            {
                Directory.CreateDirectory(TestDirectory);
            }
            catch 
            {
                // Don't want this to crash the test, we'll fail appropriately in other test 
                // cases if Directory.Create is broken.
            }
        }

        // Generates a test file path to use that is unique name per test case / call
        public string GetTestFilePath([CallerMemberName]string fileName = null, [CallerLineNumber] int lineNumber = 0)
        {
            return Path.Combine(TestDirectory, String.Format("{0}_{1}", fileName ?? "testFile", lineNumber));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            // if (disposing)  no managed resources

            // clean up non-managed resources
            try
            {
                Directory.Delete(TestDirectory, true);
            }
            catch
            {
                // Don't throw during dispose
            }
        }
    }
}
