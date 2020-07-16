// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    internal class TestServiceCollection : List<ServiceDescriptor>, IServiceCollection
    {
    }
}
