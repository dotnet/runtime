// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Nrbf.Utils;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Formats.Nrbf
{
    internal sealed class ArrayRectangularPrimitiveRecord<T> : ArrayRecord where T : unmanaged
    {
        private readonly int[] _lengths;
        private readonly IReadOnlyList<T> _values;
        private TypeName? _typeName;

        internal ArrayRectangularPrimitiveRecord(ArrayInfo arrayInfo, int[] lengths, IReadOnlyList<T> values) : base(arrayInfo)
        {
            _lengths = lengths;
            _values = values;
            ValuesToRead = 0; // there is nothing to read anymore
        }

        public override ReadOnlySpan<int> Lengths => _lengths;

        public override SerializationRecordType RecordType => SerializationRecordType.BinaryArray;

        public override TypeName TypeName
            => _typeName ??= TypeNameHelpers.GetPrimitiveTypeName(TypeNameHelpers.GetPrimitiveType<T>()).MakeArrayTypeName(Rank);

        internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetAllowedRecordType() => throw new InvalidOperationException();

        private protected override void AddValue(object value) => throw new InvalidOperationException();

        [RequiresDynamicCode("May call Array.CreateInstance().")]
        private protected override Array Deserialize(Type arrayType, bool allowNulls)
        {
            Array result =
#if NET9_0_OR_GREATER
                Array.CreateInstanceFromArrayType(arrayType, _lengths);
#else
                Array.CreateInstance(typeof(T), _lengths);
#endif
            int[] indices = new int[_lengths.Length];
            nuint numElementsWritten = 0; // only for debugging; not used in release builds

            for (int i = 0; i < _values.Count; i++)
            {
                result.SetValue(_values[i], indices);
                numElementsWritten++;

                int dimension = indices.Length - 1;
                while (dimension >= 0)
                {
                    indices[dimension]++;
                    if (indices[dimension] < Lengths[dimension])
                    {
                        break;
                    }
                    indices[dimension] = 0;
                    dimension--;
                }

                if (dimension < 0)
                {
                    break;
                }
            }

            Debug.Assert(numElementsWritten == (uint)_values.Count, "We should have traversed the entirety of the source values collection.");
            Debug.Assert(numElementsWritten == (ulong)result.LongLength, "We should have traversed the entirety of the destination array.");

            return result;
        }
    }
}
