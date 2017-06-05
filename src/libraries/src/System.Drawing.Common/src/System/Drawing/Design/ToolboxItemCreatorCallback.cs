//------------------------------------------------------------------------------
// <copyright file="ToolboxItemCreatorCallback.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.Drawing.Design {

    /// <include file='doc\ToolboxItemCreatorCallback.uex' path='docs/doc[@for="ToolboxItemCreatorCallback"]/*' />
    /// <devdoc>
    ///    <para> Represents the
    ///       method that will handle the ToolboxItemCreatorCallback event.</para>
    /// </devdoc>
    public delegate ToolboxItem ToolboxItemCreatorCallback(object serializedObject, string format);
}
