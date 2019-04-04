// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Security
{
    // SecurityCriticalAttribute
    //  Indicates that the decorated code or assembly performs security critical operations (e.g. Assert, "unsafe", LinkDemand, etc.)
    //  The attribute can be placed on most targets, except on arguments/return values.
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
