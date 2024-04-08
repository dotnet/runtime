// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The struct has an objref and is of odd size.
// The GC requires that all valuetypes containing objrefs be sized to a multiple of sizeof(void*) )== 4).
// Since the size of this struct was 17 we were throwing a TypeLoadException.

using System;
using System.Runtime.InteropServices;
using Xunit;

#pragma warning disable 618
[StructLayout(LayoutKind.Explicit)]
public struct S
{
    [FieldOffset(16), MarshalAs(UnmanagedType.VariantBool)] public bool b;
    [FieldOffset(8)] public double d;
    [FieldOffset(0), MarshalAs(UnmanagedType.BStr)] public string st;
}
#pragma warning restore 618

public class Test_explicitStruct_oddSize
{
    public static void Run()
    {
        S s;
        s.b = true;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Run();

            Console.WriteLine("PASS");
            return 100;
        }
        catch (TypeLoadException e)
        {
            Console.WriteLine("FAIL: Caught unexpected TypeLoadException: {0}", e.Message);
            return 101;
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL: Caught unexpected Exception: {0}", e.Message);
            return 101;
        }
    }

 }
