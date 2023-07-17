using System;
using System.Runtime.InteropServices;

#pragma warning disable 169

#if TYPEEQUIVALENCEASSEMBLY_1
public class TypeEquivalenceAssembly1
{}
#else
public class TypeEquivalenceAssembly2
{ }
#endif

namespace TypesWhichMatch
{
    [TypeIdentifier("TypesWhichMatch", "Type1")]
    public enum Type1
    {
    }

    [TypeIdentifier("TypesWhichMatch", "Type2")]
    public enum Type2
    {
// The actual values of the literal enums are not considered relevant for type equivalence
#if TYPEEQUIVALENCEASSEMBLY_1
        First = 1,
        Second = 2,
#else
        First = 2,
        Second = 1,
#endif
    }

    [TypeIdentifier("TypesWhichMatch", "Type3")]
    public struct Type3
    {
    }

    [TypeIdentifier("TypesWhichMatch", "Type4")]
    public struct Type4
    {
        public int X;
    }

    [TypeIdentifier("TypesWhichMatch", "Type5")]
    public struct Type5
    {
        public int X;
        public Type4 type4;
    }


    [TypeIdentifier("TypesWhichMatch", "Type6")]
    public delegate void Type6(int X);

    [TypeIdentifier("TypesWhichMatch", "Type7")]
    public delegate void Type7(int X);

    [TypeIdentifier("TypesWhichMatch", "Type9")]
    [ComImport]
    [GuidAttribute("9ED54F84-A89D-4fcd-A854-44251E925F09")]
    public interface Type9
    {
        // Mismatched methods don't impede equivalence for the purpose of IsEquivalent, but they will interfere with actual dispatch
        void Method();
#if TYPEEQUIVALENCEASSEMBLY_1
        void Method2(int x);
#else
        void Method2(short x);
        void Method3(int x, Type4 type4);
#endif

        // Test nested type
        [TypeIdentifier("TypesWhichMatch", "Type10")]
        public struct Type10
        {
            [MarshalAs(UnmanagedType.Bool)]
            public bool X;
        }
    }

    [TypeIdentifier("TypesWhichMatch", "Type11")]
    [StructLayout(LayoutKind.Explicit, Size = 40)]
    public struct Type11
    {
        [FieldOffset(0)]
        public int Lol;
        [FieldOffset(20)]
        public byte Omg;
    }

    [TypeIdentifier("TypesWhichMatch", "Type12")]
    [StructLayout(LayoutKind.Explicit)]
    public struct Type12
    {
        [FieldOffset(0)]
        public int Lol;
        [FieldOffset(20)]
        public byte Omg;
    }

    [TypeIdentifier("TypesWhichMatch", "Type13")]
    [ComEventInterface(null, null)]
    public interface Type13
    {
    }

    [TypeIdentifier("TypesWhichMatch", "Type14")]
    public struct Type14
    {
        // Validate that arrays, pointers and mdarrays are handled appropriately
        public Type4[] SzArray;
        public unsafe Type4* Ptr;
        public Type4[,,] MDArray;
    }

    // A case with a function pointer
    [TypeIdentifier("TypesWhichDoNotMatch", "Type15")]
    public struct Type15
    {
        public unsafe delegate*<int, void> functionPointer;
    }

}
