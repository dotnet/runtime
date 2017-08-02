// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        // https://msdn.microsoft.com/en-us/library/windows/desktop/bb530717.aspx
        internal struct TOKEN_ELEVATION
        {
            public BOOL TokenIsElevated;
        }
    }
}
