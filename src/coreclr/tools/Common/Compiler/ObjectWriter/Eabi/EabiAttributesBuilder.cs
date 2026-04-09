// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using static ILCompiler.ObjectWriter.EabiNative;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Builder class for constructing the .ARM.attributes table that
    /// describes the parameters of the emitted ARM code for use by the
    /// linker or debugger.
    /// </summary>
    internal sealed class EabiAttributesBuilder
    {
        private readonly SectionWriter _sectionWriter;
        private long _sectionSizePosition;
        private byte[] _sectionSize;
        private long _subsectionSizePosition;
        private byte[] _subsectionSize;

        public EabiAttributesBuilder(SectionWriter sectionWriter)
        {
            _sectionWriter = sectionWriter;

            // Version
            _sectionWriter.WriteByte(0x41);
        }

        public void StartSection(string vendor)
        {
            Debug.Assert(_sectionSize is null);
            Debug.Assert(_subsectionSize is null);

            _sectionSizePosition = _sectionWriter.Position;
            _sectionSize = new byte[4];
            _sectionWriter.EmitData(_sectionSize);

            _sectionWriter.WriteUtf8String(vendor);

            _sectionWriter.WriteByte((byte)Tag_File);

            _subsectionSizePosition = _sectionWriter.Position;
            _subsectionSize = new byte[4];
            _sectionWriter.EmitData(_subsectionSize);
        }

        public void EndSection()
        {
            Debug.Assert(_sectionSize is not null);
            Debug.Assert(_subsectionSize is not null);

            BinaryPrimitives.WriteUInt32LittleEndian(_subsectionSize, (uint)(_sectionWriter.Position - _subsectionSizePosition));
            BinaryPrimitives.WriteUInt32LittleEndian(_sectionSize, (uint)(_sectionWriter.Position - _sectionSizePosition));

            _sectionSize = null;
            _subsectionSize = null;
        }

        public void WriteAttribute(uint tag, ulong value)
        {
            Debug.Assert(_subsectionSize is not null);

            _sectionWriter.WriteULEB128(tag);
            _sectionWriter.WriteULEB128(value);
        }

        public void WriteAttribute(uint tag, string value)
        {
            Debug.Assert(_subsectionSize is not null);

            _sectionWriter.WriteULEB128(tag);
            _sectionWriter.WriteUtf8String(value);
        }
    }
}
