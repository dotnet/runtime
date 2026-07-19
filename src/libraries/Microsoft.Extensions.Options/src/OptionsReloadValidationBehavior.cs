// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Controls how an <see cref="IOptionsMonitor{TOptions}"/> serves values when asynchronous revalidation of a
    /// reloaded configuration fails, for options that opted in through the <c>ValidateOnChange</c> options-builder extension.
    /// </summary>
    public enum OptionsReloadValidationBehavior
    {
        /// <summary>
        /// Keeps serving the last successfully validated value and reports the failure through the error callback.
        /// </summary>
        KeepLastGood,

        /// <summary>
        /// Clears the cached value so the next read re-creates and validates it, surfacing the failure to the reader.
        /// </summary>
        FailReads,
    }
}
