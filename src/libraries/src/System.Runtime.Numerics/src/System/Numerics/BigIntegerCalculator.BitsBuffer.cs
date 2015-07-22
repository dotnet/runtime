// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Security;

namespace System.Numerics
{
    // ATTENTION: always pass BitsBuffer by reference,
    // it's a structure for performance reasons. Furthermore
    // it's a mutable one, so use it only with care!

    internal static partial class BigIntegerCalculator
    {
        // To spare memory allocations a buffer helps reusing memory!
        // We just create the target array twice and switch between every
        // operation. In order to not compute unnecessarily with all those
        // leading zeros we take care of the current actual length.

        internal struct BitsBuffer
        {
            private uint[] _bits;
            private int _length;

            public BitsBuffer(int size, uint value)
            {
                Debug.Assert(size >= 1);

                _bits = new uint[size];
                _length = value != 0 ? 1 : 0;

                _bits[0] = value;
            }

            public BitsBuffer(int size, uint[] value)
            {
                Debug.Assert(value != null);
                Debug.Assert(size >= ActualLength(value));

                _bits = new uint[size];
                _length = ActualLength(value);

                Array.Copy(value, 0, _bits, 0, _length);
            }

            [SecuritySafeCritical]
            public unsafe void MultiplySelf(ref BitsBuffer value,
                                            ref BitsBuffer temp)
            {
                Debug.Assert(temp._length == 0);
                Debug.Assert(_length + value._length <= temp._bits.Length);

                // Executes a multiplication for this and value, writes the
                // result to temp. Switches this and temp arrays afterwards.

                fixed (uint* b = _bits, v = value._bits, t = temp._bits)
                {
                    if (_length < value._length)
                    {
                        Multiply(v, value._length,
                                 b, _length,
                                 t, _length + value._length);
                    }
                    else
                    {
                        Multiply(b, _length,
                                 v, value._length,
                                 t, _length + value._length);
                    }
                }

                Apply(ref temp, _length + value._length);
            }

            [SecuritySafeCritical]
            public unsafe void SquareSelf(ref BitsBuffer temp)
            {
                Debug.Assert(temp._length == 0);
                Debug.Assert(_length + _length <= temp._bits.Length);

                // Executes a square for this, writes the result to temp.
                // Switches this and temp arrays afterwards.

                fixed (uint* b = _bits, t = temp._bits)
                {
                    Square(b, _length,
                           t, _length + _length);
                }

                Apply(ref temp, _length + _length);
            }

            public void Reduce(ref FastReducer reducer)
            {
                // Executes a modulo operation using an optimized reducer.
                // Thus, no need of any switching here, happens in-line.

                _length = reducer.Reduce(_bits, _length);
            }

            [SecuritySafeCritical]
            public unsafe void Reduce(uint[] modulus)
            {
                Debug.Assert(modulus != null);

                // Executes a modulo operation using the divide operation.
                // Thus, no need of any switching here, happens in-line.

                if (_length >= modulus.Length)
                {
                    fixed (uint* b = _bits, m = modulus)
                    {
                        Divide(b, _length,
                               m, modulus.Length,
                               null, 0);
                    }

                    _length = ActualLength(_bits, modulus.Length);
                }
            }

            public uint[] GetBits()
            {
                return _bits;
            }

            public int GetSize()
            {
                return _bits.Length;
            }

            private void Apply(ref BitsBuffer temp, int maxLength)
            {
                Debug.Assert(temp._length == 0);
                Debug.Assert(maxLength <= temp._bits.Length);

                // Resets this and switches this and temp afterwards.
                // The caller assumed an empty temp, the next will too.

                Array.Clear(_bits, 0, _length);

                uint[] t = temp._bits;
                temp._bits = _bits;
                _bits = t;

                _length = ActualLength(_bits, maxLength);
            }
        }
    }
}
