//------------------------------------------------------------------------------
// <copyright file="PropertyValueUIItemInvokeHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.Drawing.Design {

    using System.Diagnostics;

    using Microsoft.Win32;
    using System.Collections;
    using System.ComponentModel.Design;
    using System.ComponentModel;
   
    /// <include file='doc\PropertyValueUIItemInvokeHandler.uex' path='docs/doc[@for="PropertyValueUIItemInvokeHandler"]/*' />
    /// <devdoc>
    ///    <para>Represents the method that will handle the event 
    ///       raised when an icon in the properties window associated with
    ///       a <see cref='System.Drawing.Design.PropertyValueUIItem'/> is
    ///       double-clicked.</para>
    /// </devdoc>
    public delegate void PropertyValueUIItemInvokeHandler(ITypeDescriptorContext context, PropertyDescriptor descriptor, PropertyValueUIItem invokedItem);
}
