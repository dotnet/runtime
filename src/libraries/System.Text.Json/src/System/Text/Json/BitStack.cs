// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal struct BitStack
    {
        // We are using a ulong to represent our nested state, so we can only
        // go 64 levels deep without having to allocate.
        private const int AllocationFreeMaxDepth = sizeof(ulong) * 8;

        private const int DefaultInitialArraySize = 2;

        // The backing array for the stack used when the depth exceeds AllocationFreeMaxDepth.
        private int[]? _array;

        // This ulong container represents a tiny stack to track the state during nested transitions.
        // The first bit represents the state of the current depth (1 == object, 0 == array).
        // Each subsequent bit is the parent / containing type (object or array). Since this
        // reader does a linear scan, we only need to keep a single path as we go through the data.
        // This is primarily used as an optimization to avoid having to allocate an object for
        // depths up to 64 (which is the default max depth).
        private ulong _allocationFreeContainer;

        private int _currentDepth;

        /// <summary>
        /// Gets the number of elements in the stack.
        /// </summary>
        public readonly int CurrentDepth => _currentDepth;

        /// <summary>
        /// Pushes <see langword="true"/> onto the stack.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushTrue()
        {
            if (_currentDepth < AllocationFreeMaxDepth)
            {
                _allocationFreeContainer = (_allocationFreeContainer << 1) | 1;
            }
            else
            {
                PushToArray(true);
            }
            _currentDepth++;
        }

        /// <summary>
        /// Pushes <see langword="false"/> onto the stack.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushFalse()
        {
            if (_currentDepth < AllocationFreeMaxDepth)
            {
                _allocationFreeContainer <<= 1;
            }
            else
            {
                PushToArray(false);
            }
            _currentDepth++;
        }

        /// <summary>
        /// Pushes a bit onto the stack. Allocate the bit array lazily only when it is absolutely necessary.
        /// </summary>
        /// <param name="value">The bit to push onto the stack.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PushToArray(bool value)
        {
            _array ??= new int[DefaultInitialArraySize];

            int index = _currentDepth - AllocationFreeMaxDepth;

            Debug.Assert(index >= 0, $"Set - Negative - index: {index}, arrayLength: {_array.Length}");

            // Maximum possible array length if bitLength was int.MaxValue (i.e. 67_108_864)
            Debug.Assert(_array.Length <= int.MaxValue / 32 + 1, $"index: {index}, arrayLength: {_array.Length}");

            int elementIndex = Div32Rem(index, out int extraBits);

            // Grow the array when setting a bit if it isn't big enough
            // This way the caller doesn't have to check.
            if (elementIndex >= _array.Length)
            {
                // This multiplication can overflow, so cast to uint first.
                Debug.Assert(index >= 0 && index > (int)((uint)_array.Length * 32 - 1), $"Only grow when necessary - index: {index}, arrayLength: {_array.Length}");
                DoubleArray(elementIndex);
            }

            Debug.Assert(elementIndex < _array.Length, $"Set - index: {index}, elementIndex: {elementIndex}, arrayLength: {_array.Length}, extraBits: {extraBits}");

            int newValue = _array[elementIndex];
            if (value)
            {
                newValue |= 1 << extraBits;
            }
            else
            {
                newValue &= ~(1 << extraBits);
            }
            _array[elementIndex] = newValue;
        }

        /// <summary>
        /// Pops the bit at the top of the stack and returns its value.
        /// </summary>
        /// <returns>The bit that was popped.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Pop()
        {
            _currentDepth--;
            bool inObject;
            if (_currentDepth < AllocationFreeMaxDepth)
            {
                _allocationFreeContainer >>= 1;
                inObject = (_allocationFreeContainer & 1) != 0;
            }
            else if (_currentDepth == AllocationFreeMaxDepth)
            {
                inObject = (_allocationFreeContainer & 1) != 0;
            }
            else
            {
                // Decrementing depth above effectively pops the last element in the array-backed case.
                inObject = PeekInArray();
            }
            return inObject;
        }

        /// <summary>
        /// If the stack has a backing array allocated, this method will find the topmost bit in the array and return its value.
        /// This should only be called if the depth is greater than AllocationFreeMaxDepth and an array has been allocated.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly bool PeekInArray()
        {
            int index = _currentDepth - AllocationFreeMaxDepth - 1;
            Debug.Assert(_array != null);
            Debug.Assert(index >= 0, $"Get - Negative - index: {index}, arrayLength: {_array.Length}");

            int elementIndex = Div32Rem(index, out int extraBits);

            Debug.Assert(elementIndex < _array.Length, $"Get - index: {index}, elementIndex: {elementIndex}, arrayLength: {_array.Length}, extraBits: {extraBits}");

            return (_array[elementIndex] & (1 << extraBits)) != 0;
        }

        /// <summary>
        /// Peeks at the bit at the top of the stack.
        /// </summary>
        /// <returns>The bit at the top of the stack.</returns>
        public readonly bool Peek()
            // If the stack is small enough, we can use the allocation-free container, otherwise check the allocated array.
            => _currentDepth <= AllocationFreeMaxDepth ? (_allocationFreeContainer & 1) != 0 : PeekInArray();

        private void DoubleArray(int minSize)
        {
            Debug.Assert(_array != null);
            Debug.Assert(_array.Length < int.MaxValue / 2, $"Array too large - arrayLength: {_array.Length}");
            Debug.Assert(minSize >= 0 && minSize >= _array.Length);

            int nextDouble = Math.Max(minSize + 1, _array.Length * 2);
            Debug.Assert(nextDouble > minSize);

            Array.Resize(ref _array, nextDouble);
        }

        /// <summary>
        /// Optimization to push <see langword="true"/> as the first bit when the stack is empty.
        /// </summary>
        public void SetFirstBit()
        {
            Debug.Assert(_currentDepth == 0, "Only call SetFirstBit when depth is 0");
            _currentDepth++;
            _allocationFreeContainer = 1;
        }

        /// <summary>
        /// Optimization to push <see langword="false"/> as the first bit when the stack is empty.
        /// </summary>
        public void ResetFirstBit()
        {
            Debug.Assert(_currentDepth == 0, "Only call ResetFirstBit when depth is 0");
            _currentDepth++;
            _allocationFreeContainer = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Div32Rem(int number, out int remainder)
        {
            uint quotient = (uint)number / 32;
            remainder = number & (32 - 1);   // equivalent to number % 32, since 32 is a power of 2
            return (int)quotient;
        }
    }
}
