// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    internal static class ApplicationModel
    {
        // TODO: this is a readonly field in CoreCLR. Use PR to find the better 
        // approach here since it seems wrong for this to differ between runtimes
        internal static bool IsUap => throw new PlatformNotSupportedException();
    }
}
