// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    /// <summary>
    /// This is used by <see cref="InstrumentRule"/> to distinguish between meters created via <see cref="Meter"/> constructors (<see cref="Global"/>)
    /// and those created via Dependency Injection with <see cref="IMeterFactory.Create(MeterOptions)"/> (<see cref="Local"/>)."/>.
    /// </summary>
    [Flags]
    public enum MeterScope
    {
        /// <summary>
        /// No scope is specified. This should not be used.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates <see cref="Meter"/> instances created via <see cref="Meter"/> constructors.
        /// </summary>
        Global,

        /// <summary>
        /// Indicates <see cref="Meter"/> instances created via Dependency Injection with <see cref="IMeterFactory.Create(MeterOptions)"/>.
        /// </summary>
        Local
    }
}
