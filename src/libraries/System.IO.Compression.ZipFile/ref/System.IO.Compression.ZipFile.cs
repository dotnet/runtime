// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.IO.Compression
{
    public static partial class ZipFile
    {
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName) { }
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, System.IO.Compression.CompressionLevel compressionLevel, bool includeBaseDirectory) { }
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, System.IO.Compression.CompressionLevel compressionLevel, bool includeBaseDirectory, System.Text.Encoding entryNameEncoding) { }
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName) { }
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, System.Text.Encoding entryNameEncoding) { }
        public static System.IO.Compression.ZipArchive Open(string archiveFileName, System.IO.Compression.ZipArchiveMode mode) { return default(System.IO.Compression.ZipArchive); }
        public static System.IO.Compression.ZipArchive Open(string archiveFileName, System.IO.Compression.ZipArchiveMode mode, System.Text.Encoding entryNameEncoding) { return default(System.IO.Compression.ZipArchive); }
        public static System.IO.Compression.ZipArchive OpenRead(string archiveFileName) { return default(System.IO.Compression.ZipArchive); }
    }
    [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
    public static partial class ZipFileExtensions
    {
        public static System.IO.Compression.ZipArchiveEntry CreateEntryFromFile(this System.IO.Compression.ZipArchive destination, string sourceFileName, string entryName) { return default(System.IO.Compression.ZipArchiveEntry); }
        public static System.IO.Compression.ZipArchiveEntry CreateEntryFromFile(this System.IO.Compression.ZipArchive destination, string sourceFileName, string entryName, System.IO.Compression.CompressionLevel compressionLevel) { return default(System.IO.Compression.ZipArchiveEntry); }
        public static void ExtractToDirectory(this System.IO.Compression.ZipArchive source, string destinationDirectoryName) { }
        public static void ExtractToFile(this System.IO.Compression.ZipArchiveEntry source, string destinationFileName) { }
        public static void ExtractToFile(this System.IO.Compression.ZipArchiveEntry source, string destinationFileName, bool overwrite) { }
    }
}
