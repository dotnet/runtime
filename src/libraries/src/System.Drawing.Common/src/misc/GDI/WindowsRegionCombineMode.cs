// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Internal
{
    internal enum RegionCombineMode
    {
        AND = 1,
        OR = 2,
        XOR = 3,
        DIFF = 4,
        COPY = 5,
        MIN = AND,
        MAX = COPY
    }
}
