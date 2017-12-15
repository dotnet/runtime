// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class CompositionContainerExtensibilityTests
    {
        [Fact]
        public void Dispose_DoesNotThrow()
        {
            var container = CreateCustomCompositionContainer();
            container.Dispose();
        }

        [Fact]
        public void DerivedCompositionContainer_CanExportItself()
        {
            var container = CreateCustomCompositionContainer();
            container.AddAndComposeExportedValue<CustomCompositionContainer>(container);

            Assert.Same(container, container.GetExportedValue<CustomCompositionContainer>());
        }

        [Fact]
        public void ICompositionService_CanBeExported()
        {
            var container = CreateCustomCompositionContainer();
            container.AddAndComposeExportedValue<ICompositionService>(container);

            Assert.Same(container, container.GetExportedValue<ICompositionService>());
        }

        [Fact]
        public void CompositionContainer_CanBeExported()
        {
            var container = CreateCustomCompositionContainer();
            container.AddAndComposeExportedValue<CompositionContainer>(container);

            Assert.Same(container, container.GetExportedValue<CompositionContainer>());
        }

        [Fact]
        [ActiveIssue(25498)]
        public void CanBeCollectedAfterDispose()
        {
            AggregateExportProvider exportProvider = new AggregateExportProvider();
            var catalog = new AggregateCatalog(CatalogFactory.CreateDefaultAttributed());
            var container = new CompositionContainer(catalog, exportProvider);

            WeakReference weakContainer = new WeakReference(container);
            container.Dispose();
            container = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.False(weakContainer.IsAlive);

            GC.KeepAlive(exportProvider);
            GC.KeepAlive(catalog);
        }

        private CustomCompositionContainer CreateCustomCompositionContainer()
        {
            return new CustomCompositionContainer();
        }

        // Type needs to be public otherwise container.GetExportedValue<CustomCompositionContainer> 
        // fails on Silverlight because it cannot construct a Lazy<T,M> factory. 
        public class CustomCompositionContainer : CompositionContainer
        {
        }
    }
}
