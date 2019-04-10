// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Configuration.Test
{
    public class ConfigurationRootTest
    {
        [Fact]
        public void RootDisposesProviders()
        {
            var provider1 = new TestConfigurationProvider("foo", "foo-value");
            var provider2 = new DisposableTestConfigurationProvider("bar", "bar-value");
            var provider3 = new TestConfigurationProvider("baz", "baz-value");
            var provider4 = new DisposableTestConfigurationProvider("qux", "qux-value");
            var provider5 = new DisposableTestConfigurationProvider("quux", "quux-value");

            var config = new ConfigurationRoot(new IConfigurationProvider[] {
                provider1, provider2, provider3, provider4, provider5
            });

            Assert.Equal("foo-value", config["foo"]);
            Assert.Equal("bar-value", config["bar"]);
            Assert.Equal("baz-value", config["baz"]);
            Assert.Equal("qux-value", config["qux"]);
            Assert.Equal("quux-value", config["quux"]);

            config.Dispose();

            Assert.True(provider2.IsDisposed);
            Assert.True(provider4.IsDisposed);
            Assert.True(provider5.IsDisposed);
        }

        [Fact]
        public void RootDisposesChangeTokenRegistrations()
        {
            var changeToken = new ChangeToken();
            var providerMock = new Mock<IConfigurationProvider>();
            providerMock.Setup(p => p.GetReloadToken()).Returns(changeToken);

            var config = new ConfigurationRoot(new IConfigurationProvider[] {
                providerMock.Object,
            });

            Assert.NotEmpty(changeToken.Callbacks);

            config.Dispose();

            Assert.Empty(changeToken.Callbacks);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ChainedConfigurationIsDisposed(bool shouldDispose)
        {
            var provider = new DisposableTestConfigurationProvider("foo", "foo-value");
            var chainedConfig = new ConfigurationRoot(new IConfigurationProvider[] {
                provider
            });

            var config = new ConfigurationBuilder()
                .AddConfiguration(chainedConfig, shouldDisposeConfiguration: shouldDispose)
                .Build();

            Assert.False(provider.IsDisposed);

            (config as IDisposable).Dispose();

            Assert.Equal(shouldDispose, provider.IsDisposed);
        }

        private class TestConfigurationProvider : ConfigurationProvider
        {
            public TestConfigurationProvider(string key, string value)
                => Data.Add(key, value);
        }

        private class DisposableTestConfigurationProvider : ConfigurationProvider, IDisposable
        {
            public bool IsDisposed { get; set; }

            public DisposableTestConfigurationProvider(string key, string value)
                => Data.Add(key, value);

            public void Dispose()
                => IsDisposed = true;
        }

        public class ChangeToken : IChangeToken
        {
            public List<(Action<object>, object)> Callbacks { get; } = new List<(Action<object>, object)>();

            public bool HasChanged => false;

            public bool ActiveChangeCallbacks => true;

            public IDisposable RegisterChangeCallback(Action<object> callback, object state)
            {
                var item = (callback, state);
                Callbacks.Add(item);
                return new DisposableAction(() => Callbacks.Remove(item));
            }

            private class DisposableAction : IDisposable
            {
                private Action _action;

                public DisposableAction(Action action)
                {
                    _action = action;
                }

                public void Dispose()
                {
                    var a = _action;
                    if (a != null)
                    {
                        _action = null;
                        a();
                    }
                }
            }
        }
    }
}
