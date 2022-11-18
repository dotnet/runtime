// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

        private const byte DehydratedDataCommandMask = 0x03;
        private const int DehydratedDataCommandPayloadShift = 2;

        private const int MaxRawShortPayload = (1 << (8 - DehydratedDataCommandPayloadShift)) - 1;
        private const int MaxExtraPayloadBytes = 3;
        private const int MaxShortPayload = MaxRawShortPayload - MaxExtraPayloadBytes;

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

#if false
        static void Main()
        {
            int command, payload;

            byte[] buf = new byte[5];
            Debug.Assert(Encode(1, 0, buf) == 1);
            Debug.Assert(buf[0] == 1);
            Debug.Assert(D(buf, out command, out payload) == 1 && command == 1 && payload == 0);
            Debug.Assert(Encode(1, 1, buf) == 1);
            Debug.Assert(buf[0] == (1 | (1 << DehydratedDataCommandPayloadShift)));
            Debug.Assert(D(buf, out command, out payload) == 1 && command == 1 && payload == 1);
            Debug.Assert(Encode(1, 60, buf) == 1);
            Debug.Assert(buf[0] == (1 | (60 << DehydratedDataCommandPayloadShift)));
            Debug.Assert(D(buf, out command, out payload) == 1 && command == 1 && payload == 60);
            Debug.Assert(Encode(1, 61, buf) == 2);
            Debug.Assert(buf[0] == (1 | ((MaxShortPayload + 1) << DehydratedDataCommandPayloadShift)));
            Debug.Assert(buf[1] == 1);
            Debug.Assert(D(buf, out command, out payload) == 2 && command == 1 && payload == 61);

            Debug.Assert(Encode(3, 256, buf) == 2);
            Debug.Assert(D(buf, out command, out payload) == 2 && command == 3 && payload == 256);
            Debug.Assert(Encode(3, 6500, buf) == 3);
            Debug.Assert(D(buf, out command, out payload) == 3 && command == 3 && payload == 6500);
            Debug.Assert(Encode(3, 65000, buf) == 3);
            Debug.Assert(D(buf, out command, out payload) == 3 && command == 3 && payload == 65000);
            Debug.Assert(Encode(3, 100000, buf) == 4);
            Debug.Assert(D(buf, out command, out payload) == 4 && command == 3 && payload == 100000);

            static unsafe int D(byte[] bytes, out int command, out int payload)
            {
                fixed (byte* pBytes = bytes)
                    return (int)(Decode(pBytes, out command, out payload) - pBytes);
            }
        }
#endif
    }
}
