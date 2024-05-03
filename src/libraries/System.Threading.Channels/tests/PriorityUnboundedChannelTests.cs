// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Threading.Channels.Tests
{
    public class UnboundedPrioritizedChannelTests : UnboundedChannelTests
    {
        protected override Channel<T> CreateChannel<T>() => Channel.CreateUnboundedPrioritized<T>(new() { AllowSynchronousContinuations = AllowSynchronousContinuations });

        [Fact]
        public void ItemsReadInExpectedOrder_NoComparer()
        {
            Channel<Person> c = CreateChannel<Person>();

            for (int trial = 0; trial < 2; trial++)
            {
                Assert.True(c.Writer.WriteAsync(new Person { Age = 20 }).IsCompletedSuccessfully);
                Assert.Equal(1, c.Reader.Count);

                Assert.True(c.Writer.WriteAsync(new Person { Age = 21 }).IsCompletedSuccessfully);
                Assert.Equal(2, c.Reader.Count);

                Assert.True(c.Writer.WriteAsync(new Person { Age = 19 }).IsCompletedSuccessfully);
                Assert.Equal(3, c.Reader.Count);

                Assert.True(c.Reader.TryRead(out Person p));
                Assert.Equal(19, p.Age);
                Assert.Equal(2, c.Reader.Count);

                Assert.True(c.Writer.WriteAsync(new Person { Age = 18 }).IsCompletedSuccessfully);
                Assert.Equal(3, c.Reader.Count);

                Assert.True(c.Writer.WriteAsync(new Person { Age = 22 }).IsCompletedSuccessfully);
                Assert.Equal(4, c.Reader.Count);

                Assert.True(c.Reader.TryRead(out p));
                Assert.Equal(18, p.Age);
                Assert.Equal(3, c.Reader.Count);

                Assert.True(c.Reader.TryRead(out p));
                Assert.Equal(20, p.Age);
                Assert.Equal(2, c.Reader.Count);

                Assert.True(c.Reader.TryRead(out p));
                Assert.Equal(21, p.Age);
                Assert.Equal(1, c.Reader.Count);

                Assert.True(c.Reader.TryRead(out p));
                Assert.Equal(22, p.Age);
                Assert.Equal(0, c.Reader.Count);
            }
        }

        [Fact]
        public void ItemsReadInExpectedOrder_Comparer()
        {
            Channel<Person> c = Channel.CreateUnboundedPrioritized(new UnboundedPrioritizedChannelOptions<Person> { Comparer = Comparer<Person>.Create((p1, p2) => p2.Age.CompareTo(p1.Age)) });

            for (int trial = 0; trial < 2; trial++)
            {
                Assert.True(c.Writer.WriteAsync(new Person { Age = 20 }).IsCompletedSuccessfully);
                Assert.Equal(1, c.Reader.Count);

                Assert.True(c.Writer.WriteAsync(new Person { Age = 21 }).IsCompletedSuccessfully);
                Assert.Equal(2, c.Reader.Count);

                Assert.True(c.Writer.WriteAsync(new Person { Age = 19 }).IsCompletedSuccessfully);
                Assert.Equal(3, c.Reader.Count);

                Assert.True(c.Reader.TryRead(out Person p));
                Assert.Equal(21, p.Age);
                Assert.Equal(2, c.Reader.Count);

                Assert.True(c.Writer.WriteAsync(new Person { Age = 18 }).IsCompletedSuccessfully);
                Assert.Equal(3, c.Reader.Count);

                Assert.True(c.Writer.WriteAsync(new Person { Age = 22 }).IsCompletedSuccessfully);
                Assert.Equal(4, c.Reader.Count);

                Assert.True(c.Reader.TryRead(out p));
                Assert.Equal(22, p.Age);
                Assert.Equal(3, c.Reader.Count);

                Assert.True(c.Reader.TryRead(out p));
                Assert.Equal(20, p.Age);
                Assert.Equal(2, c.Reader.Count);

                Assert.True(c.Reader.TryRead(out p));
                Assert.Equal(19, p.Age);
                Assert.Equal(1, c.Reader.Count);

                Assert.True(c.Reader.TryRead(out p));
                Assert.Equal(18, p.Age);
                Assert.Equal(0, c.Reader.Count);
            }
        }

        private struct Person : IComparable<Person>
        {
            public int Age { get; set; }

            public int CompareTo(Person other) => Age.CompareTo(other.Age);
        }
    }
}
