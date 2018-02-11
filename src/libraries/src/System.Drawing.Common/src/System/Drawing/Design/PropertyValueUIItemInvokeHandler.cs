// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
