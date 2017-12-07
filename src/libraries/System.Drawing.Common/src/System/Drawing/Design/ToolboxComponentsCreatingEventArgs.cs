//------------------------------------------------------------------------------
// <copyright file="ToolboxComponentsCreatingEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Drawing.Design {
    using System;
    using System.ComponentModel.Design;

    
    /// <include file='doc\ToolboxComponentsCreatingEventArgs.uex' path='docs/doc[@for="ToolboxComponentsCreatingEventArgs"]/*' />
    /// <devdoc>
    /// <para>Provides data for the 'ToolboxComponentsCreatingEventArgs' event that occurs
    ///    when components are added to the toolbox.</para>
    /// </devdoc>
    public class ToolboxComponentsCreatingEventArgs : EventArgs {
        private readonly IDesignerHost host;
        
        /// <include file='doc\ToolboxComponentsCreatingEventArgs.uex' path='docs/doc[@for="ToolboxComponentsCreatingEventArgs.ToolboxComponentsCreatingEventArgs"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Design.ToolboxComponentsCreatingEventArgs'/> object.
        ///    </para>
        /// </devdoc>
        public ToolboxComponentsCreatingEventArgs(IDesignerHost host) {
            this.host = host;
        }
        
        /// <include file='doc\ToolboxComponentsCreatingEventArgs.uex' path='docs/doc[@for="ToolboxComponentsCreatingEventArgs.DesignerHost"]/*' />
        /// <devdoc>
        ///    <para>
        ///       An instance of IDesignerHost that has made the creat request.  This can be null if no designer host
        ///       was provided to the toolbox item.
        ///    </para>
        /// </devdoc>
        public IDesignerHost DesignerHost {
            get {
                return host;
            }
        }
    }
}
