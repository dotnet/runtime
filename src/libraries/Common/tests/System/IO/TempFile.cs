// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.IO
{
    /// <summary>
    /// Represents a temporary file.  Creating an instance creates a file at the specified path,
    /// and disposing the instance deletes the file.
    /// </summary>
    public sealed class TempFile : IDisposable
    {
        /// <summary>Gets the created file's path.</summary>
        public string Path { get; }

        public TempFile(string path, long length = 0)
        {
            Path = path;

            if (length > -1)
                File.WriteAllBytes(path, new byte[length]);
        }

        ~TempFile() { DeleteFile(); }

        public static TempFile Create(long length = -1, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            string file = string.Format("{0}_{1}_{2}", System.IO.Path.GetRandomFileName(), memberName, lineNumber);
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), file);

            return new TempFile(path, length);
        }

        public void AssertExists()
        {
            Assert.True(File.Exists(Path));
        }

        public string ReadAllText()
        {
            return File.ReadAllText(Path);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            DeleteFile();
        }

        private void DeleteFile()
        {
            try { File.Delete(Path); }
            catch { /* Ignore exceptions on disposal paths */ }
        }
    }
}
