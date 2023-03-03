// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ---------------------------------------------------------------------------
// Generic functions to compute the hashcode value of types
// ---------------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Internal.NativeFormat
{
    public static class TypeHashingAlgorithms
    {
        public struct HashCodeBuilder
        {
            private int _hash1;
            private int _hash2;
            private int _numCharactersHashed;

            public HashCodeBuilder(string seed)
            {
                _hash1 = 0x6DA3B944;
                _hash2 = 0;
                _numCharactersHashed = 0;

                Append(seed);
            }

            public void Append(string src)
            {
                if (src.Length == 0)
                    return;

                int startIndex = 0;
                if ((_numCharactersHashed & 1) == 1)
                {
                    _hash2 = (_hash2 + _rotl(_hash2, 5)) ^ src[0];
                    startIndex = 1;
                }

                for (int i = startIndex; i < src.Length; i += 2)
                {
                    _hash1 = (_hash1 + _rotl(_hash1, 5)) ^ src[i];
                    if ((i + 1) < src.Length)
                        _hash2 = (_hash2 + _rotl(_hash2, 5)) ^ src[i + 1];
                }

                _numCharactersHashed += src.Length;
            }

            public int ToHashCode()
            {
                int hash1 = _hash1 + _rotl(_hash1, 8);
                int hash2 = _hash2 + _rotl(_hash2, 8);

                return hash1 ^ hash2;
            }
        }

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

        // This function may be needed in a portion of the codebase which is too low level to use
        // globalization, ergo, we cannot call ToString on the integer.
        private static string IntToString(int arg)
        {
            // This IntToString function is only expected to be used for MDArrayRanks, and therefore is only for positive numbers
            Debug.Assert(arg > 0);
            StringBuilder sb = new StringBuilder(1);

            while (arg != 0)
            {
                sb.Append((char)('0' + (arg % 10)));
                arg /= 10;
            }

            // Reverse the string
            int sbLen = sb.Length;
            int pivot = sbLen / 2;
            for (int i = 0; i < pivot; i++)
            {
                int iToSwapWith = sbLen - i - 1;
                char temp = sb[i];
                sb[i] = sb[iToSwapWith];
                sb[iToSwapWith] = temp;
            }

            return sb.ToString();
        }

        public static int ComputeArrayTypeHashCode(int elementTypeHashCode, int rank)
        {
            // Arrays are treated as generic types in some parts of our system. The array hashcodes are
            // carefully crafted to be the same as the hashcodes of their implementation generic types.

            int hashCode;
            if (rank == -1)
            {
                hashCode = unchecked((int)0xd5313557u);
                Debug.Assert(hashCode == ComputeNameHashCode("System.Array`1"));
            }
            else
            {
                hashCode = ComputeNameHashCode("System.MDArrayRank" + IntToString(rank) + "`1");
            }

            hashCode = (hashCode + _rotl(hashCode, 13)) ^ elementTypeHashCode;
            return (hashCode + _rotl(hashCode, 15));
        }

        public static int ComputeArrayTypeHashCode<T>(T elementType, int rank)
        {
            return ComputeArrayTypeHashCode(elementType.GetHashCode(), rank);
        }


        public static int ComputePointerTypeHashCode(int pointeeTypeHashCode)
        {
            return (pointeeTypeHashCode + _rotl(pointeeTypeHashCode, 5)) ^ 0x12D0;
        }

        public static int ComputePointerTypeHashCode<T>(T pointeeType)
        {
            return ComputePointerTypeHashCode(pointeeType.GetHashCode());
        }


        public static int ComputeByrefTypeHashCode(int parameterTypeHashCode)
        {
            return (parameterTypeHashCode + _rotl(parameterTypeHashCode, 7)) ^ 0x4C85;
        }

        public static int ComputeByrefTypeHashCode<T>(T parameterType)
        {
            return ComputeByrefTypeHashCode(parameterType.GetHashCode());
        }


        public static int ComputeNestedTypeHashCode(int enclosingTypeHashCode, int nestedTypeNameHash)
        {
            return (enclosingTypeHashCode + _rotl(enclosingTypeHashCode, 11)) ^ nestedTypeNameHash;
        }


        public static int ComputeGenericInstanceHashCode<ARG>(int genericDefinitionHashCode, ARG[] genericTypeArguments)
        {
            int hashcode = genericDefinitionHashCode;
            for (int i = 0; i < genericTypeArguments.Length; i++)
            {
                int argumentHashCode = genericTypeArguments[i].GetHashCode();
                hashcode = (hashcode + _rotl(hashcode, 13)) ^ argumentHashCode;
            }
            return (hashcode + _rotl(hashcode, 15));
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
        /// Produce a hashcode for a specific method
        /// </summary>
        /// <param name="typeHashCode">HashCode of the type that owns the method</param>
        /// <param name="nameOrNameAndGenericArgumentsHashCode">HashCode of either the name of the method (for non-generic methods) or the GenericInstanceHashCode of the name+generic arguments of the method.</param>
        /// <returns></returns>
        public static int ComputeMethodHashCode(int typeHashCode, int nameOrNameAndGenericArgumentsHashCode)
        {
            // TODO! This hash combining function isn't good, but it matches logic used in the past
            // consider changing to a better combining function once all uses use this function
            return typeHashCode ^ nameOrNameAndGenericArgumentsHashCode;
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
