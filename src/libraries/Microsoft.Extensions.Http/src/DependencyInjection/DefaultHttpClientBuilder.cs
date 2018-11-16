﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection
{
    internal class DefaultHttpClientBuilder : IHttpClientBuilder
    {
        public DefaultHttpClientBuilder(IServiceCollection services, string name)
        {
            Services = services;
            Name = name; 
        }

        public string Name { get; }

        public IServiceCollection Services { get; }
    }
}
