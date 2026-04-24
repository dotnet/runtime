// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

using Internal.TypeSystem;

namespace ILCompiler
{
    public partial class CompilerTypeSystemContext
    {
        private volatile TypeDesc[] _valueTupleStructsBySize = Array.Empty<TypeDesc>();
        private volatile TypeDesc _cachedEmptyStruct;

        /// <summary>
        /// Gets the first empty struct type encountered during lowering, or null if none has been seen.
        /// Used by RaiseSignature to produce a roundtrippable type for the 'e' encoding.
        /// </summary>
        public TypeDesc CachedEmptyStruct => _cachedEmptyStruct;

        /// <summary>
        /// Caches an empty struct type discovered during lowering. Only the first one is retained.
        /// </summary>
        public void CacheEmptyStruct(TypeDesc type)
        {
            _cachedEmptyStruct ??= type;
        }

        /// <summary>
        /// Gets or creates a value type of the specified byte size, constructed from
        /// nested ValueTuple&lt;byte, ...&gt; types. Used by WasmLowering to represent
        /// struct parameters/returns in raised signatures.
        /// </summary>
        /// <remarks>
        /// Size 1 returns <c>byte</c>.
        /// Size 2 returns <c>ValueTuple&lt;byte, byte&gt;</c>.
        /// Size 5 returns <c>ValueTuple&lt;ValueTuple&lt;byte, byte&gt;, ValueTuple&lt;byte, ValueTuple&lt;byte, byte&gt;&gt;&gt;</c>.
        /// Size N is split into halves: <c>ValueTuple&lt;(size N/2), (size N - N/2)&gt;</c>.
        /// </remarks>
        public TypeDesc GetValueTupleStructOfSize(int size)
        {
            TypeDesc[] array = _valueTupleStructsBySize;

            if (size < array.Length && array[size] is not null)
            {
                return array[size];
            }

            return GetValueTupleStructOfSizeSlow(size);
        }

        private TypeDesc GetValueTupleStructOfSizeSlow(int size)
        {
            TypeDesc[] array = _valueTupleStructsBySize;

            if (size >= array.Length)
            {
                TypeDesc[] newArray = new TypeDesc[size + 1];
                Array.Copy(array, newArray, array.Length);
                _valueTupleStructsBySize = newArray;
                array = newArray;
            }

            TypeDesc result = BuildValueTupleStructOfSize(size);
            array[size] = result;

            return result;
        }

        private TypeDesc BuildValueTupleStructOfSize(int size)
        {
            TypeDesc byteType = GetWellKnownType(WellKnownType.Byte);

            if (size == 1)
            {
                return byteType;
            }

            MetadataType valueTuple2 = SystemModule.GetType("System"u8, "ValueTuple`2"u8);
            int leftSize = size / 2;
            int rightSize = size - leftSize;
            TypeDesc left = GetValueTupleStructOfSize(leftSize);
            TypeDesc right = GetValueTupleStructOfSize(rightSize);

            return valueTuple2.MakeInstantiatedType(left, right);
        }
    }
}
