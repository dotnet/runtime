using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using CoreFXTestLibrary;

internal struct BlittableStruct
{
    internal int a;
    internal int b;
    internal byte c;
    internal short d;
    internal IntPtr p;
}

internal struct StructWithReferenceTypes
{
    internal IntPtr ptr;
    internal string str;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    internal int[] byValArr;
}

class Test
{
    static int Main(string[] args)
    {
        TestNegativeCases();
        TestBlittableStruct();
        TestStructWithReferenceType();

        return 100;
    }

    static void TestNegativeCases()
    {
        Assert.Throws<AccessViolationException>(() => { Marshal.WriteByte(null, 0, 0); });
        Assert.Throws<AccessViolationException>(() => { Marshal.WriteInt16(null, 0, 0); });
        Assert.Throws<AccessViolationException>(() => { Marshal.WriteInt32(null, 0, 0); });
        Assert.Throws<AccessViolationException>(() => { Marshal.WriteInt64(null, 0, 0); });
        Assert.Throws<AccessViolationException>(() => { Marshal.WriteIntPtr(null, 0, IntPtr.Zero); });
        Assert.Throws<AccessViolationException>(() => { Marshal.ReadByte(null, 0); });
        Assert.Throws<AccessViolationException>(() => { Marshal.ReadInt16(null, 0); });
        Assert.Throws<AccessViolationException>(() => { Marshal.ReadInt32(null, 0); });
        Assert.Throws<AccessViolationException>(() => { Marshal.ReadIntPtr(null, 0); });
    }

    static void TestBlittableStruct()
    { 
        Console.WriteLine("TestBlittableStruct");

        BlittableStruct blittableStruct = new BlittableStruct();
        blittableStruct.a = 200;
        blittableStruct.b = 300;
        blittableStruct.c = 10;
        blittableStruct.d = 123;
        blittableStruct.p = new IntPtr(100);

        object boxedBlittableStruct = (object)blittableStruct;

        int offsetOfB = Marshal.OffsetOf<BlittableStruct>("b").ToInt32();
        int offsetOfC = Marshal.OffsetOf<BlittableStruct>("c").ToInt32();
        int offsetOfD = Marshal.OffsetOf<BlittableStruct>("d").ToInt32();
        int offsetOfP = Marshal.OffsetOf<BlittableStruct>("p").ToInt32();

        Assert.AreEqual(Marshal.ReadInt32(boxedBlittableStruct, 0), 200);
        Assert.AreEqual(Marshal.ReadInt32(boxedBlittableStruct, offsetOfB), 300);
        Assert.AreEqual(Marshal.ReadByte(boxedBlittableStruct, offsetOfC), 10);
        Assert.AreEqual(Marshal.ReadInt16(boxedBlittableStruct, offsetOfD), 123);
        Assert.AreEqual(Marshal.ReadIntPtr(boxedBlittableStruct, offsetOfP), new IntPtr(100));

        Marshal.WriteInt32(boxedBlittableStruct, 0, 300);
        Marshal.WriteInt32(boxedBlittableStruct, offsetOfB, 400);
        Marshal.WriteByte(boxedBlittableStruct, offsetOfC, 20);
        Marshal.WriteInt16(boxedBlittableStruct, offsetOfD, 144);

        Marshal.WriteIntPtr(boxedBlittableStruct, offsetOfP, new IntPtr(500));

        Assert.AreEqual(((BlittableStruct)boxedBlittableStruct).a, 300);
        Assert.AreEqual(((BlittableStruct)boxedBlittableStruct).b, 400);
        Assert.AreEqual(((BlittableStruct)boxedBlittableStruct).c, 20);
        Assert.AreEqual(((BlittableStruct)boxedBlittableStruct).d, 144);
        Assert.AreEqual(((BlittableStruct)boxedBlittableStruct).p, new IntPtr(500));
    }

    static void TestStructWithReferenceType()
    {
        Console.WriteLine("TestStructWithReferenceType");

        StructWithReferenceTypes structWithReferenceTypes = new StructWithReferenceTypes();
        structWithReferenceTypes.ptr = new IntPtr(100);
        structWithReferenceTypes.str = "ABC";
        structWithReferenceTypes.byValArr = new int[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        object boxedStruct = (object)structWithReferenceTypes;

        int offsetOfStr = Marshal.OffsetOf<StructWithReferenceTypes>("str").ToInt32();
        int offsetOfByValArr = Marshal.OffsetOf<StructWithReferenceTypes>("byValArr").ToInt32();

        Assert.AreEqual(Marshal.ReadInt32(boxedStruct, 0), 100);
        Assert.AreNotEqual(Marshal.ReadIntPtr(boxedStruct, offsetOfStr), IntPtr.Zero);
        Assert.AreEqual(Marshal.ReadInt32(boxedStruct, offsetOfByValArr + sizeof(int) * 2), 3);

        Marshal.WriteInt32(boxedStruct, 0, 200);
        Marshal.WriteInt32(boxedStruct, offsetOfByValArr + sizeof(int) * 9, 100);

        Assert.AreEqual(((StructWithReferenceTypes)boxedStruct).ptr, new IntPtr(200));
        Assert.AreEqual(((StructWithReferenceTypes)boxedStruct).byValArr[9], 100);
        Assert.AreEqual(((StructWithReferenceTypes)boxedStruct).byValArr[8], 9);
        Assert.AreEqual(((StructWithReferenceTypes)boxedStruct).str, "ABC");
    }
}

