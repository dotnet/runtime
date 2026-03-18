// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.IO
{
    public abstract partial class Stream
    {
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool HasOverriddenBeginEndRead();

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool HasOverriddenBeginEndWrite();
    }
}
