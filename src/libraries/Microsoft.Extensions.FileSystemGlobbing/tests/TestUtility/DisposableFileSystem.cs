// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

#nullable enable

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.TestUtility
{
    public class DisposableFileSystem : IDisposable
    {
        private readonly bool _useInMemory;
        private readonly List<string> _files = new List<string>();

        public DisposableFileSystem(bool useInMemory = false)
        {
            _useInMemory = useInMemory;

            RootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            if (!useInMemory)
            {
                DirectoryInfo = new DirectoryInfo(RootPath);
                DirectoryInfo.Create();
            }
        }

        public string RootPath { get; }

        public DirectoryInfo? DirectoryInfo { get; }

        public DisposableFileSystem CreateFolder(string path)
        {
            if (!_useInMemory)
            {
                Directory.CreateDirectory(Path.Combine(RootPath, path));
            }

            return this;
        }

        public DisposableFileSystem CreateFile(string path)
        {
            string fullPath = Path.Combine(RootPath, path);

            if (!_useInMemory)
            {
                File.WriteAllText(fullPath, "temp");
            }

            _files.Add(fullPath);

            return this;
        }

        public DisposableFileSystem CreateFiles(params string[] fileRelativePaths)
        {
            foreach (var path in fileRelativePaths)
            {
                var fullPath = Path.Combine(RootPath, path);

                if (!_useInMemory)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

                    File.WriteAllText(
                        fullPath,
                        string.Format("Automatically generated for testing on {0:yyyy}/{0:MM}/{0:dd} {0:hh}:{0:mm}:{0:ss}", DateTime.UtcNow));
                }

                _files.Add(fullPath);
            }

            return this;
        }

        public DirectoryInfoBase GetDirectoryInfoBase()
            => _useInMemory ? new InMemoryDirectoryInfo(RootPath, _files) : new DirectoryInfoWrapper(DirectoryInfo!);

        public void Dispose()
        {
            if (!_useInMemory)
            {
                try
                {
                    Directory.Delete(RootPath, true);
                }
                catch
                {
                    // Don't throw if this fails.
                }
            }
        }
    }
}
