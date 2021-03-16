// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.IO
{
    public partial class Stream
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool HasOverriddenBeginEndRead();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool HasOverriddenBeginEndWrite();
    }
}
