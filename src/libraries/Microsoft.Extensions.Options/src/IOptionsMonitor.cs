// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Used for notifications when TOptions instances change.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    public interface IOptionsMonitor<out TOptions>
    {
        /// <summary>
        /// Returns the current TOptions instance with the <see cref="Options.DefaultName"/>.
        /// </summary>
        TOptions CurrentValue { get; }

        /// <summary>
        /// Returns a configured TOptions instance with the given name.
        /// </summary>
        TOptions Get(string name);

        /// <summary>
        /// Registers a listener to be called whenever a named TOptions changes.
        /// </summary>
        /// <param name="listener">The action to be invoked when TOptions has changed.</param>
        /// <returns>An IDisposable which should be disposed to stop listening for changes.</returns>
        IDisposable OnChange(Action<TOptions, string> listener);
    }
}