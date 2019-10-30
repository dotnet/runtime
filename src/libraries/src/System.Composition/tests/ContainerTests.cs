// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
