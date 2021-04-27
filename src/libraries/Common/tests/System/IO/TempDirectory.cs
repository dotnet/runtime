// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.IO
{
    /// <summary>
    /// Represents a temporary directory.  Creating an instance creates a directory at the specified path,
    /// and disposing the instance deletes the directory.
    /// </summary>
    public class TempDirectory : IDisposable
    {
        public const int MaxNameLength = 255;

        /// <summary>Gets the created directory's path.</summary>
        public string Path { get; private set; }

        /// <summary>
        /// Construct a random temp directory in the temp folder.
        /// </summary>
        public TempDirectory()
            : this (IO.Path.Combine(IO.Path.GetTempPath(), IO.Path.GetRandomFileName()))
        {
        }

        public TempDirectory(string path)
        {
            Path = path;
            Directory.CreateDirectory(path);
        }

        ~TempDirectory() { DeleteDirectory(); }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            DeleteDirectory();
        }

        public string GenerateRandomFilePath() => IO.Path.Combine(Path, IO.Path.GetRandomFileName());

        protected virtual void DeleteDirectory()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch { /* Ignore exceptions on disposal paths */ }
        }

        /// <summary>
        /// Generates a string with 255 random valid filename characters.
        /// 255 is the max file/folder name length in NTFS and FAT32:
        // https://docs.microsoft.com/en-us/windows/win32/fileio/filesystem-functionality-comparison?redirectedfrom=MSDN#limits
        /// </summary>
        /// <returns>A 255 length string with random valid filename characters.</returns>
        public static string GetMaxLengthRandomName()
        {
            string guid = Guid.NewGuid().ToString("N");
            return guid + new string('x', 255 - guid.Length);
        }
    }
}
