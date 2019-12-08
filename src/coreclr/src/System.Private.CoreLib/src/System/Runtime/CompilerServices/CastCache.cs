// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    // Provides managed root for the CastCache table.
    internal static class CastCache
    {
#pragma warning disable CA1823, 169 // this field is not used by managed code, yet.
        private static int[]? s_table;
#pragma warning restore CA1823, 169
    }
}
