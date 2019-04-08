// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Security
{
    // SecurityTreatAsSafeAttribute:
    // Indicates that the code may contain violations to the security critical rules (e.g. transitions from
    //      critical to non-public transparent, transparent to non-public critical, etc.), has been audited for
    //      security concerns and is considered security clean.
    // At assembly-scope, all rule checks will be suppressed within the assembly and for calls made against the assembly.
    // At type-scope, all rule checks will be suppressed for members within the type and for calls made against the type.
    // At member level (e.g. field and method) the code will be treated as public - i.e. no rule checks for the members.

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
    [Obsolete("SecurityTreatAsSafe is only used for .NET 2.0 transparency compatibility.  Please use the SecuritySafeCriticalAttribute instead.")]
    public sealed class SecurityTreatAsSafeAttribute : Attribute
    {
        public SecurityTreatAsSafeAttribute() { }
    }
}
