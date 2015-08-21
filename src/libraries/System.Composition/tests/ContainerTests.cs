// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Composition.Hosting.Providers;
using System.Composition.Convention;
using System.Reflection;
using Xunit;

namespace System.Composition.UnitTests
{
    public class ContainerTests
    {
        protected static CompositionContext CreateContainer(params Type[] types)
        {
            return new ContainerConfiguration()
                .WithParts(types)
                .CreateContainer();
        }

        protected static CompositionContext CreateContainer(ConventionBuilder rb, params Type[] types)
        {
            return new ContainerConfiguration()
                .WithParts(types)
                .WithDefaultConventions(rb)
                .CreateContainer();
        }
    }
}
