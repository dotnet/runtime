// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILCompiler.Sorting.Implementation
{
    internal struct ArrayAccessor<T> : ISortableDataStructureAccessor<T, T[]>
    {
        public void Copy(T[] source, int sourceIndex, T[] target, int destIndex, int length)
        {
            Array.Copy(source, sourceIndex, target, destIndex, length);
        }

        public T GetElement(T[] dataStructure, int i)
        {
            return dataStructure[i];
        }

        public int GetLength(T[] dataStructure)
        {
            return dataStructure.Length;
        }

        public void SetElement(T[] dataStructure, int i, T value)
        {
            dataStructure[i] = value;
        }

        public void SwapElements(T[] dataStructure, int i, int i2)
        {
            T temp = dataStructure[i];
            dataStructure[i] = dataStructure[i2];
            dataStructure[i2] = temp;
        }
    }
}
