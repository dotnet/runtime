// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file isn't built into the .csproj in corefx but is consumed by Mono.

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
