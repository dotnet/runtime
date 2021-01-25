// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A simple struct we wrap reference types inside when storing in arrays to
    /// bypass the CLR's covariant checks when writing to arrays.
    /// </summary>
    /// <remarks>
    /// We use <see cref="RefAsValueType{T}"/> as a wrapper to avoid paying the cost of covariant checks whenever
    /// the underlying array that the <see cref="Stack{T}"/> class uses is written to.
    /// We've recognized this as a perf win in ETL traces for these stack frames:
    /// clr!JIT_Stelem_Ref
    ///   clr!ArrayStoreCheck
    ///     clr!ObjIsInstanceOf
    /// </remarks>
    [DebuggerDisplay("{Value,nq}")]
    internal struct RefAsValueType<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RefAsValueType{T}"/> struct.
        /// </summary>
        internal RefAsValueType(T value)
        {
            this.Value = value;
        }

        /// <summary>
        /// The value.
        /// </summary>
        internal T Value;
    }
}
