// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Xunit;

namespace System.Threading.Tasks.Channels.Tests
{
    public class ValueTaskTests
    {
        [Fact]
        public void DefaultValueTask_ValueType_DefaultValue()
        {
            Assert.True(default(ValueTask<int>).IsCompleted);
            Assert.True(default(ValueTask<int>).IsCompletedSuccessfully);
            Assert.False(default(ValueTask<int>).IsFaulted);
            Assert.False(default(ValueTask<int>).IsCanceled);
            Assert.Equal(0, default(ValueTask<int>).Result);

            Assert.True(default(ValueTask<string>).IsCompleted);
            Assert.True(default(ValueTask<string>).IsCompletedSuccessfully);
            Assert.False(default(ValueTask<string>).IsFaulted);
            Assert.False(default(ValueTask<string>).IsCanceled);
            Assert.Equal(null, default(ValueTask<string>).Result);
        }

        [Fact]
        public void CreateFromValue_IsRanToCompletion()
        {
            ValueTask<int> t = new ValueTask<int>(42);
            Assert.True(t.IsCompleted);
            Assert.True(t.IsCompletedSuccessfully);
            Assert.False(t.IsFaulted);
            Assert.False(t.IsCanceled);
            Assert.Equal(42, t.Result);
        }

        [Fact]
        public void CreateFromCompletedTask_IsRanToCompletion()
        {
            ValueTask<int> t = new ValueTask<int>(Task.FromResult(42));
            Assert.True(t.IsCompleted);
            Assert.True(t.IsCompletedSuccessfully);
            Assert.False(t.IsFaulted);
            Assert.False(t.IsCanceled);
            Assert.Equal(42, t.Result);
        }

        [Fact]
        public void CreateFromNotCompletedTask_IsNotRanToCompletion()
        {
            var tcs = new TaskCompletionSource<int>();
            ValueTask<int> t = new ValueTask<int>(tcs.Task);

            Assert.False(t.IsCompleted);
            Assert.False(t.IsCompletedSuccessfully);
            Assert.False(t.IsFaulted);
            Assert.False(t.IsCanceled);

            tcs.SetResult(42);

            Assert.Equal(42, t.Result);
            Assert.True(t.IsCompleted);
            Assert.True(t.IsCompletedSuccessfully);
            Assert.False(t.IsFaulted);
            Assert.False(t.IsCanceled);
        }

        [Fact]
        public void CreateFromNullTask_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ValueTask<int>((Task<int>)null));
            Assert.Throws<ArgumentNullException>(() => new ValueTask<string>((Task<string>)null));
        }

        [Fact]
        public void CastFromValue_IsRanToCompletion()
        {
            ValueTask<int> t = 42;

            Assert.Equal(42, t.Result);

            Assert.True(t.IsCompleted);
            Assert.True(t.IsCompletedSuccessfully);
            Assert.False(t.IsFaulted);
            Assert.False(t.IsCanceled);
        }

        [Fact]
        public void CastFromCompletedTask_IsRanToCompletion()
        {
            ValueTask<int> t = Task.FromResult(42);

            Assert.Equal(42, t.Result);

            Assert.True(t.IsCompleted);
            Assert.True(t.IsCompletedSuccessfully);
            Assert.False(t.IsFaulted);
            Assert.False(t.IsCanceled);
        }

        [Fact]
        public void CastFromFaultedTask_IsNotRanToCompletion()
        {
            ValueTask<int> t = Task.FromException<int>(new FormatException());

            Assert.Throws<FormatException>(() => t.Result);

            Assert.True(t.IsCompleted);
            Assert.False(t.IsCompletedSuccessfully);
            Assert.True(t.IsFaulted);
            Assert.False(t.IsCanceled);
        }

        [Fact]
        public void CastFromCanceledTask_IsNotRanToCompletion()
        {
            ValueTask<int> t = Task.FromCanceled<int>(new CancellationToken(true));

            Assert.Throws<TaskCanceledException>(() => t.Result);

            Assert.True(t.IsCompleted);
            Assert.False(t.IsCompletedSuccessfully);
            Assert.False(t.IsFaulted);
            Assert.True(t.IsCanceled);
        }

        [Fact]
        public void CastFromNullTask_Throws()
        {
            Task<int> ti = null;
            Assert.Throws<ArgumentNullException>(() => (ValueTask<int>)ti);

            Task<string> ts = null;
            Assert.Throws<ArgumentNullException>(() => (ValueTask<string>)ts);
        }

        [Fact]
        public void CreateFromTask_AsTaskIdempotent()
        {
            Task<int> source = Task.FromResult(42);
            ValueTask<int> t = new ValueTask<int>(source);
            Assert.Same(source, t.AsTask());
            Assert.Same(t.AsTask(), t.AsTask());
        }

        [Fact]
        public void CreateFromValue_AsTaskNotIdempotent()
        {
            ValueTask<int> t = new ValueTask<int>(42);
            Assert.NotSame(Task.FromResult(42), t.AsTask());
            Assert.NotSame(t.AsTask(), t.AsTask());
        }

        [Fact]
        public async Task CreateFromValue_Await()
        {
            ValueTask<int> t = new ValueTask<int>(42);
            Assert.Equal(42, await t);
            Assert.Equal(42, await t.ConfigureAwait(false));
            Assert.Equal(42, await t.ConfigureAwait(true));
        }

        [Fact]
        public async Task CreateFromTask_Await_Normal()
        {
            Task<int> source = Task.Delay(1).ContinueWith(_ => 42);
            ValueTask<int> t = new ValueTask<int>(source);
            Assert.Equal(42, await t);
        }

        [Fact]
        public async Task CreateFromTask_Await_ConfigureAwaitFalse()
        {
            Task<int> source = Task.Delay(1).ContinueWith(_ => 42);
            ValueTask<int> t = new ValueTask<int>(source);
            Assert.Equal(42, await t.ConfigureAwait(false));
        }

        [Fact]
        public async Task CreateFromTask_Await_ConfigureAwaitTrue()
        {
            Task<int> source = Task.Delay(1).ContinueWith(_ => 42);
            ValueTask<int> t = new ValueTask<int>(source);
            Assert.Equal(42, await t.ConfigureAwait(true));
        }

        [Fact]
        public void Awaiter_OnCompleted()
        {
            // Since ValueTask implements both OnCompleted and UnsafeOnCompleted,
            // OnCompleted typically won't be used by await, so we add an explicit test
            // for it here.

            ValueTask<int> t = 42;
            var mres = new ManualResetEventSlim();
            t.GetAwaiter().OnCompleted(() => mres.Set());
            Assert.True(mres.Wait(10000));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ConfiguredAwaiter_OnCompleted(bool continueOnCapturedContext)
        {
            // Since ValueTask implements both OnCompleted and UnsafeOnCompleted,
            // OnCompleted typically won't be used by await, so we add an explicit test
            // for it here.

            ValueTask<int> t = 42;
            var mres = new ManualResetEventSlim();
            t.ConfigureAwait(continueOnCapturedContext).GetAwaiter().OnCompleted(() => mres.Set());
            Assert.True(mres.Wait(10000));
        }

        [Fact]
        public async Task Awaiter_ContinuesOnCapturedContext()
        {
            await Task.Run(() =>
            {
                var tsc = new TrackingSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(tsc);
                try
                {
                    ValueTask<int> t = 42;
                    var mres = new ManualResetEventSlim();
                    t.GetAwaiter().OnCompleted(() => mres.Set());
                    Assert.True(mres.Wait(10000));
                    Assert.Equal(1, tsc.Posts);
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(null);
                }
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConfiguredAwaiter_ContinuesOnCapturedContext(bool continueOnCapturedContext)
        {
            await Task.Run(() =>
            {
                var tsc = new TrackingSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(tsc);
                try
                {
                    ValueTask<int> t = 42;
                    var mres = new ManualResetEventSlim();
                    t.ConfigureAwait(continueOnCapturedContext).GetAwaiter().OnCompleted(() => mres.Set());
                    Assert.True(mres.Wait(10000));
                    Assert.Equal(continueOnCapturedContext ? 1 : 0, tsc.Posts);
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(null);
                }
            });
        }

        [Fact]
        public void GetHashCode_ContainsResult()
        {
            ValueTask<int> t = 42;
            Assert.Equal(t.Result.GetHashCode(), t.GetHashCode());
        }

        [Fact]
        public void GetHashCode_ContainsTask()
        {
            ValueTask<string> t = Task.FromResult("42");
            Assert.Equal(t.AsTask().GetHashCode(), t.GetHashCode());
        }

        [Fact]
        public void GetHashCode_ContainsNull()
        {
            ValueTask<string> t = (string)null;
            Assert.Equal(0, t.GetHashCode());
        }

        [Fact]
        public void OperatorEquals()
        {
            Assert.True((ValueTask<int>)42 == (ValueTask<int>)42);
            Assert.False((ValueTask<int>)42 == (ValueTask<int>)43);

            Assert.True((ValueTask<string>)"42" == (ValueTask<string>)"42");
            Assert.True((ValueTask<string>)(string)null == (ValueTask<string>)(string)null);

            Assert.False((ValueTask<string>)"42" == (ValueTask<string>)(string)null);
            Assert.False((ValueTask<string>)(string)null == (ValueTask<string>)"42");

            Assert.False((ValueTask<int>)42 == (ValueTask<int>)Task.FromResult(42));
            Assert.False((ValueTask<int>)Task.FromResult(42) == (ValueTask<int>)42);
        }

        [Fact]
        public void OperatorNotEquals()
        {
            Assert.False((ValueTask<int>)42 != (ValueTask<int>)42);
            Assert.True((ValueTask<int>)42 != (ValueTask<int>)43);

            Assert.False((ValueTask<string>)"42" != (ValueTask<string>)"42");
            Assert.False((ValueTask<string>)(string)null != (ValueTask<string>)(string)null);

            Assert.True((ValueTask<string>)"42" != (ValueTask<string>)(string)null);
            Assert.True((ValueTask<string>)(string)null != (ValueTask<string>)"42");

            Assert.True((ValueTask<int>)42 != (ValueTask<int>)Task.FromResult(42));
            Assert.True((ValueTask<int>)Task.FromResult(42) != (ValueTask<int>)42);
        }

        [Fact]
        public void Equals_ValueTask()
        {
            Assert.True(((ValueTask<int>)42).Equals((ValueTask<int>)42));
            Assert.False(((ValueTask<int>)42).Equals((ValueTask<int>)43));

            Assert.True(((ValueTask<string>)"42").Equals((ValueTask<string>)"42"));
            Assert.True(((ValueTask<string>)(string)null).Equals((ValueTask<string>)(string)null));

            Assert.False(((ValueTask<string>)"42").Equals((ValueTask<string>)(string)null));
            Assert.False(((ValueTask<string>)(string)null).Equals((ValueTask<string>)"42"));

            Assert.False(((ValueTask<int>)42).Equals((ValueTask<int>)Task.FromResult(42)));
            Assert.False(((ValueTask<int>)Task.FromResult(42)).Equals((ValueTask<int>)42));
        }

        [Fact]
        public void Equals_Object()
        {
            Assert.True(((ValueTask<int>)42).Equals((object)(ValueTask<int>)42));
            Assert.False(((ValueTask<int>)42).Equals((object)(ValueTask<int>)43));

            Assert.True(((ValueTask<string>)"42").Equals((object)(ValueTask<string>)"42"));
            Assert.True(((ValueTask<string>)(string)null).Equals((object)(ValueTask<string>)(string)null));

            Assert.False(((ValueTask<string>)"42").Equals((object)(ValueTask<string>)(string)null));
            Assert.False(((ValueTask<string>)(string)null).Equals((object)(ValueTask<string>)"42"));

            Assert.False(((ValueTask<int>)42).Equals((object)(ValueTask<int>)Task.FromResult(42)));
            Assert.False(((ValueTask<int>)Task.FromResult(42)).Equals((object)(ValueTask<int>)42));

            Assert.False(((ValueTask<int>)42).Equals((object)null));
            Assert.False(((ValueTask<int>)42).Equals(new object()));
            Assert.False(((ValueTask<int>)42).Equals((object)42));
        }

        [Fact]
        public void ToString_Success()
        {
            Assert.Equal("Hello", new ValueTask<string>("Hello").ToString());
            Assert.Equal("Hello", new ValueTask<string>(Task.FromResult("Hello")).ToString());

            Assert.Equal("42", new ValueTask<int>(42).ToString());
            Assert.Equal("42", new ValueTask<int>(Task.FromResult(42)).ToString());

            Assert.Same(string.Empty, new ValueTask<string>(string.Empty).ToString());
            Assert.Same(string.Empty, new ValueTask<string>(Task.FromResult(string.Empty)).ToString());

            Assert.Same(string.Empty, new ValueTask<string>(Task.FromException<string>(new InvalidOperationException())).ToString());
            Assert.Same(string.Empty, new ValueTask<string>(Task.FromException<string>(new OperationCanceledException())).ToString());

            Assert.Same(string.Empty, new ValueTask<string>(Task.FromCanceled<string>(new CancellationToken(true))).ToString());

            Assert.Equal("0", default(ValueTask<int>).ToString());
            Assert.Same(string.Empty, default(ValueTask<string>).ToString());
            Assert.Same(string.Empty, new ValueTask<string>((string)null).ToString());
            Assert.Same(string.Empty, new ValueTask<string>(Task.FromResult<string>(null)).ToString());

            Assert.Same(string.Empty, new ValueTask<DateTime>(new TaskCompletionSource<DateTime>().Task).ToString());
        }

        private sealed class TrackingSynchronizationContext : SynchronizationContext
        {
            internal int Posts { get; set; }

            public override void Post(SendOrPostCallback d, object state)
            {
                Posts++;
                base.Post(d, state);
            }
        }
    }
}
