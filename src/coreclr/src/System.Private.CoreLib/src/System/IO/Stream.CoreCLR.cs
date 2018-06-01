// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.IO
{
    public abstract partial class Stream
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern bool HasOverriddenBeginEndRead();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern bool HasOverriddenBeginEndWrite();
    }
}
