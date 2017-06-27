// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <summary>
    /// Standard paper sources.
    /// </summary>
    public enum PaperSourceKind
    {
        // Please keep these in SafeNativeMethods.cs order

        /// <summary>
        /// The upper bin of a printer (or, if the printer only has one bin, the only bin).
        /// </summary>
        Upper = SafeNativeMethods.DMBIN_UPPER,

        /// <summary>
        /// The lower bin of a printer.
        /// </summary>
        Lower = SafeNativeMethods.DMBIN_LOWER,

        /// <summary>
        /// The middle bin of a printer.
        /// </summary>
        Middle = SafeNativeMethods.DMBIN_MIDDLE,

        /// <summary>
        /// Manually-fed paper.
        /// </summary>
        Manual = SafeNativeMethods.DMBIN_MANUAL,

        /// <summary>
        /// An envelope.
        /// </summary>
        Envelope = SafeNativeMethods.DMBIN_ENVELOPE,

        /// <summary>
        /// A manually-fed envelope.
        /// </summary>
        ManualFeed = SafeNativeMethods.DMBIN_ENVMANUAL,

        /// <summary>
        /// Automatic-fed paper.
        /// </summary>
        AutomaticFeed = SafeNativeMethods.DMBIN_AUTO,

        /// <summary>
        /// A tractor feed.
        /// </summary>
        TractorFeed = SafeNativeMethods.DMBIN_TRACTOR,

        /// <summary>
        /// Small-format paper.
        /// </summary>
        SmallFormat = SafeNativeMethods.DMBIN_SMALLFMT,

        /// <summary>
        /// Large-format paper.
        /// </summary>
        LargeFormat = SafeNativeMethods.DMBIN_LARGEFMT,

        /// <summary>
        /// A large-capacity bin printer.
        /// </summary>
        LargeCapacity = SafeNativeMethods.DMBIN_LARGECAPACITY,

        /// <summary>
        /// A paper cassette.
        /// </summary>
        Cassette = SafeNativeMethods.DMBIN_CASSETTE,

        FormSource = SafeNativeMethods.DMBIN_FORMSOURCE,

        /// <summary>
        /// A printer-specific paper source.
        /// </summary>
        Custom = SafeNativeMethods.DMBIN_USER + 1,
    }
}

