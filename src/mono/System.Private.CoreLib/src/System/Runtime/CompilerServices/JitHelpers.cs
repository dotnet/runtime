// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    internal static class JitHelpers
    {
        [Intrinsic]
        internal static void DisableInline () => throw new NotImplementedException();
    }
}
