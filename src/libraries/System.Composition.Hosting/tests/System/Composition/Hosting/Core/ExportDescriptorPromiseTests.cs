// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Composition.Hosting.Core.Tests
{
    public class ExportDescriptorPromiseTests
    {
        public static IEnumerable<object[]> Ctor_Depedencies()
        {
            yield return new object[] { null, null, false, Enumerable.Empty<CompositionDependency>() };
            yield return new object[] { new CompositionContract(typeof(int)), "Origin", true, Enumerable.Empty<CompositionDependency>() };
        }

        [Theory]
        [MemberData(nameof(Ctor_Depedencies))]
        public void Ctor_Dependencies(CompositionContract contract, string origin, bool isShared, IEnumerable<CompositionDependency> dependencies)
        {
            int calledDependencies = 0;
            int calledGetDescriptor = 0;
            var descriptor = ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            var promise = new ExportDescriptorPromise(contract, origin, isShared, () =>
            {
                calledDependencies++;
                return dependencies;
            }, getDependencies =>
            {
                Assert.Equal(dependencies, getDependencies);
                calledGetDescriptor++;
                return descriptor;
            });

            Assert.Same(contract, promise.Contract);
            Assert.Same(origin, promise.Origin);
            Assert.Equal(isShared, promise.IsShared);

            // The Dependencies parameter should only be invoked once.
            Assert.Equal(0, calledDependencies);
            Assert.Equal(0, calledGetDescriptor);
            Assert.Equal(dependencies, promise.Dependencies);
            Assert.Equal(1, calledDependencies);
            Assert.Equal(0, calledGetDescriptor);

            Assert.Same(promise.Dependencies, promise.Dependencies);
            Assert.Equal(1, calledDependencies);
            Assert.Equal(0, calledGetDescriptor);

            // The GetDescriptor parameter should only be invoked once.
            ExportDescriptor actualDescriptor = promise.GetDescriptor();
            Assert.Same(descriptor, actualDescriptor);
            Assert.Equal(1, calledDependencies);
            Assert.Equal(1, calledGetDescriptor);

            Assert.Same(actualDescriptor, promise.GetDescriptor());
            Assert.Equal(1, calledDependencies);
            Assert.Equal(1, calledGetDescriptor);
        }

        [Fact]
        public void Dependencies_GetWhenNull_ThrowsNullReferenceException()
        {
            var descriptor = ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            var promise = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", true, null, dependencies =>
            {
                return ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            });

            Assert.Throws<NullReferenceException>(() => promise.Dependencies);
        }

        [Fact]
        public void Dependencies_GetWhenReturnsNull_ThrowsArgumentNullException()
        {
            var descriptor = ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            var promise = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", true, () => null, depdendencies =>
            {
                return ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            });

            AssertExtensions.Throws<ArgumentNullException>("source", () => promise.Dependencies);
        }

        [Fact]
        public void GetDescriptor_GetWhenNull_ThrowsNullReferenceException()
        {
            var descriptor = ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            var promise = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", true, null, dependencies =>
            {
                return ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            });

            Assert.Throws<NullReferenceException>(() => promise.GetDescriptor());
        }

        [Fact]
        public void GetDescriptor_GetWhenReturnsNull_ThrowsArgumentNullException()
        {
            var descriptor = ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            var promise = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", true, () => Enumerable.Empty<CompositionDependency>(), depdendencies =>
            {
                return null;
            });

            AssertExtensions.Throws<ArgumentNullException>("descriptor", () => promise.GetDescriptor());
        }

        [Fact]
        public void GetDescriptor_CycleMetadataNotCompleted_MethodsThrowNotImplementedException()
        {
            ExportDescriptorPromise promise = null;
            promise = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", true, () => Enumerable.Empty<CompositionDependency>(), depdendencies =>
            {
                ExportDescriptor cycleDescriptor = promise.GetDescriptor();
                IDictionary<string, object> metadata = cycleDescriptor.Metadata;

                Assert.Throws<NotImplementedException>(() => metadata.Add("key", "value"));
                Assert.Throws<NotImplementedException>(() => metadata.Clear());
                Assert.Throws<NotImplementedException>(() => metadata.Add(default(KeyValuePair<string, object>)));
                Assert.Throws<NotImplementedException>(() => metadata.CopyTo(null, 0));
                Assert.Throws<NotImplementedException>(() => metadata.Contains(default(KeyValuePair<string, object>)));
                Assert.Throws<NotImplementedException>(() => metadata.ContainsKey("key"));
                Assert.Throws<NotImplementedException>(() => metadata.Count);
                Assert.Throws<NotImplementedException>(() => metadata.IsReadOnly);
                Assert.Throws<NotImplementedException>(() => metadata.GetEnumerator());
                Assert.Throws<NotImplementedException>(() => ((IEnumerable)metadata).GetEnumerator());
                Assert.Throws<NotImplementedException>(() => metadata.Keys);
                Assert.Throws<NotImplementedException>(() => metadata.Remove("key"));
                Assert.Throws<NotImplementedException>(() => metadata.Remove(default(KeyValuePair<string, object>)));
                Assert.Throws<NotImplementedException>(() => metadata.TryGetValue("key", out object _));
                Assert.Throws<NotImplementedException>(() => metadata.Values);

                Assert.Throws<NotImplementedException>(() => metadata["key"]);
                Assert.Throws<NotImplementedException>(() => metadata["key"] = "value");

                return ExportDescriptor.Create(Activator, new Dictionary<string, object> { { "key", "value" } });
            });

            // Invoke the GetDescriptor method to start the test.
            Assert.NotNull(promise.GetDescriptor());
        }

        [Fact]
        public void GetDescriptor_CycleMetadataCompleted_MethodsReturnExpected()
        {
            ExportDescriptorPromise promise = null;
            IDictionary<string, object> metadata = null;
            promise = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", true, () => Enumerable.Empty<CompositionDependency>(), depdendencies =>
            {
                ExportDescriptor cycleDescriptor = promise.GetDescriptor();
                metadata = cycleDescriptor.Metadata;

                return ExportDescriptor.Create(Activator, new Dictionary<string, object> { { "key", "value" } });
            });

            // Invoke the GetDescriptor method to start the test.
            Assert.NotNull(promise.GetDescriptor());

            // Make sure all the IDictionary methods complete successfully.
            Assert.Equal("value", metadata["key"]);
            Assert.True(metadata.Contains(new KeyValuePair<string, object>("key", "value")));
            Assert.True(metadata.ContainsKey("key"));
            Assert.False(metadata.IsReadOnly);

            metadata["key"] = "value2";
            Assert.True(metadata.TryGetValue("key", out object value));
            Assert.Equal("value2", value);

            metadata.Add("key2", "value");
            Assert.Equal(2, metadata.Count);

            metadata.Remove("key2");
            Assert.Equal(1, metadata.Count);

            metadata.Add(new KeyValuePair<string, object>("key2", "value"));
            Assert.Equal(2, metadata.Count);

            metadata.Remove(new KeyValuePair<string, object>("key2", "value"));
            Assert.Equal(1, metadata.Count);

            Assert.Equal(1, metadata.Keys.Count);
            Assert.Equal(1, metadata.Values.Count);

            var array = new KeyValuePair<string, object>[2];
            metadata.CopyTo(array, 1);
            Assert.Equal(new KeyValuePair<string, object>[] { default(KeyValuePair<string, object>), new KeyValuePair<string, object>("key", "value2") }, array);

            IEnumerator enumerator = metadata.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(new KeyValuePair<string, object>("key", "value2"), enumerator.Current);

            enumerator = ((IEnumerable)metadata).GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(new KeyValuePair<string, object>("key", "value2"), enumerator.Current);

            metadata.Clear();
            Assert.Equal(0, metadata.Count);
        }

        [Fact]
        public void GetDescriptor_CycleActivatorNotCompleted_ThrowsNotImplementedException()
        {
            ExportDescriptorPromise promise = null;
            promise = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", true, () => Enumerable.Empty<CompositionDependency>(), depdendencies =>
            {
                ExportDescriptor cycleDescriptor = promise.GetDescriptor();
                CompositeActivator activator = cycleDescriptor.Activator;
                Assert.Throws<NotImplementedException>(() => activator(null, null));

                return ExportDescriptor.Create(Activator, new Dictionary<string, object> { { "key", "value" } });
            });

            // Invoke the GetDescriptor method to start the test.
            Assert.NotNull(promise.GetDescriptor());
        }

        [Fact]
        public void GetDescriptor_CycleActivatorCompleted_Success()
        {
            ExportDescriptorPromise promise = null;
            CompositeActivator activator = null;
            promise = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", true, () => Enumerable.Empty<CompositionDependency>(), depdendencies =>
            {
                ExportDescriptor cycleDescriptor = promise.GetDescriptor();
                activator = cycleDescriptor.Activator;

                return ExportDescriptor.Create(Activator, new Dictionary<string, object> { { "key", "value" } });
            });

            ExportDescriptor descriptor = promise.GetDescriptor();
            Assert.Equal("hi", descriptor.Activator(null, null));
            Assert.Equal("hi", activator(null, null));
        }

        [Fact]
        public void GetDescriptor_CycleMetadataBroken_HasExpectedProperties()
        {
            ExportDescriptorPromise promise = null;
            ExportDescriptor cycleDescriptor = null;
            promise = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", true, () => Enumerable.Empty<CompositionDependency>(), depdendencies =>
            {
                cycleDescriptor = promise.GetDescriptor();
                return ExportDescriptor.Create(Activator, new Dictionary<string, object> { { "key", "value" } });
            });

            ExportDescriptor descriptor = promise.GetDescriptor();
            Assert.Same(descriptor.Activator, cycleDescriptor.Activator);
            Assert.Same(descriptor.Metadata, cycleDescriptor.Metadata);
        }

        [Fact]
        public void ToString_Invoke_ReturnsExpected()
        {
            var promise = new ExportDescriptorPromise(new CompositionContract(typeof(int)), "Origin", true, () => Enumerable.Empty<CompositionDependency>(), depdendencies =>
            {
                return ExportDescriptor.Create(Activator, new Dictionary<string, object>());
            });
            Assert.Equal("Int32 supplied by Origin", promise.ToString());
        }

        private static object Activator(LifetimeContext context, CompositionOperation operation) => "hi";
    }
}
