// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class LCIDConversionAttribute : Attribute
    {
        public LCIDConversionAttribute(int lcid)
        {
            Value = lcid;
        }

        public int Value { get; }
    }
}
