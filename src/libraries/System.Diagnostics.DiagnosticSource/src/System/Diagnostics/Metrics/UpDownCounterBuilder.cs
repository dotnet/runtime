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
    internal sealed class UpDownCounterBuilder<T> : IUpDownCounterBuilder<T>  where T : struct
    {
        private readonly UpDownCounter<T> _target;
        private readonly ImmutableArray<KeyValuePair<string, object?>> _tags;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpDownCounterBuilder{T}"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="tags">The tags.</param>
        public UpDownCounterBuilder(UpDownCounter<T> target, ImmutableArray<KeyValuePair<string, object?>> tags)
        {
            _target = target;
            _tags = tags;
        }

        /// <summary>
        /// Record the increment value of the measurement.
        /// </summary>
        /// <param name="delta">The increment measurement.</param>
        IUpDownCounterBuilder<T> IUpDownCounterBuilder<T>.Add(T delta)
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
        IUpDownCounterBuilder<T> ITagBuilder<T, IUpDownCounterBuilder<T>>.WithTag<TTag>(string key, TTag value)
        {
            var pair = new KeyValuePair<string, object?>(key, value);
            var list = _tags.Add(pair);
            return new UpDownCounterBuilder<T>(_target, list);
        }
    }
}
