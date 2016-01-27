// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using Xunit;

namespace Test
{
    public class ComponentModelPrimitivesTests
    {
        [Fact]
        public static void TestComponentPrimitivesModelBasic()
        {
            // dummy tests to make sure the System.ComponentModel.Primitives library loaded successfully
#pragma warning disable 0219
            IComponent iComponent = null;
            Assert.Null(iComponent);

            IContainer iContainer = null;
            Assert.Null(iContainer);

            ISite iSite = null;
            IServiceProvider iServiceProvider = iSite;
            Assert.Null(iServiceProvider);

            ComponentCollection componentCollection = null;
            Assert.Null(componentCollection);
#pragma warning restore 0219
        }
    }
}
