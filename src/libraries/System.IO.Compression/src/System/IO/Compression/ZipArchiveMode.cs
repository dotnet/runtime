// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>Specifies values for interacting with zip archive entries.</summary>
    /// <remarks>When you set the mode to Read, the underlying file or stream must support reading, but does not have to support seeking. If the underlying file or stream supports seeking, the files are read from the archive as they are requested. If the underlying file or stream does not support seeking, the entire archive is held in memory.
    /// When you set the mode to Create, the underlying file or stream must support writing, but does not have to support seeking. Each entry in the archive can be opened only once for writing. If you create a single entry, the data is written to the underlying stream or file as soon as it is available. If you create multiple entries, such as by calling the <see langword="System.IO.Compression.ZipFile.CreateFromDirectory" /> method, the data is written to the underlying stream or file after all the entries are created.
    /// When you set the mode to Update, the underlying file or stream must support reading, writing, and seeking. The content of the entire archive is held in memory, and no data is written to the underlying file or stream until the archive is disposed.
    /// The following methods include a parameter named `mode` that lets you specify the archive mode:
    /// -   <see cref="System.IO.Compression.ZipArchive.ZipArchive(System.IO.Stream,System.IO.Compression.ZipArchiveMode,bool)" />
    /// -   <see cref="System.IO.Compression.ZipArchive.ZipArchive(System.IO.Stream,System.IO.Compression.ZipArchiveMode)" />
    /// -   <see langword="System.IO.Compression.ZipFile.Open(string,System.IO.Compression.ZipArchiveMode)" /></remarks>
    /// <example>The following example shows how to specify a `ZipArchiveMode` value when creating a <see cref="System.IO.Compression.ZipArchive" /> object.
    /// <format type="text/markdown"><![CDATA[
    /// [!code-csharp[System.IO.Compression.ZipArchiveMode#1](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.ziparchivemode/cs/program1.cs#1)]
    /// [!code-vb[System.IO.Compression.ZipArchiveMode#1](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.ziparchivemode/vb/program1.vb#1)]
    /// ]]></format></example>
    public enum ZipArchiveMode
    {
        /// <summary>Only reading archive entries is permitted.</summary>
        Read,
        /// <summary>Only creating new archive entries is permitted.</summary>
        Create,
        /// <summary>Both read and write operations are permitted for archive entries.</summary>
        Update
    }
}
