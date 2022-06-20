// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Formats.Tar
{
    public sealed partial class GnuTarEntry : System.Formats.Tar.PosixTarEntry
    {
        public GnuTarEntry(System.Formats.Tar.TarEntryType entryType, string entryName) { }
        public System.DateTimeOffset AccessTime { get { throw null; } set { } }
        public System.DateTimeOffset ChangeTime { get { throw null; } set { } }
    }
    public sealed partial class PaxTarEntry : System.Formats.Tar.PosixTarEntry
    {
        public PaxTarEntry(System.Formats.Tar.TarEntryType entryType, string entryName) { }
        public PaxTarEntry(System.Formats.Tar.TarEntryType entryType, string entryName, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> extendedAttributes) { }
        public System.Collections.Generic.IReadOnlyDictionary<string, string> ExtendedAttributes { get { throw null; } }
    }
    public abstract partial class PosixTarEntry : System.Formats.Tar.TarEntry
    {
        internal PosixTarEntry() { }
        public int DeviceMajor { get { throw null; } set { } }
        public int DeviceMinor { get { throw null; } set { } }
        public string GroupName { get { throw null; } set { } }
        public string UserName { get { throw null; } set { } }
    }
    public abstract partial class TarEntry
    {
        internal TarEntry() { }
        public int Checksum { get { throw null; } }
        public System.IO.Stream? DataStream { get { throw null; } set { } }
        public System.Formats.Tar.TarEntryType EntryType { get { throw null; } }
        public int Gid { get { throw null; } set { } }
        public long Length { get { throw null; } }
        public string LinkName { get { throw null; } set { } }
        public System.Formats.Tar.TarFileMode Mode { get { throw null; } set { } }
        public System.DateTimeOffset ModificationTime { get { throw null; } set { } }
        public string Name { get { throw null; } set { } }
        public int Uid { get { throw null; } set { } }
        public void ExtractToFile(string destinationFileName, bool overwrite) { }
        public override string ToString() { throw null; }
    }
    public enum TarEntryFormat
    {
        Unknown = 0,
        V7 = 1,
        Ustar = 2,
        Pax = 3,
        Gnu = 4,
    }
    public enum TarEntryType : byte
    {
        V7RegularFile = (byte)0,
        RegularFile = (byte)48,
        HardLink = (byte)49,
        SymbolicLink = (byte)50,
        CharacterDevice = (byte)51,
        BlockDevice = (byte)52,
        Directory = (byte)53,
        Fifo = (byte)54,
        ContiguousFile = (byte)55,
        DirectoryList = (byte)68,
        LongLink = (byte)75,
        LongPath = (byte)76,
        MultiVolume = (byte)77,
        RenamedOrSymlinked = (byte)78,
        SparseFile = (byte)83,
        TapeVolume = (byte)86,
        GlobalExtendedAttributes = (byte)103,
        ExtendedAttributes = (byte)120,
    }
    public static partial class TarFile
    {
        public static void CreateFromDirectory(string sourceDirectoryName, System.IO.Stream destination, bool includeBaseDirectory) { }
        public static void CreateFromDirectory(string sourceDirectoryName, string destinationFileName, bool includeBaseDirectory) { }
        public static void ExtractToDirectory(System.IO.Stream source, string destinationDirectoryName, bool overwriteFiles) { }
        public static void ExtractToDirectory(string sourceFileName, string destinationDirectoryName, bool overwriteFiles) { }
    }
    [System.FlagsAttribute]
    public enum TarFileMode
    {
        None = 0,
        OtherExecute = 1,
        OtherWrite = 2,
        OtherRead = 4,
        GroupExecute = 8,
        GroupWrite = 16,
        GroupRead = 32,
        UserExecute = 64,
        UserWrite = 128,
        UserRead = 256,
        StickyBit = 512,
        GroupSpecial = 1024,
        UserSpecial = 2048,
    }
    public sealed partial class TarReader : System.IDisposable
    {
        public TarReader(System.IO.Stream archiveStream, bool leaveOpen = false) { }
        public System.Formats.Tar.TarEntryFormat Format { get { throw null; } }
        public System.Collections.Generic.IReadOnlyDictionary<string, string>? GlobalExtendedAttributes { get { throw null; } }
        public void Dispose() { }
        public System.Formats.Tar.TarEntry? GetNextEntry(bool copyData = false) { throw null; }
    }
    public sealed partial class TarWriter : System.IDisposable
    {
        public TarWriter(System.IO.Stream archiveStream, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>? globalExtendedAttributes = null, bool leaveOpen = false) { }
        public TarWriter(System.IO.Stream archiveStream, System.Formats.Tar.TarEntryFormat archiveFormat, bool leaveOpen = false) { }
        public System.Formats.Tar.TarEntryFormat Format { get { throw null; } }
        public void Dispose() { }
        public void WriteEntry(System.Formats.Tar.TarEntry entry) { }
        public void WriteEntry(string fileName, string? entryName) { }
    }
    public sealed partial class UstarTarEntry : System.Formats.Tar.PosixTarEntry
    {
        public UstarTarEntry(System.Formats.Tar.TarEntryType entryType, string entryName) { }
    }
    public sealed partial class V7TarEntry : System.Formats.Tar.TarEntry
    {
        public V7TarEntry(System.Formats.Tar.TarEntryType entryType, string entryName) { }
    }
}
