// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.Drawing.Design
{
    /// <summary>
    /// Provides an interface to manage the images, ToolTips, and event handlers for the properties of a component displayed in a property browser.
    /// </summary>
    public interface IPropertyValueUIService
    {
        ///  <summary>
        ///  Adds or removes an <see cref='System.EventHandler'/> that will be invoked when the global list of <see cref="System.Drawing.Design.PropertyValueUIItem"/> is modified.
        ///  </summary>
        event EventHandler PropertyUIValueItemsChanged;

        /// <summary>
        /// Adds the specified <see cref="System.Drawing.Design.PropertyValueUIHandler" /> to this service.
        /// </summary>
        /// <param name="newHandler">The UI handler to add. </param>
        void AddPropertyValueUIHandler(PropertyValueUIHandler newHandler);

        /// <summary>
        /// Gets the <see cref="System.Drawing.Design.PropertyValueUIItem" /> objects that match the specified context and property descriptor characteristics.
        /// </summary>
        /// <returns>An array of <see cref="System.Drawing.Design.PropertyValueUIItem" /> objects that match the specified parameters.</returns>
        /// <param name="context">An <see cref="System.ComponentModel.ITypeDescriptorContext" /> that can be used to gain additional context information. </param>
        /// <param name="propDesc">A <see cref="System.ComponentModel.PropertyDescriptor" /> that indicates the property to match with the properties to return. </param>
        PropertyValueUIItem[] GetPropertyUIValueItems(ITypeDescriptorContext context, PropertyDescriptor propDesc);

        ///  <summary>
        ///  Notifies the <see cref="System.Drawing.Design.IPropertyValueUIService"/> implementation that the global list of <see cref="System.Drawing.Design.PropertyValueUIItem"/> has been modified.
        ///  </summary>
        void NotifyPropertyValueUIItemsChanged();

        /// <summary>
        /// Removes a <see cref='System.Drawing.Design.PropertyValueUIHandler'/> from this service.
        /// </summary>
        /// <param name="newHandler">The handler to remove.</param>
        void RemovePropertyValueUIHandler(PropertyValueUIHandler newHandler);
    }
}
