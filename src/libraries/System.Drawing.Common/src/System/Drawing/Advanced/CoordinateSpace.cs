// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    /**
     * Coordinate space identifiers
     */
    /// <include file='doc\CoordinateSpace.uex' path='docs/doc[@for="CoordinateSpace"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the system to use when evaluating coordinates.
    ///    </para>
    /// </devdoc>
    public enum CoordinateSpace
    {
        /// <include file='doc\CoordinateSpace.uex' path='docs/doc[@for="CoordinateSpace.World"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that coordinates are in the world coordinate context. World
        ///       coordinates are used in a non physical enviroment such as a modeling
        ///       environment.
        ///    </para>
        /// </devdoc>
        World = 0,
        /// <include file='doc\CoordinateSpace.uex' path='docs/doc[@for="CoordinateSpace.Page"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that coordinates are in the page coordinate context. Page
        ///       coordinates are typically used in a multiple page documents environment.
        ///    </para>
        /// </devdoc>
        Page = 1,
        /// <include file='doc\CoordinateSpace.uex' path='docs/doc[@for="CoordinateSpace.Device"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that coordinates are in the device coordinate context. Device
        ///       coordinates occur in screen coordinates just before they are drawn on the
        ///       screen.
        ///    </para>
        /// </devdoc>
        Device = 2
    }
}
