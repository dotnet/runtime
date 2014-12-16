// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata
{
    public struct TypeLayout
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
