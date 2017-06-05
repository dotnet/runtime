// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    /**
     * Represent the internal data of a Graphics Container object
     */
    /// <include file='doc\GraphicsContainer.uex' path='docs/doc[@for="GraphicsContainer"]/*' />
    /// <devdoc>
    ///    Represents the internal data of a graphics
    ///    container.
    /// </devdoc>
    public sealed class GraphicsContainer : MarshalByRefObject
    {
        /**
         * @notes How do we want to expose region data?
         *
         * @notes Need serialization methods too.  Needs to be defined.
         */

        internal GraphicsContainer(int graphicsContainer)
        {
            nativeGraphicsContainer = graphicsContainer;
        }

        internal int nativeGraphicsContainer;
    }
}
