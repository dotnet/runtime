// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// We need to add an InternalsVisibleToAttribute here to mscorlib since we need to expose some of these types via type forwards in mscorlib
// since tooling expects some types to live there and not in System.Runtime.CompilerServices.VisualC, but we don't want to expose
// these types publicly.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("mscorlib, PublicKey=00000000000000000400000000000000")]

#pragma warning disable IDE0060

namespace System.Runtime.CompilerServices
{
    // Types used by the C++/CLI compiler during linking.
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    internal sealed class AssemblyAttributesGoHere
    {
        internal AssemblyAttributesGoHere()
        {
        }
    }
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    internal sealed class AssemblyAttributesGoHereS
    {
        internal AssemblyAttributesGoHereS()
        {
        }
    }
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    internal sealed class AssemblyAttributesGoHereM
    {
        internal AssemblyAttributesGoHereM()
        {
        }
    }
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    internal sealed class AssemblyAttributesGoHereSM
    {
        internal AssemblyAttributesGoHereSM()
        {
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    internal sealed class DecoratedNameAttribute : Attribute
    {
        public DecoratedNameAttribute(string decoratedName) { }
    }

    // Indicates that the modified instance is pinned in memory.
    public static class IsPinned
    {
    }

    public static partial class IsBoxed
    {
    }

    public static partial class IsByValue
    {
    }

    public static partial class IsCopyConstructed
    {
    }

    public static partial class IsExplicitlyDereferenced
    {
    }

    public static partial class IsImplicitlyDereferenced
    {
    }

    public static partial class IsJitIntrinsic
    {
    }

    public static partial class IsLong
    {
    }

    public static partial class IsSignUnspecifiedByte
    {
    }

    public static partial class IsUdtReturn
    {
    }

    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class HasCopySemanticsAttribute : Attribute
    {
        public HasCopySemanticsAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Enum)]
    public sealed class ScopelessEnumAttribute : Attribute
    {
        public ScopelessEnumAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Struct, Inherited = true)]
    public sealed class NativeCppClassAttribute : Attribute
    {
        public NativeCppClassAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class CppInlineNamespaceAttribute : Attribute
    {
        public CppInlineNamespaceAttribute(string dottedName) { }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public sealed class RequiredAttributeAttribute : Attribute
    {
        public RequiredAttributeAttribute(Type requiredContract) => RequiredContract = requiredContract;

        public Type RequiredContract { get; }
    }

    [AttributeUsage(AttributeTargets.Class |
                    AttributeTargets.Constructor |
                    AttributeTargets.Method |
                    AttributeTargets.Field |
                    AttributeTargets.Event |
                    AttributeTargets.Property)]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    internal sealed class SuppressMergeCheckAttribute : Attribute
    {
        public SuppressMergeCheckAttribute()
        { }
    }

    // The CLR data marshaler has some behaviors that are incompatible with
    // C++. Specifically, C++ treats boolean variables as byte size, whereas
    // the marshaller treats them as 4-byte size.  Similarly, C++ treats
    // wchar_t variables as 4-byte size, whereas the marshaller treats them
    // as single byte size under certain conditions.  In order to work around
    // such issues, the C++ compiler will emit a type that the marshaller will
    // marshal using the correct sizes.  In addition, the compiler will place
    // this modopt onto the variables to indicate that the specified type is
    // not the true type.  Any compiler that needed to deal with similar
    // marshalling incompatibilities could use this attribute as well.
    //
    // Indicates that the modified instance differs from its true type for
    // correct marshalling.
    public static class CompilerMarshalOverride
    {
    }
}
