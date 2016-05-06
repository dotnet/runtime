// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Provides an interface that provides custom type information for an object.</para>
    /// </summary>
    public interface ICustomTypeDescriptor
    {
        /// <summary>
        /// <para>Gets a collection of type <see cref='System.Attribute'/> with the attributes 
        ///    for this object.</para>
        /// </summary>
        AttributeCollection GetAttributes();

        /// <summary>
        ///    <para>Gets the class name of this object.</para>
        /// </summary>
        string GetClassName();

        /// <summary>
        ///    <para>Gets the name of this object.</para>
        /// </summary>
        string GetComponentName();

        /// <summary>
        ///    <para>Gets a type converter for this object.</para>
        /// </summary>
        TypeConverter GetConverter();

        /// <summary>
        ///    <para>Gets the default event for this object.</para>
        /// </summary>
        EventDescriptor GetDefaultEvent();

        /// <summary>
        ///    <para>Gets the default property for this object.</para>
        /// </summary>
        PropertyDescriptor GetDefaultProperty();

        /// <summary>
        ///    <para>Gets an editor of the specified type for this object.</para>
        /// </summary>
        object GetEditor(Type editorBaseType);

        /// <summary>
        ///    <para>Gets the events for this instance of a component.</para>
        /// </summary>
        EventDescriptorCollection GetEvents();

        /// <summary>
        ///    <para>Gets the events for this instance of a component using the attribute array as a
        ///       filter.</para>
        /// </summary>
        EventDescriptorCollection GetEvents(Attribute[] attributes);

        /// <summary>
        ///    <para>Gets the properties for this instance of a component.</para>
        /// </summary>
        PropertyDescriptorCollection GetProperties();

        /// <summary>
        ///    <para>Gets the properties for this instance of a component using the attribute array as a filter.</para>
        /// </summary>
        PropertyDescriptorCollection GetProperties(Attribute[] attributes);

        /// <summary>
        ///    <para>Gets the object that directly depends on this value being edited.</para>
        /// </summary>
        object GetPropertyOwner(PropertyDescriptor pd);
    }
}
