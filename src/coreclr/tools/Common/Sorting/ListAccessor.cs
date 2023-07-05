// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace ILCompiler.Sorting.Implementation
{
    internal struct ListAccessor<T> : ISortableDataStructureAccessor<T, List<T>>
    {
        public void Copy(List<T> source, int sourceIndex, T[] target, int destIndex, int length)
        {
            source.CopyTo(sourceIndex, target, destIndex, length);
        }

        public void Copy(T[] source, int sourceIndex, List<T> target, int destIndex, int length)
        {
            for (int i = 0; i < length; i++)
            {
                target[i + destIndex] = source[i + sourceIndex];
            }
        }

        public T GetElement(List<T> dataStructure, int i)
        {
            return dataStructure[i];
        }

        public int GetLength(List<T> dataStructure)
        {
            return dataStructure.Count;
        }

        public void SetElement(List<T> dataStructure, int i, T value)
        {
            dataStructure[i] = value;
        }

        public void SwapElements(List<T> dataStructure, int i, int i2)
        {
            T temp = dataStructure[i];
            dataStructure[i] = dataStructure[i2];
            dataStructure[i2] = temp;
        }
    }
}
