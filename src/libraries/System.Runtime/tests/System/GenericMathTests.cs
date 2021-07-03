// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Tests
{
    public static class GenericMath
    {
        public static TResult Average<TSelf, TResult>(IEnumerable<TSelf> values)
            where TSelf : INumber<TSelf>
            where TResult : INumber<TResult>
        {
            TResult sum = Sum<TSelf, TResult>(values);
            return TResult.Create(sum) / TResult.Create(values.Count());
        }

        public static TResult StandardDeviation<TSelf, TResult>(IEnumerable<TSelf> values)
            where TSelf : INumber<TSelf>
            where TResult : IFloatingPoint<TResult>
        {
            TResult standardDeviation = TResult.Zero;

            if (values.Any())
            {
                TResult average = Average<TSelf, TResult>(values);
                TResult sum = Sum<TResult, TResult>(values.Select((value) => {
                    var deviation = TResult.Create(value) - average;
                    return deviation * deviation;
                }));
                standardDeviation = TResult.Sqrt(sum / TResult.Create(values.Count() - 1));
            }

            return standardDeviation;
        }

        public static TResult Sum<TSelf, TResult>(IEnumerable<TSelf> values)
            where TSelf : INumber<TSelf>
            where TResult : INumber<TResult>
        {
            TResult result = TResult.Zero;

            foreach (var value in values)
            {
                result += TResult.Create(value);
            }

            return result;
        }
    }

    public abstract class GenericMathTests<TSelf>
        where TSelf : INumber<TSelf>
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/54910", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public abstract void AverageTest();

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/54910", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public abstract void StandardDeviationTest();

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/54910", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public abstract void SumTest();

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/54910", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public abstract void SumInt32Test();
    }
}
