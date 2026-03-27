// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Diagnostics.Configuration
{
    /// <summary>
    /// Represents scopes used by <see cref="TracingRule"/> to distinguish between activity sources created directly
    /// via <see cref="System.Diagnostics.ActivitySource"/> constructors (<see cref="Global"/>) and those created via
    /// dependency injection with <see cref="System.Diagnostics.IActivitySourceFactory.Create(System.Diagnostics.ActivitySourceOptions)"/> (<see cref="Local"/>).
    /// </summary>
    [Flags]
    public enum ActivitySourceScope
    {
        /// <summary>
        /// No scope is specified. This field should not be used.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates <see cref="System.Diagnostics.ActivitySource"/> instances created via constructors.
        /// </summary>
        Global,

        /// <summary>
        /// Indicates <see cref="System.Diagnostics.ActivitySource"/> instances created via <see cref="System.Diagnostics.IActivitySourceFactory"/>.
        /// </summary>
        Local
    }
}
