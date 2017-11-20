// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            public unsafe void Reduce(ref BitsBuffer modulus)
            {
                // Executes a modulo operation using the divide operation.
                // Thus, no need of any switching here, happens in-line.

                if (_length >= modulus._length)
                {
                    fixed (uint* b = _bits, m = modulus._bits)
                    {
                        Divide(b, _length,
                               m, modulus._length,
                               null, 0);
                    }

                    _length = ActualLength(_bits, modulus._length);
                }
            }

            public void Overwrite(ulong value)
            {
                Debug.Assert(_bits.Length >= 2);

                if (_length > 2)
                {
                    // Ensure leading zeros
                    Array.Clear(_bits, 2, _length - 2);
                }

                uint lo = unchecked((uint)value);
                uint hi = (uint)(value >> 32);

                _bits[0] = lo;
                _bits[1] = hi;
                _length = hi != 0 ? 2 : lo != 0 ? 1 : 0;
            }

            public void Overwrite(uint value)
            {
                Debug.Assert(_bits.Length >= 1);

                if (_length > 1)
                {
                    // Ensure leading zeros
                    Array.Clear(_bits, 1, _length - 1);
                }

                _bits[0] = value;
                _length = value != 0 ? 1 : 0;
            }

            public uint[] GetBits()
            {
                return _bits;
            }

            public int GetSize()
            {
                return _bits.Length;
            }

            public int GetLength()
            {
                return _length;
            }

            public void Refresh(int maxLength)
            {
                Debug.Assert(_bits.Length >= maxLength);

                if (_length > maxLength)
                {
                    // Ensure leading zeros
                    Array.Clear(_bits, maxLength, _length - maxLength);
                }

                _length = ActualLength(_bits, maxLength);
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
