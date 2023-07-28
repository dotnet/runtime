// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

using Debug = System.Diagnostics.Debug;

namespace Internal.Runtime
{
    /// <summary>
    /// Provides functionality to encode/decode dehydrated data instruction stream.
    /// </summary>
    /// <remarks>
    /// The instructions use a variable length encoding and are split in two parts:
    /// the instruction command kind and command data (payload).
    /// The payload is an integer. If the instruction kind and payload can fit into a single
    /// byte, the encoding is one byte. Bigger payloads produce bigger instructions.
    /// </remarks>
    internal static class DehydratedDataCommand
    {
        public const byte Copy = 0x00;
        public const byte ZeroFill = 0x01;
        public const byte RelPtr32Reloc = 0x02;
        public const byte PtrReloc = 0x03;
        public const byte InlineRelPtr32Reloc = 0x04;
        public const byte InlinePtrReloc = 0x05;

        private const byte DehydratedDataCommandMask = 0x07;
        private const int DehydratedDataCommandPayloadShift = 3;

        private const int MaxRawShortPayload = (1 << (8 - DehydratedDataCommandPayloadShift)) - 1;
        private const int MaxExtraPayloadBytes = 3;
        public const int MaxShortPayload = MaxRawShortPayload - MaxExtraPayloadBytes;

        public static byte EncodeShort(int command, int commandData)
        {
            Debug.Assert((command & DehydratedDataCommandMask) == command);
            Debug.Assert(commandData <= MaxShortPayload);
            return (byte)(command | (commandData << DehydratedDataCommandPayloadShift));
        }

        public static int Encode(int command, int commandData, byte[] buffer)
        {
            Debug.Assert((command & DehydratedDataCommandMask) == command);
            int remainingData = commandData - MaxShortPayload;
            if (remainingData <= 0)
            {
                buffer[0] = EncodeShort(command, commandData);
                return 1;
            }

            int numExtraBytes = 0;
            for (; remainingData != 0; remainingData >>= 8)
                buffer[++numExtraBytes] = (byte)remainingData;
            if (numExtraBytes > MaxExtraPayloadBytes)
                throw new InvalidOperationException(); // decoder can only decode this many extra bytes

            buffer[0] = (byte)(command | ((MaxShortPayload + numExtraBytes) << DehydratedDataCommandPayloadShift));
            return 1 + numExtraBytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe byte* Decode(byte* pB, out int command, out int payload)
        {
            byte b = *pB;
            command = b & DehydratedDataCommandMask;
            payload = b >> DehydratedDataCommandPayloadShift;
            int extraBytes = payload - MaxShortPayload;
            if (extraBytes > 0)
            {
                payload = *++pB;
                if (extraBytes > 1)
                {
                    payload += *++pB << 8;
                    if (extraBytes > 2)
                        payload += *++pB << 16;
                }

                payload += MaxShortPayload;
            }

            return pB + 1;
        }
    }
}
