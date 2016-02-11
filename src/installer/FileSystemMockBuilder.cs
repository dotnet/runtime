// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.EnvironmentAbstractions;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    class FileSystemMockBuilder
    {
        private Dictionary<string, string> _files = new Dictionary<string, string>();

        public static IFileSystem Empty { get; } = Create().Build();

        public static FileSystemMockBuilder Create()
        {
            return new FileSystemMockBuilder();
        }

        public FileSystemMockBuilder AddFile(string name, string content = "")
        {
            _files.Add(name, content);
            return this;
        }

        public FileSystemMockBuilder AddFiles(string basePath, params string[] files)
        {
            foreach (var file in files)
            {
                AddFile(Path.Combine(basePath, file));
            }
            return this;
        }

        public IFileSystem Build()
        {
            return new FileSystemMock(_files);
        }

        private class FileSystemMock : IFileSystem
        {
            public FileSystemMock(Dictionary<string, string> files)
            {
                File = new FileMock(files);
                Directory = new DirectoryMock(files);
            }

            public IFile File { get; }

            public IDirectory Directory { get; }
        }

        private class FileMock : IFile
        {
            private Dictionary<string, string> _files;
            public FileMock(Dictionary<string, string> files)
            {
                _files = files;
            }

            public bool Exists(string path)
            {
                return _files.ContainsKey(path);
            }

            public string ReadAllText(string path)
            {
                string text;
                if (!_files.TryGetValue(path, out text))
                {
                    throw new FileNotFoundException(path);
                }
                return text;
            }
        }

        private class DirectoryMock : IDirectory
        {
            private Dictionary<string, string> _files;
            public DirectoryMock(Dictionary<string, string> files)
            {
                _files = files;
            }

            public bool Exists(string path)
            {
                return _files.Keys.Any(k => k.StartsWith(path));
            }
        }
    }

}
