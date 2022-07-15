// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Tests;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarReaderStreamConformanceTests : StandaloneStreamConformanceTests
    {
        protected override Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData)
        {
            var ms = new MemoryStream();
            using (var writer = new TarWriter(ms, leaveOpen: true))
            {
                var entry = new UstarTarEntry(TarEntryType.RegularFile, "Test");
                if (initialData is not null)
                {
                    entry.DataStream = new MemoryStream(initialData);
                }

                writer.WriteEntry(entry);
            }

            ms.Position = 0;
            return Task.FromResult(new TarReader(ms).GetNextEntry().DataStream);
        }

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream>(null);

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData) => Task.FromResult<Stream>(null);

        protected override bool CanSetLength => false;
    }
}
