// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.TestUtility
{
    internal class MockDirectoryInfo : DirectoryInfoBase
    {
        public MockDirectoryInfo(
            FileSystemOperationRecorder recorder,
            DirectoryInfoBase parentDirectory,
            string fullName,
            string name,
            string[] paths)
        {
            ParentDirectory = parentDirectory;
            Recorder = recorder;
            FullName = fullName;
            Name = name;
            Paths = paths;
        }

        public FileSystemOperationRecorder Recorder { get; }

        public override string FullName { get; }

        public override string Name { get; }

        public override DirectoryInfoBase ParentDirectory { get; }

        public string[] Paths { get; }

        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
        {
            Recorder.Add("EnumerateFileSystemInfos", new { FullName, Name });

            var names = new HashSet<string>();

            foreach (var path in Paths)
            {
                if (!path.Replace('\\', '/').StartsWith(FullName.Replace('\\', '/')))
                {
                    continue;
                }
                var beginPath = FullName.Length;
                var endPath = path.Length;

                var beginSegment = beginPath;
                var endSegment = NextIndex(path, new[] { '/', '\\' }, beginSegment, path.Length);

                if (endPath == endSegment)
                {
                    yield return new MockFileInfo(
                        recorder: Recorder,
                        parentDirectory: this,
                        fullName: path,
                        name: path.Substring(beginSegment, endSegment - beginSegment));
                }
                else
                {
                    var name = path.Substring(beginSegment, endSegment - beginSegment);
                    if (!names.Contains(name))
                    {
                        names.Add(name);
                        yield return new MockDirectoryInfo(
                            recorder: Recorder,
                            parentDirectory: this,
                            fullName: path.Substring(0, endSegment + 1),
                            name: name,
                            paths: Paths);
                    }
                }
            }
        }

        private int NextIndex(string pattern, char[] anyOf, int startIndex, int endIndex)
        {
            var index = pattern.IndexOfAny(anyOf, startIndex, endIndex - startIndex);
            return index == -1 ? endIndex : index;
        }

        public override DirectoryInfoBase GetDirectory(string name)
        {
            if (string.Equals(name, "..", StringComparison.Ordinal))
            {
                var indexOfPenultimateSlash = FullName.LastIndexOf('\\', FullName.Length - 2);
                var fullName = FullName.Substring(0, indexOfPenultimateSlash + 1);
                return new MockDirectoryInfo(
                    recorder: Recorder,
                    parentDirectory: this,
                    fullName: FullName.Substring(0, indexOfPenultimateSlash + 1),
                    name: name,
                    paths: Paths);
            }
            return new MockDirectoryInfo(
                recorder: Recorder,
                parentDirectory: this,
                fullName: FullName + name + "\\",
                name: name,
                paths: Paths);
        }

        public override FileInfoBase GetFile(string name)
        {
            return new MockFileInfo(
                recorder: Recorder,
                parentDirectory: this,
                fullName: FullName + name,
                name: name);
        }
    }
}
