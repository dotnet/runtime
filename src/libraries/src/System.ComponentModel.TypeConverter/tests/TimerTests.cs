// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Threading;
using System.Timers;

using TestTimer = System.Timers.Timer;

namespace System.Timers.Tests
{
    public class TimerTests : IDisposable
    {
        private TestTimer _timer;

        [Theory]
        [InlineData(0)]
        [InlineData(-0.5D)]
        [InlineData(-1)]
        [InlineData(double.MinValue)]
        [InlineData(double.MaxValue)]
        public void NegativeTests(double interval)
        {
            Assert.Throws<ArgumentException>(() => { _timer = new TestTimer(interval); });
        }

        [Fact]
        public void TestTimerStartAutoReset()
        {
            CountdownEvent cde = new CountdownEvent(1);
            int result = 0;
            _timer = new TestTimer(100);

            // Test defaults.
            Assert.Equal(100, _timer.Interval);
            Assert.True(_timer.AutoReset);

            _timer.AutoReset = false;
            _timer.Elapsed += (sender, e) => { cde.Signal();  result = ++result; };
            _timer.Start();

            Assert.True(_timer.Enabled);
            cde.Wait();

            // Only elapsed once.
            Assert.Equal(1, result);

            cde = new CountdownEvent(10);
            _timer.AutoReset = true;

            cde.Wait();
            cde.Dispose();

            _timer.Stop();
            // Atleast elapsed 10 times.
            Assert.True(result >= 10);
        }

        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(0.5D)]
        public void TestInterval(double interval)
        {
            _timer = new TestTimer(interval);
            Assert.Equal(Math.Ceiling(interval), _timer.Interval);

            _timer.Interval = interval;
            Assert.Equal(interval, _timer.Interval);
        }

        public void Dispose()
        {
            if (_timer != null)
                _timer.Close();
        }
    }
}
