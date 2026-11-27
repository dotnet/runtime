// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public static class HelperCalls
{
    public static System.ArgumentException EagerDepthLeaf() => new System.ArgumentException();

    public static System.ArgumentException EagerDepthMiddle() => EagerDepthLeaf();

    public static System.ArgumentException EagerDepthOuter() => EagerDepthMiddle();

    public static System.ArgumentException EagerDepthAtLimit() => EagerDepthOuter();

    public static System.ArgumentException EagerDepthBeyondLimit() => EagerDepthAtLimit();

    public static int TrailingZeroCount32(uint value) =>
        System.Numerics.BitOperations.TrailingZeroCount(value);

    public static int TrailingZeroCount64(ulong value) =>
        System.Numerics.BitOperations.TrailingZeroCount(value);

    public static uint Crc32C(uint crc, byte value) =>
        System.Numerics.BitOperations.Crc32C(crc, value);

    public static int Sum(int count)
    {
        int result = 0;
        for (int i = 0; i < count; i++)
        {
            result = checked(result + i);
        }

        return result;
    }

    public static int SumWithFinally(int count)
    {
        try
        {
            return Sum(count);
        }
        finally
        {
            System.Console.WriteLine(count);
        }
    }
}
