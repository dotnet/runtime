// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines the core part of <see cref="ElfSegmentFlags"/>
    /// </summary>
    [Flags]
    public enum ElfSegmentFlagsCore : uint
    {
        /// <summary>
        /// Segment flags is undefined
        /// </summary>
        None = 0,

        /// <summary>
        /// Segment is executable
        /// </summary>
        Executable = ElfNative.PF_X,

        /// <summary>
        /// Segment is writable
        /// </summary>
        Writable = ElfNative.PF_W,

        /// <summary>
        /// Segment is readable
        /// </summary>
        Readable = ElfNative.PF_R,
    }
}