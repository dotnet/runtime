// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Represents a single opt-in to eager asynchronous revalidation on configuration reload for a named
    /// <typeparamref name="TOptions"/> instance, as registered by the <c>ValidateOnChange</c> options-builder extension.
    /// </summary>
    /// <typeparam name="TOptions">The options type this registration applies to.</typeparam>
    public sealed class ReloadValidationConfiguration<TOptions> where TOptions : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReloadValidationConfiguration{TOptions}"/> class.
        /// </summary>
        /// <param name="name">The name of the options instance to revalidate on reload.</param>
        /// <param name="behavior">How reads are served when revalidation of a reloaded configuration fails.</param>
        /// <param name="onError">An optional callback invoked with the options name and the exception when revalidation of a reloaded configuration fails.</param>
        public ReloadValidationConfiguration(string name, OptionsReloadValidationBehavior behavior, Action<string?, Exception>? onError)
        {
            ArgumentNullException.ThrowIfNull(name);

            Name = name;
            Behavior = behavior;
            OnError = onError;
        }

        /// <summary>
        /// Gets the name of the options instance to revalidate on reload.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a value that indicates how reads are served when revalidation of a reloaded configuration fails.
        /// </summary>
        public OptionsReloadValidationBehavior Behavior { get; }

        /// <summary>
        /// Gets the callback invoked with the options name and the exception when revalidation of a reloaded configuration fails.
        /// </summary>
        public Action<string?, Exception>? OnError { get; }
    }
}
