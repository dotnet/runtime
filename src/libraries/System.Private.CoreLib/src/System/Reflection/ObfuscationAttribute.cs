// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Delegate,
        AllowMultiple = true, Inherited = false)]
    public sealed class ObfuscationAttribute : Attribute
    {
        public ObfuscationAttribute()
        {
        }

        public bool StripAfterObfuscation { get; set; } = true;
        public bool Exclude { get; set; } = true;
        public bool ApplyToMembers { get; set; } = true;
        public string? Feature { get; set; } = "all";
    }
}

