// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    /**
     * Various wrap modes for brushes
     */
    /// <include file='doc\MatrixOrder.uex' path='docs/doc[@for="MatrixOrder"]/*' />
    /// <devdoc>
    ///    Specifies the order for matrix transform
    ///    operations.
    /// </devdoc>
    public enum MatrixOrder
    {
        /// <include file='doc\MatrixOrder.uex' path='docs/doc[@for="MatrixOrder.Prepend"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The new operation is applied before the old
        ///       operation.
        ///    </para>
        /// </devdoc>
        Prepend = 0,
        /// <include file='doc\MatrixOrder.uex' path='docs/doc[@for="MatrixOrder.Append"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The new operation is applied after the old operation.
        ///    </para>
        /// </devdoc>
        Append = 1
    }
}
