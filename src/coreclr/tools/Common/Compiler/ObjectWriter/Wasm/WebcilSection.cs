// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Internal.Text;
using Internal.TypeSystem;
using Microsoft.NET.WebAssembly.Webcil;

namespace ILCompiler.ObjectWriter
{
    internal class WebcilSection : WasmSection
    {
        public readonly int Index;
        public WebcilSectionHeader Header;
        public int Alignment { get; private set; } = WebCilObjectWriter.WebcilSectionAlignment;

        public uint Padding => Header.SizeOfRawData - (uint)_stream.Length;

        public WebcilSection(Utf8String name, WebcilSectionHeader header, Stream stream, int index)
            : base(WasmSectionType.Data, stream, name)
        {
            Header = header;
        }

        public void UpdateAlignment(int alignment)
        {
            Debug.Assert(BitOperations.IsPow2(alignment));
            Alignment = Math.Max(Alignment, alignment);
        }

        public override int EncodeSize()
        {
            return (int)_stream.Length;
        }

        public override int Emit(Stream outputFileStream)
        {
            // Emit the raw contents of this Webcil section followed by any required padding.
            ContentReadStream.Position = 0;
            ContentReadStream.CopyTo(outputFileStream);
            WasmDataSegmentEncoding.EmitPadding(outputFileStream, (int)Padding);

            return (int)_stream.Length + (int)Padding;
        }
    }
}
