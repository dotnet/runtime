// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    [StructLayout(LayoutKind.Sequential)]
    public partial class MarshalAsAttribute
    {
        internal object CloneInternal() => MemberwiseClone();
    }
}
