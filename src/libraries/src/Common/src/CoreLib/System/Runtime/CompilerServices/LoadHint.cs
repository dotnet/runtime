// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    public enum LoadHint
    {
        Default = 0x0000,           // No preference specified
        Always = 0x0001,            // Dependency is always loaded
        Sometimes = 0x0002,         // Dependency is sometimes loaded
    }
}