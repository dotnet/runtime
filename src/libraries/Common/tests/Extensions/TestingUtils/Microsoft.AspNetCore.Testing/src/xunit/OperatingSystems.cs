// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.AspNetCore.Testing
{
    [Flags]
    public enum OperatingSystems
    {
        Linux = 1,
        MacOSX = 2,
        Windows = 4,
    }
}