// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Text;

namespace System.Collections.Generic
{
    /// <summary>
    /// ABOUT:
    /// Helps with operations that rely on bit marking to indicate whether an item in the 
    /// collection should be added, removed, visited already, etc. 
    /// 
    /// BitHelper doesn't allocate the array; you must pass in an array or ints allocated on the 
    /// stack or heap. ToIntArrayLength() tells you the int array size you must allocate. 
    /// 
    /// USAGE:
    /// Suppose you need to represent a bit array of length (i.e. logical bit array length)
    /// BIT_ARRAY_LENGTH. Then this is the suggested way to instantiate BitHelper:
    /// ***************************************************************************
    /// int intArrayLength = BitHelper.ToIntArrayLength(BIT_ARRAY_LENGTH);
    /// BitHelper bitHelper;
    /// if (intArrayLength less than stack alloc threshold)
    ///     int* m_arrayPtr = stackalloc int[intArrayLength];
    ///     bitHelper = new BitHelper(m_arrayPtr, intArrayLength);
    /// else
    ///     int[] m_arrayPtr = new int[intArrayLength];
    ///     bitHelper = new BitHelper(m_arrayPtr, intArrayLength);
    /// ***************************************************************************
    /// 
    /// IMPORTANT:
    /// The second ctor args, length, should be specified as the length of the int array, not
    /// the logical bit array. Because length is used for bounds checking into the int array,
    /// it's especially important to get this correct for the stackalloc version. See the code 
    /// samples above; this is the value gotten from ToIntArrayLength(). 
    /// 
    /// The length ctor argument is the only exception; for other methods -- MarkBit and 
    /// IsMarked -- pass in values as indices into the logical bit array, and it will be mapped
    /// to the position within the array of ints.
    /// 
    /// FUTURE OPTIMIZATIONS:
    /// A method such as FindFirstMarked/Unmarked Bit would be useful for callers that operate 
    /// on a bit array and then need to loop over it. In particular, if it avoided visiting 
    /// every bit, it would allow good perf improvements when the bit array is sparse.
    /// </summary>
    unsafe internal sealed class BitHelper
    {   // should not be serialized
        private const byte MarkedBitFlag = 1;
        private const byte IntSize = 32;

        // m_length of underlying int array (not logical bit array)
        private int _length;

        // ptr to stack alloc'd array of ints
        [System.Security.SecurityCritical]
        private int* _arrayPtr;

        // array of ints
        private int[] _array;

        // whether to operate on stack alloc'd or heap alloc'd array 
        private bool _useStackAlloc;

        /// <summary>
        /// Instantiates a BitHelper with a heap alloc'd array of ints
        /// </summary>
        /// <param name="bitArray">int array to hold bits</param>
        /// <param name="length">length of int array</param>
        [System.Security.SecurityCritical]
        internal BitHelper(int* bitArrayPtr, int length)
        {
            _arrayPtr = bitArrayPtr;
            _length = length;
            _useStackAlloc = true;
        }

        /// <summary>
        /// Instantiates a BitHelper with a heap alloc'd array of ints
        /// </summary>
        /// <param name="bitArray">int array to hold bits</param>
        /// <param name="length">length of int array</param>
        internal BitHelper(int[] bitArray, int length)
        {
            _array = bitArray;
            _length = length;
        }

        /// <summary>
        /// Mark bit at specified position
        /// </summary>
        /// <param name="bitPosition"></param>
        [System.Security.SecuritySafeCritical]
        internal unsafe void MarkBit(int bitPosition)
        {
            int bitArrayIndex = bitPosition / IntSize;
            if (bitArrayIndex < _length && bitArrayIndex >= 0)
            {
                if (_useStackAlloc)
                {
                    _arrayPtr[bitArrayIndex] |= (MarkedBitFlag << (bitPosition % IntSize));
                }
                else
                {
                    _array[bitArrayIndex] |= (MarkedBitFlag << (bitPosition % IntSize));
                }
            }
        }

        /// <summary>
        /// Is bit at specified position marked?
        /// </summary>
        /// <param name="bitPosition"></param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]
        internal unsafe bool IsMarked(int bitPosition)
        {
            int bitArrayIndex = bitPosition / IntSize;
            if (bitArrayIndex < _length && bitArrayIndex >= 0)
            {
                if (_useStackAlloc)
                {
                    return ((_arrayPtr[bitArrayIndex] & (MarkedBitFlag << (bitPosition % IntSize))) != 0);
                }
                else
                {
                    return ((_array[bitArrayIndex] & (MarkedBitFlag << (bitPosition % IntSize))) != 0);
                }
            }
            return false;
        }

        /// <summary>
        /// How many ints must be allocated to represent n bits. Returns (n+31)/32, but 
        /// avoids overflow
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        internal static int ToIntArrayLength(int n)
        {
            return n > 0 ? ((n - 1) / IntSize + 1) : 0;
        }
    }
}
