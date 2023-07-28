// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler
{
    internal interface ISortableDataStructureAccessor<T, TDataStructure>
    {
        T GetElement(TDataStructure dataStructure, int i);
        void SetElement(TDataStructure dataStructure, int i, T value);
        void SwapElements(TDataStructure dataStructure, int i, int i2);
        void Copy(TDataStructure source, int sourceIndex, T[] target, int destIndex, int length);
        void Copy(T[] source, int sourceIndex, TDataStructure target, int destIndex, int length);
        int GetLength(TDataStructure dataStructure);
    }
}
