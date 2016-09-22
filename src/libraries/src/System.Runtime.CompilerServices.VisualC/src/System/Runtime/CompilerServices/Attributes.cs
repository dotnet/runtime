// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
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
    public static partial class IsConst
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
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public static partial class IsVolatile
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
}
