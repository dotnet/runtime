// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.IO.Compression
{
    public sealed partial class ZipExtractionOptions
    {
        public ZipExtractionOptions() { }
        public System.Text.Encoding? EntryNameEncoding { get { throw null; } set { } }
        public bool OverwriteFiles { get { throw null; } set { } }
        public System.ReadOnlyMemory<char> Password { get { throw null; } set { } }
    }
    public static partial class ZipFile
    {
        public static void CreateFromDirectory(string sourceDirectoryName, System.IO.Stream destination) { }
        public static void CreateFromDirectory(string sourceDirectoryName, System.IO.Stream destination, System.IO.Compression.CompressionLevel compressionLevel, bool includeBaseDirectory) { }
        public static void CreateFromDirectory(string sourceDirectoryName, System.IO.Stream destination, System.IO.Compression.CompressionLevel compressionLevel, bool includeBaseDirectory, System.Text.Encoding? entryNameEncoding) { }
        public static void CreateFromDirectory(string sourceDirectoryName, System.IO.Stream destination, System.IO.Compression.ZipFileCreationOptions options) { }
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName) { }
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, System.IO.Compression.CompressionLevel compressionLevel, bool includeBaseDirectory) { }
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, System.IO.Compression.CompressionLevel compressionLevel, bool includeBaseDirectory, System.Text.Encoding? entryNameEncoding) { }
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, System.IO.Compression.ZipFileCreationOptions options) { }
        public static System.Threading.Tasks.Task CreateFromDirectoryAsync(string sourceDirectoryName, System.IO.Stream destination, System.IO.Compression.CompressionLevel compressionLevel, bool includeBaseDirectory, System.Text.Encoding? entryNameEncoding, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task CreateFromDirectoryAsync(string sourceDirectoryName, System.IO.Stream destination, System.IO.Compression.CompressionLevel compressionLevel, bool includeBaseDirectory, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task CreateFromDirectoryAsync(string sourceDirectoryName, System.IO.Stream destination, System.IO.Compression.ZipFileCreationOptions options, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task CreateFromDirectoryAsync(string sourceDirectoryName, System.IO.Stream destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task CreateFromDirectoryAsync(string sourceDirectoryName, string destinationArchiveFileName, System.IO.Compression.CompressionLevel compressionLevel, bool includeBaseDirectory, System.Text.Encoding? entryNameEncoding, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task CreateFromDirectoryAsync(string sourceDirectoryName, string destinationArchiveFileName, System.IO.Compression.CompressionLevel compressionLevel, bool includeBaseDirectory, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task CreateFromDirectoryAsync(string sourceDirectoryName, string destinationArchiveFileName, System.IO.Compression.ZipFileCreationOptions options, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task CreateFromDirectoryAsync(string sourceDirectoryName, string destinationArchiveFileName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static void ExtractToDirectory(System.IO.Stream source, string destinationDirectoryName) { }
        public static void ExtractToDirectory(System.IO.Stream source, string destinationDirectoryName, bool overwriteFiles) { }
        public static void ExtractToDirectory(System.IO.Stream source, string destinationDirectoryName, System.IO.Compression.ZipExtractionOptions options) { }
        public static void ExtractToDirectory(System.IO.Stream source, string destinationDirectoryName, System.Text.Encoding? entryNameEncoding) { }
        public static void ExtractToDirectory(System.IO.Stream source, string destinationDirectoryName, System.Text.Encoding? entryNameEncoding, bool overwriteFiles) { }
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName) { }
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, bool overwriteFiles) { }
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, System.IO.Compression.ZipExtractionOptions options) { }
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, System.Text.Encoding? entryNameEncoding) { }
        public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, System.Text.Encoding? entryNameEncoding, bool overwriteFiles) { }
        public static System.Threading.Tasks.Task ExtractToDirectoryAsync(System.IO.Stream source, string destinationDirectoryName, bool overwriteFiles, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task ExtractToDirectoryAsync(System.IO.Stream source, string destinationDirectoryName, System.IO.Compression.ZipExtractionOptions options, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task ExtractToDirectoryAsync(System.IO.Stream source, string destinationDirectoryName, System.Text.Encoding? entryNameEncoding, bool overwriteFiles, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task ExtractToDirectoryAsync(System.IO.Stream source, string destinationDirectoryName, System.Text.Encoding? entryNameEncoding, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task ExtractToDirectoryAsync(System.IO.Stream source, string destinationDirectoryName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task ExtractToDirectoryAsync(string sourceArchiveFileName, string destinationDirectoryName, bool overwriteFiles, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task ExtractToDirectoryAsync(string sourceArchiveFileName, string destinationDirectoryName, System.IO.Compression.ZipExtractionOptions options, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task ExtractToDirectoryAsync(string sourceArchiveFileName, string destinationDirectoryName, System.Text.Encoding? entryNameEncoding, bool overwriteFiles, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task ExtractToDirectoryAsync(string sourceArchiveFileName, string destinationDirectoryName, System.Text.Encoding? entryNameEncoding, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task ExtractToDirectoryAsync(string sourceArchiveFileName, string destinationDirectoryName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.IO.Compression.ZipArchive Open(string archiveFileName, System.IO.Compression.ZipArchiveMode mode) { throw null; }
        public static System.IO.Compression.ZipArchive Open(string archiveFileName, System.IO.Compression.ZipArchiveMode mode, System.Text.Encoding? entryNameEncoding) { throw null; }
        public static System.Threading.Tasks.Task<System.IO.Compression.ZipArchive> OpenAsync(string archiveFileName, System.IO.Compression.ZipArchiveMode mode, System.Text.Encoding? entryNameEncoding, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<System.IO.Compression.ZipArchive> OpenAsync(string archiveFileName, System.IO.Compression.ZipArchiveMode mode, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.IO.Compression.ZipArchive OpenRead(string archiveFileName) { throw null; }
        public static System.Threading.Tasks.Task<System.IO.Compression.ZipArchive> OpenReadAsync(string archiveFileName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public sealed partial class ZipFileCreationOptions
    {
        public ZipFileCreationOptions() { }
        public System.IO.Compression.CompressionLevel CompressionLevel { get { throw null; } set { } }
        public System.IO.Compression.ZipEncryptionMethod EncryptionMethod { get { throw null; } set { } }
        public System.Text.Encoding? EntryNameEncoding { get { throw null; } set { } }
        public bool IncludeBaseDirectory { get { throw null; } set { } }
        public System.ReadOnlyMemory<char> Password { get { throw null; } set { } }
    }
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static partial class ZipFileExtensions
    {
        public static System.IO.Compression.ZipArchiveEntry CreateEntryFromFile(this System.IO.Compression.ZipArchive destination, string sourceFileName, string entryName) { throw null; }
        public static System.IO.Compression.ZipArchiveEntry CreateEntryFromFile(this System.IO.Compression.ZipArchive destination, string sourceFileName, string entryName, System.IO.Compression.CompressionLevel compressionLevel) { throw null; }
        public static System.IO.Compression.ZipArchiveEntry CreateEntryFromFile(this System.IO.Compression.ZipArchive destination, string sourceFileName, string entryName, System.IO.Compression.CompressionLevel compressionLevel, System.ReadOnlySpan<char> password, System.IO.Compression.ZipEncryptionMethod encryption) { throw null; }
        public static System.IO.Compression.ZipArchiveEntry CreateEntryFromFile(this System.IO.Compression.ZipArchive destination, string sourceFileName, string entryName, System.ReadOnlySpan<char> password, System.IO.Compression.ZipEncryptionMethod encryption) { throw null; }
        public static System.Threading.Tasks.Task<System.IO.Compression.ZipArchiveEntry> CreateEntryFromFileAsync(this System.IO.Compression.ZipArchive destination, string sourceFileName, string entryName, System.IO.Compression.CompressionLevel compressionLevel, System.ReadOnlyMemory<char> password, System.IO.Compression.ZipEncryptionMethod encryption, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<System.IO.Compression.ZipArchiveEntry> CreateEntryFromFileAsync(this System.IO.Compression.ZipArchive destination, string sourceFileName, string entryName, System.IO.Compression.CompressionLevel compressionLevel, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<System.IO.Compression.ZipArchiveEntry> CreateEntryFromFileAsync(this System.IO.Compression.ZipArchive destination, string sourceFileName, string entryName, System.ReadOnlyMemory<char> password, System.IO.Compression.ZipEncryptionMethod encryption, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task<System.IO.Compression.ZipArchiveEntry> CreateEntryFromFileAsync(this System.IO.Compression.ZipArchive destination, string sourceFileName, string entryName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static void ExtractToDirectory(this System.IO.Compression.ZipArchive source, string destinationDirectoryName) { }
        public static void ExtractToDirectory(this System.IO.Compression.ZipArchive source, string destinationDirectoryName, bool overwriteFiles) { }
        public static void ExtractToDirectory(this System.IO.Compression.ZipArchive source, string destinationDirectoryName, System.IO.Compression.ZipExtractionOptions options) { }
        public static System.Threading.Tasks.Task ExtractToDirectoryAsync(this System.IO.Compression.ZipArchive source, string destinationDirectoryName, bool overwriteFiles, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task ExtractToDirectoryAsync(this System.IO.Compression.ZipArchive source, string destinationDirectoryName, System.IO.Compression.ZipExtractionOptions options, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task ExtractToDirectoryAsync(this System.IO.Compression.ZipArchive source, string destinationDirectoryName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static void ExtractToFile(this System.IO.Compression.ZipArchiveEntry source, string destinationFileName) { }
        public static void ExtractToFile(this System.IO.Compression.ZipArchiveEntry source, string destinationFileName, bool overwrite) { }
        public static void ExtractToFile(this System.IO.Compression.ZipArchiveEntry source, string destinationFileName, System.IO.Compression.ZipExtractionOptions options) { }
        public static System.Threading.Tasks.Task ExtractToFileAsync(this System.IO.Compression.ZipArchiveEntry source, string destinationFileName, bool overwrite, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task ExtractToFileAsync(this System.IO.Compression.ZipArchiveEntry source, string destinationFileName, System.IO.Compression.ZipExtractionOptions options, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static System.Threading.Tasks.Task ExtractToFileAsync(this System.IO.Compression.ZipArchiveEntry source, string destinationFileName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
}
