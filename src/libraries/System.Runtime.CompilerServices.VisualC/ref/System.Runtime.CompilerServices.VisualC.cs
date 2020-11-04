// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Runtime.CompilerServices
{
    public static partial class CompilerMarshalOverride
    {
    }
    [AttributeUsageAttribute(AttributeTargets.Assembly, AllowMultiple=true)]
    public sealed class CppInlineNamespaceAttribute : Attribute
    {
        public CppInlineNamespaceAttribute(string dottedName) {}
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Struct)]
    public sealed partial class HasCopySemanticsAttribute : System.Attribute
    {
        public HasCopySemanticsAttribute() { }
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
    public static partial class IsPinned
    {
    }
    public static partial class IsSignUnspecifiedByte
    {
    }
    public static partial class IsUdtReturn
    {
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Struct, Inherited=true)]
    public sealed partial class NativeCppClassAttribute : System.Attribute
    {
        public NativeCppClassAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Enum | System.AttributeTargets.Interface | System.AttributeTargets.Struct, AllowMultiple=true, Inherited=false)]
    public sealed partial class RequiredAttributeAttribute : System.Attribute
    {
        public RequiredAttributeAttribute(System.Type requiredContract) { }
        public System.Type RequiredContract { get { throw null; } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Enum)]
    public sealed partial class ScopelessEnumAttribute : System.Attribute
    {
        public ScopelessEnumAttribute() { }
    }
}
