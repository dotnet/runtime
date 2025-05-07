using Xunit;

namespace System.Numerics.Tensors.Tests
{
    public class TensorGetDimensionTests
    {
        [Fact]
        public void GetDimension_ValidDimension_ReturnsCorrectView()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });

            // Act
            var dimensionView = tensor.GetDimension(1);

            // Assert
            Assert.Equal(4, dimensionView.Count);
            int[] slice = new int[dimensionView.GetSlice(0).FlattenedLength];
            dimensionView.GetSlice(0).FlattenTo(slice);
            Assert.Equal([1], dimensionView.GetSlice(0).Lengths.ToArray());
            Assert.Equal([1], slice);

            dimensionView.GetSlice(1).FlattenTo(slice);
            Assert.Equal([1], dimensionView.GetSlice(1).Lengths.ToArray());
            Assert.Equal([2], slice);

            dimensionView.GetSlice(2).FlattenTo(slice);
            Assert.Equal([1], dimensionView.GetSlice(2).Lengths.ToArray());
            Assert.Equal([3], slice);

            dimensionView.GetSlice(3).FlattenTo(slice);
            Assert.Equal([1], dimensionView.GetSlice(3).Lengths.ToArray());
            Assert.Equal([4], slice);

            // Act
            dimensionView = tensor.GetDimension(0);

            // Assert
            Assert.Equal(2, dimensionView.Count);
            slice = new int[dimensionView.GetSlice(0).FlattenedLength];
            dimensionView.GetSlice(0).FlattenTo(slice);
            Assert.Equal([2], dimensionView.GetSlice(0).Lengths.ToArray());
            Assert.Equal([1, 2], slice);

            dimensionView.GetSlice(1).FlattenTo(slice);
            Assert.Equal([2], dimensionView.GetSlice(1).Lengths.ToArray());
            Assert.Equal([3, 4], slice);
        }

        [Fact]
        public void GetDimension_InvalidDimension_ThrowsArgumentException()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });

            // Act & Assert
            Assert.Throws<ArgumentException>(() => tensor.GetDimension(-1));
            Assert.Throws<ArgumentException>(() => tensor.GetDimension(3));
        }

        [Fact]
        public void GetSlice_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var tensor = Tensor.Create(new int[] { 1, 2, 3, 4 }, new nint[] { 2, 2 });

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => tensor.GetDimension(1).GetSlice(-1));
            Assert.Throws<IndexOutOfRangeException>(() => tensor.GetDimension(1).GetSlice(4));
        }
    }
}
