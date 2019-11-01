// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Xunit;

namespace System.IO
{
    /// <summary>
    /// Represents a temporary file. Creating an instance creates a file at the specified path,
    /// and disposing the instance deletes the file.
    /// </summary>
    public sealed class TempFile : IDisposable
    {
        /// <summary>Gets the created file's path.</summary>
        public string Path { get; }

        public TempFile(string path, long length = 0) : this(path, length > -1 ? new byte[length] : null)
        {
        }

        public TempFile(string path, byte[] data)
        {
            Path = path;

            if (data != null)
            {
                File.WriteAllBytes(path, data);
            }
        }

        ~TempFile() => DeleteFile();

        public static TempFile Create(byte[] bytes, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            return new TempFile(GetFilePath(memberName, lineNumber), bytes);
        }

        public static TempFile Create(long length = -1, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            return new TempFile(GetFilePath(memberName, lineNumber), length);
        }

        public void AssertExists() => Assert.True(File.Exists(Path));

        public string ReadAllText() => File.ReadAllText(Path);

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

        private static string GetFilePath(string memberName, int lineNumber)
        {
            string file = $"{IO.Path.GetRandomFileName()}_{memberName}_{lineNumber}";
            return IO.Path.Combine(IO.Path.GetTempPath(), file);
        }
    }
}
