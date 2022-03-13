// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DllImportAttribute : Attribute
    {
        public CallingConvention CallingConvention;

        public string EntryPoint;

        public bool ExactSpelling;

        public DllImportAttribute(string dllName)
        {
        }
    }
}
