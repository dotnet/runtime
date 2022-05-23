// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Tar
{
    /// <summary>
    /// Specifies the tar entry types.
    /// </summary>
    /// <remarks>Tar entries with a metadata entry type are not exposed to the user, they are handled internally.</remarks>
    public enum TarEntryType : byte
    {
        /// <summary>
        /// <para>Regular file.</para>
        /// <para>This entry type is specific to the <see cref="TarFormat.Ustar"/>, <see cref="TarFormat.Pax"/> and <see cref="TarFormat.Gnu"/> formats.</para>
        /// </summary>
        RegularFile = (byte)'0',
        /// <summary>
        /// Hard link.
        /// </summary>
        HardLink = (byte)'1',
        /// <summary>
        /// Symbolic link.
        /// </summary>
        SymbolicLink = (byte)'2',
        /// <summary>
        /// <para>Character device special file.</para>
        /// <para>This entry type is supported only in the Unix platforms for writing.</para>
        /// </summary>
        CharacterDevice = (byte)'3',
        /// <summary>
        /// <para>Character device special file.</para>
        /// <para>This entry type is supported only in the Unix platforms for writing.</para>
        /// </summary>
        BlockDevice = (byte)'4',
        /// <summary>
        /// Directory.
        /// </summary>
        Directory = (byte)'5',
        /// <summary>
        /// <para>FIFO special file.</para>
        /// <para>This entry type is supported only in the Unix platforms for writing.</para>
        /// </summary>
        Fifo = (byte)'6',
        /// <summary>
        /// <para>GNU contiguous file</para>
        /// <para>This entry type is specific to the <see cref="TarFormat.Gnu"/> format, and is treated as a <see cref="RegularFile"/> entry type.</para>
        /// </summary>
        // According to the GNU spec, it's extremely rare to encounter a contiguous entry.
        ContiguousFile = (byte)'7',
        /// <summary>
        /// <para>PAX Extended Attributes entry.</para>
        /// <para>Metadata entry type.</para>
        /// </summary>
        ExtendedAttributes = (byte)'x',
        /// <summary>
        /// <para>PAX Global Extended Attributes entry.</para>
        /// <para>Metadata entry type.</para>
        /// </summary>
        GlobalExtendedAttributes = (byte)'g',
        /// <summary>
        /// <para>GNU directory with a list of entries.</para>
        /// <para>This entry type is specific to the <see cref="TarFormat.Gnu"/> format, and is treated as a <see cref="Directory"/> entry type that contains a data section.</para>
        /// </summary>
        DirectoryList = (byte)'D',
        /// <summary>
        /// <para>GNU long link.</para>
        /// <para>Metadata entry type.</para>
        /// </summary>
        LongLink = (byte)'K',
        /// <summary>
        /// <para>GNU long path.</para>
        /// <para>Metadata entry type.</para>
        /// </summary>
        LongPath = (byte)'L',
        /// <summary>
        /// <para>GNU multi-volume file.</para>
        /// <para>This entry type is specific to the <see cref="TarFormat.Gnu"/> format and is not supported for writing.</para>
        /// </summary>
        MultiVolume = (byte)'M',
        /// <summary>
        /// <para>V7 Regular file.</para>
        /// <para>This entry type is specific to the <see cref="TarFormat.V7"/> format.</para>
        /// </summary>
        V7RegularFile = (byte)'\0',
        /// <summary>
        /// <para>GNU file to be renamed/symlinked.</para>
        /// <para>This entry type is specific to the <see cref="TarFormat.Gnu"/> format. It is considered unsafe and is ignored by other tools.</para>
        /// </summary>
        RenamedOrSymlinked = (byte)'N',
        /// <summary>
        /// <para>GNU sparse file.</para>
        /// <para>This entry type is specific to the <see cref="TarFormat.Gnu"/> format and is not supported for writing.</para>
        /// </summary>
        SparseFile = (byte)'S',
        /// <summary>
        /// <para>GNU tape volume.</para>
        /// <para>This entry type is specific to the <see cref="TarFormat.Gnu"/> format and is not supported for writing.</para>
        /// </summary>
        TapeVolume = (byte)'V',
    }
}
