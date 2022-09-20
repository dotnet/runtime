// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Formats.Tar
{
    /// <summary>
    /// Abstract class that represents a tar entry from an archive of a format that is based on the POSIX IEEE P1003.1 standard from 1988. This includes the formats <see cref="TarEntryFormat.Ustar"/> (represented by the <see cref="UstarTarEntry"/> class), <see cref="TarEntryFormat.Pax"/> (represented by the <see cref="PaxTarEntry"/> class) and <see cref="TarEntryFormat.Gnu"/> (represented by the <see cref="GnuTarEntry"/> class).
    /// </summary>
    /// <remarks>Formats that implement the POSIX IEEE P1003.1 standard from 1988, support the following header fields: <c>devmajor</c>, <c>devminor</c>, <c>gname</c> and <c>uname</c>.
    /// Even though the <see cref="TarEntryFormat.Gnu"/> format is not POSIX compatible, it implements and supports the Unix-specific fields that were defined in that POSIX standard.</remarks>
    public abstract partial class PosixTarEntry : TarEntry
    {
        // Constructor called when reading a TarEntry from a TarReader.
        internal PosixTarEntry(TarHeader header, TarReader readerOfOrigin, TarEntryFormat format)
            : base(header, readerOfOrigin, format)
        {
        }

        // Constructor called when the user creates a TarEntry instance from scratch.
        internal PosixTarEntry(TarEntryType entryType, string entryName, TarEntryFormat format, bool isGea)
            : base(entryType, entryName, format, isGea)
        {
            _header.UName = string.Empty;
            _header.GName = string.Empty;
            _header._devMajor = 0;
            _header._devMinor = 0;
        }

        // Constructor called when converting an entry to the selected format.
        internal PosixTarEntry(TarEntry other, TarEntryFormat format)
            : base(other, format)
        {
            if (other is PosixTarEntry)
            {
                Debug.Assert(other._header.UName != null);
                Debug.Assert(other._header.GName != null);
                _header.UName = other._header.UName;
                _header.GName = other._header.GName;
                _header._devMajor = other._header._devMajor;
                _header._devMinor = other._header._devMinor;
            }
            _header.UName ??= string.Empty;
            _header.GName ??= string.Empty;
        }

        /// <summary>
        /// When the current entry represents a character device or a block device, the major number identifies the driver associated with the device.
        /// </summary>
        /// <remarks>Character and block devices are Unix-specific entry types.</remarks>
        /// <exception cref="InvalidOperationException">The entry does not represent a block device or a character device.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The value is negative, or larger than 2097151.</exception>
        public int DeviceMajor
        {
            get => _header._devMajor;
            set
            {
                if (_header._typeFlag is not TarEntryType.BlockDevice and not TarEntryType.CharacterDevice)
                {
                    throw new InvalidOperationException(SR.TarEntryBlockOrCharacterExpected);
                }

                if (value < 0 || value > 2097151) // 7777777 in octal
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _header._devMajor = value;
            }
        }

        /// <summary>
        /// When the current entry represents a character device or a block device, the minor number is used by the driver to distinguish individual devices it controls.
        /// </summary>
        /// <remarks>Character and block devices are Unix-specific entry types.</remarks>
        /// <exception cref="InvalidOperationException">The entry does not represent a block device or a character device.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The value is negative, or larger than 2097151.</exception>
        public int DeviceMinor
        {
            get => _header._devMinor;
            set
            {
                if (_header._typeFlag is not TarEntryType.BlockDevice and not TarEntryType.CharacterDevice)
                {
                    throw new InvalidOperationException(SR.TarEntryBlockOrCharacterExpected);
                }
                if (value < 0 || value > 2097151) // 7777777 in octal
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _header._devMinor = value;
            }
        }

        /// <summary>
        /// Represents the name of the group that owns this entry.
        /// </summary>
        /// <exception cref="ArgumentNullException">Cannot set a null group name.</exception>
        /// <remarks><see cref="GroupName"/> is only used in Unix platforms.</remarks>
        public string GroupName
        {
            get => _header.GName ?? string.Empty;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _header.GName = value;
            }
        }

        /// <summary>
        /// Represents the name of the user that owns this entry.
        /// </summary>
        /// <remarks><see cref="UserName"/> is only used in Unix platforms.</remarks>
        /// <exception cref="ArgumentNullException">Cannot set a null user name.</exception>
        public string UserName
        {
            get => _header.UName ?? string.Empty;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _header.UName = value;
            }
        }
    }
}
