// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Runtime_129681
{
    private const int StorageLength = 8192;

    [Fact]
    public static void TestEntryPoint()
    {
        object[] storage = new object[StorageLength];
        int index = 0;

        try
        {
            while (index < storage.Length)
            {
                storage[index++] = GC.AllocateArray<byte>(16 * 1024, pinned: true);
            }
        }
        catch (OutOfMemoryException)
        {
        }

        try
        {
            while (index < storage.Length)
            {
                storage[index++] = GC.AllocateArray<byte>(256, pinned: true);
            }
        }
        catch (OutOfMemoryException)
        {
        }

        try
        {
            while (index < storage.Length)
            {
                storage[index++] = GC.AllocateArray<byte>(1, pinned: true);
            }
        }
        catch (OutOfMemoryException)
        {
            return;
        }

        throw new Exception("The configured GC heap hard limit was not reached.");
    }
}
