//------------------------------------------------------------------------------
// <copyright file="IPropertyValueUIService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.Drawing.Design {

    using System.Diagnostics;

    using Microsoft.Win32;
    using System.Drawing.Design;
    using System.Collections;
    using System.ComponentModel;

    /// <include file='doc\IPropertyValueUIService.uex' path='docs/doc[@for="IPropertyValueUIService"]/*' />
    /// <devdoc>
    ///    <para>Provides an interface to manage the property list of 
    ///       the properties window. <see cref='System.Drawing.Design.IPropertyValueUIService'/> provides
    ///       methods that may
    ///       be used to add and remove UI components from the properties window, and to retrieve the UI components for a specific property listed in the property
    ///       browser.</para>
    /// </devdoc>
    public interface IPropertyValueUIService {
    
         /// <include file='doc\IPropertyValueUIService.uex' path='docs/doc[@for="IPropertyValueUIService.PropertyUIValueItemsChanged"]/*' />
         /// <devdoc>
         /// <para>
         ///  Adds or removes an <see cref='System.EventHandler'/> that will be invoked
         ///  when the global list of PropertyValueUIItems is modified.
         ///  </para>
         ///  </devdoc>
         event EventHandler PropertyUIValueItemsChanged;
    
         /// <include file='doc\IPropertyValueUIService.uex' path='docs/doc[@for="IPropertyValueUIService.AddPropertyValueUIHandler"]/*' />
         /// <devdoc>
         ///    <para>
         ///       Adds a <see cref='System.Drawing.Design.PropertyValueUIHandler'/>
         ///       to this service.
         ///    </para>
         /// </devdoc>
         void AddPropertyValueUIHandler(PropertyValueUIHandler newHandler);
    
         /// <include file='doc\IPropertyValueUIService.uex' path='docs/doc[@for="IPropertyValueUIService.GetPropertyUIValueItems"]/*' />
         /// <devdoc>
         /// <para>Gets all the <see cref='System.Drawing.Design.PropertyValueUIItem'/>
         /// objects that should be displayed on the specified property.</para>
         /// </devdoc>
         PropertyValueUIItem[] GetPropertyUIValueItems(ITypeDescriptorContext context, PropertyDescriptor propDesc);
         
         /// <include file='doc\IPropertyValueUIService.uex' path='docs/doc[@for="IPropertyValueUIService.NotifyPropertyValueUIItemsChanged"]/*' />
         /// <devdoc>
         /// <para>
         ///  Tell the IPropertyValueUIService implementation that the global list of PropertyValueUIItems has been modified.
         ///  </para>
         ///  </devdoc>
         void NotifyPropertyValueUIItemsChanged();
         
         /// <include file='doc\IPropertyValueUIService.uex' path='docs/doc[@for="IPropertyValueUIService.RemovePropertyValueUIHandler"]/*' />
         /// <devdoc>
         /// <para>Removes a <see cref='System.Drawing.Design.PropertyValueUIHandler'/>
         /// from this service.</para>
         /// </devdoc>
         void RemovePropertyValueUIHandler(PropertyValueUIHandler newHandler);
    }
}
