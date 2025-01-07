// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Primitives
{
    public class CompositeChangeTokenTest
    {
        [Fact]
        public void RegisteredCallbacks_AreInvokedExactlyOnce()
        {
            // Arrange
            var firstCancellationTokenSource = new CancellationTokenSource();
            var secondCancellationTokenSource = new CancellationTokenSource();
            var thirdCancellationTokenSource = new CancellationTokenSource();
            var firstCancellationToken = firstCancellationTokenSource.Token;
            var secondCancellationToken = secondCancellationTokenSource.Token;
            var thirdCancellationToken = thirdCancellationTokenSource.Token;

            var firstCancellationChangeToken = new CancellationChangeToken(firstCancellationToken);
            var secondCancellationChangeToken = new CancellationChangeToken(secondCancellationToken);
            var thirdCancellationChangeToken = new CancellationChangeToken(thirdCancellationToken);

            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { firstCancellationChangeToken, secondCancellationChangeToken, thirdCancellationChangeToken });
            var count1 = 0;
            var count2 = 0;
            compositeChangeToken.RegisterChangeCallback(_ => count1++, null);
            compositeChangeToken.RegisterChangeCallback(_ => count2++, null);

            // Act
            firstCancellationTokenSource.Cancel();
            secondCancellationTokenSource.Cancel();

            // Assert
            Assert.Equal(1, count1);
            Assert.Equal(1, count2);
        }

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void HasChanged_IsTrue_IfAnyTokenHasChanged()
        {
            // Arrange
            var firstChangeToken = new Mock<IChangeToken>();
            var secondChangeToken = new Mock<IChangeToken>();
            var thirdChangeToken = new Mock<IChangeToken>();

            secondChangeToken.Setup(t => t.HasChanged).Returns(true);

            // Act
            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { firstChangeToken.Object, secondChangeToken.Object, thirdChangeToken.Object });

            // Assert
            Assert.True(compositeChangeToken.HasChanged);
        }

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void HasChanged_IsFalse_IfNoTokenHasChanged()
        {
            // Arrange
            var firstChangeToken = new Mock<IChangeToken>();
            var secondChangeToken = new Mock<IChangeToken>();

            // Act
            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { firstChangeToken.Object, secondChangeToken.Object });            

            // Assert
            Assert.False(compositeChangeToken.HasChanged);
        }

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void ActiveChangeCallbacks_IsTrue_IfAnyTokenHasActiveChangeCallbacks()
        {
            // Arrange
            var firstChangeToken = new Mock<IChangeToken>();
            var secondChangeToken = new Mock<IChangeToken>();
            var thirdChangeToken = new Mock<IChangeToken>();

            secondChangeToken.Setup(t => t.ActiveChangeCallbacks).Returns(true);

            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { firstChangeToken.Object, secondChangeToken.Object, thirdChangeToken.Object });

            // Act & Assert
            Assert.True(compositeChangeToken.ActiveChangeCallbacks);
        }

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void ActiveChangeCallbacks_IsFalse_IfNoTokenHasActiveChangeCallbacks()
        {
            // Arrange
            var firstChangeToken = new Mock<IChangeToken>();
            var secondChangeToken = new Mock<IChangeToken>();

            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { firstChangeToken.Object, secondChangeToken.Object });

            // Act & Assert
            Assert.False(compositeChangeToken.ActiveChangeCallbacks);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task RegisteredCallbackGetsInvokedExactlyOnce_WhenMultipleConcurrentChangeEventsOccur()
        {
            // Arrange
            var event1 = new ManualResetEvent(false);
            var event2 = new ManualResetEvent(false);
            var event3 = new ManualResetEvent(false);

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var cancellationChangeToken = new CancellationChangeToken(cancellationToken);
            var count = 0;
            Action<object> callback = _ =>
            {
                count++;
                event3.Set();
                event1.WaitOne(5000);
            };

            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { cancellationChangeToken });
            compositeChangeToken.RegisterChangeCallback(callback, null);

            // Act
            var firstChange = Task.Run(() =>
            {
                event2.WaitOne(5000);
                cancellationTokenSource.Cancel();
            });
            var secondChange = Task.Run(() =>
            {
                event3.WaitOne(5000);
                cancellationTokenSource.Cancel();
                event1.Set();
            });

            event2.Set();

            await Task.WhenAll(firstChange, secondChange);

            // Assert
            Assert.Equal(1, count);
        }
        
        [Fact]
        public void ShouldNotCollectDisposablesIfChangedTokenEncountered()
        {
            // Arrange
            var firstCancellationTokenSource = new CancellationTokenSource();
            var secondCancellationTokenSource = new CancellationTokenSource();
            var thirdCancellationTokenSource = new CancellationTokenSource();
            var count = 0;
            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> {
                new ProxyCancellationChangeToken(firstCancellationTokenSource.Token, disposing: () => count++),
                new ProxyCancellationChangeToken(secondCancellationTokenSource.Token, disposing: () => count++),
                new ProxyCancellationChangeToken(thirdCancellationTokenSource.Token, disposing: () => count++) });

            // Act
            firstCancellationTokenSource.Cancel();
            compositeChangeToken.RegisterChangeCallback(_ => { }, null);
            secondCancellationTokenSource.Cancel();

            // Assert
            Assert.Equal(1, count);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task NoDeadlock_WhenMultipleConcurrentChangeEventsOccur()
        {
            // Arrange
            var firstCancellationTokenSource = new CancellationTokenSource();
            var secondCancellationTokenSource = new CancellationTokenSource();
            var firstCancellationChangeToken = new CancellationChangeToken(firstCancellationTokenSource.Token);
            var secondCancellationChangeToken = new CancellationChangeToken(secondCancellationTokenSource.Token);
            var compositeChangeToken = new CompositeChangeToken(new[] { firstCancellationChangeToken, secondCancellationChangeToken });

            var manualResetEvent = new ManualResetEvent(false);
            compositeChangeToken.RegisterChangeCallback(_ => manualResetEvent.WaitOne(5000), null);

            // Act & Assert
            var firstChange = Task.Run(firstCancellationTokenSource.Cancel);
            var secondChange = Task.Run(secondCancellationTokenSource.Cancel);
            await Task.Delay(50);
            manualResetEvent.Set();

            await Task.WhenAll(firstChange, secondChange).WaitAsync(5000);
        }
    }

    internal class ProxyCancellationChangeToken : IChangeToken
    {
        private readonly CancellationChangeToken _cancellationChangeToken;
        private readonly Action _disposing;

        public ProxyCancellationChangeToken(CancellationToken cancellationToken, Action disposing)
        {
            _cancellationChangeToken = new CancellationChangeToken(cancellationToken);
            _disposing = disposing;
        }
        public bool ActiveChangeCallbacks => _cancellationChangeToken.ActiveChangeCallbacks;

        public bool HasChanged => _cancellationChangeToken.HasChanged;

        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
        {
            IDisposable registration = _cancellationChangeToken.RegisterChangeCallback(callback, state);
            return new Registration(_disposing, registration);
        }

        private class Registration : IDisposable
        {
            private readonly Action _disposing;
            private readonly IDisposable _registration;

            public Registration(Action disposing, IDisposable registration)
            {
                _disposing = disposing;
                _registration = registration;
            }

            public void Dispose()
            {
                _registration?.Dispose();
                _disposing();
            }
        }
    }
}
