// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Diagnostics.Metrics
{
    /// <summary>
    /// Builder which appends tags before adding the delta.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class HistogramBuilder<T> : IHistogramBuilder<T>  where T : struct
    {
        private readonly Histogram<T> _target;
        private readonly ImmutableArray<KeyValuePair<string, object?>> _tags;

        /// <summary>
        /// Initializes a new instance of the <see cref="HistogramBuilder{T}"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="tags">The tags.</param>
        public HistogramBuilder(Histogram<T> target, ImmutableArray<KeyValuePair<string, object?>> tags)
        {
            _target = target;
            _tags = tags;
        }

        /// <summary>
        /// Record a measurement value.
        /// </summary>
        /// <param name="value">The measurement value.</param>
        IHistogramBuilder<T> IHistogramBuilder<T>.Record(T value)
        {
            TagList tagList = new TagList(_tags.AsSpan());
            _target.Record(value, in tagList);
            return this;
        }

        /// <summary>
        /// Appends tags before adding the delta.
        /// </summary>
        /// <typeparam name="TTag"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        IHistogramBuilder<T> ITagBuilder<T, IHistogramBuilder<T>>.WithTag<TTag>(string key, TTag value)
        {
            var pair = new KeyValuePair<string, object?>(key, value);
            var list = _tags.Add(pair);
            return new HistogramBuilder<T>(_target, list);
        }
    }
}
