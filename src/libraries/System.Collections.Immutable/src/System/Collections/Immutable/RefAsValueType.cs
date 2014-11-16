// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A simple struct we wrap reference types inside when storing in arrays to
    /// bypass the CLR's covariant checks when writing to arrays.
    /// </summary>
    /// <remarks>
    /// We use RefAsValueType{T} as a wrapper to avoid paying the cost of covariant checks whenever
    /// the underlying array that the Stack{T} class uses is written to. 
    /// We've recognized this as a perf win in ETL traces for these stack frames:
    /// clr!JIT_Stelem_Ref
    ///   clr!ArrayStoreCheck
    ///     clr!ObjIsInstanceOf
    /// </remarks>
    [DebuggerDisplay("{Value,nq}")]
    internal struct RefAsValueType<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RefAsValueType&lt;T&gt;"/> struct.
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
