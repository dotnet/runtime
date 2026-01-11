// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public class DirectoryInfoAssertions
    {
        private DirectoryInfo _dirInfo;

        public DirectoryInfoAssertions(DirectoryInfo dir)
        {
            _dirInfo = dir;
        }

        public DirectoryInfo DirectoryInfo => _dirInfo;

        public DirectoryInfoAssertions Exist()
        {
            Assert.True(_dirInfo.Exists, $"Expected directory '{_dirInfo.FullName}' does not exist.");
            return this;
        }

        public DirectoryInfoAssertions HaveFile(string expectedFile)
        {
            var file = _dirInfo.EnumerateFiles(expectedFile, SearchOption.TopDirectoryOnly).SingleOrDefault();
            Assert.True(file is not null, $"Expected File '{expectedFile}' cannot be found in directory '{_dirInfo.FullName}.");
            return this;
        }

        public DirectoryInfoAssertions NotHaveFile(string expectedFile)
        {
            var file = _dirInfo.EnumerateFiles(expectedFile, SearchOption.TopDirectoryOnly).SingleOrDefault();
            Assert.True(file is null, $"File '{expectedFile}' should not be found in directory '{_dirInfo.FullName}'.");
            return this;
        }

        public DirectoryInfoAssertions HaveFiles(IEnumerable<string> expectedFiles)
        {
            foreach (var expectedFile in expectedFiles)
            {
                HaveFile(expectedFile);
            }

            return this;
        }

        public DirectoryInfoAssertions NotHaveFiles(IEnumerable<string> expectedFiles)
        {
            foreach (var expectedFile in expectedFiles)
            {
                NotHaveFile(expectedFile);
            }

            return this;
        }

        public DirectoryInfoAssertions HaveDirectory(string expectedDir)
        {
            var dir = _dirInfo.EnumerateDirectories(expectedDir, SearchOption.TopDirectoryOnly).SingleOrDefault();
            Assert.True(dir is not null, $"Expected directory '{expectedDir}' cannot be found inside directory '{_dirInfo.FullName}'.");

            return new DirectoryInfoAssertions(dir);
        }

        public DirectoryInfoAssertions NotHaveDirectory(string expectedDir)
        {
            var dir = _dirInfo.EnumerateDirectories(expectedDir, SearchOption.TopDirectoryOnly).SingleOrDefault();
            Assert.True(dir is null, $"Directory '{expectedDir}' should not be found in found inside directory '{_dirInfo.FullName}'.");

            return new DirectoryInfoAssertions(dir);
        }

        public DirectoryInfoAssertions OnlyHaveFiles(IEnumerable<string> expectedFiles)
        {
            var actualFiles = _dirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Select(f => f.Name);
            var missingFiles = Enumerable.Except(expectedFiles, actualFiles);
            var extraFiles = Enumerable.Except(actualFiles, expectedFiles);
            var nl = Environment.NewLine;

            Assert.True(!missingFiles.Any(), $"Following files cannot be found inside directory {_dirInfo.FullName} {nl} {string.Join(nl, missingFiles)}");

            Assert.True(!extraFiles.Any(), $"Following extra files are found inside directory {_dirInfo.FullName} {nl} {string.Join(nl, extraFiles)}");

            return this;
        }

        public DirectoryInfoAssertions NotBeModifiedAfter(DateTime timeUtc)
        {
            _dirInfo.Refresh();
            DateTime writeTime = _dirInfo.LastWriteTimeUtc;

            Assert.True(writeTime <= timeUtc, $"Directory '{_dirInfo.FullName}' should not be modified after {timeUtc}, but is modified at {writeTime}.");

            return this;
        }

    }
}
