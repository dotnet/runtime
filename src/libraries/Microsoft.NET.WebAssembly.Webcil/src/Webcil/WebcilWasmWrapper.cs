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

    private void WriteDataSection(BinaryWriter writer)
    {
        uint dataSectionSize = 0;
        // uleb128 encoding of number of segments
        dataSectionSize += 1; // there's always 2 segments which encodes to 1 byte
        // compute the segment 0 size:
        //   segment 0 has 1 byte segment code, 1 byte of size and 4 bytes of payload
        dataSectionSize += SegmentCodeSize + 1 + 4;

        // encode webcil size as a uleb128
        byte[] ulebSegmentSize = ULEB128Encode(_webcilPayloadSize);

        // compute the segment 1 size:
        //   segment 1 has 1 byte segment code, a uleb128 encoding of the webcilPayloadSize, and the payload
        checked
        {
            dataSectionSize += SegmentCodeSize + (uint)ulebSegmentSize.Length + _webcilPayloadSize;
        }

        byte[] ulebSectionSize = ULEB128Encode(dataSectionSize);

        writer.Write((byte)11); // section Data
        writer.Write(ulebSectionSize, 0, ulebSectionSize.Length);

        writer.Write((byte)2); // number of segments

        // write segment 0
        writer.Write((byte)1); // passive segment
        writer.Write((byte)4); // segment size: 4
        writer.Write((uint)_webcilPayloadSize); // payload is an unsigned 32 bit number

        // write segment 1
        writer.Write((byte)1); // passive segment
        writer.Write(ulebSegmentSize, 0, ulebSegmentSize.Length); // segment size:  _webcilPayloadSize
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
}
