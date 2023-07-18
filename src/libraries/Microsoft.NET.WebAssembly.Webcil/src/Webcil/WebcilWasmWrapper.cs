// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Immutable;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace Microsoft.NET.WebAssembly.Webcil;

//
// Emits a simple WebAssembly wrapper module around a given webcil payload.
//
// The entire wasm module is going to be unchanging, except for the data section which has 2 passive
// segments.  segment 0 is 4 bytes and contains the length of the webcil payload.  segment 1 is of a
// variable size and contains the webcil payload.
//
// The unchanging parts are stored as a "prefix" and "suffix" which contain the bytes for the following
// WAT module, split into the parts that come before the data section, and the bytes that come after:
//
// (module
//  (data "\0f\00\00\00") ;; data segment 0: payload size as a 4 byte LE uint32
//  (data "webcil Payload\cc")  ;; data segment 1: webcil payload
//  (memory (import "webcil" "memory") 1)
//  (global (export "webcilVersion") i32 (i32.const 0))
//  (func (export "getWebcilSize") (param $destPtr i32) (result)
//    local.get $destPtr
//    i32.const 0
//    i32.const 4
//    memory.init 0)
//  (func (export "getWebcilPayload") (param $d i32) (param $n i32) (result)
//    local.get $d
//    i32.const 0
//    local.get $n
//    memory.init 1))
public class WebcilWasmWrapper
{
    private readonly Stream _webcilPayloadStream;
    private readonly uint _webcilPayloadSize;

    public WebcilWasmWrapper(Stream webcilPayloadStream)
    {
        _webcilPayloadStream = webcilPayloadStream;
        long len = webcilPayloadStream.Length;
        if (len > (long)uint.MaxValue)
            throw new InvalidOperationException("webcil payload too large");
        _webcilPayloadSize = (uint)len;
    }

    public void WriteWasmWrappedWebcil(Stream outputStream)
    {
        WriteWasmHeader(outputStream);
        using (var writer = new BinaryWriter(outputStream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            WriteDataSection(writer);
        }
        WriteWasmSuffix(outputStream);
    }

    //
    // Everything from the above wat module before the data section
    //
    // extracted by wasm-reader -s wrapper.wasm
    private static
#if NET7_0_OR_GREATER
        ReadOnlyMemory<byte>
#else
        byte[]
#endif
        s_wasmWrapperPrefix = new byte[] {
        0x00, 0x61, 0x73, 0x6d, 0x01, 0x00, 0x00, 0x00, 0x01, 0x0a, 0x02, 0x60, 0x01, 0x7f, 0x00, 0x60, 0x02, 0x7f, 0x7f, 0x00, 0x02, 0x12, 0x01, 0x06, 0x77, 0x65, 0x62, 0x63, 0x69, 0x6c, 0x06, 0x6d,
        0x65, 0x6d, 0x6f, 0x72, 0x79, 0x02, 0x00, 0x01, 0x03, 0x03, 0x02, 0x00, 0x01, 0x06, 0x0b, 0x02, 0x7f, 0x00, 0x41, 0x00, 0x0b, 0x7f, 0x00, 0x41, 0x00, 0x0b, 0x07, 0x41, 0x04, 0x0d, 0x77, 0x65,
        0x62, 0x63, 0x69, 0x6c, 0x56, 0x65, 0x72, 0x73, 0x69, 0x6f, 0x6e, 0x03, 0x00, 0x0a, 0x77, 0x65, 0x62, 0x63, 0x69, 0x6c, 0x53, 0x69, 0x7a, 0x65, 0x03, 0x01, 0x0d, 0x67, 0x65, 0x74, 0x57, 0x65,
        0x62, 0x63, 0x69, 0x6c, 0x53, 0x69, 0x7a, 0x65, 0x00, 0x00, 0x10, 0x67, 0x65, 0x74, 0x57, 0x65, 0x62, 0x63, 0x69, 0x6c, 0x50, 0x61, 0x79, 0x6c, 0x6f, 0x61, 0x64, 0x00, 0x01, 0x0c, 0x01, 0x02,
        0x0a, 0x1b, 0x02, 0x0c, 0x00, 0x20, 0x00, 0x41, 0x00, 0x41, 0x04, 0xfc, 0x08, 0x00, 0x00, 0x0b, 0x0c, 0x00, 0x20, 0x00, 0x41, 0x00, 0x20, 0x01, 0xfc, 0x08, 0x01, 0x00, 0x0b,
    };
    //
    // Everything from the above wat module after the data section
    //
    // extracted by wasm-reader -s wrapper.wasm
    private static
#if NET7_0_OR_GREATER
        ReadOnlyMemory<byte>
#else
        byte[]
#endif
        s_wasmWrapperSuffix = new byte[] {
        0x00, 0x1b, 0x04, 0x6e, 0x61, 0x6d, 0x65, 0x02, 0x14, 0x02, 0x00, 0x01, 0x00, 0x07, 0x64, 0x65, 0x73, 0x74, 0x50, 0x74, 0x72, 0x01, 0x02, 0x00, 0x01, 0x64, 0x01, 0x01, 0x6e,
    };

    private static void WriteWasmHeader(Stream outputStream)
    {
#if NET7_0_OR_GREATER
        outputStream.Write(s_wasmWrapperPrefix.Span);
#else
        outputStream.Write(s_wasmWrapperPrefix, 0, s_wasmWrapperPrefix.Length);
#endif
    }

    private static void WriteWasmSuffix(Stream outputStream)
    {
#if NET7_0_OR_GREATER
        outputStream.Write(s_wasmWrapperSuffix.Span);
#else
        outputStream.Write(s_wasmWrapperSuffix, 0, s_wasmWrapperSuffix.Length);
#endif
    }

    // 1 byte to encode "passive" data segment
    private const uint SegmentCodeSize = 1;

    // Align the payload start to a 4-byte boundary within the wrapper.  If the runtime reads the
    // payload directly, instead of by instantiatng the wasm module, we don't want the WebAssembly
    // prefix to push some of the values inside the image to odd byte offsets as the runtime assumes
    // the image will be aligned.
    //
    // There are requirements in ECMA-335 (Section II.25.4) that fat method headers and method data
    // sections be 4-byte aligned.
    private const uint WebcilPayloadInternalAlignment = 4;

    private void WriteDataSection(BinaryWriter writer)
    {

        uint dataSectionSize = 0;
        // uleb128 encoding of number of segments
        dataSectionSize += 1; // there's always 2 segments which encodes to 1 byte
        // compute the segment 0 size:
        //   segment 0 has 1 byte segment code, 1 byte of size and at least 4 bytes of payload
        uint segment0MinimumSize = SegmentCodeSize + 1 + 4;
        dataSectionSize += segment0MinimumSize;

        // encode webcil size as a uleb128
        byte[] ulebWebcilPayloadSize = ULEB128Encode(_webcilPayloadSize);

        // compute the segment 1 size:
        //   segment 1 has 1 byte segment code, a uleb128 encoding of the webcilPayloadSize, and the payload
        // don't count the size of the payload yet
        checked
        {
            dataSectionSize += SegmentCodeSize + (uint)ulebWebcilPayloadSize.Length;
        }

        // at this point the data section size includes everything except the data section code, the data section size and the webcil payload itself
        // and any extra padding that we may want to add to segment 0.
        // So we can compute the offset of the payload within the wasm module.
        byte[] putativeULEBDataSectionSize = ULEB128Encode(dataSectionSize + _webcilPayloadSize);
        uint payloadOffset = (uint)s_wasmWrapperPrefix.Length + 1 + (uint)putativeULEBDataSectionSize.Length + dataSectionSize ;

        uint paddingSize = PadTo(payloadOffset, WebcilPayloadInternalAlignment);

        if (paddingSize > 0)
        {
            checked
            {
                dataSectionSize += paddingSize;
            }
        }

        checked
        {
            dataSectionSize += _webcilPayloadSize;
        }

        byte[] ulebSectionSize = ULEB128Encode(dataSectionSize);

        if (putativeULEBDataSectionSize.Length != ulebSectionSize.Length)
            throw new InvalidOperationException  ("adding padding would cause data section's encoded length to chane"); // TODO: fixme: there's upto one extra byte to encode the section length - take away a padding byte.
        writer.Write((byte)11); // section Data
        writer.Write(ulebSectionSize, 0, ulebSectionSize.Length);

        writer.Write((byte)2); // number of segments

        // write segment 0
        writer.Write((byte)1); // passive segment
        if (paddingSize + 4 > 127) {
            throw new InvalidOperationException ("padding would cause segment 0 to need a multi-byte ULEB128 size encoding");
        }
        writer.Write((byte)(4 + paddingSize)); // segment size: 4 plus any padding
        writer.Write((uint)_webcilPayloadSize); // payload is an unsigned 32 bit number
        for (int i = 0; i < paddingSize; i++)
            writer.Write((byte)0);

        // write segment 1
        writer.Write((byte)1); // passive segment
        writer.Write(ulebWebcilPayloadSize, 0, ulebWebcilPayloadSize.Length); // segment size:  _webcilPayloadSize
        if (writer.BaseStream.Position % WebcilPayloadInternalAlignment != 0) {
            throw new Exception ($"predited offset {payloadOffset}, actual position {writer.BaseStream.Position}");
        }
        _webcilPayloadStream.CopyTo(writer.BaseStream); // payload is the entire webcil content
    }

    private static byte[] ULEB128Encode(uint value)
    {
        uint n = value;
        int len = 0;
        do
        {
            n >>= 7;
            len++;
        } while (n != 0);
        byte[] arr = new byte[len];
        int i = 0;
        n = value;
        do
        {
            byte b = (byte)(n & 0x7f);
            n >>= 7;
            if (n != 0)
                b |= 0x80;
            arr[i++] = b;
        } while (n != 0);
        return arr;
    }

    private static uint PadTo (uint value, uint align)
    {
        uint newValue = AlignTo(value, align);
        return newValue - value;
    }

    private static uint AlignTo (uint value, uint align)
    {
        return (value + (align - 1)) & ~(align - 1);
    }
}
