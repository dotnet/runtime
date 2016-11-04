// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    // Types used in Custom Modifier to specify calling conventions.
    [System.Runtime.InteropServices.ComVisible(true)]
    public class CallConvCdecl
    {
    }
    [System.Runtime.InteropServices.ComVisible(true)]
    public class CallConvStdcall
    {
    }
    [System.Runtime.InteropServices.ComVisible(true)]
    public class CallConvThiscall
    {
    }
    [System.Runtime.InteropServices.ComVisible(true)]
    public class CallConvFastcall
    {
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
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
    [Serializable]
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class HasCopySemanticsAttribute : Attribute
    {
        public HasCopySemanticsAttribute(){}
    }
    [Serializable]
    [AttributeUsage(AttributeTargets.Enum)]
    public sealed class ScopelessEnumAttribute : Attribute
    {
        public ScopelessEnumAttribute(){}
    }
    [Serializable]
    [AttributeUsage(AttributeTargets.Struct, Inherited = true),System.Runtime.InteropServices.ComVisible(true)]
    public sealed class NativeCppClassAttribute : Attribute
    {
        public NativeCppClassAttribute(){}
    }
    [Serializable]
    [AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface,AllowMultiple=true, Inherited=false)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class RequiredAttributeAttribute : Attribute
    {
        private Type requiredContract;

        public RequiredAttributeAttribute (Type requiredContract)
        {
            this.requiredContract= requiredContract;
        }
        public Type RequiredContract
        {
            get { return this.requiredContract; }
        }
    }
}
