// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using FluentAssertions.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public AndConstraint<DirectoryInfoAssertions> Exist()
        {
            Execute.Assertion.ForCondition(_dirInfo.Exists)
                .FailWithPreformatted($"Expected directory '{_dirInfo.FullName}' does not exist.");
            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> HaveFile(string expectedFile)
        {
            var file = _dirInfo.EnumerateFiles(expectedFile, SearchOption.TopDirectoryOnly).SingleOrDefault();
            Execute.Assertion.ForCondition(file != null)
                .FailWithPreformatted($"Expected File '{expectedFile}' cannot be found in directory '{_dirInfo.FullName}.");
            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> NotHaveFile(string expectedFile)
        {
            var file = _dirInfo.EnumerateFiles(expectedFile, SearchOption.TopDirectoryOnly).SingleOrDefault();
            Execute.Assertion.ForCondition(file == null)
                .FailWithPreformatted($"File '{expectedFile}' should not be found in directory '{_dirInfo.FullName}'.");
            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> HaveFiles(IEnumerable<string> expectedFiles)
        {
            foreach (var expectedFile in expectedFiles)
            {
                HaveFile(expectedFile);
            }

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> NotHaveFiles(IEnumerable<string> expectedFiles)
        {
            foreach (var expectedFile in expectedFiles)
            {
                NotHaveFile(expectedFile);
            }

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> HaveDirectory(string expectedDir)
        {
            var dir = _dirInfo.EnumerateDirectories(expectedDir, SearchOption.TopDirectoryOnly).SingleOrDefault();
            Execute.Assertion.ForCondition(dir != null)
                .FailWithPreformatted($"Expected directory '{expectedDir}' cannot be found inside directory '{_dirInfo.FullName}'.");

            return new AndConstraint<DirectoryInfoAssertions>(new DirectoryInfoAssertions(dir));
        }

        public AndConstraint<DirectoryInfoAssertions> NotHaveDirectory(string expectedDir)
        {
            var dir = _dirInfo.EnumerateDirectories(expectedDir, SearchOption.TopDirectoryOnly).SingleOrDefault();
            Execute.Assertion.ForCondition(dir == null)
                .FailWithPreformatted($"Directory '{expectedDir}' should not be found in found inside directory '{_dirInfo.FullName}'.");

            return new AndConstraint<DirectoryInfoAssertions>(new DirectoryInfoAssertions(dir));
        }

        public AndConstraint<DirectoryInfoAssertions> OnlyHaveFiles(IEnumerable<string> expectedFiles)
        {
            var actualFiles = _dirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Select(f => f.Name);
            var missingFiles = Enumerable.Except(expectedFiles, actualFiles);
            var extraFiles = Enumerable.Except(actualFiles, expectedFiles);
            var nl = Environment.NewLine;

            Execute.Assertion.ForCondition(!missingFiles.Any())
                .FailWithPreformatted($"Following files cannot be found inside directory {_dirInfo.FullName} {nl} {string.Join(nl, missingFiles)}");

            Execute.Assertion.ForCondition(!extraFiles.Any())
                .FailWithPreformatted($"Following extra files are found inside directory {_dirInfo.FullName} {nl} {string.Join(nl, extraFiles)}");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> NotBeModifiedAfter(DateTime timeUtc)
        {
            _dirInfo.Refresh();
            DateTime writeTime = _dirInfo.LastWriteTimeUtc;

            Execute.Assertion.ForCondition(writeTime <= timeUtc)
                .FailWithPreformatted($"Directory '{_dirInfo.FullName}' should not be modified after {timeUtc}, but is modified at {writeTime}.");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

    }
}
