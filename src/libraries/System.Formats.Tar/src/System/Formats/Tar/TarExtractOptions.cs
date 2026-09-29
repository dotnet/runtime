// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Tar
{
    /// <summary>
    /// Provides options for extracting tar archives.
    /// </summary>
    public sealed class TarExtractOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether to overwrite existing files when extracting.
        /// </summary>
        /// <value>The default value is <see langword="false"/>.</value>
        public bool OverwriteFiles { get; set; }

        /// <summary>
        /// Gets or sets how hard link entries are handled when extracting tar archives.
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
    }
}
