//------------------------------------------------------------------------------
// <copyright file="ToolboxComponentsCreatedEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

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
