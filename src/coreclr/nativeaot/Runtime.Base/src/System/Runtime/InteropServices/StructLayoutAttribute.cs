// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class StructLayoutAttribute : Attribute
    {
        public StructLayoutAttribute(LayoutKind layoutKind)
        {
        }

        // These fields are expected by C# compiler,
        // so just disable the 'unused' warning
#pragma warning disable 649
        public LayoutKind Value;
        public int Pack;
        public int Size;
        public CharSet CharSet;
    }
}
