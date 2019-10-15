// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides data for the <see cref='System.ComponentModel.Design.IDesignerEventService.ActiveDesigner'/>
    /// event.
    /// </summary>
    public class ActiveDesignerEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.Design.ActiveDesignerEventArgs'/>
        /// class.
        /// </summary>
        public ActiveDesignerEventArgs(IDesignerHost oldDesigner, IDesignerHost newDesigner)
        {
            OldDesigner = oldDesigner;
            NewDesigner = newDesigner;
        }

        /// <summary>
        /// Gets or sets the document that is losing activation.
        /// </summary>
        public IDesignerHost OldDesigner { get; }

        /// <summary>
        /// Gets or sets the document that is gaining activation.
        /// </summary>
        public IDesignerHost NewDesigner { get; }
    }
}
