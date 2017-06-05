//------------------------------------------------------------------------------
// <copyright file="IToolboxItemProvider.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.Drawing.Design {

    using System;

    /// <include file='doc\IToolboxItemProvider.uex' path='docs/doc[@for="IToolboxItemProvider"]/*' />
    /// <devdoc>
    /// </devdoc>
    public interface IToolboxItemProvider {
        
        /// <include file='doc\IToolboxItemProvider.uex' path='docs/doc[@for="IToolboxItemProvider.Items"]/*' />
        /// <devdoc>
        /// </devdoc>
        ToolboxItemCollection Items { get; }
    }
}
