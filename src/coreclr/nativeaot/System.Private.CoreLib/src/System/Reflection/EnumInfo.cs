// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    [ReflectionBlocked]
    public abstract class EnumInfo
    {
        private protected EnumInfo(Type underlyingType, string[] names, bool isFlags)
        {
            UnderlyingType = underlyingType;
            Names = names;
            HasFlagsAttribute = isFlags;
        }

        internal Type UnderlyingType { get; }
        internal string[] Names { get; }
        internal bool HasFlagsAttribute { get; }
    }

    [ReflectionBlocked]
    public sealed class EnumInfo<TUnderlyingValue> : EnumInfo
        where TUnderlyingValue : struct, INumber<TUnderlyingValue>
    {
        public EnumInfo(Type underlyingType, TUnderlyingValue[] values, string[] names, bool isFlags) :
            base(underlyingType, names, isFlags)
        {
            Debug.Assert(values.Length == names.Length);
            Debug.Assert(Enum.AreSorted(values));

            Values = values;
            ValuesAreSequentialFromZero = Enum.AreSequentialFromZero(values);
        }

        internal TUnderlyingValue[] Values { get; }
        internal bool ValuesAreSequentialFromZero { get; }

        public TUnderlyingValue[] CloneValues() =>
            new ReadOnlySpan<TUnderlyingValue>(Values).ToArray();
    }
}
