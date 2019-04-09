// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class DllImportAttribute : Attribute
    {
        public DllImportAttribute(string dllName)
        {
            Value = dllName;
        }

        public string Value { get; }

        public string? EntryPoint;
        public CharSet CharSet;
        public bool SetLastError;
        public bool ExactSpelling;
        public CallingConvention CallingConvention;
        public bool BestFitMapping;
        public bool PreserveSig;
        public bool ThrowOnUnmappableChar;
    }
}
