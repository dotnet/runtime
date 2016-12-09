// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// <para>Provides data for the System.ComponentModel.Design.IDesignerEventService.DesignerEvent
    /// event that is generated when a document is created or disposed.</para>
    /// </summary>
    public class DesignerEventArgs : EventArgs
    {
        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the System.ComponentModel.Design.DesignerEventArgs
        ///       class.
        ///    </para>
        /// </summary>
        public DesignerEventArgs(IDesignerHost host)
        {
            Designer = host;
        }

        /// <summary>
        ///    <para>
        ///       Gets or
        ///       sets the host of the document.
        ///    </para>
        /// </summary>
        public IDesignerHost Designer { get; }
    }
}

