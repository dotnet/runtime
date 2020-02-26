// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;

namespace Microsoft.Extensions.DependencyInjection.Fakes
{
    public struct StructFakeService : IFakeService
    {
        public StructFakeService(IServiceProvider serviceProvider)
        {
        }
    }
}