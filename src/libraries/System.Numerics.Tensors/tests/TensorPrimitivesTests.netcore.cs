// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.Numerics.Tensors.Tests
{
    public static partial class TensorPrimitivesTests
    {
        [Theory]
        [InlineData(0)]
        [MemberData(nameof(TensorLengths))]
        public static void ConvertToHalf(int tensorLength)
        {
            using BoundedMemory<float> source = CreateAndFillTensor(tensorLength);
            foreach (int destLength in new[] { source.Length, source.Length + 1 })
            {
                Half[] destination = new Half[destLength];

                TensorPrimitives.ConvertToHalf(source, destination);

                for (int i = 0; i < source.Length; i++)
                {
                    Assert.Equal((Half)source[i], destination[i]);
                }

                if (destination.Length > source.Length)
                {
                    for (int i = source.Length; i < destination.Length; i++)
                    {
                        Assert.Equal(Half.Zero, destination[i]);
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void ConvertToHalf_ThrowsForTooShortDestination(int tensorLength)
        {
            using BoundedMemory<float> source = CreateAndFillTensor(tensorLength);
            Half[] destination = new Half[source.Length - 1];

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.ConvertToHalf(source, destination));
        }

        [Theory]
        [InlineData(0)]
        [MemberData(nameof(TensorLengths))]
        public static void ConvertToSingle(int tensorLength)
        {
            Half[] source = new Half[tensorLength];
            for (int i = 0; i < source.Length; i++)
            {
                source[i] = (Half)s_random.NextSingle();
            }

            foreach (int destLength in new[] { source.Length, source.Length + 1 })
            {
                using BoundedMemory<float> destination = CreateTensor(destLength);
                destination.Span.Fill(0f);

                TensorPrimitives.ConvertToSingle(source, destination);

                for (int i = 0; i < source.Length; i++)
                {
                    Assert.Equal((float)source[i], destination[i]);
                }

                if (destination.Length > source.Length)
                {
                    for (int i = source.Length; i < destination.Length; i++)
                    {
                        Assert.Equal(0f, destination[i]);
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(TensorLengths))]
        public static void ConvertToSingle_ThrowsForTooShortDestination(int tensorLength)
        {
            Half[] source = new Half[tensorLength];
            using BoundedMemory<float> destination = CreateTensor(source.Length - 1);

            AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.ConvertToSingle(source, destination));
        }
    }
}
