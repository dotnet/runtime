// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using Internal.TypeSystem;
using Microsoft.NET.WebAssembly.Webcil;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// The data segment of Webcil modules that contains the Webcil payload composed of WebcilSections.
    /// </summary>
    internal sealed class WebcilPayloadDataSegment : IWasmDataSegment
    {
        private readonly WebcilHeader _header;
        private readonly WebcilSection[] _sections;
        private readonly int _alignment;
        private int _paddingBytesCount;

        public WebcilPayloadDataSegment(
            WebcilHeader header,
            WebcilSection[] sections)
        {
            _header = header;
            _sections = sections;
            _alignment = WebCilObjectWriter.WebcilSectionAlignment;
            foreach (WebcilSection section in sections)
            {
                _alignment = Math.Max(_alignment, section.Alignment);
            }
        }

        public int HeaderSize =>
            WasmDataSegmentEncoding.GetHeaderSize(WasmDataSegmentType.Passive, initExpr: null);

        public int FileAlignment => _alignment;

        private int RawContentSize
        {
            get
            {
                int size = WebcilEncoder.HeaderEncodeSize(WebcilVersion.Version1);
                size += _sections.Length * WebcilEncoder.SectionHeaderEncodeSize();
                if (_sections.Length == 0)
                {
                    return AlignmentHelper.AlignUp(size, WebCilObjectWriter.WebcilSectionAlignment);
                }

                WebcilSection lastSection = _sections[_sections.Length - 1];
                Debug.Assert(!lastSection.Header.Equals(default(WebcilSectionHeader)));
                return checked((int)(lastSection.Header.PointerToRawData + lastSection.Header.SizeOfRawData));
            }
        }

        public int ContentSize => checked(RawContentSize + _paddingBytesCount);

        public WasmDataSegmentType SegmentType => WasmDataSegmentType.Passive;

        public int EncodeSize() => HeaderSize + ContentSize;

        public int EmitToStream(Stream outputFileStream)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            int headerSize = WasmDataSegmentEncoding.EncodeHeader(
                headerBuffer,
                WasmDataSegmentType.Passive,
                initExpr: null,
                ContentSize);
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
                int padding = (int)(sectionStart - outputFileStream.Position);
                WasmDataSegmentEncoding.EmitPadding(outputFileStream, padding);
                section.EmitToStream(outputFileStream);
            }

            long payloadEnd = payloadStart + RawContentSize;
            Debug.Assert(outputFileStream.Position <= payloadEnd);
            WasmDataSegmentEncoding.EmitPadding(
                outputFileStream,
                (int)(payloadEnd - outputFileStream.Position));
            WasmDataSegmentEncoding.EmitPadding(outputFileStream, _paddingBytesCount);

            return EncodeSize();
        }

        public void SetTrailingPadding(int trailingBytesCount)
        {
            Debug.Assert(trailingBytesCount >= 0);
            _paddingBytesCount = trailingBytesCount;
        }

        public int GetMemoryAddressOfOffset(int offsetInSegment)
        {
            Debug.Assert(offsetInSegment >= 0 && offsetInSegment <= RawContentSize);
            return offsetInSegment;
        }
    }
}
