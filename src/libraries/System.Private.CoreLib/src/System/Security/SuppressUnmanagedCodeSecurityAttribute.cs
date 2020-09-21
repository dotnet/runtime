// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security
{
    // SuppressUnmanagedCodeSecurityAttribute:
    //  This attribute has no functional impact in CoreCLR.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
    public sealed class SuppressUnmanagedCodeSecurityAttribute : Attribute
    {
        public SuppressUnmanagedCodeSecurityAttribute() { }
    }
}
