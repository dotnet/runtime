// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ---------------------------------------------------------------------------
// Generic functions to compute the hashcode value of types
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Internal.NativeFormat
{
#if SYSTEM_PRIVATE_CORELIB
    [CLSCompliant(false)]
#endif
    public static class TypeHashingAlgorithms
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int _rotl(int value, int shift)
        {
            return (int)(((uint)value << shift) | ((uint)value >> (32 - shift)));
        }

        //
        // Returns the hashcode value of the 'src' string
        //
        public static int ComputeNameHashCode(string src)
        {
            int hash1 = 0x6DA3B944;
            int hash2 = 0;

            for (int i = 0; i < src.Length; i += 2)
            {
                hash1 = (hash1 + _rotl(hash1, 5)) ^ src[i];
                if ((i + 1) < src.Length)
                    hash2 = (hash2 + _rotl(hash2, 5)) ^ src[i + 1];
            }

            hash1 += _rotl(hash1, 8);
            hash2 += _rotl(hash2, 8);

            return hash1 ^ hash2;
        }

        public static unsafe int ComputeASCIINameHashCode(byte* data, int length, out bool isAscii)
        {
            int hash1 = 0x6DA3B944;
            int hash2 = 0;
            int asciiMask = 0;

            for (int i = 0; i < length; i += 2)
            {
                int b1 = data[i];
                asciiMask |= b1;
                hash1 = (hash1 + _rotl(hash1, 5)) ^ b1;
                if ((i + 1) < length)
                {
                    int b2 = data[i];
                    asciiMask |= b2;
                    hash2 = (hash2 + _rotl(hash2, 5)) ^ b2;
                }
            }

            hash1 += _rotl(hash1, 8);
            hash2 += _rotl(hash2, 8);

            isAscii = (asciiMask & 0x80) == 0;

            return hash1 ^ hash2;
        }

        public static int ComputeMethodSignatureHashCode<ARG>(int returnTypeHashCode, ARG[] parameters)
        {
            // We're not taking calling conventions into consideration here mostly because there's no
            // exchange enum type that would define them. We could define one, but the amount of additional
            // information it would bring (16 or so possibilities) is likely not worth it.
            int hashcode = returnTypeHashCode;
            for (int i = 0; i < parameters.Length; i++)
            {
                int parameterHashCode = parameters[i].GetHashCode();
                hashcode = (hashcode + _rotl(hashcode, 13)) ^ parameterHashCode;
            }
            return (hashcode + _rotl(hashcode, 15));
        }

        /// <summary>
        /// Produce a hashcode for a generic signature variable
        /// </summary>
        /// <param name="index">zero based index</param>
        /// <param name="method">true if the signature variable describes a method</param>
        public static int ComputeSignatureVariableHashCode(int index, bool method)
        {
            if (method)
            {
                return index * 0x7822381 + 0x54872645;
            }
            else
            {
                return index * 0x5498341 + 0x832424;
            }
        }
    }
}
