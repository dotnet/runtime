using System;
using System.Runtime.InteropServices;

#pragma warning disable 169

namespace TypesWhichDoNotMatch
{
    // A case where 1 type has a field, and another does not
    [TypeIdentifier("TypesWhichDoNotMatch", "Type1")]
    public struct Type1
    {
#if TYPEEQUIVALENCEASSEMBLY_1
        public int field;
#endif
    }

    // A case where there is a static method on the type
    [TypeIdentifier("TypesWhichDoNotMatch", "Type2")]
    public struct Type2
    {
        public static void Method() { }
    }

    // A case where a delegate varies in signature
    [TypeIdentifier("TypesWhichDoNotMatch", "Type3")]
    public delegate
#if TYPEEQUIVALENCEASSEMBLY_1
        object
#else
        string
#endif
        Type3();

    // A case where the type names are not the same
    [TypeIdentifier("TypesWhichDoNotMatch", "Type4")]
    public struct
#if TYPEEQUIVALENCEASSEMBLY_1
        Type4
#else
        Type4NotQuite
#endif
    { }

    // A case where MarshalAs behavior is different
    [TypeIdentifier("TypesWhichDoNotMatch", "Type5")]
    public struct Type5
    {
#if TYPEEQUIVALENCEASSEMBLY_1
        [MarshalAs(UnmanagedType.Bool)]
#else
        [MarshalAs(UnmanagedType.VariantBool)]
#endif
        public bool X;
    }

    // A different case where MarshalAs behavior is different
    [TypeIdentifier("TypesWhichDoNotMatch", "Type6")]
    public struct Type6
    {
#if TYPEEQUIVALENCEASSEMBLY_1
        [MarshalAs(UnmanagedType.Bool)]
#endif
        public bool X;
    }

    // A case where fixed layouts are not the same
    [TypeIdentifier("TypesWhichDoNotMatch", "Type7")]
    [StructLayout(LayoutKind.Explicit)]
    public struct Type7
    {
        [FieldOffset(0)]
        public int Lol;
#if TYPEEQUIVALENCEASSEMBLY_1
        [FieldOffset(20)]
#else
        [FieldOffset(21)]
#endif
        public byte Omg;
    }

    // A case where the MDArray is different
    [TypeIdentifier("TypesWhichDoNotMatch", "Type8")]
    public struct Type8
    {
        public
#if TYPEEQUIVALENCEASSEMBLY_1
            TypesWhichMatch.Type4[,,]
#else
            TypesWhichMatch.Type4[,,,]
#endif
            MDArray;
    }

    // A case where the underlying data of the enum does not match
    [TypeIdentifier("TypesWhichDoNotMatch", "Type9")]
    public enum Type9 :
#if TYPEEQUIVALENCEASSEMBLY_1
        byte
#else
        sbyte
#endif
    {
    }

    // A case with a function pointer
    [TypeIdentifier("TypesWhichDoNotMatch", "Type10")]
    public struct Type10
    {
        public unsafe delegate*<TypesWhichMatch.Type4, void> functionPointer;
    }

    // A case where the overall size is different
#if TYPEEQUIVALENCEASSEMBLY_1
    [StructLayout(LayoutKind.Explicit, Size = 40)]
#else
    [StructLayout(LayoutKind.Explicit, Size = 41)]
#endif
    [TypeIdentifier("TypesWhichDoNotMatch", "Type11")]
    public struct Type11
    {
        [FieldOffset(0)]
        public int Lol;
        [FieldOffset(20)]
        public byte Omg;

        // A test case where the type itself would match, but its containing type does not
        [TypeIdentifier("TypesWhichDoNotMatch", "Type12")]
        public struct Type12 { }
    }

    // No layoutkind auto struct will ever match (but it will load)
    [TypeIdentifier("TypesWhichDoNotMatch", "Type13")]
    [StructLayout(LayoutKind.Auto)]
    public struct Type13 { }
}
