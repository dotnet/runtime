// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file isn't built into the .csproj in corefx but is consumed by Mono.

namespace System.Drawing.Design {

    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;     
    
    /// <include file='doc\UITypeEditorEditStyle.uex' path='docs/doc[@for="UITypeEditorEditStyle"]/*' />
    /// <devdoc>
    ///    <para>Specifies identifiers to indicate the style of
    ///       a <see cref='System.Drawing.Design.UITypeEditor'/>.</para>
    /// </devdoc>    
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum UITypeEditorEditStyle {
        /// <include file='doc\UITypeEditorEditStyle.uex' path='docs/doc[@for="UITypeEditorEditStyle.None"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Indicates no editor style.
        ///    </para>
        /// </devdoc>
        None = 1,
        /// <include file='doc\UITypeEditorEditStyle.uex' path='docs/doc[@for="UITypeEditorEditStyle.Modal"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Indicates a modal editor style.
        ///    </para>
        /// </devdoc>
        Modal = 2,
        /// <include file='doc\UITypeEditorEditStyle.uex' path='docs/doc[@for="UITypeEditorEditStyle.DropDown"]/*' />
        /// <devdoc>
        ///    <para> Indicates a drop-down editor style.</para>
        /// </devdoc>
        DropDown = 3
    }
}

