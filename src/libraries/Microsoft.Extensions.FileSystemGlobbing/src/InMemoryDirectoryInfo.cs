// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing.Internal;

namespace Microsoft.Extensions.FileSystemGlobbing
{
    /// <summary>
    /// Avoids using disk for uses like Pattern Matching.
    /// </summary>
    public class InMemoryDirectoryInfo : DirectoryInfoBase
    {
        private static readonly char[] DirectorySeparators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        private readonly IEnumerable<string> _files;

        /// <summary>
        /// Creates a new InMemoryDirectoryInfo with the root directory and files given.
        /// </summary>
        /// <param name="rootDir">The root directory that this FileSystem will use.</param>
        /// <param name="files">Collection of file names. If relative paths <paramref name="rootDir"/> will be prepended to the paths.</param>
        public InMemoryDirectoryInfo(string rootDir, IEnumerable<string>? files)
            : this(rootDir, files, false)
        {
        }

        private InMemoryDirectoryInfo(string rootDir, IEnumerable<string>? files, bool normalized)
        {
            if (string.IsNullOrEmpty(rootDir))
            {
                throw new ArgumentNullException(nameof(rootDir));
            }

            files ??= new List<string>();

            Name = Path.GetFileName(rootDir);
            if (normalized)
            {
                _files = files;
                FullName = rootDir;
            }
            else
            {
                var fileList = new List<string>(files.Count());
                string normalizedRoot = Path.GetFullPath(rootDir.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));

                // normalize
                foreach (string file in files)
                {
                    if (Path.IsPathRooted(file))
                    {
                        fileList.Add(Path.GetFullPath(file.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)));
                    }
                    else
                    {
                        fileList.Add(Path.Combine(normalizedRoot, file.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)));
                    }
                }

                _files = fileList;

                FullName = normalizedRoot;
            }
        }

        /// <inheritdoc />
        public override string FullName { get; }

        /// <inheritdoc />
        public override string Name { get; }

        /// <inheritdoc />
        public override DirectoryInfoBase? ParentDirectory =>
            new InMemoryDirectoryInfo(Path.GetDirectoryName(FullName)!, _files, true);

        /// <inheritdoc />
        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
        {
            var dict = new Dictionary<string, List<string>>();
            foreach (string file in _files)
            {
                if (!IsRootDirectory(FullName, file))
                {
                    continue;
                }

                int endPath = file.Length;
                int beginSegment = FullName.Length + 1;
                int endSegment = file.IndexOfAny(DirectorySeparators, beginSegment, endPath - beginSegment);

                if (endSegment == -1)
                {
                    yield return new InMemoryFileInfo(file, this);
                }
                else
                {
                    string name = file.Substring(0, endSegment);
                    if (!dict.TryGetValue(name, out List<string>? list))
                    {
                        dict[name] = new List<string> { file };
                    }
                    else
                    {
                        list.Add(file);
                    }
                }
            }

            foreach (KeyValuePair<string, List<string>> item in dict)
            {
                yield return new InMemoryDirectoryInfo(item.Key, item.Value, true);
            }
        }

        private static bool IsRootDirectory(string rootDir, string filePath)
        {
            int rootDirLength = rootDir.Length;

            return filePath.StartsWith(rootDir, StringComparison.Ordinal) &&
                (rootDir[rootDirLength - 1] == Path.DirectorySeparatorChar ||
                filePath.IndexOf(Path.DirectorySeparatorChar, rootDirLength) == rootDirLength);
        }

        /// <inheritdoc />
        public override DirectoryInfoBase GetDirectory(string path)
        {
            if (string.Equals(path, "..", StringComparison.Ordinal))
            {
                return new InMemoryDirectoryInfo(Path.Combine(FullName, path), _files, true);
            }
            else
            {
                string normPath = Path.GetFullPath(path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
                return new InMemoryDirectoryInfo(normPath, _files, true);
            }
        }

        /// <summary>
        /// Returns an instance of <see cref="FileInfoBase"/> that matches the <paramref name="path"/> given.
        /// </summary>
        /// <param name="path">The filename.</param>
        /// <returns>Instance of <see cref="FileInfoBase"/> if the file exists, null otherwise.</returns>
        public override FileInfoBase? GetFile(string path)
        {
            string normPath = Path.GetFullPath(path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
            foreach (string file in _files)
            {
                if (string.Equals(file, normPath))
                {
                    return new InMemoryFileInfo(file, this);
                }
            }

            return null;
        }
    }
}
