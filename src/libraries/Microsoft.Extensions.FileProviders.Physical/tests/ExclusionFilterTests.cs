// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Extensions.FileProviders.Physical.Internal;
using Xunit;

namespace Microsoft.Extensions.FileProviders.Physical.Tests
{
    public class ExclusionFilterTests : IDisposable
    {
        private readonly DisposableFileSystem _fileSystem = new DisposableFileSystem();

        public void Dispose()
        {
            _fileSystem.Dispose();
        }

        [Theory]
        [MemberData(nameof(Combinations))]
        public void FiltersExcludedFiles(string filename, FileAttributes attributes, ExclusionFilters filters, bool excluded)
        {
            var fileInfo = new FileInfo(Path.Combine(_fileSystem.RootPath, filename));
            _fileSystem.CreateFile(fileInfo);
            fileInfo.Attributes = attributes;

            Assert.Equal(excluded, FileSystemInfoHelper.IsExcluded(_fileSystem.GetFile(filename), filters));
        }

        [Theory]
        [MemberData(nameof(Combinations))]
        public void FiltersExcludedDirectories(string dirname, FileAttributes attributes, ExclusionFilters filters, bool excluded)
        {
            var dirInfo = new DirectoryInfo(Path.Combine(_fileSystem.RootPath, dirname));
            dirInfo.Create();
            dirInfo.Attributes = attributes;

            Assert.Equal(excluded, FileSystemInfoHelper.IsExcluded(_fileSystem.GetDirectory(dirname), filters));
        }

        public static TheoryData Combinations
        {
            get
            {
                var names = new[] { ".dot", "hidden" };
                var attributes = new List<FileAttributes>()
                {
                    FileAttributes.Normal
                };

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    attributes.AddRange(new[]
                    {
                        FileAttributes.Hidden,
                        FileAttributes.System,
                        FileAttributes.Hidden | FileAttributes.System
                    });
                }

                var combinations = names.Join(attributes, _ => true, _ => true, (name, attr) => new { name, attr }).ToList();
                var data = new TheoryData<string, FileAttributes, ExclusionFilters, bool>();

                foreach (var combo in combinations)
                {
                    data.Add(combo.name, combo.attr, ExclusionFilters.None, false);
                    data.Add(combo.name, combo.attr, ExclusionFilters.System, (combo.attr & FileAttributes.System) != 0);
                    data.Add(combo.name, combo.attr, ExclusionFilters.DotPrefixed, combo.name[0] == '.');

                    data.Add(combo.name, combo.attr, ExclusionFilters.Hidden,
                        (combo.attr & FileAttributes.Hidden) != 0
                        || (combo.name[0] == '.' &&!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)));

                    data.Add(combo.name, combo.attr, ExclusionFilters.Sensitive,
                        combo.name[0] == '.'
                        || (combo.attr & FileAttributes.System) != 0
                        || (combo.attr & FileAttributes.Hidden) != 0);
                }

                return data;
            }
        }
    }
}
