// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Composition.Hosting.Core.Tests
{
    public class CompositionHostTests
    {
        [Fact]
        public void GetExport_CompositionContextContract_ReturnsExpected()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]))
            {
                Assert.True(host.TryGetExport(new CompositionContract(typeof(CompositionContext)), out object export));
                Assert.IsType<LifetimeContext>(export);
            }
        }

        [Fact]
        public void GetExport_MultipleDependencies_ReturnsExpected()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new MultipleDependency()))
            {
                Assert.True(host.TryGetExport(new CompositionContract(typeof(int)), out object export));
                Assert.Equal("hi", export);
            }
        }

        public class MultipleDependency : ExportDescriptorProvider
        {
            public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor descriptorAccessor)
            {
                var target = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin1", true, () =>
                {
                    var dependencyTarget = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin2", true, () => Enumerable.Empty<CompositionDependency>(), dependencies =>
                    {
                        return ExportDescriptor.Create(Activator, new Dictionary<string, object>());
                    });

                    return new CompositionDependency[]
                    {
                        CompositionDependency.Satisfied(contract, dependencyTarget, true, "Site")
                    };
                }, dependencies =>
                {
                    return ExportDescriptor.Create(Activator, new Dictionary<string, object>());
                });

                return new ExportDescriptorPromise[] { target };
            }
        }

        [Theory]
        [InlineData(typeof(int), new Type[] { typeof(int) })]
        [InlineData(typeof(IList<>), new Type[] { typeof(IList<>) })]
        [InlineData(typeof(ICollection<>), new Type[] { typeof(ICollection<>) })]
        [InlineData(typeof(IEnumerable<>), new Type[] { typeof(IEnumerable<>) })]
        [InlineData(typeof(IList<string>), new Type[] { typeof(IList<string>) })]
        [InlineData(typeof(ICollection<string>), new Type[] { typeof(ICollection<string>) })]
        [InlineData(typeof(IEnumerable<string>), new Type[] { typeof(IEnumerable<string>) })]
        [InlineData(typeof(string[]), new Type[] { typeof(string[]) })]
        [InlineData(typeof(Lazy<>), new Type[] { typeof(Lazy<>) })]
        [InlineData(typeof(Lazy<int>), new Type[] { typeof(int), typeof(Lazy<int>) })]
        [InlineData(typeof(Lazy<,>), new Type[] { typeof(Lazy<,>) })]
        [InlineData(typeof(Lazy<int, IDictionary<string, object>>), new Type[] { typeof(int), typeof(Lazy<int, IDictionary<string, object>>) })]
        [InlineData(typeof(Lazy<int, Dictionary<string, object>>), new Type[] { typeof(int), typeof(Lazy<int, Dictionary<string, object>>) })]
        [InlineData(typeof(Lazy<int, ParameterlessConstructor>), new Type[] { typeof(int), typeof(Lazy<int, ParameterlessConstructor>) })]
        [InlineData(typeof(ExportFactory<>), new Type[] { typeof(ExportFactory<>) })]
        [InlineData(typeof(ExportFactory<int>), new Type[] { typeof(int), typeof(ExportFactory<int>) })]
        [InlineData(typeof(ExportFactory<,>), new Type[] { typeof(ExportFactory<,>) })]
        [InlineData(typeof(ExportFactory<int, IDictionary<string, object>>), new Type[] { typeof(int), typeof(ExportFactory<int, IDictionary<string, object>>) })]
        public void GetExport_LazyOrExportFactoryContractType_ReturnsExpected(Type type, Type[] expectedContractTypes)
        {
            var tracker = new TrackingProvider();
            using (CompositionHost host = CompositionHost.CreateCompositionHost(tracker))
            {
                Assert.False(host.TryGetExport(type, out object export));
                Assert.Equal(expectedContractTypes, tracker.Contracts.Select(c => c.ContractType));
            }
        }

        [Theory]
        [InlineData(typeof(string[]), new Type[] { typeof(string[]), typeof(string) })]
        [InlineData(typeof(IList<string>), new Type[] { typeof(IList<string>), typeof(string) })]
        [InlineData(typeof(ICollection<string>), new Type[] { typeof(ICollection<string>), typeof(string) })]
        [InlineData(typeof(IEnumerable<string>), new Type[] { typeof(IEnumerable<string>), typeof(string) })]
        public void GetExport_ImoportManyWithMetadataConstraints_ReturnsExpected(Type contractType, Type[] expectedTypes)
        {
            var tracker = new TrackingProvider();
            using (CompositionHost host = CompositionHost.CreateCompositionHost(tracker))
            {
                var contract = new CompositionContract(contractType, "contractName", new Dictionary<string, object> { { "IsImportMany", true } });

                Assert.True(host.TryGetExport(contract, out object export));
                Assert.Empty(Assert.IsAssignableFrom<Array>(export));
                Assert.Equal(expectedTypes, tracker.Contracts.Select(c => c.ContractType));
            }
        }

        [Theory]
        [InlineData(typeof(ExportFactory<int>), null, new Type[] { typeof(int), typeof(ExportFactory<int>) })]
        [InlineData(typeof(ExportFactory<int>), new string[] { "1", "2", "3" }, new Type[] { typeof(int), typeof(ExportFactory<int>) })]
        [InlineData(typeof(ExportFactory<int, ParameterlessConstructor>), null, new Type[] { typeof(int), typeof(ExportFactory<int, ParameterlessConstructor>) })]
        [InlineData(typeof(ExportFactory<int, ParameterlessConstructor>), new string[] { "1", "2", "3" }, new Type[] { typeof(int), typeof(ExportFactory<int, ParameterlessConstructor>) })]
        public void GetExport_ExportFactoryContractWithMetadataConstraints_ReturnsExpected(Type contractType, string[] sharingBoundaryNames, Type[] expectedTypes)
        {
            var tracker = new TrackingProvider();
            using (CompositionHost host = CompositionHost.CreateCompositionHost(tracker))
            {
                var contract = new CompositionContract(contractType, "contractName", new Dictionary<string, object> { { "SharingBoundaryNames", sharingBoundaryNames } });

                Assert.False(host.TryGetExport(contract, out object export));
                Assert.Equal(expectedTypes, tracker.Contracts.Select(c => c.ContractType));
            }
        }

        private class TrackingProvider : ExportDescriptorProvider
        {
            public List<CompositionContract> Contracts { get; } = new List<CompositionContract>();

            public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor descriptorAccessor)
            {
                Contracts.Add(contract);
                return new ExportDescriptorPromise[0];
            }
        }

        [Theory]
        [InlineData(typeof(Lazy<int, int>))]
        [InlineData(typeof(Lazy<int, IDictionary<string, string>>))]
        [InlineData(typeof(Lazy<int, PrivateConstructor>))]
        [InlineData(typeof(Lazy<int, NoParameterlessConstructor>))]
        public void GetExport_InvalidMetadata_ThrowsComposititionFailedException(Type type)
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]))
            {
                Assert.Throws<CompositionFailedException>(() => host.TryGetExport(type, out object _));
            }
        }

        [Fact]
        public void GetExport_AbstractMetadata_ThrowsInvalidOperationException()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]))
            {
                Assert.Throws<InvalidOperationException>(() => host.TryGetExport(typeof(Lazy<int, AbstractConstructor>), out object _));
            }
        }

        [Fact]
        public void GetExport_NullProviderInProviders_ThrowsNullReferenceException()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[] { null }))
            {
                Assert.Throws<NullReferenceException>(() => host.GetExport<int>());
            }
        }

        [Fact]
        public void TryGetExport_NullContract_ThrowsArgumentNullException()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]))
            {
                AssertExtensions.Throws<ArgumentNullException>("key", () => host.TryGetExport((CompositionContract)null, out object export));
            }
        }

        [Fact]
        public void GetExport_MultipleReturns_ThrowsCompositionFailedException()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new MultiplePromises()))
            {
                Assert.Throws<CompositionFailedException>(() => host.TryGetExport(new CompositionContract(typeof(int)), out object export));
            }
        }

        public class MultiplePromises : ExportDescriptorProvider
        {
            public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor descriptorAccessor)
            {
                var target = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", true, () => Enumerable.Empty<CompositionDependency>(), dependencies =>
                {
                    return ExportDescriptor.Create(Activator, new Dictionary<string, object>());
                });

                return new ExportDescriptorPromise[] { target, target };
            }
        }

        [Fact]
        public void GetExport_FailedDependency_ThrowsCompositionFailedException()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new FailedDependency()))
            {
                Assert.Throws<CompositionFailedException>(() => host.TryGetExport(new CompositionContract(typeof(int)), out object export));
            }
        }

        public class FailedDependency : ExportDescriptorProvider
        {
            public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor descriptorAccessor)
            {
                var target = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", true, () =>
                {
                    return new CompositionDependency[]
                    {
                        CompositionDependency.Missing(contract, "site")
                    };
                }, dependencies =>
                {
                    return ExportDescriptor.Create(Activator, new Dictionary<string, object>());
                });

                return new ExportDescriptorPromise[] { target };
            }
        }

        [Fact]
        public void CreateCompositionHost_NullProvider_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("providers", () => CompositionHost.CreateCompositionHost(null));
            AssertExtensions.Throws<ArgumentNullException>("providers", () => CompositionHost.CreateCompositionHost((IEnumerable<ExportDescriptorProvider>)null));
        }

        [Fact]
        public void Dispose_MultipleTimes_Success()
        {
            CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]);
            host.Dispose();
            host.Dispose();
        }

        private static object Activator(LifetimeContext context, CompositionOperation operation) => "hi";

        private class ParameterlessConstructor
        {
            public ParameterlessConstructor() { }

            public int PublicProperty { get; set; }

            public static int PublicStaticProperty { get; set; }
            public int GetOnlyProperty { get; }
            public int SetOnlyProperty { set { } }
            private int PrivateProperty { get; set; }
        }

        private class PrivateConstructor
        {
            private PrivateConstructor() { }
        }

        private class NoParameterlessConstructor
        {
            public NoParameterlessConstructor(int x) { }
        }

        private abstract class AbstractConstructor
        {
            public AbstractConstructor() { }
        }
    }
}
