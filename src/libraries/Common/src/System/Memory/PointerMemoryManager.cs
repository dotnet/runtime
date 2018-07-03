// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers
{
    internal unsafe class PointerMemoryManager<T> : MemoryManager<T> where T : struct
    {
        private readonly void* _pointer;
        private readonly int _length;

        internal PointerMemoryManager(void* pointer, int length)
        {
            _pointer = pointer;
            _length = length;
        }

        protected override void Dispose(bool disposing)
        {
        }

        public override Span<T> GetSpan()
        {
            return new Span<T>(_pointer, _length);
        }

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            throw new NotSupportedException();
        }

        public override void Unpin()
        {
        }
    }
}
