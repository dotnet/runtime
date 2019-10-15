// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /// <summary>
    /// Specifies the format of a <see cref='Metafile'/>.
    /// </summary>
    public enum MetafileType
    {
        /// <summary>
        /// Specifies an invalid type.
        /// </summary>
        Invalid,
        /// <summary>
        /// Specifies a standard Windows metafile.
        /// </summary>
        Wmf,
        /// <summary>
        /// Specifies a Windows Placeable metafile.
        /// </summary>
        WmfPlaceable,
        /// <summary>
        /// Specifies a Windows enhanced metafile.
        /// </summary>
        Emf,
        /// <summary>
        /// Specifies a Windows enhanced metafile plus.
        /// </summary>
        EmfPlusOnly,
        /// <summary>
        /// Specifies both enhanced and enhanced plus commands in the same file.
        /// </summary>
        EmfPlusDual,
    }
}
