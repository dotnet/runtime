// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
 */
namespace System.Drawing.Design {

    using System.Diagnostics;

    using Microsoft.Win32;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Collections;
       
    /// <include file='doc\PropertyValueUIHandler.uex' path='docs/doc[@for="PropertyValueUIHandler"]/*' />
    /// <devdoc>
    /// <para>Represents a delegate to be added to <see cref='System.Drawing.Design.IPropertyValueUIService'/>.</para>
    /// </devdoc>
    public delegate void PropertyValueUIHandler(ITypeDescriptorContext context, PropertyDescriptor propDesc, ArrayList valueUIItemList);
}

