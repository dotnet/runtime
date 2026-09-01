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

        protected static IEnumerable<object[]> GetDataAndBooleanData(IEnumerable<object[]> data)
        {
            foreach (object[] values in data)
            {
                foreach (bool isAsync in Booleans)
                {
                    yield return values.Append((object)isAsync).ToArray();
                }
            }
        }

        // Yields all four combinations of two independent boolean parameters (e.g. some test-specific
        // flag combined with the sync/async flag).
        public static IEnumerable<object[]> GetTwoBooleansData() => GetDataAndBooleanData(GetBooleanData());

        public static IEnumerable<object[]> GetNonV7FormatBooleanData()
        {
            foreach (TarEntryFormat format in new[] { TarEntryFormat.Ustar, TarEntryFormat.Pax, TarEntryFormat.Gnu })
                foreach (bool isAsync in Booleans)
                    yield return new object[] { format, isAsync };
        }

        public static IEnumerable<object[]> GetPaxAndGnuFormatBooleanData()
        {
            foreach (TarEntryFormat format in new[] { TarEntryFormat.Pax, TarEntryFormat.Gnu })
                foreach (bool isAsync in Booleans)
                    yield return new object[] { format, isAsync };
        }

        public static IEnumerable<object[]> GetUstarPaxFormatBooleanData()
        {
            foreach (TarEntryFormat format in new[] { TarEntryFormat.Ustar, TarEntryFormat.Pax })
                foreach (bool isAsync in Booleans)
                    yield return new object[] { format, isAsync };
        }

        public static IEnumerable<object[]> GetV7UstarPaxFormatBooleanData()
        {
            foreach (TarEntryFormat format in new[] { TarEntryFormat.V7, TarEntryFormat.Ustar, TarEntryFormat.Pax })
                foreach (bool isAsync in Booleans)
                    yield return new object[] { format, isAsync };
        }

        public static IEnumerable<object[]> GetFormatsAndFilesAndBooleanData() => GetDataAndBooleanData(GetFormatsAndFiles());

        public static IEnumerable<object[]> GetFormatsAndLinksAndBooleanData() => GetDataAndBooleanData(GetFormatsAndLinks());

        public static IEnumerable<object[]> GetInvalidTarEntryFormatsAndBooleanData() => GetDataAndBooleanData(GetInvalidTarEntryFormats());

        public static IEnumerable<object[]> GetPaxExtendedAttributesRoundtripTestDataAndBooleanData() => GetDataAndBooleanData(GetPaxExtendedAttributesRoundtripTestData());

        public static IEnumerable<object[]> GetTarEntryFormatsAndBooleanData() => GetDataAndBooleanData(GetTarEntryFormats());

        public static IEnumerable<object[]> GetTestTarFormatsAndBooleanData() => GetDataAndBooleanData(GetTestTarFormats());

        protected static TarReader CreateTarReader(Stream archiveStream, bool leaveOpen = false)
        {
            return new TarReader(archiveStream, leaveOpen);
        }

        // Wraps a TarReader together with the sync/async flag that should be used to dispose it,
        // so tests can write "await using TarReaderHolder reader = CreateTarReader(...)" instead of
        // a manual try/finally block calling DisposeTarReader. Implicitly converts to TarReader so
        // it can be used anywhere a TarReader is expected.
        protected readonly struct TarReaderHolder : IAsyncDisposable
        {
            private readonly TarReader _reader;
            private readonly bool _async;

            public TarReaderHolder(TarReader reader, bool async)
            {
                _reader = reader;
                _async = async;
            }

            public static implicit operator TarReader(TarReaderHolder holder) => holder._reader;

            public async ValueTask DisposeAsync() => await DisposeTarReader(_reader, _async);
        }

        protected static TarReaderHolder CreateTarReader(Stream archiveStream, bool async, bool leaveOpen = false)
        {
            return new TarReaderHolder(new TarReader(archiveStream, leaveOpen), async);
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

        // Wraps a TarWriter together with the sync/async flag that should be used to dispose it,
        // so tests can write "await using TarWriterHolder writer = CreateTarWriter(...)" instead of
        // a manual try/finally block calling DisposeTarWriter. Implicitly converts to TarWriter so
        // it can be used anywhere a TarWriter is expected.
        protected readonly struct TarWriterHolder : IAsyncDisposable
        {
            private readonly TarWriter _writer;
            private readonly bool _async;

            public TarWriterHolder(TarWriter writer, bool async)
            {
                _writer = writer;
                _async = async;
            }

            public static implicit operator TarWriter(TarWriterHolder holder) => holder._writer;

            public async ValueTask DisposeAsync() => await DisposeTarWriter(_writer, _async);
        }

        protected static TarWriterHolder CreateTarWriter(Stream archiveStream, bool async, TarEntryFormat format = TarEntryFormat.Pax, bool leaveOpen = false)
        {
            return new TarWriterHolder(new TarWriter(archiveStream, format, leaveOpen), async);
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
