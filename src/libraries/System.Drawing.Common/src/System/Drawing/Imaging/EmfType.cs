// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Specifies the metafile type.
    /// </summary>
    public enum EmfType
    {
        /// <summary>
        /// Windows enhanced metafile. Contains GDI commands. Metafiles of this type are referred to as an EMF file.
        /// </summary>
        EmfOnly = MetafileType.Emf,
        /// <summary>
        /// Windows enhanced metafile plus. Contains GDI+ commands. Metafiles of this type are referred to as an EMF+ file.
        /// </summary>
        EmfPlusOnly = MetafileType.EmfPlusOnly,
        /// <summary>
        /// Dual Windows enhanced metafile. Contains equivalent GDI and GDI+ commands. Metafiles of this type are referred to as an EMF+ file.
        /// </summary>
        EmfPlusDual = MetafileType.EmfPlusDual
    }
}
