// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class ConstructorTests_String : ConstructorTests
    {
        public ConstructorTests_String()
            : base(JsonSerializerWrapper.StringSerializer)
        { }
    }

    public class ConstructorTests_AsyncStream : ConstructorTests
    {
        public ConstructorTests_AsyncStream()
            : base(JsonSerializerWrapper.AsyncStreamSerializer) { }
    }

    public class ConstructorTests_SyncStream : ConstructorTests
    {
        public ConstructorTests_SyncStream()
            : base(JsonSerializerWrapper.SyncStreamSerializer) { }
    }

    public class ConstructorTests_Span : ConstructorTests
    {
        public ConstructorTests_Span()
            : base(JsonSerializerWrapper.SpanSerializer) { }
    }

    public class ConstructorTests_Pipe : ConstructorTests
    {
        public ConstructorTests_Pipe()
            : base(JsonSerializerWrapper.AsyncPipeSerializer) { }
    }

    public class Runtime_137797
    {
        [Fact]
        public static void MixedOutParameter_SmallConstructor()
        {
            Small result = JsonSerializer.Deserialize<Small>("""{"A":1,"B":"two"}""");

            Assert.Equal(1, result.A);
            Assert.Equal("two", result.B);
        }

#if NET
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void MixedOutParameter_DynamicCodeUnsupported()
        {
            var options = new RemoteInvokeOptions
            {
                RuntimeConfigurationOptions =
                {
                    ["System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported"] = false
                }
            };

            RemoteExecutor.Invoke(static () =>
            {
                Assert.False(RuntimeFeature.IsDynamicCodeSupported);

                Large large = JsonSerializer.Deserialize<Large>("""{"A":1,"B":"two","C":3,"D":4,"E":5}""");
                Assert.Equal(1, large.A);
                Assert.Equal("two", large.B);
                Assert.Equal(3, large.C);
                Assert.Equal(4, large.D);
                Assert.Equal(5, large.E);

                Small small = JsonSerializer.Deserialize<Small>("""{"A":1,"B":"two"}""");
                Assert.Equal(1, small.A);
                Assert.Equal("two", small.B);
            }, options).Dispose();
        }
#endif

        public sealed class Small
        {
            [JsonConstructor]
            public Small(out int ignored, int a, string b)
            {
                ignored = 0;
                A = a;
                B = b;
            }

            public int A { get; }
            public string B { get; }
        }

        public sealed class Large
        {
            [JsonConstructor]
            public Large(int a, out int ignored, string b, int c, int d, int e)
            {
                ignored = 0;
                A = a;
                B = b;
                C = c;
                D = d;
                E = e;
            }

            public int A { get; }
            public string B { get; }
            public int C { get; }
            public int D { get; }
            public int E { get; }
        }
    }
}
