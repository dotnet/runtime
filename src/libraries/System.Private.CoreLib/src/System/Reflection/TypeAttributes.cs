// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    // This Enum matches the CorTypeAttr defined in CorHdr.h
    [Flags]
    public enum TypeAttributes
    {
        VisibilityMask = 0x00000007,
        NotPublic = 0x00000000,     // Class is not public scope.
        Public = 0x00000001,     // Class is public scope.
        NestedPublic = 0x00000002,     // Class is nested with public visibility.
        NestedPrivate = 0x00000003,     // Class is nested with private visibility.
        NestedFamily = 0x00000004,     // Class is nested with family visibility.
        NestedAssembly = 0x00000005,     // Class is nested with assembly visibility.
        NestedFamANDAssem = 0x00000006,     // Class is nested with family and assembly visibility.
        NestedFamORAssem = 0x00000007,     // Class is nested with family or assembly visibility.

        // Use this mask to retrieve class layout information
        // 0 is AutoLayout, 0x2 is SequentialLayout, 4 is ExplicitLayout
        LayoutMask = 0x00000018,
        AutoLayout = 0x00000000,     // Class fields are auto-laid out
        SequentialLayout = 0x00000008,     // Class fields are laid out sequentially
        ExplicitLayout = 0x00000010,     // Layout is supplied explicitly
                                         // end layout mask

        // Use this mask to distinguish whether a type declaration is an interface.  (Class vs. ValueType done based on whether it subclasses S.ValueType)
        ClassSemanticsMask = 0x00000020,
        Class = 0x00000000,     // Type is a class (or a value type).
        Interface = 0x00000020,     // Type is an interface.

        // Special semantics in addition to class semantics.
        Abstract = 0x00000080,     // Class is abstract
        Sealed = 0x00000100,     // Class is concrete and may not be extended
        SpecialName = 0x00000400,     // Class name is special.  Name describes how.

        // Implementation attributes.
        Import = 0x00001000,     // Class / interface is imported
        Serializable = 0x00002000,     // The class is Serializable.
        WindowsRuntime = 0x00004000,     // Type is a Windows Runtime type.

        // Use tdStringFormatMask to retrieve string information for native interop
        StringFormatMask = 0x00030000,
        AnsiClass = 0x00000000,     // LPTSTR is interpreted as ANSI in this class
        UnicodeClass = 0x00010000,     // LPTSTR is interpreted as UNICODE
        AutoClass = 0x00020000,     // LPTSTR is interpreted automatically
        CustomFormatClass = 0x00030000,     // A non-standard encoding specified by CustomFormatMask
        CustomFormatMask = 0x00C00000,     // Use this mask to retrieve non-standard encoding information for native interop. The meaning of the values of these 2 bits is unspecified.

        // end string format mask

        BeforeFieldInit = 0x00100000,     // Initialize the class any time before first static field access.

        RTSpecialName = 0x00000800,     // Runtime should check name encoding.
        HasSecurity = 0x00040000,     // Class has security associate with it.

        ReservedMask = 0x00040800,
    }
}
