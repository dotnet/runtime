// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file isn't built into the .csproj in corefx but is consumed by Mono.

namespace System.Drawing.Design {
    using System;
    using System.ComponentModel;

    /// <include file='doc\ToolboxComponentsCreatingEventHandler.uex' path='docs/doc[@for="ToolboxComponentsCreatingEventHandler"]/*' />
    /// <devdoc>
    /// <para>Represents the method that will handle the <see cref='System.Drawing.Design.ToolboxItem.ComponentsCreating'/> event.</para>
    /// </devdoc>
    public delegate void ToolboxComponentsCreatingEventHandler(object sender, ToolboxComponentsCreatingEventArgs e);
}
