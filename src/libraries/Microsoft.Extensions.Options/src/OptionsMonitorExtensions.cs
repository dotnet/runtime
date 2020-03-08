// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Extension methods for <see cref="IOptionsMonitor{TOptions}"/>.
    /// </summary>
    public static class OptionsMonitorExtensions
    {
        /// <summary>
        /// Registers a listener to be called whenever <typeparamref name="TOptions"/> changes.
        /// </summary>
        /// <param name="monitor">The IOptionsMonitor.</param>
        /// <param name="listener">The action to be invoked when <typeparamref name="TOptions"/> has changed.</param>
        /// <returns>An <see cref="IDisposable"/> which should be disposed to stop listening for changes.</returns>
        public static IDisposable OnChange<TOptions>(this IOptionsMonitor<TOptions> monitor, Action<TOptions> listener)
            => monitor.OnChange((o, _) => listener(o));
    }
}
