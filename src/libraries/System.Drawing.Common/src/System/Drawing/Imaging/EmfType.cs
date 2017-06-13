// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /**
     * EmfType Type
     */
    /// <include file='doc\EmfType.uex' path='docs/doc[@for="EmfType"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the metafile type.
    ///    </para>
    /// </devdoc>
    public enum EmfType
    {
        /// <include file='doc\EmfType.uex' path='docs/doc[@for="EmfType.EmfOnly"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Windows enhanced metafile. Contains GDI commands. Metafiles of this type are
        ///       refered to as an EMF file.
        ///    </para>
        /// </devdoc>
        EmfOnly = MetafileType.Emf,
        /// <include file='doc\EmfType.uex' path='docs/doc[@for="EmfType.EmfPlusOnly"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Windows enhanced metafile plus. Contains GDI+ commands. Metafiles of this
        ///       type are refered to as an EMF+ file.
        ///    </para>
        /// </devdoc>
        EmfPlusOnly = MetafileType.EmfPlusOnly,
        /// <include file='doc\EmfType.uex' path='docs/doc[@for="EmfType.EmfPlusDual"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Dual Windows enhanced metafile. Contains equivalent GDI and GDI+ commands.
        ///       Metafiles of this type are refered to as an EMF+ file.
        ///    </para>
        /// </devdoc>
        EmfPlusDual = MetafileType.EmfPlusDual
    }
}
