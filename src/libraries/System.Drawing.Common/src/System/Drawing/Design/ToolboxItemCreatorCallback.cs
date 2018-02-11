// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Design {

    /// <include file='doc\ToolboxItemCreatorCallback.uex' path='docs/doc[@for="ToolboxItemCreatorCallback"]/*' />
    /// <devdoc>
    ///    <para> Represents the
    ///       method that will handle the ToolboxItemCreatorCallback event.</para>
    /// </devdoc>
    public delegate ToolboxItem ToolboxItemCreatorCallback(object serializedObject, string format);
}
