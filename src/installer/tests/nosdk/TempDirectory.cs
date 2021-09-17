// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copied from dotnet/roslyn

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed class TempDirectory : IDisposable
    {
        private readonly string _path;
        private readonly TempRoot _root;

        internal TempDirectory(TempRoot root)
            : this(CreateUniqueDirectory(root.Root), root)
        { }

        private TempDirectory(string path, TempRoot root)
        {
            _path = path;
            _root = root;
        }

        private static string CreateUniqueDirectory(string basePath)
        {
            while (true)
            {
                string dir = System.IO.Path.Combine(basePath, Guid.NewGuid().ToString());
                try
                {
                    Directory.CreateDirectory(dir);
                    return dir;
                }
                catch (IOException)
                {
                    // retry
                }
            }
        }

        public string Path
        {
            get { return _path; }
        }

        /// <summary>
        /// Creates a file in this directory.
        /// </summary>
        /// <param name="name">File name.</param>
        public TempFile CreateFile(string name)
        {
            string filePath = System.IO.Path.Combine(_path, name);
            TempRoot.CreateStream(filePath, FileMode.CreateNew);
            return _root.AddFile(new DisposableFile(filePath));
        }

        /// <summary>
        /// Creates a file or opens an existing file in this directory.
        /// </summary>
        public TempFile CreateOrOpenFile(string name)
        {
            string filePath = System.IO.Path.Combine(_path, name);
            TempRoot.CreateStream(filePath, FileMode.OpenOrCreate);
            return _root.AddFile(new DisposableFile(filePath));
        }

        /// <summary>
        /// Creates a file in this directory that is a copy of the specified file.
        /// </summary>
        public TempFile CopyFile(string originalPath, string? name = null)
        {
            string filePath = System.IO.Path.Combine(_path, name ?? System.IO.Path.GetFileName(originalPath));
            File.Copy(originalPath, filePath);
            return _root.AddFile(new DisposableFile(filePath));
        }

        /// <summary>
        /// Creates a subdirectory in this directory.
        /// </summary>
        /// <param name="name">Directory name or unrooted directory path.</param>
        public TempDirectory CreateDirectory(string name)
        {
            string dirPath = System.IO.Path.Combine(_path, name);
            Directory.CreateDirectory(dirPath);
            return new TempDirectory(dirPath, _root);
        }

        public override string ToString()
        {
            return _path;
        }

        public void Dispose()
        {
            if (Path != null && Directory.Exists(Path))
            {
                try
                {
                    Directory.Delete(Path, recursive: true);
                }
                catch
                {
                }
            }
        }
    }
}