// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.IO.Compression
{
    public abstract class CompressionTestBase
    {
        public static IEnumerable<object[]> UncompressedTestFiles()
        {
            yield return new object[] { Path.Combine("UncompressedTestFiles", "TestDocument.doc") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "TestDocument.docx") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "TestDocument.pdf") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "TestDocument.txt") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "alice29.txt") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "asyoulik.txt") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "cp.html") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "fields.c") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "grammar.lsp") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "kennedy.xls") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "lcet10.txt") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "plrabn12.txt") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "ptt5") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "sum") };
            yield return new object[] { Path.Combine("UncompressedTestFiles", "xargs.1") };
        }
        protected virtual string UncompressedTestFile() => Path.Combine("UncompressedTestFiles", "TestDocument.pdf");
        protected abstract string CompressedTestFile(string uncompressedPath);
    }

    public abstract class CompressionStreamTestBase : CompressionTestBase
    {
        public abstract Stream CreateStream(Stream stream, CompressionMode mode);
        public abstract Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen);
        public abstract Stream CreateStream(Stream stream, CompressionLevel level);
        public abstract Stream CreateStream(Stream stream, CompressionLevel level, bool leaveOpen);
        public abstract Stream BaseStream(Stream stream);
        public virtual bool FlushCompletes { get => true; }
        public virtual bool FlushNoOps { get => false; }
        public virtual int BufferSize { get => 8192; }
    }
}
