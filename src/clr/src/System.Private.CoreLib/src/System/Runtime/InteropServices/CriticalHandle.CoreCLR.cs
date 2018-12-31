// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;

namespace System.Runtime.InteropServices
{
    public abstract partial class CriticalHandle : CriticalFinalizerObject, IDisposable
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void ReleaseHandleFailed();
    }
}
