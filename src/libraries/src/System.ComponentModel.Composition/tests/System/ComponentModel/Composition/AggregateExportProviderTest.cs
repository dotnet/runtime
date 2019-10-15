// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class AggregateExportProviderTests
    {
        [Fact]
        public void Constructor1_NullAsProvidersArgument_ShouldSetProvidersPropertyToEmpty()
        {
            var provider = new AggregateExportProvider((ExportProvider[])null);

            Assert.Empty(provider.Providers);
        }

        [Fact]
        public void Constructor2_NullAsProvidersArgument_ShouldSetProvidersPropertyToEmpty()
        {
            var provider = new AggregateExportProvider((IEnumerable<ExportProvider>)null);

            Assert.Empty(provider.Providers);
        }

        [Fact]
        public void Constructor1_EmptyArrayAsProvidersArgument_ShouldSetProvidersPropertyToEmpty()
        {
            var provider = new AggregateExportProvider(new ExportProvider[0]);

            Assert.Empty(provider.Providers);
        }

        [Fact]
        public void Constructor2_EmptyArrayAsProvidersArgument_ShouldSetProvidersPropertyToEmpty()
        {
            var provider = new AggregateExportProvider((IEnumerable<ExportProvider>)new ExportProvider[0]);

            Assert.Empty(provider.Providers);
        }

        [Fact]
        public void Constructor2_EmptyEnumerableAsProvidersArgument_ShouldSetProvidersPropertyToEmpty()
        {
            var provider = new AggregateExportProvider(Enumerable.Empty<ExportProvider>());

            Assert.Empty(provider.Providers);
        }

        [Fact]
        public void Constructor1_ArrayAsProvidersArgument_ShouldNotAllowModificationAfterConstruction()
        {
            var providers = new ExportProvider[] { ExportProviderFactory.Create() };
            var provider = new AggregateExportProvider(providers);

            providers[0] = null;

            Assert.NotNull(provider.Providers[0]);
        }

        [Fact]
        public void Constructor2_ArrayAsProvidersArgument_ShouldNotAllowModificationAfterConstruction()
        {
            var providers = new ExportProvider[] { ExportProviderFactory.Create() };
            var provider = new AggregateExportProvider((IEnumerable<ExportProvider>)providers);

            providers[0] = null;

            Assert.NotNull(provider.Providers[0]);
        }

        [Fact]
        public void Providers_WhenDisposed_ShouldThrowObjectDisposed()
        {
            var provider = CreateAggregateExportProvider();
            provider.Dispose();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                var providers = provider.Providers;
            });
        }

        private AggregateExportProvider CreateAggregateExportProvider()
        {
            return new AggregateExportProvider(Enumerable.Empty<ExportProvider>());
        }
    }
}
