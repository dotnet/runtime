// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security
{
    // Has no effect in .NET Core
    [AttributeUsage(AttributeTargets.Assembly |
                    AttributeTargets.Class |
                    AttributeTargets.Struct |
                    AttributeTargets.Enum |
                    AttributeTargets.Constructor |
                    AttributeTargets.Method |
                    AttributeTargets.Field |
                    AttributeTargets.Interface |
                    AttributeTargets.Delegate,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class SecurityCriticalAttribute : Attribute
    {
#pragma warning disable 618    // We still use SecurityCriticalScope for v2 compat
        public SecurityCriticalAttribute() { }

        public SecurityCriticalAttribute(SecurityCriticalScope scope)
        {
            Scope = scope;
        }

        [Obsolete("SecurityCriticalScope is only used for .NET 2.0 transparency compatibility.")]
        public SecurityCriticalScope Scope { get; }
#pragma warning restore 618
    }
}
