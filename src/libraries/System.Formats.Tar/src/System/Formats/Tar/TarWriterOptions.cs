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
        /// Gets or sets how symbolic links are handled when writing tar entries from disk.
        /// </summary>
        /// <value>The default value is <see cref="TarSymbolicLinkMode.PreserveLink"/>.</value>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a defined <see cref="TarSymbolicLinkMode"/> value.</exception>
        public TarSymbolicLinkMode SymbolicLinkMode
        {
            get => field;
            set
            {
                if (value is not TarSymbolicLinkMode.PreserveLink and not TarSymbolicLinkMode.CopyContents and not TarSymbolicLinkMode.Skip)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                field = value;
            }
        }
    }
}
