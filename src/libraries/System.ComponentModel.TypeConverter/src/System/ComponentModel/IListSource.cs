// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Win32;
using System.Collections;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [
    TypeConverterAttribute("System.Windows.Forms.Design.DataSourceConverter, " + AssemblyRef.SystemDesign),
    Editor("System.Windows.Forms.Design.DataSourceListEditor, " + AssemblyRef.SystemDesign, "System.Drawing.Design.UITypeEditor, " + AssemblyRef.SystemDrawing),
    MergableProperty(false)
    ]
    public interface IListSource
    {
        bool ContainsListCollection { get; }

        IList GetList();
    }
}
