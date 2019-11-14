// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <summary>
    /// Specifies the type of action for the <see cref='PrintEventArgs'/>.
    /// </summary>
    public enum PrintAction
    {
        /// <summary>
        /// Printing to a file.
        /// </summary>
        PrintToFile,
        /// <summary>
        /// Printing to a preview.
        /// </summary>
        PrintToPreview,
        /// <summary>
        /// Printing to a printer.
        /// </summary>
        PrintToPrinter
    }
}
