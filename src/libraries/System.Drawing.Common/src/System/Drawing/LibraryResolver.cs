// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Drawing.Printing;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal static class LibraryResolver
    {
        internal static void EnsureRegistered()
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_Unix);
        }
    }
}
