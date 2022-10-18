// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.IO;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Internal implementation of <see cref="ElfReader{TDecoder}"/> with a <see cref="ElfDecoderSwap"/>.
    /// </summary>
    internal sealed class ElfReaderSwap : ElfReader<ElfDecoderSwap>
    {
        public ElfReaderSwap(ElfObjectFile elfObjectFile, Stream stream, ElfReaderOptions options) : base(elfObjectFile, stream, options)
        {
        }
    }
}