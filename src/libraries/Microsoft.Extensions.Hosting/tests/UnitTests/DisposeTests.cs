// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public partial class DisposeTests
    {
        public static IHostBuilder CreateHostBuilder(Action<IServiceCollection> configure)
        {
            return new HostBuilder().ConfigureServices(configure);
        }

        [Fact]
        public void DisposeCalled_Interface()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddSingleton<IMyService>((sp) => new MyService());
            });

            IMyService obj;
            using (var host = hostBuilder.Build())
            {
                obj = host.Services.GetService<IMyService>();
            }

            Assert.True(obj.IsDisposed);
        }

        [Fact]
        public void DisposeCalled_Class()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddSingleton<MyService>();
            });

            MyService obj;
            using (var host = hostBuilder.Build())
            {
                obj = host.Services.GetService<MyService>();
            }

            Assert.True(obj.IsDisposed);
        }

        [Fact]
        public void DisposeNotCalled()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddSingleton(new MyService());
            });

            MyService obj;
            using (var host = hostBuilder.Build())
            {
                obj = host.Services.GetService<MyService>();
            }

            Assert.False(obj.IsDisposed);
        }

        public interface IMyService : IDisposable
        {
            bool IsDisposed { get; }
        }

        public class MyService : IMyService
        {
            private bool _isDisposed;

            public MyService()
            {
            }

            public bool IsDisposed => _isDisposed;

            protected virtual void Dispose(bool disposing)
            {
                if (!_isDisposed)
                {
                    _isDisposed = true;
                }
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }
}
