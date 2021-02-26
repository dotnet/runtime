// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>Specifies values that indicate whether a compression operation emphasizes speed or compression size.</summary>
    /// <remarks>Compression operations usually involve a tradeoff between the speed and the effectiveness of compression. You use the <see cref="System.IO.Compression.CompressionLevel" /> enumeration to indicate which factor is more important in your development scenario: the time to complete the compression operation or the size of the compressed file. These values do not correspond to specific compression levels; the object that implements compression determines how to handle them.
    /// The following methods of the <see cref="System.IO.Compression.DeflateStream" />, <see cref="System.IO.Compression.GZipStream" />, <see cref="System.IO.Compression.ZipArchive" />, <see langword="System.IO.Compression.ZipFile" />, and <see langword="System.IO.Compression.ZipFileExtensions" /> classes include a parameter named `compressionLevel` that lets you specify the compression level:
    /// -   <see cref="System.IO.Compression.DeflateStream.DeflateStream(System.IO.Stream,System.IO.Compression.CompressionLevel)" />
    /// -   <see cref="System.IO.Compression.DeflateStream.DeflateStream(System.IO.Stream,System.IO.Compression.CompressionLevel,bool)" />
    /// -   <see cref="System.IO.Compression.GZipStream.GZipStream(System.IO.Stream,System.IO.Compression.CompressionLevel)" />
    /// -   <see cref="System.IO.Compression.GZipStream.GZipStream(System.IO.Stream,System.IO.Compression.CompressionLevel,bool)" />
    /// -   <see cref="System.IO.Compression.ZipArchive.CreateEntry(string,System.IO.Compression.CompressionLevel)" />
    /// -   <see langword="System.IO.Compression.ZipFile.CreateFromDirectory(string,string,System.IO.Compression.CompressionLevel,bool)" />
    /// -   <see langword="System.IO.Compression.ZipFileExtensions.CreateEntryFromFile(System.IO.Compression.ZipArchive,string,string,System.IO.Compression.CompressionLevel)" /></remarks>
    /// <example>The following example shows how to set the compression level when creating a zip archive by using the <see langword="System.IO.Compression.ZipFile" /> class.
    /// <format type="text/markdown"><![CDATA[
    /// [!code-csharp[System.IO.Compression.ZipFile#3](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.zipfile/cs/program3.cs#3)]
    /// [!code-vb[System.IO.Compression.ZipFile#3](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.zipfile/vb/program3.vb#3)]
    /// ]]></format></example>
    public enum CompressionLevel
    {
        /// <summary>The compression operation should be optimally compressed, even if the operation takes a longer time to complete.</summary>
        Optimal = 0,

        /// <summary>The compression operation should complete as quickly as possible, even if the resulting file is not optimally compressed.</summary>
        Fastest = 1,

        /// <summary>No compression should be performed on the file.</summary>
        NoCompression = 2,

        /// <summary>
        /// The compression operation should create output as small as possible, even if the operation takes a longer time to complete.
        /// </summary>
        SmallestSize = 3,
    }
}
