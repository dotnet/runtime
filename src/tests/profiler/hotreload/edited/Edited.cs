// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace HotReloadEdited
{
    public static class Edited
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Value() => 1;
    }
}
