using System;
using System.Collections.Generic;

internal static class Inputs
{
    public static IEnumerable<ArraySegment<byte>> GetArraysVariedByOffsetAndLength()
    {
        yield return new ArraySegment<byte>(new byte[512],   0,     512);
        yield return new ArraySegment<byte>(new byte[512],   1,     511);
        yield return new ArraySegment<byte>(new byte[512],   2,     510);
        yield return new ArraySegment<byte>(new byte[512], 256,     256);
        yield return new ArraySegment<byte>(new byte[512], 512,       0);
        yield return new ArraySegment<byte>(new byte[512], 511,       1);
        yield return new ArraySegment<byte>(new byte[512], 510,       2);
    }

    public static IEnumerable<byte[]> GetArraysVariedBySize()
    {
        yield return FillWithData(new byte[0]);
        yield return FillWithData(new byte[1]);
        yield return FillWithData(new byte[2]);
        yield return FillWithData(new byte[254]);
        yield return FillWithData(new byte[255]);
        yield return FillWithData(new byte[256]);
        yield return FillWithData(new byte[511]);
        yield return FillWithData(new byte[512]);
        yield return FillWithData(new byte[513]);
        yield return FillWithData(new byte[1023]);
        yield return FillWithData(new byte[1024]);
        yield return FillWithData(new byte[1025]);
        yield return FillWithData(new byte[2047]);
        yield return FillWithData(new byte[2048]);
        yield return FillWithData(new byte[2049]);
    }

    private static byte[] FillWithData(byte[] buffer)
    {
        for (int i = 0; i < buffer.Length;i ++)
        {
            buffer[i] = (byte)i;
        }

        return buffer;
    }
}
