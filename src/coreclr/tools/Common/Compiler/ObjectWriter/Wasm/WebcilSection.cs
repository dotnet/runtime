// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Internal.Text;
using Microsoft.NET.WebAssembly.Webcil;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// A WebCIL section is a subsection of the "webcilPayload" data segment in the WebAssembly module.
    /// </summary>
    internal class WebcilSection : SectionDataEmitter
    {
        public WebcilSectionHeader Header;
        private PaddingHelper _paddingHelper;
        public int MinAlignment = 1;

        public uint Padding => Header.SizeOfRawData - (uint)ContentReadStream.Length;

        public WebcilSection(Utf8String name, WebcilSectionHeader header, Stream stream, int sectionIndex)
            : base(stream, name, sectionIndex)
        {
            Header = header;
            _paddingHelper = new PaddingHelper(WebCilObjectWriter.WebcilSectionAlignment);
        }

        public override int EncodeSize()
        {
            return (int)ContentReadStream.Length;
        }

        public override int EmitToStream(Stream outputFileStream)
        {
            // Emit the raw contents of this Webcil section followed by any required padding.
            ContentReadStream.Position = 0;
            ContentReadStream.CopyTo(outputFileStream);
            _paddingHelper.PadStream(outputFileStream, (int)Padding);

            return (int)ContentReadStream.Length + (int)Padding;
        }
    }
}
