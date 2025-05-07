// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable 1998

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2Struct
{
    [Fact]
    public static void TestEntryPoint()
    {
        Async().Wait();
        Async2().Wait();
    }

    private static async Task Async()
    {
        S s = new S(100);
        await s.Test();
        AssertEqual(100, s.Value);
    }

    private static async2 Task Async2()
    {
        S s = new S(100);
        await s.Test();
        AssertEqual(100, s.Value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertEqual(int expected, int val)
    {
        Assert.Equal(expected, val);
    }

    private struct S
    {
        public int Value;

        public S(int value) => Value = value;

        public async2 Task Test()
        {
            // TODO: C# compiler is expected to do this, but not in the prototype.
            S @this = this;

            AssertEqual(100, @this.Value);
            @this.Value++;
            await @this.InstanceCall();
            AssertEqual(101, @this.Value);

            await @this.TaskButNotAsync();
            AssertEqual(102, @this.Value);
        }

        private async2 Task InstanceCall()
        {
            // TODO: C# compiler is expected to do this, but not in the prototype.
            S @this = this;

            AssertEqual(101, @this.Value);
            @this.Value++;
            AssertEqual(102, @this.Value);
            await Task.Yield();
            AssertEqual(102, @this.Value);
        }

        private Task TaskButNotAsync()
        {
            AssertEqual(101, Value);
            Value++;
            return Task.CompletedTask;
        }
    }
}
