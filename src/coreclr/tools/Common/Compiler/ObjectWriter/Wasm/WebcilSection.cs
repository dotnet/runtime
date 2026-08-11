// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using Internal.Text;
using Internal.TypeSystem;
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

    internal sealed class WebcilPayloadDataSegment : IWasmDataSegment
    {
        private readonly WebcilHeader _header;
        private readonly WebcilSection[] _sections;

        public WebcilPayloadDataSegment(
            WebcilHeader header,
            WebcilSection[] sections)
        {
            _header = header;
            _sections = sections;
        }

        public int HeaderSize =>
            WasmDataSegmentEncoding.GetHeaderSize(WasmDataSegmentType.Passive, initExpr: null);

        public int RawContentSize
        {
            get
            {
                int size = WebcilEncoder.HeaderEncodeSize(WebcilVersion.Version1);
                size += _sections.Length * WebcilEncoder.SectionHeaderEncodeSize();
                size = AlignmentHelper.AlignUp(size, WebCilObjectWriter.WebcilSectionAlignment);

                foreach (WebcilSection section in _sections)
                {
                    size += (int)section.Header.SizeOfRawData;
                }

                return size;
            }
        }

        public int Padding { get; set; }

        public int EncodeSize() => HeaderSize + RawContentSize + Padding;

        public int EmitToStream(Stream outputFileStream)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            int headerSize = WasmDataSegmentEncoding.EncodeHeader(
                headerBuffer,
                WasmDataSegmentType.Passive,
                initExpr: null,
                RawContentSize + Padding);
            Debug.Assert(headerSize == HeaderSize);
            outputFileStream.Write(headerBuffer);

            long payloadStart = outputFileStream.Position;
            WebcilEncoder.EmitHeader(_header, outputFileStream);

            foreach (WebcilSection section in _sections)
            {
                WebcilEncoder.EncodeSectionHeader(section.Header, outputFileStream);
            }

            foreach (WebcilSection section in _sections)
            {
                long sectionStart = payloadStart + section.Header.PointerToRawData;
                Debug.Assert(outputFileStream.Position <= sectionStart);
                WasmDataSegmentEncoding.EmitPadding(
                    outputFileStream,
                    (int)(sectionStart - outputFileStream.Position));
                section.EmitToStream(outputFileStream);
            }

            long payloadEnd = payloadStart + RawContentSize;
            Debug.Assert(outputFileStream.Position <= payloadEnd);
            WasmDataSegmentEncoding.EmitPadding(
                outputFileStream,
                (int)(payloadEnd - outputFileStream.Position));
            WasmDataSegmentEncoding.EmitPadding(outputFileStream, Padding);

            return EncodeSize();
        }
    }
}
