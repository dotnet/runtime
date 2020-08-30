// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.TestUtility
{
    internal class FileSystemGlobbingTestContext
    {
        private readonly string _basePath;
        private readonly FileSystemOperationRecorder _recorder;
        private readonly Matcher _patternMatching;

        private MockDirectoryInfo _directoryInfo;

        public PatternMatchingResult Result { get; private set; }

        public FileSystemGlobbingTestContext(string basePath, Matcher matcher)
        {
            _basePath = basePath;
            _recorder = new FileSystemOperationRecorder();
            _patternMatching = matcher;

            _directoryInfo = new MockDirectoryInfo(
                recorder: _recorder,
                parentDirectory: null,
                fullName: _basePath,
                name: ".",
                paths: new string[0]);
        }

        public FileSystemGlobbingTestContext Include(params string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                _patternMatching.AddInclude(pattern);
            }

            return this;
        }

        public FileSystemGlobbingTestContext Exclude(params string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                _patternMatching.AddExclude(pattern);
            }

            return this;
        }

        public FileSystemGlobbingTestContext Files(params string[] files)
        {
            _directoryInfo = new MockDirectoryInfo(
                _directoryInfo.Recorder,
                _directoryInfo.ParentDirectory,
                _directoryInfo.FullName,
                _directoryInfo.Name,
                _directoryInfo.Paths.Concat(files.Select(file => _basePath + file)).ToArray());

            return this;
        }

        public FileSystemGlobbingTestContext Execute()
        {
            Result = _patternMatching.Execute(_directoryInfo);

            return this;
        }

        public FileSystemGlobbingTestContext AssertExact(params string[] files)
        {
            Assert.Equal(files.OrderBy(file => file), Result.Files.OrderBy(file => file.Path).Select(file => file.Path));

            return this;
        }

        public FileSystemGlobbingTestContext SubDirectory(string name)
        {
            _directoryInfo = (MockDirectoryInfo)_directoryInfo.GetDirectory(name);
            return this;
        }
    }
}
