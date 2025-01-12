// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Reflection
{
    /// <summary>
    /// Provides an abstraction for reading binary data from a stream.
    /// </summary>
    /// <seealso cref="Metadata.BlobReader"/>
    /// <seealso cref="PortableExecutable.PEBinaryReader"/>
    internal interface IBinaryReader
    {
        public int Offset { get; set; }

        public byte ReadByte();

        public short ReadInt16();

        public ushort ReadUInt16();

        public int ReadInt32();

        public uint ReadUInt32();

        public ulong ReadUInt64();

        /// <summary>
        /// Reads a fixed-length byte block as a null-padded UTF-8 encoded string.
        /// The padding is not included in the returned string.
        ///
        /// Note that it is legal for UTF-8 strings to contain NUL; if NUL occurs
        /// between non-NUL codepoints, it is not considered to be padding and
        /// is included in the result.
        /// </summary>
        public string ReadNullPaddedUTF8(int byteCount);
    }
}
