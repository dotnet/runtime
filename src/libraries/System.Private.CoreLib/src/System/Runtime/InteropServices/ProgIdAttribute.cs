// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ProgIdAttribute : Attribute
    {
        public ProgIdAttribute(string progId)
        {
            Value = progId;
        }

        public string Value { get; }
    }
}
