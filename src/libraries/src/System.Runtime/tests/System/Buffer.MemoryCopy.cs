// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

public static class BufferMemoryCopyTests
{
    [Fact]
    public static unsafe void MemoryCopy_ValidSourceAndDestination()
    {
        int[] sourceArray = new int[25000];
        for (int g = 0; g < sourceArray.Length; g++)
        {
            sourceArray[g] = g;
        }
        int[] destinationArray = new int[30000];

        fixed (int* sourceBase = sourceArray, destinationBase = destinationArray)
        {
            Buffer.MemoryCopy(sourceBase, destinationBase, 30000 * 4, 25000 * 4);
        }

        for (int g = 0; g < sourceArray.Length; g++)
        {
            Assert.Equal(sourceArray[g], destinationArray[g]);
        }
    }

    [Fact]
    public static unsafe void MemoryCopy_ValidSourceAndDestinationNonZeroDestinationIndex()
    {
        int[] sourceArray = new int[25000];
        for (int g = 0; g < sourceArray.Length; g++)
        {
            sourceArray[g] = g;
        }
        int[] destinationArray = new int[30000];

        fixed (int* sourceBase = sourceArray, destinationBase = destinationArray)
        {
            Buffer.MemoryCopy(sourceBase, destinationBase + 5000, 30000 * 4, 25000 * 4);
        }

        for (int g = 0; g < sourceArray.Length; g++)
        {
            Assert.Equal(sourceArray[g], destinationArray[g + 5000]);
        }
    }

    [Fact]
    public static unsafe void MemoryCopy_ValidSourceAndDestinationNonZeroSourceAndDestinationIndex()
    {
        int[] sourceArray = new int[25000];
        for (int g = 0; g < sourceArray.Length; g++)
        {
            sourceArray[g] = g;
        }
        int[] destinationArray = new int[30000];

        fixed (int* sourceBase = sourceArray, destinationBase = destinationArray)
        {
            Buffer.MemoryCopy(sourceBase + 5000, destinationBase + 6000, 30000 * 4, 20000 * 4);
        }

        for (int g = 5000; g < sourceArray.Length; g++)
        {
            Assert.Equal(sourceArray[g], destinationArray[g + 1000]);
        }
    }

    [Fact]
    public static unsafe void MemoryCopy_OverlappingBuffers()
    {
        int[] array = new int[200];
        for (int g = 0; g < array.Length; g++)
        {
            array[g] = g;
        }

        fixed (int* arrayBase = array)
        {
            Buffer.MemoryCopy(arrayBase, arrayBase + 50, 200 * 4, 100 * 4);
        }

        for (int g = 0; g < 100; g++)
        {
            Assert.Equal(g, array[g + 50]);
        }
    }

    [Fact]
    public static unsafe void MemoryCopy_OverlappingBuffersSmallCopy()
    {
        int[] array = new int[200];
        for (int g = 0; g < array.Length; g++)
        {
            array[g] = g;
        }

        fixed (int* arrayBase = array)
        {
            Buffer.MemoryCopy(arrayBase, arrayBase + 5, 200 * 4, 15 * 4);
        }

        for (int g = 0; g < 15; g++)
        {
            Assert.Equal(g, array[g + 5]);
        }
    }

    [Fact]
    public static unsafe void MemoryCopy_DestinationTooSmall()
    {
        int[] sourceArray = new int[25000];
        for (int g = 0; g < sourceArray.Length; g++)
        {
            sourceArray[g] = g;
        }
        int[] destinationArray = new int[5000];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            // .NET Native seems to fail to compile if the body of the method below is included here.
            // So instead we use a method call as a workaround.
            TryCopy(sourceArray, destinationArray, 5000 * 4, 20000 * 4);
        });
    }

    private unsafe static void TryCopy(int[] sourceArray, int[] destinationArray, long destinationSize, long bytesToCopy)
    {
        fixed (int* sourceBase = sourceArray, destinationBase = destinationArray)
        {
            Buffer.MemoryCopy(sourceBase, destinationBase, destinationSize, bytesToCopy);
        }
    }
}