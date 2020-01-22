// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
using System;
using TestLibrary;
using System.Runtime.CompilerServices;

class AllocateTypeAssociatedMemoryTest
{
    private static void ValidateInvalidArguments()
    {
        try
        {
            RuntimeHelpers.AllocateTypeAssociatedMemory(null, 10);
            Assert.Fail("No exception on invalid type");
        }
        catch (ArgumentException)
        {
        }

        try
        {
            RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(AllocateTypeAssociatedMemoryTest), -1);
            Assert.Fail("No exception on invalid size");
        }
        catch (ArgumentOutOfRangeException)
        {
        }
    }

    private static void ValidateSuccess()
    {
        IntPtr memory = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(AllocateTypeAssociatedMemoryTest), 32);
        Assert.AreNotEqual(memory, IntPtr.Zero);
    }

    public static void Run()
    {
        ValidateInvalidArguments();
        ValidateSuccess();
    }
}
