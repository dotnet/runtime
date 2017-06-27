// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <summary>
    /// Represents the method that will handle the <see cref='PrintDocument.BeginPrint'/>,
    /// <see cref='PrintDocument.EndPrint'/>, or <see cref='PrintDocument.QueryPageSettings'/>
    /// event of a <see cref='PrintDocument'/>.
    /// </summary>
    public delegate void PrintEventHandler(object sender, PrintEventArgs e);
}

