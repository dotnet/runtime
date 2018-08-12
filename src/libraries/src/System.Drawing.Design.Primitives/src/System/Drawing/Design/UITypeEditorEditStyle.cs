// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Drawing.Design
{
    /// <summary>
    /// Specifies identifiers to indicate the style of a <see cref='System.Drawing.Design.UITypeEditor'/>.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum UITypeEditorEditStyle
    {
        None = 1,
        Modal = 2,
        DropDown = 3
    }
}
