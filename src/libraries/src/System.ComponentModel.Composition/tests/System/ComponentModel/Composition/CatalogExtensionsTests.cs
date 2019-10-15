// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Hosting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class CatalogExtensionsTests
    {
        [Fact]
        public void CreateCompositionService_NullCatalog_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("composablePartCatalog", () =>
            {
                CatalogExtensions.CreateCompositionService(null);
            });
        }

        [Fact]
        public void CreateCompositionService_ImmutableCatalog_ShouldSucceed()
        {
            //Create and dispose an empty immutable catalog, I.e no INotifyComposablePartCatalogChanged interface
            var catalog = new TypeCatalog();
            using(var cs = catalog.CreateCompositionService())
            {
                //Do nothing
            }
        }
    }
}
