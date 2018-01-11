// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class ComposablePartCatalogTests
    {
        [Fact]
        public void GetExports_WhenDisposed_ShouldThrowObjectDisposed()
        {
            var catalog = CatalogFactory.Create();
            catalog.Dispose();
			var definition = ImportDefinitionFactory.Create();

            ExceptionAssert.ThrowsDisposed(catalog, () =>
            {
                catalog.GetExports(definition);
            });
        }

        [Fact]
        public void GetExports_NullAsConstraintArgument_ShouldThrowArgumentNull()
        {
            var catalog = CatalogFactory.Create();

            Assert.Throws<ArgumentNullException>("definition", () =>
            {
                catalog.GetExports((ImportDefinition)null);
            });
        }

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            var catalog = CatalogFactory.Create();
            catalog.Dispose();
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var catalog = CatalogFactory.Create();
            catalog.Dispose();
            catalog.Dispose();
            catalog.Dispose();
        }

        [Fact]
        public void Dispose_CallsGCSuppressFinalize()
        {
            bool finalizerCalled = false;

            var catalog = CatalogFactory.CreateDisposable(disposing =>
            {
                if (!disposing)
                {
                    finalizerCalled = true;
                }

            });

            catalog.Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(finalizerCalled);
        }

        [Fact]
        public void Dispose_CallsDisposeBoolWithTrue()
        {
            var catalog = CatalogFactory.CreateDisposable(disposing =>
            {
                Assert.True(disposing);
            });

            catalog.Dispose();
        }

        [Fact]
        public void Dispose_CallsDisposeBoolOnce()
        {
            int disposeCount = 0;
            var catalog = CatalogFactory.CreateDisposable(disposing =>
            {
                disposeCount++;
            });

            catalog.Dispose();

            Assert.Equal(1, disposeCount);
        }

        private IQueryable<ComposablePartDefinition> GetPartDefinitions(ExportDefinition definition)
        {
            var partDefinition = PartDefinitionFactory.Create(null, () => null, Enumerable.Empty<ImportDefinition>(), new ExportDefinition[] { definition });

            return new ComposablePartDefinition[] { partDefinition }.AsQueryable();
        }
    }
}

