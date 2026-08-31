// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class HostOptionsTests
    {
        [Fact]
        public void DefaultValues_AreSet()
        {
            var options = new HostOptions();

            Assert.Equal(TimeSpan.FromSeconds(30), options.ShutdownTimeout);
            Assert.Equal(Timeout.InfiniteTimeSpan, options.StartupTimeout);
            Assert.False(options.ServicesStartConcurrently);
            Assert.False(options.ServicesStopConcurrently);
            Assert.Equal(BackgroundServiceExceptionBehavior.StopHost, options.BackgroundServiceExceptionBehavior);
        }

        [Fact]
        public void Properties_CanBeSetDirectly()
        {
            var options = new HostOptions
            {
                ShutdownTimeout = TimeSpan.FromSeconds(60),
                StartupTimeout = TimeSpan.FromSeconds(90),
                ServicesStartConcurrently = true,
                ServicesStopConcurrently = true,
                BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore
            };

            Assert.Equal(TimeSpan.FromSeconds(60), options.ShutdownTimeout);
            Assert.Equal(TimeSpan.FromSeconds(90), options.StartupTimeout);
            Assert.True(options.ServicesStartConcurrently);
            Assert.True(options.ServicesStopConcurrently);
            Assert.Equal(BackgroundServiceExceptionBehavior.Ignore, options.BackgroundServiceExceptionBehavior);
        }

        [Fact]
        public void Properties_CanBeUpdated()
        {
            var options = new HostOptions
            {
                ShutdownTimeout = TimeSpan.FromSeconds(10),
                StartupTimeout = TimeSpan.FromSeconds(20)
            };

            options.ShutdownTimeout = TimeSpan.FromSeconds(50);
            options.StartupTimeout = TimeSpan.FromSeconds(100);

            Assert.Equal(TimeSpan.FromSeconds(50), options.ShutdownTimeout);
            Assert.Equal(TimeSpan.FromSeconds(100), options.StartupTimeout);
        }

        [Fact]
        public void ShutdownTimeout_AcceptsZero()
        {
            var options = new HostOptions
            {
                ShutdownTimeout = TimeSpan.Zero
            };

            Assert.Equal(TimeSpan.Zero, options.ShutdownTimeout);
        }

        [Fact]
        public void StartupTimeout_AcceptsZero()
        {
            var options = new HostOptions
            {
                StartupTimeout = TimeSpan.Zero
            };

            Assert.Equal(TimeSpan.Zero, options.StartupTimeout);
        }

        [Fact]
        public void Timeouts_AcceptMaxValue()
        {
            var options = new HostOptions
            {
                ShutdownTimeout = TimeSpan.MaxValue,
                StartupTimeout = TimeSpan.MaxValue
            };

            Assert.Equal(TimeSpan.MaxValue, options.ShutdownTimeout);
            Assert.Equal(TimeSpan.MaxValue, options.StartupTimeout);
        }
    }
}
