// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Composition.Hosting.Core.Tests
{
    public class LifetimeContextTests
    {
        [Fact]
        public void AllocateSharingId_InvokeMultipleTimes_ReturnsDifferentValue()
        {
            Assert.NotEqual(LifetimeContext.AllocateSharingId(), LifetimeContext.AllocateSharingId());
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void AddBoundInstance_NonNullInstance_DisposesInstanceOnDisposal()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]))
            {
                Assert.True(host.TryGetExport(new CompositionContract(typeof(CompositionContext)), out object export));
                LifetimeContext context = Assert.IsType<LifetimeContext>(export);

                var instance = new DisposableInstance();
                context.AddBoundInstance(instance);
                Assert.Equal(0, instance.CalledDisposed);

                context.Dispose();
                Assert.Equal(1, instance.CalledDisposed);

                context.Dispose();
                Assert.Equal(1, instance.CalledDisposed);
            }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void AddBoundInstance_NullInstance_ThrowsNullReferenceExceptionOnDisposal()
        {
            CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]);
            Assert.True(host.TryGetExport(new CompositionContract(typeof(CompositionContext)), out object export));
            LifetimeContext context = Assert.IsType<LifetimeContext>(export);

            context.AddBoundInstance(null);
            Assert.Throws<NullReferenceException>(() => context.Dispose());
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void AddBoundInstance_Disposed_ThrowsObjectDisposedException()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]))
            {
                Assert.True(host.TryGetExport(new CompositionContract(typeof(CompositionContext)), out object export));
                LifetimeContext context = Assert.IsType<LifetimeContext>(export);
                context.Dispose();

                Assert.Throws<ObjectDisposedException>(() => context.AddBoundInstance(null));
            }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void GetOrCreate_ValidActivatorDuringInitialization_Success()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]))
            {
                Assert.True(host.TryGetExport(new CompositionContract(typeof(CompositionContext)), out object export));
                LifetimeContext context = Assert.IsType<LifetimeContext>(export);

                object Activator(LifetimeContext activatorContext, CompositionOperation activatorOperation)
                {
                    var value = new object();
                    object GetOrCreateActivate(LifetimeContext getOrCreateContext, CompositionOperation getOrCreateOperator)
                    {
                        Assert.Same(activatorContext, getOrCreateContext);
                        Assert.Same(activatorOperation, getOrCreateOperator);

                        return value;
                    }

                    Assert.Same(value, activatorContext.GetOrCreate(1, activatorOperation, GetOrCreateActivate));
                    Assert.Same(value, activatorContext.GetOrCreate(1, activatorOperation, GetOrCreateActivate));
                    return "Hi";
                }

                Assert.Equal("Hi", CompositionOperation.Run(context, Activator));
            }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void GetOrCreate_ValidActivatorAfterInitialization_Success()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]))
            {
                Assert.True(host.TryGetExport(new CompositionContract(typeof(CompositionContext)), out object export));
                LifetimeContext context = Assert.IsType<LifetimeContext>(export);

                CompositionOperation operation = null;
                var value = new object();
                object GetOrCreateActivate(LifetimeContext getOrCreateContext, CompositionOperation getOrCreateOperator)
                {
                    Assert.Same(context, getOrCreateContext);
                    Assert.Same(operation, getOrCreateOperator);

                    return value;
                }
                object Activator(LifetimeContext activatorContext, CompositionOperation activatorOperation)
                {
                    operation = activatorOperation;
                    Assert.Same(value, context.GetOrCreate(1, operation, GetOrCreateActivate));
                    return "Hi";
                }

                Assert.Equal("Hi", CompositionOperation.Run(context, Activator));
                Assert.Same(value, context.GetOrCreate(1, operation, GetOrCreateActivate));
            }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void GetOrCreate_NullActivator_ThrowsNullReferenceException()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]))
            {
                Assert.True(host.TryGetExport(new CompositionContract(typeof(CompositionContext)), out object export));
                LifetimeContext context = Assert.IsType<LifetimeContext>(export);

                object Activator(LifetimeContext activatorContext, CompositionOperation activatorOperation)
                {
                    Assert.Throws<NullReferenceException>(() => activatorContext.GetOrCreate(1, activatorOperation, null));
                    return "Hi";
                }

                Assert.Equal("Hi", CompositionOperation.Run(context, Activator));
            }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void GetOrCreate_NullOperation_ThrowsNullReferenceException()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]))
            {
                Assert.True(host.TryGetExport(new CompositionContract(typeof(CompositionContext)), out object export));
                LifetimeContext context = Assert.IsType<LifetimeContext>(export);

                Assert.Throws<NullReferenceException>(() => context.GetOrCreate(1, null, Activator));
            }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void FindContextWithin_NullSharingBoundary_ReturnsRoot()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]))
            {
                Assert.True(host.TryGetExport(new CompositionContract(typeof(CompositionContext)), out object export));
                LifetimeContext context = Assert.IsType<LifetimeContext>(export);

                Assert.Same(context, context.FindContextWithin(null));
            }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void FindContextWithin_UnknownSharingBoundary_ThrowsCompositionFailedException()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]))
            {
                Assert.True(host.TryGetExport(new CompositionContract(typeof(CompositionContext)), out object export));
                LifetimeContext context = Assert.IsType<LifetimeContext>(export);

                Assert.Throws<CompositionFailedException>(() => context.FindContextWithin("sharingBoundary"));
            }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void ToString_NoParent_ReturnsExpected()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]))
            {
                Assert.True(host.TryGetExport(new CompositionContract(typeof(CompositionContext)), out object export));
                LifetimeContext context = Assert.IsType<LifetimeContext>(export);

                Assert.Equal("Root Lifetime Context", context.ToString());
            }
        }

        private class DisposableInstance : IDisposable
        {
            public int CalledDisposed { get; set; }

            public void Dispose() => CalledDisposed++;
        }

        private static object Activator(LifetimeContext context, CompositionOperation operation) => "hi";
    }
}
