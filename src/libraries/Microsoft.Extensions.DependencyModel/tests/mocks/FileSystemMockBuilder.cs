// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    class FileSystemMockBuilder
    {
        private Dictionary<string, string> _files = new Dictionary<string, string>();

        internal static IFileSystem Empty { get; } = Create().Build();

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

        internal IFileSystem Build()
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

            public Stream OpenRead(string path)
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(ReadAllText(path)));
            }

            public Stream OpenFile(
                string path,
                FileMode fileMode,
                FileAccess fileAccess,
                FileShare fileShare,
                int bufferSize,
                FileOptions fileOptions)
            {
                throw new NotImplementedException();
            }

            public void CreateEmptyFile(string path)
            {
                _files.Add(path, string.Empty);
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
