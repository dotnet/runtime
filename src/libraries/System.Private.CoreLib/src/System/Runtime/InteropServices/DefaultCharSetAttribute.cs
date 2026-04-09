// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Module, Inherited = false)]
    public sealed class DefaultCharSetAttribute : Attribute
    {
        public DefaultCharSetAttribute(CharSet charSet)
        {
            CharSet = charSet;
        }

        public CharSet CharSet { get; }
    }
}
