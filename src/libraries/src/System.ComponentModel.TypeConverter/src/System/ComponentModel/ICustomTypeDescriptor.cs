// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization.Formatters;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System;
using Microsoft.Win32;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Provides an interface that provides custom type information for an 
    ///       object.</para>
    /// </devdoc>
    public interface ICustomTypeDescriptor
    {
        /// <devdoc>
        /// <para>Gets a collection of type <see cref='System.Attribute'/> with the attributes 
        ///    for this object.</para>
        /// </devdoc>
        AttributeCollection GetAttributes();

        /// <devdoc>
        ///    <para>Gets the class name of this object.</para>
        /// </devdoc>
        string GetClassName();

        /// <devdoc>
        ///    <para>Gets the name of this object.</para>
        /// </devdoc>
        string GetComponentName();

        /// <devdoc>
        ///    <para>Gets a type converter for this object.</para>
        /// </devdoc>
        TypeConverter GetConverter();

        /// <devdoc>
        ///    <para>Gets the default event for this object.</para>
        /// </devdoc>
        EventDescriptor GetDefaultEvent();


        /// <devdoc>
        ///    <para>Gets the default property for this object.</para>
        /// </devdoc>
        PropertyDescriptor GetDefaultProperty();

        /// <devdoc>
        ///    <para>Gets an editor of the specified type for this object.</para>
        /// </devdoc>
        object GetEditor(Type editorBaseType);

        /// <devdoc>
        ///    <para>Gets the events for this instance of a component.</para>
        /// </devdoc>
        EventDescriptorCollection GetEvents();

        /// <devdoc>
        ///    <para>Gets the events for this instance of a component using the attribute array as a
        ///       filter.</para>
        /// </devdoc>
        EventDescriptorCollection GetEvents(Attribute[] attributes);

        /// <devdoc>
        ///    <para>Gets the properties for this instance of a component.</para>
        /// </devdoc>
        PropertyDescriptorCollection GetProperties();

        /// <devdoc>
        ///    <para>Gets the properties for this instance of a component using the attribute array as a filter.</para>
        /// </devdoc>
        PropertyDescriptorCollection GetProperties(Attribute[] attributes);

        /// <devdoc>
        ///    <para>Gets the object that directly depends on this value being edited.</para>
        /// </devdoc>
        object GetPropertyOwner(PropertyDescriptor pd);
    }
}
