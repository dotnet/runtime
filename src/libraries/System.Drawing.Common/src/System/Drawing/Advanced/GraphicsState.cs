// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    /// <include file='doc\GraphicsState.uex' path='docs/doc[@for="GraphicsState"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public sealed class GraphicsState : MarshalByRefObject
    {
        internal int nativeState;

        internal GraphicsState(int nativeState)
        {
            this.nativeState = nativeState;
        }
    }
}

