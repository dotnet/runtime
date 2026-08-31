// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
    public sealed class ComAliasNameAttribute : Attribute
    {
        public ComAliasNameAttribute(string alias) => Value = alias;

        public string Value { get; }
    }
}
