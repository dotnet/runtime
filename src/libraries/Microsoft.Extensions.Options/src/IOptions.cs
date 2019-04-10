// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Used to retrieve configured TOptions instances.
    /// </summary>
    /// <typeparam name="TOptions">The type of options being requested.</typeparam>
    public interface IOptions<out TOptions> where TOptions : class, new()
    {
        /// <summary>
        /// The default configured TOptions instance
        /// </summary>
        TOptions Value { get; }
    }
}
