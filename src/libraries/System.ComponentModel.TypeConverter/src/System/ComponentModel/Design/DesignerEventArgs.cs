// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides data for the System.ComponentModel.Design.IDesignerEventService.DesignerEvent
    /// event that is generated when a document is created or disposed.
    /// </summary>
    public class DesignerEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the System.ComponentModel.Design.DesignerEventArgs
        /// class.
        /// </summary>
        public DesignerEventArgs(IDesignerHost? host)
        {
            Designer = host;
        }

        /// <summary>
        /// Gets or sets the host of the document.
        /// </summary>
        public IDesignerHost? Designer { get; }
    }
}
