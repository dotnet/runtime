// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extends <see cref="IServiceCollection"/> with support for service decorations.
    /// </summary>
    public interface IDecorationServiceCollection : IServiceCollection
    {
        /// <summary>
        /// Gets the list of <see cref="ServiceDecoration"/> entries describing decorations
        /// to apply to registered services.
        /// </summary>
        IList<ServiceDecoration> Decorations { get; }
    }
}
