// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.InteropServices;
using Xunit;

public unsafe class ArrayWithOffsetTest
{
    [Fact]
    [SkipOnMono("needs triage")]
    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/170", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static int TestEntryPoint()
    {
        try
        {
            Span<int> expected = new int[] {1, 2, 3, 4, 5, 6};
            Span<int> newValue = new int[] {7, 8, 9, 10, 11, 12};

            for (int i = 0; i < expected.Length; i++)
            {
                int[] array = new int[] {1, 2, 3, 4, 5, 6};
                ArrayWithOffset offset = new ArrayWithOffset(array, i * 4); // The offset parameter in ArrayWithOffset is a byte-offset, not an element offset.

                fixed (int* expectedSubArray = expected.Slice(i))
                fixed (int* newValueSubArray = newValue.Slice(i))
                {
                    Assert.True(ArrayWithOffsetNative.Marshal_InOut(expectedSubArray, offset, expected.Length - i, newValueSubArray), $"Native call failed with element offset {i}.");
                }

                for (int j = 0; j < i; j++)
                {
                    Assert.Equal(expected[j], array[j]);
                }

                for (int j = i; j < array.Length; j++)
                {
                    Assert.Equal(newValue[j], array[j]);
                }
            }

            ArrayWithOffset arrayWithOffset = new ArrayWithOffset(new int[]{ 1 }, 0);

            Assert.Throws<MarshalDirectiveException>(() => ArrayWithOffsetNative.Marshal_Invalid(arrayWithOffset));
            Assert.Throws<MarshalDirectiveException>(() => ArrayWithOffsetNative.Marshal_Invalid(ref arrayWithOffset));
            Assert.Throws<MarshalDirectiveException>(() => ArrayWithOffsetNative.Marshal_Invalid_Return());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 101;
        }

        return 100;
    }
}
