// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    /**
     * Line cap constants
     */
    /// <include file='doc\LineCap.uex' path='docs/doc[@for="LineCap"]/*' />
    /// <devdoc>
    ///    Specifies the available cap
    ///    styles with which a <see cref='System.Drawing.Pen'/> can end a line.
    /// </devdoc>
    public enum LineCap
    {
        /// <include file='doc\LineCap.uex' path='docs/doc[@for="LineCap.Flat"]/*' />
        /// <devdoc>
        ///    Specifies a flat line cap.
        /// </devdoc>
        Flat = 0,
        /// <include file='doc\LineCap.uex' path='docs/doc[@for="LineCap.Square"]/*' />
        /// <devdoc>
        ///    Specifies a square line cap.
        /// </devdoc>
        Square = 1,
        /// <include file='doc\LineCap.uex' path='docs/doc[@for="LineCap.Round"]/*' />
        /// <devdoc>
        ///    Specifies a round line cap.
        /// </devdoc>
        Round = 2,
        /// <include file='doc\LineCap.uex' path='docs/doc[@for="LineCap.Triangle"]/*' />
        /// <devdoc>
        ///    Specifies a triangular line cap.
        /// </devdoc>
        Triangle = 3,

        /// <include file='doc\LineCap.uex' path='docs/doc[@for="LineCap.NoAnchor"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        NoAnchor = 0x10, // corresponds to flat cap
        /// <include file='doc\LineCap.uex' path='docs/doc[@for="LineCap.SquareAnchor"]/*' />
        /// <devdoc>
        ///    Specifies no line cap.
        /// </devdoc>
        SquareAnchor = 0x11, // corresponds to square cap
        /// <include file='doc\LineCap.uex' path='docs/doc[@for="LineCap.RoundAnchor"]/*' />
        /// <devdoc>
        ///    Specifies a round anchor cap.
        /// </devdoc>
        RoundAnchor = 0x12, // corresponds to round cap
        /// <include file='doc\LineCap.uex' path='docs/doc[@for="LineCap.DiamondAnchor"]/*' />
        /// <devdoc>
        ///    Specifies a diamond anchor cap.
        /// </devdoc>
        DiamondAnchor = 0x13, // corresponds to triangle cap
        /// <include file='doc\LineCap.uex' path='docs/doc[@for="LineCap.ArrowAnchor"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies an arrow-shaped anchor cap.
        ///    </para>
        /// </devdoc>
        ArrowAnchor = 0x14, // no correspondence

        /// <include file='doc\LineCap.uex' path='docs/doc[@for="LineCap.Custom"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies a custom line cap.
        ///    </para>
        /// </devdoc>
        Custom = 0xff, // custom cap

        /// <include file='doc\LineCap.uex' path='docs/doc[@for="LineCap.AnchorMask"]/*' />
        /// <devdoc>
        ///    Specifies a mask used to check whether a
        ///    line cap is an anchor cap.
        /// </devdoc>
        AnchorMask = 0xf0  // mask to check for anchor or not.
    }
}
