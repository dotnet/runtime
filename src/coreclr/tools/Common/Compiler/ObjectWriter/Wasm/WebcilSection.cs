// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Internal.Text;
using Microsoft.NET.WebAssembly.Webcil;

namespace ILCompiler.ObjectWriter
{
    internal class WebcilSection : WasmSection
    {
        public readonly int Index;
        public WebcilSectionHeader Header;
        public readonly Stream _stream;
        private PaddingHelper _paddingHelper;
        public int MinAlignment = 1;

        public uint Padding => Header.SizeOfRawData - (uint)_stream.Length;

        public WebcilSection(Utf8String name, WebcilSectionHeader header, Stream stream, int index)
            : base(WasmSectionType.Data, stream, name)
        {
            Header = header;
            _stream = stream;
            Index = index;
            _paddingHelper = new PaddingHelper(WasmObjectWriter.WebcilSectionAlignment);
        }

        public override int EncodeSize()
        {
            return (int)_stream.Length;
        }

        public override int Emit(Stream outputFileStream)
        {
            // Emit the raw contents of this Webcil section followed by any required padding.
            _stream.Position = 0;
            _stream.CopyTo(outputFileStream);
            _paddingHelper.PadStream(outputFileStream, (int)Padding);

            return (int)_stream.Length + (int)Padding;
        }
    }
}
