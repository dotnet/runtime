// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public class AsyncServiceScopeTests
    {
        [Fact]
        public void ThrowsIfServiceScopeIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new AsyncServiceScope(null));
            Assert.Equal("serviceScope", exception.ParamName);
        }

        [Fact]
        public void ReturnsServiceProviderFromWrappedScope()
        {
            var wrappedScope = new FakeSyncServiceScope();
            var asyncScope = new AsyncServiceScope(wrappedScope);

            Assert.Same(wrappedScope.ServiceProvider, asyncScope.ServiceProvider);
        }

        [Fact]
        public void CallsDisposeOnWrappedSyncScopeOnDispose()
        {
            var wrappedScope = new FakeSyncServiceScope();
            var asyncScope = new AsyncServiceScope(wrappedScope);

            asyncScope.Dispose();

            Assert.True(wrappedScope.DisposeCalled);
        }

        [Fact]
        public async ValueTask CallsDisposeOnWrappedSyncScopeOnDisposeAsync()
        {
            var wrappedScope = new FakeSyncServiceScope();
            var asyncScope = new AsyncServiceScope(wrappedScope);

            await asyncScope.DisposeAsync();

            Assert.True(wrappedScope.DisposeCalled);
        }

        [Fact]
        public void CallsDisposeOnWrappedAsyncScopeOnDispose()
        {
            var wrappedScope = new FakeAsyncServiceScope();
            var asyncScope = new AsyncServiceScope(wrappedScope);

            asyncScope.Dispose();

            Assert.True(wrappedScope.DisposeCalled);
            Assert.False(wrappedScope.DisposeAsyncCalled);
        }

        [Fact]
        public async ValueTask CallsDisposeAsyncOnWrappedSyncScopeOnDisposeAsync()
        {
            var wrappedScope = new FakeAsyncServiceScope();
            var asyncScope = new AsyncServiceScope(wrappedScope);

            await asyncScope.DisposeAsync();

            Assert.False(wrappedScope.DisposeCalled);
            Assert.True(wrappedScope.DisposeAsyncCalled);
        }

        public class FakeServiceProvider : IServiceProvider
        {
            public object? GetService(Type serviceType) => throw new NotImplementedException();
        }

        public class FakeSyncServiceScope : IServiceScope
        {
            public FakeSyncServiceScope()
            {
                ServiceProvider = new FakeServiceProvider();
            }

            public IServiceProvider ServiceProvider { get; }

            public bool DisposeCalled { get; private set; }

            public void Dispose()
            {
                DisposeCalled = true;
            }
        }

        public class FakeAsyncServiceScope : FakeSyncServiceScope, IAsyncDisposable
        {
            public FakeAsyncServiceScope() : base()
            {
            }

            public bool DisposeAsyncCalled { get; private set; }

            public ValueTask DisposeAsync()
            {
                DisposeAsyncCalled = true;

                return default;
            }
        }
    }
}
