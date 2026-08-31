using System;
using System.Runtime.InteropServices;

#pragma warning disable 169

namespace TypesWhichDoNotLoad
{
    // A case where a field is not public
    [TypeIdentifier("TypesWhichDoNotMatch", "Type1")]
    public struct Type1
    {
        int field;
    }

    // A case where the type is not both public
    [TypeIdentifier("TypesWhichDoNotLoad", "Type2")]
    enum Type2
    {
    }

    // A case where the type is not both public
    [TypeIdentifier("TypesWhichDoNotLoad", "Type3")]
    struct Type3
    {
    }

    // A case where the type is not both public
    [TypeIdentifier("TypesWhichDoNotLoad", "Type4")]
    delegate object Type4(object param);

    // A case where the type is not nested public
    [TypeIdentifier("TypesWhichDoNotLoad", "Type4_IGNORE")]
    public struct Type4_IGNORE
    {
        [TypeIdentifier("TypesWhichDoNotLoad", "Type5")]
        struct Type5
        { }
    }

    // A case with a static field
    [TypeIdentifier("TypesWhichDoNotLoad", "Type6")]
    public struct Type6
    {
        public static int field;
    }

    // A case with an instance method
    [TypeIdentifier("TypesWhichDoNotLoad", "Type7")]
    public struct Type7
    {
        public void Method() { }
    }

    // A case which is a normal interface (not ComImport of ComEventInterface)
    [TypeIdentifier("TypesWhichDoNotLoad", "Type8")]
    public interface Type8 { }

    // A generic type
    [TypeIdentifier("TypesWhichDoNotLoad", "Type9")]
    public struct Type9<T> { }

    // A type nested in a non TypeEquivalent type
    public struct NonTypeEquivalent
    {
        [TypeIdentifier("TypesWhichDoNotLoad", "Type10")]
        public struct Type10 { }
    }
}
