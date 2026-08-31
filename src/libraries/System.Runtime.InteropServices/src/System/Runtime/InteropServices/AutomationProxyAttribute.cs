// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
    public sealed class AutomationProxyAttribute : Attribute
    {
        public AutomationProxyAttribute(bool val) => Value = val;

        public bool Value { get; }
    }
}
