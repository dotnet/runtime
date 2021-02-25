// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>Specifies whether to compress or decompress the underlying stream.</summary>
    /// <remarks>This enumeration is used with the <see cref="System.IO.Compression.GZipStream" /> and <see cref="System.IO.Compression.DeflateStream" /> classes.</remarks>
    /// <example>The following code example uses the <see cref="System.IO.Compression.CompressionMode" /> enumeration with the <see cref="System.IO.Compression.GZipStream" /> class to compress and decompress a file.
    /// <format type="text/markdown"><![CDATA[
    /// [!code-csharp[IO.Compression.GZip1#1](~/samples/snippets/csharp/VS_Snippets_CLR/IO.Compression.GZip1/CS/gziptest.cs#1)]
    /// [!code-vb[IO.Compression.GZip1#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/IO.Compression.GZip1/VB/gziptest.vb#1)]
    /// ]]></format></example>
    public enum CompressionMode
    {
        /// <summary>Decompresses the underlying stream.</summary>
        Decompress = 0,
        /// <summary>Compresses the underlying stream.</summary>
        Compress = 1
    }
}
