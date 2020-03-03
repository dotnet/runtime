// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Specifies the contract for a collection of service descriptors.
    /// </summary>
    public interface IServiceCollection : IList<ServiceDescriptor>
    {
    }
}
