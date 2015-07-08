using Xunit;
using System;
using System.IO;
using System.Collections.Generic;

public class MemoryStream_TryGetBufferTests
{
    [Fact]
    public static void TryGetBuffer_Constructor_AlwaysReturnsTrue()
    {
        var stream = new MemoryStream();

        ArraySegment<byte> _;
        bool result = stream.TryGetBuffer(out _);

        Assert.True(result);
    }

    [Fact]
    public static void TryGetBuffer_Constructor_Int32_AlwaysReturnsTrue()
    {
        var stream = new MemoryStream(512);

        ArraySegment<byte> _;
        bool result = stream.TryGetBuffer(out _);

        Assert.True(result);
    }

    [Fact]
    public static void TryGetBuffer_Constructor_ByteArray_AlwaysReturnsFalse()
    {
        var stream = new MemoryStream(new byte[512]);

        ArraySegment<byte> _;
        bool result = stream.TryGetBuffer(out _);

        Assert.False(result);
    }

    [Fact]
    public static void TryGetBuffer_Constructor_ByteArray_Bool_AlwaysReturnsFalse()
    {
        var stream = new MemoryStream(new byte[512], writable: true);

        ArraySegment<byte> _;
        bool result = stream.TryGetBuffer(out _);

        Assert.False(result);
    }

    [Fact]
    public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_AlwaysReturnsFalse()
    {
        var stream = new MemoryStream(new byte[512], index: 0, count: 512);

        ArraySegment<byte> _;
        bool result = stream.TryGetBuffer(out _);

        Assert.False(result);
    }

    [Fact]
    public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_Bool_AlwaysReturnsFalse()
    {
        var stream = new MemoryStream(new byte[512], index: 0, count: 512, writable: true);

        ArraySegment<byte> _;
        bool result = stream.TryGetBuffer(out _);

        Assert.False(result);
    }

    [Fact]
    public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_Bool_Bool_FalseAsPubliclyVisible_ReturnsFalse()
    {
        var stream = new MemoryStream(new byte[512], index: 0, count: 512, writable: true, publiclyVisible: false);

        ArraySegment<byte> _;
        bool result = stream.TryGetBuffer(out _);

        Assert.False(result);
    }

    [Fact]
    public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_Bool_Bool_TrueAsPubliclyVisible_ReturnsTrue()
    {
        var stream = new MemoryStream(new byte[512], index: 0, count: 512, writable: true, publiclyVisible: true);

        ArraySegment<byte> _;
        bool result = stream.TryGetBuffer(out _);

        Assert.True(result);
    }

    [Fact]
    public static void TryGetBuffer_Constructor_ByteArray_AlwaysReturnsEmptyArraySegment()
    {
        var arrays = Inputs.GetArraysVariedBySize();

        foreach (byte[] array in arrays)
        {
            var stream = new MemoryStream(array);

            ArraySegment<byte> result;
            stream.TryGetBuffer(out result);

            // publiclyVisible = false;
            Assert.True(default(ArraySegment<byte>).Equals(result));
        }
    }

    [Fact]
    public static void TryGetBuffer_Constructor_ByteArray_Bool_AlwaysReturnsEmptyArraySegment()
    {
        var arrays = Inputs.GetArraysVariedBySize();

        foreach (byte[] array in arrays)
        {
            var stream = new MemoryStream(array, writable: true);

            ArraySegment<byte> result;
            stream.TryGetBuffer(out result);

            // publiclyVisible = false;
            Assert.True(default(ArraySegment<byte>).Equals(result));
        }
    }

    [Fact]
    public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_AlwaysReturnsEmptyArraySegment()
    {
        var arrays = Inputs.GetArraysVariedByOffsetAndLength();

        foreach (var array in arrays)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count);

            ArraySegment<byte> result;
            stream.TryGetBuffer(out result);

            // publiclyVisible = false;
            Assert.True(default(ArraySegment<byte>).Equals(result));
        }
    }

    [Fact]
    public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_Bool_AlwaysReturnsEmptyArraySegment()
    {
        var arrays = Inputs.GetArraysVariedByOffsetAndLength();

        foreach (var array in arrays)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true);

            ArraySegment<byte> result;
            stream.TryGetBuffer(out result);

            // publiclyVisible = false;
            Assert.True(default(ArraySegment<byte>).Equals(result));
        }
    }

    [Fact]
    public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_Bool_Bool_FalseAsPubliclyVisible_ReturnsEmptyArraySegment()
    {
        var arrays = Inputs.GetArraysVariedByOffsetAndLength();

        foreach (var array in arrays)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true, publiclyVisible: false);

            ArraySegment<byte> result;
            stream.TryGetBuffer(out result);

            // publiclyVisible = false;
            Assert.True(default(ArraySegment<byte>).Equals(result));
        }
    }

    [Fact]
    public static void TryGetBuffer_Constructor_AlwaysReturnsOffsetSetToZero()
    {
        var stream = new MemoryStream();

        ArraySegment<byte> result;
        stream.TryGetBuffer(out result);

        Assert.Equal(0, result.Offset);

    }

    [Fact]
    public static void TryGetBuffer_Constructor_Int32_AlwaysReturnsOffsetSetToZero()
    {
        var stream = new MemoryStream(512);

        ArraySegment<byte> result;
        stream.TryGetBuffer(out result);

        Assert.Equal(0, result.Offset);
    }

    [Fact]
    public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_Bool_Bool_ValueAsIndexAndTrueAsPubliclyVisible_AlwaysReturnsOffsetSetToIndex()
    {
        var arrays = Inputs.GetArraysVariedByOffsetAndLength();

        foreach (var array in arrays)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true, publiclyVisible: true);

            ArraySegment<byte> result;
            stream.TryGetBuffer(out result);

            Assert.Equal(array.Offset, result.Offset);
        }
    }

    [Fact]
    public static void TryGetBuffer_Constructor_ByDefaultReturnsCountSetToZero()
    {
        var stream = new MemoryStream();

        ArraySegment<byte> result;
        stream.TryGetBuffer(out result);

        Assert.Equal(0, result.Count);
    }

    [Fact]
    public static void TryGetBuffer_Constructor_ReturnsCountSetToWrittenLength()
    {
        var arrays = Inputs.GetArraysVariedBySize();
        foreach (var array in arrays)
        {
            var stream = new MemoryStream();
            stream.Write(array, 0, array.Length);

            ArraySegment<byte> result;
            stream.TryGetBuffer(out result);

            Assert.Equal(array.Length, result.Count);
        }
    }

    [Fact]
    public static void TryGetBuffer_Constructor_Int32_ByDefaultReturnsCountSetToZero()
    {
        var stream = new MemoryStream(512);

        ArraySegment<byte> result;
        stream.TryGetBuffer(out result);

        Assert.Equal(0, result.Offset);
    }

    [Fact]
    public static void TryGetBuffer_Constructor_Int32_ReturnsCountSetToWrittenLength()
    {
        var arrays = Inputs.GetArraysVariedBySize();
        foreach (var array in arrays)
        {
            var stream = new MemoryStream(512);
            stream.Write(array, 0, array.Length);

            ArraySegment<byte> result;
            stream.TryGetBuffer(out result);

            Assert.Equal(array.Length, result.Count);
        }
    }

    [Fact]
    public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_Bool_Bool_ValueAsCountAndTrueAsPubliclyVisible_AlwaysReturnsCountSetToCount()
    {
        var arrays = Inputs.GetArraysVariedByOffsetAndLength();

        foreach (var array in arrays)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true, publiclyVisible: true);

            ArraySegment<byte> result;
            stream.TryGetBuffer(out result);

            Assert.Equal(array.Count, result.Count);
        }
    }

    [Fact]
    public static void TryGetBuffer_Constructor_ReturnsArray()
    {
        var stream = new MemoryStream();

        ArraySegment<byte> result;
        stream.TryGetBuffer(out result);

        Assert.NotNull(result.Array);
    }

    [Fact]
    public static void TryGetBuffer_Constructor_MultipleCallsReturnsSameArray()
    {
        var stream = new MemoryStream();

        ArraySegment<byte> result1;
        ArraySegment<byte> result2;
        stream.TryGetBuffer(out result1);
        stream.TryGetBuffer(out result2);

        Assert.Same(result1.Array, result2.Array);
    }

    [Fact]
    public static void TryGetBuffer_Constructor_Int32_MultipleCallsReturnSameArray()
    {
        var stream = new MemoryStream(512);

        ArraySegment<byte> result1;
        ArraySegment<byte> result2;
        stream.TryGetBuffer(out result1);
        stream.TryGetBuffer(out result2);

        Assert.Same(result1.Array, result2.Array);
    }

    [Fact]
    public static void TryGetBuffer_Constructor_Int32_WhenWritingPastCapacity_ReturnsDifferentArrays()
    {
        var stream = new MemoryStream(512);

        ArraySegment<byte> result1;
        stream.TryGetBuffer(out result1);

        // Force the stream to resize the underlying array
        stream.Write(new byte[1024], 0, 1024);

        ArraySegment<byte> result2;
        stream.TryGetBuffer(out result2);

        Assert.NotSame(result1.Array, result2.Array);
    }

    [Fact]
    public static void TryGetBuffer_Constructor_ByteArray_Int32_Int32_Bool_Bool_ValueAsBufferAndTrueAsPubliclyVisible_AlwaysReturnsArraySetToBuffer()
    {
        var arrays = Inputs.GetArraysVariedByOffsetAndLength();

        foreach (var array in arrays)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true, publiclyVisible: true);

            ArraySegment<byte> result;
            stream.TryGetBuffer(out result);

            Assert.Same(array.Array, result.Array);
        }
    }

    [Fact]
    public static void TryGetBuffer_WhenDisposed_ReturnsTrue()
    {
        var arrays = Inputs.GetArraysVariedByOffsetAndLength();

        foreach (var array in arrays)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true, publiclyVisible: true);
            stream.Dispose();

            ArraySegment<byte> _;
            bool result = stream.TryGetBuffer(out _);

            Assert.True(result);
        }
    }

    [Fact]
    public static void TryGetBuffer_WhenDisposed_ReturnsOffsetSetToIndex()
    {
        var arrays = Inputs.GetArraysVariedByOffsetAndLength();

        foreach (var array in arrays)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true, publiclyVisible: true);
            stream.Dispose();

            ArraySegment<byte> result;
            stream.TryGetBuffer(out result);

            Assert.Equal(array.Offset, result.Offset);
        }
    }


    [Fact]
    public static void TryGetBuffer_WhenDisposed_ReturnsCountSetToCount()
    {
        var arrays = Inputs.GetArraysVariedByOffsetAndLength();

        foreach (var array in arrays)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true, publiclyVisible: true);
            stream.Dispose();

            ArraySegment<byte> result;
            stream.TryGetBuffer(out result);

            Assert.Equal(array.Count, result.Count);
        }
    }

    [Fact]
    public static void TryGetBuffer_WhenDisposed_ReturnsArraySetToBuffer()
    {
        var arrays = Inputs.GetArraysVariedByOffsetAndLength();

        foreach (var array in arrays)
        {
            var stream = new MemoryStream(array.Array, index: array.Offset, count: array.Count, writable: true, publiclyVisible: true);
            stream.Dispose();

            ArraySegment<byte> result;
            stream.TryGetBuffer(out result);

            Assert.Same(array.Array, result.Array);
        }
    }
}