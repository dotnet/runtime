// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
    public sealed class UnmanagedFunctionPointerAttribute : Attribute
    {
        public UnmanagedFunctionPointerAttribute()
        {
            CallingConvention = CallingConvention.Winapi;
        }

        public UnmanagedFunctionPointerAttribute(CallingConvention callingConvention)
        {
            CallingConvention = callingConvention;
        }

        public CallingConvention CallingConvention { get; }

        public bool BestFitMapping;
        public bool SetLastError;
        public bool ThrowOnUnmappableChar;
        public CharSet CharSet;
    }
}
