// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>Represents a compressed file within a zip archive.</summary>
    /// <remarks>A zip archive contains an entry for each compressed file. The <see cref="System.IO.Compression.ZipArchiveEntry" /> class enables you to examine the properties of an entry, and open or delete the entry. When you open an entry, you can modify the compressed file by writing to the stream for that compressed file.
    /// The methods for manipulating zip archives and their file entries are spread across three classes: <see langword="System.IO.Compression.ZipFile" />, <see cref="System.IO.Compression.ZipArchive" /> and <see cref="System.IO.Compression.ZipArchiveEntry" />.
    /// |To...|Use...|
    /// |---------|----------|
    /// |Create a zip archive from a directory|<see langword="System.IO.Compression.ZipFile.CreateFromDirectory" />|
    /// |Extract the contents of a zip archive to a directory|<see langword="System.IO.Compression.ZipFile.ExtractToDirectory" />|
    /// |Add new files to an existing zip archive|<see cref="O:System.IO.Compression.ZipArchive.CreateEntry" />|
    /// |Retrieve an file in a zip archive|<see cref="System.IO.Compression.ZipArchive.GetEntry" />|
    /// |Retrieve all of the files in a zip archive|<see cref="System.IO.Compression.ZipArchive.Entries" />|
    /// |To open a stream to an individual file contained in a zip archive|<see cref="System.IO.Compression.ZipArchiveEntry.Open" />|
    /// |Delete a file from a zip archive|<see cref="System.IO.Compression.ZipArchiveEntry.Delete" />|
    /// If you reference the `System.IO.Compression.FileSystem` assembly in your project, you can access two extension methods for the <see cref="System.IO.Compression.ZipArchiveEntry" /> class. Those methods are <see langword="System.IO.Compression.ZipFileExtensions.ExtractToFile(System.IO.Compression.ZipArchiveEntry,string)" /> and <see langword="System.IO.Compression.ZipFileExtensions.ExtractToFile(System.IO.Compression.ZipArchiveEntry,string,bool)" />, and they enable you to decompress the contents of the entry to a file. The `System.IO.Compression.FileSystem` assembly is not available in Windows 8. In Windows 8.x Store apps, you can decompress the contents of an archive by using <see cref="System.IO.Compression.DeflateStream" /> or <see cref="System.IO.Compression.GZipStream" />, or you can use the Windows Runtime types <a href="https://go.microsoft.com/fwlink/?LinkId=246358">Compressor](https://go.microsoft.com/fwlink/p/?LinkId=246357) and [Decompressor</a> to compress and decompress files.</remarks>
    /// <example>The first example shows how to create a new entry in a zip archive and write to it.
    /// <format type="text/markdown"><![CDATA[
    /// [!code-csharp[System.IO.Compression.ZipArchiveMode#1](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.ziparchivemode/cs/program1.cs#1)]
    /// [!code-vb[System.IO.Compression.ZipArchiveMode#1](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.ziparchivemode/vb/program1.vb#1)]
    /// ]]></format>
    /// The second example shows how to use the <see langword="System.IO.Compression.ZipFileExtensions.ExtractToFile(System.IO.Compression.ZipArchiveEntry,string)" /> extension method. You must reference the `System.IO.Compression.FileSystem` assembly in your project for the code to execute.
    /// <format type="text/markdown"><![CDATA[
    /// [!code-csharp[System.IO.Compression.ZipArchive#1](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.ziparchive/cs/program1.cs#1)]
    /// [!code-vb[System.IO.Compression.ZipArchive#1](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.ziparchive/vb/program1.vb#1)]
    /// ]]></format></example>
    public partial class ZipArchiveEntry
    {
        internal const ZipVersionMadeByPlatform CurrentZipPlatform = ZipVersionMadeByPlatform.Windows;

        /// <summary>
        /// To get the file name of a ZipArchiveEntry, we should be parsing the FullName based
        /// on the path specifications and requirements of the OS that ZipArchive was created on.
        /// This method takes in a FullName and the platform of the ZipArchiveEntry and returns
        /// the platform-correct file name.
        /// </summary>
        /// <remarks>This method ensures no validation on the paths. Invalid characters are allowed.</remarks>
        internal static string ParseFileName(string path, ZipVersionMadeByPlatform madeByPlatform) =>
            madeByPlatform == ZipVersionMadeByPlatform.Unix ? GetFileName_Unix(path) : GetFileName_Windows(path);
    }
}
