// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    /// <summary>
    /// Extension methods for <see cref="Meter" /> and <see cref="IMeterFactory" />.
    /// </summary>
    public static class MeterFactoryExtensions
    {
        /// <summary>
        /// Creates a <see cref="Meter" /> with the specified <paramref name="name" />, <paramref name="version" />, and <paramref name="tags" />.
        /// </summary>
        /// <param name="meterFactory">The <see cref="IMeterFactory" /> to use to create the <see cref="Meter" />.</param>
        /// <param name="name">The name of the <see cref="Meter" />.</param>
        /// <param name="version">The version of the <see cref="Meter" />.</param>
        /// <param name="tags">The tags to associate with the <see cref="Meter" />.</param>
        /// <returns>A <see cref="Meter" /> with the specified <paramref name="name" />, <paramref name="version" />, and <paramref name="tags" />.</returns>
        public static Meter Create(this IMeterFactory meterFactory, string name, string? version = null, IEnumerable<KeyValuePair<string, object?>>? tags = null)
        {
            if (meterFactory is null)
            {
                throw new ArgumentNullException(nameof(meterFactory));
            }

            return meterFactory.Create(new MeterOptions(name)
            {
                Version = version,
                Tags = tags,
                Scope = meterFactory
            });
        }
    }
}
