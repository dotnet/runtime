// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Composition.UnitTests
{
    public class CompositionContextExtensionsTests : ContainerTests
    {
        public interface IUnregistered { }

        [Fact]
        public void GettingAnOptionalExportThatDoesntExistReturnsNull()
        {
            var c = CreateContainer();

            IUnregistered unregistered;
            Assert.False(c.TryGetExport<IUnregistered>(null, out unregistered));
            Assert.Null(unregistered);
        }
    }
}
