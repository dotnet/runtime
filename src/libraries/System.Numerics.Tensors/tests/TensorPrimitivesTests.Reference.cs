// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Numerics.Tensors.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/97295", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsNotMonoInterpreter))]
    public class ReferenceTensorPrimitivesTests
    {
        // The 99% case for TensorPrimitives is working with value type Ts, and the rest of the tests are optimized for that.
        // These tests provide additional coverage for when T is a reference type.

        [Fact]
        public void HammingDistance_ValidateReferenceType()
        {
            Assert.Equal(0, TensorPrimitives.HammingDistance<string>(Array.Empty<string>(), Array.Empty<string>()));
            Assert.Equal(1, TensorPrimitives.HammingDistance(["a"], ["b"]));
            Assert.Equal(2, TensorPrimitives.HammingDistance(["a", "b", "c"], ["a", "c", "b"]));
            Assert.Equal(2, TensorPrimitives.HammingDistance(["a", "b", "c"], ["a", "c", "b"]));
            Assert.Equal(0, TensorPrimitives.HammingDistance(["a", "b", "c"], ["a", "b", "c"]));
            Assert.Throws<ArgumentException>(() => TensorPrimitives.HammingDistance(["a", "b"], ["a", "b", "c"]));
        }
    }
}
