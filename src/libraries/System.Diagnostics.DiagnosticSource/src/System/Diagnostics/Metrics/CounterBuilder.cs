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
    internal sealed class CounterBuilder<T> : ICounterBuilder<T>  where T : struct
    {
        private readonly Counter<T> _target;
        private readonly ImmutableArray<KeyValuePair<string, object?>> _tags;

        /// <summary>
        /// Initializes a new instance of the <see cref="CounterBuilder{T}"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="tags">The tags.</param>
        public CounterBuilder(Counter<T> target, ImmutableArray<KeyValuePair<string, object?>> tags)
        {
            _target = target;
            _tags = tags;
        }

        /// <summary>
        /// Record the increment value of the measurement.
        /// </summary>
        /// <param name="delta">The increment measurement.</param>
        ICounterBuilder<T> ICounterBuilder<T>.Add(T delta)
        {
            TagList tagList = new TagList(_tags.AsSpan());
            _target.Add(delta, in tagList);
            return this;
        }

        /// <summary>
        /// Appends tags before adding the delta.
        /// </summary>
        /// <typeparam name="TTag"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        ICounterBuilder<T> ITagBuilder<T, ICounterBuilder<T>>.WithTag<TTag>(string key, TTag value)
        {
            var pair = new KeyValuePair<string, object?>(key, value);
            var list = _tags.Add(pair);
            return new CounterBuilder<T>(_target, list);
        }
    }
}
