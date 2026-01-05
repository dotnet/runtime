// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Numerics.Tensors.Tests
{
    public class TensorMarshalTests
    {
        [Fact]
        public void CreateTensorSpanTest()
        {
            int[] data = Enumerable.Range(0, 10).ToArray();
            nint[] lengths = [2, 5];
            TensorSpan<int> tensorSpan = TensorMarshal.CreateTensorSpan(ref data[0], data.Length, lengths, strides: [], pinned: false);

            Assert.Equal(10, tensorSpan.FlattenedLength);
            Assert.Equal(lengths, tensorSpan.Lengths.ToArray());
            Assert.Equal([5, 1], tensorSpan.Strides.ToArray());
        }

        [Fact]
        public void CreateTensorSpanThrowsForInvalidLengthsTest()
        {
            int[] data = Enumerable.Range(0, 10).ToArray();
            nint[] lengths = [3, 5];
            Assert.Throws<ArgumentOutOfRangeException>(() => TensorMarshal.CreateTensorSpan(ref data[0], data.Length, lengths, strides: [], pinned: false));
        }

        [Fact]
        public void CreateReadOnlyTensorSpanTest()
        {
            int[] data = Enumerable.Range(0, 10).ToArray();
            nint[] lengths = [2, 5];
            ReadOnlyTensorSpan<int> tensorSpan = TensorMarshal.CreateReadOnlyTensorSpan(ref data[0], data.Length, lengths, strides: [], pinned: false);

            Assert.Equal(10, tensorSpan.FlattenedLength);
            Assert.Equal(lengths, tensorSpan.Lengths.ToArray());
            Assert.Equal([5, 1], tensorSpan.Strides.ToArray());
        }

        [Fact]
        public void CreateReadOnlyTensorSpanThrowsForInvalidLengthsTest()
        {
            int[] data = Enumerable.Range(0, 10).ToArray();
            nint[] lengths = [3, 5];
            Assert.Throws<ArgumentOutOfRangeException>(() => TensorMarshal.CreateReadOnlyTensorSpan(ref data[0], data.Length, lengths, strides: [], pinned: false));
        }

        [Fact]
        public void GetReferenceTensorSpanTest()
        {
            int[] data = new int[10];
            TensorSpan<int> tensorSpan = data;
            Assert.True(Unsafe.AreSame(ref data[0], ref TensorMarshal.GetReference(tensorSpan)));

            data = Array.Empty<int>();
            tensorSpan = data;
            Assert.True(Unsafe.AreSame(ref MemoryMarshal.GetArrayDataReference(data), ref TensorMarshal.GetReference(tensorSpan)));

            tensorSpan = TensorSpan<int>.Empty;
            Assert.True(Unsafe.IsNullRef(ref TensorMarshal.GetReference(tensorSpan)));
        }

        [Fact]
        public void GetReferenceReadOnlyTensorSpanTest()
        {
            int[] data = new int[10];
            ReadOnlyTensorSpan<int> tensorSpan = data;
            Assert.True(Unsafe.AreSame(ref data[0], in TensorMarshal.GetReference(tensorSpan)));

            data = Array.Empty<int>();
            tensorSpan = data;
            Assert.True(Unsafe.AreSame(ref MemoryMarshal.GetArrayDataReference(data), in TensorMarshal.GetReference(tensorSpan)));

            tensorSpan = TensorSpan<int>.Empty;
            Assert.True(Unsafe.IsNullRef(in TensorMarshal.GetReference(tensorSpan)));
        }
    }
}
