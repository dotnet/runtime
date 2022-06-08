// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

#nullable enable

namespace ILCompiler.Dataflow
{
    public class ValueNodeList : List<MultiValue>
    {
        public ValueNodeList()
        {
        }

        public ValueNodeList(int capacity)
            : base(capacity)
        {
        }

        public ValueNodeList(List<MultiValue> other)
            : base(other)
        {
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new HashCode();
            foreach (var item in this)
                hashCode.Add(item.GetHashCode());
            return hashCode.ToHashCode();
        }

        public override bool Equals(object? other)
        {
            if (!(other is ValueNodeList otherList))
                return false;

            if (otherList.Count != Count)
                return false;

            for (int i = 0; i < Count; i++)
            {
                if (!otherList[i].Equals(this[i]))
                    return false;
            }
            return true;
        }
    }

    public struct ValueBasicBlockPair
    {
        public ValueBasicBlockPair(MultiValue value, int basicBlockIndex)
        {
            Value = value;
            BasicBlockIndex = basicBlockIndex;
        }

        public MultiValue Value { get; }
        public int BasicBlockIndex { get; }
    }
}
