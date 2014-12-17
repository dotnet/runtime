// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata
{
    public struct TypeLayout
    {
        private readonly int size;
        private readonly int packingSize;

        public TypeLayout(int size, int packingSize)
        {
            this.size = size;
            this.packingSize = packingSize;
        }

        public int Size
        {
            get { return size; }
        }

        public int PackingSize
        {
            get { return packingSize; }
        }

        public bool IsDefault
        {
            get { return size == 0 && packingSize == 0; }
        }
    }
}
