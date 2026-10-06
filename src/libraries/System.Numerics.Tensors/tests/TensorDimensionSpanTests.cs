// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Numerics.Tensors.Tests
{
    public class TensorDimensionSpanTests
    {
        [Theory]
        [InlineData(0, 2, new int[] { 3, 4 })]
        [InlineData(1, 4, new int[] { 4 })]
        public void TensorDimensionSpan_ConvertsToReadOnlyView(int dimension, int expectedLength, int[] expectedSlice)
        {
            Tensor<int> tensor = Tensor.Create([1, 2, 3, 4], [2, 2]);
            TensorDimensionSpan<int> mutableView = tensor.GetDimensionSpan(dimension);
            ReadOnlyTensorDimensionSpan<int> readonlyView = mutableView;

            Assert.Equal(expectedLength, readonlyView.Length);
            ReadOnlyTensorSpan<int> slice = readonlyView[readonlyView.Length - 1];
            int[] actualSlice = new int[slice.FlattenedLength];
            slice.FlattenTo(actualSlice);
            Assert.Equal(expectedSlice, actualSlice);
        }

        [Fact]
        public void TensorAuditDimensionSpanAllowsEmptyRows()
        {
            Tensor<int> tensor = Tensor.CreateFromShape<int>([2, 0]);
            TensorDimensionSpan<int> rows = tensor.GetDimensionSpan(0);
            ReadOnlyTensorDimensionSpan<int> readOnlyRows = tensor.AsReadOnlyTensorSpan().GetDimensionSpan(0);

            Assert.Equal(2, rows.Length);
            Assert.Equal([0], rows[0].Lengths);
            Assert.Equal([0], rows[1].Lengths);
            Assert.Equal([0], readOnlyRows[0].Lengths);
            Assert.Equal([0], readOnlyRows[1].Lengths);
        }

        [Fact]
        public void TensorAuditDimensionSpanRejectsOverflowedSliceCount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(static () =>
            {
                TensorSpan<int> tensor = new TensorSpan<int>(Array.Empty<int>(), [nint.MaxValue, 2, 0]);
                _ = tensor.GetDimensionSpan(1);
            });
            Assert.Throws<ArgumentOutOfRangeException>(static () =>
            {
                ReadOnlyTensorSpan<int> tensor = new ReadOnlyTensorSpan<int>(Array.Empty<int>(), [nint.MaxValue, 2, 0]);
                _ = tensor.GetDimensionSpan(1);
            });

            TensorSpan<int> empty = new TensorSpan<int>(Array.Empty<int>(), [nint.MaxValue, 2, 0]);
            Assert.Equal(0, empty.GetDimensionSpan(2).Length);
            Assert.Equal(0, ((ReadOnlyTensorSpan<int>)empty).GetDimensionSpan(2).Length);
        }

        [Fact]
        public void TensorDimensionSpan_GetDimension_ValidDimension_ReturnsCorrectView()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });

            // Act
            var dimensionView = tensor.GetDimensionSpan(1);

            // Assert
            Assert.Equal(4, dimensionView.Length);
            int[] sliceData = new int[dimensionView[0].FlattenedLength];
            dimensionView[0].FlattenTo(sliceData);
            Assert.Equal([1], dimensionView[0].Lengths.ToArray());
            Assert.Equal([1], sliceData);

            dimensionView[1].FlattenTo(sliceData);
            Assert.Equal([1], dimensionView[1].Lengths.ToArray());
            Assert.Equal([2], sliceData);

            dimensionView[2].FlattenTo(sliceData);
            Assert.Equal([1], dimensionView[2].Lengths.ToArray());
            Assert.Equal([3], sliceData);

            dimensionView[3].FlattenTo(sliceData);
            Assert.Equal([1], dimensionView[3].Lengths.ToArray());
            Assert.Equal([4], sliceData);

            // Act
            dimensionView = tensor.GetDimensionSpan(0);

            // Assert
            Assert.Equal(2, dimensionView.Length);
            sliceData = new int[dimensionView[0].FlattenedLength];
            dimensionView[0].FlattenTo(sliceData);
            Assert.Equal([2], dimensionView[0].Lengths.ToArray());
            Assert.Equal([1, 2], sliceData);

            dimensionView[1].FlattenTo(sliceData);
            Assert.Equal([2], dimensionView[1].Lengths.ToArray());
            Assert.Equal([3, 4], sliceData);

            // check tensor with 1 dimension
            tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 4 });

            // Act
            dimensionView = tensor.GetDimensionSpan(0);

            // Assert
            Assert.Equal(4, dimensionView.Length);
            sliceData = new int[dimensionView[0].FlattenedLength];
            dimensionView[0].FlattenTo(sliceData);
            Assert.Equal([1], dimensionView[0].Lengths.ToArray());
            Assert.Equal([1], sliceData);

            dimensionView[1].FlattenTo(sliceData);
            Assert.Equal([1], dimensionView[1].Lengths.ToArray());
            Assert.Equal([2], sliceData);

            dimensionView[2].FlattenTo(sliceData);
            Assert.Equal([1], dimensionView[2].Lengths.ToArray());
            Assert.Equal([3], sliceData);

            dimensionView[3].FlattenTo(sliceData);
            Assert.Equal([1], dimensionView[3].Lengths.ToArray());
            Assert.Equal([4], sliceData);

            // check tensor with 3 dimensions
            tensor = Tensor.Create(new int[] { 0, 1, 2, 3, 4, 5, 6, 7 }, new nint[] { 2, 2, 2 });

            // Act
            dimensionView = tensor.GetDimensionSpan(0);

            // Assert
            Assert.Equal(2, dimensionView.Length);
            sliceData = new int[dimensionView[0].FlattenedLength];
            dimensionView[0].FlattenTo(sliceData);
            Assert.Equal([2, 2], dimensionView[0].Lengths.ToArray());
            Assert.Equal([0, 1, 2, 3], sliceData);

            // Assert
            Assert.Equal(2, dimensionView.Length);
            sliceData = new int[dimensionView[1].FlattenedLength];
            dimensionView[1].FlattenTo(sliceData);
            Assert.Equal([2, 2], dimensionView[0].Lengths.ToArray());
            Assert.Equal([4, 5, 6, 7], sliceData);

            // Act
            dimensionView = tensor.GetDimensionSpan(1);

            // Assert
            Assert.Equal(4, dimensionView.Length);
            for (int i = 0; i < dimensionView.Length; i++)
            {
                TensorSpan<int> slice = dimensionView[i];
                sliceData = new int[slice.FlattenedLength];
                slice.FlattenTo(sliceData);
                Assert.Equal([2], slice.Lengths.ToArray());
                Assert.Equal([(i * 2), (i * 2) + 1], sliceData);
            }

            // Act
            dimensionView = tensor.GetDimensionSpan(2);

            // Assert
            Assert.Equal(8, dimensionView.Length);
            for (int i = 0; i < 8; i++)
            {
                TensorSpan<int> slice = dimensionView[i];
                sliceData = new int[slice.FlattenedLength];
                slice.FlattenTo(sliceData);
                Assert.Equal([1], slice.Lengths.ToArray());
                Assert.Equal([i], sliceData);
            }
        }

        [Fact]
        public void TensorDimensionSpan_GetDimension_InvalidDimension_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => tensor.GetDimensionSpan(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => tensor.GetDimensionSpan(2));

            // Arrange
            tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 4 });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => tensor.GetDimensionSpan(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => tensor.GetDimensionSpan(1));
        }

        [Fact]
        public void TensorDimensionSpan_GetDimension_GetSlice_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = tensor.GetDimensionSpan(1)[-1]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = tensor.GetDimensionSpan(1)[4]; });
        }

        [Fact]
        public void ReadOnlyTensorDimensionSpan_GetDimension_ValidDimension_ReturnsCorrectView()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });

            // Act
            var dimensionView = tensor.AsReadOnlyTensorSpan().GetDimensionSpan(1);

            // Assert
            Assert.Equal(4, dimensionView.Length);
            int[] sliceData = new int[dimensionView[0].FlattenedLength];
            dimensionView[0].FlattenTo(sliceData);
            Assert.Equal([1], dimensionView[0].Lengths.ToArray());
            Assert.Equal([1], sliceData);

            dimensionView[1].FlattenTo(sliceData);
            Assert.Equal([1], dimensionView[1].Lengths.ToArray());
            Assert.Equal([2], sliceData);

            dimensionView[2].FlattenTo(sliceData);
            Assert.Equal([1], dimensionView[2].Lengths.ToArray());
            Assert.Equal([3], sliceData);

            dimensionView[3].FlattenTo(sliceData);
            Assert.Equal([1], dimensionView[3].Lengths.ToArray());
            Assert.Equal([4], sliceData);

            // Act
            dimensionView = tensor.AsReadOnlyTensorSpan().GetDimensionSpan(0);

            // Assert
            Assert.Equal(2, dimensionView.Length);
            sliceData = new int[dimensionView[0].FlattenedLength];
            dimensionView[0].FlattenTo(sliceData);
            Assert.Equal([2], dimensionView[0].Lengths.ToArray());
            Assert.Equal([1, 2], sliceData);

            dimensionView[1].FlattenTo(sliceData);
            Assert.Equal([2], dimensionView[1].Lengths.ToArray());
            Assert.Equal([3, 4], sliceData);

            // check tensor with 1 dimension
            tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 4 });

            // Act
            dimensionView = tensor.AsReadOnlyTensorSpan().GetDimensionSpan(0);

            // Assert
            Assert.Equal(4, dimensionView.Length);
            sliceData = new int[dimensionView[0].FlattenedLength];
            dimensionView[0].FlattenTo(sliceData);
            Assert.Equal([1], dimensionView[0].Lengths.ToArray());
            Assert.Equal([1], sliceData);

            dimensionView[1].FlattenTo(sliceData);
            Assert.Equal([1], dimensionView[1].Lengths.ToArray());
            Assert.Equal([2], sliceData);

            dimensionView[2].FlattenTo(sliceData);
            Assert.Equal([1], dimensionView[2].Lengths.ToArray());
            Assert.Equal([3], sliceData);

            dimensionView[3].FlattenTo(sliceData);
            Assert.Equal([1], dimensionView[3].Lengths.ToArray());
            Assert.Equal([4], sliceData);

            // check tensor with 3 dimensions
            tensor = Tensor.Create(new int[] { 0, 1, 2, 3, 4, 5, 6, 7 }, new nint[] { 2, 2, 2 });

            // Act
            dimensionView = tensor.AsReadOnlyTensorSpan().GetDimensionSpan(0);

            // Assert
            Assert.Equal(2, dimensionView.Length);
            sliceData = new int[dimensionView[0].FlattenedLength];
            dimensionView[0].FlattenTo(sliceData);
            Assert.Equal([2, 2], dimensionView[0].Lengths.ToArray());
            Assert.Equal([0, 1, 2, 3], sliceData);

            // Assert
            Assert.Equal(2, dimensionView.Length);
            sliceData = new int[dimensionView[1].FlattenedLength];
            dimensionView[1].FlattenTo(sliceData);
            Assert.Equal([2, 2], dimensionView[0].Lengths.ToArray());
            Assert.Equal([4, 5, 6, 7], sliceData);

            // Act
            dimensionView = tensor.AsReadOnlyTensorSpan().GetDimensionSpan(1);

            // Assert
            Assert.Equal(4, dimensionView.Length);
            for (int i = 0; i < dimensionView.Length; i++)
            {
                ReadOnlyTensorSpan<int> slice = dimensionView[i];
                sliceData = new int[slice.FlattenedLength];
                slice.FlattenTo(sliceData);
                Assert.Equal([2], slice.Lengths.ToArray());
                Assert.Equal([(i * 2), (i * 2) + 1], sliceData);
            }

            // Act
            dimensionView = tensor.AsReadOnlyTensorSpan().GetDimensionSpan(2);

            // Assert
            Assert.Equal(8, dimensionView.Length);
            for (int i = 0; i < 8; i++)
            {
                ReadOnlyTensorSpan<int> slice = dimensionView[i];
                sliceData = new int[slice.FlattenedLength];
                slice.FlattenTo(sliceData);
                Assert.Equal([1], slice.Lengths.ToArray());
                Assert.Equal([i], sliceData);
            }
        }

        [Fact]
        public void ReadOnlyTensorDimensionSpan_GetDimension_InvalidDimension_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => tensor.AsReadOnlyTensorSpan().GetDimensionSpan(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => tensor.AsReadOnlyTensorSpan().GetDimensionSpan(2));

            // Arrange
            tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 4 });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => tensor.AsReadOnlyTensorSpan().GetDimensionSpan(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => tensor.AsReadOnlyTensorSpan().GetDimensionSpan(1));
        }

        [Fact]
        public void ReadOnlyTensorDimensionSpan_GetDimension_GetSlice_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = tensor.AsReadOnlyTensorSpan().GetDimensionSpan(1)[-1]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { _ = tensor.AsReadOnlyTensorSpan().GetDimensionSpan(1)[4]; });
        }

        [Fact]
        public void TensorDimensionSpan_Enumerator_EnumeratesCorrectly()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });
            var dimensionView = tensor.GetDimensionSpan(1);
            
            // Act & Assert
            int index = 0;
            int[][] expectedSlices = new int[][] { new int[] { 1 }, new int[] { 2 }, new int[] { 3 }, new int[] { 4 } };
            
            foreach (var slice in dimensionView)
            {
                int[] sliceData = new int[slice.FlattenedLength];
                slice.FlattenTo(sliceData);
                Assert.Equal(expectedSlices[index], sliceData);
                index++;
            }
            
            Assert.Equal(4, index); // Verify we got all slices
            
            // Test enumeration with explicit enumerator
            index = 0;
            var enumerator = dimensionView.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var slice = enumerator.Current;
                int[] sliceData = new int[slice.FlattenedLength];
                slice.FlattenTo(sliceData);
                Assert.Equal(expectedSlices[index], sliceData);
                index++;
            }
            
            Assert.Equal(4, index);
        }
        
        [Fact]
        public void ReadOnlyTensorDimensionSpan_Enumerator_EnumeratesCorrectly()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });
            var dimensionView = tensor.AsReadOnlyTensorSpan().GetDimensionSpan(1);
            
            // Act & Assert
            int index = 0;
            int[][] expectedSlices = new int[][] { new int[] { 1 }, new int[] { 2 }, new int[] { 3 }, new int[] { 4 } };
            
            foreach (var slice in dimensionView)
            {
                int[] sliceData = new int[slice.FlattenedLength];
                slice.FlattenTo(sliceData);
                Assert.Equal(expectedSlices[index], sliceData);
                index++;
            }
            
            Assert.Equal(4, index); // Verify we got all slices
            
            // Test enumeration with explicit enumerator
            index = 0;
            var enumerator = dimensionView.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var slice = enumerator.Current;
                int[] sliceData = new int[slice.FlattenedLength];
                slice.FlattenTo(sliceData);
                Assert.Equal(expectedSlices[index], sliceData);
                index++;
            }
            
            Assert.Equal(4, index);
        }
        
        [Fact]
        public void TensorDimensionSpan_Enumerator_MultidimensionalTensor()
        {
            // Arrange - using a 3D tensor
            var tensor = Tensor.Create(new int[] { 0, 1, 2, 3, 4, 5, 6, 7 }, new nint[] { 2, 2, 2 });
            var dimensionView = tensor.GetDimensionSpan(0);
            
            // Act & Assert
            int index = 0;
            int[][] expectedSlices = new int[][] { new int[] { 0, 1, 2, 3 }, new int[] { 4, 5, 6, 7 } };
            
            foreach (var slice in dimensionView)
            {
                int[] sliceData = new int[slice.FlattenedLength];
                slice.FlattenTo(sliceData);
                Assert.Equal(expectedSlices[index], sliceData);
                index++;
            }
            
            Assert.Equal(2, index); // Verify we got all slices
        }
    }
}
