// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.Numerics.Tensors.Tests
{
    public class ConvertToHalfTests
    {
        private readonly Random _random = new Random(42);

        [Fact]
        public void ConvertToHalf()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<float> source = CreateAndFillSingleTensor(tensorLength);
                foreach (int destLength in new[] { source.Length, source.Length + 1 })
                {
                    using BoundedMemory<Half> destination = BoundedMemory.Allocate<Half>(destLength);
                    destination.Span.Fill(Half.Zero);

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
            });
        }

        [Fact]
        public void ConvertToHalf_SpecialValues()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<float> source = CreateAndFillSingleTensor(tensorLength);
                using BoundedMemory<Half> destination = BoundedMemory.Allocate<Half>(tensorLength);

                // NaN, infinities, and 0s
                source[_random.Next(source.Length)] = float.NaN;
                source[_random.Next(source.Length)] = float.PositiveInfinity;
                source[_random.Next(source.Length)] = float.NegativeInfinity;
                source[_random.Next(source.Length)] = 0;
                source[_random.Next(source.Length)] = float.NegativeZero;

                TensorPrimitives.ConvertToHalf(source, destination);

                for (int i = 0; i < source.Length; i++)
                {
                    Assert.Equal((Half)source[i], destination[i]);
                }
            });
        }

        [Fact]
        public void ConvertToHalf_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<float> source = CreateAndFillSingleTensor(tensorLength);
                Half[] destination = new Half[source.Length - 1];

                AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.ConvertToHalf(source, destination));
            });
        }

        [Fact]
        public void ConvertToSingle()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<Half> source = BoundedMemory.Allocate<Half>(tensorLength);
                for (int i = 0; i < source.Length; i++)
                {
                    source[i] = (Half)_random.NextSingle();
                }

                foreach (int destLength in new[] { source.Length, source.Length + 1 })
                {
                    using BoundedMemory<float> destination = CreateSingleTensor(destLength);
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
            });
        }

        [Fact]
        public void ConvertToSingle_SpecialValues()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<Half> source = BoundedMemory.Allocate<Half>(tensorLength);
                for (int i = 0; i < source.Length; i++)
                {
                    source[i] = (Half)_random.NextSingle();
                }

                using BoundedMemory<float> destination = CreateSingleTensor(tensorLength);

                // NaN, infinities, and 0s
                source[_random.Next(source.Length)] = Half.NaN;
                source[_random.Next(source.Length)] = Half.PositiveInfinity;
                source[_random.Next(source.Length)] = Half.NegativeInfinity;
                source[_random.Next(source.Length)] = Half.Zero;
                source[_random.Next(source.Length)] = Half.NegativeZero;

                TensorPrimitives.ConvertToSingle(source, destination);

                for (int i = 0; i < source.Length; i++)
                {
                    Assert.Equal((float)source[i], destination[i]);
                }
            });
        }

        [Fact]
        public void ConvertToSingle_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                Half[] source = new Half[tensorLength];
                using BoundedMemory<float> destination = CreateSingleTensor(source.Length - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.ConvertToSingle(source, destination));
            });
        }

        public BoundedMemory<float> CreateSingleTensor(int size) => BoundedMemory.Allocate<float>(size);

        public BoundedMemory<float> CreateAndFillSingleTensor(int size)
        {
            BoundedMemory<float> tensor = CreateSingleTensor(size);
            Span<float> span = tensor;
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = (float)((_random.NextDouble() * 2) - 1); // For testing purposes, get a mix of negative and positive values.
            }
            return tensor;
        }
    }
}
