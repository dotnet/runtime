// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata
{
    public readonly struct TypeLayout
    {
        private readonly int _size;
        private readonly int _packingSize;

        public TypeLayout(int size, int packingSize)
        {
            _size = size;
            _packingSize = packingSize;
        }

        public int Size
        {
            get { return _size; }
        }

        public int PackingSize
        {
            get { return _packingSize; }
        }

        public bool IsDefault
        {
            get { return _size == 0 && _packingSize == 0; }
        }
    }
}
