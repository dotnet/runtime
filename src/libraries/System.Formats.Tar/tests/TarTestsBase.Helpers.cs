// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace System.Formats.Tar.Tests
{
    // Helper methods that allow tests to be parameterized by a bool to run both
    // sync and async code paths, following the same pattern used by ZipArchive tests
    // (see Common/tests/System/IO/Compression/ZipTestHelper.cs).
    public abstract partial class TarTestsBase
    {
        protected static readonly bool[] Booleans = [false, true];

        public static IEnumerable<object[]> GetBooleanData() => Booleans.Select(b => new object[] { b });

        public static IEnumerable<object[]> GetFormatBooleanData()
        {
            foreach (TarEntryFormat format in new[] { TarEntryFormat.V7, TarEntryFormat.Ustar, TarEntryFormat.Pax, TarEntryFormat.Gnu })
                foreach (bool async in Booleans)
                    yield return new object[] { format, async };
        }

        protected static TarReader CreateTarReader(Stream archiveStream, bool leaveOpen = false)
        {
            return new TarReader(archiveStream, leaveOpen);
        }

        protected static async Task DisposeTarReader(TarReader reader, bool async = false)
        {
            if (async)
            {
                await reader.DisposeAsync();
            }
            else
            {
                reader.Dispose();
            }
        }

        protected static async Task<TarEntry?> GetNextEntry(TarReader reader, bool copyData = false, bool async = false)
        {
            return async
                ? await reader.GetNextEntryAsync(copyData)
                : reader.GetNextEntry(copyData);
        }

        protected static TarWriter CreateTarWriter(Stream archiveStream, TarEntryFormat format = TarEntryFormat.Pax, bool leaveOpen = false)
        {
            return new TarWriter(archiveStream, format, leaveOpen);
        }

        protected static async Task DisposeTarWriter(TarWriter writer, bool async = false)
        {
            if (async)
            {
                await writer.DisposeAsync();
            }
            else
            {
                writer.Dispose();
            }
        }

        protected static async Task WriteEntry(TarWriter writer, TarEntry entry, bool async = false)
        {
            if (async)
            {
                await writer.WriteEntryAsync(entry);
            }
            else
            {
                writer.WriteEntry(entry);
            }
        }

        protected static async Task WriteEntry(TarWriter writer, string fileName, string? entryName, bool async = false)
        {
            if (async)
            {
                await writer.WriteEntryAsync(fileName, entryName);
            }
            else
            {
                writer.WriteEntry(fileName, entryName);
            }
        }

        protected static async Task ExtractToFile(TarEntry entry, string destinationFileName, bool overwrite, bool async = false)
        {
            if (async)
            {
                await entry.ExtractToFileAsync(destinationFileName, overwrite);
            }
            else
            {
                entry.ExtractToFile(destinationFileName, overwrite);
            }
        }

        protected static async Task CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, bool includeBaseDirectory, bool async = false)
        {
            if (async)
            {
                await TarFile.CreateFromDirectoryAsync(sourceDirectoryName, destinationArchiveFileName, includeBaseDirectory);
            }
            else
            {
                TarFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, includeBaseDirectory);
            }
        }

        protected static async Task CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, bool includeBaseDirectory, TarEntryFormat format, bool async = false)
        {
            if (async)
            {
                await TarFile.CreateFromDirectoryAsync(sourceDirectoryName, destinationArchiveFileName, includeBaseDirectory, format);
            }
            else
            {
                TarFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, includeBaseDirectory, format);
            }
        }

        protected static async Task CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, bool includeBaseDirectory, TarWriterOptions options, bool async = false)
        {
            if (async)
            {
                await TarFile.CreateFromDirectoryAsync(sourceDirectoryName, destinationArchiveFileName, includeBaseDirectory, options);
            }
            else
            {
                TarFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, includeBaseDirectory, options);
            }
        }

        protected static async Task CreateFromDirectory(string sourceDirectoryName, Stream destination, bool includeBaseDirectory, bool async = false)
        {
            if (async)
            {
                await TarFile.CreateFromDirectoryAsync(sourceDirectoryName, destination, includeBaseDirectory);
            }
            else
            {
                TarFile.CreateFromDirectory(sourceDirectoryName, destination, includeBaseDirectory);
            }
        }

        protected static async Task CreateFromDirectory(string sourceDirectoryName, Stream destination, bool includeBaseDirectory, TarEntryFormat format, bool async = false)
        {
            if (async)
            {
                await TarFile.CreateFromDirectoryAsync(sourceDirectoryName, destination, includeBaseDirectory, format);
            }
            else
            {
                TarFile.CreateFromDirectory(sourceDirectoryName, destination, includeBaseDirectory, format);
            }
        }

        protected static async Task CreateFromDirectory(string sourceDirectoryName, Stream destination, bool includeBaseDirectory, TarWriterOptions options, bool async = false)
        {
            if (async)
            {
                await TarFile.CreateFromDirectoryAsync(sourceDirectoryName, destination, includeBaseDirectory, options);
            }
            else
            {
                TarFile.CreateFromDirectory(sourceDirectoryName, destination, includeBaseDirectory, options);
            }
        }

        protected static async Task ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, bool overwriteFiles, bool async = false)
        {
            if (async)
            {
                await TarFile.ExtractToDirectoryAsync(sourceArchiveFileName, destinationDirectoryName, overwriteFiles);
            }
            else
            {
                TarFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, overwriteFiles);
            }
        }

        protected static async Task ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, TarExtractOptions options, bool async = false)
        {
            if (async)
            {
                await TarFile.ExtractToDirectoryAsync(sourceArchiveFileName, destinationDirectoryName, options);
            }
            else
            {
                TarFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, options);
            }
        }

        protected static async Task ExtractToDirectory(Stream source, string destinationDirectoryName, bool overwriteFiles, bool async = false)
        {
            if (async)
            {
                await TarFile.ExtractToDirectoryAsync(source, destinationDirectoryName, overwriteFiles);
            }
            else
            {
                TarFile.ExtractToDirectory(source, destinationDirectoryName, overwriteFiles);
            }
        }
    }
}
