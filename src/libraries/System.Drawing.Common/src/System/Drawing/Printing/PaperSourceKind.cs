// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <include file='doc\PaperSourceKind.uex' path='docs/doc[@for="PaperSourceKind"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Standard paper sources.
    ///    </para>
    /// </devdoc>
    public enum PaperSourceKind
    {
        // Please keep these in SafeNativeMethods.cs order

        /// <include file='doc\PaperSourceKind.uex' path='docs/doc[@for="PaperSourceKind.Upper"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The upper bin of a printer (or, if the printer only has one bin, the only bin).
        ///    </para>
        /// </devdoc>
        Upper = SafeNativeMethods.DMBIN_UPPER,

        /// <include file='doc\PaperSourceKind.uex' path='docs/doc[@for="PaperSourceKind.Lower"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The lower bin of a printer.
        ///    </para>
        /// </devdoc>
        Lower = SafeNativeMethods.DMBIN_LOWER,

        /// <include file='doc\PaperSourceKind.uex' path='docs/doc[@for="PaperSourceKind.Middle"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The middle bin of a printer.
        ///    </para>
        /// </devdoc>
        Middle = SafeNativeMethods.DMBIN_MIDDLE,

        /// <include file='doc\PaperSourceKind.uex' path='docs/doc[@for="PaperSourceKind.Manual"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Manually-fed paper.
        ///    </para>
        /// </devdoc>
        Manual = SafeNativeMethods.DMBIN_MANUAL,

        /// <include file='doc\PaperSourceKind.uex' path='docs/doc[@for="PaperSourceKind.Envelope"]/*' />
        /// <devdoc>
        ///    <para>
        ///       An envelope.
        ///       
        ///    </para>
        /// </devdoc>
        Envelope = SafeNativeMethods.DMBIN_ENVELOPE,

        /// <include file='doc\PaperSourceKind.uex' path='docs/doc[@for="PaperSourceKind.ManualFeed"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A manually-fed envelope.
        ///    </para>
        /// </devdoc>
        ManualFeed = SafeNativeMethods.DMBIN_ENVMANUAL,

        /// <include file='doc\PaperSourceKind.uex' path='docs/doc[@for="PaperSourceKind.AutomaticFeed"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Automatic-fed paper.
        ///       
        ///    </para>
        /// </devdoc>
        AutomaticFeed = SafeNativeMethods.DMBIN_AUTO,

        /// <include file='doc\PaperSourceKind.uex' path='docs/doc[@for="PaperSourceKind.TractorFeed"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A tractor feed.
        ///    </para>
        /// </devdoc>
        TractorFeed = SafeNativeMethods.DMBIN_TRACTOR,

        /// <include file='doc\PaperSourceKind.uex' path='docs/doc[@for="PaperSourceKind.SmallFormat"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Small-format paper.
        ///    </para>
        /// </devdoc>
        SmallFormat = SafeNativeMethods.DMBIN_SMALLFMT,

        /// <include file='doc\PaperSourceKind.uex' path='docs/doc[@for="PaperSourceKind.LargeFormat"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Large-format paper.
        ///    </para>
        /// </devdoc>
        LargeFormat = SafeNativeMethods.DMBIN_LARGEFMT,

        /// <include file='doc\PaperSourceKind.uex' path='docs/doc[@for="PaperSourceKind.LargeCapacity"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A large-capacity
        ///       bin a printer.
        ///       
        ///    </para>
        /// </devdoc>
        LargeCapacity = SafeNativeMethods.DMBIN_LARGECAPACITY,

        /// <include file='doc\PaperSourceKind.uex' path='docs/doc[@for="PaperSourceKind.Cassette"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A paper cassette.
        ///    </para>
        /// </devdoc>
        Cassette = SafeNativeMethods.DMBIN_CASSETTE,

        /// <include file='doc\PaperSourceKind.uex' path='docs/doc[@for="PaperSourceKind.FormSource"]/*' />
        /// <devdoc>
        /// </devdoc>
        FormSource = SafeNativeMethods.DMBIN_FORMSOURCE,

        /// <include file='doc\PaperSourceKind.uex' path='docs/doc[@for="PaperSourceKind.Custom"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A printer-specific paper source.
        ///    </para>
        /// </devdoc>
        Custom = SafeNativeMethods.DMBIN_USER + 1,
    }
}

