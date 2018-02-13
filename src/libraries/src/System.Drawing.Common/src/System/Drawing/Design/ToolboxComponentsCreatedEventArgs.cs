// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Design {
    using System;
    using System.ComponentModel;

    
    /// <include file='doc\ToolboxComponentsCreatedEventArgs.uex' path='docs/doc[@for="ToolboxComponentsCreatedEventArgs"]/*' />
    /// <devdoc>
    /// <para>Provides data for the 'ToolboxComponentsCreatedEventArgs' event that occurs
    ///    when components are added to the toolbox.</para>
    /// </devdoc>
    public class ToolboxComponentsCreatedEventArgs : EventArgs {
        private readonly IComponent[] comps;
        
        /// <include file='doc\ToolboxComponentsCreatedEventArgs.uex' path='docs/doc[@for="ToolboxComponentsCreatedEventArgs.ToolboxComponentsCreatedEventArgs"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Design.ToolboxComponentsCreatedEventArgs'/> object.
        ///    </para>
        /// </devdoc>
        public ToolboxComponentsCreatedEventArgs(IComponent[] components) {
            this.comps = components;
        }
        
        /// <include file='doc\ToolboxComponentsCreatedEventArgs.uex' path='docs/doc[@for="ToolboxComponentsCreatedEventArgs.Components"]/*' />
        /// <devdoc>
        ///    <para>
        ///       An array storing the toolbox components.
        ///    </para>
        /// </devdoc>
        public IComponent[] Components {
            get {
                return (IComponent[])comps.Clone();
            }
        }
    }
}
