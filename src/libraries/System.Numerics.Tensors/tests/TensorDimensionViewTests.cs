using Xunit;

namespace System.Numerics.Tensors.Tests
{
    public class TensorGetDimensionTests
    {
        [Fact]
        public void TensorDimensionView_GetDimension_ValidDimension_ReturnsCorrectView()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });

            // Act
            var dimensionView = tensor.GetDimension(1);

            // Assert
            Assert.Equal(4, dimensionView.Count);
            int[] sliceData = new int[dimensionView.GetSlice(0).FlattenedLength];
            dimensionView.GetSlice(0).FlattenTo(sliceData);
            Assert.Equal([1], dimensionView.GetSlice(0).Lengths.ToArray());
            Assert.Equal([1], sliceData);

            dimensionView.GetSlice(1).FlattenTo(sliceData);
            Assert.Equal([1], dimensionView.GetSlice(1).Lengths.ToArray());
            Assert.Equal([2], sliceData);

            dimensionView.GetSlice(2).FlattenTo(sliceData);
            Assert.Equal([1], dimensionView.GetSlice(2).Lengths.ToArray());
            Assert.Equal([3], sliceData);

            dimensionView.GetSlice(3).FlattenTo(sliceData);
            Assert.Equal([1], dimensionView.GetSlice(3).Lengths.ToArray());
            Assert.Equal([4], sliceData);

            // Act
            dimensionView = tensor.GetDimension(0);

            // Assert
            Assert.Equal(2, dimensionView.Count);
            sliceData = new int[dimensionView.GetSlice(0).FlattenedLength];
            dimensionView.GetSlice(0).FlattenTo(sliceData);
            Assert.Equal([2], dimensionView.GetSlice(0).Lengths.ToArray());
            Assert.Equal([1, 2], sliceData);

            dimensionView.GetSlice(1).FlattenTo(sliceData);
            Assert.Equal([2], dimensionView.GetSlice(1).Lengths.ToArray());
            Assert.Equal([3, 4], sliceData);

            // check tensor with 1 dimension
            tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 4 });

            // Act
            dimensionView = tensor.GetDimension(1);

            // Assert
            Assert.Equal(4, dimensionView.Count);
            sliceData = new int[dimensionView.GetSlice(0).FlattenedLength];
            dimensionView.GetSlice(0).FlattenTo(sliceData);
            Assert.Equal([1], dimensionView.GetSlice(0).Lengths.ToArray());
            Assert.Equal([1], sliceData);

            dimensionView.GetSlice(1).FlattenTo(sliceData);
            Assert.Equal([1], dimensionView.GetSlice(1).Lengths.ToArray());
            Assert.Equal([2], sliceData);

            dimensionView.GetSlice(2).FlattenTo(sliceData);
            Assert.Equal([1], dimensionView.GetSlice(2).Lengths.ToArray());
            Assert.Equal([3], sliceData);

            dimensionView.GetSlice(3).FlattenTo(sliceData);
            Assert.Equal([1], dimensionView.GetSlice(3).Lengths.ToArray());
            Assert.Equal([4], sliceData);

            // check tensor with 3 dimensions
            tensor = Tensor.Create(new int[] { 0, 1, 2, 3, 4, 5, 6, 7 }, new nint[] { 2, 2, 2 });

            // Act
            dimensionView = tensor.GetDimension(0);

            // Assert
            Assert.Equal(2, dimensionView.Count);
            sliceData = new int[dimensionView.GetSlice(0).FlattenedLength];
            dimensionView.GetSlice(0).FlattenTo(sliceData);
            Assert.Equal([2, 2], dimensionView.GetSlice(0).Lengths.ToArray());
            Assert.Equal([0, 1, 2, 3], sliceData);

            // Assert
            Assert.Equal(2, dimensionView.Count);
            sliceData = new int[dimensionView.GetSlice(1).FlattenedLength];
            dimensionView.GetSlice(1).FlattenTo(sliceData);
            Assert.Equal([2, 2], dimensionView.GetSlice(0).Lengths.ToArray());
            Assert.Equal([4, 5, 6, 7], sliceData);

            // Act
            dimensionView = tensor.GetDimension(1);

            // Assert
            Assert.Equal(4, dimensionView.Count);
            for (int i = 0; i < dimensionView.Count; i+=2)
            {
                TensorSpan<int> slice = dimensionView.GetSlice(i);
                sliceData = new int[slice.FlattenedLength];
                slice.FlattenTo(sliceData);
                Assert.Equal([2], slice.Lengths.ToArray());
                Assert.Equal([i, i + 1], sliceData);
            }

            // Act
            dimensionView = tensor.GetDimension(2);

            // Assert
            Assert.Equal(8, dimensionView.Count);
            for (int i = 0; i < 8; i++)
            {
                TensorSpan<int> slice = dimensionView.GetSlice(i);
                sliceData = new int[slice.FlattenedLength];
                slice.FlattenTo(sliceData);
                Assert.Equal([1], slice.Lengths.ToArray());
                Assert.Equal([i], sliceData);
            }
        }

        [Fact]
        public void TensorDimensionView_GetDimension_InvalidDimension_ThrowsArgumentException()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });

            // Act & Assert
            Assert.Throws<ArgumentException>(() => tensor.GetDimension(-1));
            Assert.Throws<ArgumentException>(() => tensor.GetDimension(2));

            // Arrange
            tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 4 });

            // Act & Assert
            Assert.Throws<ArgumentException>(() => tensor.GetDimension(-1));
            Assert.Throws<ArgumentException>(() => tensor.GetDimension(1));
        }

        [Fact]
        public void TensorDimensionView_GetDimension_GetSlice_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => tensor.GetDimension(1).GetSlice(-1));
            Assert.Throws<IndexOutOfRangeException>(() => tensor.GetDimension(1).GetSlice(4));
        }

        [Fact]
        public void ReadOnlyTensorDimensionView_GetDimension_ValidDimension_ReturnsCorrectView()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });

            // Act
            var dimensionView = tensor.AsReadOnlyTensorSpan().GetDimension(1);

            // Assert
            Assert.Equal(4, dimensionView.Count);
            int[] sliceData = new int[dimensionView.GetSlice(0).FlattenedLength];
            dimensionView.GetSlice(0).FlattenTo(sliceData);
            Assert.Equal([1], dimensionView.GetSlice(0).Lengths.ToArray());
            Assert.Equal([1], sliceData);

            dimensionView.GetSlice(1).FlattenTo(sliceData);
            Assert.Equal([1], dimensionView.GetSlice(1).Lengths.ToArray());
            Assert.Equal([2], sliceData);

            dimensionView.GetSlice(2).FlattenTo(sliceData);
            Assert.Equal([1], dimensionView.GetSlice(2).Lengths.ToArray());
            Assert.Equal([3], sliceData);

            dimensionView.GetSlice(3).FlattenTo(sliceData);
            Assert.Equal([1], dimensionView.GetSlice(3).Lengths.ToArray());
            Assert.Equal([4], sliceData);

            // Act
            dimensionView = tensor.AsReadOnlyTensorSpan().GetDimension(0);

            // Assert
            Assert.Equal(2, dimensionView.Count);
            sliceData = new int[dimensionView.GetSlice(0).FlattenedLength];
            dimensionView.GetSlice(0).FlattenTo(sliceData);
            Assert.Equal([2], dimensionView.GetSlice(0).Lengths.ToArray());
            Assert.Equal([1, 2], sliceData);

            dimensionView.GetSlice(1).FlattenTo(sliceData);
            Assert.Equal([2], dimensionView.GetSlice(1).Lengths.ToArray());
            Assert.Equal([3, 4], sliceData);

            // check tensor with 1 dimension
            tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 4 });

            // Act
            dimensionView = tensor.AsReadOnlyTensorSpan().GetDimension(1);

            // Assert
            Assert.Equal(4, dimensionView.Count);
            sliceData = new int[dimensionView.GetSlice(0).FlattenedLength];
            dimensionView.GetSlice(0).FlattenTo(sliceData);
            Assert.Equal([1], dimensionView.GetSlice(0).Lengths.ToArray());
            Assert.Equal([1], sliceData);

            dimensionView.GetSlice(1).FlattenTo(sliceData);
            Assert.Equal([1], dimensionView.GetSlice(1).Lengths.ToArray());
            Assert.Equal([2], sliceData);

            dimensionView.GetSlice(2).FlattenTo(sliceData);
            Assert.Equal([1], dimensionView.GetSlice(2).Lengths.ToArray());
            Assert.Equal([3], sliceData);

            dimensionView.GetSlice(3).FlattenTo(sliceData);
            Assert.Equal([1], dimensionView.GetSlice(3).Lengths.ToArray());
            Assert.Equal([4], sliceData);

            // check tensor with 3 dimensions
            tensor = Tensor.Create(new int[] { 0, 1, 2, 3, 4, 5, 6, 7 }, new nint[] { 2, 2, 2 });

            // Act
            dimensionView = tensor.AsReadOnlyTensorSpan().GetDimension(0);

            // Assert
            Assert.Equal(2, dimensionView.Count);
            sliceData = new int[dimensionView.GetSlice(0).FlattenedLength];
            dimensionView.GetSlice(0).FlattenTo(sliceData);
            Assert.Equal([2, 2], dimensionView.GetSlice(0).Lengths.ToArray());
            Assert.Equal([0, 1, 2, 3], sliceData);

            // Assert
            Assert.Equal(2, dimensionView.Count);
            sliceData = new int[dimensionView.GetSlice(1).FlattenedLength];
            dimensionView.GetSlice(1).FlattenTo(sliceData);
            Assert.Equal([2, 2], dimensionView.GetSlice(0).Lengths.ToArray());
            Assert.Equal([4, 5, 6, 7], sliceData);

            // Act
            dimensionView = tensor.AsReadOnlyTensorSpan().GetDimension(1);

            // Assert
            Assert.Equal(4, dimensionView.Count);
            for (int i = 0; i < dimensionView.Count; i += 2)
            {
                ReadOnlyTensorSpan<int> slice = dimensionView.GetSlice(i);
                sliceData = new int[slice.FlattenedLength];
                slice.FlattenTo(sliceData);
                Assert.Equal([2], slice.Lengths.ToArray());
                Assert.Equal([i, i + 1], sliceData);
            }

            // Act
            dimensionView = tensor.AsReadOnlyTensorSpan().GetDimension(2);

            // Assert
            Assert.Equal(8, dimensionView.Count);
            for (int i = 0; i < 8; i++)
            {
                ReadOnlyTensorSpan<int> slice = dimensionView.GetSlice(i);
                sliceData = new int[slice.FlattenedLength];
                slice.FlattenTo(sliceData);
                Assert.Equal([1], slice.Lengths.ToArray());
                Assert.Equal([i], sliceData);
            }
        }

        [Fact]
        public void ReadOnlyTensorDimensionView_GetDimension_InvalidDimension_ThrowsArgumentException()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });

            // Act & Assert
            Assert.Throws<ArgumentException>(() => tensor.AsReadOnlyTensorSpan().GetDimension(-1));
            Assert.Throws<ArgumentException>(() => tensor.AsReadOnlyTensorSpan().GetDimension(2));

            // Arrange
            tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 4 });

            // Act & Assert
            Assert.Throws<ArgumentException>(() => tensor.AsReadOnlyTensorSpan().GetDimension(-1));
            Assert.Throws<ArgumentException>(() => tensor.AsReadOnlyTensorSpan().GetDimension(1));
        }

        [Fact]
        public void ReadOnlyTensorDimensionView_GetDimension_GetSlice_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => tensor.AsReadOnlyTensorSpan().GetDimension(1).GetSlice(-1));
            Assert.Throws<IndexOutOfRangeException>(() => tensor.AsReadOnlyTensorSpan().GetDimension(1).GetSlice(4));
        }

        [Fact]
        public void TensorDimensionView_Enumerator_EnumeratesCorrectly()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });
            var dimensionView = tensor.GetDimension(1);
            
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
        public void ReadOnlyTensorDimensionView_Enumerator_EnumeratesCorrectly()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });
            var dimensionView = tensor.AsReadOnlyTensorSpan().GetDimension(1);
            
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
        public void TensorDimensionView_Enumerator_MultidimensionalTensor()
        {
            // Arrange - using a 3D tensor
            var tensor = Tensor.Create(new int[] { 0, 1, 2, 3, 4, 5, 6, 7 }, new nint[] { 2, 2, 2 });
            var dimensionView = tensor.GetDimension(0);
            
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
