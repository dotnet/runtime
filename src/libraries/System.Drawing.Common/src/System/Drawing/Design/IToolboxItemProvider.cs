// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
