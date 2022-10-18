// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Ar
{
    /// <summary>
    /// The type of archive.
    /// </summary>
    public enum ArArchiveKind
    {
        /// <summary>
        /// The common variant, used for example by 'deb' package files.
        /// Supports only file names up to 16 characters.
        /// </summary>
        Common,

        /// <summary>
        /// The GNU variant, used by the `ar` utility on GNU and other systems (including Windows)
        /// Based on <see cref="Common"/> file format, but using a different strategy
        /// for storing long file names, incompatible with <see cref="Common"/> format.
        /// </summary>
        GNU,

        /// <summary>
        /// The BSD variant, used by the `ar` utility on BSD systems (including MacOS)
        /// Based on <see cref="Common"/> file format and backward compatible with it,
        /// but allows to store longer file names and file names containing space.
        /// </summary>
        BSD,
    }
}