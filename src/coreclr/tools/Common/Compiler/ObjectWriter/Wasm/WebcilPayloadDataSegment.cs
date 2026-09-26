// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.TypeSystem;
using Microsoft.NET.WebAssembly.Webcil;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// The data segment of Webcil modules that contains the Webcil payload composed of WebcilSections.
    /// </summary>
    internal sealed class WebcilPayloadDataSegment : IWasmActiveDataSegment
    {
        private readonly WebcilHeader _header;
        private readonly WebcilSection[] _sections;
        private readonly int _alignment;
        private readonly WasmDataSegmentType _segmentType;
        private readonly WasmInstructionGroup _initExpr;
        private int _paddingBytesCount;

        public WebcilPayloadDataSegment(
            WebcilHeader header,
            WebcilSection[] sections,
            WasmInstructionGroup initExpr = null)
        {
            _header = header;
            _sections = sections;
            _segmentType = initExpr is null ? WasmDataSegmentType.Passive : WasmDataSegmentType.Active;
            _initExpr = initExpr;
            _alignment = WebCilObjectWriter.WebcilSectionAlignment;
            foreach (WebcilSection section in sections)
            {
                _alignment = Math.Max(_alignment, section.Alignment);
            }
        }

        public int HeaderSize =>
            WasmDataSegmentEncoding.GetHeaderSize(_segmentType, _initExpr);

        public int FileAlignment => _alignment;

        public int MemoryAlignment => _alignment;

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

        public WasmDataSegmentType SegmentType => _segmentType;

        public int EncodeSize() => HeaderSize + ContentSize;

        public int EmitToStream(Stream outputFileStream)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            int headerSize = WasmDataSegmentEncoding.EncodeHeader(
                headerBuffer,
                _segmentType,
                _initExpr,
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

        public void SetMemoryOffset(int offset)
        {
            Debug.Assert(_segmentType == WasmDataSegmentType.Active);
        }

        public int GetMemoryAddressOfOffset(int offsetInSegment)
        {
            Debug.Assert(offsetInSegment >= 0 && offsetInSegment <= RawContentSize);
            if (_segmentType == WasmDataSegmentType.Active)
            {
                throw new NotSupportedException("Active WebCIL payload segments use an imported base-address global.");
            }

            return offsetInSegment;
        }
    }
}
