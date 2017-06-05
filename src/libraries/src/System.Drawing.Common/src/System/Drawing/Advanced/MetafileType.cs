// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /**
     * MetafileType Type
     */
    /// <include file='doc\MetafileType.uex' path='docs/doc[@for="MetafileType"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the format of a <see cref='System.Drawing.Imaging.Metafile'/>.
    ///    </para>
    /// </devdoc>
    public enum MetafileType
    {
        /// <include file='doc\MetafileType.uex' path='docs/doc[@for="MetafileType.Invalid"]/*' />
        /// <devdoc>
        ///    Specifies an invalid type.
        /// </devdoc>
        Invalid,            // Invalid metafile
                            /// <include file='doc\MetafileType.uex' path='docs/doc[@for="MetafileType.Wmf"]/*' />
                            /// <devdoc>
                            ///    Specifies a standard Windows metafile.
                            /// </devdoc>
        Wmf,                // Standard WMF
                            /// <include file='doc\MetafileType.uex' path='docs/doc[@for="MetafileType.WmfPlaceable"]/*' />
                            /// <devdoc>
                            ///    Specifies a Windows Placeable metafile.
                            /// </devdoc>
        WmfPlaceable,           // Placeable Metafile format
                                /// <include file='doc\MetafileType.uex' path='docs/doc[@for="MetafileType.Emf"]/*' />
                                /// <devdoc>
                                ///    Specifies a Windows enhanced metafile.
                                /// </devdoc>
        Emf,                // EMF (not EMF+)
                            /// <include file='doc\MetafileType.uex' path='docs/doc[@for="MetafileType.EmfPlusOnly"]/*' />
                            /// <devdoc>
                            ///    Specifies a Windows enhanced metafile plus.
                            /// </devdoc>
        EmfPlusOnly,        // EMF+ without dual, down-level records
                            /// <include file='doc\MetafileType.uex' path='docs/doc[@for="MetafileType.EmfPlusDual"]/*' />
                            /// <devdoc>
                            ///    Specifies both enhanced and enhanced plus
                            ///    commands in the same file.
                            /// </devdoc>
        EmfPlusDual,        // EMF+ with dual, down-level records
    }
}
