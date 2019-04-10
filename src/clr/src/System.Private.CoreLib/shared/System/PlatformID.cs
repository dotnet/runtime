// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.ComponentModel;

namespace System
{
#if PROJECTN
    [Internal.Runtime.CompilerServices.RelocatedType("System.Runtime.Extensions")]
#endif
    public enum PlatformID
    {
        [EditorBrowsable(EditorBrowsableState.Never)] Win32S = 0,
        [EditorBrowsable(EditorBrowsableState.Never)] Win32Windows = 1,
        Win32NT = 2,
        [EditorBrowsable(EditorBrowsableState.Never)] WinCE = 3,
        Unix = 4,
        [EditorBrowsable(EditorBrowsableState.Never)] Xbox = 5,
        [EditorBrowsable(EditorBrowsableState.Never)] MacOSX = 6
    }
}
