using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit, Size = 24)]
public struct OuterStruct
{
    [FieldOffset(0)]
    public object? Object;

    [FieldOffset(0)]
    public InnerStruct Inner;

    [FieldOffset(8)]
    public double Double;

    [FieldOffset(8)]
    public ulong High;

    [FieldOffset(16)]
    public ulong Low;
}

[StructLayout(LayoutKind.Explicit, Size = 16)]
public struct InnerStruct
{
    [FieldOffset(0)]
    public object? Object;

    [FieldOffset(8)]
    public int High;

    [FieldOffset(12)]
    public int Low;
}

public class Test_NestedStructsWithExplicitLayout {
    public static int Main ()
    {
        try
        {
            var x = new OuterStruct();
        }
        catch (TypeLoadException e)
        {
            return 101;
        }
        catch (Exception e)
        {
            return 102;
        }

        return 100;
    }
}
