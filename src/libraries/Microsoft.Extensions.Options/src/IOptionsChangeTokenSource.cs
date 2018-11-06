// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Used to fetch IChangeTokens used for tracking options changes.
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    public interface IOptionsChangeTokenSource<out TOptions>
    {
        /// <summary>
        /// Returns a IChangeToken which can be used to register a change notification callback.
        /// </summary>
        /// <returns></returns>
        IChangeToken GetChangeToken();

        /// <summary>
        /// The name of the option instance being changed.
        /// </summary>
        string Name { get; }
    }
}