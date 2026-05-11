// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for https://github.com/dotnet/runtime/issues/127952
// Marshal.StructureToPtr / Marshal.DestroyStructure on a struct containing a
// ByValArray of DateTime values requires the DateMarshaler stub-helper to
// survive trimming.  Without the fix this throws:
//   System.Security.VerificationException: Method
//   System.StubHelpers.StubHelpers.FreeArrayContents: type argument
//   'System.StubHelpers.DateMarshaler' violates the constraint of type
//   parameter 'TMarshaler'.

using System;
using System.Runtime.InteropServices;

var structure = new StructWithDateArray()
{
    array = new DateTime[]
    {
        DateTime.Now, DateTime.Now
    }
};

int size = Marshal.SizeOf(structure);
IntPtr memory = Marshal.AllocHGlobal(size);
try
{
    Marshal.StructureToPtr(structure, memory, false);
    Marshal.StructureToPtr(structure, memory, true);
}
finally
{
    Marshal.DestroyStructure(memory, structure.GetType());
    Marshal.FreeHGlobal(memory);
}

return 100;

public struct StructWithDateArray
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public DateTime[] array;
}
