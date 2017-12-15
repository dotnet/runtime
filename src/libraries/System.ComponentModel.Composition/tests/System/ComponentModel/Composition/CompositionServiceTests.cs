// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Hosting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class CompositionServiceTests
    {
        public interface IFoo { }

        public class CFoo : IFoo { }

        public class FooImporter
        {
            [Import]
            public ICompositionService CompositionService;

            [Import]
            public IFoo fooImporter { get; set; }
        }

        [Fact]
        public void SatisfyParts_NullArgument_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("part", () =>
            {
                var compositionService = new TypeCatalog().CreateCompositionService();
                compositionService.SatisfyImportsOnce(null);
            });
        }
    }
}

