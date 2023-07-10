using System;
using System.Runtime.InteropServices;
using Xunit;

public class Runtime_59871
{
    LargeStruct _large;
    Union _field;

    [Fact]
    public static int TestEntryPoint()
    {
        Foo(new Runtime_59871());
        return 100;
    }

    static DateTime Foo(Runtime_59871 p)
    {
        switch (Environment.TickCount % 4)
        {
            case 0: return p._field.DateTime;
            case 1: return p._field.DateTime;
            case 2: return p._field.DateTime;
            case 3: return p._field.DateTime;
        }

        return p._field.DateTime;
    }

    unsafe struct LargeStruct
    {
        public fixed byte F[0x10000];
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    struct Union
    {
        [FieldOffset(0)]
        public long Int64;
        [FieldOffset(0)]
        public DateTime DateTime;
    }
}
