// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// Builder which appends tags before adding the record.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IHistogramBuilder<T>: ITagBuilder<T, IHistogramBuilder<T>> where T : struct
    {
        /// <summary>
        /// Record a measurement value.
        /// </summary>
        /// <param name="value">The measurement value.</param>
        IHistogramBuilder<T> Record(T value);
    }
}
