// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
