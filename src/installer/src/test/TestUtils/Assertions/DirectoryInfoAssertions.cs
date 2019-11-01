// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                .FailWith("Expected directory {0} does not exist.", _dirInfo.FullName);
            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> HaveFile(string expectedFile)
        {
            var file = _dirInfo.EnumerateFiles(expectedFile, SearchOption.TopDirectoryOnly).SingleOrDefault();
            Execute.Assertion.ForCondition(file != null)
                .FailWith("Expected File {0} cannot be found in directory {1}.", expectedFile, _dirInfo.FullName);
            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> NotHaveFile(string expectedFile)
        {
            var file = _dirInfo.EnumerateFiles(expectedFile, SearchOption.TopDirectoryOnly).SingleOrDefault();
            Execute.Assertion.ForCondition(file == null)
                .FailWith("File {0} should not be found in directory {1}.", expectedFile, _dirInfo.FullName);
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
                .FailWith("Expected directory {0} cannot be found inside directory {1}.", expectedDir, _dirInfo.FullName);

            return new AndConstraint<DirectoryInfoAssertions>(new DirectoryInfoAssertions(dir));
        }

        public AndConstraint<DirectoryInfoAssertions> NotHaveDirectory(string expectedDir)
        {
            var dir = _dirInfo.EnumerateDirectories(expectedDir, SearchOption.TopDirectoryOnly).SingleOrDefault();
            Execute.Assertion.ForCondition(dir == null)
                .FailWith("Directory {0} should not be found in found inside directory {1}.", expectedDir, _dirInfo.FullName);

            return new AndConstraint<DirectoryInfoAssertions>(new DirectoryInfoAssertions(dir));
        }

        public AndConstraint<DirectoryInfoAssertions> OnlyHaveFiles(IEnumerable<string> expectedFiles)
        {
            var actualFiles = _dirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Select(f => f.Name);
            var missingFiles = Enumerable.Except(expectedFiles, actualFiles);
            var extraFiles = Enumerable.Except(actualFiles, expectedFiles);
            var nl = Environment.NewLine;

            Execute.Assertion.ForCondition(!missingFiles.Any())
                .FailWith($"Following files cannot be found inside directory {_dirInfo.FullName} {nl} {string.Join(nl, missingFiles)}");

            Execute.Assertion.ForCondition(!extraFiles.Any())
                .FailWith($"Following extra files are found inside directory {_dirInfo.FullName} {nl} {string.Join(nl, extraFiles)}");

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

        public AndConstraint<DirectoryInfoAssertions> NotBeModifiedAfter(DateTime timeUtc)
        {
            _dirInfo.Refresh();
            DateTime writeTime = _dirInfo.LastWriteTimeUtc;

            Execute.Assertion.ForCondition(writeTime <= timeUtc)
                .FailWith("Directory {0} should not be modified after {1}, but is modified at {2}.", _dirInfo.FullName, timeUtc, writeTime);

            return new AndConstraint<DirectoryInfoAssertions>(this);
        }

    }
}
