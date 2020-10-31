// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel
{
    /// <summary>
    /// Represents an exception thrown because of a Win32 error
    /// </summary>
    public class HResultException : Exception
    {
        public readonly int Win32HResult;
        public HResultException(int hResult) : base(hResult.ToString("X4"))
        {
            Win32HResult = hResult;
        }
    }
}
