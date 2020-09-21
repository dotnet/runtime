// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Drawing.Drawing2D
{
    public sealed class GraphicsState : MarshalByRefObject
    {
        internal int nativeState;

        internal GraphicsState(int nativeState) => this.nativeState = nativeState;
    }
}
