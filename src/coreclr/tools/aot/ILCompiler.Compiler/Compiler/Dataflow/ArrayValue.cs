// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using ILCompiler.Dataflow;
using ILLink.Shared.DataFlow;
using Internal.TypeSystem;

using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    internal partial record ArrayValue
    {
        public static MultiValue Create(MultiValue size, TypeDesc elementType)
        {
            MultiValue result = MultiValueLattice.Top;
            foreach (var sizeValue in size)
            {
                result = MultiValueLattice.Meet(result, new MultiValue(new ArrayValue(sizeValue, elementType)));
            }

            return result;
        }

        public static ArrayValue Create(int size, TypeDesc elementType)
        {
            return new ArrayValue(new ConstIntValue(size), elementType);
        }

        /// <summary>
        /// Constructs an array value of the given size
        /// </summary>
        private ArrayValue(SingleValue size, TypeDesc elementType)
        {
            Size = size;
            ElementType = elementType;
            IndexValues = new Dictionary<int, ValueBasicBlockPair>();
        }

        public TypeDesc ElementType { get; }
        public Dictionary<int, ValueBasicBlockPair> IndexValues { get; }

        public partial bool TryGetValueByIndex(int index, out MultiValue value)
        {
            if (IndexValues.TryGetValue(index, out var valuePair))
            {
                value = valuePair.Value;
                return true;
            }

            value = default;
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GetType().GetHashCode(), Size);
        }

        public bool Equals(ArrayValue? otherArr)
        {
            if (otherArr == null)
                return false;

            bool equals = Size.Equals(otherArr.Size);
            equals &= IndexValues.Count == otherArr.IndexValues.Count;
            if (!equals)
                return false;

            // If both sets T and O are the same size and "T intersect O" is empty, then T == O.
            HashSet<KeyValuePair<int, ValueBasicBlockPair>> thisValueSet = new(IndexValues);
            HashSet<KeyValuePair<int, ValueBasicBlockPair>> otherValueSet = new(otherArr.IndexValues);
            thisValueSet.ExceptWith(otherValueSet);
            return thisValueSet.Count == 0;
        }

        public override SingleValue DeepCopy()
        {
            var newValue = new ArrayValue(Size.DeepCopy(), ElementType);
            foreach (var kvp in IndexValues)
            {
                newValue.IndexValues.Add(kvp.Key, new ValueBasicBlockPair(kvp.Value.Value.Clone(), kvp.Value.BasicBlockIndex));
            }

            return newValue;
        }

        public override string ToString()
        {
            StringBuilder result = new();
            result.Append("Array Size:");
            result.Append(this.ValueToString(Size));

            result.Append(", Values:(");
            bool first = true;
            foreach (var element in IndexValues)
            {
                if (!first)
                {
                    result.Append(',');
                    first = false;
                }

                result.Append('(');
                result.Append(element.Key);
                result.Append(",(");
                bool firstValue = true;
                foreach (var v in element.Value.Value)
                {
                    if (firstValue)
                    {
                        result.Append(',');
                        firstValue = false;
                    }

                    result.Append(v.ToString());
                }
                result.Append("))");
            }
            result.Append(')');

            return result.ToString();
        }
    }
}
