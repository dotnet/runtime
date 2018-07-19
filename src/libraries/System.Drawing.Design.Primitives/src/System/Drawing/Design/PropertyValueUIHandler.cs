// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;

namespace System.Drawing.Design
{
    /// <summary>Represents a delegate to be added to <see cref='System.Drawing.Design.IPropertyValueUIService'/>.</summary>
    /// <param name="context">An <see cref="System.ComponentModel.ITypeDescriptorContext" /> that can be used to obtain context information. </param>
    /// <param name="propDesc">A <see cref="System.ComponentModel.PropertyDescriptor" /> that represents the property being queried. </param>
    /// <param name="valueUIItemList">An <see cref="System.Collections.ArrayList" /> of <see cref="System.Drawing.Design.PropertyValueUIItem" /> objects containing the UI items associated with the property. </param>
    public delegate void PropertyValueUIHandler(ITypeDescriptorContext context, PropertyDescriptor propDesc, ArrayList valueUIItemList);
}
