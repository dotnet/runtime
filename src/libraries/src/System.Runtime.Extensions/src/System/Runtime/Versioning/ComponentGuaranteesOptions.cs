// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Versioning
{
    [Flags]
    public enum ComponentGuaranteesOptions
    {
        None = 0,
        Exchange = 0x1,
        Stable = 0x2,
        SideBySide = 0x4,
    }
}
