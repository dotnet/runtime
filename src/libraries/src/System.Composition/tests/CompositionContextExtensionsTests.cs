// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
