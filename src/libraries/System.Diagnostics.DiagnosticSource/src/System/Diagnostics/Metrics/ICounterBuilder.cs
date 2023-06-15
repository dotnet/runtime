// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// Builder which appends tags before adding the delta.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICounterBuilder<T>: ITagBuilder<T, ICounterBuilder<T>> where T : struct
    {
        /// <summary>
        /// Record the increment value of the measurement.
        /// </summary>
        /// <param name="delta">The increment measurement.</param>
        public ICounterBuilder<T> Add(T delta);
    }
}
