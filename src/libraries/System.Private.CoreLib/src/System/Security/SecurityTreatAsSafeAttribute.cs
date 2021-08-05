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
    [Obsolete("SecurityTreatAsSafe is only used for .NET 2.0 transparency compatibility. Use the SecuritySafeCriticalAttribute instead.")]
    public sealed class SecurityTreatAsSafeAttribute : Attribute
    {
        public SecurityTreatAsSafeAttribute() { }
    }
}
