// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Tar
{
    /// <summary>
    /// Provides options for <see cref="TarWriter"/>.
    /// </summary>
    public sealed class TarWriterOptions
    {
        /// <summary>
        /// Gets or sets the format of the entries when writing entries to the archive using the <see cref="TarWriter.WriteEntry(string, string?)"/> method.
        /// </summary>
        /// <value>The default value is <see cref="TarEntryFormat.Pax"/>.</value>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is either <see cref="TarEntryFormat.Unknown"/>, or not one of the other enum values.</exception>
        public TarEntryFormat Format
        {
            get => field;
            set
            {
                if (value is not TarEntryFormat.V7 and not TarEntryFormat.Ustar and not TarEntryFormat.Pax and not TarEntryFormat.Gnu)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                field = value;
            }
        } = TarEntryFormat.Pax;

        /// <summary>
        /// Gets or sets how hard links are handled when writing tar entries from disk.
        /// </summary>
        /// <value>The default value is <see cref="TarHardLinkMode.PreserveLink"/>.</value>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a defined <see cref="TarHardLinkMode"/> value.</exception>
        public TarHardLinkMode HardLinkMode
        {
            get => field;
            set
            {
                if (value is not TarHardLinkMode.PreserveLink and not TarHardLinkMode.CopyContents)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                field = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the writer should avoid process- and host-dependent metadata.
        /// </summary>
        /// <value>The default value is <see langword="false"/>.</value>
        /// <remarks>
        /// <para>When enabled, PAX extended and global extended header names do not contain the process ID or temporary directory path.</para>
        /// <para>Entries created by <see cref="TarWriter.WriteEntry(string, string?)"/> use zero user and group IDs, empty user and group names, and <see cref="DateTimeOffset.UnixEpoch"/> as the modification time unless the corresponding override property is set.</para>
        /// <para>Metadata explicitly set on entries passed to <see cref="TarWriter.WriteEntry(TarEntry)"/> is preserved.</para>
        /// </remarks>
        public bool Deterministic { get; set; }

        /// <summary>
        /// Gets or sets the modification time used for entries created from filesystem paths.
        /// </summary>
        /// <value>
        /// <see langword="null"/> to preserve the source filesystem timestamp when <see cref="Deterministic"/> is <see langword="false"/>,
        /// or to use <see cref="DateTimeOffset.UnixEpoch"/> when <see cref="Deterministic"/> is <see langword="true"/>.
        /// </value>
        public DateTimeOffset? OverrideModificationTime { get; set; }

        /// <summary>
        /// Gets or sets the user ID used for entries created from filesystem paths.
        /// </summary>
        /// <value>
        /// <see langword="null"/> to preserve the source filesystem value when <see cref="Deterministic"/> is <see langword="false"/>,
        /// or to use zero when <see cref="Deterministic"/> is <see langword="true"/>.
        /// </value>
        public int? OverrideUid { get; set; }

        /// <summary>
        /// Gets or sets the group ID used for entries created from filesystem paths.
        /// </summary>
        /// <value>
        /// <see langword="null"/> to preserve the source filesystem value when <see cref="Deterministic"/> is <see langword="false"/>,
        /// or to use zero when <see cref="Deterministic"/> is <see langword="true"/>.
        /// </value>
        public int? OverrideGid { get; set; }

        /// <summary>
        /// Gets or sets the user name used for entries created from filesystem paths.
        /// </summary>
        /// <value>
        /// <see langword="null"/> to preserve the source filesystem value when <see cref="Deterministic"/> is <see langword="false"/>,
        /// or to use an empty string when <see cref="Deterministic"/> is <see langword="true"/>.
        /// </value>
        public string? OverrideUName { get; set; }

        /// <summary>
        /// Gets or sets the group name used for entries created from filesystem paths.
        /// </summary>
        /// <value>
        /// <see langword="null"/> to preserve the source filesystem value when <see cref="Deterministic"/> is <see langword="false"/>,
        /// or to use an empty string when <see cref="Deterministic"/> is <see langword="true"/>.
        /// </value>
        public string? OverrideGName { get; set; }
    }
}
